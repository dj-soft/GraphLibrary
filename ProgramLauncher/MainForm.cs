using DjSoft.Tools.ProgramLauncher.Components;
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
    public partial class MainForm : BaseForm
    {
        public MainForm()
        {
            InitializeComponent();
            InitializeAppearance();
            Tests();
            InitializeToolBar();
            InitializeGroupPanel();
            InitializeApplicationPanel();
            InitializeStatusBar();
        }
        private void Tests()
        {
            this.Bounds = new Rectangle(20, 300, 900, 600);

            var file = App.Settings.FileName;
            App.Settings.AppearanceName = "Default";
            App.Settings.SaveNow();

            var keys = Monitors.CurrentMonitorsKey;
            var myBounds = new Rectangle(10, 10, 780, 380);
            myBounds.DetectRelation(new Rectangle(1200, 50, 600, 120), out var dist1, out var boun1);
            myBounds.DetectRelation(new Rectangle(1200, 500, 600, 120), out var dist2, out var boun2);
            myBounds.DetectRelation(new Rectangle(700, 300, 200, 200), out var dist3, out var boun3);
            myBounds.DetectRelation(new Rectangle(300, 100, 50, 50), out var dist4, out var boun4);
            myBounds.DetectRelation(new Rectangle(-10, -10, 50, 50), out var dist5, out var boun5);

            var monb1 = Monitors.GetNearestMonitorBounds(this.Bounds);
            var monb2 = Monitors.GetNearestMonitorBounds(new Rectangle(-20, -20, 60, 1800));
        }

        #region Appearance
        /// <summary>
        /// Inicializace vzhledu a Settings a ukládání a tak
        /// </summary>
        private void InitializeAppearance()
        {
            this.SettingsName = "MainForm";                                    // Zajistí ukládání a restore pozice tohoto okna
            App.Settings.AutoSaveDelay = TimeSpan.FromMilliseconds(5000);      // Změny Settings se uloží do 5 sekund od poslední změny (tedy i změny v posunu okna)

            App.CurrentAppearanceChanged += CurrentAppearanceChanged;          // Po změně vzhledu v App.CurrentAppearance proběhne tento event-handler

            string appearanceName = App.Settings.AppearanceName;
            App.CurrentAppearance = AppearanceInfo.GetItem(appearanceName, true);        // Aktivuje posledně aktivní, anebo defaultní vzhled
        }
        /// <summary>
        /// Obsluha události po změně vzhledu z <see cref="App.CurrentAppearanceChanged"/>.
        /// Tato metoda neukládá nastavenou hodnotu do Settings <see cref="Settings.AppearanceName"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentAppearanceChanged(object sender, EventArgs e)
        {
            var toolColor = App.CurrentAppearance.ToolStripColor;
            this._ToolStrip.BackColor = toolColor;
            this._StatusStrip.BackColor = toolColor;
        }
        /// <summary>
        /// Vytvoří a zobrazí menu s výběrem vzhledu.
        /// </summary>
        private void _ShowAppearanceMenu()
        {
            var tsb = _ToolAppearanceButton.Bounds;
            var ldp = new Point(tsb.Left, tsb.Bottom + 0);
            var csb = _ToolStrip.PointToScreen(ldp);
            ToolStripDropDownMenu menu = new ToolStripDropDownMenu();

            string currentName = App.CurrentAppearance.Name;
            foreach (var appearance in AppearanceInfo.Collection)
            {
                bool isCurrent = (appearance.Name == currentName);
                Image image = appearance.ImageSmall;
                var item = new ToolStripMenuItem(appearance.Name, image) { Tag = appearance };
                if (isCurrent)
                    item.Font = App.GetFont(item.Font, null, FontStyle.Bold);
                menu.Items.Add(item);
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Další vzhled");

            menu.DropShadowEnabled = true;
            menu.RenderMode = ToolStripRenderMode.Professional;
            menu.ShowCheckMargin = false;
            menu.ShowImageMargin = true;
            menu.ItemClicked += _AppearanceMenuItemClicked;
            menu.Show(csb);
        }
        /// <summary>
        /// Obsluha výběru vzhledu v menu v Toolbaru.
        /// Aktivuje vybraný vzhled jak v <see cref="App.CurrentAppearance"/>, tak jej uloží do Settings <see cref="Settings.AppearanceName"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AppearanceMenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem?.Tag is AppearanceInfo appearanceInfo)
            {
                App.CurrentAppearance = appearanceInfo;
                App.Settings.AppearanceName = appearanceInfo.Name;
            }
        }
        #endregion
        #region ToolBar
        /// <summary>
        /// Inicializuje obsah Toolbaru
        /// </summary>
        private void InitializeToolBar()
        {
            this._ToolEditButton = new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.edit_6_48, Size = new Size(52, 52), ToolTipText = "Upravit" };
            this._ToolEditButton.Click += _ToolEditButton_Click;
            this._ToolStrip.Items.Add(this._ToolEditButton);

            this._ToolAppearanceButton = new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.applications_graphics_2_48, Size = new Size(52, 52) };
            this._ToolAppearanceButton.Click += _ToolAppearanceButton_Click;
            this._ToolStrip.Items.Add(this._ToolAppearanceButton);
        }

        private void _ToolAppearanceButton_Click(object sender, EventArgs e)
        {
            _ShowAppearanceMenu();
        }


        private void _ToolEditButton_Click(object sender, EventArgs e)
        {
            
        }

        private System.Windows.Forms.ToolStripButton _ToolEditButton;
        private System.Windows.Forms.ToolStripButton _ToolAppearanceButton;
        #endregion
        #region GroupPanel
        private void InitializeGroupPanel()
        {
            __PagePanel = new Components.InteractiveGraphicsControl();
            __PagePanel.Dock = DockStyle.Fill;

            var pages = App.Settings.ProgramPages;
            __PagePanel.DataLayout = Components.DataLayout.SetSmallBrick;
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 0, "PROJEKTY", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-blue.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 1, "DOKUMENTY", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 2, "KLIENTI", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 3, "RDP", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 4, "GRAFIKA", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 5, "WIKI", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 6, "DEV EXPRESS", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 7, "WIN HELP", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 8, "LITERATURA", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 9, "privátní", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-red.png"));
            __PagePanel.DataItems.Add(CreatePageDataItem(0, 10, "tajné", @"c:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\folder-yellow.png"));
        //    __PagePanel.DataItems[2].IsActive = true;
            __PagePanel.DataItems[4].CellBackColor = ColorSet.CreateAllColors(Color.FromArgb(180, Color.DarkViolet));

            __PagePanel.ContentSizeChanged += _AppGroupPanel_ContentSizeChanged;
            __PagePanel.InteractiveItemClick += _PageItemClick;
            this._MainContainer.Panel1.Controls.Add(__PagePanel);
        }
        private InteractiveItem CreatePageDataItem(int x, int y, string mainTitle, string imageName)
        {
            InteractiveItem data = new InteractiveItem()
            {
                Adress = new Point(x, y),
                MainTitle = mainTitle,
                ImageName = imageName
            };
            return data;
        }

        /// <summary>
        /// Akce volaná při události "Změna velikosti ContentSize" v panelu "Group".
        /// Nastaví odpovídající šířku celého bočního panelu tak, aby byla vidět celá šířka bez vodorovného scrollbaru, to by bylo obtěžující.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AppGroupPanel_ContentSizeChanged(object sender, EventArgs e)
        {
            var groupContentSize = __PagePanel.ContentSize;
            if (groupContentSize.HasValue && groupContentSize.Value.Width != _GroupPanelWidth)
                _GroupPanelWidth = groupContentSize.Value.Width;
        }
        private void _PageItemClick(object sender, Components.InteractiveItemEventArgs e)
        {
            var pageData = e.Item.UserData as Data.PageData;
            var backColor = pageData?.BackColor ?? e.Item.CellBackColor?.EnabledColor;
            this.__ApplicationsPanel.BackColorUser = backColor;
        }
        /// <summary>
        /// Šířka disponibilního prostoru v panelu skupin <see cref="__PagePanel"/>
        /// </summary>
        private int _GroupPanelWidth
        {
            get { return this._MainContainer.SplitterDistance - __PagePanel.VerticalScrollBarWidth - 2; }
            set { int width = value + __PagePanel.VerticalScrollBarWidth + 2; this._MainContainer.SplitterDistance = (width < 50 ? 50 : width); }
        }
        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl __PagePanel;
        #endregion
        #region ApplicationPanel
        private void InitializeApplicationPanel()
        {
            __ApplicationsPanel = new Components.InteractiveGraphicsControl();
            __ApplicationsPanel.Dock = DockStyle.Fill;
            __ApplicationsPanel.DataLayout = DataLayout.SetMediumBrick;
            __ApplicationsPanel.InteractiveItemClick += _ApplicationItemClick;
            __ApplicationsPanel.InteractiveItemMouseEnter += _ApplicationItemMouseEnter;
            __ApplicationsPanel.InteractiveItemMouseLeave += _ApplicationItemMouseLeave;
            this._MainContainer.Panel2.Controls.Add(__ApplicationsPanel);

            var pages = App.Settings.ProgramPages;
            _SetPageContent(pages[0]);
        }

        private void _SetPageContent(PageData pageData)
        {
            var items = new List<InteractiveItem>();

            foreach (var group in pageData.Groups)
            {
                foreach (var appl in group.Applications)
                    items.Add(appl.CreateInteractiveItem());
            }

            __ApplicationsPanel.DataItems.Clear();
            __ApplicationsPanel.AddItems(items);
        }

        private InteractiveItem CreateAppDataItem(int x, int y, string mainTitle, string imageName, DataLayout dataLayout = null)
        {
            InteractiveItem data = new InteractiveItem()
            {
                Adress = new Point(x, y),
                MainTitle = mainTitle,
                ImageName = imageName,
                DataLayout = dataLayout
            };
            return data;
        }


        private void _ApplicationItemMouseEnter(object sender, Components.InteractiveItemEventArgs e)
        {
            this._StatusCurrentItemText = e.Item.MainTitle;
        }

        private void _ApplicationItemClick(object sender, Components.InteractiveItemEventArgs e)
        {
            if (e.Item.UserData is Data.ApplicationData applInfo)
                applInfo.RunApplication();

        }

        private void _ApplicationItemMouseLeave(object sender, Components.InteractiveItemEventArgs e)
        {
            this._StatusCurrentItemText = "";
        }

        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl __ApplicationsPanel;
        #endregion
        #region StatusBar
        /// <summary>
        /// Inicializuje obsah Statusbaru
        /// </summary>
        private void InitializeStatusBar()
        {
            this._StatusVersionLabel = new System.Windows.Forms.ToolStripStatusLabel() { Spring = false, AutoSize = false, Width = 100, Text = "Verze 0", TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2) };
            this._StatusStrip.Items.Add(this._StatusVersionLabel);

            this._StatusCurrentItemLabel = new System.Windows.Forms.ToolStripStatusLabel() { Spring = true, AutoSize = false, Text = "", TextAlign = ContentAlignment.MiddleLeft, BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(2) };
            this._StatusStrip.Items.Add(this._StatusCurrentItemLabel);
            

        }
        /// <summary>
        /// Text ve Statusbaru, zobrazuje typicky MainTitle prvku pod myší
        /// </summary>
        private string _StatusCurrentItemText { get { return _StatusCurrentItemLabel.Text; } set { _StatusCurrentItemLabel.Text = value; } }

        private System.Windows.Forms.ToolStripStatusLabel _StatusVersionLabel;
        private System.Windows.Forms.ToolStripStatusLabel _StatusCurrentItemLabel;
        #endregion
    }
}
