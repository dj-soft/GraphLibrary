using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using Asol.Tools.WorkScheduler.Components.Grid;
using Asol.Tools.WorkScheduler.Application;

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
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerPanel", "InitComponents", ""))
            {
                this.Bounds = new Rectangle(0, 0, 800, 600);
                this._PanelLayout = new SchedulerPanelLayout();
                this._PanelLayout.CurrentControlSize = this.ClientSize;
                Size size = this.ClientSize;
                int x1 = 300;
                int x2 = size.Width - 300;
                int y2 = size.Height - 200;

                this._LeftPanelTabs = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Left, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
                this._LeftPanelSplitter = new GSplitter() { SplitterVisibleWidth = this._PanelLayout.SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = this._PanelLayout.LeftSplitterValue, BoundsNonActive = new Int32NRange(0, 200) };
                this._MainPanelGrid = new GGrid(this);
                this._RightPanelSplitter = new GSplitter() { SplitterVisibleWidth = this._PanelLayout.SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = this._PanelLayout.RightSplitterValue, BoundsNonActive = new Int32NRange(0, 200) };
                this._RightPanelTabs = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Right, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
                this._BottomPanelSplitter = new GSplitter() { SplitterVisibleWidth = this._PanelLayout.SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Horizontal, Value = this._PanelLayout.BottomSplitterValue, BoundsNonActive = new Int32NRange(0, 600) };
                this._BottomPanelTabs = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Bottom, TabHeaderMode = ShowTabHeaderMode.CollapseItem };

                this.AddItem(this._LeftPanelTabs);
                this.AddItem(this._LeftPanelSplitter);
                this.AddItem(this._MainPanelGrid);
                this.AddItem(this._RightPanelSplitter);
                this.AddItem(this._RightPanelTabs);
                this.AddItem(this._BottomPanelSplitter);
                this.AddItem(this._BottomPanelTabs);

                this.CalculateLayout();

                this._LeftPanelTabs.IsCollapsedChanged += _TabContainers_IsCollapsedChanged;
                this._LeftPanelSplitter.ValueChanging += _SplitterL_ValueChanging;
                this._LeftPanelSplitter.ValueChanged += _SplitterL_ValueChanging;
                this._RightPanelTabs.IsCollapsedChanged += _TabContainers_IsCollapsedChanged;
                this._RightPanelSplitter.ValueChanging += _SplitterR_ValueChanging;
                this._RightPanelSplitter.ValueChanged += _SplitterR_ValueChanging;
                this._BottomPanelTabs.IsCollapsedChanged += _TabContainers_IsCollapsedChanged;
                this._BottomPanelSplitter.ValueChanging += _SplitterB_ValueChanging;
                this._BottomPanelSplitter.ValueChanged += _SplitterB_ValueChanging;
            }
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
        /// Po změně pozice splitteru Left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterL_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            if (this.IsSuppressedEvent) return;
            if (this._LeftSplitterIsVisible)
            {
                this._PanelLayout.LeftSplit = this._LeftPanelSplitter.Value;
                this.CalculateLayout();
            }
        }
        /// <summary>
        /// Po změně pozice splitteru Right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterR_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            if (this.IsSuppressedEvent) return;
            if (this._RightSplitterIsVisible)
            {
                this._PanelLayout.RightSplit = (this.ClientSize.Width - this._RightPanelSplitter.Value);
                this.CalculateLayout();
            }
        }
        /// <summary>
        /// Po změně pozice splitteru Bottom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterB_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            if (this.IsSuppressedEvent) return;
            if (this._BottomSplitterIsVisible)
            {
                this._PanelLayout.BottomSplit = (this.ClientSize.Height - this._BottomPanelSplitter.Value);
                this.CalculateLayout();
            }
        }
        /// <summary>
        /// Po změně IsCollapsed na některém z panelů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabContainers_IsCollapsedChanged(object sender, GPropertyChangeArgs<bool> e)
        {
            this.CalculateLayout();
        }
        private void CalculateLayout()
        {
            if (this.IsSuppressedEvent) return;
            if (this._BottomPanelTabs == null) return;       // Před dokončením inicializace

            this._PanelLayout.CurrentControlSize = this.ClientSize;
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
                this._LeftSplitterIsVisible = (this._LeftPanelIsVisible && !this._LeftPanelTabs.IsCollapsed);
                int x1 = CalculateLayoutOne(this._LeftPanelIsVisible, x0, this._LeftPanelSplitter.Value, x0 + MinGridWidth, x0 + maxw);

                this._MainPanelIsVisible = true;

                this._RightPanelIsVisible = (this._RightPanelIsEnabled && this._RightPanelTabs.TabCount > 0 && width >= MinControlWidthForSideGrids);
                this._RightSplitterIsVisible = (this._RightPanelIsVisible && !this._RightPanelTabs.IsCollapsed);
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
                this._BottomSplitterIsVisible = (this._BottomPanelIsVisible && !this._BottomPanelTabs.IsCollapsed);
                int y2old = this._BottomPanelSplitter.Value;
                if (this._RightPanelIsVisible && isChangeSizeY)
                {   // Proběhla změna výšky celého okna => o tuto změnu posuneme i pozici splitteru Info (aby držel dole):
                    y2old = y2old + (size.Height - lastSize.Value.Height);
                    this._BottomPanelSplitter.ValueSilent = y2old;
                    y2old = this._BottomPanelSplitter.Value;      // Hodnota _InfoSplitter.Value může akceptovat svoje Min-Max omezení a může se lišit od očekávání...
                }
                int y2 = CalculateLayoutOne(this._BottomPanelIsVisible, y3, y2old, y3 - maxh, y3 - MinGridHeight);
                int b = y2 - (this._BottomPanelIsVisible ? sp : 0);

                bool isChangeChildItems = (this._LeftPanelTabs.Is.Visible != this._LeftPanelIsVisible) ||
                                          (this._LeftPanelSplitter.Is.Visible != this._LeftSplitterIsVisible) ||
                                          (this._MainPanelGrid.Is.Visible != this._MainPanelIsVisible) ||
                                          (this._RightPanelSplitter.Is.Visible != this._RightPanelIsVisible) ||
                                          (this._RightPanelTabs.Is.Visible != this._RightSplitterIsVisible) ||
                                          (this._BottomPanelSplitter.Is.Visible != this._BottomSplitterIsVisible) ||
                                          (this._BottomPanelTabs.Is.Visible != this._BottomPanelIsVisible);

                this._LeftPanelTabs.Is.Visible = this._LeftPanelIsVisible;
                this._LeftPanelSplitter.Is.Visible = this._LeftSplitterIsVisible;
                this._MainPanelGrid.Is.Visible = this._MainPanelIsVisible;
                this._RightPanelSplitter.Is.Visible = this._RightSplitterIsVisible;
                this._RightPanelTabs.Is.Visible = this._RightPanelIsVisible;
                this._BottomPanelSplitter.Is.Visible = this._BottomSplitterIsVisible;
                this._BottomPanelTabs.Is.Visible = this._BottomPanelIsVisible;

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

        private SchedulerPanelLayout _PanelLayout;

        private GTabContainer _LeftPanelTabs;
        private GSplitter _LeftPanelSplitter;
        private bool _LeftPanelIsVisible;
        private bool _LeftPanelIsEnabled;
        private bool _LeftSplitterIsVisible;
        private GGrid _MainPanelGrid;
        private bool _MainPanelIsVisible;
        private GSplitter _RightPanelSplitter;
        private GTabContainer _RightPanelTabs;
        private bool _RightPanelIsVisible;
        private bool _RightPanelIsEnabled;
        private bool _RightSplitterIsVisible;
        private GSplitter _BottomPanelSplitter;
        private GTabContainer _BottomPanelTabs;
        private bool _BottomPanelIsVisible;
        private bool _BottomPanelIsEnabled;
        private bool _BottomSplitterIsVisible;
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
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerPanel", "LoadData", ""))
            {
                this._DataTableList = new List<MainDataTable>();
                GuiPage guiPage = this._GuiPage;

                this._LeftPanelIsEnabled = this._LoadDataToTabs(guiPage.LeftPanel, this._LeftPanelTabs);
                this._LoadDataToGrid(guiPage.MainPanel, this._MainPanelGrid);
                this._RightPanelIsEnabled = this._LoadDataToTabs(guiPage.RightPanel, this._RightPanelTabs);
                this._BottomPanelIsEnabled = this._LoadDataToTabs(guiPage.BottomPanel, this._BottomPanelTabs);
            }
        }
        /// <summary>
        /// Souhrn všech tabulek této stránky, bez ohledu na to ve kterém panelu se nacházejí
        /// </summary>
        public IEnumerable<MainDataTable> DataTables { get { return this._DataTableList; } }
        /// <summary>
        /// Souhrn všech tabulek této stránky, bez ohledu na to ve kterém panelu se nacházejí
        /// </summary>
        private List<MainDataTable> _DataTableList;
        /// <summary>
        /// Metoda načte všechny tabulky typu <see cref="GuiGrid"/> z dodaného <see cref="GuiPanel"/> a vloží je do dodaného vizuálního objektu <see cref="GGrid"/>.
        /// Současně je ukládá do <see cref="_DataTableList"/>.
        /// </summary>
        /// <param name="guiPanel"></param>
        /// <param name="gGrid"></param>
        /// <returns></returns>
        private bool _LoadDataToGrid(GuiPanel guiPanel, GGrid gGrid)
        {
            if (guiPanel == null || guiPanel.Grids.Count == 0) return false;

            if (gGrid.SynchronizedTime == null)
                gGrid.SynchronizedTime = this.SynchronizedTime;

            foreach (GuiGrid guiGrid in guiPanel.Grids)
            {
                MainDataTable mainDataTable = this._LoadDataToMainTable(gGrid, guiGrid);
                if (mainDataTable == null) continue;
            }
            return true;
        }
        /// <summary>
        /// Metoda načte všechny tabulky typu <see cref="GuiGrid"/> z dodaného <see cref="GuiPanel"/> a vloží je jako nové Taby do dodaného vizuálního objektu <see cref="GTabContainer"/>.
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
                GGrid gGrid = new GGrid();
                gGrid.SynchronizedTime = this.SynchronizedTime;

                MainDataTable mainDataTable = this._LoadDataToMainTable(gGrid, guiGrid);
                if (mainDataTable == null) continue;

                tabs.AddTabItem(gGrid, guiGrid.Title, guiGrid.ToolTip);
            }
            return true;
        }
        /// <summary>
        /// Metoda vytvoří novou tabulku <see cref="MainDataTable"/> s daty dodanými v <see cref="GuiGrid"/>.
        /// Pokud data neobsahují tabulku s řádky, vrací null.
        /// Vytvořenou tabulku <see cref="MainDataTable"/> uloží do <see cref="_DataTableList"/>,
        /// do vizuálního gridu <see cref="GGrid"/> přidá tabulku s řádky z dodaného <see cref="GuiGrid"/>.
        /// Vytvořenou tabulku <see cref="MainDataTable"/> vrací.
        /// </summary>
        /// <param name="gGrid"></param>
        /// <param name="guiGrid"></param>
        /// <returns></returns>
        private MainDataTable _LoadDataToMainTable(GGrid gGrid, GuiGrid guiGrid)
        {
            MainDataTable mainDataTable = null;
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerPanel", "LoadDataToMainTable", "", guiGrid.FullName))
                mainDataTable = new MainDataTable(this, gGrid, guiGrid);

            if (mainDataTable.TableRow == null) return null;

            this._DataTableList.Add(mainDataTable);
            mainDataTable.AddTableToGrid(gGrid);

            return mainDataTable;
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
        /// Reference na Main control (toolbar, panel)
        /// </summary>
        public MainControl MainControl { get { return this._MainControl; } }
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
        public ValueTimeRangeSynchronizer SynchronizedTime { get { return this._MainControl.SynchronizedTime; } }
        #endregion
    }
    /// <summary>
    /// Metoda řídí Layout jednoho panelu <see cref="SchedulerPanel"/>,
    /// a dále slouží k jeho ukládání/načítání do/z Configu a k jeho reaktivaci.
    /// </summary>
    public class SchedulerPanelLayout
    {
        /// <summary>
        /// Pozice levého splitteru, měřeno zleva, pokud je viditelný
        /// </summary>
        public int LeftSplit { get { return this._LeftSplit; } set { this._LeftSplit = _AlignV(value); } } private int _LeftSplit = DefVSplit;
        /// <summary>
        /// Pozice splitteru v MainGrid, měřeno odspodu Gridu, pokud má více než jednu tabulku
        /// </summary>
        public int MainSplit { get { return this._MainSplit; } set { this._MainSplit = _AlignV(value); } } private int _MainSplit = DefVSplit;
        /// <summary>
        /// Pozice pravého splitteru, měřeno odprava, pokud je viditelný
        /// </summary>
        public int RightSplit { get { return this._RightSplit; } set { this._RightSplit = _AlignH(value); } } private int _RightSplit = DefHSplit;
        /// <summary>
        /// Pozice dolního splitteru, měřeno odspodu, pokud je viditelný
        /// </summary>
        public int BottomSplit { get { return this._BottomSplit; } set { this._BottomSplit = _AlignH(value); } } private int _BottomSplit = DefHSplit;
        /// <summary>
        /// Viditelná šířka Splitteru
        /// </summary>
        public int SplitterSize { get { return this._SplitterSize; } set { this._SplitterSize = _Align(value, 1, 6); } } private int _SplitterSize = 4;

        /// <summary>
        /// Minimální šířka controlu potřebná pro to, aby bylo možno zobrazit postranní panely
        /// </summary>
        public const int MinControlWidthForSideGrids = 800;
        /// <summary>
        /// Minimální výška controlu potřebná pro to, aby bylo možno zobrazit dolní panel
        /// </summary>
        public const int MinControlHeightForSideGrids = 600;
        #region Zarovnání hodnoty
        /// <summary>
        /// Zarovná hodnotu do mezí MinVSplit, MaxVSplit
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int _AlignV(int value)
        {
            return _Align(value, MinVSplit, MaxVSplit);
        }
        /// <summary>
        /// Zarovná hodnotu do mezí MinHSplit, MaxHSplit
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int _AlignH(int value)
        {
            return _Align(value, MinHSplit, MaxHSplit);
        }
        /// <summary>
        /// Zarovná hodnotu do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static int _Align(int value, int min, int max)
        {
            return (value > max ? max : (value < min ? min : value));
        }
        /// <summary>
        /// Minimální hodnota Vertikálního (svislého) splitteru
        /// </summary>
        private const int MinVSplit = 85;
        /// <summary>
        /// Maximální hodnota Vertikálního (svislého) splitteru
        /// </summary>
        private const int MaxVSplit = 800;
        /// <summary>
        /// Defaultní hodnota Vertikálního (svislého) splitteru
        /// </summary>
        private const int DefVSplit = 185;
        /// <summary>
        /// Minimální hodnota Horizontálního (vodorovného) splitteru
        /// </summary>
        private const int MinHSplit = 60;
        /// <summary>
        /// Maximální hodnota Horizontálního (vodorovného) splitteru
        /// </summary>
        private const int MaxHSplit = 600;
        /// <summary>
        /// Defaultní hodnota Horizontálního (vodorovného) splitteru
        /// </summary>
        private const int DefHSplit = 150;
        #endregion
        #region Výpočty Current souřadnic, non persisted
        /// <summary>
        /// Aktuální velikost plochy Controlu
        /// </summary>
        [PersistingEnabled(false)]
        public Size CurrentControlSize { get; set; }
        protected int CurrentWidth { get { return this.CurrentControlSize.Width; } }
        protected int CurrentHeight { get { return this.CurrentControlSize.Width; } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="CurrentControlSize"/> má výšku i šířku alespoň 100 nebo více
        /// </summary>
        public bool IsCurrentSizeValid { get { return (this.CurrentWidth >= 100 && this.CurrentHeight >= 100); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="CurrentControlSize"/> má šířku alespoň <see cref="MinControlWidthForSideGrids"/> nebo více
        /// </summary>
        public bool IsLeftTabsEnabled { get { return (this.CurrentWidth >= MinControlWidthForSideGrids); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="CurrentControlSize"/> má šířku alespoň <see cref="MinControlWidthForSideGrids"/> nebo více
        /// </summary>
        public bool IsRightTabsEnabled { get { return (this.CurrentWidth >= MinControlWidthForSideGrids); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="CurrentControlSize"/> má šířku alespoň <see cref="MinControlHeightForSideGrids"/> nebo více
        /// </summary>
        public bool IsBottomTabsEnabled { get { return (this.CurrentHeight >= MinControlHeightForSideGrids); } }
        /// <summary>
        /// Value pro LeftSplitter
        /// </summary>
        public int LeftSplitterValue { get { return this.LeftSplit; } }
        /// <summary>
        /// Value pro RightSplitter
        /// </summary>
        public int RightSplitterValue { get { return this.CurrentWidth - this.RightSplit; } }
        /// <summary>
        /// Value pro BottomSplitter
        /// </summary>
        public int BottomSplitterValue { get { return this.CurrentHeight - this.BottomSplit; } }


        
        #endregion


        public void 

    }
}
