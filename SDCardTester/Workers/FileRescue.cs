using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using static DjSoft.Tools.SDCardTester.Workers.FileRescue;

namespace DjSoft.Tools.SDCardTester.Workers
{
    internal class FileRescue : BackWorker<FileRescueInputInfo>
    {
        #region Konstrukce a public rozhraní
        /// <summary>
        /// Konstruktor
        /// </summary>
        public FileRescue()
        {
            InitStopwatch();
        }
        protected override bool TargetIsReady(FileRescueInputInfo target)
        {
            return (target != null);
        }
        /// <summary>
        /// Požádá o provedení analýzy daného disku
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="doRead"></param>
        /// <param name="doSave"></param>
        public void Start(FileRescueInputInfo inputInfo)
        {
            StartAction(inputInfo);
        }
        #endregion
        #region Data pro vnější svět, platná v době práce
        /// <summary>
        /// Plné jméno vstupního souboru.
        /// </summary>
        public string CurrentFileSource { get { return __CurrentFile.SourceFile; } }
        /// <summary>
        /// Velikost vstupního souboru. Null když neexistuje.
        /// </summary>
        public long? CurrentFileLength { get { return __CurrentFile.SourceFileLength; } }
        /// <summary>
        /// Plné jméno vstupního souboru.
        /// </summary>
        public string CurrentFileTarget { get { return __CurrentFile.DestinationFile; } }
        /// <summary>
        /// Stav práce na souboru.
        /// </summary>
        public FileStatus CurrentFileStatus { get { return __CurrentFile.SourceFileStatus; } }
        /// <summary>
        /// Aktuální bloky zkopírované
        /// </summary>
        public SingleBlockInfo[] CurrentBlocks { get { return __CurrentLog?.CurrentBlocks; } }
        /// <summary>
        /// Vrátí konzistentní data pro zobrazení progresu: délku souboru a jeho bloky, vždy z téhož souboru.
        /// <para/>
        /// Volá se z GUI threadu, zatímco thread Work může mezitím přejít na další soubor.
        /// Proto se <see cref="__CurrentFile"/> čte jen jednou a bloky se berou z logu téhož souboru (ne z <see cref="__CurrentLog"/>, který se nastavuje později).
        /// Bloky jsou null, dokud pro soubor není načten log.
        /// </summary>
        /// <param name="fileLength"></param>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public FileStatus GetCurrentProgress(out long fileLength, out SingleBlockInfo[] blocks)
        {
            var currentFile = __CurrentFile;
            fileLength = currentFile?.SourceFileLength ?? 0L;
            blocks = currentFile?.CurrentLog?.CurrentBlocks;
            return currentFile?.SourceFileStatus ?? FileStatus.None;
        }
        /// <summary>
        /// Vrátí konzistentní snímek dat pro Summary blok: postup přes všechny soubory a statistiku aktuálního souboru.
        /// <para/>
        /// Volá se z GUI threadu. Seznam <see cref="__Files"/> se po přípravě nemění; <see cref="__CurrentFile"/> se čte jen jednou.
        /// </summary>
        /// <returns></returns>
        public SummaryInfo GetSummary()
        {
            var files = __Files;
            var currentFile = __CurrentFile;
            var runStartTime = __RunStartTime;
            var runTotalSeconds = __RunTotalSeconds;

            var summary = new SummaryInfo();
            summary.IsFinished = runTotalSeconds.HasValue;
            summary.SourceDeviceLost = __SourceDeviceLost;
            summary.SourceHung = __SourceHung;
            summary.SourceNotRespondingSeconds = (runTotalSeconds.HasValue ? null : _GetSourceNotRespondingSeconds());
            summary.IsSecondWave = !__DeferEnabled && __RunStartTime.HasValue;
            summary.ElapsedSeconds = (runTotalSeconds.HasValue ? runTotalSeconds.Value : (runStartTime.HasValue ? this.GetSeconds(runStartTime.Value) : 0m));
            summary.Health = __Health.GetResult(summary.SourceDeviceLost, summary.SourceNotRespondingSeconds.HasValue);

            if (files != null)
            {
                summary.FilesCount = files.Count;
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    bool isCurrent = Object.ReferenceEquals(file, currentFile);
                    var statistic = file.FinalStatistic;
                    if (isCurrent)
                    {
                        summary.CurrentFileIndex = i;
                        if (statistic is null) statistic = file.CurrentLog?.Statistic;
                        summary.CurrentStatistic = statistic;
                    }

                    summary.TotalBytes += file.SourceFileLength ?? 0L;
                    if (statistic != null)
                        summary.ProcessedBytes += statistic.TotalBlocksSize;

                    summary.AddFileResult(GetFileResult(file));
                }
            }

            if (currentFile != null)
            {
                summary.CurrentFileName = currentFile.SourceFile;
                summary.CurrentFileTarget = currentFile.DestinationFile;
                summary.CurrentFileLength = currentFile.SourceFileLength ?? 0L;
                summary.CurrentFileStatus = currentFile.SourceFileStatus;
                summary.CurrentFileDeferred = currentFile.IsDeferred;
                summary.CurrentErrorLevel = currentFile.CurrentLog?.ErrorLevelProcess ?? 0;
            }

