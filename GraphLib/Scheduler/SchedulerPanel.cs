using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Manufacturing.WorkScheduler;

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
        /// <summary>
        /// Konstruktor, automaticky provede načtení dat z dat guiPage
        /// </summary>
        /// <param name="mainControl">Vizuální control</param>
        /// <param name="guiPage">Data pro tento panel</param>
        public SchedulerPanel(MainControl mainControl, GuiPage guiPage)
        {
            this._MainControl = mainControl;
            this._GuiPage = guiPage;
            this._InitComponents();
            this.LoadData();
        }
        /// <summary>
        /// Vytvoří GUI objekty potřebné pro tento panel.
        /// </summary>
        private void _InitComponents()
        {
            this.Bounds = new Rectangle(0, 0, 800, 600);
            Size size = this.ClientSize;
            int x1 = 300;
            int x2 = size.Width - 300;
            int y2 = size.Height - 200;

            this._LeftPanelTabs = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Left, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
            this._LeftPanelSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = x1, BoundsNonActive = new Int32NRange(0, 200) };
            this._MainPanelGrid = new GGrid(this);
            this._RightPanelSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = x2, BoundsNonActive = new Int32NRange(0, 200) };
            this._RightPanelTabs = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Right, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
            this._BottomPanelSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Horizontal, Value = y2, BoundsNonActive = new Int32NRange(0, 600) };
            this._BottomPanelTabs = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Bottom, TabHeaderMode = ShowTabHeaderMode.CollapseItem };

            this.AddItem(this._LeftPanelTabs);
            this.AddItem(this._LeftPanelSplitter);
            this.AddItem(this._MainPanelGrid);
            this.AddItem(this._RightPanelSplitter);
            this.AddItem(this._RightPanelTabs);
            this.AddItem(this._BottomPanelSplitter);
            this.AddItem(this._BottomPanelTabs);

            this.CalculateLayout();

            this._LeftPanelSplitter.ValueChanging += LayoutChanging;
            this._LeftPanelSplitter.ValueChanged += LayoutChanging;
            this._RightPanelSplitter.ValueChanging += LayoutChanging;
            this._RightPanelSplitter.ValueChanged += LayoutChanging;
            this._BottomPanelSplitter.ValueChanging += LayoutChanging;
            this._BottomPanelSplitter.ValueChanged += LayoutChanging;
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
            if (this._BottomPanelTabs == null) return;       // Před dokončením inicializace

            Size size = this.ClientSize;
            if (size.Width < 100 || size.Height < 100) return;

            using (this.SuppressEvents())
            {
                Size? lastSize = this._LastSize;

                // Pokud si pamatujeme předešlou velikost (na kterou byly spočítány pozice při posledním běhu této metody), pak zjistíme zda nedošlo ke změně:
                bool isChangeSizeX = false;
                bool isChangeSizeY = false;
                if (lastSize.HasValue && size != lastSize.Value)
                {   // Změna velikosti: určíme, zda X a/nebo Y:
                    isChangeSizeX = (size.Width != lastSize.Value.Width);
                    isChangeSizeY = (size.Height != lastSize.Value.Height);
                }

                int sp = SplitterSize / 2;                 // Prostor "před splitterem" = odsazení souřadnice Prev.End před souřadnicí Splitter.Value
                int sn = SplitterSize - sp;                // Prostor "za splitterem" = odsazení souřadnice Next.Begin za souřadnicí Splitter.Value

                int width = size.Width - 2;
                int maxw = width / 3;
                int x0 = 1;
                int x3 = width;

                this._LeftPanelIsVisible = (this._LeftPanelIsEnabled && this._LeftPanelTabs.TabCount > 0 && width >= MinControlWidthForSideGrids);
                int x1 = CalculateLayoutOne(this._LeftPanelIsVisible, x0, this._LeftPanelSplitter.Value, x0 + MinGridWidth, x0 + maxw);

                this._MainPanelIsVisible = true;

                this._RightPanelIsVisible = (this._RightPanelIsEnabled && this._RightPanelTabs.TabCount > 0 && width >= MinControlWidthForSideGrids);
                int x2old = this._RightPanelSplitter.Value;
                if (this._RightPanelIsVisible && isChangeSizeX)
                {   // Proběhla změna šířky celého okna => o tuto změnu posuneme i pozici splitteru Source (aby držel vpravo):
                    x2old = x2old + (size.Width - lastSize.Value.Width);
                    this._RightPanelSplitter.ValueSilent = x2old;
                    x2old = this._RightPanelSplitter.Value;    // Hodnota _SourceSplitter.Value může akceptovat svoje Min-Max omezení a může se lišit od očekávání...
                }
                int x2 = CalculateLayoutOne(this._RightPanelIsVisible, x3, x2old, x3 - maxw, x3 - MinGridWidth);

                int height = size.Height;
                int maxh = height / 2;
                int y0 = 0;
                int y3 = size.Height;

                this._BottomPanelIsVisible = (this._BottomPanelIsEnabled && this._BottomPanelTabs.TabCount > 0 && height >= MinControlHeightForSideGrids);
                int y2old = this._BottomPanelSplitter.Value;
                if (this._RightPanelIsVisible && isChangeSizeY)
                {   // Proběhla změna výšky celého okna => o tuto změnu posuneme i pozici splitteru Info (aby držel dole):
                    y2old = y2old + (size.Height - lastSize.Value.Height);
                    this._BottomPanelSplitter.ValueSilent = y2old;
                    y2old = this._BottomPanelSplitter.Value;      // Hodnota _InfoSplitter.Value může akceptovat svoje Min-Max omezení a může se lišit od očekávání...
                }
                int y2 = CalculateLayoutOne(this._BottomPanelIsVisible, y3, y2old, y3 - maxh, y3 - MinGridHeight);
                int b = y2 - (this._BottomPanelIsVisible ? sp : 0);

                bool isChangeChildItems = (this._LeftPanelTabs.IsVisible != this._LeftPanelIsVisible) ||
                                          (this._LeftPanelSplitter.IsVisible != this._LeftPanelIsVisible) ||
                                          (this._MainPanelGrid.IsVisible != this._MainPanelIsVisible) ||
                                          (this._RightPanelSplitter.IsVisible != this._RightPanelIsVisible) ||
                                          (this._RightPanelTabs.IsVisible != this._RightPanelIsVisible) ||
                                          (this._BottomPanelSplitter.IsVisible != this._BottomPanelIsVisible) ||
                                          (this._BottomPanelTabs.IsVisible != this._BottomPanelIsVisible);

                this._LeftPanelTabs.IsVisible = this._LeftPanelIsVisible;
                this._LeftPanelSplitter.IsVisible = this._LeftPanelIsVisible;
                this._MainPanelGrid.IsVisible = this._MainPanelIsVisible;
                this._RightPanelSplitter.IsVisible = this._RightPanelIsVisible;
                this._RightPanelTabs.IsVisible = this._RightPanelIsVisible;
                this._BottomPanelSplitter.IsVisible = this._BottomPanelIsVisible;
                this._BottomPanelTabs.IsVisible = this._BottomPanelIsVisible;

                if (this._LeftPanelIsVisible)
                {
                    int r = x1 - sp;
                    this._LeftPanelTabs.Bounds = new Rectangle(x0, y0, r - x0, b - y0);
                    this._LeftPanelSplitter.LoadFrom(this._LeftPanelTabs.Bounds, RectangleSide.Right, true);
                }
                if (this._MainPanelIsVisible)
                {
                    int l = x1 + (this._LeftPanelIsVisible ? sn : 0);
                    int r = x2 - (this._RightPanelIsVisible ? sp : 0);
                    int t = y0;
                    this._MainPanelGrid.Bounds = new Rectangle(l, t, r - l, b - t);
                }
                if (this._RightPanelIsVisible)
                {
                    int l = x2 + sn;
                    this._RightPanelTabs.Bounds = new Rectangle(l, y0, x3 - l, b - y0);
                    this._RightPanelSplitter.LoadFrom(this._RightPanelTabs.Bounds, RectangleSide.Left, true);
                }
                if (this._BottomPanelIsVisible)
                {
                    int t = y2 + sn;
                    this._BottomPanelTabs.Bounds = new Rectangle(x0, t, x3 - x0, y3 - t);
                    this._BottomPanelSplitter.LoadFrom(this._BottomPanelTabs.Bounds, RectangleSide.Top, true);
                }

                if (isChangeChildItems)
                    this._IsChildValid = false;

                this._LastSize = size;
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
        
        private GTabContainer _LeftPanelTabs;
        private GSplitter _LeftPanelSplitter;
        private bool _LeftPanelIsVisible;
        private bool _LeftPanelIsEnabled;
        private GGrid _MainPanelGrid;
        private bool _MainPanelIsVisible;
        private GSplitter _RightPanelSplitter;
        private GTabContainer _RightPanelTabs;
        private bool _RightPanelIsVisible;
        private bool _RightPanelIsEnabled;
        private GSplitter _BottomPanelSplitter;
        private GTabContainer _BottomPanelTabs;
        private bool _BottomPanelIsVisible;
        private bool _BottomPanelIsEnabled;
        private Size? _LastSize;

        private MainControl _MainControl;
        private GuiPage _GuiPage;
        #endregion
        #region Načítání dat jednotlivých tabulek
        /// <summary>
        /// Metoda zajistí, že veškeré údaje dodané v <see cref="GuiPage"/> pro tuto stránku budou načteny a budou z nich vytvořeny příslušné tabulky.
        /// </summary>
        protected void LoadData()
        {
            this._DataTableList = new List<MainDataTable>();
            GuiPage guiPage = this._GuiPage;

            this._LeftPanelIsEnabled = this._LoadDataToTabs(guiPage.LeftPanel, this._LeftPanelTabs);
            this._LoadDataToGrid(guiPage.MainPanel, this._MainPanelGrid);
            this._RightPanelIsEnabled = this._LoadDataToTabs(guiPage.RightPanel, this._RightPanelTabs);
            this._BottomPanelIsEnabled = this._LoadDataToTabs(guiPage.BottomPanel, this._BottomPanelTabs);
        }
        /// <summary>
        /// Souhrn všech tabulek této stránky, bez ohledu na to ve kterém panelu se nacházejí
        /// </summary>
        private List<MainDataTable> _DataTableList;
        /// <summary>
        /// Metoda načte všechny tabulky typu <see cref="GuiGrid"/> z dodaného <see cref="GuiPanel"/> a vloží je do dodaného vizuálního objektu <see cref="GGrid"/>.
        /// Současně je ukládá do <see cref="_DataTableList"/>.
        /// </summary>
        /// <param name="guiPanel"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        private bool _LoadDataToGrid(GuiPanel guiPanel, GGrid grid)
        {
            if (guiPanel == null || guiPanel.Grids.Count == 0) return false;

            if (grid.SynchronizedTime == null)
                grid.SynchronizedTime = this.SynchronizedTime;

            foreach (GuiGrid guiGrid in guiPanel.Grids)
            {
                MainDataTable graphTable = new MainDataTable(this._MainControl.MainData, guiGrid);
                if (graphTable.TableRow == null) continue;

                this._DataTableList.Add(graphTable);

                grid.AddTable(graphTable.TableRow);
            }
            return true;
        }
        /// <summary>
        /// Metoda načte všechny tabulky typu <see cref="GuiGrid"/> z dodaného <see cref="GuiPanel"/> a vloží je jako nové Taby do do dodaného vizuálního objektu <see cref="GTabContainer"/>.
        /// Současně je ukládá do <see cref="_DataTableList"/>.
        /// </summary>
        /// <param name="guiPanel"></param>
        /// <param name="tabs"></param>
        /// <returns></returns>
        private bool _LoadDataToTabs(GuiPanel guiPanel, GTabContainer tabs)
        {
            if (guiPanel == null || guiPanel.Grids.Count == 0) return false;

            foreach (GuiGrid guiGrid in guiPanel.Grids)
            {
                MainDataTable graphTable = new MainDataTable(this._MainControl.MainData, guiGrid);
                if (graphTable.TableRow == null) continue;

                this._DataTableList.Add(graphTable);

                GGrid grid = new GGrid();
                grid.SynchronizedTime = this.SynchronizedTime;
                grid.AddTable(graphTable.TableRow);
                tabs.AddTabItem(grid, guiGrid.Title, guiGrid.ToolTip);
            }
            return true;
        }
        #endregion
        #region Child items
        /// <summary>
        /// Interaktivní potomci
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._GetChildList(); } }
        private IEnumerable<IInteractiveItem> _GetChildList()
        {
            if (this._ChildList == null || !this._IsChildValid)
            {
                this._ChildList = new List<IInteractiveItem>();
                if (this._LeftPanelIsVisible) this._ChildList.Add(this._LeftPanelTabs);
                if (this._MainPanelIsVisible) this._ChildList.Add(this._MainPanelGrid);
                if (this._RightPanelIsVisible) this._ChildList.Add(this._RightPanelTabs);
                if (this._BottomPanelIsVisible) this._ChildList.Add(this._BottomPanelTabs);
                if (this._LeftPanelIsVisible) this._ChildList.Add(this._LeftPanelSplitter);
                if (this._RightPanelIsVisible) this._ChildList.Add(this._RightPanelSplitter);
                if (this._BottomPanelIsVisible) this._ChildList.Add(this._BottomPanelSplitter);
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
        public Localizable.TextLoc Title { get { return this._GuiPage.Title; } }
        /// <summary>
        /// ToolTip celých dat, zobrazí se v TabHeaderu, pokud bude datových zdrojů více než 1
        /// </summary>
        public Localizable.TextLoc ToolTip { get { return this._GuiPage.ToolTip; } }
        /// <summary>
        /// Ikona celých dat, zobrazí se v TabHeaderu, pokud bude datových zdrojů více než 1
        /// </summary>
        public Image Icon { get { return this._GuiPage.Image.Image; } }
        /// <summary>
        /// true pokud je viditelná tabulka úkolů k zapracování
        /// </summary>
        public bool IsTaskEnabled { get { return this._LeftPanelIsEnabled; } set { this._LeftPanelIsEnabled = value; this.CalculateLayout();  } }
        /// <summary>
        /// true pokud je viditelná tabulka zdrojů k zaplánování
        /// </summary>
        public bool IsSourceEnabled { get { return this._RightPanelIsEnabled; } set { this._RightPanelIsEnabled = value; this.CalculateLayout(); } }
        /// <summary>
        /// true pokud je viditelná tabulka informací o zaplánování
        /// </summary>
        public bool IsInfoEnabled { get { return this._BottomPanelIsEnabled; } set { this._BottomPanelIsEnabled = value; this.CalculateLayout(); } }
        /// <summary>
        /// Synchronizační element časové osy
        /// </summary>
        public ValueSynchronizer<TimeRange> SynchronizedTime { get { return this._MainControl.SynchronizedTime; } }
        #endregion
    }
}
