// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

using DxfData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using Noris.Clients.Win.Components.AsolDX.DataForm.Data;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxDataFormPanel : vnější panel DataForm - virtuální container
    /// <summary>
    /// <see cref="DxDataFormPanel"/> : vnější panel DataForm - virtuální container.
    /// Obsahuje vnitřní ContentPanel typu <see cref="DxDataFormContentPanel"/>, který reálně zobrazuje obsah (ve spolupráci s this třídou řeší scrollování i Zoom).
    /// Obsahuje kolekci řádků <see cref="DataFormRows"/> a deklaraci layoutu <see cref="DataFormLayoutSet"/>.
    /// Obsahuje managera fyzických controlů (obdoba RepositoryEditorů) <see cref="DxRepositoryManager"/>
    /// </summary>
    internal class DxDataFormPanel : DxVirtualPanel
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        internal DxDataFormPanel()
        {
            _InitLayout();
            _InitContent();
            __Initialized = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _DisposeContent();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Obsahuje true po skončení inicializace
        /// </summary>
        private bool __Initialized;
        /// <summary>
        /// Po změně velikosti nebo scrollbarů ve virtual panelu zajistíme přepočet interaktivních prvků
        /// </summary>
        protected override void OnVisibleDesignBoundsChanged()
        {
            _DataForm?.OnVisibleDesignBoundsChanged();
        }
        #endregion
        #region Napojení na data DataFormu
        /// <summary>
        /// Datová základna DataFormu
        /// </summary>
        internal DxDataForm DataForm 
        { 
            get 
            {
                if (__DataForm is null)
                    __DataForm = new DxDataForm(this);
                return __DataForm;
            }
            set
            {
                if (__DataForm != null)
                    __DataForm.DataFormPanel = null;

                __DataForm = value;

                if (__DataForm != null)
                    __DataForm.DataFormPanel = this;

            }
        }
        /// <summary>
        /// Datová základna DataFormu
        /// </summary>
        private DxDataForm _DataForm { get { return __DataForm; } }
        private DxDataForm __DataForm;

        /// <summary>
        /// Už máme připravenou datovou základnu DataFormu?
        /// </summary>
        private bool _HasDataForm { get { return (__DataForm != null && __DataForm.IsPrepared); } }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky, platné pro aktuální layout a řádky a pozici Scrollbaru. Validní hodnota.
        /// </summary>
        internal IList<IInteractiveItem> InteractiveItems { get { return (_HasDataForm ? _DataForm.InteractiveItems : null); } }
        /// <summary>
        /// Proběhne po změně Zoomu za běhu aplikace. Může dojít k invalidaci cachovaných souřadnic prvků.
        /// </summary>
        protected override void OnInvalidatedZoom()
        {
            base.OnInvalidatedZoom();
            if (_HasDataForm) _DataForm.InvalidateRepozitory();
        }
        /// <summary>
        /// Po změně skinu
        /// </summary>
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            if (_HasDataForm) _DataForm.InvalidateRepozitory();
        }
        /// <summary>
        /// Invaliduje základní vizuální informace o DesignSize
        /// </summary>
        /// <param name="refreshLayout"></param>
        internal void InvalidateVisualDesignSize(bool refreshLayout)
        {
            base.DesignSizeInvalidate(refreshLayout);
        }
        #endregion
        #region ContentPanel : zobrazuje vlastní obsah (grafická komponenta)
        /// <summary>
        /// Používat testovací vykreslování
        /// </summary>
        internal bool TestPainting { get { return __TestPainting; } set { __TestPainting = value; this.DrawContent(); } } private bool __TestPainting;
        /// <summary>
        /// Zajistí znovuvykreslení panelu s daty
        /// </summary>
        internal void DrawContent()
        {
            this.DataFormContent?.Draw();
        }
        /// <summary>
        /// Inicializace Content panelu
        /// </summary>
        private void _InitContent()
        {
            __DataFormContent = new DxDataFormContentPanel();
            this.ContentPanel = __DataFormContent;
        }
        /// <summary>
        /// Zahodí obsah
        /// </summary>
        private void _DisposeContent()
        {
            this.ContentPanel = null;
            this.DataForm = null;
        }
        /// <summary>
        /// ContentPanel (<see cref="DxDataFormContentPanel"/>), v něm se fyzicky zobrazují obrazy a controly DataFormu
        /// </summary>
        internal DxDataFormContentPanel DataFormContent { get { return __DataFormContent; } }
        /// <summary>
        /// ContentPanel (<see cref="DxDataFormContentPanel"/>), v něm se fyzicky zobrazují obrazy a controly DataFormu
        /// </summary>
        private DxDataFormContentPanel __DataFormContent;
        #endregion
        #region ContentDesignSize : velikost obsahu v designových pixelech
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech. Validovaná hodnota.
        /// </summary>
        public override WinDraw.Size? ContentDesignSize { get { return _HasDataForm ? (WinDraw.Size?)_DataForm.ContentDesignSize : null; } set { } }
        /// <summary>
        /// Po změně hodnoty <see cref="DxVirtualPanel.ContentPanelDesignSize"/>
        /// </summary>
        protected override void OnContentPanelDesignSizeChanged()
        {
            base.OnContentPanelDesignSizeChanged();
            if (_HasDataForm) _DataForm.InvalidateContentDesignSize(false, false);
        }
        /// <summary>
        /// Invaliduje celkovou velikost <see cref="ContentDesignSize"/> a navazující <see cref="DxVirtualPanel.ContentVirtualSize"/>.
        /// Provádí se po změně řádků nebo definice designu.
        /// Volitelně provede i <see cref="DxVirtualPanel.RefreshInnerLayout"/>
        /// </summary>
        /// <param name="refreshLayout">Vyvolat poté metodu <see cref="DxVirtualPanel.RefreshInnerLayout"/></param>
        protected override void DesignSizeInvalidate(bool refreshLayout)
        {
            base.DesignSizeInvalidate(refreshLayout);
            if (_HasDataForm) _DataForm.InvalidateContentDesignSize(true, false);
            if (refreshLayout) this.DataFormContent?.Draw();
        }
        /// <summary>
        /// Inicializuje layout samotného panelu
        /// </summary>
        private void _InitLayout()
        {
            Padding = new WinForm.Padding(0);
        }
        /// <summary>
        /// Změna Padding vede ke změně <see cref="ContentDesignSize"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaddingChanged(EventArgs e)
        {
            DesignSizeInvalidate(true);
        }
        #endregion
    }
    #endregion
    #region DxDataFormContentPanel : fyzický grafický a interaktivní panel pro zobrazení contentu DataFormu
    /// <summary>
    /// <see cref="DxDataFormContentPanel"/> : fyzický grafický a interaktivní panel pro zobrazení contentu DataFormu.
    /// Řeší grafické vykreslení prvků a řeší interaktivitu myši a klávesnice.
    /// Pro fyzické vykreslení obsahu prvku volá vlastní metodu konkrétního prvku <see cref="IInteractiveItem.Paint(PaintDataEventArgs)"/>, to neřeší sám panel.
    /// <para/>
    /// Tato třída pokud možno přenáší svoje požadavky do svého parenta = <see cref="DataFormPanel"/> a sama by měla být co nejjednodušší.
    /// Slouží primárně jen jako zobrazovač a interaktivní koordinátor.
    /// </summary>
    internal class DxDataFormContentPanel : DxInteractivePanel
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        internal DxDataFormContentPanel() { }
        /// <summary>
        /// Metoda zajistí, že velikost <see cref="DxInteractivePanel.ContentDesignSize"/> bude platná (bude odpovídat souhrnu velikosti prvků).
        /// Metoda je volána před každým Draw tohoto objektu.
        /// </summary>
        protected override void ContentDesignSizeCheckValidity(bool force = false)
        {
            // Zde jsme ve třídě DxDataFormContentPanel, kde zodpovědnost za velikost ContentDesignSize nese náš Parent = třída DxDataFormPanel.
            // Zde jsme voláni v čase Draw() tohoto ContentPanelu, a to je už mírně pozdě na přepočty ContentDesignSize, protože Parent už má svůj layout vytvořený.
            // Validaci i uchování hodnoty ContentDesignSize provádí parent (DxDataFormPanel), validaci volá před svým vykreslením.
            // Proto my vůbec nevoláme:
            //    base.ContentDesignSizeCheckValidity(force);
            // - protože by šlo o nadbytečnou akci.
            // Base třída by si napočítala ContentDesignSize ze svých InteractiveItems, které rozhodně netvoří celý obsazený prostor.
            // DataForm plní fyzické InteractiveItems pouze pro potřebné viditelné řádky, ale ContentDesignSize odpovídá všem řádkům.
        }
        #endregion
        #region Napojení zdejšího interaktivního panelu na zdroje v parentu DxDataFormPanel
        /// <summary>
        /// Hostitelský panel <see cref="DxDataFormPanel"/>; ten řeší naprostou většinu našich požadavků.
        /// My jsme jen jeho zobrazovací plocha.
        /// </summary>
        protected DxDataFormPanel DataFormPanel { get { return this.VirtualPanel as DxDataFormPanel; } }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky.
        /// Třída <see cref="DxDataFormContentPanel"/> zde vrací pole z Parenta <see cref="DxDataFormPanel.InteractiveItems"/>.
        /// </summary>
        protected override IList<IInteractiveItem> ItemsAll { get { return DataFormPanel?.InteractiveItems as IList<IInteractiveItem>; } }
        #endregion
    }
    #endregion
}
