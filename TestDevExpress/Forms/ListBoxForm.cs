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
            this.ImageNameAdd = "@text|L|#002266||B|3|#88AAFF|#CCEEFF";
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
                DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, sample.ButtonText, _SampleButtonClick, tag: sample);
                y += (h + 3);
                n++;
            }
        }
        private void _SampleButtonClick(object sender, EventArgs args)
        {
            if (sender is Control control && control.Tag is SampleInfo sampleInfo)
            {
                _DisposeSamplesAll();
                sampleInfo.ButtonClick();
            }
        }
        private void _DisposeSamplesAll()
        {
            __Samples.ForEach(s => s.DisposeContent());
        }
        private List<SampleInfo> __Samples;
        private Point __SampleBegin;
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
        }

        #endregion
        #region Sample 1
        /// <summary>
        /// Metoda je volaná reflexí v <see cref="DxMainContentPrepare"/> na základě atributu [Initializer()] !!!
        /// </summary>
        [Initializer(1)]
        private void _PrepareSample1()
        {
            __Samples.Add(new SampleInfo("Jednoduchý List", _ClickSample1, _DisposeSample1));
        }
        private void _ClickSample1()
        {
            _Sample1List = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.None };
            _Sample1List.ListItems = Randomizer.GetMenuItems(24, 60, Randomizer.ImageResourceType.PngSmall);
            this.DxMainPanel.Controls.Add(_Sample1List);
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
            __Samples.Add(new SampleInfo("List s Reorder a buttony", _ClickSample2, _DisposeSample2));
        }
        private void _ClickSample2()
        {
            _Sample2List = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            _Sample2List.SelectionMode = SelectionMode.MultiExtended;
            _Sample2List.ButtonsPosition = ToolbarPosition.RightSideCenter;
            _Sample2List.ButtonsTypes = ListBoxButtonType.MoveAll;
            _Sample2List.EnabledKeyActions = KeyActionType.AllMove;
            _Sample2List.DragDropActions = DxDragDropActionType.ReorderItems;
            _Sample2List.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            this.DxMainPanel.Controls.Add(_Sample2List);
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
            __Samples.Add(new SampleInfo("Dva Listy", _ClickSample3, _DisposeSample3));
        }
        private void _ClickSample3()
        {
            _Sample3ListA = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            _Sample3ListA.SelectionMode = SelectionMode.MultiExtended;
            _Sample3ListA.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            _Sample3ListA.ButtonsTypes = ListBoxButtonType.SelectAll | ListBoxButtonType.CopyToRightOne | ListBoxButtonType.CopyToRightAll;
            _Sample3ListA.EnabledKeyActions = KeyActionType.None;
            _Sample3ListA.DragDropActions = DxDragDropActionType.CopyItemsFrom;
            _Sample3ListA.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            _Sample3ListA.ListActionAfter += _Sample3ListA_ListActionAfter;
            this.DxMainPanel.Controls.Add(_Sample3ListA);

            _Sample3ListB = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X + 410, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            _Sample3ListB.SelectionMode = SelectionMode.MultiExtended;
            _Sample3ListB.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            _Sample3ListB.ButtonsTypes = ListBoxButtonType.SelectAll | ListBoxButtonType.Delete | ListBoxButtonType.CopyToLeftOne | ListBoxButtonType.CopyToLeftAll | ListBoxButtonType.MoveAll;
            _Sample3ListB.EnabledKeyActions = KeyActionType.None;
            _Sample3ListB.DragDropActions = DxDragDropActionType.ImportItemsInto | DxDragDropActionType.ReorderItems;
            _Sample3ListB.ListItems = Randomizer.GetMenuItems(7, Randomizer.ImageResourceType.PngSmall, true);
            _Sample3ListB.ListActionAfter += _Sample3ListB_ListActionAfter;
            this.DxMainPanel.Controls.Add(_Sample3ListB);

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
                case KeyActionType.CopyToRightOne:
                case KeyActionType.CopyToRightAll:

                    break;
            }
        }
        private void _Sample3ListB_ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case KeyActionType.CopyToLeftOne:
                case KeyActionType.CopyToLeftAll:

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
            __Samples.Add(new SampleInfo("DataTable a Template", _ClickSample11, _DisposeSample11, true));
        }
        private void _ClickSample11()
        {
            _Sample11List = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            _Sample11List.DataTable = Randomizer.GetDataTable(48,96, "id:int;name:idtext;surname:text;description:note;icon:imagenamepngfull;photo:photo"); ;
            _Sample11List.DxTemplate = _CreateTemplate11();
            _Sample11List.SelectionMode = SelectionMode.MultiExtended;
            _Sample11List.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            this.DxMainPanel.Controls.Add(_Sample11List);

        }
        private void _DisposeSample11()
        {
            _Sample11List?.RemoveControlFromParent();
            _Sample11List = null;
        }

        private DxListBoxTemplate _CreateTemplate11()
        {
            var dxTemplate = new DxListBoxTemplate();
            dxTemplate.Cells.Add(new DxListBoxTemplateCell() { TextColumnName = "name", ColIndex = 0, RowIndex = 0, Width = 160, Height = 18, FontStyle = FontStyle.Bold, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.TopLeft });
            dxTemplate.Cells.Add(new DxListBoxTemplateCell() { TextColumnName = "surname", ColIndex = 1, RowIndex = 0, Width = 280, Height = 18, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.BottomRight });
            dxTemplate.Cells.Add(new DxListBoxTemplateCell() { TextColumnName = "description", ColIndex = 0, RowIndex = 1, ColSpan = 2, Width = 440, Height = 18, FontStyle = FontStyle.Italic });
            dxTemplate.Cells.Add(new DxListBoxTemplateCell() { ImageNameColumnName = "icon", ColIndex = 2, RowIndex = 0, RowSpan = 2, Width = 48, Height = 36, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter });
            return dxTemplate;
        }

        private DxListBoxPanel _Sample11List;
        #endregion
    }
}
