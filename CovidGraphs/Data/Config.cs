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

            bool hasSignature = false;
            string[] lines = File.ReadAllLines(configFile, DataSerializable.Encoding);
            foreach (string line in lines)
            {
                if (line == ConfigContentHeader)
                    hasSignature = true;
                else if (hasSignature)
                {
                    string name = LoadNameValueFromString(line, out string value);
                    this.LoadItem(name, value);
                }
            }

            SaveEnabled = oldEnabled;
        }
        /// <summary>
        /// Zpracuje jednu položku konfigurace
        /// </summary>
        /// <param name="item"></param>
        private void LoadItem(string name, string value)
        {
            switch (name)
            {
                case SplitterPositionName:
                    this.SplitterPosition = GetValue(value, SplitterPositionDefault);
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

            this.SplitterPosition = SplitterPositionDefault;

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
                sw.WriteLine(CreateLine(SplitterPositionName, GetSerial(this.SplitterPosition)));
            }
        }
        /// <summary>
        /// Je povoleno ukládat data do Config souboru? Default = false, je nutno nastavit ručně na true po inicializaci UI
        /// </summary>
        public bool SaveEnabled { get { return _SaveEnabled; } set { _SaveEnabled = value; } } private bool _SaveEnabled = false;
        /// <summary>== BestInCovid v1.0 config ==</summary>
        protected const string ConfigContentHeader = "== BestInCovid v1.0 config ==";
        /// <summary>SplitterPosition</summary>
        protected const string SplitterPositionName = "SplitterPosition";
        protected const int SplitterPositionDefault = 240;
        /// <summary>ini</summary>
        public const string ConfigFileExtension = "ini";
        #endregion
        #region Data konfigurace
        /// <summary>
        /// Pozice splitteru. 
        /// Po setování se konfigurace ihned uloží.
        /// </summary>
        public int SplitterPosition
        {
            get { return this._SplitterPosition; }
            set { this._SplitterPosition = (value < 100 ? 100 : (value > 800 ? 800 : value)); this.Save(); }
        }
        private int _SplitterPosition;




        #endregion
    }


}
