using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestDevExpress.Forms
{
    public partial class MdiChildForm : MdiBaseForm
    {
        public MdiChildForm()
        {
            InitializeComponent();
        }
        protected override void AsolInitializeControls()
        {
            _SamplePanel = new AsolSamplePanel()
            {
                Name = "DataForm",
                Dock = DockStyle.Fill
            };
            AsolPanel.Controls.Add(_SamplePanel);
        }
        private TestDevExpress.AsolSamplePanel _SamplePanel;
    }
}
