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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.None };
            sampleList.ListItems = Randomizer.GetMenuItems(24, 60, Randomizer.ImageResourceType.PngSmall);
            this.DxMainPanel.Controls.Add(sampleList);

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
            __Samples.Add(new SampleInfo("List s Reorder a buttony", _ClickSample2, _DisposeSample2));
        }
        private void _ClickSample2()
        {
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 450, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.RightSideCenter;
            sampleList.ButtonsTypes = ListBoxButtonType.MoveAll;
            sampleList.EnabledKeyActions = KeyActionType.AllMove;
            sampleList.DragDropActions = DxDragDropActionType.ReorderItems;
            sampleList.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            this.DxMainPanel.Controls.Add(sampleList);

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
            __Samples.Add(new SampleInfo("Dva Listy", _ClickSample3, _DisposeSample3));
        }
        private void _ClickSample3()
        {
            var sampleListA = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleListA.SelectionMode = SelectionMode.MultiExtended;
            sampleListA.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListA.ButtonsTypes = ListBoxButtonType.SelectAll | ListBoxButtonType.CopyToRightOne | ListBoxButtonType.CopyToRightAll;
            sampleListA.EnabledKeyActions = KeyActionType.None;
            sampleListA.DragDropActions = DxDragDropActionType.CopyItemsFrom;
            sampleListA.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            sampleListA.ListActionAfter += _Sample3ListA_ListActionAfter;
            this.DxMainPanel.Controls.Add(sampleListA);

            var sampleListB = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X + 410, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleListB.SelectionMode = SelectionMode.MultiExtended;
            sampleListB.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            sampleListB.ButtonsTypes = ListBoxButtonType.SelectAll | ListBoxButtonType.Delete | ListBoxButtonType.CopyToLeftOne | ListBoxButtonType.CopyToLeftAll | ListBoxButtonType.MoveAll;
            sampleListB.EnabledKeyActions = KeyActionType.None;
            sampleListB.DragDropActions = DxDragDropActionType.ImportItemsInto | DxDragDropActionType.ReorderItems;
            sampleListB.ListItems = Randomizer.GetMenuItems(7, Randomizer.ImageResourceType.PngSmall, true);
            sampleListB.ListActionAfter += _Sample3ListB_ListActionAfter;
            this.DxMainPanel.Controls.Add(sampleListB);

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
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.DataTable = Randomizer.GetDataTable(48,96, "id:int;name:idtext;surname:text;description:note;icon:imagenamepngfull;photo:photo"); ;
            sampleList.DxTemplate = _CreateTemplate11();
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            this.DxMainPanel.Controls.Add(sampleList);
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
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "name", ColIndex = 0, RowIndex = 0, Width = 160, Height = 18, FontStyle = FontStyle.Bold, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.TopLeft });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "surname", ColIndex = 1, RowIndex = 0, Width = 280, Height = 18, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.BottomRight });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "description", ColIndex = 0, RowIndex = 1, ColSpan = 2, Width = 440, Height = 18, FontStyle = FontStyle.Italic });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "icon", ElementContent = ElementContentType.IconName, ColIndex = 2, RowIndex = 0, RowSpan = 2, Width = 48, Height = 36, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter });
            dxTemplate.ColumnNameToolTipTitle = "surname";
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
            __Samples.Add(new SampleInfo("DataTable a Template 2500 řádků", _ClickSample12, _DisposeSample12, true));
        }
        private void _ClickSample12()
        {
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 750), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.DataTable = Randomizer.GetDataTable(2300, 2500, "id:int;name:idtext;surname:text;description:note;icon:imagenamepngfull;photo:photo"); ;
            sampleList.DxTemplate = _CreateTemplate12();
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            this.DxMainPanel.Controls.Add(sampleList);
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
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "icon", ElementContent = ElementContentType.IconName, ColIndex = 0, RowIndex = 0, RowSpan = 2, Width = 48, Height = 36, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "name", ColIndex = 1, RowIndex = 0, Width = 160, Height = 18, FontStyle = FontStyle.Bold, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.TopLeft });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "surname", ColIndex = 2, RowIndex = 0, Width = 280, Height = 18, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.BottomRight });
            dxTemplate.Elements.Add(new DxListBoxTemplateElement() { ColumnName = "description", ColIndex = 1, RowIndex = 1, ColSpan = 2, Width = 440, Height = 18, FontStyle = FontStyle.Italic });
            dxTemplate.ColumnNameToolTipTitle = "surname";
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
            __Samples.Add(new SampleInfo("DataTable a SimpleLayout", _ClickSample13, _DisposeSample13, true));
        }
        private void _ClickSample13()
        {
            var sampleList = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 520, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            sampleList.DataTable = Randomizer.GetDataTable(48, 96, "id:int;name:idtext;surname:text;description:note;icon:imagenamepngfull;photo:photo"); ;
            sampleList.DxTemplate = sampleList.CreateSimpleDxTemplate("id", "icon", "name", "description", 16);
            sampleList.SelectionMode = SelectionMode.MultiExtended;
            sampleList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            this.DxMainPanel.Controls.Add(sampleList);
            _Sample13List = sampleList;
        }
        private void _DisposeSample13()
        {
            _Sample13List?.RemoveControlFromParent();
            _Sample13List = null;
        }
        private DxListBoxPanel _Sample13List;
        #endregion
    }
}
