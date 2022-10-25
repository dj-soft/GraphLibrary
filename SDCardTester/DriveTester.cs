using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace DjSoft.Tools.SDCardTester
{
    /// <summary>
    /// Tester zápisu a čtení na disk
    /// </summary>
    public class DriveTester : DriveWorker
    {
        #region Konstrukce a public rozhraní
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DriveTester()
        {
            InitStopwatch();
        }
        /// <summary>
        /// Požádá o provedení testu zápisu a čtení daného disku
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="doRead"></param>
        /// <param name="doSave"></param>
        public void Start(System.IO.DriveInfo drive, bool doSave, bool doRead)
        {
            StartAction(drive, () => { DoTestSave = doSave; DoTestRead = doRead; });
        }
        #endregion
        #region Privátní řízení běhu
        /// <summary>
        /// Zahájení testu, zde již v threadu Working
        /// </summary>
        protected override void Run()
        {
            LastStepTime = null;
            string testDir = null;
            RunInfoClear();
            PrepareFileGroups();
            this.RestartStopwatch();
            if (DoTestSave) RunTestSave(ref testDir);
            if (DoTestRead) RunTestRead(ref testDir);
            CallWorkingDone();
        }
        /// <summary>
        /// Vyvolá událost <see cref="TestStep"/>, pokud je odpovídající čas
        /// </summary>
        /// <param name="force"></param>
        /// <param name="currentLength"></param>
        /// <param name="startTime"></param>
        /// <param name="currentTime"></param>
        /// <param name="errorCount"></param>
        protected void CallTestStep(bool force, long? currentLength = null, long? startTime = null, long? currentTime = null, int? errorCount = null, bool? addSizeProcessed = false)
        {
            if (!CanCallWorkingStep(force)) return;

            // Do public properites dáme informace o hotových průchodech:
            TimeInfoSaveShort = TimeInfoSaveShortDone;
            TimeInfoSaveLong = TimeInfoSaveLongDone;
            TimeInfoReadShort = TimeInfoReadShortDone;
            TimeInfoReadLong = TimeInfoReadLongDone;
            TestSizeProcessed = TestSizeProcessedDone;

            if (currentLength.HasValue && startTime.HasValue && currentTime.HasValue && errorCount.HasValue)
            {   // Máme k dispozici "rozpracovaná data" => přičteme je k hodnotám *Done a uložíme do public properties:
                TestPhase workingPhase = this.CurrentWorkingPhase;
                decimal elapsedTime = this.GetSeconds(startTime.Value, currentTime.Value);
                // Do odpovídající public property vložím součet hodnoty Done + rozpracované hodnoty daného režimu:
                switch (workingPhase)
                {
                    case TestPhase.SaveShortFile:
                        TimeInfoSaveShort = new FileTimeInfo(TimeInfoSaveShortDone, 0, currentLength.Value, elapsedTime, errorCount.Value);
                        break;
                    case TestPhase.SaveLongFile:
                        TimeInfoSaveLong = new FileTimeInfo(TimeInfoSaveLongDone, 0, currentLength.Value, elapsedTime, errorCount.Value);
                        break;
                    case TestPhase.ReadShortFile:
                        TimeInfoReadShort = new FileTimeInfo(TimeInfoReadShortDone, 0, currentLength.Value, elapsedTime, errorCount.Value);
                        break;
                    case TestPhase.ReadLongFile:
                        TimeInfoReadLong = new FileTimeInfo(TimeInfoReadLongDone, 0, currentLength.Value, elapsedTime, errorCount.Value);
                        break;
                }
                if (addSizeProcessed.HasValue && addSizeProcessed.Value)
                    TestSizeProcessed += currentLength.Value;
            }
            RefreshFileGroups();
            CallWorkingStep();
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
        /// Aktuálně prováděný typ test. 
        /// Zde je fáze skutečně probíhající aktivity. Tato hodnota určuje sadu výsledků, které se právě aktualizují.
        /// </summary>
        public TestPhase CurrentWorkingPhase { get; private set; }
        /// <summary>
        /// Aktuálně běžící fáze testu. 
        /// Zde je uváděno Save i v době, kdy se provádí kontrolní Read. Tato hodnota má být zvýrazněna v okně informací.
        /// </summary>
        public TestPhase CurrentTestPhase { get; private set; }
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
        /// <summary>
        /// Stav progresu v rozsahu 0 - 1
        /// </summary>
        public decimal ProgressRatio
        {
            get
            {
                if (TestSizeTotal <= 0L) return 0;

                decimal sizeTotal = (decimal)TestSizeTotal;
                decimal sizeProcessed = (decimal)TestSizeProcessed;
                decimal ratio = sizeProcessed / sizeTotal;
                ratio = (ratio < 0m ? 0m : (ratio > 100m ? 100m : ratio));
                return ratio;
            }
        }
        protected long TestSizeTotal { get; set; }
        protected long TestSizeProcessed { get; set; }
        protected long TestSizeProcessedDone { get; set; }

        #endregion
        #region Vlastní test zápisu
        /// <summary>
        /// Fyzický test zápisu
        /// </summary>
        protected void RunTestSave(ref string testDir)
        {
            if (Stopping) return;
            if (!CanWriteFile(0L)) return;                                               // Pokud nemám na disku ani rezervu volného místa...

            if (testDir is null) testDir = GetTestDirectory(true);
            if (testDir is null) return;

            // Na otestování disku 8TB při využití jednoho adresáře připouštíme nejvýše 4096 souborů;
            // Nejprve vepíšeme 512 souborů o velikosti 4096 B = 2 MB (2 097 152 B);
            // Zbývajících 3584 souborů bude mít velikost (celková velikost disku / 3584) zarovnáno na 4KB bloky, pro 8TB disk tedy velikost = 2 454 265 856 = 2.5 GB
            long totalSize = Drive.TotalSize;
            this.TestSizeTotal = Drive.AvailableFreeSpace - GetReserveSpace(Drive.TotalSize);
            this.TestSizeProcessed = 0L;
            this.TestSizeProcessedDone = 0L;
            long longFilesLength = totalSize / LongFilesMaxCount;                        // Délka velkého souboru tak, aby jich v jednom adresáři na prázdném disku bylo celkem max 4096 souborů
            longFilesLength = (longFilesLength / ShortFilesLength) * ShortFilesLength;   // Zarovnáno na 4KB bloky
            long longFilesMinLength = LongFilesMinLength;
            if (longFilesLength < longFilesMinLength) longFilesLength = longFilesMinLength;        // Velké soubory by neměly být menší než 16 MB, aby bylo možno měřit rychlost zápisu i čtení, když někdy je deklarováno 80 MB/sec

            int fileNumber = GetNextFileNumber(testDir);
            CurrentWorkingPhase = (IsShortFile(fileNumber) ? TestPhase.SaveShortFile : TestPhase.SaveLongFile);
            CurrentTestPhase = CurrentWorkingPhase;
            CallTestStep(true);
            try
            {
                while (!Stopping)
                {
                    if (IsShortFile(fileNumber))
                    {
                        var timeInfoSaveShort = RunTestSaveOneFile(testDir, fileNumber, out string fileName, ShortFilesLength, TestPhase.SaveShortFile, TestPhase.SaveShortFile);
                        TimeInfoSaveShortDone.Add(timeInfoSaveShort);
                        TestSizeProcessedDone += timeInfoSaveShort.SizeTotal;

                        var timeInfoReadShort = RunTestReadOneFile(fileName, fileNumber, TestPhase.ReadShortFile, TestPhase.SaveShortFile, true);
                        TimeInfoReadShortDone.Add(timeInfoReadShort);
                    }
                    else
                    {
                        if (!CanWriteFile(longFilesLength, out long acceptedLength)) break;
                        var timeInfoSaveLong = RunTestSaveOneFile(testDir, fileNumber, out string fileName, acceptedLength, TestPhase.SaveLongFile, TestPhase.SaveLongFile);
                        TimeInfoSaveLongDone.Add(timeInfoSaveLong);
                        TestSizeProcessedDone += timeInfoSaveLong.SizeTotal;

                        var timeInfoReadLong = RunTestReadOneFile(fileName, fileNumber, TestPhase.ReadLongFile, TestPhase.SaveLongFile, true);
                        TimeInfoReadLongDone.Add(timeInfoReadLong);
                    }
                    fileNumber++;
                }
                CurrentWorkingPhase = TestPhase.None;
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
        /// <param name="workingPhase">Fáze, určuje zápis Short/Long souboru, bude vepsána do <see cref="CurrentWorkingPhase"/></param>
        /// <returns></returns>
        protected FileTimeInfo RunTestSaveOneFile(string testDir, int fileNumber, out string fileName, long targetLength, TestPhase workingPhase, TestPhase testPhase)
        {
            fileName = null;
            if (Stopping) return null;

            string saveFileName = GetFileName(testDir, fileNumber);
            if (Stopping) return null;
            fileName = saveFileName;

            CurrentWorkingPhase = workingPhase;
            CurrentTestPhase = testPhase;
            return RunTestSaveOneFileWriteAsync(testDir, fileNumber, fileName, targetLength, workingPhase);
        }
        protected FileTimeInfo RunTestSaveOneFileWriteAsync(string testDir, int fileNumber, string fileName, long targetLength, TestPhase workingPhase)
        {
            long startTime = this.CurrentTime;
            long currentLength = 0L;
            int bufferIndex = 0;
            using (System.IO.FileStream fst = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                // Save: buffer s daty je "v předstihu" před fyzickou prací se souborem (na rozdíl od metody pro Read)
                int bufferLength = GetBufferLength(workingPhase, currentLength, targetLength);
                var nextData = CreateTestData(fileNumber, bufferIndex, bufferLength);
                bool doWrite = true;
                while (doWrite && !Stopping)
                {
                    var currentData = nextData;            // Nezbytnost!!! : protože 'buffer' odchází do FileStreamu k zápisu, a současně (protože .WriteAsync) se do jiné proměnné 'data' připravuje nový obsah pro další cyklus
                    int oneLength = currentData.Length;
                    using (var task = fst.WriteAsync(currentData, 0, oneLength))    // Zde asynchronně začíná zápis dat do souboru
                    {
                        currentLength += (long)oneLength;
                        doWrite = currentLength < targetLength;                     // Ještě budeme připravovat další data?
                        if (doWrite)
                        {
                            bufferLength = GetBufferLength(workingPhase, currentLength, targetLength);
                            nextData = CreateTestData(fileNumber, ++bufferIndex, bufferLength);         // A zatímco se do souboru v jiném threadu zapisuje, my si zde připravujeme data do dalšího kola zápisu.
                        }
                        if (!task.IsCompleted)
                            task.Wait();                   // Počkáme na dokončení zápisu bufferu do souboru...
                    }

                    var currentTime = this.CurrentTime;
                    CallTestStep(false, currentLength, startTime, currentTime, 0, true);
                }
                fst.Flush();
                fst.Close();
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(workingPhase, 1, currentLength, elapsedTime, 0);
        }
        protected FileTimeInfo RunTestSaveOneFileBeginWrite(string testDir, int fileNumber, string fileName, long targetLength, TestPhase phase)
        {
            long startTime = this.CurrentTime;
            long currentLength = 0L;
            int bufferIndex = 0;
            using (System.IO.FileStream fst = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                int bufferLength = GetBufferLength(phase, currentLength, targetLength);
                var nextData = CreateTestData(fileNumber, bufferIndex, bufferLength);
                bool doWrite = true;
                while (doWrite && !Stopping)
                {
                    var buffer = nextData;           // Nezbytnost!!! : protože 'buffer' odchází do FileStreamu k zápisu, a současně (protože .WriteAsync) se do jiné proměnné 'data' připravuje nový obsah pro další cyklus
                    int oneLength = buffer.Length;

                    var iAsync = fst.BeginWrite(buffer, 0, oneLength, null, null);
                    currentLength += (long)oneLength;
                    doWrite = currentLength < targetLength;          // Ještě budeme připravovat další data?
                    if (doWrite)
                    {
                        bufferLength = GetBufferLength(phase, currentLength, targetLength);
                        nextData = CreateTestData(fileNumber, ++bufferIndex, bufferLength);         // A zatímco se do souboru v jiném threadu zapisuje, my si zde připravujeme data do dalšího kola zápisu.
                    }

                    if (!iAsync.IsCompleted)
                        iAsync.AsyncWaitHandle.WaitOne(1000);        // Počkáme na dokončení zápisu bufferu do souboru...

                    var currentTime = this.CurrentTime;
                    CallTestStep(false, currentLength, startTime, currentTime, 0);
                }
                fst.Flush();
                fst.Close();
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(phase, 1, currentLength, elapsedTime, 0);
        }
        protected FileTimeInfo RunTestSaveOneFileBinaryStream(string testDir, int fileNumber, string fileName, long targetLength, TestPhase phase)
        {
            long startTime = this.CurrentTime;
            int bufferIndex = 0;
            long currentLength = 0L;
            
            int bufferLength = GetBufferLength(phase, currentLength, targetLength);
            var data = CreateTestData(fileNumber, bufferIndex, bufferLength);
            
            using (var memoryStream = new System.IO.MemoryStream(data))
            using (var fileStream = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write))
            {
                memoryStream.CopyToAsync(fileStream);
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(phase, 1, currentLength, elapsedTime, 0);
        }
        /// <summary>
        /// Mohu zapsat soubor dané délky? Máme na disku dost místa?
        /// </summary>
        /// <param name="fileLength"></param>
        /// <returns></returns>
        protected bool CanWriteFile(long fileLength)
        {
            return CanWriteFile(fileLength, out var _);
        }
        /// <summary>
        /// Mohu zapsat soubor dané délky? Máme na disku dost místa?
        /// </summary>
        /// <param name="requestedLength"></param>
        /// <returns></returns>
        protected bool CanWriteFile(long requestedLength, out long acceptedLength)
        {
            acceptedLength = 0L;
            System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(Drive.Name);         // Refresh
            long available = driveInfo.AvailableFreeSpace;
            long reserve = GetReserveSpace(driveInfo.TotalSize);
            long smallest = ShortFilesLength;
            if (available <= (reserve + smallest)) return false;                         // Nevejde se ani ten nejmenší soubor
            if (available <= (reserve + requestedLength))
            {   // Požadovaná velikost se sice nevejde, ale ten nejmenší se vejde => vyplníme tedy dostupný prostor tak, aby zbyla jen rezerva:
                long availableLength = ((available - reserve) / smallest) * smallest;
                if (availableLength < smallest)
                    return false;
                acceptedLength = availableLength;
            }
            else
            {   // Požadovaný soubor se vejde celý = v požadované délce:
                acceptedLength = requestedLength;
            }
            return (requestedLength == 0L || acceptedLength > 0L);
        }
        /// <summary>
        /// Metoda vrátí počet byte, které mají zůstat na disku dané celkové velikosti nepoužité jako rezerva.
        /// </summary>
        /// <param name="totalSize"></param>
        /// <returns></returns>
        protected long GetReserveSpace(long totalSize)
        {
            long reserve = totalSize / 200L;                                             // Rezerva = 0.5% místa na disku, která má zbýt po zápisu testovacího souboru
            reserve = (reserve < ReserveMin ? ReserveMin : (reserve > ReserveMax ? ReserveMax : reserve));
            return reserve;
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
            if (Stopping) return;
            if (testDir is null) testDir = GetTestDirectory(false);
            if (testDir is null) return;

            try
            {
                var testFiles = GetTestFiles(this.Drive, null);
                if (testFiles.Length == 0) return;

                this.TestSizeTotal = testFiles.Select(fi => fi.Length).Sum();
                this.TestSizeProcessed = 0L;
                this.TestSizeProcessedDone = 0L;

                foreach (var testFile in testFiles)
                {
                    if (Stopping) break;

                    var fileName = testFile.FullName;
                    int fileNumber = GetFileNumber(fileName);
                    if (fileNumber > 0)
                    {
                        if (IsShortFile(fileNumber))
                        {
                            var timeInfoReadShort = RunTestReadOneFile(fileName, fileNumber, TestPhase.ReadShortFile, TestPhase.ReadShortFile, true);
                            TimeInfoReadShortDone.Add(timeInfoReadShort);
                        }
                        else
                        {
                            var timeInfoReadLong = RunTestReadOneFile(fileName, fileNumber, TestPhase.ReadLongFile, TestPhase.ReadLongFile, true);
                            TimeInfoReadLongDone.Add(timeInfoReadLong);
                        }
                        TestSizeProcessedDone += testFile.Length;
                    }
                }
            }
            catch { }
            CallTestStep(true);
        }
        /// <summary>
        /// Provést test čtení
        /// </summary>
        protected bool DoTestRead;
        /// <summary>
        /// Provede test čtení daného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNumber"></param>
        /// <param name="workingPhase"></param>
        /// <param name="testPhase"></param>
        /// <returns></returns>
        private FileTimeInfo RunTestReadOneFile(string fileName, int fileNumber, TestPhase workingPhase, TestPhase testPhase, bool renameOnError)
        {
            if (Stopping) return null;

            CurrentWorkingPhase = workingPhase;
            CurrentTestPhase = testPhase;

            var fileInfo = RunTestReadOneFileWriteAsync(fileName, workingPhase, fileNumber);       // První čtení
            if (fileInfo.ErrorCount > 0)
                fileInfo = RunTestReadOneFileWriteAsync(fileName, workingPhase, fileNumber);       // Po chybě dáme druhé čtení
            if (fileInfo.ErrorCount > 0 && renameOnError)
                RenameFileOnError(ref fileName);
            return fileInfo;
        }
        /// <summary>
        /// Provede test čtení jednoho daného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="workingPhase"></param>
        /// <param name="fileNumber"></param>
        /// <returns></returns>
        private FileTimeInfo RunTestReadOneFileWriteAsync(string fileName, TestPhase workingPhase, int fileNumber)
        {
            var fileInfo = new System.IO.FileInfo(fileName);
            if (!fileInfo.Exists) return null;
            long totalLength = fileInfo.Length;

            long startTime = this.CurrentTime;
            long currentLength = 0L;
            int bufferIndex = -1;
            int totalErrors = 0;
            bool addSizeProcessed = (workingPhase == CurrentTestPhase);
            using (System.IO.FileStream fst = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                // Read: buffer s daty je "pozadu" za fyzickou prací se souborem (na rozdíl od metody pro Save)
                byte[] prevData = null;
                bool doRead = true;
                while (doRead && !Stopping)
                {
                    doRead = (fst.Position < totalLength);
                    if (!doRead) break;

                    int bufferLength = GetBufferLength(workingPhase, currentLength, totalLength);
                    if (bufferLength == 0) break;

                    var readData = new byte[bufferLength];
                    using (var task = fst.ReadAsync(readData, 0, bufferLength))
                    {   // Začalo načítání dat ze souboru; a než doběhne, tak v tomto threadu mám chvilku čas: provedu porovnání předchozích načtených dat (prevData) s očekávanými daty pro daný soubor a index:
                        totalErrors += VerifyTestData(prevData, fileNumber, bufferIndex);

                        // Počkáme na doběhnutí načtení dat:
                        if (!task.IsCompleted)
                            task.Wait();

                        // Nyní načtená data přesuneme do prevData, ale nyní je nebudeme ověřovat...
                        currentLength += task.Result;
                        prevData = readData;
                        bufferIndex++;
                    }

                    var currentTime = this.CurrentTime;
                    CallTestStep(false, currentLength, startTime, currentTime, totalErrors, addSizeProcessed);
                }
                // Prověříme poslední načtený buffer:
                totalErrors += VerifyTestData(prevData, fileNumber, bufferIndex);
                fst.Close();
            }

            decimal elapsedTime = this.GetSeconds(startTime);
            return new FileTimeInfo(workingPhase, 1, currentLength, elapsedTime, totalErrors);
        }
        /// <summary>
        /// Přejmenuje daný soubor - změní mu příponu na chybovou <see cref="FileNameErrorExtension"/>
        /// </summary>
        /// <param name="fileName"></param>
        private void RenameFileOnError(ref string fileName)
        {
            var fileType = GetTestFileType(fileName);
            if (fileType == TestFileType.TestFile)
            {   // Přejmenovávat na Error budu jen dobré soubory (protože i chybné soubory testujeme):
                string oldFile = fileName;
                string newFile = System.IO.Path.ChangeExtension(fileName, FileNameErrorExtension);
                try
                {
                    System.IO.File.Move(oldFile, newFile);
                    fileName = newFile;
                }
                catch { }
            }
        }
        #endregion
        #region Obsah disku
        /// <summary>
        /// Připraví základní informace o obsahu aktuálního disku
        /// </summary>
        private void PrepareFileGroups()
        {
            var drive = Drive;
            FileGroups = DriveAnalyser.GetFileGroupsForDrive(drive, true, out long totalSize);
            TestReadGroup = FileGroups.First(g => g.Code == DriveAnalyser.FileGroup.CODE_TEST_READ);
            TestFileGroup = FileGroups.First(g => g.Code == DriveAnalyser.FileGroup.CODE_TEST_FILE);
            TestSaveGroup = FileGroups.First(g => g.Code == DriveAnalyser.FileGroup.CODE_TEST_SAVE);
            TotalSize = totalSize;
        }
        /// <summary>
        /// Aktualizuje hodnoty v testovacích grupách <see cref="TestReadGroup"/>, <see cref="TestFileGroup"/>, <see cref="TestSaveGroup"/>
        /// podle aktuálního stavu čtení a zápisu.
        /// </summary>
        private void RefreshFileGroups()
        {
            bool doRead = DoTestRead;
            bool doSave = DoTestSave;
            int readCount = (doRead ? TimeInfoReadShort.FileCount + TimeInfoReadLong.FileCount : 0);
            long readSize = (doRead ? TimeInfoReadShort.SizeTotal + TimeInfoReadLong.SizeTotal : 0L);
            int saveCount = (doSave ? TimeInfoSaveShort.FileCount + TimeInfoSaveLong.FileCount : 0);
            long saveSize = (doSave ? TimeInfoSaveShort.SizeTotal + TimeInfoSaveLong.SizeTotal : 0L);
            TestReadGroup.FilesCountDelta = readCount;
            TestReadGroup.SizeTotalDelta = readSize;
            TestFileGroup.FilesCountDelta = -readCount;
            TestFileGroup.SizeTotalDelta = -readSize;
            TestSaveGroup.FilesCountDelta = saveCount;
            TestSaveGroup.SizeTotalDelta = saveSize;
        }
        /// <summary>
        /// Obsah disku, základní složení.
        /// Při zapisování testovacích dat na disk v rámci testu je navyšována hodnota v grupě testovacích dat.
        /// </summary>
        public DriveAnalyser.FileGroup[] FileGroups { get; private set; }
        /// <summary>
        /// Velikost disku
        /// </summary>
        public long TotalSize { get; private set; }
        /// <summary>
        /// Data popisující testovací grupu (<see cref="DriveAnalyser.FileGroup"/>) v rámci testovaného disku pro soubory, které budou pouze čteny.
        /// V procesu čtení bude do této grupy navyšována hodnota zpracovaného prostoru.
        /// </summary>
        private DriveAnalyser.IFileGroup TestReadGroup { get; set; }
        /// <summary>
        /// Data popisující testovací grupu (<see cref="DriveAnalyser.FileGroup"/>) v rámci testovaného disku pro soubory, které na disku existovaly na začátku testu.
        /// V procesu čtení bude do této grupy snižována hodnota zpracovaného prostoru.
        /// V procesu zápisu se tato grupa nebude měnit.
        /// </summary>
        private DriveAnalyser.IFileGroup TestFileGroup { get; set; }
        /// <summary>
        /// Data, popisující procesovanou grupu (<see cref="DriveAnalyser.FileGroup"/>) v rámci testovaného disku.
        /// V procesu zápisu bude do této grupy navyšována hodnota obsazeného prostoru.
        /// </summary>
        private DriveAnalyser.IFileGroup TestSaveGroup { get; set; }
        // private int TestFileInitCount { get; set; }
        // private long TestFileInitLength { get; set; }
        #endregion
        #region Vyhledání testovacích souborů
        /// <summary>
        /// Na daném disku vyhledá testovací soubory (podle jména adresáře a jména souborů) a vrátí jejich pole.
        /// Pokud tam nic neexistuje, vrátí prázdné pole. Pokud není zadán drive, vrátí prázdné pole.
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="fileTypes">Požadovaný druh testovacích souborů, nebo null = všechny testovací soubory</param>
        /// <returns></returns>
        public static System.IO.FileInfo[] GetTestFiles(System.IO.DriveInfo drive, TestFileType? fileTypes)
        {
            System.IO.FileInfo[] result = null;
            if (drive != null)
            {
                string dirName = System.IO.Path.Combine(drive.RootDirectory.FullName, TestDirectory);
                var dirInfo = new System.IO.DirectoryInfo(dirName);
                if (dirInfo.Exists)
                {
                    string searchPattern = FileNameMask;                 // FileNamePrefix + "?????" + FileNameExtensionsMask;
                    var allFiles = dirInfo.GetFiles(searchPattern);
                    List<System.IO.FileInfo> testFiles;
                    if (fileTypes.HasValue && fileTypes.Value == TestFileType.TestFile)
                        testFiles = allFiles.Where(f => GetTestFileType(f.FullName) == TestFileType.TestFile).ToList();
                    else if (fileTypes.HasValue && fileTypes.Value == TestFileType.TestFileError)
                        testFiles = allFiles.Where(f => GetTestFileType(f.FullName) == TestFileType.TestFileError).ToList();
                    else
                        testFiles = allFiles.Where(f => IsTestFile(f.FullName)).ToList();
                    testFiles.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
                    result = testFiles.ToArray();
                }
            }
            if (result is null) result = new System.IO.FileInfo[0];
            return result;
        }
        #endregion
        #region Generátor dat, tvorba adresáře, tvorba a detekce názvu souborů a jejich čísla
        /// <summary>
        /// Určí velikost bufferu pro další blok zápisu nebo čtení.
        /// </summary>
        /// <param name="phase"></param>
        /// <param name="currentLength"></param>
        /// <param name="totalLength"></param>
        /// <returns></returns>
        private int GetBufferLength(TestPhase phase, long currentLength, long totalLength)
        {
            int optimalLength = ((phase == TestPhase.SaveShortFile || phase == TestPhase.ReadShortFile) ? ShortBufferLength : LongBufferLength);
            long remainingLength = totalLength - currentLength;
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
        protected byte[] CreateTestData(int fileNumber, int bufferIndex, int bufferLength)
        {
            byte[] buffer = new byte[bufferLength];
            _ProcessTestData(buffer, fileNumber, bufferIndex, true, out var _);
            return buffer;
        }
        /// <summary>
        /// Prověří, zda data dodaná v bufferu <paramref name="buffer"/> jsou korektní pro daný soubor (číslo <paramref name="fileNumber"/> a pro blok bufferu <paramref name="bufferIndex"/>).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="fileNumber"></param>
        /// <param name="bufferIndex"></param>
        /// <returns></returns>
        protected int VerifyTestData(byte[] buffer, int fileNumber, int bufferIndex)
        {
            if (buffer is null || bufferIndex < 0) return 0;
            _ProcessTestData(buffer, fileNumber, bufferIndex, false, out int errorCount);
            return errorCount;
        }
        /// <summary>
        /// Zpracuje data jednoho bufferu: buď je vypočítá a naplní do dodaného bufferu; anebo je vypočítá a porovná s obsahem bufferu a určí počet neshod do <paramref name="errorCount"/>.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="fileNumber"></param>
        /// <param name="bufferIndex"></param>
        /// <param name="isWrite"></param>
        /// <param name="errorCount"></param>
        private void _ProcessTestData(byte[] buffer, int fileNumber, int bufferIndex, bool isWrite, out int errorCount)
        {
            errorCount = 0;
            int bufferLength = buffer.Length;
            int sample = (15 * fileNumber + 7 * bufferIndex) % 256;
            int step = ((fileNumber % 5) + 1) * ((bufferIndex % 3) + 1);
            for (int i = 0; i < bufferLength; i++)
            {
                byte value = (byte)sample;
                if (isWrite)
                    buffer[i] = value;
                else if (buffer[i] != value)
                    errorCount++;
                sample = (sample + step) % 256;
            }
        }
        /// <summary>
        /// Vrátí název testovacího adresáře
        /// </summary>
        /// <param name="canCreate"></param>
        /// <returns></returns>
        protected string GetTestDirectory(bool canCreate)
        {
            string dirName = System.IO.Path.Combine(this.Drive.RootDirectory.FullName, TestDirectory);
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
            GetTestFileType(fileName, out var number);
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
        /// Vrátí true, pokud daný název souboru je jménem testovacího souboru (platný i s chybami)
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected static bool IsTestFile(string fileName)
        {
            var fileType = GetTestFileType(fileName, out var _);
            return (fileType == TestFileType.TestFile || fileType == TestFileType.TestFileError);
        }
        /// <summary>
        /// Vrátí typ souboru podle jeho názvu a přípony
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNumber"></param>
        /// <returns></returns>
        protected static TestFileType GetTestFileType(string fileName)
        {
            return GetTestFileType(fileName, out var _);
        }
        /// <summary>
        /// Vrátí true, pokud daný název souboru je jménem testovacího souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNumber"></param>
        /// <returns></returns>
        protected static TestFileType GetTestFileType(string fileName, out int fileNumber)
        {
            fileNumber = -1;
            TestFileType result = TestFileType.None;
            if (!String.IsNullOrEmpty(fileName))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower();
                var extn = System.IO.Path.GetExtension(fileName);
                bool isTest = String.Equals(extn, FileNameExtension, StringComparison.InvariantCultureIgnoreCase);
                bool isTestError = String.Equals(extn, FileNameErrorExtension, StringComparison.InvariantCultureIgnoreCase);
                if (name.StartsWith(FileNamePrefix) && name.Length == 10 && (isTest || isTestError) && Int32.TryParse(name.Substring(5, 5), out int number) && number > 0)
                {
                    fileNumber = number;
                    result = (isTestError ? TestFileType.TestFileError : TestFileType.TestFile);
                }
                else
                {
                    result = TestFileType.TestFileError;
                }
            }
            return result;
        }
        /// <summary>
        /// Vrátí true, pokud soubor s daným číslem bude "krátký"
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        protected static bool IsShortFile(int number) { return (number <= ShortFilesCount); }
        /// <summary>
        /// Typ testovacího souboru
        /// </summary>
        public enum TestFileType
        {
            /// <summary>
            /// Není to soubor (prázdné jméno)
            /// </summary>
            None = 0,
            /// <summary>
            /// Jde o standardní testovací soubor
            /// </summary>
            TestFile,
            /// <summary>
            /// Jde o testovací soubor s detekovanou chybou obsahu
            /// </summary>
            TestFileError,
            /// <summary>
            /// Nejde o testovací soubor = je to jakýkoli jiný soubor
            /// </summary>
            OtherFile
        }
        protected const string TestDirectory = "_TestDir.1968";
        protected const string FileNamePrefix = "test~";
        protected const string FileNameMask = FileNamePrefix + "?????.tmp*";
        protected const string FileNameExtensionsMask = ".tmp*";
        protected const string FileNameExtension = ".tmp";
        protected const string FileNameErrorExtension = ".tmpError";
        protected const long ShortFilesLength = 4 * KB;
        protected const int ShortFilesCount = 512;
        protected const int LongFilesMaxCount = 3584;
        protected const long LongFileReserve = 32L * MB;
        protected const long LongFilesMinLength = 16L * MB;
        protected const int ShortBufferLength = 4 * KBi;
        protected const int LongBufferLength = 1 * MBi;
        protected const long ReserveMin = 10L * MB;
        protected const long ReserveMax = 80L * MB;
        protected const long KB = 1024L;
        protected const long MB = KB * KB;
        protected const int KBi = 1024;
        protected const int MBi = KBi * KBi;
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
                SizeTotal = 0L;
                TimeTotalSec = 0m;
                ErrorCount = 0;
            }
            public FileTimeInfo(TestPhase testPhase, int fileCount, long totalLength, decimal timeTotalSec, int errorCount)
            {
                Phase = testPhase;
                FileCount = fileCount;
                SizeTotal = totalLength;
                TimeTotalSec = timeTotalSec;
                ErrorCount = errorCount;
            }
            public FileTimeInfo(FileTimeInfo a, FileTimeInfo b)
            {
                Phase = a.Phase;
                FileCount = a.FileCount + b.FileCount;
                SizeTotal = a.SizeTotal + b.SizeTotal;
                TimeTotalSec = a.TimeTotalSec + b.TimeTotalSec;
                ErrorCount = a.ErrorCount + b.ErrorCount;
            }
            public FileTimeInfo(FileTimeInfo a, int fileCount, long totalLength, decimal timeTotalSec, int errorCount)
            {
                Phase = a.Phase;
                FileCount = a.FileCount + fileCount;
                SizeTotal = a.SizeTotal + totalLength;
                TimeTotalSec = a.TimeTotalSec + timeTotalSec;
                ErrorCount = a.ErrorCount + errorCount;
            }
            public override string ToString()
            {
                return $"Phase: {Phase}; Count: {FileCount}; Length: {SizeTotal}; Time: {TimeTotalSec}; ErrorCount: {ErrorCount}";
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
            public long SizeTotal { get; private set; }
            /// <summary>
            /// Celkový čas v sekundách
            /// </summary>
            public decimal TimeTotalSec { get; private set; }
            /// <summary>
            /// Počet chyb nalezených v dané akci
            /// </summary>
            public int ErrorCount { get; private set; }
            /// <summary>
            /// Do this instance přidá data z dodané instance
            /// </summary>
            /// <param name="add"></param>
            public void Add(FileTimeInfo add)
            {
                if (add != null)
                {
                    this.FileCount += add.FileCount;
                    this.SizeTotal += add.SizeTotal;
                    this.TimeTotalSec += add.TimeTotalSec;
                    this.ErrorCount += add.ErrorCount;
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
    #region class DriveTestTimePhaseControl : zobrazovací control pro výsledky jednoho druhu testu
    /// <summary>
    /// Vizuální control zobrazující data z <see cref="DriveTester.FileTimeInfo"/>
    /// </summary>
    public class DriveTestTimePhaseControl : WorkingResultControl
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
            this.PaintErrors(e, clientBounds, outerBounds, innerBounds);
            this.PaintText(e, clientBounds, outerBounds, innerBounds);
        }
        private void PaintBackground(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            Painter.PaintRectangle(e.Graphics, this.BackColor, clientBounds);            // Šedá barva okolo
            Painter.PaintRectangle(e.Graphics, this.BackgroundColor, outerBounds);       // Barevný rámeček bez 3D efektu
        }
        /// <summary>
        /// Vykreslí informace o počtu chyb
        /// </summary>
        /// <param name="e"></param>
        /// <param name="clientBounds"></param>
        /// <param name="outerBounds"></param>
        /// <param name="innerBounds"></param>
        private void PaintErrors(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            Rectangle resultBounds = new Rectangle(innerBounds.X, innerBounds.Bottom - ResultAreaHeight, innerBounds.Width, ResultAreaHeight);
            Painter.PaintRectangle(e.Graphics, this.ResultBackgroundColor, resultBounds);
            Painter.PaintText(e.Graphics, this.Font, this.ResultInfoText, this.ForeColor, resultBounds, ContentAlignment.MiddleCenter);
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
            Rectangle textBounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width, innerBounds.Height - ResultAreaHeight);
            Painter.PaintText(e.Graphics, font, this.TitleText, this.ForeColor, textBounds, ContentAlignment.MiddleLeft);
            Painter.PaintText(e.Graphics, font, this.TimeText, this.ForeColor, textBounds, ContentAlignment.MiddleRight);

            if (disposeFont)
                font.Dispose();
        }
        /// <summary>
        /// Výška dolní oblasti, kde se zobrazuje barva a počet chyb
        /// </summary>
        private int ResultAreaHeight { get { return 24; } }
        /// <summary>
        /// Fáze tohoto času
        /// </summary>
        private DriveTester.TestPhase TimePhase { get { return (_TimeInfo?.Phase ?? DriveTester.TestPhase.None); } }
        /// <summary>
        /// Text titulku (druh testu)
        /// </summary>
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
        /// <summary>
        /// Text času (podle druhu textu)
        /// </summary>
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
                            value = ((decimal)timeInfo.SizeTotal) / 1000000m;
                            text = "MB/sec";
                            break;
                        case DriveTester.TestPhase.ReadShortFile:
                            value = timeInfo.FileCount;
                            text = "file/sec";
                            break;
                        case DriveTester.TestPhase.ReadLongFile:
                            value = ((decimal)timeInfo.SizeTotal) / 1000000m;
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
        /// <summary>
        /// Barva pozadí podle typu testu
        /// </summary>
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
        /// Barva pozadí oblasti s výsledky
        /// </summary>
        private Color ResultBackgroundColor
        {
            get
            {
                int errorsCount = this.TimeInfo?.ErrorCount ?? -1;
                if (errorsCount < 0) return Skin.TestResultUndefinedBackColor;
                if (errorsCount == 0) return Skin.TestResultCorrectBackColor;
                if (errorsCount == 1) return Skin.TestResultErrorBackColor;
                return Skin.TestResultMoreErrorsBackColor;
            }
        }
        /// <summary>
        /// Text informace o výsledcích
        /// </summary>
        private string ResultInfoText
        {
            get
            {
                int errorsCount = this.TimeInfo?.ErrorCount ?? -1;
                if (errorsCount < 0) return "???";
                if (errorsCount == 0) return "O.K.";
                if (errorsCount == 1) return "Chyba !";
                if (errorsCount < 5) return $"{errorsCount} chyby !!";
                return $"{errorsCount} chyb !!!";
            }
        }
        /// <summary>
        /// Vepíše dodané hodnoty do <see cref="TimeInfo"/> a do <see cref="CurrentTestPhase"/> a provede <see cref="WorkingResultControl.Refresh()"/>
        /// </summary>
        /// <param name="timeInfo"></param>
        /// <param name="currentTestPhase"></param>
        public void StoreInfo(DriveTester.FileTimeInfo timeInfo, DriveTester.TestPhase currentTestPhase)
        {
            _TimeInfo = timeInfo;
            _CurrentTestPhase = currentTestPhase;
            this.Refresh();
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
        public DriveTester.TestPhase CurrentTestPhase
        { 
            get { return _CurrentTestPhase; } 
            set 
            {
                if (value != _CurrentTestPhase)
                {
                    bool oldActive = IsActive;
                    _CurrentTestPhase = value;
                    bool newActive = IsActive;
                    if (oldActive != newActive)
                        this.Refresh();
                }
            } 
        }
        private DriveTester.TestPhase _CurrentTestPhase;
        /// <summary>
        /// Info o tom, že fáze zdejšího času je právě ta aktivní
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsActive { get { return (_TimeInfo != null && _TimeInfo.Phase == _CurrentTestPhase); } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override int CurrentOptimalHeight { get { return OptimalHeight; } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        public static int OptimalHeight {  get { return 48; } }
    }
    #endregion
}
