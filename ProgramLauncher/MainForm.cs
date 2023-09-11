using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            InitializePanels();
        }
        private void InitializePanels()
        {
            _AppGroupPanel = new Components.EditablePanel();
            _AppGroupPanel.Dock = DockStyle.Fill;
            this._MainContainer.Panel1.Controls.Add(_AppGroupPanel);

            _ApplicationsPanel = new Components.EditablePanel();
            _ApplicationsPanel.Dock = DockStyle.Fill;
            this._MainContainer.Panel2.Controls.Add(_ApplicationsPanel);
        }
        private DjSoft.Tools.ProgramLauncher.Components.EditablePanel _AppGroupPanel;
        private DjSoft.Tools.ProgramLauncher.Components.EditablePanel _ApplicationsPanel;
    }
}
