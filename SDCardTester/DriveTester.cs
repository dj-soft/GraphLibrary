using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace SDCardTester
{
    /// <summary>
    /// Tester zápisu a čtení na disk
    /// </summary>
    public class DriveTester
    {
        #region Konstrukce a public rozhraní
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DriveTester()
        { }
        protected System.IO.DriveInfo _Drive;
        /// <summary>
        /// Požádá o provedení testu zápisu a čtení daného disku
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="doRead"></param>
        /// <param name="doSave"></param>
        public void BeginTest(System.IO.DriveInfo drive, bool doSave, bool doRead)
        {
            if (drive != null && drive.IsReady && !TestRunning)
                StartTest(drive, doSave, doRead);
            else
                CallTestDone();
        }
        /// <summary>
        /// Požádá o zastavení běhu testu
        /// </summary>
        public void StopTest()
        {
            if (TestRunning)
                TestStopping = true;
        }
        /// <summary>
        /// Drive pro analýzu
        /// </summary>
        public System.IO.DriveInfo Drive { get { return _Drive; } }
        /// <summary>
        /// Test právě běží?
        /// </summary>
        public bool TestRunning { get; private set; }
        /// <summary>
        /// Je vydán požadavek na zastavení testu
        /// </summary>
        public bool TestStopping { get; private set; }
        /// <summary>
        /// Časový interval, po jehož uplynutí se může opakovaně volat událost <see cref="TestStep"/>.
        /// </summary>
        public TimeSpan TestStepTime { get; set; }
        /// <summary>
        /// Událost vyvolaná po změně hodnot v <see cref="FileGroup"/>, mezi dvěma událostmi bude čas nejméně <see cref="TestStepTime"/> i kdyby změny nastaly častěji.
        /// </summary>
        public event EventHandler TestStep;
        /// <summary>
        /// Událost vyvolaná po jakémkoli doběhnutí testu, i po chybách.
        /// </summary>
        public event EventHandler TestDone;
        #endregion
        #region Privátní řízení běhu
        /// <summary>
        /// Zahájení testu, zde v threadu volajícího
        /// </summary>
        /// <param name="drive"></param>
        protected void StartTest(System.IO.DriveInfo drive, bool doSave, bool doRead)
        {
            TestRunning = true;
            TestStopping = false;
            _Drive = drive;
            DoTestSave = doSave;
            DoTestRead = doRead;
            Task.Factory.StartNew(RunTest);
        }
        /// <summary>
        /// Zahájení testu, zde již v threadu Working
        /// </summary>
        protected void RunTest()
        {
            LastStepTime = null;
            string testDir = null;
            if (DoTestSave) RunTestSave(ref testDir);
            if (DoTestRead) RunTestRead(ref testDir);
        }
        /// <summary>
        /// Čas posledního hlášení změny
        /// </summary>
        protected DateTime? LastStepTime;
        /// <summary>
        /// Vyvolá událost <see cref="TestStep"/>, pokud je odpovídající čas
        /// </summary>
        /// <param name="queueCount"></param>
        /// <param name="force"></param>
        protected void CallTestStep(int queueCount, bool force = false)
        {
            var nowTime = DateTime.Now;
            var lastTime = LastStepTime;
            var stepTime = TestStepTime;
            if (force || !lastTime.HasValue || stepTime.TotalMilliseconds <= 0d || (lastTime.HasValue && ((TimeSpan)(nowTime - lastTime.Value) >= stepTime)))
            {
                TestStep?.Invoke(this, EventArgs.Empty);
                LastStepTime = nowTime;
            }
        }
        /// <summary>
        /// Vyvolá událost <see cref="TestDone"/>
        /// </summary>
        protected void CallTestDone()
        {
            TestDone?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        #region Vlastní test zápisu
        /// <summary>
        /// Fyzický test zápisu
        /// </summary>
        protected void RunTestSave(ref string testDir)
        {
            if (TestStopping) return;
            if (!CanWriteFile(0L)) return;                                               // Pokud nemám na disku ani rezervu volného místa...

            if (testDir is null) testDir = GetTestDirectory(true);
            if (testDir is null) return;

            // Na otestování disku 8TB při využití jednoho adresáře připouštíme nejvýše 4096 souborů;
            // Nejprve vepíšeme 512 souborů o velikosti 4096 B = 2 MB (2 097 152 B);
            // Zbývajících 3584 souborů bude mít velikost (celková velikost disku / 3584) zarovnáno na 4KB bloky, pro 8TB disk tedy velikost = 2 454 265 856 = 2.5 GB
            long totalSize = _Drive.TotalSize;
            long longFilesLength = totalSize / 3584L;                                    // Délka velkého souboru tak, aby jich v jednom adresáři na prázdném disku bylo max 3584 souborů
            longFilesLength = (longFilesLength / ShortFilesLength) * ShortFilesLength;   // Zarovnáno na 4KB bloky
            long minLength = (256L * 1024L * 1024L);
            if (longFilesLength < minLength) longFilesLength = minLength;                // Ne menší než 256 MB, aby bylo možno měřit rychlost zápisu i čtení, když někdy je deklarováno 80 MB/sec

            int fileNumber = GetNextFileNumber(testDir);
            while (!TestStopping)
            {
                if (IsShortFile(fileNumber))
                {
                    RunTestSaveOneShort(testDir, fileNumber, ShortFilesLength);
                }
                else
                {
                    if (!CanWriteFile(longFilesLength)) break;
                    break;
                    RunTestSaveOneLong(testDir, fileNumber, longFilesLength);
                }
                fileNumber++;
            }
        }
        protected void RunTestSaveOneShort(string testDir, int fileNumber, long targetLength)
        {
            string fileName = GetFileName(testDir, fileNumber);
            int bufferIndex = 0;
            using (System.IO.FileStream fst = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                long currentLength = 0L;
                var data = GetData(fileNumber, bufferIndex);
                bool doWrite = true;
                while (doWrite)
                {
                    var buffer = data;           // Nezbytnost!!! : protože 'buffer' odchází do FileStreamu k zápisu, a současně (protože .WriteAsync) se do jiné proměnné 'data' připravuje nový obsah pro další cyklus
                    int oneLength = buffer.Length;
                    var task = fst.WriteAsync(buffer , 0, oneLength);
                    currentLength += (long)oneLength;
                    doWrite = currentLength < targetLength;
                    if (doWrite)
                        data = GetData(fileNumber, ++bufferIndex);
                    task.Wait();
                }
                fst.Flush();
                fst.Close();
            }
        }
        protected void RunTestSaveOneLong(string testDir, int fileNumber, long length)
        {
        }

        /// <summary>
        /// Vrátí následující číslo souboru tak, aby navazovalo na soubory již existující
        /// </summary>
        /// <param name="testDir"></param>
        /// <returns></returns>
        protected int GetNextFileNumber(string testDir)
        {
            int lastNumber = 0;
            var files = System.IO.Directory.GetFiles(testDir, "*.*");
            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(file).ToLower();
                if (name.StartsWith("test~") && name.Length == 10 && Int32.TryParse(name.Substring(5,5), out int number))
                {
                    if (number > lastNumber)
                        lastNumber = number;
                }
            }
            return lastNumber + 1;
        }
        /// <summary>
        /// Vrátí číslo získané z názvu testovacího souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected int GetFileNumber(string fileName)
        {
            if (!String.IsNullOrEmpty(fileName))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower();
                if (name.StartsWith("test~") && name.Length == 10 && Int32.TryParse(name.Substring(5, 5), out int number)) 
                    return number;
            }
            return 0;
        }
        /// <summary>
        /// Vrátí jméno souboru podle pravidel pro dané číslo
        /// </summary>
        /// <param name="testDir"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        protected string GetFileName(string testDir, int number)
        {
            string name = "test~" + number.ToString("00000") + ".tmp";
            return System.IO.Path.Combine(testDir, name);
        }
        /// <summary>
        /// Mohu zapsat soubor dané délky? Máme na disku dost místa?
        /// </summary>
        /// <param name="fileLength"></param>
        /// <returns></returns>
        protected bool CanWriteFile(long fileLength)
        {
            System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(_Drive.Name);        // Refresh
            return (driveInfo.AvailableFreeSpace >= (fileLength + LongFileReserve));     // Vrátím true, když volného místa je víc než (velikost souboru plus rezerva)
        }
        /// <summary>
        /// Provést test zápisu
        /// </summary>
        protected bool DoTestSave;
        #endregion
        #region Vlastní test čtení
        /// <summary>
        /// Fyzický test čtení
        /// </summary>
        protected void RunTestRead(ref string testDir)
        {
            if (TestStopping) return;
            if (testDir is null) testDir = GetTestDirectory(true);
            if (testDir is null) return;

        }
        /// <summary>
        /// Provést test čtení
        /// </summary>
        protected bool DoTestRead;
        #endregion
        #region Generátor dat, adresáře, souborů
        /// <summary>
        /// Vygeneruje blok dat do daného čísla souboru (počínaje 1) do daného bloku (počínaje 0), v délce <see cref="BufferLength"/>.
        /// </summary>
        /// <param name="fileNumber"></param>
        /// <param name="bufferIndex"></param>
        /// <returns></returns>
        protected byte[] GetData(int fileNumber, int bufferIndex)
        {
            byte[] buffer = new byte[BufferLength];
            int sample = (15 * fileNumber + 7 * bufferIndex) % 256;
            int step = ((fileNumber % 5) + 1) * ((bufferIndex % 3) + 1);
            for (int i = 0; i < BufferLength; i++)
            {
                buffer[i] = (byte)sample;
                sample = (sample + step) % 256;
            }
            return buffer;
        }
        protected string GetTestDirectory(bool canCreate)
        {
            string dirName = System.IO.Path.Combine(this._Drive.RootDirectory.FullName, "_TestDir.1968");
            var dirInfo = new System.IO.DirectoryInfo(dirName);
            try
            {
                if (dirInfo.Exists) return dirName;
                if (!canCreate) return null;
                dirInfo.Create();
                dirInfo.Refresh();
                if (!dirInfo.Exists) return null;
                return dirName;
            }
            catch { }
            return null;
        }
        /// <summary>
        /// Vrátí true, pokud soubor s daným číslem bude "krátký"
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        protected bool IsShortFile(int number) { return (number <= ShortFilesCount); }
        protected const long ShortFilesLength = 4096L;
        protected const int ShortFilesCount = 512;
        protected const long LongFileReserve = 32L * 1024L * 1024L;
        protected const int BufferLength = 4096;
        #endregion
    }
}
