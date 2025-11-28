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
    /// <summary>
    /// Main formulář
    /// </summary>
    public partial class MainForm : BaseForm
    {
        #region Konstruktor a život okna
        /// <summary>
        /// Konstruktor
        /// </summary>
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
            this.Visible = false;

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
        /// <summary>
        /// Tuto metodu volá bázová třída okna (<see cref="BaseForm.OnFormStateDefault"/>) v okamžiku, kdy by měl být restorován stav okna (<see cref="Form.WindowState"/> a jeho Bounds) z dat uložených v Settings, ale tam dosud nic není.
        /// Potomek by v této metodě měl umístit okno do výchozí pozice.
        /// </summary>
        protected override void OnFormStateDefault()
        {
            this.Size = new Size(760, 580);
            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.WindowsDefaultLocation;
        }
        protected override void OnFirstShown()
        {
            this.CheckArgumentHelp();
        }
        /// <summary>
        /// Pokud v argumentech aplikace je požadavek na Help, zobrazí okno s popisem všech argumentů
        /// </summary>
        protected void CheckArgumentHelp()
        {
            bool needHelp = App.HasArgument("?") || App.HasArgument("help");
            if (!needHelp) return;

            string eol = Environment.NewLine;
            string q = "\"";
            string help = $@"Config={q}C:\Directory\SettingsFile.ini{q} => {App.Messages.HelpInfoSettingsFile}{eol}
Single => {App.Messages.HelpInfoSingleApp}{eol}
Reset => {App.Messages.HelpInfoReset}{eol}
RunTests => {App.Messages.HelpInfoRunTests}{eol}
Help => {App.Messages.HelpInfoHelp}{eol}
";
            App.ShowMessage(help, MessageBoxIcon.Information, App.Messages.HelpInfoTitle);
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
            bool enableExit = (App.ApplicationIsClosing || Control.ModifierKeys == Keys.Control || e.CloseReason == CloseReason.ApplicationExitCall || App.HasArgument("QX") || !App.HasArgument("Single"));
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
        /// Skryje okno aplikace (minimalizuje do TaskBaru Windows) po spuštění nějaké další aplikace, pokud je to dáno v konfiguraci.
        /// </summary>
        public void HideByConfig()
        {
            var isMinimize = App.Settings.MinimizeLauncherAfterAppStart;
            if (!isMinimize) return;
            this.WindowState = FormWindowState.Minimized;
        }
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

            App.ReloadFromSettings();

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

            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderPasswords));
            items.AddRange(Passwords.MenuActions);

            items.Add(DataMenuItem.CreateSeparator());
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderColorPalette));
            items.AddRange(AppearanceInfo.Collection);

            items.Add(DataMenuItem.CreateSeparator());
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderLayoutStyle));
            items.AddRange(LayoutSetInfo.Collection);

            items.Add(DataMenuItem.CreateSeparator());
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderToolTipType));
            items.AddRange(BaseForm.ToolTipMenuItems);

            items.Add(DataMenuItem.CreateSeparator());
            items.Add(DataMenuItem.CreateHeader(App.Messages.AppearanceMenuHeaderLanguage));
            items.AddRange(LanguageSet.Collection);

            // Prvky samy implementují svoji akci IMenuItem.Process(), takže nepotřebujeme klik na prvek řešit explicitně:
            App.SelectFromMenu(items, menuPoint, true, null);
        }
        #endregion
        #region ToolBar
        /// <summary>
        /// Inicializuje obsah Toolbaru
        /// </summary>
        private void InitializeToolBar()
        {
            this._ToolAppearanceButton = addButton(Properties.Resources.system_settings_2_48, _ToolAppearanceButton_Click);
            this._ToolSettingsButton = addButton(Properties.Resources.system_settings_48, _ToolSettingsButton_Click);
            this._ToolUndoButton = addButton(Properties.Resources.edit_undo_3_48, _ToolUndoButton_Click);
            this._ToolApplyButton = addButton(Properties.Resources.dialog_ok_apply_2_48, _ToolApplyButton_Click);
            this._ToolRedoButton = addButton(Properties.Resources.edit_redo_3_48, _ToolRedoButton_Click);
            this._ToolPreferenceButton = addButton(Properties.Resources.system_run_6_48, _ToolPreferenceButton_Click);
            this._ToolEditButton = addButton(Properties.Resources.edit_6_48, _ToolEditButton_Click);
            this._ToolMessageSyncButton = addButton(Properties.Resources.edit_text_frame_update_48, _ToolMessageSyncButton_Click);

            this._ToolPreferenceButton.Visible = false;
            this._ToolEditButton.Visible = false;
            _ToolMessageSyncRefresh();

            App.UndoRedo.CurrentStateChanged += _UndoRedoCurrentStateChanged;
            App.UndoRedo.CatchCurrentRedoData += _UndoRedoCatchCurrentRedoData;
            RefreshToolbarUndoRedoState();
            RefreshToolbarTexts();

            _UserToolInit();

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
            this._ToolSettingsButton.ToolTipText = App.Messages.ToolStripButtonSettingsToolTip;
            this._ToolUndoButton.ToolTipText = App.Messages.ToolStripButtonUndoToolTip;
            this._ToolApplyButton.ToolTipText = App.Messages.ToolStripButtonApplyToolTip;
            this._ToolRedoButton.ToolTipText = App.Messages.ToolStripButtonRedoToolTip;
            this._ToolPreferenceButton.ToolTipText = App.Messages.ToolStripButtonPreferenceToolTip;
            this._ToolEditButton.ToolTipText = App.Messages.ToolStripButtonEditToolTip;
            _ToolMessageSyncRefresh();
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
        /// Po kliknutí na tlačítko Toolbaru: Konfigurace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolSettingsButton_Click(object sender, EventArgs e)
        {
            App.Settings.EditData();
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
        private void _ToolEditButton_Click(object sender, EventArgs e) { IntegrityToys.Run(); }
        /// <summary>
        /// Po kliknutí na tlačítko Toolbaru: MessageSync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolMessageSyncButton_Click(object sender, EventArgs e) 
        {
            App.TryRun(App.Messages.SynchronizeLanguageFiles);
            _ToolMessageSyncRefresh();
        }
        /// <summary>
        /// Aktualizuje button <see cref="_ToolMessageSyncButton"/> (Visible a ToolTip)
        /// </summary>
        private void _ToolMessageSyncRefresh()
        {
            var translateInfo = App.Messages.NonTranslated;

            bool hasNonTranslated = (translateInfo.FilesCount > 0);
            this._ToolMessageSyncButton.Visible = hasNonTranslated;
            
            if (!hasNonTranslated)
            {
                this._ToolMessageSyncButton.ToolTipText = App.Messages.ToolStripButtonMessageSyncToolTip;
            }
            else 
            {
                string messagesText = App.GetCountText(translateInfo.MessagesCount, App.Messages.ToolStripButtonMessageSyncMessagesTexts);
                string filesText = App.GetCountText(translateInfo.FilesCount, App.Messages.ToolStripButtonMessageSyncFilesTexts);
                this._ToolMessageSyncButton.ToolTipText = App.Messages.Format(App.Messages.ToolStripButtonMessageSyncInfoToolTip, messagesText, filesText);
            }
        }
        private ToolStripButton _ToolAppearanceButton;
        private ToolStripButton _ToolSettingsButton;
        private ToolStripButton _ToolUndoButton;
        private ToolStripButton _ToolApplyButton;
        private ToolStripButton _ToolRedoButton;
        private ToolStripButton _ToolPreferenceButton;
        private ToolStripButton _ToolEditButton;
        private ToolStripButton _ToolMessageSyncButton;
        #endregion
        #region ToolBar uživatelem deklarovaný
        /// <summary>
        /// Inicializace dat pro UserToolbar
        /// </summary>
        private void _UserToolInit()
        {
            _UserToolItems = new List<ToolStripItem>();
        }
        /// <summary>
        /// Naplnění ToolButtonů pro UserToolbar
        /// </summary>
        private void _UserToolFill()
        {
            _UserToolClear();
            var toolApps = _PageSet?.ToolbarApplications;
            if (toolApps != null && toolApps.Length > 0)
            {
                var toolStrip = this._ToolStrip;
                var userToolItems = _UserToolItems;

                // Oddělovač od systémových prvků:
                var separator = new ToolStripSeparator();
                toolStrip.Items.Add(separator);
                userToolItems.Add(separator);

                // Aplikace:
                foreach (var toolApp in toolApps)
                {
                    var toolButtonItem = toolApp.CreateToolStripItem();
                    toolStrip.Items.Add(toolButtonItem);
                    userToolItems.Add(toolButtonItem);
                }
            }
        }
        /// <summary>
        /// Odebere z Toolbaru všechny User prvky, včetně Separátoru
        /// </summary>
        private void _UserToolClear()
        {
            var userToolItems = _UserToolItems;
            if (userToolItems != null && userToolItems.Count > 0)
            {
                var toolStrip = this._ToolStrip;
                foreach (var userToolItem in userToolItems)
                {
                    toolStrip.Items.Remove(userToolItem);
                    userToolItem.Dispose();
                }
                _UserToolItems.Clear();
            }
        }
        /// <summary>
        /// Pole prvků, které si do ToolBaru zvolil uživatel
        /// </summary>
        private List<ToolStripItem> _UserToolItems;
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
        /// Po kliknutí na tlačítko Toolbaru: APPLY
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolApplyButton_Click(object sender, EventArgs e)
        {
            if (App.UndoRedo.CanUndo || App.UndoRedo.CanRedo)
                App.UndoRedo.Clear();
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
        /// Aktualizuje Enabled buttonu Undo a Redo a Apply, podle stavu kontejneru
        /// </summary>
        private void RefreshToolbarUndoRedoState()
        {
            var canUndo = App.UndoRedo.CanUndo;
            var canRedo = App.UndoRedo.CanRedo;
            var canAny = (canUndo || canRedo);
            this._ToolUndoButton.Enabled = canUndo;
            this._ToolApplyButton.Enabled = canAny;
            this._ToolRedoButton.Enabled = canRedo;
            this._ToolUndoButton.Visible = canAny;
            this._ToolApplyButton.Visible = canAny;
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
            pagesPanel.InteractiveItemMouseEnter += _PageItemMouseEnter;
            pagesPanel.InteractiveItemMouseLeave += _PageItemMouseLeave;
            pagesPanel.InteractiveAreaClick += _PageAreaClick;
            pagesPanel.InteractiveItemClick += _PageItemClick;
            pagesPanel.InteractiveItemDragAndDropEnd += _PageItemDragAndDrop;

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
            _UserToolFill();
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
        /// Data aktuálně zobrazené stránky. Setování konkrétní stránky ji zvýrazní a načte její obsah do prostoru aplikací (metoda <see cref="ReloadApplications()"/>).
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
        /// Myš vstoupila na prvek Page: navazuje změna v ToolTipu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PageItemMouseEnter(object sender, InteractiveItemEventArgs e)
        {
            this.SetToolTip(__PagesPanel, e.Item.ToolTipText, e.Item.MainTitle);
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
                if (e.MouseState.ModifierKeys == Keys.Control)
                    this._PageSet.RunEditAction(e.MouseState, this._PagesPanel, this._PageSet, pageData);
                else if (pageData != null)
                    _ActivePageData = pageData;
            }
            else if (e.MouseState.Buttons == MouseButtons.Right)
            {   // Pravá myš: neaktivuje vybranou stránku, ale otevře pro ní menu:
                this._PageSet.ShowContextMenu(e.MouseState, this._PagesPanel, this._PageSet, pageData);
            }
        }
        /// <summary>
        /// DragAndDrop dokončeno v prostoru Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PageItemDragAndDrop(object sender, InteractiveDragItemEventArgs e)
        {
            if (e.EndMouseState.IsOnControl)
            {
                var dataInfo = e.Item.UserData as Data.BaseData;
                this._PageSet.MoveItem(e.BeginMouseState, e.EndMouseState, this._PagesPanel, this._PageSet, dataInfo);
            }
        }
        /// <summary>
        /// Myš opustila prvek Page: navazuje změna v ToolTipu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PageItemMouseLeave(object sender, InteractiveItemEventArgs e)
        {
            this.SetToolTip(__PagesPanel, null);
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
            applicationsPanel.InteractiveItemMouseEnter += _ApplicationItemMouseEnter;
            applicationsPanel.InteractiveItemMouseLeave += _ApplicationItemMouseLeave;
            applicationsPanel.InteractiveAreaClick += _ApplicationAreaClick;
            applicationsPanel.InteractiveItemClick += _ApplicationItemClick;
            applicationsPanel.InteractiveItemDragAndDropEnd += _ApplicationsItemDragAndDrop;
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
            this.SetToolTip(_ApplicationsPanel, e.Item.ToolTipText, e.Item.MainTitle);
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
            var itemData = e.Item.UserData as Data.BaseData;
            if (e.MouseState.Buttons == MouseButtons.Left)
            {
                var applInfo = e.Item.UserData as Data.ApplicationData;
                if (e.MouseState.ModifierKeys == Keys.Control)
                    this._PageSet.RunEditAction(e.MouseState, this._PagesPanel, this._PageSet, itemData);
                else if (applInfo != null)
                    App.TryRun(applInfo.RunApplication);
            }
            else if (e.MouseState.Buttons == MouseButtons.Right)
            {
                this._PageSet.ShowContextMenu(e.MouseState, this._ApplicationsPanel, this._ActivePageData, itemData);
            }
        }
        /// <summary>
        /// DragAndDrop dokončeno v prostoru Aplikace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ApplicationsItemDragAndDrop(object sender, InteractiveDragItemEventArgs e)
        {
            if (e.EndMouseState.IsOnControl)
            {
                var dataInfo = e.Item.UserData as Data.BaseData;
                this._PageSet.MoveItem(e.BeginMouseState, e.EndMouseState, this._ApplicationsPanel, this._ActivePageData, dataInfo);
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
            this.SetToolTip(_ApplicationsPanel, null);
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
    internal class IntegrityToys
    {
        public static void Run()
        {
            var inputtext = _GetText();
            var inputData = Item.Parse(inputtext);
            var result = new StringBuilder();
            Item.AddHtmlHeader(result);
            int docState = 0;
            string lastConvention = null;
            foreach (var item in inputData)
                item.AddItem(result, ref lastConvention, ref docState);
            Item.AddHtmlFooter(result, ref docState);
            string outputtext = result.ToString();
            System.Windows.Forms.Clipboard.SetText(outputtext);
        }
        internal class Item
        {
            public static List<Item> Parse(string text)
            {
                var items = new List<Item>();
                var lines = text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines) 
                {
                    var cells = line.Split('\t');
                    int count = cells.Length;
                    if (count >= 4)
                    {
                        var item = new Item()
                        {
                            Number = read(cells, 0),
                            Name = read(cells, 1),
                            FullName = read(cells, 2),
                            Convention = read(cells, 3),
                            State = read(cells, 4)
                        };
                        if (!item.IsEmpty)
                            items.Add(item);
                    }
                }
                items.Sort(Item.Comparer);
                return items;

                string read(string[] data, int index)
                {
                    if (index >= data.Length) return "";
                    return data[index].Trim();
                }
            }
            public static void AddHtmlHeader(StringBuilder sb)
            {
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head> ");
                sb.AppendLine("</head> ");
                sb.AppendLine("<body> ");

            }
            public void AddItem(StringBuilder sb, ref string lastConvention, ref int docState)
            {
                if (this.IsEmpty) return;

                bool isNewBlock = !String.Equals(lastConvention, this.Convention, StringComparison.CurrentCultureIgnoreCase);
                if (isNewBlock)
                {
                    AddItemEnd(sb, ref docState);

                    sb.Append(ParBegin);
                    if (!String.IsNullOrEmpty(this.Convention))
                    {
                        sb.AppendLine($"{BoldBegin}{(ToHtml(this.Convention))}: {BoldEnd}");
                    }
                    lastConvention = this.Convention;
                }

                if (docState > 0)
                    sb.AppendLine(ItemNext);

                sb.Append($"{(ToHtml(this.FullName))}");
                if (IsNrfb)
                    sb.Append($"{NbspCode}{InfoBegin}{NrfbText}{InfoEnd}");
                docState++;
            }
            public static void AddItemEnd(StringBuilder sb, ref int docState)
            {
                if (docState > 0)
                {
                    sb.Append(ItemEnd);
                    sb.Append(ParEnd);
                }
                docState = 0;
            }
            public static void AddHtmlFooter(StringBuilder sb, ref int docState)
            {
                AddItemEnd(sb, ref docState);

                sb.AppendLine("</body> ");
                sb.AppendLine("</html>");
            }
            public static string ToHtml(string text)
            {
                if (text is null) return "";
                replace("&", "&amp;");
                replace("<", "&lt;");
                replace(">", "&gt;");
                return text;

                void replace(string src, string trg)
                {
                    if (text.Contains(src)) text = text.Replace(src, trg);
                }
            }
            public static int Comparer(Item a, Item b)
            {
                int cmp = String.Compare(a.Convention, b.Convention, StringComparison.CurrentCultureIgnoreCase);
                if (cmp == 0)
                    cmp = String.Compare(a.FullName, b.FullName, StringComparison.CurrentCultureIgnoreCase);
                return cmp;
            }
            internal const string NbspCode = "&nbsp;";
            internal const string ItemNext = "; ";
            internal const string ItemEnd = ".";
            internal const string NrfbText = "[C]";
            internal const string BoldBegin = "<b>";
            internal const string BoldEnd = "</b>";
            internal const string InfoBegin = "<i>";
            internal const string InfoEnd = "</i>";
            internal const string ParBegin = "\r\n";
            internal const string ParEnd = "\r\n<br>\r\n";
            public override string ToString()
            {
                return $"{Number}. [{Convention}] => {FullName}" + (IsNrfb ? "  [NRFB]" : "");
            }
            public string Number;
            public string Name;
            public string FullName;
            public string Convention;
            public string State;
            public bool IsEmpty { get { return String.IsNullOrEmpty(FullName); } }
            public bool IsNrfb { get { return !String.IsNullOrEmpty(State) && State.IndexOf("NRFB", StringComparison.CurrentCultureIgnoreCase) >= 0; } }
        }
        private static string _GetText()
        {
            string text = @"1	Monroe Jillian	Monroe Jillian - Outfit: Golden Moment	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	NRFB
2	Kathy Keene	Kathy Keene Shimmering Dynasty	2015 Katy Keene	NRFB
3	Dree Hill	Dree Hill - Outfit: Rebel Desire	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	N
4	Kyori Sato	Fame Fable Kyori Sato	2015 Fashion Royalty	N
5	Binx Barone	Zine Queen Binx Barone	2017 The Industry	N
6	Fabiana Diaz	Fabiana Diaz - Outfit: Shanghai Bound	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	N
7	Adaline King	Adaline King - Gloss Convention (d)	2014 Perfect Pose Studio Convention	N
8	Kyori Sato	Idol Worship Kyori Sato	2015 Cinematic Convention FR	N
9	Finley Prince	Breeze Finley Prince	2015 ITBE Grab Box Dolls	NS
10	Phoebe Ashe	Rapture - Phoebe Ashe	2013 JEM and the Holograms	N
11	Jaeme Costas	Jaeme Costas - Problem Child	2016 In The Mix	N
12	Veronique Perrin	Style Counsel Veronique Perrin	2011 Jet Set Convention FR	N
13	Anja Christensen	Wrap-ture Anja	2012 Tropicalia Convention FR	Nude & Mint
14	Constance Madssen	Captivating Cocktails Constance Madssen	2017 The East 59th Collection	N
15	Laka O'Rion	Laka O'Rion (outfit: Poison) 	2016 Supermodel Convention CI	N
16	Fan Xi	Fan Xi - Outfit: Dark Victory	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	N
17	Veronique Perrin	Uriah Workshop Veronique Perrin	2012 Tropicalia Convention FR	N
18	Erin Salston	Chrome Noir Erin Salston	2014 NuFace Gloss Convention	N
19	Tulabelle	Come Thru! Tulabelle True	2017 The Industry	NRFB
20	Isha	Brightness Calls Isha	2011 Jet Set Convention FR	N
21	Adaline King	Adaline King - Gloss Convention (j)	2014 Perfect Pose Studio Convention	N
22	Jacqueline O'Rion	Jacqueline O'Rion	2018 Luxe Life Convention CI	NRFB
23	Monroe Jillian	Monroe Jillian – Beast	2013 Nu.Fantasy	N
24	Jaeme Costas	Jaeme Costas - Problem Child, rerooted černovlasá	2016 In The Mix	N
25	Lilith Blair	Never Ordinary Lilith	2015 NuFace Misc	NC
26	Erin Salston	You Look So Fine Erin Salston	2011 The Rock Fashion Wedding Collection	N
27	Natalia Fatale	Heart Stopper Natalia Fatale (Welcome Cocktail Doll)	2015 Cinematic Convention ITBE	N
28	Korinne Dimas	Polished Korinne	2013 Fashion Royalty	N
29	Veronique Perrin	Full Spectrum Veronique Perrin	2014 Urban Safari FR	N
30	Tulabelle	Lady Stardust Tulabelle	2016 The Industry	N
31	Tilda Brisby	Tilda Brisby - Gloss Convention	2014 Perfect Pose Studio Convention	N
32	Vanessa Perrin	Fashion Explorer Vanessa Perrin	2014 Urban Safari FR	N
33	Sterling Reise	Sterling Reise - Gloss Convention	2014 Perfect Pose Studio Convention	ND
34	Veronique Perrin	Modern Comeback Veronique Perrin – Noir	2011 Fashion Royalty	ND
35	Dominique Makeda	Electric Enthusiasm Dominique Makeda	2015 NuFace	
36	Dree Hill	Dree Hill - Best Thing Ever	2016 In The Mix	
37	Nigel North	Nigel North - Hot Shot	2015 The Model Scene - Poppy Parker	
38	Finley Prince	Touch of Whimsy Finley Prince	2017 NuFantasy / IFDC	
39	Eden Blair	Never Ordinary Eden	2015 NuFace Misc	
40	Eugenia Perrin	Reigning Grace Eugenia Perrin Frost	2015 Non-Mainline FR	N
41	Eugenia Perrin	As Dusk Falls Eugenia Perrin Frost	2015 Non-Mainline FR	NRFB
42	Veronique Perrin	Bewitching Veronique Perrin	2013 Premiere Convention FR	NRFB
43	Erin Salston	High End Envy Erin Salston	2012 NuFace	
44	Ollie Lawson	Ollie Lawson - Vice Effect	2015 Leading Ladies	
45	Anja Christensen	Glam Vamp Anja	2013 IFDC / NU.Fantasy	
46	Veronique Perrin	Nocturnal Glow Veronique Perrin	2014 Gloss Convention FR	
47	Erin Salston	In Rouges Erin Salston	2015 Cinematic Convention NuFace	NRFB
48	Blue Burkhart	Shimber Blue Burkhart	2018 Luxe Life Convention CI	
49	Adaline King	Adaline King	2018 Luxe Life Convention CI	
50	Giselle Diefendorf	Energetic Presence Giselle Diefendorf	2015 NuFace	
51	Mademoiselle Jolie	Ombres Poetique Mademoiselle Jolie	2014 Non-Mainline FR	
52	Alysa	Hot Chick Alysa (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
53	Rufus Blue	Punk Rock Rufus Blue (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
54	Kesenia Valentinova	Ambitious Kesenia Valentinova	2013 Fashion Royalty	
55	Isha	Style Notes Isha Kalpana Narayanan	2015 Fashion Royalty	
56	Erin Salston	Without You Erin Salston	2015 NuFantasy	
57	Veronique Perrin	Haute Societe Veronique Perrin	2012 Tropicalia Convention FR	
58	Fabiana Diaz	Fabiana Diaz - Outfit: Shanghai Bound	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	
59	Vanessa Perrin	Adorned Vanessa Perrin	2014 Gloss Convention FR	
60	Rio Pacheco	Rio Pacheco	2012 JEM and the Holograms	NRFB
61	Jade	Soft Focus Jade (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
62	Colette Duranger	Checking In Colette Duranger	2016 Supermodel Convention NuFace	
63	Alysa	Color Clash Alysa	2018 Lovesick Collection The Industry	
64	Fiona Goode	Coven Fiona Goode	American Horror Story	
65	Gavin Grant	Graphic Content Gavin Grant (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
66	Hollis Hughes	The Remix Hollis Hughes (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
67	Janay	Not Your Puppet Janay (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
68	Fabiana Diaz	Fabiana Diaz	2018 Luxe Life Convention CI	OOAK
69	Adaline King	Never Predictable Adaline King	2017 NuFantasy / IFDC	
70	Monroe Jillian	Monroe Jillian	2018 Luxe Life Convention CI	
71	Lark Lawrence	Cherry Bomb Lark Lawrence (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
72	Ellery Eames	Glitterazi Ellery Eames (Miss Behave Style Lab)	2018 Luxe Life Convention The Industry / Style Lab	
73	Veronique Perrin	Fresh and Delightful Veronique Perrin	2016 A Delicate Bloom FR	
74	Liu Liu Ling	Liu Liu Ling (fashion: Just a Second!)	2017 Fashion Fairytale Convention The Industry / Style Lab	
75	Rayna Ahmadi	A Fabulous Life Rayna Ahmadi	2018 Luxe Life Convention NuFace	
76	Eden Blair	Reliable Source Eden Blair	2018 NuFace Misc	
77	Imogen Lennox	Be Daring Imogen	2016 Non-Mainline NuFace	
78	Jacqueline O'Rion	Jacqueline O'Rion	2018 Luxe Life Convention CI	
79	Fabiana Diaz	Fabiana Diaz	2018 Luxe Life Convention CI	
80	Alysa	Alysa	2018 Luxe Life Convention CI	
81	Camira Domina	Camira Domina	2018 Luxe Life Convention CI	
82	Legacy Janay	Legacy Janay	2018 Lovesick Collection The Industry	
83	Zoe Benson	Zoe Benson	American Horror Story	
84	Madison Montgomery	Madison Montgomery	American Horror Story	
85	Vanessa Perrin	Dress Code Vanessa Perrin	2011 Fashion Royalty	
86	Tatyana Alexandrova	Gilded Oligarch Tatyana Alexandrova	2018 Luxe Life convention	
87	Ayumi Nakamura	Evening Siren Ayumi Nakamura	2014 NuFace Gloss Convention	
88	Vanessa Perrin	Fame & Fortune Vanessa Perrin	2017 Fashion Royalty Misc	
89	Elyse Jolie	On The Rise Elise Jolie	2014 Urban Safari FR	
90	Poppy Parker	Looks A Plenty! Poppy Parker (Auburn = zrzka)	2018 Poppy Parker Misc.	
91	Victoire Roux	Lunch At 21 Victoire Roux	2019 East 59th Capsule Collection	NRFB
92	Lilith Blair	Unknown Source Lilith Blair	2018 NuFace Misc	NRFB
93	Veronique Perrin	Breaking The Mold Veronique Perrin	2011 Fashion Royalty	ND
94	Agnes Von Weiss	Love, Life and Lace Agnes Von Weiss	2016 A Delicate Bloom FR	N
95	Evelyn Weaverton	Turquoise Sparkler Evelyn Weaverton	2018 Cocktail Collection East 59th	N
96	Natalia Fatale	Luxuriously Gifted Natalia Fatale	2018 Luxe Life Convention FR	N
97	Noah Faraday	Natural Selection Noah Faraday	2019 The Monarchs Hommes	NRFB
98	Natalia Fatale	Sweet Victory Natalia Fatale	2012 Tropicalia Convention FR	NRFB
99	Aurelia Grey	Winter Shimmer Lady Aurelia Grey	2019 Live From Fashion Week Convention East 59th	N
100	Callum Windsor	Callum Windsor (outfit: Whatta Man)	2016 Supermodel Convention CI	Nude
101	Tatyana Alexandrova	Vivid Paradise Tatyana Alexandrova	2019 Live From Fashion Week Convention FR	
102	Eugenia Perrin	Fashionista Eugenia Perrin-Frost	2016 Supermodel Convention FR	
103	Veronique Perrin	The Sweet Smell of Success Veronique Perrin	2012 Style Directive FR	N
104	Poppy Parker	Split Decision Poppy Parker (Silver)	2018 Poppy Parker Misc.	NRFB
105	Dominique Makeda	Nirvana Dominique Makeda	2018 Counter-Culture NuFace	NRFB
106	Dasha d’Amboise	Infallible Dasha	2012 Tropicalia Convention FR	
107	Laka O'Rion	Laka Orion (Subject H) - Tropicalia Convention	2012 Tropicalia Convention CI	
108	Kyori Sato	Love The One... Kyori Sato	2013 Fashion Royalty	N
109	Eugenia Perrin	Pencil Me In Eugenia Perrin Frost	2011 Fashion Royalty	N
110	Aurelia Grey	Opera on the 5th Lady Aurelia Grey	2019 East 59th Capsule Collection	N
111	Constance Madssen	Mai Tai Swizzle Constance Madssen	2018 Cocktail Collection East 59th	
112	Evelyn Weaverton	All Aboard On The 2nd Evelyn Weaverton	2019 East 59th Capsule Collection	
113	Rayna Ahmadi	Neo-Romantic Rayna Ahmadi	2017 NuFace - Heirloom Collection	N
114	Constance Madssen	Afternoon Intrigue Constance Madsen	2019 East 59th	N
115	Natalia Fatale	Contrasting Proposition Natalia Fatale	2016 A Delicate Bloom FR	N
116	Dania Zarr	Lady In Waiting Dania Zarr	2010 Foundation Collection FR	N
117	Dania Zarr	Always On Her Mind Dania Zarr	2012 Style Directive FR	N
118	Vanessa Perrin	Refinement Vanessa Perrin	2015 Fashion Royalty	NDJ
119	Plum Powers	That's All Plum Powers	2019 Live From Fashion Week Convention The Industry	NRFB
120	Sergio Silva	Man of Mystery Sergio Silva	2019 The Girl from I.N.T.E.G.R.I.T.Y.: Mission Brazil Collection	NRFB
121	Vanessa Perrin	French Kiss Vanessa Perrin	2019 Fashion Royalty Misc.	NRFB
122	Poppy Parker	Gardens of Versailles Poppy Parker	2019 Live From Fashion Week Poppy Parker	NRFB
123	Rayna Ahmadi	Eye Candy Rayna Ahmadi	2018 NuFace Misc	NRFB
124	Elyse Jolie	Spring 2017 Elyse Jolie	2019 Live From Fashion Week Convention FR	NRFB
125	Agnes Von Weiss	Ocean Drive Baroness Agnes Von Weiss	2019 Fashion Royalty Misc.	
126	Victoire Roux	Dramatic Evening Victoire Roux	2019 East 59th	
127	Vanessa Perrin	Reception A Versailles Veronique Perrin	2016 Non-Mainline FR	NRFB
128	Karolin Stone	NYFW Karolin Stone	2019 Live From Fashion Week Convention NF	NRFB
129	Anja Christensen	Captivating Anja Christensen	2013 Fashion Royalty	N
130	Agnes Von Weiss	High Visibility Agnes Von Weiss	2014 Urban Safari FR	N
131	Vanessa Perrin	High Point Vanessa Perrin	2012 Tropicalia Convention FR	ND
132	Tatyana Alexandrova	Perfect Reign Tatyana Alexandrova	2015 Fashion Royalty	W
133	Kyori Sato	Karma Kyori Sato	2018 Sacred Lotus FR	
134	Giselle Diefendorf	Majesty Giselle Diefendorf	2017 Non-Mainline NuFace	N
135	Constance Madssen	Tangier Tangerine Constance Madssen	2018 Cocktail Collection East 59th	N
136	Anja Christensen	Agent 355 Anja Christensen	2016 Non-Mainline FR	
137	Adaline King	Adaline King - Outfit: Passion	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	
138	Natalia Fatale	Ready To Dare Natalia Fatale	2012 Style Directive FR	W
139	Tatyana Alexandrova	Fashionably Ruthless Tatyana Alexandrova	2017 Fashion Fairytale Convention FR	ND
140	Kathy Keene	Blue Serenade Katy Keene	2015 Katy Keene	
141	Eugenia Perrin	Subtle Affluence Eugenia Perrin-Frost	2018 Luxe Life Convention FR	
142	Veronique Perrin	Breathless Veronique Perrin	2013 Premiere Convention FR	
143	Elyse Jolie	Key Pieces Elyse Jolie	2016 A Delicate Bloom FR	
144	Carol Roth	The Entrepreneur Equation Carol Roth	2011 Non-Mainline FR	
145	Elyse Jolie	J'Adore La Fete Elyse Jolie	2015 Non-Mainline FR	ND
146	Kyori Sato	Peak Season Kyori Sato	2012 Tropicalia Convention FR	N
147	Natalia Fatale	Inner Spark Natalia Fatale	2015 Cinematic Convention FR	W
148	Agnes Von Weiss	Feminine Perspective Agnes Von Weiss	2015 Cinematic Convention FR	N
149	Mademoiselle Jolie	Just a Tease Mademoiselle Jolie	2018 Fashion Royalty Boudoir Collection	N
150	Kyori Sato	Nightshade Kyori Sato	2014 Gloss Convention FR	ND
151	Binx Barone	Binx Barone (fashion: Heartache)	2017 Fashion Fairytale Convention The Industry / Style Lab	
152	Zara Wade	Zara Wade - Leading Ladies	2015 Leading Ladies	W-S
153	Evelyn Weaverton	Midnight Glimmer Evelyn Weaverton	2017 Fashion Fairytale Convention East 59th	
154	Mademoiselle Jolie	Kiss You in Paris Mademoiselle Jolie	2016 Supermodel Convention FR	NDB
155	Evelyn Weaverton	The Americano Evelyn Weaverton	2018 Cocktail Collection East 59th	NJB
156	Giselle Diefendorf	Sister Moguls Giselle Diefendorf	2016 Non-Mainline NuFace	NJB
157	Dasha d’Amboise	Purity Dasha	2013 Non-Mainline FR	ND
158	Victoire Roux	Story of my Life Victoire Roux	2013 Premier Convention Victoire Roux	
159	Paolo Marino	Most Influential Paolo Marino	2020 The Monarchs	
160	Aria	Chill Factor Aria	2010 Chill Factor Collection	
161	Milo Montez	Love is Love Milo Montez Wedding Gift Set	2019 The Industry	NRFB
162	Callum Windsor	Callum Windsor - Outfit: Wild One	2015 Cinematic Convention Color Infusion 'Super Stars' Style Lab	
163	Vanessa Perrin	Opulence For The Bold Vanessa Perrin	2018 Luxe Life Convention FR	
164	Korinne Dimas	Red Reign Korinne Dimas	2020 Legendary Convention Style Lab FR Dolls	
165	Anja Christensen	Exotic Interlude Anja Christensen	2020 Legendary Convention Style Lab FR Dolls	
166	Victoire Roux	Pin-Up Allure Victoire Roux	2020 East 59th Basic Editions	NRFB
167	Victoire Roux	Summer Glamour Victoire Roux	2020 East 59th Basic Editions	NRFB
168	Keeki Adaeze	Slay All Day Keeki Adaeze 	2020 Meteor Basic Editions	NRFB
169	Keeki Adaeze	Still Poppin' Keeki Adaeze	2020 Meteor Basic Editions	NRFB
170	Navia Phan	Coming Out Navia Phan	2020 Meteor Le Chic	NC
171	Kathy Keene	Katy Keene, The Odds are Stacked Gloria Grandbuilt	2015 Katy Keene	N (uvolněné vlasy)
172	Victoire Roux	Sparkling New Year Victoire Roux	2020 The East 59th Collection	N
173	Eden Blair	Never Ordinary Eden (reroot)	2015 NuFace Misc	DIY
174	Lilith Blair	Never Ordinary Lilith (reroot)	2015 NuFace Misc	DIY
175	Kathy Keene	Katy Keene, The Odds are Stacked Gloria Grandbuilt (restyle)	2015 Katy Keene	DIY (čína + Zmrzlinka)
176	Constance Madssen	Festive Lights Constance Madssen	2020 The East 59th Collection	
177	Giselle Diefendorf	Energetic Presence Giselle Diefendorff (reroot)	2015 NuFace	DIY
178	Imogen Lennox	Dark Fable Imogen Lennox	2013 IFDC / NU.Fantasy	NRFB
179	Dania Zarr	Such A Gem Dania Zarr	2020 Fashion Royalty Misc.	NRFB
180	Ayumi Nakamura	Charmed Child Ayumi Nakamura	2020 Legendary Convention NF	
181	Poppy Parker	Beach Babe Poppy Parker	2021 Poppy Parker Misc	NRFB
182	Erin Salston	Voltage Erin Salston (diy)	2015 NuFace	DIY
183	Natalia Fatale	Contrasting Proposition Natalia Fatale (reroot)	2016 A Delicate Bloom FR	DIY
184	Vanessa Perrin	Refinement Vanessa Perrin (reroot)	2015 Fashion Royalty	DIY
185	Darla Daley	Girl Talk ﻿Darla Daley (sold with Poppy Parker)	2018 City Sweetheart Poppy Parker	N
186	Imogen Lennox	Fame by Frame Imogen Lennox	2012 Tropicalia Convention NuFace	N
187	Margaret Jolie	Social Standing Mme Margaret Jolie	2020 Legendary Convention Style Lab FR Dolls	N
188	Eugenia Perrin	Point of Departure Eugenia Frost	2011 Jet Set Convention FR	
189	Elyse Jolie	Fine Print Elise Jolie (reroot)	2015 Fashion Royalty	DIY
190	Evelyn Weaverton	Red Hot Evelyn Weaverton	2020 Legendary Convention East 59th	NRFB
191	Giselle Diefendorf	Live, Work, Play Giselle Diefendorf	2012 NuFace	N
192	Lilith Blair	Natural High Lilith Blair	2021 NuFace Misc	NRFB
193	Korinne Dimas	Siren Silhouette Korinne Dimas	2020 Fashion Royalty Maison Paris	NRFB
194	Veronique Perrin	Cover Girl Veronique Perrin	2016 Supermodel Convention FR	
195	Eugenia Perrin	Wicked Narcissism Eugenia Perrin-Frost	2020 Legendary Convention FR	NRFB
196	Dasha d’Amboise	Female Icon Dasha d'Amboise	2019 Fashion Royalty Misc.	
197	Kesenia Valentinova	Hourglass Kesenia Valentinova	2010 Dark Romance Convention FR	
198	Erin Salston	In Control Erin Salston	2020 Legendary Convention NF	N
199	Nadja Rhymes	Fit To Print Nadja Rhymes	2021 NuFace Misc	NRFB
200	Elyse Jolie	Bijou Elyse Jolie	2021 Fashion Royalty Misc	NRFB
201	Poppy Parker	Co-Ed Cutie Poppy Parker	2018 City Sweetheart Poppy Parker	N
202	Violaine Perrin	A Fashionable Legacy Violaine Perrin	2020 NuFace Misc.	
203	Rayna Ahmadi	Wild Feeling Rayna Ahmadi	2020 NuFace Misc.	N
204	Agnes Von Weiss	Malibu Sky Baroness Agnes Von Weiss	2021 Fashion Royalty Misc	NRFB
205	Ginger Gilroy	Beautiful Ginger Gilroy	2021 Obsession Convention Style Lab Poppy Parker	N
206	Vanessa Perrin	Retro Dimensional Vanessa Perrin	2019 RetroFuture FR	N
207	Veronique Perrin	Velvet Rouge Veronique Perrin	2020 Fashion Royalty Misc.	NRFB
208	Astral Eldrich	Astral Eldrich (reroot Snedronningen)	2014 JEM and the Holograms	DIY bílá královna
209	Natalia Fatale	Enamorada Natalia Fatale	2020 Fashion Royalty Maison Paris	NRFB
210	Poppy Parker	Pink Lemonade Poppy Parker	2021 Poppy Parker In Palm Springs	NRFB
211	Veronique Perrin	The Originals Veronique Perrin	2020 Jason Wu 20th Anniversary Collection	
212	Poppy Parker	Resort Ready Poppy Parker	2021 Poppy Parker In Palm Springs	NRFB
213	Vanessa Perrin	Velvet Rouge Vanessa Perrin (Net-a-Porter)	2019 Fashion Royalty Misc.	NRFB
214	Keeki Adaeze	Mischievous Keeki Adaeze	2020 Legendary Convention Meteor	NRFB
215	Zuri Okoty	Dangerous Curves Zuri Okoty	2021 Meteor The Roaring 20's Remix Collection	NRFB
216	Vanessa Perrin	Color Therapy Vanessa Perrin	2008 Premium FR	NRFB
217	Adele Makeda	Sovereign Adele Makeda	2021 Obsession Convention FR	N
218	Poppy Parker	Sugar & Spice Poppy Parker (Spice)	2020 Poppy Parker Misc.	MINT
219	Veronique Perrin	Cover Story Veronique Perrin	2020 Legendary Convention Style Lab FR Dolls	N
220	Karolin Stone	Making An Entrance Karolin Stone	2015 Cinematic Convention NuFace	NRFB
221	Karolin Stone	My Allure Karolin Stone	2020 NuFace Essentials	NRFB
222	Tatyana Alexandrova	Blood Lines Tatiana	2013 IFDC / NU.Fantasy	NRFB
223	Vanessa Perrin	Black-Tie Ball Vanessa Perrin	2015 Non-Mainline FR	NRFB
224	Vanessa Perrin	Runway Right Away Vanessa Perrin	2005 Exotic Fusion Dressed	
225	Annik Vandale	Mademoiselle Annik - Annik Vandale	2022 NuFace Mademoiselle Collection	
226	Astral Eldrich	Astral Eldrich (PinkBlue)	2014 JEM and the Holograms	DIY modro-růžová
227	Cabot Clark	Love is Love Cabot Clark Wedding Gift Set	2019 The Industry	NRFB
228	Rayna Ahmadi	MVP Rayna Ahmadi	2021 Obsession Convention NuFace	N
229	Maeve Rocha	Pink Mist Maeve Rocha	2021 The East 59th La Femme Godiva Collection	NRFB
230	Evelyn Weaverton	Pressed Perfection Red Hot Evelyn Weaverton	2021 The East 59th La Femme Godiva Collection	NRFB
231	Natalia Fatale	Acquired Traits Natalia Fatale	2020 Legendary Convention Style Lab FR Dolls	N
232	Anja Christensen	Scarlett Hex Anja Christensen	2022 NU. Fantasy	NRFB
233	Ingrid Kruger	Mind Games Ingrid 'Minx' Kruger	2022 JEM and the Holograms 35th Anniversary	NRFB
234	Phoebe Ashe	Mind Games Phoebe 'Rapture' Ashe	2022 JEM and the Holograms 35th Anniversary	NRFB
235	Vanessa Perrin	Monaco Royale Vanessa Perrin	2011 Non-Mainline FR	N
236	Adele Makeda	Divining Beauty Adele Makeda	2021 NU. Fantasy	N
237	Della Roux	Frosted Passion Della Roux	2021 The East 59th La Femme Godiva Collection	
238	Dominique Makeda	Adrenaline Rush Dominique Makeda	2021 Obsession Convention NuFace	N
239	Agnes Von Weiss	Legendary Status Agnes Von Weiss	2020 Fashion Royalty Misc.	
240	Maeve Rocha	Pink Mist Maeve Rocha	2021 The East 59th La Femme Godiva Collection	N
241	Sooki	13 Days Of Halloween Sooki	2022 The Industry Misc.	NRFB
242	Cabot Clark	Bowling Date Cabot Clark	2022 Poppy Parker Loves Mystery Date	NRFB
243	Amirah Majeed	24K Shine Amirah Majeed	2021 Meteor The Roaring 20's Remix Collection	NRFB
244	Korinne Dimas	Elements of Enchantment Korinne Dimas	2022 NuFantasy	NRFB
245	Agnes Von Weiss	Up With A Twist Agnes Von Weiss	2022 Fashion Royalty Misc	NRFB
246	Poppy Parker	Bowling Date Poppy Parker	2022 Poppy Parker Loves Mystery	NRFB
247	Poppy Parker	Undercover Angel Poppy Parker	2020 Poppy Parker Misc.	N
248	Vanessa Perrin	Summer in Taormina Vanessa Perrin	2022 Integrity Toys X Magia 2000	NRFB
249	Della Roux	Legacy: Burnt Champagne Part 1 Della Roux	2022 Stay Tuned East 59th	NRFB
250	Elyse Jolie	Passion Week Elyse Jolie (reroot Morticia Addams)	2018 Fashion Royalty Misc	DIY: reroot YFG28, body Porcelain NuFace
251	Poppy Parker	Ultra Violet Poppy Parker	2022 Poppy Parker Misc.	
252	Agnes Von Weiss	Festive Decadence Agnes Von Weiss	2009 Fashion Royalty Non-Mainline	N
253	Fan Xi	Fan Xi - Crushin' It!	2016 In The Mix	DIY: zvlášť hlavička (Čína, málo navlasená) i tělo (amisa)
254	Isabella Alves	Dawn In Bloom Isabella Alves	2022 Close-up Doll The Fashion Royalty Collection	NRFB
255	Luchia Zadra	Dusk In Bloom Luchia Zadra	2022 Close-up Doll The Fashion Royalty Collection	NRFB
256	Victoire Roux	Legacy: Burnt Champagne Part 2 Victoire Roux	2022 Stay Tuned East 59th	NRFB
257	Giselle Diefendorf	Primary Subject Giselle Diefendorf	2022 NuFace Misc.	NRFB
258	Amirah Majeed	Pose Like An Egyptian Amirah Majeed	2023 Meteor Misc., IT Direct Basic Doll	NRFB
259	Erin Salston	Night Out Erin Salston	2023 NuFace Misc., IT Direct Basic Doll	NRFB
260	Natalia Fatale	Bombshell Beach Natalia Fatale	2023 Fashion Royalty Misc., IT Direct Basic Doll	NRFB
261	Poppy Parker	Island Time Poppy Parker	2023 Poppy Parker Misc., IT Direct Basic Doll	NRFB
262	Poppy Parker	Desert Dazzler Poppy Parker	2021 Poppy Parker In Palm Springs	NRFB
263	Amirah Majeed	Holding Court Amirah Majeed	2023 Meteor The Rococo Collection	NRFB
264	Eden Blair	Earth Angel Eden Blair	2022 NuFace Misc.	Nude
265	Anja Christensen	Provocatrice Anja Christensen	2010 Dark Romance Convention FR	Mint
266	Aymeline	Spring 2020 Aymeline	2022 Stay Tuned Fashion Royalty	NRFB
267	Aymeline	Winter 2021 Aymeline	2022 Stay Tuned Fashion Royalty	NRFB
268	Giselle Diefendorf	Hello Lover Giselle Diefendorf	2022 Stay Tuned NuFace	NRFB
269	Taliyah Harper	Head Over Heels Taliyah Harper	2023 Meteor The Rococo Collection	N
270	Poppy Parker	Mayhem in Monte Carlo Poppy Parker	2014 The Girl from I.N.T.E.G.R.I.T.Y. - Poppy Parker	Kompletní
271	Dania Zarr	Careless Love Dania Zarr	2010 Dazzle Collection FR	Nude
272	Kathy Keene	Everything's Keene Katy Keene	2015 Katy Keene	Nude
273	Romain Perrin	Sound Individual Romain Perrin	2021 The Monarchs Misc.	NRFB
274	Alejandra Luna	Billion Dollar Beauty Alejandra Luna	2023 NuFace Misc.	
275	Vanessa Perrin	Graceful Reign Vanessa Perrin	2021 Fashion Royalty Misc	N
276	Lilith Blair	The NU Classic Lilith Blair	2021 NuFace Misc	N
277	Tajinder Chowdhury	Metropolitan Adventurer Tajinder Chowdhury	2022 The Monarchs Misc.	NRFB
278	Victoire Roux	Love Knot Of Gold Victoire Roux	2023 East 59th The Adornments Collection	NRFB
279	Tulabelle	Trending Tulabelle True	2022 True Misc.	N
280	Alysa	Alyssa Bride Jason Wu Collection Spring 2020	2022 Fashion Royalty Misc.	NRFB
281	Poppy Parker	Belle Mariee Poppy Parker	2021 Obsession Convention Poppy Parker	N
282	Eugenia Perrin	Summer Rose Eugenia Perrin-Frost	2023 Fashion Royalty Misc.	NRFB
283	Zuri Okoty	Behind The Curtain Zuri Okoty	2023 Curated Pop Up Event Meteor	NRFB
284	Miles Morgan	Miles Morgan	2015 Cinematic Convention Color Infusion	N
285	Constance Madssen	Twilight In Blue Topaz Constance Madssen	2023 East 59th The Adornments Collection	N
286	Elyse Jolie	Glamour Coated Elyse Jolie	2021 Fashion Royalty Misc	Částečně
287	Zuri Okoty	My Hair Fair Zuri Okoty (paruková plus jedna paruka)	2021 Meteor Misc. 	N
288	Victoire Roux	Aloha Waikiki Victoire Roux	2022 East 59th Basic Editions	NRFB
289	Nadja Rhymes	Print It Pink Nadja Rhymes	2023 NuFace Moments	NRFB
290	Ayumi Nakamura	Naturally Cool Ayumi Nakamura	2023 NuFace Moments	NRFB
291	Constance Madssen	Twilight In Blue Topaz Constance Madssen	2023 East 59th The Adornments Collection 	NRFB
292	Coralynn Kwan	Dream In Aquamarine, Coralynn “Cora” Kwan	2023 East 59th The Adornments Collection 	Nude
293	Della Roux	Deepest Desire Della Roux	2023 7 Sins Integrity Toys Online Event East 59th	NRFB
294	Poppy Parker	Perfectly Palm Springs (Afro) Poppy Parker Gift Set, var.B	2022 Poppy Parker in Palm Springs	Oblečená
295	Aurelia Grey	Enchanting In Amethyst Aurelia Grey	2023 East 59th The Adornments Collection 	N
296	Tulabelle	Trending Tulabelle True (reroot)	2022 True Misc	Nude pro ReRoot
297	Elyse Jolie	On The Rise Elise Jolie	2014 Urban Safari FR	ND
298	Evelyn Weaverton	Traveling In Style Evelyn Weaverton	2023 The Weekend In The Poconos Collection East 59th	N
299	Amirah Majeed	Breaking Dawn Amirah Majeed	2020 Meteor The Launch	N
300	Dania Zarr	Delightful Indulgence Dania Zarr	2023 7 Sins Integrity Toys Online Event Fashion Royalty	NRFB
301	Adele Makeda	Pink Glam Adele Makeda	2023 Fashion Royalty Moments	NRFB
302	Victoire Roux	Legacy: Burnt Champagne Part 2 Victoire Roux (restyle)	2022 Stay Tuned East 59th	NSB
303	Elyse Jolie	Seduisante Elyse Jolie	2017 Fashion Royalty - La Femme	NS
304	Chip Farnsworth	Formal Dance Date Chip Farnsworth III	2022 Poppy Parker Loves Mystery Date	N
305	Binna Park	Modern Renaissance (Variation) Binna Park	2023 Curated Pop Up Event FR 	NRFB
306	Vanessa Perrin	Aerodynamic Vanessa	2009 Future.Perfect Collection Basic FR	ND
307	Dania Zarr	Such A Gem Dania Zarr (reroot)	2020 Fashion Royalty Misc.	NSJ
308	Rio Pacheco	Rio Pacheco (druhý)	2012 JEM and the Holograms	Náhradní oblečení
309	Auden Adler	Mr. Brightside Auden Adler	2022 The TRUE™ Collection	NRFB
310	Monroe Jillian	Monroe Jillian - Fashion Fairytale Convention 	2017 Fashion Fairytale Convention Colour Infusion	ND
311	Nadja Rhymes	Get Ready With Me Nadja Rhymes	2023 NuFace Misc.	NRFB
312	Tenzin Dahkling	Blood Moon Tenzin Dahkling	2023 and 2024 Integrity Toys X Cold Carbon NuFantasy	NRFB
313	Dasha d’Amboise	Infallible Dasha (reroot)	2012 Tropicalia Convention FR	N
314	Poppy Parker	Angel Eyes Poppy Parker	2024 Fashion Royalty Club	NRFB
315	Dania Zarr	Dania Zarr Holiday Spot	2024 Fashion Royalty Club	NRFB
316	Imogen Lennox	Charmed Life Imogen Lennox	2017 NuFace - Heirloom Collection	Oblečená bez sukně
317	Victoire Roux	The Winds Of Winter, Victoire Roux	2024 East 59th The Four Seasons Collection	NSC
318	Maeve Rocha	Seeing The Sights Maeve Rocha Dressed Doll	2023 The Weekend In The Poconos Collection East 59th	NSC
319	Navia Phan	Enigmatic Reinvention Navia Phan	2021 Meteor The Roaring 20's Remix Collection	Nude
320	Violaine Perrin	Sirene Violaine Perrin	2023 NuFantasy Misc.	NRFB
321	Zuri Okoty	My Hair Fair Zuri Okoty (paruková plus jedna paruka)	2021 Meteor Misc. 	NSC
322	Constance Madssen	Springtime Find Constance Madssen	2024 East 59th: The Four Seasons Collection	NSC
323	Constance Madssen	Springtime Find Constance Madssen (reroot)	2024 East 59th: The Four Seasons Collection	NSC
324	Victoire Roux	Love Knot Of Gold Victoire Roux	2023 East 59th The Adornments Collection	NSC
325	Vanessa Perrin	In Bloom Vanessa Perrin	2007 Exclusive	N
326	Poppy Parker	Turning Green Poppy Parker	2023 7 Sins Integrity Toys Online Event Poppy Parker	NSCJ
327	Eugenia Perrin	City Prowl Eugenia Perrin Frost	2014 Urban Safari FR	N
328	Laka Orion	Laka Orion (Subject G)	2012 Tropicalia Convention CI	N
329	Jordan Duval	Coquette Jordan Duval	2017 Fashion Royalty - La Femme	N
330	Korinne Dimas	Vaughn's Workshop Korinne (Blonde)	2011 Jet Set Convention FR	ND
331	Giselle Diefendorf	Feeling Wild Giselle Diefendorf	2017 NuFantasy / IFDC	N
332	Constance Madssen	Agent Sugar North Constance Madssen	2024 Stilettos Out East 59th	N
333	Poppy Parker	Apres-Ski Asset Poppy Parker	2024 Stilettos Out	NRFB
334	Vanessa Perrin	Lethal Rose Vanessa Perrin	2024 Stilettos Out	N
335	Tatyana Alexandrova	Gilded Oligarch Tatyana Alexandrova	2018 Luxe Life convention	Hlavička, rozčesaná + tělo
336	Victoire Roux	Divine Evening Victoire Roux	2018 Luxe Life Convention East 59th	N
337	Elyse Jolie	Ritorno Alla Vita Elyse Jolie	2024 Stilettos Out Fashion Royalty	NRFB
338	Tajinder Chowdhury	Desert Winds Tajinder Chowdhury	2020 The Monarchs	NRFB
339	Imogen Lennox	Baby Blue Imogen Lennox Mini Gift Set	2024 NuFace Misc	NRFB
340	Navia Phan	Strut It Out Navia Phan	2024 Integrity Toys X Jason Kramer Designs	NRFB
341	Adele Makeda	Glamour To Go Adele Makeda	2007 Cult Couture Basic	N
342	Tulabelle	Adored Tulabelle True	2024 TRUE Prismatica Collection	NRFB
343	Lark Lawrence	Bubble Up Lark Lawrence	2024 TRUE Prismatica Collection	NRFB
344	Taliyah Harper	Step & Repeat Taliyah Harper	2024 Integrity Toys X Jason Kramer Designs	N
";
            return text;
        }
    }
}
