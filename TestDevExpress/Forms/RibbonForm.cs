using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Menu;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formukář pro testy Ribbonů
    /// </summary>
    public class RibbonForm : DxRibbonForm
    {
        #region Konstruktor a proměnné
        public RibbonForm()
        {
            DxComponent.SplashShow("Testovací okno Ribbonů Nephrite", "DJ soft & ASOL",
                "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                this, Properties.Resources.Moon10, opacityColor: System.Drawing.Color.FromArgb(80, 80, 180), opacity: 120,
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

            this.Text = "Test Ribbons FW 4.8";

            this.AllowMdiBar = true;

            _DxMainSplit = DxComponent.CreateDxSplitContainer(this, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, splitPosition: 400, showSplitGlyph: true);

            _DxLeftSplit = DxComponent.CreateDxSplitContainer(_DxMainSplit.Panel1, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Horizontal,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel1, splitPosition: 400, showSplitGlyph: true);

            _DxBottomSplit = DxComponent.CreateDxSplitContainer(_DxLeftSplit.Panel2, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.None, splitPosition: 650, showSplitGlyph: true);

            _DxLogMemoEdit = DxComponent.CreateDxMemoEdit(_DxMainSplit.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);

            this._DxRibbonControl = new DxRibbonControl() { DebugName = "MainRibbon" };
            this._DxRibbonControl.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this._DxRibbonControl.ApplicationButtonText = " SYSTEM ";

            this.Ribbon = _DxRibbonControl;
            this.Controls.Add(this._DxRibbonControl);

            _DxRibbonFill();
            this._DxRibbonControl.RibbonItemClick += _DxRibbonControl_RibbonItemClick;

            _TestPanel1 = new RibbonTestPanel();
            _TestPanel1.UseLazyLoad = this.UseLazyLoad;
            _TestPanel1.Ribbon.DebugName = "Slave 1";
            _TestPanel1.ParentRibbon = _DxRibbonControl;
            _TestPanel1.CategoryName = "SKUPINA 1";
            _TestPanel1.CategoryColor = System.Drawing.Color.LightBlue;
            _TestPanel1.FillRibbon();
            _DxLeftSplit.Panel1.Controls.Add(_TestPanel1);

            _TestPanel2a = new RibbonTestPanel();
            _TestPanel2a.UseLazyLoad = this.UseLazyLoad;
            _TestPanel2a.Ribbon.DebugName = "Slave 2A";
            _TestPanel2a.ParentRibbon = _TestPanel1.Ribbon;
            _TestPanel2a.CategoryName = "SKUPINA 2A";
            _TestPanel2a.CategoryColor = System.Drawing.Color.LightYellow;
            _TestPanel2a.FillRibbon();
            _DxBottomSplit.Panel1.Controls.Add(_TestPanel2a);

            _TestPanel2b = new RibbonTestPanel();
            _TestPanel2b.UseLazyLoad = this.UseLazyLoad;
            _TestPanel2b.Ribbon.DebugName = "Slave 2B";
            _TestPanel2b.ParentRibbon = _TestPanel1.Ribbon;
            _TestPanel2b.CategoryName = "SKUPINA 2B";
            _TestPanel2b.CategoryColor = System.Drawing.Color.LightGreen;
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
            string imgLogClear = "svgimages/snap/cleartablestyle.svg";
            string imgInfo = "svgimages/xaf/action_aboutinfo.svg";

            List<IRibbonPage> pages = new List<IRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "DevExpress" };
            pages.Add(page);

            group = new DataRibbonGroup() { GroupId = "design", GroupText = "DESIGN" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Design.Skin", RibbonItemType = RibbonItemType.SkinSetDropDown });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Design.Palette", RibbonItemType = RibbonItemType.SkinPaletteDropDown });

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "RIBBON TEST" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.UseLazyInit", Text = "Use Lazy Init", ToolTipText = "Zaškrtnuto: používat opožděné plnění stránek Ribbonu (=až bude potřeba)\r\nNezaškrtnuto: fyzicky naplní celý Ribbon okamžitě, delší čas přípravy okna", RibbonItemType = RibbonItemType.CheckBoxToggle, Checked = UseLazyLoad, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.ImgPick", Text = "Image Picker", ToolTipText = "Otevře nabídku systémových ikon", Image = imgZoom, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.LogClear", Text = "Clear log", ToolTipText = "Smaže obsah logu vpravo", Image = imgLogClear, RibbonStyle = RibbonItemStyles.Large });

            page = new DataRibbonPage() { PageId = "HELP", PageText = "Nápověda" };
            pages.Add(page);
            group = new DataRibbonGroup() { GroupId = "help", GroupText = "NÁPOVĚDA" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Help.Help.Show", Text = "Nápovědda", ToolTipText = "Zobrazí okno s nápovědou", Image = imgInfo });

            this._DxRibbonControl.Clear();
            this._DxRibbonControl.UseLazyContentCreate = this.UseLazyLoad;
            this._DxRibbonControl.AddPages(pages);
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.UseLazyInit":
                    UseLazyLoad = e.Item.Checked ?? false;
                    _TestPanel1.UseLazyLoad = UseLazyLoad;
                    _TestPanel2a.UseLazyLoad = UseLazyLoad;
                    break;
                case "Dx.Test.ImgPick":
                    ImagePickerForm.ShowForm(this);
                    break;
                case "Dx.Test.LogClear":
                    DxComponent.LogClear();
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
    #region class RibbonTestPanel : Testovací panel s Ribbonem a tlačítky pro mergování
    /// <summary>
    /// Testovací panel s Ribbonem a tlačítky pro mergování
    /// </summary>
    public class RibbonTestPanel : DxPanelControl
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
            _Ribbon.PageOnDemandLoad += _Ribbon_PageOnDemandLoad;
            _Ribbon.RibbonApplicationButtonClick += _Ribbon_RibbonApplicationButtonClick;
            _Ribbon.RibbonPageCategoryClick += _Ribbon_RibbonPageCategoryClick;
            _Ribbon.RibbonGroupButtonClick += _Ribbon_RibbonGroupButtonClick;
            _Ribbon.RibbonItemClick += _Ribbon_RibbonItemClick;

            this.Controls.Add(_Ribbon);

            int x = 20;
            _ButtonClear = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Clear", _RunClear, toolTipText: "Smaže obsah Ribbonu a nechá jej prázdný"); x += 160;
            _ButtonFill = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, this, "Fill", _RunFill, toolTipText: "Smaže obsah Ribbonu a vepíše do něj větší množství stránek"); x += 160;
            _ButtonMenu = DxComponent.CreateDxDropDownButton(x, 160, 150, 52, this, "Add 4", click: DropDownButtonClick, itemClick: DropDownItemClick, subItems: _GetDropDownItems(), toolTipText: "Přidá menší počet prvků. Další volby jsou v menu.");

            x += 60;
            _ButtonMerge = DxComponent.CreateDxCheckButton(x, 160, 150, 52, this, "Merge nahoru", _RunMerge, toolTipText: "Obsah tohoto Ribbonu připojí k buttonu o úroveň výše"); x += 160;
            _ButtonUnMerge = DxComponent.CreateDxCheckButton(x, 160, 150, 52, this, "Unmerge", _RunUnMerge, isChecked: true, toolTipText: "Vrátí obsah tohoto Ribbonu z vyššího Ribbonu zpátky"); x += 160;

            DoLayoutButtons();

            this.IsMerged = false;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayoutButtons();
        }
        private void DoLayoutButtons()
        {
            int width = this.ClientSize.Width;
            int count1 = 3;
            int count2 = 2;
            bool isSmall = (width < 825);
            int y0 = 160;
            int w = (isSmall ? 112 : 150);
            int h1 = (isSmall ? 38 : 54);
            int h2 = (isSmall ? 44 : 54);
            int s = (isSmall ? 5 : 10);
            int xs = w + s;
            int ds = (isSmall ? 0 : 35);

            int wc = (count1 * w + (count1 - 1) * s) + (isSmall ? 0 : (ds + (count2 * w + (count2 - 1) * s)));
            int x0 = (width - wc) / 2;

            int x = x0;
            int y = y0;
            _ButtonClear.Bounds = new System.Drawing.Rectangle(x, y, w, h1); x += xs;
            _ButtonFill.Bounds = new System.Drawing.Rectangle(x, y, w, h1); x += xs;
            _ButtonMenu.Bounds = new System.Drawing.Rectangle(x, y, w, h1); x += xs;

            if (isSmall)
            {
                w = 3 * w / 2;
                xs = w + s;
                wc = (count2 * w + (count2 - 1) * s);
                x0 = (width - wc) / 2;
                x = x0;
                y = y0 + h1 + 6;
            }
            else
            {
                x += ds;
            }

            _ButtonMerge.Bounds = new System.Drawing.Rectangle(x, y, w, h2); x += xs;
            _ButtonUnMerge.Bounds = new System.Drawing.Rectangle(x, y, w, h2); x += xs;

            int svgS = (isSmall ? h1 - 8 : h1 - 12);
            System.Drawing.Size imageSize = new System.Drawing.Size(svgS, svgS);
            DxComponent.ApplyImage(_ButtonClear.ImageOptions, resourceName: "svgimages/dashboards/delete.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonMenu.ImageOptions, resourceName: "svgimages/icon%20builder/actions_add.svg", imageSize: imageSize);
            DxComponent.ApplyImage(_ButtonFill.ImageOptions, resourceName: "svgimages/icon%20builder/actions_addcircled.svg", imageSize: imageSize);
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
        /// Suffix kategorie, dovolí odlišit kategorie parenta od kategorie child ribbonu
        /// </summary>
        public string CategoryName { get; set; }
        /// <summary>
        /// Barva kategorií
        /// </summary>
        public System.Drawing.Color CategoryColor { get; set; }
        /// <summary>
        /// Obsahuje true pro mergovaný Ribbon do <see cref="ParentRibbon"/>, false pro unmergovaný.
        /// Lze setovat, reaguje mergováním dle hodnoty.
        /// Lze setovat i opakovaně stejnou hodnotu, provede se další pokus o Merge / UnMerge.
        /// </summary>
        public bool IsMerged { get { return _IsMerged; } set { _SetMerged(value); } }
        private bool _IsMerged;
        private void _SetMerged(bool isMerged)
        {
            // if (isMerged == _IsMerged) return;           takhle ne, chceme povolit i opakované spuštění MergeCurrentDxToParent() !
            if (ParentRibbon != null)
            {
                if (isMerged)
                    this.Ribbon.MergeCurrentDxToParent(ParentRibbon);
                else
                    this.Ribbon.UnMergeCurrentDxFromParent();
                _IsMerged = isMerged;
            }
            else
            {
                _IsMerged = isMerged;
            }
            _ButtonMerge.Checked = _IsMerged;
            _ButtonUnMerge.Checked = !_IsMerged;
        }
        /// <summary>
        /// Bude se používat LazyLoad
        /// </summary>
        public bool UseLazyLoad { get { return this._Ribbon.UseLazyContentCreate; } set { this._Ribbon.UseLazyContentCreate = value; } }
        /// <summary>
        /// Naplní Ribbon nějakým počtem stránek a grup
        /// </summary>
        public void FillRibbon()
        {
            FillRibbon(3, 6, 2, 3);
        }
        /// <summary>
        /// Naplní Ribbon daným počtem stránek a grup
        /// </summary>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="clearCurrentContent"></param>
        /// <param name="pageText"></param>
        public void FillRibbon(int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, bool clearCurrentContent = false, string pageText = null)
        {
            int? pageIndex = (pageText != null ? (int?)DxRibbonSample.FindPageIndex(pageText) : (int?)null);
            var items = DxRibbonSample.CreatePages(pageCountMin, pageCountMax, groupCountMin, groupCountMax, 
                CategoryName, CategoryName, CategoryColor, 
                pageIndex);
            _Ribbon.AddPages(items, clearCurrentContent);
        }
        private DxRibbonControl _Ribbon;
        private DxSimpleButton _ButtonClear;
        private DxDropDownButton _ButtonMenu;
        private DxSimpleButton _ButtonFill;
        private DxCheckButton _ButtonMerge;
        private DxCheckButton _ButtonUnMerge;
        private void _RunClear(object sender, EventArgs args) 
        {
            this._Ribbon.Clear();
        }
        private List<IMenuItem> _GetDropDownItems()
        {
            List<IMenuItem> subItems = new List<IMenuItem>();
            subItems.Add(new DataRibbonItem() { ItemId = "ClearRibbon", Text = "Clear ", Image = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "ClearContent", Text = "Clear Content only", Image = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "AddOnDemand", Text = "Add ON DEMAND page", Image = "", ToolTipText = "", ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ItemId = "AddRandom", Text = "Add RANDOM page", Image = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "AddWiki", Text = "Add WIKI page", Image = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "Add7Pages", Text = "Add 7 pages", Image = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "RemoveEmpty", Text = "Remove Empty pages", Image = "", ToolTipText = "", ItemIsFirstInGroup = true });
            return subItems;

            /* TEXTOVÁ VARIANTA:

            string resource1 = "devav/actions/driving.svg";
            string resource2 = "devav/actions/filter.svg";
            string resource3 = "devav/actions/gettingstarted.svg";
            string resource4 = "devav/actions/hide.svg";
            string resource5 = "devav/actions/picture.svg";
            string resource6 = "devav/actions/redo.svg";
            string resource7 = "devav/actions/refresh.svg";
            string resource8 = "devav/actions/remove.svg";

            string subItemsText =
                $"Clear Ribbon•Smaže vše (stránky i obsah)•{resource2}♦" +
                $"Clear Content•Smaže grupy ze stránek (včetně buttonů), ale stránky ponechá•{resource8}♦" +
                $"Add ON DEMAND page•••_♦" +
                $"Add RANDOM page♦" +
                $"Add VZTAHY♦" +
                $"Remove Empty•Odstraní stránky, které nic neobsahují••_"

            */

        }
        private void DropDownButtonClick(object sender, EventArgs e)
        {
            this.FillRibbon(1, 2, 2, 3);
        }
        private void DropDownItemClick(object sender, TEventArgs<IMenuItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "ClearRibbon":
                    this._Ribbon.Clear();
                    break;
                case "ClearContent":
                    this._Ribbon.ClearPageContents();
                    break;
                case "AddOnDemand":
                    this.FillRibbon(1, 1, 2, 3, false, "ON.DEMAND");
                    break;
                case "AddRandom":
                    this.FillRibbon(1, 1, 2, 3, false, "RANDOM");
                    break;
                case "AddWiki":
                    this.FillRibbon(1, 2, 2, 3, false, "WIKI");
                    break;
                case "Add7Pages":
                    this.FillRibbon(5, 8, 4, 8);
                    break;
                case "RemoveEmpty":
                    this._Ribbon.RemoveVoidContainers();
                    break;
            }
        }
        private void _RunFill(object sender, EventArgs args)
        {
            FillRibbon(3, 6, 2, 5, true);
        }
        private void _RunMerge(object sender, EventArgs args)
        {
            IsMerged = true;
            // Obezlička kvůli DevExpress, kde Click akce na CheckedButtonu provede { Checked = !Checked; }
            // Ale my chceme, aby button měl ve výsledku hodnotu _IsMerged, takže mu musíme předem nastavit hodnotu opačnou (on si ji pak sám obrátí na tu požadovanou):
            this._ButtonMerge.Checked = !_IsMerged;        // Takže nyní dáme do buttonu opačnou hodnotu, logika DevExpress ji otočí:
            this._ButtonUnMerge.Checked = !_IsMerged;      // V druhém buttonu se hodnota neotočí (tam jsme neklikli), dáme tam tedy hodnotu požadovanou
        }
        private void _RunUnMerge(object sender, EventArgs args) 
        {
            // Abych nemohl já provést Parent.UnMerge, když v Parentu nejsem já Mergován = to bych UnMergoval cizí Ribbon !!!  :
            if (IsMerged)
                IsMerged = false;
            // Obezlička kvůli DevExpress, kde po Click akci na CheckedButtonu se následně provede { Checked = !Checked; }
            // Ale my chceme, aby button měl ve výsledku hodnotu !_IsMerged, takže mu musíme předem nastavit hodnotu opačnou (on si ji pak sám obrátí na tu požadovanou):
            this._ButtonUnMerge.Checked = _IsMerged;       // Takže nyní dáme do buttonu opačnou hodnotu, logika DevExpress ji otočí:
            this._ButtonMerge.Checked = _IsMerged;         // V druhém buttonu se hodnota neotočí (tam jsme neklikli), dáme tam tedy hodnotu požadovanou
        }
        private void _Ribbon_PageOnDemandLoad(object sender, TEventArgs<IRibbonPage> e)
        {
            ThreadManager.AddAction(_LoadItemsFromServer, e.Item);
        }
        private void _LoadItemsFromServer(object[] args)
        {
            System.Threading.Thread.Sleep(850);

            IRibbonPage ribbonPage = args[0] as IRibbonPage;
            int pageIndex = DxRibbonSample.FindPageIndex(ribbonPage.PageText);
            var pages = DxRibbonSample.CreatePages(1, 1, 4, 8, CategoryName, CategoryName, CategoryColor, pageIndex);
            this._Ribbon.ReFillPages(pages);
        }
        private void _Ribbon_RibbonApplicationButtonClick(object sender, EventArgs e)
        {
        }
        private void _Ribbon_RibbonPageCategoryClick(object sender, TEventArgs<IRibbonCategory> e)
        {
            IRibbonCategory ribbonCategory = e.Item;

            Noris.Clients.Win.Components.DialogArgs dialogArgs = new Noris.Clients.Win.Components.DialogArgs();
            dialogArgs.Title = "Ribbon Category Click";
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.MessageText = $"Uživatel kliknul na záhlaví kategorie <b>{ribbonCategory.CategoryId}</b>, s textem <b>{ribbonCategory.CategoryText}</b>, z Ribbonu <b>{this.Ribbon.DebugName}</b>.";
            dialogArgs.PrepareButtons(System.Windows.Forms.MessageBoxButtons.OK);
            dialogArgs.Owner = this.FindForm();
            Noris.Clients.Win.Components.DialogForm.ShowDialog(dialogArgs);
        }
        private void _Ribbon_RibbonGroupButtonClick(object sender, TEventArgs<IRibbonGroup> e)
        {
            IRibbonGroup ribbonGroup = e.Item;

            Noris.Clients.Win.Components.DialogArgs dialogArgs = new Noris.Clients.Win.Components.DialogArgs();
            dialogArgs.Title = "Ribbon Group Click";
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.MessageText = $"Uživatel kliknul na tlačítko skupiny <b>{ribbonGroup.GroupId}</b>, s textem <b>{ribbonGroup.GroupText}</b>, z Ribbonu <b>{this.Ribbon.DebugName}</b>.";
            dialogArgs.PrepareButtons(System.Windows.Forms.MessageBoxButtons.OK);
            dialogArgs.Owner = this.FindForm();
            Noris.Clients.Win.Components.DialogForm.ShowDialog(dialogArgs);
        }
        private void _Ribbon_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            IRibbonItem iRibbonItem = e.Item;

            if (iRibbonItem.RibbonItemType == RibbonItemType.Menu) return;

            Noris.Clients.Win.Components.DialogArgs dialogArgs = new Noris.Clients.Win.Components.DialogArgs();
            dialogArgs.Title = "Ribbon Item Click";
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.MessageText = $"Uživatel kliknul na prvek <b>{iRibbonItem.ItemType}</b>, s textem <b>{iRibbonItem.Text}</b>, z Ribbonu <b>{this.Ribbon.DebugName}</b>";
            if (iRibbonItem.ParentGroup != null) dialogArgs.MessageText += $",\r\nskupina <b>{iRibbonItem.ParentGroup.GroupText}</b>";
            if (iRibbonItem.ParentGroup?.ParentPage != null) dialogArgs.MessageText += $", stránka <b>{iRibbonItem.ParentGroup.ParentPage.PageText}</b>";
            if (iRibbonItem.ParentGroup?.ParentPage?.Category != null) dialogArgs.MessageText += $", kategorie <b>{iRibbonItem.ParentGroup.ParentPage.Category.CategoryText}</b>";
            dialogArgs.MessageText += ".";
            dialogArgs.PrepareButtons(System.Windows.Forms.MessageBoxButtons.OK);
            dialogArgs.Owner = this.FindForm();
            Noris.Clients.Win.Components.DialogForm.ShowDialog(dialogArgs);
        }
    }
    #endregion
    #region RibbonSample : testovací zdroj dat pro Ribbon
    /// <summary>
    /// RibbonSample : testovací zdroj dat pro Ribbon
    /// </summary>
    public class DxRibbonSample
    {
        /// <summary>
        /// Metoda najde a vrátí index stránky s daným textem
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public static int FindPageIndex(string pageText)
        {
            if (String.IsNullOrEmpty(pageText)) return -1;
            return PageNames.ToList().FindIndex(p => String.Equals(p, pageText));
        }
        /// <summary>
        /// Metoda vytvoří soupis stránek s obsah pro ribbon
        /// </summary>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public static List<IRibbonPage> CreatePages(int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, 
            string categoryId = null, string categoryText = null, System.Drawing.Color? categoryColor = null, 
            int? pageIndex = null)
        {
            List<IRibbonPage> pages = new List<IRibbonPage>();
            _AddPages(pages, pageCountMin, pageCountMax, groupCountMin, groupCountMax, pageIndex, categoryId, categoryText, categoryColor);
            return pages;
        }
        /// <summary>
        /// Metoda vytvoří soupis stránek s obsahem pro ribbon
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <param name="pageIndex"></param>
        public static void CreatePagesTo(List<IRibbonPage> pages, int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, string categoryId = null, string categoryText = null, System.Drawing.Color? categoryColor = null, int? pageIndex = null)
        {
            _AddPages(pages, pageCountMin, pageCountMax, groupCountMin, groupCountMax, pageIndex, categoryId, categoryText, categoryColor);
        }
        /// <summary>
        /// Do pole přidá stránky
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="pageIndex"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        private static void _AddPages(List<IRibbonPage> pages, int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, int? pageIndex, string categoryId = null, string categoryText = null, System.Drawing.Color? categoryColor = null)
        {
            var startTime = DxComponent.LogTimeCurrent;
            if (!categoryColor.HasValue) categoryColor = System.Drawing.Color.DarkViolet;
            _RibbonItemCount = 0;

            int pc = Rand.Next(pageCountMin, pageCountMax + 1);
            for (int p = 0; p < pc; p++)
            {
                DataRibbonPage page = _GetPage(pageIndex, categoryId, categoryText, categoryColor);
                if (page == null) continue;

                pages.Add(page);

                // Pokud NENÍ explicitně daná stránka (pageIndex je null), a my jsme náhodně určili stránku v režimu OnDemandLoad:
                //  Pak do této stránky nebudu dávat nyní žádný viditelný obsah (protože je OnDemand!),
                //  ale jen zajistím, že seznam bude obsahovat právě jeden záznam pro tuto stránku s prvkem typu None:
                bool isFirstOnDemand = (!pageIndex.HasValue && (page.PageContentMode == RibbonContentMode.OnDemandLoadOnce || page.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime));
                if (isFirstOnDemand) continue;

                _AddGroups(page, groupCountMin, groupCountMax);
            }

            DxComponent.LogAddLineTime($"Vygenerováno {_RibbonItemCount} prvků v čase {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Do stránky přidá grupy
        /// </summary>
        /// <param name="page"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        private static void _AddGroups(DataRibbonPage page, int groupCountMin, int groupCountMax)
        {
            int gc = Rand.Next(groupCountMin, groupCountMax + 1);
            for (int g = 0; g < gc; g++)
            {
                DataRibbonGroup group = _GetGroup(page.PageId);
                page.Groups.Add(group);

                _AddItems(group, 1, 6);
            }
        }
        /// <summary>
        /// Do grupy přidá prvky
        /// </summary>
        /// <param name="group"></param>
        /// <param name="itemCountMin"></param>
        /// <param name="itemCountMax"></param>
        private static void _AddItems(DataRibbonGroup group, int itemCountMin, int itemCountMax)
        {
            bool containsRadioGroup = false;
            int remainingRadioCount = 0;
            bool forceFirstInGroup = false;
            int ic = Rand.Next(itemCountMin, itemCountMax + 1);
            for (int i = 0; i < ic; i++)
            {
                DataRibbonItem item = _GetItem(group.GroupId, ref containsRadioGroup, ref remainingRadioCount, ref forceFirstInGroup);
                group.Items.Add(item);
                if (remainingRadioCount > 0 && i == (ic - 1))   // Dokud zrovna generuji RadioGrupu (mám remainingRadioCount kladné) a blížím se ke konci počtu našich prvků,
                    ic++;                                       //   pak přidám ještě další prvek, abych RadioGrupu dotáhl do konce.
            }
        }
        /// <summary>
        /// Pro daný titulek stránky určí, zda jde nebo nejde o kategorii, a vrací null nebo instanci kategorie
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <returns></returns>
        private static DataRibbonCategory _GetCategory(string pageText, string categoryId, string categoryText, System.Drawing.Color? categoryColor)
        {
            bool isCategory = (pageText == "VZTAHY" || pageText == "MODIFIKACE");
            if (!isCategory) return null;

            if (String.IsNullOrEmpty(categoryId)) categoryId = "Extend1";
            if (String.IsNullOrEmpty(categoryText)) categoryText = "DALŠÍ VOLBY";

            return new DataRibbonCategory()
            {
                CategoryId = categoryId,
                CategoryText = categoryText,
                CategoryColor = categoryColor ?? System.Drawing.Color.PaleVioletRed,
                CategoryVisible = true
            };
        }
        /// <summary>
        /// Vytvoří a vrátí stránku Ribbonu, podle potřeby ji zařadí do patřičné kategorie. Bez Groups.
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <returns></returns>
        private static DataRibbonPage _GetPage(int? pageIndex, string categoryId, string categoryText, System.Drawing.Color? categoryColor)
        {
            int pageTotal = PageNames.Length;
            if (pageIndex.HasValue && (pageIndex.Value < 0 || pageIndex.Value >= pageTotal)) return null;
            int pi = pageIndex ?? Rand.Next(pageTotal);
            if (pi < 0 || pi >= pageTotal) throw new ArgumentException($"Požadovaný index stránky {pi} je mimo rozsah 0 až {pageTotal}");

            string pageText = PageNames[pi];
            IRibbonCategory category = _GetCategory(pageText, categoryId, categoryText, categoryColor);
            RibbonContentMode contentMode = (pageText == "ON.DEMAND" ? RibbonContentMode.OnDemandLoadOnce :
                                            (pageText == "RANDOM" ? RibbonContentMode.OnDemandLoadEveryTime : RibbonContentMode.Static));

            return new DataRibbonPage()
            {
                Category = category,
                PageId = "Page" + pi,
                PageText = pageText,
                PageOrder = pi + 1,
                PageContentMode = contentMode,
                PageType = RibbonPageType.Default
            };
        }
        /// <summary>
        /// Vytvoří a vrátí grupu Ribbonu (bez Items)
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        private static DataRibbonGroup _GetGroup(string pageId)
        {
            int groupTotal = GroupNames.Length;
            int gi = Rand.Next(groupTotal);
            string groupId = pageId + "." + "Group" + gi;    // GroupId je shodné pro grupy konkrétního názvu na shodné stránce = pro Mergování!
            string groupText = GroupNames[gi];
            bool groupButtonVisible = (groupText == "Rozšířené" || groupText == "Údržba" || groupText == "Oblíbené" || groupText == "Systém" || groupText == "Systém");

            return new DataRibbonGroup()
            {
                GroupId = groupId,
                GroupText = groupText,
                GroupButtonVisible = groupButtonVisible
            };
        }
        /// <summary>
        /// Vytvoří a vrátí prvek Ribbonu. Prvek podle potřeby obsahuje i SubItems.
        /// Další parametry řídí tvorbu RadioGrupy.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="containsRadioGroup"></param>
        /// <param name="remainingRadioCount"></param>
        /// <param name="forceFirstInGroup"></param>
        /// <returns></returns>
        private static DataRibbonItem _GetItem(string groupId, ref bool containsRadioGroup, ref int remainingRadioCount, ref bool forceFirstInGroup)
        {
            _RibbonItemCount++;
            string itemId = "Item" + (++_RibbonItemId);
            string itemText = Random.GetWord(true);
            string itemImageName = GetRandomImageName();
            string toolTip = Random.GetSentence(Rand.Next(5, 16));
            string toolTipTitle = Random.GetSentence(Rand.Next(1, 3));
            bool isFirst = (remainingRadioCount == 0 ? (forceFirstInGroup || (Rand.Next(10) < 3)) : false);          // Pokud nyní připravuji Radio, pak nedávám IsFirst !
            int? toolbarOrder = ((Rand.Next(100) < 3) ? (int?)Rand.Next(1, 101) : null);

            DataRibbonItem item = new DataRibbonItem()
            {
                ItemId = itemId,
                Text = itemText,
                Image = itemImageName,
                RibbonStyle = RibbonItemStyles.All,
                ToolTipText = toolTip,
                ToolTipTitle = toolTipTitle,
                ToolTipIcon = "help_hint_48_"
            };

            if (remainingRadioCount > 0)
            {   // Pokračujeme v přípravě skupiny RadioButtonů:
                item.RibbonItemType = RibbonItemType.RadioItem;
                item.RibbonStyle = RibbonItemStyles.SmallWithText;
                isFirst = false;
                toolbarOrder = null;                                      // RadioButtony nedávám do Toolbaru
                remainingRadioCount--;
                if (remainingRadioCount == 0) forceFirstInGroup = true;   // Dokončili jsme počet RadioButtonů: příští prvek bude ForceFirst!
            }
            else
            {
                RibbonItemType itemType = GetRandomItemType();
                if (itemType == RibbonItemType.RadioItem && containsRadioGroup) 
                    itemType = RibbonItemType.CheckBoxStandard;           // V jedné grupě Ribbonu bude nanejvýše jedna RadioButton grupa

                item.RibbonItemType = itemType;

                if (itemType == RibbonItemType.RadioItem)                 // Zde začíná RadioButton grupa
                {
                    isFirst = true;                                       // První RadioItem si zahajuje svoji sub-grupu
                    toolbarOrder = null;                                  // RadioButtony nedávám do Toolbaru
                    item.RibbonStyle = RibbonItemStyles.SmallWithText;    // RadioItem je vždy Small
                    remainingRadioCount = Rand.Next(3, 6);                // RadioItemů do jedné grupy dám 3 - 5 za sebou
                    containsRadioGroup = true;                            // RibbonGroup již obsahuje RadioGrupu, víc RadioSkupin tam dávat už nebudu
                }

                if (item.RibbonItemType == RibbonItemType.CheckBoxStandard || item.RibbonItemType == RibbonItemType.RadioItem)
                {
                    if (Rand.Next(100) < 15) item.Image = null;
                    if (Rand.Next(100) < 50) item.Checked = true;
                }

                if (Rand.Next(10) < 3)
                {
                    item.RibbonStyle = RibbonItemStyles.SmallWithText;
                }

                if (NeedSubItem(itemType))
                    item.SubRibbonItems = _CreateSubItems(itemType, 4, 15);
            }

            item.ToolTipTitle = item.ToolTipTitle + "  {" + item.ItemType.ToString() + "}";
            item.ItemIsFirstInGroup = isFirst;
            item.ItemToolbarOrder = toolbarOrder;

            return item;
        }
        /// <summary>
        /// Vytvoří a vrátí pole SubItems, možná i rekurzivně
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="subItemsCountMin"></param>
        /// <param name="subItemsCountMax"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        protected static List<IRibbonItem> _CreateSubItems(RibbonItemType itemType, int subItemsCountMin, int subItemsCountMax, int level = 0)
        {
            List<IRibbonItem> subItems = new List<IRibbonItem>();

            int sc = Rand.Next(subItemsCountMin, subItemsCountMax + 1);
            for (int i = 0; i < sc; i++)
            {
                _RibbonItemCount++;
                string itemId = "Item" + (++_RibbonItemId);
                string itemText = Random.GetWord(true);
                string itemImage = GetRandomImageName(33);
                string toolTip = Random.GetSentence(Rand.Next(5, 16));
                string toolTipTitle = Random.GetSentence(Rand.Next(1, 3));
                bool isFirst = (Rand.Next(10) < 3);

                DataRibbonItem item = new DataRibbonItem()
                {
                    ItemId = itemId,
                    Text = itemText,
                    ItemIsFirstInGroup = isFirst,
                    RibbonStyle = RibbonItemStyles.Default,
                    ToolTipText = toolTip,
                    ToolTipTitle = toolTipTitle,
                    ToolTipIcon = "help_hint_48_",
                    Image = itemImage
                };
          
                item.RibbonItemType = (itemType == RibbonItemType.InRibbonGallery ? RibbonItemType.Button : GetRandomSubItemType());

                int nextLevel = level + 1;
                if (NeedSubItem(item.RibbonItemType, nextLevel))
                {
                    if (level <= 4)
                        item.SubRibbonItems = _CreateSubItems(itemType, 3, 7, nextLevel);
                    else
                        item.RibbonItemType = RibbonItemType.Button;
                }

                if (item.RibbonItemType == RibbonItemType.CheckBoxStandard || item.RibbonItemType == RibbonItemType.RadioItem)
                {
                    if (Rand.Next(100) < 65) item.Image = null;
                    if (Rand.Next(100) < 50) item.Checked = true;
                }

                item.ToolTipTitle = item.ToolTipTitle + "  {" + item.ItemType.ToString() + "}";

                subItems.Add(item);
            }

            return subItems;
        }
        /// <summary>
        /// Vrátí náhodný typ prvku v Ribbonu
        /// </summary>
        /// <returns></returns>
        public static RibbonItemType GetRandomItemType()
        {
            int rand = Rand.Next(100);
            if (rand < 60) return RibbonItemType.Button;
            if (rand < 67) return RibbonItemType.CheckBoxStandard;
            if (rand < 74) return RibbonItemType.CheckBoxToggle;
            if (rand < 80) return RibbonItemType.RadioItem;
            // if (rand < 85) return RibbonItemType.ButtonGroup;         nějak se mi nelíbí
            if (rand < 85) return RibbonItemType.InRibbonGallery;
            if (rand < 90) return RibbonItemType.SplitButton;
            if (rand < 100) return RibbonItemType.Menu;
            return RibbonItemType.Button;
        }
        /// <summary>
        /// Vrátí náhodný typ prvku v SubItems
        /// </summary>
        /// <returns></returns>
        public static RibbonItemType GetRandomSubItemType()
        {
            int rand = Rand.Next(100);
            if (rand < 60) return RibbonItemType.Button;
            if (rand < 67) return RibbonItemType.CheckBoxStandard;
            if (rand < 74) return RibbonItemType.CheckBoxToggle;
            if (rand < 90) return RibbonItemType.SplitButton;
            if (rand < 100) return RibbonItemType.Menu;
            return RibbonItemType.Button;
        }
        /// <summary>
        /// Potřebuje daný typ nějaké SubItems?
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static bool NeedSubItem(RibbonItemType itemType, int level = 0)
        {
            if (itemType == RibbonItemType.InRibbonGallery && level == 0) return true;
            bool canSubItem = (itemType == RibbonItemType.ButtonGroup || itemType == RibbonItemType.SplitButton || itemType == RibbonItemType.Menu);
            if (canSubItem && (level == 0 || Rand.Next(100) < 40)) return true;
            return false;
        }
        /// <summary>
        /// Vrátí jméno náhodného obrázku
        /// </summary>
        /// <param name="randomEmpty"></param>
        /// <returns></returns>
        public static string GetRandomImageName(int randomEmpty = 0)
        {
            if ((randomEmpty > 0) && (Rand.Next(100) < randomEmpty)) return null;
            return ResourceImages[Rand.Next(ResourceImages.Length)];
        }
        /// <summary>
        /// Konstantní pole se jmény stránek
        /// </summary>
        public static string[] PageNames { get { if (_PageNames is null) _PageNames = "ASTROLOGIE;PŘÍRODA;TECHNIKA;VOLNÝ ČAS;ON.DEMAND;RANDOM;LITERATURA;VZTAHY;MODIFIKACE;WIKI".Split(';'); return _PageNames; } }
        private static string[] _PageNames;
        /// <summary>
        /// Konstantní pole se jmény skupin
        /// </summary>
        public static string[] GroupNames { get { if (_GroupNames is null) _GroupNames = "Základní;Rozšířené;Údržba;Oblíbené;Systém;Grafy;Archivace;Expertní funkce;Tisky;Další vlastnosti".Split(';'); return _GroupNames; } }
        private static string[] _GroupNames;
        /// <summary>
        /// Konstantní pole se jmény obrázků
        /// </summary>
        public static string[] ResourceImages { get { if (_ResourceImages is null) _ResourceImages = _GetResourceImages(); return _ResourceImages; } }
        private static string[] _ResourceImages;
        /// <summary>
        /// Vrací seznam Images v Properties.Resources
        /// </summary>
        /// <returns></returns>
        private static string[] _GetResourceImages()
        {
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
        /// <summary>
        /// Random
        /// </summary>
        public static System.Random Rand { get { if (_Rand is null) _Rand = new System.Random(); return _Rand; } }
        private static System.Random _Rand;
    }
    #endregion
}
