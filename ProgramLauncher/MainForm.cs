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
            InitializeMainForm();
            Tests();
            InitializeToolBar();
            InitializePagesPanel();
            InitializeApplicationPanel();
            InitializeStatusBar();
            InitializeAppearance();

            ReloadPages();
        }
        private void Tests() { }
        #region Okno
        private void InitializeMainForm()
        {
            
        }
        protected override void WndProc(ref Message m)
        {
            if (SingleProcess.IsShowMeWmMessage(ref m))
            {
                ReActivateForm();
                m.Result = new IntPtr(SingleProcess.RESULT_VALID);
            }
            else
            {
                base.WndProc(ref m);
            }
        }
        /// <summary>
        /// Reaktivace formuláře
        /// </summary>
        protected override void ReActivateForm()
        {
            App.HideTrayNotifyIcon();
            base.ReActivateForm();
            if (!this.ShowInTaskbar) this.ShowInTaskbar = true;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Kdy se povoluje velký Exit:
            bool enableExit = App.ApplicationIsClosing || Control.ModifierKeys == Keys.Control || e.CloseReason == CloseReason.ApplicationExitCall || App.HasArgument("QX");
            if (enableExit)
            {   
                base.OnFormClosing(e);
                return;
            }

            App.ActivateTrayNotifyIcon();
            e.Cancel = true;

            // Schováme aplikaci, ale nebudu jí dávat Visible = false:
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;


            //if (App.IsDebugMode)
            //    this.WindowState = FormWindowState.Minimized;
            //else
            //    this.Visible = false;
        }
        #endregion
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
            App.CurrentLanguageChanged += CurrentLanguageChanged;

            App.CurrentAppearance = AppearanceInfo.GetItem(App.Settings.AppearanceName, true);     // Aktivuje posledně aktivní, anebo defaultní vzhled
            App.CurrentLayoutSet = ItemLayoutSet.GetItem(App.Settings.LayoutSetName, true);
            App.CurrentLanguage = LanguageSet.GetItem(App.Settings.LanguageCode, true);

            this.StatusLabelVersion.Text = "DjSoft";
        }
        /// <summary>
        /// Obsluha události po změně vzhledu z <see cref="App.CurrentAppearanceChanged"/>.
        /// Tato metoda zajistí promítnutí barev do ToolStrip a do StatusStrip. Nikoli do datových panelů.
        /// <para/>
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CurrentLanguageChanged(object sender, EventArgs e)
        {
            RefreshToolbarTexts();
            RefreshPagesApplicationCount();
        }

        /// <summary>
        /// Vytvoří a zobrazí menu s výběrem vzhledu.
        /// </summary>
        private void _ShowAppearanceMenu()
        {
            var menuPoint = _ToolStrip.PointToScreen(_ToolAppearanceButton.Bounds.GetPoint(RectanglePointPosition.BottomLeft));

            List<IMenuItem> items = new List<IMenuItem>();
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderColorPalette));
            items.AddRange(AppearanceInfo.Collection);
            items.Add(DataMenuItem.CreateSeparator());
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderLayoutStyle));
            items.AddRange(ItemLayoutSet.Collection);
            items.Add(DataMenuItem.CreateSeparator());
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderLanguage));
            items.AddRange(LanguageSet.Collection);

            App.SelectFromMenu(items, onAppearanceMenuSelect, menuPoint);

            // Po výběru prvku v menu
            void onAppearanceMenuSelect(IMenuItem selectedItem)
            {
                if (selectedItem is AppearanceInfo appearanceInfo)
                {
                    App.CurrentAppearance = appearanceInfo;
                    App.Settings.AppearanceName = appearanceInfo.Name;
                }
                else if (selectedItem is ItemLayoutSet itemLayoutSet)
                {
                    App.CurrentLayoutSet = itemLayoutSet;
                    App.Settings.LayoutSetName = itemLayoutSet.Name;
                }
                else if (selectedItem is Language language)
                {
                    App.CurrentLanguage = language;
                    App.Settings.LanguageCode = language.Code;
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
            this._ToolAppearanceButton = addButton(Properties.Resources.applications_graphics_2_48, _ToolAppearanceButton_Click);
            this._ToolPreferenceButton = addButton(Properties.Resources.system_run_6_48, _ToolPreferenceButton_Click);
            this._ToolEditButton = addButton(Properties.Resources.edit_6_48, _ToolEditButton_Click);

            RefreshToolbarTexts();

            ToolStripButton addButton(Image image, EventHandler onClick)
            {
                var button = new ToolStripButton() { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = image, Size = new Size(52, 52), AutoToolTip = true };
                button.Click += onClick;
                this._ToolStrip.Items.Add(button);
                return button;
            }
        }
        /// <summary>
        /// Aktualizuje texty na prvcích Toolbaru 
        /// </summary>
        private void RefreshToolbarTexts()
        {
            this._ToolAppearanceButton.ToolTipText = App.Messages.ToolStripButtonAppearanceToolTip;
            this._ToolPreferenceButton.ToolTipText = App.Messages.ToolStripButtonPreferenceToolTip;
            this._ToolEditButton.ToolTipText = App.Messages.ToolStripButtonEditToolTip;
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
        /// Po kliknutí na tlačítko Toolbaru: Preference
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolPreferenceButton_Click(object sender, EventArgs e)
        { }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: Edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolEditButton_Click(object sender, EventArgs e)
        {
            
        }
        private System.Windows.Forms.ToolStripButton _ToolAppearanceButton;
        private System.Windows.Forms.ToolStripButton _ToolPreferenceButton;
        private System.Windows.Forms.ToolStripButton _ToolEditButton;
        #endregion
        #region PagesPanel
        /// <summary>
        /// Inicializace datového panelu Grupy (TabHeader vlevo)
        /// </summary>
        private void InitializePagesPanel()
        {
            __PagesPanel = new Components.InteractiveGraphicsControl();
            __PagesPanel.Dock = DockStyle.Fill;
            __PagesPanel.DefaultLayoutKind = DataLayoutKind.Pages;
            __PagesPanel.ContentSizeChanged += _AppPagesPanel_ContentSizeChanged;
            __PagesPanel.InteractiveAreaClick += _PageAreaClick;
            __PagesPanel.InteractiveItemClick += _PageItemClick;
            this._MainContainer.Panel1.Controls.Add(__PagesPanel);
        }
        /// <summary>
        /// Znovu načte stránky z datového objektu do záložek v levé části.
        /// Součástí je i reload aplikací z aktivní stránky.
        /// </summary>
        private void ReloadPages(PageData activePageData = null, int? activePageIndex = null)
        {
            var items = new List<InteractiveItem>();

            int pageCount = 0;
            int appCount = 0;
            var pages = _Pages;
            if (pages != null)
            {
                foreach (var page in pages)
                {
                    items.Add(page.CreateInteractiveItem());
                    pageCount++;
                    appCount += page.ApplicationsCount;
                }
            }

            __PagesPanel.DataItems.Clear();
            __PagesPanel.AddItems(items);

            bool groupsForceVisible = true;
            _PagesPanelVisible = (groupsForceVisible || items.Count > 1);
            ReloadApplications(activePageData, activePageIndex);
            RefreshPagesApplicationCount(pageCount, appCount);
        }
        private void RefreshPagesApplicationCount()
        {
            int pageCount = 0;
            int appCount = 0;
            var pages = _Pages;
            if (pages != null)
            {
                foreach (var page in pages)
                {
                    pageCount++;
                    appCount += page.ApplicationsCount;
                }
            }
            RefreshPagesApplicationCount(pageCount, appCount);
        }
        private void RefreshPagesApplicationCount(int pageCount, int appCount)
        {
            string pageText = App.GetCountText(pageCount, App.Messages.StatusStripPageCountText);
            string appText = App.GetCountText(appCount, App.Messages.StatusStripApplicationText);
            this.StatusLabelData.Text = $"{pageText}; {appText}";
        }
        /// <summary>
        /// Kompletní sada se stránkami
        /// </summary>
        private PageSetData _PageSet { get { return App.Settings.PageSet; } }
        /// <summary>
        /// Seznam stránek s nabídkami = záložky v levé části, je uložen v <see cref="Settings.ProgramPages"/>
        /// </summary>
        private IList<PageData> _Pages { get { return _PageSet.Pages; } }
        /// <summary>
        /// Data aktuálně zobrazené stránky
        /// </summary>
        private PageData _ActivePageData { get { return __ActivePageData; } set { __ActivePageData = value; ReloadApplications(); } } private PageData __ActivePageData;
        /// <summary>
        /// Akce volaná při události "Změna velikosti ContentSize" v panelu "Group".
        /// Nastaví odpovídající šířku celého bočního panelu tak, aby byla vidět celá šířka bez vodorovného scrollbaru, to by bylo obtěžující.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AppPagesPanel_ContentSizeChanged(object sender, EventArgs e)
        {
            var groupContentSize = __PagesPanel.ContentSize;
            if (groupContentSize.HasValue && groupContentSize.Value.Width != _PagesPanelWidth)
                _PagesPanelWidth = groupContentSize.Value.Width;
        }
        /// <summary>
        /// Kliknutí myši (levá, pravá) na prázdnou plochu mimo prvek Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PageAreaClick(object sender, Components.InteractiveItemEventArgs e)
        {
            if (e.MouseState.Buttons == MouseButtons.Right)
            {
                this._PageSet.RunContextMenu(e.MouseState, this._PageSet, null);
            }
        }
        /// <summary>
        /// Uživatel kliknul na TabHeader od Page: aktivujeme její obsah
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PageItemClick(object sender, Components.InteractiveItemEventArgs e)
        {
            var pageData = e.Item.UserData as Data.PageData;
            if (e.MouseState.Buttons == MouseButtons.Left)
            {
                if (pageData != null)
                    _ActivePageData = pageData;
            }
            else if (e.MouseState.Buttons == MouseButtons.Right)
            {   // Pravá myš: neaktivuje vybranou stránku, ale otevře pro ní menu:
                this._PageSet.RunContextMenu(e.MouseState, this._PageSet, pageData);
            }
        }
        /// <summary>
        /// Panel 1 (zobrazuje Pages) je viditelný?
        /// </summary>
        private bool _PagesPanelVisible { get{ return !this._MainContainer.Panel1Collapsed; } set { this._MainContainer.Panel1Collapsed = !value; } }
        /// <summary>
        /// Šířka disponibilního prostoru v panelu skupin <see cref="__PagesPanel"/>
        /// </summary>
        private int _PagesPanelWidth
        {
            get { return this._MainContainer.SplitterDistance - __PagesPanel.VerticalScrollBarWidth - 2; }
            set { int width = value + __PagesPanel.VerticalScrollBarWidth + 2; this._MainContainer.SplitterDistance = (width < 50 ? 50 : width); }
        }
        private DjSoft.Tools.ProgramLauncher.Components.InteractiveGraphicsControl __PagesPanel;
        #endregion
        #region ApplicationPanel
        /// <summary>
        /// Inicializace datového panelu Aplikace (ikony v hlavní ploše)
        /// </summary>
        private void InitializeApplicationPanel()
        {
            __ApplicationsPanel = new Components.InteractiveGraphicsControl();
            __ApplicationsPanel.Dock = DockStyle.Fill;
            __ApplicationsPanel.DefaultLayoutKind = DataLayoutKind.Applications;
            __ApplicationsPanel.InteractiveAreaClick += _ApplicationAreaClick;
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
                pageData.CreateInteractiveItems(items);
            __ActivePageData = pageData;

            __ApplicationsPanel.DataItems.Clear();
            __ApplicationsPanel.AddItems(items);

            // Grupa má možnost definovat barvu BackColor pro svoje tlačítko a pro celou stránku s aplikacemi:
            this.__ApplicationsPanel.BackColorUser = pageData?.BackColor;       // Pokud stránka není určena, pak jako BackColorUser bude null = default

            // Vrátí požadovanou nebo aktivní stránku s daty
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
        /// <summary>
        /// Myš vstoupila na prvek Aplikace: navazuje změna ve statusBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ApplicationItemMouseEnter(object sender, Components.InteractiveItemEventArgs e)
        {
            this.StatusLabelApplicationMouseText = e.Item.MainTitle;
            this.StatusLabelApplicationMouseImage = ImageKindType.DocumentProperties;
        }
        /// <summary>
        /// Kliknutí myši (levá, pravá) na prázdnou plochu mimo prvek Aplikace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ApplicationAreaClick(object sender, Components.InteractiveItemEventArgs e)
        {
            if (e.MouseState.Buttons == MouseButtons.Right)
            {
                this._PageSet.RunContextMenu(e.MouseState, this._ActivePageData, null);
            }
        }
        /// <summary>
        /// Kliknutí myši (levá, pravá) na prvek Aplikace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ApplicationItemClick(object sender, Components.InteractiveItemEventArgs e)
        {
            if (e.MouseState.Buttons == MouseButtons.Left)
            {
                var applInfo = e.Item.UserData as Data.ApplicationData;
                if (applInfo != null)
                    applInfo.RunApplication();
            }
            else if (e.MouseState.Buttons == MouseButtons.Right)
            {
                var dataInfo = e.Item.UserData as Data.BaseData;
                this._PageSet.RunContextMenu(e.MouseState, this._ActivePageData, dataInfo);
            }
        }
        /// <summary>
        /// Myš opustila prvek Aplikace: navazuje změna ve statusBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ApplicationItemMouseLeave(object sender, Components.InteractiveItemEventArgs e)
        {
            this.StatusLabelApplicationMouseText = null;
            this.StatusLabelApplicationMouseImage = null;
        }
        /// <summary>
        /// Instance interaktivního panelu pro Aplikace
        /// </summary>
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
