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
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(48, 24));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(192, 24));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(336, 24));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(48, 80));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(192, 80));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(48, 136));

            this._MainContainer.Panel2.Controls.Add(_ApplicationsPanel);
        }
        private DjSoft.Tools.ProgramLauncher.Components.EditablePanel _AppGroupPanel;
        private DjSoft.Tools.ProgramLauncher.Components.EditablePanel _ApplicationsPanel;

        private Data.BaseData CreateAppDataItem(int x, int y)
        {
            Data.ApplicationData data = new Data.ApplicationData()
            {
                BackColor = Color.LightBlue,
                VirtualLocation = new Point(x, y),
                Size = new Size(128, 48)
            };
            return data;
        }
    }
}
