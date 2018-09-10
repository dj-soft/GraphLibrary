using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Components;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Manufacturing.WorkScheduler;

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
            this._ToolBarInit();
            this._SchedulerPanelInit();
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
            if (this._ToolBar.IsVisible)
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
        /// Metoda přidá jednu stránku s daty podle dat dodaných v parametru
        /// </summary>
        /// <param name="guiPage"></param>
        public void AddPage(GuiPage guiPage)
        {
            this._SchedulerPanelAdd(guiPage);
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
        public ValueSynchronizer<TimeRange> SynchronizedTime
        {
            get { if (this._SynchronizedTime == null) this._SynchronizedTime = new ValueSynchronizer<TimeRange>(); return this._SynchronizedTime; }
            set { this._SynchronizedTime = value; }
        }
        private ValueSynchronizer<TimeRange> _SynchronizedTime;

        #endregion
        #region ToolBar
        /// <summary>
        /// Bude zobrazován ToolBar?
        /// </summary>
        public bool ToolBarVisible { get { return this._ToolBar.IsVisible; } set { this._ToolBar.IsVisible = value; this.CalculateLayout(); } }
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
        /// Inicializace toolbaru
        /// </summary>
        private void _ToolBarInit()
        {
            this._ToolBar = new GToolBar() { Bounds = new Rectangle(0, 0, 1024, 64) };
            this._ToolBar.ToolbarSizeChanged += _ToolBarSizeChanged;
            this.AddItem(this._ToolBar);
            this._ToolBar.ItemClicked += _ToolBar_ItemClicked;
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek (<see cref="GToolBar"/>) po kliknutí na něj, úkolem je vyvolat event <see cref="MainControl.ToolBarItemClicked"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBar_ItemClicked(object sender, FunctionItemEventArgs args)
        {
            if (this.ToolBarItemClicked != null)
                this.ToolBarItemClicked(this, args);
        }
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

            this._SchedulerPanelList = new List<TabSchedulerPanelInfo>();
        }
        /// <summary>
        /// Metoda přidá jednu stránku s daty podle dat dodaných v parametru
        /// </summary>
        /// <param name="guiPage"></param>
        private void _SchedulerPanelAdd(GuiPage guiPage)
        {
            int tabPageIndex = this._TabContainer.TabCount;
            SchedulerPanel schedulerPanel = new SchedulerPanel(this, guiPage);
            GTabPage tabPage = this._TabContainer.AddTabItem(schedulerPanel, guiPage.Title, toolTip: guiPage.ToolTip, image: null);
            TabSchedulerPanelInfo tspInfo = new TabSchedulerPanelInfo(guiPage, tabPageIndex, tabPage, schedulerPanel);
            this._SchedulerPanelList.Add(tspInfo);
        }
        /// <summary>
        /// Instance všech Scheduler panelů a na něj napojených dat.
        /// </summary>
        private List<TabSchedulerPanelInfo> _SchedulerPanelList;
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
        /// Obsahue (vrací) instanci <see cref="TabSchedulerPanelInfo"/> ze seznamu <see cref="_SchedulerPanelList"/>, která se týká té stránky <see cref="GTabPage"/>, 
        /// která je aktivní v <see cref="_TabContainer"/>.
        /// </summary>
        protected TabSchedulerPanelInfo SchedulerTabPanelCurrent
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
        protected SchedulerPanel SchedulerPanelCurrent { get { TabSchedulerPanelInfo tsp = this.SchedulerTabPanelCurrent;  return (tsp != null ? tsp.SchedulerPanel : null); } }
        #endregion
        #region class TabSchedulerPanelInfo - třída obsaující data o jednom panelu pro jeden klíč DataId
        /// <summary>
        /// TabSchedulerPanelInfo - třída obsaující data o jednom panelu pro jeden klíč DataId
        /// </summary>
        protected class TabSchedulerPanelInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="guiPage"></param>
            /// <param name="tabPageIndex"></param>
            /// <param name="gTabPage"></param>
            /// <param name="schedulerPanel"></param>
            public TabSchedulerPanelInfo(GuiPage guiPage, int tabPageIndex, GTabPage gTabPage, SchedulerPanel schedulerPanel)
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
}
