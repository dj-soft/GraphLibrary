﻿using DjSoft.Tools.ProgramLauncher.Data;
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

            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);  // C:\Users\{userName}\AppData\Local
            var settingPath = System.IO.Path.Combine(dataPath, App.Company, App.ProductName);
            return System.IO.Path.Combine(settingPath, File);
        }
        /// <summary>
        /// Metoda se pokusí najít jméno Settings souboru v argumentech aplikace
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool _TryGetConfigFileFromArguments(out string fileName)
        {
            fileName = null;
            if (!App.TryGetArgument("config", out string argument)) return false;
            // Pokud v argumentech je např.:
            // __reset__ maximized config="C:\DavidPrac\WindowsUI/data aplikací.cfg" QX
            // pak v proměnné 'argument' je:
            // config=C:\DavidPrac\WindowsUI/data aplikací.cfg
            if (argument.Length < 10) return false;
            argument = argument.Substring(6).Trim();                   // Odeberu text  'config'
            if (argument.StartsWith("=") || argument.StartsWith(":"))
                argument = argument.Substring(1).Trim();               // Odeberu znak '='   nebo   ':'
            // Operační systém sám odebral případné uvozovky okolo souboru, takže očekávám, že v 'argument' nyní zůstalo:   C:\DavidPrac\WindowsUI/data aplikací.cfg

            if (argument.Contains("/")) argument = argument.Replace("/", "\\");
            if (Uri.TryCreate(argument, UriKind.RelativeOrAbsolute, out var uri) && uri.IsFile)
            {
                if (uri.IsAbsoluteUri)
                    fileName = uri.LocalPath;
                else
                    fileName = System.IO.Path.Combine(App.ApplicationPath, argument);
                return true;
            }

            return false;
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
        public string AppearanceName { get { return __AppearanceName; } set { __AppearanceName = value; SetChanged(nameof(AppearanceName)); } } private string __AppearanceName;
        public string LayoutSetName { get { return __LayoutSetName; } set { __LayoutSetName = value; SetChanged(nameof(LayoutSetName)); } } private string __LayoutSetName;
        public string LanguageCode { get { return __LanguageCode; } set { __LanguageCode = value; SetChanged(nameof(LanguageCode)); } } private string __LanguageCode;
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
