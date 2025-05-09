﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using Noris.Clients.Win.Components.AsolDX;
using TestDevExpress.Components;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy Ribbonů
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "Ribbon", buttonOrder: 40, buttonImage: "svgimages/dashboards/grid.svg", buttonToolTip: "Otevře okno s ukázkou Ribbonů s možností mergování nahoru")]
    public class RibbonForm : DxRibbonForm
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public RibbonForm()
        {
            this.ImageName = "svgimages/dashboards/grid.svg";
            this.ImageNameAdd = "@text|R|#660022||B|3|#FF88AA|#FFCCEE";

            var moon10 = DxComponent.CreateBitmapImage("Images/Moon10.png");
            DxComponent.SplashShow("Testovací okno Ribbonů Nephrite", "DJ soft & ASOL",
                "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                this, moon10, opacityColor: System.Drawing.Color.FromArgb(80, 80, 180), opacity: 120,
                useFadeOut: false);

            DxQuickAccessToolbar.ConfigValueChanged += DxQuickAccessToolbar_QATItemKeysChanged;

            this.InitializeForm();
            _SetUseLazyLoad(__UseLazyContentCreate);

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
        }
        protected void InitializeForm()
        {
            this.Size = new System.Drawing.Size(800, 600);

            this.Text = $"Test Ribbons :: {DxComponent.FrameworkName}";
            this.ImageName = "svgimages/dashboards/scatterchart.svg";

            this.AllowMdiBar = true;

            _DxMainSplit = DxComponent.CreateDxSplitContainer(DxMainPanel, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, splitPosition: 400, showSplitGlyph: true);

            _DxLeftSplit = DxComponent.CreateDxSplitContainer(_DxMainSplit.Panel1, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Horizontal,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel1, splitPosition: 400, showSplitGlyph: true);

            _DxBottomSplit = DxComponent.CreateDxSplitContainer(_DxLeftSplit.Panel2, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.None, splitPosition: 650, showSplitGlyph: true);

            _DxLogMemoEdit = DxComponent.CreateDxMemoEdit(_DxMainSplit.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);

            _TestPanel1 = new RibbonTestPanel();
            _TestPanel1.UseLazyContentCreate = this.UseLazyContentCreate;
            _TestPanel1.Ribbon.DebugName = "Slave 1";
            //     _TestPanel1.Ribbon.ImageRightFull = DxComponent.CreateBitmapImage("Images/ImagesBig/Homer 01b.png");
            //     _TestPanel1.Ribbon.ImageRightMini = DxComponent.CreateBitmapImage("Images/ImagesBig/Homer 01c.png");
            _TestPanel1.ParentRibbon = DxRibbon;
            _TestPanel1.PageMergeOrder = 100;
            _TestPanel1.CategoryName = "SKUPINA 1";
            _TestPanel1.CategoryColor = System.Drawing.Color.FromArgb(100, System.Drawing.Color.LightBlue);
            _TestPanel1.FillRibbon();
            _DxLeftSplit.Panel1.Controls.Add(_TestPanel1);

            _TestPanel2a = new RibbonTestPanel();
            _TestPanel2a.UseLazyContentCreate = this.UseLazyContentCreate;
            _TestPanel2a.Ribbon.DebugName = "Slave 2A";
            //     _TestPanel2a.Ribbon.ImageRightFull = DxComponent.CreateBitmapImage("Images/ImagesBig/Lisa 01b.png");
            //     _TestPanel2a.Ribbon.ImageRightMini = DxComponent.CreateBitmapImage("Images/ImagesBig/Lisa 01c.png");
            _TestPanel2a.ParentRibbon = _TestPanel1.Ribbon;
            _TestPanel2a.PageMergeOrder = 200;
            _TestPanel2a.CategoryName = "SKUPINA 2A";
            _TestPanel2a.CategoryColor = null;   // System.Drawing.Color.FromArgb(64, System.Drawing.Color.LightYellow);
            _TestPanel2a.FillRibbon();
            _DxBottomSplit.Panel1.Controls.Add(_TestPanel2a);

            _TestPanel2b = new RibbonTestPanel();
            _TestPanel2b.UseLazyContentCreate = this.UseLazyContentCreate;
            _TestPanel2b.Ribbon.DebugName = "Slave 2B";
            //     _TestPanel2b.Ribbon.ImageRightFull = DxComponent.CreateBitmapImage("Images/ImagesBig/Marge 01b.png");
            //     _TestPanel2b.Ribbon.ImageRightMini = DxComponent.CreateBitmapImage("Images/ImagesBig/Marge 01c.png");
            _TestPanel2b.ParentRibbon = _TestPanel1.Ribbon;
            _TestPanel2b.PageMergeOrder = 300;
            _TestPanel2b.CategoryName = "SKUPINA 2B";
            _TestPanel2b.CategoryColor = System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightGreen);
            _TestPanel2b.FillRibbon();
            _DxBottomSplit.Panel2.Controls.Add(_TestPanel2b);
            
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DxComponent.LogTextChanged += DxComponent_LogTextChanged;
            _LogContainChanges = true;
        }
        /// <summary>
        /// Po jakékoli změně obsahu QAT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxQuickAccessToolbar_QATItemKeysChanged(object sender, EventArgs e)
        {
            string line = "Nový obsah QAT: " + DxQuickAccessToolbar.ConfigValue;
            DxComponent.LogAddLine(LogActivityKind.Ribbon, line);
        }

        private DxSplitContainerControl _DxMainSplit;
        private DxSplitContainerControl _DxLeftSplit;
        private DxSplitContainerControl _DxBottomSplit;
        private RibbonTestPanel _TestPanel1;
        private RibbonTestPanel _TestPanel2a;
        private RibbonTestPanel _TestPanel2b;
        private DxMemoEdit _DxLogMemoEdit;
        private DevExpress.XtraBars.BarStaticItem CreateStatusBarItem(float? fontSizeRatio = null)
        {
            DevExpress.XtraBars.BarStaticItem item = new DevExpress.XtraBars.BarStaticItem();
            item.MinWidth = 240;
            item.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            if (fontSizeRatio.HasValue)
                item.Appearance.FontSizeDelta = DxComponent.GetFontSizeDelta(fontSizeRatio);
            item.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            return item;
        }
        
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        #endregion
        #region Ribbon a StatusBar - obsah a rozcestník
        /// <summary>
        /// Připraví obsah Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.UseLazyContentCreate = DxRibbonControl.LazyContentMode.Auto;

            this.DxRibbon.DebugName = "MainRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this.DxRibbon.ApplicationButtonText = " SYSTEM ";
            this.DxRibbon.LogActive = true;

            //     this.DxRibbon.ImageRightFull = DxComponent.CreateBitmapImage("Images/ImagesBig/Bart 01bt.png");
            //     this.DxRibbon.ImageRightMini = DxComponent.CreateBitmapImage("Images/ImagesBig/Bart 01c.png");

            string imgLogClear = "svgimages/snap/cleartablestyle.svg";
            string imgInfo = "svgimages/xaf/action_aboutinfo.svg";

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonGroup group;

            DataRibbonPage homePage = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);
            pages.Add(homePage);

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "RIBBON TEST" };
            homePage.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.UseLazyInit", Text = "Use Lazy Init", ToolTipText = "Zaškrtnuto: používat opožděné plnění stránek Ribbonu (=až bude potřeba)\r\nNezaškrtnuto: fyzicky naplní celý Ribbon okamžitě, delší čas přípravy okna", ItemType = RibbonItemType.CheckButton, Checked = (UseLazyContentCreate == DxRibbonControl.LazyContentMode.Auto), RibbonStyle = RibbonItemStyles.Large });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.LogClear", Text = "Clear log", ToolTipText = "Smaže obsah logu vpravo", ImageName = imgLogClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.ShowTextInQat", Text = "Show Text in QAT", ToolTipText = "Aktivuje / Deaktivuje text u prvků QAT", ItemType = RibbonItemType.CheckButton, Checked = this.ShowTextInQAT, RibbonStyle = RibbonItemStyles.Large });

            DataRibbonPage helpPage = new DataRibbonPage() { PageId = "HELP", PageText = "Nápověda", MergeOrder = 9999, PageOrder = 9999 };
            pages.Add(helpPage);
            group = new DataRibbonGroup() { GroupId = "help", GroupText = "NÁPOVĚDA" };
            helpPage.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Help.Help.Show", Text = "Nápověda", ToolTipText = "Zobrazí okno s nápovědou", ImageName = imgInfo });

            _RibbonTestsInit(pages, homePage);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.UseLazyInit":
                    UseLazyContentCreate = ((e.Item.Checked ?? false) ? DxRibbonControl.LazyContentMode.Auto : DxRibbonControl.LazyContentMode.CreateAllItems);           // Do ribbonů hodnotu vepíše set metoda.
                    break;
                case "Dx.Test.ImgPick":
                    ImagePickerForm.ShowForm(this);
                    break;
                case "Dx.Test.LogClear":
                    DxComponent.LogClear();
                    break;
                case "Dx.ShowTextInQat":
                    ShowTextInQAT = e.Item.Checked ?? false;         // Do ribbonů hodnotu vepíše set metoda.
                    break;

            }
        }
        /// <summary>
        /// Bude se používat LazyLoad?
        /// </summary>
        public DxRibbonControl.LazyContentMode UseLazyContentCreate { get { return __UseLazyContentCreate; } set { _SetUseLazyLoad(value); } }
        private DxRibbonControl.LazyContentMode __UseLazyContentCreate;
        /// <summary>
        /// Zobrazovat v Toolbaru u tlačítek i text?
        /// </summary>
        public bool? ShowTextInQAT
        {
            get { return DxRibbonControl.DefaultShowTextInQAT; }
            set
            {
                DxRibbonControl.DefaultShowTextInQAT = value;
                var dxRibbons = DxComponent.GetListeners<DxRibbonControl>();
                foreach (var dxRibbon in dxRibbons)
                    dxRibbon.ShowTextInQAT = value;
            }
        }
        /// <summary>
        /// Nastaví hodnotu <see cref="UseLazyContentCreate"/> a vepíše ji do ribbonů. Neřeší ale hodnotu v CheckBoxu v Ribbonu.
        /// </summary>
        /// <param name="useLazyContentCreate"></param>
        private void _SetUseLazyLoad(DxRibbonControl.LazyContentMode useLazyContentCreate)
        {
            __UseLazyContentCreate = useLazyContentCreate;

            if (this.DxRibbon != null) this.DxRibbon.UseLazyContentCreate = useLazyContentCreate;
            if (this._TestPanel1 != null) this._TestPanel1.UseLazyContentCreate = useLazyContentCreate;
            if (this._TestPanel2a != null) this._TestPanel2a.UseLazyContentCreate = useLazyContentCreate;
            if (this._TestPanel2b != null) this._TestPanel2b.UseLazyContentCreate = useLazyContentCreate;
        }
        /// <summary>
        /// Připraví obsah StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this._StatusItemTitle = CreateStatusBarItem();

            // V tomto pořadí budou StatusItemy viditelné (tady je zatím jen jeden):
            this.DxStatusBar.ItemLinks.Add(this._StatusItemTitle);
        }
        #endregion
        #region Řízené testy Ribbonu
        /// <summary>
        /// Do Ribbonu přidá definici pro testovací prvky
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="homePage"></param>
        private void _RibbonTestsInit(List<DataRibbonPage> pages, DataRibbonPage homePage)
        {
            _RibbonAutoHideGroupInit(pages, homePage);
            _RibbonSpecificTestsInit(pages, homePage);
        }
        #region Testy viditelnosti grupy podle obsahu tlačítek, testy refreshe obsahu grupy
        /// <summary>
        /// Do Ribbonu přidá definici pro testovací prvky
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="homePage"></param>
        private void _RibbonAutoHideGroupInit(List<DataRibbonPage> pages, DataRibbonPage homePage)
        {
            DataRibbonPage testPage = new DataRibbonPage() { PageId = "GroupTests", PageText = "Test AutoHide grupy", MergeOrder = 200, PageOrder = 200 };
            pages.Add(testPage);
            _TestSampleImages = new string[]
{
    "svgimages/spreadsheet/create%20rotated%20bar%20chart.svg",
    "svgimages/spreadsheet/createarea3dchart.svg",
    "svgimages/spreadsheet/createareachart.svg",
    "svgimages/spreadsheet/createbar3dchart.svg",
    "svgimages/spreadsheet/createbarchart.svg",
    "svgimages/spreadsheet/createbubble3dchart.svg",
    "svgimages/spreadsheet/createbubblechart.svg",
    "svgimages/spreadsheet/createconebar3dchart.svg",
    "svgimages/spreadsheet/createconefullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createconemanhattanbarchart.svg",
    "svgimages/spreadsheet/createconestackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylindermanhattanbarchart.svg",
    "svgimages/spreadsheet/createcylinderstackedbar3dchart.svg",
    "svgimages/spreadsheet/createdoughnutchart.svg",
    "svgimages/spreadsheet/createexplodeddoughnutchart.svg",
    "svgimages/spreadsheet/createexplodedpie3dchart.svg",
    "svgimages/spreadsheet/createexplodedpiechart.svg",
    "svgimages/spreadsheet/createfromselection.svg",
    "svgimages/spreadsheet/createfullstackedarea3dchart.svg",
    "svgimages/spreadsheet/createfullstackedareachart.svg",
    "svgimages/spreadsheet/createfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createfullstackedbarchart.svg",
    "svgimages/spreadsheet/createfullstackedlinechart.svg",
    "svgimages/spreadsheet/createfullstackedlinechartnomarkers.svg",
    "svgimages/spreadsheet/createline3dchart.svg",
    "svgimages/spreadsheet/createlinechart.svg",
    "svgimages/spreadsheet/createlinechartnomarkers.svg",
    "svgimages/spreadsheet/createmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpie3dchart.svg",
    "svgimages/spreadsheet/createpiechart.svg",
    "svgimages/spreadsheet/createpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpyramidstackedbar3dchart.svg",
    "svgimages/spreadsheet/createradarlinechart.svg",
    "svgimages/spreadsheet/createradarlinechartfilled.svg",
    "svgimages/spreadsheet/createradarlinechartnomarkers.svg",
    "svgimages/spreadsheet/createrotatedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedbarchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedbarchart.svg",
    "svgimages/spreadsheet/createrotatedstackedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createscatterchartlines.svg",
    "svgimages/spreadsheet/createscatterchartlinesandmarkers.svg",
    "svgimages/spreadsheet/createscatterchartsmoothlines.svg",
    "svgimages/spreadsheet/createscatterchartsmoothlinesandmarkers.svg",
    "svgimages/spreadsheet/createstackedarea3dchart.svg",
    "svgimages/spreadsheet/createstackedareachart.svg",
    "svgimages/spreadsheet/createstackedbar3dchart.svg",
    "svgimages/spreadsheet/createstackedbarchart.svg",
    "svgimages/spreadsheet/createstackedlinechart.svg",
    "svgimages/spreadsheet/createstackedlinechartnomarkers.svg",
    "svgimages/spreadsheet/createstockcharthighlowclose.svg",
    "svgimages/spreadsheet/createstockchartopenhighlowclose.svg",
    "svgimages/spreadsheet/createstockchartvolumehighlowclose.svg",
    "svgimages/spreadsheet/createstockchartvolumeopenhighlowclose.svg"
};
            _TestItemId = 0;
            _TestSampleHideEmptyGroup = false;

            _TestControlGroup = new DataRibbonGroup() { GroupText = "OVLÁDÁNÍ TESTU" };
            _TestControlGroup.Items.Add(new DataRibbonItem() { ItemId = "TestAdd", Text = "Přidat", ToolTipText = "Do testovací grupy přidá tlačítko", ImageName = "devav/actions/add.svg", ClickAction = _RibbonTestActionAdd });
            _TestControlGroup.Items.Add(new DataRibbonItem() { ItemId = "TestRemove", Text = "Odebrat", ToolTipText = "Z testovací grupy odebere tlačítko", ImageName = "devav/actions/remove.svg", ClickAction = _RibbonTestActionRemove });
            _TestControlGroup.Items.Add(new DataRibbonItem() { ItemId = "TestHideAny", Text = "Skrýt", ToolTipText = "Nastaví Visible = false pro některý z viditelných prvků", ImageName = "svgimages/icon%20builder/actions_remove.svg", ClickAction = _RibbonTestActionHideAny });
            _TestControlGroup.Items.Add(new DataRibbonItem() { ItemId = "TestShowAll", Text = "Zobrazit", ToolTipText = "Nastaví Visible = true pro všechny neviditelné prvky", ImageName = "svgimages/icon%20builder/actions_add.svg", ClickAction = _RibbonTestActionShowAll });
            _TestControlGroup.Items.Add(new DataRibbonItem() { ItemId = "TestExchange", Text = "Vyměnit", ToolTipText = "Pošle nová data pro testovací grupu", ImageName = "svgimages/xaf/action_refresh.svg", ClickAction = _RibbonTestActionExchange });
            _TestControlGroup.Items.Add(new DataRibbonItem() { ItemId = "TestAutoHide", Text = "Skrýt prázdnou", ToolTipText = "Pokud bude aktivováno, pak prázdná grupa bude skryta.",
                ItemType = RibbonItemType.CheckButton, ImageName = _ImageVisibilityNull, ImageNameChecked = _ImageVisibilityHide, ImageNameUnChecked = _ImageVisibilityShow,
                Checked = null, ClickAction = _RibbonTestActionAutoHide
            });
            testPage.Groups.Add(_TestControlGroup);

            _TestSampleGroup = _RibbonTestCreateSampleGroup();
            _RibbonTestActionAddMore(_TestSampleGroup, Randomizer.Rand.Next(3));
            testPage.Groups.Add(_TestSampleGroup);

            string radioGroup1 = "Směr odchodu";
            string radioGroup2 = "Směr úprku";
            _TestControlAfterGroup = new DataRibbonGroup() { GroupText = "NÁSLEDUJÍCÍ GRUPA" };
            _TestControlAfterGroup.Items.Add(new DataRibbonItem() { ItemId = "After1", Text = "Tlačítko 1", ToolTipText = "Toto je zbytečné tlačítko", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = radioGroup2, ImageName = "devav/arrows/left2.svg" });
            _TestControlAfterGroup.Items.Add(new DataRibbonItem() { ItemId = "After2", Text = "Tlačítko 2", ToolTipText = "Toto je zbytečné tlačítko", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = radioGroup1, ImageName = "devav/arrows/left.svg" });
            _TestControlAfterGroup.Items.Add(new DataRibbonItem() { ItemId = "After3", Text = "Tlačítko 3", ToolTipText = "Toto je zbytečné tlačítko", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = radioGroup1, Checked = true, ImageName = "devav/arrows/forward.svg" });
            _TestControlAfterGroup.Items.Add(new DataRibbonItem() { ItemId = "After4", Text = "Tlačítko 4", ToolTipText = "Toto je zbytečné tlačítko", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = radioGroup1, ImageName = "devav/arrows/right2.svg" });
            _TestControlAfterGroup.Items.Add(new DataRibbonItem() { ItemId = "After5", Text = "Tlačítko 5", ToolTipText = "Toto je zbytečné tlačítko", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = radioGroup2, ImageName = "devav/arrows/right.svg" });
            testPage.Groups.Add(_TestControlAfterGroup);
        }
        private void _RibbonTestActionAdd(IMenuItem item)
        {
            DataRibbonGroup sampleGroup = _TestSampleGroup;
            _RibbonTestActionAddOne(sampleGroup);
            this.DxRibbon.RefreshGroup(sampleGroup);
        }
        private void _RibbonTestActionRemove(IMenuItem item)
        {
            _RibbonTestActionOne(null, TestActionType.RemoveRandom);
        }
        private void _RibbonTestActionHideAny(IMenuItem item)
        {
            _RibbonTestActionOne(null, TestActionType.HideRandom);
        }
        private void _RibbonTestActionShowAll(IMenuItem item)
        {
            DataRibbonGroup sampleGroup = _TestSampleGroup;
            foreach (var iItem in sampleGroup.Items)
            {
                if (!iItem.Visible)
                    ((DataRibbonItem)iItem).Visible = true;
            }
            sampleGroup.ChangeMode = ContentChangeMode.Add;
            this.DxRibbon.RefreshGroup(sampleGroup);
        }
        private void _RibbonTestActionExchange(IMenuItem item)
        {
            int count = _TestSampleGroup.Items.Count;
            DataRibbonGroup sampleGroup = _RibbonTestCreateSampleGroup();
            _RibbonTestActionAddMore(sampleGroup, count);
            sampleGroup.ChangeMode = ContentChangeMode.ReFill;
            this.DxRibbon.RefreshGroup(sampleGroup);
            _TestSampleGroup = sampleGroup;
        }
        private void _RibbonTestActionAutoHide(IMenuItem item)
        {
            bool isChecked = item.Checked ?? false;
            _TestSampleHideEmptyGroup = isChecked;
            DataRibbonGroup sampleGroup = _TestSampleGroup;
            sampleGroup.HideEmptyGroup = _TestSampleHideEmptyGroup;
            this.DxRibbon.RefreshGroup(sampleGroup);
        }
        private static string _ImageVisibilityNull { get { return "images/xaf/templatesv2images/state_itemvisibility_show.svg"; } }
        private static string _ImageVisibilityShow { get { return "svgimages/xaf/state_itemvisibility_show.svg"; } }
        private static string _ImageVisibilityHide { get { return "svgimages/xaf/state_itemvisibility_hide.svg"; } }
        private DataRibbonGroup _RibbonTestCreateSampleGroup()
        {
            var sampleGroup = new DataRibbonGroup() { GroupId = "TestSampleGroup", GroupText = "TESTOVACÍ GRUPA", HideEmptyGroup = _TestSampleHideEmptyGroup };
            return sampleGroup;
        }
        private void _RibbonTestClickTestItem(IMenuItem item)
        {
            _RibbonTestActionOne(item, TestActionType.HideThis);
        }
        private void _RibbonTestActionAddMore(DataRibbonGroup sampleGroup, int count)
        {
            for (int i = 0; i < count; i++)
                _RibbonTestActionAddOne(sampleGroup);
        }
        private void _RibbonTestActionAddOne(DataRibbonGroup sampleGroup)
        {
            int count = sampleGroup.Items.Count;
            int id = ++_TestItemId;
            var image = Randomizer.GetItem(_TestSampleImages);
            var item = new DataRibbonItem() { ItemId = "Test" + id.ToString(), Text = "Akce " + id.ToString(), ToolTipText = "Toto je testovací tlačítko.\r\nKdyž na něj klikneš, tlačítko se z grupy vyhodí.", ImageName = image, ClickAction = _RibbonTestClickTestItem };
            sampleGroup.Items.Add(item);
            sampleGroup.ChangeMode = ContentChangeMode.Add;
        }
        private void _RibbonTestActionOne(IMenuItem item, TestActionType actionType)
        {
            DataRibbonGroup sampleGroup = _TestSampleGroup;
            int count = sampleGroup.Items.Count;
            if (count > 0)
            {
                // Najdeme index prvku, který máme zpracovat:
                int index = 0;
                switch (actionType)
                {
                    case TestActionType.RemoveRandom:
                    case TestActionType.HideRandom:
                        // Remove i Hide se týká jen těch viditelných prvků:
                        var hideItem = Randomizer.GetItem(sampleGroup.Items.Where(i => i.Visible).ToArray());
                        if (hideItem != null)
                            index = sampleGroup.Items.FindIndex(i => Object.ReferenceEquals(i, hideItem));
                        break;
                    case TestActionType.RemoveThis:
                    case TestActionType.HideThis:
                        // This se týká zadaného prvku:
                        if (item != null)
                            index = sampleGroup.Items.FindIndex(i => Object.ReferenceEquals(i, item));
                        break;
                }

                // Máme nalezený prvek?
                if (index >= 0)
                {
                    switch (actionType)
                    {
                        case TestActionType.RemoveRandom:
                        case TestActionType.RemoveThis:
                            sampleGroup.Items.RemoveAt(index);
                            sampleGroup.ChangeMode = ContentChangeMode.ReFill;
                            break;
                        case TestActionType.HideRandom:
                        case TestActionType.HideThis:
                            ((DataRibbonItem)sampleGroup.Items[index]).Visible = false;
                            sampleGroup.ChangeMode = ContentChangeMode.Add;
                            break;
                    }
                    this.DxRibbon.RefreshGroup(sampleGroup);
                }
            }
        }
        private enum TestActionType { None, RemoveRandom, RemoveThis, HideRandom, HideThis }
        private bool _TestSampleHideEmptyGroup;
        private DataRibbonGroup _TestControlGroup;
        private DataRibbonGroup _TestSampleGroup;
        private DataRibbonGroup _TestControlAfterGroup;
        private int _TestItemId;
        private string[] _TestSampleImages;
        #endregion
        #region Testy specifického chování
        /// <summary>
        /// Do Ribbonu přidá definici pro specifické testy
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="homePage"></param>
        private void _RibbonSpecificTestsInit(List<DataRibbonPage> pages, DataRibbonPage homePage)
        {
            _TestSpecificGroup = new DataRibbonGroup() { GroupText = "TESTOVACÍ PRVKY", HideEmptyGroup = false };
            _TestSpecificImages = new string[]
            {
    "images/actions/squeeze_16x16.png",
    "images/actions/stretch_16x16.png",
    "images/alignment/alignhorizontalbottom_16x16.png",
    "images/alignment/alignhorizontalbottom2_16x16.png",
    "images/alignment/alignhorizontalcenter_16x16.png",
    "images/alignment/alignhorizontalcenter2_16x16.png",
    "images/alignment/alignhorizontaltop_16x16.png",
    "images/alignment/alignhorizontaltop2_16x16.png",
    "images/alignment/alignverticalcenter_16x16.png",
    "images/alignment/alignverticalcenter2_16x16.png",
    "images/alignment/alignverticalleft_16x16.png",
    "images/alignment/alignverticalleft2_16x16.png",
    "images/alignment/alignverticalright_16x16.png",
    "images/alignment/alignverticalright2_16x16.png",
    "images/alignment/mergeacross_16x16.png",
    "images/alignment/mergecells_16x16.png",
    "images/alignment/mergecenter_16x16.png",
    "images/alignment/unmergecells_16x16.png"
            };

            homePage.Groups.Add(_TestSpecificGroup);
            DataRibbonItem testItem = new DataRibbonItem()
            {
                ItemId = "MenuWithCheckedItems",
                Text ="Nabídka šablon",
                ImageName = "images/richedit/differentoddevenpages_32x32.png",
                ItemType = RibbonItemType.Menu,
                RibbonStyle = RibbonItemStyles.Large,
                ToolTipTitle = "Menu se šablonami",
                ToolTipText = "Zobrazí menu, kde některé položky budou Checked",
                SubItems = new ListExt<IRibbonItem>()
            };
            addSubItem();
            addSubItem(true);
            addSubItem(false, true, true);
            addSubItem();
            addSubItem(true);
            addSubItem();
            addSubItem();
            _TestSpecificGroup.Items.Add(testItem);

            void addSubItem(bool isFirst = false, bool isChecked = false, bool isBold = false)
            {
                string text = Randomizer.GetSentence(2, 4, false); // + (isChecked ? " (aktivní)" : "");
                string imageName = null;  //  Randomizer.GetItem(_TestSpecificImages),
                var subItem = new DataRibbonItem()
                {
                    ItemId = "TemplateSubItem" + testItem.SubItems.Count.ToString(),
                    ItemType = RibbonItemType.CheckBoxStandard,
                    Text = text,
                    ImageName = imageName,
                    Checked = isChecked,
                    ItemIsFirstInGroup = isFirst,
                    FontStyle = (isBold ? (System.Drawing.FontStyle?)System.Drawing.FontStyle.Bold : null)
                };
                testItem.SubItems.Add(subItem);
            }
        }
        private DataRibbonGroup _TestSpecificGroup;
        private string[] _TestSpecificImages;
        #endregion
        #endregion
        #region Logování
        private void DxComponent_LogTextChanged(object sender, EventArgs e)
        {
            _LogContainChanges = true;
        }
        protected override void OnApplicationIdle()
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
            _MainPanel = DxComponent.CreateDxPanel(this, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);

            this.Dock = System.Windows.Forms.DockStyle.Fill;
            _Ribbon = new DxRibbonControl() { Dock = System.Windows.Forms.DockStyle.Top, ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False, LogActive = true };
            _Ribbon.PageOnDemandLoad += _Ribbon_PageOnDemandLoad;
            _Ribbon.ItemOnDemandLoad += _Ribbon_ItemOnDemandLoad;
            _Ribbon.RibbonApplicationButtonClick += _Ribbon_RibbonApplicationButtonClick;
            _Ribbon.RibbonPageCategoryClick += _Ribbon_RibbonPageCategoryClick;
            _Ribbon.RibbonGroupButtonClick += _Ribbon_RibbonGroupButtonClick;
            _Ribbon.RibbonItemClick += _Ribbon_RibbonItemClick;
            _Ribbon.QATItemKeysChanged += _Ribbon_QATItemKeysChanged;
            _Ribbon.LoadSearchEditItems += _Ribbon_LoadSearchEditItems;

            this.Controls.Add(_Ribbon);

            int x = 00;
            _ButtonContext = DxComponent.CreateDxSimpleButton(x, 160, 20, 52, _MainPanel, "...", _RunContext, toolTipTitle: "Menu", toolTipText: "Zobrazí kontextové menu");

            x = 20;
            _ButtonClear = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, _MainPanel, "Clear", _RunClear, toolTipText: "Smaže obsah Ribbonu a nechá jej prázdný"); x += 160;
            _ButtonFill = DxComponent.CreateDxSimpleButton(x, 160, 150, 52, _MainPanel, "Fill", _RunFill, toolTipText: "Smaže obsah Ribbonu a vepíše do něj větší množství stránek"); x += 160;
            _ButtonMenu = DxComponent.CreateDxDropDownButton(x, 160, 150, 52, _MainPanel, "Add 4", click: DropDownButtonClick, itemClick: DropDownItemClick, subItems: _GetDropDownItems(), toolTipText: "Přidá menší počet prvků. Další volby jsou v menu.");

            x += 60;
            _ButtonMerge = DxComponent.CreateDxCheckButton(x, 160, 150, 52, _MainPanel, "Merge nahoru", _RunMerge, toolTipText: "Obsah tohoto Ribbonu připojí k buttonu o úroveň výše"); x += 160;
            _ButtonUnMerge = DxComponent.CreateDxCheckButton(x, 160, 150, 52, _MainPanel, "Unmerge", _RunUnMerge, isChecked: true, toolTipText: "Vrátí obsah tohoto Ribbonu z vyššího Ribbonu zpátky"); x += 160;

            DoLayoutButtons();

            this.IsMerged = false;
        }
        protected override void OnContentSizeChanged()
        {
            base.OnContentSizeChanged();
            DoLayoutButtons();
        }
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayoutButtons();
        }
        private void DoLayoutButtons()
        {
            int width = _MainPanel.ClientSize.Width;

            int dpi = this.CurrentDpi;

            int count1 = 3;
            int count2 = 2;

            // Určím, jestli buttony budu dávat do jedné řady = když se vejdou, anebo do dvou:
            int widthButton1 = DxComponent.ZoomToGui(150, dpi);
            int widthButton2 = widthButton1;
            int spaceButtons = DxComponent.ZoomToGui(12, dpi);
            int spaceGroup = DxComponent.ZoomToGui(24, dpi);
            int widthFull = (count1 * widthButton1) + ((count1 - 1) * spaceButtons) + spaceGroup + (count2 * widthButton2) + ((count2 - 1) * spaceButtons);
            int widthTotal = spaceGroup + widthFull + spaceGroup;
            bool isOneRow = widthTotal <= width;

            if (!isOneRow)
            {   // Pokud nebudu dávat buttony do jedné řady => budou ve dvou řadách: určíme jiné velikosti:
                widthButton1 = DxComponent.ZoomToGui(120, dpi);
                spaceButtons = DxComponent.ZoomToGui(9, dpi);
                widthFull = (count1 * widthButton1) + ((count1 - 1) * spaceButtons);
                widthButton2 = (widthFull - ((count2 - 1) * spaceButtons)) / count2;
                widthTotal = spaceGroup + widthFull + spaceGroup;
            }

            int spaceY1 = DxComponent.ZoomToGui(32, dpi);
            int spaceY2 = DxComponent.ZoomToGui(9, dpi);
            int heightButton1 = DxComponent.ZoomToGui((isOneRow ? 54 : 38), dpi);
            int heightButton2 = DxComponent.ZoomToGui((isOneRow ? 54 : 44), dpi);
            int distanceX = widthButton1 + spaceButtons;
            int distanceY = heightButton1 + spaceButtons;

            int x0 = ((width - widthTotal) / 2) + spaceGroup;

            if (!this.IsMerged && this.Ribbon.Bounds.Height > 32) _LastRibbonBottom = this.Ribbon.Bounds.Bottom;           // Udržuji souřadnici Y za stavu, kdy Ribbon je v panelu zobrazen
            int x = x0;
            int y = 0 /* _LastRibbonBottom */ + spaceY1;

            _ButtonContext.Bounds = new System.Drawing.Rectangle(2, y, 20, heightButton1);

            _ButtonClear.Bounds = new System.Drawing.Rectangle(x, y, widthButton1, heightButton1); x += distanceX;
            _ButtonFill.Bounds = new System.Drawing.Rectangle(x, y, widthButton1, heightButton1); x += distanceX;
            _ButtonMenu.Bounds = new System.Drawing.Rectangle(x, y, widthButton1, heightButton1); x += distanceX;
            if (isOneRow)
            {
                x += (spaceGroup - spaceButtons);
            }
            else
            {
                x = x0;
                y += distanceY;
                distanceX = widthButton2 + spaceButtons;
            }
            _ButtonMerge.Bounds = new System.Drawing.Rectangle(x, y, widthButton2, heightButton2); x += distanceX;
            _ButtonUnMerge.Bounds = new System.Drawing.Rectangle(x, y, widthButton2, heightButton2); x += distanceX;

            int svgHeight1 = 8 * heightButton1 / 10;
            int svgHeight2 = 8 * heightButton2 / 10;
            System.Drawing.Size svgSize1 = new System.Drawing.Size(svgHeight1, svgHeight1);
            System.Drawing.Size svgSize2 = new System.Drawing.Size(svgHeight2, svgHeight2);
            DxComponent.ApplyImage(_ButtonClear.ImageOptions, imageName: "svgimages/dashboards/delete.svg", imageSize: svgSize1);
            DxComponent.ApplyImage(_ButtonMenu.ImageOptions, imageName: "svgimages/icon%20builder/actions_add.svg", imageSize: svgSize1);
            DxComponent.ApplyImage(_ButtonFill.ImageOptions, imageName: "svgimages/icon%20builder/actions_addcircled.svg", imageSize: svgSize1);
            DxComponent.ApplyImage(_ButtonMerge.ImageOptions, imageName: "svgimages/spreadsheet/fillup.svg", imageSize: svgSize2);
            DxComponent.ApplyImage(_ButtonUnMerge.ImageOptions, imageName: "svgimages/spreadsheet/filldown.svg", imageSize: svgSize2);
        }
        /// <summary>
        /// Poslední známá souřadnice Bottom u Ribbonu, který NENÍ mergovaný
        /// </summary>
        private int _LastRibbonBottom;
        /// <summary>
        /// Zdejší Ribbon
        /// </summary>
        public DxRibbonControl Ribbon { get { return _Ribbon; } }
        /// <summary>
        /// Parent Ribbon, do něhož se mergujeme
        /// </summary>
        public DxRibbonControl ParentRibbon { get; set; }
        /// <summary>
        /// MergeOrder pro stránky tohoto Ribbonu
        /// </summary>
        public int PageMergeOrder { get; set; }
        /// <summary>
        /// Suffix kategorie, dovolí odlišit kategorie parenta od kategorie child ribbonu
        /// </summary>
        public string CategoryName { get; set; }
        /// <summary>
        /// Barva kategorií
        /// </summary>
        public System.Drawing.Color? CategoryColor { get; set; }
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
            this.DoLayoutButtons();
        }
        /// <summary>
        /// Bude se používat LazyLoad na Ribbonu tohoto panelu
        /// </summary>
        public DxRibbonControl.LazyContentMode UseLazyContentCreate { get { return this._Ribbon.UseLazyContentCreate; } set { this._Ribbon.UseLazyContentCreate = value; } }
        /// <summary>
        /// Naplní Ribbon nějakým počtem stránek a grup
        /// </summary>
        public void FillRibbon()
        {
            FillRibbon(3, 6, 4, 7);
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
            var pages = DxRibbonSample.CreatePages(this.Ribbon.DebugName, pageCountMin, pageCountMax, groupCountMin, groupCountMax, out var qatItems,
                CategoryName, CategoryName, CategoryColor, 
                pageIndex);
            DxComponent.LogAddLine(LogActivityKind.Ribbon, "Ribon: " + this._Ribbon.DebugName +"; QAT: " + qatItems);
            AddNewQatItems(qatItems);
            DxRibbonSample.SetPageMergeOrder(pages, this.PageMergeOrder);
            _Ribbon.AddPages(pages, clearCurrentContent);
        }
        /// <summary>
        /// Do Ribbonu přidá novou neviditelnou stránku
        /// </summary>
        public void AddInvisiblePageRibbon()
        {
            DataRibbonPage page = new DataRibbonPage() { PageId = "invisible", PageText = "   ", Visible = true };
            page.Groups.Add(new DataRibbonGroup() { GroupId = "invisible", GroupText = "Invisible", Visible = false });
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            pages.Add(page);
            DxRibbonSample.SetPageMergeOrder(pages, this.PageMergeOrder);
            _Ribbon.AddPages(pages, false);
        }
        /// <summary>
        /// Metoda najde v dodaném stringu klíče těch prvků, které dosud nejsou v <see cref="DxQuickAccessToolbar.QATItems"/>, a jednou dávkou je do něj přidá.
        /// </summary>
        /// <param name="newQatItems"></param>
        /// <returns></returns>
        private void AddNewQatItems(string newQatItems)
        {
            var location = DxQuickAccessToolbar.QATLocation;
            List<string> currItems = DxQuickAccessToolbar.QATItems?.ToList() ?? new List<string>();
            int currCount = currItems.Count;

            var newItems = newQatItems.Split('\t');
            currItems.AddRange(newItems.Where(i => !DxQuickAccessToolbar.ContainsQATItem(i)));     // Jen ty, které tam dosud nejsou
            if (currCount != currItems.Count)
                DxQuickAccessToolbar.QATItems = currItems.ToArray();
        }
        /// <summary>
        /// Do dané stránky a dané grupy pošle nový obsah.
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="groupText"></param>
        public void ReloadGroup(string pageText, string groupText)
        {
            string pageId = DxRibbonSample.GetPageId(pageText);
            if (pageId == null) return;
            string groupId = DxRibbonSample.GetGroupId(pageId, groupText);
            if (groupId == null) return;

            var iRibbonPage = new DataRibbonPage() { PageId = pageId };
            var iRibbonGroup = new DataRibbonGroup()
            {
                ParentPage = iRibbonPage,
                GroupId = groupId,
                GroupText = groupText,
                GroupButtonVisible = true,
                GroupState = RibbonGroupState.Expanded,
                ChangeMode = ContentChangeMode.ReFill
            };

            string qatItems = "";
            DxRibbonSample.AddItemsToGroup(iRibbonGroup, 6, 20, ref qatItems);

            _Ribbon.RefreshGroup(iRibbonGroup);
        }
        /// <summary>
        /// Do dané stránky a dané grupy pošle nový obsah.
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="groupText"></param>
        public void ClearGroup(string pageText, string groupText)
        {
            string pageId = DxRibbonSample.GetPageId(pageText);
            if (pageId == null) return;
            string groupId = DxRibbonSample.GetGroupId(pageId, groupText);
            if (groupId == null) return;

            var iRibbonPage = new DataRibbonPage() { PageId = pageId };
            var iRibbonGroup = new DataRibbonGroup()
            {
                ParentPage = iRibbonPage,
                GroupId = groupId,
                GroupText = groupText,
                GroupButtonVisible = true,
                GroupState = RibbonGroupState.Expanded,
                ChangeMode = ContentChangeMode.ReFill
            };

            // Bez položek v režimu ReFill = Clear

            _Ribbon.RefreshGroup(iRibbonGroup);
        }
        /// <summary>
        /// Do dané stránky a dané grupy pošle nový obsah.
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="groupText"></param>
        public void RemoveGroup(string pageText, string groupText)
        {
            string pageId = DxRibbonSample.GetPageId(pageText);
            if (pageId == null) return;
            string groupId = DxRibbonSample.GetGroupId(pageId, groupText);
            if (groupId == null) return;

            var iRibbonGroup = new DataRibbonGroup()
            {
                GroupId = groupId,
                ChangeMode = ContentChangeMode.Remove
            };

            _Ribbon.RefreshGroup(iRibbonGroup);
        }
        /// <summary>
        /// Přidá několik málo prvků do QAT
        /// </summary>
        public void FillRibbonQAT()
        {
            _Ribbon.QATDirectItems = DxRibbonSample.CreateItems(3, 8).ToArray();
        }
        private DxRibbonControl _Ribbon;
        private DxPanelControl _MainPanel;
        private DxSimpleButton _ButtonClear;
        private DxDropDownButton _ButtonMenu;
        private DxSimpleButton _ButtonFill;
        private DxCheckButton _ButtonMerge;
        private DxCheckButton _ButtonUnMerge;
        private DxSimpleButton _ButtonContext;
        private void _RunClear(object sender, EventArgs args) 
        {
            this._Ribbon.Clear();
        }
        private List<IMenuItem> _GetDropDownItems()
        {
            List<IMenuItem> subItems = new List<IMenuItem>();
            subItems.Add(new DataRibbonItem() { ItemId = "ClearRibbon", Text = "Clear ", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "ClearContent", Text = "Clear Content only", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "AddOnDemand", Text = "Add ON DEMAND page", ImageName = "", ToolTipText = "", ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ItemId = "AddRandom", Text = "Add RANDOM page", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "AddWiki", Text = "Add WIKI page", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "Add7Pages", Text = "Add 7 pages", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "AddInvisiblePage", Text = "Add Invisible Page", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "ReloadGroupWikiSystem", Text = "Reload Group: Wiki-Systém", ImageName = "", ToolTipText = "", ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ItemId = "ClearGroupWikiSystem", Text = "Clear Group: Wiki-Systém", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "RemoveGroupWikiSystem", Text = "Remove Group: Wiki-Systém", ImageName = "", ToolTipText = "" });
            subItems.Add(new DataRibbonItem() { ItemId = "AddQAT", Text = "Add Direct QAT Items", ImageName = "", ToolTipText = "", ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ItemId = "RemoveEmpty", Text = "Remove Empty pages", ImageName = "", ToolTipText = "", ItemIsFirstInGroup = true });
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
            try
            {
                switch (e.Item.ItemId)
                {
                    case "ClearRibbon":
                        this._Ribbon.Clear();
                        break;
                    case "ClearContent":
                        this._Ribbon.ClearPagesContents();
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
                    case "AddInvisiblePage":
                        this.AddInvisiblePageRibbon();
                        break;
                    case "ReloadGroupWikiSystem":
                        this.ReloadGroup("WIKI", "Systém");
                        break;
                    case "ClearGroupWikiSystem":
                        this.ClearGroup("WIKI", "Systém");
                        break;
                    case "RemoveGroupWikiSystem":
                        this.RemoveGroup("WIKI", "Systém");
                        break;
                    case "AddQAT":
                        this.FillRibbonQAT();
                        break;
                    case "RemoveEmpty":
                        this._Ribbon.RemoveVoidContainers();
                        break;
                }
            }
            catch (Exception exc)
            {
                DxComponent.ShowMessageWarning(exc.Message, "Chyba");
            }
        }
        private void _RunContext(object sender, EventArgs args)
        {

        }
        private void _RunFill(object sender, EventArgs args)
        {
            FillRibbon(3, 6, 4, 9, true);
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
            ThreadManager.AddAction(_LoadPagesFromServer, e.Item);
        }
        private void _LoadPagesFromServer(object[] args)
        {
            System.Threading.Thread.Sleep(850);

            IRibbonPage ribbonPage = args[0] as IRibbonPage;
            int pageIndex = DxRibbonSample.FindPageIndex(ribbonPage.PageText);
            string qatItems;
            var pages = DxRibbonSample.CreatePages(this.Ribbon.DebugName, 1, 1, 4, 8, out qatItems, CategoryName, CategoryName, CategoryColor, pageIndex);
            this._Ribbon.RefreshPages(pages);
        }
        private void _Ribbon_ItemOnDemandLoad(object sender, TEventArgs<IRibbonItem> e)
        {
            ThreadManager.AddAction(_LoadItemFromServer, e.Item);
        }
        private void _Ribbon_LoadSearchEditItems(object sender, EventArgs e)
        {
            ThreadManager.AddAction(_LoadSearchEditItems);
        }
        private void _LoadSearchEditItems()
        {
            System.Threading.Thread.Sleep(100);

            List<IRibbonItem> searchItems = new List<IRibbonItem>();
            string[] resources = new string[]
{
    "svgimages/spreadsheet/createconebar3dchart.svg",
    "svgimages/spreadsheet/createconefullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createconemanhattanbarchart.svg",
    "svgimages/spreadsheet/createconestackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylindermanhattanbarchart.svg",
    "svgimages/spreadsheet/createcylinderstackedbar3dchart.svg"
};

            DataRibbonGroup groupF = new DataRibbonGroup() { GroupText = "++ Funkce" };
            DataRibbonGroup groupV = new DataRibbonGroup() { GroupText = "++ Vztahy" };
            searchItems.Add(new DataRibbonItem() { Text = $"Funkce VYTVOŘ [{Ribbon.DebugName}.1]", ParentGroup = groupF, SearchTags = "create", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[0] });
            searchItems.Add(new DataRibbonItem() { Text = $"Funkce REALIZUJ [{Ribbon.DebugName}.2]", ParentGroup = groupF, ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[1] });
            searchItems.Add(new DataRibbonItem() { Text = $"Funkce ZAÚČTUJ [{Ribbon.DebugName}].3", ParentGroup = groupF, SearchTags = "account", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[2] });
            searchItems.Add(new DataRibbonItem() { Text = $"Funkce AKTUALIZUJ [{Ribbon.DebugName}.4]", ParentGroup = groupF, ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[3] });
            searchItems.Add(new DataRibbonItem() { Text = $"Vztah DODAVATEL [{Ribbon.DebugName}.5]", ParentGroup = groupV, SearchTags = "1234", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[4] });
            searchItems.Add(new DataRibbonItem() { Text = $"Vztah SKLAD [{Ribbon.DebugName}.6]", ParentGroup = groupV, SearchTags = "2345", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[5] });
            searchItems.Add(new DataRibbonItem() { Text = $"Vztah ODBĚRATEL [{Ribbon.DebugName}.7]", ParentGroup = groupV, SearchTags = "3456", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[6] });
            searchItems.Add(new DataRibbonItem() { Text = $"Vztah PLÁTCE [{Ribbon.DebugName}.8]", ParentGroup = groupV, SearchTags = "4567", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.SmallWithText, ImageName = resources[7] });

            this._Ribbon.SearchEditItems = searchItems.ToArray();
        }
        private void _LoadItemFromServer(object[] args)
        {
            System.Threading.Thread.Sleep(850);

            IRibbonItem iRibbonItem = args[0] as IRibbonItem;
            DataRibbonItem ribbonItem = DataRibbonItem.CreateClone(iRibbonItem);
            ribbonItem.Text = Randomizer.GetSentence(2);
            ribbonItem.SubItems = DxRibbonSample.CreateSubItems(ribbonItem, ribbonItem.ItemType, 8, 15, 1);
            ribbonItem.SubItemsContentMode = RibbonContentMode.Static;
            DxRibbonSample.ApplyToolTip(ribbonItem);

            this._Ribbon.RefreshItem(ribbonItem);
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
        private void _Ribbon_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            IRibbonItem iRibbonItem = e.Item;

            if (iRibbonItem.ItemType == RibbonItemType.Menu) return;

            var ribbonButton = iRibbonItem.RibbonItem as DevExpress.XtraBars.BarButtonItem;
            if (ribbonButton != null && false)
            {
                ribbonButton.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.Check;
                bool isChecked = !(iRibbonItem.Checked ?? false);
                ribbonButton.Down = isChecked;
                iRibbonItem.Checked = isChecked;
            }

            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control)
            {
                Noris.Clients.Win.Components.DialogArgs dialogArgs = new Noris.Clients.Win.Components.DialogArgs();
                dialogArgs.Title = "Ribbon Item Click";
                dialogArgs.MessageTextContainsHtml = true;

                string messageText = $"Uživatel kliknul na prvek <b>{iRibbonItem.ItemType}</b>, s textem <b>{iRibbonItem.Text}</b>, z Ribbonu <b>{this.Ribbon.DebugName}</b>\r\n";
                if (iRibbonItem.ParentGroup?.ParentPage?.Category != null) messageText += $"Kategorie: <b>{iRibbonItem.ParentGroup.ParentPage.Category.CategoryText}</b>;\r\n";
                if (iRibbonItem.ParentGroup?.ParentPage != null) messageText += $"Stránka: <b>{iRibbonItem.ParentGroup.ParentPage.PageText}</b>;\r\n";
                if (iRibbonItem.ParentGroup != null) messageText += $"Skupina <b>{iRibbonItem.ParentGroup.GroupText}</b>;\r\n";
                messageText += $"ImageName <b>{iRibbonItem.ImageName}</b>;  ";
                dialogArgs.MessageText = messageText.Trim();

                dialogArgs.PrepareButtons(System.Windows.Forms.MessageBoxButtons.OK);
                dialogArgs.Owner = this.FindForm();
                Noris.Clients.Win.Components.DialogForm.ShowDialog(dialogArgs);
            }
        }
        private void _Ribbon_QATItemKeysChanged(object sender, EventArgs e)
        {
            string qatConfigValue = DxQuickAccessToolbar.ConfigValue;

            Noris.Clients.Win.Components.DialogArgs dialogArgs = new Noris.Clients.Win.Components.DialogArgs();
            dialogArgs.Title = "Ribbon Quick Access Toolbar change";
            dialogArgs.MessageTextContainsHtml = true;

            string messageText = $"Uživatel změnil obsah lišty <b>Ribbon Quick Access</b>:\r\nRibbon: <b>{this.Ribbon.DebugName}</b>;\r\nKeys: <b>{qatConfigValue}</b>";
            dialogArgs.MessageText = messageText.Trim();

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
        /// <param name="parentRibbonName"></param>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="qatItems"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public static List<DataRibbonPage> CreatePages(string parentRibbonName, int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, out string qatItems,
            string categoryId = null, string categoryText = null, System.Drawing.Color? categoryColor = null,
            int? pageIndex = null)
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            qatItems = "";
            _AddPages(pages, parentRibbonName, pageCountMin, pageCountMax, groupCountMin, groupCountMax, pageIndex, ref qatItems, categoryId, categoryText, categoryColor);
            return pages;
        }
        /// <summary>
        /// Metoda vytvoří soupis stránek s obsahem pro ribbon
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="parentRibbonName"></param>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="qatItems"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <param name="pageIndex"></param>
        public static void CreatePagesTo(List<DataRibbonPage> pages, string parentRibbonName, int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, ref string qatItems, string categoryId = null, string categoryText = null, System.Drawing.Color? categoryColor = null, int? pageIndex = null)
        {
            _AddPages(pages, parentRibbonName, pageCountMin, pageCountMax, groupCountMin, groupCountMax, pageIndex, ref qatItems,
                categoryId, categoryText, categoryColor);
        }
        /// <summary>
        /// Vytvoří a vrátí několik prvků Items
        /// </summary>
        /// <param name="itemCountMin"></param>
        /// <param name="itemCountMax"></param>
        /// <returns></returns>
        public static ListExt<IRibbonItem> CreateItems(int itemCountMin, int itemCountMax)
        {
            ListExt<IRibbonItem> items = new ListExt<IRibbonItem>();
            int count = Rand.Next(itemCountMin, itemCountMax + 1);
            for (int i = 0; i < count; i++)
            {
                DataRibbonItem item = _GetItem();
                items.Add(item);
            }
            return items;
        }
        /// <summary>
        /// Do pole přidá stránky
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="parentRibbonName"></param>
        /// <param name="pageCountMin"></param>
        /// <param name="pageCountMax"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="pageIndex"></param>
        /// <param name="qatItems"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        private static void _AddPages(List<DataRibbonPage> pages, string parentRibbonName, int pageCountMin, int pageCountMax, int groupCountMin, int groupCountMax, int? pageIndex, ref string qatItems, 
            string categoryId = null, string categoryText = null, System.Drawing.Color? categoryColor = null)
        {
            var startTime = DxComponent.LogTimeCurrent;
            int prevId = _RibbonItemId;

            // if (!categoryColor.HasValue) categoryColor = System.Drawing.Color.DarkViolet;
            
            int pc = Rand.Next(pageCountMin, pageCountMax + 1);
            for (int p = 0; p < pc; p++)
            {
                DataRibbonPage page = _GetPage(parentRibbonName, pageIndex, categoryId, categoryText, categoryColor);
                if (page == null) continue;

                pages.Add(page);

                // Pokud NENÍ explicitně daná stránka (pageIndex je null), a my jsme náhodně určili stránku v režimu OnDemandLoad:
                //  Pak do této stránky nebudu dávat nyní žádný viditelný obsah (protože je OnDemand!),
                //  ale jen zajistím, že seznam bude obsahovat právě jeden záznam pro tuto stránku s prvkem typu None:
                bool isFirstOnDemand = (!pageIndex.HasValue && (page.PageContentMode == RibbonContentMode.OnDemandLoadOnce || page.PageContentMode == RibbonContentMode.OnDemandLoadEveryTime));
                if (isFirstOnDemand) continue;

                _AddGroups(page, groupCountMin, groupCountMax, ref qatItems);
            }
            int count = _RibbonItemId - prevId;
            DxComponent.LogAddLineTime(LogActivityKind.Ribbon, $"Vygenerováno {count} prvků v čase {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Do stránky přidá grupy
        /// </summary>
        /// <param name="page"></param>
        /// <param name="groupCountMin"></param>
        /// <param name="groupCountMax"></param>
        /// <param name="qatItems"></param>
        private static void _AddGroups(DataRibbonPage page, int groupCountMin, int groupCountMax, ref string qatItems)
        {
            int gc = Rand.Next(groupCountMin, groupCountMax + 1);
            for (int g = 0; g < gc; g++)
            {
                DataRibbonGroup group = _GetGroup(page.PageId);
                page.Groups.Add(group);
                group.ParentPage = page;

                AddItemsToGroup(group, 1, 6, ref qatItems);
            }
        }
        /// <summary>
        /// Do grupy přidá prvky
        /// </summary>
        /// <param name="group"></param>
        /// <param name="itemCountMin"></param>
        /// <param name="itemCountMax"></param>
        /// <param name="qatItems"></param>
        internal static void AddItemsToGroup(DataRibbonGroup group, int itemCountMin, int itemCountMax, ref string qatItems)
        {
            bool containsRadioGroup = false;
            int remainingRadioCount = 0;
            string buttonGroupName = null;
            bool forceFirstInGroup = false;
            int ic = Rand.Next(itemCountMin, itemCountMax + 1);
            for (int i = 0; i < ic; i++)
            {
                DataRibbonItem item = _GetItem(group.GroupId, ref containsRadioGroup, ref remainingRadioCount, ref buttonGroupName, ref forceFirstInGroup, ref qatItems);
                group.Items.Add(item);
                item.ParentGroup = group;
                ApplyToolTip(item);
                if (remainingRadioCount > 0 && i == (ic - 1))   // Dokud zrovna generuji RadioGrupu (mám remainingRadioCount kladné) a blížím se ke konci počtu našich prvků,
                    ic++;                                       //   pak přidám ještě další prvek, abych RadioGrupu dotáhl do konce.
            }
        }
        /// <summary>
        /// Pro daný titulek stránky určí, zda jde nebo nejde o kategorii, a vrací null nebo instanci kategorie
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="parentRibbonName"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <returns></returns>
        private static DataRibbonCategory _GetCategory(string parentRibbonName, string pageText, string categoryId, string categoryText, System.Drawing.Color? categoryColor)
        {
            bool isCategory = (pageText == "VZTAHY" || pageText == "MODIFIKACE");
            if (!isCategory) return null;

            if (String.IsNullOrEmpty(categoryId)) categoryId = "Extend1";
            if (String.IsNullOrEmpty(categoryText)) categoryText = "DALŠÍ VOLBY";

            return new DataRibbonCategory()
            {
                ParentRibbonName = parentRibbonName,
                CategoryId = categoryId,
                CategoryText = categoryText,
                CategoryColor = categoryColor,
                CategoryVisible = true
            };
        }
        /// <summary>
        /// Vytvoří a vrátí stránku Ribbonu, podle potřeby ji zařadí do patřičné kategorie. Bez Groups.
        /// </summary>
        /// <param name="parentRibbonName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="categoryId"></param>
        /// <param name="categoryText"></param>
        /// <param name="categoryColor"></param>
        /// <returns></returns>
        private static DataRibbonPage _GetPage(string parentRibbonName, int? pageIndex, string categoryId, string categoryText, System.Drawing.Color? categoryColor)
        {
            int pageTotal = PageNames.Length;
            if (pageIndex.HasValue && (pageIndex.Value < 0 || pageIndex.Value >= pageTotal)) return null;
            int pi = pageIndex ?? Rand.Next(pageTotal);
            if (pi < 0 || pi >= pageTotal) throw new ArgumentException($"Požadovaný index stránky {pi} je mimo rozsah 0 až {pageTotal}");

            string pageText = PageNames[pi];
            IRibbonCategory category = _GetCategory(parentRibbonName, pageText, categoryId, categoryText, categoryColor);
            RibbonContentMode contentMode = (pageText == "ON.DEMAND" ? RibbonContentMode.OnDemandLoadOnce :
                                            (pageText == "RANDOM" ? RibbonContentMode.OnDemandLoadEveryTime : RibbonContentMode.Static));

            return new DataRibbonPage()
            {
                ParentRibbonName = parentRibbonName,
                Category = category,
                PageId = "Page" + pi,
                PageText = pageText,
                MergeOrder = pi + 1,
                PageContentMode = contentMode,
                PageType = RibbonPageType.Default
            };
        }
        /// <summary>
        /// Vrátí PageId pro daný text stránky, např. pro text "WIKI" vrací "Page5"
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public static string GetPageId(string pageText)
        {
            int index = PageNames.IndexOf(n => n == pageText);
            if (index < 0) return null;
            return $"Page{index}";
        }
        /// <summary>
        /// Vrátí GroupId pro dané ID stránky a daný text skupiny, např. pro text "Systém" vrací "Page4.Group5"
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="groupText"></param>
        /// <returns></returns>
        public static string GetGroupId(string pageId, string groupText)
        {
            int index = GroupNames.IndexOf(n => n == groupText);
            if (index < 0) return null;
            return $"{pageId}.Group{index}";
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
            string groupImageName = GetRandomImageName();
            bool groupButtonVisible = (groupText == "Rozšířené" || groupText == "Údržba" || groupText == "Oblíbené" || groupText == "Systém" || groupText == "Systém");
            bool groupCollapsed = (Randomizer.IsTrue(10));

            return new DataRibbonGroup()
            {
                GroupId = groupId,
                GroupText = groupText,
                GroupImageName = groupImageName,
                GroupButtonVisible = groupButtonVisible,
                GroupState = (groupCollapsed ? RibbonGroupState.Collapsed : RibbonGroupState.Auto)
            };
        }
        /// <summary>
        /// Vytvoří a vrátí prvek Ribbonu. Prvek podle potřeby obsahuje i SubItems.
        /// </summary>
        /// <returns></returns>
        private static DataRibbonItem _GetItem()
        {
            bool containsRadioGroup = false;
            int remainingRadioCount = 0;
            string buttonGroupName = null;
            bool forceFirstInGroup = false;
            string qatItems = null;
            DataRibbonItem item = _GetItem(null, ref containsRadioGroup, ref remainingRadioCount, ref buttonGroupName, ref forceFirstInGroup, ref qatItems);
            ApplyToolTip(item);
            return item;
        }
        /// <summary>
        /// Vytvoří a vrátí prvek Ribbonu. Prvek podle potřeby obsahuje i SubItems.
        /// Další parametry řídí tvorbu RadioGrupy.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="containsRadioGroup">Obsahuje false na začátku DxGroup, nastaví se na true při prvním prvku RadioGrupy, pokud je true pak další grupa už nezačne.</param>
        /// <param name="remainingRadioCount">Počet dalších buttonů, které máme přidat do RadioGrupy. Na počátku je 0, při zahájení RadioGrupy je nastaveno na číslo, pokud na vstupu není 0, pak se generují zbývající prvky RadioGrupy.</param>
        /// <param name="buttonGroupName">Jméno grupy</param>
        /// <param name="forceFirstInGroup">Vytvořený prvek má být první po separátoru</param>
        /// <param name="qatItems"></param>
        /// <returns></returns>
        private static DataRibbonItem _GetItem(string groupId, ref bool containsRadioGroup, ref int remainingRadioCount, ref string buttonGroupName, ref bool forceFirstInGroup, ref string qatItems)
        {
            string itemId = "Item" + (++_RibbonItemId);
            string itemText = Randomizer.GetWord(true);
            string itemImageName = GetRandomImageName();
            bool isFirst = (remainingRadioCount == 0 ? (forceFirstInGroup || (Rand.Next(10) < 3)) : false);          // Pokud nyní připravuji Radio, pak nedávám IsFirst !
            bool isEnabled = Randomizer.IsTrue(80);                           // 80% prvků má Enabled = true
            bool addToQat = Randomizer.IsTrue(10);                            // 10% prvků jde do QAT

            DataRibbonItem item = new DataRibbonItem()
            {
                ItemId = itemId,
                Text = itemText,
                Enabled = isEnabled,
                ImageName = itemImageName,
                RibbonStyle = RibbonItemStyles.All,
                ToolTipIcon = "help_hint_48_"
            };

            // Pokračujeme v přípravě skupiny RadioButtonů / CheckButtonRadio:
            if (remainingRadioCount != 0)
            {
                if (remainingRadioCount > 0)
                {   // Standardní RadioItem:
                    item.ItemType = RibbonItemType.RadioItem;
                    item.RibbonStyle = RibbonItemStyles.SmallWithText;    // RadioItem je vždy Small
                    item.RadioButtonGroupName = buttonGroupName;
                    addToQat = false;                                     // RadioButtony nedávám do Toolbaru
                    remainingRadioCount--;
                }
                else
                {   // CheckButton Radio:
                    item.ItemType = RibbonItemType.CheckButton;
                    item.RibbonStyle = RibbonItemStyles.Large;            // CheckButton grupa je vždy velká
                    item.RadioButtonGroupName = buttonGroupName;
                    remainingRadioCount++;                                // Záporné počitadlo => nahoru k nule
                }
                isFirst = false;
                if (remainingRadioCount == 0) forceFirstInGroup = true;   // Dokončili jsme stanovený počet RadioButtonů: příští prvek bude ForceFirst!
            }
            else
            {   // Vytváříme první prvek (jakýkoli, i první prvek grupy):
                RibbonItemType itemType = GetRandomItemType();
                if (itemType == RibbonItemType.RadioItem && containsRadioGroup) 
                    itemType = RibbonItemType.CheckBoxStandard;           // V jedné grupě Ribbonu bude nanejvýše jedna RadioButton grupa

                item.ItemType = itemType;
                if (itemType == RibbonItemType.CheckBoxStandard || itemType == RibbonItemType.RadioItem || itemType == RibbonItemType.CheckButton)
                    item.Checked = Randomizer.IsTrue(30);                     // 30% CheckBoxů je zaškrtnutých

                if (Randomizer.IsTrue(30))                                    // 30% všech prvků je Small
                    item.RibbonStyle = RibbonItemStyles.SmallWithText;

                if (itemType == RibbonItemType.RadioItem)
                {   // Zde začíná RadioButton grupa
                    isFirst = true;                                       // První RadioItem si zahajuje svoji sub-grupu
                    addToQat = false;                                     // RadioButtony nedávám do Toolbaru
                    remainingRadioCount = Rand.Next(3, 6);                // RadioItemů do jedné grupy dám 3 - 5 za sebou
                    containsRadioGroup = true;                            // RibbonGroup již obsahuje RadioGrupu, víc RadioSkupin tam dávat už nebudu
                    buttonGroupName = Randomizer.GetWord(true);
                    item.Checked = true;
                    item.RibbonStyle = RibbonItemStyles.SmallWithText;    // RadioItem je vždy Small
                    item.RadioButtonGroupName = buttonGroupName;
                }
                if (itemType == RibbonItemType.CheckButton && !containsRadioGroup && Randomizer.IsTrue(50))
                {   // zde začne CheckButton Radio:
                    isFirst = true;                                       // První RadioItem si zahajuje svoji sub-grupu
                    addToQat = false;                                     // RadioButtony nedávám do Toolbaru
                    remainingRadioCount = -Rand.Next(3, 6);               // RadioItemů do jedné grupy dám 3 - 5 za sebou;    Záporné číslo = CheckButton grupa
                    containsRadioGroup = true;                            // RibbonGroup již obsahuje RadioGrupu, víc RadioSkupin tam dávat už nebudu
                    buttonGroupName = Randomizer.GetWord(true);
                    item.Checked = true;
                    item.RibbonStyle = RibbonItemStyles.Large;            // CheckButton grupa je vždy velká
                    item.RadioButtonGroupName = buttonGroupName;
                }

                if (Randomizer.IsTrue(15))                                    // 15% prvků nemá Image
                    item.ImageName = null;

              
                if (NeedSubItem(itemType))
                    item.SubItems = CreateSubItems(item, itemType, 4, 15);
            }

            item.ItemIsFirstInGroup = isFirst;

            // QAT (Quick Access ToolBar):
            if (addToQat)
                // Občas (a jen pro některé prvky) zařadíme prvek do QAT:
                qatItems += item.ItemId + "\t";
            else if (Rand.Next(100) < 8)
                // Občas do klíče QAT zařadíme nesmysl = ID prvku, který není součástí GUI. Tento prvek v evidenci Ribbonu musí vydržet přes všechny změny:
                qatItems += Randomizer.GetWord(true) + "\t";

            return item;
        }
        /// <summary>
        /// Vytvoří a vrátí pole SubItems, možná i rekurzivně
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="itemType"></param>
        /// <param name="subItemsCountMin"></param>
        /// <param name="subItemsCountMax"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ListExt<IRibbonItem> CreateSubItems(DataRibbonItem parentItem, RibbonItemType itemType, int subItemsCountMin, int subItemsCountMax, int level = 0)
        {
            if ((itemType == RibbonItemType.SplitButton || itemType == RibbonItemType.Menu) && Rand.Next(100) < 65 && level == 0)
            {   // SplitButton nebo Menu někdy dáme OnDemandLoadOnce:
                parentItem.Text = "...---...";
                parentItem.ToolTipText = "Prvky budou donačteny on-demand!";
                parentItem.SubItemsContentMode = RibbonContentMode.OnDemandLoadOnce;
                return null;
            }
            parentItem.SubItemsContentMode = RibbonContentMode.Static;

            ListExt<IRibbonItem> subItems = new ListExt<IRibbonItem>();

            int sc = Rand.Next(subItemsCountMin, subItemsCountMax + 1);
            for (int i = 0; i < sc; i++)
            {
                string itemId = "Item" + (++_RibbonItemId);
                string itemText = Randomizer.GetWord(true);
                string itemImage = GetRandomImageName(33);
                string toolTip = Randomizer.GetSentence(Rand.Next(5, 16));
                string toolTipTitle = Randomizer.GetSentence(Rand.Next(1, 3));
                bool isFirst = (Rand.Next(10) < 3);

                DataRibbonItem subItem = new DataRibbonItem()
                {
                    ItemId = itemId,
                    Text = itemText,
                    ItemIsFirstInGroup = isFirst,
                    RibbonStyle = RibbonItemStyles.Default,
                    ToolTipText = toolTip,
                    ToolTipTitle = toolTipTitle,
                    ToolTipIcon = "help_hint_48_",
                    ImageName = itemImage
                };
          
                subItem.ItemType = (itemType == RibbonItemType.InRibbonGallery ? RibbonItemType.Button : GetRandomSubItemType());

                int nextLevel = level + 1;
                if (NeedSubItem(subItem.ItemType, nextLevel))
                {
                    if (level <= 4)
                        subItem.SubItems = CreateSubItems(subItem, itemType, 3, 7, nextLevel);
                    else
                        subItem.ItemType = RibbonItemType.Button;
                }

                if (subItem.ItemType == RibbonItemType.CheckBoxStandard || subItem.ItemType == RibbonItemType.RadioItem)
                {
                    if (Rand.Next(100) < 65) subItem.ImageName = null;
                    if (Rand.Next(100) < 50) subItem.Checked = true;
                }

                subItem.ParentItem = parentItem;
                ApplyToolTip(subItem);

                subItems.Add(subItem);
            }

            return subItems;
        }
        /// <summary>
        /// Do daného prvku vloží konkrétní ToolTip
        /// </summary>
        /// <param name="item"></param>
        public static void ApplyToolTip(DataRibbonItem item)
        {
            string itemText = item.Text;
            string itemType = item.ItemType.ToString();
            string groupText = "";
            string pageText = "";
            string ribbonName = "";
            if (item.ParentGroup != null)
            {
                groupText = item.ParentGroup.GroupText;
                if (item.ParentGroup.ParentPage != null)
                {
                    pageText = item.ParentGroup.ParentPage.PageText;
                    ribbonName = item.ParentGroup.ParentPage.ParentRibbonName;
                }
            }
            item.ToolTipTitle = $"{itemText} ({itemType}) [{ribbonName}.{pageText}.{groupText}]";
            item.ToolTipText = Randomizer.GetSentence(Rand.Next(5, 16));
        }
        /// <summary>
        /// Vrátí náhodný typ prvku v Ribbonu
        /// </summary>
        /// <returns></returns>
        public static RibbonItemType GetRandomItemType()
        {
            int rand = Rand.Next(100);
            if (rand < 45) return RibbonItemType.Button;
            if (rand < 50) return RibbonItemType.CheckBoxStandard;
            if (rand < 55) return RibbonItemType.CheckButton;
            if (rand < 60) return RibbonItemType.RadioItem;
            // if (rand < 85) return RibbonItemType.ButtonGroup;         nějak se mi nelíbí
            if (rand < 70) return RibbonItemType.InRibbonGallery;
            if (rand < 85) return RibbonItemType.SplitButton;
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
            if (rand < 50) return RibbonItemType.Button;
            if (rand < 60) return RibbonItemType.CheckBoxStandard;
            if (rand < 65) return RibbonItemType.CheckBoxToggle;
            if (rand < 80) return RibbonItemType.SplitButton;
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
            int rnd = Rand.Next(100);
            if (rnd < 34) return Randomizer.GetItem(DxSvgResourceImages);
            if (rnd < 67) return Randomizer.GetItem(DxPngApplicationImages);
            return Randomizer.GetItem(DxSvgApplicationImages);
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
        /// Konstantní pole se jmény obrázků DevExpress SVG
        /// </summary>
        public static string[] DxSvgResourceImages { get { if (_DxSvgResourceImages is null) _DxSvgResourceImages = _GetDxSvgResourceImages(); return _DxSvgResourceImages; } }
        private static string[] _DxSvgResourceImages;
        /// <summary>
        /// Vrací seznam Images se jmény obrázků DevExpress SVG
        /// </summary>
        /// <returns></returns>
        private static string[] _GetDxSvgResourceImages()
        {
            return DxComponent.GetResourceNames(".svg", false, true);
        }
        /// <summary>
        /// Konstantní pole se jmény obrázků Application PNG
        /// </summary>
        public static string[] DxPngApplicationImages { get { if (_DxPngApplicationImages is null) _DxPngApplicationImages = _GetDxPngApplicationImages(); return _DxPngApplicationImages; } }
        private static string[] _DxPngApplicationImages;
        /// <summary>
        /// Vrací seznam Images se jmény obrázků DevExpress SVG
        /// </summary>
        /// <returns></returns>
        private static string[] _GetDxPngApplicationImages()
        {
            return DxComponent.GetResourceNames(".png", true, false);
        }
        /// <summary>
        /// Konstantní pole se jmény obrázků Application PNG
        /// </summary>
        public static string[] DxSvgApplicationImages { get { if (_DxSvgApplicationImages is null) _DxSvgApplicationImages = _GetDxSvgApplicationImages(); return _DxSvgApplicationImages; } }
        private static string[] _DxSvgApplicationImages;
        /// <summary>
        /// Vrací seznam Images se jmény obrázků DevExpress PNG
        /// </summary>
        /// <returns></returns>
        private static string[] _GetDxSvgApplicationImages()
        {
            return DxComponent.GetResourceNames(".svg", true, false);
        }

        /// <summary>
        /// Do daných stránek vepíše postupně MergeOrder 
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="order"></param>
        internal static void SetPageMergeOrder(IEnumerable<DataRibbonPage> pages, int order)
        {
            pages.ForEachExec(p => { p.MergeOrder = ++order; p.PageOrder = order; });
        }

        private static int _RibbonItemId = 0;
        /// <summary>
        /// Random
        /// </summary>
        public static System.Random Rand { get { if (_Rand is null) _Rand = new System.Random(); return _Rand; } }
        private static System.Random _Rand;
    }
    #endregion
}
