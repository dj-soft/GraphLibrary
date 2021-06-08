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

            ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
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

            this.AllowCustomization = true;
            this.AllowGlyphSkinning = false;       // nikdy ne true!
            this.ShowItemCaptionsInQAT = true;
            this.ShowQatLocationSelector = true;

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
        /// Vrátí soupis stránek z this ribbonu z daných pozic
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public List<DevExpress.XtraBars.Ribbon.RibbonPage> GetPages(PagePosition position)
        {
            List<DevExpress.XtraBars.Ribbon.RibbonPage> result = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();

            bool withDefault = position.HasFlag(PagePosition.Default);
            bool withCategories = position.HasFlag(PagePosition.Categories);
            bool withMergedDefault = position.HasFlag(PagePosition.MergedDefault);
            bool withMergedCategories = position.HasFlag(PagePosition.MergedCategories);

            if (withDefault)
                result.AddRange(this.Pages);

            if (withCategories)
                result.AddRange(this.Categories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.Pages));

            if (withCategories && (withMergedDefault || withMergedCategories))
                result.AddRange(this.Categories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().SelectMany(c => c.MergedPages));


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
            if (_IsPageOnPosition(page, PagePosition.Categories)) return PagePosition.Categories;
            if (_IsPageOnPosition(page, PagePosition.MergedDefault)) return PagePosition.MergedDefault;
            if (_IsPageOnPosition(page, PagePosition.MergedCategories)) return PagePosition.MergedCategories;
            return PagePosition.None;
        }
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
            Categories = 0x02,
            /// <summary>
            /// Mergovaná základní stránka
            /// </summary>
            MergedDefault = 0x10,
            /// <summary>
            /// Stránka patřící mergovaé kategorii
            /// </summary>
            MergedCategories = 0x20,

            /// <summary>
            /// Vlastní stránky <see cref="Default"/> a <see cref="Categories"/>
            /// </summary>
            AllOwn = Default | Categories,
            /// <summary>
            /// Mergované stránky <see cref="MergedDefault"/> a <see cref="MergedCategories"/>
            /// </summary>
            AllMerged = MergedDefault | MergedCategories,
            /// <summary>
            /// Všechny stránky
            /// </summary>
            All = Default | Categories | MergedDefault | MergedCategories
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
        #endregion
        #region Tvorba obsahu Ribbonu: Clear, AddPages, ReFillPages
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

            DxComponent.LogAddLineTime($" === ClearRibbon {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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

            // var x = this.PageHeaderItemLinks.ToArray();
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
        /// Smaže obsah (itemy a grupy) ale ponechá Pages a Categories
        /// </summary>
        private void _ClearPageContents()
        {
            var startTime = DxComponent.LogTimeCurrent;

            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.AllOwnPages)
                DxRibbonPage.ClearContentPage(page);

            int removeItemsCount = 0;
            this._ClearItems(ref removeItemsCount);

            DxComponent.LogAddLineTime($" === ClearPageContents {this.DebugName}; Removed {removeItemsCount} items; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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
        /// </summary>
        private void _RemoveVoidContainers()
        {
            var categories = this.Categories.OfType<DevExpress.XtraBars.Ribbon.RibbonPageCategory>().ToArray();
            foreach (var category in categories)
            {
                var cPages = category.Pages.Where(p => p.Groups.Count == 0).ToArray();
                foreach (var cPage in cPages)
                    category.Pages.Remove(cPage);
                if (category.Pages.Count == 0)
                {
                    int index = this.Categories.IndexOf(category.Name);
                    if (index >= 0)
                        this.Categories.RemoveAt(index);
                }
            }

            var nPages = this.Pages.Where(p => p.Groups.Count == 0).ToArray();
            foreach (var nPage in nPages)
                this.Pages.Remove(nPage);
        }
        /// <summary>
        /// Přidá dodané prvky do this ribbonu, zakládá stránky, kategorie, grupy...
        /// Pokud má být aktivní <see cref="UseLazyContentCreate"/>, musí být nastaveno na true před přidáním prvku.
        /// <para/>
        /// Tato metoda si sama dokáže zajistit invokaci GUI threadu.
        /// Pokud v době volání je aktuální Ribbon mergovaný v parent ribbonech, pak si korektně zajistí re-merge (=promítnutí nového obsahu do parent ribbonu).
        /// </summary>
        /// <param name="iRibbonPages">Definice obsahu</param>
        /// <param name="clearCurrentContent">Smazat stávající obsah Ribbonu, smaže se bez bliknutí</param>
        public void AddPages(IEnumerable<IRibbonPage> iRibbonPages, bool clearCurrentContent = false)
        {
            this.RunInGui(() =>
            {
                this.ModifyCurrentDxContent(() =>
                {
                    if (clearCurrentContent) _ClearPageContents();
                    _AddPages(iRibbonPages, this.UseLazyContentCreate, false, "Fill");
                    if (clearCurrentContent) _RemoveVoidContainers();
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
        /// <param name="iRibbonPages"></param>
        public void ReFillPages(IEnumerable<IRibbonPage> iRibbonPages)
        {
            this.RunInGui(() =>
            {
                this.ModifyCurrentDxContent(() =>
                {
                    var reFillPages = _PrepareReFill(iRibbonPages, true);
                    _AddPages(iRibbonPages, this.UseLazyContentCreate, true, "OnDemand");
                    _FinishReFill(reFillPages);
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
        /// Z this Ribbonu odstraní takové stránky z daného seznamu, které aktuálně neobsahují žádnou grupu = jsou prázdné
        /// </summary>
        /// <param name="ribbonPages"></param>
        private void _FinishReFill(List<DevExpress.XtraBars.Ribbon.RibbonPage> ribbonPages)
        {
            foreach (var ribbonPage in ribbonPages)
            {
                if (ribbonPage.Groups.Count == 0)
                    this.Pages.Remove(ribbonPage);
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
            // Nemám data nebo jsou neaktivní (=už jsme v procesu PrepareRealLazyItems):
            if (lazyGroup == null || !lazyGroup.IsActive) return;

            // Pokud právě nyní plníme Ribbon daty dodanými OnDemand (=data jsou v ribbonu už vložena) a režime je EveryTime, pak skončíme;
            // protože: a) obsah mazat nechceme,  b) prvky opakovaně vkládat nechceme (už tam jsou),  c) LazyGroup si chceme ponechat,  d) další event spouštět nebudeme:
            if (isReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime) return;

            lazyGroup.IsActive = false;
            bool addItems = lazyGroup.HasItems && lazyGroup.PageContentMode == RibbonContentMode.Static;
            var iRibbonPage = lazyGroup.PageData;

            // V této jediné situaci si necháme LazyInfo: když proběhlo naplnění prvků OnDemand => ReFill, a režim OnDemand je "Po každé aktivaci stránky":
            //  Pak máme na aktuální stránce uložené reálné prvky, a současně tam je OnDemand "Group", která zajistí nový OnDemand při následující aktivaci stránky!
            bool leaveLazyInfo = (isReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime);

            // Za určité situace provedeme Clear prvků stránky: pokud NENÍ ReFill a pokud je režim EveryTime = 
            //  právě zobrazujeme stránku, která při každém zobrazení načítá nová data, takže předešlá data máme zahodit...
            bool clearContent = (!isReFill && lazyGroup.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime);
            
            if (addItems || clearContent || !leaveLazyInfo)
            {   // Když je důvod něco provádět (máme nové prvky, nebo budeme odstraňovat LazyInfo z Ribbonu):
                this.ModifyCurrentDxContent(() =>
                {
                    if (clearContent)
                    {
                        lazyGroup.OwnerPage?.ClearContent(true, !leaveLazyInfo);
                    }
                    if (addItems)
                    {   // Když máme co zobrazit, tak nyní vygenerujeme reálné Grupy a BarItems:
                        string info = "LazyFill; Page: " + lazyGroup.PageData.PageText;
                        _AddPageLazy(lazyGroup.PageData, false, false, info);        // Fyzicky vygenerujeme prvky stránky Ribbonu
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

            var startTime = DxComponent.LogTimeCurrent;
            var list = DataRibbonPage.SortPages(iRibbonPages);
            int count = 0;
            foreach (var iRibbonPage in list)
                _AddPage(iRibbonPage, isLazyContentFill, isOnDemandFill, ref count);

            DxComponent.LogAddLineTime($" === Ribbon {DebugName}: {logText} {list.Count} item[s]; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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

            DxComponent.LogAddLineTime($" === Ribbon {DebugName}: {logText}; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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

            var category = GetCategory(iRibbonPage.Category);                   // Pokud je to třeba, vygeneruje Kategorii
            var page = GetPage(iRibbonPage, category);                          // Najde / Vytvoří stránku do this.Pages nebo do category.Pages
            if (page is null) return;

            if (this.SelectedPageId == page.Name) isLazyContentFill = false;    // Pokud v Ribbonu je aktuálně vybraná ta stránka, která se nyní generuje, pak se NEBUDE plnit v režimu Lazy
            bool createContent = page.PreparePageForContent(iRibbonPage, isLazyContentFill, isOnDemandFill);
            if (!createContent) return;                                         // víc už dělat nemusíme. Máme stránku a v ní LazyInfo.

            var list = DataRibbonGroup.SortGroups(iRibbonPage.Groups);
            foreach (var iRibbonGroup in list)
            {
                iRibbonGroup.ParentPage = iRibbonPage;
                _AddGroup(iRibbonGroup, page, ref count);
            }
        }
        /// <summary>
        /// Metoda přidá danou grupu do dané stránky
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        private void _AddGroup(IRibbonGroup iRibbonGroup, DxRibbonPage page, ref int count)
        {
            if (iRibbonGroup == null || page == null) return;
            var group = GetGroup(iRibbonGroup, page);
            if (group is null) return;
          
            var list = DataMenuItem.SortItems(iRibbonGroup.Items);
            foreach (var iMenuItem in list)
            {
                iMenuItem.ParentGroup = iRibbonGroup;
                _AddBarItem(iMenuItem, group, ref count);
            }
        }
        /// <summary>
        /// Metoda přidá daný prvek do dané grupy
        /// </summary>
        /// <param name="iMenuItem"></param>
        /// <param name="group"></param>
        /// <param name="count"></param>
        private void _AddBarItem(IMenuItem iMenuItem, DxRibbonGroup group, ref int count)
        {
            if (iMenuItem == null || group == null) return;
            var barItem = GetBarItem(iMenuItem, group, ref count, true, false);
            if (barItem is null) return;
            
            // více není třeba.
        }
        /// <summary>
        /// Metoda vrátí true, pokud se má vytvářet plný obsah dané stránky Ribbonu.
        /// Vrátí false, i když by se měly vytvořit nějaké prvky pro QuickAccessToolbar.
        /// </summary>
        /// <param name="pageData"></param>
        /// <param name="isLazyContent"></param>
        /// <returns></returns>
        private bool _NeedCreateFullContentForPage(IRibbonPage pageData, bool isLazyContent)
        {
            if (this.SelectedPageId == pageData.PageId) return true;           // Pro aktivní stránku musíme vytvářet obsah, nemá smysl čekat...
            if (!isLazyContent) return true;                                   // Pokud není režim LazyLoad, pak obsah vytváříme hned

            return false;

            // Není nutno generovat obsah hned, krom případu, kdy stránka obsahuje prvky, které mají být v Toolbaru (QuickAccessToolbar).
            // Jde o stránku, která není aktuálně zobrazena (SelectedPageId), ale Toolbar je vidět vždy.
            // Prvky v Toolbaru nejde generovat přímo do Toolbaru, musí být přítomny v nějaké stránce Ribbonu a pak vloženy do kolekce this.Toolbar.ItemLinks!
            // Projdeme tedy obsah stránky Ribbonu a vytáhneme všechny Items, které mají příznak Toolbar:
            //toolItems = DataRibbonPage.GetAllToolbarItems(pageData);
            //return (toolItems != null && toolItems.Length > 0);
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

            // Něco k DevExpress a k Ribbonům si přečti v XML komentáři k metodě DxRibbonPageLazyGroupInfo.TryGetLazyDxPages!

            if (!DxRibbonLazyLoadInfo.TryGetLazyDxPages(page, isReFill, out var lazyDxPages)) return;

            // Pro danou stránku jsme našli jednu nebo i více definic LazyGroups.
            // Na základní stránce (stránka definovaná přímo v Ribbonu) je vždy jen jedna LazyGroup.
            // Ale pokud budu mergovat postupně více Ribbonů nahoru: (1) => (2); (2) => (3),
            //  pak Ribbon 1 může mít stránku "PageDoc" a obsahuje grupu pro LazyInfo s BarItem Buttonem, který v Tagu nese konkrétní instanci DxRibbonLazyLoadInfo.
            //  Když se Ribbon 1 přimerguje do stejnojmenné stránky "PageDoc" Ribbonu 2, kde se nachází jeho grupa LazyInfo, tak se v dané grupě sejdou
            //  už dva BarItem Buttony, každý nese v Tagu svou instanci DxRibbonLazyLoadInfo. A tak lze mergovat DxRibbonLazyLoadInfo víceúrovňově.
            // Pak tedy pro jednu stránku můžeme získat sadu instancí DxRibbonLazyLoadInfo, které definují LazyLoad nebo OnDemand načítání obsahu.
            foreach (var lazyDxPage in lazyDxPages)
                lazyDxPage.PrepareRealLazyItems(isReFill);
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
                this.LastSelectedPageId = pageId;
        }
        /// <summary>
        /// Pokud je true (běžný aktivní stav), pak se po aktivaci stránky provádí kontroly LazyLoad obsahu.
        /// Nastavením na false se tyto kotnroly deaktivují. Používá se při hromadnám Unmerge a zpět Merge, kdy dochází ke změnám SelectedPage v každém kroku, a zcela zbytečně.
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
        #region Fyzická tvorba prvků Ribbonu (Kategorie, Stránka, Grupa, Prvek, konkrétní prvky, ...)
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí kategorii pro daná data
        /// </summary>
        /// <param name="iRibbonCategory"></param>
        /// <param name="enableNew"></param>
        /// <returns></returns>
        protected DxRibbonPageCategory GetCategory(IRibbonCategory iRibbonCategory, bool enableNew = true)
        {
            if (iRibbonCategory is null || String.IsNullOrEmpty(iRibbonCategory.CategoryId)) return null;
            DxRibbonPageCategory category = PageCategories.GetCategoryByName(iRibbonCategory.CategoryId) as DxRibbonPageCategory;
            if (category is null && enableNew)
            {
                category = new DxRibbonPageCategory(iRibbonCategory.CategoryText, iRibbonCategory.CategoryColor, iRibbonCategory.CategoryVisible);
                category.Name = iRibbonCategory.CategoryId;
                category.Tag = iRibbonCategory;
                PageCategories.Add(category);
            }
            if (category != null) category.Tag = iRibbonCategory;
            return category;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí stránku pro daná data.
        /// Stránku přidá do this Ribbonu nebo do dané kategorie.
        /// </summary>
        /// <param name="iRibbonPage"></param>
        /// <param name="category"></param>
        /// <param name="enableNew"></param>
        /// <returns></returns>
        protected DxRibbonPage GetPage(IRibbonPage iRibbonPage, DxRibbonPageCategory category = null, bool enableNew = true)
        {
            if (iRibbonPage is null) return null;
            bool isCategory = !(category is null);
            DxRibbonPage page = (isCategory ? 
                category.Pages.FirstOrDefault(r => (r.Name == iRibbonPage.PageId)) : 
                Pages.FirstOrDefault(r => r.Name == iRibbonPage.PageId)) as DxRibbonPage;

            if (page is null && enableNew)
            {
                page = new DxRibbonPage(this, iRibbonPage.PageText)
                {
                    Name = iRibbonPage.PageId,
                    Tag = iRibbonPage
                };
                if (isCategory)
                    category.Pages.Add(page);
                else
                    Pages.Add(page);
            }
            page.PageData = iRibbonPage;
            return page;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí grupu pro daná data.
        /// Grupu přidá do dané stránky.
        /// </summary>
        /// <param name="iRibbonGroup"></param>
        /// <param name="page"></param>
        /// <param name="enableNew"></param>
        /// <returns></returns>
        protected DxRibbonGroup GetGroup(IRibbonGroup iRibbonGroup, DxRibbonPage page, bool enableNew = true)
        {
            if (iRibbonGroup is null || page is null) return null;
            DxRibbonGroup group = page.Groups.GetGroupByName(iRibbonGroup.GroupId) as DxRibbonGroup;
            if (group is null && enableNew)
            {
                group = new DxRibbonGroup(iRibbonGroup.GroupText)
                {
                    Name = iRibbonGroup.GroupId,
                    CaptionButtonVisible = (iRibbonGroup.GroupButtonVisible ? DefaultBoolean.True : DefaultBoolean.False),
                    State = DevExpress.XtraBars.Ribbon.RibbonPageGroupState.Auto,
                    Tag = iRibbonGroup
                };
                group.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(iRibbonGroup.GroupImage, ImagesSize, iRibbonGroup.GroupText);
                page.Groups.Add(group);
            }
            return group;
        }
        /// <summary>
        /// Rozpozná, najde, vytvoří a vrátí BarItem pro daná data.
        /// BarItem přidá do dané grupy.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="group"></param>
        /// <param name="count"></param>
        /// <param name="enableNew"></param>
        /// <param name="reallyCreateSubItems"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem GetBarItem(IMenuItem item, DevExpress.XtraBars.Ribbon.RibbonPageGroup group, ref int count, bool enableNew = true, bool reallyCreateSubItems = false)
        {
            if (item is null || group is null) return null;
            DevExpress.XtraBars.BarItem barItem = Items[item.ItemId];
            if (barItem is null)
            {
                if (!enableNew) return null;
                barItem = CreateBarItem(item, reallyCreateSubItems, ref count);
                if (barItem is null) return null;
                var barLink = group.ItemLinks.Add(barItem);
                if (item.ItemIsFirstInGroup) barLink.BeginGroup = true;
            }
            else
            {
                FillBarItem(barItem, item);
            }

            if (barItem.Tag == null)
                // Některé druhy prvků (například Menu) už mají Tag naplněn "něčím lepším", tak to nebudeme ničit:
                barItem.Tag = item;

            if (item.ItemToolbarOrder.HasValue)
                this.Toolbar.ItemLinks.Add(barItem);

            return barItem;
        }
        /// <summary>
        /// Vytvoří prvek BarItem pro daná data a vrátí jej.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="reallyCreateSubItems">Skutečně se mají vytvářet SubMenu?</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.BarItem CreateBarItem(IMenuItem item, bool reallyCreateSubItems, ref int count)
        {
            DevExpress.XtraBars.BarItem barItem = null;
            switch (item.ItemType)
            {
                case RibbonItemType.ButtonGroup:
                    count++;
                    DevExpress.XtraBars.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(item, item.SubItems, reallyCreateSubItems, ref count));
                    buttonGroup.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    count++;
                    var dxPopup = CreateXPopupMenu(item, item.SubItems, reallyCreateSubItems, ref count);
                    DevExpress.XtraBars.BarButtonItem splitButton = Items.CreateSplitButton(item.ItemText, dxPopup);
                    barItem = splitButton;
                    break;
                case RibbonItemType.CheckBoxStandard:
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
                    PrepareXBarMenu(item, item.SubItems, menu, reallyCreateSubItems, ref count);
                    barItem = menu;
                    break;
                case RibbonItemType.InRibbonGallery:
                    count++;
                    var galleryItem = CreateGalleryItem(item);
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

        protected DevExpress.XtraBars.BarBaseButtonItem[] GetBarBaseButtons(IMenuItem parentItem, IEnumerable<IMenuItem> subItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarBaseButtonItem> baseButtons = new List<DevExpress.XtraBars.BarBaseButtonItem>();
            if (subItems != null && reallyCreate)
            {
                foreach (IMenuItem subItem in subItems)
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
        /// <param name="item"></param>
        /// <returns></returns>
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

        // PopupMenu pro SplitButton:
        /// <summary>
        /// Vytvoří a vrátí objekt <see cref="DevExpress.XtraBars.PopupMenu"/>, který se používá pro prvek typu <see cref="RibbonItemType.SplitButton"/> jako jeho DropDown menu
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="reallyCreate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected DevExpress.XtraBars.PopupMenu CreateXPopupMenu(IMenuItem parentItem, IEnumerable<IMenuItem> subItems, bool reallyCreate, ref int count)
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
            dxPopup.Tag = null;                            // dxPopup není prvek Ribbonu, ale PopupMenu navázané na SplitButtonu. Jeho Tag nechť je null, protože definující prvek IMenuItem je v Tagu toho SplitButtonu.

            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            _XPopupMenu_FillItems(dxPopup, lazySubItems.ParentItem, lazySubItems.SubItems, ref count);
            DxComponent.LogAddLineTime($"LazyLoad SplitButton menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.PopupMenu"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu tvorby menu (při inicializaci nebo při BeforePopup v LazyInit modu)
        /// </summary>
        /// <param name="dxPopup"></param>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="count"></param>
        private void _XPopupMenu_FillItems(DevExpress.XtraBars.PopupMenu dxPopup, IMenuItem parentItem, IEnumerable<IMenuItem> subItems, ref int count)
        {
            foreach (IMenuItem subItem in subItems)
            {
                subItem.ParentItem = parentItem;
                subItem.ParentGroup = parentItem.ParentGroup;
                DevExpress.XtraBars.BarItem barItem = CreateBarItem(subItem, true, ref count);
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
        private void PrepareXBarMenu(IMenuItem parentItem, IEnumerable<IMenuItem> subItems, DevExpress.XtraBars.BarSubItem xBarMenu, bool reallyCreate, ref int count)
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
            xBarMenu.Tag = lazySubItems.ParentItem;        // Tady je xBarMenu = přímo prvek Ribbonu, a tam chci mít v Tagu referenci na IMenuItem, který prvek založil...

            var startTime = DxComponent.LogTimeCurrent;
            int count = 0;
            xBarMenu.ItemLinks.Clear();                    // V téhle kolekci byl jeden prvek "...", který mi zajistil aktivaci menu = zdejší metoda Popup.
            _XBarMenu_FillItems(xBarMenu, lazySubItems.ParentItem, lazySubItems.SubItems, ref count);
            DxComponent.LogAddLineTime($"LazyLoad Menu create: {count} items, {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Do daného menu <see cref="DevExpress.XtraBars.BarSubItem"/> vygeneruje všechny jeho položky.
        /// Volá se v procesu tvorby menu (při inicializaci nebo při BeforePopup v LazyInit modu)
        /// </summary>
        /// <param name="xBarMenu"></param>
        /// <param name="parentItem"></param>
        /// <param name="subItems"></param>
        /// <param name="count"></param>
        private void _XBarMenu_FillItems(DevExpress.XtraBars.BarSubItem xBarMenu, IMenuItem parentItem, IEnumerable<IMenuItem> subItems, ref int count)
        {
            var menuItems = GetBarSubItems(parentItem, subItems, true, ref count);
            foreach (var menuItem in menuItems)
            {
                var menuLink = xBarMenu.AddItem(menuItem);
                if ((menuItem.Tag is IMenuItem ribbonData) && ribbonData.ItemIsFirstInGroup)
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
        protected DevExpress.XtraBars.BarItem[] GetBarSubItems(IMenuItem parentItem, IEnumerable<IMenuItem> subItems, bool reallyCreate, ref int count)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (subItems != null && reallyCreate)
            {
                foreach (IMenuItem subItem in subItems)
                {
                    subItem.ParentItem = parentItem;
                    subItem.ParentGroup = parentItem.ParentGroup;
                    DevExpress.XtraBars.BarItem barItem = CreateBarItem(subItem, true, ref count);
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
        /// <param name="item"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.RibbonGalleryBarItem CreateGalleryItem(IMenuItem item)
        {
            var galleryBarItem = new DevExpress.XtraBars.RibbonGalleryBarItem(this.BarManager);
            galleryBarItem.Gallery.Images = ComponentConnector.GraphicsCache.GetImageList();
            galleryBarItem.Gallery.HoverImages = ComponentConnector.GraphicsCache.GetImageList();
            galleryBarItem.Gallery.AllowHoverImages = true;
            galleryBarItem.SuperTip = GetSuperTip(item);

            // Create a gallery item group and add it to the gallery.
            var galleryGroup = new DevExpress.XtraBars.Ribbon.GalleryItemGroup();
            galleryBarItem.Gallery.Groups.Add(galleryGroup);

            // Create gallery items and add them to the group.
            List<DevExpress.XtraBars.Ribbon.GalleryItem> items = new List<DevExpress.XtraBars.Ribbon.GalleryItem>();

            foreach (var subItem in item.SubItems)
                items.Add(CreateGallerySubItem(subItem));

            galleryGroup.Items.AddRange(items.ToArray());

            // Specify the number of items to display horizontally.
            galleryBarItem.Gallery.ColumnCount = 4;

            return galleryBarItem;
        }
        /// <summary>
        /// Vytvoří a vrátí jeden prvek galerie
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.Ribbon.GalleryItem CreateGallerySubItem(IMenuItem item)
        {
            var galleryItem = new DevExpress.XtraBars.Ribbon.GalleryItem();
            galleryItem.ImageIndex = galleryItem.HoverImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage);
            galleryItem.SuperTip = this.GetSuperTip(item);
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
            public LazySubItemsInfo(IMenuItem parentItem, IEnumerable<IMenuItem> subItems)
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
            public IMenuItem ParentItem { get; private set; }
            /// <summary>
            /// SubPrvky
            /// </summary>
            public IEnumerable<IMenuItem> SubItems { get; private set; }
        }
        private void RibbonControl_SubItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            RibbonControl_ItemClick(sender, e);
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
                    DxComponent.ApplyImage(barItem.ImageOptions, resourceName: imageName);
                }
                else
                {
                    barItem.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(imageName, ImagesSize, item.ItemText);
                    barItem.LargeImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(imageName, LargeImagesSize, item.ItemText);
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

            if (barItem.Tag == null)
                // Některé druhy prvků (například Menu) už mají Tag naplněn "něčím lepším", tak to nebudeme ničit:
                barItem.Tag = item;
        }
        /// <summary>
        /// Vygeneruje a vrátí SuperTip pro daný prvek Ribbonu
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected DevExpress.Utils.SuperToolTip GetSuperTip(IMenuItem item)
        {
            return GetSuperTip(item.ToolTip, item.ToolTipTitle, item.ItemText, item.ToolTipIcon);
        }
        /// <summary>
        /// Vygeneruje a vrátí SuperTip pro dané texty
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="itemText"></param>
        /// <param name="image"></param>
        /// <returns></returns>
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
            superTip.Items.Add(text);
            return superTip;
        }
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
            if (_TryGetIRibbonItem(e.Item, out IMenuItem menuItem))
            {
                if (e.Item is DevExpress.XtraBars.BarCheckItem checkItem)
                    menuItem.ItemIsChecked = checkItem.Checked;

                _RibbonItemClick(menuItem);
            }
        }
        /// <summary>
        /// Provede akci odpovídající kliknutí na prvek Ribbonu, na vstupu jsou data prvku
        /// </summary>
        /// <param name="iMenuItem"></param>
        internal void RaiseRibbonItemClick(IMenuItem iMenuItem) { _RibbonItemClick(iMenuItem); }
        /// <summary>
        /// Vyvolá reakce na kliknutí na prvek Ribbonu:
        /// event <see cref="RibbonItemClick"/>.
        /// </summary>
        /// <param name="iMenuItem"></param>
        private void _RibbonItemClick(IMenuItem iMenuItem)
        {
            var args = new TEventArgs<IMenuItem>(iMenuItem);
            OnRibbonItemClick(args);
            RibbonItemClick?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po kliknutí na prvek Ribbonu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRibbonItemClick(TEventArgs<IMenuItem> args) { }
        /// <summary>
        /// Událost volaná po kliknutí na prvek Ribbonu
        /// </summary>
        public event EventHandler<TEventArgs<IMenuItem>> RibbonItemClick;

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
        /// V rámci daného prvku se pokusí najít odpovídající definici prvku <see cref="IMenuItem"/> v jeho Tagu.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private bool _TryGetIRibbonItem(DevExpress.XtraBars.BarItem item, out IMenuItem menuItem)
        {
            menuItem = null;
            if (item == null) return false;
            if (item.Tag is IMenuItem iRibbonItem) { menuItem = iRibbonItem; return true; }


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
        /// V rámci daných stránek se pokusí najít odpovídající definici prvního prvku <see cref="IMenuItem"/> v tagu Item.
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="iMenuItem"></param>
        /// <returns></returns>
        private bool _TryGetIRibbonItem(IEnumerable<DevExpress.XtraBars.Ribbon.RibbonPage> pages, out IMenuItem iMenuItem)
        {
            iMenuItem = null;
            if (pages == null) return false;
            var found = pages.SelectMany(p => p.Groups)
                             .SelectMany(g => g.ItemLinks)
                             .Select(i => i.Item.Tag)
                             .OfType<IMenuItem>()
                             .FirstOrDefault();
            if (found == null) return false;
            iMenuItem = found;
            return true;
        }
        /// <summary>
        /// V rámci dodané kolekce linků na BarItem najde první, který ve svém Tagu nese <see cref="IMenuItem"/> (=definice prvku).
        /// </summary>
        /// <param name="barItemLinks"></param>
        /// <param name="iMenuItem"></param>
        /// <returns></returns>
        private bool _TryGetIRibbonItem(IEnumerable<DevExpress.XtraBars.BarItemLink> barItemLinks, out IMenuItem iMenuItem)
        {
            iMenuItem = null;
            if (barItemLinks == null) return false;
            foreach (DevExpress.XtraBars.BarItemLink link in barItemLinks)
            {
                if (link.Item.Tag is IMenuItem found)
                {
                    iMenuItem = found;
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

            DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: UnMerge + Action + Merge; Total Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
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
            DxComponent.LogAddLineTime($"ModifyRibbon {this.DebugName}: RunAction; Time: {DxComponent.LogTokenTimeMilisec}", startTime);

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
        /// Data definující stránku a její obsah
        /// </summary>
        public IRibbonPage PageData { get; set; }
        /// <summary>
        /// true pokud this stránka má nějaký aktivní LazyLoad obsah
        /// </summary>
        internal bool HasActiveLazyContent { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.HasActiveLazyContent); } }
        /// <summary>
        /// true pokud this stránka má aktivní LazyLoad obsah v režimu <see cref="RibbonContentMode.OnDemandLoadEveryTime"/>
        /// </summary>
        internal bool IsLazyLoadEveryTime { get { return (this.LazyLoadInfo != null && this.LazyLoadInfo.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime); } }
        /// <summary>
        /// Zajistí přípravu prvku LazyInfo pro režim definovaný v daném prvku. Prvek si uschová.
        /// Založí grupu pokud dosud neexistuje a je potřebná.
        /// Vrátí true, pokud se v dané situaci má generovat reálný obsah do this stránky Ribbonu, false pokud ne.
        /// </summary>
        /// <param name="pageData">Deklarace stránky</param>
        /// <param name="isLazyContentFill">Obsahuje true, když se prvky typu Group a BarItem NEMAJÍ fyzicky generovat (mají se jen registrovat do LazyGroup) / false pokud se mají reálně generovat (spotřebuje výrazný čas)</param>
        /// <param name="isOnDemandFill">Obsahuje true, pokud tyto položky jsou právě nyní donačtené OnDemand / false pokud pocházejí z první statické deklarace obsahu</param>
        internal bool PreparePageForContent(IRibbonPage pageData, bool isLazyContentFill, bool isOnDemandFill)
        {
            // Určíme, zda budeme potřebovat LazyInfo objekt (a tedy LazyGroup grupu v GUI Ribbonu):
            // a) podle režimu práce s obsahem
            // b) podle aktuálních příznaků
            var contentMode = pageData.PageContentMode;
            bool createLazyInfo = (isLazyContentFill ||
                                   contentMode == RibbonContentMode.OnDemandLoadEveryTime ||
                                   (contentMode == RibbonContentMode.OnDemandLoadOnce && !isOnDemandFill));

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
                lazyLoadInfo = new DxRibbonLazyLoadInfo(OwnerRibbon, this, pageData);
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
        /// <param name="isReFill">false při uživatelské aktivaci stránky, true při jejím naplnění daty z aplikačního kódu</param>
        public void PrepareRealLazyItems(bool isReFill)
        {
            if (this.HasActiveLazyContent)
                this.OwnerRibbon.PrepareRealLazyItems(this.LazyLoadInfo, isReFill);
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
        /// <summary>
        /// V dané kolekci Groups vyhledá grupy s ID = <see cref="LazyLoadGroupId"/>, 
        /// v nich vyhledá BarItemy, jejichž Tag obsahuje instanci <see cref="DxRibbonPage"/>, a kde tato stránka má aktivní LazyContent.
        /// Tyto nalezené stránky přidává do listu <paramref name="lazyDxPages"/>.
        /// Metoda průběžně odebírá zmíněné Linky na Buttony, a odebírá i prázdné grupy.
        /// </summary>
        /// <param name="pageGroups"></param>
        /// <param name="isReFill"></param>
        /// <param name="lazyDxPages"></param>
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
            this.PageData = null;
            this.Group = null;
            this.BarItem = null;
        }
        /// <summary>
        /// true pokud this prvek má nějaký aktivní LazyLoad obsah
        /// </summary>
        public bool HasActiveLazyContent { get { return (this.IsActive && this.HasData); } }
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
    #region Třídy definující Ribbon : defaultní implementace odpovídajících interface
    /// <summary>
    /// Definice stránky v Ribbonu
    /// </summary>
    public class DataRibbonPage : IRibbonPage
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonPage()
        {
            this.Groups = new List<IRibbonGroup>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Page: {this.PageText}; Groups: {Groups.Count}";
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
        /// Metoda vrátí všechny prvky z dané stránky, které nesou příznak <see cref="IMenuItem.ItemToolbarOrder"/> s ne null hodnotou.
        /// prochází pole prvků v grupách i pole SubItems z prvků (ale jen první úroveň).
        /// </summary>
        /// <param name="pageData"></param>
        internal static IMenuItem[] GetAllToolbarItems(IRibbonPage pageData)
        {
            if (pageData?.Groups == null) return null;

            List<IMenuItem> toolItems = new List<IMenuItem>();

            // Prvky v grupách - všechny, lineárně:
            var items = pageData.Groups
                .Where(g => g.Items != null)
                .SelectMany(g => g.Items)
                .Where(i => i != null)
                .ToArray();
            toolItems.AddRange(items.Where(i => i.ItemToolbarOrder.HasValue));

            // Plus SubItems:
            toolItems.AddRange(items
                .Where(i => i.SubItems != null)
                .SelectMany(i => i.SubItems)
                .Where(i => i.ItemToolbarOrder.HasValue));

            return toolItems.ToArray();
        }
        /// <summary>
        /// Kategorie, do které patří tato stránka. Může být null pro běžné stránky Ribbonu.
        /// </summary>
        public IRibbonCategory Category { get; set; }
        /// <summary>
        /// ID grupy, musí být jednoznačné v rámci Ribbonu
        /// </summary>
        public string PageId { get; set; }
        /// <summary>
        /// Pořadí stránky, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public int PageOrder { get; set; }
        /// <summary>
        /// Jméno stránky
        /// </summary>
        public string PageText { get; set; }
        /// <summary>
        /// Typ stránky
        /// </summary>
        public RibbonPageType PageType { get; set; }
        /// <summary>
        /// Režim práce se stránkou (opožděné načítání, refresh před každým načítáním)
        /// </summary>
        public RibbonContentMode PageContentMode { get; set; }
        /// <summary>
        /// Pole skupin v této stránce
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        public List<IRibbonGroup> Groups { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IRibbonGroup> IRibbonPage.Groups { get { return this.Groups; } }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public object Tag { get; set; }
    }
    /// <summary>
    /// Definice kategorie, do které patří stránka v Ribbonu
    /// </summary>
    public class DataRibbonCategory : IRibbonCategory
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonCategory()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Category: {this.CategoryText}";
        }
        /// <summary>
        /// ID kategorie, jednoznačné per Ribbon
        /// </summary>
        public string CategoryId { get; set; }
        /// <summary>
        /// Titulek kategorie zobrazovaný uživateli
        /// </summary>
        public string CategoryText { get; set; }
        /// <summary>
        /// Barva kategorie
        /// </summary>
        public Color CategoryColor { get; set; }
        /// <summary>
        /// Kategorie je viditelná?
        /// </summary>
        public bool CategoryVisible { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public object Tag { get; set; }
    }
    /// <summary>
    /// Definice skupiny ve stránce Ribbonu
    /// </summary>
    public class DataRibbonGroup : IRibbonGroup
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataRibbonGroup()
        {
            this.Items = new List<IMenuItem>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Page: {this.GroupText}; Items: {Items.Count}";
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
        public string GroupId { get; set; }
        /// <summary>
        /// Pořadí grupy, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public int GroupOrder { get; set; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        public string GroupText { get; set; }
        /// <summary>
        /// Obrázek grupy
        /// </summary>
        public string GroupImage { get; set; }
        /// <summary>
        /// Zobrazit speciální tlačítko grupy vpravo dole v titulku grupy (lze tak otevřít nějaké okno vlastností pro celou grupu)
        /// </summary>
        public bool GroupButtonVisible { get; set; }
        /// <summary>
        /// Soupis prvků grupy (tlačítka, menu, checkboxy, galerie)
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        public List<IMenuItem> Items { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IMenuItem> IRibbonGroup.Items { get { return this.Items; } }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public object Tag { get; set; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public class DataMenuItem : IMenuItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataMenuItem()
        {
            this.ItemEnabled = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Item: {this.ItemText}; Type: {this.ItemType}" + (this.SubItems != null ? $"; SubItems: {this.SubItems.Count}" : "");
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
        /// Parent prvku = <see cref="IRibbonGroup"/>
        /// </summary>
        public IRibbonGroup ParentGroup { get; set; }
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IMenuItem"/>
        /// </summary>
        public IMenuItem ParentItem { get; set; }
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        public string ItemId { get; set; }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public int ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje tre tehdy, když před prvkem má být oddělovač
        /// </summary>
        public bool ItemIsFirstInGroup { get; set; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public RibbonItemType ItemType { get; set; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        public RibbonItemStyles RibbonStyle { get; set; }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        public bool ItemEnabled { get; set; }
        /// <summary>
        /// Pořadí prvku v ToolBaru = QuickAccesToolbar
        /// </summary>
        public int? ItemToolbarOrder { get; set; }
        /// <summary>
        /// Jméno ikony.
        /// Pro prvek typu <see cref="RibbonItemType.CheckBoxToggle"/> tato ikona reprezentuje stav, kdy <see cref="ItemIsChecked"/> = NULL.
        /// </summary>
        public string ItemImage { get; set; }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        public string ItemImageUnChecked { get; set; }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        public string ItemImageChecked { get; set; }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Ribbonu (uživatelem) je do této property setována aktuální hodnota z Ribbonu 
        /// a poté je vyvolána událost <see cref="DxRibbonControl.RibbonItemClick"/>.
        /// Hodnota může být null, pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat)
        /// </summary>
        public bool? ItemIsChecked { get; set; }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        public BarItemPaintStyle ItemPaintStyle { get; set; }
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        public string ItemText { get; set; }
        /// <summary>
        /// Klávesa
        /// </summary>
        public string HotKey { get; set; }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public string ToolTip { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se <see cref="ItemText"/>.
        /// </summary>
        public string ToolTipTitle { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public string ToolTipIcon { get; set; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        public RibbonContentMode SubItemsContentMode { get; set; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu.
        /// Výchozí hodnota je null.
        /// </summary>
        public List<IMenuItem> SubItems { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IMenuItem> IMenuItem.SubItems { get { return this.SubItems; } }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public object Tag { get; set; }
    }
    #endregion
    #region Interface IRibbonPage, IRibbonCategory, IRibbonGroup, IMenuItem;  Enumy RibbonPageType, RibbonContentMode, RibbonItemStyles, BarItemPaintStyle, RibbonItemType.
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
        IEnumerable<IMenuItem> Items { get; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public interface IMenuItem
    {
        /// <summary>
        /// Parent prvku = <see cref="IRibbonGroup"/>
        /// </summary>
        IRibbonGroup ParentGroup { get; set; }
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IMenuItem"/>
        /// </summary>
        IMenuItem ParentItem { get; set; }
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        string ItemId { get; }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje tre tehdy, když před prvkem má být oddělovač
        /// </summary>
        bool ItemIsFirstInGroup { get; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        RibbonItemType ItemType { get; }
        /// <summary>
        /// Styl zobrazení prvku
        /// </summary>
        RibbonItemStyles RibbonStyle { get; }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        bool ItemEnabled { get; }
        /// <summary>
        /// Pořadí prvku v ToolBaru = QuickAccesToolbar
        /// </summary>
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
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        BarItemPaintStyle ItemPaintStyle { get; }
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        string ItemText { get; }
        /// <summary>
        /// Klávesa
        /// </summary>
        string HotKey { get; }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        string ToolTip { get; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se <see cref="ItemText"/>.
        /// </summary>
        string ToolTipTitle { get; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        string ToolTipIcon { get; }
        /// <summary>
        /// Režim práce se subpoložkami
        /// </summary>
        RibbonContentMode SubItemsContentMode { get; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu
        /// </summary>
        IEnumerable<IMenuItem> SubItems { get; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
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
        InRibbonGallery,
        SkinSetDropDown,
        SkinPaletteDropDown,
        SkinPaletteGallery

    }
    #endregion
}
