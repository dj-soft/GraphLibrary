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
using Noris.WS.DataContracts.DxForm;
using Noris.Clients.Win.Components.AsolDX.DataForm;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy Threads
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "ListBox", buttonOrder: 130, buttonImage: "svgimages/diagramicons/insertlist.svg", buttonToolTip: "Zobrazí různé ListBoxy a testuje jejich chování", tabViewToolTip: "Testuje různé ListBoxy a jejich chování")]
    internal class ListBoxForm : DxRibbonForm
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ListBoxForm()
        {
            this.Text = "DxListBox tester";
            this.ImageName = "svgimages/diagramicons/insertlist.svg";
            this.ImageNameAdd = "@text|Lb|#002266||N|6|#88AAFF|#CCEEFF";
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _DisposeSamplesAll();
        }
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
            listPanel.SelectedItemsChanged += _SelectedItemsChanged;
            listPanel.ItemMouseClick += _ItemMouseClick;
            listPanel.ItemMouseDoubleClick += _ItemMouseDoubleClick;

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
                text += $"; ActiveItemId: {listBox.CurrentItemId}; SelectedCount: {listBox.SelectedItems.Length}";

                var visItms = listBox.ListBox.VisibleItems;
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
        private Point __SampleBegin;
        private List<Control> __ResizedControls;

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
            public readonly string ButtonText;
            public readonly Action ButtonClick;
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.None };
            sampleList.ListItems = Randomizer.GetMenuItems(24, 60, Randomizer.ImageResourceType.PngSmall);
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.RightSideCenter;
            sampleList.ButtonsTypes = ControlKeyActionType.MoveAll;
            sampleList.EnabledKeyActions = ControlKeyActionType.MoveAll;
            sampleList.DragDropActions = DxDragDropActionType.ReorderItems;
            sampleList.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
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
            var sampleListA = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleListA.SelectionMode = SelectionMode.MultiExtended;
            sampleListA.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListA.ButtonsTypes = ControlKeyActionType.SelectAll | ControlKeyActionType.CopyToRightOne | ControlKeyActionType.CopyToRightAll;
            sampleListA.EnabledKeyActions = ControlKeyActionType.None;
            sampleListA.DragDropActions = DxDragDropActionType.CopyItemsFrom;
            sampleListA.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            sampleListA.ListActionAfter += _Sample3ListA_ListActionAfter;
            _AddEventHandlers(sampleListA, true);
            this._HostContainer.Controls.Add(sampleListA);

            var sampleListB = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X + 410, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleListB.SelectionMode = SelectionMode.MultiExtended;
            sampleListB.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListB.ButtonsTypes = ControlKeyActionType.SelectAll | ControlKeyActionType.Delete | ControlKeyActionType.CopyToLeftOne | ControlKeyActionType.CopyToLeftAll | ControlKeyActionType.MoveAll;
            sampleListB.EnabledKeyActions = ControlKeyActionType.None;
            sampleListB.DragDropActions = DxDragDropActionType.ImportItemsInto | DxDragDropActionType.ReorderItems;
            sampleListB.ListItems = Randomizer.GetMenuItems(7, Randomizer.ImageResourceType.PngSmall, true);
            sampleListB.ListActionAfter += _Sample3ListB_ListActionAfter;
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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.DataTable = Randomizer.GetDataTable(48, 96, "id:int;name:idtext;surname:text;description:note;pocet:number;icon:imagenamepngfull;photo:thumb");
            sampleList.DxTemplate = _CreateTemplate11();
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleList.ButtonsTypes = ControlKeyActionType.MoveAll;
            sampleList.EnabledKeyActions = ControlKeyActionType.MoveAll;
            sampleList.DragDropActions = DxDragDropActionType.ReorderItems;
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
            __Samples.Add(new SampleInfo("DataTable a Template 2500 řádků", _CreateSample12, _DisposeSample12, true));
        }
        private void _CreateSample12()
        {
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 675, 700), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.DataTable = Randomizer.GetDataTable(2300, 2500, "id:int;name:idtext;surname:text;description:note;pocet:number;icon:imagenamepngfull;photo:thumb");
            sampleList.DxTemplate = _CreateTemplate12();
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
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
            __Samples.Add(new SampleInfo("DataTable a SimpleLayout", _CreateSample13, _DisposeSample13, true));
        }
        private void _CreateSample13()
        {
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.DataTable = Randomizer.GetDataTable(48, 96, "id:int;name:idtext;surname:text;description:note;pocet:number;icon:imagenamepngfull;photo:thumb");
            sampleList.DxTemplate = sampleList.CreateSimpleDxTemplate("id", "icon", "name", "description", 16);
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
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
            __Samples.Add(new SampleInfo("Katalog hub, 80 řádků", _CreateSample14, _DisposeSample14, true));
        }
        private void _CreateSample14()
        {
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 675, 420), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };

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
            sampleList.DataTable = table;

            sampleList.DxTemplate = _CreateTemplate14();
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
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
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "czname", ColIndex = 2, RowIndex = 0, Width = 250, Height = 18, FontStyle = FontStyle.Bold, FontSizeDelta = 1 });

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
