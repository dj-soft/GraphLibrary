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

            // Pro všechny metody, které jsou označené atributem 'InitializerAttribute' je vyvolám 
            //  a získám tak pole __Samples = jednotlivá tlačítka pro jednotlivé testy:
            __Samples = new List<SampleInfo>();
            var methods = InitializerAttribute.SearchInitializerMethods(this.GetType());
            foreach (var method in methods )
                method.Invoke(this, null);       // Volá metody označené atributem [Initializer()]

            __ResizedControls = new List<Control>();

            // Na základě prvků v poli __Samples vytvořím sadu buttonů pro jednotlivé testy:
            int y = 12;
            int x = 12;
            int w = 260;
            int h = 32;
            int n = 0;
            __SampleBegin = new Point(x + w + 6, y);
            foreach (var sample in __Samples)
            {
                if (n > 0 && sample.IsNewGroup) y += 6;
                sample.Button = DxComponent.CreateDxSimpleButton(x, y, w, h, this._HostContainer, sample.ButtonText, _SampleButtonClick, tag: sample);
                y += (h + 3);
                n++;
            }

            _HostContainer.ClientSizeChanged += _HostContainer_ClientSizeChanged;
        }
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
        private Control _HostContainer { get { return this.DxMainPanel; } }
        private List<SampleInfo> __Samples;
        private SampleInfo __CurrentSample;
        private Point __SampleBegin;
        private List<Control> __ResizedControls;
        private Rectangle __SampleBounds;

        private void _HostContainer_ClientSizeChanged(object sender, EventArgs e)
        {
            _ResizeChildControls();
        }
        private void _ResizeChildControls()
        { 
            var clientSize = _HostContainer.ClientSize;
            int top = __SampleBegin.Y;
            int bottom = clientSize.Height - top;
            int height = bottom - top;

            foreach (var child in __ResizedControls)
            {
                child.Top = top;
                child.Height = height;
            }

            __SampleBounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, clientSize.Width - __SampleBegin.X - 3, clientSize.Height - __SampleBegin.Y - 3);
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320) };
            sampleList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.None;
            sampleList.DxProperties.MenuItems = Randomizer.GetMenuItems(24, 60, Randomizer.ImageResourceType.PngSmall);
            _AddEventHandlers(sampleList, true);
            this._HostContainer.Controls.Add(sampleList);

            _Sample1List = sampleList;
        }
        private void _DisposeSample1()
        {
            _Sample1List?.RemoveControlFromParent();
            _Sample1List = null;
        }
        private DxListBoxPanel _Sample1List;
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320) };
            sampleList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
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
        #region Sample 3
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
            var sampleListA = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 400, 320) };
            sampleListA.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
            sampleListA.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleListA.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListA.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.SelectAll, ControlKeyActionType.Delimiter, ControlKeyActionType.CopyToRightOne, ControlKeyActionType.CopyToRightAll, ControlKeyActionType.CopyToRightOne, ControlKeyActionType.CopyToRightAll };
            sampleListA.DxProperties.EnabledKeyActions = ControlKeyActionType.None;
            sampleListA.DxProperties.DragDropActions = DxDragDropActionType.CopyItemsFrom;
            sampleListA.DxProperties.MenuItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            sampleListA.DxProperties.ListActionAfter += _Sample3ListA_ListActionAfter;
            _AddEventHandlers(sampleListA, true);
            this._HostContainer.Controls.Add(sampleListA);

            var sampleListB = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X + 410, __SampleBegin.Y, 400, 320) };
            sampleListB.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
            sampleListB.DxProperties.SelectionMode = SelectionMode.MultiExtended;
            sampleListB.DxProperties.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListB.DxProperties.ButtonsTypes = new ControlKeyActionType[] { ControlKeyActionType.SelectAll, ControlKeyActionType.Delimiter, ControlKeyActionType.Delete, ControlKeyActionType.CopyToLeftOne, ControlKeyActionType.CopyToLeftAll, ControlKeyActionType.Delimiter, ControlKeyActionType.Move_All };
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
                case ControlKeyActionType.CopyToRightOne:
                case ControlKeyActionType.CopyToRightAll:

                    break;
            }
        }
        private void _Sample3ListB_ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case ControlKeyActionType.CopyToLeftOne:
                case ControlKeyActionType.CopyToLeftAll:

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
            var sampleDblList = new DxDblListBoxPanel();

            sampleDblList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
            sampleDblList.DxProperties.ButtonsPosition = DxDblListBoxPanel.ButtonsPositionType.Bottom;
            sampleDblList.DxProperties.SourceListReadOnly = true;
            sampleDblList.DxProperties.ClipboardActionsEnabled = true;
            sampleDblList.DxProperties.ReorderItemsEnabled = true;
            sampleDblList.DxProperties.DragAndDropEnabled = true;

            sampleDblList.DxProperties.SourceMenuItems = Randomizer.GetMenuItems(48, 80, Randomizer.ImageResourceType.PngSmall, true, true);
            sampleDblList.DxProperties.TargetMenuItems = Randomizer.GetMenuItems(3, 8, Randomizer.ImageResourceType.PngSmall, true, true);

            this._HostContainer.Controls.Add(sampleDblList);

            _Sample4DblList = sampleDblList;

            _CreateSample4Params();                        // Params = prvky pro nastavování vlastností pro testy různého nastavení
        }
        private void _CreateLayout4()
        {
            var bounds = __SampleBounds;
            _Sample4DblList.Bounds = new Rectangle(bounds.X, bounds.Y + 30, bounds.Width, bounds.Height - 30);
        }
        private void _DisposeSample4()
        {
            _Sample4DblList?.RemoveControlFromParent();
            _Sample4DblList = null;

            __Sample4ButtonsLabel.RemoveControlFromParent();
            __Sample4ButtonsLabel = null;

            __Sample4ButtonsCombo.RemoveControlFromParent();
            __Sample4ButtonsCombo = null;

            __Sample4SourceReadOnlyCheck.RemoveControlFromParent();
            __Sample4SourceReadOnlyCheck = null;

            __Sample4ClipActionsEnabledCheck.RemoveControlFromParent();
            __Sample4ClipActionsEnabledCheck = null;

            __Sample4ReorderEnabledCheck.RemoveControlFromParent();
            __Sample4ReorderEnabledCheck = null;

            __Sample4DragDropEnabledCheck.RemoveControlFromParent();
            __Sample4DragDropEnabledCheck = null;
        }
        private DxDblListBoxPanel _Sample4DblList;
        #region Nastavování vlastností

        private void _CreateSample4Params()
        {
            __Sample4ParamsValid = false;
            var sampleDblList = _Sample4DblList;
            int x = __SampleBounds.Left + 6;
            int y = __SampleBounds.Top + 6;
            __Sample4ButtonsLabel = DxComponent.CreateDxLabel(x, y + 4, 100, this._HostContainer, "ButtonsPosition:"); x += 105;

            __Sample4ButtonsCombo = DxComponent.CreateDxImageComboBox(x, y, 160, this._HostContainer, _Sample4ParamsChanged); x += 185;
            __Sample4ButtonsCombo.ComboItems = new IMenuItem[]
            {
                new DataMenuItem(){ Text = "None", Tag = DxDblListBoxPanel.ButtonsPositionType.None },
                new DataMenuItem(){ Text = "Bottom", Tag = DxDblListBoxPanel.ButtonsPositionType.Bottom, ImageName = "svgimages/align/alignhorizontalbottom.svg" },
                new DataMenuItem(){ Text = "Center", Tag = DxDblListBoxPanel.ButtonsPositionType.Center, ImageName = "svgimages/align/alignverticalcenter.svg" },
                new DataMenuItem(){ Text = "Right", Tag = DxDblListBoxPanel.ButtonsPositionType.Right, ImageName = "svgimages/align/alignverticalright.svg" }
            };

            var dblListPosition = _Sample4DblList.DxProperties.ButtonsPosition;
            __Sample4ButtonsCombo.SelectedComboItem = __Sample4ButtonsCombo.ComboItems.FirstOrDefault(mi => ((DxDblListBoxPanel.ButtonsPositionType)mi.Tag) == dblListPosition);

            __Sample4SourceReadOnlyCheck = DxComponent.CreateDxCheckEdit(x, y, 160, this._HostContainer, "SourceListReadOnly", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); x += 165;
            __Sample4SourceReadOnlyCheck.Checked = sampleDblList.DxProperties.SourceListReadOnly;

            __Sample4ClipActionsEnabledCheck = DxComponent.CreateDxCheckEdit(x, y, 160, this._HostContainer, "ClipboardActionsEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); x += 165;
            __Sample4ClipActionsEnabledCheck.Checked = sampleDblList.DxProperties.ClipboardActionsEnabled;

            __Sample4ReorderEnabledCheck = DxComponent.CreateDxCheckEdit(x, y, 160, this._HostContainer, "ReorderItemsEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); x += 165;
            __Sample4ReorderEnabledCheck.Checked = sampleDblList.DxProperties.ReorderItemsEnabled;

            __Sample4DragDropEnabledCheck = DxComponent.CreateDxCheckEdit(x, y, 160, this._HostContainer, "DragAndDropEnabled", _Sample4ParamsChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); x += 165;
            __Sample4DragDropEnabledCheck.Checked = sampleDblList.DxProperties.DragAndDropEnabled;


            __Sample4ParamsValid = true;
        }
        private void _Sample4ParamsChanged(object sender, EventArgs args )
        {
            if (!__Sample4ParamsValid) return;

            var sampleDblList = _Sample4DblList;

            if (__Sample4ButtonsCombo.SelectedComboItem != null && __Sample4ButtonsCombo.SelectedComboItem.Tag is DxDblListBoxPanel.ButtonsPositionType buttonPosition)
                sampleDblList.DxProperties.ButtonsPosition = buttonPosition;

            sampleDblList.DxProperties.SourceListReadOnly = __Sample4SourceReadOnlyCheck.Checked;
            sampleDblList.DxProperties.ClipboardActionsEnabled = __Sample4ClipActionsEnabledCheck.Checked;
            sampleDblList.DxProperties.ReorderItemsEnabled = __Sample4ReorderEnabledCheck.Checked;
            sampleDblList.DxProperties.DragAndDropEnabled = __Sample4DragDropEnabledCheck.Checked;
        }
        private DxLabelControl __Sample4ButtonsLabel;
        private DxImageComboBoxEdit __Sample4ButtonsCombo;
        private DxCheckEdit __Sample4SourceReadOnlyCheck;
        private DxCheckEdit __Sample4ClipActionsEnabledCheck;
        private DxCheckEdit __Sample4ReorderEnabledCheck;
        private DxCheckEdit __Sample4DragDropEnabledCheck;

        private bool __Sample4ParamsValid;
        #endregion
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320) };
            sampleList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 675, 700) };
            sampleList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320) };
            sampleList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 675, 420) };
            sampleList.DxProperties.RowFilterMode = DxListBoxPanel.FilterRowMode.Client;

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
    }
}
