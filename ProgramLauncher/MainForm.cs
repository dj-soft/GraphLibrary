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
            _AppGroupPanel = new Components.InteractiveGraphicsControl();
            _AppGroupPanel.Dock = DockStyle.Fill;
            _AppGroupPanel.ContentSize = new Size(32, 600);
            this._MainContainer.Panel1.Controls.Add(_AppGroupPanel);

            _ApplicationsPanel = new Components.InteractiveGraphicsControl();
            _ApplicationsPanel.Dock = DockStyle.Fill;

            _ApplicationsPanel.DataLayout = Data.DataLayout.SetMediumBrick;

            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(0, 0, "Windows"));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(1, 0, "Hotline"));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(2, 0, "Sirius"));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(0, 1, "Notebook"));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(1, 1, "Music"));
            _ApplicationsPanel.DataItems.Add(CreateAppDataItem(2, 3, "Nastavení"));

            this._MainContainer.Panel2.Controls.Add(_ApplicationsPanel);
        }
        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl _AppGroupPanel;
        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl _ApplicationsPanel;

        private Data.DataItemBase CreateAppDataItem(int x, int y, string mainTitle)
        {
            Data.DataItemApplication data = new Data.DataItemApplication()
            {
                Adress = new Point(x, y),
                MainTitle = mainTitle
            };
            return data;
        }
    }
}
