using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using XmlSerial = DjSoft.App.iCollect.Data.XmlSerializer;

namespace DjSoft.App.iCollect.Application
{
    internal class Settings
    {

        /// <summary>
        /// Vizuální styl uložený v konfiguraci.
        /// Změna v této property nezmění aktuální vizuální styl.
        /// </summary>
        public DxVisualStyle VisualStyle { get { return __VisualStyle; } set { _Set(ref __VisualStyle, value); } } private DxVisualStyle __VisualStyle;

        #region Provedení změny hodnoty, načtení a ukládání configu do jeho souboru

        /// <summary>
        /// Uloží do proměnné hodnotu, a pokud je hodnota změněna, pak zavolá <see cref="Save"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        private void _Set<T>(ref T variable, T value)
        {
            bool isEquals = (Object.Equals(variable, value));
            variable = value;
            if (!isEquals)
            {
                if (!__IsSuppressSave)
                    this._Save();
                else
                    __ContainChanges = true;
            }
        }
        /// <summary>
        /// Uloží svoje data do konfigurace
        /// </summary>
        public void Save()
        {
            this._Save();
        }
        /// <summary>
        /// Pro persistor
        /// </summary>
        private Settings() { }
        /// <summary>
        /// Konstruktor, zajistí načtení dat, pokud existuje odpovídající soubor
        /// </summary>
        /// <param name="configPath"></param>
        public Settings(string configPath)
        {
            __ConfigFile = (!String.IsNullOrEmpty(configPath) ? System.IO.Path.Combine(configPath, ConfigName) : "");
            this._Load();
        }
        /// <summary>
        /// Načte do this instance data z Configu
        /// </summary>
        private void _Load()
        {
            string configFile = __ConfigFile;
            if (!String.IsNullOrEmpty(configFile) && System.IO.File.Exists(configFile))
            {
                try
                {
                    __IsSuppressSave = true;
                    var args = new XmlSerial.PersistArgs() { XmlFile = __ConfigFile };
                    XmlSerial.Persist.LoadTo(args, this);
                }
                catch { }
                finally
                {
                    __ContainChanges = false;
                    __IsSuppressSave = false;
                }
            }
        }
        /// <summary>
        /// Uloží aktuální data
        /// </summary>
        private void _Save()
        {
            string configFile = __ConfigFile;
            if (!String.IsNullOrEmpty(configFile))
            {
                try
                {
                    if (MainApp.TryPrepareAppPath(System.IO.Path.GetDirectoryName(configFile)))
                    {
                        __IsSuppressSave = true;
                        var args = new XmlSerial.PersistArgs() { XmlFile = __ConfigFile };
                        XmlSerial.Persist.Serialize(this, args);
                    }
                }
                catch { }
                finally
                {
                    __ContainChanges = false;
                    __IsSuppressSave = false;
                }
            }
        }
        private const string ConfigName = "Settings.ini";
        private string __ConfigFile;
        /// <summary>
        /// Pokud je true, pak po změně jakékoli hodnoty nebudeme provádět Save. Typicky proto, že právě probíhá Load.
        /// </summary>
        private bool __IsSuppressSave;
        /// <summary>
        /// Pokud je true, pak objekt obsahuje neuložené změny, které byly provedeny do property, ale nemohly být uloženy z důvodu <see cref="__IsSuppressSave"/>.
        /// </summary>
        private bool __ContainChanges;
        #endregion
    }

