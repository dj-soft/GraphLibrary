using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher
{
    /// <summary>
    /// Obsahuje kompletní nastavení aplikace.
    /// <para/>
    /// Třída je partial, je tedy možné do ní přidávat další části i v rámci jiných souborů.
    /// Třída sama zajišťuje načítání ze svého souboru, a ukládání svých dat do něj při ukončení aplikace (deserializace + serializace).<br/>
    /// <para/>
    /// Rozšířující moduly mohou zavolat metodu <see cref="Save"/> kdykoliv chtějí = po provedení výraznější změny.<br/>
    /// Rozšířující moduly si mohou zaevidovat handlery <see cref="AfterCreate"/> a <see cref="AfterLoad"/> pro konsolidaci a přípravu svých dat po načtení, 
    /// anebo <see cref="BeforeSave"/> pro konsolidaci dat před uložením.
    /// <para/>
    /// Rozšířující moduly si přidají svoje property, a pokud budou mít get i set přístup (i kdyby privátní), pak budou ukládány.
    /// Pro řízení ukládání lze pro konkrétní property použít atributy <see cref="Data.PropertyNameAttribute"/> a/nebo <see cref="Data.PersistingEnabledAttribute"/>.
    /// </summary>
    public partial class Settings
    {
        #region Create, Load, Konstruktor, Soubor
        /// <summary>
        /// Vytvoří instanci Settings a načte do ní data z patřičného souboru
        /// </summary>
        /// <returns></returns>
        public static Settings Create()
        {
            string fileName = _GetFileName();
            if (System.IO.File.Exists(fileName))
            {
                var persistArgs = new Data.PersistArgs() { XmlFile = fileName };
                var result = DjSoft.Tools.ProgramLauncher.Data.Persist.Deserialize(persistArgs);
                if (result is Settings settings)
                {
                    settings._RunAfterLoad(fileName);
                    return settings;
                }
            }

            var blankSettings = new Settings();
            blankSettings._RunAfterCreate(fileName);
            blankSettings.Save();
            return blankSettings;
        }
        /// <summary>
        /// Privátní konstruktor, používá se při deserializaci
        /// </summary>
        private Settings() { }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        private Settings(string fileName)
        {
            this.__FileName = fileName;
        }
        /// <summary>
        /// Soubor, z něhož je Settings načten a kam bude ukládán.
        /// </summary>
        public string FileName { get { return __FileName; } } private string __FileName;
        /// <summary>
        /// Určí a vrátí jméno souboru pro zdejší ukládání dat. Nezajišťuje existenci adresáře ani souboru, to se řeší v <see cref="_Save"/>.
        /// </summary>
        /// <returns></returns>
        private static string _GetFileName()
        {
            var dataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);  // C:\Users\{userName}\AppData\Local
            var settingPath = System.IO.Path.Combine(dataPath, App.Company, App.ProductName);
            return System.IO.Path.Combine(settingPath, File);
        }
        /// <summary>
        /// Jméno souboru - bez adresáře, s příponou.
        /// </summary>
        private const string File = "Settings.dat";
        #endregion
        #region Validate, eventy BeforeSave a AfterLoad a jejich obsluha
        /// <summary>
        /// Je spuštěno po vytvoření new instance, když neexistuje soubor s konfigurací
        /// </summary>
        /// <param name="fileName"></param>
        private void _RunAfterCreate(string fileName = null)
        {
            if (fileName != null) this.__FileName = fileName;
            AfterCreate?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je spuštěno po načtení dat ze souboru
        /// </summary>
        /// <param name="fileName"></param>
        private void _RunAfterLoad(string fileName = null)
        {
            if (fileName != null) this.__FileName = fileName;
            AfterLoad?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je spuštěno před uložením dat do souboru
        /// </summary>
        private void _RunBeforeSave()
        {
            BeforeSave?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost volaná po vytvoření new instance v případě, kdy nemáme soubor z něhož bude načítáno
        /// </summary>
        public event EventHandler AfterCreate;
        /// <summary>
        /// Událost volaná po načtení instance ze souboru
        /// </summary>
        public event EventHandler AfterLoad;
        /// <summary>
        /// Událost volaná před ukládáním instance do souboru
        /// </summary>
        public event EventHandler BeforeSave;
        #endregion
        #region Základní data
        public string AppearanceName { get; set; }
        public Rectangle MainFormBounds { get; set; }

        #endregion
        #region Save
        /// <summary>
        /// Uloží data konfigurace do jejího souboru.
        /// </summary>
        public void Save()
        {
            try { _Save(); }
            catch { }
        }
        private void _Save()
        {
            _RunBeforeSave();

            string fileName = __FileName;
            if (String.IsNullOrEmpty(fileName)) return;
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (String.IsNullOrEmpty(path)) return;

            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            var persistArgs = new Data.PersistArgs() 
            {
                XmlFile = fileName,
                CompressMode = Data.XmlCompressMode.None,
                DataHeapEnabled = false,
                UseDotNetFwSerializer = false,
                WriterSettings = new System.Xml.XmlWriterSettings() { Indent = true, IndentChars = "  ", NewLineChars = "\r\n", NewLineHandling = System.Xml.NewLineHandling.Entitize, NewLineOnAttributes = false }
            };
            Data.Persist.Serialize(this, persistArgs);
        }
        #endregion
    }
}
