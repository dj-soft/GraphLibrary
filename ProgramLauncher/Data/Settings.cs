using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher
{
    /// <summary>
    /// Obsahuje kompletní nastavení aplikace.
    /// <para/>
    /// Třída sama zajišťuje načítání ze svého souboru, a ukládání svých dat do něj při ukončení aplikace (deserializace + serializace).<br/>
    /// Dále zajišťuje možnost volat po změně obsahu metodu <see cref="SetChanged"/>, čímž bude zajištěno <u>AutoSave</u> po zadaném delay: <see cref="AutoSaveDelay"/>.
    /// <para/>
    /// Třída je partial, je tedy možné do ní přidávat další části i v rámci jiných souborů.
    /// Rozšířující moduly mohou zavolat metodu <see cref="SaveDelayed(TimeSpan?)"/> anebo <see cref="SaveNow"/> kdykoliv chtějí = po provedení výraznější změny.<br/>
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
            bool isReset = App.HasArgument("reset", false, true);
            string fileName = _GetFileName();
            bool isFile = System.IO.File.Exists(fileName);
            if (isFile)
            {
                if (isReset)
                {
                    App.ShowMessage($"Aplikace je spuštěna s parametrem 'reset', konfigurace z předešlého běhu je ignorována.", System.Windows.Forms.MessageBoxIcon.Warning);
                }
                else
                {   // Máme soubor a není Reset => načteme jej:
                    var persistArgs = new PersistArgs() { XmlFile = fileName };
                    try
                    {
                        var result = Persist.Deserialize(persistArgs);
                        if (result is Settings settings)
                        {
                            settings._RunAfterLoad(fileName);
                            return settings;
                        }
                    }
                    catch (Exception ex)
                    {
                        App.ShowMessage($"Konfigurační soubor {fileName} je poškozen a nelze z něj načíst data programu.\r\n{ex.Message}", System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                }
            }

            var blankSettings = new Settings();
            blankSettings._RunAfterCreate(fileName);
            blankSettings.SaveNow();
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
            if (_TryGetConfigFileFromArguments(out var fileName)) return fileName;

            return System.IO.Path.Combine(App.ConfigPath, File);
        }
        /// <summary>
        /// Metoda se pokusí najít jméno Settings souboru v argumentech aplikace
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool _TryGetConfigFileFromArguments(out string fileName)
        {
            return App.TryGetFileFromArgument("config", out fileName);
        }
        /// <summary>
        /// Implicitní jméno souboru - bez adresáře, s příponou.
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
        #region Okno konfigurace
        /// <summary>
        /// Metoda vytvoří okno <see cref="DialogForm"/>, vloží do něj data své základní konfigurace a okno otevře.
        /// Po ukončení editace v okně uloží konfiguraci.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="formTitle"></param>
        /// <returns></returns>
        public void EditData(Point? startPoint = null, string formTitle = null)
        {
            bool result = false;
            using (var form = new Components.DialogForm())
            {
                var dataControlPanel = this.CreateEditPanel();
                form.DataControl = dataControlPanel;
                form.Text = formTitle ?? "Nastavení aplikace";
                form.StartPosition = FormStartPosition.Manual;
                form.Location = startPoint ?? Control.MousePosition;
                form.ShowDialog(App.MainForm);
                result = (form.DialogResult == DialogResult.OK);
                if (result)
                {
                    // this.AcceptedEditPanel(dataControlPanel);
                }
            }
        }
        protected Components.DataControlPanel CreateEditPanel()
        {
            var panel = new Components.DataControlPanel();

            int x1 = panel.SpacingX;
            int y0 = panel.SpacingY + panel.LabelHeight;
            int w1 = 320;

            int y = y0;
            panel.AddCell(Components.ControlType.ComboBox, App.Messages.EditSettingsAppearanceText, nameof(AppearanceName), x1, ref y, w1, initializer: c => initComboItems(c, AppearanceInfo.Collection));
            panel.AddCell(Components.ControlType.ComboBox, App.Messages.EditSettingsLayoutSetText, nameof(LayoutSetName), x1, ref y, w1, initializer: c => initComboItems(c, LayoutSetInfo.Collection));
            panel.AddCell(Components.ControlType.ComboBox, App.Messages.EditSettingsLanguageText, nameof(LanguageCode), x1, ref y, w1, initializer: c => initComboItems(c, LanguageSet.Collection));
            panel.AddCell(Components.ControlType.CheckBox, App.Messages.EditSettingsMinimizeOnRunText, nameof(MinimizeLauncherAfterAppStart), x1, ref y, w1);

            panel.Buttons = new Components.DialogButtonType[] { Components.DialogButtonType.Ok, Components.DialogButtonType.Cancel };
            panel.BackColor = Color.AntiqueWhite;

            panel.DataObject = this;
            panel.DataStoreAfter += dataStoreAfter;
            return panel;


            void initComboItems(Control control, object[] items)
            {
                if (control is ComboBox comboBox)
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(items);
                }
            }
            void dataStoreAfter(object sender, EventArgs e)
            {
                
            }
        }

        #endregion
        #region Save
        /// <summary>
        /// Uloží data konfigurace do patřičného souboru. 
        /// Uloží je až po nějakém čase (zadaný v parametru anebo <see cref="SaveDelay"/>).
        /// Lze tak volat tuto metodu i stokrát za sekundu, a soubor se fyzicky uloží jen jedenkrát, a to danou dobu po posledním volání této metody.
        /// </summary>
        /// <param name="delay"></param>
        public void SaveDelayed(TimeSpan? delay = null)
        {
            int milisecs = (int)(delay?.TotalMilliseconds ?? SaveDelay.TotalMilliseconds);
            if (milisecs <= 50) milisecs = 50;
            __SaveDelayedGuid = WatchTimer.CallMeAfter(_Save, milisecs, id: __SaveDelayedGuid);
        }
        private Guid? __SaveDelayedGuid;
        /// <summary>
        /// Interval, po kterém proběhne ukládání do souboru po posledním volání metody <see cref="SaveDelayed"/>.
        /// Výchozí hodnota je 0.700 sekundy. Minimální hodnota je 0.100 sekundy.
        /// </summary>
        [PersistingEnabled(false)]
        public TimeSpan SaveDelay 
        { 
            get { return (__SaveDelay ?? TimeSpan.FromMilliseconds(700d)); } 
            set { __SaveDelay = (value.TotalMilliseconds > 100d ? (TimeSpan?)value : (TimeSpan?)null); }
        }
        private TimeSpan? __SaveDelay;
        /// <summary>
        /// Interval, po kterém proběhne ukládání do souboru po nastavení hodnoty <see cref="IsChanged"/> = true.
        /// Výchozí hodnota je 7.000 sekund. Minimální hodnota je 0.100 sekundy.
        /// </summary>
        [PersistingEnabled(false)]
        public TimeSpan AutoSaveDelay
        {
            get { return (__AutoSaveDelay ?? TimeSpan.FromMilliseconds(7000d)); }
            set { __AutoSaveDelay = (value.TotalMilliseconds > 100d ? (TimeSpan?)value : (TimeSpan?)null); }
        }
        private TimeSpan? __AutoSaveDelay;
        /// <summary>
        /// Obsahuje true, pokud data jsou změněna
        /// </summary>
        [PersistingEnabled(false)]
        public bool IsChanged { get { return __IsChanged; } }
        private bool __IsChanged;
        /// <summary>
        /// Nastaví příznak <see cref="IsChanged"/> = true a zahájí odpočet času do provedení AutoSave.
        /// Vyvolá event <see cref="Changed"/>.
        /// </summary>
        public void SetChanged(string changedProperty)
        {
            __IsChanged = true;

            _RunChanged(changedProperty);

            int milisecs = (int)AutoSaveDelay.TotalMilliseconds;
            __AutoSaveDelayedGuid = WatchTimer.CallMeAfter(_Save, milisecs, id: __AutoSaveDelayedGuid);
        }
        private Guid? __AutoSaveDelayedGuid;
        /// <summary>
        /// Uloží data konfigurace do patřičného souboru. Uloží je ihned.
        /// </summary>
        public void SaveNow()
        {
            _Save();
        }
        /// <summary>
        /// Provede uložení dat do souboru, vlastní výkonná metoda.
        /// </summary>
        private void _Save()
        { 
            try
            {
                WatchTimer.Remove(__SaveDelayedGuid);
                __SaveDelayedGuid = null;
                WatchTimer.Remove(__AutoSaveDelayedGuid);
                __AutoSaveDelayedGuid = null;

                _RunBeforeSave();

                string fileName = __FileName;
                if (String.IsNullOrEmpty(fileName)) return;
                string path = System.IO.Path.GetDirectoryName(fileName);
                if (String.IsNullOrEmpty(path)) return;

                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                var persistArgs = Data.PersistArgs.Default;
                persistArgs.XmlFile = fileName;
                Data.Persist.Serialize(this, persistArgs);

                __IsChanged = false;
            }
            catch { }
        }
        #endregion
        #region Základní data konfigurace
        /// <summary>
        /// Název vzhledu = barvy
        /// </summary>
        public string AppearanceName { get { return __AppearanceName; } set { __AppearanceName = value; SetChanged(nameof(AppearanceName)); } } private string __AppearanceName;
        /// <summary>
        /// Název rozložení = velikosti
        /// </summary>
        public string LayoutSetName { get { return __LayoutSetName; } set { __LayoutSetName = value; SetChanged(nameof(LayoutSetName)); } } private string __LayoutSetName;
        /// <summary>
        /// Název jazyka
        /// </summary>
        public string LanguageCode { get { return __LanguageCode; } set { __LanguageCode = value; SetChanged(nameof(LanguageCode)); } } private string __LanguageCode;
        /// <summary>
        /// Minimalizovat Launcher po spuštění aplikace
        /// </summary>
        public bool MinimizeLauncherAfterAppStart { get { return __MinimizeLauncherAfterAppStart; } set { __MinimizeLauncherAfterAppStart = value; SetChanged(nameof(MinimizeLauncherAfterAppStart)); } } private bool __MinimizeLauncherAfterAppStart;

        public bool TrayInfoIsAccepted { get { return __TrayInfoIsAccepted; } set { __TrayInfoIsAccepted = value; SetChanged(nameof(TrayInfoIsAccepted)); } } private bool __TrayInfoIsAccepted;

        /// <summary>
        /// Vyvolá event <see cref="Changed"/>
        /// </summary>
        private void _RunChanged(string changedProperty)
        {
            SettingChangedEventArgs args = new SettingChangedEventArgs(changedProperty);
            OnChanged(args);
            Changed?.Invoke(this, args);
        }
        /// <summary>
        /// Po změně dat
        /// </summary>
        protected virtual void OnChanged(SettingChangedEventArgs args) { }
        /// <summary>
        /// Proběhne po změně dat
        /// </summary>
        public event EventHandler<SettingChangedEventArgs> Changed;
        /// <summary>
        /// Data pro handler události <see cref="Changed"/>
        /// </summary>
        public class SettingChangedEventArgs : EventArgs
        {
            public SettingChangedEventArgs(string changedProperty)
            {
                ChangedProperty = changedProperty;
            }
            /// <summary>
            /// Property, jejích obsaz se změnil
            /// </summary>
            public string ChangedProperty { get; private set; }
        }
        #endregion
    }
}