            return summary;
        }
        /// <summary>
        /// Vrátí výsledek zpracování daného souboru
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static FileResult GetFileResult(SingleFileInfo file)
        {
            if (!String.IsNullOrEmpty(file.ErrorMessage)) return FileResult.Failed;
            if (file.SourceFileStatus == FileStatus.NotExists) return FileResult.NotExists;
            if (file.IsDeferred) return FileResult.Deferred;
            var statistic = file.FinalStatistic;
            if (statistic is null) return FileResult.Pending;
            if (statistic.BadBlocksCount > 0) return FileResult.WithErrors;
            if (statistic.TotalBlocksSize < statistic.FileSize) return FileResult.Interrupted;
            return FileResult.Ok;
        }
        /// <summary>
        /// Výsledek zpracování jednoho souboru
        /// </summary>
        public enum FileResult
        {
            /// <summary>Dosud nezpracován (nebo se právě zpracovává)</summary>
            Pending,
            /// <summary>Kompletně zachráněn bez chyb</summary>
            Ok,
            /// <summary>Zpracován, obsahuje nečitelné bloky</summary>
            WithErrors,
            /// <summary>Zpracování bylo přerušeno (Stop), soubor není kompletní</summary>
            Interrupted,
            /// <summary>Zdrojový soubor neexistuje</summary>
            NotExists,
            /// <summary>Chyba při zpracování (výjimka)</summary>
            Failed,
            /// <summary>Odložen pro vysokou chybovost čtení v 1. vlně (nečitelný soubor), viz <see cref="DeferWindowBlocks"/></summary>
            Deferred
        }
        /// <summary>
        /// Snímek dat pro Summary blok, viz <see cref="GetSummary"/>
        /// </summary>
        public class SummaryInfo
        {
            /// <summary>Akce je ukončena</summary>
            public bool IsFinished { get; set; }
            /// <summary>Čas práce celé akce v sekundách (bez pauz)</summary>
            public decimal ElapsedSeconds { get; set; }
            /// <summary>Počet souborů k záchraně</summary>
            public int FilesCount { get; set; }
            /// <summary>Index aktuálního souboru v seznamu; -1 = dosud žádný</summary>
            public int CurrentFileIndex { get; set; } = -1;
            /// <summary>Součet délek všech existujících souborů</summary>
            public long TotalBytes { get; set; }
            /// <summary>Součet zpracovaných bloků všech souborů (dokončených i aktuálního)</summary>
            public long ProcessedBytes { get; set; }
            /// <summary>Podíl <see cref="ProcessedBytes"/> / <see cref="TotalBytes"/> v rozsahu 0 ÷ 1</summary>
            public double ProcessedRatio { get { return (TotalBytes > 0L ? Math.Min(1d, (double)ProcessedBytes / (double)TotalBytes) : 0d); } }
            /// <summary>Počet souborů zachráněných bez chyb</summary>
            public int FilesOk { get; private set; }
            /// <summary>Počet souborů s nečitelnými bloky</summary>
            public int FilesWithErrors { get; private set; }
            /// <summary>Počet souborů, jejichž zpracování bylo přerušeno</summary>
            public int FilesInterrupted { get; private set; }
            /// <summary>Počet neexistujících souborů</summary>
            public int FilesNotExists { get; private set; }
            /// <summary>Počet souborů, při jejichž zpracování došlo k chybě</summary>
            public int FilesFailed { get; private set; }
            /// <summary>Počet nečitelných souborů = odložených pro vysokou chybovost čtení</summary>
            public int FilesDeferred { get; private set; }
            /// <summary>
            /// Započítá výsledek jednoho souboru
            /// </summary>
            /// <param name="result"></param>
            public void AddFileResult(FileResult result)
            {
                switch (result)
                {
                    case FileResult.Ok: FilesOk++; break;
                    case FileResult.WithErrors: FilesWithErrors++; break;
                    case FileResult.Interrupted: FilesInterrupted++; break;
                    case FileResult.NotExists: FilesNotExists++; break;
                    case FileResult.Failed: FilesFailed++; break;
                    case FileResult.Deferred: FilesDeferred++; break;
                }
            }
            /// <summary>Plné jméno aktuálního zdrojového souboru</summary>
            public string CurrentFileName { get; set; }
            /// <summary>Plné jméno aktuálního cílového souboru</summary>
            public string CurrentFileTarget { get; set; }
            /// <summary>Délka aktuálního souboru</summary>
            public long CurrentFileLength { get; set; }
            /// <summary>Stav práce na aktuálním souboru</summary>
            public FileStatus CurrentFileStatus { get; set; }
            /// <summary>Úroveň chyb, které se v aktuálním souboru zpracovávají: 0 = první průchod, 1 a více = opakované čtení</summary>
            public int CurrentErrorLevel { get; set; }
            /// <summary>Statistika aktuálního souboru; null když dosud není</summary>
            public StatisticInfo CurrentStatistic { get; set; }
            /// <summary>Zdrojové zařízení se odpojilo (nebo přestalo odpovídat) a běh byl proto ukončen</summary>
            public bool SourceDeviceLost { get; set; }
            /// <summary>Zdrojové zařízení zamrzlo (operace opuštěna); zároveň je nastaveno <see cref="SourceDeviceLost"/></summary>
            public bool SourceHung { get; set; }
            /// <summary>Právě probíhající operace se zdrojem visí déle než <see cref="SourceHangWarningSeconds"/>: počet sekund; jinak null</summary>
            public double? SourceNotRespondingSeconds { get; set; }
            /// <summary>Aktuální soubor byl odložen pro vysokou chybovost čtení</summary>
            public bool CurrentFileDeferred { get; set; }
            /// <summary>Probíhá 2. vlna = opakovaný pokus o čtení odložených souborů</summary>
            public bool IsSecondWave { get; set; }
            /// <summary>Teploměr zdraví zdrojového média (čtení v tomto běhu)</summary>
            public HealthResult Health { get; set; }
        }
        #endregion
        #region Privátní řízení běhu
        /// <summary>
        /// Zahájení testu, zde již v threadu Working
        /// </summary>
        protected override void Run()
        {
            if (Stopping) return;

            __RunStartTime = this.CurrentTime;
            __RunTotalSeconds = null;
            __SourceDeviceLost = false;
            __SourceHung = false;
            __AbandonedSourceIo = null;
            Interlocked.Exchange(ref __SourceIoStartTicks, 0L);
            __ConsecutiveReadErrors = 0;
            __NextDeviceCheckAt = ConsecutiveErrorsCheckCount;
            __DeferEnabled = true;
            __AbortedByUser = false;
            __Health.Reset();
            _PreventSystemSleep(true);
            _PrepareWrittingThread();
            try
            {
                // Cílový adresář: pokud neexistuje, vytvoříme jej; nejde-li to, nemá smysl pokračovat (chyba by se opakovala u každého souboru):
                if (!_PrepareOutputDirectory()) return;                       // finally: ukončí Writter, CallWorkingDone()

                // Příprava seznamu musí být uvnitř try: výjimka mimo něj by ukončila Task bez CallWorkingDone() => GUI by zůstalo ve stavu Working.
                //  Prochází adresáře zdroje => zamrzlá karta by zablokovala i Stop, proto s hlídáním:
                try
                {
                    _SourceIo(() => { _PrepareFileList(); return true; });
                }
                catch (SourceHungException)
                {
                    __SourceDeviceLost = true;
                    return;                                                    // finally: ukončí Writter, CallWorkingDone(), oznámí zamrzlý zdroj
                }

                // 1. vlna: všechny soubory; nečitelné soubory (vysoká chybovost v 1. průchodu) se odkládají:
                _RunFiles(__Files);

                // 2. vlna: odložené soubory, jen pokud je uživatel potvrdí (ne po Stop, ne po odpojení zdroje):
                var deferredFiles = __Files.Where(f => f.IsDeferred).ToList();
                if (deferredFiles.Count > 0 && !IsStoped && !__SourceDeviceLost && !__AbortedByUser && _AskForSecondWave(deferredFiles))
                {
                    __DeferEnabled = false;                                    // Ve 2. vlně se soubor už neodkládá: plné zpracování včetně opakování chybných bloků
                    foreach (var file in deferredFiles)
                    {
                        file.IsDeferred = false;
                        file.FinalStatistic = null;
                    }
                    _RunFiles(deferredFiles);
                }
            }
            catch (Exception exc)
            {
                App.ShowError(exc.ToString(), "Chyba při záchraně souborů");
            }
            finally
            {
                _PreventSystemSleep(false);                                        // Ve stejném vlákně jako zákaz (nastavení platí pro vlákno); před případnými dialogy níže
                __RunTotalSeconds = this.GetSeconds(__RunStartTime ?? 0L);         // Zafixuje celkový čas: Stopwatch po skončení dál běží
                _SaveHealthReport();
                _WrittingThreadAddRequest(new WritterRequestInfo(WritterRequestType.EndThread));
                CallWorkingDone();
                if (__SourceDeviceLost)
                    _ShowSourceDeviceLost();
            }
        }
        /// <summary>
        /// Zpracuje dané soubory v daném pořadí (jedna vlna). Skončí při Stop nebo ztrátě zdrojového zařízení.
        /// </summary>
        /// <param name="files"></param>
        private void _RunFiles(IEnumerable<SingleFileInfo> files)
        {
            var fileList = files.ToList();
            for (int i = 0; i < fileList.Count; i++)
            {
                var fileInfo = fileList[i];
                try
                {
                    _RunSingleFile(fileInfo);
                }
                catch (Exception exc)
                {
                    fileInfo.ErrorMessage = exc.ToString();
                    // Pokud následuje další soubor, zeptáme se, zda pokračovat (stejná chyba se typicky opakuje u všech souborů);
                    //  u posledního souboru jen oznámíme chybu. App.Show* se volají synchronně v GUI threadu (Invoke).
                    bool hasNextFile = (i < fileList.Count - 1) && !IsStoped && !__SourceDeviceLost;
                    if (hasNextFile)
                    {
                        if (!_AskContinueAfterFileError(fileInfo, exc))
                        {
                            __AbortedByUser = true;
                            break;
                        }
                    }
                    else
                        App.ShowError($"Při zpracování souboru došlo k chybě:\r\n{fileInfo.SourceFile}\r\n\r\n{exc.Message}", "Chyba při zpracování souboru");
                }
                if (Stopping) break;
                if (__SourceDeviceLost) break;                                 // Zdroj odpojen: další soubory jsou typicky na témže zařízení => končíme celý běh
            }
        }
        /// <summary>
        /// Oznámí chybu při zpracování souboru a zeptá se, zda pokračovat dalším souborem. Vrátí true = Ano.
        /// Čas čekání na odpověď se nepočítá do času akce (časomíra se zastaví jako při Pauze).
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="exc"></param>
        /// <returns></returns>
        private bool _AskContinueAfterFileError(SingleFileInfo fileInfo, Exception exc)
        {
            string text = $"Při zpracování souboru došlo k chybě:\r\n{fileInfo.SourceFile}\r\n\r\n{exc.Message}\r\n\r\nPřejete si pokračovat dalším souborem?";
            bool isRunning = this.Stopwatch.IsRunning;
            if (isRunning) this.Stopwatch.Stop();
            try
            {
                return App.ShowQuestion(text, "Chyba při zpracování souboru");
            }
            finally
            {
                if (isRunning) this.Stopwatch.Start();
            }
        }
        /// <summary>
        /// Uživatel po chybě souboru odmítl pokračovat: běh končí (bez 2. vlny)
        /// </summary>
        private bool __AbortedByUser;
        /// <summary>
        /// Zakáže (<paramref name="prevent"/> = true) nebo opět povolí automatický přechod Windows do spánku po dobu záchrany.
        /// Spánek uprostřed čtení zkresluje časy a může odpojit USB čtečku. Monitor se vypnout smí; ruční uspání (nebo zavření víka) to nezablokuje.
        /// Nastavení platí pro volající vlákno: musí se volat v threadu Work na začátku i na konci <see cref="Run"/>.
        /// </summary>
        /// <param name="prevent"></param>
        private static void _PreventSystemSleep(bool prevent)
        {
            try
            {
                SetThreadExecutionState(prevent ? (ES_CONTINUOUS | ES_SYSTEM_REQUIRED) : ES_CONTINUOUS);
            }
            catch { }
        }
        /// <summary>
        /// Teploměr zdraví zdrojového média: sbírá data z čtení bloků v tomto běhu
        /// </summary>
        private readonly MediaHealth __Health = new MediaHealth();
        /// <summary>
        /// Po skončení běhu uloží výsledek Teploměru zdraví do textového souboru ve výstupním adresáři.
        /// Jen pokud v běhu proběhlo nějaké čtení nebo odpojení; chybu uložení ignoruje (výsledek je stále vidět v okně).
        /// </summary>
        private void _SaveHealthReport()
        {
            try
            {
                var result = __Health.GetResult(__SourceDeviceLost, false);
                if (!result.HasData) return;
                string runEndText = (__SourceHung ? "Přerušeno: zdroj neodpovídá" :
                                    (__SourceDeviceLost ? "Přerušeno: zdroj odpojen" :
                                    (IsStoped ? "Zastaveno uživatelem (Stop)" :
                                    (__AbortedByUser ? "Ukončeno po chybě souboru" : "Dokončeno"))));
                MediaHealth.SaveReport(_GetOutputPath(), this.Target?.InputFileNames, result, __RunTotalSeconds ?? 0m, runEndText);
            }
            catch { }
        }
        /// <summary>
        /// Vrátí cílový adresář záchrany ze zadání; pokud není zadán, pak adresář "FildeRescue" na ploše
        /// </summary>
        /// <returns></returns>
        private string _GetOutputPath()
        {
            var outputPath = (this.Target?.OutputFilesPath ?? "").Trim();
            if (String.IsNullOrEmpty(outputPath))
                outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FildeRescue");
            return outputPath;
        }
        /// <summary>
        /// Zajistí existenci cílového adresáře: pokud neexistuje, vytvoří jej (včetně nadřazených).
        /// Pokud jej vytvořit nelze, oznámí chybu a vrátí false = záchranu nelze provést.
        /// </summary>
        /// <returns></returns>
        private bool _PrepareOutputDirectory()
        {
            var outputPath = _GetOutputPath();
            try
            {
                if (!System.IO.Directory.Exists(outputPath))
                    System.IO.Directory.CreateDirectory(outputPath);
                return true;
            }
            catch (Exception exc)
            {
                App.ShowError($"Cílový adresář neexistuje a nepodařilo se jej vytvořit:\r\n{outputPath}\r\n\r\n{exc.Message}", "Chyba cílového adresáře");
                return false;
            }
        }
        #region Odkládání nečitelných souborů (vysoká chybovost v 1. průchodu)
        /// <summary>
        /// Velikost posuvného okna pro vyhodnocení chybovosti = počet posledních bloků přečtených v tomto běhu (256 × 128 kB = 32 MB).
        /// TODO: přesunout do konfigurace.
        /// </summary>
        private const int DeferWindowBlocks = 256;
        /// <summary>
        /// Chybovost se vyhodnocuje až po tomto počtu bloků přečtených v tomto běhu (64 × 128 kB = 8 MB)
        /// </summary>
        private const int DeferMinBlocks = 64;
        /// <summary>
        /// Soubor se odloží, pokud podíl chybných bloků v okně dosáhne této hodnoty (0.5 = 50 %)
        /// </summary>
        private const double DeferErrorRatio = 0.5d;
        /// <summary>
        /// Soubor se odloží, pokud po sobě následuje tento počet chybných bloků (128 × 128 kB = 16 MB bez jediného čitelného bloku)
        /// </summary>
        private const int DeferConsecutiveErrors = 128;
        /// <summary>
        /// Odkládání je povoleno = 1. vlna. Ve 2. vlně (opakovaný pokus o odložené soubory) se soubory neodkládají.
        /// </summary>
        private bool __DeferEnabled = true;
        /// <summary>
        /// Posuvné okno výsledků čtení aktuálního souboru (true = chyba), kruhový buffer
        /// </summary>
        private bool[] __DeferWindow = new bool[DeferWindowBlocks];
        /// <summary>Počet platných prvků v <see cref="__DeferWindow"/> (nejvýše <see cref="DeferWindowBlocks"/>)</summary>
        private int __DeferWindowCount;
        /// <summary>Pozice pro příští zápis do <see cref="__DeferWindow"/></summary>
        private int __DeferWindowPosition;
        /// <summary>Počet chyb v <see cref="__DeferWindow"/></summary>
        private int __DeferWindowErrors;
        /// <summary>Počet chybných bloků po sobě v aktuálním souboru (pro <see cref="DeferConsecutiveErrors"/>)</summary>
        private int __DeferConsecutiveErrors;
        /// <summary>
        /// Aktuální soubor má být odložen (zjištěno v <see cref="_DeferWindowAdd(bool)"/>), smyčka kopírování skončí
        /// </summary>
        private bool __CurrentFileDefer;
        /// <summary>
        /// Vynuluje vyhodnocení chybovosti pro nový soubor
        /// </summary>
        private void _DeferWindowReset()
        {
            Array.Clear(__DeferWindow, 0, __DeferWindow.Length);
            __DeferWindowCount = 0;
            __DeferWindowPosition = 0;
            __DeferWindowErrors = 0;
            __DeferConsecutiveErrors = 0;
            __CurrentFileDefer = false;
        }
        /// <summary>
        /// Započítá výsledek čtení jednoho bloku do vyhodnocení chybovosti a případně nastaví <see cref="__CurrentFileDefer"/>.
        /// Vyhodnocuje se jen v 1. vlně (<see cref="__DeferEnabled"/>) a v 1. průchodu souboru (<see cref="SingleLogInfo.ErrorLevelProcess"/> == 0).
        /// </summary>
        /// <param name="isError"></param>
        private void _DeferWindowAdd(bool isError)
        {
            if (!__DeferEnabled || (this.__CurrentLog?.ErrorLevelProcess ?? 0) > 0) return;

            // Kruhový buffer: vyjmeme nejstarší hodnotu (pokud je okno plné) a vložíme novou:
            if (__DeferWindowCount == DeferWindowBlocks && __DeferWindow[__DeferWindowPosition]) __DeferWindowErrors--;
            __DeferWindow[__DeferWindowPosition] = isError;
            if (isError) __DeferWindowErrors++;
            __DeferWindowPosition = (__DeferWindowPosition + 1) % DeferWindowBlocks;
            if (__DeferWindowCount < DeferWindowBlocks) __DeferWindowCount++;
            __DeferConsecutiveErrors = (isError ? __DeferConsecutiveErrors + 1 : 0);

            if (__DeferConsecutiveErrors >= DeferConsecutiveErrors ||
                (__DeferWindowCount >= DeferMinBlocks && __DeferWindowErrors >= DeferErrorRatio * __DeferWindowCount))
                __CurrentFileDefer = true;
        }
        /// <summary>
        /// Po 1. vlně zobrazí seznam odložených (nečitelných) souborů a zeptá se, zda je zkusit znovu. Vrátí true = Ano.
        /// Čas čekání na odpověď se nepočítá do času akce (časomíra se zastaví jako při Pauze).
        /// </summary>
        /// <param name="deferredFiles"></param>
        /// <returns></returns>
        private bool _AskForSecondWave(List<SingleFileInfo> deferredFiles)
        {
            const int maxListed = 15;
            var text = new StringBuilder();
            text.AppendLine($"Tyto soubory byly odloženy pro vysokou chybovost čtení ({deferredFiles.Count}):");
            foreach (var file in deferredFiles.Take(maxListed))
            {
                var statistic = file.FinalStatistic;
                string info = "";
                if (statistic != null && statistic.FileSize > 0L && statistic.TotalBlocksSize > 0L)
                    info = $" (zpracováno {100d * statistic.TotalBlocksSize / statistic.FileSize:N1} %, chybných {100d * statistic.BadBlocksSize / statistic.TotalBlocksSize:N0} %)";
                text.AppendLine($"– {System.IO.Path.GetFileName(file.SourceFile)}{info}");
            }
            if (deferredFiles.Count > maxListed)
                text.AppendLine($"… a dalších {deferredFiles.Count - maxListed}");
            text.AppendLine();
            text.AppendLine("Mám nyní zkusit opakovaný pokus o čtení?");
            text.AppendLine();
            text.Append("Ne = ukončit; odložené soubory lze zkusit později novým spuštěním Záchrany souborů (zařadí se na konec).");

            bool isRunning = this.Stopwatch.IsRunning;
            this.Stopwatch.Stop();
            try
            {
                return App.ShowQuestion(text.ToString(), "Nečitelné soubory");
            }
            finally
            {
                if (isRunning) this.Stopwatch.Start();
            }
        }
        #endregion
        #region Ztráta zdrojového zařízení (odpojená karta)
        /// <summary>
        /// Zdrojové zařízení se odpojilo nebo přestalo odpovídat. Nastavuje thread Work, běh se pak ukončí.
        /// <para/>
        /// Blok, jehož čtení selhalo kvůli ztrátě zařízení, se <b>nepočítá jako chybný</b>: nic se nezapíše, <see cref="SingleBlockInfo.ErrorsCount"/> se nemění,
        /// blok zůstává nezpracovaný. Po znovupřipojení karty a novém spuštění se přečte znovu (navázání najde i nezpracované bloky uprostřed souboru).
        /// </summary>
        private bool __SourceDeviceLost;
        /// <summary>
        /// Počet chyb čtení po sobě (bez úspěšného čtení mezi nimi). Po každých <see cref="ConsecutiveErrorsCheckCount"/> chybách se ověřuje, zda zdroj ještě odpovídá.
        /// </summary>
        private int __ConsecutiveReadErrors;
        /// <summary>
        /// Při kolika chybách po sobě proběhne další kontrola dostupnosti zdroje (8, 16, 32, ...); po úspěšném čtení se vrací na <see cref="ConsecutiveErrorsCheckCount"/>
        /// </summary>
        private int __NextDeviceCheckAt = ConsecutiveErrorsCheckCount;
        /// <summary>
        /// Počet chyb čtení (chyb bloků, ne ztráty zařízení) v aktuálním souboru v tomto běhu; pro <see cref="SingleLogInfo.ProblemRuns"/>
        /// </summary>
        private int __CurrentFileReadErrors;
        /// <summary>
        /// Po kolika chybách čtení po sobě se ověřuje dostupnost zdroje (s čekáním <see cref="ConsecutiveErrorsWaitMilisecs"/>)
        /// </summary>
        private const int ConsecutiveErrorsCheckCount = 8;
        /// <summary>
        /// Jak dlouho se po sérii chyb čeká, zda Windows zdrojové zařízení neodpojí (ms)
        /// </summary>
        private const int ConsecutiveErrorsWaitMilisecs = 3000;
        /// <summary>
        /// Kódy chyb Win32 (z HResult výjimky), které znamenají ztrátu zařízení / média, ne chybu bloku
        /// </summary>
        private static readonly int[] DeviceLostErrorCodes = new int[]
        {
            2,      // ERROR_FILE_NOT_FOUND
            3,      // ERROR_PATH_NOT_FOUND
            6,      // ERROR_INVALID_HANDLE
            15,     // ERROR_INVALID_DRIVE
            21,     // ERROR_NOT_READY
            55,     // ERROR_DEV_NOT_EXIST
            433,    // ERROR_NO_SUCH_DEVICE
            1006,   // ERROR_FILE_INVALID: svazek otevřeného souboru byl externě změněn (vyjmutá karta)
            1110,   // ERROR_MEDIA_CHANGED
            1112,   // ERROR_NO_MEDIA_IN_DRIVE
            1167,   // ERROR_DEVICE_NOT_CONNECTED
            1617    // ERROR_DEVICE_REMOVED
        };
        /// <summary>
        /// Vrátí true, pokud daná výjimka (při otevření / čtení zdroje) znamená ztrátu zdrojového zařízení:
        /// podle kódu chyby, anebo podle toho, že zdroj už není dostupný.
        /// </summary>
        /// <param name="exc"></param>
        /// <returns></returns>
        private bool _IsSourceDeviceLostError(Exception exc)
        {
            if (exc is DriveNotFoundException || exc is DirectoryNotFoundException || exc is FileNotFoundException) return true;
            if (exc is IOException)
            {
                int hResult = exc.HResult;
                if ((hResult & unchecked((int)0xFFFF0000)) == unchecked((int)0x80070000) && DeviceLostErrorCodes.Contains(hResult & 0xFFFF))
                    return true;
            }
            return !_IsSourceAvailable(true);
        }
        /// <summary>
        /// Vrátí true, pokud je zdrojové zařízení aktuálního souboru dostupné (disk je Ready) a případně (<paramref name="checkFile"/>) i soubor existuje.
        /// Nikdy nevyhodí výjimku.
        /// </summary>
        /// <param name="checkFile"></param>
        /// <returns></returns>
        private bool _IsSourceAvailable(bool checkFile)
        {
            try
            {
                var sourceFile = __CurrentFile?.SourceFile;
                if (String.IsNullOrEmpty(sourceFile)) return true;
                // Zamrzlá karta neodpoví ani na IsReady => s časovým limitem; nedočkáme-li se odpovědi, zdroj je nedostupný (SourceHungException => false):
                return _SourceIo(() =>
                {
                    var root = System.IO.Path.GetPathRoot(sourceFile);
                    if (!String.IsNullOrEmpty(root) && root.Length <= 3)          // Jen lokální disk "H:\"; UNC cesty neřešíme
                    {
                        var drive = new DriveInfo(root);
                        if (!drive.IsReady) return false;
                    }
                    return (!checkFile || File.Exists(sourceFile));
                }, false, SourceAvailableTimeoutSeconds);
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Po sérii chyb čtení počká až <see cref="ConsecutiveErrorsWaitMilisecs"/>, zda Windows zdrojové zařízení neodpojí.
        /// Vrátí true, pokud zdroj přestal být dostupný. Při Stop přestane čekat.
        /// </summary>
        /// <returns></returns>
        private bool _WaitForSourceDeviceLost()
        {
            var timeEnd = DateTime.UtcNow.AddMilliseconds(ConsecutiveErrorsWaitMilisecs);
            while (true)
            {
                if (!_IsSourceAvailable(true)) return true;
                if (IsStoped || DateTime.UtcNow >= timeEnd) return false;
                Thread.Sleep(250);
            }
        }
        /// <summary>
        /// Oznámí uživateli ztrátu zdrojového zařízení a jak pokračovat
        /// </summary>
        private void _ShowSourceDeviceLost()
        {
            var sourceFile = __CurrentFile?.SourceFile ?? "";
            var root = System.IO.Path.GetPathRoot(sourceFile);
            if (__SourceHung)
                App.ShowWarning("Zdrojové zařízení přestalo odpovídat: operace s kartou zůstala viset ve Windows a nevrátila ani data, ani chybu.\r\n" +
                              $"Soubor: {sourceFile}\r\n\r\n" +
                              "Záchrana byla zastavena. Dosud přečtená data i log jsou uloženy, nepřečtené bloky NEJSOU označeny jako chybné.\r\n\r\n" +
                              "Jak pokračovat:\r\n" +
                              "1. Vyjměte kartu (tím se uvolní i visící čtení), počkejte asi 10 sekund a znovu ji připojte.\r\n" +
                              $"2. Ověřte, že karta dostala stejné písmeno disku ({root}).\r\n" +
                              "3. Spusťte Záchranu souborů znovu se stejnými parametry - kopírování naváže tam, kde skončilo.",
                              "Zdroj neodpovídá");
            else
                App.ShowWarning("Zdrojové zařízení se během záchrany odpojilo nebo přestalo odpovídat.\r\n" +
                              $"Soubor: {sourceFile}\r\n\r\n" +
                              "Záchrana byla přerušena. Dosud přečtená data i log jsou uloženy, nepřečtené bloky NEJSOU označeny jako chybné.\r\n\r\n" +
                              "Jak pokračovat:\r\n" +
                              "1. Vyjměte kartu, počkejte asi 10 sekund a znovu ji připojte.\r\n" +
                              $"2. Ověřte, že karta dostala stejné písmeno disku ({root}).\r\n" +
                              "3. Spusťte Záchranu souborů znovu se stejnými parametry - kopírování naváže tam, kde skončilo.",
                              "Zdroj odpojen");
        }
        #region Zamrzlé zařízení: operace se zdrojem s hlídáním (watchdog)
        /// <summary>
        /// Po této době (sekundy) se operace se zdrojem považuje za zamrzlou a Summary ukazuje "Zdroj neodpovídá".
        /// Běžné čtení bloku trvá do 1 s, chybné (karta odmítne) desetiny sekundy.
        /// </summary>
        private const int SourceHangWarningSeconds = 20;
        /// <summary>
        /// Po stisku Stop se na probíhající operaci se zdrojem čeká nejvýše tolik sekund, pak se opustí (vlákno operace zůstane viset na pozadí)
        /// </summary>
        private const int SourceStopAbandonSeconds = 3;
        /// <summary>
        /// Časový limit (sekundy) pro test dostupnosti zdroje <see cref="_IsSourceAvailable(bool)"/>: disk, který neodpoví ani na dotaz IsReady, je nedostupný
        /// </summary>
        private const int SourceAvailableTimeoutSeconds = 5;
        /// <summary>
        /// Interval kontroly stavu během čekání na operaci se zdrojem (ms)
        /// </summary>
        private const int SourceIoCheckMilisecs = 100;
        /// <summary>
        /// Zdrojové zařízení zamrzlo (operace opuštěna po Stop nebo po časovém limitu). Zároveň je nastaveno <see cref="__SourceDeviceLost"/>.
        /// </summary>
        private bool __SourceHung;
        /// <summary>
        /// Čas zahájení právě probíhající operace se zdrojem (<see cref="_GetAwakeTicks"/> = bez doby spánku), 0 = žádná neprobíhá. Čte GUI thread v <see cref="GetSummary"/>.
        /// </summary>
        private long __SourceIoStartTicks;
        /// <summary>
        /// Vrátí čas od startu systému BEZ doby spánku a hibernace, v jednotkách 100 ns (rozlišení cca 15,6 ms).
        /// <para/>
        /// Operace se zdrojem, která přečká spánek Windows (vlákna stojí, po probuzení doběhne), se tak nehodnotí jako zamrzlá karta:
        /// DateTime ani Stopwatch dobu spánku nevylučují (ověřeno 2026-11-22: blok čtený přes spánek měl v logu 824 s).
        /// </summary>
        /// <returns></returns>
        private static long _GetAwakeTicks()
        {
            QueryUnbiasedInterruptTime(out ulong ticks);
            return (long)ticks;
        }
        /// <summary>
        /// Vrátí počet sekund bez doby spánku od daného času <see cref="_GetAwakeTicks"/>
        /// </summary>
        /// <param name="startAwakeTicks"></param>
        /// <returns></returns>
        private static double _GetAwakeSeconds(long startAwakeTicks)
        {
            return (double)(_GetAwakeTicks() - startAwakeTicks) / 10000000d;
        }
        /// <summary>
        /// Vrátí dobu spánku systému (sekundy) od daného okamžiku: rozdíl přesného času (<see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>, počítá i spánek)
        /// a času bez spánku (<see cref="_GetAwakeTicks"/>). Rozdíly pod <see cref="SleepDetectSeconds"/> jsou šum hrubšího rozlišení => 0.
        /// </summary>
        /// <param name="startTimestamp">Počátek: <see cref="System.Diagnostics.Stopwatch.GetTimestamp"/></param>
        /// <param name="startAwakeTicks">Počátek: <see cref="_GetAwakeTicks"/></param>
        /// <returns></returns>
        private static double _GetSleptSeconds(long startTimestamp, long startAwakeTicks)
        {
            double totalSeconds = (double)(System.Diagnostics.Stopwatch.GetTimestamp() - startTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency;
            double slept = totalSeconds - _GetAwakeSeconds(startAwakeTicks);
            return (slept >= SleepDetectSeconds ? slept : 0d);
        }
        /// <summary>
        /// Minimální rozdíl času se spánkem a bez spánku (sekundy), který se považuje za spánek systému
        /// </summary>
        private const double SleepDetectSeconds = 1d;
        /// <summary>
        /// Opuštěná (visící) operace se zdrojem; po jejím případném dokončení se uvolní zdrojový stream.
        /// </summary>
        private Task __AbandonedSourceIo;
        /// <summary>
        /// Vrátí počet sekund, po které visí aktuální operace se zdrojem, pokud překročila <see cref="SourceHangWarningSeconds"/>; jinak null
        /// </summary>
        /// <returns></returns>
        private double? _GetSourceNotRespondingSeconds()
        {
            long startTicks = Interlocked.Read(ref __SourceIoStartTicks);
            if (startTicks == 0L) return null;
            double seconds = _GetAwakeSeconds(startTicks);                     // Bez doby spánku systému
            return (seconds >= SourceHangWarningSeconds ? seconds : (double?)null);
        }
        /// <summary>
        /// Provede operaci se zdrojovým zařízením (otevření, čtení, test dostupnosti) v samostatném vlákně a hlídá ji.
        /// <para/>
        /// Důvod: zamrzlá karta (Windows ji neodpojí) blokuje synchronní I/O neomezeně dlouho a nevrátí ani data, ani chybu.
        /// Pracovní vlákno by pak nereagovalo na Stop. Zde pracovní vlákno čeká s kontrolou Stop a časového limitu;
        /// po Stop (a <see cref="SourceStopAbandonSeconds"/>) nebo po <paramref name="timeoutSeconds"/> operaci opustí, nastaví <see cref="__SourceHung"/>
        /// a vyhodí <see cref="SourceHungException"/>. Opuštěné vlákno visí dál, dokud Windows operaci neukončí (typicky vyjmutí karty).
        /// <para/>
        /// Výjimka z operace se předá beze změny (zachová HResult pro <see cref="_IsSourceDeviceLostError(Exception)"/>).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="io">Operace se zdrojem</param>
        /// <param name="disposeLateResult">true = pokud opuštěná operace později doběhne, její výsledek (IDisposable, např. stream) se uvolní</param>
        /// <param name="timeoutSeconds">Časový limit operace v sekundách; 0 = bez limitu (opouští se jen po Stop)</param>
        /// <returns></returns>
        private T _SourceIo<T>(Func<T> io, bool disposeLateResult = false, int timeoutSeconds = 0)
        {
            var task = Task.Factory.StartNew(io, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            // Časy bez doby spánku systému: operace, která přečká spánek Windows, není zamrzlá karta (ani pro limit, ani pro zobrazení, ani pro Teploměr zdraví):
            long startTicks = _GetAwakeTicks();
            bool isOuter = (Interlocked.CompareExchange(ref __SourceIoStartTicks, startTicks, 0L) == 0L);   // Vnořené volání (test dostupnosti uvnitř) čas nepřepisuje
            try
            {
                long? stopTicks = null;
                while (!task.Wait(SourceIoCheckMilisecs))
                {
                    this.CallWorkingStepWhenTime();                            // GUI se obnovuje i během čekání => zobrazí "Zdroj neodpovídá N s"
                    bool abandon = false;
                    if (IsStoped)
                    {
                        if (!stopTicks.HasValue) stopTicks = _GetAwakeTicks();
                        abandon = (_GetAwakeSeconds(stopTicks.Value) >= SourceStopAbandonSeconds);
                    }
                    if (timeoutSeconds > 0 && _GetAwakeSeconds(startTicks) >= timeoutSeconds)
                        abandon = true;
                    if (abandon)
                    {
                        __Health.AddHang();                                    // Teploměr zdraví: opuštěná operace = zamrznutí
                        __SourceHung = true;
                        __AbandonedSourceIo = task;
                        if (disposeLateResult)
                            task.ContinueWith(t => { if (t.Status == TaskStatus.RanToCompletion) (t.Result as IDisposable)?.Dispose(); });
                        throw new SourceHungException($"Operace se zdrojovým zařízením neodpověděla do {_GetAwakeSeconds(startTicks):N0} s a byla opuštěna.");
                    }
                }
                // Teploměr zdraví: operace doběhla, ale visela déle než práh varování = zamrznutí (karta se vzpamatovala).
                //  Jen vnější volání a ne opuštěné vnořené (to je už započteno):
                if (isOuter && _GetAwakeSeconds(startTicks) >= SourceHangWarningSeconds && !(task.Exception?.InnerException is SourceHungException))
                    __Health.AddHang();
                return task.GetAwaiter().GetResult();                      // Výsledek, nebo původní výjimka (ne AggregateException)
            }
            finally
            {
                if (isOuter) Interlocked.Exchange(ref __SourceIoStartTicks, 0L);
            }
        }
        /// <summary>
        /// Uvolní zdrojový stream. Pokud na něm visí opuštěné čtení, uvolní jej až po jeho dokončení (uzavření by mohlo zablokovat i pracovní vlákno).
        /// </summary>
        /// <param name="sourceStream"></param>
        private void _CloseSourceStream(FileStream sourceStream)
        {
            if (sourceStream is null) return;
            var abandonedIo = __AbandonedSourceIo;
            if (abandonedIo != null && !abandonedIo.IsCompleted)
                abandonedIo.ContinueWith(t => { try { sourceStream.Dispose(); } catch { } });
            else
            {
                sourceStream.Close();
                sourceStream.Dispose();
            }
        }
        /// <summary>
        /// Operace se zdrojovým zařízením neodpověděla (zamrzlá karta) a byla opuštěna, viz <see cref="_SourceIo{T}(Func{T}, bool, int)"/>
        /// </summary>
        internal class SourceHungException : IOException
        {
            public SourceHungException(string message) : base(message) { }
        }
        #endregion
        #endregion
        /// <summary>
        /// Čas (ticky <see cref="BackWorker{TTarget}.CurrentTime"/>) zahájení celé akce; null = dosud nezačala
        /// </summary>
        private long? __RunStartTime;
        /// <summary>
        /// Celkový čas akce v sekundách, naplněn po jejím skončení; null = akce dosud běží
        /// </summary>
        private decimal? __RunTotalSeconds;
        /// <summary>
        /// Příprava seznamu souborů k záchraně = pole <see cref="__Files"/>
        /// </summary>
        private void _PrepareFileList()
        {
            // Seznam plníme do lokální proměnné a do __Files jej vložíme až hotový: GetSummary() jej čte z GUI threadu.
            var files = new List<SingleFileInfo>();
            var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);         // Duplicity (týž soubor explicitně i přes wildcard) zpracujeme jen jednou
            var excludes = new List<Tuple<System.Text.RegularExpressions.Regex, bool>>();  // Řádky začínající "!" = soubory k vynechání; Item2 = maska obsahuje adresář
            var inputInfo = this.Target;
            if (inputInfo != null && !String.IsNullOrEmpty(inputInfo.InputFileNames))
            {
                var outputPath = _GetOutputPath();

                // Nejprve oddělíme řádky k vynechání (mohou být kdekoli, i za řádkem, který soubor zahrnuje), prázdné řádky ignorujeme:
                var includeLines = new List<string>();
                foreach (var inputFile in inputInfo.InputFileNames.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = getInnerText(inputFile);
                    if (line.Length == 0) continue;
                    if (line[0] == '!')
                    {
                        var exclude = getInnerText(line.Substring(1));
                        if (exclude.Length > 0) excludes.Add(createWildcardRegex(exclude));
                    }
                    else
                        includeLines.Add(line);
                }

                foreach (var includeLine in includeLines)
                    prepareSingleLine(includeLine, inputInfo, outputPath);
            }
            __Files = _SortFilesByProblems(files);


            // Vrátí Regex pro masku k vynechání (wildcards * a ?), bez ohledu na velikost písmen. Item2 = maska obsahuje adresář.
            Tuple<System.Text.RegularExpressions.Regex, bool> createWildcardRegex(string mask)
            {
                mask = mask.Replace('/', '\\');
                bool withPath = (mask.IndexOf('\\') >= 0 || mask.IndexOf(':') >= 0);
                var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(mask).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                return new Tuple<System.Text.RegularExpressions.Regex, bool>(regex, withPath);
            }
            // Vrátí true, pokud daný soubor má být vynechán
            bool isExcluded(string file)
            {
                if (excludes.Count == 0) return false;
                var name = System.IO.Path.GetFileName(file);
                foreach (var exclude in excludes)
                {   // Maska s adresářem se porovnává s celou cestou, maska bez adresáře jen se jménem souboru:
                    if (exclude.Item1.IsMatch(exclude.Item2 ? file : name)) return true;
                }
                return false;
            }


            // Připraví soubor[y] za jeden vstupní řádek
            void prepareSingleLine(string inputLine, FileRescueInputInfo info, string outputPath)
            {
                if (String.IsNullOrEmpty(inputLine)) return;

                // Očistit název na holé jméno souboru, bez uvozovek a mezer na začátku a konci:
                inputLine = getInnerText(inputLine);

                inputLine = inputLine.Trim();
                if (inputLine.Contains("*") || inputLine.Contains("?"))
                {   // Wildcards:
                    //  Chybný řádek (neexistující / nepřístupný adresář, relativní cesta, neplatné znaky) ohlásíme a pokračujeme dalšími řádky.
                    string[] wildFiles;
                    try
                    {
                        var directory = System.IO.Path.GetDirectoryName(inputLine);
                        if (String.IsNullOrEmpty(directory)) throw new ArgumentException("Vstup se zástupnými znaky musí obsahovat plnou cestu k adresáři.");
                        wildFiles = System.IO.Directory.GetFiles(directory, System.IO.Path.GetFileName(inputLine));
                    }
                    catch (Exception exc)
                    {
                        App.ShowError($"Nelze načíst seznam souborů pro vstup:\r\n{inputLine}\r\n\r\n{exc.Message}", "Chyba vstupních souborů");
                        return;
                    }
                    foreach (var file in wildFiles)
                        prepareSingleFile(file, info, outputPath);
                }
                else
                {   // Single file:
                    prepareSingleFile(inputLine, info, outputPath);
                }
            }
            // Připraví jeden soubor
            void prepareSingleFile(string inputFile, FileRescueInputInfo info, string outputPath)
            {
                // Očistit název na holé jméno souboru, bez uvozovek a mezer na začátku a konci:
                inputFile = getInnerText(inputFile);
                if (inputFile.Length == 0) return;
                if (isExcluded(inputFile)) return;                             // Vynechán řádkem "!maska"
                if (!fileNames.Add(inputFile)) return;                         // Duplicita

                var fileName = System.IO.Path.GetFileName(inputFile);                               // Bez adresáře                Anděl Páně.ts1
                var fileLog = System.IO.Path.GetFileName(inputFile) + ".rescuelog";                  // Soubor obsahující log       Anděl Páně.ts1.rescuelog
                var fileInfo = new SingleFileInfo()
                {
                    SourceFile = inputFile,
                    DestinationFile = System.IO.Path.Combine(outputPath, fileName),
                    DestinationLog = System.IO.Path.Combine(outputPath, fileLog)
                };

                if (info.TestAddBadBlocks)
                {
                    fileInfo.SimulatedErrors = new Tuple<long, int>[]
                    {
                        new Tuple<long, int>(12345678, 3),
                        new Tuple<long, int>(123456789, 1),
                        new Tuple<long, int>(456789123, 2),
                        new Tuple<long, int>(789456123, 99),
                        new Tuple<long, int>(654987321, 1)
                    };
                }

                if (info.TestAddSimulatedWaiting)
                {
                    fileInfo.SimulatedWaiting = 12;
                }
                fileInfo.CheckFile();                                          // Délka souboru je potřeba předem pro celkový progress přes všechny soubory
                files.Add(fileInfo);
            }
            string getInnerText(string input)
            {
                if (String.IsNullOrEmpty(input)) return "";
                input = input.Trim();
                if (input.Length > 2 && input[0] == '"')
                    input = input.Substring(1, input.Length - 2).Trim();
                return input;
            }
        }
        /// <summary>
        /// Vrátí seznam souborů v pořadí zpracování: nejprve soubory bez problémů (podle jména), na konec soubory, které při dřívějších bězích
        /// dělaly problémy (odpojení zdroje, přerušení s chybami čtení) – podle počtu problémů vzestupně, pak podle jména.
        /// <para/>
        /// Smysl: trvale nečitelný soubor (který nelze na zdroji smazat ani přejmenovat) nesmí při každém opakování blokovat zpracování ostatních souborů.
        /// Počty problémů se čtou z hlavičky logu (<see cref="SingleLogInfo.ReadProblemCounters"/>); soubor bez logu je bez problémů.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static List<SingleFileInfo> _SortFilesByProblems(List<SingleFileInfo> files)
        {
            var items = files
                .Select(f => { SingleLogInfo.ReadProblemCounters(f.DestinationLog, out int deviceLostCount, out int problemRuns); return new { File = f, Problems = deviceLostCount + problemRuns }; })
                .ToList();
            items.Sort((a, b) =>
            {
                int cmp = (a.Problems > 0).CompareTo(b.Problems > 0);           // Bez problémů dříve
                if (cmp == 0) cmp = a.Problems.CompareTo(b.Problems);           // Méně problémů dříve
                if (cmp == 0) cmp = SingleFileInfo.CompareByFullFileName(a.File, b.File);
                return cmp;
            });
            return items.Select(i => i.File).ToList();
        }
        /// <summary>
        /// Pole souborů k záchraně, každý záznam obsahuje informace o zdrojovém souboru, cílovém souboru a log souboru.
        /// </summary>
        private List<SingleFileInfo> __Files;
        #endregion
        #region Práce na jednom souboru
        /// <summary>
        /// Provede záchranu jednoho souboru podle informací v <paramref name="fileInfo"/>.
        /// </summary>
        /// <param name="fileInfo"></param>
        private void _RunSingleFile(SingleFileInfo fileInfo)
        {
            var isPrepared = _PrepareSingleFile(fileInfo);
            if (!isPrepared) return;

            this._CopySingleFile();

            // Konečná statistika pro souhrn přes soubory. _CopySingleFile() ji ukládá sám, ale pokud skončil předčasně
            // (soubor byl podle logu už celý zpracován, nebo Stop), uložíme ji zde:
            if (fileInfo.FinalStatistic is null && fileInfo.CurrentLog != null)
                fileInfo.FinalStatistic = fileInfo.CurrentLog.Statistic;

            // Pokud najdeme Output soubor, pak jsme s kopírováním začali už dříve. Zkusíme k němu najít log soubor a podle něj se rozhodneme, zda a odkud a jak pokračovat.
            // Pokud Log soubor obsahuje "Hotovo OK", pak je záchrana dokončena a pro daný soubor nic neděláme.

            // Nyní máme načten Log soubor, anebo jej máme nově vytvořený, tak s jeho pomocí budeme postupně kopírovat zdrojový soubor do cíle - kopírujeme dosud chybějící neznámé bloky,
            // a později se pokusíme zkopírovat i chybové bloky:

        }
        /// <summary>
        /// Provede přípravu struktur pro kopírování, ale nekopíruje
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        private bool _PrepareSingleFile(SingleFileInfo fileInfo)
        {
            if (Stopping) return false;

            __CurrentFile = fileInfo;
            try
            {   // CheckFile čte metadata zdroje => zamrzlá karta by zablokovala i Stop, proto s hlídáním:
                _SourceIo(() => { fileInfo.CheckFile(); return true; });
            }
            catch (SourceHungException)
            {
                __SourceDeviceLost = true;
                this.__CurrentFile.SourceFileStatus = FileStatus.None;
                return false;
            }
            this.CallWorkingStep();
            if (this.__CurrentFile.SourceFileStatus == FileStatus.NotExists)
            {   // Soubor neexistuje: pokud neexistuje ani zdrojový disk (karta se odpojila mezi soubory), ukončíme celý běh:
                if (!_IsSourceAvailable(false))
                {
                    __SourceDeviceLost = true;
                    this.__CurrentFile.SourceFileStatus = FileStatus.None;      // Nevykazovat jako "Nenalezeno": soubor nejspíš existuje, jen karta není připojena
                }
                return false;
            }
            if (Stopping) return false;

            __CurrentFile.LoadLog();
            __CurrentLog = __CurrentFile.CurrentLog;
            this.CallWorkingStep();
            if (Stopping) return false;

            return true;
        }
        /// <summary>
        /// Provede kopírování jednoho souboru, data jsou připravena
        /// </summary>
        private void _CopySingleFile()
        {
            if (Stopping) return;                                              // Uživatel chce skončit a nastavil Stop = true

            var fileLength = this.__CurrentFile?.SourceFileLength ?? 0L;
            var currentBlock = this.__CurrentLog.GetFirstBlock();              // Získáme data o prvním bloku ke kopírování. K tomu máme Log, k tomu nepotřebujeme otevírat SourceStream.
            if (currentBlock is null) return;
            this.__CurrentLog.StartTime = this.CurrentTime;

            __CurrentFileReadErrors = 0;
            _DeferWindowReset();
            FileStream sourceStream = null;
            bool isComplete = false;                                          // true = zpracovány všechny bloky (ne Stop, ne chyba zápisu, ne výjimka)
            try
            {
                __CurrentFile.SourceFileStatus = FileStatus.Copying;
                this.CallWorkingStep();

                var sourceFile = this.__CurrentFile.SourceFile;
                __Health.StartFile(sourceFile);
                sourceStream = _SourceIo(() => System.IO.File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read), true);
                _WrittingThreadAddRequest(new WritterRequestInfo(WritterRequestType.OpenWritterStream, this.__CurrentFile.DestinationFile));
             
                while (currentBlock != null)
                {
                    _CopySingleBlock(sourceStream, currentBlock);
                    this.CallWorkingStepWhenTime();
                    if (__SourceDeviceLost) break;                             // Zdroj odpojen: soubor nedokončen (bez ~E), nepřečtené bloky zůstávají nezpracované
                    if (__CurrentFileDefer) break;                             // Nečitelný soubor (vysoká chybovost v 1. průchodu): odložit, pokračovat dalším souborem
                    if (Stopping) break;                                       // Uživatel chce skončit a nastavil Stop = true
                    if (__WrittingStreamAbort) break;                          // Ve Write streamu došlo k chybě => končíme jeden soubor
                    // Při opakování chybných bloků a na konci 1. průchodu se mapa bloků prohledává od začátku => musí obsahovat výsledek právě zapisovaného bloku:
                    if (this.__CurrentLog.ErrorLevelProcess > 0 || currentBlock.BlockEnd >= fileLength)
                        _WrittingThreadWaitIdle();
                    currentBlock = this.__CurrentLog.GetNextBlock();
                }
                isComplete = (currentBlock is null);
            }
            catch (Exception ex)
            {   // Typicky selhání File.Open() zdroje; pokud je důvodem odpojená nebo zamrzlá karta, ukončíme celý běh:
                if (ex is SourceHungException || _IsSourceDeviceLostError(ex)) __SourceDeviceLost = true;
                this.__CurrentLog.SaveException(ex);
            }
            finally
            {
                if (sourceStream != null)
                {   // Pokud File.Open() selhal (např. zamčený soubor), stream neexistuje a zbytek finally musí proběhnout.
                    //  Pokud na streamu visí opuštěné čtení (zamrzlá karta), uvolní se až po jeho dokončení:
                    _CloseSourceStream(sourceStream);
                    sourceStream = null;
                }
                _WrittingThreadAddRequest(new WritterRequestInfo(WritterRequestType.CloseWritterStream));    // Pokud ve WriteStreamu došlo k chybě, pak tady se WriteThread vyčistí a připraví na nový request OpenStream
                this.__CurrentLog.TotalTime = this.GetSeconds(this.__CurrentLog.StartTime);
                // Počítadla problémů souboru (hlavička logu): problémový soubor se při dalším spuštění zpracuje až po ostatních souborech.
                //  Problém = soubor nedokončen a v tomto běhu se odpojil zdroj, nebo došlo k chybám čtení (typicky uživatel dá Stop u beznadějného souboru).
                if (__SourceDeviceLost)
                    this.__CurrentLog.DeviceLostCount++;
                if (!isComplete && (__SourceDeviceLost || __CurrentFileReadErrors > 0))
                    this.__CurrentLog.ProblemRuns++;
                this.__CurrentLog.SaveLogFile(true);
                // Datum, atributy a přejmenování na ~E jen u úplně zpracovaného souboru: u přerušeného by navázání zapisovalo do nového souboru
                //  s původním jménem (data by zůstala v ~E), a atribut ReadOnly by navázání znemožnil.
                if (isComplete)
                    _CopySingleAttributes();
                if (__CurrentFileDefer && !isComplete)
                    __CurrentFile.IsDeferred = true;
                __CurrentFile.FinalStatistic = this.__CurrentLog.Statistic;
                __CurrentFile.SourceFileStatus = FileStatus.Finished;
                this.CallWorkingStep();
            }


            // App.ShowError

        }
        /// <summary>
        /// Zajistí kopírování jednoho bloku
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="currentBlock"></param>
        /// <returns></returns>
        private void _CopySingleBlock(FileStream sourceStream, SingleBlockInfo currentBlock)
        {
            var buffer = new byte[currentBlock.BlockLength];                   // BlockLength je skutečně platná délka i u posledního bloku, protože je určená mj. délkou vstupního souboru.
            long timeBegin = 0L;
            long sleepCheckTimestamp = 0L;                                     // Pro odečtení doby spánku systému z doby čtení bloku
            long sleepCheckAwakeTicks = 0L;
            bool hadPreviousErrors = (currentBlock.ErrorsCount > 0);            // Pro Teploměr zdraví: opakované čtení dříve chybného bloku
            try
            {
                simulateError(currentBlock);

                timeBegin = this.CurrentTime;
                sleepCheckTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
                sleepCheckAwakeTicks = _GetAwakeTicks();
                simulateWaiting(currentBlock);

                // Tady probíhá čtení vstupního souboru; s hlídáním zamrzlé karty (čtení může viset neomezeně dlouho a Stop by nefungoval):
                long blockStart = currentBlock.BlockStart;
                int blockLength = currentBlock.BlockLength;
                currentBlock.ContentLength = _SourceIo(() =>
                {
                    simulateHang(blockStart, blockLength);
                    sourceStream.Seek(blockStart, SeekOrigin.Begin);
                    return sourceStream.Read(buffer, 0, blockLength);
                });
                // Blok leží uvnitř délky souboru, takže 0 bajtů není konec souboru, ale chyba (např. odpojené zařízení).
                //  Dříve se ukládal výsledek Empty = OK bez zápisu => díra v cíli vedená jako OK.
                if (currentBlock.ContentLength <= 0)
                    throw new IOException($"Čtení bloku na pozici {currentBlock.BlockStart:N0} vrátilo 0 bajtů.");
                decimal readSeconds = getReadSeconds();
                currentBlock.StoreReadTime(readSeconds, false);
                __Health.AddBlockRead(blockStart, currentBlock.ContentLength, (double)readSeconds, false, hadPreviousErrors);
                __ConsecutiveReadErrors = 0;
                __NextDeviceCheckAt = ConsecutiveErrorsCheckCount;
                _DeferWindowAdd(false);

                // Výsledek Success vepíše do bloku až thread Writter po úspěšném zápisu:
                currentBlock.Content = buffer;
                _WrittingThreadAddRequest(new WritterRequestInfo(WritterRequestType.WriteDataBlock, currentBlock, CopyBlockResult.Success));
            }
            catch (Exception ex)
            {
                // Ztráta zdrojového zařízení (vyjmutá / odpojená karta) NENÍ chyba bloku: nic nezapíšeme, ErrorsCount neměníme,
                //  blok zůstane nezpracovaný a po znovupřipojení karty se při navázání přečte znovu. Běh se ukončí.
                //  Totéž platí pro zamrzlou kartu (čtení opuštěno po Stop): blok zůstane nezpracovaný, na buffer už opuštěné čtení může zapisovat.
                if (ex is SourceHungException || _IsSourceDeviceLostError(ex))
                {
                    __SourceDeviceLost = true;
                    currentBlock.Content = null;
                    return;
                }

                // Toto je chyba při čtení bloku ze zdroje => tuto chybu zaznamenám do bloku, ale nezrušíme celé kopírování
                //   = toto je smysl celého tohoto procesu !!!!
                decimal errorSeconds = getReadSeconds();
                currentBlock.StoreReadTime(errorSeconds, true);
                // Teploměr zdraví: doba jen pokud čtení skutečně začalo (simulovaná chyba nastává před měřením => timeBegin = 0):
                __Health.AddBlockRead(currentBlock.BlockStart, 0, (timeBegin > 0L ? (double?)(double)errorSeconds : null), true, hadPreviousErrors);
                Array.Clear(buffer, 0, buffer.Length);
                currentBlock.Content = buffer;
                currentBlock.ContentLength = currentBlock.Content.Length;
                // Výsledek Error (včetně navýšení ErrorsCount) vepíše do bloku až thread Writter po úspěšném zápisu prázdného bloku:
                _WrittingThreadAddRequest(new WritterRequestInfo(WritterRequestType.WriteVoidBlock, currentBlock, CopyBlockResult.Error));

                // Série chyb po sobě: karta před odpojením typicky hlásí řadu chyb, a Windows ji odpojí až za chvíli.
                //  Počkáme, zda zdroj nezmizí; pokud ano, další bloky už nebudeme označovat jako chybné:
                __CurrentFileReadErrors++;
                __ConsecutiveReadErrors++;
                _DeferWindowAdd(true);
                //  Kontrola po 8, 16, 32, 64... chybách: u souboru nečitelného v celé délce (karta zůstává připojená) by čekání 3 s po každých 8 chybách
                //  zabralo většinu času (ověřeno: 2 190 s, z toho ~31 min čekání).
                if (__ConsecutiveReadErrors >= __NextDeviceCheckAt && !__CurrentFileDefer)        // Soubor se právě odkládá => kontrola nemá smysl, proběhne případně u dalšího souboru
                {
                    __NextDeviceCheckAt = 2 * __NextDeviceCheckAt;
                    if (_WaitForSourceDeviceLost())
                        __SourceDeviceLost = true;
                }
            }
            // Výsledky Success a Error ukládá do bloku thread Writter až po zápisu (ResultAfterWrite):
            //  dříve se ukládaly zde, předem => při Stop nebo chybě zápisu byl blok v logu OK, ale v cílovém souboru chyběl.

            // Aktualizace Logu, případně i na disk:
            this.__CurrentLog.AddUnsavedChange();
            if (this.__CurrentLog.NeedSave)
                this.__CurrentLog.SaveLogFileAsync();


            // Doba čtení bloku bez doby spánku systému (čtení, které přečká spánek Windows, by jinak trvalo celou dobu spánku);
            //  pokud čtení nezačalo (simulovaná chyba před měřením), pak 0 (dříve se ukládal čas od startu Stopwatch):
            decimal getReadSeconds()
            {
                if (timeBegin <= 0L) return 0m;
                decimal seconds = this.GetSeconds(timeBegin) - (decimal)_GetSleptSeconds(sleepCheckTimestamp, sleepCheckAwakeTicks);
                return (seconds < 0m ? 0m : seconds);
            }
            void simulateError(SingleBlockInfo block)
            {
                var simErrors = this.__CurrentFile.SimulatedErrors;
                if (simErrors is null) return;

                // Najdu definici simulované chyby, která je definovaná právě pro ten blok, který nyní řešíme:
                var simError = simErrors.FirstOrDefault(s => block.BlockStart <= s.Item1 && block.BlockEnd > s.Item1);
                if (simError is null) return;

                // Hodnota simulované chyby (Item2) určuje, na kolikátý pokus se chybu podaří přečíst.
                // Například: Item2 = 2 => chybu vyhodíme, pokud this.__CurrentLog.ErrorLevelProcess bude 0 a 1, ale až bude this.__CurrentLog.ErrorLevelProcess == 2, tak chybu nevyhodíme.
                if (this.__CurrentLog.ErrorLevelProcess >= simError.Item2) return;

                // Vyhodíme chybu, která simuluje chybu čtení:
                throw new System.IO.IOException($"Došlo k simulované chybě ve vstupním souboru: pozice {simError.Item1:N0}; ErrorLevel: {simError.Item2}.");
            }
            void simulateHang(long blockStart, int blockLength)
            {   // Test: čtení bloku obsahujícího danou pozici "zamrzne" na neomezeně dlouho (jako zamrzlá karta)
                var hangPosition = this.__CurrentFile.SimulatedHangPosition;
                if (hangPosition.HasValue && blockStart <= hangPosition.Value && (blockStart + blockLength) > hangPosition.Value)
                    System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            void simulateWaiting(SingleBlockInfo block)
            {
                var simWait = this.__CurrentFile.SimulatedWaiting ?? 0;
                if (simWait < 10) return;
                var minWait = 8 * simWait / 10;
                var maxWait = 15 * simWait / 10;
                var currWait = this.Random.Next(minWait, maxWait);
                System.Threading.Thread.Sleep(currWait - 8);
            }
        }
        /// <summary>
        /// Do výstupního souboru vepíše datum a čas z originálního souboru
        /// </summary>
        private void _CopySingleAttributes()
        {
            try
            {
                var sourceInfo = __CurrentFile.SourceFileInfo;
                var targetInfo = new FileInfo(__CurrentFile.DestinationFile);
                if (targetInfo.Exists)
                {
                    targetInfo.LastWriteTime = sourceInfo.LastWriteTime;
                    targetInfo.Attributes = sourceInfo.Attributes;

                    var statistic = this.__CurrentLog.Statistic;
                    if (statistic.BadBlocksCount > 0)
                    {
                        Thread.Sleep(150);

                        var targetErrName = System.IO.Path.Combine(
                            targetInfo.DirectoryName,
                            System.IO.Path.GetFileNameWithoutExtension(targetInfo.Name) + "~E" + statistic.BadBlocksCount.ToString() + System.IO.Path.GetExtension(targetInfo.Name));
                        System.IO.File.Move(targetInfo.FullName, targetErrName);
                        __CurrentFile.DestinationFile = targetErrName;
                    }
                }
            }
            catch { }
        }
        /// <summary>
        /// Výsledek kopírování jednoho bloku
        /// </summary>
        internal enum CopyBlockResult
        {
            None,
            Empty,
            Success,
            Error
        }

        /*
      private void _CopyFastNet()
        {
            try
            {
                FileCopierRequest request = this.Request;
                var sourceFile = request.SourceFile;
                var targetFile = request.DestinationFile;
                var fastBlockSize = request.FastCopyBlockSize;

                using (var sourceStream = System.IO.File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))                 // System.IO.File.OpenRead(sourceFile))
                using (var targetStream = System.IO.File.Open(targetFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))        // System.IO.File.OpenWrite(targetFile))
                {
                    // sourceStream.ReadTimeout = 400;
                    _CallProgress(ProgressStateType.Begin);
                    long position = this.ContentMap.StartPositionFastCopy;
                    long unsavedLength = 0L;
                    while (true)
                    {
                        // _CallProgressStep(getInfo(offset + _BlockSizeDefault, length), offset, 0);
                        var size = _CopyFastNetBlock(sourceStream, targetStream, position, fastBlockSize, ref unsavedLength);
                        if (size == 0 || CancelProcess) break;
                        position += size;
                    }
                    targetStream.Flush();
                    this.ContentMap.FlushMetadata();
                    _CallProgress(ProgressStateType.End);
                }
            }
            catch (Exception exc)
            {
            }
        }
        private int _CopyFastNetBlock(FileStream sourceStream, FileStream targetStream, long position, int fastBlockSize, ref long unsavedLength)
        {
            var bufferSize = this.ContentMap.GetRealBufferSize(position, fastBlockSize);
            if (bufferSize <= 0L) return 0;
            _CallProgress(ProgressStateType.Read, position, bufferSize);

            int processSize = 0;
            try
            {
                var buffer = new byte[bufferSize];
                _ResetTime();
                sourceStream.Seek(position, SeekOrigin.Begin);
                processSize = sourceStream.Read(buffer, 0, fastBlockSize);
                if (processSize > 0)
                {
                    targetStream.Seek(position, SeekOrigin.Begin);
                    targetStream.Write(buffer, 0, processSize);
                    unsavedLength += processSize;
                }
                this.ContentMap.AddBlock(position, processSize, BlockStateType.ContainsData, _TimeMilisec);
                _CallProgress(ProgressStateType.Written, position, bufferSize);
            }
            catch (Exception exc)
            {
                _CallProgress(ProgressStateType.Error, position, bufferSize, 1, exc.Message, true);
                // Do target vepíšu blok délky 'bufferSize' od pozice 'position' obsahující 0:
                processSize = bufferSize;
                var emptyBuffer = new byte[processSize];
                Array.Clear(emptyBuffer, 0, emptyBuffer.Length);
                targetStream.Seek(position, SeekOrigin.Begin);
                targetStream.Write(emptyBuffer, 0, processSize);
                unsavedLength += processSize;
                this.ContentMap.AddBlock(position, bufferSize, BlockStateType.ContainsError, _TimeMilisec);
            }

            if (unsavedLength >= 1048576L)
            {
                _CallProgress(ProgressStateType.Flush);
                targetStream.Flush();
                this.ContentMap.FlushMetadata();
                unsavedLength = 0L;
            }

            return processSize;
        }

        */

        /// <summary>
        /// Aktuální soubor
        /// </summary>
        private SingleFileInfo __CurrentFile;
        /// <summary>
        /// Aktuální data o práci na jednom souboru
        /// </summary>
        private SingleLogInfo __CurrentLog;
        /// <summary>
        /// Stavy práce na jednom souboru
        /// </summary>
        public enum FileStatus
        {
            None,
            NotExists,
            SourceFileExists,
            LogPrepared,
            LogLoaded,
            Copying,
            Finished
        }
        #endregion
        #region class WritterRequest : třída, která přináší další úkol z threadu Work do threadu Writter
        internal class WritterRequestInfo
        {
            public WritterRequestInfo(WritterRequestType requestType)
            {
                this.RequestType = requestType;
            }
            public WritterRequestInfo(WritterRequestType requestType, string writeFileName)
            {
                this.RequestType = requestType;
                this.WriteFileName = writeFileName;
            }
            public WritterRequestInfo(WritterRequestType requestType, SingleBlockInfo writeBlock, CopyBlockResult resultAfterWrite)
            {
                this.RequestType = requestType;
                this.WriteBlock = writeBlock;
                this.ResultAfterWrite = resultAfterWrite;
            }
            public override string ToString()
            {
                return this.RequestType.ToString();
            }
            /// <summary>
            /// Co je po nás požadováno
            /// </summary>
            public WritterRequestType RequestType { get; private set; }
            /// <summary>
            /// Název souboru pro zápis
            /// </summary>
            public string WriteFileName { get; private set; }
            /// <summary>
            /// Data
            /// </summary>
            public SingleBlockInfo WriteBlock { get; private set; }
            /// <summary>
            /// Výsledek čtení bloku, který thread Writter vepíše do <see cref="WriteBlock"/> (<see cref="SingleBlockInfo.StoreBlockResultRead(CopyBlockResult)"/>)
            /// až po úspěšném dokončení zápisu. Pokud se zápis nepovede (nebo se request vůbec nezpracuje), blok zůstane ve stavu před čtením
            /// a při příštím zpracování (i po navázání z logu) se přečte znovu.
            /// </summary>
            public CopyBlockResult ResultAfterWrite { get; private set; }
        }
        internal enum WritterRequestType
        {
            /// <summary>
            /// Neurčeno
            /// </summary>
            None,
            /// <summary>
            /// Otevři stream pro zápis do výstupního souboru
            /// </summary>
            OpenWritterStream,
            /// <summary>
            /// Zapiš blok s reálnými daty, data jsou dodána v requestu
            /// </summary>
            WriteDataBlock,
            /// <summary>
            /// Zapiš blok s Void daty, data jsou dodána v requestu
            /// </summary>
            WriteVoidBlock,
            /// <summary>
            /// Zavři stream pro zápis do výstupního souboru
            /// </summary>
            CloseWritterStream,
            /// <summary>
            /// Ukonči svůj thread
            /// </summary>
            EndThread
        }
        #endregion
        #region Zápis do cílového souboru běží v jiném threadu
        /// <summary>
        /// Připraví thread Writter pro zápis na pozadí, připraví semafory pro jeho řízení, spustí jeho smyčku.
        /// Volá se z threadu Work.
        /// </summary>
        private void _PrepareWrittingThread()
        {
            __WritterIsEndingThread = false;
            __WritterProcessedRequest = null;
            __WritterCurrentRequest = null;
            __WrittingSemaphoreStart = new System.Threading.AutoResetEvent(false);
            __WrittingSemaphoreDone = new System.Threading.AutoResetEvent(false);
            __WrittingThread = new Thread(_WrittingThreadStart);
            __WrittingThread.Name = "WrittingThread";
            __WrittingThread.IsBackground = true;
            __WrittingThread.Start();
        }
        /// <summary>
        /// Předá požadavek ke zpracování do threadu Writter.
        /// Volá se výhradně z threadu Work. Nebude tedy současně voláno z více threadů.
        /// </summary>
        private bool _WrittingThreadAddRequest(WritterRequestInfo request)
        {
            var timeEnd = DateTime.UtcNow.AddSeconds(60);
            while (__WritterCurrentRequest != null)
            {   // Pokud thread Writter aktuálně zpracovává nějaký požadavek, tak počkáme (max 1 minutu):
                if (DateTime.UtcNow > timeEnd) return false;                             // Timeout  čekání na volný Writter
                // Na Stop zde nereagujeme: rozpracovaný blok se musí zapsat a stream zavřít (CloseWritterStream). Writter končí jen na request EndThread.
                if (__WritterIsEndingThread) return false;                               // Writter byl požádán o ukončení své práce

                // Počkáme u semaforu, až Writter dokončí aktuální operaci:
                //  Když skončí, tak nuluje __WritterCurrentRequest a aktivuje semafor __WrittingSemaphoreDone
                //  a my otestujeme (__WritterCurrentRequest != null) a opustíme smyčku while:
                __WrittingSemaphoreDone.WaitOne(250);
                // Jakmile nás semafor pustí dál, je to:
                // a) Writter dokončil svou práci a uvolnil se, takže __WritterCurrentRequest je null a my můžeme vložit nový request
                // b) Uběhl timeout 250ms, a my pro jistotu ověříme stav, zda Writter ještě nedokončil svou práci, takže __WritterCurrentRequest je stále != null a my počkáme dál
            }

            if (__WritterIsEndingThread) return false;                                   // Writter byl požádán o ukončení své práce
            // Writter aktuálně nic neprovádí (nikdo jiný sem __WritterCurrentRequest nevloží) = má čas na další request:
            // Uložíme request a rozsvítíme jeho semafor Start  __WrittingSemaphoreStart:
            __WritterCurrentRequest = request;
            __WrittingSemaphoreStart.Set();
            return true;
        }
        /// <summary>
        /// Počká, až thread Writter dokončí aktuální request (max 1 minutu).
        /// Volá se z threadu Work před hledáním dalšího bloku tam, kde se prohledává mapa bloků znovu od začátku (opakování chybných bloků, konec 1. průchodu):
        /// výsledek posledního bloku (stav, <see cref="SingleBlockInfo.ErrorsCount"/>) vepisuje až Writter po zápisu, a hledání jej musí vidět aktuální.
        /// </summary>
        private void _WrittingThreadWaitIdle()
        {
            var timeEnd = DateTime.UtcNow.AddSeconds(60);
            while (__WritterCurrentRequest != null)
            {
                if (DateTime.UtcNow > timeEnd) return;
                if (__WritterIsEndingThread) return;
                __WrittingSemaphoreDone.WaitOne(250);
            }
        }
        /// <summary>
        /// Vstupní bod a komplexní smyčka threadu Writter.
        /// </summary>
        private void _WrittingThreadStart()
        {
            __WritterIsEndingThread = false;
            while (!__WritterIsEndingThread)
            {   // Zpracováváme požadavky, dokud nedostaneme požadavek EndThread

                // Počkáme, až dostaneme požadavek:
                var request = waitForRequest();
                if (request is null) break;

                __WritterProcessedRequest = request;
                switch (request.RequestType)
                {
                    case WritterRequestType.OpenWritterStream:
                        doOpenStream(request);
                        break;
                    case WritterRequestType.WriteDataBlock:
                    case WritterRequestType.WriteVoidBlock:
                        doWriteBlock(request);
                        break;
                    case WritterRequestType.CloseWritterStream:
                        doCloseStream(request);
                        break;
                    case WritterRequestType.EndThread:
                        __WritterIsEndingThread = true;
                        break;
                }
                __WritterProcessedRequest = null;

                // Máme volno pro další požadavek:
                __WritterCurrentRequest = null;
                // Pokud stále pracujeme, tak dáme signál čekajícímu vláknu Worker, že nám může uložit další request:
                if (!__WritterIsEndingThread)
                    __WrittingSemaphoreDone.Set();
            }

            // Vrátí nově dodaný Request do __WritterCurrentRequest
            WritterRequestInfo waitForRequest()
            {
                while (true)
                {
                    var request = __WritterCurrentRequest;
                    if (request != null) return request;
                    // Na Stop zde nereagujeme (dříve: if (IsStoped) return null;): Writter by skončil dřív, než zapíše poslední blok a zavře stream.
                    //  Thread Work posílá EndThread vždy ve finally metody Run(), a thread je IsBackground.
                    __WrittingSemaphoreStart.WaitOne(250);
                }
            }
            // Otevře zápis do souboru skrze __WrittingStream
            void doOpenStream(WritterRequestInfo openRequest)
            {
                __WrittingStreamAbort = false;
                try
                {
                    __WrittingStream = System.IO.File.Open(openRequest.WriteFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);        // System.IO.File.OpenWrite(targetFile))
                    __WrittingStreamUnflushedBytes = 0;
                }
                catch (Exception e)
                {
                    __WrittingStreamAbort = true;
                    __WrittingStream = null;
                }
            }
            // Vepíše do __WrittingStream blok s daty
            void doWriteBlock(WritterRequestInfo writeRequest)
            {
                if (__WrittingStreamAbort) return;
                
                var writeBlock = writeRequest.WriteBlock;
                long timeBegin = 0L;
                try
                {
                    timeBegin = this.CurrentTime;
                    __WrittingStream.Seek(writeBlock.BlockStart, SeekOrigin.Begin);
                    __WrittingStream.Write(writeBlock.Content, 0, writeBlock.Content.Length);
                    writeBlock.StoreWriteTime(this.GetSeconds(timeBegin), false);
                    writeBlock.StoreBlockResultRead(writeRequest.ResultAfterWrite);        // Teprve nyní je blok skutečně zapsán => vepíšeme výsledek čtení (Success / Error)
                    __WrittingStreamUnflushedBytes += writeBlock.Content.Length;
                    if (__WrittingStreamUnflushedBytes >= 1048576L)
                    {
                        __WrittingStream.Flush();
                        __WrittingStreamUnflushedBytes = 0;
                    }
                }
                catch (Exception e)
                {
                    writeBlock.StoreWriteTime(this.GetSeconds(timeBegin), true);
                    doCloseStream(writeRequest);                               // Tady se zavře stream, ale neměníme __WrittingStreamAbort
                    __WrittingStreamAbort = true;                              // Ale my jsme našli chybu při Write, tak musíme oznámit, že v souboru je chyba
                }
                finally
                {
                    writeBlock.Content = null;
                }
            }
            // Ukončí zápis do __WrittingStream
            void doCloseStream(WritterRequestInfo closeRequest)
            {
                if (__WrittingStream != null)
                {
                    try
                    {
                        __WrittingStream.Flush();
                        __WrittingStream.Close();
                        __WrittingStream.Dispose();
                    }
                    catch (Exception e)
                    { }
                    __WrittingStream = null;
                }

                // Pokud zavíráme stream na explicitní požadavek, pak zrušíme červenou vlajku __WrittingStreamAbort 
                if (closeRequest != null && closeRequest.RequestType == WritterRequestType.CloseWritterStream)
                    __WrittingStreamAbort = false;
            }
        }
        /// <summary>
        /// Instance threadu Writter
        /// </summary>
        private Thread __WrittingThread;
        /// <summary>
        /// Stream pro zápis do cílového souboru, který je otevřený a zapisuje se do něj (pracuje se s ním) z threadu Writter
        /// </summary>
        private FileStream __WrittingStream;
        /// <summary>
        /// Počet byte, které nejsou fyzicky zapsané (Flush) do __WrittingStream
        /// </summary>
        private int __WrittingStreamUnflushedBytes;
        /// <summary>
        /// Aktuální WriteStream má chybu a je nepoužitelný!<br/>
        /// Bude nastaveno na true po chybě ve Write streamu.<br/>
        /// Procedura kopírování jednoho souboru, jakmile detekuje zdejší true, tak zajistí ukončení práce na jednom souboru, <br/>
        /// protože <see cref="__WrittingThread"/> od této chvíle nebude akceptovat další požadavky na zápis do streamu.<br/>
        /// Stream je po chybě zavřen a zahozen.
        /// </summary>
        private bool __WrittingStreamAbort;
        /// <summary>
        /// Nově požadovaný request - který byl právě doručen, null když nic neděláme...
        /// </summary>
        private WritterRequestInfo __WritterCurrentRequest;
        /// <summary>
        /// Aktuálně zpracovávaný request, null když nic neděláme...
        /// </summary>
        private WritterRequestInfo __WritterProcessedRequest;
        /// <summary>
        /// Zapisovací thread je ve stavu, kdy ukončuje svůj thread a již nepřijímá další požadavky.
        /// </summary>
        private bool __WritterIsEndingThread;
        /// <summary>
        /// Semafor, který se rozsvítí z threadu Work do threadu Writter tehdy, když přijde nová práce.
        /// </summary>
        private System.Threading.AutoResetEvent __WrittingSemaphoreStart;
        /// <summary>
        /// Semafor, který se rozsvítí z threadu Writter do threadu Work tehdy, když Writter dokončil práci a může přijmout nová data.
        /// U tohoto semaforu čeká thread Work s novým Requestem, než se uvolní thread Writter, který pak může zpracovat tento Request.
        /// </summary>
        private System.Threading.AutoResetEvent __WrittingSemaphoreDone;
        #endregion
        #region class SingleFileInfo : Jeden kopírovaný soubor, jeho metadata a proměnné ovliňující kopírování
        /// <summary>
        /// Třída obsahující základní data o jednom souboru k záchraně, včetně parametrů pro kopírování a výsledku operace.
        /// </summary>
        internal class SingleFileInfo
        {
            /// <summary>
            /// Konstruktor, nastaví defaultní hodnoty parametrů pro kopírování a inicializuje stav výsledku.
            /// </summary>
            public SingleFileInfo()
            {
                FastCopyBlockSize = CommonSegmentLengthHdd;
                SkipOnError = 512;
                AbortAfterContinueErrors = 1024;
                Success = false;
                ErrorMessage = null;
                SourceFileStatus = FileStatus.None;
            }
            /// <summary>
            /// Vizualizace objektu, vrací název zdrojového souboru.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.SourceFile;
            }
            /// <summary>
            /// Vstupní zdrojový soubor k záchraně.
            /// </summary>
            public string SourceFile { get; set; }
            /// <summary>
            /// Info o zdrojovém souboru.
            /// </summary>
            public FileInfo SourceFileInfo { get; private set; }
            /// <summary>
            /// Délka vstupního souboru <see cref="SourceFile"/>, pokud existuje, jinak null.
            /// </summary>
            public long? SourceFileLength { get { return (SourceFileInfo != null && SourceFileInfo.Exists) ? (long?)SourceFileInfo.Length : (long?)null; } }
            /// <summary>
            /// Stav práce na souboru.
            /// </summary>
            public FileStatus SourceFileStatus { get; set; }
            /// <summary>
            /// Cílový soubor pro uložení toho, co lze uložit
            /// </summary>
            public string DestinationFile { get; set; }
            /// <summary>
            /// Cílový soubor pro uložení logu záchrany.
            /// </summary>
            public string DestinationLog { get; set; }
            /// <summary>
            /// Velikost výchozího bloku
            /// <para/>
            /// Doporučení od AI<br/>
            /// •	HDD (sequential): 64 KB – 1 MB, doporučené výchozí: 256 KB<br/>
            /// •	SSD (sequential): 64 KB – 1 MB, doporučené výchozí: 256 KB<br/>
            /// •	SD karta přes USB: 32 KB – 256 KB, doporučené výchozí: 64 KB<br/>
            /// Krátké vysvětlení a tipy<br/>
            /// •	Základní pravidlo: používejte násobky fyzického sektoru (obvykle 4096 B nebo 512 B). To zajišťuje lepší zarovnání a výkon, zvlášť při NoBuffering/unbuffered I/O.<br/>
            /// •	Velké sekvenční přenosy: větší buffery snižují režii (méně syscalů) a často zvýší propustnost. Ale nad určitou velikost (řád MB) už nárůst výkonu zpravidla stagnuje.<br/>
            /// •	Náhodný přístup: menší bloky (4 KB–64 KB) jsou obvykle lepší, protože velké bloky způsobí zbytečné čtení/zápis.<br/>
            /// •	SD přes USB: záleží na USB–SD bridge a kartě; mnoho řadičů má lepší výkon při ~64 KB blokách.<br/>
            /// •	.NET tipy: při velkým souborech používejte FileOptions.SequentialScan a async I/O (ReadAsync/WriteAsync) nebo FileStream s dostatečným bufferem. Pokud použijete unbuffered/WriteThrough, dodržujte zarovnání bufferů na sektor a velikost v násobcích sektoru.<br/>
            /// •	Měřit vždy: otestujte několik hodnot (např. 64 KB, 256 KB, 1 MB) a změřte průtok a CPU — výsledky se liší podle hardware/firmware/USB řadiče.<br/>
            /// Krátké doporučení k testování<br/>
            /// •	Vyzkoušejte 64 KB, 256 KB, 1 MB; měřte MB/s a CPU. Vyberte nejmenší buffer, který dává maximální/saturovaný propustnost bez neúměrného CPU použití.
            /// </summary>
            public int FastCopyBlockSize { get; set; }
            public int SkipOnError { get; set; }
            public int AbortAfterContinueErrors { get; set; }

            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            /// <summary>
            /// Simulované chyby: Item1 = byte s chybou; Item2 = na kolikátý pokus bude správně bezchybně přečtena (0 = není chyba, ... 99 = nikdy)
            /// </summary>
            public Tuple<long, int>[] SimulatedErrors { get; set; }
            /// <summary>
            /// Simulované zdržení po každém bloku, v milisekundách
            /// </summary>
            public int? SimulatedWaiting { get; set; }
            /// <summary>
            /// Test: čtení bloku, který obsahuje tuto pozici, zamrzne na neomezeně dlouho (simulace zamrzlé karty). Null = bez simulace.
            /// </summary>
            public long? SimulatedHangPosition { get; set; }
            /// <summary>
            /// Aktuálně načtený LOG
            /// </summary>
            public SingleLogInfo CurrentLog { get; private set; }
            /// <summary>
            /// Konečná statistika souboru, uložená po ukončení jeho zpracování. Null = soubor dosud nebyl zpracován (nebo se zpracovává).
            /// </summary>
            public StatisticInfo FinalStatistic { get; set; }
            /// <summary>
            /// Soubor byl v tomto běhu odložen pro vysokou chybovost čtení (nečitelný soubor). Zpracuje se až ve 2. vlně, pokud ji uživatel potvrdí.
            /// </summary>
            public bool IsDeferred { get; set; }
            /// <summary>
            /// Zkontroluje existenci zdrojového souboru a nastaví <see cref="SourceFileStatus"/> na <see cref="FileStatus.SourceFileExists"/> nebo <see cref="FileStatus.NotExists"/>.
            /// </summary>
            public void CheckFile()
            {
                SourceFileInfo = new FileInfo(SourceFile);
                SourceFileStatus = (SourceFileInfo.Exists) ? FileStatus.SourceFileExists : FileStatus.NotExists;
            }
            /// <summary>
            /// Zajistí vytvoření a načtení obsahu logu o souboru
            /// </summary>
            public void LoadLog()
            {
                CurrentLog = new SingleLogInfo(this);
                SourceFileStatus = CurrentLog.DestinationLogExists ? FileStatus.LogLoaded : FileStatus.LogPrepared;
            }
            /// <summary>
            /// Komparátor pro řazení podle názvu zdrojového souboru, ignoruje velikost písmen.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public static int CompareByFullFileName(SingleFileInfo x, SingleFileInfo y)
            {
                if (x is null && y is null) return 0;
                if (x is null) return -1;
                if (y is null) return 1;
                return string.Compare(x.SourceFile, y.SourceFile, StringComparison.CurrentCultureIgnoreCase);          // Ignoruje velikost písmen, akceptuje aktuální jazyk systému (čeština)
            }
        }
        #endregion
        #region class SingleBlockInfo : Data o jednom kopírovaném bloku souboru, včetně jeho stavu a počtu pokusů o znovunačtení.
        /// <summary>
        /// Data o jednom kopírovaném bloku souboru, včetně jeho stavu a počtu pokusů o znovunačtení.
        /// </summary>
        internal class SingleBlockInfo : ILinearMapControlItem
        {
            #region Konstruktor
            /// <summary>
            /// Konstruktor
            /// </summary>
            public SingleBlockInfo()
            {
                this.Status = BlockStatus.None;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="blockStart"></param>
            /// <param name="blockLength"></param>
            public SingleBlockInfo(long blockStart, int blockLength)
            {
                this.Status = BlockStatus.None;
                this.BlockStart = blockStart;
                this.BlockLength = blockLength;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Start: {BlockStart:N0}; Length: {BlockLength:N0}";
            }
            #endregion
            #region Data bloku: Start, Length, End
            /// <summary>
            /// Adresa začátku bloku v souboru, offset od začátku souboru.
            /// </summary>
            public long BlockStart { get; set; }
            /// <summary>
            /// Délka bloku. Pokud je blok OK, pak má standardní délku <see cref="SingleFileInfo.FastCopyBlockSize"/>, pokud je blok chybový, pak může být kratší.
            /// </summary>
            public int BlockLength { get; set; }
            /// <summary>
            /// Adresa prvního byte za tímto blokem = <see cref="BlockStart"/> + <see cref="BlockLength"/>.
            /// </summary>
            public long BlockEnd { get { return this.BlockStart + this.BlockLength; } }
            #endregion
            #region Byte[] Content
            /// <summary>
            /// Binární obsah bloku: je naplněn po načtení, použit pro zápis, a po zápisu je nulován.
            /// </summary>
            public byte[] Content { get; set; }
            /// <summary>
            /// Reálně načtená délka dat, přičemž požadovaná délka byla = <see cref="BlockLength"/>
            /// </summary>
            public int ContentLength { get; set; }
            #endregion
            #region Chyby: počet, stav, příznak
            /// <summary>
            /// Počet chyb při pokusech o čtení.
            /// <para/>
            /// Při vytvoření nového bloku je zde 0.<br/>
            /// Pokud při kopírování dojde k chybě, zdejší hodnota se navýší o 1.<br/>
            /// Následně se pokusíme blok zkopírovat opakovaně (nikoli ihned po sobě), a při dalších chybách tuto hodnotu navyšujeme o +1.<br/>
            /// </summary>
            public int ErrorsCount { get; set; }
            /// <summary>
            /// Stav bloku, zda je OK, nebo obsahuje chyby. Pokud je blok OK, pak je <see cref="ErrorsCount"/> == 0, pokud je blok chybový, pak je <see cref="ErrorsCount"/> větší než 0.
            /// </summary>
            public BlockStatus Status { get; set; }
            /// <summary>
            /// Obsahuje true, pokud <see cref="Status"/> říká, že blok je nějakým způsobem dokončen (OK, OK po chybě, Chyba a Abort)
            /// </summary>
            public bool IsDone { get { var st = this.Status; return (st == SingleBlockInfo.BlockStatus.OK || st == SingleBlockInfo.BlockStatus.OKAfterErrors || st == SingleBlockInfo.BlockStatus.ErrorAborted); } }
            /// <summary>
            /// Obsahuje true, pokud blok nemá platná data: <see cref="BlockStatus.WithError"/> nebo <see cref="BlockStatus.ErrorAborted"/>.
            /// Blok <see cref="BlockStatus.OKAfterErrors"/> se nakonec podařilo přečíst, takže chybový není.
            /// </summary>
            public bool IsErrorBlock { get { var st = this.Status; return (st == SingleBlockInfo.BlockStatus.WithError || st == SingleBlockInfo.BlockStatus.ErrorAborted); } }
            /// <summary>
            /// Uloží do daného bloku výsledný stav po dokončení jeho čtení
            /// </summary>
            /// <param name="block"></param>
            /// <param name="result"></param>
            public void StoreBlockResultRead(CopyBlockResult result)
            {
                switch (result)
                {
                    case CopyBlockResult.None:
                        break;
                    case CopyBlockResult.Empty:
                        this.Status = SingleBlockInfo.BlockStatus.OK;
                        break;
                    case CopyBlockResult.Success:
                        this.Status = (this.ErrorsCount == 0 ? SingleBlockInfo.BlockStatus.OK : SingleBlockInfo.BlockStatus.OKAfterErrors);
                        break;
                    case CopyBlockResult.Error:
                        // Skončili jsme s chybou: 
                        this.ErrorsCount++;
                        this.Status = (this.ErrorsCount < SingleLogInfo.MaxErrorsCount ? SingleBlockInfo.BlockStatus.WithError : SingleBlockInfo.BlockStatus.ErrorAborted);
                        break;
                }
            }
            /// <summary>
            /// Stav bloku
            /// </summary>
            public enum BlockStatus
            {
                /// <summary>
                /// Blok je v neznámém stavu, nebyl dosud zpracován.
                /// Počitadlo <see cref="SingleBlockInfo.ErrorsCount"/> je 0.
                /// </summary>
                None,
                /// <summary>
                /// Blok byl úspěšně zkopírován, bez chyb, je OK.
                /// Počitadlo <see cref="SingleBlockInfo.ErrorsCount"/> je 0.
                /// </summary>
                OK,
                /// <summary>
                /// Blok obsahuje chyby, ale po několika pokusech byl úspěšně zkopírován a záchrana pokračuje na dalším bloku.
                /// Počitadlo <see cref="SingleBlockInfo.ErrorsCount"/> obsahuje počet pokusů. které skončily chybou, před tím než se jej podařilo zkopírovat.
                /// </summary>
                OKAfterErrors,
                /// <summary>
                /// Blok hlásí chyby: dosud nebyl úspěšně zkopírován. Ještě proběhne další pokus.
                /// Počitadlo <see cref="SingleBlockInfo.ErrorsCount"/> obsahuje počet pokusů, které skončily chybou.
                /// </summary>
                WithError,
                /// <summary>
                /// Blok obsahuje chyby, a další pokusy o záchranu byly zrušeny. Blok byl přeskočen a záchrana pokračuje na dalším bloku.
                /// Počitadlo <see cref="SingleBlockInfo.ErrorsCount"/> obsahuje počet pokusů. které skončily chybou. Po posledním z nich byl blok trvale abortován.
                /// </summary>
                ErrorAborted,
            }
            #endregion
            #region Časomíra
            /// <summary>
            /// Uloží si čas čtení bloku
            /// </summary>
            /// <param name="seconds"></param>
            /// <param name="isError"></param>
            public void StoreReadTime(decimal seconds, bool isError)
            {
                this.ReadTimeSeconds = seconds;
            }
            public decimal? ReadTimeSeconds { get; private set; }
            public void StoreWriteTime(decimal seconds, bool isError)
            {
                this.WriteTimeSeconds = seconds;
            }
            public decimal? WriteTimeSeconds { get; private set; }
            /// <summary>
            /// Rychlost čtení v okolí bloku v MB/s (propustnost okna <see cref="SpeedShading.WindowBlocks"/> bloků).
            /// Počítá ji <see cref="SpeedShading.Apply"/> v GUI threadu; do logu se neukládá. Null = nelze určit.
            /// </summary>
            public double? SpeedMBs { get; set; }
            /// <summary>
            /// Index odstínu barvy podle rychlosti (0 = nejpomalejší) pro blok v kategorii <see cref="BlockCategory.Ok"/>.
            /// Nastavuje <see cref="SpeedShading.Apply"/>. Null = použije se základní barva <see cref="Skin.RescueBlockOkColor"/>.
            /// </summary>
            public int? SpeedShade { get; set; }
            #endregion
            #region ILinearMapControlItem
            /// <summary>
            /// Počátek dat v tomto prvku; pokud bude null pak počátek = konec předešlého
            /// </summary>
            long? ILinearMapControlItem.Start { get { return this.BlockStart; } }
            /// <summary>
            /// Délka dat v tomto prvku
            /// </summary>
            long ILinearMapControlItem.Length { get { return this.BlockLength; } }
            /// <summary>
            /// Barva prvku
            /// </summary>
            Color? ILinearMapControlItem.Color { get { return GetColor(); } }
            /// <summary>
            /// Vrátí barvu tohoto bloku: podle kategorie, a OK blok s určeným <see cref="SpeedShade"/> odstínem podle rychlosti.
            /// </summary>
            /// <returns></returns>
            public Color GetColor()
            {
                var category = GetCategory(this.Status, this.ErrorsCount);
                var shade = this.SpeedShade;
                if (category == BlockCategory.Ok && shade.HasValue)
                    return SpeedShading.GetShadeColor(shade.Value);
                return GetColor(category);
            }
            /// <summary>
            /// Text do ToolTipu
            /// </summary>
            string ILinearMapControlItem.Text { get { return GetToolTipText(); } }
            /// <summary>
            /// Vrátí kategorii bloku pro zobrazení (barva, souhrn) podle jeho stavu a počtu chyb
            /// </summary>
            /// <param name="status"></param>
            /// <param name="errorsCount"></param>
            /// <returns></returns>
            public static BlockCategory GetCategory(BlockStatus status, int errorsCount)
            {
                switch (status)
                {
                    case BlockStatus.OK: return BlockCategory.Ok;
                    case BlockStatus.OKAfterErrors: return BlockCategory.OkAfterErrors;
                    case BlockStatus.WithError: return (errorsCount <= 1 ? BlockCategory.Error1 : BlockCategory.Error2);
                    case BlockStatus.ErrorAborted: return BlockCategory.Aborted;
                    default: return BlockCategory.Reading;
                }
            }
            /// <summary>
            /// Vrátí barvu bloku podle jeho stavu a počtu chyb. Barvy jsou definovány ve <see cref="Skin"/>.
            /// </summary>
            /// <param name="status"></param>
            /// <param name="errorsCount"></param>
            /// <returns></returns>
            public static Color GetColor(BlockStatus status, int errorsCount)
            {
                return GetColor(GetCategory(status, errorsCount));
            }
            /// <summary>
            /// Vrátí barvu pro danou kategorii bloku. Barvy jsou definovány ve <see cref="Skin"/>.
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public static Color GetColor(BlockCategory category)
            {
                switch (category)
                {
                    case BlockCategory.Ok: return Skin.RescueBlockOkColor;
                    case BlockCategory.OkAfterErrors: return Skin.RescueBlockOkAfterErrorsColor;
                    case BlockCategory.Error1: return Skin.RescueBlockError1Color;
                    case BlockCategory.Error2: return Skin.RescueBlockError2Color;
                    case BlockCategory.Aborted: return Skin.RescueBlockAbortedColor;
                    default: return Skin.RescueBlockReadingColor;
                }
            }
            /// <summary>
            /// Vrátí uživatelský název kategorie bloku (pro souhrn a legendu)
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public static string GetCategoryText(BlockCategory category)
            {
                switch (category)
                {
                    case BlockCategory.Ok: return "Přečteno";
                    case BlockCategory.OkAfterErrors: return "Přečteno po opakování";
                    case BlockCategory.Error1: return "Chyba (1×)";
                    case BlockCategory.Error2: return "Chyba (2× a více)";
                    case BlockCategory.Aborted: return "Nečitelné, vzdáno";
                    default: return "Čte se";
                }
            }
            /// <summary>
            /// Kategorie bloku pro zobrazení: odpovídá barvám ve <see cref="Skin"/>
            /// </summary>
            public enum BlockCategory
            {
                /// <summary>Blok vytvořený, právě se čte</summary>
                Reading,
                /// <summary>Přečteno napoprvé</summary>
                Ok,
                /// <summary>Přečteno až po opakování</summary>
                OkAfterErrors,
                /// <summary>1 chyba, čeká na další pokus</summary>
                Error1,
                /// <summary>2 a více chyb, čeká na další pokus</summary>
                Error2,
                /// <summary>Vzdáno po maximálním počtu chyb</summary>
                Aborted
            }
            /// <summary>
            /// Vrátí text do ToolTipu: pozice, délka, stav, počet chyb a rychlost čtení v okolí bloku
            /// </summary>
            /// <returns></returns>
            public string GetToolTipText()
            {
                var text = new StringBuilder();
                text.AppendLine($"Pozice: {BlockStart:N0} ÷ {BlockEnd:N0}");
                text.AppendLine($"Délka: {BlockLength:N0} B");
                text.AppendLine($"Stav: {GetStatusText(this.Status)}");
                if (this.ErrorsCount > 0)
                    text.AppendLine($"Počet chyb: {ErrorsCount} z {SingleLogInfo.MaxErrorsCount}");
                // Časy čtení a zápisu jednotlivého bloku se nezobrazují: samostatně nemají vypovídací hodnotu (fyzické čtení 256 kB, cache), nahrazuje je rychlost v okolí.
                var speed = this.SpeedMBs;
                if (speed.HasValue && (this.Status == BlockStatus.OK || this.Status == BlockStatus.OKAfterErrors))
                    text.AppendLine($"Rychlost čtení v okolí: {SpeedShading.FormatSpeed(speed.Value)} MB/s");
                return text.ToString().TrimEnd();
            }
            /// <summary>
            /// Vrátí uživatelský název stavu bloku
            /// </summary>
            /// <param name="status"></param>
            /// <returns></returns>
            public static string GetStatusText(BlockStatus status)
            {
                switch (status)
                {
                    case BlockStatus.OK: return "Přečteno";
                    case BlockStatus.OKAfterErrors: return "Přečteno po opakování";
                    case BlockStatus.WithError: return "Chyba, čeká na další pokus";
                    case BlockStatus.ErrorAborted: return "Nečitelný, vzdáno";
                    default: return "Čte se";
                }
            }
            #endregion
            #region Serializace a deserializace do logového souboru
            /// <summary>
            /// Vrátí řádek s textem titulku bloků = obsah sloupců
            /// </summary>
            /// <returns></returns>
            public static string GetTitleToLog()
            {
                var d = DELIMITER_ITEMS;
                return $"## BlockStart{d}BlockLength{d}ErrorsCount{d}Status{d}ReadTime Microsecs{d}WriteTime Microsecs";
            }
            /// <summary>
            /// Vrátí řádek, který lze uložit do logového souboru.
            /// Reverzní metoda je <see cref="FromLogLine(string)"/>.
            /// </summary>
            /// <returns></returns>
            public string GetLineToLog()
            {
                var d = DELIMITER_ITEMS;
                return $"{BlockStart}{d}{BlockLength}{d}{ErrorsCount}{d}{Status}{d}{getMicroSecs(ReadTimeSeconds)}{d}{getMicroSecs(WriteTimeSeconds)}";

                string getMicroSecs(decimal? seconds)
                {
                    if (seconds.HasValue)
                        return ((long)(Math.Round(seconds.Value * 1000000m, 0))).ToString();
                    return "";
                }
            }
            /// <summary>
            /// Vytvoří nový blok z dat v dodané řádce. Anebo vrátí null.
            /// Zdejší data do řádku formátuje reverzní metoda <see cref="GetLineToLog()"/>.
            /// </summary>
            /// <param name="line"></param>
            /// <returns></returns>
            public static SingleBlockInfo FromLogLine(string line)
            {
                if (String.IsNullOrWhiteSpace(line)) return null;
                var d = DELIMITER_ITEMS;
                var parts = line.Split(new string[] { d }, StringSplitOptions.None);
                var count = parts.Length;

                long? blockStart = null;
                int? blockLength = null;
                int? reReadCount = null;
                BlockStatus? status = null;
                long? readTimeMicrosecs = null;
                long? writeTimeMicrosecs = null;

                if (count >= 1 && Int64.TryParse(parts[0], out long start) && start >= 0L) blockStart = start;
                if (count >= 2 && Int32.TryParse(parts[1], out int length) && length >= 0) blockLength = length;
                if (count >= 3 && Int32.TryParse(parts[2], out int rrCnt) && rrCnt >= 0) reReadCount = rrCnt;
                if (count >= 4 && Enum.TryParse<BlockStatus>(parts[3], out var stt)) status = stt;
                if (count >= 5 && Int64.TryParse(parts[4], out long rtim) && rtim >= 0L) readTimeMicrosecs = rtim;
                if (count >= 6 && Int64.TryParse(parts[5], out long wtim) && wtim >= 0L) writeTimeMicrosecs = wtim;

                var hasData = blockStart.HasValue && blockLength.HasValue && reReadCount.HasValue && status.HasValue;
                if (!hasData) return null;

                return new SingleBlockInfo()
                {
                    BlockStart = blockStart.Value,
                    BlockLength = blockLength.Value,
                    ErrorsCount = reReadCount.Value,
                    Status = status.Value,
                    ReadTimeSeconds = getSeconds(readTimeMicrosecs),
                    WriteTimeSeconds = getSeconds(writeTimeMicrosecs)
                };

                decimal? getSeconds(long? microsecs)
                {
                    return (microsecs.HasValue ? ((decimal)microsecs.Value / 1000000m) : (decimal?)null);
                }
            }
            private const string DELIMITER_ITEMS = ";";
            #endregion
        }
        #endregion
        #region class SpeedShading : Barvení OK bloků v mapě odstíny podle rychlosti čtení
        /// <summary>
        /// Barvení OK bloků v mapě odstíny podle rychlosti čtení.
        /// <para/>
        /// Metrika: propustnost okna <see cref="WindowBlocks"/> sousedních bloků = Σ bajtů / Σ času čtení (ne průměr časů).
        /// Čas jednotlivého bloku samostatně smysl nemá: karta fyzicky čte 256 kB, takže bloky se střídají "dlouhý čas" a "desítky µs z cache".
        /// <para/>
        /// Rozsah je adaptivní pro aktuální soubor: percentily <see cref="LowPercentile"/> ÷ <see cref="HighPercentile"/> z histogramu log10(rychlost),
        /// minimální šíře <see cref="MinRangeDecades"/> dekády kolem mediánu. Škála je logaritmická a kvantovaná na <see cref="ShadesCount"/> odstínů, pomalé = tmavé.
        /// <para/>
        /// Výpočet probíhá v GUI threadu při refreshi mapy, výsledek se ukládá do bloků (<see cref="SingleBlockInfo.SpeedMBs"/>, <see cref="SingleBlockInfo.SpeedShade"/>), log se nemění.
        /// </summary>
        internal static class SpeedShading
        {
            #region Konstanty
            /// <summary>
            /// Počet bloků v okně pro výpočet rychlosti (16 × 128 kB = 2 MB), okno je centrované na blok
            /// </summary>
            public const int WindowBlocks = 16;
            /// <summary>
            /// Minimální počet bloků s časem čtení v okně, jinak se rychlost neurčí
            /// </summary>
            public const int WindowMinBlocks = 4;
            /// <summary>
            /// Počet odstínů barvy
            /// </summary>
            public const int ShadesCount = 7;
            /// <summary>
            /// Minimální počet bloků s rychlostí, aby se určoval rozsah a barvilo
            /// </summary>
            public const int MinSamples = 32;
            /// <summary>
            /// Dolní percentil rozsahu (robustní vůči výkyvům)
            /// </summary>
            public const double LowPercentile = 0.02d;
            /// <summary>
            /// Horní percentil rozsahu (robustní vůči výkyvům)
            /// </summary>
            public const double HighPercentile = 0.98d;
            /// <summary>
            /// Minimální šíře rozsahu v dekádách (1 = ×10), aby při stálé rychlosti nevznikal falešný kontrast
            /// </summary>
            public const double MinRangeDecades = 1.0d;
            /// <summary>
            /// Histogram log10(MB/s): dolní mez (0,01 MB/s)
            /// </summary>
            private const double HistogramLogMin = -2.0d;
            /// <summary>
            /// Histogram log10(MB/s): horní mez (10 GB/s)
            /// </summary>
            private const double HistogramLogMax = 4.0d;
            /// <summary>
            /// Histogram: počet košů (0,05 dekády na koš)
            /// </summary>
            private const int HistogramBins = 120;
            private const double BytesPerMB = 1024d * 1024d;
            #endregion
            #region Výpočet
            /// <summary>
            /// Pro dané bloky (jednoho souboru) spočítá rychlost v okolí každého přečteného bloku a adaptivní rozsah,
            /// a pokud je <paramref name="shadingEnabled"/>, určí odstíny OK bloků. Jinak odstíny nuluje (bloky mají základní barvu).
            /// Volá se z GUI threadu. Pro null nebo prázdné pole vrací neplatný rozsah.
            /// </summary>
            /// <param name="blocks"></param>
            /// <param name="shadingEnabled"></param>
            /// <returns></returns>
            public static SpeedRange Apply(IEnumerable<SingleBlockInfo> blocks, bool shadingEnabled)
            {
                var range = new SpeedRange() { Enabled = shadingEnabled };
                if (blocks is null) return range;

                // Thread Work mezitím může měnit stav a časy bloků => každý blok čtu jedenkrát do lokálních hodnot:
                var sorted = blocks.Where(b => b != null).OrderBy(b => b.BlockStart).ToArray();
                int count = sorted.Length;
                if (count == 0) return range;

                var isRead = new bool[count];
                var prefixBytes = new double[count + 1];
                var prefixTime = new double[count + 1];
                var prefixCount = new int[count + 1];
                for (int i = 0; i < count; i++)
                {
                    var block = sorted[i];
                    var status = block.Status;
                    var readTime = block.ReadTimeSeconds;
                    bool read = ((status == SingleBlockInfo.BlockStatus.OK || status == SingleBlockInfo.BlockStatus.OKAfterErrors) && readTime.HasValue && block.BlockLength > 0);
                    isRead[i] = read;
                    prefixBytes[i + 1] = prefixBytes[i] + (read ? (double)block.BlockLength : 0d);
                    prefixTime[i + 1] = prefixTime[i] + (read ? (double)readTime.Value : 0d);
                    prefixCount[i + 1] = prefixCount[i] + (read ? 1 : 0);
                }

                // Rychlost okna pro každý přečtený blok + histogram log10:
                var speeds = new double?[count];
                var histogram = new int[HistogramBins];
                int samples = 0;
                int half = WindowBlocks / 2;
                for (int i = 0; i < count; i++)
                {
                    if (!isRead[i]) continue;
                    int lo = i - half;
                    int hi = i + half;                                       // hi je exkluzivní
                    if (lo < 0) { hi -= lo; lo = 0; }
                    if (hi > count) { lo -= (hi - count); hi = count; if (lo < 0) lo = 0; }
                    int n = prefixCount[hi] - prefixCount[lo];
                    double time = prefixTime[hi] - prefixTime[lo];
                    if (n < WindowMinBlocks || time <= 0d) continue;
                    double speed = (prefixBytes[hi] - prefixBytes[lo]) / time / BytesPerMB;
                    speeds[i] = speed;
                    histogram[GetBin(speed)]++;
                    samples++;
                }

                // Adaptivní rozsah:
                bool hasRange = (samples >= MinSamples);
                double logLo = 0d, logHi = 0d;
                if (hasRange)
                {
                    logLo = GetPercentileLog(histogram, samples, LowPercentile);
                    logHi = GetPercentileLog(histogram, samples, HighPercentile);
                    if ((logHi - logLo) < MinRangeDecades)
                    {   // Minimální šíře kolem mediánu, ale tak, aby rozsah pokryl percentily:
                        double median = GetPercentileLog(histogram, samples, 0.5d);
                        double newLo = median - MinRangeDecades / 2d;
                        double newHi = median + MinRangeDecades / 2d;
                        if (logLo < newLo) { newLo = logLo; newHi = logLo + MinRangeDecades; }
                        else if (logHi > newHi) { newHi = logHi; newLo = logHi - MinRangeDecades; }
                        logLo = newLo;
                        logHi = newHi;
                    }
                    range.IsValid = true;
                    range.LowMBs = Math.Pow(10d, logLo);
                    range.HighMBs = Math.Pow(10d, logHi);
                }

                // Uložení do bloků:
                bool shade = (shadingEnabled && hasRange);
                for (int i = 0; i < count; i++)
                {
                    var block = sorted[i];
                    var speed = speeds[i];
                    block.SpeedMBs = speed;
                    block.SpeedShade = ((shade && speed.HasValue) ? GetShade(speed.Value, logLo, logHi) : (int?)null);
                }
                return range;
            }
            /// <summary>
            /// Vrátí index odstínu 0 ÷ <see cref="ShadesCount"/>-1 pro danou rychlost v logaritmické škále mezi danými mezemi (log10)
            /// </summary>
            /// <param name="speedMBs"></param>
            /// <param name="logLo"></param>
            /// <param name="logHi"></param>
            /// <returns></returns>
            private static int GetShade(double speedMBs, double logLo, double logHi)
            {
                double t = (GetLog(speedMBs) - logLo) / (logHi - logLo);
                int shade = (int)Math.Floor(t * ShadesCount);
                return (shade < 0 ? 0 : (shade >= ShadesCount ? ShadesCount - 1 : shade));
            }
            /// <summary>
            /// Vrátí log10 rychlosti, oříznutý do mezí histogramu
            /// </summary>
            /// <param name="speedMBs"></param>
            /// <returns></returns>
            private static double GetLog(double speedMBs)
            {
                double log = (speedMBs > 0d ? Math.Log10(speedMBs) : HistogramLogMin);
                return (log < HistogramLogMin ? HistogramLogMin : (log > HistogramLogMax ? HistogramLogMax : log));
            }
            /// <summary>
            /// Vrátí index koše histogramu pro danou rychlost
            /// </summary>
            /// <param name="speedMBs"></param>
            /// <returns></returns>
            private static int GetBin(double speedMBs)
            {
                int bin = (int)Math.Floor((GetLog(speedMBs) - HistogramLogMin) / (HistogramLogMax - HistogramLogMin) * HistogramBins);
                return (bin < 0 ? 0 : (bin >= HistogramBins ? HistogramBins - 1 : bin));
            }
            /// <summary>
            /// Vrátí hodnotu log10 (střed koše) pro daný percentil z histogramu
            /// </summary>
            /// <param name="histogram"></param>
            /// <param name="samples"></param>
            /// <param name="percentile"></param>
            /// <returns></returns>
            private static double GetPercentileLog(int[] histogram, int samples, double percentile)
            {
                double target = percentile * samples;
                int cumulative = 0;
                int bin = histogram.Length - 1;
                for (int i = 0; i < histogram.Length; i++)
                {
                    cumulative += histogram[i];
                    if (cumulative >= target && cumulative > 0) { bin = i; break; }
                }
                double binWidth = (HistogramLogMax - HistogramLogMin) / HistogramBins;
                return HistogramLogMin + (bin + 0.5d) * binWidth;
            }
            #endregion
            #region Barvy a formátování
            /// <summary>
            /// Vrátí barvu daného odstínu: lineární interpolace mezi <see cref="Skin.RescueBlockOkSlowColor"/> (0) a <see cref="Skin.RescueBlockOkFastColor"/> (<see cref="ShadesCount"/>-1)
            /// </summary>
            /// <param name="shade"></param>
            /// <returns></returns>
            public static Color GetShadeColor(int shade)
            {
                var slow = Skin.RescueBlockOkSlowColor;
                var fast = Skin.RescueBlockOkFastColor;
                double t = (ShadesCount <= 1 ? 1d : (double)shade / (double)(ShadesCount - 1));
                t = (t < 0d ? 0d : (t > 1d ? 1d : t));
                return Color.FromArgb(lerp(slow.R, fast.R), lerp(slow.G, fast.G), lerp(slow.B, fast.B));

                int lerp(int a, int b) { return (int)Math.Round(a + (b - a) * t, 0); }
            }
            /// <summary>
            /// Vrátí rychlost v MB/s jako text: pod 1 na 2 desetinná místa, pod 10 na 1 desetinné místo, jinak celé číslo
            /// </summary>
            /// <param name="speedMBs"></param>
            /// <returns></returns>
            public static string FormatSpeed(double speedMBs)
            {
                if (speedMBs < 1d) return speedMBs.ToString("N2");
                if (speedMBs < 10d) return speedMBs.ToString("N1");
                return speedMBs.ToString("N0");
            }
            #endregion
        }
        /// <summary>
        /// Výsledek <see cref="SpeedShading.Apply"/>: adaptivní rozsah rychlostí aktuálního souboru pro legendu
        /// </summary>
        internal class SpeedRange
        {
            /// <summary>
            /// Barvení podle rychlosti je zapnuto
            /// </summary>
            public bool Enabled { get; set; }
            /// <summary>
            /// Rozsah je určen (dost bloků s rychlostí)
            /// </summary>
            public bool IsValid { get; set; }
            /// <summary>
            /// Dolní mez rozsahu v MB/s = nejtmavší odstín
            /// </summary>
            public double LowMBs { get; set; }
            /// <summary>
            /// Horní mez rozsahu v MB/s = nejsvětlejší odstín
            /// </summary>
            public double HighMBs { get; set; }
        }
        #endregion
        #region class SingleLogInfo :Informace o stavu záchrany jednoho souboru, včetně mapy bloků a jejich stavu. Řídící prvek pro řešení toho, co bude kopírováno.
        /// <summary>
        /// Informace o stavu záchrany jednoho souboru, včetně mapy bloků a jejich stavu. Řídící prvek pro řešení toho, co bude kopírováno.
        /// </summary>
        internal class SingleLogInfo
        {
            #region Data celého logu
            /// <summary>
            /// Konstruktor
            /// </summary>
            public SingleLogInfo(SingleFileInfo fileInfo)
            {
                FileInfo = fileInfo;
                var segmentStep = CommonSegmentLengthMin;
                var length = segmentStep * (fileInfo.FastCopyBlockSize / segmentStep);                                                                              // Délka bloku standardní
                BlockLength = (length < CommonSegmentLengthMin ? CommonSegmentLengthMin : (length > CommonSegmentLengthMax ? CommonSegmentLengthMax : length));     // Do rozmezí Min-Max
                Blocks = new List<SingleBlockInfo>();
                this.LoadLogFile();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                var statistic = this.Statistic;
                return $"File: {SourceFile}; Length: {SourceFileLength:3} B; Processed: {statistic.TotalBlocksSize:3} B; WithErrors: {statistic.BadBlocksSize:3} B";
            }
            /// <summary>
            /// Zámek, který zajišťuje, že při současném přístupu z více threadů se log soubor ukládá bezpečně a nedojde k poškození dat.
            /// </summary>
            private object __LogSaveLock = new object();
            /// <summary>
            /// Informace o zdrojovém souboru.
            /// </summary>
            public SingleFileInfo FileInfo { get; private set; }
            /// <summary>
            /// Vstupní zdrojový soubor k záchraně.
            /// </summary>
            public string SourceFile { get { return FileInfo.SourceFile; } }
            /// <summary>
            /// Cílový soubor pro uložení toho, co lze uložit
            /// </summary>
            public string DestinationFile { get { return FileInfo.DestinationFile; } }
            /// <summary>
            /// Cílový soubor pro uložení logu záchrany.
            /// </summary>
            public string DestinationLog { get { return FileInfo.DestinationLog; } }
            /// <summary>
            /// Cílový soubor pro uložení logu existuje?
            /// </summary>
            public bool DestinationLogExists { get; private set; }
            /// <summary>
            /// Délka vstupního souboru <see cref="SourceFile"/>
            /// </summary>
            public long? SourceFileLength { get { return FileInfo?.SourceFileLength; } }
            /// <summary>
            /// Délka standardního bloku pro kopírování, podle které se vytváří mapování bloků v <see cref="Blocks"/>.
            /// </summary>
            public int BlockLength { get; private set; }
            /// <summary>
            /// Bloky souboru, kde klíčem je počáteční offset bloku a hodnotou je informace o daném bloku <see cref="SingleBlockInfo"/>.
            /// </summary>
            protected List<SingleBlockInfo> Blocks { get; private set; }
            /// <summary>
            /// Index prvku v poli <see cref="Blocks"/>, který se právě zpracovává
            /// </summary>
            public int? CurrentBlockIndex { get; private set; }
            /// <summary>
            /// Čas počátku, Ticks
            /// </summary>
            public long StartTime { get; set; }
            /// <summary>
            /// Čas trvání, sekundy
            /// </summary>
            public decimal TotalTime { get; set; }
            /// <summary>
            /// Kolikrát se při zpracování tohoto souboru odpojilo zdrojové zařízení (přes všechny běhy). Ukládá se do hlavičky logu.
            /// </summary>
            public int DeviceLostCount { get; set; }
            /// <summary>
            /// V kolika bězích skončilo zpracování tohoto souboru nedokončené a s problémem (odpojení zdroje, nebo přerušení po chybách čtení v tomto běhu).
            /// Ukládá se do hlavičky logu. Soubory s problémy se zpracovávají až po ostatních, viz <see cref="FileRescue._SortFilesByProblems"/>.
            /// </summary>
            public int ProblemRuns { get; set; }
            #endregion
            #region Správa bloků - řízení procesu kopírování ve smyslu toho, co se bude kopírovat
            /// <summary>
            /// Aktuální bloky zkopírované
            /// </summary>
            public SingleBlockInfo[] CurrentBlocks 
            {
                get 
                {
                    SingleBlockInfo[] currentBlocks = null;
                    lock (this.Blocks)
                    {
                        currentBlocks = this.Blocks.ToArray();
                    }
                    return currentBlocks;
                }
            }
            /// <summary>
            /// Vrátí první blok, který se bude kopírovat.
            /// <para/>
            /// Tato metoda aktivuje lock na <see cref="Blocks"/> a uvnitř locku volá <see cref="_GetFirstBlockLock"/>.
            /// </summary>
            /// <returns></returns>
            internal SingleBlockInfo GetFirstBlock()
            {
                // Začínáme číst ty bloky, které nemají chybu = tedy ty, které jsme dosud nečetli:
                this.ErrorLevelProcess = 0;

                var fileLength = SourceFileLength ?? 0L;
                if (fileLength <= 0L) return null;

                // Pokusíme se najít první blok: úplně první, anebo první po dosavadním posledním, pokud existuje pozice, která dosud není zpracovaná:
                //  Toto pod jedním Lockem:
                lock (this.Blocks)
                {
                    return _GetFirstBlockLock();
                }
            }
            /// <summary>
            /// Vrátí první blok, který se bude kopírovat.
            /// <para/>
            /// Tato metoda neaktivuje lock na <see cref="Blocks"/> - ten již musí být aktivován.
            /// </summary>
            /// <returns></returns>
            private SingleBlockInfo _GetFirstBlockLock()
            {
                // Hledáme od začátku mapy, na úrovni ErrorLevelProcess = 0:
                //  a) Dosud nemám žádný blok => _GetNextBlockLock založí první blok na pozici 0.
                //  b) V mapě (načtené z logu) je nezpracovaný blok (None, ErrorsCount 0) = "díra", např. blok přečtený, ale nezapsaný před Stop => vrátí se ten.
                //  c) Jinak se pokračuje za posledním blokem v evidenci (navázání po přerušení).
                //  d) Pokud poslední blok je i poslední blok souboru, přejde se na opakování chybných bloků (ErrorLevelProcess 1, 2, ...).
                this.ErrorLevelProcess = 0;
                this.CurrentBlockIndex = null;
                return _GetNextBlockLock();
            }
            /// <summary>
            /// Vrátí další blok ke zpracování = volá se až poté, kdy byl vydán první blok metodou <see cref="GetFirstBlock()"/>.
            /// <para/>
            /// Tato metoda aktivuje lock na <see cref="Blocks"/> a uvnitř locku volá <see cref="_GetNextBlockLock"/>.
            /// </summary>
            /// <returns></returns>
            internal SingleBlockInfo GetNextBlock()
            {
                var fileLength = SourceFileLength ?? 0L;
                if (fileLength <= 0L) return null;

                lock (this.Blocks)
                {
                    return _GetNextBlockLock();
                }
            }
            /// <summary>
            /// Vrátí další blok ke zpracování = volá se až poté, kdy byl vydán první blok metodou <see cref="GetFirstBlock()"/>.
            /// <para/>
            /// Tato metoda neaktivuje lock na <see cref="Blocks"/> - ten již musí být aktivován.
            /// </summary>
            /// <returns></returns>
            private SingleBlockInfo _GetNextBlockLock()
            {
                var count = this.Blocks.Count;
                var blockLength = this.BlockLength;
                var initialBlockIndex = this.CurrentBlockIndex;

                // Vyhledáme nejbližší blok za indexem CurrentBlockIndex (anebo od indexu 0, když CurrentBlockIndex = null),
                // který má ErrorsCount == this.ErrorLevelProcess:
                int currentIndex = (this.CurrentBlockIndex.HasValue ? (this.CurrentBlockIndex.Value + 1) : 0);
                int searchErrorLevel = this.ErrorLevelProcess;
                while (true)
                {
                    searchErrorLevel = this.ErrorLevelProcess;
                    if (searchErrorLevel > SingleLogInfo.MaxErrorsCount) return null;              // Další ErrorLevelProcess už se nezpracovává...

                    // 1) Pokud na pozici [currentIndex] není přítomen blok:
                    if (currentIndex >= count)
                    {   // Tedy currentIndex ukazuje za poslední existující prvek Blocks:
                        // 1a) Pokud existující blok na poslední existující pozici má (BlockEnd < fileLength), pak se za poslední blok ještě vejde další blok:
                        var currentBlockEnd = (count > 0 ? this.Blocks[count - 1].BlockEnd : 0L);  // BlockEnd z posledního bloku; nebo 0 pokud dosud není
                        var nextBlockLength = this._GetValidLength(ref currentBlockEnd);            // Kolik místa je za posledním blokem?
                        if (nextBlockLength > 0)
                            return _CreateNewBlockLock(currentBlockEnd);                                // Vytvoří new blok, začínající na dané pozici 'currentBlockEnd', s odpovídající délkou; přidá do Blocks, vepíše jeho index do CurrentBlockIndex, a vrátí jej.

                        // 1b) Za poslední blok se už nevejde žádný další blok (=aktuální poslední blok skutečně končí na konci souboru) = už máme všechny bloky:
                        // Tak se tedy vrátíme na začátek (currentIndex = 0), a budeme hledat bloky počínaje od indexu 0, ale s ErrorLevelProcess +1 :
                        this.CurrentBlockIndex = null;
                        currentIndex = 0;

                        this.ErrorLevelProcess++;
                        continue;
                    }

                    // Tady jsme v situaci, kdy currentIndex ukazuje na některý reálný prvek, který máme prověřit.
                    // Reálný = už jsme jej zkusili kopírovat: zkontrolujeme, v jakém je stavu.
                    // Pokud prvek není hotov, a jeho ErrorsCount == searchErrorLevel, pak jej vrátíme; jinak hledáme další...
                    //   Proč rovnost (ErrorsCount == searchErrorLevel) ? Protože po první chybě (ErrorLevel = 1) v prvku 005 nebudeme řešit opakování čtení prvku 005 (to by byla ErrorLevel = 2),
                    //   ale najdeme prvek 055 s ErrorsCount == 1 a ten zkusíme...
                    bool scanFromZero = (currentIndex == 0);
                    bool hasNextBlocks = false;
                    while (currentIndex < count)
                    {
                        var currentBlock = this.Blocks[currentIndex];
                        if (!currentBlock.IsDone)
                        {
                            if (currentBlock.ErrorsCount == searchErrorLevel)
                            {   // Tento blok ještě není hotový (není zkopírován a ještě jsme nevzdali snahu o jeho zkopírování):
                                // Pokud jeho počet chyb odpovídá tomu počtu, který nyní řešíme, tak jej akceptujeme pro zpracování:
                                this.CurrentBlockIndex = currentIndex;
                                return currentBlock;
                            }
                            // Prvek není hotov, ale jeho počet chyb neodpovídá aktuální úrovni:
                            // Pokud nyní scanujeme od 0, tak si poznamenáme, že máme prvky s vyšším ErrorsCount, takže když nyní nic nenajdeme, tak možná najdeme v příštím kole:
                            if (scanFromZero && !hasNextBlocks)
                            {
                                hasNextBlocks = true;
                            }
                        }
                        currentIndex++;
                    }

                    // Úroveň 0 (nezpracované bloky): po projití existujících bloků pokračujeme krokem 1) = nový blok za posledním blokem,
                    //  anebo (na konci souboru) přechod na úroveň 1. Tady nesmíme skončit ani přeskočit na další úroveň:
                    if (searchErrorLevel == 0) continue;

                    // V hledané ErrorLevel jsme už nic nenašli...
                    // A pokud jsme nyní prohledávali vše, a nemáme ani nic ve vyšší úrovni, pak skončíme:
                    if (scanFromZero && !hasNextBlocks) return null;

                    // Jdeme na další ErrorLevel = zkusíme načíst nehotové prvky, které měly chybu:
                    this.CurrentBlockIndex = null;
                    currentIndex = 0;
                    this.ErrorLevelProcess++;
                }
            }
            /// <summary>
            /// Vrátí new blok, pro danou pozici Start, který přidá na konec soupisu do Listu <see cref="Blocks"/>.
            /// Pokud pro danou pozici <paramref name="start"/> a délku souboru <see cref="SourceFileLength"/> není třeba žádný blok, vrátí null.
            /// <para/>
            /// Pokud vytvoří new blok, pak jej přidá (na konec) do pole <see cref="Blocks"/>, a do <see cref="CurrentBlockIndex"/> vepíše jeho index.
            /// </summary>
            /// <param name="start"></param>
            /// <returns></returns>
            private SingleBlockInfo _CreateNewBlockLock(long start)
            {
                var length = _GetValidLength(ref start);                                 // ref Validní pozice Start; vrátí Validní délka pro daný start: reflektuje standardní BlockLength, ošetřuje jej na celkovou délku souboru SourceFileLength
                if (length <= 0) return null;

                var block = new SingleBlockInfo(start, length);
                // Následující sekvence musí být v locku:
                this.CurrentBlockIndex = this.Blocks.Count;                              // Index prvku, který za chvilku přidám, bude == aktuální Count (prvek bude poslední v Listu)
                this.Blocks.Add(block);
                return block;
            }
            /// <summary>
            /// Vrátí validní délku bloku, pokud bude začínat na dané pozici <paramref name="start"/>. Tuto pozici validuje s ohledem na rozsah 0 ÷ <see cref="SourceFileLength"/>.
            /// </summary>
            /// <param name="start"></param>
            /// <returns></returns>
            private int _GetValidLength(ref long start)
            {
                var fileLength = SourceFileLength ?? 0L;
                start = (start < 0L ? 0L : (start > fileLength ? fileLength : start));   // Validní pozice Start

                if (start >= fileLength) return 0;                                       // Pro danou pozici Start a délku souboru není třeba vytvářet žádný blok

                var blockLength = this.BlockLength;
                var end = start + blockLength;                                           // Konec bloku = Zadaný Start + standardní délka bloku
                if (end > fileLength) end = fileLength;                                  // Pokud konec bloku > Konec souboru, pak Konec bloku = Konec souboru
                long length = end - start;                                               // Reálná validní Délka bloku
                return (length <= 0L ? 0 : (length > CommonSegmentLengthMax ? CommonSegmentLengthMax : (int)length));
            }
            internal void SaveException(Exception ex)
            {
                // Zde můžeme uložit informace o výjimce do logu, pokud je to potřeba.
                // Například můžeme přidat záznam do BlockMap s informací o chybě.
            }
            /// <summary>
            /// Úroveň chyb, které řešíme.
            /// <para/>
            /// Po zahájení kopírování nejprve kopírujeme dosud nezkopírované chyby, <see cref="ErrorLevelProcess"/> je null.<br/>
            /// Pokud při kopírování bloku dojde k chybě, zvýší se počet <see cref="SingleBlockInfo.ErrorsCount"/>
            /// Jakmile dojdeme s kopírováním do konce (tedy nelze najít blok, který by dosud nebyl kopírován), 
            /// </summary>
            internal int ErrorLevelProcess { get; private set; }
            /// <summary>
            /// 3 = Nejvyšší počet chyb, po kterých blok odepíšeme jako nečitelný (3x a dost!)
            /// </summary>
            public static int MaxErrorsCount { get { return 3; } }
            /// <summary>
            /// Setřídí bloky podle pozice <see cref="SingleBlockInfo.BlockStart"/>
            /// </summary>
            private void BlocksSort()
            {
                BlocksSort(this.Blocks);
            }
            /// <summary>
            /// Setřídí bloky podle pozice <see cref="SingleBlockInfo.BlockStart"/>
            /// </summary>
            private static void BlocksSort(List<SingleBlockInfo> blocks)
            {
                if (blocks != null && blocks.Count > 1)
                    blocks.Sort((a, b) => a.BlockStart.CompareTo(b.BlockStart));
            }
            /// <summary>
            /// Přičte si jednu neuloženou změnu do <see cref="UnsavedChangesCount"/>.
            /// <para/>
            /// Až ten počet dosáhne nebo překročí <see cref="TresholdSaveOnChanges"/>, pak bude <see cref="NeedSave"/> = true a bude vhodné zavolat <see cref="SaveLogFileAsync"/>.
            /// </summary>
            /// <param name="block"></param>
            /// <param name="result"></param>
            internal void AddUnsavedChange()
            {
                this.UnsavedChangesCount++;
            }
            /// <summary>
            /// Počet změn (nový/změněný blok), které byly zaznamenány od posledního Load nebo Save
            /// </summary>
            internal int UnsavedChangesCount { get; private set; }
            /// <summary>
            /// Po tolika změnách si vyžádáme uložení logu
            /// </summary>
            internal int TresholdSaveOnChanges { get { return 64; } }
            /// <summary>
            /// Obsahuje true, pokud Log obsahuje tolik nových dat, že by bylo vhodné jej uložit do souboru...
            /// </summary>
            internal bool NeedSave { get { return (UnsavedChangesCount >= TresholdSaveOnChanges); } }
            #endregion
            #region Statistika
            /// <summary>
            /// Statistická data
            /// </summary>
            public StatisticInfo Statistic
            {
                get
                {   // Thread Work do Blocks přidává, statistiku čte i GUI thread => pod zámkem jako CurrentBlocks:
                    lock (this.Blocks)
                    {
                        return GetStatistic(this.SourceFileLength, this.Blocks);
                    }
                }
            }
            /// <summary>
            /// Z dodaných bloků spočítá statistiku
            /// </summary>
            /// <param name="blocks"></param>
            /// <returns></returns>
            private static StatisticInfo GetStatistic(long? fileLength, IEnumerable<SingleBlockInfo> blocks)
            {
                var statistic = new StatisticInfo(fileLength ?? 0L);
                if (blocks != null)
                {
                    foreach (var block in blocks)
                        statistic.AddBlock(block);
                }
                return statistic;
            }
            #endregion
            #region Ukládání a načítání dat logu
            /// <summary>
            /// Uloží Log do souboru <see cref="DestinationLog"/>. Pokud soubor existuje, přepíše jej. Asynchronní metoda.
            /// </summary>
            public async Task SaveLogFileAsync()
            {
                if (SaveLogProcessing) return;

                try
                {
                    await Task.Run(() => SaveLogFile(false));
                }
                catch (Exception ex)
                {
                    try { SaveException(ex); } catch { /* ignore logging failure */ }
                    throw;
                }
            }
            /// <summary>
            /// Uloží Log do souboru <see cref="DestinationLog"/>. Pokud soubor existuje, přepíše jej. Synchronní metoda.
            /// </summary>
            public void SaveLogFile(bool isFinal)
            {
                // Pokud nejsem Final, a aktuálně probíhá zápis, tak tento nový požadavek ignoruji. Finální požadavek ale neignoruji!
                if (!isFinal && SaveLogProcessing) return;

                // Počkám na dokončení předchozího zápisu (to jen když jsem Final):
                var end = DateTime.Now.AddSeconds(5);
                while (SaveLogProcessing && DateTime.Now < end)                // Čekám nejvýše 5 sekund
                {
                    Thread.Sleep(150);                                         // Testuji vždy po 150 milisec, tady nebudu řešit semafor = jde jen o jediný poslední zápis logu
                }
                if (SaveLogProcessing) return;


                // Zápis do logu je označen hodnotou SaveLogProcessing = true:
                try
                {
                    SaveLogProcessing = true;
                    doSaveLog();
                }
                finally
                {
                    SaveLogProcessing = false;
                }

                void doSaveLog()
                {
                    // Toto může chvilku trvat...:
                    List<SingleBlockInfo> blocks = null;
                    lock (this.Blocks)
                    {   // Zámek je nutný proto, že do pole Block mohu přidávat prvky v jiném threadu...
                        blocks = Blocks.ToList();                                            // Oddělený List, a následně pracuji jen s ním
                        UnsavedChangesCount = 0;                                             // V tuto chvíli jsem převzal data
                    }
                    var statistic = GetStatistic(this.SourceFileLength, blocks);
                    // BlocksSort(blocks);
                    var badBlocks = blocks.Where(b => b.IsErrorBlock).ToList();

                    // Zápis jen z jednoho threadu:
                    lock (__LogSaveLock)
                    {
                        string delim = DELIMITER_HEADER;
                        using (var logWriter = new StreamWriter(DestinationLog, false))
                        {
                            logWriter.WriteLine("#############################################################################################");
                            logWriter.WriteLine($"SourceFile:       {delim}{SourceFile}");
                            logWriter.WriteLine($"DestinationFile:  {delim}{DestinationFile}");
                            logWriter.WriteLine($"FileLength:       {delim}{statistic.FileSize:N0} B");
                            logWriter.WriteLine($"ProcessedSize:    {delim}{statistic.TotalBlocksSize:N0} B");
                            logWriter.WriteLine($"ProcessedPercent: {delim}{statistic.TotalBlocksPercent} %");
                            logWriter.WriteLine($"BadBlocksCount:   {delim}{statistic.BadBlocksCount:N0}");
                            logWriter.WriteLine($"BadBlocksSize:    {delim}{statistic.BadBlocksSize:N0} B");
                            if (isFinal)
                                logWriter.WriteLine($"TotalCopyTime:    {delim}{this.TotalTime} sec");
                            logWriter.WriteLine($"{HEADER_DEVICE_LOST_COUNT}  {delim}{this.DeviceLostCount}");
                            logWriter.WriteLine($"{HEADER_PROBLEM_RUNS}      {delim}{this.ProblemRuns}");

                            if (badBlocks.Count > 0)
                            {
                                logWriter.WriteLine("#############################################################################################");
                                logWriter.WriteLine(SingleBlockInfo.GetTitleToLog());
                                logWriter.WriteLine($"BadBlocks:");
                                foreach (var badBlock in badBlocks)
                                    logWriter.WriteLine(badBlock.GetLineToLog());
                            }

                            logWriter.WriteLine("#############################################################################################");
                            logWriter.WriteLine(SingleBlockInfo.GetTitleToLog());
                            logWriter.WriteLine($"Blocks:");
                            foreach (var block in blocks)
                                logWriter.WriteLine(block.GetLineToLog());

                            if (isFinal)
                                logWriter.WriteLine("#############################################################################################");

                            logWriter.Flush();
                            logWriter.Close();
                        }
                    }
                }
            }
            /// <summary>
            /// Obsahuje true v době, kdy probíhá zápis logu
            /// </summary>
            private bool SaveLogProcessing;
            /// <summary>
            /// Načte data ze souboru <see cref="DestinationLog"/> a naplní mapu bloků <see cref="Blocks"/> podle obsahu logu. Pokud soubor neexistuje, vytvoří prázdnou mapu bloků.
            /// </summary>
            private void LoadLogFile()
            {
                this.Clear();
                if (File.Exists(DestinationLog))
                {
                    var blockDict = new Dictionary<long, SingleBlockInfo>();
                    string delim = DELIMITER_HEADER;
                    var state = LogFilePartType.None;
                    using (var logReader = new StreamReader(DestinationLog))
                    {
                        string line;
                        while ((line = logReader.ReadLine()) != null)
                        {
                            var text = line.Trim();

                            // Řádek typicky ###################################################################### je oddělovačem odstavců:
                            if (text.StartsWith("#######"))
                            {   // Oddělovač částí resetuje stav, následně budeme teprve detekovat, co obsahuje:
                                state = LogFilePartType.None;
                                continue;
                            }
                            // Řádek typicky ## xxx yyy zzz               je komentář a nemění nic:
                            if (text.StartsWith("## ") || text == "##" || text.StartsWith("# ") || text == "#")
                            {   // Komentář nic nedělá:
                                continue;
                            }

                            // Detekce: Pokud jsme na začátku odstavce a nevíme, co bude obsahovat, tak zkusíme detekovat obsah:
                            if (state == LogFilePartType.None)
                            {
                                if (text.Contains(delim)) state = LogFilePartType.Header;                         // Tento odstavec pokračuje dál a zpracuje i svůj první řádek
                                if (text == "BadBlocks:") { state = LogFilePartType.BadBlocks; continue; }        // Tento odstavec nezpracovává svůj vlastní řádek titulku
                                if (text == "Blocks:") { state = LogFilePartType.AllBocks; continue; }            // Tento odstavec nezpracovává svůj vlastní řádek titulku
                            }

                            // Obsah načítaných bloků:
                            switch (state)
                            {
                                case LogFilePartType.Header:
                                    // Načítáme záhlaví, které obsahuje řádky typicky: "Jméno      :TAB hodnota
                                    var headerParts = text.Split(new string[] { delim }, StringSplitOptions.None);
                                    var headerCount = headerParts.Length;
                                    var headerName = headerParts[0].Trim();

                                    /* Takto lze načíst data, která uchovává log soubor v hlavičce, a jsou primárně daná Logem, a nikoli Souborem:
                                    if (headerCount == 2 && headerName == "SourceFile:")
                                        SourceFile = headerParts[1].Trim();
                                    else if (headerCount == 2 && headerName == "DestinationFile:")
                                        DestinationFile = headerParts[1].Trim();
                                    */

                                    // Počítadla problémů souboru (přes všechny běhy):
                                    if (TryParseHeaderCounter(headerParts, HEADER_DEVICE_LOST_COUNT, out int deviceLostCount))
                                        this.DeviceLostCount = deviceLostCount;
                                    else if (TryParseHeaderCounter(headerParts, HEADER_PROBLEM_RUNS, out int problemRuns))
                                        this.ProblemRuns = problemRuns;

                                    // Header obsahuje i další informace, které jsou primárně určeny pro lidského čtenáře (ProcessedSize, BadBlocksCount, BadBlockSize).
                                    break;

                                case LogFilePartType.BadBlocks:
                                    // BadBlocks jsou do Logu vypisovány jen informativně pro uživatele, ale jsou standardně obsaženy v následné sekci AllBlocks:
                                    break;

                                case LogFilePartType.AllBocks:
                                    // Načítáme řádek obshaující jednotlivé bloky:
                                    var blockInfo = SingleBlockInfo.FromLogLine(line);
                                    if (blockInfo != null)
                                    {   // Oprava logů ze starší verze (výsledek se ukládal před zápisem): blok přečtený OK, ale bez času zápisu, nebyl do cíle zapsán.
                                        //  Vrátíme jej do stavu "nezpracováno", aby se při navázání přečetl a zapsal znovu (ErrorsCount ponecháme):
                                        if ((blockInfo.Status == SingleBlockInfo.BlockStatus.OK || blockInfo.Status == SingleBlockInfo.BlockStatus.OKAfterErrors) && !blockInfo.WriteTimeSeconds.HasValue && blockInfo.BlockLength > 0)
                                            blockInfo.Status = (blockInfo.ErrorsCount == 0 ? SingleBlockInfo.BlockStatus.None : SingleBlockInfo.BlockStatus.WithError);

                                        // Akceptujeme jen první výskyt bloku s daným počátečním offsetem!
                                        // Pokud by se v logu vyskytl duplicitně, tak ten následující ignorujeme.
                                        // V Dictionary smí být pouze 1x, takže do záznamu se měl dostat jen jedinkrát.
                                        var blockStart = blockInfo.BlockStart;
                                        if (!blockDict.ContainsKey(blockStart))
                                            blockDict.Add(blockStart, blockInfo);
                                    }
                                    break;

                                    // Jiné bloky nenačítáme...
                            }
                        }
                    }
                    this.Blocks = blockDict.Values.ToList();
                    this.BlocksSort();
                }
                this.UnsavedChangesCount = 0;
            }
            private void Clear()
            {
                this.Blocks = new List<SingleBlockInfo>();
                this.UnsavedChangesCount = 0;
            }
            /// <summary>
            /// Typ odstavce v načítaném souboru logu, který určuje, co se v něm nachází. Podle toho se rozhodujeme, zda a jak jej načítat.
            /// </summary>
            private enum LogFilePartType
            {
                None,
                Header,
                BadBlocks,
                AllBocks
            }
            /// <summary>
            /// Oddělovač v hlavičce: název hodnoty od vlastní hodnoty
            /// </summary>
            private const string DELIMITER_HEADER = "\t";
            /// <summary>Název hodnoty v hlavičce logu: <see cref="DeviceLostCount"/></summary>
            private const string HEADER_DEVICE_LOST_COUNT = "DeviceLostCount:";
            /// <summary>Název hodnoty v hlavičce logu: <see cref="ProblemRuns"/></summary>
            private const string HEADER_PROBLEM_RUNS = "ProblemRuns:";
            /// <summary>
            /// Z rozděleného řádku hlavičky ("Název: TAB hodnota") načte celočíselnou hodnotu daného názvu
            /// </summary>
            private static bool TryParseHeaderCounter(string[] headerParts, string name, out int value)
            {
                value = 0;
                return (headerParts.Length == 2 && headerParts[0].Trim() == name && Int32.TryParse(headerParts[1].Trim(), out value) && value >= 0);
            }
            /// <summary>
            /// Z hlavičky daného logu rychle načte počítadla problémů (<see cref="DeviceLostCount"/>, <see cref="ProblemRuns"/>) – bez načítání bloků.
            /// Neexistující / nečitelný log = 0 a 0. Nikdy nevyhodí výjimku.
            /// </summary>
            /// <param name="logFile"></param>
            /// <param name="deviceLostCount"></param>
            /// <param name="problemRuns"></param>
            public static void ReadProblemCounters(string logFile, out int deviceLostCount, out int problemRuns)
            {
                deviceLostCount = 0;
                problemRuns = 0;
                try
                {
                    if (String.IsNullOrEmpty(logFile) || !File.Exists(logFile)) return;
                    using (var logReader = new StreamReader(logFile))
                    {
                        string line;
                        int separators = 0;
                        while ((line = logReader.ReadLine()) != null)
                        {
                            var text = line.Trim();
                            if (text.StartsWith("#######"))
                            {   // Hlavička je mezi prvním a druhým oddělovačem:
                                if (++separators >= 2) break;
                                continue;
                            }
                            var parts = text.Split(new string[] { DELIMITER_HEADER }, StringSplitOptions.None);
                            if (TryParseHeaderCounter(parts, HEADER_DEVICE_LOST_COUNT, out int dlc)) deviceLostCount = dlc;
                            else if (TryParseHeaderCounter(parts, HEADER_PROBLEM_RUNS, out int pr)) problemRuns = pr;
                        }
                    }
                }
                catch { }
            }
            #endregion
        }
        #endregion
        #region class StatisticInfo : Statistická data o stavu aktuálního souboru
        /// <summary>
        /// Statistická data
        /// </summary>
        public class StatisticInfo
        {
            public StatisticInfo(long fileSize)
            {
                this.FileSize = fileSize;
                var count = Enum.GetValues(typeof(SingleBlockInfo.BlockCategory)).Length;
                __CategoryCount = new int[count];
                __CategorySize = new long[count];
            }
            public void AddBlock(SingleBlockInfo block)
            {
                TotalBlocks++;
                TotalBlocksSize += block.BlockLength;

                if (block.IsErrorBlock)
                {
                    this.BadBlocksCount++;
                    this.BadBlocksSize += block.BlockLength;
                }

                var category = (int)SingleBlockInfo.GetCategory(block.Status, block.ErrorsCount);
                __CategoryCount[category]++;
                __CategorySize[category] += block.BlockLength;

                // Rychlost čtení počítáme jen z úspěšně přečtených bloků:
                if (block.ReadTimeSeconds.HasValue && (block.Status == SingleBlockInfo.BlockStatus.OK || block.Status == SingleBlockInfo.BlockStatus.OKAfterErrors))
                {
                    this.ReadBytes += block.BlockLength;
                    this.ReadSeconds += block.ReadTimeSeconds.Value;
                }
            }
            /// <summary>
            /// Vrátí počet bloků v dané kategorii
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public int GetCategoryCount(SingleBlockInfo.BlockCategory category) { return __CategoryCount[(int)category]; }
            /// <summary>
            /// Vrátí celkovou délku bloků v dané kategorii
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
            public long GetCategorySize(SingleBlockInfo.BlockCategory category) { return __CategorySize[(int)category]; }
            private int[] __CategoryCount;
            private long[] __CategorySize;
            /// <summary>
            /// Celková délka úspěšně přečtených bloků, které mají změřený čas čtení
            /// </summary>
            public long ReadBytes { get; private set; }
            /// <summary>
            /// Celkový čas čtení bloků <see cref="ReadBytes"/>, v sekundách
            /// </summary>
            public decimal ReadSeconds { get; private set; }
            /// <summary>
            /// Průměrná rychlost čtení v Byte/sec; null pokud dosud není změřena
            /// </summary>
            public decimal? ReadSpeed { get { return (ReadSeconds > 0m ? (decimal?)(ReadBytes / ReadSeconds) : (decimal?)null); } }
            /// <summary>
            /// Délka celého souboru
            /// </summary>
            public long FileSize { get; private set; }
            /// <summary>
            /// Celkový počet všech bloků, které jsme již zkoušeli (proběhl pokus o čtení)
            /// </summary>
            public int TotalBlocks { get; private set; }
            /// <summary>
            /// Celková délka všech bloků, které jsme již zkoušeli (proběhl pokus o čtení)
            /// </summary>
            public long TotalBlocksSize { get; private set; }
            /// <summary>
            /// Procento <see cref="TotalBlocksSize"/> vůči <see cref="FileSize"/> = kolik už jsme nějak zpracovali
            /// </summary>
            public double TotalBlocksPercent { get { return _GetPercent(TotalBlocksSize, FileSize); } }
            /// <summary>
            /// Celkový počet bloků s chyou, které jsme již zkoušeli (proběhl pokus o čtení a skončil chybou)
            /// </summary>
            public int BadBlocksCount { get; private set; }
            /// <summary>
            /// Celková délka bloků s chyou, které jsme již zkoušeli (proběhl pokus o čtení a skončil chybou)
            /// </summary>
            public long BadBlocksSize { get; private set; }
            /// <summary>
            /// Procento <see cref="BadBlocksSize"/> vůči <see cref="FileSize"/> = kolik máme zatím chyb
            /// </summary>
            public double BadBlocksPercent { get { return _GetPercent(BadBlocksSize, FileSize); } }
            /// <summary>
            /// Vrací procentuální hodnotu <paramref name="value"/> vůči <paramref name="total"/>, v rozsahu 0 - 100%.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="total"></param>
            /// <returns></returns>
            private static double _GetPercent(long value, long total)
            {
                if (total <= 0L) return 0f;
                if (value <= 0L) return 0f;
                if (value >= total) return 100f;

                var ratio = (double)value / (double)total;
                return Math.Round(100d * ratio, 2);
            }
        }
        #endregion
        #region Win32 API deklarace + konstanty
        // Win32 API deklarace
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFilePointer(
            SafeFileHandle hFile,
            long liDistanceToMove,
            IntPtr lpNewFilePointer,
            uint dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern long GetFileSize(
            SafeFileHandle hFile,
            IntPtr lpFileSizeHigh);

        /// <summary>
        /// Oznámí Windows, že vlákno vyžaduje běh systému (zákaz automatického spánku). Platí pro volající vlákno.
        /// </summary>
        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);
        /// <summary>SetThreadExecutionState: nastavení platí trvale, dokud se nezmění dalším voláním s ES_CONTINUOUS</summary>
        private const uint ES_CONTINUOUS = 0x80000000;
        /// <summary>SetThreadExecutionState: systém nesmí automaticky přejít do spánku (monitor se vypnout smí)</summary>
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;

        /// <summary>
        /// Vrátí "unbiased interrupt time" v jednotkách 100 ns = čas od startu systému BEZ doby spánku a hibernace. Rozlišení cca 15,6 ms.
        /// </summary>
        [DllImport("kernel32.dll")]
        private static extern bool QueryUnbiasedInterruptTime(out ulong unbiasedTime);

        // Konstanty - Přístup
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;

        // Konstanty - Share mode
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_NONE = 0x00000000;

        // Konstanty - Creation disposition
        private const uint OPEN_EXISTING = 3;
        private const uint CREATE_ALWAYS = 2;

        // Konstanty - Flags
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        private const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;

        // Seek mode
        private const uint FILE_BEGIN = 0;
        #endregion
        #region Společné konstanty
        internal const int CommonSegmentLengthHdd = 128 * 1024;
        internal const int CommonSegmentLengthUsb = 32 * 1024;
        internal const int CommonSegmentLengthMin = 4 * 1024;
        internal const int CommonSegmentLengthMax = 1024 * 1024;
        #endregion
    }
    #region Podklady pro práci = Args
    public class FileRescueInputInfo
    {
        public string InputFileNames { get; set; }
        public string OutputFilesPath { get; set; }

        public bool TestAddBadBlocks { get; set; }
        public bool TestAddSimulatedWaiting { get; set; }
    }
    #endregion
    #region Okno se zadáním parametrů
    public class FileRescueInputWindow : Form
    {
        #region Konstrukce a tvorba komponent
        /// <summary>
        /// Konstruktor, okno bez dat
        /// </summary>
        public FileRescueInputWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Konstruktor, okno s daty
        /// </summary>
        /// <param name="inputInfo"></param>
        public FileRescueInputWindow(FileRescueInputInfo inputInfo)
        {
            InputData = inputInfo;
            InitializeComponent();
        }
        /// <summary>
        /// Vytvoří komponenty formuláře. Souřadnice prvků určuje <see cref="_DoLayout"/>.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Záchrana souborů - parametry";
            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.ShowInTaskbar = false;

            _InputFilesLabel = new Label()
            {
                Text = "Vstupní soubory:\r\n" +
                       "Zadejte adresář + názvy souborů nebo wildcards;\r\n" +
                       "Zadejte více možností na samostatné řádky;\r\n" +
                       "Pro vynechání souborů zadejte jako první znak vykřičník\r\n" +
                       "Prázdné řádky budou ignorovány",
                AutoSize = true,
                TabIndex = 0
            };
            _InputFilesMemo = new TextBox() { Multiline = true, AcceptsReturn = true, WordWrap = false, ScrollBars = ScrollBars.Both, TabIndex = 1 };
            _OutputPathLabel = new Label() { Text = "Výstupní adresář:", AutoSize = true, TabIndex = 2 };
            _OutputPathText = new TextBox() { TabIndex = 3 };
            _OutputPathButton = new Button() { Text = "...", TabIndex = 4 };
            _OkButton = new Button() { Text = "OK", TabIndex = 5 };
            _CancelButton = new Button() { Text = "Storno", TabIndex = 6 };

            _OutputPathButton.Click += _OutputPathButton_Click;
            _OkButton.Click += _OkButton_Click;
            _CancelButton.Click += _CancelButton_Click;

            this.Controls.AddRange(new Control[] { _InputFilesLabel, _InputFilesMemo, _OutputPathLabel, _OutputPathText, _OutputPathButton, _OkButton, _CancelButton });
            this.AcceptButton = _OkButton;
            this.CancelButton = _CancelButton;

            // Výchozí velikost okna odvodím od požadované velikosti MemoEditu (500 x 200):
            int inputLabelHeight = _InputFilesLabel.PreferredHeight;            // Víceřádkový popis
            int outputLabelHeight = _OutputPathLabel.PreferredHeight;
            int textHeight = _OutputPathText.Height;
            int clientWidth = EdgeMargin + DefaultMemoWidth + ButtonGap + SmallButtonWidth + EdgeMargin;
            int clientHeight = EdgeMargin + inputLabelHeight + LabelGap + DefaultMemoHeight + RowGap + outputLabelHeight + LabelGap + textHeight + RowGap + ButtonHeight + EdgeMargin;
            this.ClientSize = new Size(clientWidth, clientHeight);
            this.MinimumSize = new Size(this.Width - DefaultMemoWidth + 200, this.Height - DefaultMemoHeight + 60);

            this.ResumeLayout(false);
            _DoLayout();
        }
        private Label _InputFilesLabel;
        private TextBox _InputFilesMemo;
        private Label _OutputPathLabel;
        private TextBox _OutputPathText;
        private Button _OutputPathButton;
        private Button _OkButton;
        private Button _CancelButton;
        #endregion
        #region Layout: ruční rozmístění prvků v OnResize
        /// <summary>
        /// Po změně velikosti okna přepočítáme souřadnice prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _DoLayout();
        }
        /// <summary>
        /// Rozmístí prvky podle aktuální velikosti <see cref="Form.ClientSize"/>.
        /// MemoEdit mění Width i Height; TextBox mění Width a je vázán k dolnímu okraji; Buttony OK a Storno jsou vázány k dolnímu okraji vlevo.
        /// </summary>
        private void _DoLayout()
        {
            if (_InputFilesMemo is null) return;                               // Ještě neproběhl InitializeComponent()

            var clientSize = this.ClientSize;
            int inputLabelHeight = _InputFilesLabel.PreferredHeight;            // Víceřádkový popis
            int outputLabelHeight = _OutputPathLabel.PreferredHeight;
            int textHeight = _OutputPathText.Height;
            int contentWidth = clientSize.Width - EdgeMargin - ButtonGap - SmallButtonWidth - EdgeMargin;
            if (contentWidth < 50) contentWidth = 50;

            // Odspodu: Buttony OK a Storno
            int buttonTop = clientSize.Height - EdgeMargin - ButtonHeight;
            _OkButton.Bounds = new Rectangle(EdgeMargin, buttonTop, ButtonWidth, ButtonHeight);
            _CancelButton.Bounds = new Rectangle(EdgeMargin + ButtonWidth + ButtonGap, buttonTop, ButtonWidth, ButtonHeight);

            // Nad nimi: TextBox s buttonem "..." a nad ním jeho Label
            int textTop = buttonTop - RowGap - textHeight;
            _OutputPathText.Bounds = new Rectangle(EdgeMargin, textTop, contentWidth, textHeight);
            _OutputPathButton.Bounds = new Rectangle(EdgeMargin + contentWidth + ButtonGap, textTop - 1, SmallButtonWidth, textHeight + 2);
            int outputLabelTop = textTop - LabelGap - outputLabelHeight;
            _OutputPathLabel.Location = new Point(EdgeMargin, outputLabelTop);

            // Odshora: Label a MemoEdit, který vyplní zbývající výšku
            _InputFilesLabel.Location = new Point(EdgeMargin, EdgeMargin);
            int memoTop = EdgeMargin + inputLabelHeight + LabelGap;
            int memoHeight = outputLabelTop - RowGap - memoTop;
            if (memoHeight < 30) memoHeight = 30;
            _InputFilesMemo.Bounds = new Rectangle(EdgeMargin, memoTop, contentWidth, memoHeight);
        }
        private const int EdgeMargin = 12;
        private const int LabelGap = 3;
        private const int RowGap = 12;
        private const int ButtonGap = 6;
        private const int ButtonWidth = 90;
        private const int ButtonHeight = 28;
        private const int SmallButtonWidth = 28;
        private const int DefaultMemoWidth = 500;
        private const int DefaultMemoHeight = 200;
        #endregion
        #region Data
        /// <summary>
        /// Data okna. Při zobrazení okna se přenesou do prvků (pokud nejsou null).
        /// Po stisku OK se sem uloží hodnoty z prvků (pokud je null, vytvoří se nová instance).
        /// </summary>
        public FileRescueInputInfo InputData { get; set; }
        /// <summary>
        /// Při každém zobrazení okna (Show i ShowDialog) přenese data z <see cref="InputData"/> do prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (this.Visible)
                _DataToControls();
            base.OnVisibleChanged(e);
        }
        /// <summary>
        /// Přenese data z <see cref="InputData"/> do prvků. Pokud <see cref="InputData"/> je null, prvky budou prázdné.
        /// </summary>
        private void _DataToControls()
        {
            var inputData = this.InputData;
            _InputFilesMemo.Text = inputData?.InputFileNames ?? "";
            _OutputPathText.Text = inputData?.OutputFilesPath ?? "";
        }
        /// <summary>
        /// Přenese data z prvků do <see cref="InputData"/>, který případně vytvoří.
        /// </summary>
        private void _DataFromControls()
        {
            if (this.InputData is null)
                this.InputData = new FileRescueInputInfo();
            this.InputData.InputFileNames = _InputFilesMemo.Text;
            this.InputData.OutputFilesPath = _OutputPathText.Text;
        }
        #endregion
        #region Buttony
        /// <summary>
        /// Button "..." = výběr výstupního adresáře
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OutputPathButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Výstupní adresář";
                dialog.ShowNewFolderButton = true;
                var currentPath = (_OutputPathText.Text ?? "").Trim();
                if (currentPath.Length > 0 && System.IO.Directory.Exists(currentPath))
                    dialog.SelectedPath = currentPath;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    _OutputPathText.Text = dialog.SelectedPath;
            }
        }
        /// <summary>
        /// Button OK = uložit data a zavřít
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OkButton_Click(object sender, EventArgs e)
        {
            _DataFromControls();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        /// <summary>
        /// Button Storno = zavřít bez uložení dat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        #endregion
    }
    #endregion
    #region Summary blok: controly pro levý panel výsledků
    /// <summary>
    /// Společný předek controlů Summary bloku FileRescue: rámeček, řádky textu, formátování hodnot, ToolTip.
    /// Data dostává jako snímek <see cref="FileRescue.SummaryInfo"/> metodou <see cref="StoreInfo"/>.
    /// </summary>
    internal abstract class FileRescueSummaryControl : WorkingResultControl
    {
        #region Konstrukce, data
        public FileRescueSummaryControl()
            : base()
        {
            __ToolTip = new ToolTip() { InitialDelay = 400, ReshowDelay = 100, AutoPopDelay = 30000, ShowAlways = true };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && __ToolTip != null)
            {
                __ToolTip.Dispose();
                __ToolTip = null;
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Uloží snímek dat a překreslí control
        /// </summary>
        /// <param name="summary"></param>
        public void StoreInfo(FileRescue.SummaryInfo summary)
        {
            this.Summary = summary;
            SetToolTip(GetToolTipText());
            this.Refresh();
        }
        /// <summary>
        /// Aktuální snímek dat; může být null
        /// </summary>
        protected FileRescue.SummaryInfo Summary { get; private set; }
        /// <summary>
        /// Potomek vrátí text do ToolTipu, nebo null
        /// </summary>
        /// <returns></returns>
        protected virtual string GetToolTipText() { return null; }
        /// <summary>
        /// Nastaví text ToolTipu, jen při jeho změně
        /// </summary>
        /// <param name="text"></param>
        private void SetToolTip(string text)
        {
            if (__ToolTip is null || String.Equals(text, __ToolTipText, StringComparison.Ordinal)) return;
            __ToolTipText = text;
            __ToolTip.SetToolTip(this, text);
        }
        private ToolTip __ToolTip;
        private string __ToolTipText;
        #endregion
        #region Kreslení
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle clientBounds = this.ClientRectangle;
            Rectangle outerBounds = new Rectangle(clientBounds.X + 1, clientBounds.Y + 1, clientBounds.Width - 3, clientBounds.Height - 3);
            Rectangle innerBounds = new Rectangle(outerBounds.X + 4, outerBounds.Y + 3, outerBounds.Width - 8, outerBounds.Height - 6);
            Painter.PaintRectangle(e.Graphics, this.BackColor, clientBounds);
            Painter.PaintRectangle(e.Graphics, Skin.BackgroundColor, outerBounds);
            e.Graphics.DrawRectangle(Painter.GetPen(Painter.GetColor(this.BackColor, -48, -48, -48)), outerBounds);
            PaintContent(e.Graphics, innerBounds);
        }
        /// <summary>
        /// Potomek vykreslí obsah do daného prostoru
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        protected abstract void PaintContent(Graphics graphics, Rectangle bounds);
        /// <summary>
        /// Vykreslí jeden řádek: text vlevo, volitelně text vpravo
        /// </summary>
        protected void PaintRow(Graphics graphics, Rectangle row, string leftText, string rightText, Font font = null, Color? color = null)
        {
            font = font ?? this.Font;
            var textColor = color ?? this.ForeColor;
            if (!String.IsNullOrEmpty(rightText))
                Painter.PaintText(graphics, font, rightText, textColor, row, ContentAlignment.MiddleRight);
            if (!String.IsNullOrEmpty(leftText))
                Painter.PaintText(graphics, font, leftText, textColor, row, ContentAlignment.MiddleLeft);
        }
        /// <summary>
        /// Vykreslí text zkrácený uprostřed cesty (...), pokud se nevejde
        /// </summary>
        protected void PaintPathText(Graphics graphics, Rectangle row, string text)
        {
            if (String.IsNullOrEmpty(text)) return;
            TextRenderer.DrawText(graphics, text, this.Font, row, this.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.PathEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
        /// <summary>
        /// Vrátí řádek na dané pozici Y
        /// </summary>
        protected static Rectangle GetRow(Rectangle bounds, int y) { return new Rectangle(bounds.X, y, bounds.Width, LineHeight); }
        /// <summary>
        /// Výška jednoho řádku textu
        /// </summary>
        protected static int LineHeight { get { return Control.DefaultFont.Height + 3; } }
        /// <summary>
        /// Výška okrajů controlu nad a pod obsahem
        /// </summary>
        protected static int FrameHeight { get { return 10; } }
        #endregion
        #region Formátování hodnot
        /// <summary>
        /// Vrátí velikost ve vhodných jednotkách (B, kB, MB, GB, TB)
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string FormatSize(decimal size)
        {
            if (size < 1024m) return $"{size:N0} B";
            string[] units = new string[] { "kB", "MB", "GB", "TB" };
            int index = -1;
            while (size >= 1024m && index < units.Length - 1)
            {
                size = size / 1024m;
                index++;
            }
            return (size < 10m ? $"{size:N2} {units[index]}" : $"{size:N1} {units[index]}");
        }
        /// <summary>
        /// Vrátí čas ve formě h:mm:ss
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string FormatTime(decimal seconds)
        {
            var time = TimeSpan.FromSeconds((double)(seconds < 0m ? 0m : seconds));
            return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
        }
        #endregion
    }
    /// <summary>
    /// Summary blok FileRescue, část A: celkový postup přes všechny soubory
    /// </summary>
    internal class FileRescueTotalControl : FileRescueSummaryControl
    {
        protected override void PaintContent(Graphics graphics, Rectangle bounds)
        {
            var summary = this.Summary;
            int y = bounds.Y;
            int lineHeight = LineHeight;

            // 1. Soubor X z N, čas:
            string filesText = "Záchrana souborů";
            if (summary != null)
            {
                if (summary.IsFinished && summary.SourceHung)
                    filesText = "Přerušeno: zdroj neodpovídá";
                else if (summary.IsFinished && summary.SourceDeviceLost)
                    filesText = "Přerušeno: zdroj odpojen";
                else if (summary.IsFinished)
                    filesText = $"Dokončeno, souborů: {summary.FilesCount}";
                else if (summary.FilesCount > 0)
                    filesText = $"Soubor {(summary.CurrentFileIndex >= 0 ? (summary.CurrentFileIndex + 1).ToString() : "-")} z {summary.FilesCount}";
            }
            using (var boldFont = new Font(this.Font, FontStyle.Bold))
                PaintRow(graphics, GetRow(bounds, y), filesText, (summary != null ? FormatTime(summary.ElapsedSeconds) : null), boldFont);
            y += lineHeight;

            // 2. Aktuální soubor (jen jméno, celá cesta v ToolTipu):
            if (summary?.CurrentFileName != null)
                PaintPathText(graphics, GetRow(bounds, y), System.IO.Path.GetFileName(summary.CurrentFileName));
            y += lineHeight;

            // 3. Progress bar přes všechny soubory podle bajtů:
            var barBounds = new Rectangle(bounds.X, y + 1, bounds.Width, lineHeight + 2);
            Painter.PaintRectangle(graphics, Skin.FreeSpaceColor, barBounds);
            double ratio = summary?.ProcessedRatio ?? 0d;
            int barWidth = (int)Math.Round(ratio * barBounds.Width, 0);
            if (barWidth > 0)
                Painter.PaintBar3D(graphics, Skin.RescueBlockOkColor, new Rectangle(barBounds.X, barBounds.Y, barWidth, barBounds.Height));
            if (summary != null)
                Painter.PaintText(graphics, this.Font, $"{(100d * ratio):N1} %   ({FormatSize(summary.ProcessedBytes)} z {FormatSize(summary.TotalBytes)})", this.ForeColor, barBounds, ContentAlignment.MiddleCenter);
            y += lineHeight + 4;

            // 4. Výsledky souborů:
            if (summary != null)
            {
                string text = $"OK: {summary.FilesOk}   S chybami: {summary.FilesWithErrors}";
                if (summary.FilesInterrupted > 0) text += $"   Přerušeno: {summary.FilesInterrupted}";
                if (summary.FilesNotExists > 0) text += $"   Nenalezeno: {summary.FilesNotExists}";
                if (summary.FilesFailed > 0) text += $"   Chyba: {summary.FilesFailed}";
                PaintRow(graphics, GetRow(bounds, y), text, null);
            }
            y += lineHeight;

            // 5. Nečitelné (odložené) soubory; při nenulové hodnotě zvýrazněno (Bold + barva nečitelného bloku):
            if (summary != null)
            {
                string text = $"Počet nečitelných souborů: {summary.FilesDeferred}";
                if (summary.FilesDeferred > 0)
                {
                    using (var boldFont = new Font(this.Font, FontStyle.Bold))
                        PaintRow(graphics, GetRow(bounds, y), text, null, boldFont, Skin.RescueBlockAbortedColor);
                }
                else
                    PaintRow(graphics, GetRow(bounds, y), text, null);
            }
        }
        protected override string GetToolTipText()
        {
            var summary = this.Summary;
            if (summary?.CurrentFileName is null) return null;
            return $"Zdroj: {summary.CurrentFileName}\r\nCíl: {summary.CurrentFileTarget}";
        }
        protected override int CurrentOptimalHeight { get { return OptimalHeight; } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        public static int OptimalHeight { get { return FrameHeight + 5 * LineHeight + 4; } }
    }
    /// <summary>
    /// Summary blok FileRescue, část B: aktuální soubor a jeho bloky podle stavu (zároveň legenda barev mapy)
    /// </summary>
    internal class FileRescueFileControl : FileRescueSummaryControl
    {
        protected override void PaintContent(Graphics graphics, Rectangle bounds)
        {
            var summary = this.Summary;
            var statistic = summary?.CurrentStatistic;
            int y = bounds.Y;
            int lineHeight = LineHeight;

            // 1. Fáze; zamrzlý zdroj zvýrazněn barvou nečitelného bloku:
            bool isHangAlert = (summary != null && (summary.SourceNotRespondingSeconds.HasValue || summary.SourceHung));
            using (var boldFont = new Font(this.Font, FontStyle.Bold))
                PaintRow(graphics, GetRow(bounds, y), GetPhaseText(summary), null, boldFont, (isHangAlert ? Skin.RescueBlockAbortedColor : (Color?)null));
            y += lineHeight;

            // 2. Zpracováno:
            if (summary != null && statistic != null)
            {
                double percent = (summary.CurrentFileLength > 0L ? Math.Min(100d, 100d * statistic.TotalBlocksSize / summary.CurrentFileLength) : 0d);
                PaintRow(graphics, GetRow(bounds, y), $"Zpracováno: {percent:N1} %", $"{FormatSize(statistic.TotalBlocksSize)} z {FormatSize(summary.CurrentFileLength)}");
            }
            y += lineHeight;

            // 3. Rychlost čtení:
            var readSpeed = statistic?.ReadSpeed;
            if (readSpeed.HasValue)
                PaintRow(graphics, GetRow(bounds, y), "Rychlost čtení:", FormatSize(readSpeed.Value) + "/s");
            y += lineHeight + 4;

            // 4. Bloky podle kategorie = legenda barev mapy:
            var speedRange = this.SpeedRange;
            bool isShaded = (speedRange != null && speedRange.Enabled && speedRange.IsValid);
            foreach (var category in CategoryOrder)
            {
                var row = GetRow(bounds, y);
                int boxSize = row.Height - 5;
                var boxBounds = new Rectangle(row.X, row.Y + 2, boxSize, boxSize);
                if (category == FileRescue.SingleBlockInfo.BlockCategory.Ok && isShaded)
                    PaintShadesBar(graphics, boxBounds);
                else
                    Painter.PaintBar3D(graphics, FileRescue.SingleBlockInfo.GetColor(category), boxBounds);
                var textRow = new Rectangle(row.X + boxSize + 5, row.Y, row.Width - boxSize - 5, row.Height);
                string valueText = (statistic != null ? $"{statistic.GetCategoryCount(category):N0}  ({FormatSize(statistic.GetCategorySize(category))})" : null);
                PaintRow(graphics, textRow, FileRescue.SingleBlockInfo.GetCategoryText(category), valueText);
                y += lineHeight;
            }
            y += 4;

            // 5. Legenda rychlosti: "Rychlost:  [tmavá ... světlá]  1,2 – 11 MB/s"
            if (isShaded)
            {
                var row = GetRow(bounds, y);
                string label = "Rychlost:";
                string rangeText = $"{FileRescue.SpeedShading.FormatSpeed(speedRange.LowMBs)} – {FileRescue.SpeedShading.FormatSpeed(speedRange.HighMBs)} MB/s";
                int labelWidth = TextRenderer.MeasureText(graphics, label, this.Font).Width + 2;
                int rangeWidth = TextRenderer.MeasureText(graphics, rangeText, this.Font).Width + 2;
                PaintRow(graphics, row, label, rangeText);
                var barBounds = new Rectangle(row.X + labelWidth, row.Y + 2, row.Width - labelWidth - rangeWidth - 4, row.Height - 5);
                if (barBounds.Width > 10)
                    PaintShadesBar(graphics, barBounds);
            }
            else if (speedRange != null && speedRange.Enabled)
                PaintRow(graphics, GetRow(bounds, y), "Rychlost: zatím málo dat", null);
            y += lineHeight;

            // 6. Přepínač "Barvit podle rychlosti":
            {
                var row = GetRow(bounds, y);
                bool isChecked = (speedRange?.Enabled ?? App.Settings.FileRescueSpeedShading);
                var glyphSize = CheckBoxRenderer.GetGlyphSize(graphics, System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                var glyphPoint = new Point(row.X, row.Y + (row.Height - glyphSize.Height) / 2);
                CheckBoxRenderer.DrawCheckBox(graphics, glyphPoint, (isChecked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal));
                var textRow = new Rectangle(row.X + glyphSize.Width + 4, row.Y, row.Width - glyphSize.Width - 4, row.Height);
                PaintRow(graphics, textRow, "Barvit podle rychlosti", null);
                int textWidth = TextRenderer.MeasureText(graphics, "Barvit podle rychlosti", this.Font).Width;
                __SpeedToggleBounds = new Rectangle(row.X, row.Y, glyphSize.Width + 4 + textWidth + 4, row.Height);
            }
        }
        /// <summary>
        /// Vykreslí pruh všech odstínů rychlosti zleva (pomalé, tmavé) doprava (rychlé, světlé)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        private static void PaintShadesBar(Graphics graphics, Rectangle bounds)
        {
            int count = FileRescue.SpeedShading.ShadesCount;
            for (int i = 0; i < count; i++)
            {
                int x0 = bounds.X + (int)Math.Round((double)bounds.Width * i / count, 0);
                int x1 = bounds.X + (int)Math.Round((double)bounds.Width * (i + 1) / count, 0);
                if (x1 > x0)
                    Painter.PaintRectangle(graphics, FileRescue.SpeedShading.GetShadeColor(i), new Rectangle(x0, bounds.Y, x1 - x0, bounds.Height));
            }
        }
        #region Barvení podle rychlosti: rozsah pro legendu, přepínač
        /// <summary>
        /// Rozsah rychlostí aktuálního souboru pro legendu, z <see cref="FileRescue.SpeedShading.Apply"/>. Setuje se před <see cref="FileRescueSummaryControl.StoreInfo"/>, samo nepřekresluje.
        /// </summary>
        public FileRescue.SpeedRange SpeedRange { get; set; }
        /// <summary>
        /// Uživatel klikl na přepínač "Barvit podle rychlosti". Hodnotu v <see cref="AppSettings.FileRescueSpeedShading"/> mění handler.
        /// </summary>
        public event EventHandler SpeedShadingToggled;
        /// <summary>
        /// Prostor přepínače "Barvit podle rychlosti" z posledního vykreslení
        /// </summary>
        private Rectangle __SpeedToggleBounds;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var cursor = (__SpeedToggleBounds.Contains(e.Location) ? Cursors.Hand : Cursors.Default);
            if (this.Cursor != cursor) this.Cursor = cursor;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left && __SpeedToggleBounds.Contains(e.Location))
                SpeedShadingToggled?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        /// <summary>
        /// Vrátí text fáze zpracování aktuálního souboru
        /// </summary>
        /// <param name="summary"></param>
        /// <returns></returns>
        private static string GetPhaseText(FileRescue.SummaryInfo summary)
        {
            if (summary != null && summary.SourceNotRespondingSeconds.HasValue) return $"Zdroj neodpovídá {summary.SourceNotRespondingSeconds.Value:N0} s – vyjměte kartu";
            if (summary is null || summary.CurrentFileIndex < 0) return (summary?.IsFinished ?? false) ? (summary.SourceHung ? "Zdroj neodpovídá" : "Dokončeno") : "Příprava...";
            if (summary.SourceHung) return "Zdroj neodpovídá – soubor nedokončen";
            if (summary.SourceDeviceLost) return "Zdroj odpojen – soubor nedokončen";
            if (summary.CurrentFileStatus == FileRescue.FileStatus.NotExists) return "Soubor neexistuje";
            if (summary.CurrentFileDeferred) return "Nečitelný soubor – odložen";
            if (summary.IsFinished || summary.CurrentFileStatus == FileRescue.FileStatus.Finished) return "Soubor dokončen";
            string prefix = (summary.IsSecondWave ? "Opakovaný pokus: " : "");
            if (summary.CurrentErrorLevel <= 0) return prefix + "1. průchod souborem";
            return prefix + $"Opakované čtení chyb: pokus {summary.CurrentErrorLevel + 1} z {FileRescue.SingleLogInfo.MaxErrorsCount}";
        }
        /// <summary>
        /// Pořadí kategorií v tabulce
        /// </summary>
        private static readonly FileRescue.SingleBlockInfo.BlockCategory[] CategoryOrder = new FileRescue.SingleBlockInfo.BlockCategory[]
        {
            FileRescue.SingleBlockInfo.BlockCategory.Ok,
            FileRescue.SingleBlockInfo.BlockCategory.OkAfterErrors,
            FileRescue.SingleBlockInfo.BlockCategory.Error1,
            FileRescue.SingleBlockInfo.BlockCategory.Error2,
            FileRescue.SingleBlockInfo.BlockCategory.Aborted,
            FileRescue.SingleBlockInfo.BlockCategory.Reading
        };
        protected override string GetToolTipText()
        {
            var summary = this.Summary;
            if (summary?.CurrentFileName is null) return null;
            return $"Zdroj: {summary.CurrentFileName}\r\nCíl: {summary.CurrentFileTarget}";
        }
        protected override int CurrentOptimalHeight { get { return OptimalHeight; } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        public static int OptimalHeight { get { return FrameHeight + 11 * LineHeight + 8; } }
    }
    #endregion
}
