using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Djs.Tools.WebDownloader
{
    public class Config : WebData
    {
        public Config(string configFile)
        {
            this.ConfigFile = configFile;
            this.Load();
        }
        public string ConfigFile { get; private set; }
        #region Load & Save souboru s konfigurací
        /// <summary>
        /// Načte konfiguraci ze souboru this.ConfigFile.
        /// Pokud není zadán nebo neexistuje, nic nedělá.
        /// </summary>
        public void Load()
        {
            string configFile = this.ConfigFile;
            if (String.IsNullOrEmpty(configFile) || !File.Exists(configFile))
                return;
            bool hasSignature = false;
            string[] lines = File.ReadAllLines(configFile, WebData.Encoding);
            foreach (string line in lines)
            {
                if (line == NAME_TITLE)
                    hasSignature = true;
                else if (hasSignature)
                {
                    List<KeyValuePair<string, string>> list = LoadFromString(line);
                    foreach (KeyValuePair<string, string> item in list)
                        this.LoadItem(item);
                }
            }
        }
        /// <summary>
        /// Zpracuje jednu položku konfigurace
        /// </summary>
        /// <param name="item"></param>
        private void LoadItem(KeyValuePair<string, string> item)
        {
            switch (item.Key)
            {
                case NAME_SAVEAUTO:
                    this.SaveAutomatic = GetValue(item.Value, false);
                    break;
                case NAME_SAVEONDOWNLOAD:
                    this.SaveOnDownload = GetValue(item.Value, false);
                    break;
                case NAME_SAVETOPATH:
                    this.SaveToPath = GetValue(item.Value, "");
                    break;
            }
        }
        /// <summary>
        /// Uloží konfiguraci do svého souboru config.ini
        /// </summary>
        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(this.ConfigFile, false, WebData.Encoding))
            {
                sw.WriteLine(NAME_TITLE);
                sw.WriteLine(CreatePair(NAME_SAVEAUTO, this.SaveAutomatic, false));
                sw.WriteLine(CreatePair(NAME_SAVEONDOWNLOAD, this.SaveOnDownload, false));
                sw.WriteLine(CreatePair(NAME_SAVETOPATH, this.SaveToPath, false));
            }
        }
        /// <summary>== WebDownloader v3.0 config ==</summary>
        protected const string NAME_TITLE = "== WebDownloader v3.0 config ==";
        /// <summary>SaveAutomatic</summary>
        protected const string NAME_SAVEAUTO = "SaveAutomatic";
        /// <summary>SaveOnDownload</summary>
        protected const string NAME_SAVEONDOWNLOAD = "SaveOnDownload";
        /// <summary>SaveToPath</summary>
        protected const string NAME_SAVETOPATH= "SaveToPath";
        /// <summary>wdc</summary>
        public const string EXTENSION = "wdc";
        #endregion
        #region Data konfigurace
        /// <summary>
        /// Ukládat data zadané konfigurace adresy po každé změně?
        /// </summary>
        public bool SaveAutomatic
        {
            get { return this._SaveAutomatic; }
            set { this._SaveAutomatic = value; this.Save(); }
        }
        private bool _SaveAutomatic;
        /// <summary>
        /// Ukládat data zadané konfigurace adresy při downloadu?
        /// </summary>
        public bool SaveOnDownload
        {
            get { return this._SaveOnDownload; }
            set { this._SaveOnDownload = value; this.Save(); }
        }
        private bool _SaveOnDownload;
        /// <summary>
        /// Ukládat download data do tohoto adresáře
        /// </summary>
        public string SaveToPath
        {
            get { return this._SaveToPath; }
            set { this._SaveToPath = value; this.Save(); }
        }
        private string _SaveToPath;

        #endregion
    }
}
