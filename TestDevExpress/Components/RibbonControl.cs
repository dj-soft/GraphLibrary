using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

using WinFormServices.Drawing;

using XB = DevExpress.XtraBars;
using XR = DevExpress.XtraBars.Ribbon;


namespace TestDevExpress
{
    /// <summary>
    /// RibbonControl + Freeze
    /// </summary>
    public class RibbonControl : XR.RibbonControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public RibbonControl()
        {
            Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Initialize()
        {
            MdiMergeStyle = XR.RibbonMdiMergeStyle.Always;

            var iconList = ComponentConnector.GraphicsCache;
            Images = iconList.GetImageList(ImagesSize);
            LargeImages = iconList.GetImageList(LargeImagesSize);


            AllowKeyTips = true;
            ButtonGroupsLayout = XB.ButtonGroupsLayout.ThreeRows;
            ColorScheme = XR.RibbonControlColorScheme.DarkBlue;
            RibbonStyle = XR.RibbonControlStyle.Office2019;
            CommandLayout = XR.CommandLayout.Classic;
            DrawGroupCaptions = DevExpress.Utils.DefaultBoolean.True;
            DrawGroupsBorderMode = DevExpress.Utils.DefaultBoolean.True;
            GalleryAnimationLength = 300;
            GroupAnimationLength = 300;
            ItemAnimationLength = 300;
            ItemsVertAlign = DevExpress.Utils.VertAlignment.Top;
            MdiMergeStyle = XR.RibbonMdiMergeStyle.Always;
            OptionsAnimation.PageCategoryShowAnimation = DevExpress.Utils.DefaultBoolean.True;
            RibbonCaptionAlignment = XR.RibbonCaptionAlignment.Center;
            SearchItemShortcut = new XB.BarShortcut(Keys.Control | Keys.F);
            ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;        //  True;
            ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.True;
            ShowMoreCommandsButton = DevExpress.Utils.DefaultBoolean.True;
            ShowToolbarCustomizeItem = true;
            ShowPageHeadersMode = XR.ShowPageHeadersMode.Show;
            ShowSearchItem = true;
            
            ShowToolbarCustomizeItem = true;
            ToolbarLocation = XR.RibbonQuickAccessToolbarLocation.Below;


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

            _FreezePanelInit();
        }
        protected override void OnAddToToolbar(XB.BarItemLink link)
        {
            string text = link.Caption;
            var info = link.Item.Tag;
            base.OnAddToToolbar(link);
        }
        protected override void OnCustomizeRibbon()
        {
            base.OnCustomizeRibbon();
        }

        private void RibbonControl_ApplicationButtonClick(object sender, EventArgs e)
        {
            ShowInfo("ApplicationButtonClick");
        }

        private void RibbonControl_ItemClick(object sender, XB.ItemClickEventArgs e)
        {
            if (!(e.Item.Tag is IRibbonData ribbonData)) return;
            _RibbonItemClick(ribbonData);
            ShowInfo("ItemClick", ribbonData.ToString());
        }
        private void RibbonControl_PageGroupCaptionButtonClick(object sender, XR.RibbonPageGroupEventArgs e)
        {
            ShowInfo("PageGroupCaptionButtonClick", e.PageGroup.Text);
        }

