using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO = System.IO;

namespace Djs.Tools.CovidGraphs.Data
{
    public class Database
    {
        #region Tvorba databáze, načtení a uložení dat
        #region Konstruktor, základní proměnné pro data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="file"></param>
        public Database()
        {
            this.Init();
        }
        private object InterLock;
        private World _World; // return new DataVisualInfo(valueType, "", "", null, GraphSerieAxisType.BigValuesLinear);;
        // private Zeme xxx_Zeme;
        private Dictionary<string, Vesnice> _Vesnice;
        private Dictionary<string, Pocet> _Pocet;
        private DateTime? _DataContentTime;
        private bool _HasData;
        protected void Init()
        {
            State = StateType.Initializing;
            InterLock = new object();
            _World = new World(this);
            _Vesnice = new Dictionary<string, Vesnice>();
            _Pocet = new Dictionary<string, Pocet>();
            _DataContentTime = null;
            _CovidInfo = null;
            _PocetInfo = null;
            State = StateType.Empty;
        }
        /// <summary>
        /// Stav databáze
        /// </summary>
        public StateType State { get; private set; }
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
            ClearData(FileContentType.Structure);
            ClearData(FileContentType.Data);
            ClearData(FileContentType.DataPack);
            ClearData(FileContentType.CovidObce1);
            ClearData(FileContentType.PocetObyvatel);
            State = StateType.Empty;
        }
        private void ClearData(FileContentType contentType)
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
                    _HasData = false;
                    break;
                case FileContentType.DataPack:
                    _DataPackLoadInfo = null;
                    _DataPackSaveInfo = null;
                    _World.Clear(FileContentType.DataPack);
                    _Vesnice.Clear();
                    _Pocet.Clear();
                    _DataContentTime = null;
                    _HasData = false;
                    break;
                case FileContentType.CovidObce1:
                case FileContentType.CovidObce2:
                    _CovidInfo = null;
                    _World.Clear(FileContentType.Data);
                    _HasData = false;
                    break;
                case FileContentType.PocetObyvatel:
                    _PocetInfo = null;
                    _Pocet.Clear();
                    _HasData = false;
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
                    isData = true;
                    _CovidInfo = processInfo;
                    break;
                case FileContentType.PocetObyvatel:
                    _PocetInfo = processInfo;
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
        public void LoadStandardDataAsync(Action<ProgressArgs> progress = null)
        {
            ThreadManager.AddAction(() => _LoadStandardData(progress));
        }
        /// <summary>
        /// Prvotní načtení dat, ze standardních souborů
        /// </summary>
        /// <param name="progress"></param>
        public void LoadStandardData(Action<ProgressArgs> progress = null)
        {
            App.TryRun(() => _LoadStandardData(progress));
        }
        /// <summary>
        /// Provede standardní načtení dat, včetně WebUpdate
        /// </summary>
        /// <param name="progress"></param>
        private void _LoadStandardData(Action<ProgressArgs> progress = null)
        {
            _LoadStandardDataFile(progress);
            _LoadStandardDataWebUpdate(progress);
        }
        /// <summary>
        /// Provede standardní načtení dat ze souborů, bez webové aktualizace
        /// </summary>
        /// <param name="progress"></param>
        private void _LoadStandardDataFile(Action<ProgressArgs> progress = null)
        {
            this.Clear();

            string appDataPath = IO.Path.Combine(App.AppPath, "Data");
            string usrDataPath = App.ConfigPath;

            // 1. Standardní varianta: Struktura + Data:
            string structureFile = SearchFile(StandardStructureFileName, usrDataPath, appDataPath);
            string dataFile = SearchFile(StandardDataFileName, usrDataPath, appDataPath);
            if (structureFile != null && dataFile != null)
            {
                Load(structureFile, progress);
                Load(dataFile, progress);
                return;
            }

            // 2. Záložní varianta: DataPack (vše v jednom), typicky první poinstalační spuštění:
            string dataPackFile = SearchFile(StandardDataPackFileName, usrDataPath, appDataPath);
            if (dataPackFile != null)
            {
                Load(dataPackFile, progress);
                _SaveStandardData(FileContentType.Structure, true, progress);
                _SaveStandardData(FileContentType.Data, true, progress);
                return;
            }

            // 3. Z plných webových dat (počet obyvatel + první verze Obce, obsahující kompletní strukturu):
            string pocetFile = SearchFile(StandardWebPocetFileName, usrDataPath, appDataPath);
            string webObce1File = SearchFile(StandardWebObce1FileName, usrDataPath, appDataPath);
            if (dataPackFile != null && webObce1File != null)
            {
                Load(pocetFile, progress);
                Load(webObce1File, progress);
                _SaveStandardData(FileContentType.Structure, true, progress);
                _SaveStandardData(FileContentType.Data, true, progress);
                return;
            }

            // 4. Z kombinovaných dat (struktura obcí + druhá verze Obce, obsahující NE-kompletní strukturu):
            string webObce2File = SearchFile(StandardWebObce2FileName, usrDataPath, appDataPath);
            if (structureFile != null && webObce2File != null)
            {
                Load(structureFile, progress);
                Load(webObce2File, progress);
                _SaveStandardData(FileContentType.Structure, true, progress);
                _SaveStandardData(FileContentType.Data, true, progress);
                return;
            }
        }
        /// <summary>
        /// Zajistí aktualizaci dat z internetu, pokud je vhodná
        /// </summary>
        /// <param name="progress"></param>
        private void _LoadStandardDataWebUpdate(Action<ProgressArgs> progress = null)
        {
            if (this.WebUpdateIsNeed)
                this.WebUpdateAsync(progress);
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
            App.TryRun(() => _LoadFile(file, progress));
            State = StateType.Ready;
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
            App.TryRun(() => _LoadContent(content, progress));
        }
        /// <summary>
        /// Načte obsah daného souboru, detekuje a zpracuje jej
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        private void _LoadFile(string file, Action<ProgressArgs> progress = null)
        {
            if (String.IsNullOrEmpty(file)) throw new ArgumentException($"Database.Import() : není zadán vstupní soubor.");
            if (!IO.File.Exists(file)) throw new ArgumentException($"Database.Import() : zadaný vstupní soubor {file} neexistuje.");
            ProcessFileInfo loadInfo = new ProcessFileInfo(file);
            loadInfo.ProgressAction = progress;
            using (var stream = new IO.StreamReader(file, Encoding.UTF8))
            {
                _LoadStream(stream, loadInfo);
                stream.Close();
            }
        }
        /// <summary>
        /// Načte obsah daného souboru, detekuje a zpracuje jej
        /// </summary>
        /// <param name="content"></param>
        /// <param name="progress"></param>
        private void _LoadContent(byte[] content, Action<ProgressArgs> progress = null)
        {
            if (content == null || content.Length == 0) throw new ArgumentException($"Database.Load() : není zadán vstupní obsah dat.");
            ProcessFileInfo loadInfo = new ProcessFileInfo("Content");
            loadInfo.ProgressAction = progress;
            using (var memoryStream = new IO.MemoryStream(content))
            using (var stream = new IO.StreamReader(memoryStream, Encoding.UTF8))
            {
                _LoadStream(stream, loadInfo);
                stream.Close();
            }
        }
        /// <summary>
        /// Načte data dodané v daném streamu (ten může pocházet ze souboru, ze zipu, z paměti...)
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="loadInfo"></param>
        private void _LoadStream(IO.StreamReader stream, ProcessFileInfo loadInfo)
        {
            lock (this.InterLock)
            {
                State = StateType.LoadingFile;
                loadInfo.ProcessState = ProcessFileState.Open;
                loadInfo.Length = stream.BaseStream.Length;
                while (!stream.EndOfStream)
                {
                    string line = stream.ReadLine();
                    _LoadLine(line, loadInfo);
                    _CallProgress(loadInfo, stream: stream);
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
        private void _LoadLine(string line, ProcessFileInfo processInfo)
        {
            if (String.IsNullOrEmpty(line)) return;
            line = line.Trim();
            switch (processInfo.ProcessState)
            {
                case ProcessFileState.Open:
                    if (line.StartsWith(StructureHeaderExpected, StringComparison.CurrentCultureIgnoreCase))
                        processInfo.ContentType = FileContentType.Structure;
                    else if (line.StartsWith(DataHeaderExpected, StringComparison.CurrentCultureIgnoreCase))
                        processInfo.ContentType = FileContentType.Data;
                    else if(line.StartsWith(DataPackHeaderExpected, StringComparison.CurrentCultureIgnoreCase))
                        processInfo.ContentType = FileContentType.DataPack;
                    else if (String.Equals(line, Covid1HeaderExpected, StringComparison.CurrentCultureIgnoreCase))
                        processInfo.ContentType = FileContentType.CovidObce1;
                    else if (String.Equals(line, Covid2HeaderExpected, StringComparison.CurrentCultureIgnoreCase))
                        processInfo.ContentType = FileContentType.CovidObce2;
                    else if (String.Equals(line, PocetHeaderExpected, StringComparison.CurrentCultureIgnoreCase))
                        processInfo.ContentType = FileContentType.PocetObyvatel;
                    else
                        throw new FormatException($"Database.Load() : zadaný vstupní soubor {processInfo.FileName} nemá odpovídající záhlaví (úvodní řádek).");
                    this._CheckDataContent(processInfo);
                    processInfo.ProcessState = ProcessFileState.Loading;
                    this.ClearData(processInfo.ContentType);
                    break;

                case ProcessFileState.Loading:
                    switch (processInfo.ContentType)
                    {
                        case FileContentType.Structure:
                            _LoadLineStructure(processInfo, line);
                            break;
                        case FileContentType.Data:
                            _LoadLineData(processInfo, line);
                            break;
                        case FileContentType.DataPack:
                            _LoadLineDataPack(processInfo, line);
                            break;
                        case FileContentType.CovidObce1:
                            _LoadLineCovid1(processInfo, line);
                            break;
                        case FileContentType.CovidObce2:
                            _LoadLineCovid2(processInfo, line);
                            break;
                        case FileContentType.PocetObyvatel:
                            _LoadLinePocet(processInfo, line);
                            break;
                    }
                    break;
            }
        }
        /// <summary>
        /// Metoda prověří stav databáze před načítáním obsahu daného souboru
        /// </summary>
        /// <param name="processInfo"></param>
        private void _CheckDataContent(ProcessFileInfo processInfo)
        {
            switch (processInfo.ContentType)
            {
                case FileContentType.CovidObce2:
                    if (this._Vesnice == null || this._Vesnice.Count == 0)
                    {
                        throw new InvalidOperationException($"Nelze načítat data typu {FileContentType.CovidObce2} do databáze, která nemá načtenou strukturu obcí.");
                    }
                    break;
            }
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.Structure"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineStructure(ProcessFileInfo loadInfo, string line)
        {
            _LoadLineDataPack(loadInfo, line);
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.Data"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineData(ProcessFileInfo loadInfo, string line)
        {
            string[] items = line.Split(';');
            if (items.Length < 3) return;

            if (loadInfo.CurrentInfo == null) loadInfo.CurrentInfo = new ProcessFileCurrentInfo();

            string header = items[0];
            switch (header)
            {
                case HeaderVesnice:
                    string vesniceKod = items[1];
                    string vesniceNazev = items[2];
                    bool hasVesnice = (this._Vesnice.TryGetValue(vesniceKod, out var vesnice));
                    loadInfo.CurrentInfo.Vesnice = (hasVesnice ? vesnice : null);
                    break;
                case HeaderInfo:
                    if (loadInfo.CurrentInfo.Vesnice != null)
                    {
                        DateTime infoDate = GetDate(items[1]);
                        int infoNewCount = GetInt32(items[2]);
                        int infoCurrentCount = GetInt32(items[3]);
                        int infoKey = infoDate.GetDateKey();
                        loadInfo.CurrentInfo.Info = loadInfo.CurrentInfo.Vesnice.Childs.AddOrCreate(infoKey, () => new Info(loadInfo.CurrentInfo.Vesnice, infoDate, infoNewCount, infoCurrentCount));
                        _RegisterMaxContentTime(infoDate);
                    }
                    break;
            }

            loadInfo.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.DataPack"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineDataPack(ProcessFileInfo loadInfo, string line)
        {
            string[] items = line.Split(';');
            if (items.Length < 3) return;

            if (loadInfo.CurrentInfo == null)
            {
                loadInfo.CurrentInfo = new ProcessFileCurrentInfo();
                loadInfo.CurrentInfo.World = _World;
            }

            string header = items[0];
            string code = items[1];
            string name = items[2];

            switch (header)
            {
                case HeaderZeme:
                    string zemeKod = code;
                    string zemeNazev = name;
                    loadInfo.CurrentInfo.Zeme = loadInfo.CurrentInfo.World.Childs.AddOrCreate(zemeKod, () => new Zeme(loadInfo.CurrentInfo.World, zemeKod, zemeNazev));
                    break;
                case HeaderKraj:
                    string krajKod = code;
                    string krajNazev = name;
                    loadInfo.CurrentInfo.Kraj = loadInfo.CurrentInfo.Zeme.Childs.AddOrCreate(krajKod, () => new Kraj(loadInfo.CurrentInfo.Zeme, krajKod, krajNazev));
                    break;
                case HeaderOkres:
                    string okresKod = code;
                    string okresNazev = name;
                    loadInfo.CurrentInfo.Okres = loadInfo.CurrentInfo.Kraj.Childs.AddOrCreate(okresKod, () => new Okres(loadInfo.CurrentInfo.Kraj, okresKod, okresNazev));
                    break;
                case HeaderMesto:
                    string mestoKod = code;
                    string mestoNazev = name;
                    loadInfo.CurrentInfo.Mesto = loadInfo.CurrentInfo.Okres.Childs.AddOrCreate(mestoKod, () => new Mesto(loadInfo.CurrentInfo.Okres, mestoKod, mestoNazev));
                    break;
                case HeaderObec:
                    string obecKod = code;
                    string obecNazev = name;
                    loadInfo.CurrentInfo.Obec = loadInfo.CurrentInfo.Mesto.Childs.AddOrCreate(obecKod, () => new Obec(loadInfo.CurrentInfo.Mesto, obecKod, obecNazev));
                    break;
                case HeaderVesnice:
                    string vesniceKod = code;
                    string vesniceNazev = name;
                    loadInfo.CurrentInfo.Vesnice = loadInfo.CurrentInfo.Obec.Childs.AddOrCreate(vesniceKod, () => new Vesnice(loadInfo.CurrentInfo.Obec, vesniceKod, vesniceNazev));
                    this._Vesnice.AddIfNotContains(vesniceKod, loadInfo.CurrentInfo.Vesnice);
                    break;
                case HeaderPocet:
                    // P;554979;Abertamy;Ostrov;Karlovarský;458;412;422;368
                    string pocetKod = code;
                    string pocetVesnice = name;
                    string pocetMesto = items[3];
                    string pocetKraj = items[4];
                    int pocetMC = GetInt32(items[5]);
                    int pocetMS = GetInt32(items[6]);
                    int pocetFC = GetInt32(items[7]);
                    int pocetFS = GetInt32(items[8]);
                    Pocet pocet = new Pocet(this, pocetKod, pocetVesnice, pocetMesto, pocetKraj, pocetMC, pocetMS, pocetFC, pocetFS);
                    this._Pocet.AddOrUpdate(pocetKod, pocet);
                    break;
                case HeaderInfo:
                    DateTime infoDate = GetDate(items[1]);
                    int infoNewCount = GetInt32(items[2]);
                    int infoCurrentCount = GetInt32(items[3]);
                    int infoKey = infoDate.GetDateKey();
                    loadInfo.CurrentInfo.Info = loadInfo.CurrentInfo.Vesnice.Childs.AddOrCreate(infoKey, () => new Info(loadInfo.CurrentInfo.Vesnice, infoDate, infoNewCount, infoCurrentCount));
                    _RegisterMaxContentTime(infoDate);
                    break;
            }

            loadInfo.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.CovidObce1"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineCovid1(ProcessFileInfo loadInfo, string line)
        {
            // Verze 1 obsahuje 5 úrovní: Kraj - Okres - Město - Obec - Vesnice
            // Příklad řádku se všemi úrovněmi:
            // čtvrtek;2020-08-20;CZ032;Plzeňský kraj;CZ0327;Tachov;3213;Stříbro;32131;Bezdružice;541290;Horní Kozolupy;0;0
            string[] items = line.Split(';');
            if (items.Length != Covid1ItemCountExpected) return;

            World world = this._World;

            string zemeKod = "CZ";
            string zemeNazev = "Česká republika";
            Zeme zeme = world.Childs.AddOrCreate(zemeKod, () => new Zeme(world, zemeKod, zemeNazev));

            string krajKod = items[2];
            string krajNazev = items[3];
            Kraj kraj = zeme.Childs.AddOrCreate(krajKod, () => new Kraj(zeme, krajKod, krajNazev));

            string okresKod = items[4];
            string okresNazev = items[5];
            Okres okres = kraj.Childs.AddOrCreate(okresKod, () => new Okres(kraj, okresKod, okresNazev));

            string mestoKod = items[6];
            string mestoNazev = items[7];
            Mesto mesto = okres.Childs.AddOrCreate(mestoKod, () => new Mesto(okres, mestoKod, mestoNazev));

            string obecKod = items[8];
            string obecNazev = items[9];
            Obec obec = mesto.Childs.AddOrCreate(obecKod, () => new Obec(mesto, obecKod, obecNazev));

            string vesniceKod = items[10];
            string vesniceNazev = items[11];
            Vesnice vesnice = obec.Childs.AddOrCreate(vesniceKod, () => new Vesnice(obec, vesniceKod, vesniceNazev));

            this._Vesnice.AddIfNotContains(vesniceKod, vesnice);

            DateTime infoDate = GetDate(items[1]);
            int newCount = GetInt32(items[12]);
            int currentCount = GetInt32(items[13]);
            int key = infoDate.GetDateKey();
            Info info = vesnice.Childs.AddOrCreate(key, () => new Info(vesnice, infoDate, newCount, currentCount));
            _RegisterMaxContentTime(infoDate);

            loadInfo.RecordCount += 1;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.CovidObce2"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLineCovid2(ProcessFileInfo loadInfo, string line)
        {
            // Verze 2 obsahuje 4 úrovně: Kraj - Okres - Město - Obec - Vesnice
            // Příklad řádku se všemi úrovněmi pro verzi 1:
            // neděle;2020-03-01;CZ053;Pardubický kraj;CZ0531;Chrudim;5304;Chrudim;53043;Chrudim;571164;Chrudim;0;0
            // Tentýž řádek ve verzi 2:
            // neděle,2020-03-01,CZ053,"Pardubický kraj",CZ0531,Chrudim,5304,Chrudim,            571164,Chrudim,0,0

            // Abychom měli data v paměti umístěná ve správné struktuře (plných 5 úrovní, a nikoli jedna z nich Void), 
            // tak nebudeme z tohoto souboru načítat kompoetní strukturu obcí = tato musí být načtena dříve,
            // a zde budeme načítat pouze kód Vesnice, tu dohledáme, a do ní vepíšeme data Info:
            line = line.Replace("\"", "");
            string[] items = line.Split(',');
            if (items.Length != Covid2ItemCountExpected) return;

            string krajKod = items[2];           // CZ053
            string krajNazev = items[3];
            string okresKod = items[4];          // CZ0531
            string okresNazev = items[5];
            string mestoKod = items[6];          // 5304
            string mestoNazev = items[7];
            string obecKod = VoidEntityCode;     // zde není 53043
            string obecNazev = null;
            string vesniceKod = items[8];        // 571164
            string vesniceNazev = items[9];
            if (!this._Vesnice.TryGetValue(vesniceKod, out Vesnice vesnice))
            {   // V datech je kód 5. úrovně "Vesnice", ale my jej nemáme ve struktuře:
                throw new KeyNotFoundException($"Ve vstupních datech je uvedena obec {vesniceKod}: {vesniceNazev}, patřící do města {mestoNazev} (okres {okresNazev}), ale tuto obec nemáme načtenou ve struktuře obcí.");
            }

            DateTime infoDate = GetDate(items[1]);
            int newCount = GetInt32(items[10]);
            int currentCount = GetInt32(items[11]);
            int key = infoDate.GetDateKey();
            Info info = vesnice.Childs.AddOrCreate(key, () => new Info(vesnice, infoDate, newCount, currentCount));
            _RegisterMaxContentTime(infoDate);

            loadInfo.RecordCount += 1;
        }
        /// <summary>
        /// Zaeviduje maximální datum s daty
        /// </summary>
        /// <param name="infoDate"></param>
        private void _RegisterMaxContentTime(DateTime infoDate)
        {
            if (!_DataContentTime.HasValue || infoDate > _DataContentTime.Value)
                _DataContentTime = infoDate;
        }
        /// <summary>
        /// Načte řádek dat ve struktuře <see cref="FileContentType.PocetObyvatel"/>
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="line"></param>
        private void _LoadLinePocet(ProcessFileInfo loadInfo, string line)
        {
            // ;kraj;mesto;kod_obce;nazev_obce;muzi;muzi15;zeny;zeny15;celkem;celkem15
            // ;Pardubický;Chrudim;571164;Chrudim;10 899;9 192;11 720;10 164;22 619;19 356

            string[] items = line.Split(';');
            if (items.Length != PocetItemCountExpected) return;

            string kraj = items[1];
            string mesto = items[2];
            string kod = items[3];
            string vesnice = items[4];
            int pocetMC = GetInt32(items[5]);
            int pocetMS = GetInt32(items[6]);
            int pocetFC = GetInt32(items[7]);
            int pocetFS = GetInt32(items[8]);

            Pocet pocet = new Pocet(this, kod, vesnice, mesto, kraj, pocetMC, pocetMS, pocetFC, pocetFS);
            this._Pocet.AddOrUpdate(kod, pocet);

            loadInfo.RecordCount += 1;
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
        #region WebUpdate : aktualizace dat z internetu
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
                return (now.TimeOfDay >= StandardWebUpdateTime);                    // Data jsou právě včerejší: nová stáhneme jen tehdy, když je aktuální čas větší než čas, kdy se data na webu aktualizují
            }
        }
        /// <summary>
        /// Provede aktualizaci dat z internetu. Asynchronní metoda, vrátí řízení ihned, v průběhu downloadu a po jeho dokončení se volá daná akce Progress.
        /// </summary>
        /// <param name="progress"></param>
        public void WebUpdateAsync(Action<ProgressArgs> progress = null)
        {
            App.TryRun(() => _WebUpdateAsync(progress));
        }
        /// <summary>
        /// Provede aktualizaci z internetu
        /// </summary>
        /// <param name="progress"></param>
        private void _WebUpdateAsync(Action<ProgressArgs> progress = null)
        {
            string url = StandardWebObce2UpdateUrl;
            ProcessFileInfo updateInfo = new ProcessFileInfo(url);
            updateInfo.ProgressAction = progress;
            updateInfo.ContentType = FileContentType.CovidObce2;
            updateInfo.ProcessState = ProcessFileState.WebDownloading;

            State = StateType.Downloading;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                wc.DownloadProgressChanged += _WebUpdate_DownloadProgressChanged;
                wc.DownloadDataCompleted += _WebUpdate_DownloadDataCompleted;
                Uri uri = new Uri(url);
                wc.DownloadDataAsync(uri, updateInfo);
            }
        }
        /// <summary>
        /// Progress v Downloadu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _WebUpdate_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            // Sem přijde řízení po každých cca 16 kB
            ProcessFileInfo updateInfo = e.UserState as ProcessFileInfo;

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
        private void _WebUpdate_DownloadDataCompleted(object sender, System.Net.DownloadDataCompletedEventArgs e)
        {   // Sem přijde řízení po jakémkoli způsobu dokončení přenosu (OK, error, cancel)
            if (e.Cancelled) return;
            if (e.Error != null)
            {
                Data.App.ShowError(e.Error.Message);
                return;
            }

            ProcessFileInfo updateInfo = e.UserState as ProcessFileInfo;
            if (updateInfo == null)
                throw new InvalidOperationException($"Po dokončení downloadu dat nelze provést zpracování, není předán 'ProcessFileInfo updateInfo'.");

            this.StoreProcessFileResults(updateInfo);
            _CallProgress(updateInfo, force: true, isDone: true);

            // Zdejší metoda běží v Main threadu (to je dané WebClientem), ale my chceme zpracování dat provést v Background threadu, kvůli GUI:
            var content = e.Result;
            ThreadManager.AddAction(() => _WebUpdateCompletedProcessData(content, updateInfo.ProgressAction));
        }
        private void _WebUpdateCompletedProcessData(byte[] content, Action<ProgressArgs> progress = null)
        {
            this.Load(content, progress);
            this.SaveStandardData(false, progress);
        }
        #endregion
        #region Save : ukládání do interního formátu
        public void SaveStandardData(bool withStructure, Action<ProgressArgs> progress = null)
        {
            State = StateType.SavingFile;
            if (withStructure)
                App.TryRun(() => _SaveStandardData(FileContentType.Structure, true, progress));
            App.TryRun(() => _SaveStandardData(FileContentType.Data, true, progress));
            State = StateType.Ready;
        }
        public void SaveDataPackData(Action<ProgressArgs> progress = null)
        {
            App.TryRun(() => _SaveStandardData(FileContentType.DataPack, true, progress));
        }
        private void _SaveStandardData(FileContentType contentType, bool packed, Action<ProgressArgs> progress = null)
        {
            packed = false;                // Zatím neumíme
            string file = GetSaveFileName(contentType, packed);
            _Save(file, contentType, progress);
        }
        protected string GetSaveFileName(FileContentType contentType, bool packed)
        {
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
            if (packed)
                name = IO.Path.ChangeExtension(name, StandardPackExtension);
            return IO.Path.Combine(DataPathLocal, name);
        }
        /// <summary>
        /// Uloží data do souboru v interním formátu
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        public void Save(string file, FileContentType contentType, Action<ProgressArgs> progress = null)
        {
            App.TryRun(() => _Save(file, contentType, progress));
        }
        private void _Save(string file, FileContentType contentType, Action<ProgressArgs> progress = null)
        {
            if (!(contentType == FileContentType.Structure || contentType == FileContentType.Data || contentType == FileContentType.DataPack))
                throw new ArgumentException($"Databáze může ukládat pouze data typu {FileContentType.Structure}, {FileContentType.Data} nebo {FileContentType.DataPack}. Nelze uložit data typu {contentType}.");

            lock (this.InterLock)
            {
                State = StateType.SavingFile;
                ProcessFileInfo saveInfo = new ProcessFileInfo(file);
                saveInfo.ProgressAction = progress;
                saveInfo.ContentType = contentType;
                saveInfo.ProcessState = ProcessFileState.Saving;
                using (var stream = new IO.StreamWriter(file, false, Encoding.UTF8))
                {
                    _SaveFileHeader(saveInfo, stream);
                    _SaveFileProperties(saveInfo, stream);
                    _World.Save(saveInfo, stream);
                }
                this.StoreProcessFileResults(saveInfo);
                _CallProgress(saveInfo, force: true, isDone: true);
                State = StateType.Ready;
            }
        }
        /// <summary>
        /// Do streamu zapíše řádek popisující záhlaví souboru daného typu.
        /// Podle záhlaví poté bude soubor identifikován.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private void _SaveFileHeader(ProcessFileInfo saveInfo, IO.StreamWriter stream)
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
        private void _SaveFileProperties(ProcessFileInfo saveInfo, IO.StreamWriter stream)
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
        #endregion
        #region Privátní podpora: adresáře, jména standardních souborů, hlavičkové konstanty v souborech, konverzní metody
        /// <summary>
        /// Zajistí zobrazení progresu
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="stream"></param>
        /// <param name="processInfo"></param>
        private void _CallProgress(ProcessFileInfo processInfo, bool force = false, IO.StreamReader stream = null, bool isDone = false)
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

            if (!isDone && stream != null && stream.BaseStream != null)
                processInfo.Position = stream.BaseStream.Position;

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
        /// Adresář pro data u aplikace = nezapisovat!
        /// </summary>
        protected static string DataPathApp { get { return IO.Path.Combine(App.AppPath, "Data"); } }
        /// <summary>
        /// Adresář pro data pracovaní, zde možno zapisovat
        /// </summary>
        protected static string DataPathLocal { get { return App.ConfigPath; } }

        protected const string StandardStructureFileName = "Structure.db";
        protected const string StandardDataFileName = "Data.db";
        protected const string StandardDataPackFileName = "DataPack.db";
        protected const string StandardWebObce1FileName = "WebObce1.csv";
        protected const string StandardWebObce2FileName = "WebObce2.csv";
        protected const string StandardWebPocetFileName = "WebPocet.csv";
        protected const string StandardPackExtension = ".pack";

        protected const string StandardWebObce2UpdateUrl = @"https://onemocneni-aktualne.mzcr.cz/api/v2/covid-19/obce.csv";
        /// <summary>
        /// Čas, kdy je aktualizována webová databáze. Obsahuje hodiny a minuty, typicky 8:15.
        /// </summary>
        protected static TimeSpan StandardWebUpdateTime { get { return TimeSpan.FromHours(8.25d); } }

        protected const string StructureHeaderExpected = "H;Structure;BestInCovid;V1;";
        protected const string DataHeaderExpected = "H;Data;BestInCovid;V1;";
        protected const string DataPackHeaderExpected = "H;BestInCovid;V1;";
        protected const string Covid1HeaderExpected = "den;datum;kraj_kod;kraj_nazev;okres_kod;okres_nazev;orp_kod;orp_nazev;opou_kod;opou_nazev;obec_kod;obec_nazev;nove_pripady;aktualne_nemocnych";
        protected const int Covid1ItemCountExpected = 14;
        protected const string Covid2HeaderExpected = "den,datum,kraj_nuts_kod,kraj_nazev,okres_lau_kod,okres_nazev,orp_kod,orp_nazev,obec_kod,obec_nazev,nove_pripady,aktivni_pripady";
        protected const int Covid2ItemCountExpected = 12;
        protected const string PocetHeaderExpected = ";kraj;mesto;kod_obce;nazev_obce;muzi;muzi15;zeny;zeny15;celkem;celkem15";
        protected const int PocetItemCountExpected = 11;
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
        #endregion
        #region Získání dat za určitou úroveň
        public ResultInfo[] GetResult(string fullCode, DataValueType valueType, DateTime? begin = null, DateTime? end = null)
        {
            return _GetResult(fullCode, null, valueType, begin, end);
        }
        public ResultInfo[] GetResult(IEntity entity, DataValueType valueType, DateTime? begin = null, DateTime? end = null)
        {
            return _GetResult(null, entity, valueType, begin, end);
        }
        private ResultInfo[] _GetResult(string fullCode, IEntity entity, DataValueType valueType, DateTime? begin = null, DateTime? end = null)
        {
            ResultInfo[] results = null;
            lock (this.InterLock)
            {
                if (entity == null && fullCode != null)
                    entity = GetEntity(fullCode);

                if (entity != null)
                {
                    ResultArgs args = new ResultArgs(entity, valueType, begin, end);
                    entity.AddResults(args);
                    results = ProcessResultValue(args);
                }
                else
                {
                    results = new ResultInfo[0];
                }
            }
            return results;
        }
        private ResultInfo[] ProcessResultValue(ResultArgs args)
        {
            ProcessResultValueDirect(args);
            switch (args.ValueType)
            {
                case DataValueType.CurrentCount:
                case DataValueType.NewCount:
                    break;
                case DataValueType.CurrentCountAvg:
                case DataValueType.NewCountAvg:
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRound(args);
                    break;
                case DataValueType.CurrentCountRelative:
                case DataValueType.NewCountRelative:
                    ProcessResultValueRelative(args);
                    ProcessResultValueRound(args);
                    break;
                case DataValueType.CurrentCountRelativeAvg:
                case DataValueType.NewCountRelativeAvg:
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRelative(args);
                    ProcessResultValueRound(args);
                    break;
                case DataValueType.RZero:
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRZero(args);
                    ProcessResultValueRound(args, 2);
                    break;
                case DataValueType.RZeroAvg:
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRZero(args);
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRound(args, 2);
                    break;
                case DataValueType.NewCount7DaySum:
                    ProcessResultValue7DayLastSum(args);
                    ProcessResultValueRound(args);
                    break;
                case DataValueType.NewCount7DaySumAvg:
                    ProcessResultValue7DayLastSum(args);
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRound(args);
                    break;
                case DataValueType.NewCount7DaySumRelative:
                    ProcessResultValue7DayLastSum(args);
                    ProcessResultValueRelative(args);
                    ProcessResultValueRound(args);
                    break;
                case DataValueType.NewCount7DaySumRelativeAvg:
                    ProcessResultValue7DayLastSum(args);
                    ProcessResultValue7DayFlowAverage(args);
                    ProcessResultValueRelative(args);
                    ProcessResultValueRound(args);
                    break;

            }
            List<ResultInfo> resultList = new List<ResultInfo>(args.Results.Values);
            resultList.Sort((a, b) => a.Date.CompareTo(b.Date));
            return resultList.ToArray();
        }
        private void ProcessResultValueDirect(ResultArgs args)
        {
            args.Results.Values.ForEachExec(r => r.Value = r.RawValue);
        }
        private void ProcessResultValue7DayFlowAverage(ResultArgs args)
        {
            var data = args.Results;
            int[] keys = data.Keys.ToArray();
            foreach (int key in keys)
            {
                var result = data[key];
                DateTime date = result.Date.AddDays(-3d);
                DateTime end = date.AddDays(7d);
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
            args.Results.Values.ForEachExec(r => r.Value = r.TempValue);
        }
        private void ProcessResultValue7DayLastSum(ResultArgs args)
        {
            var data = args.Results;
            int[] keys = data.Keys.ToArray();
            foreach (int key in keys)
            {
                var result = data[key];
                DateTime date = result.Date.AddDays(-6d);            // První datum pro sumu do dne 18.1.2021 (pondělí) je minulé úterý 12.1.2021
                DateTime end = date.AddDays(7d);                     // End je datum, které se už počítat nebude = 12.1. + 7 = 19.1.2021
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
            // Na závěr vložím TempValue do Value:
            args.Results.Values.ForEachExec(r => r.Value = r.TempValue);
        }
        private void ProcessResultValueRelative(ResultArgs args)
        {
            decimal coefficient = (args.PocetObyvatel > 0 ? (100000m / (decimal)args.PocetObyvatel) : 0m);
            args.Results.Values.ForEachExec(r => r.Value = coefficient * r.Value);
        }
        /// <summary>
        /// Vypočítá hodnotu R0 jako poměr hodnoty Value proti Value [mínus 5 dní] a výsledky na závěr vloží do Value
        /// </summary>
        /// <param name="args"></param>
        private void ProcessResultValueRZero(ResultArgs args)
        {
            var data = args.Results;
            int[] keys = data.Keys.ToArray();
            decimal lastRZero = 0m;
            foreach (int key in keys)
            {   // Nejprve vypočtu hodnotu RZero a uložím ji do TempValue, protože hodnoty v Value průběžně potřebuji pro následující výpočty
                //  (mohl bych jít datumově od konce a rovnou hodnotu Value přepisovat, ale pak bych neměl šanci řešit chybějící dny = pomocí lastRZero):
                var result = data[key];
                DateTime date = result.Date.AddDays(-5d);
                int sourceKey = date.GetDateKey();
                if (data.TryGetValue(sourceKey, out var source) && source.Value > 0m)
                    lastRZero = result.Value / source.Value;
                result.TempValue = lastRZero;
            }
            // Na závěr vložím TempValue do Value:
            args.Results.Values.ForEachExec(r => r.Value = r.TempValue);
        }
        private void ProcessResultValueRound(ResultArgs args, int decimals = 0)
        {
            args.Results.Values.ForEachExec(r => r.Value = Math.Round(r.Value, decimals));
        }
        public IEntity[] GetEntities(string searchNazev)
        {
            List<IEntity> entitesStart = new List<IEntity>();
            List<IEntity> entitesContains = new List<IEntity>();

            if (!String.IsNullOrEmpty(searchNazev))
            {
                searchNazev = searchNazev.Trim();

                if (searchNazev.StartsWith("*") || searchNazev.StartsWith("%"))
                {
                    entitesStart = null;
                    searchNazev = searchNazev.Substring(1).Trim();
                }
                if (searchNazev.Length > 0)
                    this._World.AddEntities(searchNazev, entitesStart, entitesContains);
            }

            List<IEntity> entites = null;
            if (entitesStart != null && entitesStart.Count > 0) entites = entitesStart;
            else if (entitesContains != null && entitesContains.Count > 0) entites = entitesContains;

            if (entites == null) return new IEntity[0];

            entites.Sort((a, b) => String.Compare(a.Nazev, b.Nazev, StringComparison.CurrentCultureIgnoreCase));
            return entites.ToArray();
        }
        protected static void AddEntities(IEntity entity, string nazev, string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
        {
            if (String.IsNullOrEmpty(nazev)) return;
            if (entitesStart != null && nazev.StartsWith(search, StringComparison.CurrentCultureIgnoreCase))
                entitesStart.Add(entity);
            else if (entitesContains != null && nazev.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
                entitesContains.Add(entity);
        }
        public IEntity GetEntity(string fullCode)
        {
            if (String.IsNullOrEmpty(fullCode)) return null;
            string[] codes = fullCode.Split(EntityDelimiter[0]);
            int count = codes.Length;

            if (count < 1) return null;

            World world = this._World;
            if (TryGetChildEntity(world, world.Childs, codes[0], (count == 1), out var zeme)) return zeme;
            if (TryGetChildEntity(zeme, zeme.Childs, codes[1], (count == 2), out var kraj)) return kraj;
            if (TryGetChildEntity(kraj, kraj.Childs, codes[2], (count == 3), out var okres)) return okres;
            if (TryGetChildEntity(okres, okres.Childs, codes[3], (count == 4), out var mesto)) return mesto;
            if (TryGetChildEntity(mesto, mesto.Childs, codes[4], (count == 5), out var obec)) return obec;
            if (TryGetChildEntity(obec, obec.Childs, codes[5], (count == 6), out var vesnice)) return vesnice;

            return null;
        }
        protected bool TryGetChildEntity<TKey, TValue>(IEntity entity, Dictionary<TKey, TValue> childs, TKey key, bool isTarget, out TValue result) where TValue : class, IEntity
        {
            result = null;
            bool isEnd = isTarget;
            if (!childs.TryGetValue(key, out result))                // Pokud jsme nenašli záznam pro zadaný klíč, může to ýt proto, že máme jen jeden Void Child:
            {   // Pokud hledaný klíč nemáme:
                if (entity.ChildsIsVoid && childs.Count == 1)
                    // Tato entita má pouze jeden Void child (tzv. "průhledný" child):
                    result = childs.Values.FirstOrDefault();         // Vezmeme jeden jediný Child, to je ten správný
                else
                    // Tato entita nemá Void childs = má běžné Childs dohledatelné podle klíčů, ale nenašla požadovaný kód:
                    isEnd = true;                                    // hodnota "result" je null, a vrátíme true => hledání končí, ale nic nenalezlo
            }
            return isEnd;
        }
        /// <summary>
        /// Načte a vrátí záznam pro počet obyvatel z databáze z datové tabulky Počet obyvatel. Vstupem je vždy jednoduchý kód entity <see cref="EntityType.Vesnice"/>.
        /// </summary>
        /// <param name="kod"></param>
        /// <returns></returns>
        private Pocet GetPocet(string kod)
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
            if (!String.IsNullOrEmpty(kod) && _Pocet != null && _Pocet.TryGetValue(kod, out var pocet)) return pocet.PocetCelkem;
            return 0;
        }
        public const string EntityDelimiter = ".";
        public enum EntityType { None, World, Zeme, Kraj, Okres, Mesto, Obec, Vesnice }
        #endregion
        #region Třídy dat
        public class World : IEntity
        {
            public World(Database database)
            {
                this.Database = database;
                this.Childs = new Dictionary<string, Zeme>();
            }
            public override string ToString()
            {
                return Text;
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return false; } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.World; } }
            public string FullCode { get { return ""; } }
            public Database Database { get; private set; }
            public string Kod { get { return ""; } }
            public string Nazev { get { return "World"; } }
            public int PocetObyvatel { get { return (this.Childs.Count > 0 ? this.Childs.Values.Select(child => child.PocetObyvatel).Sum() : 0); } }
            public Dictionary<string, Zeme> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                foreach (var item in this.Childs.Values)
                    item.Clear(contentType);
                if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                if (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack)
                    stream.WriteLine($"{HeaderWorld};{Kod};{Nazev}");
                foreach (var item in this.Childs.Values)
                    item.Save(saveInfo, stream);
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
                foreach (var item in this.Childs.Values)
                    item.AddEntities(search, entitesStart, entitesContains);
            }
        }
        public class Zeme : IEntity
        {
            public Zeme(World world, string kod, string nazev)
            {
                this.Parent = world;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Childs = new Dictionary<string, Kraj>();
                Parent.ChildsIsVoid = this.IsVoid;

            }
            public override string ToString()
            {
                return Text;
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return (Kod == VoidEntityCode); } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.Kraj; } }
            public string FullCode { get { return Kod; } }
            public World Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public int PocetObyvatel { get { if (!_PocetObyvatel.HasValue) _PocetObyvatel = (this.Childs.Count > 0 ? this.Childs.Values.Select(child => child.PocetObyvatel).Sum() : 0); return _PocetObyvatel.Value; } } private int? _PocetObyvatel = null;
            public Dictionary<string, Kraj> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                foreach (var item in this.Childs.Values)
                    item.Clear(contentType);
                if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                if (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack)
                    stream.WriteLine($"{HeaderZeme};{Kod};{Nazev}");
                foreach (var item in this.Childs.Values)
                    item.Save(saveInfo, stream);
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
                foreach (var item in this.Childs.Values)
                    item.AddEntities(search, entitesStart, entitesContains);
            }
        }
        public class Kraj : IEntity
        {
            public Kraj(Zeme parent, string kod, string nazev)
            {
                this.Parent = parent;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Childs = new Dictionary<string, Okres>();
                Parent.ChildsIsVoid = this.IsVoid;
            }
            public override string ToString()
            {
                return Text;
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return (Kod == VoidEntityCode); } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.Kraj; } }
            public string FullCode { get { return Parent.FullCode + EntityDelimiter + Kod; } }
            public Zeme Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public int PocetObyvatel { get { if (!_PocetObyvatel.HasValue) _PocetObyvatel = (this.Childs.Count > 0 ? this.Childs.Values.Select(child => child.PocetObyvatel).Sum() : 0); return _PocetObyvatel.Value; } } private int? _PocetObyvatel = null;
            public Dictionary<string, Okres> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                foreach (var item in this.Childs.Values)
                    item.Clear(contentType);
                if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                if (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack)
                    stream.WriteLine($"{HeaderKraj};{Kod};{Nazev}");
                foreach (var item in this.Childs.Values)
                    item.Save(saveInfo, stream);
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
                foreach (var item in this.Childs.Values)
                    item.AddEntities(search, entitesStart, entitesContains);
            }
        }
        public class Okres : IEntity
        {
            public Okres(Kraj parent, string kod, string nazev)
            {
                this.Parent = parent;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Childs = new Dictionary<string, Mesto>();
                Parent.ChildsIsVoid = this.IsVoid;
            }
            public override string ToString()
            {
                return $"{Text}; Parent: {Parent.Text}";
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return (Kod == VoidEntityCode); } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.Okres; } }
            public string FullCode { get { return Parent.FullCode + EntityDelimiter + Kod; } }
            public Kraj Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public int PocetObyvatel { get { if (!_PocetObyvatel.HasValue) _PocetObyvatel = (this.Childs.Count > 0 ? this.Childs.Values.Select(child => child.PocetObyvatel).Sum() : 0); return _PocetObyvatel.Value; } } private int? _PocetObyvatel = null;
            public Dictionary<string, Mesto> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                foreach (var item in this.Childs.Values)
                    item.Clear(contentType);
                if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                if (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack)
                    stream.WriteLine($"{HeaderOkres};{Kod};{Nazev}");
                foreach (var item in this.Childs.Values)
                    item.Save(saveInfo, stream);
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
                foreach (var item in this.Childs.Values)
                    item.AddEntities(search, entitesStart, entitesContains);
            }
        }
        public class Mesto : IEntity
        {
            public Mesto(Okres parent, string kod, string nazev)
            {
                this.Parent = parent;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Childs = new Dictionary<string, Obec>();
                Parent.ChildsIsVoid = this.IsVoid;
            }
            public override string ToString()
            {
                return $"{Text}; Parent: {Parent.Text}";
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return (Kod == VoidEntityCode); } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.Mesto; } }
            public string FullCode { get { return Parent.FullCode + EntityDelimiter + Kod; } }
            public Okres Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public int PocetObyvatel { get { if (!_PocetObyvatel.HasValue) _PocetObyvatel = (this.Childs.Count > 0 ? this.Childs.Values.Select(child => child.PocetObyvatel).Sum() : 0); return _PocetObyvatel.Value; } } private int? _PocetObyvatel = null;
            public Dictionary<string, Obec> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                foreach (var item in this.Childs.Values)
                    item.Clear(contentType);
                if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                if (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack)
                    stream.WriteLine($"{HeaderMesto};{Kod};{Nazev}");
                foreach (var item in this.Childs.Values)
                    item.Save(saveInfo, stream);
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
                foreach (var item in this.Childs.Values)
                    item.AddEntities(search, entitesStart, entitesContains);
            }
        }
        public class Obec : IEntity
        {
            public Obec(Mesto parent, string kod, string nazev)
            {
                this.Parent = parent;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Childs = new Dictionary<string, Vesnice>();
                Parent.ChildsIsVoid = this.IsVoid;
            }
            public override string ToString()
            {
                return $"{Text}; Parent: {Parent.Text}";
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return (Kod == VoidEntityCode); } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.Obec; } }
            public string FullCode { get { return Parent.FullCode + EntityDelimiter + Kod; } }
            public Mesto Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public int PocetObyvatel { get { if (!_PocetObyvatel.HasValue) _PocetObyvatel = (this.Childs.Count > 0 ? this.Childs.Values.Select(child => child.PocetObyvatel).Sum() : 0); return _PocetObyvatel.Value; } } private int? _PocetObyvatel = null;
            public Dictionary<string, Vesnice> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                foreach (var item in this.Childs.Values)
                    item.Clear(contentType);
                if (contentType == FileContentType.Structure || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                if (saveInfo.ContentType == FileContentType.Structure || saveInfo.ContentType == FileContentType.DataPack)
                    stream.WriteLine($"{HeaderObec};{Kod};{Nazev}");
                foreach (var item in this.Childs.Values)
                    item.Save(saveInfo, stream);
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
                foreach (var item in this.Childs.Values)
                    item.AddEntities(search, entitesStart, entitesContains);
            }
        }
        public class Vesnice : IEntity
        {
            public Vesnice(Obec parent, string kod, string nazev)
            {
                this.Parent = parent;
                this.Kod = kod;
                this.Nazev = nazev;
                this.Childs = new Dictionary<int, Info>();
                Parent.ChildsIsVoid = this.IsVoid;
            }
            public override string ToString()
            {
                return $"{Text}; Parent: {Parent.Text}";
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}"; } }
            public bool IsVoid { get { return (Kod == VoidEntityCode); } }
            public bool ChildsIsVoid { get; set; }
            public EntityType Entity { get { return EntityType.Vesnice; } }
            public string FullCode { get { return Parent.FullCode + EntityDelimiter + Kod; } }
            public Obec Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public string Kod { get; private set; }
            public string Nazev { get; private set; }
            public Pocet Pocet { get { return this.Database.GetPocet(this.Kod); } }
            public int PocetObyvatel { get { if (!_PocetObyvatel.HasValue) _PocetObyvatel = this.Database.GetPocetObyvatelInternal(this.Kod); return _PocetObyvatel.Value; } } private int? _PocetObyvatel = null;
            public Dictionary<int, Info> Childs { get; private set; }
            public void Clear(FileContentType contentType)
            {
                if (contentType == FileContentType.Data || contentType == FileContentType.DataPack)
                    this.Childs.Clear();
            }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
            {
                Pocet pocet;
                switch (saveInfo.ContentType)
                {
                    case FileContentType.Structure:
                        stream.WriteLine($"{HeaderVesnice};{Kod};{Nazev};");
                        pocet = this.Pocet;
                        if (pocet != null) pocet.Save(saveInfo, stream);
                        break;
                    case FileContentType.Data:
                        stream.WriteLine($"{HeaderVesnice};{Kod};{Nazev};");
                        foreach (var item in this.Childs.Values)
                            item.Save(saveInfo, stream);
                        break;
                    case FileContentType.DataPack:
                        stream.WriteLine($"{HeaderVesnice};{Kod};{Nazev};");
                        pocet = this.Pocet;
                        if (pocet != null) pocet.Save(saveInfo, stream);
                        foreach (var item in this.Childs.Values)
                            item.Save(saveInfo, stream);
                        break;
                }
            }
            public void AddResults(ResultArgs args)
            {
                foreach (var item in this.Childs.Values)
                    item.AddResults(args);

                args.PocetObyvatel += this.PocetObyvatel;
            }
            public void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains)
            {
                if (!this.IsVoid)
                    Database.AddEntities(this, this.Nazev, search, entitesStart, entitesContains);
            }
        }
        public class Info
        {
            public Info(Vesnice parent, DateTime date, int newCount, int currentCount)
            {
                this.Parent = parent;
                this.Date = date;
                this.NewCount = newCount;
                this.CurrentCount = currentCount;
            }
            public override string ToString()
            {
                return $"{Text}; Parent: {Parent.Text}";
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Datum: {(Date.ToString("dd.MM.yyyy"))}; NewCount: {NewCount}; CurrentCount: {CurrentCount}"; } }
            public Vesnice Parent { get; private set; }
            public Database Database { get { return Parent.Database; } }
            public DateTime Date { get; private set; }
            public int DateKey { get { return Date.GetDateKey(); } }
            public int DateKeyShort { get { return this.Date.GetDateKeyShort(); } }
            public int NewCount { get; private set; }
            public int CurrentCount { get; private set; }
            public void Load(string[] items)
            {
            }
            public void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream)
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
            public void AddResults(ResultArgs args)
            {
                if (args.Begin.HasValue && this.Date < args.Begin.Value) return;
                if (args.End.HasValue && this.Date >= args.End.Value) return;
                ResultInfo result = args.Results.AddOrCreate(this.DateKey, () => new ResultInfo(args.Entity, this.Date));
                result.AddInfo(this, args.ValueType);
            }
        }
        public class Pocet
        {
            public Pocet(Database database, string kod, string nazev, string mesto, string kraj, int pocetMC, int pocetMS, int pocetFC, int pocetFS)
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
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Kód: {Kod}; Název: {Nazev}; Mesto: {Mesto}; Počet obyvatel: {PocetCelkem}" ; } }
            public EntityType Entity { get { return EntityType.Vesnice; } }
            public Database Database { get; private set; }
            public void Load(string[] items)
            {
            }
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
        public class ResultInfo
        {
            public ResultInfo(IEntity entity, DateTime date)
            {
                this.Entity = entity;
                this.Date = date;
                this.RawValue = 0;
            }
            public override string ToString()
            {
                return $"{Text}; Entity: {Entity.Text}";
            }
            public string Text { get { return $"Prvek: {(this.GetType().Name)}; Datum: {(Date.ToString("dd.MM.yyyy"))}; Value: {Value}"; } }
            public IEntity Entity { get; private set; }
            public Database Database { get { return Entity.Database; } }
            public DateTime Date { get; private set; }
            public int DateKey { get { return Date.GetDateKey(); } }
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
            /// <param name="valueType"></param>
            internal void AddInfo(Info info, DataValueType valueType)
            {
                decimal value = 0m;
                switch (valueType)
                {
                    case DataValueType.NewCount:
                    case DataValueType.NewCountAvg:
                    case DataValueType.NewCountRelative:
                    case DataValueType.NewCountRelativeAvg:
                    case DataValueType.NewCount7DaySum:
                    case DataValueType.NewCount7DaySumAvg:
                    case DataValueType.NewCount7DaySumRelative:
                    case DataValueType.NewCount7DaySumRelativeAvg:
                    case DataValueType.RZero:
                    case DataValueType.RZeroAvg:
                        value = info.NewCount;
                        break;
                    case DataValueType.CurrentCount:
                    case DataValueType.CurrentCountAvg:
                    case DataValueType.CurrentCountRelative:
                    case DataValueType.CurrentCountRelativeAvg:
                        value = info.CurrentCount;
                        break;
                }
                this.RawValue += value;
            }
        }
        public interface IEntity
        {
            EntityType Entity { get; }
            bool IsVoid { get; }
            bool ChildsIsVoid { get; }
            string FullCode { get; }
            string Text { get; }
            Database Database { get; }
            string Kod { get; }
            string Nazev { get; }
            int PocetObyvatel { get; }
            void Clear(FileContentType contentType);
            void Load(string[] items);
            void Save(ProcessFileInfo saveInfo, IO.StreamWriter stream);
            void AddResults(ResultArgs args);
            void AddEntities(string search, List<IEntity> entitesStart, List<IEntity> entitesContains);
        }
        public class ProcessFileInfo
        {
            public ProcessFileInfo(string fileName)
            {
                FileName = fileName;
                ContentType = FileContentType.None;
                StartTime = DateTime.Now;
                LastProgressTime = DateTime.MinValue;
                DoneTime = DateTime.MinValue;
                Length = 0L;
                ProcessState = ProcessFileState.None;
                Position = 0L;
                RecordCount = 0;
            }

            public string FileName { get; set; }
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
                            description = "Načítám soubor, obsahující ";
                            break;
                        case ProcessFileState.Loaded:
                            description = "Načten soubor, obsahující ";
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
                        case FileContentType.PocetObyvatel:
                            description += "počty obyvatel ";
                            break;
                        default:
                            description += "soubor typu " + this.ContentType.ToString();
                            break;
                    }
                    return description.Trim();
                }
            }
        }
        public class ProcessFileCurrentInfo
        {
            public World World { get; set; }
            public Zeme Zeme { get; set; }
            public Kraj Kraj { get; set; }
            public Okres Okres { get; set; }
            public Mesto Mesto { get; set; }
            public Obec Obec { get; set; }
            public Vesnice Vesnice { get; set; }
            public Pocet Pocet { get; set; }
            public Info Info { get; set; }
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
            CovidObce2
        }

        public class ResultArgs
        {
            public ResultArgs(IEntity entity, DataValueType valueType, DateTime? begin, DateTime? end)
            {
                this.Entity = entity;
                this.ValueType = valueType;
                this.Begin = begin;
                this.End = end;
                this.Results = new Dictionary<int, ResultInfo>();
                this.PocetObyvatel = 0;
            }

            public IEntity Entity { get; private set; }
            public DataValueType ValueType { get; private set; }
            public DateTime? Begin { get; private set; }
            public DateTime? End { get; private set; }
            public Dictionary<int, ResultInfo> Results { get; private set; }
            public int PocetObyvatel { get; set; }

        }
        #endregion
        #region Komprimace a dekomprimace stringu
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
        #region Třída pro progress ProgressArgs
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
        }
        #endregion
    }
    /// <summary>
    /// Data pro vizualizaci objektu
    /// </summary>
    public class DataVisualInfo
    {
        public DataVisualInfo(object value, string text, string toolTip = null, System.Drawing.Image icon = null, object tag = null)
        {
            this.Value = value;
            this.Text = text;
            this.ToolTip = toolTip;
            this.Icon = icon;
            this.Tag = tag;
        }
        public object Value { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public System.Drawing.Image Icon { get; set; }
        public object Tag { get; set; }
    }
    /// <summary>
    /// Druh dat získaných z databáze pro jednu serii
    /// </summary>
    public enum DataValueType
    {
        None,
        NewCount,
        NewCountAvg,
        NewCountRelative,
        NewCountRelativeAvg,
        NewCount7DaySum,
        NewCount7DaySumAvg,
        NewCount7DaySumRelative,
        NewCount7DaySumRelativeAvg,
        CurrentCount,
        CurrentCountAvg,
        CurrentCountRelative,
        CurrentCountRelativeAvg,
        RZero,
        RZeroAvg
    }

    public static class Extensions
    {
        public static int GetDateKey(this DateTime date)
        {
            return 10000 * date.Year + 100 * date.Month + date.Day;
        }
        public static int GetDateKeyShort(this DateTime date)
        {
            TimeSpan time = date - new DateTime(2019, 1, 1);
            return (int)time.TotalDays;
        }
        public static TValue AddOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> data, TKey key, Func<TValue> creator)
        {
            if (data.TryGetValue(key, out TValue value)) return value;
            value = creator();
            data.Add(key, value);
            return value;
        }
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> data, TKey key, TValue value)
        {
            if (!data.ContainsKey(key))
                data.Add(key, value);
            else
                data[key] = value;
        }
        public static void AddIfNotContains<TKey, TValue>(this Dictionary<TKey, TValue> data, TKey key, TValue value)
        {
            if (!data.ContainsKey(key))
                data.Add(key, value);
        }
        public static TValue[] GetSortedValues<TKey, TValue>(this Dictionary<TKey, TValue> data, Func<TValue, TValue, int> sorter)
        {
            List<TValue> values = data.Values.ToList();
            values.Sort((a, b) => sorter(a, b));
            return values.ToArray();
        }
        public static void ForEachExec<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }
    }
}
