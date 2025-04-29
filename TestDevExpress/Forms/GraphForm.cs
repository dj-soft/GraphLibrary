using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;

using SIO = System.IO;
using NWC = Noris.Clients.Win.Components;
using TestDevExpress.Components;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro hrátky s grafy
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "Graf", buttonOrder: 50, buttonImage: "svgimages/dashboards/chart.svg", buttonToolTip: "Otevře okno s ukázkou grafů DevExpress včetně editoru")]
    public partial class GraphForm : DevExpress.XtraEditors.XtraForm //   MdiBaseForm
    {
        /// <summary>
        /// Formulář pro hrátky s grafy.
        /// Konstruktor
        /// </summary>
        public GraphForm()
        {
            InitializeComponent();
            InitChart();
        }
        /// <summary>
        /// Inicializace grafu
        /// </summary>
        protected void InitChart()
        {
            ChartSettingsInit();

            ChartControl = new NWC.ChartPanel();
            ChartControl.DataSource = NWC.ChartPanel.CreateSampleData();
            var settings = _ChartSettings;
            if (settings is null || settings.Count == 0)
            {   // První spuštění (bez uložených dat) anebo po smazání všech dat => vytvořím Samples a uložím je:
                settings = NWC.ChartPanel.CreateSampleSettings().ToList();
                foreach (var setting in settings)
                    SaveSetting(setting);
            }
            ChartControl.ChartSettings = settings;
            ChartControl.CurrentSettings = settings.FirstOrDefault();
            ChartControl.ChartChanged += ChartPanel_ChartChanged;
            this.Controls.Add(ChartControl);
        }
        private NWC.ChartPanel ChartControl;
        #region Správa layoutů grafu
        /// <summary>
        /// Inicializace systému pro persistenci settings grafů
        /// </summary>
        private void ChartSettingsInit()
        {
            ChartSettingsInitPath();
            ChartSettingsLoadFiles();
        }
        /// <summary>
        /// Určí adresář pro persistenci = (MyDocuments)/DevExpress/ChartTester do <see cref="_ChartSettingsPath"/>
        /// </summary>
        private void ChartSettingsInitPath()
        {
            if (ChartSettingsExistsPath(Environment.SpecialFolder.MyDocuments)) return;
            if (ChartSettingsExistsPath(Environment.SpecialFolder.LocalApplicationData)) return;
            if (ChartSettingsCreatePath(Environment.SpecialFolder.MyDocuments)) return;
            if (ChartSettingsCreatePath(Environment.SpecialFolder.LocalApplicationData)) return;
        }
        /// <summary>
        /// Pokud existuje adresář pro Setting soubory v dané složce, nastaví jeho fyzické jméno a vrátí true
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private bool ChartSettingsExistsPath(Environment.SpecialFolder folder)
        {
            string path = SIO.Path.Combine(Environment.GetFolderPath(folder), "DevExpress", "ChartTester");
            if (!SIO.Directory.Exists(path)) return false;
            _ChartSettingsPath = path;
            return true;
        }
        /// <summary>
        /// Pokud dokáže vytvořit adresář pro Setting soubory v dané složce, nastaví jeho fyzické jméno a vrátí true
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private bool ChartSettingsCreatePath(Environment.SpecialFolder folder)
        {
            bool result = false;
            string path = SIO.Path.Combine(Environment.GetFolderPath(folder), "DevExpress", "ChartTester");
            if (SIO.Directory.Exists(path))
            {
                result = true;
            }
            else
            {
                try
                {
                    SIO.Directory.CreateDirectory(path);
                    if (SIO.Directory.Exists(path))
                        result = true;
                    else
                        path = null;
                }
                catch
                {
                    path = null;
                }
            }
            _ChartSettingsPath = path;
            return result;
        }
        /// <summary>
        /// Načte z adresáře soubory a vytvoří z nich pole Settings <see cref="_ChartSettings"/>
        /// </summary>
        private void ChartSettingsLoadFiles()
        {
            _LastUsedNumber = 0;
            _ChartSettings = null;

            // 1. Z adresáře uživatele (jeho dokumenty):
            string path = _ChartSettingsPath;
            if (ChartSettingsLoadFilesFromPath(path)) return;

            List<NWC.ChartSetting> settings = null;

            // 2. Z adresáře aplikace (dodávané jako výchozí):
            string executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            path = SIO.Path.GetDirectoryName(executable);
            if (ChartSettingsLoadFilesFromPath(path))
            {   // Našli jsme výchozí soubory u aplikace: budeme je ukládt do dokumentů uživatele:
                settings = _ChartSettings;
            }
            else
            {   // 3. Implicitní dodávané kódem:
                settings = NWC.ChartPanel.CreateSampleSettings().ToList();
                _ChartSettings = settings;
            }

            // Uložíme je = do složky dokumentů uživatele:
            foreach (var setting in settings)
                SaveSetting(setting);
        }
        /// <summary>
        /// Pokusí se najít a načíst soubory se Setting pro grafy z dodaného adresáře
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool ChartSettingsLoadFilesFromPath(string path)
        {
            if (String.IsNullOrEmpty(path) || !SIO.Directory.Exists(path)) return false;

            List<NWC.ChartSetting> settings = new List<NWC.ChartSetting>();
            int lastUsedNumber = 0;

            var files = SIO.Directory.GetFiles(path, "????_*.xml", SIO.SearchOption.TopDirectoryOnly).ToList();
            int count = files.Count;
            if (count == 0) return false;
            if (count > 1) files.Sort();
            foreach (var file in files)
            {
                NWC.ChartSetting setting = null;
                try
                {
                    string definition = SIO.File.ReadAllText(file, Encoding.UTF8);
                    setting = NWC.ChartSetting.CreateFromDefinition(definition);
                    if (setting != null)
                    {
                        Int32.TryParse(SIO.Path.GetFileNameWithoutExtension(file).Substring(0, 4), out int number);     // Název souboru má mít formát: "0012_Název settingu.xml"
                        setting.Tag = new ChartFileInfo(number, file);
                        settings.Add(setting);
                        if (lastUsedNumber < number) lastUsedNumber = number;
                    }
                }
                catch { }
            }
            if (settings.Count == 0) return false;

            _ChartSettings = settings;
            _LastUsedNumber = lastUsedNumber;
            return true;
        }
        /// <summary>
        /// Adresář pro persistenci
        /// </summary>
        private string _ChartSettingsPath;
        /// <summary>
        /// Číslo naposledy použité pro soubor
        /// </summary>
        private int _LastUsedNumber;
        /// <summary>
        /// Pole settingů načtených ze souborů
        /// </summary>
        private List<NWC.ChartSetting> _ChartSettings;
        /// <summary>
        /// Eventhandler volaný po změně grafů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChartPanel_ChartChanged(object sender, NWC.ChartChangedArgs e)
        {
            switch (e.ChangeType)
            {
                case NWC.ChartChangeType.NewSettings:
                case NWC.ChartChangeType.ChangeName:
                case NWC.ChartChangeType.ChangeLayout:
                    SaveSetting(e.Setting);
                    break;
                case NWC.ChartChangeType.Delete:
                    DeleteSetting(e.Setting);
                    break;
            }
        }
        /// <summary>
        /// Uloží daný Setting do souboru.
        /// Pokud byl Setting již dříve uložen, použije jeho pořadové číslo; pokud nebyl pak inkrementuje <see cref="_LastUsedNumber"/> a toto číslo přidělí do Settingu.
        /// Pokud byl dříve Setting uložen do souboru s jiným názvem, než bude mít nyní, pak bude starý soubor odstraněn.
        /// </summary>
        /// <param name="setting"></param>
        private void SaveSetting(NWC.ChartSetting setting)
        {
            string path = _ChartSettingsPath;
            if (path is null) return;

            var info = setting.Tag as ChartFileInfo;
            if (info is null)
            {
                info = new ChartFileInfo(++_LastUsedNumber, null);
                setting.Tag = info;
            }
            string oldName = info.FileName;
            string newName = SIO.Path.Combine(path, $"{(info.Number.ToString("0000"))}_{setting.NameFile}.xml");
            bool deleteOldFile =                                     // Původní soubor Settingu vymažeme tehdy, pokud:
                     !String.IsNullOrEmpty(oldName) &&               //  1. Je zadané původní jméno, a ...
                     !String.Equals(oldName, newName, StringComparison.InvariantCultureIgnoreCase) &&                            // 2. Původní jméno je jiné než aktuální jméno, a ...
                     String.Equals(SIO.Path.GetDirectoryName(oldName), path, StringComparison.InvariantCultureIgnoreCase) &&     // 3. Adresář původního souboru je stejný jako aktuální adresář ukládací (=abychom nemazali soubory dodávané s aplikací !!!)
                     SIO.File.Exists(oldName);                       //  4. A pokud původní soubor existuje (testuji až nakonec, protože toto je fyzický přístup na hardware)
            if (deleteOldFile)
            {
                try { SIO.File.Delete(oldName); info.FileName = null; }
                catch { }
            }

            string definition = setting.Definition;
            try { SIO.File.WriteAllText(newName, definition, Encoding.UTF8); info.FileName = newName; }
            catch { }
        }
        /// <summary>
        /// Vymaže soubor odpovídající danému Setting 
        /// </summary>
        /// <param name="setting"></param>
        private void DeleteSetting(NWC.ChartSetting setting)
        {
            if (_ChartSettingsPath is null) return;

            if (setting.Tag is ChartFileInfo info)
            {
                string name = info.FileName;
                if (!String.IsNullOrEmpty(name) && SIO.File.Exists(name))
                {
                    try { SIO.File.Delete(name); }
                    catch { }
                }
            }
        }
        /// <summary>
        /// Přibalené informace o zdrojovém souboru pro jeden Setting grafu, ukládá se do jeho Tagu.
        /// Obsahuje informace používané při načítání a ukládání Settingu z/do souboru.
        /// </summary>
        private class ChartFileInfo
        {
            public ChartFileInfo() { }
            public ChartFileInfo(int number, string fileName)
            {
                Number = number;
                FileName = fileName;
            }
            public int Number { get; set; }
            public string FileName { get; set; }
        }
        #endregion
    }
}
