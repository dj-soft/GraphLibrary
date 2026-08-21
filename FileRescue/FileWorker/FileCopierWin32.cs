using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace Djs.Tools.CopyFile
{
    /// <summary>
    /// Robustní čtečka souborů s možností obnovení po chybách
    /// Přeskakuje nečitelné sektory a vyplňuje je nulami ve výstupu
    /// </summary>
    public class FileCopierWin32
    {
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
        public FileCopierWin32()
        {
            __StopWatch = new System.Diagnostics.Stopwatch();
            __StopWatchFreq = System.Diagnostics.Stopwatch.Frequency;
        }
        /// <summary>
        /// Požadavek na zpracování
        /// </summary>
        public FileCopierRequest Request { get; set; }
        /// <summary>
        /// Požadavek na zastavení. Lze nastavit true, poté už nejde vrátit na false.
        /// Viz film "Hvězdná brána".
        /// </summary>
        public bool CancelProcess { get { return __CancelProcess; } set { __CancelProcess = (value || __CancelProcess); } } private bool __CancelProcess;

        public void Copy()
        {
            FileCopierRequest request = this.Request;
            if (request is null) throw new ArgumentNullException(nameof(request));
            request.CheckValidity();

            this.ContentMap = new FileContentMap(request.SourceFile, request.DestinationFile);

            _CopyFastNet();

            this.ContentMap.SaveMetadata();
        }

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

        #region Časomíra
        private void _ResetTime()
        {
            if (!__StopWatch.IsRunning)
                __StopWatch.Start();

            __StartTicks = __StopWatch.ElapsedTicks;
        }
        /// <summary>
        /// Čas v milisekundách od doby <see cref="_ResetTime"/>
        /// </summary>
        private double _TimeMilisec { get { return (1000d * (double)(__StopWatch.ElapsedTicks - __StartTicks)) / __StopWatchFreq; } }
        private System.Diagnostics.Stopwatch __StopWatch;
        private long __StartTicks;
        private double __StopWatchFreq;
        #endregion
        #region Progress event
        /// <summary>
        /// Event pro hlášení každého progresu
        /// </summary>
        public event EventHandler<FileReadProgressEventArgs> Progress;

        private void _CallProgress(ProgressStateType state)
        {
            _CallProgress(state, null, null, null, null, true);
        }
        private void _CallProgress(ProgressStateType state, long startPosition, long blockLength)
        {
            _CallProgress(state, startPosition, blockLength, null, null, true);
        }
        private void _CallProgress(ProgressStateType state, long? startPosition, long? blockLength, int? errorCode, string errorDescription, bool abortOnCancel)
        {
            var args = __ProgressArgs;
            if (args is null)
            {
                args = new FileReadProgressEventArgs();
                args.FileMap = this.ContentMap;
                __ProgressArgs = args;
            }

            args.State = state;
            if (startPosition.HasValue) args.BlockStartPosition = startPosition.Value;
            if (blockLength.HasValue) args.BlockLength = blockLength.Value;
            if (errorCode.HasValue) args.ErrorCode = errorCode.Value;
            args.ErrorDescription = errorDescription;
            args.Cancel = false;

            if (this.Progress != null)
                this.Progress(this, args);

            if (abortOnCancel && (args.Cancel || this.CancelProcess))
                throw new OperationCanceledException("Operace byla zrušena uživatelem");
        }
        private FileReadProgressEventArgs __ProgressArgs;
        #endregion
        public FileContentMap ContentMap { get; private set; }





        /// <summary>
        /// Čte zdrojový soubor s chybami a zapisuje výstup, přičemž chybné oblasti vyplňuje nulami
        /// </summary>
        public void CopyWithErrorRecovery()
        {
            FileCopierRequest request = this.Request;
            if (request is null) throw new ArgumentNullException(nameof(request));
            request.CheckValidity();

            SafeFileHandle sourceHandle = null;
            SafeFileHandle destHandle = null;

            try
            {
                // Otevři zdrojový soubor
                sourceHandle = CreateFile(request.SourceFile, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN, IntPtr.Zero);

                if (sourceHandle.IsInvalid)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), $"Nelze otevřít zdrojový soubor '{request.SourceFile}'");
                }

                // Otevři cílový soubor (vytvoř nebo přepiš)
                destHandle = CreateFile(request.DestinationFile, GENERIC_WRITE, FILE_SHARE_NONE, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING, IntPtr.Zero);

                if (destHandle.IsInvalid)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), $"Nelze vytvořit cílový soubor '{request.DestinationFile}'");
                }

                // Zjisti velikost zdrojového souboru
                string name = System.IO.Path.GetFileName(request.SourceFile);
                long fileSize = GetFileSize(sourceHandle, IntPtr.Zero);
                if (fileSize <= 0)
                {
                    throw new InvalidOperationException($"Zdrojový soubor '{request.SourceFile}' je prázdný nebo jeho velikost je nezpracovatelná");
                }

                // Mapa celého souboru:
                var fileMap = new FileContentMap(name, fileSize);

                var progressArgs = new FileReadProgressEventArgs { State = ProgressStateType.Begin, FileMap = fileMap, ErrorCode = 0, ErrorDescription = "Začíná čtení..." };
                OnProgress(progressArgs, true);

                byte[] readBuffer = new byte[request.BufferSize];
                byte[] writeBuffer = new byte[request.BufferSize];
                long currentPosition = 0;
                int consecutiveErrors = 0;

                while (currentPosition < fileSize)
                {
                    // Zahájení bloku:
                    fileMap.Processed = currentPosition;
                    progressArgs = new FileReadProgressEventArgs { State = ProgressStateType.Read, FileMap = fileMap, BlockStartPosition = currentPosition, BlockLength = 0, ErrorCode = 0, ErrorDescription = "Probíhá čtení..." };
                    OnProgress(progressArgs, true);

                    uint bytesToRead = (uint)Math.Min(request.BufferSize, fileSize - currentPosition);
                    uint bytesRead = 0;

                    // Pokus o čtení
                    bool readSuccess = ReadFile(sourceHandle, readBuffer, bytesToRead, out bytesRead, IntPtr.Zero);

                    if (readSuccess && bytesRead > 0)
                    {
                        // ✅ Úspěšné čtení - zkopíruj data
                        uint bytesWritten = 0;
                        bool writeSuccess = WriteFile(destHandle, readBuffer, bytesRead, out bytesWritten, IntPtr.Zero);

                        if (!writeSuccess || bytesWritten != bytesRead)
                        {
                            int writeErrorCode = Marshal.GetLastWin32Error();
                            throw new System.IO.IOException($"Chyba při zápisu na pozici {currentPosition:X}: Kód {writeErrorCode}");
                        }

                        // Hlášení úspěchu
                        fileMap.Processed = currentPosition + bytesRead;
                        var successArgs = new FileReadProgressEventArgs { State = ProgressStateType.Written, FileMap = fileMap, BlockStartPosition = currentPosition, BlockLength = bytesRead, ErrorCode = 0, ErrorDescription = $"OK - Přečteno {bytesRead} bytů" };
                        OnProgress(successArgs, true);

                        currentPosition += bytesRead;
                        consecutiveErrors = 0;
                    }
                    else
                    {
                        // ❌ Chyba čtení
                        int errorCode = Marshal.GetLastWin32Error();
                        string errorDesc = Win32FileErrors.GetErrorDescription(errorCode);

                        // Hlášení chyby
                        // fileMap.FileBlocks.Add(currentPosition, new BlockInfo(BlockStateType.None, 0L, 0L)); //   (BlockStateType.CotainsError, currentPosition, (long)bytesRead));
                        fileMap.Processed = currentPosition + bytesRead;
                        var errorArgs = new FileReadProgressEventArgs { State = ProgressStateType.Error, FileMap = fileMap, BlockStartPosition = currentPosition, BlockLength = request.SkipOnError, ErrorCode = errorCode, ErrorDescription = errorDesc };
                        OnProgress(errorArgs, true);

                        // Vyplň nulami
                        Array.Clear(writeBuffer, 0, writeBuffer.Length);

                        uint zeroBytesToWrite = (uint)Math.Min(request.SkipOnError, fileSize - currentPosition);
                        uint zeroesWritten = 0;

                        bool zeroWriteSuccess = WriteFile(destHandle, writeBuffer, zeroBytesToWrite, out zeroesWritten, IntPtr.Zero);

                        if (!zeroWriteSuccess || zeroesWritten != zeroBytesToWrite)
                        {
                            throw new System.IO.IOException($"Kritická chyba: Nelze zapsat nulový blok obnovení na pozici {currentPosition:X}");
                        }

                        // Přeskoč v zdrojovém souboru
                        currentPosition += request.SkipOnError;

                        if (!SetFilePointer(sourceHandle, currentPosition, IntPtr.Zero, FILE_BEGIN))
                        {
                            throw new System.IO.IOException($"Kritická chyba: Nelze se přesunout na pozici {currentPosition:X} ve zdrojovém souboru");
                        }

                        consecutiveErrors++;

                        // Bezpečnost - příliš mnoho chyb
                        if (consecutiveErrors > request.AbortAfterContinueErrors)
                        {
                            throw new System.IO.IOException($"Příliš mnoho po sobě jdoucích chyb ({consecutiveErrors}) - disk je pravděpodobně vážně poškozen");
                        }
                    }
                }

                // Finální hlášení - hotovo
                fileMap.Processed = fileSize;
                var finalArgs = new FileReadProgressEventArgs { State = ProgressStateType.End, FileMap = fileMap, BlockStartPosition = fileSize, BlockLength = 0, ErrorCode = 0, ErrorDescription = "Hotovo - Soubor úspěšně zkopírován" };
                OnProgress(finalArgs, false);
            }
            finally
            {
                sourceHandle?.Dispose();
                destHandle?.Dispose();
            }
        }

        /// <summary>
        /// Vyvolá event Progress
        /// </summary>
        protected virtual void OnProgress(FileReadProgressEventArgs e, bool abortOnCancel)
        {
            Progress?.Invoke(this, e);
            if (abortOnCancel && (e.Cancel || this.CancelProcess))
                throw new OperationCanceledException("Operace byla zrušena uživatelem");
        }
    }
    #region class FileReaderRequest : vstupní zadání pro kopírku
    /// <summary>
    /// FileReaderRequest : vstupní zadání pro kopírku
    /// </summary>
    public class FileCopierRequest
    {
        public FileCopierRequest()
        {
            FastCopyBlockSize = 16384;
            BufferSize = 4096;
            SkipOnError = 512;
            AbortAfterContinueErrors = 1024;
        }
        public void CheckValidity()
        {
            if (String.IsNullOrEmpty(SourceFile)) throw new ArgumentNullException(nameof(FileCopierRequest.SourceFile));
            if (String.IsNullOrEmpty(DestinationFile) && !String.IsNullOrEmpty(DestinationPath))
                DestinationFile = System.IO.Path.Combine(DestinationPath, System.IO.Path.GetFileName(SourceFile));

            if (String.IsNullOrEmpty(DestinationFile)) throw new ArgumentNullException(nameof(FileCopierRequest.DestinationFile));

            FastCopyBlockSize = validate(FastCopyBlockSize, 4096, 65536, 4096);
            BufferSize = validate(BufferSize, 1024, 16384, 4096);
            SkipOnError = validate(SkipOnError, 256, 1024, 256);
            AbortAfterContinueErrors = validate(AbortAfterContinueErrors, 2, 4096, 1);

            int validate(int value, int min, int max, int modulo)
            {
                if (value < min) value = min;
                if (value > max) value = max;
                if ((value % modulo) != 0) value = ((value / modulo) + 1) * modulo;
                return value;
            }
        }
        public string SourceFile { get; set; }
        public string DestinationPath { get; set; }
        public string DestinationFile { get; set; }
        public int FastCopyBlockSize { get; set; }
        public int BufferSize { get; set; } 
        public int SkipOnError { get; set; }
        public int AbortAfterContinueErrors { get; set; }
    }
    #endregion
    #region class FileReadProgressEventArgs : Třída argumentu
    /// <summary>
    /// Argument pro event progress - obsahuje informace o aktuálním stavu čtení
    /// </summary>
    public class FileReadProgressEventArgs : EventArgs
    {
        public FileReadProgressEventArgs()
        {
            Cancel = false;
            ErrorCode = 0;
            ErrorDescription = "OK";
        }
        /// <summary>Stav progresu</summary>
        public ProgressStateType State { get; set; }
        /// <summary>Mapa souboru</summary>
        public FileContentMap FileMap { get; set; }
        /// <summary>Jméno souboru bez adresáře</summary>
        public string Name { get { return FileMap.Name; } }
        /// <summary>Celková délka souboru v bajtech</summary>
        public long TotalFileSize { get { return FileMap.TotalFileSize; } }
        /// <summary>Pozice v souboru, kde začíná aktuálně zpracovaný blok</summary>
        public long BlockStartPosition { get; set; }
        /// <summary>Délka aktuálně zpracovaného bloku v bajtech</summary>
        public long BlockLength { get; set; }
        /// <summary>Pozice v souboru, kde končí aktuálně zpracovaný blok</summary>
        public long BlockEndPosition { get { return BlockStartPosition + BlockLength; } }
        /// <summary>Win32 error kód (0 = bez chyby)</summary>
        public int ErrorCode { get; set; }
        /// <summary>Textový popis chyby</summary>
        public string ErrorDescription { get; set; }
        /// <summary>True pokud byl aktuální blok správně přečten, False pokud obsahuje chybu</summary>
        public bool IsSuccessful => ErrorCode == 0;
        /// <summary>Poměr dokončení (0.00 ÷ 1.00)</summary>
        public double ProgressRatio => TotalFileSize > 0L ? ((double)(BlockStartPosition + BlockLength) / (double)TotalFileSize) : 0d;
        public string StatusInfo
        {
            get
            {
                double percent = Math.Round(100d * ProgressRatio, 1);
                long currentKb = BlockEndPosition / 1024L;
                long totalKb = TotalFileSize / 1024L;
                if (currentKb > totalKb) currentKb = totalKb;
                return $"{percent:F1}%   [ {currentKb:N0} KB / {totalKb:N0} KB ]";
            }
        }
        /// <summary>Stornovat proces</summary>
        public bool Cancel { get; set; }
    }
    public enum ProgressStateType
    {
        Begin,
        Read,
        Written,
        Flush,
        Error,
        End
    }
    #endregion
    #region class Win32FileErrors
    /// <summary>
    /// Win32 chyby
    /// </summary>
    internal class Win32FileErrors
    {
        // Mapa chybových kódů na jejich popis
        public static readonly Dictionary<int, string> FileReadErrors = new Dictionary<int, string>
        {
            // Nejčastější chyby při čtení disku
            { 0, "ERROR_SUCCESS - Bez chyby" },
            { 2, "ERROR_FILE_NOT_FOUND - Soubor nenalezen" },
            { 3, "ERROR_PATH_NOT_FOUND - Cesta nenalezena" },
            { 4, "ERROR_TOO_MANY_OPEN_FILES - Příliš mnoho otevřených souborů" },
            { 5, "ERROR_ACCESS_DENIED - Přístup odepřen (chybí oprávnění)" },
            { 6, "ERROR_INVALID_HANDLE - Neplatný handle" },
            { 32, "ERROR_SHARING_VIOLATION - Soubor již používán jiným procesem" },
            { 33, "ERROR_LOCK_VIOLATION - Soubor je zamčen" },
        
            // KRITICKÉ - hardwarové chyby
            { 23, "ERROR_CRC - Chyba CRC (špatný sektor, poškozená data)" },
            { 1117, "ERROR_IO_DEVICE - Chyba vstupně-výstupního zařízení (HDD problém)" },
            { 1148, "ERROR_READ_FAULT - Chyba čtení (kontroler, kabel, disk)" },
            { 1149, "ERROR_WRITE_FAULT - Chyba zápisu (kontroler, disk)" },
            { 1159, "ERROR_SECTOR_NOT_FOUND - Sektor nenalezen (fyzické poškození)" },
            { 1200, "ERROR_NO_SUCH_DEVICE - Zařízení neexistuje (disk odpojený)" },
        
            // Timeout a problematické situace
            { 121, "ERROR_SEM_TIMEOUT - Timeout (dlouhé čekání)" },
            { 1312, "ERROR_INVALID_USER_BUFFER - Neplatný buffer" },
        
            // Ostatní relevantní chyby
            { 24, "ERROR_SHARING_BUFFER_EXCEEDED - Překročen limit sdíleného bufferu" },
            { 1004, "ERROR_INVALID_ENVIRONMENT - Neplatné prostředí" },
            { 112, "ERROR_DISK_FULL - Disk je plný (nejen při čtení)" },
        };

        /// <summary>
        /// Vrátí popis chyby na základě Win32 error kódu
        /// </summary>
        public static string GetErrorDescription(int errorCode)
        {
            if (FileReadErrors.TryGetValue(errorCode, out string description))
            {
                return description;
            }
            return $"ERROR_{errorCode} - Neznámá chyba";
        }

        /// <summary>
        /// Zjistí, zda se jedná o kritickou hardwarovou chybu
        /// </summary>
        public static bool IsHardwareError(int errorCode)
        {
            return errorCode == 23 ||      // CRC
                   errorCode == 1117 ||    // I/O Device
                   errorCode == 1148 ||    // Read Fault
                   errorCode == 1149 ||    // Write Fault
                   errorCode == 1159;      // Sector Not Found
        }

        /// <summary>
        /// Zjistí, zda se jedná o chybu, kterou lze přeskočit
        /// </summary>
        public static bool IsSkippableError(int errorCode)
        {
            return errorCode == 23 ||      // CRC - lze přeskočit sektor
                   errorCode == 1148 ||    // Read Fault - lze přeskočit
                   errorCode == 1159;      // Sector Not Found - lze přeskočit
        }


        /*

Nejdůležitější chyby pro tvůj případ:
Kód	Popis	Řešení
23	CRC chyba	✅ Přeskoč sektor
1117	I/O Device Error	✅ Přeskoč sektor
1148	Read Fault	✅ Přeskoč sektor
1159	Sector Not Found	✅ Přeskoč sektor
1200	Device Disconnected	❌ Zastavit čtení


*/
    }
    #endregion
}
