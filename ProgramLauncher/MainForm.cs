using DjSoft.Tools.ProgramLauncher.Data;
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
            InitializeToolBar();
            InitializeGroupPanel();
            InitializeApplicationPanel();
            InitializeStatusBar();
        }

        #region ToolBar
        private void InitializeToolBar()
        {
            App.CurrentAppearance = AppearanceSet.DarkBlue;

            this._ToolEditButton = new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.edit_6_48, Size = new Size(52, 52), ToolTipText = "Upravit" };
            this._ToolEditButton.Click += _ToolEditButton_Click;
            this._ToolStrip.Items.Add(this._ToolEditButton);

            var palette = new ToolStripDropDownButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.applications_graphics_2_48, Size = new Size(52, 52) };
            palette.DropDownItems.Add(new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, TextImageRelation = TextImageRelation.ImageBeforeText, Text = "Default", AutoSize = true, Image = Properties.Resources.bullet_green_16, ImageScaling = ToolStripItemImageScaling.None });
            palette.DropDownItems.Add(new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, TextImageRelation = TextImageRelation.ImageBeforeText, Text = "DarkBlue", AutoSize = true , Image = Properties.Resources.bullet_pink_16, ImageScaling = ToolStripItemImageScaling.None });
            this._ToolStrip.Items.Add(palette);

        }

        private void _ToolEditButton_Click(object sender, EventArgs e)
        {
            
        }

        private System.Windows.Forms.ToolStripButton _ToolEditButton;
        #endregion
        #region GroupPanel
        private void InitializeGroupPanel()
        {
            __GroupsPanel = new Components.InteractiveGraphicsControl();
            __GroupsPanel.Dock = DockStyle.Fill;

            __GroupsPanel.DataLayout = Data.DataLayout.SetSmallBrick;
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 0, "PROJEKTY", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-blue.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 1, "DOKUMENTY", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 2, "KLIENTI", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 3, "RDP", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 4, "GRAFIKA", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 5, "WIKI", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 6, "DEV EXPRESS", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 7, "WIN HELP", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 8, "LITERATURA", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 9, "privátní", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-red.png"));
            __GroupsPanel.DataItems.Add(CreateGroupDataItem(0, 10, "tajné", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __GroupsPanel.DataItems[2].IsActive = true;

            __GroupsPanel.ContentSizeChanged += _AppGroupPanel_ContentSizeChanged;
            this._MainContainer.Panel1.Controls.Add(__GroupsPanel);
        }
        private Data.DataItemBase CreateGroupDataItem(int x, int y, string mainTitle, string imageName)
        {
            Data.DataItemGroup data = new Data.DataItemGroup()
            {
                Adress = new Point(x, y),
                MainTitle = mainTitle,
                ImageName = imageName
            };
            return data;
        }

        /// <summary>
        /// Akce volaná při události "Změna velikosti ContentSize" v panelu "Group"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AppGroupPanel_ContentSizeChanged(object sender, EventArgs e)
        {
            var groupContentSize = __GroupsPanel.ContentSize;
            if (groupContentSize.HasValue && groupContentSize.Value.Width != _GroupPanelWidth)
                _GroupPanelWidth = groupContentSize.Value.Width;
        }
        /// <summary>
        /// Šířka disponibilního prostoru v panelu skupin <see cref="__GroupsPanel"/>
        /// </summary>
        private int _GroupPanelWidth
        {
            get { return this._MainContainer.SplitterDistance - __GroupsPanel.VerticalScrollBarWidth - 2; }
            set { int width = value + __GroupsPanel.VerticalScrollBarWidth + 2; this._MainContainer.SplitterDistance = (width < 50 ? 50 : width); }
        }
        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl __GroupsPanel;
        #endregion
        #region ApplicationPanel
        private void InitializeApplicationPanel()
        {
            __ApplicationsPanel = new Components.InteractiveGraphicsControl();
            __ApplicationsPanel.Dock = DockStyle.Fill;
            __ApplicationsPanel.DataLayout = Data.DataLayout.SetMediumBrick;
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(0, 0, "Windows", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\wine.png"));
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(1, 0, "Hotline", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\accessories-clock-3.png "));
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(2, 0, "Sirius", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\gmail-notify.png"));
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(0, 1, "Skupina klientů", null, Data.DataLayout.SetTitle));
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(0, 2, "Notebook", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\gtk-gnutella-3.png"));
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(1, 2, "Music", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\flsudoku.png"));
            __ApplicationsPanel.DataItems.Add(CreateAppDataItem(2, 3, "Nastavení", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\abiword.png"));


            __ApplicationsPanel.DataItemClick += _ApplicationsPanel_DataItemClick;
            __ApplicationsPanel.DataItemMouseEnter += _ApplicationsPanel_DataItemMouseEnter;
            __ApplicationsPanel.DataItemMouseLeave += _ApplicationsPanel_DataItemMouseLeave;

            this._MainContainer.Panel2.Controls.Add(__ApplicationsPanel);
        }
        private Data.DataItemBase CreateAppDataItem(int x, int y, string mainTitle, string imageName, Data.DataLayout dataLayout = null)
        {
            Data.DataItemApplication data = new Data.DataItemApplication()
            {
                Adress = new Point(x, y),
                MainTitle = mainTitle,
                ImageName = imageName,
                DataLayout = dataLayout
            };
            return data;
        }


        private void _ApplicationsPanel_DataItemMouseEnter(object sender, Components.DataItemEventArgs e)
        {
            this._StatusCurrentItemText = e.DataItem.MainTitle;
        }

        private void _ApplicationsPanel_DataItemClick(object sender, Components.DataItemEventArgs e)
        {
        }

        private void _ApplicationsPanel_DataItemMouseLeave(object sender, Components.DataItemEventArgs e)
        {
            this._StatusCurrentItemText = "";
        }

        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl __ApplicationsPanel;
        #endregion
        #region StatusBar
        private void InitializeStatusBar()
        {
            this._StatusVersionLabel = new System.Windows.Forms.ToolStripStatusLabel() { Spring = false, AutoSize = false, Width = 100, Text = "Verze 0", TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2) };
            this._StatusStrip.Items.Add(this._StatusVersionLabel);

            this._StatusCurrentItemLabel = new System.Windows.Forms.ToolStripStatusLabel() { Spring = true, AutoSize = false, Text = "", TextAlign = ContentAlignment.MiddleLeft, BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(2) };
            this._StatusStrip.Items.Add(this._StatusCurrentItemLabel);
            

        }

        private string _StatusCurrentItemText { get { return _StatusCurrentItemLabel.Text; } set { _StatusCurrentItemLabel.Text = value; } }

        private System.Windows.Forms.ToolStripStatusLabel _StatusVersionLabel;
        private System.Windows.Forms.ToolStripStatusLabel _StatusCurrentItemLabel;
        #endregion
    }
}
