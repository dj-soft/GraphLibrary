using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.App.iCollect.Application
{
    internal class Settings
    {
        /// <summary>
        /// Uloží jméno skinu a palety do konfigurace = do <see cref="SkinName"/> a <see cref="SkinPaletteName"/>.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        internal void StoreSkin(string skinName, bool skinCompact, string paletteName)
        {
            __SkinName = skinName;
            __SkinCompact = skinCompact;
            __SkinPaletteName = paletteName;
            this.Save();
        }
        /// <summary>
        /// Jméno Skinu.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public string SkinName { get { return __SkinName; } set { _Set(ref __SkinName, value); } }
        private string __SkinName;
        /// <summary>
        /// Příznak kompaktního Skinu.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public bool SkinCompact { get { return __SkinCompact; } set { _Set(ref __SkinCompact, value); } }
        private bool __SkinCompact;
        /// <summary>
        /// Jméno palety.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public string SkinPaletteName { get { return __SkinPaletteName; } set { _Set(ref __SkinPaletteName, value); } }
        private string __SkinPaletteName;

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
            if (!isEquals) this.Save();
        }
        /// <summary>
        /// Uloží svoje data do konfigurace
        /// </summary>
        public void Save()
        { }
    }

    #region class DxStyleToConfigListener : spojovací prvek mezi stylem (skin + paleta) z DevExpress a (abstract) konfigurací
    /// <summary>
    /// <see cref="DxStyleManager"/> : spojovací prvek mezi stylem (skin + paleta) z DevExpress a konfigurací.
    /// </summary>
    public class DxStyleManager
    {
        /// <summary>
        /// Konstruktor s provedením automatické inicializace.
        /// Tedy v rámci konstruktoru jsou načteny hodnoty z konfigurace as aplikovány do GUI.
        /// </summary>
        public DxStyleManager()
            : this(true)
        { }
        /// <summary>
        /// Konstruktor s možností automatické inicializace (<paramref name="withInitialize"/> = true).
        /// Pokud v době konstruktoru je již funkční konfigurace, může se předat true a bude rovnou nastaven i skin.
        /// Pokud není konfigurace k dispozici, předá se false a explicitně se pak vyvolá metoda <see cref="ActivateConfigStyle"/>.
        /// </summary>
        /// <param name="withInitialize">Požadavek true na provedení inicializace</param>
        public DxStyleManager(bool withInitialize)
        {
            __IsInitialized = false;
            __IsSupressEvent = false;
            __LastSkinName = null;
            __LastSkinCompact = null;
            __LastSkinPaletteName = null;

            DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += _DevExpress_StyleChanged;

            if (withInitialize)
                ActivateConfigStyle();
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
        /// Posledně uložený / načtený název skinu, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="IListenerStyleChanged.StyleChanged()"/> i když k reálné změně nedochází.
        /// </summary>
        private string __LastSkinName;
        /// <summary>
        /// Posledně uložený / načtený příznak Compact skinu, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="IListenerStyleChanged.StyleChanged()"/> i když k reálné změně nedochází.
        /// </summary>
        private bool? __LastSkinCompact;
        /// <summary>
        /// Posledně uložený / načtený název palety, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="IListenerStyleChanged.StyleChanged()"/> i když k reálné změně nedochází.
        /// </summary>
        private string __LastSkinPaletteName;

        /// <summary>
        /// Jméno Skinu.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>.
        /// </summary>
        public string SkinName { get { return App.Settings.SkinName; } }
        /// <summary>
        /// Příznak kompaktního Skinu.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>.
        /// </summary>
        public bool SkinCompact { get { return App.Settings.SkinCompact; } }
        /// <summary>
        /// Jméno palety.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>.
        /// </summary>
        public string SkinPaletteName { get { return App.Settings.SkinPaletteName; } }
        /// <summary>
        /// Aktivuje v GUI rozhraní skin daný <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>.
        /// </summary>
        public void ActivateConfigStyle()
        {
            __IsInitialized = true;

            string skinName = SkinName;
            bool skinCompact = SkinCompact;
            string skinPaletteName = SkinPaletteName;
            _ActivateStyle(skinName, skinCompact, skinPaletteName, false);
            __LastSkinName = skinName;
            __LastSkinCompact = skinCompact;
            __LastSkinPaletteName = skinPaletteName;
        }
        /// <summary>
        /// Aktivuje v GUI rozhraní explicitně daný Skin.
        /// Uloží jej i do konfigurace (do properties <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>), jako by jej vybral uživatel.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        public void ActivateStyle(string skinName, bool skinCompact, string paletteName)
        {
            _ActivateStyle(skinName, skinCompact, paletteName, true);
        }
        /// <summary>
        /// Aktivuje v GUI rozhraní explicitně daný Skin.
        /// Podle hodnoty <paramref name="storeToConfig"/> jej uloží i do konfigurace (do properties <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="SkinPaletteName"/>), jako by jej vybral uživatel.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        /// <param name="storeToConfig"></param>
        private void _ActivateStyle(string skinName, bool skinCompact, string paletteName, bool storeToConfig)
        {
            bool isActivated = false;
            try
            {
                __IsSupressEvent = true;

                if (!String.IsNullOrEmpty(skinName))
                {   // https://docs.devexpress.com/WindowsForms/2399/build-an-application/skins?utm_source=SupportCenter&utm_medium=website&utm_campaign=docs-feedback&utm_content=T1093158#how-to-re-apply-the-last-active-skin-when-an-application-restarts
                    DevExpress.LookAndFeel.UserLookAndFeel.ForceCompactUIMode(skinCompact, false);
                    if (String.IsNullOrEmpty(paletteName)) paletteName = "DefaultSkinPalette";
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(skinName, paletteName);
                }

                /*
                DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skinName;
                if (!String.IsNullOrEmpty(paletteName))
                {   // https://supportcenter.devexpress.com/ticket/details/t827424/save-and-restore-svg-palette-name
                    var skin = DevExpress.Skins.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
                    if (skin.CustomSvgPalettes.Count > 0)
                    {
                        var palette = skin.CustomSvgPalettes[paletteName];               // Když není nalezena, vrátí se null, nikoli Exception
                        if (palette != null)
                            skin.SvgPalettes[DevExpress.Skins.Skin.DefaultSkinPaletteName].SetCustomPalette(palette);
                    }
                }
                */

                isActivated = true;
            }
            catch { /* Pokud by na vstupu byly nepřijatelné hodnoty, neshodím kvůli tomu aplikaci... */ }
            finally
            {
                __IsSupressEvent = false;
            }

            if (storeToConfig && isActivated)
                _StoreToConfig(skinName, skinCompact, paletteName);
        }

        /// <summary>
        /// Načte a vrátí aktuální jméno skinu a SVG palety
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="isCompact"></param>
        /// <param name="paletteName"></param>
        internal static void ReadCurrentSkinPalette(out string skinName, out bool isCompact, out string paletteName)
        {
            ReadCurrentSkinPalette(out var _, out skinName, out isCompact, out paletteName);
        }
        /// <summary>
        /// Načte a vrátí aktuální jméno skinu a SVG palety
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="skinName"></param>
        /// <param name="isCompact"></param>
        /// <param name="paletteName"></param>
        internal static void ReadCurrentSkinPalette(out DevExpress.Skins.Skin skin, out string skinName, out bool isCompact, out string paletteName)
        {
            skin = null;
            skinName = null;
            isCompact = false;
            paletteName = null;
            try
            {
                var ulaf = DevExpress.LookAndFeel.UserLookAndFeel.Default;
                skin = DevExpress.Skins.CommonSkins.GetSkin(ulaf);
                skinName = (ulaf.ActiveSkinName ?? "").Trim();
                isCompact = ulaf.CompactUIModeForced;
                paletteName = (ulaf.ActiveSvgPaletteName ?? "").Trim();
            }
            catch (Exception)
            { }
        }



        /// <summary>
        /// Uloží jméno skinu a palety do konfigurace = do <see cref="SkinName"/> a <see cref="SkinPaletteName"/>.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        private void _StoreToConfig(string skinName, bool skinCompact, string paletteName)
        {
            App.Settings.StoreSkin(skinName, skinCompact, paletteName);
            __LastSkinName = skinName;
            __LastSkinCompact = skinCompact;
            __LastSkinPaletteName = paletteName;
        }
        /// <summary>
        /// Handler události, kdy DevExpress změní styl (skin)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DevExpress_StyleChanged(object sender, EventArgs e)
        {
            _StyleChanged();
        }
        /// <summary>
        /// Po změně skinu / palety v GUI
        /// </summary>
        void _StyleChanged()
        {
            if (__IsInitialized && !__IsSupressEvent)
            {
                ReadCurrentSkinPalette(out string skinName, out bool isCompact, out string paletteName);
                if (!String.Equals(skinName, __LastSkinName) || (((bool?)isCompact) != __LastSkinCompact) || !String.Equals(paletteName, __LastSkinPaletteName))
                    _StoreToConfig(skinName, isCompact, paletteName);
            }
        }
    }
    #endregion
}
