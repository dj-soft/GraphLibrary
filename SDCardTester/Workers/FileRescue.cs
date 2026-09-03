using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

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
        #region Privátní řízení běhu
        /// <summary>
        /// Zahájení testu, zde již v threadu Working
        /// </summary>
        protected override void Run()
        {
            _PrepareResults();
            foreach (var fileInfo in __Files)
            {
                try
                {
                    _RunSingleFile(fileInfo);
                }
                catch (Exception exc)
                {
                    fileInfo.ErrorMessage = exc.ToString();
                    App.ShowError(fileInfo.ErrorMessage, "Chyba při zpracování souboru");
                }
            }
            CallWorkingDone();
        }
        /// <summary>
        /// Příprava seznamu souborů k záchraně = pole <see cref="__Files"/>
        /// </summary>
        private void _PrepareResults()
        {
            __Files = new List<SingleFileInfo>();
            var args = this.Target;
            if (args != null)
            {
                var inputFiles = args.InputFileNames.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var outputPath = (args.OutputFilesPath ?? "").Trim();
                if (String.IsNullOrEmpty( outputPath))
                    outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                foreach (var inputFile in inputFiles)
                {
                    var fileName = System.IO.Path.GetFileName(inputFile);                                // Bez adresáře
                    var fileLog = System.IO.Path.GetFileNameWithoutExtension(inputFile) + ".rescuelog";  // Soubor obsahující log
                    var fileInfo = new SingleFileInfo()
                    {
                        SourceFile = inputFile,
                        DestinationFile = System.IO.Path.Combine(outputPath, fileName),
                        DestinationLog = System.IO.Path.Combine(outputPath, fileLog)
                    };
                    __Files.Add(fileInfo);
                }
            }
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
            __CurrentFile = fileInfo;
            __CurrentFile.CheckFile();
            this.CallWorkingStep();
            if (this.__CurrentFile.SourceFileStatus == FileStatus.NotExists) return;

            __CurrentFile.LoadLog();
            this.CallWorkingStep();

            this._RunCopySingleFile();

            // Pokud najdeme Output soubor, pak jsme s kopírováním začali už dříve. Zkusíme k němu najít log soubor a podle něj se rozhodneme, zda a odkud a jak pokračovat.
            // Pokud Log soubor obsahuje "Hotovo OK", pak je záchrana dokončena a pro daný soubor nic neděláme.
           
            // Nyní máme načten Log soubor, anebo jej máme nově vytvořený, tak s jeho pomocí budeme postupně kopírovat zdrojový soubor do cíle - kopírujeme dosud chybějící neznámé bloky,
            // a později se pokusíme zkopírovat i chybové bloky:

        }
        private void _RunCopySingleFile()
        {

        }
        private void _PrepareLogFile(SingleFileInfo fileInfo)
        {
            if (File.Exists(fileInfo.DestinationLog))
            {
                // Log soubor existuje, načteme jej a podle jeho obsahu se rozhodneme, zda pokračovat, anebo je záchrana dokončena.
                var logLines = File.ReadAllLines(fileInfo.DestinationLog);
                if (logLines.Length > 0 && logLines[0].Contains("Hotovo OK"))
                {
                    fileInfo.Success = true;
                    return;
                }
            }
            else
            {
                // Log soubor neexistuje, vytvoříme jej s úvodními informacemi.
                using (var logWriter = new StreamWriter(fileInfo.DestinationLog, false))
                {
                    logWriter.WriteLine($"Záchrana souboru: {fileInfo.SourceFile}");
                    logWriter.WriteLine($"Cílový soubor: {fileInfo.DestinationFile}");
                    logWriter.WriteLine($"Datum zahájení: {DateTime.Now}");
                    logWriter.WriteLine("Stav: Zahájeno");
                }
            }
        }
        private SingleFileInfo __CurrentFile;
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
        #region Pracovní třídy: SingleFileInfo, SingleLogInfo
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
                FastCopyBlockSize = 16384;
                BufferSize = 4096;
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
            public FileStatus SourceFileStatus { get; private set; }
            /// <summary>
            /// Cílový soubor pro uložení toho, co lze uložit
            /// </summary>
            public string DestinationFile { get; set; }
            /// <summary>
            /// Cílový soubor pro uložení logu záchrany.
            /// </summary>
            public string DestinationLog { get; set; }

            public int FastCopyBlockSize { get; set; }
            public int BufferSize { get; set; }
            public int SkipOnError { get; set; }
            public int AbortAfterContinueErrors { get; set; }

            public bool Success { get; set; }
            public string ErrorMessage { get; set; }

            public SingleLogInfo CurrentLog { get; private set; }
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
        }
        /// <summary>
        /// Informace o stavu záchrany jednoho souboru, včetně mapy bloků a jejich stavu.
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
                BlockLength = fileInfo.FastCopyBlockSize;
                BlockMap = new Dictionary<long, SingleBlockInfo>();
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
            public long? SourceFileLength { get { return FileInfo.SourceFileLength; } }
            /// <summary>
            /// Délka standardního bloku pro kopírování, podle které se vytváří mapování bloků v <see cref="BlockMap"/>.
            /// </summary>
            public long BlockLength { get; private set; }
            /// <summary>
            /// Bloky souboru, kde klíčem je počáteční offset bloku a hodnotou je informace o daném bloku <see cref="SingleBlockInfo"/>.
            /// </summary>
            public System.Collections.Generic.Dictionary<long, SingleBlockInfo> BlockMap { get; private set; }
            #endregion
            #region Statistika
            public StatisticInfo Statistic
            {
                get
                {
                    var statistic = new StatisticInfo(SourceFileLength ?? 0L);
                    var blocks = BlockMap.Values;
                    foreach ( var block in blocks )
                        statistic.AddBlock( block );

                    return statistic;
                }
            }
            public class StatisticInfo
            {
                public StatisticInfo(long fileSize)
                {
                    this.FileSize = fileSize;
                }
                public void AddBlock(SingleBlockInfo block)
                {
                    TotalBlocks++;
                    TotalBlocksSize += block.BlockLength;
                    
                    if (block.IsErrorBlock)
                    {
                        this.BadBlocks++;
                        this.BadBlocksSize += block.BlockLength;
                    }
                }
                public long FileSize { get; private set; }
                public int TotalBlocks { get; private set; }
                public long TotalBlocksSize { get; private set; }
                public int BadBlocks { get; private set; }
                public long BadBlocksSize { get; private set; }
            }
            #endregion

            #region Ukládání a načítání dat logu
            /// <summary>
            /// Uloží Log do souboru <see cref="DestinationLog"/>. Pokud soubor existuje, přepíše jej.
            /// </summary>
            public void SaveLogFile()
            {
                // Toto může chvilku trvat...:
                var blocks = BlockMap.Values.ToList();
                blocks.Sort((a, b) => a.BlockStart.CompareTo(b.BlockStart));
                var totalBlocksSize = blocks.Sum(b => b.BlockLength);
                var badBlocks = blocks.Where(b => b.IsErrorBlock).ToList();
                var badBlocksSize = badBlocks.Sum(b => b.BlockLength);

                // Zápis jen z jednoho threadu:
                lock (__LogSaveLock)
                {
                    string delim = DELIMITER_HEADER;
                    using (var logWriter = new StreamWriter(DestinationLog, false))
                    {
                        logWriter.WriteLine("#############################################################################################");
                        logWriter.WriteLine($"SourceFile:       {delim}{SourceFile}");
                        logWriter.WriteLine($"DestinationFile:  {delim}{DestinationFile}");
                        logWriter.WriteLine($"FileLength:       {delim}{SourceFileLength}B");
                        logWriter.WriteLine($"ProcessedSize:    {delim}{totalBlocksSize}B");
                        logWriter.WriteLine($"BadBlocksCount:   {delim}{badBlocks.Count}");
                        logWriter.WriteLine($"BadBlockSize:     {delim}{badBlocksSize}B");

                        if (badBlocks.Count > 0)
                        {
                            logWriter.WriteLine("#############################################################################################");
                            logWriter.WriteLine($"BadBlocks:");
                            foreach (var badBlock in badBlocks)
                                logWriter.WriteLine(badBlock.GetLineToLog());
                        }


                        logWriter.WriteLine("#############################################################################################");
                        logWriter.WriteLine($"Blocks:");
                        foreach (var block in blocks)
                            logWriter.WriteLine(block.GetLineToLog());

                        logWriter.WriteLine("#############################################################################################");

                        logWriter.Flush();
                        logWriter.Close();
                    }
                }
            }
            /// <summary>
            /// Načte data ze souboru <see cref="DestinationLog"/> a naplní mapu bloků <see cref="BlockMap"/> podle obsahu logu. Pokud soubor neexistuje, vytvoří prázdnou mapu bloků.
            /// </summary>
            private void LoadLogFile()
            {
                if (File.Exists(DestinationLog))
                {
                    string delim = DELIMITER_HEADER;
                    var state = LogFilePartType.None;
                    using (var logReader = new StreamReader(DestinationLog))
                    {
                        string line;
                        while ((line = logReader.ReadLine()) != null)
                        {
                            var text = line.Trim();

                            if (text.StartsWith("#######"))
                            {   // Oddělovač částí resetuje stav, následně budeme teprve detekovat, co obsahuje:
                                state = LogFilePartType.None;
                                continue;
                            }

                            if ((state == LogFilePartType.None || state == LogFilePartType.Header) && text.Contains(delim))
                            {
                                state = LogFilePartType.Header;
                                var headerParts = text.Split(new string[] { delim }, StringSplitOptions.None);
                                var headerCount = headerParts.Length;
                                var headerName = headerParts[0].Trim();

                                /* Takto lze načíst data, která uchovává log soubor v hlavičce, a jsou primárně daná Logem, a nikoli Souborem:
                                if (headerCount == 2 && headerName == "SourceFile:")
                                    SourceFile = headerParts[1].Trim();
                                else if (headerCount == 2 && headerName == "DestinationFile:")
                                    DestinationFile = headerParts[1].Trim();
                                */

                                // Header obsahuje i další informace, které jsou primárně určeny pro lidského čtenáře (ProcessedSize, BadBlocksCount, BadBlockSize).
                                continue;
                            }

                            if ((state == LogFilePartType.None) && text == "BadBlocks:")
                            {   // Pokud nyní narazíme na text "BadBlocks:", pak přejdeme do stavu BadBlocks a následující blok nebudeme načítat. BadBlocks jsou součástí všech bloků.
                                state = LogFilePartType.BadBlocks;
                                continue;
                            }

                            if ((state == LogFilePartType.None) && text == "Blocks:")
                            {   // Pokud nyní narazíme na text "Blocks:", pak přejdeme do stavu AllBocks a následně budeme načítat jednotlivé bloky:
                                state = LogFilePartType.AllBocks;
                                continue;
                            }

                            if (state == LogFilePartType.BadBlocks)
                            {   // Bloky chybových bloků jsou součástí všech bloků, takže je zde nebudeme načítat:
                                continue;
                            }

                            if (state == LogFilePartType.AllBocks)
                            {   // Načítáme jednotlivé bloky a ukládáme je do mapy <see cref="BlockMap"/>:
                                var blockInfo = SingleBlockInfo.FromLogLine(line);
                                if (blockInfo != null)
                                {   // Akceptujeme jen první výskyt bloku s daným počátečním offsetem, pokud by se v logu vyskytl duplicitně:
                                    // V Dictionary smí být pouze 1x
                                    var blockStart = blockInfo.BlockStart;
                                    if (!BlockMap.ContainsKey(blockStart))
                                        BlockMap.Add(blockStart, blockInfo);
                                }
                                continue;
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Odstavec v načítaném souboru logu, který určuje, co se v něm nachází. Podle toho se rozhodujeme, zda a jak jej načítat.
            /// </summary>
            private enum LogFilePartType
            {
                None,
                Header,
                BadBlocks,
                AllBocks
            }
            private const string DELIMITER_HEADER = "\t";
            #endregion
        }
        /// <summary>
        /// Data o jednom kopírovaném bloku souboru, včetně jeho stavu a počtu pokusů o znovunačtení.
        /// </summary>
        internal class SingleBlockInfo
        {
            #region Data bloku
            public override string ToString()
            {
                return $"Start: {BlockStart:3}; Length: {BlockLength:3}";
            }
            /// <summary>
            /// Adresa začátku bloku v souboru, offset od začátku souboru.
            /// </summary>
            public long BlockStart { get; set; }
            /// <summary>
            /// Délka bloku. Pokud je blok OK, pak má standardní délku <see cref="SingleFileInfo.FastCopyBlockSize"/>, pokud je blok chybový, pak může být kratší.
            /// </summary>
            public long BlockLength { get; set; }
            /// <summary>
            /// Počet pokusů o znovunačtení po chybě = počet pokusů o čtení, které skončily chybou. Pokud je blok OK, pak je 0, pokud je blok chybový, pak je větší než 0.
            /// </summary>
            public int ReReadCount { get; set; }
            /// <summary>
            /// Stav bloku, zda je OK, nebo obsahuje chyby. Pokud je blok OK, pak je <see cref="ReReadCount"/> 0, pokud je blok chybový, pak je <see cref="ReReadCount"/> větší než 0.
            /// </summary>
            public BlockStatus Status { get; set; }
            /// <summary>
            /// Obsahuje true, pokud <see cref="Status"/> obsahuje nějakou chybu.
            /// </summary>
            public bool IsErrorBlock { get { var st = this.Status; return (st == SingleBlockInfo.BlockStatus.WithError || st == SingleBlockInfo.BlockStatus.ErrorAborted || st == SingleBlockInfo.BlockStatus.ErrorCopied); } }
            /// <summary>
            /// Stav bloku
            /// </summary>
            public enum BlockStatus
            {
                /// <summary>
                /// Blok je v neznámém stavu, nebyl dosud zpracován.
                /// Počitadlo <see cref="SingleBlockInfo.ReReadCount"/> je 0.
                /// </summary>
                None,
                /// <summary>
                /// Blok byl úspěšně zkopírován, bez chyb, je OK.
                /// Počitadlo <see cref="SingleBlockInfo.ReReadCount"/> je 0.
                /// </summary>
                OK,
                /// <summary>
                /// Blok hlásí chyby, dosud nebyl úspěšně zkopírován, proběhne další pokus.
                /// Počitadlo <see cref="SingleBlockInfo.ReReadCount"/> obsahuje počet pokusů. které skončily chybou.
                /// </summary>
                WithError,
                /// <summary>
                /// Blok obsahuje chyby, ale byl přeskočen a záchrana pokračuje na dalším bloku.
                /// Počitadlo <see cref="SingleBlockInfo.ReReadCount"/> obsahuje počet pokusů. které skončily chybou. Po posledním z nich byl blok trvale abortován.
                /// </summary>
                ErrorAborted,
                /// <summary>
                /// Blok obsahuje chyby, ale po několika pokusech byl úspěšně zkopírován a záchrana pokračuje na dalším bloku.
                /// Počitadlo <see cref="SingleBlockInfo.ReReadCount"/> obsahuje počet pokusů. které skončily chybou, před tím než se jej podařilo zkopírovat.
                /// </summary>
                ErrorCopied
            }
            #endregion
            #region Serializace a deserializace do logového souboru
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
                long? blockLength = null;
                int? reReadCount = null;
                BlockStatus? status = null;

                if (count >= 1 && Int64.TryParse(parts[0], out long start) && start >= 0L) blockStart = start;
                if (count >= 2 && Int64.TryParse(parts[1], out long length) && length >= 0L) blockLength = length;
                if (count >= 3 && Int32.TryParse(parts[2], out int rrCnt) && rrCnt >= 0) reReadCount = rrCnt;
                if (count >= 4 && Enum.TryParse<BlockStatus>(parts[3], out var stt)) status = stt;

                var hasData = blockStart.HasValue && blockLength.HasValue && reReadCount.HasValue && status.HasValue;
                if (!hasData) return null;

                return new SingleBlockInfo()
                {
                    BlockStart = blockStart.Value,
                    BlockLength = blockLength.Value,
                    ReReadCount = reReadCount.Value,
                    Status = status.Value
                };
            }
            /// <summary>
            /// Vrátí řádek, který lze uložit do logového souboru.
            /// Reverzní metoda je <see cref="FromLogLine(string)"/>.
            /// </summary>
            /// <returns></returns>
            public string GetLineToLog()
            {
                var d = DELIMITER_ITEMS;
                return $"{BlockStart}{d}{BlockLength}{d}{ReReadCount}{d}{Status}";
            }
            private const string DELIMITER_ITEMS = ";";
            #endregion
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
    }
    #region Podklady pro práci = Args
    public class FileRescueInputInfo
    {
        public string InputFileNames { get; set; }
        public string OutputFilesPath { get; set; }
    }
    #endregion
}
