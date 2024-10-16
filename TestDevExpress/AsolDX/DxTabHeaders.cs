﻿// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors.ViewInfo;

using XS = Noris.WS.Parser.XmlSerializer;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region ITabHeaderControl : interface definující obecný control se záložkami
    /// <summary>
    /// Rozhraní na obecný TabHeader control
    /// </summary>
    public interface ITabHeaderControl
    {
        /// <summary>
        /// Pozice záhlaví a jeho vlastnosti
        /// </summary>
        DxPageHeaderPosition PageHeaderPosition { get; set; }
        /// <summary>
        /// Umístění záložek na začátek / střed / konec
        /// </summary>
        AlignContentToSide PageHeaderAlignment { get; set; }
        /// <summary>
        /// Možnost zobrazit více řádek záhlaví
        /// </summary>
        bool PageHeaderMultiLine { get; set; }
        /// <summary>
        /// Počet stránek
        /// </summary>
        int IPageCount { get; }
        /// <summary>
        /// Stránky datové.
        /// Lze i setovat. Při setování se control pokusí ponechat jako selectovanou stránku tu, která je aktuálně selectovaná, podle jejího <see cref="ITextItem.ItemId"/>.
        /// Pokud to nebude možné (v nové sadě stránek nebude takové ID existovat), pak vyselectuje první existující stránku a vyvolá událost <see cref="SelectedIPageChanged"/>
        /// </summary>
        IPageItem[] IPages { get; set; }
        /// <summary>
        /// Data aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        IPageItem SelectedIPage { get; set; }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        string SelectedIPageId { get; set; }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně NEPROBĚHNE událost <see cref="SelectedIPageChanging"/>, pouze proběhne <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        string SelectedIPageIdForce { get; set; }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně NEPROBĚHNE žádná událost, ani <see cref="SelectedIPageChanging"/>, ani <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        string SelectedIPageIdSilent { get; set; }
        /// <summary>
        /// Prostý index aktuálně vybrané stránky nebo -1 pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        int SelectedIPageIndex { get; set; }
        /// <summary>
        /// Volá se při pokusu o aktivaci jiné záložky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, nová stránka bude aktivována.
        /// </summary>
        event EventHandler<TEventCancelArgs<IPageItem>> SelectedIPageChanging;
        /// <summary>
        /// Událost volaná při změně aktivní stránky <see cref="SelectedIPage"/>
        /// </summary>
        event EventHandler<TEventArgs<IPageItem>> SelectedIPageChanged;
        /// <summary>
        /// Zjistí, zda na zadané relativní souřadnici se nachází nějaké záhlaví, a pokud ano pak najde odpovídající stránku <see cref="IPageItem"/>.
        /// </summary>
        /// <param name="relativePoint">Souřadnice relativní k this controlu</param>
        /// <param name="iPage">Výstup = nalezená stránka<see cref="IPageItem"/></param>
        /// <returns></returns>
        bool TryFindTabHeader(Point relativePoint, out IPageItem iPage);
        /// <summary>
        /// Smaže všechny stránky
        /// </summary>
        void ClearPages();
        /// <summary>
        /// Smaže aktuální stránky, vloží dodanou sadu, a poté se pokusí reaktivovat nově dodanou stránku se shodným ID jaké měla dosud aktivní stránka.
        /// Pokud nebyla nebo ji nenajde, pak aktivuje stránku s dodaným ID, anebo první dodanou stránku.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="selectPageId"></param>
        /// <param name="callEventChanging">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanging"/></param>
        /// <param name="callEventChanged">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanged"/></param>
        void SetPages(IEnumerable<IPageItem> pages, string selectPageId, bool callEventChanging, bool callEventChanged);
        /// <summary>
        /// Přidá dané stránky.
        /// </summary>
        /// <param name="pages"></param>
        void AddPages(IEnumerable<IPageItem> pages);
        /// <summary>
        /// Odstraní danou stránku podle jejího ID
        /// </summary>
        /// <param name="itemId"></param>
        void RemovePage(string itemId);
        /// <summary>
        /// Volá se při pokusu o zavírání stránky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, stránka bude zavřena.
        /// </summary>
        event EventHandler<TEventCancelArgs<IPageItem>> IPageClosing;
        /// <summary>
        /// Volá se po zavření (odebrání) stránky (záložky).
        /// </summary>
        event EventHandler<TEventArgs<IPageItem>> IPageRemoved;
        /// <summary>
        /// Aktuálně prověří velikost záhlaví <see cref="HeaderHeight"/> a/nebo <see cref="HeaderWidth"/>.
        /// </summary>
        void CheckHeaderSize();
        /// <summary>
        /// Aktuální výška záhlaví. 
        /// Je nastavováno pouze pro záhlaví umístěné Top nebo Bottom.
        /// </summary>
        int HeaderHeight { get; }
        /// <summary>
        /// Aktuální šířka záhlaví. 
        /// Je nastavováno pouze pro záhlaví umístěné Left nebo Right.
        /// </summary>
        int HeaderWidth { get; }
        /// <summary>
        /// Došlo ke změně výšky nebo šířky záhlaví:
        /// výška <see cref="HeaderHeight"/> pro pozici záhlaví Top nebo Bottom;
        /// šířka <see cref="HeaderWidth"/> pro pozici záhlaví Left nebo Right;
        /// </summary>
        event EventHandler HeaderSizeChanged;
        /// <summary>
        /// Metoda, která zapisuje do trace
        /// </summary>
        Action<Type, string, string, string, string, string> TraceMethod { get; set; }
        /// <summary>
        /// Souřadnice prvku
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Byl objekt Disposován?
        /// </summary>
        bool IsDisposed { get; }
        /// <summary>
        /// Disposuj objekt
        /// </summary>
        void Dispose();
    }
    /// <summary>
    /// Pozice záhlaví stránky
    /// </summary>
    [Flags]
    public enum DxPageHeaderPosition
    {
        /// <summary>
        /// Bez záhlaví
        /// </summary>
        None = 0,
        /// <summary>
        /// Záhlaví nahoře, přidejte hodnotu <see cref="IconOnly"/> anebo <see cref="TextOnly"/> anebo <see cref="IconText"/>
        /// </summary>
        PositionTop = 0x01,
        /// <summary>
        /// Záhlaví dole, přidejte hodnotu <see cref="IconOnly"/> anebo <see cref="TextOnly"/> anebo <see cref="IconText"/>
        /// </summary>
        PositionBottom = 0x02,
        /// <summary>
        /// Záhlaví vlevo, přidejte hodnotu <see cref="IconOnly"/> anebo <see cref="TextOnly"/> anebo <see cref="IconText"/>
        /// </summary>
        PositionLeft = 0x04,
        /// <summary>
        /// Záhlaví vpravo, přidejte hodnotu <see cref="IconOnly"/> anebo <see cref="TextOnly"/> anebo <see cref="IconText"/>
        /// </summary>
        PositionRight = 0x08,
        /// <summary>
        /// Zobrazit jen ikonu
        /// </summary>
        IconOnly = 0x10,
        /// <summary>
        /// Zobrazit jen text
        /// </summary>
        TextOnly = 0x20,
        /// <summary>
        /// Svislý text, vypadá to sice hrozně, ale 'De gustibus non est disputandum'...
        /// </summary>
        VerticalText = 0x100,

        /// <summary>
        /// Ikona a text
        /// </summary>
        IconText = IconOnly | TextOnly,
        /// <summary>
        /// Nahoře s ikonou a textem
        /// </summary>
        Top = PositionTop | IconText,
        /// <summary>
        /// Dole s ikonou a textem
        /// </summary>
        Bottom = PositionBottom | IconText,
        /// <summary>
        /// Vlevo s ikonou
        /// </summary>
        Left = PositionLeft | IconOnly,
        /// <summary>
        /// Vpravo s ikonou
        /// </summary>
        Right = PositionRight | IconOnly,
        /// <summary>
        /// Default = <see cref="Top"/> = Nahoře s ikonou a textem
        /// </summary>
        Default = Top,
        /// <summary>
        /// Souhrn všech bitů pozic, pro výpočet masky. Běžně se jako hodnota nepoužívá.
        /// </summary>
        PositionSummary = PositionTop | PositionBottom | PositionLeft | PositionRight
    }
    #endregion
    #region DxTabPane : Control se záložkami = DevExpress.XtraBars.Navigation.TabPane
    /// <summary>
    /// Control se záložkami = <see cref="DevExpress.XtraBars.Navigation.TabPane"/>
    /// </summary>
    public class DxTabPane : DevExpress.XtraBars.Navigation.TabPane, ITabHeaderControl, IListenerStyleChanged, IListenerZoomChange
    {
        #region Konstruktor, vnitřní eventy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTabPane()
        {
            InitProperties();
            InitTab();
            InitEvents();
            DxComponent.RegisterListener(this);
            this.IsPrepared = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.IsPrepared = false;
            DxComponent.UnregisterListener(this);
            this.RemoveEvents();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Obsahuje true v době, kdy control je korektně inicializován (na true se nastaví na konci konstruktoru),
        /// a pouze do doby, než začíná Dispose (na false se nastaví na začátku Dispose).
        /// </summary>
        public bool IsPrepared { get; protected set; }
        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void InitProperties()
        {
            this.Size = new Size(640, 120);

            this.TabAlignment = DevExpress.XtraEditors.Alignment.Near;           // Near = doleva, Far = doprava, Center = uprostřed
            this.PageProperties.AllowBorderColorBlending = true;
            this.PageProperties.ShowMode = DevExpress.XtraBars.Navigation.ItemShowMode.ImageAndText;
            this.AllowCollapse = DevExpress.Utils.DefaultBoolean.False;          // Nedovolí uživateli skrýt headery

            //this.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            //this.LookAndFeel.UseWindowsXPTheme = true;
            //this.OverlayResizeZoneThickness = 20;
            this.ItemOrientation = Orientation.Horizontal;                       // Vertical = kreslí řadu záhlaví vodorovně, ale obsah jednotlivého buttonu svisle :-(

            this.TransitionType = DxTabPaneTransitionType.FadeFast;

            // Požadavky designu na vzhled buttonů:
            this.AppearanceButton.Normal.FontStyleDelta = FontStyle.Regular;
            this.AppearanceButton.Normal.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.AppearanceButton.Hovered.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.AppearanceButton.Pressed.FontStyleDelta = FontStyle.Bold;
            this.AppearanceButton.Pressed.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            this.PageHeaderPosition = DxPageHeaderPosition.Top;
            this.PageHeaderAlignment = AlignContentToSide.Begin;
        }
        /// <summary>
        /// Vloží jednu výchozí záložku, aby Control mohl správně detekovat svoje rozměry.
        /// </summary>
        protected void InitTab()
        {
            // nepomáhá :  this.AddPage("", "");
        }
        /// <summary>
        /// Aktivuje vlastní eventy
        /// </summary>
        private void InitEvents()
        {
            this.TransitionManager.BeforeTransitionStarts += TransitionManager_BeforeTransitionStarts;
            this.TransitionManager.AfterTransitionEnds += TransitionManager_AfterTransitionEnds;
            this.SelectedPageChanging += _SelectedPageChanging;
            this.SelectedPageChanged += _SelectedPageChanged;
            this.ClientSizeChanged += _ClientSizeChanged;
            this.SizeChanged += _SizeChanged;
        }
        /// <summary>
        /// Deaktivuje vlastní eventy
        /// </summary>
        private void RemoveEvents()
        {
            if (this.IsDisposed) return;
            if (this.TransitionManager != null)
            {
                this.TransitionManager.BeforeTransitionStarts -= TransitionManager_BeforeTransitionStarts;
                this.TransitionManager.AfterTransitionEnds -= TransitionManager_AfterTransitionEnds;
            }
            this.SelectedPageChanging -= _SelectedPageChanging;
            this.SelectedPageChanged -= _SelectedPageChanged;
        }

        // Pořadí eventů v konstuktoru:
        //     DxTabPane_SelectedPageChanging;
        //     DxTabPane_SelectedPageChanged;
        // Pořadí eventů při přepínání stránky s transicí:        SelectedPageIndex
        //     DxTabPane_SelectedPageChanging;                           old           mám k dispozici old i new page v argumentu
        //     TransitionManager_BeforeTransitionStarts;                 old
        //     DxTabPane_SelectedPageChanged;                            new
        //     TransitionManager_AfterTransitionEnds;                    new

        /// <summary>
        /// Před zahájením přepnutí stránky
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="e"></param>
        private void TransitionManager_BeforeTransitionStarts(DevExpress.Utils.Animation.ITransition transition, System.ComponentModel.CancelEventArgs e)
        {
        }
        /// <summary>
        /// Po dokončení přepnutí stránky
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="e"></param>
        private void TransitionManager_AfterTransitionEnds(DevExpress.Utils.Animation.ITransition transition, EventArgs e)
        {
        }
        /// <summary>
        /// Na začátku přepínání záložek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SelectedPageChanging(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangingEventArgs e)
        {
            if (!_SelectedIPageChangingSuppress && e.Page is DxTabPage dxPage)
            {   // Eventy nejsou blokované, a cílová stránka je DxTabPage :
                TEventCancelArgs<IPageItem> args = new TEventCancelArgs<IPageItem>(dxPage.PageData);
                RunSelectedIPageChanging(args);
                if (args.Cancel)
                {   // Aplikace stornovala přepnutí záložky:
                    e.Cancel = true;
                    return;
                }
            }

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
        private void _SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            this.RunPageChangingActivate(this.PageChangingPageNew);
            this.RunPageChangingRelease(this.PageChangingPageOld);
            this.PageChangingIsRunning = false;

            if (!_SelectedIPageChangedSuppress && e.Page is DxTabPage dxPage)
            {
                TEventArgs<IPageItem> args = new TEventArgs<IPageItem>(dxPage.PageData);
                RunSelectedIPageChanged(args);
            }
        }
        /// <summary>
        /// Uloží dodané informace do trace
        /// </summary>
        /// <param name="method"></param>
        /// <param name="user0"></param>
        /// <param name="user1"></param>
        /// <param name="user2"></param>
        private void _Trace(string method, string user0 = null, string user1 = null, string user2 = null)
        {
            if (TraceMethod != null)
                TraceMethod(this.GetType(), method, "AsolDx.internal", user0, user1, user2);
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Umístění a vzhled záhlaví
        /// </summary>
        public DxPageHeaderPosition PageHeaderPosition { get { return _PageHeaderPosition; } set { ApplyHeaderPosition(value); } }
        private DxPageHeaderPosition _PageHeaderPosition = DxPageHeaderPosition.None;
        /// <summary>
        /// Aplikuje pozici záhlaví
        /// </summary>
        /// <param name="newHeaderPosition"></param>
        protected void ApplyHeaderPosition(DxPageHeaderPosition newHeaderPosition)
        {
            try
            {
                using (this.ScopeSuspendParentLayout())
                {
                    var oldHeaderPosition = _PageHeaderPosition;

                    // Tato třída implementuje jen podmnožinu požadovaných hodnot:
                    DxPageHeaderPosition validPositions = DxPageHeaderPosition.PositionTop | DxPageHeaderPosition.PositionBottom | DxPageHeaderPosition.IconOnly | DxPageHeaderPosition.TextOnly;
                    newHeaderPosition = newHeaderPosition & validPositions;
                    // Náhradní pozice při zadání Left nebo Right je Top:
                    if ((newHeaderPosition & (DxPageHeaderPosition.PositionTop | DxPageHeaderPosition.PositionBottom)) == 0)
                        newHeaderPosition |= DxPageHeaderPosition.PositionTop;

                    _PageHeaderPosition = newHeaderPosition;             // Od teď platí nová pravidla

                    // Změna zobrazení Icon a Text:
                    if (!newHeaderPosition.HasEqualsBit(oldHeaderPosition, DxPageHeaderPosition.IconOnly) || !newHeaderPosition.HasEqualsBit(oldHeaderPosition, DxPageHeaderPosition.TextOnly))
                        DxPages.ForEachExec(p => p.RefreshData());

                    // Pozice záhlaví, orientace:
                    //   tady není možno měnit.
                }
            }
            finally
            {
                _CheckHeaderSizeChangeForce();
            }
        }
        /// <summary>
        /// Umístění záložek na začátek / střed / konec
        /// </summary>
        public AlignContentToSide PageHeaderAlignment { get { return _PageHeaderAlignment; } set { ApplyHeaderAlignment(value); } }
        private AlignContentToSide _PageHeaderAlignment;
        /// <summary>
        /// Aplikuje zarovnání záhlaví
        /// </summary>
        /// <param name="headerAlignment"></param>
        protected void ApplyHeaderAlignment(AlignContentToSide headerAlignment)
        {
            this._PageHeaderAlignment = headerAlignment;
            this.TabAlignment = (headerAlignment == AlignContentToSide.Begin ? Alignment.Near :
                                (headerAlignment == AlignContentToSide.Center ? Alignment.Center :
                                (headerAlignment == AlignContentToSide.End ? Alignment.Far : Alignment.Near)));
        }
        /// <summary>
        /// Možnost zobrazit více řádek záhlaví.
        /// Tato třída tuto vlastnost nepodporuje, vždy je false.
        /// </summary>
        public bool PageHeaderMultiLine { get { return false; } set { } }
        #endregion
        #region Transitions
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
            if (this.IsPrepared)
            {
                TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageNew);
                OnPageChangingPrepare(args);
                PageChangingPrepare?.Invoke(this, args);
            }
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
            if (this.IsPrepared)
            {
                TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageNew);
                OnPageChangingActivate(args);
                PageChangingActivate?.Invoke(this, args);
            }
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
            if (this.IsPrepared)
            {
                TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageOld);
                OnPageChangingDeactivate(args);
                PageChangingDeactivate?.Invoke(this, args);
            }
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
            if (this.IsPrepared)
            {
                TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageOld);
                OnPageChangingRelease(args);
                PageChangingRelease?.Invoke(this, args);
            }
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

        #endregion
        #region Pages
        /// <summary>
        /// Stránky datové.
        /// Lze i setovat. Při setování se control pokusí ponechat jako selectovanou stránku tu, která je aktuálně selectovaná, podle jejího <see cref="ITextItem.ItemId"/>.
        /// Pokud to nebude možné (v nové sadě stránek nebude takové ID existovat), pak vyselectuje první existující stránku a vyvolá událost <see cref="SelectedIPageChanged"/>
        /// </summary>
        public IPageItem[] IPages
        {
            get { return this.DxPages.Select(p => p.PageData).ToArray(); }
            set { SetPages(value, null, true, true); }
        }
        /// <summary>
        /// Stránky vizuální
        /// </summary>
        protected DxTabPage[] DxPages { get { return this.Pages.OfType<DxTabPage>().ToArray(); } }
        /// <summary>
        /// Počet stránek
        /// </summary>
        public int IPageCount { get { return IPages.Length; } }
        /// <summary>
        /// Data aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public IPageItem SelectedIPage
        {
            get { return this.GetGuiValue<IPageItem>(() => GetGuiIPage()); }
            set { this.SetGuiValue<IPageItem>(v => SetGuiIPage(v, false, false), value); }
        }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public string SelectedIPageId
        {
            get { return this.GetGuiValue<string>(() => GetGuiIPage()?.ItemId); }
            set { this.SetGuiValue<string>(v => SetGuiPageId(v, false, false), value); }
        }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně NEPROBĚHNE událost <see cref="SelectedIPageChanging"/>, pouze proběhne <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public string SelectedIPageIdForce
        {
            get { return this.GetGuiValue<string>(() => GetGuiIPage()?.ItemId); }
            set { this.SetGuiValue<string>(v => SetGuiPageId(v, true, false), value); }
        }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně NEPROBĚHNE žádná událost, ani <see cref="SelectedIPageChanging"/>, ani <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public string SelectedIPageIdSilent
        {
            get { return this.GetGuiValue<string>(() => GetGuiIPage()?.ItemId); }
            set { this.SetGuiValue<string>(v => SetGuiPageId(v, true, true), value); }
        }
        /// <summary>
        /// Prostý index aktuálně vybrané stránky nebo -1 pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public int SelectedIPageIndex
        {
            get { return this.GetGuiValue<int>(() => GetGuiIndex()); }
            set { this.SetGuiValue<int>(v => SetGuiIndex(v, true, false), value); }
        }
        /// <summary>
        /// Vrátí aktuální stránku <see cref="IPageItem"/>, provádí se v GUI threadu
        /// </summary>
        /// <returns></returns>
        private IPageItem GetGuiIPage()
        {
            var tabPage = this.SelectedPage;
            return (tabPage != null && tabPage is DxTabPage dxPage) ? dxPage.PageData : null;
        }
        /// <summary>
        /// Setuje hodnotu SelectedPage na danou stránku, potlačí eventy, provádí se v GUI threadu
        /// </summary>
        /// <param name="iPage"></param>
        /// <param name="suppressChanging"></param>
        /// <param name="suppressChanged"></param>
        private void SetGuiIPage(IPageItem iPage, bool suppressChanging, bool suppressChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = suppressChanging;
                _SelectedIPageChangedSuppress = suppressChanged;

                this.SelectedPage = SearchDxPage(iPage);
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }
        /// <summary>
        /// Setuje hodnotu SelectedPage na danou stránku, potlačí eventy, provádí se v GUI threadu
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="suppressChanging"></param>
        /// <param name="suppressChanged"></param>
        private void SetGuiPageId(string pageId, bool suppressChanging, bool suppressChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = suppressChanging;
                _SelectedIPageChangedSuppress = suppressChanged;

                this.SelectedPage = SearchDxPage(pageId);
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }
        /// <summary>
        /// Vrátí index aktuální stránky, provádí se v GUI threadu
        /// </summary>
        /// <returns></returns>
        private int GetGuiIndex()
        {
            return this.SelectedPageIndex;
        }
        /// <summary>
        /// Setuje index aktuální stránky na danou stránku, potlačí eventy, provádí se v GUI threadu
        /// </summary>
        /// <param name="index"></param>
        /// <param name="suppressChanging"></param>
        /// <param name="suppressChanged"></param>
        private void SetGuiIndex(int index, bool suppressChanging, bool suppressChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = suppressChanging;
                _SelectedIPageChangedSuppress = suppressChanged;

                this.SelectedPageIndex = index;
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }

        /// <summary>
        /// Vyvolá metodu <see cref="OnSelectedIPageChanging(TEventCancelArgs{IPageItem})"/> a event <see cref="SelectedIPageChanging"/>, 
        /// bez podmínky na <see cref="_SelectedIPageChangedSuppress"/>
        /// </summary>
        /// <param name="args"></param>
        private void RunSelectedIPageChanging(TEventCancelArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnSelectedIPageChanging(args);
                SelectedIPageChanging?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Událost volaná při pokusu o aktivaci jiné záložky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, nová stránka bude aktivována.
        /// </summary>
        protected virtual void OnSelectedIPageChanging(TEventCancelArgs<IPageItem> args) { }
        /// <summary>
        /// Volá se při pokusu o aktivaci jiné záložky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, nová stránka bude aktivována.
        /// </summary>
        public event EventHandler<TEventCancelArgs<IPageItem>> SelectedIPageChanging;

        /// <summary>
        /// Vyvolá metodu <see cref="OnSelectedIPageChanged(TEventArgs{IPageItem})"/> a event <see cref="SelectedIPageChanged"/>, bez podmínky na <see cref="_SelectedIPageChangedSuppress"/>
        /// </summary>
        /// <param name="args"></param>
        private void RunSelectedIPageChanged(TEventArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnSelectedIPageChanged(args);
                SelectedIPageChanged?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Událost volaná při změně aktivní stránky <see cref="SelectedIPage"/>
        /// </summary>
        protected virtual void OnSelectedIPageChanged(TEventArgs<IPageItem> args) { }
        /// <summary>
        /// Událost volaná při změně aktivní stránky <see cref="SelectedIPage"/>
        /// </summary>
        public event EventHandler<TEventArgs<IPageItem>> SelectedIPageChanged;
        /// <summary>
        /// Hodnota true potlačí volání události <see cref="SelectedIPageChanging"/>, ale nepotlačí volání události <see cref="SelectedIPageChanged"/>, 
        /// použije se při interních změnách obsahu, 
        /// když víme že na konci změn nastavíme správnou hodnotu a/nebo vyvoláme explicitně event.
        /// </summary>
        private bool _SelectedIPageChangingSuppress = false;
        /// <summary>
        /// Hodnota true potlačí volání události <see cref="SelectedIPageChanging"/> a <see cref="SelectedIPageChanged"/>, 
        /// použije se při interních změnách obsahu, 
        /// když víme že na konci změn nastavíme správnou hodnotu a/nebo vyvoláme explicitně event.
        /// </summary>
        private bool _SelectedIPageChangedSuppress = false;
        /// <summary>
        /// Hodnota true potlačí kontroly výšky záhlaví. Nastavuje se na true v procesu mazání stránek v <see cref="_RemovePages()"/>.
        /// </summary>
        private bool _SizeChangedHeightCheckSuppress = false;
        /// <summary>
        /// Zjistí, zda na zadané relativní souřadnici se nachází nějaké záhlaví, a pokud ano pak najde odpovídající stránku <see cref="IPageItem"/>.
        /// </summary>
        /// <param name="relativePoint">Souřadnice relativní k this controlu</param>
        /// <param name="iPage">Výstup = nalezená stránka<see cref="IPageItem"/></param>
        /// <returns></returns>
        public bool TryFindTabHeader(Point relativePoint, out IPageItem iPage)
        {
            var hit = this.CalcHitInfo(relativePoint);
            if (hit != null && hit is DxTabPage dxPage)
            {
                iPage = dxPage.PageData;
                return (iPage != null);
            }
            iPage = null;
            return false;
        }
        /// <summary>
        /// Smaže všechny stránky
        /// </summary>
        public void ClearPages()
        {
            _AddPages(true, null, null, true, true);
        }
        /// <summary>
        /// Smaže aktuální stránky, vloží dodanou sadu, a poté se pokusí reaktivovat nově dodanou stránku se shodným ID jaké měla dosud aktivní stránka.
        /// Pokud nebyla nebo ji nenajde, pak aktivuje stránku s dodaným ID, anebo první dodanou stránku.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="selectPageId"></param>
        /// <param name="callEventChanging">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanging"/></param>
        /// <param name="callEventChanged">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanged"/></param>
        public void SetPages(IEnumerable<IPageItem> pages, string selectPageId, bool callEventChanging, bool callEventChanged)
        {
            _AddPages(true, pages, selectPageId, callEventChanging, callEventChanged);
        }
        /// <summary>
        /// Přidá dané stránky.
        /// </summary>
        /// <param name="pages"></param>
        public void AddPages(IEnumerable<IPageItem> pages)
        {
            _AddPages(false, pages, null, true, true);
        }
        /// <summary>
        /// Přidá danou stránku.
        /// </summary>
        /// <param name="page"></param>
        public void AddPage(IPageItem page)
        {
            if (page != null)
                _AddPages(false, new IPageItem[] { page }, null, true, true);
        }
        /// <summary>
        /// Přidá stránky
        /// </summary>
        /// <param name="clear"></param>
        /// <param name="pages"></param>
        /// <param name="selectPageId"></param>
        /// <param name="callEventChanging">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanging"/></param>
        /// <param name="callEventChanged">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanged"/></param>
        private void _AddPages(bool clear, IEnumerable<IPageItem> pages, string selectPageId, bool callEventChanging, bool callEventChanged)
        {
            int oldPagesCount = this.Pages.Count;
            int oldDxPagesCount = this.DxPages.Length;
            int newPagesCount = pages?.Count() ?? 0;

            _Trace("_AddPages", $"(clear={clear}, pages={pages?.Count().ToString() ?? "NULL"}, selectPageId={selectPageId}, callEventChanging={callEventChanging}, callEventChanged={callEventChanged});",
                $"oldPagesCount={oldPagesCount}; oldDxPagesCount={oldDxPagesCount}");

            if (oldPagesCount == 0 && newPagesCount == 0)
            {   // Nic nebylo, nic nebude = zkratka:
                _Trace("_AddPages", "mode: Nothing", "Zero=Zero");
                return;
            }
            if (oldPagesCount == newPagesCount && oldDxPagesCount == newPagesCount && clear)
            {   // Stejný počet stránek = nepotřebujeme provádět Remove a Add, provedeme jen Refresh jejich obsahu (a možná změnu aktivní stránky):
                _Trace("_AddPages", "mode: RefreshOnly", $"Pages.Count={oldPagesCount }");
                _RefreshPages(pages.ToArray(), selectPageId, callEventChanging, callEventChanged);
                return;
            }

            if (clear && oldPagesCount == 0) clear = false;          // Požadavek na Clear při stávajícím počtu stránek == 0 stornujeme.

            // Reload / Add pages:
            _Trace("_AddPages", (clear ? "mode: ExChangePages" : "mode: AddPages"), $"Different PagesCount: Old:{oldPagesCount} <> New:{newPagesCount}");

            var oldPage = SelectedIPage;
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            bool isHeightCheckSuppress = _SizeChangedHeightCheckSuppress;
            bool isActiveOldPage = false;
            bool runEventChanged = false;

            _Trace("_AddPages", $"OldPages.count: {oldPagesCount}");
            Size headerSizeOld = (this.Pages.Count > 0 ? this.ViewInfo.ButtonsBounds.Size : Size.Empty);
            _Trace("_AddPages", $"OldHeaders.Size: {headerSizeOld}");

            DxTabPage activatePage = null;
            IPageItem[] allPages = null;
            try
            {
                using (this.ScopeSuspendParentLayout())
                {
                    _Trace("_AddPages", $"in ScopeSuspendParentLayout");

                    _SelectedIPageChangingSuppress = true;
                    _SelectedIPageChangedSuppress = true;
                    _SizeChangedHeightCheckSuppress = true;

                    bool forceResize = (this.Pages.Count == 0);

                    if (clear)
                        this._RemovePages();

                    int pageIndex = 0;
                    if (pages != null)
                    {
                        foreach (var page in pages)
                        {
                            if (page != null)
                            {
                                _Trace("_AddPages", $"ProcessPage [{pageIndex}]: {page.Text}", "Begin");
                                DxTabPage dxPage = new DxTabPage(this, page);
                                this.Pages.Add(dxPage);
                                if (forceResize)
                                {   // Po přidání úplně první stránky do TabHeaders:
                                    //  potlačeno ... _Trace("_AddPages", $"_CheckHeaderSizeChange");
                                    //  potlačeno ... _CheckHeaderSizeChange(true, headerSizeOld);
                                    forceResize = false;
                                }
                                _Trace("_AddPages", $"ProcessPage [{pageIndex}]: {page.Text}", "End");
                            }
                            pageIndex++;
                        }
                    }
                    allPages = this.IPages;
                    _Trace("_AddPages", $"Processed all pages, total count: {allPages.Length}");

                    bool hasOldPage = (oldPage != null);
                    isActiveOldPage = (hasOldPage && Object.ReferenceEquals(oldPage, this.SelectedIPage));
                    if (hasOldPage && !isActiveOldPage && allPages.Length > 0)
                    {   // Pokud dříve byla nějaká stránka aktivní, a nyní je aktivní jiná, pak se pokusím reaktivovat původní stránku:
                        _Trace("_AddPages", $"Search OldPage: {oldPage.Text}");
                        activatePage = this.SearchDxPage(oldPage);
                        if (activatePage != null)
                        {
                            _Trace("_AddPages", $"ActivateOldPage: {activatePage.Text}", "SilentMode");
                            this.SelectedPage = activatePage;  // Fyzicky aktivuji stránku, ale event SelectedIPageChanged je potlačený (_SelectedIPageChangedSuppress) = takže se neprovede
                            isActiveOldPage = true;
                        }
                    }

                    // Pokud se mi nepodařilo aktivovat původní stránku (oldPage), tak nejspíš proto že původně nebylo nic, anebo jsme původní stránku smazali;
                    // pak nyní najdu vhodnou stránku a vložím ji do 
                    if (!isActiveOldPage && allPages != null)
                    {
                        activatePage = null;
                        if (allPages.Length == 0)
                        {   // Nyní nemám žádné stránky,
                            // a pokud jsem dosud měl nějakou aktivní, tak zavoláme event (změna Page => null):
                            runEventChanged = (oldPage != null);
                            if (oldPage != null)
                                _Trace("_AddPages", $"ActivateOldPage: No pages exists, deactivate old page: {oldPage.Text}");
                        }
                        else
                        {
                            if (selectPageId != null)
                                activatePage = this.SearchDxPage(selectPageId);
                            if (activatePage == null)
                                activatePage = this.Pages[0] as DxTabPage;
                            runEventChanged = true;
                            _Trace("_AddPages", $"ActivateOldPage: {activatePage.Text}");
                        }
                    }
                }
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
                _SizeChangedHeightCheckSuppress = isHeightCheckSuppress;
                _CheckHeaderSizeChangeForce(headerSizeOld);
            }

            if (runEventChanged)
            {
                _Trace("_AddPages", $"SelectPage: {activatePage.Text}, callEventChanging={callEventChanging}, callEventChanged={callEventChanged})");
                this.SelectPage(activatePage, callEventChanging, callEventChanged);
            }

            _Trace("_AddPages", $"Done.");
        }
        /// <summary>
        /// Refresh vzhledu stránek = bez jejich změny
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="selectPageId"></param>
        /// <param name="callEventChanging"></param>
        /// <param name="callEventChanged"></param>
        private void _RefreshPages(IPageItem[] pages, string selectPageId, bool callEventChanging, bool callEventChanged)
        {
            var dxPages = this.DxPages;
            if (dxPages.Length != pages.Length)
                throw new ArgumentException($"DxTabPane error in _RefreshPages(): new pages length = {pages.Length} differ from current DxPages length = {dxPages.Length}.");

            // Nyní aktivní stránka:
            var oldSelectedPageId = this.SelectedIPageId;

            // Refresh vizuálních stránek pomocí dat z dodaných logických stránek:
            int length = dxPages.Length;
            for (int p = 0; p < length; p++)
            {
                _Trace("_RefreshPages", $"Page[{p}]: {pages[p].Text} [PageId: {pages[p].PageId}]");
                dxPages[p].RefreshData(pages[p]);
            }
            _Trace("_RefreshPages", $"Done");

            // Po změně obsahu stránek se sice nezmění aktuálně vybraná stránka, ale může se změnit její ID:
            string currentSelectedIPageId = this.SelectedIPageId;
            if (String.IsNullOrEmpty(selectPageId))
            {   // Pokud není zadána explicitní stránka, aktivujeme stránku s ID odpovídající dřívějšímu ID - pokud tedy došlo ke změně ID (málokdy):
                if (!String.Equals(currentSelectedIPageId, oldSelectedPageId, StringComparison.Ordinal))
                {
                    _Trace("_RefreshPages", $"ReActivate page: {oldSelectedPageId}");
                    this.SelectedIPageIdSilent = oldSelectedPageId;
                }
            }
            else if (!String.Equals(currentSelectedIPageId, selectPageId, StringComparison.Ordinal))
            {   // Je dána explicitní stránka pro aktivaci, a nyní není aktivní? Aktivujme ji - potichu nebo s eventy, podle požadavků:
                _Trace("_RefreshPages", $"Activate page: {selectPageId}");
                SetGuiPageId(selectPageId, !callEventChanging, !callEventChanged);
            }
        }
        /// <summary>
        /// Metoda zajistí odstranění všech stránek, ale potlačí volání událostí při změně velikosti záhlaví.
        /// V této době mají být potlačeny i eventy o změnách aktivní stránky, to ale tato metoda neřídí.
        /// </summary>
        private void _RemovePages()
        {
            int count = this.Pages.Count;
            _Trace("_RemovePages", $"Clear pages begin, Count={count}");
            if (count > 0)
            {
                bool isHeightCheckSuppress = _SizeChangedHeightCheckSuppress;
                try
                {
                    _SizeChangedHeightCheckSuppress = true;
                    var pages = this.Pages.ToArray();
                    foreach (var page in pages)
                    {
                        if (this.Pages.Contains(page))
                        {
                            _Trace("_RemovePages", $"Remove page: {page.Caption}...");
                            this.Pages.Remove(page);
                            _Trace("_RemovePages", $"Page: {page.Caption} removed.");
                        }
                        else
                        {
                            _Trace("_RemovePages", $"Removeing page: {page.Caption} : page disappeared.");
                        }
                    }
                }
                finally
                {
                    _SizeChangedHeightCheckSuppress = isHeightCheckSuppress;
                }
            }
            _Trace("_RemovePages", $"Clear pages done.");
        }
        /// <summary>
        /// Selectuje danou stránku, vyvolá přitom požadované události
        /// </summary>
        /// <param name="activatePage"></param>
        /// <param name="callEventChanging"></param>
        /// <param name="callEventChanged"></param>
        protected void SelectPage(DxTabPage activatePage, bool callEventChanging, bool callEventChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = !callEventChanging; // callEventChanging = true => volat event => NEpotlačit volání eventu !
                _SelectedIPageChangedSuppress = !callEventChanged;
                this.SelectedPage = activatePage;
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }
        /// <summary>
        /// Odstraní danou stránku podle jejího ID
        /// </summary>
        /// <param name="itemId"></param>
        public void RemovePage(string itemId)
        {
            _RemovePage(SearchDxPage(itemId));
        }
        /// <summary>
        /// Odstraní danou stránku podle jejího objektu
        /// </summary>
        /// <param name="iPage"></param>
        public void RemovePage(IPageItem iPage)
        {
            _RemovePage(SearchDxPage(iPage));
        }
        /// <summary>
        /// Odstraní danou stránku
        /// </summary>
        /// <param name="dxPage"></param>
        private void _RemovePage(DxTabPage dxPage)
        {
            if (dxPage != null)
            {
                Size headerSizeOld = (this.Pages.Count > 0 ? this.ViewInfo.ButtonsBounds.Size : Size.Empty);
                if (Object.ReferenceEquals(this.SelectedPage, dxPage))
                    _SelectAnyNearPage(dxPage);
                this.Pages.Remove(dxPage);
                RunIPageRemoved(new TEventArgs<IPageItem>(dxPage.PageData));
                _CheckHeaderSizeChange(true, headerSizeOld);
            }
        }
        /// <summary>
        /// Aktivuje nejbližší sousední stránku ke stránce dané
        /// </summary>
        /// <param name="dxPage"></param>
        private void _SelectAnyNearPage(DxTabPage dxPage)
        {
            if (dxPage is null) return;
            int index = this.Pages.IndexOf(dxPage);
            if (index < 0) return;
            int last = this.Pages.Count - 1;
            if (index < last)
                this.SelectedPageIndex = index + 1;
            else if (index > 0)
                this.SelectedPageIndex = index - 1;
        }
        /// <summary>
        /// Uživatel kliknul na křížek, a chce zavřít stránku. 
        /// Zavírací křížek je uživateli zobrazen proto, že stránka je deklarovaná s <see cref="IPageItem.CloseButtonVisible"/> = true.
        /// </summary>
        /// <param name="args"></param>
        private void RunIPageClosing(TEventCancelArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnIPageClosing(args);
                IPageClosing?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Volá se při pokusu o zavírání stránky.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIPageClosing(TEventCancelArgs<IPageItem> args) { }
        /// <summary>
        /// Volá se při pokusu o zavírání stránky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, stránka bude zavřena.
        /// </summary>
        public event EventHandler<TEventCancelArgs<IPageItem>> IPageClosing;
        /// <summary>
        /// Proběhlo odstranění stránky (záložky).
        /// </summary>
        /// <param name="args"></param>
        private void RunIPageRemoved(TEventArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnIPageRemoved(args);
                IPageRemoved?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Volá se po zavření (odebrání) stránky (záložky).
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIPageRemoved(TEventArgs<IPageItem> args) { }
        /// <summary>
        /// Volá se po zavření (odebrání) stránky (záložky).
        /// </summary>
        public event EventHandler<TEventArgs<IPageItem>> IPageRemoved;
        /// <summary>
        /// Najde fyzickou stránku pro danou datovou stránku, nebo pro její ID
        /// </summary>
        /// <param name="iPage"></param>
        /// <returns></returns>
        private DxTabPage SearchDxPage(IPageItem iPage)
        {
            DxTabPage dxPage = null;
            if (iPage != null)
            {
                DxTabPage[] dxPages = DxPages;
                dxPage = dxPages.FirstOrDefault(p => Object.ReferenceEquals(p.PageData, iPage));
                if (dxPage == null && iPage.ItemId != null)
                    dxPage = dxPages.FirstOrDefault(p => String.Equals(p.PageData.ItemId, iPage.ItemId));
            }
            return dxPage;
        }
        /// <summary>
        /// Najde fyzickou stránku pro dané ID stránky
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private DxTabPage SearchDxPage(string itemId)
        {
            DxTabPage dxPage = null;
            if (itemId != null)
            {
                DxTabPage[] dxPages = DxPages;
                dxPage = dxPages.FirstOrDefault(p => String.Equals(p.PageData.ItemId, itemId));
            }
            return dxPage;
        }
        #endregion
        #region Výška a šířka prostoru záhlaví
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            _CheckHeaderSizeChange();
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SizeChanged(object sender, EventArgs e)
        {
            _CheckHeaderSizeChange();
        }
        /// <summary>
        /// Ověří, zda nedošlo ke změně výšky nebo šířky záhlaví (podle orientace), a pokud ano, pak vyvolá patřičné události.
        /// Použije brutální sílu k prolomení neproniknutelné hradby DevExpress.
        /// </summary>
        /// <param name="headerSizeOld">Předešlá velikost záhlaví</param>
        private void _CheckHeaderSizeChangeForce(Size? headerSizeOld = null)
        {   // Musí být dvakrát, jinak špatně funguje změna z nuly na více záložek, nebo změna u levého i pravého umístění.
            _CheckHeaderSizeChange(true, headerSizeOld);
            // TabPane nepomáhá nic...   ani druhé volání :      _CheckHeaderSizeChange(true);
        }
        /// <summary>
        /// Ověří, zda nedošlo ke změně výšky nebo šířky záhlaví (podle orientace), a pokud ano, pak vyvolá patřičné události.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="headerSizeOld">Předešlá velikost záhlaví</param>
        private void _CheckHeaderSizeChange(bool force = false, Size? headerSizeOld = null)
        {
            bool parentIsNull = (this.Parent is null);
            bool checkSuppress = _SizeChangedHeightCheckSuppress;

            string stackTrace = (DxComponent.IsDebuggerActive ? Environment.StackTrace : "");

            _Trace("_CheckHeaderSizeChange", $"Parent: {(parentIsNull ? "Null=>Skip" : "OK")}; HeightCheckSuppress: {(checkSuppress ? "True=>Skip" : "False")}", stackTrace);
            if (parentIsNull || checkSuppress) return;

            if (!_CheckHeaderSizeInProgress || force)
            {
                bool oldInProgress = _CheckHeaderSizeInProgress;
                try
                {
                    _CheckHeaderSizeInProgress = true;
                    // Tato třída má záhlaví pouze v pozici Top, hlídáme pouze Height:
                    _CheckHeaderHeightChange(force, headerSizeOld);
                }
                finally
                {
                    _CheckHeaderSizeInProgress = oldInProgress;
                }
            }
        }
        /// <summary>
        /// Právě nyní probíhá kontrola velikosti záhlaví
        /// </summary>
        private bool _CheckHeaderSizeInProgress = false;
        /// <summary>
        /// Ověří, zda nedošlo ke změně výšky záhlaví (pro záhlaví umístěné Top nebo Bottom),
        /// a pokud ano, pak vyvolá patřičné události.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="headerSizeOld">Předešlá velikost záhlaví</param>
        private void _CheckHeaderHeightChange(bool force, Size? headerSizeOld = null)
        {
            _HeaderWidth = 0;
            if (this.Width < 10) return;

            int headerHeight = 0;
            if (this.Pages.Count > 0)
            {
                /*   Nebudu hledat informace v this.ViewInfo?.ButtonsBounds; neboť je to nespolehlivé.
                     Vytvořil jsem DxComponent.SkinColorSet a tam hodnotu TabHeaderHeight; která je specifická pro každý známý skin.
                     Tu hodnotu dole načtu, upravím pomocí Zoomu a použiju.


                int minHeight = 10;

                // První naplnění stránkami dělá problémy, protože velikost může být menší než je potřebná, a pak ViewInfo obsahuje "náhradní záporné hodnoty".
                // To ale tady nechceme!
                if (force || this.Height <= minHeight)
                {
                    if (this.Height <= minHeight)
                        this.Height = 50;
                    this.Refresh();
                }

                var headerBounds = this.ViewInfo?.ButtonsBounds;
                if (headerBounds.HasValue)
                {   // Čistá cesta:
                    headerHeight = headerBounds.Value.Height;

                    // Máme výšku z nedávného stavu?  Ta sem přichází z metody _AddPages(), kde jsme si ji zapamatovali před Pages.Clear():
                    if (headerHeight < minHeight && headerSizeOld.HasValue) headerHeight = headerSizeOld.Value.Height;

                    if (headerHeight >= minHeight)
                    {   // Komponenta dokázala určit správnou výšku - zapamatujeme si ji:
                        _HeaderHeightLastValid = headerHeight;
                        // a přidáme 1px pro okraj:
                        headerHeight += 3;
                    }
                    else
                    {   // Komponenta NEdokázala určit správnou výšku - možná si ji pamatujeme offline od posledně:
                        headerHeight = _HeaderHeightLastValid;
                        if (headerHeight > 0)
                            // Máme jí! Tak přidáme 12px pro okraj a tělo stránky:
                            headerHeight += 12;
                        else
                            // Nemáme ji, použijeme kouzelnou konstantu:
                            headerHeight = 50;
                    }
                }
                else
                {   // Náhradní cesta:
                    bool isTop = !this.PageHeaderPosition.HasFlag(DxPageHeaderPosition.PositionBottom);
                    headerHeight = (isTop ? (this.DisplayRectangle.Y + 2) : (this.ClientSize.Height - this.DisplayRectangle.Height));
                }
                if (headerHeight < minHeight) headerHeight = minHeight;


                */

                // Využijeme statickou informaci o Skinu:
                var tabSkinHeight = DxComponent.SkinColorSet.TabHeaderHeight;
                var formDpi = this.FindForm()?.DeviceDpi ?? 96;
                var currentHeight = DxComponent.ZoomToGui(tabSkinHeight, formDpi);
                headerHeight = currentHeight;
            }

            if (headerHeight != _HeaderHeight)
            {   // Výška záhlaví byla změněna:
                _Trace("_CheckHeaderSizeChange", $"OldHeight: {_HeaderHeight}; NewHeight: {headerHeight}");
                _HeaderHeight = headerHeight;
                OnHeaderSizeChanged();
                HeaderSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aktuálně prověří velikost záhlaví <see cref="HeaderHeight"/> a/nebo <see cref="HeaderWidth"/>.
        /// </summary>
        public void CheckHeaderSize() { _CheckHeaderSizeChangeForce(); }
        /// <summary>
        /// Aktuální výška záhlaví. 
        /// Je nastavováno pouze pro záhlaví umístěné Top nebo Bottom.
        /// </summary>
        public int HeaderHeight { get { return _HeaderHeight; } }
        private int _HeaderHeight;
        /// <summary>
        /// Posledně platná výška záhlaví. Tahle komponenta někdy nedokáže určit ViewInfo.ButtonsBounds.Height (po Clear() a po AddPage() obsahuje Height = 0).
        /// </summary>
        private int _HeaderHeightLastValid;
        /// <summary>
        /// Aktuální šířka záhlaví. 
        /// Je nastavováno pouze pro záhlaví umístěné Left nebo Right.
        /// </summary>
        public int HeaderWidth { get { return _HeaderWidth; } }
        private int _HeaderWidth;
        /// <summary>
        /// Při změně výšky nebo šířky záhlaví:
        /// výška <see cref="HeaderHeight"/> pro pozici záhlaví Top nebo Bottom;
        /// šířka <see cref="HeaderWidth"/> pro pozici záhlaví Left nebo Right;
        /// </summary>
        protected virtual void OnHeaderSizeChanged() { }
        /// <summary>
        /// Došlo ke změně výšky nebo šířky záhlaví:
        /// výška <see cref="HeaderHeight"/> pro pozici záhlaví Top nebo Bottom;
        /// šířka <see cref="HeaderWidth"/> pro pozici záhlaví Left nebo Right;
        /// </summary>
        public event EventHandler HeaderSizeChanged;
        /// <summary>
        /// Metoda, která zapisuje do trace
        /// </summary>
        public Action<Type, string, string, string, string, string> TraceMethod { get; set; }
        #endregion
        #region IListenerStyleChanged, IListenerZoomChange
        void IListenerStyleChanged.StyleChanged() { this._CheckHeaderSizeChange(true); }
        void IListenerZoomChange.ZoomChanged() { this._CheckHeaderSizeChange(true); }
        #endregion
    }
    #region DxTabPage : jedna stránka v DxTabPane
    /// <summary>
    /// Třída jedné stránky
    /// </summary>
    public class DxTabPage : DevExpress.XtraBars.Navigation.TabNavigationPage
    {
        /// <summary>
        /// Konstruktor, nepoužívat v aplikaci
        /// </summary>
        public DxTabPage()
        {
            this.IsPrepared = true;
        }
        /// <summary>
        /// Konstruktor s daty
        /// </summary>
        /// <param name="tabOwner"></param>
        /// <param name="pageData"></param>
        public DxTabPage(DxTabPane tabOwner, IPageItem pageData)
        {
            _TabOwner = tabOwner;
            PageData = pageData;
            RefreshData();
            this.IsPrepared = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.PageData?.ToString() ?? this.Caption;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.IsPrepared = false;
            this.ReleasePageData();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Uvolní dosud přiřazený objekt <see cref="PageData"/>
        /// </summary>
        protected void ReleasePageData()
        {
            var pageData = PageData;
            if (pageData != null)
                pageData.PageControl = null;
            PageData = null;
        }
        /// <summary>
        /// Obsahuje true v době, kdy control je korektně inicializován (na true se nastaví na konci konstruktoru),
        /// a pouze do doby, neý začíná Dispose (na false se nastaví na začátku Dispose).
        /// </summary>
        public bool IsPrepared { get; protected set; }
        /// <summary>
        /// Vlastník
        /// </summary>
        protected DxTabPane TabOwner { get { return _TabOwner?.Target; } }
        private WeakTarget<DxTabPane> _TabOwner;
        /// <summary>
        /// Data stránky
        /// </summary>
        public IPageItem PageData { get; private set; }
        /// <summary>
        /// Do this stránky refreshuje data z dodaného objektu <see cref="IPageItem"/>
        /// </summary>
        /// <param name="pageData"></param>
        public void RefreshData(IPageItem pageData)
        {
            ReleasePageData();
            PageData = pageData;
            RefreshData();
        }
        /// <summary>
        /// Do this stránky refreshuje data ze svého objektu <see cref="IPageItem"/>
        /// </summary>
        public void RefreshData()
        {
            var pageData = PageData;
            if (pageData is null) return;

            var headerPosition = TabOwner?.PageHeaderPosition ?? DxPageHeaderPosition.Top;
            bool hasText = headerPosition.HasFlag(DxPageHeaderPosition.TextOnly);
            bool hasIcon = headerPosition.HasFlag(DxPageHeaderPosition.IconOnly);

            this.Name = pageData.ItemId;
            this.Caption = (hasText ? pageData.Text : "");
            this.SuperTip = DxComponent.CreateDxSuperTip(pageData);

            if (hasIcon)
                DxComponent.ApplyImage(ImageOptions, pageData.ImageName, sizeType: ResourceImageSizeType.Medium);
            else
                ImageOptions.Reset();

            pageData.PageControl = this;
        }
    }
    #endregion
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
    #region DxXtraTabControl : rozsáhlejší záložkovník
    /// <summary>
    /// <see cref="DxXtraTabControl"/> : Jiný záložkovník
    /// </summary>
    public class DxXtraTabControl : DevExpress.XtraTab.XtraTabControl, ITabHeaderControl, IListenerStyleChanged, IListenerZoomChange
    {
        #region Konstruktor, vnitřní eventy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxXtraTabControl()
        {
            this.InitProperties();
            InitTab();
            this.InitEvents();
            DxComponent.RegisterListener(this);
            this.IsPrepared = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.IsPrepared = false;
            DxComponent.UnregisterListener(this);
            this.RemoveEvents();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Obsahuje true v době, kdy control je korektně inicializován (na true se nastaví na konci konstruktoru),
        /// a pouze do doby, neý začíná Dispose (na false se nastaví na začátku Dispose).
        /// </summary>
        public bool IsPrepared { get; protected set; }
        /// <summary>
        /// Inicializace výchozích vlastností
        /// </summary>
        protected void InitProperties()
        {
            Size = new Size(640, 120);

            Transition.AllowTransition = DefaultBoolean.False;

            ClosePageButtonShowMode = DevExpress.XtraTab.ClosePageButtonShowMode.InActiveTabPageHeaderAndOnMouseHover;   // Podporuje zavírací křížek na konkrétní stránce
            CustomHeaderButtons.Clear();
            HeaderAutoFill = DevExpress.Utils.DefaultBoolean.False;
            HeaderButtonsShowMode = DevExpress.XtraTab.TabButtonShowMode.WhenNeeded;     // Podporuje zavírací křížek na konkrétní stránce
            MultiLine = DevExpress.Utils.DefaultBoolean.True;
            PageImagePosition = DevExpress.XtraTab.TabPageImagePosition.Near;

            // Požadavky designu na vzhled buttonů:
            AppearancePage.Header.FontStyleDelta = FontStyle.Regular;
            AppearancePage.HeaderActive.FontStyleDelta = FontStyle.Bold;

            PageHeaderPosition = DxPageHeaderPosition.Default;
            PageHeaderAlignment = AlignContentToSide.Begin;
            PageHeaderMultiLine = false;
        }
        /// <summary>
        /// Vloží jednu výchozí záložku, aby Control mohl správně detekovat svoje rozměry.
        /// </summary>
        protected void InitTab()
        {
            // nepomáhá :  this.TabPages.Add();
        }
        /// <summary>
        /// Nastaví zarovnání textu v záhlaví. Je vhodné nastavit podle pozice záhlaví.
        /// </summary>
        /// <param name="headerLocation"></param>
        protected void SetHeaderHAlignment(DevExpress.XtraTab.TabHeaderLocation headerLocation)
        {
            HorzAlignment hAlignment =
                (headerLocation == DevExpress.XtraTab.TabHeaderLocation.Top ? HorzAlignment.Center :
                (headerLocation == DevExpress.XtraTab.TabHeaderLocation.Bottom ? HorzAlignment.Center :
                (headerLocation == DevExpress.XtraTab.TabHeaderLocation.Left ? HorzAlignment.Near :
                (headerLocation == DevExpress.XtraTab.TabHeaderLocation.Right ? HorzAlignment.Near : HorzAlignment.Center))));

            SetHeaderHAlignment(hAlignment);
        }
        /// <summary>
        /// Nastaví zarovnání textu v záhlaví. Je vhodné nastavit podle pozice záhlaví.
        /// </summary>
        /// <param name="hAlignment"></param>
        protected void SetHeaderHAlignment(HorzAlignment hAlignment)
        {
            AppearancePage.Header.TextOptions.HAlignment = hAlignment;
            AppearancePage.HeaderHotTracked.TextOptions.HAlignment = hAlignment;
            AppearancePage.HeaderActive.TextOptions.HAlignment = hAlignment;
        }
        /// <summary>
        /// Inicializace eventů
        /// </summary>
        private void InitEvents()
        {
            this.SelectedPageChanging += _SelectedPageChanging;
            this.SelectedPageChanged += _SelectedPageChanged;
            this.PageClosing += _PageClosing;
            this.SizeChanged += _SizeChanged;
        }
        /// <summary>
        /// Deaktivuje vlastní eventy
        /// </summary>
        private void RemoveEvents()
        {
            if (this.IsDisposed) return;
            this.SelectedPageChanging -= _SelectedPageChanging;
            this.SelectedPageChanged -= _SelectedPageChanged;
            this.PageClosing -= _PageClosing;
            this.SizeChanged -= _SizeChanged;
        }
        /// <summary>
        /// Při změně vybrané stránky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SelectedPageChanging(object sender, DevExpress.XtraTab.TabPageChangingEventArgs e)
        {
            if (!_SelectedIPageChangingSuppress && e.Page is DxXtraTabPage dxPage)
            {   // Eventy nejsou blokované, a cílová stránka je DxTabPage :
                TEventCancelArgs<IPageItem> args = new TEventCancelArgs<IPageItem>(dxPage.PageData);
                RunSelectedIPageChanging(args);
                if (args.Cancel)
                {   // Aplikace stornovala přepnutí záložky:
                    // Tady má DEX potvrzenou chybu:
                    //  XtraTabControl does not respond when page selection is canceled and the AllowTransition property is set to True
                    //  https://supportcenter.devexpress.com/ticket/details/t943233/xtratabcontrol-does-not-respond-when-page-selection-is-canceled-and-the-allowtransition
                    // Musí být vypnuté : Transition.AllowTransition = DefaultBoolean.False;   !!!
                    e.Cancel = true;
                    return;
                }
            }
        }
        /// <summary>
        /// Po změně vybrané stránky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SelectedPageChanged(object sender, DevExpress.XtraTab.TabPageChangedEventArgs e)
        {
            if (!_SelectedIPageChangedSuppress && e.Page is DxXtraTabPage dxPage)
            {   // Eventy nejsou blokované, a cílová stránka je DxTabPage :
                TEventArgs<IPageItem> args = new TEventArgs<IPageItem>(dxPage.PageData);
                RunSelectedIPageChanged(args);
            }
        }
        /// <summary>
        /// Zavírání konkrétní stránky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PageClosing(object sender, DevExpress.XtraTab.TabPageCancelEventArgs e)
        {
            if (e.Page is DxXtraTabPage dxPage)
            {
                TEventCancelArgs<IPageItem> args = new TEventCancelArgs<IPageItem>(dxPage.PageData);
                RunIPageClosing(args);
                if (!args.Cancel)
                    this.RemovePage(dxPage.PageData);
            }
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Umístění a vzhled záhlaví
        /// </summary>
        public DxPageHeaderPosition PageHeaderPosition { get { return _PageHeaderPosition; } set { ApplyHeaderPosition(value); } }
        private DxPageHeaderPosition _PageHeaderPosition = DxPageHeaderPosition.None;
        /// <summary>
        /// Aplikuje pozici záhlaví
        /// </summary>
        /// <param name="newHeaderPosition"></param>
        protected void ApplyHeaderPosition(DxPageHeaderPosition newHeaderPosition)
        {
            try
            {
                this.BeginUpdate();
                var oldHeaderPosition = _PageHeaderPosition;
                _PageHeaderPosition = newHeaderPosition;             // Od teď platí nová pravidla

                // Změna zobrazení Icon a Text:
                if (!newHeaderPosition.HasEqualsBit(oldHeaderPosition, DxPageHeaderPosition.IconOnly) || !newHeaderPosition.HasEqualsBit(oldHeaderPosition, DxPageHeaderPosition.TextOnly))
                    DxPages.ForEachExec(p => p.RefreshData());

                // Pozice záhlaví, orientace:
                var headerLocation = GetTabHeaderLocation(newHeaderPosition);
                if (headerLocation.HasValue)
                {   // Záhlaví jsou zobrazena:
                    HeaderLocation = headerLocation.Value;
                    HeaderOrientation = GetTabHeaderOrientation(newHeaderPosition);
                    ShowTabHeader = DefaultBoolean.True;
                    SetHeaderHAlignment(headerLocation.Value);
                }
                else
                {   // Bez záhlaví:
                    ShowTabHeader = DefaultBoolean.False;
                }
            }
            finally
            {
                this.EndUpdate();
                _CheckHeaderSizeChangeForce();
            }
        }
        /// <summary>
        /// Konverze typu <see cref="DxPageHeaderPosition"/> na <see cref="DevExpress.XtraTab.TabHeaderLocation"/>
        /// </summary>
        /// <param name="headerPosition"></param>
        /// <returns></returns>
        protected static DevExpress.XtraTab.TabHeaderLocation? GetTabHeaderLocation(DxPageHeaderPosition headerPosition)
        {
            if (headerPosition.HasFlag(DxPageHeaderPosition.PositionTop)) return DevExpress.XtraTab.TabHeaderLocation.Top;
            if (headerPosition.HasFlag(DxPageHeaderPosition.PositionBottom)) return DevExpress.XtraTab.TabHeaderLocation.Bottom;
            if (headerPosition.HasFlag(DxPageHeaderPosition.PositionLeft)) return DevExpress.XtraTab.TabHeaderLocation.Left;
            if (headerPosition.HasFlag(DxPageHeaderPosition.PositionRight)) return DevExpress.XtraTab.TabHeaderLocation.Right;
            return null;
        }
        /// <summary>
        /// Konverze typu <see cref="DxPageHeaderPosition"/> na <see cref="DevExpress.XtraTab.TabOrientation"/>
        /// </summary>
        /// <param name="headerPosition"></param>
        /// <returns></returns>
        protected static DevExpress.XtraTab.TabOrientation GetTabHeaderOrientation(DxPageHeaderPosition headerPosition)
        {
            if ((headerPosition.HasFlag(DxPageHeaderPosition.PositionLeft) || headerPosition.HasFlag(DxPageHeaderPosition.PositionRight)) && headerPosition.HasFlag(DxPageHeaderPosition.VerticalText)) return DevExpress.XtraTab.TabOrientation.Vertical;
            return DevExpress.XtraTab.TabOrientation.Horizontal;
        }
        /// <summary>
        /// Umístění záložek na začátek / střed / konec
        /// </summary>
        public AlignContentToSide PageHeaderAlignment { get { return _PageHeaderAlignment; } set { ApplyHeaderAlignment(value); } }
        private AlignContentToSide _PageHeaderAlignment;
        /// <summary>
        /// Aplikuje zarovnání záhlaví
        /// </summary>
        /// <param name="headerAlignment"></param>
        protected void ApplyHeaderAlignment(AlignContentToSide headerAlignment)
        {
            // Zatím nevím jak nastavit jinou hodnotu:
            this._PageHeaderAlignment = AlignContentToSide.Begin;
        }
        /// <summary>
        /// Možnost zobrazit více řádek záhlaví
        /// </summary>
        public bool PageHeaderMultiLine
        {
            get { return (this.MultiLine == DefaultBoolean.True); }
            set
            {
                this.MultiLine = (value ? DefaultBoolean.True : DefaultBoolean.False);
                // Když je možno zobrazit více řádků, nepotřebujeme buttony Prev a Next; a naopak:
                this.HeaderButtons = (value ? DevExpress.XtraTab.TabButtons.None : DevExpress.XtraTab.TabButtons.Prev | DevExpress.XtraTab.TabButtons.Next);
            }
        }
        #endregion
        #region Pages
        /// <summary>
        /// Stránky datové.
        /// Lze i setovat. Při setování se control pokusí ponechat jako selectovanou stránku tu, která je aktuálně selectovaná, podle jejího <see cref="ITextItem.ItemId"/>.
        /// Pokud to nebude možné (v nové sadě stránek nebude takové ID existovat), pak vyselectuje první existující stránku a vyvolá událost <see cref="SelectedIPageChanged"/>
        /// </summary>
        public IPageItem[] IPages
        {
            get { return this.DxPages.Select(p => p.PageData).ToArray(); }
            set { SetPages(value, null, true, true); }
        }
        /// <summary>
        /// Stránky vizuální
        /// </summary>
        protected DxXtraTabPage[] DxPages { get { return this.TabPages.OfType<DxXtraTabPage>().ToArray(); } }
        /// <summary>
        /// Počet stránek
        /// </summary>
        public int IPageCount { get { return IPages.Length; } }
        /// <summary>
        /// Data aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public IPageItem SelectedIPage
        {
            get { return this.GetGuiValue<IPageItem>(() => GetGuiIPage()); }
            set { this.SetGuiValue<IPageItem>(v => SetGuiIPage(v, false, false), value); }
        }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public string SelectedIPageId
        {
            get { return this.GetGuiValue<string>(() => GetGuiIPage()?.ItemId); }
            set { this.SetGuiValue<string>(v => SetGuiPageId(v, false, false), value); }
        }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně NEPROBĚHNE událost <see cref="SelectedIPageChanging"/>, pouze proběhne <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public string SelectedIPageIdForce
        {
            get { return this.GetGuiValue<string>(() => GetGuiIPage()?.ItemId); }
            set { this.SetGuiValue<string>(v => SetGuiPageId(v, true, false), value); }
        }
        /// <summary>
        /// <see cref="ITextItem.ItemId"/> aktuálně vybrané stránky nebo null pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně NEPROBĚHNE žádná událost, ani <see cref="SelectedIPageChanging"/>, ani <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public string SelectedIPageIdSilent
        {
            get { return this.GetGuiValue<string>(() => GetGuiIPage()?.ItemId); }
            set { this.SetGuiValue<string>(v => SetGuiPageId(v, true, true), value); }
        }
        /// <summary>
        /// Prostý index aktuálně vybrané stránky nebo -1 pokud <see cref="IPageCount"/> == 0.
        /// Lze setovat. Při změně proběhne událost <see cref="SelectedIPageChanging"/> a poté <see cref="SelectedIPageChanged"/>.
        /// <para/>
        /// Lze setovat i z threadu na pozadí
        /// </summary>
        public int SelectedIPageIndex
        {
            get { return this.GetGuiValue<int>(() => GetGuiIndex()); }
            set { this.SetGuiValue<int>(v => SetGuiIndex(v, true, false), value); }
        }
        /// <summary>
        /// Vrátí aktuální stránku <see cref="IPageItem"/>, provádí se v GUI threadu
        /// </summary>
        /// <returns></returns>
        private IPageItem GetGuiIPage()
        {
            var tabPage = this.SelectedTabPage;
            return (tabPage != null && tabPage is DxXtraTabPage dxPage) ? dxPage.PageData : null;
        }
        /// <summary>
        /// Setuje hodnotu SelectedPage na danou stránku, potlačí eventy, provádí se v GUI threadu
        /// </summary>
        /// <param name="iPage"></param>
        /// <param name="suppressChanging"></param>
        /// <param name="suppressChanged"></param>
        private void SetGuiIPage(IPageItem iPage, bool suppressChanging, bool suppressChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = suppressChanging;
                _SelectedIPageChangedSuppress = suppressChanged;

                this.SelectedTabPage = SearchDxPage(iPage);
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }
        /// <summary>
        /// Setuje hodnotu SelectedPage na danou stránku, potlačí eventy, provádí se v GUI threadu
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="suppressChanging"></param>
        /// <param name="suppressChanged"></param>
        private void SetGuiPageId(string pageId, bool suppressChanging, bool suppressChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = suppressChanging;
                _SelectedIPageChangedSuppress = suppressChanged;

                this.SelectedTabPage = SearchDxPage(pageId);
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }
        /// <summary>
        /// Vrátí index aktuální stránky, provádí se v GUI threadu
        /// </summary>
        /// <returns></returns>
        private int GetGuiIndex()
        {
            return this.SelectedTabPageIndex;
        }
        /// <summary>
        /// Setuje index aktuální stránky na danou stránku, potlačí eventy, provádí se v GUI threadu
        /// </summary>
        /// <param name="index"></param>
        /// <param name="suppressChanging"></param>
        /// <param name="suppressChanged"></param>
        private void SetGuiIndex(int index, bool suppressChanging, bool suppressChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = suppressChanging;
                _SelectedIPageChangedSuppress = suppressChanged;

                this.SelectedTabPageIndex = index;
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }

        /// <summary>
        /// Vyvolá metodu <see cref="OnSelectedIPageChanging(TEventCancelArgs{IPageItem})"/> a event <see cref="SelectedIPageChanging"/>, 
        /// bez podmínky na <see cref="_SelectedIPageChangedSuppress"/>
        /// </summary>
        /// <param name="args"></param>
        private void RunSelectedIPageChanging(TEventCancelArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnSelectedIPageChanging(args);
                SelectedIPageChanging?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Událost volaná při pokusu o aktivaci jiné záložky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, nová stránka bude aktivována.
        /// </summary>
        protected virtual void OnSelectedIPageChanging(TEventCancelArgs<IPageItem> args) { }
        /// <summary>
        /// Volá se při pokusu o aktivaci jiné záložky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, nová stránka bude aktivována.
        /// </summary>
        public event EventHandler<TEventCancelArgs<IPageItem>> SelectedIPageChanging;

        /// <summary>
        /// Vyvolá metodu <see cref="OnSelectedIPageChanged"/> a event <see cref="SelectedIPageChanged"/>, bez podmínky na <see cref="_SelectedIPageChangedSuppress"/>
        /// </summary>
        private void RunSelectedIPageChanged(TEventArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnSelectedIPageChanged(args);
                SelectedIPageChanged?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Událost volaná při změně aktivní stránky <see cref="SelectedIPage"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSelectedIPageChanged(TEventArgs<IPageItem> args) { }
        /// <summary>
        /// Událost volaná při změně aktivní stránky <see cref="SelectedIPage"/>
        /// </summary>
        public event EventHandler<TEventArgs<IPageItem>> SelectedIPageChanged;
        /// <summary>
        /// Hodnota true potlačí volání události <see cref="SelectedIPageChanging"/>, ale nepotlačí volání události <see cref="SelectedIPageChanged"/>, 
        /// použije se při interních změnách obsahu, 
        /// když víme že na konci změn nastavíme správnou hodnotu a/nebo vyvoláme explicitně event.
        /// </summary>
        private bool _SelectedIPageChangingSuppress = false;
        /// <summary>
        /// Hodnota true potlačí volání události <see cref="SelectedIPageChanged"/>, 
        /// použije se při interních změnách obsahu, 
        /// když víme že na konci změn nastavíme správnou hodnotu a/nebo vyvoláme explicitně event.
        /// </summary>
        private bool _SelectedIPageChangedSuppress = false;
        /// <summary>
        /// Zjistí, zda na zadané relativní souřadnici se nachází nějaké záhlaví, a pokud ano pak najde odpovídající stránku <see cref="IPageItem"/>.
        /// </summary>
        /// <param name="relativePoint">Souřadnice relativní k this controlu</param>
        /// <param name="iPage">Výstup = nalezená stránka<see cref="IPageItem"/></param>
        /// <returns></returns>
        public bool TryFindTabHeader(Point relativePoint, out IPageItem iPage)
        {
            var hit = this.CalcHitInfo(relativePoint);
            if (hit != null && hit.IsValid && hit.Page is DxXtraTabPage dxPage)
            {
                iPage = dxPage.PageData;
                return (iPage != null);
            }
            iPage = null;
            return false;
        }

        /// <summary>
        /// Smaže všechny stránky
        /// </summary>
        public void ClearPages()
        {
            _AddPages(true, null, null, true, true);
        }
        /// <summary>
        /// Smaže aktuální stránky, vloží dodanou sadu, a poté se pokusí reaktivovat nově dodanou stránku se shodným ID jaké měla dosud aktivní stránka.
        /// Pokud nebyla nebo ji nenajde, pak aktivuje stránku s dodaným ID, anebo první dodanou stránku.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="selectPageId"></param>
        /// <param name="callEventChanging">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanging"/></param>
        /// <param name="callEventChanged">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanged"/></param>
        public void SetPages(IEnumerable<IPageItem> pages, string selectPageId, bool callEventChanging, bool callEventChanged)
        {
            _AddPages(true, pages, selectPageId, callEventChanging, callEventChanged);
        }
        /// <summary>
        /// Přidá dané stránky.
        /// </summary>
        /// <param name="pages"></param>
        public void AddPages(IEnumerable<IPageItem> pages)
        {
            _AddPages(false, pages, null, true, true);
        }
        /// <summary>
        /// Přidá danou stránku.
        /// </summary>
        /// <param name="page"></param>
        public void AddPage(IPageItem page)
        {
            if (page != null)
                _AddPages(false, new IPageItem[] { page }, null, true, true);
        }
        /// <summary>
        /// Přidá stránky
        /// </summary>
        /// <param name="clear"></param>
        /// <param name="pages"></param>
        /// <param name="selectPageId"></param>
        /// <param name="callEventChanging">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanging"/></param>
        /// <param name="callEventChanged">true = při reálné změně stránky volat událost <see cref="SelectedIPageChanged"/></param>
        private void _AddPages(bool clear, IEnumerable<IPageItem> pages, string selectPageId, bool callEventChanging, bool callEventChanged)
        {
            var oldPage = SelectedIPage;
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            bool isActiveOldPage = false;
            bool runEventChanged = false;
            bool hasPages = this.TabPages.Count > 0;
            Size headerSizeOld = ((hasPages && this.ViewInfo?.HeaderInfo != null) ? this.ViewInfo.HeaderInfo.Bounds.Size : Size.Empty);
            DxXtraTabPage activatePage = null;
            IPageItem[] allPages = null;
            try
            {
                this.BeginUpdate();
                using (this.ScopeSuspendParentLayout())
                {
                    _SelectedIPageChangingSuppress = true;
                    _SelectedIPageChangedSuppress = true;
                    bool forceResize = !hasPages;

                    if (clear)
                        this.TabPages.Clear();

                    if (pages != null)
                    {
                        foreach (var page in pages)
                        {
                            if (page != null)
                            {
                                DxXtraTabPage dxPage = new DxXtraTabPage(this, page);
                                this.TabPages.Add(dxPage);
                                if (forceResize)
                                {   // Po přidání úplně první stránky do TabHeaders:
                                    _CheckHeaderSizeChange(true, headerSizeOld);
                                    forceResize = false;
                                }
                            }
                        }
                    }
                    allPages = this.IPages;

                    bool hasOldPage = (oldPage != null);
                    isActiveOldPage = (hasOldPage && Object.ReferenceEquals(oldPage, this.SelectedIPage));
                    if (hasOldPage && !isActiveOldPage && allPages.Length > 0)
                    {   // Pokud dříve byla nějaká stránka aktivní, a nyní je aktivní jiná, pak se pokusím reaktivovat původní stránku:
                        activatePage = this.SearchDxPage(oldPage);
                        if (activatePage != null)
                        {
                            this.SelectedTabPage = activatePage;     // Fyzicky aktivuji stránku, ale event SelectedIPageChanged je potlačený (_SelectedIPageChangedSuppress) = takže se neprovede
                            isActiveOldPage = true;
                        }
                    }

                    // Pokud se mi nepodařilo aktivovat původní stránku (oldPage), tak nejspíš proto že původně nebylo nic, anebo jsme původní stránku smazali;
                    // pak nyní najdu vhodnou stránku a vložím ji do 
                    if (!isActiveOldPage && allPages != null)
                    {
                        activatePage = null;
                        if (allPages.Length == 0)
                        {   // Nyní nemám žádné stránky,
                            // a pokud jsem dosud měl nějakou aktivní, tak zavoláme event (změna Page => null):
                            runEventChanged = (oldPage != null);
                        }
                        else
                        {
                            if (selectPageId != null)
                                activatePage = this.SearchDxPage(selectPageId);
                            if (activatePage == null)
                                activatePage = this.TabPages[0] as DxXtraTabPage;
                            runEventChanged = true;
                        }
                    }
                }
            }
            finally
            {
                this.EndUpdate();
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
                _CheckHeaderSizeChangeForce(headerSizeOld);
            }

            if (runEventChanged)
                this.SelectPage(activatePage, callEventChanging, callEventChanged);
        }
        /// <summary>
        /// Selectuje danou stránku, vyvolá přitom požadované události
        /// </summary>
        /// <param name="activatePage"></param>
        /// <param name="callEventChanging"></param>
        /// <param name="callEventChanged"></param>
        protected void SelectPage(DxXtraTabPage activatePage, bool callEventChanging, bool callEventChanged)
        {
            bool isChangingSuppress = _SelectedIPageChangingSuppress;
            bool isChangedSuppress = _SelectedIPageChangedSuppress;
            try
            {
                _SelectedIPageChangingSuppress = !callEventChanging; // callEventChanging = true => volat event => NEpotlačit volání eventu !
                _SelectedIPageChangedSuppress = !callEventChanged;
                this.SelectedTabPage = activatePage;
            }
            finally
            {
                _SelectedIPageChangingSuppress = isChangingSuppress;
                _SelectedIPageChangedSuppress = isChangedSuppress;
            }
        }
        /// <summary>
        /// Odstraní danou stránku podle jejího ID
        /// </summary>
        /// <param name="itemId"></param>
        public void RemovePage(string itemId)
        {
            _RemovePage(SearchDxPage(itemId));
        }
        /// <summary>
        /// Odstraní danou stránku podle jejího objektu
        /// </summary>
        /// <param name="iPage"></param>
        public void RemovePage(IPageItem iPage)
        {
            _RemovePage(SearchDxPage(iPage));
        }
        /// <summary>
        /// Odstraní danou stránku
        /// </summary>
        /// <param name="dxPage"></param>
        private void _RemovePage(DxXtraTabPage dxPage)
        {
            if (dxPage != null)
            {
                Size headerSizeOld = ((this.TabPages.Count > 0 && this.ViewInfo?.HeaderInfo != null) ? this.ViewInfo.HeaderInfo.Bounds.Size : Size.Empty);
                if (Object.ReferenceEquals(this.SelectedTabPage, dxPage))
                    _SelectAnyNearPage(dxPage);
                this.TabPages.Remove(dxPage);
                RunIPageRemoved(new TEventArgs<IPageItem>(dxPage.PageData));
                _CheckHeaderSizeChange(true, headerSizeOld);
            }
        }
        /// <summary>
        /// Aktivuje nejbližší sousední stránku ke stránce dané
        /// </summary>
        /// <param name="dxPage"></param>
        private void _SelectAnyNearPage(DxXtraTabPage dxPage)
        {
            if (dxPage is null) return;
            int index = this.TabPages.IndexOf(dxPage);
            if (index < 0) return;
            int last = this.TabPages.Count - 1;
            if (index < last)
                this.SelectedTabPageIndex = index + 1;
            else if (index > 0)
                this.SelectedTabPageIndex = index - 1;
        }
        /// <summary>
        /// Uživatel kliknul na křížek, a chce zavřít stránku. 
        /// Zavírací křížek je uživateli zobrazen proto, že stránka je deklarovaná s <see cref="IPageItem.CloseButtonVisible"/> = true.
        /// </summary>
        /// <param name="args"></param>
        private void RunIPageClosing(TEventCancelArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnIPageClosing(args);
                IPageClosing?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Volá se při pokusu o zavírání stránky.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIPageClosing(TEventCancelArgs<IPageItem> args) { }
        /// <summary>
        /// Volá se při pokusu o zavírání stránky.
        /// Eventhandler může zavření potlačit nastavením Cancel = true. Pokud to nenastaví, stránka bude zavřena.
        /// </summary>
        public event EventHandler<TEventCancelArgs<IPageItem>> IPageClosing;
        /// <summary>
        /// Proběhlo odstranění stránky (záložky).
        /// </summary>
        /// <param name="args"></param>
        private void RunIPageRemoved(TEventArgs<IPageItem> args)
        {
            if (this.IsPrepared)
            {
                OnIPageRemoved(args);
                IPageRemoved?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Volá se po zavření (odebrání) stránky (záložky).
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIPageRemoved(TEventArgs<IPageItem> args) { }
        /// <summary>
        /// Volá se po zavření (odebrání) stránky (záložky).
        /// </summary>
        public event EventHandler<TEventArgs<IPageItem>> IPageRemoved;
        /// <summary>
        /// Najde fyzickou stránku pro danou datovou stránku, nebo pro její ID
        /// </summary>
        /// <param name="iPage"></param>
        /// <returns></returns>
        private DxXtraTabPage SearchDxPage(IPageItem iPage)
        {
            DxXtraTabPage dxPage = null;
            if (iPage != null)
            {
                DxXtraTabPage[] dxPages = DxPages;
                dxPage = dxPages.FirstOrDefault(p => Object.ReferenceEquals(p.PageData, iPage));
                if (dxPage == null && iPage.ItemId != null)
                    dxPage = dxPages.FirstOrDefault(p => String.Equals(p.PageData.ItemId, iPage.ItemId));
            }
            return dxPage;
        }
        /// <summary>
        /// Najde fyzickou stránku pro dané ID stránky
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private DxXtraTabPage SearchDxPage(string itemId)
        {
            DxXtraTabPage dxPage = null;
            if (itemId != null)
            {
                DxXtraTabPage[] dxPages = DxPages;
                dxPage = dxPages.FirstOrDefault(p => String.Equals(p.PageData.ItemId, itemId));
            }
            return dxPage;
        }
        #endregion
        #region Výška a šířka prostoru záhlaví
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SizeChanged(object sender, EventArgs e)
        {
            _CheckHeaderSizeChange();
        }
        /// <summary>
        /// Ověří, zda nedošlo ke změně výšky nebo šířky záhlaví (podle orientace), a pokud ano, pak vyvolá patřičné události.
        /// Použije brutální sílu k prolomení neproniknutelné hradby DevExpress.
        /// </summary>
        private void _CheckHeaderSizeChangeForce(Size? headerSizeOld = null)
        {   // Musí být dvakrát, jinak špatně funguje změna z nuly na více záložek, nebo změna u levého i pravého umístění.
            _CheckHeaderSizeChange(true);
            _CheckHeaderSizeChange(true);
        }
        /// <summary>
        /// Ověří, zda nedošlo ke změně výšky nebo šířky záhlaví (podle orientace), a pokud ano, pak vyvolá patřičné události.
        /// </summary>
        private void _CheckHeaderSizeChange(bool force = false, Size? headerSizeOld = null)
        {
            if (this.Parent is null) return;

            if (!_CheckHeaderSizeInProgress || force)
            {
                bool oldInProgress = _CheckHeaderSizeInProgress;
                try
                {
                    _CheckHeaderSizeInProgress = true;
                    var headerPosition = this.PageHeaderPosition;
                    if (headerPosition.HasFlag(DxPageHeaderPosition.PositionTop) || headerPosition.HasFlag(DxPageHeaderPosition.PositionBottom))
                        _CheckHeaderHeightChange(force, headerSizeOld);
                    else if (headerPosition.HasFlag(DxPageHeaderPosition.PositionLeft) || headerPosition.HasFlag(DxPageHeaderPosition.PositionRight))
                        _CheckHeaderWidthChange(force, headerSizeOld);
                }
                finally
                {
                    _CheckHeaderSizeInProgress = oldInProgress;
                }
            }
        }
        private bool _CheckHeaderSizeInProgress = false;
        /// <summary>
        /// Ověří, zda nedošlo ke změně výšky záhlaví (pro záhlaví umístěné Top nebo Bottom),
        /// a pokud ano, pak vyvolá patřičné události.
        /// </summary>
        private void _CheckHeaderHeightChange(bool force, Size? headerSizeOld)
        {
            _HeaderWidth = 0;
            if (this.Width < 10) return;

            int headerHeight = 0;
            if (this.TabPages.Count > 0)
            {
                int minHeight = 10;

                // První naplnění stránkami dělá problémy, protože velikost může být menší než je potřebná, a pak ViewInfo obsahuje "náhradní záporné hodnoty".
                // To ale tady nechceme!
                if (force || this.Height <= minHeight)
                {
                    if (this.Height <= minHeight)
                        this.Height = 50;
                    this.ViewInfo.Resize();
                    this.Refresh();
                }
                bool isTop = !this.PageHeaderPosition.HasFlag(DxPageHeaderPosition.PositionBottom);
                var headerBounds = this.ViewInfo?.HeaderInfo?.Bounds;
                if (headerBounds.HasValue)
                {   // Čistá cesta:
                    headerHeight = headerBounds.Value.Height;

                    // Kontroly, korekce, náhrady:
                    if (headerHeight >= minHeight)
                        headerHeight += (isTop ? 1 : 1);             // Máme výšku určenou: přidáme 1px pro okraj
                    else if (headerSizeOld.HasValue)
                        headerHeight = headerSizeOld.Value.Height;   // Výška je nyní určena špatně, ale máme info od posledně
                    else
                        headerHeight = 40;                           // Nouzový stav, snad nebude nastávat často
                }
                else
                {   // Náhradní cesta:
                    headerHeight = (isTop ? (this.DisplayRectangle.Y + 2) : (this.ClientSize.Height - this.DisplayRectangle.Height));
                }
                if (headerHeight < minHeight) headerHeight = minHeight;
            }
            if (headerHeight != _HeaderHeight)
            {   // Výška záhlaví byla změněna:
                _HeaderHeight = headerHeight;
                OnHeaderSizeChanged();
                HeaderSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Ověří, zda nedošlo ke změně šířky záhlaví (pro záhlaví umístěné Left nebo Right),
        /// a pokud ano, pak vyvolá patřičné události.
        /// </summary>
        private void _CheckHeaderWidthChange(bool force, Size? headerSizeOld)
        {
            _HeaderHeight = 0;
            if (this.Height < 10) return;

            int headerWidth = 0;
            if (this.TabPages.Count > 0)
            {
                int minWidth = 15;

                // První naplnění stránkami dělá problémy, protože velikost může být menší než je potřebná, a pak ViewInfo obsahuje "náhradní záporné hodnoty".
                // To ale tady nechceme!
                if (force || this.Width <= minWidth)
                {
                    if (this.Width <= minWidth)
                        this.Width = 50;
                    this.ViewInfo.Resize();
                    this.Refresh();
                }

                var headerBounds = this.ViewInfo?.HeaderInfo?.Bounds;
                if (headerBounds.HasValue)
                {   // Čistá cesta:
                    headerWidth = headerBounds.Value.Width;

                    // Kontroly, korekce, náhrady:
                    if (headerWidth >= minWidth)
                        headerWidth -= 1;                            // Máme výšku určenou: odebereme 1px pro okraj
                    else if (headerSizeOld.HasValue)
                        headerWidth = headerSizeOld.Value.Width;     // Výška je nyní určena špatně, ale máme info od posledně
                    else
                        headerWidth = 40;                            // Nouzový stav, snad nebude nastávat často
                }
                else
                {   // Náhradní cesta:
                    bool isLeft = !this.PageHeaderPosition.HasFlag(DxPageHeaderPosition.PositionRight);
                    headerWidth = (isLeft ? this.DisplayRectangle.X : this.Bounds.Width - this.DisplayRectangle.Width);
                }
                if (headerWidth < minWidth) headerWidth = minWidth;
                headerWidth = headerWidth + 2;
            }
            if (headerWidth != _HeaderWidth)
            {   // Šířka záhlaví byla změněna:
                _HeaderWidth = headerWidth;
                OnHeaderSizeChanged();
                HeaderSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aktuálně prověří velikost záhlaví <see cref="HeaderHeight"/> a/nebo <see cref="HeaderWidth"/>.
        /// </summary>
        public void CheckHeaderSize() { _CheckHeaderSizeChangeForce(); }
        /// <summary>
        /// Aktuální výška záhlaví. 
        /// Je nastavováno pouze pro záhlaví umístěné Top nebo Bottom.
        /// </summary>
        public int HeaderHeight { get { return _HeaderHeight; } }
        private int _HeaderHeight;
        /// <summary>
        /// Aktuální šířka záhlaví. 
        /// Je nastavováno pouze pro záhlaví umístěné Left nebo Right.
        /// </summary>
        public int HeaderWidth { get { return _HeaderWidth; } }
        private int _HeaderWidth;
        /// <summary>
        /// Při změně výšky nebo šířky záhlaví:
        /// výška <see cref="HeaderHeight"/> pro pozici záhlaví Top nebo Bottom;
        /// šířka <see cref="HeaderWidth"/> pro pozici záhlaví Left nebo Right;
        /// </summary>
        protected virtual void OnHeaderSizeChanged() { }
        /// <summary>
        /// Došlo ke změně výšky nebo šířky záhlaví:
        /// výška <see cref="HeaderHeight"/> pro pozici záhlaví Top nebo Bottom;
        /// šířka <see cref="HeaderWidth"/> pro pozici záhlaví Left nebo Right;
        /// </summary>
        public event EventHandler HeaderSizeChanged;
        /// <summary>
        /// Metoda, která zapisuje do trace
        /// </summary>
        public Action<Type, string, string, string, string, string> TraceMethod { get; set; }
        #endregion
        #region IListenerStyleChanged, IListenerZoomChange
        void IListenerStyleChanged.StyleChanged() { this._CheckHeaderSizeChange(); }
        void IListenerZoomChange.ZoomChanged() { this._CheckHeaderSizeChange(); }
        #endregion
    }
    #region DxXtraTabPage : jedna stránka v DxXtraTabControl
    /// <summary>
    /// Jedna stránka
    /// </summary>
    public class DxXtraTabPage : DevExpress.XtraTab.XtraTabPage
    {
        /// <summary>
        /// Konstruktor, nepoužívat v aplikaci
        /// </summary>
        public DxXtraTabPage()
        {
            this.IsPrepared = true;
        }
        /// <summary>
        /// Konstruktor s daty
        /// </summary>
        /// <param name="tabOwner"></param>
        /// <param name="pageData"></param>
        public DxXtraTabPage(DxXtraTabControl tabOwner, IPageItem pageData)
        {
            _TabOwner = tabOwner;
            PageData = pageData;
            RefreshData();
            this.IsPrepared = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.PageData?.ToString() ?? this.Text;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.IsPrepared = false;
            if (PageData != null)
            {
                PageData.PageControl = null;
                PageData = null;
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Obsahuje true v době, kdy control je korektně inicializován (na true se nastaví na konci konstruktoru),
        /// a pouze do doby, neý začíná Dispose (na false se nastaví na začátku Dispose).
        /// </summary>
        public bool IsPrepared { get; protected set; }
        /// <summary>
        /// Vlastník
        /// </summary>
        protected DxXtraTabControl TabOwner { get { return _TabOwner?.Target; } }
        private WeakTarget<DxXtraTabControl> _TabOwner;
        /// <summary>
        /// Data stránky
        /// </summary>
        public IPageItem PageData { get; private set; }
        /// <summary>
        /// Do this stránky refreshuje data ze svého objektu <see cref="IPageItem"/>
        /// </summary>
        public void RefreshData()
        {
            var pageData = PageData;
            if (pageData is null) return;

            var headerPosition = TabOwner?.PageHeaderPosition ?? DxPageHeaderPosition.Top;
            bool hasText = headerPosition.HasFlag(DxPageHeaderPosition.TextOnly);
            bool hasIcon = headerPosition.HasFlag(DxPageHeaderPosition.IconOnly);

            Name = pageData.ItemId;
            Text = (hasText ? pageData.Text : "");
            SuperTip = DxComponent.CreateDxSuperTip(pageData);

            this.ShowCloseButton = (pageData.CloseButtonVisible ? DefaultBoolean.True : DefaultBoolean.False);

            if (hasIcon)
                DxComponent.ApplyImage(ImageOptions, pageData.ImageName, sizeType: ResourceImageSizeType.Medium);
            else
                ImageOptions.Reset();

            pageData.PageControl = this;
        }
    }
    #endregion
    #endregion
}
