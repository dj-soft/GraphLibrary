using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using WinDraw = System.Drawing;
using DxDForm = Noris.Clients.Win.Components.AsolDX.DataForm;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DxLData = Noris.Clients.Win.Components.AsolDX.DataForm.Layout;
using System.Drawing;
using Noris.Clients.Win.Components.AsolDX.DxForm;
using Noris.Clients.Win.Components.AsolDX.DataForm;
using TestDevExpress.Components;
using DevExpress.XtraRichEdit.Model.History;
using DevExpress.XtraRichEdit.Model;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy Threads
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "ListBox", buttonOrder: 130, buttonImage: "svgimages/diagramicons/insertlist.svg", buttonToolTip: "Zobrazí různé ListBoxy a testuje jejich chování", tabViewToolTip: "Testuje různé ListBoxy a jejich chování")]
    internal class ListBoxForm : DxRibbonForm
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ListBoxForm()
        {
            Counter = (Counter % 6) + 1;
            this.Text = $"DxListBox tester [{Counter}]";

            string imageName = "";
            string iconText = "";
            bool isBold = false;
            Color iconBack = Color.Wheat;
            switch (Counter)
            {
                case 1:
                    imageName = "svgimages/dashboards/insertlistbox.svg";
                    iconText = "i";
                    iconBack = Color.LightYellow;
                    break;
                case 2:
                    imageName = "svgimages/diagramicons/insertlist.svg";
                    iconText = "M";
                    iconBack = Color.DarkOrange;
                    break;
                case 3:
                    imageName = "svgimages/dashboards/insertlistbox.svg";
                    iconText = "ii";
                    iconBack = Color.LightBlue;
                    break;
                case 4:
                    imageName = "svgimages/diagramicons/insertlist.svg";
                    iconText = "WW";
                    iconBack = Color.DarkBlue;
                    isBold = true;
                    break;
                case 5:
                    imageName = "svgimages/dashboards/insertlistbox.svg";
                    iconText = "MM";
                    iconBack = Color.LightCoral;
                    isBold = true;
                    break;
                case 6:
                    imageName = "svgimages/diagramicons/insertlist.svg";
                    iconText = "<|>";
                    iconBack = Color.DarkMagenta;
                    break;
            }

            this.ImageName = imageName;
            this.ImageNameAdd = SvgImageTextIcon.CreateImageName(iconText, textFont: SvgImageTextIcon.TextFontType.Tahoma, backColor: iconBack, textBold: isBold, rounding: 6, padding: 2, borderWidth: 1);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _DisposeSamplesAll();
        }
        private static int Counter = 0;
        #endregion
        #region Main Content
        /// <summary>
        /// Provede přípravu obsahu hlavního panelu <see cref="DxRibbonForm.DxMainPanel"/>. Panel je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected override void DxMainContentPrepare()
        {
            base.DxMainContentPrepare();

            __MainSplit = new DxSplitContainerControl() { Dock = DockStyle.Fill };
            __MainSplit.Panel1.MinSize = 180;
            __MainSplit.Panel2.MinSize = 240;
            __MainSplit.FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1;
            __MainSplit.SplitterPosition = DxComponent.ZoomToGui(240, this.DeviceDpi);
            __MainSplit.Panel1.ClientSizeChanged += _SampleButtons_SizeChanged;
            __MainSplit.Panel2.ClientSizeChanged += _SamplePanel_SizeChanged;
            this.DxMainPanel.Controls.Add(__MainSplit);

            __ResizedControls = new List<Control>();

            // Pro všechny metody, které jsou označené atributem 'InitializerAttribute' je vyvolám 
            //  a získám tak pole __Samples = jednotlivá tlačítka pro jednotlivé testy:
            __Samples = new List<SampleInfo>();
            var methods = InitializerAttribute.SearchInitializerMethods(this.GetType());
            foreach (var method in methods )
                method.Invoke(this, null);       // Volá metody označené atributem [Initializer()]


            // Na základě prvků v poli __Samples vytvořím sadu buttonů pro jednotlivé testy:
            _SampleButtons = new List<DxSimpleButton>();
            foreach (var sample in __Samples)
            {
                var sampleButton = DxComponent.CreateDxSimpleButton(0,0,200,30, this.__MainSplit.Panel1, sample.ButtonText, _SampleButtonClick, tag: sample);
                sample.Button = sampleButton;
                _SampleButtons.Add(sampleButton);
            }
            _DoLayoutButtons();
        }

        private DxSplitContainerControl __MainSplit;
        /// <summary>
        /// Buttony samplů
        /// </summary>
        private List<DxSimpleButton> _SampleButtons;

        /// <summary>
        /// Kliknutí na button se samplem = zahodí se dosavadní listy, a zavolá se sample pro tvorbu nové komponenty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _SampleButtonClick(object sender, EventArgs args)
        {
            if (sender is Control control && control.Tag is SampleInfo sampleInfo)
            {
                _DisposeSamplesAll();
                __CurrentSample = sampleInfo;
                sampleInfo.Button.Appearance.FontStyleDelta = FontStyle.Bold;
                sampleInfo.ButtonClick();
                _ResizeChildControls();
            }
        }
        private void _SampleButtons_SizeChanged(object sender, EventArgs e)
        {
            _DoLayoutButtons();
        }
        private void _DoLayoutButtons()
        {
            var clientSize = this.__MainSplit.Panel1.ClientSize;
            int x = 12;
            int w = clientSize.Width - 24;
            int y = 12;
            int h = 36;
            int s = 6;
            int g = 9;
            int n = 0;
            foreach (var sample in __Samples)
            {
                if (n > 0 && sample.IsNewGroup) y += g;
                var button = sample.Button;
                if (button != null)
                {
                    button.Bounds = new Rectangle(x, y, w, h);
                    y += h + s;
                    n++;
                }
            }
        }

        /// <summary>
        /// Do daného List se navážou obecné eventhandlery, vedou na LogAddLine
        /// </summary>
        /// <param name="listPanel"></param>
        /// <param name="resizeHeight"></param>
        private void _AddEventHandlers(DxListBoxPanel listPanel, bool resizeHeight = false)
        {
            listPanel.DxProperties.SelectedItemsChanged += _SelectedItemsChanged;
            listPanel.DxProperties.ItemMouseClick += _ItemMouseClick;
            listPanel.DxProperties.ItemMouseDoubleClick += _ItemMouseDoubleClick;

            if (resizeHeight)
                __ResizedControls.Add(listPanel);
        }
        /// <summary>
        /// List změnil prvek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SelectedItemsChanged(object sender, EventArgs e)
        {
            string text = "SelectedItemsChanged";
            if (sender is DxListBoxPanel listBox)
            {
                text += $"; ActiveItem: {listBox.CurrentItem}; SelectedCount: {listBox.SelectedItems.Length}";

                var visItms = listBox.ListBox.DxProperties.CurrentVisibleMenuItems;
                int cnt = visItms.Length;

            }
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, text);
        }
        /// <summary>
        /// List provedl Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemMouseClick(object sender, DxListBoxItemMouseClickEventArgs args)
        {
            string text = $"ItemMouseClick: ItemId: '{args.ItemId}'; Button: '{args.Buttons}'";
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, text);
        }
        /// <summary>
        /// List provedl DoubleClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemMouseDoubleClick(object sender, DxListBoxItemMouseClickEventArgs args)
        {
            string text = $"ItemMouseDoubleClick: ItemId: '{args.ItemId}'; Button: '{args.Buttons}'";
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, text);
        }
        private void _DisposeSamplesAll()
        {
            __ResizedControls.Clear();
            __Samples.ForEach(s =>
            {
                s.Button.Appearance.FontStyleDelta = FontStyle.Regular;
                s.DisposeContent();
            });
        }
        private Control _HostContainer { get { return this.__MainSplit.Panel2; } }
        private List<SampleInfo> __Samples;
        private SampleInfo __CurrentSample;
        private List<Control> __ResizedControls;
        /// <summary>
        /// Souřadnice pro Sample
        /// </summary>
        private Rectangle _SampleBounds { get { return __MainSplit.Panel2.ClientRectangle; } }

        private void _SamplePanel_SizeChanged(object sender, EventArgs e)
        {
            _ResizeChildControls();
        }

        private void _HostContainer_ClientSizeChanged(object sender, EventArgs e)
        {
        }
        private void _ResizeChildControls()
        { 
            var sampleBounds = _SampleBounds;
            foreach (var child in __ResizedControls)
            {
                child.Height = sampleBounds.Bottom - child.Top;
            }

            __CurrentSample?.DoLayout?.Invoke();
        }

        private class SampleInfo
        {
            public SampleInfo(string buttonText, Action buttonClick, Action disposeContent, bool isNewGroup = false)
            {
                this.ButtonText = buttonText;
                this.ButtonClick = buttonClick;
                this.DisposeContent = disposeContent;
                this.IsNewGroup = isNewGroup;
            }
            public SampleInfo(string buttonText, Action buttonClick, Action doLayout, Action disposeContent, bool isNewGroup = false)
            {
                this.ButtonText = buttonText;
                this.ButtonClick = buttonClick;
                this.DisposeContent = disposeContent;
                this.DoLayout = doLayout;
                this.IsNewGroup = isNewGroup;
            }
            public readonly string ButtonText;
            public readonly Action ButtonClick;
            public readonly Action DoLayout;
            public readonly Action DisposeContent;
            public readonly bool IsNewGroup;
            public DxSimpleButton Button;
        }

        #endregion
        #region Sample 1
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(1)]
        private void _PrepareSample1()
        {
            __Samples.Add(new SampleInfo("Jednoduchý List", _CreateSample1, _DisposeSample1));
        }
        private void _CreateSample1()
        {
            var bounds = _SampleBounds;
            var position = (_Sample1SplitPosition > 0 ? _Sample1SplitPosition : 320);
            _Sample1Split = new DxSplitContainerControl() { Bounds = bounds, SplitterOrientation = Orientation.Horizontal, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterPosition = position };
            _Sample1Split.SplitterMoved += _Sample1Split_SplitterMoved;
            this._HostContainer.Controls.Add(_Sample1Split);
            __ResizedControls.Add(_Sample1Split);                                                  // Pro _Sample1Split resizovat výšku

            var sampleList = new DxListBoxPanel() { Dock = DockStyle.Fill };                       // ListBox je Docked v Split.Panel1 a tak mu interaktivně měníme šířku
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.None;
            sampleList.DxProperties.TitleText = "ListBox s jedním sloupcem";
            sampleList.DxProperties.MenuItems = Randomizer.GetMenuItems(24, 60, Randomizer.ImageResourceType.PngSmall);
            sampleList.DxProperties.MenuItemColumnWidths = new int[] { 400 };                      // Jediný sloupec, pro jediný text v řádku, ale umožní vodorovný Scroll
            _AddEventHandlers(sampleList, false);                                                  // ... , false)  =>  pro ListBox neresizovat výšku 

            this._Sample1Split.Panel1.Controls.Add(sampleList);

            _Sample1List = sampleList;
        }
        private void _Sample1Split_SplitterMoved(object sender, EventArgs e)
        {
            _Sample1SplitPosition = _Sample1Split.SplitterPosition;
        }
        private void _DisposeSample1()
        {
            _Sample1List?.RemoveControlFromParent();
            _Sample1List = null;

            _Sample1Split?.RemoveControlFromParent();
            _Sample1Split = null;
        }
        private DxSplitContainerControl _Sample1Split;
        private DxListBoxPanel _Sample1List;
        private int _Sample1SplitPosition;
        #endregion
        #region Sample 2
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(2)]
        private void _PrepareSample2()
        {
            __Samples.Add(new SampleInfo("List s Reorder a buttony", _CreateSample2, _DisposeSample2));
        }
        private void _CreateSample2()
        {
            var bounds = _SampleBounds;
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X, bounds.Y, 450, 320) };
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleList.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.RightSideCenter;
            sampleList.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.Move_All };
            sampleList.DxProperties.EnabledKeyActions = ControlKeyActionType.Move_All;
            sampleList.DxProperties.DragDropActions = DxDragDropActionType.ReorderItems;

            sampleList.DxProperties.MenuItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);

            _Sample2List = sampleList;
        }
        private void _DisposeSample2()
        {
            _Sample2List?.RemoveControlFromParent();
            _Sample2List = null;
        }
        private DxListBoxPanel _Sample2List;
        #endregion
        #region Sample 3 Dva samostatné ListBoxy
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(3)]
        private void _PrepareSample3()
        {
            __Samples.Add(new SampleInfo("Dva Listy", _CreateSample3, _DisposeSample3));
        }
        private void _CreateSample3()
        {
            var bounds = _SampleBounds;
            var sampleListA = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X, bounds.Y, 400, 320) };
            sampleListA.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleListA.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleListA.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListA.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.SelectAll, ControlKeyActionType.Delimiter, ControlKeyActionType.CopyToTargetOneE, ControlKeyActionType.CopyToTargetOneC, ControlKeyActionType.CopyToTargetAllE };
            sampleListA.DxProperties.EnabledKeyActions = ControlKeyActionType.None;
            sampleListA.DxProperties.DragDropActions = DxDragDropActionType.CopyItemsFrom;
            sampleListA.DxProperties.MenuItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            sampleListA.DxProperties.ListActionAfter += _Sample3ListA_ListActionAfter;
            _AddEventHandlers(sampleListA, true);
            this._HostContainer.Controls.Add(sampleListA);

            var sampleListB = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X + 410, bounds.Y, 400, 320) };
            sampleListB.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleListB.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleListB.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListB.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.SelectAll, ControlKeyActionType.Delimiter, ControlKeyActionType.Delete, ControlKeyActionType.CopyToSourceOneE, ControlKeyActionType.CopyToSourceOneC, ControlKeyActionType.CopyToSourceAllE, ControlKeyActionType.Delimiter, ControlKeyActionType.Move_All };
            sampleListB.DxProperties.EnabledKeyActions = ControlKeyActionType.None;
            sampleListB.DxProperties.DragDropActions = DxDragDropActionType.ImportItemsInto | DxDragDropActionType.ReorderItems;
            sampleListB.DxProperties.MenuItems = Randomizer.GetMenuItems(7, Randomizer.ImageResourceType.PngSmall, true);
            sampleListB.DxProperties.ListActionAfter += _Sample3ListB_ListActionAfter;
            _AddEventHandlers(sampleListB, true);
            this._HostContainer.Controls.Add(sampleListB);

            _Sample3ListA = sampleListA;
            _Sample3ListB = sampleListB;
        }
        private void _DisposeSample3()
        {
            _Sample3ListA?.RemoveControlFromParent();
            _Sample3ListA = null;

            _Sample3ListB?.RemoveControlFromParent();
            _Sample3ListB = null;
        }
        private void _Sample3ListA_ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case ControlKeyActionType.CopyToTargetOneC:
                    _Sample3ListB.DxProperties.InsertItems(_Sample3ListA.DxProperties.SelectedMenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToTargetOneE:
                    _Sample3ListB.DxProperties.InsertItems(_Sample3ListA.DxProperties.SelectedMenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToTargetAllC:
                    _Sample3ListB.DxProperties.InsertItems(_Sample3ListA.DxProperties.MenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToTargetAllE:
                    _Sample3ListB.DxProperties.InsertItems(_Sample3ListA.DxProperties.MenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
            }
        }
        private void _Sample3ListB_ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case ControlKeyActionType.CopyToSourceOneC:
                    _Sample3ListA.DxProperties.InsertItems(_Sample3ListB.DxProperties.SelectedMenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToSourceOneE:
                    _Sample3ListA.DxProperties.InsertItems(_Sample3ListB.DxProperties.SelectedMenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToSourceAllC:
                    _Sample3ListA.DxProperties.InsertItems(_Sample3ListB.DxProperties.MenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToSourceAllE:
                    _Sample3ListA.DxProperties.InsertItems(_Sample3ListB.DxProperties.MenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
            }
        }
        private DxListBoxPanel _Sample3ListA;
        private DxListBoxPanel _Sample3ListB;
        #endregion
        #region Sample 4 DblListBox
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(4)]
        private void _PrepareSample4()
        {
            __Samples.Add(new SampleInfo("DoubleListBox", _CreateSample4, _CreateLayout4, _DisposeSample4, true));
        }
        private void _CreateSample4()
        {
            var bounds = _SampleBounds;
            var dblListBounds = new Rectangle(bounds.X, bounds.Y + 62, 800, 600);
            var sampleDblList = new DxDblListBoxPanel() { Bounds = dblListBounds };
            __ResizedControls.Add(sampleDblList);                                                  // Pro sampleDblList resizovat výšku

            sampleDblList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;

            var position = (_Sample4SplitPosition > 0 ? _Sample4SplitPosition : 360);
            sampleDblList.DxProperties.SplitterPosition = position;

            /* Ostatní property nechám default:
            sampleDblList.DxProperties.ButtonsPosition = DxDblListBoxPanel.DblButtonsPositionType.Bottom;
            sampleDblList.DxProperties.SourceListReadOnly = true;
            sampleDblList.DxProperties.ClipboardActionsEnabled = true;
            sampleDblList.DxProperties.ReorderItemsEnabled = true;
            sampleDblList.DxProperties.DragAndDropEnabled = true;
            */
            sampleDblList.DxProperties.SourceMenuItems = Randomizer.GetMenuItems(48, 80, Randomizer.ImageResourceType.PngSmall, true, true);
            sampleDblList.DxProperties.TargetMenuItems = Randomizer.GetMenuItems(3, 8, Randomizer.ImageResourceType.PngSmall, true, true);
            sampleDblList.DxProperties.TargetMenuItemsChanged += _Sample4_TargetMenuItemsChanged;

            sampleDblList.SplitterPositionChanged += Sample4DblList_SplitterPositionChanged;
            this._HostContainer.Controls.Add(sampleDblList);

            _Sample4DblList = sampleDblList;

            _CreateSample4Params();                        // Params = prvky pro nastavování vlastností pro testy různého nastavení
            _Sample4SetTitles();
        }
        private void Sample4DblList_SplitterPositionChanged(object sender, EventArgs e)
        {
            _Sample4SplitPosition = _Sample4DblList.SplitterPosition;
        }
        private void _CreateLayout4()
        {
            var bounds = _SampleBounds;
            _Sample4DblList.Bounds = new Rectangle(bounds.X, bounds.Y + 62, bounds.Width, bounds.Height - 65);
        }
        private void _DisposeSample4()
        {
            _Sample4DblList?.RemoveControlFromParent();
            _Sample4DblList = null;

            _DisposeSample4Params();
        }
        private void _Sample4_TargetMenuItemsChanged(object sender, DxListBoxMenuItemsChangedEventArgs e)
        {
            if (__Sample4ShowContentButton != null)
            {
                _Sample4_TargetMenuItemsChangeCounter++;
                __Sample4ShowContentButton.Text = $"Obsah (Změna číslo {_Sample4_TargetMenuItemsChangeCounter})";
            }
        }
        private int _Sample4_TargetMenuItemsChangeCounter;
        private DxDblListBoxPanel _Sample4DblList;
        private int _Sample4SplitPosition;
        #region Nastavování vlastností
        private void _CreateSample4Params()
        {
            __Sample4ParamsValid = false;
            var sampleDblList = _Sample4DblList;
            int x = _SampleBounds.Left + 6;
            int y1 = _SampleBounds.Top + 6;
            int y2 = _SampleBounds.Top + 28;

            __Sample4ModeLabel = DxComponent.CreateDxLabel(x, y1 + 4, 100, this._HostContainer, "DblListMode:");
            __Sample4ModeCombo = DxComponent.CreateDxImageComboBox(x, y2, 185, this._HostContainer, _Sample4ParamsChanged, toolTipText: "Zvolte režim pro DblList");

            var imgFreeTarget = "@arrowsmall|right|green";
            var imgBallance = "devav/actions/pagenext.svg";   // "@arrow|right|blue";

            var svgimg = DxComponent.GetVectorImage(imgBallance, true, ResourceImageSizeType.Small);
            var svgXml = svgimg.ToXmlString();

            __Sample4ModeCombo.ComboItems = new IMenuItem[]
            {
                new DataMenuItem(){ Text = "FixedSourceToFreeTarget", ImageName = imgFreeTarget, Tag = DxDblListBoxPanel.DblListModeType.Mode_FixedSourceToFreeTarget, ToolTipText = "Vlevo je neměnný zdroj, z něhož lze doprava přenášet libovolné položky, i opakovaně. Vpravo lze třídit a smazat. Vlevo nelze měnit." },
                new DataMenuItem(){ Text = "Ballance", ImageName = imgBallance, Tag = DxDblListBoxPanel.DblListModeType.Mode_Ballance },
                new DataMenuItem(){ Text = "None", Tag = DxDblListBoxPanel.DblListModeType.None }
            };
            var dblListMode = _Sample4DblList.DxProperties.DblListMode;
            __Sample4ModeCombo.SelectedComboItem = __Sample4ModeCombo.ComboItems.FirstOrDefault(mi => ((DxDblListBoxPanel.DblListModeType)mi.Tag) == dblListMode);
            x += 195;

            __Sample4ButtonsLabel = DxComponent.CreateDxLabel(x, y1 + 4, 100, this._HostContainer, "ButtonsPosition:");
            __Sample4ButtonsCombo = DxComponent.CreateDxImageComboBox(x, y2, 185, this._HostContainer, _Sample4ParamsChanged, toolTipText: "Zvolte umístění buttonů okolo ListBoxů a mezi nimi uprostřed");
            __Sample4ButtonsCombo.ComboItems = new IMenuItem[]
            {
                new DataMenuItem(){ Text = "None", Tag = DxDblListBoxPanel.DblButtonsPositionType.None },
                new DataMenuItem(){ Text = "Bottom", Tag = DxDblListBoxPanel.DblButtonsPositionType.Bottom, ImageName = "svgimages/align/alignhorizontalbottom.svg" },
                new DataMenuItem(){ Text = "Center", Tag = DxDblListBoxPanel.DblButtonsPositionType.Center, ImageName = "svgimages/align/alignverticalcenter.svg" },
                new DataMenuItem(){ Text = "Bottom and Center", Tag = DxDblListBoxPanel.DblButtonsPositionType.BottomAndCenter, ImageName = "svgimages/align/alignverticalcenter.svg" },
                new DataMenuItem(){ Text = "Right", Tag = DxDblListBoxPanel.DblButtonsPositionType.Right, ImageName = "svgimages/align/alignverticalright.svg" }
            };
            var dblListPosition = _Sample4DblList.DxProperties.ButtonsPosition;
            __Sample4ButtonsCombo.SelectedComboItem = __Sample4ButtonsCombo.ComboItems.FirstOrDefault(mi => ((DxDblListBoxPanel.DblButtonsPositionType)mi.Tag) == dblListPosition);
            x += 195;

            __Sample4DragDropEnabledCheck = DxComponent.CreateDxCheckEdit(x, y1, 160, this._HostContainer, "DragAndDropEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Je povoleno přemístit prvky myší (zleva doprava, nahoru a dolů)");
            __Sample4DragDropEnabledCheck.Checked = sampleDblList.DxProperties.DragAndDropEnabled;

            __Sample4MoveAllEnabledCheck = DxComponent.CreateDxCheckEdit(x, y2, 160, this._HostContainer, "MoveAllEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Je povolena akce 'Zkopírovat všechny prvky bez nutnosti jejich označení'");
            __Sample4MoveAllEnabledCheck.Checked = sampleDblList.DxProperties.MoveAllEnabled;
            x += 165;


            __Sample4ClipActionsEnabledCheck = DxComponent.CreateDxCheckEdit(x, y1, 160, this._HostContainer, "ClipboardActionsEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Jsou povoleny akce s Clipboardem (Ctrl+C, V, X)");
            __Sample4ClipActionsEnabledCheck.Checked = sampleDblList.DxProperties.ClipboardActionsEnabled;

            __Sample4DoubleClickEnabledCheck = DxComponent.CreateDxCheckEdit(x, y2, 160, this._HostContainer, "DoubleClickEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Je povolen DoubleClick: přžemístí označený prvek ze zdroje do druhého Listu na konec seznamu, bez ohledu na aktuální řádek v cíli");
            __Sample4DoubleClickEnabledCheck.Checked = sampleDblList.DxProperties.DoubleClickEnabled;
            x += 165;

            __Sample4DirectDrawCheck = DxComponent.CreateDxCheckEdit(x, y1, 160, this._HostContainer, "DrawImageDirectly", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Vykreslovat ikony v ListBoxu napřímo");
            __Sample4DirectDrawCheck.Checked = sampleDblList.DxProperties.DrawImageDirectly;
            x += 165;

            x += 35;
            __Sample4ShowContentButton = DxComponent.CreateDxSimpleButton(x, y1, 145, 40, this._HostContainer, "Zobrazit obsah", this._Sample4ShowClick);

            _Sample4_TargetMenuItemsChangeCounter = 0;
            __Sample4ParamsValid = true;
        }
        private void _DisposeSample4Params()
        {
            __Sample4ModeLabel.RemoveControlFromParent();
            __Sample4ModeLabel = null;

            __Sample4ModeCombo.RemoveControlFromParent();
            __Sample4ModeCombo = null;

            __Sample4ButtonsLabel.RemoveControlFromParent();
            __Sample4ButtonsLabel = null;

            __Sample4ButtonsCombo.RemoveControlFromParent();
            __Sample4ButtonsCombo = null;

            __Sample4MoveAllEnabledCheck.RemoveControlFromParent();
            __Sample4MoveAllEnabledCheck = null;

            __Sample4ClipActionsEnabledCheck.RemoveControlFromParent();
            __Sample4ClipActionsEnabledCheck = null;

            __Sample4DragDropEnabledCheck.RemoveControlFromParent();
            __Sample4DragDropEnabledCheck = null;

            __Sample4DoubleClickEnabledCheck.RemoveControlFromParent();
            __Sample4DoubleClickEnabledCheck = null;

            __Sample4ShowContentButton.RemoveControlFromParent();
            __Sample4ShowContentButton = null;

            __Sample4DirectDrawCheck.RemoveControlFromParent();
            __Sample4DirectDrawCheck = null;
        }
        private void _Sample4ParamsChanged(object sender, EventArgs args)
        {
            if (!__Sample4ParamsValid) return;

            var sampleDblList = _Sample4DblList;

            if (Object.ReferenceEquals(sender, __Sample4ModeCombo))
            {
                if (__Sample4ModeCombo.SelectedComboItem != null && __Sample4ModeCombo.SelectedComboItem.Tag is DxDblListBoxPanel.DblListModeType dblListMode)
                {
                    sampleDblList.DxProperties.DblListMode = dblListMode;
                    _Sample4SetTitles();
                }
            }

            if (Object.ReferenceEquals(sender, __Sample4ButtonsCombo))
            {
                if (__Sample4ButtonsCombo.SelectedComboItem != null && __Sample4ButtonsCombo.SelectedComboItem.Tag is DxDblListBoxPanel.DblButtonsPositionType buttonPosition)
                    sampleDblList.DxProperties.ButtonsPosition = buttonPosition;
            }

            if (Object.ReferenceEquals(sender, __Sample4DragDropEnabledCheck))
                sampleDblList.DxProperties.DragAndDropEnabled = __Sample4DragDropEnabledCheck.Checked;

            if (Object.ReferenceEquals(sender, __Sample4MoveAllEnabledCheck))
                sampleDblList.DxProperties.MoveAllEnabled = __Sample4MoveAllEnabledCheck.Checked;

            if (Object.ReferenceEquals(sender, __Sample4ClipActionsEnabledCheck))
                sampleDblList.DxProperties.ClipboardActionsEnabled = __Sample4ClipActionsEnabledCheck.Checked;

            if (Object.ReferenceEquals(sender, __Sample4DoubleClickEnabledCheck))
                sampleDblList.DxProperties.DoubleClickEnabled = __Sample4DoubleClickEnabledCheck.Checked;

            if (Object.ReferenceEquals(sender, __Sample4DirectDrawCheck))
                sampleDblList.DxProperties.DrawImageDirectly = __Sample4DirectDrawCheck.Checked;
            
        }
        private void _Sample4ShowClick(object sender, EventArgs args)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("<b><u>SelectedItems</u></b>");
            var selItems = _Sample4DblList.DxProperties.TargetSelectedMenuItems;
            foreach (var selItem in selItems)
                sb.AppendLine("* " + selItem.Text);
            sb.AppendLine();

            sb.AppendLine("<b><u>All Items</u></b>");
            var allItems = _Sample4DblList.DxProperties.TargetMenuItems;
            foreach (var allItem in allItems)
                sb.AppendLine("* " + allItem.Text);
            sb.AppendLine();

            DxComponent.ShowMessageInfo(sb.ToString(), "Obsah Target Listboxu");
        }
        private void _Sample4SetTitles()
        {
            var mode = _Sample4DblList.DxProperties.DblListMode;
            switch (mode)
            {
                case DxDblListBoxPanel.DblListModeType.Mode_FixedSourceToFreeTarget:
                    _Sample4DblList.DxProperties.SourceTitleText = "Nekonečný zdroj:";
                    _Sample4DblList.DxProperties.TargetTitleText = "Vybrané prvky:";
                    break;

                case DxDblListBoxPanel.DblListModeType.Mode_Ballance:
                    _Sample4DblList.DxProperties.SourceTitleText = "Dosud nepoužité prvky:";
                    _Sample4DblList.DxProperties.TargetTitleText = "Již použité prvky:";
                    break;

                case DxDblListBoxPanel.DblListModeType.None:
                    _Sample4DblList.DxProperties.SourceTitleText = null;
                    _Sample4DblList.DxProperties.TargetTitleText = null;
                    break;
            }
        }
        private DxLabelControl __Sample4ModeLabel;
        private DxImageComboBoxEdit __Sample4ModeCombo;
        private DxLabelControl __Sample4ButtonsLabel;
        private DxImageComboBoxEdit __Sample4ButtonsCombo;
        private DxCheckEdit __Sample4MoveAllEnabledCheck;
        private DxCheckEdit __Sample4ClipActionsEnabledCheck;
        private DxCheckEdit __Sample4DragDropEnabledCheck;
        private DxCheckEdit __Sample4DoubleClickEnabledCheck;
        private DxCheckEdit __Sample4DirectDrawCheck;
        private DxSimpleButton __Sample4ShowContentButton;

        private bool __Sample4ParamsValid;
        #endregion
        #endregion
        #region Sample 5 ColumnWidths + ScrollBar
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(5)]
        private void _PrepareSample5()
        {
            __Samples.Add(new SampleInfo("List s Columns", _CreateSample5, _DisposeSample5, true));
        }
        private void _CreateSample5()
        {
            var bounds = _SampleBounds;
            var position = (_Sample5SplitPosition > 0 ? _Sample5SplitPosition : 320);
            _Sample5Split = new DxSplitContainerControl() { Bounds = new Rectangle(bounds.X, bounds.Y + 52, 800, 600), SplitterOrientation = Orientation.Horizontal, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterPosition = position };
            _Sample5Split.SplitterMoved += _Sample5Split_SplitterMoved;
            this._HostContainer.Controls.Add(_Sample5Split);
            __ResizedControls.Add(_Sample5Split);                                                  // Pro _Sample1Split resizovat výšku

            var sampleList = new DxListBoxPanel() { Dock = DockStyle.Fill };                       // ListBox je Docked v Split.Panel1 a tak mu interaktivně měníme šířku
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleList.DxProperties.MultiSelectEnabled = true;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleList.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.Move_All };
            sampleList.DxProperties.EnabledKeyActions = ControlKeyActionType.Move_All;
            sampleList.DxProperties.DragDropActions = DxDragDropActionType.ReorderItems;

            sampleList.DxProperties.MenuItemColumnWidths = new int[] { 240, 120, 120 };
            _AddEventHandlers(sampleList, false);                                                  // ... , false)  =>  pro ListBox neresizovat výšku 

            this._Sample5Split.Panel1.Controls.Add(sampleList);

            _Sample5List = sampleList;

            int x = _SampleBounds.Left + 6;
            int y1 = _SampleBounds.Top + 6;
            int y2 = _SampleBounds.Top + 28;

            _Sample5SvgImagesCheck = DxComponent.CreateDxCheckEdit(x, y1, 160, this._HostContainer, "Use SVG Images!", _Sample5ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Zaškrtnuto: používat SVG ikony / nezaškrtnuto: PNG ikony");
            _Sample5SvgImagesCheck.Checked = sampleList.DxProperties.DrawImageDirectly;
            x += 185;

            _Sample5DirectDrawCheck = DxComponent.CreateDxCheckEdit(x, y1, 160, this._HostContainer, "DrawImageDirectly", _Sample5ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, toolTipText: "Vykreslovat ikony v ListBoxu napřímo");
            _Sample5DirectDrawCheck.Checked = sampleList.DxProperties.DrawImageDirectly;


            _Sample5Refill();


            _Sample5ParamsValid = true;

        }
        private void _Sample5Refill()
        {
            var sampleList = _Sample5List;
            if (sampleList is null) return;

            var useSvg = _Sample5SvgImagesCheck?.Checked ?? false;
            var iconType = (useSvg ? Randomizer.ImageResourceType.Svg : Randomizer.ImageResourceType.PngSmall);
            var menuItems = Randomizer.GetMenuItems(48, 72, iconType);
            foreach (var menuItem in menuItems.OfType<DataMenuItem>())
                createCells(menuItem);

            sampleList.DxProperties.MenuItems = menuItems;

            void createCells(DataMenuItem menuItem)
            {
                var t1 = Randomizer.GetSentence(2, 4, false);
                var t2 = Randomizer.GetSentence(1, 2, false);
                var t3 = Randomizer.GetSentence(1, 2, false);
                menuItem.Text = $"{t1}  {t2}  {t3}";
                menuItem.Cells = new string[] { t1, t2, t3 };
                menuItem.ToolTipTitle = t1;

            }
        }
        private void _Sample5Split_SplitterMoved(object sender, EventArgs e)
        {
            _Sample5SplitPosition = _Sample5Split.SplitterPosition;
        }
        private void _Sample5ParamsChanged(object sender, EventArgs args)
        {
            if (!_Sample5ParamsValid) return;

            var sampleList = _Sample5List;
            if (sampleList is null) return;

            if (Object.ReferenceEquals(sender, _Sample5DirectDrawCheck))
                sampleList.DxProperties.DrawImageDirectly = _Sample5DirectDrawCheck.Checked;

            if (Object.ReferenceEquals(sender, _Sample5SvgImagesCheck))
                _Sample5Refill();

        }
        private void _DisposeSample5()
        {
            _Sample5ParamsValid = false;

            _Sample5SvgImagesCheck.RemoveControlFromParent();
            _Sample5SvgImagesCheck = null;

            _Sample5DirectDrawCheck.RemoveControlFromParent();
            _Sample5DirectDrawCheck = null;

            _Sample5List?.RemoveControlFromParent();
            _Sample5List = null;

            _Sample5Split?.RemoveControlFromParent();
            _Sample5Split = null;
        }
        private bool _Sample5ParamsValid;
        private DxSplitContainerControl _Sample5Split;
        private DxListBoxPanel _Sample5List;
        private DxCheckEdit _Sample5SvgImagesCheck;
        private DxCheckEdit _Sample5DirectDrawCheck;
        private int _Sample5SplitPosition;
        #endregion
        #region Sample 11
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(11)]
        private void _PrepareSample11()
        {
            __Samples.Add(new SampleInfo("DataTable a Template", _CreateSample11, _DisposeSample11, true));
        }
        private void _CreateSample11()
        {
            var bounds = _SampleBounds;
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X, bounds.Y, 520, 320) };
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleList.DxProperties.DataTable = Randomizer.GetDataTable(48, 96, "id:int;name:idtext;surname:text;description:note;pocet:number;icon:imagenamepngfull;photo:thumb");
            sampleList.DxProperties.DxTemplate = _CreateTemplate11();
            sampleList.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleList.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.Move_All };
            sampleList.DxProperties.EnabledKeyActions = ControlKeyActionType.Move_All;
            sampleList.DxProperties.DragDropActions = DxDragDropActionType.ReorderItems;
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);
            _Sample11List = sampleList;
        }
        private void _DisposeSample11()
        {
            _Sample11List?.RemoveControlFromParent();
            _Sample11List = null;
        }
        private DxListBoxTemplate _CreateTemplate11()
        {
            var dxTemplate = new DxListBoxTemplate();
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "name", ColIndex = 0, RowIndex = 0, Width = 160, Height = 18, FontStyle = FontStyle.Bold, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.TopLeft });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "surname", ColIndex = 1, RowIndex = 0, Width = 280, Height = 18, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.BottomRight });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "description", ColIndex = 0, RowIndex = 1, ColSpan = 2, Width = 440, Height = 18, FontStyle = FontStyle.Italic });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "icon", ElementContent = ElementContentType.IconName, ColIndex = 2, RowIndex = 0, RowSpan = 2, Width = 48, Height = 36, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter });
            dxTemplate.ColumnNameItemId = "id";
            dxTemplate.ColumnNameToolTipTitle = "name";
            dxTemplate.ColumnNameToolTipText = "description";
            return dxTemplate;
        }
        private DxListBoxPanel _Sample11List;
        #endregion
        #region Sample 12
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(12)]
        private void _PrepareSample12()
        {
            __Samples.Add(new SampleInfo("DataTable a Template 2500 řádků", _CreateSample12, _DisposeSample12, false));
        }
        private void _CreateSample12()
        {
            var bounds = _SampleBounds;
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X, bounds.Y, 675, 700) };
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleList.DxProperties.DataTable = Randomizer.GetDataTable(2300, 2500, "id:int;name:idtext;surname:text;description:note;pocet:number;icon:imagenamepngfull;photo:thumb");
            sampleList.DxProperties.DxTemplate = _CreateTemplate12();
            sampleList.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);
            _Sample12List = sampleList;
        }
        private void _DisposeSample12()
        {
            _Sample12List?.RemoveControlFromParent();
            _Sample12List = null;
        }
        private DxListBoxTemplate _CreateTemplate12()
        {
            var dxTemplate = new DxListBoxTemplate();
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "icon", ElementContent = ElementContentType.IconName, ColIndex = 0, RowIndex = 0, RowSpan = 2, Width = 48, Height = 36, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "name", ColIndex = 1, RowIndex = 0, Width = 160, Height = 18, FontStyle = FontStyle.Bold, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.TopLeft });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "surname", ColIndex = 2, RowIndex = 0, Width = 280, Height = 18, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.BottomRight });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "pocet", ColIndex = 3, RowIndex = 0, Width = 80, Height = 18, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleRight });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "description", ColIndex = 1, RowIndex = 1, ColSpan = 3, Width = 520, Height = 18, FontStyle = FontStyle.Italic });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "photo", ColIndex = 4, RowIndex = 0, ColSpan = 1, RowSpan = 2, Width = 80, Height = 48, ElementContent = ElementContentType.ImageData });
            dxTemplate.ColumnNameItemId = "id";
            dxTemplate.ColumnNameToolTipTitle = "name";
            dxTemplate.ColumnNameToolTipText = "description";
            return dxTemplate;
        }
        private DxListBoxPanel _Sample12List;
        #endregion
        #region Sample 13
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(13)]
        private void _PrepareSample13()
        {
            __Samples.Add(new SampleInfo("DataTable a SimpleLayout", _CreateSample13, _DisposeSample13, false));
        }
        private void _CreateSample13()
        {
            var bounds = _SampleBounds;
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X, bounds.Y, 520, 320) };
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleList.DxProperties.DataTable = Randomizer.GetDataTable(48, 96, "id:int;name:idtext;surname:text;description:note;pocet:number;icon:imagenamepngfull;photo:thumb");
            sampleList.DxProperties.DxTemplate = sampleList.DxProperties.CreateSimpleDxTemplate("id", "icon", "name", "description", 16);
            sampleList.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);
            _Sample13List = sampleList;
        }
        private void _DisposeSample13()
        {
            _Sample13List?.RemoveControlFromParent();
            _Sample13List = null;
        }
        private DxListBoxPanel _Sample13List;
        #endregion
        #region Sample 14
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(14)]
        private void _PrepareSample14()
        {
            __Samples.Add(new SampleInfo("Katalog hub, 80 řádků", _CreateSample14, _DisposeSample14, false));
        }
        private void _CreateSample14()
        {
            var bounds = _SampleBounds;
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X, bounds.Y, 675, 420) };
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;

            var table = Randomizer.GetDataTable(72, 96, "id:int; iconjedla:label; czname:label; latname:label; area:label; description:note; photo:photo");
            var mycelias = Randomizer.Mycelias;
            var areas = "Jižní Evropa;Střední Evropa;Severní Evropa;Východní Evropa;Západní Evropa;Nížiny;Bažiny;Sahara;Tundra;Východní Asie;Kanada;Listnaté lesy;Lesní houštiny".Split(';');
            var icons = new string[] { "images/xaf/bo_skull_32x32.png", "images/xaf/bo_attention_32x32.png", "images/xaf/action_grant_32x32.png", "images/xaf/action_bell_32x32.png" };
            for (int r = 0; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                var mycelium = Randomizer.GetItem(mycelias);
                row["iconjedla"] = Randomizer.GetItem(icons);
                row["czname"] = mycelium.Item2;
                row["latname"] = mycelium.Item1;
                row["area"] = Randomizer.GetItem(areas);
            }
            sampleList.DxProperties.DataTable = table;

            sampleList.DxProperties.DxTemplate = _CreateTemplate14();
            sampleList.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);
            _Sample14List = sampleList;
        }
        private void _DisposeSample14()
        {
            _Sample14List?.RemoveControlFromParent();
            _Sample14List = null;
        }
        private DxListBoxTemplate _CreateTemplate14()
        {
            var dxTemplate = new DxListBoxTemplate();
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "photo", ColIndex = 0, RowIndex = 0, ColSpan = 1, RowSpan = 4, Width = 240, Height = 240, ElementContent = ElementContentType.ImageData });

            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "cznamelabel", ColIndex = 1, RowIndex = 0, Width = 100, Height = 18, Label = "České jméno:", FontStyle = FontStyle.Italic, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleRight, ElementContent = ElementContentType.Label });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "czname", ColIndex = 2, RowIndex = 0, Width = 250, Height = 18, FontStyle = FontStyle.Bold, FontSizeRatio = 1.1f });

            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "latnamelabel", ColIndex = 1, RowIndex = 1, Width = 100, Height = 18, Label = "Latinské jméno:", FontStyle = FontStyle.Italic, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleRight, ElementContent = ElementContentType.Label });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "latname", ColIndex = 2, RowIndex = 1, Width = 250, Height = 18, FontStyle = FontStyle.Bold });

            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "arealabel", ColIndex = 1, RowIndex = 2, Width = 100, Height = 18, Label = "Oblast výskytu:", FontStyle = FontStyle.Italic, ContentAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleRight, ElementContent = ElementContentType.Label });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "area", ColIndex = 2, RowIndex = 2, Width = 250, Height = 18, FontStyle = FontStyle.Bold });

            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "iconjedla", ColIndex = 3, RowIndex = 0, RowSpan = 3, Width = 36, Height = 36, ElementContent = ElementContentType.IconName });

            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "description", ColIndex = 1, RowIndex = 3, ColSpan = 3, Width = 380, Height = 48, FontStyle = FontStyle.Regular});

            dxTemplate.ColumnNameItemId = "id";
            dxTemplate.ColumnNameToolTipTitle = "czname";
            dxTemplate.ColumnNameToolTipText = "description";
            return dxTemplate;
        }
        private DxListBoxPanel _Sample14List;
        #endregion
        #region Sample 21 TreeList + ListBox
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(21)]
        private void _PrepareSample21()
        {
            __Samples.Add(new SampleInfo("Tree + List", _CreateSample21, _DisposeSample21));
        }
        private void _CreateSample21()
        {
            var bounds = _SampleBounds;
            var sampleTree = new DxTreeList() { Bounds = new Rectangle(bounds.X, bounds.Y, 400, 320) };
            sampleTree.DxProperties.FilterBoxMode = RowFilterBoxMode.Client;
            sampleTree.DxProperties.AddNodes(_Sample21CreateTreeNodes());
            _Sample21Tree = sampleTree;
            this._HostContainer.Controls.Add(sampleTree);

            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(bounds.X + 410, bounds.Y, 400, 320) };
            sampleList.DxProperties.RowFilterMode = RowFilterBoxMode.Client;
            sampleList.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleList.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleList.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.SelectAll, ControlKeyActionType.Delimiter, ControlKeyActionType.Delete, ControlKeyActionType.CopyToSourceOneE, ControlKeyActionType.CopyToSourceOneC, ControlKeyActionType.CopyToSourceAllE, ControlKeyActionType.Delimiter, ControlKeyActionType.Move_All };
            sampleList.DxProperties.EnabledKeyActions = ControlKeyActionType.None;
            sampleList.DxProperties.DragDropActions = DxDragDropActionType.ImportItemsInto | DxDragDropActionType.ReorderItems;
            sampleList.DxProperties.MenuItems = Randomizer.GetMenuItems(7, Randomizer.ImageResourceType.PngSmall, true);
            sampleList.DxProperties.ListActionAfter += _Sample21ListB_ListActionAfter;
            _Sample21List = sampleList;
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);
        }
        private ITreeListNode[] _Sample21CreateTreeNodes()
        {
            var nodes = new List<ITreeListNode>();


            return nodes.ToArray();
        }
        private void _DisposeSample21()
        {
            _Sample21Tree?.RemoveControlFromParent();
            _Sample21Tree = null;

            _Sample21List?.RemoveControlFromParent();
            _Sample21List = null;
        }
        private void _Sample21ListA_ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case ControlKeyActionType.CopyToTargetOneC:
                    _Sample21List.DxProperties.InsertItems(_Sample21ListA.DxProperties.SelectedMenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToTargetOneE:
                    _Sample21List.DxProperties.InsertItems(_Sample21ListA.DxProperties.SelectedMenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToTargetAllC:
                    _Sample21List.DxProperties.InsertItems(_Sample21ListA.DxProperties.MenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToTargetAllE:
                    _Sample21List.DxProperties.InsertItems(_Sample21ListA.DxProperties.MenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
            }
        }
        private void _Sample21ListB_ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case ControlKeyActionType.CopyToSourceOneC:
                    _Sample21ListA.DxProperties.InsertItems(_Sample21List.DxProperties.SelectedMenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToSourceOneE:
                    _Sample21ListA.DxProperties.InsertItems(_Sample21List.DxProperties.SelectedMenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToSourceAllC:
                    _Sample21ListA.DxProperties.InsertItems(_Sample21List.DxProperties.MenuItems, true, true, DxItemsChangeType.UserInteractive);
                    break;
                case ControlKeyActionType.CopyToSourceAllE:
                    _Sample21ListA.DxProperties.InsertItems(_Sample21List.DxProperties.MenuItems, false, true, DxItemsChangeType.UserInteractive);
                    break;
            }
        }
        private DxTreeList _Sample21Tree;
        private DxListBoxPanel _Sample21ListA;
        private DxListBoxPanel _Sample21List;
        #endregion
    }
}
