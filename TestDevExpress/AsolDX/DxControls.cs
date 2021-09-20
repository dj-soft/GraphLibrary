// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SWF = System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Drawing.Drawing2D;
using DevExpress.Pdf.Native;
using DevExpress.XtraPdfViewer;
using DevExpress.XtraEditors;
using DevExpress.Office.History;
using System.Diagnostics;
using DevExpress.XtraEditors.Filtering.Templates;
using System.Windows.Forms;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors.ViewInfo;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxStdForm
    /// <summary>
    /// Základní formulář bez Ribbonu a StatusBaru
    /// </summary>
    public class DxStdForm : DevExpress.XtraEditors.XtraForm, IListenerZoomChange, IListenerStyleChanged, IListenerApplicationIdle
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxStdForm()
        {
            this.IconOptions.SvgImage = DxComponent.GetSvgImage(DxComponent.ImageNameFormIcon);
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxComponent.UnregisterListener(this);
        }
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        #endregion
    }
    #endregion
    #region DxRibbonForm
    /// <summary>
    /// Formulář s ribbonem.
    /// Obsahuje připravený Ribbon <see cref="DxRibbon"/> a připravený StatusBar <see cref="DxStatusBar"/>, 
    /// a hlavní Panel nacházející se mzi Ribbonem a StatusBarem <see cref="DxMainPanel"/>.
    /// </summary>
    public class DxRibbonForm : DevExpress.XtraBars.Ribbon.RibbonForm, IListenerZoomChange, IListenerStyleChanged, IListenerApplicationIdle
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonForm()
        {
            this.IconOptions.SvgImage = DxComponent.GetSvgImage(DxComponent.ImageNameFormIcon);
            this.InitDxRibbonForm();
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxComponent.UnregisterListener(this);
        }
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        #endregion
        #region Ribbon, MainPanel a StatusBar
        /// <summary>
        /// Ribbon
        /// </summary>
        public DxRibbonControl DxRibbon { get { return _DxRibbon; } }
        /// <summary>
        /// Hlavní panel, mezi Ribbonem a StatusBarem
        /// </summary>
        public DxPanelControl DxMainPanel { get { return _DxMainPanel; } }
        /// <summary>
        /// Status bar
        /// </summary>
        public DxRibbonStatusBar DxStatusBar { get { return _DxStatusBar; } }
        /// <summary>
        /// Inicializace Ribbonu a StatusBaru. Volá se v konstruktoru <see cref="DxRibbonForm"/>.
        /// </summary>
        protected virtual void InitDxRibbonForm()
        {
            this._DxMainPanel = DxComponent.CreateDxPanel(this, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);

            this._DxRibbon = new DxRibbonControl() { Visible = true };
            this.Ribbon = _DxRibbon;
            this.Controls.Add(this._DxRibbon);

            this._DxStatusBar = new DxRibbonStatusBar() { Visible = true };
            this._DxStatusBar.Ribbon = this._DxRibbon;
            this.StatusBar = _DxStatusBar;
            this.Controls.Add(this._DxStatusBar);

            this.DxRibbonPrepare();
            this.DxStatusPrepare();
        }
        /// <summary>
        /// Provede přípravu obsahu Ribbonu.
        /// Pozor: Bázová třída <see cref="DxRibbonForm"/> nastaví <see cref="DxRibbon"/>.Visible = false; !!!
        /// To proto, když by potomek nijak s Ribbonem nepracoval, pak nebude Ribbon zobrazen.
        /// </summary>
        protected virtual void DxRibbonPrepare() { this._DxRibbon.Visible = false; }
        /// <summary>
        /// Provede přípravu obsahu Ribbonu.
        /// Pozor: Bázová třída <see cref="DxRibbonForm"/> nastaví <see cref="DxStatusBar"/>.Visible = false; !!!
        /// To proto, když by potomek nijak se StatusBarem nepracoval, pak nebude StatusBar zobrazen.
        /// </summary>
        protected virtual void DxStatusPrepare() { this._DxStatusBar.Visible = false; }
        private DxRibbonControl _DxRibbon;
        private DxPanelControl _DxMainPanel;
        private DxRibbonStatusBar _DxStatusBar;
        #endregion
    }
    #endregion
    #region DxPanelControl
    /// <summary>
    /// PanelControl
    /// </summary>
    public class DxPanelControl : DevExpress.XtraEditors.PanelControl, IListenerZoomChange, IListenerStyleChanged, IListenerApplicationIdle
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxPanelControl()
        {
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.Margin = new SWF.Padding(0);
            this.Padding = new SWF.Padding(0);
            this.LogActive = false;
            this._CurrentDpi = DxComponent.DesignDpi;
            this._LastDpi = DxComponent.DesignDpi;           // ??? anebo   0 ?
            this.AllowTransparency = true;
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxComponent.UnregisterListener(this);
        }
        /// <summary>
        /// Barva pozadí uživatelská, má přednost před skinem, aplikuje se na hotový skin, může obsahovat Alpha kanál = pak skrz tuto barvu prosvítá podkladový skin
        /// </summary>
        public Color? BackColorUser { get { return _BackColorUser; } set { _BackColorUser = value; Invalidate(); } }
        private Color? _BackColorUser;
        /// <summary>
        /// Počet pixelů aktuálního rámečku (na každé straně)
        /// </summary>
        public int BorderWidth
        {
            get
            {
                switch (this.BorderStyle)
                {
                    case DevExpress.XtraEditors.Controls.BorderStyles.NoBorder: return 0;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Simple: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Flat: return 2;
                    case DevExpress.XtraEditors.Controls.BorderStyles.HotFlat: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Style3D: return 2;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Office2003: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Default: return 1;
                }
                return 0;
            }
        }
        /// <summary>
        /// Gets or sets the panel's border style.
        /// </summary>
        public new DevExpress.XtraEditors.Controls.BorderStyles BorderStyle 
        { 
            get { return base.BorderStyle; }
            set
            {
                if (value != base.BorderStyle)
                {
                    base.BorderStyle = value;
                    OnBorderStyleChanged();
                    BorderStyleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Při změně <see cref="BorderStyle"/>
        /// </summary>
        protected virtual void OnBorderStyleChanged() { }
        /// <summary>
        /// Při změně <see cref="BorderStyle"/>
        /// </summary>
        public event EventHandler BorderStyleChanged;
        /// <summary>
        /// Povolit průhlednost panelu?
        /// </summary>
        public bool AllowTransparency { get; set; }
        /// <summary>
        /// Povolit průhlednost panelu?
        /// Hodnotu čte DevExpress při zpracování panelu. V DxPanelControl ji umožníme nastavit.
        /// </summary>
        protected override bool AllowTotalTransparency { get { return AllowTransparency; /* namísto base.AllowTotalTransparency */ } }
        /// <summary>
        /// Souřadnice vnitřního prostoru panelu.
        /// Pokud Panel má nějaký Border, který je vykreslován uvnitř <see cref="Control.ClientRectangle"/>, 
        /// pak <see cref="InnerRectangle"/> je o tento Border zmenšený.
        /// </summary>
        public Rectangle InnerRectangle
        {
            get
            {
                var size = Size;
                var clientSize = ClientSize;
                var borderWidth = BorderWidth;
                if (clientSize.Width == size.Width && borderWidth > 0)
                {   // DevExpress s oblibou tvrdí, že ClientSize == Size, a přitom mají Border nenulové velikosti. Pak by se nám obsah kreslil přes Border.
                    int b2 = 2 * borderWidth;
                    return new Rectangle(borderWidth, borderWidth, size.Width - b2, size.Height - b2);
                }
                return new Rectangle(Point.Empty, clientSize);
            }
        }
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public virtual bool LogActive { get; set; }
        #endregion
        #region HasMouse
        /// <summary>
        /// Panel má na sobě myš?
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region Paint
        /// <summary>
        /// Základní kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(SWF.PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintBackColorUser(e);
        }
        /// <summary>
        /// Overlay kreslení BackColorUser
        /// </summary>
        /// <param name="e"></param>
        protected void PaintBackColorUser(SWF.PaintEventArgs e)
        {
            var backColorUser = BackColorUser;
            if (!backColorUser.HasValue) return;
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColorUser.Value), this.ClientRectangle);
        }
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); DeviceDpiCheck(false); OnContentSizeChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); DeviceDpiCheck(false); OnContentSizeChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        /// <summary>
        /// Po změně Parenta prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DeviceDpiCheck(true);
        }
        /// <summary>
        /// Po změně DPI v parentu prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            DeviceDpiCheck(true);
        }
        /// <summary>
        /// Při invalidaci prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            DeviceDpiCheck(true);
            base.OnInvalidated(e);
        }
        /// <summary>
        /// Tento háček je vyvolán po jakékoli akci, která může vést k přepočtu vnitřních velikostí controlů.
        /// Je volán: po změně Zoomu, po změně Skinu, po změně DPI hostitelského okna.
        /// <para/>
        /// Potomek by v této metodě měl provést přepočty velikosti svých controlů, pokud závisejí na Zoomu a DPI (a možná Skinu) (rozdílnost DesignSize a CurrentSize).
        /// <para/>
        /// Metoda není volána po změně velikosti controlu samotného ani po změně ClientBounds, ta změna nezakládá důvod k přepočtu velikosti obsahu
        /// </summary>
        protected virtual void OnContentSizeChanged() { }
        #endregion
        #region DPI - podpora pro MultiMonitory s různým rozlišením / pro jiné DPI než designové
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        public int CurrentDpi { get { return this._CurrentDpi; } }
        /// <summary>
        /// Aktuální hodnota DeviceDpi z formuláře / anebo z this controlu
        /// </summary>
        private int _CurrentDpi;
        /// <summary>
        /// Znovu načte hodnotu DeviceDpi z formuláře / anebo z this controlu
        /// </summary>
        /// <returns></returns>
        private int _ReloadCurrentDpi()
        {
            _CurrentDpi = this.FindForm()?.DeviceDpi ?? this.DeviceDpi;
            return _CurrentDpi;
        }
        /// <summary>
        /// Hodnota DeviceDpi, pro kterou byly naposledy přepočteny souřadnice prostoru
        /// </summary>
        private int _LastDpi;
        /// <summary>
        /// Obsahuje true, pokud se nyní platné DPI liší od DPI posledně použitého pro přepočet souřadnic
        /// </summary>
        private bool _DpiChanged { get { return (this._CurrentDpi != this._LastDpi); } }
        /// <summary>
        /// Ověří, zda nedošlo ke změně DeviceDpi, a pokud ano pak zajistí vyvolání metod <see cref="OnDpiChanged()"/> a eventu <see cref="DpiChanged"/>.
        /// Pokud this panel není umístěn na formuláři, neprovede nic, protože DPI nemůže být platné.
        /// </summary>
        /// <param name="callContentSizeChanged">Pokud došlo ke změně DPI, má být volán háček <see cref="OnContentSizeChanged()"/>? Někdy to není nutné, protože se bude volat po této metodě vždy (i bez změny DPI).</param>
        protected void DeviceDpiCheck(bool callContentSizeChanged)
        {
            if (this.FindForm() == null) return;
            var currentDpi = _ReloadCurrentDpi();
            if (_DpiChanged)
            {
                OnDpiChanged();
                if (callContentSizeChanged)
                    OnContentSizeChanged();
                DpiChanged?.Invoke(this, EventArgs.Empty);
                _LastDpi = currentDpi;
            }
        }
        /// <summary>
        /// Po jakékoli změně DPI
        /// </summary>
        protected virtual void OnDpiChanged() { }
        /// <summary>
        /// Po jakékoli změně DPI
        /// </summary>
        public event EventHandler DpiChanged;
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
    }
    #endregion
    #region DxAutoScrollPanelControl
    /// <summary>
    /// DxAutoScrollPanelControl : Panel s podporou AutoScroll a s podporou události při změně VisibleBounds
    /// </summary>
    public class DxAutoScrollPanelControl : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxAutoScrollPanelControl()
        {
            this.AutoScroll = true;
            this.SetAutoScrollMargin(40, 6);
            this.Padding = new SWF.Padding(10);
            this.SetStyle(SWF.ControlStyles.UserPaint, true);
        }
        #region VisibleBounds
        /// <summary>
        /// Souřadnice Child prvků, které jsou nyní vidět (=logické koordináty).
        /// Pokud tedy mám Child prvek s Bounds = { 0, 0, 600, 2000 } 
        /// a this container má velikost { 500, 300 } a je odscrollovaný trochu dolů (o 100 pixelů),
        /// pak VisibleBounds obsahuje právě to "viditelné okno" v Child controlu = { 100, 0, 500, 300 }
        /// </summary>
        public Rectangle VisibleBounds { get { return __CurrentVisibleBounds; } }
        /// <summary>
        /// Je provedeno po změně <see cref="DxAutoScrollPanelControl.VisibleBounds"/>
        /// </summary>
        protected virtual void OnVisibleBoundsChanged() { }
        /// <summary>
        /// Událost je vyvolaná po každé změně <see cref="VisibleBounds"/>
        /// </summary>
        public event EventHandler VisibleBoundsChanged;
        /// <summary>
        /// Zkontroluje, zda aktuální viditelná oblast je shodná/jiná než dosavadní, a pokud je jiná pak ji upraví a vyvolá události.
        /// </summary>
        private void _CheckVisibleBoundsChange()
        {
            Rectangle last = __CurrentVisibleBounds;
            Rectangle current = _GetVisibleBounds();
            if (current == last) return;
            __CurrentVisibleBounds = current;
            OnVisibleBoundsChanged();
            VisibleBoundsChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vrátí aktuálně viditelnou oblast vypočtenou pro AutoScrollPosition a ClientSize
        /// </summary>
        /// <returns></returns>
        private Rectangle _GetVisibleBounds()
        {
            Point autoScrollPoint = this.AutoScrollPosition;
            Point origin = new Point(-autoScrollPoint.X, -autoScrollPoint.Y);
            Size size = this.ClientSize;
            return new Rectangle(origin, size);
        }
        private Rectangle __CurrentVisibleBounds;
        /// <summary>
        /// Volá se při kreslení pozadí.
        /// Potomci zde mohou detekovat nové <see cref="VisibleBounds"/> a podle nich zobrazit potřebné controly.
        /// V této metodě budou controly zobrazeny bez blikání = ještě dříve, než se Panel naroluje na novou souřadnici.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(SWF.PaintEventArgs e)
        {
            this._CheckVisibleBoundsChange();
            base.OnPaintBackground(e);
        }
        //   TATO METODA SE VOLÁ AŽ PO OnPaintBackground() A NENÍ TEDY NUTNO JI ŘEŠIT:
        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    this._CheckVisibleBoundsChange();
        //    base.OnPaint(e);
        //}
        /// <summary>
        /// Tato metoda je jako jediná vyvolaná při posunu obsahu pomocí kolečka myší a některých dalších akcích (pohyb po controlech, resize), 
        /// ale není volaná při manipulaci se Scrollbary.
        /// </summary>
        protected override void SyncScrollbars()
        {
            base.SyncScrollbars();
            this._CheckVisibleBoundsChange();
        }
        /// <summary>
        /// Tato metoda je vyvolaná při manipulaci se Scrollbary.
        /// Při té se ale nevolá SyncScrollbars().
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnScroll(object sender, XtraScrollEventArgs e)
        {
            base.OnScroll(sender, e);
            this._CheckVisibleBoundsChange();
        }
        /// <summary>
        /// Volá se po změně velikosti tohoto controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this._CheckVisibleBoundsChange();
        }
        #endregion
    }
    #endregion
    #region DxScrollableContent a DxScrollBars a ScrollBarIndicators
    /// <summary>
    /// Panel, který v sobě hostuje virtuální control <see cref="ContentControl"/>
    /// a dovoluje uživateli pomocí scrollbarů posouvat jeho virtuální obsah.
    /// <para/>
    /// Rozdíly od standardního <see cref="DxAutoScrollPanelControl"/> panelu:
    /// Tato třída (<see cref="DxScrollableContent"/>) dává prostor pro umístění uživatelského controlu do <see cref="ContentControl"/>, 
    /// kterému pak udržuje maximální možnou velikost v rámci své velikosti [mínus potřebné scrollbary].
    /// Eviduje se zde virtuální celkovou velikost uživatelského controlu v <see cref="ContentTotalSize"/> a pro tuto velikost zajišťuje ScrollBary, 
    /// a s jejich pomocí řídí virtuální zobrazený prostor v <see cref="ContentVirtualBounds"/> (plus event <see cref="ContentVirtualBoundsChanged"/>).
    /// </summary>
    public class DxScrollableContent : DxPanelControl
    {
        #region Konstrukce, proměnné a základní public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxScrollableContent()
        {
            _ContentControl = null;
            _SuppressEvent = false;
            _ContentTotalSize = Size.Empty;
            _ContentVirtualBounds = Rectangle.Empty;
            _ContentVisualPadding = Padding.Empty;
            _ContentVisualSize = this.ClientSize;
            _HScrollBarAllowed = true;
            _HScrollBarVisible = false;
            _VScrollBarAllowed = true;
            _VScrollBarVisible = false;

            _HScrollBar = new DxHScrollBar() { Visible = false, Minimum = 0, SmallChange = 80 };
            _HScrollBar.ValueChanged += _HScrollBar_ValueChanged;
            _HScrollBar.MouseWheel += _HScrollBar_MouseWheel;
            Controls.Add(_HScrollBar);

            _VScrollBar = new DxVScrollBar() { Visible = false, Minimum = 0, SmallChange = 40 };
            _VScrollBar.ValueChanged += _VScrollBar_ValueChanged;
            _VScrollBar.MouseWheel += _VScrollBar_MouseWheel;
            Controls.Add(_VScrollBar);

            ScrollToBoundsBasicPadding = new Padding(3);
            ScrollToBoundsScrollPadding = new Padding(24);
        }
        /// <summary>Control, který zobrazuje obsah</summary>
        private SWF.Control _ContentControl;
        /// <summary>Horizontal scrollbar</summary>
        private DxHScrollBar _HScrollBar;
        /// <summary>Horizontal scrollbar: je povolený? = v případě potřeby bude zobrazen</summary>
        private bool _HScrollBarAllowed;
        /// <summary>Horizontal scrollbar: je potřebný? = obsah je větší než současný prostor na ose X</summary>
        private bool _HScrollBarRequired;
        /// <summary>Horizontal scrollbar: je reálně viditelný? = je potřebný a je povolený</summary>
        private bool _HScrollBarVisible;
        /// <summary>Vertical scrollbar</summary>
        private DxVScrollBar _VScrollBar;
        /// <summary>Vertical scrollbar: je povolený? = v případě potřeby bude zobrazen</summary>
        private bool _VScrollBarAllowed;
        /// <summary>Vertical scrollbar: je potřebný? = obsah je větší než současný prostor na ose Y</summary>
        private bool _VScrollBarRequired;
        /// <summary>Vertical scrollbar: je reálně viditelný? = je potřebný a je povolený</summary>
        private bool _VScrollBarVisible;
        /// <summary>Velikost obsahu, který je scrollován = pokud bue větší než viditelný prostor, bude možno scrollovat</summary>
        private Size _ContentTotalSize;
        /// <summary>Aktuálně viditelný výřez obsahu</summary>
        private Rectangle _ContentVirtualBounds;
        /// <summary>Okraje kolem contentu = mezi vnitřním okrajem this panelu a vnějším okrajem panelu <see cref="ContentControl"/></summary>
        private Padding _ContentVisualPadding;
        /// <summary>Viditelná velikost obsahu</summary>
        private Size _ContentVisualSize;
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public override bool LogActive { get { return base.LogActive; } set { base.LogActive = value; if (_ContentControl != null && _ContentControl is DxPanelControl dxPanel) dxPanel.LogActive = value; } }
        #endregion
        #region Splittery
        public bool VSplitterEnabled { get { return _VSplitterEnabled; } set { _SetVSplitterEnabled(value); } }
        private bool _VSplitterEnabled;
        private DevExpress.XtraEditors.SplitterControl _VSplitter;
        private void _SetVSplitterEnabled(bool enabled)
        {
            if (enabled && _VSplitter == null)
            {
                _VSplitter = new DevExpress.XtraEditors.SplitterControl();
                _VSplitter.Bounds = new Rectangle(10, 5, 5, 500);
                _VSplitter.SplitPosition = 15;
                _VSplitter.ShowSplitGlyph = DefaultBoolean.True;
                // _VSplitter.Dock = DockStyle.None;               nemůže být!!!
                _VSplitter.Enabled = true;
                this.Controls.Add(_VSplitter);
            }
            _VSplitterEnabled = enabled;
            if (_VSplitter != null)
                _VSplitter.Visible = enabled;
        }
        #endregion
        #region Public vlastnosti - ContentControl, Size, ContentVirtualBounds...
        /// <summary>
        /// Aktuálně zobrazený obsah.
        /// Jeho fyzický rozměr bude vždy odpovídat aktuálně viditelnému prostoru.
        /// Je třeba zadat celkovou velikost obsahu do <see cref="ContentTotalSize"/>, na tuto velikost budou dimenzovány scrollbary a jejich posuny.
        /// Virtuálně zobrazené souřadnice controlu <see cref="ContentControl"/> jsou vždy uloženy v <see cref="ContentVirtualBounds"/>.
        /// Na změny virtuálních souřadnic (dané změnou fyzického prostoru anebo posunem scrollbarů) lze reagovat v handleru události <see cref="ContentVirtualBoundsChanged"/>.
        /// <para/>
        /// Setování instance do této property ji zařadí do this controlu jako Child , změna instance vyřadí dosavadní z this.Controls atd.
        /// <para/>
        /// Uživatel by nikdy neměl řídit pozici tohoto vnitřního objektu, ta je dána prostorem uvnitř this panelu <see cref="DxScrollableContent"/>.
        /// Při každé změně rozměru this panelu bude správně umístěn i tento <see cref="ContentControl"/>.
        /// </summary>
        public SWF.Control ContentControl
        {
            get { return _ContentControl; }
            set
            {
                SWF.Control contentControl = _ContentControl;
                if (contentControl != null)
                {
                    if (this.Controls.Contains(contentControl))
                        this.Controls.Remove(contentControl);
                    contentControl.MouseWheel -= ContentControl_MouseWheel;
                    if (contentControl is DxPanelControl dxPanel)
                        dxPanel.LogActive = false;
                    _ContentControl = null;
                }
                contentControl = value;
                if (contentControl != null)
                {
                    this.Controls.Add(contentControl);
                    _ContentControl = contentControl;
                    contentControl.MouseWheel += ContentControl_MouseWheel;
                    if (contentControl is DxPanelControl dxPanel)
                        dxPanel.LogActive = this.LogActive;
                    DoLayoutContent();
                }
            }
        }
        /// <summary>
        /// Okraje kolem contentu = mezi vnitřním okrajem this panelu a vnějším okrajem panelu <see cref="ContentControl"/>.
        /// Výchozí hodnota = {0,0,0,0}. Pak Content (obsah) obsazuje celý vnitřní prostor this panelu, vyjma potřebné scrollbary.
        /// Jde o designovou hodnotu, na vizuální pixely je přepočtena podle aktuálního Zoomu a DPI.
        /// <para/>
        /// Zadáním kladných hodnot dojde k vytvoření prostoru ("okraje") v daných oblastech okolo <see cref="ContentControl"/> 
        /// (<see cref="ContentControl"/> bude menší než dostupný vnitřní prostor). 
        /// Tyto okraje pak aplikace může využít k umístění fixních = nescrollovaných prvků (záhlaví sloupců, řádků; pravítka nahoře, dole; dolní součtový řádek, atd).
        /// <para/>
        /// Společně s panelem <see cref="ContentControl"/> budou odsunuty a upraveny i ScrollBary.
        /// Ty budou vždy na úplném okaji this panelu, ale budou jen ve velikosti odpovídající <see cref="ContentControl"/>.
        /// <para/>
        /// Záporné hodnoty v této souřadnici nejsou akceptovány.
        /// Příliš velké hodnoty nejsou doporučovány, mohou vést ke zmizení obsahu.
        /// </summary>
        public Padding ContentVisualPadding { get { return _ContentVisualPadding; } set { SetContentVisualPadding(value); } }
        /// <summary>
        /// Aktuální viditelná velikost obsahu
        /// </summary>
        public Size ContentVisualSize { get { return _ContentVisualSize; } }
        /// <summary>
        /// Celková (virtuální) velikost obsahu. Na tuto plochu jsou dimenzovány ScrollBary a tato plocha je posouvána.
        /// </summary>
        public Size ContentTotalSize { get { return _ContentTotalSize; } set { _ContentTotalSize = value; DoLayoutContent(); } }
        /// <summary>
        /// Aktuální viditelné souřadnice virtuálního obsahu. 
        /// Počáteční bod je dán ScrollBary, velikost je dána fyzickou velikostí this panelu (mínus prostor ScrollBarů).
        /// <para/>
        /// Tuto hodnotu není možno změnit, je odvozena od fyzické velikosti celého controlu, zmenšené o případné ScrollBary, a pozice je dána hodnotou ScrollBarů.
        /// Setovat lze <see cref="ContentVirtualLocation"/>.
        /// </summary>
        public Rectangle ContentVirtualBounds
        {
            get { return _ContentVirtualBounds; }
            private set
            {   // Tady nebudeme řešit kontroly ani návaznosti na ScrollBary, to musel řešit volající. Tady jen hlídáme změnu a voláme event:
                var oldValue = _ContentVirtualBounds;
                var newValue = value;
                if (oldValue != newValue)
                {
                    _ContentVirtualBounds = newValue;
                    _RunContentVirtualBoundsChanged();
                }
            }
        }
        /// <summary>
        /// Souřadnice počátku viditelné části obsahu <see cref="ContentVirtualBounds"/>.
        /// Tuto hodnotu je možno setovat a tak programově řídit posuny obsahu.
        /// Setovaná hodnota bude zkonrolována, upravena a následně vložena do <see cref="ContentVirtualBounds"/>. 
        /// Po změně dojde k volání události <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        public Point ContentVirtualLocation { get { return _ContentVirtualBounds.Location; } set { SetVirtualLocation(value); } }
        /// <summary>
        /// Tuto událost vyvolá this panel <see cref="DxScrollableContent"/> 
        /// při každé změně velikosti nebo pozice virtuálního prostoru <see cref="ContentVirtualBounds"/>.
        /// </summary>
        public EventHandler ContentVirtualBoundsChanged;
        /// <summary>
        /// Obsahuje true, pokud je povolenou zobrazit a používat Horizontální (=vodorovný) ScrollBar.
        /// Výchozí je true. ScrollBar bude zobrazen, když bude potřeba.
        /// Pokud je false, ScrollBar nebude zobrazován, ale myší kolečko na prvku bude fungovat jako by byl vidět.
        /// <para/>
        /// Používá se tedy, když je vedle sebe více panelů se spřaženými souřadnicemi (typicky Bands v tabulkách), 
        /// kde máme vedle sebe dvě skupiny dat, v pravé je svislý ScollBar zobrazen, ale nechceme jej mít i ve skupině vlevo.
        /// </summary>
        public bool HScrollBarAllowed { get { return _HScrollBarAllowed; } set { _HScrollBarAllowed = value; DoLayoutContent(); } }
        /// <summary>
        /// Obsahuje true, pokud je Horizontální (=vodorovný) ScrollBar potřebný = obsah je větší než viditelná oblast.
        /// Pokud ale není povole (<see cref="HScrollBarAllowed"/> je false), pak ScrollBar nebude fyzicky zobrazen (<see cref="HScrollBarVisible"/> bude false).
        /// </summary>
        public bool HScrollBarRequired { get { return _HScrollBarRequired; } }
        /// <summary>
        /// Obsahuje true, pokud je viditelný Horizontální (=vodorovný) ScrollBar
        /// </summary>
        public bool HScrollBarVisible { get { return _HScrollBarVisible; } }
        /// <summary>
        /// Indikátory přítomné na horizontálním scrollbaru
        /// </summary>
        public ScrollBarIndicators HScrollBarIndicators { get { return _HScrollBar.Indicators; } }
        /// <summary>
        /// Hodnota na horizontálním ScrollBaru, bez korekcí, podle povolení pro horizontální ScrollBar:
        /// <para/>
        /// Pokud je viditelný (<see cref="_HScrollBarVisible"/> = true), 
        /// pak je zde reálná hodnota ze ScrollBaru <see cref="_HScrollBar"/>.
        /// <para/>
        /// Pokud není potřebný (<see cref="_HScrollBarRequired"/> = false), 
        /// pak je zde 0 (protože obsah je zobrazen celý = obsah není větší než disponibilní prostor).
        /// <para/>
        /// Pokud je potřebný (<see cref="_HScrollBarRequired"/> = true), ale z nějakého důvodu není viditelný
        /// (není povolen: <see cref="_HScrollBarAllowed"/> = false) anebo není možno jej zobrazit: <see cref="_HScrollBarVisible"/> = false), 
        /// pak je zde odpovídající hodnota z <see cref="_ContentVirtualBounds"/>.X.
        /// </summary>
        private int _HScrollBarCurrentValue
        {
            get
            {
                if (_HScrollBarVisible) return _HScrollBar.Value;              // Je fyzicky viditelný = převezmu hodnotu
                if (_HScrollBarRequired) return _ContentVirtualBounds.X;       // Není viditelný, ale je potřebný = převezmu souřadnici
                return 0;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud je povolenou zobrazit a používat Vertikální (=svislý) ScrollBar.
        /// Výchozí je true. ScrollBar bude zobrazen, když bude potřeba.
        /// Pokud je false, ScrollBar nebude zobrazován, ale myší kolečko na prvku bude fungovat jako by byl vidět.
        /// <para/>
        /// Používá se tedy, když je nad sebou více panelů se spřaženými souřadnicemi (typicky Bands v tabulkách), 
        /// kde máme nad sebou dvě skupiny dat, v dolní je vodorovný ScollBar zobrazen, ale nechceme jej mít i ve skupině nahoře.
        /// </summary>
        public bool VScrollBarAllowed { get { return _VScrollBarAllowed; } set { _VScrollBarAllowed = value; DoLayoutContent(); } }
        /// <summary>
        /// Obsahuje true, pokud je Vertikální (=svislý) ScrollBar potřebný = obsah je větší než viditelná oblast.
        /// Pokud ale není povole (<see cref="VScrollBarAllowed"/> je false), pak ScrollBar nebude fyzicky zobrazen (<see cref="VScrollBarVisible"/> bude false).
        /// </summary>
        public bool VScrollBarRequired { get { return _VScrollBarRequired; } }
        /// <summary>
        /// Obsahuje true, pokud je viditelný Vertikální (=svislý) ScrollBar
        /// </summary>
        public bool VScrollBarVisible { get { return _VScrollBarVisible; } }
        /// <summary>
        /// Indikátory přítomné na vertikálním scrollbaru
        /// </summary>
        public ScrollBarIndicators VScrollBarIndicators { get { return _VScrollBar.Indicators; } }
        /// <summary>
        /// Hodnota na vertikálním ScrollBaru, bez korekcí, podle povolení pro vertikální ScrollBar:
        /// <para/>
        /// Pokud je viditelný (<see cref="_VScrollBarVisible"/> = true), 
        /// pak je zde reálná hodnota ze ScrollBaru <see cref="_VScrollBar"/>.
        /// <para/>
        /// Pokud není potřebný (<see cref="_VScrollBarRequired"/> = false), 
        /// pak je zde 0 (protože obsah je zobrazen celý = obsah není větší než disponibilní prostor).
        /// <para/>
        /// Pokud je potřebný (<see cref="_VScrollBarRequired"/> = true), ale z nějakého důvodu není viditelný
        /// (není povolen: <see cref="_VScrollBarAllowed"/> = false) anebo není možno jej zobrazit: <see cref="_VScrollBarVisible"/> = false), 
        /// pak je zde odpovídající hodnota z <see cref="_ContentVirtualBounds"/>.Y.
        /// </summary>
        private int _VScrollBarCurrentValue 
        { 
            get 
            {
                if (_VScrollBarVisible) return _VScrollBar.Value;              // Je fyzicky viditelný = převezmu hodnotu
                if (_VScrollBarRequired) return _ContentVirtualBounds.Y;       // Není viditelný, ale je potřebný = převezmu souřadnici
                return 0;
            }
        }
        #endregion
        #region Layout a řízení ScrollBarů
        /// <summary>
        /// Uloží a akceptuje souřadnici <see cref="ContentVisualPadding"/>
        /// </summary>
        /// <param name="contentVisualPadding"></param>
        protected void SetContentVisualPadding(Padding contentVisualPadding)
        {
            int l = contentVisualPadding.Left.Align(0, 800);
            int t = contentVisualPadding.Top.Align(0, 600);
            int r = contentVisualPadding.Right.Align(0, 800);
            int b = contentVisualPadding.Bottom.Align(0, 600);

            _ContentVisualPadding = new Padding(l, t, r, b);
            DoLayoutContent();
        }
        /// <summary>
        /// Na základě aktuálních fyzických rozměrů a podle <see cref="ContentTotalSize"/> určí potřebnou viditelnost ScrollBarů,
        /// určí souřadnice prvků (Content i ScrollBary), určí vlastnosti pro ScrollBary a velikost prosotru pro vlastní obsah (<see cref="_ContentVisualSize"/>).
        /// Pokud dojde k jakékoli změně, vyvolá jedenkrát událost <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        protected void DoLayoutContent()
        {
            // Vizuální prostor:
            Rectangle innerBounds = InnerRectangle;
            Padding visualPadding = DxComponent.ZoomToGui(_ContentVisualPadding, this.CurrentDpi);           // přepočet Design => Current
            Rectangle contentBounds = Rectangle.FromLTRB(innerBounds.Left + visualPadding.Left, innerBounds.Top + visualPadding.Top, innerBounds.Right - visualPadding.Right, innerBounds.Bottom - visualPadding.Bottom);
            if (contentBounds.Width < 0) contentBounds.Width = 0;
            if (contentBounds.Height < 0) contentBounds.Height = 0;
            if (this.Parent == null)
            {   // Bez parenta toho moc neděláme, je to předčasné (typicky: nemáme správnou velikost, ani neznáme CurrentDPI).
                // Po změně parenta tahle metoda proběhne taky, a vyřešíme vše potřebné.
                _ContentVisualSize = contentBounds.Size;
                _ContentControl?.SetBounds(contentBounds);
                return;
            }

            // Velikost virtuálního obsahu:
            Size contentTotalSize = this.ContentTotalSize;

            // Horizontální (vodorovný) ScrollBar: bude potřebný (a viditelný), když šířka obsahu je větší než šířka klienta, a zmenší tak výšku klienta:
            bool hRequired = (contentBounds.Width > 0 && contentTotalSize.Width > contentBounds.Width);
            bool hVisible = (_HScrollBarAllowed && hRequired);
            int hScrollSize = (hVisible ? _VScrollBar.GetDefaultHorizontalScrollBarHeight() : 0);
            if (hVisible) contentBounds.Height -= hScrollSize;

            // Vertikální (svislý) ScrollBar: bude potřebný (a viditelný), když výška obsahu je větší než výška klienta, a zmenší tak šířku klienta:
            bool vRequired = (contentBounds.Height > 0 && contentTotalSize.Height > contentBounds.Height);
            bool vVisible = (_VScrollBarAllowed && vRequired);
            int vScrollSize = (vVisible ? _VScrollBar.GetDefaultVerticalScrollBarWidth() : 0);
            if (vVisible) contentBounds.Width -= vScrollSize;

            // Pokud dosud nebyl viditelný Vertikální (svislý) ScrollBar, ale je viditelný Horizontální (vodorovný) ScrollBar:
            //  pak Horizontální ScrollBar zmenšil výšku obsahu (clientHeight), a může se stát, že bude třeba zobrazit i Vertikální ScrollBar:
            if (!vVisible && hVisible && (contentTotalSize.Height > contentBounds.Height))
            {
                vVisible = true;
                vScrollSize = _VScrollBar.GetDefaultVerticalScrollBarWidth();
                contentBounds.Width -= vScrollSize;
            }

            // Pokud je přílš malá šířka a je viditelný Vertikální (svislý) ScrollBar: vrátit plnou šířku a zrušit scrollBar:
            if (contentBounds.Width < 10 && vVisible)
            {
                contentBounds.Width += vScrollSize;
                vVisible = false;
                vScrollSize = 0;
            }
            // Pokud je přílš malá výška a je viditelný Horizontální (vodorovný) ScrollBar: vrátit plnou výšku a zrušit scrollBar:
            if (contentBounds.Height < 10 && hVisible)
            {
                contentBounds.Height += hScrollSize;
                hVisible = false;
                hScrollSize = 0;
            }

            // bool reCalcVirtualBounds = (clientWidth != contentVirtualBounds.Width || clientHeight != contentVirtualBounds.Height);

            _ContentControl?.SetBounds(contentBounds);
            _ContentVisualSize = new Size(contentBounds.Width, contentBounds.Height);
            _HScrollBarRequired = hRequired;
            _HScrollBarVisible = hVisible;
            _VScrollBarRequired = vRequired;
            _VScrollBarVisible = vVisible;

            bool suppressEvent = _SuppressEvent;
            try
            {
                _SuppressEvent = true;

                if (vVisible)
                {
                    _VScrollBar.SetBounds(new Rectangle(innerBounds.Right - vScrollSize, contentBounds.Y, vScrollSize, contentBounds.Height));
                    _VScrollBar.Maximum = contentTotalSize.Height;
                    _VScrollBar.LargeChange = contentBounds.Height;
                }
                if (hVisible)
                {
                    _HScrollBar.SetBounds(new Rectangle(contentBounds.X, innerBounds.Bottom - hScrollSize, contentBounds.Width, hScrollSize));
                    _HScrollBar.Maximum = contentTotalSize.Width;
                    _HScrollBar.LargeChange = contentBounds.Width;
                }

                if (_VScrollBar.VisibleInternal != vVisible) _VScrollBar.Visible = vVisible;
                if (_HScrollBar.VisibleInternal != hVisible) _HScrollBar.Visible = hVisible;
            }
            finally
            {
                _SuppressEvent = suppressEvent;
            }

            // Tady se vezmou souřadnice X a Y ze ScrollBarů (z těch viditelných), vezme se i aktuální _ContentVisualSize,
            //  určí se a uloží reálné souřadnice ContentVirtualBounds a pokud dojde ke změně, vyvolá se patřičný event:
            ApplyScrollBarsToVirtualLocation();
        }
        /// <summary>
        /// OnParentChanged
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DoLayoutContent();
        }
        /// <summary>
        /// OnClientSizeChanged
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            DoLayoutContent();
        }
        /// <summary>
        /// OnBorderStyleChanged
        /// </summary>
        protected override void OnBorderStyleChanged()
        {
            base.OnBorderStyleChanged();
            DoLayoutContent();
        }
        /// <summary>
        /// OnZoomChanged
        /// </summary>
        protected override void OnZoomChanged()
        {
            base.OnZoomChanged();
            DoLayoutContent();
            OnInvalidateContentAfter();
        }
        /// <summary>
        /// OnStyleChanged
        /// </summary>
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            DoLayoutContent();
            OnInvalidateContentAfter();
        }
        /// <summary>
        /// OnDpiChangedAfterParent
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            DoLayoutContent();
            OnInvalidateContentAfter();
        }
        /// <summary>
        /// Je vyvoláno po změně DPI, po změně Zoomu a po změně skinu. Volá se po přepočtu layoutu.
        /// Může vést k invalidaci interních dat v <see cref="DxScrollableContent.ContentControl"/>.
        /// </summary>
        protected virtual void OnInvalidateContentAfter() { }
        #endregion
        #region Scroll to control / to virtual bounds
        /// <summary>
        /// Zajistí nascrollování obsahu tak, aby daný prostor byl viditelný.
        /// Akceptuje přídavek k rozměru daný v <see cref="ScrollToBoundsBasicPaddingCurrent"/>.
        /// </summary>
        /// <param name="controlVirtualBounds"></param>
        /// <param name="groupVirtualBounds"></param>
        /// <param name="skipEvent"></param>
        /// <returns></returns>
        public bool ScrollToBounds(Rectangle controlVirtualBounds, Rectangle? groupVirtualBounds = null, bool skipEvent = false)
        {
            Size totalSize = this.ContentTotalSize;
            Rectangle currentBounds = this.ContentVirtualBounds;
            Rectangle targetBounds = controlVirtualBounds.Add(ScrollToBoundsBasicPaddingCurrent);           // Malý přídavek, jen aby daný control nebyl zobrazen úplně na hraně
            if (currentBounds.Contains(targetBounds)) return false;                                         // Požadovaný prostor je zcela vidět

            // Budeme muset Scrollovat:
            Point oldVirtualOrigin = currentBounds.Location;
            bool suppressEvent = _SuppressEvent;
            try
            {
                _SuppressEvent = true;

                if (groupVirtualBounds.HasValue)
                {   // Když už scrollovat, tak se pokusíme narolovat na větší prostor:
                    targetBounds = groupVirtualBounds.Value.Add(ScrollToBoundsScrollPaddingCurrent);        // Například control plus jeho label nebo celá grupa...  Plus větší přídavek.
                    ScrollToBounds(currentBounds, targetBounds, totalSize);
                }

                targetBounds = controlVirtualBounds.Add(ScrollToBoundsScrollPaddingCurrent);                // Větší přídavek, když už scrollujeme, aby cílový prostor nebyl úplně na okraji
                ScrollToBounds(currentBounds, targetBounds, totalSize);
            }
            finally
            {
                _SuppressEvent = suppressEvent;
            }

            Point newVirtualOrigin = this.ContentVirtualBounds.Location;
            if (newVirtualOrigin == oldVirtualOrigin) return false;

            // Nyní víme, že došlo ke změně:
            if (!skipEvent)
                _RunContentVirtualBoundsChanged();

            return true;
        }
        /// <summary>
        /// Zajistí scrollování podle patřičných pravidel, pro požadované souřadnice, pro aktuální zobrazené souřadnice a celkovou velikost obsahu
        /// </summary>
        /// <param name="currentBounds">Aktuální zobrazený prostor</param>
        /// <param name="targetBounds">Požadovaný prostor, který má být zobrazen</param>
        /// <param name="totalSize">Velikost obsahu</param>
        protected void ScrollToBounds(Rectangle currentBounds, Rectangle targetBounds, Size totalSize)
        {
            ScrollToBounds(targetBounds.X, targetBounds.Right, currentBounds.X, currentBounds.Right, totalSize.Width, HScrollBarVisible, _HScrollBar);
            ScrollToBounds(targetBounds.Y, targetBounds.Bottom, currentBounds.Y, currentBounds.Bottom, totalSize.Height, VScrollBarVisible, _VScrollBar);
        }
        /// <summary>
        /// Zajistí scrollování podle patřičných pravidel v jednom směru (Vertikální nebo Horizontální)
        /// </summary>
        /// <param name="targetBegin"></param>
        /// <param name="targetEnd"></param>
        /// <param name="currentBegin"></param>
        /// <param name="currentEnd"></param>
        /// <param name="totalSize"></param>
        /// <param name="scrollBarVisible"></param>
        /// <param name="scrollBar"></param>
        protected void ScrollToBounds(int targetBegin, int targetEnd, int currentBegin, int currentEnd, int totalSize, bool scrollBarVisible, ScrollBarBase scrollBar)
        {
            if (!scrollBarVisible || scrollBar == null) return;
            int currentStart = currentBegin;
            int currentSize = currentEnd - currentBegin;

            if (targetEnd > currentEnd)
            {
                currentEnd = targetEnd;
                if (currentEnd > totalSize) currentEnd = totalSize;
                currentBegin = currentEnd - currentSize;
            }

            if (targetBegin < currentBegin)
            {
                currentBegin = targetBegin;
                if (currentBegin < 0) currentBegin = 0;
                currentEnd = currentBegin + currentSize;
            }

            if (currentBegin != currentStart)
            {
                scrollBar.Value = currentBegin;
            }
        }
        /// <summary>
        /// Okraje, přidávané k požadovaném prostoru controlu v metodě <see cref="ScrollToBounds(Rectangle, Rectangle?, bool)"/> před tím, než se ověří jeho aktuální viditelnost.
        /// Tyto okraje "zvětšují" control, tak aby se Scroll provedl i tehdy, když vlastní control sice je vidět, ale je těsně na okraji viditelného prostoru.
        /// <para/>
        /// Výchozí hodnota = 3 pixely.
        /// Jde o designové pixely = bez aplikování odlišného DPI a Zoomu, ty se aplikují interně.
        /// </summary>
        public Padding ScrollToBoundsBasicPadding { get; set; }
        /// <summary>
        /// Aktuální hodnota <see cref="ScrollToBoundsBasicPadding"/> (pro aktuální Zoom a DPI)
        /// </summary>
        protected Padding ScrollToBoundsBasicPaddingCurrent { get { return DxComponent.ZoomToGui(ScrollToBoundsBasicPadding, CurrentDpi); } }
        /// <summary>
        /// Okraje, přidávané ke scrollu prováděnému v metodě <see cref="ScrollToBounds(Rectangle, Rectangle?)"/> v situaci, kdy je potřeba reálně posunout obsah.
        /// Tedy: pokud požadovaný obsah (s přidáním <see cref="ScrollToBoundsBasicPadding"/>) je celý viditelný, pak se scrollovat nebude ani když nebude dodržen zde uvedený okraj.
        /// Jakmile ale bude část (zvětšeného) obsahu neviditelná, pak se provede Scroll tak, aby okolo obsahu byl právě tento okraj.
        /// <para/>
        /// Výchozí hodnota = 24 pixelů.
        /// Jde o designové pixely = bez aplikování odlišného DPI a Zoomu, ty se aplikují interně.
        /// </summary>
        public Padding ScrollToBoundsScrollPadding { get; set; }
        /// <summary>
        /// Aktuální hodnota <see cref="ScrollToBoundsScrollPadding"/> (pro aktuální Zoom a DPI)
        /// </summary>
        protected Padding ScrollToBoundsScrollPaddingCurrent { get { return DxComponent.ZoomToGui(ScrollToBoundsScrollPadding, CurrentDpi); } }
        #endregion
        #region Výpočty virtuální souřadnice a reakce na interaktivní posuny
        /// <summary>
        /// Nastaví počáteční souřadnici virtuálního prostoru podle daného bodu, před tím provede veškeré kontroly, při změně reálné hodnoty vyvolá událost
        /// </summary>
        /// <param name="virtualLocation"></param>
        protected void SetVirtualLocation(Point virtualLocation)
        {
            Rectangle virtualBoundsOld = this.ContentVirtualBounds;

            Size contentVisualSize = _ContentVisualSize;
            int x = virtualLocation.X;
            int y = virtualLocation.Y;
            int vw = contentVisualSize.Width;
            int vh = contentVisualSize.Height;

            Size contentTotalSize = _ContentTotalSize;
            int tw = contentTotalSize.Width;
            int th = contentTotalSize.Height;
            if ((x + vw) > tw) x = tw - vw;                // Pokud by aktuální X bylo větší, takže by viditelná šířka přesahovala celkovou šířku, pak posunu X doleva...
            if ((y + vh) > th) y = th - vh;                //  stejně tak výška a Y
            if (x < 0) x = 0;
            if (y < 0) y = 0;

            // Scrollbary a případná změna jejich hodnoty - jen pokud jsou reálně viditelné:
            // (změna souřadnice Location nemění šířku - a to ani vizuální, ani celkovou, proto nemění Visible ScrollBarů ani jejich maximum a LargeChange).
            bool hv = _HScrollBarVisible;
            bool vv = _VScrollBarVisible;
            if (hv || vv)
            {
                int sx = _HScrollBarCurrentValue;
                int sy = _VScrollBarCurrentValue;
                bool changeX = (hv && x != sx);
                bool changeY = (vv && y != sy);
                if (changeX || changeY)
                {   // Došlo k tomu, že musíme změnit hodnotu na některém ScrollBaru, protože setovaná hodnota je jiná, než ukazují ScrollBary.
                    bool suppressEvent = _SuppressEvent;
                    try
                    {
                        // Tedy vyvoláme setování upravené souřadnice do ScrollBarů, to vyvolá zápis nové hodnoty do ContentVirtualBounds, a to klidně dvakrát (X i Y).
                        // Ale nechci volat dva eventy, jeden pro každý směr (s ohledem na náročnost navazujících přepočtů),
                        // takže potlačím volání eventu ContentVirtualBoundsChanged :
                        _SuppressEvent = true;
                        if (changeX) _HScrollBar.Value = x;
                        if (changeY) _VScrollBar.Value = y;
                    }
                    finally
                    {
                        _SuppressEvent = suppressEvent;
                    }
                }
            }

            // Nyní musím vložit novou hodnotu do ContentVirtualBounds - ale s vyvoláním eventhandleru ContentVirtualBoundsChanged,
            // protože ke změně reálně došlo, a aplikace musí dostat informaci o této změně:
            // Ono je totiž možné, že vložením korigované hodnoty do ScrollBarů (o pár řádků nahoře) se hodnota ContentVirtualBoundsChanged už změnila, ale byl potlačen eventhandler!
            Rectangle virtualBoundsNew = new Rectangle(x, y, vw, vh);
            this._ContentVirtualBounds = virtualBoundsOld;
            this.ContentVirtualBounds = virtualBoundsNew;                      // Tato sekvence spolehlivě zajistí, že pokud došlo ke změně hodnoty v rámci této metody, bude volán eventhandler, a že hodnota bude ve výsledku platná.
        }
        /// <summary>
        /// Pokud není potlačen event <see cref="_SuppressEvent"/>, pak vyvolá háček <see cref="OnContentVirtualBoundsChanged"/> a event <see cref="ContentVirtualBoundsChanged"/>
        /// </summary>
        private void _RunContentVirtualBoundsChanged()
        {
            if (!_SuppressEvent)
            {
                OnContentVirtualBoundsChanged();
                ContentVirtualBoundsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Je voláno pokud dojde ke změně hodnoty <see cref="DxScrollableContent.ContentVirtualBounds"/>, před eventem <see cref="DxScrollableContent.ContentVirtualBoundsChanged"/>
        /// </summary>
        protected virtual void OnContentVirtualBoundsChanged() { }
        /// <summary>
        /// Po změně hodnoty na ScrollBarech - přemístí <see cref="ContentVirtualLocation"/> (a vyvolá události, pokud nejsou potlačené)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HScrollBar_ValueChanged(object sender, EventArgs e)
        {
            if (_HScrollBarAllowed && _HScrollBarVisible)
                ApplyScrollBarsToVirtualLocation();
        }
        /// <summary>
        /// Po změně hodnoty na ScrollBarech - přemístí <see cref="ContentVirtualLocation"/> (a vyvolá události, pokud nejsou potlačené)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            if (_VScrollBarAllowed && _VScrollBarVisible)
                ApplyScrollBarsToVirtualLocation();
        }
        /// <summary>
        /// Na controlu <see cref="ContentControl"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentControl_MouseWheel(object sender, SWF.MouseEventArgs e)
        {
            Orientation? orientation = GetContentShiftOrientation();
            bool largeStep = ModifierKeys.HasFlag(Keys.Shift);
            DoContentShift(orientation, e.Delta, largeStep);
        }
        /// <summary>
        /// Na controlu <see cref="_VScrollBar"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VScrollBar_MouseWheel(object sender, SWF.MouseEventArgs e)
        {
            bool largeStep = ModifierKeys.HasFlag(Keys.Shift);
            DoContentShift(Orientation.Vertical, e.Delta, largeStep);
        }
        /// <summary>
        /// Na controlu <see cref="_HScrollBar"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HScrollBar_MouseWheel(object sender, SWF.MouseEventArgs e)
        {
            bool largeStep = ModifierKeys.HasFlag(Keys.Shift);
            DoContentShift(Orientation.Horizontal, e.Delta, largeStep);
        }
        /// <summary>
        /// Vrátí vhodný scrollbar pro případ, kdy uživatel skroluje na clastním ContentPanelu (typická situace).
        /// Primárně vrací svislý, při klávese Control vrací vodorovný (pokud jsou přítomny oba).
        /// </summary>
        /// <returns></returns>
        private Orientation? GetContentShiftOrientation()
        {
            bool hasVScrollBar = _VScrollBarVisible;
            bool hasHScrollBar = _HScrollBarVisible;
            if (hasVScrollBar && hasHScrollBar)
            {
                if (ModifierKeys.HasFlag(Keys.Control)) return Orientation.Horizontal;
                return Orientation.Vertical;
            }
            if (hasVScrollBar) return Orientation.Vertical;
            if (hasHScrollBar) return Orientation.Horizontal;
            return null;
        }
        /// <summary>
        /// Má být proveden posun obsahu v dané orientaci, v daném směru a v kroku malém/velkém.
        /// Zajistí posunutí obsahu pomocí výpočtu nové souřadnice (které bude vložena do odpovídajícího ScrollBaru).
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="delta"></param>
        /// <param name="largeStep"></param>
        private void DoContentShift(Orientation? orientation, int delta, bool largeStep)
        {
            if (!orientation.HasValue) return;
            ScrollBarBase scrollBar = (orientation.Value == Orientation.Horizontal ? (ScrollBarBase)_HScrollBar : (ScrollBarBase)_VScrollBar);

            int direction = (delta < 0 ? 1 : (delta > 0 ? -1 : 0));                                          // Směr posunutí
            int coefficient = (largeStep ? (9 * scrollBar.LargeChange / 10) : (2 * scrollBar.SmallChange));  // largeStep posouvá o 90% LargeChange, smallStep posouvá 2 * SmallChange
            int distance = direction * coefficient;
            int value = scrollBar.Value;
            int maxValue = scrollBar.Maximum - scrollBar.LargeChange + 1;
            int newValue = value + distance;
            newValue = (newValue < 0 ? 0 : (newValue > maxValue ? maxValue : newValue));
            if (newValue == value) return;

            // Došlo ke změně hodnoty. Mám dvě možnosti, jak ji do controlu dostat:
            //  a) Vložím ji do zde používaného ScrollBaru:
            //        scrollBar.Value = newValue;
            //    Ale to vede k následujícímu: pokud zde určím hodnotu, kterou následně ScrollBar pošle skrze ScrollBars_ValueChanged(), do ApplyScrollBarsToVirtualLocation(),
            //    a pak do SetVirtualLocation(), kde následně dojde ke korekci hodnoty ScrollBaru = v části kódu: if (changeX || changeY) ... 
            //    Pak se hodnota daného ScrollBaru znovu změní a proběhne rekurze celé sekvence.
            //  b) Čistší řešení je určit cílovou souřadnici zde, poslat ji do metody SetVirtualLocation() přímo odsud,
            //    a tam se pak případně nastaví aktuálně platné hodnoty ScrollBarů.
            if (orientation.Value == Orientation.Horizontal)
                SetVirtualLocation(new Point(newValue, _VScrollBarCurrentValue));
            else
                SetVirtualLocation(new Point(_HScrollBarCurrentValue, newValue));
        }
        /// <summary>
        /// Hodnoty ze ScrollBarů (pokud jsou viditelné) aplikuje do <see cref="SetVirtualLocation(Point)"/>
        /// </summary>
        private void ApplyScrollBarsToVirtualLocation()
        {
            SetVirtualLocation(new Point(_HScrollBarCurrentValue, _VScrollBarCurrentValue));
        }
        /// <summary>
        /// Hodnota true potlačí vyvolání události <see cref="OnContentVirtualBoundsChanged"/> a eventu <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        private bool _SuppressEvent;
        #endregion
    }
    /// <summary>
    /// Horizontální ScrollBar (vodorovný = zleva doprava)
    /// </summary>
    public class DxHScrollBar : DevExpress.XtraEditors.HScrollBar
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region Indikátory
        /// <summary>
        /// Indikátory, označující část plochy scrollbaru
        /// </summary>
        public ScrollBarIndicators Indicators { get { if (_Indicators == null) _Indicators = new ScrollBarIndicators(this, Orientation.Horizontal); return _Indicators; } }
        private ScrollBarIndicators _Indicators;
        /// <summary>
        /// Obsahuje true, pokud máme reálně nějaké indikátory
        /// </summary>
        protected bool HasIndicators { get { return (_Indicators != null && _Indicators.HasIndicators); } }
        /// <summary>
        /// Kreslení ScrollBaru vyvolá i kreslení indikátorů
        /// </summary>
        /// <param name="args"></param>
        protected override void OnPaint(ScrollBarInfoArgs args)
        {
            base.OnPaint(args);
            _Indicators?.PaintIndicators(args);
        }
        #endregion
    }
    /// <summary>
    /// Vertikální ScrollBar (svislý = zeshora dolů)
    /// </summary>
    public class DxVScrollBar : DevExpress.XtraEditors.VScrollBar
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region Indikátory
        /// <summary>
        /// Indikátory, označující část plochy scrollbaru
        /// </summary>
        public ScrollBarIndicators Indicators { get { if (_Indicators == null) _Indicators = new ScrollBarIndicators(this, Orientation.Vertical); return _Indicators; } }
        private ScrollBarIndicators _Indicators;
        /// <summary>
        /// Obsahuje true, pokud máme reálně nějaké indikátory
        /// </summary>
        protected bool HasIndicators { get { return (_Indicators != null && _Indicators.HasIndicators); } }
        /// <summary>
        /// Kreslení ScrollBaru vyvolá i kreslení indikátorů
        /// </summary>
        /// <param name="args"></param>
        protected override void OnPaint(ScrollBarInfoArgs args)
        {
            base.OnPaint(args);
            _Indicators?.PaintIndicators(args);
        }
        #endregion
    }
    /// <summary>
    /// Třída definující a vykreslující sadu indikátorů v prostoru ScrollBaru
    /// </summary>
    public class ScrollBarIndicators
    {
        #region Konstrukce a základní public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="orientation"></param>
        public ScrollBarIndicators(DevExpress.XtraEditors.ScrollTouchBase owner, Orientation orientation)
        {
            _Owner = owner;
            _Orientation = orientation;
            _Indicators = new List<Indicator>();
            _ColorAlphaArea = 200;
            _ColorAlphaThumb = 90;
            _Effect3DRatio = 0.25f;
        }
        private DevExpress.XtraEditors.ScrollTouchBase _Owner;
        private Orientation _Orientation;
        private List<Indicator> _Indicators;
        /// <summary>
        /// Obsahuje true, pokud v této sadě indikátorů je alespoň jeden platný indikátor, který se má vykreslit
        /// </summary>
        public bool HasIndicators { get { return _Indicators.Any(i => i.IsValid); } }
        /// <summary>
        /// Obsahuje počet všech indikátorů
        /// </summary>
        public int Count { get { return _Indicators.Count; } }
        /// <summary>
        /// Pole indikátorů
        /// </summary>
        public Indicator[] Indicators { get { return _Indicators.ToArray(); } }
        /// <summary>
        /// Přidat další indikátor.
        /// Po změnách indikátorů je nutno vyvolat <see cref="Refresh()"/>, jinak budou vykresleny "až tam uživatel najede myší".
        /// </summary>
        /// <param name="values"></param>
        /// <param name="alignment"></param>
        /// <param name="color"></param>
        public void AddIndicator(Int32Range values, ScrollBarIndicatorType alignment, Color color)
        {
            if ((values?.Size ?? 0) > 0)
                _Indicators.Add(new Indicator(values, alignment, color));
        }
        /// <summary>
        /// Smaže pole indikátorů.
        /// Po změnách indikátorů je nutno vyvolat <see cref="Refresh()"/>, jinak budou vykresleny "až tam uživatel najede myší".
        /// </summary>
        public void Clear()
        {
            _Indicators.Clear();
        }
        /// <summary>
        /// Odstraní indikátory vyhovující danému filtru.
        /// Po změnách indikátorů je nutno vyvolat <see cref="Refresh()"/>, jinak budou vykresleny "až tam uživatel najede myší".
        /// </summary>
        public void Remove(Predicate<Indicator> filter)
        {
            _Indicators.Remove(filter);
        }
        /// <summary>
        /// Metoda zajistí překreslení zadaných indikátorů.
        /// Je nutno volat po změně indikátorů (metody: <see cref="Clear()"/>, <see cref="AddIndicator(Int32Range, ScrollBarIndicatorType, Color)"/>, <see cref="Remove(Predicate{Indicator})"/>...
        /// </summary>
        public void Refresh()
        {
            _Owner.Refresh();
        }
        /// <summary>
        /// Průhlednost zadané barvy indikátoru při vykreslení mimo thumb (=přímo viditelná).
        /// Výchozí hodnota = 200, maximum = 255 (plná barva přes thumb, nehezké), minimum = 20, rozumné minimum = 140;
        /// </summary>
        public int ColorAlphaArea { get { return _ColorAlphaArea; } set { _ColorAlphaArea = value.Align(20, 255); } }
        private int _ColorAlphaArea;
        /// <summary>
        /// Průhlednost zadané barvy indikátoru při vykreslení do prostoru thumbu (=lehce překrytá thumbem).
        /// Výchozí hodnota = 90, maximum = 255 (plná barva přes thumb, nehezké), minimum = 20, rozumné minimum = 50;
        /// </summary>
        public int ColorAlphaThumb { get { return _ColorAlphaThumb; } set { _ColorAlphaThumb = value.Align(20, 255); } }
        private int _ColorAlphaThumb;
        /// <summary>
        /// Síla efektu 3D pro prvky, které mají vlastnost <see cref="ScrollBarIndicatorType.InnerGradientEffect"/> nebo <see cref="ScrollBarIndicatorType.OutsideGradientEffect"/>.
        /// Hodnota 0 = plochý prvek (to se ale nemusí nastavovat Gardient), hodnota 1 je maximum (příšerně kulatý prvek), defaultní = 0.25f.
        /// </summary>
        public float Effect3DRatio { get { return _Effect3DRatio; } set { _Effect3DRatio = value.Align(0f, 1f); } }
        private float _Effect3DRatio;
        #endregion
        #region Kreslení
        /// <summary>
        /// Vykreslí svoje indikátory
        /// </summary>
        /// <param name="args"></param>
        internal void PaintIndicators(ScrollBarInfoArgs args)
        {
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    _PaintIndicatorsHorizontal(args);
                    break;
                case Orientation.Vertical:
                    _PaintIndicatorsVertical(args);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí svoje indikátory - Horizontal
        /// </summary>
        /// <param name="args"></param>
        private void _PaintIndicatorsHorizontal(ScrollBarInfoArgs args)
        {
            // Rozsah viditelných pixelů i překrytí prvkem Thumb - v souřadnici Y:
            var areaBegin = args.DecButtonBounds.Right;
            var thumbBegin = args.ThumbButtonBounds.X;
            var thumbEnd = args.ThumbButtonBounds.Right;
            var areaEnd = args.IncButtonBounds.X;
            var areaBefore = new Int32Range(areaBegin, thumbBegin);
            var areaThumb = new Int32Range(thumbBegin, thumbEnd);
            var areaAfter = new Int32Range(thumbEnd, areaEnd);

            // Cache pro souřadnice a efekty, pro typy reálně použité v indikátorech:
            var sizeCache = new Dictionary<ScrollBarIndicatorType, Tuple<Int32Range, Gradient3DEffectType>>();

            // Přepočtová funkce z value (X) na visual (Y) hodnoty:
            var function = Algebra.GetLinearEquation(args.ViewInfo.Minimum, areaBegin, args.ViewInfo.Maximum, areaEnd);
            foreach (var indicator in _Indicators)
            {
                // Pixely na výšku:
                Int32Range sizeV = _GetVSize(indicator.Alignment, args.IncButtonBounds.Y, args.IncButtonBounds.Bottom, sizeCache, out var effect);
                int vBegin = sizeV.Begin;
                int vEnd = sizeV.End;

                Int32Range sizeL = _GetVisualRangeIndicator(indicator.Values, function, areaBegin, areaEnd); // Celý viditelný rozsah aktuálního intervalu na šířku

                // Před thumbem:
                Int32Range partBefore = Int32Range.Intersect(areaBefore, sizeL);
                if (partBefore != null && partBefore.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(partBefore.Begin, vBegin, partBefore.End, vEnd), effect);

                // Over thumb:
                Int32Range partThumb = Int32Range.Intersect(areaThumb, sizeL);
                if (partThumb != null && partThumb.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaThumb, Rectangle.FromLTRB(partThumb.Begin, vBegin, partThumb.End, vEnd), effect);

                // Pod thumbem:
                Int32Range partAfter = Int32Range.Intersect(areaAfter, sizeL);
                if (partAfter != null && partAfter.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(partAfter.Begin, vBegin, partAfter.End, vEnd), effect);
            }
        }
        /// <summary>
        /// Vykreslí svoje indikátory - Vertical
        /// </summary>
        /// <param name="args"></param>
        private void _PaintIndicatorsVertical(ScrollBarInfoArgs args)
        {
            // Rozsah viditelných pixelů i překrytí prvkem Thumb - v souřadnici Y:
            var areaBegin = args.DecButtonBounds.Bottom;
            var thumbBegin = args.ThumbButtonBounds.Y;
            var thumbEnd = args.ThumbButtonBounds.Bottom;
            var areaEnd = args.IncButtonBounds.Y;
            var areaBefore = new Int32Range(areaBegin, thumbBegin);
            var areaThumb = new Int32Range(thumbBegin, thumbEnd);
            var areaAfter = new Int32Range(thumbEnd, areaEnd);

            // Cache pro souřadnice a efekty, pro typy reálně použité v indikátorech:
            var sizeCache = new Dictionary<ScrollBarIndicatorType, Tuple<Int32Range, Gradient3DEffectType>>();

            // Přepočtová funkce z value (X) na visual (Y) hodnoty:
            var function = Algebra.GetLinearEquation(args.ViewInfo.Minimum, areaBegin, args.ViewInfo.Maximum, areaEnd);
            foreach (var indicator in _Indicators)
            {
                // Pixely na šířku:
                Int32Range sizeV = _GetVSize(indicator.Alignment, args.IncButtonBounds.X, args.IncButtonBounds.Right, sizeCache, out var effect);
                int vBegin = sizeV.Begin;
                int vEnd = sizeV.End;

                Int32Range sizeL = _GetVisualRangeIndicator(indicator.Values, function, areaBegin, areaEnd); // Celý viditelný rozsah aktuálního intervalu na výšku

                // Před thumbem:
                Int32Range partBefore = Int32Range.Intersect(areaBefore, sizeL);
                if (partBefore != null && partBefore.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(vBegin, partBefore.Begin, vEnd, partBefore.End), effect);

                // Over thumb:
                Int32Range partThumb = Int32Range.Intersect(areaThumb, sizeL);
                if (partThumb != null && partThumb.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaThumb, Rectangle.FromLTRB(vBegin, partThumb.Begin, vEnd, partThumb.End), effect);

                // Pod thumbem:
                Int32Range partAfter = Int32Range.Intersect(areaAfter, sizeL);
                if (partAfter != null && partAfter.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(vBegin, partAfter.Begin, vEnd, partAfter.End), effect);
            }
        }
        /// <summary>
        /// Metoda vrátí vizuální rozsah (odkud kam v pixelech zobrazen) je pro daný datový rozsah (od jaké do jaké hodnoty se nachází),
        /// s pomocí dané lineární rovnice.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="function"></param>
        /// <param name="areaBegin"></param>
        /// <param name="areaEnd"></param>
        /// <returns></returns>
        private Int32Range _GetVisualRangeIndicator(Int32Range values, Algebra.LinearEquation function, int areaBegin, int areaEnd)
        {
            int visualBegin = (int)Math.Round(function.GetY(values.Begin), 0);
            int visualEnd = (int)Math.Round(function.GetY(values.End), 0);

            // Korekce - chceme indikátor vidět i tehdy, když jeho exaktní velikost je 0 (nebo 1) pixel, tedy chceme nejméně 2 pixely azarovnané do viditelného rozmezí:
            int visualSize = visualEnd - visualBegin;
            if (visualSize <= 0)
            {
                visualBegin = (visualBegin - 1).Align(areaBegin, areaEnd - 2);
                visualEnd = visualBegin + 2;
            }
            else if (visualSize == 1)
            {
                visualBegin = visualBegin.Align(areaBegin, areaEnd - 2);
                visualEnd = visualBegin + 2;
            }
            return new Int32Range(visualBegin, visualEnd);
        }
        /// <summary>
        /// Vrátí rozsah pozice indikátoru dle jeho šířky
        /// </summary>
        /// <param name="type"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="effect"></param>
        /// <param name="sizeCache"></param>
        /// <returns></returns>
        private Int32Range _GetVSize(ScrollBarIndicatorType type, int begin, int end,
            Dictionary<ScrollBarIndicatorType, Tuple<Int32Range, Gradient3DEffectType>> sizeCache, 
            out Gradient3DEffectType effect)
        {
            Tuple<Int32Range, Gradient3DEffectType> info;
            if (!sizeCache.TryGetValue(type, out info))
            {
                int vb = begin + 2;
                int ve = end - 2;
                int vs = ve - vb;

                int start = vb;
                int size = vs;
                if (!type.HasFlag(ScrollBarIndicatorType.FullSize))
                {
                    if (type.HasFlag(ScrollBarIndicatorType.BigSize))
                        size = 20 * vs / 30;
                    else if (type.HasFlag(ScrollBarIndicatorType.HalfSize))
                        size = 15 * vs / 30;
                    else if (type.HasFlag(ScrollBarIndicatorType.ThirdSize))
                        size = 10 * vs / 30;

                    if (type.HasFlag(ScrollBarIndicatorType.Center))
                        start = vb + (vs - size) / 2;
                    else if (type.HasFlag(ScrollBarIndicatorType.Far))
                        start = ve - size;
                }
                Gradient3DEffectType ef = (type.HasFlag(ScrollBarIndicatorType.InnerGradientEffect) ? Gradient3DEffectType.Inset :
                                       (type.HasFlag(ScrollBarIndicatorType.OutsideGradientEffect) ? Gradient3DEffectType.Outward : Gradient3DEffectType.None));

                info = new Tuple<Int32Range, Gradient3DEffectType>(new Int32Range(start, start + size), ef);
                sizeCache.Add(type, info);
            }
            effect = info.Item2;
            return info.Item1;
        }
        /// <summary>
        /// Vykreslí jeden daný indikátor
        /// </summary>
        /// <param name="args"></param>
        /// <param name="indicator"></param>
        /// <param name="alpha"></param>
        /// <param name="bounds"></param>
        /// <param name="effect"></param>
        private void _PaintIndicatorOne(ScrollBarInfoArgs args, Indicator indicator, int alpha, Rectangle bounds, Gradient3DEffectType effect)
        {
            if (indicator.Color.A < 255) alpha = alpha * indicator.Color.A / 255;        // Sloučení Alpha kanálu z dodané barvy + explicitní Alpha definovaná indikátorem
            Color color = Color.FromArgb(alpha, indicator.Color);
            switch (effect)
            {
                case Gradient3DEffectType.Inset:
                case Gradient3DEffectType.Outward:
                    float effectRatio = (effect == Gradient3DEffectType.Inset ? -_Effect3DRatio : _Effect3DRatio);
                    using (var brush = DxComponent.PaintCreateBrushForGradient(bounds, color, _Orientation, effectRatio))
                        args.Graphics.FillRectangle(brush, bounds);
                    break;
                default:
                    args.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color), bounds);
                    break;
            }
        }
        #endregion
        #region class Indicator = Třída jednoho konkrétního indikátoru = značky
        /// <summary>
        /// Třída jednoho konkrétního indikátoru = značky
        /// </summary>
        public class Indicator
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="values"></param>
            /// <param name="alignment"></param>
            /// <param name="color"></param>
            public Indicator(Int32Range values, ScrollBarIndicatorType alignment, Color color)
            {
                Values = values;
                Alignment = alignment;
                Color = color;
            }
            /// <summary>
            /// Rozmezí indikátoru v datech
            /// </summary>
            public Int32Range Values { get; private set; }
            /// <summary>
            /// Umístění indikátoru na ScrollBaru
            /// </summary>
            public ScrollBarIndicatorType Alignment { get; private set; }
            /// <summary>
            /// Barva indikátoru, smí obsahovat Alpha kanál (průhlednost)
            /// </summary>
            public Color Color { get; private set; }
            /// <summary>
            /// Tento indikátor je platný? Tzn. jeho <see cref="Values"/> má kladnou délku
            /// </summary>
            public bool IsValid { get { return ((Values?.Size ?? 0) > 0); } }
        }
        #endregion
    }
    #region enum ScrollBarIndicatorType
    /// <summary>
    /// Typy indikátorů na ScrollBaru, používají se v <see cref="ScrollBarIndicators"/>
    /// </summary>
    [Flags]
    public enum ScrollBarIndicatorType
    {
        /// <summary>Žádný</summary>
        None = 0,

        /// <summary>U vnitřního okraje (vlevo / nahoře)</summary>
        Near = 0x0001,
        /// <summary>Uprostřed</summary>
        Center = 0x0002,
        /// <summary>U vzdálenějšího okraje (vpravo / dole)</summary>
        Far = 0x0004,
        /// <summary>Přes plnou velikost (pak není třeba určovat <see cref="Near"/> / <see cref="Center"/> / <see cref="Far"/>)</summary>
        FullSize = 0x0010,
        /// <summary>Poloviční velikost ScrollBaru</summary>
        HalfSize = 0x0020,
        /// <summary>Třetina ScrollBaru</summary>
        ThirdSize = 0x0040,
        /// <summary>Dvě třetiny ScrollBaru</summary>
        BigSize = 0x0080,
        /// <summary>Gradient "dovnitř" = "dolů"</summary>
        InnerGradientEffect = 0x0100,
        /// <summary>Gradient "vně" = "nahoru"</summary>
        OutsideGradientEffect = 0x0200,

        /// <summary>Poloviční velikost, vnitřní okraj</summary>
        HalfNear = HalfSize | Near,
        /// <summary>Poloviční velikost, uprostřed</summary>
        HalfCenter = HalfSize | Center,
        /// <summary>Poloviční velikost, vnější okraj</summary>
        HalfFar = HalfSize | Far,
        /// <summary>Třetinová velikost, vnitřní okraj</summary>
        ThirdNear = ThirdSize | Near,
        /// <summary>Třetinová velikost, uprostřed</summary>
        ThirdCenter = ThirdSize | Center,
        /// <summary>Třetinová velikost, vnější okraj</summary>
        ThirdFar = ThirdSize | Far,
        /// <summary>Dvoutřetinová velikost, vnitřní okraj</summary>
        BigNear = BigSize | Near,
        /// <summary>Dvoutřetinová velikost, uprostřed</summary>
        BigCenter = BigSize | Center,
        /// <summary>Dvoutřetinová velikost, vnější okraj</summary>
        BigFar = BigSize | Far,

        /// <summary>Defaultní = Dvoutřetinová velikost, uprostřed</summary>
        Default = BigCenter
    }
    #endregion
    #endregion
    #region DxSplitContainerControl
    /// <summary>
    /// SplitContainerControl
    /// </summary>
    public class DxSplitContainerControl : DevExpress.XtraEditors.SplitContainerControl
    { }
    #endregion
    #region DxTabPane : Control se záložkami = DevExpress.XtraBars.Navigation.TabPane
    /// <summary>
    /// Control se záložkami = <see cref="DevExpress.XtraBars.Navigation.TabPane"/>
    /// </summary>
    public class DxTabPane : DevExpress.XtraBars.Navigation.TabPane
    {
        #region Konstruktor a zjednodušené přidání záložky
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTabPane()
        {
            InitProperties();
            InitEvents();
        }
        /// <summary>
        /// Přidá novou stránku (záložku) do this containeru
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="pageText"></param>
        /// <param name="pageToolTip"></param>
        /// <param name="pageImageName"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.Navigation.TabNavigationPage AddNewPage(string pageName, string pageText, string pageToolTip = null, string pageImageName = null)
        {
            string text = pageText;
            var page = this.CreateNewPage() as DevExpress.XtraBars.Navigation.TabNavigationPage;
            page.Name = pageName;
            page.Caption = text;
            page.PageText = text;
            page.ToolTip = pageToolTip;
            page.ImageOptions.Image = null; // pageImageName tabHeaderItem.Image;
            page.Properties.ShowMode = DevExpress.XtraBars.Navigation.ItemShowMode.ImageAndText;

            this.Pages.Add(page);

            return page;
        }
        /// <summary>
        /// Odebere danou stránku
        /// </summary>
        /// <param name="page"></param>
        public void RemovePage(DevExpress.XtraBars.Navigation.TabNavigationPage page)
        {
            this.Pages.Remove(page);
        }
        /// <summary>
        /// Odebere stránku daného jména
        /// </summary>
        /// <param name="pageName"></param>
        public void RemovePage(string pageName)
        {
            this.Pages.Remove(p => p.Name == pageName);
        }
        /// <summary>
        /// Zahodí všechny stránky
        /// </summary>
        public void ClearPages()
        {
            this.Pages.Clear();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.RemoveEvents();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void InitProperties()
        {
            this.TabAlignment = DevExpress.XtraEditors.Alignment.Near;           // Near = doleva, Far = doprava, Center = uprostřed
            this.PageProperties.AllowBorderColorBlending = true;
            this.PageProperties.ShowMode = DevExpress.XtraBars.Navigation.ItemShowMode.ImageAndText;
            this.AllowCollapse = DevExpress.Utils.DefaultBoolean.False;          // Nedovolí uživateli skrýt headery

            this.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            this.LookAndFeel.UseWindowsXPTheme = true;
            this.OverlayResizeZoneThickness = 20;
            this.ItemOrientation = SWF.Orientation.Horizontal;                       // Vertical = kreslí řadu záhlaví vodorovně, ale obsah jednotlivého buttonu svisle :-(

            this.TransitionType = DxTabPaneTransitionType.FadeFast;

            // Požadavky designu na vzhled buttonů:
            this.AppearanceButton.Normal.FontSizeDelta = 2;
            this.AppearanceButton.Normal.FontStyleDelta = FontStyle.Regular;
            this.AppearanceButton.Normal.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.AppearanceButton.Hovered.FontSizeDelta = 2;
            this.AppearanceButton.Hovered.FontStyleDelta = FontStyle.Underline;
            this.AppearanceButton.Hovered.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.AppearanceButton.Pressed.FontSizeDelta = 2;
            this.AppearanceButton.Pressed.FontStyleDelta = FontStyle.Bold;
            this.AppearanceButton.Pressed.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        }
        /// <summary>
        /// Typ přechodového efektu
        /// </summary>
        public new DxTabPaneTransitionType TransitionType
        {
            get { return _TransitionType; }
            set
            {
                _TransitionType = value;
                DxTabPaneTransitionType type = value & DxTabPaneTransitionType.AllTypes;
                if (type == DxTabPaneTransitionType.None)
                {
                    this.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.False;
                }
                else
                {
                    this.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;
                    this.TransitionManager.UseDirectXPaint = DefaultBoolean.True;
                    switch (type)
                    {
                        case DxTabPaneTransitionType.Fade:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Fade;
                            break;
                        case DxTabPaneTransitionType.Slide:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.SlideFade;
                            break;
                        case DxTabPaneTransitionType.Push:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Push;
                            break;
                        case DxTabPaneTransitionType.Shape:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Shape;
                            break;
                        default:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Fade;
                            break;
                    }

                    DxTabPaneTransitionType time = value & DxTabPaneTransitionType.AllTimes;
                    switch (time)
                    {
                        case DxTabPaneTransitionType.Fast:
                            this.TransitionAnimationProperties.FrameCount = 50;                  // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        case DxTabPaneTransitionType.Medium:
                            this.TransitionAnimationProperties.FrameCount = 100;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        case DxTabPaneTransitionType.Slow:
                            this.TransitionAnimationProperties.FrameCount = 250;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        case DxTabPaneTransitionType.VerySlow:
                            this.TransitionAnimationProperties.FrameCount = 500;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        default:
                            this.TransitionAnimationProperties.FrameCount = 100;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                    }
                }
            }
        }
        private DxTabPaneTransitionType _TransitionType;
        /// <summary>
        /// Aktivuje vlastní eventy
        /// </summary>
        private void InitEvents()
        {
            this.TransitionManager.BeforeTransitionStarts += TransitionManager_BeforeTransitionStarts;
            this.TransitionManager.AfterTransitionEnds += TransitionManager_AfterTransitionEnds;
            this.SelectedPageChanging += DxTabPane_SelectedPageChanging;
            this.SelectedPageChanged += DxTabPane_SelectedPageChanged;
        }
        /// <summary>
        /// Deaktivuje vlastní eventy
        /// </summary>
        private void RemoveEvents()
        {
            this.TransitionManager.BeforeTransitionStarts -= TransitionManager_BeforeTransitionStarts;
            this.TransitionManager.AfterTransitionEnds -= TransitionManager_AfterTransitionEnds;
            this.SelectedPageChanging -= DxTabPane_SelectedPageChanging;
            this.SelectedPageChanged -= DxTabPane_SelectedPageChanged;
        }
        #endregion
        #region Přepínání záložek a volání událostí pro podporu deaktivace a aktivace správné stránky
        /// <summary>
        /// Obsahuje true, pokud jsme v procesu přepínání záložek, false v běžném stavu
        /// </summary>
        public bool PageChangingIsRunning { get; private set; }
        /// <summary>
        /// Obsahuje dřívější aktivní stránku před procesem přepínání záložek
        /// </summary>
        public DevExpress.XtraBars.Navigation.TabNavigationPage PageChangingPageOld { get; private set; }
        /// <summary>
        /// Obsahuje novou aktivní stránku po procesu přepínání záložek
        /// </summary>
        public DevExpress.XtraBars.Navigation.TabNavigationPage PageChangingPageNew { get; private set; }

        /// <summary>
        /// Zajistí události pro přípravu obsahu nové stránky před přepnutím na ni (stránka dosud není vidět)
        /// </summary>
        /// <param name="pageNew"></param>
        private void RunPageChangingPrepare(DevExpress.XtraBars.Navigation.TabNavigationPage pageNew)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageNew);
            OnPageChangingPrepare(args);
            PageChangingPrepare?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro přípravu obsahu nové stránky před přepnutím na ni (stránka dosud není vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingPrepare(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při přípravu obsahu nové stránky před přepnutím na ni (stránka dosud není vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingPrepare;

        /// <summary>
        /// Zajistí události pro aktivaci obsahu nové stránky po přepnutím na ni (stránka už je vidět)
        /// </summary>
        /// <param name="pageNew"></param>
        private void RunPageChangingActivate(DevExpress.XtraBars.Navigation.TabNavigationPage pageNew)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageNew);
            OnPageChangingActivate(args);
            PageChangingActivate?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro aktivaci obsahu nové stránky po přepnutím na ni (stránka už je vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingActivate(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při aktivaci obsahu nové stránky po přepnutím na ni (stránka už je vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingActivate;

        /// <summary>
        /// Zajistí události pro deaktivaci obsahu staré stránky před přepnutím z ní (stránka je dosud vidět)
        /// </summary>
        /// <param name="pageOld"></param>
        private void RunPageChangingDeactivate(DevExpress.XtraBars.Navigation.TabNavigationPage pageOld)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageOld);
            OnPageChangingDeactivate(args);
            PageChangingDeactivate?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro deaktivaci obsahu staré stránky před přepnutím z ní (stránka je dosud vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingDeactivate(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při deaktivaci obsahu staré stránky před přepnutím z ní (stránka je dosud vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingDeactivate;

        /// <summary>
        /// Zajistí události pro uvolnění obsahu staré stránky před přepnutím z ní (stránka už není vidět)
        /// </summary>
        /// <param name="pageOld"></param>
        private void RunPageChangingRelease(DevExpress.XtraBars.Navigation.TabNavigationPage pageOld)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageOld);
            OnPageChangingRelease(args);
            PageChangingRelease?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro uvolnění obsahu staré stránky před přepnutím z ní (stránka už není vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingRelease(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při uvolnění obsahu staré stránky před přepnutím z ní (stránka už není vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingRelease;

        // Pořadí eventů v konstuktoru:
        //     DxTabPane_SelectedPageChanging;
        //     DxTabPane_SelectedPageChanged;
        // Pořadí eventů při přepínání stránky s transicí:        SelectedPageIndex
        //     DxTabPane_SelectedPageChanging;                           old           mám k dispozici old i new page v argumentu
        //     TransitionManager_BeforeTransitionStarts;                 old
        //     DxTabPane_SelectedPageChanged;                            new
        //     TransitionManager_AfterTransitionEnds;                    new

        private void TransitionManager_BeforeTransitionStarts(DevExpress.Utils.Animation.ITransition transition, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void TransitionManager_AfterTransitionEnds(DevExpress.Utils.Animation.ITransition transition, EventArgs e)
        {
        }
        /// <summary>
        /// Na začátku přepínání záložek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxTabPane_SelectedPageChanging(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangingEventArgs e)
        {
            this.PageChangingPageOld = e.OldPage as DevExpress.XtraBars.Navigation.TabNavigationPage;
            this.PageChangingPageNew = e.Page as DevExpress.XtraBars.Navigation.TabNavigationPage;
            this.PageChangingIsRunning = true;
            this.RunPageChangingDeactivate(this.PageChangingPageOld);
            this.RunPageChangingPrepare(this.PageChangingPageNew);
        }
        /// <summary>
        /// Na konci přepínání záložek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxTabPane_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            this.RunPageChangingActivate(this.PageChangingPageNew);
            this.RunPageChangingRelease(this.PageChangingPageOld);
            this.PageChangingIsRunning = false;

        }
        #endregion
    }
    #region enum DxTabPaneTransitionType
    /// <summary>
    /// Typ přechodového efektu v <see cref="DxTabPane"/>
    /// </summary>
    [Flags]
    public enum DxTabPaneTransitionType
    {
        /// <summary>Žádný efekt</summary>
        None = 0,

        /// <summary>Rychlý</summary>
        Fast = 0x0001,
        /// <summary>Střední</summary>
        Medium = 0x0002,
        /// <summary>Pomalý</summary>
        Slow = 0x0004,
        /// <summary>Extra pomalý</summary>
        VerySlow = 0x0008,

        /// <summary></summary>
        Fade = 0x0100,
        /// <summary></summary>
        Slide = 0x0200,
        /// <summary></summary>
        Push = 0x0400,
        /// <summary></summary>
        Shape = 0x0800,

        /// <summary>Kombinace</summary>
        FadeFast = Fade | Fast,
        /// <summary>Kombinace</summary>
        FadeMedium = Fade | Medium,
        /// <summary>Kombinace</summary>
        FadeSlow = Fade | Slow,

        /// <summary>Kombinace</summary>
        SlideFast = Slide | Fast,
        /// <summary>Kombinace</summary>
        SlideMedium = Slide | Medium,
        /// <summary>Kombinace</summary>
        SlideSlow = Slide | Slow,

        /// <summary>Kombinace</summary>
        PushFast = Push | Fast,
        /// <summary>Kombinace</summary>
        PushMedium = Push | Medium,
        /// <summary>Kombinace</summary>
        PushSlow = Push | Slow,

        /// <summary>Kombinace</summary>
        ShapeFast = Shape | Fast,
        /// <summary>Kombinace</summary>
        ShapeMedium = Shape | Medium,
        /// <summary>Kombinace</summary>
        ShapeSlow = Shape | Slow,

        /// <summary>Kombinace</summary>
        Default = FadeFast,

        /// <summary>Kombinace</summary>
        AllTimes = Fast | Medium | Slow | VerySlow,
        /// <summary>Kombinace</summary>
        AllTypes = Fade | Slide | Push | Shape
    }
    #endregion
    #endregion
    #region DxLabelControl
    /// <summary>
    /// LabelControl
    /// </summary>
    public class DxLabelControl : DevExpress.XtraEditors.LabelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLabelControl()
        {
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetType().Name + "'" + (this.Text ?? "NULL") + "'"; }
        #endregion
    }
    #endregion
    #region DxTextEdit
    /// <summary>
    /// TextEdit
    /// </summary>
    public class DxTextEdit : DevExpress.XtraEditors.TextEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTextEdit()
        {
            EnterMoveNextControl = true;
        }
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region HasMouse
        /// <summary>
        /// Panel má na sobě myš?
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxButtonEdit
    /// <summary>
    /// Třída spojující TextEdit + sadu buttonů, implicitně jeden vpravo.
    /// </summary>
    public class DxButtonEdit : DevExpress.XtraEditors.ButtonEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxButtonEdit()
        {
            _ButtonsVisibility = DxChildControlVisibility.Allways;
            _ButtonsIsVisible = null;
        }

        #region Buttony mohou být viditelné jen na 'aktivním' prvku
        /// <summary>
        /// Viditelnost buttonů z hlediska aktivity
        /// </summary>
        public DxChildControlVisibility ButtonsVisibility { get { return _ButtonsVisibility; } set { _ButtonsVisibility = value; RefreshButtonsVisibility(); } }
        private DxChildControlVisibility _ButtonsVisibility;
        /// <summary>
        /// Předdefinovaný druh prvního buttonu
        /// </summary>
        public DevExpress.XtraEditors.Controls.ButtonPredefines ButtonKind
        {
            get { return this.Properties.Buttons[0].Kind; }
            set { this.Properties.Buttons[0].Kind = value; }
        }
        /// <summary>
        /// Předdefinovaný styl zobrazení buttonů
        /// </summary>
        public DevExpress.XtraEditors.Controls.BorderStyles ButtonsStyle
        {
            get { return this.Properties.ButtonsStyle; }
            set { this.Properties.ButtonsStyle = value; }
        }
        /// <summary>
        /// Nastaví aktuální viditelnost buttonu podle definice a podle aktuálního stavu
        /// </summary>
        private void RefreshButtonsVisibility()
        {
            var visibility = ButtonsVisibility;
            bool isVisible = (visibility.HasFlag(DxChildControlVisibility.Allways) ||
                              (visibility.HasFlag(DxChildControlVisibility.OnMouse) && HasMouse) ||
                              (visibility.HasFlag(DxChildControlVisibility.OnFocus) && HasFocus));
            if (!_ButtonsIsVisible.HasValue || isVisible != _ButtonsIsVisible.Value)
            {
                _ButtonsIsVisible = isVisible;
                this.Properties.Buttons.ForEachExec(b => b.Visible = isVisible);
            }
        }
        private bool? _ButtonsIsVisible;
        #endregion
        #region HasMouse
        /// <summary>
        /// Panel má na sobě myš?
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    RefreshButtonsVisibility();
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            Point location = this.PointToClient(Control.MousePosition);
            base.OnMouseLeave(e);
            if (!this.ClientRectangle.Contains(location))
                this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    RefreshButtonsVisibility();
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
    }
    #endregion
    #region DxMemoEdit
    /// <summary>
    /// MemoEdit
    /// </summary>
    public class DxMemoEdit : DevExpress.XtraEditors.MemoEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxTokenEdit
    /// <summary>
    /// <see cref="DxTokenEdit"/>
    /// </summary>
    public class DxTokenEdit : DevExpress.XtraEditors.TokenEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTokenEdit()
        {
            _Tokens = new List<IMenuItem>();
            ShowDropDown = true;
        }
        #region Tokeny = položky v nabídce
        /// <summary>
        /// Soupis nabídek v tomto prvku. Lze setovat.
        /// </summary>
        public IEnumerable<IMenuItem> Tokens
        {
            get { return _Tokens; }
            set { _AddTokens(value, true); }
        }
        /// <summary>
        /// Zobrazovat DropDown?
        /// </summary>
        public bool ShowDropDown { get { return this.Properties.ShowDropDown; } set { this.Properties.ShowDropDown = value; } }
        /// <summary>
        /// Počet nabídek v tomto prvku.
        /// </summary>
        public int TokensCount { get { return _Tokens.Count; } }
        /// <summary>
        /// Smaže stávající tokeny
        /// </summary>
        public void TokensClear()
        {
            this.Properties.Tokens.Clear();
            _Tokens.Clear();
        }
        /// <summary>
        /// Přidá další prvky do nabídek v tomto prvku.
        /// </summary>
        /// <param name="tokens"></param>
        public void TokensAddRange(IEnumerable<IMenuItem> tokens)
        {
            _AddTokens(tokens, false);
        }
        /// <summary>
        /// Do this prvku přidá dané tokeny. Volitelně před tím dosavadní prvky odstraní.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="clear"></param>
        private void _AddTokens(IEnumerable<IMenuItem> tokens, bool clear)
        { 
            var dxTokens = _CreateDxTokens(tokens);

            this.Properties.BeginUpdate();
            if (clear)
            {
                this.Properties.Tokens.Clear();
                _Tokens.Clear();
            }
            if (dxTokens.Count > 0)
            {
                this.Properties.Tokens.AddRange(dxTokens);
                _Tokens.AddRange(tokens);
            }
            this.Properties.EndUpdate();
        }
        /// <summary>
        /// Z dodaných dat typu <see cref="IMenuItem"/> vrátí pole prvků natvního typu pro TokenEdit : <see cref="DevExpress.XtraEditors.TokenEditToken"/>.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private List<DevExpress.XtraEditors.TokenEditToken> _CreateDxTokens(IEnumerable<IMenuItem> tokens)
        {
            List<DevExpress.XtraEditors.TokenEditToken> dxTokens = new List<DevExpress.XtraEditors.TokenEditToken>();
            if (tokens != null)
            {
                foreach (var token in tokens)
                    dxTokens.Add(new DevExpress.XtraEditors.TokenEditToken(token.Text, token.ItemId));
            }
            return dxTokens;
        }
        private List<IMenuItem> _Tokens;
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region HasMouse
        /// <summary>
        /// Panel má na sobě myš?
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion

    }
    #endregion
    #region DxImageComboBoxEdit
    /// <summary>
    /// ImageComboBoxEdit
    /// </summary>
    public class DxImageComboBoxEdit : DevExpress.XtraEditors.ImageComboBoxEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxSpinEdit
    /// <summary>
    /// SpinEdit
    /// </summary>
    public class DxSpinEdit : DevExpress.XtraEditors.SpinEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxCheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    public class DxCheckEdit : DevExpress.XtraEditors.CheckEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxListBoxControl
    /// <summary>
    /// ListBoxControl
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.ImageListBoxControl, IDxDragDropControl   // původně :ListBoxControl, nyní: https://docs.devexpress.com/WindowsForms/DevExpress.XtraEditors.ImageListBoxControl
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            KeyActionsInit();
            DxDragDropInit(DxDragDropActionType.None);
            ToolTipInit();
            // ReorderInit();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxDragDropDispose();
            ToolTipDispose();
        }
        /// <summary>
        /// Pole, obsahující informace o právě viditelných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, IMenuItem, Rectangle>[] VisibleItems
        {
            get
            {
                var listItems = this.ListItems;
                var visibleItems = new List<Tuple<int, IMenuItem, Rectangle>>();
                int topIndex = this.TopIndex;
                int index = (topIndex > 0 ? topIndex - 1 : topIndex);
                int count = this.ItemCount;
                while (index < count)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    if (bounds.HasValue)
                        visibleItems.Add(new Tuple<int, IMenuItem, Rectangle>(index, listItems[index], bounds.Value));
                    else if (index > topIndex)
                        break;
                    index++;
                }
                return visibleItems.ToArray();
            }
        }
        /// <summary>
        /// Pole, obsahující informace o právě selectovaných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, IMenuItem, Rectangle?>[] SelectedItemsInfo
        {
            get
            {
                var listItems = this.ListItems;
                var selectedItemsInfo = new List<Tuple<int, IMenuItem, Rectangle?>>();
                foreach (var index in this.SelectedIndices)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    selectedItemsInfo.Add(new Tuple<int, IMenuItem, Rectangle?>(index, listItems[index], bounds));
                }
                return selectedItemsInfo.ToArray();
            }
        }
        /// <summary>
        /// Obsahuje pole indexů prvků, které jsou aktuálně Selected. 
        /// Lze setovat. Setování nastaví stav Selected na určených prvcích this.Items. Ostatní budou not selected.
        /// </summary>
        public IEnumerable<int> SelectedIndexes
        {
            get
            {
                return this.SelectedIndices.ToArray();
            }
            set
            {
                int count = this.ItemCount;
                Dictionary<int, int> indexes = value.CreateDictionary(i => i, true);
                for (int i = 0; i < count; i++)
                {
                    bool isSelected = indexes.ContainsKey(i);
                    this.SetSelected(i, isSelected);
                }
            }
        }
        /// <summary>
        /// Obsahuje pole prvků, které jsou aktuálně Selected. 
        /// Lze setovat. Setování nastaví stav Selected na těch prvcích this.Items, které jsou Object.ReferenceEquals() shodné s některým dodaným prvkem. Ostatní budou not selected.
        /// </summary>
        public new IEnumerable<IMenuItem> SelectedItems
        {
            get
            {
                var listItems = this.ListItems;
                var selectedItems = new List<IMenuItem>();
                foreach (var index in this.SelectedIndices)
                    selectedItems.Add(listItems[index]);
                return selectedItems.ToArray();
            }
            set
            {
                var selectedItems = (value?.ToList() ?? new List<IMenuItem>());
                var listItems = this.ListItems;
                int count = this.ItemCount;
                for (int i = 0; i < count; i++)
                {
                    object item = listItems[i];
                    bool isSelected = selectedItems.Any(s => Object.ReferenceEquals(s, item));
                    this.SetSelected(i, isSelected);
                }
            }
        }
        /// <summary>
        /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
        /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
        /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
        /// Hodnota vyšší než 10 se akceptuje jako 10.
        /// </summary>
        public int ItemHeightPadding
        {
            get { return _ItemHeightPadding; }
            set
            {
                if (value > 0)
                {
                    int padding = (value > 10 ? 10 : value);
                    int fontheight = this.Appearance.GetFont().Height;
                    this.ItemAutoHeight = false;
                    this.ItemHeight = fontheight + (2 * padding);
                    _ItemHeightPadding = padding;
                }
                else
                {
                    this.ItemAutoHeight = true;
                    _ItemHeightPadding = 0;
                }
            }
        }
        private int _ItemHeightPadding = 0;
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        /// <summary>
        /// Prvky Listu typované na <see cref="IMenuItem"/>.
        /// Pokud v Listu budou obsaženy jiné prvky než <see cref="IMenuItem"/>, pak na jejich místě v tomto poli bude null.
        /// Toto pole má stejný počet prvků jako pole this.Items
        /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
        /// </summary>
        public IMenuItem[] ListItems
        {
            get
            {
                return this.Items.Select(i => i.Value as IMenuItem).ToArray();
            }
        }
        #endregion
        #region Overrides
        /// <summary>
        /// Při vykreslování
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(SWF.PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintList?.Invoke(this, e);
            this.MouseDragPaint(e);
        }
        /// <summary>
        /// Po stisku klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(SWF.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseItemIndex = -1;
        }
        #endregion
        #region Images
        /// <summary>
        /// Vrátí Image pro daný index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override Image GetItemImage(int index)
        {
            var menuItem = this.ListItems[index];
            if (menuItem != null && menuItem.Image != null)
                return DxComponent.GetImageFromResource(menuItem.Image);

            return base.GetItemImage(index);
        }
        /// <summary>
        /// Vrátí ImageSize pro daný index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override Size GetItemImageSize(int index)
        {
            return new Size(16, 16);
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy?
        /// </summary>
        public bool ToolTipAllowHtmlText { get; set; }
        private void ToolTipInit()
        {
            this.ToolTipController = DxComponent.CreateNewToolTipController();
            this.ToolTipController.GetActiveObjectInfo += ToolTipController_GetActiveObjectInfo;
        }
        private void ToolTipDispose()
        {
            this.ToolTipController?.Dispose();
        }
        /// <summary>
        /// Připraví ToolTip pro aktuální Node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTipController_GetActiveObjectInfo(object sender, ToolTipControllerGetActiveObjectInfoEventArgs e)
        {
            if (e.SelectedControl is DxListBoxControl listBox)
            {
                int index = listBox.IndexFromPoint(e.ControlMousePosition);
                if (index != -1)
                {
                    var menuItem = listBox.ListItems[index];
                    if (menuItem != null)
                    {
                        string toolTipText = menuItem.ToolTipText;
                        string toolTipTitle = menuItem.ToolTipTitle ?? menuItem.Text;
                        var ttci = new DevExpress.Utils.ToolTipControlInfo(menuItem, toolTipText, toolTipTitle);
                        ttci.ToolTipType = ToolTipType.SuperTip;
                        ttci.AllowHtmlText = (ToolTipAllowHtmlText ? DefaultBoolean.True : DefaultBoolean.False);
                        e.Info = ttci;
                    }
                }
            }
        }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
        #region Remove, Delete, CtrlC, CtrlX
        /// <summary>
        /// Povolené akce. Výchozí je <see cref="KeyActionType.None"/>
        /// </summary>
        public KeyActionType EnabledKeyActions { get; set; }
        /// <summary>
        /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="KeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
        /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
        /// </summary>
        /// <param name="actions"></param>
        public void DoKeyActions(params KeyActionType[] actions)
        {
            foreach (KeyActionType action in actions)
                _DoKeyAction(action, true);
        }
        /// <summary>
        /// Inicializace eventhandlerů a hodnot pro KeyActions
        /// </summary>
        private void KeyActionsInit()
        {
            this.PreviewKeyDown += DxListBoxControl_PreviewKeyDown;
            this.KeyDown += DxListBoxControl_KeyDown;
            this.EnabledKeyActions = KeyActionType.None;
        }
        private void DxListBoxControl_PreviewKeyDown(object sender, SWF.PreviewKeyDownEventArgs e)
        {
            
        }
        /// <summary>
        /// Obsluha kláves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxListBoxControl_KeyDown(object sender, SWF.KeyEventArgs e)
        {
            var enabledActions = EnabledKeyActions;
            switch (e.KeyData)
            {
                case SWF.Keys.Delete:
                    _DoKeyAction(KeyActionType.Delete);
                    break;
                case SWF.Keys.Control | SWF.Keys.A:
                    _DoKeyAction(KeyActionType.CtrlA);
                    break;
                case SWF.Keys.Control | SWF.Keys.C:
                    _DoKeyAction(KeyActionType.CtrlC);
                    break;
                case SWF.Keys.Control | SWF.Keys.X:
                    // Ctrl+X : pokud je povoleno, provedu; pokud nelze provést Ctrl+X ale lze provést Ctrl+C, tak se provede to:
                    if (EnabledKeyActions.HasFlag(KeyActionType.CtrlX))
                        _DoKeyAction(KeyActionType.CtrlX);
                    else if (EnabledKeyActions.HasFlag(KeyActionType.CtrlC))
                        _DoKeyAction(KeyActionType.CtrlC);
                    break;
                case SWF.Keys.Control | SWF.Keys.V:
                    _DoKeyAction(KeyActionType.CtrlV);
                    break;
                case SWF.Keys.Alt | SWF.Keys.Up:
                    _DoKeyAction(KeyActionType.AltUp);
                    break;
                case SWF.Keys.Alt | SWF.Keys.Down:
                    _DoKeyAction(KeyActionType.AltDown);
                    break;
            }
        }
        /// <summary>
        /// Provede akce zadané jako bity v dané akci (<paramref name="action"/>), s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="force"></param>
        private void _DoKeyAction(KeyActionType action, bool force = false)
        {
            _DoKeyAction(action, KeyActionType.Delete, force, _DoKeyActionDelete);
            _DoKeyAction(action, KeyActionType.CtrlA, force, _DoKeyActionCtrlA);
            _DoKeyAction(action, KeyActionType.CtrlC, force, _DoKeyActionCtrlC);
            _DoKeyAction(action, KeyActionType.CtrlX, force, _DoKeyActionCtrlX);
            _DoKeyAction(action, KeyActionType.CtrlV, force, _DoKeyActionCtrlV);
            _DoKeyAction(action, KeyActionType.AltUp, force, _DoKeyActionAltUp);
            _DoKeyAction(action, KeyActionType.AltDown, force, _DoKeyActionAltDown);
        }
        /// <summary>
        /// Pokud v soupisu akcí <paramref name="action"/> je příznak akce <paramref name="flag"/>, pak provede danou akci <paramref name="runMethod"/>, 
        /// s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="flag"></param>
        /// <param name="force"></param>
        /// <param name="runMethod"></param>
        private void _DoKeyAction(KeyActionType action, KeyActionType flag, bool force, Action runMethod)
        {
            if (!action.HasFlag(flag)) return;
            if (!force && !EnabledKeyActions.HasFlag(flag)) return;
            runMethod();
        }
        /// <summary>
        /// Provedení klávesové akce: Delete
        /// </summary>
        private void _DoKeyActionDelete()
        {
            RemoveIndexes(this.SelectedIndexes);
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlA
        /// </summary>
        private void _DoKeyActionCtrlA()
        {
            this.SelectedItems = this.ListItems;
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlC
        /// </summary>
        private void _DoKeyActionCtrlC()
        {
            var selectedItems = this.SelectedItems;
            string textTxt = selectedItems.ToOneString();
            DxComponent.ClipboardInsert(selectedItems, textTxt);
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlX
        /// </summary>
        private void _DoKeyActionCtrlX()
        {
            _DoKeyActionCtrlC();
            _DoKeyActionDelete();
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlV
        /// </summary>
        private void _DoKeyActionCtrlV()
        {
            if (!DxComponent.ClipboardTryGetApplicationData(out var data)) return;
            if (!(data is IEnumerable<IMenuItem> items)) return;

            var itemList = items.ToList();
            if (itemList.Count == 0) return;

            foreach (var item in itemList)
                this.Items.Add(item);

            this.SelectedItem = itemList[0];
            this.SelectedItems = itemList;
        }
        /// <summary>
        /// Provedení klávesové akce: AltUp
        /// </summary>
        private void _DoKeyActionAltUp()
        {
            var selectedItems = this.SelectedItems;



        }
        /// <summary>
        /// Provedení klávesové akce: AltDown
        /// </summary>
        private void _DoKeyActionAltDown()
        {
            var selectedItems = this.SelectedItems;



        }
        /// <summary>
        /// Z this Listu odebere prvky na daných indexech.
        /// </summary>
        /// <param name="removeIndexes"></param>
        public void RemoveIndexes(IEnumerable<int> removeIndexes)
        {
            if (removeIndexes == null) return;
            int count = this.ItemCount;
            var removeList = removeIndexes
                .CreateDictionary(i => i, true)                      // Odstraním duplicitní hodnoty indexů;
                .Keys.Where(i => (i >= 0 && i < count))              //  z klíčů (indexy) vyberu jen hodnoty, které reálně existují v ListBoxu;
                .ToList();                                           //  a vytvořím List pro další práci:
            removeList.Sort((a, b) => b.CompareTo(a));               // Setřídím indexy sestupně, pro korektní postup odebírání
            removeList.ForEachExec(i => this.Items.RemoveAt(i));     // A v sestupném pořadí indexů odeberu odpovídající prvky
        }
        /// <summary>
        /// Z this Listu odebere všechny dané prvky
        /// </summary>
        /// <param name="removeItems"></param>
        public void RemoveItems(IEnumerable<IMenuItem> removeItems)
        {
            if (removeItems == null) return;
            var removeArray = removeItems.ToArray();
            var listItems = this.ListItems;
            for (int i = this.ItemCount - 1; i >= 0; i--)
            {
                var listItem = listItems[i];
                if (listItem != null && removeArray.Any(t => Object.ReferenceEquals(t, listItem)))
                    this.Items.RemoveAt(i);
            }
        }
        #endregion
        #region Přesouvání prvků pomocí myši
        /// <summary>
        /// Souhrn povolených akcí Drag and Drop
        /// </summary>
        public DxDragDropActionType DragDropActions { get { return _DragDropActions; } set { DxDragDropInit(value); } }
        private DxDragDropActionType _DragDropActions;
        /// <summary>
        /// Vrátí true, pokud je povolena daná akce
        /// </summary>
        /// <param name="action"></param>
        private bool _IsDragDropActionEnabled(DxDragDropActionType action) { return _DragDropActions.HasFlag(action); }
        /// <summary>
        /// Nepoužívejme v aplikačním kódu. 
        /// Místo toho používejme property <see cref="DragDropActions"/>.
        /// </summary>
        public override bool AllowDrop { get { return this._AllowDrop; } set { } }
        /// <summary>
        /// Obsahuje true, pokud this prvek může být cílem Drag and Drop
        /// </summary>
        private bool _AllowDrop
        {
            get
            {
                var actions = this._DragDropActions;
                return (actions.HasFlag(DxDragDropActionType.ReorderItems) || actions.HasFlag(DxDragDropActionType.ImportItemsInto));
            }
        }
        /// <summary>
        /// Inicializace controlleru Drag and Drop
        /// </summary>
        /// <param name="actions"></param>
        private void DxDragDropInit(DxDragDropActionType actions)
        {
            if (actions != DxDragDropActionType.None && _DxDragDrop == null)
                _DxDragDrop = new DxDragDrop(this);
            _DragDropActions = actions;
        }
        /// <summary>
        /// Dispose controlleru Drag and Drop
        /// </summary>
        private void DxDragDropDispose()
        {
            if (_DxDragDrop != null)
                _DxDragDrop.Dispose();
            _DxDragDrop = null;
        }
        /// <summary>
        /// Controller pro aktivitu Drag and Drop, vycházející z this objektu
        /// </summary>
        private DxDragDrop _DxDragDrop;
        /// <summary>
        /// Controller pro DxDragDrop v this controlu
        /// </summary>
        DxDragDrop IDxDragDropControl.DxDragDrop { get { return _DxDragDrop; } }
        /// <summary>
        /// Metoda volaná do objektu Source (zdroj Drag and Drop) při každé akci na straně zdroje.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void IDxDragDropControl.DoDragSource(DxDragDropArgs args)
        {
            switch (args.Event)
            {
                case DxDragDropEventType.DragStart:
                    DoDragSourceStart(args);
                    break;
                case DxDragDropEventType.DragDropAccept:
                    DoDragSourceDrop(args);
                    break;
            }
            return;
        }
        /// <summary>
        /// Metoda volaná do objektu Target (cíl Drag and Drop) při každé akci, pokud se myš nachází nad objektem který implementuje <see cref="IDxDragDropControl"/>.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void IDxDragDropControl.DoDragTarget(DxDragDropArgs args)
        {
            switch (args.Event)
            {
                case DxDragDropEventType.DragMove:
                    DoDragTargetMove(args);
                    break;
                case DxDragDropEventType.DragLeaveOfTarget:
                    DoDragTargetLeave(args);
                    break;
                case DxDragDropEventType.DragDropAccept:
                    DoDragTargetDrop(args);
                    break;
                case DxDragDropEventType.DragEnd:
                    DoDragTargetEnd(args);
                    break;
            }
        }
        /// <summary>
        /// Když začíná proces Drag, a this objekt je zdrojem
        /// </summary>
        /// <param name="args"></param>
        private void DoDragSourceStart(DxDragDropArgs args)
        {
            var selectedItems = this.SelectedItemsInfo;
            if (selectedItems.Length == 0)
            {
                args.SourceDragEnabled = false;
            }
            else
            {
                args.SourceText = selectedItems.ToOneString(convertor: i => i.Item2.ToString());
                args.SourceObject = selectedItems;
                args.SourceDragEnabled = true;
            }
        }
        /// <summary>
        /// Když probíhá proces Drag, a this objekt je možným cílem.
        /// Objekt this může být současně i zdrojem akce (pokud probíhá Drag and Drop nad týmž objektem), pak jde o Reorder.
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetMove(DxDragDropArgs args)
        {
            Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
            IndexRatio index = DoDragSearchIndexRatio(targetPoint);
            if (!IndexRatio.IsEqual(index, MouseDragTargetIndex))
            {
                MouseDragTargetIndex = index;
                this.Invalidate();
            }
            args.CurrentEffect = args.SuggestedDragDropEffect;
        }
        /// <summary>
        /// Když úspěšně končí proces Drag, a this objekt je zdrojem
        /// </summary>
        /// <param name="args"></param>
        private void DoDragSourceDrop(DxDragDropArgs args)
        {
            args.TargetIndex = null;
            args.InsertIndex = null;
            var selectedItemsInfo = args.SourceObject as Tuple<int, IMenuItem, Rectangle?>[];
            if (selectedItemsInfo != null && (args.TargetIsSource || args.CurrentEffect == SWF.DragDropEffects.Move))
            {
                // Pokud provádíme přesun v rámci jednoho Listu (tj. Target == Source),
                //  pak si musíme najít správný TargetIndex nyní = uživatel chce přemístit prvky před/za určitý prvek, a jeho index se odebráním prvků změní:
                if (args.TargetIsSource)
                {
                    Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
                    args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
                    args.InsertIndex = args.TargetIndex.GetInsertIndex(selectedItemsInfo.Select(t => t.Item1));
                }
                // Odebereme zdrojové prvky:
                this.RemoveIndexes(selectedItemsInfo.Select(t => t.Item1));
            }
        }
        /// <summary>
        /// Když úspěšně končí proces Drag, a this objekt je možným cílem
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetDrop(DxDragDropArgs args)
        {
            if (args.TargetIndex == null)
            {
                Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
                args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
                args.InsertIndex = null;
            }
            if (!args.InsertIndex.HasValue)
                args.InsertIndex = args.TargetIndex.GetInsertIndex();

            if (DxDragTargetTryGetItems(args, out var items))
            {
                List<int> selectedIndexes = new List<int>();
                if (args.InsertIndex.HasValue && args.InsertIndex.Value >= 0 && args.InsertIndex.Value < this.ItemCount)
                {
                    int insertIndex = args.InsertIndex.Value;
                    foreach (var item in items)
                    {
                        DevExpress.XtraEditors.Controls.ImageListBoxItem imgItem = new DevExpress.XtraEditors.Controls.ImageListBoxItem(item);
                        selectedIndexes.Add(insertIndex);
                        this.Items.Insert(insertIndex++, imgItem);
                    }
                }
                else
                {
                    int addIndex = this.ItemCount;
                    foreach (var item in items)
                        selectedIndexes.Add(addIndex++);
                    this.Items.AddRange(items);
                }
                this.SelectedIndexes = selectedIndexes;
            }
            
            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private bool DxDragTargetTryGetItems(DxDragDropArgs args, out IMenuItem[] items)
        {
            items = null;
            if (args.SourceObject is IEnumerable<IMenuItem> menuItems) { items = menuItems.ToArray(); return true; }
            if (args.SourceObject is IEnumerable<Tuple<int, IMenuItem, Rectangle?>> listItemsInfo) { items = listItemsInfo.Select(i => i.Item2).ToArray(); return true; }
            return false;
        }
        /// <summary>
        /// Když probíhá proces Drag, ale opouští this objekt, který dosud byl možným cílem (probíhala pro něj metoda <see cref="DoDragTargetMove(DxDragDropArgs)"/>)
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetLeave(DxDragDropArgs args)
        {
            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        private void DoDragTargetEnd(DxDragDropArgs args)
        {
            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// Metoda vrátí data o prvku pod myší nebo poblíž myši, který je aktivním cílem procesu Drag, pro myš na daném bodě
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        private IndexRatio DoDragSearchIndexRatio(Point targetPoint)
        {
            return IndexRatio.Create(targetPoint, this.ClientRectangle, p => this.IndexFromPoint(p), i => GetItemBounds(i, false), this.ItemCount, SWF.Orientation.Vertical);
        }
        /// <summary>
        /// Informace o prvku, nad kterým je myš, pro umístění obsahu v procesu Drag and Drop.
        /// Pokud je null, pak pro this prvek neprobíhá Drag and Drop.
        /// <para/>
        /// Tuto hodnotu vykresluje metoda <see cref="MouseDragPaint(SWF.PaintEventArgs)"/>.
        /// </summary>
        private IndexRatio MouseDragTargetIndex;
        /// <summary>
        /// Obsahuje true, pokud v procesu Paint má být volána metoda <see cref="MouseDragPaint(SWF.PaintEventArgs)"/>.
        /// </summary>
        private bool MouseDragNeedRePaint { get { return (MouseDragTargetIndex != null); } }
        /// <summary>
        /// Volá se proto, aby this prvek mohl vykreslit Target pozici pro MouseDrag proces
        /// </summary>
        /// <param name="e"></param>
        private void MouseDragPaint(SWF.PaintEventArgs e)
        {
            if (!MouseDragNeedRePaint) return;
            var bounds = MouseDragTargetIndex.GetMarkLineBounds();
            if (!bounds.HasValue) return;
            var color = this.ForeColor;
            using (var brush = new SolidBrush(color))
                e.Graphics.FillRectangle(brush, bounds.Value);
        }
        /// <summary>
        /// Index prvku, nad kterým se pohybuje myš
        /// </summary>
        public int OnMouseItemIndex
        {
            get
            {
                if (_OnMouseItemIndex >= this.ItemCount)
                    _OnMouseItemIndex = -1;
                return _OnMouseItemIndex;
            }
            protected set
            {
                if (value != _OnMouseItemIndex)
                {
                    _OnMouseItemIndex = value;
                    this.Invalidate();
                }
            }
        }
        private int _OnMouseItemIndex = -1;
        /// <summary>
        /// Vrátí souřadnice prvku na daném indexu. Volitelně může provést kontrolu na to, zda daný prvek je ve viditelné oblasti Listu. Pokud prvek neexistuje nebo není vidět, vrací null.
        /// Vrácené souřadnice jsou relativní v prostoru this ListBoxu.
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        public Rectangle? GetItemBounds(int itemIndex, bool onlyVisible = true)
        {
            if (itemIndex < 0 || itemIndex >= this.ItemCount) return null;

            Rectangle itemBounds = this.GetItemRectangle(itemIndex);
            if (onlyVisible)
            {   // Pokud chceme souřadnice pouze viditelného prvku, pak prověříme souřadnice prvku proti souřadnici prostoru v ListBoxu:
                Rectangle listBounds = this.ClientRectangle;
                if (itemBounds.Right <= listBounds.X || itemBounds.X >= listBounds.Right || itemBounds.Bottom <= listBounds.Y || itemBounds.Y >= listBounds.Bottom)
                    return null;   // Prvek není vidět
            }

            return itemBounds;
        }
        /// <summary>
        /// Vrátí souřadnici prostoru pro myší ikonu
        /// </summary>
        /// <param name="onMouseItemIndex"></param>
        /// <returns></returns>
        protected Rectangle? GetOnMouseIconBounds(int onMouseItemIndex)
        {
            Rectangle? itemBounds = this.GetItemBounds(onMouseItemIndex, true);
            if (!itemBounds.HasValue || itemBounds.Value.Width < 35) return null;        // Pokud prvek neexistuje, nebo není vidět, nebo je příliš úzký => vrátíme null

            int wb = 14;
            int x0 = itemBounds.Value.Right - wb - 6;
            int yc = itemBounds.Value.Y + itemBounds.Value.Height / 2;
            Rectangle iconBounds = new Rectangle(x0 - 1, itemBounds.Value.Y, wb + 1, itemBounds.Value.Height);
            return iconBounds;
        }
        #endregion
        #region Public eventy

        /// <summary>
        /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        public event SWF.PaintEventHandler PaintList;

        #endregion
    }
    #endregion
    #region DxSimpleButton
    /// <summary>
    /// SimpleButton
    /// </summary>
    public class DxSimpleButton : DevExpress.XtraEditors.SimpleButton
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region HasMouse
        /// <summary>
        /// Panel má na sobě myš?
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
        #region Resize a SvgImage

        #endregion
    }
    #endregion
    #region DxDropDownButton
    /// <summary>
    /// DxDropDownButton
    /// </summary>
    public class DxDropDownButton : DevExpress.XtraEditors.DropDownButton
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxCheckButton
    /// <summary>
    /// CheckButton
    /// </summary>
    public class DxCheckButton : DevExpress.XtraEditors.CheckButton
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
        #region Resize a SvgImage

        #endregion
    }
    #endregion
    #region DxSuperToolTip
    /// <summary>
    /// SuperToolTip s přímým přístupem do standardních textů v ToolTipu
    /// </summary>
    public class DxSuperToolTip : DevExpress.Utils.SuperToolTip
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSuperToolTip() 
            : base()
        {
            _TitleItem = this.Items.AddTitle("");
            _TitleItem.ImageOptions.Images = ComponentConnector.GraphicsCache.GetImageList(WinFormServices.Drawing.UserGraphicsSize.Large);
            _TitleItem.ImageOptions.ImageToTextDistance = 12;

            _SeparatorItem = this.Items.AddSeparator();

            _TextItem = this.Items.Add("");

            AcceptTitleOnlyAsValid = false;
        }
        private ToolTipTitleItem _TitleItem;
        private ToolTipSeparatorItem _SeparatorItem;
        private ToolTipItem _TextItem;
        /// <summary>
        /// Řídí určení hodnoty <see cref="IsValid"/> (= ToolTip je platný) :
        /// <para/>
        /// a) Pokud <see cref="AcceptTitleOnlyAsValid"/> = true: pak <see cref="IsValid"/> je true tehdy, když je vyplněn i jen titulek.
        /// Tedy, když je zadán <see cref="Title"/> a nemusí být zadán <see cref="Text"/> = tehdy se zobrazuje ToolTip obsahující pouze titulek
        /// <para/>
        /// b) Pokud <see cref="AcceptTitleOnlyAsValid"/> = false: pak <see cref="IsValid"/> je true jen tehdy, když je vyplněn text (bez ohledu na vyplnění titulku).
        /// Tedy, když je zadán <see cref="Text"/> (a může i nemusí být zadán <see cref="Title"/>) = tedy zobrazuje se ToolTip obsahující [titulek když je], a obsahuje Text.
        /// <para/>
        /// Výchozí je false = pro platný ToolTip je třeba zadat jeho <see cref="Text"/>, nestačí zadat pouze <see cref="Title"/>.
        /// </summary>
        public bool AcceptTitleOnlyAsValid { get; set; }
        /// <summary>
        /// Text titulku
        /// </summary>
        public string Title { get { return _TitleItem.Text; } set { _TitleItem.Text = value; } }
        /// <summary>
        /// Titulek může obsahovat HTML kódy
        /// </summary>
        public bool? TitleContainsHtml { get { return DxComponent.Convert(_TitleItem.AllowHtmlText); } set { _TitleItem.AllowHtmlText = DxComponent.Convert(value); } }
        /// <summary>
        /// Jméno ikony
        /// </summary>
        public string IconName { get { return _IconName; } set { _SetIconName(value); } }
        private void _SetIconName(string iconName)
        {
            _IconName = iconName;
            _TitleItem.ImageOptions.ImageIndex = ((!String.IsNullOrEmpty(iconName)) ? ComponentConnector.GraphicsCache.GetResourceIndex(iconName, WinFormServices.Drawing.UserGraphicsSize.Large) : -1);
        }
        private string _IconName;
        /// <summary>
        /// Text tooltipu
        /// </summary>
        public string Text { get { return _TextItem.Text; } set { _TextItem.Text = value; } }
        /// <summary>
        /// Text tooltipu může obsahovat HTML kódy
        /// </summary>
        public bool? TextContainsHtml { get { return DxComponent.Convert(_TextItem.AllowHtmlText); } set { _TextItem.AllowHtmlText = DxComponent.Convert(value); } }
        /// <summary>
        /// Obsahuje true v případě, že ToolTip má text alespoň v titulku <see cref="Title"/> anebo v textu <see cref="Text"/>, pak má význam aby byl zobrazen.
        /// Pokud texty nemá, neměl by být zobrazován.
        /// </summary>
        public bool IsValid
        {
            get
            {
                bool acceptTitleOnlyAsValid = this.AcceptTitleOnlyAsValid;
                bool hasTitle = !String.IsNullOrEmpty(this.Title);
                bool hasText = !String.IsNullOrEmpty(this.Text);
                return ((acceptTitleOnlyAsValid && hasTitle) || hasText);
            }
        }
        /// <summary>
        /// Nuluje svůj obsah
        /// </summary>
        public void ClearValues()
        {
            Title = "";
            TitleContainsHtml = null;
            IconName = null;
            Text = "";
            TextContainsHtml = null;
        }
        /// <summary>
        /// Naplní do sebe hodnoty z dané definice
        /// </summary>
        /// <param name="toolTipItem"></param>
        public void LoadValues(IToolTipItem toolTipItem)
        {
            if (toolTipItem == null)
                ClearValues();
            else
                LoadValues(toolTipItem.ToolTipTitle, toolTipItem.ToolTipText, toolTipIcon: toolTipItem.ToolTipIcon);
        }
        /// <summary>
        /// Naplní do sebe hodnoty z dané definice
        /// </summary>
        /// <param name="textItem"></param>
        public void LoadValues(ITextItem textItem)
        {
            if (textItem == null)
                ClearValues();
            else
                LoadValues(textItem.ToolTipTitle, textItem.ToolTipText, textItem.Text, textItem.ToolTipIcon);
        }
        /// <summary>
        /// Naplní do sebe hodnoty z daných parametrů
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="defaultTitle"></param>
        /// <param name="toolTipIcon"></param>
        public void LoadValues(string title, string text, string defaultTitle = null, string toolTipIcon = null)
        {
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(text) && String.IsNullOrEmpty(defaultTitle))
                ClearValues();
            else
            {
                Title = (title ?? defaultTitle);
                TitleContainsHtml = null;
                IconName = toolTipIcon;
                Text = text;
                TextContainsHtml = null;
            }
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="toolTipItem"></param>
        /// <returns></returns>
        public static SuperToolTip CreateDxSuperTip(IToolTipItem toolTipItem)
        {
            if (toolTipItem == null) return null;
            return CreateDxSuperTip(toolTipItem.ToolTipTitle, toolTipItem.ToolTipText, toolTipIcon: toolTipItem.ToolTipIcon);
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="textItem"></param>
        /// <returns></returns>
        public static SuperToolTip CreateDxSuperTip(ITextItem textItem)
        {
            if (textItem == null) return null;
            return CreateDxSuperTip(textItem.ToolTipTitle, textItem.ToolTipText, textItem.Text, textItem.ToolTipIcon);
        }
        /// <summary>
        /// Vytvoří a vrátí standardní SuperToolTip pro daný titulek a text.
        /// Pokud nebude zadán text <paramref name="text"/>, ani titulek (<paramref name="title"/> nebo <paramref name="defaultTitle"/>), pak vrátí null.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="defaultTitle"></param>
        /// <param name="toolTipIcon"></param>
        /// <returns></returns>
        public static DxSuperToolTip CreateDxSuperTip(string title, string text, string defaultTitle = null, string toolTipIcon = null)
        {
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(text) && String.IsNullOrEmpty(defaultTitle)) return null;

            var superTip = new DxSuperToolTip()
            {
                Title = (title ?? defaultTitle),
                IconName = toolTipIcon,
                Text = text
            };

            return superTip;
        }
    }
    #endregion
    #region DxStatus
    /// <summary>
    /// StatusBar : Statický prvek = Label
    /// </summary>
    public class DxBarStaticItem : DevExpress.XtraBars.BarStaticItem
    { }
    /// <summary>
    /// StatusBar : Button
    /// </summary>
    public class DxBarButtonItem : DevExpress.XtraBars.BarButtonItem
    {
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="defaultTitle"></param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    /// <summary>
    /// StatusBar : CheckItem
    /// </summary>
    public class DxBarCheckItem : DevExpress.XtraBars.BarCheckItem
    {
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="defaultTitle"></param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    #endregion
    #region DxImagePickerListBox
    /// <summary>
    /// ListBox nabízející DevExpress Resources
    /// </summary>
    public class DxImagePickerListBox : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxImagePickerListBox()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Initialize()
        {
            _ClipboardCopyIndex = 0;

            _FilterClearButton = DxComponent.CreateDxMiniButton(0, 0, 20, 20, this, _FilterClearButtonClick,
                resourceName: "svgimages/spreadsheet/clearfilter.svg",
                toolTipTitle: "Zrušit filtr", toolTipText: "Zruší filtr, budou zobrazeny všechny dostupné zdroje.");
            _FilterClearButton.MouseEnter += _AnyControlEnter;
            _FilterClearButton.Enter += _AnyControlEnter;

            _FilterText = DxComponent.CreateDxTextEdit(27, 0, 200, this,
                toolTipTitle: "Filtr Resources", toolTipText: "Vepište část názvu zdroje.\r\nLze použít filtrační znaky * a ?.\r\nLze zadat víc filtrů, oddělených středníkem nebo čárkou.\r\n\r\nNapříklad: 'add' zobrazí všechny položky obsahující 'add',\r\n'*close*.svg' zobrazí něco obsahující 'close' s příponou '.svg',\r\n'*close*.svg;*delete*' zobrazí prvky close nebo delete");
            _FilterText.MouseEnter += _AnyControlEnter;
            _FilterText.Enter += _AnyControlEnter;
            _FilterText.KeyUp += _FilterText_KeyUp;

            _ListCopyButton = DxComponent.CreateDxMiniButton(230, 0, 20, 20, this, _ListCopyButtonClick,
                resourceName: "svgimages/xaf/action_copy.svg", hotResourceName: "svgimages/xaf/action_modeldifferences_copy.svg",
                toolTipTitle: "Zkopírovat", toolTipText: "Označené řádky v seznamu zdrojů vloží do schránky, jako Ctrl+C.");
            _ListCopyButton.MouseEnter += _AnyControlEnter;
            _ListCopyButton.Enter += _AnyControlEnter;

            _ListBox = DxComponent.CreateDxListBox(SWF.DockStyle.None, parent: this, selectionMode: SWF.SelectionMode.MultiExtended, itemHeight: 32,
                toolTipTitle: "Seznam Resources", toolTipText: "Označte jeden nebo více řádků, klávesou Ctrl+C zkopírujete názvy Resources jako kód C#.");
            _ListBox.MouseEnter += _AnyControlEnter;
            _ListBox.Enter += _AnyControlEnter;
            _ListBox.KeyDown += _ListBox_KeyDown;
            _ListBox.PaintList += _ListBox_PaintList;
            _ListBox.SelectedIndexChanged += _ListBox_SelectedIndexChanged;

            _ResourceNames = DxComponent.GetResourceKeys();
            _ResourceFilter = "";
            _StatusText = "";

            _FillListByFilter();
        }
        /// <summary>
        /// Po vstupu do jakéhokoli controlu nastavím výchozí status text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnyControlEnter(object sender, EventArgs e)
        {
            _ResetStatusText();
        }
        /// <summary>
        /// Po změně velikosti provedu rozmístění prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Správně rozmístí prvky
        /// </summary>
        protected void DoLayout()
        {
            var size = this.ClientSize;
            int mx = 3;
            int my = 3;
            int x = mx;
            int y = my;
            int sx = 2;
            int sy = 2;
            int r = size.Width - mx;
            int w = size.Width - mx - mx;
            int h = size.Height - my - my;
            int txs = _FilterText.Height;
            int bts = txs + sx;

            _FilterClearButton.Bounds = new Rectangle(x, y, txs, txs);
            _FilterText.Bounds = new Rectangle(x + bts, y, w - bts - bts, txs);
            _ListCopyButton.Bounds = new Rectangle(r - txs, y, txs, txs);

            y = _FilterText.Bounds.Bottom + sy;
            _ListBox.Bounds = new Rectangle(x, y, w, h - y);
        }
        /// <summary>
        /// Clear filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterClearButtonClick(object sender, EventArgs e)
        {
            _ResourceFilter = "";
            _FillListByFilter();
            _FilterText.Focus();
        }
        /// <summary>
        /// FilterText: Po klávese ve filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterText_KeyUp(object sender, SWF.KeyEventArgs e)
        {
            if (e.KeyCode == SWF.Keys.Down || e.KeyCode == SWF.Keys.Enter)
                _ListBox.Focus();
            else if (e.KeyCode == SWF.Keys.Home || e.KeyCode == SWF.Keys.End || e.KeyCode == SWF.Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == SWF.Keys.Left || e.KeyCode == SWF.Keys.Right || e.KeyCode == SWF.Keys.PageUp || e.KeyCode == SWF.Keys.PageDown || e.KeyCode == SWF.Keys.Tab || e.KeyCode == SWF.Keys.Escape)
            { }
            else if (e.Modifiers == SWF.Keys.Control)
            { }
            else
                _FillListByFilter();
        }
        /// <summary>
        /// Copy selected items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListCopyButtonClick(object sender, EventArgs e)
        {
            _FilterText.Focus();
            _DoCopyClipboard();
        }
        /// <summary>
        /// ListBox: Obsluha kláves: detekce Ctrl+C
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (SWF.Keys.Control | SWF.Keys.C)) _DoCopyClipboard();
        }
        /// <summary>
        /// Po změně řádku v ListBoxu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItems = _ListBox.SelectedItemsInfo;
            StatusText = "Označeny řádky: " + selectedItems.Length.ToString();
        }
        /// <summary>
        /// Vykreslí obrázky do ListBoxu do viditelných prvků
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_PaintList(object sender, SWF.PaintEventArgs e)
        {
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = DxComponent.GetSvgPalette();
            var visibleItems = _ListBox.VisibleItems;
            foreach (var visibleItem in visibleItems)
            {
                string resourceName = visibleItem.Item2?.Text;
                Rectangle itemBounds = visibleItem.Item3;
                var image = DxComponent.GetImageFromResource(resourceName, out Size size, maxSize: new Size(32, 32), optimalSvgSize: new Size(32, 32), svgPalette: svgPalette);
                if (image != null)
                {
                    Point imagePoint = new Point((itemBounds.Right - 24 - size.Width / 2), itemBounds.Top + ((itemBounds.Height - size.Height) / 2));
                    Rectangle imageBounds = new Rectangle(imagePoint, size);
                    e.Graphics.DrawImage(image, imageBounds);
                }
            }
        }
        DxSimpleButton _FilterClearButton;
        DxTextEdit _FilterText;
        DxSimpleButton _ListCopyButton;
        DxListBoxControl _ListBox;
        #region Seznam resources - získání, filtrování, tvorba Image, CopyToClipboard
        /// <summary>
        /// Do seznamu ListBox vloží zdroje odpovídající aktuálnímu filtru
        /// </summary>
        private void _FillListByFilter()
        {
            string[] resources = _GetResourcesByFilter();
            IMenuItem[] items = resources.Select(s => new DataMenuItem() { Text = s }).ToArray();
            _FilteredItemsCount = resources.Length;

            _ListBox.SuspendLayout();
            _ListBox.Items.Clear();
            _ListBox.Items.AddRange(items);
            _ListBox.ResumeLayout(false);
            _ListBox.PerformLayout();

            _ResetStatusText();
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících aktuálnímu filtru
        /// </summary>
        /// <returns></returns>
        private string[] _GetResourcesByFilter()
        {
            if (_ResourceNames == null || _ResourceNames.Length == 0) return new string[0];

            var filter = _ResourceFilter;
            if (String.IsNullOrEmpty(filter)) return _ResourceNames;

            filter = RegexSupport.ReplaceGreenWildcards(filter);

            string[] result = _GetResourcesByFilter(filter);
            if (result.Length == 0)
                result = _GetResourcesByFilter("*" + filter + "*");

            return result;
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících danému filtru
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string[] _GetResourcesByFilter(string filter)
        {
            var regexes = RegexSupport.CreateWildcardsRegexes(filter);
            var result = RegexSupport.FilterByRegexes(_ResourceNames, regexes);
            return result.ToArray();
        }
        /// <summary>
        /// Vloží do Clipboardu kód obsahující aktuálně vybrané texty
        /// </summary>
        private void _DoCopyClipboard()
        {
            var selectedItems = _ListBox.SelectedItemsInfo;
            int rowCount = selectedItems.Length;
            if (rowCount > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var selectedItem in selectedItems)
                {
                    _ClipboardCopyIndex++;
                    string resourceName = selectedItem.Item2?.Text;
                    if (!String.IsNullOrEmpty(resourceName))
                        sb.AppendLine($"  string resource{_ClipboardCopyIndex} = \"{resourceName}\";");
                }
                if (sb.Length > 0)
                {
                    DxComponent.ClipboardInsert(sb.ToString());
                }

                StatusText = "Položky zkopírovány do schránky: " + rowCount.ToString();
            }
            else
            {
                StatusText = $"Nejsou označeny žádné položky.";
            }
        }
        /// <summary>
        /// Filtrační text z textboxu
        /// </summary>
        private string _ResourceFilter { get { return this._FilterText.Text.Trim(); } set { this._FilterText.Text = (value ?? ""); } }
        /// <summary>
        /// Jména všech zdrojů
        /// </summary>
        private string[] _ResourceNames;
        /// <summary>
        /// Číslo pro číslování proměnných do Clipboardu
        /// </summary>
        private int _ClipboardCopyIndex;
        /// <summary>
        /// Počet aktuálně filtrovaných položek
        /// </summary>
        private int _FilteredItemsCount;
        #endregion
        #region Podpora pro StatusBar
        /// <summary>
        /// Text vhodný pro zobrazení ve statusbaru. Setování nové hodnoty vyvolá event <see cref="StatusTextChanged"/>
        /// </summary>
        public string StatusText
        {
            get { return _StatusText; }
            protected set
            {
                bool isChanged = !String.Equals(value, _StatusText);
                _StatusText = value;
                if (isChanged)
                    StatusTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _StatusText;
        private void _ResetStatusText()
        {
            int allItemsCount = _ResourceNames.Length;
            int filteredItemsCount = _FilteredItemsCount;
            bool hasFilter = !String.IsNullOrEmpty(_ResourceFilter);
            string filter = (hasFilter ? "'" + _ResourceFilter + "'" : "");
            if (allItemsCount == 0)
                StatusText = $"Neexistují žádné položky";
            else if (!hasFilter)
                StatusText = $"Zobrazeny všechny položky: {allItemsCount}";
            else if (filteredItemsCount == 0)
                StatusText = $"Zadanému filtru {filter} nevyhovuje žádná položka";
            else if (filteredItemsCount == allItemsCount)
                StatusText = $"Zadanému filtru {filter} vyhovují všechny položky: {filteredItemsCount}";
            else
                StatusText = $"Zadanému filtru {filter} vyhovují zobrazené položky: {filteredItemsCount}";
        }
        /// <summary>
        /// Událost vyvolaná po změně textu v <see cref="StatusText"/>
        /// </summary>
        public event EventHandler StatusTextChanged;
        #endregion
    }
    #endregion
    #region DxSimpleSplitter
    /// <summary>
    /// Jednoduchý splitter
    /// </summary>
    public class DxSimpleSplitter : Control
    {
        #region Konstruktor, privátní eventhandlery
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSimpleSplitter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            _CursorOrientation = null;
            _Orientation = Orientation.Horizontal;
            _VisualLogoMode = LogoMode.Allways;                      // Viditelnost grafiky = vždy
            _VisualLogoDotsCount = 4;
            SetCursor();
            base.BackColor = Color.Transparent;
            SplitterColor = SystemColors.ControlDark;
            _SplitterActiveColor = Color.Yellow;
            _SplitterColorByParent = true;
            _SplitThick = 6;                                         // Opsáno z MS Outlook
            _SplitterEnabled = true;
            _AcceptBoundsToSplitter = false;
            _CurrentMouseState = MouseState.None;                    // Výchozí stav
            DevExpressSkinEnabled = true;                            // Tady se z aktuálního skinu přečtou barvy a uloží do barev _SkinBackColor a _SkinActiveColor
            Enabled = true;
            Initialized = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Splitter: {Name}; Orientation: {Orientation}, SplitPosition: {SplitPosition}";
        }
        /// <summary>
        /// Hodnota true povoluje práci v instanci.
        /// Obsahuje true po dokončení konstruktoru.
        /// Na začátku Dispose se shodí na false.
        /// </summary>
        public bool Initialized { get; protected set; } = false;
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Initialized = false;
            DevExpressSkinEnabled = false;
            base.Dispose(disposing);
        }
        #endregion
        #region Vzhled, kreslení, aktuální barvy, kreslící Brush, kurzor
        /// <summary>
        /// Refresh. 
        /// Je vhodné zavolat po změně souřadnic navázaných controlů, pak si Splitter podle nich určí svoji velikost.
        /// </summary>
        public override void Refresh()
        {
            RecalculateBounds();
            base.Refresh();
        }
        /// <summary>
        /// Po změně Enabled
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }
        /// <summary>
        /// Po změně barvy pozadí parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            if (this.SplitterColorByParent) this.Invalidate();
        }
        /// <summary>
        /// Zajistí znovuvykreslení prvku
        /// </summary>
        protected virtual void PaintSplitter()
        {
            if (!Initialized) return;
            PaintEventArgs e = new PaintEventArgs(this.CreateGraphics(), this.ClientRectangle);
            PaintSplitter(e);
        }
        /// <summary>
        /// Provede kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Initialized) return;
            PaintSplitter(e);
        }
        /// <summary>
        /// Vykreslí Splitter
        /// </summary>
        /// <param name="e"></param>
        protected void PaintSplitter(PaintEventArgs e)
        {
            if (!Initialized) return;
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    PaintHorizontal(e);
                    break;
                case Orientation.Vertical:
                    PaintVertical(e);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí Splitter v orientaci Horizontal
        /// </summary>
        /// <param name="e"></param>
        private void PaintHorizontal(PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int size = bounds.Height;
            if (size <= 2)
            {   // Tenký splitter do 2px:
                e.Graphics.FillRectangle(CurrentBrush, bounds);
                return;
            }
            Rectangle brushBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height + 1);
            Color color = CurrentColor;
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, GetCurrentColor3DBegin(color), GetCurrentColor3DEnd(color), 90f))
            {
                e.Graphics.FillRectangle(lgb, bounds);
            }

            if (size > 4 && CurrentShowDots && VisualLogoDotsCount > 0)
            {
                int numbers = VisualLogoDotsCount;
                int space = (int)Math.Round(((double)SplitThick * 0.4d), 0);
                if (space < 4) space = 4;
                Point center = bounds.Center();
                int t = center.Y - 1;
                int d = center.X - ((space * numbers / 2) - 1);
                var dotBrush = CurrentDotBrush;
                for (int q = 0; q < numbers; q++)
                    e.Graphics.FillRectangle(dotBrush, new Rectangle(d + space * q, t, 2, 2));
            }
        }
        /// <summary>
        /// Vykreslí Splitter v orientaci Vertical
        /// </summary>
        /// <param name="e"></param>
        private void PaintVertical(PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int size = bounds.Width;
            if (size <= 2)
            {   // Tenký splitter do 2px:
                e.Graphics.FillRectangle(CurrentBrush, bounds);
                return;
            }
            Rectangle brushBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height);
            Color color = CurrentColor;
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, GetCurrentColor3DBegin(color), GetCurrentColor3DEnd(color), 0f))
            {
                e.Graphics.FillRectangle(lgb, bounds);
            }
            if (size > 4 && CurrentShowDots && VisualLogoDotsCount > 0)
            {
                int numbers = VisualLogoDotsCount;
                int space = (int)Math.Round(((double)SplitThick * 0.4d), 0);
                if (space < 4) space = 4;
                Point center = bounds.Center();
                int t = center.X - 1;
                int d = center.Y - ((space * numbers / 2) - 1);
                var dotBrush = CurrentDotBrush;
                for (int q = 0; q < numbers; q++)
                    e.Graphics.FillRectangle(dotBrush, new Rectangle(t, d + space * q, 2, 2));
            }
        }
        /// <summary>
        /// Aktuální barva, reaguje na hodnotu <see cref="SplitterColorByParent"/> a na Parenta,
        /// na stav splitteru <see cref="CurrentSplitterState"/> a na zvolené barvy LineColor*
        /// </summary>
        protected Color CurrentColor { get { return GetCurrentColorFrom(this.CurrentColorBase); } }
        /// <summary>
        /// Aktuální základní barva: reaguje na <see cref="SplitterColorByParent"/>, <see cref="DevExpressSkinEnabled"/> 
        /// a případně vrací <see cref="_SplitterColor"/>
        /// </summary>
        protected Color CurrentColorBase
        {
            get
            {
                if (DevExpressSkinEnabled && _DevExpressSkinBackColor.HasValue)
                    // Dle skinu:
                    return GetCurrentColorFrom(_DevExpressSkinBackColor.Value);

                if (this.SplitterColorByParent && this.Parent != null)
                    // Dle parenta:
                    return GetCurrentColorFrom(this.Parent.BackColor);

                return _SplitterColor;
            }
        }
        /// <summary>
        /// Aktuální barva pro aktivní splitter: reaguje na <see cref="DevExpressSkinEnabled"/> 
        /// a případně vrací <see cref="_SplitterActiveColor"/>
        /// </summary>
        protected Color CurrentColorActive
        {
            get
            {
                if (DevExpressSkinEnabled && _DevExpressSkinActiveColor.HasValue)
                    // Dle skinu:
                    return _DevExpressSkinActiveColor.Value;

                return _SplitterActiveColor;
            }
        }
        /// <summary>
        /// Vrací danou barvu modifikovanou dle aktuálního stavu
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetCurrentColorFrom(Color color)
        {
            color = Color.FromArgb(255, color);
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return GetColorDisable(color);
                case SplitterState.Enabled: return GetColorEnabled(color);
                case SplitterState.Hot: return GetColorActive(color);
                case SplitterState.Down: return GetColorDrag(color);
                case SplitterState.Drag: return GetColorDrag(color);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro 3D zobrazení na straně počátku (Top/Left)
        /// </summary>
        protected Color GetCurrentColor3DBegin(Color color)
        {
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return color.Morph(Color.LightGray, 0.25f);
                case SplitterState.Enabled: return color.Morph(Color.White, 0.15f);
                case SplitterState.Hot: return color.Morph(Color.White, 0.25f);
                case SplitterState.Down: return color.Morph(Color.Black, 0.15f);
                case SplitterState.Drag: return color.Morph(Color.Black, 0.15f);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro 3D zobrazení na straně konce (Bottom/Right)
        /// </summary>
        protected Color GetCurrentColor3DEnd(Color color)
        {
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return color.Morph(Color.LightGray, 0.25f);
                case SplitterState.Enabled: return color.Morph(Color.Black, 0.15f);
                case SplitterState.Hot: return color.Morph(Color.Black, 0.25f);
                case SplitterState.Down: return color.Morph(Color.White, 0.15f);
                case SplitterState.Drag: return color.Morph(Color.White, 0.15f);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro zobrazení grafiky (čtyřtečka)
        /// </summary>
        protected Color CurrentDotColor { get { return CurrentColor.Contrast(64); } }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorDisable(Color color) { return color.GrayScale(0.75f); }
        /// <summary>
        /// Vrátí barvu Enabled k barvě dané.
        /// Záleží na <see cref="SplitThick"/>: pokud je 2 (a menší), pak vrací danou barvu lehce kontrastní, aby byl splitter vidět.
        /// Pokud je 3 a více, pak vrací danou barvu beze změn, protože se bude vykreslovat 3D efektem.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorEnabled(Color color) { return (this.SplitThick <= 2 ? color.Contrast(12) : color); }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorActive(Color color) { return color.Morph(CurrentColorActive, 0.40f); }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorDrag(Color color) { return color.Morph(CurrentColorActive, 0.60f); }
        /// <summary>
        /// Brush s aktuální barvou <see cref="CurrentColor"/>
        /// </summary>
        protected SolidBrush CurrentBrush { get { return DxComponent.PaintGetSolidBrush(CurrentColor); } }
        /// <summary>
        /// Brush s aktuální barvou <see cref="CurrentDotColor"/>
        /// </summary>
        protected SolidBrush CurrentDotBrush { get { return DxComponent.PaintGetSolidBrush(CurrentDotColor); } }
        /// <summary>
        /// Má se aktuálně zobrazovat grafika (čtyřtečka) uvnitř Splitteru?
        /// </summary>
        protected bool CurrentShowDots
        {
            get
            {
                var mode = VisualLogoMode;
                switch (CurrentSplitterState)
                {
                    case SplitterState.Disabled: return (mode == LogoMode.Allways);
                    case SplitterState.Enabled: return (mode == LogoMode.Allways);
                    case SplitterState.Hot: return (mode == LogoMode.Allways || mode == LogoMode.OnMouse);
                    case SplitterState.Down: return (mode == LogoMode.Allways || mode == LogoMode.OnMouse);
                    case SplitterState.Drag: return (mode == LogoMode.Allways || mode == LogoMode.OnMouse);
                }
                return false;
            }
        }
        /// <summary>
        /// Nastaví typ kurzoru pro this prvek podle aktuální orientace.
        /// </summary>
        /// <param name="force"></param>
        protected void SetCursor(bool force = false)
        {
            System.Windows.Forms.Orientation orientation = _Orientation;
            if (force || !_CursorOrientation.HasValue || _CursorOrientation.Value != orientation)
                this.Cursor = (orientation == System.Windows.Forms.Orientation.Horizontal ? Cursors.HSplit : Cursors.VSplit);
            _CursorOrientation = orientation;
        }
        private System.Windows.Forms.Orientation? _CursorOrientation;
        #endregion
        #region Interaktivita splitteru - reakce Splitteru na akce a pohyby myši
        /// <summary>
        /// Při vstupu myši nad control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this._SplitterMouseEnter();
            CurrentSplitterEnabled = SplitterEnabled;
            if (!CurrentSplitterEnabled) return;
            BringSplitterToFront(true);
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            CurrentMouseState = MouseState.Over;
            ChangeCursor(true);
            PaintSplitter();
        }
        /// <summary>
        /// Při odchodu myši z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            CurrentMouseState = MouseState.None;
            ChangeCursor(false);
            PaintSplitter();
        }
        /// <summary>
        /// Při stisknutí myši - příprava na možný Drag and Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!CurrentSplitterEnabled) return;
            if (e.Button != MouseButtons.Left) return;
            Point point = Control.MousePosition;
            MouseDownAbsolutePoint = point;
            MouseDownWorkingBounds = CurrentWorkingBounds;
            MouseDragAbsoluteSilentZone = new Rectangle(point.X - 2, point.Y - 2, 5, 5);
            MouseDragOriginalSplitPosition = SplitPosition;
            MouseDragLastSplitPosition = null;
            CurrentMouseState = MouseState.Down;
            PaintSplitter();
        }
        /// <summary>
        /// Při pohybu myši - mžná provedeme Drag and Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!CurrentSplitterEnabled) return;
            Point point = Control.MousePosition;
            if (CurrentSplitterState == SplitterState.Down) DetectSilentZone(point);          // Pokud je zmáčknutá myš, je stav Down; pokud se pohne o malý kousek, přejde stav do Drag
            if (CurrentSplitterState == SplitterState.Drag) DetectSplitterDragMove(point);    // Ve stavu Drag řídíme přesun splitteru
        }
        /// <summary>
        /// Při zvednutí myši - pokud byl Drag and Drop, pak jej dokončíme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!CurrentSplitterEnabled) return;
            if (CurrentSplitterState == SplitterState.Drag) DetectSplitterDragDone();         // Pokud jsme ve stavu Drag, ukončíme přesun splitteru
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            MouseDragAbsoluteSilentZone = null;
            MouseDragOriginalSplitPosition = null;
            MouseDragLastSplitPosition = null;
            CurrentMouseState = MouseState.Over;
            PaintSplitter();
        }
        /// <summary>
        /// Detekuje pohyb mimo <see cref="MouseDragAbsoluteSilentZone"/>.
        /// Pokud se myš pohybuje uvnitř (anebo pokud SilentZone už není), nic neprovádí.
        /// Pokud je ale SilentZone definovaná a myš se nachází mimo ni, pak SilentZone nuluje a nastaví <see cref="CurrentMouseState"/> = <see cref="MouseState.Drag"/>
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void DetectSilentZone(Point absolutePoint)
        {
            if (!MouseDragAbsoluteSilentZone.HasValue) return;
            if (MouseDragAbsoluteSilentZone.Value.Contains(absolutePoint)) return;
            MouseDragAbsoluteSilentZone = null;
            _SplitPositionDragBegin();
            CurrentMouseState = MouseState.Drag;
        }
        /// <summary>
        /// Detekuje pohyb myši ve stavu  <see cref="MouseState.Drag"/>, určuje novou hodnotu pozice a volá event 
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void DetectSplitterDragMove(Point absolutePoint)
        {
            if (!MouseDownAbsolutePoint.HasValue) return;
            Point originPoint = MouseDownAbsolutePoint.Value;
            Rectangle workingBounds = MouseDownWorkingBounds.Value;
            int distance = (Orientation == Orientation.Horizontal ? (absolutePoint.Y - originPoint.Y) : (absolutePoint.X - originPoint.X));
            int oldValue = MouseDragOriginalSplitPosition.Value;
            int newValue = oldValue + distance;                                          // Hodnota splitteru požadovaná posunem myši
            SetValidSplitPosition(newValue, useWorkingBounds: workingBounds, actions: SetActions.None);           // Korigovat danou myšovitou hodnotu, ale neměnit ani Bounds, ani Controls ani nevolat event PositionChanged
            int validValue = SplitPosition;                                              // Hodnota po korekci (se zohledněním Min distance Before a After)
            if (!MouseDragLastSplitPosition.HasValue || MouseDragLastSplitPosition.Value != validValue)
            {
                TEventValueChangeArgs<double> args = new TEventValueChangeArgs<double>(EventSource.User, oldValue, validValue);
                _SplitPositionDragMove(args);                                            // Tady voláme event SplitPositionDragMove
                RunSplitPositionChanging(args);                                            // Tady voláme event PositionChanging (po reálné změně hodnoty, a event Changing - nikoli Changed)
                DetectSplitterEventsModify(args, ref validValue);
                MouseDragLastSplitPosition = SplitPosition;
                RecalculateBounds(workingBounds);
                PaintSplitter();
            }
        }
        /// <summary>
        /// Po dokončení procesu Drag vyvolá event <see cref="SplitPositionChanged"/>.
        /// </summary>
        protected void DetectSplitterDragDone()
        {
            if (!MouseDragOriginalSplitPosition.HasValue || MouseDragOriginalSplitPosition.Value != SplitPosition)
            {
                int oldValue = MouseDragOriginalSplitPosition ?? 0;
                int newValue = SplitPosition;
                TEventValueChangeArgs<double> args = new TEventValueChangeArgs<double>(EventSource.User, oldValue, newValue);
                _SplitPositionDragDone(args);
                RunSplitPositionChanged(args);
                bool isChanged = DetectSplitterEventsModify(args, ref newValue);
                MouseDragOriginalSplitPosition = SplitPosition;
                if (isChanged)
                    RecalculateBounds(MouseDownWorkingBounds);
                PaintSplitter();
            }
        }
        /// <summary>
        /// Metoda zpracuje odpovědi v argumentu <paramref name="args"/>.
        /// Reaguje na Cancel, pak vrátí do <paramref name="validValue"/> původní hodnotu z argumentu = <see cref="TEventValueChangeArgs{T}.OldValue"/>;
        /// reaguje na Changed, pak do <paramref name="validValue"/> vloží nově zadanou hodnotu z argumentu = <see cref="TEventValueChangeArgs{T}.NewValue"/>;
        /// Pokud takto zaregistruje změnu, tak novou hodnotu vloží do SplitPosition a do Bounds a vrátí true.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="validValue"></param>
        /// <returns></returns>
        protected bool DetectSplitterEventsModify(TEventValueChangeArgs<double> args, ref int validValue)
        {
            bool changed = false;
            if (args.Cancel)
            {
                validValue = (int)args.OldValue;
                changed = true;
            }
            else if (args.Changed)
            {
                validValue = (int)args.NewValue;
                changed = true;
            }
            if (changed)
            {
                SetValidSplitPosition(validValue, useWorkingBounds: MouseDownWorkingBounds, actions: SetActions.None);     // Korigovat hodnotu dodanou aplikačním eventhandlerem, ale neměnit ani Bounds, ani Controls ani nevolat event PositionChanged
                validValue = SplitPosition;                                              // Hodnota po korekci (se zohledněním Min distance Before a After)
            }
            return changed;
        }
        /// <summary>
        /// Hodnota <see cref="SplitterEnabled"/> zachycená při MouseEnter, po skončení eventu <see cref="SplitterMouseEnter"/>, platná pro aktuální akce myši.
        /// Tzn. pokud při MouseEnter bude v eventu <see cref="SplitterMouseEnter"/> určena hodnota <see cref="SplitterEnabled"/> = false, 
        /// pak až do odchodu myši ze splitteru a do nového příchodu platí tato hodnota.
        /// </summary>
        protected bool CurrentSplitterEnabled { get; set; }
        /// <summary>
        /// Souřadnice bodu, kde byla stisknuta myš - v absolutních koordinátech
        /// </summary>
        protected Point? MouseDownAbsolutePoint { get; set; }
        /// <summary>
        /// Aktuální pracovní souřadnice splitteru <see cref="CurrentWorkingBounds"/>, platné v okamžiku MouseDown. Jindy je null.
        /// </summary>
        protected Rectangle? MouseDownWorkingBounds { get; set; }
        /// <summary>
        /// Souřadnice prostoru, kde budeme ignorovat pohyb myši po jejím MouseDown (v absolutních koordinátech).
        /// Tím potlačíme malé pohyby před zahájením Drag.
        /// Pokud je zde null, a v <see cref="MouseDownAbsolutePoint"/> pak už myš opustila výchozí SilentZone a reagujeme na její pohyby.
        /// </summary>
        protected Rectangle? MouseDragAbsoluteSilentZone { get; set; }
        /// <summary>
        /// Počáteční hodnota <see cref="SplitPosition"/> před zahájením Drag
        /// </summary>
        protected int? MouseDragOriginalSplitPosition { get; set; }
        /// <summary>
        /// Předchozí hodnota <see cref="SplitPosition"/> při posledním volání eventu 
        /// </summary>
        protected int? MouseDragLastSplitPosition { get; set; }
        /// <summary>
        /// Aktuální stav myši. Změna hodnoty vyvolá invalidaci = překreslení.
        /// </summary>
        protected MouseState CurrentMouseState { get { return _CurrentMouseState; } set { _CurrentMouseState = value; Invalidate(); } }
        private MouseState _CurrentMouseState;
        /// <summary>
        /// Metoda zajistí změnu kurzoru podle dané aktivity a aktuální orientace splitteru.
        /// </summary>
        /// <param name="active"></param>
        protected void ChangeCursor(bool active)
        {
            if (active)
                this.Cursor = (this.Orientation == Orientation.Horizontal ? Cursors.HSplit : (this.Orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.Default));
            else
                this.Cursor = Cursors.Default;
        }
        /// <summary>
        /// Aktuální stav Splitteru odpovídající stavu Enabled a stavu myši <see cref="CurrentMouseState"/>.
        /// </summary>
        protected SplitterState CurrentSplitterState
        {
            get
            {
                if (!this.Enabled) return SplitterState.Disabled;
                switch (this.CurrentMouseState)
                {
                    case MouseState.None: return SplitterState.Enabled;
                    case MouseState.Over: return SplitterState.Hot;
                    case MouseState.Down: return SplitterState.Down;
                    case MouseState.Drag: return SplitterState.Drag;
                }
                return SplitterState.Enabled;
            }
        }
        /// <summary>
        /// Stavy myši
        /// </summary>
        protected enum MouseState { None, Over, Down, Drag }
        /// <summary>
        /// Stavy prvku
        /// </summary>
        protected enum SplitterState { Enabled, Disabled, Hot, Down, Drag }
        #endregion
        #region Public properties - funkcionalita Splitteru (hodnota, orientace, šířka)
        /// <summary>
        /// Aktuální pozice splitteru = hodnota středového pixelu na ose X nebo Y, podle orientace.
        /// Setování této hodnoty VYVOLÁ event <see cref="SplitPositionChanged"/> a zajistí úpravu souřadnic navázaných objektů podle režimu <see cref="ActivityMode"/>.
        /// </summary>
        public int SplitPosition { get { return (int)Math.Round(_SplitPosition, 0); } set { SetValidSplitPosition(value, actions: SetActions.Default); } }
        /// <summary>
        /// Pozice splitteru uložená jako Double, slouží pro přesné výpočty pozic při <see cref="AnchorType"/> == <see cref="SplitterAnchorType.Relative"/>,
        /// kdy potřebujeme mít pozici i na desetinná místa.
        /// <para/>
        /// Interaktivní přesouvání vkládá vždy integer číslo, public hodnota <see cref="SplitPosition"/> je čtena jako Math.Round(<see cref="SplitPosition"/>, 0).
        /// Setovat double hodnotu je možno pomocí metody <see cref="SetValidSplitPosition(double?, int?, Rectangle?, SetActions)"/>.
        /// </summary>
        private double _SplitPosition;
        /// <summary>
        /// Viditelná šířka splitteru. Nastavuje se automaticky na nejbližší vyšší sudé číslo.
        /// Tento počet pixelů bude vykreslován.
        /// Rozsah hodnot = 0 až 30 px.
        /// Hodnota 0 je přípustná, splitter pak nebude viditelný.
        /// </summary>
        public int SplitThick { get { return this._SplitThick; } set { SetValidSplitPosition(null, value, actions: SetActions.Silent); } }
        private int _SplitThick;
        /// <summary>
        /// Orientace splitteru = vodorovná nebo svislá
        /// </summary>
        public Orientation Orientation { get { return this._Orientation; } set { _Orientation = value; SetCursor(); SetValidSplitPosition(null, actions: SetActions.Silent); } }
        private Orientation _Orientation;
        /// <summary>
        /// Příznak, zda má Splitter reagovat na vložení souřadnic do <see cref="Control.Bounds"/>.
        /// Pokud je true, pak po vložení souřadnic se ze souřadnic odvodí <see cref="SplitPosition"/> a <see cref="SplitThick"/>, a vepíše se do Splitteru.
        /// Default = false: souřadnice splitteru nelze změnit vložením hodnoty do <see cref="Control.Bounds"/>, takový pokus bude ignorován.
        /// </summary>
        public bool AcceptBoundsToSplitter { get { return this._AcceptBoundsToSplitter; } set { _AcceptBoundsToSplitter = value; } }
        private bool _AcceptBoundsToSplitter;
        /// <summary>
        /// Povolení aktivity splitteru.
        /// Vyhodnocuje se při vstupu myši nad Splitter, po proběhnutí eventu <see cref="SplitterMouseEnter"/>.
        /// Pokud je true, je povolen MouseDrag, jinak není.
        /// </summary>
        public bool SplitterEnabled { get { return this._SplitterEnabled; } set { _SplitterEnabled = value; } }
        private bool _SplitterEnabled;
        #endregion
        #region Public properties - vzhled
        /// <summary>
        /// Barva pozadí je vždy Transparent, nemá význam ji setovat
        /// </summary>
        public override Color BackColor { get { return Color.Transparent; } set { Invalidate(); } }
        /// <summary>
        /// Barvu splitteru vždy přebírej z barvy pozadí Parenta.
        /// Default hodnota = true, má přednost před barvou Skinu.
        /// Při souběhu <see cref="DevExpressSkinEnabled"/> = true; a <see cref="SplitterColorByParent"/> = true; bude barva převzata z Parent controlu.
        /// Pokud bude <see cref="SplitterColorByParent"/> = false; a <see cref="DevExpressSkinEnabled"/> = true; pak se barva splitteru bude přebírat ze Skinu.
        /// Pokud budou obě false, pak barva Splitteru bude dána barvou <see cref="SplitterColor"/>.
        /// </summary>
        public bool SplitterColorByParent { get { return _SplitterColorByParent; } set { _SplitterColorByParent = value; Invalidate(); } }
        private bool _SplitterColorByParent;
        /// <summary>
        /// Základní barva splitteru.
        /// Pokud je ale nastaveno <see cref="SplitterColorByParent"/> = true, pak je hodnota <see cref="SplitterColor"/> čtena z Parent.BackColor.
        /// Setování hodnoty je sice interně uložena, ale setovaná hodnota nemá vliv na zobrazení (až do změny nastaveni <see cref="SplitterColorByParent"/> na false).
        /// </summary>
        public Color SplitterColor { get { return CurrentColorBase; } set { _SplitterColor = value; Invalidate(); } }
        private Color _SplitterColor;
        /// <summary>
        /// Barva aktivního splitteru.
        /// Toto je pouze vzdálená cílová barva; reálně má splitter v aktivním stavu barvu základní <see cref="SplitterColor"/>,
        /// jen mírně modifikovanou směrem k této aktivní barvě <see cref="SplitterActiveColor"/>.
        /// </summary>
        public Color SplitterActiveColor { get { return CurrentColorActive; } set { _SplitterActiveColor = value; Invalidate(); } }
        private Color _SplitterActiveColor;
        /// <summary>
        /// Režim zobrazování grafiky (čtyřtečka) uprostřed Splitteru.
        /// Výchozí hodnota je <see cref="LogoMode.OnMouse"/>
        /// </summary>
        public LogoMode VisualLogoMode { get { return _VisualLogoMode; } set { _VisualLogoMode = value; Invalidate(); } }
        private LogoMode _VisualLogoMode;
        /// <summary>
        /// Počet teček zobrazovaných jako grafika ("čtyřtečka").
        /// Default = 4. Platné rozmezí = 0 až 30
        /// </summary>
        public int VisualLogoDotsCount { get { return _VisualLogoDotsCount; } set { _VisualLogoDotsCount = (value < 0 ? 0 : (value > 30 ? 30 : value)); Invalidate(); } }
        private int _VisualLogoDotsCount;
        #endregion
        #region DevExpress - reakce na změnu skinu, akceptování skinu pro vzhled Splitteru
        /// <summary>
        /// Obsahuje true, pokud this splitter je napojen na DevExpress skin.
        /// Výchozí hodnota je true.
        /// </summary>
        public bool DevExpressSkinEnabled
        {
            get { return _DevExpressSkinEnabled; }
            set
            {
                if (_DevExpressSkinEnabled)
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged -= DevExpressSkinChanged;
                _DevExpressSkinEnabled = value;
                if (_DevExpressSkinEnabled)
                {
                    DevExpressSkinLoad();
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpressSkinChanged;
                }
            }
        }
        private bool _DevExpressSkinEnabled;
        /// <summary>
        /// Provede se po změně DevExpress Skinu (event je volán z <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/> : <see cref="DevExpress.LookAndFeel.UserLookAndFeel.StyleChanged"/>)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpressSkinChanged(object sender, EventArgs e)
        {
            DevExpressSkinLoad();
        }
        /// <summary>
        /// Načte aktuální hodnoty DevExpress Skinu do this controlu
        /// </summary>
        private void DevExpressSkinLoad()
        {
            if (this.DevExpressSkinEnabled)
            {
                var skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
                var skin = DevExpress.Skins.SkinManager.Default.GetSkin(DevExpress.Skins.SkinProductId.Common, skinName);
                _DevExpressSkinBackColor = skin.GetSystemColor(SystemColors.ControlLight);
                _DevExpressSkinActiveColor = skin.GetSystemColor(SystemColors.HotTrack);
            }
        }
        /// <summary>
        /// Barva pozadí načtená z aktuálního Skinu.
        /// Bude akceptována, pokud je zadána a pokud <see cref="DevExpressSkinEnabled"/> je true.
        /// </summary>
        private Color? _DevExpressSkinBackColor;
        /// <summary>
        /// Barva aktivní načtená z aktuálního Skinu.
        /// Bude akceptována, pokud je zadána a pokud <see cref="DevExpressSkinEnabled"/> je true.
        /// </summary>
        private Color? _DevExpressSkinActiveColor;
        #endregion
        #region Abstraktní věci jsou, když tady nic není. Virtuální jsou ty, které sice jsou, ale stejně nic nedělají. GetWorkingBounds(), ApplySplitterToControls()
        /// <summary>
        /// V této metodě potomek určí prostor, ve kterém se může pohybovat Splitter.
        /// <para/>
        /// Vrácený prostor má dva významy:
        /// <para/>
        /// a) V první řadě určuje rozsah pohybu Splitteru od-do: např. pro svislý splitter je klíčem hodnota Left a Right vráceného prostoru = odkud a kam může splitter jezdit
        /// (k tomu poznámka: jde o souřadnice vnějšího okraje splitteru, tedy včetně jeho tloušťky: 
        /// pokud tedy X = 0, pak splitter bude mít svůj levý okraj nejméně na pozici 0, a jeho <see cref="SplitterControl.SplitPosition"/> tedy bude o půl <see cref="SplitterControl.SplitThick"/> větší).
        /// Pro vodorovný Splitter je v tomto ohledu klíčová souřadnice Top a Bottom.
        /// <para/>
        /// b) V druhé řadě určuje vrácený prostor velikost Splitteru v "neaktivním" směru: např. pro svislý splitter bude kreslen nahoře od pozice Top dolů, jeho výška bude = Height.
        /// Vodorovný Splitter si pak převezme Left a Width.
        /// <para/>
        /// Metoda je volaná při změně hodnoty nebo orientace nebo tloušťky, a na začátku interaktivního přemísťování pomocí myši.
        /// <para/>
        /// Tato metoda dostává jako parametr maximální možnou velikost = prostor v parentu. Metoda ji může vrátit beze změny, pak Splitter bude "jezdit" v celém parentu bez omezení.
        /// Bázová metoda to tak dělá - vrací beze změny dodaný parametr.
        /// </summary>
        /// <param name="currentArea">Souřadnice ClientArea, ve kterých se může pohybovat Splitter v rámci svého parenta</param>
        /// <returns></returns>
        protected virtual Rectangle GetCurrentWorkingBounds(Rectangle currentArea) { return currentArea; }
        #endregion
        #region Jádro splitteru - vložení hodnoty do splitteru, kontroly, výpočty souřadnic
        /// <summary>
        /// Provede změnu pozice splitteru na zadanou hodnotu <paramref name="splitPosition"/> a/nebo <see cref="SplitThick"/>.
        /// Lze tedy zadat všechny hodnoty najednou a navázané výpočty proběhnou jen jedenkrát.
        /// Všechny tyto hodnoty mají nějaký vliv na pozici a souřadnice splitteru, proto je vhodnější je setovat jedním voláním, které je tedy optimálnější.
        /// Zadanou hodnotu zkontroluje s ohledem na vlastnosti splitteru, uloží hodnotu do <see cref="_SplitPosition"/>.
        /// <para/>
        /// Tato metoda se používá interně při interaktivních pohybech, při zadání limitujících hodnot i jako reakce na vložení hodnoty do property <see cref="SplitPosition"/>.
        /// Touto metodou lze vložit hodnotu <see cref="SplitPosition"/> typu <see cref="Double"/>, což se využívá při změně velikosti parenta a typu kotvy <see cref="SplitterAnchorType.Relative"/>.
        /// Tam by se s hodnotou typu <see cref="Int32"/> nedalo pracovat.
        /// <para/>
        /// Tato metoda se aktivně brání rekurzivnímu vyvolání (k čemuž může dojít při použití techniky "TransferToParent").
        /// </summary>
        /// <param name="splitPosition">Nová hodnota <see cref="SplitPosition"/>. Pokud bude NULL, vezme se stávající pozice.</param>
        /// <param name="splitThick">Nová hodnota <see cref="SplitThick"/>, hodnota null = beze změny</param>
        /// <param name="useWorkingBounds">Použít dané souřadnice jako WorkingBounds (=nvyhodnocovat <see cref="CurrentWorkingBounds"/>, ani neukládat do <see cref="LastWorkingBounds"/>)</param>
        /// <param name="actions"></param>
        protected void SetValidSplitPosition(double? splitPosition, int? splitThick = null, Rectangle? useWorkingBounds = null, SetActions actions = SetActions.Default)
        {
            if (SetValidSplitPositionInProgress) return;

            try
            {
                SetValidSplitPositionInProgress = true;

                // Nejprve zpracuji explicitně zadanou hodnotu SplitThick, protože ta může mít vliv na algoritmus GetValidSplitPosition():
                bool changedThick = false;
                if (splitThick.HasValue)
                {
                    int oldThick = _SplitThick;
                    int newThick = GetValidSplitThick(splitThick.Value);
                    changedThick = (oldThick != newThick);
                    if (changedThick)
                        _SplitThick = newThick;
                }

                // Změna WorkingBounds:
                bool changedBounds = false;
                Rectangle workingBounds;
                if (useWorkingBounds.HasValue)
                {
                    workingBounds = useWorkingBounds.Value;
                }
                else
                {
                    Rectangle oldWorkingBounds = LastWorkingBounds;
                    Rectangle newWorkingBounds = CurrentWorkingBounds;
                    changedBounds = (newWorkingBounds != oldWorkingBounds);
                    if (changedBounds)
                        LastWorkingBounds = newWorkingBounds;
                    workingBounds = newWorkingBounds;
                }

                // A poté zpracuji Position - tu zpracuji i když by na vstupu byla hodnota null (pak jako požadovanou novou hodnotu budu brát hodnotu současnou),
                //  protože v metodě GetValidSplitPosition() se aplikují veškeré limity pro hodnotu, a ty se mohly změnit => proto může být volána this metoda:
                double oldPosition = _SplitPosition;
                double newPosition = GetValidSplitPosition(splitPosition ?? oldPosition, workingBounds);
                bool changedPosition = (Math.Round(newPosition, 2) != Math.Round(oldPosition, 2));
                if (changedPosition)
                    _SplitPosition = newPosition;

                // Pokud není žádná změna, a není ani požadavek na ForceActions, pak skončíme:
                bool force = (actions.HasFlag(SetActions.ForceActions));
                if (!(changedThick || changedBounds || changedPosition || force)) return;

                // Nastavit souřadnice podle aktuální hodnoty:
                if (actions.HasFlag(SetActions.RecalculateBounds)) RecalculateBounds(workingBounds, true);

                // Události:
                if (actions.HasFlag(SetActions.CallEventChanging)) RunSplitPositionChanging(new TEventValueChangeArgs<double>(EventSource.Code, oldPosition, newPosition));
                if (actions.HasFlag(SetActions.CallEventChanged)) RunSplitPositionChanged(new TEventValueChangeArgs<double>(EventSource.Code, oldPosition, newPosition));
            }
            finally
            {
                SetValidSplitPositionInProgress = false;
            }
        }
        /// <summary>
        /// Metoda ze zadaných souřadnic odvodí hodnoty splitPosition a splitThick a vloží je do this Splitteru.
        /// Pozor: potomek smí metodu přepsat, a z neaktivních souřadnic si může odvodit WorkingBounds, musí ale zavolat base.SetSplitterByBounds() ! Jinak nebude proveden základní výpočet.
        /// Základní výpočet ve třídě <see cref="SplitterControl"/> zajistí určení platné hodnoty <see cref="SplitThick"/> a <see cref="SplitPosition"/>, a jejich vložení do splitteru, 
        /// včetně validace hodnot a případné korekce souřadnic splitetru !
        /// <para/>
        /// Tato metoda je volána pouze tehdy, když jsou změněny souřadnice splitteru, a tento má nastaveno <see cref="SplitterControl.AcceptBoundsToSplitter"/> = true.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="actions">Akce prováděné Splitterem, pokud nebude zadáno použije se <see cref="SetActions.Default"/>.</param>
        protected virtual void SetSplitterByBounds(Rectangle bounds, SetActions? actions = null)
        {
            bool isHorizontal = (this.Orientation == Orientation.Horizontal);
            int splitThick = GetValidSplitThick((isHorizontal ? bounds.Height : bounds.Width));
            int th = splitThick / 2;
            double splitPosition = (isHorizontal ? bounds.Y : bounds.X) + th;
            SetValidSplitPosition(splitPosition, splitThick, null, (actions ?? SetActions.Default));
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně probíhá výkon metody <see cref="SetValidSplitPosition(double?, int?, Rectangle?, SetActions)"/>, v té době nebude spouštěna další iterace této metody
        /// </summary>
        protected bool SetValidSplitPositionInProgress { get; private set; } = false;
        /// <summary>
        /// Posledně platné pracovní souřadnice Splitteru. K těmto pracovním souřadnicím byly určeny souřadnice Splitteru.
        /// </summary>
        protected Rectangle LastWorkingBounds { get; private set; } = Rectangle.Empty;
        /// <summary>
        /// Metoda vrátí platnou hodnotu pro <see cref="SplitThick"/> pro libovolnou vstupní hodnotu.
        /// </summary>
        /// <param name="splitThick"></param>
        /// <returns></returns>
        protected static int GetValidSplitThick(int splitThick)
        {
            int t = splitThick.Align(0, 30);
            if ((t % 2) == 1) t++;               // Hodnota nesmí být lichá, to kvůli správnému počítání grafiky. Takže nejbližší vyšší sudá...
            return t;
        }
        /// <summary>
        /// Metoda ověří zadanou požadovanou pozici splitteru a vrátí hodnotu platnou.
        /// Potomek může metodu přepsat a hodnotu kontrolovat jinak.
        /// Na vstupu je požadovaná hodnota <see cref="SplitterControl.SplitPosition"/>
        /// a souřadnice pracovního prostoru, které vygenerovala metoda <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/>
        /// </summary>
        /// <param name="splitPosition">Zvenku daná pozice Splitteru, požadavek</param>
        /// <param name="currentWorkingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <returns></returns>
        protected virtual double GetValidSplitPosition(double splitPosition, Rectangle currentWorkingBounds)
        {
            Rectangle logicalWorkingBounds = GetLogicalRectangle(currentWorkingBounds);
            double th = (double)SplitThick / 2d;
            double min = 0d;
            double max = (double)MaxSize;
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    min = (double)logicalWorkingBounds.Top + th;
                    max = (double)logicalWorkingBounds.Bottom - th;
                    break;
                case Orientation.Vertical:
                    min = (double)logicalWorkingBounds.Left + th;
                    max = (double)logicalWorkingBounds.Right - th;
                    break;
            }
            return splitPosition.Align(min, max);
        }
        /// <summary>
        /// Aktuální pozice splitteru posunutá o Scroll pozici aktuálního containeru.
        /// Pokud Parent container je AutoScroll, pak se Splitter má vykreslovat na jiných souřadnicích, než odpovídá hodnotě <see cref="SplitPosition"/> = právě o posun AutoScrollu.
        /// </summary>
        protected int CurrentSplitPosition
        {
            get
            {
                int splitPosition = SplitPosition;
                Point offset = CurrentOrigin;
                if (!offset.IsEmpty)
                {
                    switch (_Orientation)
                    {
                        case Orientation.Horizontal: return splitPosition + offset.Y;
                        case Orientation.Vertical: return splitPosition + offset.X;
                    }
                }
                return splitPosition;
            }
        }
        /// <summary>
        /// Maximální velikost - použitá v případě, kdy není znám Parent
        /// </summary>
        protected const int MaxSize = 10240;
        #endregion
        #region Eventy, háčky a jejich spouštěče
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitterMouseEnter()"/> a event <see cref="SplitterMouseEnter"/>
        /// </summary>
        private void _SplitterMouseEnter()
        {
            OnSplitterMouseEnter();
            SplitterMouseEnter?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Volá se při vstupu myši na splitter. 
        /// Důsledkem události může být změna stavu splitteru v property <see cref="SplitterEnabled"/>.
        /// <see cref="SplitterControl"/> si po proběhnutí této události uschová hodnotu <see cref="SplitterEnabled"/> do soukromé proměnné, 
        /// která následně řídí funkcionalitu splitteru i jeho vykreslování jako reakci na pohyb myši.
        /// <para/>
        /// Následovat budou události <see cref="OnSplitPositionDragBegin()"/> (při zahájení pohybu),
        /// <see cref="OnSplitPositionDragMove"/> (po každém pixelu) a <see cref="OnSplitPositionDragDone"/> (po zvednutí myši).
        /// </summary>
        protected virtual void OnSplitterMouseEnter() { }
        /// <summary>
        /// Událost volaná jedenkrát při každém vstupu myši na splitter.
        /// Důsledkem události může být změna stavu splitteru v property <see cref="SplitterEnabled"/>.
        /// <see cref="SplitterControl"/> si po proběhnutí této události uschová hodnotu <see cref="SplitterEnabled"/> do soukromé proměnné, 
        /// která následně řídí funkcionalitu splitteru i jeho vykreslování jako reakci na pohyb myši.
        /// <para/>
        /// Následovat budou eventy <see cref="SplitPositionDragBegin"/> (při zahájení pohybu),
        /// <see cref="SplitPositionDragMove"/> (po každém pixelu) a <see cref="SplitPositionDragDone"/> (po zvednutí myši).
        /// </summary>
        public event EventHandler SplitterMouseEnter;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragBegin()"/> a event <see cref="SplitPositionDragBegin"/>
        /// </summary>
        private void _SplitPositionDragBegin()
        {
            OnSplitPositionDragBegin();
            SplitPositionDragBegin?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Volá se při zahájení interaktivního přesunu splitteru pomocí myši (po stisknutí myši a prvním pohybu).
        /// </summary>
        protected virtual void OnSplitPositionDragBegin() { }
        /// <summary>
        /// Událost volaná jedenkrát při každém zahájení interaktivního přesunu splitteru pomocí myši (po stisknutí myši a prvním pohybu).
        /// Následovat budou eventy <see cref="SplitPositionChanging"/> (po každém pixelu) a <see cref="SplitPositionChanged"/> (po zvednutí myši).
        /// </summary>
        public event EventHandler SplitPositionDragBegin;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragMove(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionDragMove"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionDragMove(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionDragMove(args);
            SplitPositionDragMove?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionDragMove(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> při interaktivním přemísťování splitteru myší
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionDragMove;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragDone(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionDragDone"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionDragDone(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionDragDone(args);
            SplitPositionDragDone?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionDragDone(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> při dokončení interaktivního přemísťování splitteru myší
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionDragDone;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionChanging(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionChanging"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void RunSplitPositionChanging(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionChanging(args);
            SplitPositionChanging?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionChanging(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> v procesu interaktivního přemísťování
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionChanging;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionChanged(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionChanged"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void RunSplitPositionChanged(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionChanged(args);
            SplitPositionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se po dokončení pohybu splitteru = po pohybu a po zvednutí myši.
        /// </summary>
        protected virtual void OnSplitPositionChanged(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> z kódu, a po dokončení procesu interaktivního přemísťování
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionChanged;
        #endregion
        #region Souřadnice jsou věc specifická...   Vkládání souřadnic, konverze souřadnic při AutoScrollu (logické / aktuální)
        /// <summary>
        /// Tudy chodí externí setování souřadnic...
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="specified"></param>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // Po změnách datových hodnot Splitteru vkládáme jeho nové souřadnice v metodě RecalculateBounds() přímo do base metody SetBoundsCore().
            //    (takže vložení souřadnic do splitteru po vložení hodnoty Splitteru NEJDE touto metodou!)
            // Do této metody nám tedy vstupuje řízení pouze po EXTERNÍ změně souřadnic.

            if (this.AcceptBoundsToSplitter)
            {   // Pokud je aktivní příznak AcceptBoundsToSplitter, pak dodané souřadnice zpracujeme do souřadnic i do dat Splitteru:
                base.SetBoundsCore(x, y, width, height, specified);            // Nejprve vložíme souřadnice...
                this.SetSplitterByBounds(new Rectangle(x, y, width, height));  // A pak podle souřadnic nastavíme Splitter
            }
        }
        /// <summary>
        /// Vypočítá správné vnější souřadnice Splitteru a uloží je do base.Bounds; volitelně vyvolá invalidaci = překreslení.
        /// <para/>
        /// Tato metoda se aktivně brání rekurzivnímu vyvolání (k čemuž může dojít při použití techniky "TransferToParent").
        /// </summary>
        /// <param name="workingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <param name="withInvalidate"></param>
        protected void RecalculateBounds(Rectangle? workingBounds = null, bool withInvalidate = false)
        {
            if (RecalculateBoundsInProgress) return;
            try
            {
                RecalculateBoundsInProgress = true;

                Rectangle bounds = GetCurrentBounds(workingBounds);
                // Splitter umisťuje jen sám sebe:
                if (bounds != this.Bounds)
                    base.SetBoundsCore(bounds.X, bounds.Y, bounds.Width, bounds.Height, BoundsSpecified.All);   // Tato metoda REÁLNĚ nastaví Bounds this controlu.
                if (withInvalidate && Initialized) Invalidate();
            }
            finally
            {
                RecalculateBoundsInProgress = false;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně probíhá výkon metody <see cref="SplitterControl.RecalculateBounds(Rectangle?, bool)"/>, v té době nebude spouštěna další iterace této metody
        /// </summary>
        protected bool RecalculateBoundsInProgress { get; private set; } = false;
        /// <summary>
        /// Vrátí aktuální souřadnice prvku (Bounds) pro jeho umístění = nikoli pro jeho vykreslení.
        /// Souřadnice určí na základě pozice splitteru <see cref="SplitterControl.SplitPosition"/> a jeho orientace <see cref="SplitterControl.Orientation"/>, 
        /// jeho šíři <see cref="SplitterControl.SplitThick"/>
        /// a na základě pracovních souřadnic dle parametru <paramref name="workingBounds"/>, viz i metoda <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/>.
        /// <para/>
        /// Výsledné souřadnice posune o AutoScroll position <see cref="CurrentOrigin"/>.
        /// </summary>
        /// <param name="workingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <returns></returns>
        protected virtual Rectangle GetCurrentBounds(Rectangle? workingBounds = null)
        {
            int sp = CurrentSplitPosition;
            int th = (SplitThick / 2);
            Rectangle cwb = workingBounds ?? CurrentWorkingBounds;
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    return new Rectangle(cwb.X, sp - th, cwb.Width, SplitThick);
                case Orientation.Vertical:
                    return new Rectangle(sp - th, cwb.Y, SplitThick, cwb.Height);
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vrátí souřadnice vizuální (akceptující AutoScroll) pro dané souřadnice logické
        /// </summary>
        /// <param name="logicalBounds"></param>
        /// <returns></returns>
        protected Rectangle GetCurrentRectangle(Rectangle logicalBounds)
        {
            Point currentOrigin = CurrentOrigin;
            if (currentOrigin.IsEmpty) return logicalBounds;
            return new Rectangle(logicalBounds.X + currentOrigin.X, logicalBounds.Y + currentOrigin.Y, logicalBounds.Width, logicalBounds.Height);
        }
        /// <summary>
        /// Metoda vrátí souřadnice logické (akceptující původní bod 0/0) pro dané souřadnice vizuálně, akceptujíc AutoScroll
        /// </summary>
        /// <param name="currentBounds"></param>
        /// <returns></returns>
        protected Rectangle GetLogicalRectangle(Rectangle currentBounds)
        {
            Point currentOrigin = CurrentOrigin;
            if (currentOrigin.IsEmpty) return currentBounds;
            return new Rectangle(currentBounds.X - currentOrigin.X, currentBounds.Y - currentOrigin.Y, currentBounds.Width, currentBounds.Height);
        }
        /// <summary>
        /// Souřadnice bodu 0/0.
        /// On totiž počáteční bod ve WinForm controlech může být posunutý, pokud Parent control je typu <see cref="ScrollableControl"/> s aktivním scrollingem.
        /// </summary>
        protected Point CurrentOrigin
        {
            get
            {
                if (!(this.Parent is ScrollableControl parent) || !parent.AutoScroll) return Point.Empty;
                return parent.AutoScrollPosition;
            }
        }
        /// <summary>
        /// Obsahuje velikost plochy Parenta, ve které se může pohybovat splitter
        /// </summary>
        protected Size CurrentParentSize
        {
            get
            {
                var parent = this.Parent;
                if (parent is null) return new Size(MaxSize, MaxSize);
                if (parent is ScrollableControl scrollParent)
                    return scrollParent.ClientSize;
                return parent.ClientSize;
            }
        }
        /// <summary>
        /// Aktuální pracovní souřadnice Splitteru. Určuje je potomek ve virtual metodě <see cref="GetCurrentWorkingBounds(Rectangle)"/>.
        /// Výsledné souřadnice posune o AutoScroll position <see cref="CurrentOrigin"/>.
        /// </summary>
        protected Rectangle CurrentWorkingBounds
        {
            get
            {
                Rectangle currentArea = new Rectangle(CurrentOrigin, CurrentParentSize);
                Rectangle currentWorkingBounds = GetCurrentWorkingBounds(currentArea);
                return currentWorkingBounds;
            }
        }
        #endregion
        #region Splitter si hlídá svého parenta, aby zareagoval na jeho barvy
        /// <summary>
        /// Po změně Parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DetectCurrentParent();
            if (this.SplitterColorByParent) this.Invalidate();
        }
        /// <summary>
        /// Reaguje na změnu parenta
        /// </summary>
        protected void DetectCurrentParent()
        {
            Control parentNew = this.Parent;
            Control parentOld = _ParentOld;
            if (Object.ReferenceEquals(parentNew, parentOld)) return;        // Pokud oba jsou null, nebo oba jsou totéž, nemusím nic dělat

            if (parentOld != null)
            {
                parentOld.ControlAdded -= _CurrentParent_ControlAdded;
                _ParentOld = null;
            }
            if (parentNew != null)
            {
                parentNew.ControlAdded += _CurrentParent_ControlAdded;
                _ParentOld = parentNew;
            }
        }
        /// <summary>
        /// Když si můj parent přidá jakýkoli nový control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentParent_ControlAdded(object sender, ControlEventArgs e)
        {
            BringSplitterToFront(false);
        }
        private Control _ParentOld;
        #endregion
        #region Splitter je rád, když je úplně nahoře v Z-Order
        /// <summary>
        /// Přemístí this splitter nahoru v poli controlů našeho parenta.
        /// <para/>
        /// Parametr <paramref name="isMouseEnter"/> říká:
        /// true = metoda je volána z události MouseEnter, je požadováno aby this splitter byl naprosto navrchu;
        /// false = metoda je volána z události Parent.ControlAdded, je požadováno aby nad this splitterem byly už pouze jiné splittery.
        /// </summary>
        /// <param name="isMouseEnter">Informace: true = voláno z MouseEnter, false = volánoz Parent.ControlAdded</param>
        protected void BringSplitterToFront(bool isMouseEnter)
        {
            var allControls = AllControls;
            if (allControls.Count <= 1) return;            // Pokud nejsou žádné prvky (=blbost, jsem tu já), anebo je jen jeden prvek (to jsem já), pak není co řešit...
            int index = allControls.FindIndex(c => object.ReferenceEquals(c, this));
            if (index <= 0) return;                        // Pokud já jsem na indexu [0] (tj. úplně nahoře), anebo tam nejsem vůbec (blbost), pak není co řešit

            // Já nejsem na pozici [0] = někdo je ještě nade mnou:
            bool bringToFront = false;
            if (isMouseEnter)
                // Máme být úplně navrchu:
                bringToFront = true;
            else
            {   // Nad námi smí být pouze jiné splittery:
                for (int i = 0; i < index && !bringToFront; i++)
                {
                    if (!(allControls[i] is DxSimpleSplitter))
                        bringToFront = true;
                }
            }

            // Dáme sebe (=Splitter) nahoru:
            if (bringToFront)
                this.BringToFront();
        }
        /// <summary>
        /// Pole Child controlů mého Parenta = "moji sourozenci včetně mě".
        /// Pokud ještě nemám parenta, pak toto pole obsahuje jen jeden prvek a to jsem já.
        /// Pokud má vrácené pole více prvků, pak někde v něm budu i já = <see cref="CurrentParent"/> :-).
        /// <para/>
        /// Index na pozici [0] je úplně nahoře nade všemi, postupně jsou prvky směrem dolů...
        /// Pozici aktuální prvku 
        /// </summary>
        protected List<Control> AllControls
        {
            get
            {
                var parent = this.Parent;
                if (parent == null) return new List<Control> { this };
                return parent.Controls.OfType<Control>().ToList();
            }
        }
        #endregion
        #region Enumy těsně svázané se Splitterem
        /// <summary>
        /// Režim zobrazování vizuálního loga (například čtyřtečka) uprostřed splitbaru (při velikosti <see cref="SplitThick"/> nad 4px)
        /// </summary>
        public enum LogoMode
        {
            /// <summary>
            /// Nikdy
            /// </summary>
            None = 0,
            /// <summary>
            /// Jen pod myší
            /// </summary>
            OnMouse,
            /// <summary>
            /// Vždy
            /// </summary>
            Allways
        }
        /// <summary>
        /// Akce prováděné po vložení hodnot do splitteru
        /// </summary>
        [Flags]
        protected enum SetActions
        {
            /// <summary>
            /// Žádná akce
            /// </summary>
            None = 0,
            /// <summary>
            /// Povinně provést akce, i když nebude detekována žádná změna hodnoty
            /// </summary>
            ForceActions = 0x0001,
            /// <summary>
            /// Přepočítat souřadnice splitteru
            /// </summary>
            RecalculateBounds = 0x0010,
            /// <summary>
            /// Přemístit navázané controly podle režimu aktivity
            /// </summary>
            MoveControlsByActivityMode = 0x0100,
            /// <summary>
            /// Přemístit navázané controly vždy = bez ohledu na režim aktivity
            /// </summary>
            MoveControlsAlways = 0x0200,
            /// <summary>
            /// Vyvolat událost Changing (stále probíhá změna)
            /// </summary>
            CallEventChanging = 0x1000,
            /// <summary>
            /// Vyvolat událost Changed (změna proběhla a je dokončena)
            /// </summary>
            CallEventChanged = 0x2000,

            /// <summary>
            /// Defaultní sada akcí: <see cref="RecalculateBounds"/> + <see cref="MoveControlsByActivityMode"/> + <see cref="CallEventChanged"/>, ale žádné násilí
            /// </summary>
            Default = RecalculateBounds | MoveControlsByActivityMode | CallEventChanged,
            /// <summary>
            /// Tichá sada akcí: <see cref="RecalculateBounds"/> + <see cref="MoveControlsByActivityMode"/>, ale žádné eventy a žádné násilí
            /// </summary>
            Silent = RecalculateBounds | MoveControlsByActivityMode
        }
        #endregion
    }
    #endregion
    #region DxChartControl
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.ChartControl"/> pro použití v ASOL.Nephrite
    /// </summary>
    internal class DxChartControl : DevExpress.XtraCharts.ChartControl
    {
        #region Support pro práci s grafem
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxChartControl()
        {
            this.HardwareAcceleration = true;
        }
        /// <summary>
        /// XML definice vzhledu grafu (Layout), aktuálně načtený z komponenty
        /// </summary>
        public string ChartXmlLayout
        {
            get { return GetGuiValue<string>(() => this._GetChartXmlLayout()); }
            set { SetGuiValue<string>(v => this._SetChartXmlLayout(v), value); }
        }
        /// <summary>
        /// Reference na data zobrazená v grafu
        /// </summary>
        public object ChartDataSource
        {
            get { return GetGuiValue<object>(() => this._GetChartDataSource()); }
            set { SetGuiValue<object>(v => this._SetChartDataSource(v), value); }
        }
        /// <summary>
        /// Vloží do grafu layout, a současně data, v jednom kroku
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        public void SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Zajistí editaci grafu pomocí Wizarda DevExpress
        /// </summary>
        public void ShowChartWizard()
        {
            if (!IsChartWorking)
                throw new InvalidOperationException($"V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.");

            bool valid = DxChartDesigner.DesignChart(this, "Upravte graf...", true, false);

            // Wizard pracuje nad naším controlem, veškeré změny ve Wizardu provedené se ihned promítají do našeho grafu.
            // Pokud uživatel dal OK, chceme změny uložit i do příště,
            // pokud ale dal Cancel, pak změny chceme zahodit a vracíme se k původnímu layoutu:
            if (valid)
            {
                string newLayout = _GetLayoutFromControl();
                _ValidChartXmlLayout = newLayout;
                OnChartXmlLayoutEdited();
            }
            else
            {
                _RestoreChartXmlLayout();
            }
        }
        /// <summary>
        /// Je voláno po editaci layoutu, vyvolá event <see cref="ChartXmlLayoutEdited"/>
        /// </summary>
        protected virtual void OnChartXmlLayoutEdited()
        {
            ChartXmlLayoutEdited?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost, kdy uživatel změnil data (vyvoláno metodou <see cref="ShowChartWizard"/>), a v designeru uložil změnu dat.
        /// V tuto chvíli je již nový layout k dispozici v <see cref="ChartXmlLayout"/>.
        /// </summary>
        public event EventHandler ChartXmlLayoutEdited;
        #endregion
        #region private práce s layoutem a daty
        /// <summary>
        /// Vloží do grafu layout, ponechává data
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        private void _SetChartXmlLayout(string xmlLayout)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            var dataSource = _ChartDataSource;             // Načteme z WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí XML layout načtený přímo z grafu
        /// </summary>
        /// <returns></returns>
        private string _GetChartXmlLayout()
        {
            return _GetLayoutFromControl();
        }
        /// <summary>
        /// Vloží do grafu dříve platný layout (uložený v metodě <see cref="_SetChartXmlLayout(string)"/>), ponechává data.
        /// </summary>
        private void _RestoreChartXmlLayout()
        {
            var dataSource = _ChartDataSource;             // Tabulka z WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vloží do grafu data, ponechává layout (může dojít k chybě)
        /// </summary>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        private void _SetChartDataSource(object dataSource)
        {
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí data grafu
        /// </summary>
        /// <returns></returns>
        private object _GetChartDataSource()
        {
            return _ChartDataSource;
        }
        /// <summary>
        /// Do grafu korektně vloží data i layout, ve správném pořadí.
        /// Pozor: tato metoda neukládá dodané objekty do lokálních proměnných <see cref="_ValidChartXmlLayout"/> a <see cref="_ChartDataSource"/>, pouze do controlu grafu!
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="xmlLayout"></param>
        private void _SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            // Tato sekvence je důležitá, jinak dojde ke zbytečným chybám:
            try
            {
                this._SetLayoutToControl(xmlLayout);
            }
            finally
            {   // Datový zdroj do this uložím i po chybě, abych mohl i po vložení chybného layoutu otevřít editor:
                this.DataSource = dataSource;
            }
        }
        /// <summary>
        /// Fyzicky načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetLayoutFromControl()
        {
            string layout = null;
            if (IsChartValid)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    this.SaveToStream(ms);
                    layout = Encoding.UTF8.GetString(ms.GetBuffer());
                }
            }
            return layout;
        }
        /// <summary>
        /// Vloží daný string jako Layout do grafu. 
        /// Neřeší try catch, to má řešit volající včetně ošetření chyby.
        /// POZOR: tato metoda odpojí datový zdroj, proto je třeba po jejím skončení znovu vložit zdroj do _ChartControl.DataSource !
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControl(string layout)
        {
            if (IsChartValid)
            {
                try
                {
                    this.BeginInit();
                    this.Reset();
                    _SetLayoutToControlDirect(layout);
                }
                finally
                {
                    this.EndInit();
                }
            }
        }
        /// <summary>
        /// Vynuluje layout
        /// </summary>
        public void Reset()
        {
            this.DataSource = null;
            string emptyLayout = @"<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='20.1.6.0'>
  <Chart AppearanceNameSerializable='Default' SelectionMode='None' SeriesSelectionMode='Series'>
    <DataContainer ValidateDataMembers='true' BoundSeriesSorting='None'>
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
  </Chart>
</ChartXmlSerializer>";
            emptyLayout = emptyLayout.Replace("'", "\"");
            _SetLayoutToControlDirect(emptyLayout);

            this.ResetLegendTextPattern();
            this.Series.Clear();
            this.Legends.Clear();
            this.Titles.Clear();
            this.AutoLayout = false;
            this.CalculatedFields.Clear();
            this.ClearCache();
            this.Diagram = null;
        }
        /// <summary>
        /// Fyzicky vloží daný string do layoutu grafu
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControlDirect(string layout)
        {
            byte[] buffer = (!String.IsNullOrEmpty(layout) ? Encoding.UTF8.GetBytes(layout) : new byte[0]);
            if (buffer.Length > 0)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer))
                    this.LoadFromStream(ms);     // Pozor, zahodí data !!!
            }
        }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) 
        /// </summary>
        public bool IsChartValid { get { return (!this.IsDisposed); } }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) a obsahuje data (má datový zdroj)
        /// </summary>
        public bool IsChartWorking { get { return (IsChartValid && this.DataSource != null); } }
        /// <summary>
        /// Offline uložený layout grafu, který byl setovaný zvenku; používá se při refreshi dat pro nové vložení stávajícího layoutu do grafu. 
        /// Při public čtení layoutu se nevrací tento string, ale fyzicky se načte aktuálně použitý layout z grafu.
        /// </summary>
        private string _ValidChartXmlLayout;
        /// <summary>
        /// Reference na tabulku s daty grafu, nebo null
        /// </summary>
        private object _ChartDataSource
        {
            get
            {
                var wr = _ChartDataSourceWR;
                return ((wr != null && wr.TryGetTarget(out var table)) ? table : null);
            }
            set
            {
                _ChartDataSourceWR = ((value != null) ? new WeakReference<object>(value) : null);
            }
        }
        /// <summary>
        /// WeakReference na tabulku s daty grafu
        /// </summary>
        private WeakReference<object> _ChartDataSourceWR;
        #endregion
        #region ASOL standardní rozšíření
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="SWF.Control.Visible"/> = true.
        /// Hodnota <see cref="SWF.Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="SWF.Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="SWF.Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="SWF.Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu
        /// </summary>
        /// <param name="action"></param>
        protected void RunInGui(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected T GetGuiValue<T>(Func<T> reader)
        {
            if (this.InvokeRequired)
                return (T)this.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        protected void SetGuiValue<T>(Action<T> writer, T value)
        {
            if (this.InvokeRequired)
                this.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
    }
    #endregion
    #region DxChartDesigner
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.Designer.ChartDesigner"/> pro editaci definice grafu
    /// </summary>
    internal class DxChartDesigner : DevExpress.XtraCharts.Designer.ChartDesigner, Noris.Clients.Win.Components.IEscapeHandler
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        public DxChartDesigner(object chart) : base(chart) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="designerHost"></param>
        public DxChartDesigner(object chart, System.ComponentModel.Design.IDesignerHost designerHost) : base(chart, designerHost) { }
        /// <summary>
        /// Zobrazí designer pro daný graf, a vrátí true = OK
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="caption"></param>
        /// <param name="showActualData"></param>
        /// <param name="topMost"></param>
        /// <returns></returns>
        public static bool DesignChart(object chart, string caption, bool showActualData, bool topMost)
        {
            DxChartDesigner chartDesigner = new DxChartDesigner(chart);
            chartDesigner.Caption = caption;
            chartDesigner.ShowActualData = showActualData;
            var result = chartDesigner.ShowDialog(topMost);
            return (result == SWF.DialogResult.OK);
        }
        /// <summary>
        /// Desktop okno hlídá klávesu Escape: 
        /// c:\inetpub\wwwroot\Noris46\Noris\ClientImages\ClientWinForms\WinForms.Host\Windows\WDesktop.cs
        /// metoda escapeKeyFilter_EscapeKeyDown()
        /// Když dostane Escape, najde OnTop okno které implementuje IEscapeHandler, a zavolá zdejší metodu.
        /// My vrátíme true = OK, aby desktop dál ten Escape neřešil, a my se v klidu zavřeme nativním zpracováním klávesy Escape ve WinFormu.
        /// </summary>
        /// <returns></returns>
        bool Noris.Clients.Win.Components.IEscapeHandler.HandleEscapeKey()
        {
            return true;
        }
    }
    #endregion
    #region DataMenuItem + DataTextItem, interface IMenuItem + ITextItem + IToolTipItem
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataMenuItem : DataTextItem, IMenuItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataMenuItem()
        {
            this.Enabled = true;
        }
        /// <summary>
        /// Metoda vytvoří new instanci třídy <see cref="DataMenuItem"/>, které bude obsahovat data z dodané <see cref="IMenuItem"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static DataMenuItem CreateClone(IMenuItem source, Action<DataMenuItem> modifier = null)
        {
            if (source == null) return null;
            DataMenuItem clone = new DataMenuItem();
            clone.FillFrom(source);
            if (modifier != null) modifier(clone);
            return clone;
        }
        /// <summary>
        /// Do this instance přenese patřičné hodnoty ze source instance
        /// </summary>
        /// <param name="source"></param>
        protected void FillFrom(IMenuItem source)
        {
            base.FillFrom((ITextItem)source);

            ParentItem = source.ParentItem;
            ItemType = source.ItemType;
            ChangeMode = source.ChangeMode;
            HotKey = source.HotKey;
            SubItems = (source.SubItems != null ? new List<IMenuItem>(source.SubItems) : null);
            MenuAction = source.MenuAction;
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="DataTextItem.ToString()"/>
        /// </summary>
        protected override string DebugText
        {
            get
            {
                string debugText = $"Id: {_ItemId}; Text: {Text}; Type: {ItemType}";
                if (this.SubItems != null)
                    debugText += $"; SubItems: {this.SubItems.Count}";
                return debugText;
            }
        }
        /// <summary>
        /// Z dodané kolekce prvků sestaví setříděný List a vrátí jej
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<IMenuItem> SortItems(IEnumerable<IMenuItem> items)
        {
            List<IMenuItem> list = new List<IMenuItem>();
            if (items != null)
                list.AddRange(items.Where(p => p != null));
            if (list.Count > 1)
            {
                int itemOrder = 0;
                foreach (var item in list)
                {
                    if (item.ItemOrder == 0) item.ItemOrder = ++itemOrder; else if (item.ItemOrder > itemOrder) itemOrder = item.ItemOrder;
                }
                list.Sort((a, b) => a.ItemOrder.CompareTo(b.ItemOrder));
            }
            return list;
        }
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IMenuItem"/>
        /// </summary>
        public virtual IMenuItem ParentItem { get; set; }
        /// <summary>
        /// Typ položky
        /// </summary>
        public virtual MenuItemType ItemType { get; set; }
        /// <summary>
        /// Režim pro vytvoření / refill / remove tohoto prvku
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Klávesa
        /// </summary>
        public virtual string HotKey { get; set; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu.
        /// Výchozí hodnota je null.
        /// </summary>
        public virtual List<IMenuItem> SubItems { get; set; }
        /// <summary>
        /// Explicitně daná akce po aktivaci této položky menu
        /// </summary>
        public Action<IMenuItem> MenuAction { get; set; }

        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IMenuItem> IMenuItem.SubItems { get { return this.SubItems; } }
        /// <summary>
        /// Titulek ToolTipu (pokud není zadán explicitně) se přebírá z textu prvku
        /// </summary>
        string IToolTipItem.ToolTipTitle { get { return ToolTipTitle ?? Text; } }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public interface IMenuItem : ITextItem
    {
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IMenuItem"/>
        /// </summary>
        IMenuItem ParentItem { get; set; }
        /// <summary>
        /// Typ položky
        /// </summary>
        MenuItemType ItemType { get; }
        /// <summary>
        /// Režim pro vytvoření / refill / remove tohoto prvku
        /// </summary>
        ContentChangeMode ChangeMode { get; }
        /// <summary>
        /// Klávesa
        /// </summary>
        string HotKey { get; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu
        /// </summary>
        IEnumerable<IMenuItem> SubItems { get; }
        /// <summary>
        /// Explicitně daná akce po aktivaci této položky menu
        /// </summary>
        Action<IMenuItem> MenuAction { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataTextItem : ITextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataTextItem()
        {
            this._ItemId = null;
            this.Enabled = true;
        }
        /// <summary>
        /// Metoda vytvoří new instanci třídy <see cref="DataMenuItem"/>, které bude obsahovat data z dodané <see cref="IMenuItem"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static DataTextItem CreateClone(ITextItem source, Action<DataTextItem> modifier = null)
        {
            if (source == null) return null;
            DataTextItem clone = new DataTextItem();
            clone.FillFrom(source);
            if (modifier != null) modifier(clone);
            return clone;
        }
        /// <summary>
        /// Do this instance přenese patřičné hodnoty ze source instance
        /// </summary>
        /// <param name="source"></param>
        protected void FillFrom(ITextItem source)
        {
            ItemId = source.ItemId;
            Text = source.Text;
            ItemOrder = source.ItemOrder;
            ItemIsFirstInGroup = source.ItemIsFirstInGroup;
            Enabled = source.Enabled;
            Checked = source.Checked;
            Image = source.Image;
            ImageUnChecked = source.ImageUnChecked;
            ImageChecked = source.ImageChecked;
            ItemPaintStyle = source.ItemPaintStyle;
            ToolTipText = source.ToolTipText;
            ToolTipTitle = source.ToolTipTitle;
            ToolTipIcon = source.ToolTipIcon;
            Tag = source.Tag;
        }
        /// <summary>
        /// Vizualizace = pro přímé použití v GUI objektech (např. jako prvek ListBoxu)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (this.Text ?? "");
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="ToString()"/>
        /// </summary>
        protected virtual string DebugText
        {
            get
            {
                string debugText = $"Id: {_ItemId}; Text: {Text}";
                return debugText;
            }
        }
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        public virtual string ItemId 
        {
            get
            {
                if (_ItemId == null) _ItemId = Guid.NewGuid().ToString();
                return _ItemId;
            }
            set { _ItemId = value; }
        }
        /// <summary>
        /// Reálně uložené ID
        /// </summary>
        protected string _ItemId;
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        public virtual string Text { get; set; }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public virtual int ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje true tehdy, když před prvkem má být oddělovač
        /// </summary>
        public virtual bool ItemIsFirstInGroup { get; set; }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        public virtual bool Enabled { get; set; }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Menu / Ribbonu (uživatelem) je do této property setována aktuální hodnota z Menu / Ribbonu,
        /// a poté je vyvolána odpovídající událost ItemClick.
        /// Zadaná hodnota může být null (pak ikona je <see cref="Image"/>), pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat)
        /// </summary>
        public virtual bool? Checked { get; set; }
        /// <summary>
        /// Jméno běžné ikony.
        /// Pro prvek typu CheckBox tato ikona reprezentuje stav, kdy <see cref="Checked"/> = NULL.
        /// </summary>
        public virtual string Image { get; set; }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        public virtual string ImageUnChecked { get; set; }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        public virtual string ImageChecked { get; set; }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        public virtual BarItemPaintStyle ItemPaintStyle { get; set; }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se <see cref="Text"/>.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public virtual string ToolTipIcon { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Titulek ToolTipu (pokud není zadán explicitně) se přebírá z textu prvku
        /// </summary>
        string IToolTipItem.ToolTipTitle { get { return ToolTipTitle ?? Text; } }
    }
    /// <summary>
    /// Definice jednoduchého prvku, který nese ID, text, ikony, tooltip a Tag
    /// </summary>
    public interface ITextItem : IToolTipItem
    {
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        string ItemId { get; }
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje tre tehdy, když před prvkem má být oddělovač
        /// </summary>
        bool ItemIsFirstInGroup { get; }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        bool Enabled { get; }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Ribbonu (uživatelem) je do této property setována aktuální hodnota z Ribbonu 
        /// a poté je vyvolána událost <see cref="DxRibbonControl.RibbonItemClick"/>.
        /// Hodnota může být null, pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat).
        /// <para/>
        /// Pokud konkrétní prvek nepodporuje null, akceptuje null jako false.
        /// </summary>
        bool? Checked { get; set; }
        /// <summary>
        /// Jméno ikony.
        /// Pro prvek typu CheckBox tato ikona reprezentuje stav, kdy <see cref="Checked"/> = NULL.
        /// </summary>
        string Image { get; }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ImageUnChecked { get; }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ImageChecked { get; }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        BarItemPaintStyle ItemPaintStyle { get; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Interface definující vlastnosti prvku, který může nabídnout ToolTip
    /// </summary>
    public interface IToolTipItem
    {
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        string ToolTipText { get; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        string ToolTipTitle { get; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        string ToolTipIcon { get; }
    }
    #endregion
    #region Enumy: LabelStyleType, RectangleSide, RectangleCorner
    /// <summary>
    /// Druh položky menu
    /// </summary>
    public enum MenuItemType
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None,
        /// <summary>
        /// Položka menu
        /// </summary>
        MenuItem,
        /// <summary>
        /// CheckBox
        /// </summary>
        CheckBox
    }
    /// <summary>
    /// Styl použitý pro Label
    /// </summary>
    public enum LabelStyleType
    {
        /// <summary>
        /// Běžný label u jednotlivých input prvků
        /// </summary>
        Default,
        /// <summary>
        /// Titulkový label menší, typicky grupa
        /// </summary>
        SubTitle,
        /// <summary>
        /// Titulkový label větší, jeden na formuláři
        /// </summary>
        MainTitle,
        /// <summary>
        /// Dodatková informace
        /// </summary>
        Info
    }
    /// <summary>
    /// Vyjádření názvu hrany na objektu Rectangle (Horní, Vpravo, Dolní, Vlevo).
    /// Enum povoluje sčítání hodnot, ale různé funkce nemusejí sečtené hodnoty akceptovat (z důvodu jejich logiky).
    /// </summary>
    [Flags]
    public enum RectangleSide
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo svislá na ose X</summary>
        Left = 0x01,
        /// <summary>Střed na ose X</summary>
        CenterX = 0x02,
        /// <summary>Vpravo svislá na ose X</summary>
        Right = 0x04,
        /// <summary>Horní vodorovná na ose Y</summary>
        Top = 0x10,
        /// <summary>Střed na ose Y</summary>
        CenterY = 0x20,
        /// <summary>Dolní vodorovná na ose Y</summary>
        Bottom = 0x40,
        /// <summary>Vodorovné = Top + Bottom</summary>
        Horizontal = Top | Bottom,
        /// <summary>Svislé = Left + Right</summary>
        Vertical = Left | Right,
        /// <summary>
        /// Prostřední bod
        /// </summary>
        Center = CenterX | CenterY,
        /// <summary>
        /// Horní levý bod
        /// </summary>
        TopLeft = Top | Left,
        /// <summary>
        /// Horní prostřední bod
        /// </summary>
        TopCenter = Top | CenterX,
        /// <summary>
        /// Horní pravý bod
        /// </summary>
        TopRight = Top | Right,
        /// <summary>
        /// Střední levý bod
        /// </summary>
        MiddleLeft = CenterY | Left,
        /// <summary>
        /// Úplně střední bod (X i Y)
        /// </summary>
        MiddleCenter = CenterY | CenterX,
        /// <summary>
        /// Střední pravý bod
        /// </summary>
        MiddleRight = CenterY | Right,
        /// <summary>
        /// Dolní levý bod
        /// </summary>
        BottomLeft = Bottom | Left,
        /// <summary>
        /// Dolní prostřední bod
        /// </summary>
        BottomCenter = Bottom | CenterX,
        /// <summary>
        /// Dolní pravý roh
        /// </summary>
        BottomRight = Bottom | Right,
        /// <summary>Všechny</summary>
        All = Left | Top | Right | Bottom
    }
    /// <summary>
    /// Vyjádření názvu rohu na objektu Rectangle (Vlevo nahoře, Vpravo nahoře, ...)
    /// </summary>
    public enum RectangleCorner
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo nahoře</summary>
        LeftTop,
        /// <summary>Vpravo nahoře</summary>
        TopRight,
        /// <summary>Vpravo dole</summary>
        RightBottom,
        /// <summary>Vlevo dole</summary>
        BottomLeft
    }
    /// <summary>
    /// Jaké hodnoty zachovat při změně obsahu dat
    /// </summary>
    [Flags]
    public enum PreservePropertiesMode
    {
        /// <summary>
        /// Nic nezachovat, vše bude resetováno do výchozího stavu
        /// </summary>
        None = 0,
        /// <summary>
        /// Zachovat první viditelný prvek (řádek, sloupec)
        /// </summary>
        FirstVisibleItem = 0x0001,
        /// <summary>
        /// Zachovat první viditelný pixel
        /// </summary>
        FirstVisiblePixel = 0x0002,
        /// <summary>
        /// Zachovat stav vybraných prvků
        /// </summary>
        SelectedItems = 0x0010
    }
    /// <summary>
    /// Viditelnost některého prvku v rámci parenta s ohledem na interaktivitu
    /// </summary>
    [Flags]
    public enum DxChildControlVisibility
    {
        /// <summary>
        /// Prvek není vidět nikdy
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek je vidět tehdy, když parent má na sobě myš
        /// </summary>
        OnMouse = 0x0001,
        /// <summary>
        /// Prvek je vidět tehdy, když parent má v sobě klávesový focus (kurzor)
        /// </summary>
        OnFocus = 0x0002,
        /// <summary>
        /// Prvek je vidět vždy
        /// </summary>
        Allways = 0x0004,

        /// <summary>
        /// Prvek je vidět pod myší anebo s focusem
        /// </summary>
        OnActiveControl = OnMouse | OnFocus
    }

    /// <summary>
    /// Klávesové akce
    /// </summary>
    [Flags]
    public enum KeyActionType
    {
        /// <summary>
        /// Žádná akce
        /// </summary>
        None = 0,
        /// <summary>
        /// Klávesa Delete = smazat výběr
        /// </summary>
        Delete = 0x0001,
        /// <summary>
        /// Klávesa CtrlA = vybrat vše
        /// </summary>
        CtrlA = 0x0010,
        /// <summary>
        /// Klávesa CtrlC = zkopírovat
        /// </summary>
        CtrlC = 0x0020,
        /// <summary>
        /// Klávesa CtrlX = vyjmout
        /// </summary>
        CtrlX = 0x0040,
        /// <summary>
        /// Klávesa CtrlV = vložit
        /// </summary>
        CtrlV = 0x0080,

        /// <summary>
        /// Klávesa AltUp (kurzor) = přemístit o jednu pozici nahoru
        /// </summary>
        AltUp = 0x0100,
        /// <summary>
        /// Klávesa AltDown (kurzor) = přemístit o jednu pozici dolů
        /// </summary>
        AltDown = 0x0200,

        /// <summary>
        /// Všechny akce
        /// </summary>
        All = Delete | CtrlA | CtrlC | CtrlX | CtrlV | AltUp
    }
    #endregion
}
