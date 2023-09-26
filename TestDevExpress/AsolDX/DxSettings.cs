// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Konfigurační data.
    /// Obsahují dvě úrovně dat: sekce a klíč,
    /// </summary>
    public class DxSettings
    {
        #region Public svět
        /// <summary>
        /// Vrátí obsah daného klíče z dané sekce. Může vrátit null, když nejsou data.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public string GetRawValue(string section, string key, string defValue = null)
        {
            if (section == null || key == null) return null;
            var values = _ValidValues;
            if (!values.TryGetValue(section, out var sectionValues)) return null;
            if (!sectionValues.TryGetValue(key, out var value)) return null;
            return value;
        }
        /// <summary>
        /// Vloží hodnotu
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetRawValue(string section, string key, string value)
        {
            if (section == null || key == null) 
                throw new ArgumentException($"DxSettings.SetRawValue(): section and key cannot be null.");
            if (String.IsNullOrEmpty(_ConfigFileName))
                throw new ArgumentException($"DxSettings.SetRawValue(): ConfigFileName is not set.");

            if (value is null) value = "";
            var values = _ValidValues;
            bool isChanged = true;
            if (!values.TryGetValue(section, out var sectionValues))
            {
                lock (values)
                {
                    if (!values.TryGetValue(section, out sectionValues))
                    {
                        sectionValues = new Dictionary<string, string>();
                        values.Add(section, sectionValues);
                    }
                }
            }
            lock (sectionValues)
            {
                if (!sectionValues.ContainsKey(key))
                    sectionValues.Add(key, value);
                else if (!String.Equals(sectionValues[key], value, StringComparison.Ordinal))
                    sectionValues[key] = value;
                else
                    isChanged = false;
            }
            if (isChanged)
                _SaveValues(values, _ConfigFileName);
        }
        /// <summary>
        /// Jméno firmy, použije se pro určení adresáře pro config soubor v rámci adresáře ProgramData
        /// </summary>
        public string CompanyName { get { return _CompanyName; } set { _SetConfigFile(null, value, _ApplicationName); } }
        private string _CompanyName;
        /// <summary>
        /// Jméno aplikace, použije se pro určení adresáře pro config soubor v rámci adresáře ProgramData
        /// </summary>
        public string ApplicationName { get { return _ApplicationName; } set { _SetConfigFile(null, _CompanyName, value); } }
        private string _ApplicationName;
        /// <summary>
        /// Plné jméno konfiguračního souboru
        /// </summary>
        public string ConfigFileName { get { return _ConfigFileName; } set { _SetConfigFile(value, null, null); } }
        /// <summary>
        /// Jméno souboru, který má být načten ve <see cref="_Values"/>
        /// </summary>
        private string _ConfigFileName;
        /// <summary>
        /// Do proměnných uloží dané hodnoty určující Config soubor
        /// </summary>
        /// <param name="configFile"></param>
        /// <param name="companyName"></param>
        /// <param name="applicationName"></param>
        private void _SetConfigFile(string configFile, string companyName, string applicationName)
        {
            _CompanyName = companyName;
            _ApplicationName = applicationName;
            _ConfigFileName = configFile;
            if (String.IsNullOrEmpty(_ConfigFileName))
            {
                companyName = GetValidFileName(companyName, "MsDotNet");
                applicationName = GetValidFileName(applicationName, "SampleApp");
                string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _ConfigFileName = Path.Combine(rootPath, companyName, applicationName, "settings.dat");
            }
            _InvalidateData();
        }
        private static string GetValidFileName(string file, string defValue)
        {
            string name = file.RemoveChars(Path.GetInvalidFileNameChars());
            if (String.IsNullOrEmpty(name)) return defValue;
            return (name.Length < 100 ? name : name.Substring(0, 100));
        }
        #endregion
        #region Získání dat ze souboru, kontroly aktuálnosti, načtení dat
        /// <summary>
        /// Obsahuje platná data nebo prázdnou dvojitou Dictionary
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> _ValidValues
        {
            get
            {
                Dictionary<string, Dictionary<string, string>> values = null;
                if (_ConfigFileName != null)
                {
                    // Pokud máme data načtena, a dosud není potřeba provést kontrolu aktuálnosti dat, pak data vrátíme:
                    values = _Values;
                    // Aktuálnost dat kontrolujeme po době 60 sekund od jejich načtení nebo od poslední kontroly.
                    // Vedle toho je konfigurační soubor hlídán aktivně (FileWatch).
                    // Tím dovolujeme šťouralovi u počítače editovat data ručně a tuto jeho editaci v naší aplikaci akceptovat.

                    // Nemáme data anebo je pouze vhodné zkontrolovat jejich aktuálnost?
                    FileInfo configFileInfo = null;
                    if (values != null)
                        _CheckValidData(ref values, ref configFileInfo);

                    if (values == null)
                        _LoadValidData(ref values, ref configFileInfo);
                }

                if (values == null)
                    _PrepareEmptyData(ref values);
                return values;
            }
        }
        private void _CheckValidData(ref Dictionary<string, Dictionary<string, string>> values, ref FileInfo configFileInfo)
        {
            if (values == null || !_WatchedFileValid || !_FileStateNextCheckTime.HasValue)
            {   // Pokud jsou data z nějakého důvodu neplatná, tak data z proměnné zahazuji:
                values = null;
                return;
            }

            if (_FileStateNextCheckTime.HasValue && DateTime.Now >= _FileStateNextCheckTime.Value)
                // Pokud data existují, a nejsou invalidovaná explicitně, a neuplynul čas pro jejich pasivní kontrolu, pak je akceptujeme:
                return;

            // Kontrola, zda načtená data pochází z toho souboru, který chceme mít načtený nyní:
            string loadedFileName = (_LoadedFileName ?? "").Trim().ToLower();
            string configFileName = (_ConfigFileName ?? "").Trim().ToLower();

            if (loadedFileName == "" || configFileName == "" || loadedFileName != configFileName)
            {   // Pokud nemáme data načtená, nebo je nemáme mít načtená, nebo jsou načtení z jiného souboru, tak data z proměnné zahazuji:
                values = null;
                return;
            }

            // Kontrola neexistence souboru:
            configFileInfo = new FileInfo(configFileName);
            if (!configFileInfo.Exists)
            {   // Pokud máme nějaká data načtená, ale požadovaný soubor zmizel, tak data z proměnné zahazuji:
                values = null;
                return;
            }

            // Vlastní kontrola aktuálnosti: pokud reálný (aktuální) soubor odpovídá tomu, který jsme posledně načetli, pak máme platná data:
            bool isEqualFile = (_LoadedFileNameTime.HasValue && _LoadedFileNameTime.Value == configFileInfo.LastWriteTimeUtc 
                             && _LoadedFileNameLength.HasValue && _LoadedFileNameLength.Value == configFileInfo.Length);
            if (!isEqualFile)
            {   // Máme existující, ale nějak změněný soubor: zajistíme, že stará data zahodíme a ze souboru načteme nová data:
                values = null;
                return;
            }

            // Data v proměnné values necháme beze změn... = jsou platná. Příští kontrola bude za 60 sekund:
            _SetNextCheckTimeFileState();
        }
        /// <summary>
        /// Načte a vrátí aktuální obsah dodaného souboru
        /// </summary>
        /// <param name="values"></param>
        /// <param name="configFileInfo"></param>
        private void _LoadValidData(ref Dictionary<string, Dictionary<string, string>> values, ref FileInfo configFileInfo)
        {
            if (configFileInfo == null)
            {
                string configFileName = (_ConfigFileName ?? "").Trim().ToLower();
                if (String.IsNullOrEmpty(configFileName))
                {   // Pokud máme nějaká data načtená, ale zmizel název souboru, tak data z proměnné zahazuji, a soubor nenačítám = není odkud:
                    values = null;
                    return;
                }
                configFileInfo = new FileInfo(configFileName);
            }

            if (!configFileInfo.Exists)
            {   // Pokud máme nějaká data načtená, ale zmizel soubor, tak data z proměnné zahazuji:
                values = null;
                return;
            }

            // Máme existující soubor, načteme jej a pak si zajistíme jeho hlídání:
            values = _LoadFile(configFileInfo.FullName);

            // Zajistíme hlídání změn:
            _SetFileWatchers(configFileInfo);

            // Zapamatujeme si obsah:
            _Values = values;
        }
        /// <summary>
        /// Připraví new empty Dictionary a uloží ji i do <see cref="_Values"/>.
        /// </summary>
        /// <param name="values"></param>
        private void _PrepareEmptyData(ref Dictionary<string, Dictionary<string, string>> values)
        {
            if (values != null) return;
            values = new Dictionary<string, Dictionary<string, string>>();
            _Values = values;
        }
        /// <summary>
        /// Načte obsah daného souboru, deserializuje objekt a zkonvertuje na dvojitou Dictionary
        /// </summary>
        /// <param name="configFileName"></param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, string>> _LoadFile(string configFileName)
        {
            Dictionary<string, Dictionary<string, string>> values = null;
            try
            {
                string text = File.ReadAllText(configFileName, Encoding.UTF8);
                var data = Noris.WS.Parser.XmlSerializer.Persist.Deserialize(text);
                if (data is DxSettingsData dxSettingsData)
                    values = _ConvertValues(dxSettingsData);
            }
            catch
            {
                values = null;
            }
            return values;
        }
        /// <summary>
        /// Z dodaných hrubých dat vrátí dvojitou Dictionary
        /// </summary>
        /// <param name="dxSettingsData"></param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, string>> _ConvertValues(DxSettingsData dxSettingsData)
        {
            Dictionary<string, Dictionary<string, string>> values = new Dictionary<string, Dictionary<string, string>>();
            foreach (var section in dxSettingsData.Data)
            {   // Data obsahují pole sekcí, kde Item1 = klíč sekce, a Item2 = pole párů (klíč : hodnota).
                string sectionName = section.Name;
                if (sectionName == null) sectionName = "";
                Dictionary<string, string> sectionDict = null;       // Do values přidáme sekci až tehdy, když bude něco obsahovat.
                var keysvalues = section.Values;
                foreach (var keyvalue in keysvalues)
                {   // keyvalue je pár Item1 = klíč : Item2 = hodnota
                    if (sectionDict == null && !values.TryGetValue(sectionName, out sectionDict))
                    {   // Sekci přidáme až budeme mít hodnotu, přidáme jen jednu sekci, i kdyby byla rozdělena ve více pozicích:
                        sectionDict = new Dictionary<string, string>();
                        values.Add(sectionName, sectionDict);
                    }
                    // Klíč a hodnotu přidáme jen na prvním výskytu klíče:
                    string key = keyvalue.Name;
                    if (key == null) key = "";
                    if (!sectionDict.ContainsKey(key))
                        sectionDict.Add(key, keyvalue.Value);
                }
            }
            return values;
        }
        /// <summary>
        /// Aktivuje aktivní i pasivní hlídání změn souboru
        /// </summary>
        /// <param name="configFileName"></param>
        private void _SetFileWatchers(string configFileName)
        {
            FileInfo configFileInfo = new FileInfo(configFileName);
            _SetFileWatchers(configFileInfo);
        }
        /// <summary>
        /// Aktivuje aktivní i pasivní hlídání změn souboru.
        /// Soubor je v tuto chvíli načten/uložen a nejsou žádné změny.
        /// </summary>
        /// <param name="configFileInfo"></param>
        private void _SetFileWatchers(FileInfo configFileInfo)
        {
            string configFileName = (configFileInfo?.FullName ?? "").Trim().ToLower();
            string loadedFileName = (_LoadedFileName ?? "").Trim().ToLower();
            string watchedFileName = (_WatchedFileName ?? "").Trim().ToLower();

            // Zapamatování vlastností načteného souboru:
            _LoadedFileName = configFileName;
            _LoadedFileNameTime = configFileInfo?.LastWriteTimeUtc;
            _LoadedFileNameLength = configFileInfo?.Length;

            // Aktivní hlídání načteného souboru:
            if (watchedFileName != configFileName || _FileWatcher is null)
            {   // Dosud nehlídáme žádný soubor, anebo hlídáme - ale špatný soubor:
                if (_FileWatcher != null)
                    _FileWatcherDispose();

                _FileWatcher = new FileSystemWatcher();
                _FileWatcher.Path = Path.GetDirectoryName(configFileName);
                _FileWatcher.IncludeSubdirectories = false;
                _FileWatcher.Filter = Path.GetFileName(configFileName);
                _FileWatcher.Created += _FileWatcher_Changed;
                _FileWatcher.Changed += _FileWatcher_Changed;
                _FileWatcher.Deleted += _FileWatcher_Changed;
                _FileWatcher.Renamed += _FileWatcher_Changed;
                _WatchedFileName = configFileName;
            }
            _FileWatcher.EnableRaisingEvents = true;
            _WatchedFileValid = true;

            // Pasivní hlídání:
            _SetNextCheckTimeFileState();
        }
        /// <summary>
        /// Deaktivuje hlídáček na souboru. Poté je možno ukládat data do souboru.
        /// </summary>
        private void _FileWatcherDeactivate()
        {
            if (_FileWatcher != null)
                _FileWatcher.EnableRaisingEvents = true;
        }
        /// <summary>
        /// Zruší aktivní hlídač souboru
        /// </summary>
        private void _FileWatcherDispose()
        {
            if (_FileWatcher != null)
            {
                _FileWatcher.EnableRaisingEvents = false;
                _FileWatcher.Created -= _FileWatcher_Changed;
                _FileWatcher.Changed -= _FileWatcher_Changed;
                _FileWatcher.Deleted -= _FileWatcher_Changed;
                _FileWatcher.Renamed -= _FileWatcher_Changed;
                _FileWatcher.Dispose();
                _FileWatcher = null;
            }
        }
        /// <summary>
        /// Po systémové změně hlídaného souboru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Jistota je jistota:
            if (_FileWatcher != null && _FileWatcher.EnableRaisingEvents)
                _InvalidateData();
        }
        /// <summary>
        /// Invaliduje data, takže následující jejich použití si provede jejich načtení.
        /// </summary>
        private void _InvalidateData()
        {
            _WatchedFileValid = false;
            _FileStateNextCheckTime = null;
        }
        private void _SetNextCheckTimeFileState()
        {
            _FileStateNextCheckTime = DateTime.Now.AddSeconds(60d);
        }
        /// <summary>
        /// Jméno souboru, který je načten ve <see cref="_Values"/>
        /// </summary>
        private string _LoadedFileName;
        /// <summary>
        /// Načtené hodnoty: Sekce; Klíče; Hodnota
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> _Values;
        /// <summary>
        /// Datum a čas souboru načteného ve <see cref="_Values"/>, pro pasivní hlídání změn. 
        /// Jde o hodnotu <see cref="FileSystemInfo.LastWriteTimeUtc"/>
        /// </summary>
        private DateTime? _LoadedFileNameTime;
        /// <summary>
        /// Velikost souboru načteného ve <see cref="_Values"/>, pro pasivní hlídání změn
        /// </summary>
        private long? _LoadedFileNameLength;
        /// <summary>
        /// Hlídač změn souboru <see cref="_WatchedFileName"/>, po změně vyvolá událost a  ta shodí platnost v <see cref="_WatchedFileValid"/>
        /// </summary>
        private FileSystemWatcher _FileWatcher;
        /// <summary>
        /// Jméno souboru, pro které hlídá změny  <see cref="_FileWatcher"/>
        /// </summary>
        private string _WatchedFileName;
        /// <summary>
        /// Obsahuje true po načtení dat ze souboru <see cref="_WatchedFileName"/>, obsahuje false po systémové události o změně tohoto souboru
        /// </summary>
        private bool _WatchedFileValid;
        /// <summary>
        /// Termín (čas) příští povinné kontroly souboru <see cref="_LoadedFileName"/> bez ohledu na hlídání pomocí <see cref="_FileWatcher"/>
        /// </summary>
        private DateTime? _FileStateNextCheckTime;

        #endregion
        #region Ukládání dat
        /// <summary>
        /// Uloží data
        /// </summary>
        /// <param name="values"></param>
        /// <param name="configFileName"></param>
        private void _SaveValues(Dictionary<string, Dictionary<string, string>> values, string configFileName)
        {
            DxSettingsData dxSettingsData = this._ConvertValues(values);
            try
            {
                _FileWatcherDeactivate();
                string text = Noris.WS.Parser.XmlSerializer.Persist.Serialize(dxSettingsData, Noris.WS.Parser.XmlSerializer.PersistArgs.Default);
                string path = Path.GetDirectoryName(configFileName);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllText(configFileName, text, Encoding.UTF8);
            }
            catch (Exception) { }
            finally
            {
                _SetFileWatchers(configFileName);
            }
        }
        /// <summary>
        /// Konvertuje dvojitou Dictionary na instanci <see cref="DxSettingsData"/>
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private DxSettingsData _ConvertValues(Dictionary<string, Dictionary<string, string>> values)
        {
            List<DxSettingsData.SectionInfo> sections = new List<DxSettingsData.SectionInfo>();
            foreach (var section in values)
            {
                DxSettingsData.SectionInfo sectionInfo = new DxSettingsData.SectionInfo() { Name = section.Key, Values = new List<DxSettingsData.ValueInfo>() };
                foreach (var keyvalue in section.Value)
                    sectionInfo.Values.Add(new DxSettingsData.ValueInfo() { Name = keyvalue.Key, Value = keyvalue.Value });
                sections.Add(sectionInfo);
            }
            return new DxSettingsData() { Data = sections.ToArray() };
        }
        #endregion
    }
    #region DxSettingsData : Ukládaná data konfigurace
    /// <summary>
    /// Ukládaná data konfigurace
    /// </summary>
    internal class DxSettingsData
    {
        public DxSettingsData() { }
        public SectionInfo[] Data { get; set; }
        internal class SectionInfo
        {
            public string Name { get; set; }
            public List<ValueInfo> Values { get; set; }
        }
        internal class ValueInfo
        {
            public string Name { get; set; }
            public string Value{ get; set; }
        }
    }
    #endregion
    #region Přístup na Settings z DxComponent
    public partial class DxComponent
    {
        /// <summary>
        /// Provozní konfigurace
        /// </summary>
        public static DxSettings Settings { get { return Instance._Settings; } }
        /// <summary>
        /// Provozní konfigurace, autoinicializační property
        /// </summary>
        private DxSettings _Settings
        {
            get
            {
                if (__Settings == null)
                    __Settings = new DxSettings();
                return __Settings;
            }
        }
        /// <summary>
        /// Field
        /// </summary>
        private DxSettings __Settings;
    }
    #endregion
}
