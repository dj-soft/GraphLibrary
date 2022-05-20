using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors.Filtering.Templates;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro měření spotřeby GDI handles
    /// </summary>
    public class HandleScanForm : DxControlForm
    {
        public HandleScanForm()
        {
            this.InitSplitContainer();
            this.InitProcessList();

        }
        protected override void InitializeForm()
        {
            this.Text = "Měření počtu SystemHandles u běžících procesů";
            base.InitializeForm();
        }
        private void InitSplitContainer()
        {
            _SplitContainer = new DxSplitContainerControl()
            {
                SplitterOrientation = System.Windows.Forms.Orientation.Vertical,
                SplitterPosition = 300,
                FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1,
                IsSplitterFixed = false,
                Dock = System.Windows.Forms.DockStyle.Fill,
            };
            this.ControlPanel.Controls.Add(_SplitContainer);
        }
        private void InitProcessList()
        {
            _ProcessListBox = new DxListBoxPanel()
            {
                ButtonsPosition = ToolbarPosition.TopSideLeft,
                ButtonsTypes = ListBoxButtonType.None
            }
        }
        private DxSplitContainerControl _SplitContainer;
        private DxListBoxPanel _ProcessListBox;

    }
}
