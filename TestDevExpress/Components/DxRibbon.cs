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
    #region DxRibbon
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

            ApplicationButtonClick += RibbonControl_ApplicationButtonClick;
            ItemClick += RibbonControl_ItemClick;
            PageCategoryClick += RibbonControl_PageCategoryClick;
            PageGroupCaptionButtonClick += RibbonControl_PageGroupCaptionButtonClick;

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
        #endregion
        #region Tvorba obsahu Ribbonu
        public void Clear()
        {
            this.Pages.Clear();
            this.Categories.Clear();
            this.PageCategories.Clear();
            this.Items.Clear();
        }
        public void AddItems(IEnumerable<IRibbonItem> items)
        {
            if (items is null) return;
            List<IRibbonItem> list = items.Where(i => i != null).ToList();
            if (list.Count > 1) list.Sort((a, b) => CompareByOrder(a, b));
            foreach (var item in list)
                _AddItem(item);
        }
        public void AddItem(IRibbonItem item)
        {
            _AddItem(item);
        }
        public string SelectedPageId
        {
            get { return this.SelectedPage?.Name; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    var page = Pages.FirstOrDefault(p => p.Name == value);
                    if (page != null) this.SelectedPage = page;
                }
            }
        }
        private void _AddItem(IRibbonItem item)
        {
            if (item is null) return;
            var category = GetCategory(item);
            var page = GetPage(item, category);
            var group = GetGroup(item, page);
            var button = GetBarItem(item, group);
        }
        /// <summary>
        /// Komparátor pro třídění: <see cref="IRibbonItem.PageOrder"/>, <see cref="IRibbonItem.GroupOrder"/>, <see cref="IRibbonItem.ItemOrder"/> ASC
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
        protected DevExpress.XtraBars.Ribbon.RibbonPage GetPage(IRibbonItem item, DevExpress.XtraBars.Ribbon.RibbonPageCategory category = null, bool enableNew = true)
        {
            if (item is null) return null;
            bool isCategory = !(category is null);
            DevExpress.XtraBars.Ribbon.RibbonPage page;
            if (item.PageIsHome && !isCategory)            // Pokud je předána kategorie, pak jde o kontextovou stránku a ta nesmí být "Home"!
            {
                page = Pages.GetPageByText(item.PageText);
                if (page != null)
                {
                    Name = item.PageId;
                }
            }
            page = isCategory ? category.Pages.FirstOrDefault(r => (r.Name == item.PageId)) : Pages.FirstOrDefault(r => r.Name == item.PageId);
            if (page is null && enableNew)
            {
                page = new DevExpress.XtraBars.Ribbon.RibbonPage(item.PageText)
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
        protected DevExpress.XtraBars.Ribbon.RibbonPageGroup GetGroup(IRibbonItem item, DevExpress.XtraBars.Ribbon.RibbonPage page, bool enableNew = true)
        {
            if (item is null || page is null) return null;
            DevExpress.XtraBars.Ribbon.RibbonPageGroup group = page.Groups.GetGroupByName(item.GroupId);
            if (group is null && enableNew)
            {
                group = new DevExpress.XtraBars.Ribbon.RibbonPageGroup(item.GroupText)
                {
                    Name = item.GroupId,
                    ShowCaptionButton = false
                };
                group.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.GroupImage, ImagesSize, item.GroupText);
                page.Groups.Add(group);
            }
            return group;
        }

        protected DevExpress.XtraBars.BarItem GetBarItem(IMenuItem item, DevExpress.XtraBars.Ribbon.RibbonPageGroup group, bool enableNew = true)
        {
            if (item is null || group is null) return null;
            DevExpress.XtraBars.BarItem barItem = Items[item.ItemId];
            if (barItem is null)
            {
                if (!enableNew) return null;
                barItem = CreateBarItem(item);
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
        protected DevExpress.XtraBars.BarItem CreateBarItem(IMenuItem item)
        {
            DevExpress.XtraBars.BarItem barItem = null;
            switch (item.ItemType)
            {
                case RibbonItemType.ButtonGroup:
                    DevExpress.XtraBars.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(item.SubItems));
                    buttonGroup.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    DevExpress.XtraBars.BarButtonItem splitButton = Items.CreateSplitButton(item.ItemText, GetPopupSubItems(item.SubItems));
                    barItem = splitButton;
                    break;
                case RibbonItemType.CheckBoxStandard:
                case RibbonItemType.CheckBoxSlider:
                    DevExpress.XtraBars.BarCheckItem checkItem = Items.CreateCheckItem(item.ItemText, item.ItemIsChecked);
                    barItem = checkItem;
                    break;
                case RibbonItemType.RadioItem:
                    DevExpress.XtraBars.BarCheckItem radioItem = Items.CreateCheckItem(item.ItemText, item.ItemIsChecked);
                    barItem = radioItem;
                    break;
                case RibbonItemType.Menu:
                    DevExpress.XtraBars.BarSubItem menu = Items.CreateMenu(item.ItemText);
                    var menuItems = GetBarSubItems(item.SubItems);
                    foreach (var menuItem in menuItems)
                    {
                        var menuLink = menu.AddItem(menuItem);
                        if ((menuItem.Tag is IMenuItem ribbonData) && ribbonData.ItemIsFirstInGroup)
                            menuLink.BeginGroup = true;
                    }
                    barItem = menu;
                    break;
                case RibbonItemType.Button:
                default:
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
        protected DevExpress.XtraBars.BarItem[] GetBarSubItems(IMenuItem[] items)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (items != null)
            {
                foreach (IMenuItem item in items)
                {
                    DevExpress.XtraBars.BarItem barItem = CreateBarItem(item);
                    if (barItem != null)
                        barItems.Add(barItem);
                }
            }
            return barItems.ToArray();
        }
        protected DevExpress.XtraBars.BarBaseButtonItem[] GetBarBaseButtons(IMenuItem[] items)
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
        protected DevExpress.XtraBars.PopupMenu GetPopupSubItems(IMenuItem[] items)
        {
            DevExpress.XtraBars.PopupMenu dxPopup = new DevExpress.XtraBars.PopupMenu(BarManager);
            if (items != null)
            {
                foreach (IMenuItem item in items)
                {
                    DevExpress.XtraBars.BarItem barItem = CreateBarItem(item);
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
            barItem.Caption = item.ItemText;
            barItem.Enabled = item.ItemEnabled;
            barItem.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage, ImagesSize, item.ItemText);
            barItem.LargeImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage, LargeImagesSize, item.ItemText);
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
                checkItem.CheckStyle = (item.ItemType == RibbonItemType.RadioItem ? DevExpress.XtraBars.BarCheckStyles.Radio : DevExpress.XtraBars.BarCheckStyles.Standard);
                checkItem.Checked = item.ItemIsChecked;
            }

            barItem.PaintStyle = item.ItemPaintStyle;
            if (item.RibbonStyle != DevExpress.XtraBars.Ribbon.RibbonItemStyles.Default)
                barItem.RibbonStyle = item.RibbonStyle;

            if (item.ToolTip != null)
                barItem.SuperTip = GetSuperTip(item.ToolTip, item.ToolTipTitle, item.ToolTipIcon);

            barItem.Tag = item;
        }
        protected DevExpress.Utils.SuperToolTip GetSuperTip(string text, string title, string image)
        {
            if (text is null) return null;
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
            //if (image != null)
            //{
            //    dxText.ImageOptions.Images = ComponentConnector.GraphicsCache.GetImageList(UserGraphicsSize.Large);
            //    dxText.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(image, UserGraphicsSize.Large);
            //}
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
            if (!(e.Item.Tag is IMenuItem ribbonData)) return;
            _RibbonItemClick(ribbonData);
        }
        private void RibbonControl_PageCategoryClick(object sender, DevExpress.XtraBars.Ribbon.PageCategoryClickEventArgs e)
        {
        }
        private void RibbonControl_PageGroupCaptionButtonClick(object sender, DevExpress.XtraBars.Ribbon.RibbonPageGroupEventArgs e)
        {
        }

        private void _RibbonItemClick(IMenuItem item)
        {
            OnRibbonItemClick(item);
            RibbonItemClick?.Invoke(this, new TEventArgs<IMenuItem>(item));
        }
        protected virtual void OnRibbonItemClick(IMenuItem item) { }
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
            ItemPaintStyle = DevExpress.XtraBars.BarItemPaintStyle.Standard;
            RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
        }
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
        public bool ItemIsFirstInGroup { get; set; }
        public DevExpress.XtraBars.Ribbon.RibbonItemStyles RibbonStyle { get; set; }
        public bool ItemEnabled { get; set; }
        public int? ItemToolbarOrder { get; set; }
        public bool ItemIsChecked { get; set; }
        public DevExpress.XtraBars.BarItemPaintStyle ItemPaintStyle { get; set; }
        public string HotKey { get; set; }
        public string ToolTip { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipIcon { get; set; }
        public IMenuItem[] SubItems { get; set; }
        public object Tag { get; set; }
    }
    #endregion
    #region Interface IRibbonItem a IRibbonData;  Enumy RibbonItemType a RibbonPageType

    public interface IRibbonItem : IMenuItem
    {
        string CategoryId { get; }
        string CategoryText { get; }
        Color CategoryColor { get; }
        bool CategoryVisible { get; }
        bool PageIsHome { get; }
        int PageOrder { get; }
        string PageId { get; }
        string PageText { get; }
        Color PageColor { get; }
        RibbonPageType PageType { get; }
        int GroupOrder { get; }
        string GroupId { get; }
        string GroupText { get; }
        string GroupImage { get; }
    }
    public interface IMenuItem
    {
        int ItemOrder { get; }
        string ItemId { get; }
        bool ItemIsFirstInGroup { get; }
        RibbonItemType ItemType { get; }
        DevExpress.XtraBars.Ribbon.RibbonItemStyles RibbonStyle { get; }
        bool ItemEnabled { get; }
        int? ItemToolbarOrder { get; }
        string ItemImage { get; }
        bool ItemIsChecked { get; }
        DevExpress.XtraBars.BarItemPaintStyle ItemPaintStyle { get; }
        string ItemText { get; }
        string HotKey { get; }
        string ToolTip { get; }
        string ToolTipTitle { get; }
        string ToolTipIcon { get; }
        IMenuItem[] SubItems { get; }
        object Tag { get; set; }
    }

    public enum RibbonItemType
    {
        None,
        Button,
        ButtonGroup,
        SplitButton,
        CheckBoxStandard,
        CheckBoxSlider,
        RadioItem,
        Menu
    }
    public enum RibbonPageType
    {
        Default,
        Contextual
    }
    #endregion
}
