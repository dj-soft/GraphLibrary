using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    public class RibbonForm : DxRibbonForm
    {
        #region Konstruktor a proměnné
        public RibbonForm()
        {
            DxComponent.SplashShow("Testovací okno Ribbonů Nephrite", "DJ soft & ASOL",
                "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                this, Properties.Resources.Moon10,opacityColor: System.Drawing.Color.BlueViolet,
                useFadeOut: false);

            this.UseLazyLoad = true;
            this.InitializeForm();
            System.Windows.Forms.Application.Idle += Application_Idle;

            DxComponent.SplashUpdate(rightFooter: "Už to jede...");
        }
        protected override void OnShown(EventArgs e)
        {
            DxComponent.SplashHide();
            base.OnShown(e);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxComponent.LogTextChanged -= DxComponent_LogTextChanged;
            System.Windows.Forms.Application.Idle -= Application_Idle;
        }
        public static void PrepareSkin()
        {
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "iMaginary";
        }
        protected void InitializeForm()
        {
            this.Size = new System.Drawing.Size(800, 600);
            this.Text = "TESTER DataForm";
            this.AllowMdiBar = true;

            _DxMainSplit = DxComponent.CreateDxSplitContainer(this, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, splitPosition: 400, showSplitGlyph: true);

            _DxLeftSplit = DxComponent.CreateDxSplitContainer(_DxMainSplit.Panel1, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Horizontal,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel1, splitPosition: 400, showSplitGlyph: true);

            _DxBottomSplit = DxComponent.CreateDxSplitContainer(_DxLeftSplit.Panel2, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.None, splitPosition: 400, showSplitGlyph: true);

            _DxLogMemoEdit = DxComponent.CreateDxMemoEdit(_DxMainSplit.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);

            this._DxRibbonControl = new DxRibbonControl() { DebugName = "MainRibbon" };
            this.Ribbon = _DxRibbonControl;
            this.Controls.Add(this._DxRibbonControl);

            _DxRibbonFill();
            this._DxRibbonControl.RibbonItemClick += _DxRibbonControl_RibbonItemClick;

            _TestPanel1 = new RibbonTestPanel();
            _TestPanel1.UseLazyLoad = this.UseLazyLoad;
            _TestPanel1.Ribbon.DebugName = "Slave 1";
            _TestPanel1.ParentRibbon = _DxRibbonControl;
            _TestPanel1.FillRibbon();
            _DxLeftSplit.Panel1.Controls.Add(_TestPanel1);

            _TestPanel2a = new RibbonTestPanel();
            _TestPanel2a.UseLazyLoad = this.UseLazyLoad;
            _TestPanel2a.Ribbon.DebugName = "Slave 2A";
            _TestPanel2a.ParentRibbon = _TestPanel1.Ribbon;
            _TestPanel2a.FillRibbon();
            _DxBottomSplit.Panel1.Controls.Add(_TestPanel2a);

            _TestPanel2b = new RibbonTestPanel();
            _TestPanel2b.UseLazyLoad = this.UseLazyLoad;
            _TestPanel2b.Ribbon.DebugName = "Slave 2B";
            _TestPanel2b.ParentRibbon = _TestPanel1.Ribbon;
            _TestPanel2b.FillRibbon();
            _DxBottomSplit.Panel2.Controls.Add(_TestPanel2b);

            this._DxRibbonStatusBar = new DxRibbonStatusBar();
            this._DxRibbonStatusBar.Ribbon = this._DxRibbonControl;
            this.StatusBar = _DxRibbonStatusBar;
            this.Controls.Add(this._DxRibbonStatusBar);

            this._StatusItemTitle = CreateStatusBarItem();
          
            // V tomto pořadí budou StatusItemy viditelné (tady je zatím jen jeden):
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemTitle);
            
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DxComponent.LogTextChanged += DxComponent_LogTextChanged;
            _LogContainChanges = true;
        }
        private DxRibbonControl _DxRibbonControl;
        private DxRibbonStatusBar _DxRibbonStatusBar;
        private DxSplitContainerControl _DxMainSplit;
        private DxSplitContainerControl _DxLeftSplit;
        private DxSplitContainerControl _DxBottomSplit;
        private RibbonTestPanel _TestPanel1;
        private RibbonTestPanel _TestPanel2a;
        private RibbonTestPanel _TestPanel2b;
        private DxMemoEdit _DxLogMemoEdit;
        private DevExpress.XtraBars.BarStaticItem CreateStatusBarItem(int? fontSizeDelta = null)
        {
            DevExpress.XtraBars.BarStaticItem item = new DevExpress.XtraBars.BarStaticItem();
            item.MinWidth = 240;
            item.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            if (fontSizeDelta.HasValue)
                item.Appearance.FontSizeDelta = fontSizeDelta.Value;
            item.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            return item;
        }
        
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        #endregion
        #region Ribbon - obsah a rozcestník
        private void _DxRibbonFill()
        {
            string imgZoom = "images/zoom/zoom_32x32.png";

            List<IRibbonItem> items = new List<IRibbonItem>();

            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "design", GroupText = "DESIGN", ItemId = "Dx.Design.Skin", ItemType = RibbonItemType.SkinSetDropDown });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "design", GroupText = "DESIGN", ItemId = "Dx.Design.Palette", ItemType = RibbonItemType.SkinPaletteDropDown });

            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.UseLazyInit", ItemText = "Use Lazy Init", ToolTip = "Zaškrtnuto: používat opožděné plnění stránek Ribbonu (=až bude potřeba)\r\nNezaškrtnuto: fyzicky naplní celý Ribbon okamžitě, delší čas přípravy okna", ItemType = RibbonItemType.CheckBoxToggle, ItemIsChecked = UseLazyLoad, RibbonStyle = RibbonItemStyles.Large });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.ImgPick", ItemText = "Image Picker", ItemImage = imgZoom });

            this._DxRibbonControl.Clear();
            this._DxRibbonControl.UseLazyContentCreate = this.UseLazyLoad;
            this._DxRibbonControl.AddItems(items);
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IMenuItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.UseLazyInit":
                    UseLazyLoad = e.Item.ItemIsChecked ?? false;
                    _TestPanel1.UseLazyLoad = UseLazyLoad;
                    _TestPanel2a.UseLazyLoad = UseLazyLoad;
                    break;
                case "Dx.Test.ImgPick":
                    ImagePickerForm.ShowForm(this);
                    break;
            }
        }
        /// <summary>
        /// Bude se používat LazyLoad
        /// </summary>
        public bool UseLazyLoad { get; set; }
        #endregion
        #region Logování
        private void DxComponent_LogTextChanged(object sender, EventArgs e)
        {
            _LogContainChanges = true;
        }
        private void Application_Idle(object sender, EventArgs e)
        {
            if (_LogContainChanges)
                _RefreshLog();
        }
        private void _RefreshLog()
        {
            var logText = DxComponent.LogText;
            if (logText != null)
            {
                _DxLogMemoEdit.Text = logText;
                _DxLogMemoEdit.SelectionStart = logText.Length;
                _DxLogMemoEdit.SelectionLength = 0;
                _DxLogMemoEdit.ScrollToCaret();
            }
            _LogContainChanges = false;
        }
        bool _LogContainChanges;
        #endregion
    }
    /// <summary>
    /// Testovací panel
    /// </summary>
    public class RibbonTestPanel : DxPanelControl, IMenuItemActionHandler
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public RibbonTestPanel()
        {
            InitializeContent();
        }
        /// <summary>
        /// Inicializaace obsahu
        /// </summary>
        protected void InitializeContent()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            _Ribbon = new DxRibbonControl() { Dock = System.Windows.Forms.DockStyle.Top, ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False };
            this.Controls.Add(_Ribbon);

            int x = 20;
            _ButtonClear = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Clear All", _RunClear); x += 160;
            _ButtonEmpty = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Empty", _RunEmpty); x += 160;
            _ButtonAdd5 = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Add 7 groups", _RunAdd5Groups); x += 160;
            _ButtonAdd30 = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Add 30 groups", _RunAdd30Groups); x += 160;
            _ButtonFinal = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Final", _RunFinal); x += 160;

            x += 60;
            _ButtonMerge = DxComponent.CreateDxCheckButton(x, 160, 150, 52, this, "Merge nahoru", _RunMerge); x += 160;
            _ButtonUnMerge = DxComponent.CreateDxCheckButton(x, 160, 150, 52, this, "Unmerge", _RunUnMerge, isChecked: true); x += 160;

            DoLayoutButtons();
        }
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayoutButtons();
        }
        private void DoLayoutButtons()
        {
            int width = this.ClientSize.Width;
            bool isSmall = (width < 1200);
            int x0 = (isSmall ? 10 : 20);
            int y0 = 160;
            int w = (isSmall ? 112 : 150);
            int h = (isSmall ? 44 : 54);
            int s = (isSmall ? 5 : 10);
            int xs = w + s;
            int ds = (isSmall ? 25 : 60);
            int tw = (5 * w + 4 * s) + (isSmall ? 0 : (ds + 2 * w + s));
            if ((2 * x0 + tw) < width)
                x0 = (width - tw) / 2;

            int x = x0;
            int y = y0;
            _ButtonClear.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;
            _ButtonEmpty.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;
            _ButtonAdd5.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;
            _ButtonAdd30.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;
            _ButtonFinal.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;

            if (isSmall)
            {
                x = x0 + xs;
                y = y0 + h + 6;
                w = 3 * w / 2;
                xs = w + 10;
            }
            else
            {
                x += ds;
            }

            _ButtonMerge.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;
            _ButtonUnMerge.Bounds = new System.Drawing.Rectangle(x, y, w, h); x += xs;

            System.Drawing.Size imageSize = new System.Drawing.Size(h - 8, h - 8);
            DxComponent.ApplyImage(_ButtonClear.ImageOptions, resourceName: "svgimages/dashboards/delete.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonEmpty.ImageOptions, resourceName: "images/xaf/templatesv2images/action_delete.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonAdd5.ImageOptions, resourceName: "svgimages/icon%20builder/actions_add.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonAdd30.ImageOptions, resourceName: "svgimages/icon%20builder/actions_addcircled.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonFinal.ImageOptions, resourceName: "svgimages/icon%20builder/actions_send.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonMerge.ImageOptions, resourceName: "svgimages/spreadsheet/fillup.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonUnMerge.ImageOptions, resourceName: "svgimages/spreadsheet/filldown.svg", imageSize: imageSize);
        }
        /// <summary>
        /// Zdejší Ribbon
        /// </summary>
        public DxRibbonControl Ribbon { get { return _Ribbon; } }
        /// <summary>
        /// Parent Ribbon, do něhož se mergujeme
        /// </summary>
        public DxRibbonControl ParentRibbon { get; set; }
        /// <summary>
        /// Bude se používat LazyLoad
        /// </summary>
        public bool UseLazyLoad { get { return this._Ribbon.UseLazyContentCreate; } set { this._Ribbon.UseLazyContentCreate = value; } }
        /// <summary>
        /// Naplní Robbon daným épočtem grup
        /// </summary>
        /// <param name="groups"></param>
        public void FillRibbon(int groups = 30)
        {
            var items = DxRibbonSample.CreateItems(groups);
            items.Cast<RibbonItem>().ForEachExec(i => i.ActionHandler = this);
            _Ribbon.AddItems(items);
        }
        private DxRibbonControl _Ribbon;
        private DxSimpleButton _ButtonClear;
        private DxSimpleButton _ButtonEmpty;
        private DxSimpleButton _ButtonAdd5;
        private DxSimpleButton _ButtonAdd30;
        private DxSimpleButton _ButtonFinal;
        private DxCheckButton _ButtonMerge;
        private DxCheckButton _ButtonUnMerge;
        private void _RunClear(object sender, EventArgs args) 
        {
            this._Ribbon.Clear();
        }
        private void _RunEmpty(object sender, EventArgs args)
        {
            this._Ribbon.Empty();
        }
        private void _RunAdd5Groups(object sender, EventArgs args) 
        {
            FillRibbon(5);
        }
        private void _RunAdd30Groups(object sender, EventArgs args)
        {
            FillRibbon(30);
        }
        private void _RunFinal(object sender, EventArgs args) 
        {
            this._Ribbon.Final(); 
        }
        private void _RunMerge(object sender, EventArgs args)
        {
            if (ParentRibbon == null) return;
            ParentRibbon.MergeRibbon(this.Ribbon);
            // this._ButtonMerge.Checked = true;
            this._ButtonUnMerge.Checked = false;
        }
        private void _RunUnMerge(object sender, EventArgs args) 
        {
            if (ParentRibbon == null) return;
            ParentRibbon.UnMergeRibbon();
            this._ButtonMerge.Checked = false;
            // this._ButtonUnMerge.Checked = true;
        }
        void IMenuItemActionHandler.MenuItemAction(IMenuItem menuItem)
        {
            Noris.Clients.Win.Components.DialogArgs dialogArgs = new Noris.Clients.Win.Components.DialogArgs();
            dialogArgs.Title = "Ribbon Click";
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.MessageText = $"Uživatel kliknul na prvek <b>{menuItem.ItemType}</b>, s textem <b>{menuItem.ItemText}</b>.";
            dialogArgs.PrepareButtons(System.Windows.Forms.MessageBoxButtons.OK);
            dialogArgs.Owner = this.FindForm();
            Noris.Clients.Win.Components.DialogForm.ShowDialog(dialogArgs);
        }
    }
    #region RibbonSample : testovací zdroj dat pro Ribbon
    /// <summary>
    /// RibbonSample : testovací zdroj dat pro Ribbon
    /// </summary>
    public class DxRibbonSample
    {
        public static List<IRibbonItem> CreateItems(int groupCount)
        {
            List<IRibbonItem> items = new List<IRibbonItem>();
            _AddItems(items, groupCount);
            return items;
        }
        public static void CreateItemsTo(List<IRibbonItem> items, int groupCount)
        {
            _AddItems(items, groupCount);
        }
        private static void _AddItems(List<IRibbonItem> items, int groupCount)
        {
            _RibbonItemCount = 0;
            for (int g = 0; g < groupCount; g++)
            {
                int count = Rand.Next(3, 7);
                _AddGroups(items, count);
            }
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
            RibbonItemStyles ribbonStyle = RibbonItemStyles.All;
            _AddItems(items, pageId, pageText, pageOrder, groupId, groupText, ribbonStyle, count);
        }
        private static void _AddItems(List<IRibbonItem> items, string pageId, string pageText, int pageOrder, string groupId, string groupText, RibbonItemStyles ribbonStyle, int count)
        {
            bool isCategory = (pageText == "VZTAHY" || pageText == "MODIFIKACE");
            string categoryId = (isCategory ? "Extend1" : null);
            string categoryText = (isCategory ? "...rozšířené informace..." : null);
            System.Drawing.Color categoryColor = (isCategory ? System.Drawing.Color.DarkViolet : System.Drawing.Color.Empty);
            bool categoryVisible = (isCategory ? true : false);
            int radioCount = 0;
            bool hasRadio = false;
            bool nextIsFirst = false;
            for (int w = 0; (w < count || radioCount > 0); w++)
            {
                string itemText = Random.GetWord(true);
                string itemImageName = GetRandomImageName();
                string toolTip = Random.GetSentence(Rand.Next(5, 16));
                string toolTipTitle = Random.GetSentence(Rand.Next(1, 3));
                int? inToolbar = ((Rand.Next(100) < 2) ? (int?)10 : null);
                if (inToolbar.HasValue)
                { }

                bool isFirst = (radioCount == 0 ? nextIsFirst || (Rand.Next(10) < 3) : false);          // Pokud nyní připravuji Radio, pak nedávám IsFirst !
                RibbonItem item = new RibbonItem()
                {
                    CategoryId = categoryId,
                    CategoryText = categoryText,
                    CategoryColor = categoryColor,
                    CategoryVisible = categoryVisible,
                    PageId = pageId,
                    PageText = pageText,
                    PageOrder = pageOrder,
                    GroupId = groupId,
                    GroupText = groupText,
                    ItemId = "Item" + (++_RibbonItemId),
                    ItemText = itemText,
                    ItemImage = itemImageName,
                    ItemIsFirstInGroup = isFirst,
                    RibbonStyle = ribbonStyle,
                    ItemToolbarOrder = inToolbar,
                    ToolTip = toolTip,
                    ToolTipTitle = toolTipTitle,
                    ToolTipIcon = "help_hint_48_"
                };
                _RibbonItemCount++;

                if (radioCount > 0)
                {
                    item.ItemType = RibbonItemType.RadioItem;
                    item.RibbonStyle = RibbonItemStyles.SmallWithText;
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
                        item.RibbonStyle = RibbonItemStyles.SmallWithText;

                    if (NeedSubItem(itemType, 0))
                        item.SubItems = _CreateSubItems(13);


                    nextIsFirst = false;
                }
                item.ToolTipTitle = item.ToolTipTitle + "  {" + item.ItemType.ToString() + "}";

                items.Add(item);
            }
        }
        protected static IMenuItem[] _CreateSubItems(int maxCount, int level = 0)
        {
            List<IMenuItem> subItems = new List<IMenuItem>();

            if (maxCount < 5) maxCount = 5;
            int count = Rand.Next(3, maxCount);
            for (int i = 0; i < count; i++)
            {
                string itemText = Random.GetWord(true);
                string itemImage = GetRandomImageName(33);
                string toolTip = Random.GetSentence(Rand.Next(5, 16));
                string toolTipTitle = Random.GetSentence(Rand.Next(1, 3));
                bool isFirst = (Rand.Next(10) < 3);

                RibbonItem item = new RibbonItem()
                {
                    ItemId = "Item" + (++_RibbonItemId),
                    ItemText = itemText,
                    ItemIsFirstInGroup = isFirst,
                    RibbonStyle = RibbonItemStyles.Default,
                    ToolTip = toolTip,
                    ToolTipTitle = toolTipTitle,
                    ToolTipIcon = "help_hint_48_",
                    ItemImage = itemImage
                };
                _RibbonItemCount++;

                item.ItemType = GetRandomItemType();
                if (NeedSubItem(item.ItemType, level))
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
        public static bool NeedSubItem(RibbonItemType itemType, int level)
        {
            bool canSubItem = (itemType == RibbonItemType.ButtonGroup || itemType == RibbonItemType.SplitButton || itemType == RibbonItemType.Menu);
            if (canSubItem && (level == 0 || Rand.Next(100) < 40)) return true;
            return false;
        }

        public static string GetRandomImageName(int randomEmpty = 0)
        {
            if ((randomEmpty > 0) && (Rand.Next(100) < randomEmpty)) return null;
            return ResourceImages[Rand.Next(ResourceImages.Length)];
        }
        public static System.Random Rand { get { if (_Rand is null) _Rand = new System.Random(); return _Rand; } }
        private static System.Random _Rand;
        public static string[] PageNames { get { if (_PageNames is null) _PageNames = "ASTROLOGIE;PŘÍRODA;TECHNIKA;VOLNÝ ČAS;LITERATURA;VZTAHY;MODIFIKACE;WIKI".Split(';'); return _PageNames; } }
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
                if (value is System.Drawing.Image image && image.Size.Width <= 48)
                    names.Add(property.Name);
            }
            return names.ToArray();
        }
        private static int _RibbonItemId = 0;
        private static int _RibbonItemCount = 0;
    }
    #endregion
}
