﻿// Supervisor: David Janáček, od 01.02.2021
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

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Drawing.Drawing2D;
using DevExpress.Pdf.Native;
using DevExpress.XtraPdfViewer;
using DevExpress.XtraEditors;
using DevExpress.Office.History;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxStdForm
    public class DxStdForm : DevExpress.XtraEditors.XtraForm
    {
        public DxStdForm()
        {
            this.IconOptions.SvgImage = DxComponent.GetSvgImage(DxComponent.ImageNameFormIcon);
        }
    }
    #endregion
    #region DxRibbonForm
    /// <summary>
    /// Formulář s ribbonem
    /// </summary>
    public class DxRibbonForm : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public DxRibbonForm()
        {
            this.IconOptions.SvgImage = DxComponent.GetSvgImage(DxComponent.ImageNameFormIcon);
        }
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
            this.Padding = new Padding(10);
            this.SetStyle(ControlStyles.UserPaint, true);
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
        /// V této metodě budou controly zobrazeny bez blikání = ještě dříve, než se Panel naroluje na novou souřadnici
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
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
    public class DxPanelControl : DevExpress.XtraEditors.PanelControl, IListenerZoomChange, IListenerStyleChanged
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxPanelControl()
        {
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
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
        #endregion
        #region Paint
        /// <summary>
        /// Základní kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintBackColorUser(e);
        }
        /// <summary>
        /// Overlay kreslení BackColorUser
        /// </summary>
        /// <param name="e"></param>
        protected void PaintBackColorUser(PaintEventArgs e)
        {
            var backColorUser = BackColorUser;
            if (!backColorUser.HasValue) return;
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColorUser.Value), this.ClientRectangle);
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
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
            this.ItemOrientation = Orientation.Horizontal;                       // Vertical = kreslí řadu záhlaví vodorovně, ale obsah jednotlivého buttonu svisle :-(

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
        None = 0,

        Fast = 0x0001,
        Medium = 0x0002,
        Slow = 0x0004,
        VerySlow = 0x0008,

        Fade = 0x0100,
        Slide = 0x0200,
        Push = 0x0400,
        Shape = 0x0800,

        FadeFast = Fade | Fast,
        FadeMedium = Fade | Medium,
        FadeSlow = Fade | Slow,

        SlideFast = Slide | Fast,
        SlideMedium = Slide | Medium,
        SlideSlow = Slide | Slow,

        PushFast = Push | Fast,
        PushMedium = Push | Medium,
        PushSlow = Push | Slow,

        ShapeFast = Shape | Fast,
        ShapeMedium = Shape | Medium,
        ShapeSlow = Shape | Slow,

        Default = FadeFast,

        AllTimes = Fast | Medium | Slow | VerySlow,
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DxLabelControl
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxListBoxControl
    /// <summary>
    /// ListBoxControl
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.ListBoxControl, IDxDragDropControl, IDxDragDropTarget
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            PrepareDxDragDrop();
            ReorderInit();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DragAndDropDispose();
        }
        /// <summary>
        /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        public event PaintEventHandler PaintList;
        /// <summary>
        /// Pole, obsahující informace o právě viditelných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, object, Rectangle>[] VisibleItems
        {
            get
            {
                List<Tuple<int, object, Rectangle>> visibleItems = new List<Tuple<int, object, Rectangle>>();
                int topIndex = this.TopIndex;
                int index = (topIndex > 0 ? topIndex - 1 : topIndex);
                int count = this.ItemCount;
                while (index < count)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    if (bounds.HasValue)
                        visibleItems.Add(new Tuple<int, object, Rectangle>(index, this.Items[index], bounds.Value));
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
        public Tuple<int, object, Rectangle?>[] SelectedItemsInfo
        {
            get
            {
                List<Tuple<int, object, Rectangle?>> selectedItems = new List<Tuple<int, object, Rectangle?>>();
                foreach (var index in this.SelectedIndices)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    selectedItems.Add(new Tuple<int, object, Rectangle?>(index, this.Items[index], bounds));
                }
                return selectedItems.ToArray();
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region Overrides
        /// <summary>
        /// Při vykreslování
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintList?.Invoke(this, e);
            this.PaintOnMouseItem(e);
        }
        /// <summary>
        /// Po stisku klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseItemIndex = -1;
        }
        #endregion
        #region Přesouvání prvků pomocí myši
        /// <summary>
        /// Umožní this controlu být zdrojem procesu Drag and Drop = odsud je možno vzít prvky a přesunout je jinam.
        /// </summary>
        public bool AllowDrag { get { return this._AllowDrag; } set { this._AllowDrag = value; PrepareDxDragDrop(); } } private bool _AllowDrag = true;
        /// <summary>
        /// Umožní this controlu být cílem procesu Drag and Drop.
        /// </summary>
        public override bool AllowDrop { get { return base.AllowDrop; } set { base.AllowDrop = value; PrepareDxDragDrop(); } }
        /// <summary>
        /// Inicializace controlleru Drag and Drop
        /// </summary>
        private void PrepareDxDragDrop()
        {
            if ((this.AllowDrag || this.AllowDrop) && _DxDragDrop == null)
                _DxDragDrop = new DxDragDrop(this);





            // SysDragInit();
        }
        /// <summary>
        /// Dispose controlleru Drag and Drop
        /// </summary>
        private void DragAndDropDispose()
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

            return;


            DragDropSearchSource(args.SourceMouseLocation);
            if (args.State == DxDragDropState.DragStart)
                SysDragStart();
        }
        void IDxDragDropControl.DoDragTarget(DxDragDropArgs args)
        { }
        /// <summary>
        /// Metoda volaná do objektu Target (cíl Drag and Drop) při každé akci, pokud se myš nachází nad objektem který implementuje <see cref="IDxDragDropTarget"/>.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void IDxDragDropTarget.DoDragTarget(DxDragDropArgs args)
        {
            Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
            int index = this.IndexFromPoint(targetPoint);
            var bounds = this.GetItemBounds(index);
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
        #region ReorderItems, obsolete
        protected void ReorderInit()
        {
            ReorderByDragEnabled = false;
            ReorderIconColor = Color.FromArgb(192, 116, 116, 96);
            ReorderIconColorHot = Color.FromArgb(220, 160, 160, 122);
        }
        /// <summary>
        /// Povolení pro reorder prvků pomocí myši (pak se zobrazí ikona pro drag)
        /// </summary>
        public bool ReorderByDragEnabled { get; set; }
        /// <summary>
        /// Barva ikonky pro reorder <see cref="ReorderByDragEnabled"/>, v běžném stavu.
        /// Průhlednost je podporována.
        /// </summary>
        public Color ReorderIconColor { get; set; }
        /// <summary>
        /// Barva ikonky pro reorder <see cref="ReorderByDragEnabled"/>, v běžném stavu.
        /// Průhlednost je podporována.
        /// </summary>
        public Color ReorderIconColorHot { get; set; }
        /// <summary>
        /// Zajistí vykreslení ikony pro ReorderByDrag a případně i přemisťovaného prvku
        /// </summary>
        /// <param name="e"></param>
        private void PaintOnMouseItem(PaintEventArgs e)
        {
            PaintReorderIcon(e);
        }
        /// <summary>
        /// Metoda vyhledá zdrojový prvek pro případný přesun Drag and Drop pro danou souřadnici myši (v lokálním souřadném systému).
        /// Dále určí a uloží hodnoty: <see cref="OnMouseItemIndex"/>, <see cref="OnMouseItemAlphaRatio"/>, <see cref="_IsMouseOverReorderIcon"/>,
        /// a v případě potřeby nastaví aktuální kurzor.
        /// </summary>
        /// <param name="sourcePoint"></param>
        private void DragDropSearchSource(Point? sourcePoint)
        {
            // Najdeme index prvku pod myší, a určíme Alpha ratio pro ikonu vpravo, podle pozice myši vodorovně v řádku:
            int index = -1;
            float ratio = 0f;
            if (sourcePoint.HasValue)
            {
                index = this.IndexFromPoint(sourcePoint.Value);
                float ratioX = (float)sourcePoint.Value.X / (float)this.Width;           // Relativní pozice myši na ose X v rámci celého controlu v rozmezí 0.0 až 1.0
                ratio = 0.30f + (ratioX < 0.5f ? 0f : 1.40f * (ratioX - 0.5f));          // 0.30 je pevná dolní hodnota. Ta platí pro pozici myši 0.0 až 0.5. Pak se ratio zvyšuje od 0.30 do 1.0.
            }
            OnMouseItemIndex = index;
            OnMouseItemAlphaRatio = ratio;

            // Vyřešíme druh kurzoru - pokud se myš nachází v prostoru ikony a pokud je povoleno pomocí myši přesouvat prvky, pak dáme kurzor typu Nahoru-Dolů:
            bool isCursorActive = false;
            if (sourcePoint.HasValue)
            {
                var iconBounds = OnMouseIconBounds;
                isCursorActive = (iconBounds.HasValue && iconBounds.Value.Contains(sourcePoint.Value));
            }

            if (isCursorActive != _IsMouseOverReorderIcon)
            {
                _IsMouseOverReorderIcon = isCursorActive;
                if (ReorderByDragEnabled)
                {
                    this.Cursor = (isCursorActive ? Cursors.SizeNS : Cursors.Default);
                    this.Invalidate();                                                   // Překreslení => jiná barva ikony
                }
            }
        }
        /// <summary>
        /// Vykreslí ikonu pro přesouvání prvku
        /// </summary>
        /// <param name="e"></param>
        protected void PaintReorderIcon(PaintEventArgs e)
        {
            if (!ReorderByDragEnabled) return;

            Rectangle? iconBounds = OnMouseIconBounds;
            if (!iconBounds.HasValue) return;

            int wb = iconBounds.Value.Width;
            int x0 = iconBounds.Value.X;
            int yc = iconBounds.Value.Y + iconBounds.Value.Height / 2;
            Color iconColor = (_IsMouseOverReorderIcon ? ReorderIconColorHot : ReorderIconColor);
            float alphaRatio = OnMouseItemAlphaRatio;
            int alpha = (int)((float)iconColor.A * alphaRatio);
            iconColor = Color.FromArgb(alpha, iconColor);
            using (SolidBrush brush = new SolidBrush(iconColor))
            {
                e.Graphics.FillRectangle(brush, x0, yc - 6, wb, 2);
                e.Graphics.FillRectangle(brush, x0, yc - 1, wb, 2);
                e.Graphics.FillRectangle(brush, x0, yc + 4, wb, 2);
            }
        }
        /// <summary>
        /// Příznak, že myš je nad ikonou pro přesouvání prvku, a kurzor je tedy upraven na šipku nahoru/dolů.
        /// Pak tedy MouseDown a MouseMove bude provádět Reorder prvku.
        /// </summary>
        private bool _IsMouseOverReorderIcon = false;
        /// <summary>
        /// Blízkost myši k ikoně na ose X v poměru: 0.15 = úplně daleko, 1.00 = přímo na ikoně.
        /// Ovlivní průhlednost barvy (kanál Alpha).
        /// </summary>
        public float OnMouseItemAlphaRatio
        {
            get { return _OnMouseItemAlphaRatio; }
            protected set
            {
                float ratio = (value < 0.15f ? 0.15f : (value > 1.0f ? 1.0f : value));
                float diff = ratio - _OnMouseItemAlphaRatio;
                if (diff <= -0.04f || diff >= 0.04f)
                {
                    _OnMouseItemAlphaRatio = ratio;
                    this.Invalidate();
                }
            }
        }
        private float _OnMouseItemAlphaRatio = 0.15f;
        /// <summary>
        /// Souřadnice prostoru pro myší ikonu vpravo v myšoaktivním řádku.
        /// Souřadnice se vypočítají pro prvek na indexu <see cref="_OnMouseItemIndex"/>.
        /// </summary>
        protected Rectangle? OnMouseIconBounds
        {
            get
            {
                if (_OnMouseIconBoundsIndex != _OnMouseItemIndex)
                {
                    _OnMouseIconBounds = GetOnMouseIconBounds(_OnMouseItemIndex);
                    _OnMouseIconBoundsIndex = _OnMouseItemIndex;
                }
                return _OnMouseIconBounds;
            }
        }
        /// <summary>
        /// Souřadnice prostoru ikony vpravo pro myš
        /// </summary>
        private Rectangle? _OnMouseIconBounds = null;
        /// <summary>
        /// Index prvku, pro který je vypočtena souřadnice a uložena v <see cref="_OnMouseIconBounds"/>
        /// </summary>
        private int _OnMouseIconBoundsIndex = -1;

        #endregion

        protected void SysDragInit()
        {
            this.AllowDrop = true;


        }

        protected void SysDragStart()
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}SysDragStart(){TAB}START{TAB}DoDragDrop(){MousePoint}");
            CreateDragForm();
            var result = this.DoDragDrop(this, DragDropEffects.All | DragDropEffects.Link);
            RemoveDragForm();
            DxComponent.LogAddLine($"{TypeName}{TAB}DoDragDrop(){TAB}END{TAB}result: {result}{MousePoint}");

            bool isCopy = (result == DragDropEffects.Copy);
        }
        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}OnDragEnter(){TAB}Effect: {drgevent.Effect}{MousePoint}");
            base.OnDragEnter(drgevent);
            drgevent.Effect = DragDropEffects.Copy;
        }
        protected override void OnDragOver(DragEventArgs drgevent)
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}OnDragOver(){TAB}Effect: {drgevent.Effect}{MousePoint}");
            base.OnDragOver(drgevent);
            drgevent.Effect = DragDropEffects.Copy;
        }
        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}OnQueryContinueDrag(){TAB}Action: {qcdevent.Action}{MousePoint}");
            base.OnQueryContinueDrag(qcdevent);
            MoveDragForm(LastDragEffect);

            // qcdevent.Action = DragAction.Continue;

            // qcdevent.Action = DragAction.Drop;
            // this.RaiseDragEvent(null, new DragEventArgs())
        }
        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}OnDragDrop(){TAB}Effect: {drgevent.Effect}{MousePoint}");
            base.OnDragDrop(drgevent);

        }
        protected override void OnDragLeave(EventArgs e)
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}OnDragLeave(){TAB}{MousePoint}");
            base.OnDragLeave(e);
        }
        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            DxComponent.LogAddLine($"{TypeName}{TAB}OnGiveFeedback(){TAB}Effect: {gfbevent.Effect}{MousePoint}");
            base.OnGiveFeedback(gfbevent);
            LastDragEffect = gfbevent.Effect;
        }
        protected DragDropEffects LastDragEffect;
        protected string TAB { get { return "\t"; } }
        protected string TypeName { get { return $"{this.GetType().Name} '{this.Name}'"; } }
        protected string MousePoint { get { var ap = Control.MousePosition; var lp = this.PointToClient(ap); return $"{TAB}Mouse Abs: {ap.X},{ap.Y};{TAB}Loc: {lp.X},{lp.Y}"; } }

        protected void CreateDragForm()
        {
            Form form = new Form()
            {
                FormBorderStyle = FormBorderStyle.None,
                AllowTransparency = true,
                TransparencyKey = Color.Magenta,
                BackColor = Color.Magenta,
                Size = new Size(250, 50),
                Text = "",
                StartPosition = FormStartPosition.Manual,
                Location = DragFormLocation,
                ShowInTaskbar = false,
                TopLevel = true
            };
            var label = new DxLabelControl() { Bounds = new Rectangle(0, 0, 250, 35), Text = "<b>PŘESOUVÁME</b> položky:\r\n<i>1: první,\r\n2: druhá,\r\n3: třetí,\r\n4: pátá,</i>...", AllowHtmlString = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            label.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
            label.Padding = new Padding(3);
            label.Height = 50;
            label.Appearance.GradientMode = LinearGradientMode.Vertical;
            label.Appearance.Options.UseForeColor = true;
            label.Appearance.Options.UseBackColor = true;

            label.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            label.Appearance.TextOptions.HAlignment = HorzAlignment.Near;
            label.Appearance.Options.UseTextOptions = true;
            label.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;

            var optimalSize = label.CalcBestSize();
            if (optimalSize.Width > 500) optimalSize.Width = 500;
            if (optimalSize.Height > 150) optimalSize.Height = 150;
            optimalSize.Width += 10;
            optimalSize.Height += 10;
            label.Size = optimalSize;
            optimalSize.Width += 1;
            optimalSize.Height += 1;
            form.Size = optimalSize;


            form.Controls.Add(label);

            DragLabel = label;
            DragForm = form;

            MoveDragSetEnabled(true);

            form.Show();
        }
        protected void MoveDragForm(DragDropEffects lastDragEffect)
        {
            var form = DragForm;
            if (form == null) return;
            form.Location = DragFormLocation;

            bool enabled = (lastDragEffect != DragDropEffects.None);
            if (enabled != DragLabelEnabled)
                MoveDragSetEnabled(enabled);
        }
        protected void MoveDragSetEnabled(bool enabled)
        {
            var label = DragLabel;
            if (label != null)
            {
                label.Appearance.ForeColor = (enabled ? Color.Black : Color.DimGray);
                label.Appearance.BackColor = (enabled ? Color.FromArgb(250, 250, 240) : Color.LightGray);
                label.Appearance.BackColor2 = (enabled ? Color.FromArgb(245, 245, 225) : Color.LightGray);
            }
            DragLabelEnabled = enabled;
        }
        protected void RemoveDragForm()
        {
            var form = DragForm;
            if (form == null) return;
            DragLabel = null;
            form.Hide();
            form.Close();
            form.Dispose();
            DragForm = null;
        }
        protected Point DragFormLocation
        {
            get
            {
                var point = Control.MousePosition;
                point.X -= 25;
                point.Y += 25;
                return point;
            }
        }
        protected bool DragLabelEnabled;
        protected Form DragForm;
        protected DxLabelControl DragLabel;


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
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
        #region Resize a SvgImage

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

            _ListBox = DxComponent.CreateDxListBox(DockStyle.None, parent: this, selectionMode: SelectionMode.MultiExtended, itemHeight: 32,
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
        private void _FilterText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                _ListBox.Focus();
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
            { }
            else if (e.Modifiers == Keys.Control)
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
        private void _ListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.C)) _DoCopyClipboard();
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
        private void _ListBox_PaintList(object sender, PaintEventArgs e)
        {
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = DxComponent.GetSvgPalette();
            var visibleItems = _ListBox.VisibleItems;
            foreach (var visibleItem in visibleItems)
            {
                string resourceName = visibleItem.Item2 as string;
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
            _FilteredItemsCount = resources.Length;

            _ListBox.SuspendLayout();
            _ListBox.Items.Clear();
            _ListBox.Items.AddRange(resources);
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
                    string resourceName = selectedItem.Item2 as string;
                    sb.AppendLine($"  string resource{_ClipboardCopyIndex} = \"{resourceName}\";");
                }
                if (sb.Length > 0)
                {
                    Clipboard.Clear();
                    Clipboard.SetText(sb.ToString());
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
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
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
            return (result == DialogResult.OK);
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
    #region Enumy: LabelStyleType, RectangleSide, RectangleCorner
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
    #endregion
}