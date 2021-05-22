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
            return this.GetType().Name + ": " + (DebugName ?? "NULL");
        }
        /// <summary>
        /// Jméno Ribbonu pro debugování
        /// </summary>
        public string DebugName { get; set; }
        #endregion
        #region LazyLoad page content
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
            this.CheckLazyContentItems(this.SelectedPage);
            base.OnSelectedPageChanged(prev);
        }
        /// <summary>
        /// Prověří, zda aktuální stránka má nějaké HasLazyContentItems, a pokud ano tak je fyzicky vytvoří
        /// </summary>
        protected virtual void CheckLazyLoadCurrentPageContent()
        {
            this.CheckLazyContentItems(this.SelectedPage);
        }
        /// <summary>
        /// Prověří, zda daná stránka má nějaké HasLazyContentItems, a pokud ano tak je fyzicky vytvoří
        /// </summary>
        /// <param name="page"></param>
        protected virtual void CheckLazyContentItems(DevExpress.XtraBars.Ribbon.RibbonPage page)
        {
            // Něco k DevExpress a k Ribbonům si přečti v komentáři k metodě DxRibbonPageLazyGroupInfo.TryGetLazyContentForPage!

            if (!DxRibbonPageLazyGroupInfo.TryGetLazyContentForPage(page, out var lazyGroups)) return;

            foreach (var lazyGroup in lazyGroups)
                lazyGroup.CreateRealItems(this, page);

           

        //    if (!dxRibbonPage.HasLazyContentItems) return;
        //    List<IRibbonItem> list = null;
        //    lock (this)
        //    {
        //        list = dxRibbonPage.LazyContentItems.ToList();
        //        dxRibbonPage.LazyContentItems.Clear();
        //    }
        //    if (list.Count == 0) return;

        //    var startTime = DxComponent.LogTimeCurrent;
        //    this.SuspendLayout();
        //    this.Manager.BeginUpdate();
        //    int count = 0;
        //    foreach (var item in list)
        //        _AddItem(item, false, ref count);
        //    this.Manager.EndUpdate();
        //    this.ResumeLayout(false);
        //    this.PerformLayout();
        //    DxComponent.LogAddLineTime($" === RIBBON Page '{dxRibbonPage.Text}': LazyFill {list.Count} item[s]; Create: {count} BarItem[s] {DxComponent.LogTokenTimeMilisec} === ", startTime);
        

        }


        public override void MergeRibbon(DevExpress.XtraBars.Ribbon.RibbonControl ribbon)
        {
            var startTime = DxComponent.LogTimeCurrent;

            string slaveSelectedPage = (ribbon is DxRibbonControl dxRibbon) ? dxRibbon.SelectedPageId : null;

            int mainPageCount1 = this.Pages.Count;
            int mainMergesPageCount1 = this.MergedPages.Count;
            int slavePageCount1 = ribbon?.Pages.Count ?? -1;
            int slaveMergesPageCount1 = ribbon?.MergedPages.Count ?? -1;

            base.MergeRibbon(ribbon);

            int mainPageCount2 = this.Pages.Count;
            int mainMergesPageCount2 = this.MergedPages.Count;
            int slavePageCount2 = ribbon?.Pages.Count ?? -1;
            int slaveMergesPageCount2 = ribbon?.MergedPages.Count ?? -1;

            if (slaveSelectedPage != null) this.SelectedPageId = slaveSelectedPage;

            DxComponent.LogAddLineTime($"MergeRibbon: Master: {this.DebugName}; Slave: {(ribbon?.ToString())}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        public override void UnMergeRibbon()
        {
            var startTime = DxComponent.LogTimeCurrent;

            base.UnMergeRibbon();

            DxComponent.LogAddLineTime($"UnMergeRibbon: Master: {this.DebugName}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        protected override void RaiseMerge(DevExpress.XtraBars.Ribbon.RibbonMergeEventArgs e)
        {
            base.RaiseMerge(e);
        }
        protected override void RaiseUnMerge(DevExpress.XtraBars.Ribbon.RibbonMergeEventArgs e)
        {
            base.RaiseUnMerge(e);
        }
        #endregion
        #region Tvorba obsahu Ribbonu
        /// <summary>
        /// Smaže celý obsah Ribbonu. Ribbon se zmenší na řádek pro záhlaví a celé okno pod ním se přeuspořádá.
        /// Důvodem je smazání všech stránek.
        /// Jde o poměrně nehezký efekt.
        /// </summary>
        public void Clear()
        {
            var startTime = DxComponent.LogTimeCurrent;
            this.Pages.Clear();
            this.Categories.Clear();
            this.PageCategories.Clear();
            this.Items.Clear();
            DxComponent.LogAddLineTime($" === RIBBON Clear: {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Smaže výhradně jednotlivé prvky z Ribbonu (Items a LazyLoadContent) a grupy prvků (Page.Groups).
        /// Ponechává naživu Pages, Categories a PageCategories.
        /// </summary>
        public void Empty()
        {
            var startTime = DxComponent.LogTimeCurrent;
            this.Items.Clear();
            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.Pages)
            {
                page.Groups.Clear();
                if (page is DxRibbonPage dxRibbonPage)
                {
                    dxRibbonPage.Empty();
                }
            }
            DxComponent.LogAddLineTime($" === RIBBON Empty: {DxComponent.LogTokenTimeMilisec} === ", startTime);
        }
        /// <summary>
        /// Smaže pouze prvky z Ribbonu.
        /// </summary>
        public void Final()
        { }
        /// <summary>
        /// Přidá dodané prvky do this ribbonu, zakládá stránky, kategorie, grupy...
        /// Pokud má být aktivní <see cref="UseLazyContentCreate"/>, musí být nastaveno na true před přidáním prvku.
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(IEnumerable<IRibbonItem> items)
        {
            _AddItems(items, this.UseLazyContentCreate, "Fill");
            CheckLazyLoadCurrentPageContent();
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané LazyGroup do this Ribbonu. Zde se prvky přidávají vždy jako reálné, už ne Lazy.
        /// </summary>
        /// <param name="lazyGroup"></param>
        internal void AddItems(DxRibbonPageLazyGroupInfo lazyGroup)
        {
            _AddItems(lazyGroup.Items, false, "LazyFill");
        }
        /// <summary>
        /// Přidá prvky do this Ribbonu z dodané kolekce, v daném režimu LazyLoad
        /// </summary>
        /// <param name="items"></param>
        /// <param name="useLazyContentCreate"></param>
        /// <param name="logText"></param>
        private void _AddItems(IEnumerable<IRibbonItem> items, bool useLazyContentCreate, string logText)
        {
            if (items is null) return;
            var startTime = DxComponent.LogTimeCurrent;
            List<IRibbonItem> list = items.Where(i => i != null).ToList();
            SortItems(list);
            int count = 0;
            foreach (var item in list)
                _AddItem(item, useLazyContentCreate, ref count);
            DxComponent.LogAddLineTime($" === RIBBON: {logText} {list.Count} item[s]; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);
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
            _AddItem(item, this.UseLazyContentCreate, ref count);
            DxComponent.LogAddLineTime($" === RIBBON: Add 1 item; Create: {count} BarItem[s]; {DxComponent.LogTokenTimeMilisec} === ", startTime);

            CheckLazyLoadCurrentPageContent();
        }
        /// <summary>
        /// Zajistí správné setřídění prvků v poli
        /// </summary>
        /// <param name="list"></param>
        private void SortItems(List<IRibbonItem> list)
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
            if (list.Count > 1) list.Sort((a, b) => CompareByOrder(a, b));
        }
        /// <summary>
        /// ID aktuálně vybrané stránky = <see cref="DevExpress.XtraBars.Ribbon.RibbonPage.Name"/>.
        /// Lze setovat, dojde k aktivaci dané stránky (pokud je nalezena).
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
        public List<DevExpress.XtraBars.Ribbon.RibbonPage> AllPages
        {
            get
            {
                List<DevExpress.XtraBars.Ribbon.RibbonPage> pages = new List<DevExpress.XtraBars.Ribbon.RibbonPage>();
                pages.AddRange(this.Pages);
                foreach (DevExpress.XtraBars.Ribbon.RibbonPageCategory category in this.Categories)
                    pages.AddRange(category.Pages);
                pages.AddRange(this.MergedPages);
                foreach (DevExpress.XtraBars.Ribbon.RibbonPageCategory category in this.MergedCategories)
                    pages.AddRange(category.Pages);
                return pages;
            }
        }
        private void _AddItem(IRibbonItem item, bool useLazyContentCreate, ref int count)
        {
            if (item is null) return;
            var category = GetCategory(item);
            var page = GetPage(item, category);
            if (!useLazyContentCreate)
            {
                var group = GetGroup(item, page);
                var button = GetBarItem(item, group, ref count);
            }
            else
            {
                page.AddLazyContentItem(item);
            }
        }
        /// <summary>
        /// Komparátor pro třídění: <see cref="IRibbonItem.PageOrder"/>, <see cref="IRibbonItem.GroupOrder"/>, <see cref="IMenuItem.ItemOrder"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static int CompareByOrder(IRibbonItem a, IRibbonItem b)
        {
            int cmp = a.PageOrder.CompareTo(b.PageOrder);
            if (cmp == 0) cmp = a.GroupOrder.CompareTo(b.GroupOrder);
            if (cmp == 0) cmp = a.ItemOrder.CompareTo(b.ItemOrder);
            return cmp;
        }
        protected DevExpress.XtraBars.Ribbon.RibbonPageCategory GetCategory(IRibbonItem item, bool enableNew = true)
        {
            if (item is null) return null;
            if (String.IsNullOrEmpty(item.CategoryId)) return null;
            DevExpress.XtraBars.Ribbon.RibbonPageCategory category = PageCategories.GetCategoryByName(item.CategoryId);
            if (category is null && enableNew)
            {
                category = new DevExpress.XtraBars.Ribbon.RibbonPageCategory(item.CategoryText, item.CategoryColor, item.CategoryVisible);
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
                    CaptionButtonVisible = DefaultBoolean.False
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
            if (!(e.Item.Tag is IMenuItem ribbonData)) return;
            if (e.Item is DevExpress.XtraBars.BarCheckItem checkItem)
                ribbonData.ItemIsChecked = checkItem.Checked;

            _RibbonItemClick(ribbonData);
        }
        private void RibbonControl_PageCategoryClick(object sender, DevExpress.XtraBars.Ribbon.PageCategoryClickEventArgs e)
        {
        }
        private void RibbonControl_PageGroupCaptionButtonClick(object sender, DevExpress.XtraBars.Ribbon.RibbonPageGroupEventArgs e)
        {
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
    #region DxRibbonPage : stránka Ribbonu s podporou LazyContentItems
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
            LazyGroupInfo = null;
        }
        /// <summary>
        /// Přidá do this stránky další prvek do seznamu LazyContent
        /// </summary>
        /// <param name="item"></param>
        public void AddLazyContentItem(IRibbonItem item)
        {
            var lazyGroupInfo = GetLazyGroupInfo();
            lazyGroupInfo.Items.Add(item);
        }
        /// <summary>
        /// Odstraní z this stránky celou LazyGroup - data i reálnou grupu
        /// </summary>
        public void RemoveLazyGroup()
        {
            var lazyGroupInfo = LazyGroupInfo;
            if (lazyGroupInfo != null)
            {
                this.Groups.Remove(lazyGroupInfo.Group);
                lazyGroupInfo.Clear();
                LazyGroupInfo = null;
            }
        }
        /// <summary>
        /// Vytvoří (pokud je třeba) a vrátí instanci <see cref="DxRibbonPageLazyGroupInfo"/> pro ukládání informací pro LazyContent
        /// </summary>
        /// <returns></returns>
        protected DxRibbonPageLazyGroupInfo GetLazyGroupInfo()
        {
            DxRibbonPageLazyGroupInfo lazyGroupInfo = LazyGroupInfo;
            if (lazyGroupInfo == null)
            {
                lazyGroupInfo = new DxRibbonPageLazyGroupInfo(OwnerRibbon, this);
                this.Groups.Add(lazyGroupInfo.Group);
                LazyGroupInfo = lazyGroupInfo;
            }
            return lazyGroupInfo;
        }
        /// <summary>
        /// Definice dat pro LazyLoad content pro tuto Page. Obsahuje deklarace prvků i referenci na grupu, která LazyLoad zajistí.
        /// </summary>
        protected DxRibbonPageLazyGroupInfo LazyGroupInfo { get; private set; }
        /// <summary>
        /// Smaže obsah <see cref="LazyGroupInfo"/>.
        /// </summary>
        public void Empty()
        {
            if (LazyGroupInfo == null) return;
            this.Groups.Remove(LazyGroupInfo.Group);
            LazyGroupInfo.Clear();
            LazyGroupInfo = null;
        }
    }
    /// <summary>
    /// Třída, která obsahuje data o LazyContent prvcích jedné stránky Ribbonu
    /// </summary>
    public class DxRibbonPageLazyGroupInfo
    {
        /// <summary>
        /// Konstuktor
        /// </summary>
        /// <param name="ownerRibbon"></param>
        /// <param name="ownerPage"></param>
        public DxRibbonPageLazyGroupInfo(DxRibbonControl ownerRibbon, DxRibbonPage ownerPage)
        {
            this.OwnerRibbon = ownerRibbon;
            this.OwnerPage = ownerPage;
            this.Group = new DevExpress.XtraBars.Ribbon.RibbonPageGroup("INVISIBLE")
            {
                Name = this.OwnerPage.Name + "___LazyLoadGroup",
                // Visible = false,
                CaptionButtonVisible = DefaultBoolean.False,
                Tag = this
            };
            this.Items = new List<IRibbonItem>();
        }
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
        /// 6. Aby fungovalo generování BarItems "OnDemand", máme někde (!) uložené podklady pro tvorbu BarItems (někde = v instanci <see cref="DxRibbonPageLazyGroupInfo"/>)
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
        /// 1. Podklady pro tvorbu BarItems (typ <see cref="IRibbonItem"/>) ukládám do instance <see cref="DxRibbonPageLazyGroupInfo"/> (spolu s dalšími referencemi);
        /// 2. Primární stránka je typu <see cref="DxRibbonPage"/> a tyto podklady má ve své property <see cref="DxRibbonPage.LazyGroupInfo"/>;
        /// 3. Tyto podklady "uložím" navíc do speciální Grupy v primárním Ribbonu v odpovídající stránce, do Group.Tag
        /// 4. Při aktivaci stránky Ribbonu jsou dvě možnosti:
        ///  a) Jde o stránku primárního Ribbonu, tedy jde o stránku typu <see cref="DxRibbonPage"/>: 
        ///     - pak do ní vygenerují BarItemy vcelku jednoduše
        ///  b) Jde o stránku jiného Ribbonu, kde jsou mergována data z jiných Ribbonů, a je jedno zda jde o primární stránku jiného (Parent) ribbonu,
        ///      anebo jde o stránky MergedPages v Parent Ribbonu: 
        ///     - pak musím vygenerovat BarItemy do jejich primární stránky a tu pak znovu mergovat (netestováno ???)
        /// </summary>
        /// <param name="page"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        internal static bool TryGetLazyContentForPage(DevExpress.XtraBars.Ribbon.RibbonPage page, out List<DxRibbonPageLazyGroupInfo> items)
        {
            items = null;
            if (page == null) return false;
            var groups = page.Groups.Where(g => g.Tag is DxRibbonPageLazyGroupInfo).ToArray();
            items = new List<DxRibbonPageLazyGroupInfo>();
            foreach (var group in groups)
            {
                DxRibbonPageLazyGroupInfo lazyGroup = group.Tag as DxRibbonPageLazyGroupInfo;
                if (lazyGroup.IsEmpty)
                    page.Groups.Remove(group);
                else
                    items.Add(lazyGroup);
            }
            return (items.Count > 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentRibbon"></param>
        /// <param name="currentPage"></param>
        internal void CreateRealItems(DxRibbonControl currentRibbon, DevExpress.XtraBars.Ribbon.RibbonPage currentPage)
        {
            this.OwnerRibbon.AddItems(this);
            if (!Object.ReferenceEquals(this.OwnerRibbon, currentRibbon))
                this.MergeOwnerTo(currentRibbon);          // Pokud aktuálně pracujeme s jiným Ribbonem, než je náš vlastní, pak do nového zkusíme náš Ribbon znovu mergovat!
            this.OwnerPage?.RemoveLazyGroup();
        }
        protected void MergeOwnerTo(DxRibbonControl currentRibbon)
        {
            currentRibbon.UnMergeRibbon();
            currentRibbon.MergeRibbon(this.OwnerRibbon);
        }
        /// <summary>
        /// Zahodí všechny svoje reference
        /// </summary>
        public void Clear()
        {
            this.OwnerRibbon = null;
            this.OwnerPage = null;
            this.Group = null;
            this.Items.Clear();
            this.Items = null;
        }
        /// <summary>
        /// Obsahuje true, pokud this instance už je prázdná = její Items byly už použity pro tvorbu BarItems a z instance byly odebrány reference na Ribbon i Page i Group.
        /// Taková instance může někde viset uložená v Tagu některé Mergované grupy.
        /// </summary>
        public bool IsEmpty { get { return (OwnerRibbon == null || OwnerPage == null || Items == null || Items.Count == 0); } }
        public DxRibbonControl OwnerRibbon { get; private set; }
        public DxRibbonPage OwnerPage { get; private set; }
        public DevExpress.XtraBars.Ribbon.RibbonPageGroup Group { get; private set; }
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

            /*

 string resource5 = "svgimages/icon%20builder/actions_question.svg";
            string resource3 = "svgimages/icon%20builder/actions_checkcircled.svg";
            string resource4 = "svgimages/icon%20builder/actions_removecircled.svg";
            string resource6 = "svgimages/icon%20builder/actions_deletecircled.svg";
            string resource1 = "images/xaf/templatesv2images/bo_unknown.svg";
            string resource2 = "images/xaf/templatesv2images/bo_unknown_disabled.svg";


            string resource1 = "svgimages/outlook%20inspired/needassistance.svg";

            string resource2 = "svgimages/icon%20builder/security_warningcircled1.svg";


        string resource1 = "svgimages/xaf/state_validation_information.svg";
        string resource2 = "svgimages/xaf/state_validation_invalid.svg";
        string resource3 = "svgimages/xaf/state_validation_skipped.svg";
        string resource4 = "svgimages/xaf/state_validation_valid.svg";

            */

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
        public int GroupOrder { get; set; }
        public string GroupId { get; set; }
        public string GroupText { get; set; }
        public string GroupImage { get; set; }
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
        int GroupOrder { get; set; }
        string GroupId { get; }
        string GroupText { get; }
        string GroupImage { get; }
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
        /// Uživatel kliknul na prvek menu (je v parametru).
        /// </summary>
        /// <param name="menuItem"></param>
        void MenuItemAction(IMenuItem menuItem);
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
