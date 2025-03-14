﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using Asol.Tools.WorkScheduler.Components.Grids;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Panel jedné Dílenské tabule: obsahuje všechny prvky pro zobrazení dat jedné verze plánu (potřebné <see cref="TabContainer"/>, <see cref="GGrid"/>, <see cref="Splitter"/>), ale neobsahuje <see cref="ToolBar"/>.
    /// Hlavní control <see cref="MainControl"/> se skládá z jednoho prvku <see cref="ToolBar"/> a z jednoho <see cref="TabContainer"/>, 
    /// který v sobě hostuje controly <see cref="SchedulerPanel"/>, jeden pro každou jednu zadanou verzi plánu (DataId).
    /// </summary>
    public class SchedulerPanel : InteractiveContainer, IInteractiveItem
    {
        #region Konstruktor, inicializace, privátní proměnné
        /// <summary>
        /// Konstruktor.
        /// Vytvoří vizuální control, ale nenačítá data - k tomu slouží metoda <see cref="LoadData()"/>.
        /// </summary>
        /// <param name="mainControl">Vizuální control</param>
        /// <param name="guiPage">Data pro tento panel</param>
        public SchedulerPanel(MainControl mainControl, GuiPage guiPage)
        {
            this._MainControl = mainControl;
            this._GuiPage = guiPage;
            this._InitComponents();
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

                this._LeftPanelTabs = new TabContainer(this) { TabHeaderPosition = RectangleSide.Left, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
                this._LeftPanelSplitter = new Components.Splitter() { SplitterVisibleWidth = this._PanelLayout.SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = this._PanelLayout.LeftSplitterValue, BoundsNonActive = new Int32NRange(0, 200) };
                this._MainPanelGrid = new GGrid(this);
                this._RightPanelSplitter = new Components.Splitter() { SplitterVisibleWidth = this._PanelLayout.SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = this._PanelLayout.RightSplitterValue, BoundsNonActive = new Int32NRange(0, 200) };
                this._RightPanelTabs = new TabContainer(this) { TabHeaderPosition = RectangleSide.Right, TabHeaderMode = ShowTabHeaderMode.CollapseItem };
                this._BottomPanelSplitter = new Components.Splitter() { SplitterVisibleWidth = this._PanelLayout.SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Horizontal, Value = this._PanelLayout.BottomSplitterValue, BoundsNonActive = new Int32NRange(0, 600) };
                this._BottomPanelTabs = new TabContainer(this) { TabHeaderPosition = RectangleSide.Bottom, TabHeaderMode = ShowTabHeaderMode.CollapseItem };

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
            this._PanelLayout.LeftSplitterValue = this._LeftPanelSplitter.Value;
            e.CorrectValue = this._PanelLayout.LeftSplitterValue;
            this.CalculateLayout();
            if (e.IsChangeValue)
                this.ConfigSaveDeffered();
        }
        /// <summary>
        /// Po změně pozice splitteru Right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterR_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            if (this.IsSuppressedEvent) return;
            this._PanelLayout.RightSplitterValue = this._RightPanelSplitter.Value;
            e.CorrectValue = this._PanelLayout.RightSplitterValue;
            this.CalculateLayout();
            if (e.IsChangeValue)
                this.ConfigSaveDeffered();
        }
        /// <summary>
        /// Po změně pozice splitteru Bottom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterB_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            if (this.IsSuppressedEvent) return;
            this._PanelLayout.BottomSplitterValue = this._BottomPanelSplitter.Value;
            e.CorrectValue = this._PanelLayout.BottomSplitterValue;
            this.CalculateLayout();
            if (e.IsChangeValue)
                this.ConfigSaveDeffered();
        }
        /// <summary>
        /// Po změně IsCollapsed na některém z panelů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabContainers_IsCollapsedChanged(object sender, GPropertyChangeArgs<bool> e)
        {
            // this._PanelLayout.LeftSplit qqq
            this.CalculateLayout();
            this.Repaint();
            this.ConfigSaveDeffered();
        }
        /// <summary>
        /// Vypočte souřadnice pro všechny své controly
        /// </summary>
        private void CalculateLayout()
        {
            if (this.IsSuppressedEvent) return;
            if (this._BottomPanelTabs == null) return;       // Před dokončením inicializace

            this._PanelLayout.CurrentControlSize = this.ClientSize;
            if (!this._PanelLayout.IsCurrentSizeValid) return;

            using (this.SuppressEvents())
            {
                // Převezmu hodnoty z bočních panelů (počet jejich tabulek, jejich stav IsCollapsed) a vložím je do _PanelLayout, který z nich počítá vnitřní souřadnice prvků:
                this._PanelLayout.CurrentLeftPanelFixedSize = this._LeftPanelFixedSize;
                this._PanelLayout.CurrentRightPanelFixedSize = this._RightPanelFixedSize;
                this._PanelLayout.CurrentBottomPanelFixedSize = this._BottomPanelFixedSize;

                bool isChanged = false;
                this._SetBounds(this._LeftPanelTabs, this._PanelLayout.CurrentLeftPanelBounds, ref isChanged);
                this._SetBounds(this._LeftPanelSplitter, this._PanelLayout.CurrentLeftSplitterValue, this._PanelLayout.CurrentLeftSplitterBounds, ref isChanged);
                this._SetBounds(this._MainPanelGrid, this._PanelLayout.CurrentMainGridBounds, ref isChanged);
                this._SetBounds(this._RightPanelSplitter, this._PanelLayout.CurrentRightSplitterValue, this._PanelLayout.CurrentRightSplitterBounds, ref isChanged);
                this._SetBounds(this._RightPanelTabs, this._PanelLayout.CurrentRightPanelBounds, ref isChanged);
                this._SetBounds(this._BottomPanelSplitter, this._PanelLayout.CurrentBottomSplitterValue, this._PanelLayout.CurrentBottomSplitterBounds, ref isChanged);
                this._SetBounds(this._BottomPanelTabs, this._PanelLayout.CurrentBottomPanelBounds, ref isChanged);

                if (isChanged)
                    this._IsChildValid = false;
            }
        }
        /// <summary>
        /// Fixní velikost panelu Left.
        /// Pokud má hodnotu, pak tento panel nevyužívá Splitter.
        /// Může mít hodnotu 0 = pak je panel neviditelný, nebo hodnotu kladnou = pak je panel Collapsed (a zobrazuje se jen jeho Header).
        /// Panel je neviditelný tehdy, když velikost celého controlu je menší než potřebné minimu, anebo pokud panel neobsahuje žádnou stránku.
        /// Pokud je FixedSize = null, pak má je panel zobrazen, má zobrazen i Splitter, a aktuální velikost panelu je dána pozici splitteru.
        /// <para/>
        /// Aby hodnota FixedSize byla platná, musí <see cref="_PanelLayout"/> mít zadanou aktuální velikost v <see cref="SchedulerPanelLayout.CurrentControlSize"/>.
        /// </summary>
        private int? _LeftPanelFixedSize { get { return _GetTabFixedSize(this._PanelLayout.IsLeftTabsEnabled, this._LeftPanelTabs, Orientation.Horizontal); } }
        /// <summary>
        /// Fixní velikost panelu Right.
        /// Pokud má hodnotu, pak tento panel nevyužívá Splitter.
        /// Může mít hodnotu 0 = pak je panel neviditelný, nebo hodnotu kladnou = pak je panel Collapsed (a zobrazuje se jen jeho Header).
        /// Panel je neviditelný tehdy, když velikost celého controlu je menší než potřebné minimu, anebo pokud panel neobsahuje žádnou stránku.
        /// Pokud je FixedSize = null, pak má je panel zobrazen, má zobrazen i Splitter, a aktuální velikost panelu je dána pozici splitteru.
        /// <para/>
        /// Aby hodnota FixedSize byla platná, musí <see cref="_PanelLayout"/> mít zadanou aktuální velikost v <see cref="SchedulerPanelLayout.CurrentControlSize"/>.
        /// </summary>
        private int? _RightPanelFixedSize { get { return _GetTabFixedSize(this._PanelLayout.IsRightTabsEnabled, this._RightPanelTabs, Orientation.Horizontal); } }
        /// <summary>
        /// Fixní velikost panelu Bottom.
        /// Pokud má hodnotu, pak tento panel nevyužívá Splitter.
        /// Může mít hodnotu 0 = pak je panel neviditelný, nebo hodnotu kladnou = pak je panel Collapsed (a zobrazuje se jen jeho Header).
        /// Panel je neviditelný tehdy, když velikost celého controlu je menší než potřebné minimu, anebo pokud panel neobsahuje žádnou stránku.
        /// Pokud je FixedSize = null, pak má je panel zobrazen, má zobrazen i Splitter, a aktuální velikost panelu je dána pozici splitteru.
        /// <para/>
        /// Aby hodnota FixedSize byla platná, musí <see cref="_PanelLayout"/> mít zadanou aktuální velikost v <see cref="SchedulerPanelLayout.CurrentControlSize"/>.
        /// </summary>
        private int? _BottomPanelFixedSize { get { return _GetTabFixedSize(this._PanelLayout.IsBottomTabsEnabled, this._BottomPanelTabs, Orientation.Vertical); } }
        /// <summary>
        /// Vrátí velikost TabContaineru
        /// </summary>
        /// <param name="isEnabled">Je povolen podle prostoru v Containeru?</param>
        /// <param name="tabContainer">TabContainer</param>
        /// <param name="orientation">Orientace TABu pro situaci IsCollapsed</param>
        /// <returns></returns>
        private static int? _GetTabFixedSize(bool isEnabled, TabContainer tabContainer, Orientation orientation)
        {
            if (!isEnabled) return 0;                      // Pokud TAB není Enabled = není dostatek prostoru => Size = 0 = neviditelný
            if (tabContainer.TabCount == 0) return 0;      // Pokud TAB nemá co nabídnout = je prázdný => Size = 0 = neviditelný
            if (!tabContainer.IsCollapsed) return null;    // Pokud TAB není IsCollapsed = je normálně viditelný => Size = null = podle Splitteru
            Rectangle bounds = tabContainer.Bounds;
            return (orientation == Orientation.Horizontal ? bounds.Width : bounds.Height);    // TAB je Collapsed => jeho Size = (šířka nebo výška)
        }
        /// <summary>
        /// Vloží dané souřadnice do daného controlu, nebo nastaví jeho Visible. Aktualizuje příznak změny.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="bounds"></param>
        /// <param name="isChanged"></param>
        private void _SetBounds(InteractiveObject control, Rectangle? bounds, ref bool isChanged)
        {
            if (bounds.HasValue)
            {
                if (!control.Is.Visible)
                {
                    control.Is.Visible = true;
                    isChanged = true;
                }
                if (control.Bounds != bounds.Value)
                {
                    control.Bounds = bounds.Value;
                    isChanged = true;
                }
            }
            else
            {
                if (control.Is.Visible)
                {
                    control.Is.Visible = false;
                    isChanged = true;
                }
            }
        }
        /// <summary>
        /// Vloží dané souřadnice do daného controlu, nebo nastaví jeho Visible. Řeší i Value splitteru. Aktualizuje příznak změny.
        /// </summary>
        /// <param name="splitter"></param>
        /// <param name="value"></param>
        /// <param name="bounds"></param>
        /// <param name="isChanged"></param>
        private void _SetBounds(Components.Splitter splitter, int? value, Rectangle? bounds, ref bool isChanged)
        {
            if (value.HasValue && bounds.HasValue)
            {
                if (!splitter.Is.Visible)
                {
                    splitter.Is.Visible = true;
                    isChanged = true;
                }
                if (splitter.Bounds != bounds.Value)
                {
                    splitter.Bounds = bounds.Value;
                    isChanged = true;
                }
                if (splitter.Value != value.Value)
                {
                    splitter.Value = value.Value;
                    isChanged = true;
                }
            }
            else
            {
                if (splitter.Is.Visible)
                {
                    splitter.Is.Visible = false;
                    isChanged = true;
                }
            }
        }
        /// <summary>
        /// Controler pro tvorbu Layoutu panelu
        /// </summary>
        private SchedulerPanelLayout _PanelLayout;

        private TabContainer _LeftPanelTabs;
        private Components.Splitter _LeftPanelSplitter;
        private GGrid _MainPanelGrid;
        private Components.Splitter _RightPanelSplitter;
        private TabContainer _RightPanelTabs;
        private Components.Splitter _BottomPanelSplitter;
        private TabContainer _BottomPanelTabs;

        private MainControl _MainControl;
        private GuiPage _GuiPage;
        /// <summary>
        /// MainData
        /// </summary>
        protected MainData MainData { get { return _MainControl?.MainData; } }
        /// <summary>
        /// IMainDataInternal
        /// </summary>
        protected IMainDataInternal IMainData { get { return MainData as IMainDataInternal; } }
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        protected SchedulerConfig Config { get { return this._MainControl?.MainData?.Config; } }
        /// <summary>
        /// Zajistí uložení konfigurace. Ne hned, provede se za 30 sekund od prvního požadavku.
        /// </summary>
        protected void ConfigSaveDeffered()
        {
            if (!this.MainData.ConfigActive) return;
            SchedulerConfig config = this.Config;
            if (config == null) return;
            config.Save(TimeSpan.FromSeconds(30d));
        }
        #endregion
        #region Načítání dat jednotlivých tabulek
        /// <summary>
        /// Metoda zajistí, že veškeré údaje dodané v <see cref="GuiPage"/> pro tuto stránku budou načteny a budou z nich vytvořeny příslušné tabulky.
        /// </summary>
        internal void LoadData()
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerPanel", "LoadData", ""))
            {
                this._GGridList = new List<GGrid>();
                this._DataTableList = new List<MainDataTable>();
                GuiPage guiPage = this._GuiPage;
                if (guiPage != null)
                {
                    this._LoadTables(guiPage.GraphItemTextTables, ref this._TableTextList);
                    this._LoadTables(guiPage.GraphItemToolTipTables, ref this._TableToolTipList);
                    this._LoadDataToTabs(guiPage.LeftPanel, this._LeftPanelTabs);
                    this._LoadDataToGrid(guiPage.MainPanel, this._MainPanelGrid);
                    this._LoadDataToTabs(guiPage.RightPanel, this._RightPanelTabs);
                    this._LoadDataToTabs(guiPage.BottomPanel, this._BottomPanelTabs);

                    this.ConnectConfigLayout(guiPage);
                }
            }
            this.ConnectGridEvents();
        }
        /// <summary>
        /// Aplikuje data z Configu do this panelu
        /// </summary>
        internal void ApplyConfig()
        {
            GuiPage guiPage = this._GuiPage;
            if (guiPage is null) return;

            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerPanel", "AplyConfig", ""))
            {
                this.ConnectConfigLayout(guiPage);
            }
        }
        /// <summary>
        /// Napojí zdejší Layout do/z Configu, protože tak se bude ukládat a načítat rozložení stránky.
        /// Je vyvoláno na konci načítání dat, po prvním zobrazení controlu, po aktivaco Configu.
        /// POkud MainControl nemá aktivní Config, pak tato metoda nic nedělá.
        /// </summary>
        /// <param name="guiPage"></param>
        protected void ConnectConfigLayout(GuiPage guiPage)
        {
            bool isConfigActive = (this.MainControl?.ConfigActive ?? false);
            if (!isConfigActive) return;

            SchedulerConfig config = this.Config;
            if (config == null) return;

            // Máme načtená data z GUI, zkusíme najít SchedulerPanelLayout našeho jména v Configu, anebo tam náš přidáme:
            string name = guiPage.Name;
            if (String.IsNullOrEmpty(name)) name = guiPage.Title;
            SchedulerPanelLayout panelLayout = config.UserConfigSearch<SchedulerPanelLayout>(l => String.Equals(l.Name, name)).FirstOrDefault();
            if (panelLayout != null)
            {   // Máme data nalezená z konfigurace => použijeme je:
                panelLayout.CurrentControlSize = this.ClientSize;
                this._PanelLayout = panelLayout;
                // Podle konfigurace nastavíme velikosti bočních panelů:
                this.CalculateLayout();
            }
            else
            {   // V konfiguraci ještě náš PanelLayout není => přidáme tam zdejší:
                panelLayout = this._PanelLayout;
                panelLayout.Name = name;
                config.UserConfig.Add(panelLayout);
            }

            // Načteme Layouty sloupců pro jednotlivé GGridy:
            foreach (GGrid gGrid in this._GGridList)
            {
                if (!String.IsNullOrEmpty(gGrid.Name))
                {
                    string layout = this._PanelLayout.GridColumns[gGrid.Name];
                    gGrid.ColumnLayout = layout;
                }
            }
        }
        /// <summary>
        /// Souhrn všech tabulek této stránky, bez ohledu na to ve kterém panelu se nacházejí
        /// </summary>
        public IEnumerable<MainDataTable> DataTables { get { return this._DataTableList; } }
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

            gGrid.Name = guiPanel.FullName + "\\" + _GRID_MAIN_NAME; // Fullname gridu slouží jako ID do konfigurace pro data o layoutu sloupců v gridu
            this._GGridList.Add(gGrid);                              // Toto je seznam GRIDŮ. A v této metodě se pracuje jen s jedním gridem.

            foreach (GuiGrid guiGrid in guiPanel.Grids)
            {
                MainDataTable mainDataTable = this._LoadDataToMainTable(gGrid, guiGrid);
                if (mainDataTable == null) continue;
            }
            return true;
        }
        /// <summary>
        /// Metoda načte všechny tabulky typu <see cref="GuiGrid"/> z dodaného <see cref="GuiPanel"/> a vloží je jako nové Taby do dodaného vizuálního objektu <see cref="TabContainer"/>.
        /// Současně je ukládá do <see cref="_DataTableList"/>.
        /// </summary>
        /// <param name="guiPanel"></param>
        /// <param name="tabs"></param>
        /// <returns></returns>
        private bool _LoadDataToTabs(GuiPanel guiPanel, TabContainer tabs)
        {
            if (guiPanel == null || guiPanel.Grids.Count == 0) return false;

            foreach (GuiGrid guiGrid in guiPanel.Grids)
            {
                GGrid gGrid = new GGrid();
                gGrid.SynchronizedTime = this.SynchronizedTime;
                gGrid.Name = guiGrid.FullName;                       // Fullname gridu slouží jako ID do konfigurace pro data o layoutu sloupců v gridu

                MainDataTable mainDataTable = this._LoadDataToMainTable(gGrid, guiGrid);
                if (mainDataTable == null) continue;

                tabs.AddTabItem(gGrid, guiGrid.Title, guiGrid.ToolTip);

                this._GGridList.Add(gGrid);                          // Toto je seznam GRIDŮ. A v této metodě se pracuje více gridy - jedna smyčka = jeden grid
            }
            return true;
        }
        /// <summary>
        /// Vloží eventhandlery do všech grafických komponent <see cref="GGrid"/> uvedených v <see cref="_GGridList"/>.
        /// </summary>
        private void ConnectGridEvents()
        {
            List<GGrid> gridList = this._GGridList;
            foreach (GGrid grid in gridList)
            {
                grid.ColumnWidthChanged += GGrid_ColumnLayoutIntChanged;
                grid.ColumnOrderChanged += GGrid_ColumnLayoutIntChanged;
                grid.ColumnVisibleChanged += GGrid_ColumnLayoutBooleanChanged;
            }
        }
        /// <summary>
        /// Eventhandler události, kdy grafický <see cref="GGrid"/> provedl změnu šířky nebo pořadí sloupce (hodnota Int32)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GGrid_ColumnLayoutIntChanged(object sender, GObjectPropertyChangeArgs<GridColumn, int> e)
        {
            this._SaveGridLayout(e.CurrentObject?.Grid);
        }
        /// <summary>
        /// Eventhandler události, kdy grafický <see cref="GGrid"/> provedl změnu viditelnosti sloupce (hodnota Boolean)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GGrid_ColumnLayoutBooleanChanged(object sender, GObjectPropertyChangeArgs<GridColumn, bool> e)
        {
            this._SaveGridLayout(e.CurrentObject?.Grid);
        }
        /// <summary>
        /// Metoda zajistí uložení layoutu jednoho daného GGridu do konfigurace <see cref="_PanelLayout"/> a návazně do souboru .config
        /// </summary>
        /// <param name="gGrid"></param>
        private void _SaveGridLayout(GGrid gGrid)
        {
            bool isConfigActive = (this.MainControl?.ConfigActive ?? false);
            if (isConfigActive && gGrid != null && !String.IsNullOrEmpty(gGrid.Name))
            {
                this._PanelLayout.GridColumns[gGrid.Name] = gGrid.ColumnLayout;
                this.ConfigSaveDeffered();
            }
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
            this.IMainData.Colorize(guiGrid);
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerPanel", "LoadDataToMainTable", "", guiGrid.FullName))
                mainDataTable = new MainDataTable(this, gGrid, guiGrid);

            if (mainDataTable.TableRow == null) return null;

            this._DataTableList.Add(mainDataTable);
            mainDataTable.AddTableToGrid(gGrid);

            return mainDataTable;
        }
        /// <summary>
        /// Souhrn všech tabulek této stránky, bez ohledu na to ve kterém panelu se nacházejí
        /// </summary>
        private List<MainDataTable> _DataTableList;
        /// <summary>
        /// Souhrn všech grafických gridů této stránky, bez ohledu na to ve kterém panelu se nacházejí
        /// </summary>
        private List<GGrid> _GGridList;
        private const string _GRID_MAIN_NAME = "MainGrid";
        #endregion
        #region Společné textové tabulky
        /// <summary>
        /// Načte textová data (texty nebo tooltipy) z dodaných GUI tabulek do datových tabulek do this.
        /// </summary>
        /// <param name="guiTables"></param>
        /// <param name="dataTables"></param>
        private void _LoadTables(List<GuiDataTable> guiTables, ref List<Table> dataTables)
        {
            dataTables = null;
            if (guiTables == null) return;
            dataTables = new List<Table>();
            foreach (GuiDataTable guiTable in guiTables)
            {
                Table dataTable = Table.CreateFrom(guiTable);
                if (dataTable == null) continue;
                dataTable.ReIndex();
                dataTables.Add(dataTable);
            }
        }
        /// <summary>
        /// Tabulky, které mohou obsahovat Texty pro prvky grafů
        /// </summary>
        internal List<Table> TableTextList { get { return this._TableTextList; } }
        /// <summary>
        /// Tabulky, které mohou obsahovat ToolTipy pro prvky grafů
        /// </summary>
        internal List<Table> TableToolTipList { get { return this._TableToolTipList; } }
        /// <summary> Tabulky, které mohou obsahovat Texty pro prvky grafů </summary>
        private List<Table> _TableTextList;
        /// <summary> Tabulky, které mohou obsahovat ToolTipy pro prvky grafů </summary>
        private List<Table> _TableToolTipList;
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
                this._ChildList.AddItems(this._LeftPanelTabs, this._MainPanelGrid, this._RightPanelTabs, this._BottomPanelTabs, this._LeftPanelSplitter, this._RightPanelSplitter, this._BottomPanelSplitter);
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
        /// Levý panel záložek
        /// </summary>
        public TabContainer LeftPanelTabs { get { return this._LeftPanelTabs; } }
        /// <summary>
        /// Hlavní Grid
        /// </summary>
        public GGrid MainPanelGrid { get { return this._MainPanelGrid; } }
        /// <summary>
        /// Pravý panel záložek
        /// </summary>
        public TabContainer RightPanelTabs { get { return this._RightPanelTabs; } }
        /// <summary>
        /// Dolní panel záložek
        /// </summary>
        public TabContainer BottomPanelTabs { get { return this._BottomPanelTabs; } }
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
        /// Synchronizační element časové osy
        /// </summary>
        public ValueTimeRangeSynchronizer SynchronizedTime { get { return this._MainControl.SynchronizedTime; } }
        #endregion
    }
    #region SchedulerPanelLayout : controller pro Layout jednoho panelu SchedulerPanel
    /// <summary>
    /// SchedulerPanelLayout : controller pro Layout jednoho panelu <see cref="SchedulerPanel"/>,
    /// a dále slouží k jeho ukládání/načítání do/z Configu a k jeho reaktivaci.
    /// </summary>
    public class SchedulerPanelLayout
    {
        #region Public data persistovaná
        /// <summary>
        /// Jméno layoutu v uložené konfiguraci (pro persistenci)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Index aktuální záložky v levém panelu, počítáné od 0. Záporná hodnota = panel je minimalizován.
        /// </summary>
        public int LeftTabIndex { get { return this._LeftTabIndex; } set { this._LeftTabIndex = value; } } private int _LeftTabIndex = 0;
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
        /// Index aktuální záložky v pravém panelu, počítáné od 0. Záporná hodnota = panel je minimalizován.
        /// </summary>
        public int RightTabIndex { get { return this._RightTabIndex; } set { this._RightTabIndex = value; } } private int _RightTabIndex = 0;
        /// <summary>
        /// Pozice dolního splitteru, měřeno odspodu, pokud je viditelný
        /// </summary>
        public int BottomSplit { get { return this._BottomSplit; } set { this._BottomSplit = _AlignH(value); } } private int _BottomSplit = DefHSplit;
        /// <summary>
        /// Index aktuální záložky v pravém panelu, počítáné od 0. Záporná hodnota = panel je minimalizován.
        /// </summary>
        public int BottomTabIndex { get { return this._BottomTabIndex; } set { this._BottomTabIndex = value; } } private int _BottomTabIndex = 0;
        /// <summary>
        /// Viditelná šířka Splitteru
        /// </summary>
        public int SplitterSize { get { return this._SplitterSize; } set { this._SplitterSize = _Align(value, 1, 6); } } private int _SplitterSize = 4;
        /// <summary>
        /// Layouty pro jednotlivé Gridy (=sloupce: jejich pozice, šířka, viditelnost)
        /// </summary>
        public KeyValueArray<string, string> GridColumns { get { if (this._GridColumns == null) this._GridColumns = new KeyValueArray<string, string>(); return this._GridColumns; } set { this._GridColumns = value; } } private KeyValueArray<string, string> _GridColumns;
        #endregion
        #region Konstanty
        /// <summary>
        /// Minimální šířka controlu potřebná pro to, aby bylo možno zobrazit postranní panely
        /// </summary>
        public const int MinControlWidthForSideGrids = 300;
        /// <summary>
        /// Minimální výška controlu potřebná pro to, aby bylo možno zobrazit dolní panel
        /// </summary>
        public const int MinControlHeightForSideGrids = 200;
        #endregion
        #region Align: Zarovnání hodnoty
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
        private const int DefVSplit = 235;
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
        private const int DefHSplit = 180;
        #endregion
        #region Výpočty Current souřadnic, non persisted
        /// <summary>
        /// Aktuální velikost plochy Controlu
        /// </summary>
        [PersistingEnabled(false)]
        public Size CurrentControlSize { get; set; }
        /// <summary>
        /// Aktuální velikost panelu Left.
        /// Pokud má hodnotu, pak jde o fixní hodnotu a není zobrazován Splitter. 
        /// Může mít hodnotu 0 = pak je panel Invisible.
        /// Pokud nemá hodnotu, pak je panel vidět a jeho velikost řídí Splitter, který je rovněž vidět.
        /// </summary>
        [PersistingEnabled(false)]
        public int? CurrentLeftPanelFixedSize { get; set; }
        /// <summary>
        /// Aktuální velikost panelu Right.
        /// Pokud má hodnotu, pak jde o fixní hodnotu a není zobrazován Splitter. 
        /// Může mít hodnotu 0 = pak je panel Invisible.
        /// Pokud nemá hodnotu, pak je panel vidět a jeho velikost řídí Splitter, který je rovněž vidět.
        /// </summary>
        [PersistingEnabled(false)]
        public int? CurrentRightPanelFixedSize { get; set; }
        /// <summary>
        /// Aktuální velikost panelu Bottom.
        /// Pokud má hodnotu, pak jde o fixní hodnotu a není zobrazován Splitter. 
        /// Může mít hodnotu 0 = pak je panel Invisible.
        /// Pokud nemá hodnotu, pak je panel vidět a jeho velikost řídí Splitter, který je rovněž vidět.
        /// </summary>
        [PersistingEnabled(false)]
        public int? CurrentBottomPanelFixedSize { get; set; }
        /// <summary>
        /// Aktuální Width
        /// </summary>
        protected int CurrentWidth { get { return this.CurrentControlSize.Width; } }
        /// <summary>
        /// Aktuální Height
        /// </summary>
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
        [PersistingEnabled(false)]
        public int LeftSplitterValue { get { return this.LeftSplit; } set { this.LeftSplit = value; } }
        /// <summary>
        /// Value pro RightSplitter.
        /// Tato hodnota se opírá o hodnotu <see cref="CurrentControlSize"/>, protože je měřena vzhledem k Width.
        /// </summary>
        [PersistingEnabled(false)]
        public int RightSplitterValue { get { return this.CurrentWidth - this.RightSplit; } set { this.RightSplit = this.CurrentWidth - value; } }
        /// <summary>
        /// Value pro BottomSplitter.
        /// Tato hodnota se opírá o hodnotu <see cref="CurrentControlSize"/>, protože je měřena vzhledem k Height.
        /// </summary>
        [PersistingEnabled(false)]
        public int BottomSplitterValue { get { return this.CurrentHeight - this.BottomSplit; } set { this.BottomSplit = this.CurrentHeight - value; } }
        /// <summary>
        /// Obsahuje souřadnice panelu Left. Obsahuje null, pokud má být Invisible.
        /// </summary>
        public Rectangle? CurrentLeftPanelBounds
        {
            get
            {
                int? panelSize = this.CurrentLeftPanelFixedSize;
                if (panelSize.HasValue && panelSize.Value <= 0) return null;                                 // Velikost Panelu = 0 => panel je Invisible.
                int width = (panelSize.HasValue ? panelSize.Value : (this.LeftSplit - this.SplitterSize1));  // Šířka LeftPanelu: fixní nebo odvozená od LeftSplitteru
                int height = this.CurrentMainHeight;
                return new Rectangle(0, 0, width, height);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice SplitterLeft. Obsahuje null, pokud má být Invisible. 
        /// </summary>
        public int? CurrentLeftSplitterValue
        {
            get
            {
                return (!this.CurrentLeftPanelFixedSize.HasValue ? (int?)this.LeftSplitterValue : (int?)null);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice SplitterLeft. Obsahuje null, pokud má být Invisible. 
        /// </summary>
        public Rectangle? CurrentLeftSplitterBounds
        {
            get
            {
                int? leftSize = this.CurrentLeftPanelFixedSize;
                if (leftSize.HasValue) return null;                                                        // Velikost Panelu má hodnotu => Splitter je Invisible.
                int left = this.LeftSplitterValue - this.SplitterSize1;
                int width = this.SplitterSize;
                int height = this.CurrentMainHeight;
                return new Rectangle(left, 0, width, height);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice gridu Main. Obsahuje null, pokud má být Invisible. Prakticky nikdy není null.
        /// </summary>
        public Rectangle? CurrentMainGridBounds
        {
            get
            {
                int? leftSize = this.CurrentLeftPanelFixedSize;
                int left = (leftSize.HasValue ? leftSize.Value : (this.LeftSplit + this.SplitterSize2));     // Velikost prostoru Main:Left
                int? rightSize = this.CurrentRightPanelFixedSize;
                int right = (rightSize.HasValue ? rightSize.Value : (this.RightSplit + this.SplitterSize1)); // Velikost prostoru Main:Right
                int width = this.CurrentWidth - right - left;
                int height = this.CurrentMainHeight;
                return new Rectangle(left, 0, width, height);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice SplitterRight. Obsahuje null, pokud má být Invisible. 
        /// </summary>
        public int? CurrentRightSplitterValue
        {
            get
            {
                return (!this.CurrentRightPanelFixedSize.HasValue ? (int?)this.RightSplitterValue : (int?)null);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice SplitterRight. Obsahuje null, pokud má být Invisible. 
        /// </summary>
        public Rectangle? CurrentRightSplitterBounds
        {
            get
            {
                int? rightSize = this.CurrentRightPanelFixedSize;
                if (rightSize.HasValue) return null;                                                         // Velikost Panelu má hodnotu => Splitter je Invisible.
                int left = this.RightSplitterValue - this.SplitterSize1;
                int width = this.SplitterSize;
                int height = this.CurrentMainHeight;
                return new Rectangle(left, 0, width, height);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice panelu Right. Obsahuje null, pokud má být Invisible.
        /// </summary>
        public Rectangle? CurrentRightPanelBounds
        {
            get
            {
                int? panelSize = this.CurrentRightPanelFixedSize;
                if (panelSize.HasValue && panelSize.Value <= 0) return null;                                 // Velikost Panelu = 0 => panel je Invisible.
                int width = (panelSize.HasValue ? panelSize.Value : (this.RightSplit - this.SplitterSize2)); // Šířka RightPanelu: fixní nebo odvozená od RightSplitteru
                int left = this.CurrentWidth - width;
                int height = this.CurrentMainHeight;
                return new Rectangle(left, 0, width, height);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice SplitterBottom. Obsahuje null, pokud má být Invisible. 
        /// </summary>
        public int? CurrentBottomSplitterValue
        {
            get
            {
                return (!this.CurrentBottomPanelFixedSize.HasValue ? (int?)this.BottomSplitterValue : (int?)null);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice SplitterBottom. Obsahuje null, pokud má být Invisible. 
        /// </summary>
        public Rectangle? CurrentBottomSplitterBounds
        {
            get
            {
                int? panelSize = this.CurrentBottomPanelFixedSize;
                if (panelSize.HasValue) return null;                                                         // Velikost Panelu má hodnotu => Splitter je Invisible.
                int top = this.BottomSplitterValue - this.SplitterSize1;
                int height = this.SplitterSize;
                int width = this.CurrentWidth;
                return new Rectangle(0, top, width, height);
            }
        }
        /// <summary>
        /// Obsahuje souřadnice panelu Bottom. Obsahuje null, pokud má být Invisible.
        /// </summary>
        public Rectangle? CurrentBottomPanelBounds
        {
            get
            {
                int? panelSize = this.CurrentBottomPanelFixedSize;
                if (panelSize.HasValue && panelSize.Value <= 0) return null;                                 // Velikost Panelu = 0 => panel je Invisible.
                int height = (panelSize.HasValue ? panelSize.Value : (this.BottomSplit - this.SplitterSize2)); // Výška BottomPanelu: fixní nebo odvozená od BottomSplitteru
                int top = this.CurrentHeight - height;
                int width = this.CurrentWidth;
                return new Rectangle(0, top, width, height);
            }
        }
        /// <summary>
        /// Výška všech prvků v části Main : LeftPanel, LeftSplitter, MainGrid, RightSplitter, RightPanel.
        /// Je to prostor výšky: buď celý dle <see cref="CurrentControlSize"/>, nebo část tohoto prostoru nad Bottom splitterem nebo nad Bottom panelem.
        /// </summary>
        protected int CurrentMainHeight
        {
            get
            {
                int height = this.CurrentControlSize.Height;
                if (this.IsCurrentSizeValid)
                {
                    int? bSize = this.CurrentBottomPanelFixedSize;
                    if (bSize.HasValue)
                    {   // Dolní panel má danou velikost: pak nemá zobrazen Splitter
                        if (bSize.Value > 0)
                            height -= bSize.Value;
                    }
                    else
                    {   // Dolní panel nemá stanovenou velikost: pak je vidět dolní Splitter, a MainHeight končí nad ním:
                        height -= (this.SplitterSize1 + this.BottomSplit);
                    }
                }
                return height;
            }
        }
        /// <summary>
        /// Počet pixelů před pozicí Splitteru, které jsou pro Splitter rezervovány
        /// </summary>
        protected int SplitterSize1 { get { return this.SplitterSize / 2; } }
        /// <summary>
        /// Počet pixelů za pozicí Splitteru, které jsou pro Splitter rezervovány
        /// </summary>
        protected int SplitterSize2 { get { return this.SplitterSize - this.SplitterSize1; } }
        #endregion
    }
    #endregion
}
