using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace DjSoftSDCardTester
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
        {
            InitStopwatch();
        }
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
            RunInfoClear();
            if (DoTestSave) RunTestSave(ref testDir);
            if (DoTestRead) RunTestRead(ref testDir);
            CallTestDone();
        }
        /// <summary>
        /// Čas posledního hlášení změny
        /// </summary>
        protected DateTime? LastStepTime;
        /// <summary>
        /// Vyvolá událost <see cref="TestStep"/>, pokud je odpovídající čas
        /// </summary>
        /// <param name="force"></param>
        /// <param name="currentLength"></param>
        /// <param name="startTime"></param>
        /// <param name="currentTime"></param>
        protected void CallTestStep(bool force = false, long? currentLength = null, long? startTime = null, long? currentTime = null, int? asyncOK = null, int? asyncSlow = null)
        {
            var nowTime = DateTime.Now;
            var lastTime = LastStepTime;
            var stepTime = TestStepTime;
            if (force || !lastTime.HasValue || stepTime.TotalMilliseconds <= 0d || (lastTime.HasValue && ((TimeSpan)(nowTime - lastTime.Value) >= stepTime)))
            {   // Je čas na volání eventu:

                // Do public properites dáme informace o hotových průchodech:
                TimeInfoSaveShort = TimeInfoSaveShortDone;
                TimeInfoSaveLong = TimeInfoSaveLongDone;
                TimeInfoReadShort = TimeInfoReadShortDone;
                TimeInfoReadLong = TimeInfoReadLongDone;

                if (currentLength.HasValue && startTime.HasValue && currentTime.HasValue && asyncOK.HasValue && asyncSlow.HasValue)
                {   // Máme k dispozici "rozpracovaná data" => přičteme je k hodnotám *Done a uložíme do public properties:
                    TestPhase testPhase = this.TimeInfoCurrentPhase;
                    decimal elapsedTime = this.GetSeconds(startTime.Value, currentTime.Value);
                    // Do odpovídající public property vložím součet hodnoty Done + rozpracované hodnoty daného režimu:
                    switch (testPhase)
                    {
                        case TestPhase.SaveShortFile:
                            TimeInfoSaveShort = new FileTimeInfo(TimeInfoSaveShortDone, 0, currentLength.Value, elapsedTime, asyncOK.Value, asyncSlow.Value);
                            break;
                        case TestPhase.SaveLongFile:
                            TimeInfoSaveLong = new FileTimeInfo(TimeInfoSaveLongDone, 0, currentLength.Value, elapsedTime, asyncOK.Value, asyncSlow.Value);
                            break;
                        case TestPhase.ReadShortFile:
                            TimeInfoReadShort = new FileTimeInfo(TimeInfoReadShortDone, 0, currentLength.Value, elapsedTime, asyncOK.Value, asyncSlow.Value);
                            break;
                        case TestPhase.ReadLongFile:
                            TimeInfoReadLong = new FileTimeInfo(TimeInfoReadLongDone, 0, currentLength.Value, elapsedTime, asyncOK.Value, asyncSlow.Value);
                            break;
                    }
                }

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
        /// <summary>
        /// Vynuluje data o výsledných časech
        /// </summary>
        protected void RunInfoClear()
        {
            TimeInfoSaveShort = new FileTimeInfo(TestPhase.SaveShortFile);
            TimeInfoSaveLong = new FileTimeInfo(TestPhase.SaveLongFile);
            TimeInfoReadShort = new FileTimeInfo(TestPhase.ReadShortFile);
            TimeInfoReadLong = new FileTimeInfo(TestPhase.ReadLongFile);
            TimeInfoSaveShortDone = new FileTimeInfo(TestPhase.SaveShortFile);
            TimeInfoSaveLongDone = new FileTimeInfo(TestPhase.SaveLongFile);
            TimeInfoReadShortDone = new FileTimeInfo(TestPhase.ReadShortFile);
            TimeInfoReadLongDone = new FileTimeInfo(TestPhase.ReadLongFile);
        }
        /// <summary>
        /// Aktuálně běžící fáze testu
        /// </summary>
        public TestPhase TimeInfoCurrentPhase { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Save, souborů typu Short.
        /// </summary>
        public FileTimeInfo TimeInfoSaveShort { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Save, souborů typu Long.
        /// </summary>
        public FileTimeInfo TimeInfoSaveLong { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Read, souborů typu Short.
        /// </summary>
        public FileTimeInfo TimeInfoReadShort { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Read, souborů typu Long.
        /// </summary>
        public FileTimeInfo TimeInfoReadLong { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Save, souborů typu Short, které jsou již dokončeny.
        /// K nim se přičte informace o aktuálně probíhajícím přenosu a výsledek se vloží do <see cref="TimeInfoSaveShort"/> = aktuální žhavá hodnota k zobrazení.
        /// </summary>
        protected FileTimeInfo TimeInfoSaveShortDone { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Save, souborů typu Long, které jsou již dokončeny.
        /// K nim se přičte informace o aktuálně probíhajícím přenosu a výsledek se vloží do <see cref="TimeInfoSaveShort"/> = aktuální žhavá hodnota k zobrazení.
        /// </summary>
        protected FileTimeInfo TimeInfoSaveLongDone { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Read, souborů typu Short, které jsou již dokončeny.
        /// K nim se přičte informace o aktuálně probíhajícím přenosu a výsledek se vloží do <see cref="TimeInfoSaveShort"/> = aktuální žhavá hodnota k zobrazení.
        /// </summary>
        protected FileTimeInfo TimeInfoReadShortDone { get; private set; }
        /// <summary>
        /// Informace o přenosech typu Read, souborů typu Long, které jsou již dokončeny.
        /// K nim se přičte informace o aktuálně probíhajícím přenosu a výsledek se vloží do <see cref="TimeInfoSaveShort"/> = aktuální žhavá hodnota k zobrazení.
        /// </summary>
        protected FileTimeInfo TimeInfoReadLongDone { get; private set; }
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
            long longFilesLength = totalSize / LongFilesMaxCount;                           // Délka velkého souboru tak, aby jich v jednom adresáři na prázdném disku bylo celkem max 4096 souborů
            longFilesLength = (longFilesLength / ShortFilesLength) * ShortFilesLength;   // Zarovnáno na 4KB bloky
            long longFilesMinLength = LongFilesMinLength;
            if (longFilesLength < longFilesMinLength) longFilesLength = longFilesMinLength;        // Velké soubory by neměly být menší než 16 MB, aby bylo možno měřit rychlost zápisu i čtení, když někdy je deklarováno 80 MB/sec

            int fileNumber = GetNextFileNumber(testDir);
            TimeInfoCurrentPhase = (IsShortFile(fileNumber) ? TestPhase.SaveShortFile : TestPhase.SaveLongFile);
            CallTestStep(true);
            this.RestartStopwatch();
            try
            {
                while (!TestStopping)
                {
                    if (IsShortFile(fileNumber))
                    {
                        var timeInfoSaveShort = RunTestSaveOneFile(testDir, fileNumber, out string fileName, ShortFilesLength, TestPhase.SaveShortFile);
                        TimeInfoSaveShortDone.Add(timeInfoSaveShort);
                    }
                    else
                    {
                        if (!CanWriteFile(longFilesLength)) break;
                        var timeInfoSaveLong = RunTestSaveOneFile(testDir, fileNumber, out string fileName, longFilesLength, TestPhase.SaveLongFile);
                        TimeInfoSaveLongDone.Add(timeInfoSaveLong);
                    }
                    fileNumber++;
                }
                TimeInfoCurrentPhase = TestPhase.None;
            }
            catch (Exception exc) { }
            CallTestStep(true);
        }
        /// <summary>
        /// Zapíše jeden soubor daného čísla, dané délky a změří čas a vrátí časovou položku.
        /// </summary>
        /// <param name="testDir"></param>
        /// <param name="fileNumber"></param>
        /// <param name="fileName">Out jméno souboru pro případný test čtení</param>
        /// <param name="targetLength"></param>
        /// <param name="phase">Fáze, určuje zápis Short/Long souboru, bude vepsána do <see cref="TimeInfoCurrentPhase"/></param>
        /// <returns></returns>
        protected FileTimeInfo RunTestSaveOneFile(string testDir, int fileNumber, out string fileName, long targetLength, TestPhase phase)
        {
            fileName = null;
            if (TestStopping) return null;

            string saveFileName = GetFileName(testDir, fileNumber);
            if (TestStopping) return null;
            fileName = saveFileName;

            TimeInfoCurrentPhase = phase;
            return RunTestSaveOneFileWriteAsync(testDir, fileNumber, fileName, targetLength, phase);
        }

        protected FileTimeInfo RunTestSaveOneFileWriteAsync(string testDir, int fileNumber, string fileName, long targetLength, TestPhase phase)
        {
            long startTime = this.CurrentTime;
            long currentLength = 0L;
            int asyncOK = 0;
            int asyncSlow = 0;
            decimal asyncTimeOk = 0m;
            int bufferIndex = 0;
            int bufferLength = 0;
            using (System.IO.FileStream fst = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                bufferLength = GetBufferLength(phase, currentLength, targetLength);
                var data = GetData(fileNumber, bufferIndex, bufferLength);
                bool doWrite = true;
                while (doWrite && !TestStopping)
                {
                    var buffer = data;           // Nezbytnost!!! : protože 'buffer' odchází do FileStreamu k zápisu, a současně (protože .WriteAsync) se do jiné proměnné 'data' připravuje nový obsah pro další cyklus
                    int oneLength = buffer.Length;
                    using (var task = fst.WriteAsync(buffer, 0, oneLength))    // Zde asynchronně začíná zápis dat do souboru
                    {
                        currentLength += (long)oneLength;
                        doWrite = currentLength < targetLength;
                        if (doWrite)
                        {
                            bufferLength = GetBufferLength(phase, currentLength, targetLength);
                            data = GetData(fileNumber, ++bufferIndex, bufferLength);         // A zatímco se do souboru v jiném threadu zapisuje, my si zde připravujeme data do dalšího kola zápisu.
                        }
                        if (task.IsCompleted)
                        {   // Problém: jsme pomalí! Než jsme si stihli připravit data (data = GetData()), tak se předchozí buffer stihl zapsat na cílový disk.
                            asyncSlow++;
                        }
                        else
                        {   // Sem bychom měli dojít vždy = značí to, že data pro příští buffer jsme připravili včas, a zápis souboru ještě nedoběhl = stíháme data generovat rychleji, než se zapisují...
                            long waitStart = this.CurrentTime;                 // Pro zajímavost: jak dlouho čekáme?
                            asyncOK++;
                            task.Wait();                                       // Počkáme na dokončení zápisu bufferu do souboru...
                            long waitDone = this.CurrentTime;
                            decimal waitTime = this.GetSeconds(waitStart, waitDone);     // Sekundy čekání na dokončení Write jednoho bufferu.  Sem bychom měli dojít vždy.
                            asyncTimeOk += waitTime;
                        }
                    }

                    var currentTime = this.CurrentTime;
                    CallTestStep(false, currentLength, startTime, currentTime, asyncOK, asyncSlow);
                }
                fst.Flush();
                fst.Close();
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(phase, 1, currentLength, elapsedTime, asyncOK, asyncSlow);
        }
        protected FileTimeInfo RunTestSaveOneFileBeginWrite(string testDir, int fileNumber, string fileName, long targetLength, TestPhase phase)
        {
            long startTime = this.CurrentTime;
            long currentLength = 0L;
            int asyncOK = 0;
            int asyncSlow = 0;
            decimal asyncTimeOk = 0m;
            int bufferIndex = 0;
            int bufferLength = 0;
            using (System.IO.FileStream fst = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                bufferLength = GetBufferLength(phase, currentLength, targetLength);
                var data = GetData(fileNumber, bufferIndex, bufferLength);
                bool doWrite = true;
                while (doWrite && !TestStopping)
                {
                    var buffer = data;           // Nezbytnost!!! : protože 'buffer' odchází do FileStreamu k zápisu, a současně (protože .WriteAsync) se do jiné proměnné 'data' připravuje nový obsah pro další cyklus
                    int oneLength = buffer.Length;

                    var iAsync = fst.BeginWrite(buffer, 0, oneLength, null, null);
                    currentLength += (long)oneLength;
                    doWrite = currentLength < targetLength;
                    if (doWrite)
                    {
                        bufferLength = GetBufferLength(phase, currentLength, targetLength);
                        data = GetData(fileNumber, ++bufferIndex, bufferLength);         // A zatímco se do souboru v jiném threadu zapisuje, my si zde připravujeme data do dalšího kola zápisu.
                    }

                    if (iAsync.IsCompleted)
                    {   // Problém: jsme pomalí! Než jsme si stihli připravit data (data = GetData()), tak se předchozí buffer stihl zapsat na cílový disk.
                        asyncSlow++;
                    }
                    else
                    {   // Sem bychom měli dojít vždy = značí to, že data pro příští buffer jsme připravili včas, a zápis souboru ještě nedoběhl = stíháme data generovat rychleji, než se zapisují...
                        asyncOK++;
                        iAsync.AsyncWaitHandle.WaitOne(1000);
                    }

                    var currentTime = this.CurrentTime;
                    CallTestStep(false, currentLength, startTime, currentTime, asyncOK, asyncSlow);
                }
                fst.Flush();
                fst.Close();
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(phase, 1, currentLength, elapsedTime, asyncOK, asyncSlow);
        }
        protected FileTimeInfo RunTestSaveOneFileBinaryStream(string testDir, int fileNumber, string fileName, long targetLength, TestPhase phase)
        {
            long startTime = this.CurrentTime;
            int asyncOK = 0;
            int asyncSlow = 0;
            int bufferIndex = 0;
            int bufferLength = 0;
            long currentLength = 0L;
            bufferLength = GetBufferLength(phase, currentLength, targetLength);
            var data = GetData(fileNumber, bufferIndex, bufferLength);
            
            using (var memoryStream = new System.IO.MemoryStream(data))
            using (var fileStream = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write))
            {
                memoryStream.CopyToAsync(fileStream);
                asyncOK++;
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(phase, 1, currentLength, elapsedTime, asyncOK, asyncSlow);
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
        #region Přesná časomíra
        /// <summary>
        /// Inicializuje časomíru
        /// </summary>
        protected void InitStopwatch()
        {
            Stopwatch = new System.Diagnostics.Stopwatch();
            Frequency = (decimal)System.Diagnostics.Stopwatch.Frequency;
        }
        /// <summary>
        /// Nuluje a nastartuje časomíru
        /// </summary>
        /// <returns></returns>
        protected long RestartStopwatch()
        {
            Stopwatch.Restart();
            return Stopwatch.ElapsedTicks;
        }
        /// <summary>
        /// Aktuální čas (ticky), použije se jako parametr do metody <see cref="GetSeconds(long)"/> na konci měřeného cyklu
        /// </summary>
        protected long CurrentTime { get { return Stopwatch.ElapsedTicks; } }
        /// <summary>
        /// Vrátí počet sekund od daného počátečního času. Bez parametru = od restartu časovače = <see cref="RestartStopwatch"/>.
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        protected decimal GetSeconds(long startTime = 0L) { return GetSeconds(startTime, CurrentTime); }
        /// <summary>
        /// Vrátí počet sekund od daného počátečního do daného koncového času.
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        protected decimal GetSeconds(long startTime, long stopTime)
        {
            decimal elapsedTime = (decimal)(stopTime - startTime);
            return elapsedTime / Frequency;
        }
        /// <summary>
        /// Časovač
        /// </summary>
        protected System.Diagnostics.Stopwatch Stopwatch;
        /// <summary>
        /// Frekvence časovače = počet ticků / sekunda
        /// </summary>
        protected decimal Frequency;
        #endregion
        #region Vyhledání testovacích souborů
        /// <summary>
        /// Na daném disku vyhledá testovací soubory (podle jména adresáře a jména souborů) a vrátí jejich pole.
        /// Pokud nic neexistuje, vrátí prázdné pole.
        /// </summary>
        /// <param name="drive"></param>
        /// <returns></returns>
        public static System.IO.FileInfo[] GetTestFiles(System.IO.DriveInfo drive)
        {
            System.IO.FileInfo[] files = null;
            if (drive != null)
            {
                string dirName = System.IO.Path.Combine(drive.RootDirectory.FullName, TestDirectory);
                var dirInfo = new System.IO.DirectoryInfo(dirName);
                if (dirInfo.Exists)
                {
                    string searchPattern = FileNamePrefix + "?????" + FileNameExtension;
                    var testFiles = dirInfo.GetFiles(searchPattern);
                    files = testFiles.Where(f => IsTestFileName(f.FullName)).ToArray();
                }
            }
            if (files is null) files = new System.IO.FileInfo[0];
            return files.ToArray();
        }
        #endregion
        #region Generátor dat, tvorba adresáře, tvorba a detekce názvu souborů a jejich čísla
        /// <summary>
        /// Určí délku bufferu pro další blok zápisu
        /// </summary>
        /// <param name="phase"></param>
        /// <param name="currentLength"></param>
        /// <param name="targetLength"></param>
        /// <returns></returns>
        private int GetBufferLength(TestPhase phase, long currentLength, long targetLength)
        {
            int optimalLength = ((phase == TestPhase.SaveShortFile || phase == TestPhase.ReadShortFile) ? ShortBufferLength : LongBufferLength);
            long remainingLength = targetLength - currentLength;
            if (remainingLength > 0L && remainingLength < (long)optimalLength)
                optimalLength = (int)remainingLength;
            return optimalLength;
        }
        /// <summary>
        /// Vygeneruje blok dat do daného čísla souboru (počínaje 1) do daného bloku (počínaje 0), v délce <see cref="ShortBufferLength"/>.
        /// </summary>
        /// <param name="fileNumber"></param>
        /// <param name="bufferIndex"></param>
        /// <returns></returns>
        protected byte[] GetData(int fileNumber, int bufferIndex, int bufferLength)
        {
            byte[] buffer = new byte[bufferLength];
            int sample = (15 * fileNumber + 7 * bufferIndex) % 256;
            int step = ((fileNumber % 5) + 1) * ((bufferIndex % 3) + 1);
            for (int i = 0; i < bufferLength; i++)
            {
                buffer[i] = (byte)sample;
                sample = (sample + step) % 256;
            }
            return buffer;
        }
        /// <summary>
        /// Vrátí název testovacího adresáře
        /// </summary>
        /// <param name="canCreate"></param>
        /// <returns></returns>
        protected string GetTestDirectory(bool canCreate)
        {
            string dirName = System.IO.Path.Combine(this._Drive.RootDirectory.FullName, TestDirectory);
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
        /// Vrátí následující číslo souboru tak, aby navazovalo na soubory již existující
        /// </summary>
        /// <param name="testDir"></param>
        /// <returns></returns>
        protected static int GetNextFileNumber(string testDir)
        {
            int lastNumber = 0;
            var files = System.IO.Directory.GetFiles(testDir, "*.*");
            foreach (var file in files)
            {
                int number = GetFileNumber(file);
                if (number > lastNumber)
                    lastNumber = number;
            }
            return lastNumber + 1;
        }
        /// <summary>
        /// Vrátí číslo získané z názvu testovacího souboru. 
        /// Pokud na vstupu není testovací soubor, pak výstupem je -1.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected static int GetFileNumber(string fileName)
        {
            IsTestFileName(fileName, out var number);
            return number;
        }
        /// <summary>
        /// Vrátí jméno souboru podle pravidel pro dané číslo
        /// </summary>
        /// <param name="testDir"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        protected static string GetFileName(string testDir, int number)
        {
            string name = FileNamePrefix + number.ToString("00000") + FileNameExtension;
            return System.IO.Path.Combine(testDir, name);
        }
        /// <summary>
        /// Vrátí true, pokud daný název souboru je jménem testovacího souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected static bool IsTestFileName(string fileName)
        {
            return IsTestFileName(fileName, out var _);
        }
        /// <summary>
        /// Vrátí true, pokud daný název souboru je jménem testovacího souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNumber"></param>
        /// <returns></returns>
        protected static bool IsTestFileName(string fileName, out int fileNumber)
        {
            if (!String.IsNullOrEmpty(fileName))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower();
                var extn = System.IO.Path.GetExtension(fileName).ToLower();
                if (name.StartsWith(FileNamePrefix) && name.Length == 10 && extn == FileNameExtension && Int32.TryParse(name.Substring(5, 5), out int number) && number > 0)
                {
                    fileNumber = number;
                    return true;
                }
            }
            fileNumber = -1;
            return true;
        }
        /// <summary>
        /// Vrátí true, pokud soubor s daným číslem bude "krátký"
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        protected static bool IsShortFile(int number) { return (number <= ShortFilesCount); }
        protected const string TestDirectory = "_TestDir.1968";
        protected const string FileNamePrefix = "test~";
        protected const string FileNameExtension = ".tmp";
        protected const long ShortFilesLength = 4096L;
        protected const int ShortFilesCount = 512;
        protected const int LongFilesMaxCount = 3584;
        protected const long LongFileReserve = 32L * 1024L * 1024L;
        protected const long LongFilesMinLength = 16L * 1024L * 1024L;
        protected const int ShortBufferLength = 4096;
        protected const int LongBufferLength = 1024 * 1024;
        #endregion
        #region SubClass
        /// <summary>
        /// Třída obsahující údaje o počtu testovaných souborů, o jejich celkové délce a celkové době času testu
        /// </summary>
        public class FileTimeInfo
        {
            public FileTimeInfo(TestPhase testPhase)
            {
                Phase = testPhase;
                FileCount = 0;
                TotalLength = 0L;
                TimeTotalSec = 0m;
                AsyncOK = 0;
                AsyncSlow = 0;
            }
            public FileTimeInfo(TestPhase testPhase, int fileCount, long totalLength, decimal timeTotalSec, int asyncOK, int asyncSlow)
            {
                Phase = testPhase;
                FileCount = fileCount;
                TotalLength = totalLength;
                TimeTotalSec = timeTotalSec;
                AsyncOK = asyncOK;
                AsyncSlow = asyncSlow;
            }
            public FileTimeInfo(FileTimeInfo a, FileTimeInfo b)
            {
                Phase = a.Phase;
                FileCount = a.FileCount + b.FileCount;
                TotalLength = a.TotalLength + b.TotalLength;
                TimeTotalSec = a.TimeTotalSec + b.TimeTotalSec;
                AsyncOK = a.AsyncOK + b.AsyncOK;
                AsyncSlow = a.AsyncSlow + b.AsyncSlow;
            }
            public FileTimeInfo(FileTimeInfo a, int fileCount, long totalLength, decimal timeTotalSec, int asyncOK, int asyncSlow)
            {
                Phase = a.Phase;
                FileCount = a.FileCount + fileCount;
                TotalLength = a.TotalLength + totalLength;
                TimeTotalSec = a.TimeTotalSec + timeTotalSec;
                AsyncOK = a.AsyncOK + asyncOK;
                AsyncSlow = a.AsyncSlow + asyncSlow;
            }
            public override string ToString()
            {
                return $"Phase: {Phase}; Count: {FileCount}; Length: {TotalLength}; Time: {TimeTotalSec}";
            }
            /// <summary>
            /// Testovací fáze = typ testu
            /// </summary>
            public TestPhase Phase { get; private set; }
            /// <summary>
            /// Celkový počet souborů
            /// </summary>
            public int FileCount { get; private set; }
            /// <summary>
            /// Celková délka
            /// </summary>
            public long TotalLength { get; private set; }
            /// <summary>
            /// Celkový čas v sekundách
            /// </summary>
            public decimal TimeTotalSec { get; private set; }
            /// <summary>
            /// Počet asynchronních přístupu s časem OK
            /// </summary>
            public int AsyncOK { get; private set; }
            /// <summary>
            /// Počet asynchronních přístupu s časem Slow
            /// </summary>
            public int AsyncSlow { get; private set; }
            /// <summary>
            /// Počet asynchronních přístupu celkem
            /// </summary>
            public int AsyncCount { get { return AsyncOK + AsyncSlow; } }
            /// <summary>
            /// Do this instance přidá data z dodané instance
            /// </summary>
            /// <param name="add"></param>
            public void Add(FileTimeInfo add)
            {
                if (add != null)
                {
                    this.FileCount += add.FileCount;
                    this.TotalLength += add.TotalLength;
                    this.TimeTotalSec += add.TimeTotalSec;
                    this.AsyncOK += add.AsyncOK;
                    this.AsyncSlow += add.AsyncSlow;
                }
            }
        }
        /// <summary>
        /// Fáze testu = konkrétní typ akce
        /// </summary>
        public enum TestPhase
        {
            None,
            SaveShortFile,
            SaveLongFile,
            ReadShortFile,
            ReadLongFile
        }
        #endregion
    }
    /// <summary>
    /// Vizuální control zobrazující data z <see cref="DriveTester.FileTimeInfo"/>
    /// </summary>
    public class DriveTestTimePhaseControl : DriveResultControl
    {
        public DriveTestTimePhaseControl()
            : base()
        {
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle clientBounds = this.ClientRectangle;
            Rectangle outerBounds = new Rectangle(clientBounds.X + 1, clientBounds.Y + 1, clientBounds.Width - 2, clientBounds.Height - 2);
            Rectangle innerBounds = new Rectangle(outerBounds.X + 2, outerBounds.Y + 2, outerBounds.Width - 4, outerBounds.Height - 4);
            this.PaintBackground(e, clientBounds, outerBounds, innerBounds);
            this.PaintRatio(e, clientBounds, outerBounds, innerBounds);
            this.PaintText(e, clientBounds, outerBounds, innerBounds);
        }
        private void PaintBackground(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            Painter.PaintRectangle(e.Graphics, this.BackColor, clientBounds);            // Šedá barva okolo
            Painter.PaintRectangle(e.Graphics, this.BackgroundColor, outerBounds);       // Barevný rámeček bez 3D efektu
        }
        /// <summary>
        /// Vykreslí poměr chybných Async kroků (=počet případů, kdy algoritmus je pomalejší než zápis na disk)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="clientBounds"></param>
        /// <param name="outerBounds"></param>
        /// <param name="innerBounds"></param>
        private void PaintRatio(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            decimal asyncRatio = this.AsyncRatio;
            if (asyncRatio <= 0m) return;

            decimal widthRatio = (asyncRatio < 0.1m ? (10m * asyncRatio) : 1m);
            int widthPixel = (int)(Math.Round((widthRatio * innerBounds.Width), 0));
            if (widthPixel < 5) widthPixel = 5;
            Rectangle ratioBounds = new Rectangle(innerBounds.X, innerBounds.Bottom - 3, widthPixel, 5);
            Painter.PaintBar3D(e.Graphics, Skin.TestAsyncErrorColor, ratioBounds);
        }
        /// <summary>
        /// Vykreslí texty
        /// </summary>
        /// <param name="e"></param>
        /// <param name="clientBounds"></param>
        /// <param name="outerBounds"></param>
        /// <param name="innerBounds"></param>
        private void PaintText(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            var font = this.Font;
            bool disposeFont = false;
            if (this.IsActive)
            {
                font = new Font(font, FontStyle.Bold);
                disposeFont = true;
            }
            Painter.PaintText(e.Graphics, font, this.TitleText, this.ForeColor, innerBounds, ContentAlignment.MiddleLeft);
            Painter.PaintText(e.Graphics, font, this.TimeText, this.ForeColor, innerBounds, ContentAlignment.MiddleRight);

            if (disposeFont)
                font.Dispose();
        }
        /// <summary>
        /// Fáze tohoto času
        /// </summary>
        private DriveTester.TestPhase TimePhase { get { return (_TimeInfo?.Phase ?? DriveTester.TestPhase.None); } }
        /// <summary>
        /// Poměr chybných Async proti celkovému počtu Async v rozmezí 0 - 1
        /// </summary>
        private decimal AsyncRatio
        {
            get
            {
                decimal ratio = 0m;
                var timeInfo = this._TimeInfo;
                if (timeInfo != null && timeInfo.AsyncCount > 0)
                {
                    ratio = (decimal)timeInfo.AsyncSlow / (decimal)timeInfo.AsyncCount;
                    ratio = (ratio < 0m ? 0m : (ratio < 1m ? ratio : 1m));
                }
                return ratio;
            }
        }
        private string TitleText
        {
            get
            {
                switch (TimePhase)
                {
                    case DriveTester.TestPhase.None: return "?";
                    case DriveTester.TestPhase.SaveShortFile: return "Zápis krátkých souborů";
                    case DriveTester.TestPhase.SaveLongFile: return "Zápis dlouhých souborů";
                    case DriveTester.TestPhase.ReadShortFile: return "Čtení krátkých souborů";
                    case DriveTester.TestPhase.ReadLongFile: return "Čtení dlouhých souborů";
                }
                return "?";
            }
        }
        private string TimeText
        {
            get
            {
                decimal value = 0m;
                string text = "";
                var timeInfo = this._TimeInfo;
                if (timeInfo != null)
                {
                    switch (TimePhase)
                    {
                        case DriveTester.TestPhase.SaveShortFile:
                            value = timeInfo.FileCount;
                            text = "file/sec";
                            break;
                        case DriveTester.TestPhase.SaveLongFile:
                            value = ((decimal)timeInfo.TotalLength) / 1000000m;
                            text = "MB/sec";
                            break;
                        case DriveTester.TestPhase.ReadShortFile:
                            value = timeInfo.FileCount;
                            text = "file/sec";
                            break;
                        case DriveTester.TestPhase.ReadLongFile:
                            value = ((decimal)timeInfo.TotalLength) / 1000000m;
                            text = "MB/sec";
                            break;
                    }
                    decimal seconds = timeInfo.TimeTotalSec;
                    if (value > 0m &&  text.Length > 0 && seconds > 0m)
                    {
                        value = value / seconds;
                        value = Math.Round(value, 3);
                        text = value.ToString() + " " + text;
                    }
                }
                return text;
            }
        }
        private Color BackgroundColor
        {
            get
            {
                switch (TimePhase)
                {
                    case DriveTester.TestPhase.SaveShortFile: return Skin.TestPhaseSaveShortFileBackColor;
                    case DriveTester.TestPhase.SaveLongFile: return Skin.TestPhaseSaveLongFileBackColor;
                    case DriveTester.TestPhase.ReadShortFile: return Skin.TestPhaseReadShortFileBackColor;
                    case DriveTester.TestPhase.ReadLongFile: return Skin.TestPhaseReadLongFileBackColor;
                }
                return this.BackColor;
            }
        }
     
        /// <summary>
        /// Info o čase.
        /// Po setování hodnoty se automaticky vyvolá Refresh.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DriveTester.FileTimeInfo TimeInfo { get { return _TimeInfo; } set { _TimeInfo = value; this.Refresh(); } } private DriveTester.FileTimeInfo _TimeInfo;
        /// <summary>
        /// Info o aktuální fázi testu. To nemusí být zdejší fáze.
        /// Podle ní se určuje, zda zdejší čas <see cref="TimeInfo"/> (jeho fáze <see cref="DriveTester.FileTimeInfo.Phase"/>) je ta aktivní.
        /// Po setování takové hodnoty fáze, která změní hodnotu v <see cref="IsActive"/>, se automaticky vyvolá Refresh.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DriveTester.TestPhase CurrentActivePhase 
        { 
            get { return _CurrentActivePhase; } 
            set 
            {
                if (value != _CurrentActivePhase)
                {
                    bool oldActive = IsActive;
                    _CurrentActivePhase = value;
                    bool newActive = IsActive;
                    if (oldActive != newActive)
                        this.Refresh();
                }
            } 
        }
        private DriveTester.TestPhase _CurrentActivePhase;
        /// <summary>
        /// Info o tom, že fáze zdejšího času je právě ta aktivní
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsActive { get { return (_TimeInfo != null && _TimeInfo.Phase == _CurrentActivePhase); } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override int CurrentOptimalHeight { get { return OptimalHeight; } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        public static int OptimalHeight {  get { return 35; } }
    }
}
