using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Components;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Hlavní control Dílenské tabule: obsahuje <see cref="GToolBar"/> + <see cref="SchedulerPanel"/>.
    /// </summary>
    public class MainControl : InteractiveControl
    {
        #region Konstruktor, inicializace, privátní proměnné grafiky
        /// <summary>
        /// Konstruktor základní
        /// </summary>
        public MainControl()
            : this(null)
        { }
        /// <summary>
        /// Konstruktor s předáním reference na datový objekt
        /// </summary>
        /// <param name="mainData"></param>
        public MainControl(MainData mainData)
        {
            this._MainData = mainData;
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", "LayoutInit", ""))
                this._LayoutInit();

            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", "ToolBarInit", ""))
                this._ToolBarInit();

            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", "SchedulerPanelInit", ""))
                this._SchedulerPanelInit();

            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", "CalculateLayout", ""))
                this.CalculateLayout();
        }
        /// <summary>
        /// Reference na hlavní datový objekt
        /// </summary>
        public MainData MainData { get { return this._MainData; } }
        private MainData _MainData;
        /// <summary>
        /// Po změně velikosti controlu přepočítá souřadnice vnitřních prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            try
            {   // Tuhle metodu volá Windows podle potřeby, ...
                this.CalculateLayout();          // Nejdříve rozmístím vizuální prvky...
                base.OnSizeChanged(e);           //  a tady se provádí Draw
            }
            catch (Exception exc)
            {   //  ... a jakákoli chyba by zbořila celou aplikaci:
                Application.App.ShowError(exc);
            }
        }
        #endregion
        #region Po prvním vykreslení
        /// <summary>
        /// Metoda je volána POUZE PO PRVNÍM vykreslení obsahu
        /// </summary>
        protected override void OnFirstDrawAfter()
        {
            base.OnFirstDrawAfter();
            GuiDialog initialDialog = this.InitialDialog;
            if (initialDialog != null)
                this.ShowDialog(initialDialog);
            this.InitialDialog = null;
        }
        /// <summary>
        /// Deklarace výchozího dialogu, načítá se z <see cref="GuiData.InitialDialog"/>, zpracovává se v <see cref="OnFirstDrawAfter()"/>.
        /// </summary>
        public GuiDialog InitialDialog
        {
            get { return this._InitialDialog; }
            set
            {
                this._InitialDialog = null;
                if (value != null)
                {
                    this._InitialDialog = new GuiDialog()
                    {
                        Title = value.Title,
                        Icon = value.Icon,
                        Message = value.Message,
                        Buttons = value.Buttons
                    };
                }
            }
        }

        private GuiDialog _InitialDialog;
        #endregion
        #region Ukládání / Načítání layoutu a dalších hodnot z konfigurace (persistence stavu)
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        protected SchedulerConfig Config { get { return this._MainData?.Config; } }
        /// <summary>
        /// Inicializace objektu pro Layout
        /// </summary>
        private void _LayoutInit()
        {
            SchedulerConfig config = this.Config;
            MainControlLayout layout = (config != null ? config.UserConfigSearch<MainControlLayout>().FirstOrDefault() : null);
            if (layout == null)
            {   // Pokud nemáme MainData, nebo nemáme Config, nebo v něm dosud není Layout, vytvoříme si Layout nový:
                layout = new MainControlLayout();
                // Pokud máme Config, pak Layout do něj přidáme:
                if (config != null)
                    config.UserConfig.Add(layout);
            }
            this._ControlLayout = layout;
        }
        /// <summary>
        /// Controller layoutu
        /// </summary>
        private MainControlLayout _ControlLayout;
        /// <summary>
        /// Zajistí uložení konfigurace. Ne hned, provede se za 30 sekund od prvního požadavku.
        /// </summary>
        protected void ConfigSaveDeffered()
        {
            SchedulerConfig config = this.Config;
            if (config != null)
                config.Save(TimeSpan.FromSeconds(30d));
        }
        /// <summary>
        /// Přepočítá souřadnice vnitřních prvků v instanci <see cref="MainControl"/>
        /// </summary>
        protected void CalculateLayout()
        {
            Size size = this.ClientSize;
            if (!size.IsVisible()) return;
            int y = 0;
            if (this._ToolBar.Is.Visible)
            {
                int th = this._ToolBar.Bounds.Height;
                this._ToolBar.Bounds = new Rectangle(y, 0, size.Width, th);
                y = this._ToolBar.Bounds.Bottom;
            }
            this._TabContainer.Bounds = new Rectangle(0, y, size.Width, size.Height - y);
            this.Refresh();
        }
        #endregion
        #region Public rozhraní: vkládání tabulek a dalších dat, synchronizační element časové osy
        /// <summary>
        /// Metoda přidá jednu stránku s daty podle dat dodaných v parametru.
        /// Vrací komplexní objekt obsahující reference na veškeré vytvořené instance dané stránky.
        /// </summary>
        /// <param name="guiPage"></param>
        public MainDataPanel AddPage(GuiPage guiPage)
        {
            return this._SchedulerPanelAdd(guiPage);
        }
        /// <summary>
        /// Metoda smaže všechny stránky s daty
        /// </summary>
        public void ClearPages()
        {
            this._TabContainer.ClearItems();
        }
        /// <summary>
        /// Obsahuje data <see cref="GuiPage"/> aktuální viditelné stránky s daty.
        /// </summary>
        public GuiPage ActiveGuiPage { get { return this.ActiveDataPanel?.GuiPage; } }
        /// <summary>
        /// Data aktivní stránky. Může být null.
        /// </summary>
        protected MainDataPanel ActiveDataPanel { get { return (this._TabContainer.ActivePage?.UserData as MainDataPanel); } }
        /// <summary>
        /// Synchronizační element časové osy
        /// </summary>
        public ValueTimeRangeSynchronizer SynchronizedTime
        {
            get { if (this._SynchronizedTime == null) this._SynchronizedTime = new ValueTimeRangeSynchronizer(); return this._SynchronizedTime; }
            set { this._SynchronizedTime = value; }
        }
        private ValueTimeRangeSynchronizer _SynchronizedTime;
        #endregion
        #region ToolBar
        /// <summary>
        /// Bude zobrazován ToolBar?
        /// </summary>
        public bool ToolBarVisible { get { return this._ToolBar.Is.Visible; } set { this._ToolBar.Is.Visible = value; this.CalculateLayout(); } }
        /// <summary>
        /// Přidá grupu do toolbaru
        /// </summary>
        /// <param name="group"></param>
        public void AddToolBarGroup(FunctionGlobalGroup group)
        {
            this._ToolBar.AddGroup(group);
        }
        /// <summary>
        /// Přidá grupy do toolbaru
        /// </summary>
        /// <param name="groups"></param>
        public void AddToolBarGroups(IEnumerable<FunctionGlobalGroup> groups)
        {
            this._ToolBar.AddGroups(groups);
        }
        /// <summary>
        /// Aktuální živý stav ToolBaru, pro persistenci do příštího spuštění.
        /// Obsahuje názvy prvků Toolbaru a jejich persistované hodnoty.
        /// Lze číst i setovat. Setování vyvolá aplikační logiku daných prvků.
        /// </summary>
        public string ToolBarCurrentStatus { get { return this._ToolBar.CurrentStatus; } set { this._ToolBar.CurrentStatus = value; } }
        /// <summary>
        /// Souhrn všech grup v toolbaru
        /// </summary>
        public FunctionGlobalGroup[] ToolBarFunctionGroups { get { return this._ToolBar.FunctionGroups; } }
        /// <summary>
        /// Souhrn všech prvků ve všech grupách toolbaru
        /// </summary>
        public FunctionGlobalItem[] ToolBarFunctionItems { get { return this._ToolBar.FunctionItems; } }
        /// <summary>
        /// Souhrn všech grafických grup v toolbaru (vizuální objekty)
        /// </summary>
        internal GToolBarGroup[] ToolBarGFunctionGroups { get { return this._ToolBar.GFunctionGroups; } }
        /// <summary>
        /// Souhrn všech grafických prvků v toolbaru (vizuální objekty) = tlačítka, labely, images...
        /// </summary>
        internal GToolBarItem[] ToolBarGFunctionItems { get { return this._ToolBar.GFunctionItems; } }
        /// <summary>
        /// Vymaže všechny prvky Toolbaru
        /// </summary>
        public void ClearToolBar()
        {
            this._ToolBar.ClearToolBar();
        }
        /// <summary>
        /// Provede řízený refresh Toolbaru
        /// </summary>
        /// <param name="refreshMode">Co je třeba refreshovat</param>
        public void RefreshToolBar(GToolBarRefreshMode refreshMode)
        {
            this._ToolBar.Refresh(refreshMode);
        }
        /// <summary>
        /// Inicializace toolbaru
        /// </summary>
        private void _ToolBarInit()
        {
            this._ToolBar = new GToolBar() { Bounds = new Rectangle(0, 0, 1024, 64) };
            this._ToolBar.ToolbarSize = this._ControlLayout.ToolbarSize;
            this._ToolBar.ToolbarSizeChanged += _ToolBarSizeChanged;
            this.AddItem(this._ToolBar);
            this._ToolBar.ItemCheckedChange += _ToolBar_ItemSelectedChange;
            this._ToolBar.ItemClicked += _ToolBar_ItemClicked;
            this._ToolBar.CurrentStatusChanged += _ToolBar_CurrentStatusChanged;
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek (<see cref="GToolBar"/>) po změně IsSelected na některém jeho prvku.
        /// Úkolem je vyvolat event <see cref="MainControl.ToolBarItemSelectedChange"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBar_ItemSelectedChange(object sender, FunctionItemEventArgs args)
        {
            if (this.ToolBarItemSelectedChange != null)
                this.ToolBarItemSelectedChange(this, args);
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek (<see cref="GToolBar"/>) po kliknutí na některý z jeho prvků.
        /// Úkolem je vyvolat event <see cref="MainControl.ToolBarItemClicked"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBar_ItemClicked(object sender, FunctionItemEventArgs args)
        {
            if (this.ToolBarItemClicked != null)
                this.ToolBarItemClicked(this, args);
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek (<see cref="GToolBar"/>) po změně stavu některého z jeho prvků, která má být persistována.
        /// Úkolem je vyvolat event <see cref="MainControl.ToolBarItemClicked"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBar_CurrentStatusChanged(object sender, GPropertyEventArgs<string> args)
        {
            if (this.ToolBarStatusChanged != null)
                this.ToolBarStatusChanged(this, args);
        }
        /// <summary>
        /// Událost vyvolaná po změně IsSelected na určitém prvku ToolBaru
        /// </summary>
        public event FunctionItemEventHandler ToolBarItemSelectedChange;
        /// <summary>
        /// Událost vyvolaná po kliknutí na určitý prvek ToolBaru
        /// </summary>
        public event FunctionItemEventHandler ToolBarItemClicked;
        /// <summary>
        /// Událost vyvolaná po změně persistovaného stavu ToolBaru
        /// </summary>
        public event GPropertyEventHandler<string> ToolBarStatusChanged;
        /// <summary>
        /// Po změně velikosti toolbaru přepočítá souřadnice panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolBarSizeChanged(object sender, GPropertyChangeArgs<ComponentSize> e)
        {
            this._ControlLayout.ToolbarSize = e.NewValue;
            this.ConfigSaveDeffered();
            this.CalculateLayout();
        }
        /// <summary>
        /// Instance toolbaru
        /// </summary>
        private GToolBar _ToolBar;
        #endregion
        #region Jednotlivé panely SchedulerPanel + TabContainer
        /// <summary>
        /// Inicializace dat panelů
        /// </summary>
        private void _SchedulerPanelInit()
        {
            this._TabContainer = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Top, TabHeaderMode = ShowTabHeaderMode.Default };
            this._TabContainer.ActivePageChanged += _TabContainerActivePageChanged;
            this.AddItem(this._TabContainer);

            this._DataPanelsList = new List<MainDataPanel>();
        }
        /// <summary>
        /// Metoda přidá jednu stránku s daty podle dat dodaných v parametru
        /// </summary>
        /// <param name="guiPage"></param>
        private MainDataPanel _SchedulerPanelAdd(GuiPage guiPage)
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", "SchedulerPanelAdd", ""))
            {
                int tabPageIndex = this._TabContainer.TabCount;
                SchedulerPanel schedulerPanel = new SchedulerPanel(this, guiPage);
                GTabPage tabPage = this._TabContainer.AddTabItem(schedulerPanel, guiPage.Title, toolTip: guiPage.ToolTip, image: null);
                MainDataPanel tspInfo = new MainDataPanel(guiPage, tabPageIndex, tabPage, schedulerPanel);
                this._DataPanelsList.Add(tspInfo);
                tabPage.UserData = tspInfo;
                schedulerPanel.LoadData();
                return tspInfo;
            }
        }
        /// <summary>
        /// Soupis všech stránek s daty, zobrazenými v GUI. Typický bývá jedna.
        /// Stránky (instance <see cref="MainDataPanel"/>) obsahují referenci na vstupní data <see cref="MainDataPanel.GuiPage"/>,
        /// refeenci na vizuální control Scheduleru <see cref="MainDataPanel.SchedulerPanel"/>
        /// a refeenci na záložku stránky <see cref="MainDataPanel.GTabPage"/>.
        /// </summary>
        public MainDataPanel[] DataPanels { get { return this._DataPanelsList.ToArray(); } }
        /// <summary>
        /// Instance všech Scheduler panelů a na něj napojených dat.
        /// </summary>
        private List<MainDataPanel> _DataPanelsList;
        /// <summary>
        /// Po změně záložky, která reprezentuje komplexní GUI datového zdroje
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabContainerActivePageChanged(object sender, GPropertyChangeArgs<GTabPage> e)
        {

        }
        /// <summary>
        /// Záložky s daty jednotlivých datových zdrojů
        /// </summary>
        private GTabContainer _TabContainer;
        /// <summary>
        /// Aktuálně zobrazená stránka s daty.
        /// Obsahue (vrací) instanci <see cref="MainDataPanel"/> ze seznamu <see cref="_DataPanelsList"/>, která se týká té stránky <see cref="GTabPage"/>, 
        /// která je aktivní v <see cref="_TabContainer"/>.
        /// </summary>
        protected MainDataPanel SchedulerTabPanelCurrent
        {
            get
            {
                if (this._TabContainer == null) return null;
                GTabPage activePage = this._TabContainer.ActivePage;
                if (activePage == null) return null;
                return this._DataPanelsList.FirstOrDefault(p => Object.ReferenceEquals(p.GTabPage, activePage));
            }
        }
        /// <summary>
        /// true pokud v <see cref="SchedulerPanelCurrent"/> je vybraný panel, false pokud není.
        /// </summary>
        protected bool SchedulerPanelExists { get { return (this.SchedulerTabPanelCurrent != null); } }
        /// <summary>
        /// Aktuálně zobrazovaný Scheduler panel
        /// </summary>
        protected SchedulerPanel SchedulerPanelCurrent { get { MainDataPanel tsp = this.SchedulerTabPanelCurrent;  return (tsp != null ? tsp.SchedulerPanel : null); } }
        #endregion
        #region Dialogy
        /// <summary>
        /// Zobrazí daný dialog a vrátí odpověď.
        /// </summary>
        /// <param name="dialog">Data pro dialog</param>
        /// <returns></returns>
        public GuiDialogButtons ShowDialog(GuiDialog dialog)
        {
            if (dialog == null || dialog.IsEmpty) return GuiDialogButtons.None;
            return this.ShowDialog(dialog.Message, dialog.Title, dialog.Buttons, dialog.Icon);
        }
        /// <summary>
        /// Zobrazí daný dialog a vrátí odpověď.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="guiButtons"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public GuiDialogButtons ShowDialog(string message, string title = null, GuiDialogButtons guiButtons = GuiDialogButtons.None, GuiImage icon = null)
        {
            if (String.IsNullOrEmpty(message)) return GuiDialogButtons.None;
            if (this.InvokeRequired)
                return (GuiDialogButtons)this.Invoke(new Func<string, string, GuiDialogButtons, GuiImage, GuiDialogButtons>(_ShowDialogGUI), message, title, guiButtons, icon);
            else
                return this._ShowDialogGUI(message, title, guiButtons, icon);
        }
        /// <summary>
        /// Zobrazí dialog, vrátí volbu uživatele
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="guiButtons"></param>
        /// <param name="guiIcon"></param>
        /// <returns></returns>
        private GuiDialogButtons _ShowDialogGUI(string message, string title, GuiDialogButtons guiButtons, GuiImage guiIcon)
        {
            return WinFormDialog.ShowDialog(this.FindForm(), message, title, guiButtons, guiIcon);
        }
        /// <summary>
        /// Metoda zajistí, že daná akce bude vyvolaná v GUI threadu, asynchronně, nevrací result.
        /// </summary>
        /// <param name="action"></param>
        public void RunInGUI(Action action)
        {
            if (action == null) return;
            if (this.InvokeRequired)
                this.BeginInvoke(action);
            else
                action();
        }
        /// <summary>
        /// Reference na MainForm
        /// </summary>
        public Form MainForm { get { return this.FindForm(); } }
        #endregion
    }
    #region MainControlLayout : controller pro Layout hlavního controlu MainControl
    /// <summary>
    /// MainControlLayout : controller pro Layout hlavního controlu <see cref="MainControl"/>,
    /// a dále slouží k jeho ukládání/načítání do/z Configu a k jeho reaktivaci.
    /// </summary>
    public class MainControlLayout
    {
        #region Public data persistovaná
        /// <summary>
        /// Velikost Toolbaru
        /// </summary>
        public ComponentSize ToolbarSize { get { return this._ToolbarSize; } set { this._ToolbarSize = value; } } private ComponentSize _ToolbarSize = ComponentSize.Medium;
        #endregion
    }
    #endregion
}
