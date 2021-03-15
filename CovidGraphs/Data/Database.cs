using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO = System.IO;
using DW = System.Drawing;

namespace Djs.Tools.CovidGraphs.Data
{
    /// <summary>
    /// Databáze
    /// </summary>
    public class DatabaseInfo
    {
        #region Tvorba databáze, načtení a uložení dat, WebUpdate
        #region Konstruktor, základní proměnné pro data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="file"></param>
        public DatabaseInfo()
        {
            this.Init();
        }
        private object InterLock;
        private EntityInfo _World;
        private Dictionary<string, EntityInfo> _Vesnice;
        private Dictionary<string, PocetObyvatelInfo> _Pocet;
        private DateTime? _DataContentTime;
        private DateTime? _LastValidDataDate;
        private bool _HasData;
        protected void Init()
        {
            State = StateType.Initializing;
            InterLock = new object();
            _World = new EntityInfo(this, "", "World");
            _Vesnice = new Dictionary<string, EntityInfo>();
            _Pocet = new Dictionary<string, PocetObyvatelInfo>();
            _DataContentTime = null;
            _LastValidDataDate = null;
            _CovidInfo = null;
            _PocetInfo = null;
            State = StateType.Empty;
        }
        /// <summary>
        /// Stav databáze
        /// </summary>
        public StateType State { get { return _State; } private set { _State = value; } } private StateType _State;
        /// <summary>
        /// Obsahuje true, pokud databáze je připravena k práci.
        /// Pokud bude zadána práce v době, kdy <see cref="IsReady"/> je false, bude volající akce čekat.
        /// </summary>
        public bool IsReady { get { return (State == StateType.Ready); } }
        /// <summary>
        /// Obsahuje true tehdy, kdy jsou načtena reálná data (nejen struktury dat).
        /// </summary>
        public bool HasData { get { return _HasData; } private set { _HasData = value; } }
        /// <summary>
        /// Stavy databáze
        /// </summary>
        public enum StateType
        {
            None,
            Empty,
            Ready,
            Initializing,
            LoadingFile,
            SavingFile,
            Downloading,
            SearchingResult
        }
        #endregion
        #region Clear, info o načtených datech
        /// <summary>
        /// Smaže veškerá data uvnitř této databáze
        /// </summary>
        public void Clear()
        {
            _ClearAll(null);
        }
        /// <summary>
        /// Smaže veškerá data uvnitř této databáze
        /// </summary>
        private void _ClearAll(ProcessQueueInfo processQueue)
        {
            _ClearData(FileContentType.Structure);
            _ClearData(FileContentType.Data);
            _ClearData(FileContentType.DataPack);
            _ClearData(FileContentType.CovidObce1);
            _ClearData(FileContentType.PocetObyvatel);
            State = StateType.Empty;
        }
        private void _ClearData(FileContentType contentType)
        {
            switch (contentType)
            {
                case FileContentType.Structure:
                    _StructureLoadInfo = null;
                    _StructureSaveInfo = null;
                    _World.Clear(FileContentType.Structure);
                    _Vesnice.Clear();
                    _Pocet.Clear();
                    _HasData = false;
                    break;
                case FileContentType.Data:
                    _DataLoadInfo = null;
                    _DataSaveInfo = null;
                    _World.Clear(FileContentType.Data);
                    _DataContentTime = null;
                    _LastValidDataDate = null;
                    _HasData = false;
                    break;
                case FileContentType.DataPack:
                    _DataPackLoadInfo = null;
                    _DataPackSaveInfo = null;
                    _World.Clear(FileContentType.DataPack);
                    _Vesnice.Clear();
                    _Pocet.Clear();
                    _DataContentTime = null;
                    _LastValidDataDate = null;
                    _HasData = false;
                    break;
                case FileContentType.CovidObce1:
                case FileContentType.CovidObce2:
                case FileContentType.CovidObce3:
                    _CovidInfo = null;
                    _World.Clear(FileContentType.Data);
                    _DataContentTime = null;
                    _LastValidDataDate = null;
                    _HasData = false;
                    break;
                case FileContentType.PocetObyvatel:
                    _PocetInfo = null;
                    _Pocet.Clear();
                    _HasData = false;
                    break;
                case FileContentType.Umrti:
                    _World.Clear(FileContentType.Umrti);
                    break;
            }
        }
        /// <summary>
        /// Je voláno po dokončení zpracování daného souboru.
        /// </summary>
        /// <param name="processInfo"></param>
        private void StoreProcessFileResults(ProcessFileInfo processInfo)
        {
            processInfo.DoneTime = DateTime.Now;
            processInfo.CurrentInfo = null;

            bool isLoading = (processInfo.ProcessState == ProcessFileState.Loading || processInfo.ProcessState == ProcessFileState.Loaded);
            bool isSaving = (processInfo.ProcessState == ProcessFileState.Saving || processInfo.ProcessState == ProcessFileState.Saved);
            bool isWebDownloading = (processInfo.ProcessState == ProcessFileState.WebDownloading|| processInfo.ProcessState == ProcessFileState.WebDownloaded);
            bool isData = false;

            switch (processInfo.ContentType)
            {
                case FileContentType.Structure:
                    if (isLoading)
                        _StructureLoadInfo = processInfo;
                    else if (isSaving)
                        _StructureSaveInfo = processInfo;
                    break;
                case FileContentType.Data:
                    isData = true;
                    if (isLoading)
                        _DataLoadInfo = processInfo;
                    else if (isSaving)
                        _DataSaveInfo = processInfo;
                    break;
                case FileContentType.DataPack:
                    isData = true;
                    if (isLoading)
                        _DataPackLoadInfo = processInfo;
                    else if (isSaving)
                        _DataPackSaveInfo = processInfo;
                    break;
                case FileContentType.CovidObce1:
                case FileContentType.CovidObce2:
                case FileContentType.CovidObce3:
                    isData = true;
                    _CovidInfo = processInfo;
                    break;
                case FileContentType.PocetObyvatel:
                    _PocetInfo = processInfo;
                    break;
                case FileContentType.Umrti:

                    break;
            }
            processInfo.ProcessState = _GetFinalStateAfter(processInfo.ProcessState);

            if (isLoading)
                _LastLoadInfo = processInfo;
            else if (isSaving)
                _LastSaveInfo = processInfo;
            else if (isWebDownloading)
                _LastWebDownloadInfo = processInfo;

            _LastProcessInfo = processInfo;

            if (!HasData && isLoading && isData) HasData = true;
        }
        public ProcessFileInfo LastProcessInfo { get { return this._LastProcessInfo; } }
        public ProcessFileInfo LastLoadInfo { get { return this._LastLoadInfo; } }
        public ProcessFileInfo LastSaveInfo { get { return this._LastSaveInfo; } }
        public ProcessFileInfo LastWebDownloadInfo { get { return this._LastWebDownloadInfo; } }
        private ProcessFileInfo _CovidInfo;
        private ProcessFileInfo _PocetInfo;
        private ProcessFileInfo _StructureLoadInfo;
        private ProcessFileInfo _StructureSaveInfo;
        private ProcessFileInfo _DataLoadInfo;
        private ProcessFileInfo _DataSaveInfo;
        private ProcessFileInfo _DataPackLoadInfo;
        private ProcessFileInfo _DataPackSaveInfo;
        private ProcessFileInfo _LastProcessInfo;
        private ProcessFileInfo _LastLoadInfo;
        private ProcessFileInfo _LastSaveInfo;
        private ProcessFileInfo _LastWebDownloadInfo;
        #endregion
        #region Load : načítání
        /// <summary>
        /// Prvotní načtení dat, ze standardních souborů
        /// </summary>
        /// <param name="progress"></param>
        public void LoadInitialData(Action<ProgressArgs> progress = null)
        {
            ProcessQueueInfo processQueue = new ProcessQueueInfo(progress);
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.ClearAll));
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.LoadInitial));
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.DownloadUpdateNew));
            _DoProcessAsync(processQueue);
        }
        /// <summary>
        /// Importuje data z daného souboru.
        /// Detekuje zadaný string, uzpůsobí tomu načítací algoritmus, detekuje formát.
        /// Načítání stavu ze stránky: https://onemocneni-aktualne.mzcr.cz/api/v2/covid-19/obce.csv
        /// Načítání počtu obyvatel ze souboru ze stránky : https://www.mvcr.cz/clanek/statistiky-pocty-obyvatel-v-obcich.aspx
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        public void Load(string file, Action<ProgressArgs> progress = null)
        {
            ProcessQueueInfo processQueue = new ProcessQueueInfo(progress);
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.LoadFile, file));
            _DoProcessAsync(processQueue);
        }
        /// <summary>
        /// Importuje data z daného souboru.
        /// Detekuje zadaný string, uzpůsobí tomu načítací algoritmus, detekuje formát.
        /// Načítání stavu ze stránky: https://onemocneni-aktualne.mzcr.cz/api/v2/covid-19/obce.csv
        /// Načítání počtu obyvatel ze souboru ze stránky : https://www.mvcr.cz/clanek/statistiky-pocty-obyvatel-v-obcich.aspx
        /// </summary>
        /// <param name="content"></param>
        /// <param name="progress"></param>
        public void Load(byte[] content, Action<ProgressArgs> progress = null)
        {
            ProcessQueueInfo processQueue = new ProcessQueueInfo(progress);
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.LoadContent, content));
            _DoProcessAsync(processQueue);
        }
        /// <summary>
        /// Najde soubory, z nichž se budou načítat data. Soubory a navazující akce přidá do fronty.
        /// </summary>
        /// <param name="progress"></param>
        private void _LoadInitial(ProcessQueueInfo processQueue)
        {
            string standardDataPath = PathData;            // Tady jsou data ukládaná docela standardně
            string applicationDataPath = App.AppDataPath;  // Tady jsou data distribuovaná spolu s aplikací, ale sem se neukládají

            string structureFile = null;
            string dataFile = null;
            string dataPackFile = null;
            string pocetFile = null;
            string webObce1File = null;
            string webObce2File = null;

            // Připravíme si sekvenci akcí typu LoadFile a SaveFile, které provedem ihned po doběhnutí this akce:
            List<ProcessQueueItem> loadActions = new List<ProcessQueueItem>();

            if (loadActions.Count == 0)
            {   // 1. Standardní varianta: Struktura + Data:
                structureFile = SearchFile(StandardStructureFileName, standardDataPath, applicationDataPath);
                dataFile = SearchFile(StandardDataFileName, standardDataPath, applicationDataPath);
                if (structureFile != null && dataFile != null)
                {
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, structureFile));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, dataFile));
                }
            }

            if (loadActions.Count == 0)
            {   // 2. Záložní varianta: DataPack (vše v jednom), typicky první poinstalační spuštění:
                dataPackFile = SearchFile(StandardDataPackFileName, standardDataPath, applicationDataPath);
                if (dataPackFile != null)
                {
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, dataPackFile));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Structure));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Data));
                }
            }

            if (loadActions.Count == 0)
            {   // 3. Z plných webových dat (počet obyvatel + první verze Obce, obsahující kompletní strukturu):
                pocetFile = SearchFile(StandardWebPocetFileName, standardDataPath, applicationDataPath);
                webObce1File = SearchFile(StandardWebObce1FileName, standardDataPath, applicationDataPath);
                if (pocetFile != null && webObce1File != null)
                {
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, pocetFile));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, webObce1File));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Structure));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Data));
                }
            }

            if (loadActions.Count == 0)
            {   // 4. Z kombinovaných dat (struktura obcí + druhá verze Obce, obsahující NE-kompletní strukturu):
                webObce2File = SearchFile(StandardWebObce2FileName, standardDataPath, applicationDataPath);
                if (structureFile != null && webObce2File != null)
                {
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, pocetFile));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.LoadFile, webObce2File));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Structure));
                    loadActions.Add(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Data));
                }
            }

            processQueue.InsertActions(loadActions.ToArray());
        }
        /// <summary>
        /// Načte obsah daného souboru, detekuje a zpracuje jej
        /// </summary>
        /// <param name="processQueue"></param>
        private void _LoadFile(ProcessQueueInfo processQueue)
        {
            if (processQueue == null || processQueue.CurrentAction == null) return;

            ProcessQueueItem action = processQueue.CurrentAction;
            string fileName = action.File;
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentException($"Database.Import() : není zadán vstupní soubor.");
            if (!IO.File.Exists(fileName)) throw new ArgumentException($"Database.Import() : zadaný vstupní soubor {fileName} neexistuje.");
            ProcessFileInfo loadInfo = new ProcessFileInfo(DataMediumType.LocalFile, fileName);
            loadInfo.ProgressAction = processQueue.ProgressAction;
            action.ProcessFile = loadInfo;
            using (var stream = new DZipFileReader(fileName, CompressMode.ByContent))
            {
                _LoadStream(stream, action);
                stream.Close();
            }
        }
        /// <summary>
        /// Načte obsah daného bufferu, detekuje a zpracuje jej
        /// </summary>
        /// <param name="content"></param>
        /// <param name="progress"></param>
        private void _LoadContent(ProcessQueueInfo processQueue)
        {
            if (processQueue == null || processQueue.CurrentAction == null) return;

            ProcessQueueItem action = processQueue.CurrentAction;
            byte[] content = action.Content;
            if (content == null || content.Length == 0) throw new ArgumentException($"Database.Load() : není zadán vstupní obsah dat.");
            ProcessFileInfo loadInfo = new ProcessFileInfo(DataMediumType.BinaryContent, "Content");
            loadInfo.ProgressAction = processQueue.ProgressAction;
            action.ProcessFile = loadInfo;
            using (var stream = new DZipFileReader(content))
            {
                _LoadStream(stream, action);
                stream.Close();
            }
        }
        /// <summary>
        /// Načte data dodané v daném streamu (ten může pocházet ze souboru, ze zipu, z paměti...)
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="action"></param>
        private void _LoadStream(DZipFileReader stream, ProcessQueueItem action)
        {
            ProcessFileInfo loadInfo = action.ProcessFile;
            lock (this.InterLock)
            {
                State = StateType.LoadingFile;
                loadInfo.ProcessState = ProcessFileState.Open;
                loadInfo.Length = stream.Length;
                while (!stream.IsEnd)
                {
                    string line = stream.ReadLine();
                    _LoadLine(line, action);
                    _CallProgress(loadInfo, position: stream.Position);
                }
                this.StoreProcessFileResults(loadInfo);
                _CallProgress(loadInfo, force: true, isDone: true);
                State = StateType.Ready;
            }
        }
        /// <summary>
        /// Zpracuje daný řádek ze vstupního souboru
        /// </summary>
        /// <param name="line"></param>
        /// <param name="processInfo"></param>
        private void _LoadLine(string line, ProcessQueueItem action)
        {
            ProcessFileInfo processInfo = action.ProcessFile;
            if (String.IsNullOrEmpty(line)) return;
            line = line.Trim();
            switch (processInfo.ProcessState)
            {
                case ProcessFileState.Open:
                    processInfo.ContentType = _LoadDetectContentTypeByHeader(line, action);
                    this._CheckDataContent(action);
                    processInfo.ProcessState = ProcessFileState.Loading;
                    this._ClearData(processInfo.ContentType);
                    break;

                case ProcessFileState.Loading:
                    this._LoadContentByType(line, action);
                    break;
            }
        }
        /// <summary>
        /// Najde a vrátí fullname souboru na některém z adresářů, nejnovější.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        private string SearchFile(string fileName, string path1, string path2)
        {
            string fileNamePack = IO.Path.ChangeExtension(fileName, StandardPackExtension);

            // Přednost má nejnovější existující soubor, ze souborů v prvním nebo v druhém adresáři, otevřený nebo zipovaný formát:
            IO.FileInfo file = null;
            file = SearchBestFile(file, new IO.FileInfo(IO.Path.Combine(path1, fileName)));
            file = SearchBestFile(file, new IO.FileInfo(IO.Path.Combine(path1, fileNamePack)));
            file = SearchBestFile(file, new IO.FileInfo(IO.Path.Combine(path2, fileName)));
            file = SearchBestFile(file, new IO.FileInfo(IO.Path.Combine(path2, fileNamePack)));

            return file?.FullName;
        }
        /// <summary>
        /// Vrátí lepší soubor z daných dvou (=existující a novější)
        /// </summary>
        /// <param name="fileA"></param>
        /// <param name="fileB"></param>
        /// <returns></returns>
        private IO.FileInfo SearchBestFile(IO.FileInfo fileA, IO.FileInfo fileB)
        {
            bool existsA = (fileA != null && fileA.Exists);
            bool existsB = (fileB != null && fileB.Exists);
            if (!existsA && !existsB) return null;
            if (existsA && !existsB) return fileA;
            if (!existsA && existsB) return fileB;
            return (fileA.LastWriteTimeUtc >= fileB.LastWriteTimeUtc ? fileA : fileB);
        }
        #endregion
        #region Autodetekce a načítání datových souborů různých typů
        /// <summary>
        /// Podle titulkového řádku rozpozná obsah souboru
        /// </summary>
        /// <param name="header"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private FileContentType _LoadDetectContentTypeByHeader(string header, ProcessQueueItem action)
        {
            ProcessFileInfo processInfo = action.ProcessFile;

            if (header.StartsWith(StructureHeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.Structure;
            if (header.StartsWith(DataHeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.Data;
            if (header.StartsWith(DataPackHeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.DataPack;
            if (String.Equals(header, Covid1HeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.CovidObce1;
            if (String.Equals(header, Covid2HeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.CovidObce2;
            if (String.Equals(header, Covid3HeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.CovidObce3;
            if (String.Equals(header, PocetHeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.PocetObyvatel;
            if (String.Equals(header, UmrtiHeaderExpected, StringComparison.CurrentCultureIgnoreCase)) return FileContentType.Umrti;

            string name = IO.Path.GetFileName(processInfo.FileName);
            throw new FormatException($"Database.Load() : zadaný vstupní soubor {name} nemá odpovídající záhlaví (úvodní řádek).");
        }
        /// <summary>
        /// Metoda prověří stav databáze před načítáním obsahu daného souboru
        /// </summary>
        /// <param name="action"></param>
        private void _CheckDataContent(ProcessQueueItem action)
        {
            ProcessFileInfo processInfo = action.ProcessFile;
            switch (processInfo.ContentType)
            {
                case FileContentType.CovidObce2:
                case FileContentType.CovidObce3:
                case FileContentType.Umrti:
                    if (this._Vesnice == null || this._Vesnice.Count == 0)
                    {
                        throw new InvalidOperationException($"Nelze načítat data typu {processInfo.ContentType} do databáze, která nemá načtenou strukturu obcí.");
                    }
                    break;
            }
        }
        /// <summary>
        /// Načte řádek s daty, druh dat je již detekován
        /// </summary>
        /// <param name="line"></param>
        /// <param name="processInfo"></param>
        private void _LoadContentByType(string line, ProcessQueueItem action)
        {
            switch (action.ProcessFile.ContentType)
            {
                case FileContentType.Structure:
                    _LoadLineStructure(line, action);
                    break;
                case FileContentType.Data:
                    _LoadLineData(line, action);
                    break;
                case FileContentType.DataPack:
                    _LoadLineDataPack(line, action);
                    break;
                case FileContentType.CovidObce1:
                    _LoadLineCovid1(line, action);
                    break;
                case FileContentType.CovidObce2:
                    _LoadLineCovid2(line, action);
                    break;
                case FileContentType.CovidObce3:
                    _LoadLineCovid3(line, action);
                    break;
                case FileContentType.PocetObyvatel:
                    _LoadLinePocet(line, action);
                    break;
                case FileContentType.Umrti:
                    _LoadLineUmrti(line, action);
                    break;
            }
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.Structure"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineStructure(string line, ProcessQueueItem action)
        {
            _LoadLineDataPack(line, action);
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.Data"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineData(string line, ProcessQueueItem action)
        {
            string[] items = line.Split(';');
            if (items.Length < 3) return;

            ProcessFileCurrentInfo currentInfo = _LoadGetCurrentInfo(action);

            string header = items[0];
            switch (header)
            {
                case HeaderVesnice:
                    string vesniceKod = items[1];
                    bool hasVesnice = (this._Vesnice.TryGetValue(vesniceKod, out var vesnice));
                    currentInfo.Vesnice = (hasVesnice ? vesnice : null);
                    break;
                case HeaderInfo:
                    if (currentInfo.Vesnice != null)
                    {
                        DateTime infoDate = GetDate(items[1]);
                        if (IsValidDate(infoDate, action))
                        {
                            int key = infoDate.GetDateKey();
                            var info = currentInfo.Vesnice.AddOrCreateInfo(infoDate, key);
                            int newCount = GetInt32(items[2]);
                            info.AddData(DataValueType.SourceNewCount, newCount);
                            int currentCount = GetInt32(items[3]);
                            info.AddData(DataValueType.SourceCurrentCount, currentCount);
                            bool hasValidData = (newCount != 0 && currentInfo.Vesnice.PocetObyvatel != 0);
                            _RegisterMaxContentTime(infoDate, hasValidData);
                        }
                    }
                    break;
            }

            action.ProcessFile.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.DataPack"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineDataPack(string line, ProcessQueueItem action)
        {
            string[] items = line.Split(';');
            int count = items.Length;
            if (count < 3) return;

            ProcessFileCurrentInfo currentInfo = _LoadGetCurrentInfo(action);

            string header = items[0];
            string code = items[1];

            switch (header)
            {
                case HeaderZeme:
                    currentInfo.Zeme = currentInfo.World.AddOrCreateChild(code, () => new EntityInfo(currentInfo.World, items));
                    break;
                case HeaderKraj:
                    currentInfo.Kraj = currentInfo.Zeme.AddOrCreateChild(code, () => new EntityInfo(currentInfo.Zeme, items));
                    break;
                case HeaderOkres:
                    currentInfo.Okres = currentInfo.Kraj.AddOrCreateChild(code, () => new EntityInfo(currentInfo.Kraj, items));
                    break;
                case HeaderMesto:
                    currentInfo.Mesto = currentInfo.Okres.AddOrCreateChild(code, () => new EntityInfo(currentInfo.Okres, items));
                    break;
                case HeaderObec:
                    currentInfo.Obec = currentInfo.Mesto.AddOrCreateChild(code, () => new EntityInfo(currentInfo.Mesto, items));
                    break;
                case HeaderVesnice:
                    currentInfo.Vesnice = currentInfo.Obec.AddOrCreateChild(code, () => new EntityInfo(currentInfo.Obec, items));
                    this._Vesnice.AddIfNotContains(code, currentInfo.Vesnice);
                    if (currentInfo.Vesnice.IsPocetObyvLoaded)
                        this._Pocet.AddOrUpdate(code, currentInfo.Vesnice.PocetObyv);
                    break;
                case HeaderPocet:
                    bool hasPocet = false;
                    PocetObyvatelInfo pocet = null;
                    if (count == 9)
                        // P;554979;Abertamy;Ostrov;Karlovarský;458;412;422;368
                        hasPocet = PocetObyvatelInfo.TryCreate(items, 5, out pocet);
                    else if (count == 6)
                        // P;554979;458;412;422;368
                        hasPocet = PocetObyvatelInfo.TryCreate(items, 2, out pocet);
                    this._Pocet.AddOrUpdate(code, pocet);
                    break;
                case HeaderInfo:
                    DateTime infoDate = GetDate(items[1]);
                    if (IsValidDate(infoDate, action))
                    {
                        int key = infoDate.GetDateKey();
                        var info = currentInfo.Vesnice.AddOrCreateInfo(infoDate, key);
                        int newCount = GetInt32(items[2]);
                        info.AddData(DataValueType.SourceNewCount, newCount);
                        int currentCount = GetInt32(items[3]);
                        info.AddData(DataValueType.SourceCurrentCount, currentCount);
                        bool hasValidData = (newCount != 0 && currentInfo.Vesnice.PocetObyvatel != 0);
                        _RegisterMaxContentTime(infoDate, hasValidData);
                    }
                    break;
            }

            action.ProcessFile.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.CovidObce1"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineCovid1(string line, ProcessQueueItem action)
        {
            // Verze 1 obsahuje 5 úrovní: Kraj - Okres - Město - Obec - Vesnice
            // Příklad řádku se všemi úrovněmi:
            // čtvrtek;2020-08-20;CZ032;Plzeňský kraj;CZ0327;Tachov;3213;Stříbro;32131;Bezdružice;541290;Horní Kozolupy;0;0
            string[] items = line.Split(';');
            if (items.Length != Covid1ItemCountExpected) return;

            EntityInfo world = this._World;
            EntityInfo zeme = world.AddOrCreateChild("CZ", () => new EntityInfo(world, "CZ", "Česká republika"));
            EntityInfo kraj = zeme.AddOrCreateChild(items[2], () => new EntityInfo(zeme, items, 2));
            EntityInfo okres = kraj.AddOrCreateChild(items[4], () => new EntityInfo(zeme, items, 4));
            EntityInfo mesto = okres.AddOrCreateChild(items[6], () => new EntityInfo(zeme, items, 6));
            EntityInfo obec = mesto.AddOrCreateChild(items[8], () => new EntityInfo(zeme, items, 8));
            EntityInfo vesnice = obec.AddOrCreateChild(items[10], () => new EntityInfo(obec, items, 10));

            this._Vesnice.AddIfNotContains(items[10], vesnice);

            DateTime infoDate = GetDate(items[1]);
            if (IsValidDate(infoDate, action))
            {
                int key = infoDate.GetDateKey();
                var info = vesnice.AddOrCreateInfo(infoDate, key);
                int newCount = GetInt32(items[12]);
                info.AddData(DataValueType.SourceNewCount, newCount);
                int currentCount = GetInt32(items[13]);
                info.AddData(DataValueType.SourceCurrentCount, currentCount);
                bool hasValidData = (newCount != 0 && vesnice.PocetObyvatel != 0);
                _RegisterMaxContentTime(infoDate, hasValidData);
            }

            action.ProcessFile.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.CovidObce2"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineCovid2(string line, ProcessQueueItem action)
        {
            _LoadLineCovid23(line, action, Covid2ItemCountExpected);
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.CovidObce3"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineCovid3(string line, ProcessQueueItem action)
        {
            // Verze 3 obsahuje totéž co verze 2, pouze má navíc sloupce: ",nove_pripady_65,nove_pripady_7_dni,nove_pripade_14_dni"
            // Zatím je nenačítám, takže použiju načítadlo společné pro verzi 2 a 3:
            _LoadLineCovid23(line, action, Covid3ItemCountExpected);
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.CovidObce2"/> nebo <see cref="FileContentType.CovidObce3"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineCovid23(string line, ProcessQueueItem action, int itemCountExpected)
        {
            // Verze 2 i 3 obsahuje 4 úrovně: Kraj - Okres - Město - Obec - Vesnice
            // Příklad řádku se všemi úrovněmi pro verzi 1:
            // neděle;2020-03-01;CZ053;Pardubický kraj;CZ0531;Chrudim;5304;Chrudim;53043;Chrudim;571164;Chrudim;0;0
            // Tentýž řádek ve verzi 2:
            // neděle,2020-03-01,CZ053,"Pardubický kraj",CZ0531,Chrudim,5304,Chrudim,            571164,Chrudim,0,0

            // Abychom měli data v paměti umístěná ve správné struktuře (plných 5 úrovní, a nikoli jedna z nich Void), 
            // tak nebudeme z tohoto souboru načítat kompletní strukturu obcí = tato musí být načtena dříve,
            // a zde budeme načítat pouze kód Vesnice, tu dohledáme v indexu, a do ní vepíšeme data Info:
            line = line.Replace("\"", "");
            string[] items = line.Split(',');
            if (items.Length != itemCountExpected) return;

            string vesniceKod = items[8];        // 571164
            if (!this._Vesnice.TryGetValue(vesniceKod, out EntityInfo vesnice))
            {   // V datech je kód 5. úrovně "Vesnice", ale my jej nemáme ve struktuře:
                string okresNazev = items[5];
                string mestoNazev = items[7];
                string vesniceNazev = items[9];
                action.ProcessQueue.AddMessage($"Ve vstupních datech {action.ProcessFile.ContentType} je uvedena obec {vesniceKod}: {vesniceNazev}, patřící do města {mestoNazev} (okres {okresNazev}), ale tuto obec nemáme načtenou ve struktuře obcí.");
                return;
            }

            DateTime infoDate = GetDate(items[1]);
            if (IsValidDate(infoDate, action))
            {
                int key = infoDate.GetDateKey();
                var info = vesnice.AddOrCreateInfo(infoDate, key);
                int newCount = GetInt32(items[10]);
                info.AddData(DataValueType.SourceNewCount, newCount);
                int currentCount = GetInt32(items[11]);
                info.AddData(DataValueType.SourceCurrentCount, currentCount);
                bool hasValidData = (newCount != 0 && vesnice.PocetObyvatel != 0);
                _RegisterMaxContentTime(infoDate, hasValidData);
            }

            action.ProcessFile.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.PocetObyvatel"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLinePocet(string line, ProcessQueueItem action)
        {
            // ;kraj;mesto;kod_obce;nazev_obce;muzi;muzi15;zeny;zeny15;celkem;celkem15
            // ;Pardubický;Chrudim;571164;Chrudim;10 899;9 192;11 720;10 164;22 619;19 356

            string[] items = line.Split(';');
            if (items.Length != PocetItemCountExpected) return;

            string kod = items[3];                         // Kód vesnice
            int pocetMuziCelkem = GetInt32(items[5]);
            int pocetMuziNad15 = GetInt32(items[6]);
            int pocetZenyCelkem = GetInt32(items[7]);
            int pocetZenyNad15 = GetInt32(items[8]);

            PocetObyvatelInfo pocet = new PocetObyvatelInfo(pocetMuziCelkem, pocetMuziNad15, pocetZenyCelkem, pocetZenyNad15);
            this._Pocet.AddOrUpdate(kod, pocet);

            action.ProcessFile.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.Umrti"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineUmrti(string line, ProcessQueueItem action)
        {
            // Struktura: "datum,vek,pohlavi,kraj_nuts_kod,okres_lau_kod"
            //              0     1   2       3             4
            string[] items = line.Split(',');
            if (items.Length < 5) return;

            string fullCode = "CZ." + items[3] + "." + items[4];               // CZ.KRAJ.OKRES
            EntityInfo entity = GetEntity(fullCode);
            if (entity == null)
            {
                action.ProcessQueue.AddMessage($"Ve vstupních datech {action.ProcessFile.ContentType} je uvedena informace pro entitu {fullCode}, ale tuto entitu nemáme načtenou ve struktuře obcí.");
                return;
            }

            DateTime infoDate = GetDate(items[0]);
            if (IsValidDate(infoDate, action))
            {
                int key = infoDate.GetDateKey();
                var info = entity.AddOrCreateInfo(infoDate, key);
                int age = GetInt32(items[1]);
                string poh = items[2].Trim().ToUpper();
                DataGenderType gender = (poh == "M" ? DataGenderType.Male : ((poh == "F" || poh == "Z") ? DataGenderType.Female : DataGenderType.Male));
                info.AddData(DataValueType.SourceNewDeath, 1, gender, age, 1);
            }

            action.ProcessFile.RecordCount += 1;
        }
        /// <summary>
        /// Metoda v případě potřeby vytvoří new instanci <see cref="ProcessFileCurrentInfo"/> do <see cref="ProcessFileInfo.CurrentInfo"/>, a vrátí ji.
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <returns></returns>
        private ProcessFileCurrentInfo _LoadGetCurrentInfo(ProcessQueueItem action)
        {
            var processInfo = action.ProcessFile;
            if (processInfo.CurrentInfo == null)
            {   // Výchozí pozice je vždy nastavena na náš World:
                processInfo.CurrentInfo = new ProcessFileCurrentInfo();
                processInfo.CurrentInfo.World = _World;
            }
            return processInfo.CurrentInfo;
        }
        /// <summary>
        /// Zaeviduje maximální datum s daty
        /// </summary>
        /// <param name="infoDate"></param>
        private void _RegisterMaxContentTime(DateTime infoDate, bool hasValidData)
        {
            if (!_DataContentTime.HasValue || infoDate > _DataContentTime.Value)
                _DataContentTime = infoDate;

            if (hasValidData && (!_LastValidDataDate.HasValue || infoDate > _LastValidDataDate.Value))
                _LastValidDataDate = infoDate;
        }
        #endregion
        #region WebUpdate : aktualizace dat z internetu
        /// <summary>
        /// Provede aktualizaci dat z internetu. Asynchronní metoda, vrátí řízení ihned, v průběhu downloadu a po jeho dokončení se volá daná akce Progress.
        /// </summary>
        /// <param name="progress"></param>
        public void WebUpdate(Action<ProgressArgs> progress = null)
        {
            ProcessQueueInfo processQueue = new ProcessQueueInfo(progress);
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.DownloadUpdateAll));
            _DoProcessAsync(processQueue);
        }
        /// <summary>
        /// Obsahuje true, pokud je vhodný čas na aktualizaci dat z internetu
        /// </summary>
        public bool WebUpdateIsNeed
        {
            get
            {
                if (!this._DataContentTime.HasValue) return true;                   // Pokud neznám datum dat, pak ano
                DateTime content = this._DataContentTime.Value;
                DateTime now = DateTime.Now;
                int diff = ((TimeSpan)(now.Date - content.Date)).Days;              // Kolik dní jsou stará data? 0=dnešní, 1=včerejší, 2=předvčerejší, ...
                if (diff <= 0) return false;                                        // Pokud data jsou dnešní (bez ohledu na čas dat, a čas aktuální), pak není třeba dělat download
                if (diff > 1) return true;                                          // Předvčerejší data: stáhněme nová
                return (now.TimeOfDay >= StandardUpdateTimeWebCzV2);                    // Data jsou právě včerejší: nová stáhneme jen tehdy, když je aktuální čas větší než čas, kdy se data na webu aktualizují
            }
        }
        /// <summary>
        /// Metoda zjistí, které datové sady je třeba stáhnout z internetu a zařadí je do fronty
        /// </summary>
        /// <param name="processQueue"></param>
        /// <returns></returns>
        private void _WebDownloadUpdate(ProcessQueueInfo processQueue, bool forceAll)
        {
            if (forceAll || _WebUpdateIsTimeForUpload(ConfigUserDataTimeDownloadCzV2Obce, StandardUpdateTimeWebCzV2))
                processQueue.AddActions(new ProcessQueueItem(ProcessActionType.DownloadUrl, StandardUpdateUrlCzV2Obce) { FileType = FileContentType.CovidObce3, WebUpdateTimeConfigName = ConfigUserDataTimeDownloadCzV2Obce, WebUpdateMinAcceptSize = 20000000 });

            if (forceAll || _WebUpdateIsTimeForUpload(ConfigUserDataTimeDownloadCzV2Umrti , StandardUpdateTimeWebCzV2))
                processQueue.AddActions(new ProcessQueueItem(ProcessActionType.DownloadUrl, StandardUpdateUrlCzV2Umrti) { FileType = FileContentType.Umrti, WebUpdateTimeConfigName = ConfigUserDataTimeDownloadCzV2Umrti , WebUpdateMinAcceptSize = 1000 });

        }
        /// <summary>
        /// Metoda vrátí true, pokud je vhodné provést download dat z webu, když poslední stahování dat bylo v daném čase, a hodina aktualizace je zadaná.
        /// </summary>
        /// <param name="lastDateTime"></param>
        /// <param name="updateTime"></param>
        /// <returns></returns>
        private bool _WebUpdateIsTimeForUpload(string configName, TimeSpan standardUpdateTime)
        {
            DateTime? lastUpdateTime = App.Config.UserDataGet(configName, (DateTime?)null);
            return _WebUpdateIsTimeForUpload(lastUpdateTime, standardUpdateTime);
        }
        /// <summary>
        /// Metoda vrátí true, pokud je vhodné provést download dat z webu, když poslední stahování dat bylo v daném čase, a hodina aktualizace je zadaná.
        /// </summary>
        /// <param name="lastUpdateTime"></param>
        /// <param name="standardUpdateTime"></param>
        /// <returns></returns>
        private bool _WebUpdateIsTimeForUpload(DateTime? lastUpdateTime, TimeSpan standardUpdateTime)
        {
            if (!lastUpdateTime.HasValue) return true;

            DateTime now = DateTime.Now;
            DateTime nowDate = now.Date;
            TimeSpan nowTime = now.TimeOfDay;

            // Čas, kdy měla proběhnout poslední aktualizace dat na webu: pokud aktuální čas je větší, pak dnešní aktualizace, jinak včerejší:
            DateTime lastUpdate = ((nowTime > standardUpdateTime) ? (nowDate + standardUpdateTime) : (nowDate.AddDays(-1d) + standardUpdateTime));

            return (lastUpdateTime.Value < lastUpdate);
        }
        /// <summary>
        /// Provede aktualizaci z internetu
        /// </summary>
        /// <param name="processQueue"></param>
        private bool _WebDownloadUrl(ProcessQueueInfo processQueue)
        {
            if (processQueue == null || processQueue.CurrentAction == null) return false;

            ProcessQueueItem action = processQueue.CurrentAction;

            action.File = _WebDownloadGetUrl(action);

            ProcessFileInfo updateInfo = new ProcessFileInfo(DataMediumType.WebUrl, action.File);
            updateInfo.ContentType = action.FileType ?? FileContentType.None;
            updateInfo.ProcessState = ProcessFileState.WebDownloading;
            updateInfo.ProgressAction = processQueue.ProgressAction;
            action.ProcessFile = updateInfo;

            State = StateType.Downloading;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                wc.DownloadProgressChanged += _WebClient_DownloadProgressChanged;
                wc.DownloadDataCompleted += _WebClient_DownloadDataCompleted;
                Uri uri = new Uri(action.File);
                wc.DownloadDataAsync(uri, action);
            }

            return true;               // Rozběhla se asynchronní akce => vrátíme true.
        }
        /// <summary>
        /// Vrátí URL pro danou akci
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private string _WebDownloadGetUrl(ProcessQueueItem action)
        {
            if (!String.IsNullOrEmpty(action.File)) return action.File;
            if (!action.FileType.HasValue) return null;

            FileContentType contentType = action.FileType.Value;
            switch (contentType)
            {
                case FileContentType.CovidObce1:
                case FileContentType.CovidObce2:
                case FileContentType.CovidObce3:
                    return StandardUpdateUrlCzV2Obce;
                case FileContentType.Umrti:
                    return StandardUpdateUrlCzV2Umrti;
                default:
                    throw new ArgumentException($"Nelze z internetu aktualizovat data typu: {contentType}.");
            }
        }
        /// <summary>
        /// Progress v Downloadu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _WebClient_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            // Sem přijde řízení po každých cca 16 kB:
            if (!(e.UserState is ProcessQueueItem action)) return;
            ProcessFileInfo updateInfo = action.ProcessFile;

            if (updateInfo.Length != e.TotalBytesToReceive)
                updateInfo.Length = e.TotalBytesToReceive;
            updateInfo.Position = e.BytesReceived;

            _CallProgress(updateInfo);
        }
        /// <summary>
        /// Po dokončení downloadu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _WebClient_DownloadDataCompleted(object sender, System.Net.DownloadDataCompletedEventArgs e)
        {   // Sem přijde řízení po jakémkoli způsobu dokončení přenosu (OK, error, cancel)
            State = StateType.Ready;

            if (!(e.UserState is ProcessQueueItem action)) return;
            if (action == null) return;

            _WebDownloadDataCompleted(e, action);

            // Restart fronty po doběhnutí downloadu jednoho souboru: pokud máme download data, pak jsou ve frontě na první pozici a fronta vyvolá metodu _WebDownloadDone():
            // Proč "restart"? Protože spuštění downloadu v metodě _WebDownloadUrl() se provedlo asynchronně, a provádění fronty akcí se pak zrušilo (metoda _DoProcessDownloadUrl() vrátila false, a process loop skončila).
            // Takže nyní vezmeme frontu akcí ProcessQueue a znovu ji spustíme, asynchronně = na pozadí, a pojedou tam veškeré další operace.
            _DoProcessAsync(action.ProcessQueue);
        }
        /// <summary>
        /// Po doběhnutí downloadu zpracuje jeho chyby, nebo získá načtená data a insertuje je do fronty pro další zpracování
        /// </summary>
        /// <param name="e"></param>
        /// <param name="action"></param>
        private void _WebDownloadDataCompleted(System.Net.DownloadDataCompletedEventArgs e, ProcessQueueItem action)
        {
            if (e.Cancelled) return;
            if (e.Error != null)
            {
                Data.App.ShowError(e.Error.Message);
                return;
            }

            ProcessFileInfo updateInfo = action.ProcessFile;
            if (updateInfo == null)
            {
                Data.App.ShowError($"Po dokončení downloadu dat nelze provést zpracování, nebyl předán objekt 'ProcessFileInfo updateInfo'.");
                return;
            }

            // Zdejší metoda běží v Main threadu (to je dané WebClientem), pro následující akce je to OK:
            this.StoreProcessFileResults(updateInfo);
            _CallProgress(updateInfo, force: true, isDone: true);

            // Zpracování načtených dat ale chceme provést v threadu OnBackground, proto tuto akci vložíme na začátek do fronty akcí:
            var content = e.Result;
            ProcessQueueInfo processQueue = action.ProcessQueue;
            processQueue.InsertActions(new ProcessQueueItem(ProcessActionType.DownloadDone, content) { ProcessFile = updateInfo, WebUpdateTimeConfigName = action.WebUpdateTimeConfigName, WebUpdateMinAcceptSize = action.WebUpdateMinAcceptSize });
        }
        /// <summary>
        /// Provádí se po downloadu a po prověření dat z internetu: zazálohuje dodaná data z internetu, načte je do databáze, databázi prověří a případně rollbackuje.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="updateInfo"></param>
        private void _WebDownloadDone(ProcessQueueInfo processQueue)
        {
            if (processQueue == null || processQueue.CurrentAction == null) return;

            ProcessQueueItem action = processQueue.CurrentAction;
            byte[] content = action.Content;
            ProcessFileInfo updateInfo = action.ProcessFile;

            if (!this._IsValidDownloadData(content, action))
            {
                processQueue.AddMessage($"Data načtená z internetu ({updateInfo.ContentType}) nejsou zřejmě v pořádku, byla odmítnuta.");
                return;
            }

            this._BackupContentToFile(content, updateInfo);

            processQueue.InsertActions(new ProcessQueueItem(ProcessActionType.LoadContent, content));
            _DoProcessOneAction(processQueue);                                 // Pozor, od této chvíle je v processQueue.CurrentAction akce LoadContent, kdežto v action je uchována akce DownloadDone!

            if (_IsValidDataAfterUpdate(out string errorMessage))
            {   // Načtená data jsou OK: uložím si datum načtení, a uložíme data do souboru Data:
                App.Config.UserDataAdd(action.WebUpdateTimeConfigName, DateTime.Now);

                processQueue.InsertActions(new ProcessQueueItem(ProcessActionType.SaveFile, FileContentType.Data) { SaveFileFormat = SaveFormat.Pack, SaveFileBackupMode = BackupMode.OneForDay });
            }
            else
            {   // Po chybě při načítání dat: zajistíme restore dat (Clear + LoadInitial), a chybovou hlášku:
                // Tím se vrátíme k posledně uloženému stavu dat.
                // V případě vícenásobného update z internetu (více stahování) a chybě někde uprostřed se tímto principem vracíme ke stavu z předchozího update.
                processQueue.InsertActions(new ProcessQueueItem(ProcessActionType.ClearAll), new ProcessQueueItem(ProcessActionType.LoadInitial));

                processQueue.AddMessage($"Data načtená z internetu ({updateInfo.ContentType}) neobsahují platné informace a budou zahozena.\r\nProblém: {errorMessage}.");
            }
        }
        /// <summary>
        /// Daný obsah uloží jako zálohu 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="updateInfo"></param>
        private void _BackupContentToFile(byte[] content, ProcessFileInfo updateInfo)
        {
            try
            {
                string backupName = IO.Path.Combine(PathBackup, StandardBackupdownloadFileName);
                IO.FileInfo backupFile = new IO.FileInfo(backupName);
                if (backupFile.Exists) backupFile.Delete();
                using (var stream = new DZipFileWriter(backupName, CompressMode.Compress, true))
                {
                    stream.WriteBuffer(content);
                }
            }
            catch (Exception exc) { }
        }
        /// <summary>
        /// Zlehka ověří data stažená z internetu, zda vypadají OK
        /// </summary>
        /// <param name="content"></param>
        /// <param name="updateInfo"></param>
        /// <returns></returns>
        private bool _IsValidDownloadData(byte[] content, ProcessQueueItem action)
        {
            bool isValid = true;
            if (action.WebUpdateMinAcceptSize.HasValue && action.WebUpdateMinAcceptSize.Value > 0 && content.Length < action.WebUpdateMinAcceptSize.Value)
            {
                ProcessFileInfo updateInfo = action.ProcessFile;
                action.ProcessQueue.AddMessage($"Data typu {action.FileType} načtená z internetu jsou příliš malá a nebudou použita. Načteno: {content.Length}, očekáváno nejméně: {action.WebUpdateMinAcceptSize.Value}.");
                isValid = false;
            }
            return isValid;
        }
        /// <summary>
        /// Prověří data v databázi, zda jsou OK.
        /// Používá se po downloadu a aplikaci dat z internetu.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private bool _IsValidDataAfterUpdate(out string errorMessage)
        {
            errorMessage = null;

            // Získám data z databáze za jeden okres, za minulý měsíc, a vyhodnotím zda obsahuje přiměřené hodnoty:
            string entityCode = "CZ.CZ053.CZ0531";               // Okres Chrudim
            var entity = GetEntity(entityCode);
            if (entity == null)
            {
                errorMessage = "V datech není nalezen testovací okres Chrudim.";
                return false;
            }

            DateTime now = DateTime.Now;
            DateTime end = new DateTime(now.Year, now.Month, 1);
            DateTime begin = end.AddMonths(-1);
            var result = this.GetResultSimple(entity, DataValueType.NewCount, begin: begin, end: end);
            if (result == null || result.Results == null)
            {
                errorMessage = "V datech testovacího okresu Chrudim nejsou k dispozici výsledky.";
                return false;
            }
            if (result.Results.Length < 25)
            {
                errorMessage = $"V datech testovacího okresu Chrudim nejsou k dispozici výsledky za celý minulý měsíc, obsahuje jen {result.Results.Length} denních hodnot.";
                return false;
            }
            decimal maxValue = result.Results.Select(r => r.Value).Max();
            if (maxValue < 2m)
            {
                errorMessage = $"V datech testovacího okresu Chrudim nejsou k dispozici výsledky za celý minulý měsíc, hodnoty nových případů jsou menší než 2.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Čas, kdy je aktualizována webová databáze. Obsahuje hodiny a minuty, typicky 8:15.
        /// </summary>
        protected static TimeSpan StandardUpdateTimeWebCzV2 { get { return TimeSpan.FromHours(8.25d); } }

        protected const string ConfigUserDataTimeDownloadCzV2Obce = "DownloadTimeCzV2WebObce";
        protected const string StandardUpdateUrlCzV2Obce = @"https://onemocneni-aktualne.mzcr.cz/api/v2/covid-19/obce.csv";
        protected const string ConfigUserDataTimeDownloadCzV2Umrti = "DownloadTimeCzV2WebUmrti";
        protected const string StandardUpdateUrlCzV2Umrti = @"https://onemocneni-aktualne.mzcr.cz/api/v2/covid-19/umrti.csv";


        #endregion
        #region Save : ukládání do interního formátu
        /// <summary>
        /// Uloží sadu souborů dle požadavku do standardního umístění (volající nemůže specifikovat, kam se mají soubory ukládat).
        /// </summary>
        /// <param name="fileTypes"></param>
        /// <param name="saveFormat"></param>
        /// <param name="backupMode"></param>
        /// <param name="progress"></param>
        public void SaveStandardData(IEnumerable<FileContentType> fileTypes , SaveFormat saveFormat = SaveFormat.Pack, BackupMode backupMode = BackupMode.OneForDay, Action<ProgressArgs> progress = null)
        {
            ProcessQueueInfo processQueue = new ProcessQueueInfo(progress);
            foreach (var fileType in fileTypes)
                processQueue.AddActions(new ProcessQueueItem(ProcessActionType.SaveFile, fileType) { SaveFileFormat = saveFormat, SaveFileBackupMode = backupMode });
            App.TryRun(() => _DoProcess(processQueue));
        }
        /// <summary>
        /// Uloží data do souboru v interním formátu
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        public void Save(FileContentType contentType, string file, SaveFormat saveFormat = SaveFormat.Pack, BackupMode backupMode = BackupMode.OneForDay, Action<ProgressArgs> progress = null)
        {
            ProcessQueueInfo processQueue = new ProcessQueueInfo(progress);
            processQueue.AddActions(new ProcessQueueItem(ProcessActionType.SaveFile, contentType) { File = file, SaveFileFormat = saveFormat, SaveFileBackupMode = backupMode });
            App.TryRun(() => _DoProcess(processQueue));
        }
        /// <summary>
        /// Zajistí uložení dat dle požadavku. Zajistí i zálohování stávajícího souboru.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        private void _SaveDataFile(ProcessQueueInfo processQueue)
        {
            ProcessQueueItem action = processQueue.CurrentAction;

            if (action == null || !action.FileType.HasValue)
                throw new ArgumentException($"Databáze nemá dodané podklady pro ukládání: není dodaná akce, nebo neobsahuje typ souboru.");

            FileContentType contentType = action.FileType.Value;
            if (!(contentType == FileContentType.Structure || contentType == FileContentType.Data || contentType == FileContentType.DataPack))
                throw new ArgumentException($"Databáze může ukládat pouze data typu {FileContentType.Structure}, {FileContentType.Data} nebo {FileContentType.DataPack}. Nelze uložit data typu {contentType}.");

            string fileName = _SaveDataGetFileName(action);

            string path = IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

            _SaveDataBackupOldFile(fileName, action);

            lock (this.InterLock)
            {
                try
                {
                    State = StateType.SavingFile;
                    ProcessFileInfo saveInfo = new ProcessFileInfo(DataMediumType.LocalFile, fileName);
                    saveInfo.ProgressAction = processQueue.ProgressAction;
                    saveInfo.ContentType = contentType;
                    saveInfo.ProcessState = ProcessFileState.Saving;
                    action.ProcessFile = saveInfo;

                    using (var stream = new DZipFileWriter(fileName, CompressMode.ByName, false))
                    {
                        _SaveFileHeader(saveInfo, stream);
                        _SaveFileProperties(saveInfo, stream);
                        _World.Save(saveInfo, stream);
                    }
                    this.StoreProcessFileResults(saveInfo);
                    _CallProgress(saveInfo, force: true, isDone: true);
                }
                finally
                {
                    State = StateType.Ready;
                }
            }
        }
        /// <summary>
        /// Vrátí reálné jméno souboru pro ukládání dat
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private string _SaveDataGetFileName(ProcessQueueItem action)
        {
            if (!String.IsNullOrEmpty(action.File)) return action.File;
            if (!action.FileType.HasValue) return null;

            FileContentType contentType = action.FileType.Value;
            string name = null;
            switch (contentType)
            {
                case FileContentType.Structure:
                    name = StandardStructureFileName;
                    break;
                case FileContentType.Data:
                    name = StandardDataFileName;
                    break;
                case FileContentType.DataPack:
                    name = StandardDataPackFileName;
                    break;
                default:
                    throw new ArgumentException($"Nelze uložit data typu: {contentType}.");
            }
            if (action.SaveFileFormat.HasValue && action.SaveFileFormat.Value == SaveFormat.Pack)
                name = IO.Path.ChangeExtension(name, StandardPackExtension);
            else if (action.SaveFileFormat.HasValue && action.SaveFileFormat.Value == SaveFormat.Zip)
                name = IO.Path.ChangeExtension(name, ".zip");
            string fullName = IO.Path.Combine(PathData, name);
            return fullName;
        }
        /// <summary>
        /// Zajistí zálohování existujícího souboru daného jména podle požadavku.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="args"></param>
        private void _SaveDataBackupOldFile(string fileName, ProcessQueueItem action)
        {
            if (action == null || !action.SaveFileBackupMode.HasValue) return;

            var mode = action.SaveFileBackupMode.Value;
            if (mode == BackupMode.None || mode == BackupMode.Overwrite) return;

            IO.FileInfo fileInfo = new IO.FileInfo(fileName);
            if (!fileInfo.Exists) return;

            string path = PathBackup;
            string name = IO.Path.GetFileNameWithoutExtension(fileName);
            string extension = IO.Path.GetExtension(fileName);

            string backupName = null;
            switch (mode)
            {
                case BackupMode.OneForDay:
                    backupName = IO.Path.Combine(path, name + "_" + fileInfo.LastWriteTime.ToString("yyyyMMdd") + extension);
                    break;
                case BackupMode.AllFiles:
                    backupName = IO.Path.Combine(path, name + "_" + fileInfo.LastWriteTime.ToString("yyyyMMdd-HHmm") + extension);
                    break;
            }
            if (backupName != null)
            {
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                IO.FileInfo backupFile = new IO.FileInfo(backupName);
                if (backupFile.Exists)
                    backupFile.Delete();
                IO.File.Move(fileName, backupName);
            }
        }
        /// <summary>
        /// Do streamu zapíše řádek popisující záhlaví souboru daného typu.
        /// Podle záhlaví poté bude soubor identifikován.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private void _SaveFileHeader(ProcessFileInfo saveInfo, DZipFileWriter stream)
        {
            switch (saveInfo.ContentType)
            {
                case FileContentType.Structure:
                    stream.WriteLine(StructureHeaderExpected);
                    break;
                case FileContentType.Data:
                    stream.WriteLine(DataHeaderExpected);
                    break;
                case FileContentType.DataPack:
                    stream.WriteLine(DataPackHeaderExpected);
                    break;
                default:
                    throw new ArgumentException($"Neplatná hodnota ContentType: {saveInfo.ContentType} v metodě Database._SaveFileHeader().");
            }
        }
        /// <summary>
        /// Do streamu zapíše řádek popisující informace o obsahu souboru daného typu
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private void _SaveFileProperties(ProcessFileInfo saveInfo, DZipFileWriter stream)
        {
            switch (saveInfo.ContentType)
            {
                case FileContentType.Data:
                case FileContentType.DataPack:
                    string now = DateTime.Now.GetDateKey().ToString();
                    string content = (this._DataContentTime.HasValue ? this._DataContentTime.Value.GetDateKey().ToString() : "NULL");
                    stream.WriteLine($"{HeaderDataProperties};{now};{content}");
                    break;
            }
        }
        /// <summary>
        /// Data pro ukládání souboru
        /// </summary>
        public class SaveDataArgs
        {
            /// <summary>
            /// Konstruktor. Nastaví <see cref="Packed"/> = <see cref="SaveFormat.Pack"/>; a <see cref="BackupOldFileMode"/> = <see cref="BackupMode.OneForDay"/>.
            /// </summary>
            public SaveDataArgs()
            {
                Packed = SaveFormat.Pack;
                BackupOldFileMode = BackupMode.OneForDay;
            }
            /// <summary>
            /// Druh ukládaných dat
            /// </summary>
            public FileContentType ContentType { get; set; }
            /// <summary>
            /// Explicitní jméno souboru, default = null = odvodí se podle <see cref="ContentType"/>
            /// </summary>
            public string FileName { get; set; }
            /// <summary>
            /// Jak zálohovat existující soubor (přejmenovat starší před tím, než se uloží nový)
            /// </summary>
            public BackupMode BackupOldFileMode { get; set; }
            /// <summary>
            /// Režim formátu a komprimace
            /// </summary>
            public SaveFormat Packed { get; set; }
            public Action<ProgressArgs> Progress { get; set; }
        }
        #endregion
        #region Fronta procesů (jednotlivé akce: Load, Update, Save...) - jejich zpracování
        /// <summary>
        /// Zpracuje jednotlivé akce zadané ve frontě akcí.
        /// Tato metoda zajistí spuštění fronty asynchronně = na pozadí.
        /// </summary>
        /// <param name="processQueue"></param>
        private void _DoProcessAsync(ProcessQueueInfo processQueue)
        {
            ThreadManager.AddAction(() => _DoProcess(processQueue));
        }
        /// <summary>
        /// Zpracuje jednotlivé akce zadané ve frontě akcí.
        /// Fronta je dynamická, a každé provedení jedné akce ji může změnit.
        /// </summary>
        /// <param name="processQueue"></param>
        private void _DoProcess(ProcessQueueInfo processQueue)
        {
            if (processQueue == null) return;

            while (!processQueue.IsEmpty)
            {
                bool enableNextStep = _DoProcessOneAction(processQueue);
                if (!enableNextStep) break;
            }

            if (processQueue.HasMessages)
                App.ShowWarning(text: processQueue.Messages);
        }
        /// <summary>
        /// Z fronty akcí v procesu získá další akci, první na řadě, a zajistí její provedení.
        /// </summary>
        /// <param name="processQueue"></param>
        /// <returns></returns>
        private bool _DoProcessOneAction(ProcessQueueInfo processQueue)
        {
            var action = processQueue.GetAction();
            if (action == null) return true;
            bool enableNextStep = true;
            try
            {
                switch (action.ActionType)
                {
                    case ProcessActionType.ClearAll:
                        this._ClearAll(processQueue);
                        break;
                    case ProcessActionType.LoadInitial:
                        _LoadInitial(processQueue);
                        break;
                    case ProcessActionType.LoadFile:
                        this._LoadFile(processQueue);
                        break;
                    case ProcessActionType.LoadContent:
                        this._LoadContent(processQueue);
                        break;
                    case ProcessActionType.DownloadUpdateAll:
                        this._WebDownloadUpdate(processQueue, true);
                        break;
                    case ProcessActionType.DownloadUpdateNew:
                        this._WebDownloadUpdate(processQueue, false);
                        break;
                    case ProcessActionType.DownloadUrl:
                        enableNextStep = !this._WebDownloadUrl(processQueue);  // Tato akce pravděpodobně vrátí false => nepokračovat v dalších akcích (to pro přechod do jiného threadu: download probíhá asynchronně). V tuto chvíli nesmíme pokračovat ve zpracování dalších akcí fronty, frontu rozeběhne až event DownloadCompleted. Vracíme false.
                        break;
                    case ProcessActionType.DownloadDone:
                        this._WebDownloadDone(processQueue);
                        break;
                    case ProcessActionType.SaveFile:
                        this._SaveDataFile(processQueue);
                        break;
                }
            }
            catch (Exception exc)
            {
                processQueue.AddMessage($"Při zpracování akce {action.ActionType} došlo k chybě {exc}.");
            }
            return enableNextStep;
        }
        #endregion
        #region Privátní podpora: adresáře, jména standardních souborů, hlavičkové konstanty v souborech, konverzní metody
        /// <summary>
        /// Zajistí zobrazení progresu
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="stream"></param>
        /// <param name="processInfo"></param>
        private void _CallProgress(ProcessFileInfo processInfo, bool force = false, long? position = null, bool isDone = false)
        {
            if (processInfo is null || processInfo.ProgressAction is null) return;

            if (isDone)
            {
                processInfo.Position = processInfo.Length;
                processInfo.ProcessState = _GetFinalStateAfter(processInfo.ProcessState);
                processInfo.DoneTime = DateTime.Now;
            }

            DateTime now = DateTime.Now;
            if (!force && (((TimeSpan)(now - processInfo.LastProgressTime)).TotalMilliseconds < 80d)) return;

            if (!isDone && position.HasValue)
                processInfo.Position = position.Value;

            ProgressArgs args = new ProgressArgs(processInfo, isDone);
            processInfo.ProgressAction(args);
            processInfo.LastProgressTime = now;
        }
        /// <summary>
        /// Vrátí finální stav procesu po dokončení operace daného typu
        /// </summary>
        /// <param name="processState"></param>
        /// <returns></returns>
        private ProcessFileState _GetFinalStateAfter(ProcessFileState processState)
        {
            switch (processState)
            {
                case ProcessFileState.Loading:
                case ProcessFileState.Loaded: return ProcessFileState.Loaded;
                case ProcessFileState.Saving:
                case ProcessFileState.Saved: return ProcessFileState.Saved;
                case ProcessFileState.WebDownloading:
                case ProcessFileState.WebDownloaded:  return ProcessFileState.WebDownloaded;
            }
            return processState;
        }
        /// <summary>
        /// Adresář pro standardní načítání a ukládání pracovních dat
        /// </summary>
        protected static string PathData { get { return IO.Path.Combine(App.ConfigPath, "Data"); } }
        /// <summary>
        /// Adresář pro zálohování dat
        /// </summary>
        protected static string PathBackup { get { return IO.Path.Combine(App.ConfigPath, "Backup"); } }
        protected const string StandardStructureFileName = "Structure.db";
        protected const string StandardDataFileName = "Data.db";
        protected const string StandardDataPackFileName = "DataPack.db";
        protected const string StandardWebObce1FileName = "WebObce1.csv";
        protected const string StandardWebObce2FileName = "WebObce2.csv";
        protected const string StandardWebPocetFileName = "WebPocet.csv";
        protected const string StandardBackupdownloadFileName = "LastDownload" + StandardPackExtension;
        protected const string StandardPackExtension = ".pack";


        protected const string StructureHeaderExpected = "H;Structure;BestInCovid;V1;";
        protected const string DataHeaderExpected = "H;Data;BestInCovid;V1;";
        protected const string DataPackHeaderExpected = "H;BestInCovid;V1;";
        protected const string Covid1HeaderExpected = "den;datum;kraj_kod;kraj_nazev;okres_kod;okres_nazev;orp_kod;orp_nazev;opou_kod;opou_nazev;obec_kod;obec_nazev;nove_pripady;aktualne_nemocnych";
        protected const int Covid1ItemCountExpected = 14;
        protected const string Covid2HeaderExpected = "den,datum,kraj_nuts_kod,kraj_nazev,okres_lau_kod,okres_nazev,orp_kod,orp_nazev,obec_kod,obec_nazev,nove_pripady,aktivni_pripady";
        protected const int Covid2ItemCountExpected = 12;
        protected const string Covid3HeaderExpected = "den,datum,kraj_nuts_kod,kraj_nazev,okres_lau_kod,okres_nazev,orp_kod,orp_nazev,obec_kod,obec_nazev,nove_pripady,aktivni_pripady,nove_pripady_65,nove_pripady_7_dni,nove_pripady_14_dni";
        protected const int Covid3ItemCountExpected = 15;
        protected const string PocetHeaderExpected = ";kraj;mesto;kod_obce;nazev_obce;muzi;muzi15;zeny;zeny15;celkem;celkem15";
        protected const int PocetItemCountExpected = 11;
        protected const string UmrtiHeaderExpected = "datum,vek,pohlavi,kraj_nuts_kod,okres_lau_kod";
        protected const int UmrtiItemCountExpected = 5;
       
        protected const string VoidEntityCode = "0";

        protected const string HeaderDataProperties = "C";
        protected const string HeaderWorld = "W";
        protected const string HeaderZeme = "Z";
        protected const string HeaderKraj = "K";
        protected const string HeaderOkres = "O";
        protected const string HeaderMesto = "M";
        protected const string HeaderObec = "B";
        protected const string HeaderVesnice = "V";
        protected const string HeaderPocet = "P";
        protected const string HeaderInfo = "I";

        protected static DateTime GetDate(string text)
        {   // Formát: "2021-01-12" nebo "20210112", vždy RRRR MM DD; anebo číslo 1 až 9999 = počet dnů od 1.1.2019 (9999 dní = 27 roků)
            if (text.Contains(' ')) text = text.Replace(" ", "");
            if (text.Contains('-')) text = text.Replace("-", "");
            int length = text.Length;
            if (length == 8)
            {
                int y = GetInt32(text.Substring(0, 4));
                int m = GetInt32(text.Substring(4, 2));
                int d = GetInt32(text.Substring(6, 2));
                if (y >= 2020 && m >= 1 && m <= 12 && d >= 1 && d <= 31 && d <= DateTime.DaysInMonth(y, m)) return new DateTime(y, m, d);
            }
            if (length <= 4)
            {
                int days = GetInt32(text);
                return new DateTime(2019, 1, 1).AddDays(days);
            }
            return DateTime.MinValue;
        }
        protected static int GetInt32(string text)
        {
            if (text.Contains(SpaceChar)) text = text.Replace(SpaceChar.ToString(), "");
            if (text.Contains(FixSpaceChar)) text = text.Replace(FixSpaceChar.ToString(), "");
            if (Int32.TryParse(text, out int value)) return value;
            return 0;
        }
        protected const char SpaceChar = (char)32;
        protected const char FixSpaceChar = (char)160;


        #endregion
        #region Komprimace a dekomprimace streamu

        public class DZipFileReader : IDisposable
        {
            public DZipFileReader(string file, CompressMode compressMode)
            {
                _IsCompress = _DetectComprimed(file, compressMode);
                _IsMemory = false;
                _Length = (new IO.FileInfo(file)).Length;
                if (_IsCompress)
                {
                    _FileStream = IO.File.OpenRead(file);
                    _ZipStream = new IO.Compression.GZipStream(_FileStream, IO.Compression.CompressionMode.Decompress);
                    _StreamReader = new IO.StreamReader(_ZipStream);
                }
                else
                {
                    _StreamReader = new IO.StreamReader(file);
                }
            }
            public DZipFileReader(byte[] buffer)
            {
                _IsCompress = false;
                _IsMemory = true;
                _Length = buffer.LongLength;
                _MemoryStream = new IO.MemoryStream(buffer);
                _StreamReader = new IO.StreamReader(_MemoryStream, Encoding.UTF8);
            }
            protected bool _DetectComprimed(string file, CompressMode compressMode)
            {
                switch (compressMode)
                {
                    case CompressMode.None: return false;
                    case CompressMode.ByName:
                        string extension = IO.Path.GetExtension(file).ToLower().Trim();
                        return (extension == StandardPackExtension && extension == ".pack" || extension == ".zip");
                    case CompressMode.ByContent:
                        byte[] header = new byte[16];
                        int length = 0;
                        using (var inp = IO.File.OpenRead(file))
                        {
                            length = inp.Read(header, 0, 16);
                            inp.Close();
                        }
                        return (length > 3 && header[0] == 0x1F && header[1] == 0x8B && header[2] == 0x08 && header[3] == 0x00);
                    case CompressMode.Compress: return true;
                }
                return false;
            }
            CompressMode _CompressMode;
            bool _IsCompress;
            bool _IsMemory;
            long _Length;
            IO.FileStream _FileStream;
            IO.Compression.GZipStream _ZipStream;
            IO.MemoryStream _MemoryStream;
            IO.StreamReader _StreamReader;

            public bool IsEnd { get { return _StreamReader.EndOfStream; } }
            public long Length { get { return _Length; } }
            public long? Position
            {
                get
                {
                    if (_IsCompress) return _FileStream.Position;
                    else if (_IsMemory) return _MemoryStream.Position;
                    else return _StreamReader.BaseStream?.Position;
                }
            }
            public string ReadLine() { return _StreamReader.ReadLine(); }
            public void Close()
            {
                if (_IsCompress)
                {
                    _StreamReader.Close();
                    _ZipStream.Close();
                    _FileStream.Close();
                }
                else if (_IsMemory)
                {
                    _StreamReader.Close();
                    _MemoryStream.Close();
                }
                else
                {
                    _StreamReader.Close();
                }
            }
            public void Dispose()
            {
                if (_IsCompress)
                {
                    _StreamReader.Dispose();
                    _ZipStream.Dispose();
                    _FileStream.Dispose();
                }
                else if (_IsMemory)
                {
                    _StreamReader.Dispose();
                    _MemoryStream.Dispose();
                }
                else
                {
                    _StreamReader.Dispose();
                }
            }
        }

        public class DZipFileWriter : IDisposable
        {
            public DZipFileWriter(string file, CompressMode compressMode, bool isBinary)
            {
                _IsCompress = _DetectComprimed(file, compressMode);
                _IsBinary = isBinary;
                _StreamMode = StreamMode.None;
                if (_IsCompress)
                {
                    if (isBinary)
                    {
                        _StreamMode = StreamMode.BinaryCompress;
                        _FileStream = IO.File.OpenWrite(file);
                        _ZipStream = new IO.Compression.GZipStream(_FileStream, IO.Compression.CompressionMode.Compress);
                        _BinaryWriter = new IO.BinaryWriter(_ZipStream);
                    }
                    else
                    {
                        _StreamMode = StreamMode.TextCompress;
                        _FileStream = IO.File.OpenWrite(file);
                        _ZipStream = new IO.Compression.GZipStream(_FileStream, IO.Compression.CompressionMode.Compress);
                        _StreamWriter = new IO.StreamWriter(_ZipStream);
                    }
                }
                else
                {
                    if (isBinary)
                    {
                        _StreamMode = StreamMode.BinaryPlain;
                        _FileStream = IO.File.OpenWrite(file);
                        _BinaryWriter = new IO.BinaryWriter(_FileStream);
                    }
                    else
                    {
                        _StreamMode = StreamMode.TextPlain;
                        _StreamWriter = new IO.StreamWriter(file);
                    }
                }
            }
            protected enum StreamMode { None, TextPlain, TextCompress, BinaryPlain, BinaryCompress }
            protected bool _DetectComprimed(string file, CompressMode compressMode)
            {
                switch (compressMode)
                {
                    case CompressMode.None: return false;
                    case CompressMode.ByName:
                        string extension = IO.Path.GetExtension(file).ToLower().Trim();
                        return (extension == StandardPackExtension && extension == ".pack" || extension == ".zip");
                    case CompressMode.ByContent:
                        throw new ArgumentException("Ve třídě DZipFileWriter nelze použít compressMode = CompressMode.ByContent !");
                    case CompressMode.Compress: return true;
                }
                return false;
            }
            bool _IsBinary;
            bool _IsCompress;
            StreamMode _StreamMode;
            IO.FileStream _FileStream;
            IO.Compression.GZipStream _ZipStream;
            IO.MemoryStream _MemoryStream;
            IO.StreamWriter _StreamWriter;
            IO.BinaryWriter _BinaryWriter;
            /// <summary>
            /// Do tohoto streamu vepíše daný text jako nový řádek (text + EOL)
            /// </summary>
            /// <param name="line"></param>
            public void WriteLine(string line)
            {
                if (_IsBinary)
                    throw new InvalidOperationException("DZipFileWriter je vytvořen jako Binární, a je požadován textový zápis metodou WriteLine(string).");
                _StreamWriter.WriteLine(line);
            }
            /// <summary>
            /// Do tohoto streamu vepíše daný obsah dat
            /// </summary>
            /// <param name="content"></param>
            public void WriteBuffer(byte[] content)
            {
                if (!_IsBinary)
                    throw new InvalidOperationException("DZipFileWriter je vytvořen jako Textový, a je požadován binární zápis metodou WriteBuffer(byte[]).");

                _BinaryWriter.Write(content);
            }
            public void Close()
            {
                switch (_StreamMode)
                {
                    case StreamMode.BinaryCompress:
                        _BinaryWriter.Close();
                        _ZipStream.Close();
                        _FileStream.Close();
                        break;
                    case StreamMode.TextCompress:
                        _StreamWriter.Close();
                        _ZipStream.Close();
                        _FileStream.Close();
                        break;
                    case StreamMode.BinaryPlain:
                        _BinaryWriter.Close();
                        _FileStream.Close();
                        break;
                    case StreamMode.TextPlain:
                        _StreamWriter.Close();
                        break;
                }
            }
            public void Dispose()
            {
                this.Close();
                switch (_StreamMode)
                {
                    case StreamMode.BinaryCompress:
                        _BinaryWriter.Dispose();
                        _ZipStream.Dispose();
                        _FileStream.Dispose();
                        break;
                    case StreamMode.TextCompress:
                        _StreamWriter.Dispose();
                        _ZipStream.Dispose();
                        _FileStream.Dispose();
                        break;
                    case StreamMode.BinaryPlain:
                        _BinaryWriter.Dispose();
                        _FileStream.Dispose();
                        break;
                    case StreamMode.TextPlain:
                        _StreamWriter.Dispose();
                        break;
                }
                _StreamWriter = null;
                _ZipStream = null;
                _FileStream = null;
            }
        }
        /// <summary>
        /// Režim komprimace
        /// </summary>
        public enum CompressMode
        {
            None,
            ByName,
            ByContent,
            Compress
        }
        /// <summary>
        /// Metoda vrátí daný string KOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený do Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-5% původní délky stringu.
        /// </summary>
        /// <param name="source">Vstupní string</param>
        /// <param name="splitToRows">Rozdělit výstupní komprimát na řádky (použije se <see cref="Base64FormattingOptions.InsertLineBreaks"/>).</param>
        /// <returns></returns>
        public static string Compress(string source, bool splitToRows = false)
        {
            if (source == null || source.Length == 0) return source;

            string target = null;
            byte[] inpBuffer = System.Text.Encoding.UTF8.GetBytes(source);
            using (IO.MemoryStream inpStream = new IO.MemoryStream(inpBuffer))
            using (IO.MemoryStream outStream = new IO.MemoryStream())
            {
                using (IO.Compression.GZipStream zipStream = new IO.Compression.GZipStream(outStream, IO.Compression.CompressionMode.Compress))
                {
                    inpStream.CopyTo(zipStream);
                }   // Obsah streamu outStream je použitelný až po Dispose streamu GZipStream !
                outStream.Flush();
                byte[] outBuffer = outStream.ToArray();
                Base64FormattingOptions options = (splitToRows ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None);
                target = System.Convert.ToBase64String(outBuffer, options);
            }
            return target;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="IO.Compression.GZipStream"/>, převedený z Base64 stringu.
        /// Pokud někde dojde k chybě, vrátí null ale ne chybu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string TryDecompress(string source)
        {
            try
            {
                return Decompress(source);
            }
            catch { }
            return null;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="IO.Compression.GZipStream"/>, převedený z Base64 stringu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Decompress(string source)
        {
            if (source == null || source.Length == 0) return source;

            string target = null;
            byte[] inpBuffer = System.Convert.FromBase64String(source);
            using (IO.MemoryStream inpStream = new IO.MemoryStream(inpBuffer))
            using (IO.MemoryStream outStream = new IO.MemoryStream())
            {
                using (System.IO.Compression.GZipStream zipStream = new System.IO.Compression.GZipStream(inpStream, System.IO.Compression.CompressionMode.Decompress))
                {
                    zipStream.CopyTo(outStream);
                }   // Obsah streamu outStream je použitelný až po Dispose streamu GZipStream !
                outStream.Flush();
                byte[] outBuffer = outStream.ToArray();
                target = System.Text.Encoding.UTF8.GetString(outBuffer);
            }
            return target;
        }
        #endregion
        #endregion
        #region Získání exaktních dat za určitou entitu
        /// <summary>
        /// Získá a vrátí exaktní data za danou entitu
        /// </summary>
        /// <param name="entity">Pro kterou entitu máme hledat data. Entitu lze snadno najít pomocí <see cref="GetEntity(string)"./></param>
        /// <param name="valueType">Jaký datový typ získat pro analýzy</param>
        /// <param name="dataTypeInfo">Info o datovém typu, může být null</param>
        /// <param name="begin">Počátek dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="end">Konec dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="pocetOd">Filtr na počet obyvatel na nejnižší úrovni, od</param>
        /// <param name="pocetDo">Filtr na počet obyvatel na nejnižší úrovni, do</param>
        /// <returns></returns>
        public ResultSetInfo GetResultSimple(EntityInfo entity, DataValueType valueType, DataValueTypeInfo dataTypeInfo = null, DateTime? begin = null, DateTime? end = null, int? pocetOd = null, int? pocetDo = null)
        {
            return _GetResultSimple(entity, valueType, dataTypeInfo, begin, end, pocetOd, pocetDo);
        }
        /// <summary>
        /// Získá a vrátí exaktní data za danou entitu
        /// </summary>
        /// <param name="entity">Pro kterou entitu máme hledat data. Entitu lze snadno najít pomocí <see cref="GetEntity(string)"./></param>
        /// <param name="valueType">Jaký datový typ získat pro analýzy</param>
        /// <param name="dataTypeInfo">Info o datovém typu, může být null</param>
        /// <param name="begin">Počátek dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="end">Konec dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="pocetOd">Filtr na počet obyvatel na nejnižší úrovni, od</param>
        /// <param name="pocetDo">Filtr na počet obyvatel na nejnižší úrovni, do</param>
        /// <returns></returns>
        private ResultSetInfo _GetResultSimple(EntityInfo entity, DataValueType valueType, DataValueTypeInfo dataTypeInfo = null, DateTime? begin = null, DateTime? end = null, int? pocetOd = null, int? pocetDo = null)
        {
            if (dataTypeInfo == null) dataTypeInfo = DataValueTypeInfo.CreateFor(valueType);
            _PrepareSourceTimeRange(dataTypeInfo, begin, end, out DateTime? sourceBegin, out DateTime? sourceEnd);
            ResultSetInfo resultSet = null;
            lock (this.InterLock)
            {
                if (entity != null)
                {
                    SearchInfoArgs args = new SearchInfoArgs(entity, valueType, dataTypeInfo, begin, end, sourceBegin, sourceEnd, pocetOd, pocetDo);
                    entity.SearchInfo(args);
                    ProcessResultValue(args, begin, end);
                    resultSet = args.ResultSet;
                }
            }
            return resultSet;
        }
        /// <summary>
        /// Určí časové rozmezí pro načítání vstupních dat z databáze na základě zadaného časového rozmezí grafu (=uživatelův výběr) plus/mínus offsety potřebné pro agregační funkci.
        /// Například pro typ hodnoty 
        /// </summary>
        /// <param name="dataTypeInfo"></param>
        /// <param name="begin">Vstup: uživatelem požadovaný počátek (včetně) = tato data chce vidět</param>
        /// <param name="end">Vstup: uživatelem požadovaný konec (mimo) = tato data chce vidět</param>
        /// <param name="sourceBegin">Výstup: potřebný počátek načítaných dat (včetně) = tato data je třeba načíst, abychom mohli spočítat podklady k uživatelskému počátku</param>
        /// <param name="sourceEnd">Výstup: potřebný konec načítaných dat (mimo) = tato data je třeba načíst, abychom mohli spočítat podklady k uživatelskému počátku</param>
        private void _PrepareSourceTimeRange(DataValueTypeInfo dataTypeInfo, DateTime? begin, DateTime? end, out DateTime? sourceBegin, out DateTime? sourceEnd)
        {
            sourceBegin = (begin.HasValue ? (dataTypeInfo.DateOffsetBefore.HasValue ? (DateTime?)begin.Value.AddDays(dataTypeInfo.DateOffsetBefore.Value) : begin) : (DateTime?)null);
            sourceEnd = (end.HasValue ? (dataTypeInfo.DateOffsetAfter.HasValue ? (DateTime?)end.Value.AddDays(dataTypeInfo.DateOffsetAfter.Value) : end) : (DateTime?)null);
        }
        /// <summary>
        /// Zpracuje výchozí data načtená z databáze <see cref="SearchInfoArgs.Results"/> 
        /// pomocí zadané agregační funkce <see cref="SearchInfoArgs.ValueType"/> do výstupního pole 
        /// <see cref="ResultSetInfo.Results"/>
        /// </summary>
        /// <param name="args"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        private void ProcessResultValue(SearchInfoArgs args, DateTime? begin = null, DateTime? end = null)
        {
            ProcessResultValueDirect(args);

            // enum DataValueType obsahuje bity, které předepisují jednotlivé agregátní funkce. Nutno je provést ve správném pořadí 
            //  - tak, aby následující výpočet měl připravená správná data z předešlého výpočtu:

            if (args.ValueType.HasFlag(DataValueType.AggrPrepareLast7DayAverage)) ProcessResultAggrLast7DayAverage(args);        // Přípravný agregát: O tohle se může opřít Coefficient (=o průměr z posledních dnů)

            if (args.ValueType.HasFlag(DataValueType.AggrCoefficient5Days)) ProcessResultAggrCoefficient5Days(args);             // Koeficient se smí počítat jen 5Days nebo 7Days (nebo žádný), ale nikdy ne oba
            else if (args.ValueType.HasFlag(DataValueType.AggrCoefficient7Days)) ProcessResultAggrCoefficient7Days(args);
 
            if (args.ValueType.HasFlag(DataValueType.AggrLast7DaySum)) ProcessResultAggrLast7DaySum(args);                       // Součet posledních dnů se používá jako výchozí hodnota pro jiné typy než Coefficient

            if (args.ValueType.HasFlag(DataValueType.AggrStd7DayAverage)) ProcessResultAggrStd7DayAverage(args);                 // Průměr 7 dnů standardní dle konfigurace
            else if (args.ValueType.HasFlag(DataValueType.AggrLast7DayAverage)) ProcessResultAggrLast7DayAverage(args);          // Průměr průběžných 7 dnů (klouzavý průměr) je typická závěrečná akce na vyhlazení křivek
            else if (args.ValueType.HasFlag(DataValueType.AggrFlow7DayAverage)) ProcessResultAggrFlow7DayAverage(args);          // Průměr průběžných 7 dnů (klouzavý průměr) je typická závěrečná akce na vyhlazení křivek

            if (args.ValueType.HasFlag(DataValueType.AggrRelativeTo100K)) ProcessResultAggrRelativeTo100K(args);                 // Přepočet na 100T nebo 1M může být před i po klouzavém průměru
            else if (args.ValueType.HasFlag(DataValueType.AggrRelativeTo1M)) ProcessResultAggrRelativeTo1M(args);                //  ale nikdy nesmí být ba přepočty najednou

            if (args.ValueType.HasFlag(DataValueType.Round0D)) ProcessResultRound0D(args);                                       // Některé ze zaokrouhlení je poslední
            else if (args.ValueType.HasFlag(DataValueType.Round1D)) ProcessResultRound1D(args);
            else if (args.ValueType.HasFlag(DataValueType.Round2D)) ProcessResultRound2D(args);

            ProcessResultValueByTimeRange(args, begin, end);
        }
        /// <summary>
        /// Opíše RawValue do Value.
        /// <para/>
        /// Jde vždy o první proces. Všechny další procesy pracují s hodnotou Value (vstup i výstup) s případnou pomocí TempValue (mezivýsledky u časových řad).
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultValueDirect(SearchInfoArgs args)
        {
            args.Results.ForEachExec(r => r.Value = r.RawValue);
        }
        /// <summary>
        /// Průměr za standardních 7 dní = minulé nebo plovoucí, dle nastavení
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrStd7DayAverage(SearchInfoArgs args)
        {
            bool useLastDaysAvg = true;
            if (useLastDaysAvg)
                ProcessResultAggrLast7DayAverage(args);
            else
                ProcessResultAggrFlow7DayAverage(args);
        }
        /// <summary>
        /// Průměr za posledních 7 dní = -6 až 0 dny
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrLast7DayAverage(SearchInfoArgs args)
        {
            ProcessResultAggrAnyAverage(args, -6, 7);
        }
        /// <summary>
        /// Plovoucí průměr za 7 dní = -3 až +3 dny
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrFlow7DayAverage(SearchInfoArgs args)
        {
            ProcessResultAggrAnyAverage(args, -3, 7);
        }
        /// <summary>
        /// Průměr počínaje daným offsetem ke dnešku v daném počtu dní
        /// </summary>
        /// <param name="args"></param>
        /// <param name="daysBefore">Záporné číslo = před dneškem, 0 = dnes, kladné číslo = po dnešku</param>
        /// <param name="daysCount">Počet dní započítaných</param>
        private void ProcessResultAggrAnyAverage(SearchInfoArgs args, int daysBefore, int daysCount)
        {
            var data = args.ResultSet.WorkingDict;
            int[] keys = data.Keys.ToArray();
            foreach (int key in keys)
            {
                var result = data[key];
                DateTime date = result.Date.AddDays(daysBefore);
                DateTime end = date.AddDays(daysCount);
                int count = 0;
                decimal sum = 0m;
                while (date < end)
                {   // V tomto cyklu najdu přinejmenším jeden platný záznam = a to "result" = ten "cílový":
                    int avgKey = date.GetDateKey();
                    if (data.TryGetValue(avgKey, out var source))
                    {
                        count++;
                        sum += source.Value;
                    }
                    date = date.AddDays(1d);
                }
                result.TempValue = sum / (decimal)count;
            }
            // Na závěr vložím TempValue do Value:
            args.Results.ForEachExec(r => r.Value = r.TempValue);
        }
        /// <summary>
        /// Součet za posledních 7 dní, bez počítání průměru
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrLast7DaySum(SearchInfoArgs args)
        {
            ProcessResultAggrLastAnyDaySum(args, -6, 7);
        }
        /// <summary>
        /// Součet počínaje daným offsetem ke dnešku v daném počtu dní, bez počítání průměru
        /// </summary>
        /// <param name="args"></param>
        /// <param name="daysBefore">Záporné číslo = před dneškem, 0 = dnes, kladné číslo = po dnešku</param>
        /// <param name="daysCount">Počet dní započítaných</param>
        private void ProcessResultAggrLastAnyDaySum(SearchInfoArgs args, int daysBefore, int daysCount)
        {
            DateTime currentDate = DateTime.Now.Date;
            var data = args.ResultDict;
            int[] keys = data.Keys.ToArray();
            foreach (int key in keys)
            {
                var result = data[key];
                result.TempValue = 0m;
                if (IsValidDate(result.Date, currentDate))
                {
                    DateTime date = result.Date.AddDays(daysBefore);     // První datum pro sumu do dne 18.1.2021 (pondělí) je minulé úterý 12.1.2021
                    DateTime end = date.AddDays(daysCount);              // End je datum, které se už počítat nebude = 12.1. + 7 = 19.1.2021
                    int count = 0;
                    decimal sum = 0m;
                    while (date < end)
                    {   // V tomto cyklu najdu přinejmenším jeden platný záznam = a to "result" = ten "cílový":
                        int avgKey = date.GetDateKey();
                        if (data.TryGetValue(avgKey, out var source))
                        {
                            count++;
                            sum += source.Value;
                        }
                        date = date.AddDays(1d);
                    }
                    result.TempValue = sum;
                }
            }
            // Na závěr vložím TempValue do Value:
            args.Results.ForEachExec(r => r.Value = r.TempValue);
        }
        /// <summary>
        /// Vypočítá poměr hodnoty Value ku počtu obyvatel, na 100 000 (výsledná hodnota = počet případů na 100 000 obyvatel)
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrRelativeTo100K(SearchInfoArgs args)
        {
            ProcessResultAggrRelativeToAny(args, 100000);
        }
        /// <summary>
        /// Vypočítá poměr hodnoty Value ku počtu obyvatel, na 100 000 (výsledná hodnota = počet případů na 100 000 obyvatel)
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrRelativeTo1M(SearchInfoArgs args)
        {
            ProcessResultAggrRelativeToAny(args, 1000000);
        }
        /// <summary>
        /// Vypočítá poměr hodnoty Value ku počtu obyvatel, na daný počet obyvatel (výsledná hodnota = počet případů na dané číslo)
        /// </summary>
        /// <param name="args"></param>
        /// <param name="relativeBase">Základna pro počet obyvatel (100 000 nebo 1 000 000)</param>
        private void ProcessResultAggrRelativeToAny(SearchInfoArgs args, int relativeBase)
        {
            decimal coefficient = (args.PocetObyvatel > 0 ? ((decimal)relativeBase / (decimal)args.PocetObyvatel) : 0m);
            args.Results.ForEachExec(r => r.Value = coefficient * r.Value);
        }
        /// <summary>
        /// Vypočítá hodnotu R0 jako poměr hodnoty Value proti Value [mínus 5 dní] a výsledky na závěr vloží do Value
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrCoefficient5Days(SearchInfoArgs args)
        {
            ProcessResultAggrCoefficientAny(args, -5);
        }
        /// <summary>
        /// Vypočítá hodnotu R0 jako poměr hodnoty Value proti Value [mínus 5 dní] a výsledky na závěr vloží do Value
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultAggrCoefficient7Days(SearchInfoArgs args)
        {
            ProcessResultAggrCoefficientAny(args, -7);
        }
        /// <summary>
        /// Vypočítá hodnotu R0 jako poměr hodnoty Value proti Value [mínus 5 dní] a výsledky na závěr vloží do Value
        /// </summary>
        /// <param name="args"></param>
        /// <param name="daysBefore">Záporné číslo = před dneškem, 0 = dnes, kladné číslo = po dnešku</param>
        private void ProcessResultAggrCoefficientAny(SearchInfoArgs args, int daysBefore)
        {
            var data = args.ResultDict;
            int[] keys = data.Keys.ToArray();
            decimal lastRZero = 0m;
            foreach (int key in keys)
            {   // Nejprve vypočtu hodnotu RZero a uložím ji do TempValue, protože hodnoty v Value průběžně potřebuji pro následující výpočty
                //  (mohl bych jít datumově od konce a rovnou hodnotu Value přepisovat, ale pak bych neměl šanci řešit chybějící dny = pomocí lastRZero):
                var result = data[key];
                DateTime date = result.Date.AddDays(daysBefore);
                int sourceKey = date.GetDateKey();
                if (data.TryGetValue(sourceKey, out var source) && source.Value > 0m)
                    lastRZero = result.Value / source.Value;
                result.TempValue = lastRZero;
            }
            // Na závěr vložím TempValue do Value:
            args.Results.ForEachExec(r => r.Value = r.TempValue);
        }
        /// <summary>
        /// Zaokrouhlí hodnotu na daný počet desetinných míst
        /// </summary>
        /// <param name="args"></param>
        /// <param name="decimals"></param>
        private void ProcessResultRound0D(SearchInfoArgs args, int decimals = 0)
        {
            ProcessResultRoundAny(args, 0);
        }
        /// <summary>
        /// Zaokrouhlí hodnotu na daný počet desetinných míst
        /// </summary>
        /// <param name="args"></param>
        /// <param name="decimals"></param>
        private void ProcessResultRound1D(SearchInfoArgs args, int decimals = 0)
        {
            ProcessResultRoundAny(args, 1);
        }
        /// <summary>
        /// Zaokrouhlí hodnotu na daný počet desetinných míst
        /// </summary>
        /// <param name="args"></param>
        /// <param name="decimals"></param>
        private void ProcessResultRound2D(SearchInfoArgs args, int decimals = 0)
        {
            ProcessResultRoundAny(args, 2);
        }
        /// <summary>
        /// Zaokrouhlí hodnotu na daný počet desetinných míst
        /// </summary>
        /// <param name="args"></param>
        /// <param name="decimals"></param>
        private void ProcessResultRoundAny(SearchInfoArgs args, int decimals)
        {
            args.Results.ForEachExec(r => r.Value = Math.Round(r.Value, decimals));
        }
        /// <summary>
        /// Z dodané kolekce hodnot <see cref="ResultSetInfo.WorkingDict"/> vybere jen ty, které vyhovují danému časovému rozmezí, 
        /// setřídí dle data a uloží jako pole do <see cref="ResultSetInfo.Results"/>.
        /// <para/>
        /// Tato metoda se vždy volá jako poslední v řadě procesu, protože tato metoda jediná plní pole <see cref="ResultSetInfo.Results"/> a aplikuje výstupní časový filtr.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private void ProcessResultValueByTimeRange(SearchInfoArgs args, DateTime? begin = null, DateTime? end = null)
        {
            // Do výsledku přebírám pouze záznamy, jejichž datum je menší nebo rovno _LastValidDataDate (poslední datum, za které máme nenulová data),
            //  a současně je menší než dnešní den (za dnešní den nikdy nejsou data směrodatná, i kdyby byly nenulové):
            //  A přitom akceptujeme datum 'end' z parametru, pokud není větší než dnešní (uživatel může chtít vidět třeba jen data za první kvartál):
            DateTime? last = _LastValidDataDate;
            DateTime now = DateTime.Now.Date;
            if (!end.HasValue || end.Value.Date >= now.Date)
                end = now;

            List<ResultInfo> resultList;
            bool hasBegin = begin.HasValue;
            bool hasEnd = end.HasValue;
            resultList = args.ResultSet.WorkingDict.Values.Where(v => ComplyInfoByDate(v.Date, begin, end, last)).ToList();
            resultList.Sort((a, b) => a.Date.CompareTo(b.Date));
            args.ResultSet.Results = resultList.ToArray();
            args.ResultSet.ValueLast = (resultList.Count > 0 ? resultList[resultList.Count - 1].Value : 0m);
        }
        /// <summary>
        /// Vrátí true, pokud dané datum <paramref name="infoDate"/> vyhovuje daným mezím
        /// </summary>
        /// <param name="infoDate"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        private bool ComplyInfoByDate(DateTime infoDate, DateTime? begin, DateTime? end, DateTime? last)
        {
            if (begin.HasValue && infoDate < begin.Value) return false;
            if (end.HasValue && infoDate >= end.Value) return false;
            if (last.HasValue && infoDate > last.Value) return false;
            return true;
        }
        /// <summary>
        /// Vrátí true pokud dané datum je platné, tj. obsahuje rok 2000 - 2050
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private static bool IsValidDate(DateTime dateTime, ProcessQueueItem action)
        {
            return IsValidDate(dateTime, action.ProcessFile.CurrentDate);
        }
        /// <summary>
        /// Vrátí true pokud dané datum je platné, tj. obsahuje rok 2000 - 2050
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static bool IsValidDate(DateTime dateTime, ProcessFileInfo processFileInfo)
        {
            return IsValidDate(dateTime, processFileInfo.CurrentDate);
        }
        /// <summary>
        /// Vrátí true pokud dané datum je platné, tj. obsahuje rok 2000 - 2050
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static bool IsValidDate(DateTime dateTime, DateTime currentDate)
        {
            // Časová platnost:
            if (dateTime >= currentDate) return false;

            // Formální platnost:
            int year = dateTime.Year;
            return (year >= 2000 || year <= 2050);
        }
        #endregion
        #region Získání analytických dat za určitou entitu
        /// <summary>
        /// Získá a vrátí sadu analytických dat za danou entitu
        /// </summary>
        /// <param name="rootEntity">V jaké výchozí entitě analyzovat její Child prvky (typicky celá Česká republika = "CZ"). Lze ji snadno najít pomocí <see cref="GetEntity(string)"./></param>
        /// <param name="valueType">Jaký datový typ získat pro analýzy</param>
        /// <param name="analyseEntityLevel">Jaké entity analyzovat (například "Vyhledáme obce v Pardubickém kraji..." pak rootFullCode = Kraj Pardubice, a analyseEntityLevel = Obec)</param>
        /// <param name="highestCount">Kolik prvků vracet s nalezenou nejvyšší hodnotou</param>
        /// <param name="lowestCount">Kolik prvků vracet s nalezenou nejnižší hodnotou</param>
        /// <param name="addRootResult">Do výsledku přidat i sumární data za Root entitu (tj. vždy, bez analýzy)</param>
        /// <param name="analyseBegin">Počáteční datum pro analytické vyhodnocení hodnot = například: načteme data (pro zobrazení) za poslední 4 měsíce, ale analýzu provádíme za poslední dva týdny</param>
        /// <param name="analyseEnd">Koncové datum pro analytické vyhodnocení hodnot = například: načteme data (pro zobrazení) za poslední 4 měsíce, ale analýzu provádíme za poslední dva týdny</param>
        /// <param name="begin">Počátek dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="end">Konec dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="pocetOd">Filtr na počet obyvatel na nejnižší úrovni, od</param>
        /// <param name="pocetDo">Filtr na počet obyvatel na nejnižší úrovni, do</param>
        /// <returns></returns>
        public (ResultSetInfo[], GraphScanCountsInfo) GetResultsAnalytic(EntityInfo rootEntity, DataValueType valueType, EntityType analyseEntityLevel, 
            int highestCount, bool addRootResult, int lowestCount, DateTime analyseBegin, DateTime analyseEnd,
            DateTime? begin = null, DateTime? end = null, int? pocetOd = null, int? pocetDo = null)
        {
            return _GetResultsAnalytic(rootEntity, valueType, analyseEntityLevel,
                highestCount, addRootResult, lowestCount, analyseBegin, analyseEnd,
                begin, end, pocetOd, pocetDo);
        }
        /// <summary>
        /// Získá a vrátí sadu analytických dat za danou entitu
        /// </summary>
        /// <param name="rootEntity">V jaké výchozí entitě analyzovat její Child prvky (typicky celá Česká republika = "CZ"). Lze ji snadno najít pomocí <see cref="GetEntity(string)"./></param>
        /// <param name="valueType">Jaký datový typ získat pro analýzy</param>
        /// <param name="analyseEntityLevel">Jaké entity analyzovat (například "Vyhledáme obce v Pardubickém kraji..." pak rootFullCode = Kraj Pardubice, a analyseEntityLevel = Obec)</param>
        /// <param name="highestCount">Kolik prvků vracet s nalezenou nejvyšší hodnotou</param>
        /// <param name="lowestCount">Kolik prvků vracet s nalezenou nejnižší hodnotou</param>
        /// <param name="addRootResult">Do výsledku přidat i sumární data za Root entitu (tj. vždy, bez analýzy)</param>
        /// <param name="analyseBegin">Počáteční datum pro analytické vyhodnocení hodnot = například: načteme data (pro zobrazení) za poslední 4 měsíce, ale analýzu provádíme za poslední dva týdny</param>
        /// <param name="analyseEnd">Koncové datum pro analytické vyhodnocení hodnot = například: načteme data (pro zobrazení) za poslední 4 měsíce, ale analýzu provádíme za poslední dva týdny</param>
        /// <param name="begin">Počátek dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="end">Konec dat, která budou zobrazována (to ale není datum analýzy)</param>
        /// <param name="pocetOd">Filtr na počet obyvatel na nejnižší úrovni, od</param>
        /// <param name="pocetDo">Filtr na počet obyvatel na nejnižší úrovni, do</param>
        /// <returns></returns>
        private (ResultSetInfo[], GraphScanCountsInfo) _GetResultsAnalytic(EntityInfo rootEntity, DataValueType valueType, EntityType analyseEntityLevel,
            int highestCount, bool addRootResult, int lowestCount, DateTime analyseBegin, DateTime analyseEnd,
            DateTime? begin = null, DateTime? end = null, int? pocetOd = null, int? pocetDo = null)
        {
            if (rootEntity == null) throw new ArgumentNullException($"DatabaseInfo.GetResultsAnalytic() : chyba, zadaná rootEntity je null.");
            if (highestCount < 0 || lowestCount < 0) throw new ArgumentNullException($"DatabaseInfo.GetResultsAnalytic() : chyba, počet highestCount nebo lowestCount je záporný. Může být 0 a kladný.");

            List<ResultSetInfo> analyseResult = new List<ResultSetInfo>();
            GraphScanCountsInfo counts = new GraphScanCountsInfo();

            if (lowestCount > 20) lowestCount = 12;
            if (highestCount > 20) highestCount = 12;
            int analyseCount = lowestCount + highestCount;
            if (analyseCount == 0)
            {   // Nebudeme dělat analýzu:
                if (addRootResult)
                    analyseResult.Add(_GetResultSimple(rootEntity, valueType, null, begin, end, pocetOd, pocetDo));
                return (analyseResult.ToArray(), counts);
            }

            // Jdeme na analýzu:
            List<AnalyseInfo> analyseInfos = new List<AnalyseInfo>();
            EntityInfo[] entities = rootEntity.GetChildsOfType(analyseEntityLevel);
            foreach (EntityInfo entity in entities)
            {
                ResultSetInfo result = _GetResultSimple(entity, valueType, null, begin, end, pocetOd, pocetDo);
                counts.Add(result);                        // Nápočty všech prvků a prvků scanovaných
                AnalyseInfo analyseInfo = AnalyseInfo.CreateAnalyse(result, analyseBegin, analyseEnd);
                if (analyseInfo != null)                   // Null je vráceno tehdy, když v zadaném období (analyseBegin, analyseEnd) nebyla nalezena data.
                    analyseInfo.AddToListByCondition(analyseInfos, highestCount, lowestCount);
            }

            // Sestavíme výsledek:
            if (addRootResult)
            {
                ResultSetInfo result = _GetResultSimple(rootEntity, valueType, null, begin, end, pocetOd, pocetDo);
                counts.Add(result);                        // Nápočty všech prvků a prvků scanovaných
                analyseResult.Add(result);
            }
            analyseResult.AddRange(analyseInfos.Select(ai => ai.ResultSet));
            analyseResult.Sort(ResultSetInfo.CompareByValueLastDesc);   // nebo podle názvu obce: ResultSetInfo.CompareByEntityText

            // Dořešíme nápočty: ponecháme Load a Scan počet, ale počet Show nastavíme jen jako součet z výsledných prvků:
            counts.ShowRecordCount = (analyseResult.Count > 0 ? analyseResult.Select(r => r.ShowRecordCount).Sum() : 0);

            // Hotovo
            return (analyseResult.ToArray(), counts);
        }
        /// <summary>
        /// Třída pro provádění analýzy výsledků (hledání hodnot Min a Max v daném období) a pro střádání pole výsledků s daným počtem Lowest a Highest hodnot
        /// </summary>
        private class AnalyseInfo
        {
            /// <summary>
            /// Provede analýzu jedné řady dat (najde Min a Max v daném období)
            /// </summary>
            /// <param name="resultSet"></param>
            /// <param name="analyseBegin"></param>
            /// <param name="analyseEnd"></param>
            /// <returns></returns>
            internal static AnalyseInfo CreateAnalyse(ResultSetInfo resultSet, DateTime analyseBegin, DateTime analyseEnd)
            {
                if (resultSet.Entity.PocetObyvatel <= 0) return null;

                bool hasData = false;
                decimal valueMin = 0m;
                decimal valueMax = 0m;

                foreach (var resultInfo in resultSet.Results.Where(r => r.Date >= analyseBegin && r.Date < analyseEnd))
                {
                    decimal value = resultInfo.Value;
                    if (!hasData)
                    {
                        valueMin = value;
                        valueMax = value;
                        hasData = true;
                    }
                    else
                    {
                        if (value < valueMin) valueMin = value;
                        if (value > valueMax) valueMax = value;
                    }
                }
                if (!hasData) return null;

                return new AnalyseInfo(resultSet, valueMin, valueMax);
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="resultSet"></param>
            /// <param name="valueMin"></param>
            /// <param name="valueMax"></param>
            private AnalyseInfo(ResultSetInfo resultSet, decimal valueMin, decimal valueMax)
            {
                this.ResultSet = resultSet;
                this.ValueMin = valueMin;
                this.ValueMax = valueMax;
            }
            public override string ToString()
            {
                return $"{ResultSet.Entity.Text}: ValueMin={this.ValueMin}, ValueMax:{this.ValueMax}, OrderMin:{_OrderMin}, OrderMax:{_OrderMax}";
            }
            internal ResultSetInfo ResultSet { get; private set; }
            internal decimal ValueMin { get; private set; }
            internal decimal ValueMax { get; private set; }
            /// <summary>
            /// Do daného Listu možná přidá this prvek, pokud tam patří.
            /// </summary>
            /// <param name="analyseInfos"></param>
            /// <param name="highestCount"></param>
            /// <param name="lowestCount"></param>
            internal void AddToListByCondition(List<AnalyseInfo> analyseInfos, int highestCount, int lowestCount)
            {
                int count = analyseInfos.Count;
                if (count == 0)
                {
                    analyseInfos.Add(this);
                    return;
                }

                // David: Asi by šlo vymyslet sofistikovanější řešení, než je to zdejší: 
                //            máme tady dvojí List.Sort a potom dvakrát Scan setříděného lsitu, ukládání pořadí OrderMin a OrderMax,
                //            a následně pak Remove nevyhoujících prvků a pak Add nového prvku.
                //  Ale:   1. Před jedním každým voláním této metody proběhl rozsáhlý kus kódu, kde se scanuje řádově tisíc raw dat, kde se dělají součty za jedno datum,
                //         2. Pak se nad těmi sečtenými daty (v počtu dní analyzovaného úseku, tedy řádově 100 - 500) provádí dva až čtyři agregátní výpočty,
                //            => tedy provedlo se dost jiné práce
                //         3. A navíc dodaný List analyseInfos z principu má nanejvýš 24 položek (je omezeno highestCount i lowestCount na nejvýše 12 + 12 položek).
                //         4. Takže časový poměr zdejší metody (pro max 24 položek Listu) vůči předchozímu kódu (tisíce a stovky záznamů) nestojí za nějaké speciální algoritmy výpočtů.
                //            => a zdejší algoritmus je poměrně čitelný, udržitelný a blbuvzdorný. 
                //               A navíc dělá přesně to co je požadováno na nejrůznějších vstupních datech.

                // 1. Najdu svoji vlastní pozici ve směru od nejmenší ValueMin:
                if (count > 1) analyseInfos.Sort(CompareMinAsc);
                this._OrderMin = null;
                int orderMin = 0;
                foreach (var iMin in analyseInfos)
                {   // Procházím seznam v pořadí od nejmenší hodnoty ValueMin směrem k větším:
                    orderMin++;
                    if (this.ValueMin < iMin.ValueMin && !this._OrderMin.HasValue)
                    {
                        this._OrderMin = orderMin;
                        orderMin++;
                    }
                    iMin._OrderMin = orderMin;
                }
                if (!this._OrderMin.HasValue) this._OrderMin = count + 1;

                // 2. Najdu svoji vlastní pozici ve směru od největší ValueMax:
                if (count > 1) analyseInfos.Sort(CompareMaxDesc);
                this._OrderMax = null;
                int orderMax = 0;
                foreach (var iMax in analyseInfos)
                {   // Procházím seznam v pořadí od největší hodnoty ValueMax směrem k menším:
                    orderMax++;
                    if (this.ValueMax > iMax.ValueMax && !this._OrderMax.HasValue)
                    {
                        this._OrderMax = orderMax;
                        orderMax++;
                    }
                    iMax._OrderMax = orderMax;
                }
                if (!this._OrderMax.HasValue) this._OrderMax = count + 1;

                // 3. Pokud moje pozice není taková, abych patřil do výsledného seznamu, tak skončím:
                if (!this._ValidForResult(highestCount, lowestCount)) return;
                //   ... pokud jsem tady, pak do výsledného seznamu patřím.

                // 4. Pokud nyní jsou v seznamu položky, které do něj nepatří, pak je vyhodíme:
                analyseInfos.RemoveAll(ai => !ai._ValidForResult(highestCount, lowestCount));

                // Přidám se do seznamu a hotovo:
                analyseInfos.Add(this);
            }
            /// <summary>
            /// Vrací true, pokud moje pořadí Min nebo Max mi dává právo být v seznamu s daným počtem nejnižších Min nebo nejvyšších Max hodnot
            /// </summary>
            /// <param name="highestCount"></param>
            /// <param name="lowestCount"></param>
            /// <returns></returns>
            private bool _ValidForResult(int highestCount, int lowestCount)
            {
                return (this._OrderMin <= lowestCount || this._OrderMax <= highestCount);
            }

            private int? _OrderMin;
            private int? _OrderMax;
            /// <summary>
            /// Komparátor ValueMin ASC
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            private static int CompareMinAsc(AnalyseInfo a, AnalyseInfo b) { return a.ValueMin.CompareTo(b.ValueMin); }
            /// <summary>
            /// Komparátor ValueMin ASC
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            private static int CompareMaxDesc(AnalyseInfo a, AnalyseInfo b) { return b.ValueMax.CompareTo(a.ValueMax); }
        }
        #endregion
        #region Vyhledání entit podle názvu a prefixu a Wildcards
        /// <summary>
        /// Metoda vyhledá v databázi obce (a jiné celky) odpovídající danému textu.
        /// </summary>
        /// <param name="searchNazev"></param>
        /// <returns></returns>
        public EntityInfo[] SearchEntities(string searchNazev)
        {
            if (String.IsNullOrEmpty(searchNazev)) return new EntityInfo[0];

            searchNazev = searchNazev.Trim();

            EntityType? entityType = SearchGetPrefixEntity(ref searchNazev);

            bool isWildCard = (searchNazev.StartsWith("*") || searchNazev.StartsWith("%"));
            if (isWildCard)
                searchNazev = searchNazev.Substring(1).Trim();
            if (!entityType.HasValue && String.IsNullOrEmpty(searchNazev)) return new EntityInfo[0];             // Lze zadat jen prefix územního celku: pak se hledá i bez zadání textu = najdou se všechny
            if (entityType.HasValue && String.IsNullOrEmpty(searchNazev))                                     // Po zadání jen prefixu bez názvu = "okres:" bez textu budeme hledat všechny okresy.
                isWildCard = true;

            SearchEntityArgs args = new SearchEntityArgs(entityType, searchNazev, isWildCard);
            this._World.SearchEntities(args);

            List<EntityInfo> result = null;
            if (!args.IsWildCard && args.FoundBeginEntities != null && args.FoundBeginEntities.Count > 0) result = args.FoundBeginEntities;
            else if (args.FoundContainsEntities != null && args.FoundContainsEntities.Count > 0) result = args.FoundContainsEntities;
            if (result == null) return new EntityInfo[0];

            result.Sort((a, b) => String.Compare(a.Nazev, b.Nazev, StringComparison.CurrentCultureIgnoreCase));
            return result.ToArray();
        }
        /// <summary>
        /// Detekuje prefix zadaný do vyhledávacího textu.
        /// Pokud je např. zadaný text = "okres: Jičín", pak detekuje prefix "okres:", metoda vrátí <see cref="EntityType.Okres"/> a v ref parametru zůstane "Jičín".
        /// </summary>
        /// <param name="searchNazev"></param>
        /// <returns></returns>
        private EntityType? SearchGetPrefixEntity(ref string searchNazev)
        {
            EntityType? result = null;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixZeme1, EntityType.Zeme, ref result)) return result;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixZeme2, EntityType.Zeme, ref result)) return result;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixKraj, EntityType.Kraj, ref result)) return result;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixOkres, EntityType.Okres, ref result)) return result;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixMesto1, EntityType.Mesto, ref result)) return result;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixMesto2, EntityType.Mesto, ref result)) return result;
            if (TrySearchGetPrefixEntityOne(ref searchNazev, SearchPrefixObec, EntityType.Obec, ref result)) return result;
            return null;
        }
        /// <summary>
        /// Detekuje přítomnost daného prefixu, a vyřeší ji (určí hodnotu do out parametru, text zkrátí o prefxi, a vrací true).
        /// </summary>
        /// <param name="searchNazev"></param>
        /// <param name="prefix"></param>
        /// <param name="entity"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool TrySearchGetPrefixEntityOne(ref string searchNazev, string prefix, EntityType entity, ref EntityType? result)
        {
            if (!searchNazev.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase)) return false;
            searchNazev = searchNazev.Substring(prefix.Length).Trim();
            result = entity;
            return true;
        }

        private const string SearchPrefixZeme1 = "země:";
        private const string SearchPrefixZeme2 = "zeme:";
        private const string SearchPrefixKraj = "kraj:";
        private const string SearchPrefixOkres = "okres:";
        private const string SearchPrefixMesto1 = "město:";
        private const string SearchPrefixMesto2 = "mesto:";
        private const string SearchPrefixObec = "obec:";
        protected static void SearchEntityAdd(EntityInfo entity, SearchEntityArgs args)
        {
            if (entity == null || String.IsNullOrEmpty(entity.Nazev)) return;
            if (args.EntityType.HasValue && entity.Entity != args.EntityType.Value) return;

            if (args.HasText)
            {
                if (!args.IsWildCard && entity.Nazev.StartsWith(args.SearchText, StringComparison.CurrentCultureIgnoreCase))
                    args.FoundBeginEntities.Add(entity);

                else if (entity.Nazev.IndexOf(args.SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    args.FoundContainsEntities.Add(entity);
            }
            else if (args.IsWildCard && args.EntityType.HasValue && args.SearchText.Length == 0)
                args.FoundContainsEntities.Add(entity);
        }
        /// <summary>
        /// Najde a vrátí entity s daným FullCode.
        /// </summary>
        /// <param name="fullCodes"></param>
        /// <returns></returns>
        public EntityInfo[] GetEntities(IEnumerable<string> fullCodes)
        {
            List<EntityInfo> entities = new List<EntityInfo>();
            if (fullCodes != null)
            {
                foreach (string fullCode in fullCodes)
                {
                    EntityInfo entity = GetEntity(fullCode);
                    if (entity != null)
                        entities.Add(entity);
                }
            }
            return entities.ToArray();
        }
        /// <summary>
        /// Najde a vrátí entitu s daným FullCode.
        /// FullCode začíná kódem Země (typicky "CZ") a následují postupně kódy nižších entit (kraje, okresy, města), oddělené tečkou (= <see cref="EntityInfo.EntityDelimiter"/>).
        /// </summary>
        /// <param name="fullCode"></param>
        /// <returns></returns>
        public EntityInfo GetEntity(string fullCode)
        {
            return this._World.GetEntity(fullCode);
        }
        /// <summary>
        /// Načte a vrátí záznam pro počet obyvatel z databáze z datové tabulky Počet obyvatel. Vstupem je vždy jednoduchý kód entity <see cref="EntityType.Vesnice"/>.
        /// </summary>
        /// <param name="kod"></param>
        /// <returns></returns>
        private PocetObyvatelInfo GetPocet(string kod)
        {
            if (!String.IsNullOrEmpty(kod) && _Pocet != null && _Pocet.TryGetValue(kod, out var pocet)) return pocet;
            return null;
        }
        /// <summary>
        /// Načte a vrátí počet obyvatel z databáze z datové tabulky Počet obyvatel. Vstupem je vždy jednoduchý kód entity <see cref="EntityType.Vesnice"/>.
        /// </summary>
        /// <param name="kod"></param>
        /// <returns></returns>
        private int GetPocetObyvatelInternal(string kod)
        {
            if (!String.IsNullOrEmpty(kod) && _Pocet != null && _Pocet.TryGetValue(kod, out var pocet)) return pocet.Pocet;
            return 0;
        }
        /// <summary>
        /// Vrací typ entity pro danou úroveň, kde 0 = World ... 6 = Vesnice
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static EntityType GetEntityType(int level)
        {
            switch (level)
            {
                case 0: return EntityType.World;
                case 1: return EntityType.Zeme;
                case 2: return EntityType.Kraj;
                case 3: return EntityType.Okres;
                case 4: return EntityType.Mesto;
                case 5: return EntityType.Obec;
                case 6: return EntityType.Vesnice;
            }
            return EntityType.None;
        }
        /// <summary>
        /// Vrátí text Header pro daný level struktury
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static string GetStructureHeader(int level)
        {
            switch (level)
            {
                case 0: return HeaderWorld;
                case 1: return HeaderZeme;
                case 2: return HeaderKraj;
                case 3: return HeaderOkres;
                case 4: return HeaderMesto;
                case 5: return HeaderObec;
                case 6: return HeaderVesnice;
            }
            return "X";
        }
        /// <summary>
        /// Vrátí uživatelsky použitelný text pro danou entitu.
        /// Vrací text ve tvaru: "Nasavrky (obec, 26 440 obyv., okr. Chrudim)"
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static string GetEntityText(EntityInfo entity)
        {
            string entityName = GetEntityName(entity.Entity);
            string pocet = entity.PocetObyvatel.ToString("### ### ### ##0").Trim();

            // Pro malé entity najděme jejich Parent okres:
            string okres = null;
            if (entity.Entity == EntityType.Vesnice)
                okres = entity?.Parent?.Parent?.Parent?.Nazev;
            else if (entity.Entity == EntityType.Obec)
                okres = entity?.Parent?.Parent?.Nazev;
            else if (entity.Entity == EntityType.Mesto)
                okres = entity?.Parent?.Nazev;
            okres = (okres == null ? "" : ", okr. " + okres);
            string obyv = " obyv.";
            if (entity.FullCode.StartsWith("CZ.CZ010")) obyv = " pražáků";          // EasterEggs!     "CZ.CZ010.CZ0100" = okres Praha, "CZ.CZ010" = kraj Praha
            string text = $"{entity.Nazev} ({entityName}, {pocet}{obyv}{okres})";
            return text;
        }
        /// <summary>
        /// Vrací popisek entity do titulku grafu.
        /// Vrací text ve tvaru: "Nasavrky (26 440 obyv.)"
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static string GetEntityTitleText(EntityInfo entity)
        {
            string entityName = GetEntityName(entity.Entity);
            string pocet = entity.PocetObyvatel.ToString("### ### ### ##0").Trim();
            string obyv = " obyv.";
            if (entity.FullCode.StartsWith("CZ.CZ010")) obyv = " pražáků";          // EasterEggs!     "CZ.CZ010.CZ0100" = okres Praha, "CZ.CZ010" = kraj Praha
            string text = $"{entity.Nazev} ({pocet}{obyv})";
            return text;
        }
        /// <summary>
        /// Vrátí uživatelský text druhu entity
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        internal static string GetEntityName(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.World: return "svět";
                case EntityType.Zeme: return "stát";
                case EntityType.Kraj: return "kraj";
                case EntityType.Okres: return "okres";
                case EntityType.Mesto: return "malý okres";
                case EntityType.Obec: return "město a okolí";
                case EntityType.Vesnice: return "obec";
            }
            return entityType.ToString();
        }
        #endregion
        #region Třídy dat: EntityInfo, PocetObyvatelInfo, DataInfo
        /// <summary>
        /// Místní entita (Země, kraj, okres, město, obec, vesnice).
        /// Obsahuje svoje popisná data (kód, název), počet obyvatel, svůj typ, parenta (nadřízený uzel), svoje data, svoje podřízenéí prvky.
        /// </summary>
        public class EntityInfo : ItemInfo
        {
            /// <summary>
            /// Konstruktor pro Root prvek = nemá Parenta, ale má Databázi. Použije se pouze jedenkrát pro root entitu = World.
            /// </summary>
            /// <param name="database"></param>
            /// <param name="kod"></param>
            /// <param name="nazev"></param>
            public EntityInfo(DatabaseInfo database, string kod, string nazev)
            {
                this.Database = database;
                this.Kod = kod;
                this.Nazev = nazev;
            }
            /// <summary>
            /// Konstruktor pro běžný prvek, který má svého Parenta
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="items"></param>
            /// <param name="index"></param>
            public EntityInfo(EntityInfo parent, string kod, string nazev)
            {
                this.Parent = parent;
                this.Kod = kod;
                this.Nazev = nazev;
            }
            /// <summary>
            /// Konstruktor pro běžný prvek, který má svého Parenta
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="items"></param>
            /// <param name="index"></param>
            public EntityInfo(EntityInfo parent, string[] items, int index = 1)
            {
                this.Parent = parent;
                if (items != null && items.Length >= (index + 2))
                {
                    this.Kod = items[index];
                    this.Nazev = items[index + 1];
                    if (items.Length >= (index + 6) && PocetObyvatelInfo.TryCreate(items, (index + 2), out var pocetObyv))
                    {
                        _PocetObyv = pocetObyv;
                        IsPocetObyvLoaded = true;
                    }
                }
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString() { return Text; }
            /// <summary>
            /// Parent entita, u Root entity je null
            /// </summary>
            public EntityInfo Parent { get; protected set; }
            /// <summary>
            /// Reference na databázi
            /// </summary>
            public DatabaseInfo Database { get { return Parent?.Database ?? _Database; } protected set { _Database = value; } } private DatabaseInfo _Database;
            /// <summary>
            /// Úroveň, Root má 0, jeho Child mají postupně +1
            /// </summary>
            public int Level { get { return (Parent != null ? (Parent.Level + 1) : 0); } }
            /// <summary>
            /// Plný kód entity počínaje Root entitou
            /// </summary>
            public virtual string FullCode
            {
                get
                {
                    string parentCode = Parent?.FullCode ?? "";
                    return (parentCode.Length > 0 ? (parentCode + EntityDelimiter) : "") + Kod;
                }
            }
            /// <summary>
            /// Typ this entity
            /// </summary>
            public EntityType Entity { get { return DatabaseInfo.GetEntityType(this.Level); } }
            /// <summary>
            /// Text záhlaví při ukládání entity
            /// </summary>
            protected string StructureHeader { get { return DatabaseInfo.GetStructureHeader(this.Level); } }
            /// <summary>
            /// true pokud reálně máme nějaké <see cref="ChildDict "/>
            /// </summary>
            protected bool HasChilds { get { return (this.ChildDict != null && this.ChildDict.Count > 0); } }
            /// <summary>
            /// Moje vlastní Child entity (například pod Krajem jsou Okresy)
            /// </summary>
            public Dictionary<string, EntityInfo> ChildDict { get; protected set; }
            /// <summary>
            /// true pokud reálně máme nějaké <see cref="LocalDataDict"/>
            /// </summary>
            protected bool HasLocalData { get { return (this.LocalDataDict != null && this.LocalDataDict.Count > 0); } }
            /// <summary>
            /// Nativní informace, vztahující se výhradně k této entitě. 
            /// Zde nejsou informace sumarizované z Child entit.
            /// </summary>
            protected Dictionary<int, DataInfo> LocalDataDict { get; private set; }
            /// <summary>
            /// true pokud zde máme cachované informace z podřízených Childs prvků
            /// </summary>
            protected bool HasCachedData { get { return (this.CachedDataDict != null); } }
            /// <summary>
            /// Souhrn cachovaných informací z Child prvků i z this prvků. Je použitelný pouze pokud nefiltrujeme i podle počtu obyvatel.
            /// </summary>
            protected Dictionary<int, DataInfo> CachedDataDict { get; private set; }
            /// <summary>
            /// Uživatelský text popisující this entitu.
            /// Vrací text ve tvaru: "Nasavrky (obec, 26 440 obyv., okr. Chrudim)"
            /// </summary>
            public string Text { get { return GetEntityText(this); } }
            /// <summary>
            /// Uživatelský text popisující zkráceně this entitu.
            /// Vrací text ve tvaru: "Nasavrky (26 440 obyv.)"
            /// </summary>
            public string TextTitle { get { return GetEntityTitleText(this); } }
            /// <summary>
            /// Kód entity, používá se do <see cref="FullCode"/>
            /// </summary>
            public string Kod { get; protected set; }
            /// <summary>
            /// Holý název entity (jméno města, obce...)
            /// </summary>
            public string Nazev { get; protected set; }
            /// <summary>
            /// Strukturovaný počet obyvatel
            /// </summary>
            public PocetObyvatelInfo PocetObyv
            {
                get
                {
                    if (_PocetObyv == null)
                    {
                        if (this.Entity == EntityType.Vesnice)
                        {
                            var pocet = this.Database.GetPocet(this.Kod);
                            if (pocet != null)
                            {
                                _PocetObyv = pocet;
                                IsPocetObyvLoaded = true;
                            }
                        }
                        if (_PocetObyv == null)
                        {
                            if (this.HasChilds)
                            {
                                _PocetObyv = PocetObyvatelInfo.CreateFromSum(this.ChildDict.Values.Select(c => c.PocetObyv));
                            }
                        }
                        if (_PocetObyv == null)
                        {
                            _PocetObyv = new PocetObyvatelInfo(0, 0, 0, 0);
                        }
                    }
                    return _PocetObyv;
                }
            }
            /// <summary>
            /// Obsahuje true v případě, že Počet obyvatel je načten z reálných dat, false pokud je sečten z Childs údajů. Takový se neukládá.
            /// </summary>
            public bool IsPocetObyvLoaded { get; private set; }
            /// <summary>
            /// Strukturovaný počet obyvatel
            /// </summary>
            private PocetObyvatelInfo _PocetObyv;
            /// <summary>
            /// Počet obyvatel
            /// </summary>
            public int PocetObyvatel { get { return PocetObyv.Pocet; } }
            /// <summary>
            /// Najde a vrátí entitu s daným kódem, kde první část kódu hledá ve svých Childs.
            /// </summary>
            /// <param name="fullCode"></param>
            /// <returns></returns>
            public EntityInfo GetEntity(string fullCode)
            {
                if (String.IsNullOrEmpty(fullCode)) return null;
                if (!HasChilds) return null;

                string[] codes = fullCode.Split(EntityInfo.EntityDelimiter[0]);
                int count = codes.Length;

                if (count < 1) return null;
                Queue<string> codeQueue = new Queue<string>(codes);

                return GetEntity(codeQueue);
            }
            /// <summary>
            /// Najde a vrátí entitu s daným kódem, kde první část kódu hledá ve svých Childs.
            /// </summary>
            /// <param name="codeQueue"></param>
            /// <returns></returns>
            public EntityInfo GetEntity(Queue<string> codeQueue)
            {
                if (codeQueue == null || codeQueue.Count == 0) return null;
                if (!HasChilds) return null;

                string code = codeQueue.Dequeue();
                if (code == null) return null;
                if (!this.ChildDict.TryGetValue(code, out var entity) || entity == null) return null;

                if (codeQueue.Count == 0) return entity;
                return entity.GetEntity(codeQueue);
            }
            /// <summary>
            /// Přidá nový prvek Child. Lze přidat do jakékoli úrovně.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="creator"></param>
            /// <returns></returns>
            public virtual EntityInfo AddOrCreateChild(string kod, Func<EntityInfo> creator)
            {
                if (kod == null) throw new ArgumentNullException($"Nelze najít ani přidat Child prvek do prvku typu {this.GetType().Name}, pokud kód Child prvku je NULL.");
                EntityInfo result;
                if (this.ChildDict == null) this.ChildDict = new Dictionary<string, EntityInfo>();
                if (!this.ChildDict.TryGetValue(kod, out result))
                {
                    result = creator();
                    this.ChildDict.Add(kod, result);
                }
                return result;
            }
            ///// <summary>
            ///// Přidá nový prvek Info = reálná informace na této úrovni. Lze přidat do jakékoli úrovně.
            ///// </summary>
            ///// <param name="key"></param>
            ///// <param name="creator"></param>
            ///// <returns></returns>
            //public virtual DataInfo AddOrCreateData(int key, Func<DataInfo> creator)
            //{
            //    DataInfo result;
            //    if (this.LocalDataDict == null) this.LocalDataDict = new Dictionary<int, DataInfo>();
            //    if (!this.LocalDataDict.TryGetValue(key, out result))
            //    {
            //        result = creator();
            //        this.LocalDataDict.Add(key, result);
            //    }
            //    return result;
            //}
            /// <summary>
            /// Přidá nový prvek Info = reálná informace na této úrovni. Lze přidat do jakékoli úrovně.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="creator"></param>
            /// <returns></returns>
            public virtual DataInfo AddOrCreateInfo(DateTime infoDate, int key)
            {
                DataInfo result;
                if (this.LocalDataDict == null) this.LocalDataDict = new Dictionary<int, DataInfo>();
                if (!this.LocalDataDict.TryGetValue(key, out result))
                {
                    result = new DataInfo(this, infoDate);
                    this.LocalDataDict.Add(key, result);
                }
                return result;
            }
            /// <summary>
            /// Přidá nová data
            /// </summary>
            /// <param name="infoDate"></param>
            /// <param name="key"></param>
            /// <param name="valueType"></param>
            /// <param name="value"></param>
            /// <param name="gender"></param>
            /// <param name="ageFrom"></param>
            /// <param name="ageTo"></param>
            /// <param name="specKey"></param>
            public virtual void AddData(DateTime infoDate, int key, DataValueType valueType, int value, DataGenderType gender = DataGenderType.NotSpecified, int ageFrom = 0, int ageTo = 0, string specKey = null)
            {
                DataInfo result = AddOrCreateInfo(infoDate, key);
                result.AddData(valueType, value, gender, ageFrom, ageTo, specKey);
            }
            public virtual void Clear(FileContentType contentType)
            {
                if (HasChilds)
                {
                    foreach (var item in this.ChildDict.Values)
                        item.Clear(contentType);

                    if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    {
                        this.ChildDict.Clear();
                        this.ChildDict = null;
                    }
                }
                if (HasLocalData)
                {
                    if (contentType == FileContentType.Data || contentType == FileContentType.DataPack)
                    {
                        this.LocalDataDict.Clear();
                        this.LocalDataDict = null;
                    }
                    else
                    {
                        this.LocalDataDict.Values.ForEachExec(d => d.Clear(contentType));
                    }
                }
            }
            public virtual void Save(ProcessFileInfo saveInfo, DZipFileWriter stream)
            {
                // Uložit hlavičku pro this instanci? Pokud se ukládá komletní struktura entit (Structure nebo DataPack), 
                ///  anebo tehdy pokud máme vlastní data = pak řádek typu Entita reprezentuje konkrétní entitu, do které patří následující data:
                bool saveHeader = (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack || HasLocalData);
                if (saveHeader)
                    SaveHeader(saveInfo, stream);

                // Uložit informace: pokud se ukládá odpovídající formát, a pokud my máme nějaké vlastní informace:
                bool saveData = ((saveInfo.ContentType == FileContentType.Data || saveInfo.ContentType == FileContentType.DataPack) && HasLocalData);
                if (saveData)
                    this.LocalDataDict.Values.ForEachExec(c => c.Save(saveInfo, stream));

                // Childs vždycky, když jsou:
                if (HasChilds)
                    this.ChildDict.Values.ForEachExec(c => c.Save(saveInfo, stream));
            }
            /// <summary>
            /// Uloží jeden řádek obsahující vlastní data this entity (kód, název, volitelně počet obyvatel)
            /// </summary>
            /// <param name="saveInfo"></param>
            /// <param name="stream"></param>
            protected virtual void SaveHeader(ProcessFileInfo saveInfo, DZipFileWriter stream)
            {
                // Plné ukládání (včetně počtu obyvatel) při ukládání Struktury nebo DataPack, ale ne při ukládání holých dat:
                bool saveFull = (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack);
                string line = $"{StructureHeader};{Kod};{Nazev}";
                if (saveFull && IsPocetObyvLoaded)
                    line += ";" + this.PocetObyv.Line;
                stream.WriteLine(line);
            }
            /// <summary>
            /// Prohledá informace a vyhovující vloží do argumentu
            /// </summary>
            /// <param name="args"></param>
            public virtual void SearchInfo(SearchInfoArgs args)
            {
                args.ResultSet.ScanRecordCount++;                    // Statistika

                // Filtr na Počet obyvatel je závažný rozdíl v algoritmu z hlediska cachovaných informací:
                bool isTestPocetObyvatel = args.IsTestPocetObyvatel;

                // Cachované data (až budou) budou akceptovatelné jen tehdy, když NEBUDE potřebný filtr na počet obyvatel:
                if (HasCachedData && !isTestPocetObyvatel)
                {



                    return;    // nejdeme dál, a hlavně neřešíme Childs, protože jejich informace už máme zpracované v cache.
                }

                // Pokud mám vlastní data, pak je zpracuji:
                if (HasLocalData)
                {
                    bool isValidPocetObyvatel = true;
                    // Filtr na Počet obyvatel se aplikuje pouze na nejnižší úrovni entit (=tam, kde nemám Childs).
                    // Pokud bych měl lokální data (HasLocalData) a současně měl Childs (HasChilds), pak svoje lokální data budu akceptovat i bez filtru na počet obyvatel,
                    //  šlo by totiž o nějaké jiné informace než ty standardně filtrované počtem obyvatel (typicky: počet zemřelých, uváděný na úrovni Okresu: tam nemá význam filtrovat dle velikosti obce).
                    if (!HasChilds)
                        isValidPocetObyvatel = args.IsValidPocetObyvatel(this.PocetObyvatel);

                    if (isValidPocetObyvatel)
                    {
                        args.PocetObyvatel += this.PocetObyvatel;
                        foreach (var data in this.LocalDataDict.Values)
                            args.AddDataToResult(data);
                    }
                }

                // Projdu všechny svoje Child entity:
                if (this.HasChilds)
                    this.ChildDict.Values.ForEachExec(c => c.SearchInfo(args));
            }
            public virtual void SearchEntities(SearchEntityArgs args)
            {
                DatabaseInfo.SearchEntityAdd(this, args);
                if (this.HasChilds)
                    this.ChildDict.Values.ForEachExec(c => c.SearchEntities(args));
            }
            /// <summary>
            /// Vrátí pole entit daného typu, které jsou některé moje Childs (včetně this), a volitelně vyhovují filtru.
            /// </summary>
            /// <param name="entityType"></param>
            /// <param name="filter"></param>
            /// <returns></returns>
            internal EntityInfo[] GetChildsOfType(EntityType entityType, Func<EntityInfo, bool> filter = null)
            {
                List<EntityInfo> entities = new List<EntityInfo>();
                this.AddChildsOfType(entityType, filter, entities);
                return entities.ToArray();
            }

            private void AddChildsOfType(EntityType entityType, Func<EntityInfo, bool> filter, List<EntityInfo> entities)
            {
                // Využívám Int32 hodnoty enumu, kde logixky vyšší entita (např. Země = 2) má menší numerickou hodnotu než nižší (např. Město = 5):
                int diff = ((int)entityType) - ((int)this.Entity);

                // Pokud např. hledáme Okres (=4), a já jsem Město (=5), pak diff = (4 - 5) = -1, záporná hodnota, a tak rovnou skončíme. Nehledáme ani mě ani moje Childs:
                if (diff < 0) return;

                // Hledaná úroveň je ta moje:
                if (diff == 0)
                {
                    if (filter == null || filter(this))
                        entities.Add(this);
                    return;
                }

                // Hledáme některou nižší úroveň:
                if (this.HasChilds)
                    this.ChildDict.Values.ForEachExec(c => c.AddChildsOfType(entityType, filter, entities));
            }

            /// <summary>
            /// Oddělovač složek v plném kódu entity
            /// </summary>
            public const string EntityDelimiter = ".";
        }
        /// <summary>
        /// Počet obyvatel lehce strukturovaný
        /// </summary>
        public class PocetObyvatelInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="pocetMuziCelkem"></param>
            /// <param name="pocetMuziNad15"></param>
            /// <param name="pocetZenyCelkem"></param>
            /// <param name="pocetZenyNad15"></param>
            public PocetObyvatelInfo(int pocetMuziCelkem, int pocetMuziNad15, int pocetZenyCelkem, int pocetZenyNad15)
            {
                this.MuziCelkem = pocetMuziCelkem;
                this.MuziNad15 = pocetMuziNad15;
                this.ZenyCelkem = pocetZenyCelkem;
                this.ZenyNad15 = pocetZenyNad15;
            }
            /// <summary>
            /// Vytvoří new instanci ze součtu hodnot dodaných instancí. Pokud není dodáno nic, vrací empty instanci (ne null)
            /// </summary>
            /// <param name="items"></param>
            /// <returns></returns>
            public static PocetObyvatelInfo CreateFromSum(IEnumerable<PocetObyvatelInfo> items)
            {
                int mc = 0;
                int mn = 0;
                int zc = 0;
                int zn = 0;
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item == null) continue;
                        mc += item.MuziCelkem;
                        mn += item.MuziNad15;
                        zc += item.ZenyCelkem;
                        zn += item.ZenyNad15;
                    }
                }
                return new PocetObyvatelInfo(mc, mn, zc, zn);
            }
            /// <summary>
            /// Zkusí vytvořit instanci z textů v poli items, počínaje daným indexem.
            /// Používá se při načítání dat ze souboru.
            /// Víceméně koreluje s property <see cref="Line"/>, která z dat instance vytvoří text ukládaný do souboru při Save().
            /// </summary>
            /// <param name="items"></param>
            /// <param name="index"></param>
            /// <param name="pocetObyv"></param>
            /// <returns></returns>
            internal static bool TryCreate(string[] items, int index, out PocetObyvatelInfo pocetObyv)
            {
                pocetObyv = null;
                if (items == null || items.Length < (index + 4)) return false;
                if (!(Int32.TryParse(items[index], out int mc) &&
                      Int32.TryParse(items[index + 1], out int mn) &&
                      Int32.TryParse(items[index + 2], out int zc) &&
                      Int32.TryParse(items[index + 3], out int zn)))
                    return false;
                pocetObyv = new PocetObyvatelInfo(mc, mn, zc, zn);
                return true;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Počet obyvatel: {Pocet}";
            }
            /// <summary>
            /// Text pro uložení do databáze
            /// </summary>
            public string Line { get { return $"{MuziCelkem};{MuziNad15};{ZenyCelkem};{ZenyNad15}"; } }
            /// <summary>
            /// Počet mužů celkem
            /// </summary>
            public int MuziCelkem { get; private set; }
            /// <summary>
            /// Počet mužů nad 15 roků
            /// </summary>
            public int MuziNad15 { get; private set; }
            /// <summary>
            /// Počet žen celkem
            /// </summary>
            public int ZenyCelkem { get; private set; }
            /// <summary>
            /// Počet žen nad 15 roků
            /// </summary>
            public int ZenyNad15 { get; private set; }
            /// <summary>
            /// Počet celkem
            /// </summary>
            public int Pocet { get { return (MuziCelkem + ZenyCelkem); } }
        }
        /// <summary>
        /// Datové informace základní / cachované
        /// </summary>
        public class DataInfo : ItemInfo
        {
            public DataInfo(EntityInfo parent, DateTime date)
            {
                this.Parent = parent;
                this.Date = date;
                this._Items = new List<DataItem>();
                this.NewCount = 0;
                this.CurrentCount = 0;
            }
            public DataInfo(EntityInfo parent, DateTime date, int newCount, int currentCount)
            {
                this.Parent = parent;
                this.Date = date;
                this.NewCount = newCount;
                this.CurrentCount = currentCount;
            }
            public override string ToString()
            {
                return Text;
            }
            /// <summary>
            /// Úložiště dat
            /// </summary>
            private List<DataItem> _Items;
            public string Text { get { return $"Datum: {(Date.ToString("dd.MM.yyyy"))}; NewCount: {NewCount}; CurrentCount: {CurrentCount}; Parent: {Parent.Text}"; } }
            public EntityInfo Parent { get; private set; }
            public DatabaseInfo Database { get { return Parent.Database; } }
            public DateTime Date { get; private set; }
            public int DateKey { get { return Date.GetDateKey(); } }
            protected int DateKeyShort { get { return this.Date.GetDateKeyShort(); } }
            public void Save(ProcessFileInfo saveInfo, DZipFileWriter stream)
            {
                switch (saveInfo.ContentType)
                {
                    case FileContentType.Data:
                        stream.WriteLine($"{HeaderInfo};{DateKeyShort};{NewCount};{CurrentCount}");
                        break;
                    case FileContentType.DataPack:
                        stream.WriteLine($"{HeaderInfo};{DateKey};{NewCount};{CurrentCount}");
                        break;
                }
            }
            public int NewCount { get; private set; }
            public int CurrentCount { get; private set; }
            /// <summary>
            /// Přidá nová data
            /// </summary>
            /// <param name="valueType"></param>
            /// <param name="value"></param>
            /// <param name="gender"></param>
            /// <param name="ageFrom"></param>
            /// <param name="ageTo"></param>
            /// <param name="specKey"></param>
            public virtual void AddData(DataValueType valueType, int value, DataGenderType gender = DataGenderType.NotSpecified, int ageFrom = 0, int ageTo = 0, string specKey = null)
            {
                switch (valueType)
                {
                    case DataValueType.SourceNewCount:
                        this.NewCount += value;
                        break;
                    case DataValueType.SourceCurrentCount:
                        this.CurrentCount += value;
                        break;
                }
            }
            /// <summary>
            /// Smaže data daného typu
            /// </summary>
            /// <param name="contentType"></param>
            public virtual void Clear(FileContentType contentType)
            { }
            public virtual void AddResult()
            {
                // qqq;
            }

        }
        public class DataItem
        {
            public DataValueType ValueType { get; private set; }
            public int Value { get; private set; }
            public DataGenderType Gender { get; private set; }
            public int AgeFrom { get; private set; }
            public int AgeTo { get; private set; }
            public string SpecKey { get; private set; }
        }
        public class Pocet : ItemInfo
        {
            public Pocet(DatabaseInfo database, string kod, string nazev, string mesto, string kraj, int pocetMC, int pocetMS, int pocetFC, int pocetFS)
            {
                this.Database = database;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Mesto = mesto;
                this.Kraj = kraj;
                this.PocetMC = pocetMC;
                this.PocetMS = pocetMS;
                this.PocetFC = pocetFC;
                this.PocetFS = pocetFS;
                this.PocetCelkem = (pocetMC + pocetFC);
            }
            public override string ToString()
            {
                return Text;
            }
            public string Text { get { return TextDebug; } }
            public string TextDebug { get { return $"Počet; Kód: {Kod}; Název: {Nazev}; Mesto: {Mesto}; Počet obyvatel: {PocetCelkem}" ; } }
            public DatabaseInfo Database { get; private set; }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                stream.WriteLine($"{HeaderPocet};{Kod};{Nazev};{Mesto};{Kraj};{PocetMC};{PocetMS};{PocetFC};{PocetFS}");
            }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public string Mesto { get; private set; }
            public string Kraj { get; private set; }
            public int PocetMC { get; private set; }
            public int PocetMS { get; private set; }
            public int PocetFC { get; private set; }
            public int PocetFS { get; private set; }
            public int PocetCelkem { get; private set; }
        }
        public abstract class ItemInfo
        { }
        #endregion
    }
    #region Třídy pro výsledky analýzy
    /// <summary>
    /// Argument pro hledání dat grafů
    /// </summary>
    public class SearchInfoArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="valueType"></param>
        /// <param name="dataTypeInfo"></param>
        /// <param name="begin">Vstup: uživatelem požadovaný počátek (včetně) = tato data chce vidět</param>
        /// <param name="end">Vstup: uživatelem požadovaný konec (mimo) = tato data chce vidět</param>
        /// <param name="sourceBegin">Potřebný počátek načítaných dat (včetně) = tato data je třeba načíst, abychom mohli spočítat podklady k uživatelskému počátku</param>
        /// <param name="sourceEnd">Potřebný konec načítaných dat (mimo) = tato data je třeba načíst, abychom mohli spočítat podklady k uživatelskému počátku</param>
        /// <param name="pocetOd"></param>
        /// <param name="pocetDo"></param>
        public SearchInfoArgs(DatabaseInfo.EntityInfo entity, DataValueType valueType, DataValueTypeInfo dataTypeInfo, DateTime? begin, DateTime? end, DateTime? sourceBegin, DateTime? sourceEnd, int? pocetOd, int? pocetDo)
        {
            this.Entity = entity;
            this.SourceType = (DataValueType)(valueType & DataValueType.CommonSources);
            this.ValueType = valueType;
            this.DataTypeInfo = dataTypeInfo;
            this.Begin = begin;
            this.End = end;
            this.SourceBegin = sourceBegin;
            this.SourceEnd = sourceEnd;
            this.PocetObyvatel = 0;
            this.PocetOd = pocetOd;
            this.PocetDo = pocetDo;
            this.ResultSet = new ResultSetInfo(entity, valueType);
        }
        /// <summary>
        /// Výchozí entita hledání
        /// </summary>
        public DatabaseInfo.EntityInfo Entity { get; private set; }
        /// <summary>
        /// Zdroj dat z databáze (jde o hodnotu <see cref="ValueType"/> oseknutou pouze na bity Source = <see cref="DataValueType.CommonSources"/>).
        /// Používá se při fyzickém načítání z databáze.
        /// </summary>
        public DataValueType SourceType { get; private set; }
        /// <summary>
        /// Typ datové hodnoty
        /// </summary>
        public DataValueType ValueType { get; private set; }
        /// <summary>
        /// Info o typu datové hodnoty
        /// </summary>
        public DataValueTypeInfo DataTypeInfo { get; private set; }
        /// <summary>
        /// Počátek období, finální uživatelská hodnota (včetně) (toto datum půjde do grafu)
        /// </summary>
        public DateTime? Begin { get; private set; }
        /// <summary>
        /// Konec období, finální uživatelská hodnota (mimo) (toto datum půjde do grafu)
        /// </summary>
        public DateTime? End { get; private set; }
        /// <summary>
        /// Potřebný počátek načítaných dat (včetně) = tato data je třeba načíst, abychom mohli spočítat podklady k uživatelskému počátku
        /// </summary>
        public DateTime? SourceBegin { get; private set; }
        /// <summary>
        /// Potřebný konec načítaných dat (mimo) = tato data je třeba načíst, abychom mohli spočítat podklady k uživatelskému počátku
        /// </summary>
        public DateTime? SourceEnd { get; private set; }
        /// <summary>
        /// Filtrovat (na nejnižší úrovni) pouze obce s počtem obyvatel v rozmezí <see cref="PocetOd"/> až <see cref="PocetDo"/>
        /// </summary>
        public int? PocetOd { get; private set; }
        /// <summary>
        /// Filtrovat (na nejnižší úrovni) pouze obce s počtem obyvatel v rozmezí <see cref="PocetOd"/> až <see cref="PocetDo"/>
        /// </summary>
        public int? PocetDo { get; private set; }
        /// <summary>
        /// Sumární počet obyvatel výchozí entity, pro relativní výpočty
        /// </summary>
        public int PocetObyvatel { get; set; }
        /// <summary>
        /// Pole nalezených záznamů s daty
        /// </summary>
        public IEnumerable<ResultInfo> Results { get { return this.ResultSet.WorkingDict.Values; } }
        /// <summary>
        /// Pole nalezených záznamů s daty, Dictionary, kde Key = datum
        /// </summary>
        public Dictionary<int, ResultInfo> ResultDict { get { return this.ResultSet.WorkingDict; } }
        /// <summary>
        /// Kompletní data výsledků
        /// </summary>
        public ResultSetInfo ResultSet { get; private set; }

        /// <summary>
        /// Obsahuje true, pokud this argument obsahuje filtr na Počet obyvatel.
        /// Pokud ano, pak se filtr na počet obyvatel <see cref="IsValidPocetObyvatel(int)"/> musí aplikovat na nejnižší úrovni struktury (entity), 
        /// a nelze akceptovat cachované informace na vyšších úrovních entity, protože ty jsou sumované ze všech podřízených entit bez filtru na počet obyvatel.
        /// </summary>
        public bool IsTestPocetObyvatel { get { return (this.PocetOd.HasValue || this.PocetDo.HasValue); } }
        /// <summary>
        /// Vrátí true, pokud daný počet obyvatel vyhovuje požadavku.
        /// </summary>
        /// <param name="pocetObyvatel"></param>
        /// <returns></returns>
        public bool IsValidPocetObyvatel(int pocetObyvatel)
        {
            if (this.PocetOd.HasValue && pocetObyvatel < this.PocetOd.Value) return false;         // Filtr je: "Od 1000", pak 999 nevyhovuje, ale 1000 vyhovuje
            if (this.PocetDo.HasValue && pocetObyvatel >= this.PocetDo.Value) return false;        // Filtr je: "Do 5000", pak 5000 už nevyhovuje (ale 4999 vyhovuje)
            return true;
        }
        /// <summary>
        /// Otestuje dodaná data zda vyhovují z hlediska datumu, a pokud ano pak do resultu přidá jejich patřičnou hodnotu.
        /// </summary>
        /// <param name="data"></param>
        public void AddDataToResult(DatabaseInfo.DataInfo data)
        {
            this.ResultSet.ScanRecordCount++;                                                      // Statistika

            // qqq;

            if (this.SourceBegin.HasValue && data.Date < this.SourceBegin.Value) return;
            if (this.SourceEnd.HasValue && data.Date >= this.SourceEnd.Value) return;
            this.ResultSet.AddInfo(this.Entity, this.SourceType, data);                            // SourceType obsahuje pouze Source bity z hodnoty ValueType
        }
    }
    /// <summary>
    /// Třída výsledků
    /// </summary>
    public class ResultSetInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ResultSetInfo(DatabaseInfo.EntityInfo entity, DataValueType valueType)
        {
            this.Entity = entity;
            this.EntityText = Entity?.Text ?? "";
            this.ValueType = valueType;
            this.ScanRecordCount = 0;
            this.LoadRecordCount = 0;
            this.WorkingDict = new Dictionary<int, ResultInfo>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Entity.Text}, {ValueType}";
        }
        /// <summary>
        /// Entita, pro kterou jsou tyto výsledky nalezeny
        /// </summary>
        public DatabaseInfo.EntityInfo Entity { get; private set; }
        /// <summary>
        /// Hodnota, kterou tyto výsledky vyjadřují
        /// </summary>
        public DataValueType ValueType { get; private set; }
        /// <summary>
        /// Text entity
        /// </summary>
        protected string EntityText;
        /// <summary>
        /// Počet všech záznamů, které prošly vyhledáváním
        /// </summary>
        public int ScanRecordCount { get; set; }
        /// <summary>
        /// Počet datových záznamů, započtených do výsledků vyhledání
        /// </summary>
        public int LoadRecordCount { get; set; }
        /// <summary>
        /// Počet záznamů, které budeme zobrazovat
        /// </summary>
        public int ShowRecordCount { get { return (Results != null ? Results.Length : 0); } }
        /// <summary>
        /// Pole nalezených záznamů s daty, Dictionary, Key = datum.
        /// Jde o záznamy vstupní a pracovní, bez filtrování dle data.
        /// </summary>
        public Dictionary<int, ResultInfo> WorkingDict { get; private set; }
        /// <summary>
        /// Čisté pole výstupních záznamů, filtrované dle data.
        /// </summary>
        public ResultInfo[] Results { get; set; }
        /// <summary>
        /// Poslední hodnota = hodnota Value ze záznamu z <see cref="Results"/> s nejvyšším datumem. Určuje se v procesu zpracování dat.
        /// </summary>
        public decimal ValueLast { get; set; }
        /// <summary>
        /// Třídění podle <see cref="Entity"/>.Text
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int CompareByEntityText(ResultSetInfo a, ResultSetInfo b)
        {
            return String.Compare(a.EntityText, b.EntityText, StringComparison.CurrentCultureIgnoreCase);
        }
        /// <summary>
        /// Komparátor podle ValueLast ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByValueLastAsc(ResultSetInfo a, ResultSetInfo b)
        {
            return a.ValueLast.CompareTo(b.ValueLast);
        }
        /// <summary>
        /// Komparátor podle ValueLast DESC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByValueLastDesc(ResultSetInfo a, ResultSetInfo b)
        {
            return b.ValueLast.CompareTo(a.ValueLast);
        }
        /// <summary>
        /// Přidá další hodnotu.
        /// Sumarizuje hodnoty za shodné datum: pokud přidám pro jedno datum více hodnot, budou v daném dni sečteny.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="sourceType"></param>
        /// <param name="info"></param>
        internal void AddInfo(DatabaseInfo.EntityInfo entity, DataValueType sourceType, DatabaseInfo.DataInfo info)
        {
            this.LoadRecordCount++;                                  // Statistika

            int key = info.DateKey;
            ResultInfo result = this.WorkingDict.AddOrCreate(key, () => new ResultInfo(entity, info.Date));
            result.AddInfo(info, sourceType);
        }
    }
    /// <summary>
    /// Třída jednoho výsledku
    /// </summary>
    public class ResultInfo
    {
        public ResultInfo(DatabaseInfo.EntityInfo entity, DateTime date)
        {
            this.Entity = entity;
            this.Date = date;
            this.RawValue = 0;
        }
        public override string ToString()
        {
            return Text;
        }
        public string Text { get { return TextDebug; } }
        public string TextDebug { get { return $"ResultInfo; Datum: {(Date.ToString("dd.MM.yyyy"))}; RawValue: {RawValueText}, Value: {ValueText}, TempValue: {TempValueText}; Entita: {Entity.Text}"; } }
        public DatabaseInfo.EntityInfo Entity { get; private set; }
        public DatabaseInfo Database { get { return Entity.Database; } }
        public DateTime Date { get; private set; }
        public int DateKey { get { return Date.GetDateKey(); } }
        private string RawValueText { get { return GetText(RawValue); } }
        private string ValueText { get { return GetText(Value); } }
        private string TempValueText { get { return GetText(TempValue); } }
        private static string GetText(decimal value)
        {
            if (value < 20m)
                return Math.Round(value, 2).ToString("### ##0.00").Trim();
            else if (value < 80m)
                return Math.Round(value, 1).ToString("##0.0").Trim();
            else
                return Math.Round(value, 0).ToString("### ### ##0").Trim();
        }

        /// <summary>
        /// Vstupující hodnota (konkrétní počet)
        /// </summary>
        public decimal RawValue { get; private set; }
        /// <summary>
        /// Výstupní hodnota (například Average, nebo Relative)
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// Pracovní hodnota pro výpočty
        /// </summary>
        public decimal TempValue { get; set; }
        /// <summary>
        /// Do this resultu vloží výchozí Raw data z <see cref="Info"/> podle požadovaného typu cílové hodnoty
        /// </summary>
        /// <param name="info"></param>
        /// <param name="sourceType">Source bity z ValueType</param>
        internal void AddInfo(DatabaseInfo.DataInfo info, DataValueType sourceType)
        {
            decimal value = 0m;
            switch (sourceType)
            {
                case DataValueType.SourceNewCount:
                    value = info.NewCount;
                    break;
                case DataValueType.SourceCurrentCount:
                    value = info.CurrentCount;
                    break;
            }
            this.RawValue += value;
        }
    }
    /// <summary>
    /// Průběžná data pro hledání obcí (okresů, krajů...)
    /// </summary>
    public class SearchEntityArgs
    {
        public SearchEntityArgs(EntityType? entityType, string searchText, bool isWildCard)
        {
            this.EntityType = entityType;
            this.HasText = !String.IsNullOrEmpty(searchText);
            this.SearchText = searchText;
            this.IsWildCard = isWildCard;
            this.FoundBeginEntities = new List<DatabaseInfo.EntityInfo>();
            this.FoundContainsEntities = new List<DatabaseInfo.EntityInfo>();
        }
        public EntityType? EntityType { get; private set; }
        public bool HasText { get; private set; }
        public string SearchText { get; private set; }
        public bool IsWildCard { get; private set; }
        public List<DatabaseInfo.EntityInfo> FoundBeginEntities { get; private set; }
        public List<DatabaseInfo.EntityInfo> FoundContainsEntities { get; private set; }
    }
    #endregion
    #region Obecný přístup k datům entity (země, kraj, okres, město, obec, ves)
    /// <summary>
    /// Druh entity
    /// </summary>
    public enum EntityType : int
    {
        None = 0,
        /// <summary>
        /// Celá planeta = součet všech dat, nejvyší level v roce 2021
        /// </summary>
        World,
        /// <summary>
        /// Země = Česká republika, Německo, Polsko
        /// </summary>
        Zeme,
        /// <summary>
        /// Kraj = Pardubický, anebo spolková země v Německu = Bavorsko, Braniborsko atd, anebo Městské státy = Berlín, Brémy
        /// </summary>
        Kraj,
        /// <summary>
        /// Okres = Chrudim, Kolín
        /// </summary>
        Okres,
        /// <summary>
        /// Město = obec s rozšířenou působností, takové jsou v okrese 2 až 4 = Hlinsko
        /// </summary>
        Mesto,
        /// <summary>
        /// Obec = malé městečko, které spravuje 1-2 okolní vesničky = Nasavrky
        /// </summary>
        Obec,
        /// <summary>
        /// Nejmenší celek = Holetín
        /// </summary>
        Vesnice
    }
    /// <summary>
    /// Třída pro sekvenční zpracování dat (Load, Update, Save...)
    /// </summary>
    public class ProcessQueueInfo
    {
        public ProcessQueueInfo(Action<ProgressArgs> progressAction)
        {
            this.ProgressAction = progressAction;
            this.CurrentAction = null;
            this._ActionsList = new List<ProcessQueueItem>();
            this._MessageList = new List<string>();
        }
        private List<ProcessQueueItem> _ActionsList;
        private List<string> _MessageList;
        public override string ToString()
        {
            return (this.IsEmpty ? "Empty Queue" : this._ActionsList.Count.ToString() + " actions");
        }
        /// <summary>
        /// true pokud je fronta prázdná
        /// </summary>
        public bool IsEmpty { get { return _ActionsList.Count == 0; } }
        public Action<ProgressArgs> ProgressAction { get; set; }
        /// <summary>
        /// Dané akce vloží na začátek fronty, před akce které ve frontě již jsou.
        /// Vložené akce (parametr) budou tedy ve frontě na první pozici, v tom pořadí v jakém jsou předány.
        /// </summary>
        /// <param name="actions"></param>
        internal void InsertActions(params ProcessQueueItem[] actions)
        {
            foreach (var action in actions)
                action.ProcessQueue = this;

            List<ProcessQueueItem> actionList = new List<ProcessQueueItem>(actions);
            actionList.AddRange(this._ActionsList);
            this._ActionsList = actionList;
        }
        /// <summary>
        /// Přidá dané akce na konec fronty.
        /// </summary>
        /// <param name="actions"></param>
        internal void AddActions(params ProcessQueueItem[] actions)
        {
            foreach (var action in actions)
                action.ProcessQueue = this;

            this._ActionsList.AddRange(actions);
        }
        internal ProcessQueueItem GetAction()
        {
            ProcessQueueItem action = null;
            lock (_ActionsList)
            {
                if (_ActionsList.Count > 0)
                {
                    action = _ActionsList[0];
                    _ActionsList.RemoveAt(0);
                }
            }
            CurrentAction = action;
            return action;
        }
        /// <summary>
        /// true pokud existuje alespoň jedna zpráva, pak má smysl řešit Messages
        /// </summary>
        internal bool HasMessages
        {
            get
            {
                return (this._MessageList.Count > 0);
            }
        }
        internal string Messages
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var message in this._MessageList)
                {
                    sb.AppendLine(message);
                    sb.AppendLine();
                }
                return sb.ToString();
            }
        }
        internal void AddMessage(string message)
        {
            if (!String.IsNullOrEmpty(message))
                this._MessageList.Add(message);
        }

        /// <summary>
        /// Aktuálně prováděná akce. Je nastavena v metodě <see cref="GetAction"/>, a od té doby je zde dostupná.
        /// </summary>
        internal ProcessQueueItem CurrentAction { get; private set; }
    }
    /// <summary>
    /// Jedna akce ve frontě akcí <see cref="ProcessQueueInfo"/>
    /// </summary>
    public class ProcessQueueItem
    {
        public ProcessQueueItem(ProcessActionType actionType)
        {
            this.ActionType = actionType;
            this.File = null;
            this.FileType = null;
            this.Content = null;
            this.SaveFileFormat = null;
            this.SaveFileBackupMode = null;
            this.ProcessFile = null;
            this.WebUpdateTimeConfigName = null;
            this.WebUpdateMinAcceptSize = null;
        }
        public ProcessQueueItem(ProcessActionType actionType, string file) : this(actionType)
        {
            this.ActionType = actionType;
            this.File = file;
        }
        public ProcessQueueItem(ProcessActionType actionType, FileContentType fileType) : this(actionType)
        {
            this.ActionType = actionType;
            this.FileType = fileType;
        }
        public ProcessQueueItem(ProcessActionType actionType, byte[] content) : this(actionType)
        {
            this.ActionType = actionType;
            this.Content = content;
        }
        public override string ToString()
        {
            string text = this.ActionType.ToString();
            if (this.File != null)
                text += "; File: " + this.File;
            else if (this.FileType.HasValue)
                text += "; FileType: " + this.FileType.Value.ToString();
            else if (this.Content != null)
                text += "; Content: " + this.Content.Length.ToString("### ### ### ##0").Trim() + " Bytes";

            return text;
        }
        public ProcessQueueInfo ProcessQueue { get; set; }
        public ProcessActionType ActionType { get; private set; }
        public string File { get; set; }
        public FileContentType? FileType { get; set; }
        public byte[] Content { get; private set; }
        public SaveFormat? SaveFileFormat { get; set; }
        /// <summary>
        /// Jak zálohovat existující soubor (přejmenovat starší před tím, než se uloží nový)
        /// </summary>
        public BackupMode? SaveFileBackupMode { get; set; }
        public ProcessFileInfo ProcessFile { get; set; }
        /// <summary>
        /// Název konfigurace, kam se ukládá datum posledního úspěšného WebUpdate
        /// </summary>
        public string WebUpdateTimeConfigName { get; set; }
        /// <summary>
        /// Nejmenší platná / akceptovatelná velikost souboru. Pokud bude staženo menší množství dat, nebudou se akceptovat.
        /// </summary>
        public int? WebUpdateMinAcceptSize { get; set; }

    }
    /// <summary>
    /// Druh akce, která má být provedena
    /// </summary>
    public enum ProcessActionType
    {
        /// <summary>
        /// Žádná
        /// </summary>
        None,
        /// <summary>
        /// Smazání všech dat v databázi
        /// </summary>
        ClearAll,
        /// <summary>
        /// Úvodní načtení dat = vyhledání souborů na disku a zadání jednotlivých akcí <see cref="LoadFile"/> na konkrétní soubory do fronty,
        /// plus případně <see cref="SaveFile"/> pro uložení provozních dat
        /// </summary>
        LoadInitial,
        /// <summary>
        /// Načtení jednoho konkrétního souboru z disku
        /// </summary>
        LoadFile,
        /// <summary>
        /// Načtení dat dodaných jako byte[] Content
        /// </summary>
        LoadContent,
        /// <summary>
        /// Zajištění aktualizace z internetu = načte všechny dostupné soubory, bez ohledu na datum poslední aktualizace.
        /// </summary>
        DownloadUpdateAll,
        /// <summary>
        /// Zajištění aktualizace z internetu = prověření data posledního načtení jednotlivých datových sad a zadání akcí typu <see cref="DownloadUrl"/> pro jednotlivé URL adresy
        /// </summary>
        DownloadUpdateNew,
        /// <summary>
        /// Zahájení stahování jednoho souboru z jedné URL adresy. Pozor, tato akce proběhne asynchronně (=vlastnost komponenty),
        /// tedy následující akce ve frontě nemohou být prováděny po doběhnutí této akce, ale pokračování zpracování fronty je restartováno v události DownloadComplete.
        /// Akce <see cref="DownloadUrl"/> tedy po nastartování downloadu insertuje do fronty (na první pozici) akci navazující = <see cref="DownloadDone"/>.
        /// </summary>
        DownloadUrl,
        /// <summary>
        /// Akce prováděná po úspěšném dokončení downloadu
        /// </summary>
        DownloadDone,
        /// <summary>
        /// Uložit jeden soubor
        /// </summary>
        SaveFile
    }
    public class ProcessFileInfo
    {
        public ProcessFileInfo(DataMediumType medium, string fileName)
        {
            DateTime now = DateTime.Now;
            Medium = medium;
            FileName = fileName;
            ContentType = FileContentType.None;
            CurrentDate = now.Date;
            StartTime = now;
            LastProgressTime = DateTime.MinValue;
            DoneTime = DateTime.MinValue;
            Length = 0L;
            ProcessState = ProcessFileState.None;
            Position = 0L;
            RecordCount = 0;
        }
        public DataMediumType Medium { get; set; }
        public string FileName { get; set; }
        /// <summary>
        /// Dnešní datum bez času. Načítaná data musí mít datum menší, protože dnešní data nejsou kompletní a nebudeme s nimi pracovat.
        /// </summary>
        public DateTime CurrentDate { get; private set; }
        public Action<ProgressArgs> ProgressAction { get; set; }
        public FileContentType ContentType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastProgressTime { get; set; }
        public DateTime DoneTime { get; set; }
        public long Length { get; set; }
        public ProcessFileState ProcessState { get; set; }
        public long Position { get; set; }
        public decimal Ratio { get { return (Position > 0L && Length > 0L ? ((decimal)Position / (decimal)Length) : 0m); } }
        public int RecordCount { get; set; }
        public TimeSpan ProcessTime { get { return (DoneTime < StartTime ? TimeSpan.Zero : (DoneTime - StartTime)); } }
        public ProcessFileCurrentInfo CurrentInfo { get; set; }
        public string Description
        {
            get
            {
                string description = "";
                switch (this.ProcessState)
                {
                    case ProcessFileState.Loading:
                        switch (this.Medium)
                        {
                            case DataMediumType.LocalFile:
                                description = "Načítám soubor, obsahující ";
                                break;
                            case DataMediumType.BinaryContent:
                                description = "Zpracovávám data, obsahující ";
                                break;
                            case DataMediumType.WebUrl:
                                description = "Stahuji soubor, obsahující ";
                                break;
                            default:
                                description = "Zpracovávám data, obsahující ";
                                break;
                        }
                        break;
                    case ProcessFileState.Loaded:
                        switch (this.Medium)
                        {
                            case DataMediumType.LocalFile:
                                description = "Načten soubor, obsahující ";
                                break;
                            case DataMediumType.BinaryContent:
                                description = "Zpracována data, obsahující ";
                                break;
                            case DataMediumType.WebUrl:
                                description = "Stahužen soubor, obsahující ";
                                break;
                            default:
                                description = "Zpracována data, obsahující ";
                                break;
                        }
                        break;
                    case ProcessFileState.Saving:
                        description = "Ukládám soubor, obsahující ";
                        break;
                    case ProcessFileState.Saved:
                        description = "Uložen soubor, obsahující ";
                        break;
                    case ProcessFileState.WebDownloading:
                        description = "Stahuji z webu soubor, obsahující ";
                        break;
                    case ProcessFileState.WebDownloaded:
                        description = "Stažen z webu soubor, obsahující ";
                        break;
                    default:
                        description = "Zpracovávám ";
                        break;
                }
                switch (this.ContentType)
                {
                    case FileContentType.Structure:
                        description += "strukturu obcí ";
                        break;
                    case FileContentType.Data:
                        description += "pracovní data ";
                        break;
                    case FileContentType.DataPack:
                        description += "kompletní data ";
                        break;
                    case FileContentType.CovidObce1:
                        description += "veřejná data verze 1 ";
                        break;
                    case FileContentType.CovidObce2:
                        description += "veřejná data verze 2 ";
                        break;
                    case FileContentType.CovidObce3:
                        description += "veřejná data verze 3 ";
                        break;
                    case FileContentType.PocetObyvatel:
                        description += "počty obyvatel ";
                        break;
                    case FileContentType.Umrti:
                        description += "počty zemřelých ";
                        break;
                    default:
                        description += "soubor typu " + this.ContentType.ToString();
                        break;
                }
                return description.Trim();
            }
        }
    }
    /// <summary>
    /// Zdrojové médium s daty
    /// </summary>
    public enum DataMediumType
    {
        None,
        BinaryContent,
        LocalFile,
        WebUrl
    }
    public class ProcessFileCurrentInfo
    {
        public DatabaseInfo.EntityInfo World { get; set; }
        public DatabaseInfo.EntityInfo Zeme { get; set; }
        public DatabaseInfo.EntityInfo Kraj { get; set; }
        public DatabaseInfo.EntityInfo Okres { get; set; }
        public DatabaseInfo.EntityInfo Mesto { get; set; }
        public DatabaseInfo.EntityInfo Obec { get; set; }
        public DatabaseInfo.EntityInfo Vesnice { get; set; }
        public DatabaseInfo.Pocet Pocet { get; set; }
        public DatabaseInfo.DataInfo Info { get; set; }
    }
    public class ProgressArgs
    {
        public ProgressArgs(ProcessFileInfo processInfo, bool isDone)
        {
            this.ProcessInfo = processInfo;
            this.IsDone = isDone;
        }
        protected ProcessFileInfo ProcessInfo { get; private set; }
        public string Description { get { return ProcessInfo.Description; } }
        public long DataLength { get { return ProcessInfo.Length; } }
        public long DataPosition { get { return ProcessInfo.Position; } }
        public decimal Ratio { get { return ProcessInfo.Ratio; } }
        public FileContentType ContentType { get { return ProcessInfo.ContentType; } }
        public ProcessFileState ProcessState { get { return ProcessInfo.ProcessState; } }
        public TimeSpan ProcessTime { get { return ProcessInfo.ProcessTime; } }
        public int RecordCount { get { return ProcessInfo.RecordCount; } }
        public bool IsDone { get; private set; }
        /// <summary>
        /// Obsahuje true tehdy, když obsah <see cref="ContentType"/> reprezentuje jakákoli data k zobrazení.
        /// Pokud je dokončen nějaký proces v databázi
        /// </summary>
        public bool ContentHasData
        {
            get
            {
                var contentType = this.ContentType;
                return (contentType == FileContentType.Data || contentType == FileContentType.DataPack || contentType == FileContentType.CovidObce1 || contentType == FileContentType.CovidObce2 || contentType == FileContentType.CovidObce3 || contentType == FileContentType.Umrti);
            }
        }
    }
    /// <summary>
    /// Obsah souboru
    /// </summary>
    public enum FileContentType
    {
        None,
        Structure,
        Data,
        DataPack,
        PocetObyvatel,
        CovidObce1,
        CovidObce2,
        CovidObce3,
        Umrti
    }
    /// <summary>
    /// Stav zpracování souboru
    /// </summary>
    public enum ProcessFileState
    {
        None,
        Open,
        Loading,
        Loaded,
        Saving,
        Saved,
        WebDownloading,
        WebDownloaded,
        Invalid
    }
    public enum SaveFormat
    {
        Csv,
        Pack,
        Zip
    }
    /// <summary>
    /// Režim zálohování při ukládání
    /// </summary>
    public enum BackupMode
    {
        /// <summary>
        /// Nezadáno, použije se <see cref="Overwrite"/>
        /// </summary>
        None,
        /// <summary>
        /// Vždy přepsat starší soubor
        /// </summary>
        Overwrite,
        /// <summary>
        /// Zachovat jeden soubor za jeden den: pokud cílový soubor existuje, pak zajistíme že dostane suffix podle jeho dne
        /// </summary>
        OneForDay,
        /// <summary>
        /// Vždy zálohovat (i více souborů za jeden den)
        /// </summary>
        AllFiles
    }
    #endregion
    #region DataVisualInfo
    /// <summary>
    /// Data pro vizualizaci objektu
    /// </summary>
    public class DataVisualInfo
    {
        protected DataVisualInfo(object value)
        {
            this.Value = value;
        }
        public DataVisualInfo(object value, string text, string toolTip = null, DW.Image icon = null, object tag = null)
        {
            this.Value = value;
            this.Text = text;
            this.ToolTip = toolTip;
            this.Icon = icon;
            this.Tag = tag;
        }
        public override string ToString()
        {
            return this.Text;
        }
        public object Value { get; protected set; }
        public string Text { get; protected set; }
        public string ToolTip { get; protected set; }
        public DW.Image Icon { get; protected set; }
        public object Tag { get; protected set; }
    }
    #endregion
}
