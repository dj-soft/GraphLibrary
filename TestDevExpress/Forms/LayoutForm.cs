using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using WF = System.Windows.Forms;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using Noris.Clients.Win.Components.AsolDX.DxLayout;
using TestDevExpress.Forms;
using Noris.Clients.Win.Components;
using DevExpress.PivotGrid.OLAP;

namespace TestDevExpress.Components
{
    /// <summary>
    /// Testovací okno pro Layout
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "Layout", buttonOrder: 20, buttonImage: "devav/layout/pages.svg", buttonToolTip: "Otevře okno pro testování layoutu (pod-okna)")]
    public class LayoutForm : DxRibbonForm
    {
        #region Konstruktor a tvorba obsahu okna
        /// <summary>
        /// Konstruktor
        /// </summary>
        public LayoutForm()
        {
            this.ImageName = "devav/layout/pages.svg";
            this.ImageNameAdd = SvgImageTextIcon.CreateImageName("Ly", true, null, "#006622", null, "#CCFFEE", true, "88FFAA", null, true, 1, 1, 32);
            this.Text = $"Test DxLayoutPanel";

            var resourcesSvg = DxComponent.GetResourceNames(".svg", true, false);
            _ImageNames = Randomizer.GetItems(36, resourcesSvg);

            this._Timer = new Timer() { Interval = 1800 };
            this._Timer.Tick += _Timer_Tick;
            this._Timer.Enabled = false;

            _ActivateIconSet(LayoutIconSetType.Default);
            _ActivateDockButtons(ControlVisibility.OnMouse);
        }
        #region MainContent
        protected override void DxMainContentPrepare()
        {
            base.DxMainContentPrepare();

            _LayoutPanel = new DxLayoutPanel()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                SplitterContextMenuEnabled = true,
                DragDropEnabled = true,
                DockButtonVisibility = ControlVisibility.OnMouse,
                CloseButtonVisibility = ControlVisibility.Allways,     // ControlVisibility.OnNonPrimaryPanelAllways,
                EmptyPanelButtons = EmptyPanelVisibleButtons.Close,
                DockButtonLeftToolTip = "Přemístit tento panel doleva",
                DockButtonTopToolTip = "Přemístit tento panel nahoru",
                DockButtonBottomToolTip = "Přemístit tento panel dolů",
                DockButtonRightToolTip = "Přemístit tento panel doprava",
                CloseButtonToolTip = "Zavřít tento panel",
                UseSvgIcons = true,
                IconLayoutsSet = LayoutIconSetType.Default,
                UseDxPainter = true
            };
            _LayoutPanel.UserControlAdd += _LayoutPanel_UserControlAdd;
            _LayoutPanel.LastControlRemoved += _LayoutPanel_LastControlRemoved;
            _LayoutPanel.SplitterPositionChanged += _LayoutPanel_SplitterPositionChanged;
            _LayoutPanel.LayoutPanelChanged += _LayoutPanel_LayoutPanelChanged;
            _LayoutPanel.XmlLayoutChanged += _LayoutPanel_XmlLayoutChanged;

            this.DxMainPanel.Controls.Add(_LayoutPanel);

            this._DoAddNewPanel();
        }
        #endregion
        #region Ribbon a StatusBar - obsah a rozcestník
        /// <summary>
        /// Připraví obsah Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.Text = "LayoutForm";

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);
            pages.Add(page);

            group = new DataRibbonGroup() { GroupId = "DxLayout", GroupText = "DX Layout" };
            page.Groups.Add(group);

            string dxLayoutCopy = "svgimages/edit/copy.svg";
            string dxLayoutPaste = "svgimages/edit/paste.svg";
            string dxLayoutClear = "svgimages/dashboards/edit.svg";

            string dxLayoutSet = "svgimages/richedit/nextfootnote.svg";

            string dxLayoutAdd = "devav/actions/add.svg";
            string dxLayoutClose = "devav/actions/close.svg";

            string copyImage = "svgimages/edit/copy.svg";
            string scanStructImageName = "svgimages/xaf/action_chart_printing_preview.svg";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Copy", Text = "Copy XmlLayout", ToolTipText = "Zkopíruje aktuální XML layout do schránky", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutCopy, RibbonStyle = RibbonItemStyles.SmallWithText });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Paste", Text = "Paste XmlLayout", ToolTipText = "Vloží text ze schránky do XML layoutu", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutPaste, RibbonStyle = RibbonItemStyles.SmallWithText });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Clear", Text = "Clear layout", ToolTipText = "Smaže celý layout", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutClear, RibbonStyle = RibbonItemStyles.SmallWithText });

            group.Items.Add(_CreateIconSetRibbonItem());
            group.Items.Add(_CreateDockButtonsVisibilityRibbonItem());

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Set1", Text = "Set Layout 1", ToolTipText = "Vloží předdefinovaný layout 1", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutSet, RibbonStyle = RibbonItemStyles.SmallWithText, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Set2", Text = "Set Layout 2", ToolTipText = "Vloží předdefinovaný layout 2", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutSet, RibbonStyle = RibbonItemStyles.SmallWithText });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Set3", Text = "Set Layout 3", ToolTipText = "Vloží předdefinovaný layout 3", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutSet, RibbonStyle = RibbonItemStyles.SmallWithText });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Set4", Text = "Set Layout 4", ToolTipText = "Vloží předdefinovaný layout 4", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutSet, RibbonStyle = RibbonItemStyles.SmallWithText });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Add", Text = "Add Default", ToolTipText = "Přidá nový panel do výchozí polohy", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutAdd, RibbonStyle = RibbonItemStyles.SmallWithText, ItemIsFirstInGroup = false});
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.Close", Text = "Close Panel", ToolTipText = "Zavře aktivní panel", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutClose, RibbonStyle = RibbonItemStyles.SmallWithText});

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.AddToCP1", Text = "Add ToCP1", ToolTipText = "Přidá nový panel explicitně do Area C/P1. Pokud v cílovém prostoru něco bude, dojde k chybě.", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutAdd, RibbonStyle = RibbonItemStyles.SmallWithText, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.AddToCP2", Text = "Add ToCP2", ToolTipText = "Přidá nový panel explicitně do Area C/P2. Pokud v cílovém prostoru něco bude, bude původní obsah odebrán v režimu Hide.", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutAdd, RibbonStyle = RibbonItemStyles.SmallWithText });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.RemoveHidden", Text = "Remove hidden", ToolTipText = "Najde první Hidden panel (ten, který byl zavřen v režimu HideControlAndKeepTile) a odebere jej z layoutu. Jde o nevizuální operaci, kdy je fyzicky odebrán skrytý panel ze struktury.", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = dxLayoutClose, RibbonStyle = RibbonItemStyles.SmallWithText });

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Layout.ScanStruct", Text = "Scan control", ToolTipText = "Zmapuje aktuální strukturu controlu LayoutPanel a vloží ji do Clipboardu", ItemType = RibbonItemType.Button, RadioButtonGroupName = "CountGroup", ImageName = scanStructImageName, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });

            this.DxRibbon.AddPages(pages, true);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private DataRibbonItem _CreateIconSetRibbonItem()
        {
            string iconSetImage = "svgimages/outlook%20inspired/fittopage.svg";
            var button = new DataRibbonItem() { ItemId = "Dx.Layout.IconSet", ItemType = RibbonItemType.Menu, ImageName = iconSetImage, Text = "IconSet", ToolTipTitle = "Volba sady ikon", ToolTipText = "Volba ikon pro tlačítka pro dokování", RibbonStyle = RibbonItemStyles.SmallWithText };
            button.SubItems = new ListExt<IRibbonItem>();
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.0", Text = "Default", Tag = LayoutIconSetType.Default });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.1", Text = "Align", Tag = LayoutIconSetType.Align });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.2", Text = "Legend", Tag = LayoutIconSetType.DashboardLegend });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.4", Text = "Arrow1", Tag = LayoutIconSetType.IconBuilderArrow1 });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.5", Text = "Arrow2", Tag = LayoutIconSetType.IconBuilderArrow2 });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.6", Text = "Arrow3", Tag = LayoutIconSetType.IconBuilderArrow3 });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.7", Text = "Arrow4", Tag = LayoutIconSetType.IconBuilderArrow4 });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.8", Text = "SprFill", Tag = LayoutIconSetType.SpreadsheetFill });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.IconSet.9", Text = "SprChartLeg", Tag = LayoutIconSetType.SpreadsheetChartLegend });
            __IconSetRibbonItem = button;
            return button;
        }
        private void _ActivateIconSet(LayoutIconSetType iconSet)
        {
            DataRibbonItem ribbonMenu = __IconSetRibbonItem;
            DataRibbonItem activeItem = null;
            foreach (var iSubItem in ribbonMenu.SubItems)
            {
                var subItem = iSubItem as DataRibbonItem;
                var itemSet = (LayoutIconSetType)subItem.Tag;
                var isActive = (itemSet == iconSet);
                if (isActive)
                    activeItem = subItem;

                var itemStyle = isActive ? FontStyle.Bold : FontStyle.Regular;
                if (subItem.FontStyle != itemStyle)
                {
                    subItem.FontStyle = itemStyle;
                    subItem.Refresh();
                }
            }

            if (activeItem != null)
            {
                ribbonMenu.Text = activeItem.Text;
                ribbonMenu.Refresh();
            }

            this._LayoutPanel.IconLayoutsSet = iconSet;
            this._LayoutPanel.Refresh();
        }
        private DataRibbonItem _CreateDockButtonsVisibilityRibbonItem()
        {
            string iconSetImage = "svgimages/outlook%20inspired/fittopage.svg";
            var button = new DataRibbonItem() { ItemId = "Dx.Layout.DockButtons", ItemType = RibbonItemType.Menu, ImageName = iconSetImage, Text = "DockButons", ToolTipTitle = "Volba viditelnosti Dock buttonů", ToolTipText = "Volba typu viditelnosti tlačítek pro dokování oken", RibbonStyle = RibbonItemStyles.SmallWithText };
            button.SubItems = new ListExt<IRibbonItem>();
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.DockButtons.0", Text = "None", Tag = ControlVisibility.None });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.DockButtons.1", Text = "OnMouse", Tag = ControlVisibility.OnMouse });
            button.SubItems.Add(new DataRibbonItem() { ItemId = "Dx.Layout.DockButtons.2", Text = "Allways", Tag = ControlVisibility.Allways});
            __DockButtonRibbonItem = button;
            return button;
        }
        private void _ActivateDockButtons(ControlVisibility dockButtons)
        {
            DataRibbonItem ribbonMenu = __DockButtonRibbonItem;
            DataRibbonItem activeItem = null;
            foreach (var iSubItem in ribbonMenu.SubItems)
            {
                var subItem = iSubItem as DataRibbonItem;
                var itemButtons = (ControlVisibility)subItem.Tag;
                var isActive = (itemButtons == dockButtons);
                if (isActive)
                    activeItem = subItem;

                var itemStyle = isActive ? FontStyle.Bold : FontStyle.Regular;
                if (subItem.FontStyle != itemStyle)
                {
                    subItem.FontStyle = itemStyle;
                    subItem.Refresh();
                }
            }

            if (activeItem != null)
            {
                ribbonMenu.Text = activeItem.Text;
                ribbonMenu.Refresh();
            }

            this._LayoutPanel.DockButtonVisibility = dockButtons;
            this._LayoutPanel.Refresh();
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            DxComponent.TryRun(() =>
            {
                _DxRibbonItemClick(e);
            });
        }
        private void _DxRibbonItemClick(DxRibbonItemClickArgs e)
        {
            if (e.Item.ItemId.StartsWith("Dx.Layout.IconSet.") && e.Item.Tag is LayoutIconSetType)
            {
                _ActivateIconSet((LayoutIconSetType)e.Item.Tag);
                return;
            }

            if (e.Item.ItemId.StartsWith("Dx.Layout.DockButtons.") && e.Item.Tag is ControlVisibility)
            {
                _ActivateDockButtons((ControlVisibility)e.Item.Tag);
                return;
            }

            switch (e.Item.ItemId)
            {
                case "Dx.Layout.Copy":
                    _DoLayoutCopy();
                    break;
                case "Dx.Layout.Paste":
                    _DoLayoutPaste();
                    break;
                case "Dx.Layout.Clear":
                    _DoLayoutClear();
                    break;

                case "Dx.Layout.Set1":
                    _DoLayoutSet1();
                    break;
                case "Dx.Layout.Set2":
                    _DoLayoutSet2();
                    break;
                case "Dx.Layout.Set3":
                    _DoLayoutSet3();
                    break;
                case "Dx.Layout.Set4":
                    _DoLayoutSet4();
                    break;
                case "Dx.Layout.Set5":
                    _DoLayoutSet5();
                    break;

                case "Dx.Layout.Add":
                    _DoAddNewPanel();
                    break;
                case "Dx.Layout.Close":
                    _DoCloseActivePanel();
                    break;

                case "Dx.Layout.AddToCP1":
                    _DoAddNewPanelTo("C/P1", DxLayoutPanel.RemoveContentMode.Default, false);
                    break;
                case "Dx.Layout.AddToCP2":
                    _DoAddNewPanelTo("C/P2", DxLayoutPanel.RemoveContentMode.HideControlAndKeepTile, false);
                    break;
                case "Dx.Layout.RemoveHidden":
                    _DoRemoveHiddenPanel();
                    break;

                case "Dx.Layout.ScanStruct":
                    _DoScanFormStruct();
                    break;
            }
        }
        private DataRibbonItem __IconSetRibbonItem;
        private DataRibbonItem __DockButtonRibbonItem;
        #endregion
        #endregion
        #region Panely v layoutu
        /// <summary>
        /// Přidá new panel do layoutu do implicitního umístění
        /// </summary>
        private void _DoAddNewPanel()
        {
            var panel = new LayoutTestPanel();
            this._AddControlAsPanel(panel, null, DxLayoutPanel.RemoveContentMode.Default);
        }
        private void _DoCloseActivePanel()
        {
            _LayoutPanel.DoCloseActivePanelOnEscapeKey();
        }
        /// <summary>
        /// Přidá new panel do layoutu do daného umístění
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="removeCurrentContentMode"></param>
        /// <param name="enableCloseButton"></param>
        private void _DoAddNewPanelTo(string areaId, DxLayoutPanel.RemoveContentMode removeCurrentContentMode, bool enableCloseButton)
        {
            var panel = new LayoutTestPanel();
            panel.EnableCloseButton = enableCloseButton;
            this._AddControlAsPanel(panel, areaId, removeCurrentContentMode);
        }
        /// <summary>
        /// Odebere první nalezený Hidden panel
        /// </summary>
        private void _DoRemoveHiddenPanel()
        {
            var layoutPanel = this.LayoutPanel;
            var hiddenPanels = layoutPanel.DxLayoutItems.Where(i => i.State == DxLayoutPanel.LayoutTileStateType.Hidden).ToArray();
            if (hiddenPanels.Length > 0)
                layoutPanel.RemoveControl(hiddenPanels[0].UserControl, DxLayoutPanel.RemoveContentMode.Default);
        }
        /// <summary>
        /// Přidá dodaný panel do layoutu do daného umístění
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="areaId"></param>
        /// <param name="removeCurrentContentMode"></param>
        private void _AddControlAsPanel(LayoutTestPanel panel, string areaId, DxLayoutPanel.RemoveContentMode removeCurrentContentMode)
        {
            _PrepareTestPanel(panel);

            if (String.IsNullOrEmpty(areaId))
                _LayoutPanel.AddControl(panel);
            else
                _LayoutPanel.AddControlToArea(panel, areaId, removeCurrentContentMode: removeCurrentContentMode);

        }
        /// <summary>
        /// Někdo zavřel poslední panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_LastControlRemoved(object sender, EventArgs e)
        {
            this.Close();
        }
        private void _LayoutPanel_UserControlAdd(object sender, TEventArgs<Control> e)
        {
            if (e.Item is LayoutTestPanel testPanel)
                _PrepareTestPanel(testPanel);
        }
        private void _PrepareTestPanel(LayoutTestPanel testPanel)
        {
            testPanel.OwnerForm = this;

            testPanel.TitleImageName = _GetIconName();

            if (Randomizer.IsTrue(40))
            {   // 40% prvků bude mít podtržení:
                Color lineColor = Color.FromArgb(255, 255, 32);
                if (Randomizer.IsTrue(20))
                    // 20% z nich bude mít náhodnou barvu podtržení:
                    lineColor = Randomizer.GetColor(48, 160);
                testPanel.LineColor = Color.FromArgb(160, lineColor);
                testPanel.LineColorEnd = Color.FromArgb(12, lineColor);
                testPanel.LineWidth = 4;
            }

            if (Randomizer.IsTrue(40))
            {   // 40% prvků bude mít BackColor:
                Color backColor = Randomizer.GetColor(64, 256);
                testPanel.TitleBackColor = Color.FromArgb(160, backColor);
                if (Randomizer.IsTrue(40))
                    // 40% z nich bude mít fadeout:
                    testPanel.TitleBackColorEnd = Color.FromArgb(0, backColor);
            }
        }
        #endregion
        #region XmlLayout - akce volané z menu
        private void _LayoutPanel_XmlLayoutChanged(object sender, EventArgs e)
        {
            var xmlLayout = _LayoutPanel.XmlLayout;
            int len = xmlLayout.Length;
        }
        private void _DoLayoutCopy()
        {
            string text = "";
            var xmlLayout = _LayoutPanel.XmlLayout;
            var controls = _LayoutPanel.DxLayoutItems;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                string space = "                ";
                string eol = Environment.NewLine;

                string areaIds = "";
                foreach (var control in controls)
                    areaIds += (areaIds.Length == 0 ? "" : "; ") + control.AreaId;

                string code = space + "string xmlLayout = @\"" + xmlLayout.Replace("\"", "'") + "\";" + eol +
                              space + "string areaIds = \"" + areaIds + "\";" + eol +
                              space + "ApplyLayout(xmlLayout, areaIds);" + eol;

                text = code;
            }
            else
            {
                text = xmlLayout;
            }

            Clipboard.Clear();
            Clipboard.SetText(text);
        }
        private void _DoLayoutPaste()
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                var xmlLayoutTest = _LayoutPanel.XmlLayout;
                var xmlLayoutClip = Clipboard.GetText();
                if (xmlLayoutClip != null && xmlLayoutClip.Length > 50 && xmlLayoutClip.Length < 25000 && xmlLayoutClip.Substring(0,38) == xmlLayoutTest.Substring(0, 38))
                    _LayoutPanel.XmlLayout = xmlLayoutClip;
            }
        }
        private void _DoLayoutClear()
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00'>
 <id-data>
  <id-value id-value.Type='Noris.WS.DataContracts.Desktop.Forms.FormLayout' FormNormalBounds='0;0;286;262' FormState='Normal' IsTabbed='true' Zoom='1'>
   <id-value id-value.Target='RootArea' AreaID='C' Content='DxLayoutItemPanel' ControlID='0' />
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C";
            ApplyLayout(xmlLayout, areaIds, true);
        }
        private void _DoScanFormStruct()
        {
            int count = 0;
            int visibleCount = 0;
            int invisibleCount = 0;
            var structure = _LayoutPanel.GetControlStructure(null, false, c =>
            {
                if (c is TestDevExpress.Components.LayoutTestPanel) 
                {
                    count++; 
                    if (c.Visible)
                        visibleCount++;
                    else
                        invisibleCount++;
                    return DebugControl.ScanFilterMode.HideMyChilds; 
                }
                if (c is Noris.Clients.Win.Components.AsolDX.DxLayout.DxLayoutTitlePanel) return DebugControl.ScanFilterMode.HideMyChilds;
                return DebugControl.ScanFilterMode.Default;
            });

            string xmlLayout = _LayoutPanel.XmlLayout;

            string debugText = structure + Environment.NewLine + Environment.NewLine + xmlLayout;

            DxComponent.ClipboardInsert(debugText);
            DxComponent.ShowMessageInfo($"Bylo nalezeno {count} panelů celkem.\r\nZ toho viditelných {visibleCount},\r\nNeviditelných {invisibleCount}.\r\n\r\nStruktura layoutu je v Clipboardu.");
        }
        private void _DoLayoutSet1()
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00'>
 <id-data>
  <id-value id-value.Type='Noris.WS.DataContracts.Desktop.Forms.FormLayout' FormNormalBounds='0;0;286;262' FormState='Normal' IsTabbed='true' Zoom='1'>
   <id-value id-value.Target='RootArea' AreaID='C' Content='DxLayoutItemPanel' ControlID='4' />
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C";
            ApplyLayout(xmlLayout, areaIds, true);
        }
        private void _DoLayoutSet2()
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00'>
 <id-data>
  <id-value id-value.Type='Noris.WS.DataContracts.Desktop.Forms.FormLayout' FormNormalBounds='0;0;286;262' FormState='Normal' IsTabbed='true' Zoom='1'>
   <id-value id-value.Target='RootArea' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='570' SplitterRange='1870'>
    <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='4' />
    <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxLayoutItemPanel' ControlID='5' />
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1; C/P2";
            ApplyLayout(xmlLayout, areaIds, true);
        }
        private void _DoLayoutSet3()
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00'>
 <id-data>
  <id-value id-value.Type='Noris.WS.DataContracts.Desktop.Forms.FormLayout' FormNormalBounds='0;0;286;262' FormState='Normal' IsTabbed='true' Zoom='1'>
   <id-value id-value.Target='RootArea' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='570' SplitterRange='1870'>
    <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='4' />
    <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='288' SplitterRange='751'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='5' />
     <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxLayoutItemPanel' ControlID='6' />
    </id-value>
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1; C/P2/P1; C/P2/P2";
            ApplyLayout(xmlLayout, areaIds, false);
        }
        private void _DoLayoutSet4()
        {
            /*   Vytvořím něco takového:
               +--------------+----------------------------+
               |              |                            |
               |    C/P1/P1   |                            |
               |              |                            |
               +--------------+          C/P2              |
               |              |                            |
               |    C/P1/P2   |                            |
               |              |                            |
               +--------------+----------------------------+
            */
            var layout = new Noris.UI.Desktop.MultiPage.WindowLayout();
            var root = layout.RootArea;
            root.ContentType = Noris.UI.Desktop.MultiPage.WindowAreaContentType.SplitterVertical;
            root.SplitterPosition = 450;
            root.FixedContent = Noris.UI.Desktop.MultiPage.WindowAreaFixedContent.Content1;

            root.Content1.ContentType = Noris.UI.Desktop.MultiPage.WindowAreaContentType.SplitterHorizontal;
            root.Content1.SplitterPosition = 320;
            root.Content1.FixedContent = Noris.UI.Desktop.MultiPage.WindowAreaFixedContent.Content2;

            string areaId = root.Content1.Content2.AreaId;           // C/P1/P2
            var allAreaIds = root.AllAreaIds.ToOneString("; ");
            string xmlLayout = layout.LayoutXml;
            ApplyLayout(xmlLayout, allAreaIds, true);
        }
        private void _DoLayoutSet5()
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00' Created='2021-04-17 18:32:14.095' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='312' SplitterRange='781'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='124' SplitterRange='312'>
    <id-value id-value.Target='Content1' AreaID='C/P1/P1' Content='DxLayoutItemPanel' ControlID='39' />
    <id-value id-value.Target='Content2' AreaID='C/P1/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='293' SplitterRange='1376'>
     <id-value id-value.Target='Content1' AreaID='C/P1/P2/P1' Content='DxLayoutItemPanel' ControlID='41' />
     <id-value id-value.Target='Content2' AreaID='C/P1/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='846' SplitterRange='1078'>
      <id-value id-value.Target='Content1' AreaID='C/P1/P2/P2/P1' Content='DxLayoutItemPanel' ControlID='38' />
      <id-value id-value.Target='Content2' AreaID='C/P1/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='42' />
     </id-value>
    </id-value>
   </id-value>
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='278' SplitterRange='464'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='37' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='679' SplitterRange='1376'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1' Content='DxLayoutItemPanel' ControlID='40' />
     <id-value id-value.Target='Content2' AreaID='C/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='43' />
    </id-value>
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1/P1; C/P1/P2/P2/P2";
            ApplyLayout(xmlLayout, areaIds, true);
        }
        private void ApplyLayout(string xmlLayout, string areaIds, bool fillPanels)
        {
            using (_LayoutPanel.ScopeSuspendParentLayout())
            {
                _LayoutPanel.DisableAllEvents = true;
                _LayoutPanel.RemoveAllControls();
                _LayoutPanel.XmlLayout = xmlLayout.Replace("'", "\"");
                if (fillPanels)
                {
                    string[] areasId = areaIds.Split(';', ',');
                    foreach (string areaId in areasId)
                    {
                        LayoutTestPanel testPanel = new LayoutTestPanel();
                        _PrepareTestPanel(testPanel);
                        _LayoutPanel.AddControlToArea(testPanel, areaId.Trim());
                    }
                }
                _LayoutPanel.DisableAllEvents = false;
            }
        }
        #endregion
        #region Eventy a private
        /// <summary>
        /// Po změně layoutu (pozice prvků)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_LayoutPanelChanged(object sender,  DxLayoutPanelSplitterChangedArgs e)
        {
            _ShowLayout(e);
        }
        /// <summary>
        /// Po změně pozice splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_SplitterPositionChanged(object sender, DxLayoutPanelSplitterChangedArgs e)
        {
            _ShowLayout(e);
        }
        private void _ShowLayout(DxLayoutPanelSplitterChangedArgs e)
        {
            LayoutTestPanel panel1 = e.Control1 as LayoutTestPanel;
            LayoutTestPanel panel2 = e.Control2 as LayoutTestPanel;
            var orientation = e.SplitterOrientation;
            var position = e.SplitterPosition;
        }
        /// <summary>
        /// Občas změním titulek některého panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Timer_Tick(object sender, EventArgs e)
        {
            this._Timer.Enabled = false;

            // Najdeme si náhodně nějaký panel:
            var layoutsItem = _LayoutPanel.DxLayoutItems;
            int count = layoutsItem.Length;
            if (count > 0)
            {
                DxLayoutItemInfo layoutItem = Randomizer.GetItem(layoutsItem);
                if (layoutItem != null)
                {
                    if (layoutItem.UserControl is LayoutTestPanel testPanel)
                    {
                        if (Randomizer.IsTrue(70))
                        {   // 70% prvků bude mít náhodný textový suffix:
                            string title = testPanel.TitleTextBasic;
                            string appendix = Randomizer.GetSentence(2, 5, false);
                            title = title + " (" + appendix + ")";
                            testPanel.TitleText = title;              // Set => Event => DxLayout eventhandler
                        }

                        if (Randomizer.IsTrue(20))
                        {   // 20% prvků bude mít náhodně změněnou ikonu:
                            testPanel.TitleImageName = _GetIconName();
                        }
                    }

                    this._Timer.Interval = Randomizer.Rand.Next(700, 3200);
                }
            }

            this._Timer.Enabled = true;
        }
        private string _GetIconName()
        {
            return Randomizer.GetItem(_ImageNames);
        }
        /// <summary>
        /// Chtěl bych zavřít formulář
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            this._LayoutPanel.RemoveAllControls();
            if (this._LayoutPanel.ControlCount > 0)
                e.Cancel = true;
        }
        /// <summary>
        /// Panel layoutu
        /// </summary>
        public DxLayoutPanel LayoutPanel { get { return _LayoutPanel; } }
        private DxLayoutPanel _LayoutPanel;

        private string[] _ImageNames;
        private Timer _Timer;
        #endregion
    }
    /// <summary>
    /// Testovací panel reprezentující UserControl v <see cref="DxLayoutPanel"/>, obdoba panelu pro DynamicPage
    /// </summary>
    public class LayoutTestPanel : DxPanelControl, ILayoutUserControl // DevExpress.XtraEditors.PanelControl / System.Windows.Forms.Panel
    {
        #region Public vrstva: konstruktor, property, eventy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public LayoutTestPanel()
        {
            this.Initialize();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.OwnerForm = null;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"TestPanel Id: {Id}";
        }
        /// <summary>
        /// ID tohoto panelu
        /// </summary>
        protected int Id { get; private set; }
        /// <summary>
        /// ID panelu naposledy vytvořeného
        /// </summary>
        protected static int LastPanelId = 0;
        /// <summary>
        /// Základní text titulku, nemění se, nepropisuje se do <see cref="TitleText"/>
        /// </summary>
        public string TitleTextBasic { get; set; }
        /// <summary>
        /// Text v titulku
        /// </summary>
        public string TitleText 
        {
            get { return _TitleText; }
            set { _TitleText = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleText;
        /// <summary>
        /// Text v titulku náhradní
        /// </summary>
        public string TitleSubstitute
        {
            get { return _TitleSubstitute; }
            set { _TitleSubstitute = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleSubstitute;
        /// <summary>
        /// Ikona u titulku
        /// </summary>
        public string TitleImageName
        {
            get { return _TitleImageName; }
            set { _TitleImageName = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleImageName;
        /// <summary>
        /// Ikona u titulku
        /// </summary>
        public string[] TitleAdditionalIcons
        {
            get { return _TitleAdditionalIcons; }
            set { _TitleAdditionalIcons = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string[] _TitleAdditionalIcons;
        /// <summary>
        /// Je povolen button Close?
        /// </summary>
        public bool EnableCloseButton
        {
            get { return _EnableCloseButton; }
            set { _EnableCloseButton = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private bool _EnableCloseButton;
        /// <summary>
        /// Owner form, poskytuje služby...
        /// </summary>
        public LayoutForm OwnerForm { get; set; }
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? LineColor { get; set; }
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? LineColorEnd { get; set; }
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        public int? LineWidth { get; set; }

        public int TitleBackMargins { get; set; }
        public Color? TitleBackColor { get; set; }
        public Color? TitleBackColorEnd { get; set; }
        public Color? TitleTextColor { get; set; }

        /// <summary>
        /// Došlo ke změně <see cref="TitleText"/>
        /// </summary>
        public event EventHandler TitleChanged;
        #endregion
        #region Inicializace, jednotlivé controly, jejich eventy
        /// <summary>
        /// Inicializace panelu
        /// </summary>
        protected void Initialize()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;

            Id = ++LastPanelId;
            this.TitleTextBasic = "Panel číslo " + Id.ToString();
            this.TitleText = this.TitleTextBasic;
            this.EnableCloseButton = true;

            // Připravíme centrální panel s fixním layoutem:
            int panelWidth = 300;
            int panelHeight = 240;
            _NavPanel = new DevExpress.XtraEditors.PanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Width = panelWidth, Height = panelHeight };

            // Add buttony Left - Top - Right - Bottom :
            int buttonAddSize = 36;
            int marginX = 3;
            int marginY = 3;
            int buttonAddHalf = buttonAddSize / 2;
            int panelCenterX = panelWidth / 2;
            int panelCenterY = panelHeight / 2;

            string imageLeft = "svgimages/xaf/action_navigation_history_back.svg";
            string imageTop = "svgimages/xaf/action_navigation_previous_object.svg";
            string imageRight  = "svgimages/xaf/action_navigation_history_forward.svg";
            string imageBottom = "svgimages/xaf/action_navigation_next_object.svg";

            _AddLeftButton = CreateDxButton("", imageLeft, marginX, panelCenterY - buttonAddHalf, buttonAddSize, buttonAddSize, LayoutPosition.Left);
            _AddTopButton = CreateDxButton("", imageTop, panelCenterX - buttonAddHalf, marginY, buttonAddSize, buttonAddSize, LayoutPosition.Top);
            _AddRightButton = CreateDxButton("", imageRight, panelWidth - marginX - buttonAddSize, panelCenterY - buttonAddHalf, buttonAddSize, buttonAddSize, LayoutPosition.Right);
            _AddBottomButton = CreateDxButton("", imageBottom, panelCenterX - buttonAddHalf, panelHeight - marginY - buttonAddSize, buttonAddSize, buttonAddSize, LayoutPosition.Bottom);


            // Titulek a Remove buttony:
            int textTitleHeight = 28;
            int spaceTitleY = 8;
            int buttonRemoveWidth = 196;
            int buttonRemoveHeight = 28;
            int spaceButtonY = 4;

            int centerHeight = textTitleHeight + spaceTitleY + 3 * buttonRemoveHeight + 2 * spaceButtonY;
            int currentY = panelCenterY - centerHeight / 2;
            int currentX = panelCenterX - buttonRemoveWidth / 2;
            _TextTitle = CreateDxTitle(this.TitleText, null, currentX, currentY, buttonRemoveWidth, textTitleHeight);
            currentY += textTitleHeight + spaceTitleY;
            _CloseDefaultButton = CreateDxButton("Close Default", null, currentX, currentY, buttonRemoveWidth, buttonRemoveHeight, DxLayoutPanel.RemoveContentMode.Default);
            currentY += buttonRemoveHeight + spaceButtonY;
            _CloseRemoveControlAndKeepTileButton = CreateDxButton("Close RemoveControlAndKeepTile", null, currentX, currentY, buttonRemoveWidth, buttonRemoveHeight, DxLayoutPanel.RemoveContentMode.RemoveControlAndKeepTile);
            currentY += buttonRemoveHeight + spaceButtonY;
            _CloseHideControlAndKeepTileButton = CreateDxButton("Close HideControlAndKeepTile", null, currentX, currentY, buttonRemoveWidth, buttonRemoveHeight, DxLayoutPanel.RemoveContentMode.HideControlAndKeepTile);

            this.Controls.Add(_NavPanel);

            this.BackColorUser = Randomizer.GetColor(64, 256, 64);

            MouseActivityInit();
        }
        /// <summary>
        /// Vytvoří a vrátí button
        /// </summary>
        /// <returns></returns>
        private DevExpress.XtraEditors.SimpleButton CreateDxButton(string text, string imageName, int x, int y, int w, int h, object tag)
        {
            var button = new DevExpress.XtraEditors.SimpleButton() { Text = text, Bounds = new Rectangle(x, y, w, h), Tag = tag };
            if (!String.IsNullOrEmpty(imageName))
            {
                DxComponent.ApplyImage(button.ImageOptions, imageName, imageSize: new Size(32, 32));

                if (String.IsNullOrEmpty(text))
                {   // Jen ikona
                    button.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.TopCenter;          // To je důležité pro vystředění samotné ikony bez textu
                    button.ImageOptions.ImageToTextIndent = 0;              // Pro button s Image a bez Textu nemá význam
                }
                else
                {   // Ikona a text
                    button.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
                    button.ImageOptions.ImageToTextIndent = 3;              // Mezera mezi textem a ikonou
                }
            }

            button.Click += _AnyButton_Click;
            _NavPanel.Controls.Add(button);
            return button;
        }
        /// <summary>
        /// Vytvoří a vrátí titulek
        /// </summary>
        /// <returns></returns>
        private DevExpress.XtraEditors.LabelControl CreateDxTitle(string text, string imageName, int x, int y, int w, int h)
        {
            var title = new DevExpress.XtraEditors.LabelControl() { Text = this.TitleText, Bounds = new Rectangle(x, y, w, h) };
            title.Appearance.FontSizeDelta = 4;
            title.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            title.Appearance.Options.UseTextOptions = true;
            title.Appearance.Options.UseFont = true;
            title.Paint += Title_Paint;
            _NavPanel.Controls.Add(title);
            return title;
        }

        private void Title_Paint(object sender, PaintEventArgs e)
        {
            var label = sender as DevExpress.XtraEditors.LabelControl;
            var alig = label.Appearance.TextOptions.HAlignment;
            var uset = label.Appearance.Options.UseTextOptions;

            //
        }

        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            this.DoLayout();
        }
        /// <summary>
        /// Rozmístí svoje controly do aktuálního prostoru
        /// </summary>
        protected void DoLayout()
        {
            var clientSize = this.ClientSize;
            var clientCenterX = clientSize.Width / 2;
            var clientCenterY = clientSize.Height / 2;

            _NavPanel.Bounds = _NavPanel.Size.CreateRectangleFromCenter(new Point(clientCenterX, clientCenterY));
        }
        /// <summary>
        /// Obsahuje (najde) control, který řídí layout a vkládání nových prvků a odebírání existujících
        /// </summary>
        protected DxLayoutPanel LayoutPanel { get { return DxLayoutPanel.SearchParentLayoutPanel(this); } }
        /// <summary>
        /// Moje aktuální adresa
        /// </summary>
        protected string AreaId 
        {
            get 
            {
                var layoutPanel = this.LayoutPanel;
                bool hasInfo = layoutPanel.TryGetLayoutItemInfo(this, out var dxInfo);
                if (hasInfo)
                    return dxInfo.AreaId;
                return null;
            }
        }
        /// <summary>
        /// Doporučená velikost buttonů Add
        /// </summary>
        protected Size ButtonAddToLocationSize { get { return new Size(120, 38); } }
        /// <summary>
        /// Doporučená velikost buttonů Close
        /// </summary>
        protected Size ButtonCloseSize { get { return new Size(192, 30); } }
        /// <summary>
        /// Po kliknutí na button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnyButton_Click(object sender, EventArgs e)
        {
            if (!(sender is DevExpress.XtraEditors.SimpleButton button)) return;

            if (button.Tag is LayoutPosition)
            {
                LayoutPosition position = (LayoutPosition)button.Tag;
                if (position == LayoutPosition.Left || position == LayoutPosition.Top || position == LayoutPosition.Bottom || position == LayoutPosition.Right)
                {
                    int size = ((position == LayoutPosition.Left || position == LayoutPosition.Right) ? ButtonAddToLocationSize.Width * 4 : ButtonAddToLocationSize.Height * 6);
                    LayoutTestPanel newPanel = new LayoutTestPanel();

                    float ratio = 0.4f;
                    LayoutPanel.AddControl(newPanel, this, position, currentSizeRatio: ratio);
                    this.MouseActivityDetect();
                    newPanel.MouseActivityDetect();
                }
            }

            if (button.Tag is DxLayoutPanel.RemoveContentMode)
            {
                DxLayoutPanel.RemoveContentMode removeMode = (DxLayoutPanel.RemoveContentMode)button.Tag;
                var areaId = this.AreaId;
                var layoutPanel = this.LayoutPanel;
                layoutPanel.RemoveControl(this, removeMode);
            }
        }
        DevExpress.XtraEditors.PanelControl _NavPanel;
        DevExpress.XtraEditors.SimpleButton _AddRightButton;
        DevExpress.XtraEditors.SimpleButton _AddBottomButton;
        DevExpress.XtraEditors.SimpleButton _AddLeftButton;
        DevExpress.XtraEditors.SimpleButton _AddTopButton;
        DevExpress.XtraEditors.LabelControl _TextTitle;
        DevExpress.XtraEditors.SimpleButton _CloseDefaultButton;
        DevExpress.XtraEditors.SimpleButton _CloseRemoveControlAndKeepTileButton;
        DevExpress.XtraEditors.SimpleButton _CloseHideControlAndKeepTileButton;
        #endregion
        #region Pohyb myši a viditelnost buttonů
        /// <summary>
        /// Inicializace eventů a proměnných pro myší aktivity
        /// </summary>
        private void MouseActivityInit()
        {
            RegisterMouseActivityEvents(this);
            foreach (Control control in this.Controls)
                RegisterMouseActivityEvents(control);
            this.ParentChanged += Control_MouseActivityChanged;
            this.MouseActivityDetect(true);
        }
        /// <summary>
        /// Zaregistruje pro daný control eventhandlery, které budou řídit viditelnost prvků this panelu (buttony podle myši)
        /// </summary>
        /// <param name="control"></param>
        private void RegisterMouseActivityEvents(Control control)
        {
            control.MouseEnter += Control_MouseActivityChanged;
            control.MouseLeave += Control_MouseActivityChanged;
            control.MouseMove += Control_MouseMove;
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_MouseActivityChanged(object sender, EventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Provede se po myší aktivitě, zajistí Visible a Enabled pro buttony
        /// </summary>
        /// <param name="force"></param>
        private void MouseActivityDetect(bool force = false)
        {
            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated) return;

            bool isMouseOnControl = false;
            if (this.Parent != null)
            {
                Point absolutePoint = Control.MousePosition;
                Point relativePoint = this.PointToClient(absolutePoint);
                isMouseOnControl = this.ClientRectangle.Contains(relativePoint);
            }
            if (force || isMouseOnControl != _IsMouseOnControl)
            {
                _IsMouseOnControl = isMouseOnControl;
                RefreshButtonVisibility();
            }
        }
        private void RefreshButtonVisibility()
        {
            bool isVisible = this._IsMouseOnControl;
            _AddRightButton.Visible = isVisible;
            _AddBottomButton.Visible = isVisible;
            _AddLeftButton.Visible = isVisible;
            _AddTopButton.Visible = isVisible;
        }
        /// <summary>
        /// Obsahuje true, pokud je myš nad controlem (nad kterýmkoli prvkem), false když je myš mimo
        /// </summary>
        private bool _IsMouseOnControl;
        #endregion
        #region ILayoutUserControl implementace
        string ILayoutUserControl.Id { get { return this.Id.ToString(); } }
        bool ILayoutUserControl.TitleVisible { get { return true; } }
        string ILayoutUserControl.TitleText { get { return this.TitleText; } }
        string ILayoutUserControl.TitleSubstitute { get { return this.TitleSubstitute; } }
        string ILayoutUserControl.TitleImageName { get { return this.TitleImageName; } }
        IEnumerable<string> ILayoutUserControl.TitleAdditionalIcons { get { return this.TitleAdditionalIcons; } }
        bool ILayoutUserControl.EnableCloseButton { get { return this.EnableCloseButton; } }
        #endregion
    }
}
