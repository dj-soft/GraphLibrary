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
            /// <summary>
            /// Konstruktor
            /// </summary>
            public SingleLogInfo(SingleFileInfo fileInfo)
            {
                __FileInfo = fileInfo;
                BlockLength = fileInfo.FastCopyBlockSize;
                BlockMap = new System.Collections.Generic.SortedList<long, SingleBlockInfo>();
                this.LoadLogFile();
            }
            private SingleFileInfo __FileInfo;
            /// <summary>
            /// Vstupní zdrojový soubor k záchraně.
            /// </summary>
            public string SourceFile { get { return __FileInfo.SourceFile; } }
            /// <summary>
            /// Cílový soubor pro uložení toho, co lze uložit
            /// </summary>
            public string DestinationFile { get { return __FileInfo.DestinationFile; } }
            /// <summary>
            /// Cílový soubor pro uložení logu záchrany.
            /// </summary>
            public string DestinationLog { get { return __FileInfo.DestinationLog; } }
            /// <summary>
            /// Cílový soubor pro uložení logu existuje?
            /// </summary>
            public bool DestinationLogExists { get; private set; }
            /// <summary>
            /// Délka vstupního souboru <see cref="SourceFile"/>
            /// </summary>
            public long? SourceFileLength { get { return __FileInfo.SourceFileLength; } }
            /// <summary>
            /// Délka standardního bloku pro kopírování, podle které se vytváří mapování bloků v <see cref="BlockMap"/>.
            /// </summary>
            public long BlockLength { get; private set; }
            /// <summary>
            /// Bloky souboru, kde klíčem je počáteční offset bloku a hodnotou je informace o daném bloku <see cref="SingleBlockInfo"/>.
            /// </summary>
            public System.Collections.Generic.SortedList<long, SingleBlockInfo> BlockMap { get; private set; }

            private void LoadLogFile()
            {
                if (File.Exists(DestinationLog))
                {
                    using (var logReader = new StreamReader(DestinationLog))
                    {
                        string line;
                        while ((line = logReader.ReadLine()) != null)
                        {


                            if (line.StartsWith("Záchrana souboru:"))
                            {
                                SourceFile = line.Substring(17).Trim();
                            }
                            else if (line.StartsWith("Cílový soubor:"))
                            {
                                DestinationFile = line.Substring(15).Trim();
                            }
                            else if (line.StartsWith("Datum zahájení:"))
                            {
                                // Můžeme načíst datum zahájení, pokud je potřeba
                            }
                            else if (line.StartsWith("Stav:"))
                            {
                                // Můžeme načíst stav, pokud je potřeba
                            }
                        }
                    }


                    var logLines = File.ReadAllLines(DestinationLog);
                    foreach (var line in logLines)
                    {
                        if (line.StartsWith("Block:"))
                        {
                            var parts = line.Substring(6).Split(',');
                            if (parts.Length == 3)
                            {
                                long blockStart = long.Parse(parts[0]);
                                long blockLength = long.Parse(parts[1]);
                                bool isErrorBlock = bool.Parse(parts[2]);
                                BlockMap[blockStart] = new SingleBlockInfo()
                                {
                                    BlockStart = blockStart,
                                    BlockLength = blockLength,
                                    IsErrorBlock = isErrorBlock
                                };
                            }
                        }
                    }
                }

            }
            private void SaveLogFile()
            {
                using (var logWriter = new StreamWriter(DestinationLog, false))
                {
                    logWriter.WriteLine($"Záchrana souboru: {SourceFile}");
                    logWriter.WriteLine($"Cílový soubor: {DestinationFile}");
                    logWriter.WriteLine($"Datum zahájení: {DateTime.Now}");
                    logWriter.WriteLine("Stav: Zahájeno");
                    foreach (var block in BlockMap.Values)
                    {
                        logWriter.WriteLine($"Block:{block.BlockStart},{block.BlockLength},{block.IsErrorBlock}");
                    }
                }
            }
        }

        internal class SingleBlockInfo
        {
            public long BlockStart { get; set; }
            public long BlockLength { get; set; }
            public bool IsErrorBlock { get; set; }
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
