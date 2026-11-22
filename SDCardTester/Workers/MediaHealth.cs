using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DjSoft.Tools.SDCardTester.Workers
{
    #region class MediaHealth : průběžné vyhodnocení zdraví zdrojového média ("Teploměr zdraví")
    /// <summary>
    /// Průběžné vyhodnocení zdraví zdrojového média ("Teploměr zdraví") z čtení bloků v aktuálním běhu.
    /// <para/>
    /// Data dodává thread Work (<see cref="StartFile(string)"/>, <see cref="AddBlockRead"/>, <see cref="AddHang"/>),
    /// výsledek čte GUI thread (<see cref="GetResult(bool, bool)"/>); přístup je chráněn zámkem.
    /// <para/>
    /// Hodnotí se jen aktuální běh (rozhodnutí autora 2026-11-21): časy z jiného dne, jiné čtečky nebo portu by hodnocení zkreslily.
    /// Absolutní rychlost se nehodnotí (závisí na čtečce a portu). Tichý převrat bitů (data přečtena bez chyby, ale jiná) nelze bez reference poznat.
    /// <para/>
    /// Verdikt = nejhorší ukazatel:
    /// <code>
    /// Ukazatel                       Excelentní   V pořádku    S rizikem    Vyhodit
    /// Nečitelné bloky                0            0            ≤ 0,1 %      > 0,1 %
    /// Přečteno až po opakování       0            ≤ 0,01 %     ≤ 1 %        > 1 %
    /// Odpojení / zamrznutí           0            0            1–2          ≥ 3
    /// Bloky čtené déle než 0,5 s     0            ≤ 0,01 %     ≤ 0,5 %      > 0,5 %
    /// Bloky čtené déle než 0,1 s     ≤ 0,01 %     ≤ 0,5 %      ≤ 5 %        > 5 %
    /// Stabilita (p5 / medián)        ≥ 0,30       ≥ 0,15       nižší        –
    /// </code>
    /// </summary>
    internal class MediaHealth
    {
        #region Konstanty
        /// <summary>
        /// Pod tímto objemem přečtených dat je hodnocení jen předběžné (zobrazí se "Zatím málo dat", pokud není výsledek horší než "V pořádku")
        /// </summary>
        public const long MinDataBytes = 256L * 1024L * 1024L;
        /// <summary>
        /// Práh pomalého čtení bloku (sekundy)
        /// </summary>
        public const double SlowReadSeconds = 0.1d;
        /// <summary>
        /// Práh výpadku čtení bloku (sekundy)
        /// </summary>
        public const double StallReadSeconds = 0.5d;
        /// <summary>
        /// Pojistka pro pomalé čtečky a porty: práh pomalého čtení a výpadku je nejméně tento násobek mediánu doby čtení bloku.
        /// Na pomalém portu (medián nad 33 ms na blok) by jinak běžné bloky překračovaly 0,1 s a zdravá karta by vyšla jako vadná.
        /// </summary>
        public const double SlowReadMedianFactor = 3d;
        /// <summary>
        /// Počet po sobě přečtených bloků pro výpočet rychlosti okna (stejně jako barvení podle rychlosti)
        /// </summary>
        public const int SpeedWindowBlocks = 16;
        /// <summary>
        /// Minimální počet vzorků rychlosti okna pro vyhodnocení stability
        /// </summary>
        public const int MinSpeedSamples = 64;
        /// <summary>
        /// Minimální počet úspěšně přečtených bloků pro výpočet mediánu doby čtení
        /// </summary>
        public const int MinMedianSamples = 32;
        /// <summary>
        /// Percentil pomalých úseků pro stabilitu rychlosti (p5)
        /// </summary>
        public const double StabilityPercentile = 0.05d;
        /// <summary>
        /// Počet tříd histogramu na jeden řád (desítkový logaritmus) = rozlišení cca 4,7 %
        /// </summary>
        private const int BinsPerDecade = 50;
        /// <summary>
        /// Histogram časů: dolní mez log10(sekundy) = 1 µs
        /// </summary>
        private const double TimeMinLog = -6d;
        /// <summary>
        /// Histogram časů: počet řádů (do 1000 s)
        /// </summary>
        private const int TimeDecades = 9;
        /// <summary>
        /// Histogram rychlostí: dolní mez log10(MB/s) = 0,001 MB/s
        /// </summary>
        private const double SpeedMinLog = -3d;
        /// <summary>
        /// Histogram rychlostí: počet řádů (do 100 000 MB/s)
        /// </summary>
        private const int SpeedDecades = 8;
        #endregion
        #region Konstrukce, data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MediaHealth()
        {
            Reset();
        }
        /// <summary>
        /// Vynuluje všechna data (nový běh)
        /// </summary>
        public void Reset()
        {
            lock (__Lock)
            {
                __FileKey = "";
                __FailedBlocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                __TotalBlocks = 0L;
                __RecoveredBlocks = 0L;
                __ReadErrors = 0L;
                __BytesRead = 0L;
                __TimedReads = 0L;
                __SlowReads = 0L;
                __StallReads = 0L;
                __SuccessReads = 0L;
                __Hangs = 0;
                __SpeedSamples = 0L;
                __AttemptTimes = new long[TimeDecades * BinsPerDecade];
                __SuccessTimes = new long[TimeDecades * BinsPerDecade];
                __WindowSpeeds = new long[SpeedDecades * BinsPerDecade];
                __WindowBytes = new long[SpeedWindowBlocks];
                __WindowSeconds = new double[SpeedWindowBlocks];
                _WindowReset();
            }
        }
        private readonly object __Lock = new object();
        /// <summary>Klíč aktuálního souboru (plné jméno zdroje)</summary>
        private string __FileKey;
        /// <summary>Bloky, jejichž čtení v tomto běhu selhalo a dosud nebyly přečteny (klíč = soubor + pozice)</summary>
        private HashSet<string> __FailedBlocks;
        /// <summary>Počet různých bloků zpracovaných v tomto běhu (přečtených nebo chybných)</summary>
        private long __TotalBlocks;
        /// <summary>Počet bloků přečtených až po předchozí chybě (v tomto nebo dřívějším běhu)</summary>
        private long __RecoveredBlocks;
        /// <summary>Počet chybných pokusů o čtení</summary>
        private long __ReadErrors;
        /// <summary>Úspěšně přečtené bajty</summary>
        private long __BytesRead;
        /// <summary>Počet změřených pokusů o čtení bloků bez předchozí chyby (základ pro pomalé čtení a výpadky)</summary>
        private long __TimedReads;
        /// <summary>Z <see cref="__TimedReads"/>: počet čtení delších než <see cref="SlowReadSeconds"/></summary>
        private long __SlowReads;
        /// <summary>Z <see cref="__TimedReads"/>: počet čtení delších než <see cref="StallReadSeconds"/></summary>
        private long __StallReads;
        /// <summary>Z <see cref="__TimedReads"/>: počet úspěšných čtení</summary>
        private long __SuccessReads;
        /// <summary>Počet zamrznutí zdroje (operace delší než práh varování, nebo opuštěná)</summary>
        private int __Hangs;
        /// <summary>Počet vzorků rychlosti okna</summary>
        private long __SpeedSamples;
        /// <summary>Histogram doby všech změřených pokusů o čtení (log10 sekund)</summary>
        private long[] __AttemptTimes;
        /// <summary>Histogram doby úspěšných čtení (log10 sekund), pro medián</summary>
        private long[] __SuccessTimes;
        /// <summary>Histogram rychlosti posuvného okna (log10 MB/s)</summary>
        private long[] __WindowSpeeds;
        /// <summary>Posuvné okno rychlosti: bajty bloků (kruhový buffer)</summary>
        private long[] __WindowBytes;
        /// <summary>Posuvné okno rychlosti: doby čtení bloků (kruhový buffer)</summary>
        private double[] __WindowSeconds;
        private int __WindowIndex;
        private int __WindowCount;
        private long __WindowSumBytes;
        private double __WindowSumSeconds;
        #endregion
        #region Vstup dat (thread Work)
        /// <summary>
        /// Začátek práce na souboru: posuvné okno rychlosti začíná znovu (rychlost se počítá jen ze sousedních bloků téhož souboru)
        /// </summary>
        /// <param name="fileKey">Plné jméno zdrojového souboru</param>
        public void StartFile(string fileKey)
        {
            lock (__Lock)
            {
                __FileKey = fileKey ?? "";
                _WindowReset();
            }
        }
        /// <summary>
        /// Započítá jeden pokus o čtení bloku.
        /// <para/>
        /// Ztráta zdroje (odpojená / zamrzlá karta) se sem nehlásí, není to chyba bloku.
        /// </summary>
        /// <param name="blockStart">Pozice bloku v souboru</param>
        /// <param name="bytes">Počet přečtených bajtů (u chyby se nepoužije)</param>
        /// <param name="seconds">Doba čtení; null = neznámá (nezapočte se do pomalého čtení a rychlosti)</param>
        /// <param name="isError">Čtení selhalo</param>
        /// <param name="hadPreviousErrors">Blok měl chybu už před tímto pokusem (opakované čtení)</param>
        public void AddBlockRead(long blockStart, int bytes, double? seconds, bool isError, bool hadPreviousErrors)
        {
            lock (__Lock)
            {
                string key = __FileKey + "|" + blockStart.ToString();
                bool wasFailed = __FailedBlocks.Contains(key);
                if (isError)
                {
                    if (!wasFailed)
                    {
                        __FailedBlocks.Add(key);
                        __TotalBlocks++;
                    }
                    __ReadErrors++;
                    _WindowReset();                                        // Chyba přerušuje souvislý úsek čtení
                }
                else
                {
                    if (wasFailed)
                    {
                        __FailedBlocks.Remove(key);
                        __RecoveredBlocks++;
                    }
                    else
                    {
                        __TotalBlocks++;
                        if (hadPreviousErrors) __RecoveredBlocks++;
                    }
                    __BytesRead += Math.Max(0, bytes);
                }

                // Doba čtení se hodnotí jen u bloků bez předchozí chyby: opakované čtení vadného místa je pomalé a je už započteno chybou.
                if (wasFailed || hadPreviousErrors || !seconds.HasValue || seconds.Value < 0d) return;
                double time = seconds.Value;
                __TimedReads++;
                _AddToHistogram(__AttemptTimes, time, TimeMinLog);
                if (time > SlowReadSeconds) __SlowReads++;
                if (time > StallReadSeconds) __StallReads++;
                if (isError) return;

                __SuccessReads++;
                _AddToHistogram(__SuccessTimes, time, TimeMinLog);
                _WindowAdd(bytes, time);
            }
        }
        /// <summary>
        /// Započítá zamrznutí zdroje: operace se zdrojem trvala déle než práh varování, nebo byla opuštěna
        /// </summary>
        public void AddHang()
        {
            lock (__Lock)
            {
                __Hangs++;
            }
        }
        /// <summary>
        /// Vyprázdní posuvné okno rychlosti
        /// </summary>
        private void _WindowReset()
        {
            __WindowIndex = 0;
            __WindowCount = 0;
            __WindowSumBytes = 0L;
            __WindowSumSeconds = 0d;
        }
        /// <summary>
        /// Přidá blok do posuvného okna rychlosti; je-li okno plné, započte jeho rychlost do histogramu
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="seconds"></param>
        private void _WindowAdd(int bytes, double seconds)
        {
            if (__WindowCount == SpeedWindowBlocks)
            {
                __WindowSumBytes -= __WindowBytes[__WindowIndex];
                __WindowSumSeconds -= __WindowSeconds[__WindowIndex];
            }
            else
                __WindowCount++;
            __WindowBytes[__WindowIndex] = bytes;
            __WindowSeconds[__WindowIndex] = seconds;
            __WindowSumBytes += bytes;
            __WindowSumSeconds += seconds;
            __WindowIndex = (__WindowIndex + 1) % SpeedWindowBlocks;

            if (__WindowCount == SpeedWindowBlocks && __WindowSumSeconds > 0d)
            {
                double speedMBs = (double)__WindowSumBytes / (1024d * 1024d) / __WindowSumSeconds;
                _AddToHistogram(__WindowSpeeds, speedMBs, SpeedMinLog);
                __SpeedSamples++;
            }
        }
        #endregion
        #region Histogramy
        /// <summary>
        /// Započte hodnotu do logaritmického histogramu; hodnoty mimo rozsah do krajní třídy
        /// </summary>
        private static void _AddToHistogram(long[] histogram, double value, double minLog)
        {
            histogram[_GetBin(histogram, value, minLog)]++;
        }
        /// <summary>
        /// Vrátí index třídy histogramu pro danou hodnotu
        /// </summary>
        private static int _GetBin(long[] histogram, double value, double minLog)
        {
            if (value <= 0d) return 0;
            int bin = (int)Math.Floor((Math.Log10(value) - minLog) * BinsPerDecade);
            return (bin < 0 ? 0 : (bin >= histogram.Length ? histogram.Length - 1 : bin));
        }
        /// <summary>
        /// Vrátí hodnotu daného percentilu (0 ÷ 1) z logaritmického histogramu (střed třídy); null pokud je histogram prázdný
        /// </summary>
        private static double? _GetPercentile(long[] histogram, double percentile, double minLog)
        {
            long count = histogram.Sum();
            if (count <= 0L) return null;
            long target = (long)Math.Ceiling(percentile * count);
            if (target < 1L) target = 1L;
            long sum = 0L;
            for (int i = 0; i < histogram.Length; i++)
            {
                sum += histogram[i];
                if (sum >= target)
                    return Math.Pow(10d, minLog + (i + 0.5d) / BinsPerDecade);
            }
            return null;
        }
        /// <summary>
        /// Vrátí počet hodnot v histogramu, jejichž třída leží nad danou hodnotou (střed třídy &gt; <paramref name="threshold"/>)
        /// </summary>
        private static long _GetCountAbove(long[] histogram, double threshold, double minLog)
        {
            long count = 0L;
            for (int i = 0; i < histogram.Length; i++)
            {
                double center = Math.Pow(10d, minLog + (i + 0.5d) / BinsPerDecade);
                if (center > threshold) count += histogram[i];
            }
            return count;
        }
        #endregion
        #region Výsledek (GUI thread)
        /// <summary>
        /// Vrátí aktuální vyhodnocení
        /// </summary>
        /// <param name="sourceLost">Zdroj se v tomto běhu odpojil (běh byl ukončen)</param>
        /// <param name="hangInProgress">Právě probíhající operace se zdrojem visí déle než práh varování</param>
        /// <returns></returns>
        public HealthResult GetResult(bool sourceLost, bool hangInProgress)
        {
            lock (__Lock)
            {
                var result = new HealthResult();
                result.TotalBlocks = __TotalBlocks;
                result.UnreadableBlocks = __FailedBlocks.Count;
                result.RecoveredBlocks = __RecoveredBlocks;
                result.ReadErrors = __ReadErrors;
                result.BytesRead = __BytesRead;
                result.Hangs = __Hangs + (hangInProgress ? 1 : 0);
                result.DeviceLosts = (sourceLost && __Hangs == 0 && !hangInProgress ? 1 : 0);   // Zamrznutí, které ukončilo běh, je už započteno jako zamrznutí
                result.MedianReadSeconds = (__SuccessReads >= MinMedianSamples ? _GetPercentile(__SuccessTimes, 0.5d, TimeMinLog) : null);

                int deviceEvents = result.Hangs + result.DeviceLosts;
                result.HasData = (__TotalBlocks > 0L || deviceEvents > 0);
                result.IsPreliminary = (__BytesRead < MinDataBytes);

                double total = Math.Max(1L, __TotalBlocks);
                var indicators = new List<HealthIndicator>();

                // A. Nečitelné bloky (vzdané nebo dosud chybné):
                double unreadable = result.UnreadableBlocks / total;
                indicators.Add(HealthIndicator.CreateLowerBetter("Nečitelné bloky", $"{result.UnreadableBlocks:N0} ({FormatPercent(unreadable)})",
                    unreadable, 0d, 0d, 0.001d, "0 | 0 | ≤ 0,1 % | > 0,1 %",
                    $"Nečitelné bloky: {result.UnreadableBlocks:N0} ({FormatPercent(unreadable)})"));

                // A2. Přečteno až po opakování:
                double recovered = result.RecoveredBlocks / total;
                indicators.Add(HealthIndicator.CreateLowerBetter("Přečteno po opakování", $"{result.RecoveredBlocks:N0} ({FormatPercent(recovered)})",
                    recovered, 0d, 0.0001d, 0.01d, "0 | ≤ 0,01 % | ≤ 1 % | > 1 %",
                    $"Přečteno až po opakování: {result.RecoveredBlocks:N0} bl. ({FormatPercent(recovered)})"));

                // B. Odpojení a zamrznutí:
                indicators.Add(HealthIndicator.CreateLowerBetter("Odpojení / zamrznutí", $"{result.DeviceLosts} / {result.Hangs}",
                    deviceEvents, 0d, 0d, 2d, "0 | 0 | 1–2 | ≥ 3",
                    GetDeviceEventsText(result.DeviceLosts, result.Hangs)));

                // C. Výpadky a pomalé čtení; práh nejméně násobek mediánu (pomalá čtečka / port):
                double stallThreshold = StallReadSeconds;
                double slowThreshold = SlowReadSeconds;
                long stallReads = __StallReads;
                long slowReads = __SlowReads;
                double median = result.MedianReadSeconds ?? 0d;
                if (SlowReadMedianFactor * median > slowThreshold)
                {
                    slowThreshold = SlowReadMedianFactor * median;
                    slowReads = _GetCountAbove(__AttemptTimes, slowThreshold, TimeMinLog);
                }
                if (SlowReadMedianFactor * median > stallThreshold)
                {
                    stallThreshold = SlowReadMedianFactor * median;
                    stallReads = _GetCountAbove(__AttemptTimes, stallThreshold, TimeMinLog);
                }
                double timed = Math.Max(1L, __TimedReads);
                double stall = stallReads / timed;
                double slow = slowReads / timed;
                result.StallThresholdSeconds = stallThreshold;
                result.SlowThresholdSeconds = slowThreshold;
                indicators.Add(HealthIndicator.CreateLowerBetter($"Čtení bloku > {FormatSeconds(stallThreshold)}", $"{stallReads:N0} ({FormatPercent(stall)})",
                    stall, 0d, 0.0001d, 0.005d, "0 | ≤ 0,01 % | ≤ 0,5 % | > 0,5 %",
                    $"Výpadky čtení > {FormatSeconds(stallThreshold)}: {FormatPercent(stall)} bloků"));
                indicators.Add(HealthIndicator.CreateLowerBetter($"Čtení bloku > {FormatSeconds(slowThreshold)}", $"{slowReads:N0} ({FormatPercent(slow)})",
                    slow, 0.0001d, 0.005d, 0.05d, "≤ 0,01 % | ≤ 0,5 % | ≤ 5 % | > 5 %",
                    $"Pomalé čtení > {FormatSeconds(slowThreshold)}: {FormatPercent(slow)} bloků"));

                // D. Stabilita rychlosti (p5 / medián rychlosti okna):
                double? stability = null;
                if (__SpeedSamples >= MinSpeedSamples)
                {
                    var p5 = _GetPercentile(__WindowSpeeds, StabilityPercentile, SpeedMinLog);
                    var p50 = _GetPercentile(__WindowSpeeds, 0.5d, SpeedMinLog);
                    if (p5.HasValue && p50.HasValue && p50.Value > 0d)
                    {
                        stability = Math.Min(1d, p5.Value / p50.Value);
                        result.MedianSpeedMBs = p50.Value;
                    }
                }
                result.Stability = stability;
                if (stability.HasValue)
                    indicators.Add(HealthIndicator.CreateHigherBetter("Stabilita rychlosti (p5/medián)", $"{stability.Value:0.00}",
                        stability.Value, 0.30d, 0.15d, 0.60d, "≥ 0,30 | ≥ 0,15 | nižší | –",
                        $"Kolísání rychlosti: p5/medián {stability.Value:0.00}"));
                else
                    indicators.Add(HealthIndicator.CreateNotEvaluated("Stabilita rychlosti (p5/medián)", "málo dat", "≥ 0,30 | ≥ 0,15 | nižší | –"));

                result.Indicators = indicators.ToArray();

                // Verdikt = nejhorší ukazatel; při shodě úrovně ten, který je nejblíže horší úrovni:
                var worst = indicators
                    .Where(i => i.Level.HasValue)
                    .OrderBy(i => (int)i.Level.Value)
                    .ThenBy(i => i.Position)
                    .First();
                result.Level = worst.Level.Value;
                result.Position = ((int)worst.Level.Value + worst.Position) / 4d;
                result.Reason = (result.Level == HealthLevel.Excellent ? "Bez chyb, výpadků a odpojení" : worst.ReasonText);
                return result;
            }
        }
        /// <summary>
        /// Vrátí text důvodu pro odpojení a zamrznutí
        /// </summary>
        private static string GetDeviceEventsText(int deviceLosts, int hangs)
        {
            var parts = new List<string>();
            if (deviceLosts > 0) parts.Add($"{deviceLosts}× odpojení karty");
            if (hangs > 0) parts.Add($"{hangs}× zamrznutí karty");
            return (parts.Count > 0 ? String.Join(", ", parts) : "Bez odpojení");
        }
        #endregion
        #region Formátování
        /// <summary>
        /// Vrátí podíl (0 ÷ 1) jako procenta s přesností přiměřenou velikosti
        /// </summary>
        public static string FormatPercent(double fraction)
        {
            double percent = 100d * fraction;
            if (percent <= 0d) return "0 %";
            if (percent < 0.001d) return "< 0,001 %";
            if (percent < 0.1d) return $"{percent:0.###} %";
            if (percent < 10d) return $"{percent:0.##} %";
            return $"{percent:0.#} %";
        }
        /// <summary>
        /// Vrátí čas v sekundách s přiměřenou přesností
        /// </summary>
        public static string FormatSeconds(double seconds)
        {
            if (seconds < 0.01d) return $"{1000d * seconds:0.##} ms";
            return (seconds < 1d ? $"{seconds:0.0##} s" : $"{seconds:0.0} s");
        }
        /// <summary>
        /// Vrátí text verdiktu pro danou úroveň
        /// </summary>
        public static string GetLevelText(HealthLevel level)
        {
            switch (level)
            {
                case HealthLevel.Discard: return "Zcela vyhodit";
                case HealthLevel.Risk: return "Používat s rizikem";
                case HealthLevel.Ok: return "Funguje v pořádku";
                case HealthLevel.Excellent: return "Excelentní stav";
            }
            return "";
        }
        /// <summary>
        /// Vrátí krátký text úrovně (popisek pásma teploměru)
        /// </summary>
        public static string GetLevelShortText(HealthLevel level)
        {
            switch (level)
            {
                case HealthLevel.Discard: return "Vyhodit";
                case HealthLevel.Risk: return "S rizikem";
                case HealthLevel.Ok: return "V pořádku";
                case HealthLevel.Excellent: return "Excelentní";
            }
            return "";
        }
        /// <summary>
        /// Vrátí barvu pásma dané úrovně (barvy bloků mapy, aby odpovídaly aktuální paletě)
        /// </summary>
        public static Color GetLevelColor(HealthLevel level)
        {
            switch (level)
            {
                case HealthLevel.Discard: return Skin.RescueBlockAbortedColor;
                case HealthLevel.Risk: return Skin.RescueBlockError2Color;
                case HealthLevel.Ok: return Skin.RescueBlockOkAfterErrorsColor;
                case HealthLevel.Excellent: return Skin.RescueBlockOkColor;
            }
            return Skin.RescueBlockReadingColor;
        }
        #endregion
        #region Uložení výsledku do souboru
        /// <summary>
        /// Uloží výsledek do textového souboru "ZdraviMedia yyyy-MM-dd HH-mm-ss.txt" v daném adresáři. Vrátí plné jméno souboru.
        /// Výjimku nezachytává.
        /// </summary>
        /// <param name="directory">Cílový adresář</param>
        /// <param name="sourceText">Zadání zdroje (vstupní soubory)</param>
        /// <param name="result">Výsledek</param>
        /// <param name="elapsedSeconds">Čas běhu bez pauz</param>
        /// <param name="runEndText">Způsob ukončení běhu (Dokončeno, Stop, ...)</param>
        /// <returns></returns>
        public static string SaveReport(string directory, string sourceText, HealthResult result, decimal elapsedSeconds, string runEndText)
        {
            var now = DateTime.Now;
            string fileName = System.IO.Path.Combine(directory, $"ZdraviMedia {now:yyyy-MM-dd HH-mm-ss}.txt");
            var text = new StringBuilder();
            text.AppendLine("Teploměr zdraví média (FileRescue)");
            text.AppendLine("==================================");
            text.AppendLine($"Datum:      {now:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Zdroj:      {sourceText}");
            text.AppendLine($"Cíl:        {directory}");
            text.AppendLine($"Čas běhu:   {FileRescueSummaryControl.FormatTime(elapsedSeconds)} (bez pauz)");
            text.AppendLine($"Ukončení:   {runEndText}");
            text.AppendLine();
            text.AppendLine($"VERDIKT:    {result.GetVerdictText()}");
            text.AppendLine($"Důvod:      {result.Reason}");
            text.AppendLine();
            text.Append(result.GetDetailText(true));
            System.IO.File.WriteAllText(fileName, text.ToString(), Encoding.UTF8);
            return fileName;
        }
        #endregion
    }
    /// <summary>
    /// Úroveň zdraví média = pásmo teploměru (číselná hodnota = pořadí zleva)
    /// </summary>
    internal enum HealthLevel
    {
        /// <summary>Zcela vyhodit</summary>
        Discard = 0,
        /// <summary>Používat s rizikem</summary>
        Risk = 1,
        /// <summary>Funguje v pořádku</summary>
        Ok = 2,
        /// <summary>Excelentní stav</summary>
        Excellent = 3
    }
    /// <summary>
    /// Výsledek vyhodnocení zdraví média, viz <see cref="MediaHealth.GetResult(bool, bool)"/>
    /// </summary>
    internal class HealthResult
    {
        /// <summary>Jsou nějaká data (zpracovaný blok nebo odpojení / zamrznutí)</summary>
        public bool HasData { get; set; }
        /// <summary>Přečteno méně než <see cref="MediaHealth.MinDataBytes"/>: hodnocení je předběžné</summary>
        public bool IsPreliminary { get; set; }
        /// <summary>Verdikt = úroveň nejhoršího ukazatele</summary>
        public HealthLevel Level { get; set; }
        /// <summary>Pozice ukazatele na celé stupnici 0 (vlevo, nejhorší) ÷ 1 (vpravo, nejlepší)</summary>
        public double Position { get; set; }
        /// <summary>Hlavní důvod verdiktu</summary>
        public string Reason { get; set; }
        /// <summary>Jednotlivé ukazatele</summary>
        public HealthIndicator[] Indicators { get; set; }
        /// <summary>Počet různých bloků zpracovaných v tomto běhu</summary>
        public long TotalBlocks { get; set; }
        /// <summary>Počet bloků, které se v tomto běhu nepodařilo přečíst (vzdané nebo dosud chybné)</summary>
        public long UnreadableBlocks { get; set; }
        /// <summary>Počet bloků přečtených až po předchozí chybě</summary>
        public long RecoveredBlocks { get; set; }
        /// <summary>Počet chybných pokusů o čtení</summary>
        public long ReadErrors { get; set; }
        /// <summary>Úspěšně přečtené bajty</summary>
        public long BytesRead { get; set; }
        /// <summary>Počet zamrznutí zdroje</summary>
        public int Hangs { get; set; }
        /// <summary>Počet odpojení zdroje (mimo zamrznutí)</summary>
        public int DeviceLosts { get; set; }
        /// <summary>Medián doby čtení bloku; null = málo dat</summary>
        public double? MedianReadSeconds { get; set; }
        /// <summary>Medián rychlosti okna MB/s; null = málo dat</summary>
        public double? MedianSpeedMBs { get; set; }
        /// <summary>Stabilita rychlosti p5/medián; null = málo dat</summary>
        public double? Stability { get; set; }
        /// <summary>Použitý práh pomalého čtení (s)</summary>
        public double SlowThresholdSeconds { get; set; }
        /// <summary>Použitý práh výpadku čtení (s)</summary>
        public double StallThresholdSeconds { get; set; }
        /// <summary>
        /// Má se zobrazit ukazatel a verdikt? Ne, pokud nejsou data, nebo je jich málo a výsledek není horší než "V pořádku".
        /// </summary>
        public bool ShowVerdict { get { return HasData && (!IsPreliminary || Level <= HealthLevel.Risk); } }
        /// <summary>
        /// Vrátí text verdiktu včetně příznaku předběžnosti / nedostatku dat
        /// </summary>
        public string GetVerdictText()
        {
            if (!HasData) return "Zatím bez dat";
            if (!ShowVerdict) return $"Zatím málo dat ({FileRescueSummaryControl.FormatSize(BytesRead)} z {FileRescueSummaryControl.FormatSize(MediaHealth.MinDataBytes)})";
            return MediaHealth.GetLevelText(Level) + (IsPreliminary ? " (předběžně)" : "");
        }
        /// <summary>
        /// Vrátí rozpis ukazatelů (ToolTip, soubor s výsledkem)
        /// </summary>
        /// <param name="withThresholds">Včetně prahů jednotlivých pásem</param>
        /// <returns></returns>
        public string GetDetailText(bool withThresholds)
        {
            var text = new StringBuilder();
            text.AppendLine($"Přečteno: {FileRescueSummaryControl.FormatSize(BytesRead)}, bloků: {TotalBlocks:N0}, chybných pokusů o čtení: {ReadErrors:N0}");
            if (MedianReadSeconds.HasValue || MedianSpeedMBs.HasValue)
                text.AppendLine($"Medián: čtení bloku {(MedianReadSeconds.HasValue ? MediaHealth.FormatSeconds(MedianReadSeconds.Value) : "-")}, rychlost {(MedianSpeedMBs.HasValue ? FileRescue.SpeedShading.FormatSpeed(MedianSpeedMBs.Value) + " MB/s" : "-")}");
            text.AppendLine();
            if (Indicators != null)
            {
                foreach (var indicator in Indicators)
                {
                    string level = (indicator.Level.HasValue ? MediaHealth.GetLevelShortText(indicator.Level.Value) : "nehodnoceno");
                    text.AppendLine($"{indicator.Name}: {indicator.ValueText}  =>  {level}");
                    if (withThresholds)
                        text.AppendLine($"      (Excelentní | V pořádku | S rizikem | Vyhodit:  {indicator.ThresholdsText})");
                }
            }
            text.AppendLine();
            text.AppendLine("Verdikt = nejhorší ukazatel. Hodnotí se jen aktuální běh.");
            if (SlowThresholdSeconds > MediaHealth.SlowReadSeconds)
                text.AppendLine($"Práh pomalého čtení zvýšen na {MediaHealth.SlowReadMedianFactor:0}× medián (pomalá čtečka nebo port).");
            text.AppendLine("Tichý převrat bitů (data přečtena bez chyby, ale jiná) tato analýza nepozná – k tomu slouží test zápisu a čtení.");
            return text.ToString();
        }
    }
    /// <summary>
    /// Jeden ukazatel zdraví média
    /// </summary>
    internal class HealthIndicator
    {
        /// <summary>Název ukazatele</summary>
        public string Name { get; private set; }
        /// <summary>Hodnota jako text</summary>
        public string ValueText { get; private set; }
        /// <summary>Úroveň; null = nehodnoceno (málo dat)</summary>
        public HealthLevel? Level { get; private set; }
        /// <summary>Pozice uvnitř pásma úrovně: 0 = na hranici horšího pásma, 1 = na hranici lepšího pásma (nebo ideál)</summary>
        public double Position { get; private set; }
        /// <summary>Prahy pásem jako text</summary>
        public string ThresholdsText { get; private set; }
        /// <summary>Text důvodu, pokud tento ukazatel určí verdikt</summary>
        public string ReasonText { get; private set; }
        /// <summary>
        /// Vytvoří ukazatel, u něhož je menší hodnota lepší. Excelentní: hodnota ≤ <paramref name="excellentMax"/>; V pořádku: ≤ <paramref name="okMax"/>; S rizikem: ≤ <paramref name="riskMax"/>; jinak Vyhodit.
        /// </summary>
        public static HealthIndicator CreateLowerBetter(string name, string valueText, double value, double excellentMax, double okMax, double riskMax, string thresholdsText, string reasonText)
        {
            HealthLevel level;
            double position;
            if (value <= excellentMax)
            {
                level = HealthLevel.Excellent;
                position = (excellentMax > 0d ? 1d - value / excellentMax : 1d);
            }
            else if (value <= okMax)
            {
                level = HealthLevel.Ok;
                position = 1d - (value - excellentMax) / (okMax - excellentMax);
            }
            else if (value <= riskMax)
            {
                level = HealthLevel.Risk;
                position = 1d - (value - okMax) / (riskMax - okMax);
            }
            else
            {
                level = HealthLevel.Discard;
                position = (riskMax > 0d ? riskMax / value : 0d);           // Na hranici 1, s rostoucí hodnotou k 0
            }
            return new HealthIndicator() { Name = name, ValueText = valueText, Level = level, Position = _Clamp(position), ThresholdsText = thresholdsText, ReasonText = reasonText };
        }
        /// <summary>
        /// Vytvoří ukazatel, u něhož je větší hodnota lepší. Excelentní: hodnota ≥ <paramref name="excellentMin"/>; V pořádku: ≥ <paramref name="okMin"/>; jinak S rizikem (Vyhodit nenastává).
        /// </summary>
        /// <param name="idealValue">Hodnota, při níž je ukazatel na pravém konci pásma Excelentní</param>
        public static HealthIndicator CreateHigherBetter(string name, string valueText, double value, double excellentMin, double okMin, double idealValue, string thresholdsText, string reasonText)
        {
            HealthLevel level;
            double position;
            if (value >= excellentMin)
            {
                level = HealthLevel.Excellent;
                position = (idealValue > excellentMin ? (value - excellentMin) / (idealValue - excellentMin) : 1d);
            }
            else if (value >= okMin)
            {
                level = HealthLevel.Ok;
                position = (value - okMin) / (excellentMin - okMin);
            }
            else
            {
                level = HealthLevel.Risk;
                position = (okMin > 0d ? value / okMin : 0d);
            }
            return new HealthIndicator() { Name = name, ValueText = valueText, Level = level, Position = _Clamp(position), ThresholdsText = thresholdsText, ReasonText = reasonText };
        }
        /// <summary>
        /// Vytvoří nehodnocený ukazatel (málo dat)
        /// </summary>
        public static HealthIndicator CreateNotEvaluated(string name, string valueText, string thresholdsText)
        {
            return new HealthIndicator() { Name = name, ValueText = valueText, Level = null, Position = 1d, ThresholdsText = thresholdsText, ReasonText = null };
        }
        private static double _Clamp(double value) { return (value < 0d ? 0d : (value > 1d ? 1d : value)); }
    }
    #endregion
    #region MediaHealthControl : Teploměr zdraví v levém panelu výsledků
    /// <summary>
    /// Summary blok FileRescue, část C: Teploměr zdraví zdrojového média.
    /// Vodorovný pruh se 4 pásmy (Vyhodit – S rizikem – V pořádku – Excelentní), ukazatel, verdikt a hlavní důvod; rozpis ukazatelů v ToolTipu.
    /// Data bere z <see cref="FileRescue.SummaryInfo.Health"/>.
    /// </summary>
    internal class MediaHealthControl : FileRescueSummaryControl
    {
        protected override void PaintContent(Graphics graphics, Rectangle bounds)
        {
            var health = this.Summary?.Health;
            int y = bounds.Y;
            int lineHeight = LineHeight;
            bool showVerdict = (health != null && health.ShowVerdict);

            // 1. Titulek a verdikt:
            using (var boldFont = new Font(this.Font, FontStyle.Bold))
            {
                var row = GetRow(bounds, y);
                string title = "Zdraví média:";
                PaintRow(graphics, row, title, null, boldFont);
                int titleWidth = TextRenderer.MeasureText(graphics, title, boldFont).Width + 4;
                var verdictRow = new Rectangle(row.X + titleWidth, row.Y, row.Width - titleWidth, row.Height);
                // Krátký verdikt (šířka panelu); množství dat a předběžnost jsou na řádku důvodu:
                string verdict = (health is null || !health.HasData ? "Zatím bez dat" : (!showVerdict ? "Zatím málo dat" : MediaHealth.GetLevelText(health.Level)));
                TextRenderer.DrawText(graphics, verdict, (showVerdict ? boldFont : this.Font), verdictRow, this.ForeColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
            y += lineHeight;

            // 2. Pruh se 4 pásmy; bez verdiktu bledé:
            var barBounds = new Rectangle(bounds.X + MarkerHalfWidth, y + MarkerHeight, bounds.Width - 2 * MarkerHalfWidth, lineHeight + 2);
            for (int i = 0; i < 4; i++)
            {
                var level = (HealthLevel)i;
                int x0 = barBounds.X + (int)Math.Round((double)barBounds.Width * i / 4d, 0);
                int x1 = barBounds.X + (int)Math.Round((double)barBounds.Width * (i + 1) / 4d, 0);
                var zoneBounds = new Rectangle(x0, barBounds.Y, x1 - x0, barBounds.Height);
                var color = MediaHealth.GetLevelColor(level);
                if (!showVerdict) color = _Blend(color, Skin.BackgroundColor, 0.65d);
                Painter.PaintBar3D(graphics, color, zoneBounds);
                using (var zoneFont = new Font(this.Font.FontFamily, this.Font.Size - 0.75f))
                    TextRenderer.DrawText(graphics, MediaHealth.GetLevelShortText(level), zoneFont, zoneBounds, _GetTextColor(color),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            }
            graphics.DrawRectangle(Painter.GetPen(Painter.GetColor(this.BackColor, -64, -64, -64)), barBounds.X, barBounds.Y, barBounds.Width - 1, barBounds.Height - 1);

            // 3. Ukazatel: trojúhelník nad pruhem a svislá čára přes pruh:
            if (showVerdict)
            {
                int x = barBounds.X + (int)Math.Round(health.Position * (barBounds.Width - 1), 0);
                var markerColor = Color.FromArgb(32, 32, 32);
                var oldMode = graphics.SmoothingMode;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var points = new Point[] { new Point(x - MarkerHalfWidth, y), new Point(x + MarkerHalfWidth, y), new Point(x, y + MarkerHeight + 1) };
                graphics.FillPolygon(Painter.GetSolidBrush(markerColor), points);
                graphics.SmoothingMode = oldMode;
                graphics.FillRectangle(Painter.GetSolidBrush(markerColor), new Rectangle(x - 1, barBounds.Y, 3, barBounds.Height));
                graphics.FillRectangle(Painter.GetSolidBrush(Color.White), new Rectangle(x, barBounds.Y + 1, 1, barBounds.Height - 2));
            }
            y = barBounds.Bottom + 3;

            // 4. Hlavní důvod:
            string reasonText = null;
            if (showVerdict)
                reasonText = (health.IsPreliminary ? "Předběžně: " : "") + health.Reason;
            else if (health != null && health.HasData && health.TotalBlocks > 0)
                reasonText = $"Přečteno {FormatSize(health.BytesRead)}, verdikt po {FormatSize(MediaHealth.MinDataBytes)}";
            else if (health == null || !health.HasData)
                reasonText = "Hodnotí se čtení v tomto běhu";
            if (!String.IsNullOrEmpty(reasonText))
                TextRenderer.DrawText(graphics, reasonText, this.Font, GetRow(bounds, y), this.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
        /// <summary>
        /// Smíchá barvu s pozadím; <paramref name="ratio"/> = podíl pozadí
        /// </summary>
        private static Color _Blend(Color color, Color background, double ratio)
        {
            int mix(int a, int b) { return (int)Math.Round(a * (1d - ratio) + b * ratio, 0); }
            return Color.FromArgb(mix(color.R, background.R), mix(color.G, background.G), mix(color.B, background.B));
        }
        /// <summary>
        /// Vrátí čitelnou barvu textu (černá / bílá) na dané barvě pozadí
        /// </summary>
        private static Color _GetTextColor(Color backColor)
        {
            double luminance = 0.299d * backColor.R + 0.587d * backColor.G + 0.114d * backColor.B;
            return (luminance < 140d ? Color.White : Color.Black);
        }
        protected override string GetToolTipText()
        {
            var health = this.Summary?.Health;
            if (health is null || !health.HasData) return "Teploměr zdraví média: hodnotí čtení bloků v tomto běhu (chyby, odpojení, výpadky a stabilitu rychlosti).";
            return $"Teploměr zdraví média: {health.GetVerdictText()}\r\n{(health.ShowVerdict ? "Důvod: " + health.Reason + "\r\n" : "")}\r\n{health.GetDetailText(false)}";
        }
        /// <summary>Výška trojúhelníku ukazatele nad pruhem</summary>
        private const int MarkerHeight = 6;
        /// <summary>Poloviční šířka trojúhelníku ukazatele (o tolik je pruh odsazen od okrajů)</summary>
        private const int MarkerHalfWidth = 5;
        protected override int CurrentOptimalHeight { get { return OptimalHeight; } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        public static int OptimalHeight { get { return FrameHeight + 3 * LineHeight + MarkerHeight + 2 + 3 + 4; } }
    }
    #endregion
}
