﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

using XB = DevExpress.XtraBars;
using XR = DevExpress.XtraBars.Ribbon;
using DM = DevExpress.Utils.Menu;
using DS = DevExpress.Skins;
using DXN = DevExpress.XtraBars.Navigation;
using DXT = DevExpress.XtraTabbedMdi;

using NWC = Noris.Clients.Win.Components;
using TestDevExpress.Components;

namespace TestDevExpress.Forms
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            InitData();
            InitDevExpress();
            InitSkinList();

            InitPageEvents();
            InitBarManager();

            InitPopupPage();           // 0
            InitTabHeaders();          // 1
            InitSplitters();           // 2
            InitAnimation();           // 3
            InitResize();              // 4
            InitMdiPage();             // 5
            InitChart();               // 6
            InitMsgBox();              // 7
            InitEditors();             // 8
            InitTreeView();            // 9

            ActivatePage(9);
        }
        private void InitData()
        {
            Rand = new Random();
        }
        private void InitDevExpress()
        {
            DevExpress.UserSkins.BonusSkins.Register();
            DS.SkinManager.EnableFormSkins();
            DS.SkinManager.EnableMdiFormSkins();

            var skins = DS.SkinManager.Default.Skins;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "Stardust";


            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;
        }
        private void InitSkinList()
        {

            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();

            this.Skins = new List<DS.SkinContainer>();

            this.SkinList.Items.Clear();
            List<DS.SkinContainer> skins = new List<DS.SkinContainer>();
            foreach (DS.SkinContainer skin in DS.SkinManager.Default.Skins)
                skins.Add(skin);
            skins.Sort((a, b) => String.Compare(a.SkinName, b.SkinName));
            string initialSkinName = "Seven";
            TextItem selectedItem = null;
            foreach (DS.SkinContainer skin in skins)
            {
                TextItem item = new TextItem() { Text = skin.SkinName, Item = skin };
                this.SkinList.Items.Add(item);
                if (selectedItem == null || item.Text == initialSkinName) selectedItem = item;
                this.Skins.Add(skin);
            }
            this.SkinList.SelectedIndexChanged += SkinList_SelectedIndexChanged;
            this.SkinList.SelectedItem = selectedItem;
        }
        private void InitPageEvents()
        {
            this._TabContainer.SelectedIndexChanged += _TabContainer_SelectedIndexChanged;
        }

        private void _TabContainer_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (_TabContainer.SelectedIndex)
            {
                case 5:
                    ActivateRibbonPage();
                    break;
            }
        }
        private void SkinList_SelectedIndexChanged(object sender, EventArgs e)
        {
            TextItem item = this.SkinList.SelectedItem as TextItem;
            if (item == null) return;
            DS.SkinContainer skin = item.Item as DS.SkinContainer;
            if (skin == null) return;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skin.SkinName;
            var subSkins = skin.GetSkins();
        }
        private List<DS.SkinContainer> Skins;
        private void ActivatePage(int pageIndex)
        {
            this._TabContainer.SelectedIndex = pageIndex;
        }

        #region BarManager
        private void InitBarManager()
        {
            this._BarManager = new XB.BarManager();
            this._BarManager.Form = this.button1;

            this._BarManager.ToolTipController = new DevExpress.Utils.ToolTipController();
            this._BarManager.ToolTipController.AddClientControl(this);

            this._BarManager.ToolTipController.ShowShadow = true;
            this._BarManager.ToolTipController.Active = true;
            this._BarManager.ToolTipController.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            this._BarManager.ToolTipController.AutoPopDelay = 500;
            this._BarManager.ToolTipController.InitialDelay = 800;
            this._BarManager.ToolTipController.KeepWhileHovered = true;
            this._BarManager.ToolTipController.ReshowDelay = 2000;
            this._BarManager.ToolTipController.Rounded = true;
            this._BarManager.ToolTipController.RoundRadius = 25;
            this._BarManager.ToolTipController.ShowShadow = true;
            this._BarManager.ToolTipController.ToolTipStyle = DevExpress.Utils.ToolTipStyle.Windows7;
            this._BarManager.ToolTipController.ToolTipType = DevExpress.Utils.ToolTipType.Standard;


            var ttc = this._BarManager.GetToolTipController();
            //this._BarManager = new XB.BarManager(this.Container);
            //this._BarManager.ForceInitialize();

            //this._BarManager.AllowCustomization = true;
            //this._BarManager.AllowQuickCustomization = true;
            //this._BarManager.MenuAnimationType = XB.AnimationType.Fade;

            //this._BarManager.AllowItemAnimatedHighlighting = true;

            this._BarManager.ItemClick += _BarManager_ItemClick;
            //this._BarManager.CloseButtonClick += _BarManager_CloseButtonClick;
            //this._BarManager
        }
        XB.BarManager _BarManager;

        private void _BarManager_CloseButtonClick(object sender, EventArgs e)
        {
            
        }

        private void _BarManager_ItemClick(object sender, XB.ItemClickEventArgs e)
        {
            if (e.Item.Name == "CheckItem")
                CheckItemChecked = (e.Item as XB.BarCheckItem).Checked;
        }

        #endregion
        #region XtraBars Popup
        private void InitPopupPage()
        { }
        private void button1_Click(object sender, EventArgs e)
        {
            XB.PopupMenu pm = new XB.PopupMenu();
            // pm.MenuCaption = "Kontextové menu";    Používám BarHeaderItem !
            // pm.ShowCaption = true;
            //pm.ShowNavigationHeader = DevExpress.Utils.DefaultBoolean.True;
            //pm.DrawMenuSideStrip = DevExpress.Utils.DefaultBoolean.True;
            //pm.MenuAppearance.AppearanceMenu.Normal.BackColor = Color.Violet;
            //pm.MenuAppearance.HeaderItemAppearance.BackColor = Color.LightBlue;

            pm.DrawMenuSideStrip = DevExpress.Utils.DefaultBoolean.True;
            pm.DrawMenuRightIndent = DevExpress.Utils.DefaultBoolean.True;
            pm.MenuDrawMode = XB.MenuDrawMode.SmallImagesText;
            pm.Name = "menu";

            XB.BarHeaderItem bh1 = new XB.BarHeaderItem() { Caption = "Základní" };
            pm.AddItem(bh1);
            pm.AddItem(new XB.BarButtonItem(_BarManager, "První") {  Hint = "Hint k položce", Glyph = TestDevExpress.Properties.Resources.db_add_24_ });
            pm.AddItem(new XB.BarButtonItem(_BarManager, "Druhý") { ButtonStyle = XB.BarButtonStyle.Check, Glyph = TestDevExpress.Properties.Resources.dialog_close_24_, PaintStyle = XB.BarItemPaintStyle.Caption });

            XB.BarButtonItem bi3 = new XB.BarButtonItem(_BarManager, "Třetí&nbsp;<b>zvýrazněný</b> a <i>kurzivový</i> <u>text</u>");
            bi3.Glyph = TestDevExpress.Properties.Resources.arrow_right_double_2_24_;
            bi3.ShortcutKeyDisplayString = "Ctrl+F3";
            bi3.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            pm.AddItem(bi3);
            bi3.Links[0].BeginGroup = true;

            XB.BarHeaderItem bh2 = new XB.BarHeaderItem() { Caption = "Rozšiřující" };
            pm.AddItem(bh2);
            XB.BarCheckItem bbs = new XB.BarCheckItem(_BarManager) { Caption = "CheckItem zkouška", Name = "CheckItem", CheckBoxVisibility = XB.CheckBoxVisibility.BeforeText, CheckStyle = XB.BarCheckStyles.Standard };
            bbs.Checked = CheckItemChecked;
            pm.AddItem(bbs);

            XB.BarButtonItem bei = new XB.BarButtonItem(_BarManager, "BarButtonItem with Tip");
            bei.SuperTip = new DevExpress.Utils.SuperToolTip();
            bei.SuperTip.Items.AddTitle("NÁPOVĚDA");
            bei.SuperTip.Items.AddSeparator();
            var superItem = bei.SuperTip.Items.Add("BarButtonItem SuperTip");
            superItem.ImageOptions.Image = TestDevExpress.Properties.Resources.call_start_24_;

            bei.ItemAppearance.Normal.BackColor = Color.PaleVioletRed;
            pm.AddItem(bei);

            XB.BarButtonGroup bbg = new XB.BarButtonGroup(_BarManager)
            {
                Caption = "BarButtonGroup",
                ButtonGroupsLayout = XB.ButtonGroupsLayout.Default,
                Border = DevExpress.XtraEditors.Controls.BorderStyles.Style3D,
                MenuCaption = "Caption BarButtonGroup"
            };
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "1/4 in container") { Glyph = TestDevExpress.Properties.Resources.distribute_horizontal_x_24_ });
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "2/4 in container") { Glyph = TestDevExpress.Properties.Resources.distribute_horizontal_left_24_ });
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "3/4 in container") { Glyph = TestDevExpress.Properties.Resources.distribute_horizontal_right_24_ });
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "4/4 in container") { Glyph = TestDevExpress.Properties.Resources.distribute_horozontal_page_24_ });
            pm.AddItem(bbg);


            XB.BarHeaderItem bh3 = new XB.BarHeaderItem() { Caption = "Podřízené funkce on demand..." };
            pm.AddItem(bh3);
            XB.BarSubItem bsi = new XB.BarSubItem(_BarManager, "BarButtonGroup");
            // bbg.GetItemData += Bbg_GetItemData;                           // Tudy to chodí při každém rozsvícení MainMenu
            bsi.Popup += Bbg_Popup;                                       // Tudy to chodí při každém rozbalení SubMenu
            bsi.ItemAppearance.Normal.ForeColor = Color.Violet;
            bsi.Tag = "RELOAD";
            pm.AddItem(bsi);

            XB.BarHeaderItem bh4 = new XB.BarHeaderItem() { Caption = "Funkce se načítají...", Tag = "Funkce:" };
            bsi.AddItem(bh4);
            // XB.BarButtonItem bf0 = new XB.BarButtonItem(_BarManager, "funkce se načítají...");
            // bbg.AddItem(bf0);

            var links = pm.ItemLinks;
            links[2].Item.Enabled = false;
            links[7].Item.Caption = "Hlavička se změněným textem";


            _BarManager.SetPopupContextMenu(this, pm);
            pm.ShowPopup(_BarManager, Control.MousePosition);

        }
        /// <summary>
        /// Tudy to chodí při každém rozbalení SubMenu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bbg_Popup(object sender, EventArgs e)
        {   // Někdo rozbaluje SubItems...
            XB.BarSubItem bbg = sender as XB.BarSubItem;
            if (bbg == null) return;

            var tag = bbg.Tag;
            if (tag is string && ((string)tag) == "RELOAD")
            {
                bbg.Tag = "reloading...";
                StartReload(bbg);
            }
        }

        private void StartReload(XB.BarSubItem bbg)
        {
            this._WorkThread = new System.Threading.Thread(RunThread);
            this._WorkThread.IsBackground = true;
            this._WorkThread.Name = "ReloadThread";
            this._WorkThread.Start(bbg);
        }
        private void RunThread(object sender)
        {
            System.Threading.Thread.Sleep(500);
            this.Invoke(new Action<object>(DoneReload), sender);
            this._WorkThread = null;
        }
        private void DoneReload(object sender)
        {
            XB.BarSubItem bbg = sender as XB.BarSubItem;
            bbg.Tag = "loaded.";
            try
            {
                bbg.BeginUpdate();
                bbg.ItemLinks[0].Item.Caption = bbg.ItemLinks[0].Item.Tag as string;
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "1. podpoložka"));
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "2. podpoložka"));
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "4. podpoložka") { ShortcutKeyDisplayString = "Alt+F4", ShowItemShortcut = DevExpress.Utils.DefaultBoolean.True });

                XB.BarButtonItem bbi3 = new XB.BarButtonItem(_BarManager, "5. podpoložka");
                bbg.AddItem(bbi3);
                bbi3.Links[0].BeginGroup = true;

                bbg.AddItem(new XB.BarButtonItem(_BarManager, "3. podpoložka"));
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "6. podpoložka"));
            }
            finally
            {
                bbg.EndUpdate();
            }
        }
        System.Threading.Thread _WorkThread;


        private void Bbg_GetItemData(object sender, EventArgs e)
        {
        }

        private bool CheckItemChecked = true;
        #endregion
        #region RadialMenu
        private void button2_Click(object sender, EventArgs e)
        {
            var barManager = _BarManager;
            var rm = new XR.RadialMenu(barManager);
            rm.AutoExpand = true;                   // Menu je po vytvoření otevřené
            rm.ButtonRadius = 25;                   // Prostřední button
            rm.InnerRadius = 25;
            rm.MenuRadius = 140;                    // Celkem menu
            rm.MenuColor = Color.DarkCyan;          // Barva aktivních segmentů
            rm.BackColor = Color.LightBlue;         // Barva pozadí
            rm.Glyph = TestDevExpress.Properties.Resources.dialog_close_24_;      // Ikona uprostřed menu
            rm.PaintStyle = XR.PaintStyle.Skin;
            

            // Create bar items to display in Radial Menu 
            XB.BarItem btnCopy = new XB.BarButtonItem(barManager, "Copy");
            btnCopy.ImageOptions.ImageUri.Uri = "Copy;Size16x16";

            XB.BarItem btnCut = new XB.BarButtonItem(barManager, "Cut");
            btnCut.ImageOptions.ImageUri.Uri = "Cut;Size16x16";

            XB.BarItem btnDelete = new XB.BarButtonItem(barManager, "Delete");
            btnDelete.ImageOptions.ImageUri.Uri = "Delete;Size16x16";

            XB.BarItem btnPaste = new XB.BarButtonItem(barManager, "Paste");
            btnPaste.ImageOptions.ImageUri.Uri = "Paste;Size16x16";

            // Sub-menu with 3 check buttons 
            XB.BarSubItem btnMenuFormat = new XB.BarSubItem(barManager, "Format");
            XB.BarCheckItem btnCheckBold = new XB.BarCheckItem(barManager, false);
            btnCheckBold.Caption = "Bold";
            btnCheckBold.Checked = true;
            btnCheckBold.ImageOptions.ImageUri.Uri = "Bold;Size16x16";

            XB.BarCheckItem btnCheckItalic = new XB.BarCheckItem(barManager, true);
            btnCheckItalic.Caption = "Italic";
            btnCheckItalic.Checked = true;
            btnCheckItalic.ImageOptions.ImageUri.Uri = "Italic;Size16x16";

            XB.BarCheckItem btnCheckUnderline = new XB.BarCheckItem(barManager, false);
            btnCheckUnderline.Caption = "Underline";
            btnCheckUnderline.ImageOptions.ImageUri.Uri = "Underline;Size16x16";

            XB.BarItem[] subMenuItems = new XB.BarItem[] { btnCheckBold, btnCheckItalic, btnCheckUnderline };
            btnMenuFormat.AddItems(subMenuItems);

            XB.BarItem btnFind = new XB.BarButtonItem(barManager, "Find");
            btnFind.ImageOptions.ImageUri.Uri = "Find;Size16x16";

            XB.BarItem btnUndo = new XB.BarButtonItem(barManager, "Undo");
            btnUndo.ImageOptions.ImageUri.Uri = "Undo;Size16x16";

            XB.BarItem btnRedo = new XB.BarButtonItem(barManager, "Redo");
            btnRedo.ImageOptions.ImageUri.Uri = "Redo;Size16x16";

            var items = new XB.BarItem[] { btnCopy, btnCut, btnDelete, btnPaste, btnMenuFormat, btnFind, btnUndo, btnRedo };
            rm.AddItems(items);

            rm.ShowPopup(Control.MousePosition);
        }
        #endregion
        #region DXMenu
        private void button4_Click(object sender, EventArgs e)
        {
            DM.DXPopupMenu popup = new DM.DXPopupMenu(this);
            popup.Items.Add(new DM.DXMenuHeaderItem() { Caption = "Header" });
            popup.Items.Add(new DM.DXMenuItem("MenuItem 1"));
            popup.Items.Add(new DM.DXMenuItem("MenuItem 2") { BeginGroup = true });
            popup.Items.Add(new DM.DXMenuItem("MenuItem 3") { Enabled = false });
            popup.Items.Add(new DM.DXMenuItem("MenuItem 4"));

            popup.Items.Add(new DM.DXMenuHeaderItem() { Caption = "Header 2" });
            popup.Items.Add(new DM.DXMenuCheckItem("CheckItem 5") { Checked = true });

            var mc6 = new DM.DXSubMenuItem("DXSubMenuItem");
            mc6.Items.Add(new DM.DXMenuItem("SubItem 1"));
            mc6.Items.Add(new DM.DXMenuItem("SubItem 2"));
            mc6.Items.Add(new DM.DXMenuItem("SubItem 3"));
            mc6.Items.Add(new DM.DXMenuItem("SubItem 4"));
            popup.Items.Add(mc6);

            Point point = this.PointToClient(MousePosition);
            popup.ShowPopup(this, point);
        }
        #endregion
        #region WinForm menu
        private void button3_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolStripDropDownMenu ddm = new ToolStripDropDownMenu();
            ddm.RenderMode = ToolStripRenderMode.Professional;
            ddm.AllowTransparency = true;
            ddm.Opacity = 0.90d;
            ddm.AutoClose = true;
            ddm.DefaultDropDownDirection = ToolStripDropDownDirection.BelowRight;
            ddm.DropShadowEnabled = true;
            ddm.ShowCheckMargin = true;
            ddm.ShowImageMargin = true;
            ddm.ShowItemToolTips = true;
            // ddm.BackColor = Color.FromArgb(0, 0, 32);
            // ddm.ForeColor = Color.FromArgb(255, 255, 255);
            ddm.Margin = new Padding(6);

            // Title
            ToolStripLabel titleItem = new ToolStripLabel("TITULEK");
            titleItem.ToolTipText = "Popisek titulku";
            titleItem.Size = new Size(100, 28 + 4);
            // titleItem.Font = new Font(titleItem.Font, FontStyle.Bold);
            titleItem.TextAlign = ContentAlignment.MiddleCenter;
            ddm.Items.Add(titleItem);
            ddm.Items.Add(new ToolStripSeparator());

            // Položky
            ddm.Items.Add(new ToolStripMenuItem("První") { ToolTipText = "Tooltip k položce", Image = TestDevExpress.Properties.Resources.arrow_right_double_2_24_ });
            ddm.Items.Add(new ToolStripMenuItem("Druhý") { CheckOnClick = true, CheckState = CheckState.Checked, Image = TestDevExpress.Properties.Resources.arrow_left_double_2_24_ });
            ddm.Items.Add(new ToolStripMenuItem("Třetí"));

            // SubPoložka
            ToolStripMenuItem ddb = new ToolStripMenuItem() { Text = "DropDown &w", ToolTipText = "Submenu...", DropDownDirection = ToolStripDropDownDirection.Right };
            ddb.DropDown.BackColor = ddm.BackColor;
            ddb.DropDown.ForeColor = ddm.ForeColor;
            ddb.ShortcutKeys = Keys.Control | Keys.W;
            // její položky
            ddb.DropDownItems.Add("1. podpoložka");
            ddb.DropDownItems.Add("2. podpoložka");
            ddb.DropDownItems.Add("3. podpoložka");
            ddb.DropDownItems.Add("4. podpoložka");
            ddb.DropDownItems.Add("5. podpoložka");
            ddm.Items.Add(ddb);

            ddm.ItemClicked += Ddm_ItemClicked;
            ddm.LayoutStyle = ToolStripLayoutStyle.Table;
            ddm.Renderer.RenderItemText += Renderer_RenderItemText;

            ddm.Show(this, this.PointToClient(Control.MousePosition));
        }

        private void Renderer_RenderItemText(object sender, ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.YellowGreen;
            e.Text = e.Text + " *";
        }

        private void Ddm_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var ic = e.ClickedItem;
        }
        #endregion
        #region TabHeaders
        private void InitTabHeaders()
        {
            this.InitTabHeaders1();
            this.InitTabHeaders2();
        }
        private void InitTabHeaders1()
        {
            _TabHeaderStrip1 = TabHeaderStrip.Create(TabHeaderStrip.HeaderType.DevExpressTop);
            _TabHeaderControl1 = _TabHeaderStrip1.Control;
            _TabHeaderControl1.Dock = DockStyle.Fill;

            //DXN.TabPane tabPane = _TabHeaderStrip1.Control as DXN.TabPane;
            //tabPane.AppearanceButton.Normal.FontStyleDelta = FontStyle.Regular;
            //tabPane.AppearanceButton.Pressed.FontStyleDelta = FontStyle.Italic | FontStyle.Bold;

            using (_TabHeaderStrip1.SilentScope())
            {
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
            }

            this._PanelHeaders1.Controls.Add(_TabHeaderControl1);
            this._PanelHeaders1.BackColor = Color.FromArgb(160, 160, 190);
            this._PanelHeaders1.Dock = DockStyle.Top;
            this._PanelHeaders1.Height = _TabHeaderStrip1.OptimalSize;

            _TabHeaderStrip1.SelectedTabChanging += _TabHeaderStrip1_SelectedTabChanging;
            _TabHeaderStrip1.SelectedTabChanged += _TabHeaderStrip1_SelectedTabChanged;
            _TabHeaderStrip1.HeaderSizeChanged += _TabHeaderStrip1_HeaderSizeChanged;

        }
        /// <summary>
        /// Přidá novou záložku
        /// </summary>
        private void _TabStrip1AddItem()
        {
            int i = ++_TabStrip1BtnItem;
            Image tabImage = _GetImage(i);
            TabHeaderItem tabHeaderItem = TabHeaderItem.CreateItem("Key" + i, "Záhlaví " + i, "Titulek stránky " + i, "Nápověda k záložce číslo " + i, null, tabImage);
            _TabStrip1AddItem(tabHeaderItem);
        }
        private void _TabStrip1AddItem(TabHeaderItem tabHeaderItem)
        {
            bool nativeAdd = _NativeAddCheck.Checked;
            if (nativeAdd)
            {
                TabHeaderStripDXTop control = _TabHeaderStrip1.Control as TabHeaderStripDXTop;
                DXN.TabPane tabs = control as DXN.TabPane;
                // control.PageProperties.ShowMode = DXN.ItemShowMode.ImageAndText;

                // DXN.NavigationPageBounds
                // DXN.NavigationPageBase npb = new DXN.NavigationPageBase();

                // OK : DXN.NavigationPage page = new DXN.NavigationPage();
                var page = control.CreateNewPage() as DXN.TabNavigationPage;
                page.Name = tabHeaderItem.Key;
                page.Caption = tabHeaderItem.Label;
                page.PageText = tabHeaderItem.Label;
                page.ToolTip = tabHeaderItem.ToolTip;
                page.ImageOptions.Image = tabHeaderItem.Image;
                page.Properties.ShowMode = DXN.ItemShowMode.ImageAndText; // tabHeaderItem.ImageTextMode;

                tabs.Pages.Add(page);

                control.SelectedPage = page;
            }
            else
            {
                _TabHeaderStrip1.AddItem(tabHeaderItem);
            }
            _TestStacks();
        }
        private void _TabStrip1Clear()
        {
            bool nativeAdd = _NativeAddCheck.Checked;
            if (nativeAdd)
            {
                TabHeaderStripDXTop control = _TabHeaderStrip1.Control as TabHeaderStripDXTop;
                DXN.TabPane tabs = control as DXN.TabPane;
                tabs.Pages.Clear();
            }
            else
            {
                _TabHeaderStrip1.Clear();
            }
            _TestStacks();
        }
        private void _TestStacks()
        {
            StringBuilder sb = new StringBuilder();
            var sfis = StackFrameInfo.CreateStackTrace(1);
            StackFrameInfo.AddTo(sfis, sb, true);
            string result = sb.ToString();
        }
        /// <summary>
        /// Informace o jedné položce stacku
        /// </summary>
        private class StackFrameInfo
        {
            #region Konstruktor a data
            /// <summary>
            /// Vytvoří a vrátí pole položek stacktrace.
            /// </summary>
            /// <returns></returns>
            public static StackFrameInfo[] CreateStackTrace(int ignoreLevels, bool reverseOrder = false)
            {
                List<StackFrameInfo> result = new List<StackFrameInfo>();
                System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace(true).GetFrames();
                int count = frames.Length;
                int begin = ignoreLevels + 1;    // Na pozici [0] je this metoda, tu budu skrývat vždy; a přidám počet ignorovaných pozic z volající metody...
                if (begin < 0) begin = 0;
                for (int f = begin; f < count; f++)  
                    result.Add(new StackFrameInfo(frames[f]));
                if (reverseOrder) result.Reverse();
                return result.ToArray();
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="stackFrame"></param>
            public StackFrameInfo(System.Diagnostics.StackFrame stackFrame)
            {
                var method = stackFrame.GetMethod();
                var pars = method.GetParameters();

                FileName = stackFrame.GetFileName();
                LineNumber = stackFrame.GetFileLineNumber();
                DeclaringType = method.DeclaringType.FullName;
                MethodModifiers = (method.IsPublic ? "public " : "") +
                                  (method.IsPrivate ? "private " : "") +
                                  (!method.IsPrivate && !method.IsPublic ? "protected " : "") +
                                  (method.IsVirtual ? "virtual " : "") +
                                  (method.IsAbstract ? "abstract " : "") +
                                  (method.IsStatic ? "static " : "");
                MethodName = method.Name;
                string parameters = "(";
                string pard = "";
                foreach (var par in pars)
                {
                    var type = par.ParameterType;
                    parameters += (pard + (par.IsOut ? "out " : "") + type.FullName + " " + par.Name);
                    pard = ", ";
                }
                parameters += ")";
                Parameters = parameters;
                IsExternal = (DeclaringType.StartsWith("System.") || DeclaringType.StartsWith("Infragistics.") || DeclaringType.StartsWith("DevExpress."));
            }
            public override string ToString()
            {
                return $"{DeclaringType} : {MethodName}{Parameters}";
            }
            public readonly string FileName;
            public readonly int LineNumber;
            public readonly string DeclaringType;
            public readonly string MethodModifiers;
            public readonly string MethodName;
            public readonly string Parameters;
            public readonly bool IsExternal;
            #endregion
            /// <summary>
            /// Vloží data všech objektů do daného textového výstupu
            /// </summary>
            /// <param name="frames"></param>
            /// <param name="sb"></param>
            /// <param name="collapseExternals"></param>
            public static void AddTo(IEnumerable<StackFrameInfo> frames, StringBuilder sb, bool collapseExternals = false)
            {
                if (frames == null) return;
                bool inExternals = false;
                foreach (var frame in frames)
                {
                    if (collapseExternals && frame.IsExternal)
                    {
                        if (!inExternals)
                        {
                            frame.AddTo(sb, true);
                            inExternals = true;

                        }
                    }
                    else
                    {
                        inExternals = false;
                        frame.AddTo(sb);
                    }
                }
            }
            /// <summary>
            /// Vloží data this objektu do daného textového výstupu
            /// </summary>
            /// <param name="sb"></param>
            /// <param name="asExternal"></param>
            public void AddTo(StringBuilder sb, bool asExternal = false)
            {
                string tab = "\t";
                if (asExternal)
                {
                    sb.Append("[External code]" + tab);
                    sb.Append(tab);
                }
                else
                {
                    sb.Append(FileName + tab);
                    sb.Append(LineNumber + tab);
                }
                sb.Append(DeclaringType + tab);
                sb.Append(MethodModifiers + tab);
                sb.Append(MethodName + tab);
                sb.Append(Parameters);
                sb.AppendLine();
            }
        }
        private void _TabStrip1Refresh()
        {
            TabHeaderStripDXTop control = _TabHeaderStrip1.Control as TabHeaderStripDXTop;
        }
        private Image _GetImage(int i)
        {
            return RibbonSample.GetRandomImage();
        }
        private int _TabStrip1BtnItem = 0;
        /// <summary>
        /// Přidej 2 záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabTopAddBtn1_Click(object sender, EventArgs e)
        {
            bool silent = _TabTopAddSilentCheck.Checked;
            if (silent)
            {
                using (_TabHeaderStrip1.SilentScope())
                {
                    _TabStrip1AddItem();
                    _TabStrip1AddItem();
                    _TabStrip1Refresh();
                }
            }
            else
            {
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1Refresh();
            }
        }
        /// <summary>
        /// Smaž záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabTopAddBtn2_Click(object sender, EventArgs e)
        {
            bool silent = _TabTopAddSilentCheck.Checked;
            if (silent)
            {
                using (_TabHeaderStrip1.SilentScope())
                {
                    _TabStrip1Clear();
                    _TabStrip1Refresh();
                }
            }
            else
            {
                _TabStrip1Clear();
                _TabStrip1Refresh();
            }
        }
        /// <summary>
        /// Smaž a přidej
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabTopAddBtn3_Click(object sender, EventArgs e)
        {
            bool silent = _TabTopAddSilentCheck.Checked;
            if (silent)
            {
                using (_TabHeaderStrip1.SilentScope())
                {
                    _TabStrip1Clear();
                    _TabStrip1AddItem();
                    _TabStrip1AddItem();
                    _TabStrip1Refresh();
                }
            }
            else
            {
                _TabStrip1Clear();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1Refresh();
            }
        }

        private void _TabHeaderStrip1_SelectedTabChanging(object sender, ValueChangingArgs<string> e)
        {
            // S pomocí tohoto handleru realizuji požadavek: "Na novou záložku přepni až po druhém kliknutí na ni:"
            if (!String.Equals(e.ValueNew, _TabHeaderStrip1_AttemptKey))
            {   // Pokud jsem klikl na záložku, jejíž klíč není v _TabHeaderStrip1_AttemptKey, tak si klíč uložím (pro příště) a nyní to zakážu:
                _TabHeaderStrip1_AttemptKey = e.ValueNew;
                e.Cancel = true;
            }
        }
        private string _TabHeaderStrip1_AttemptKey = null;

        private void _TabHeaderStrip1_SelectedTabChanged(object sender, ValueChangedArgs<string> e)
        {
            this._PanelHeaders1.Height = _TabHeaderStrip1.OptimalSize;
            this.Text = e.ValueNew;
        }
        private void _TabHeaderStrip1_HeaderSizeChanged(object sender, ValueChangedArgs<Size> e)
        {
            this._PanelHeaders1.Height = _TabHeaderStrip1.OptimalSize;
        }

        private TabHeaderStrip _TabHeaderStrip1;
        private Control _TabHeaderControl1;

        private void InitTabHeaders2()
        {
            _TabHeaderStrip2 = TabHeaderStrip.Create(TabHeaderStrip.HeaderType.DevExpressLeft);
            _TabHeaderControl2 = _TabHeaderStrip2.Control;
            _TabHeaderControl2.Dock = DockStyle.Fill;

            using (_TabHeaderStrip2.SilentScope())
            {
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key1", "Záhlaví první 1", "Titulek stránky 1, poměrně velký prostor na šířku", "Nápověda 1", null, Properties.Resources.arrow_right_24_));
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key2", "Záhlaví druhé 2", "Titulek stránky 2, obsahuje doplňkové informace", "Nápověda 2", null, Properties.Resources.arrow_right_2_24_));
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key3", "Záhlaví třetí 3", "Titulek stránky 3, například: Uživatelem definované atributy", "Nápověda 3", null, Properties.Resources.arrow_right_3_24_));
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key4", "Záhlaví čtvrté 4", "Titulek stránky 4", "Nápověda 4", null, Properties.Resources.arrow_right_3_24_));
            }

            this._PanelHeaders2.Controls.Add(_TabHeaderControl2);
            this._PanelHeaders2.BackColor = Color.FromArgb(160, 190, 180);
            this._PanelHeaders2.Dock = DockStyle.Fill;
            this._PanelHeaders2.Height = _TabHeaderStrip2.OptimalSize;
            _TabHeaderStrip2.SelectedTabChanged += _TabHeaderStrip2_SelectedTabChanged;
            _TabHeaderStrip2.HeaderSizeChanged += _TabHeaderStrip2_HeaderSizeChanged;
        }
        private void _TabHeaderStrip2_SelectedTabChanged(object sender, ValueChangedArgs<string> e)
        {
            // this._PanelHeaders2.Width = _TabHeaderStrip2.OptimalSize;
            this.Text = e.ValueNew;
        }
        private void _TabHeaderStrip2_HeaderSizeChanged(object sender, ValueChangedArgs<Size> e)
        {
            this._PanelHeaders2.Width = _TabHeaderStrip1.OptimalSize;
        }
        private TabHeaderStrip _TabHeaderStrip2;
        private Control _TabHeaderControl2;

        private void InitTabHeaders8()
        {
            // Standardní horní navigační lišta, pouze buttony
            var tabPane = new TestTabPane()                // XB.Navigation.TabPane()
            {
                Name = "InnerTabControl",
                Dock = DockStyle.Fill,
                TabAlignment = DevExpress.XtraEditors.Alignment.Near,
                AllowCollapse = DevExpress.Utils.DefaultBoolean.True,       // Dovolí uživateli skrýt headery
                OverlayResizeZoneThickness = 25,
                ItemOrientation = Orientation.Horizontal
            };
            tabPane.PageProperties.ShowMode = XB.Navigation.ItemShowMode.ImageAndText;
            tabPane.PageProperties.AppearanceCaption.FontSizeDelta = 2;
            tabPane.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            tabPane.LookAndFeel.UseWindowsXPTheme = true;

            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            tabPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;  // ???
            tabPane.TransitionAnimationProperties.FrameInterval = 5 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
            tabPane.TransitionAnimationProperties.FrameCount = 20;                  // Celkový čas = interval * count
            tabPane.TransitionType = DevExpress.Utils.Animation.Transitions.Push;

            AddTabPages(tabPane);

            #region fungující algoritmy
            /*
             * 
            tabPane.AddPage("TabPane.TabNavigationPage 1", "page1");
            tabPane.AddPage("Titulek 2", "page2");
            tabPane.AddPage("Titulek 3", "page3");
            XB.Navigation.TabNavigationPage px = new XB.Navigation.TabNavigationPage()
            {
                Name = "p3",
                ControlName = "p3",
                Caption = "Titulek 5",
                PageText = "Titulek dalších dat",
                Image = Properties.Resources.address_book_new_4
            };
            tabPane.AddPage(px);

            ListView listView = new ListView();
            listView.Items.Add("Položka 1");
            listView.Items.Add("Položka 2");
            listView.Items.Add("Položka 3");
            listView.Items.Add("Položka 4");
            listView.Items.Add("Položka 5");
            listView.View = View.LargeIcon;
            var pl = tabPane.AddPage(listView);
            listView.Dock = DockStyle.Fill;
            pl.PageText = "List";
            pl.Caption = "Caption";

            var p0 = tabPane.Pages[0] as XB.Navigation.TabNavigationPage;
            p0.ImageOptions.Image = Properties.Resources.distribute_vertical_equal;
            p0.ToolTip = "Záhlaví dokladu / záznamu";
            p0.Caption = "Záznam";
            p0.PageText = "Data záznamu";

            var p1 = tabPane.Pages[1] as XB.Navigation.TabNavigationPage;
            p1.ImageOptions.Image = Properties.Resources.address_book_new_4;

            var type = tabPane.Pages[0].GetType();


            */
            #endregion

            this._PanelHeaders2.Controls.Add(tabPane);
            this._PanelHeaders2.BackColor = Color.FromArgb(180, 196, 180);

            tabPane.SelectedPageChanged += TabPane_SelectedPageChanged;
        }
        private void AddTabPageXX0(TestTabPane tabPane, string key, string caption, string pageText = null, Image image = null, Action<XB.Navigation.TabNavigationPage> fillAction = null)
        {
            XB.Navigation.TabNavigationPage page = new XB.Navigation.TabNavigationPage()
            {
                Name = key,
                ControlName = key,
                Caption = caption,
                PageText = pageText ?? caption,
                Image = image
            };
            fillAction?.Invoke(page);
            tabPane.AddPage(page);

            page.PageText = pageText ?? caption;
        }
        private void AddTabPages(XB.Navigation.TabPane tabPane)
        {
            AddTabPage(tabPane, "page1", "Titulek 1", image: Properties.Resources.arrow_right_24_);
            AddTabPage(tabPane, "page2", "Titulek 2", image: Properties.Resources.arrow_right_24_);
            AddTabPage(tabPane, "page3", "Titulek 3", image: Properties.Resources.arrow_right_24_);
            AddTabPage(tabPane, "page4", "Titulek 4", image: Properties.Resources.arrow_right_24_);
        }
        private void AddTabPage(XB.Navigation.TabPane tabPane, string key, string caption, string pageText = null, Image image = null, Action<XB.Navigation.TabNavigationPage> fillAction = null)
        {
            tabPane.AddPage(caption, key);
            XB.Navigation.TabNavigationPage page = tabPane.Pages[tabPane.Pages.Count - 1] as XB.Navigation.TabNavigationPage;
            page.Name = key;
            page.ControlName = key;
            page.Caption = caption;
            page.PageText = pageText ?? caption;
            page.Image = image;

            fillAction?.Invoke(page);
        }
        private void TabPane_SelectedPageChanged(object sender, XB.Navigation.SelectedPageChangedEventArgs e)
        {
            Type newType = e.Page.GetType();
            XB.Navigation.TabNavigationPage np = e.Page as XB.Navigation.TabNavigationPage;
            var name = np.Name;
            this.Text = np.PageText;
        }
        internal class TestTabPane : XB.Navigation.TabPane
        { }
        private void InitTabHeaders9()
        {
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;

            // DevExpress.Utils.Animation.TransitionManager tm; tm.

            // Boční lišta záhlaví + objekt
            XB.Navigation.NavigationPane navPane = new XB.Navigation.NavigationPane()
            {
                Name = "InnerTabControl",
                Dock = DockStyle.Fill
                // TabAlignment = DevExpress.XtraEditors.Alignment.Near
            };
            navPane.PageProperties.ShowMode = XB.Navigation.ItemShowMode.ImageAndText;
            navPane.PageProperties.AppearanceCaption.FontSizeDelta = 2;
            navPane.ItemOrientation = Orientation.Horizontal;             // Otáčí obsah buttonu

            navPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;
            navPane.TransitionAnimationProperties.FrameInterval = 5 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
            navPane.TransitionAnimationProperties.FrameCount = 20;                  // Celkový čas = interval * count
            navPane.TransitionType = DevExpress.Utils.Animation.Transitions.Push;

            navPane.PageProperties.ShowCollapseButton = false;
            navPane.PageProperties.ShowExpandButton = false;
            navPane.PageProperties.AllowBorderColorBlending = true;
            navPane.ShowToolTips = DevExpress.Utils.DefaultBoolean.True;
            navPane.State = XB.Navigation.NavigationPaneState.Expanded;

            navPane.AddPage("Titulek 1", "page1");

            navPane.AddPage(new TextBox() { Text = "Obsah textboxu", Name = "TextBox" });

            ListView listView = new ListView();
            listView.Items.Add("Položka 1");
            listView.Items.Add("Položka 2");
            listView.Items.Add("Položka 3");
            listView.Items.Add("Položka 4");
            listView.Items.Add("Položka 5");
            listView.View = View.LargeIcon;
            navPane.AddPage(listView);
            listView.Dock = DockStyle.Fill;

            var type = navPane.Pages[0].GetType();

            var p0 = navPane.Pages[0] as XB.Navigation.NavigationPage;
            p0.PageText = "Hlavička";
            p0.Caption = "Hlavička záznamu";
            p0.CustomHeaderButtons.Add(new XB.Docking2010.WindowsUIButton("Tlačítko", XB.Docking2010.ButtonStyle.PushButton) { UseImage = true } );
            p0.PageVisible = true;
            p0.ToolTip = "Hlavička záznamu";
            p0.ImageOptions.Image = Properties.Resources.align_horizontal_left_24_;



            var p1 = navPane.Pages[1] as XB.Navigation.NavigationPage;
            p1.ImageOptions.Image = Properties.Resources.align_horizontal_right_2_24_;
            p1.PageText = "UDA";
            p1.Caption = "Uživatelem definované atributy";

            var p2 = navPane.Pages[2] as XB.Navigation.NavigationPage;
            p2.ImageOptions.Image = Properties.Resources.align_vertical_bottom_2_24_;
            p2.Caption = "Titulkový text = Seznam položek";
            p2.PageText = "Položky";

            this._PanelHeaders1.Controls.Add(navPane);
        }

        #endregion
        #region Splittery
        private void InitSplitters()
        {
            InitAsolSplitters();
        }
        private void InitAsolSplitters()
        {
            _AsolPanel = new AsolPanel();
            _AsolPanel.Dock = DockStyle.None;
            _AsolPanel.AutoScroll = true;
            _PanelSplitter.Controls.Add(_AsolPanel);
            _PanelSplitter.SizeChanged += _PanelSplitter_SizeChanged;

            int x = 50;
            int y = 50;
            int w = 350;
            int h = 250;
            var gp1 = new AsolSamplePanel() { Name = "Rhombus", Bounds = new Rectangle(x, y, w, h), Shape = AsolSamplePanel.ShapeType.Rhombus, CenterColor = Color.DarkGreen };
            _AsolPanel.Controls.Add(gp1);
            var gp2 = new AsolSamplePanel() { Name = "Star4", Bounds = new Rectangle(x, y + h, w, h), Shape = AsolSamplePanel.ShapeType.Star4, CenterColor = Color.BlueViolet };
            _AsolPanel.Controls.Add(gp2);
            var gp3 = new AsolSamplePanel() { Name = "Star8AcuteAngles", Bounds = new Rectangle(x + w, y, w, h), Shape = AsolSamplePanel.ShapeType.Star8AcuteAngles, CenterColor = Color.DarkOrchid };
            _AsolPanel.Controls.Add(gp3);
            var gp4 = new AsolSamplePanel() { Name = "Star8ObtuseAngles", Bounds = new Rectangle(x + w, y + h, w, h), Shape = AsolSamplePanel.ShapeType.Star8ObtuseAngles, CenterColor = Color.Cyan };
            _AsolPanel.Controls.Add(gp4);

            var sp1 = new NWC.SplitterManager() { Name = "SplitterVertical1", SplitPosition = 245, Orientation = Orientation.Vertical, OnTopMode = NWC.SplitterBar.SplitterOnTopMode.OnMouseEnter };
            sp1.ControlsBefore.Add(gp1);
            sp1.ControlsBefore.Add(gp2);
            sp1.ControlsAfter.Add(gp3);
            sp1.ControlsAfter.Add(gp4);
            sp1.SplitterColorByParent = false;
            sp1.DevExpressSkinEnabled = true;
            sp1.ActivityMode = NWC.SplitterBar.SplitterActivityMode.ResizeAfterMove;
            sp1.ApplySplitterToControls();
            sp1.SplitPositionChanging += _SplitterValueChanging;
            _AsolPanel.Controls.Add(sp1);

            sp1.AcceptBoundsToSplitter = true;
            sp1.Bounds = new Rectangle(100, 15, 14, 650);

            var sp2 = new NWC.SplitterManager() { Name = "SplitterHorizontal1", SplitPosition = 105, Orientation = Orientation.Horizontal, OnTopMode = NWC.SplitterBar.SplitterOnTopMode.None };
            sp2.ControlsBefore.Add(gp1);
            sp2.ControlsBefore.Add(gp3);
            sp2.ControlsAfter.Add(gp2);
            sp2.ControlsAfter.Add(gp4);
            sp2.ApplySplitterToControls();
            sp2.SplitPositionChanging += _SplitterValueChanging;
            _AsolPanel.Controls.Add(sp2);

            _Splitter3Panel = new Panel()
            {
                Name = "SpliterPanel",
                BackColor = Color.DarkGoldenrod
            };
            _AsolPanel.Controls.Add(_Splitter3Panel);

            _Splitter3 = new NWC.SplitterBar()
            {
                Name = _SplitterTransferName,
                SplitPosition = 240,
                Orientation = Orientation.Horizontal,
                DevExpressSkinEnabled = false,
                SplitterColorByParent = false,
                SplitterColor = Color.LightCoral,
                AnchorType = NWC.SplitterAnchorType.Relative,
                OnTopMode = NWC.SplitterBar.SplitterOnTopMode.OnMouseEnter,
                TransferToParentSelector = _SpliterPanelSearch,
                TransferToParentEnabled = true
            };
            _Splitter3.SplitPositionChanging += _Splitter3_SplitPositionChanging;
            _Splitter3.SplitPositionChanging += _SplitterValueChanging;
            _Splitter3.SplitInactiveRange = new Range<int>(16, -16);
            _Splitter3Panel.Controls.Add(_Splitter3);
            SetWorkingBounds(_AsolPanel);

            _SplitLabel = new Label() { AutoSize = false, Dock = DockStyle.Left, Width = _SplitterLabelWidth - 5, Text = "Splittery", Font = SystemFonts.StatusFont };
            _PanelSplitter.Controls.Add(_SplitLabel);

            _AsolPanel.Resize += X_Resize;
            _SetAsolPanelBounds();
        }

        private void _PanelSplitter_SizeChanged(object sender, EventArgs e)
        {
            _SetAsolPanelBounds();
        }
        private void _SetAsolPanelBounds()
        {
            int dx = _SplitterLabelWidth;
            Size totalSize = _PanelSplitter.ClientSize;
            _AsolPanel.Bounds = new Rectangle(dx, 0, totalSize.Width - dx, totalSize.Height);
        }
        private const int _SplitterLabelWidth = 140;
        private const string _SplitterTransferName = "SplitterTransfer";
        private void X_Resize(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                SetWorkingBounds(control);
            }
        }
        private void _SplitterValueChanging(object sender, TEventValueChangeArgs<double> e)
        {
            if (sender is NWC.SplitterBar splitter)
            {
                string eol = Environment.NewLine;
                var bounds = splitter.Bounds;
                if (splitter.Name == _SplitterTransferName) bounds = splitter.Parent.Bounds;
                var offset = _AsolPanel.AutoScrollPosition;
                string text = splitter.Name + eol +
                              "Position: " + splitter.SplitPosition + eol +
                              "OldValue: " + e.OldValue + eol +
                              "NewValue: " + e.NewValue + eol +
                              "X: " + bounds.X + eol +
                              "Y: " + bounds.Y + eol +
                              "Scroll.X: " + offset.X + eol +
                              "Scroll.Y: " + offset.Y + eol;
                _SplitLabel.Text = text;
            }
        }
        private void _Splitter3_SplitPositionChanging(object sender, TEventValueChangeArgs<double> e)
        {
            int newValue = (int)e.NewValue;
            int step = 1;
            int changedValue = (newValue < 50 ? 50 : (newValue > 420 ? 420 : newValue));
            changedValue = step * (changedValue / step);
            if (changedValue != newValue)
                e.NewValue = changedValue;
            var spr = _Splitter3.SplitPositionRange;
        }

        /// <summary>
        /// Tato metoda je volaná v situaci, kdy splitter má určit svého Parenta, kterým bude pohybovat.
        /// Tato metoda dostává tedy různé parenty našeho splitteru a určuje, který je ten správný.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private NWC.SplitterBar.SplitterParentSelectorMode _SpliterPanelSearch(Control control)
        {
            return ((control.Name == "SpliterPanel") ? NWC.SplitterBar.SplitterParentSelectorMode.Accept : NWC.SplitterBar.SplitterParentSelectorMode.SearchAnother);
        }
        private AsolPanel _AsolPanel;
        private Panel _Splitter3Panel;
        private NWC.SplitterBar _Splitter3;
        private Label _SplitLabel;
        private void SetWorkingBounds(Control control)
        {
            var clientSize = control.ClientSize;
            Rectangle workingBounds = new Rectangle(16, 10, clientSize.Width - 32, clientSize.Height - 20);
            // _Splitter3.WorkingBounds = workingBounds;
        }
        #endregion
        #region Animace
        private void InitAnimation()
        {
            string imgFile = @"D:\Asol\Práce\Tools\TestDevExpress\TestDevExpress\Images\Animated kitty.gif";
            if (System.IO.File.Exists(imgFile))
            {
                System.Windows.Forms.PictureBox pcb = new PictureBox();
                pcb.Image = System.Drawing.Bitmap.FromFile(imgFile);
                var size = pcb.Image.Size;
                pcb.SizeMode = PictureBoxSizeMode.Zoom;
                pcb.Bounds = new Rectangle(new Point(20, 20), new Size(size.Width / 2, size.Height / 2));
                pcb.BackColor = Color.Transparent;
                _PanelAnimation.Controls.Add(pcb);
            }
        }
        #endregion
        #region Ribbon a MDI Tabbed
        /// <summary>
        /// Iniciace ribbonu
        /// </summary>
        protected void InitMdiPage()
        {
            InitRibbon();
            InitMdiTab();
        }
        #region Ribbon : tvorba, eventy
        protected void InitRibbon()
        {
            _Ribbon = new RibbonControl();
            _Ribbon.RibbonItemClick += _Ribbon_RibbonItemClick;
            _PanelRibbon.Controls.Add(_Ribbon.Control);
            _RibbonBoundsText.Font = SystemFonts.StatusFont;
        }
        protected void ActivateRibbonPage()
        {
            if (!IsRibbonFilled)
            {
                IsRibbonFilled = true;

                var items = RibbonSample.CreateItems(14, out string info);
                _AddRibbonText(info);
                _RibbonAddItems(items, false);
            }
        }
        protected bool IsRibbonFilled = false;
        private void _Ribbon_RibbonItemClick(object sender, TEventArgs<IRibbonData> e)
        {
            if (e.Item is null) return;
            string line = $"RibbonItem.Click: {e.Item}";
            _AddRibbonText(line);
        }
        private void _RibbonClearBtn_Click(object sender, EventArgs e)
        {
            bool useFreeze = _RibbonFreezeCheck.Checked;

            _Ribbon.Freeze = useFreeze;

            string selectedPageId = _Ribbon.SelectedPageId;
            var items = RibbonSample.CreateItems(12, out string info);
            _AddRibbonText(info);

            _RibbonAddItems(items, true);
            _Ribbon.SelectedPageId = selectedPageId;

            _Ribbon.Freeze = false;
        }
        private void _RibbonAdd1Btn_Click(object sender, EventArgs e)
        {
            var items = RibbonSample.CreateItems(2, out string info);
            _AddRibbonText(info);
            _RibbonAddItems(items, false);
        }
        private void _RibbonAdd2Btn_Click(object sender, EventArgs e)
        {
            var items = RibbonSample.CreateItems(6, out string info);
            _AddRibbonText(info);
            _RibbonAddItems(items, false);
        }
        private void _RunMdiFormBtn_Click(object sender, EventArgs e)
        {
            using (var mdiParent = new MdiParentForm())
            {
                mdiParent.WindowState = FormWindowState.Maximized;
                mdiParent.ShowDialog();
            }
        }
        private void _RibbonResetTextBtn_Click(object sender, EventArgs e)
        {
            _DynamicPageSizeText = "";
            _RibbonBoundsText.Text = _DynamicPageSizeText;
        }
        private void _RibbonAddItems(List<IRibbonItem> items, bool clear = false)
        {
            DateTime begin = DateTime.Now;
            if (clear)
                _Ribbon.Clear();
            _Ribbon.AddItems(items);
            TimeSpan time = DateTime.Now - begin;
            _AddRibbonText((clear ? "Smazán Ribbon a vloženy " : "Přidány ") + $"prvky do Ribbonu v čase {time.TotalMilliseconds} milisec");
        }
        private void DynamicPage_SizeChanged(object sender, EventArgs e)
        {
            Rectangle bounds = this.DynamicPage.Bounds;
            int i = ++_DynamicPageSizeCount;
            string line = $"DynamicPage.Resize: {i}. X: {bounds.X}, Y: {bounds.Y}, W: {bounds.Width}, H: {bounds.Height}";
            _AddRibbonText(line);
        }
        private int _DynamicPageSizeCount = 0;
        private string _DynamicPageSizeText = "";
        private void _AddRibbonText(string line)
        {
            _DynamicPageSizeText += line + Environment.NewLine;
            _RibbonBoundsText.Text = _DynamicPageSizeText;
        }
        private TestDevExpress.RibbonControl _Ribbon;
        #endregion
        #region MDI Tabbed
        protected void InitMdiTab()
        {
            _MdiManager = new DXT.XtraTabbedMdiManager();
        }
        private DXT.XtraTabbedMdiManager _MdiManager;
        #endregion
        #endregion
        #region Resize
        private void InitResize()
        {
            _ChildResize = new PanelResize()
            {
                Bounds = new Rectangle(40, 40, 200, 100),
                BackColor = Color.FromArgb(255, 230, 240, 255),
                BorderStyle = BorderStyle.Fixed3D
            };

            _PanelResize.Controls.Add(_ChildResize);
            _PanelChildResizeSetPosition();
            _PanelResize.SizeChanged += _PanelResize_SizeChanged;
        }
        private void _PanelResize_SizeChanged(object sender, EventArgs e)
        {
            _PanelChildResizeSetPosition();
        }
        private void _PanelChildResizeSetPosition()
        {
            Rectangle clientBounds = _PanelResize.ClientRectangle;
            int d1 = 60;
            int d2 = 120;
            Rectangle childBounds = new Rectangle(clientBounds.X + d1, clientBounds.Y + d1, clientBounds.Width - d2, clientBounds.Height - d2);
            if (childBounds.Width < 10) childBounds.Width = 10;
            if (childBounds.Height < 10) childBounds.Height = 10;
            _ChildResize.Bounds = childBounds;
        }
        private PanelResize _ChildResize;
        #endregion
        #region Chart
        protected void InitChart()
        {
            NWC.ChartPanel chart = new NWC.ChartPanel() { Dock = DockStyle.Fill };
            chart.DataSource = NWC.ChartPanel.CreateSampleData();
            chart.ChartSettings = NWC.ChartPanel.CreateSampleSettings();
            _PanelChart.Controls.Add(chart);
        }
        #endregion
        #region MsgBox
        protected void InitMsgBox()
        {
            _MsgBoxPanel.AutoScroll = true;
            int x0 = 15;
            int y0 = 12;
            int xs = 20;
            int ys = 6;
            int w = 320;
            int h = 35;
            int x = x0;
            int y = y0;

            _CreateOneButton("Dialog [ OK ]", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKClick); y += (h + ys);
            _CreateOneButton("Dialog [ OK ] / Center", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKCenterClick); y += (h + ys);
            _CreateOneButton("Dialog [ OK ] / AutoCenter", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKAutoCenterClick); y += (h + ys);
            _CreateOneButton("Dialog Yes/No", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogYesNoClick); y += (h + ys);
            _CreateOneButton("Dialog Yes/No / Right", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogYesNoRightClick); y += (h + ys);
            _CreateOneButton("Dialog Abort/Retry/Ignore", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogAbortRetryIgnoreClick); y += (h + ys);
            _CreateOneButton("Dialog Abort/Retry/Ignore / Right", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogAbortRetryIgnoreRightRightClick); y += (h + ys);
            _CreateOneButton("Dialog Abort/Retry/Ignore / TopRight", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogAbortRetryIgnoreTopRightClick); y += (h + ys);
            _CreateOneButton("Dialog OK / HTML ", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKHtmlButtonClick); y += (h + ys);
            _CreateOneButton("Dialog Extra dlouhý text", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKExtraLongButtonClick); y += (h + ys);
            _CreateOneButton("Dialog Vícetlačítkový", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogMultiButtonButtonClick); y += (h + ys);

            x = x0 + w + xs;
            y = y0;

            _CreateOneButton("Otevři obyčejné okno", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgOpenStandardFormClick); y += (h + ys);
            _CreateOneButton("Otevři TopMost okno", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgOpenTopMostFormClick); y += (h + ys);
            _CreateOneButton("Otevři Modal okno", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgOpenModalFormClick); y += (h + ys);
           
            y += (h + ys);
            _CreateOneButton("Dialog InputTextLine", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogInputTextLineClick); y += (h + ys);
            _CreateOneButton("Dialog InputMemoLine", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogInputTextMemoClick); y += (h + ys);
            _CreateOneButton("Dialog NonModal", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogNonModalClick); y += (h + ys);

            _MsgBoxResultLabel = new Label() { Bounds = new Rectangle(x, y, w + 60, h), AutoSize = false, Text = "Výsledek: " };
            _MsgBoxPanel.Controls.Add(_MsgBoxResultLabel);
        }
        private Label _MsgBoxResultLabel;
        private void _MsgShowDialogOKClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs(LocalizerSK);
            dialogArgs.Title = "Dialog [OK]";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = RandomText.GetRandomSentences(4, 8, 3, 12);

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKCenterClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] Center";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Jistě, pane ministře.";
            dialogArgs.AutoCenterSmallText = true;
            //  dialogArgs.MessageHorizontalAlignment = NWC.AlignContentToSide.Center;
            //  dialogArgs.MessageVerticalAlignment = NWC.AlignContentToSide.Center;
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Center;
            dialogArgs.StatusBarVisible = false;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKAutoCenterClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] AutoCenter";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Tento text má být automaticky vystředěn, pokud je v jednom řádku a má dostatek místa.";
            dialogArgs.AutoCenterSmallText = true;
            //  dialogArgs.MessageHorizontalAlignment = NWC.AlignContentToSide.Center;
            //  dialogArgs.MessageVerticalAlignment = NWC.AlignContentToSide.Center;
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Center;
            dialogArgs.StatusBarVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogYesNoClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs(LocalizerEN, IconGenerator);
            dialogArgs.Title = "Dialog Yes/No";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Question;
            dialogArgs.MessageText = "Přejete si další chod k obědu?" + Environment.NewLine + RandomText.GetRandomSentences(4, 8, 3, 12);
            dialogArgs.PrepareButtons(DialogResult.Yes, DialogResult.No);
            dialogArgs.IconFile = "Quest";
            dialogArgs.Buttons[0].ImageFile = "Yes";
            dialogArgs.Buttons[1].ImageFile = "No";
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogYesNoRightClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs(LocalizerEN);
            dialogArgs.Title = "Dialog Yes/No / Right";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Question;
            dialogArgs.MessageText = "Přejete si další chod k obědu?" + Environment.NewLine + RandomText.GetRandomSentences(4, 8, 3, 12);
            dialogArgs.MessageHorizontalAlignment = NWC.AlignContentToSide.End;
            dialogArgs.MessageVerticalAlignment = NWC.AlignContentToSide.Center;
            dialogArgs.PrepareButtons(DialogResult.Yes, DialogResult.No);
            dialogArgs.Buttons[1].IsInitialButton = true;
            dialogArgs.ButtonPanelDock = DockStyle.Bottom;
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.End;
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = Zkopíruj";
            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogAbortRetryIgnoreClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs(LocalizerSK);
            dialogArgs.Title = "Dialog Abort/Retry/Ignore";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Error;
            dialogArgs.MessageText = "Došlo k chybě. Můžete zrušit celou akci, nebo zopakovat pokus, anebo tuto chybu ignorovat a pokračovat dál..." + Environment.NewLine + RandomText.GetRandomSentences(4, 8, 3, 12);
            dialogArgs.PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
            dialogArgs.Buttons[2].IsInitialButton = true;
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogAbortRetryIgnoreRightRightClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs(LocalizerSK);
            dialogArgs.Title = "Dialog Abort/Retry/Ignore / RightRight";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Exclamation;
            dialogArgs.MessageText = "Došlo k chybě. Můžete zrušit celou akci, nebo zopakovat pokus, anebo tuto chybu ignorovat a pokračovat dál..." + Environment.NewLine + RandomText.GetRandomSentences(4, 9, 8, 20);
            dialogArgs.MessageHorizontalAlignment = NWC.AlignContentToSide.End;
            dialogArgs.MessageVerticalAlignment = NWC.AlignContentToSide.End;
            dialogArgs.PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
            dialogArgs.ButtonPanelDock = DockStyle.Right;
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.End;
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogAbortRetryIgnoreTopRightClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs(LocalizerSK);
            dialogArgs.Title = "Dialog Abort/Retry/Ignore / TopRight";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Hand;
            dialogArgs.MessageText = "Došlo k chybě. Můžete zrušit celou akci, nebo zopakovat pokus, anebo tuto chybu ignorovat a pokračovat dál..." + Environment.NewLine + RandomText.GetRandomSentences(4, 9, 8, 20);
            dialogArgs.PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
            dialogArgs.ButtonPanelDock = DockStyle.Right;
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Begin;
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKHtmlButtonClick(object sender, EventArgs args)
        {
            string html = @"<size=14><b><color=255,96,96><backcolor=0,0,0>Doklad není uložen</backcolor></color></b><size=11>
Změny provedené do tohoto dokladu nejsou dosud uloženy do databáze.<br>
<b>Uložit</b> - změny budou uloženy a okno bude zavřeno<br>
<b>Neukládat</b> - změny se neuloží, okno bude zavřeno<br>
<b>Storno</b> - změny se neuloží, a okno zůstane otevřené<br>
<size=14><b>Co si přejete provést?</b><size=11>
 ";

            html = html.Replace("'", "\"");

            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] / HTML";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Shield;
            dialogArgs.MessageText = html;
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Uložit", ResultValue = "SAVE", StatusBarText = "Aktuální stav uloží do databáze", Image = Properties.Resources.document_save_24_});
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Neukládat", ResultValue = "DISCARD", StatusBarText = "Aktuální změny se zahodí", Image = Properties.Resources.document_revert_24_ });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Storno", ResultValue = "CANCEL", StatusBarText = "Nezavírat okno, neukládat změny", Image = Properties.Resources.edit_delete_9_24_ });
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKExtraLongButtonClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] ExtraLong";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.MessageText = RandomText.Text1;
            dialogArgs.PrepareButtons(DialogResult.OK, DialogResult.No);
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Center;
            dialogArgs.ButtonHeight = 26;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogMultiButtonButtonClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] ExtraLong";
            dialogArgs.Icon = Properties.Resources.help_hint_48_;
            dialogArgs.MessageText = "Více tlačítek";
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = COPY";
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.End;
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Zkopíruj do schránky", ResultValue = "COPY", StatusBarText = "Zobrazený text zkopíruje do schránky Windows, pak můžete Ctrl+V text vložit jinam.", Image = Properties.Resources.edit_copy_3_24_ });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Odešli mailem", ResultValue = "MAIL", StatusBarText = "Otevře novou mailovou zprávu, a do ní vloží tuto hlášku.", Image = Properties.Resources.document_import_24_ });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Otevři v prohlížeči", ResultValue = "VIEW", StatusBarText = "Otevře hlášku v internetovém prohlížeči. Netuším, jak.", Image = Properties.Resources.go_home_9_24_ });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Zavřít", IsEscapeButton = true, ResultValue = "EXIT", StatusBarText = "Zavře okno, zavře i okenice a zhasne v kamnech.", Image = Properties.Resources.edit_delete_6_24_ });
            // dialogArgs.StatusBarVisible = true;     nastaví se autodetekcí automaticky
            dialogArgs.ButtonHeight = 32;
            dialogArgs.UserZoomRatio = 1.15f;
            dialogArgs.DefaultResultValue = "CLOSE";

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogInputTextLineClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [InputTextLine]";
            dialogArgs.MessageText = "Zadejte prosím jedno svoje přání:";
            dialogArgs.InputTextType = NWC.ShowInputTextType.TextBox;
            dialogArgs.InputTextValue = "Moje přání je...";
            dialogArgs.InputTextStatusInfo = "Bez obav zadejte svoje přání, ale pozor - může se splnit!";
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = COPY";
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Begin;
            dialogArgs.PrepareButtons(DialogResult.OK, DialogResult.Cancel);
            dialogArgs.DefaultResultValue = DialogResult.Cancel;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogInputTextMemoClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [InputTextMemo]";
            dialogArgs.MessageText = "Zadejte prosím několik svých přání, jedno na jeden řádek:";
            dialogArgs.InputTextType = NWC.ShowInputTextType.MemoEdit;
            dialogArgs.InputTextValue = "Nemám přání...";
            dialogArgs.InputTextStatusInfo = "Bez obav zadejte svoje přání, ale pozor - může se splnit!";
            dialogArgs.InputTextSize = new Size(300, 70);
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = COPY";
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Begin;
            dialogArgs.PrepareButtons(DialogResult.OK, DialogResult.Cancel);
            dialogArgs.DefaultResultValue = DialogResult.Cancel;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogNonModalClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] NonModal";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Jistě, pane premiére.";
            dialogArgs.ButtonsAlignment = NWC.AlignContentToSide.Center;

            DialogFormNonModal(dialogArgs);
        }
        private Image IconGenerator(string iconName)
        {
            string key = iconName.ToLower();
            switch (key)
            {
                case "quest": return Properties.Resources.help_3_24_;
                case "yes": return Properties.Resources.dialog_ok_apply_2_24_;
                case "no": return Properties.Resources.dialog_no_2_24_;
            }
            return null;
        }
        private string LocalizerEN(string code)
        {
            string key = code.ToLower();
            switch (key)
            {
                case "ctrlctext": return "Ctrl+C";
                case "ctrlctooltip": return "Copies the entire text of this window (title, information and button texts) to the Windows clipboard.\r\nThen you can open a new mail and press Ctrl+V to copy the contents of this window.";
                case "ctrlcinfo": return "Copied to clipboard";
                case "dialogresult_ok": return "OK";
                case "dialogresult_cancel": return "Cancel";
                case "dialogresult_abort": return "Abort";
                case "dialogresult_retry": return "Retry";
                case "dialogresult_ignore": return "Ignore";
                case "dialogresult_yes": return "Yes";
                case "dialogresult_no": return "Oh, no";
            }
            return null;
        }
        private string LocalizerSK(string code)
        {
            string key = code.ToLower();
            switch (key)
            {
                case "ctrlctext": return "Ctrl+C";
                case "ctrlctooltip": return "Skopíruje do schránky Windows celý text tohto okna (titulok, informáciu i texty tlačidiel).\r\nPak je možné otvoriť nový mail a klávesom Ctrl + V doň opísať obsah tohto okna.";
                case "ctrlcinfo": return "Skopírované do schránky";
                case "dialogresult_ok": return "&Inu dobre";
                case "dialogresult_cancel": return "&Nekonaj";
                case "dialogresult_abort": return "&Zahoď";
                case "dialogresult_retry": return "&Ešte raz";
                case "dialogresult_ignore": return "&Nechaj tak";
                case "dialogresult_yes": return "Áno";
                case "dialogresult_no": return "&Nie";
            }
            return null;
        }
        private void DialogForm(NWC.DialogArgs dialogArgs)
        {
            dialogArgs.Owner = OwnerWindow;
            var result = NWC.DialogForm.ShowDialog(dialogArgs);
            
            string text = $"Výsledek dialogu je: [{result}]";
            if (dialogArgs.InputTextType != NWC.ShowInputTextType.None)
                text += $"; Text: [{dialogArgs.InputTextValue}]";
            _MsgBoxResultLabel.Text = text;
        }
        private void DialogFormNonModal(NWC.DialogArgs dialogArgs)
        {
            dialogArgs.Owner = OwnerWindow;
            NWC.DialogForm.Show(dialogArgs, DialogCallback);
        }
        private void DialogCallback(object sender, NWC.DialogFormClosingArgs args)
        {
            var dialogArgs = args.DialogArgs;
            object result = dialogArgs.ResultValue;
            string text = $"Výsledek dialogu je: [{result}]";
            if (dialogArgs.InputTextType != NWC.ShowInputTextType.None)
                text += $"; Text: [{dialogArgs.InputTextValue}]";
            _MsgBoxResultLabel.Text = text;
        }
        private Form OwnerWindow
        {
            get
            {
                var form = _LastWindow;
                if (form != null && form.TryGetTarget(out Form lastWindow)) return lastWindow;
                _LastWindow = null;
                return this;
            }
        }
        private WeakReference<Form> _LastWindow;
        private void _MsgOpenStandardFormClick(object sender, EventArgs args)
        {
            _MsgOpenShowForm(" [STANDARD]", false, false);
        }
        private void _MsgOpenTopMostFormClick(object sender, EventArgs args)
        {
            _MsgOpenShowForm(" [TOPMOST]", true, false);
        }
        private void _MsgOpenModalFormClick(object sender, EventArgs args)
        {
            _MsgOpenShowForm(" [MODAL]", true, true);
        }
        private void _MsgOpenShowForm(string subTitle, bool topMost, bool asModal)
        {
            Rectangle bounds = GetRandomRectangle();
            string caption = RandomText.GetRandomSentence(2, 5) + subTitle;
            string text = RandomText.GetRandomSentences(4, 8, 3, 12);
            DevExpress.XtraEditors.XtraForm form = new DevExpress.XtraEditors.XtraForm() { Bounds = bounds, Text = caption, TopMost = topMost, ShowInTaskbar = topMost };
            Label label = new Label() { Text = text, Name = "Label", AutoSize = false, Bounds = new Rectangle(12, 9, bounds.Width - 28, bounds.Height - 30), Font = SystemFonts.DialogFont };
            form.Controls.Add(label);
            SampleForm_SetLabelBounds(form, label);
            form.ClientSizeChanged += SampleForm_ClientSizeChanged;
            _LastWindow = new WeakReference<Form>(form);
            if (asModal)
                form.ShowDialog(this);
            else
                form.Show(this);
        }

        private void SampleForm_ClientSizeChanged(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form is null) return;
            int index = form.Controls.IndexOfKey("Label");
            if (index < 0) return;
            Label label = form.Controls[index] as Label;
            if (label is null) return;
            SampleForm_SetLabelBounds(form, label);
        }

        private void SampleForm_SetLabelBounds(Form form, Label label)
        {
            Size clientSize = form.ClientSize;
            Rectangle labelBounds = new Rectangle(12, 9, clientSize.Width - 24, clientSize.Height - 18);
            label.Bounds = labelBounds;
        }

        /// <summary>
        /// Vytvoří a vrátí Button
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="parent"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        protected Button _CreateOneButton(string text, Rectangle bounds, Control parent, EventHandler clickHandler)
        {
            Button button = new Button() { Bounds = bounds, Text = text };
            if (parent != null) parent.Controls.Add(button);
            if (clickHandler != null) button.Click += clickHandler;
            return button;
        }
        #endregion
        #region Editors
        protected void InitEditors()
        {
            _TokenLabel = new DevExpress.XtraEditors.LabelControl() { Bounds = new Rectangle(25, 12, 250, 20), Text = "Zvolte počet prvků k přidání a stiskněte 'Generuj'" };
            _EditorsPanel.Controls.Add(_TokenLabel);

            _TokenCountSpin = new DevExpress.XtraEditors.SpinEdit() { Bounds = new Rectangle(20, 40, 90, 20), Value = 5000m };
            _TokenCountSpin.Properties.MinValue = 500m;
            _TokenCountSpin.Properties.MaxValue = 1000000m;
            _TokenCountSpin.Properties.EditMask = "### ### ##0";
            _TokenCountSpin.Properties.SpinStyle = DevExpress.XtraEditors.Controls.SpinStyles.Horizontal;
            _TokenCountSpin.Properties.Increment = 500m;

            _EditorsPanel.Controls.Add(_TokenCountSpin);

            _TokenAddButtonGreen = new DevExpress.XtraEditors.SimpleButton() { Bounds = new Rectangle(130, 37, 120, 28), Text = "Generuj GREEN" };
            _TokenAddButtonGreen.Click += _TokenAddButtonGreen_Click;
            _EditorsPanel.Controls.Add(_TokenAddButtonGreen);

            _TokenAddButtonDaj = new DevExpress.XtraEditors.SimpleButton() { Bounds = new Rectangle(260, 37, 120, 28), Text = "Generuj DAJ" };
            _TokenAddButtonDaj.Click += _TokenAddButtonDaj_Click;
            _EditorsPanel.Controls.Add(_TokenAddButtonDaj);
            

            _TokenEdit = new DevExpress.XtraEditors.TokenEdit() { Bounds = new Rectangle(20, 68, 350, 25) };
            _EditorsPanel.Controls.Add(_TokenEdit);

            _TokenInfoLabel = new DevExpress.XtraEditors.LabelControl { Bounds = new Rectangle(25, 100, 250, 20), Text = "" };
            _EditorsPanel.Controls.Add(_TokenInfoLabel);
        }

        private void _TokenAddButtonGreen_Click(object sender, EventArgs e)
        {
            _TokenInfoLabel.Text = "probíhá příprava dat...";
            _TokenInfoLabel.Refresh();

            DateTime time0 = DateTime.Now;
            int count = (int)_TokenCountSpin.Value;
            var tokens = CreateTokenTuples(count);

            DateTime time1 = DateTime.Now;
            this._TokenEdit.Properties.Tokens.Clear();

            DateTime time2 = DateTime.Now;
            this._TokenEdit.Properties.BeginUpdate();
            foreach (var token in tokens)
            {
                this._TokenEdit.Properties.Tokens.AddToken(token.Item1, token.Item2);
            }
            this._TokenEdit.Properties.EndUpdate();
            DateTime time3 = DateTime.Now;

            string diff1 = ((TimeSpan)(time1 - time0)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff2 = ((TimeSpan)(time2 - time1)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff3 = ((TimeSpan)(time3 - time2)).TotalMilliseconds.ToString("### ### ##0").Trim();
            string message = $"Počet: {count}, Generátor: {diff1} ms, Clear: {diff2} ms, AddTokens: {diff3} ms";
            _TokenInfoLabel.Text = message;

            string tab = "\t";
            string clip = count.ToString() + tab + diff1.Replace(" ", "") + tab + diff2.Replace(" ", "") + tab + diff3.Replace(" ", "");
            Clipboard.Clear();
            Clipboard.SetText(clip);

        }
        private List<Tuple<string, int>> CreateTokenTuples(int count)
        {
            List<Tuple<string, int>> tokens = new List<Tuple<string, int>>();
            for (int n = 0; n < count; n++)
            {
                string text = RandomText.GetRandomSentence(1, 4, false);
                tokens.Add(new Tuple<string, int>(text, n));
            }
            return tokens;
        }

        private void _TokenAddButtonDaj_Click(object sender, EventArgs e)
        {
            _TokenInfoLabel.Text = "probíhá příprava dat...";
            _TokenInfoLabel.Refresh();

            DateTime time0 = DateTime.Now;
            int count = (int)_TokenCountSpin.Value;
            var tokens = CreateTokens(count);

            DateTime time1 = DateTime.Now;
            this._TokenEdit.Properties.Tokens.Clear();

            DateTime time2 = DateTime.Now;
            this._TokenEdit.Properties.BeginUpdate();
            this._TokenEdit.Properties.Tokens.AddRange(tokens);
            this._TokenEdit.Properties.EndUpdate();
            DateTime time3 = DateTime.Now;

            string diff1 = ((TimeSpan)(time1 - time0)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff2 = ((TimeSpan)(time2 - time1)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff3 = ((TimeSpan)(time3 - time2)).TotalMilliseconds.ToString("### ### ##0").Trim();
            string message = $"Počet: {count}, Generátor: {diff1} ms, Clear: {diff2} ms, AddTokens: {diff3} ms";
            _TokenInfoLabel.Text = message;

            string tab = "\t";
            string clip = count.ToString() + tab + diff1.Replace(" ", "") + tab + diff2.Replace(" ", "") + tab + diff3.Replace(" ", "");
            Clipboard.Clear();
            Clipboard.SetText(clip);

        }
        private List<DevExpress.XtraEditors.TokenEditToken> CreateTokens(int count)
        {
            List<DevExpress.XtraEditors.TokenEditToken> tokens = new List<DevExpress.XtraEditors.TokenEditToken>();
            for (int n = 0; n < count; n++)
            {
                string text = RandomText.GetRandomSentence(1, 4, false);
                tokens.Add(new DevExpress.XtraEditors.TokenEditToken(text, n));
            }
            return tokens;
        }


        private DevExpress.XtraEditors.LabelControl _TokenLabel;
        private DevExpress.XtraEditors.SpinEdit _TokenCountSpin;
        private DevExpress.XtraEditors.SimpleButton _TokenAddButtonGreen;
        private DevExpress.XtraEditors.SimpleButton _TokenAddButtonDaj;
        private DevExpress.XtraEditors.TokenEdit _TokenEdit;
        private DevExpress.XtraEditors.LabelControl _TokenInfoLabel;
        #endregion
        #region TreeView
        protected void InitTreeView()
        {
            CreateTreeViewComponents();
        }


        private void CreateTreeViewComponents()
        {
            CreateImageList();
            CreateTreeView();
        }
        private void CreateImageList()
        {
            _Images16 = new ImageList();
            _Images16.Images.Add("Ball01_16", Properties.Resources.Ball01_16);
            _Images16.Images.Add("Ball02_16", Properties.Resources.Ball02_16);
            _Images16.Images.Add("Ball03_16", Properties.Resources.Ball03_16);
            _Images16.Images.Add("Ball04_16", Properties.Resources.Ball04_16);
            _Images16.Images.Add("Ball05_16", Properties.Resources.Ball05_16);
            _Images16.Images.Add("Ball06_16", Properties.Resources.Ball06_16);
            _Images16.Images.Add("Ball07_16", Properties.Resources.Ball07_16);
            _Images16.Images.Add("Ball08_16", Properties.Resources.Ball08_16);
            _Images16.Images.Add("Ball09_16", Properties.Resources.Ball09_16);
            _Images16.Images.Add("Ball10_16", Properties.Resources.Ball10_16);
            _Images16.Images.Add("Ball11_16", Properties.Resources.Ball11_16);
            _Images16.Images.Add("Ball12_16", Properties.Resources.Ball12_16);
            _Images16.Images.Add("Ball13_16", Properties.Resources.Ball13_16);
            _Images16.Images.Add("Ball14_16", Properties.Resources.Ball14_16);
            _Images16.Images.Add("Ball15_16", Properties.Resources.Ball15_16);
            _Images16.Images.Add("Ball16_16", Properties.Resources.Ball16_16);
            _Images16.Images.Add("Ball17_16", Properties.Resources.Ball17_16);
            _Images16.Images.Add("Ball18_16", Properties.Resources.Ball18_16);
            _Images16.Images.Add("Ball19_16", Properties.Resources.Ball19_16);
            _Images16.Images.Add("Ball20_16", Properties.Resources.Ball20_16);
            _Images16.Images.Add("Ball21_16", Properties.Resources.Ball21_16);
            _Images16.Images.Add("Ball22_16", Properties.Resources.Ball22_16);
            _Images16.Images.Add("Ball23_16", Properties.Resources.Ball23_16);

            _Images16.Images.Add("edit_add_4_16", Properties.Resources.edit_add_4_16);
            _Images16.Images.Add("list_add_3_16", Properties.Resources.list_add_3_16);
            _Images16.Images.Add("lock_5_16", Properties.Resources.lock_5_16);
            _Images16.Images.Add("object_locked_2_16", Properties.Resources.object_locked_2_16);
            _Images16.Images.Add("object_unlocked_2_16", Properties.Resources.object_unlocked_2_16);
            _Images16.Images.Add("msn_blocked_16", Properties.Resources.msn_blocked_16);
            _Images16.Images.Add("hourglass_16", Properties.Resources.hourglass_16);
        }
        private int GetImageIndex(string imageName)
        {
            return (_Images16.Images.ContainsKey(imageName) ? _Images16.Images.IndexOfKey(imageName) : -1);
        }
        ImageList _Images16;
        private void CreateTreeView()
        {
            _SplitContainer = DxComponent.CreateDxSplitContainer(this._TreeViewPanel, null, DockStyle.Fill, Orientation.Vertical, DevExpress.XtraEditors.SplitFixedPanel.Panel1, 280, showSplitGlyph: true);

            _TreeList = new DxTreeViewListSimple() { Dock = DockStyle.Fill };
            _TreeList.SelectImageList = _Images16;
            _TreeList.StateImageList = _Images16;
            _TreeList.ImageIndexSearcher = GetImageIndex;
            _TreeList.LazyLoadNodeText = "Copak to tu asi bude?";
            _TreeList.LazyLoadNodeImageName = "hourglass_16";
            _TreeList.EditorShowMode = DevExpress.XtraTreeList.TreeListEditorShowMode.Click;

            _TreeList.Parent = this;
            _SplitContainer.Panel1.Controls.Add(_TreeList);               // Musí být dřív než se začne pracovat s daty!!!

            DateTime t0 = DateTime.Now;
            var nodes = _CreateSampleList(ItemCountType.Big);
            DateTime t1 = DateTime.Now;
            _TreeList.AddNodes(nodes);
            DateTime t2 = DateTime.Now;

            _TreeList.NodeSelected += _TreeList_AnyAction;
            _TreeList.NodeDoubleClick += _TreeList_AnyAction;
            _TreeList.NodeExpanded += _TreeList_AnyAction;
            _TreeList.NodeCollapsed += _TreeList_AnyAction;
            _TreeList.ActivatedEditor += _TreeList_AnyAction;
            _TreeList.EditorDoubleClick += _TreeList_AnyAction;
            _TreeList.NodeEdited += _TreeList_NodeEdited;
            _TreeList.NodeDelete += _TreeList_NodeDelete;
            _TreeList.LazyLoadChilds += _TreeList_LazyLoadChilds;

            int y = 0;
            _MemoEdit = DxComponent.CreateDxMemoEdit(0, ref y, 100, 100, this._SplitContainer.Panel2, readOnly: true);
            _MemoEdit.Dock = DockStyle.Fill;
            _LogId = 0;
            _Log = "";

            string line = "Počet nodů: " + nodes.Count.ToString();
            _AddLogLine(line);
            line = "Tvorba nodů: " + ((TimeSpan)(t1 - t0)).TotalMilliseconds.ToString("##0.000") + " ms";
            _AddLogLine(line);
            line = "Plnění do TreeView: " + ((TimeSpan)(t2 - t1)).TotalMilliseconds.ToString("##0.000") + " ms";
            _AddLogLine(line);
        }
        private void _TreeList_AnyAction(object sender, DxTreeViewNodeArgs args) { _AddLog(args.Action.ToString(), args, (args.Action == TreeViewActionType.NodeEdited || args.Action == TreeViewActionType.EditorDoubleClick)); }
        private void _TreeList_LazyLoadChilds(object sender, DxTreeViewNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _LoadChildNodesFromServerBgr(args));
        }
        private void _LoadChildNodesFromServerBgr(DxTreeViewNodeArgs args)
        {
            string parentNodeId = args.NodeItemInfo.NodeId;
            _AddLogLine($"Načítám data pro node '{parentNodeId}'...");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            // Upravíme hodnoty v otevřeném nodu:
            string text = args.NodeItemInfo.Text;
            if (text.EndsWith(" ..."))
            {
                args.NodeItemInfo.Text = text.Substring(0, text.Length - 4);
                args.NodeItemInfo.Refresh();
            }

            // Vytvoříme ChildNodes a zobrazíme je:
            bool empty = (RandomText.Rand.Next(10) > 7);
            var nodes = _CreateSampleChilds(parentNodeId, ItemCountType.Standard);       // A pak vyrobíme Child nody
            _AddLogLine($"Načtena data: {nodes.Count} prvků.");
            _TreeList.AddLazyLoadNodes(parentNodeId, nodes);            //  a pošleme je do TreeView.
        }
        private void _TreeList_NodeEdited(object sender, DxTreeViewNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeEditedBgr(args));
        }
        private void _TreeNodeEditedBgr(DxTreeViewNodeArgs args)
        {
            var nodeInfo = args.NodeItemInfo;
            string nodeId = nodeInfo.NodeId;
            string parentNodeId = nodeInfo.ParentNodeId;
            string oldValue = nodeInfo.Text;
            string newValue = (args.EditedValue is string text ? text : "");
            _AddLogLine($"Změna textu pro node '{nodeId}': '{oldValue}' => '{newValue}'");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            if (String.IsNullOrEmpty(newValue))
            {   // Delete node:
                if (nodeInfo.CanDelete)
                    _TreeList.RemoveNode(nodeId);
            }
            else if (oldValue == "")
            {   // Insert node:
                _TreeList.RunInLock(new Action<NodeItemInfo>(node =>
                {   // V jednom vizuálním zámku:
                    _TreeList.RemoveNode(node.NodeId);              // Odeberu blank node, to kvůli pořadí: nový blank přidám nakonec

                    // Přidám nový node pro konkrétní text = jakoby záznam:
                    NodeItemInfo newNode = _CreateChildNode(node.ParentNodeId, false);
                    newNode.Text = newValue;
                    _TreeList.AddNode(newNode);

                    // Přidám Blank node, ten bude opět na konci Childs:
                    NodeItemInfo blankNode = _CreateChildNode(node.ParentNodeId, true);
                    _TreeList.AddNode(blankNode);

                    // Aktivuji editovaný node:
                    _TreeList.SelectNode(newNode);
                }
                ), nodeInfo);
            }
            else
            {   // Edited node:
                args.NodeItemInfo.Text = newValue + " [OK]";
                args.NodeItemInfo.Refresh();
            }
        }
        private void _TreeList_NodeDelete(object sender, DxTreeViewNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeDeleteBgr(args));
        }
        private void _TreeNodeDeleteBgr(DxTreeViewNodeArgs args)
        {
            string nodeId = args.NodeItemInfo.NodeId;

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            _TreeList.RemoveNode(nodeId);
        }
        private void _AddLog(string actionName, DxTreeViewNodeArgs args, bool showValue = false)
        {
            string value = (showValue ? ", Value: " + (args.EditedValue == null ? "NULL" : "'" + args.EditedValue.ToString() + "'") : "");
            _AddLogLine($"{actionName}: Node: {args.NodeItemInfo}{value}");
        }
        private void _AddLogLine(string line)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string>(_AddLogLine), line); return; }

            int id = ++_LogId;
            string log = id.ToString() + ". " + line + Environment.NewLine + _Log;
            _Log = log;
            _MemoEdit.Text = log;
        }
        int _InternalNodeId;
        private List<NodeItemInfo> _CreateSampleList(ItemCountType countType = ItemCountType.Standard)
        {
            List<NodeItemInfo> list = new List<NodeItemInfo>();

            int rootCount = GetItemCount(countType, false);
            for (int r = 0; r < rootCount; r++)
            {
                bool isLazy = (RandomText.Rand.Next(10) >= 5);
                bool addChilds = !isLazy && (RandomText.Rand.Next(10) >= 3);
                bool isExpanded = (addChilds && (RandomText.Rand.Next(10) >= 2));

                string rootKey = "R." + (++_InternalNodeId).ToString();
                string text = RandomText.GetRandomSentence(2, 5) + (isLazy ? " ..." : "");
                FontStyle fontStyleDelta = FontStyle.Bold;
                NodeItemInfo rootNode = new NodeItemInfo(rootKey, null, text, expanded: isExpanded, lazyLoadChilds: isLazy, fontStyleDelta: fontStyleDelta);
                _FillNode(rootNode);
                list.Add(rootNode);

                if (addChilds)
                    list.AddRange(_CreateSampleChilds(rootKey, countType));
            }
            return list;
        }
        private List<NodeItemInfo> _CreateSampleChilds(string parentKey, ItemCountType countType = ItemCountType.Standard)
        {
            List<NodeItemInfo> list = new List<NodeItemInfo>();

            int childCount = GetItemCount(countType, true);
            bool addEditable = (RandomText.Rand.Next(20) >= 8);
            if (addEditable) childCount++;
            for (int c = 0; c < childCount; c++)
            {
                bool isBlankNode = (addEditable && c == (childCount - 1));
                list.Add(_CreateChildNode(parentKey, isBlankNode));
            }
            return list;
        }
        private NodeItemInfo _CreateChildNode(string parentKey, bool isBlankNode)
        {
            string childKey = "C." + (++_InternalNodeId).ToString();
            NodeItemInfo childNode = null;
            if (isBlankNode)
            {
                string text = "";
                childNode = new NodeItemInfo(childKey, parentKey, text, canEdit: true, canDelete: false);          // Node pro přidání nového prvku (Blank) nelze odstranit
                childNode.ToolTipText = "Zadejte referenci nového prvku";
                childNode.ImageName1 = "list_add_3_16";
            }
            else
            {
                string text = RandomText.GetRandomSentence(2, 5);
                childNode = new NodeItemInfo(childKey, parentKey, text, canEdit: true, canDelete: true);
                _FillNode(childNode);
            }
            return childNode;
        }
        private void _FillNode(NodeItemInfo node)
        {
            if (RandomText.Rand.Next(20) >= 15)
                node.ImageName0 = "object_locked_2_16";

            string imageNumb = RandomText.Rand.Next(1, 24).ToString("00");
            node.ImageName1 = $"Ball{imageNumb }_16";

            node.ToolTipTitle = null; // RandomText.GetRandomSentence(2, 5);
            node.ToolTipText = RandomText.GetRandomSentence(10, 50);
        }
        private int GetItemCount(ItemCountType countType, bool forChilds)
        {
            switch (countType)
            {
                case ItemCountType.Empty: return 0;
                case ItemCountType.Standard: return (forChilds ? RandomText.Rand.Next(1, 12) : RandomText.Rand.Next(10, 30));
                case ItemCountType.Big: return (forChilds ? RandomText.Rand.Next(40, 120) : RandomText.Rand.Next(120, 400));
            }
            return 0;
        }
        private enum ItemCountType { Empty, Standard, Big }
        DxSplitContainerControl _SplitContainer;
        DxTreeViewListSimple _TreeList;
        DxMemoEdit _MemoEdit;
        string _Log;
        int _LogId;
        #endregion
        #region Random
        Random Rand;
        protected int GetRandomInt(int min, int max)
        {
            return Rand.Next(min, max);
        }
        protected Size GetRandomSize()
        {
            int w = 25 * GetRandomInt(14, 35);
            int h = 25 * GetRandomInt(6, 21);
            return new Size(w, h);
        }
        protected Point GetRandomPoint()
        {
            int x = 25 * GetRandomInt(2, 16);
            int y = 25 * GetRandomInt(2, 14);
            return new Point(x, y);
        }
        protected Rectangle GetRandomRectangle()
        {
            return new Rectangle(GetRandomPoint(), GetRandomSize());
        }
        #endregion
    }
}