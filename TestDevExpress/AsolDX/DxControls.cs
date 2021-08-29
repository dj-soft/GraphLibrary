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
            this._LastDpi = 0;

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
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public virtual bool LogActive { get; set; }
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
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); DeviceDpiCheck(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); DeviceDpiCheck(); }
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
        protected void DeviceDpiCheck()
        {
            if (this.FindForm() == null) return;
            var currentDpi = _ReloadCurrentDpi();
            if (_DpiChanged)
            {
                OnDpiChanged();
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
    #region DxScrollableContent a DxScrollBars
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
            _ContentVisualSize = this.ClientSize;
            _VScrollBarVisible = false;
            _HScrollBarVisible = false;

            _VScrollBar = new DxVScrollBar() { Visible = false, Minimum = 0, SmallChange = 40 };
            _VScrollBar.ValueChanged += ScrollBar_ValueChanged;
            _VScrollBar.MouseWheel += _VScrollBar_MouseWheel;
            Controls.Add(_VScrollBar);

            _HScrollBar = new DxHScrollBar() { Visible = false, Minimum = 0, SmallChange = 80 };
            _HScrollBar.ValueChanged += ScrollBar_ValueChanged;
            _HScrollBar.MouseWheel += _HScrollBar_MouseWheel;
            Controls.Add(_HScrollBar);

            ScrollToBoundsBasicPadding = new Padding(3);
            ScrollToBoundsScrollPadding = new Padding(24);

            this.ParentChanged += _ParentChanged;
            this.DpiChangedAfterParent += _DpiChangedAfterParent;
            this.Invalidated += _Invalidated;
        }
        /// <summary>
        /// Po změně Parenta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ParentChanged(object sender, EventArgs e)
        {
            DeviceDpiCheck();
        }
        /// <summary>
        /// Po změně DPI (neuhlídá ale všechny situace)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DpiChangedAfterParent(object sender, EventArgs e)
        {
            DeviceDpiCheck();
        }
        /// <summary>
        /// Po Invalidaci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Invalidated(object sender, SWF.InvalidateEventArgs e)
        {
            DeviceDpiCheck();
        }
        private SWF.Control _ContentControl;
        private DxVScrollBar _VScrollBar;
        private bool _VScrollBarVisible;
        private DxHScrollBar _HScrollBar;
        private bool _HScrollBarVisible;
        private Size _ContentTotalSize;
        private Rectangle _ContentVirtualBounds;
        private Size _ContentVisualSize;
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public override bool LogActive
        {
            get { return base.LogActive; }
            set
            {
                base.LogActive = value;
                if (_ContentControl != null && _ContentControl is DxPanelControl dxPanel)
                    dxPanel.LogActive = value;
            }
        }
        #endregion
        #region Public vlastnosti - ContentPanel, Size, ContentVirtualBounds...
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
        /// Obsahuje true, pokud je viditelný Horizontální (=vodorovný) ScrollBar
        /// </summary>
        public bool HScrollBarVisible{ get { return _HScrollBarVisible; } }
        /// <summary>
        /// Obsahuje true, pokud je viditelný Vertikální (=svislý) ScrollBar
        /// </summary>
        public bool VScrollBarVisible { get { return _VScrollBarVisible; } }
        #endregion
        #region Layout a řízení ScrollBarů
        /// <summary>
        /// Na základě aktuálních fyzických rozměrů a podle <see cref="ContentTotalSize"/> určí potřebnou viditelnost ScrollBarů,
        /// určí souřadnice prvků (Content i ScrollBary), určí vlastnosti pro ScrollBary a velikost prosotru pro vlastní obsah (<see cref="_ContentVisualSize"/>).
        /// Pokud dojde k jakékoli změně, vyvolá jedenkrát událost <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        protected void DoLayoutContent()
        {
            Size clientSize = this.ClientSize;
            if (this.Parent == null)
            {
                _ContentVisualSize = clientSize;
                return;
            }

            Size contentTotalSize = this.ContentTotalSize;
            int clientWidth = clientSize.Width;
            int clientHeight = clientSize.Height;

            // Vertikální (svislý) ScrollBar: bude viditelný, když výška obsahu je větší než výška klienta, a zmenší šířku klienta:
            bool vVisible = (contentTotalSize.Height > clientHeight);
            int vScrollSize = (vVisible ? _VScrollBar.GetDefaultVerticalScrollBarWidth() : 0);
            if (vVisible) clientWidth -= vScrollSize;

            // Horizontální (vodorovný) ScrollBar: bude viditelný, když šířka obsahu je větší než šířka klienta, a zmenší výšku klienta:
            bool hVisible = (contentTotalSize.Width > clientWidth);
            int hScrollSize = (hVisible ? _VScrollBar.GetDefaultHorizontalScrollBarHeight() : 0);
            if (hVisible) clientHeight -= hScrollSize;

            // Pokud dosud nebyl viditelný Vertikální (svislý) ScrollBar, ale je viditelný Horizontální (vodorovný) ScrollBar:
            //  pak Horizontální ScrollBar zmenšil výšku obsahu (clientHeight), a může se stát, že bude třeba zobrazit i Vertikální ScrollBar:
            if (!vVisible && hVisible && (contentTotalSize.Height > clientHeight))
            {
                vVisible = true;
                vScrollSize = _VScrollBar.GetDefaultVerticalScrollBarWidth();
                clientWidth -= vScrollSize;
            }

            // Pokud je přílš malá šířka a je viditelný Vertikální (svislý) ScrollBar: vrátit plnou šířku a zrušit scrollBar:
            if (clientWidth < 10 && vVisible)
            {
                clientWidth = clientSize.Width;
                vVisible = false;
                vScrollSize = 0;
            }
            // Pokud je přílš malá výška a je viditelný Horizontální (vodorovný) ScrollBar: vrátit plnou výšku a zrušit scrollBar:
            if (clientHeight < 10 && hVisible)
            {
                clientHeight = clientSize.Height;
                hVisible = false;
                hScrollSize = 0;
            }

            // bool reCalcVirtualBounds = (clientWidth != contentVirtualBounds.Width || clientHeight != contentVirtualBounds.Height);

            _ContentControl?.SetBounds(new Rectangle(0, 0, clientWidth, clientHeight));
            _ContentVisualSize = new Size(clientWidth, clientHeight);
            _VScrollBarVisible = vVisible;
            _HScrollBarVisible = hVisible;

            bool suppressEvent = _SuppressEvent;
            try
            {
                _SuppressEvent = true;

                if (vVisible)
                {
                    _VScrollBar.SetBounds(new Rectangle(clientWidth, 0, vScrollSize, clientHeight));
                    _VScrollBar.Maximum = contentTotalSize.Height;
                    _VScrollBar.LargeChange = clientHeight;
                }
                if (hVisible)
                {
                    _HScrollBar.SetBounds(new Rectangle(0, clientHeight, clientWidth, hScrollSize));
                    _HScrollBar.Maximum = contentTotalSize.Width;
                    _HScrollBar.LargeChange = clientWidth;
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
            ApplyScrollBarToVirtualLocation();
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
            this.DoLayoutContent();
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
        public bool ScrollToBounds(Rectangle controlVirtualBounds, Rectangle? groupVirtualBounds = null, bool skipEvent = false)
        {
            Size totalSize = this.ContentTotalSize;
            Rectangle currentBounds = this.ContentVirtualBounds;
            Rectangle controlBounds = controlVirtualBounds.Add(ScrollToBoundsBasicPaddingCurrent);
            if (currentBounds.Contains(controlBounds)) return false;           // Požadovaný prostor je zcela vidět

            // Budeme muset Scrollovat:
            Point oldVirtualOrigin = currentBounds.Location;
            bool suppressEvent = _SuppressEvent;
            try
            {
                _SuppressEvent = true;

                if (groupVirtualBounds.HasValue)
                { }

                controlBounds = controlVirtualBounds.Add(ScrollToBoundsScrollPaddingCurrent);
                ScrollToBounds(controlBounds.X, controlBounds.Right, currentBounds.X, currentBounds.Right, totalSize.Width, HScrollBarVisible, _HScrollBar);
                ScrollToBounds(controlBounds.Y, controlBounds.Bottom, currentBounds.Y, currentBounds.Bottom, totalSize.Height, VScrollBarVisible, _VScrollBar);
            }
            finally
            {
                _SuppressEvent = suppressEvent;
            }

            Point newVirtualOrigin = this.ContentVirtualBounds.Location;
            if (newVirtualOrigin == oldVirtualOrigin) return false;

            if (!skipEvent)
                _RunContentVirtualBoundsChanged();

            return true;
        }
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
        /// Okraje, přidávané k požadovaném prostoru controlu v metodě <see cref="ScrollToBounds(Rectangle, Rectangle?)"/> před tím, než se ověří jeho aktuální viditelnost.
        /// Tyto okraje "zvětšují" control, tak aby se Scroll provedl i tehdy, když vlastní control sice je vidět, ale je těsně na okraji viditelného prostoru.
        /// <para/>
        /// Výchozí hodnota = 3 pixely.
        /// Jde o designové pixely = bez aplikování odlišného DPI a Zoomu, ty se aplikují interně.
        /// </summary>
        public Padding ScrollToBoundsBasicPadding { get; set; }
        /// <summary>
        /// Aktuální hodnota <see cref="ScrollToBoundsBasicPadding"/> (pro aktuální Zoom a DPI)
        /// </summary>
        protected Padding ScrollToBoundsBasicPaddingCurrent { get { return DxComponent.ZoomToGuiInt(ScrollToBoundsBasicPadding, CurrentDpi); } }
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
        protected Padding ScrollToBoundsScrollPaddingCurrent { get { return DxComponent.ZoomToGuiInt(ScrollToBoundsScrollPadding, CurrentDpi); } }
        #endregion
        #region Výpočty virtuální souřadnice a reakce na interaktivní posuny
        /// <summary>
        /// Nastaví počáteční souřadnici virtuálního prostoru podle daného bodu, před tím provede veškeré kontroly, při změně reálné hodnoty vyvolá událost
        /// </summary>
        /// <param name="virtualLocation"></param>
        protected void SetVirtualLocation(Point virtualLocation)
        {
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

            // Scrollbary a jejich hodnota:
            // (změna souřadnice Location nemění šířku - a to ani vizuální, ani celkovou, proto nemění Visible ScrollBarů ani jejich maximum a LargeChange).
            bool hv = _HScrollBarVisible;
            bool vv = _VScrollBarVisible;
            int sx = (hv ? _HScrollBar.Value : 0);
            int sy = (vv ? _VScrollBar.Value : 0);
            bool changeX = (hv && x != sx);
            bool changeY = (vv && y != sy);
            if (changeX || changeY)
            {
                bool suppressEvent = _SuppressEvent;
                try
                {
                    _SuppressEvent = true;
                    if (changeX) _HScrollBar.Value = x;
                    if (changeY) _VScrollBar.Value = y;
                }
                finally
                {
                    _SuppressEvent = suppressEvent;
                }
            }

            this.ContentVirtualBounds = new Rectangle(x, y, vw, vh);
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
        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            ApplyScrollBarToVirtualLocation();
        }
        /// <summary>
        /// Na controlu <see cref="ContentControl"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentControl_MouseWheel(object sender, SWF.MouseEventArgs e)
        {
            DevExpress.XtraEditors.ScrollBarBase scrollBar = (_VScrollBarVisible ? (DevExpress.XtraEditors.ScrollBarBase)_VScrollBar : (_HScrollBarVisible ? (DevExpress.XtraEditors.ScrollBarBase)_HScrollBar : null));
            ContentControl_MouseWheel(scrollBar, e.Delta);
        }
        /// <summary>
        /// Na controlu <see cref="_VScrollBar"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VScrollBar_MouseWheel(object sender, SWF.MouseEventArgs e)
        {
            ContentControl_MouseWheel(_VScrollBar, e.Delta);
        }
        /// <summary>
        /// Na controlu <see cref="_HScrollBar"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HScrollBar_MouseWheel(object sender, SWF.MouseEventArgs e)
        {
            ContentControl_MouseWheel(_HScrollBar, e.Delta);
        }
        /// <summary>
        /// Na daném scrollbaru bylo otočeno myškou
        /// </summary>
        /// <param name="scrollBar"></param>
        /// <param name="delta"></param>
        private void ContentControl_MouseWheel(DevExpress.XtraEditors.ScrollBarBase scrollBar, int delta)
        {
            if (scrollBar == null) return;
            int diff = (delta < 0 ? 3 : (delta > 0 ? -3 : 0));
            int value = scrollBar.Value;
            int maxValue = scrollBar.Maximum - scrollBar.LargeChange + 1;
            int newValue = value + diff * scrollBar.SmallChange;
            newValue = (newValue < 0 ? 0 : (newValue > maxValue ? maxValue : newValue));
            if (newValue != value)
                scrollBar.Value = newValue;
        }
        /// <summary>
        /// Hodnoty ze ScrollBarů (pokud jsou viditelné) aplikuje do <see cref="SetVirtualLocation(Point)"/>
        /// </summary>
        private void ApplyScrollBarToVirtualLocation()
        {
            int x = (_HScrollBarVisible ? _HScrollBar.Value : 0);
            int y = (_VScrollBarVisible ? _VScrollBar.Value : 0);
            Point virtualLocation = new Point(x, y);
            SetVirtualLocation(virtualLocation);
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
            _ListBox.KeyUp += _ListBox_KeyUp;
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
        private void _ListBox_KeyUp(object sender, SWF.KeyEventArgs e)
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
