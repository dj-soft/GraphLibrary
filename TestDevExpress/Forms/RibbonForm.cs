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
            DxComponent.SplashShow("Testovací aplikace Helios Nephrite", "DJ soft & ASOL",
                "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                this, Properties.Resources.Moon10,
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

            _DxMainSplit = DxComponent.CreateDxSplitContainer(this, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, splitPosition: 300, showSplitGlyph: true);

            _DxMainPanel = DxComponent.CreateDxPanel(_DxMainSplit.Panel1, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
            _DxMainPanel.SizeChanged += _DxMainPanel_SizeChanged;
            DxComponent.CreateDxLabel(10, 10, 500, _DxMainPanel, "Zde bude DataForm", styleType: LabelStyleType.SubTitle);

            _DxLogMemoEdit = DxComponent.CreateDxMemoEdit(_DxMainSplit.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);

            this._DxRibbonControl = new DxRibbonControl();
            this.Ribbon = _DxRibbonControl;
            this.Controls.Add(this._DxRibbonControl);

            _DxRibbonFill();
            this._DxRibbonControl.RibbonItemClick += _DxRibbonControl_RibbonItemClick;


            this._DxRibbonStatusBar = new DxRibbonStatusBar();
            this._DxRibbonStatusBar.Ribbon = this._DxRibbonControl;
            this.StatusBar = _DxRibbonStatusBar;
            this.Controls.Add(this._DxRibbonStatusBar);

            this._StatusItemTitle = CreateStatusBarItem();
          
            // V tomto pořadí budou viditelné:
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemTitle);
            
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DxComponent.LogTextChanged += DxComponent_LogTextChanged;
            _LogContainChanges = true;
        }
        private DxRibbonControl _DxRibbonControl;
        private DxRibbonStatusBar _DxRibbonStatusBar;
        private DxSplitContainerControl _DxMainSplit;
        private DxPanelControl _DxMainPanel;
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
        private void _DxMainPanel_SizeChanged(object sender, EventArgs e)
        {
            
        }
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        #endregion
        #region Ribbon - obsah a rozcestník
        private void _DxRibbonFill()
        {
            string imgReload = "images/xaf/action_reload_32x32.png";
            string imgZoom = "images/zoom/zoom_32x32.png";

            List<IRibbonItem> items = new List<IRibbonItem>();

            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "design", GroupText = "DESIGN", ItemId = "Dx.Design.Skin", ItemType = RibbonItemType.SkinSetDropDown });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "design", GroupText = "DESIGN", ItemId = "Dx.Design.Palette", ItemType = RibbonItemType.SkinPaletteDropDown });

            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.UseLazyInit", ItemText = "Use Lazy Init", ToolTip = "Zaškrtnuto: používat opožděné plnění stránek Ribbonu (=až bude potřeba)\r\nNezaškrtnuto: fyzicky naplní celý Ribbon okamžitě, delší čas přípravy okna", ItemType = RibbonItemType.CheckBoxToggle, ItemIsChecked = UseLazyLoad, RibbonStyle = RibbonItemStyles.Large });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.Refill", ItemText = "Refill ribbonu", ItemImage = imgReload });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.ClearRefill", ItemText = "Clear a Refill", ItemImage = imgReload });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.EmptyRefill", ItemText = "Empty a Refill", ItemImage = imgReload });
            items.Add(new RibbonItem() { PageId = "DX", PageText = "DevExpress", GroupId = "params", GroupText = "RIBBON TEST", ItemId = "Dx.Test.ImgPick", ItemText = "Image Picker", ItemImage = imgZoom });

            DxRibbonSample.CreateItemsTo(items, 68);

            this._DxRibbonControl.Clear();
            this._DxRibbonControl.UseLazyContentCreate = this.UseLazyLoad;
            this._DxRibbonControl.AddItems(items);
        }

        private void _RibbonTestRefill(RefillCommand command)
        {
            var pageId = this._DxRibbonControl.SelectedPageId;
            switch (command)
            {
                case RefillCommand.Fast:
                case RefillCommand.Slow:
                    this._DxRibbonControl.Clear();
                    break;
                case RefillCommand.Empty:
                    this._DxRibbonControl.Empty();
                    break;
            }
            ThreadManager.AddAction(_RibbonTestRefill2, pageId, command);
        }
        private void _RibbonTestRefill2(object[] pars)
        {
            RefillCommand command = (RefillCommand)pars[1];
            switch (command)
            {
                case RefillCommand.Fast:
                    break;
                case RefillCommand.Slow:
                case RefillCommand.Empty:
                    System.Threading.Thread.Sleep(650);
                    break;
            }

            this.RunInGui(() => _RibbonTestRefill3(pars[0] as string, command));
        }
        private void _RibbonTestRefill3(string pageId, RefillCommand command)
        {
            _DxRibbonFill();
            switch (command)
            {
                case RefillCommand.Fast:
                case RefillCommand.Slow:
                    break;
                case RefillCommand.Empty:
                    this._DxRibbonControl.Final();
                    break;
            }
            this._DxRibbonControl.SelectedPageId = pageId;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IMenuItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.UseLazyInit":
                    UseLazyLoad = e.Item.ItemIsChecked ?? false;
                    break;
                case "Dx.Test.Refill":
                    DxComponent.LogClear();
                    _RibbonTestRefill(RefillCommand.Fast);
                    break;
                case "Dx.Test.ClearRefill":
                    DxComponent.LogClear();
                    _RibbonTestRefill(RefillCommand.Slow);
                    break;
                case "Dx.Test.EmptyRefill":
                    DxComponent.LogClear();
                    _RibbonTestRefill(RefillCommand.Empty);
                    break;
                case "Dx.Test.ImgPick":
                    ImagePickerForm.ShowForm(this);
                    break;
            }
        }
        private enum RefillCommand { None, Fast, Slow, Empty }
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
