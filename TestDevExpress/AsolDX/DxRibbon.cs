// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Diagnostics;
using DevExpress.Utils.Extensions;
using System.Security.Policy;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.PivotGrid.CollapseState;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Potomek Ribbonu
    /// </summary>
    public class DxRibbonControl : DevExpress.XtraBars.Ribbon.RibbonControl, IDxRibbonInternal, IListenerApplicationIdle
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonControl()
        {
            InitSystemProperties();
            InitData();
            InitEvents();
            InitQuickAccessToolbar();
            DxComponent.RegisterListener(this);
            DxQuickAccessToolbar.QATItemKeysChanged += _DxQATItemKeysChanged;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnregisterListener(this);
            DxQuickAccessToolbar.QATItemKeysChanged -= _DxQATItemKeysChanged;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Nastaví základní systémové vlastnosti Ribbonu.
        /// </summary>
        private void InitSystemProperties()
        {
            Images = SystemAdapter.GetResourceImageList(RibbonImageSize);
            LargeImages = SystemAdapter.GetResourceImageList(RibbonLargeImageSize);

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
            ApplicationButtonText = DxComponent.LocalizeDef(MsgCode.RibbonAppHomeText, "DOMŮ");
            ToolTipController = DxComponent.DefaultToolTipController;

            Margin = new System.Windows.Forms.Padding(2);
            MdiMergeStyle = DevExpress.XtraBars.Ribbon.RibbonMdiMergeStyle.Always;

            AllowMinimizeRibbon = false;    // Povolit minimalizaci Ribbonu? Pak ale nejde vrátit :-(
            AllowCustomization = false;     // Hodnota true povoluje (na pravé myši) otevřít okno Customizace Ribbonu, a to v Greenu nepodporujeme
            ShowQatLocationSelector = true; // Hodnota true povoluje změnu umístění Ribbonu

            AllowGlyphSkinning = false;     // Nikdy ne true!
            ShowItemCaptionsInQAT = true;

            SelectChildActivePageOnMerge = true;
            UseLazyContentCreate = true;
            CheckLazyContentEnabled = true;

            _ImageHideOnMouse = true;       // Logo nekreslit, když v tom místě je myš

            Visible = true;
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

            ToolbarLocation = RibbonQuickAccessToolbarLocation.Above;
            ShowItemCaptionsInQAT = true;
            ShowQatLocationSelector = true;
            ShowToolbarCustomizeItem = true;
            Toolbar.ShowCustomizeItem = false;

            if (forDesktop)
            {   // pro Desktop
                MdiMergeStyle = RibbonMdiMergeStyle.Always;
                ShowSearchItem = true;
                ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            }
            else
            {   // pro ChildWindow
                MdiMergeStyle = RibbonMdiMergeStyle.Always;
                ShowSearchItem = true;
                ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            }
        }
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
            base.OnPaint(e);
            this.PaintAfter(e);
        }
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
        #region Obrázek vpravo
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
        /// Vykreslí ikonu vpravo
        /// </summary>
        /// <param name="e"></param>
        private void PaintAfter(PaintEventArgs e)
        {
            OnPaintImageRightBefore(e);

            bool isSmallRibbon = (this.CommandLayout == DevExpress.XtraBars.Ribbon.CommandLayout.Simplified);
            Image image = (isSmallRibbon ? (_ImageRightMini ?? _ImageRightFull) : (_ImageRightFull ?? _ImageRightMini));
            if (image != null && image.Width > 0 && image.Height > 0)
                PaintLogoImage(e, image, isSmallRibbon);
            else
                _ImageBounds = Rectangle.Empty;

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
                        this.SelectedPage = page;
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
            this.StoreLastSelectedPage();
            base.OnSelectedPageChanged(prev);
            if (!this.CurrentModifiedState) this.CheckLazyContent(this.SelectedPage, false);
        }
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
                DxRibbonPage[] nativePages = _GetNativePages(this.SelectedPage);
                nativePages.ForEachExec(p => p.OnActivate());
            }
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
                    if (page != null && !pages.Any(p => Object.ReferenceEquals(p, nativePage)))
                        pages.Add(nativePage);
                }
                else if (item.Tag is DxRibbonPage dxRibbonPage)
                {   // Pokud stránka je definovaná jako LazyLoad, pak má speciální grupu obsahující jeden BarItem, v jehož tagu je jeho nativní stránka DxRibbonPage:
                    if (page != null && !pages.Any(p => Object.ReferenceEquals(p, dxRibbonPage)))
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
        /// Metoda zkusí najít existující grupu, pouze vlastní v this ribbonu, ne v mergovaných.
        /// Hledá podle jejího ID <see cref="RibbonPageGroup.Name"/>, které by mělo být unikátní.
        /// Unikátnost přes celý Ribbon ale není striktně vyžadována, teoreticky smí být více grup se shodným ID, pokud jsou na různých stránkách.
        /// Doporučuje se ale používat ID unikátně přes celý Ribbon.
        /// <para/>
        /// Tato metoda vrací FirstOrDefault(). Pokud je třeba najít všechny grupy podle ID, použijte pole <see cref="Groups"/>.
        /// Tato metoda tedy nepoužívá Dictionary, ale prosté iterování pole.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="dxGroup"></param>
        /// <returns></returns>
        internal bool TryGetGroup(string groupId, out DxRibbonGroup dxGroup)
        {
            if (groupId == null)
            {
                dxGroup = null;
                return false;
            }
            return this._Groups.TryGetFirst(g => g.Name == groupId, out dxGroup);
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
                this._ClearItems(true, ref removeItemsCount);
            }
            finally
            {
                this.LastSelectedPageFullId = lastSelectedPageFullId;
                _ClearingNow = false;
            }

            if (LogActive) DxComponent.LogAddLineTime($" === ClearRibbon {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Korektně smaže BarItemy z this.Items.
        /// Ponechává tam systémové prvky a volitelně DirectQAT prvky!
        /// </summary>
        /// <param name="removeDirectQatItems"></param>
        /// <param name="removeItemsCount"></param>
        private void _ClearItems(bool removeDirectQatItems, ref int removeItemsCount)
        {
            // Pokud bych dal this.Items.Clear(), tak přijdu o všechny prvky celého Ribbonu,
            //   a to i o "servisní" = RibbonSearchEditItem, RibbonExpandCollapseItem, AutoHiddenPagesMenuItem.
            // Ale když nevyčistím Itemy, budou tady pořád strašit...
            // Ponecháme prvky těchto typů: "DevExpress.XtraBars.RibbonSearchEditItem", "DevExpress.XtraBars.InternalItems.RibbonExpandCollapseItem", "DevExpress.XtraBars.InternalItems.AutoHiddenPagesMenuItem"
            // Následující algoritmus NENÍ POMALÝ: smazání 700 Itemů trvá 11 milisekund.
            // Pokud by Clear náhodou smazal i nějaké další sytémové prvky, je nutno je určit = určit jejich FullType a přidat jej do metody _IsSystemItem() !
            var qatDirectKeys = _QATDirectAllItemKeys;
            bool hasQatDirectKeys = (qatDirectKeys != null && qatDirectKeys.Count > 0);
            int count = this.Items.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var item = this.Items[i];
                var key = GetValidQATKey(item.Name);
                bool isDelete = (!_IsSystemItem(item) && (removeDirectQatItems || (hasQatDirectKeys && !qatDirectKeys.ContainsKey(key))));
                if (isDelete)
                {
                    this.Items.RemoveAt(i);
                    removeItemsCount++;
                }
            }
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
            this._ClearItems(false, ref removeItemsCount);

            if (LogActive) DxComponent.LogAddLineTime($" === ClearPageContents {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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
            if (iRibbonPages == null) return;

            //  Hodnota 'isCalledFromReFill' je důležitá v následujícím scénáři:
            // 1. Provádíme první naplnění Ribbonu;
            // 2. Definice Ribbonu obsahuje na první pozici stránku typu OdDemandLoad: taková stránka se aktivuje v procesu přidávání stránek;
            // 3. Následně se metoda CheckLazyContentCurrentPage() má postarat o to, aby aktivní stránka (SelectedPage) měla správně načtená data (OnDemand)
            // 4. Musíme tady ale rozlišit dvě situace, a to:
            //   a) vstupující OnDemand stránka neobsahuje data => pokud bude aktivována, pak by MĚLA provést OnDemand donačtení,
            //       protože zdroj dat jen nadeklaroval OnDemnd stránku bez obsahu, ale uživatel chce vidět její obsah;
            //     anebo
            //   b) OnDemand stránka již OBSHAUJE data => pak ale metoda CheckLazyContentCurrentPage() nemá vyvolat OnDemand donačítání (protože bychom se zacyklili),
            //       to je situace, když zdroj dat právě už naplnil stránku daty a posílá ji do Ribbonu.
            //  Řešení:
            //   - příznak isReFill říká, že nyní přicházejí naplněná data (a nebude se tedy žádat o jejich další donačtení)
            //   - příznak isReFill určíme podle toho, že ve vstupních datech je alespoň jedna stránka typu OnDemand, která už v sobě obsahuje data

            bool isCalledFromReFill = iRibbonPages.Any(p => ((p.PageContentMode == RibbonContentMode.OnDemandLoadOnce || p.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime) && p.Groups.Any()));
            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    if (clearCurrentContent) _ClearPagesContents();
                    _AddPages(iRibbonPages, this.UseLazyContentCreate, false, "Fill");
                    if (clearCurrentContent) _RemoveVoidContainers();
                    // CheckLazyContentCurrentPage(isCalledFromReFill);
                }
                ,true);
            });
            CheckLazyContentCurrentPage(isCalledFromReFill);
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
                        _AddPageLazy(pageData, false, false, lazyInfo);
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
                        AddQATUserListToRibbon();
                    }
                }, true);
            }

            if (!removeLazyInfo && lazyGroup != null)
                lazyGroup.IsActive = true;

            if (lazyGroup.IsOnDemand && !isCalledFromReFill)
            {   // Oba režimy OnDemandLoad vyvolají patřičný event, pokud tato metoda NENÍ volána právě z akce naplnění ribbonu daty OnDemand:
                RunPageOnDemandLoad(iRibbonPage);
            }
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané kolekce, v daném režimu LazyLoad
        /// </summary>
        /// <param name="iRibbonPages"></param>
        /// <param name="isLazyContentFill">Obsahuje true, když se prvky typu Group a BarItem nemají fyzicky generovat, ale mají se jen registrovat do LazyGroup / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemandFill">Obsahuje true, pokud tyto položky jsou donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        /// <param name="logText"></param>
        private void _AddPages(IEnumerable<IRibbonPage> iRibbonPages, bool isLazyContentFill, bool isOnDemandFill, string logText)
        {
            if (iRibbonPages is null) return;

            var startTime = DxComponent.LogTimeCurrent;
            var list = DataRibbonPage.SortPages(iRibbonPages);
            int count = 0;
            foreach (var iRibbonPage in list)
                _AddPage(iRibbonPage, isLazyContentFill, isOnDemandFill, ref count);

            AddQATUserListToRibbon();

            if (LogActive) DxComponent.LogAddLineTime($" === Ribbon {DebugName}: {logText} {list.Count} item[s]; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané kolekce, v daném režimu LazyLoad
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="isLazyContentFill">Obsahuje true, když se prvky typu Group a BarItem nemají fyzicky generovat, ale mají se jen registrovat do LazyGroup / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemandFill">Obsahuje true, pokud tyto položky jsou donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        /// <param name="logText"></param>
        private void _AddPageLazy(IRibbonPage iRibbonPage, bool isLazyContentFill, bool isOnDemandFill, string logText)
        {
            if (iRibbonPage is null) return;

            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            _AddPage(iRibbonPage, isLazyContentFill, isOnDemandFill, ref count);

            if (LogActive) DxComponent.LogAddLineTime($" === Ribbon {DebugName}: {logText}; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Metoda přidá do this Ribbonu data další stránky.
        /// </summary>
        /// <param name="iRibbonPage">Deklarace stránky</param>
        /// <param name="isLazyContentFill">Obsahuje true, když se prvky typu Group a BarItem nemají fyzicky generovat, ale mají se jen registrovat do LazyGroup / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemandFill">Obsahuje true, pokud tyto položky jsou donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        /// <param name="count"></param>
        private void _AddPage(IRibbonPage iRibbonPage, bool isLazyContentFill, bool isOnDemandFill, ref int count)
        {
            if (iRibbonPage is null) return;

            var pageCategory = GetPageCategory(iRibbonPage.Category, iRibbonPage.ChangeMode);      // Pokud je to třeba, vygeneruje Kategorii
            RibbonPageCollection pages = (pageCategory != null ? pageCategory.Pages : this.Pages); // Kolekce stránek: kategorie / ribbon
            var page = GetPage(iRibbonPage, pages);                            // Najde / Vytvoří stránku do this.Pages nebo do category.Pages
            if (page is null) return;

            string fullId = GetPageFullId(page);
            if (this.SelectedPageFullId == fullId) isLazyContentFill = false;  // Pokud v Ribbonu je aktuálně vybraná ta stránka, která se nyní generuje, pak se NEBUDE plnit v režimu Lazy
            bool createContent = page.PreparePageForContent(iRibbonPage, isLazyContentFill, isOnDemandFill, out bool isStaticLazyContent);
            if (isStaticLazyContent)
                _ActiveLazyLoadOnIdle = true;                                  // Pokud tuhle stránku nebudu plnit (=nyní jen generujeme prázdnou stránku, anebo jen obsahující QAT prvky), tak si poznamenám, že budu chtít stránky naplnit ve stavu OnIdle

            // Problematika QAT je v detailu popsána v této metodě:
            bool createOnlyQATItems = !createContent && ContainsQAT(iRibbonPage);       // isNeedQAT je true tehdy, když bychom stránku nemuseli plnit (je LazyLoad), ale musíme do ní vložit pouze prvky typu QAT - a stránku přitom máme ponechat v režimu LazyLoad

            if (!createContent && !createOnlyQATItems) return;                 // víc už dělat nemusíme. Máme stránku a v ní LazyInfo, a prvky QAT nepotřebujeme (v dané stránce nejsou).

            if (!createOnlyQATItems && page.HasOnlyQatContent)
                page.ClearContent(true, false);
            
            page.HasOnlyQatContent = createOnlyQATItems;                       // Do stránky si poznamenáme, zda stránka obsahuje jen QAT prvky!

            var list = DataRibbonGroup.SortGroups(iRibbonPage.Groups);
            foreach (var iRibbonGroup in list)
            {
                iRibbonGroup.ParentPage = iRibbonPage;
                _AddGroup(iRibbonGroup, page, createOnlyQATItems, ref count);
            }
        }
        /// <summary>
        /// Metoda přidá danou grupu do dané stránky
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxPage"></param>
        /// <param name="createOnlyQATItems">Přidávat pouze prvky označené QAT (stránka jako celek je v režimu LazyContent, ale prvky QAT potřebujeme i v této stránce)</param>
        /// <param name="count"></param>
        private void _AddGroup(IRibbonGroup iRibbonGroup, DxRibbonPage dxPage, bool createOnlyQATItems, ref int count)
        {
            if (iRibbonGroup == null || dxPage == null) return;
            if (createOnlyQATItems && !ContainsQAT(iRibbonGroup)) return;      // V režimu isNeedQAT přidáváme jen prvky QAT, a ten v dané grupě není žádný

            var dxGroup = GetGroup(iRibbonGroup, dxPage);
            if (dxGroup is null) return;

            AddItemsToGroup(iRibbonGroup, dxGroup, createOnlyQATItems, ref count);
        }
        /// <summary>
        /// Přidá prvky do grupy
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxGroup"></param>
        /// <param name="createOnlyQATItems"></param>
        /// <param name="count"></param>
        private void AddItemsToGroup(IRibbonGroup iRibbonGroup, DxRibbonGroup dxGroup, bool createOnlyQATItems, ref int count)
        {
            var iRibbonItems = DataRibbonItem.SortRibbonItems(iRibbonGroup.Items);
            foreach (var iRibbonItem in iRibbonItems)
            {
                iRibbonItem.ParentGroup = iRibbonGroup;
                _AddBarItem(iRibbonItem, dxGroup, createOnlyQATItems, ref count);
            }
        }

        /// <summary>
        /// Metoda přidá daný prvek do dané grupy
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="createOnlyQATItems">Přidávat pouze prvky označené QAT (stránka jako celek je v režimu LazyContent, ale prvky QAT potřebujeme i v této stránce)</param>
        /// <param name="count"></param>
        private void _AddBarItem(IRibbonItem iRibbonItem, DxRibbonGroup dxGroup, bool createOnlyQATItems, ref int count)
        {
            if (iRibbonItem == null || dxGroup == null) return;
            if (createOnlyQATItems && !ContainsQAT(iRibbonItem)) return;       // V režimu createOnlyQATItems přidáváme jen prvky QAT, a ten v daném prvku není žádný

            GetItem(iRibbonItem, 0, dxGroup, createOnlyQATItems, ref count);      // Najde / Vytvoří / Naplní prvek
        }
        #endregion
        #region Refresh obsahu Ribbonu
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
            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    _RefreshPages(iRibbonPages);
                }, true);
            });
        }
        /// <summary>
        /// Provede refresh dodaných stránek. Provede se jedním chodem.
        /// Ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonPages"></param>
        private void _RefreshPages(IEnumerable<IRibbonPage> iRibbonPages)
        {
            var reFillPages = _PrepareReFill(iRibbonPages, true);
            _AddPages(iRibbonPages, this.UseLazyContentCreate, true, "OnDemand");
            _RemoveVoidContainers(reFillPages);
            CheckLazyContentCurrentPage(true);
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

            _UnMergeModifyMergeCurrentRibbon(() => _RefreshGroups(iRibbonGroups), true);
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
            _UnMergeModifyMergeCurrentRibbon(() => _RefreshGroup(iRibbonGroup, dxGroup, dxPage), true);
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
            if (!NeedRefreshItems(iRibbonItems)) return;             // Zkratka

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
        /// <param name="iRibbonItem"></param>
        public void RefreshItem(IRibbonItem iRibbonItem)
        {
            if (iRibbonItem == null) return;
            if (!NeedRefreshItem(iRibbonItem)) return;               // Zkratka

            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    RefreshCurrentItem(iRibbonItem);
                }, true);
                DoOpenMenu();
            });
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
        /// Provede refresh dodaných objektů. Provede se jedním chodem.
        /// </summary>
        /// <param name="iRibbonObjects"></param>
        /// <param name="openMenuItemId"></param>
        public void RefreshObjects(IEnumerable<IRibbonObject> iRibbonObjects, string openMenuItemId = null)
        {
            if (iRibbonObjects == null) return;

            // Vstupní data roztřídím na: Page - Group - Items, a pak půjdu standardními postupy:
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
            if (iRibbonPages.Count == 0 && iRibbonGroups.Count == 0 && iRibbonItems.Count == 0) return;
            if (iRibbonPages.Count == 0 && iRibbonGroups.Count == 0 && !NeedRefreshItems(iRibbonItems)) return;             // Zkratka

            // Máme data, půjdeme dělat něco viditelného:
            this.ParentOwner.RunInGui(() =>
            {
                _UnMergeModifyMergeCurrentRibbon(() =>
                {
                    _RefreshObjects(iRibbonPages, iRibbonGroups, iRibbonItems);
                }, true);
                DoOpenMenu();
            });
        }
        /// <summary>
        /// Provede refresh dodaných objektů. Provede se jedním chodem.
        /// Ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonPages"></param>
        /// <param name="iRibbonGroups"></param>
        /// <param name="iRibbonItems"></param>
        private void _RefreshObjects(List<IRibbonPage> iRibbonPages, List<IRibbonGroup> iRibbonGroups, List<IRibbonItem> iRibbonItems)
        {
            if (iRibbonPages.Count > 0) _RefreshPages(iRibbonPages);
            if (iRibbonGroups.Count > 0) _RefreshGroups(iRibbonGroups);
            if (iRibbonItems.Count > 0) _RefreshItems(iRibbonItems);
        }
        /// <summary>
        /// Refresh sady skupin, data fyzické grupy jsou dodána, ribbon je unmergován.
        /// </summary>
        /// <param name="iRibbonGroups"></param>
        private void _RefreshGroups(IEnumerable<IRibbonGroup> iRibbonGroups)
        {
            var startTime = DxComponent.LogTimeCurrent;
            int groupCount = 0;
            int itemCount = 0;
            foreach (var iRibbonGroup in iRibbonGroups)
            {
                if (iRibbonGroup is null) continue;

                // Najdeme fyzická data pro provedení refreshe aktuální grupy, a pokud je to OK, pak provedeme Refresh:
                if (_TryGetDataForRefreshGroup(iRibbonGroup, out var dxGroup, out var dxPage))
                    _RefreshGroup(iRibbonGroup, dxGroup, dxPage);
            }
            if (LogActive) DxComponent.LogAddLineTime($" === Ribbon {DebugName}: Refresh groups '{groupCount}', Items count: {itemCount}; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Metoda zkusí najít fyzickou grupu pro refresh, a pokud ji nenajde, pak zkusí najít stránku pro vložení nové grupy (pokud je to zapotřebí).
        /// Nalezené prvky vkládá do out parametrů.
        /// Vrací true = je třeba provést nějaký Refresh (ten zajistí metoda <see cref="_RefreshGroup(IRibbonGroup, DxRibbonGroup, DxRibbonPage)"/>),
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
            bool hasGroup = TryGetGroup(iRibbonGroup.GroupId, out dxGroup);
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
        private void _RefreshGroup(IRibbonGroup iRibbonGroup, DxRibbonGroup dxGroup, DxRibbonPage dxPage)
        {
            var startTime = DxComponent.LogTimeCurrent;

            int count = 0;
            ReloadGroup(iRibbonGroup, dxGroup, dxPage, ref count);
            AddQATUserListToRibbon();

            if (LogActive) DxComponent.LogAddLineTime($" === Ribbon {DebugName}: Refresh group '{iRibbonGroup.GroupId}', Items count: {count}; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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
        /// To je tehdy, 
        /// </summary>
        /// <param name="iRibbonItems"></param>
        /// <returns></returns>
        private bool NeedRefreshItems(IEnumerable<IRibbonItem> iRibbonItems)
        {
            if (iRibbonItems == null) return false;
            return iRibbonItems.Any(i => NeedRefreshItem(i));        // Jakýkoli jeden prvek, který potřebuje Refresh, zajistí vrácení true = Refresh je nutný.
        }
        /// <summary>
        /// Vrátí true, pokud dodaný prvek reálně potřebuje provést Refresh. Tj. obsahuje jiná data, než jaká máme v GUI kolekci.
        /// To je tehdy, 
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private bool NeedRefreshItem(IRibbonItem iRibbonItem)
        {
            if (iRibbonItem == null) return false;
            var changeMode = iRibbonItem.ChangeMode;
            bool needExists = (changeMode == ContentChangeMode.Add || changeMode == ContentChangeMode.ReFill);
            if (TryGetIRibbonData(iRibbonItem.ItemId, out var iCurrentItem))
            {   // Prvek existuje: Refresh je třeba, pokud prvek nemá existovat, anebo když aktuálně obsahuje jiná data než je nyní požadováno:
                if (!needExists) return true;
                if (DataRibbonItem.HasEqualContent(iRibbonItem, iCurrentItem)) return false;       
                return true;                                         // Výstup je true (Refresh) tehdy, když dva prvky (nově požadovaný a stávající) NEJSOU shodné!   Takhle je tu podmínka proto, abych na Equals mohl dát breakpoint...
            }
            else
            {   // Prvek neexistuje: Refresh je třeba, pokud se prvek má vytvořit nebo upravit:
                return needExists;
            }
        }
        #endregion
        #region LazyLoad page content : OnSelectedPageChanged => CheckLazyContent; OnDemand loading Page
        /// <summary>
        /// Požadavek na používání opožděné tvorby obsahu stránek Ribbonu.
        /// Pokud bude true, pak jednotlivé prvky na stránce Ribbonu budou fyzicky vygenerovány až tehdy, až bude stránka vybrána k zobrazení.
        /// Změna hodnoty se projeví až při následujícím přidávání prvků do Ribbonu. 
        /// Pokud tedy byla hodnota false (=výchozí stav), pak se přidá 600 prvků, a teprve pak se nastaví <see cref="UseLazyContentCreate"/> = true, je to fakt pozdě.
        /// </summary>
        public bool UseLazyContentCreate { get; set; }
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
                lazyDxPage.PrepareRealLazyItems(isCalledFromReFill);           // Tato metoda převolá zdejší metodu : IDxRibbonInternal.PrepareRealLazyItems()
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
        /// Po nějakém čase - když aplikace získá data, pak zavolá zdejší metodu <see cref="DxRibbonControl.RefreshItem(IRibbonItem, bool)"/>;
        /// tato metoda zajistí zobrazení nově dodaných dat v daném prvku.
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonItem>> ItemOnDemandLoad;
        #endregion
        #region LazyLoadOnIdle : dovolujeme provést opožděné plnění stránek (typu LazyLoad: Static) v režimu Application.OnIdle = "když je vhodná chvíle"
        /// <summary>
        /// Aplikace má volný čas, Ribbon by si mohl vygenerovat LazyLoad Static pages, pokud takové má.
        /// </summary>
        private void _ApplicationIdle()
        {
            if (_ActiveLazyLoadOnIdle)
                _PrepareLazyLoadStaticPages();
        }
        /// <summary>
        /// Tato metoda je volána v situaci, kdy GUI thread má nějaký volný čas (ApplicationIdle) 
        /// a this Ribbon má nastavený příznak <see cref="_ActiveLazyLoadOnIdle"/> == true;
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
                    AddQATUserListToRibbon();
                }, true);

                if (LogActive) DxComponent.LogAddLineTime($" === Ribbon {DebugName}: CreateLazyStaticPages; Create: {pageCount} Pages; {itemCount} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
            }
            _ActiveLazyLoadOnIdle = false;
        }
        private void _PrepareLazyLoadStaticPage(DxRibbonPage lazyPage, ref int itemCount)
        {
            IRibbonPage iRibbonPage = lazyPage.PageData;
            _AddPage(iRibbonPage, false, false, ref itemCount);   // Existující Page se ponechá, obsah se nahradí, naplní se reálné prvky
            // Ještě jedna věc je LazyLoad, a to jsou submenu a splitbuttons atd.
            // To ale tady zatím neřeším.
            // Důsledek: Rychlé hledání v Ribbonu nenajde subpoložky menu.
        }
        /// <summary>
        /// Hodnota true říká, že this Ribon má nějaké Pages ve stavu LazyLoad se statickým obsahem.
        /// Pak v době, kdy systém má volný čas (když je volána metoda <see cref="IListenerApplicationIdle.ApplicationIdle()"/>) 
        /// si Ribbon fyzicky vygeneruje reálný obsah daných Pages.
        /// </summary>
        private bool _ActiveLazyLoadOnIdle;
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

            IRibbonPage iRibbonPage = page?.PageData;
            if (page != null) page.PageData = null;
            if (iRibbonPage != null) iRibbonPage.RibbonPage = null;
        }

        /// <summary>
        /// Vytvoří a vrátí grupu daného jména, grupu vloží do dané stránky.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public DxRibbonGroup CreateGroup(string text, DxRibbonPage page)
        {
            var dxGroup = new DxRibbonGroup(text)
            {
                State = DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto
            };
            if (page != null) page.Groups.Add(dxGroup);
            return dxGroup;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a naplní grupu pro daná data.
        /// Grupu přidá do dané stránky, pokud tam dosud není.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxGroup"></param>
        /// <param name="dxPage"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected void ReloadGroup(IRibbonGroup iRibbonGroup, DxRibbonGroup dxGroup, DxRibbonPage dxPage, ref int count)
        {
            var changeMode = iRibbonGroup.ChangeMode;
            bool isRemove = (changeMode == ContentChangeMode.Remove);
            if (dxGroup == null)
            {
                if (isRemove) return;
                if (dxPage == null) return;
                dxGroup = CreateGroup(iRibbonGroup, dxPage);
            }
            else if (isRemove)
            {
                RemoveGroup(dxGroup, dxPage);
                return;
            }

            FillGroup(dxGroup, iRibbonGroup);
            if (changeMode == ContentChangeMode.ReFill)
                dxGroup.ClearContent();

            AddItemsToGroup(iRibbonGroup, dxGroup, false, ref count);
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí grupu pro daná data.
        /// Grupu přidá do dané stránky.
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
                if (HasReFill(changeMode))
                    ClearGroup(dxGroup);
                FillGroup(dxGroup, iRibbonGroup);
            }
            else if (HasRemove(changeMode))
            {
                RemoveGroup(dxGroup, dxPage);
                dxGroup = null;
            }

            return dxGroup;
        }
        /// <summary>
        /// Vytvoří a vrátí grupu
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        protected DxRibbonGroup CreateGroup(IRibbonGroup iRibbonGroup, DxRibbonPage dxPage)
        {
            var dxGroup = new DxRibbonGroup(iRibbonGroup.GroupText)
            {
                Name = iRibbonGroup.GroupId,
            };
            if (dxPage != null) dxPage.Groups.Add(dxGroup);
            this._Groups.Add(dxGroup);
            return dxGroup;
        }
        /// <summary>
        /// Naplní vlastnosti grupy z definice - ale nevkládá do grupy jednotlivé prvky.
        /// </summary>
        /// <param name="dxGroup"></param>
        /// <param name="iRibbonGroup"></param>
        protected void FillGroup(DxRibbonGroup dxGroup, IRibbonGroup iRibbonGroup)
        {
            dxGroup.Text = iRibbonGroup.GroupText;
            dxGroup.Visible = iRibbonGroup.Visible;
            if (iRibbonGroup.MergeOrder > 0) dxGroup.MergeOrder = iRibbonGroup.MergeOrder;             // Záporné číslo IRibbonGroup.MergeOrder říká: neměnit hodnotu, pokud grupa existuje. Důvod: při Refreshi existující grupy nechceme měnit její pozici.
            dxGroup.CaptionButtonVisible = (iRibbonGroup.GroupButtonVisible ? DefaultBoolean.True : DefaultBoolean.False);
            dxGroup.AllowTextClipping = iRibbonGroup.AllowTextClipping;
            dxGroup.State = (iRibbonGroup.GroupState == RibbonGroupState.Expanded ? DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Expanded :
                            (iRibbonGroup.GroupState == RibbonGroupState.Collapsed ? DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Collapsed :
                             DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto));
            dxGroup.ItemsLayout = (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.Default ? RibbonPageGroupItemsLayout.Default :
                                  (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.OneRow ? RibbonPageGroupItemsLayout.OneRow :
                                  (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.TwoRows ? RibbonPageGroupItemsLayout.TwoRows :
                                  (iRibbonGroup.LayoutType == RibbonGroupItemsLayout.ThreeRows ? RibbonPageGroupItemsLayout.ThreeRows :
                                   RibbonPageGroupItemsLayout.Default))));
            dxGroup.ImageOptions.ImageIndex = SystemAdapter.GetResourceIndex(iRibbonGroup.GroupImageName, RibbonImageSize, iRibbonGroup.GroupText);
            dxGroup.DataGroup = iRibbonGroup;
            dxGroup.Tag = iRibbonGroup;
        }
        /// <summary>
        /// Smaže obsah grupy
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        protected void ClearGroup(DxRibbonGroup group)
        {
            group?.ClearContent();
        }
        /// <summary>
        /// Odstraní obsah grupy a pak odstraní grupu ze stránky
        /// </summary>
        /// <param name="dxGroup"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        protected void RemoveGroup(DxRibbonGroup dxGroup, DxRibbonPage dxPage)
        {
            if (dxPage == null && dxGroup != null) dxPage = dxGroup.OwnerDxPage;

            if (dxGroup != null && dxPage != null && dxPage.Groups.Contains(dxGroup))
            {
                dxGroup.ClearContent();
                dxPage.Groups.Remove(dxGroup);
                this._Groups.Remove(dxGroup);
            }

            IRibbonGroup iRibbonGroup = dxGroup.DataGroup;
            if (dxGroup != null) dxGroup.DataGroup = null;
            if (iRibbonGroup != null) iRibbonGroup.RibbonGroup = null;
        }

        /// <summary>
        /// Vytvoří a vrátí prvek dle definice.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.BarItem CreateItem(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            int count = 0;
            var barItem = CreateItem(iRibbonItem, level, dxGroup, true, ref count);

            if (barItem is null) return null;
            if (dxGroup != null)
            {
                var barLink = dxGroup.ItemLinks.Add(barItem);
                barLink.BeginGroup = iRibbonItem.ItemIsFirstInGroup;
            }
            FillBarItem(barItem, iRibbonItem);

            if (clickHandler != null) barItem.ItemClick += clickHandler;

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
            var item = this.Items[itemId];
            if (item == null) return;
            var itemInfo = item.Tag as BarItemTagInfo;
            if (itemInfo == null) return;

            FillBarItem(item, iRibbonItem);

            if (iRibbonItem.SubItems != null)
            {
                switch (itemInfo.Data.ItemType)
                {
                    case RibbonItemType.SplitButton:
                        if (item is DevExpress.XtraBars.BarButtonItem splitButton)
                            // SplitButton má v sobě vytvořené PopupMenu, které nyní obsahuje jen jeden prvek; zajistíme vložení reálného menu:
                            _PopupMenu_RefreshItems(splitButton, itemInfo.Data, iRibbonItem);
                        break;
                    case RibbonItemType.Menu:
                        if (item is DevExpress.XtraBars.BarSubItem menu)
                            _BarMenu_RefreshItems(menu, itemInfo.Data, iRibbonItem);
                        break;
                }
            }
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí BarItem pro daná data.
        /// BarItem přidá do dané grupy.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="level"></param>
        /// <param name="dxGroup"></param>
        /// <param name="createOnlyQATItems">Přidávat pouze prvky označené QAT (stránka jako celek je v režimu LazyContent, ale prvky QAT potřebujeme i v této stránce)</param>
        /// <param name="count"></param>
        /// <param name="reallyCreateSubItems"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem GetItem(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, bool createOnlyQATItems, ref int count, bool reallyCreateSubItems = false)
        {
            if (iRibbonItem is null || dxGroup is null) return null;

            var changeMode = ((IRibbonObject)iRibbonItem).ChangeMode;
            DevExpress.XtraBars.BarItem barItem = Items[iRibbonItem.ItemId];
            if (HasCreate(changeMode))
            {
                if (HasReFill(changeMode) && barItem != null)
                { }
                if (barItem is null)
                    barItem = CreateItem(iRibbonItem, level, dxGroup, reallyCreateSubItems, ref count);
                if (HasReFill(changeMode))
                {
                    //  ClearItem(barItem);
                }
               
                if (barItem is null) return null;
                var barLink = dxGroup.ItemLinks.Add(barItem);
                barLink.BeginGroup = iRibbonItem.ItemIsFirstInGroup;
                PrepareBarItemTag(barItem, iRibbonItem, level, dxGroup);
                FillBarItem(barItem, iRibbonItem);
            }
            else if (HasRemove(changeMode))
            {

            }

            // Prvek patří do QAT?
            if (DefinedInQAT(iRibbonItem.ItemId))
                this.AddBarItemToQATUserList(barItem, iRibbonItem);

            return barItem;
        }
        /// <summary>
        /// Vytvoří prvek BarItem pro daná data a vrátí jej.
        /// Tato metoda NEVKLÁDÁ vytovřený prvek do dodané grupy; grupa smí být null.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="dxGroup"></param>
        /// <param name="level"></param>
        /// <param name="reallyCreateSubItems">Skutečně se mají vytvářet SubMenu?</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem CreateItem(IRibbonItem iRibbonItem, int level, DxRibbonGroup dxGroup, bool reallyCreateSubItems, ref int count)
        {
            DevExpress.XtraBars.BarItem barItem = null;
            switch (iRibbonItem.ItemType)
            {
                case RibbonItemType.ButtonGroup:
                    count++;
                    DevExpress.XtraBars.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, reallyCreateSubItems, ref count));
                    buttonGroup.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    count++;
                    var dxPopup = CreatePopupMenu(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, reallyCreateSubItems, ref count);
                    DevExpress.XtraBars.BarButtonItem splitButton = Items.CreateSplitButton(iRibbonItem.Text, dxPopup);
                    barItem = splitButton;
                    break;
                case RibbonItemType.Menu:
                    count++;
                    DevExpress.XtraBars.BarSubItem menu = Items.CreateMenu(iRibbonItem.Text);
                    PrepareBarMenu(iRibbonItem, level, dxGroup, iRibbonItem.SubItems, menu, reallyCreateSubItems, ref count);
                    barItem = menu;
                    break;
                case RibbonItemType.CheckBoxStandard:
                    count++;
                    DevExpress.XtraBars.BarCheckItem checkItem = Items.CreateCheckItem(iRibbonItem.Text, iRibbonItem.Checked ?? false);
                    checkItem.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
                    barItem = checkItem;
                    break;
                case RibbonItemType.CheckBoxToggle:
                    count++;
                    DxBarCheckBoxToggle toggleSwitch = new DxBarCheckBoxToggle(this.BarManager, iRibbonItem.Text);
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
                case RibbonItemType.InRibbonGallery:
                    count++;
                    var galleryItem = CreateGalleryItem(iRibbonItem, level, dxGroup);
                    barItem = galleryItem;
                    break;
                case RibbonItemType.SkinSetDropDown:
                    count++;
                    barItem = new DevExpress.XtraBars.SkinDropDownButtonItem();
                    break;
                case RibbonItemType.SkinPaletteDropDown:
                    count++;
                    barItem = new DevExpress.XtraBars.SkinPaletteDropDownButtonItem();
                    break;
                case RibbonItemType.SkinPaletteGallery:
                    count++;
                    barItem = new DevExpress.XtraBars.SkinPaletteRibbonGalleryBarItem();
                    break;
                case RibbonItemType.Button:
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
                // FillBarItem(barItem, iRibbonItem);
            }
            return barItem;
        }
        /// <summary>
        /// Do daného prvku vepíše data z definice.
        /// Aktualizuje i obsah BarItem.Tag
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="withReset"></param>
        protected void FillBarItem(DevExpress.XtraBars.BarItem barItem, IRibbonItem iRibbonItem, bool withReset = false)
        {
            if (iRibbonItem.Text != null || withReset)
                barItem.Caption = iRibbonItem.Text ?? "";

            barItem.Enabled = iRibbonItem.Enabled;
            barItem.Visibility = iRibbonItem.Visible ? BarItemVisibility.Always : BarItemVisibility.Never;
            barItem.VisibleInSearchMenu = iRibbonItem.VisibleInSearchMenu;
            FillBarItemImage(barItem, iRibbonItem, withReset);
            FillBarItemHotKey(barItem, iRibbonItem, withReset);

            if (barItem is DevExpress.XtraBars.BarCheckItem checkItem)
            {   // Do CheckBoxu vepisujeme víc vlastností:
                checkItem.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
                checkItem.CheckStyle =
                    (iRibbonItem.ItemType == RibbonItemType.RadioItem ? DevExpress.XtraBars.BarCheckStyles.Radio :
                    (iRibbonItem.ItemType == RibbonItemType.CheckBoxToggle ? DevExpress.XtraBars.BarCheckStyles.Standard :
                     DevExpress.XtraBars.BarCheckStyles.Standard));
                checkItem.Checked = iRibbonItem.Checked ?? false;
            }

            if (barItem is DxBarCheckBoxToggle dxCheckBoxToggle)
            {
                dxCheckBoxToggle.CheckedSilent = iRibbonItem.Checked;
                if (iRibbonItem.ImageName != null) dxCheckBoxToggle.ImageNameNull = iRibbonItem.ImageName;
                if (iRibbonItem.ImageNameUnChecked != null) dxCheckBoxToggle.ImageNameUnChecked = iRibbonItem.ImageNameUnChecked;
                if (iRibbonItem.ImageNameChecked != null) dxCheckBoxToggle.ImageNameChecked = iRibbonItem.ImageNameChecked;
            }

            barItem.PaintStyle = Convert(iRibbonItem.ItemPaintStyle);
            if (iRibbonItem.RibbonStyle != RibbonItemStyles.Default)
                barItem.RibbonStyle = Convert(iRibbonItem.RibbonStyle);

            if (iRibbonItem.ToolTipText != null || withReset)
                barItem.SuperTip = DxComponent.CreateDxSuperTip(iRibbonItem);

            RefreshBarItemTag(barItem, iRibbonItem);
        }
        /// <summary>
        /// Do daného prvku Ribbonu vepíše vše pro jeho Image
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="withReset"></param>
        protected void FillBarItemImage(DevExpress.XtraBars.BarItem barItem, IRibbonItem iRibbonItem, bool withReset = false)
        {
            var image = iRibbonItem.Image;
            string imageName = iRibbonItem.ImageName;
            if (image != null)
            {
                barItem.ImageOptions.Image = image;
                barItem.ImageOptions.LargeImage = image;
            }
            else if (imageName != null && !(barItem is DxBarCheckBoxToggle))           // DxCheckBoxToggle si řídí Image sám
            {
                if (DxComponent.TryGetResourceExtension(imageName, out var _))
                {
                    DxComponent.ApplyImage(barItem.ImageOptions, resourceName: imageName);
                }
                else
                {
                    barItem.ImageIndex = SystemAdapter.GetResourceIndex(imageName, RibbonImageSize, iRibbonItem.Text);
                    barItem.LargeImageIndex = SystemAdapter.GetResourceIndex(imageName, RibbonLargeImageSize, iRibbonItem.Text);
                }
            }
        }
        /// <summary>
        /// Do daného prvku Ribbonu vepíše vše pro jeho HotKey
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="withReset"></param>
        protected void FillBarItemHotKey(DevExpress.XtraBars.BarItem barItem, IRibbonItem iRibbonItem, bool withReset = false)
        {
            if (iRibbonItem.HotKeys.HasValue)
            {
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(iRibbonItem.HotKeys.Value);
            }
            else if (iRibbonItem.Shortcut.HasValue)
            {
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(iRibbonItem.Shortcut.Value);
            }
            else if (!string.IsNullOrEmpty(iRibbonItem.HotKey))
            {
                if (!(barItem is DevExpress.XtraBars.BarSubItem))
                    barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(SystemAdapter.GetShortcutKeys(iRibbonItem.HotKey));
            }
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
                    DevExpress.XtraBars.BarCheckItem checkItem = new DevExpress.XtraBars.BarCheckItem(this.Manager);
                    baseButton = checkItem;
                    break;
            }
            if (baseButton != null)
            {
                baseButton.Name = iRibbonItem.ItemId;
                PrepareBarItemTag(baseButton, iRibbonItem, level, dxGroup);
                FillBarItem(baseButton, iRibbonItem);
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
            DevExpress.XtraBars.PopupMenu dxPopupMenu = new DevExpress.XtraBars.PopupMenu(BarManager);

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
            {   // Události řeším je pro menu základní úrovně (=na tlačítku), ne pro submenu:
                dxPopupMenu.BeforePopup += _PopupMenu_BeforePopup;
                dxPopupMenu.CloseUp += _PopupMenu_CloseUp;

                // Pokud právě nyní generujeme PopupMenu, jehož ItemId se shoduje s prvkem menu, který je připraven ke znovuotevření v proměnné _OpenItemPopupMenu (identifikátor _OpenItemPopupMenuItemId je shodný s aktuálním menu),
                //   pak se právě nyní provádí velký refresh (stránky / grupy), který zahodil dosavadní staré PopupMenu a nyní se vygeneroval nový PopupMenu.
                // Znamená to, že si do proměnné _OpenItemPopupMenu musíme uložit novou instanci (menu), protože ji zanedlouho budeme otevírat! Stará instance, uložená v _OpenItemPopupMenu, je k nepoužití, protože je odebraná z Ribbonu...
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
                var lazySubItems = itemInfo.LazyInfo;
                // Mohou nastat dvě situace:
                bool cancel = true;                                       // Zobrazení menu možná potlačíme, pokud nebudeme mít k dispozici stávající definované prvky
                if (lazySubItems.SubItems != null)
                {   // 1. SubItems jsou deklarované přímo zde (Static), pak z nich vytvoříme nabídku a necháme ji uživateli zobrazit:
                    var startTime = DxComponent.LogTimeCurrent;
                    int count = 0;
                    _PopupMenu_FillItems(dxPopup, level + 1, itemInfo.DxGroup, lazySubItems.ParentItem, lazySubItems.SubItems, true, ref count);
                    if (LogActive) DxComponent.LogAddLineTime($"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
                    
                    if (lazySubItems.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || lazySubItems.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce)
                        deactivatePopupEvent = false;                     // Pokud máme o SubItems žádat server, necháme si aktivní událost Popup
                    else
                        itemInfo.LazyInfo = null;                         // Data máme Více již LazyInfo nebudeme potřebovat, a událost Popup deaktivujeme.
                    cancel = false;                                       // Otevírání Popup menu nebudeme blokovat, protože v něm jsou nějaká data
                }
                if (lazySubItems.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce || lazySubItems.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
                {   // 2. SubItems mi dodá aplikace - na základě obsluhy eventu ItemOnDemandLoad; pak dojde k vyvolání zdejší metody _PopupMenu_RefreshItems():
                    _OpenItemPopupMenu = dxPopup;
                    _OpenItemPopupMenuItemId = itemInfo.Data.ItemId;
                    _OpenMenuLocation = GetPopupLocation(dxPopup.Activator as DevExpress.XtraBars.BarItemLink);
                    lazySubItems.Activator = null;
                    lazySubItems.PopupLocation = _OpenMenuLocation;
                    lazySubItems.CurrentMergeLevel = this.MergeLevel;
                    this.RunItemOnDemandLoad(lazySubItems.ParentItem);    // Vyvoláme event pro načtení dat, ten zavolá RefreshItem, ten provede UnMerge - Modify - Merge, a v Modify vyvolá zdejší metodu _PopupMenu_RefreshItems()...
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
                if (LogActive) DxComponent.LogAddLineTime($"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);

                // Pokud menu je definováno jako 'OnDemandLoadEveryTime', pak bychom měli zajistit předání LazyInfo i pro následující otevření:
                if (newRibbonItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
                {
                    itemInfo.LazyInfo = new LazySubItemsInfo(newRibbonItem, newRibbonItem.SubItemsContentMode, newRibbonItem.SubItems);
                    deactivatePopupEvent = false;                     // Pokud máme o SubItems vždycky žádat server, necháme si aktivní událost Popup
                }

                // Aktualizujeme info uložené v splitButton.DropDownControl.Tag  (tj. PopupMenu.Tag):
                itemInfo.Data = newRibbonItem;
                itemInfo.LazyInfo = null;

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
                subItem.ParentItem = parentItem;
                subItem.ParentGroup = parentItem.ParentGroup;
                DevExpress.XtraBars.BarItem barItem = CreateItem(subItem, level, dxGroup, true, ref count);
                if (barItem != null)
                {
                    PrepareBarItemTag(barItem, subItem, level, dxGroup);
                    var barLink = dxPopup.AddItem(barItem);
                    if (subItem.ItemIsFirstInGroup) barLink.BeginGroup = true;
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
            openMenuPopupMenu.ShowPopup(this.BarManager, popupLocation);
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
            bool isLazyLoad = (parentItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || parentItem.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
            if (hasSubItems && reallyCreate)
            {
                _BarMenu_FillItems(menu, level + 1, dxGroup, parentItem, subItems, false, ref count);
                PrepareBarItemTag(menu, parentItem, level, dxGroup);
            }
            else if (hasSubItems || isLazyLoad)
            {
                // menu.AddItem(new DevExpress.XtraBars.BarButtonItem(this.BarManager, "..."));     // Musí tu být alespoň jeden prvek, jinak při kliknutí na Menu se nebude nic dít (neproběhne event xBarMenu.Popup)
                PrepareBarItemTag(menu, parentItem, level, dxGroup, new LazySubItemsInfo(parentItem, parentItem.SubItemsContentMode, subItems));
            }
            if (level == 0)
            {   // Události řeším je pro menu základní úrovně (=na tlačítku), ne pro submenu:
                menu.GetItemData += Menu_GetItemData;
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
        private void Menu_GetItemData(object sender, EventArgs e)
        {
            if (!(sender is BarSubItem menu)) return;

            var mousePoint = Control.MousePosition;
            _OpenMenuReset(true);

            bool deactivateEvent = true;
            var itemInfo = menu.Tag as BarItemTagInfo;               // Do Menu jsme instanci BarItemTagInfo vytvořili při jeho tvorbě v metodě PrepareBarMenu()
            if (itemInfo != null && itemInfo.LazyInfo != null)
            {   // Pokud máme LazyInfo:
                var lazySubItems = itemInfo.LazyInfo;
                int level = itemInfo.Level;
                bool isOnDemand = (lazySubItems.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime || lazySubItems.SubItemsContentMode == RibbonContentMode.OnDemandLoadOnce);
                // Mohou nastat dvě situace, které mohou být souběžné (tedy 1 a poté i 2):
                if (lazySubItems.SubItems != null)
                {   // 1. SubItems jsou deklarované přímo zde (Static), pak z nich vytvoříme nabídku a necháme ji uživateli zobrazit:
                    var startTime = DxComponent.LogTimeCurrent;
                    int count = 0;
                    _BarMenu_FillItems(menu, level + 1, itemInfo.DxGroup, lazySubItems.ParentItem, lazySubItems.SubItems, true, ref count);
                    if (LogActive) DxComponent.LogAddLineTime($"LazyLoad Menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);

                    if (isOnDemand)
                        deactivateEvent = false;                     // Pokud máme o SubItems žádat server, necháme si aktivní událost Popup
                    else
                        itemInfo.LazyInfo = null;                    // Data máme Více již LazyInfo nebudeme potřebovat, a událost Popup deaktivujeme.
                }
                if (isOnDemand)
                {   // 2. O prvky SubItems máme požádat server - na základě obsluhy eventu ItemOnDemandLoad => RefreshItems => _BarMenu_OpenMenu() :
                    _OpenMenuLocation = mousePoint;
                    _OpenItemBarMenu = menu;
                    _OpenItemBarMenuItemId = itemInfo.Data.ItemId;

                    lazySubItems.PopupLocation = mousePoint;
                    lazySubItems.CurrentMergeLevel = this.MergeLevel;
                    this.RunItemOnDemandLoad(lazySubItems.ParentItem);
                    deactivateEvent = false;                         // Dokud nedoběhnou ze serveru data (OnDemandLoad => RefreshItem() ), tak necháme aktivní událost Popup = pro jistotu...
                }
                else
                {   // 3. Nemáme Static prvky SubItems, ale ani je nemáme načítat OnDemand...
                    //    necháme odpojit event Popup, a do Tagu vložíme IRibbonItem:
                    itemInfo.LazyInfo = null;                        // Více již LazyInfo nebudeme potřebovat...
                }
            }

            // Deaktivovat zdejší handler?
            if (deactivateEvent)
                menu.GetItemData -= Menu_GetItemData;
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

                // Pokud je menu v režimu OnDemandLoadEveryTime, tak po jeho zavření zajistíme znovu vyvolání handleru pro OnDemand donačtení při dalším otevírání menu:
                if (itemInfo.Data != null && itemInfo.Data.SubItemsContentMode == RibbonContentMode.OnDemandLoadEveryTime)
                    menu.GetItemData += Menu_GetItemData;
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
            // Toto menu bylo až dosud v režimu LazyLoad; odpojíme eventhandler GetItemData - aby se nám už opakovaně nevolal...
            menu.GetItemData -= Menu_GetItemData;
            if (menu.Tag is BarItemTagInfo itemInfo)
            {
                // Do Menu vložíme dodané prvky (newRibbonItem.SubItems):
                int level = itemInfo.Level;
                int count = 0;
                _BarMenu_FillItems(menu, level + 1, itemInfo.DxGroup, newRibbonItem, newRibbonItem.SubItems, true, ref count);
               
                // Aktualizujeme info uložené v splitButton.DropDownControl.Tag  (tj. PopupMenu.Tag):
                itemInfo.Data = newRibbonItem;
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
        /// <param name="subItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem[] GetBarSubItems(IRibbonItem parentItem, int level, DxRibbonGroup dxGroup, IEnumerable<IRibbonItem> subItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (subItems != null && reallyCreate)
            {
                foreach (IRibbonItem subItem in subItems)
                {
                    subItem.ParentItem = parentItem;
                    subItem.ParentGroup = parentItem.ParentGroup;
                    DevExpress.XtraBars.BarItem barItem = CreateItem(subItem, level, dxGroup, true, ref count);
                    if (barItem != null)
                        barItems.Add(barItem);
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
                    var preferredLink = GetLinkAtPoint(screenPoint);
                    if (preferredLink != null)
                        link = links.FirstOrDefault(l => Object.ReferenceEquals(l, preferredLink));
                }

                if (link == null)
                    link = links[0];

                link.OpenMenu();
            }
        }
        /// <summary>
        /// Metoda vrátí Link, který se nachází na zadané souřadnici (default = aktuální pozice myši).
        /// Tato metoda má na vstupu absolutní souřadnice (Screen) nebo null = převezme aktuální pozici myši.
        /// Metoda pracuje s ribbonem <see cref="TopRibbonControl"/>, protože ten je fyzicky zobrazen a na něm se (asi) kliklo na určitý prvek.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.BarItemLink GetLinkAtPoint(Point? screenPoint = null)
        {
            var topRibbon = TopRibbonControl;
            Point controlPoint = topRibbon.PointToClient(screenPoint ?? Control.MousePosition);
            var hit = topRibbon.CalcHitInfo(controlPoint);
            return hit?.Item;
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
            var galleryBarItem = new DevExpress.XtraBars.RibbonGalleryBarItem(this.BarManager);
            galleryBarItem.Gallery.Images = SystemAdapter.GetResourceImageList(RibbonGalleryImageSize);
            galleryBarItem.Gallery.HoverImages = SystemAdapter.GetResourceImageList(RibbonGalleryImageSize);
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
            galleryItem.ImageIndex = galleryItem.HoverImageIndex = SystemAdapter.GetResourceIndex(iRibbonItem.ImageName, DxRibbonControl.RibbonGalleryImageSize);
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
                this.RaiseRibbonItemClick(itemInfo.Data);
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
        /// BarManager
        /// </summary>
        public DevExpress.XtraBars.Ribbon.RibbonBarManager BarManager
        {
            get
            {
                if (_BarManager is null)
                {
                    DevExpress.XtraBars.Ribbon.RibbonBarManager rbm = new DevExpress.XtraBars.Ribbon.RibbonBarManager(this);
                    rbm.AllowMoveBarOnToolbar = true;
                    _BarManager = rbm;
                }
                return _BarManager;
            }
        }
        private DevExpress.XtraBars.Ribbon.RibbonBarManager _BarManager;
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
                itemInfo.Data = iRibbonItem;
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
            /// SubPrvky
            /// </summary>
            public IEnumerable<IRibbonItem> SubItems { get; private set; }
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

            // Pozor, po každém mergování / unmergování do this ribbonu se vytvoří nové menu CustomizationPopupMenu, takže je třeba znovunavázat eventhandler:
            this.CustomizationPopupMenuRefreshHandlers();

            this._SetQATUserItemKeys(DxQuickAccessToolbar.QATItemKeys, false);
        }
        /// <summary>
        /// Událost, kdy někdě došlo ke změně prvků QAT.
        /// Tuto metodu vyvolá event <see cref="DxQuickAccessToolbar.QATItemKeysChanged"/>.
        /// <para/>
        /// Mohlo to být ze serveru (poslal data) anebo uživatel v některém Ribbonu přidal / odebral prvek v QAT.
        /// Mohl to být jiný Ribbon, anebo i náš Ribbon - všechny změny se posílají do <see cref="DxQuickAccessToolbar.QATItemKeys"/> 
        /// a tamodtud se posílá událost o změně všem Ribbonům.
        /// <para/>
        /// Pokud to byl náš Ribbon, pak ten si před zveřejněním nového stavu (soupis QAT prvků) do <see cref="DxQuickAccessToolbar.QATItemKeys"/> tutéž hodnotu uložil interně do <see cref="_QATUserItemKeys"/>,
        /// tedy na změnu zvenku nebude reagovat protože pro něj nedošlo ke změně.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxQATItemKeysChanged(object sender, EventArgs e)
        {
            string newKeys = DxQuickAccessToolbar.QATItemKeys;
            string oldKeys = this._QATUserItemKeys;                  // Pokud jsem já (this, prostřednictvím uživatele) provedl změnu v QAT prvcích, pak jsem si nový stav uložil sem, a pak jsem nový stav poslal do DxQuickAccessToolbar.QATItemKeys
            if (String.Equals(newKeys, oldKeys)) return;             //  => tedy pro mě ke změně nedošlo.
            this._SetQATUserItemKeys(newKeys, true);                 // Změnu způsobil jiný Ribbon anebo server = poslal nová data
        }
        /// <summary>
        /// Souhrn zdejších klíčů QAT Items, zapamatovaný při poslední změně.
        /// </summary>
        private string _QATUserItemKeys;
        /// <summary>
        /// Obsahuje klíče prvků, které mají být / jsou zobrazeny v QAT (Quick Access Toolbar).
        /// Jednotlivé klíče jsou odděleny znakem tabulátorem.
        /// Vkládání hodnoty se typicky neřeší setováním do této property, ale do globálního registru <see cref="DxQuickAccessToolbar.QATItemKeys"/>, 
        /// která prostřednictvím eventu <see cref="DxQuickAccessToolbar.QATItemKeysChanged"/> odešle požadavek na aktualizaci do všech živých Ribbonů, 
        /// které si hodnotu z <see cref="DxQuickAccessToolbar.QATItemKeys"/> načtou a interně aplikují.
        /// </summary>
        public string QATUserItemKeys { get { return _GetQATUserItemKeys(); } set { this._SetQATUserItemKeys(value, true); }  }
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
        /// Používá se při setování nového seznamu do <see cref="QATUserItemKeys"/>.
        /// </summary>
        private void ResetQATUserItems()
        {
            _QATUserItems.ForEachExec(q => q.Reset());
            _QATUserItems.Clear();
            _QATUserItemDict.Clear();
        }
        /// <summary>
        /// Vrací sumární string z klíčů všech prvků v QAT
        /// </summary>
        /// <returns></returns>
        private string _GetQATUserItemKeys()
        {
            return DxQuickAccessToolbar.ConvertToString(this.ToolbarLocation, _QATUserItems.Select(q => q.Key));
        }
        /// <summary>
        /// Ze zadaného stringu vytvoří struktury pro evidenci prvků pro toolbar QAT (pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/>).
        /// Před tím zruší obsah fyzického QAT. Volitelně na konci znovu naplní fyzický QAT.
        /// </summary>
        /// <param name="qatItemKeys"></param>
        /// <param name="refreshToolbar"></param>
        private void _SetQATUserItemKeys(string qatItemKeys, bool refreshToolbar = false)
        {
            var qatData = DxQuickAccessToolbar.ConvertFromString(qatItemKeys);
            if (qatData == null) return;

            // Zjistíme, zda nový řetězec reálně povede ke změně obsahu v this Ribbonu (tzn. zda reálně budeme odstraňovat / přidávat naše reálné prvky).
            // Totiž, náš Ribbon nemusí fyzicky obsahovat ty prvky, které jsou přidané nebo odebrané.
            // Pak nemá význam změnu provádět!
            if (_ContainsRealChangeQatUserKeys(qatData))
                // Pokud tedy dojde ke změně, pak se musíme unmergovat, změnit a zpět mergovat:
                this._UnMergeModifyMergeCurrentRibbon(() => _SetQATUserItemKeysReal(qatData, qatItemKeys, refreshToolbar), true);
            else
                // Nejde o vizuální změnu, v tom případě pouze aktualizujeme obsah v datových strukturách (_QATUserItems a _QATUserItemDict):
                _SetQATUserItemKeysData(qatData, qatItemKeys);

            // Upravíme umístění QAT:
            if (qatData.Location != this.ToolbarLocation)
                this.ToolbarLocation = qatData.Location;
        }
        /// <summary>
        /// Metoda zjistí, zda dodaný string (obsahující klíče QAT User prvků) reálně povede k nějaké změně v aktuálním Ribbonu.
        /// Pokud tedy dodaný klíč bude obsahovat některý prvek, který aktuálně nemáme v QAT a přitom jej máme v Items, pak jde o změnu (budeme jej přidávat).
        /// Nebo naopak, pokud máme nějaký prvek fyzicky v QAT, ale není uveden v dodaném klíči, pak jde o změnu (budeme jej odebírat).
        /// Detekujeme i změnu pořadí (prvek sice máme, ale v jiném místě, než jej očekáváme).
        /// <para/>
        /// Vrací true = jsou tu změny / false = beze změn.
        /// </summary>
        /// <param name="qatData"></param>
        /// <returns></returns>
        private bool _ContainsRealChangeQatUserKeys(DxQuickAccessToolbar.Data qatData)
        {
            Queue<QatItem> qatQueue = new Queue<QatItem>();          // Fronta prvků QAT User, které my reálně máme v QAT, v jejich aktuálním pořadí
            if (_QATUserItems != null)
                _QATUserItems.Where(q => q.IsInQAT).ForEachExec(i => qatQueue.Enqueue(i));

            // Tento algoritmus provede kontrolu, zda požadované prvky (qatItemKeys), jejich podmnožina reálně existující v this Ribbonu,
            // je/není obsažena v reálném QAT v odpovídajícím pořadí.
            // Vrátíme true = je reálná změna / false = bez reálné změny
            foreach (string itemId in qatData.ItemsId)
            {
                string key = GetValidQATKey(itemId);
                if (key == "") continue;
                if (this.Items[itemId] == null) continue;            // Prvek daného klíče v našem Ribbonu není, nemůžeme jej ani přidat ani odebrat - nejde o změnu.

                // a) Pokud požadovaný QAT prvek [itemId] v našem Ribbonu fyzicky máme:
                //  => Měli bychom jej mít i v naší frontě viditelných prvků:
                if (qatQueue.Count == 0) return true;                // Naše reálné QAT prvky už nic neobsahují => musíme prvek přidat, jde tedy o změnu
                var qatItem = qatQueue.Dequeue();                    // Nějaký prvek tam máme, musí mát shodný klíč:
                if (qatItem.Key != key) return true;                 // Ale ten prvek, který máme na této pozici, má jiný klíč - jde o změnu.
            }

            return (qatQueue.Count > 0);                             // Pokud v našem soupisu máme nějaké další prvky, musíme je odstranit - jde o změnu.
        }
        /// <summary>
        /// Ze zadaného stringu vytvoří struktury pro evidenci prvků pro toolbar QAT (pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/>).
        /// Před tím zruší obsah fyzického QAT. Volitelně na konci znovu naplní fyzický QAT.
        /// </summary>
        /// <param name="qatData"></param>
        /// <param name="qatItemKeys"></param>
        /// <param name="refreshToolbar"></param>
        private void _SetQATUserItemKeysReal(DxQuickAccessToolbar.Data qatData, string qatItemKeys, bool refreshToolbar = false)
        {
            ResetQATUserItems();

            foreach (string itemId in qatData.ItemsId)
            {
                string key = GetValidQATKey(itemId);
                if (key == "") continue;
                if (_QATUserItemDict.ContainsKey(key)) continue;     // Duplicita na vstupu: ignorujeme
                QatItem qatItem = new QatItem(this, key);
                if (refreshToolbar) qatItem.BarItem = this.Items[itemId];
                _QATUserItems.Add(qatItem);
                _QATUserItemDict.Add(key, qatItem);
            }

            // Fyzická tvroba obsahu QAT:
            if (refreshToolbar) AddQATUserListToRibbon();

            // Lokální cache:
            _QATUserItemKeys = qatItemKeys;

            // Změna klíčů zdejšího proti veřejnému?
            string localKeys = _GetQATUserItemKeys();
            string publicKeys = DxQuickAccessToolbar.QATItemKeys;
            if (!String.Equals(localKeys, publicKeys))
                DxQuickAccessToolbar.QATItemKeys = localKeys;        // Tady dojde k eventu DxQuickAccessToolbar.QATItemKeysChanged, a vyvolá se zdejší handler _DxQATItemKeysChanged. Ale protože ten porovnává DxQuickAccessToolbar.QATItemKeys se zdejší hodnotou _QATUserItemKeys, k další změně už zde nedojde.
        }
        /// <summary>
        /// Naplní <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/> ze stávajích instancí, v pořadí dle dodaného stringu
        /// </summary>
        /// <param name="qatData"></param>
        /// <param name="qatItemKeys"></param>
        private void _SetQATUserItemKeysData(DxQuickAccessToolbar.Data qatData, string qatItemKeys)
        {
            var oldDict = _QATUserItemDict;
            var newList = new List<QatItem>();
            var newDict = new Dictionary<string, QatItem>();

            foreach (string itemId in qatData.ItemsId)
            {
                string key = GetValidQATKey(itemId);
                if (key == "") continue;
                if (newDict.ContainsKey(key)) continue;              // Duplicita na vstupu: ignorujeme
                bool oldExists = oldDict.TryGetValue(key, out var qatItem);
                if (!oldExists) qatItem = new QatItem(this, key);    // Toto by měly být pouze takové nové prvky, které nejsou přítomny v this Ribbonu. To ověřila metoda _ContainsRealChangeQatUserKeys().
                newList.Add(qatItem);                                // Do nového seznamu přidáváme prvky ve správném pořadí = zařazujeme do něj "cizí" prvky (které u nás nejsou)
                newDict.Add(key, qatItem);
            }

            _QATUserItems = newList;
            _QATUserItemDict = newDict;

            // Lokální cache:
            _QATUserItemKeys = qatItemKeys;
        }
        private List<QatItem> _QATUserItems;
        private Dictionary<string, QatItem> _QATUserItemDict;
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
            return (ExistsAnyQat && iRibbonItem != null && (DefinedInQAT(iRibbonItem.ItemId) || (iRibbonItem.SubItems != null && iRibbonItem.SubItems.Any(s => ContainsQAT(s)))));
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
            return _QATUserItemDict?.ContainsKey(key) ?? false;
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně existuje alespoň jeden záznam QAT v poli <see cref="_QATUserItems"/>
        /// </summary>
        protected bool ExistsAnyQat { get { return ((_QATUserItems?.Count ?? 0) > 0); } }
        /// <summary>
        /// Metoda je volána v procesu tvorby nových prvků Ribbonu, když je vytvořen prvek BarItem, který má být obsažen v QAT.
        /// Tato metoda si zaeviduje odkaz na tento BarItem v interním poli, z něhož následně (v metodě <see cref="AddQATUserListToRibbon()"/>) 
        /// všechny patřičné prvky uloží do fyzického ToolBaru QAT.
        /// Tato metoda tedy dodaný prvek nevloží okamžitě do ToolBaru.
        /// Tato metoda, pokud je volána pro prvek který v QAT nemá být, nepřidá tento prvek 
        /// nad rámec požadovaného seznamu prvků v QAT = <see cref="QATUserItemKeys"/> (nepřidává nové prvky do pole <see cref="_QATUserItems"/> a <see cref="_QATUserItemDict"/>).
        /// <para/>
        /// Duplicita: Pokud na vstupu bude prvek, pro jehož klíč už evidujeme prvek dřívější, pak nový prvek vložíme namísto prvku původního.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="ribbonItem"></param>
        private void AddBarItemToQATUserList(DevExpress.XtraBars.BarItem barItem, IRibbonItem ribbonItem)
        {
            if (barItem == null || ribbonItem == null) return;
            string key = GetValidQATKey(ribbonItem.ItemId);
            if (!_QATUserItemDict.TryGetValue(key, out var qatItem)) return;       // Pro daný prvek Ribbonu nemáme záznam, že bychom měli přidat prvek do QAT.
            qatItem.BarItem = barItem;
        }
        /// <summary>
        /// Volá se na závěr přidávání dat do stránek: 
        /// do fyzického ToolBaru QAT (Quick Access Toolbar) vepíše všechny aktuálně přítomné prvky z <see cref="_QATUserItems"/>.
        /// </summary>
        protected void AddQATUserListToRibbon()
        {
            var qatItems = _QATUserItems;
            if (qatItems == null || qatItems.Count == 0) return;

            // Pokud v seznamu není žádný prvek, který potřebuje provést změnu, tak nebudu toolbarem mrkat:
            if (!qatItems.Any(q => q.NeedChangeInQat)) return;

            foreach (var qatItem in qatItems.Where(q => q.CanRemoveFromQat))
                qatItem.RemoveBarItemLink();

            // Přidáme potřebné UserItems, a před první z nich volitelně přidáme oddělovač grupy (oddělí Direct a User prvky), pokud v Toolbaru něco zůstává:
            bool isGroupBegin = (this.Toolbar.ItemLinks.Count > 0);
            foreach (var qatItem in qatItems.Where(q => q.NeedAddToQat))
                qatItem.AddLinkToQat(ref isGroupBegin);
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
            RemoveLinkFromQat(iRibbonItem.ItemId);

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
            if (key != null && this._QATUserItemDict.TryGetValue(key, out var qatItem))
                qatItem.RemoveBarItemLink();
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

            // Zrušit systémové menu:
            this.CustomizationPopupMenu.HidePopup();
            this.CustomizationPopupMenu = new DevExpress.XtraBars.Ribbon.Internal.RibbonCustomizationPopupMenu(this);
            this.CustomizationPopupMenu.ClearLinks();
            this.CustomizationPopupMenuRefreshHandlers();                      // Právě jsem vygeneroval nové Popup menu, a chci i příště vyvolat tuhle metodu...

            // Připravit a aktivovat vlastní menu:
            DevExpress.XtraBars.BarItemLink link = GetLinkAtPoint(point);
            bool isQatDirectItem = _IsQATDirectItem(link);
            bool isQatAnyItem = _IsQATAnyItem(link);

            List<IMenuItem> items = new List<IMenuItem>();
            if (isQatDirectItem)
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonDirectQatItem}", Text = DxComponent.LocalizeDef(MsgCode.RibbonDirectQatItem, "Systémový prvek, nelze jej odebrat"), ImageName = "", Enabled = false });
            else if (!isQatAnyItem)
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonAddToQat}", Text = DxComponent.LocalizeDef(MsgCode.RibbonAddToQat, "Přidat na panel nástrojů Rychlý přístup"), ImageName = "svgimages/icon%20builder/actions_add.svg", Tag = link, ClickAction = CustomizationPopupMenu_ExecAdd });
            else
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonRemoveFromQat}", Text = DxComponent.LocalizeDef(MsgCode.RibbonRemoveFromQat, "Odebrat z panelu nástrojů Rychlý přístup"), ImageName = "svgimages/icon%20builder/actions_remove.svg", Tag = link, ClickAction = CustomizationPopupMenu_ExecRemove });

            bool isAbove = (this.ToolbarLocation == RibbonQuickAccessToolbarLocation.Above || this.ToolbarLocation == RibbonQuickAccessToolbarLocation.Default);
            if (!isAbove)
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonShowQatTop}", Text = DxComponent.LocalizeDef(MsgCode.RibbonShowQatTop, "Zobrazit panel nástrojů Rychlý přístup nad pásem karet"), ImageName = "svgimages/icon%20builder/actions_arrow2up.svg", ItemIsFirstInGroup = true, ClickAction = CustomizationPopupMenu_ExecMoveUp });
            else
                items.Add(new DataMenuItem() { ItemId = $"CPM_{MsgCode.RibbonShowQatDown}", Text = DxComponent.LocalizeDef(MsgCode.RibbonShowQatDown, "Zobrazit panel nástrojů Rychlý přístup pod pásem karet"), ImageName = "svgimages/icon%20builder/actions_arrow2down.svg", ItemIsFirstInGroup = true, ClickAction = CustomizationPopupMenu_ExecMoveDown });

            var popup = DxComponent.CreateDXPopupMenu(items);
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
            this._RunQATItemKeysChanged();
        }
        /// <summary>
        /// Kontextové menu QAT: Dej QAT nahoru
        /// </summary>
        /// <param name="menuItem"></param>
        private void CustomizationPopupMenu_ExecMoveUp(IMenuItem menuItem)
        {
            this.ToolbarLocation = RibbonQuickAccessToolbarLocation.Above;
            this._RunQATItemKeysChanged();
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
        /// Uživatel rukama přidal něco do QAT. Sem to nechodí při změnách z kódu!
        /// </summary>
        /// <param name="link"></param>
        protected override void OnAddToToolbar(DevExpress.XtraBars.BarItemLink link)
        {
            base.OnAddToToolbar(link);
            this.UserAddItemToQat(link);
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
                _RunQATItemKeysChanged();
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
            if (TryGetIRibbonData(link, out var key, out var iRibbonItem, out var iRibbonGroup, out var ownerRibbon))
            {   // Našli jsme data o dodaném prvku? Předáme to k vyřešení do instance Owner Ribbonu!
                //  - Totiž this může být nějaký Master Ribbon, do něhož je Mergován nějaký Slave Ribbon, a aktuální BarItem pochází z toho Slave Ribbonu.
                //  - Pak i evidenci QAT a volání události QATItemKeysChanged musí proběhnout v té instanci Ribbonu, které se věc týká, a nikoli this, kde je věc jen hostována ve formě Merge.
                if (this.CustomizationPopupMenu.Visible) CustomizationPopupMenu.HidePopup();
                ownerRibbon.UserRemoveItemFromQat(link, key, iRibbonItem, iRibbonGroup);
            }
        }
        /// <summary>
        /// Uživatel něco odebral z QAT. 
        /// Už víme, co to bylo, a je zajištěno, že zdejší instance (this) je majitelem daného prvku a je tedy odpovědná za evidenci QAT a vyvolání správného eventhandleru.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="key"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        private void UserRemoveItemFromQat(DevExpress.XtraBars.BarItemLink link, string key, IRibbonItem iRibbonItem, IRibbonGroup iRibbonGroup)
        {
            {   // Našli jsme data o dodaném prvku?
                if (_QATUserItemDict.TryGetValue(key, out var qatItem))
                {   // Známe => odebrat:
                    qatItem.Reset();
                    _QATUserItemDict.Remove(key);
                    _QATUserItems.RemoveAll(q => q.Key == key);
                    _RunQATItemKeysChanged();
                }
            }
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit data o prvku Ribbonu <see cref="IRibbonItem"/> podle jeho ItemId
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private bool TryGetIRibbonData(string itemId, out IRibbonItem iRibbonItem)
        {
            iRibbonItem = null;
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
                return true;
            }
            if (tag is IRibbonItem iItem)
            {
                iRibbonItem = iItem;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Metoda v dodaném linku najde jeho Item, jeho Tag, detekuje jeho typ a určí jeho Key, najde Ribbon, který prvek deklaroval, uloží typové výsledky a vrátí true.
        /// Pokud se nezdaří, vrátí false.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="key"></param>
        /// <param name="iRibbonItem"></param>
        /// <param name="iRibbonGroup"></param>
        /// <param name="ownerRibbon"></param>
        /// <returns></returns>
        private bool TryGetIRibbonData(DevExpress.XtraBars.BarItemLink link, out string key, out IRibbonItem iRibbonItem, out IRibbonGroup iRibbonGroup, out DxRibbonControl ownerRibbon)
        {
            DevExpress.XtraBars.BarItem barItem = link?.Item;
            if (barItem != null)
            {
                object tag = barItem.Tag;
                if (tag is BarItemTagInfo itemInfo)
                {
                    key = GetValidQATKey(itemInfo.Data.ItemId);
                    iRibbonItem = itemInfo.Data;
                    iRibbonGroup = itemInfo.Data.ParentGroup;
                    return (_TryGetDxRibbon(barItem, out ownerRibbon));
                }
                if (tag is IRibbonItem iItem)
                {
                    key = GetValidQATKey(iItem.ItemId);
                    iRibbonItem = iItem;
                    iRibbonGroup = iItem.ParentGroup;
                    return (_TryGetDxRibbon(barItem, out ownerRibbon));
                }
                if (tag is IRibbonGroup iGroup)
                {
                    key = GetValidQATKey(iGroup.GroupId);
                    iRibbonItem = null;
                    iRibbonGroup = iGroup;
                    return (_TryGetDxRibbon(barItem, out ownerRibbon));
                }
                if ((tag is DxRibbonGroup ribbonGroup) && (ribbonGroup.Tag is IRibbonGroup iiGroup))
                {
                    key = GetValidQATKey(iiGroup.GroupId);
                    iRibbonItem = null;
                    iRibbonGroup = iiGroup;
                    return (_TryGetDxRibbon(barItem, out ownerRibbon));
                }
            }
            key = null;
            iRibbonItem = null;
            iRibbonGroup = null;
            ownerRibbon = null;
            return false;
        }
        /// <summary>
        /// Došlo ke změně v obsahu <see cref="QATUserItemKeys"/>, zavolej události
        /// </summary>
        private void _RunQATItemKeysChanged()
        {
            if (this.CustomizationPopupMenu.Visible) this.CustomizationPopupMenu.HidePopup();

            string currentKeys = _GetQATUserItemKeys();
            _QATUserItemKeys = currentKeys;
            OnQATItemKeysChanged();
            QATItemKeysChanged?.Invoke(this, EventArgs.Empty);

            DxQuickAccessToolbar.QATItemKeys = currentKeys;          // Tady dojde k eventu DxQuickAccessToolbar.QATItemKeysChanged, a vyvolá se zdejší handler _DxQATItemKeysChanged
        }
        /// <summary>
        /// Je provedeno po změně hodnoty v <see cref="QATUserItemKeys"/>
        /// </summary>
        protected virtual void OnQATItemKeysChanged() { }
        /// <summary>
        /// Je voláno po změně hodnoty v <see cref="QATUserItemKeys"/> přímo v this Ribbonu
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
                return allItems.Select(t => t.Item2).CreateDictionary(i => GetValidQATKey(i.ItemId), true);
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
        /// <param name="items"></param>
        private void _SetQATDirectItemsInner(IRibbonItem[] items)
        {
            _ClearQATDirectItems();

            
            List<QatItem> qatItems = new List<QatItem>();
            if (items != null)
            {
                var qatUserLinks = this.Toolbar.ItemLinks.ToArray();
                int mergeOrder = -1000;
                foreach (var item in items)
                {
                    if (item is null) continue;
                    var barItem = CreateItem(item, 0, null, null);
                    if (barItem != null)
                    {
                        barItem.MergeOrder = mergeOrder++;
                        var barLink = this.Toolbar.ItemLinks.Add(barItem, item.ItemIsFirstInGroup);
                        qatItems.Add(new QatItem(this, item, barItem, barLink));
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
            var key = GetValidQATKey(iRibbonItem.ItemId);
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
                qatItem.RemoveBarItemLink(true);
                qatItem.Dispose();
            }
            _QATDirectItems = null;
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
            /// <param name="key"></param>
            public QatItem(DxRibbonControl owner, string key)
            {
                this._Owner = owner;
                this._Key = key;
            }
            /// <summary>
            /// Konstruktor pro existující <see cref="IRibbonItem"/>
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="iRibbonItem"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, IRibbonItem iRibbonItem, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, iRibbonItem.ItemId)
            {
                _BarItem = barItem;
                _BarItemLink = barItemLink;
                RibbonItem = iRibbonItem;
            }
            /// <summary>
            /// Konstruktor pro existující <see cref="IRibbonItem"/>
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="key"></param>
            /// <param name="iRibbonItem"></param>
            /// <param name="iRibbonGroup"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, string key, IRibbonItem iRibbonItem, IRibbonGroup iRibbonGroup, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, key)
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
            /// <param name="key"></param>
            /// <param name="ribbonGroup"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, string key, IRibbonGroup ribbonGroup, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, key)
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
                string text = RibbonItem?.ItemId ?? _Key;
                if (RibbonItem != null) text += $" = \"{RibbonItem.Text}\"";

                if (_BarItem != null) text += "; BarItem exists";
                if (_BarItemLink != null) text += "; BarLink exists";

                if (NeedChangeInQat) text += "; NeedChangeInQat";
                if (NeedAddToQat) text += "; NeedAddToQat";
                if (CanRemoveFromQat) text += "; NeedRemoveFromQat";

                return text;
            }
            /// <summary>Vlastník = Ribbon</summary>
            private DxRibbonControl _Owner;
            /// <summary>Klíč</summary>
            private string _Key;
            /// <summary>
            /// Obsahuje klíč prvku nebo grupy. Není null, neobsahuje TAB ani mezery na okrajích
            /// </summary>
            public string Key { get { return _Key; } }
            /// <summary>
            /// Definice prvku Ribbonu
            /// </summary>
            public IRibbonItem RibbonItem { get; private set; }
            /// <summary>
            /// Fyzický objekt Ribbonu.
            /// Setováním objektu do této property dojde k odstranění případně existujícího linku <see cref="BarItemLink"/> 
            /// (z fyzického ToolBaru, z paměti, z tohoto objektu).
            /// </summary>
            public DevExpress.XtraBars.BarItem BarItem 
            {
                get { return _BarItem; }
                set
                {
                    RemoveBarItemLink();
                    _BarItem = value;
                }
            }
            private DevExpress.XtraBars.BarItem _BarItem;
            /// <summary>
            /// Definice grupy Ribbonu
            /// </summary>
            public IRibbonGroup RibbonGroup { get; private set; }
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
                    RemoveBarItemLink();
                    _BarGroup = value;
                }
            }
            private DevExpress.XtraBars.Ribbon.RibbonPageGroup _BarGroup;
            /// <summary>
            /// Link na Objekt Ribbonu
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
            /// Metoda fyzicky přidá Link do QAT
            /// </summary>
            /// <param name="isGroupBegin">Nastavit v prvku BeginGroup = true?</param>
            public void AddLinkToQat(ref bool isGroupBegin)
            {
                this.RemoveBarItemLink();
                if (this.BarItem != null)
                {
                    var link = _Owner.Toolbar.ItemLinks.Add(this.BarItem);
                    if (isGroupBegin)
                    {
                        link.BeginGroup = true;
                        isGroupBegin = false;
                    }
                    this._BarItemLink = link;
                }
            }
            /// <summary>
            /// Metoda zajistí, že pokud tento prvek je zobrazen ve fyzickém QAT, pak z něj bude odebrán, Disposován a z tohoto objektu vynulován.
            /// </summary>
            /// <param name="disposeBarItem">Volitelně disposovat i samotný prvek <see cref="BarItem"/></param>
            public void RemoveBarItemLink(bool disposeBarItem = false)
            {
                var barLink = this.BarItemLink;
                if (barLink != null)
                {
                    if (_Owner != null)
                        _Owner.Toolbar.ItemLinks.Remove(barLink);
                    // není vždy OK: barLink.Dispose();
                    this._BarItemLink = null;
                }

                if (disposeBarItem)
                {
                    var barItem = this.BarItem;
                    if (barItem != null)
                    {
                        if (_Owner.Items.Contains(barItem))
                            _Owner.Items.Remove(barItem);
                        _BarItem = null;
                    }
                }
            }
            /// <summary>
            /// Zruší obsah this instance, 
            /// odebere prvek Link z reálného toolbaru QAT (pokud tam je) a Disposuje jej
            /// </summary>
            public void Reset()
            {
                RemoveBarItemLink();

                this._Owner = null;
                this.RibbonItem = null;
                this.RibbonGroup = null;
                this.BarItem = null;
            }
        }
        #endregion
        #endregion
        #region Kliknutí na prvek Ribbonu
        private void InitEvents()
        {
            ApplicationButtonClick += RibbonControl_ApplicationButtonClick;
            ItemClick += RibbonControl_ItemClick;
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
                var args = new TEventArgs<IRibbonCategory>(iRibbonCategory);
                OnRibbonPageCategoryClick(args);
                RibbonPageCategoryClick?.Invoke(this, args);
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
                var args = new TEventArgs<IRibbonGroup>(iRibbonGroup);
                OnRibbonGroupButtonClick(args);
                RibbonGroupButtonClick?.Invoke(this, args);
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
        private void RibbonControl_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // poznámka: tady nemusím řešit přechod z Ribbonu "ke se kliklo" do Ribbonu "kde byl prvek definován", tady to už interně vyřešil DevExpress!
            if (_TryGetIRibbonItem(e.Item, out IRibbonItem iRibbonItem))
            {
                if (e.Item is DevExpress.XtraBars.BarCheckItem checkItem)
                    iRibbonItem.Checked = checkItem.Checked;

                _RibbonItemClick(iRibbonItem);
            }
        }
        /// <summary>
        /// Provede akci odpovídající kliknutí na prvek Ribbonu, na vstupu jsou data prvku
        /// </summary>
        /// <param name="iRibbonItem"></param>
        internal void RaiseRibbonItemClick(IRibbonItem iRibbonItem) { _RibbonItemClick(iRibbonItem); }
        /// <summary>
        /// Vyvolá reakce na kliknutí na prvek Ribbonu:
        /// event <see cref="RibbonItemClick"/>.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        private void _RibbonItemClick(IRibbonItem iRibbonItem)
        {
            iRibbonItem?.ClickAction?.Invoke(iRibbonItem);
            var args = new TEventArgs<IRibbonItem>(iRibbonItem);
            OnRibbonItemClick(args);
            RibbonItemClick?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po kliknutí na prvek Ribbonu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRibbonItemClick(TEventArgs<IRibbonItem> args) { }
        /// <summary>
        /// Událost volaná po kliknutí na prvek Ribbonu
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonItem>> RibbonItemClick;

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
        private bool _TryGetIRibbonGroup(DevExpress.XtraBars.Ribbon.RibbonPageGroup group, out IRibbonGroup iRibbonGroup, out DxRibbonControl definingRibbon)
        {
            iRibbonGroup = null;
            definingRibbon = null;
            if (group == null) return false;
            _TryGetDxRibbon(group, out definingRibbon);
            if (group.Tag is IRibbonGroup iRibbonItem) { iRibbonGroup = iRibbonItem; return true; }
            if (_TryGetIRibbonItem(group.ItemLinks, out var iMenuItem)) { iRibbonGroup = iMenuItem.ParentGroup; return (iRibbonGroup != null); }
            return false;
        }
        /// <summary>
        /// V rámci daného prvku se pokusí najít odpovídající definici prvku <see cref="IRibbonItem"/> v jeho Tagu.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private bool _TryGetIRibbonItem(DevExpress.XtraBars.BarItem item, out IRibbonItem iRibbonItem)
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
        private bool _TryGetIRibbonPage(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IRibbonPage iRibbonPage)
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
        private bool _TryGetIRibbonGroup(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IRibbonGroup iRibbonGroup)
        {
            iRibbonGroup = null;
            if (pages == null) return false;
            var found = pages.SelectMany(p => p.Groups)
                             .Select(g => g.Tag)
                             .OfType<IRibbonGroup>()
                             .FirstOrDefault();
            if (found == null) return false;
            iRibbonGroup = found;
            return true;
        }
        /// <summary>
        /// V rámci daných stránek se pokusí najít odpovídající definici prvního prvku <see cref="IRibbonItem"/> v tagu Item.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private bool _TryGetIRibbonItem(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IRibbonItem iRibbonItem)
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
        private bool _TryGetIRibbonItem(IEnumerable<DevExpress.XtraBars.BarItemLink> barItemLinks, out IRibbonItem iRibbonItem)
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
        private bool _TryGetDxRibbon(DevExpress.XtraBars.Ribbon.RibbonPageCategory category, out DxRibbonControl definingRibbon)
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
        private bool _TryGetDxRibbon(DevExpress.XtraBars.Ribbon.RibbonPageGroup group, out DxRibbonControl definingRibbon)
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
        private bool _TryGetDxRibbon(DevExpress.XtraBars.BarItem barItem, out DxRibbonControl definingRibbon)
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
        private DxRibbonControl _SearchRibbonInPages(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages)
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
        private DxRibbonControl _SearchRibbonInItems(IEnumerable<DevExpress.XtraBars.BarItem> items)
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
        /// Aktuálně mergovaný Child <see cref="DxRibbonControl"/>
        /// </summary>
        public DxRibbonControl MergedChildDxRibbon { get { return (MergedChildRibbon as DxRibbonControl); } }
        /// <summary>
        /// Aktuálně nejvyšší Ribbon = ten, ve kterém se zobrazují data z this Ribbonu.
        /// Tedy: pokud this není Merged, pak zde je this.
        /// Pokud this je Merged, pak zde je nejvyšší <see cref="MergedIntoParentDxRibbon"/>.
        /// </summary>
        public DxRibbonControl TopRibbonControl { get { return ((this.MergedIntoParentDxRibbon != null) ? (this.MergedIntoParentDxRibbon.TopRibbonControl) : this); } }
        /// <summary>
        /// Aktuálně používaný BarManager = z TopRibbonu.
        /// Tedy: pokud this není Merged, pak zde je zdejší <see cref="BarManager"/>.
        /// Pokud this je Merged, pak zde je <see cref="BarManager"/> z <see cref="TopRibbonControl"/>.
        /// </summary>
        public BarManager CurrentBarManager { get { return TopRibbonControl.BarManager; } }
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
                this._MergeChildRibbon(childDxRibbon, forceSelectChildPage);    // Tady se do Child DxRibbonu vepíše, že jeho MergedIntoParentDxRibbon je this

            // A pokud já jsem byl původně mergován nahoru, tak se nahoru zase vrátím:
            if (parentRibbon != null)
                parentRibbon._MergeChildDxRibbon(this, forceSelectChildPage);   // Tady se může rozběhnout rekurze ve zdejší metodě až do instance Top Parenta...
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
            var startTime = DxComponent.LogTimeCurrent;

            bool selectPage = _CurrentSelectChildActivePageOnMerge ?? SelectChildActivePageOnMerge;  // Rád bych si předal _CurrentSelectChildActivePageOnMerge jako parametr, ale tady to nejde, jsme override bázové metody = bez toho parametru.
            string slaveSelectedPage = (selectPage && childRibbon is DxRibbonControl dxRibbon) ? dxRibbon.SelectedPageFullId : null;

            bool currentDxRibbonState = this.CurrentModifiedState;

            var childDxRibbon = childRibbon as DxRibbonControl;
            bool childDxRibbonState = false;
            try
            {
                if (childDxRibbon != null)
                {
                    childDxRibbonState = childDxRibbon.CurrentModifiedState;
                    childDxRibbon.MergedIntoParentDxRibbon = this;
                    childDxRibbon._CurrentMergeState = MergeState.MergeToParent;
                    childDxRibbon.SetModifiedState(true, true);
                }

                _CurrentMergeState = MergeState.MergeWithChild;
                SetModifiedState(true, true);
                CurrentModifiedState = true;
                DxComponent.TryRun(() => base.MergeRibbon(childRibbon));
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

            if (selectPage && slaveSelectedPage != null) this.SelectedPageFullId = slaveSelectedPage;
            this.StoreLastSelectedPage();

            if (LogActive) DxComponent.LogAddLineTime($"MergeRibbon: to Parent: {this.DebugName}; from Child: {(childRibbon?.ToString())}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
        private void _UnMergeModifyMergeCurrentRibbon(Action action, bool mergeBack)
        {
            var startTime = DxComponent.LogTimeCurrent;

            var ribbonsUp = this.MergedRibbonsUp;
            int count = ribbonsUp.Count;

            // Pokud this Ribbon není nikam mergován (když počet nahoru mergovaných je 1):
            if (count == 1)
            {   // Provedu požadovanou akci rovnou (není třeba dělat UnMerge a Merge), a skončíme:
                SetModifiedState(true, true);
                _RunUnMergedAction(action);
                SetModifiedState(ribbonsUp[0].Item2, true);          // Vrátím stav CurrentModifiedState původní, nikoliv false - ono tam mohlo být true!
                if (LogActive) DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: Current; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Odmergovat - Provést akci (pokud je) - Mergovat zpátky (vše nebo jen UpRibbony):
            int last = count - 1;
            var topRibbon = ribbonsUp[last].Item1;
            var topRibbonSelectedPage1 = topRibbon.SelectedPageFullId;

            try
            {
                // Top Ribbon pozastaví svoji práci:
                topRibbon.SuspendLayout();
                topRibbon._PrepareEmptyPageForUMM();

                // Všem Ribonům v řadě potlačíme CurrentModifiedState a CheckLazyContentEnabled, protože bude docházet ke změně SelectedPage, ale to nemá vyvolat požadavek na její LazyLoad donačítání:
                ribbonsUp.ForEach(r => { r.Item1.SetModifiedState(true, true); });

                // UnMerge proběhne od posledního (=TopMost: count - 1) Ribbonu dolů až k našemu Parentu (u >= 1):
                for (int u = last; u >= 1; u--)
                    ribbonsUp[u].Item1.UnMergeDxRibbon();

                // Konečně máme this Ribbon osamocený (není Merge nahoru, ani neobsahuje MergedChild), provedeme tedy akci:
                _RunUnMergedAction(action);

                // ...a pak se přimerguje náš parent / nebo i zdejší Ribbon zpátky nahoru do TopMost:
                // Nazpátek se bude mergovat (mergeBack) i this Ribbon do svého Parenta? Anebo jen náš Parent do jeho Parenta a náš Ribbon zůstane UnMergovaný?
                int mergeFrom = (mergeBack ? 0 : 1);
                for (int m = mergeFrom; m < last; m++)
                    ribbonsUp[m].Item1._MergeCurrentDxToParent(ribbonsUp[m + 1].Item1, true);
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
            var topRibbonSelectedPage2 = topRibbon.SelectedPageFullId;
            if (topRibbonSelectedPage2 != topRibbonSelectedPage1 && topRibbon.CheckLazyContentEnabled)
                topRibbon.CheckLazyContent(topRibbon.SelectedPage, false);

            if (LogActive) DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: UnMerge + Action + Merge; Total Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
                // This Ribbon pozastaví svoji práci:
                this.BarManager.BeginUpdate();

                // Nyní máme náš Ribbon UnMergovaný z Parentů, ale i on v sobě může mít mergovaného Childa:
                if (childRibbon != null) this._UnMergeDxRibbon();

                action();
            }
            finally
            {
                // Do this Ribbonu vrátíme jeho Child Ribbon:
                if (childRibbon != null) this._MergeChildRibbon(childRibbon, false);

                // This Ribbon obnoví svoji práci:
                this.BarManager.EndUpdate();
            }
            if (LogActive) DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: RunAction; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
            _CurrentMergeState = MergeState.None;
            CustomizationPopupMenuRefreshHandlers();

            if (childDxRibbon != null)
            {
                childDxRibbon._CurrentMergeState = MergeState.None;
                childDxRibbon.CustomizationPopupMenuRefreshHandlers();
                childDxRibbon.MergedIntoParentDxRibbon = null;
                childDxRibbon.ActivateLastActivePage();
            }
            this.MergedChildRibbon = null;

            // Po reálném UnMerge (tj. když existuje childDxRibbon) zkusíme selectovat tu naši vlastní stránku, která byla naposledy aktivní:
            //  (Poznámka: DevExpress volá metodu UnMergeRibbon() i v procesu nastavování vlastností Ribbonu, proto tahle podmínka)
            if (childDxRibbon != null)
                this.ActivateLastActivePage();

            if (LogActive) DxComponent.LogAddLineTime($"UnMergeRibbon from Parent: {this.DebugName}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
        /// Vytvoří a vrátí logickou Grupu do Ribbonu s obsahem tlačítek pro skiny (tedy definici pro tuto grupu)
        /// </summary>
        /// <param name="groupText"></param>
        /// <param name="addSkinButton"></param>
        /// <param name="addPaletteButton"></param>
        /// <param name="addPaletteGallery"></param>
        /// <param name="addUhdSupport"></param>
        /// <returns></returns>
        public static IRibbonGroup CreateSkinIGroup(string groupText = null, bool addSkinButton = true, bool addPaletteButton = true, bool addPaletteGallery = false, bool addUhdSupport = false)
        {
            string text = (!String.IsNullOrEmpty(groupText) ? groupText : "Výběr vzhledu");
            DataRibbonGroup iGroup = new DataRibbonGroup() { GroupText = text };

            if (addSkinButton) iGroup.Items.Add(new DataRibbonItem() { ItemId = "_SYS__DevExpress_SkinSetDropDown", ItemType = RibbonItemType.SkinSetDropDown });
            if (addPaletteButton) iGroup.Items.Add(new DataRibbonItem() { ItemId = "_SYS__DevExpress_SkinPaletteDropDown", ItemType = RibbonItemType.SkinPaletteDropDown });
            if (addPaletteGallery) iGroup.Items.Add(new DataRibbonItem() { ItemId = "_SYS__DevExpress_SkinPaletteGallery", ItemType = RibbonItemType.SkinPaletteGallery });
            if (addUhdSupport) iGroup.Items.Add(new DataRibbonItem() 
            { 
                ItemId = "_SYS__DevExpress_UhdSupportCheckBox", Text = "UHD Paint", ToolTipText = "Zapíná podporu pro Full vykreslování na UHD monitoru",
                ItemType = RibbonItemType.CheckBoxToggle, 
                // ImageUnChecked = "svgimages/zoom/zoomout.svg", ImageChecked = "svgimages/zoom/zoomin.svg",
                Checked = DxComponent.UhdPaintEnabled, ClickAction = SetUhdPaint 
            });

            return iGroup;
        }
        private static void SetUhdPaint(IMenuItem menuItem) 
        {
            DxComponent.UhdPaintEnabled = (menuItem?.Checked ?? false);
            DxComponent.ApplicationRestart();
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
                if (itemInfo.DxGroup.OwnerDxRibbon != null) itemInfo.DxGroup.OwnerDxRibbon.RaiseRibbonItemClick(itemInfo.Data);
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
        /// Aktualizuje svoje vlastnosti z dodané definice
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="withId">Aktualizuj i ID</param>
        public void Fill(IRibbonPage iRibbonPage, bool withId = false)
        {
            if (withId) this.Name = iRibbonPage.PageId;
            this.Text = iRibbonPage.PageText;
            if (iRibbonPage.MergeOrder > 0) this.MergeOrder = iRibbonPage.MergeOrder;          // Záporné číslo iRibbonPage.MergeOrder říká: neměnit hodnotu, pokud stránka existuje. Důvod: při Refreshi existující stránky nechceme měnit její pozici.
            this.Visible = iRibbonPage.Visible;
            this.PageData = iRibbonPage;
            this.Tag = iRibbonPage;
            iRibbonPage.RibbonPage = this;
        }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        protected DxRibbonControl OwnerDxRibbon { get; private set; }
        /// <summary>
        /// Vlastník Ribbon přetypovaný na <see cref="IDxRibbonInternal"/>
        /// </summary>
        protected IDxRibbonInternal IOwnerDxRibbon { get { return OwnerDxRibbon; } }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="ribbon"></param>
        protected void Init(DxRibbonControl ribbon)
        {
            this.OwnerDxRibbon = ribbon;
            LazyLoadInfo = null;
        }
        /// <summary>
        /// Data definující stránku a její obsah
        /// </summary>
        public IRibbonPage PageData { get; private set; }
        /// <summary>
        /// ID poslední aktivace této stránky. Při aktivaci stránky se volá metoda <see cref="OnActivate()"/>.
        /// Slouží k určení stránky, která má být aktivní.
        /// </summary>
        public int ActivateTimeStamp { get; private set; }
        /// <summary>
        /// Vyvolá se po každé aktivaci stránky, inkrementuje <see cref="ActivateTimeStamp"/> a vyvolá událost <see cref="DxRibbonControl.SelectedDxPageChanged"/> prostřednictvím <see cref="IDxRibbonInternal.OnActivatePage(DxRibbonPage)"/>.
        /// </summary>
        public void OnActivate()  
        {
            if (this.ActivateTimeStamp == 0 || this.ActivateTimeStamp < this.IOwnerDxRibbon.CurrentTimeStamp)
            {   // Pokud naše (Page) hodnota ActivateTimeStamp je menší, než jakou eviduje náš Ribbon, je zjevné že Ribbon už nějakou další hodnotu přidělil nějaké jiné stránce.
                // Pak tedy aktivace this stránky (zdejší metoda) je reálnou změnou aktivní stránky, a ne jen opakovanou akcí na téže stránce...
                ActivateTimeStamp = this.IOwnerDxRibbon.GetNextTimeStamp();
                this.IOwnerDxRibbon.OnActivatePage(this);
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

                string groupId = DxRibbonLazyLoadInfo.LazyLoadGroupId;
                var ownerRibbonItems = this.OwnerDxRibbon.Items;
                var groupsToDelete = this.Groups.OfType<DevExpress.XtraBars.Ribbon.RibbonPageGroup>().Where(g => g.Name != groupId).ToList();
                var itemsToDelete = groupsToDelete.SelectMany(g => g.ItemLinks).Select(l => l.Item).ToList();
                iOwnerRibbon.RemoveGroups(groupsToDelete.OfType<DxRibbonGroup>());

                // Před fyzickým odebráním grup a prvků z RibbonPage je předám do QAT systému v Ribbonu, aby si je odebral ze své evidence: 
                iOwnerRibbon.RemoveGroupsFromQat(groupsToDelete);
                iOwnerRibbon.RemoveItemsFromQat(itemsToDelete);

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
        public DxRibbonGroup() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        public DxRibbonGroup(string text) : base(text) { }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        internal DxRibbonControl OwnerDxRibbon { get { return this.Ribbon as DxRibbonControl; } }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        internal DxRibbonPage OwnerDxPage { get { return this.Page as DxRibbonPage; } }
        /// <summary>
        /// Data definující grupu a její obsah
        /// </summary>
        internal IRibbonGroup DataGroup { get; set; }
        /// <summary>
        /// Smaže obsah this grupy
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
        }
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
                    __LazyLoadGroupId = "LazyLoadGroupId_" + new Guid().ToString();
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

            return (lazyDxPages.Count > 0);
        }
        /// <summary>
        /// V dané kolekci Groups vyhledá grupy s ID = <see cref="LazyLoadGroupId"/>, 
        /// v nich vyhledá BarItemy, jejichž Tag obsahuje instanci <see cref="DxRibbonPage"/>, a kde tato stránka má aktivní LazyContent.
        /// Tyto nalezené stránky přidává do listu <paramref name="lazyDxPages"/>.
        /// Metoda průběžně odebírá zmíněné Linky na Buttony, a odebírá i prázdné grupy.
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
            _ImageNameNull = "images/xaf/templatesv2images/bo_unknown_disabled.svg";     //  "svgimages/xaf/state_validation_skipped.svg";
            _ImageNameUnChecked = "svgimages/icon%20builder/actions_deletecircled.svg";  //  "svgimages/xaf/state_validation_invalid.svg";
            _ImageNameChecked = "svgimages/icon%20builder/actions_checkcircled.svg";     //  "svgimages/xaf/state_validation_valid.svg";
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
            DxComponent.ApplyImage(this.ImageOptions, resourceName: ImageNameCurrent);
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
    #region TrackBar - TODO
    
    #warning TrackBar - TODO

    [DevExpress.XtraEditors.Registrator.UserRepositoryItem("RegisterMyTrackBar")]
    public class RepositoryItemMyTrackBar : DevExpress.XtraEditors.Repository.RepositoryItemTrackBar
    {
        static RepositoryItemMyTrackBar()
        {
            RegisterMyTrackBar();
        }
        public static void RegisterMyTrackBar()
        {
            Image img = null;
            DevExpress.XtraEditors.Registrator.EditorRegistrationInfo.Default.Editors.Add(new DevExpress.XtraEditors.Registrator.EditorClassInfo(CustomEditName, typeof(MyTrackBar), typeof(RepositoryItemMyTrackBar), typeof(MyTrackBarViewInfo), new MyTrackBarPainter(), true, img));
        }


        protected override int ConvertValue(object val)
        {
            return base.ConvertValue(val);
        }

        public const string CustomEditName = "MyTrackBar";

        public RepositoryItemMyTrackBar() { }



        //---

        public static RepositoryItemMyTrackBar SetupTrackBarDouble(double paramValue)
        {

            int paramValueInt = Convert.ToInt32(paramValue * 100);

            RepositoryItemMyTrackBar trackbar = new RepositoryItemMyTrackBar()
            {
                Minimum = paramValueInt - 500,
                Maximum = paramValueInt + 500,
                SmallChange = 5,
                ShowLabels = true
            };

            trackbar.Labels.Add(new DevExpress.XtraEditors.Repository.TrackBarLabel((Convert.ToDouble(trackbar.Minimum / 100d)).ToString(),
              trackbar.Minimum));
            trackbar.Labels.Add(new DevExpress.XtraEditors.Repository.TrackBarLabel((Convert.ToDouble(trackbar.Maximum / 100d)).ToString(),
              trackbar.Maximum));
            trackbar.Labels.Add(new DevExpress.XtraEditors.Repository.TrackBarLabel(paramValue.ToString(), paramValueInt));

            return trackbar;
        }

        //---




        public override string EditorTypeName { get { return CustomEditName; } }

      

        public override void Assign(DevExpress.XtraEditors.Repository.RepositoryItem item)
        {
            BeginUpdate();
            try
            {
                base.Assign(item);
                RepositoryItemMyTrackBar source = item as RepositoryItemMyTrackBar;
                if (source == null) return;
                //
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    [System.ComponentModel.ToolboxItem(true)]
    public class MyTrackBar : DevExpress.XtraEditors.TrackBarControl
    {
        static MyTrackBar()
        {
            RepositoryItemMyTrackBar.RegisterMyTrackBar();
        }

        public MyTrackBar()
        {
        }

        public override object EditValue { get { return base.EditValue; } set { base.EditValue = value; } }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public new RepositoryItemMyTrackBar Properties { get { return base.Properties as RepositoryItemMyTrackBar; } }

        protected override object ConvertCheckValue(object val)
        {
            return base.ConvertCheckValue(val);
        }

        public override string EditorTypeName { get { return RepositoryItemMyTrackBar.CustomEditName; } }
    }

    public class MyTrackBarViewInfo : DevExpress.XtraEditors.ViewInfo.TrackBarViewInfo
    {
        public MyTrackBarViewInfo(DevExpress.XtraEditors.Repository.RepositoryItem item)
            : base(item)
        {
        }



        public override object EditValue
        {
            get
            {
                return base.EditValue;
            }
            set
            {
                try
                {
                    if (value is float)
                    {
                        int result = 0;
                        result = (int)(Convert.ToSingle(value) * 100);
                        base.EditValue = result;
                    }
                    else
                        base.EditValue = value;
                }
                catch { }
            }
        }

        public override DevExpress.XtraEditors.Drawing.TrackBarObjectPainter GetTrackPainter()
        {
            return new SkinMyTrackBarObjectPainter(LookAndFeel);
        }
    }

    public class MyTrackBarPainter : DevExpress.XtraEditors.Drawing.TrackBarPainter
    {
        public MyTrackBarPainter()
        {
        }
    }

    public class SkinMyTrackBarObjectPainter : DevExpress.XtraEditors.Drawing.SkinTrackBarObjectPainter
    {
        public SkinMyTrackBarObjectPainter(DevExpress.Skins.ISkinProvider provider)
            : base(provider)
        {
        }
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
        /// Obsahuje true, když v this StatusBaru je mergován nějaký Child (<see cref="ChildDxStatusBar"/> nebo alespoň <see cref="MergedStatusBar"/>)
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
            __QATItemKeys = "";
        }
        private static object __Lock = new object();
        private static DxQuickAccessToolbar __Current;
        #endregion
        #region Public static, private instance
        /// <summary>
        /// Souhrn všech klíčů prvků, které mají být zobrazeny v každém Ribbonu v QuickAccessToolbar.
        /// Při tvorbě nového Ribbonu se odsud získá hodnota, kerá určuje prvky, které budou v QAT zobrazeny.
        /// Pro existující Ribbon se hodnota odsud do Ribbonu načítá prostřednictvím události o změně <see cref="QATItemKeysChanged"/>.
        /// </summary>
        public static string QATItemKeys { get { return Current._QATItemKeys; } set { Current._QATItemKeys = value; } }
        /// <summary>
        /// Událost, která je vyvolána po každé změně hodnoty <see cref="QATItemKeys"/>.
        /// Pozor, parametr sender je null; nelze tedy určit kdo změnu způsobil.
        /// To je dáno tím, že hodnota se setuje prostým přiřazením stringu do <see cref="QATItemKeys"/>, bez předání způsobitele.
        /// </summary>
        public static event EventHandler QATItemKeysChanged { add { Current.__QATItemKeysChanged += value; } remove { Current.__QATItemKeysChanged -= value; } }
        /// <summary>
        /// Aktuální soupis prvků
        /// </summary>
        private string _QATItemKeys 
        { 
            get { return __QATItemKeys; } 
            set 
            {
                string newKeys = (value ?? "").Trim();
                string oldKeys = __QATItemKeys;
                if (newKeys != oldKeys)
                {
                    __QATItemKeys = newKeys;
                    __QATItemKeysChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }
        /// <summary>Úložiště hodnoty pro <see cref="QATItemKeys"/></summary>
        private string __QATItemKeys;
        /// <summary>Úložiště pro eventhandlery</summary>
        private event EventHandler __QATItemKeysChanged;
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
        #endregion
        #region Serializace / deserializace
        /// <summary>
        /// Vrátí jeden string obsahující serializované zadané údaje
        /// </summary>
        /// <param name="location"></param>
        /// <param name="itemsId"></param>
        /// <returns></returns>
        public static string ConvertToString(RibbonQuickAccessToolbarLocation location, IEnumerable<string> itemsId)
        {
            StringBuilder sb = new StringBuilder();
            string del = QATItemKeysDelimiterChar.ToString();
            string loc = LocationPrefix + location.ToString();
            sb.Append(loc + del);
            if (itemsId != null)
                itemsId.ForEachExec(itemId => sb.Append(GetValidQATKey(itemId) + del));
            return sb.ToString();
        }
        /// <summary>
        /// Z daného stringu vrátí uložené údaje. Může vrátit NULL.
        /// </summary>
        /// <param name="qatItemKeys"></param>
        /// <returns></returns>
        public static Data ConvertFromString(string qatItemKeys)
        {
            if (String.IsNullOrEmpty(qatItemKeys)) return null;
            RibbonQuickAccessToolbarLocation location = RibbonQuickAccessToolbarLocation.Default;
            List<string> itemsId = new List<string>();

            var items = qatItemKeys.Split(QATItemKeysDelimiterChar);
            int count = items.Length;
            string locationPrefix = LocationPrefix;
            for (int i = 0; i < count; i++)
            {
                string item = items[i];
                if (String.IsNullOrEmpty(item)) continue;
                if (i == 0 && item.StartsWith(locationPrefix) && Enum.TryParse<RibbonQuickAccessToolbarLocation>(item.Substring(locationPrefix.Length), out RibbonQuickAccessToolbarLocation loc))
                    location = loc;
                else
                    itemsId.Add(item);
            }
            return new Data(location, itemsId.ToArray());
        }
        /// <summary>
        /// Analyzovaná data
        /// </summary>
        public class Data
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="location"></param>
            /// <param name="itemsId"></param>
            public Data(RibbonQuickAccessToolbarLocation location, string[] itemsId)
            {
                this.Location = location;
                this.ItemsId = itemsId;
            }
            /// <summary>
            /// Umístění Toolbaru
            /// </summary>
            public RibbonQuickAccessToolbarLocation Location { get; private set; }
            /// <summary>
            /// Klíče prvků
            /// </summary>
            public string[] ItemsId { get; private set; }
        }
        /// <summary>
        /// Prefix umístění QAT, nachází se na pozici [0]
        /// </summary>
        private const string LocationPrefix = "Location:";
        /// <summary>
        /// Oddělovač jednotlivých klíčů v <see cref="QATItemKeys"/> (=tabulátor), string délky 1 znak
        /// </summary>
        private const string QATItemKeysDelimiter = "\t";
        /// <summary>
        /// Oddělovač jednotlivých klíčů v <see cref="QATItemKeys"/> (=tabulátor), char
        /// </summary>
        private const char QATItemKeysDelimiterChar = '\t';
        #endregion
    }
    #endregion
    #region Třídy definující Ribbon : defaultní implementace odpovídajících interface
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
            this.Groups = new List<IRibbonGroup>();
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
        public static List<IRibbonPage> SortPages(IEnumerable<IRibbonPage> pages)
        {
            List<IRibbonPage> list = new List<IRibbonPage>();
            if (pages != null)
                list.AddRange(pages.Where(p => p != null));
            if (list.Count > 1)
            {
                int pageOrder = 0;
                foreach (var item in list)
                {
                    if (item.PageOrder == 0) item.PageOrder = ++pageOrder; else if (item.PageOrder > pageOrder) pageOrder = item.PageOrder;
                }
                list.Sort((a, b) => a.MergeOrder.CompareTo(b.MergeOrder));
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
                if (_PageId == null) _PageId = Guid.NewGuid().ToString();
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
        public virtual List<IRibbonGroup> Groups { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonGroup> IRibbonPage.Groups { get { return this.Groups; } }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonPage"/> po jejím vytvoření.
        /// </summary>
        public virtual WeakTarget<DxRibbonPage> RibbonPage { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
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
                if (_CategoryId == null) _CategoryId = Guid.NewGuid().ToString();
                return _CategoryId;
            }
            set { _CategoryId = value; }
        }
        /// <summary>
        /// Reálně uložené ID stránky
        /// </summary>
        protected string _CategoryId;
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
        public virtual WeakTarget<DxRibbonPageCategory> RibbonCategory { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
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
            this.Items = new List<IRibbonItem>();
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
        public static List<IRibbonGroup> SortGroups(IEnumerable<IRibbonGroup> groups)
        {
            List<IRibbonGroup> list = new List<IRibbonGroup>();
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
        public IRibbonPage ParentPage { get; set; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci stránky
        /// </summary>
        public string GroupId
        {
            get
            {
                if (_GroupId == null) _GroupId = Guid.NewGuid().ToString();
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
        public virtual List<IRibbonItem> Items { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonItem> IRibbonGroup.Items { get { return this.Items; } }
        /// <summary>
        /// Sem bude umístěna fyzická <see cref="DxRibbonGroup"/> po jejím vytvoření.
        /// </summary>
        public virtual WeakTarget<DxRibbonGroup> RibbonGroup { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public virtual object Tag { get; set; }
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
            RibbonStyle = source.RibbonStyle;
            VisibleInSearchMenu = source.VisibleInSearchMenu;
            SubItemsContentMode = source.SubItemsContentMode;
            SubItems = source.SubItems?.ToList();
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
        public static List<IRibbonItem> SortRibbonItems(IEnumerable<IRibbonItem> items)
        {
            List<IRibbonItem> list = new List<IRibbonItem>();
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
            if (itemA.Shortcut != itemB.Shortcut) return false;
            if (itemA.Visible != itemB.Visible) return false;
            if (itemA.VisibleInSearchMenu != itemB.VisibleInSearchMenu) return false;

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
        public virtual IRibbonGroup ParentGroup { get; set; }
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IRibbonItem"/>
        /// </summary>
        public virtual IRibbonItem ParentRibbonItem { get; set; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public new RibbonItemType ItemType { get; set; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        public virtual RibbonItemStyles RibbonStyle { get; set; }
        /// <summary>
        /// Zobrazit v Search menu?
        /// </summary>
        public virtual bool VisibleInSearchMenu { get; set; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        public virtual RibbonContentMode SubItemsContentMode { get; set; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu.
        /// Výchozí hodnota je null.
        /// </summary>
        public new List<IRibbonItem> SubItems { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonItem> IRibbonItem.SubItems { get { return this.SubItems; } }
        /// <summary>
        /// Sem bude umístěn fyzický BarItem po jeho vytvoření.
        /// </summary>
        public virtual WeakTarget<BarItem> RibbonItem { get; set; }
    }
    #endregion
    #region Interface IRibbonPage, IRibbonCategory, IRibbonGroup, IRibbonItem;  Enumy RibbonPageType, RibbonContentMode, RibbonItemStyles, BarItemPaintStyle, RibbonItemType.
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
        /// Pořadí stránky v rámci mergování, vkládá se do RibbonPage
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
        /// Pořadí grupy v rámci mergování, vkládá se do RibbonGroup
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
        /// Sem bude umístěna fyzická <see cref="DxRibbonGroup"/> po jejím vytvoření.
        /// </summary>
        WeakTarget<DxRibbonGroup> RibbonGroup { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
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
        /// Styl zobrazení prvku
        /// </summary>
        RibbonItemStyles RibbonStyle { get; }
        /// <summary>
        /// Zobrazit v Search menu?
        /// </summary>
        bool VisibleInSearchMenu { get; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        RibbonContentMode SubItemsContentMode { get; }
        /// <summary>
        /// Subpoložky Ribbonu (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu
        /// </summary>
        new IEnumerable<IRibbonItem> SubItems { get; }
        /// <summary>
        /// Sem bude umístěn fyzický BarItem po jeho vytvoření.
        /// </summary>
        WeakTarget<BarItem> RibbonItem { get; set; }
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
        /// If active, an item can be displayed like a smal bar item with its caption.
        /// </summary>
        SmallWithText = 2,
        /// <summary>
        /// If active, an item can be displayed like a smal bar item without its caption.
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
        None,
        /// <summary>
        /// Používá se ve StatusBaru
        /// </summary>
        Label,
        /// <summary>
        /// Tlačítko
        /// </summary>
        Button,
        /// <summary>
        /// Skupina tlačítek ???
        /// </summary>
        ButtonGroup,
        /// <summary>
        /// Tlačítko plus menu
        /// </summary>
        SplitButton,
        /// <summary>
        /// Standardní CheckBox
        /// </summary>
        CheckBoxStandard,
        /// <summary>
        /// Button se stavem Checked, který může být NULL (výchozí hodnota). 
        /// Pokud má být výchozí stav false, je třeba jej do <see cref="ITextItem.Checked"/> vložit!
        /// Lze specifikovat ikony pro všechny tři stavy (NULL - false - true)
        /// </summary>
        CheckBoxToggle,
        /// <summary>
        /// Prvek RadioGrupy
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
        SkinPaletteGallery
    }
    #endregion
}
