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
using static DjSoft.Tools.ProgramLauncher.Settings;

namespace DjSoft.Tools.ProgramLauncher
{
    public partial class MainForm : BaseForm
    {
        #region Konstruktor a život okna
        public MainForm()
        {
            InitializeMainForm();
            InitializeToolBar();
            InitializePagesPanel();
            InitializeApplicationPanel();
            InitializeStatusBar();
            InitializeAppearance();

            ReloadPages();
        }
        private void InitializeMainForm()
        {
            int tabIndex = 0;
            this._MainContainer = new DSplitContainer() { IsSplitterFixed = true, SplitterDistance = 150, SplitterWidth = 1, TabIndex = ++tabIndex };
            this._ToolStrip = new DToolStrip() { TabIndex = ++tabIndex };
            this._StatusStrip = new DStatusStrip() { TabIndex = ++tabIndex };

            ((System.ComponentModel.ISupportInitialize)(this._MainContainer)).BeginInit();
            this._MainContainer.SuspendLayout();
            this._ToolStrip.SuspendLayout();
            this._StatusStrip.SuspendLayout();
            this.SuspendLayout();

            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this._MainContainer);
            this.Controls.Add(this._StatusStrip);
            this.Controls.Add(this._ToolStrip);
            this.Name = "MainForm";
            this.Text = App.Messages.TrayIconText;

            ((System.ComponentModel.ISupportInitialize)(this._MainContainer)).EndInit();
            this._MainContainer.ResumeLayout(false);
            this._ToolStrip.ResumeLayout(false);
            this._ToolStrip.PerformLayout();
            this._StatusStrip.ResumeLayout(false);
            this._StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
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
        private DSplitContainer _MainContainer;
        private DToolStrip _ToolStrip;
        private DStatusStrip _StatusStrip;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
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
            App.Settings.Changed += _SettingsChanged;

            App.CurrentAppearance = AppearanceInfo.GetItem(App.Settings.AppearanceName, true);     // Aktivuje posledně aktivní, anebo defaultní vzhled
            App.CurrentLayoutSet = ItemLayoutSet.GetItem(App.Settings.LayoutSetName, true);
            App.CurrentLanguage = LanguageSet.GetItem(App.Settings.LanguageCode, true);

