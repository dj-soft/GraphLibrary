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

using DevExpress.Utils;
using System.Diagnostics;
using DevExpress.Utils.Extensions;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using XS = Noris.WS.Parser.XmlSerializer;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Potomek Ribbonu
    /// </summary>
    public class DxRibbonControl : DevExpress.XtraBars.Ribbon.RibbonControl, IDxRibbonInternal, IListenerApplicationIdle, IListenerLightDarkChanged, IListenerZoomChange
    {
        #region Konstruktor a public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonControl()
        {
            InitBarManagers();
            InitSystemProperties();
            InitData();
            InitEvents();
            InitQuickAccessToolbar();
            InitSearchItem();
            DxComponent.RegisterListener(this);
            DxQuickAccessToolbar.ConfigValueChanged += _DxQATItemKeysChanged;
            IsActive = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                this.DestroyContent();
                this.DxDisposed = true;
                DxComponent.UnregisterListener(this);
                DxQuickAccessToolbar.ConfigValueChanged -= _DxQATItemKeysChanged;
                base.Dispose(disposing);
            }
            catch (Exception exc)
            { /* Zavření jednoho okna by nemělo shodit klienta jako celek */
                DxComponent.LogAddException(exc);
            }
        }
        /// <summary>
        /// Zruší veškerý svůj obsah v procesu Dispose. Volá base.DestroyContent() !!!
        /// </summary>
        protected virtual void DestroyContent()
        {
            Images = null;
            LargeImages = null;
            SearchItemShortcut = null;
            ToolTipController = null;

            _Groups?.ForEach(g => g?.Dispose());
            _Groups = null;

            SelectedDxPageChanged = null;

            _SearchEditItems = null;
            _AddedSearchMenuItems = null;

            _OpenItemPopupMenu = null;
            _OpenItemBarMenu = null;

            PageOnDemandLoad = null;
            ItemOnDemandLoad = null;

            _QATUserItems?.ForEach(q => q?.Dispose());
            _QATUserItems?.Clear();
            _QATUserItems = null;
            _QATUserItemDict?.Clear();
            _QATUserItemDict = null;

            _QATDirectItems?.ForEach(q => q?.Dispose());
            _QATDirectItems = null;

            ImageRightDestroy();

            SearchMenuDestroyContent();
        }
        /// <summary>
        /// Nastaví základní systémové vlastnosti Ribbonu.
        /// </summary>
        private void InitSystemProperties()
        {
            Images = DxComponent.GetPreferredImageList(RibbonImageSize);
            LargeImages = DxComponent.GetPreferredImageList(RibbonLargeImageSize);

            AllowKeyTips = true;
            ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
            RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonControlStyle.Office2019;
            CommandLayout = DevExpress.XtraBars.Ribbon.CommandLayout.Classic;
            DrawGroupCaptions = DevExpress.Utils.DefaultBoolean.True;
            DrawGroupsBorderMode = DevExpress.Utils.DefaultBoolean.True;
            GalleryAnimationLength = 300;
            GroupAnimationLength = 300;
            ItemAnimationLength = 300;
            ItemsVertAlign = DevExpress.Utils.VertAlignment.Center;                      // Svislé zarovnání prvků, když mám 1 až 2 malé buttony v třířádkovém Ribbonu
            OptionsAnimation.PageCategoryShowAnimation = DevExpress.Utils.DefaultBoolean.True;
            RibbonCaptionAlignment = DevExpress.XtraBars.Ribbon.RibbonCaptionAlignment.Center;
            SearchItemShortcut = new DevExpress.XtraBars.BarShortcut(Keys.Control | Keys.F);
            ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;        //  True;
            ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.True;
            ShowMoreCommandsButton = DevExpress.Utils.DefaultBoolean.True;
            ShowPageHeadersMode = DevExpress.XtraBars.Ribbon.ShowPageHeadersMode.Show;
            ShowSearchItem = true;

            ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            ApplicationButtonText = DxComponent.Localize(MsgCode.RibbonAppHomeText);
            ToolTipController = DxComponent.DefaultRibbonToolTipController;

            Margin = new System.Windows.Forms.Padding(2);
            MdiMergeStyle = DefaultMdiMergeStyleForms;
            // this.ShouldMergeActivate

            AllowMinimizeRibbon = false;    // Povolit minimalizaci Ribbonu? Pak ale nejde vrátit :-(
            AllowCustomization = false;     // Hodnota true povoluje (na pravé myši) otevřít okno Customizace Ribbonu, a to v Greenu nepodporujeme
            ShowQatLocationSelector = true; // Hodnota true povoluje změnu umístění Ribbonu
            ShowTextInQAT = DefaultShowTextInQAT;

            AllowGlyphSkinning = false;     // Nikdy ne true!
            ShowItemCaptionsInQAT = true;

            SelectChildActivePageOnMerge = true;
            UseLazyContentCreate = LazyContentMode.DefaultLazy;
            CheckLazyContentEnabled = true;


            SearchMenuGroupSort = SearchMenuGroupSortMode.PageOrderGroupCaption;
            SearchMenuMaxResultCount = 24;
            SearchMenuShrinkResultCount = 0;

            ImageRightInit();
            RemoveItemsShortcuts();
            Visible = true;
            DxDisposed = false;
        }
        /// <summary>
        /// Nastaví ImageListy pro Ribbon, podle aktuálně platné předvolby <see cref="DxComponent.IsPreferredVectorImage"/> (Vektor /Retro)
        /// </summary>
        /// <param name="force"></param>
        internal void InitImageLists(bool force = false)
        {
            Images = DxComponent.GetPreferredImageList(RibbonImageSize);
            LargeImages = DxComponent.GetPreferredImageList(RibbonLargeImageSize);

            // Projde všechny BarItem, které jsou v evidenci Ribbonu, i Grupy v Ribonu, a do každé znovu aplikuje Image.
            // Nyní je připraven vhodný ImageList, a Indexy obrázku tedy budou odpovídat novému ImageListu:
            ReloadImages();
        }
        /// <summary>
        /// Nastaví základní uživatelské vlastnosti Ribbonu.
        /// </summary>
        /// <param name="forDesktop"></param>
        internal void InitUserProperties(bool forDesktop = false)
        {
            CommandLayout = CommandLayout.Classic;                                         // RMC 0065065 17.04.2020 Změna výchozího skinu a Ribbonu - změněno na Classic
            AllowContentChangeAnimation = DevExpress.Utils.DefaultBoolean.False;           // JD 0069074 13.07.2021 - zamezí duplicitnímu refresh záložek ribbonu ve skinech Nephrite
            ApplicationButtonAnimationLength = 0;                                          // JD 0069074 13.07.2021 - vypnutí animací
            GalleryAnimationLength = 0;
            PageAnimationLength = 0;
            GroupAnimationLength = 0;
            ItemAnimationLength = 0;                                                       // zamezí obrazení černého orámování kolem ikon po najetí myši u nevektorových skinů

            ToolbarLocation = DxQuickAccessToolbar.QATLocation;                            // V této době již obsahuje platnou hodnotu.
            ShowItemCaptionsInQAT = true;
            ShowQatLocationSelector = true;
            ShowTextInQAT = DefaultShowTextInQAT;
            ShowToolbarCustomizeItem = true;
            Toolbar.ShowCustomizeItem = false;
            ShowItemCaptionsInCaptionBar = true;

            if (forDesktop)
            {   // pro Desktop
                MdiMergeStyle = DefaultMdiMergeStyleDesktop;
                ShowSearchItem = true;
                ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            }
            else
            {   // pro ChildWindow
                MdiMergeStyle = DefaultMdiMergeStyleForms;
                ShowSearchItem = true;
                ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            }

            RemoveItemsShortcuts();
        }
        /// <summary>
        /// Projde všechny Itemy v tomto Ribbonu, a pokud mají klávesovou zkratku, pak jí odebere (deaktivuje).<br/>
        /// Účelem je uvolnit veškeré klávesové zkratky (tedy implicitní DevExpress) pro použití z aplikace (tedy Nephrite).<br/>
        /// Ukázkou je Ctrl+F, která je defaultně přiřazena pro RibbonSearch, ale obecně ji chceme použít pro Fulltext.<br/>
        /// Volá se při inicializaci Ribbonu.
        /// </summary>
        protected void RemoveItemsShortcuts()
        {   // DAJ 0076218 9.7.2024
            foreach (BarItem item in this.Items)
            {
                if (item.ItemShortcut != null && item.ItemShortcut.IsExist)
                    item.ItemShortcut = null;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud mergování ribbonu v MDI oknech je nastaveno Always = automaticky jej řeší MDI DocumentManager;
        /// false když má být řešeno explicitně v rámci aktivace / deaktivace oken
        /// </summary>
        public static bool IsAutomaticMdiRibbonMerge { get { return false; } }
        /// <summary>
        /// Režim mergování MDI Ribbonu pro Desktop
        /// </summary>
        protected static RibbonMdiMergeStyle DefaultMdiMergeStyleDesktop { get { return (IsAutomaticMdiRibbonMerge ? RibbonMdiMergeStyle.Always : RibbonMdiMergeStyle.Never); } }
        /// <summary>
        /// Režim mergování MDI Ribbonu pro Forms
        /// </summary>
        protected static RibbonMdiMergeStyle DefaultMdiMergeStyleForms { get { return (IsAutomaticMdiRibbonMerge ? RibbonMdiMergeStyle.Always : RibbonMdiMergeStyle.Never); } }
        /// <summary>
        /// Inicializuje interní data
        /// </summary>
        private void InitData()
        {
            _Groups = new List<DxRibbonGroup>();
            _RibbonId = ++TotalRibbonId;
        }
        /// <summary>
        /// Vykreslení controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!this.IsRibbonAlreadyPainted) { BeforeFirstPaint(); }
            base.OnPaint(e);
            this.PaintAfter(e);
            this.ContentPaintedAfter();
            if (!this.IsRibbonAlreadyPainted) { this.IsRibbonAlreadyPainted = true; AfterFirstPaint(); }
        }
        /// <summary>
        /// Volá se před zahájením prvního vykreslení Ribbonu.
        /// V tuto dobu je <see cref="IsRibbonAlreadyPainted"/> = false.
        /// </summary>
        protected virtual void BeforeFirstPaint() { }
        /// <summary>
        /// Vykreslí ikonu vpravo.
        /// V tuto dobu je <see cref="IsRibbonAlreadyPainted"/> = podle stavu vykreslení: při prvním kreslení je false, při dalším je true.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void PaintAfter(PaintEventArgs e)
        {
            // Bez tohoto řádku je obtížnější změnit stránku ribbonu "Domů" na jinou stránku po prvním otevření okna (musí se kliknout dvakrát):
            this.SelectedPageFixed = null;
        }
        /// <summary>
        /// Zajistí nastavení <see cref="IsContentAlreadyPainted"/> pro všechny aktuálně mergované Ribbony.
        /// </summary>
        private void ContentPaintedAfter()
        {
            var ribbons = this.MergedRibbonsDown;
            foreach (var ribbon in ribbons)
            {
                if (!ribbon.Item1.IsContentAlreadyPainted)
                    ribbon.Item1.IsContentAlreadyPainted = true;
            }
        }
        /// <summary>
        /// Volá se po dokončení zahájením prvního vykreslení Ribbonu.
        /// V tuto dobu je <see cref="IsRibbonAlreadyPainted"/> = true.
        /// </summary>
        protected virtual void AfterFirstPaint()
        { }
        /// <summary>
        /// Obsahuje true po skončení prvního vykreslení tohoto Ribbonu = tedy po zobrazení Ribbonu uživateli.
        /// POZOR: u Ribbonu, který je Mergován do Parent Ribbonu, může tato hodnota být stále false, protože Ribbon jako prvek vykreslen nebyl.
        /// Pokud nás zajímá, zda náš obsah (tj. naše vlastní stránky a jejich prvky) byly někdy vykresleny, testujeme property <see cref="IsContentAlreadyPainted"/>.
        /// </summary>
        public bool IsRibbonAlreadyPainted { get; private set; }
        /// <summary>
        /// Obsahuje true po skončení prvního vykreslení OBSAHU tohoto Ribbonu = tedy po zobrazení zdejších dat v některém Ribbonu uživateli.
        /// Zde je tedy true i tehdy, když this Ribbon fyzicky vykreslen není (protože je Mergován do některého parenta).
        /// </summary>
        public bool IsContentAlreadyPainted { get; private set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = $"Ribbon: {_RibbonId}; {DebugName ?? this.GetType().Name}";
            var selectedPage = this.SelectedPage;
            var lastActiveOwnPage = this.LastActiveOwnPage;
            text += $"; SelectedPage: {selectedPage?.Text ?? "NULL"}";
            text += $"; LastActiveOwnPage: {lastActiveOwnPage?.Text ?? "NULL"}";
            text += $"; OwnPages: {this.GetPages(PagePosition.AllOwn).Count}";
            text += $"; MergedPages: {this.GetPages(PagePosition.AllMerged).Count}";
            return text;
        }
        /// <summary>
        /// Nastaví se na true v okamžiku Dispose
        /// </summary>
        public bool DxDisposed { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud this Ribbon má svůj <see cref="OwnerControl"/>, který je Disposing nebo IsDisposed.
        /// </summary>
        public bool DxOwnerDisposed
        {
            get
            {
                var ownerControl = this.OwnerControl;
                if (ownerControl is null) return false;
                return ownerControl.Disposing || ownerControl.IsDisposed;
            }
        }
        /// <summary>
        /// Je tento Ribbon aktivní?
        /// Výchozí hodnota (nastavená na konci konstruktoru) je true.
        /// Pokud je false, pak se neprovádí eventy Ribbonu (ItemClick, GroupClick).
        /// Setování hodnoty do <see cref="IsActive"/> ji setuje pouze do this instance, nikdy ne do Child mergovaných Ribbonů.
        /// Čtení hodnoty ji vyhodnocuje pouze z this instance.
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public virtual bool LogActive { get; set; }
        /// <summary>
        /// Jméno Ribbonu pro debugování
        /// </summary>
        public string DebugName { get; set; }
        /// <summary>
        /// Unikátní ID tohoto Ribbonu
        /// </summary>
        private int _RibbonId;
        /// <summary>
        /// Celkem vytvořeno Ribbonů
        /// </summary>
        private static int TotalRibbonId;
        /// <summary>
        /// Zobrazovat v Toolbaru u tlačítek i text?
        /// Hodnota true = všechny prvky v QAT budou s textem (pokud je k dispozici);
        /// false = všechny budou bez textu (jen ikony);
        /// null = viditelnost textu je daná stylem prvku dle <see cref="IRibbonItem.RibbonStyle"/>.
        /// <para/>
        /// Hodnotu lze změnit i za běhu.
        /// Výchozí hodnota je převzata ze static property <see cref="DefaultShowTextInQAT"/>.
        /// </summary>
        public bool? ShowTextInQAT { get { return _ShowTextInQAT; } set { _ShowTextInQAT = value; ModifyLinksForToolbar(); } }
        /// <summary>Zobrazovat v Toolbaru u tlačítek i text?</summary>
        private bool? _ShowTextInQAT;
        /// <summary>
        /// Výchozí hodnota pro 'Zobrazovat v Toolbaru u tlačítek i text'.
        /// Hodnota true = všechny prvky v QAT budou s textem (pokud je k dispozici);
        /// false = všechny budou bez textu (jen ikony);
        /// null = viditelnost textu je daná stylem prvku dle <see cref="IRibbonItem.RibbonStyle"/>.
        /// <para/>
        /// Setování je možné.
        /// Nově setovaná hodnota se ale použije pouze pro nově vytvářené Ribbony.
        /// Změna v této property se nepromítá do již existujících Ribbonů.
        /// <para/>
        /// Kterak změnit hodnotu <see cref="ShowTextInQAT"/> v existujících Ribbonech? Díky tomu, že <see cref="DxRibbonControl"/> je <see cref="IListener"/>,
        /// tak je možno použít metodu <see cref="DxComponent.GetListeners{T}()"/>, ze které budou vráceny živé instance Ribbonů, 
        /// a v nich je možno nastavit jejich instanční property <see cref="ShowTextInQAT"/>.
        /// </summary>
        public static bool? DefaultShowTextInQAT { get; set; } = false;
        /// <summary>
        /// Velikost malých ikon v Ribbonu
        /// </summary>
        internal static ResourceImageSizeType RibbonImageSize { get { return ResourceImageSizeType.Small; } }
        /// <summary>
        /// Velikost velkých ikon v Ribbonu
        /// </summary>
        internal static ResourceImageSizeType RibbonLargeImageSize { get { return ResourceImageSizeType.Large; } }
        /// <summary>
        /// Velikost ikon v galerii
        /// </summary>
        internal static ResourceImageSizeType RibbonGalleryImageSize { get { return ResourceImageSizeType.Medium; } }
        /// <summary>
        /// Vlastník = okno nebo panel. 
        /// Slouží k invokaci GUI threadu.
        /// </summary>
        public Control OwnerControl
        {
            get { return _OwnerControl; }
            set { _OwnerControl = value; }
        }
        /// <summary>Vlastník</summary>
        private Control _OwnerControl;
        /// <summary>
        /// Parent Control / Vlastník, slouží jako Control k invokaci GUI
        /// </summary>
        protected Control ParentOwner { get { return (this.Parent ?? this.OwnerControl ?? this); } }
        /// <summary>TimeStamp pro aktivní stránky</summary>
        private int LastTimeStamp;
        #endregion
        #region Obrázek vpravo. Od verze DevExpress 21.1 bude možno použít property RibbonControl.EmptyAreaImageOptions, aktuálně jsme na 20.2
        private void ImageRightInit()
        {
            // _ImageHideOnMouse = true;       // Logo nekreslit, když v tom místě je myš
        }
        private void ImageRightDestroy()
        {
            // _ImageRightFull = null;
            // _ImageRightMini = null;
        }
        /*
        /// <summary>
        /// Ikona vpravo pro velký Ribbon
        /// </summary>
        public Image ImageRightFull { get { return _ImageRightFull; } set { _ImageRightFull = value; this.Refresh(); } }
        private Image _ImageRightFull;
        /// <summary>
        /// Ikona vpravo pro malý Ribbon
        /// </summary>
        public Image ImageRightMini { get { return _ImageRightMini; } set { _ImageRightMini = value; this.Refresh(); } }
        private Image _ImageRightMini;
        /// <summary>
        /// Skrýt obrázek, když do daného prostoru najede myš?
        /// </summary>
        public bool ImageHideOnMouse { get { return _ImageHideOnMouse; } set { _ImageHideOnMouse = value; this.Refresh(); } }
        private bool _ImageHideOnMouse;
        /// <summary>
        /// Vykreslí ikonu vpravo.
        /// V tuto dobu je <see cref="IsRibbonAlreadyPainted"/> = podle stavu vykreslení: při prvním kreslení je false, při dalším je true.
        /// </summary>
        /// <param name="e"></param>
        private void PaintImageRight(PaintEventArgs e)
        {
            OnPaintImageRightBefore(e);

            bool isSmallRibbon = (this.CommandLayout == DevExpress.XtraBars.Ribbon.CommandLayout.Simplified);
            Image image = (isSmallRibbon ? (_ImageRightMini ?? _ImageRightFull) : (_ImageRightFull ?? _ImageRightMini));
            if (image != null && image.Width > 0 && image.Height > 0)
                PaintLogoImage(e, image, isSmallRibbon);
            else
                _ImageBounds = Rectangle.Empty;

            // Bez tohoto řádku je obtížnější změnit stránku ribbonu "Domů" na jinou stránku po prvním otevření okna (musí se kliknout dvakrát):
            this.SelectedPageFixed = null;

            OnPaintImageRightAfter(e);
        }
        /// <summary>
        /// Vykreslí zadané Logo
        /// </summary>
        /// <param name="e"></param>
        /// <param name="image"></param>
        /// <param name="isSmallRibbon"></param>
        private void PaintLogoImage(PaintEventArgs e, Image image, bool isSmallRibbon)
        {
            Point mousePoint = this.PointToClient(Control.MousePosition);

            Size imageNativeSize = image.Size;
            if (imageNativeSize.Width <= 0 || imageNativeSize.Height <= 0) return;

            // Rectangle buttonsBounds = (isSmallRibbon ? ClientRectangle.Enlarge(0, -4, -28, -4) : ButtonsBounds.Enlarge(-4));
            Rectangle contentBounds = ContentBounds;
            // contentBounds = (isSmallRibbon ? contentBounds.Enlarge(0, -4, -28, +16) : contentBounds.Enlarge(-4));
            contentBounds = contentBounds.Enlarge(-2);
            ContentAlignment alignment = (isSmallRibbon ? ContentAlignment.TopRight : ContentAlignment.BottomRight);
            _ImageBounds = imageNativeSize.AlignTo(contentBounds, alignment, true, true);

            bool hideImage = (_ImageHideOnMouse && _ImageBounds.Contains(mousePoint));
            bool paintImage = !hideImage;
            if (paintImage)
                e.Graphics.DrawImage(image, _ImageBounds);
            _ImagePainted = paintImage;
        }
        /// <summary>
        /// Při pohybu myši
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_ImageBounds.HasPixels())
            {
                bool hideImage = (_ImageHideOnMouse && _ImageBounds.Contains(e.Location));
                bool paintImage = !hideImage;
                if (paintImage != _ImagePainted)
                    this.Refresh();
            }
        }
        /// <summary>
        /// Souřadnice oblasti Ribbonu, kde jsou aktuálně buttony
        /// </summary>
        public Rectangle ContentBounds
        {
            get
            {
                var contentBounds = this.ClientRectangle;
                var page = this.SelectedPage;
                if (page != null)
                {
                    var panelInfo = page.PageInfo?.ViewInfo?.Panel;
                    if (panelInfo != null)
                        contentBounds = panelInfo.ContentBounds;
                }
                return contentBounds;
            }
        }
        /// <summary>
        /// Souřadnice obrázku
        /// </summary>
        private Rectangle _ImageBounds;
        /// <summary>
        /// Obrázek je aktuálně vykreslen?
        /// </summary>
        private bool _ImagePainted;
        /// <summary>
        /// Provede se před vykreslením obrázku vpravo v ribbonu
        /// </summary>
        protected virtual void OnPaintImageRightBefore(PaintEventArgs e)
        {
            PaintImageRightBefore?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se před vykreslením obrázku vpravo v ribbonu
        /// </summary>
        public event EventHandler<PaintEventArgs> PaintImageRightBefore;
        /// <summary>
        /// Provede se po vykreslení obrázku vpravo v ribbonu
        /// </summary>
        protected virtual void OnPaintImageRightAfter(PaintEventArgs e)
        {
            PaintImageRightAfter?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se po vykreslení obrázku vpravo v ribbonu
        /// </summary>
        public event EventHandler<PaintEventArgs> PaintImageRightAfter;
        */
        #endregion
        #region Aktivní stránka Ribbonu, její aktivace, změna
        /// <summary>
        /// ID aktuálně vybrané stránky = <see cref="DevExpress.XtraBars.Ribbon.RibbonPage.Name"/> = <see cref="IRibbonPage.PageId"/>.
        /// <para/>
        /// Lze setovat, dojde k aktivaci dané stránky (pokud je nalezena).
        /// Funguje správně i pro stránky kategorií i pro mergované stránky, pokud jejich holé jméno stránky je jednoznačné.
        /// <para/>
        /// Pozor, pokud v definici stránek je použito shodné jméno pro stránku bez kategorie a pro jinou stránku s kategorií, pak <see cref="SelectedPageId"/> není jednoznačené. Pak je vhodnější používat <see cref="SelectedPageFullId"/>
        /// </summary>
        public string SelectedPageId
        {
            get { return this.SelectedPage?.Name; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    var page = AllPages.FirstOrDefault(p => p.Name == value);
                    if (page != null)
                        this.SelectedPage = page;
                }
            }
        }
        /// <summary>
        /// Obsahuje FullID stránky. 
        /// FullID v sobě obsahuje i ID kategorie, pokud na stránce je zadaná. Oddělovač je zpětné lomítko \. Pokud kategorie není uvedena, pak <see cref="SelectedPageFullId"/> neobsahuje zpětné lomítko \ .
        /// <para/>
        /// Lze setovat, dojde k aktivaci dané stránky (pokud je nalezena).
        /// Funguje správně i pro stránky kategorií i pro mergované stránky.
        /// </summary>
        public string SelectedPageFullId
        {
            get { return GetPageFullId(this.SelectedPage); }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    var page = AllPages.FirstOrDefault(p => GetPageFullId(p) == value);
                    if (page != null)
                    {   // Dvakrát je lepší:
                        this.SelectPage(page);
                        this.SelectedPage = page;
                    }
                }
            }
        }
        /// <summary>
        /// Aktuálně vybraná stránka v this Ribonu.
        /// Upozornění: jde o stránku v this Ribbonu deklarovanou (proto "OwnDxPage"), takže pokud this Ribbon nedeklaruje žádnou stránku, a zobrazuje tedy jen mergované stránky, je zde null.
        /// <para/>
        /// Tuto hodnotu nelze setovat, k setování lze využít property <see cref="SelectedPageId"/>. Ta zahrnuje i stránky z Child Ribbonů.
        /// </summary>
        public IRibbonPage SelectedOwnDxPage { get { return this.LastActiveOwnPage?.PageData; } }
        /// <summary>
        /// Vrátí FullId stránky. Obsahuje : [ID kategorie\]ID stránky
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        protected static string GetPageFullId(DevExpress.XtraBars.Ribbon.RibbonPage page)
        {
            if (page == null) return null;
            if (page.Category == null) return page.Name;
            return $"{page.Category.Name}\\{page.Name}";
        }
        /// <summary>
        /// FullID posledně reálné vybrané stránky (viz <see cref="SelectedPageFullId"/>). V procesu změny aktivní stránky obsahuje předchozí.
        /// Pokud se provádí Clear, tato hodnota se neaktualizuje = obsahuje předchozí aktivní stránku.
        /// Pokud po Clear nezbude žádná stránka, pak zde bude zachycena poslední platná.
        /// Pokud po Clear bude aktivována jiná existující stránka, pak zde bude tato nová.
        /// Před aktivací první stránky je zde null, ale poté už nikdy ne.
        /// </summary>
        public string LastSelectedPageFullId { get; private set; }
        /// <summary>
        /// FullID posledně vybrané stránky (viz <see cref="SelectedPageFullId"/>), která je naše nativní (bude k dispozici i po provedení Unmerge Child ribbonu).
        /// Na tuto stránku seRibbon vrátí po Unmerge.
        /// </summary>
        public string LastSelectedOwnPageFullId { get; private set; }
        /// <summary>
        /// Při aktivaci stránky (těsně před tím) zajistí vygenerování prvků LazyLoad
        /// </summary>
        /// <param name="prev"></param>
        protected override void OnSelectedPageChanged(DevExpress.XtraBars.Ribbon.RibbonPage prev)
        {
            if (this.ModifySelectedPage()) return;

            this.StoreLastSelectedPage();
            base.OnSelectedPageChanged(prev);
            if (!this.CurrentModifiedState) this.CheckLazyContent(this.SelectedPage, false);
        }
        /// <summary>
        /// Pokud je přednastavena fixní stránka <see cref="SelectedPageFixed"/>, pak zajistí její aktivaci.
        /// </summary>
        /// <returns></returns>
        private bool ModifySelectedPage()
        {
            var selectedPageFixed = this.SelectedPageFixed;
            if (selectedPageFixed is null) return false;   // Nemáme fixní cílovou stránku = necháme přepnout na požadovanou...

            this.SelectedPageFixed = null;
            this.SelectedPage = selectedPageFixed;         // Tady se vyvolá OnSelectedPageChanged() pro nově zadanou stránku, vleze to do této metody, ale v druhím řádku vypadneme protože (SelectedPageFixed == null)!
            return true;
        }
        /// <summary>
        /// Povinná cílová stránka Ribbonu.
        /// <para/>
        /// DAJ 0071844 31.8.2022:<br/>
        /// Řeší to komplikaci, kdy máme otevřené okno "A" a na něm aktivní stránku "X" (např. Přehled, nikoli Domů), 
        /// a nově otevíráme okno B, kde chceme aktivovat jako výchozí stránku = "Domů".<br/>
        /// Nicméně v procesu Mergování ribbonu pocházejícího z okna "B" do Desktopového Ribbonu tento (desktopový = vizuální) Ribbon má tendenci i pro nové okno "B" ponechat aktivní stránku Ribbonu stejnou, 
        /// jaká byla aktivní dosud z okna "A" = stránka "Přehled", a tak při aktivaci okna "B" před jeho zobrazením ten RibbonControl nasetuje do SelectedPage tu stránku "Přehled" = probíhá metoda OnSelectedPageChanged(),
        /// kde 'prev' = "Domů" a SelectedPage = "Přehled".<br/>
        /// Nedá se tomu zabránit jinak, než v dané metodě OnSelectedPageChanged() násilně změnit SelectedPage = SelectedPageFixed.<br/>
        /// Nastavení hodnoty do SelectedPageFixed se děje při Mergování ribbonu, když do Parent Ribbonu aktivuji vhodnou SelectedPage.<br/>
        /// Hodnota SelectedPageFixed se použije jen jedenkrát, ihned se nuluje - viz <see cref="ModifySelectedPage()"/>.<br/>
        /// Vykreslení Ribbonu (OnPaint) hodnotu SelectedPageFixed rovněž nuluje, aby bylo možno uživatelsky změnit aktivní stránku 
        /// (bez tohoto nulování je po otevření okna "B" zafixována stránka "Domů" a na jinou stránku se musí kliknout dvakrát, protože první kliknutí jenom vynuluje fixovanou stránku).<br/>
        /// </summary>
        private RibbonPage SelectedPageFixed { get; set; }
        /// <summary>
        /// Označí si nativní stránky Ribonu jako "aktuálně selectované".
        /// Na základě tohoto označení následně lze určit pro každý (i mergovaný) Ribbon, která jeho stránka byla naposledy aktivní, 
        /// a v procesu UnMerge může Ribbon tuto stránku aktivovat = metoda <see cref="ActivateLastActivePage()"/>.
        /// <para/>
        /// Jedna stránka Ribbonu, nyní aktivovaná (<see cref="RibbonControl.SelectedPage"/>) v sobě může obsahovat data (grupy a prvky) z více pod sebou mergovaných Ribbonů.
        /// Tato metoda najde tyto zdrojové Ribbony, najde jejich stránky a označí je jako "právě aktivní".
        /// <para/>
        /// Současně s tím do těchto (nativních) Ribbonů pošle event o aktivaci stránky 
        /// </summary>
        protected void StoreLastSelectedPage(bool force = false)
        {
            if (this.SelectedPage == null) return;

            if (force || (!_ClearingNow && _CurrentMergeState == MergeState.None && !this.CurrentModifiedState))
            {   // Pokud akce je povinná (force) anebo pokud je přípustná za aktuálního stavu:
                // Označím si stránky (nativní = těch může být více = z více mergovaných ribbonů), které jsou nyní aktivní:
                StoreLastSelectedPage(this.SelectedPage, false);
            }
        }
        /// <summary>
        /// Metoda najde první svoji stránku a nastaví ji jako aktivní - a to ještě před zobrazením Ribbonu uživateli.
        /// </summary>
        protected void StoreFirstPageAsLastActivePage()
        {
            var pages = this.GetPages(PagePosition.AllOwn);
            if (pages.Count > 0)
                StoreLastSelectedPage(pages[0], true);
        }
        /// <summary>
        /// Pro danou stránku <paramref name="selectedPage"/> najde její nativní stránky (tj. všechny, které jsou do dané stránky aktuálně mergované),
        /// a označí si tyto nativní stránky jako 'Aktivované' = právě nyní aktivní.
        /// Volá se po uživatelské aktivaci stránky, a po prvotním naplnění Ribbonu daty (tam se aktivuje první z naplněných stránek).
        /// </summary>
        /// <param name="selectedPage">Stránka která má být aktivní</param>
        /// <param name="force">Požadavek na provedení aktivace stránky i pro takový Ribbon, který ještě nebyl zobrazen uživateli (používá se při inicializaci pro aktivaci první stránky)</param>
        protected void StoreLastSelectedPage(RibbonPage selectedPage, bool force = false)
        {
            DxRibbonPage[] nativePages = _GetNativePages(selectedPage);
            nativePages.ForEachExec(p => p.OnActivate(force));
        }
        /// <summary>
        /// Metoda v this Ribbonu aktivuje (selectuje) tu stránku, která byla naposledy aktivní = včetně stránek Mergovaných (pokud jsou).
        /// </summary>
        protected void ActivateLastActivePage()
        {
            var lastActivePage = this.LastActiveAllPage;
            if (lastActivePage != null)
                this.SelectPage(lastActivePage);
        }
        /// <summary>
        /// Metoda najde a vrátí ty nativní stránky, jejichž obsah je mergován do dodané stránky.
        /// Nativní stránka = ta, která byla fyzicky vytvořena a naplněna v určitém Ribbonu.
        /// Následně byla Mergována (i spolu s dalšími) do určité výsledné stránky nějakého vyššího Ribbonu.
        /// Tato výsledná mergovaná stránka je vstupem této metody, metoda najde výchozí podklady a tyto stránky vrátí.
        /// Pokud je na vstupu null, je null i na výstupu. 
        /// Pokud je na vstupu zcela prázdná stránka, která je typu <see cref="DxRibbonPage"/>, bude vrácena ona.
        /// Pokud na vstupu je prázdná stránka jiného typu, je vráceno prázdné pole.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        private static DxRibbonPage[] _GetNativePages(DevExpress.XtraBars.Ribbon.RibbonPage page)
        {
            if (page == null) return null;

            List<DxRibbonPage> pages = new List<DxRibbonPage>();

            // Ze skupin + mergovaných skupin získám soupis prvků:
            var items = new List<BarItem>();
            items.AddRange(page.Groups.SelectMany(g => g.ItemLinks).Select(l => l.Item));
            items.AddRange(page.MergedGroups.SelectMany(g => g.ItemLinks).Select(l => l.Item));

            // Získám distinct seznam DxPages:
            foreach (var item in items)
            {
                if (item.Tag is BarItemTagInfo itemInfo)
                {   // V běžných BarItemech je Tag typu BarItemTagInfo:
                    var nativePage = itemInfo.DxPage;
                    if (nativePage != null && !pages.Any(p => Object.ReferenceEquals(p, nativePage)))
                        pages.Add(nativePage);
                }
                else if (item.Tag is DxRibbonPage dxRibbonPage)
                {   // Pokud stránka je definovaná jako LazyLoad, pak má speciální grupu obsahující jeden BarItem, v jehož tagu je jeho nativní stránka DxRibbonPage:
                    if (dxRibbonPage != null && !pages.Any(p => Object.ReferenceEquals(p, dxRibbonPage)))
                        pages.Add(dxRibbonPage);
                }
            }

            return pages.ToArray();
        }
        /// <summary>
        /// Obsahuje posledně aktivní stránku z this Ribbonu (nepočítaje v to stránky mergovaných Ribbonů).
        /// Posledně aktivní stránka je dána jejím TimeStampem <see cref="DxRibbonPage.ActivateTimeStamp"/>.
        /// </summary>
        protected DxRibbonPage LastActiveOwnPage { get { return this.AllOwnDxPages.TopMost(p => p.ActivateTimeStamp); } }
        /// <summary>
        /// Obsahuje posledně aktivní stránku pro tento Ribbon, přednost mají stránky z Mergovaných Ribbonů (rekurzivně).
        /// Posledně aktivní stránka je dána jejím TimeStampem <see cref="DxRibbonPage.ActivateTimeStamp"/>.
        /// </summary>
        protected DxRibbonPage LastActiveAllPage { get { return this.MergedChildDxRibbon?.LastActiveAllPage ?? this.LastActiveOwnPage; } }
        /// <summary>
        /// Uživatel aktivoval danou stránku.
        /// Volá se na tom Ribbonu, který danou stránku deklaroval. Mergovaný TopRibbon tedy zajistí, že najde všechny mergované Child Ribbony a pošle se událost do všech.
        /// </summary>
        /// <param name="dxRibbonPage"></param>
        void IDxRibbonInternal.OnActivatePage(DxRibbonPage dxRibbonPage) 
        {
            TEventArgs<IRibbonPage> args = new TEventArgs<IRibbonPage>(dxRibbonPage.PageData);
            this.OnSelectedDxPageChanged(args);
            this.SelectedDxPageChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Provede se po aktivaci stránky, která náleží do this Ribbonu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSelectedDxPageChanged(TEventArgs<IRibbonPage> args) { }
        /// <summary>
        /// Vyvolá se po aktivaci stránky, která náleží do this Ribbonu.
        /// Pokud má stránka deklarován LazyLoad, pak po této události bude následovat událost pro načtení obsahu stránky <see cref="PageOnDemandLoad"/>.
        /// Není tedy nutno ve zdejší události <see cref="SelectedDxPageChanged"/> reagovat na LazyLoad, stačí to vyřešit v handleru <see cref="PageOnDemandLoad"/>.
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonPage>> SelectedDxPageChanged;
        #endregion
        #region Setování obsahu Ribbonu pomocí kompletní sady dat do RibbonContent
        /// <summary>
        /// Obsah Ribbonu = stránky, a další přímo vložené prvky.
        /// <para/>
        /// Upozornění:<br/>
        /// Pokud budou prvky vkládány jednotlivě (do property <see cref="QATDirectItems"/> a <see cref="TitleBarItems"/>, a metodou <see cref="AddPages(IEnumerable{IRibbonPage}, bool)"/>),
        /// pak v této property <see cref="RibbonContent"/> bude null (nebo jiná posledně vložená hodnota) - neprovádíme mergování jednotlivých hodnot do <see cref="RibbonContent"/>.<br/>
        /// Stejně tak různé Refreshe stránek, grup a prvků a submenu se do této property nepromítají.<br/>
        /// Property <see cref="RibbonContent"/> slouží primárně k nasetování výchozího stavu. 
        /// Skoro se tedy jedná o ideálního kandidáta na property, která by neměla mít get accessor - ale to působí tak divně, jako by to byla černá díra :-)
        /// <para/>
        /// Účel této property je jednoduchý: aplikační kód si může do instance třídy <see cref="DataRibbonContent"/> průběžně připravit obsah Ribbonu, 
        /// a ten se pak jedním příkazem nasetuje do fyzického Ribbonu.
        /// </summary>
        public IRibbonContent RibbonContent
        {
            get { return __RibbonContent; }
            set { _SetRibbonContent(value); }
        }
        /// <summary>
        /// Vloží dodaná data do this Ribbonu. Stávající data zruší.
        /// </summary>
        /// <param name="ribbonContent"></param>
        private void _SetRibbonContent(IRibbonContent ribbonContent)
        {
            this.ApplicationButtonText = ribbonContent?.ApplicationButtonText;
            this.TitleBarItems = ribbonContent?.TitleBarItems;
            this.QATDirectItems = ribbonContent?.QATDirectItems;
            this.AddPages(ribbonContent?.Pages, true);
            this.StatusBarItems = ribbonContent?.StatusBarItems;
            __RibbonContent = ribbonContent;
        }
        private IRibbonContent __RibbonContent;
        #endregion
        #region Quick Search menu
        /// <summary>
        /// Inicializace eventů v prvku SearchEditItemLink?.Edit
        /// </summary>
        protected void InitSearchItem()
        {
            var editor = this.SearchEditItemLink?.Edit;
            if (editor != null)
            {
                editor.Enter += SearchEdit_Enter;
                editor.Leave += SearchEdit_Leave;
            }
            this.CustomizeSearchMenu += _CustomizeSearchMenu;
            _CurrentIsSearchEditActive = false;
            _SearchEditItems = null;
            _SearchEditItemsLoading = false;
        }
        /// <summary>
        /// Uživatel vstoupil v this Ribbonu do políčka Search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchEdit_Enter(object sender, EventArgs e)
        {
            RefreshSearchEditItems();
            _AddedSearchMenuItems = new List<BarItemLink>();
            _CurrentIsSearchEditActive = true;
        }
        /// <summary>
        /// Uživatel opustil v this Ribbonu políčko Search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchEdit_Leave(object sender, EventArgs e)
        {
            DisposeLastSearchMenuItems();
            _CurrentIsSearchEditActive = false;
            _AddedSearchMenuItems = null;
        }
        /// <summary>
        /// Umožní upravit položky menu Search v this Ribbonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CustomizeSearchMenu(object sender, RibbonSearchMenuEventArgs e)
        {
            ModifySearchEditItems(e);
        }
        /// <summary>
        /// this Ribbon má aktivní políčko SearchEdit = uživatel provádí hledání prvků Ribbonu.
        /// Pokud this Ribbon je mergován do <see cref="MergedIntoParentDxRibbon"/>, pak this Ribbon fyzicky nemá políčko Search, a uživatel hledá prostřednictvím Top parent Ribbonu.
        /// Pak je zde false, ale v TopParent Ribbonu je true. Viz property <see cref="IsSearchEditActive"/>.
        /// </summary>
        private bool _CurrentIsSearchEditActive;
        /// <summary>
        /// Obsahuje true, pokud this Ribbon (nebo některý náš Parent) má nyní aktivní SearchEditor.
        /// V tomto stavu se NESMÍ vyhodnocovat OnDemandLoad, protože to vede k refreshím a k reaktivaci Popup, což je divočina.
        /// </summary>
        protected bool IsSearchEditActive
        { 
            get 
            {
                if (_CurrentIsSearchEditActive) return true;
                if (this.MergedIntoParentDxRibbon != null) return this.MergedIntoParentDxRibbon.IsSearchEditActive;
                return false;
            }
        }
        /// <summary>
        /// Metoda zajistí kompletně nové vytvoření všech položek do QuickSearch Menu.
        /// Metoda je volána po každém vepsání / editaci textu v políčku pro rychlé hledání funkcí v Ribbonu.
        /// <para/>
        /// Ribbon předpřipraví menu (v e.Menu), ale toto menu má z hlediska Nephrite několik vad na kráse:
        /// a) neobsahuje všechny prvky, protože Ribbon má některé prvky dotahované OnDemand (otevírací menu na tlačítku "Funkce: Tři tečky"), a ty nemusí být v Ribbonu reálně vloženy = Ribbon je nenajde;
        /// b) Nephrite vyžaduje, aby hlavičky skupin v menu obsahovaly "Název stránky: Název grupy", ale Ribbon dává jen "Název grupy";
        /// c) Předešlý bod chování komponenty DevExpress vede k tomu, že: když máme stejné tlačítko ve stejně pojmenované grupě na dvou různých stránkách (Domů - Záznam - Otevřít; a Přehled - Záznam - Otevřít), 
        ///   pak DevExpress obě tlačítka vidí ve stejnojmenné skupině "Záznam" a v menu je zařadí do společné skupiny "Záznam", což dost znemožňuje zobrazit každé tlačítko v jiné správně pojmenované skupině
        /// d) Nephrite vyžaduje, aby skupiny byly tříděny podle POŘADÍ stránky a grupy ve stránce, kdežto DevExpress řadí grupy abecedně.
        /// <para/>
        /// Řešení: zahodíme vygenerované DevExpress menu, najdeme si vyhovující položky podle našich pravidel, pojmenujeme jejich skupiny podle naší potřeby, seřadíme grupy podle našich požadavků, a vygenerujeme nové menu z našich prvků.
        /// </summary>
        /// <param name="e"></param>
        private void ModifySearchEditItems(RibbonSearchMenuEventArgs e)
        {
            // Sem vstupuje řízení pokaždé, když uživatel v SearchMenu edituje text (vepíše/smaže znak):
            DisposeLastSearchMenuItems();

            // Pokud uživatel nic nezadal, skončíme:
            if (String.IsNullOrEmpty(e.SearchString) || e.SearchString.Trim().Length < 1)
            {
                e.Menu.HidePopup();
                return;
            }

            // Najdeme případný NotFound prvek:
            DetectNotFoundItem(e);

            // Začneme s kompletním seznamem prvků v Ribbonu, a to včetně Mergovaných ribbonů::
            List<BarItem> items = new List<BarItem>();
            FillItemsForSearch(items, this);
            SearchMenuItems finalItems = new SearchMenuItems(items);
           
            // Získáme seznam nativních prvků (nikoli GroupHeader), které vyhovují zadanému textu:
            AddSearchItemsNative(e, finalItems);

            // Přidáme seznam přidaných prvků (bez GroupHeader), které dosud nejsou vygenerovány do Ribbonu:
            AddSearchItemsAdded(e, finalItems);

            // Zajistíme prvek "Nic nenalezeno":
            AddSearchItemsNotFound(e, finalItems);

            // Modifikujeme vzhled i obsah menu podle počtu výsledků:
            ModifySearchMenuByResults(e, finalItems);

            // Získám setříděné grupy prvků, kde klíčem grupy je text grupy:
            var groups = CreateSearchItemGroups(finalItems);

            // Vložím tyto grupy do menu:
            AddSearchGroupsToMenu(e, finalItems, groups);
        }
        /// <summary>
        /// Pokud dosud nemáme nalezen prvek <see cref="SearchItemNotFoundRibbonItem"/>, pokusí se jej najít nyní v dodaném menu.
        /// </summary>
        /// <param name="e"></param>
        private void DetectNotFoundItem(RibbonSearchMenuEventArgs e)
        {
            if (SearchItemNotFoundRibbonItem is null)
            {   // Jen poprve za život Ribbonu:
                string localizedCaption = DxComponent.Localize(MsgCode.RibbonSearchMenuItemNoMatchesCaption);
                foreach (BarItemLink link in e.Menu.ItemLinks)
                {
                    if (IsSearchItemNotFound(link, localizedCaption, out BarStaticItem notFoundItem))
                    {   // Prvek "Nenalezeny odpovídající položky":
                        notFoundItem.Caption = localizedCaption;               // DAJ 0072010: Do prvku vložím náš lokalizovaný překlad namísto DevExpress lokalizace...
                        SearchItemNotFoundRibbonItem = notFoundItem;
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud dodaný prvek obsahuje význam "Nenalezeny odpovídající položky"
        /// </summary>
        /// <param name="link"></param>
        /// <param name="localizedCaption"></param>
        /// <param name="notFoundItem"></param>
        /// <returns></returns>
        private bool IsSearchItemNotFound(BarItemLink link, string localizedCaption, out BarStaticItem notFoundItem)
        {
            // Prvek musí být BarStaticItem, jiné neberu:
            notFoundItem = null;
            if (!(link.Item is BarStaticItem staticItem)) return false;

            // Měl bych mít od posledního hledání v Tagu uložený kód hlášky:
            bool result = (staticItem.Tag is MsgCode code && code == MsgCode.RibbonSearchMenuItemNoMatchesCaption);

            if (!result)
            {
                // Zkusím najít prvek podle standardně lokalizovaného textu:
                string caption = DevExpress.XtraBars.Localization.BarLocalizer.Active.GetLocalizedString(DevExpress.XtraBars.Localization.BarString.RibbonSearchItemNoMatchesFound);
                result = (staticItem.Caption == caption);

                // Standardní lokalizace:
                if (!result && localizedCaption != null) result = (staticItem.Caption == localizedCaption);

                // Zoufalství v jazyce anglickém:
                if (!result) result = (staticItem.Caption == "No matches found");

                // Šílené zoufalství v jazyce českém poprvé a podruhé:
                if (!result) result = (staticItem.Caption == "Nenalezeny žádné shody");
                if (!result) result = (staticItem.Caption == "Upřesněte hledaný text");

                // Nalezeno? Označkujme:
                if (result)
                    staticItem.Tag = MsgCode.RibbonSearchMenuItemNoMatchesCaption;
            }

            if (result)
                notFoundItem = staticItem;

            return result;
        }
        /// <summary>
        /// Metoda projde seznam linků v menu, a prvky (nikoli <see cref="BarHeaderItem"/>), které jsou Visible, vloží do <paramref name="finalItems"/>.
        /// Současně k nim přidává potřebný titulek grupy.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="finalItems"></param>
        /// <returns></returns>
        private void AddSearchItemsNative(RibbonSearchMenuEventArgs e, SearchMenuItems finalItems)
        {
            foreach (BarItem barItem in finalItems.AllItems)
            {
                if (_TryGetIRibbonItem(barItem, out var iRibbonItem))
                {   // a) prvky vytvořené pro konkrétní IRibbonItem:
                    string itemId = "ID:" + iRibbonItem.ItemId;
                    if (!finalItems.FoundItems.ContainsKey(itemId))
                    {
                        if (CanAddItemToSearchMenu(iRibbonItem, e.SearchString, out string itemCaption))
                        {
                            string groupCaption = GetSearchItemGroupCaption(iRibbonItem, out string groupSortOrder);
                            finalItems.FoundItems.Add(itemId, new SearchMenuItem(itemId, itemCaption, groupCaption, groupSortOrder, iRibbonItem, barItem));
                        }
                    }
                }
                else
                {   // b) prvky vytvořené fixně v WDesktopu:
                    if (CanAddItemToSearchMenu(barItem, e.SearchString, out string itemCaption))
                    {
                        string itemId = "TX:" + finalItems.FoundItems.Count.ToString();
                        string groupCaption = GetSearchItemGroupCaption(barItem, out string groupSortOrder);
                        finalItems.FoundItems.Add(itemId, new SearchMenuItem(itemId, itemCaption, groupCaption, groupSortOrder, null, barItem));
                    }
                }
            }
        }
        /// <summary>
        /// Metoda přidá donačtené prvky nad rámec prvků přítomných v Ribbonu do seznamu v <paramref name="finalItems"/>.
        /// Současně k nim přidává potřebný titulek grupy.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="finalItems"></param>
        private void AddSearchItemsAdded(RibbonSearchMenuEventArgs e, SearchMenuItems finalItems)
        {
            // Provedeme pro všechny mergované instance Ribonů, počínaje this, až do dvanáctého kolena:
            var ribbon = this;
            for (int l = 0; l < 12; l++)
            {
                if (ribbon is null) break;
                var iRibbonItems = ribbon._SearchEditItems;
                if (iRibbonItems != null && iRibbonItems.Length > 0)
                {
                    foreach (var ribbonItem in iRibbonItems)
                    {
                        var iRibbonItem = ribbonItem;
                        if (iRibbonItem != null && !String.IsNullOrEmpty(iRibbonItem.ItemId))
                        {
                            // Máme prvek a máme ID:
                            string itemId = "ID:" + iRibbonItem.ItemId;
                            if (!finalItems.FoundItems.ContainsKey(itemId) && CanAddItemToSearchMenu(iRibbonItem, e.SearchString, out string itemCaption))
                            {   // Prvek má být přidán do menu (ještě tam není, a jeho text vyhovuje zadaném stringu):
                                // zajistím, že v IRibbonItem bude vytvořen fyzický BarItem, protože ten musí vytvořit this Ribbon jako autor, on si pak bude obsluhovat Click event na itemu:
                                ribbon.PrepareSearchEditBarItem(ref iRibbonItem);

                                string groupCaption = ribbon.GetSearchItemGroupCaption(iRibbonItem, out string groupSortOrder);
                                finalItems.FoundItems.Add(itemId, new SearchMenuItem(itemId, itemCaption, groupCaption, groupSortOrder, iRibbonItem, iRibbonItem.RibbonItem));
                            }
                        }
                    }
                }
                // Přejdeme na můj Child Ribbon:
                ribbon = ribbon.MergedChildDxRibbon;
            }
        }
        /// <summary>
        /// Vrátí true pokud prvek s daným textem má být přidán do Search menu
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="searchText"></param>
        /// <param name="itemCaption"></param>
        /// <returns></returns>
        private bool CanAddItemToSearchMenu(IRibbonItem iRibbonItem, string searchText, out string itemCaption)
        {
            itemCaption = null;
            if (iRibbonItem is null || !iRibbonItem.VisibleInSearchMenu) return false;
            if (CanAddTextToSearchMenu(iRibbonItem.Text, searchText)) return true;
            if (CanAddTextToSearchMenu(iRibbonItem.SearchTags, searchText))
            {
                itemCaption = $"{iRibbonItem.Text} ({iRibbonItem.SearchTags})";
                return true;
            }
            return false;
        }
        /// <summary>
        /// Vrátí true pokud prvek s daným textem má být přidán do Search menu
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="searchText"></param>
        /// <param name="itemCaption"></param>
        /// <returns></returns>
        private bool CanAddItemToSearchMenu(BarItem barItem, string searchText, out string itemCaption)
        {
            itemCaption = null;
            if (barItem is null || !barItem.VisibleInSearchMenu) return false;
            if (CanAddTextToSearchMenu(barItem.Caption, searchText)) return true;
            if (CanAddTextToSearchMenu(barItem.SearchTags, searchText))
            {
                itemCaption = $"{barItem.Caption} ({barItem.SearchTags})";
                return true;
            }
            return false;
        }
        /// <summary>
        /// Vrátí true pokud daným text vyhovuje zadání a má být přidán do Search menu
        /// </summary>
        /// <param name="itemText"></param>
        /// <param name="searchText"></param>
        /// <returns></returns>
        private bool CanAddTextToSearchMenu(string itemText, string searchText)
        {
            return (itemText != null && itemText.Length > 0 && itemText.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }
        /// <summary>
        /// V rámci daného <see cref="IRibbonItem"/> prověří existenci reálného BarItem v <see cref="IRibbonItem.RibbonItem"/>, a případně jej vytvoří.
        /// Klíčové je, že BarItem vytváří právě ta instance Ribbonu, kde je prvek deklarován, protože ta instance pak bude obsluhovat jeho Click.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        private void PrepareSearchEditBarItem(ref IRibbonItem iRibbonItem)
        {
            BarItem barItem = iRibbonItem.RibbonItem;
            if (barItem is null)
            {
                int count = 0;
                barItem = GetItem(ref iRibbonItem, null, 0, DxRibbonCreateContentMode.CreateAllSubItems, ref count);
                iRibbonItem.RibbonItem = barItem;
            }
        }
        /// <summary>
        /// Modifikuje menu podle nalezených položek
        /// </summary>
        /// <param name="e"></param>
        /// <param name="finalItems"></param>
        private void ModifySearchMenuByResults(RibbonSearchMenuEventArgs e, SearchMenuItems finalItems)
        {
            e.Menu.MenuDrawMode = MenuDrawMode.SmallImagesText;

            int resultCount = finalItems.FoundItems.Count;
            if (SearchMenuMaxResultCount > 0 && resultCount > SearchMenuMaxResultCount)
            {
                AddSearchItemsTooManyMatches(e, finalItems, resultCount);
            }
            else if (SearchMenuShrinkResultCount > 0 && resultCount > SearchMenuShrinkResultCount)
            {
                setMenuColumns(10);
            }
            else
            {
                setMenuColumns(0);
            }

            void setMenuColumns(int columnCount)
            {
                DefaultBoolean multiColumn = (columnCount <= 0 ? DefaultBoolean.False : DefaultBoolean.True);
                if (e.Menu.MultiColumn != multiColumn) e.Menu.MultiColumn = multiColumn;
                if (e.Menu.OptionsMultiColumn.ColumnCount != columnCount) e.Menu.OptionsMultiColumn.ColumnCount = columnCount;
            }
        }
        /// <summary>
        /// Metoda sestaví a vrátí grupy do menu Search.
        /// Výstupem je List, který obsahuje prvky grupy, kde Key = titulek grupy, a prvky grupy jsou typu <see cref="SearchMenuItem"/>.
        /// </summary>
        /// <param name="finalItems"></param>
        /// <returns></returns>
        private List<IGrouping<string, SearchMenuItem>> CreateSearchItemGroups(SearchMenuItems finalItems)
        {
            var groups = finalItems.FoundItems.Values
                .GroupBy(i => i.GroupCaption)
                .ToList();

            if (SearchMenuGroupSort == SearchMenuGroupSortMode.PageOrderGroupCaption)
                // Třídění podle grupovacího výrazu:
                groups.Sort((a, b) => String.Compare(a.FirstOrDefault().GroupSortOrder, b.FirstOrDefault().GroupSortOrder));
            else
                // Třídění podle textu grupy (Název stránky: Název grupy):
                groups.Sort((a, b) => String.Compare(a.Key, b.Key));

            return groups;
        }
        /// <summary>
        /// Zajistí, že v menu budou obsaženy prvky z dodaných dat groups.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="finalItems"></param>
        /// <param name="groups"></param>
        private void AddSearchGroupsToMenu(RibbonSearchMenuEventArgs e, SearchMenuItems finalItems, List<IGrouping<string, SearchMenuItem>> groups)
        {
            e.Menu.ClearLinks();

            foreach (var group in groups)
            {
                AddSearchGroupToMenuTitle(e, group.Key);
                // Třídění obsahu skupiny bych řešil tady:
                var items = group.ToList();
                items.Sort((a, b) => String.Compare(a.ItemCaption, b.ItemCaption));
                foreach (var item in items)
                    AddSearchGroupToMenuItem(e, item);
            }
        }
        /// <summary>
        /// Do SearchMenu přidá záhlaví skupiny = <see cref="BarHeaderItem"/> pro daný titulek
        /// </summary>
        /// <param name="e"></param>
        /// <param name="title"></param>
        private void AddSearchGroupToMenuTitle(RibbonSearchMenuEventArgs e, string title)
        {
            if (!String.IsNullOrEmpty(title))
                e.Menu.AddItem(new BarHeaderItem() { Caption = title });
        }
        /// <summary>
        /// Do SearchMenu přidá nový prvek (Link) z dodaných dat
        /// </summary>
        /// <param name="e"></param>
        /// <param name="item"></param>
        private void AddSearchGroupToMenuItem(RibbonSearchMenuEventArgs e, SearchMenuItem item)
        {
            if (item is null || item.BarItem is null) return;
            
            var barLink = e.Menu.AddItem(item.BarItem);
            ModifySearchMenuItemImage(barLink, item);
            item.BarLink = barLink;
        }
        /// <summary>
        /// Metoda upraví velikost Image pro SearchMenu v případě, kdy je to nezbytné.
        /// </summary>
        /// <param name="barLink"></param>
        /// <param name="item"></param>
        private void ModifySearchMenuItemImage(BarItemLink barLink, SearchMenuItem item)
        {
            // Fixní styl:
            barLink.UserPaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            barLink.UserRibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText;

            // Kdy je nutné upravit Image? Když máme definovaný Image typu Bitmapa natvrdo = nikoli přes ImageList, a je veliký:
            if (barLink.ImageOptions.ImageIndex < 0 && barLink.ImageOptions.Image != null)
            {   // Řeším jen Image, nikoli LargeImage... Tohle menu používá malé obrázky.
                if (barLink.ImageOptions.Image.Size.Height > 20)
                {
                    item.BarLinkThumbImage = CreateSmallImage(barLink.ImageOptions.Image, 16);
                    barLink.ImageOptions.Image = item.BarLinkThumbImage;
                }
            }
        }
        /// <summary>
        /// Vytvoří thumbnail obrázek v dané velikosti
        /// </summary>
        /// <param name="source"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private Image CreateSmallImage(Image source, int height)
        {
            if (source is null) return null;
            try
            {
                var size = source.Size;
                int width = size.Width * height / size.Height;
                return source.GetThumbnailImage(width, height, null, IntPtr.Zero);
            }
            catch { return null; }
        }
        /// <summary>
        /// Vrací standardní titulek grupy pro daný prvek v menu QuickSearch
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="groupSortOrder"></param>
        /// <returns></returns>
        private string GetSearchItemGroupCaption(IRibbonItem iRibbonItem, out string groupSortOrder)
        {
            groupSortOrder = SearchMenuGroupOtherSortOrder;
            if (iRibbonItem is null) return SearchMenuGroupOtherCaption;
            var parentGroup = SearchForParentGroupForItem(iRibbonItem);
            if (parentGroup is null) return SearchMenuGroupOtherCaption;

            if (parentGroup.ParentPage is null)
            {
                groupSortOrder = parentGroup.MergeOrder.ToString("0000000") + " => " + parentGroup.GroupText;
                return parentGroup.GroupText;
            }
            var parentPage = parentGroup.ParentPage;
            groupSortOrder = parentPage.MergeOrder.ToString("0000000") + " => " + parentPage.PageText + " => " + parentGroup.MergeOrder.ToString("0000000") + " => " + parentGroup.GroupText;
            return $"{parentPage.PageText}: {parentGroup.GroupText}";
        }
        /// <summary>
        /// Najde grupu Ribbonu <see cref="IRibbonGroup"/>, do které patří daný prvek ribbonu.
        /// Pozor, prvek může existovat v Ribbonu standardně (viditelný) anebo může být Added = připravený pro OnDemand doplnění, ale i v tom případě chceme najít reálnou deklaraci grupy, do které bude prvek patřit!
        /// </summary>
        /// <param name="iItemData"></param>
        /// <returns></returns>
        private IRibbonGroup SearchForParentGroupForItem(IRibbonItem iItemData)
        {
            if (iItemData is null || iItemData.ParentGroup is null) return null;               // Pokud prvek není anebo nemá definici grupy, nelze ji najít.

            // Musíme najít reálnou grupu použitou v Ribbonu - podle jejího ID a ID stránky:
            string groupId = iItemData.ParentGroup.GroupId;
            string pageId = iItemData.ParentGroup.ParentPage?.PageId;
            if (_TryGetGroupData(pageId, groupId, out var iGroupData)) return iGroupData;
            return null;
        }
        /// <summary>
        /// Zkusí najít data grupy pro danou stránku.
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="groupId"></param>
        /// <param name="iGroupData"></param>
        /// <returns></returns>
        private bool _TryGetGroupData(string pageId, string groupId, out IRibbonGroup iGroupData)
        {
            if (this.AllGroups.TryGetFirst(g => (g.GroupId == groupId && (pageId == null || g.PageId == pageId)), out var dxGroup))
            {
                iGroupData = dxGroup.DataGroupLast;
                return true;
            }
            iGroupData = null;
            return false;
        }
        /// <summary>
        /// Vrací standardní titulek grupy pro daný prvek v menu QuickSearch
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="groupSortOrder"></param>
        /// <returns></returns>
        private string GetSearchItemGroupCaption(BarItem barItem, out string groupSortOrder)
        {
            groupSortOrder = SearchMenuGroupOtherSortOrder;
            var links = barItem.GetVisibleLinks();
            var link = (links.Count > 0 ? links[0] : null);
            return SearchMenuGroupOtherCaption;
        }
        /// <summary>
        /// Metoda do menu přidá prvek "Příliš mnoho výsledků"
        /// </summary>
        /// <param name="e"></param>
        /// <param name="finalItems"></param>
        /// <param name="resultCount"></param>
        private void AddSearchItemsTooManyMatches(RibbonSearchMenuEventArgs e, SearchMenuItems finalItems, int? resultCount = null)
        {
            if (!resultCount.HasValue) resultCount = finalItems.FoundItems.Count;

            if (finalItems.FoundItems.Count > 0)
                finalItems.FoundItems.Clear();

            // Musím někde najít/vytvořit prvek MaxCount:
            var maxCountItem = SearchItemMaxCountLocalItem;
            if (maxCountItem is null)
            {
                CreateSearchItemTooManyMatches();
                maxCountItem = SearchItemMaxCountLocalItem;
            }

            string groupCaption = SearchMenuGroupTooManyMatchesFoundCaption;
            finalItems.FoundItems.Add("", new SearchMenuItem("", null, groupCaption, null, null, maxCountItem));
        }
        /// <summary>
        /// Metoda zajistí, že pokud není nalezen žádný prvek, bude v menu alespoň prvek NotFound.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="finalItems"></param>
        /// <returns></returns>
        private void AddSearchItemsNotFound(RibbonSearchMenuEventArgs e, SearchMenuItems finalItems)
        {
            // Pokud jsme našli nějaké použitelné prvky, pak NotFound přidávat netřeba:
            if (finalItems.FoundItems.Count > 0) return;

            // Musím někde najít/vytvořit prvek NotFound:
            var notFoundItem = SearchItemNotFoundRibbonItem;
            if (notFoundItem is null)
            {
                notFoundItem = SearchItemNotFoundLocalItem;
                if (notFoundItem is null)
                {
                    CreateSearchItemsNotFound();
                    notFoundItem = SearchItemNotFoundLocalItem;
                }
            }
            string groupCaption = SearchMenuGroupNoMatchesFoundCaption;
            finalItems.FoundItems.Add("", new SearchMenuItem("", null, groupCaption, null, null, notFoundItem));
        }
        /// <summary>
        /// Obsahuje lokalizovaný titulek obecné skupiny v SearchMenu "Výsledky hledání"
        /// </summary>
        private string SearchMenuGroupGeneralCaption { get { return DxComponent.Localize(MsgCode.RibbonSearchMenuGroupGeneralCaption); } }
        /// <summary>
        /// Obsahuje hodnotu třídění náhradní skupiny v SearchMenu "Ostatní"
        /// </summary>
        private string SearchMenuGroupOtherSortOrder { get { return "zzzzzz"; /* "§§§§§§"; */ } }
        /// <summary>
        /// Obsahuje lokalizovaný titulek náhradní skupiny v SearchMenu "Ostatní"
        /// </summary>
        private string SearchMenuGroupOtherCaption { get { return DxComponent.Localize(MsgCode.RibbonSearchMenuGroupOtherCaption); } }
        /// <summary>
        /// Obsahuje lokalizovaný titulek skupiny v SearchMenu "Příliš mnoho výsledků"
        /// </summary>
        private string SearchMenuGroupTooManyMatchesFoundCaption  { get { return DxComponent.Localize(MsgCode.RibbonSearchMenuGroupTooManyMatchesCaption); } }
        /// <summary>
        /// Obsahuje lokalizovaný text prvku v SearchMenu "Upřesněte hledaný text"
        /// </summary>
        private string SearchMenuItemTooManyMatchesFoundCaption { get { return DxComponent.Localize(MsgCode.RibbonSearchMenuItemTooManyMatchesCaption); } }
        /// <summary>
        /// Obsahuje lokalizovaný text grupy (=nadpis) v SearchMenu pro NotFound: "Žádný výsledek"
        /// </summary>
        private string SearchMenuGroupNoMatchesFoundCaption { get { return DxComponent.Localize(MsgCode.RibbonSearchMenuGroupNoMatchesCaption); } }
        /// <summary>
        /// Obsahuje lokalizovaný text prvku (=položka) v SearchMenu pro NotFound: "Upřesněte hledaný text"
        /// </summary>
        private string SearchMenuItemNoMatchesFoundCaption { get { return DxComponent.Localize(MsgCode.RibbonSearchMenuItemNoMatchesCaption); } }
        /// <summary>
        /// Vytvoří new prvek menu typu "Příliš mnoho výsledků"
        /// </summary>
        /// <returns></returns>
        private void CreateSearchItemTooManyMatches()
        {
            BarStaticItem maxCountItem = new BarStaticItem() { Caption = SearchMenuItemTooManyMatchesFoundCaption, Tag = MsgCode.RibbonSearchMenuItemTooManyMatchesCaption };
            SearchItemMaxCountLocalItem = maxCountItem;
            this.Items.Add(maxCountItem);
        }
        /// <summary>
        /// Vytvoří new prvek menu typu "Nenalezeny odpovídající položky"
        /// </summary>
        /// <returns></returns>
        private void CreateSearchItemsNotFound()
        {
            BarStaticItem notFoundItem = new BarStaticItem() { Caption = SearchMenuItemNoMatchesFoundCaption, Tag = MsgCode.RibbonSearchMenuItemNoMatchesCaption };
            SearchItemNotFoundLocalItem = notFoundItem;
            this.Items.Add(notFoundItem);
        }
        /// <summary>
        /// Korektně uvolní prvky, které byly vytvořeny pro Search menu
        /// </summary>
        private void DisposeLastSearchMenuItems()
        {
            var searchMenuLastItems = SearchMenuLastItems;
            if (searchMenuLastItems is null) return;

            searchMenuLastItems.DisposeItems();

            SearchMenuLastItems = null;
        }
        /// <summary>
        /// Při Dispose Ribbonu
        /// </summary>
        private void SearchMenuDestroyContent()
        {
            DisposeLastSearchMenuItems();
            SearchItemNotFoundRibbonItem = null;
            if (SearchItemNotFoundLocalItem != null)
            {
                this.Items.Remove(SearchItemNotFoundLocalItem);
                SearchItemNotFoundLocalItem.Dispose();
            }
            SearchItemNotFoundLocalItem = null;

            if (SearchItemMaxCountLocalItem != null)
            {
                this.Items.Remove(SearchItemMaxCountLocalItem);
                SearchItemMaxCountLocalItem.Dispose();
            }
            SearchItemMaxCountLocalItem = null;
        }
        /// <summary>
        /// Prvky menu SearchMenu posledně vygenerované. Měly by být zlikvidovány. 
        /// Provede se na začátku hledání v <see cref="ModifySearchEditItems(RibbonSearchMenuEventArgs)"/> a při opuštění políčka v <see cref="SearchEdit_Leave(object, EventArgs)"/>, pomocí metody <see cref="DisposeLastSearchMenuItems"/>.
        /// </summary>
        private SearchMenuItems SearchMenuLastItems;
        /// <summary>
        /// Ribbonem vytvořený BarItem pro prvek SearchMenu "Nenalezeny odpovídající položky", používaný opakovaně v rámci Ribbonu
        /// </summary>
        private BarStaticItem SearchItemNotFoundRibbonItem;
        /// <summary>
        /// Lokálně vytvořený BarItem pro prvek SearchMenu "Nenalezeny odpovídající položky", používaný opakovaně v rámci Ribbonu
        /// </summary>
        private BarStaticItem SearchItemNotFoundLocalItem;
        /// <summary>
        /// Lokálně vytvořený BarItem pro prvek SearchMenu "Příliš mnoho výsledků", používaný opakovaně v rámci Ribbonu
        /// </summary>
        private BarStaticItem SearchItemMaxCountLocalItem;
        /// <summary>
        /// Režim třídění skupin v SearchMenu
        /// </summary>
        public SearchMenuGroupSortMode SearchMenuGroupSort { get; set; }
        /// <summary>
        /// Počet výsledků v SearchMenu, při jehož překročení bude ve výsledcích zobrazen pouze static item s textem "Příliš mnoho výsledků".
        /// Hodnota 0 a záporné = neaktivní, vždy budou zobrazeny všechny výsledky.
        /// </summary>
        public int SearchMenuMaxResultCount { get; set; }
        /// <summary>
        /// Počet výsledků v SearchMenu, při jehož překročení budou výsledky zobrazeny "úsporně" = pouze ikony vedle sebe, bez textu.
        /// Hodnota 0 a záporné = neaktivní, vždy budou zobrazeny všechny výsledky v plné formě.
        /// </summary>
        public int SearchMenuShrinkResultCount { get; set; }
        /// <summary>
        /// Třídění skupin v SearchMenu
        /// </summary>
        public enum SearchMenuGroupSortMode 
        {
            /// <summary>
            /// Skupiny v SearchMenu budou tříděny podle <u>názvu stránky a názvu skupiny</u>.
            /// </summary>
            PageCaptionGroupCaption,
            /// <summary>
            /// Skupiny v SearchMenu budou tříděny podle <u>pořadí stránky v Ribbonu a pořadí skupiny skupiny na stránce</u>.
            /// </summary>
            PageOrderGroupCaption
        }

        #region private třídy SearchMenuItems (celý balíček dat) a SearchMenuItem (jedna položka v SearchMenu)
        /// <summary>
        /// Třída zapouzdřující data pro modifikaci Search menu
        /// </summary>
        private class SearchMenuItems
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public SearchMenuItems(List<BarItem> allItems)
            {
                this.AllItems = allItems;
                this.FoundItems = new Dictionary<string, SearchMenuItem>();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "FoundItems.Count: " + FoundItems.Count.ToString();
            }
            /// <summary>
            /// Korektně uvolní prvky, které byly vytvořeny pro Search menu
            /// </summary>
            public void DisposeItems()
            {
                this.AllItems = null;

                var foundItems = this.FoundItems;
                if (foundItems != null)
                {
                    foreach (var foundItem in foundItems.Values)
                        foundItem.DisposeItem();
                    this.FoundItems = null;
                }
            }
            /// <summary>
            /// Všechny prvky k prohledání
            /// </summary>
            public List<BarItem> AllItems;
            /// <summary>
            /// Kolekce prvků.
            /// Klíčem v Dictionary je ID prvku s prefixem "ID:", prvky které nemají ID zde mají pořadové číslo prvku s prefixem "TX:";
            /// </summary>
            public Dictionary<string, SearchMenuItem> FoundItems;
        }
        /// <summary>
        /// Jeden prvek v SearchMenu, jeho data
        /// </summary>
        private class SearchMenuItem
        {
            public SearchMenuItem() { }
            public SearchMenuItem(string itemId, string itemCaption, string groupCaption, string groupSortOrder, IRibbonItem item, BarItem barItem)
            {
                ItemId = itemId;
                ItemCaption = itemCaption ?? item?.Text ?? barItem?.Caption ?? "";
                GroupCaption = groupCaption;
                GroupSortOrder = groupSortOrder ?? "";
                Item = item;
                BarItem = barItem;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Group: {GroupCaption}; Item: {ItemCaption}";
            }

            /// <summary>
            /// Uvolní z paměti svoje data
            /// </summary>
            public void DisposeItem()
            {
                BarLink?.Dispose();
                BarLinkThumbImage?.Dispose();

                ItemId = null;
                ItemCaption = null;
                GroupCaption = null;
                Item = null;
                BarItem = null;
                BarLink = null;
                BarLinkThumbImage = null;
            }
            public string ItemId;
            public string ItemCaption;
            public string GroupCaption;
            public string GroupSortOrder;
            public IRibbonItem Item;
            public BarItem BarItem;
            public BarItemLink BarLink;
            public Image BarLinkThumbImage;
        }
        #endregion

        /// <summary>
        /// Zajistí, že v this Ribbonu a v jeho Child ribbonech bude připraven seznam dodatkových prvků v poli <see cref="_SearchEditItems"/>.
        /// Tato metoda se má volat při každém vstupu do políčka SearchEdit.
        /// </summary>
        protected void RefreshSearchEditItems()
        {
            // Provedeme pro všechny mergované instance Ribonů, počínaje this, až do dvanáctého kolena:
            var ribbon = this;
            for (int l = 0; l < 12; l++)
            {
                if (ribbon is null) break;

                // Pokud konkrétní Ribbon dosud nemá připravené prvky v _SearchEditItems a aktuálně ani nebylo vyvoláno jejich donačítání, zahájíme to nyní v rímci toho Ribbonu:
                if (ribbon._SearchEditItems == null && !ribbon._SearchEditItemsLoading)
                    ribbon.RunLoadSearchEditItems();

                // Přejdeme na můj Child Ribbon:
                ribbon = ribbon.MergedChildDxRibbon;
            }
        }
        /// <summary>
        /// Vyžádá si donačtení prvků do <see cref="_SearchEditItems"/>
        /// </summary>
        private void RunLoadSearchEditItems()
        {
            _SearchEditItemsLoading = true;
            OnLoadSearchEditItems();
            LoadSearchEditItems?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Metoda je volána v okamžiku, kdy je třeba získat dodatkové prvky do SearchEdit v tomto Ribbonu.
        /// </summary>
        protected virtual void OnLoadSearchEditItems() { }
        /// <summary>
        /// Událost je volána v okamžiku, kdy je třeba získat dodatkové prvky do SearchEdit v tomto Ribbonu.
        /// Aplikace má za úkol vytvořit pole prvků, které budou nabízeny v SearchEdit políčku pro rychlé hledání, a toto pole vložit do <see cref="SearchEditItems"/>.
        /// </summary>
        public event EventHandler LoadSearchEditItems;
        /// <summary>
        /// Pole prvků, které budou nabízeny v SearchEdit políčku pro rychlé hledání.
        /// Prvky mohou být vloženy kdykoliv, i z threadu na pozadí.
        /// Typicky jsou vloženy po proběhnutí eventu <see cref="LoadSearchEditItems"/>, ale mohou být vloženy i kdykoliv jindy (při refreshi ribbonu).
        /// Pozor: po vložení null bude v případě potřeby znovu volán event <see cref="LoadSearchEditItems"/>. 
        /// Pokud aplikace nemá žádné takové prvky, pak ať nereaguje na event <see cref="LoadSearchEditItems"/>, nebo ať sem vloží prázdné pole.
        /// </summary>
        public IRibbonItem[] SearchEditItems
        {
            get { return _SearchEditItems; }
            set
            {
                _SearchEditItems = value;
                _SearchEditItemsLoading = false;
            }
        }
        /// <summary>
        /// Pole dodatkových prvků do SearchEdit, za this Ribbon (neobsahuje prvky Child Ribbonů).
        /// Pokud je null, nebylo dosud načteno. 
        /// Pokud je null a přitom v <see cref="_SearchEditItemsLoading"/> je true, pak aktuálně běží načítání a není třeba načítat opakovaně.
        /// </summary>
        private IRibbonItem[] _SearchEditItems;
        /// <summary>
        /// Pole dodatkových prvků v menu SearchEdit, které byly fyzicky přidány do this Ribbonu. 
        /// Toto pole obsahuje linky pocházející z this Ribbonu i ze všech Child ribbonů.
        /// Při vstupu do SearchEdit je vygenerována prázdní instance pole, v procesu editace je plněno aktuálními prvky, po odchodu je nullováno.
        /// Při změně textu SearchText jsou tyto prvky odebrány z SearchMenu, jsou vyhledány nové a přidány do menu i do tohoto pole.
        /// </summary>
        private List<BarItemLink> _AddedSearchMenuItems;
        /// <summary>
        /// Aktuálně byl vydán požadavek na načtení prvků do pole <see cref="_SearchEditItems"/> (událost <see cref="LoadSearchEditItems"/>), 
        /// ale dosud nebyla dodána data do <see cref="SearchEditItems"/>.
        /// Po dodání dat bude opět nastaveno false.
        /// </summary>
        private bool _SearchEditItemsLoading;
        #endregion
        #region TitleBarItems
        /// <summary>
        /// Prvky zobrazené v TitleBaru = titulek okna, napravo, doleva od tří systémových buttonů (Minimize - Maximize - Close).
        /// </summary>
        public IRibbonItem[] TitleBarItems
        {
            get { return _TitleBarItems; }
            set
            {
                _TitleBarRemoveItems(_TitleBarItems);
                _TitleBarAddItems(value, true);
                _TitleBarItems = value;
            }
        }
        /// <summary>Prvky zobrazené v TitleBaru</summary>
        private IRibbonItem[] _TitleBarItems;
        /// <summary>
        /// Metoda přidá do titulkového baru 'CaptionBarItemLinks' dodané prvky.
        /// </summary>
        /// <param name="titleBarItems"></param>
        /// <param name="clear"></param>
        private void _TitleBarAddItems(IEnumerable<IRibbonItem> titleBarItems, bool clear = false)
        {
            if (clear) _TitleBarClearItems();

            // this._AddCaptionTest();

            if (titleBarItems != null)
            {
                // DAJ 28.3.2025: komponenty DevExpress počínaje verzí 24 mají opačné třídění prvků než dřívější verze...
                // Z pohledu DevExpress byla ve starších verzích chyba, kdy prvky vložené do CaptionBarItemLinks se přidávaly zprava doleva.
                //   Na toto pořadí máme nastavený aplikační server, který generuje ikony do actions.HeaderBarActions.Items v pořadí zprava doleva.
                //   Protože v tom jsou zapojené i extendery partnerů, nebudeme obracet ikony na zdroji = na serveru.
                // DevExpress to od verze 23.2.4 opravil, a ikony vkládá v nativním pořadí zleva doprava.
                //   https://supportcenter.devexpress.com/ticket/details/T1220891/ribboncontrol-item-position-is-changed-in-the-captionbaritemlinks-collection
                // Řešení: od verze DX knihoven 23.2.4 budeme obracet pořadí definovaných ikon na klientu = právě zde:
                var sortedItems = titleBarItems.ToList();
                if (sortedItems.Count > 1 && DxVersion >= Version.Parse("23.2.4"))
                    sortedItems.Reverse();

                foreach (var ribbonItem in sortedItems)
                {
                    var iRibbonItem = ribbonItem;
                    var barItem = CreateItem(ref iRibbonItem);
                    if (barItem != null)
                    {
                        _TitleBarSetManagerToItem(barItem);
                        var barLink = this.CaptionBarItemLinks.Add(barItem, iRibbonItem.ItemIsFirstInGroup);
                    }
                }
            }

            // this._AddCaptionTest();
        }
        /// <summary>
        /// Aktuální verze DX komponenty RibbonControl
        /// </summary>
        private static System.Version DxVersion
        {
            get
            {
                if (__DxVersion is null)
                    __DxVersion = _ReadDxVersion();
                return __DxVersion;
            }
        }
        private static System.Version __DxVersion;
        /// <summary>
        /// Načte a vrátí aktuální verzi DX komponenty RibbonControl
        /// </summary>
        /// <returns></returns>
        private static Version _ReadDxVersion()
        {
            var info = new DxAssemblyInfo(typeof(DevExpress.XtraBars.Ribbon.RibbonControl));



            Version result = null;
            try
            {
                var assembly = typeof(DevExpress.XtraBars.Ribbon.RibbonControl).Assembly;
                var attribute = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false).FirstOrDefault();
                if (attribute != null && attribute is System.Reflection.AssemblyFileVersionAttribute version)
                {
                    result = Version.Parse(version.Version);
                }
            }
            catch
            {
                result = null;
            }
            if (result is null)
                result = Version.Parse("99.00.00");
            return result;
        }
        private void _TitleBarRemoveItems(IEnumerable<IRibbonItem> items)
        {
            if (items is null) return;

        }
        /// <summary>
        /// Smaže všechny prvky zobrazované v TitleBaru
        /// </summary>
        private void _TitleBarClearItems()
        {
            var captionLinks = this.CaptionBarItemLinks;
            if (captionLinks is null || captionLinks.Count == 0) return;

            BarItemLink[] links = captionLinks.OfType<BarItemLink>().ToArray();
            captionLinks.Clear();

            foreach (var link in links)
            {
                var item = link.Item;
                if (this.Items.Contains(item))
                    this.Items.Remove(item);
            }
        }
        /// <summary>
        /// Explicitně vloží Managera do předaného prvku, který bude umístěn do CaptionBaru (TitleBar).
        /// Tam se má vkládat manager trochu odlišně než do běžných prvků
        /// </summary>
        /// <param name="titleBarItem"></param>
        private void _TitleBarSetManagerToItem(BarItem titleBarItem)
        {
            titleBarItem.Manager = this.BarManagerInt;
        }
        /// <summary>
        /// Jméno ikony, která se použije jako defaultní pro Caption menu = tlačítka v titulkovém řádku.
        /// Ikona musí být viditelná ve všech skinech.
        /// </summary>
        internal static string CaptionMenuDefaultIcon { get { return "images/setup/properties_16x16.png"; } }
        #endregion
        #region StatusBarItems
        /// <summary>
        /// Prvky zobrazené ve StatusBaru.
        /// </summary>
        public IRibbonItem[] StatusBarItems
        {
            get { return _StatusBarItems; }
            set
            {
                _StatusBarRemoveItems(_StatusBarItems);
                _StatusBarAddItems(value, true);
                _StatusBarItems = value;
            }
        }
        /// <summary>Prvky zobrazené v StatusBaru</summary>
        private IRibbonItem[] _StatusBarItems;
        private void _StatusBarAddItems(IEnumerable<IRibbonItem> items, bool clear = false)
        {
            if (clear) _StatusBarClearItems();
            if (items is null) return;

            var mode = DxRibbonCreateContentMode.CreateAllSubItems;
            int count = 0;
            foreach (var ribbonItem in items)
            {
                var iRibbonItem = ribbonItem;
                bool isFirst = this.StatusBar.ItemLinks.Count == 0;
                var barItem = this.GetItem(ref iRibbonItem, null, 0, mode, ref count);
                if (barItem != null)
                {
                    _TitleBarSetManagerToItem(barItem);
                    var barLink = this.StatusBar.ItemLinks.Add(barItem, iRibbonItem.ItemIsFirstInGroup && !isFirst);
                }
            }
        }
        private void _StatusBarRemoveItems(IEnumerable<IRibbonItem> items)
        {
            if (items is null) return;

        }
        /// <summary>
        /// Smaže všechny prvky zobrazované v TitleBaru
        /// </summary>
        private void _StatusBarClearItems()
        {
            var statusBarLinks = this.StatusBar.ItemLinks;
            if (statusBarLinks is null || statusBarLinks.Count == 0) return;

            BarItemLink[] links = statusBarLinks.OfType<BarItemLink>().ToArray();
            statusBarLinks.Clear();

            foreach (var link in links)
            {
                var item = link.Item;
                if (this.Items.Contains(item))
                    this.Items.Remove(item);
            }
        }
        #endregion
        #region Aktuálně otevřené menu
        /// <summary>
        /// Control, který bude otevřen jako PopupControl v případě potřeby.
        /// <para/>
        /// Ribbon smí mít v jeden okamžik otevřené jen jedno menu = k jednomu tlačítku.
        /// Otevřít jej může uživatel kliknutím na dané tlačítko, systém může oteření pozdržet (pokud to jde OnDemand load) a následně po dodání dat bude tlačítko reálně otevřeno = proběhne otevření tohoto controlu.
        /// </summary>
        private PopupControl _OpenItemPopupMenu;
        /// <summary>
        /// Control, který bude otevřen jako BarSubItem v případě potřeby.
        /// <para/>
        /// Ribbon smí mít v jeden okamžik otevřené jen jedno menu = k jednomu tlačítku.
        /// Otevřít jej může uživatel kliknutím na dané tlačítko, systém může oteření pozdržet (pokud to jde OnDemand load) a následně po dodání dat bude tlačítko reálně otevřeno = proběhne otevření tohoto controlu.
        /// </summary>
        private BarSubItem _OpenItemBarMenu;
        /// <summary>
        /// Požadavek true: 
        /// Aktuálně otevírané menu BarSubItem nemá v rámci metody GetItemData() vyvolat RunItemOnDemandLoad() - to bychom se zacyklili.
        /// Menu je nyní otevíráno z požadavku OpenMenu v rámci RefreshItem, a NESMÍME tedy volat opakovaně RunItemOnDemandLoad() !!!
        /// </summary>
        private bool _OpenItemBarMenuSkipRunOnDemandLoad;
        /// <summary>
        /// ItemId prvku _OpenMenuBarMenu. Identifikuje prvek _OpenMenuBarMenu v situaci, kdy tento se při refreshi (Group nebo Page) zahazuje a generuje nové menu, tedy instance _OpenMenuBarMenu se ztrácí.
        /// </summary>
        private string _OpenItemBarMenuItemId;
        /// <summary>
        /// ItemId prvku _OpenMenuBarLink. Identifikuje prvek _OpenMenuBarLink v situaci, kdy tento se při refreshi (Group nebo Page) zahazuje a generuje nové menu, tedy instance _OpenMenuBarLink se ztrácí.
        /// </summary>
        private string _OpenItemPopupMenuItemId;
        /// <summary>
        /// Souřadnice pro otevření menu.
        /// <para/>
        /// Ribbon smí mít v jeden okamžik otevřené jen jedno menu = k jednomu tlačítku.
        /// Otevřít jej může uživatel kliknutím na dané tlačítko, systém může oteření pozdržet (pokud to jde OnDemand load) a následně po dodání dat bude tlačítko reálně otevřeno = na této souřadnici.
        /// </summary>
        private Point? _OpenMenuLocation;
        /// <summary>
        /// Resetuje info o aktuálně otevřeném menu (pokud právě neprobíhá UnMerge/Merge), anebo pokud je to výslovné příní volajícího (<paramref name="force"/> = true).
        /// </summary>
        /// <param name="force"></param>
        private void _OpenMenuReset(bool force = false)
        {
            if (force || (!CurrentModifiedState && _CurrentMergeState == MergeState.None))
            {   // Pokud by se aktuálně prováděl Merge nebo UnMerge, pak zhasnutí menu nemá provádět reset menu - protože poté (po UnMerge - Modify - Merge) budeme potřebovat dosavadní menu opět rozsvítit!!!
                _OpenItemPopupMenu = null;
                _OpenItemBarMenu = null;
                _OpenItemBarMenuItemId = null;
                _OpenItemPopupMenuItemId = null;
                _OpenMenuLocation = null;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this instance <see cref="DxRibbonControl"/> anebo mergovaný Child ribbon (<see cref="MergedChildDxRibbon"/>) má mít otevřené nějaké menu.
        /// Po dokončení procesu { UnMerge - Modify - Merge } by mělo být otevřeno!
        /// Tato property se dívá do Child Ribbonů.
        /// </summary>
        protected bool NeedOpenMenu 
        {
            get 
            {
                if (NeedOpenMenuCurrent) return true;
                if (this.MergedChildDxRibbon != null) return this.MergedChildDxRibbon.NeedOpenMenu;
                return false;
            }
        }
        /// <summary>
        /// Metoda zajistí otevření toho menu, které by mělo být otevřeno.
        /// Tato metoda vyhledá vhodný Child Ribbon v pořadí this (=Parent) - <see cref="MergedChildDxRibbon"/> - SubChild..., a vyvolá (rekurzivně) výkonnou metodu v patřičné instanci.
        /// </summary>
        protected void DoOpenMenu()
        {
            if (this.NeedOpenMenuCurrent)
                DoOpenMenuCurrent();
            else if (this.MergedChildDxRibbon != null)
                this.MergedChildDxRibbon.DoOpenMenu();
        }
        /// <summary>
        /// Obsahuje true, pokud this instance <see cref="DxRibbonControl"/> má mít otevřené nějaké menu. Po dokončení procesu { UnMerge - Modify - Merge } by mělo být otevřeno!
        /// Tato property se nedívá do Child Ribbonů.
        /// </summary>
        protected bool NeedOpenMenuCurrent { get { return (_OpenItemPopupMenu != null || _OpenItemBarMenu != null); } }
        /// <summary>
        /// Metoda zajistí otevření menu v this instanci Ribbonu.
        /// </summary>
        private void DoOpenMenuCurrent()
        {
            Point popupLocation = _OpenMenuLocation ?? Control.MousePosition;
            if (_OpenItemPopupMenu != null)
                _PopupMenu_OpenMenu(_OpenItemPopupMenu, popupLocation);
            else if (_OpenItemBarMenu != null)
                _BarMenu_OpenMenu(_OpenItemBarMenu, popupLocation);
        }
        #endregion
        #region Přístup ke stránkám Ribbonu (vlastní, od kategorií, mergované)
        /// <summary>
        /// Vrátí soupis stránek z this ribbonu z daných pozic
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public List<DevExpress.XtraBars.Ribbon.RibbonPage> GetPages(PagePosition position)
        {
            var tpc = this.TotalPageCategory;
            var vpc = tpc.GetVisiblePages();

            List<DevExpress.XtraBars.Ribbon.RibbonPage> result = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();

            bool withDefault = position.HasFlag(PagePosition.Default);
            bool withCategories = position.HasFlag(PagePosition.PageCategories);
            bool withMergedDefault = position.HasFlag(PagePosition.MergedDefault);
            bool withMergedCategories = position.HasFlag(PagePosition.MergedCategories);

            if (withDefault)
                result.AddRange(this.Pages);

            if (withCategories)
                result.AddRange(this.PageCategories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.Pages));

            if (withCategories && (withMergedDefault || withMergedCategories))
                result.AddRange(this.PageCategories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.MergedPages));


            if (withMergedDefault)
                result.AddRange(this.MergedPages);

            if (withMergedCategories)
                result.AddRange(this.MergedCategories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.Pages));

            if (withMergedCategories)
                result.AddRange(this.MergedCategories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.MergedPages));

            return result;
        }
        /// <summary>
        /// Vrátí pozici, na které se v this Ribbonu nachází daná stránka
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public PagePosition GetPagePosition(DevExpress.XtraBars.Ribbon.RibbonPage page)
        {
            if (page == null) return PagePosition.None;
            if (_IsPageOnPosition(page, PagePosition.Default)) return PagePosition.Default;
            if (_IsPageOnPosition(page, PagePosition.PageCategories)) return PagePosition.PageCategories;
            if (_IsPageOnPosition(page, PagePosition.MergedDefault)) return PagePosition.MergedDefault;
            if (_IsPageOnPosition(page, PagePosition.MergedCategories)) return PagePosition.MergedCategories;
            return PagePosition.None;
        }
        /// <summary>
        /// Úplně všechny stránky v Ribbonu.
        /// Obsahuje i pro stránky kategorií i stránky mergované a stránky z mergovaných kategorií.
        /// </summary>
        protected List<DevExpress.XtraBars.Ribbon.RibbonPage> AllPages { get { return GetPages(PagePosition.All); } }
        /// <summary>
        /// Úplně všechny VLASTNÍ stránky v Ribbonu.
        /// Obsahuje tedy základní stránky plus pro stránky vlastních kategorií.
        /// Neobsahuje stránky mergované ani stránky z mergovaných kategorií.
        /// </summary>
        protected List<DevExpress.XtraBars.Ribbon.RibbonPage> AllOwnPages { get { return GetPages(PagePosition.AllOwn); } }
        /// <summary>
        /// Úplně všechny VLASTNÍ stránky v Ribbonu.
        /// Obsahuje tedy základní stránky plus pro stránky vlastních kategorií.
        /// Neobsahuje stránky mergované ani stránky z mergovaných kategorií.
        /// </summary>
        protected List<DxRibbonPage> AllOwnDxPages { get { return AllOwnPages.OfType<DxRibbonPage>().ToList(); } }
        /// <summary>
        /// Vrátí true, pokud daná stránka je v this Ribbonu na dané pozici. Lze zadat více pozic najednou (<see cref="PagePosition"/> je Flags)
        /// </summary>
        /// <param name="page"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool _IsPageOnPosition(DevExpress.XtraBars.Ribbon.RibbonPage page, PagePosition position)
        {
            var pages = GetPages(position);
            return pages.Contains(page);
        }
        #region enum PagePosition
        /// <summary>
        /// Pozice stránky
        /// </summary>
        [Flags]
        public enum PagePosition
        {
            /// <summary>
            /// Nelze určit
            /// </summary>
            None = 0,
            /// <summary>
            /// Základní stránka, nepatřící do kategorie
            /// </summary>
            Default = 0x01,
            /// <summary>
            /// Stránka patřící kategorii
            /// </summary>
            PageCategories = 0x02,
            /// <summary>
            /// Mergovaná základní stránka
            /// </summary>
            MergedDefault = 0x10,
            /// <summary>
            /// Stránka patřící mergovaé kategorii
            /// </summary>
            MergedCategories = 0x20,

            /// <summary>
            /// Vlastní stránky <see cref="Default"/> a <see cref="PageCategories"/>
            /// </summary>
            AllOwn = Default | PageCategories,
            /// <summary>
            /// Mergované stránky <see cref="MergedDefault"/> a <see cref="MergedCategories"/>
            /// </summary>
            AllMerged = MergedDefault | MergedCategories,
            /// <summary>
            /// Všechny stránky
            /// </summary>
            All = Default | PageCategories | MergedDefault | MergedCategories
        }
        #endregion
        #endregion
        #region Přístup ke grupám Ribbonu
        /// <summary>
        /// Pole všech skupin, které jsou vygenerované přímo v this Ribbonu
        /// </summary>
        public DxRibbonGroup[] Groups
        {
            get
            {
                return _Groups.ToArray();
            }
        }
        /// <summary>
        /// Pole všech skupin, které jsou vygenerované jak přímo v this Ribbonu, tak přidané grupy ze všech Ribbonů, které jsou mergovány do this.
        /// Pořadí: nejprve zdejší, potom nejbližší nižší, atd.
        /// </summary>
        public DxRibbonGroup[] AllGroups
        {
            get
            {
                List<DxRibbonGroup> allGroups = new List<DxRibbonGroup>();
                var ribbon = this;
                while (ribbon != null)
                {
                    allGroups.AddRange(ribbon._Groups);
                    ribbon = ribbon.MergedChildDxRibbon;
                }
                return allGroups.ToArray();
            }
        }
        private List<DxRibbonGroup> _Groups;
        /// <summary>
        /// Metoda zkusí najít existující grupu, pouze vlastní v this ribbonu, anebo včetně mergovaných (podle <paramref name="searchAllGroups"/>).
        /// Hledá podle jejího ID <see cref="RibbonPageGroup.Name"/>, které by mělo být unikátní.
        /// Unikátnost přes celý Ribbon ale není striktně vyžadována, teoreticky smí být více grup se shodným ID, pokud jsou na různých stránkách.
        /// Doporučuje se ale používat ID unikátně přes celý Ribbon.
        /// <para/>
        /// Tato metoda vrací FirstOrDefault(). Pokud je třeba najít všechny grupy podle ID, použijte pole <see cref="Groups"/>.
        /// Tato metoda tedy nepoužívá Dictionary, ale prosté iterování pole.
        /// </summary>
        /// <param name="groupId">ID hledané grupy</param>
        /// <param name="searchAllGroups">true = hledat včetně mergovaných grup / false = jen zdejší</param>
        /// <param name="dxGroup">Out nalezená grupa</param>
        /// <returns></returns>
        internal bool TryGetGroup(string groupId, bool searchAllGroups, out DxRibbonGroup dxGroup)
        {
            if (groupId == null)
            {
                dxGroup = null;
                return false;
            }
            var groups = searchAllGroups ? this.AllGroups : this.Groups;
            return groups.TryGetFirst(g => g.Name == groupId, out dxGroup);
        }
        /// <summary>
        /// Metoda zkusí najít existující stránku, pouze vlastní v this ribbonu, ne v mergovaných
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        internal bool TryGetPage(string pageId, out DxRibbonPage dxPage)
        {
            if (pageId == null)
            {
                dxPage = null;
                return false;
            }
            return this.AllOwnDxPages.TryGetFirst(p => p.Name == pageId, out dxPage);
        }
        #endregion
        #region Tvorba obsahu Ribbonu: Clear(), ClearPageContents(), RemoveVoidContainers(), AddPages(), RefreshPages(), RefreshItems(), RefreshItem()
        /// <summary>
        /// Smaže celý obsah Ribbonu. Ribbon se zmenší na řádek pro záhlaví a celé okno pod ním se přeuspořádá.
        /// Důvodem je smazání všech stránek.
        /// Jde o poměrně nehezký efekt.
        /// </summary>
        public void Clear()
        {
            _UnMergeModifyMergeCurrentRibbon(_Clear, true);
        }
        /// <summary>
        /// Smaže svůj obsah - stránky, kategorie, QAT toolbar, evidence položek.
        /// </summary>
        private void _Clear()
        { 
            var startTime = DxComponent.LogTimeCurrent;

            string lastSelectedPageFullId = this.SelectedPageFullId;
            int removeItemsCount = 0;
            try
            {
                _ClearingNow = true;

                this._Groups.Clear();
                this.Pages.Clear();
                this.Categories.Clear();
                this.PageCategories.Clear();
                this.Toolbar.ItemLinks.Clear();
                this._QATDirectItems = null;
                this._ClearItems(true, true, true, ref removeItemsCount);
            }
            finally
            {
                this.LastSelectedPageFullId = lastSelectedPageFullId;
                _ClearingNow = false;
            }

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === ClearRibbon {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Korektně smaže BarItemy z this.Items.
        /// Ponechává tam systémové prvky a volitelně DirectQAT prvky + CaptionBar !
        /// </summary>
        /// <param name="removeDirectQatItems"></param>
        /// <param name="removeCaptionItems"></param>
        /// <param name="removeStatusItems"></param>
        /// <param name="removeItemsCount"></param>
        private void _ClearItems(bool removeDirectQatItems, bool removeCaptionItems, bool removeStatusItems, ref int removeItemsCount)
        {
            // Pokud bych dal this.Items.Clear(), tak přijdu o všechny prvky celého Ribbonu,
            //   a to i o "servisní" = RibbonSearchEditItem, RibbonExpandCollapseItem, AutoHiddenPagesMenuItem.
            // Ale když nevyčistím Itemy, budou tady pořád strašit...
            // Ponecháme prvky těchto typů: "DevExpress.XtraBars.RibbonSearchEditItem", "DevExpress.XtraBars.InternalItems.RibbonExpandCollapseItem", "DevExpress.XtraBars.InternalItems.AutoHiddenPagesMenuItem"
            // Ponecháme volitelně prvky QAT panelu, a CaptionBar
            // Následující algoritmus NENÍ POMALÝ: smazání 700 Itemů trvá 11 milisekund.
            // Pokud by Clear náhodou smazal i nějaké další systémové prvky, je nutno je určit = určit jejich FullType a přidat jej do metody _IsSystemItem() !

            int count = this.Items.Count;

            // Poznámka 8.9.2023: Ribbon si po smazání prvků ze stránek dobře čistí Items, takže zdejší (dolní) metoda je nepotřebná.
            //    Můžeme provést test, zda najdu nějaký this.Items, který nikam nepatří (nemá BarItemLink):
            for (int i = count - 1; i >= 0; i--)
            {
                var item = this.Items[i];
                bool isUsed = false;
                var links = item.Links;
                foreach (BarItemLink link in links)
                {
                    isUsed = link.IsLinkInMenu || link.IsPageGroupContentToolbarButtonLink || link.LinkedObject != null || link.OwnerPageGroup != null || link.Bar != null || link.IsGalleryToolbarItemLink;
                    if (isUsed) break;
                }

                if (!isUsed)
                {
                    this.Items.RemoveAt(i);
                    removeItemsCount++;
                }
            }


            /*
            // Komplexní smazání - není nutné, Ribbon se čistí dobře:
            var qatItems = _QATDirectAllItemKeys;
            var captionItems = this.CaptionBarItemLinks?.Where(l => !String.IsNullOrEmpty(l.Item.Name)).CreateDictionary(l => l.Item.Name, true);
            var statusBarItems = this.StatusBar?.ItemLinks.Where(l => !String.IsNullOrEmpty(l.Item.Name)).CreateDictionary(l => l.Item.Name, true);

            for (int i = count - 1; i >= 0; i--)
            {
                var item = this.Items[i];
                var name = item.Name;

                if (_IsSystemItem(item)) continue;                   // Systémové nesmažu

                if (!removeDirectQatItems && qatItems != null)
                {
                    var qatKey = GetValidQATKey(name);
                    if (qatItems.ContainsKey(qatKey)) continue;      // QAT nemám mazat, a toto je QAT prvek
                }

                if (!removeCaptionItems && captionItems != null)
                {
                    if (captionItems.ContainsKey(name)) continue;    // Caption prvky nemám mazat, a tento prvek je na Caption baru
                }

                if (!removeStatusItems && statusBarItems != null)
                {
                    if (statusBarItems.ContainsKey(name)) continue;  // Status prvky nemám mazat, a tento prvek je na Status baru
                }

                // Nic ho nezachránilo => smažu ho:
                this.Items.RemoveAt(i);
                removeItemsCount++;
            }
            */
        }
        /// <summary>
        /// Vrátí true, pokud daný objekt (pochází z kolekce RibbonControl.Items) je takového typu, že se má považovat za systémový = nesmazatelný z Items
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool _IsSystemItem(BarItem item)
        {
            string fullType = item.GetType().FullName;
            switch (fullType)
            {
                case "DevExpress.XtraBars.RibbonSearchEditItem":
                case "DevExpress.XtraBars.InternalItems.RibbonExpandCollapseItem":
                case "DevExpress.XtraBars.InternalItems.AutoHiddenPagesMenuItem":
                // Až najdu další typy prvků v Ribbonu, které mi Clear smaže a neměl by, tak je přidám sem...
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Vrátí true, pokud daný objekt (pochází z kolekce RibbonControl.Items) je uložen v poli <see cref="_QATDirectItems"/> = jde o přímý QAT prvek
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool _IsQatItem(BarItem item)
        {
            if (_QATDirectItems == null) return false;
            return _QATDirectItems.Any(i => Object.ReferenceEquals(i, item));
        }
        private bool _ClearingNow;
        /// <summary>
        /// Smaže výhradně jednotlivé prvky z Ribbonu (Items a LazyLoadContent) a grupy prvků (Page.Groups).
        /// Ponechává naživu Pages, Categories a PageCategories.
        /// Nesmaže přímý obsah QAT toolbaru.
        /// Tím zabraňuje blikání.
        /// </summary>
        public void ClearPagesContents()
        {
            _UnMergeModifyMergeCurrentRibbon(_ClearPagesContents, true);
        }
        /// <summary>
        /// Smaže obsah (itemy a grupy) ale ponechá Pages, Categories a PageCategories.
        /// </summary>
        private void _ClearPagesContents()
        {
            var startTime = DxComponent.LogTimeCurrent;

            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.AllOwnPages)
                DxRibbonPage.ClearContentPage(page);

            int removeItemsCount = 0;
            this._ClearItems(false, false, false, ref removeItemsCount);

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === ClearPageContents {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Smaže prázdné prázdné stránky a nevyužité kategorie v rámci this Ribbonu.
        /// Dovoluje provádět výměnu obsahu Ribbonu bez blikání, procesem: 
        /// <see cref="ClearPagesContents()"/>; 
        /// <see cref="AddPages(IEnumerable{IRibbonPage}, bool)"/>;
        /// <see cref="RemoveVoidContainers()"/>;
        /// <para/>
        /// Výměnu obsahu je možno provést i pomocí <see cref="AddPages(IEnumerable{IRibbonPage}, bool)"/> s parametrem clearCurrentContent = true.
        /// </summary>
        public void RemoveVoidContainers()
        {
            _UnMergeModifyMergeCurrentRibbon(_RemoveVoidContainers, true);
        }
        /// <summary>
        /// Smaže prázdné prázdné stránky a nevyužité kategorie v rámci this Ribbonu.
        /// Může být předán seznam stránek, pak smaže pouze prázdné stránky z daného seznamu.
        /// </summary>
        private void _RemoveVoidContainers()
        {
            _RemoveVoidContainers(null);
        }
        /// <summary>
        /// Smaže prázdné prázdné stránky a nevyužité kategorie v rámci this Ribbonu.
        /// Může být předán seznam stránek, pak smaže pouze prázdné stránky z daného seznamu.
        /// </summary>
        /// <param name="pages">Smazat pouze stránky z tohoto seznamu, pokud jsou prázdné. Pokud je null, pak smaže všechny prázdné stránky.</param>
        private void _RemoveVoidContainers(List<DevExpress.XtraBars.Ribbon.RibbonPage> pages)
        {
            bool hasExplicitPages = (pages != null);
            if (hasExplicitPages && pages.Count == 0) return;

            var pageCategories = this.PageCategories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().ToArray();
            foreach (var pageCategory in pageCategories)
            {
                var cPages = pageCategory.Pages.Where(p => p.Groups.Count == 0).ToArray();
                foreach (var cPage in cPages)
                {
                    if (!hasExplicitPages || (hasExplicitPages && pages.Contains(cPage)))
                        pageCategory.Pages.Remove(cPage);
                }
                if (pageCategory.Pages.Count == 0)
                {
                    int index = this.PageCategories.IndexOf(pageCategory);
                    if (index >= 0)
                        this.PageCategories.RemoveAt(index);
                }
            }

            var nPages = this.Pages.Where(p => p.Groups.Count == 0).ToArray();
            foreach (var nPage in nPages)
            {
                if (!hasExplicitPages || (hasExplicitPages && pages.Contains(nPage)))
                    this.Pages.Remove(nPage);
            }
        }
        /// <summary>
        /// Přidá dodané prvky do this ribbonu, zakládá stránky, kategorie, grupy...
        /// Pokud má být aktivní <see cref="UseLazyContentCreate"/>, musí být nastaveno na true před přidáním prvku.
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// <para/>
        /// Pokud bude zadán parametr <paramref name="clearCurrentContent"/>, pak dojde k hladké výměně obsahu Ribbonu. Bez blikání.
        /// </summary>
        /// <param name="iRibbonPages">Definice obsahu</param>
        /// <param name="clearCurrentContent">Smazat stávající obsah Ribbonu, smaže se bez bliknutí</param>
        public void AddPages(IEnumerable<IRibbonPage> iRibbonPages, bool clearCurrentContent = false)
        {
            AddPages(iRibbonPages, clearCurrentContent, false);
        }
        /// <summary>
        /// Přidá dodané prvky do this ribbonu, zakládá stránky, kategorie, grupy...
        /// Pokud má být aktivní <see cref="UseLazyContentCreate"/>, musí být nastaveno na true před přidáním prvku.
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// <para/>
        /// Pokud bude zadán parametr <paramref name="clearCurrentContent"/>, pak dojde k hladké výměně obsahu Ribbonu. Bez blikání.
        /// </summary>
        /// <param name="iRibbonPages">Definice obsahu</param>
        /// <param name="clearCurrentContent">Smazat stávající obsah Ribbonu, smaže se bez bliknutí</param>
        /// <param name="preserveActivePage">Ponechat současnou Selectedpage i po výměně dat, a to i po <paramref name="clearCurrentContent"/></param>
        public void AddPages(IEnumerable<IRibbonPage> iRibbonPages, bool clearCurrentContent, bool preserveActivePage)
        {
            if (iRibbonPages == null) return;

            //  Hodnota 'isCalledFromReFill' je důležitá v následujícím scénáři:
            // 1. Provádíme první naplnění Ribbonu;
            // 2. Definice Ribbonu obsahuje na první pozici stránku typu OdDemandLoad: taková stránka se aktivuje v procesu přidávání stránek;
            // 3. Následně se metoda CheckLazyContentCurrentPage() má postarat o to, aby aktivní stránka (SelectedPage) měla správně načtená data (OnDemand)
            // 4. Musíme tady ale rozlišit dvě situace, a to:
            //   a) vstupující OnDemand stránka neobsahuje data => pokud bude aktivována, pak by MĚLA provést OnDemand donačtení,
            //       protože zdroj dat jen nadeklaroval OnDemnd stránku bez obsahu, ale uživatel chce vidět její obsah;
            //     anebo
            //   b) OnDemand stránka již OBSAHUJE data => pak ale metoda CheckLazyContentCurrentPage() nemá vyvolat OnDemand donačítání (protože bychom se zacyklili),
            //       to je situace, když zdroj dat právě už naplnil stránku daty a posílá ji do Ribbonu.
            //  Řešení:
            //   - příznak isReFill říká, že nyní přicházejí naplněná data (a nebude se tedy žádat o jejich další donačtení)
            //   - příznak isReFill určíme podle toho, že ve vstupních datech je alespoň jedna stránka typu OnDemand, která už v sobě obsahuje data
            bool isEmptyRibbon = (this.GetPages(PagePosition.AllOwn).Count == 0);
            bool isCalledFromReFill = iRibbonPages.Any(p => ((p.PageContentMode == RibbonContentMode.OnDemandLoadOnce || p.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime) && p.Groups.Any()));
            DxRibbonCreateContentMode createMode = _ConvertLazyMode(this.UseLazyContentCreate);
            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    if (clearCurrentContent) _ClearPagesContents();
                    _AddPages(iRibbonPages, true, createMode, "Fill");
                    if (clearCurrentContent) _RemoveVoidContainers();
                }
                ,true);
            });
            CheckLazyContentCurrentPage(isCalledFromReFill);

            // Do první stránky (v aktuálním Ribbonu) vepsat, že má být Aktivní:
            if (isEmptyRibbon || (clearCurrentContent && !preserveActivePage)) StoreFirstPageAsLastActivePage();
        }
        /// <summary>
        /// Konvertuje vstupní požadavek na CreateLazy <see cref="LazyContentMode"/> do interního režimu <see cref="DxRibbonCreateContentMode"/>
        /// </summary>
        /// <param name="lazyMode"></param>
        /// <returns></returns>
        private DxRibbonCreateContentMode _ConvertLazyMode(LazyContentMode lazyMode)
        {
            switch (lazyMode)
            {
                case LazyContentMode.DefaultLazy: return DxRibbonCreateContentMode.None;                                 // Původní Lazy
                case LazyContentMode.Auto: return DxRibbonCreateContentMode.CreateAutoByCount;
                case LazyContentMode.CreateVisibleBarsOnly: return DxRibbonCreateContentMode.CreateGroupsContent;        // Původní NonLazy
                case LazyContentMode.CreateAllItems: return DxRibbonCreateContentMode.CreateAll;
            }
            return DxRibbonCreateContentMode.CreateAll;
        }
        /// <summary>
        /// Metoda vrátí seznam těch zdejších stránek, jejichž Name se vyskytují v dodaném seznamu nových prvků.
        /// </summary>
        /// <param name="iRibbonPages"></param>
        /// <param name="clearContent"></param>
        /// <returns></returns>
        private List<DevExpress.XtraBars.Ribbon.RibbonPage> _PrepareReFill(IEnumerable<IRibbonPage> iRibbonPages, bool clearContent)
        {
            List<DevExpress.XtraBars.Ribbon.RibbonPage> result = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();
            var pageDict = this.AllOwnPages.ToDictionary(p => p.Name);
            foreach (var iRibbonPage in iRibbonPages)
            {
                string pageId = iRibbonPage.PageId;
                if (pageId != null && pageDict.TryGetValue(pageId, out var page))
                    result.Add(page);
            }

            if (clearContent)
                result.ForEachExec(p => DxRibbonPage.ClearContentPage(p));

            return result;
        }
        /// <summary>
        /// Metoda je volána při aktivaci stránky this Ribbonu v situaci, kdy tato stránka má aktivní nějaký Lazy režim pro načtení svého obsahu.
        /// Může to být prostě opožděné vytváření fyzických controlů z dat v paměti, nebo reálné OnDemand donačítání obsahu z aplikačního serveru.
        /// Přidá prvky do this Ribbonu z dodané LazyGroup do this Ribbonu. Zde se prvky přidávají vždy jako reálné, už ne Lazy.
        /// </summary>
        /// <param name="lazyGroup"></param>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        private void PrepareRealLazyItems(DxRibbonLazyLoadInfo lazyGroup, bool isCalledFromReFill)
        {
            // Nemám data nebo jsou neaktivní (=už jsme v procesu PrepareRealLazyItems):
            if (lazyGroup == null || !lazyGroup.IsActive) return;

            // Pokud právě nyní plníme Ribbon daty dodanými OnDemand (=data jsou v ribbonu už vložena) a režime je EveryTime, pak skončíme;
            // protože: a) obsah mazat nechceme,  b) prvky opakovaně vkládat nechceme (už tam jsou),  c) LazyGroup si chceme ponechat,  d) další event spouštět nebudeme:
            if (isCalledFromReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime) return;

            lazyGroup.IsActive = false;
            bool addItems = lazyGroup.HasItems && lazyGroup.PageContentMode == RibbonContentMode.Static;
            var pageData = lazyGroup.PageData;
            var iRibbonPage = lazyGroup.PageData;

            // V této jediné situaci si necháme LazyInfo: když proběhlo naplnění prvků OnDemand => ReFill, a režim OnDemand je "Po každé aktivaci stránky":
            //  Pak máme na aktuální stránce uložené reálné prvky, a současně tam je OnDemand "Group", která zajistí nový OnDemand při následující aktivaci stránky!
            // removeLazyInfo je v opačné situaci:
            bool removeLazyInfo = !((isCalledFromReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime));

            // Za určité situace provedeme Clear prvků stránky: pokud NENÍ ReFill a pokud je režim EveryTime = 
            //  právě zobrazujeme stránku, která při každém zobrazení načítá nová data, takže předešlá data máme zahodit...
            var ownerPage = lazyGroup.OwnerPage;
            bool clearContent = (!isCalledFromReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime) || ownerPage.HasOnlyQatContent;
            string lazyInfo = "LazyFill; Page: " + lazyGroup.PageData.PageText;

            if (addItems || clearContent || removeLazyInfo)
            {   // Když je důvod něco provádět (máme nové prvky, nebo budeme odstraňovat LazyInfo z Ribbonu):
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    if (clearContent)
                    {
                        lazyGroup.OwnerPage?.ClearContent(true, removeLazyInfo);
                    }
                    if (addItems)
                    {   // Když máme co zobrazit, tak nyní vygenerujeme reálné Grupy a BarItems:
                        DxRibbonCreateContentMode createMode = (!isCalledFromReFill ? DxRibbonCreateContentMode.CreateGroupsContent : (DxRibbonCreateContentMode.CreateGroupsContent | DxRibbonCreateContentMode.CreateAllSubItems | DxRibbonCreateContentMode.RunningOnDemandFill));
                        _AddPageLazy(pageData, createMode, lazyInfo);
                    }
                    if (removeLazyInfo)
                    {
                        // LazyGroup odstraníme vždy, i pro oba režimy OnDemand (pro ně za chvilku vyvoláme event OnDemandLoad):
                        // Pokud by byl režim OnDemandLoadEveryTime, pak si novou LazyGroup vygenerujeme společně s dodanými položkami
                        //  v metodě _AddItems s parametrem isReFill = true, podle režimu OnDemandLoadEveryTime!
                        lazyGroup.OwnerPage?.RemoveLazyLoadInfo();
                    }
                    if (clearContent || addItems)
                    {
                        AddQATUserListToRibbon(null);
                    }
                }, true, true);
            }

            if (!removeLazyInfo && lazyGroup != null)
                lazyGroup.IsActive = true;

            if (lazyGroup.IsOnDemand && !isCalledFromReFill)
            {   // Oba režimy OnDemandLoad vyvolají patřičný event, pokud tato metoda NENÍ volána právě z akce naplnění ribbonu daty OnDemand:
                if (!IsSearchEditActive)
                    RunPageOnDemandLoad(iRibbonPage);
            }
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané kolekce, v daném režimu LazyLoad
        /// </summary>
        /// <param name="iRibbonPages"></param>
        /// <param name="reloadQatToRibbon">Na závěr zavolat <see cref="AddQATUserListToRibbon(Dictionary{string, Tuple{BarItem, IRibbonItem}})"/>?</param>
        /// <param name="createMode">Režim přidávání prvků</param>
        /// <param name="logText"></param>
        private void _AddPages(IEnumerable<IRibbonPage> iRibbonPages, bool reloadQatToRibbon, DxRibbonCreateContentMode createMode, string logText)
        {
            if (iRibbonPages is null) return;

            var startTime = DxComponent.LogTimeCurrent;
            var list = DataRibbonPage.SortPages(iRibbonPages);
            int count = 0;
            foreach (var iRibbonPage in list)
                _AddPage(iRibbonPage, createMode, ref count);

            if (reloadQatToRibbon) AddQATUserListToRibbon(null);

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === Ribbon {DebugName}: {logText} {list.Count} item[s]; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané kolekce, v daném režimu LazyLoad
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="createMode">Režim přidávání prvků</param>
        /// <param name="logText"></param>
        private void _AddPageLazy(IRibbonPage iRibbonPage, DxRibbonCreateContentMode createMode, string logText)
        {
            if (iRibbonPage is null) return;

            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            _AddPage(iRibbonPage, createMode, ref count);

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === Ribbon {DebugName}: {logText}; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Metoda přidá do this Ribbonu data další stránky.
        /// </summary>
        /// <param name="iRibbonPage">Deklarace stránky</param>
        /// <param name="createMode">Režim přidávání prvků</param>
        /// <param name="count"></param>
        private void _AddPage(IRibbonPage iRibbonPage, DxRibbonCreateContentMode createMode, ref int count)
        {
            if (iRibbonPage is null) return;

            // Stránku jako takovou vygenerujeme vždycky:
            var pageCategory = GetPageCategory(iRibbonPage.Category, iRibbonPage.ChangeMode);      // Pokud je to třeba, vygeneruje Kategorii
            RibbonPageCollection pages = (pageCategory != null ? pageCategory.Pages : this.Pages); // Kolekce stránek: kategorie / ribbon
            var page = GetPage(iRibbonPage, pages);                                                // Najde / Vytvoří stránku do this.Pages nebo do category.Pages
            if (page is null) return;

            // Spousta logiky kolem toho, co všechno budeme v rámci této stránky generovat teď, a co až někdy v budoucnu:
            bool isActivePage = (this.SelectedPageFullId == GetPageFullId(page));                  // Pokud je stránka aktivní, bude se do ní muset něco dát
            bool pageContainsQatItems = ContainsQAT(iRibbonPage);                                  // Pokud stránka obsahuje nějaké prvky, které jsou přítomny v QAT, budou se muset vygenerovat
            DxRibbonCreateContentMode currentMode = page.PreparePageForCreateContent(iRibbonPage, createMode, isActivePage, pageContainsQatItems);

            // Pokud pro tuhle stránku mám aktivovat budoucí plnění v režimu OnIdle (=nyní jen generujeme prázdnou stránku, anebo jen obsahující QAT prvky, nebo ne-kompletní Sub-Menu), tak si to poznamenám:
            if (currentMode.HasFlag(DxRibbonCreateContentMode.ActivateOnIdleLoad))
                _ActiveLazyLoadPagesOnIdle = true;

            // Pokud pro tuhle stránku v tuhle chvíli nebudu generovat vůbec nic, skončíme:
            if (!currentMode.HasFlag(DxRibbonCreateContentMode.PrepareAnyContent))
                return;

            // Jdeme něco generovat:
            var list = DataRibbonGroup.SortGroups(iRibbonPage.Groups);
            foreach (var iRibbonGroup in list)
            {
                iRibbonGroup.ParentPage = iRibbonPage;
                _AddGroup(iRibbonGroup, page, currentMode, ref count);
            }
        }
        /// <summary>
        /// Metoda přidá danou grupu do dané stránky
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxPage"></param>
        /// <param name="currentMode">Režim tvorby obsahu</param>
        /// <param name="count"></param>
        private void _AddGroup(IRibbonGroup iRibbonGroup, DxRibbonPage dxPage, DxRibbonCreateContentMode currentMode, ref int count)
        {
            if (iRibbonGroup == null || dxPage == null) return;
            if (currentMode.HasFlag(DxRibbonCreateContentMode.CreateOnlyQATItems) && !ContainsQAT(iRibbonGroup)) return;         // V režimu isNeedQAT přidáváme jen prvky QAT, a ten v dané grupě není žádný

            var dxGroup = GetGroup(iRibbonGroup, dxPage);
            if (dxGroup is null) return;

            AddItemsToGroup(iRibbonGroup, dxGroup, currentMode, ref count);
        }
        /// <summary>
        /// Přidá prvky do grupy
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxGroup"></param>
        /// <param name="currentMode">Režim tvorby obsahu</param>
        /// <param name="count"></param>
        private void AddItemsToGroup(IRibbonGroup iRibbonGroup, DxRibbonGroup dxGroup, DxRibbonCreateContentMode currentMode, ref int count)
        {
            var iRibbonItems = DataRibbonItem.SortRibbonItems(iRibbonGroup.Items);
            foreach (var ribbonItem in iRibbonItems)
            {
                var iRibbonItem = ribbonItem;
                _AddBarItem(ref iRibbonItem, dxGroup, currentMode, ref count);
                iRibbonItem.ParentGroup = iRibbonGroup;
            }
            dxGroup.RefreshGroupVisibility();
        }
        /// <summary>
        /// Metoda přidá daný prvek do dané grupy
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="currentMode">Režim tvorby obsahu</param>
        /// <param name="count"></param>
        private void _AddBarItem(ref IRibbonItem iRibbonItem, DxRibbonGroup dxGroup, DxRibbonCreateContentMode currentMode, ref int count)
        {
            if (iRibbonItem == null || dxGroup == null) return;
            if (currentMode.HasFlag(DxRibbonCreateContentMode.CreateOnlyQATItems) && !ContainsQAT(iRibbonItem)) return;          // V režimu createOnlyQATItems přidáváme jen prvky QAT, a ten v daném prvku není žádný

            GetItem(ref iRibbonItem, dxGroup, 0, currentMode, ref count);          // Najde / Vytvoří / Naplní prvek
        }
        #endregion
        #region Refresh obsahu Ribbonu
        /// <summary>
        /// Provede refresh dodaných objektů. Provede se jedním chodem. Dodané objekty rozpozná a provede odpovídající refreshe.
        /// </summary>
        /// <param name="iRibbonObjects"></param>
        public void RefreshObjects(IEnumerable<IRibbonObject> iRibbonObjects)
        {
            if (iRibbonObjects == null) return;

            this.ParentOwner.RunInGui(() =>
            {
                _RefreshObjectsOnly(iRibbonObjects);
                if (NeedOpenMenu)
                    DoOpenMenu();
            });
        }
        /// <summary>
        /// Zajistí refresh dodaných objektů. Dost možná nezajistí otevření menu, to musí zkontrolovat volající.
        /// Tato metoda má být spuštěna v GUI threadu. Sama si invokování neřeší.
        /// </summary>
        /// <param name="iRibbonObjects"></param>
        private void _RefreshObjectsOnly(IEnumerable<IRibbonObject> iRibbonObjects)
        {
            // Vstupní data roztřídím na: { Page | Group | Items }, a pak půjdu standardními postupy:
            List<IRibbonPage> iRibbonPages = new List<IRibbonPage>();
            List<IRibbonGroup> iRibbonGroups = new List<IRibbonGroup>();
            List<IRibbonItem> iRibbonItems = new List<IRibbonItem>();
            foreach (var iRibbonObject in iRibbonObjects)
            {
                if (iRibbonObject != null)
                {
                    if (iRibbonObject is IRibbonPage iRibbonPage) { if (!iRibbonPages.Contains(iRibbonPage)) iRibbonPages.Add(iRibbonPage); }
                    else if (iRibbonObject is IRibbonGroup iRibbonGroup) { if (!iRibbonGroups.Contains(iRibbonGroup)) iRibbonGroups.Add(iRibbonGroup); }
                    else if (iRibbonObject is IRibbonItem iRibbonItem) { if (!iRibbonItems.Contains(iRibbonItem)) iRibbonItems.Add(iRibbonItem); }
                }
            }
            bool hasPages = (iRibbonPages.Count > 0);
            bool hasGroups = (iRibbonGroups.Count > 0);
            bool hasItems = (iRibbonItems.Count > 0);
            if (!hasPages && !hasGroups)
            {   // Když nemám stránky ani grupy, tak řeším jen jednotlivé Itemy:
                if (!hasItems) return;                               // Itemy taky nejsou
                if (!AnalyseRefreshItems(iRibbonItems, out var reFillItems, out var reCreateItems)) return;         // Ani dodané Itemy nepotřebují žádnou změnu

                if (reFillItems != null)
                {
                    this._RefreshItems(reFillItems);
                }
                if (reCreateItems is null) return;                   // Měl jsem jen prvky ReFill, nic dalšího řešit nebudu

                // Do dalšího Refreshe pokračují jen ReCreate items:
                iRibbonItems = reCreateItems;
                hasItems = true;
            }

            // Máme data, půjdeme dělat něco viditelného:
            bool refreshDirect = false; // !hasPages;
            bool needInvoke = this.ParentOwner.InvokeRequired;
            if (refreshDirect && !needInvoke)
            {   // Přímý refresh (bez změn stránek)
                _RefreshObjects(iRibbonPages, iRibbonGroups, iRibbonItems, true);
            }
            else
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    _RefreshObjects(iRibbonPages, iRibbonGroups, iRibbonItems, true);
                }, true);
            }
        }
        /// <summary>
        /// Znovu naplní stránky this Ribbonu specifikované v dodaných datech.
        /// Nejprve zahodí obsah stránek, které jsou uvedeny v dodaných datech.
        /// Pak do Ribbonu vygeneruje nový obsah do specifikovaných stránek.
        /// Pokud pro některou stránku nebudou dodána žádná platná data, stránku zruší
        /// (k tomu se použije typ prvku <see cref="IRibbonItem.ItemType"/> == <see cref="RibbonItemType.None"/>, kde daný záznam pouze definuje Page ke zrušení).
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// </summary>
        /// <param name="iRibbonPages"></param>
        public void RefreshPages(IEnumerable<IRibbonPage> iRibbonPages)
        {
            if (iRibbonPages == null) return;
            if (this.IsDisposed) return;

            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    _RefreshPages(iRibbonPages, true);
                }, true);
            });
        }
        /// <summary>
        /// Zajistí provedení refreshe dodané grupy (podle <see cref="IRibbonGroup.GroupId"/> v this Ribbonu.
        /// Pokud tato grupa v Ribbonu není přítomna: pak pokud v dodané grupě je přítomna stránka <see cref="IRibbonGroup.ParentPage"/> pak se pokusí vyhledat odpovídající stránku v this Ribbonu a grupu do ní přidat.
        /// </summary>
        /// <param name="iRibbonGroups"></param>
        public void RefreshGroups(IEnumerable<IRibbonGroup> iRibbonGroups)
        {
            if (iRibbonGroups == null)
                throw new ArgumentException($"DxRibbonControl.RefreshGroups() error: groups for refresh is null.");
            if (!iRibbonGroups.Any()) return;

            _UnMergeModifyMergeCurrentRibbon(() => _RefreshGroups(iRibbonGroups, true), true);
        }
        /// <summary>
        /// Zajistí provedení refreshe dodané grupy (podle <see cref="IRibbonGroup.GroupId"/> v this Ribbonu.
        /// Pokud tato grupa v Ribbonu není přítomna: pak pokud v dodané grupě je přítomna stránka <see cref="IRibbonGroup.ParentPage"/> pak se pokusí vyhledat odpovídající stránku v this Ribbonu a grupu do ní přidat.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        public void RefreshGroup(IRibbonGroup iRibbonGroup)
        {
            if (iRibbonGroup == null)
                throw new ArgumentException($"DxRibbonControl.RefreshGroup() error: group for refresh is null.");

            // Najdeme fyzická data pro provedení refreshe dané grupy:
            if (!_TryGetDataForRefreshGroup(iRibbonGroup, out var dxGroup, out var dxPage)) return;

            // Podklady jsou OK, můžeme se pustit do větší akce (Unmerge - Akce - Merge):
            _UnMergeModifyMergeCurrentRibbon(() => _RefreshGroup(iRibbonGroup, dxGroup, dxPage, true), true);
        }
        /// <summary>
        /// Metoda zajistí, že bude nalezen každý daný prvek (pokud neexistuje, tak ale nebude vytvořen!);
        /// a do daného prvku se znovunaplní jeho vlastnosti (=Refresh) + jeho subpoložky.
        /// Touto cestou nelze změnit typ prvku!
        /// Lze refreshovat text, tooltip, image, checked, subitems.
        /// Lze vyžádat otevření submenu, pokud to submenu je.
        /// </summary>
        /// <param name="iRibbonItems"></param>
        public void RefreshItems(IEnumerable<IRibbonItem> iRibbonItems)
        {
            if (iRibbonItems == null) return;
            if (!AnalyseRefreshItems(iRibbonItems, out var reFillItems, out var reCreateItems)) return;             // Zkratka

            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    _RefreshItems(iRibbonItems);
                }, true);
                DoOpenMenu();
            });
        }
        /// <summary>
        /// Metoda zajistí, že bude nalezen daný prvek (pokud neexistuje, tak ale nebude vytvořen!);
        /// a do daného prvku se znovunaplní jeho vlastnosti (=Refresh) + jeho subpoložky.
        /// Touto cestou nelze změnit typ prvku!
        /// Lze refreshovat text, tooltip, image, checked, subitems.
        /// Lze vyžádat otevření submenu, pokud to submenu je.
        /// </summary>
        /// <param name="iRibbonItem">Definice prvku</param>
        /// <param name="force">Provést refresh povinně, i když se prvek zdá být nezměněn</param>
        /// <param name="refreshInSite">Pro refresh prvku neprováděj Unmerge - Modify - Merge. Změna je malá a režie by byla zbytečná.</param>
        public void RefreshItem(IRibbonItem iRibbonItem, bool force = false, bool refreshInSite = false)
        {
            if (iRibbonItem == null) return;
            if (!force && GetRefreshModeForItem(iRibbonItem) == ItemRefreshType.None) return;               // Zkratka

            if (!refreshInSite)
            {   // UmMerge - Modify - Merge :
                this.ParentOwner.RunInGui(() =>
                {
                    _UnMergeModifyMergeCurrentRibbon(() =>
                    {
                        RefreshCurrentItem(iRibbonItem);
                    }, true);
                    DoOpenMenu();
                });
            }
            else
            {   // In Site:
                this.ParentOwner.RunInGui(() =>
                {
                    RefreshCurrentItem(iRibbonItem);
                    DoOpenMenu();
                });
            }
        }
        /// <summary>
        /// Metoda zajistí, že vizuální prvek (BarItem nebo obdobná instance) v jeho Ribbonu bude refreshován.
        /// Pokud dosud vizuální prvek neexistuje, tak ale nebude vytvořen!
        /// Do daného prvku se znovunaplní jeho vlastnosti (=Refresh) + jeho subpoložky.
        /// Touto cestou nelze změnit typ prvku!
        /// Lze refreshovat text, tooltip, image, checked, subitems.
        /// </summary>
        /// <param name="iRibbonItem">Definice prvku</param>
        /// <param name="force">Provést refresh povinně, i když se prvek zdá být nezměněn</param>
        /// <param name="refreshInSite">Pro refresh prvku neprováděj Unmerge - Modify - Merge. Změna je malá a režie by byla zbytečná.</param>
        public static void RefreshIRibbonItem(IRibbonItem iRibbonItem, bool force = false, bool refreshInSite = false)
        {
            if (iRibbonItem == null) return;
            var barItem = iRibbonItem.RibbonItem;
            if (barItem is null) return;

            // Musíme najít instanci toho Ribbonu, který je majitelem daného prvku (který jej vytvořil), a nikoli tu, která jej právě nyní zobrazuje (případ mergovaného Ribbonu):
            var dxRibbon = (barItem.Manager as RibbonBarManager)?.Ribbon as DxRibbonControl;
            dxRibbon?.RefreshItem(iRibbonItem, force, refreshInSite);
        }
        /// <summary>
        /// Provede refresh dodaných objektů. Provede se jedním chodem.
        /// Ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonPages"></param>
        /// <param name="iRibbonGroups"></param>
        /// <param name="iRibbonItems"></param>
        /// <param name="reloadQatToRibbon">Na závěr zavolat <see cref="AddQATUserListToRibbon(Dictionary{string, Tuple{BarItem, IRibbonItem}})"/>?</param>
        private void _RefreshObjects(List<IRibbonPage> iRibbonPages, List<IRibbonGroup> iRibbonGroups, List<IRibbonItem> iRibbonItems, bool reloadQatToRibbon)
        {
            if (iRibbonPages.Count > 0) _RefreshPages(iRibbonPages, false);
            if (iRibbonGroups.Count > 0) _RefreshGroups(iRibbonGroups, false);
            if (iRibbonItems.Count > 0) _RefreshItems(iRibbonItems);
            if (reloadQatToRibbon) AddQATUserListToRibbon(null);
        }
        /// <summary>
        /// Provede refresh dodaných stránek. Provede se jedním chodem.
        /// Ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonPages"></param>
        /// <param name="reloadQatToRibbon">Na závěr zavolat <see cref="AddQATUserListToRibbon(Dictionary{string, Tuple{BarItem, IRibbonItem}})"/>?</param>
        private void _RefreshPages(IEnumerable<IRibbonPage> iRibbonPages, bool reloadQatToRibbon)
        {
            if (this.IsDisposed) return;
            
            var reFillPages = _PrepareReFill(iRibbonPages, true);
            DxRibbonCreateContentMode createMode = DxRibbonCreateContentMode.CreateGroupsContent | DxRibbonCreateContentMode.CreateAllSubItems | DxRibbonCreateContentMode.RunningOnDemandFill;
            _AddPages(iRibbonPages, false, createMode, "OnDemand");
            _RemoveVoidContainers(reFillPages);
            CheckLazyContentCurrentPage(true);
            if (reloadQatToRibbon) AddQATUserListToRibbon(null);
        }
        /// <summary>
        /// Refresh sady skupin, data fyzické grupy jsou dodána, ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonGroups"></param>
        /// <param name="reloadQatToRibbon">Na závěr zavolat <see cref="AddQATUserListToRibbon(Dictionary{string, Tuple{BarItem, IRibbonItem}})"/>?</param>
        private void _RefreshGroups(IEnumerable<IRibbonGroup> iRibbonGroups, bool reloadQatToRibbon)
        {
            var startTime = DxComponent.LogTimeCurrent;
            int groupCount = 0;
            int itemCount = 0;
            bool hasData = false;
            foreach (var iRibbonGroup in iRibbonGroups)
            {
                if (iRibbonGroup is null) continue;

                // Najdeme fyzická data pro provedení refreshe aktuální grupy, a pokud je to OK, pak provedeme Refresh:
                if (_TryGetDataForRefreshGroup(iRibbonGroup, out var dxGroup, out var dxPage))
                {
                    _RefreshGroup(iRibbonGroup, dxGroup, dxPage, false);
                    hasData = true;
                }
            }
            if (hasData && reloadQatToRibbon) AddQATUserListToRibbon(null);
            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === Ribbon {DebugName}: Refresh groups '{groupCount}', Items count: {itemCount}; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Provede refresh dodaných prvků. Provede se jedním chodem.
        /// Ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonItems"></param>
        private void _RefreshItems(IEnumerable<IRibbonItem> iRibbonItems)
        {
            foreach (var iRibbonItem in iRibbonItems)
            {
                RefreshCurrentItem(iRibbonItem);
            }
        }
        /// <summary>
        /// Metoda zkusí najít fyzickou grupu pro refresh, a pokud ji nenajde, pak zkusí najít stránku pro vložení nové grupy (pokud je to zapotřebí).
        /// Nalezené prvky vkládá do out parametrů.
        /// Vrací true = je třeba provést nějaký Refresh (ten zajistí metoda <see cref="_RefreshGroup(IRibbonGroup, DxRibbonGroup, DxRibbonPage, bool)"/>),
        /// ať už je to Create / ReFill / Remove.
        /// Nebo vrátí false = grupa neexistuje, ale protože je požadováno její Remove, pak není potřeba provádět nic.
        /// <para/>
        /// Tato metoda nezakládá novou grupu ani stránku, pouze je hledá a vyhodnocuje.
        /// <para/>
        /// Metoda může vyhodit chybu.
        /// </summary>
        /// <param name="iRibbonGroup">Data popisující grupu</param>
        /// <param name="dxGroup">Out nalezená grupa nebo null (pokud se bude vytvářet nová do nalezené stránky)</param>
        /// <param name="dxPage">Out nalezená stránka, pokud grupa neexistuje ale její stránka je deklarována</param>
        /// <returns></returns>
        private bool _TryGetDataForRefreshGroup(IRibbonGroup iRibbonGroup, out DxRibbonGroup dxGroup, out DxRibbonPage dxPage)
        {
            dxPage = null;
            bool hasGroup = TryGetGroup(iRibbonGroup.GroupId, false, out dxGroup);
            if (!hasGroup)
            {
                if (iRibbonGroup.ChangeMode == ContentChangeMode.Remove) return false;             // Odebrat stránku, která neexistuje: to není problém :-)
                if (iRibbonGroup.ParentPage == null)
                    throw new ArgumentException($"DxRibbonControl.RefreshGroup() error: group '{iRibbonGroup.GroupId}' is not found, and ParentPage is not specified.");
                bool hasPage = TryGetPage(iRibbonGroup.ParentPage.PageId, out dxPage);
                if (!hasPage)
                    throw new ArgumentException($"DxRibbonControl.RefreshGroup() error: group '{iRibbonGroup.GroupId}' is not found, and ParentPage '{iRibbonGroup.ParentPage.PageId}' does not exists.");
            }
            return true;
        }
        /// <summary>
        /// Zajistí provedení refreshe dodané grupy (podle <see cref="IRibbonGroup.GroupId"/> v this Ribbonu, data fyzické grupy jsou dodána, ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxGroup"></param>
        /// <param name="dxPage">Stránka. Je dodána jen tehdy, když grupa <paramref name="dxGroup"/> dosud v Ribbonu neexistuje, v tom případě je ověřeno, že tato stránka existuje.</param>
        /// <param name="reloadQatToRibbon">Na závěr zavolat <see cref="AddQATUserListToRibbon(Dictionary{string, Tuple{BarItem, IRibbonItem}})"/>?</param>
        private void _RefreshGroup(IRibbonGroup iRibbonGroup, DxRibbonGroup dxGroup, DxRibbonPage dxPage, bool reloadQatToRibbon)
        {
            var startTime = DxComponent.LogTimeCurrent;

            DxRibbonCreateContentMode createMode = DxRibbonCreateContentMode.CreateGroupsContent | DxRibbonCreateContentMode.CreateAllSubItems | DxRibbonCreateContentMode.RunningOnDemandFill;
            int count = 0;
            ReloadGroup(iRibbonGroup, dxGroup, dxPage, createMode, ref count);

            if (reloadQatToRibbon) AddQATUserListToRibbon(null);

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === Ribbon {DebugName}: Refresh group '{iRibbonGroup.GroupId}', Items count: {count}; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Odebere z evidence dodané grupy
        /// </summary>
        /// <param name="groupsToDelete"></param>
        private void RemoveGroups(IEnumerable<DxRibbonGroup> groupsToDelete)
        {
            if (groupsToDelete != null)
                groupsToDelete.ForEachExec(g => this._Groups.Remove(g));
        }
        #endregion
        #region NeedRefreshItem : detekuje potřebu provést Refresh prvku
        /// <summary>
        /// Vrátí true, pokud dodaná kolekce obsahuje přinejmenším jeden prvek, který reálně potřebuje provést Refresh.
        /// To je tehdy, když v aktuálním Ribbonu prvek není a má být, nebo je a nemá být, nebo je a obsahuje jiná data.
        /// Metoda současně vytvoří dvě pole, 
        /// kde první <paramref name="reFillItems"/> obsahuje prvky, které v Ribbonu existují a stačí je jen znovunaplnit a není třeba provádět Unmerge ribbonu.
        /// Druhé out pole obsahuje prvky, které v Ribbonu neexistují nebo jsou v jiné grupě, pak je skutečně třeba provész Unmerge ribbonu a velký Refresh.
        /// Out pole mohou být null, když do nich nebylo co uložit.
        /// </summary>
        /// <param name="iRibbonItems"></param>
        /// <param name="reFillItems"></param>
        /// <param name="reCreateItems"></param>
        /// <returns></returns>
        private bool AnalyseRefreshItems(IEnumerable<IRibbonItem> iRibbonItems, out List<IRibbonItem> reFillItems, out List<IRibbonItem> reCreateItems)
        {
            reFillItems = null;
            reCreateItems = null;
            bool needRefresh = false;
            if (iRibbonItems != null)
            {
                foreach (var iRibbonItem in iRibbonItems)
                {
                    var mode = GetRefreshModeForItem(iRibbonItem);
                    switch (mode)
                    {
                        case ItemRefreshType.ReCreate:
                        case ItemRefreshType.Remove:
                            if (reCreateItems is null) reCreateItems = new List<IRibbonItem>();
                            reCreateItems.Add(iRibbonItem);
                            needRefresh = true;
                            break;
                        case ItemRefreshType.ReFill:
                            if (reFillItems is null) reFillItems = new List<IRibbonItem>();
                            reFillItems.Add(iRibbonItem);
                            needRefresh = true;
                            break;
                    }
                }
            }
            return needRefresh;
        }
        /// <summary>
        /// Vrátí režim refreshe pro daný prvek: pokud dodaný prvek reálně potřebuje provést Refresh. Tj. obsahuje jiná data, než jaká máme v GUI kolekci.
        /// To je tehdy, 
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private ItemRefreshType GetRefreshModeForItem(IRibbonItem iRibbonItem)
        {
            if (iRibbonItem == null) return ItemRefreshType.None;
            var changeMode = iRibbonItem.ChangeMode;
            bool needExists = (changeMode == ContentChangeMode.Add || changeMode == ContentChangeMode.ReFill);
            if (TryGetIRibbonData(iRibbonItem.ItemId, out var iCurrentItem, out bool isValid, true))         // Pokud najdeme prvek Menu, a to je ve stavu Reloading (jeho BarItemTagInfo.ValidMenu je ContentValidityType.ReloadInProgress), pak to bereme jako validní prvek
            {   // Prvek existuje: Refresh je třeba, pokud prvek nemá existovat, anebo když aktuálně obsahuje jiná data než je nyní požadováno:
                if (!needExists) return ItemRefreshType.Remove;
                if (!isValid) return ItemRefreshType.ReCreate;                                               // Nevalidní prvek je třeba refreshovat
                if (Object.ReferenceEquals(iRibbonItem, iCurrentItem)) return ItemRefreshType.ReFill;        // Pokud dodaná instance (iRibbonItem) a zdejší instance (iCurrentItem) je totožná, pak Refresh je nutný - neboť nedokážu rozpoznat, jestli došlo ke změně. To dokážu jen pro dvě odlišné instance!
                if (needReload(iCurrentItem)) return ItemRefreshType.ReFill;                                 // Pokud naše dosavadní instance je OnDemandLoad, pak musí dostat ReFill !!!  Kvůli refreshi obsahu a předání informace, že prvek dostal data a může být otevřen...
                if (DataRibbonItem.HasEqualContent(iRibbonItem, iCurrentItem)) return ItemRefreshType.None;  // Shodný obsah => netřeba refresh.
                return ItemRefreshType.ReFill;                                 // Neshodný obsah => ReFill tehdy, když dva prvky (nově požadovaný a stávající) NEJSOU shodné!   Takhle je tu podmínka proto, abych na Equals mohl dát breakpoint...
            }
            else
            {   // Prvek neexistuje: Refresh je třeba, pokud se prvek má vytvořit nebo upravit:
                return ItemRefreshType.ReCreate;
            }

            // Vrátí true, pokud dodaný prvek potřebuje Reload položek SubItems. Takový prvek musí mít vyvolánu metodu Refresh, aby validně zobrazil nově dodané anebo i původní menu...
            bool needReload(IRibbonItem ribbonItem)
            {
                return (ribbonItem != null && (ribbonItem.SubItemsIsOnDemand || ribbonItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce || ribbonItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime));
            }
        }
        /// <summary>
        /// Jak zpracovat refresh prvku
        /// </summary>
        private enum ItemRefreshType { None, ReFill, ReCreate, Remove }
        #endregion
        #region LazyLoad page content : OnSelectedPageChanged => CheckLazyContent; OnDemand loading Page
        /// <summary>
        /// Požadavek na používání opožděné tvorby obsahu stránek Ribbonu.
        /// Podle nastavení budou jednotlivé prvky na stránce Ribbonu fyzicky vygenerovány až tehdy, až bude stránka vybrána k zobrazení.
        /// Změna hodnoty se projeví až při následujícím přidávání prvků do Ribbonu. 
        /// Pokud tedy byla hodnota <see cref="LazyContentMode.DefaultLazy"/> (=výchozí stav), pak se přidá 600 prvků, 
        /// a teprve pak se nastaví <see cref="LazyContentMode.CreateAllItems"/> = true, je to fakt pozdě.
        /// </summary>
        public LazyContentMode UseLazyContentCreate { get; set; }
        /// <summary>
        /// Prověří, zda aktuální stránka <see cref="DevExpress.XtraBars.Ribbon.RibbonControl.SelectedPage"/> 
        /// má na sobě nějaké HasLazyContentItems, a pokud ano, tak je fyzicky vytvoří
        /// </summary>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        protected virtual void CheckLazyContentCurrentPage(bool isCalledFromReFill)
        {
            this.CheckLazyContent(this.SelectedPage, isCalledFromReFill);
        }
        /// <summary>
        /// Prověří, zda daná stránka má nějaké HasLazyContentItems, a pokud ano tak je fyzicky vytvoří
        /// </summary>
        /// <param name="page"></param>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        protected virtual void CheckLazyContent(DevExpress.XtraBars.Ribbon.RibbonPage page, bool isCalledFromReFill)
        {
            if (page == null || !this.CheckLazyContentEnabled || _CurrentMergeState != MergeState.None) return;

            if (_ClearingNow)
            {   // Pokud nyní probíhá Clear, tak po Pages.Clear proběhne Select na první CategoryPage nebo MergedPage, která je dostupná.
                // Ale pokud to bude pozice Default nebo PageCategories (=naše vlastní), tak ty se nyní odstraňují a nemá cenu pro ně řešit SelectedPage a LazyLoad,
                //  na to už je zbytečně pozdě!

                // Přípravu obsahu stránky tedy provedu pouze pro stránky typu Merged:
                if (!_IsPageOnPosition(page, PagePosition.AllMerged)) return;
            }

            // Něco k DevExpress a k Ribbonům si přečti v XML komentáři k metodě DxRibbonPageLazyGroupInfo.TryGetLazyDxPages!

            if (!DxRibbonLazyLoadInfo.TryGetLazyDxPages(page, isCalledFromReFill, out var lazyDxPages)) return;

            // Pro danou stránku jsme našli jednu nebo i více definic LazyGroups.
            // Na základní stránce (stránka definovaná přímo v Ribbonu) je vždy jen jedna LazyGroup.
            // Ale pokud budu mergovat postupně více Ribbonů nahoru: (1) => (2); (2) => (3),
            //  pak Ribbon 1 může mít stránku "PageDoc" a obsahuje grupu pro LazyInfo s BarItem Buttonem, který v Tagu nese konkrétní instanci DxRibbonLazyLoadInfo.
            //  Když se Ribbon 1 přimerguje do stejnojmenné stránky "PageDoc" Ribbonu 2, kde se nachází jeho grupa LazyInfo, tak se v dané grupě sejdou
            //  už dva BarItem Buttony, každý nese v Tagu svou instanci DxRibbonLazyLoadInfo. A tak lze mergovat DxRibbonLazyLoadInfo víceúrovňově.
            // Pak tedy pro jednu stránku můžeme získat sadu instancí DxRibbonLazyLoadInfo, které definují LazyLoad nebo OnDemand načítání obsahu.
            foreach (var lazyDxPage in lazyDxPages)
                lazyDxPage.PrepareRealLazyItems(isCalledFromReFill);           // Tato metoda převolá zdejší metodu : void PrepareRealLazyItems(DxRibbonLazyLoadInfo lazyGroup, bool isCalledFromReFill)
        }
        /// <summary>
        /// Vrátí true, pokud stránka s daným <paramref name="pageId"/> je naší vlastní stránkou (je v <see cref="AllOwnPages"/>).
        /// Vrátí false pokud neexistuje nebo je mergovaná.
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        protected bool IsOwnPageId(string pageId)
        {
            if (String.IsNullOrEmpty(pageId)) return false;
            return (this.AllOwnPages.Any(p => p.Name == pageId));
        }
        /// <summary>
        /// Pokud je true (běžný aktivní stav), pak se po aktivaci stránky provádí kontroly LazyLoad obsahu.
        /// Nastavením na false se tyto kontroly deaktivují. 
        /// Používá se při hromadnám Unmerge a zpět Merge, kdy dochází ke změnám SelectedPage v každém kroku, 
        /// ale kontrola LazyContent by v této situaci byla zcela zbytečná (protože proces Unmerge - Modify - Merge pravděpodobně přinese nová data,
        /// a navíc proces Merge zase aktivuje původní stránku Ribbonu, takže není nutné materializovat stránku jinou).
        /// </summary>
        protected bool CheckLazyContentEnabled { get; set; }
        /// <summary>
        /// Nastartuje požadavek na OnDemand Load obsahu stránky this Ribbonu
        /// </summary>
        /// <param name="iRibbonPage"></param>
        protected void RunPageOnDemandLoad(IRibbonPage iRibbonPage)
        {
            var args = new TEventArgs<IRibbonPage>(iRibbonPage);
            this.OnPageOnDemandLoad(args);
            PageOnDemandLoad?.Invoke(this, args);
        }
        /// <summary>
        /// Provede se při OnDemand načítání obsahu stránky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageOnDemandLoad(TEventArgs<IRibbonPage> args) { }
        /// <summary>
        /// Vyvolá se při OnDemand načítání obsahu stránky.
        /// Načítání typicky probíhá v threadu na pozadí.
        /// Po nějakém čase - když aplikace získá data, pak zavolá zdejší metodu <see cref="DxRibbonControl.RefreshPages(IEnumerable{IRibbonPage})"/>;
        /// tato metoda zajistí zobrazení nově dodaných dat v odpovídajících stránkách Ribbonu.
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonPage>> PageOnDemandLoad;
        /// <summary>
        /// Nastartuje požadavek na OnDemand Load obsahu prvku this Ribbonu
        /// </summary>
        /// <param name="iRibbonItem"></param>
        protected void RunItemOnDemandLoad(IRibbonItem iRibbonItem)
        {
            var args = new TEventArgs<IRibbonItem>(iRibbonItem);
            this.OnItemOnDemandLoad(args);
            ItemOnDemandLoad?.Invoke(this, args);
        }
        /// <summary>
        /// Provede se při OnDemand načítání obsahu stránky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemOnDemandLoad(TEventArgs<IRibbonItem> args) { }
        /// <summary>
        /// Vyvolá se při OnDemand načítání obsahu submenu v prvku.
        /// Načítání typicky probíhá v threadu na pozadí.
        /// Po nějakém čase - když aplikace získá data, pak zavolá zdejší metodu <see cref="DxRibbonControl.RefreshItem(IRibbonItem, bool, bool)"/>;
        /// tato metoda zajistí zobrazení nově dodaných dat v daném prvku.
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonItem>> ItemOnDemandLoad;
        /// <summary>
        /// Režim vytváření prvků "ihned" / "lazy" (až budou potřeba)
        /// </summary>
        public enum LazyContentMode
        {
            /// <summary>
            /// Defaultní = lazy
            /// </summary>
            DefaultLazy,
            /// <summary>
            /// Vytvořit všechno, pokud toho není mnoho.
            /// Tedy vytvoří i prvky pro neaktivní stránky, pokud prvků základní viditelné úrovně je méně než 50.
            /// Vytvoří i SubItems, pokud jich na stránce celkem je méně než 50.
            /// </summary>
            Auto,
            /// <summary>
            /// Vytvořit jen viditelné prvky = aktivní stránka a její grupy a jejich BarItems první úrovně, nikoli SubItems
            /// </summary>
            CreateVisibleBarsOnly,
            /// <summary>
            /// Vytvořit zcela všechny prvky = všechny stránky, grupy, prvky, i včetně subitems
            /// </summary>
            CreateAllItems
        }
        #endregion
        #region LazyLoadOnIdle : dovolujeme provést opožděné plnění stránek (typu LazyLoad: Static) v režimu Application.OnIdle = "když je vhodná chvíle"
        /// <summary>
        /// Aplikace má volný čas, Ribbon by si mohl vygenerovat LazyLoad Static pages, pokud takové má.
        /// </summary>
        private void _ApplicationIdle()
        {
            if (!_ActiveLazyLoadOnIdle) return;

            if (_ActiveLazyLoadPagesOnIdle)
                // Při první události 'Application.OnIdle' si vygeneruji obsah všech stránek Ribbonu, pro které máme statická data:
                //  ale nebudu volat OnDemand load jednotlivých položek serveru - to si necháme na druhou událost 'Application.OnIdle':
                //  (metoda na svém konci shodí příznak _ActiveLazyLoadPagesOnIdle na false)
                _PrepareLazyLoadStaticPages();
            else if (_ActiveLazyLoadItemsOnIdle)
                // Při první události 'Application.OnIdle' si vyhledám prvky Ribbonu, které dosud nemají svůj obsah,
                //  a buď fyzicky vytvořím obsah prvku (typicky: menu) anebo o něj požádáme server:
                //  (metoda na svém konci shodí příznak _ActiveLazyLoadItemsOnIdle na false)
                _PrepareLazyLoadOnDemandItems();
        }
        /// <summary>
        /// Hodnota true říká, že this Ribon má nějaké Pages nebo Items ve stavu LazyLoad se statickým obsahem.
        /// Pak v době, kdy systém má volný čas (když je volána metoda <see cref="IListenerApplicationIdle.ApplicationIdle()"/>) 
        /// si Ribbon fyzicky vygeneruje reálný obsah daných Pages anebo si vyžádá donačtení dat ze serveru a následný Refresh.
        /// </summary>
        private bool _ActiveLazyLoadOnIdle { get { return (_ActiveLazyLoadPagesOnIdle | _ActiveLazyLoadItemsOnIdle); } }
        /// <summary>
        /// Tato metoda je volána v situaci, kdy GUI thread má nějaký volný čas (ApplicationIdle) 
        /// a this Ribbon má nastavený příznak <see cref="_ActiveLazyLoadPagesOnIdle"/> == true;
        /// tedy očekáváme, že existuje nějaká vlastní stránka Ribbonu, která má Static data v režimu LazyLoad, 
        /// a bylo by vhodné z nich vytvořit fyzické controly.
        /// To je úkolem této metody.
        /// </summary>
        private void _PrepareLazyLoadStaticPages()
        {
            var lazyPages = this.GetPages(PagePosition.AllOwn).OfType<DxRibbonPage>().Where(p => p.HasActiveStaticLazyContent).ToList();
            if (lazyPages.Count > 0)
            {
                var startTime = DxComponent.LogTimeCurrent;

                int pageCount = lazyPages.Count;
                int itemCount = 0;
                _UnMergeModifyMergeCurrentRibbon(() =>
                {   // Provede Unmerge this Ribbonu, pak provede následující akci, a poté zase zpětně Merge do původního stavu, se zachováním SelectedPage:
                    int icnt = 0;
                    foreach (var lazyPage in lazyPages)
                        _PrepareLazyLoadStaticPage(lazyPage, ref icnt);
                    itemCount = icnt;
                    AddQATUserListToRibbon(null);
                }, true);

                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $" === Ribbon {DebugName}: CreateLazyStaticPages; Create: {pageCount} Pages; {itemCount} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
            }
            _ActiveLazyLoadPagesOnIdle = false;
        }
        /// <summary>
        /// Zajistí vygenerování obsahu dané stránky ribbonu v režimu LazyLoad
        /// </summary>
        /// <param name="lazyPage"></param>
        /// <param name="itemCount"></param>
        private void _PrepareLazyLoadStaticPage(DxRibbonPage lazyPage, ref int itemCount)
        {
            IRibbonPage iRibbonPage = lazyPage.PageData;
            DxRibbonCreateContentMode createMode = DxRibbonCreateContentMode.CreateGroupsContent | DxRibbonCreateContentMode.CreateAllSubItems | DxRibbonCreateContentMode.RunningOnIdleFill;
            _AddPage(iRibbonPage, createMode, ref itemCount);                        // Existující Page se ponechá, obsah se nahradí, naplní se reálné prvky, včetně staticky deklarovaných subpoložek
        }
        /// <summary>
        /// Hodnota true říká, že this Ribon má nějaké Pages ve stavu LazyLoad se statickým obsahem.
        /// Pak v době, kdy systém má volný čas (když je volána metoda <see cref="IListenerApplicationIdle.ApplicationIdle()"/>) 
        /// si Ribbon fyzicky vygeneruje reálný obsah daných Pages.
        /// </summary>
        private bool _ActiveLazyLoadPagesOnIdle;
        /// <summary>
        /// Tato metoda je volána v situaci, kdy GUI thread má nějaký volný čas (ApplicationIdle) 
        /// a this Ribbon má nastavený příznak <see cref="_ActiveLazyLoadItemsOnIdle"/> == true;
        /// tedy evidujeme nějaký prvek typu Grupa nebo Menu, které se má donačítat OnDemand ze serveru.
        /// Tato metoda vyvolá akce, které zajistí donačtení dat na serveru a jejich odeslání na klienta, do události Menu.Refresh,
        /// a následně se tedy vygeneruje obsah těchto prvků do Ribbonu.
        /// Důvodem je to, aby tyto OnDemand prvky byly fyzicky přítomné v Ribbonu a bylo je možno najít pomocí akce Search v záhlaví Ribbonu.
        /// </summary>
        private void _PrepareLazyLoadOnDemandItems()
        {
            // DAJ + STR : zrušeno, výkonová katastrofa - v závislosti na velikosti dat...

            //var dxPages = this.GetPages(PagePosition.AllOwn).OfType<DxRibbonPage>().ToList();
            //foreach (var dxPage in dxPages)
            //    _PrepareLazyLoadOnDemandItemsOnPage(dxPage);
            
            _ActiveLazyLoadItemsOnIdle = false;
        }
        /// <summary>
        /// Zajistí vytvoření obsahu fyzických prvků v dané stránce Ribbonu v režimu LazyLoad v čase ApplicationIdle.
        /// Stránka jako taková by již měla být vygenerována, protože zdejší metoda běží až ve druhé vlně,
        ///  když v první vlně (v čase ApplicationIdle) byly vygenerovány všechny stránky a jejich obsah (viz metoda _PrepareLazyLoadStaticPages()).
        /// Nyní tedy v každé stránce najdeme její prvky, které ještě nemají svůj obsah (SubItems), a zajistíme jeho tvorbu, podle typu prvku (Menu, SplitButton, atd).
        /// </summary>
        /// <param name="dxPage"></param>
        private void _PrepareLazyLoadOnDemandItemsOnPage(DxRibbonPage dxPage)
        {
            var allPageItems = dxPage.GroupItems.SelectMany(gi => gi.Item2).ToList();
            foreach (var barItem in allPageItems)
            {
                if (barItem is DevExpress.XtraBars.BarSubItem barMenu && barItem.Tag is BarItemTagInfo menuItemInfo && menuItemInfo.LazyInfo != null)
                    _BarMenu_CreateItems(barMenu, menuItemInfo);
                else if (barItem is DevExpress.XtraBars.BarButtonItem splitButton && splitButton.DropDownControl is DevExpress.XtraBars.PopupMenu dxPopup && dxPopup.Tag is BarItemTagInfo popupItemInfo && popupItemInfo.LazyInfo != null)
                    _PopupMenu_CreateLazyItems(dxPopup, popupItemInfo);
            }
        }
        /// <summary>
        /// Hodnota true říká, že this Ribon má nějaké prvky (grupy, menu, atd) Pages ve stavu LazyLoad s obsahem typu LoadOnce.
        /// Pak v době, kdy systém má volný čas (když je volána metoda <see cref="IListenerApplicationIdle.ApplicationIdle()"/>) 
        /// si Ribbon fyzicky vyžádá donačtení dat ze serveru a následný Refresh.
        /// </summary>
        private bool _ActiveLazyLoadItemsOnIdle;
        /// <summary>
        /// Vrátí true, pokud daná stránka obsahuje v jakékoli grupě alespoň jeden prvek, který má deklarované statické subpoložky v <see cref="IRibbonItem.SubItems"/>.
        /// Tento test slouží k určení potřeby LazyLoad OnIdle.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <returns></returns>
        internal static bool ContainsAnyStaticSubItems(IRibbonPage iRibbonPage)
        {
            if (iRibbonPage == null || iRibbonPage.Groups == null) return false;
            return iRibbonPage.Groups
                .Where(g => g.Items != null)
                .SelectMany(g => g.Items)
                .Any(i => ContainsAnyStaticSubItems(i));
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek obsahuje nějaké existující subprvky v <see cref="IRibbonItem.SubItems"/>.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        internal static bool ContainsAnyStaticSubItems(IRibbonItem iRibbonItem)
        {
            return (iRibbonItem != null && iRibbonItem.SubItems != null && iRibbonItem.SubItems.Any());
        }
        #endregion
        #region Fyzická tvorba prvků Ribbonu (Kategorie, Stránka, Grupa, Prvek, konkrétní prvky, ...) : Get/Create/Clear/Add/Remove; plus Refresh + LazyLoad SubItems
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí kategorii pro daná data
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <param name="pageChangeMode"></param>
        /// <returns></returns>
        internal DxRibbonPageCategory GetPageCategory(IRibbonCategory iRibbonCategory, ContentChangeMode pageChangeMode = ContentChangeMode.ReFill)
        {
            if (iRibbonCategory is null || String.IsNullOrEmpty(iRibbonCategory.CategoryId)) return null;

            var changeMode = pageChangeMode;
            DxRibbonPageCategory pageCategory = PageCategories.GetCategoryByName(iRibbonCategory.CategoryId) as DxRibbonPageCategory;

            if (HasCreate(changeMode))
            {
                if (pageCategory is null)
                    pageCategory = new DxRibbonPageCategory(iRibbonCategory, PageCategories);
                else
                    pageCategory.Fill(iRibbonCategory);
            }
            return pageCategory;
        }

        /// <summary>
        /// Vytvoří a vrátí novou stránku, vloží ji do kolekce this.Pages
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public DxRibbonPage CreatePage(string text)
        {
            DxRibbonPage page = new DxRibbonPage(this, text);
            this.Pages.Add(page);
            return page;
        }
        /// <summary>
        /// Vytvoří a vrátí novou stránku, vloží ji do kolekce this.Pages.
        /// Tato metoda nevkládá grupy z dodané stránky.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="category">Kategorie, může být null</param>
        /// <returns></returns>
        public DxRibbonPage CreatePage(IRibbonPage iRibbonPage, DxRibbonPageCategory category = null)
        {
            var pages = category?.Pages ?? this.Pages;
            return new DxRibbonPage(this, iRibbonPage, pages);
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí stránku pro daná data.
        /// Stránku přidá do this Ribbonu nebo do dané kategorie.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="pages"></param>
        /// <returns></returns>
        protected DxRibbonPage GetPage(IRibbonPage iRibbonPage, RibbonPageCollection pages = null)
        {
            if (iRibbonPage is null) return null;

            var changeMode = iRibbonPage.ChangeMode;
            DxRibbonPage page = pages?.FirstOrDefault(r => (r.Name == iRibbonPage.PageId)) as DxRibbonPage;
            if (HasCreate(changeMode))
            {
                if (page is null)
                    page = new DxRibbonPage(this, iRibbonPage, pages);
                else
                {
                    page.Fill(iRibbonPage);
                    if (HasReFill(changeMode))
                        ClearPage(page);
                }
            }
            else if (HasRemove(changeMode))
            {
                RemovePage(page, pages);
            }

            return page;
        }
        /// <summary>
        /// Vyprázdní obsah dané stránky: odstraní grupy i itemy, i Lazy group.
        /// </summary>
        /// <param name="page"></param>
        protected void ClearPage(DxRibbonPage page)
        {
            page?.ClearContent();
        }
        /// <summary>
        /// Odebere danou stránku z kolekce stránek, pokud je zadáno a stránka tam existuje
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pages"></param>
        protected void RemovePage(DxRibbonPage page, RibbonPageCollection pages)
        {
            if (page != null && pages != null && pages.Contains(page))
                pages.Remove(page);
            page?.Reset();
        }

        /// <summary>
        /// Vytvoří a vrátí grupu daného jména, grupu vloží do dané stránky.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public DxRibbonGroup CreateGroup(string text, DxRibbonPage page)
        {
            var dxGroup = new DxRibbonGroup(text);
            if (page != null) page.Groups.Add(dxGroup);
            return dxGroup;
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí grupu, tu vloží do dané stránky Ribbonu
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        internal DxRibbonGroup CreateGroup(IRibbonGroup iRibbonGroup, DxRibbonPage dxPage)
        {
            var dxGroup = new DxRibbonGroup(iRibbonGroup, dxPage?.Groups);
            // event  dxGroup.CaptionButtonClick   nám řeší Ribbon,
            //   jehož event PageGroupCaptionButtonClick vede do metody:
            //   RibbonControl_PageGroupCaptionButtonClick(object sender, RibbonPageGroupEventArgs e)
            //     a tato pak vyvolá Ribbonový event RibbonGroupButtonClick.
            this._Groups.Add(dxGroup);
            return dxGroup;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a naplní grupu pro daná data.
        /// Grupu přidá do dané stránky, pokud tam dosud není.
        /// <para/>
        /// Pozor, tato metoda do grupy vloží její položky z dodané definice.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxGroup"></param>
        /// <param name="dxPage"></param>
        /// <param name="createMode"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected void ReloadGroup(IRibbonGroup iRibbonGroup, DxRibbonGroup dxGroup, DxRibbonPage dxPage, DxRibbonCreateContentMode createMode, ref int count)
        {
            var changeMode = iRibbonGroup.ChangeMode;
            if (dxGroup == null)
            {
                if (HasRemove(changeMode)) return;
                if (dxPage == null) return;
                dxGroup = CreateGroup(iRibbonGroup, dxPage);
            }
            else if (HasRemove(changeMode))
            {
                RemoveGroup(dxGroup, dxPage);
                return;
            }
            else
            {
                dxGroup.Fill(iRibbonGroup);
                if (HasReFill(changeMode))
                    dxGroup.ClearContent();
            }
            AddItemsToGroup(iRibbonGroup, dxGroup, createMode, ref count);
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí grupu pro daná data.
        /// Grupu přidá do dané stránky.
        /// <para/>
        /// Do grupy neplní položky.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        protected DxRibbonGroup GetGroup(IRibbonGroup iRibbonGroup, DxRibbonPage dxPage)
        {
            if (iRibbonGroup is null || dxPage is null) return null;

            var changeMode = iRibbonGroup.ChangeMode;
            DxRibbonGroup dxGroup = dxPage.Groups.GetGroupByName(iRibbonGroup.GroupId) as DxRibbonGroup;
            if (HasCreate(changeMode))
            {
                if (dxGroup is null)
                    dxGroup = CreateGroup(iRibbonGroup, dxPage);
                else
                {
                    dxGroup.Fill(iRibbonGroup);
                    if (HasReFill(changeMode))
                        dxGroup.ClearContent();
                }
            }
            else if (HasRemove(changeMode))
            {
                RemoveGroup(dxGroup, dxPage);
                dxGroup = null;
            }

            return dxGroup;
        }
        /// <summary>
        /// Odstraní obsah grupy a pak odstraní grupu ze stránky
        /// </summary>
        /// <param name="dxGroup"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        protected void RemoveGroup(DxRibbonGroup dxGroup, DxRibbonPage dxPage)
        {
            if (dxGroup is null) return;

            dxGroup.ClearContent();

            if (dxPage == null && dxGroup != null) 
                dxPage = dxGroup.OwnerDxPage;

            if (dxGroup != null && dxPage != null && dxPage.Groups.Contains(dxGroup))
                dxPage.Groups.Remove(dxGroup);

            dxGroup.Reset();
            this._Groups.Remove(dxGroup);
            dxGroup.Dispose();
        }

        /// <summary>
        /// Vytvoří a vrátí prvek dle definice. Prvek má naplněny všechny vlastnosti a má i svoje podpoložky.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="level"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.BarItem CreateItem(ref IRibbonItem iRibbonItem, DxRibbonGroup dxGroup = null, int level = 0, DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            int count = 0;
            var barItem = PrepareItem(ref iRibbonItem, dxGroup, level, true, null, ref count);
            if (barItem is null) return null;

            FillBarItem(barItem, iRibbonItem, level);
            if (clickHandler != null) barItem.ItemClick += clickHandler;

            if (dxGroup != null)
                dxGroup.ItemLinks.Add(barItem, iRibbonItem.ItemIsFirstInGroup);

            return barItem;
        }
        /// <summary>
        /// Metoda najde prvek podle jeho jména a aktualizuje jeho hodnoty.
        /// Pokud prvek neexistuje, tak ale nebude vytvořen!
        /// Do daného prvku se znovunaplní jeho vlastnosti (=Refresh) + jeho subpoložky.
        /// Touto cestou nelze změnit typ prvku!
        /// Lze refreshovat text, tooltip, image, checked, subitems.
        /// Lze vyžádat otevření submenu, pokud to submenu je.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        protected void RefreshCurrentItem(IRibbonItem iRibbonItem)
        {
            if (iRibbonItem == null) return;
            string itemId = iRibbonItem.ItemId;
            var barItem = this.Items[itemId];
            if (barItem == null) return;
            var itemInfo = barItem.Tag as BarItemTagInfo;
            if (itemInfo == null) return;

            RefreshCurrentIRibbonItem(itemInfo, barItem, iRibbonItem);
            FillBarItem(barItem, iRibbonItem, itemInfo.Level);

            if (iRibbonItem.SubItems != null)
            {
                switch (itemInfo.Data.ItemType)
                {
                    case RibbonItemType.SplitButton:
                        if (barItem is DevExpress.XtraBars.BarButtonItem splitButton)
                            // SplitButton má v sobě vytvořené PopupMenu, které nyní obsahuje jen jeden prvek; zajistíme vložení reálného menu:
                            _PopupMenu_RefreshItems(splitButton, itemInfo.Data, iRibbonItem);
                        break;
                    case RibbonItemType.Menu:
                        if (barItem is DevExpress.XtraBars.BarSubItem menu)
                            _BarMenu_RefreshItems(menu, itemInfo.Data, iRibbonItem);
                        break;
                }
            }
        }
        /// <summary>
        /// Do nově dodaného datového <see cref="IRibbonItem"/> vloží existující data o vizuálním prvku Ribbonu z dodaného balíčku informací <see cref="BarItemTagInfo"/>.
        /// Provádí se v procesu Refreshe, když do Ribbonu dorazí nová definice prvku <see cref="IRibbonItem"/>, a je třeba do ní vepsat reálné vazby na prvky/grupy v Ribbonu.
        /// </summary>
        /// <param name="sourceItemInfo"></param>
        /// <param name="barItem"></param>
        /// <param name="targetIRibbonItem"></param>
        protected void RefreshCurrentIRibbonItem(BarItemTagInfo sourceItemInfo, BarItem barItem, IRibbonItem targetIRibbonItem)
        {
            targetIRibbonItem.RibbonItem = barItem;
            targetIRibbonItem.ParentGroup = sourceItemInfo.Data.ParentGroup;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí BarItem pro daná data.
        /// BarItem přidá do dané grupy.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="level"></param>
        /// <param name="currentMode">Režim tvorby obsahu</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem GetItem(ref IRibbonItem iRibbonItem, DxRibbonGroup dxGroup, int level, DxRibbonCreateContentMode currentMode, ref int count)
        {
            if (iRibbonItem is null) return null;

            var changeMode = ((IRibbonObject)iRibbonItem).ChangeMode;
            var itemid = iRibbonItem.ItemId;
            DevExpress.XtraBars.BarItem barItem = Items[itemid];
            if (HasCreate(changeMode))
            {
                if (HasReFill(changeMode) && barItem != null)
                { /*  Vymazat reálné SubBarItemy ...  */ }

                bool reallyCreateSubItems = currentMode.HasFlag(DxRibbonCreateContentMode.CreateAllSubItems);
                if (barItem is null)
                    barItem = PrepareItem(ref iRibbonItem, dxGroup, level, reallyCreateSubItems, null, ref count);
                else if (reallyCreateSubItems && iRibbonItem.SubItems != null && iRibbonItem.SubItems.Any())
                    RefillSubItems(iRibbonItem, dxGroup, barItem, level, null, ref count);
               
                if (barItem is null) return null;

                // Prvek přidám do grupy jen tehdy, když máme grupu, a prvek v ní ještě není:
                if (dxGroup != null)
                {
                    if (!dxGroup.ItemLinks.TryGetFirst(l => l.Item.Name == itemid, out var barLink))
                        barLink = dxGroup.ItemLinks.Add(barItem);
                    barLink.BeginGroup = iRibbonItem.ItemIsFirstInGroup;
                }
                PrepareBarItemTag(barItem, iRibbonItem, level, dxGroup);
                FillBarItem(barItem, iRibbonItem, level);

                // Pokud prvek má mít SubItems, ale dosud nebyly vygenerovány (buď LazyLoad, anebo OnDemandLoad), řešíme to zde:
                DetectLazyLoadSubItems(barItem, iRibbonItem, level);
            }
            else if (HasRemove(changeMode))
            {   // Zatím prvky neodebíráme touto cestou.
                // V případě potřeby se prvky reálně odebírají tak, že se refreshuje celá grupa, tam se nejprve zahodí všechny její prvky a pak se vygenerují nové platné.
                // Zdejší cesta by byla o refreshi jednoho prvku, se zadáním jeho ItemId a s režimem změny Remove. To se ale dosud nepoužívá.
            }

            return barItem;
        }
        /// <summary>
        /// Vytvoří prvek BarItem pro daná data a vrátí jej.
        /// Prvek NEMÁ naplněné svoje property, to nechť zajistí volající metoda.
        /// Tato metoda NEVKLÁDÁ vytovřený prvek do dodané grupy; grupa smí být null.
        /// <para/>
        /// Tato metoda ale může vygenerovat SubItems do vytvořeného prvku, pokud je to možné dle typu prvku, a je to požadováno dle parametru <paramref name="reallyCreateSubItems"/>
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="level"></param>
        /// <param name="reallyCreateSubItems">Skutečně se mají vytvářet SubMenu?</param>
        /// <param name="forceItemType">Vynucený typ prvku, má přednost před <see cref="IRibbonItem.ItemType"/>; standardně zadávejme NULL</param>
        /// <param name="count"></param>
        /// <param name="ignoreQat">Neřešit QAT</param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem PrepareItem(ref IRibbonItem iRibbonItem, DxRibbonGroup dxGroup, int level, bool reallyCreateSubItems, RibbonItemType? forceItemType, ref int count, bool ignoreQat = false)
        {
            DevExpress.XtraBars.BarItem barItem = null;
            RibbonItemType itemType = forceItemType ?? GetValidCommonItemType(iRibbonItem.ItemType) ?? iRibbonItem.ItemType;
            bool createSubItems = reallyCreateSubItems;
            switch (itemType)
            {
                case RibbonItemType.Label:
                case RibbonItemType.LabelSpring:
                    count++;
                    var labelItem = new BarStaticItem() { Manager = this.Manager };
                    this.Items.Add(labelItem);
                    barItem = labelItem;
                    break;
                case RibbonItemType.Static:
                case RibbonItemType.StaticSpring:
                    count++;
                    var staticItem = new BarStaticItem() { Manager = this.Manager };
                    //    staticItem.ItemAppearance.Normal.TextOptions.VAlignment = VertAlignment.Center;       řeší se centrálně v 
                    this.Items.Add(staticItem);
                    barItem = staticItem;
                    break;
                case RibbonItemType.CheckButton:
                case RibbonItemType.CheckButtonPassive:
                    count++;
                    BarButtonItem checkButton = Items.CreateButton(iRibbonItem.Text);
                    checkButton.ButtonStyle = BarButtonStyle.Check;
                    checkButton.Down = (iRibbonItem.Checked ?? false);
                    barItem = checkButton;
                    break;
                case RibbonItemType.ButtonGroup:
                    count++;
                    createSubItems |= (iRibbonItem.SubItems != null && ContainsQAT(iRibbonItem));
                    DevExpress.XtraBars.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, createSubItems, ref count));
                    buttonGroup.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    count++;
                    createSubItems |= (iRibbonItem.SubItems != null && ContainsQAT(iRibbonItem));
                    var dxPopup = CreatePopupMenu(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, createSubItems, ref count);
                    DevExpress.XtraBars.BarButtonItem splitButton = Items.CreateSplitButton(iRibbonItem.Text, dxPopup);
                    barItem = splitButton;
                    break;
                case RibbonItemType.CheckBoxStandard:
                case RibbonItemType.CheckBoxPasive:
                    count++;
                    DevExpress.XtraBars.BarCheckItem checkItem = Items.CreateCheckItem(iRibbonItem.Text, iRibbonItem.Checked ?? false);
                    checkItem.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
                    barItem = checkItem;
                    break;
                case RibbonItemType.CheckBoxToggle:
                    count++;
                    DxBarCheckBoxToggle toggleSwitch = new DxBarCheckBoxToggle(this.BarManagerInt, iRibbonItem.Text);
                    this.Items.Add(toggleSwitch);
                    barItem = toggleSwitch;
                    break;
                case RibbonItemType.RadioItem:
                    count++;
                    DevExpress.XtraBars.BarCheckItem radioItem = Items.CreateCheckItem(iRibbonItem.Text, iRibbonItem.Checked ?? false);
                    barItem = radioItem;
                    break;
                case RibbonItemType.TrackBar:
                    //count++;
                    //DevExpress.XtraBars.BarCheckItem trackBarItem = Items.createtr .CreateCheckItem(iRibbonItem.Text, iRibbonItem.Checked ?? false);
                    //barItem = radioItem;
                    break;
                case RibbonItemType.Menu:
                    count++;
                    createSubItems |= (iRibbonItem.SubItems != null && ContainsQAT(iRibbonItem));
                    DevExpress.XtraBars.BarSubItem menu = Items.CreateMenu(iRibbonItem.Text);
                    PrepareBarMenu(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, menu, createSubItems, ref count);
                    barItem = menu;
                    break;
                case RibbonItemType.PopupMenuDropDown:
                    count++;
                    createSubItems |= (iRibbonItem.SubItems != null && ContainsQAT(iRibbonItem));
                    DevExpress.XtraBars.BarButtonItem dropDownItem = CreatePopupMenuDropDown(iRibbonItem, level, dxGroup, createSubItems, ref count);
                    if (dropDownItem != null)
                        this.Items.Add(dropDownItem);
                    barItem = dropDownItem;
                    break;
                case RibbonItemType.InRibbonGallery:
                    count++;
                    var galleryItem = CreateGalleryItem(iRibbonItem, level, dxGroup);
                    this.Items.Add(galleryItem);
                    barItem = galleryItem;
                    break;
                case RibbonItemType.SkinSetDropDown:
                    count++;
                    barItem = new DevExpress.XtraBars.SkinDropDownButtonItem();
                    this.Items.Add(barItem);
                    break;
                case RibbonItemType.SkinPaletteDropDown:
                    count++;
                    barItem = new DevExpress.XtraBars.SkinPaletteDropDownButtonItem();
                    this.Items.Add(barItem);
                    break;
                case RibbonItemType.SkinPaletteGallery:
                    count++;
                    barItem = new DevExpress.XtraBars.SkinPaletteRibbonGalleryBarItem();
                    this.Items.Add(barItem);
                    break;
                case RibbonItemType.ZoomPresetMenu:
                    count++;
                    var zoomMenu = new DxZoomMenuBarSubItem(ref iRibbonItem, this.Manager);
                    PrepareBarMenu(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, zoomMenu, true, ref count);
                    zoomMenu.RefreshMenuForCurrentZoom();
                    barItem = zoomMenu;
                    this.Items.Add(barItem);
                    break;
                case RibbonItemType.ComboListBox:
                    count++;
                    BarItem comboItem = CreateComboListBoxItem(iRibbonItem, level, dxGroup);
                    if (comboItem != null)
                        this.Items.Add(comboItem);
                    barItem = comboItem;
                    break;
                case RibbonItemType.RepositoryEditor:
                    count++;
                    BarEditItem editItem = CreateRepositoryEditorItem(iRibbonItem, level, dxGroup);
                    if (editItem != null)
                        this.Items.Add(editItem);
                    barItem = editItem;
                    break;
                case RibbonItemType.Button:
                case RibbonItemType.Header:
                default:
                    count++;
                    DevExpress.XtraBars.BarButtonItem button = Items.CreateButton(iRibbonItem.Text);
                    barItem = button;
                    break;
            }
            if (barItem != null)
            {
                barItem.Name = iRibbonItem.ItemId;
                PrepareBarItemTag(barItem, iRibbonItem, level, dxGroup);

                // Prvek patří do QAT?
                if (!ignoreQat && DefinedInQAT(GetRibbonQATKey(iRibbonItem)))
                    this.AddBarItemToQATUserList(barItem, iRibbonItem);
            }
            return barItem;
        }
        /// <summary>
        /// Do daného prvku znovu vygeneruje SubItems
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        /// <param name="forceItemType"></param>
        /// <param name="count"></param>
        protected void RefillSubItems(IRibbonItem iRibbonItem, DxRibbonGroup dxGroup, DevExpress.XtraBars.BarItem barItem, int level, RibbonItemType? forceItemType, ref int count)
        {
            RibbonItemType itemType = forceItemType ?? GetValidCommonItemType(iRibbonItem.ItemType) ?? iRibbonItem.ItemType;
            switch (itemType)
            {
                case RibbonItemType.Menu:
                    if (barItem is DevExpress.XtraBars.BarSubItem menu)
                    {
                        _BarMenu_FillItems(menu, level + 1, null, iRibbonItem, iRibbonItem.SubItems, true, ref count);
                    }
                    break;
            }
        }
        /// <summary>
        /// Do daného prvku vepíše data z definice.
        /// Aktualizuje i obsah BarItem.Tag.
        /// Tato metoda negeneruje SubItems!  Pouze řeší vlastnosti prvku (jako je Enabled, ImageName, Text a ToolTip, atd).
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level">0 pro Ribbonitem, 1 a vyšší pro prvky v menu</param>
        /// <param name="withReset"></param>
        protected void FillBarItem(DevExpress.XtraBars.BarItem barItem, IRibbonItem iRibbonItem, int level, bool withReset = false)
        {
            DelayResetForce(iRibbonItem, barItem);

            // Společné hodnoty:
            DxComponent.FillBarItemFrom(barItem, iRibbonItem, level);

            // Specifické styly:
            if (barItem is DevExpress.XtraBars.BarCheckItem checkItem)
            {   // Do CheckBoxu + RadioButtonu vepisujeme víc vlastností:
                checkItem.CheckBoxVisibility = CheckBoxVisibility.BeforeText;
                checkItem.CheckStyle =
                    (iRibbonItem.ItemType == RibbonItemType.RadioItem ? BarCheckStyles.Radio :
                    (iRibbonItem.ItemType == RibbonItemType.CheckBoxToggle ? BarCheckStyles.Standard : BarCheckStyles.Standard));
                checkItem.Checked = iRibbonItem.Checked ?? false;
            }
            if ((iRibbonItem.ItemType == RibbonItemType.CheckButton || iRibbonItem.ItemType == RibbonItemType.CheckButton) && barItem is BarBaseButtonItem barButton)
            {   // CheckButton:
                barButton.ButtonStyle = BarButtonStyle.Check;
                barButton.Down = (iRibbonItem.Checked.HasValue && iRibbonItem.Checked.Value);
            }
            if (barItem is DxBarCheckBoxToggle dxCheckBoxToggle)
            {
                dxCheckBoxToggle.CheckedSilent = iRibbonItem.Checked;
                if (iRibbonItem.ImageName != null) dxCheckBoxToggle.ImageNameNull = iRibbonItem.ImageName;
                if (iRibbonItem.ImageNameUnChecked != null) dxCheckBoxToggle.ImageNameUnChecked = iRibbonItem.ImageNameUnChecked;
                if (iRibbonItem.ImageNameChecked != null) dxCheckBoxToggle.ImageNameChecked = iRibbonItem.ImageNameChecked;
            }

            // FillBarItemImage(barItem, iRibbonItem, level, withReset);          // Image můžu řešit až po vložení velikosti, protože Image se řídí i podle velikosti prvku 

            RefreshBarItemTag(barItem, iRibbonItem);
        }
        /// <summary>
        /// Nastaví dodaný styl písma do daného prvku, do jeho odpovídající Appearance, do všech stavů
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="fontStyle"></param>
        /// <param name="level"></param>
        /// <param name="withReset"></param>
        protected void FillBarItemFontStyle(BarItem barItem, System.Drawing.FontStyle fontStyle, int level, bool withReset = false)
        {
            var appearance = ((level == 0) ? barItem.ItemAppearance : barItem.ItemInMenuAppearance);
            appearance.Normal.FontStyleDelta = fontStyle;
            appearance.Hovered.FontStyleDelta = fontStyle;
            appearance.Pressed.FontStyleDelta = fontStyle;
            appearance.Disabled.FontStyleDelta = fontStyle;
        }
        /// <summary>
        /// Do daného prvku Ribbonu vepíše vše pro jeho Image
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level">0 pro Ribbonitem, 1 a vyšší pro prvky v menu</param>
        /// <param name="withReset"></param>
        protected void FillBarItemImage(BarItem barItem, IRibbonItem iRibbonItem, int level, bool withReset = false)
        {
            if (barItem is DxBarCheckBoxToggle) return;                        // DxCheckBoxToggle si řídí Image sám

            if ((iRibbonItem.ItemType == RibbonItemType.CheckButton || iRibbonItem.ItemType == RibbonItemType.CheckButtonPassive || iRibbonItem.ItemType == RibbonItemType.CheckBoxStandard || iRibbonItem.ItemType == RibbonItemType.CheckBoxPasive || iRibbonItem.ItemType == RibbonItemType.RadioItem) && barItem is BarBaseButtonItem barButton)
                RibbonItemSetImageByChecked(iRibbonItem, barButton, level);    // S možností volby podle Checked
            else
                RibbonItemSetImageStandard(iRibbonItem, barItem, level);
        }
        /// <summary>
        /// Připraví do prvku Ribbonu obrázek (ikonu) podle aktuálního stavu a dodané definice, pro standardní button
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        protected void RibbonItemSetImageStandard(IRibbonItem iRibbonItem, BarItem barItem, int? level = null)
        {
            // Určíme, zda prvek je přímo v Ribbonu nebo až jako subpoložka:
            //  Hodnota level se předává v procesu prvotní tvorby, pak Root prvek má level == 0;
            //  Pokud hodnota level není předána, pak jsme volání z obsluhy kliknutí na prvek, a tam se spolehneme na hodnotu IRibbonItem.ParentItem.
            bool isRootItem = (level.HasValue ? (level.Value == 0) : (iRibbonItem.ParentItem is null));

            // Velikost obrázku: pro RootItem (vlastní prvky v Ribbonu) ve stylu Large nebo Default dáme obrázky Large, jinak dáme Small (pro malé prvky Ribbonu a pro položky menu, ty mají Level 1 a vyšší):
            bool isLargeIcon = (isRootItem && (iRibbonItem.RibbonStyle.HasFlag(RibbonItemStyles.Large) || iRibbonItem.RibbonStyle == RibbonItemStyles.Default));
            ResourceImageSizeType sizeType = (isLargeIcon ? ResourceImageSizeType.Large : ResourceImageSizeType.Small);

            // Náhradní ikonky (pro nezadané nebo neexistující ImageName) budeme generovat jen pro level = 0 = Ribbon, a ne pro Menu!
            string imageCaption = DxComponent.GetCaptionForRibbonImage(iRibbonItem, level);
            DxComponent.ApplyImage(barItem.ImageOptions, iRibbonItem.ImageName, iRibbonItem.Image, sizeType, caption: imageCaption, prepareDisabledImage: iRibbonItem.PrepareDisabledImage, imageListMode: iRibbonItem.ImageListMode);
        }
        /// <summary>
        /// Připraví do prvku Ribbonu obrázek (ikonu) podle aktuálního stavu a dodané definice, pro button typu CheckButton nebo CheckBox
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="barButton"></param>
        /// <param name="level"></param>
        protected void RibbonItemSetImageByChecked(IRibbonItem iRibbonItem, BarBaseButtonItem barButton, int? level = null)
        {
            // Určíme, zda prvek je přímo v Ribbonu nebo až jako subpoložka:
            //  Hodnota level se předává v procesu prvotní tvorby, pak Root prvek má level == 0;
            //  Pokud hodnota level není předána, pak jsme volání z obsluhy kliknutí na prvek, a tam se spolehneme na hodnotu IRibbonItem.ParentItem.
            bool isRootItem = (level.HasValue ? (level.Value == 0) : (iRibbonItem.ParentItem is null));

            // Velikost obrázku: pro RootItem (vlastní prvky v Ribbonu) ve stylu Large nebo Default dáme obrázky Large, jinak dáme Small (pro malé prvky Ribbonu a pro položky menu, ty mají Level 1 a vyšší):
            bool isLargeIcon = (isRootItem && (iRibbonItem.RibbonStyle.HasFlag(RibbonItemStyles.Large) || iRibbonItem.RibbonStyle == RibbonItemStyles.Default));
            ResourceImageSizeType sizeType = (isLargeIcon ? ResourceImageSizeType.Large : ResourceImageSizeType.Small);

            // Náhradní ikonky (pro nezadané nebo neexistující ImageName) budeme generovat jen pro level = 0 = Ribbon, a ne pro Menu!
            string imageCaption = DxComponent.GetCaptionForRibbonImage(iRibbonItem, level);

            // Zvolíme aktuálně platný obrázek - podle hodnoty iRibbonItem.Checked a pro zadáné obrázky:
            string imageName = (!iRibbonItem.Checked.HasValue ? iRibbonItem.ImageName :                                                                        // Pro hodnotu NULL
                   ((iRibbonItem.Checked.HasValue && !iRibbonItem.Checked.Value) ? _GetDefinedImage(iRibbonItem.ImageNameUnChecked, iRibbonItem.ImageName) :   // pro False
                   ((iRibbonItem.Checked.HasValue && iRibbonItem.Checked.Value) ? _GetDefinedImage(iRibbonItem.ImageNameChecked, iRibbonItem.ImageName) :      // pro True
                   null)));
            var image = iRibbonItem.Image;

            // Pozor, pro DevExpress platí:
            // Máme-li prvek na SubPoložoce v menu, a prvek je Checked, pak nesmí mít Image - protože DevExpress nezobrazuje vedle sebe Image a CheckIcon, ale zobrazí prioritně Image a tím skryje CheckIcon:
            if (!isRootItem && iRibbonItem.Checked.HasValue && iRibbonItem.Checked.Value)
            {
                imageName = null;
                image = null;
            }
            DxComponent.ApplyImage(barButton.ImageOptions, imageName, iRibbonItem.Image, sizeType, caption: imageCaption, prepareDisabledImage: iRibbonItem.PrepareDisabledImage, imageListMode: iRibbonItem.ImageListMode);
        }
        /// <summary>
        /// Vrátí první neprázdný obrázek
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        private static string _GetDefinedImage(params string[] images)
        {
            return images.FirstOrDefault(i => !String.IsNullOrEmpty(i));
        }
        /// <summary>
        /// Do daného prvku Ribbonu vepíše vše pro jeho HotKey
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level">0 pro Ribbonitem, 1 a vyšší pro prvky v menu</param>
        /// <param name="withReset"></param>
        protected void FillBarItemHotKey(DevExpress.XtraBars.BarItem barItem, IRibbonItem iRibbonItem, int level, bool withReset = false)
        {
            DxComponent.FillBarItemHotKey(barItem, iRibbonItem);
        }
        /// <summary>
        /// Vrátí Buttony pro dané SubItemy
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="subItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected BarBaseButtonItem[] GetBarBaseButtons(IRibbonItem parentItem, int level, DxRibbonGroup dxGroup, IEnumerable<IRibbonItem> subItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarBaseButtonItem> baseButtons = new List<DevExpress.XtraBars.BarBaseButtonItem>();
            if (subItems != null && reallyCreate)
            {
                foreach (IRibbonItem subItem in subItems)
                {
                    subItem.ParentItem = parentItem;
                    subItem.ParentGroup = parentItem.ParentGroup;
                    DevExpress.XtraBars.BarBaseButtonItem baseButton = CreateBaseButton(subItem, level, dxGroup);
                    if (baseButton != null)
                        baseButtons.Add(baseButton);
                }
            }
            return baseButtons.ToArray();
        }
        /// <summary>
        /// Vytvoří a vrátí jednoduchý Button
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <returns></returns>
        protected BarBaseButtonItem CreateBaseButton(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup)
        {
            DevExpress.XtraBars.BarBaseButtonItem baseButton = null;
            switch (iRibbonItem.ItemType)
            {
                case RibbonItemType.Button:
                case RibbonItemType.ButtonGroup:
                    // DevExpress.XtraBars.BarBaseButtonItem buttonItem = new DevExpress.XtraBars.BarButtonItem(this.Manager, item.ItemText);
                    DevExpress.XtraBars.BarLargeButtonItem buttonItem = new DevExpress.XtraBars.BarLargeButtonItem(this.Manager, iRibbonItem.Text);
                    baseButton = buttonItem;
                    break;
                case RibbonItemType.CheckBoxStandard:
                case RibbonItemType.CheckBoxPasive:
                    DevExpress.XtraBars.BarCheckItem checkItem = new DevExpress.XtraBars.BarCheckItem(this.Manager);
                    baseButton = checkItem;
                    break;
            }
            if (baseButton != null)
            {
                baseButton.Name = iRibbonItem.ItemId;
                PrepareBarItemTag(baseButton, iRibbonItem, level, dxGroup);
                FillBarItem(baseButton, iRibbonItem, level);
            }
            return baseButton;
        }
        /// <summary>
        /// V daném prvku najde a vrátí kolekci jeho SubItems - jen první úrovně.
        /// Detekuje tedy známé konkrétní podtypy a vyhledá jejich konkrétní prvky.
        /// </summary>
        /// <param name="barItem"></param>
        /// <returns></returns>
        internal static IEnumerable<BarItem> GetSubItems(BarItem barItem)
        {
            if (barItem is DevExpress.XtraBars.BarButtonGroup buttonGroup) return buttonGroup.ItemLinks?.Select(l => l.Item);
            if (barItem is DevExpress.XtraBars.BarButtonItem splitButton)
            {
                if (splitButton.DropDownControl is PopupMenu popupMenu) return popupMenu.ItemLinks.Select(l => l.Item);
                return null;
            }
            if (barItem is BarSubItem menu) return menu.ItemLinks.Select(l => l.Item);
            // Galery: obsahuje v sobě GalleryGroup a ty obsahují GalleryItem, ale nic z toho není BarItem !!!   if (barItem is RibbonGalleryBarItem galleryItem) return galleryItem.Gallery.Groups.SelectMany(g => g.Items.SelectMany(i => i..Add(galleryGroup); .ItemLinks.Select(l => l.Item);
            return null;
        }
        /// <summary>
        /// Vrací reálně použitelný typ pro prvek v Ribbonu tak, aby Ribbon neměl problémy takové prvky zobrazit.
        /// Problémy nastávaly při zobrazení některých prvků, kdy se znemožnilo zobrazit obsah collapsed grupy (zhasínala).
        /// </summary>
        /// <param name="subItemType"></param>
        /// <returns></returns>
        protected static RibbonItemType? GetValidCommonItemType(RibbonItemType subItemType)
        {
            switch (subItemType)
            {   // Toto jsou náhrady za prvek Ribbonu, kdy některé požadované typy prvků neze používat, na jejich místo dáme prvek jiného typu:
                case RibbonItemType.Label: return RibbonItemType.Label;
                case RibbonItemType.LabelSpring: return RibbonItemType.LabelSpring;
                case RibbonItemType.Static: return RibbonItemType.Static;
                case RibbonItemType.StaticSpring: return RibbonItemType.StaticSpring;
                case RibbonItemType.CheckButton: return null;
                case RibbonItemType.CheckButtonPassive: return null;
                case RibbonItemType.ButtonGroup: return RibbonItemType.Menu;
                case RibbonItemType.SplitButton: return RibbonItemType.Menu;             // SplitButton dělá chyby, nahrazuji dočasně Menu            KEY: COLLAPSEDGROUP ERROR
                case RibbonItemType.CheckBoxStandard: return null;
                case RibbonItemType.CheckBoxPasive: return null;
                case RibbonItemType.CheckBoxToggle: return RibbonItemType.CheckBoxStandard;
                case RibbonItemType.RadioItem: return null;
                case RibbonItemType.TrackBar: return null;
                case RibbonItemType.Menu: return RibbonItemType.Menu;
                case RibbonItemType.PopupMenuDropDown: return RibbonItemType.PopupMenuDropDown;
                case RibbonItemType.InRibbonGallery: return null;  // otestujeme v nových DX, namísto:  RibbonItemType.Menu;         // InRibbonGallery dělá chyby, nahrazuji dočasně Menu        KEY: COLLAPSEDGROUP ERROR
                case RibbonItemType.SkinSetDropDown: return null;
                case RibbonItemType.SkinPaletteDropDown: return null;
                case RibbonItemType.SkinPaletteGallery: return null;
                case RibbonItemType.ZoomPresetMenu: return null;
                case RibbonItemType.ComboListBox: return RibbonItemType.ComboListBox;
                case RibbonItemType.RepositoryEditor: return RibbonItemType.RepositoryEditor;
                case RibbonItemType.Header: return RibbonItemType.Header;
                case RibbonItemType.Button:
                default:
                    return RibbonItemType.Button;
            }
        }
        /// <summary>
        /// Vrací reálně použitelný typ pro prvek v Menu tak, aby Ribbon neměl problémy takové menu zobrazit.
        /// Problémy nastávaly při zobrazení InRibbonGallery a CheckBoxToggle v menu, kdy takové menu znemožnilo zobrazit obsah collapsed grupy (zhasínala).
        /// </summary>
        /// <param name="subItemType"></param>
        /// <returns></returns>
        protected static RibbonItemType? GetValidSubItemType(RibbonItemType subItemType)
        {
            switch (subItemType)
            {   // Toto jsou náhrady za prvek SubMenu, kdy některé požadované typy prvků neze používat, na jejich místo dáme prvek jiného typu:
                case RibbonItemType.Label: return RibbonItemType.Button;
                case RibbonItemType.LabelSpring: return RibbonItemType.Button;
                case RibbonItemType.Static: return RibbonItemType.Button;
                case RibbonItemType.StaticSpring: return RibbonItemType.Button;
                case RibbonItemType.CheckButton: return null;
                case RibbonItemType.CheckButtonPassive: return null;
                case RibbonItemType.ButtonGroup: return RibbonItemType.Menu;                          // Menu        KEY: COLLAPSEDGROUP ERROR
                case RibbonItemType.SplitButton: return RibbonItemType.Menu;                          // Menu        KEY: COLLAPSEDGROUP ERROR
                case RibbonItemType.CheckBoxStandard: return null;
                case RibbonItemType.CheckBoxPasive: return null;
                case RibbonItemType.CheckBoxToggle: return RibbonItemType.CheckBoxStandard;           // Std         KEY: COLLAPSEDGROUP ERROR
                case RibbonItemType.RadioItem: return null;
                case RibbonItemType.TrackBar: return null;
                case RibbonItemType.Menu: return null;
                case RibbonItemType.PopupMenuDropDown: return RibbonItemType.PopupMenuDropDown;
                case RibbonItemType.InRibbonGallery: return null;  // otestujeme v nových DX, namísto:  RibbonItemType.Menu;                      // Menu        KEY: COLLAPSEDGROUP ERROR
                case RibbonItemType.SkinSetDropDown: return null;
                case RibbonItemType.SkinPaletteDropDown: return null;
                case RibbonItemType.SkinPaletteGallery: return null;
                case RibbonItemType.ZoomPresetMenu: return null;
                case RibbonItemType.ComboListBox: return RibbonItemType.Menu;
                case RibbonItemType.RepositoryEditor: return RibbonItemType.RepositoryEditor;
                case RibbonItemType.Header: return RibbonItemType.Header;
                case RibbonItemType.Button:
                default:
                    return RibbonItemType.Button;
            }
        }
        /// <summary>
        /// Metoda detekuje, zda prvek má mít nějaké SubItems, zda je reálně má, a vytvoří odpovídající příznaky pro LazyLoad tvorbu SubItems
        /// a nastaví do <see cref="_ActiveLazyLoadItemsOnIdle"/> hodnotu true = aby Ribbon věděl, že ve stavu ApplicationIdle má něco provádět pro svoje Items.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        protected void DetectLazyLoadSubItems(BarItem barItem, IRibbonItem iRibbonItem, int level)
        {
            // Pokud už teď máme nastaven příznak _ActiveLazyLoadItemsOnIdle = true, pak další není nutno provádět (jeden příznak stačí, víc jich stejně nemáme):
            if (_ActiveLazyLoadItemsOnIdle) return;

            // Zjistíme, zda daný prvek bude potřebovat nějaký LazyLoad:
            RibbonItemType itemType = iRibbonItem.ItemType;
            bool needSubItems = (itemType == RibbonItemType.ButtonGroup || itemType == RibbonItemType.SplitButton || itemType == RibbonItemType.Menu || itemType == RibbonItemType.InRibbonGallery);
            if (needSubItems && barItem.Tag is BarItemTagInfo itemInfo && itemInfo.LazyInfo != null)
                _ActiveLazyLoadItemsOnIdle = true;
        }

        // PopupMenu - pro SplitButton:
        /// <summary>
        /// Vytvoří a vrátí objekt <see cref="DevExpress.XtraBars.PopupMenu"/>, který se používá pro prvek typu <see cref="RibbonItemType.SplitButton"/> jako jeho DropDown menu
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="subItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.PopupMenu CreatePopupMenu(IRibbonItem parentItem, int level, DxRibbonGroup dxGroup, IEnumerable<IRibbonItem> subItems, bool reallyCreate, ref int count)
        {
            DevExpress.XtraBars.PopupMenu dxPopupMenu = new DevExpress.XtraBars.PopupMenu(BarManagerInt);

            bool hasSubItems = (subItems != null);
            bool isLazyLoad = (parentItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || parentItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
            if (hasSubItems && reallyCreate)
            {   // Vytvořit menu hned:
                _PopupMenu_FillItems(dxPopupMenu, level + 1, dxGroup, parentItem, subItems, false, ref count);
                PrepareBarItemTag(dxPopupMenu, parentItem, level, dxGroup, null);
            }
            else if (hasSubItems || isLazyLoad)
            {   // Vytvořit obsah až bude třeba (BeforePopup) = tj. když máme prvky, ale není požadavek reallyCreate, anebo je definováno LazyLoad:
                PrepareBarItemTag(dxPopupMenu, parentItem, level, dxGroup, new LazySubItemsInfo(parentItem, parentItem.SubItemsContentMode, subItems));
            }
            if (level == 0)
            {   // Události řeším jenom pro menu základní úrovně (=na tlačítku), ne pro jejich vnořené submenu:
                dxPopupMenu.BeforePopup += _PopupMenu_BeforePopup;
                dxPopupMenu.CloseUp += _PopupMenu_CloseUp;

                // Pokud právě nyní generujeme PopupMenu, jehož ItemId se shoduje s prvkem menu, který je připraven ke znovuotevření v proměnné _OpenItemPopupMenu (identifikátor _OpenItemPopupMenuItemId je shodný s aktuálním menu),
                //   pak se právě nyní provádí velký refresh (stránky / grupy), který zahodil dosavadní staré PopupMenu a nyní se vygeneroval nový PopupMenu.
                // Znamená to, že si do proměnné _OpenItemPopupMenu musíme uložit novou instanci (menu), protože ji zanedlouho budeme otevírat!
                // Stará instance, uložená v _OpenItemPopupMenu, je k nepoužití, protože je odebraná z Ribbonu...
                if (_OpenItemPopupMenuItemId != null && _OpenItemPopupMenu != null && _OpenItemPopupMenuItemId == parentItem.ItemId)
                    _OpenItemPopupMenu = dxPopupMenu;
            }
            return dxPopupMenu;
        }
        /// <summary>
        /// Událost před otevřením <see cref="DevExpress.XtraBars.PopupMenu"/> (je použito ve Split Buttonu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PopupMenu_BeforePopup(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!(sender is DevExpress.XtraBars.PopupMenu dxPopup)) return;

            _OpenMenuReset(true);

            bool deactivatePopupEvent = true;
            var itemInfo = dxPopup.Tag as BarItemTagInfo;                 // Do Popup jsme instanci BarItemTagInfo vytvořili při jeho tvorbě v metodě CreatePopupMenu()
            if (itemInfo != null && itemInfo.LazyInfo != null)
            {   // Pokud máme LazyInfo:
                int level = itemInfo.Level;
                var lazyinfo = itemInfo.LazyInfo;
                // Mohou nastat dvě situace:
                bool cancel = true;                                       // Zobrazení menu možná potlačíme, pokud nebudeme mít k dispozici stávající definované prvky
                if (lazyinfo.SubItems != null)
                {   // 1. SubItems jsou deklarované přímo zde (Static), pak z nich vytvoříme nabídku a necháme ji uživateli zobrazit:
                    var startTime = DxComponent.LogTimeCurrent;
                    int count = 0;
                    _PopupMenu_FillItems(dxPopup, level + 1, itemInfo.DxGroup, lazyinfo.ParentItem, lazyinfo.SubItems, true, ref count);
                    if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
                    
                    if (lazyinfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || lazyinfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce)
                        deactivatePopupEvent = false;                     // Pokud máme o SubItems žádat server, necháme si aktivní událost Popup
                    else
                        itemInfo.LazyInfo = null;                         // Data máme. Více již LazyInfo nebudeme potřebovat, a událost Popup deaktivujeme.
                    cancel = false;                                       // Otevírání Popup menu nebudeme blokovat, protože v něm jsou nějaká data
                }
                if (lazyinfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce || lazyinfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
                {   // 2. SubItems mi dodá aplikace - na základě obsluhy eventu ItemOnDemandLoad; pak dojde k vyvolání zdejší metody _PopupMenu_RefreshItems():
                    if (!IsSearchEditActive && !lazyinfo.RefreshPending)
                    {
                        _OpenItemPopupMenu = dxPopup;
                        _OpenItemPopupMenuItemId = itemInfo.Data.ItemId;
                        _OpenMenuLocation = GetPopupLocation(dxPopup.Activator as DevExpress.XtraBars.BarItemLink);
                        lazyinfo.Activator = null;
                        lazyinfo.PopupLocation = _OpenMenuLocation;
                        lazyinfo.CurrentMergeLevel = this.MergeLevel;
                        lazyinfo.RefreshPending = true;
                        this.RunItemOnDemandLoad(lazyinfo.ParentItem);    // Vyvoláme event pro načtení dat, ten zavolá RefreshItem, ten provede UnMerge - Modify - Merge, a v Modify vyvolá zdejší metodu _PopupMenu_RefreshItems()...
                    }
                    deactivatePopupEvent = false;                         // Dokud nedoběhnou ze serveru data (OnDemandLoad => RefreshItem() ), tak necháme aktivní událost Popup = pro jistotu...
                }
                else
                {   // 3. Nemáme Static prvky SubItems, ale ani je nemáme načítat OnDemand...
                    //    necháme odpojit event Popup, a zahodíme LazyInfo.
                    itemInfo.LazyInfo = null;                             // Více již LazyInfo nebudeme potřebovat...
                }
                e.Cancel = cancel;
            }

            // Deaktivovat zdejší handler?
            if (deactivatePopupEvent)
                dxPopup.BeforePopup -= _PopupMenu_BeforePopup;
        }
        /// <summary>
        /// Metoda zajistí vytvoření obsahu menu Popup, buď z prvků již definovaných anebo si položky menu vyžádá prostřednictvím metody <see cref="RunItemOnDemandLoad(IRibbonItem)"/>.
        /// Tato metoda se volá výhradně při řešení LazyItems v rámci Application.Idle.
        /// </summary>
        private void _PopupMenu_CreateLazyItems(DevExpress.XtraBars.PopupMenu dxPopup, BarItemTagInfo itemInfo)
        {
            if (dxPopup is null || itemInfo is null || itemInfo.LazyInfo is null) return;

            bool deactivatePopupEvent = true;
            var lazyInfo = itemInfo.LazyInfo;
            if (lazyInfo.SubItems != null)
            {   // 1. SubItems jsou deklarované přímo zde (Static), pak z nich vytvoříme nabídku a je hotovo:
                var startTime = DxComponent.LogTimeCurrent;
                int count = 0;
                _PopupMenu_FillItems(dxPopup, itemInfo.Level + 1, itemInfo.DxGroup, lazyInfo.ParentItem, lazyInfo.SubItems, true, ref count);
                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);

                if (lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce)
                    deactivatePopupEvent = false;                     // Pokud máme o SubItems žádat server, necháme si aktivní událost Popup
                else
                    itemInfo.LazyInfo = null;                         // Data máme. Více již LazyInfo nebudeme potřebovat, a událost Popup deaktivujeme.
            }
            if (lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce || lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
            {   // 2. SubItems mi dodá aplikace - na základě obsluhy eventu ItemOnDemandLoad; pak dojde k vyvolání zdejší metody _PopupMenu_RefreshItems():
                if (!IsSearchEditActive && !lazyInfo.RefreshPending)
                {
                    lazyInfo.CurrentMergeLevel = this.MergeLevel;
                    lazyInfo.RefreshPending = true;
                    this.RunItemOnDemandLoad(lazyInfo.ParentItem);    // Vyvoláme event pro načtení dat, ten zavolá RefreshItem, ten provede UnMerge - Modify - Merge, a v Modify vyvolá zdejší metodu _PopupMenu_RefreshItems()...
                }
                deactivatePopupEvent = false;                         // Dokud nedoběhnou ze serveru data (OnDemandLoad => RefreshItem() ), tak necháme aktivní událost Popup = pro jistotu...
            }
            else
            {   // 3. Nemáme Static prvky SubItems, ale ani je nemáme načítat OnDemand...
                //    necháme odpojit event Popup, a zahodíme LazyInfo.
                itemInfo.LazyInfo = null;                             // Více již LazyInfo nebudeme potřebovat...
            }

            // Deaktivovat zdejší handler?
            if (deactivatePopupEvent)
                dxPopup.BeforePopup -= _PopupMenu_BeforePopup;
        }
        /// <summary>
        /// Při zavírání Popup menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PopupMenu_CloseUp(object sender, EventArgs e)
        {
            if (sender is PopupMenu dxPopup && dxPopup.Tag is BarItemTagInfo itemInfo && itemInfo.Level == 0)
            {   // Menu se může zavírat obecně ze dvou důvodů:
                // 1. Uživatel něco vybral a akce se provádí, anebo uživatel nic nevybral a kliknul mimo a menu se zavírá => toto menu má tedy být úmyslně zavřené!
                //   anebo
                // 2. Menu je otevřené, ale přišel nám do Ribbonu Refresh nějakých dat, provádí se UnMerge - Modify - Merge, a při tom procesu se menu zavře automaticky => toto menu budeme chtít poté otevřít, a nesmíme jej tedy resetovat!
                if (!this.CurrentModifiedState)
                    _OpenMenuReset();

                // Pokud je menu v režimu OnDemandLoadEveryTime, tak po jeho zavření zajistíme znovu vyvolání handleru pro OnDemand donačtení při dalším otevírání menu:
                if (itemInfo.Data != null && itemInfo.Data.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
                    dxPopup.BeforePopup += _PopupMenu_BeforePopup;
            }
        }
        /// <summary>
        /// Metoda najde menu v daném prvku <paramref name="splitButton"/>, a vygeneruje do tohoto menu správné prvky dodané explicitně.
        /// Pokud je požadováno, otevře toto menu.
        /// Pozor, tato metoda by byla ráda, když by na vstupu měla nezměněný Tag v prvku <paramref name="splitButton"/>, protože v něm je uložena pozice pro DropDown menu.
        /// </summary>
        /// <param name="splitButton"></param>
        /// <param name="oldRibbonItem">Původní definiční objekt</param>
        /// <param name="newRibbonItem">Nový definiční objekt</param>
        private void _PopupMenu_RefreshItems(DevExpress.XtraBars.BarButtonItem splitButton, IRibbonItem oldRibbonItem, IRibbonItem newRibbonItem)
        {
            if (splitButton.DropDownControl is DevExpress.XtraBars.PopupMenu dxPopup && dxPopup.Tag is BarItemTagInfo itemInfo)
            {
                bool deactivatePopupEvent = true;

                // Do Popup vložíme dodané prvky (newRibbonItem.SubItems):
                var startTime = DxComponent.LogTimeCurrent;
                int level = itemInfo.Level;
                int count = 0;
                _PopupMenu_FillItems(dxPopup, level + 1, itemInfo.DxGroup, newRibbonItem, newRibbonItem.SubItems, true, ref count);
                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);

                // Pokud menu je definováno jako 'OnDemandLoadEveryTime', pak bychom měli zajistit předání LazyInfo i pro následující otevření:
                if (newRibbonItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
                {
                    itemInfo.LazyInfo = new LazySubItemsInfo(newRibbonItem, newRibbonItem.SubItemsContentMode, newRibbonItem.SubItems);
                    deactivatePopupEvent = false;                     // Pokud máme o SubItems vždycky žádat server, necháme si aktivní událost Popup
                }
                else
                {
                    itemInfo.LazyInfo = null;
                }
                // Aktualizujeme info uložené v splitButton.DropDownControl.Tag  (tj. PopupMenu.Tag):
                itemInfo.Data = newRibbonItem;

                // Deaktivace eventu BeforePopup, pokud je požadována (tj. kromě režimu OnDemandLoadEveryTime)
                if (deactivatePopupEvent)
                    dxPopup.BeforePopup -= _PopupMenu_BeforePopup;
            }
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.PopupMenu"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu tvorby menu (při inicializaci nebo při BeforePopup v LazyInit modu)
        /// </summary>
        /// <param name="dxPopup"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="clear">Smazat dosavadní obsah</param>
        /// <param name="count"></param>
        private void _PopupMenu_FillItems(DevExpress.XtraBars.PopupMenu dxPopup, int level, DxRibbonGroup dxGroup, IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, bool clear, ref int count)
        {
            if (clear && dxPopup.ItemLinks.Count > 0)
            {
                this.RemoveItemsFromQat(dxPopup.ItemLinks.Select(l => l.Item));
                dxPopup.ItemLinks.Clear();
            }

            dxPopup.BeforePopup -= _PopupMenu_BeforePopup;

            foreach (IRibbonItem subItem in subItems)
            {
                var iSubItem = subItem;
                RibbonItemType? forceItemType = GetValidSubItemType(iSubItem.ItemType);
                DevExpress.XtraBars.BarItem barItem = PrepareItem(ref iSubItem, dxGroup, level, true, forceItemType, ref count);
                iSubItem.ParentItem = parentItem;
                iSubItem.ParentGroup = parentItem.ParentGroup;

                if (barItem != null)
                {
                    PrepareBarItemTag(barItem, iSubItem, level, dxGroup);
                    var barLink = dxPopup.AddItem(barItem);
                    if (iSubItem.ItemIsFirstInGroup) barLink.BeginGroup = true;
                }
            }
        }
        /// <summary>
        /// Zajistí otevření daného menu na daném místě
        /// </summary>
        /// <param name="openMenuPopupMenu"></param>
        /// <param name="popupLocation"></param>
        private void _PopupMenu_OpenMenu(PopupControl openMenuPopupMenu, Point popupLocation)
        {
            openMenuPopupMenu.ShowPopup(this.BarManagerInt, popupLocation);
        }
        /// <summary>
        /// Vrátí souřadnici pro zobrazení Popup menu
        /// </summary>
        /// <param name="barItemLink"></param>
        /// <returns></returns>
        private Point? GetPopupLocation(DevExpress.XtraBars.BarItemLink barItemLink)
        {
            if (barItemLink == null) return Control.MousePosition;
            Rectangle bounds = barItemLink.ScreenBounds;
            return new Point(bounds.X, bounds.Bottom);
        }
        /// <summary>
        /// Vrátí souřadnici pro zobrazení Popup menu
        /// </summary>
        /// <param name="lazySubItems"></param>
        /// <returns></returns>
        private Point GetPopupLocation(LazySubItemsInfo lazySubItems)
        {
            if (lazySubItems != null)
            {
                if (lazySubItems.PopupLocation.HasValue) return lazySubItems.PopupLocation.Value;
                if (lazySubItems.Activator != null && lazySubItems.Activator is DevExpress.XtraBars.BarItemLink barItemLink)
                {
                    Rectangle bounds = barItemLink.ScreenBounds;
                    return new Point(bounds.X, bounds.Bottom);
                }
            }
            return Control.MousePosition;
        }

        // BarSubItem pro Menu
        /// <summary>
        /// Naplní položky do daného menu <see cref="DevExpress.XtraBars.BarSubItem"/>, používá se pro prvek typu <see cref="RibbonItemType.Menu"/>
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="subItems"></param>
        /// <param name="menu"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        private void PrepareBarMenu(IRibbonItem parentItem, int level, DxRibbonGroup dxGroup, IEnumerable<IRibbonItem> subItems, DevExpress.XtraBars.BarSubItem menu, bool reallyCreate, ref int count)
        {
            bool hasSubItems = (subItems != null);
            if (hasSubItems && reallyCreate)
                _BarMenu_FillItems(menu, level + 1, dxGroup, parentItem, subItems, false, ref count);

            // Tag info plus LazySubItemsInfo:
            bool isLazyLoad = (parentItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || parentItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
            LazySubItemsInfo lazyInfo = (((hasSubItems && !reallyCreate ) || isLazyLoad) ? new LazySubItemsInfo(parentItem, parentItem.SubItemsContentMode, subItems) : null);
            PrepareBarItemTag(menu, parentItem, level, dxGroup, lazyInfo);

            if (level == 0)
            {   // Události navazuji jen pro level = 0 = tlačítko Menu:
                menu.GetItemData += _BarMenu_GetItemData;
                menu.CloseUp += _BarMenu_CloseUp;

                // Pokud právě nyní generujeme BarItem Menu, jehož ItemId se shoduje s prvkem menu, který je připraven ke znovuotevření v proměnné _OpenItemBarMenu (identifikátor _OpenItemBarMenuItemId je shodný s aktuálním menu),
                //   pak se právě nyní provádí velký refresh (stránky / grupy), který zahodil dosavadní starý BarSubItem pro Menu a nyní se vygeneroval nový BarSubItem.
                // Znamená to, že si do proměnné _OpenItemBarMenu musíme uložit novou instanci (menu), protože ji zanedlouho budeme otevírat! Stará instance, uložená v _OpenItemBarMenu, je k nepoužití, protože je odebraná z Ribbonu...
                if (_OpenItemBarMenuItemId != null && _OpenItemBarMenu != null && _OpenItemBarMenuItemId == parentItem.ItemId)
                    _OpenItemBarMenu = menu;
            }
        }
        /// <summary>
        /// Vyvolá se pro menu (BarSubItem) před pokusem o jeho otevření. Tato metoda se volá i tehdy, když menu nemá žádnou položku (neotevřou se tedy tři tečky).
        /// Zde je možno menu naplnit, a otevře se; anebo spustit OnDemand donačtení a přípravu pro jeho otevření (inicializaci <see cref="_OpenItemBarMenu"/>), menu se otevře ihned jak přijdou data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BarMenu_GetItemData(object sender, EventArgs e)
        {
            if (!(sender is BarSubItem menu)) return;

            var mousePoint = Control.MousePosition;
            _OpenMenuReset(true);

            var itemInfo = menu.Tag as BarItemTagInfo;               // Do Menu jsme instanci BarItemTagInfo vytvořili při jeho tvorbě v metodě PrepareBarMenu()
            if (itemInfo != null && itemInfo.LazyInfo != null)
            {   // Pokud máme LazyInfo:
                var lazyInfo = itemInfo.LazyInfo;
                // OnDemand = položka se má donačíst ze serveru:
                bool isOnDemand = (lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
                // Budeme volat server? Ano když je OnDemand, ale ne když to máme přeskočit (je to potlačeno) a ne když jsme v Search textboxu (to by se rozjelo donačítání všech prvků):
                bool needRunOnDemandLoad = (isOnDemand && !_OpenItemBarMenuSkipRunOnDemandLoad && !IsSearchEditActive && !lazyInfo.RefreshPending);
                // Máme statická data?
                bool hasNewItems = (lazyInfo.SubItems != null);
                // Budeme generovat nové položky do menu? Ano, pokud je máme, a pokud NEbudeme žádat server o nové položky
                //  (kdybychom položky vytvořili, a následně volali server pro OnDemandLoad, pak bychom ty prvky z menu hned zase zahodili):
                bool needFillMenu = hasNewItems && !needRunOnDemandLoad;
                // Mohou nastat dvě situace:
                //  a) Máme nové prky do menu, a NEbudeme volat server = pak ty prvky uživateli nabídneme:
                if (needFillMenu)
                {   // 1. SubItems jsou deklarované přímo zde (Static), pak z nich vytvoříme nabídku a necháme ji uživateli zobrazit:
                    var startTime = DxComponent.LogTimeCurrent;
                    int count = 0;
                    _BarMenu_FillItems(menu, itemInfo.Level + 1, itemInfo.DxGroup, lazyInfo.ParentItem, lazyInfo.SubItems, true, ref count);
                    lazyInfo.SubItems = null;                        // SubItems, které jsme nyní vložili do menu, už příště nebudeme potřebovat. Jsou v menu, a dokud nepřijde Refresh, tak tam budou.
                    itemInfo.ValidMenu = ContentValidityType.Valid;  // Prvek nyní obsahuje validní menu
                    if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"LazyLoad Menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
                }
                //  b) Je třeba zavolat server, aby nám dal nové prvky do menu (eventu ItemOnDemandLoad => RefreshItems => _BarMenu_OpenMenu()),
                //      pak ale stávající prvky menu uživateli nezobrazíme:
                if (needRunOnDemandLoad)
                {   // 2. O prvky SubItems máme požádat server - na základě obsluhy eventu ItemOnDemandLoad; pak dojde k vyvolání zdejší metody _BarMenu_RefreshItems():
                    _OpenMenuLocation = mousePoint;
                    _OpenItemBarMenu = menu;
                    _OpenItemBarMenuItemId = itemInfo.Data.ItemId;

                    lazyInfo.PopupLocation = mousePoint;
                    lazyInfo.CurrentMergeLevel = this.MergeLevel;
                    lazyInfo.RefreshPending = true;
                    this.RunItemOnDemandLoad(lazyInfo.ParentItem);

                    // Tento kód zajistí, že vlastní menu nebude rozsvíceno - protože očekáváme dodání nového obsahu menu ze serveru (v RefreshMenu).
                    // Kdyby to tu nebylo, pak bude menu blikat (rozsvítil by se starý obsah a po refreshi by se aktivoval nový obsah).
                    if (menu.ItemLinks.Count > 0)
                    {
                        menu.ItemLinks.Clear();
                        itemInfo.ValidMenu = ContentValidityType.ReloadInProgress;       // Rozběhl se OnDemandLoad, menu dosud není naplněno, ale není nevalidní...
                    }
                }
                // Prvek již NENÍ OnDemand: odpojíme tedy jeho LazyInfo:
                if (!isOnDemand)
                    itemInfo.LazyInfo = null;                        // Data máme. Prvek již NENÍ OnDemand. Více již LazyInfo nebudeme potřebovat.

            }
        }
        /// <summary>
        /// Metoda zajistí vytvoření obsahu menu, buď z prvků již definovaných anebo si položky menu vyžádá prostřednictvím metody <see cref="RunItemOnDemandLoad(IRibbonItem)"/>.
        /// Tato metoda se volá výhradně při řešení LazyItems v rámci Application.Idle.
        /// </summary>
        private void _BarMenu_CreateItems(DevExpress.XtraBars.BarSubItem barMenu, BarItemTagInfo itemInfo)
        {
            if (barMenu is null || itemInfo is null || itemInfo.LazyInfo is null) return;

            var lazyInfo = itemInfo.LazyInfo;

            // OnDemand = položka se má donačíst ze serveru:
            bool isOnDemand = (lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || lazyInfo.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
            // Budeme volat server? Ano když je OnDemand, ale ne když to máme přeskočit (je to potlačeno) a ne když jsme v Search textboxu (to by se rozjelo donačítání všech prvků):
            bool needRunOnDemandLoad = (isOnDemand && !_OpenItemBarMenuSkipRunOnDemandLoad && !IsSearchEditActive && !lazyInfo.RefreshPending);
            // Máme statická data?
            bool hasNewItems = (lazyInfo.SubItems != null);
            // Budeme generovat nové položky do menu? Ano, pokud je máme, a pokud NEbudeme žádat server o nové položky
            //  (kdybychom položky vytvořili, a následně volali server pro OnDemandLoad, pak bychom ty prvky z menu hned zase zahodili):
            bool needFillMenu = hasNewItems && !needRunOnDemandLoad;
            // Mohou nastat dvě situace:
            //  a) Máme nové prky do menu, a NEbudeme volat server = pak ty prvky uživateli nabídneme:
            if (needFillMenu)
            {   // 1. SubItems jsou deklarované přímo zde (Static), pak z nich vytvoříme nabídku a necháme ji uživateli zobrazit:
                var startTime = DxComponent.LogTimeCurrent;
                int count = 0;
                _BarMenu_FillItems(barMenu, itemInfo.Level + 1, itemInfo.DxGroup, lazyInfo.ParentItem, lazyInfo.SubItems, true, ref count);
                lazyInfo.SubItems = null;                            // SubItems, které jsme nyní vložili do menu, už příště nebudeme potřebovat. Jsou v menu, a dokud nepřijde Refresh, tak tam budou.
                itemInfo.ValidMenu = ContentValidityType.Valid;      // Prvek nyní obsahuje validní menu
                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"LazyLoad Menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
            }
            //  b) Je třeba zavolat server, aby nám dal nové prvky do menu (eventu ItemOnDemandLoad => RefreshItems => _BarMenu_OpenMenu()),
            //      pak ale stávající prvky menu uživateli nezobrazíme:
            if (needRunOnDemandLoad)
            {   // 2. O prvky SubItems máme požádat server - na základě obsluhy eventu ItemOnDemandLoad; pak dojde k vyvolání zdejší metody _BarMenu_RefreshItems():
                lazyInfo.CurrentMergeLevel = this.MergeLevel;
                lazyInfo.RefreshPending = true;
                this.RunItemOnDemandLoad(lazyInfo.ParentItem);
                itemInfo.ValidMenu = ContentValidityType.ReloadInProgress;       // Rozběhl se OnDemandLoad, menu dosud není naplněno, ale není nevalidní...
            }
            // Prvek již NENÍ OnDemand: odpojíme tedy jeho LazyInfo:
            if (!isOnDemand)
                itemInfo.LazyInfo = null;                            // Data máme. Prvek již NENÍ OnDemand. Více již LazyInfo nebudeme potřebovat.

        }
        /// <summary>
        /// Při zavírání Bar menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BarMenu_CloseUp(object sender, EventArgs e)
        {
            if (sender is BarSubItem menu && menu.Tag is BarItemTagInfo itemInfo && itemInfo.Level == 0)
            {   // Menu se může zavírat obecně ze dvou důvodů:
                // 1. Uživatel něco vybral a akce se provádí, anebo uživatel nic nevybral a kliknul mimo a menu se zavírá => toto menu má tedy být úmyslně zavřené!
                //   anebo
                // 2. Menu je otevřené, ale přišel nám do Ribbonu Refresh nějakých dat, provádí se UnMerge - Modify - Merge, a při tom procesu se menu zavře automaticky => toto menu budeme chtít poté otevřít, a nesmíme jej tedy resetovat!
                if (!this.CurrentModifiedState)
                    _OpenMenuReset();

                // Pokud je menu v režimu OnDemandLoad*, tak při jeho zavírání odstraníme všechny jeho položky (ItemLinks) a zajistíme znovu vyvolání handleru GetItemData pro OnDemand donačtení při dalším otevírání menu:
                // Položky odstraňuji proto, aby při příští aktivaci menu se nerozsvěcovaly "ty staré" a pak po refreshi hned ty nově refreshované:
                bool isOnDemand = (itemInfo.Data != null && (itemInfo.Data.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce || itemInfo.Data.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime));
                if (isOnDemand)
                {
                    if (!this.CurrentModifiedState)        // V procesu Modify - Refresh to nedělám, pouze když uživatel ručně zhasíná menu (klikne do něj nebo mimo něj):
                    {
                        menu.ItemLinks.Clear();            // Tím při příštím rozsvícení bude menu prázdné a nebude blikat (Blikání = rozsvítí se nejprve starý obsah, provede se CallReload + Refresh + DoOpenMenu) ...
                        itemInfo.ValidMenu = ContentValidityType.ReloadInProgress;       // Rozběhl se OnDemandLoad, menu dosud není naplněno, ale není nevalidní...  Tím při Refreshi zajistím, že prvek bude vyhodnocen jako "změněný" a bude reálně refreshován.
                    }
                }
            }
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.BarSubItem"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu refreshe položky po ondemand donačtení obsahu.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="oldRibbonItem">Původní definiční objekt</param>
        /// <param name="newRibbonItem">Nový definiční objekt</param>
        private void _BarMenu_RefreshItems(DevExpress.XtraBars.BarSubItem menu, IRibbonItem oldRibbonItem, IRibbonItem newRibbonItem)
        {
            if (menu.Tag is BarItemTagInfo itemInfo)
            {
                // Do Menu vložíme dodané prvky (newRibbonItem.SubItems):
                int level = itemInfo.Level;
                int count = 0;
                _BarMenu_FillItems(menu, level + 1, itemInfo.DxGroup, newRibbonItem, newRibbonItem.SubItems, true, ref count);
               
                // Aktualizujeme info uložené v menu.Tag:
                itemInfo.Data = newRibbonItem;
                itemInfo.ValidMenu = ContentValidityType.Valid;                          // Menu je validně naplněno platnými daty
                bool isOnDemand = (newRibbonItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || newRibbonItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
                if (isOnDemand)
                    itemInfo.LazyInfo = new LazySubItemsInfo(newRibbonItem, newRibbonItem.SubItemsContentMode, null);             // Tady si už neukládám newRibbonItem.SubItems, protože položky menu jsou již vygenerovány...
                else
                    itemInfo.LazyInfo = null;
            }
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.BarSubItem"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu tvorby menu (při inicializaci nebo při BeforePopup v LazyInit modu)
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="clear"></param>
        /// <param name="count"></param>
        private void _BarMenu_FillItems(DevExpress.XtraBars.BarSubItem menu, int level, DxRibbonGroup dxGroup, IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, bool clear, ref int count)
        {
            if (clear)
                menu.ItemLinks.Clear();
            var menuItems = GetBarSubItems(parentItem, level, dxGroup, subItems, true, ref count);
            foreach (var menuItem in menuItems)
            {
                var menuLink = menu.AddItem(menuItem);
                if ((menuItem.Tag is BarItemTagInfo itemInfo) && itemInfo.Data.ItemIsFirstInGroup)
                    menuLink.BeginGroup = true;
            }
        }
        /// <summary>
        /// Metoda vytvoří a vrátí pole prvků <see cref="DevExpress.XtraBars.BarItem"/> pro daný prvek Parent a dané pole definic SubItems.
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="iSubItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem[] GetBarSubItems(IRibbonItem parentItem, int level, DxRibbonGroup dxGroup, IEnumerable<IRibbonItem> iSubItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (iSubItems != null && reallyCreate)
            {
                foreach (IRibbonItem subItem in iSubItems)
                {
                    var iSubItem = subItem;
                    RibbonItemType? forceItemType = GetValidSubItemType(iSubItem.ItemType);
                    DevExpress.XtraBars.BarItem barItem = PrepareItem(ref iSubItem, dxGroup, level, true, forceItemType, ref count);
                    iSubItem.ParentItem = parentItem;
                    iSubItem.ParentGroup = parentItem.ParentGroup;

                    if (barItem != null)
                    {
                        FillBarItem(barItem, iSubItem, level);
                        barItems.Add(barItem);
                    }
                }
            }
            return barItems.ToArray();
        }
        /// <summary>
        /// Metoda rozsvítí menu k danému prvku, optimálně k jeho linku na dané souřadnici
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="screenPoint"></param>
        private void _BarMenu_OpenMenu(DevExpress.XtraBars.BarSubItem menu, Point? screenPoint)
        {
            DevExpress.XtraBars.BarSubItemLink link = null;

            // Dostupné linky pro daný prvek menu:
            var links = menu?.Links
                .Where(l => l.Bounds.Width > 0)
                .OfType<DevExpress.XtraBars.BarSubItemLink>()
                .ToArray();

            if (links.Length > 0)
            {
                this.TopRibbonControl.Refresh();

                if (links.Length > 1 && screenPoint.HasValue)
                {   // Pokud máme více linků, a máme uloženu souřadnici, kde by měl být aktivní prvek, zkusíme jej najít:
                    var preferredLink = GetLinkAtPoint(links, screenPoint);
                    if (preferredLink != null)
                        // Metoda GetLinkAtPoint() mohla vrátit instanci, která není v našem seznamu, protože může hledat v celém Ribbonu podle souřadnice!
                        // Pokud tedy vrátila nějakou cizí (ne z našeho menu), tak ji raději zahodíme a použijeme nejnovější naši (za pár řádků dole):
                        link = links.FirstOrDefault(l => Object.ReferenceEquals(l, preferredLink));
                }

                if (link == null)
                    link = links[links.Length - 1];

                try
                {
                    _OpenItemBarMenuSkipRunOnDemandLoad = true;
                    link.OpenMenu();
                }
                finally
                {
                    _OpenItemBarMenuSkipRunOnDemandLoad = false;
                }
            }
        }
        /// <summary>
        /// Metoda vrátí Link, který se nachází na zadané souřadnici (default = aktuální pozice myši).
        /// Tato metoda má na vstupu absolutní souřadnice (Screen) nebo null = převezme aktuální pozici myši.
        /// Metoda pracuje s ribbonem <see cref="TopRibbonControl"/>, protože ten je fyzicky zobrazen a na něm se (asi) kliklo na určitý prvek.
        /// </summary>
        /// <param name="links"></param>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.BarItemLink GetLinkAtPoint(DevExpress.XtraBars.BarSubItemLink[] links, Point? screenPoint = null)
        {
            var point = screenPoint ?? Control.MousePosition;

            // Z Linků:
            if (links != null && links.Length > 0)
            {
                if (links.TryGetFirst(l => l.ScreenBounds.Contains(point), out var link))
                    return link;
                return links[links.Length - 1];
            }

            // Z Ribbonu:
            var topRibbon = TopRibbonControl;
            Point controlPoint = topRibbon.PointToClient(point);
            var hit = topRibbon.CalcHitInfo(controlPoint);
            return hit?.Item;
        }

        // PopupMenuDropDown
        private BarButtonItem CreatePopupMenuDropDown(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, bool createSubItems, ref int count)
        {
            var popup = DxPopupMenu.CreateDxPopupMenu(iRibbonItem.SubItems, null, null, this.Manager);
            popup.Ribbon = this;

            BarButtonItem barButtonItem = new BarButtonItem() 
            {
                ActAsDropDown = true, 
                ButtonStyle = DevExpress.XtraBars.BarButtonStyle.DropDown, 
                DropDownControl = popup
            };
            count++;
            return barButtonItem;
        }

        // Gallery
        /// <summary>
        /// Vytvoří a vrátí Galerii buttonů
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.RibbonGalleryBarItem CreateGalleryItem(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup)
        {
            var galleryBarItem = new DevExpress.XtraBars.RibbonGalleryBarItem(this.BarManagerInt);
            galleryBarItem.Gallery.Images = DxComponent.GetBitmapImageList(RibbonGalleryImageSize);
            galleryBarItem.Gallery.HoverImages = DxComponent.GetBitmapImageList(RibbonLargeImageSize);
            galleryBarItem.Gallery.AllowHoverImages = true;
            galleryBarItem.Gallery.ColumnCount = 4;
            galleryBarItem.SuperTip = DxComponent.CreateDxSuperTip(iRibbonItem);
            galleryBarItem.AllowGlyphSkinning = DefaultBoolean.True;
            galleryBarItem.Caption = iRibbonItem.Text;
            galleryBarItem.Enabled = iRibbonItem.Enabled;
            galleryBarItem.GalleryItemClick += GalleryBarItem_GalleryItemClick;

            // Galerie musí obsahovat grupy, ne prvky:
            var galleryGroup = new DevExpress.XtraBars.Ribbon.GalleryItemGroup();
            galleryBarItem.Gallery.Groups.Add(galleryGroup);

            // Teprve do grupy přidám prvky:
            List<DevExpress.XtraBars.Ribbon.GalleryItem> items = new List<DevExpress.XtraBars.Ribbon.GalleryItem>();
            int subLevel = level + 1;
            foreach (var subRibbonItem in iRibbonItem.SubItems)
                items.Add(CreateGallerySubItem(iRibbonItem, subRibbonItem, subLevel, dxGroup));

            galleryGroup.Items.AddRange(items.ToArray());

            return galleryBarItem;
        }
        /// <summary>
        /// Vytvoří a vrátí jeden prvek galerie
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.Ribbon.GalleryItem CreateGallerySubItem(IRibbonItem parentItem, IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup)
        {
            iRibbonItem.ParentItem = parentItem;
            iRibbonItem.ParentGroup = parentItem.ParentGroup;

            var galleryItem = new DevExpress.XtraBars.Ribbon.GalleryItem();
            galleryItem.ImageOptions.Image = iRibbonItem.Image;
            galleryItem.ImageIndex = DxComponent.GetBitmapImageIndex(iRibbonItem.ImageName, DxRibbonControl.RibbonGalleryImageSize);
            galleryItem.HoverImageIndex = DxComponent.GetBitmapImageIndex(iRibbonItem.ImageName, DxRibbonControl.RibbonLargeImageSize);
            galleryItem.Caption = iRibbonItem.Text;
            galleryItem.Checked = iRibbonItem.Checked ?? false;
            galleryItem.Description = iRibbonItem.ToolTipText;
            galleryItem.Enabled = iRibbonItem.Enabled;
            galleryItem.SuperTip = DxComponent.CreateDxSuperTip(iRibbonItem);
            PrepareBarItemTag(galleryItem, iRibbonItem, level, dxGroup, null);
            return galleryItem;
        }
        /// <summary>
        /// Click na prvek Gallery
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GalleryBarItem_GalleryItemClick(object sender, DevExpress.XtraBars.Ribbon.GalleryItemClickEventArgs e)
        {
            if (e.Item?.Tag is BarItemTagInfo itemInfo)
                this.RaiseRibbonItemClick(new DxRibbonItemClickArgs(itemInfo.Data));
        }

        // Combo
        /// <summary>
        /// Vytvoří a vrátí prvek Ribbonu, který reprezentuje ComboBox
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <returns></returns>
        private BarItem CreateComboListBoxItem(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup)
        {
            DxRibbonComboBox dxRibbonCombo = new DxRibbonComboBox();
            dxRibbonCombo.IRibbonItem = iRibbonItem;
            dxRibbonCombo.SelectedDxItemChanged += DxRibbonCombo_SelectedDxItemChanged;
            dxRibbonCombo.ComboButtonClick += DxRibbonCombo_ComboButtonClick;
            return dxRibbonCombo;
        }
        /// <summary>
        /// Výběr prvku v ComboBoxu na Ribbonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxRibbonCombo_SelectedDxItemChanged(object sender, DxRibbonItemClickArgs e)
        {
            _RibbonItem_ItemClick(sender, e);
        }
        /// <summary>
        /// Klik na button vedle ComboBoxu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxRibbonCombo_ComboButtonClick(object sender, DxRibbonItemButtonClickArgs e)
        {
            DxRibbonItemClickArgs args = new DxRibbonItemClickArgs(e.Item, e.ButtonName);
            _RibbonItem_ItemClick(sender, args);
        }

        // RepositoryEditor
        private BarEditItem CreateRepositoryEditorItem(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup)
        {
            // My v Ribbonu nevíme, jaký konkrétní RepositoryEditor si volající přeje. A ani nás to netrápí.
            // My ho skrz RepositoryEditorInfo požádáme o vygenerování jeho potřebné instance.
            var repoInfo = iRibbonItem.RepositoryEditorInfo;
            if (repoInfo is null) return null;
            if (repoInfo.RepositoryCreator is null) return null;

            BarEditItem barEdit = null;
            if (repoInfo.EditorCreator != null) barEdit = repoInfo.EditorCreator(iRibbonItem);
            if (barEdit is null) barEdit = new BarEditItem();
            repoInfo.EditorModifier?.Invoke(iRibbonItem, barEdit);

            DevExpress.XtraEditors.Repository.RepositoryItem repoItem = repoInfo.RepositoryCreator(iRibbonItem, barEdit);
            if (repoItem is null) return null;

            this.RepositoryItems.Add(repoItem);

            return barEdit;
        }

        /// <summary>
        /// Vrací true pokud se má objekt vytvořit
        /// </summary>
        /// <param name="changeMode"></param>
        /// <returns></returns>
        protected static bool HasCreate(ContentChangeMode changeMode) { return (changeMode != ContentChangeMode.Remove); }
        /// <summary>
        /// Vrací true pokud se má objekt vyprázdnit
        /// </summary>
        /// <param name="changeMode"></param>
        /// <returns></returns>
        protected static bool HasReFill(ContentChangeMode changeMode) { return (changeMode == ContentChangeMode.ReFill); }
        /// <summary>
        /// Vrací true pokud se má objekt smazat
        /// </summary>
        /// <param name="changeMode"></param>
        /// <returns></returns>
        protected static bool HasRemove(ContentChangeMode changeMode) { return (changeMode == ContentChangeMode.Remove); }
        /// <summary>
        /// Konvertuje typ <see cref="BarItemPaintStyle"/> na typ <see cref="DevExpress.XtraBars.BarItemPaintStyle"/>
        /// </summary>
        /// <param name="itemPaintStyle"></param>
        /// <returns></returns>
        internal static DevExpress.XtraBars.BarItemPaintStyle Convert(BarItemPaintStyle itemPaintStyle)
        {
            int styles = (int)itemPaintStyle;
            return (DevExpress.XtraBars.BarItemPaintStyle)styles;
        }
        /// <summary>
        /// Konvertuje typ <see cref="BarItemPaintStyle"/> na typ <see cref="DevExpress.XtraBars.BarItemPaintStyle"/>.
        /// Zohlední kompletní obsah prvku Ribbonu, nejen jeho PaintStyle.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal static DevExpress.XtraBars.BarItemPaintStyle ConvertPaintStyle(IRibbonItem iRibbonItem, int? level = null)
        {
            return ConvertPaintStyle(iRibbonItem, out bool _, level);
        }
        /// <summary>
        /// Konvertuje typ <see cref="BarItemPaintStyle"/> na typ <see cref="DevExpress.XtraBars.BarItemPaintStyle"/>.
        /// Zohlední kompletní obsah prvku Ribbonu, nejen jeho PaintStyle.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="hasImage"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal static DevExpress.XtraBars.BarItemPaintStyle ConvertPaintStyle(IRibbonItem iRibbonItem, out bool hasImage, int? level = null)
        {
            // Zjistím, zda pro prvek budeme mít Image:
            hasImage = iRibbonItem.Image != null || !String.IsNullOrEmpty(iRibbonItem.ImageName);
            if (!hasImage)
            {   // Přímo obrázek definován není. Může být generován náhradní podle Caption?
                string imageCaption = DxComponent.GetCaptionForRibbonImage(iRibbonItem, level);
                hasImage = !String.IsNullOrEmpty(imageCaption);
            }

            switch (iRibbonItem.ItemPaintStyle)
            {   // Pokud máme Image, pak výstupní PaintStyle bude odpovídat požadavku. Pokud Image nemáme, pak výstupem bude Caption:
                case BarItemPaintStyle.Standard: return (hasImage ? DevExpress.XtraBars.BarItemPaintStyle.Standard : DevExpress.XtraBars.BarItemPaintStyle.Caption);
                case BarItemPaintStyle.Caption: return DevExpress.XtraBars.BarItemPaintStyle.Caption;
                case BarItemPaintStyle.CaptionGlyph: return (hasImage ? DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph : DevExpress.XtraBars.BarItemPaintStyle.Caption);
                case BarItemPaintStyle.CaptionInMenu: return (hasImage ? DevExpress.XtraBars.BarItemPaintStyle.CaptionInMenu : DevExpress.XtraBars.BarItemPaintStyle.Caption);
            }
            return Convert(iRibbonItem.ItemPaintStyle);
        }
        /// <summary>
        /// Konvertuje typ <see cref="RibbonItemStyles"/> na typ <see cref="DevExpress.XtraBars.Ribbon.RibbonItemStyles"/>
        /// </summary>
        /// <param name="ribbonStyle"></param>
        /// <returns></returns>
        internal static DevExpress.XtraBars.Ribbon.RibbonItemStyles Convert(RibbonItemStyles ribbonStyle)
        {
            int styles = (int)ribbonStyle;
            return (DevExpress.XtraBars.Ribbon.RibbonItemStyles)styles;
        }
        /// <summary>
        /// Zajistí vytvoření instance <see cref="BarItemTagInfo"/> do daného prvku do jeho Tagu.
        /// Stávající Tag může být null, nebo může obsahovat instanci <see cref="LazySubItemsInfo"/> (ta bude převzata) anebo již může obsahovat instanci <see cref="BarItemTagInfo"/>.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        protected void PrepareBarItemTag(BarItem barItem, IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup)
        {
            if (barItem == null) return;

            if (barItem.Tag is BarItemTagInfo itemInfo)              // V Tagu už možná je BarItemTagInfo ? Jen do ní vložím IRibbonItem, ale neměním grupu !
                itemInfo.Data = iRibbonItem;
            else
            {
                LazySubItemsInfo lazyInfo = barItem.Tag as LazySubItemsInfo;   // V Tagu může být připravená LazyInfo, zachovám ji - ale bude umístěna do ItemInfo.
                barItem.Tag = new BarItemTagInfo(iRibbonItem, level, this, dxGroup, lazyInfo);
            }
        }
        /// <summary>
        /// Zajistí vytvoření instance <see cref="BarItemTagInfo"/> do daného prvku do jeho Tagu.
        /// Stávající Tag bude vždy přepsán nově dodanými daty.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="lazyInfo"></param>
        protected void PrepareBarItemTag(BarItem barItem, IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, LazySubItemsInfo lazyInfo)
        {
            if (barItem != null)
                barItem.Tag = new BarItemTagInfo(iRibbonItem, level, this, dxGroup, lazyInfo);
        }
        /// <summary>
        /// Zajistí vytvoření instance <see cref="BarItemTagInfo"/> do daného prvku do jeho Tagu.
        /// Stávající Tag bude vždy přepsán nově dodanými daty.
        /// </summary>
        /// <param name="galleryItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="lazyInfo"></param>
        protected void PrepareBarItemTag(GalleryItem galleryItem, IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, LazySubItemsInfo lazyInfo)
        {
            if (galleryItem != null)
                galleryItem.Tag = new BarItemTagInfo(iRibbonItem, level, this, dxGroup, lazyInfo);
        }
        /// <summary>
        /// Zajistí vytvoření instance <see cref="BarItemTagInfo"/> do daného prvku do jeho Tagu.
        /// </summary>
        /// <param name="popupMenu"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="lazyInfo"></param>
        protected void PrepareBarItemTag(DevExpress.XtraBars.PopupMenu popupMenu, IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, LazySubItemsInfo lazyInfo)
        {
            if (popupMenu != null)
                popupMenu.Tag = new BarItemTagInfo(iRibbonItem, level, this, dxGroup, lazyInfo);
        }
        /// <summary>
        /// Do Tagu daného prvku vloží nová definiční data
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        protected void RefreshBarItemTag(BarItem barItem, IRibbonItem iRibbonItem)
        {
            iRibbonItem.RibbonItem = barItem;
            if (barItem == null) return;
            if (barItem.Tag is BarItemTagInfo itemInfo)
            {
                if (iRibbonItem.ParentGroup == null && itemInfo.Data != null && itemInfo.Data.ParentGroup != null)
                    // Pokud dodaná definice prvku iRibbonItem neobsahuje vztah na definující grupu (iRibbonItem.ParentGroup), ale stávající data (v barItem.Tag) mají definici obsahující grupu,
                    //  pak tuto grupu převezmu:
                    iRibbonItem.ParentGroup = itemInfo.Data.ParentGroup;
                itemInfo.Data = iRibbonItem;
            }
        }
        /// <summary>
        /// Do Tagu daného prvku vloží nová definiční data
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="lazyInfo"></param>
        protected void RefreshBarItemTag(BarItem barItem, LazySubItemsInfo lazyInfo)
        {
            if (barItem == null) return;
            if (barItem.Tag is BarItemTagInfo itemInfo)
                itemInfo.LazyInfo = lazyInfo;
        }
        /// <summary>
        /// Instance této třídy je uložena v každém vygenerovaném BarItemu.
        /// Obsahuje: data <see cref="IRibbonItem"/>, podle kterých byl prvek vytvořen, obsahuje případné <see cref="LazySubItemsInfo"/>, 
        /// obsahuje Weak referenci na grupu <see cref="DxRibbonGroup"/> do které nativně prvek patří
        /// </summary>
        protected class BarItemTagInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="data"></param>
            /// <param name="level"></param>
            /// <param name="dxRibbon"></param>
            /// <param name="dxGroup"></param>
            /// <param name="lazyInfo"></param>
            public BarItemTagInfo(IRibbonItem data, int level, DxRibbonControl dxRibbon, DxRibbonGroup dxGroup, LazySubItemsInfo lazyInfo = null)
            {
                this.Data = data;
                this.ValidMenu = ContentValidityType.Valid;                    // V konstruktoru nastavuji validitu true
                this.Level = level;
                this._DxRibbon = dxRibbon;
                this._DxGroup = dxGroup;
                this.LazyInfo = lazyInfo;
            }
            private WeakTarget<DxRibbonControl> _DxRibbon;
            private WeakTarget<DxRibbonGroup> _DxGroup;
            /// <summary>
            /// Data která prvek definují
            /// </summary>
            internal IRibbonItem Data { get; set; }
            /// <summary>
            /// Obsah menu je validní nebo se právě donačítá jeho obsah?
            /// <para/>
            /// Prvek může mít menu validní (pokud při vytvoření je dodán platný obsah), pak je zde <see cref="ContentValidityType.Valid"/>;
            /// Nebo je dodán příznak NeedReload, pak po vytvoření je zde <see cref="ContentValidityType.NonValid"/> (a menu může obsahovat jen náhradní prvek, který dovolí systému zobrazit menu), 
            /// pak při pokusu o jeho rozbalení proběhne událost OnDemandLoad a stav se změní na <see cref="ContentValidityType.ReloadInProgress"/>.
            /// <para/>
            /// Při vyhledávání dat prvku se validita menu vyhodnocuje, viz metoda <see cref="TryGetIRibbonData(string, out IRibbonItem, out bool, bool)"/>...
            /// </summary>
            internal ContentValidityType ValidMenu { get; set; }
            /// <summary>
            /// Hladina prvku
            /// </summary>
            internal int Level { get; private set; }
            /// <summary>
            /// Grupa, do které byl prvek vytvořen
            /// </summary>
            internal DxRibbonGroup DxGroup { get { return _DxGroup?.Target; } }
            /// <summary>
            /// Page, do které patří Grupa, do které byl prvek vytvořen
            /// </summary>
            internal DxRibbonPage DxPage { get { return DxGroup?.OwnerDxPage; } }
            /// <summary>
            /// Ribbon, do něhož patří zdejší prvek. I pokud <see cref="DxGroup"/> a <see cref="DxPage"/> je null, pak zdejší odkaz může být platý = pro přímé QAT prvky.
            /// </summary>
            internal DxRibbonControl DxRibbon { get { return _DxRibbon?.Target; } }
            /// <summary>
            /// Informace pro LazyLoad
            /// </summary>
            internal LazySubItemsInfo LazyInfo { get; set; }
        }
        /// <summary>
        /// Třída pro uchování informací pro LazyLoad SubItems
        /// </summary>
        protected class LazySubItemsInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="parentItem"></param>
            /// <param name="subItemsContentMode">Režim subpoložek</param>
            /// <param name="subItems"></param>
            public LazySubItemsInfo(IRibbonItem parentItem, RibbonContentMode subItemsContentMode, IEnumerable<IRibbonItem> subItems)
            {
                this.ParentItem = parentItem;
                this.SubItemsContentMode = subItemsContentMode;
                this.SubItems = subItems;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"LazySubItems Parent: {ParentItem}; SubItems: {(SubItems?.Count().ToString() ?? "NULL")}";
            }
            /// <summary>
            /// Prvek definující Button / SplitButton / Menu = Parent aktuálního menu
            /// </summary>
            public IRibbonItem ParentItem { get; private set; }
            /// <summary>
            /// Režim subpoložek
            /// </summary>
            public RibbonContentMode SubItemsContentMode { get; private set; }
            /// <summary>
            /// Právě probíhá Refresh prvků menu: byl vydán požadavek na refresh, ale dosud nebyla dodána data.
            /// Na true se nastavuje při otevírání menu a při LazyLoad, na false pak při Refresh obsahu prvku.
            /// Obě metody probíhají v GUI threadu.
            /// </summary>
            public bool RefreshPending { get; set; }
            /// <summary>
            /// SubPrvky
            /// </summary>
            public IEnumerable<IRibbonItem> SubItems { get; set; }
            /// <summary>
            /// Aktivátor menu
            /// </summary>
            public DevExpress.XtraBars.BarItemLink Activator { get; set; }
            /// <summary>
            /// Souřadnice pro zobrazení Popup
            /// </summary>
            public Point? PopupLocation { get; set; }
            /// <summary>
            /// Úroveň mergování v době zobrazení Popup. Pokud je větší než 0 (jsme někam Mergování), chová se zobrazení Popup jinak.
            /// </summary>
            public int? CurrentMergeLevel { get; set; }
        }
        /// <summary>
        /// Připraví BarManager
        /// </summary>
        private void InitBarManagers()
        {
            _InitializeBarManager(this.Manager);
        }
        /// <summary>
        /// Nastaví standardní vlastnosti do <paramref name="barManager"/>
        /// </summary>
        /// <param name="barManager"></param>
        private static void _InitializeBarManager(RibbonBarManager barManager)
        {
            barManager.AllowMoveBarOnToolbar = true;
            barManager.AllowQuickCustomization = true;
            barManager.AllowShowToolbarsPopup = true;
            barManager.PopupMenuAlignment = PopupMenuAlignment.Left;
            barManager.PopupShowMode = PopupShowMode.Classic;
            barManager.ShowScreenTipsInMenus = true;
            barManager.ShowScreenTipsInToolbars = true;
            barManager.ShowShortcutInScreenTips = true;
            barManager.ToolTipAnchor = DevExpress.XtraBars.ToolTipAnchor.Cursor;

            barManager.UseAltKeyForMenu = true;
        }
        /// <summary>
        /// BarManager interní, vestavěný v Ribbonu, pro běžné použití
        /// </summary>
        public override RibbonBarManager Manager { get { return base.Manager; } }
        /// <summary>
        /// BarManager interní, vestavěný v Ribbonu, pro běžné použití
        /// </summary>
        public RibbonBarManager BarManagerInt { get { return base.Manager; } }
        /// <summary>
        /// Stav obsahu prvku
        /// </summary>
        protected enum ContentValidityType
        {
            /// <summary>
            /// Nevalidní (nenaplněn nebo zcela invalidován), nemá být zobrazen, obsah je třeba donačíst
            /// </summary>
            NonValid,
            /// <summary>
            /// Právě byl vyžádán reload prvku, ale dosud nebyl naplněn
            /// </summary>
            ReloadInProgress,
            /// <summary>
            /// Prvek je naplněn a je validní
            /// </summary>
            Valid
        }
        #endregion
        #region Podpora pro QAT - Quick Access Toolbar
        /*     Jak to tady funguje?

         A. Vstup z aplikačního kódu
          1. Aplikační kód nejprve vloží hodnotu do QATItemKeys (kde ji vzal, uvidíme na konci)
              - zdejší kód si dodaný text rozebere, analyzuje, a vytvoří si prvky typu QatItem do seznamu _QatItems a do Dictionary _QatItemDict
              - prvky v tuto chvíli mají pouze stringový klíč, a očekávají dodání fyzického objektu pro zobrazení
          2. Teprve poté Aplikační kód vloží definici stránek Ribbonu (metoda AddPages())
              - v procesu přidávání dat se zjistí, zda právě přidaný prvek má být obsažen v QAT (metodou DefinedInQAT(string)), 
                a pokud ano, pak se fyzický prvek (BarItem) připojí do odpovídající instance QatItem (metoda AddBarItemToQatList()), ale to stále ještě není fyzicky v ToolBaru
          3. Až skončí přidávání stránek (v metodě AddPages()), vyvolá se metoda AddQatListToRibbon(), která zajistí, že patřičné prvky se fyzicky objeví v ToolBaru
              - patřičné = pouze ty, pro které je vygenerován fyzický control BarItem
              - ve správném pořadí = podle pořadí v Listu _QatItems (ten vzniknul v pořadí podle dodané definice do QATItemKeys)
              - Obsah ToolBaru je vyprázdněn před tímto naplněním, a stávající Linky (objekt BarItemLink spojující fyzický BarItem s ToolBarem) jsou odstraněny a Disposovány
              - Může se stát, že v evidenci (list _QatItems) budeme mít nějaké prvky, k nimž dosud nebyl vytvořen fyzický BarItem: takové prvky do ToolBaru nevložíme.
                 - ale pokud později dorazí další stránka, obsahující prvek tohoto jména, proběhne tedy bod 2 + 3, 
                    fyzický prvek BarItem se vytvoří, zařadí se do (podle jména odpovídající) instance QatItem, a následně se zobrazí v ToolBaru, právě na tom pořadí, kde byl definován.

         B. Interaktivní práce s uživatelem
          1. Uživatel přidá nějaký prvek do ToolBaru, nebo nějaký odebere, nebo přemístí:
              - Vyvolá se odpovídající událost
              - Daný prvek se přidá nebo odebere do/z seznamu _QatItems a Dictionary _QatItemDict
              - Přidání do seznamu zařadí prvek na odpovídající pozici (před/za okolní existující prvky)
              - Vyvolá se událost QATItemKeysChanged
          2. Aplikační kód reaguje na událost QATItemKeysChanged
              - Převezme si hodnotu QATItemKeys
                 tato hodnota obsahuje souhrn klíčů přidaných v bodě 1, plus prvek / prvky interaktivně přidané nebo mínus prvky odebrané, 
                 obsahuje tedy i klíče prvků, které dosud nejsou v ToolBaru fyzicky zobrazeny (protože dosud nebyl vygenerován jejich BarItem) - neviditelný prvek nelze odebrat, dokud nebude fyzicky zobrazen
              - Tuto hodnotu si někam uloží do konfigurace
              - Při příštím otevření menu tuto hodnotu vloží do Ribbonu - viz bod 1

         C. Interaktivita a Mergované Ribbony + QAT Toolbary (pro Slave + Master Ribbony)
          1. Ribbon, který má vygenerované nějaké QAT prvky, může být Mergován nahoru = jeho QAT se promítnou do Toolbaru nadřazeného Ribbonu, a po UnMerge se zase vrátí do lokálního Toolbaru;
          2. Prvky ze Slave Ribbonu lze do QAT přidat i odebrat ručně (uživatelem) i tehdy, když je Slave Ribbon mergován do nadřazeného Ribbonu:
              - Metoda OnAddToToolbar() => UserAddItemToQat() proběhne v instanci nadřazeného (Master) Ribbonu;
              - Stejně tak OnRemoveFromToolbar() => UserRemoveItemFromQat() proběhne v instanci nadřazeného (Master) Ribbonu;
              - Přidaný prvek ze Slave Ribbonu se samozřejmě objeví v Toolbaru nadřazeného Ribbonu;
              - Ke cti DevExpress je nutno uznat, že po UnMerge toho Ribbonu se nově přidaný Slave prvek z toolbaru Master Ribbonu odebere a objeví se v toolbaru Slave Ribbonu = očekávaný a správný výsledek;
              - Zdejší metody pracují s evidencí QAT prvků na úrovni DxRibbonControl, a QAT prvky by tedy nejprve najít instanci toho Ribbonu, ve kterém jsou nativně definovány, a nikoli toho Ribbonu, kde se akce děje
              - Tento přechod (z this instance do instance, která reálně definuje svoje vlastní QAT prvky) zajišťuje metoda TryGetIRibbonData()

         D. Explicitní obsah QAT = prvky mimo Ribbon
          1. Lze je deklarovat shodně jako jiné Items = jako prvky typu IRibbonItem
          2. Vkládají se do property QATDirectItems
         
        */
        #region Základní evidence prvků QAT : string QATItemKeys, List a Dictionary, konverze
        /// <summary>
        /// Inicializace dat a eventhandlerů pro QAT
        /// </summary>
        private void InitQuickAccessToolbar()
        {
            _QATUserItems = new List<QatItem>();
            _QATUserItemDict = new Dictionary<string, QatItem>();

            this.SourceToolbar.LinksChanged += SourceToolbar_LinksChanged;

            // this.ShowToolbarCustomizeItem = false;   // Zobrazit malou šipku dolů v Toolbaru ?
            this.CustomizeQatMenu += DxRibbonControl_CustomizeQatMenu;
            this.ToolbarLocationChanged += DxRibbonControl_ToolbarLocationChanged;

            // Pozor, po každém mergování / unmergování do this ribbonu se vytvoří nové menu CustomizationPopupMenu, takže je třeba znovunavázat eventhandler:
            this.CustomizationPopupMenuRefreshHandlers();

            this._SetQATUserItemKeys(false);
        }
        /// <summary>
        /// Událost, kdy někdě došlo ke změně prvků QAT.
        /// Tuto metodu vyvolá event <see cref="DxQuickAccessToolbar.ConfigValueChanged"/>.
        /// <para/>
        /// Mohlo to být ze serveru (poslal data) anebo uživatel v některém Ribbonu přidal / odebral prvek v QAT.
        /// Mohl to být jiný Ribbon, anebo i náš Ribbon - všechny změny se posílají do <see cref="DxQuickAccessToolbar.QATItems"/> anebo <see cref="DxQuickAccessToolbar.QATLocation"/>
        /// a tamodtud se posílá událost o změně všem živým Ribbonům.
        /// <para/>
        /// Pokud to byl náš Ribbon, pak ten si před zveřejněním nového stavu (soupis QAT prvků) do <see cref="DxQuickAccessToolbar.QATItems"/> tutéž hodnotu uložil interně do <see cref="_QATLocalConfigValue"/>,
        /// tedy na změnu zvenku nebude reagovat protože pro něj nedošlo ke změně.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxQATItemKeysChanged(object sender, EventArgs e)
        {
            string newKeys = DxQuickAccessToolbar.ConfigValue;
            string oldKeys = this._QATLocalConfigValue;              // Pokud jsem já (this, prostřednictvím uživatele) provedl změnu v QAT prvcích, pak jsem si nový stav uložil sem, a pak jsem nový stav poslal do DxQuickAccessToolbar.QATItemKeys
            if (String.Equals(newKeys, oldKeys)) return;             //  => tedy pro mě ke změně nedošlo.
            this._SetQATUserItemKeys(true);                          // Změnu způsobil jiný Ribbon anebo server = poslal nová data
        }
        /// <summary>
        /// Metoda vrátí platný QAT klíč, pro použití v Dictionary.
        /// Vrácený klíč není NULL, je Trim. Jsou odstraněny nepatřičné výskyty delimiteru. Není změněna velikost písmen.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        protected static string GetValidQATKey(IRibbonItem ribbonItem)
        {
            string itemId = GetRibbonQATKey(ribbonItem);
            return DxQuickAccessToolbar.GetValidQATKey(itemId);
        }
        /// <summary>
        /// Metoda vrátí QAT klíč pro daný prvek Ribbonu.
        /// Vrácený klíč není NULL, ale může být "".
        /// Neprošel metodou <see cref="DxQuickAccessToolbar.GetValidQATKey(string)"/>.
        /// Pokud na výstupu je "", pak prvek nemá být přítomen v QAT Ribbonu.
        /// Metoda vrátí buď <see cref="IRibbonItem.QatKey"/> (pokud není NULL), anebo základní <see cref="ITextItem.ItemId"/>.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        protected static string GetRibbonQATKey(IRibbonItem ribbonItem)
        {
            if (ribbonItem is null) return "";
            if (ribbonItem.QatKey != null) return ribbonItem.QatKey;
            return ribbonItem.ItemId ?? "";
        }
        /// <summary>
        /// Metoda vrátí platný QAT klíč, pro použití v Dictionary.
        /// Vrácený klíč není NULL, je Trim. Jsou odstraněny nepatřičné výskyty delimiteru. Není změněna velikost písmen.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        protected static string GetValidQATKey(string itemId)
        {
            return DxQuickAccessToolbar.GetValidQATKey(itemId);
        }
        /// <summary>
        /// Resetuje pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/> (tj. provede Reset jejich prvků <see cref="QatItem.Reset()"/> a následně Clear Listu a Dictionary);
        /// odebere každý prvek Link z reálného toolbaru QAT (pokud tam je) = vyprázdní se pole i toolbar.
        /// <para/>
        /// Používá se při setování nových dat z <see cref="DxQuickAccessToolbar"/>.
        /// </summary>
        private void ResetQATUserItems()
        {
            _QATUserItems.ForEachExec(q => q.Reset());
            _QATUserItems.Clear();
            _QATUserItemDict.Clear();
            _QATLocalConfigValue = null;
        }
        /// <summary>
        /// Ze stringu v singletonu <see cref="DxQuickAccessToolbar"/> vytvoří struktury pro evidenci prvků pro toolbar QAT (pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/>).
        /// Před tím zruší obsah fyzického QAT. Volitelně na konci znovu naplní fyzický QAT.
        /// </summary>
        /// <param name="refreshToolbar"></param>
        /// <param name="force"></param>
        private void _SetQATUserItemKeys(bool refreshToolbar = false, bool force = false)
        {
            string[] qatKeys = DxQuickAccessToolbar.QATItems;
            if (qatKeys == null) return;

            // Lokální cache:
            _QATLocalConfigValue = DxQuickAccessToolbar.ConfigValue;

            // Zjistíme, zda nová data reálně povedou ke změně obsahu v this Ribbonu (tzn. zda reálně budeme odstraňovat / přidávat naše reálné prvky).
            //   Totiž, náš Ribbon nemusí fyzicky obsahovat právě ty prvky, které jsou přidané nebo odebrané.
            //   Pak nemá význam změnu provádět!
            var qatBarItems = this.ItemsByQat;
            if (force || _ContainsRealChangeQatUserKeys(qatKeys, qatBarItems))
                // Pokud tedy dojde ke změně, pak se musíme unmergovat, změnit a zpět mergovat:
                this._UnMergeModifyMergeCurrentRibbon(() => _SetQATUserItemKeysReal(qatKeys, qatBarItems, refreshToolbar), true);
            else
                // Nejde o vizuální změnu, v tom případě pouze aktualizujeme obsah v datových strukturách (_QATUserItems a _QATUserItemDict):
                _SetQATUserItemKeysData(qatKeys);

            // Upravíme umístění QAT:
            var qatLocation = DxQuickAccessToolbar.QATLocation;
            if (this.ToolbarLocation != qatLocation)
                this.ToolbarLocation = qatLocation;
        }
        /// <summary>
        /// Metoda zjistí, zda aktuální platná data <see cref="DxQuickAccessToolbar.QATItems"/> (obsahující klíče QAT User prvků) reálně povedou k nějaké změně v aktuálním Ribbonu.
        /// Pokud tedy aktuálně data obsahují některý prvek, který aktuálně nemáme v QAT a přitom jej máme v Items, pak jde o změnu (budeme jej přidávat).
        /// Nebo naopak, pokud máme nějaký prvek fyzicky v QAT, ale není uveden v datech, pak jde o změnu (budeme jej odebírat).
        /// Detekujeme i změnu pořadí (prvek sice máme, ale v jiném místě, než jej očekáváme).
        /// <para/>
        /// Vrací true = jsou tu změny / false = beze změn.
        /// </summary>
        /// <param name="qatKeys">Nově platné klíče prvků</param>
        /// <param name="qatBarItems">Aktuálně přítomné BarItemy + RibbonItemy v this Ribbonu, klíčem je QatKey. Nesmí být null !!!</param>
        /// <returns></returns>
        private bool _ContainsRealChangeQatUserKeys(string[] qatKeys, Dictionary<string, Tuple<BarItem, IRibbonItem>> qatBarItems)
        {
            Queue<QatItem> qatQueue = new Queue<QatItem>();                    // Fronta prvků QAT User, které my reálně máme v QAT, v jejich aktuálním pořadí
            if (_QATUserItems != null)
                _QATUserItems.Where(q => q.IsInQAT).ForEachExec(i => qatQueue.Enqueue(i));

            // Tento algoritmus provede kontrolu, zda požadované prvky (qatKeys), jejich podmnožina reálně existující v this Ribbonu,
            //   je/není obsažena v našem reálném QAT v odpovídajícím pořadí.
            // Vrátíme true = je reálná změna / false = bez reálné změny
            foreach (string qatKey in qatKeys)
            {
                string key = GetValidQATKey(qatKey);
                if (key == "") continue;
                if (!qatBarItems.TryGetValue(key, out var barData)) continue;  // Prvek daného klíče v našem Ribbonu není, nemůžeme jej ani přidat ani odebrat - nejde o změnu.

                // a) Pokud požadovaný QAT prvek [itemId] v našem Ribbonu fyzicky máme:
                //  => Měli bychom jej mít i v naší frontě viditelných prvků:
                if (qatQueue.Count == 0) return true;                          // Naše reálné QAT prvky už nic neobsahují => musíme prvek přidat, jde tedy o změnu
                var qatItem = qatQueue.Dequeue();                              // Nějaký prvek tam máme, musí mít shodný klíč:
                if (qatItem.QatKey != key) return true;                        // Ale ten prvek, který máme na této pozici, má jiný klíč - jde o změnu.
            }

            return (qatQueue.Count > 0);                                       // Pokud v našem soupisu máme nějaké další prvky, musíme je odstranit - jde o změnu.
        }
        /// <summary>
        /// Ze zadaného stringu vytvoří struktury pro evidenci prvků pro toolbar QAT (pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/>).
        /// Před tím zruší obsah fyzického QAT. Volitelně na konci znovu naplní fyzický QAT.
        /// </summary>
        /// <param name="qatKeys">Nově platné klíče prvků</param>
        /// <param name="qatBarItems">Aktuálně přítomné BarItemy + RibbonItemy v this Ribbonu, klíčem je QatKey</param>
        /// <param name="refreshToolbar"></param>
        private void _SetQATUserItemKeysReal(string[] qatKeys, Dictionary<string, Tuple<BarItem, IRibbonItem>> qatBarItems, bool refreshToolbar = false)
        {
            ResetQATUserItems();

            foreach (string qatKey in qatKeys)
            {
                string key = GetValidQATKey(qatKey);
                if (key == "") continue;
                if (_QATUserItemDict.ContainsKey(key)) continue;               // Duplicita na vstupu: ignorujeme

                QatItem qatItem = new QatItem(this, key);
                if (refreshToolbar && qatBarItems != null && qatBarItems.TryGetValue(key, out var barData))
                {
                    qatItem.BarItem = barData.Item1;
                    qatItem.RibbonItem = barData.Item2;
                    qatItem.RibbonGroup = barData.Item2.ParentGroup;
                }
                _QATUserItems.Add(qatItem);
                _QATUserItemDict.Add(key, qatItem);
            }

            // Fyzická tvroba obsahu QAT:
            if (refreshToolbar) AddQATUserListToRibbon(qatBarItems);


            //// Změna klíčů zdejšího proti veřejnému?
            //string localKeys = _GetQATUserItemKeys();
            //string publicKeys = DxQuickAccessToolbar.QATItemKeys;
            //if (!String.Equals(localKeys, publicKeys))
            //    DxQuickAccessToolbar.QATItemKeys = localKeys;        // Tady dojde k eventu DxQuickAccessToolbar.QATItemKeysChanged, a vyvolá se zdejší handler _DxQATItemKeysChanged. Ale protože ten porovnává DxQuickAccessToolbar.QATItemKeys se zdejší hodnotou _QATUserItemKeys, k další změně už zde nedojde.
        }
        /// <summary>
        /// Naplní <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/> ze stávajích instancí, v pořadí dle dodaného stringu
        /// </summary>
        /// <param name="qatKeys"></param>
        private void _SetQATUserItemKeysData(string[] qatKeys)
        {
            var oldDict = _QATUserItemDict;
            var newList = new List<QatItem>();
            var newDict = new Dictionary<string, QatItem>();

            foreach (string qatKey in qatKeys)
            {
                string key = GetValidQATKey(qatKey);
                if (key == "") continue;
                if (newDict.ContainsKey(key)) continue;              // Duplicita na vstupu: ignorujeme
                bool oldExists = oldDict.TryGetValue(key, out var qatItem);
                if (!oldExists) qatItem = new QatItem(this, key);    // Toto by měly být pouze takové nové prvky, které nejsou přítomny v this Ribbonu. To ověřila metoda _ContainsRealChangeQatUserKeys().
                newList.Add(qatItem);                                // Do nového seznamu přidáváme prvky ve správném pořadí = zařazujeme do něj "cizí" prvky (které u nás nejsou)
                newDict.Add(key, qatItem);
            }

            _QATUserItems = newList;
            _QATUserItemDict = newDict;
        }
        /// <summary>
        /// Souhrn zdejších klíčů QAT Items, zapamatovaný při poslední změně.
        /// </summary>
        private string _QATLocalConfigValue;
        private List<QatItem> _QATUserItems;
        private Dictionary<string, QatItem> _QATUserItemDict;
        /// <summary>
        /// Příznak, že došlo ke změně obsahu QAT prvků v this Ribbonu, v době kdy tento Ribbon byl mergován do Parent Ribbonu.
        /// V takovém případě má DevExpress problém v tom, že Linky na naše prvky (Items) přidal do Parent Ribbonu a po odmergování this Ribbonu se tyto linky nenávratně ztratí.
        /// DevExpress je nepřidá zpátky do QAT Toolbaru v this Ribbonu, a nezůstanou ani v Parent Ribbonu.
        /// <para/>
        /// Po provedení UnMerge pro this Ribbon (kdy this je odmergovaný Child) si this Ribbon musí refreshovat svůj QAT ze společné základny.
        /// </summary>
        private bool _HasQATUserChangedItems;
        /// <summary>
        /// Dictionary obsahující aktuálně přítomné prvky Ribbonu <see cref="BarItem"/>, a jejich odpovídající data <see cref="IRibbonItem"/>,
        /// a kde klíčem je jejich QatKey.
        /// Získání této Dictionary není zadarmo, stojí nějaký čas, proto si výsledek pro opakované používání uložme.
        /// Vychází ze s kolekce Items.
        /// Pokud existuje více BarItemů s různým ItemId ale shodným QatKey, pak je zde ten první.
        /// Pokud prvek Ribbonu nemá svůj <see cref="IRibbonItem"/>, pak zde není = nelze určit jeho QatKey.
        /// </summary>
        internal Dictionary<string, Tuple<BarItem, IRibbonItem>> ItemsByQat
        {
            get
            {
                var result = new Dictionary<string, Tuple<BarItem, IRibbonItem>>();
                var barItems = this.Items;
                var count = barItems.Count;
                for (int i = 0; i < count; i++)
                { 
                    var barItem = barItems[i];
                    if (_TryGetIRibbonData(barItem, out var qatKey, out var ribbonItem, out var _) && qatKey != null && !result.ContainsKey(qatKey))
                        result.Add(qatKey, new Tuple<BarItem, IRibbonItem>(barItem, ribbonItem));
                }
                return result;
            }
        }
        #endregion
        #region Tvorba QAT na základě zadání z aplikace, podpora pro tvorbu QAT spojená s tvorbou prvků Ribbonu
        /// <summary>
        /// Vrátí true, pokud daná stránka obsahuje něco, co bude přidáno do QAT (Quick Access Toolbar)?
        /// Pokud ano, bude třeba do dané stránky připravit alespoň ty grupy a prvky, které k danému QAT prvku vedou, aby bylo co přidat do QAT.
        /// <para/>
        /// Do toolbaru QAT se mohou přidat pouze prvky, které existují někde v rámci Ribbonu. Nemohu do QAT přidat new prvek, který nikde není. Asi.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <returns></returns>
        protected bool ContainsQAT(IRibbonPage iRibbonPage)
        {
            return (ExistsAnyQat && iRibbonPage != null && iRibbonPage.Groups != null && iRibbonPage.Groups.Any(g => ContainsQAT(g)));
        }
        /// <summary>
        /// Vrátí true, pokud daná grupa jako celek má být přítomna v QAT (Quick Access Toolbar), anebo v sobě obsahuje nějaký prvek, který tam má být umístěn
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <returns></returns>
        protected bool ContainsQAT(IRibbonGroup iRibbonGroup)
        {
            return (ExistsAnyQat && iRibbonGroup != null && (DefinedInQAT(iRibbonGroup.GroupId) || (iRibbonGroup.Items != null && iRibbonGroup.Items.Any(i => ContainsQAT(i)))));
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek jako celek má být přítomen v QAT (Quick Access Toolbar), anebo v sobě obsahuje nějaký Sub-prvek, který tam má být umístěn
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        protected bool ContainsQAT(IRibbonItem iRibbonItem)
        {
            return (ExistsAnyQat && iRibbonItem != null && (DefinedInQAT(GetRibbonQATKey(iRibbonItem)) || (iRibbonItem.SubItems != null && iRibbonItem.SubItems.Any(s => ContainsQAT(s)))));
        }
        /// <summary>
        /// Vrátí true pokud daný klíč je obsažen v požadavku na prvky do QAT
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        protected bool DefinedInQAT(string itemId)
        {
            if (!ExistsAnyQat) return false;
            string key = GetValidQATKey(itemId);
            return (!String.IsNullOrEmpty(key) && _QATUserItemDict != null && _QATUserItemDict.ContainsKey(key));
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně existuje alespoň jeden záznam QAT v poli <see cref="_QATUserItems"/>
        /// </summary>
        protected bool ExistsAnyQat { get { return ((_QATUserItems?.Count ?? 0) > 0); } }
        /// <summary>
        /// Metoda je volána v procesu tvorby nových prvků Ribbonu, když je vytvořen prvek BarItem, který má být obsažen v QAT.
        /// Tato metoda si zaeviduje odkaz na tento BarItem v interním poli, z něhož následně (v metodě <see cref="AddQATUserListToRibbon(Dictionary{string, Tuple{BarItem, IRibbonItem}})"/>) 
        /// všechny patřičné prvky uloží do fyzického ToolBaru QAT.
        /// Tato metoda tedy dodaný prvek nevloží okamžitě do ToolBaru.
        /// Tato metoda, pokud je volána pro prvek který v QAT nemá být, nepřidá tento prvek 
        /// nad rámec požadovaného seznamu prvků v QAT = <see cref="DxQuickAccessToolbar.QATItems"/> (nepřidává nové prvky do pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/>).
        /// <para/>
        /// Duplicita: Pokud na vstupu bude prvek, pro jehož klíč už evidujeme prvek dřívější, pak nový prvek vložíme namísto prvku původního.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="ribbonItem"></param>
        private void AddBarItemToQATUserList(DevExpress.XtraBars.BarItem barItem, IRibbonItem ribbonItem)
        {
            if (barItem == null || ribbonItem == null) return;
            string key = GetValidQATKey(ribbonItem);
            if (String.IsNullOrEmpty(key) || !_QATUserItemDict.TryGetValue(key, out var qatItem)) return;       // Pro daný prvek Ribbonu nemáme záznam, že bychom měli přidat prvek do QAT.
            qatItem.BarItem = barItem;
            qatItem.RibbonItem = ribbonItem;
            qatItem.RibbonGroup = ribbonItem.ParentGroup;
        }
        /// <summary>
        /// Volá se na závěr přidávání dat do stránek: 
        /// do fyzického ToolBaru QAT (Quick Access Toolbar) vepíše všechny aktuálně přítomné prvky z <see cref="_QATUserItems"/>.
        /// </summary>
        /// <param name="qatBarItems">Aktuálně přítomné BarItemy + RibbonItemy v this Ribbonu, klíčem je QatKey. Smí být null.</param>
        protected void AddQATUserListToRibbon(Dictionary<string, Tuple<BarItem, IRibbonItem>> qatBarItems)
        {
            var qatItems = _QATUserItems;
            if (qatItems == null || qatItems.Count == 0) return;

            // Index BarItemů s klíčem QytKey:
            if (qatBarItems is null) qatBarItems = this.ItemsByQat;

            // 1. Pro prvky, které mají být v QAT přítomny, ale nemám pro ně nalezený BarItem, jej zkusím najít:
            //    K tomu může snadno dojít při částečném refreshi prvků
            var searchItems = qatItems.Where(q => q.NeedSearchBarItem).ToArray();
            foreach (var searchItem in searchItems)
                searchItem.SearchForBarItem(qatBarItems);

            // 2. Pokud v seznamu není žádný prvek, který potřebuje provést fyzickou změnu, tak nebudu toolbarem mrkat:
            if (!qatItems.Any(q => q.NeedChangeInQat)) return;

            // Máme tedy provést nějaké změny v Toolbaru. Nemohu pouze přidat nové potřebné prvky, protože bych nedodržel požadované pořadí prvků.
            // (Pořadí je důležité, je dané pořadím v poli _QATUserItems, a když bych přidával jen ty nově vytvořené prvky, které dosud v QAT nebyly, přidaly by se na konec v nesprávním pořadí)

            // 3. Odeberu všechny QAT prvky, které odebrat lze (tj. mají BarLink):
            var removeItems = qatItems.Where(q => q.CanRemoveFromQat).ToArray();
            foreach (var removeItem in removeItems)
                removeItem.RemoveBarItemLink(false, false);

            // 4. Prvky, které mohou jít do QAT do něj reálně zařadím:
            //     (Přidáme potřebné UserItems, a před první z nich volitelně přidáme oddělovač grupy (oddělí Direct a User prvky), pokud v Toolbaru něco zůstává)
            var insertItems = qatItems.Where(q => q.NeedAddToQat).ToArray();
            if (insertItems.Length > 0)
            {
                bool isGroupBegin = (this.Toolbar.ItemLinks.Count > 0);
                foreach (var insertItem in insertItems)
                    insertItem.AddLinkToQat(ref isGroupBegin);
            }
        }
        #endregion
        #region Remove při smazání obsahu stránky DxRibbonPage.ClearContent()
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány grupy.
        /// QAT by si měl odpovídající prvky deaktivovat.
        /// </summary>
        /// <param name="groupsToDelete"></param>
        private void RemoveGroupsFromQat(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPageGroup> groupsToDelete)
        {
            if (groupsToDelete != null)
                RemoveLinksFromQat(groupsToDelete.Select(g => g.Name));
        }
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány prvky.
        /// QAT by si měl odpovídající prvky deaktivovat.
        /// </summary>
        /// <param name="itemsToDelete"></param>
        private void RemoveItemsFromQat(IEnumerable<DevExpress.XtraBars.BarItem> itemsToDelete)
        {
            if (itemsToDelete != null)
            {
                var ribbonItems = itemsToDelete
                    .Select(i => i.Tag)
                    .OfType<BarItemTagInfo>()
                    .Select(i => i.Data)
                    .ToArray();
                ribbonItems.ForEachExec(i => RemoveItemFromQat(i));
            }
        }
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány prvky.
        /// QAT by si měl odpovídající prvky deaktivovat.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        private void RemoveItemFromQat(IRibbonItem iRibbonItem)
        {
            if (iRibbonItem == null) return;
            RemoveLinkFromQat(GetRibbonQATKey(iRibbonItem));

            // Rekurzivně jeho subpoložky (to může být i opakovaně):
            var items = iRibbonItem.SubItems;
            if (items != null)
                items.ForEachExec(i => RemoveItemFromQat(i));
        }
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány prvky s danými jmény.
        /// QAT by si měl odpovídající prvky deaktivovat, pokud je eviduje.
        /// </summary>
        /// <param name="keysToDelete"></param>
        private void RemoveLinksFromQat(IEnumerable<string> keysToDelete)
        {
            if (keysToDelete != null)
                keysToDelete.ForEachExec(key => RemoveLinkFromQat(key));
        }
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány prvky s danými jmény.
        /// QAT by si měl odpovídající prvky deaktivovat, pokud je eviduje.
        /// </summary>
        /// <param name="itemId"></param>
        private void RemoveLinkFromQat(string itemId)
        {
            string key = GetValidQATKey(itemId);
            if (!String.IsNullOrEmpty(key) && this._QATUserItemDict.TryGetValue(key, out var qatItem))
                qatItem.RemoveBarItemLink(false, true);
        }
        #endregion
        #region Uživatelská interaktivita - pozor, je nutno řešit odlišnosti pro mergované prvky
        /// <summary>
        /// Zajistí navázání eventhandleru pro CustomizationPopupMenu.
        /// Volá se v Initu a pak po každém Merge i Unmerge v this Ribbonu...
        /// </summary>
        private void CustomizationPopupMenuRefreshHandlers()
        {
            this.CustomizationPopupMenu.Popup -= CustomizationPopupMenu_Popup;
            this.CustomizationPopupMenu.Popup += CustomizationPopupMenu_Popup;
        }
        /// <summary>
        /// Úpravy kontextového menu před jeho zobrazením
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CustomizationPopupMenu_Popup(object sender, EventArgs e)
        {
            Point point = Control.MousePosition;
            Point localPoint = this.PointToClient(point);

            DevExpress.XtraBars.BarItemLink activeLink = this.CustomizationPopupMenu.Activator as DevExpress.XtraBars.BarItemLink;

            // Zrušit systémové menu:
            this.CustomizationPopupMenu.HidePopup();
            this.CustomizationPopupMenu = new DevExpress.XtraBars.Ribbon.Internal.RibbonCustomizationPopupMenu(this);
            this.CustomizationPopupMenu.ClearLinks();
            this.CustomizationPopupMenuRefreshHandlers();                      // Právě jsem vygeneroval nové Popup menu, a chci i příště vyvolat tuhle metodu...

            // Připravit a aktivovat vlastní menu:
            DevExpress.XtraBars.BarItemLink link = activeLink ?? GetLinkAtPoint(null, point);
            bool isLink = (link != null);
            bool hasItem = TryGetIRibbonData(link, out var _, out var ribbonItem, out var _, out var _);

            bool isQatDirectItem = isLink && _IsQATDirectItem(link);
            bool isAllowedForQat = isLink && _IsItemAllowedForQAT(ribbonItem);
            bool isInUserQatItem = isLink && _IsInUserQatItem(ribbonItem);
            bool isQatAnyItem = isLink && _IsQATAnyItem(link);

            List<IMenuItem> items = new List<IMenuItem>();
            if (isQatDirectItem)
                // Button je DIRECT (=zadaný v definici menu jako QAT Button):
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonDirectQatItem}", Text = DxComponent.Localize(MsgCode.RibbonDirectQatItem), ImageName = "", Enabled = false });
            else if (isAllowedForQat)
            {   // Button smí být přítomen v QAT:
                if (!isInUserQatItem)
                    items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonAddToQat}", Text = DxComponent.Localize(MsgCode.RibbonAddToQat), ImageName = ImageName.DxRibbonQatMenuAdd, Tag = link, Enabled = isLink, ClickAction = CustomizationPopupMenu_ExecAdd });
                else
                    items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonRemoveFromQat}", Text = DxComponent.Localize(MsgCode.RibbonRemoveFromQat), ImageName = ImageName.DxRibbonQatMenuRemove, Tag = link, ClickAction = CustomizationPopupMenu_ExecRemove });
            }
            else
                // Nelze přidat na QAT:
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonDisabledForQatItem}", Text = DxComponent.Localize(MsgCode.RibbonDisabledForQatItem), ImageName = "", Enabled = false });

            // Pozice QAT:
            bool isAbove = (this.ToolbarLocation == RibbonQuickAccessToolbarLocation.Above || this.ToolbarLocation == RibbonQuickAccessToolbarLocation.Default);
            if (!isAbove)
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonShowQatTop}", Text = DxComponent.Localize(MsgCode.RibbonShowQatTop), ImageName = ImageName.DxRibbonQatMenuMoveUp, ItemIsFirstInGroup = true, ClickAction = CustomizationPopupMenu_ExecMoveUp });
            else
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonShowQatDown}", Text = DxComponent.Localize(MsgCode.RibbonShowQatDown), ImageName = ImageName.DxRibbonQatMenuMoveDown, ItemIsFirstInGroup = true, ClickAction = CustomizationPopupMenu_ExecMoveDown });

            bool hasQatManager = true;           // Možnost potlačit managera je jednoduchá...
            if (hasQatManager)
            {
                bool isAnyQatContent = (QatManagerItems.Length > 0);
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonShowManager}", Text = DxComponent.Localize(MsgCode.RibbonShowManager), ImageName = ImageName.DxRibbonQatMenuShowManager, Enabled = isAnyQatContent, ClickAction = CustomizationPopupMenu_ExecShowManager });
            }

            var popup = DxComponent.CreateDXPopupMenu(items, caption: link?.Caption);
            popup.ShowPopup(this, localPoint);
        }
        /// <summary>
        /// Kontextové menu QAT: Přidej prvek
        /// </summary>
        /// <param name="menuItem"></param>
        private void CustomizationPopupMenu_ExecAdd(IMenuItem menuItem)
        {
            if (menuItem?.Tag is DevExpress.XtraBars.BarItemLink link)
                OnAddToToolbar(link);
        }
        /// <summary>
        /// Kontextové menu QAT: Odeber prvek
        /// </summary>
        /// <param name="menuItem"></param>
        private void CustomizationPopupMenu_ExecRemove(IMenuItem menuItem)
        {
            if (menuItem?.Tag is DevExpress.XtraBars.BarItemLink link)
                OnRemoveFromToolbar(link);
        }
        /// <summary>
        /// Kontextové menu QAT: Dej QAT dolů
        /// </summary>
        /// <param name="menuItem"></param>
        private void CustomizationPopupMenu_ExecMoveDown(IMenuItem menuItem)
        {
            this.ToolbarLocation = RibbonQuickAccessToolbarLocation.Below;
            this._RunQATItemKeysChanged(RibbonQuickAccessToolbarLocation.Below, null);
        }
        /// <summary>
        /// Kontextové menu QAT: Dej QAT nahoru
        /// </summary>
        /// <param name="menuItem"></param>
        private void CustomizationPopupMenu_ExecMoveUp(IMenuItem menuItem)
        {
            this.ToolbarLocation = RibbonQuickAccessToolbarLocation.Above;
            this._RunQATItemKeysChanged(RibbonQuickAccessToolbarLocation.Above, null);
        }
        /// <summary>
        /// Kontextové menu QAT: Ukaž managera
        /// </summary>
        /// <param name="menuItem"></param>
        private void CustomizationPopupMenu_ExecShowManager(IMenuItem menuItem)
        {
            ShowQatManager();
        }
        /// <summary>
        /// Uživatel zmáčkl malou šipku dolů v Toolbaru, kde jsou zobrazeny jednotlivé prvky QAT a on je může dát Visible/Invisible.
        /// Taky můžeme přeložit Caption v prvku "Show Quick Access Toolbar Above the Ribbon"...
        /// Anebo můžeme odebrat (e.ItemLinks.RemoveAt(0)) některé prvky, které jsou "QATDirect"...
        /// Pozor, event probíhá dvakrát, než se zobrazí menu!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxRibbonControl_CustomizeQatMenu(object sender, CustomizeQatMenuEventArgs e)
        {
            // e.ItemLinks.RemoveAt(0);
        }
        /// <summary>
        /// Událost, kdy dojde k přidání či odebrání prvku QAT, a to jak uživatelem tak z programu
        /// </summary>
        /// <param name="e"></param>
        private void SourceToolbar_LinksChanged(System.ComponentModel.CollectionChangeEventArgs e)
        {
            // Sem to chodí při změnách jak od uživatele, tak z kódu.
            // Tady bych musel řešit odlišení Code/User, ale v metodách OnAddToToolbar() a OnRemoveFromToolbar() to chodí jen od User !!!
        }
        /// <summary>
        /// Změnila se pozice Toolbaru QAT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxRibbonControl_ToolbarLocationChanged(object sender, EventArgs e)
        {
            
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek odpovídá některému prvku v QAT Toolbaru.
        /// Detekuje přímo příslušnost prvku k Toolbaru.
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private bool _IsQATAnyItem(BarItemLink link)
        {
            if (link is null) return false;
            return (this.Toolbar != null && this.Toolbar.ItemLinks.Contains(link));
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek <see cref="IRibbonItem"/> obsahuje data, která je možno přidat do QAT
        /// </summary>
        /// <param name="ribbonItem">Nalezený datový prvek</param>
        /// <returns></returns>
        private bool _IsItemAllowedForQAT(IRibbonItem ribbonItem)
        {
            if (ribbonItem is null) return false;
            string qatKey = GetValidQATKey(ribbonItem);
            return (!String.IsNullOrEmpty(qatKey));
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek <see cref="IRibbonItem"/> má svoji reprezentaci v UserQAT Toolbaru
        /// </summary>
        /// <param name="ribbonItem">Nalezený datový prvek</param>
        /// <returns></returns>
        private bool _IsInUserQatItem(IRibbonItem ribbonItem)
        {
            if (ribbonItem is null) return false;
            string qatKey = GetValidQATKey(ribbonItem);
            return DxQuickAccessToolbar.ContainsQATItem(qatKey);
        }
        /// <summary>
        /// Počet reálně zobrazených QAT prvků
        /// </summary>
        private int UserQatItemsCount
        {
            get
            {
                int count = 0;
                if (_QATUserItems != null)
                    count += _QATUserItems.Count(q => q.IsInQAT);
                if (this.MergedChildDxRibbon != null)
                    count += this.MergedChildDxRibbon.UserQatItemsCount;
                return count;
            }
        }
        /// <summary>
        /// Uživatel rukama přidal něco do QAT. Sem to nechodí při změnách z kódu!
        /// </summary>
        /// <param name="link"></param>
        protected override void OnAddToToolbar(DevExpress.XtraBars.BarItemLink link)
        {
            var qatLinks = link.Item.Links;           // Vstupní parametr (link) je fyzické tlačítko, na které bylo kliknuto
            int count1 = qatLinks.Count;              // Hodnota count1 odpovídá indexu prvního linku, který bude do pole qatLinks vygenerován jako QAT Link...
            base.OnAddToToolbar(link);                // Tady vznikne new instance Linku pro tlačítko, které bude umístěno do QAT
            ModifyLinksForToolbar(qatLinks, count1);  // Modifikujeme všechny nově přidané BarItemLink = všechny tyto nové linky jsou linky umístěné v QAT Toolbarech (nativní plus mergovaný Ribbon)
            this.UserAddItemToQat(link);
        }
        /// <summary>
        /// Metoda upraví vzhled tlačítka, které je aktuálně přidáváno do QAT.
        /// Může změnit jeho styl atd.
        /// Na vstupu je kolekce všech linků na daný prvek, a index prvního linku který je třeba modifikovat.
        /// </summary>
        /// <param name="qatLinks"></param>
        /// <param name="index"></param>
        private void ModifyLinksForToolbar(DevExpress.XtraBars.BarItemLinkCollection qatLinks, int index)
        {
            int count = qatLinks.Count;
            for (int i = index; i < count; i++)
                ModifyLinkForToolbar(qatLinks[i]);
        }
        /// <summary>
        /// Metoda upraví vzhled všech tlačítek, které jsou aktuálně přítomny v QAT.
        /// </summary>
        private void ModifyLinksForToolbar()
        {
            this.Toolbar.ItemLinks.ForEachExec(l => ModifyLinkForToolbar(l));
        }
        /// <summary>
        /// Upraví dodaný <see cref="BarItemLink"/> pro zobrazení v QAT
        /// </summary>
        /// <param name="qatLink"></param>
        void IDxRibbonInternal.ModifyLinkForQat(BarItemLink qatLink) { ModifyLinkForToolbar(qatLink); }
        /// <summary>
        /// Metoda upraví vzhled tlačítka, které je aktuálně přidáváno do QAT.
        /// Může změnit jeho styl atd.
        /// </summary>
        /// <param name="qatLink"></param>
        private void ModifyLinkForToolbar(DevExpress.XtraBars.BarItemLink qatLink)
        {
            bool? isVisibleQatText = this.ShowTextInQAT;
            qatLink.UserRibbonStyle = 
                (!isVisibleQatText.HasValue ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.Default :          // null  : default = převezme se z BarItem
                (isVisibleQatText.Value ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText :        // true  : s textem
                DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithoutText));                              // false : bez textu

            //if (qatLink?.Item?.Tag is BarItemTagInfo itemInfo)
            //{   // Pokud BarItem je vytvořen na základě dat IRibbonItem:
            //    //  lze načíst další data
            //}

            //  Výměna ikony mi ještě nejde (změna se neprojeví v buttonu):
            // qatLink.ImageIndex = DxComponent.GetVectorImageIndex("svgimages/chart/chart.svg", ResourceImageSizeType.Small);
            // qatLink.ImageOptions.ImageIndex = qatLink.ImageIndex;
        }
        /// <summary>
        /// Uživatel něco přidal do QAT
        /// </summary>
        /// <param name="link"></param>
        private void UserAddItemToQat(DevExpress.XtraBars.BarItemLink link)
        {
            if (TryGetIRibbonData(link, out var key, out var iRibbonItem, out var iRibbonGroup, out var ownerRibbon))
            {   // Našli jsme data o dodaném prvku? Předáme to k vyřešení do instance Owner Ribbonu!
                //  - Totiž this může být nějaký Master Ribbon, do něhož je Mergován nějaký Slave Ribbon, a aktuální BarItem pochází z toho Slave Ribbonu.
                //  - Pak i evidenci QAT a volání události QATItemKeysChanged musí proběhnout v té instanci Ribbonu, které se věc týká, a nikoli this, kde je věc jen hostována ve formě Merge.
                if (this.CustomizationPopupMenu.Visible) CustomizationPopupMenu.HidePopup();
                ownerRibbon.UserAddItemToQat(link, key, iRibbonItem, iRibbonGroup);
            }
        }
        /// <summary>
        /// Uživatel něco přidal do QAT. 
        /// Už víme, co to bylo, a je zajištěno, že zdejší instance (this) je majitelem daného prvku a je tedy odpovědná za evidenci QAT a vyvolání správného eventhandleru.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="key"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        private void UserAddItemToQat(DevExpress.XtraBars.BarItemLink link, string key, IRibbonItem iRibbonItem, IRibbonGroup iRibbonGroup)
        {
            if (!_QATUserItemDict.ContainsKey(key))
            {   // Neznáme => přidat:
                QatItem qatItem = new QatItem(this, key, iRibbonItem, iRibbonGroup, link.Item, link);
                _QATUserItemDict.Add(key, qatItem);
                _QATUserItems.Add(qatItem);
                if (this.IsMergedIntoDxParent)
                    _HasQATUserChangedItems = true;
                _RunQATItemKeysChanged(null, _QATUserItems.Select(q => q.QatKey).ToArray());
            }
        }
        /// <summary>
        /// Uživatel rukama odebral něco z QAT. Sem to nechodí při změnách z kódu!
        /// </summary>
        /// <param name="link"></param>
        protected override void OnRemoveFromToolbar(DevExpress.XtraBars.BarItemLink link)
        {
            this.UserRemoveItemFromQat(link);
            base.OnRemoveFromToolbar(link);
        }
        /// <summary>
        /// Uživatel něco odebral z QAT
        /// </summary>
        /// <param name="link"></param>
        private void UserRemoveItemFromQat(DevExpress.XtraBars.BarItemLink link)
        {
            if (TryGetIRibbonData(link, out var qatKey, out var iRibbonItem, out var iRibbonGroup, out var ownerRibbon))
            {   // Našli jsme data o dodaném prvku? Předáme to k vyřešení do instance Owner Ribbonu!
                //  - Totiž this může být nějaký Master Ribbon, do něhož je Mergován nějaký Slave Ribbon, a aktuální BarItem pochází z toho Slave Ribbonu.
                //  - Pak i evidenci QAT a volání události QATItemKeysChanged musí proběhnout v té instanci Ribbonu, které se věc týká, a nikoli this, kde je věc jen hostována ve formě Merge.
                if (this.CustomizationPopupMenu.Visible) CustomizationPopupMenu.HidePopup();
                ownerRibbon.UserRemoveItemFromQat(link, qatKey, iRibbonItem, iRibbonGroup);
            }
        }
        /// <summary>
        /// Uživatel něco odebral z QAT. 
        /// Už víme, co to bylo, a je zajištěno, že zdejší instance (this) je majitelem daného prvku a je tedy odpovědná za evidenci QAT a vyvolání správného eventhandleru.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="qatKey"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        private void UserRemoveItemFromQat(DevExpress.XtraBars.BarItemLink link, string qatKey, IRibbonItem iRibbonItem, IRibbonGroup iRibbonGroup)
        {
            {   // Našli jsme data o dodaném prvku?
                if (_QATUserItemDict.TryGetValue(qatKey, out var qatItem))
                {   // Známe => odebrat:
                    qatItem.Reset();
                    _QATUserItemDict.Remove(qatKey);
                    _QATUserItems.RemoveAll(q => q.QatKey == qatKey);
                    if (this.IsMergedIntoDxParent)
                        _HasQATUserChangedItems = true;
                    _RunQATItemKeysChanged(null, _QATUserItems.Select(q => q.QatKey).ToArray());
                }
            }
        }
        /// <summary>
        /// Pokud this Ribbon má nějaké změněné QAT prvky od uživatele, pak nyní by si je měl promítnout do svého nativního QAT - pokud je uživatel přidal do nějakého Parent Ribbonu.
        /// To signalizuje proměnná <see cref="_HasQATUserChangedItems"/>.
        /// </summary>
        private void RefreshUserQatAfterUnMerge()
        {
            if (DxDisposed) return;
            if (_HasQATUserChangedItems)
            {
                _HasQATUserChangedItems = false;
                _SetQATUserItemKeys(true, true);
            }
            this.MergedChildDxRibbon?.RefreshUserQatAfterUnMerge();
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit data o prvku Ribbonu <see cref="IRibbonItem"/> podle jeho ItemId
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="isValid"></param>
        /// <param name="isReloadingMenuValid">Upřesnění pro prvek typu Menu: pokud najdeš prvek Menu, a ten bude ve stavu Reloading, pak to máme akceptovat jako validní (vstup true == výstup true) nebo nevalidní (vstup false == výstup false) ?</param>
        /// <returns></returns>
        private bool TryGetIRibbonData(string itemId, out IRibbonItem iRibbonItem, out bool isValid, bool isReloadingMenuValid)
        {
            iRibbonItem = null;
            isValid = false;
            if (String.IsNullOrEmpty(itemId)) return false;
            DxRibbonControl ribbon = this;
            BarItem barItem = null;
            while (ribbon != null)
            {
                barItem = ribbon.Items[itemId];
                if (barItem != null) break;
                ribbon = ribbon.MergedChildDxRibbon;
            }
            if (barItem == null || barItem.Tag == null) return false;

            object tag = barItem.Tag;
            if (tag is BarItemTagInfo itemInfo)
            {
                iRibbonItem = itemInfo.Data;
                var menuValidity = itemInfo.ValidMenu;     // Obsahuje informaci o validitě prvku. U menu zde evidujeme navíc stav Reloading...
                isValid = (menuValidity == ContentValidityType.Valid || (isReloadingMenuValid && menuValidity == ContentValidityType.ReloadInProgress));     // Pokud isReloadingMenuValid je true, pak jako Valid akceptujeme i stav .ReloadInProgress !
                return true;
            }
            if (tag is IRibbonItem iItem)
            {
                iRibbonItem = iItem;
                isValid = true;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Metoda v dodaném linku najde jeho Item, jeho Tag, detekuje jeho typ a určí jeho Key, najde Ribbon, který prvek deklaroval, uloží typové výsledky a vrátí true.
        /// Pokud se nezdaří, vrátí false.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="qatKey"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        /// <param name="ownerRibbon"></param>
        /// <returns></returns>
        private bool TryGetIRibbonData(DevExpress.XtraBars.BarItemLink link, out string qatKey, out IRibbonItem iRibbonItem, out IRibbonGroup iRibbonGroup, out DxRibbonControl ownerRibbon)
        {
            return TryGetIRibbonData(link?.Item, out qatKey, out iRibbonItem, out iRibbonGroup, out ownerRibbon);
        }
        /// <summary>
        /// Metoda pro dodaný BarItem najde jeho Tag, detekuje jeho typ a určí jeho Key, a najde i Ribbon, který prvek deklaroval, uloží typové výsledky a vrátí true.
        /// Pokud se nezdaří, vrátí false.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="qatKey"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        /// <param name="ownerRibbon"></param>
        /// <returns></returns>
        private static bool TryGetIRibbonData(DevExpress.XtraBars.BarItem barItem, out string qatKey, out IRibbonItem iRibbonItem, out IRibbonGroup iRibbonGroup, out DxRibbonControl ownerRibbon)
        {
            if (_TryGetIRibbonData(barItem, out qatKey, out iRibbonItem, out iRibbonGroup))
                return (_TryGetDxRibbon(barItem, out ownerRibbon));
            ownerRibbon = null;
            return false;
        }
        /// <summary>
        /// Metoda pro dodaný BarItem najde jeho Tag, detekuje jeho typ a určí jeho Key, uloží typové výsledky a vrátí true.
        /// Pokud se nezdaří, vrátí false.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="qatKey"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonData(DevExpress.XtraBars.BarItem barItem, out string qatKey, out IRibbonItem iRibbonItem, out IRibbonGroup iRibbonGroup)
        {
            if (barItem != null)
            {
                object tag = barItem.Tag;
                if (tag is BarItemTagInfo itemInfo)
                {
                    qatKey = GetValidQATKey(itemInfo.Data);
                    if (!String.IsNullOrEmpty(qatKey))
                    {
                        iRibbonItem = itemInfo.Data;
                        iRibbonGroup = itemInfo.Data.ParentGroup;
                        return true;
                    }
                }
                else if (tag is IRibbonItem iItem)
                {
                    qatKey = GetValidQATKey(iItem);
                    if (!String.IsNullOrEmpty(qatKey))
                    {
                        iRibbonItem = iItem;
                        iRibbonGroup = iItem.ParentGroup;
                        return true;
                    }
                }
                else if (tag is IRibbonGroup iGroup)
                {
                    qatKey = GetValidQATKey(iGroup.GroupId);
                    if (!String.IsNullOrEmpty(qatKey))
                    {
                        iRibbonItem = null;
                        iRibbonGroup = iGroup;
                        return true;
                    }
                }
                else if (tag is DxRibbonGroup dxGroup)
                {
                    qatKey = GetValidQATKey(dxGroup.GroupId);
                    if (!String.IsNullOrEmpty(qatKey))
                    {
                        iRibbonItem = null;
                        iRibbonGroup = dxGroup.DataGroupLast;
                        return true;
                    }
                }
            }
            qatKey = null;
            iRibbonItem = null;
            iRibbonGroup = null;
            return false;
        }
        /// <summary>
        /// Došlo k interaktivní změně v obsahu UserQAT prvcích, zavolej událost <see cref="QATItemKeysChanged"/>
        /// </summary>
        private void _RunQATItemKeysChanged(RibbonQuickAccessToolbarLocation? qatLocation, string[] qatItems)
        {
            if (this.CustomizationPopupMenu.Visible) this.CustomizationPopupMenu.HidePopup();

            _QATLocalConfigValue = DxQuickAccessToolbar.SerializeConfigValue(qatLocation ?? DxQuickAccessToolbar.QATLocation, qatItems ?? DxQuickAccessToolbar.QATItems);

            OnQATItemKeysChanged();
            QATItemKeysChanged?.Invoke(this, EventArgs.Empty);

            // Tady dojde k eventu DxQuickAccessToolbar.QATItemKeysChanged, a vyvolá se zdejší handler _DxQATItemKeysChanged.
            //   Z principu je provedena vždy jen jedna změna (Location anebo Items), nikdy ne obě:
            if (qatLocation.HasValue) DxQuickAccessToolbar.QATLocation = qatLocation.Value;
            if (qatItems != null) DxQuickAccessToolbar.QATItems = qatItems;
        }
        /// <summary>
        /// Je provedeno po změně hodnoty v poli UserQAT items
        /// </summary>
        protected virtual void OnQATItemKeysChanged() { }
        /// <summary>
        /// Je voláno po změně hodnoty v poli UserQAT items přímo v this Ribbonu
        /// </summary>
        public event EventHandler QATItemKeysChanged;
        #endregion
        #region QATDirectItems - Explicitní obsah QAT = prvky mimo Ribbon
        /// <summary>
        /// Prvky obsažené v QAT - explicitně definované mimo stránky Ribbonu
        /// </summary>
        public IRibbonItem[] QATDirectItems { get { return _QATDirectItems?.Select(q => q.RibbonItem).ToArray(); } set { _SetQATDirectItems(value); } }
        private QatItem[] _QATDirectItems;
        /// <summary>
        /// Dictionary úplně všech prvků v QAT Direct, podle klíče. Obsahuje i rekurzivně svoje Childs.
        /// Pozor na rozdíl: pole <see cref="_QATDirectItems"/> obsahuje jen prvky, které mají být přímo v Ribbonu,
        /// ale Dictionary <see cref="_QATDirectAllItemKeys"/> obsahuje i jejich Child a Child Childů atd...
        /// </summary>
        private Dictionary<string, IRibbonItem> _QATDirectAllItemKeys
        {
            get
            {
                if (_QATDirectItems is null) return null;
                var allItems = _QATDirectItems.Select(q => q.RibbonItem).Flatten(i => i.SubItems);
                return allItems.Select(t => t.Item2).CreateDictionary(i => GetValidQATKey(i), true);
            }
        }
        /// <summary>
        /// Vloží dané prvky do QAT Direct
        /// </summary>
        /// <param name="items"></param>
        private void _SetQATDirectItems(IRibbonItem[] items)
        {
            bool hasOldItems = (_QATDirectItems != null && _QATDirectItems.Length > 0);
            bool hasNewItems = (items != null && items.Length > 0);
            if (!hasOldItems && !hasNewItems) return;

            // Změna QAT musí probíhat v UnMerged stavu, jinak se neprojeví v TopParent ribbonu!
            _UnMergeModifyMergeCurrentRibbon(() => _SetQATDirectItemsInner(items), true);
        }
        /// <summary>
        /// Vloží dané prvky do QAT Direct, ve stavu ribbonu UnMerged
        /// </summary>
        /// <param name="iRibbonItems"></param>
        private void _SetQATDirectItemsInner(IRibbonItem[] iRibbonItems)
        {
            _ClearQATDirectItems();
            
            List<QatItem> qatItems = new List<QatItem>();
            if (iRibbonItems != null)
            {
                var qatUserLinks = this.Toolbar.ItemLinks.ToArray();
                int mergeOrder = -1000;
                foreach (var ribbonItem in iRibbonItems)
                {
                    var iRibbonItem = ribbonItem;
                    if (iRibbonItem is null) continue;
                    var barItem = CreateItem(ref iRibbonItem);
                    if (barItem != null)
                    {
                        barItem.MergeOrder = mergeOrder++;
                        var barLink = this.Toolbar.ItemLinks.Add(barItem, iRibbonItem.ItemIsFirstInGroup);
                        qatItems.Add(new QatItem(this, iRibbonItem, barItem, barLink));
                    }
                }

                if (qatUserLinks.Length > 0 && qatItems.Count > 0)
                    _MoveQatLinksToEnd(qatUserLinks, true);
            }
            _QATDirectItems = qatItems.ToArray();
        }
        /// <summary>
        /// Metoda zajistí přenesení prvků Link v rámci Toolbaru z jejich současné pozice na konec Toolbaru. Do prvního z přenášených prvků může nastavit separátor skupiny.
        /// </summary>
        /// <param name="qatUserLinks"></param>
        /// <param name="separeGroup"></param>
        private void _MoveQatLinksToEnd(BarItemLink[] qatUserLinks, bool separeGroup = false)
        {
            if (qatUserLinks == null || qatUserLinks.Length == 0) return;

            foreach (var qatUserLink in qatUserLinks)
            {
                if (qatUserLink != null && qatUserLink.Item != null)
                {
                    var qatItem = qatUserLink.Item;
                    if (this.Toolbar.ItemLinks.Contains(qatUserLink))
                        this.Toolbar.ItemLinks.Remove(qatUserLink);
                    var newLink = this.Toolbar.ItemLinks.Add(qatItem);
                    if (separeGroup)
                    {
                        newLink.BeginGroup = true;
                        separeGroup = false;
                    }
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek odpovídá některému prvku QAT Direct = prvek Toolbaru deklarovaný aplikací v Owner Ribbonu daného prvku (tzn. dohledá Owner Ribbon k danému prvku, a v jeho QAT Direct hledá).
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private bool _IsQATDirectItem(BarItemLink link)
        {
            if (link is null) return false;
            if (!TryGetIRibbonData(link, out var _, out var iRibbonItem, out var _, out var ownerRibbon)) return false;   // Link může být umístěn na mergovaném Ribbonu (instance this), ale pochází z nějakého Child Ribbonu = ten nyní najdeme do ownerRibbon.
            return ownerRibbon._IsQATDirectOwnItem(iRibbonItem);
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek odpovídá některému prvku QAT Direct = prvek Toolbaru deklarovaný aplikací v this Ribbonu.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private bool _IsQATDirectOwnItem(IRibbonItem iRibbonItem)
        { 
            if (iRibbonItem is null || _QATDirectItems is null) return false;
            var key = GetValidQATKey(iRibbonItem);
            if (String.IsNullOrEmpty(key)) return false;

            var allQATItems = _QATDirectAllItemKeys;                 // _QATDirectAllItemKeys není null, pokud není null _QATDirectItems; a to jsme testovali.
            return allQATItems.ContainsKey(key);
        }
        /// <summary>
        /// Odebere současné prvky QATDirectItems (pokud takové jsou)
        /// </summary>
        private void _ClearQATDirectItems()
        {
            var qatItems = _QATDirectItems;
            if (qatItems == null) return;
            foreach (var qatItem in qatItems)
            {
                qatItem.RemoveBarItemLink(true, true);
                qatItem.Dispose();
            }
            _QATDirectItems = null;
        }
        #endregion
        #region QatManager - úprava položek v QAT pomocí okna s Listem dostupných prvků
        /// <summary>
        /// Zobrazí managera pro nastavení QAT prvků
        /// </summary>
        private void ShowQatManager()
        {
            using (DxControlForm form = new DxControlForm())
            {
                Size formSize = new Size(750, 450);
                form.PrepareStartPosition(true, false, formSize);
                form.Buttons = new IMenuItem[]
                {
                    new DataMenuItem() { ItemId = "OK", Text = "OK", ImageName = ImageName.DxDialogApply, HotKeys = Keys.Control | Keys.Enter, Tag = DialogResult.OK },
                    new DataMenuItem() { ItemId = "Cancel", Text = "Cancel", ImageName = ImageName.DxDialogCancel, HotKeys = Keys.Escape, Tag = DialogResult.Cancel }
                };
                form.Text = DxComponent.Localize(MsgCode.RibbonQatManagerTitle);

                var qatPanel = new DxListBoxPanel() { Dock = DockStyle.Fill };
                form.ControlPanel.Controls.Add(qatPanel);
                var currentItems = QatManagerItems;
                qatPanel.ListBox.MenuItems = currentItems;
                qatPanel.ListBox.SelectionMode = SelectionMode.MultiExtended;
                qatPanel.ListBox.ItemHeight = 20;
                qatPanel.ListBox.DragDropActions = DxDragDropActionType.ReorderItems;
                qatPanel.ListBox.EnabledKeyActions = ControlKeyActionType.MoveDown | ControlKeyActionType.MoveUp | ControlKeyActionType.Delete;
                qatPanel.ButtonsPosition = ToolbarPosition.RightSideCenter;

                ControlKeyActionType buttonsTypes =
                    ControlKeyActionType.MoveTop | ControlKeyActionType.MoveUp | ControlKeyActionType.MoveDown | ControlKeyActionType.MoveBottom |
                    ControlKeyActionType.SelectAll | ControlKeyActionType.Delete;
                
                if (DxComponent.IsDebuggerActive)
                    buttonsTypes |= ControlKeyActionType.Undo | ControlKeyActionType.Redo;

                qatPanel.ButtonsTypes = buttonsTypes;


                var result = form.ShowDialog(this.FindForm());

                if (result == DialogResult.OK)
                {   // V ListBoxu jsou prvky typované jako IMenuItem, ale protože jsem tam dával IRibbonItem, tak tam budou tito potomkové:
                    var modifiedItems = qatPanel.ListBox.MenuItems.OfType<IRibbonItem>().ToArray();
                    ChangeQatItems(currentItems, modifiedItems);
                }    
            }     
        }
        /// <summary>
        /// Prvky QAT, které se budou zobrazovat v QatManageru
        /// </summary>
        private IRibbonItem[] QatManagerItems
        {
            get 
            {
                // Získám aktuálně viditelné prvky = BarItemy z this Ribbonu, plus BarItemy ze všech mergovaných ribbonů:
                var items = this.Toolbar.ItemLinks.Select(l => l.Item).ToArray();

                // Pokud najdeme ikonu, která má prázdný text a/nebo ikonu, získáme ji z BarItemu:
                var ribbonItems = new List<IRibbonItem>();
                for (int i = 0; i < items.Length; i++)
                {
                    var barItem = items[i];
                    if (barItem.Visibility == BarItemVisibility.Never) continue;
                    if (!_TryGetIRibbonData(barItem, out var _, out var iRibbonItem, out var _)) continue;

                    string qatKey = GetRibbonQATKey(iRibbonItem);
                    if (!DxQuickAccessToolbar.ContainsQATItem(qatKey)) continue;            // Toto není UserQAT, ale FixedQAT prvek!

                    bool hasImage = (!String.IsNullOrEmpty(iRibbonItem.ImageName) || iRibbonItem.Image != null || iRibbonItem.SvgImage != null);
                    bool hasText = !String.IsNullOrEmpty(iRibbonItem.Text);
                    if (hasImage && hasText)
                    {   // Běžný standardně definovaný IRibbonItem s obrázkem i textem:
                        ribbonItems.Add(iRibbonItem);
                    }
                    else
                    {   // Tady jsou typicky DevExpress itemy (skin, paleta), tam nedefinujeme ani text, ani obrázek, protože Ribbon je zobrazuje proměnné:
                        // ... anebo deklarované prvky bez textu, pro ně se podíváme i do ToolTipu:
                        var cloneItem = DataRibbonItem.CreateClone(iRibbonItem);
                        if (!hasImage) { cloneItem.Image = barItem.ImageOptions.Image; cloneItem.SvgImage = barItem.ImageOptions.SvgImage; }
                        if (!hasText) cloneItem.Text = barItem.Caption;
                        if (String.IsNullOrEmpty(cloneItem.Text)) cloneItem.Text = cloneItem.ToolTipText;   // Když nenajdu text ani přímo v Buttonu, zkusím ještě ToolTip...

                        hasImage = !String.IsNullOrEmpty(cloneItem.ImageName) || cloneItem.Image != null;
                        hasText = !String.IsNullOrEmpty(cloneItem.Text);
                        if (hasImage || hasText)
                            ribbonItems.Add(cloneItem);
                    }
                }
                return ribbonItems.ToArray();
            }
        }
        /// <summary>
        /// Zpracuje požadavek na změnu ikon (přetřídění anebo vymazání) s ohledem na kompletní sadu QAT ikon v <see cref="DxQuickAccessToolbar.QATItems"/>.
        /// V našich prvcích (parametry) je jen podmnožina z celkového pole QAT prvků (protože náš Ribbon nejspíš nezobrazuje všechny buttony z celé aplikace).
        /// </summary>
        /// <param name="currentItems"></param>
        /// <param name="modifiedItems"></param>
        private void ChangeQatItems(IRibbonItem[] currentItems, IRibbonItem[] modifiedItems)
        {
            // Z dodaných dat si vytáhneme jen jejich klíče QatKey:
            var currentIds = currentItems.Select(i => GetRibbonQATKey(i)).ToArray();
            var modifiedIds = modifiedItems.Select(i => GetRibbonQATKey(i)).ToArray();
            if (modifiedIds.Length > currentIds.Length)
                throw new InvalidOperationException($"DxRibbonControl.ChangeQatItems() error: modifiedItems has more items than currentItems.");

            // Zjistíme, zda vůbec došlo k nějaké změně:   pokud ne, pak nic dalšího nepodnikáme:
            if (currentIds.Length == modifiedIds.Length && (currentIds.Length == 0 || currentIds.ToOneString() == modifiedIds.ToOneString())) return;

            // Získám pole klíčů všech QAT prvků:
            //  (QAT prvky mají tu vlastnost, že v jednom poli se nikdy nevyskytuje stejný klíč vícekrát = neexistují duplicity!!!)
            var allQatItems = DxQuickAccessToolbar.QATItems.ToList();

            // Nyní si založím pole s informacemi ChangeQatInfo, ve kterém shrnu potřebná data:
            int count = currentIds.Length;
            ChangeQatInfo[] changeQats = new ChangeQatInfo[count];
            for (int i = 0; i < currentIds.Length; i++)
            {
                string currentId = currentIds[i];
                ChangeQatInfo changeQat = new ChangeQatInfo()
                {
                    CurrentQatKey = currentId,
                    CurrentAllQatIndex = allQatItems.IndexOf(currentId),
                    IsDeleted = !modifiedIds.Any(m => m == currentId)
                };
                changeQats[i] = changeQat;
            }

            // Do těch prvků pole changeQats, které nejsou určeny ke smazání, vepíšu QatKey nových prvků = tím zajistím uložení změn pořadí prvků:
            int index = 0;
            foreach (var modifiedId in modifiedIds)
            {
                // Přeskočím prvky, které obsahují prvek, který se má smazat = jeho pozici nebudu obsazovat, ale na konci ji odeberu z DxQuickAccessToolbar.QATItems:
                while (index < count && changeQats[index].IsDeleted) index++;
                if (index >= count)
                    throw new InvalidOperationException($"DxRibbonControl.ChangeQatItems() error: not enough space in changeQats to store modifiedItems.");

                changeQats[index].ModifiedId = modifiedId;
                index++;
            }

            // Kontrola: každý vstupující prvek musí být buď vymazaný, anebo pojmenovaný:
            if (changeQats.Any(i => !i.IsDeleted && i.ModifiedId == null))
                throw new InvalidOperationException($"DxRibbonControl.ChangeQatItems() error: not defined ModifiedId for odl CurrentId.");

            // Nyní provedu nastřádané změny v poli allQatItems a uložím jej nazpátek:
            var changeList = changeQats.ToList();
            changeList.Sort((a, b) => b.CurrentAllQatIndex.CompareTo(a.CurrentAllQatIndex));       // Setřídím sestupně podle CurrentAllQatIndex, to pro snadnější odebírání prvků
            foreach (var changeItem in changeList)
            {
                if (changeItem.IsDeleted)
                    // Pokud určitý prvek má být odebrán, odebereme prvek na jeho indexu (jdeme sestupně, netřeba odčítávat již odebrané indexy):
                    allQatItems.RemoveAt(changeItem.CurrentAllQatIndex);
                else
                    // Na místě daného původního prvku bude nyní modifikovaný prvek:
                    allQatItems[changeItem.CurrentAllQatIndex] = changeItem.ModifiedId;
            }

            // Modifikované pole vložíme zpátky do správce, ten zajistí jeho promítnutí do všech aktivních Ribbonů i do Nephrite
            //  - pomocí eventu DxQuickAccessToolbar.ConfigValueChanged:
            DxQuickAccessToolbar.QATItems = allQatItems.ToArray();
        }
        /// <summary>
        /// Malá třída pro usnadnění modifikace QAT prvků
        /// </summary>
        private class ChangeQatInfo
        {
            /// <summary>
            /// QatKey stávajícího prvku
            /// </summary>
            public string CurrentQatKey;
            /// <summary>
            /// Index stávajícího prvku QatKey v poli <see cref="DxQuickAccessToolbar.QATItems"/>
            /// </summary>
            public int CurrentAllQatIndex;
            /// <summary>
            /// Obsahuje true, když prvek s tímto QatKey (<see cref="CurrentQatKey"/>) není přítomen v poli modifikovaný prvků = má být smazán
            /// </summary>
            public bool IsDeleted;
            /// <summary>
            /// QatKey prvku, který bude na této pozici <see cref="CurrentAllQatIndex"/> po změně = po změně pořadí prvků
            /// </summary>
            public string ModifiedId;
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"CurrentId: {CurrentQatKey}, at index [{CurrentAllQatIndex}]; " + (IsDeleted ? "IsDeleted" : "ModifiedId: " + ModifiedId);
            }
        }
        #endregion
        #region class QatItem : evidence pro jedno tlačítko QAT
        /// <summary>
        /// Třída pro průběžné shrnování informací o prvcích, které mají být umístěny do QAT (Quick Access Toolbar)
        /// </summary>
        protected class QatItem : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="qatKey"></param>
            public QatItem(DxRibbonControl owner, string qatKey)
            {
                this._Owner = owner;
                this._QatKey = qatKey;
            }
            /// <summary>
            /// Konstruktor pro existující <see cref="IRibbonItem"/>
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="iRibbonItem"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, IRibbonItem iRibbonItem, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, GetRibbonQATKey(iRibbonItem))
            {
                _BarItem = barItem;
                _BarItemLink = barItemLink;
                RibbonItem = iRibbonItem;
            }
            /// <summary>
            /// Konstruktor pro existující <see cref="IRibbonItem"/>
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="qatKey"></param>
            /// <param name="iRibbonItem"></param>
            /// <param name="iRibbonGroup"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, string qatKey, IRibbonItem iRibbonItem, IRibbonGroup iRibbonGroup, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, qatKey)
            {
                _BarItem = barItem;
                _BarItemLink = barItemLink;
                RibbonItem = iRibbonItem;
                RibbonGroup = iRibbonGroup;
            }
            /// <summary>
            /// Konstruktor pro existující <see cref="IRibbonGroup"/>
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="qatKey"></param>
            /// <param name="ribbonGroup"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, string qatKey, IRibbonGroup ribbonGroup, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, qatKey)
            {
                _BarItem = barItem;
                _BarItemLink = barItemLink;
                RibbonGroup = ribbonGroup;
            }
            /// <summary>
            /// Dispose: neodebírá prvky z Toolbaru, ale zahazuje všechny reference = fyzický Toolbar se nezmění, jen se od něj odpojíme.
            /// </summary>
            public void Dispose()
            {
                _Owner = null;
                _BarItem = null;
                _BarItemLink = null;
                RibbonItem = null;
                RibbonGroup = null;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string qatKey = DxRibbonControl.GetRibbonQATKey(RibbonItem);
                string text = $"QatKey: '{qatKey ?? _QatKey}'";
                if (RibbonItem != null) text += $"; Text: \"{RibbonItem.Text}\"";

                if (_BarItem != null) text += "; BarItem exists";
                if (_BarItemLink != null) text += "; BarLink exists";

                if (NeedChangeInQat) text += "; NeedChangeInQat";
                if (NeedAddToQat) text += "; NeedAddToQat";
                if (CanRemoveFromQat) text += "; CanRemoveFromQat";

                return text;
            }
            /// <summary>Vlastník = Ribbon</summary>
            private DxRibbonControl _Owner;
            /// <summary>Klíč</summary>
            private string _QatKey;
            /// <summary>
            /// Obsahuje klíč prvku nebo grupy. Není null, neobsahuje TAB ani mezery na okrajích.
            /// Odpovídá <see cref="IRibbonItem.QatKey"/> pokud je zadáno not null, anebo <see cref="ITextItem.ItemId"/>.
            /// </summary>
            public string QatKey { get { return _QatKey; } }
            /// <summary>
            /// Datová definice prvku Ribbonu.
            /// Zůstává zde uložen stále, i při Refreshi a Reloadu ribbonu. 
            /// Pouze pokud uživatel aktivně odebere prvek z QAT panelu (pomocí kontextového menu), pak je odsud odebrán (nullován) metodou <see cref="Reset"/>.
            /// </summary>
            public IRibbonItem RibbonItem { get; internal set; }
            /// <summary>
            /// Fyzický objekt Ribbonu.
            /// BarItem je prvek Ribbonu, a v případě potřeby (refreshe) je z instance QatItem odebrán.<br/>
            /// Setováním objektu do této property dojde k odstranění případně existujícího linku <see cref="BarItemLink"/> 
            /// (z fyzického ToolBaru, z paměti, z tohoto objektu).
            /// </summary>
            public DevExpress.XtraBars.BarItem BarItem 
            {
                get { return _BarItem; }
                set
                {
                    RemoveBarItemLink(false, false);
                    _BarItem = value;
                }
            }
            private DevExpress.XtraBars.BarItem _BarItem;
            /// <summary>
            /// Definice grupy Ribbonu
            /// </summary>
            public IRibbonGroup RibbonGroup { get; internal set; }
            /// <summary>
            /// Fyzický objekt Ribbonu typu Grupa.
            /// Setováním objektu do této property dojde k odstranění případně existujícího linku <see cref="BarItemLink"/> 
            /// (z fyzického ToolBaru, z paměti, z tohoto objektu).
            /// </summary>
            public DevExpress.XtraBars.Ribbon.RibbonPageGroup BarGroup
            {
                get { return _BarGroup; }
                set
                {
                    RemoveBarItemLink(false, false);
                    _BarGroup = value;
                }
            }
            private DevExpress.XtraBars.Ribbon.RibbonPageGroup _BarGroup;
            /// <summary>
            /// Link na object <see cref="BarItem"/> Ribbonu, tento Link je umístěn v QAT Toolbaru.
            /// Je vytvářen a odebírán podle potřeby.
            /// </summary>
            public DevExpress.XtraBars.BarItemLink BarItemLink { get { return _BarItemLink; } }
            private DevExpress.XtraBars.BarItemLink _BarItemLink;
            /// <summary>
            /// Obsahuje true, pokud this prvek potřebuje provést změnu v QAT ToolBaru.
            /// To je tehdy, když má fyzický <see cref="BarItem"/> nebo <see cref="BarGroup"/>, ale nemá link <see cref="BarItemLink"/> (pak je třeba Link přidat).
            /// Anebo opačně (je třeba Link odebrat).
            /// </summary>
            public bool NeedChangeInQat
            {
                get
                {
                    bool hasItem = (this.BarItem != null || this.BarGroup != null);
                    bool hasLink = (this.BarItemLink != null);
                    return (hasItem != hasLink);
                }
            }
            /// <summary>
            /// Obsahuje true, pokud this prvek by měl být zobrazen v QAT (tedy má vložen datový prvek <see cref="RibbonItem"/>),
            /// ale dosud nemá uloženou referenci na živý prvek Ribbonu v <see cref="BarItem"/>.
            /// Pak by si měl dohledat tento <see cref="BarItem"/> pomocí metody <see cref="SearchForBarItem"/>.
            /// </summary>
            public bool NeedSearchBarItem
            {
                get
                {
                    bool hasData = (this.RibbonItem != null);
                    bool hasItem = (this.BarItem != null || this.BarGroup != null);
                    return (hasData && !hasItem);
                }
            }
            /// <summary>
            /// Obsahuje true, když tento prvek máme fyzicky přidat do QAT. Tj. máme odpovídající BarItem, ale nemáme BarItemLink.
            /// </summary>
            public bool NeedAddToQat
            {
                get
                {
                    bool hasItem = (this.BarItem != null || this.BarGroup != null);
                    bool hasLink = (this.BarItemLink != null);
                    return (hasItem && !hasLink);
                }
            }
            /// <summary>
            /// Obsahuje true, když tento prvek můžeme odebrat z QAT. Tj. máme odpovídající BarItemLink. Bez ohledu na BarItem nebo BarGroup.
            /// </summary>
            public bool CanRemoveFromQat
            {
                get
                {
                    bool hasLink = (this.BarItemLink != null);
                    return (hasLink);
                }
            }
            /// <summary>
            /// Obsahuje true, když tento prvek reálně existuje v QAT. Tj. máme odpovídající BarItemLink.
            /// </summary>
            public bool IsInQAT
            {
                get
                {
                    bool hasLink = (this.BarItemLink != null);
                    return (hasLink);
                }
            }
            /// <summary>
            /// Pokusí se dohledat živý BarItem v Ribbonu pro zde přítomný datový prvek <see cref="RibbonItem"/>.
            /// </summary>
            /// <param name="qatBarItems">Aktuálně přítomné BarItemy + RibbonItemy v this Ribbonu, klíčem je QatKey. Smí být null.</param>
            public void SearchForBarItem(Dictionary<string, Tuple<BarItem, IRibbonItem>> qatBarItems)
            {
                if (!NeedSearchBarItem) return;

                bool found = false;
                if (qatBarItems != null)
                {   // Hledáme v Dictionary podle QatKey:
                    string qatKey = DxRibbonControl.GetValidQATKey(this.RibbonItem);
                    if (!String.IsNullOrEmpty(qatKey) && qatBarItems.TryGetValue(qatKey, out var barItem))
                    {   // Nalezený prvek obsahuje živou instanci BarItem:
                        this.BarItem = barItem.Item1;
                        found = true;
                    }
                }

                if (!found)
                {   // Hledáme podle RibbonItem.ItemId = BarItem.Name :
                    string itemId = this.RibbonItem.ItemId;
                    if (this._Owner.Items.TryGetFirst(b => b.Name == itemId, out var foundBarItem))
                        this.BarItem = foundBarItem;
                }
            }
            /// <summary>
            /// Metoda fyzicky přidá Link do QAT
            /// </summary>
            /// <param name="isGroupBegin">Nastavit v prvku BeginGroup = true?</param>
            public void AddLinkToQat(ref bool isGroupBegin)
            {
                this.RemoveBarItemLink(false, false);
                if (this.BarItem != null)
                {
                    var link = _Owner.Toolbar.ItemLinks.Add(this.BarItem);
                    if (isGroupBegin)
                    {
                        link.BeginGroup = true;
                        isGroupBegin = false;
                    }
                    ((IDxRibbonInternal)this._Owner).ModifyLinkForQat(link);
                    this._BarItemLink = link;
                }
            }
            /// <summary>
            /// Metoda zajistí, že pokud tento prvek je zobrazen ve fyzickém QAT, pak z něj bude odebrán, Disposován a z tohoto objektu vynulován.
            /// </summary>
            /// <param name="removeBarItemFromRibbon">Volitelně odebrat samotný prvek <see cref="BarItem"/> z Ribbonu</param>
            /// <param name="resetBarItem">Odebrat BarItem z tohoto objektu, tehdy když se už nebude příště generovat link do QAT (po Clear grupy)</param>
            public void RemoveBarItemLink(bool removeBarItemFromRibbon, bool resetBarItem)
            {
                if (_Owner != null && _Owner.Toolbar != null && _Owner.Toolbar.ItemLinks.Count > 0)
                {   // Pokud mám Ownera a jeho Toolbar, pak z Toolbaru odeberu Link na "náš" prvek:
                    _Owner.Toolbar.ItemLinks.RemoveWhere<BarItemLink>(l => isCurrentLink(l));
                }
                this._BarItemLink = null;                  // Tím zajistím, že já (QatItem) budu vědět, že můj prvek není v QAT toolbaru

                if (removeBarItemFromRibbon)
                {   // Vlastní BarItem má být odebrán a disposován?
                    var barItem = this.BarItem;
                    if (barItem != null)
                    {
                        if (_Owner.Items.Contains(barItem))
                            _Owner.Items.Remove(barItem);
                    }
                }
                if (resetBarItem)
                    this._BarItem = null;                  // Tím zajistím, že já (QatItem) budu vědět, že ke mě se neváže žádný BarItem.


                // Vrátí true, pokud dodaný testovací link (který pochází z _Owner.Toolbar.ItemLinks) odpovídá zdejšímu prvku.
                bool isCurrentLink(BarItemLink testLink)
                {
                    // NULL odeberu:
                    if (testLink is null) return true;

                    // Pokud this eviduje Link, a toto je on, pak jej odeberu:
                    var currentLink = this._BarItemLink;
                    if (currentLink != null && Object.ReferenceEquals(currentLink, testLink)) return true;

                    // Pokud this eviduje Item, a dodaný Link se odkazuje na něj, pak jej odeberu:
                    var currentItem = this._BarItem;
                    if (currentItem != null && Object.ReferenceEquals(currentItem, testLink.Item)) return true;

                    // Z testovaného Linku získám jeho Item, z něj získám data a přečtu jejich QatKey a porovnám s naším Key:
                    if (this._Owner.TryGetIRibbonData(testLink, out var _, out var ribbonItem, out var _, out var _))
                    {
                        var currentKey = this.QatKey;
                        var testQatKey = GetRibbonQATKey(ribbonItem);
                        if (String.Equals(currentKey, testQatKey, StringComparison.Ordinal)) return true;
                    }

                    return false;
                }
            }
            /// <summary>
            /// Zruší obsah this instance, 
            /// odebere prvek Link z reálného toolbaru QAT (pokud tam je) a Disposuje jej
            /// </summary>
            public void Reset()
            {
                RemoveBarItemLink(false, true);

                this._Owner = null;
                this.RibbonItem = null;
                this.RibbonGroup = null;
                this.BarItem = null;
            }
        }
        #endregion
        #endregion
        #region Kliknutí na prvek Ribbonu (včetně Grupy)
        private void InitEvents()
        {
            ApplicationButtonClick += RibbonControl_ApplicationButtonClick;
            ItemClick += _RibbonControl_ItemClick;
            PageCategoryClick += RibbonControl_PageCategoryClick;
            PageGroupCaptionButtonClick += RibbonControl_PageGroupCaptionButtonClick;
        }
        /// <summary>
        /// Uživatel kliknul na button aplikace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RibbonControl_ApplicationButtonClick(object sender, EventArgs e)
        {
            _ApplicationButtonClick();
        }
        /// <summary>
        /// Vyvolá reakce na kliknutí na button aplikace
        /// </summary>
        private void _ApplicationButtonClick()
        {
            OnRibbonApplicationButtonClick();
            RibbonApplicationButtonClick?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Proběhne po kliknutí na button aplikace
        /// </summary>
        protected virtual void OnRibbonApplicationButtonClick() { }
        /// <summary>
        /// Událost volaná po kliknutí na button aplikace
        /// </summary>
        public event EventHandler RibbonApplicationButtonClick;

        /// <summary>
        /// Uživatel kliknul na záhlaví kategorie = barevný pruh nad barevnými stránkami kategorií.
        /// Ten pruh je vidět jen v Ribbonu umístěném nahoře na formuláři RibbonForm, není vidět na Ribbonu umístěném v panelu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RibbonControl_PageCategoryClick(object sender, DevExpress.XtraBars.Ribbon.PageCategoryClickEventArgs e)
        {
            if (_TryGetIRibbonCategory(e.Category, out IRibbonCategory iRibbonCategory, out DxRibbonControl dxRibbon))
                _RibbonPageCategoryClick(iRibbonCategory, dxRibbon);
        }
        /// <summary>
        /// Vyvolá reakce na kliknutí na záhlaví kategorie:
        /// event <see cref="RibbonItemClick"/>.
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <param name="ownerDxRibbon">Ribbon, který jako první deklaroval tuto kategorii</param>
        private void _RibbonPageCategoryClick(IRibbonCategory iRibbonCategory, DxRibbonControl ownerDxRibbon = null)
        {
            if (ownerDxRibbon != null)                                    // Nyní jsme v instanci, kde je zrovna vidět daná kategorie - ale událost máme řešit v té instanci Ribbonu...
                ownerDxRibbon._RibbonPageCategoryClick(iRibbonCategory);  //  ... kde byla grupa definována = tam je navázaný patřičný eventhandler!
            else
            {
                if (this.IsActive)
                {
                    var args = new TEventArgs<IRibbonCategory>(iRibbonCategory);
                    OnRibbonPageCategoryClick(args);
                    RibbonPageCategoryClick?.Invoke(this, args);
                }
            }
        }
        /// <summary>
        /// Proběhne po kliknutí na kategorii Ribbonu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRibbonPageCategoryClick(TEventArgs<IRibbonCategory> args) { }
        /// <summary>
        /// Událost volaná po kliknutí na kategorii Ribbonu
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonCategory>> RibbonPageCategoryClick;

        /// <summary>
        /// Uživatel kliknul na GroupButton = tlačítko v grupě v Ribbonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RibbonControl_PageGroupCaptionButtonClick(object sender, DevExpress.XtraBars.Ribbon.RibbonPageGroupEventArgs e)
        {
            if (_TryGetIRibbonGroup(e.PageGroup, out IRibbonGroup iRibbonGroup, out DxRibbonControl dxRibbon))
                _RibbonGroupButtonClick(iRibbonGroup, dxRibbon);
        }
        /// <summary>
        /// Vyvolá reakce na kliknutí na GroupButton = tlačítko v grupě v Ribbonu:
        /// event <see cref="RibbonGroupButtonClick"/>.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="ownerDxRibbon">Ribbon, který jako první deklaroval tuto kategorii</param>
        private void _RibbonGroupButtonClick(IRibbonGroup iRibbonGroup, DxRibbonControl ownerDxRibbon = null)
        {
            if (ownerDxRibbon != null)                                    // Nyní jsme v instanci, kde je zrovna vidět daná grupa - ale událost máme řešit v té instanci Ribbonu...
                ownerDxRibbon._RibbonGroupButtonClick(iRibbonGroup);      //  ... kde byla grupa definována = tam je navázaný patřičný eventhandler!
            else
            {
                if (this.IsActive)
                {
                    var args = new TEventArgs<IRibbonGroup>(iRibbonGroup);
                    OnRibbonGroupButtonClick(args);
                    RibbonGroupButtonClick?.Invoke(this, args);
                }
            }
        }
        /// <summary>
        /// Proběhne po kliknutí na GroupButton = tlačítko v grupě v Ribbonu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRibbonGroupButtonClick(TEventArgs<IRibbonGroup> args) { }
        /// <summary>
        /// Událost volaná po kliknutí na GroupButton = tlačítko v grupě v Ribbonu
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonGroup>> RibbonGroupButtonClick;

        /// <summary>
        /// Uživatel kliknul na kterýkoli button v Ribbonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RibbonControl_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!this.IsActive) return;

            // Prvky deklarované v DevExpress vůbec nebudu odchytávat:
            if (_IsDxRibbonItem(e.Item)) return;

            // poznámka: tady nemusím řešit přechod z Ribbonu "kde se kliklo" do Ribbonu "kde byl prvek definován", tady to už interně vyřešil DevExpress!
            // Tady jsem v té instanci Ribbonu, která deklarovala BarItem a navázala do něj svůj Click eventhandler...
            if (_TryGetIRibbonItem(e.Item, out IRibbonItem iRibbonItem))
            {
                if (DelayIsValidForClick(iRibbonItem, e.Item, true))
                {
                    var dxArgs = _SearchItemClickInfo(iRibbonItem, e);
                    _RibbonItemTestCheckChanges(e.Item, dxArgs);
                    _RibbonItemClick(dxArgs);
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek je specifický DevExpress a nebudeme tedy řešit jeho eventy
        /// </summary>
        /// <param name="barItem"></param>
        /// <returns></returns>
        private static bool _IsDxRibbonItem(DevExpress.XtraBars.BarItem barItem)
        {
            if (barItem is null) return true;

            if ((barItem is DevExpress.XtraBars.SkinDropDownButtonItem) ||
                (barItem is DevExpress.XtraBars.SkinPaletteDropDownButtonItem) ||
                (barItem is DevExpress.XtraBars.SkinPaletteRibbonGalleryBarItem))
                return true;

            return false;
        }
        /// <summary>
        /// Uživatel kliknul na prvek Ribbonu, který sám určil <see cref="IRibbonItem"/> a předává nám jej.
        /// Typicky sem chodíme z ComboBoxu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dxArgs"></param>
        private void _RibbonItem_ItemClick(object sender, DxRibbonItemClickArgs dxArgs)
        {
            if (!this.IsActive) return;

            // poznámka: tady nemusím řešit přechod z Ribbonu "kde se kliklo" do Ribbonu "kde byl prvek definován", tady to už interně vyřešil DevExpress!
            // Tady jsem v té instanci Ribbonu, která deklarovala BarItem a navázala do něj svůj Click eventhandler...
            if (dxArgs.Item != null)
            {
                _RibbonItemClick(dxArgs);
            }
        }
        /// <summary>
        /// Metoda dohledá informace o tom, ve kterém místě se nachází prvek Ribbonu, na který bylo kliknuto; 
        /// a vloží vše do <see cref="DxRibbonItemClickArgs"/>.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="eArgs"></param>
        /// <returns></returns>
        private DxRibbonItemClickArgs _SearchItemClickInfo(IRibbonItem iRibbonItem, DevExpress.XtraBars.ItemClickEventArgs eArgs)
        {
            if (eArgs.Link is null)
                // Není žádný vizuální Link => je to HotKey:
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.HotKey);

            if (eArgs.Link.LinkedObject is null)
                // Link je, ale nemá LinkedObject => nevíme:
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.Other);

            if (eArgs.Link.LinkedObject is DevExpress.XtraBars.Ribbon.RibbonQuickToolbarItemLinkCollection qats)
                // QAT:
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.QatPanel);

            if (eArgs.Link.LinkedObject is DevExpress.XtraBars.Ribbon.RibbonPageGroupItemLinkCollection group)
            {   // Button přímo v grupě:
                string groupId = group.PageGroup?.Name;
                if (groupId != null && groupId.Length > 3 && groupId.StartsWith("G_#")) groupId = groupId.Substring(3);
                string pageId = group.PageGroup?.Page?.Name;
                if (pageId != null && pageId.Length > 3 && pageId.StartsWith("P_#")) pageId = pageId.Substring(3);
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.RibbonGroup, pageId, groupId);
            }

            if (eArgs.Link.LinkedObject is DevExpress.XtraBars.Ribbon.RibbonStatusBarItemLinkCollection status)
            {   // Prevk StatusBaru:
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.StatusBar);
            }

            if (eArgs.Link.LinkedObject is DevExpress.XtraBars.Ribbon.RibbonCaptionBarItemLinkCollection caption)
            {   // Prevk StatusBaru:
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.CaptionPanel);
            }

            if (eArgs.Link.LinkedObject is DevExpress.XtraBars.BarItem barItem)
            {   // SubItem v Menu/atd:
                var name = barItem.Name;
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.RibbonGroup, null, null);
            }

            if (eArgs.Link.LinkedObject is DevExpress.XtraBars.PopupMenu popup)
                // Vyhledání v Ribbonu:
                return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.SearchCombo);

            return new DxRibbonItemClickArgs(iRibbonItem, DxRibbonItemClickArea.Other);
        }
        /// <summary>
        /// Tato metoda řeší změnu hodnoty <see cref="ITextItem.Checked"/> na daném prvku Ribbonu poté, kdy na něj uživatel klikl.
        /// Řeší tedy CheckBoxy, RadioButtony a CheckButtony obou režimů.
        /// U obou typů RadioButtonů řeší zhasnutí okolních (=ne-kliknutých) RadioButtonů v jejich grupě.
        /// <para/>
        /// Volá event <see cref="RibbonItemCheck"/> a související pro každý prvek, jemuž je změněna hodnota Checked.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="dxArgs"></param>
        private void _RibbonItemTestCheckChanges(BarItem barItem, DxRibbonItemClickArgs dxArgs)
        {
            if (!this.IsActive) return;

            var itemType = dxArgs.Item.ItemType;
            // Z vizuálního objektu BarCheckItem si opíšu jeho Checked do datového objektu, anebo pro pasivní prvky vrátím stav:
            bool isPasive = (itemType == RibbonItemType.CheckButtonPassive || itemType == RibbonItemType.CheckBoxPasive);
            if (barItem is BarCheckItem checkItem)
            {   // CheckBox i RadioButton (jde o stejný prvek, pouze s jiným stylem zobrazení):
                _RibbonCheckBoxItemClick(checkItem, dxArgs, isPasive);
            }
            else if ((itemType == RibbonItemType.CheckButton || itemType == RibbonItemType.CheckButtonPassive) && barItem is BarBaseButtonItem barButton)
            {   // BarButton v obou režimech:
                _RibbonCheckButtonItemClick(barButton, dxArgs, isPasive);
            }
        }
        /// <summary>
        /// Vyřeší stavy BarCheckItem po kliknutí na něj. 
        /// Zpracuje režim CheckBox (samostaný CheckButton) i RadioButton (skupinový CheckButton).
        /// </summary>
        /// <param name="checkButton"></param>
        /// <param name="dxArgs"></param>
        /// <param name="isPasive">Pasivní chování = do viditelného prvku vrátit stav z datového, a neřešit návaznosti</param>
        private void _RibbonCheckBoxItemClick(BarCheckItem checkButton, DxRibbonItemClickArgs dxArgs, bool isPasive)
        {
            var iRibbonItem = dxArgs.Item;
            bool itemChecked = (iRibbonItem.Checked ?? false);
            if (isPasive)
            {   // Pasivní prvek: do Buttonu i do IRibbonItem vepíšu hodnotu Checked z datového objektu = beze změny:
                _RibbonItemSetChecked(dxArgs, itemChecked, true, false, null, true, checkButton);
            }
            else if (String.IsNullOrEmpty(iRibbonItem.RadioButtonGroupName))
            {   // Není daná grupa RadioButtonGroupName? Jde o obyčejný CheckBox:
                _RibbonItemSetChecked(dxArgs, !itemChecked, true, false, null, true, checkButton);
            }
            else
            {   // Máme řešit Radio grupu:
                _RibbonRadioButtonItemClick(dxArgs, true, false, true);
            }
        }
        /// <summary>
        /// Vyřeší stavy CheckButtonu po kliknutí na něj. 
        /// Zpracuje režim CheckBox (samostaný CheckButton) i RadioButton (skupinový CheckButton).
        /// </summary>
        /// <param name="barButton"></param>
        /// <param name="dxArgs"></param>
        /// <param name="isPasive">Pasivní chování = do viditelného prvku vrátit stav z datového, a neřešit návaznosti</param>
        private void _RibbonCheckButtonItemClick(BarBaseButtonItem barButton, DxRibbonItemClickArgs dxArgs, bool isPasive)
        {
            var iRibbonItem = dxArgs.Item;
            bool itemChecked = (iRibbonItem.Checked ?? false);
            if (isPasive)
            {   // Pasivní prvek: do Buttonu i do IRibbonItem vepíšu hodnotu Checked z datového objektu = beze změny:
                _RibbonItemSetChecked(dxArgs, itemChecked, true, true, barButton, false, null);
            }
            else if (String.IsNullOrEmpty(iRibbonItem.RadioButtonGroupName))
            {   // Není daná grupa RadioButtonGroupName? Jde o obyčejný CheckBox:
                _RibbonItemSetChecked(dxArgs, !itemChecked, true, true, barButton, false, null);
            }
            else
            {   // Máme řešit Radio grupu:
                _RibbonRadioButtonItemClick(dxArgs, true, true, false);
            }
        }
        /// <summary>
        /// Metoda najde všechny prvky jedné Radiogrupy (se shodným <see cref="IRibbonItem.RadioButtonGroupName"/> jako má dodaný prvek v <paramref name="dxArgs"/>),
        /// a pro všechny prvky této grupy jiné než dodaný v <paramref name="dxArgs"/> nastaví jejich Checked = false,
        /// a poté pro dodaný prvek nastaví jeho Checked = true.
        /// <para/>
        /// Podle požadavku volá event změny pro každý dotčený prvek, a nastaví do vizuálního prvku stav down a Checked.
        /// </summary>
        /// <param name="dxArgs"></param>
        /// <param name="callEvent"></param>
        /// <param name="setDownState"></param>
        /// <param name="setChecked"></param>
        private void _RibbonRadioButtonItemClick(DxRibbonItemClickArgs dxArgs, bool callEvent, bool setDownState, bool setChecked)
        {
            // Získáme všechny prvky té skupiny RadioButtonGroupName, na jejíhož člena se kliklo:
            var iRibbonItem = dxArgs.Item;
            var groupName = iRibbonItem.RadioButtonGroupName;
            var groupItems = iRibbonItem.ParentGroup?.Items.Where(i => (!String.IsNullOrEmpty(i.RadioButtonGroupName) && i.RadioButtonGroupName == groupName)).ToArray();
            if (groupItems is null || groupItems.Length == 0) return;

            // Nejprve nastavím všechny ostatní prvky (nikoli ten aktivní) na Checked = false:
            groupItems.Where(i => !Object.ReferenceEquals(i, iRibbonItem)).ForEachExec(i => _RibbonItemSetChecked(new DxRibbonItemClickArgs(i), false, callEvent, setDownState, null, setChecked, null));

            // A až poté nastavím ten jeden aktivní na Checked = true:
            _RibbonItemSetChecked(dxArgs, true, callEvent, setDownState, null, setChecked, null);
        }
        /// <summary>
        /// Do daného datového prvku v <paramref name="dxArgs"/> vloží danou hodnotu Checked <paramref name="isChecked"/>.
        /// Pokud je požadováno nastavení hodnoty <see cref="BarBaseButtonItem.Down"/> pro odpovídajcí vizuální prvek, provede to 
        /// (může být předán button v parametru <paramref name="barButton"/>, anebo bude vyhledán v <see cref="IRibbonItem.RibbonItem"/>).
        /// Pokud došlo ke změně hodnoty IsChecked v <paramref name="dxArgs"/> a pokud je požadováno v <paramref name="callEvent"/>, 
        /// vyvolá se událost <see cref="RibbonItemCheck"/> a související.
        /// </summary>
        /// <param name="dxArgs"></param>
        /// <param name="isChecked"></param>
        /// <param name="callEvent"></param>
        /// <param name="setDownState"></param>
        /// <param name="barButton"></param>
        /// <param name="setChecked"></param>
        /// <param name="checkButton"></param>
        private void _RibbonItemSetChecked(DxRibbonItemClickArgs dxArgs, bool isChecked, bool callEvent, bool setDownState, BarBaseButtonItem barButton, bool setChecked, BarCheckItem checkButton)
        {
            var iRibbonItem = dxArgs.Item;
            bool oldChecked = (iRibbonItem.Checked ?? false);
            bool isChanged = (isChecked != oldChecked);
            iRibbonItem.Checked = isChecked;
            if (setDownState)
            {
                if (barButton is null) barButton = iRibbonItem.RibbonItem as BarButtonItem;
                if (barButton != null)
                {
                    barButton.Down = isChecked;                      // Nativní eventu DownChanged nehlídáme, tak mi nevadí že proběhne.
                    RibbonItemSetImageByChecked(iRibbonItem, barButton);
                }
            }
            if (setChecked)
            {
                if (checkButton is null) checkButton = iRibbonItem.RibbonItem as BarCheckItem;
                if (checkButton != null)
                {
                    checkButton.Checked = isChecked;                 // Nativní eventu CheckedChanged nehlídáme, tak mi nevadí že proběhne.
                    RibbonItemSetImageByChecked(iRibbonItem, checkButton);
                }
            }
            if (callEvent && isChanged)
                _RibbonItemCheck(dxArgs);
        }

        /// <summary>
        /// Provede akci odpovídající kliknutí na prvek Ribbonu, na vstupu jsou data prvku
        /// </summary>
        /// <param name="dxArgs"></param>
        internal void RaiseRibbonItemClick(DxRibbonItemClickArgs dxArgs) { _RibbonItemClick(dxArgs); }
        /// <summary>
        /// Vyvolá reakce na kliknutí na prvek Ribbonu:
        /// event <see cref="RibbonItemClick"/>.
        /// </summary>
        /// <param name="dxArgs"></param>
        private void _RibbonItemClick(DxRibbonItemClickArgs dxArgs)
        {
            if (!this.IsActive) return;

            dxArgs.Item?.ClickAction?.Invoke(dxArgs.Item);
            OnRibbonItemClick(dxArgs);
            RibbonItemClick?.Invoke(this, dxArgs);
        }
        /// <summary>
        /// Proběhne po kliknutí na prvek Ribbonu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRibbonItemClick(DxRibbonItemClickArgs args) { }
        /// <summary>
        /// Událost volaná po kliknutí na prvek Ribbonu
        /// </summary>
        public event EventHandler<DxRibbonItemClickArgs> RibbonItemClick;

        /// <summary>
        /// Provede akci odpovídající změně hodnoty <see cref="ITextItem.Checked"/> tohoto prvku Ribbonu.
        /// V době volání této akce už je hodnota změněna. Volající garantuje, že skutečně došlo ke změně.
        /// </summary>
        /// <param name="dxArgs"></param>
        internal void RaiseRibbonItemCheck(DxRibbonItemClickArgs dxArgs) { _RibbonItemCheck(dxArgs); }
        /// <summary>
        /// Vyvolá reakce na změnu hodnoty <see cref="ITextItem.Checked"/> tohoto prvku Ribbonu.
        /// V době volání této akce už je hodnota změněna. Volající garantuje, že skutečně došlo ke změně.
        /// event <see cref="RibbonItemCheck"/>.
        /// </summary>
        /// <param name="dxArgs"></param>
        private void _RibbonItemCheck(DxRibbonItemClickArgs dxArgs)
        {
            dxArgs.Item?.ClickAction?.Invoke(dxArgs.Item);
            OnRibbonItemCheck(dxArgs);
            RibbonItemCheck?.Invoke(this, dxArgs);
        }
        /// <summary>
        /// Proběhne po změně hodnoty <see cref="ITextItem.Checked"/> tohoto prvku Ribbonu.
        /// V době volání této akce už je hodnota změněna. Volající garantuje, že skutečně došlo ke změně.
        /// </summary>
        /// <param name="dxArgs"></param>
        protected virtual void OnRibbonItemCheck(DxRibbonItemClickArgs dxArgs) { }
        /// <summary>
        /// Událost volaná po změně hodnoty <see cref="ITextItem.Checked"/> tohoto prvku Ribbonu.
        /// V době volání této akce už je hodnota změněna. Volající garantuje, že skutečně došlo ke změně.
        /// </summary>
        public event EventHandler<DxRibbonItemClickArgs> RibbonItemCheck;

        /// <summary>
        /// V rámci dané kategorie se pokusí najít odpovídající definici kategorie <see cref="IRibbonCategory"/> v některém tagu.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="iRibbonCategory"></param>
        /// <param name="definingRibbon"></param>
        /// <returns></returns>
        private bool _TryGetIRibbonCategory(DevExpress.XtraBars.Ribbon.RibbonPageCategory category, out IRibbonCategory iRibbonCategory, out DxRibbonControl definingRibbon)
        {
            iRibbonCategory = null;
            definingRibbon = null;
            if (category == null) return false;

            if (category.Pages.Count == 0 && category.MergedPages.Count == 0)
            {   // DevExpress mají chybku: pokud uživatel klikne na záhlaví kategorie po mergování Child Ribbonu a před tím kliknutím na kategorii nepřepne aktivní Page,
                //  pak ve zdejším parametru (tj. v eventu PageCategoryClick v parametru DevExpress.XtraBars.Ribbon.PageCategoryClickEventArgs) je sice Category,
                //  ale ta má category.Pages.Count = 0 i category.MergedPages.Count = 0, a já pak nenajdu stránku dané kategorie a ta ani nenajdu definující IRibbonItem.
                // Ale když požádám Ribbon o GetPageCategories(), pak si tam najdu odpovídající kategorii, tak už má Pages správně naplněné:
                var categories = this.GetPageCategories();
                category = categories.FirstOrDefault(c => c.Name == category.Name);
                if (category == null) return false;
            }

            _TryGetDxRibbon(category, out definingRibbon); 

            if (category.Tag is IRibbonCategory iRibbonItem) { iRibbonCategory = iRibbonItem; return true; }

            IRibbonPage iRibbonPage;
            if (_TryGetIRibbonPage(category.Pages, out iRibbonPage)) { iRibbonCategory = iRibbonPage.Category; return (iRibbonCategory != null); }
            if (_TryGetIRibbonPage(category.MergedPages, out iRibbonPage)) { iRibbonCategory = iRibbonPage.Category; return (iRibbonCategory != null); }

            return false;
        }
        /// <summary>
        /// V rámci dané grupy se pokusí najít odpovídající definici grupy <see cref="IRibbonGroup"/> v některém tagu.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="iRibbonGroup"></param>
        /// <param name="definingRibbon"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonGroup(DevExpress.XtraBars.Ribbon.RibbonPageGroup group, out IRibbonGroup iRibbonGroup, out DxRibbonControl definingRibbon)
        {
            iRibbonGroup = null;
            definingRibbon = null;
            if (group == null) return false;
            _TryGetDxRibbon(group, out definingRibbon);
            if (group is DxRibbonGroup dxGroup) { iRibbonGroup = dxGroup.DataGroupLast; return true; }       // Čistá cesta
            if (group.Tag is IRibbonGroup iDataGroup) { iRibbonGroup = iDataGroup; return true; }            // Nouzová cesta
            if (_TryGetIRibbonItem(group.ItemLinks, out var iMenuItem)) { iRibbonGroup = iMenuItem.ParentGroup; return (iRibbonGroup != null); }
            return false;
        }
        /// <summary>
        /// V rámci daného prvku se pokusí najít odpovídající definici prvku <see cref="IRibbonItem"/> v jeho Tagu.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonItem(DevExpress.XtraBars.BarItem item, out IRibbonItem iRibbonItem)
        {
            iRibbonItem = null;
            if (item == null) return false;
            if (item.Tag is BarItemTagInfo itemInfo) { iRibbonItem = itemInfo.Data; return true; }
            if (item.Tag is IRibbonItem found) { iRibbonItem = found; return true; }
            return false;
        }
        /// <summary>
        /// V rámci daných stránek se pokusí najít odpovídající definici stránky <see cref="IRibbonPage"/> v některém tagu.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="iRibbonPage"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonPage(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IRibbonPage iRibbonPage)
        {
            iRibbonPage = null;
            if (pages == null) return false;
            var found = pages.Select(p => p.Tag)
                             .OfType<IRibbonPage>()
                             .FirstOrDefault();
            if (found == null) return false;
            iRibbonPage = found;
            return true;
        }
        /// <summary>
        /// V rámci daných stránek se pokusí najít první definici grupy typu <see cref="IRibbonGroup"/> v tagu skupiny.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="iRibbonGroup"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonGroup(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IRibbonGroup iRibbonGroup)
        {
            iRibbonGroup = null;
            if (pages == null) return false;
            var groups = pages.SelectMany(p => p.Groups).ToArray();
            var iDataGroup = groups.OfType<DxRibbonGroup>().FirstOrDefault()?.DataGroupLast;                 // Čistá cesta
            if (iDataGroup is null)
                iDataGroup = groups.Select(g => g.Tag).OfType<IRibbonGroup>().FirstOrDefault();              // Nouzová cesta
            if (iDataGroup is null) return false;
            iRibbonGroup = iDataGroup;
            return true;
        }
        /// <summary>
        /// V rámci daných stránek se pokusí najít odpovídající definici prvního prvku <see cref="IRibbonItem"/> v tagu Item.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonItem(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IRibbonItem iRibbonItem)
        {
            iRibbonItem = null;
            if (pages == null) return false;
            var found = pages.SelectMany(p => p.Groups)
                             .SelectMany(g => g.ItemLinks)
                             .Select(i => i.Item.Tag)
                             .OfType<BarItemTagInfo>()
                             .Select(i => i.Data)
                             .FirstOrDefault();
            if (found == null) return false;
            iRibbonItem = found;
            return true;
        }
        /// <summary>
        /// V rámci dodané kolekce linků na BarItem najde první, který ve svém Tagu nese <see cref="IRibbonItem"/> (=definice prvku).
        /// </summary>
        /// <param name="barItemLinks"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private static bool _TryGetIRibbonItem(IEnumerable<DevExpress.XtraBars.BarItemLink> barItemLinks, out IRibbonItem iRibbonItem)
        {
            iRibbonItem = null;
            if (barItemLinks == null) return false;
            foreach (DevExpress.XtraBars.BarItemLink link in barItemLinks)
            {
                if (link.Item.Tag is BarItemTagInfo itemInfo)
                {
                    iRibbonItem = itemInfo.Data;
                    return true;
                }
                if (link.Item.Tag is IRibbonItem found)
                {
                    iRibbonItem = found;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Metoda zkusí najít <see cref="DxRibbonControl"/>, který první přispěl ke vzniku dané kategorie
        /// </summary>
        /// <param name="category"></param>
        /// <param name="definingRibbon"></param>
        private static bool _TryGetDxRibbon(DevExpress.XtraBars.Ribbon.RibbonPageCategory category, out DxRibbonControl definingRibbon)
        {
            definingRibbon = null;
            if (category == null) return false;
            definingRibbon = _SearchRibbonInPages(category.Pages);
            if (definingRibbon == null)
                definingRibbon = _SearchRibbonInPages(category.MergedPages);
            return (definingRibbon != null);
        }
        /// <summary>
        /// Metoda zkusí najít <see cref="DxRibbonControl"/>, který první přispěl ke vzniku dané grupy
        /// </summary>
        /// <param name="group"></param>
        /// <param name="definingRibbon"></param>
        private static bool _TryGetDxRibbon(DevExpress.XtraBars.Ribbon.RibbonPageGroup group, out DxRibbonControl definingRibbon)
        {
            definingRibbon = null;
            if (group == null) return false;

            // Najdu všechny BarItem v naší grupě:
            var items = group.ItemLinks
                         .Select(l => l.Item)
                         .OfType<DevExpress.XtraBars.BarItem>();
            // Najdu první DxRibbonControl, který grupu deklaroval (ono jich ale může být víc, pokud grupa je mergovaná):
            definingRibbon = _SearchRibbonInItems(items);
            return (definingRibbon != null);
        }
        /// <summary>
        /// Metoda zkusí najít <see cref="DxRibbonControl"/>, který deklaroval daný prvek
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="definingRibbon"></param>
        private static bool _TryGetDxRibbon(DevExpress.XtraBars.BarItem barItem, out DxRibbonControl definingRibbon)
        {
            if (barItem != null && (barItem.Manager is DevExpress.XtraBars.Ribbon.RibbonBarManager manager) && (manager.Ribbon is DxRibbonControl dxRibbonControl))
            {
                definingRibbon = dxRibbonControl;
                return true;
            }
            definingRibbon = null;
            return false;
        }
        /// <summary>
        /// V daných stránkách vyhledá prvky a jejich nativní Ribbon = ten, který je eviduje na svých vlastních stránkách.
        /// Hledá tedy takový (Child) mergovaný Ribbon, který svoje vlastní stránky mergoval kamsi nahoru.
        /// </summary>
        /// <param name="pages"></param>
        /// <returns></returns>
        private static DxRibbonControl _SearchRibbonInPages(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages)
        {
            var items = pages
                         .SelectMany(p => p.Groups)
                         .SelectMany(g => g.ItemLinks)
                         .Select(l => l.Item)
                         .OfType<DevExpress.XtraBars.BarItem>();
            return _SearchRibbonInItems(items);
        }
        /// <summary>
        /// V daných prvcích vyhledá jejich nativní Ribbon = ten, který je eviduje na svých vlastních stránkách.
        /// Hledá tedy takový (Child) mergovaný Ribbon, který svoje vlastní stránky mergoval kamsi nahoru.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private static DxRibbonControl _SearchRibbonInItems(IEnumerable<DevExpress.XtraBars.BarItem> items)
        {
            var ribbon = items
                         .Select(i => i.Manager)
                         .OfType<DevExpress.XtraBars.Ribbon.RibbonBarManager>()
                         .Select(m => m.Ribbon)
                         .OfType<DxRibbonControl>()
                         .FirstOrDefault();
            return ribbon;
        }
        #endregion
        #region Button Click + Delay ( Disable / Enable )
        /// <summary>
        /// Metoda zajistí nulování intervalu Delay pro daný prvek Ribbonu.
        /// Volá se typicky v procesu inicializace / refreshe dat pro BarItem.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="barItem"></param>
        protected void DelayResetForce(IRibbonItem iRibbonItem, BarItem barItem)
        {
            iRibbonItem.DelayLastClickTime = null;
            iRibbonItem.DelayTimerGuid = null;
            if (barItem.Enabled != iRibbonItem.Enabled)
            {
                barItem.Enabled = iRibbonItem.Enabled;
                this.RefreshBarItem(barItem);
            }
        }
        /// <summary>
        /// Provede Refresh daného BarItem, včetně jeho Ribbonu a včetně Parent Ribbonů
        /// </summary>
        /// <param name="barItem"></param>
        protected void RefreshBarItem(BarItem barItem)
        {
            if (barItem is null) return;
            barItem.Refresh();
            if (_TryGetDxRibbon(barItem, out var defRibbon))
            {
                var upRibbons = defRibbon.MergedRibbonsUp;
                foreach (var upRibbon in upRibbons)
                    upRibbon.Item1.Refresh();
            }
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný prvek Ribbonu může akceptovat kliknutí z hlediska Delay.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="temporaryDisable">Pokud je prvek nastaven jako Delay, a bude akceptován jeho Click, pak na něm nastavit Disable po dobu jeho Delay intervalu</param>
        /// <returns></returns>
        protected bool DelayIsValidForClick(IRibbonItem iRibbonItem, BarItem barItem, bool temporaryDisable)
        {
            // Pokud delay není dané, anebo je 0 či záporné, pak kliknutí akceptuji bez problému:
            var delay = iRibbonItem.DelayMilisecBeforeNextClick ?? 0;
            if (delay <= 0) return true;

            // Pokud předchozí kliknutí dosud nebylo (DelayLastClickTime je null = výchozí), anebo od posledního kliknutní uběhla přinejmenším stanovená doba,
            //  pak si nastavím ochranný čas pro příští kliknutí, a aktuální kliknutí akceptuji:
            
            // Pokud máme čas minulého kliknutí, a uplynulý čas do teď je menší než stanovená doba delay, pak nemůžeme nové kliknutí akceptovat:
            var last = iRibbonItem.DelayLastClickTime;
            var now = DateTime.Now;
            if (last.HasValue && (((TimeSpan)(now - last.Value)).TotalMilliseconds < delay))
            {
                return false;
            }

            // Prvek sice má stanovený čas Delay, ale buď jde o prvoklik, anebo o klik po uplynutí stanoveného času. Kliknutí tedy lze akceptovat.
            if (temporaryDisable)
            {   // Nicméně je tady ten Delay, proto prvek BarItem nyní dáme Disabled, a nastavíme časovač (WatchTimer) na daný čas, abychom po jeho uplynutí znovu nastavili Enabled = true:
                barItem.Enabled = false;
                iRibbonItem.DelayLastClickTime = now;
                iRibbonItem.DelayTimerGuid = WatchTimer.CallMeAfter(DelayTimeElapsed, barItem, delay, true, iRibbonItem.DelayTimerGuid);
            }
            return true;
        }
        /// <summary>
        /// Metoda volaná po dosažení stanoveného času Delay
        /// </summary>
        /// <param name="param"></param>
        protected void DelayTimeElapsed(object param)
        {
            if (this.IsDisposed || this.Disposing) return;

            if (param is BarItem barItem)
            {
                if (_TryGetIRibbonItem(barItem, out IRibbonItem iRibbonItem))
                {
                    DelayResetForce(iRibbonItem, barItem);
                }
                else
                {
                    barItem.Enabled = true;
                }
            }
        }
        #endregion
        #region Mergování, Unmergování, podpora pro ReMerge (Unmerge - Modify - Merge back)
        /// <summary>
        /// Při Mergování Child Ribbonu provést aktivaci jeho (Child) aktivní stránky i v zdejším (Parent) Ribbonu.
        /// Výchozí = true, jde o vizuálně lepší chování.
        /// </summary>
        public bool SelectChildActivePageOnMerge { get; set; }
        /// <summary>
        /// Parent Ribbon, do něhož je this Ribbon aktuálně mergován.
        /// </summary>
        public DxRibbonControl MergedIntoParentDxRibbon { get; protected set; }
        /// <summary>
        /// Obsahuje true, pokud this Ribbon je mergován do nějakého parenta <see cref="MergedIntoParentDxRibbon"/>
        /// </summary>
        protected bool IsMergedIntoDxParent { get { return (MergedIntoParentDxRibbon != null); } }
        /// <summary>
        /// Aktuálně mergovaný Child <see cref="DxRibbonControl"/>
        /// </summary>
        public DxRibbonControl MergedChildDxRibbon { get { return (MergedChildRibbon as DxRibbonControl); } }
        /// <summary>
        /// Obsahuje true, pokud do this Ribbonu je mergován nějaký Child Ribbon <see cref="MergedChildDxRibbon"/>
        /// </summary>
        protected bool HasMergedChildDxParent { get { return (MergedIntoParentDxRibbon != null); } }
        /// <summary>
        /// Aktuálně nejvyšší Ribbon = ten, ve kterém se zobrazují data z this Ribbonu.
        /// Tedy: pokud this není Merged, pak zde je this.
        /// Pokud this je Merged, pak zde je nejvyšší <see cref="MergedIntoParentDxRibbon"/>.
        /// </summary>
        public DxRibbonControl TopRibbonControl { get { return ((this.MergedIntoParentDxRibbon != null) ? (this.MergedIntoParentDxRibbon.TopRibbonControl) : this); } }
        /// <summary>
        /// Aktuálně používaný BarManager = z TopRibbonu.
        /// Tedy: pokud this není Merged, pak zde je zdejší <see cref="BarManagerInt"/>.
        /// Pokud this je Merged, pak zde je <see cref="BarManagerInt"/> z <see cref="TopRibbonControl"/>.
        /// </summary>
        public BarManager CurrentBarManager { get { return TopRibbonControl.BarManagerInt; } }
        /// <summary>
        /// Úroveň mergování this Ribbonu nahoru.
        /// Pokud this Ribbon není nikam mergován, je zde 0.
        /// Pokud this Ribbon je mergován do Parenta, a ten už nikam, pak je zde 1. A tak dále.
        /// </summary>
        public int MergeLevel { get { return ((this.MergedIntoParentDxRibbon != null) ? (this.MergedIntoParentDxRibbon.MergeLevel + 1) : 0); } }
        /// <summary>
        /// Obsahuje všechny aktuální Ribbony mergované od this až do nejvyššího Parenta, včetně this.
        /// this ribbon je na pozici [0], jeho Parent je na pozici [1], a tak dál nahoru. Pole tedy má vždy alespoň jeden prvek.
        /// Každý další prvek (na vyšším indexu) je Parent prvku předchozího.
        /// <para/>
        /// Prvek Tuple.Item1 = Ribbon; prvek Tuple.Item2 je stav <see cref="CurrentModifiedState"/> daného Ribbonu.
        /// <para/>
        /// Používá se při skupinový akcích typu "Odmerguje mě ze všech parentů, Oprav mě a pak mě zase Mererguj nazpátek".
        /// </summary>
        public List<Tuple<DxRibbonControl, bool>> MergedRibbonsUp
        {
            get
            {
                var result = new List<Tuple<DxRibbonControl, bool>>();
                var ribbon = this;
                while (ribbon != null)
                {
                    result.Add(new Tuple<DxRibbonControl, bool>(ribbon, ribbon.CurrentModifiedState));
                    ribbon = ribbon.MergedIntoParentDxRibbon;
                }
                return result;
            }
        }
        /// <summary>
        /// Obsahuje všechny aktuální Ribbony mergované od this až do nejnižšího Child, včetně this.
        /// this ribbon je na pozici [0], jeho Child je na pozici [1], a tak dál dolů. Pole tedy má vždy alespoň jeden prvek.
        /// Každý další prvek (na nižším indexu) je Child prvku předchozího.
        /// <para/>
        /// Prvek Tuple.Item1 = Ribbon; prvek Tuple.Item2 je stav <see cref="CurrentModifiedState"/> daného Ribbonu.
        /// <para/>
        /// Používá se při zápisu do všech Child Ribbonů.
        /// </summary>
        public List<Tuple<DxRibbonControl, bool>> MergedRibbonsDown
        {
            get
            {
                var result = new List<Tuple<DxRibbonControl, bool>>();
                DxRibbonControl ribbon = this;
                while (ribbon != null)
                {
                    result.Add(new Tuple<DxRibbonControl, bool>(ribbon, ribbon.CurrentModifiedState));
                    ribbon = ribbon.MergedChildDxRibbon;
                }
                return result;
            }
        }
        /// <summary>
        /// Aktuálně mergovaný Child Ribbon
        /// </summary>
        protected DevExpress.XtraBars.Ribbon.RibbonControl MergedChildRibbon { get; set; }
        /// <summary>
        /// Merguje this Ribbon do daného parenta. Pokud je dodán null, neprovede nic.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        /// <param name="parentDxRibbon"></param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        public void MergeCurrentDxToParent(DxRibbonControl parentDxRibbon, bool? forceSelectChildPage = null)
        {
            if (parentDxRibbon != null)
                this.ParentOwner.RunInGui(() => _MergeCurrentDxToParent(parentDxRibbon, forceSelectChildPage));
        }
        /// <summary>
        /// Merguje this Ribbon do daného parenta. Pokud je dodán null, neprovede nic.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="parentDxRibbon"></param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        private void _MergeCurrentDxToParent(DxRibbonControl parentDxRibbon, bool? forceSelectChildPage)
        {
            if (parentDxRibbon != null)
                parentDxRibbon._MergeChildDxRibbon(this, forceSelectChildPage);
        }
        /// <summary>
        /// Merguje do this Ribbonu dodaný Child Ribbon.
        /// Pokud aktuálně v this Ribbonu byl mergovaný nějaký Ribbon, bude nejprve korektně odmergován metodou <see cref="UnMergeDxRibbon()"/>.
        /// To proto, že nativní metoda DevExpress to nedělá a pak vzniká zmatek = dosavadní Child Ribbon o tom neví, a zdejší Parent Ribbon je zmaten.
        /// <para/>
        /// Po Mergování nového Child Ribbonu se provede opakované Mergování this Ribbonu do Parent Ribbonu, rekurzivně.
        /// Tím dotáhneme nový Child Ribbon až do viditelného Top Parenta (to DevExpress taky nedělá). 
        /// Bez toho by aktuální Child Ribbon nebyl vidět ani v sobě (on je Mergován), ani v nás (náš obsah je od dřívějška v Parentu, ale s obsahem bez nového Childu).
        /// Opakovaným Mergování se zajistí, že do našeho Parenta se dostane i nový Child.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        /// <param name="childDxRibbon"></param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        public void MergeChildDxRibbon(DxRibbonControl childDxRibbon, bool? forceSelectChildPage = null)
        {
            this.ParentOwner.RunInGui(() => _MergeChildDxRibbon(childDxRibbon, forceSelectChildPage));
        }
        /// <summary>
        /// Merguje do this Ribbonu dodaný Child Ribbon.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="childDxRibbon"></param>
        /// <param name="forceSelectChildPage"></param>
        private void _MergeChildDxRibbon(DxRibbonControl childDxRibbon, bool? forceSelectChildPage)
        {
            // Pokud nyní mám v sobě mergován nějaký Child Ribbon, pak jej UnMergujeme teď = jinak v tom bude guláš:
            if (this.MergedChildRibbon != null)
                this._UnMergeDxRibbon();

            // Pokud já jsem nyní Mergován v nějakém Parentu, pak sebe z něj musím nejprve odmergovat, a pak - s novým obsahem - zase zpátky mergovat do něj:
            var parentRibbon = MergedIntoParentDxRibbon;
            if (parentRibbon != null)
            {
                parentRibbon._UnMergeDxRibbon();
                forceSelectChildPage = true;                                   // Protože se budu mergovat zpátky, chci mít opět vybranou svoji SelectedPage! Povinně!
            }

            // Nyní do sebe mergujeme nově dodaný obsah:
            if (childDxRibbon != null)
                this._MergeChildRibbon(childDxRibbon, forceSelectChildPage);   // Tady se do Child DxRibbonu vepíše, že jeho MergedIntoParentDxRibbon je this

            // A pokud já jsem byl původně mergován nahoru, tak se nahoru zase vrátím:
            if (parentRibbon != null)
                parentRibbon._MergeChildDxRibbon(this, forceSelectChildPage);  // Tady se může rozběhnout rekurze ve zdejší metodě až do instance Top Parenta...
        }
        /// <summary>
        /// Provede Mergování daného Child Ribbonu do this (Parent) Ribbonu.
        /// Tato metoda neprovádí žádné další akce, pouze dovoluje explicitně určit režim SelectChildPage: 
        /// pokud bude v parametru <paramref name="forceSelectChildPage"/> zadána hodnota, pak bude akceptována namísto <see cref="SelectChildActivePageOnMerge"/>.
        /// <para/>
        /// Vedle toho existuje komplexnější metoda <see cref="MergeChildDxRibbon(DxRibbonControl, bool?)"/>, která dokáže mergovat Child Ribbon až do aktuálního top parenta.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        /// <param name="childRibbon">Mergovaný Child Ribbon</param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        public void MergeChildRibbon(DevExpress.XtraBars.Ribbon.RibbonControl childRibbon, bool? forceSelectChildPage = null)
        {
            if (childRibbon is DxRibbonControl childDxRibbon)
                this.ParentOwner.RunInGui(() => _MergeChildDxRibbon(childDxRibbon, forceSelectChildPage));
            else
                this.ParentOwner.RunInGui(() => _MergeChildRibbon(childRibbon, forceSelectChildPage));
        }
        /// <summary>
        /// Provede Mergování daného Child Ribbonu do this (Parent) Ribbonu.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="childRibbon"></param>
        /// <param name="forceSelectChildPage"></param>
        private void _MergeChildRibbon(DevExpress.XtraBars.Ribbon.RibbonControl childRibbon, bool? forceSelectChildPage)
        {
            _CurrentSelectChildActivePageOnMerge = forceSelectChildPage;       // Do metody MergeRibbon(Ribbon) to nemůžu poslat jako parametr, protože je overridovaná, 
            this.MergeRibbon(childRibbon);                                     //  a protože chci aby byla plně funkční (včetně správného chování 'selectPage') i v základní variantě.
            _CurrentSelectChildActivePageOnMerge = null;
        }
        /// <summary>
        /// Provede Mergování daného Child Ribbonu.
        /// Pokud je <see cref="SelectChildActivePageOnMerge"/> = true, pak po mergování bude v mergovaném ribbonu selectována ta stránka, 
        /// která byla selectována v mergovaném Child ribbonu.
        /// Lze využít metodu <see cref="MergeChildRibbon(DevExpress.XtraBars.Ribbon.RibbonControl, bool?)"/> a explicitně zadat styl selectování.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="childRibbon"></param>
        public override void MergeRibbon(DevExpress.XtraBars.Ribbon.RibbonControl childRibbon)
        {
            if (childRibbon == null) return;
            if (this.DxDisposed || this.DxOwnerDisposed) return;               // V tomhle stavu už nemá smysl pracovat. V komponentě DevExpress pak dochází k chybám.

            var childDxRibbon = childRibbon as DxRibbonControl;
            if (childDxRibbon != null && (childDxRibbon.DxDisposed || childDxRibbon.DxOwnerDisposed)) return;          // V tomhle stavu Child Ribbonu už taky nemá smysl pracovat.

            var startTime = DxComponent.LogTimeCurrent;

            bool selectPage = _CurrentSelectChildActivePageOnMerge ?? SelectChildActivePageOnMerge;  // Rád bych si předal _CurrentSelectChildActivePageOnMerge jako parametr, ale tady to nejde, jsme override bázové metody = bez toho parametru.
            string slaveSelectedPageId = (selectPage && childRibbon is DxRibbonControl dxRibbon) ? dxRibbon.SelectedPageFullId : null;
            var slaveSelectedPage = (selectPage ? childRibbon.SelectedPage : null);

            bool currentDxRibbonState = this.CurrentModifiedState;

            bool childDxRibbonState = false;
            try
            {
                if (childDxRibbon != null)
                {
                    childDxRibbonState = childDxRibbon.CurrentModifiedState;
                    childDxRibbon.MergedIntoParentDxRibbon = this;
                    childDxRibbon._CurrentMergeState = MergeState.MergeToParent;
                    childDxRibbon.IsActive = this.IsActive;
                    childDxRibbon.SetModifiedState(true, true);
                }

                _CurrentMergeState = MergeState.MergeWithChild;
                SetModifiedState(true, true);
                CurrentModifiedState = true;
                DxComponent.TryRun(() => base.MergeRibbon(childRibbon));
                _SetVisibleOnModifyManualRibbonOnMdiForm(childDxRibbon, true);
                this.MergedChildRibbon = childRibbon;
                _CurrentMergeState = MergeState.None;
                CustomizationPopupMenuRefreshHandlers();
            }
            finally
            {
                if (childDxRibbon != null)
                {
                    childDxRibbon._CurrentMergeState = MergeState.None;
                    childDxRibbon.CustomizationPopupMenuRefreshHandlers();
                    childDxRibbon.SetModifiedState(childDxRibbonState, true);
                }
                SetModifiedState(currentDxRibbonState, true);
            }

            if (selectPage)
            {
                if (slaveSelectedPageId != null) this.SelectedPageFullId = slaveSelectedPageId;
                if (!String.Equals(this.SelectedPageFullId, slaveSelectedPageId) && slaveSelectedPage != null)
                {
                    this.SelectedPage = slaveSelectedPage;
                    if (this.IsContentAlreadyPainted)
                        this.SelectedPageFixed = this.SelectedPage;
                }
            }
            
            this.StoreLastSelectedPage();

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"MergeRibbon: to Parent: {this.DebugName}; from Child: {(childRibbon?.ToString())}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Dočasné úložiště požadavku (parametr forceSelectChildPage různých metod) na provedení SelectPage z Child Ribbonu po dokončení mergování do Parent Ribbonu.
        /// Pokud má hodnotu, pak má přednost před <see cref="SelectChildActivePageOnMerge"/>.
        /// </summary>
        private bool? _CurrentSelectChildActivePageOnMerge = null;
        /// <summary>
        /// Provede odmergování this Ribbonu z Parenta, ale pokud Parent je mergován ve vyšším parentu, zajistí jeho ponechání tam.
        /// Není to triviální věc, protože pokud this (1) je mergován v Parentu (2) , a Parent (2) v ještě vyšším Parentu (3),
        /// pak se nejprve musí odebrat (2) z (3), pak (1) z (2) a pak zase vrátit (2) do (3).
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        public void UnMergeCurrentDxFromParent()
        {
            this.ParentOwner.RunInGui(() => _UnMergeModifyMergeCurrentRibbon(null, false));
        }
        /// <summary>
        /// Provede zadanou akci, která modifikuje obsah Ribbonu.
        /// Zajistí korektní stav this Ribbonu (tedy, že bude UnMerged!) pro danou akci, která chce modifikovat this Ribbon;
        /// Následně spustí danou akci; 
        /// A po dané akci zase vrátí this Ribbon pomocí mergování do původní struktury parentů.
        /// <para/>
        /// Tedy proběhne UnMerge celé hierarchie Ribbonů od TopParenta až k našemu Parentu;
        /// Pak se provede zadaná akce;
        /// A na konec se this Ribbon zase Merguje do celé původní hierarchie (pokud v ní na začátku byl).
        /// Nedojde ke změně aktuální stránky (pokud ji daná akce nechce odebrat).
        /// <para/>
        /// Toto je jediná cesta jak korektně modifikovat obsah Ribbonu v situaci, když je Mergován nahoru.
        /// Režie s tím spojená je relativně snesitelná, lze počítat 8 milisekund na jednu úroveň (součet za UnMerge a na konci zase Merge).
        /// <para/>
        /// Pokud this Ribbon aktuálně není nikam Mergován, pak se provede zadaná akce bez zbytečných dalších režií.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci si zajistí sama.
        /// </summary>
        public void ModifyCurrentDxContent(Action action)
        {
            this.ParentOwner.RunInGui(() => _UnMergeModifyMergeCurrentRibbon(action, true));
        }
        /// <summary>
        /// Provede odmergování this Ribbonu z Parentů (hierarchicky); poté až bude this Ribbon odmergován pak provede danou akci; a na závěr vrátí this Ribbon tam kde byl Mergován (pokud je požadováno: <paramref name="mergeBack"/>).
        /// Pokud není požadováno, pak this Ribbon nechá UnMerge, ale vyšší Ribbony vrátí do původního stavu.
        /// Není to triviální věc, protože pokud this (1) je mergován v Parentu (2) , a Parent (2) v ještě vyšším Parentu (3),
        /// pak se nejprve musí odebrat (2) z (3), pak (1) z (2) a pak zase vrátit (2) do (3).
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        private void _UnMergeModifyMergeCurrentRibbon(Action action, bool mergeBack, bool skipCheckLazy = false)
        {
            var startTime = DxComponent.LogTimeCurrent;

            var ribbonsUp = this.MergedRibbonsUp;
            int count = ribbonsUp.Count;

            int last = count - 1;
            var topRibbon = ribbonsUp[last].Item1;
            var topRibbonSelectedPage1 = topRibbon.SelectedPageFullId;

            // Pokud this Ribbon není nikam mergován (když počet nahoru mergovaných je 1):
            if (count == 1)
            {   // Provedu požadovanou akci rovnou (není třeba dělat UnMerge a Merge), a skončíme:
                SetModifiedState(true, true);
                _RunUnMergedAction(action);
                SetModifiedState(ribbonsUp[0].Item2, true);          // Vrátím stav CurrentModifiedState původní, nikoliv false - ono tam mohlo být true!
                activateOriginPage(true);
                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"ModifyRibbon {this.DebugName}: Current; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Odmergovat - Provést akci (pokud je) - Mergovat zpátky (vše nebo jen UpRibbony):
            try
            {
                // Top Ribbon pozastaví svoji práci:
                topRibbon.SuspendLayout();
                topRibbon._PrepareEmptyPageForUMM();

                // Všem Ribonům v řadě potlačíme CurrentModifiedState a CheckLazyContentEnabled, protože bude docházet ke změně SelectedPage, ale to nemá vyvolat požadavek na její LazyLoad donačítání:
                ribbonsUp.ForEach(r => { r.Item1.SetModifiedState(true, true); });

                // UnMerge proběhne od posledního (=TopMost: count - 1) Ribbonu dolů až k našemu Parentu (u >= 1):
                for (int u = last; u >= 1; u--)
                    ribbonsUp[u].Item1.UnMergeRibbon();

                // Konečně máme this Ribbon osamocený (není Merge nahoru, ani neobsahuje MergedChild), provedeme tedy akci:
                _RunUnMergedAction(action);

                // ...a pak se přimerguje náš parent / nebo i zdejší Ribbon zpátky nahoru do TopMost:
                // Nazpátek se bude mergovat (mergeBack) i this Ribbon do svého Parenta? Anebo jen náš Parent do jeho Parenta a náš Ribbon zůstane UnMergovaný?
                int mergeFrom = (mergeBack ? 0 : 1);
                for (int m = mergeFrom; m < last; m++)
                    ribbonsUp[m].Item1._MergeCurrentDxToParent(ribbonsUp[m + 1].Item1, true);
                // Součástí tohoto zpětného mergování je i aktivace naposledy aktivní stránky v rámci Child ribbonu.
                // Proto finální activateOriginPage(false); má hodnotu doActivate = false.
            }
            finally
            {
                // Všem Ribonům v řadě nastavím CurrentModifiedState a CheckLazyContentEnabled na původní hodnotu:
                ribbonsUp.ForEach(r => { r.Item1.SetModifiedState(r.Item2, true); });

                topRibbon._RemoveEmptyPageForUMM();

                // Top Ribbon obnoví svoji práci:
                topRibbon.ResumeLayout(false);
                topRibbon.PerformLayout();
            }

            // A protože po celou dobu byl potlačen CheckLazyContentEnabled, tak pro Top Ribbon to nyní provedu explicitně (pokud už je to povoleno : topRibbon.CheckLazyContentEnabled == true):
            activateOriginPage(false);

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"ModifyRibbon {this.DebugName}: UnMerge + Action + Merge; Total Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);


            // Aktivuje original page a volitelně prověří Lazy obsah stránky
            void activateOriginPage(bool doActivate)
            {
                var topRibbonSelectedPage2 = topRibbon.SelectedPageFullId;
                bool isChanged = (topRibbonSelectedPage2 != topRibbonSelectedPage1);

                if (isChanged && doActivate)
                    topRibbon.SelectedPageFullId = topRibbonSelectedPage1;

                // Pokud mám řešit CheckLazyContent, a jen po změně stránky: CheckLazyContent()
                //   Pokud by NEBYLA změna stránky, pak se zacyklíme v Refreshi po donačtení obsahu stránky typicky Workflow = ta má režim LoadOnDemand stále...
                if (!skipCheckLazy && isChanged && topRibbon.CheckLazyContentEnabled)
                    topRibbon.CheckLazyContent(topRibbon.SelectedPage, false);
            }
        }
        /// <summary>
        /// Z aktuálního Ribbonu odmerguje jeho současný ChildRibbon (pokud je);
        /// Pak provede danou akci;
        /// Pak zase vrátí původní Child Ribbon;
        /// a do logu vepíše čas celé této akce.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="action"></param>
        private void _RunUnMergedAction(Action action)
        {
            if (action == null) return;

            var startTime = DxComponent.LogTimeCurrent;
            var childRibbon = this.MergedChildRibbon;
            try
            {
                // This Ribbon pozastaví svoji práci:    tohle nedělej !!!!   Tohle způsobilo, že CollapsedGroup nešla otevřít, jenom blikla po dobu MouseDown:                    KEY: COLLAPSEDGROUP ERROR
                //           this.BarManager.BeginUpdate();

                // Nyní máme náš Ribbon UnMergovaný z Parentů, ale i on v sobě může mít mergovaného Childa:
                if (childRibbon != null) this._UnMergeDxRibbon();

                action();
            }
            finally
            {
                // Do this Ribbonu vrátíme jeho Child Ribbon:
                if (childRibbon != null) this._MergeChildRibbon(childRibbon, false);

                // This Ribbon obnoví svoji práci:    tohle nedělej !!!!   Tohle způsobilo, že CollapsedGroup nešla otevřít, jenom blikla po dobu MouseDown:                       KEY: COLLAPSEDGROUP ERROR
                //           this.BarManager.EndUpdate();
            }
            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"ModifyRibbon {this.DebugName}: RunAction; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Nastaví <see cref="CurrentModifiedState"/> = <paramref name="modifiedState"/>; a volitelně i <see cref="CheckLazyContentEnabled"/> = !<paramref name="modifiedState"/>; 
        /// Zadejme tedy true = budou probíhat modifikace, false = jdeme do plného provozu (budou aktivní handlery atd)
        /// </summary>
        /// <param name="modifiedState"></param>
        /// <param name="setCheckLazy">Nastavit i </param>
        private void SetModifiedState(bool modifiedState, bool setCheckLazy)
        {
            this.CurrentModifiedState = modifiedState;
            if (setCheckLazy)
                this.CheckLazyContentEnabled = !modifiedState;
        }
        /// <summary>
        /// Připraví this Ribbon (který je TopRibbonem = reálně viditelný Ribbon) na proces UMM = UnMerge + Modify + Merge.
        /// Konkrétně: pokud this Ribbon nemá žádnou vlastní stránku, pak do sebe dočasně přidá prázdnou stránku.
        /// Na závěr procesu UMM je nutno volat <see cref="_RemoveEmptyPageForUMM()"/>, kde tato stránka bude odebrána.
        /// <para/>
        /// Důvod? Zabránění blikání okna v případě, kdy pod Ribbonem je zadokován další panel, a v proces UMM by tento panel nehezky blikal.
        /// </summary>
        private void _PrepareEmptyPageForUMM()
        {
            if (this.GetPages(PagePosition.Default).Count > 0) return;

            if (_EmptyPageUMM == null)
                _EmptyPageUMM = new DxRibbonPage(this, "    ") { Name = _EmptyPageUMMPageId };
            if (_EmptyPageUMM != null)
            {
                this.Pages.Add(_EmptyPageUMM);
                this._EmptyPageUMMActive = true;
            }
        }
        /// <summary>
        /// Uklidí prázdnou stránku na konci procesu UMM. Více v <see cref="_PrepareEmptyPageForUMM()"/>.
        /// </summary>
        private void _RemoveEmptyPageForUMM()
        {
            if (_EmptyPageUMMActive && _EmptyPageUMM != null)
                this.Pages.Remove(_EmptyPageUMM);
            _EmptyPageUMMActive = false;
        }
        /// <summary>
        /// Instance prázdné stránky pro proces UMM
        /// </summary>
        private DxRibbonPage _EmptyPageUMM;
        /// <summary>
        /// Prázdná stránka pro proces UMM je aktivní (tj. je obsažena v this Ribbonu)?
        /// </summary>
        private bool _EmptyPageUMMActive;
        /// <summary>
        /// PageId pro prázdnou stránku pro proces UMM
        /// </summary>
        private const string _EmptyPageUMMPageId = "#empty-page#";
        /// <summary>
        /// Odmerguje z this Ribbonu jeho případně mergovaný obsah, nic dalšího nedělá.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci si zajistí sama.
        /// </summary>
        public void UnMergeDxRibbon()
        {
            this.ParentOwner.RunInGui(() => _UnMergeDxRibbon());
        }
        /// <summary>
        /// Odmerguje z this Ribbonu jeho případně mergovaný obsah, nic dalšího nedělá.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu.
        /// Namísto zdejší metody lze rovnou volat metodu this.UnMergeRibbon().
        /// </summary>
        private void _UnMergeDxRibbon()
        {
            this.UnMergeRibbon();
        }
        /// <summary>
        /// Odebere mergovaný Ribbon a vepíše čas do Logu
        /// </summary>
        public override void UnMergeRibbon()
        {
            var startTime = DxComponent.LogTimeCurrent;

            var lastSelectedOwnPageId = this.LastSelectedOwnPageFullId;

            DxRibbonControl childDxRibbon = this.MergedChildDxRibbon;
            if (childDxRibbon != null)
                childDxRibbon._CurrentMergeState = MergeState.UnMergeFromParent;

            _CurrentMergeState = MergeState.UnMergeChild;
            DxComponent.TryRun(() => base.UnMergeRibbon());
            _SetVisibleOnModifyManualRibbonOnMdiForm(childDxRibbon, false);
            _CurrentMergeState = MergeState.None;
            CustomizationPopupMenuRefreshHandlers();

            if (childDxRibbon != null)
            {
                childDxRibbon._CurrentMergeState = MergeState.None;
                childDxRibbon.CustomizationPopupMenuRefreshHandlers();
                childDxRibbon.MergedIntoParentDxRibbon = null;
                childDxRibbon.RefreshUserQatAfterUnMerge();
                childDxRibbon.ActivateLastActivePage();
            }
            this.MergedChildRibbon = null;

            // Po reálném UnMerge (tj. když existuje childDxRibbon) zkusíme selectovat tu naši vlastní stránku, která byla naposledy aktivní:
            //  (Poznámka: DevExpress volá metodu UnMergeRibbon() i v procesu nastavování vlastností Ribbonu, proto tahle podmínka)
            if (childDxRibbon != null)
                this.ActivateLastActivePage();

            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"UnMergeRibbon from Parent: {this.DebugName}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Metoda do daného Ribbonu (- pokud je umístěn na Formuláři, který je MDI Child, a Ribbon je v režimu ManualMdiMerge) vepíše danou Visible.
        /// </summary>
        /// <param name="dxRibbon"></param>
        /// <param name="visible"></param>
        private static void _SetVisibleOnModifyManualRibbonOnMdiForm(DxRibbonControl dxRibbon, bool visible)
        {
            if (dxRibbon != null && !IsAutomaticMdiRibbonMerge && dxRibbon.OwnerControl is Form form && form.IsMdiChild && dxRibbon.CurrentModifiedState)
                dxRibbon.Visible = visible;
        }
        /// <summary>
        /// Aktuální stav mergování this Ribbonu
        /// </summary>
        private MergeState _CurrentMergeState = MergeState.None;
        /// <summary>
        /// Ribbon je aktuálně v procesu modifikace ( UnMerge - Modify - Merge )
        /// </summary>
        private bool CurrentModifiedState = false;
        /// <summary>
        /// Stavy mergování Ribbonu
        /// </summary>
        private enum MergeState 
        { 
            None = 0, 
            /// <summary>
            /// Do Current Parent Ribbonu je právě mergován nějaký Child Ribbon.
            /// </summary>
            MergeWithChild,
            /// <summary>
            /// Current Child Ribbon je právě mergován do nějakého Parent Ribbonu.
            /// </summary>
            MergeToParent,
            /// <summary>
            /// Z Current Parent Ribbonu je právě odmergován nějaký Child Ribbon.
            /// </summary>
            UnMergeChild,
            /// <summary>
            /// Current Child Ribbon je právě odmergován z nějakého Parent Ribbonu.
            /// </summary>
            UnMergeFromParent
        }
        #endregion
        #region Static helpers
        /// <summary>
        /// Vytvoří a vrátí definici Home stránky Ribbonu. Stránka neobsahuje žádné grupy.
        /// </summary>
        /// <returns></returns>
        public static DataRibbonPage CreateStandardHomePage()
        {
            DataRibbonPage homePage = new DataRibbonPage()
            {
                PageId = "Standard",
                PageText = "Domů",         // přejdi na lokalizaci
                MergeOrder = 1
            };
            return homePage;
        }
        /// <summary>
        /// Vytvoří a vrátí logickou Grupu do Ribbonu s obsahem tlačítek pro skiny (tedy definici pro tuto grupu) a další prvky dle požadavků.
        /// Grupa má ID = <see cref="DesignRibbonGroupId"/>.
        /// (Grupa si svoje tlačítka obsluhuje sama.)
        /// </summary>
        /// <param name="designGroupParts"></param>
        /// <param name="groupText"></param>
        /// <returns></returns>
        public static IRibbonGroup CreateDesignHomeGroup(FormRibbonDesignGroupPart designGroupParts = FormRibbonDesignGroupPart.Default, string groupText = null)
        {
            if (designGroupParts == FormRibbonDesignGroupPart.None) return null;

            string text = (!String.IsNullOrEmpty(groupText) ? groupText : "Design");

            DataRibbonGroup iGroup = new DataRibbonGroup() { GroupId = DesignRibbonGroupId, GroupText = text };

            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.SkinButton)) iGroup.Items.Add(new DataRibbonItem() { ItemId = DesignRibbonItemSkinSetId, ItemType = RibbonItemType.SkinSetDropDown });
            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.PaletteButton)) iGroup.Items.Add(new DataRibbonItem() { ItemId = DesignRibbonItemSkinPaletteDropDownId, ItemType = RibbonItemType.SkinPaletteDropDown });
            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.PaletteGallery)) iGroup.Items.Add(new DataRibbonItem() { ItemId = DesignRibbonItemSkinPaletteDropGalleryId, ItemType = RibbonItemType.SkinPaletteGallery });

            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.ZoomPresetMenu))
                iGroup.Items.Add(new DataRibbonItem()
                {
                    ItemId = DesignRibbonItemZoomPresetMenuId,
                    ItemType = RibbonItemType.ZoomPresetMenu,
                    RibbonStyle = RibbonItemStyles.Large
                });
            else if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.ZoomPresetMenuTest))
                iGroup.Items.Add(new DataRibbonItem()
                {
                    ItemId = DesignRibbonItemZoomPresetMenuId,
                    ItemType = RibbonItemType.ZoomPresetMenu,
                    RibbonStyle = RibbonItemStyles.Large,
                    Tag = "50,60,75,85,92,100,108,115,125,150,175,189,200"
                });

            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.UhdSupport))
                iGroup.Items.Add(new DataRibbonItem()
                {
                    ItemId = DesignRibbonItemLogUhdSupport,
                    Text = "UHD Paint",
                    ToolTipText = "Zapíná podporu pro Full vykreslování na UHD monitoru",
                    ItemType = RibbonItemType.CheckButton,
                    RibbonStyle = RibbonItemStyles.SmallWithText,
                    ImageName = "svgimages/xaf/action_view_chart.svg",
                    Checked = DxComponent.UhdPaintEnabled,
                    ClickAction = _SetUhdPaint
                });

            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.LogActivity))
                iGroup.Items.Add(new DataRibbonItem()
                {
                    ItemId = DesignRibbonItemLogActivityId,
                    Text = "Log Active",
                    ToolTipText = "Zapíná / vypíná logování aktivity",
                    ItemType = RibbonItemType.CheckButton,
                    RibbonStyle = RibbonItemStyles.SmallWithText,
                    ImageName = "svgimages/spreadsheet/movepivottable.svg",
                    Checked = DxComponent.LogActive,
                    ClickAction = _SetLogActivity
                });

            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.NotCaptureWindows))
                iGroup.Items.Add(new DataRibbonItem()
                {
                    ItemId = DesignRibbonItemNotCaptureWindowsId,
                    Text = "Hide Capture",
                    ToolTipText = "Zapíná / vypíná možnost skrývat obsah oken pro Capture programy (Teams, VideoRecording, PrintScreen). Aktivní button (orámovaný) = true = obsah oken není možno zachycovat.",
                    ItemType = RibbonItemType.CheckButton,
                    RibbonStyle = RibbonItemStyles.SmallWithText,
                    ImageName = "images/xaf/templatesv2images/state_itemvisibility_hide.svg",
                    Checked = DxComponent.ExcludeFromCaptureContent,
                    ClickAction = _SetNotCaptureWindows
                });

           
            if (designGroupParts.HasFlag(FormRibbonDesignGroupPart.ImageGallery))
                iGroup.Items.Add(new DataRibbonItem()
                {
                    ItemId = DesignRibbonItemLogImageGallery,
                    Text = "DX Images",
                    ToolTipText = "Otevře okno s nabídkou systémových ikon",
                    ItemType = RibbonItemType.Button,
                    RibbonStyle = RibbonItemStyles.Large,
                    ImageName = "svgimages/icon%20builder/actions_image.svg",
                    ClickAction = _ShowImages
                });

            return iGroup;
        }
        internal const string DesignRibbonGroupId = "_SYS__DevExpress_Design";
        internal const string DesignRibbonItemSkinSetId = "_SYS__DevExpress_SkinSetDropDown";
        internal const string DesignRibbonItemSkinPaletteDropDownId = "_SYS__DevExpress_SkinPaletteDropDown";
        internal const string DesignRibbonItemSkinPaletteDropGalleryId = "_SYS__DevExpress_SkinPaletteGallery";
        internal const string DesignRibbonItemLogUhdSupport = "_SYS__DevExpress_UhdSupportCheckBox";
        internal const string DesignRibbonItemLogImageGallery = "_SYS__DevExpress_DxImageGallery";
        internal const string DesignRibbonItemLogActivityId = "_SYS__DevExpress_SetLogActivity";
        internal const string DesignRibbonItemNotCaptureWindowsId = "_SYS__DevExpress_SetNotCaptureWindows";
        internal const string DesignRibbonItemZoomPresetMenuId = "_SYS__DevExpress_ZoomTrackbar";

        /// <summary>
        /// Metoda zkusí najít definiční data pro daný prvek Ribbonu.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        internal static bool TryGetRibbonItem(BarItem barItem, out IRibbonItem ribbonItem)
        {
            if (_TryGetIRibbonData(barItem, out string qatKey, out IRibbonItem iRibbonItem, out IRibbonGroup iRibbonGroup))
            {
                ribbonItem = iRibbonItem;
                return true;
            }
            ribbonItem = null;
            return false;
        }
        /// <summary>
        /// Nastaví UHD paint. Pouze v Testovací aplikaci.
        /// </summary>
        /// <param name="menuItem"></param>
        private static void _SetUhdPaint(IMenuItem menuItem)
        {
#if Compile_TestDevExpress
            DxComponent.UhdPaintEnabled = (menuItem?.Checked ?? false);
            DxComponent.Settings.SetRawValue("Components", DxComponent.UhdPaintEnabledCfgName, DxComponent.UhdPaintEnabled ? "True" : "False");
            DxComponent.ApplicationRestart();
#endif
        }
        /// <summary>
        /// Zobrazí hgalerii obrázků. Pouze v Testovací aplikaci.
        /// </summary>
        /// <param name="menuItem"></param>
        private static void _ShowImages(IMenuItem menuItem)
        {
#if Compile_TestDevExpress
            TestDevExpress.Forms.ImagePickerForm.ShowForm();
#endif
        }
        /// <summary>
        /// Aktivuje / deaktivuje LOG. Pouze v Testovací aplikaci.
        /// </summary>
        /// <param name="menuItem"></param>
        private static void _SetLogActivity(IMenuItem menuItem)
        {
#if Compile_TestDevExpress
            DxComponent.LogActive = (menuItem?.Checked ?? false);
            DxComponent.Settings.SetRawValue("Components", DxComponent.LogActiveCfgName, DxComponent.LogActive ? "True" : "False");
#endif
        }
        /// <summary>
        /// Aktivuje / deaktivuje NotCaptureWindows. Pouze v Testovací aplikaci.
        /// </summary>
        /// <param name="menuItem"></param>
        private static void _SetNotCaptureWindows(IMenuItem menuItem)
        {
#if Compile_TestDevExpress
            DxComponent.ExcludeFromCaptureContent = (menuItem?.Checked ?? false);
            DxComponent.Settings.SetRawValue("Components", DxComponent.ExcludeFromCaptureContentCfgName, DxComponent.ExcludeFromCaptureContent ? "True" : "False");
#endif
        }
        /// <summary>
        /// Zajistí nastavení stavu Checked do navázaného prvku a odeslání události do odpovídajícho Ribbonu
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isChecked"></param>
        internal static void DoItemCheckedChange(BarItem item, bool? isChecked)
        {
            if (item != null && item.Tag is BarItemTagInfo itemInfo)
            {
                if (itemInfo.Data != null) itemInfo.Data.Checked = isChecked;
                if (itemInfo.DxGroup.OwnerDxRibbon != null) itemInfo.DxGroup.OwnerDxRibbon.RaiseRibbonItemClick(new DxRibbonItemClickArgs(itemInfo.Data));
            }
        }
        #endregion
        #region IDxRibbonInternal + IListenerApplicationIdle implementace
        void IDxRibbonInternal.PrepareRealLazyItems(DxRibbonLazyLoadInfo lazyGroup, bool isCalledFromReFill) { PrepareRealLazyItems(lazyGroup, isCalledFromReFill); }
        void IDxRibbonInternal.RemoveGroupsFromQat(List<DevExpress.XtraBars.Ribbon.RibbonPageGroup> groupsToDelete) { RemoveGroupsFromQat(groupsToDelete); }
        void IDxRibbonInternal.RemoveItemsFromQat(List<DevExpress.XtraBars.BarItem> itemsToDelete) { RemoveItemsFromQat(itemsToDelete); }
        int IDxRibbonInternal.CurrentTimeStamp { get { return LastTimeStamp; } }
        int IDxRibbonInternal.GetNextTimeStamp() { return ++LastTimeStamp; }
        void IDxRibbonInternal.RemoveGroups(IEnumerable<DxRibbonGroup> groupsToDelete) { RemoveGroups(groupsToDelete); }
        void IListenerApplicationIdle.ApplicationIdle() { _ApplicationIdle(); }
        #endregion
        #region IListenerLightDarkChanged a IListenerZoomChange : reakce na změnu skinu světlý / tmavý = modifikace ikon, a na Zoom
        /// <summary>
        /// Po změně skinu světlý / tmavý
        /// </summary>
        void IListenerLightDarkChanged.LightDarkChanged()
        {
            this.ReloadImages();
        }
        void IListenerZoomChange.ZoomChanged()
        {
            // Nemá význam přenačítat Images, když Zoom se netýká reálné velikosti Images v Ribbonu.
            // Zatím nemám cestu, jak zoomovat obrázky v Ribbonu podle Zoomu Nephrite.
            // Zoomuje se jen velikost textu.

            // this.ReloadImages();
        }
        private void ReloadImages()
        {
            try
            {
                this.BeginUpdate();

                foreach (BarItem barItem in this.Items)
                {
                    if (barItem.Tag is BarItemTagInfo tagInfo)
                    {
                        FillBarItemImage(barItem, tagInfo.Data, tagInfo.Level);
                    }
                }

//                foreach (var dxPage in this.AllOwnPages)
//                    dxPage..ReApplyImage();

                foreach (var dxGroup in this.Groups)
                    dxGroup.ReApplyImage();
            }
            finally
            {
                this.EndUpdate();
            }
        }
        #endregion
        #region INFORMACE A POSTŘEHY, FUNGOVÁNÍ: CREATE, MERGE, UNMERGE, časy
        /*
           1. LAZY CREATE CONTENT :
        Vytvoření Ribbonu s 8 stránkami, 30 group, 142 BarItem, celkem včetně SubItems 1131 prvků: 450 ms
           v dalším uvádím počty ve tvaru: {Page/Group/Itm/SubItem}
        Vytvoření Lazy Ribbonu s {8/30/0/0} = ale bez BarItems : 0,8 ms
        OnDemand tvorba pro jednu aktivní stránku: {0/0/34/233} : 92 ms
        ... {0/0/18/92}: 69 ms

           2. MERGOVÁNÍ :
        Tvorba Ribbonu {7/30/144/1132} : 435 ms
        MERGE do Parenta: 9 ms
        UNMEGE z Parenta: 5 ms
          druhý pokus
        MERGE do Parenta: 4.5 ms
        UNMEGE z Parenta: 3.5 ms

        MERGOVÁNÍ a vliv na funkce:
        a) mějme Ribbon Child "Child" a Ribbon Parent "Parent"
        b) Provedu Parent.Merge(Child)
            - do Parenta se vytvoří new instance RibbonPage = MergedPages
            - do MergedPages se vytvoří new instance grup podle grup v Child, 
                přetáhne s sebou i Tag s původní grupy = vznikne zde kopie reference na původní objekt
                Tady používám třídu DxRibbonPageLazyGroupInfo
            - do MergedPage.Group se vytvoří new BarItemLink = linky na prvky primárně definované v Child.RibbonPage
        c) Pokud provedu Merge na stránku s LazyContent grupou (obsahuje v Tagu DxRibbonPageLazyGroupInfo):
            - Tag v Mergované grupě obsahuje referenci na DxRibbonPageLazyGroupInfo
            - ale do MergedPages už nemohu dostat LazyContent
            - Musím Ribbon UnMergovat až do základního Child Ribbonu
            - v Child Ribbonu musím vytvořit LazyContent
            - pak musím zase Mergovat spojený obsah nahoru

        d) Pořadí:
            - Mergování:  od nejspodnějšího Child (DynamicPage) do jeho Parenta a toho až pak do nejvyššího Parenta (Desktop)
               V jiném pořadí nelze (když bych nejspodnějšího Childa mergoval až později, už se nedostane do nejvyššího Parenta)
            - UnMerge:    správný postup je od nejvyššího Parenta (Desktop), v něm dát UnMerge, 
                          a potom prostřeední Parent UnMerge, tím se dostane obsah do nejspodnějšího Childa
               V jiném pořadí to kupodivu jde, ale je to divné: když dám UnMerge na prostředním Parentu, pak se obsah vrátí do nejspodnějšího Childa,
                          ale přitom je stále zobrazen v nejvyším Parentu = je vidět duplicitně.

        d) Obecně:
            - Když dám Parent.Megre(Child), tak Child Ribbon zmizí
            - Mergování může být vícestupňové
            - Vždycky lze do jednoho Parenta mergovat jen jeden Child
            - Jakmile do toho samého Parenta dám mergovat jiný Child, tak ten Parent toho předchozího v sobě mergovaného Childa vyplivne a vrátí na jeho původní místo
            - Když má Parent v sobě mergovaného Childa:
               - pak jeho Clear (v rámci Parenta): neprovede Clear těch dat z Childa, ty v něm zůstanou
               - pak Clear provedený v Childu (!): smaže BarItemy (tlačítka), ale v Parentu zůstanou prázdné stránky a v nich prázdné grupy od Childa
               - pak AddItems v Childu (tvorba nových stránek, grup, baritemů) se nepřenáší do parenta, kde jsou jeho dosavadní prvky mergované
                   => nově vytvořené prvky jsou ve vzduchoprázdnu, protože nejsou vidět ani v Childu (ten je neviditelný), ani v Parentu (ten na ně nereaguje)
            - Pokud v Parentu je mergován Child, a pak do Childu přidám další data, a pak znovu zavolám Merge, 
               - pak se v Parentu ta nová data nezobrazí - protože náš Child už v Parentu je mergován (=nedojde k vyplivnutí Childa jako když jde o jinou instanci)
               - V tom případě se nejdřív musí provést UnMerge a pak znovu Merge
               - ale může to proběhnout v pořadí: { Add - UnMerge - Merge }, není nutno provádět: { UnMerge - Add - Merge }

        e) Slučování při Mergování:
            - Pokud dvě Pages mají shodné PageId, budou sloučeny do jedné stránky
            - Pokud dvě Pages mají rozdílné PageId a přitom mají shodný PageText, budou existovat dvě Pages se shodným vizuálním titulkem PageText
            - Totéž platí pro Grupy: merguje se na základě GroupId, a popisný text GroupText je jen viditelným popisem (mohou tedy být zobrazeny dvě grupy se stejným titulkem)

        */
        #endregion
    }
    #region Args a enumy pro Ribbon
    /// <summary>
    /// Třída argumentu pro eventy, kde se aktivuje prvek <see cref="IRibbonItem"/> v Ribbonu.
    /// </summary>
    public class DxRibbonItemClickArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        public DxRibbonItemClickArgs(IRibbonItem item)
        {
            this.Item = item;
            this.Area = DxRibbonItemClickArea.None;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="subButtonName"></param>
        public DxRibbonItemClickArgs(IRibbonItem item, string subButtonName)
        {
            this.Item = item;
            this.SubButtonName = subButtonName;
            this.Area = DxRibbonItemClickArea.SubButton;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="area"></param>
        public DxRibbonItemClickArgs(IRibbonItem item, DxRibbonItemClickArea area)
        {
            this.Item = item;
            this.Area = area;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="area"></param>
        /// <param name="pageId"></param>
        /// <param name="groupId"></param>
        public DxRibbonItemClickArgs(IRibbonItem item, DxRibbonItemClickArea area, string pageId, string groupId)
        {
            this.Item = item;
            this.Area = area;
            this.PageId = pageId;
            this.GroupId = groupId;
        }
        /// <summary>
        /// Prvek
        /// </summary>
        public IRibbonItem Item { get; private set; }
        /// <summary>
        /// Button, na který bylo kliknuto - jeho Name.
        /// Pokud nebylo kliknuto na button, pak je zde null (=klik přímo na prvek).
        /// Pokud bylo kliknuto na SubButton typu DropDown, pak se rozbalí combo, ale odpovídající událost se nevyvolá.
        /// </summary>
        public string SubButtonName { get; private set; }
        /// <summary>
        /// V jaké části bylo kliknuto na prvek Ribbonu
        /// </summary>
        public DxRibbonItemClickArea Area { get; private set; }
        /// <summary>
        /// Na jaké stránce bylo kliknuto na prvek Ribbonu
        /// </summary>
        public string PageId { get; private set; }
        /// <summary>
        /// Na jaké grupě bylo kliknuto na prvek Ribbonu
        /// </summary>
        public string GroupId { get; private set; }
    }
    /// <summary>
    /// Třída argumentu pro eventy, kde se aktivuje specifický Button v prvku <see cref="IRibbonItem"/> v Ribbonu.
    /// </summary>
    public class DxRibbonItemButtonClickArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item">Prvek, na který bylo kliknuto</param>
        /// <param name="button">Button, na který bylo kliknuto</param>
        public DxRibbonItemButtonClickArgs(IRibbonItem item, DevExpress.XtraEditors.Controls.EditorButton button)
        {
            this.Item = item;
            this.Button = button;
        }
        /// <summary>
        /// Prvek, na který bylo kliknuto
        /// </summary>
        public IRibbonItem Item { get; private set; }
        /// <summary>
        /// Button, na který bylo kliknuto
        /// </summary>
        public DevExpress.XtraEditors.Controls.EditorButton Button { get; private set; }
        /// <summary>
        /// Definiční data SubButtonu 
        /// </summary>
        internal DataSubButton ButtonData { get { return this.Button?.Tag as DataSubButton; } }
        /// <summary>
        /// Name buttonu
        /// </summary>
        public string ButtonName { get { return this.ButtonData?.ButtonId; } }
    }
    /// <summary>
    /// Oblast Ribbonu, na které bylo na prvek kliknuto
    /// </summary>
    public enum DxRibbonItemClickArea
    {
        /// <summary>
        /// Nebylo určeno
        /// </summary>
        None,
        /// <summary>
        /// Standardní oblast Ribbonu = stránka a grupa
        /// </summary>
        RibbonGroup,
        /// <summary>
        /// QAT panel
        /// </summary>
        QatPanel,
        /// <summary>
        /// Caption (titulkový) panel
        /// </summary>
        CaptionPanel,
        /// <summary>
        /// Vyhledávací políčko
        /// </summary>
        SearchCombo,
        /// <summary>
        /// Status bar
        /// </summary>
        StatusBar,
        /// <summary>
        /// Klávesová zkratka
        /// </summary>
        HotKey,
        /// <summary>
        /// SubButton
        /// </summary>
        SubButton,
        /// <summary>
        /// Hledáno a nelze určit
        /// </summary>
        Other
    }
    /// <summary>
    /// Režim, v jakém jsou vytvářeny a přidávány fyzické prvky (BarItems) do Ribbonu.
    /// </summary>
    [Flags]
    public enum DxRibbonCreateContentMode
    {
        /// <summary>
        /// Nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Pokud je nastaveno, MAJÍ se generovat všechny grupy a prvky první úrovně (viditelný obsah stránky v Ribbonu).
        /// Pokud NENÍ nastaveno, nemusí se vytvářet. 
        /// Pak se ale přihlédne k 
        /// </summary>
        CreateGroupsContent = 0x0001,
        /// <summary>
        /// Pokud je nastaveno, MAJÍ se generovat všechny grupy a prvky všech úrovní (viditelný obsah stránky v Ribbonu, a kompletní seznam jejich SubItems = Menu, SplitButton atd).
        /// Tato hodnota samozřejmě provede i volbu <see cref="CreateGroupsContent"/>.
        /// </summary>
        CreateAllSubItems = 0x0004,
        /// <summary>
        /// Pokud je položek málo, udělej je všechny, jinak je odlož na Lazy
        /// </summary>
        CreateAutoByCount = 0x0008,
        /// <summary>
        /// Pokud je nastaveno, MAJÍ se generovat alespoň ty prvky, které jsou obsaženy v QAT seznamu (podle jejich ItemId se detekují pomocí metody <see cref="DxRibbonControl.ContainsQAT(IRibbonItem)"/>.
        /// Tato hodnota se akceptuje jen tehdy, když NENÍ aktivní hodnota <see cref="CreateGroupsContent"/> ani <see cref="CreateAllSubItems"/>.
        /// </summary>
        CreateOnlyQATItems = 0x0010,
        /// <summary>
        /// Aktuální běh je vyvolán OnDemand donačtením dat
        /// </summary>
        RunningOnDemandFill = 0x0100,
        /// <summary>
        /// Aktuální běh je vyvolán OnIdle procesem, máme vygenerovat všechno co lze
        /// </summary>
        RunningOnIdleFill = 0x0200,
        /// <summary>
        /// Aktivovat doplnění obsahu stránky v režimu OnApplicationIdle
        /// </summary>
        ActivateOnIdleLoad = 0x1000,
        /// <summary>
        /// připravit nějaký obsah (sumární příznak v procesu analýzy, nezadávat přímo - bude vyhodnocen na základě ostatních požadavků a dle aktuálního stavu)
        /// </summary>
        PrepareAnyContent = 0x2000,

        /// <summary>
        /// Vytvoř grupy i SubItems
        /// </summary>
        CreateAll = CreateGroupsContent | CreateAllSubItems

    }
    #endregion
    #region IDxRibbonInternal : Interface pro interní přístup do ribbonu
    /// <summary>
    /// Interface pro interní přístup do ribbonu
    /// </summary>
    public interface IDxRibbonInternal
    {
        /// <summary>
        /// Metoda je volána při aktivaci stránky this Ribbonu v situaci, kdy tato stránka má aktivní nějaký Lazy režim pro načtení svého obsahu.
        /// Může to být prostě opožděné vytváření fyzických controlů z dat v paměti, nebo reálné OnDemand donačítání obsahu z aplikačního serveru.
        /// Přidá prvky do this Ribbonu z dodané LazyGroup do this Ribbonu. Zde se prvky přidávají vždy jako reálné, už ne Lazy.
        /// </summary>
        /// <param name="lazyGroup"></param>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        void PrepareRealLazyItems(DxRibbonLazyLoadInfo lazyGroup, bool isCalledFromReFill);
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány grupy.
        /// QAT by si měl odpovídající prvky deaktivovat.
        /// </summary>
        /// <param name="groupsToDelete"></param>
        void RemoveGroupsFromQat(List<DevExpress.XtraBars.Ribbon.RibbonPageGroup> groupsToDelete);
        /// <summary>
        /// Volá se v procesu <see cref="DxRibbonPage.ClearContent(bool, bool)"/>, před tím než jsou odebrány prvky.
        /// QAT by si měl odpovídající prvky deaktivovat.
        /// </summary>
        /// <param name="itemsToDelete"></param>
        void RemoveItemsFromQat(List<DevExpress.XtraBars.BarItem> itemsToDelete);
        /// <summary>
        /// Upraví dodaný <see cref="BarItemLink"/> pro zobrazení v QAT
        /// </summary>
        /// <param name="qatLink"></param>
        void ModifyLinkForQat(BarItemLink qatLink);
        /// <summary>
        /// Aktuální TimeStamp, naposledy přidělený některé stránce
        /// </summary>
        int CurrentTimeStamp { get; }
        /// <summary>
        /// Vrátí next TimeStamp
        /// </summary>
        /// <returns></returns>
        int GetNextTimeStamp();
        /// <summary>
        /// Odebere dané grupy z interní evidence Ribbonu
        /// </summary>
        /// <param name="groupsToDelete"></param>
        void RemoveGroups(IEnumerable<DxRibbonGroup> groupsToDelete);
        /// <summary>
        /// Uživatel aktivoval danou stránku.
        /// Volá se na tom Ribbonu, který danou stránku deklaroval. Mergovaný TopRibbon tedy zajistí, že najde všechny mergované Child Ribbony a pošle se událost do všech.
        /// </summary>
        /// <param name="dxRibbonPage"></param>
        void OnActivatePage(DxRibbonPage dxRibbonPage);
    }
    #endregion
    #region DxRibbonPage : stránka Ribbonu s podporou LazyContentItems, class DxRibbonPageLazyGroupInfo
    /// <summary>
    /// Kategorie
    /// </summary>
    public class DxRibbonPageCategory : DevExpress.XtraBars.Ribbon.RibbonPageCategory
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonPageCategory() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public DxRibbonPageCategory(string text, Color? color) : base() 
        {
            this.Text = text;
            this.CategoryColor = color;
            this.Visible = true;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="visible"></param>
        public DxRibbonPageCategory(string text, Color? color, bool visible) : base()
        {
            this.Text = text;
            this.CategoryColor = color;
            this.Visible = visible;
        }
        /// <summary>
        /// Konstruktor pro danou definici
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <param name="categories"></param>
        public DxRibbonPageCategory(IRibbonCategory iRibbonCategory, RibbonPageCategoryCollection categories = null) : base()
        {
            this.Fill(iRibbonCategory, true);
            if (categories != null) categories.Add(this);
        }
        /// <summary>
        /// Aktualizuje svoje vlastnosti z dodané definice
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <param name="withId">Aktualizuj i ID</param>
        public void Fill(IRibbonCategory iRibbonCategory, bool withId = false)
        {
            if (withId) this.Name = iRibbonCategory.CategoryId;
            this.Text = iRibbonCategory.CategoryText;
            this.CategoryColor = iRibbonCategory.CategoryColor;
            this.Visible = iRibbonCategory.CategoryVisible;
            this.CategoryData = iRibbonCategory;
            this.Tag = iRibbonCategory;
            iRibbonCategory.RibbonCategory = this;
        }
        /// <summary>
        /// Barva kategorie, může být null
        /// </summary>
        public Color? CategoryColor
        {
            get { return (this.Appearance.Options.UseBackColor ? (Color?)this.Appearance.BackColor : (Color?)null); }
            set
            {
                if (value.HasValue)
                {
                    this.Appearance.BackColor = value.Value;
                    this.Appearance.Options.UseBackColor = true;
                }
                else
                {
                    this.Appearance.BackColor = Color.Empty;
                    this.Appearance.Options.UseBackColor = false;
                }
            }
        }
        /// <summary>
        /// Data definující kategorii
        /// </summary>
        public IRibbonCategory CategoryData { get; private set; }
    }
    /// <summary>
    /// Stránka Ribbonu s vlastností LazyContentItems
    /// </summary>
    public class DxRibbonPage : DevExpress.XtraBars.Ribbon.RibbonPage
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        public DxRibbonPage(string text) : base(text)
        {
            this.Init(null);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="ribbon"></param>
        /// <param name="text"></param>
        public DxRibbonPage(DxRibbonControl ribbon, string text) : base(text)
        {
            this.Init(ribbon);
        }
        /// <summary>
        /// Konstruktor pro danou definici
        /// </summary>
        /// <param name="ribbon"></param>
        /// <param name="iRibbonPage"></param>
        /// <param name="pages"></param>
        public DxRibbonPage(DxRibbonControl ribbon, IRibbonPage iRibbonPage, RibbonPageCollection pages = null) : base()
        {
            this.Init(ribbon);
            this.Fill(iRibbonPage, true);
            if (pages != null) pages.Add(this);
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="ribbon"></param>
        protected void Init(DxRibbonControl ribbon)
        {
            this.LazyLoadInfo = null;
            this.__PageId = null;
        }
        /// <summary>
        /// Aktualizuje svoje vlastnosti z dodané definice
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="withId">Aktualizuj i ID</param>
        public void Fill(IRibbonPage iRibbonPage, bool withId = false)
        {
            if (withId) this.Name = iRibbonPage.PageId;
            if (this.__PageId is null) this.__PageId = iRibbonPage.PageId;
            this.Text = iRibbonPage.PageText;
            if (iRibbonPage.MergeOrder > 0) this.MergeOrder = iRibbonPage.MergeOrder;          // Záporné číslo iRibbonPage.MergeOrder říká: neměnit hodnotu, pokud stránka existuje. Důvod: při Refreshi existující stránky nechceme měnit její pozici.
            this.Visible = iRibbonPage.Visible;
            this.PageData = iRibbonPage;
            this.Tag = iRibbonPage;
            iRibbonPage.RibbonPage = this;
        }
        /// <summary>
        /// Uvolní svoje interní reference
        /// </summary>
        public void Reset()
        {
            IRibbonPage iRibbonPage = this.PageData;
            this.LazyLoadInfo = null;
            this.PageData = null;
            this.Tag = null;
            if (iRibbonPage != null) iRibbonPage.RibbonPage = null;
        }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        protected DxRibbonControl OwnerDxRibbon { get { return this.Ribbon as DxRibbonControl; } }
        /// <summary>
        /// Vlastník Ribbon přetypovaný na <see cref="IDxRibbonInternal"/>
        /// </summary>
        protected IDxRibbonInternal IOwnerDxRibbon { get { return OwnerDxRibbon; } }
        /// <summary>
        /// ID této stránky.
        /// </summary>
        public string PageId { get { return __PageId; } } private string __PageId;
        /// <summary>
        /// Data definující stránku a její obsah
        /// </summary>
        public IRibbonPage PageData { get; private set; }
        /// <summary>
        /// ID poslední aktivace této stránky. Při aktivaci stránky se volá metoda <see cref="OnActivate(bool)"/>.
        /// Slouží k určení stránky, která má být aktivní.
        /// </summary>
        public int ActivateTimeStamp { get; private set; }
        /// <summary>
        /// Vyvolá se po každé aktivaci stránky, inkrementuje <see cref="ActivateTimeStamp"/> a vyvolá událost <see cref="DxRibbonControl.SelectedDxPageChanged"/> prostřednictvím <see cref="IDxRibbonInternal.OnActivatePage(DxRibbonPage)"/>.
        /// </summary>
        /// <param name="force">Požadavek na provedení aktivace i pro takový Ribbon, který ještě nebyl zobrazen uživateli (používá se při inicializaci pro aktivaci první stránky)</param>
        public void OnActivate(bool force = false)
        {
            // Pokud obsah Ribbonu dosud nebyl zobrazen uživateli (OwnerDxRibbon.IsContentAlreadyPainted), pak nebudu aktivovat stránku = jde o Mergovací hrátky Ribbonu před jeho vlastním zobrazením:
            bool canActivate = (this.OwnerDxRibbon != null && (force || this.OwnerDxRibbon.IsContentAlreadyPainted));
            if (canActivate)
            {
                if (this.IOwnerDxRibbon != null && (this.ActivateTimeStamp == 0 || this.ActivateTimeStamp < this.IOwnerDxRibbon.CurrentTimeStamp))
                {   // Pokud naše (Page) hodnota ActivateTimeStamp je menší, než jakou eviduje náš Ribbon, je zjevné že Ribbon už nějakou další hodnotu přidělil nějaké jiné stránce.
                    // Pak tedy aktivace this stránky (zdejší metoda) je reálnou změnou aktivní stránky, a ne jen opakovanou akcí na téže stránce...
                    ActivateTimeStamp = this.IOwnerDxRibbon.GetNextTimeStamp();
                    this.IOwnerDxRibbon.OnActivatePage(this);
                }
            }
        }
        /// <summary>
        /// true pokud this stránka má nějaký aktivní LazyLoad obsah
        /// </summary>
        internal bool HasActiveLazyContent { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.HasActiveLazyContent); } }
        /// <summary>
        /// true pokud this stránka má LazyLoad obsah typu Static
        /// </summary>
        internal bool HasActiveStaticLazyContent { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.HasActiveStaticLazyContent); } }
        /// <summary>
        /// Definiční data této stránky Ribbonu z LazyLoad
        /// </summary>
        internal IRibbonPage LazyStaticPageData { get { return this.LazyLoadInfo.PageData; } }
        /// <summary>
        /// true pokud this stránka má aktivní LazyLoad obsah v režimu <see cref="RibbonContentMode.OnDemandLoadEveryTime"/>
        /// </summary>
        internal bool IsLazyLoadEveryTime { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime); } }
        /// <summary>
        /// Obsahuje true v případě, kdy stránka obsahuje pouze QAT (Quick Access Toolbar) prvky. Při reálném naplnění daty se musí taková stránka smazat, budou se do ní plnit všechny prvky ve správném pořadí.
        /// </summary>
        internal bool HasOnlyQatContent { get; set; }
        /// <summary>
        /// Podle požadovaného režimu tvorby připraví this stránku pro tvorbu jejího obsahu, výstupem je reálný režim tvorby v aktuální situaci.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="createMode"></param>
        /// <param name="isActivePage"></param>
        /// <param name="pageContainsQatItems"></param>
        /// <returns></returns>
        internal DxRibbonCreateContentMode PreparePageForCreateContent(IRibbonPage iRibbonPage, DxRibbonCreateContentMode createMode, bool isActivePage, bool pageContainsQatItems)
        {
            bool isOnDemandFill = createMode.HasFlag(DxRibbonCreateContentMode.RunningOnDemandFill);              // OnDemand Fill - ovlivní tvorbu a přenáší se do výstupu
            bool isOnIdleFill = createMode.HasFlag(DxRibbonCreateContentMode.RunningOnIdleFill);                  // OnIdle Fill - ovlivní tvorbu a přenáší se do výstupu
            bool isCreateAuto = createMode.HasFlag(DxRibbonCreateContentMode.CreateAutoByCount);                  // Auto = podle počtu věcí na stránce
            bool canAutoCreateGroups = false;
            bool canAutoCreateSubItems = false;
            if (isCreateAuto) _DetectAutoCreateMode(iRibbonPage, ref canAutoCreateGroups, ref canAutoCreateSubItems);    // Auto režim určí možnosti podle obsahu stránky

            bool isCreateAllSubItems = createMode.HasFlag(DxRibbonCreateContentMode.CreateAllSubItems) || canAutoCreateSubItems || isOnIdleFill;                            // Subpoložky: dle požadavku anebo v OnIdleFill
            bool isCreateGroupContent = createMode.HasFlag(DxRibbonCreateContentMode.CreateGroupsContent) || isCreateAllSubItems || canAutoCreateGroups || isActivePage;    // Základní obsah stránky generuji když je to vyžádáno, nebo když se generují SubItems (tedy v OnIdleFill), anebo když je stránka aktivní
            bool isCreateOnlyQatItems = (!isCreateGroupContent && (pageContainsQatItems && createMode.HasFlag(DxRibbonCreateContentMode.CreateOnlyQATItems)));
            bool isPrepareAnyContent = (isCreateGroupContent || isCreateAllSubItems || isCreateOnlyQatItems);     // Budu vůbec něco generovat?
            var pageContentMode = iRibbonPage.PageContentMode;

            // Budu potřebovat něco řešit OnIdle? To bude když obsah je dán staticky (nikoli OnDemand, tedy máme obsah k dispozici), ale nebudu ho generovat nyní (buď grupy a prvky, anebo subItemy a ty přitom někde existují):
            bool activateOnIdle = (pageContentMode == RibbonContentMode.Static && (!isCreateGroupContent || (!isCreateAllSubItems && DxRibbonControl.ContainsAnyStaticSubItems(iRibbonPage))));

            // Potřebujeme nebo nepotřebujeme LazyInfo (tj. data pro budoucí OnDemand tvorbu obsahu nebo požadavek na donačtení obsahu ze serveru)?
            bool createLazyInfo = (activateOnIdle || isCreateOnlyQatItems || pageContentMode == RibbonContentMode.OnDemandLoadEveryTime || (pageContentMode == RibbonContentMode.OnDemandLoadOnce   /*   && !isOnDemandFill   */ ));
            if (createLazyInfo)
                PrepareLazyLoadInfo(iRibbonPage);
            else
                RemoveLazyLoadInfo();

            // Co bude s QAT prvky?  Pokud this stránka má pouze QAT prvky, a nyní budu generovat nějaké standardní, pak dosavadní musíme smazat:
            if (this.HasOnlyQatContent && isCreateGroupContent)
                this.ClearContent(true, false);
            this.HasOnlyQatContent = isCreateOnlyQatItems;                     // Příznak, že tato stránka bude obsahovat jen QAT prvky - pro příští kolečko plnění...

            // Jaké prvky do stránky Ribbonu tedy nyní budeme generovat?
            DxRibbonCreateContentMode currentMode =
                (isOnDemandFill ? DxRibbonCreateContentMode.RunningOnDemandFill : DxRibbonCreateContentMode.None) |
                (isCreateAllSubItems ? DxRibbonCreateContentMode.CreateAllSubItems : DxRibbonCreateContentMode.None) |
                (isCreateGroupContent ? DxRibbonCreateContentMode.CreateGroupsContent : DxRibbonCreateContentMode.None) |
                (isCreateOnlyQatItems ? DxRibbonCreateContentMode.CreateOnlyQATItems : DxRibbonCreateContentMode.None) |
                (activateOnIdle ? DxRibbonCreateContentMode.ActivateOnIdleLoad : DxRibbonCreateContentMode.None) |
                (isPrepareAnyContent ? DxRibbonCreateContentMode.PrepareAnyContent : DxRibbonCreateContentMode.None);

            return currentMode;
        }
        /// <summary>
        /// Auto režim určí možnosti Lazy/Direct load prvků podle obsahu stránky
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="canAutoCreateGroups"></param>
        /// <param name="canAutoCreateSubItems"></param>
        private void _DetectAutoCreateMode(IRibbonPage iRibbonPage, ref bool canAutoCreateGroups, ref bool canAutoCreateSubItems)
        {
            if (iRibbonPage is null) return;

            if (iRibbonPage.Groups is null || !iRibbonPage.Groups.Any())
            {   // žádná grupa:
                canAutoCreateGroups = true;
                canAutoCreateSubItems = true;
                return;
            }

            // Automaticky vytvořit grupy pro stránku tehdy, pokud počet prvků v základní úrovni nepřesáhne 50:
            int itemsCount = iRibbonPage.Groups.Sum(g => g.Items.Count());     // Součet počtu prvků z jednotlivých skupin v Root úrovni
            if (itemsCount <= 50)
                canAutoCreateGroups = true;

            // Automaticky vytvořit SubItems pro stránku tehdy, pokud počet SubItems všech Items v základní úrovni nepřesáhne 50:
            var items = iRibbonPage.Groups.SelectMany(g => g.Items).Where(i => i.SubItems != null).ToArray();   // Souhrn Root prvků ze všech grup, kde tyto prvky mají SubItems
            int subItemsCount = items.Sum(i => i.SubItems.Count());            // Součet počtu SubItems z Root prvků z jednotlivých skupin
            if (subItemsCount <= 50)
                canAutoCreateSubItems = true;
        }
        /// <summary>
        /// Zajistí přípravu prvku LazyInfo pro režim definovaný v daném prvku. Prvek si uschová.
        /// Založí grupu pro LazyLoad - pokud dosud neexistuje a je potřebná (LazyLoad je realizován pomocí grupy, která obsahuje prvek s příznakem LazyLoad).
        /// Vrátí true, pokud se v dané situaci má generovat reálný obsah do this stránky Ribbonu, false pokud ne.
        /// </summary>
        /// <param name="pageData">Deklarace stránky</param>
        /// <param name="isLazyContentFill">Obsahuje true, když se prvky typu Group a BarItem NEMAJÍ fyzicky generovat (mají se jen registrovat do LazyGroup) / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemandFill">Obsahuje true, pokud tyto položky jsou právě nyní donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        /// <param name="isStaticLazyContent"></param>
        internal bool PreparePageForContent(IRibbonPage pageData, bool isLazyContentFill, bool isOnDemandFill, out bool isStaticLazyContent)
        {
            // Určíme, zda budeme potřebovat LazyInfo objekt (a tedy LazyGroup grupu v GUI Ribbonu):
            // a) podle režimu práce s obsahem (pageData.PageContentMode je některý OnDemandLoad)
            // b) podle aktuálních příznaků (isLazyContentFill, isOnDemandFill)
            var contentMode = pageData.PageContentMode;
            bool createLazyInfo = (isLazyContentFill ||
                                   contentMode == RibbonContentMode.OnDemandLoadEveryTime ||
                                   (contentMode == RibbonContentMode.OnDemandLoadOnce && !isOnDemandFill));
            isStaticLazyContent = (isLazyContentFill && contentMode == RibbonContentMode.Static);

            if (!createLazyInfo)
            {   // Pokud nebudeme používat LazyInfo, pak případný existující objekt LazyInfo zrušíme (včetně GUI grupy), a vrátím true = bude se generovat plné GUI:
                RemoveLazyLoadInfo();
                return true;
            }
            else
            {   // Potřebujeme LazyInfo:
                PrepareLazyLoadInfo(pageData);
            }

            // Výstupem bude true, pokud NENÍ LazyFill (=musíme generovat data) anebo pokud JE OnDemand (=přišla reálná data pro konkrétní stránku, musíme je do ní zobrazit):
            return (!isLazyContentFill || isOnDemandFill);
        }
        /// <summary>
        /// Vytvoří (pokud je třeba) a vrátí instanci <see cref="DxRibbonLazyLoadInfo"/> pro ukládání informací pro LazyContent, a odpovídající GUI grupu vloží do this.Groups
        /// </summary>
        /// <param name="pageData"></param>
        /// <returns></returns>
        protected DxRibbonLazyLoadInfo PrepareLazyLoadInfo(IRibbonPage pageData)
        {
            DxRibbonLazyLoadInfo lazyLoadInfo = LazyLoadInfo;
            if (lazyLoadInfo == null)
            {
                lazyLoadInfo = new DxRibbonLazyLoadInfo(OwnerDxRibbon, this, pageData);
                this.Groups.Add(lazyLoadInfo.Group);
                lazyLoadInfo.Group.Visible = false;        // Vložení grupy do this.Groups nastavuje Group.Visible = true, to ale nechceme (je to dáno chováním AutoHide pro prázdné grupy / 
                lazyLoadInfo.Group.GroupVisible = false;
                LazyLoadInfo = lazyLoadInfo;
            }
            return lazyLoadInfo;
        }
        /// <summary>
        /// Odstraní z this stránky celou LazyGroup - data i reálnou grupu
        /// </summary>
        public void RemoveLazyLoadInfo()
        {
            var lazyGroupInfo = LazyLoadInfo;
            if (lazyGroupInfo != null)
            {
                this.Groups.Remove(lazyGroupInfo.Group);
                lazyGroupInfo.Clear();
                LazyLoadInfo = null;
            }
        }
        /// <summary>
        /// Metoda je volána při aktivaci stránky this Ribbonu v situaci, kdy tato stránka má aktivní nějaký Lazy režim pro načtení svého obsahu.
        /// Může to být prostě opožděné vytváření fyzických controlů z dat v paměti, nebo reálné OnDemand donačítání obsahu z aplikačního serveru.
        /// Přidá prvky do this Ribbonu z dodané LazyGroup do this Ribbonu. Zde se prvky přidávají vždy jako reálné, už ne Lazy.
        /// </summary>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        public void PrepareRealLazyItems(bool isCalledFromReFill)
        {
            if (this.HasActiveLazyContent)
                this.IOwnerDxRibbon.PrepareRealLazyItems(this.LazyLoadInfo, isCalledFromReFill);
        }
        /// <summary>
        /// Definice dat pro LazyLoad content pro tuto Page. Obsahuje deklarace prvků i referenci na grupu, která LazyLoad zajistí.
        /// </summary>
        protected DxRibbonLazyLoadInfo LazyLoadInfo { get; private set; }
        /// <summary>
        /// Obsahuje všechny grupy a jejich prvky, které jsou fyzicky přítomné v this stránce.
        /// Jde o pole, jehož prvky jsou Tuple s dvěma prvky, kde Item1 je samotná grupa, a Item2 je pole prvků BarItem, přítomných v dané grupě.
        /// Z tohoto pole lze poměrně snadno sestavit jednoduché pole všech BarItemů:
        /// <code>
        /// var barItems = GroupItems.SelectMany(gi => gi.Item2).ToArray();
        /// </code>
        /// </summary>
        internal Tuple<RibbonPageGroup, BarItem[]>[] GroupItems
        {
            get
            {
                List<Tuple<RibbonPageGroup, BarItem[]>> groupItems = new List<Tuple<RibbonPageGroup, BarItem[]>>();

                string lazyGroupId = DxRibbonLazyLoadInfo.LazyLoadGroupId;
                foreach (RibbonPageGroup group in this.Groups)
                {
                    if (group.Name == lazyGroupId) continue;
                    groupItems.Add(new Tuple<RibbonPageGroup, BarItem[]>(group, group.ItemLinks.Select(l => l.Item).ToArray()));
                }
                var barItems = groupItems.SelectMany(gi => gi.Item2).ToArray();
                return groupItems.ToArray();
            }
        }
        /// <summary>
        /// Smaže obsah dané stránky.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="clearUserItems">Smazat uživatelské prvky</param>
        /// <param name="clearLazyGroup">Smazat LazyInfo</param>
        internal static void ClearContentPage(DevExpress.XtraBars.Ribbon.RibbonPage page, bool clearUserItems = true, bool clearLazyGroup = true)
        {
            if (page is DxRibbonPage dxRibbonPage)
                dxRibbonPage.ClearContent(clearUserItems, clearLazyGroup);
            else
            {
                page.Groups.Clear();
            }
        }
        /// <summary>
        /// Smaže obsah this stránky: grupy i jejich Itemy i LazyInfo.
        /// </summary>
        internal void ClearContent()
        {
            ClearContent(true, true);
        }
        /// <summary>
        /// Smaže obsah this stránky: grupy i jejich Itemy. Volitelně i LazyInfo.
        /// </summary>
        /// <param name="clearUserItems">Smazat uživatelské prvky</param>
        /// <param name="clearLazyGroup">Smazat LazyInfo</param>
        internal void ClearContent(bool clearUserItems, bool clearLazyGroup)
        {
            if (clearUserItems)
            {   // Standardní grupy a itemy:
                var iOwnerRibbon = this.OwnerDxRibbon as IDxRibbonInternal;

                var groupItems = this.GroupItems;
                var groupsToDelete = groupItems.Select(gi => gi.Item1).ToList();
                var itemsToDelete = groupItems.SelectMany(gi => gi.Item2).ToList();
                iOwnerRibbon.RemoveGroups(groupsToDelete.OfType<DxRibbonGroup>());

                // Před fyzickým odebráním grup a prvků z RibbonPage je předám do QAT systému v Ribbonu, aby si je odebral ze své evidence: 
                iOwnerRibbon.RemoveGroupsFromQat(groupsToDelete);
                iOwnerRibbon.RemoveItemsFromQat(itemsToDelete);

                var ownerRibbonItems = this.OwnerDxRibbon.Items;
                var groups = this.Groups;
                groupsToDelete.ForEach(g => groups.Remove(g));
                itemsToDelete.ForEach(i => ownerRibbonItems.Remove(i));

                HasOnlyQatContent = false;
            }
            if (clearLazyGroup)
            {   // Lazy group a její itemy:
                this.RemoveLazyLoadInfo();
            }
        }
    }
    /// <summary>
    /// Grupa v Ribbonu
    /// </summary>
    public class DxRibbonGroup : RibbonPageGroup
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonGroup() : base() { Init(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        public DxRibbonGroup(string text) : base(text) { Init(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="groups"></param>
        public DxRibbonGroup(IRibbonGroup iRibbonGroup, RibbonPageGroupCollection groups) : base()
        {
            if (groups != null) groups.Add(this);
            Init();
            Fill(iRibbonGroup, true);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.Reset();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Init()
        {
            State = DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto;
            __GroupId = null;
            __HideEmptyGroup = false;
            __GroupVisible = true;
            __GroupDataList = new List<IRibbonGroup>();
            this.ItemLinks.CollectionChanged += ItemLinks_CollectionChanged;
        }
        /// <summary>
        /// Aktualizuje svoje vlastnosti z dodané definice.
        /// Do grupy nevkládá prvky.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="withId">Aktualizuj i ID</param>
        public void Fill(IRibbonGroup iRibbonGroup, bool withId = false)
        {
            if (withId) this.Name = iRibbonGroup.GroupId;
            if (this.__GroupId is null) this.__GroupId = iRibbonGroup.GroupId;           // První vložení dat grupy nasetuje její ID, to pak zůstává neměnné.
            this.Text = iRibbonGroup.GroupText;
            if (iRibbonGroup.MergeOrder > 0) this.MergeOrder = iRibbonGroup.MergeOrder;  // Záporné číslo IRibbonGroup.MergeOrder říká: neměnit hodnotu, pokud grupa existuje. Důvod: při Refreshi existující grupy nechceme měnit její pozici.
            this.CaptionButtonVisible = (iRibbonGroup.GroupButtonVisible ? DefaultBoolean.True : DefaultBoolean.False);
            this.AllowTextClipping = iRibbonGroup.AllowTextClipping;
            this.State = (iRibbonGroup.GroupState == RibbonGroupState.Expanded ? RibbonPageGroupState.Expanded :
                         (iRibbonGroup.GroupState == RibbonGroupState.Collapsed ? RibbonPageGroupState.Collapsed :
                          DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto));
            this.ItemsLayout = (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.Default ? RibbonPageGroupItemsLayout.Default :
                               (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.OneRow ? RibbonPageGroupItemsLayout.OneRow :
                               (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.TwoRows ? RibbonPageGroupItemsLayout.TwoRows :
                               (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.ThreeRows ? RibbonPageGroupItemsLayout.ThreeRows :
                                RibbonPageGroupItemsLayout.Default))));
            this.HideEmptyGroup = iRibbonGroup.HideEmptyGroup;
            this.GroupVisible = iRibbonGroup.Visible;
            DxComponent.ApplyImage(this.ImageOptions, iRibbonGroup.GroupImageName, null, DxRibbonControl.RibbonImageSize);
            this.__GroupDataList.Add(iRibbonGroup);
            this.Tag = iRibbonGroup;                       // Nouzové úložiště pro mergované grupy, kdy při mergování se přenáší Tag
            iRibbonGroup.RibbonGroup = this;
            RefreshGroupVisibility();
        }
        /// <summary>
        /// Znovu aplikuje Image ze svého objektu <see cref="DataGroups"/>
        /// </summary>
        public void ReApplyImage()
        {
            IRibbonGroup iRibbonGroup = this.DataGroupLast;
            if (iRibbonGroup != null)
                DxComponent.ApplyImage(this.ImageOptions, iRibbonGroup.GroupImageName, null, DxRibbonControl.RibbonImageSize);
        }
        /// <summary>
        /// Uvolní svoje interní reference
        /// </summary>
        public void Reset()
        {
            __GroupDataList.ForEachExec(d => d.RibbonGroup = null);
            __GroupDataList.Clear();
            this.Tag = null;
        }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        internal DxRibbonControl OwnerDxRibbon { get { return this.Ribbon as DxRibbonControl; } }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        internal DxRibbonPage OwnerDxPage { get { return this.Page as DxRibbonPage; } }
        /// <summary>
        /// ID stránky, kam patří této grupa. Za jejího života se nemění, je společné pro všechny deklarace v <see cref="DataGroups"/>.
        /// </summary>
        public string PageId { get { return OwnerDxPage?.PageId; } }
        /// <summary>
        /// ID této grupy. Za jejího života se nemění, je společné pro všechny deklarace v <see cref="DataGroups"/>.
        /// </summary>
        public string GroupId { get { return __GroupId; } } private string __GroupId;
        /// <summary>
        /// Posledně přidaná definice grupy = zdroj nejčerstvějších dat grupy. 
        /// Nemusí obsahovat všechny Itemy, jejich kompletní souhrn je v jednotlivých definicích grupách <see cref="DataGroups"/>, sumárně v <see cref="DataItems"/>
        /// </summary>
        public IRibbonGroup DataGroupLast { get { var count = __GroupDataList.Count; return (count > 0 ? __GroupDataList[count - 1] : null); } }
        /// <summary>
        /// Data definující grupu a její obsah.
        /// Protože dovolujeme, aby více definic grup mělo shodné ID <see cref="IRibbonGroup.GroupId"/>, a povolujeme prvky z těchto grup přidávat v režimu Add,
        /// pak dojde k tomu, že máme v jedné vizuální grupě data (viditelné prvky) z vícero definic.
        /// Teprve až vložíme definici grupy v režimu Reload, pak budou staré definice zahozeny a bude přítomna jen jedna definice.
        /// </summary>
        internal IRibbonGroup[] DataGroups { get { return __GroupDataList.ToArray(); } } private List<IRibbonGroup> __GroupDataList;
        /// <summary>
        /// Data všech Itemů v této vizuální grupě. Je zde souhrn <see cref="IRibbonGroup.Items"/> ze všech aktuálních <see cref="DataGroups"/>.
        /// </summary>
        internal IRibbonItem[] DataItems { get { return DataGroups.SelectMany(g =>g.Items).ToArray(); } }
        /// <summary>
        /// Smaže obsah this grupy.
        /// Neprovádí <see cref="Reset()"/>.
        /// </summary>
        public void ClearContent()
        {
            var oneItemsToDelete = this.ItemLinks.Select(l => l.Item);                                // Grupa v sobě obsahuje BarItemLinky, z nich získám BarItemy
            var allItemsToDelete = oneItemsToDelete.Flatten(i => DxRibbonControl.GetSubItems(i));     // Z BarItemů v grupě získám i všechny jejich SubItems, rekurzivně - pokud prvek nějaké vnořené má (SubItem, PopupItem)
            var itemsToDelete = allItemsToDelete.Select(t => t.Item2).ToList();                       // allItemsToDelete obsahuje Tuple, kde Item1 = Level a Item2 = prvek

            // Před fyzickým odebráním prvků z RibbonGroup je předám do QAT systému v Ribbonu, aby si je odebral ze své evidence:
            var iOwnerRibbon = this.OwnerDxRibbon as IDxRibbonInternal;
            iOwnerRibbon.RemoveItemsFromQat(itemsToDelete);

            this.ItemLinks.Clear();

            var ownerRibbonItems = this.OwnerDxRibbon.Items;
            itemsToDelete.ForEach(i => ownerRibbonItems.Remove(i));

            // Odebrat definice:
            __GroupDataList.ForEachExec(d => d.RibbonGroup = null);
            __GroupDataList.Clear();
        }
        /// <summary>
        /// Požadavek (true) na skrývání grupy, která neobsahuje žádné prvky.<br/>
        /// Default = false: pokud bude dodána prázdná grupa, bude zobrazena.
        /// </summary>
        public bool HideEmptyGroup { get { return __HideEmptyGroup; } set { __HideEmptyGroup = value; RefreshGroupVisibility(); } } private bool __HideEmptyGroup;
        /// <summary>
        /// Viditelnost grupy podle nastavení dat <see cref="IRibbonGroup.Visible"/>, default = true
        /// </summary>
        public bool GroupVisible { get { return __GroupVisible; } set { __GroupVisible = value; RefreshGroupVisibility(); } } private bool __GroupVisible;
        /// <summary>
        /// Po změně prvků zobrazených v této grupě se vyvolá tato metoda, a zajistí nastavení viditelnosti grupy podle počtu prvků a nastavení.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemLinks_CollectionChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e)
        {
            RefreshGroupVisibility();
        }
        /// <summary>
        /// Aktualizuje nastavení viditelnosti grupy podle počtu prvků a nastavení.
        /// Je nutno volat i po změně Visibility na prvcích, to grupa sama nedokáže hlídat.
        /// </summary>
        public void RefreshGroupVisibility()
        {
            bool visible = this.GroupVisible;
            if (visible && HideEmptyGroup && !this._HasVisibleItems) visible = false;
            this.Visible = visible;
        }
        /// <summary>
        /// Obsahuje pole prvků v této skupině, které jsou právě nyní viditelné
        /// </summary>
        private bool _HasVisibleItems { get { return this._VisibleItems.Length > 0; } }
        /// <summary>
        /// Obsahuje pole prvků v této skupině, které jsou právě nyní viditelné
        /// </summary>
        private BarItem[] _VisibleItems { get { return this.ItemLinks.Select(l => l.Item).Where(l => l.Visibility != BarItemVisibility.Never).ToArray(); } }
    }
    /// <summary>
    /// Třída, která pomáhá řešit opožděné načítání a tvorbu prvků v Ribbonu.
    /// <para/>
    /// Ribbon má: Kategorie stránek (=kontextové, barevně zvýrazněné), pak samostatné stránky (běžně viditelné),
    /// ve stránkách grupy a v grupách prvky, a v prvcích pak subItems (SplitButton, Menu).
    /// Tvorba celého obsahu zabere jistý čas, podle složitosti (počtu prvků) od 100 do 1000 milisekund.
    /// Přičemž samotná tvorba Ribbonu a řádově 10 stránek bez jejich obsahu (bez Grup a BarItem) zabere cca 5 milisekund.
    /// Tvorba obsahu jedné stránky (její Grupy a BarItemy) zabere průměrně 100 milisekund.
    /// Uživatelsky je 100 milisekund čas nepostřehnutelný, ale čas 1000 milisekund je viditelně pomalý projev.
    /// <para/>
    /// Existují v zásadě čtyři způsoby, jak mít v Ribbonu připraven obsah (Grupy a BarItemy):
    /// 1. Statický = vytvořený při tvorbě Ribbonu. Výhoda = žádné složité programování. Nevýhoda = čas až 1000 milisekund, nemožnost dynamické změny obsahu.
    /// 2. LazyLoad = při tvorbě Ribbonu se vygenerují stránky bez obsahu, a někam se uloží definice obsahu pro každou stránku.
    ///     Reálný obsah se vygeneruje až při první aktivaci konkrétní jedné stránky. 
    ///     Výhoda = rychlost tvorby ribbonu (10 ms). Nevýhoda = programování, nemožnost dynamické změny obsahu.
    ///     Důsledek: první aktivace stránky trvá cca 100 ms. To uživateli nevadí. 
    ///     Styl naprogramování zajistí, že po aktivaci stránky nebude ani po krátký čas zobrazena prázdná stránka Ribbonu, 
    ///     ale ribbon viditelně překlikne na novou stránku až poté, kdy je naplněna obsahem.
    /// 3. OnDemandLoadOnce = při tvorbě Ribbonu se vygenerují stránky bez obsahu, a někam se uloží definice pro donačtení obsahu ze serveru, pro každou nativní stránku jedna.
    ///     Při aktivaci stránky se zobrazí prázdná stránka, na pozadí proběhne dotaz na server, který teprve přinese data pro tuto stránku, a poté se do stránky obsah promítne.
    ///     Výhoda proti bodu 2: zmenšení obsahu dat přenášených na klienta při inicializaci, aktuálnost dat ze serveru
    ///     Nevýhoda proti bodu 2: viditelně pomalejší naplnění stránky Ribbonu. Zobrazí se prázdná stránka, na kterou se až po nějakém čase (komunikace + 100ms) nahraje obsah.
    /// 4. OnDemandLoadEveryTime = jako ad. 3, ale dotaz na server probíhá při každé aktivaci stránky Ribbonu, ne jen při první.
    ///     Výhoda proti bodu 3: umožní se dynamický obsah Ribbonu
    /// <para/>
    /// Chování DevExpress Ribbonu:
    /// a) jednotlivý Ribbon: na aktivaci stránky reagujeme v události OnSelectedPageChanged()
    /// b) najdeme podklady o LazyLoad, vyřešíme je
    /// c) mergovaný Ribbon: to je chuťovka, protože:
    ///   - Každá stránka každého Ribbonu se může chovat jinak (jiný režim lazyLoad)
    ///   - Mergování může vytvořit:
    ///      a. Novou mergovanou stránku, ve které je původní zdrojová stránka
    ///      b. Novou mergovanou stránku, ve které je spojeno více zdrojových stránek (z více pod sebou zařazených Ribbonů)
    ///      c. V Parent Ribbonu zůstane jeho nativní stránka, do které se přimergují jednotlivé grupy z podřízených stránek (jedné nebo více)
    ///      d. jako c. ale i grupy se mohou zmergovat do jedné grupy, pokud mají shodný Name, a to do grupy Parent Ribbonu, anebo grupy více Child Ribbonů do jedné MergedGroup
    ///   - Mergování může spojit do jedné stránky data z více Child stránek, kde každá nativní Child stránka má jiný režim LazyLoad, typicky:
    ///      a. Parent Ribbon má obsah stránky "DOMŮ" staticky připravený
    ///      b. K tomu přijde z child Ribbonu stránka "DOMŮ" s LazyLoad obsahem (=má se vygenerovat při aktivaci stránky)
    ///      c. K tomů může teoreticky přijít z ještě hlubšího Chil-Child Ribbonu jeho stránka "DOMŮ" s obsahem typu "OnDemandLoadOnce"
    ///   - Tyhle věci je třeba řešit na úrovni toho Top Parent Ribbonu (ten jediný je viditelný) při jeho aktivaci stránky 
    ///      - poznat, co na stránce je (příspěvky z jednotlivých Child Ribbonů), zda tam něco takového je,
    ///      - a který Child Ribbon tam má nějaký LazyLoad obsah
    ///      - a předat to tomu Ribbonu k řešení
    /// <para/>
    /// Klacky pod nohy:
    ///  - Mergování Ribbonu = tvorba obsahu Parent Ribbonu na základě obsahu Child Ribbonů
    ///  - Do Parent Ribbonu se generují new instance Pages a Groups, takže nelze čekávat, že ve stránce / grupě v Parent Ribbonu najdu nějaké "moje" instance z mé Child stránky
    ///  - Není snadné se dostat ani na originální instance Groups z child Ribbonu, navíc více Childs Group se může mergovat do jedné grupy
    ///  - Jediné co se přenáší do Parent Ribbonu je Tag z prvního mergovaného objektu, ale když merguju více stránek nebo grup, tak Tag je jen z té první
    /// <para/>
    /// Možné a tedy použití řešení:
    ///  - Každá nativní Pages, která má LazyLoad (=jiný režim než Statický):
    ///    - si do sebe (DxRibbonPage) uloží jednu instanci <see cref="DxRibbonLazyLoadInfo"/>, která věc řídí, ale tahle instance se nedostává do MergedPages
    ///    - do svých Groups zařadí jednu novou specifickou grupu, která má přes celou aplikaci identické Name i Text (takže při mergování bude na výsledné stránce jen jedna grupa toho jména)
    ///    - do této grupy si zařadí jeden BarItem Button s unikátním ID
    ///    - do Tagu tohoto BarItem Buttonu si uloží instanci nativní stránky <see cref="DxRibbonPage"/>
    ///  - Při mergování do Parent Ribbonu vznikne jedna grupa s tím specifickým jménem (nebo nevznikne pokud žádná mergovaná stránka nebude mát nějaky LazyLoad režim)
    ///    - při aktivaci stránky se najde tato grupa a v ní BarItemLink na jednotlivé Buttony, ty v sobě v Tagu nesou odkaz na nativní stránku
    ///    - a nativní stránka si vyřeší LazyLoad podle své instance <see cref="DxRibbonLazyLoadInfo"/>
    /// </summary>
    public class DxRibbonLazyLoadInfo
    {
        /// <summary>
        /// Konstuktor
        /// </summary>
        /// <param name="ownerRibbon"></param>
        /// <param name="ownerPage"></param>
        /// <param name="iRibbonPage"></param>
        public DxRibbonLazyLoadInfo(DxRibbonControl ownerRibbon, DxRibbonPage ownerPage, IRibbonPage iRibbonPage)
        {
            this.OwnerRibbon = ownerRibbon;
            this.OwnerPage = ownerPage;
            this.PageData = iRibbonPage;
            this.PageContentMode = iRibbonPage.PageContentMode;
            this.CreateRibbonGui();
            this.IsActive = true;
        }
        /// <summary>
        /// Vytvoří GUI prvky pro tuto LazyGroup
        /// </summary>
        private void CreateRibbonGui()
        {
            this.Group = new DxRibbonGroup(LazyLoadGroupText)
            {
                Name = LazyLoadGroupId,
                Visible = false,
                GroupVisible = false,
                CaptionButtonVisible = DefaultBoolean.False
            };
            this.BarItem = OwnerRibbon.Items.CreateButton(LazyLoadButtonText);
            uint id = ++_LastLazyId;
            this.BarItem.Name = $"{PageData.PageId}_Wait_{id}";
            this.BarItem.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            this.BarItem.Tag = OwnerPage;

            Group.ItemLinks.Add(BarItem);
        }
        internal static string LazyLoadGroupId 
        {
            get 
            {
                if (__LazyLoadGroupId == null)
                    __LazyLoadGroupId = "LazyLoadGroupId_" + DxComponent.CreateGuid();
                return __LazyLoadGroupId;
            }
        }
        private static string __LazyLoadGroupId = null;
        internal static string LazyLoadGroupText { get { return "...Loading..."; } }
        internal static string LazyLoadButtonText { get { return "..."; } }
        /// <summary>
        /// Zajistí unikátní GroupId, tak aby se dvě různé LazyGroup při mergování nespojily do jedné.
        /// UInt32 je atomární hodnota.
        /// Max hodnota = 4 miliardy, tolik grup by se vygenerovalo při 20 grupách za sekundu až za 7 roků usilovné nepřetržité práce.
        /// </summary>
        private static uint _LastLazyId = 0;
        /// <summary>
        /// Metoda zjistí, zda na dané stránce jsou nějaké prvky k vytvoření v režimu LazyLoad.
        /// Pokud jsou, vrátí true a prvky dá do out parametru.
        /// Jinak vrací false.
        /// <para/>
        ///  DevExpress, Ribbony, Stránky a Mergování, podle zjištění Davida Janáčka:
        /// 1. Vytvořím Ribbon, v něm Pages a v nich Groups, a do nich dávám BarItems = docela jednoduchá pohoda
        /// 2. Tvorba BarItems žere čas, slušně velké rozhraní se generuje 0.6 - 1.0 sekundy
        /// 3. Zajistíme, že BarItems se budou generovat "až to bude zapotřebí" = při prvním otevření stránky
        /// 4. Tím se dostaneme na čas vytvoření Ribbonu cca 5 milisekund (=generujeme jen Pages), 
        /// 5.  a pak potřebujeme čas prvního otevření Page cca 80 miliskund (generujeme jen potřebné Groups a BarItems), to uživatel akceptuje
        /// 6. Aby fungovalo generování BarItems "OnDemand", máme někde (!) uložené podklady pro tvorbu BarItems (někde = v instanci <see cref="DxRibbonLazyLoadInfo"/>)
        /// <para/>
        ///  Mergování do ale toho hodilo widle:
        /// 1. Když mám "Primární Ribbon A", jehož stránky dosud nebyly zobrazeny, tak dosud neexistují reálné BarItems pro naše stránky. 
        ///    Existují odpovídající Pages, ale jsou prázdné. Existuje seznam podkladů pro tvorbu BarItems pro každou stránku.
        /// 2. Pak najdu "Parent Ribbon B", do kterého chci mergovat náš Ribbon "A", a pustím Merge()
        /// 3. Do "Parent Ribbonu B" se vepíšou stránky z "Primárního Ribbon A", ale:
        ///     - nedostávají se tam živé reference na stránky "Ribbonu A", ale vytvoří se new instance pro MergedPages
        ///     - pokud "Parent Ribbon B" obsahuje svoje vlastní stránky shodného textu jako má "Primární Ribbon A", pak se do Parent Ribbonu ani nevytváří Mergované stránky,
        ///        ale do jeho nativních stránek (Parent.Pages) se přidají grupy a BarItemy z Primární stránky (se shodným textem titulku).
        /// <para/>
        ///  Řešení:
        /// 1. Podklady pro tvorbu BarItems (typ <see cref="IRibbonItem"/>) ukládám do instance <see cref="DxRibbonLazyLoadInfo"/> (spolu s dalšími referencemi);
        /// 2. Primární stránka je typu <see cref="DxRibbonPage"/> a tyto podklady má ve své property <see cref="DxRibbonPage.LazyLoadInfo"/>;
        /// 3. Tyto podklady "uložím" navíc do speciální Grupy v primárním Ribbonu v odpovídající stránce, do Group.Tag
        /// 4. Při aktivaci stránky Ribbonu jsou dvě možnosti:
        ///  a) Jde o stránku primárního Ribbonu, tedy jde o stránku typu <see cref="DxRibbonPage"/>: 
        ///     - pak do ní vygenerují BarItemy vcelku jednoduše
        ///  b) Jde o stránku jiného Ribbonu, kde jsou mergována data z jiných Ribbonů, a je jedno zda jde o primární stránku jiného (Parent) ribbonu,
        ///      anebo jde o stránky MergedPages v Parent Ribbonu: 
        ///     - pak musím vygenerovat BarItemy do jejich primární stránky a tu pak znovu mergovat (netestováno ???)
        /// </summary>
        /// <param name="page"></param>
        /// <param name="lazyDxPages"></param>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        /// <returns></returns>
        internal static bool TryGetLazyDxPages(DevExpress.XtraBars.Ribbon.RibbonPage page, bool isCalledFromReFill, out List<DxRibbonPage> lazyDxPages)
        {
            lazyDxPages = null;
            if (page == null) return false;

            lazyDxPages = new List<DxRibbonPage>();
            AddLazyDxPages(page.Groups, isCalledFromReFill, lazyDxPages);
            AddLazyDxPages(page.MergedGroups, isCalledFromReFill, lazyDxPages);
            AddLazyDxPages(page, isCalledFromReFill, lazyDxPages);

            return (lazyDxPages.Count > 0);
        }
        /// <summary>
        /// V dané kolekci Groups vyhledá grupy s ID = <see cref="LazyLoadGroupId"/>, 
        /// v nich vyhledá BarItemy, jejichž Tag obsahuje instanci <see cref="DxRibbonPage"/>, a kde tato stránka má aktivní LazyContent.
        /// Tyto nalezené stránky přidává do listu <paramref name="lazyDxPages"/>.
        /// Metoda průběžně odebírá zmíněné Linky na Buttony, a odebírá i prázdné grupy.
        /// <para/>
        /// Účel: pokud na vstupu volající metody <see cref="TryGetLazyDxPages(RibbonPage, bool, out List{DxRibbonPage})"/> je stránka Ribbonu z okna Desktop (=Main okno aplikace), 
        /// tak v tomto Ribbonu jsou mergovány i Child Ribbony, které teprve obsahují stránky v režimu Lazy.
        /// Stránka sama je tedy typicky typu DevExpress, a v ní jsou Grupy a jejich prvky mergované z Child Ribbonů.
        /// Lazy stránka (v Child Ribbonu) obsahuje grupu s ID = <see cref="LazyLoadGroupId"/> (statická konstanta), a v této grupě je prvek obsahující ve svém Tagu referenci na zdrojovou stránku Child Ribbonu typu DxRibbonPage.
        /// Jedině touto cestou se může domergovat odkaz za zdrojopvou stránku z Childu do Desktopu.
        /// Touto cestou tedy najdeme a určíme zdrojovou Lazy stránku a dáme ji do výstupu.
        /// <para/>
        /// Poznámka: Jedna stránka v Desktopu může teoreticky obsahovat více mergovaných Childů (hierarchicky) a tedy více zdrojových Lazy stránek, proto tady pracujeme ForEach in groups a výsledky strádáme do Listu.
        /// </summary>
        /// <param name="pageGroups"></param>
        /// <param name="isCalledFromReFill">Odkud je akce volaná: false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        /// <param name="lazyDxPages"></param>
        private static void AddLazyDxPages(DevExpress.XtraBars.Ribbon.RibbonPageGroupCollection pageGroups, bool isCalledFromReFill, List<DxRibbonPage> lazyDxPages)
        {
            string groupId = LazyLoadGroupId;
            var lazyGroups = pageGroups.Where(g => g.Name == groupId).ToArray();
            if (lazyGroups.Length == 0) return;

            foreach (var lazyGroup in lazyGroups)
            {
                var itemLinks = lazyGroup.ItemLinks.ToArray();
                foreach (DevExpress.XtraBars.BarItemLink link in itemLinks)
                {
                    bool removeLink = true;
                    if (link.Item.Tag is DxRibbonPage dxRibbonPage)
                    {
                        if (dxRibbonPage.HasActiveLazyContent)
                        {
                            lazyDxPages.Add(dxRibbonPage);
                            // Pokud nyní probíhá naplnění prvků OnDemand a tato stránka je v režimu LoadEveryTime, pak nebudeme odstraňovat link a grupu, která k tomu vede:
                            if (isCalledFromReFill && dxRibbonPage.IsLazyLoadEveryTime)
                                removeLink = false;
                        }
                    }
                    if (removeLink)
                        lazyGroup.ItemLinks.Remove(link);
                }
                if (lazyGroup.ItemLinks.Count == 0)
                    pageGroups.Remove(lazyGroup);
            }
        }
        /// <summary>
        /// Pokud pole <paramref name="lazyDxPages"/> neobsahuje danou stránku <paramref name="page"/>, a tato stránka je typu <see cref="DxRibbonPage"/> a má Lazy content, pak je do výsledného pole přidána.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="isCalledFromReFill"></param>
        /// <param name="lazyDxPages"></param>
        private static void AddLazyDxPages(DevExpress.XtraBars.Ribbon.RibbonPage page, bool isCalledFromReFill, List<DxRibbonPage> lazyDxPages)
        {
            if (page is DxRibbonPage dxRibbonPage && dxRibbonPage.HasActiveLazyContent && !lazyDxPages.Contains(dxRibbonPage))
            {
                lazyDxPages.Add(dxRibbonPage);
            }
        }
        /// <summary>
        /// Zahodí všechny svoje reference
        /// </summary>
        public void Clear()
        {
            this.IsActive = false;
            this.OwnerRibbon = null;
            this.OwnerPage = null;
            this.PageData = null;
            this.Group = null;
            this.BarItem = null;
        }
        /// <summary>
        /// true pokud this prvek má nějaký aktivní LazyLoad obsah
        /// </summary>
        public bool HasActiveLazyContent { get { return (this.IsActive && this.HasData); } }
        /// <summary>
        /// true pokud this stránka má LazyLoad obsah typu Static = má připravená data, a stačí je jen materializovat do fyzických controlů
        /// </summary>
        internal bool HasActiveStaticLazyContent { get { return (this.IsActive && this.HasData && this.PageContentMode == RibbonContentMode.Static && this.HasItems); } }
        /// <summary>
        /// Obsahuje true, pokud this instance obsahuje nějaká data je zpracování (má referenci na <see cref="OwnerRibbon"/> a <see cref="OwnerPage"/>)
        /// a buď je <see cref="IsOnDemand"/> anebo má data pro stránku podle <see cref="HasItems"/>.
        /// </summary>
        public bool HasData { get { return (OwnerRibbon != null && OwnerPage != null && (IsOnDemand || HasItems)); } }
        /// <summary>
        /// Příznak, že this LazyGroup je aktivní
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Obsahuje true, pokud this instance definuje režim OnDemandLoad
        /// </summary>
        public bool IsOnDemand { get { var mode = this.PageContentMode; return (mode == RibbonContentMode.OnDemandLoadOnce || mode == RibbonContentMode.OnDemandLoadEveryTime); } }
        /// <summary>
        /// Obsahuje true, pokud this instance obsahuje definici stránky <see cref="PageData"/>, v níž jsou nějaké prvky k zobrazení.
        /// </summary>
        public bool HasItems { get { return (this.PageData != null && this.PageData.Groups != null && this.PageData.Groups.Any(g => g.Items != null && g.Items.Any())); } }
        /// <summary>
        /// Režim práce stránky
        /// </summary>
        public RibbonContentMode PageContentMode { get; set; }
        /// <summary>
        /// Odkaz na Ribbon
        /// </summary>
        public DxRibbonControl OwnerRibbon { get; private set; }
        /// <summary>
        /// GUI instance stránky
        /// </summary>
        public DxRibbonPage OwnerPage { get; private set; }
        /// <summary>
        /// Definiční data stránky
        /// </summary>
        public IRibbonPage PageData { get; private set; }
        /// <summary>
        /// Toto je GUI prvek, v jehož GroupItems je zahrnut zdejší <see cref="BarItem"/>
        /// </summary>
        public DxRibbonGroup Group { get; private set; }
        /// <summary>
        /// Toto je GUI prvek, v jehož Tagu je reference na this instanci
        /// </summary>
        public DevExpress.XtraBars.BarButtonItem BarItem { get; private set; }
    }
    #endregion
    #region DxBarCheckBoxToggle : Button reprezentující hodnotu "Checked" { NULL - false - true } s využitím tří ikonek 
    /// <summary>
    /// Button reprezentující hodnotu <see cref="Checked"/> { NULL - false - true } s využitím tří ikonek 
    /// </summary>
    public class DxBarCheckBoxToggle : DevExpress.XtraBars.BarButtonItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBarCheckBoxToggle() : base() { Initialize(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        public DxBarCheckBoxToggle(DevExpress.XtraBars.BarManager manager, string caption) : base(manager, caption) { Initialize(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        /// <param name="imageIndex"></param>
        public DxBarCheckBoxToggle(DevExpress.XtraBars.BarManager manager, string caption, int imageIndex) : base(manager, caption, imageIndex) { Initialize(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        /// <param name="imageIndex"></param>
        /// <param name="shortcut"></param>
        public DxBarCheckBoxToggle(DevExpress.XtraBars.BarManager manager, string caption, int imageIndex, DevExpress.XtraBars.BarShortcut shortcut) : base(manager, caption, imageIndex, shortcut) { Initialize(); }
        /// <summary>
        /// Inicializace buttonu
        /// </summary>
        protected void Initialize()
        {
            _ImageNameNull = ImageName.DxBarCheckToggleNull;
            _ImageNameUnChecked = ImageName.DxBarCheckToggleFalse;
            _ImageNameChecked = ImageName.DxBarCheckToggleTrue;
            _Checked = null;
            ApplyImage();
        }
        /// <summary>
        /// Po kliknutí na tlačítko
        /// </summary>
        /// <param name="link"></param>
        protected override void OnClick(DevExpress.XtraBars.BarItemLink link)
        {
            var value = this.Checked;
            this.Checked = (!value.HasValue ? false : !value.Value);           // Změní se hodnota Checked => vyvolá se OnCheckedChanged()
            base.OnClick(link);
        }
        /// <summary>
        /// Po změně hodnoty <see cref="Checked"/>
        /// </summary>
        protected virtual void OnCheckedChanged()
        {
            ApplyImage();
            DxRibbonControl.DoItemCheckedChange(this, this.Checked);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost po změně hodnoty <see cref="Checked"/>
        /// </summary>
        public event EventHandler CheckedChanged;
        /// <summary>
        /// Aplikuje do sebe aktuální obrázek
        /// </summary>
        protected void ApplyImage()
        {
            DxComponent.ApplyImage(this.ImageOptions, imageName: ImageNameCurrent);
        }
        /// <summary>
        /// Aktuálně platný obrázek podle hodnoty <see cref="Checked"/>
        /// </summary>
        protected string ImageNameCurrent 
        {
            get { var value = this.Checked; return (value.HasValue ? (value.Value ? ImageNameChecked : ImageNameUnChecked) : ImageNameNull); } 
        }
        /// <summary>
        /// Hodnota označení / neoznačení / NULL
        /// </summary>
        public bool? Checked 
        { 
            get { return _Checked; } 
            set 
            {
                if (value != _Checked)
                {
                    _Checked = value; 
                    OnCheckedChanged();
                }
            }
        }
        /// <summary>
        /// Hodnota označení / neoznačení / NULL
        /// </summary>
        public bool? CheckedSilent
        {
            get { return _Checked; }
            set
            {
                if (value != _Checked)
                    _Checked = value;
            }
        }
        private bool? _Checked;
        /// <summary>
        /// Jméno ikony za stavu <see cref="Checked"/> = NULL
        /// </summary>
        public string ImageNameNull
        {
            get { return _ImageNameNull; }
            set
            {
                if (!String.Equals(_ImageNameNull, value, StringComparison.Ordinal))
                {
                    _ImageNameNull = value;
                    ApplyImage();
                }
            }
        }
        private string _ImageNameNull;
        /// <summary>
        /// Jméno ikony za stavu <see cref="Checked"/> = false
        /// </summary>
        public string ImageNameUnChecked 
        { 
            get { return _ImageNameUnChecked; } 
            set 
            {
                if (!String.Equals(_ImageNameUnChecked, value, StringComparison.Ordinal))
                {
                    _ImageNameUnChecked = value;
                    ApplyImage();
                }
            }
        }
        private string _ImageNameUnChecked;
        /// <summary>
        /// Jméno ikony za stavu <see cref="Checked"/> = true
        /// </summary>
        public string ImageNameChecked
        { 
            get { return _ImageNameChecked; } 
            set 
            {
                if (!String.Equals(_ImageNameChecked, value, StringComparison.Ordinal))
                {
                    _ImageNameChecked = value;
                    ApplyImage();
                }
            } 
        }
        private string _ImageNameChecked;
    }
    #endregion
    #region DxZoomMenuBarSubItem : BarItem typu Menu pro zobrazení Zoomu
    /// <summary>
    /// <see cref="DxZoomMenuBarSubItem"/> : BarItem typu Menu pro zobrazení Zoomu
    /// </summary>
    public class DxZoomMenuBarSubItem : BarSubItem, IListenerZoomChange
    {
        /// <summary>
        /// Konstruktor, defaultně připraví prvek
        /// </summary>
        public DxZoomMenuBarSubItem()
        {
            IRibbonItem ribbonItem = null;
            this._Initialize(ref ribbonItem);
        }
        /// <summary>
        /// Konstruktor, plně připraví prvek
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barManager"></param>
        public DxZoomMenuBarSubItem(ref IRibbonItem ribbonItem, DevExpress.XtraBars.BarManager barManager)
            : base(barManager, "")
        {
            this._Initialize(ref ribbonItem);
        }
        /// <summary>
        /// Připraví this prvek a nadefinuje odpovídající položky menu
        /// </summary>
        private void _Initialize(ref IRibbonItem ribbonItem)
        {
            var dataRibbonItem = (ribbonItem is null ? new DataRibbonItem() : DataRibbonItem.CreateClone(ribbonItem));
            dataRibbonItem.ItemType = RibbonItemType.ZoomPresetMenu;
            dataRibbonItem.ImageName = (ribbonItem.ImageName is null ? ZoomImageName : ribbonItem.ImageName);              // Defaultní ikona namísto NULL, jinak ponechám (prázdný string = bez ikony)
            dataRibbonItem.SubItems = _CreateZoomMenuItems(ribbonItem);
            dataRibbonItem.Text = _CurrentZoomText;
            DxComponent.FillBarItemFrom(this, dataRibbonItem, 0);    // Do this instance (potomek prvku Ribbonu BarSubItem) vepíše definiční data z dataRibbonItem
            DxComponent.RegisterListener(this);                      // Já jsem Listener změny IListenerZoomChange
            ribbonItem = dataRibbonItem;
        }
        /// <summary>
        /// Dispose provede odregistrování listeneru
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnregisterListener(this);
            base.Dispose(disposing);
        }
        /// <summary>
        /// Vytvoří položky do nabídky Zoom Menu, obsahující jednotlivé doporučené Zoom hodnoty.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        private ListExt<IRibbonItem> _CreateZoomMenuItems(IRibbonItem ribbonItem)
        {
            // Položky v nabídce Zoomu:
            var zoomValueItems = new List<Tuple<int, DataRibbonItem>>();

            // Vyhledám zadané subItemy (nejspíš tam nebudou, ale co kdyby je někdo externě nadeklaroval?)
            var iSubItems = ribbonItem?.SubItems;
            if (iSubItems != null)
            {   // Ale prvky musí mít v Tag hodnotu typu Int, která reprezentuje Zoom:
                var validSubItems = iSubItems.Where(i => i.Tag is int).ToArray();
                if (validSubItems.Length > 0)
                    zoomValueItems.AddRange(validSubItems.Select(i => new Tuple<int, DataRibbonItem>((int)i.Tag, DataRibbonItem.CreateClone(i))));
            }

            // Nemám dodané explicitní validní prvky (stačil by nám jeden jediný) => vytvořím je sám:
            if (zoomValueItems.Count == 0)
            {   // Hodnoty Zoomu mohou být dodány v Tagu
                var zoomValues = getZoomValues(ribbonItem?.Tag);
                foreach (var zoomValue in zoomValues)
                    zoomValueItems.Add(createSubItem(zoomValue));
            }

            // Zajistím, že v nabídce bude i aktuální hodnota Zoomu:
            int currentZoom = _CurrentZoomPct;
            if (!zoomValueItems.Any(i => i.Item1 == currentZoom))
                zoomValueItems.Add(createSubItem(currentZoom));

            // SubItemy budou napojeny na zdejší Click eventhandler:
            zoomValueItems.ForEach(t => t.Item2.ClickAction = _ZoomSubItemClick);

            // Setřídím podle hodnoty Zoomu:
            zoomValueItems.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return new ListExt<IRibbonItem>(zoomValueItems.Select(t => t.Item2));


            // Vrátí pole Int32, reprezentující jednotlivé hodnoty Zoomu. Mohou být dodány jako pole Int v dodaném tagu, nebo jako string s čísly oddělenými čárkami (atd). Default = _ZoomValues
            int[] getZoomValues(object tag)
            {
                int[] result = null;
                if (tag is int[] array)
                    result = array;
                else if (tag is string text && text != null && text.Length > 0)
                    result = text.Split(" ,;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                 .Where(i => Int32.TryParse(i, out var _))
                                 .Select(i => Int32.Parse(i))
                                 .ToArray();
                
                if (result != null)
                    result = result.Where(r => _IsValidZoom(r)).ToArray();

                if (result is null || result.Length == 0)
                    result = _ZoomValues;

                return result;
            }
            // Vytvoří a vrátí standardní pár obsahující Zoom a SubItem pro daný Zoom
            Tuple<int, DataRibbonItem> createSubItem(int zoomVal)
            {
                var zoomSubItem = new DataRibbonItem()
                {
                    Text = $"{zoomVal}%",
                    ItemId = $"{_SubItemName_ZoomValue}{zoomVal}",
                    Tag = zoomVal,
                    RibbonStyle = RibbonItemStyles.SmallWithText,
                    FontSizeRelativeToDesign = ((float)zoomVal) / 100f
                };
                return new Tuple<int, DataRibbonItem>(zoomVal, zoomSubItem);
            }
        }
        /// <summary>
        /// Po kliknutí na konkrétní SubItem Zoomu
        /// </summary>
        /// <param name="menuItem"></param>
        private void _ZoomSubItemClick(IMenuItem menuItem)
        {
            if (menuItem != null && menuItem.Tag is int)
            {
                int zoom = _AlignZoomValue((int)menuItem.Tag);
                _CurrentZoomPct = zoom;                              // Změnu Zoomu provádí systém, po změně Zoomu systém vyvolá IListenerZoomChange.ZoomChanged()
            }
        }
        /// <summary>
        /// Refreshuje své menu pro aktuální Zoom
        /// </summary>
        public void RefreshMenuForCurrentZoom()
        {
            // Aktuální zoom:
            int zoom = _CurrentZoomPct;
            string text = _CurrentZoomText;

            // this prvek:
            this.Caption = text;
            string toolTipTitle = DxComponent.Localize(MsgCode.RibbonZoomMenuTitle, text);
            string toolTipText = DxComponent.Localize(MsgCode.RibbonZoomMenuText);
            this.SuperTip = DxComponent.CreateDxSuperTip(toolTipTitle, toolTipText);
            if (DxRibbonControl.TryGetRibbonItem(this, out var iMenuData))
            {
                if (iMenuData is DataRibbonItem menuData)
                {
                    menuData.Text = text;
                    menuData.ToolTipTitle = toolTipTitle;
                    menuData.ToolTipText = toolTipText;
                }
            }

            // Jednotlivé prvky menu:
            var subItems = this.ItemLinks;
            if (subItems != null)
            {
                foreach (BarItemLink subItemLink in subItems)
                {
                    var subItem = subItemLink.Item;
                    if (DxRibbonControl.TryGetRibbonItem(subItem, out var iSubMenuData) && iSubMenuData.Tag is int)
                    {
                        if (iSubMenuData is DataRibbonItem subMenuData)
                        {
                            var subItemZoom = (int)(subMenuData.Tag);
                            bool isActive = (subItemZoom == zoom);
                            subMenuData.FontStyle = (isActive ? FontStyle.Bold : FontStyle.Regular);
                            subMenuData.ImageName = (isActive ? DxZoomMenuBarSubItem.ZoomImageName : null);
                            DxComponent.FillBarItemFrom(subItem, subMenuData, 1);
                        }
                    }
                }
            } 
        }
        /// <summary>
        /// Došlo ke změně Zoomu v systému
        /// </summary>
        void IListenerZoomChange.ZoomChanged()
        {
            RefreshMenuForCurrentZoom();
        }
        /// <summary>
        /// Jméno prvku SubItem pro nabídku Zoomu
        /// </summary>
        private const string _SubItemName_ZoomValue = "_SYS__DevExpress_ZoomMenuValue_";
        /// <summary>
        /// Hodnoty Zoomu v nabídce v Ribbonu
        /// </summary>
        private static int[] _ZoomValues { get { return new int[] { 50, 60, 75, 80, 90, 100, 105, 110, 125, 150, 175, 200 }; } }
        /// <summary>
        /// Vrátí danou hodnotu Zoomu zarovnanou do validních mezí
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private static int _AlignZoomValue(int zoom) { return (zoom < 50 ? 50 : (zoom > 250 ? 250 : zoom)); }
        /// <summary>
        /// Vrátí true pro validní hodnotu zoomu
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private static bool _IsValidZoom(int zoom) { return (zoom >= 50 && zoom <= 250); }
        /// <summary>
        /// Aktuální systémový zoom jako Int32 v procentech
        /// </summary>
        private static int _CurrentZoomPct
        {
            get { return (int)(100m * DxComponent.Zoom); }
            set { DxComponent.Zoom = (decimal)value / 100m; }
        }
        /// <summary>
        /// Aktuální systémový zoom jako text "125%"
        /// </summary>
        private static string _CurrentZoomText { get { return $"{_CurrentZoomPct}%"; } }
        /// <summary>
        /// Event vyvolaný tehdy, když uživatel změní Zoom
        /// </summary>
        public event EventHandler ZoomChanged;
        /// <summary>
        /// ImageName pro ikonu Zoomu
        /// </summary>
        public static string ZoomImageName { get { return "svgimages/pdf%20viewer/marqueezoom.svg"; } }
    }
    #endregion
    #region TrackBar - TODO

    //#warning TrackBar - TODO

    //[DevExpress.XtraEditors.Registrator.UserRepositoryItem("RegisterMyTrackBar")]
    //public class RepositoryItemMyTrackBar : DevExpress.XtraEditors.Repository.RepositoryItemTrackBar
    //{
    //    static RepositoryItemMyTrackBar()
    //    {
    //        RegisterMyTrackBar();
    //    }
    //    public static void RegisterMyTrackBar()
    //    {
    //        Image img = null;
    //        DevExpress.XtraEditors.Registrator.EditorRegistrationInfo.Default.Editors.Add(new DevExpress.XtraEditors.Registrator.EditorClassInfo(CustomEditName, typeof(MyTrackBar), typeof(RepositoryItemMyTrackBar), typeof(MyTrackBarViewInfo), new MyTrackBarPainter(), true, img));
    //    }


    //    protected override int ConvertValue(object val)
    //    {
    //        return base.ConvertValue(val);
    //    }

    //    public const string CustomEditName = "MyTrackBar";

    //    public RepositoryItemMyTrackBar() { }



    //    //---

    //    public static RepositoryItemMyTrackBar SetupTrackBarDouble(double paramValue)
    //    {

    //        int paramValueInt = Convert.ToInt32(paramValue * 100);

    //        RepositoryItemMyTrackBar trackbar = new RepositoryItemMyTrackBar()
    //        {
    //            Minimum = paramValueInt - 500,
    //            Maximum = paramValueInt + 500,
    //            SmallChange = 5,
    //            ShowLabels = true
    //        };

    //        trackbar.Labels.Add(new DevExpress.XtraEditors.Repository.TrackBarLabel((Convert.ToDouble(trackbar.Minimum / 100d)).ToString(),
    //          trackbar.Minimum));
    //        trackbar.Labels.Add(new DevExpress.XtraEditors.Repository.TrackBarLabel((Convert.ToDouble(trackbar.Maximum / 100d)).ToString(),
    //          trackbar.Maximum));
    //        trackbar.Labels.Add(new DevExpress.XtraEditors.Repository.TrackBarLabel(paramValue.ToString(), paramValueInt));

    //        return trackbar;
    //    }

    //    //---




    //    public override string EditorTypeName { get { return CustomEditName; } }



    //    public override void Assign(DevExpress.XtraEditors.Repository.RepositoryItem item)
    //    {
    //        BeginUpdate();
    //        try
    //        {
    //            base.Assign(item);
    //            RepositoryItemMyTrackBar source = item as RepositoryItemMyTrackBar;
    //            if (source == null) return;
    //            //
    //        }
    //        finally
    //        {
    //            EndUpdate();
    //        }
    //    }
    //}

    //[System.ComponentModel.ToolboxItem(true)]
    //public class MyTrackBar : DevExpress.XtraEditors.TrackBarControl
    //{
    //    static MyTrackBar()
    //    {
    //        RepositoryItemMyTrackBar.RegisterMyTrackBar();
    //    }

    //    public MyTrackBar()
    //    {
    //    }

    //    public override object EditValue { get { return base.EditValue; } set { base.EditValue = value; } }

    //    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
    //    public new RepositoryItemMyTrackBar Properties { get { return base.Properties as RepositoryItemMyTrackBar; } }

    //    protected override object ConvertCheckValue(object val)
    //    {
    //        return base.ConvertCheckValue(val);
    //    }

    //    public override string EditorTypeName { get { return RepositoryItemMyTrackBar.CustomEditName; } }
    //}

    //public class MyTrackBarViewInfo : DevExpress.XtraEditors.ViewInfo.TrackBarViewInfo
    //{
    //    public MyTrackBarViewInfo(DevExpress.XtraEditors.Repository.RepositoryItem item)
    //        : base(item)
    //    {
    //    }



    //    public override object EditValue
    //    {
    //        get
    //        {
    //            return base.EditValue;
    //        }
    //        set
    //        {
    //            try
    //            {
    //                if (value is float)
    //                {
    //                    int result = 0;
    //                    result = (int)(Convert.ToSingle(value) * 100);
    //                    base.EditValue = result;
    //                }
    //                else
    //                    base.EditValue = value;
    //            }
    //            catch { }
    //        }
    //    }

    //    public override DevExpress.XtraEditors.Drawing.TrackBarObjectPainter GetTrackPainter()
    //    {
    //        return new SkinMyTrackBarObjectPainter(LookAndFeel);
    //    }
    //}

    //public class MyTrackBarPainter : DevExpress.XtraEditors.Drawing.TrackBarPainter
    //{
    //    public MyTrackBarPainter()
    //    {
    //    }
    //}

    //public class SkinMyTrackBarObjectPainter : DevExpress.XtraEditors.Drawing.SkinTrackBarObjectPainter
    //{
    //    public SkinMyTrackBarObjectPainter(DevExpress.Skins.ISkinProvider provider)
    //        : base(provider)
    //    {
    //    }
    //}
    #endregion
    #region DxRibbonComboBox
    /// <summary>
    /// Prvek Ribbonu reprezentující ComboBox
    /// </summary>
    public class DxRibbonComboBox : BarEditItem, IReloadable
    {
        #region Public
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        public DxRibbonComboBox(BarManager manager) : base(manager)
        {
            this._Init();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonComboBox() : base()
        {
            this._Init();
        }
        /// <summary>
        /// Událost volaná po kliknutí na prvku v ComboBoxu. Tato událost se volá i po opakované aktivaci stejného prvku. 
        /// V takové situaci se nevolá <see cref="SelectedDxItemChanged"/>.
        /// </summary>
        public event EventHandler<DxRibbonItemClickArgs> SelectedDxItemActivated;
        /// <summary>
        /// Metoda volaná po kliknutí na prvku v ComboBoxu. Tato událost se volá i po opakované aktivaci stejného prvku. 
        /// V takové situaci se nevolá <see cref="SelectedDxItemChanged"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSelectedDxItemActivated(DxRibbonItemClickArgs args) { }
        /// <summary>
        /// Událost volaná po změně aktivního prvku v ComboBoxu
        /// </summary>
        public event EventHandler<DxRibbonItemClickArgs> SelectedDxItemChanged;
        /// <summary>
        /// Metoda volaná po změně aktivního prvku v ComboBoxu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSelectedDxItemChanged(DxRibbonItemClickArgs args) { }
        /// <summary>
        /// Událost volaná po kliknutí na button v ComboBoxu
        /// </summary>
        public event EventHandler<DxRibbonItemButtonClickArgs> ComboButtonClick;
        /// <summary>
        /// Metoda volaná po kliknutí na button v ComboBoxu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnComboButtonClick(DxRibbonItemButtonClickArgs args) { }
        /// <summary>
        /// Aktuálně vybraný prvek v ComboBoxu
        /// </summary>
        public IRibbonItem SelectedDxItem
        {
            get
            {
                var selectedItem = this.EditValue;
                if (selectedItem is IRibbonItem iRibbonItem) return iRibbonItem;
                return null;
            }
            set
            {
                object selectedItem = null;
                if (value != null && this.__ComboBox != null && this.__ComboBox.Items.Count > 0)
                    selectedItem = this.__ComboBox.Items.OfType<IRibbonItem>().FirstOrDefault(i => Object.ReferenceEquals(i, value));
                this.EditValue = selectedItem;
            }
        }
        /// <summary>
        /// Data, která deklarují obsah tohoto prvku.
        /// Setováním prvku dojde k naplnění specifických položek (Items a Buttons) ComboBoxu.
        /// Přenačtení těchto dat lze vyvolat i metodou <see cref="Reload()"/>.
        /// Nicméně tímto plněním se <u>neprovádí běžné setování dat</u> (Caption, Image, SuperTip, Enabled atd), to provádí Ribbon standardně sám - tak jako u jiných BarItems.
        /// </summary>
        public IRibbonItem IRibbonItem
        {
            get { return __IRibbonItem; }
            set { this._SetIRibbonItem(value); }
        }
        private IRibbonItem __IRibbonItem;
        /// <summary>
        /// Znovu vytvoří prvky ComboBoxu : Items a Buttons.
        /// Používá se pro Refresh obsahu.
        /// Nenaplní běžné hodnoty BarItemu (Caption, Image, SuperTip, Enabled atd), to provádí Ribbon tak jako u jiných BarItems.
        /// </summary>
        public void Reload()
        {
            try
            {
                __SilentChange = true;
                _ClearItems();
                _ClearButtons(false);
                _ReloadIRibbonData();
            }
            finally
            {
                __SilentChange = false;
                _CheckChangeItem();
            }
        }
        /// <summary>
        /// Uloží do sebe daný definiční prvek a znovu vytvoří prvky ComboBoxu : Items a Buttons.
        /// Používá se pro Refresh obsahu.
        /// Nenaplní běžné hodnoty BarItemu (Caption, Image, SuperTip, Enabled atd), to provádí Ribbon tak jako u jiných BarItems.
        /// </summary>
        public void ReloadFrom(IRibbonItem iRibbonItem)
        {
            if (iRibbonItem != null)
                _SetIRibbonItem(iRibbonItem);
        }
        #endregion
        #region Privátní život - tvorba, vnitřní eventy
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _Init()
        {
            var comboBox = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            comboBox.AutoHeight = false;
            comboBox.PopupSizeable = true;
            comboBox.ReadOnly = false;
            comboBox.AllowDropDownWhenReadOnly = DefaultBoolean.True;
            comboBox.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            comboBox.DropDownRows = 12;
            comboBox.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            comboBox.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            comboBox.HighlightedItemStyle = DevExpress.XtraEditors.HighlightStyle.Skinned;
            comboBox.HotTrackItems = true;
            // comboBox.BestFitWidth = 250;         // Nefunguje

            this.CausesValidation = true;
            comboBox.QueryPopUp += ComboBox_QueryPopUp;
            comboBox.BeforePopup += ComboBox_BeforePopup;
            comboBox.BeforeShowMenu += ComboBox_BeforeShowMenu;
            comboBox.SelectedValueChanged += ComboBox_SelectedValueChanged;
            comboBox.Validating += ComboBox_Validating;
            comboBox.Popup += ComboBox_Popup;
            comboBox.Closed += ComboBox_Closed;
            comboBox.ButtonPressed += ComboBox_ButtonPressed;
            comboBox.ButtonClick += ComboBox_ButtonClick;
            comboBox.Click += ComboBox_Click;

            this.__SilentChange = false;
            this.__LastSelectedItem = null;
            this.Edit = comboBox;
            this.Width = 250;                       // Tohle jediné nastaví šířku cca přesně
            // this.Size = new Size(250, 25);       // Nefunguje
            this.__ComboBox = comboBox;
        }
        private void ComboBox_BeforeShowMenu(object sender, DevExpress.XtraEditors.Controls.BeforeShowMenuEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_BeforeShowMenu: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
        }
        private void ComboBox_BeforePopup(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_BeforePopup: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
        }
        /// <summary>
        /// Obsahuje true, když se nemá aktivovat událost o změně vybrané hodnoty
        /// </summary>
        private bool __SilentChange;
        /// <summary>
        /// Obsahuje hodnotu, která byla posledně hlášena jako <see cref="SelectedDxItem"/> v eventu <see cref="SelectedDxItemChanged"/>.
        /// </summary>
        private object __LastSelectedItem;
        private void ComboBox_Click(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_Click: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
        }
        /// <summary>
        /// Před otevřením Popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_QueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_Click: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
        }
        private void ComboBox_Popup(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_Popup: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
            __IsPopupOpen = true;
        }
        private void ComboBox_Closed(object sender, DevExpress.XtraEditors.Controls.ClosedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_Closed: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
            __IsPopupOpen = false;
        }
        private bool __IsPopupOpen;
        private void ComboBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_Validating: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
        }
        /// <summary>
        /// Po změně vybrané hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_SelectedValueChanged: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}");
            _CheckChangeItem();
        }
        private void ComboBox_ButtonPressed(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
           DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_ButtonPressed: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}; Button={e.Button.Kind}");

            // Při stisknutí buttonu DropDown v situaci, kdy DropDown button není první button vpravo přímo za textem v ComboBoxu, 
            //  se v DevExpress nativně neotevírá Popup. Nevím proč.
            if (e.Button.Kind == DevExpress.XtraEditors.Controls.ButtonPredefines.DropDown && !__IsPopupOpen && this.Enabled && this.__ComboBox.Items.Count > 0)
            {   // Takže pokud jde o button DropDown a není otevřen Popup, a přitom by být mohl:
                //  pak získám živý editor ComboBoxEdit a otevřu jeho Popup:
                if (this.GetActiveEditor() is DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit)
                    comboBoxEdit.ShowPopup();
            }
        }
        private void ComboBox_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.Ribbon, $"DxRibbonComboBox.ComboBox_ButtonClick: IsPopupOpen={__IsPopupOpen}; SelectedItem={this.SelectedDxItem}; Button={e.Button.Kind}");
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.DropDown)
                this._RunComboButtonClick(e.Button);
        }
        /// <summary>
        /// Repository Item ComboBox
        /// </summary>
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox __ComboBox;
        /// <summary>
        /// Naplní prvek daty z dodaného <see cref="IRibbonItem"/>.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        private void _SetIRibbonItem(IRibbonItem iRibbonItem)
        {
            try
            {
                __SilentChange = true;
                _Reset();
                __IRibbonItem = iRibbonItem;
                if (iRibbonItem != null)
                {
                    iRibbonItem.RibbonItem = this;
                    _ReloadIRibbonData();
                }
            }
            finally
            {
                __SilentChange = false;
                _CheckChangeItem();
            }
        }
        /// <summary>
        /// Resetuje svůj obsah (Items a Buttons), ponechá buttonDropDown, a odpojí se z aktuálního prvku <see cref="IRibbonItem"/>
        /// </summary>
        private void _Reset()
        {
            _ClearItems();
            _ClearButtons(false);

            var iRibbonItem = __IRibbonItem;
            if (iRibbonItem != null)
            {
                iRibbonItem.RibbonItem = null;
                __IRibbonItem = null;
            }
        }
        /// <summary>
        /// Z prvku odebere Items
        /// </summary>
        private void _ClearItems()
        {
            // Items:
            var comboBox = __ComboBox;
            comboBox.Items.Clear();
        }
        /// <summary>
        /// Z prvku odebere Buttons, volitelně ponechá nebo přidá button DropDown
        /// </summary>
        /// <param name="clearAll">Smazat vše = smazat i DropDown</param>
        private void _ClearButtons(bool clearAll = false)
        {
            // Buttons: ponechat jen DropDown, anebo pokud by tam nebyl, tak jej přidat:
            var comboBox = __ComboBox;
            if (clearAll)
            {
                if (comboBox.Buttons.Count > 0)
                    comboBox.Buttons.Clear();
            }
            else
            {
                comboBox.Buttons.RemoveWhere(b => b.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.DropDown);
                if (comboBox.Buttons.Count == 0)
                    comboBox.Buttons.Add(new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.DropDown));
            }
        }
        /// <summary>
        /// Z aktuálně přítomného prvku <see cref="__IRibbonItem"/> z jeho SubItems do this combo naplní Items a Buttons, a nastaví Value podle Checked prvku
        /// </summary>
        private void _ReloadIRibbonData()
        {
            var comboBox = __ComboBox;
            IRibbonItem iRibbonItem = __IRibbonItem;
            if (iRibbonItem != null)
            {
                // Items:
                if (iRibbonItem.SubItems != null)
                {
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(iRibbonItem.SubItems.ToArray());

                    // SelectedItem:
                    if (iRibbonItem.SubItems.TryGetFirst(i => i.Checked.HasValue && i.Checked.Value, out var checkedItem))
                        this.EditValue = checkedItem;
                }

                if (iRibbonItem is IRibbonComboItem iRibbonCombo)
                {   // Buttons a další vlastnosti:
                    _ClearButtons(true);
                    var subButtons = iRibbonCombo.SubButtons;
                    if (subButtons is null) subButtons = "DropDown";
                    if (subButtons.Length > 0)
                    {
                        var buttons = DataSubButton.Deserialize(subButtons);
                        if (buttons != null)
                        {
                            foreach (var button in buttons)
                                addComboButton(comboBox, button);
                        }
                    }

                    // Border:
                    applyComboBorderStyle(iRibbonCombo.ComboBorderStyle);
                    applyButtonsBorderStyle(iRibbonCombo.SubButtonsBorderStyle);
              
                    // Width:
                    if (iRibbonCombo.Width.HasValue && iRibbonCombo.Width.Value > 0)
                        this.Width = iRibbonCombo.Width.Value;       // Tohle jediné nastaví šířku cca přesně

                    // Detaily:
                    comboBox.NullValuePrompt = iRibbonCombo.NullValuePrompt;
                    comboBox.ShowNullValuePrompt = DevExpress.XtraEditors.ShowNullValuePromptOptions.EmptyValue;

                    comboBox.ShowDropDown = DevExpress.XtraEditors.Controls.ShowDropDown.SingleClick;
                }
            }

            // Přidá daný button. Na pořadí záleží.
            void addComboButton(DevExpress.XtraEditors.Repository.RepositoryItemComboBox combo, DataSubButton button)
            {
                if (button is null) return;

                // Předdefinovaný button, nebo uživatelský obrázek?
                var image = button.ImageName;
                bool hasImage = !String.IsNullOrEmpty(image);
                var kind = (hasImage ? DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph : _ConvertButtonType(button.ButtonType));

                var dxButton = new DevExpress.XtraEditors.Controls.EditorButton();
                dxButton.Tag = button;
                dxButton.Kind = kind;
                dxButton.Caption = "";
                dxButton.IsLeft = button.IsLeft;
                dxButton.Enabled = button.Enabled;
                dxButton.SuperTip = DxComponent.CreateDxSuperTip(button.ToolTipTitle, button.ToolTipText);
                dxButton.Shortcut = (button.Shortcut.HasValue ? new KeyShortcut(button.Shortcut.Value) : null);

                if (hasImage)
                    DxComponent.ApplyImage(dxButton.ImageOptions, image, sizeType: ResourceImageSizeType.Small);

                comboBox.Buttons.Add(dxButton);
            }

            // Aplikuje styl borderu na ComboBox
            void applyComboBorderStyle(DxBorderStyle border)
            {
                switch (border)
                {
                    case DxBorderStyle.None:
                        comboBox.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                        comboBox.HighlightedItemStyle = DevExpress.XtraEditors.HighlightStyle.Skinned;
                        comboBox.HotTrackItems = true;
                        break;
                    case DxBorderStyle.HotFlat:
                        comboBox.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
                        comboBox.HighlightedItemStyle = DevExpress.XtraEditors.HighlightStyle.Skinned;
                        comboBox.HotTrackItems = true;
                        break;
                    case DxBorderStyle.Single:
                        comboBox.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
                        comboBox.HighlightedItemStyle = DevExpress.XtraEditors.HighlightStyle.Skinned;
                        comboBox.HotTrackItems = true;
                        break;
                    case DxBorderStyle.Style3D:
                        comboBox.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
                        comboBox.HighlightedItemStyle = DevExpress.XtraEditors.HighlightStyle.Skinned;
                        comboBox.HotTrackItems = true;
                        break;
                }
            }
            
            // Aplikuje styl borderu na ButtonStyle
            void applyButtonsBorderStyle(DxBorderStyle border)
            {
                switch (border)
                {
                    case DxBorderStyle.None:
                        comboBox.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                        break;
                    case DxBorderStyle.HotFlat:
                        comboBox.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
                        break;
                    case DxBorderStyle.Single:
                        comboBox.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
                        break;
                    case DxBorderStyle.Style3D:
                        comboBox.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
                        break;
                }
            }
        }
        /// <summary>
        /// Konvertuje typ buttonu <see cref="DevExpress.XtraEditors.Controls.ButtonPredefines"/> na typ <see cref="PredefinedButtonType"/>
        /// </summary>
        /// <param name="buttonKind"></param>
        /// <returns></returns>
        private static PredefinedButtonType _ConvertButtonType(DevExpress.XtraEditors.Controls.ButtonPredefines buttonKind)
        {
            switch (buttonKind)
            {
                case DevExpress.XtraEditors.Controls.ButtonPredefines.DropDown: return PredefinedButtonType.DropDown;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis: return PredefinedButtonType.Ellipsis;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Delete: return PredefinedButtonType.Delete;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Close: return PredefinedButtonType.Close;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Right: return PredefinedButtonType.Right;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Left: return PredefinedButtonType.Left;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Up: return PredefinedButtonType.Up;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Down: return PredefinedButtonType.Down;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.OK: return PredefinedButtonType.OK;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Plus: return PredefinedButtonType.Plus;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Minus: return PredefinedButtonType.Minus;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Undo: return PredefinedButtonType.Undo;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Redo: return PredefinedButtonType.Redo;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Search: return PredefinedButtonType.Search;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Clear: return PredefinedButtonType.Clear;
                case DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph: return PredefinedButtonType.Glyph;
            }
            return PredefinedButtonType.None;
        }
        /// <summary>
        /// Konvertuje typ buttonu <see cref="PredefinedButtonType"/> na typ <see cref="DevExpress.XtraEditors.Controls.ButtonPredefines"/>
        /// </summary>
        /// <param name="buttonType"></param>
        /// <returns></returns>
        private static DevExpress.XtraEditors.Controls.ButtonPredefines _ConvertButtonType(PredefinedButtonType buttonType)
        {
            switch (buttonType)
            {
                case PredefinedButtonType.DropDown: return DevExpress.XtraEditors.Controls.ButtonPredefines.DropDown;
                case PredefinedButtonType.Ellipsis: return DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis;
                case PredefinedButtonType.Delete: return DevExpress.XtraEditors.Controls.ButtonPredefines.Delete;
                case PredefinedButtonType.Close: return DevExpress.XtraEditors.Controls.ButtonPredefines.Close;
                case PredefinedButtonType.Right: return DevExpress.XtraEditors.Controls.ButtonPredefines.Right;
                case PredefinedButtonType.Left: return DevExpress.XtraEditors.Controls.ButtonPredefines.Left;
                case PredefinedButtonType.Up: return DevExpress.XtraEditors.Controls.ButtonPredefines.Up;
                case PredefinedButtonType.Down: return DevExpress.XtraEditors.Controls.ButtonPredefines.Down;
                case PredefinedButtonType.OK: return DevExpress.XtraEditors.Controls.ButtonPredefines.OK;
                case PredefinedButtonType.Plus: return DevExpress.XtraEditors.Controls.ButtonPredefines.Plus;
                case PredefinedButtonType.Minus: return DevExpress.XtraEditors.Controls.ButtonPredefines.Minus;
                case PredefinedButtonType.Undo: return DevExpress.XtraEditors.Controls.ButtonPredefines.Undo;
                case PredefinedButtonType.Redo: return DevExpress.XtraEditors.Controls.ButtonPredefines.Redo;
                case PredefinedButtonType.Search: return DevExpress.XtraEditors.Controls.ButtonPredefines.Search;
                case PredefinedButtonType.Clear: return DevExpress.XtraEditors.Controls.ButtonPredefines.Clear;
            }
            return DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
        }
        /// <summary>
        /// Metoda zajistí vyvolání metody <see cref="OnComboButtonClick(DxRibbonItemButtonClickArgs)"/> a eventu <see cref="ComboButtonClick"/> pro daný typ Buttonu.
        /// </summary>
        /// <param name="button"></param>
        private void _RunComboButtonClick(DevExpress.XtraEditors.Controls.EditorButton button)
        {
            var args = new DxRibbonItemButtonClickArgs(this.IRibbonItem, button);
            OnComboButtonClick(args);
            ComboButtonClick?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda zajistí vyvolání metody <see cref="OnSelectedDxItemActivated(DxRibbonItemClickArgs)"/> a eventu <see cref="SelectedDxItemActivated"/>.
        /// Tato metoda neřeší stav <see cref="__SilentChange"/>, 
        /// a danou akci volá i když aktuální vybraný prvek <see cref="SelectedDxItem"/> je stejný jako poslední aktivní <see cref="__LastSelectedItem"/> = nejde o Změnu ale o Aktivaci.
        /// </summary>
        private void _RunActivatedItem()
        {
            var currItem = this.SelectedDxItem;
            var args = new DxRibbonItemClickArgs(currItem);
            this.OnSelectedDxItemActivated(args); ;
            this.SelectedDxItemActivated?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda zajistí vyvolání metody <see cref="OnSelectedDxItemChanged(DxRibbonItemClickArgs)"/> a eventu <see cref="SelectedDxItemChanged"/>.
        /// Tato metoda neřeší stav <see cref="__SilentChange"/>, 
        /// a danou akci volá i když aktuální vybraný prvek <see cref="SelectedDxItem"/> je stejný jako poslední aktivní <see cref="__LastSelectedItem"/> = změnu musí detekovat volající.
        /// </summary>
        private void _RunChangeItem()
        {
            _RunChangeItem(this.SelectedDxItem);
        }
        /// <summary>
        /// Metoda zajistí vyvolání metody <see cref="OnSelectedDxItemChanged(DxRibbonItemClickArgs)"/> a eventu <see cref="SelectedDxItemChanged"/>.
        /// Tato metoda neřeší stav <see cref="__SilentChange"/>, 
        /// a danou akci volá i když aktuální vybraný prvek <see cref="SelectedDxItem"/> je stejný jako poslední aktivní <see cref="__LastSelectedItem"/> = změnu musí detekovat volající.
        /// </summary>
        /// <param name="selectedDxItem"></param>
        private void _RunChangeItem(IRibbonItem selectedDxItem)
        {
            this.__LastSelectedItem = selectedDxItem;
            var args = new DxRibbonItemClickArgs(selectedDxItem);

            this.OnSelectedDxItemChanged(args);
            this.SelectedDxItemChanged?.Invoke(this, args);

            this.OnSelectedDxItemActivated(args); ;
            this.SelectedDxItemActivated?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda ověří, zda aktuálně vybraná hodnota je jiná než posledně hlášená, a pokud je jiná, ohlásí to.
        /// Pokud aktuálně máme tichý režim <see cref="__SilentChange"/>, pak nedělá nic.
        /// </summary>
        private void _CheckChangeItem()
        {
            if (__SilentChange) return;

            var selectedDxItem = this.SelectedDxItem;
            var lastItem = this.__LastSelectedItem;
            if (Object.ReferenceEquals(selectedDxItem, lastItem)) return;

            _RunChangeItem(selectedDxItem);
        }
        #endregion
    }
    #endregion
    #region DxRibbonStatusBar
    /// <summary>
    /// Potomek StatusBaru
    /// </summary>
    public class DxRibbonStatusBar : DevExpress.XtraBars.Ribbon.RibbonStatusBar
    {
        #region Konstruktor
        /// <summary>
        /// Vlastník = okno nebo panel. 
        /// Slouží k invokaci GUI threadu.
        /// </summary>
        public Control OwnerControl
        {
            get { return _OwnerControl; }
            set { _OwnerControl = value; }
        }
        /// <summary>Vlastník</summary>
        private Control _OwnerControl;
        /// <summary>
        /// Parent Control / Vlastník, slouží jako Control k invokaci GUI
        /// </summary>
        protected Control ParentOwner { get { return (this.Parent ?? this.OwnerControl ?? this); } }
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
        #region Merge a UnMerge s invokací GUI
        /// <summary>
        /// Merguje do sebe dodaný child StatusBar.
        /// Před voláním této metody není nutno provádět <see cref="UnMergeDxStatusBar"/>, to si zajistí sám.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        /// <param name="childDxStatusBar"></param>
        public void MergeDxStatusBar(DxRibbonStatusBar childDxStatusBar)
        {
            if (!HasMergedChildStatusBar && childDxStatusBar is null) return;
            this.ParentOwner.RunInGui(() => _MergeDxStatusBar(childDxStatusBar));
        }
        /// <summary>
        /// Merguje do sebe dodaný child StatusBar.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="childDxStatusBar"></param>
        private void _MergeDxStatusBar(DxRibbonStatusBar childDxStatusBar)
        {
            _UnMergeDxStatusBar();
            this.MergeStatusBar(childDxStatusBar);
        }
        /// <summary>
        /// Merge dodaného StatusBaru.
        /// Tato metoda by se obecně neměla používat, používat se má <see cref="MergeDxStatusBar(DxRibbonStatusBar)"/>.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        /// <param name="childStatusBar"></param>
        public override void MergeStatusBar(RibbonStatusBar childStatusBar)
        {
            if (ChildDxStatusBar != null) ChildDxStatusBar.ParentDxStatusBar = null;
            ChildDxStatusBar = null;

            base.MergeStatusBar(childStatusBar);

            ChildDxStatusBar = childStatusBar as DxRibbonStatusBar;
            if (ChildDxStatusBar != null) ChildDxStatusBar.ParentDxStatusBar = this;
        }
        /// <summary>
        /// Odmerguje ze sebe svůj Child StatusBar, pokud takový je.
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        public void UnMergeDxStatusBar()
        {
            if (!HasMergedChildStatusBar) return;
            this.ParentOwner.RunInGui(() => _UnMergeDxStatusBar());
        }
        /// <summary>
        /// Odmerguje sebe ze svého Parenta, pokud jsem do něj mergován
        /// <para/>
        /// Tato metoda smí být volaná z libovolného threadu, invokaci GUI si zajistí sama.
        /// </summary>
        public void UnMergeCurrentDxFromParent()
        {
            var parentDxStatusBar = ParentDxStatusBar;
            if (parentDxStatusBar is null) return;
            this.ParentOwner.RunInGui(() => parentDxStatusBar.UnMergeStatusBar());
        }
        /// <summary>
        /// Odmerguje ze sebe svůj Child StatusBar.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        private void _UnMergeDxStatusBar()
        {
            if (HasMergedChildStatusBar)
                this.UnMergeStatusBar();
        }
        /// <summary>
        /// UnMerge stávajícího StatusBaru.
        /// Tato metoda by se obecně neměla používat, používat se má <see cref="UnMergeDxStatusBar()"/>.
        /// <para/>
        /// Tato metoda MUSÍ být volaná výhradně z GUI threadu
        /// </summary>
        public override void UnMergeStatusBar()
        {
            base.UnMergeStatusBar();
            if (ChildDxStatusBar != null) ChildDxStatusBar.ParentDxStatusBar = null;
            ChildDxStatusBar = null;
        }
        /// <summary>
        /// Obsahuje true, když v this StatusBaru je mergován nějaký Child (<see cref="ChildDxStatusBar"/> nebo alespoň <see cref="RibbonStatusBar.MergedStatusBar"/>)
        /// </summary>
        protected bool HasMergedChildStatusBar { get { return (this.MergedStatusBar != null); } }
        /// <summary>
        /// Aktuálně mergovaný Child <see cref="DxRibbonStatusBar"/>.
        /// Pozor, pokud někdo mergoval nativně nativní <see cref="RibbonStatusBar"/>, pak zde bude null!
        /// </summary>
        protected DxRibbonStatusBar ChildDxStatusBar { get; private set; }
        /// <summary>
        /// Obsahuje true, když this StatusBar je aktuálně mergovaný do nějakého Parenta (<see cref="ParentDxStatusBar"/>)
        /// </summary>
        protected bool IsMergedIntoParentStatusBar { get { return (this.ParentDxStatusBar != null); } }
        /// <summary>
        /// Parent <see cref="DxRibbonStatusBar"/>, do kterého je this <see cref="DxRibbonStatusBar"/> mergován jako Child.
        /// Pozor, pokud někdo mergoval nativně nativní <see cref="RibbonStatusBar"/>, pak zde bude null!
        /// </summary>
        protected DxRibbonStatusBar ParentDxStatusBar { get; private set; }
        #endregion
    }
    #endregion
    #region DxQuickAccessToolbar
    /// <summary>
    /// Singleton, který v sobě eviduje aktuální soupis uživatelských tlačítek QuickAccessToolbar, společně platný pro všechny Ribbony
    /// </summary>
    public class DxQuickAccessToolbar
    {
        #region Singleton
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static DxQuickAccessToolbar Current
        {
            get
            {
                if (__Current == null)
                {
                    lock (__Lock)
                    {
                        if (__Current == null)
                            __Current = new DxQuickAccessToolbar();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Private konstruktor
        /// </summary>
        private DxQuickAccessToolbar()
        {
            _QATItems = null;
            _QATLocation = RibbonQuickAccessToolbarLocation.Default;
            _ConfigValue = null;
        }
        private static object __Lock = new object();
        private static DxQuickAccessToolbar __Current;
        #endregion
        #region Public static, private instance
        /// <summary>
        /// Klíče všech User prvků v QAT, lze setovat. Setování změny vyvolá událost.
        /// Ribbon má setovat tuto hodnotu, nikoli hodnotu <see cref="ConfigValue"/>.
        /// </summary>
        public static string[] QATItems { get { return Current._QATItems; } set { Current._SetQATItems(value); } }
        /// <summary>
        /// Pozice QAT Toolbaru, lze setovat. Setování změny vyvolá událost.
        /// Ribbon má setovat tuto hodnotu, nikoli hodnotu <see cref="ConfigValue"/>.
        /// </summary>
        public static RibbonQuickAccessToolbarLocation QATLocation { get { return Current._QATLocation; } set { Current._SetLocation(value); } }
        /// <summary>
        /// Serializovaná hodnota pro ukládání do konfigurace.
        /// S touto hodnotou nemá pracovat Ribbon, ale správce (typicky Desktop).
        /// </summary>
        public static string ConfigValue { get { return Current._ConfigValue; } set { Current._SetConfigValue(value); } }
        /// <summary>
        /// Událost, která je vyvolána po každé změně hodnoty <see cref="ConfigValue"/>.
        /// Pozor, parametr sender je null; nelze tedy určit kdo změnu způsobil.
        /// To je dáno tím, že hodnota se setuje prostým přiřazením stringu do <see cref="ConfigValue"/>, bez předání způsobitele.
        /// </summary>
        public static event EventHandler ConfigValueChanged { add { Current._ConfigValueChanged += value; } remove { Current._ConfigValueChanged -= value; } }
        /// <summary>
        /// Vrátí true pokud aktuální QAT obsahuje prvek s daným klíčem
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static bool ContainsQATItem(string itemId) { return Current._ContainsQATItem(itemId); }
        /// <summary>
        /// Vloží nové položky QAT. Vložení NULL nedělá nic.
        /// </summary>
        /// <param name="qATItems"></param>
        private void _SetQATItems(string[] qATItems)
        {
            if (qATItems == null) return;
            List<string> itemIds = new List<string>();
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string oldConfigValue = _ConfigValue;
            string newConfigValue = _SerializeConfigValue(_QATLocation, qATItems, itemIds, dictionary);
            if (newConfigValue != oldConfigValue)
            {
                _QATItems = itemIds.ToArray();
                _QATDict = dictionary;
                _ConfigValue = _SerializeConfigValue();
                _RunConfigValueChanged();
            }
        }
        /// <summary>
        /// Položky QAT
        /// </summary>
        private string[] _QATItems;
        /// <summary>
        /// POložky QAT jako Dictionary pro rychlé hledání
        /// </summary>
        private Dictionary<string, string> _QATDict;
        /// <summary>
        /// Vloží hodnotu určující umístění Ribbonu. Platné jsou pouze hodnoty <see cref="RibbonQuickAccessToolbarLocation.Above"/> a <see cref="RibbonQuickAccessToolbarLocation.Below"/>, jiné nebudou vloženy.
        /// Vložení hodnoty, která zde už je, nevyvolá event.
        /// </summary>
        /// <param name="location"></param>
        private void _SetLocation(RibbonQuickAccessToolbarLocation location)
        {
            if ((location == RibbonQuickAccessToolbarLocation.Above || location == RibbonQuickAccessToolbarLocation.Below) && location != _QATLocation)
            {
                _QATLocation = location;
                _ConfigValue = _SerializeConfigValue();
                _RunConfigValueChanged();
            }
        }
        /// <summary>
        /// Pozice QAT
        /// </summary>
        private RibbonQuickAccessToolbarLocation _QATLocation;
        /// <summary>
        /// Vloží nový kompletní serializovaný string. Vložení NULL nedělá nic.
        /// </summary>
        /// <param name="configValue"></param>
        private void _SetConfigValue(string configValue)
        {
            if (configValue == null) return;
            string oldConfigValue = _ConfigValue;

            _DeSerializeConfigValue(configValue, out var location, out var qATItems);              // Získám korektní typové hodnoty
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string newConfigValue = _SerializeConfigValue(location, qATItems, null, dictionary);   // Získám zpětně formálně správný string
            if (!String.Equals(newConfigValue, oldConfigValue))
            {
                _QATLocation = location;
                _QATItems = qATItems;
                _QATDict = dictionary;
                _ConfigValue = newConfigValue;
                _RunConfigValueChanged();
            }
        }
        /// <summary>
        /// Kompletní string
        /// </summary>
        private string _ConfigValue;
        /// <summary>
        /// Vrátí true pokud aktuální QAT obsahuje prvek s daným klíčem
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private bool _ContainsQATItem(string itemId)
        {
            string key = GetValidQATKey(itemId);
            return (key.Length > 0 && _QATDict != null && _QATDict.ContainsKey(key));
        }
        #endregion
        #region Serializace
        /// <summary>
        /// Vrátí jeden string obsahující serializované zadané údaje
        /// </summary>
        /// <param name="location"></param>
        /// <param name="itemsId"></param>
        /// <returns></returns>
        public static string SerializeConfigValue(RibbonQuickAccessToolbarLocation location, IEnumerable<string> itemsId)
        {
            return _SerializeConfigValue(location, itemsId);
        }
        /// <summary>
        /// Vrátí jeden string obsahující serializované zdejší aktuální údaje
        /// </summary>
        /// <returns></returns>
        private string _SerializeConfigValue()
        {
            return _SerializeConfigValue(_QATLocation, _QATItems);
        }
        /// <summary>
        /// Vrátí jeden string obsahující serializované zadané údaje
        /// </summary>
        /// <param name="location"></param>
        /// <param name="qatKeys"></param>
        /// <param name="itemIds"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        private static string _SerializeConfigValue(RibbonQuickAccessToolbarLocation location, IEnumerable<string> qatKeys, List<string> itemIds = null, Dictionary<string, string> dictionary = null)
        {
            bool addItems = (itemIds != null);
            StringBuilder sb = new StringBuilder();
            string del = QATItemKeysDelimiterChar.ToString();
            string loc = LocationPrefix + location.ToString();
            sb.Append(loc + del);
            if (qatKeys != null)
            {
                if (dictionary == null)
                    dictionary = new Dictionary<string, string>();
                else if (dictionary.Count > 0)
                    dictionary.Clear();

                foreach (var qatKey in qatKeys)
                {
                    string key = GetValidQATKey(qatKey);
                    if (key.Length == 0 || dictionary.ContainsKey(key)) continue;
                    dictionary.Add(key, qatKey);
                    sb.Append(key + del);
                    if (addItems) itemIds.Add(key);
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Z daného stringu nalezene a do out parametrů uloží hodnoty v něm uložené.
        /// </summary>
        /// <param name="configValue"></param>
        /// <param name="location"></param>
        /// <param name="qATItems"></param>
        private static void _DeSerializeConfigValue(string configValue, out RibbonQuickAccessToolbarLocation location, out string[] qATItems)
        {
            location = RibbonQuickAccessToolbarLocation.Default;
            qATItems = null;
            if (String.IsNullOrEmpty(configValue)) return;

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            var qatKeys = configValue.Split(QATItemKeysDelimiterChar);
            int count = qatKeys.Length;
            string locationPrefix = LocationPrefix;
            for (int i = 0; i < count; i++)
            {
                string qatKey = qatKeys[i];
                if (String.IsNullOrEmpty(qatKey)) continue;
                if (i == 0 && qatKey.StartsWith(locationPrefix))
                {
                    if ((Enum.TryParse<RibbonQuickAccessToolbarLocation>(qatKey.Substring(locationPrefix.Length), out RibbonQuickAccessToolbarLocation loc)) && (loc == RibbonQuickAccessToolbarLocation.Above || loc == RibbonQuickAccessToolbarLocation.Below))
                        location = loc;
                }
                else
                {
                    string key = GetValidQATKey(qatKey);
                    if (key.Length > 0 && !dictionary.ContainsKey(key))
                        dictionary.Add(key, qatKey);
                }
            }
            qATItems = dictionary.Keys.ToArray();
        }
        /// <summary>
        /// Vyvolá událost <see cref="ConfigValueChanged"/>
        /// </summary>
        private void _RunConfigValueChanged()
        {
            _ConfigValueChanged?.Invoke(null, EventArgs.Empty);
        }
        /// <summary>Úložiště pro eventhandlery</summary>
        private event EventHandler _ConfigValueChanged;
        /// <summary>
        /// Metoda vrátí platný QAT klíč, pro použití v Dictionary.
        /// Vrácený klíč není NULL, je Trim. Jsou odstraněny nepatřičné výskyty delimiteru. Není změněna velikost písmen.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static string GetValidQATKey(string itemId)
        {
            return (itemId != null ? itemId.Trim().Replace(DxQuickAccessToolbar.QATItemKeysDelimiter, " ") : "");
        }
        /// <summary>
        /// Prefix umístění QAT, nachází se na pozici [0]
        /// </summary>
        private const string LocationPrefix = "Location:";
        /// <summary>
        /// Oddělovač jednotlivých klíčů v <see cref="QATItems"/> (=tabulátor), string délky 1 znak
        /// </summary>
        private const string QATItemKeysDelimiter = "\t";
        /// <summary>
        /// Oddělovač jednotlivých klíčů v <see cref="QATItems"/> (=tabulátor), char
        /// </summary>
        private const char QATItemKeysDelimiterChar = '\t';
        #endregion
    }
    #endregion
    #region Třídy definující Ribbon : defaultní implementace odpovídajících interface
    /// <summary>
    /// Kompletní deklarace dat Ribbonu, lze ji setovat do Ribbonu jedním řádkem
    /// </summary>
    public class DataRibbonContent : IRibbonContent
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonContent()
        {
            __ApplicationButtonText = null;
            __QATDirectItems = new ListExt<DataRibbonItem>();
            __TitleBarItems = new ListExt<DataRibbonItem>();
            __Pages = new ListExt<DataRibbonPage>();
            __StatusBarItems = new ListExt<DataRibbonItem>();
        }
        private string __ApplicationButtonText;
        private ListExt<DataRibbonItem> __QATDirectItems;
        private ListExt<DataRibbonItem> __TitleBarItems;
        private ListExt<DataRibbonPage> __Pages;
        private ListExt<DataRibbonItem> __StatusBarItems;

        /// <summary>
        /// Text tlačítka BackStage (typicky "HELIOS")
        /// </summary>
        public string ApplicationButtonText { get { return __ApplicationButtonText; } set { __ApplicationButtonText = value; } }
        /// <summary>
        /// Explicitně přidané prvky do prostoru QAT (Quick Acces Toolbar), nad rámec prvků ze standardních stránek Ribbonu
        /// </summary>
        public ListExt<DataRibbonItem> QATDirectItems { get { return __QATDirectItems; } }
        /// <summary>
        /// Prvky fixně umístěné vpravo v titulkovém řádku
        /// </summary>
        public ListExt<DataRibbonItem> TitleBarItems { get { return __TitleBarItems; } }
        /// <summary>
        /// Souhrn standardních stránek Ribbonu
        /// </summary>
        public ListExt<DataRibbonPage> Pages { get { return __Pages; } }
        /// <summary>
        /// Prvky umístěné dole ve stavovém řádku
        /// </summary>
        public ListExt<DataRibbonItem> StatusBarItems { get { return __StatusBarItems; } }
        /// <summary>
        /// Metoda najde a vrátí prvek daného jména <paramref name="itemId"/>.
        /// Metoda prohledává všechny prvky ve své deklaraci = ve všech polích.
        /// Pokud takový prvek neexistuje, anebo je jich více, pak vrátí false.
        /// Je možno použít metodu <see cref="GetDataItems(string)"/>, která je stejně rychlá, ale vrací pole všech nalezených prvků - pokud si s nimi volající poradí.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        public bool TryGetDataItem(string itemId, out DataRibbonItem dataItem)
        {
            var items = GetDataItems(itemId);
            if (items.Length == 1)
            {
                dataItem = items[0];
                return true;
            }
            dataItem = null;
            return false;
        }
        /// <summary>
        /// Metoda najde a vrátí prvky daného jména <paramref name="itemId"/>.
        /// Metoda prohledává všechny prvky ve své deklaraci = ve všech polích.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public DataRibbonItem[] GetDataItems(string itemId)
        {
            return GetDataItems(i => String.Equals(i?.ItemId, itemId));
        }
        /// <summary>
        /// Metoda najde a vrátí prvky vyhovující danému filtru <paramref name="filter"/>.
        /// Metoda prohledává všechny prvky ve své deklaraci = ve všech polích.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public DataRibbonItem[] GetDataItems(Func<DataRibbonItem, bool> filter)
        {
            List<DataRibbonItem> result = new List<DataRibbonItem>();
            addDItems(this.QATDirectItems);
            addDItems(this.TitleBarItems);
            addIItems(this.Pages.SelectMany(p => p.Groups).SelectMany(g => g.Items));
            addDItems(this.StatusBarItems);


            return result.ToArray();

            void addIItems(IEnumerable<IRibbonItem> items)
            {
                if (items != null)
                    result.AddRange(items.OfType<DataRibbonItem>().Where(i => filter(i)));
            }
            void addDItems(IEnumerable<DataRibbonItem> items)
            {
                if (items != null)
                    result.AddRange(items.Where(i => filter(i)));
            }
        }

        string IRibbonContent.ApplicationButtonText { get { return __ApplicationButtonText; } }
        IRibbonItem[] IRibbonContent.QATDirectItems { get { return __QATDirectItems.ToArray(); } }
        IRibbonItem[] IRibbonContent.TitleBarItems { get { return __TitleBarItems.ToArray(); } }
        IRibbonPage[] IRibbonContent.Pages { get { return __Pages.ToArray(); } }
        IRibbonItem[] IRibbonContent.StatusBarItems { get { return __StatusBarItems.ToArray(); } }
    }
    /// <summary>
    /// Definice stránky v Ribbonu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataRibbonPage : IRibbonPage
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonPage()
        {
            this._PageId = null;
            this.Visible = true;
            this.ChangeMode = ContentChangeMode.Add;
            this.Groups = new ListExt<IRibbonGroup>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.PageText;
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="ToString()"/>
        /// </summary>
        protected virtual string DebugText
        {
            get
            {
                string debugText = $"Page: {this.PageText}; Groups: {Groups.Count}";
                return debugText;
            }
        }
        /// <summary>
        /// Z dodané kolekce stránek sestaví setříděný List a vrátí jej
        /// </summary>
        /// <param name="pages"></param>
        /// <returns></returns>
        public static ListExt<IRibbonPage> SortPages(IEnumerable<IRibbonPage> pages)
        {
            var list = new ListExt<IRibbonPage>();
            if (pages != null)
                list.AddRange(pages.Where(p => p != null));
            if (list.Count > 1)
            {
                int pageOrder = 0;
                foreach (var item in list)
                {
                    if (item.PageOrder == 0) item.PageOrder = ++pageOrder; else if (item.PageOrder > pageOrder) pageOrder = item.PageOrder;
                }
                list.Sort((a, b) => a.PageOrder.CompareTo(b.PageOrder));
            }
            return list;
        }
        /// <summary>
        /// Jméno Ribbonu
        /// </summary>
        public virtual string ParentRibbonName { get; set; }
        /// <summary>
        /// Kategorie, do které patří tato stránka. Může být null pro běžné stránky Ribbonu.
        /// </summary>
        public virtual IRibbonCategory Category { get; set; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci Ribbonu
        /// </summary>
        public virtual string PageId
        {
            get
            {
                if (_PageId == null) _PageId = DxComponent.CreateGuid();
                return _PageId;
            }
            set { _PageId = value; }
        }
        /// <summary>
        /// Reálně uložené ID stránky
        /// </summary>
        private string _PageId;
        /// <summary>
        /// Režim pro vytvoření / refill / remove této stránky
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Pořadí stránky v rámci jednoho pole, použije se pro setřídění v rámci nadřazeného prvku.
        /// <para/>
        /// POZOR: 0 a záporná čísla jsou ignorována, taková stránka bude na konci!!!
        /// </summary>
        public virtual int PageOrder { get; set; }
        /// <summary>
        /// Pořadí stránky v rámci mergování, vkládá se do RibbonPage.
        /// <para/>
        /// POZOR: 0 a záporná čísla jsou ignorována, taková stránka bude na konci!!!
        /// </summary>
        public virtual int MergeOrder { get; set; }
        /// <summary>
        /// Jméno stránky
        /// </summary>
        public virtual string PageText { get; set; }
        /// <summary>
        /// Viditelnost stránky
        /// </summary>
        public virtual bool Visible { get; set; }
        /// <summary>
        /// Typ stránky
        /// </summary>
        public virtual RibbonPageType PageType { get; set; }
        /// <summary>
        /// Režim práce se stránkou (opožděné načítání, refresh před každým načítáním)
        /// </summary>
        public virtual RibbonContentMode PageContentMode { get; set; }
        /// <summary>
        /// Pole skupin v této stránce
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        public virtual ListExt<IRibbonGroup> Groups { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonGroup> IRibbonPage.Groups { get { return this.Groups; } }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonPage"/> po jejím vytvoření.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual WeakTarget<DxRibbonPage> RibbonPage { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual object Tag { get; set; }
    }
    /// <summary>
    /// Definice kategorie, do které patří stránka v Ribbonu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataRibbonCategory : IRibbonCategory
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonCategory()
        {
            _CategoryId = null;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.CategoryText;
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="ToString()"/>
        /// </summary>
        protected virtual string DebugText
        {
            get
            {
                string debugText = $"Category: {this.CategoryText}";
                return debugText;
            }
        }
        /// <summary>
        /// Jméno Ribbonu
        /// </summary>
        public virtual string ParentRibbonName { get; set; }
        /// <summary>
        /// ID kategorie, jednoznačné per Ribbon
        /// </summary>
        public virtual string CategoryId
        {
            get
            {
                if (_CategoryId == null) _CategoryId = DxComponent.CreateGuid();
                return _CategoryId;
            }
            set { _CategoryId = value; }
        }
        /// <summary>
        /// Reálně uložené ID stránky
        /// </summary>
        private string _CategoryId;
        /// <summary>
        /// Režim pro vytvoření / refill / remove této grupy
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Titulek kategorie zobrazovaný uživateli
        /// </summary>
        public virtual string CategoryText { get; set; }
        /// <summary>
        /// Barva kategorie
        /// </summary>
        public virtual Color? CategoryColor { get; set; }
        /// <summary>
        /// Kategorie je viditelná?
        /// </summary>
        public virtual bool CategoryVisible { get; set; }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonPageCategory"/> po jejím vytvoření.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual WeakTarget<DxRibbonPageCategory> RibbonCategory { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual object Tag { get; set; }
    }
    /// <summary>
    /// Definice skupiny ve stránce Ribbonu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataRibbonGroup : IRibbonGroup
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonGroup()
        {
            this._GroupId = null;
            this.ChangeMode = ContentChangeMode.Add;
            this.GroupState = RibbonGroupState.Expanded;
            this.AllowTextClipping = true;
            this.Visible = true;
            this.Items = new ListExt<IRibbonItem>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GroupText;
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="ToString()"/>
        /// </summary>
        protected virtual string DebugText
        {
            get
            {
                string debugText = $"Id: {_GroupId}; Text: {GroupText}; Items: {Items.Count}";
                return debugText;
            }
        }
        /// <summary>
        /// Z dodané kolekce stránek sestaví setříděný List a vrátí jej
        /// </summary>
        /// <param name="groups"></param>
        /// <returns></returns>
        public static ListExt<IRibbonGroup> SortGroups(IEnumerable<IRibbonGroup> groups)
        {
            var list = new ListExt<IRibbonGroup>();
            if (groups != null)
                list.AddRange(groups.Where(p => p != null));
            if (list.Count > 1)
            {
                int groupOrder = 0;
                foreach (var item in list)
                {
                    if (item.GroupOrder == 0) item.GroupOrder = ++groupOrder; else if (item.GroupOrder > groupOrder) groupOrder = item.GroupOrder;
                }
                list.Sort((a, b) => a.GroupOrder.CompareTo(b.GroupOrder));
            }
            return list;
        }
        /// <summary>
        /// Parent prvku = <see cref="IRibbonPage"/>
        /// </summary>
        [XS.PersistingEnabled(false)]
        public IRibbonPage ParentPage { get; set; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci stránky
        /// </summary>
        public string GroupId
        {
            get
            {
                if (_GroupId == null) _GroupId = DxComponent.CreateGuid();
                return _GroupId;
            }
            set { _GroupId = value; }
        }
        /// <summary>
        /// Reálně uložené ID grupy
        /// </summary>
        private string _GroupId;
        /// <summary>
        /// Režim pro vytvoření / refill / remove této grupy
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Pořadí grupy v rámci jednoho pole, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public virtual int GroupOrder { get; set; }
        /// <summary>
        /// Pořadí grupy v rámci mergování, vkládá se do RibbonGroup
        /// </summary>
        public virtual int MergeOrder { get; set; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        public virtual string GroupText { get; set; }
        /// <summary>
        /// Viditelnost grupy
        /// </summary>
        public virtual bool Visible { get; set; }
        /// <summary>
        /// Obrázek grupy
        /// </summary>
        public virtual string GroupImageName { get; set; }
        /// <summary>
        /// Zobrazit speciální tlačítko grupy vpravo dole v titulku grupy (lze tak otevřít nějaké okno vlastností pro celou grupu)
        /// </summary>
        public virtual bool GroupButtonVisible { get; set; }
        /// <summary>
        /// Povolit zkrácení textu? Default = true
        /// </summary>
        public virtual bool AllowTextClipping { get; set; }
        /// <summary>
        /// Stav grupy
        /// </summary>
        public virtual RibbonGroupState GroupState { get; set; }
        /// <summary>
        /// Rozložení prvků v grupě
        /// </summary>
        public virtual RibbonGroupItemsLayout LayoutType { get; set; }
        /// <summary>
        /// Soupis prvků grupy (tlačítka, menu, checkboxy, galerie)
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        public virtual ListExt<IRibbonItem> Items { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonItem> IRibbonGroup.Items { get { return this.Items; } }
        /// <summary>
        /// Požadavek (true) na skrývání grupy, která neobsahuje žádné prvky.<br/>
        /// Default = false: pokud bude dodána prázdná grupa, bude zobrazena.
        /// </summary>
        public bool HideEmptyGroup { get; set; }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonGroup"/> po jejím vytvoření.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual WeakTarget<DxRibbonGroup> RibbonGroup { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual object Tag { get; set; }
    }
    /// <summary>
    /// Definice prvku typu ComboBox umístěného v Ribbonu
    /// </summary>
    public class DataRibbonComboItem : DataRibbonItem, IRibbonComboItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonComboItem() : base()
        {
            ItemType = RibbonItemType.ComboListBox;
            ComboBorderStyle = DxBorderStyle.None;
            SubButtonsBorderStyle = DxBorderStyle.HotFlat;
            SubButtons = null;
        }
        /// <summary>
        /// Typ okraje celého ComboBoxu
        /// </summary>
        public DxBorderStyle ComboBorderStyle { get; set; }
        /// <summary>
        /// Typ okraje jednotlivých sub-buttonů v ComboBoxu
        /// </summary>
        public DxBorderStyle SubButtonsBorderStyle { get; set; }
        /// <summary>
        /// Šířka prvku v pixelech
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Text zobrazený při nevybraném žádném prvku
        /// </summary>
        public string NullValuePrompt { get; set; }
        /// <summary>
        /// SubButtony zobrazené vedle prvku (podporuje je zatím jen typ <see cref="RibbonItemType.ComboListBox"/>).
        /// Typicky tlačítko "+", třitečky, Clear, Search, Delete, a libovolné další.<br/>
        /// String má formu: "Name=Image[;Name=Image[;...]]<br/>
        /// Kde Name = jméno buttonu, které se následně přidává ke jménu vlastního prvku (za tečku) po kliknutí na něj, když se volá aplikační server (MenuItemClick).
        /// Jako název Image lze použít buď hodnotu z enumu <see cref="PredefinedButtonType"/>, anebo standardní jméno ikony v rámci systému.<br/>
        /// Pořadí ve stringu definuje pořadí buttonů.
        /// Například <see cref="SubButtons"/> <![CDATA[Clear=Close;Manager=pic_0/Menu/bigfilter]]> definuje dva buttony, 
        /// první má význam "Zrušit" a vyvolá Command = "command.Clear", druhý má význam "Nabídni okno filtrů" (ikona pic_0/Menu/bigfilter) a vyvolá Command = "command.Manager"
        /// <para/>
        /// Pokud zde bude null, pak bude ComboBox obsahovat standardní DropDown button (šipka dolů, rozbalující ComboBox).<br/>
        /// Pokud uživatel chce přidat DropDown button a poté i další svoje Buttony, pak na začátek stringu vloží definici <u><c>"DropDown"</c></u> (netřeba definovat Image); a pak za středníkem dává svoje buttony.<br/>
        /// Pokud uživatel naplní definici a nebude v ní DropDown, pak prostě nebude zobrazen!<br/>
        /// Pokud na začátku jména bude &lt; pak button bude vlevo, například: <![CDATA[Clear=Close;<Manager=pic_0/Menu/bigfilter]]> 
        /// </summary>
        public string SubButtons { get; set; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataRibbonItem : DataMenuItem, IRibbonItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonItem() : base()
        {
            this.ChangeMode = ContentChangeMode.Add;
            this.ItemType = RibbonItemType.Button;
            this.RibbonStyle = RibbonItemStyles.All;
            this.ItemPaintStyle = BarItemPaintStyle.CaptionGlyph;
            this.QatKey = null;
            this.DelayMilisecBeforeNextClick = null;
            this.DelayLastClickTime = null;
            this.DelayTimerGuid = null;
            this.VisibleInSearchMenu = true;
        }
        /// <summary>
        /// Metoda vytvoří new instanci třídy <see cref="DataRibbonItem"/>, které bude obsahovat data z dodané <see cref="IRibbonItem"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static DataRibbonItem CreateClone(IRibbonItem source, Action<DataRibbonItem> modifier = null)
        {
            if (source == null) return null;
            DataRibbonItem clone = new DataRibbonItem();
            clone.FillFrom(source);
            if (modifier != null) modifier(clone);
            return clone;
        }
        /// <summary>
        /// Do this instance přenese patřičné hodnoty ze source instance
        /// </summary>
        /// <param name="source"></param>
        protected void FillFrom(IRibbonItem source)
        {
            base.FillFrom((IMenuItem)source);

            ParentGroup = source.ParentGroup;
            ParentRibbonItem = source.ParentRibbonItem;
            ItemType = source.ItemType;
            RadioButtonGroupName = source.RadioButtonGroupName;
            RibbonStyle = source.RibbonStyle;
            ButtonGroupColumnCount = source.ButtonGroupColumnCount;
            PrepareDisabledImage = source.PrepareDisabledImage;
            ImageListMode = source.ImageListMode;
            Size = source.Size;
            BackColor = source.BackColor;
            TextColor = source.TextColor;
            StyleName = source.StyleName;
            Alignment = source.Alignment;
            DelayMilisecBeforeNextClick = source.DelayMilisecBeforeNextClick;
            DelayLastClickTime = source.DelayLastClickTime;
            DelayTimerGuid = source.DelayTimerGuid;
            VisibleInSearchMenu = source.VisibleInSearchMenu;
            Size = source.Size;
            QatKey = source.QatKey;
            SearchTags = source.SearchTags;
            SubItemsContentMode = source.SubItemsContentMode;
            SubItems = source.SubItems?.ToListExt();
            RepositoryEditorInfo = source.RepositoryEditorInfo;
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
        /// Počet SubItems jako string
        /// </summary>
        private string _SubItemsCount { get { return (this.SubItems == null ? "NULL" : this.SubItems.Count.ToString()); } }
        /// <summary>
        /// Z dodané kolekce prvků sestaví setříděný List a vrátí jej
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static ListExt<IRibbonItem> SortRibbonItems(IEnumerable<IRibbonItem> items)
        {
            var list = new ListExt<IRibbonItem>();
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
        /// Metoda vrátí true, pokud dodané dvě instance obsahují shodné hodnoty. Včetně rekurzivních SubItems.
        /// <para/>
        /// Porovnávají se hodnoty: ItemId, Text, ToolTipText, ToolTipTitle, ToolTipIcon, ImageName, ImageNameChecked, ImageNameUnChecked, 
        /// Enabled, HotKey, HotKeys, ItemIsFirstInGroup, ItemPaintStyle, ItemType, RibbonStyle, Shortcut, Visible, VisibleInSearchMenu.
        /// <para/>
        /// Neporovnává se hodnota Checked, ani Parent.
        /// </summary>
        /// <param name="itemA"></param>
        /// <param name="itemB"></param>
        /// <returns></returns>
        internal static bool HasEqualContent(IRibbonItem itemA, IRibbonItem itemB)
        {
            bool nullA = (itemA is null);
            bool nullB = (itemB is null);
            if (nullA && nullB) return true;               // Dvě NULL jsou Equal
            if (nullA || nullB) return false;              // NULL a NotNULL není Equal

            // Obě instance jsou NotNULL, porovnáme obsah, první odlišnost vrátí false = Neshodnost:
            if (!String.Equals(itemA.ItemId, itemB.ItemId)) return false;
            if (!String.Equals(itemA.Text, itemB.Text)) return false;
            if (!String.Equals(itemA.ToolTipText, itemB.ToolTipText)) return false;
            if (!String.Equals(itemA.ToolTipTitle, itemB.ToolTipTitle)) return false;
            if (!String.Equals(itemA.ToolTipIcon, itemB.ToolTipIcon)) return false;
            if (!String.Equals(itemA.ImageName, itemB.ImageName)) return false;
            if (!String.Equals(itemA.ImageNameChecked, itemB.ImageNameChecked)) return false;
            if (!String.Equals(itemA.ImageNameUnChecked, itemB.ImageNameUnChecked)) return false;
            if (itemA.Enabled != itemB.Enabled) return false;
            if (!String.Equals(itemA.HotKey, itemB.HotKey)) return false;
            if (itemA.HotKeys != itemB.HotKeys) return false;
            if (itemA.ItemIsFirstInGroup != itemB.ItemIsFirstInGroup) return false;
            if (itemA.ItemPaintStyle != itemB.ItemPaintStyle) return false;
            if (itemA.ItemType != itemB.ItemType) return false;
            if (itemA.RibbonStyle != itemB.RibbonStyle) return false;
            if (itemA.ButtonGroupColumnCount != itemB.ButtonGroupColumnCount) return false;
            if (itemA.Shortcut != itemB.Shortcut) return false;
            if (itemA.Visible != itemB.Visible) return false;
            if (itemA.VisibleInSearchMenu != itemB.VisibleInSearchMenu) return false;
            if (itemA.PrepareDisabledImage != itemB.PrepareDisabledImage) return false;
            if (itemA.ImageListMode != itemB.ImageListMode) return false;
            if (itemA.Size != itemB.Size) return false;
            if (itemA.BackColor != itemB.BackColor) return false;
            if (itemA.TextColor != itemB.TextColor) return false;
            if (itemA.SubItemsContentMode != itemB.SubItemsContentMode) return false;

            if ((itemA.ItemType == RibbonItemType.CheckBoxStandard || itemA.ItemType == RibbonItemType.CheckBoxPasive || itemA.ItemType == RibbonItemType.CheckBoxToggle) && itemA.Checked != itemB.Checked) return false;

            // a SubItems:
            nullA = (itemA.SubItems is null);
            nullB = (itemB.SubItems is null);
            if (nullA && nullB) return true;               // Dvě NULL jsou Equal
            if (nullA || nullB) return false;              // NULL a NotNULL není Equal

            // Jednotlivé prvky, rekurzivně:
            var itemsA = itemA.SubItems.ToArray();
            var itemsB = itemB.SubItems.ToArray();
            if (itemsA.Length != itemsB.Length) return false;
            int count = itemsA.Length;
            for (int i = 0; i < count; i++)
            {
                if (!HasEqualContent(itemsA[i], itemsB[i])) return false;
            }

            return true;                                   // Nikde jsme nevypadli na rozdíl hodnoty? Téměř zázrak...
        }
        /// <summary>
        /// Parent prvku = <see cref="IRibbonGroup"/>
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual IRibbonGroup ParentGroup 
        {
            get { return __ParentGroup; }
            set { __ParentGroup = value; }
        }
        private IRibbonGroup __ParentGroup;
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IRibbonItem"/>
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual IRibbonItem ParentRibbonItem { get; set; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public new RibbonItemType ItemType { get; set; }
        /// <summary>
        /// Jméno grupy, v jejímž rámci se buttony typu <see cref="RibbonItemType.RadioItem"/> a <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/> přetahují o stav <see cref="ITextItem.Checked"/>.
        /// Pokud je zde prázdný string, pak buttony typu <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/> fungují jako samostatný CheckBox.
        /// </summary>
        public string RadioButtonGroupName { get; set; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        public virtual RibbonItemStyles RibbonStyle { get; set; }
        /// <summary>
        /// Počet sloupců (=počet ikon vedle sebe) v bloku "Rychlá volba", definovaný <see cref="ItemType"/> == <see cref="RibbonItemType.ButtonGroup"/>.
        /// Pokud je definována grupa, ale není dán počet buttonů, pak je vytvořena jako běžné SubMenu.
        /// </summary>
        public virtual int? ButtonGroupColumnCount { get; set; }
        /// <summary>
        /// Požadavek na přípravu ikony typu 'Disabled' i pro ten prvek Ribbonu, který má aktuálně hodnotu Enabled = true;
        /// <para/>
        /// Důvod: pokud budeme řídit přímo hodnotu BarItem.Enabled až po vytvoření BarItem, pak si tento BarItem sám řídí, který Image zobrazuje: zda standardní, nebo Disabled.
        /// <para/>
        /// V Nephrite může zůstat false, protože Nephrite mění hodnotu Enabled pomocí refreshe celého prvku, a po změně Enabled se vygeneruje správný Image automaticky.
        /// </summary>
        public virtual bool PrepareDisabledImage { get; set; }
        /// <summary>
        /// Režim práce s ImageList a Image
        /// </summary>
        public virtual DxImageListMode? ImageListMode { get; set; }
        /// <summary>
        /// Cílová velikost; využije se jen u některých prvků
        /// </summary>
        public virtual System.Drawing.Size? Size { get; set; }
        /// <summary>
        /// Barva pozadí; využije se jen u některých prvků, pokud není null
        /// </summary>
        public virtual System.Drawing.Color? BackColor { get; set; }
        /// <summary>
        /// Barva textu; využije se jen u některých prvků, pokud není null
        /// </summary>
        public virtual System.Drawing.Color? TextColor { get; set; }
        /// <summary>
        /// Kalíšek s barvami
        /// </summary>
        public virtual string StyleName { get; set; }
        /// <summary>
        /// Zarovnání prvku; uplatní se u StatusBaru
        /// </summary>
        public virtual BarItemAlignment? Alignment { get; set; }
        /// <summary>
        /// Doba (v milisekundách), která musí uplynout před opakovaným kliknutím na tento button. Rychlejší kliknutí (doubleclick) bude ignorováno.
        /// <para/>
        /// U běžných tlačítek nám opakované kliknutí nevadí (refresh, posun řádku nahoru/dolů, atd).<br/>
        /// Jsou ale tlačítka (funkce, workflow, ...), kde opakované vyvolání téže akce je problémem. 
        /// Pak se doporučuje nastavit dostatečnou kladnou hodnotu, která zajistí, že první kliknutí se dostane z klienta na server, zpracuje se a odešle na klienta refresh menu, kde zdrojový button např. již nebude přítomen.<br/>
        /// Hodnota null je default a umožní opakované odeslání akce na server bez časového omezení.
        /// </summary>
        public virtual int? DelayMilisecBeforeNextClick { get; set; }
        /// <summary>
        /// DateTime kdy bylo naposledy kliknuto. Řídí neaktivní interval spolu s hodnotou <see cref="DelayMilisecBeforeNextClick"/>.
        /// </summary>
        public virtual DateTime? DelayLastClickTime { get; set; }
        /// <summary>
        /// Prostor pro ID budíku, který řídí obnovení stavu Enabled pro odpovídající BarItem po uplynutí času <see cref="DelayMilisecBeforeNextClick"/> po akceptování kliknutí
        /// </summary>
        public virtual Guid? DelayTimerGuid { get; set; }
        /// <summary>
        /// Zobrazit v Search menu?
        /// </summary>
        public virtual bool VisibleInSearchMenu { get; set; }
        /// <summary>
        /// Explicitní klíč pro QAT (Quick Access Toolbar = Panel rychlého přístupu nad/pod Ribbonem). 
        /// Pokud zde bude null, použije se jako default <see cref="ITextItem.ItemId"/> = výchozí stav.
        /// Pokud zde bude prázdný string, pak tento prvek do QAT nelze přidat = nezobrazí nse odpovídající kontextové menu (nebo jeho položky).
        /// A prvky dříve přidané (s původním klíčem) nebudou nově v QAT zobrazeny.
        /// <para/>
        /// Pokud zde bude explicitní string, potom do QAT bude použit tento klíč. 
        /// Tímto způsobem je možno stabilně dostat do QAT taková tlačítka, jimž se dynamicky mění jejich ItemId.
        /// Typicky: mějme funkci, která <u>není vybraná jako Oblíbená</u>. Taková funkce má ItemId = <c>MnBrwFunc#Zpracování#F:Z10177(10177):11</c>. <br/>
        /// Přidělme jí ale <see cref="QatKey"/> = <c>MenuFunkce#F:10177</c>.<br/>
        /// Pak ji uživatel přidá do QAT, tam bude mít nově klíč <c>MenuFunkce#F:10177</c> (namísto defaultního dle ItemId).<br/>
        /// Dále ji uživatel v Nephrite zvolí jako Oblíbenou. Tím se přesune z grupy "Funkce:Zpracování" do grupy "Home:Funkce" a "Funkce:Oblíbené", tím se jí změní ItemId.
        /// Pokud bude i nadále mít <see cref="QatKey"/> = <c>MenuFunkce#F:10177</c>, pak ji uživatel uvidí v QAT (jedenkrát, protože oba buttony mají shodné QatKey).<br/>
        /// Pokud bychom neměli přidělen <see cref="QatKey"/>, pak by v daném postupu funkce z QAT zmizela, a uživatel by si ji tam mohl přidat dvakrát = ze dvou různých míst (Home, Funkce).
        /// </summary>
        public virtual string QatKey { get { return (__QatKey is null ? ItemId : __QatKey); } set { __QatKey = value; } } private string __QatKey;
        /// <summary>
        /// Přidané texty, podle kterých může být prvek vyhledán v Search menu. Texty jsou oddělené čárkou (comma-separated).
        /// </summary>
        public virtual string SearchTags { get; set; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        public virtual RibbonContentMode SubItemsContentMode { get; set; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu.
        /// Výchozí hodnota je null.
        /// </summary>
        public new ListExt<IRibbonItem> SubItems { get; set; }
        /// <summary>
        /// Data pro RepositoryEditor
        /// </summary>
        public virtual IRepositoryEditorInfo RepositoryEditorInfo { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonItem> IRibbonItem.SubItems { get { return this.SubItems; } }
        /// <summary>
        /// Sem bude umístěn fyzický BarItem po jeho vytvoření.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual BarItem RibbonItem
        {
            get { var weakItem = __GuiItem; if (weakItem != null && weakItem.IsAlive && weakItem.Target is BarItem barItem) return barItem; return null; }
            set { __GuiItem = null; if (value != null) __GuiItem = new WeakReference(value); }
        }
        /// <summary>
        /// Sem bude umístěno Popup menu po jeho vytvoření.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual PopupMenu PopupMenu
        {
            get { var weakItem = __GuiItem; if (weakItem != null && weakItem.IsAlive && weakItem.Target is PopupMenu popupMenu) return popupMenu; return null; }
            set { __GuiItem = null; if (value != null) __GuiItem = new WeakReference(value); }
        }
        /// <summary>
        /// Zkusí získat a vrátit fyzický GUI prvek z tohoto datového prvku.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        public bool TryGetRibbonItem(out BarItem ribbonItem)
        {
            ribbonItem = RibbonItem;
            return (ribbonItem != null);
        }
        private WeakReference __GuiItem;
        /// <summary>
        /// Metoda zajistí aktualizaci vizuálního buttonu <see cref="RibbonItem"/> z dat, která jsou aktuálně přítomna v this instanci.
        /// Je vhodné volat tehdy, když podle this definice už byl vytvořen prvek v reálném Ribbonu, a my jsme v definici provedli nějaké změny a přejeme si je promítnout do vizuálního prvku.
        /// </summary>
        public virtual void Refresh()
        {
            DxRibbonControl.RefreshIRibbonItem(this, false, false);
        }
        /// <summary>
        /// Metoda zajistí aktualizaci vizuálního buttonu <see cref="RibbonItem"/> z dat, která jsou aktuálně přítomna v this instanci.
        /// Je vhodné volat tehdy, když podle this definice už byl vytvořen prvek v reálném Ribbonu, a my jsme v definici provedli nějaké změny a přejeme si je promítnout do vizuálního prvku.
        /// </summary>
        /// <param name="refreshInSite">Pro refresh prvku neprováděj Unmerge - Modify - Merge. Změna je malá a režie by byla zbytečná.</param>
        public virtual void Refresh(bool refreshInSite)
        {
            DxRibbonControl.RefreshIRibbonItem(this, false, refreshInSite);
        }
    }
    /// <summary>
    /// Data jednoho SubButtonu, a jeho de/serializace. Obecně použitelná třída.
    /// </summary>
    internal class DataSubButton
    {
        #region Data
        public DataSubButton()
        {
            ButtonType = PredefinedButtonType.DropDown;
            Enabled = true;
        }
        /// <summary>
        /// ID Buttonu
        /// </summary>
        public string ButtonId { get; set; }
        /// <summary>
        /// Předdefinovaný typ buttonu:
        /// Tím se definuje druh chování (např. <see cref="PredefinedButtonType.DropDown"/>) a použití standardní ikony (kromě <see cref="PredefinedButtonType.Glyph"/>.
        /// Default = <see cref="PredefinedButtonType.DropDown"/>.
        /// </summary>
        public PredefinedButtonType ButtonType { get; set; }
        /// <summary>
        /// Název ikony
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Enabled. Default = true.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Umístěn vlevo od základního prvku
        /// </summary>
        public bool IsLeft { get; set; }
        /// <summary>
        /// ToolTip: titulek
        /// </summary>
        public string ToolTipTitle { get; set; }
        /// <summary>
        /// ToolTip: text
        /// </summary>
        public string ToolTipText { get; set; }
        /// <summary>
        /// HotKey
        /// </summary>
        public Keys? Shortcut { get; set; }
        #endregion
        #region De + Serializace
        /// <summary>
        /// Z dodaného textu vygeneruje (deserializuje) sadu buttonů.
        /// Výstupem může být null.
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        public static DataSubButton[] Deserialize(string serial)
        {
            if (String.IsNullOrEmpty(serial)) return null;

            List<DataSubButton> buttons = new List<DataSubButton>();

            if (!String.IsNullOrEmpty(serial))
            {
                var parts = serial.Split(_ButtonDelimiter);
                foreach (string part in parts)
                {
                    var button = deserializeOne(part);
                    if (button != null)
                        buttons.Add(button);
                }
            }

            return buttons.ToArray();

            // deserializuje z dodaného textu jeden button
            DataSubButton deserializeOne(string text)
            {
                if (String.IsNullOrEmpty(text)) return null;
                var items = text.Split(_ItemDelimiter);
                int length = items.Length;
                if (length < 2) return null;

                DataSubButton button = new DataSubButton();
                button.ButtonId = items[0];
                button.ButtonType = Enum.TryParse<PredefinedButtonType>(items[1], true, out var bt) ? bt : PredefinedButtonType.DropDown;
                button.ImageName = (length > 2 ? items[2] : null);
                button.Enabled = (length > 3 ? items[3] : "E") != "D";
                button.IsLeft = (length > 4 ? items[4] : "R") == "L";
                button.ToolTipTitle = (length > 5 ? items[5] : null);
                button.ToolTipText = (length > 6 ? items[6] : null);

                string shortcut = (length > 7 ? items[7] : null);
                if (!String.IsNullOrEmpty(shortcut) && Int32.TryParse(shortcut, out int keys))
                    button.Shortcut = (Keys)keys;

                return button;
            }
        }
        /// <summary>
        /// Z dodané kolekce buttonů vrátí jeden string
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public static string Serialize(IEnumerable<DataSubButton> buttons)
        {
            if (buttons is null) return null;

            StringBuilder serial = new StringBuilder();
            string buttonDelimiter = _ButtonDelimiter.ToString();
            string buttonReplacement = ">";
            string itemDelimiter = _ItemDelimiter.ToString();
            string itemReplacement = "<";

            // Buttony:
            foreach (var but in buttons)
                serializeOne(but);

            return serial.ToString();

            // Do serial přidá serializovaná data dodaného buttonu
            void serializeOne(DataSubButton button)
            {
                if (button is null) return;

                // Oddělovač mezi prvky:
                if (serial.Length > 0) serial.Append(buttonDelimiter);

                // Sekvence dat:
                append(button.ButtonId);
                append(button.ButtonType.ToString());
                append(button.ImageName);
                append(button.Enabled ? "E" : "D");
                append(button.IsLeft ? "L" : "R");
                append(button.ToolTipTitle);
                append(button.ToolTipText);
                append(button.Shortcut.HasValue ? ((int)(button.Shortcut.Value)).ToString() : "");
            }
            // Přidá daný text a itemDelimier
            void append(string text)
            {
                text = getText(text);
                serial.Append(text + itemDelimiter);
            }
            // Vrátí not null text, kde namísto funkčních delimiterů budu jejich neškodné náhrady
            string getText(string text)
            {
                if (text is null) return "";
                if (text.Contains(buttonDelimiter)) text = text.Replace(buttonDelimiter, buttonReplacement);
                if (text.Contains(itemDelimiter)) text = text.Replace(itemDelimiter, itemReplacement);
                return text;
            }
        }
        private static char _ButtonDelimiter { get { return '»'; } }
        private static char _ItemDelimiter { get { return '«'; } }
        #endregion
    }
    #endregion
    #region Interface IRibbonContent, IRibbonPage, IRibbonCategory, IRibbonGroup, IRibbonItem;  Enumy RibbonPageType, RibbonContentMode, RibbonItemStyles, BarItemPaintStyle, RibbonItemType.
    /// <summary>
    /// Kompletní deklarace dat Ribbonu, lze ji setovat do Ribbonu jedním řádkem
    /// </summary>
    public interface IRibbonContent
    {
        /// <summary>
        /// Text tlačítka BackStage (typicky "HELIOS")
        /// </summary>
        string ApplicationButtonText { get; }
        /// <summary>
        /// Explicitně přidané prvky do prostoru QAT (Quick Acces Toolbar), nad rámec prvků ze standardních stránek Ribbonu
        /// </summary>
        IRibbonItem[] QATDirectItems { get; }
        /// <summary>
        /// Prvky fixně umístěné vpravo v titulkovém řádku
        /// </summary>
        IRibbonItem[] TitleBarItems { get; }
        /// <summary>
        /// Souhrn standardních stránek Ribbonu
        /// </summary>
        IRibbonPage[] Pages { get; }
        /// <summary>
        /// Prvky umístěné dole ve stavovém řádku
        /// </summary>
        IRibbonItem[] StatusBarItems { get; }
    }
    /// <summary>
    /// Definice stránky v Ribbonu
    /// </summary>
    public interface IRibbonPage : IRibbonObject
    {
        /// <summary>
        /// Jméno Ribbonu
        /// </summary>
        string ParentRibbonName { get; set; }
        /// <summary>
        /// Kategorie, do které patří tato stránka. Může být null pro běžné stránky Ribbonu.
        /// </summary>
        IRibbonCategory Category { get; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci Ribbonu
        /// </summary>
        string PageId { get; }
        /// <summary>
        /// Pořadí stránky v rámci jednoho pole, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int PageOrder { get; set; }
        /// <summary>
        /// Pořadí stránky v rámci mergování, vkládá se do RibbonPage.
        /// Pokud při refreshi nechceme měnit hodnotu, zadejme -1.
        /// </summary>
        int MergeOrder { get; }
        /// <summary>
        /// Jméno stránky
        /// </summary>
        string PageText { get; }
        /// <summary>
        /// Viditelnost stránky
        /// </summary>
        bool Visible { get; }
        /// <summary>
        /// Typ stránky
        /// </summary>
        RibbonPageType PageType { get; }
        /// <summary>
        /// Režim práce se stránkou (opožděné načítání, refresh před každým načítáním)
        /// </summary>
        RibbonContentMode PageContentMode { get; }
        /// <summary>
        /// 
        /// </summary>
        IEnumerable<IRibbonGroup> Groups { get; }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonPage"/> po jejím vytvoření.
        /// </summary>
        WeakTarget<DxRibbonPage> RibbonPage { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Definice kategorie, do které patří stránka v Ribbonu
    /// </summary>
    public interface IRibbonCategory : IRibbonObject
    {
        /// <summary>
        /// Jméno Ribbonu
        /// </summary>
        string ParentRibbonName { get; set; }
        /// <summary>
        /// ID kategorie, jednoznačné per Ribbon
        /// </summary>
        string CategoryId { get; }
        /// <summary>
        /// Titulek kategorie zobrazovaný uživateli
        /// </summary>
        string CategoryText { get; }
        /// <summary>
        /// Barva kategorie
        /// </summary>
        Color? CategoryColor { get; }
        /// <summary>
        /// Kategorie je viditelná?
        /// </summary>
        bool CategoryVisible { get; }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonPageCategory"/> po jejím vytvoření.
        /// </summary>
        WeakTarget<DxRibbonPageCategory> RibbonCategory { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Definice skupiny ve stránce Ribbonu
    /// </summary>
    public interface IRibbonGroup : IRibbonObject
    {
        /// <summary>
        /// Parent prvku = <see cref="IRibbonPage"/>
        /// </summary>
        IRibbonPage ParentPage { get; set; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci stránky
        /// </summary>
        string GroupId { get; }
        /// <summary>
        /// Pořadí grupy v rámci jednoho pole, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int GroupOrder { get; set; }
        /// <summary>
        /// Pořadí grupy v rámci mergování, vkládá se do RibbonGroup.
        /// Pokud při refreshi nechceme měnit hodnotu, zadejme -1.
        /// </summary>
        int MergeOrder { get; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        string GroupText { get; }
        /// <summary>
        /// Viditelnost grupy
        /// </summary>
        bool Visible { get; }
        /// <summary>
        /// Obrázek grupy
        /// </summary>
        string GroupImageName { get; }
        /// <summary>
        /// Zobrazit speciální tlačítko grupy vpravo dole v titulku grupy (lze tak otevřít nějaké okno vlastností pro celou grupu)
        /// </summary>
        bool GroupButtonVisible { get; }
        /// <summary>
        /// Povolit zkrácení textu
        /// </summary>
        bool AllowTextClipping { get; }
        /// <summary>
        /// Stav grupy
        /// </summary>
        RibbonGroupState GroupState { get; }
        /// <summary>
        /// Rozložení prvků v grupě
        /// </summary>
        RibbonGroupItemsLayout LayoutType { get; }
        /// <summary>
        /// Soupis prvků grupy (tlačítka, menu, checkboxy, galerie)
        /// </summary>
        IEnumerable<IRibbonItem> Items { get; }
        /// <summary>
        /// Požadavek (true) na skrývání grupy, která neobsahuje žádné prvky.<br/>
        /// Default = false: pokud bude dodána prázdná grupa, bude zobrazena.
        /// </summary>
        bool HideEmptyGroup { get; }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonGroup"/> po jejím vytvoření.
        /// </summary>
        WeakTarget<DxRibbonGroup> RibbonGroup { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Rozšiřující rozhraní pro prvek ComboBox v Ribbonu.
    /// Používá se když aplikace chce deklarovat další vlastnosti ComboBoxu.
    /// Na základní vlastnosti postačuje <see cref="IRibbonItem"/>.
    /// </summary>
    public interface IRibbonComboItem : IRibbonItem
    {
        /// <summary>
        /// Šířka prvku v pixelech, null = nenastavovat
        /// </summary>
        int? Width { get; }
        /// <summary>
        /// Text zobrazený při nevybraném žádném prvku
        /// </summary>
        string NullValuePrompt { get; }
        /// <summary>
        /// Typ okraje celého ComboBoxu
        /// </summary>
        DxBorderStyle ComboBorderStyle { get; }
        /// <summary>
        /// Typ okraje jednotlivých sub-buttonů v ComboBoxu
        /// </summary>
        DxBorderStyle SubButtonsBorderStyle { get; }
        /// <summary>
        /// SubButtony zobrazené vedle prvku (podporuje je zatím jen typ <see cref="RibbonItemType.ComboListBox"/>).
        /// Typicky tlačítko "+", třitečky, Clear, Search, Delete, a libovolné další.<br/>
        /// String má formu: "Name=Image[;Name=Image[;...]]<br/>
        /// Kde Name = jméno buttonu, které se následně přidává ke jménu vlastního prvku (za tečku) po kliknutí na něj, když se volá aplikační server (MenuItemClick).
        /// Jako název Image lze použít buď hodnotu z enumu <see cref="PredefinedButtonType"/>, anebo standardní jméno ikony v rámci systému.<br/>
        /// Pořadí ve stringu definuje pořadí buttonů.
        /// Například <see cref="SubButtons"/> <![CDATA[Clear=Close;Manager=pic_0/Menu/bigfilter]]> definuje dva buttony, 
        /// první má význam "Zrušit" a vyvolá Command = "command.Clear", druhý má význam "Nabídni okno filtrů" (ikona pic_0/Menu/bigfilter) a vyvolá Command = "command.Manager"
        /// <para/>
        /// Pokud zde bude null, pak bude ComboBox obsahovat standardní DropDown button (šipka dolů, rozbalující ComboBox).<br/>
        /// Pokud uživatel chce přidat DropDown button a poté i další svoje Buttony, pak na začátek stringu vloží definici <u><c>"DropDown"</c></u> (netřeba definovat Image); a pak za středníkem dává svoje buttony.<br/>
        /// Pokud uživatel naplní definici a nebude v ní DropDown, pak prostě nebude zobrazen!
        /// </summary>
        string SubButtons { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public interface IRibbonItem : IMenuItem, IRibbonObject
    {
        /// <summary>
        /// Parent prvku = <see cref="IRibbonGroup"/>
        /// </summary>
        IRibbonGroup ParentGroup { get; set; }
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IRibbonItem"/>
        /// </summary>
        IRibbonItem ParentRibbonItem { get; set; }
        /// <summary>
        /// Režim pro vytvoření / refill / remove tohoto prvku
        /// </summary>
        new ContentChangeMode ChangeMode { get; }
        /// <summary>
        /// Typ prvku Ribbonu
        /// </summary>
        new RibbonItemType ItemType { get; }
        /// <summary>
        /// Jméno grupy, v jejímž rámci se buttony typu <see cref="RibbonItemType.RadioItem"/> a <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/> přetahují o stav <see cref="ITextItem.Checked"/>.
        /// Pokud je zde prázdný string, pak buttony typu <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/> fungují jako samostatný CheckBox.
        /// </summary>
        string RadioButtonGroupName { get; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        RibbonItemStyles RibbonStyle { get; }
        /// <summary>
        /// Počet sloupců (=počet ikon vedle sebe) v bloku "Rychlá volba", definovaný <see cref="ItemType"/> == <see cref="RibbonItemType.ButtonGroup"/>.
        /// Pokud je definována grupa, ale není dán počet buttonů, pak je vytvořena jako běžné SubMenu.
        /// </summary>
        int? ButtonGroupColumnCount { get; }
        /// <summary>
        /// Požadavek na přípravu ikony typu 'Disabled' i pro ten prvek Ribbonu, který má aktuálně hodnotu Enabled = true;
        /// <para/>
        /// Důvod: pokud budeme řídit přímo hodnotu BarItem.Enabled až po vytvoření BarItem, pak si tento BarItem sám řídí, který Image zobrazuje: zda standardní, nebo Disabled.
        /// <para/>
        /// V Nephrite může zůstat false, protože Nephrite mění hodnotu Enabled pomocí refreshe celého prvku, a po změně Enabled se vygeneruje správný Image automaticky.
        /// </summary>
        bool PrepareDisabledImage { get; }
        /// <summary>
        /// Režim práce s ImageList a Image
        /// </summary>
        DxImageListMode? ImageListMode { get; }
        /// <summary>
        /// Cílová velikost; využije se jen u některých prvků
        /// </summary>
        System.Drawing.Size? Size { get; }
        /// <summary>
        /// Barva pozadí; využije se jen u některých prvků, pokud není null
        /// </summary>
        System.Drawing.Color? BackColor { get; }
        /// <summary>
        /// Barva textu; využije se jen u některých prvků, pokud není null
        /// </summary>
        System.Drawing.Color? TextColor { get; }
        /// <summary>
        /// Kalíšek s barvami
        /// </summary>
        string StyleName { get; }
        /// <summary>
        /// Zarovnání prvku; uplatní se u StatusBaru
        /// </summary>
        BarItemAlignment? Alignment { get; }
        /// <summary>
        /// Doba (v milisekundách), která musí uplynout před opakovaným kliknutím na tento button. Rychlejší kliknutí (doubleclick) bude ignorováno.
        /// <para/>
        /// U běžných tlačítek nám opakované kliknutí nevadí (refresh, posun řádku nahoru/dolů, atd).<br/>
        /// Jsou ale tlačítka (funkce, workflow, ...), kde opakované vyvolání téže akce je problémem. 
        /// Pak se doporučuje nastavit dostatečnou kladnou hodnotu, která zajistí, že první kliknutí se dostane z klienta na server, zpracuje se a odešle na klienta refresh menu, kde zdrojový button např. již nebude přítomen.<br/>
        /// Hodnota null je default a umožní opakované odeslání akce na server bez časového omezení.
        /// </summary>
        int? DelayMilisecBeforeNextClick { get; }
        /// <summary>
        /// DateTime kdy bylo naposledy kliknuto. Řídí neaktivní interval spolu s hodnotou <see cref="DelayMilisecBeforeNextClick"/>.
        /// </summary>
        DateTime? DelayLastClickTime { get; set; }
        /// <summary>
        /// Prostor pro ID budíku, který řídí obnovení stavu Enabled pro odpovídající BarItem po uplynutí času <see cref="DelayMilisecBeforeNextClick"/> po akceptování kliknutí
        /// </summary>
        Guid? DelayTimerGuid { get; set; }
        /// <summary>
        /// Zobrazit v Search menu?
        /// </summary>
        bool VisibleInSearchMenu { get; }
        /// <summary>
        /// Explicitní klíč pro QAT. 
        /// Pokud bude null, použije se jako default <see cref="ITextItem.ItemId"/>.
        /// Pokud zde bude prázdný string, pak tento prvek do QAT nelze přidat.
        /// </summary>
        string QatKey { get; }
        /// <summary>
        /// Přidané texty, podle kterých může být prvek vyhledán v Search menu. Texty jsou oddělené čárkou (comma-separated).
        /// </summary>
        string SearchTags { get; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        RibbonContentMode SubItemsContentMode { get; }
        /// <summary>
        /// Subpoložky Ribbonu (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu
        /// </summary>
        new IEnumerable<IRibbonItem> SubItems { get; }
        /// <summary>
        /// Data pro RepositoryEditor.
        /// Musí být naplněna pro prvek typu <see cref="ItemType"/> == <see cref="RibbonItemType.RepositoryEditor"/>.
        /// Pro jiné prvky je ignorováno.
        /// </summary>
        IRepositoryEditorInfo RepositoryEditorInfo { get; }
        /// <summary>
        /// Sem bude umístěn fyzický BarItem po jeho vytvoření.
        /// </summary>
        BarItem RibbonItem { get; set; }
        /// <summary>
        /// Sem bude umístěno Popup menu po jeho vytvoření.
        /// </summary>
        PopupMenu PopupMenu { get; set; }
    }
    /// <summary>
    /// Rozšířená data pro typ prvku Ribbonu <see cref="RibbonItemType.RepositoryEditor"/>,
    /// jsou ukládána do <see cref="IRibbonItem.RepositoryEditorInfo"/>.
    /// </summary>
    public class DataRepositoryEditorInfo : IRepositoryEditorInfo
    {
        /// <summary>
        /// Funkce, která vygeneruje BarItem = součást Ribbonu.
        /// Pokud je null, je vytvořen standardní <see cref="DevExpress.XtraBars.BarEditItem"/>.
        /// </summary>
        public Func<IRibbonItem, BarEditItem> EditorCreator { get; set; }
        /// <summary>
        /// Funkce, která modifikuje BarItem = součást Ribbonu.
        /// </summary>
        public Action<IRibbonItem, BarEditItem> EditorModifier { get; set; }
        /// <summary>
        /// Funkce, která vygeneruje RepositoryItem = editor umístěný v BarItem.
        /// Pokud funkce není zadána nebo nic nevytvoří, pak se prvek do Ribbonu nepřidává.
        /// </summary>
        public Func<IRibbonItem, BarEditItem, DevExpress.XtraEditors.Repository.RepositoryItem> RepositoryCreator { get; set; }
        /// <summary>
        /// Typ a další informace pro editor
        /// </summary>
        public string RepositoryEditorType { get; set; }
    }
    /// <summary>
    /// Rozšířená data pro typ prvku Ribbonu <see cref="RibbonItemType.RepositoryEditor"/>,
    /// jsou ukládána do <see cref="IRibbonItem.RepositoryEditorInfo"/>.
    /// </summary>
    public interface IRepositoryEditorInfo
    {
        /// <summary>
        /// Funkce, která vygeneruje BarItem = součást Ribbonu.
        /// Pokud je null, pak se vytvoří standardní new instance.
        /// </summary>
        Func<IRibbonItem, BarEditItem> EditorCreator { get; }
        /// <summary>
        /// Funkce, která modifikuje BarItem = součást Ribbonu.
        /// Je volána po <see cref="EditorCreator"/>, před <see cref="RepositoryCreator"/>.
        /// </summary>
        Action<IRibbonItem, BarEditItem> EditorModifier { get; }
        /// <summary>
        /// Funkce, která vygeneruje RepositoryItem = editor umístěný v BarItem.
        /// Na vstupu je hostitelský BarItem, vytvořený v metodě <see cref="EditorCreator"/>, anebo standardní.
        /// </summary>
        Func<IRibbonItem, BarEditItem, DevExpress.XtraEditors.Repository.RepositoryItem> RepositoryCreator { get; }
        /// <summary>
        /// Typ a další informace pro editor
        /// </summary>
        string RepositoryEditorType { get; }
    }
    /// <summary>
    /// Společný interface pro prvek definice Ribbonu (Stránka - Kategorie - Grupa - Prvek - SubPrvek)
    /// </summary>
    public interface IRibbonObject
    {
        /// <summary>
        /// Režim pro vytvoření / refill / remove tohoto prvku
        /// </summary>
        ContentChangeMode ChangeMode { get; }
    }
    /// <summary>
    /// Interface, který zaručuje možnost provést <see cref="Reload"/> specifických hodnot prvku nad rámec běžného Refreshe pro obecný BarItem.
    /// </summary>
    public interface IReloadable
    {
        /// <summary>
        /// Znovu načte prvky z <see cref="IRibbonItem"/> nad rámec běžného Refreshe pro obecný BarItem.
        /// </summary>
        void Reload();
        /// <summary>
        /// Uloží do sebe daný definiční prvek a znovu vytvoří prvky ComboBoxu : Items a Buttons.
        /// Používá se pro Refresh obsahu.
        /// Nenaplní běžné hodnoty BarItemu (Caption, Image, SuperTip, Enabled atd), to provádí Ribbon tak jako u jiných BarItems.
        /// </summary>
        void ReloadFrom(IRibbonItem iRibbonItem);
    }
    /// <summary>
    /// Druh Borderu
    /// </summary>
    public enum DxBorderStyle
    {
        /// <summary>
        /// Žádný
        /// </summary>
        None,
        /// <summary>
        /// Žádný, jen pod myší se vykreslí
        /// </summary>
        HotFlat,
        /// <summary>
        /// Jedna tenká linka
        /// </summary>
        Single,
        /// <summary>
        /// 3D efekt
        /// </summary>
        Style3D
    }
    /// <summary>
    /// Typ buttonu v ComboBoxu.
    /// Odpovídá zčásti enumu <see cref="DevExpress.XtraEditors.Controls.ButtonPredefines"/>.
    /// </summary>
    [Flags]
    public enum PredefinedButtonType : int
    {
        /// <summary>
        /// Žádný Button
        /// </summary>
        None = 0,
        /// <summary>
        /// DropDown
        /// </summary>
        DropDown = 0x0001,
        /// <summary>
        /// TřiTečky
        /// </summary>
        Ellipsis = 0x0002,
        /// <summary>
        /// Delete křížek, červený
        /// </summary>
        Delete = 0x0004,
        /// <summary>
        /// Zavřít
        /// </summary>
        Close = 0x0008,
        /// <summary>
        /// Šipka doprava
        /// </summary>
        Right = 0x0010,
        /// <summary>
        /// Šipka doleva
        /// </summary>
        Left = 0x0020,
        /// <summary>
        /// Šipka nahoru
        /// </summary>
        Up = 0x0040,
        /// <summary>
        /// Šipka dolů
        /// </summary>
        Down = 0x0080,
        /// <summary>
        /// Ikona OK
        /// </summary>
        OK = 0x0100,
        /// <summary>
        /// Znaménko Plus
        /// </summary>
        Plus = 0x0200,
        /// <summary>
        /// Znaménko Mínus
        /// </summary>
        Minus = 0x0400,
        /// <summary>
        /// Undo
        /// </summary>
        Undo = 0x1000,
        /// <summary>
        /// Redo
        /// </summary>
        Redo = 0x2000,
        /// <summary>
        /// Lupa pro hledání
        /// </summary>
        Search = 0x4000,
        /// <summary>
        /// Smazání
        /// </summary>
        Clear = 0x8000,
        /// <summary>
        /// Uživatelský obrázek
        /// </summary>
        Glyph = 0x10000
    }
    /// <summary>
    /// Typ stránky
    /// </summary>
    public enum RibbonPageType
    {
        /// <summary>
        /// Defaultní stránka
        /// </summary>
        Default,
        /// <summary>
        /// Kontextová stránka
        /// </summary>
        Contextual
    }
    /// <summary>
    /// Stav grupy
    /// </summary>
    public enum RibbonGroupState
    {
        /// <summary>
        /// Auto
        /// </summary>
        Auto = 0,
        /// <summary>
        /// Expanded
        /// </summary>
        Expanded = 1,
        /// <summary>
        /// Collapsed
        /// </summary>
        Collapsed = 2
    }
    /// <summary>
    /// Specifies how small items are arranged within a DevExpress.XtraBars.Ribbon.RibbonPageGroup in the Office2007, Office2010 and Office2013 Ribbon styles.
    /// </summary>
    public enum RibbonGroupItemsLayout
    {
        /// <summary>
        /// The same as the DevExpress.XtraBars.Ribbon.RibbonPageGroupItemsLayout.ThreeRows option.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Small items are arranged in one row.
        /// </summary>
        OneRow = 1,
        /// <summary>
        /// Small items are arranged in two rows.
        /// </summary>
        TwoRows = 2,
        /// <summary>
        /// Small items are arranged in three rows.
        /// </summary>
        ThreeRows = 3
    }
    /// <summary>
    /// Způsob práce s prvky stránky / prvku při jeho aktivaci (Static / OnDemand)
    /// </summary>
    public enum RibbonContentMode
    {
        /// <summary>
        /// Stránka / prvek obsahuje již ve své definici seznam reálných prvků, není třeba je donačítat On-Demand při aktivaci.
        /// Pokud prvek má toto nastavení a nemá definované položky, pak je prostě nemá a mít nebude.
        /// </summary>
        Static,
        /// <summary>
        /// Stránka / prvek typicky neobsahuje definici podřízených prvků při inicializaci, ale bude se donačítat ze serveru až při své aktivaci.
        /// Po jejich načtení bude seznam konstantní (jde o odložené načtení fixního seznamu).
        /// <para/>
        /// V této položce <see cref="IRibbonPage"/> (v té, která deklaruje stránku s tímto režimem) se pak typicky naplní 
        /// <see cref="IRibbonItem.ItemType"/> = <see cref="RibbonItemType.None"/>, a nevznikne žádný vizuální prvek ani grupa.
        /// <para/>
        /// Po aktivaci takové stránky se provede dotaz na Aplikační server pro RefreshMenu a získané prvky se do této stránky doplní, 
        /// a následně se režim stránky přepne na <see cref="Static"/>.
        /// </summary>
        OnDemandLoadOnce,
        /// <summary>
        /// Stránka / prvek neobsahuje definici podřízených prvků při inicializaci, ale bude se při každé aktivaci stránky / prvku načítat ze serveru.
        /// Po jejich načtení bude seznam zobrazen, ale při další aktivaci stránky / prvku bude ze serveru načítán znovu.
        /// Jde o dynamický soupis prvků.
        /// <para/>
        /// V této položce <see cref="IRibbonPage"/> (v té, která deklaruje stránku s tímto režimem) se pak typicky naplní 
        /// <see cref="IRibbonItem.ItemType"/> = <see cref="RibbonItemType.None"/>, a nevznikne žádný vizuální prvek ani grupa.
        /// <para/>
        /// Po aktivaci takové stránky se provede dotaz na Aplikační server pro RefreshMenu a získané prvky se do této stránky doplní, 
        /// ale režim stránky nadále zůstává <see cref="OnDemandLoadEveryTime"/> = tím se zajistí stále opakované načítání obsahu při každé aktivaci.
        /// </summary>
        OnDemandLoadEveryTime
    }
    /// <summary>
    /// Styl zobrazení prvku Ribbonu (velikost, text).
    /// Lists the options that specify the bar item's possible states within a Ribbon Control.
    /// </summary>
    [Flags]
    public enum RibbonItemStyles
    {
        /// <summary>
        /// If active, an item's possible states with a Ribbon Control are determined based on the item's settings. 
        /// For example, if the item is associated with a small image and isn't associated with a large image, 
        /// its possible states within the Ribbon Control are DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithoutText 
        /// and DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText.
        /// </summary>
        Default = 0,
        /// <summary>
        /// If active, a bar item can be displayed as a large bar item.
        /// </summary>
        Large = 1,
        /// <summary>
        /// If active, an item can be displayed like a small bar item with its caption.
        /// </summary>
        SmallWithText = 2,
        /// <summary>
        /// If active, an item can be displayed like a small bar item without its caption.
        /// </summary>
        SmallWithoutText = 4,
        /// <summary>
        /// If active, enables all other options.
        /// </summary>
        All = 7
    }
    /// <summary>
    /// Defines the paint style for a specific item.
    /// </summary>
    public enum BarItemPaintStyle
    {
        /// <summary>
        /// Specifies that a specific item is represented using its default settings.
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Specifies that a specific item is represented by its caption only.
        /// </summary>
        Caption = 1,
        /// <summary>
        /// Specifies that a specific item is represented by its caption when it is in a submenu, or by its image when it is in a bar.
        /// </summary>
        CaptionInMenu = 2,
        /// <summary>
        /// Specifies that a specific item is represented both by its caption and the glyph image.
        /// </summary>
        CaptionGlyph = 3
    }
    /// <summary>
    /// Typ prvku ribbonu
    /// </summary>
    public enum RibbonItemType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Používá se ve StatusBaru
        /// </summary>
        Label,
        /// <summary>
        /// Používá se ve StatusBaru. Roztáhne se na celou šířku.
        /// </summary>
        LabelSpring,
        /// <summary>
        /// Vypadá jako Button ale chová se jako mrtvej brouk, nicméně se na něj dá kliknout
        /// </summary>
        Static,
        /// <summary>
        /// Vypadá jako Button ale chová se jako mrtvej brouk, nicméně se na něj dá kliknout. Roztáhne se na celou šířku.
        /// </summary>
        StaticSpring,
        /// <summary>
        /// Tlačítko
        /// </summary>
        Button,
        /// <summary>
        /// Button bez CheckBoxu, ale s možností jeho zvýraznění ve stylu "Button je stále zamáčknutý".<br/>
        /// Využívá tedy hodnotu v <see cref="ITextItem.Checked"/>.<br/>
        /// Pokud prvek Ribbonu <see cref="IRibbonItem"/> má tento typ prvku, a současně má určenou Radiogrupu <see cref="IRibbonItem.RadioButtonGroupName"/>, pak se toto označování chová jako RadioButton (prvky se vzájemně přetahují o stav .
        /// </summary>
        CheckButton,
        /// <summary>
        /// Button bez CheckBoxu, ale s možností jeho zvýraznění ve stylu "Button je stále zamáčknutý".<br/>
        /// Využívá tedy hodnotu v <see cref="ITextItem.Checked"/>.<br/>
        /// Pokud prvek Ribbonu <see cref="IRibbonItem"/> má tento typ prvku, a současně má určenou Radiogrupu <see cref="IRibbonItem.RadioButtonGroupName"/>, pak se toto označování chová jako RadioButton (prvky se vzájemně přetahují o stav .
        /// <para/>
        /// Passive = kliknutí na GUI prvek nezmění jeho vizuální ani datový stav Checked, ale odesílá se akce na server, 
        /// a server teprve rozhodne o změně Checked a může tedy poslat Refresh prvků (přebírá se ze stavu <see cref="ITextItem.Checked"/>).
        /// </summary>
        CheckButtonPassive,
        /// <summary>
        /// Skupina tlačítek, definovaná v Popup menu = více tlačítek vedle sebe bez popisku (bez textu), jako Rychlá volba.
        /// Počet tlačítek vedle sebe = <see cref="IRibbonItem.ButtonGroupColumnCount"/>.
        /// Velikost jednotlivých tlačítek je dána zdejší hodnotou <see cref="IRibbonItem.RibbonStyle"/> (Large / Small).
        /// </summary>
        ButtonGroup,
        /// <summary>
        /// Tlačítko plus menu
        /// </summary>
        SplitButton,
        /// <summary>
        /// Standardní CheckBox s automatickým přepínáním stavu Checked po kliknutí.
        /// </summary>
        CheckBoxStandard,
        /// <summary>
        /// Pasivní CheckBox, jehož stav Checked se nezmění po kliknutí na něj, ale pouze pokynem ze serveru v odpovědi Refresh 
        /// (přebírá se ze stavu <see cref="ITextItem.Checked"/>).
        /// </summary>
        CheckBoxPasive,
        /// <summary>
        /// Button se stavem Checked, který může být NULL (výchozí hodnota). 
        /// Pokud má být výchozí stav false, je třeba jej do <see cref="ITextItem.Checked"/> vložit!
        /// Lze specifikovat ikony pro všechny tři stavy (NULL - false - true)
        /// </summary>
        CheckBoxToggle,
        /// <summary>
        /// Prvek RadioGrupy.
        /// Měl by mít vyplněný název skupiny v <see cref="IRibbonItem.RadioButtonGroupName"/>, Ribbon dokáže sám přepínat aktivitu 1:N v rámci této skupiny.
        /// </summary>
        RadioItem,
        /// <summary>
        /// Trackbar, nepoužívat než bude dokončeno
        /// </summary>
        TrackBar,
        /// <summary>
        /// Menu = tlačítko, které se vždy rozbalí
        /// </summary>
        Menu,
        /// <summary>
        /// PopupMenu vytvořené ze SubItems. 
        /// Button s hodnotou ButtonStyle = DevExpress.XtraBars.BarButtonStyle.DropDown;
        /// a s DropDownControl = new DevExpress.XtraBars.PopupMenu(..)
        /// </summary>
        PopupMenuDropDown,
        /// <summary>
        /// Galerie v Ribbonu
        /// </summary>
        InRibbonGallery,
        /// <summary>
        /// DevExpress volba skinů
        /// </summary>
        SkinSetDropDown,
        /// <summary>
        /// DevExpress volba palety rozbalovací
        /// </summary>
        SkinPaletteDropDown,
        /// <summary>
        /// DevExpress volba palety v formě InRibbon galerie
        /// </summary>
        SkinPaletteGallery,
        /// <summary>
        /// Nabídka Zoomu = samotné menu, bez labelu.
        /// Jednotlivé nabízené položky lze nadefinovat buď kompletně jako <see cref="IRibbonItem.SubItems"/> tohoto prvku, 
        /// anebo lze do <see cref="ITextItem.Tag"/> vložit pole int[] obsahující jednotlivé položky Zommu, anebo do tohoto Tagu vložit jeden string s čísly oddělenými čárkami.
        /// Pokud nebude definováno nijak, použije se defaultní sada hodnot.
        /// </summary>
        ZoomPresetMenu,
        /// <summary>
        /// ComboListBox
        /// </summary>
        ComboListBox, 
        /// <summary>
        /// Speciální prvek s Repository editorem (TrackBar, TextBox, a další).
        /// Prvek musí mít naplněnou instanci <see cref="IRibbonItem.RepositoryEditorInfo"/>
        /// </summary>
        RepositoryEditor,
        /// <summary>
        /// Záhlaví (titulek) = výraznější než běžný řádek. Používá se pro Popup menu.
        /// </summary>
        Header
    }
    /// <summary>
    /// Zarovnání prvku v Ribbonu / StatusBaru
    /// </summary>
    public enum BarItemAlignment
    {
        /// <summary>
        /// Defaultní
        /// </summary>
        Default = 0,
        /// <summary>
        /// Vlevo
        /// </summary>
        Left = 1,
        /// <summary>
        /// Vpravo
        /// </summary>
        Right = 2
    }
    #endregion
}
