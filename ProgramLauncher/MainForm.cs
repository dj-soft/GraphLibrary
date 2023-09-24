using DjSoft.Tools.ProgramLauncher.Components;
using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
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
            Tests();
            InitializeToolBar();
            InitializeGroupPanel();
            InitializeApplicationPanel();
            InitializeStatusBar();
            InitializeAppearance();

            ReloadPages();
        }
        private void Tests() { }

        #region Appearance
        /// <summary>
        /// Inicializace vzhledu a Settings a ukládání a tak
        /// </summary>
        private void InitializeAppearance()
        {
            App.MainForm = this;
            this.SettingsName = "MainForm";                                    // Zajistí ukládání a restore pozice tohoto okna
            App.Settings.AutoSaveDelay = TimeSpan.FromMilliseconds(5000);      // Změny Settings se uloží do 5 sekund od poslední změny (tedy i změny v posunu okna)

            App.CurrentAppearanceChanged += CurrentAppearanceChanged;          // Po změně vzhledu v App.CurrentAppearance proběhne tento event-handler

            string appearanceName = App.Settings.AppearanceName;
            App.CurrentAppearance = AppearanceInfo.GetItem(appearanceName, true);        // Aktivuje posledně aktivní, anebo defaultní vzhled

            this.StatusLabelVersion.Text = "DjSoft";
            this.StatusLabelData.Text = "Hromada dat";
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
            var textColor = App.CurrentAppearance.StandardTextColors.EnabledColor ?? this.ForeColor;
            this._ToolStrip.BackColor = toolColor;
            this._StatusStrip.BackColor = toolColor;
            this._StatusVersionLabel.ForeColor = textColor;
            this._StatusDataLabel.ForeColor = textColor;
            this._StatusCurrentItemLabel.ForeColor = textColor;
        }
        /// <summary>
        /// Vytvoří a zobrazí menu s výběrem vzhledu.
        /// </summary>
        private void _ShowAppearanceMenu()
        {
            var buttonBounds = _ToolAppearanceButton.Bounds;
            var leftBottom = new Point(buttonBounds.Left, buttonBounds.Bottom + 0);
            var menuPoint = _ToolStrip.PointToScreen(leftBottom);

            List<IMenuItem> items = new List<IMenuItem>();
            items.Add(DataMenuItem.CreateHeader("VZHLED"));
            items.Add(DataMenuItem.CreateSeparator());
            items.AddRange(AppearanceInfo.Collection);

            App.SelectFromMenu(items, onAppearanceMenuSelect, menuPoint);

            // Po výběru prvku v menu
            void onAppearanceMenuSelect(IMenuItem selectedItem)
            {
                if (selectedItem is AppearanceInfo appearanceInfo)
                {
                    App.CurrentAppearance = appearanceInfo;
                    App.Settings.AppearanceName = appearanceInfo.Name;
                }
            }
        }
        #endregion
        #region ToolBar
        /// <summary>
        /// Inicializuje obsah Toolbaru
        /// </summary>
        private void InitializeToolBar()
        {
            this._ToolAppearanceButton = new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.applications_graphics_2_48, Size = new Size(52, 52) };
            this._ToolAppearanceButton.Click += _ToolAppearanceButton_Click;
            this._ToolStrip.Items.Add(this._ToolAppearanceButton);

            this._ToolEditButton = new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.edit_6_48, Size = new Size(52, 52), ToolTipText = "Upravit" };
            this._ToolEditButton.Click += _ToolEditButton_Click;
            this._ToolStrip.Items.Add(this._ToolEditButton);
        }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: Vzhled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolAppearanceButton_Click(object sender, EventArgs e)
        {
            _ShowAppearanceMenu();
        }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: Edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolEditButton_Click(object sender, EventArgs e)
        {
            
        }
        private System.Windows.Forms.ToolStripButton _ToolEditButton;
        private System.Windows.Forms.ToolStripButton _ToolAppearanceButton;
        #endregion
        #region GroupPanel
        /// <summary>
        /// Inicializace datového panelu Grupy (TabHeader vlevo)
        /// </summary>
        private void InitializeGroupPanel()
        {
            __PagePanel = new Components.InteractiveGraphicsControl();
            __PagePanel.Dock = DockStyle.Fill;
            __PagePanel.DataLayout = Components.DataLayout.SetSmallBrick;

            /*
            var pages = App.Settings.ProgramPages;
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
            */
            __PagePanel.ContentSizeChanged += _AppGroupPanel_ContentSizeChanged;
            __PagePanel.InteractiveItemClick += _PageItemClick;
            this._MainContainer.Panel1.Controls.Add(__PagePanel);
        }
        /// <summary>
        /// Znovu načte stránky z datového objektu do záložek v levé části.
        /// Součástí je i reload aplikací z aktivní stránky.
        /// </summary>
        private void ReloadPages(PageData activePageData = null, int? activePageIndex = null)
        {
            var items = new List<InteractiveItem>();

            var pages = _Pages;
            if (pages != null)
            {
                foreach (var page in pages)
                {
                    items.Add(page.CreateInteractiveItem());
                }
            }

            __PagePanel.DataItems.Clear();
            __PagePanel.AddItems(items);

            bool groupsForceVisible = true;
            _GroupPanelVisible = (groupsForceVisible || items.Count > 1);

            ReloadApplications(activePageData, activePageIndex);
        }
        /// <summary>
        /// Seznam stránek s nabídkami = záložky v levé části, je uložen v <see cref="Settings.ProgramPages"/>
        /// </summary>
        private List<PageData> _Pages { get { return App.Settings.ProgramPages; } }
        /// <summary>
        /// Data aktuálně zobrazené stránky
        /// </summary>
        private PageData _ActivePageData { get { return __ActivePageData; } set { __ActivePageData = value; ReloadApplications(); } } private PageData __ActivePageData;


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
        /// Panel 1 (grupy) je viditelný?
        /// </summary>
        private bool _GroupPanelVisible { get{ return !this._MainContainer.Panel1Collapsed; } set { this._MainContainer.Panel1Collapsed = !value; } }
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
        /// <summary>
        /// Inicializace datového panelu Aplikace (ikony v hlavní ploše)
        /// </summary>
        private void InitializeApplicationPanel()
        {
            __ApplicationsPanel = new Components.InteractiveGraphicsControl();
            __ApplicationsPanel.Dock = DockStyle.Fill;
            __ApplicationsPanel.DataLayout = DataLayout.SetMediumBrick;
            __ApplicationsPanel.InteractiveItemClick += _ApplicationItemClick;
            __ApplicationsPanel.InteractiveItemMouseEnter += _ApplicationItemMouseEnter;
            __ApplicationsPanel.InteractiveItemMouseLeave += _ApplicationItemMouseLeave;
            this._MainContainer.Panel2.Controls.Add(__ApplicationsPanel);
        }
        /// <summary>
        /// Načte aplikace z aktivní stránky a vepíše je do panelu s nabídkou aplikací
        /// </summary>
        /// <param name="pageData"></param>
        private void ReloadApplications(PageData activePageData = null, int? activePageIndex = null)
        {
            var items = new List<InteractiveItem>();

            PageData pageData = getPageData();
            if (pageData != null)
            {
                foreach (var group in pageData.Groups)
                {
                    items.Add(group.CreateInteractiveItem());
                    foreach (var appl in group.Applications)
                        items.Add(appl.CreateInteractiveItem());
                }
            }

            __ApplicationsPanel.DataItems.Clear();
            __ApplicationsPanel.AddItems(items);

            // Vrátí aktivní stránku s daty
            PageData getPageData()
            {
                if (activePageData != null) return activePageData;

                var pages = this._Pages;
                if (activePageIndex.HasValue && pages != null && activePageIndex.Value >= 0 && activePageIndex.Value < pages.Count) return pages[activePageIndex.Value];
                var p = _ActivePageData;
                if (p != null) return p;
                if (pages != null && pages.Count > 0) return pages[0];
                return null;
            }
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
            this.Cursor = Cursors.Hand;
            this.StatusLabelApplicationMouseText = e.Item.MainTitle;
            this.StatusLabelApplicationMouseImage = ImageKindType.DocumentProperties;
        }

        private void _ApplicationItemClick(object sender, Components.InteractiveItemEventArgs e)
        {
            if (e.Item.UserData is Data.ApplicationData applInfo)
            {
                if (e.MouseState.Buttons == MouseButtons.Left)
                    applInfo.RunApplication();
                else
                    applInfo.RunContextMenu(e.MouseState);
            }
        }

        private void _ApplicationItemMouseLeave(object sender, Components.InteractiveItemEventArgs e)
        {
            this.StatusLabelApplicationMouseText = null;
            this.StatusLabelApplicationMouseImage = null;
            this.Cursor = Cursors.Default;
        }

        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl __ApplicationsPanel;
        #endregion
        #region StatusBar
        /// <summary>
        /// Inicializuje obsah Statusbaru
        /// </summary>
        private void InitializeStatusBar()
        {
            this._StatusStrip.Height = 30;
            this._StatusStrip.ImageScalingSize = new Size(20, 20);
            this._StatusStrip.RenderMode = ToolStripRenderMode.Professional;
            this._StatusStrip.AutoSize = false;

            this._StatusVersionLabel = createLabel(120, false, Properties.Resources.amp_01_20);
            this._StatusStrip.Items.Add(this._StatusVersionLabel);
            this.__StatusLabelVersion = new StatusInfo(this._StatusVersionLabel);

            this._StatusDataLabel = createLabel(160, false);
            this._StatusStrip.Items.Add(this._StatusDataLabel);
            this.__StatusLabelData = new StatusInfo(this._StatusDataLabel);

            this._StatusCurrentItemLabel = createLabel(600, true);
            this._StatusStrip.Items.Add(this._StatusCurrentItemLabel);
            this.__StatusLabelApplication = new StatusInfo(this._StatusCurrentItemLabel);


            // Vytvoří a vrátí label do statusbaru
            ToolStripStatusLabel createLabel(int width, bool spring, Image image = null)
            {
                var label = new ToolStripStatusLabel()
                { 
                    Spring = spring,
                    AutoSize = false, 
                    Width = width,
                    Text = "", 
                    Image = image,
                    ImageScaling = ToolStripItemImageScaling.None,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleLeft, 
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    BorderSides = (spring ? ToolStripStatusLabelBorderSides.None : ToolStripStatusLabelBorderSides.Right),
                    Padding = new Padding(2) 
                };
                return label;
            }
        }
        /// <summary>
        /// Text popisující aplikaci daný pozicí myši
        /// </summary>
        public string StatusLabelApplicationMouseText { get { return __StatusLabelApplicationMouseText; } set { __StatusLabelApplicationMouseText = value; __StatusLabelApplicationValues(); } } private string __StatusLabelApplicationMouseText;
        /// <summary>
        /// Ikona popisující aplikaci daný pozicí myši
        /// </summary>
        public ImageKindType? StatusLabelApplicationMouseImage { get { return __StatusLabelApplicationMouseImage; } set { __StatusLabelApplicationMouseImage = value; __StatusLabelApplicationValues(); } } private ImageKindType? __StatusLabelApplicationMouseImage;
        /// <summary>
        /// Text popisující aplikaci daný startem aplikace, setování null vrátí text daný pozicí myši <see cref="StatusLabelApplicationMouseText"/>
        /// </summary>
        public string StatusLabelApplicationRunText { get { return __StatusLabelApplicationRunText; } set { __StatusLabelApplicationRunText = value; __StatusLabelApplicationValues(); } } private string __StatusLabelApplicationRunText;
        /// <summary>
        /// Ikona popisující aplikaci daný startem aplikace, setování null vrátí text daný pozicí myši <see cref="StatusLabelApplicationMouseText"/>
        /// </summary>
        public ImageKindType? StatusLabelApplicationRunImage { get { return __StatusLabelApplicationRunImage; } set { __StatusLabelApplicationRunImage = value; __StatusLabelApplicationValues(); } } private ImageKindType? __StatusLabelApplicationRunImage;
        /// <summary>
        /// Nastaví text a ikonu aplikace
        /// </summary>
        private void __StatusLabelApplicationValues()
        {
            StatusLabelApplication.Text = StatusLabelApplicationRunText ?? StatusLabelApplicationMouseText;
            StatusLabelApplication.ImageKind = StatusLabelApplicationRunImage ?? StatusLabelApplicationMouseImage ?? ImageKindType.None;
        }
        /// <summary>
        /// Data ve Statusbaru pro údaje Verze
        /// </summary>
        public StatusInfo StatusLabelVersion { get { return __StatusLabelVersion; } } private StatusInfo __StatusLabelVersion;
        /// <summary>
        /// Data ve Statusbaru pro údaje Data
        /// </summary>
        public StatusInfo StatusLabelData { get { return __StatusLabelData; } } private StatusInfo __StatusLabelData;
        /// <summary>
        /// Data ve Statusbaru pro údaje Aplikace
        /// </summary>
        public StatusInfo StatusLabelApplication { get { return __StatusLabelApplication; } } private StatusInfo __StatusLabelApplication;

        /// <summary>
        /// Status bar item - první text, popisuje this program
        /// </summary>
        private ToolStripStatusLabel _StatusVersionLabel;
        /// <summary>
        /// Status bar item - druhý text, popisuje stav da
        /// </summary>
        private ToolStripStatusLabel _StatusDataLabel;
        /// <summary>
        /// Status bar item - třetí text, popisuje konkrétní aplikaci ke spuštění
        /// </summary>
        private ToolStripStatusLabel _StatusCurrentItemLabel;
        #endregion
    }
}
