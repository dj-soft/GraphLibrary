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
    public class MainControl : GInteractiveControl
    {
        #region Konstruktor, inicializace, privátní proměnné grafiky
        /// <summary>
        /// Konstruktor s předáním reference na datový objekt
        /// </summary>
        /// <param name="mainData"></param>
        public MainControl(MainData mainData)
            : this()
        {
            this._MainData = mainData;
        }
        /// <summary>
        /// Konstruktor základní
        /// </summary>
        public MainControl()
        {
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
            base.OnSizeChanged(e);
            this.CalculateLayout();
        }
        /// <summary>
        /// Přepočítá souřadnice vnitřních prvků v instanci <see cref="MainControl"/>
        /// </summary>
        protected void CalculateLayout()
        {
            Size size = this.ClientSize;
            int y = 0;
            if (this._ToolBar.Is.Visible)
            {
                int th = this._ToolBar.Bounds.Height;
                this._ToolBar.Bounds = new Rectangle(y, 0, size.Width, th);
                y = this._ToolBar.Bounds.Bottom + 1;
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
        public SchedulerPanelInfo AddPage(GuiPage guiPage)
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
            this._ToolBar.ToolbarSizeChanged += _ToolBarSizeChanged;
            this.AddItem(this._ToolBar);
            this._ToolBar.ItemCheckedChange += _ToolBar_ItemSelectedChange;
            this._ToolBar.ItemClicked += _ToolBar_ItemClicked;
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek (<see cref="GToolBar"/>) po změně IsSelected na některém jeho prvku,
        /// úkolem je vyvolat event <see cref="MainControl.ToolBarItemSelectedChange"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBar_ItemSelectedChange(object sender, FunctionItemEventArgs args)
        {
            if (this.ToolBarItemSelectedChange != null)
                this.ToolBarItemSelectedChange(this, args);
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek (<see cref="GToolBar"/>) po kliknutí na některý z jeho prvků,
        /// úkolem je vyvolat event <see cref="MainControl.ToolBarItemClicked"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBar_ItemClicked(object sender, FunctionItemEventArgs args)
        {
            if (this.ToolBarItemClicked != null)
                this.ToolBarItemClicked(this, args);
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
        /// Po změně velikosti toolbaru přepočítá souřadnice panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolBarSizeChanged(object sender, GPropertyChangeArgs<ComponentSize> e)
        {
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

            this._SchedulerPanelList = new List<SchedulerPanelInfo>();
        }
        /// <summary>
        /// Metoda přidá jednu stránku s daty podle dat dodaných v parametru
        /// </summary>
        /// <param name="guiPage"></param>
        private SchedulerPanelInfo _SchedulerPanelAdd(GuiPage guiPage)
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", "SchedulerPanelAdd", ""))
            {
                int tabPageIndex = this._TabContainer.TabCount;
                SchedulerPanel schedulerPanel = new SchedulerPanel(this, guiPage);
                GTabPage tabPage = this._TabContainer.AddTabItem(schedulerPanel, guiPage.Title, toolTip: guiPage.ToolTip, image: null);
                SchedulerPanelInfo tspInfo = new SchedulerPanelInfo(guiPage, tabPageIndex, tabPage, schedulerPanel);
                this._SchedulerPanelList.Add(tspInfo);
                return tspInfo;
            }
        }
        /// <summary>
        /// Soupis všech stránek s daty, zobrazenými v GUI. Typický bývá jedna.
        /// Stránky (instance <see cref="SchedulerPanelInfo"/>) obsahují referenci na vstupní data <see cref="SchedulerPanelInfo.GuiPage"/>,
        /// refeenci na vizuální control Scheduleru <see cref="SchedulerPanelInfo.SchedulerPanel"/>
        /// a refeenci na záložku stránky <see cref="SchedulerPanelInfo.GTabPage"/>.
        /// </summary>
        public IEnumerable<SchedulerPanelInfo> SchedulerPanels { get { return this._SchedulerPanelList; } }
        /// <summary>
        /// Instance všech Scheduler panelů a na něj napojených dat.
        /// </summary>
        private List<SchedulerPanelInfo> _SchedulerPanelList;
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
        /// Obsahue (vrací) instanci <see cref="SchedulerPanelInfo"/> ze seznamu <see cref="_SchedulerPanelList"/>, která se týká té stránky <see cref="GTabPage"/>, 
        /// která je aktivní v <see cref="_TabContainer"/>.
        /// </summary>
        protected SchedulerPanelInfo SchedulerTabPanelCurrent
        {
            get
            {
                if (this._TabContainer == null) return null;
                GTabPage activePage = this._TabContainer.ActivePage;
                if (activePage == null) return null;
                return this._SchedulerPanelList.FirstOrDefault(p => Object.ReferenceEquals(p.GTabPage, activePage));
            }
        }
        /// <summary>
        /// true pokud v <see cref="SchedulerPanelCurrent"/> je vybraný panel, false pokud není.
        /// </summary>
        protected bool SchedulerPanelExists { get { return (this.SchedulerTabPanelCurrent != null); } }
        /// <summary>
        /// Aktuálně zobrazovaný Scheduler panel
        /// </summary>
        protected SchedulerPanel SchedulerPanelCurrent { get { SchedulerPanelInfo tsp = this.SchedulerTabPanelCurrent;  return (tsp != null ? tsp.SchedulerPanel : null); } }
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
    #region class SchedulerPanelInfo - třída obsaující data o jednom panelu.
    /// <summary>
    /// SchedulerPanelInfo - třída obsaující data o jednom panelu.
    /// Obsahuje referenci na vstupní data <see cref="GuiPage"/>, referenci na záložku <see cref="GTabPage"/> 
    /// i referenci na vlastní vizuální control <see cref="SchedulerPanel"/>.
    /// </summary>
    public class SchedulerPanelInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="guiPage"></param>
        /// <param name="tabPageIndex"></param>
        /// <param name="gTabPage"></param>
        /// <param name="schedulerPanel"></param>
        public SchedulerPanelInfo(GuiPage guiPage, int tabPageIndex, GTabPage gTabPage, SchedulerPanel schedulerPanel)
        {
            this.GuiPage = guiPage;
            this.TabPageIndex = tabPageIndex;
            this.GTabPage = gTabPage;
            this.SchedulerPanel = schedulerPanel;
        }
        /// <summary>
        /// Vstupní data pro tento panel
        /// </summary>
        public GuiPage GuiPage { get; private set; }
        /// <summary>
        /// Index záložky
        /// </summary>
        public int TabPageIndex { get; private set; }
        /// <summary>
        /// Objekt záložky obsahující panel
        /// </summary>
        public GTabPage GTabPage { get; private set; }
        /// <summary>
        /// Data panelu
        /// </summary>
        public SchedulerPanel SchedulerPanel { get; private set; }
    }
    #endregion
}
