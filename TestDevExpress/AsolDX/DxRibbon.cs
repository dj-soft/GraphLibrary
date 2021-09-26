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
            InitProperties();
            InitEvents();
            InitQuickAccessToolbar();
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnregisterListener(this);
            base.Dispose(disposing);
        }
        /// <summary>
        /// Výchozí nastavení
        /// </summary>
        private void InitProperties()
        {
            var iconList = ComponentConnector.GraphicsCache;
            Images = iconList.GetImageList(ImagesSize);
            LargeImages = iconList.GetImageList(LargeImagesSize);

            AllowKeyTips = true;
            ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
            ColorScheme = DevExpress.XtraBars.Ribbon.RibbonControlColorScheme.DarkBlue;
            RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonControlStyle.Office2019;
            CommandLayout = DevExpress.XtraBars.Ribbon.CommandLayout.Classic;
            DrawGroupCaptions = DevExpress.Utils.DefaultBoolean.True;
            DrawGroupsBorderMode = DevExpress.Utils.DefaultBoolean.True;
            GalleryAnimationLength = 300;
            GroupAnimationLength = 300;
            ItemAnimationLength = 300;
            ItemsVertAlign = DevExpress.Utils.VertAlignment.Top;
            MdiMergeStyle = DevExpress.XtraBars.Ribbon.RibbonMdiMergeStyle.Always;
            OptionsAnimation.PageCategoryShowAnimation = DevExpress.Utils.DefaultBoolean.True;
            RibbonCaptionAlignment = DevExpress.XtraBars.Ribbon.RibbonCaptionAlignment.Center;
            SearchItemShortcut = new DevExpress.XtraBars.BarShortcut(Keys.Control | Keys.F);
            ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;        //  True;
            ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.True;
            ShowMoreCommandsButton = DevExpress.Utils.DefaultBoolean.True;
            ShowToolbarCustomizeItem = true;
            ShowPageHeadersMode = DevExpress.XtraBars.Ribbon.ShowPageHeadersMode.Show;
            ShowSearchItem = true;
            ShowToolbarCustomizeItem = true;
            ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Below;

            ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            ApplicationButtonText = " HELIOS ";
            ToolTipController = DxComponent.DefaultToolTipController;

            Visible = true;

            this.AllowMinimizeRibbon = false;    // Povolit minimalizaci Ribbonu? Pak ale nejde vrátit :-(
            this.AllowCustomization = false;     // Hodnota true povoluje (na pravé myši) otevřít okno Customizace Ribbonu, a to v Greenu nepodporujeme
            this.ShowQatLocationSelector = true; // Hodnota true povoluje změnu umístění Ribbonu

            this.AllowGlyphSkinning = false;     // Nikdy ne true!
            this.ShowItemCaptionsInQAT = true;

            this.SelectChildActivePageOnMerge = true;
            this.CheckLazyContentEnabled = true;
        }
        /// <summary>
        /// Vykreslení controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintAfter(e);
            // this.CustomizeQatMenu();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DebugName ?? this.GetType().Name;
        }
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public virtual bool LogActive { get; set; }
        /// <summary>
        /// Jméno Ribbonu pro debugování
        /// </summary>
        public string DebugName { get; set; }
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
        /// Vykreslí ikonu vpravo
        /// </summary>
        /// <param name="e"></param>
        private void PaintAfter(PaintEventArgs e)
        {
            OnPaintImageRightBefore(e);

            bool isSmallRibbon = (this.CommandLayout == DevExpress.XtraBars.Ribbon.CommandLayout.Simplified);
            Image image = (isSmallRibbon ? (_ImageRightMini ?? _ImageRightFull) : (_ImageRightFull ?? _ImageRightMini));
            if (image == null) return;
            Size imageNativeSize = image.Size;
            if (imageNativeSize.Width <= 0 || imageNativeSize.Height <= 0) return;

            // Rectangle buttonsBounds = (isSmallRibbon ? ClientRectangle.Enlarge(0, -4, -28, -4) : ButtonsBounds.Enlarge(-4));
            Rectangle buttonsBounds = (isSmallRibbon ? ButtonsBounds.Enlarge(0, -4, -28, +16) : ButtonsBounds.Enlarge(-4));
            ContentAlignment alignment = (isSmallRibbon ? ContentAlignment.TopRight : ContentAlignment.BottomRight);
            Rectangle imageBounds = imageNativeSize.AlignTo(buttonsBounds, alignment, true, true);

            e.Graphics.DrawImage(image, imageBounds);

            OnPaintImageRightAfter(e);
        }
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
        #region Pole obsahující stránky Ribbonu
        /// <summary>
        /// ID aktuálně vybrané stránky = <see cref="DevExpress.XtraBars.Ribbon.RibbonPage.Name"/>.
        /// Lze setovat, dojde k aktivaci dané stránky (pokud je nalezena).
        /// Funguje správně i pro stránky kategorií i pro mergované stránky.
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
        /// ID posledně reálné vybrané stránky. V procesu změny aktivní stránky obsahuje předchozí.
        /// Pokud se provádí Clear, tato hodnota se neaktualizuje = obsahuje předchozí aktivní stránku.
        /// Pokud po Clear nezbude žádná stránka, pak zde bude zachycena poslední platná.
        /// Pokud po Clear bude aktivována jiná existující stránka, pak zde bude tato nová.
        /// Před aktivací první stránky je zde null, ale poté už nikdy ne.
        /// </summary>
        public string LastSelectedPageId { get; private set; }
        /// <summary>
        /// ID posledně vybrané stránky, která je naše nativní (bude k dispozici i po provedení Unmerge Child ribbonu).
        /// Na tuto stránku seRibbon vrátí po Unmerge.
        /// </summary>
        public string LastSelectedOwnPageId { get; private set; }
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
        #region Tvorba obsahu Ribbonu: Clear(), ClearPageContents(), RemoveVoidContainers(), AddPages(), ReFillPages()
        /// <summary>
        /// Smaže celý obsah Ribbonu. Ribbon se zmenší na řádek pro záhlaví a celé okno pod ním se přeuspořádá.
        /// Důvodem je smazání všech stránek.
        /// Jde o poměrně nehezký efekt.
        /// </summary>
        public void Clear()
        {
            ModifyCurrentDxContent(_Clear);
        }
        /// <summary>
        /// Smaže svůj obsah
        /// </summary>
        private void _Clear()
        { 
            var startTime = DxComponent.LogTimeCurrent;

            string lastSelectedPageId = this.SelectedPageId;
            int removeItemsCount = 0;
            try
            {
                _ClearingNow = true;

                this.Pages.Clear();
                this.Categories.Clear();
                this.PageCategories.Clear();
                this.Toolbar.ItemLinks.Clear();
                this._ClearItems(ref removeItemsCount);
            }
            finally
            {
                this.LastSelectedPageId = lastSelectedPageId;
                _ClearingNow = false;
            }

            if (LogActive) DxComponent.LogAddLineTime($" === ClearRibbon {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Korektně smaže BarItemy z this.Items.
        /// Ponechává tam sysstémové prvky!
        /// </summary>
        /// <param name="removeItemsCount"></param>
        private void _ClearItems(ref int removeItemsCount)
        {
            // var itns = this.Items.Select(i => i.GetType().FullName).ToArray();

            // Pokud bych dal this.Items.Clear(), tak přijdu o všechny prvky celého Ribbonu,
            //   a to i o "servisní" = RibbonSearchEditItem, RibbonExpandCollapseItem, AutoHiddenPagesMenuItem.
            // Ale když nevyčistím Itemy, budou tady pořád strašit...
            // Ponecháme prvky těchto typů: "DevExpress.XtraBars.RibbonSearchEditItem", "DevExpress.XtraBars.InternalItems.RibbonExpandCollapseItem", "DevExpress.XtraBars.InternalItems.AutoHiddenPagesMenuItem"
            // Následující algoritmus NENÍ POMALÝ: smazání 700 Itemů trvá 11 milisekund.
            // Pokud by Clear náhodou smazal i nějaké další sytémové prvky, je nutno je určit = určit jejich FullType a přidat jej do metody _IsSystemItem() !
            int count = this.Items.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (!_IsSystemItem(this.Items[i]))
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
        private bool _IsSystemItem(object item)
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
        private bool _ClearingNow;
        /// <summary>
        /// Smaže výhradně jednotlivé prvky z Ribbonu (Items a LazyLoadContent) a grupy prvků (Page.Groups).
        /// Ponechává naživu Pages, Categories a PageCategories.
        /// Tím zabraňuje blikání.
        /// </summary>
        public void ClearPageContents()
        {
            ModifyCurrentDxContent(_ClearPageContents);
        }
        /// <summary>
        /// Smaže obsah (itemy a grupy) ale ponechá Pages, Categories a PageCategories.
        /// </summary>
        private void _ClearPageContents()
        {
            var startTime = DxComponent.LogTimeCurrent;

            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.AllOwnPages)
                DxRibbonPage.ClearContentPage(page);

            int removeItemsCount = 0;
            this._ClearItems(ref removeItemsCount);

            if (LogActive) DxComponent.LogAddLineTime($" === ClearPageContents {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Smaže prázdné prázdné stránky a nevyužité kategorie v rámci this Ribbonu.
        /// Dovoluje provádět výměnu obsahu Ribbonu bez blikání, procesem: 
        /// <see cref="ClearPageContents()"/>; 
        /// <see cref="AddPages(IEnumerable{IRibbonPage}, bool)"/>;
        /// <see cref="RemoveVoidContainers()"/>;
        /// <para/>
        /// Výměnu obsahu je možno provést i pomocí <see cref="AddPages(IEnumerable{IRibbonPage}, bool)"/> s parametrem clearCurrentContent = true.
        /// </summary>
        public void RemoveVoidContainers()
        {
            ModifyCurrentDxContent(_RemoveVoidContainers);
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
            this.RunInGui(() =>
            {
                this.ModifyCurrentDxContent(() =>
                {
                    if (clearCurrentContent) _ClearPageContents();
                    _AddPages(iRibbonPages, this.UseLazyContentCreate, false, "Fill");
                    if (clearCurrentContent) _RemoveVoidContainers();
                    CheckLazyContentCurrentPage(isCalledFromReFill);
                });
            });
        }
        /// <summary>
        /// Znovu naplní stránky this Ribbonu specifikované v dodaných datech.
        /// Nejprve zahodí obsah stránek, které jsou uvedeny v dodaných datech.
        /// Pak do Ribbonu vygeneruje nový obsah do specifikovaných stránek.
        /// Pokud pro některou stránku nebudou dodána žádná platná data, stránku zruší
        /// (k tomu se použije typ prvku <see cref="IRibbonItem.RibbonItemType"/> == <see cref="RibbonItemType.None"/>, kde daný záznam pouze definuje Page ke zrušení).
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// </summary>
        /// <param name="iRibbonPages"></param>
        public void ReFillPages(IEnumerable<IRibbonPage> iRibbonPages)
        {
            if (iRibbonPages == null) return;
            this.RunInGui(() =>
            {
                this.ModifyCurrentDxContent(() =>
                {
                    var reFillPages = _PrepareReFill(iRibbonPages, true);
                    _AddPages(iRibbonPages, this.UseLazyContentCreate, true, "OnDemand");
                    _RemoveVoidContainers(reFillPages);
                    CheckLazyContentCurrentPage(true);
                });
            });
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
                this.ModifyCurrentDxContent(() =>
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
                        AddQatListToRibbon();
                    }
                });
            }

            if (!removeLazyInfo && lazyGroup != null)
                lazyGroup.IsActive = true;

            if (lazyGroup.IsOnDemand && !isCalledFromReFill)
            {   // Oba režimy OnDemandLoad vyvolají patřičný event, pokud tato metoda NENÍ volána právě z akce naplnění ribbonu daty OnDemand:
                RunStartOnDemandLoad(iRibbonPage);
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

            _ActivateLazyLoadOnIdle = false;

            var startTime = DxComponent.LogTimeCurrent;
            var list = DataRibbonPage.SortPages(iRibbonPages);
            int count = 0;
            foreach (var iRibbonPage in list)
                _AddPage(iRibbonPage, isLazyContentFill, isOnDemandFill, ref count);

            AddQatListToRibbon();

            _StartLazyLoadOnIdle();

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

            var pageCategory = GetPageCategory(iRibbonPage.Category, iRibbonPage.ChangeMode);     // Pokud je to třeba, vygeneruje Kategorii
            var page = GetPage(iRibbonPage, pageCategory);                     // Najde / Vytvoří stránku do this.Pages nebo do category.Pages
            if (page is null) return;

            if (this.SelectedPageId == page.Name) isLazyContentFill = false;   // Pokud v Ribbonu je aktuálně vybraná ta stránka, která se nyní generuje, pak se NEBUDE plnit v režimu Lazy
            bool createContent = page.PreparePageForContent(iRibbonPage, isLazyContentFill, isOnDemandFill, out bool isStaticLazyContent);
            if (isStaticLazyContent)
                _ActivateLazyLoadOnIdle = true;                                // Pokud tuhle stránku nebudu plnit (=nyní jen generujeme prázdnou stránku, anebo jen obsahující QAT prvky), tak si poznamenám, že ji budu plnit ve stavu OnIdle

            // Problematika QAT je v detailu popsána v této metodě:
            bool isNeedQAT = !createContent && ContainsQAT(iRibbonPage);       // isNeedQAT je true tehdy, když bychom stránku nemuseli plnit (je LazyLoad), ale musíme do ní vložit pouze prvky typu QAT - a stránku přitom máme ponechat v režimu LazyLoad

            if (!createContent && !isNeedQAT) return;                          // víc už dělat nemusíme. Máme stránku a v ní LazyInfo, a prvky QAT nepotřebujeme (v dané stránce nejsou).

            if (!isNeedQAT && page.HasOnlyQatContent)
                page.ClearContent(true, false);
            
            page.HasOnlyQatContent = isNeedQAT;                                // Do stránky si poznamenáme, zda stránka obsahuje jen QAT prvky!

            var list = DataRibbonGroup.SortGroups(iRibbonPage.Groups);
            foreach (var iRibbonGroup in list)
            {
                iRibbonGroup.ParentPage = iRibbonPage;
                _AddGroup(iRibbonGroup, page, isNeedQAT, ref count);
            }
        }
        /// <summary>
        /// Metoda přidá danou grupu do dané stránky
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="page"></param>
        /// <param name="isNeedQAT">Přidávat pouze prvky označené QAT (stránka jako celek je v režimu LazyContent, ale prvky QAT potřebujeme i v této stránce)</param>
        /// <param name="count"></param>
        private void _AddGroup(IRibbonGroup iRibbonGroup, DxRibbonPage page, bool isNeedQAT, ref int count)
        {
            if (iRibbonGroup == null || page == null) return;
            if (isNeedQAT && !ContainsQAT(iRibbonGroup)) return;               // V režimu isNeedQAT přidáváme jen prvky QAT, a ten v dané grupě není žádný

            var group = GetGroup(iRibbonGroup, page);
            if (group is null) return;
          
            var iRibbonItems = DataRibbonItem.SortRibbonItems(iRibbonGroup.Items);
            foreach (var iRibbonItem in iRibbonItems)
            {
                iRibbonItem.ParentGroup = iRibbonGroup;
                _AddBarItem(iRibbonItem, group, isNeedQAT, ref count);
            }
        }
        /// <summary>
        /// Metoda přidá daný prvek do dané grupy
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="group"></param>
        /// <param name="isNeedQAT">Přidávat pouze prvky označené QAT (stránka jako celek je v režimu LazyContent, ale prvky QAT potřebujeme i v této stránce)</param>
        /// <param name="count"></param>
        private void _AddBarItem(IRibbonItem iRibbonItem, DxRibbonGroup group, bool isNeedQAT, ref int count)
        {
            if (iRibbonItem == null || group == null) return;
            if (isNeedQAT && !ContainsQAT(iRibbonItem)) return;               // V režimu isNeedQAT přidáváme jen prvky QAT, a ten v dané grupě není žádný

            var barItem = GetItem(iRibbonItem, group, isNeedQAT, ref count);
            if (barItem is null) return;
            // více není třeba.
        }
        #endregion
        #region LazyLoad page content : OnSelectedPageChanged => CheckLazyContent; OnDemand loading
        /// <summary>
        /// Požadavek na používání opožděné tvorby obsahu stránek Ribbonu.
        /// Pokud bude true, pak jednotlivé prvky na stránce Ribbonu budou fyzicky vygenerovány až tehdy, až bude stránka vybrána k zobrazení.
        /// Změna hodnoty se projeví až při následujícím přidávání prvků do Ribbonu. 
        /// Pokud tedy byla hodnota false (=výchozí stav), pak se přidá 600 prvků, a teprve pak se nastaví <see cref="UseLazyContentCreate"/> = true, je to fakt pozdě.
        /// </summary>
        public bool UseLazyContentCreate { get; set; }
        /// <summary>
        /// Při aktivaci stránky (těsně před tím) zajistí vygenerování prvků LazyLoad
        /// </summary>
        /// <param name="prev"></param>
        protected override void OnSelectedPageChanged(DevExpress.XtraBars.Ribbon.RibbonPage prev)
        {
            this.CheckLazyContent(this.SelectedPage, false);
            this.StoreLastSelectedPage();
            base.OnSelectedPageChanged(prev);
        }
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
            if (page == null) return;
            if (!this.CheckLazyContentEnabled) return;

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
        /// Uloží ID aktuální stránky do <see cref="LastSelectedPageId"/>.
        /// Ukládá pouze když zrovna neprobíhá Clear, a pouze pokud je reálně nějaká stránka vybrána (edy existuje alespoň jedna).
        /// Jinak hodnotu <see cref="LastSelectedPageId"/> nemění.
        /// </summary>
        protected void StoreLastSelectedPage()
        {
            if (_ClearingNow || this.SelectedPage == null) return;
            var pageId = this.SelectedPageId;
            if (!String.IsNullOrEmpty(pageId))
            {
                this.LastSelectedPageId = pageId;
                if (IsOwnPageId(pageId))
                    this.LastSelectedOwnPageId = pageId;
            }
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
        protected void RunStartOnDemandLoad(IRibbonPage iRibbonPage)
        {
            TEventArgs<IRibbonPage> args = new TEventArgs<IRibbonPage>(iRibbonPage);
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
        /// Po nějakém čase - když aplikace získá data, pak zavolá zdejší metodu <see cref="DxRibbonControl.ReFillPages(IEnumerable{IRibbonPage})"/>;
        /// tato metoda zajistí zobrazení nově dodaných dat v odpovídajících stránkách Ribbonu.
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonPage>> PageOnDemandLoad;
        #endregion
        #region LazyLoadOnIdle : dovolujeme provést opožděné plnění stránek (LazyLoad) v režimu Application.OnIdle
        /// <summary>
        /// Volá se na konci přidávání stránek. Pokud je <see cref="_ActivateLazyLoadOnIdle"/> == true, pak nastaví <see cref="_ActiveLazyLoadOnIdle"/> = true.
        /// Zajistí se tak, že při nejbližší situaci ApplicationIdle (volá se <see cref="IListenerApplicationIdle.ApplicationIdle()"/>)
        /// si this Ribbon vygeneruje fyzický obsah oněch LazyLoad stránek.
        /// </summary>
        private void _StartLazyLoadOnIdle()
        {
            if (_ActivateLazyLoadOnIdle)
                _ActiveLazyLoadOnIdle = true; ;
        }
        /// <summary>
        /// Aplikace má volný čas, Ribbon by si mohl vygenerovat LazyLoad Static pages, pokud takové má.
        /// </summary>
        private void _ApplicationIdle()
        {
            if (_ActiveLazyLoadOnIdle)
                _PrepareLazyLoadStaticPages();
            _ActivateLazyLoadOnIdle = false;
            _ActiveLazyLoadOnIdle = false;
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
            if (lazyPages.Count == 0) return;

            var startTime = DxComponent.LogTimeCurrent;
         
            int pageCount = lazyPages.Count;
            int itemCount = 0;
            this.ModifyCurrentDxContent(() =>
            {   // Provede Unmerge this Ribbonu, pak provede následující akci, a poté zase zpětně Merge do původního stavu, se zachováním SelectedPage:
                int icnt = 0;
                foreach (var lazyPage in lazyPages)
                    _PrepareLazyLoadStaticPage(lazyPage, ref icnt);
                itemCount = icnt;
            });

            if (LogActive) DxComponent.LogAddLineTime($" === Ribbon {DebugName}: CreateLazyStaticPages; Create: {pageCount} Pages; {itemCount} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        private void _PrepareLazyLoadStaticPage(DxRibbonPage lazyPage, ref int itemCount)
        {
            IRibbonPage iRibbonPage = lazyPage.PageData;
            _AddPage(iRibbonPage, false, false, ref itemCount);
        }
        /// <summary>
        /// Nastavuje se na true v procesu tvorby takových LazyLoad stránek, které mají svůj obsah staticky deklarován, ale dosud nejsou fyzicky vytvořeny controly v Ribbonu.
        /// Na konci tvorby Ribbonu v metodě <see cref="_StartLazyLoadOnIdle"/> se v případě <see cref="_ActivateLazyLoadOnIdle"/> 
        /// nastaví jiná proměnná <see cref="_ActiveLazyLoadOnIdle"/>, a ta hodnotou true zajistí, že v nejbližším volání 
        /// <see cref="IListenerApplicationIdle.ApplicationIdle()"/> provedene reálné generování controlů.
        /// </summary>
        private bool _ActivateLazyLoadOnIdle;
        /// <summary>
        /// Hodnota true říká, že this Ribon má nějaké Pages ve stavu LazyLoad se statickým obsahem.
        /// Pak v době, kdy systém má volný čas (když je volána metoda <see cref="IListenerApplicationIdle.ApplicationIdle()"/>) 
        /// si Ribbon fyzicky vygeneruje reálný obsah daných Pages.
        /// </summary>
        private bool _ActiveLazyLoadOnIdle;
        #endregion
        #region Fyzická tvorba prvků Ribbonu (Kategorie, Stránka, Grupa, Prvek, konkrétní prvky, ...) : Get/Create/Clear/Add/Remove
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí kategorii pro daná data
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <param name="pageChangeMode"></param>
        /// <returns></returns>
        protected DxRibbonPageCategory GetPageCategory(IRibbonCategory iRibbonCategory, ContentChangeMode pageChangeMode)
        {
            if (iRibbonCategory is null || String.IsNullOrEmpty(iRibbonCategory.CategoryId)) return null;

            var changeMode = pageChangeMode;
            DxRibbonPageCategory pageCategory = PageCategories.GetCategoryByName(iRibbonCategory.CategoryId) as DxRibbonPageCategory;

            if (HasCreate(changeMode))
            {
                if (pageCategory is null)
                    pageCategory = CreatePageCategory(iRibbonCategory);
                pageCategory.Tag = iRibbonCategory;
            }

            return pageCategory;
        }
        /// <summary>
        /// Vytvoří new kategorii, naplní a vrátí ji
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <returns></returns>
        protected DxRibbonPageCategory CreatePageCategory(IRibbonCategory iRibbonCategory)
        {
            DxRibbonPageCategory pageCategory = new DxRibbonPageCategory(iRibbonCategory.CategoryText, iRibbonCategory.CategoryColor, iRibbonCategory.CategoryVisible);
            pageCategory.Name = iRibbonCategory.CategoryId;
            pageCategory.Tag = iRibbonCategory;
            PageCategories.Add(pageCategory);
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
        /// <returns></returns>
        public DxRibbonPage CreatePage(IRibbonPage iRibbonPage)
        {
            return CreatePage(iRibbonPage, this.Pages);
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí stránku pro daná data.
        /// Stránku přidá do this Ribbonu nebo do dané kategorie.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="pageCategory"></param>
        /// <returns></returns>
        protected DxRibbonPage GetPage(IRibbonPage iRibbonPage, DxRibbonPageCategory pageCategory = null)
        {
            if (iRibbonPage is null) return null;
            bool isCategory = !(pageCategory is null);
            var pageCollection = (isCategory ? pageCategory.Pages : this.Pages);

            var changeMode = iRibbonPage.ChangeMode;
            DxRibbonPage page = pageCollection.FirstOrDefault(r => (r.Name == iRibbonPage.PageId)) as DxRibbonPage;
            if (HasCreate(changeMode))
            {
                if (page is null)
                    page = CreatePage(iRibbonPage, pageCollection);
                if (HasReFill(changeMode))
                    ClearPage(page);
                page.Tag = iRibbonPage;
                page.PageData = iRibbonPage;
            }
            else if (HasRemove(changeMode))
            {
                RemovePage(page, pageCollection);
            }

            return page;
        }
        /// <summary>
        /// Vygeneruje new Page a zařadí do patřičného parenta (kategorie / Ribbon.Pages)
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="pageCollection"></param>
        /// <returns></returns>
        protected DxRibbonPage CreatePage(IRibbonPage iRibbonPage, DevExpress.XtraBars.Ribbon.RibbonPageCollection pageCollection)
        {
            DxRibbonPage page = new DxRibbonPage(this, iRibbonPage.PageText)
            {
                Name = iRibbonPage.PageId,
                Tag = iRibbonPage
            };
            pageCollection.Add(page);
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
        /// <param name="pageCollection"></param>
        protected void RemovePage(DxRibbonPage page, DevExpress.XtraBars.Ribbon.RibbonPageCollection pageCollection)
        {
            if (page != null && pageCollection != null && pageCollection.Contains(page))
                pageCollection.Remove(page);
        }

        /// <summary>
        /// Vytvoří a vrátí grupu daného jména, grupu vloží do dané stránky.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public DxRibbonGroup CreateGroup(string text, DxRibbonPage page)
        {
            var group = new DxRibbonGroup(text)
            {
                State = DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto
            };
            if (page != null) page.Groups.Add(group);
            return group;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí grupu pro daná data.
        /// Grupu přidá do dané stránky.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        protected DxRibbonGroup GetGroup(IRibbonGroup iRibbonGroup, DxRibbonPage page)
        {
            if (iRibbonGroup is null || page is null) return null;

            var changeMode = iRibbonGroup.ChangeMode;
            DxRibbonGroup group = page.Groups.GetGroupByName(iRibbonGroup.GroupId) as DxRibbonGroup;
            if (HasCreate(changeMode))
            {
                if (group is null)
                    group = CreateGroup(iRibbonGroup, page);
                if (HasReFill(changeMode))
                    ClearGroup(group);
                group.Tag = iRibbonGroup;
            }
            else if (HasRemove(changeMode))
            {
                RemoveGroup(group, page);
            }

            return group;
        }
        /// <summary>
        /// Vytvoří a vrátí grupu
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        protected DxRibbonGroup CreateGroup(IRibbonGroup iRibbonGroup, DxRibbonPage page)
        {
            var group = new DxRibbonGroup(iRibbonGroup.GroupText)
            {
                Name = iRibbonGroup.GroupId,
                CaptionButtonVisible = (iRibbonGroup.GroupButtonVisible ? DefaultBoolean.True : DefaultBoolean.False),
                State = DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto,
                Tag = iRibbonGroup
            };
            group.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(iRibbonGroup.GroupImage, ImagesSize, iRibbonGroup.GroupText);
            if (page != null) page.Groups.Add(group);
            return group;
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
        /// Odstraní grupu ze stránky
        /// </summary>
        /// <param name="group"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        protected void RemoveGroup(DxRibbonGroup group, DxRibbonPage page)
        {
            if (group != null && page != null && page.Groups.Contains(group))
                page.Groups.Remove(group);
        }

        /// <summary>
        /// Vytvoří a vrátí prvek dle definice.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="group"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.BarItem CreateItem(IRibbonItem iRibbonItem, DevExpress.XtraBars.Ribbon.RibbonPageGroup group, DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            int count = 0;
            var barItem = CreateItem(iRibbonItem, true, ref count);

            if (barItem is null) return null;
            if (group != null)
            {
                var barLink = group.ItemLinks.Add(barItem);
                barLink.BeginGroup = iRibbonItem.ItemIsFirstInGroup;
            }
            FillBarItem(barItem, iRibbonItem);

            if (clickHandler != null) barItem.ItemClick += clickHandler;

            return barItem;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí BarItem pro daná data.
        /// BarItem přidá do dané grupy.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="group"></param>
        /// <param name="isNeedQAT">Přidávat pouze prvky označené QAT (stránka jako celek je v režimu LazyContent, ale prvky QAT potřebujeme i v této stránce)</param>
        /// <param name="count"></param>
        /// <param name="reallyCreateSubItems"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem GetItem(IRibbonItem iRibbonItem, DevExpress.XtraBars.Ribbon.RibbonPageGroup group, bool isNeedQAT, ref int count, bool reallyCreateSubItems = false)
        {
            if (iRibbonItem is null || group is null) return null;

            var changeMode = iRibbonItem.ChangeMode;
            DevExpress.XtraBars.BarItem barItem = Items[iRibbonItem.ItemId];
            if (HasCreate(changeMode))
            {
                if (HasReFill(changeMode) && barItem != null)
                { }
                if (barItem is null)
                    barItem = CreateItem(iRibbonItem, reallyCreateSubItems, ref count);
                if (HasReFill(changeMode))
                {
                    //  ClearItem(barItem);
                }
               
                if (barItem is null) return null;
                var barLink = group.ItemLinks.Add(barItem);
                barLink.BeginGroup = iRibbonItem.ItemIsFirstInGroup;
                FillBarItem(barItem, iRibbonItem);

                // Některé druhy prvků (například Menu) už mají Tag naplněn "něčím lepším", tak to nebudeme ničit:
                if (barItem.Tag == null) barItem.Tag = iRibbonItem;
            }
            else if (HasRemove(changeMode))
            {

            }

            // Prvek patří do QAT?
            if (DefinedInQAT(iRibbonItem.ItemId))
                this.AddBarItemToQatList(barItem, iRibbonItem);

            return barItem;
        }
        /// <summary>
        /// Vytvoří prvek BarItem pro daná data a vrátí jej.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="reallyCreateSubItems">Skutečně se mají vytvářet SubMenu?</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem CreateItem(IRibbonItem iRibbonItem, bool reallyCreateSubItems, ref int count)
        {
            DevExpress.XtraBars.BarItem barItem = null;
            switch (iRibbonItem.RibbonItemType)
            {
                case RibbonItemType.ButtonGroup:
                    count++;
                    DevExpress.XtraBars.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(iRibbonItem, iRibbonItem.SubRibbonItems, reallyCreateSubItems, ref count));
                    buttonGroup.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    count++;
                    var dxPopup = CreateXPopupMenu(iRibbonItem, iRibbonItem.SubRibbonItems, reallyCreateSubItems, ref count);
                    DevExpress.XtraBars.BarButtonItem splitButton = Items.CreateSplitButton(iRibbonItem.Text, dxPopup);
                    barItem = splitButton;
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
                case RibbonItemType.Menu:
                    count++;
                    DevExpress.XtraBars.BarSubItem menu = Items.CreateMenu(iRibbonItem.Text);
                    PrepareXBarMenu(iRibbonItem, iRibbonItem.SubRibbonItems, menu, reallyCreateSubItems, ref count);
                    barItem = menu;
                    break;
                case RibbonItemType.InRibbonGallery:
                    count++;
                    var galleryItem = CreateGalleryItem(iRibbonItem);
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
                FillBarItem(barItem, iRibbonItem);
            }
            return barItem;
        }
        /// <summary>
        /// Do daného prvku vepíše data z definice
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        protected void FillBarItem(DevExpress.XtraBars.BarItem barItem, IRibbonItem iRibbonItem)
        {
            if (iRibbonItem.Text != null)
                barItem.Caption = iRibbonItem.Text;

            barItem.Enabled = iRibbonItem.Enabled;

            string imageName = iRibbonItem.Image;
            if (imageName != null && !(barItem is DxBarCheckBoxToggle))           // DxCheckBoxToggle si řídí Image sám
            {
                if (DxComponent.TryGetResourceExtension(imageName, out var _))
                {
                    DxComponent.ApplyImage(barItem.ImageOptions, resourceName: imageName);
                }
                else
                {
                    barItem.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(imageName, ImagesSize, iRibbonItem.Text);
                    barItem.LargeImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(imageName, LargeImagesSize, iRibbonItem.Text);
                }
            }

            if (!string.IsNullOrEmpty(iRibbonItem.HotKey))
            {
                if (!(barItem is DevExpress.XtraBars.BarSubItem))
                    barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(WinFormServices.KeyboardHelper.GetShortcutFromServerHotKey(iRibbonItem.HotKey));
                // else
                //    ComponentConnector.ShowWarningToDeveloper($"Setup keyboard shortcut {item.HotKey} to non button barItem {barItem.Name}. This is {barItem.GetType().Name}");
            }

            if (barItem is DevExpress.XtraBars.BarCheckItem checkItem)
            {   // Do CheckBoxu vepisujeme víc vlastností:
                checkItem.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
                checkItem.CheckStyle =
                    (iRibbonItem.RibbonItemType == RibbonItemType.RadioItem ? DevExpress.XtraBars.BarCheckStyles.Radio :
                    (iRibbonItem.RibbonItemType == RibbonItemType.CheckBoxToggle ? DevExpress.XtraBars.BarCheckStyles.Standard :
                     DevExpress.XtraBars.BarCheckStyles.Standard));
                checkItem.Checked = iRibbonItem.Checked ?? false;
            }

            if (barItem is DxBarCheckBoxToggle dxCheckBoxToggle)
            {
                dxCheckBoxToggle.Checked = iRibbonItem.Checked;
                if (iRibbonItem.Image != null) dxCheckBoxToggle.ImageNameNull = iRibbonItem.Image;
                if (iRibbonItem.ImageUnChecked != null) dxCheckBoxToggle.ImageNameUnChecked = iRibbonItem.ImageUnChecked;
                if (iRibbonItem.ImageChecked != null) dxCheckBoxToggle.ImageNameChecked = iRibbonItem.ImageChecked;
            }

            barItem.PaintStyle = Convert(iRibbonItem.ItemPaintStyle);
            if (iRibbonItem.RibbonStyle != RibbonItemStyles.Default)
                barItem.RibbonStyle = Convert(iRibbonItem.RibbonStyle);

            if (iRibbonItem.ToolTipText != null)
                barItem.SuperTip = DxComponent.CreateDxSuperTip(iRibbonItem);

            if (barItem.Tag == null)
                // Některé druhy prvků (například Menu) už mají Tag naplněn "něčím lepším", tak to nebudeme ničit:
                barItem.Tag = iRibbonItem;
        }

        protected DevExpress.XtraBars.BarBaseButtonItem[] GetBarBaseButtons(IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarBaseButtonItem> baseButtons = new List<DevExpress.XtraBars.BarBaseButtonItem>();
            if (subItems != null && reallyCreate)
            {
                foreach (IRibbonItem subItem in subItems)
                {
                    subItem.ParentItem = parentItem;
                    subItem.ParentGroup = parentItem.ParentGroup;
                    DevExpress.XtraBars.BarBaseButtonItem baseButton = CreateBaseButton(subItem);
                    if (baseButton != null)
                    {
                        baseButton.Tag = subItem;
                        baseButtons.Add(baseButton);
                    }
                }
            }
            return baseButtons.ToArray();
        }
        /// <summary>
        /// Vytvoří a vrátí jednoduchý Button
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarBaseButtonItem CreateBaseButton(IRibbonItem iRibbonItem)
        {
            DevExpress.XtraBars.BarBaseButtonItem baseButton = null;
            switch (iRibbonItem.RibbonItemType)
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
                FillBarItem(baseButton, iRibbonItem);
            }
            return baseButton;
        }

        // PopupMenu pro SplitButton:
        /// <summary>
        /// Vytvoří a vrátí objekt <see cref="DevExpress.XtraBars.PopupMenu"/>, který se používá pro prvek typu <see cref="RibbonItemType.SplitButton"/> jako jeho DropDown menu
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.PopupMenu CreateXPopupMenu(IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, bool reallyCreate, ref int count)
        {
            DevExpress.XtraBars.PopupMenu xPopupMenu = new DevExpress.XtraBars.PopupMenu(BarManager);
            if (subItems != null)
            {
                if (reallyCreate)
                {   // Vytvořit menu hned:
                    _XPopupMenu_FillItems(xPopupMenu, parentItem, subItems, ref count);
                }
                else
                {   // Vytvořit až bude třeba (BeforePopup):
                    xPopupMenu.Tag = new LazySubItemsInfo(parentItem, subItems);
                    xPopupMenu.BeforePopup += _XPopupMenu_BeforePopup;
                }
            }
            return xPopupMenu;
        }
        /// <summary>
        /// Událost před otevřením <see cref="DevExpress.XtraBars.PopupMenu"/> (je použito pro Split Buttonu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _XPopupMenu_BeforePopup(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!(sender is DevExpress.XtraBars.PopupMenu dxPopup)) return;
            dxPopup.BeforePopup -= _XPopupMenu_BeforePopup;
            if (!(dxPopup.Tag is LazySubItemsInfo lazySubItems)) return;
            dxPopup.Tag = null;                            // dxPopup není prvek Ribbonu, ale PopupMenu navázané na SplitButtonu. Jeho Tag nechť je null, protože definující prvek IRibbonItem je v Tagu toho SplitButtonu.

            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            _XPopupMenu_FillItems(dxPopup, lazySubItems.ParentItem, lazySubItems.SubItems, ref count);
            if (LogActive) DxComponent.LogAddLineTime($"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.PopupMenu"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu tvorby menu (při inicializaci nebo při BeforePopup v LazyInit modu)
        /// </summary>
        /// <param name="dxPopup"></param>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="count"></param>
        private void _XPopupMenu_FillItems(DevExpress.XtraBars.PopupMenu dxPopup, IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, ref int count)
        {
            foreach (IRibbonItem subItem in subItems)
            {
                subItem.ParentItem = parentItem;
                subItem.ParentGroup = parentItem.ParentGroup;
                DevExpress.XtraBars.BarItem barItem = CreateItem(subItem, true, ref count);
                if (barItem != null)
                {
                    barItem.Tag = subItem;
                    var barLink = dxPopup.AddItem(barItem);
                    if (subItem.ItemIsFirstInGroup) barLink.BeginGroup = true;
                }
            }
        }

        // BarSubItem pro Menu
        /// <summary>
        /// Naplní položky do daného menu <see cref="DevExpress.XtraBars.BarSubItem"/>, používá se pro prvek typu <see cref="RibbonItemType.Menu"/>
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="xBarMenu"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        private void PrepareXBarMenu(IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, DevExpress.XtraBars.BarSubItem xBarMenu, bool reallyCreate, ref int count)
        {
            if (parentItem.SubItems != null)
            {
                if (reallyCreate)
                {
                    _XBarMenu_FillItems(xBarMenu, parentItem, subItems, ref count);
                }
                else
                {
                    xBarMenu.AddItem(new DevExpress.XtraBars.BarButtonItem(this.BarManager, "..."));     // Musí tu být alespoň jeden prvek, jinak při kliknutí na Menu se nebude nic dít (neproběhne event xBarMenu.Popup)
                    xBarMenu.Tag = new LazySubItemsInfo(parentItem, subItems);
                    xBarMenu.Popup += _XBarMenu_BeforePopup;
                }
            }
        }
        /// <summary>
        /// Událost před otevřením <see cref="DevExpress.XtraBars.BarSubItem "/> (je použito pro Menu Button)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _XBarMenu_BeforePopup(object sender, EventArgs e)
        {
            if (!(sender is DevExpress.XtraBars.BarSubItem xBarMenu)) return;
            xBarMenu.Popup -= _XBarMenu_BeforePopup;
            if (!(xBarMenu.Tag is LazySubItemsInfo lazySubItems)) return;
            xBarMenu.Tag = lazySubItems.ParentItem;        // Tady je xBarMenu = přímo prvek Ribbonu, a tam chci mít v Tagu referenci na IRibbonItem, který prvek založil...

            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            xBarMenu.ItemLinks.Clear();                    // V téhle kolekci byl jeden prvek "...", který mi zajistil aktivaci menu = zdejší metoda Popup.
            _XBarMenu_FillItems(xBarMenu, lazySubItems.ParentItem, lazySubItems.SubItems, ref count);
            if (LogActive) DxComponent.LogAddLineTime($"LazyLoad Menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.BarSubItem"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu tvorby menu (při inicializaci nebo při BeforePopup v LazyInit modu)
        /// </summary>
        /// <param name="xBarMenu"></param>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="count"></param>
        private void _XBarMenu_FillItems(DevExpress.XtraBars.BarSubItem xBarMenu, IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, ref int count)
        {
            var menuItems = GetBarSubItems(parentItem, subItems, true, ref count);
            foreach (var menuItem in menuItems)
            {
                var menuLink = xBarMenu.AddItem(menuItem);
                if ((menuItem.Tag is IRibbonItem ribbonData) && ribbonData.ItemIsFirstInGroup)
                    menuLink.BeginGroup = true;
            }
        }
        /// <summary>
        /// Metoda vytvoří a vrátí pole prvků <see cref="DevExpress.XtraBars.BarItem"/> pro daný prvek Parent a dané pole definic SubItems.
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem[] GetBarSubItems(IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (subItems != null && reallyCreate)
            {
                foreach (IRibbonItem subItem in subItems)
                {
                    subItem.ParentItem = parentItem;
                    subItem.ParentGroup = parentItem.ParentGroup;
                    DevExpress.XtraBars.BarItem barItem = CreateItem(subItem, true, ref count);
                    if (barItem != null)
                    {   // tohle není opakovaně potřeba, to zařizuje Ribbon nativně!  ...   barItem.ItemClick += this.RibbonControl_SubItemClick;
                        barItem.Tag = subItem;
                        barItems.Add(barItem);
                    }
                }
            }
            return barItems.ToArray();
        }

        // Gallery
        /// <summary>
        /// Vytvoří a vrátí Galerii buttonů
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.RibbonGalleryBarItem CreateGalleryItem(IRibbonItem iRibbonItem)
        {
            var galleryBarItem = new DevExpress.XtraBars.RibbonGalleryBarItem(this.BarManager);
            galleryBarItem.Gallery.Images = ComponentConnector.GraphicsCache.GetImageList();
            galleryBarItem.Gallery.HoverImages = ComponentConnector.GraphicsCache.GetImageList();
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
            foreach (var subRibbonItem in iRibbonItem.SubRibbonItems)
                items.Add(CreateGallerySubItem(iRibbonItem, subRibbonItem));

            galleryGroup.Items.AddRange(items.ToArray());

            return galleryBarItem;
        }

        private void GalleryBarItem_GalleryItemClick(object sender, DevExpress.XtraBars.Ribbon.GalleryItemClickEventArgs e)
        {
            if (!(e.Item?.Tag is IRibbonItem iRibbonItem)) return;
            this.RaiseRibbonItemClick(iRibbonItem);
        }
        /// <summary>
        /// Vytvoří a vrátí jeden prvek galerie
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="iRibbonItem"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.Ribbon.GalleryItem CreateGallerySubItem(IRibbonItem parentItem, IRibbonItem iRibbonItem)
        {
            var galleryItem = new DevExpress.XtraBars.Ribbon.GalleryItem();
            galleryItem.ImageIndex = galleryItem.HoverImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(iRibbonItem.Image);
            galleryItem.Caption = iRibbonItem.Text;
            galleryItem.Checked = iRibbonItem.Checked ?? false;
            galleryItem.Description = iRibbonItem.ToolTipText;
            galleryItem.Enabled = iRibbonItem.Enabled;
            galleryItem.SuperTip = DxComponent.CreateDxSuperTip(iRibbonItem);
            galleryItem.Tag = iRibbonItem;
            iRibbonItem.ParentItem = parentItem;
            iRibbonItem.ParentGroup = parentItem.ParentGroup;
            return galleryItem;
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
            /// <param name="subItems"></param>
            public LazySubItemsInfo(IRibbonItem parentItem, IEnumerable<IRibbonItem> subItems)
            {
                this.ParentItem = parentItem;
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
            /// Prvek definující Button = Parent celého menu
            /// </summary>
            public IRibbonItem ParentItem { get; private set; }
            /// <summary>
            /// SubPrvky
            /// </summary>
            public IEnumerable<IRibbonItem> SubItems { get; private set; }
        }
        private void RibbonControl_SubItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            RibbonControl_ItemClick(sender, e);
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
        /// Standardized small image size.
        /// </summary>
        internal static readonly WinFormServices.Drawing.UserGraphicsSize ImagesSize = WinFormServices.Drawing.UserGraphicsSize.Small;
        /// <summary>
        /// Standardized large image size.
        /// </summary>
        internal static readonly WinFormServices.Drawing.UserGraphicsSize LargeImagesSize = WinFormServices.Drawing.UserGraphicsSize.Large;
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
         
        */
        #region Základní evidence prvků QAT : string QATItemKeys, List a Dictionary, konverze
        /// <summary>
        /// Obsahuje klíče prvků, které mají být / jsou zobrazeny v QAT (Quick Access Toolbar).
        /// Jednotlivé klíče jsou odděleny znakem <see cref="QATItemKeyDelimiter"/> (=tabulátorem).
        /// </summary>
        public string QATItemKeys { get { return _GetQATItemKeys(); }  set { this._SetQATItemKeys(value); }  }
        /// <summary>
        /// Oddělovač jednotlivých klíčů v <see cref="QATItemKeys"/> (=tabulátor)
        /// </summary>
        public static string QATItemKeyDelimiter { get { return "\t"; } }
        /// <summary>
        /// Metoda vrátí platný QAT klíč
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        protected static string GetValidQATKey(string itemId)
        {
            if (itemId == null) return "";
            return itemId.Trim().Replace(QATItemKeyDelimiter, " ");
        }
        /// <summary>
        /// Resetuje pole <see cref="_QatItems"/>, odebere kadžý prvek Link z reálného toolbaru QAT (pokud tam je) = vyprázdní se pole i toolbar.
        /// Používá se při setování nového seznamu do <see cref="QATItemKeys"/>.
        /// </summary>
        private void ResetQuickAccessToolbar()
        {
            if (_QatItems != null)
            {
                _QatItems.ForEachExec(q => q.Reset());
                _QatItems = null;
            }
            _QatItemDict = null;
        }
        /// <summary>
        /// Vrací sumární string z klíčů všech prvků v QAT
        /// </summary>
        /// <returns></returns>
        private string _GetQATItemKeys()
        {
            if (_QatItems == null) return null;
            return _QatItems.Select(q => q.Key).ToOneString(QATItemKeyDelimiter);
        }
        /// <summary>
        /// Ze zadaného stringu vytvoří struktury pro evidenci prvků pro toolbar QAT (pole <see cref="_QatItems"/> a <see cref="_QatItemDict"/>).
        /// Před tím zruší obsah fyzického QAT.
        /// </summary>
        /// <param name="qatItemKeys"></param>
        private void _SetQATItemKeys(string qatItemKeys)
        {
            ResetQuickAccessToolbar();

            _QatItems = new List<QatItem>();
            _QatItemDict = new Dictionary<string, QatItem>();
            foreach (string itemId in qatItemKeys.Split(QATItemKeyDelimiter[0]))
            {
                string key = GetValidQATKey(itemId);
                if (key == "") continue;
                if (_QatItemDict.ContainsKey(key)) continue;
                QatItem qatItem = new QatItem(this, key);
                _QatItems.Add(qatItem);
                _QatItemDict.Add(key, qatItem);
            }
        }
        private List<QatItem> _QatItems;
        private Dictionary<string, QatItem> _QatItemDict;
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
            return (ExistsAnyQat && iRibbonItem != null && (DefinedInQAT(iRibbonItem.ItemId) || (iRibbonItem.SubRibbonItems != null && iRibbonItem.SubRibbonItems.Any(s => ContainsQAT(s)))));
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
            return _QatItemDict?.ContainsKey(key) ?? false;
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně existuje alespoň jeden záznam QAT v poli <see cref="_QatItems"/>
        /// </summary>
        protected bool ExistsAnyQat { get { return ((_QatItems?.Count ?? 0) > 0); } }
        /// <summary>
        /// Metoda je volána v procesu tvorby nových prvků Ribbonu, když je vytvořen prvek BarItem, který má být obsažen v QAT.
        /// Tato metoda si zaeviduje odkaz na tento BarItem v interním poli, z něhož následně (v metodě <see cref="AddQatListToRibbon()"/>) 
        /// všechny patřičné prvky uloží do fyzického ToolBaru QAT.
        /// Tato metoda tedy dodaný prvek nevloží okamžitě do ToolBaru.
        /// Tato metoda, pokud je volána pro prvek který v QAT nemá být, nepřidá tento prvek 
        /// nad rámec požadovaného seznamu prvků v QAT = <see cref="QATItemKeys"/> (nepřidává nové prvky do pole <see cref="_QatItems"/> a <see cref="_QatItemDict"/>).
        /// <para/>
        /// Duplicita: Pokud na vstupu bude prvek, pro jehož klíč už evidujeme prvek dřívější, pak nový prvek vložíme namísto prvku původního.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="ribbonItem"></param>
        private void AddBarItemToQatList(DevExpress.XtraBars.BarItem barItem, IRibbonItem ribbonItem)
        {
            if (barItem == null || ribbonItem == null) return;
            string key = GetValidQATKey(ribbonItem.ItemId);
            if (!_QatItemDict.TryGetValue(key, out var qatItem)) return;       // Pro daný prvek Ribbonu nemáme záznam, že bychom měli přidat prvek do QAT.
            qatItem.BarItem = barItem;
        }
        /// <summary>
        /// Volá se na závěr přidávání dat do stránek, do fyzického ToolBaru QAT (Quick Access Toolbar) 
        /// vepíše všechny aktuálně přítomné prvky z <see cref="_QatItems"/>.
        /// </summary>
        protected void AddQatListToRibbon()
        {
            var qatItems = _QatItems;
            if (qatItems == null) return;

            // Pokud v seznamu není žádný prvek, který potřebuje provést změnu, tak nebudu toolbarem mrkat:
            if (!qatItems.Any(q => q.NeedChangeInQat)) return;

            foreach (var qatItem in qatItems.Where(q => q.NeedRemoveFromQat))
                qatItem.RemoveBarItemLink();

            foreach (var qatItem in qatItems.Where(q => q.NeedAddToQat))
                qatItem.AddLinkToQat();
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
                var ribbonItems = itemsToDelete.Select(i => i.Tag).OfType<IRibbonItem>().ToArray();
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
            var items = iRibbonItem.SubRibbonItems;
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
            if (key != null && this._QatItemDict.TryGetValue(key, out var qatItem))
                qatItem.RemoveBarItemLink();
        }
        #endregion
        #region Uživatelská interaktivita - pozor, je nutno řešit odlišnosti pro mergované prvky
        /// <summary>
        /// Inicializace dat a eventhandlerů pro QAT
        /// </summary>
        private void InitQuickAccessToolbar()
        {
            this.SourceToolbar.LinksChanged += SourceToolbar_LinksChanged;
            this.QATItemKeys = "";               // Tím vytvořím struktury pro QAT
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
            if (!_QatItemDict.ContainsKey(key))
            {   // Neznáme => přidat:
                QatItem qatItem = new QatItem(this, key, iRibbonItem, iRibbonGroup, link.Item, link);
                _QatItemDict.Add(key, qatItem);
                _QatItems.Add(qatItem);
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
                if (_QatItemDict.TryGetValue(key, out var qatItem))
                {   // Známe => odebrat:
                    qatItem.Reset();
                    _QatItemDict.Remove(key);
                    _QatItems.RemoveAll(q => q.Key == key);
                    _RunQATItemKeysChanged();
                }
            }
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
                if (tag is IRibbonItem iItem)
                {
                    key = GetValidQATKey(iItem.ItemId);
                    iRibbonItem = iItem;
                    iRibbonGroup = null;
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
        /// Došlo ke změně v obsahu <see cref="QATItemKeys"/>, zavolej události
        /// </summary>
        private void _RunQATItemKeysChanged()
        {
            if (this.CustomizationPopupMenu.Visible)
                this.CustomizationPopupMenu.HidePopup();

            OnQATItemKeysChanged();
            QATItemKeysChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je provedeno po změně hodnoty v <see cref="QATItemKeys"/>
        /// </summary>
        protected virtual void OnQATItemKeysChanged() { }
        /// <summary>
        /// Je voláno po změně hodnoty v <see cref="QATItemKeys"/>
        /// </summary>
        public event EventHandler QATItemKeysChanged;
        #endregion
        #region class QatItem : evidence pro jedno tlačítko QAT
        /// <summary>
        /// Třída pro průběžné shrnování informací o pvcích, které mají být umístěny do QAT (Quick Access Toolbar)
        /// </summary>
        protected class QatItem
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
            /// <param name="key"></param>
            /// <param name="iRibbonItem"></param>
            /// <param name="iRibbonGroup"></param>
            /// <param name="barItem"></param>
            /// <param name="barItemLink"></param>
            public QatItem(DxRibbonControl owner, string key, IRibbonItem iRibbonItem, IRibbonGroup iRibbonGroup, DevExpress.XtraBars.BarItem barItem, DevExpress.XtraBars.BarItemLink barItemLink)
                : this(owner, key)
            {
                RibbonItem = iRibbonItem;
                RibbonGroup = iRibbonGroup;
                _BarItem = barItem;
                BarItemLink = barItemLink;
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
                RibbonGroup = ribbonGroup;
                _BarItem = barItem;
                BarItemLink = barItemLink;
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
            public DevExpress.XtraBars.BarItemLink BarItemLink { get; private set; }
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
            /// Obsahuje true, když tento prvek máme odebrat z QAT. Tj. máme odpovídající BarItemLink. Bez ohledu na BarItem nebo BarGroup.
            /// </summary>
            public bool NeedRemoveFromQat
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
            public void AddLinkToQat()
            {
                this.RemoveBarItemLink();
                if (this.BarItem != null)
                    this.BarItemLink = _Owner.Toolbar.ItemLinks.Add(this.BarItem);
            }
            /// <summary>
            /// Metoda zajistí, že pokud tento prvek je zobrazen ve fyzickém QAT, pak z něj bude odebrán, Disposován a z tohoto objektu vynulován.
            /// </summary>
            public void RemoveBarItemLink()
            {
                var barLink = this.BarItemLink;
                if (barLink != null)
                {
                    if (_Owner != null)
                        _Owner.Toolbar.ItemLinks.Remove(barLink);
                    // není vždy OK: barLink.Dispose();
                    this.BarItemLink = null;
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
            iRibbonItem?.MenuAction?.Invoke(iRibbonItem);
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
                             .OfType<IRibbonItem>()
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
        /// Obsahuje všechny aktuální Ribbony mergované od this až do nejvyššího Parenta, včetně this.
        /// this ribbon je na pozici [0], jeho Parent je na pozici [1], a tak dál nahoru. Pole tedy má vždy alespoň jeden prvek.
        /// Každý další prvek je Parent prvku předchozího.
        /// <para/>
        /// Používá se při skupinový akcích typu "Odmerguje mě ze všech parentů, Oprav mě a pak mě zase Mererguj nazpátek".
        /// </summary>
        public List<DxRibbonControl> MergedRibbonsUp
        {
            get
            {
                var parent = this.MergedIntoParentDxRibbon;
                if (parent == null) return new List<DxRibbonControl>() { this };         // Zkratka
                List<DxRibbonControl> result = new List<DxRibbonControl>();
                result.Add(this);
                while (parent != null)
                {
                    result.Add(parent);
                    parent = parent.MergedIntoParentDxRibbon;
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
        /// </summary>
        /// <param name="parentDxRibbon"></param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        public void MergeCurrentDxToParent(DxRibbonControl parentDxRibbon, bool? forceSelectChildPage = null)
        {
            if (parentDxRibbon != null)
                parentDxRibbon.MergeChildDxRibbon(this, forceSelectChildPage);
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
        /// </summary>
        /// <param name="childDxRibbon"></param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        public void MergeChildDxRibbon(DxRibbonControl childDxRibbon, bool? forceSelectChildPage = null)
        {
            // Pokud nyní mám v sobě mergován nějaký Child Ribbon, pak jej UnMergujeme teď = jinak v tom bude guláš:
            if (this.MergedChildRibbon != null)
                this.UnMergeDxRibbon();

            // Pokud já jsem nyní Mergován v nějakém Parentu, pak sebe z něj musím nejprve odmergovat, a pak - s novým obsahem - zase zpátky mergovat do něj:
            var parentRibbon = MergedIntoParentDxRibbon;
            if (parentRibbon != null)
            {
                parentRibbon.UnMergeDxRibbon();
                forceSelectChildPage = true;                         // Protože se budu mergovat zpátky, chci mít opět vybranou svoji SelectedPage!
            }

            // Nyní do sebe mergujeme nově dodaný obsah:
            if (childDxRibbon != null)
                this.MergeChildRibbon(childDxRibbon, forceSelectChildPage);    // Tady se do Child DxRibbonu vepíše, že jeho MergedIntoParentDxRibbon je this

            // A pokud jí jsem byl mergován nahoru, tak se nahoru zase vrátím:
            if (parentRibbon != null)
                parentRibbon.MergeChildDxRibbon(this, forceSelectChildPage);   // Tady se může rozběhnout rekurze ve zdejší metodě až do instance Top Parenta...
        }
        /// <summary>
        /// Provede Mergování daného Child Ribbonu do this (Parent) Ribbonu.
        /// Tato metoda neprovádí žádné další akce, pouze dovoluje explicitně určit režim SelectChildPage: 
        /// pokud bude v parametru <paramref name="forceSelectChildPage"/> zadána hodnota, pak bude akceptována namísto <see cref="SelectChildActivePageOnMerge"/>.
        /// <para/>
        /// Vedle toho existuje komplexnější metoda <see cref="MergeChildDxRibbon(DxRibbonControl, bool?)"/>, která dokáže mergovat Child Ribbon až do aktuálního top parenta.
        /// </summary>
        /// <param name="childRibbon">Mergovaný Child Ribbon</param>
        /// <param name="forceSelectChildPage">Povinně selectovat Child SelectedPage v parentu, bez ohledu na hodnotu <see cref="SelectChildActivePageOnMerge"/></param>
        public void MergeChildRibbon(DevExpress.XtraBars.Ribbon.RibbonControl childRibbon, bool? forceSelectChildPage = null)
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
        /// </summary>
        /// <param name="childRibbon"></param>
        public override void MergeRibbon(DevExpress.XtraBars.Ribbon.RibbonControl childRibbon)
        {
            var startTime = DxComponent.LogTimeCurrent;

            bool selectPage = _CurrentSelectChildActivePageOnMerge ?? SelectChildActivePageOnMerge;  // Rád bych si předal _CurrentSelectChildActivePageOnMerge jako parametr, ale tady to nejde, jsme override bázové metody = bez toho parametru.
            string slaveSelectedPage = (selectPage && childRibbon is DxRibbonControl dxRibbon) ? dxRibbon.SelectedPageId : null;

            base.MergeRibbon(childRibbon);
            this.MergedChildRibbon = childRibbon;
            if (childRibbon is DxRibbonControl childDxRibbon)
                childDxRibbon.MergedIntoParentDxRibbon = this;

            if (selectPage && slaveSelectedPage != null) this.SelectedPageId = slaveSelectedPage;

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
        /// </summary>
        public void UnMergeCurrentDxFromParent()
        {
            _UnmergeModifyMergeCurrentRibbon(null, false);
        }
        /// <summary>
        /// Zajistí korektní stav this Ribbonu (=že bude UnMerged) pro danou akci, která chce modifikovat this Ribbon;
        /// Následně spustí danou akci; 
        /// A po dané akci zase vrátí this Ribbon pomocí mergování do původní struktury parentů.
        /// <para/>
        /// Pokud this Ribbon aktuálně není nikam Mergován, pak se provede zadaná akce bez zbytečných dalších režií.
        /// <para/>
        /// Tedy proběhne UnMerge celé hierarchie Ribbonů od TopParenta až k našemu Parentu;
        /// Pak se provede zadaná akce;
        /// A na konec se this Ribbon zase Merguje do celé původní hierarchie (pokud v ní na začátku byl).
        /// <para/>
        /// Toto je jediná cesta jak korektně modifikovat obsah Ribbonu v situaci, když je Mergován nahoru.
        /// Režie s tím spojená je relativně snesitelná, lze počítat 8 milisekund na jednu úroveň (součet za UnMerge a na konci zase Merge).
        /// </summary>
        public void ModifyCurrentDxContent(Action action)
        {
            _UnmergeModifyMergeCurrentRibbon(action, true);
        }
        /// <summary>
        /// Provede odmergování this Ribbonu z Parenta, ale pokud Parent je mergován ve vyšším parentu, zajistí jeho ponechání tam.
        /// Není to triviální věc, protože pokud this (1) je mergován v Parentu (2) , a Parent (2) v ještě vyšším Parentu (3),
        /// pak se nejprve musí odebrat (2) z (3), pak (1) z (2) a pak zase vrátit (2) do (3).
        /// </summary>
        private void _UnmergeModifyMergeCurrentRibbon(Action action, bool mergeBack)
        {
            var startTime = DxComponent.LogTimeCurrent;

            var ribbonsUp = this.MergedRibbonsUp;
            int count = ribbonsUp.Count;

            // Pokud this Ribbon není nikam mergován (když počet mergovaných je 1):
            if (count == 1)
            {   // Provedu požadovanou akci rovnou (není třeba dělat UnMerge a Merge), a skončíme:
                _RunLogAction(action);
                if (LogActive) DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: Current; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Odmergovat - Provést akci (pokud je) - Mergovat zpátky (vše nebo jen UpRibbony):
            int last = count - 1;
            var topRibbon = ribbonsUp[last];
            var topRibbonSelectedPage1 = topRibbon.SelectedPageId;

            try
            {
                // Top Ribbon pozastaví svoji práci:
                topRibbon.BarManager.BeginUpdate();

                // Všem Ribonům v řadě potlačíme CheckLazyContentEnabled:
                ribbonsUp.ForEach(r => r.CheckLazyContentEnabled = false);

                // UnMerge proběhne od posledního (=TopMost: count - 1) Ribbonu dolů až k našemu Parentu (u >= 1):
                for (int u = last; u >= 1; u--)
                {
                    ribbonsUp[u].UnMergeDxRibbon();
                }

                // Konečně máme this Ribbon osamocený (není Merge nahoru, ani neobsahuje MergedChild), provedeme tedy akci:
                _RunLogAction(action);

                // Nazpátek se bude mergovat (mergeBack) i this Ribbon do svého Parenta? Anebo jen náš Parent do jeho Parenta a náš Ribbon zůstane UnMergovaný?
                int mergeFrom = (mergeBack ? 0 : 1);

                // ...a pak se přimerguje náš parent / nebo i zdejší Ribbon zpátky nahoru do TopMost:
                for (int m = mergeFrom; m < last; m++)
                    ribbonsUp[m].MergeCurrentDxToParent(ribbonsUp[m + 1], true);

            }
            finally
            {
                // Všem Ribonům v řadě nastavím CheckLazyContentEnabled = true:
                ribbonsUp.ForEach(r => r.CheckLazyContentEnabled = true);

                // Top Ribbon obnoví svoji práci:
                topRibbon.BarManager.EndUpdate();
            }

            // A protože po celou dobu byl potlačen CheckLazyContentEnabled, tak pro Top Ribbon to nyní provedu explicitně:
            var topRibbonSelectedPage2 = topRibbon.SelectedPageId;
            if (topRibbonSelectedPage2 != topRibbonSelectedPage1)
                topRibbon.CheckLazyContent(topRibbon.SelectedPage, false);

            if (LogActive) DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: UnMerge + Action + Merge; Total Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Provede danou akci, do logu vepíše její čas
        /// </summary>
        /// <param name="action"></param>
        private void _RunLogAction(Action action)
        {
            if (action == null) return;

            // Nyní máme náš Ribbon UnMergovaný, ale i on v sobě může mít mergovaného Childa:
            var childRibbon = this.MergedChildRibbon;
            if (childRibbon != null) this.UnMergeDxRibbon();

            var startTime = DxComponent.LogTimeCurrent;
            action();
            if (LogActive) DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: RunAction; Time: {DxComponent.LogTokenTimeMilisec}", startTime);

            // Do this Ribbonu vrátíme jeho Child Ribbon:
            if (childRibbon != null) this.MergeChildRibbon(childRibbon, false);
        }
        /// <summary>
        /// Odmerguje z this Ribbonu jeho případně mergovaný obsah, nic dalšího nedělá
        /// </summary>
        public void UnMergeDxRibbon()
        {
            this.UnMergeRibbon();
        }
        /// <summary>
        /// Odebere mergovaný Ribbon a vepíše čas do Logu
        /// </summary>
        public override void UnMergeRibbon()
        {
            var startTime = DxComponent.LogTimeCurrent;

            var lastSelectedOwnPageId = this.LastSelectedOwnPageId;

            base.UnMergeRibbon();

            DxRibbonControl childDxRibbon = this.MergedChildDxRibbon;
            if (childDxRibbon != null)
                childDxRibbon.MergedIntoParentDxRibbon = null;

            this.MergedChildRibbon = null;

            // Po UnMerge zkusíme selectovat tu naši vlastní stránku, která byla naposledy aktivní:
            if (lastSelectedOwnPageId != null)
                this.SelectedPageId = lastSelectedOwnPageId;

            if (LogActive) DxComponent.LogAddLineTime($"UnMergeRibbon from Parent: {this.DebugName}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        #endregion
        #region Souřadnice oblasti Ribbonu, kde jsou aktuálně buttony
        /// <summary>
        /// Souřadnice oblasti Ribbonu, kde jsou aktuálně buttony
        /// </summary>
        public Rectangle ButtonsBounds
        {
            get { return GetInnerBounds(50); }
        }
        /// <summary>
        /// Určí a vrátí prostor, v němž se reálně nacházejí buttony a další prvky uvnitř Ribbonu.
        /// Řeší tedy aktuální skin, vzhled, umístění Ribbonu (on někdy zastává funkci Titlebar okna) atd.
        /// </summary>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        private Rectangle GetInnerBounds(int itemCount = 50)
        {
            if (itemCount < 5) itemCount = 5;
            int c = 0;
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            // Původně jsem procházel všechny stránky :
            //       foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.Pages)
            //   Ale mělo to chybu: pokud byl Ribbon nejprve ve velikosti Standard, pak jeho buttony měly plnou výšku, na stránce "A" i "B". To je OK.
            //   Pak jsem přepnul na stránku "A" a zmenšil Ribbon, na stránce "A" se upravily souřadnice buttonů na malý Ribbon.
            //   Jenže buttony, které jsou na stránce "B", si prozatím nechaly velikost původní = plnou. Z hlediska komponenty OK, šetří čas - nepočítá zbytečně layout pro stránky, které nejsou zobrazeny.
            //   To ale tady narušuje výpočet.
            // Proto pro tento výpočet beru jen aktuálně zobrazenou stránku, pokud stránka obsahuje nějaká data; teprve pokud by byla prázdná, pak beru všechny:
            var pages = GetPagesForBounds();
            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.Pages)
            {
                foreach (DevExpress.XtraBars.Ribbon.RibbonPageGroup group in page.Groups)
                {
                    foreach (var itemLink in group.ItemLinks)
                    {
                        if (itemLink is DevExpress.XtraBars.BarButtonItemLink link)
                        {
                            var bounds = link.Bounds;             // Bounds = relativně v Ribbonu, ScreenBounds = absolutně v monitoru
                            if (bounds.Left > 0 && bounds.Top > 0 && bounds.Width > 0 && bounds.Height > 0)
                            {
                                if (c == 0)
                                {
                                    l = bounds.Left;
                                    t = bounds.Top;
                                    r = bounds.Right;
                                    b = bounds.Bottom;
                                }
                                else
                                {
                                    if (bounds.Left < l) l = bounds.Left;
                                    if (bounds.Top < t) t = bounds.Top;
                                    if (bounds.Right > r) r = bounds.Right;
                                    if (bounds.Bottom > b) b = bounds.Bottom;
                                }
                                c++;

                                if (c > itemCount) break;
                            }
                        }
                    }
                    if (c > itemCount) break;
                }
                // if (c > itemCount) break;
            }
            Rectangle clientBounds = this.ClientRectangle;
            if (c == 0) return clientBounds;

            int cr = clientBounds.Right - 6;
            if (r < cr) r = cr;
            return Rectangle.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Vrátí stránky, pro které se má vypočítat InnerBounds
        /// </summary>
        /// <returns></returns>
        private List<DevExpress.XtraBars.Ribbon.RibbonPage> GetPagesForBounds()
        {
            List<DevExpress.XtraBars.Ribbon.RibbonPage> pages = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();
            if (this.SelectedPage != null && this.SelectedPage.Groups.Count > 0)
                pages.Add(this.SelectedPage);
            else
                pages.AddRange(GetPages(PagePosition.All));
            return pages;
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

            if (addSkinButton) iGroup.Items.Add(new DataRibbonItem() { ItemId = "_SYS__DevExpress_SkinSetDropDown", RibbonItemType = RibbonItemType.SkinSetDropDown });
            if (addPaletteButton) iGroup.Items.Add(new DataRibbonItem() { ItemId = "_SYS__DevExpress_SkinPaletteDropDown", RibbonItemType = RibbonItemType.SkinPaletteDropDown });
            if (addPaletteGallery) iGroup.Items.Add(new DataRibbonItem() { ItemId = "_SYS__DevExpress_SkinPaletteGallery", RibbonItemType = RibbonItemType.SkinPaletteGallery });
            if (addUhdSupport) iGroup.Items.Add(new DataRibbonItem() 
            { 
                ItemId = "_SYS__DevExpress_UhdSupportCheckBox", Text = "UHD Paint", ToolTipText = "Zapíná podporu pro Full vykreslování na UHD monitoru",
                RibbonItemType = RibbonItemType.CheckBoxToggle, 
                // ImageUnChecked = "svgimages/zoom/zoomout.svg", ImageChecked = "svgimages/zoom/zoomin.svg",
                Checked = DxComponent.UhdPaintEnabled, MenuAction = SetUhdPaint 
            });

            return iGroup;
        }
        private static void SetUhdPaint(IMenuItem menuItem) 
        {
            DxComponent.UhdPaintEnabled = (menuItem?.Checked ?? false);
            DxComponent.ApplicationRestart();
        }
        #endregion
        #region IDxRibbonInternal + IListenerApplicationIdle implementace
        void IDxRibbonInternal.PrepareRealLazyItems(DxRibbonLazyLoadInfo lazyGroup, bool isCalledFromReFill) { PrepareRealLazyItems(lazyGroup, isCalledFromReFill); }
        void IDxRibbonInternal.RemoveGroupsFromQat(List<DevExpress.XtraBars.Ribbon.RibbonPageGroup> groupsToDelete) { RemoveGroupsFromQat(groupsToDelete); }
        void IDxRibbonInternal.RemoveItemsFromQat(List<DevExpress.XtraBars.BarItem> itemsToDelete) { RemoveItemsFromQat(itemsToDelete); }
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
        public DxRibbonPageCategory(string text, Color color) : base(text, color) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="visible"></param>
        public DxRibbonPageCategory(string text, Color color, bool visible) : base(text, color, visible) { }
    }
    /// <summary>
    /// Stránka Ribbonu s vlastností LazyContentItems
    /// </summary>
    public class DxRibbonPage : DevExpress.XtraBars.Ribbon.RibbonPage
    {
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
        public IRibbonPage PageData { get; set; }
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
                string groupId = DxRibbonLazyLoadInfo.LazyLoadGroupId;
                var ownerRibbonItems = this.OwnerDxRibbon.Items;
                var groupsToDelete = this.Groups.OfType<DevExpress.XtraBars.Ribbon.RibbonPageGroup>().Where(g => g.Name != groupId).ToList();
                var itemsToDelete = groupsToDelete.SelectMany(g => g.ItemLinks).Select(l => l.Item).ToList();

                // Před fyzickým odebráním grup a prvků z RibbonPage je předám do QAT systému v Ribbonu, aby si je odebral ze své evidence: 
                var iOwnerRibbon = this.OwnerDxRibbon as IDxRibbonInternal;
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
    public class DxRibbonGroup : DevExpress.XtraBars.Ribbon.RibbonPageGroup
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
        protected DxRibbonControl OwnerDxRibbon { get { return this.Ribbon as DxRibbonControl; } }
        /// <summary>
        /// Vlastník Ribbon typu <see cref="DxRibbonControl"/>
        /// </summary>
        protected DxRibbonPage OwnerDxPage { get { return this.Page as DxRibbonPage; } }
        /// <summary>
        /// Smaže obsah this grupy
        /// </summary>
        public void ClearContent()
        {
            var itemsToDelete = this.ItemLinks.Select(l => l.Item).ToList();

            // Před fyzickým odebráním prvků z RibbonGroup je předám do QAT systému v Ribbonu, aby si je odebral ze své evidence:
            var iOwnerRibbon = this.OwnerDxRibbon as IDxRibbonInternal;
            iOwnerRibbon.RemoveItemsFromQat(itemsToDelete);

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
            if (this.Ribbon is DxRibbonControl dxRibbonControl && this.Tag is IRibbonItem iRibbonItem)
            {
                iRibbonItem.Checked = this.Checked;
                dxRibbonControl.RaiseRibbonItemClick(iRibbonItem);
            }
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
    #region TrackBar
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
    { }
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
                list.Sort((a, b) => a.PageOrder.CompareTo(b.PageOrder));
            }
            return list;
        }
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
        protected string _PageId;
        /// <summary>
        /// Režim pro vytvoření / refill / remove této stránky
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Pořadí stránky, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public virtual int PageOrder { get; set; }
        /// <summary>
        /// Jméno stránky
        /// </summary>
        public virtual string PageText { get; set; }
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
        /// Titulek kategorie zobrazovaný uživateli
        /// </summary>
        public virtual string CategoryText { get; set; }
        /// <summary>
        /// Barva kategorie
        /// </summary>
        public virtual Color CategoryColor { get; set; }
        /// <summary>
        /// Kategorie je viditelná?
        /// </summary>
        public virtual bool CategoryVisible { get; set; }
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
        protected string _GroupId;
        /// <summary>
        /// Režim pro vytvoření / refill / remove této grupy
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Pořadí grupy, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public virtual int GroupOrder { get; set; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        public virtual string GroupText { get; set; }
        /// <summary>
        /// Obrázek grupy
        /// </summary>
        public virtual string GroupImage { get; set; }
        /// <summary>
        /// Zobrazit speciální tlačítko grupy vpravo dole v titulku grupy (lze tak otevřít nějaké okno vlastností pro celou grupu)
        /// </summary>
        public virtual bool GroupButtonVisible { get; set; }
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
        /// Libovolná data aplikace
        /// </summary>
        public virtual object Tag { get; set; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    public class DataRibbonItem : DataMenuItem, IRibbonItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonItem() : base()
        {
            this.ChangeMode = ContentChangeMode.Add;
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
                string debugText = $"Id: {_ItemId}; Text: {Text}; Type: {RibbonItemType}";
                if (this.SubRibbonItems != null)
                    debugText += $"; SubItems: {this.SubRibbonItems.Count}";
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
        public virtual RibbonItemType RibbonItemType { get; set; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        public virtual RibbonItemStyles RibbonStyle { get; set; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        public virtual RibbonContentMode SubItemsContentMode { get; set; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu.
        /// Výchozí hodnota je null.
        /// </summary>
        public virtual List<IRibbonItem> SubRibbonItems { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonItem> IRibbonItem.SubRibbonItems { get { return this.SubRibbonItems; } }
    }
    #endregion
    #region Interface IRibbonPage, IRibbonCategory, IRibbonGroup, IRibbonItem;  Enumy RibbonPageType, RibbonContentMode, RibbonItemStyles, BarItemPaintStyle, RibbonItemType.
    /// <summary>
    /// Definice stránky v Ribbonu
    /// </summary>
    public interface IRibbonPage
    {
        /// <summary>
        /// Kategorie, do které patří tato stránka. Může být null pro běžné stránky Ribbonu.
        /// </summary>
        IRibbonCategory Category { get; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci Ribbonu
        /// </summary>
        string PageId { get; }
        /// <summary>
        /// Režim pro vytvoření / refill / remove této stránky
        /// </summary>
        ContentChangeMode ChangeMode { get; }
        /// <summary>
        /// Pořadí stránky, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int PageOrder { get; set; }
        /// <summary>
        /// Jméno stránky
        /// </summary>
        string PageText { get; }
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
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Definice kategorie, do které patří stránka v Ribbonu
    /// </summary>
    public interface IRibbonCategory
    {
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
        Color CategoryColor { get; }
        /// <summary>
        /// Kategorie je viditelná?
        /// </summary>
        bool CategoryVisible { get; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Definice skupiny ve stránce Ribbonu
    /// </summary>
    public interface IRibbonGroup
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
        /// Režim pro vytvoření / refill / remove této grupy
        /// </summary>
        ContentChangeMode ChangeMode { get; }
        /// <summary>
        /// Pořadí grupy, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int GroupOrder { get; set; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        string GroupText { get; }
        /// <summary>
        /// Obrázek grupy
        /// </summary>
        string GroupImage { get; }
        /// <summary>
        /// Zobrazit speciální tlačítko grupy vpravo dole v titulku grupy (lze tak otevřít nějaké okno vlastností pro celou grupu)
        /// </summary>
        bool GroupButtonVisible { get; }
        /// <summary>
        /// Soupis prvků grupy (tlačítka, menu, checkboxy, galerie)
        /// </summary>
        IEnumerable<IRibbonItem> Items { get; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public interface IRibbonItem : IMenuItem
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
        /// Typ prvku
        /// </summary>
        RibbonItemType RibbonItemType { get; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        RibbonItemStyles RibbonStyle { get; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        RibbonContentMode SubItemsContentMode { get; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu
        /// </summary>
        IEnumerable<IRibbonItem> SubRibbonItems { get; }
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
        /// <see cref="IRibbonItem.RibbonItemType"/> = <see cref="RibbonItemType.None"/>, a nevznikne žádný vizuální prvek ani grupa.
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
        /// <see cref="IRibbonItem.RibbonItemType"/> = <see cref="RibbonItemType.None"/>, a nevznikne žádný vizuální prvek ani grupa.
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
        Button,
        ButtonGroup,
        SplitButton,
        CheckBoxStandard,
        /// <summary>
        /// Button se stavem Checked, který může být NULL (výchozí hodnota). 
        /// Pokud má být výchozí stav false, je třeba jej do <see cref="IMenuItem.Checked"/> vložit!
        /// Lze specifikovat ikony pro všechny tři stavy (NULL - false - true)
        /// </summary>
        CheckBoxToggle,
        RadioItem,
        TrackBar,
        Menu,
        InRibbonGallery,
        SkinSetDropDown,
        SkinPaletteDropDown,
        SkinPaletteGallery

    }
    /// <summary>
    /// Druh změny obsahu aktuálního prvku
    /// </summary>
    public enum ContentChangeMode
    {
        /// <summary>
        /// Nezadáno explicitně, použije se defaultní hodnota (typicky <see cref="Add"/>)
        /// </summary>
        None = 0,
        /// <summary>
        /// Přidat nový obsah ke stávajícímu obsahu, prvky se shodným ID aktualizovat, nic neodebírat
        /// </summary>
        Add,
        /// <summary>
        /// Znovu naplnit prvek: pokud prvek existuje, nejprve bude jeho obsah odstraněn, a poté bude vložen nově definovaný obsah.
        /// Pokud prvek neexistuje, bude vytvořen nový a prázdný.
        /// </summary>
        ReFill,
        /// <summary>
        /// Odstranit prvek: pokud existuje, bude zahozen jeho obsah i prvek samotný. Pokud neexistuje, nebude vytvářen.
        /// Pokud definice prvku má režim <see cref="Remove"/>, pak případný definovaný obsah prvku nebude použit.
        /// </summary>
        Remove
    }
    #endregion
}
