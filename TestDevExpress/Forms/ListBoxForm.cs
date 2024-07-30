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

            __Samples = new List<SampleInfo>();

            var methods = InitializerAttribute.SearchInitializerMethods(this.GetType());
            foreach (var method in methods )
                method.Invoke(this, null);       // Volá metody označené atributem [Initializer()]

            int y = 12;
            foreach (var sample in __Samples)
            {
                DxComponent.CreateDxSimpleButton(12, y, 260, 32, this.DxMainPanel, sample.ButtonText, _SampleButtonClick, tag: sample);
                y += 35;
            }
            __SampleBegin = new Point(290, 12);
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
            public SampleInfo(string buttonText, Action buttonClick, Action disposeContent)
            {
                this.ButtonText = buttonText;
                this.ButtonClick = buttonClick;
                this.DisposeContent = disposeContent;
            }
            public readonly string ButtonText;
            public readonly Action ButtonClick;
            public readonly Action DisposeContent;
        }

        #endregion
        #region Sample 1
        [Initializer(10)]
        private void _PrepareSample1()           // Metoda je volaná reflexí v this.DxMainContentPrepare() na základě atributu [Initializer()] !!!
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
        [Initializer(10)]
        private void _PrepareSample2()           // Metoda je volaná reflexí v this.DxMainContentPrepare() na základě atributu [Initializer()] !!!
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
        [Initializer(10)]
        private void _PrepareSample3()           // Metoda je volaná reflexí v this.DxMainContentPrepare() na základě atributu [Initializer()] !!!
        {
            __Samples.Add(new SampleInfo("Dva Listy", _ClickSample3, _DisposeSample3));
        }
        private void _ClickSample3()
        {
            _Sample3ListA = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            _Sample3ListA.SelectionMode = SelectionMode.MultiExtended;
            _Sample3ListA.ButtonsPosition = ToolbarPosition.RightSideCenter;
            _Sample3ListA.ButtonsTypes = ListBoxButtonType.SelectAll;
            _Sample3ListA.EnabledKeyActions = KeyActionType.None;
            _Sample3ListA.DragDropActions = DxDragDropActionType.CopyItemsFrom;
            _Sample3ListA.ListItems = Randomizer.GetMenuItems(36, 80, Randomizer.ImageResourceType.PngSmall, true);
            this.DxMainPanel.Controls.Add(_Sample3ListA);

            _Sample3ListB = new DxListBoxPanel() { Bounds = new Rectangle(__SampleBegin.X + 410, __SampleBegin.Y, 400, 320), RowFilterMode = DxListBoxPanel.FilterRowMode.Client };
            _Sample3ListB.SelectionMode = SelectionMode.MultiExtended;
            _Sample3ListB.ButtonsPosition = ToolbarPosition.RightSideCenter;
            _Sample3ListB.ButtonsTypes = ListBoxButtonType.SelectAll | ListBoxButtonType.Delete;
            _Sample3ListB.EnabledKeyActions = KeyActionType.None;
            _Sample3ListB.DragDropActions = DxDragDropActionType.ImportItemsInto | DxDragDropActionType.ReorderItems;
            _Sample3ListB.ListItems = Randomizer.GetMenuItems(2, Randomizer.ImageResourceType.PngSmall, true);
            this.DxMainPanel.Controls.Add(_Sample3ListB);

        }
        private void _DisposeSample3()
        {
            _Sample3ListA?.RemoveControlFromParent();
            _Sample3ListA = null;
        }
        private DxListBoxPanel _Sample3ListA;
        private DxListBoxPanel _Sample3ListB;
        #endregion
    }
}
