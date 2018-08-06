using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Components;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Services;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Hlavní control Dílenské tabule: obsahuje <see cref="GToolBar"/> + <see cref="SchedulerPanel"/>.
    /// </summary>
    public class MainControl : GInteractiveControl
    {
        #region Konstruktor, inicializace, privátní proměnné grafiky
        public MainControl(MainData mainData)
            : this()
        {
            this._MainData = mainData;
        }
        public MainControl()
        {
            this._ToolBarInit();
            this._SchedulerPanelInit();
            this.CalculateLayout();
        }
        private MainData _MainData;
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.CalculateLayout();
        }
        protected void CalculateLayout()
        {
            Size size = this.ClientSize;
            int th = this._ToolBar.Bounds.Height;
            int y = 0;
            this._ToolBar.Bounds = new Rectangle(y, 0, size.Width, th);
            y = this._ToolBar.Bounds.Bottom + 1;
            this._TabContainer.Bounds = new Rectangle(0, y, size.Width, size.Height - y);
            this.Refresh();
        }
        #endregion
        #region Public rozhraní: vkládání tabulek a dalších dat
        /// <summary>
        /// Vloží novou tabulku do controlu. Metoda sama nejlépe ví, kam ji zařadit, a udělá vše potřebné.
        /// </summary>
        /// <param name="graphTable"></param>
        public void AddGraphTable(DataGraphTable graphTable)
        {
            if (graphTable == null) return;
            TabSchedulerPanelInfo tspInfo = this._TabSchedulerPanelPrepare(graphTable.DataDeclaration);
            tspInfo.SchedulerPanel.AddTable(graphTable);
        }
        #endregion
        #region ToolBar
        public void AddToolBarGroup(FunctionGlobalGroup group)
        {
            this._ToolBar.AddGroup(group);
        }
        public void AddToolBarGroups(IEnumerable<FunctionGlobalGroup> groups)
        {
            this._ToolBar.AddGroups(groups);
        }
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
        /// <param name="dataGroup"></param>
        /// <param name="activeItem"></param>
        private void _ToolBar_ItemClicked(object sender, FunctionItemEventArgs args)
        {
            if (this.ToolBarItemClicked != null)
                this.ToolBarItemClicked(this, args);
        }
        /// <summary>
        /// Událost vyvolaná po kliknutí na určitý prvek ToolBaru
        /// </summary>
        public event FunctionItemEventHandler ToolBarItemClicked;
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
        private void _SchedulerPanelInit()
        {
            this._TabContainer = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Top, TabHeaderMode = ShowTabHeaderMode.Default };
            this._TabContainer.ActivePageChanged += _TabContainerActivePageChanged;
            this.AddItem(this._TabContainer);

            this._SchedulerPanelDict = new Dictionary<int, TabSchedulerPanelInfo>();
            this._SchedulerPanelCurrentDataId = -1;
        }
        /// <summary>
        /// Najde / přidá a vrátí instanci TabSchedulerPanelInfo pro DataId dané tabulky.
        /// </summary>
        /// <param name="dataDeclaration"></param>
        private TabSchedulerPanelInfo _TabSchedulerPanelPrepare(DataDeclaration dataDeclaration)
        {
            TabSchedulerPanelInfo tspInfo;
            int dataId = dataDeclaration.DataId;
            if (!this._SchedulerPanelDict.TryGetValue(dataId, out tspInfo))
            {
                int tabPageIndex = this._TabContainer.TabCount;
                SchedulerPanel schedulerPanel = new SchedulerPanel(dataDeclaration);
                GTabPage tabPage = this._TabContainer.AddTabItem(schedulerPanel, dataDeclaration.Title, toolTip: dataDeclaration.ToolTip, image: null);
                tspInfo = new TabSchedulerPanelInfo(dataId, tabPageIndex, tabPage, schedulerPanel);
                this._SchedulerPanelDict.Add(dataId, tspInfo);
            }
            return tspInfo;
        }


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
        /// Instance všech Scheduler panelů a na něj napojených dat.
        /// Klíčem je DataId.
        /// </summary>
        private Dictionary<int, TabSchedulerPanelInfo> _SchedulerPanelDict;
        /// <summary>
        /// Index aktuálního Scheduler panelu
        /// </summary>
        private int _SchedulerPanelCurrentDataId;
        /// <summary>
        /// true pokud v <see cref="SchedulerPanelCurrent"/> je vybraný panel, false pokud není.
        /// </summary>
        protected bool SchedulerPanelExists { get { return (this._SchedulerPanelDict != null && this._SchedulerPanelCurrentDataId > 0 && this._SchedulerPanelDict.ContainsKey(this._SchedulerPanelCurrentDataId)); } }
        /// <summary>
        /// Aktuálně zobrazovaný Scheduler panel
        /// </summary>
        protected SchedulerPanel SchedulerPanelCurrent { get { return (this.SchedulerPanelExists ? this._SchedulerPanelDict[this._SchedulerPanelCurrentDataId].SchedulerPanel : null); } }
        #endregion
        #region class TabSchedulerPanelInfo - třída obsaující data o jednom panelu pro jeden klíč DataId
        /// <summary>
        /// TabSchedulerPanelInfo - třída obsaující data o jednom panelu pro jeden klíč DataId
        /// </summary>
        protected class TabSchedulerPanelInfo
        {
            public TabSchedulerPanelInfo(int dataId, int tabPageIndex, GTabPage gTabPage, SchedulerPanel schedulerPanel)
            {
                this.DataId = dataId;
                this.TabPageIndex = tabPageIndex;
                this.GTabPage = gTabPage;
                this.SchedulerPanel = schedulerPanel;
            }
            /// <summary>
            /// DataId dat v tomto panelu
            /// </summary>
            public int DataId { get; private set; }
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