            this.StatusLabelVersion.Text = "DjSoft";
        }
        /// <summary>
        /// Po změně dat v Settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _SettingsChanged(object sender, SettingChangedEventArgs args)
        {
            switch (args.ChangedProperty)
            {
                case "PageSet":
                    this._SettingsChangedPageSet();
                    break;
            }
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
            RefreshStatusBarTexts();
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
            this._ToolUndoButton = addButton(Properties.Resources.edit_undo_3_48, _ToolUndoButton_Click);
            this._ToolRedoButton = addButton(Properties.Resources.edit_redo_3_48, _ToolRedoButton_Click);
            this._ToolPreferenceButton = addButton(Properties.Resources.system_run_6_48, _ToolPreferenceButton_Click);
            this._ToolEditButton = addButton(Properties.Resources.edit_6_48, _ToolEditButton_Click);
            this._ToolMessageSyncButton = addButton(Properties.Resources.edit_text_frame_update_48, _ToolMessageSyncButton_Click);

            this._ToolPreferenceButton.Visible = false;
            this._ToolEditButton.Visible = false;
            this._ToolMessageSyncButton.Visible = !App.Messages.IsTranslateComplete;

            App.UndoRedo.CurrentStateChanged += _UndoRedoCurrentStateChanged;
            App.UndoRedo.CatchCurrentRedoData += _UndoRedoCatchCurrentRedoData;
            RefreshToolbarUndoRedoState();
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
        /// Po jakékoli změně stavu kontejneru UndoRedo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <summary>
        /// Aktualizuje texty na prvcích Toolbaru 
        /// </summary>
        private void RefreshToolbarTexts()
        {
            this._ToolAppearanceButton.ToolTipText = App.Messages.ToolStripButtonAppearanceToolTip;
            this._ToolUndoButton.ToolTipText = App.Messages.ToolStripButtonUndoToolTip;
            this._ToolRedoButton.ToolTipText = App.Messages.ToolStripButtonRedoToolTip;
            this._ToolPreferenceButton.ToolTipText = App.Messages.ToolStripButtonPreferenceToolTip;
            this._ToolEditButton.ToolTipText = App.Messages.ToolStripButtonEditToolTip;
            this._ToolMessageSyncButton.ToolTipText = App.Messages.ToolStripButtonMessageSyncToolTip;
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
        private void _ToolPreferenceButton_Click(object sender, EventArgs e) { }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: Edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolEditButton_Click(object sender, EventArgs e) { }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: MessageSync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolMessageSyncButton_Click(object sender, EventArgs e) 
        {
            App.TryRun(App.Messages.SynchronizeLanguageFiles);
            this._ToolMessageSyncButton.Visible = !App.Messages.IsTranslateComplete;
        }
        private ToolStripButton _ToolAppearanceButton;
        private ToolStripButton _ToolUndoButton;
        private ToolStripButton _ToolRedoButton;
        private ToolStripButton _ToolPreferenceButton;
        private ToolStripButton _ToolEditButton;
        private ToolStripButton _ToolMessageSyncButton;
        #endregion
        #region Undo a Redo
        /// <summary>
        /// Událost volaná po změně hodnoty v Settings.PageSet.
        /// Provedeme přenačtení obsahu stránek.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SettingsChangedPageSet()
        {
            ReloadPages();
        }
        /// <summary>
        /// Událost volaná po změně stavu UndoRedo containeru.
        /// Aktualizujeme stav Enabled pro buttony Undo a Redo v Toolbaru.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _UndoRedoCurrentStateChanged(object sender, EventArgs e)
        {
            RefreshToolbarUndoRedoState();
        }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: UNDO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolUndoButton_Click(object sender, EventArgs e)
        {
            if (App.UndoRedo.CanUndo)
                _UndoRedoApplyPageSet(App.UndoRedo.Undo());
        }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: REDO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolRedoButton_Click(object sender, EventArgs e)
        {
            if (App.UndoRedo.CanRedo)
                _UndoRedoApplyPageSet(App.UndoRedo.Redo());
        }
        /// <summary>
        /// Dostává data získaná z UndoRedo containeru a má je aplikovat do aktuálnho <see cref="Settings.PageSet"/>.
        /// Aplikuje tam jejich klon, tak aby v UndoRedo containeru zůstal izolovaný stav.
        /// </summary>
        /// <param name="undoRedoData"></param>
        private void _UndoRedoApplyPageSet(PageSetData undoRedoData)
        {
            if (undoRedoData != null)
                App.Settings.PageSet = undoRedoData.Clone(true);     // Setování vyvolá event App.Settings.Changed, takže se dostaneme do zdejší metody ReloadPages()
        }
        /// <summary>
        /// Container pro UndoRedo si zde vyžádá aktuální data ze systému, pro jejich uložení pro krok Redo.
        /// Provádí se při prvním kroku Undo, když je možné, že aktuální data v systému (tedy nynější <see cref="Settings.PageSet"/>) obsahuje změny, 
        /// které dosud nejsou uloženy v UndoRedo containeru, a právě prováděný krok Undo by je zahodil. 
        /// A uživatel by po provedení aktuálního Undo mohl chtít zpětně provést Redo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _UndoRedoCatchCurrentRedoData(object sender, UndoRedo<PageSetData>.CatchCurrentRedoDataEventArgs e)
        {
            e.RedoData = App.Settings.PageSet.Clone(true);           // true = generujeme klon se shodnými ID
        }
        /// <summary>
        /// Aktualizuje Enabled buttonu Undo a Redo podle stavu kontejneru
        /// </summary>
        private void RefreshToolbarUndoRedoState()
        {
            var canUndo = App.UndoRedo.CanUndo;
            var canRedo = App.UndoRedo.CanRedo;
            var canAny = (canUndo || canRedo);
            this._ToolUndoButton.Enabled = canUndo;
            this._ToolRedoButton.Enabled = canRedo;
            this._ToolUndoButton.Visible = canAny;
            this._ToolRedoButton.Visible = canAny;
        }
        #endregion
        #region PagesPanel
        /// <summary>
        /// Inicializace datového panelu Grupy (TabHeader vlevo)
        /// </summary>
        private void InitializePagesPanel()
        {
            var pagesPanel = new Components.InteractiveGraphicsControl();
            pagesPanel.Dock = DockStyle.Fill;
            pagesPanel.DefaultLayoutKind = DataLayoutKind.Pages;
            pagesPanel.EnabledDrag = true;
            pagesPanel.ContentSizeChanged += _AppPagesPanel_ContentSizeChanged;
            pagesPanel.InteractiveAreaClick += _PageAreaClick;
            pagesPanel.InteractiveItemClick += _PageItemClick;
            this._MainContainer.Panel1.Controls.Add(pagesPanel);

            __PagesPanel = pagesPanel;
        }
        /// <summary>
        /// Znovu načte stránky z datového objektu do záložek v levé části.
        /// Poté reaktivuje dosavadní stránku <see cref="_ActivePageData"/>, anebo první stránku
        /// Součástí je i znovunačtení aplikací z aktivní stránky = <see cref="ReloadApplications()"/>.
        /// </summary>
        private void ReloadPages()
        {
            _PagesPanel.DataItems = _PageSet.CreateInteractiveItems();         // InteractiveItems, jsou zobrazené v levém panelu a jsou myšoaktivní
            _ActivePageData = searchActivePageData();
            _PagesPanelVisible = true || _PagesPanel.DataItems.Count > 1;
            RefreshStatusBarTexts();


            // Vrátí aktivní stránku s daty
            PageData searchActivePageData()
            {
                var pages = this._Pages;

                // Máme nyní aktivní stránku: pokud je to fyzicky stránka z aktuálního seznamu, pak ji akceptujeme:
                var activePage = _ActivePageData;
                if (activePage != null && pages.Any(p => Object.ReferenceEquals(p, activePage))) return activePage;

                // Máme nyní aktivní stránku (ale ona není v aktuálním seznamu stránek): najdeme mezi aktuálními stránkami takovou, která má shodné Id:
                if (activePage != null && pages.TryFindFirst(p => p.Id == activePage.Id, out var currentPage)) return currentPage;

                // Máme alespoň první stránku?
                if (pages.Count > 0) return pages[0];

                // Nemáme nic:
                return null;
            }
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
        private PageData _ActivePageData 
        {
            get { return __ActivePageData; } 
            set 
            { 
                __ActivePageData = value;
                var pageItem = __ActivePageData?.InteractiveItem;
                if (pageItem != null) _PagesPanel.SelectedItems = new InteractiveItem[] { pageItem };
                ReloadApplications(); 
            }
        }
        private PageData __ActivePageData;
        /// <summary>
        /// Akce volaná při události "Změna velikosti ContentSize" v panelu "Group".
        /// Nastaví odpovídající šířku celého bočního panelu tak, aby byla vidět celá šířka bez vodorovného scrollbaru, to by bylo obtěžující.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AppPagesPanel_ContentSizeChanged(object sender, EventArgs e)
        {
            var groupContentSize = _PagesPanel.ContentSize;
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
                this._PageSet.ShowContextMenu(e.MouseState, this._PagesPanel, this._PageSet, null);
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
                this._PageSet.ShowContextMenu(e.MouseState, this._PagesPanel, this._PageSet, pageData);
            }
        }
        /// <summary>
        /// Panel 1 (zobrazuje Pages) je viditelný?
        /// </summary>
        private bool _PagesPanelVisible { get{ return !this._MainContainer.Panel1Collapsed; } set { this._MainContainer.Panel1Collapsed = !value; } }
        /// <summary>
        /// Šířka disponibilního prostoru v panelu skupin <see cref="_PagesPanel"/>
        /// </summary>
        private int _PagesPanelWidth
        {
            get { return this._MainContainer.SplitterDistance - _PagesPanel.VerticalScrollBarWidth - 2; }
            set { int width = value + _PagesPanel.VerticalScrollBarWidth + 2; this._MainContainer.SplitterDistance = (width < 50 ? 50 : width); }
        }
        /// <summary>
        /// Interaktovní panel zobrazující stránky (Pages)
        /// </summary>
        private InteractiveGraphicsControl _PagesPanel { get { return __PagesPanel; } }
        private InteractiveGraphicsControl __PagesPanel;
        #endregion
        #region ApplicationPanel
        /// <summary>
        /// Inicializace datového panelu Aplikace (ikony v hlavní ploše)
        /// </summary>
        private void InitializeApplicationPanel()
        {
            var applicationsPanel = new Components.InteractiveGraphicsControl();
            applicationsPanel.Dock = DockStyle.Fill;
            applicationsPanel.DefaultLayoutKind = DataLayoutKind.Applications;
            applicationsPanel.EnabledDrag = true;
            applicationsPanel.InteractiveAreaClick += _ApplicationAreaClick;
            applicationsPanel.InteractiveItemClick += _ApplicationItemClick;
            applicationsPanel.InteractiveItemMouseEnter += _ApplicationItemMouseEnter;
            applicationsPanel.InteractiveItemMouseLeave += _ApplicationItemMouseLeave;
            this._MainContainer.Panel2.Controls.Add(applicationsPanel);

            __ApplicationsPanel = applicationsPanel;
        }
        /// <summary>
        /// Načte aplikace z aktivní stránky a vepíše je do panelu s nabídkou aplikací
        /// </summary>
        /// <param name="pageData"></param>
        private void ReloadApplications()
        {
            _ApplicationsPanel.DataItems = _ActivePageData?.CreateInteractiveItems();

            // Grupa má možnost definovat barvu BackColor pro svoje tlačítko a pro celou stránku s aplikacemi:
            this._ApplicationsPanel.BackColorUser = _ActivePageData?.BackColor;       // Pokud stránka není určena, pak jako BackColorUser bude null = default
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
                this._PageSet.ShowContextMenu(e.MouseState, this._ApplicationsPanel, this._ActivePageData, null);
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
                this._PageSet.ShowContextMenu(e.MouseState, this._ApplicationsPanel, this._ActivePageData, dataInfo);
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
        private InteractiveGraphicsControl _ApplicationsPanel { get { return __ApplicationsPanel; } }
        /// <summary>
        /// Instance interaktivního panelu pro Aplikace
        /// </summary>
        private InteractiveGraphicsControl __ApplicationsPanel;
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
        /// Aktualizuje texty do StatusBaru
        /// </summary>
        public void RefreshStatusBarTexts()
        {
            RefreshStatusBarLabelData();
        }
        /// <summary>
        /// Refreshuje obsah <see cref="StatusLabelData"/> = počet stránek a počet aplikací, v aktuálním jazyce.
        /// </summary>
        private void RefreshStatusBarLabelData()
        {
            string pageText = App.GetCountText(_PageSet.PagesCount, App.Messages.StatusStripPageCountText);
            string appText = App.GetCountText(_PageSet.ApplicationsCount, App.Messages.StatusStripApplicationText);
            this.StatusLabelData.Text = $"{pageText}; {appText}";
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