        private void RibbonControl_PageCategoryClick(object sender, XR.PageCategoryClickEventArgs e)
        {
            ShowInfo("PageCategoryClick", e.Category.Text);
        }
        private void ShowInfo(string action, string info = "")
        {
            // MessageBox.Show(action + Environment.NewLine + info ?? "", "Click", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void _RibbonItemClick(IRibbonData item)
        {
            OnRibbonItemClick(item);
            RibbonItemClick?.Invoke(this, new TEventArgs<IRibbonData>(item));
        }
        protected virtual void OnRibbonItemClick(IRibbonData item) { }
        public event EventHandler<TEventArgs<IRibbonData>> RibbonItemClick;


        /// <summary>
        /// Standardized small image size.
        /// </summary>
        internal static readonly UserGraphicsSize ImagesSize = UserGraphicsSize.Small;
        /// <summary>
        /// Standardized large image size.
        /// </summary>
        internal static readonly UserGraphicsSize LargeImagesSize = UserGraphicsSize.Large;
        #region Non-Flicker FreezePanel
        /// <summary>
        /// Zmrazení stavu controlu tak, aby při změně obsahu neblikal a neměnil svoji velikost
        /// </summary>
        public bool Freeze { get { return _FreezePanel.Freeze; } set { _FreezePanel.Freeze = value; } }
        /// <summary>
        /// Control, který se používá pro umístění do parent containeru namísto Ribbonu. Tento control zajišťuje režim Freeze v době změn obsahu Ribbonu.
        /// </summary>
        public Panel Control { get { return _FreezePanel; } }
        /// <summary>
        /// Iniciace panelu <see cref="FreezePanel"/>
        /// </summary>
        private void _FreezePanelInit()
        {
            _FreezePanel = new FreezePanel();
            _FreezePanel.ClientControl = this;
        }
        /// <summary>
        /// Vlastní panel <see cref="FreezePanel"/>
        /// </summary>
        private FreezePanel _FreezePanel;
        #endregion
        #region Tvorba obsahu Ribbonu
        public void AddItem(IRibbonItem item)
        {
            _AddItem(item);
        }
        public void AddItems(IEnumerable<IRibbonItem> items)
        {
            if (items is null) return;
            List<IRibbonItem> list = items.Where(i => i != null).ToList();
            if (list.Count > 1) list.Sort((a, b) => CompareByOrder(a, b));
            foreach (var item in list)
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
        public void Clear()
        {
            this.Pages.Clear();
            this.Categories.Clear();
            this.PageCategories.Clear();
            this.Items.Clear();
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
        protected XR.RibbonPageCategory GetCategory(IRibbonItem item, bool enableNew = true)
        {
            if (item is null) return null;
            if (String.IsNullOrEmpty(item.CategoryId)) return null;
            XR.RibbonPageCategory category = PageCategories.GetCategoryByName(item.CategoryId);
            if (category is null && enableNew)
            {
                category = new XR.RibbonPageCategory(item.CategoryText, item.CategoryColor, item.CategoryVisible);
                category.Name = item.CategoryId;
                PageCategories.Add(category);
            }
            return category;
        }

        protected XR.RibbonPage GetPage(IRibbonItem item, XR.RibbonPageCategory category = null, bool enableNew = true)
        {
            if (item is null) return null;
            bool isCategory = !(category is null);
            XR.RibbonPage page;
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
                page = new XR.RibbonPage(item.PageText)
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

        protected XR.RibbonPageGroup GetGroup(IRibbonItem item, XR.RibbonPage page, bool enableNew = true)
        {
            if (item is null || page is null) return null;
            XR.RibbonPageGroup group = page.Groups.GetGroupByName(item.GroupId);
            if (group is null && enableNew)
            {
                group = new XR.RibbonPageGroup(item.GroupText)
                {
                    Name = item.GroupId,
                    ShowCaptionButton = false
                };
                group.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.GroupImage, ImagesSize, item.GroupText);
                page.Groups.Add(group);
            }
            return group;
        }

        protected XB.BarItem GetBarItem(IRibbonData item, XR.RibbonPageGroup group, bool enableNew = true)
        {
            if (item is null || group is null) return null;
            XB.BarItem barItem = Items[item.ItemId];
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
        protected XB.BarItem CreateBarItem(IRibbonData item)
        {
            XB.BarItem barItem = null;
            switch (item.ItemType)
            {
                case RibbonItemType.ButtonGroup:
                    XB.BarButtonGroup buttonGroup = Items.CreateButtonGroup(GetBarBaseButtons(item.SubItems));
                    buttonGroup.ButtonGroupsLayout = XB.ButtonGroupsLayout.ThreeRows;
                    buttonGroup.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
                    buttonGroup.OptionsMultiColumn.ShowItemText = DevExpress.Utils.DefaultBoolean.True;
                    barItem = buttonGroup;
                    break;
                case RibbonItemType.SplitButton:
                    XB.BarButtonItem splitButton = Items.CreateSplitButton(item.ItemText, GetPopupSubItems(item.SubItems));
                    barItem = splitButton;
                    break;
                case RibbonItemType.CheckBoxStandard:
                case RibbonItemType.CheckBoxSlider:
                    XB.BarCheckItem checkItem = Items.CreateCheckItem(item.ItemText, item.ItemIsChecked);
                    barItem = checkItem;
                    break;
                case RibbonItemType.RadioItem:
                    XB.BarCheckItem radioItem = Items.CreateCheckItem(item.ItemText, item.ItemIsChecked);
                    barItem = radioItem;
                    break;
                case RibbonItemType.Menu:
                    XB.BarSubItem menu = Items.CreateMenu(item.ItemText);
                    var menuItems = GetBarSubItems(item.SubItems);
                    foreach (var menuItem in menuItems)
                    {
                        var menuLink = menu.AddItem(menuItem);
                        if ((menuItem.Tag is IRibbonData ribbonData) && ribbonData.ItemIsFirstInGroup)
                            menuLink.BeginGroup = true;
                    }
                    barItem = menu;
                    break;
                case RibbonItemType.Button:
                default:
                    XB.BarButtonItem button = Items.CreateButton(item.ItemText);
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
        protected XB.BarBaseButtonItem CreateBaseButton(IRibbonData item)
        {
            XB.BarBaseButtonItem baseButton = null;
            switch (item.ItemType)
            {
                case RibbonItemType.Button:
                case RibbonItemType.ButtonGroup:
                    // XB.BarBaseButtonItem buttonItem = new XB.BarButtonItem(this.Manager, item.ItemText);
                    XB.BarLargeButtonItem buttonItem = new XB.BarLargeButtonItem(this.Manager, item.ItemText);
                    baseButton = buttonItem;
                    break;
                case RibbonItemType.CheckBoxStandard:
                    XB.BarCheckItem checkItem = new XB.BarCheckItem(this.Manager);
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
        protected XB.BarItem[] GetBarSubItems(IRibbonData[] items)
        {
            List<XB.BarItem> barItems = new List<XB.BarItem>();
            if (items != null)
            {
                foreach (IRibbonData item in items)
                {
                    XB.BarItem barItem = CreateBarItem(item);
                    if (barItem != null)
                        barItems.Add(barItem);
                }
            }
            return barItems.ToArray();
        }
        protected XB.BarBaseButtonItem[] GetBarBaseButtons(IRibbonData[] items)
        {
            List<XB.BarBaseButtonItem> baseButtons = new List<XB.BarBaseButtonItem>();
            if (items != null)
            {
                foreach (IRibbonData item in items)
                {
                    XB.BarBaseButtonItem baseButton = CreateBaseButton(item);
                    if (baseButton != null)
                        baseButtons.Add(baseButton);
                }
            }
            return baseButtons.ToArray();
        }
        protected XB.PopupMenu GetPopupSubItems(IRibbonData[] items)
        {
            XB.PopupMenu dxPopup = new XB.PopupMenu(BarManager);
            if (items != null)
            {
                foreach (IRibbonData item in items)
                {
                    XB.BarItem barItem = CreateBarItem(item);
                    if (barItem != null)
                    {
                        var barLink = dxPopup.AddItem(barItem);
                        if (item.ItemIsFirstInGroup) barLink.BeginGroup = true;
                    }
                }
            }
            return dxPopup;
        }
        protected void FillBarItem(XB.BarItem barItem, IRibbonData item)
        {
            barItem.Caption = item.ItemText;
            barItem.Enabled = item.ItemEnabled;
            barItem.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage, ImagesSize, item.ItemText);
            barItem.LargeImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(item.ItemImage, LargeImagesSize, item.ItemText);
            if (!string.IsNullOrEmpty(item.HotKey))
            {
                if (barItem is XB.BarSubItem)
                    ComponentConnector.ShowWarningToDeveloper($"Setup keyboard shortcut {item.HotKey} to non button barItem {barItem.Name}. This is {barItem.GetType().Name}");
                else
                    barItem.ItemShortcut = new XB.BarShortcut(WinFormServices.KeyboardHelper.GetShortcutFromServerHotKey(item.HotKey));
            }

            if (barItem is XB.BarCheckItem checkItem)
            {   // Do CheckBoxu vepisujeme víc vlastností:
                checkItem.CheckBoxVisibility = XB.CheckBoxVisibility.BeforeText;
                checkItem.CheckStyle = (item.ItemType == RibbonItemType.RadioItem ? XB.BarCheckStyles.Radio : XB.BarCheckStyles.Standard);
                checkItem.Checked = item.ItemIsChecked;
            }

            barItem.PaintStyle = item.ItemPaintStyle;
            if (item.RibbonStyle != XR.RibbonItemStyles.Default)
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
                    dxTitle.ImageOptions.Images = ComponentConnector.GraphicsCache.GetImageList(UserGraphicsSize.Large);
                    dxTitle.ImageOptions.ImageIndex = ComponentConnector.GraphicsCache.GetResourceIndex(image, UserGraphicsSize.Large);
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
        protected XR.RibbonBarManager BarManager
        {
            get
            {
                if (_BarManager is null)
                {
                    XR.RibbonBarManager rbm = new XR.RibbonBarManager(this);
                    rbm.AllowMoveBarOnToolbar = true;
                    _BarManager = rbm;
                }
                return _BarManager;
            }
        }
        private XR.RibbonBarManager _BarManager;
        #endregion
    }
    #region Interface IRibbonItem a IRibbonData;  Enumy RibbonItemType a RibbonPageType


    public interface IRibbonItem : IRibbonData
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
    public interface IRibbonData
    {
        int ItemOrder { get; }
        string ItemId { get; }
        bool ItemIsFirstInGroup { get; }
        RibbonItemType ItemType { get; }
        XR.RibbonItemStyles RibbonStyle { get; }
        bool ItemEnabled { get; }
        int? ItemToolbarOrder { get; }
        string ItemImage { get; }
        bool ItemIsChecked { get; }
        XB.BarItemPaintStyle ItemPaintStyle { get; }
        string ItemText { get; }
        string HotKey { get; }
        string ToolTip { get; }
        string ToolTipTitle { get; }
        string ToolTipIcon { get; }
        IRibbonData[] SubItems { get; }
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
    #region class FreezePanel : Panel, který umí zamrazit control na sobě hostovaný
    /// <summary>
    /// Panel, který umí zamrazit control na sobě hostovaný.
    /// Tento <see cref="FreezePanel"/> se "podsouvá" pod klientský control <see cref="ClientControl"/>, a hostuje jej "na sobě".
    /// V případě potřeby je možno nastavit <see cref="Freeze"/> = true, tím dojde k zachycení aktuálního vzhledu <see cref="ClientControl"/> do Bitmapy,
    /// tato bitmapa je vykreslena na this <see cref="FreezePanel"/>, následně je "zhasnut" klientský control (jeho "Visible" = false) a poté si s ním může aplikační kód dělat co chce.
    /// Na konci své práce dá aplikační kód <see cref="Freeze"/> = false, tím dojde k zobrazení klientského controlu (a odstranění Bitmapy).
    /// </summary>
    public class FreezePanel : Panel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public FreezePanel()
        {
            _Freeze = false;
            BorderStyle = BorderStyle.None;
        }
        /// <summary>
        /// Hostovaný klientský control. Běžně si žije svým životem, občas je zmrazen.
        /// </summary>
        public Control ClientControl { get { return _ClientControl; } set { _SetClientControl(value); } } private Control _ClientControl;
        /// <summary>
        /// Stav zmrazení vzhledu klientského controlu.
        /// <para/>
        /// V případě potřeby je možno nastavit <see cref="Freeze"/> = true, tím dojde k zachycení aktuálního vzhledu <see cref="ClientControl"/> do Bitmapy,
        /// tato bitmapa je vykreslena na this <see cref="FreezePanel"/>, následně je "zhasnut" klientský control (jeho "Visible" = false) a poté si s ním může aplikační kód dělat co chce.
        /// Na konci své práce dá aplikační kód <see cref="Freeze"/> = false, tím dojde k zobrazení klientského controlu (a odstranění Bitmapy).
        /// </summary>
        public bool Freeze { get { return _Freeze; } set { _SetFreeze(value); } } private bool _Freeze;
        /// <summary>
        /// Vložení klientského controlu včetně odvázání eventů (ze starého) a navázání eventů (do nového) controlu
        /// </summary>
        /// <param name="clientControl"></param>
        private void _SetClientControl(Control clientControl)
        {
            if (_ClientControl != null)
                _UnregisterControl(_ClientControl);
            _ClientControl = clientControl;
            if (_ClientControl != null)
                _RegisterControl(_ClientControl);
        }
        /// <summary>
        /// Navázání daného klientského controlu
        /// </summary>
        /// <param name="clientControl"></param>
        private void _RegisterControl(Control clientControl)
        {
            if (clientControl is null) return;
            bool freeze = _Freeze;
            _Freeze = false;           // Tím potlačím eventhandlery
            clientControl.SizeChanged += ClientControl_SizeChanged;
            clientControl.DockChanged += ClientControl_DockChanged;
            clientControl.VisibleChanged += ClientControl_VisibleChanged;
            this.Dock = clientControl.Dock;
            this.Size = clientControl.Size;
            if (!this.Controls.Contains(clientControl))
                this.Controls.Add(clientControl);
            _Freeze = freeze;
        }
        /// <summary>
        /// Odvázání daného klientského controlu
        /// </summary>
        /// <param name="clientControl"></param>
        private void _UnregisterControl(Control clientControl)
        {
            if (clientControl is null) return;
            clientControl.SizeChanged -= ClientControl_SizeChanged;
            clientControl.DockChanged -= ClientControl_DockChanged;
            clientControl.VisibleChanged -= ClientControl_VisibleChanged;
            if (this.Controls.Contains(clientControl))
                this.Controls.Remove(clientControl);
        }


        private void ClientControl_VisibleChanged(object sender, EventArgs e)
        {
            if (!_Freeze)
                this.Visible = _ClientControl.Visible;
        }

        private void ClientControl_DockChanged(object sender, EventArgs e)
        {
            
        }

        private void ClientControl_SizeChanged(object sender, EventArgs e)
        {
            if (!_Freeze)
                this.Size = _ClientControl.Size;
        }
        /// <summary>
        /// Nastavení stavu zmrazení
        /// </summary>
        /// <param name="freeze"></param>
        private void _SetFreeze(bool freeze)
        {
            if (freeze == _Freeze) return;                 // Reaguji jen na změnu!
            if (_ClientControl is null) return;            // Pokud nemám klienta, není co řešit...
            if (freeze)
            {
                _Freeze = true;                            // Odteď se nepřenáší _ClientControl.Visible do this.Visible, a ani Size
                Size size = this.Size;
                Bitmap bitmap = new Bitmap(size.Width, size.Height);
                Rectangle targetBounds = new Rectangle(Point.Empty, size);
                _ClientControl.DrawToBitmap(bitmap, targetBounds);
                this.BackgroundImage = bitmap;
                _ClientControl.Visible = false;
            }
            else
            {
                _ClientControl.Refresh();
                _ClientControl.Visible = true;
                if (this.BackgroundImage != null)
                    this.BackgroundImage.Dispose();
                this.BackgroundImage = null;
                _Freeze = false;
            }
        }
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
            ItemPaintStyle = XB.BarItemPaintStyle.Standard;
            RibbonStyle = XR.RibbonItemStyles.Large;
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
        public bool ItemIsFirstInGroup { get; set; }
        public RibbonItemType ItemType { get; set; }
        public XR.RibbonItemStyles RibbonStyle { get; set; }
        public bool ItemEnabled { get; set; }
        public int? ItemToolbarOrder { get; set; }
        public string ItemImage { get; set; }
        public bool ItemIsChecked { get; set; }
        public XB.BarItemPaintStyle ItemPaintStyle { get; set; }
        public string ItemText { get; set; }
        public string HotKey { get; set; }
        public string ToolTip { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipIcon { get; set; }
        public IRibbonData[] SubItems { get; set; }
        public object Tag { get; set; }
    }
    #endregion
    #region RibbonSample : testovací zdroj dat pro Ribbon
    /// <summary>
    /// RibbonSample : testovací zdroj dat pro Ribbon
    /// </summary>
    public class RibbonSample
    {
        public static void AddItems(List<IRibbonItem> items, int groupCount)
        {
            _AddItems(items, groupCount, out string info);
        }
        public static void AddItems(List<IRibbonItem> items, int groupCount, out string info)
        {
            _AddItems(items, groupCount, out info);
        }
        public static List<IRibbonItem> CreateItems(int groupCount)
        {
            List<IRibbonItem> items = new List<IRibbonItem>();
            _AddItems(items, groupCount, out string info);
            return items;
        }
        public static List<IRibbonItem> CreateItems(int groupCount, out string info)
        {
            List<IRibbonItem> items = new List<IRibbonItem>();
            _AddItems(items, groupCount, out info);
            return items;
        }
        private static void _AddItems(List<IRibbonItem> items, int groupCount, out string info)
        {
            _RibbonItemCount = 0;
            DateTime begin = DateTime.Now;
            for (int g = 0; g < groupCount; g++)
            {
                int count = Rand.Next(3, 7);
                _AddGroups(items, count);
            }
            TimeSpan time = DateTime.Now - begin;
            info = $"Vygenerováno {_RibbonItemCount} prvků v čase {time.TotalMilliseconds} milisec";
        }
        private static void _AddGroups(List<IRibbonItem> items, int count)
        {
            int page = Rand.Next(PageNames.Length);
            int pageOrder = page + 1;
            string pageId = "Page" + pageOrder;
            string pageText = PageNames[page];
            int group = Rand.Next(GroupNames.Length);
            string groupId = pageId + "." + "Group" + group;
            string groupText = GroupNames[group];
            XR.RibbonItemStyles ribbonStyle = XR.RibbonItemStyles.All;
            _AddItems(items, pageId, pageText, pageOrder, groupId, groupText, ribbonStyle, count);
        }
        private static void _AddItems(List<IRibbonItem> items, string pageId, string pageText, int pageOrder, string groupId, string groupText, XR.RibbonItemStyles ribbonStyle, int count)
        {
            int radioCount = 0;
            bool hasRadio = false;
            bool nextIsFirst = false;
            for (int w = 0; (w < count || radioCount > 0); w++)
            {
                string itemText = RandomText.GetRandomWord(true);
                string itemImage = GetRandomImageName();
                string toolTip = RandomText.GetRandomSentence(Rand.Next(5, 16));
                string toolTipTitle = RandomText.GetRandomSentence(Rand.Next(1, 3));
                int? inToolbar = ((Rand.Next(100) < 2) ? (int?)10 : null);
                if (inToolbar.HasValue)
                { }

                bool isFirst = (radioCount == 0 ? nextIsFirst || (Rand.Next(10) < 3) : false);          // Pokud nyní připravuji Radio, pak nedávám IsFirst !
                RibbonItem item = new RibbonItem()
                {
                    PageId = pageId,
                    PageText = pageText,
                    PageOrder = pageOrder,
                    GroupId = groupId,
                    GroupText = groupText,
                    ItemId = "Item" + (++_RibbonItemId),
                    ItemText = itemText,
                    ItemIsFirstInGroup = isFirst,
                    RibbonStyle = ribbonStyle,
                    ItemToolbarOrder = inToolbar,
                    ToolTip = toolTip,
                    ToolTipTitle = toolTipTitle,
                    ToolTipIcon = "help_hint_48_",
                    ItemImage = itemImage
                };
                _RibbonItemCount++;

                if (radioCount > 0)
                {
                    item.ItemType = RibbonItemType.RadioItem;
                    item.RibbonStyle = XR.RibbonItemStyles.SmallWithText;
                    radioCount--;
                    if (radioCount == 0) nextIsFirst = true;
                }
                else
                {
                    RibbonItemType itemType = GetRandomItemType();
                    if (itemType == RibbonItemType.RadioItem && hasRadio)
                        itemType = RibbonItemType.CheckBoxStandard;

                    if (itemType == RibbonItemType.RadioItem)
                    {
                        item.ItemIsFirstInGroup = true;              // RadioItem si zahajuje svoji sub-grupu
                        radioCount = Rand.Next(3, 6);                // RadioItemů do jedné grupy dám 3 - 5 za sebou
                    }

                    item.ItemType = itemType;

                    if (item.ItemType == RibbonItemType.CheckBoxStandard || item.ItemType == RibbonItemType.RadioItem)
                    {
                        if (Rand.Next(100) < 15) item.ItemImage = null;
                        if (Rand.Next(100) < 50) item.ItemIsChecked = true;
                    }

                    if (Rand.Next(10) < 3)
                        item.RibbonStyle = XR.RibbonItemStyles.SmallWithText;

                    if (NeedSubItem(itemType))
                        item.SubItems = _CreateSubItems(13);


                    nextIsFirst = false;
                }
                item.ToolTipTitle = item.ToolTipTitle + "  {" + item.ItemType.ToString() + "}";

                items.Add(item);
            }
        }
        protected static IRibbonData[] _CreateSubItems(int maxCount, int level = 0)
        {
            List<IRibbonData> subItems = new List<IRibbonData>();

            if (maxCount < 5) maxCount = 5;
            int count = Rand.Next(3, maxCount);
            for (int i = 0; i < count; i++)
            {
                string itemText = RandomText.GetRandomWord(true);
                string itemImage = GetRandomImageName(33);
                string toolTip = RandomText.GetRandomSentence(Rand.Next(5, 16));
                string toolTipTitle = RandomText.GetRandomSentence(Rand.Next(1, 3));
                bool isFirst = (Rand.Next(10) < 3);

                RibbonItem item = new RibbonItem()
                {
                    ItemId = "Item" + (++_RibbonItemId),
                    ItemText = itemText,
                    ItemIsFirstInGroup = isFirst,
                    RibbonStyle = XR.RibbonItemStyles.Default,
                    ToolTip = toolTip,
                    ToolTipTitle = toolTipTitle,
                    ToolTipIcon = "help_hint_48_",
                    ItemImage = itemImage
                };
                _RibbonItemCount++;

                item.ItemType = GetRandomItemType();
                if (NeedSubItem(item.ItemType))
                {
                    if (level <= 4)
                        item.SubItems = _CreateSubItems(7, (level + 1));
                    else
                        item.ItemType = RibbonItemType.Button;
                }

                if (item.ItemType == RibbonItemType.CheckBoxStandard || item.ItemType == RibbonItemType.RadioItem)
                {
                    if (Rand.Next(100) < 65) item.ItemImage = null;
                    if (Rand.Next(100) < 50) item.ItemIsChecked = true;
                }

                item.ToolTipTitle = item.ToolTipTitle + "  {" + item.ItemType.ToString() + "}";

                subItems.Add(item);
            }

            return subItems.ToArray();
        }
        public static RibbonItemType GetRandomItemType()
        {
            int rand = Rand.Next(100);
            if (rand < 60) return RibbonItemType.Button;
            if (rand < 70) return RibbonItemType.CheckBoxStandard;
            if (rand < 73) return RibbonItemType.RadioItem;
            // if (rand < 85) return RibbonItemType.ButtonGroup;         nějak se mi nelíbí
            if (rand < 90) return RibbonItemType.SplitButton;
            if (rand < 100) return RibbonItemType.Menu;
            return RibbonItemType.Button;
        }
        public static bool NeedSubItem(RibbonItemType itemType) { return (itemType == RibbonItemType.ButtonGroup || itemType == RibbonItemType.SplitButton || itemType == RibbonItemType.Menu); }

        public static Image GetRandomImage()
        {
            string name = GetRandomImageName();
            return ComponentConnector.GraphicsCache.GetResourceContent(name, WinFormServices.Drawing.UserGraphicsSize.Large);
        }
        public static string GetRandomImageName(int randomEmpty = 0)
        {
            if ((randomEmpty > 0) && (Rand.Next(100) < randomEmpty)) return null;
            return ResourceImages[Rand.Next(ResourceImages.Length)];
        }
        public static Random Rand { get { if (_Rand is null) _Rand = new Random(); return _Rand; } }
        private static Random _Rand;
        public static string[] PageNames { get { if (_PageNames is null) _PageNames = "DOMŮ;PŘÍRODA;TECHNIKA;VOLNÝ ČAS;LITERATURA;VZTAHY;MODIFIKACE;WIKI".Split(';'); return _PageNames; } }
        private static string[] _PageNames;
        public static string[] GroupNames { get { if (_GroupNames is null) _GroupNames = "Základní;Rozšířené;Údržba;Oblíbené;Systém;Grafy;Archivace;Expertní funkce;Tisky".Split(';'); return _GroupNames; } }
        private static string[] _GroupNames;
        public static string[] ResourceImages { get { if (_ResourceImages is null) _ResourceImages = _GetResourceImages(); return _ResourceImages; } }
        private static string[] _ResourceImages;
        private static string[] _GetResourceImages()
        {
            // TestDevExpress.Properties.Resources.address_book_new
            List<string> names = new List<string>();
            var properties = typeof(TestDevExpress.Properties.Resources).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var property in properties)
            {
                object value = property.GetValue(null);
                if (value is Image)
                    names.Add(property.Name);
            }
            return names.ToArray();
        }
        private static int _RibbonItemId = 0;
        private static int _RibbonItemCount = 0;
    }
    #endregion

}