    #region class DxStyleToConfigListener : spojovací prvek mezi stylem (skin + paleta) z DevExpress a (abstract) konfigurací
    /// <summary>
    /// <see cref="DxVisualStyleManager"/> : spojovací prvek mezi stylem (skin + paleta) z DevExpress a konfigurací.
    /// </summary>
    public class DxVisualStyleManager
    {
        /// <summary>
        /// Konstruktor s provedením automatické inicializace.
        /// Tedy v rámci konstruktoru jsou načteny hodnoty z konfigurace as aplikovány do GUI.
        /// </summary>
        public DxVisualStyleManager()
            : this(true)
        { }
        /// <summary>
        /// Konstruktor s možností automatické inicializace (<paramref name="withInitialize"/> = true).
        /// Pokud v době konstruktoru je již funkční konfigurace, může se předat true a bude rovnou nastaven i skin.
        /// Pokud není konfigurace k dispozici, předá se false a explicitně se pak vyvolá metoda <see cref="_ApplyConfigVisualStyle"/>.
        /// </summary>
        /// <param name="withInitialize">Požadavek true na provedení inicializace</param>
        public DxVisualStyleManager(bool withInitialize)
        {
            __IsInitialized = false;
            __IsSupressEvent = false;
            __LastVisualStyle = null;

            if (withInitialize)
                _ActivateStyle(this._ConfigVisualStyle, true, false);

            DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += _DevExpress_StyleChanged;
        }
        /// <summary>
        /// Nastaví se na true po inicializaci. Dokud je false, nebudeme řešit změny skinu.
        /// </summary>
        private bool __IsInitialized;
        /// <summary>
        /// Pokud obsahuje true, pak nebudeme reagovat na změny skinu v <see cref="IListenerStyleChanged.StyleChanged()"/> = provádíme je my sami!
        /// </summary>
        private bool __IsSupressEvent;
        /// <summary>
        /// Posledně uložený / načtený vizuální styl, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged"/> i když k reálné změně nedochází.
        /// </summary>
        private DxVisualStyle __LastVisualStyle;

