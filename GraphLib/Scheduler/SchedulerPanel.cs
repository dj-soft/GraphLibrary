using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Panel jedné Dílenské tabule: obsahuje všechny prvky pro zobrazení dat jedné verze plánu (potřebné <see cref="GTabContainer"/>, <see cref="GGrid"/>, <see cref="GSplitter"/>), ale neobsahuje <see cref="GToolBar"/>.
    /// Hlavní control <see cref="MainControl"/> se skládá z jednoho prvku <see cref="GToolBar"/> a z jednoho <see cref="GTabContainer"/>, 
    /// který v sobě hostuje controly <see cref="SchedulerPanel"/>, jeden pro každou jednu zadanou verzi plánu (DataId).
    /// </summary>
    public class SchedulerPanel : InteractiveContainer, IInteractiveItem
    {
        #region Konstruktor, inicializace, privátní proměnné
        public SchedulerPanel(MainControl mainControl, DataDeclaration panelDataDeclaration)
        {
            this._MainControl = mainControl;
            this._PanelDataDeclaration = panelDataDeclaration;
            this._InitComponents();
        }
        /// <summary>
        /// Vytvoří GUI objekty potřebné pro tento panel.
        /// </summary>
        private void _InitComponents()
        {
            this._TaskContainer = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Left, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
            this._TaskSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 300, BoundsNonActive = new Int32NRange(0, 200) };
            this._SchedulerGrid = new GGrid(this);
            this._SourceSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 600, BoundsNonActive = new Int32NRange(0, 200) };
            this._SourceContainer = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Right, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
            this._InfoSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Horizontal, Value = 300, BoundsNonActive = new Int32NRange(0, 600) };
            this._InfoContainer = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Bottom, TabHeaderMode = ShowTabHeaderMode.CollapseItem };

            this.AddItem(this._TaskContainer);
            this.AddItem(this._TaskSplitter);
            this.AddItem(this._SchedulerGrid);
            this.AddItem(this._SourceSplitter);
            this.AddItem(this._SourceContainer);
            this.AddItem(this._InfoSplitter);
            this.AddItem(this._InfoContainer);

            this._TaskSplitter.ValueChanging += LayoutChanging;
            this._TaskSplitter.ValueChanged += LayoutChanging;
            this._SourceSplitter.ValueChanging += LayoutChanging;
            this._SourceSplitter.ValueChanged += LayoutChanging;
            this._InfoSplitter.ValueChanging += LayoutChanging;
            this._InfoSplitter.ValueChanged += LayoutChanging;
        }
        /// <summary>
        /// Po změně velikosti controlu
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            this.CalculateLayout();
        }
        /// <summary>
        /// Po změně pozice kteréhokoliv splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutChanging(object sender, GPropertyChangeArgs<int> e)
        {
            this.CalculateLayout();
        }
        private void CalculateLayout()
        {
            if (this.IsSuppressedEvent) return;

            using (this.SuppressEvents())
            {
                Size size = this.ClientSize;

                int sp = SplitterSize / 2;                 // Prostor "před splitterem" = odsazení souřadnice Prev.End před souřadnicí Splitter.Value
                int sn = SplitterSize - sp;                // Prostor "za splitterem" = odsazení souřadnice Next.Begin za souřadnicí Splitter.Value

                int width = size.Width - 2;
                int maxw = width / 3;
                int x0 = 1;
                int x3 = width;

                this._IsTaskVisible = (this._IsTaskEnabled && this._TaskContainer.TabCount > 0 && width >= MinControlWidthForSideGrids);
                int x1 = CalculateLayoutOne(this._IsTaskVisible, x0, this._TaskSplitter.Value, x0 + MinGridWidth, x0 + maxw);

                this._IsSchedulerVisible = true;

                this._IsSourceVisible = (this._IsSourceEnabled && this._SourceContainer.TabCount > 0 && width >= MinControlWidthForSideGrids);
                int x2 = CalculateLayoutOne(this._IsSourceVisible, x3, this._SourceSplitter.Value, x3 - maxw, x3 - MinGridWidth);

                int height = size.Height;
                int maxh = height / 2;
                int y0 = 0;
                int y3 = size.Height;

                this._IsInfoVisible = (this._IsInfoEnabled && this._InfoContainer.TabCount > 0 && height >= MinControlHeightForSideGrids);
                int y2 = CalculateLayoutOne(this._IsInfoVisible, y3, this._InfoSplitter.Value, y3 - maxh, y3 - MinGridHeight);

                bool isChangeChildItems = (this._TaskContainer.IsVisible != this._IsTaskVisible) ||
                                          (this._TaskSplitter.IsVisible != this._IsTaskVisible) ||
                                          (this._SchedulerGrid.IsVisible != this._IsSchedulerVisible) ||
                                          (this._SourceSplitter.IsVisible != this._IsSourceVisible) ||
                                          (this._SourceContainer.IsVisible != this._IsSourceVisible) ||
                                          (this._InfoSplitter.IsVisible != this._IsInfoVisible) ||
                                          (this._InfoContainer.IsVisible != this._IsInfoVisible);

                this._TaskContainer.IsVisible = this._IsTaskVisible;
                this._TaskSplitter.IsVisible = this._IsTaskVisible;
                this._SchedulerGrid.IsVisible = this._IsSchedulerVisible;
                this._SourceSplitter.IsVisible = this._IsSourceVisible;
                this._SourceContainer.IsVisible = this._IsSourceVisible;
                this._InfoSplitter.IsVisible = this._IsInfoVisible;
                this._InfoContainer.IsVisible = this._IsInfoVisible;

                if (this._IsTaskVisible)
                {
                    int r = x1 - sp;
                    int b = y2 - (this._IsInfoVisible ? sp : 0);
                    this._TaskContainer.Bounds = new Rectangle(x0, y0, r - x0, b - y0);
                    this._TaskSplitter.LoadFrom(this._TaskContainer.Bounds, RectangleSide.Right, true);
                }
                if (this._IsSchedulerVisible)
                {
                    int l = x1 + (this._IsTaskVisible ? sn : 0);
                    int r = x2 - (this._IsSourceVisible ? sp : 0);
                    int t = y0;
                    int b = y2 - (this._IsInfoVisible ? sp : 0);
                    this._SchedulerGrid.Bounds = new Rectangle(l, t, r - l, b - t);
                }
                if (this._IsSourceVisible)
                {
                    int l = x2 + sn;
                    int b = y2 - (this._IsInfoVisible ? sp : 0);
                    this._SourceContainer.Bounds = new Rectangle(l, y0, x3 - l, b - y0);
                    this._SourceSplitter.LoadFrom(this._SourceContainer.Bounds, RectangleSide.Left, true);
                }
                if (this._IsInfoVisible)
                {
                    int t = y2 + sn;
                    this._InfoContainer.Bounds = new Rectangle(x0, t, x3 - x0, y3 - t);
                    this._InfoSplitter.LoadFrom(this._InfoContainer.Bounds, RectangleSide.Top, true);
                }

                if (isChangeChildItems)
                    this._IsChildValid = false;
            }
        }
        private static int CalculateLayoutOne(bool isVisible, int valueInvisible, int valueVisible, int valueMin, int valueMax)
        {
            if (!isVisible) return valueInvisible;
            if (valueVisible < valueMin) return valueMin;
            if (valueVisible > valueMax) return valueMax;
            return valueVisible;
        }
        private const int MinControlWidthForSideGrids = 800;
        private const int MinControlHeightForSideGrids = 600;
        private const int MinGridWidth = 95;
        private const int MinGridHeight = 75;
        private const int SplitterSize = 4;
        
        private GTabContainer _TaskContainer;
        private GSplitter _TaskSplitter;
        private bool _IsTaskVisible;
        private bool _IsTaskEnabled;
        private GGrid _SchedulerGrid;
        private bool _IsSchedulerVisible;
        private GSplitter _SourceSplitter;
        private GTabContainer _SourceContainer;
        private bool _IsSourceVisible;
        private bool _IsSourceEnabled;
        private GSplitter _InfoSplitter;
        private GTabContainer _InfoContainer;
        private bool _IsInfoVisible;
        private bool _IsInfoEnabled;
        private MainControl _MainControl;
        private DataDeclaration _PanelDataDeclaration;
        #endregion
        #region Child items
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._GetChildList(); } }
        private IEnumerable<IInteractiveItem> _GetChildList()
        {
            if (this._ChildList == null || !this._IsChildValid)
            {
                this._ChildList = new List<IInteractiveItem>();
                if (this._IsTaskVisible) this._ChildList.Add(this._TaskContainer);
                if (this._IsSchedulerVisible) this._ChildList.Add(this._SchedulerGrid);
                if (this._IsSourceVisible) this._ChildList.Add(this._SourceContainer);
                if (this._IsInfoVisible) this._ChildList.Add(this._InfoContainer);
                if (this._IsTaskVisible) this._ChildList.Add(this._TaskSplitter);
                if (this._IsSourceVisible) this._ChildList.Add(this._SourceSplitter);
                if (this._IsInfoVisible) this._ChildList.Add(this._InfoSplitter);
            }
            return this._ChildList;
        }
        private List<IInteractiveItem> _ChildList;
        private bool _IsChildValid;
        #endregion
        #region Public data
        /// <summary>
        /// Titulek celých dat, zobrazí se v TabHeaderu, pokud bude datových zdrojů více než 1
        /// </summary>
        public Localizable.TextLoc Title { get { return this._Title; } set { this._Title = value; } }
        private Localizable.TextLoc _Title;
        /// <summary>
        /// ToolTip celých dat, zobrazí se v TabHeaderu, pokud bude datových zdrojů více než 1
        /// </summary>
        public Localizable.TextLoc ToolTip { get { return this._ToolTip; } set { this._ToolTip = value; } }
        private Localizable.TextLoc _ToolTip;
        /// <summary>
        /// Ikona celých dat, zobrazí se v TabHeaderu, pokud bude datových zdrojů více než 1
        /// </summary>
        public Image Icon { get { return this._Icon; } set { this._Icon = value; } }
        private Image _Icon;
        /// <summary>
        /// true pokud je viditelná tabulka úkolů k zapracování
        /// </summary>
        public bool IsTaskEnabled { get { return this._IsTaskEnabled; } set { this._IsTaskEnabled = value; this.CalculateLayout();  } }
        /// <summary>
        /// true pokud je viditelná tabulka zdrojů k zaplánování
        /// </summary>
        public bool IsSourceEnabled { get { return this._IsSourceEnabled; } set { this._IsSourceEnabled = value; this.CalculateLayout(); } }
        /// <summary>
        /// true pokud je viditelná tabulka informací o zaplánování
        /// </summary>
        public bool IsInfoEnabled { get { return this._IsInfoEnabled; } set { this._IsInfoEnabled = value; this.CalculateLayout(); } }
        /// <summary>
        /// Synchronizační element časové osy
        /// </summary>
        public ValueSynchronizer<TimeRange> SynchronizedTime { get { return this._MainControl.SynchronizedTime; } }

        #endregion
        #region Tabulky
        /// <summary>
        /// Do this panelu přidá další tabulku.
        /// Pozici tabulky určí z <see cref="MainDataTable.DataDeclaration"/> : <see cref="DataDeclaration.Target"/>
        /// </summary>
        /// <param name="graphTable"></param>
        public void AddGraphTable(MainDataTable graphTable)
        {
            DataTargetType target = graphTable.Target;
            switch (target)
            {
                case DataTargetType.Task:
                    this.AddTableToTabs(graphTable, this._TaskContainer);
                    this._IsTaskEnabled = (this._TaskContainer.TabCount > 0);
                    break;
                case DataTargetType.Schedule:
                    this.AddTableToGrid(graphTable, this._SchedulerGrid);
                    break;
                case DataTargetType.Source:
                    this.AddTableToTabs(graphTable, this._SourceContainer);
                    this._IsSourceEnabled = (this._SourceContainer.TabCount > 0);
                    break;
                case DataTargetType.Info:
                    this.AddTableToTabs(graphTable, this._InfoContainer);
                    this._IsInfoEnabled = (this._InfoContainer.TabCount > 0);
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí, že do daného TabContaineru se přidá nová záložka, zobrazující danou tabulku.
        /// </summary>
        /// <param name="graphTable"></param>
        /// <param name="tabs"></param>
        private void AddTableToTabs(MainDataTable graphTable, GTabContainer tabs)
        {
            if (graphTable == null || graphTable.TableRow == null) return;
            GGrid grid = new GGrid();
            grid.SynchronizedTime = this.SynchronizedTime;
            grid.AddTable(graphTable.TableRow);
            tabs.AddTabItem(grid, graphTable.DataDeclaration.Title, graphTable.DataDeclaration.ToolTip);
        }
        /// <summary>
        /// Metoda zajistí, že do daného Gridu se přidá nová tabulka z dodaného objektu.
        /// </summary>
        /// <param name="graphTable"></param>
        /// <param name="grid"></param>
        private void AddTableToGrid(MainDataTable graphTable, GGrid grid)
        {
            if (graphTable == null || graphTable.TableRow == null) return;
            grid.SynchronizedTime = this.SynchronizedTime;
            grid.AddTable(graphTable.TableRow);
        }
        #endregion
    }
}
