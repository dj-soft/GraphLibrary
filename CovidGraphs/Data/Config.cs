using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Djs.Tools.CovidGraphs.Data
{
    /// <summary>
    /// Třída s konfigurací
    /// </summary>
    public class Config : DataSerializable
    {
        #region Konstruktor, soubor s konfigurací
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="configFile"></param>
        public Config(string configFile)
        {
            this._ConfigFile = configFile;
            this.Load();
        }
        /// <summary>
        /// Soubor s uloženými daty.
        /// Lze změnit hodnotu, po změně se do nově specifikovaného souboru uloží aktuální data.
        /// </summary>
        public string ConfigFile
        {
            get { return _ConfigFile; }
            set
            {
                _ConfigFile = value;
                this.Save();
            }
        }
        /// <summary>
        /// Soubor s uloženými daty.
        /// Lze změnit hodnotu, po změně se z nově určeného souboru načtou data do this instance.
        /// Po změně bude tatáž hodnota vrácena i z property <see cref="ConfigFile"/>.
        /// </summary>
        public string ConfigFileConnect
        {
            get { return _ConfigFile; }
            set
            {
                _ConfigFile = value;
                this.Load();
            }
        }
        private string _ConfigFile;
        #endregion
        #region Load & Save souboru s konfigurací
        /// <summary>
        /// Načte konfiguraci ze souboru this.ConfigFile.
        /// Pokud není zadán nebo neexistuje, nic nedělá.
        /// </summary>
        public void Load()
        {
            this.LoadDefaults();

            bool oldEnabled = SaveEnabled;
            SaveEnabled = false;

            string configFile = this.ConfigFile;
            if (String.IsNullOrEmpty(configFile) || !File.Exists(configFile))
                return;

            ConfigVersionType versionType = ConfigVersionType.None;

            string[] lines = File.ReadAllLines(configFile, DataSerializable.Encoding);
            foreach (string line in lines)
            {
                switch (versionType)
                {
                    case ConfigVersionType.None:
                        if (line == ConfigContentHeader)
                            versionType = ConfigVersionType.Version1;
                        break;
                    case ConfigVersionType.Version1:
                        string name = LoadNameValueFromString(line, out string value);
                        this.LoadItem(name, value);
                        break;
                }
            }

            SaveEnabled = oldEnabled;
        }
        private enum ConfigVersionType
        {
            None,
            Version1
        }
        /// <summary>
        /// Zpracuje jednu položku konfigurace
        /// </summary>
        /// <param name="item"></param>
        private void LoadItem(string name, string value)
        {
            switch (name)
            {
                case CfgNameLastSaveGraphId:
                    this.LastSaveGraphId = GetValue(value, 0);
                    break;
                case CfgNameMainSplitterPosition:
                    this.MainSplitterPosition = GetValue(value, MainSplitterPositionDefault);
                    break;
                case CfgNameSkinName:
                    this.ActiveSkinName = GetValue(value, "");
                    break;
                case CfgNameSkinPalette:
                    this.ActiveSkinPalette = GetValue(value, "");
                    break;
                case CfgNameEditFormPosition:
                    this.EditFormPosition = GetValueInt32Array(value);
                    break;
                case CfgNameEditFormLayout:
                    this.EditFormLayout = GetValueInt32Array(value);
                    break;
                case CfgNameEditFormGraphPanelLayout:
                    this.EditFormGraphPanelLayout = GetValueInt32Array(value);
                    break;
                default:
                    if (name.StartsWith(CfgNameUserDataKeyPrefix) && name.Length > CfgNameUserDataKeyPrefixLength)
                    {
                        string key = name.Substring(CfgNameUserDataKeyPrefixLength);
                        this._UserDataAdd(key, value);
                    }
                    break;
            }
        }
        /// <summary>
        /// Načte defaultní hodnoty
        /// </summary>
        private void LoadDefaults()
        {
            bool oldEnabled = SaveEnabled;
            SaveEnabled = false;

            this.LastSaveGraphId = 0;
            this.MainSplitterPosition = MainSplitterPositionDefault;
            this.EditFormPosition = null;
            this.EditFormLayout = null;
            this.EditFormGraphPanelLayout = null;
            this._UserData = new Dictionary<string, string>();

            SaveEnabled = oldEnabled;
        }
        /// <summary>
        /// Uloží konfiguraci do svého souboru config.ini
        /// </summary>
        public void Save()
        {
            if (!SaveEnabled) return;

            using (var sw = CreateWriteStream(this.ConfigFile))
            {
                sw.WriteLine(ConfigContentHeader);
                sw.WriteLine(CreateLine(CfgNameLastSaveGraphId, GetSerial(this.LastSaveGraphId)));
                sw.WriteLine(CreateLine(CfgNameMainSplitterPosition, GetSerial(this.MainSplitterPosition)));
                sw.WriteLine(CreateLine(CfgNameSkinName, GetSerial(this.ActiveSkinName)));
                sw.WriteLine(CreateLine(CfgNameSkinPalette, GetSerial(this.ActiveSkinPalette)));
                sw.WriteLine(CreateLine(CfgNameEditFormPosition, GetSerialInt32Array(this.EditFormPosition)));
                sw.WriteLine(CreateLine(CfgNameEditFormLayout, GetSerialInt32Array(this.EditFormLayout)));
                sw.WriteLine(CreateLine(CfgNameEditFormGraphPanelLayout, GetSerialInt32Array(this.EditFormGraphPanelLayout)));

                foreach (var userKey in this._UserData)
                    sw.WriteLine(CreateLine(CfgNameUserDataKeyPrefix + userKey.Key, GetSerial(userKey.Value)));
            }
        }
        /// <summary>
        /// Je povoleno ukládat data do Config souboru? Default = false, je nutno nastavit ručně na true po inicializaci UI
        /// </summary>
        public bool SaveEnabled { get { return _SaveEnabled; } set { _SaveEnabled = value; } } private bool _SaveEnabled = false;
        /// <summary>MainSplitterPositionDefault</summary>
        protected const int MainSplitterPositionDefault = 240;
        /// <summary>== BestInCovid v1.0 config ==</summary>
        protected const string ConfigContentHeader = "== BestInCovid v1.0 config ==";
        /// <summary>LastSaveGraphId</summary>
        protected const string CfgNameLastSaveGraphId = "LastSaveGraphId";
        /// <summary>MainSplitterPosition</summary>
        protected const string CfgNameMainSplitterPosition = "MainSplitterPosition";
        /// <summary>SkinName</summary>
        protected const string CfgNameSkinName = "SkinName";
        /// <summary>SkinPalette</summary>
        protected const string CfgNameSkinPalette = "SkinPalette";
        /// <summary>EditFormBounds</summary>
        protected const string CfgNameEditFormPosition = "EditFormBounds";
        /// <summary>EditFormMainSplitter</summary>
        protected const string CfgNameEditFormLayout = "EditFormMainSplitter";
        /// <summary>EditFormGraphPanelLayout</summary>
        protected const string CfgNameEditFormGraphPanelLayout = "EditFormGraphPanelLayout";
        /// <summary>Key.</summary>
        protected const string CfgNameUserDataKeyPrefix = "Key.";
        /// <summary>Délka textu v <see cref="CfgNameUserDataKeyPrefix": 4/></summary>
        protected const int CfgNameUserDataKeyPrefixLength = 4;

        /// <summary>ini</summary>
        public const string ConfigFileExtension = "ini";
        #endregion
        #region Data konfigurace
        /// <summary>
        /// Vygeneruje a vrátí ID pro nový graf. Používá se až při ukládání grafu na disk.
        /// </summary>
        /// <returns></returns>
        public int GetNextGraphId()
        {
            int nextId = LastSaveGraphId + 1;
            LastSaveGraphId = nextId;
            return nextId;
        }
        /// <summary>
        /// ID posledně uloženého grafu, následující bude mít +1.
        /// ID se přiděluje při ukládání na disk pro graf, který dosud ID nemá.
        /// </summary>
        public int LastSaveGraphId
        {
            get { return _LastSaveGraphId; }
            set { _LastSaveGraphId = value; this.Save(); }
        }
        private int _LastSaveGraphId;
        /// <summary>
        /// Pozice splitteru. 
        /// Po setování se konfigurace ihned uloží.
        /// </summary>
        public int MainSplitterPosition
        {
            get { return this._MainSplitterPosition; }
            set { this._MainSplitterPosition = (value < 100 ? 100 : (value > 800 ? 800 : value)); this.Save(); }
        }
        private int _MainSplitterPosition;
        /// <summary>
        /// Jméno Skinu
        /// Po setování se konfigurace ihned uloží.
        /// </summary>
        public string ActiveSkinName
        {
            get { return this._ActiveSkinName; }
            set { this._ActiveSkinName = value; this.Save(); }
        }
        private string _ActiveSkinName;
        /// <summary>
        /// Jméno Palety Skinu
        /// Po setování se konfigurace ihned uloží.
        /// </summary>
        public string ActiveSkinPalette
        {
            get { return this._ActiveSkinPalette; }
            set { this._ActiveSkinPalette = value; this.Save(); }
        }
        private string _ActiveSkinPalette;
        /// <summary>
        /// Uloží najednou skin i paletu
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="paletteName"></param>
        public void SetSkin(string skinName, string paletteName)
        {
            this._ActiveSkinName = skinName;
            this._ActiveSkinPalette = paletteName;
            this.Save();
        }
        public bool HasSkin { get { return !String.IsNullOrEmpty(_ActiveSkinName); } }
        /// <summary>
        /// Souřadnice okna editoru, null = nezadáno, Empty = maximalizováno
        /// </summary>
        public int[] EditFormPosition
        {
            get { return _EditFormBounds; }
            set { _EditFormBounds = value; this.Save(); }
        }
        private int[] _EditFormBounds;
        /// <summary>
        /// Layout pro okno editoru (splittery atd)
        /// </summary>
        public int[] EditFormLayout
        {
            get { return _EditFormLayout; }
            set { _EditFormLayout = value; this.Save(); }
        }
        private int[] _EditFormLayout;
        /// <summary>
        /// Layout pro panel s daty grafu (splittery atd)
        /// </summary>
        public int[] EditFormGraphPanelLayout
        {
            get { return _EditFormGraphPanelLayout; }
            set { _EditFormGraphPanelLayout = value; this.Save(); }
        }
        private int[] _EditFormGraphPanelLayout;


        #endregion
        #region UserData

        public void UserDataAdd(string key, string value) { _UserDataAdd(key, value); }
        public void UserDataAdd(string key, bool value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, bool? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, int value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, int? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, long value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, long? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, DateTime value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, DateTime? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAddEnum<TEnum>(string key, TEnum value) where TEnum : struct { _UserDataAdd(key, GetSerialEnum<TEnum>(value)); }
        public void UserDataAddEnum<TEnum>(string key, TEnum? value) where TEnum : struct { _UserDataAdd(key, GetSerialEnum(value)); }

        public void UserDataAdd(string key, System.Drawing.Color value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Color? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Point value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Point? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Size value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Size? value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Rectangle value) { _UserDataAdd(key, GetSerial(value)); }
        public void UserDataAdd(string key, System.Drawing.Rectangle? value) { _UserDataAdd(key, GetSerial(value)); }

        public string UserDataGet(string key, string defValue) { return GetValue(_UserDataGet(key), defValue); }
        public bool UserDataGet(string key, bool defValue) { return GetValue(_UserDataGet(key), defValue); }
        public bool? UserDataGet(string key, bool? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public int UserDataGet(string key, int defValue) { return GetValue(_UserDataGet(key), defValue); }
        public int? UserDataGet(string key, int? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public long UserDataGet(string key, long defValue) { return GetValue(_UserDataGet(key), defValue); }
        public long? UserDataGet(string key, long? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public DateTime UserDataGet(string key, DateTime defValue) { return GetValue(_UserDataGet(key), defValue); }
        public DateTime? UserDataGet(string key, DateTime? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public TEnum UserDataGetEnum<TEnum>(string key, TEnum defValue) where TEnum : struct { return GetValueEnum(_UserDataGet(key), defValue); }
        public TEnum? UserDataGetEnum<TEnum>(string key, TEnum? defValue) where TEnum : struct { return GetValueEnum(_UserDataGet(key), defValue); }

        public System.Drawing.Color UserDataGet(string key, System.Drawing.Color defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Color? UserDataGet(string key, System.Drawing.Color? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Point UserDataGet(string key, System.Drawing.Point defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Point? UserDataGet(string key, System.Drawing.Point? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Size UserDataGet(string key, System.Drawing.Size defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Size? UserDataGet(string key, System.Drawing.Size? defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Rectangle UserDataGet(string key, System.Drawing.Rectangle defValue) { return GetValue(_UserDataGet(key), defValue); }
        public System.Drawing.Rectangle? UserDataGet(string key, System.Drawing.Rectangle? defValue) { return GetValue(_UserDataGet(key), defValue); }


        private void _UserDataAdd(string key, string value)
        {
            if (String.IsNullOrEmpty(key)) return;
            if (_UserData.ContainsKey(key))
                _UserData[key] = value;
            else
                _UserData.Add(key, value);

            if (SaveEnabled)
                this.Save();
        }
        private string _UserDataGet(string key)
        {
            if (!String.IsNullOrEmpty(key) && _UserData.TryGetValue(key, out string value)) return value;
            return null;
        }
        public void UserDataRemove(string key)
        {
            if (!String.IsNullOrEmpty(key) && _UserData.ContainsKey(key))
                _UserData.Remove(key);
        }
        private Dictionary<string, string> _UserData;
        #endregion
    }
}
