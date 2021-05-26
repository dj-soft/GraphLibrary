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

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxRibbonControl
    /// <summary>
    /// Potomek Ribbonu
    /// </summary>
    public class DxRibbonControl : DevExpress.XtraBars.Ribbon.RibbonControl
    {
        #region Konstruktor a vykreslení ikony
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonControl()
        {
            InitProperties();
            InitEvents();
        }
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

            ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            ApplicationButtonText = " HELIOS ";

            ToolTipController = new DevExpress.Utils.ToolTipController
            {
                Rounded = true,
                ShowShadow = true,
                ToolTipAnchor = DevExpress.Utils.ToolTipAnchor.Cursor,
                ToolTipLocation = DevExpress.Utils.ToolTipLocation.RightBottom,
                ToolTipStyle = DevExpress.Utils.ToolTipStyle.Windows7,
                ToolTipType = DevExpress.Utils.ToolTipType.SuperTip,       // Standard   Flyout   SuperTip;
                IconSize = DevExpress.Utils.ToolTipIconSize.Large,
                CloseOnClick = DevExpress.Utils.DefaultBoolean.True
            };

            Visible = true;

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
        /// Jméno Ribbonu pro debugování
        /// </summary>
        public string DebugName { get; set; }
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
        #region Tvorba obsahu Ribbonu: Clear, Empty, Final, Add
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

            try
            {
                _ClearingNow = true;
                this.Pages.Clear();
                this.Categories.Clear();
                this.PageCategories.Clear();
                this.Items.Clear();

                this.PageHeaderItemLinks.Clear();
                this.Toolbar.ItemLinks.Clear();

                //this.MergedCategories.Clear();
                //this.MergedPages.Clear();
            }
            finally
            {
                _ClearingNow = false;
            }

            DxComponent.LogAddLineTime($" === ClearRibbon {this.DebugName}: {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        private bool _ClearingNow;
        /// <summary>
        /// Smaže výhradně jednotlivé prvky z Ribbonu (Items a LazyLoadContent) a grupy prvků (Page.Groups).
        /// Ponechává naživu Pages, Categories a PageCategories.
        /// </summary>
        public void ClearPageContents()
        {
            var startTime = DxComponent.LogTimeCurrent;
            this.Items.Clear();
            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.AllPages)
            {
                page.Groups.Clear();
                if (page is DxRibbonPage dxRibbonPage)
                {
                    dxRibbonPage.RemoveLazyLoadInfo();
                }
            }
            DxComponent.LogAddLineTime($" === EmptyRibbon {this.DebugName}: {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Přidá dodané prvky do this ribbonu, zakládá stránky, kategorie, grupy...
        /// Pokud má být aktivní <see cref="UseLazyContentCreate"/>, musí být nastaveno na true před přidáním prvku.
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(IEnumerable<IRibbonItem> items)
        {
            this.RunInGui(() =>
            {
                this.ModifyCurrentDxContent(() =>
                {
                    _AddItems(items, this.UseLazyContentCreate, false, "Fill");
                    CheckLazyContentCurrentPage(true);
                });
            });
        }
        /// <summary>
        /// Znovu naplní stránky this Ribbonu specifikované v dodaných datech.
        /// Nejprve zahodí obsah stránek, které jsou uvedeny v dodaných datech.
        /// Pak do Ribbonu vygeneruje nový obsah do specifikovaných stránek.
        /// Pokud pro některou stránku nebudou dodána žádná platná data, stránku zruší
        /// (k tomu se použije typ prvku <see cref="IMenuItem.ItemType"/> == <see cref="RibbonItemType.None"/>, kde daný záznam pouze definuje Page ke zrušení).
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// </summary>
        /// <param name="items"></param>
        public void ReFillPageItems(IEnumerable<IRibbonItem> items)
        {
            this.RunInGui(() =>
            {
                this.ModifyCurrentDxContent(() =>
                {
                    var reFillPages = _PrepareReFill(items, true);
                    _AddItems(items, this.UseLazyContentCreate, true, "OnDemand");
                    _FinishReFill(reFillPages);
                    CheckLazyContentCurrentPage(true);
                });
            });
        }
        /// <summary>
        /// Metoda vrátí seznam těch zdejších stránek, jejichž Name se vyskytují v dodaném seznamu nových prvků.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="clearContent"></param>
        /// <returns></returns>
        private List<DevExpress.XtraBars.Ribbon.RibbonPage> _PrepareReFill(IEnumerable<IRibbonItem> items, bool clearContent)
        {
            List<DevExpress.XtraBars.Ribbon.RibbonPage> result = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();
            var pageDict = this.AllOwnPages.ToDictionary(p => p.Name);
            var itemsPages = items.GroupBy(i => i.PageId);
            foreach (var itemsPage in itemsPages)
            {
                string pageId = itemsPage.Key;
                if (pageId != null && pageDict.TryGetValue(pageId, out var page))
                    result.Add(page);
            }

            if (clearContent)
                result.ForEachExec(p => DxRibbonPage.ClearContentPage(p));

            return result;
        }
        private void _FinishReFill(List<DevExpress.XtraBars.Ribbon.RibbonPage> reFillPages)
        {
            foreach (var reFillPage in reFillPages)
            {
                if (reFillPage.Groups.Count == 0)
                    this.Pages.Remove(reFillPage);
            }
        }
        /// <summary>
        /// Metoda je volána při aktivaci stránky this Ribbonu v situaci, kdy tato stránka má aktivní nějaký Lazy režim pro načtení svého obsahu.
        /// Může to být prostě opožděné vytváření fyzických controlů z dat v paměti, nebo reálné OnDemand donačítání obsahu z aplikačního serveru.
        /// Přidá prvky do this Ribbonu z dodané LazyGroup do this Ribbonu. Zde se prvky přidávají vždy jako reálné, už ne Lazy.
        /// </summary>
        /// <param name="lazyGroup"></param>
        /// <param name="isReFill">false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        internal void PrepareRealLazyItems(DxRibbonLazyLoadInfo lazyGroup, bool isReFill)
        {
            if (lazyGroup == null || !lazyGroup.IsActive) return;

            lazyGroup.IsActive = false;
            bool hasItems = lazyGroup.HasItems;

            // V této jediné situaci si necháme LazyInfo: když proběhlo naplnění prvků OnDemand => ReFill, a režim OnDemand je "Po každé aktivaci stránky":
            //  Pak máme na aktuální stránce uložené reálné prvky, a současně tam je OnDemand "Group", která zajistí nový OnDemand při následující aktivaci stránky!
            bool leaveLazyInfo = (isReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime);

            // Za určité situace provedeme Clear prvků stránky: pokud NENÍ ReFill a pokud je režim EveryTime = 
            //  právě zobrazujeme stránku, která při každém zobrazení načítá nová data, takže předešlá data máme zahodit...
            bool clearContent = (!isReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime);
            
            if (hasItems || clearContent || !leaveLazyInfo)
            {   // Když je důvod něco provádět (máme nové prvky, nebo budeme odstraňovat LazyInfo z Ribbonu):
                this.ModifyCurrentDxContent(() =>
                {
                    if (clearContent)
                    {
                        lazyGroup.OwnerPage?.ClearContent(true, !leaveLazyInfo);
                    }
                    if (hasItems)
                    {   // Když máme co zobrazit, tak nyní vygenerujeme reálné Grupy a BarItems:
                        string info = "LazyFill; Page: " + lazyGroup.FirstItem.PageText;
                        _AddItems(lazyGroup.Items, false, false, info);        // Fyzicky vygenerujeme prvky stránky Ribbonu
                    }
                    if (!leaveLazyInfo)
                    {
                        // LazyGroup odstraníme vždy, i pro oba režimy OnDemand (pro ně za chvilku vyvoláme event OnDemandLoad):
                        // Pokud by byl režim OnDemandLoadEveryTime, pak si novou LazyGroup vygenerujeme společně s dodanými položkami
                        //  v metodě _AddItems s parametrem isReFill = true, podle režimu OnDemandLoadEveryTime!
                        lazyGroup.OwnerPage?.RemoveLazyLoadInfo();
                    }
                });
            }

            if (leaveLazyInfo && lazyGroup != null)
                lazyGroup.IsActive = true;

            if (lazyGroup.IsOnDemand && !isReFill)
            {   // Oba režimy OnDemandLoad vyvolají patřičný event, pokud tato metoda NENÍ volána právě z akce naplnění ribbonu daty OnDemand:
                RunStartOnDemandLoad(lazyGroup);
            }
        }
        /// <summary>
        /// Přidá jeden prvek do Ribbonu, zakládá stránky, kategorie, grupy...
        /// Pokud má být aktivní <see cref="UseLazyContentCreate"/>, musí být nastaveno na true před přidáním prvku.
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(IRibbonItem item)
        {
            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            _AddItem(item, this.UseLazyContentCreate, false, ref count);
            DxComponent.LogAddLineTime($" === Ribbon {DebugName}: Add 1 item; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);

            CheckLazyContentCurrentPage(true);
        }
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
        /// Vrátí soupis stránek z this ribbonu z daných pozic
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public List<DevExpress.XtraBars.Ribbon.RibbonPage> GetPages(PagePosition position)
        {
            List<DevExpress.XtraBars.Ribbon.RibbonPage> result = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();

            if (position.HasFlag(PagePosition.Default))
                result.AddRange(this.Pages);

            if (position.HasFlag(PagePosition.Categories))
                result.AddRange(this.Categories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.Pages));

            if (position.HasFlag(PagePosition.MergedDefault))
                result.AddRange(this.MergedPages);

            if (position.HasFlag(PagePosition.MergedCategories))
                result.AddRange(this.MergedCategories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.Pages));

            return result;
        }
        public PagePosition GetPagePosition(DevExpress.XtraBars.Ribbon.RibbonPage page)
        {
            if (page == null) return PagePosition.None;
            if (_IsPageOnPosition(page, PagePosition.Default)) return PagePosition.Default;
            if (_IsPageOnPosition(page, PagePosition.Categories)) return PagePosition.Categories;
            if (_IsPageOnPosition(page, PagePosition.MergedDefault)) return PagePosition.MergedDefault;
            if (_IsPageOnPosition(page, PagePosition.MergedCategories)) return PagePosition.MergedCategories;
            return PagePosition.None;
        }
        private bool _IsPageOnPosition(DevExpress.XtraBars.Ribbon.RibbonPage page, PagePosition position)
        {
            var pages = GetPages(position);
            return pages.Contains(page);
        }
        /// <summary>
        /// Pozice stránky
        /// </summary>
        [Flags]
        public enum PagePosition 
        { 
            None = 0, 
            Default = 0x01,
            Categories = 0x02,
            MergedDefault = 0x10,
            MergedCategories = 0x20,

            All = Default | Categories | MergedDefault | MergedCategories,
            AllOwn = Default | Categories,
            AllMerged = MergedDefault | MergedCategories
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané kolekce, v daném režimu LazyLoad
        /// </summary>
        /// <param name="items"></param>
        /// <param name="isLazyContent">Obsahuje true, když se prvky typu Group a BarItem nemají fyzicky generovat, ale mají se jen registrovat do LazyGroup / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemand">Obsahuje true, pokud tyto položky jsou donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        /// <param name="logText"></param>
        private void _AddItems(IEnumerable<IRibbonItem> items, bool isLazyContent, bool isOnDemand, string logText)
        {
            if (items is null) return;
            var startTime = DxComponent.LogTimeCurrent;
            List<IRibbonItem> list = items.Where(i => i != null).ToList();
            _SortItems(list);
            int count = 0;
            foreach (var item in list)
                _AddItem(item, isLazyContent, isOnDemand, ref count);
            DxComponent.LogAddLineTime($" === Ribbon {DebugName}: {logText} {list.Count} item[s]; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Zajistí správné setřídění prvků v poli
        /// </summary>
        /// <param name="list"></param>
        private void _SortItems(List<IRibbonItem> list)
        {
            if (list.Count <= 1) return;

            int pageOrder = 0;
            int groupOrder = 0;
            int itemOrder = 0;
            foreach (var item in list)
            {
                if (item.PageOrder == 0) item.PageOrder = ++pageOrder; else if (item.PageOrder > pageOrder) pageOrder = item.PageOrder;
                if (item.GroupOrder == 0) item.GroupOrder = ++groupOrder; else if (item.GroupOrder > groupOrder) groupOrder = item.GroupOrder;
                if (item.ItemOrder == 0) item.ItemOrder = ++itemOrder; else if (item.ItemOrder > itemOrder) itemOrder = item.ItemOrder;
            }
            if (list.Count > 1) list.Sort((a, b) => RibbonItem.CompareByOrder(a, b));
        }
        /// <summary>
        /// Metoda přidá do this Ribbonu data dalšího prvku.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isLazyContent">Obsahuje true, když se prvky typu Group a BarItem nemají fyzicky generovat, ale mají se jen registrovat do LazyGroup / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemand">Obsahuje true, pokud tyto položky jsou donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        /// <param name="count"></param>
        private void _AddItem(IRibbonItem item, bool isLazyContent, bool isOnDemand, ref int count)
        {
            if (item is null) return;

            var category = GetCategory(item);
            var page = GetPage(item, category);

            bool needOnDemandGroup = (item.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime ||
                                     (item.PageContentMode == RibbonContentMode.OnDemandLoadOnce && !isOnDemand));
            if (needOnDemandGroup)
            {
                page.PrepareOnDemandInfo(item);
            }

            if (item.ItemType != RibbonItemType.None)            // Prvek typu None slouží jen k deklaraci Kategorie a Stránky, ale ne Grupy a Prvku.
            {
                bool isSelectedPage = (this.SelectedPageId == page.Name);
                if (isSelectedPage || !isLazyContent)
                {   // Pokud aktuální stránka je Selected, pak do ní rovnou nasypu reálné BarItems (anebo pokud není dán režim LazyLoad),
                    // protože bych beztak BarItems generoval hned po vrácení řízení do GUI v akci SelectedPageChanged:
                    var group = GetGroup(item, page);
                    var button = GetBarItem(item, group, ref count);
                }
                else
                {
                    page.AddLazyLoadItem(item);
                }
            }
        }
        #endregion
        #region LazyLoad page content : OnSelectedPageChanged => CheckLazyContent
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
            base.OnSelectedPageChanged(prev);
        }
        /// <summary>
        /// Prověří, zda aktuální stránka <see cref="DevExpress.XtraBars.Ribbon.RibbonControl.SelectedPage"/> 
        /// má na sobě nějaké HasLazyContentItems, a pokud ano, tak je fyzicky vytvoří
        /// </summary>
        /// <param name="isReFill">false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        protected virtual void CheckLazyContentCurrentPage(bool isReFill)
        {
            this.CheckLazyContent(this.SelectedPage, isReFill);
        }
        /// <summary>
        /// Prověří, zda daná stránka má nějaké HasLazyContentItems, a pokud ano tak je fyzicky vytvoří
        /// </summary>
        /// <param name="page"></param>
        /// <param name="isReFill">false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        protected virtual void CheckLazyContent(DevExpress.XtraBars.Ribbon.RibbonPage page, bool isReFill)
        {
            if (page == null) return;
            if (!this.CheckLazyContentEnabled) return;

            if (_ClearingNow)
            {   // Pokud nyní probíhá Clear, tak po Pages.Clear proběhne Select na první CategoryPage nebo MergedPage, která je dostupná.
                // Ale pokud to bude pozice Default nebo Categories (=naše vlastní), tak ty se nyní odstraňují a nemá cenu pro ně řešit SelectedPage a LazyLoad,
                //  na to už je zbytečně pozdě!

                // Přípravu obsahu stránky tedy provedu pouze pro stránky typu Merged:
                if (!_IsPageOnPosition(page, PagePosition.AllMerged)) return;
            }

            // Něco k DevExpress a k Ribbonům si přečti v komentáři k metodě DxRibbonPageLazyGroupInfo.TryGetLazyContentForPage!

            if (!DxRibbonLazyLoadInfo.TryGetLazyDxPages(page, isReFill, out var lazyDxPages)) return;

            // Pro danou stránku jsme našli jednu nebo i více definic LazyGroups.
            // Na základní stránce (stránka definovaná přímo v Ribbonu) je vždy jen jedna LazyGroup.
            // Ale pokud budu mergovat postupně více Ribbonů nahoru: (1) => (2); (2) => (3),
            //  pak Ribbon 1 může mít stránku "PageDoc" a její LazyGroup se přimerguje do stejnojmenné stránky "PageDoc" Ribbonu 2,
            //  přičemž Ribbon 2 může mít na této stránce svoji vlastní LazyGroup (ale s unikátním Name), takže už tady jsou dvě LazyGroup.
            //  Při dalším mergování Ribbonu 2 do Ribbonu 3 tak máme jednu (mergovanou) stránku PageDoc, která má dvě i tři instance LazyGroup.
            // Instance LazyGroup má vždy unikátní Name, takže se při mergování zachovají všechny jednotlivě.
            foreach (var lazyDxPage in lazyDxPages)
                lazyDxPage.PrepareRealLazyItems(isReFill);
        }
        /// <summary>
        /// Pokud je true (běžný aktivní stav), pak se po aktivaci stránky provádí kontroly LazyLoad obsahu.
        /// Nastavením na false se tyto kotnroly deaktivují. Používá se při hromadnám Unmerge a zpět Merge, kdy dochází ke změnám SelectedPage v každém kroku, a zcela zbytečně.
        /// </summary>
        protected bool CheckLazyContentEnabled { get; set; }
        #endregion
        #region OnDemand loading
        /// <summary>
        /// Nastartuje požadavek na OnDemand Load obsahu stránky this Ribbonu
        /// </summary>
        /// <param name="lazyGroup"></param>
        protected void RunStartOnDemandLoad(DxRibbonLazyLoadInfo lazyGroup)
        {
            TEventArgs<IRibbonItem> args = new TEventArgs<IRibbonItem>(lazyGroup.PageOnDemandItem);
            this.OnPageOnDemandLoad(args);
            PageOnDemandLoad?.Invoke(this, args);
        }
        /// <summary>
        /// Provede se při OnDemand načítání obsahu stránky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageOnDemandLoad(TEventArgs<IRibbonItem> args) { }
        /// <summary>
        /// Vyvolá se při OnDemand načítání obsahu stránky
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonItem>> PageOnDemandLoad;
        #endregion
        #region Fyzická tvorba prvků Ribbonu (kategorie, Stránka, Grupa, Prvek, konkrétní prvky, ...)
        protected DxRibbonPageCategory GetCategory(IRibbonItem item, bool enableNew = true)
        {
            if (item is null) return null;
            if (String.IsNullOrEmpty(item.CategoryId)) return null;
            DxRibbonPageCategory category = PageCategories.GetCategoryByName(item.CategoryId) as DxRibbonPageCategory;
            if (category is null && enableNew)
            {
                category = new DxRibbonPageCategory(item.CategoryText, item.CategoryColor, item.CategoryVisible);
                category.Name = item.CategoryId;
                PageCategories.Add(category);
            }
            return category;
        }
        protected DxRibbonPage GetPage(IRibbonItem item, DevExpress.XtraBars.Ribbon.RibbonPageCategory category = null, bool enableNew = true)
        {
            if (item is null) return null;
            bool isCategory = !(category is null);
            DxRibbonPage page;
            if (item.PageIsHome && !isCategory)            // Pokud je předána kategorie, pak jde o kontextovou stránku a ta nesmí být "Home"!
            {
                page = Pages.GetPageByText(item.PageText) as DxRibbonPage;
                if (page != null)
                {
                    Name = item.PageId;
                }
            }
            page = (isCategory ? category.Pages.FirstOrDefault(r => (r.Name == item.PageId)) : Pages.FirstOrDefault(r => r.Name == item.PageId)) as DxRibbonPage;
            if (page is null && enableNew)
            {
                page = new DxRibbonPage(this, item.PageText)
                {
                    Name = item.PageId,
                    Tag = item
                };
                if (isCategory)
                    category.Pages.Add(page);
                else
                    Pages.Add(page);
            }
            return page;
        }
        protected DevExpress.XtraBars.Ribbon.RibbonPageGroup GetGroup(IRibbonItem item, DxRibbonPage page, bool enableNew = true)
        {
            if (item is null || page is null) return null;
            DevExpress.XtraBars.Ribbon.RibbonPageGroup group = page.Groups.GetGroupByName(item.GroupId);
            if (group is null && enableNew)
            {
                group = new DevExpress.XtraBars.Ribbon.RibbonPageGroup(item.GroupText)
                {
                    Name = item.GroupId,
                    CaptionButtonVisible = (item.GroupButtonVisible ? DefaultBoolean.True : DefaultBoolean.False),
                    State = DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto,
                    Tag = item
                };
                group.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.GroupImage, ImagesSize, item.GroupText);
                page.Groups.Add(group);
            }
            return group;
        }
        protected DevExpress.XtraBars.BarItem GetBarItem(IMenuItem item, DevExpress.XtraBars.Ribbon.RibbonPageGroup group, ref int count, bool enableNew = true)
        {
            if (item is null || group is null) return null;
            DevExpress.XtraBars.BarItem barItem = Items[item.ItemId];
            if (barItem is null)
            {
                if (!enableNew) return null;
                barItem = CreateBarItem(item, ref count);
                if (barItem is null) return null;
                var barLink = group.ItemLinks.Add(barItem);
                if (item.ItemIsFirstInGroup) barLink.BeginGroup = true;
            }
            else
            {
                FillBarItem(barItem, item);
            }
            barItem.Tag = item;

            if (item.ItemToolbarOrder.HasValue)
                this.Toolbar.ItemLinks.Add(barItem);

            return barItem;
        }
        protected DevExpress.XtraBars.BarItem CreateBarItem(IMenuItem item, ref int count)
        {
            DevExpress.XtraBars.BarItem barItem = null;
            switch (item.ItemType)
            {
                case RibbonItemType.ButtonGroup:
                    count++;
                    DevExpress.XtraBars.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(item.SubItems, ref count));
                    buttonGroup.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    count++;
                    DevExpress.XtraBars.BarButtonItem splitButton = Items.CreateSplitButton(item.ItemText, GetPopupSubItems(item.SubItems, ref count));
                    barItem = splitButton;
                    break;
                case RibbonItemType.CheckBoxStandard:
                    bool isSlider = (item.ItemType == RibbonItemType.CheckBoxToggle);
                    count++;
                    DevExpress.XtraBars.BarCheckItem checkItem = Items.CreateCheckItem(item.ItemText, item.ItemIsChecked ?? false);
                    checkItem.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
                    barItem = checkItem;
                    break;
                case RibbonItemType.RadioItem:
                    count++;
                    DevExpress.XtraBars.BarCheckItem radioItem = Items.CreateCheckItem(item.ItemText, item.ItemIsChecked ?? false);
                    barItem = radioItem;
                    break;
                case RibbonItemType.CheckBoxToggle:
                    count++;
                    DxCheckBoxToggle toggleSwitch = new DxCheckBoxToggle(this.BarManager, item.ItemText);
                    barItem = toggleSwitch;
                    break;
                case RibbonItemType.Menu:
                    count++;
                    DevExpress.XtraBars.BarSubItem menu = Items.CreateMenu(item.ItemText);
                    var menuItems = GetBarSubItems(item.SubItems, ref count);
                    foreach (var menuItem in menuItems)
                    {
                        var menuLink = menu.AddItem(menuItem);
                        if ((menuItem.Tag is IMenuItem ribbonData) && ribbonData.ItemIsFirstInGroup)
                            menuLink.BeginGroup = true;
                    }
                    barItem = menu;
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
                    DevExpress.XtraBars.BarButtonItem button = Items.CreateButton(item.ItemText);
                    barItem = button;
                    break;
            }
            if (barItem != null)
            {
                barItem.Name = item.ItemId;
                FillBarItem(barItem, item);
            }
            return barItem;
        }
        protected DevExpress.XtraBars.BarBaseButtonItem CreateBaseButton(IMenuItem item)
        {
            DevExpress.XtraBars.BarBaseButtonItem baseButton = null;
            switch (item.ItemType)
            {
                case RibbonItemType.Button:
                case RibbonItemType.ButtonGroup:
                    // DevExpress.XtraBars.BarBaseButtonItem buttonItem = new DevExpress.XtraBars.BarButtonItem(this.Manager, item.ItemText);
                    DevExpress.XtraBars.BarLargeButtonItem buttonItem = new DevExpress.XtraBars.BarLargeButtonItem(this.Manager, item.ItemText);
                    baseButton = buttonItem;
                    break;
                case RibbonItemType.CheckBoxStandard:
                    DevExpress.XtraBars.BarCheckItem checkItem = new DevExpress.XtraBars.BarCheckItem(this.Manager);
                    baseButton = checkItem;
                    break;
            }
            if (baseButton != null)
            {
                baseButton.Name = item.ItemId;
                FillBarItem(baseButton, item);
            }
            return baseButton;
        }
        protected DevExpress.XtraBars.BarItem[] GetBarSubItems(IMenuItem[] items, ref int count)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (items != null)
            {
                foreach (IMenuItem item in items)
                {
                    DevExpress.XtraBars.BarItem barItem = CreateBarItem(item, ref count);
                    if (barItem != null)
                        barItems.Add(barItem);
                }
            }
            return barItems.ToArray();
        }
        protected DevExpress.XtraBars.BarBaseButtonItem[] GetBarBaseButtons(IMenuItem[] items, ref int count)
        {
            List<DevExpress.XtraBars.BarBaseButtonItem> baseButtons = new List<DevExpress.XtraBars.BarBaseButtonItem>();
            if (items != null)
            {
                foreach (IMenuItem item in items)
                {
                    DevExpress.XtraBars.BarBaseButtonItem baseButton = CreateBaseButton(item);
                    if (baseButton != null)
                        baseButtons.Add(baseButton);
                }
            }
            return baseButtons.ToArray();
        }
        protected DevExpress.XtraBars.PopupMenu GetPopupSubItems(IMenuItem[] items, ref int count)
        {
            DevExpress.XtraBars.PopupMenu dxPopup = new DevExpress.XtraBars.PopupMenu(BarManager);
            if (items != null)
            {
                foreach (IMenuItem item in items)
                {
                    DevExpress.XtraBars.BarItem barItem = CreateBarItem(item, ref count);
                    if (barItem != null)
                    {
                        var barLink = dxPopup.AddItem(barItem);
                        if (item.ItemIsFirstInGroup) barLink.BeginGroup = true;
                    }
                }
            }
            return dxPopup;
        }
        protected void FillBarItem(DevExpress.XtraBars.BarItem barItem, IMenuItem item)
        {
            if (item.ItemText != null)
                barItem.Caption = item.ItemText;

            barItem.Enabled = item.ItemEnabled;

            string imageName = item.ItemImage;
            if (imageName != null && !(barItem is DxCheckBoxToggle))           // DxCheckBoxToggle si řídí Image sám
            {
                if (DxComponent.TryGetResourceExtension(imageName, out var _))
                {
                    DxComponent.ApplyImage(barItem.ImageOptions, resourceName: item.ItemImage);
                }
                else
                {
                    barItem.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage, ImagesSize, item.ItemText);
                    barItem.LargeImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage, LargeImagesSize, item.ItemText);
                }
            }

            if (!string.IsNullOrEmpty(item.HotKey))
            {
                if (barItem is DevExpress.XtraBars.BarSubItem)
                    ComponentConnector.ShowWarningToDeveloper($"Setup keyboard shortcut {item.HotKey} to non button barItem {barItem.Name}. This is {barItem.GetType().Name}");
                else
                    barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(WinFormServices.KeyboardHelper.GetShortcutFromServerHotKey(item.HotKey));
            }

            if (barItem is DevExpress.XtraBars.BarCheckItem checkItem)
            {   // Do CheckBoxu vepisujeme víc vlastností:
                checkItem.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
                checkItem.CheckStyle = 
                    (item.ItemType == RibbonItemType.RadioItem ? DevExpress.XtraBars.BarCheckStyles.Radio :
                    (item.ItemType == RibbonItemType.CheckBoxToggle ? DevExpress.XtraBars.BarCheckStyles.Standard :
                     DevExpress.XtraBars.BarCheckStyles.Standard));
                checkItem.Checked = item.ItemIsChecked ?? false;
            }

            if (barItem is DxCheckBoxToggle dxCheckBoxToggle)
            {
                dxCheckBoxToggle.Checked = item.ItemIsChecked;
                if (item.ItemImage != null) dxCheckBoxToggle.ImageNameNull = item.ItemImage;
                if (item.ItemImageUnChecked != null) dxCheckBoxToggle.ImageNameUnChecked = item.ItemImageUnChecked;
                if (item.ItemImageChecked != null) dxCheckBoxToggle.ImageNameChecked = item.ItemImageChecked;
            }

            barItem.PaintStyle = Convert(item.ItemPaintStyle);
            if (item.RibbonStyle != RibbonItemStyles.Default)
                barItem.RibbonStyle = Convert(item.RibbonStyle);

            if (item.ToolTip != null)
                barItem.SuperTip = GetSuperTip(item.ToolTip, item.ToolTipTitle, item.ItemText, item.ToolTipIcon);

            barItem.Tag = item;
        }
        /// <summary>
        /// Konvertuje typ <see cref="BarItemPaintStyle"/> na typ <see cref="DevExpress.XtraBars.BarItemPaintStyle"/>
        /// </summary>
        /// <param name="itemPaintStyle"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.BarItemPaintStyle Convert(BarItemPaintStyle itemPaintStyle)
        {
            int styles = (int)itemPaintStyle;
            return (DevExpress.XtraBars.BarItemPaintStyle)styles;
        }
        /// <summary>
        /// Konvertuje typ <see cref="RibbonItemStyles"/> na typ <see cref="DevExpress.XtraBars.Ribbon.RibbonItemStyles"/>
        /// </summary>
        /// <param name="ribbonStyle"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.Ribbon.RibbonItemStyles Convert(RibbonItemStyles ribbonStyle)
        {
            int styles = (int)ribbonStyle;
            return (DevExpress.XtraBars.Ribbon.RibbonItemStyles)styles;
        }
        protected DevExpress.Utils.SuperToolTip GetSuperTip(string text, string title, string itemText, string image)
        {
            if (text is null) return null;
            if (title == null) title = itemText;
            var superTip = new DevExpress.Utils.SuperToolTip();
            if (title != null)
            {
                var dxTitle = superTip.Items.AddTitle(title);
                if (image != null)
                {
                    dxTitle.ImageOptions.Images = ComponentConnector.GraphicsCache.GetImageList(WinFormServices.Drawing.UserGraphicsSize.Large);
                    dxTitle.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(image, WinFormServices.Drawing.UserGraphicsSize.Large);
                    dxTitle.ImageOptions.ImageToTextDistance = 12;
                }
                superTip.Items.AddSeparator();
            }
            var dxText = superTip.Items.Add(text);
            return superTip;
        }
        protected DevExpress.XtraBars.Ribbon.RibbonBarManager BarManager
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
        #region Kliknutí na prvek Ribbonu
        private void InitEvents()
        {
            ApplicationButtonClick += RibbonControl_ApplicationButtonClick;
            ItemClick += RibbonControl_ItemClick;
            PageCategoryClick += RibbonControl_PageCategoryClick;
            PageGroupCaptionButtonClick += RibbonControl_PageGroupCaptionButtonClick;
        }

        private void RibbonControl_ApplicationButtonClick(object sender, EventArgs e)
        {
        }

        private void RibbonControl_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (e.Item is null) return;
            if (!(e.Item.Tag is IMenuItem menuItem)) return;
            if (e.Item is DevExpress.XtraBars.BarCheckItem checkItem)
                menuItem.ItemIsChecked = checkItem.Checked;

            _RibbonItemClick(menuItem);
        }
        internal void RaiseRibbonItemClick(IMenuItem menuItem) { _RibbonItemClick(menuItem); }
        private void _RibbonItemClick(IMenuItem menuItem)
        {
            var handler = menuItem.ActionHandler;
            if (handler != null) handler.MenuItemAction(menuItem);

            var args = new TEventArgs<IMenuItem>(menuItem);
            OnRibbonItemClick(args);
            RibbonItemClick?.Invoke(this, args);
        }
        protected virtual void OnRibbonItemClick(TEventArgs<IMenuItem> args) { }
        public event EventHandler<TEventArgs<IMenuItem>> RibbonItemClick;

        private void RibbonControl_PageCategoryClick(object sender, DevExpress.XtraBars.Ribbon.PageCategoryClickEventArgs e)
        {
        }

        private void RibbonControl_PageGroupCaptionButtonClick(object sender, DevExpress.XtraBars.Ribbon.RibbonPageGroupEventArgs e)
        {
            if (e.PageGroup is null) return;
            if (!(e.PageGroup.Tag is IRibbonItem ribbonItem)) return;

            _RibbonGroupButtonClick(ribbonItem);
        }
        private void _RibbonGroupButtonClick(IRibbonItem ribbonItem)
        {
            var handler = ribbonItem.ActionHandler;
            if (handler != null) handler.MenuGroupAction(ribbonItem);

            var args = new TEventArgs<IRibbonItem>(ribbonItem);
            OnRibbonGroupButtonClick(args);
            RibbonGroupButtonClick?.Invoke(this, args);
        }
        protected virtual void OnRibbonGroupButtonClick(TEventArgs<IRibbonItem> args) { }
        public event EventHandler<TEventArgs<IRibbonItem>> RibbonGroupButtonClick;
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

            DxComponent.LogAddLineTime($"MergeRibbon: to Parent: {this.DebugName}; from Child: {(childRibbon?.ToString())}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
                DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: Current; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Odmergovat - Provést akci (pokud je) - Mergovat zpátky (vše nebo jen UpRibbony):
            int last = count - 1;
            var topRibbon = ribbonsUp[last];
            var topRibbonSelectedPage1 = topRibbon.SelectedPageId;

            try
            {
                // Všem Ribonům v řadě potlačíme CheckLazyContentEnabled:
                ribbonsUp.ForEach(r => r.CheckLazyContentEnabled = false);

                // UnMerge proběhne od posledního (=TopMost: count - 1) Ribbonu dolů až k našemu Parentu (u >= 1):
                for (int u = last; u >= 1; u--)
                {
                    ribbonsUp[u].UnMergeDxRibbon();
                }

                // Nyní máme náš Ribbon UnMergovaný...
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
            }

            // A protože po celou dobu byl potlačen CheckLazyContentEnabled, tak pro Top Ribbon to nyní provedu explicitně:
            var topRibbonSelectedPage2 = topRibbon.SelectedPageId;
            if (topRibbonSelectedPage2 != topRibbonSelectedPage1)
                topRibbon.CheckLazyContent(topRibbon.SelectedPage, false);

            DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: UnMerge + Action + Merge; Total Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Provede danou akci, do logu vepíše její čas
        /// </summary>
        /// <param name="action"></param>
        private void _RunLogAction(Action action)
        {
            if (action == null) return;
            var startTime = DxComponent.LogTimeCurrent;
            action();
            DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: RunAction; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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

            base.UnMergeRibbon();

            DxRibbonControl childDxRibbon = this.MergedChildDxRibbon;
            if (childDxRibbon != null)
                childDxRibbon.MergedIntoParentDxRibbon = null;

            this.MergedChildRibbon = null;

            DxComponent.LogAddLineTime($"UnMergeRibbon from Parent: {this.DebugName}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
                if (c > itemCount) break;
            }

            Rectangle clientBounds = this.ClientRectangle;
            int cr = clientBounds.Right - 6;
            if (r < cr) r = cr;
            return Rectangle.FromLTRB(l, t, r, b);
        }
        #endregion
        #region Ikonka vpravo
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
            Image image = GetImageRight(isSmallRibbon);
            if (image == null) return;
            Size imageNativeSize = image.Size;
            if (imageNativeSize.Width <= 0 || imageNativeSize.Height <= 0) return;

            Rectangle buttonsBounds = ButtonsBounds;
            int imageHeight = (isSmallRibbon ? 24 : 48);
            float ratio = (float)imageNativeSize.Width / (float)imageNativeSize.Height;
            int imageWidth = (int)(ratio * (float)imageHeight);

            Rectangle imageBounds = new Rectangle(buttonsBounds.Right - 6 - imageWidth, buttonsBounds.Y + 4, imageWidth, imageHeight);
            e.Graphics.DrawImage(image, imageBounds);

            OnPaintImageRightAfter(e);
        }
        /// <summary>
        /// Metoda vrátí vhodný obrázek pro obrázek vpravo pro aktuální velikost. 
        /// Může vrátit null.
        /// </summary>
        /// <param name="isSmallRibbon"></param>
        /// <returns></returns>
        private Image GetImageRight(bool isSmallRibbon)
        {
            if (!isSmallRibbon && _ImageRightFull != null) return _ImageRightFull;
            if (isSmallRibbon && _ImageRightMini != null) return _ImageRightMini;
            if (_ImageRightFull != null) return _ImageRightFull;
            return _ImageRightMini;
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
    }
    #endregion
    #region DxRibbonPage : stránka Ribbonu s podporou LazyContentItems, class DxRibbonPageLazyGroupInfo
    /// <summary>
    /// Kategorie
    /// </summary>
    public class DxRibbonPageCategory : DevExpress.XtraBars.Ribbon.RibbonPageCategory
    {
        public DxRibbonPageCategory() : base() { }
        public DxRibbonPageCategory(string text, Color color) : base(text, color) { }
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
        protected DxRibbonControl OwnerRibbon { get; private set; }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="ribbon"></param>
        protected void Init(DxRibbonControl ribbon)
        {
            this.OwnerRibbon = ribbon;
            LazyLoadInfo = null;
        }
        /// <summary>
        /// true pokud this stránka má nějaký aktivní LazyLoad obsah
        /// </summary>
        internal bool HasActiveLazyContent { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.HasActiveLazyContent); } }
        /// <summary>
        /// true pokud this stránka má aktivní LazyLoad obsah v režimu <see cref="RibbonContentMode.OnDemandLoadEveryTime"/>
        /// </summary>
        internal bool IsLazyLoadEveryTime { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime); } }
        /// <summary>
        /// Zajistí přípravu LazyLoad pro režim definovaný v daném prvku. Prvek si uschová.
        /// Založí grupu pokud dosud neexistuje a nastaví do ní první prvek do <see cref="DxRibbonLazyLoadInfo.PageOnDemandItem"/>
        /// a jeho režim <see cref="DxRibbonLazyLoadInfo.PageContentMode"/>.
        /// </summary>
        /// <param name="ribbonItem"></param>
        internal void PrepareOnDemandInfo(IRibbonItem ribbonItem)
        {
            var lazyGroupInfo = GetLazyLoadInfo(ribbonItem);
            if (lazyGroupInfo.PageOnDemandItem == null)
            {
                lazyGroupInfo.PageOnDemandItem = ribbonItem;
                lazyGroupInfo.PageContentMode = ribbonItem.PageContentMode;
            }
        }
        /// <summary>
        /// Přidá do this stránky další prvek do seznamu LazyContent
        /// </summary>
        /// <param name="ribbonItem"></param>
        public void AddLazyLoadItem(IRibbonItem ribbonItem)
        {
            var lazyGroupInfo = GetLazyLoadInfo(ribbonItem);
            lazyGroupInfo.Items.Add(ribbonItem);
        }
        /// <summary>
        /// Metoda je volána při aktivaci stránky this Ribbonu v situaci, kdy tato stránka má aktivní nějaký Lazy režim pro načtení svého obsahu.
        /// Může to být prostě opožděné vytváření fyzických controlů z dat v paměti, nebo reálné OnDemand donačítání obsahu z aplikačního serveru.
        /// Přidá prvky do this Ribbonu z dodané LazyGroup do this Ribbonu. Zde se prvky přidávají vždy jako reálné, už ne Lazy.
        /// </summary>
        /// <param name="isReFill">false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        public void PrepareRealLazyItems(bool isReFill)
        {
            if (this.HasActiveLazyContent)
                this.OwnerRibbon.PrepareRealLazyItems(this.LazyLoadInfo, isReFill);
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
        /// Vytvoří (pokud je třeba) a vrátí instanci <see cref="DxRibbonLazyLoadInfo"/> pro ukládání informací pro LazyContent
        /// </summary>
        /// <returns></returns>
        protected DxRibbonLazyLoadInfo GetLazyLoadInfo(IRibbonItem ribbonItem)
        {
            DxRibbonLazyLoadInfo lazyLoadInfo = LazyLoadInfo;
            if (lazyLoadInfo == null || lazyLoadInfo.IsEmpty)
            {
                lazyLoadInfo = new DxRibbonLazyLoadInfo(OwnerRibbon, this, ribbonItem);
                lazyLoadInfo.PageContentMode = RibbonContentMode.Static;
                this.Groups.Add(lazyLoadInfo.Group);
                LazyLoadInfo = lazyLoadInfo;
            }
            return lazyLoadInfo;
        }
        /// <summary>
        /// Definice dat pro LazyLoad content pro tuto Page. Obsahuje deklarace prvků i referenci na grupu, která LazyLoad zajistí.
        /// </summary>
        protected DxRibbonLazyLoadInfo LazyLoadInfo { get; private set; }
     

        internal static void ClearContentPage(DevExpress.XtraBars.Ribbon.RibbonPage page)
        {
            if (page is DxRibbonPage dxRibbonPage)
                dxRibbonPage.ClearContent();
            else
            {
                page.Groups.Clear();
            }
        }
        internal void ClearContent()
        {
            ClearContent(true, true);
        }
        internal void ClearContent(bool clearUserItems, bool clearLazyGroup)
        {
            if (clearUserItems)
            {
                string groupId = DxRibbonLazyLoadInfo.LazyLoadGroupId;
                var groupsToDelete = this.Groups.OfType<DevExpress.XtraBars.Ribbon.RibbonPageGroup>().Where(g => g.Name != groupId).ToList();
                var itemsToDelete = groupsToDelete.SelectMany(g => g.ItemLinks).Select(l => l.Item).ToList();
                groupsToDelete.ForEach(g => this.Groups.Remove(g));
                itemsToDelete.ForEach(i => this.OwnerRibbon.Items.Remove(i));
            }
            if (clearLazyGroup)
                this.RemoveLazyLoadInfo();
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
        /// <param name="ribbonItem"></param>
        public DxRibbonLazyLoadInfo(DxRibbonControl ownerRibbon, DxRibbonPage ownerPage, IRibbonItem ribbonItem)
        {
            this.OwnerRibbon = ownerRibbon;
            this.OwnerPage = ownerPage;
            this.FirstItem = ribbonItem;
            this.Items = new List<IRibbonItem>();
            this.CreateRibbonGui(ribbonItem);
            this.IsActive = true;
        }
        private void CreateRibbonGui(IRibbonItem ribbonItem)
        {
            this.Group = new DevExpress.XtraBars.Ribbon.RibbonPageGroup(LazyLoadGroupText)
            {
                Name = LazyLoadGroupId,
                // Visible = false,
                CaptionButtonVisible = DefaultBoolean.False
            };
            this.BarItem = OwnerRibbon.Items.CreateButton(LazyLoadButtonText);
            uint id = ++_LastLazyId;
            this.BarItem.Name = $"{ribbonItem.PageId}_{ribbonItem.GroupId}_Wait_{id}";
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
        /// <param name="isReFill">false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        /// <returns></returns>
        internal static bool TryGetLazyDxPages(DevExpress.XtraBars.Ribbon.RibbonPage page, bool isReFill, out List<DxRibbonPage> lazyDxPages)
        {
            lazyDxPages = null;
            if (page == null) return false;

            lazyDxPages = new List<DxRibbonPage>();
            AddLazyDxPages(page.Groups, isReFill, lazyDxPages);
            AddLazyDxPages(page.MergedGroups, isReFill, lazyDxPages);

            return (lazyDxPages.Count > 0);
        }

        private static void AddLazyDxPages(DevExpress.XtraBars.Ribbon.RibbonPageGroupCollection pageGroups, bool isReFill, List<DxRibbonPage> lazyDxPages)
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
                            if (isReFill && dxRibbonPage.IsLazyLoadEveryTime)
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
            this.FirstItem = null;
            this.Group = null;
            this.BarItem = null;
            this.Items.Clear();
            this.Items = null;
        }
        /// <summary>
        /// true pokud this prvek má nějaký aktivní LazyLoad obsah
        /// </summary>
        public bool HasActiveLazyContent { get { return (!this.IsEmpty && this.IsActive); } }
        /// <summary>
        /// Obsahuje true, pokud this instance už je prázdná = její Items byly už použity pro tvorbu BarItems a z instance byly odebrány reference na Ribbon i Page i Group.
        /// Taková instance může někde viset uložená v Tagu některé Mergované grupy. 
        /// Nepovažuje se ale za funkční a bude ignorována.
        /// </summary>
        public bool IsEmpty { get { return (OwnerRibbon == null || OwnerPage == null || Items == null || (!IsOnDemand && !HasItems)); } }
        /// <summary>
        /// Příznak, že this LazyGroup je aktivní
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Obsahuje true, pokud this instance definuje režim OnDemandLoad
        /// </summary>
        public bool IsOnDemand { get { var mode = this.PageContentMode; return (mode == RibbonContentMode.OnDemandLoadOnce || mode == RibbonContentMode.OnDemandLoadEveryTime); } }
        /// <summary>
        /// Obsahuje true, pokud this instance má nastřádané nějaké prvky k aktuálnímu zobrazení v poli <see cref="Items"/>.
        /// </summary>
        public bool HasItems { get { return (this.Items != null && this.Items.Count > 0); } }
        /// <summary>
        /// Prvek Ribbonu, který deklaruje režim stránky. Obsahuje údaje o stránce.
        /// </summary>
        public IRibbonItem PageOnDemandItem { get; set; }
        /// <summary>
        /// Režim práce stránky
        /// </summary>
        public RibbonContentMode PageContentMode { get; set; }
        public DxRibbonControl OwnerRibbon { get; private set; }
        public DxRibbonPage OwnerPage { get; private set; }
        public IRibbonItem FirstItem { get; private set; }
        public DevExpress.XtraBars.Ribbon.RibbonPageGroup Group { get; private set; }
        public DevExpress.XtraBars.BarButtonItem BarItem { get; private set; }
        public List<IRibbonItem> Items { get; private set; }
    }
    #endregion
    #region class DxCheckBoxToggle : Button reprezentující hodnotu "Checked" { NULL - false - true } s využitím tří ikonek 
    /// <summary>
    /// Button reprezentující hodnotu <see cref="Checked"/> { NULL - false - true } s využitím tří ikonek 
    /// </summary>
    public class DxCheckBoxToggle : DevExpress.XtraBars.BarButtonItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxCheckBoxToggle() : base() { Initialize(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        public DxCheckBoxToggle(DevExpress.XtraBars.BarManager manager, string caption) : base(manager, caption) { Initialize(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        /// <param name="imageIndex"></param>
        public DxCheckBoxToggle(DevExpress.XtraBars.BarManager manager, string caption, int imageIndex) : base(manager, caption, imageIndex) { Initialize(); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        /// <param name="imageIndex"></param>
        /// <param name="shortcut"></param>
        public DxCheckBoxToggle(DevExpress.XtraBars.BarManager manager, string caption, int imageIndex, DevExpress.XtraBars.BarShortcut shortcut) : base(manager, caption, imageIndex, shortcut) { Initialize(); }
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
            if (this.Ribbon is DxRibbonControl dxRibbonControl && this.Tag is IMenuItem menuItem)
            {
                menuItem.ItemIsChecked = this.Checked;
                dxRibbonControl.RaiseRibbonItemClick(menuItem);
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
    #region DxRibbonStatusBar
    /// <summary>
    /// Potomek StatusBaru
    /// </summary>
    public class DxRibbonStatusBar : DevExpress.XtraBars.Ribbon.RibbonStatusBar
    { }
    #endregion
    #region RibbonItem : základní implementace IRibbonItem
    /// <summary>
    /// RibbonItem : základní implementace <see cref="IRibbonItem"/>
    /// </summary>
    public class RibbonItem : IRibbonItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public RibbonItem()
        {
            PageId = "";
            PageIsHome = true;
            GroupId = "";
            GroupText = "";
            ItemEnabled = true;
            ItemType = RibbonItemType.Button;
            ItemPaintStyle = BarItemPaintStyle.Standard;
            RibbonStyle = RibbonItemStyles.Large;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            if (!String.IsNullOrEmpty(PageText)) text += $"Page: {PageText}, ";
            if (!String.IsNullOrEmpty(GroupText)) text += $"Group: {GroupText}, ";
            text += $"Item: {ItemText}, Type: {ItemType}";
            return text;
        }
        /// <summary>
        /// Komparátor pro třídění: <see cref="IRibbonItem.PageOrder"/>, <see cref="IRibbonItem.GroupOrder"/>, <see cref="IMenuItem.ItemOrder"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int CompareByOrder(IRibbonItem a, IRibbonItem b)
        {
            int cmp = a.PageOrder.CompareTo(b.PageOrder);
            if (cmp == 0) cmp = a.GroupOrder.CompareTo(b.GroupOrder);
            if (cmp == 0) cmp = a.ItemOrder.CompareTo(b.ItemOrder);
            return cmp;
        }
        public string CategoryId { get; set; }
        public string CategoryText { get; set; }
        public Color CategoryColor { get; set; }
        public bool CategoryVisible { get; set; }
        public bool PageIsHome { get; set; }
        public int PageOrder { get; set; }
        public string PageId { get; set; }
        public string PageText { get; set; }
        public Color PageColor { get; set; }
        public RibbonPageType PageType { get; set; }
        public RibbonContentMode PageContentMode { get; set; }
        public int GroupOrder { get; set; }
        public string GroupId { get; set; }
        public string GroupText { get; set; }
        public string GroupImage { get; set; }
        public bool GroupButtonVisible { get; set; }
        public int ItemOrder { get; set; }
        public string ItemId { get; set; }
        public RibbonItemType ItemType { get; set; }
        public string ItemText { get; set; }
        public string ItemImage { get; set; }
        public string ItemImageUnChecked { get; set; }
        public string ItemImageChecked { get; set; }
        public bool ItemIsFirstInGroup { get; set; }
        public RibbonItemStyles RibbonStyle { get; set; }
        public bool ItemEnabled { get; set; }
        public int? ItemToolbarOrder { get; set; }
        public bool? ItemIsChecked { get; set; }
        public BarItemPaintStyle ItemPaintStyle { get; set; }
        public string HotKey { get; set; }
        public string ToolTip { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipIcon { get; set; }
        public RibbonContentMode SubItemsContentMode { get; set; }
        public IMenuItem[] SubItems { get; set; }
        public object Tag { get; set; }
        public IMenuItemActionHandler ActionHandler { get { return __ActionHandler?.Target; } set { __ActionHandler = (value != null ? new WeakTarget<IMenuItemActionHandler>(value) : null); } }
        private WeakTarget<IMenuItemActionHandler> __ActionHandler;
    }
    #endregion
    #region Interface IRibbonItem a IRibbonData;  Enumy RibbonItemType a RibbonPageType
    /// <summary>
    /// Definice prvku umístěného v Ribbonu
    /// </summary>
    public interface IRibbonItem : IMenuItem
    {
        string CategoryId { get; }
        string CategoryText { get; }
        Color CategoryColor { get; }
        bool CategoryVisible { get; }
        string PageId { get; }
        bool PageIsHome { get; }
        int PageOrder { get; set; }
        string PageText { get; }
        Color PageColor { get; }
        RibbonPageType PageType { get; }
        RibbonContentMode PageContentMode { get; }
        int GroupOrder { get; set; }
        string GroupId { get; }
        string GroupText { get; }
        string GroupImage { get; }
        bool GroupButtonVisible { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public interface IMenuItem
    {
        string ItemId { get; }
        int ItemOrder { get; set; }
        bool ItemIsFirstInGroup { get; }
        RibbonItemType ItemType { get; }
        RibbonItemStyles RibbonStyle { get; }
        bool ItemEnabled { get; }
        int? ItemToolbarOrder { get; }
        /// <summary>
        /// Jméno ikony.
        /// Pro prvek typu <see cref="RibbonItemType.CheckBoxToggle"/> tato ikona reprezentuje stav, kdy <see cref="ItemIsChecked"/> = NULL.
        /// </summary>
        string ItemImage { get; }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ItemImageUnChecked { get; }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ItemImageChecked { get; }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Ribbonu (uživatelem) je do této property setována aktuální hodnota z Ribbonu 
        /// a poté je vyvolána událost <see cref="DxRibbonControl.RibbonItemClick"/>.
        /// Hodnota může být null, pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat)
        /// </summary>
        bool? ItemIsChecked { get; set; }
        BarItemPaintStyle ItemPaintStyle { get; }
        string ItemText { get; }
        string HotKey { get; }
        string ToolTip { get; }
        string ToolTipTitle { get; }
        string ToolTipIcon { get; }
        RibbonContentMode SubItemsContentMode { get; }
        IMenuItem[] SubItems { get; }
        object Tag { get; set; }
        IMenuItemActionHandler ActionHandler { get; }
    }
    /// <summary>
    /// Předpis rozhraní pro třídu, jejíž instance bude dostávat informaci o kliknutí na daný prvek menu
    /// </summary>
    public interface IMenuItemActionHandler
    {
        /// <summary>
        /// Uživatel kliknul na prvek menu (tlačítko, položka), aktivní prvek je v parametru.
        /// </summary>
        /// <param name="menuItem"></param>
        void MenuItemAction(IMenuItem menuItem);
        /// <summary>
        /// Uživatel kliknul na tlačítko skupiny menu, první prvek který deklaruje danou grupu je v parametru.
        /// Pozor při mergování více Ribbonů, pak dochází i k mergování stejnojmenných skupin na stejnojmenné stránce, a tady dostává řízení první kdo danou grupu založil!        /// </summary>
        /// <param name="ribbonItem"></param>
        void MenuGroupAction(IRibbonItem ribbonItem);
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
        /// V této položce <see cref="IRibbonItem"/> (v té, která deklaruje stránku s tímto režimem) se pak typicky naplní 
        /// <see cref="IMenuItem.ItemType"/> = <see cref="RibbonItemType.None"/>, a nevznikne žádný vizuální prvek ani grupa.
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
        /// V této položce <see cref="IRibbonItem"/> (v té, která deklaruje stránku s tímto režimem) se pak typicky naplní 
        /// <see cref="IMenuItem.ItemType"/> = <see cref="RibbonItemType.None"/>, a nevznikne žádný vizuální prvek ani grupa.
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
        /// Použije se tehdy, když chceme deklarovat určitou stránku Ribbonu, která nemá dosud definovaný obsah (prvky).
        /// Taková stránka má typicky deklarovaný režim obsahu <see cref="IRibbonItem.PageContentMode"/> = <see cref="RibbonContentMode.OnDemandLoadOnce"/>
        /// nebo <see cref="RibbonContentMode.OnDemandLoadEveryTime"/>. Stránka se vytvoří, je prázdná, ale při její aktivaci uživatelem se vyvolá logika donačítání obsahu.
        /// </summary>
        None,
        Button,
        ButtonGroup,
        SplitButton,
        CheckBoxStandard,
        /// <summary>
        /// Button se stavem Checked, který může být NULL (výchozí hodnota). 
        /// Pokud má být výchozí stav false, je třeba jej do <see cref="IMenuItem.ItemIsChecked"/> vložit!
        /// Lze specifikovat ikony pro všechny tři stavy (NULL - false - true)
        /// </summary>
        CheckBoxToggle,
        RadioItem,
        Menu,
        SkinSetDropDown,
        SkinPaletteDropDown,
        SkinPaletteGallery

    }
    #endregion
}