        /// <summary>
        /// Vizuální styl uložený v Configu v <see cref="MainApp.Settings"/>.
        /// <para/>
        /// Setování hodnoty nezmění aktuální vizuální GUI, pouze uloží zadaný styl do Configu. Ani Config nevyvolá změnu GUI stylu.<br/>
        /// Pokud chceme změnit GUI vizuální styl, setujme hodnotu do <see cref="CurrentVisualStyle"/>.
        /// </summary>
        private DxVisualStyle _ConfigVisualStyle { get { return MainApp.Settings.VisualStyle; } set { MainApp.Settings.VisualStyle = value; } }
        /// <summary>
        /// Aktuální GUI vizuální styl. Vždy je načten z GUI. Setování vepíše hodnoty do GUI a následně vyvolá event o změně a uložení do Configu.
        /// </summary>
        public DxVisualStyle CurrentVisualStyle { get { return _ReadCurrentStyle(out var _); } set { _ActivateStyle(value, false, true); } }
        /// <summary>
        /// Aktivuje v GUI rozhraní explicitně daný Skin.
        /// Podle hodnoty <paramref name="storeToConfig"/> jej uloží i do konfigurace (do properties <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>), jako by jej vybral uživatel.
        /// </summary>
        /// <param name="visualStyle">Vizuální styl</param>
        /// <param name="force">Aktivovat povinně, i když není změna</param>
        /// <param name="storeToConfig">Uložit do Settings</param>
        private void _ActivateStyle(DxVisualStyle visualStyle, bool force, bool storeToConfig)
        {
            bool isActivated = false;

            if (visualStyle != null && (force || !DxVisualStyle.IsEquals(visualStyle, this.__LastVisualStyle)))
            {   // Pokud je zadán vizuální styl, a (setování je povinné nebo nový styl se ličí od dosavadního):
                try
                {
                    __IsSupressEvent = true;

                    if (!String.IsNullOrEmpty(visualStyle.SkinName))
                    {   // https://docs.devexpress.com/WindowsForms/2399/build-an-application/skins?utm_source=SupportCenter&utm_medium=website&utm_campaign=docs-feedback&utm_content=T1093158#how-to-re-apply-the-last-active-skin-when-an-application-restarts
                        DevExpress.LookAndFeel.UserLookAndFeel.ForceCompactUIMode(visualStyle.SkinCompact, false);
                        string paletteName = (!String.IsNullOrEmpty(visualStyle.SkinPaletteName) ? visualStyle.SkinPaletteName : "DefaultSkinPalette");
                        DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(visualStyle.SkinName, paletteName);
                    }
                    __LastVisualStyle = visualStyle;

                    isActivated = true;
                }
                catch { /* Pokud by na vstupu byly nepřijatelné hodnoty, neshodím kvůli tomu aplikaci... */ }
                finally
                {
                    __IsSupressEvent = false;
                }

                if (storeToConfig && isActivated)
                    this._ConfigVisualStyle = visualStyle;
            }

            __IsInitialized = true;
        }
        /// <summary>
        /// Načte a vrátí aktuální jméno skinu a SVG palety
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="skinName"></param>
        /// <param name="isCompact"></param>
        /// <param name="paletteName"></param>
        private static DxVisualStyle _ReadCurrentStyle(out DevExpress.Skins.Skin skin)
        {
            DxVisualStyle currentStyle = null;
            skin = null;
            try
            {
                var ulaf = DevExpress.LookAndFeel.UserLookAndFeel.Default;
                skin = DevExpress.Skins.CommonSkins.GetSkin(ulaf);
                var skinName = (ulaf.ActiveSkinName ?? "").Trim();
                var isCompact = ulaf.CompactUIModeForced;
                var skinPaletteName = (ulaf.ActiveSvgPaletteName ?? "").Trim();
                currentStyle = new DxVisualStyle(skinName, isCompact, skinPaletteName);
            }
            catch (Exception)
            { }
            return currentStyle;
        }
        /// <summary>
        /// Handler události, kdy DevExpress změní styl (skin)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DevExpress_StyleChanged(object sender, EventArgs e)
        {
            OnStyleChanged();
        }
        /// <summary>
        /// Po změně skinu / palety v GUI
        /// </summary>
        private void OnStyleChanged()
        {
            if (__IsInitialized && !__IsSupressEvent)
            {
                var currentStyle = this.CurrentVisualStyle;
                var lastStyle = this.__LastVisualStyle;
                if (!DxVisualStyle.IsEquals(currentStyle, lastStyle))
                    this._ConfigVisualStyle = currentStyle;
            }
        }
    }
    /// <summary>
    /// Obsahuje statický popis skinu, kompaktu, a palety.
    /// Neřeší jeho načtení ani aplikaci ani hlídání změny, na to máme <see cref="DxVisualStyleManager"/>.
    /// </summary>
    public class DxVisualStyle
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="skinPaletteName"></param>
        public DxVisualStyle(string skinName, bool skinCompact, string skinPaletteName)
        {
            this.SkinName = skinName;
            this.SkinCompact = skinCompact;
            this.SkinPaletteName = skinPaletteName;
        }
        /// <summary>
        /// Pro persistor
        /// </summary>
        private DxVisualStyle() { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text;
        }
        /// <summary>
        /// Standardní Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Text.GetHashCode();
        }
        /// <summary>
        /// Standardní Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is DxVisualStyle other)
                return IsEquals(this, other);
            return false;
        }
        /// <summary>
        /// Vrací true, pokud dva objekty obsahují shodná data, nebo pokud oba jsou null.
        /// Vrací false, pokud obsahuj odlišná data, nebo jeden je null a druhý není.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsEquals(DxVisualStyle a, DxVisualStyle b)
        {
            bool an = a is null;
            bool bn = b is null;
            if (an && bn) return true;
            if (an || bn) return false;
            return String.Equals(a.Text, b.Text);
        }
        /// <summary>
        /// Textové vyjádření
        /// </summary>
        public string Text
        {
            get
            {
                string text = this.SkinName;
                if (this.SkinCompact) text += " (Compact)";
                if (!String.IsNullOrEmpty(this.SkinPaletteName)) text += ": " + this.SkinPaletteName;
                return text;
            }
        }
        /// <summary>
        /// Jméno Skinu.
        /// </summary>
        public string SkinName { get; private set; }
        /// <summary>
        /// Příznak kompaktního Skinu.
        /// </summary>
        public bool SkinCompact { get; private set; }
        /// <summary>
        /// Jméno palety.
        /// </summary>
        public string SkinPaletteName { get; private set; }

    }
    #endregion
}
