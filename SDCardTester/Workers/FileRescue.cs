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
            fileInfo.SourceFileInfo = new FileInfo(fileInfo.SourceFile);
            if (!fileInfo.SourceFileInfo.Exists)
            {
                fileInfo.ErrorMessage = $"Soubor '{fileInfo.SourceFile}' neexistuje.";
                return;
            }


        }
        #endregion
        #region Pracovní třídy: SingleFileInfo, SingleLogInfo
        /// <summary>
        /// Třída obsahující základní data o jednom souboru k záchraně, včetně parametrů pro kopírování a výsledku operace.
        /// </summary>
        private class SingleFileInfo
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
            public FileInfo SourceFileInfo
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
