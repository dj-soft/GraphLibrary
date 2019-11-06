using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Components.Graph;
using Noris.LCS.Base.WorkScheduler;
using Asol.Tools.WorkScheduler.Components.Grid;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// MainDataTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, grafy, položky grafů, vztahy mezi položkami grafů a popisky položek grafů.
    /// Tvoří základ pro jeden vizuální objekt <see cref="GTable"/>.
    /// </summary>
    public class MainDataTable : IMainDataTableInternal, ITimeGraphDataSource, ITimeGraphLinkDataSource
    {
        #region Konstrukce, základní property
        /// <summary>
        /// Konstruktor, automaticky provede načtení dat z dat guiGrid
        /// </summary>
        /// <param name="panel">Vizuální panel, v němž bude this table umístěna</param>
        /// <param name="gGrid">Vizuální grid, v němž bude this table umístěna</param>
        /// <param name="guiGrid">Data pro tuto tabulku</param>
        public MainDataTable(SchedulerPanel panel, GGrid gGrid, GuiGrid guiGrid)
        {
            this.Panel = panel;
            this.GGrid = gGrid;
            this.GuiGrid = guiGrid;
            this.LoadData();
        }
        /// <summary>
        /// Vlastník tabulky = SchedulerPanel, jeden z několika (možných) panelů v controlu
        /// </summary>
        internal SchedulerPanel Panel { get; private set; }
        /// <summary>
        /// Vizuální Grid, v němž bude umístěna tabulka řádků <see cref="TableRow"/>.
        /// </summary>
        internal GGrid GGrid { get; private set; }
        /// <summary>
        /// Main control = celý vizuální control se všemi prvky (Toolbar, panely) = instance třídy <see cref="Scheduler.MainControl"/>
        /// </summary>
        internal MainControl MainControl { get { return this.Panel.MainControl; } }
        /// <summary>
        /// Main data = celý datový objekt Scheduleru. instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        internal MainData MainData { get { return this.MainControl.MainData; } }
        /// <summary>
        /// Obsahuje true, pokud máme instanci <see cref="MainData"/>
        /// </summary>
        internal bool HasMainData { get { return this.MainData != null; } }
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        public SchedulerConfig Config { get { return (this.HasMainData ? this.MainData.Config : null); ; } }
        /// <summary>
        /// Instance <see cref="GuiGrid"/>, která tvoří datový základ této tabulky
        /// </summary>
        internal GuiGrid GuiGrid { get; private set; }
        /// <summary>
        /// Plné jméno this tabulky, pochází z <see cref="GuiGrid"/>.FullName.
        /// Slouží k vyhledání konkrétní tabulky podle jejího názvu, a k pojmenování konkrétních prvků z této tabulky odesílaných dále.
        /// </summary>
        internal string TableName { get; private set; }
        /// <summary>
        /// Vlastník přetypovaný na IMainDataInternal
        /// </summary>
        protected IMainDataInternal IMainData { get { return (this.MainData as IMainDataInternal); } }
        #endregion
        #region Načtení dat z GuiGrid
        /// <summary>
        /// Metoda zajistí, že veškeré údaje dodané v <see cref="GuiGrid"/> pro tuto tabulku budou načteny a budou z nich vytvořeny příslušné prvky.
        /// </summary>
        protected void LoadData()
        {
            this.LoadRows();
            this.LoadGraphs();
            this.LoadLinks();
            this.LoadTexts();
            this.LoadPaint();
        }
        /// <summary>
        /// Metoda zajistí přípravu dat této tabulky poté, kdy projdou přípravou všechny tabulky systému.
        /// V této metodě se tedy this tabulka může datové základny dotazovat i na jiné tabulky.
        /// V rámci konstruktoru a při načítání dat to není možné.
        /// </summary>
        internal void PrepareAfterLoad()
        {
            this.PrepareDynamicChilds(this.MainData.GuiData.Properties?.InitialTimeRange, true);
        }
        #endregion
        #region Tabulka s řádky (TableRow, GTableRow) : tvorba, naplnění daty, navázání eventhandlerů a jejich obsluha
        /// <summary>
        /// Načte tabulku s řádky <see cref="TableRow"/>: sloupce, řádky, filtr
        /// </summary>
        protected void LoadRows()
        {
            this.TableName = this.GuiGrid.FullName;
            this.TableRow = Table.CreateFrom(this.GuiGrid.RowTable);
            this.TableRow.CalculateBoundsForAllRows = true;
            this.TableRow.OpenRecordForm += _TableRow_OpenRecordForm;
            this.TableRow.KeyboardKeyUp += TableRow_KeyboardKeyUp;
            this.TableRow.ActiveCellRightClick += TableRow_ActiveCellRightClick;
            this.TableRow.UserData = this;
            this._PrepareActiveKeys();
            this._PrepareRowDragMove();
            this._PrepareRowSearchChild();
        }
        /// <summary>
        /// Tabulka s řádky.
        /// Tato tabulka je zobrazována.
        /// </summary>
        public Table TableRow { get; private set; }
        /// <summary>
        /// Grafická komponenta reprezentující data z <see cref="TableRow"/>.
        /// </summary>
        protected GTable GTableRow { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud tato tabulka má aktuálně focus
        /// </summary>
        public bool HasFocus { get { return this.GTableRow.HasFocus; } }
        /// <summary>
        /// Metoda vloží svoji datovou tabulku <see cref="TableRow"/> do předaného grafické komponenty <see cref="GGrid"/>.
        /// Tím vytvoří grafickou komponentu <see cref="GTable"/> (kterou nakonec vrací).
        /// Do této grafické komponenty zaregistruje patřičné eventhandlery.
        /// </summary>
        /// <param name="gGrid"></param>
        /// <returns></returns>
        internal GTable AddTableToGrid(GGrid gGrid)
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainDataTable", "AddTableToGrid", "", this.GuiGrid.FullName))
            {
                this.GTableRow = gGrid.AddTable(this.TableRow);
                this.FillGTableProperties();
                this.FillGTableLinks();
                this.FillGTableEventHandlers();
                return this.GTableRow;
            }
        }
        /// <summary>
        /// Metoda zajistí zrušení všech filtrů na aktuální tabulce
        /// </summary>
        public void ResetAllRowFilters(ref bool callRefresh)
        {
            if (this.GTableRow != null)
                this.GTableRow.ResetAllRowFilters(ref callRefresh);
            this.InteractionRowFilterDict = null;
        }
        /// <summary>
        /// Metoda zajistí zrušení filtrů Interakce na aktuální tabulce
        /// </summary>
        /// <param name="callRefresh"></param>
        public void ResetInteractionFilters(ref bool callRefresh)
        {
            if (this.GTableRow != null)
            {
                if (this.GTableRow.RemoveFilter(InteractionRowFilterName))
                    callRefresh = true;
            }
            this.InteractionRowFilterDict = null;
        }
        /// <summary>
        /// Obsahuje pole identifikátorů řádků, které jsou označeny <see cref="Row.IsChecked"/>
        /// </summary>
        public GuiGridRowId[] CheckedRowsId { get { return (this.GuiGrid.RowTable.RowCheckEnabled ? this.GetRowsId(r => r.IsChecked) : new GuiGridRowId[0]); } }
        /// <summary>
        /// Vrátí pole identifikátorů řádků z this tabulky <see cref="TableRow"/>, kde řádky jsou filtrovány daným filtrem.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        protected GuiGridRowId[] GetRowsId(Func<Row, bool> filter)
        {
            return this.TableRow.Rows.Where(filter).Select(r => GetRowId(r)).ToArray();
        }
        /// <summary>
        /// Vrátí <see cref="GuiGridRowId"/> pro daný řádek, resp. pro jeho <see cref="Row.RecordGId"/>
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected GuiGridRowId GetRowId(Row row)
        {
            return new GuiGridRowId() { RowId = row.RecordGId, TableName = this.TableName };
        }
        /// <summary>
        /// Metoda se pokusí najít řádek podle daného klíče, vrací true pokud je nalezen.
        /// Pokud dojde k chybě (nezadaný ID, neexistující PrimaryKey, více záznamů pro daný klíč), pak vrací false (=řádek nenalezen).
        /// Toto chování lze změnit parametrem checkErrors: false = default = chyby nehlásit, vrátit false; true = chyby hlásit.
        /// </summary>
        /// <param name="rowGId">Identifikátor řádku</param>
        /// <param name="row">Out nalezený řádek</param>
        /// <returns></returns>
        protected bool TryGetRow(GId rowGId, out Row row)
        {
            return this.TableRow.TryGetRow(rowGId, out row);
        }
        /// <summary>
        /// Metoda zajistí převedení konfigurace z <see cref="GuiGrid"/> do <see cref="GTableRow"/>
        /// </summary>
        protected void FillGTableProperties()
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainDataTable", "FillGTableProperties", "", this.GuiGrid.FullName))
            {
                GTable gTableRow = this.GTableRow;
                GuiGridProperties gridProperties = this.GuiGrid.GridProperties;
                gTableRow.TagFilterBackColor = gridProperties.TagFilterBackColor;
                gTableRow.TagFilterEnabled = gridProperties.TagFilterEnabled;
                gTableRow.TagFilterItemHeight = gridProperties.TagFilterItemHeight;
                gTableRow.TagFilterItemMaxCount = gridProperties.TagFilterItemMaxCount;
                gTableRow.TagFilterRoundItemPercent = gridProperties.TagFilterRoundItemPercent;
            }
        }
        /// <summary>
        /// Metoda nastaví režim zobrazování vztahů do grafického controlu <see cref="GTableRow"/> a zajistí načtení linků dle tohoto režimu
        /// </summary>
        protected void FillGTableLinks()
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainDataTable", "FillGTableLinks", "", this.GuiGrid.FullName))
            {
                if (this.GTableRow != null)
                {
                    this.GraphLinkArray.DefaultLinksMode = this.GraphProperties.DefaultLinksMode;
                    this.GraphLinkArray.ReloadLinks();
                }
            }
        }
        /// <summary>
        /// Metoda zajistí navázání eventhandlerů v this třídě do grafické komponenty <see cref="GTable"/> <see cref="GTableRow"/>.
        /// </summary>
        protected void FillGTableEventHandlers()
        {
            Table table = this.TableRow;
            table.ActiveRowChanged += _TableRowActiveRowChanged;
            table.CheckedRowChanged += _TableRowCheckedRowChanged;

            GTable gTable = this.GTableRow;
            gTable.InteractiveStateChange += _GTableInteractiveStateChange;

            gTable.TimeAxisValueChanged += _GTableTimeAxisValueChanged;
        }
        /// <summary>
        /// Eventhandler události "Byl aktivován řádek"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRowActiveRowChanged(object sender, GPropertyChangeArgs<Row> e)
        {
            Row activeRow = e.NewValue;
            Row[] checkedRows = this.TableRow.CheckedRows;
            bool isOnlyActivadedRow = (checkedRows.Length == 0);
            SourceActionType sourceAction = isOnlyActivadedRow ? SourceActionType.TableRowActivatedOnly : SourceActionType.TableRowActivatedWithRowsChecked;
            GridInteractionRunInfo[] runInteractions = this.GetInteractions(sourceAction);
            if (runInteractions == null) return;
            this.InteractionThisSource(runInteractions, activeRow, checkedRows, null, null);
        }
        /// <summary>
        /// Eventhandler události "Byl označen řádek"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRowCheckedRowChanged(object sender, GObjectPropertyChangeArgs<Row, bool> e)
        {
            GridInteractionRunInfo[] runInteractions = this.GetInteractions(SourceActionType.TableRowChecked);
            if (runInteractions == null) return;
            Row checkedRow = e.CurrentObject;
            Row[] checkedRows = this.TableRow.CheckedRows;
            this.InteractionThisSource(runInteractions, checkedRow, checkedRows, null, null);
        }
        /// <summary>
        /// Hlídáme interaktivitu GTable = ukládáme aktivní tabulku do <see cref="IMainDataInternal.ActiveDataTable"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _GTableInteractiveStateChange(object sender, GInteractiveChangeStateArgs e)
        {
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardFocusEnter:
                case GInteractiveChangeState.LeftDown:
                    this.IMainData.ActiveDataTable = this;
                    break;
            }
        }
        /// <summary>
        /// Eventhandler události "Došlo ke změně času na časové ose v naší tabulce, na daném sloupci"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _GTableTimeAxisValueChanged(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            // Pokud současný systém vztahů Parent-Child je závislý na viditelném časovém intervalu, zavoláme jeho refresh:
            if (this.CurrentSearchChildInfo.IsVisibleTimeOnly)
                this.PrepareDynamicChilds(e.NewValue, false);
        }
        #endregion
        #region Grafy a položky grafů
        #region Prvotní tvorba grafů do tabulky, do jejich řádků
        /// <summary>
        /// Do tabulky s řádky vytvoří grafy do všech řádků, zatím prázdné
        /// </summary>
        protected void LoadGraphs()
        {
            this.TimeGraphDict = new Dictionary<GId, GTimeGraph>();
            this.TimeGraphItemDict = new Dictionary<GId, DataGraphItem>();
            this.TimeGraphGroupDict = new DictionaryList<GId, DataGraphItem>(g => g.GroupGId);
            this.TimeIdIndex = new Index<GId>();

            this.PrepareGraphProperties();

            if (this.TableRow == null || this.GraphPosition == DataGraphPositionType.None) return;

            this.PrepareTableForGraphs();
            foreach (Row row in this.TableRow.Rows)
            {
                this.PrepareGraphForRow(row);
            }
        }
        /// <summary>
        /// Načte vlastnosti grafů z <see cref="GuiGraphProperties"/> do <see cref="GraphProperties"/>.
        /// </summary>
        protected void PrepareGraphProperties()
        {
            DataGraphProperties graphProperties = DataGraphProperties.CreateFrom(this, this.MainData.GuiData.Properties, this.GuiGrid.GraphProperties);
            this.GraphProperties = graphProperties;

            DataGraphPositionType graphPosition = (this.TableRow != null ? graphProperties.GraphPosition : DataGraphPositionType.None);
            this.GraphPosition = graphPosition;

            this.TimeAxisMode = (graphPosition == DataGraphPositionType.InLastColumn ? TimeGraphTimeAxisMode.Standard :
                                (graphPosition == DataGraphPositionType.OnBackgroundProportional ? TimeGraphTimeAxisMode.ProportionalScale :
                                (graphPosition == DataGraphPositionType.OnBackgroundLogarithmic ? TimeGraphTimeAxisMode.LogarithmicScale : TimeGraphTimeAxisMode.Default)));

            // this.GraphLinkArray.DefaultLinksMode = graphProperties.DefaultLinksMode;
        }
        /// <summary>
        /// Metoda připraví tabulku <see cref="TableRow"/> na vkládání grafů daného typu (podle pozice grafu <see cref="GraphPosition"/>).
        /// Tzn. v případě, kdy pozice je <see cref="DataGraphPositionType.InLastColumn"/>, tak bude vytvořen a patřičně nastaven nový sloupec pro graf 
        /// (reference na sloupec je uložena do <see cref="GraphColumn"/>),
        /// a do vhodného umístění je vložena instance vlastností grafu <see cref="TimeGraphProperties"/>.
        /// </summary>
        protected void PrepareTableForGraphs()
        {
            bool isGraphInColumn = (this.GraphPosition == DataGraphPositionType.InLastColumn);
            TimeGraphProperties graphProperties = this.GraphProperties.CreateTimeGraphProperties(isGraphInColumn, this.MainControl.SynchronizedTime.Value, this.MainData.GuiData.Properties.TotalTimeRange);
            if (isGraphInColumn)
            {
                Column graphColumn = new Column("__time__graph__");

                graphColumn.AllowColumnResize = true;
                graphColumn.AllowColumnSortByClick = false;
                graphColumn.AutoWidth = true;
                graphColumn.ColumnContent = ColumnContentType.TimeGraphSynchronized;
                graphColumn.IsVisible = true;
                graphColumn.WidthMininum = 250;
                graphColumn.GraphParameters = graphProperties;

                this.TableRow.Columns.Add(graphColumn);
                this.GraphColumn = graphColumn;
            }
            else
            {
                this.TableRow.GraphParameters = graphProperties;
            }
        }
        /// <summary>
        /// Zajistí přípravu grafu pro daný řádek.
        /// Používá se při prvotním načítání i následně při GuiRefresh
        /// </summary>
        /// <param name="row"></param>
        protected void PrepareGraphForRow(Row row)
        {
            if (this.GraphPosition == DataGraphPositionType.None) return;

            GId rowGid = row.RecordGId;
            if (rowGid == null) return;

            GTimeGraph gTimeGraph = this.CreateGraphForRow(row);
            this.TimeGraphDict.AddRefresh(rowGid, gTimeGraph);
        }
        /// <summary>
        /// Zajistí aktualizaci grafu do daného řádku
        /// </summary>
        /// <param name="row"></param>
        protected void UpdateGraphFromRow(Row row)
        {
            if (this.GraphPosition == DataGraphPositionType.None) return;

            GId rowGid = row.RecordGId;
            if (rowGid == null) return;

            GTimeGraph gTimeGraph;
            if (this.TimeGraphDict.TryGetValue(rowGid, out gTimeGraph))
            {
                this.StoreGraphToRow(gTimeGraph, row);
                this.RefreshGraphFromGui(gTimeGraph, row);
            }
            else
            {
                gTimeGraph = this.CreateGraphForRow(row);
                this.TimeGraphDict.AddRefresh(rowGid, gTimeGraph);
            }
        }
        /// <summary>
        /// Metoda vytvoří nový <see cref="GTimeGraph"/> pro daný řádek a pozici, umístí jej do řádku, a graf vrátí.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected GTimeGraph CreateGraphForRow(Row row)
        {
            GTimeGraph gTimeGraph = new GTimeGraph();
            gTimeGraph.GraphId = this.GetId(row.RecordGId);
            gTimeGraph.DataSource = this;

            this.StoreGraphToRow(gTimeGraph, row);
            this.RefreshGraphFromGui(gTimeGraph, row);
           
            return gTimeGraph;
        }
        /// <summary>
        /// Vloží dodaný graf do dodaného řádku, vzájemně je prováže - podle definice <see cref="GraphPosition"/>
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="row"></param>
        protected void StoreGraphToRow(GTimeGraph gTimeGraph, Row row)
        {
            gTimeGraph.UserData = row;

            ITimeInteractiveGraph iTimeGraph = gTimeGraph as ITimeInteractiveGraph;
            if (this.GraphPosition == DataGraphPositionType.InLastColumn)
            {
                iTimeGraph.TimeAxisConvertor = this.GraphColumn.ColumnHeader.TimeConvertor;
                Cell graphCell = row[this.GraphColumn];
                graphCell.Value = gTimeGraph;
            }
            else
            {
                iTimeGraph.TimeAxisConvertor = this.GGrid.SynchronizedTimeConvertor;
                row.BackgroundValue = gTimeGraph;
            }
        }
        /// <summary>
        /// Naplní do dodaného grafu data dodaná z GUI vrstvy, pokud nějaká data dodaná byla
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="row"></param>
        protected void RefreshGraphFromGui(GTimeGraph gTimeGraph, Row row)
        {
            if (row != null && row.UserData != null && row.UserData is GuiDataRow)
                this.RefreshGraphFromGui(gTimeGraph, row.UserData as GuiDataRow);
        }
        /// <summary>
        /// Naplní do dodaného grafu data dodaná z GUI vrstvy, pokud nějaká data dodaná byla
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="dataRow"></param>
        protected void RefreshGraphFromGui(GTimeGraph gTimeGraph, GuiDataRow dataRow)
        {
            if (dataRow != null && dataRow.Graph != null)
                this.RefreshGraphFromGui(gTimeGraph, dataRow.Graph);
        }
        /// <summary>
        /// Naplní do dodaného grafu data dodaná z GUI vrstvy, pokud nějaká data dodaná byla
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="guiGraph"></param>
        protected void RefreshGraphFromGui(GTimeGraph gTimeGraph, GuiGraph guiGraph)
        {
            if (guiGraph != null)
            {
                gTimeGraph.UpdateGraphData(guiGraph);
                this._RefreshGraphItems(gTimeGraph, guiGraph.GraphItems);
            }
        }
        #endregion
        #region Vyhledání prvků grafu
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected DataGraphItem GetGraphItem(int id)
        {
            return this.GetGraphItem(this.GetGId(id));
        }
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho GId
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected DataGraphItem GetGraphItem(GId gId)
        {
            if (gId == null) return null;
            DataGraphItem dataGraphItem;
            if (!this.TimeGraphItemDict.TryGetValue(gId, out dataGraphItem)) return null;
            return dataGraphItem;
        }
        /// <summary>
        /// Metoda vrací <see cref="GId"/> řádku, na němž je umístěn daný graf.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        protected GId GetGraphRowGid(GTimeGraph graph)
        {
            if (graph == null) return null;
            GRow gRow = graph.SearchForParent(typeof(GRow)) as GRow;
            if (gRow == null) return null;
            return gRow.OwnerRow.RecordGId;
        }
        /// <summary>
        /// Metoda pro daný prvek <see cref="IInteractiveItem"/> zjistí, zda se jedná o prvek grafu <see cref="GTimeGraphItem"/>. Pokud ne, pak vrací null.
        /// Pokud ano, pak z vizuálního prvku grafu načte všechny datové prvky grafu = kolekce <see cref="ITimeGraphItem"/>.
        /// Najde odpovídající Schedulerovou tabulku, do které patří daný prvek grafu <see cref="MainDataTable"/>.
        /// Z tabulky <see cref="MainDataTable"/> si nechá určit identifikátory <see cref="GuiGridItemId"/> nalezených prvků grafů, a ty vrátí.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="wholeGroup">Vrátit všechny prvky i tehdy, když daný prvek reprezentuje jednu položku?</param>
        /// <returns></returns>
        public static GuiGridItemId[] GetGuiGridItems(IInteractiveItem item, bool wholeGroup)
        {
            if (item == null) return null;
            GTimeGraphItem graphItem = InteractiveObject.SearchForItem(item, true, typeof(GTimeGraphItem)) as GTimeGraphItem;
            if (graphItem == null) return null;
            ITimeGraphItem[] dataItems = graphItem.GetDataItems(wholeGroup);             // Najdu datové prvky odpovídající vizuálnímu prvku, najdu všechny prvky grupy
            if (dataItems == null || dataItems.Length == 0) return null;
            GTable gTable = graphItem.SearchForParent(typeof(GTable)) as GTable;         // Najdu vizuální tabulku, v níž daný prvek grafu bydlí
            if (gTable == null) return null;
            MainDataTable mainDataTable = gTable.DataTable.UserData as MainDataTable;    // Ve vizuální tabulce najdu její datový základ, a jeho UserData by měla být instance MainDataTable
            if (mainDataTable == null) return null;

            return mainDataTable.GetGuiGridItems(dataItems);                             // Instance MainDataTable vrátí identifikátory předaných prvků.
        }
        /// <summary>
        /// Metoda pro dané prvky <see cref="ITimeGraphItem"/> najde a vrátí pole jejich identifikátorů <see cref="GuiGridItemId"/>.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <returns></returns>
        public GuiGridItemId[] GetGuiGridItems(IEnumerable<ITimeGraphItem> dataItems)
        {
            if (dataItems == null) return null;
            List<GuiGridItemId> gridItemIdList = new List<GuiGridItemId>();
            foreach (ITimeGraphItem dataItem in dataItems)
            {
                DataGraphItem gridItem = dataItem as DataGraphItem;
                if (dataItem == null) continue;
                GuiGridItemId gridItemId = this.GetGridItemId(gridItem);
                if (gridItemId == null) continue;
                gridItemIdList.Add(gridItemId);
            }
            return gridItemIdList.ToArray();
        }
        /// <summary>
        /// Metoda z dodaného soupisu prvků <see cref="GuiGridItemId"/> sestaví lineární seznam jejich <see cref="GuiId"/>
        /// </summary>
        /// <param name="gridItemIds"></param>
        /// <param name="addItem"></param>
        /// <param name="addGroup"></param>
        /// <param name="addData"></param>
        /// <param name="addRow"></param>
        /// <returns></returns>
        protected static GuiId[] GetGuiIds(IEnumerable<GuiGridItemId> gridItemIds, bool addItem = true, bool addGroup = true, bool addData = true, bool addRow = true)
        {
            Dictionary<GuiId, object> searchIdDict = new Dictionary<GuiId, object>();
            if (gridItemIds != null)
            {
                foreach (GuiGridItemId id in gridItemIds)
                {
                    if (id == null) continue;
                    if (addItem) _GetGuiIdsAddOne(id.ItemId, searchIdDict);
                    if (addGroup) _GetGuiIdsAddOne(id.GroupId, searchIdDict);
                    if (addData) _GetGuiIdsAddOne(id.DataId, searchIdDict);
                    if (addRow) _GetGuiIdsAddOne(id.RowId, searchIdDict);
                }
            }
            return searchIdDict.Keys.ToArray();
        }
        private static void _GetGuiIdsAddOne(GuiId itemId, Dictionary<GuiId, object> searchIdDict)
        {
            if (itemId != null && itemId.RecordId != 0 && !searchIdDict.ContainsKey(itemId))
                searchIdDict.Add(itemId, null);
        }
        /// <summary>
        /// Metoda vrátí Int32 ID pro daný <see cref="GId"/>.
        /// Pro opakovaný požadavek na tentýž <see cref="GId"/> vrací shodnou hodnotu ID.
        /// Pro první požadavek na určitý <see cref="GId"/> vytvoří nový ID.
        /// Reverzní metoda je <see cref="GetGId(int)"/>.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected int GetId(GId gId)
        {
            if (gId == null) return 0;
            return this.TimeIdIndex.GetIndex(gId);
        }
        /// <summary>
        /// Pro daný Int32 ID vrátí <see cref="GId"/>, ale pouze pokud byl přidělen v metodě <see cref="GetId(GId)"/>.
        /// Pokud daný int nezná, vrátí null.
        /// Reverzní metoda je <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected GId GetGId(int id)
        {
            if (id == 0) return null;
            GId gId;
            if (!this.TimeIdIndex.TryGetKey(id, out gId)) return null;
            return gId;
        }
        #endregion
        #region Data (Dictionary) pro evidenci grafů a jejich položek a grup, index GId-Id
        /// <summary>
        /// Dictionary všech instancí grafů, které jsou vytvořeny do řádků zdejší tabulky <see cref="TableRow"/>.
        /// Klíčem je GId řádku. Zde jsou grafy jak "plné", umístěné v samostatném sloupci, tak i grafy "na pozadí".
        /// </summary>
        protected Dictionary<GId, GTimeGraph> TimeGraphDict { get; private set; }
        /// <summary>
        /// Dictionary pro vyhledání prvku grafu podle jeho GId. Primární úložiště položek grafů.
        /// Klíčem je GId grafického prvku <see cref="DataGraphItem.ItemGId"/>.
        /// </summary>
        protected Dictionary<GId, DataGraphItem> TimeGraphItemDict { get; private set; }
        /// <summary>
        /// DictionaryList pro uchování informací o grupách grafických prvků.
        /// Klíčem je tedy Int32 GroupId (pokud prvek má zadanou grupu), a hodnotou je seznam všech prvků v dané grupě.
        /// </summary>
        protected DictionaryList<GId, DataGraphItem> TimeGraphGroupDict { get; private set; }
        /// <summary>
        /// Index pro obousměrnou konverzi Int32 - GId
        /// </summary>
        protected Index<GId> TimeIdIndex { get; set; }
        #endregion
        #region Vlastnosti grafů a další property pro grafy
        /// <summary>
        /// Aktuální synchronizovaný časový interval
        /// </summary>
        protected TimeRange SynchronizedTime { get { return this.IMainData.SynchronizedTime; } set { this.IMainData.SynchronizedTime = value; } }
        /// <summary>
        /// Celkový časový interval <see cref="GuiProperties.TotalTimeRange"/>
        /// </summary>
        protected TimeRange TotalTime { get { return this.IMainData?.GuiData?.Properties?.TotalTimeRange; } }
        /// <summary>
        /// Vlastnosti tabulky, načtené z DataDeclaration
        /// </summary>
        public DataGraphProperties GraphProperties { get; private set; }
        /// <summary>
        /// Režim časové osy v grafu, podle zadání v deklaraci
        /// </summary>
        protected TimeGraphTimeAxisMode TimeAxisMode { get; private set; }
        /// <summary>
        /// Pozice grafu. Obsahuje None, pokud graf není definován.
        /// </summary>
        protected DataGraphPositionType GraphPosition { get; private set; }
        /// <summary>
        /// Sloupec hlavní tabulky, který zobrazuje graf při umístění <see cref="DataGraphPositionType.InLastColumn"/>
        /// </summary>
        protected Column GraphColumn { get; private set; }
        #endregion
        #endregion
        #region Refresh obsahu tabulky na základě dat z GuiResponse : přidání/aktualizace/odebrání : řádků/grafů/prvků grafu
        #region Refresh a Expand řádků
        /// <summary>
        /// Metoda přidá/aktualizuje/odebere daný řádek this tabulky.
        /// </summary>
        /// <param name="refreshRow"></param>
        /// <param name="repaintGraphDict"></param>
        public void RefreshRow(GuiRefreshRow refreshRow, Dictionary<uint, GTimeGraph> repaintGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._UpdateRow(refreshRow);
            _AddRepaintModifiedGraph(modifiedGraph, repaintGraphDict);
        }
        /// <summary>
        /// Provede aktualizaci řádku
        /// </summary>
        /// <param name="refreshRow"></param>
        /// <returns></returns>
        private GTimeGraph _UpdateRow(GuiRefreshRow refreshRow)
        {
            if (refreshRow == null || (refreshRow.GridRowId == null && refreshRow.RowData == null)) return null;

            GTimeGraph modifiedGraph = null;
            GId rowGId;
            Row row;
            if (refreshRow.RowData != null)
            {   // Máme zadaná data řádku - půjde o Insert nebo Update:
                rowGId = (refreshRow.RowData?.RowGuiId ?? refreshRow.GridRowId?.RowId);    // ID řádku: primárně z dat řádku, sekundárně z ID
                if (rowGId == null)
                    throw new GraphLibDataException("Chyba: v řádku GuiRefreshRow není přítomen RowId ani v RowData, ani v GridRowId.");

                if (!this.TryGetRow(rowGId, out row))
                {   // Insert: V tabulce nebyl nalezen řádek pro daný GId => vytvoříme nový řádek a přidáme do tabulky:
                    row = Row.CreateFrom(refreshRow.RowData);
                    this.TableRow.AddRow(row);
                    this.PrepareGraphForRow(row);
                }
                else
                {   // Update:
                    row.UpdateFrom(refreshRow.RowData);
                    this.UpdateGraphFromRow(row);
         // ???     this.PrepareGraphForRow(row);
                }
                this.TimeGraphDict.TryGetValue(rowGId, out modifiedGraph);
            }
            else if (refreshRow.GridRowId != null && refreshRow.GridRowId.RowId != null && this.TryGetRow(refreshRow.GridRowId.RowId, out row))
            {   // Delete:
                this.TableRow.Rows.Remove(row);
            }

            return modifiedGraph;
        }
        /// <summary>
        /// Metoda zajistí provedení Expand pro daný řádek tabulky a pro jeho Parents.
        /// </summary>
        /// <param name="expandRow"></param>
        /// <param name="repaintGraphDict"></param>
        public void ExpandRow(GuiGridRowId expandRow, Dictionary<uint, GTimeGraph> repaintGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._ExpandRow(expandRow);
            _AddRepaintModifiedGraph(modifiedGraph, repaintGraphDict);
        }
        /// <summary>
        /// Provede expand řádku a jeho parentů
        /// </summary>
        /// <param name="expandRow"></param>
        /// <returns></returns>
        private GTimeGraph _ExpandRow(GuiGridRowId expandRow)
        {
            if (expandRow == null || expandRow.RowId == null) return null;

            GTimeGraph modifiedGraph = null;
            GId rowGId = expandRow.RowId;
            Row row;
            if (this.TryGetRow(rowGId, out row))
                row.TreeNode.ExpandWithParents();

            return modifiedGraph;
        }
        #endregion
        #region Refresh grafů
        /// <summary>
        /// Metoda přidá/aktualizuje/odebere daný graf.
        /// </summary>
        /// <param name="refreshGraph"></param>
        /// <param name="repaintGraphDict"></param>
        public void RefreshGraph(GuiRefreshGraph refreshGraph, Dictionary<uint, GTimeGraph> repaintGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._RefreshGraph(refreshGraph);
            _AddRepaintModifiedGraph(modifiedGraph, repaintGraphDict);
        }
        /// <summary>
        /// Metoda z dodaného prvku <see cref="GuiRefreshGraph"/> aktualizuje data odpovídajícího grafu <see cref="GTimeGraph"/>.
        /// Vrací referenci na zmíněný modifikovaný graf.
        /// </summary>
        /// <param name="refreshGraph"></param>
        /// <returns></returns>
        private GTimeGraph _RefreshGraph(GuiRefreshGraph refreshGraph)
        {
            if (refreshGraph == null || (refreshGraph.GridRowId == null && refreshGraph.GraphData == null)) return null;

            GTimeGraph modifiedGraph = null;
            GTimeGraph gTimeGraph;
            GId rowGId;
            if (refreshGraph.GraphData != null)
            {   // Máme zadaná data grafu - půjde o Insert nebo Update:
                rowGId = (refreshGraph.GraphData?.RowId ?? refreshGraph.GridRowId?.RowId);    // ID řádku: primárně z grafu, sekundárně z ID
                if (!this.TimeGraphDict.TryGetValue(rowGId, out gTimeGraph))
                {   // Insert: Pro daný řádek ještě graf nemám:




                }
                else
                {   // Update properties:
                    gTimeGraph.UpdateGraphData(refreshGraph.GraphData);

                    // RemoveAllOld:
                    if (refreshGraph.ItemsMergeMode == GuiMergeMode.RemoveAllOld)
                    {
                        Dictionary<GuiId, DataGraphItem> itemDict = this.GetGraphItemDict(gTimeGraph);
                        GuiGridItemId[] removeIds = itemDict.Values
                            .Select(d => new GuiGridItemId() { ItemId = d.ItemGId, DataId = d.DataGId, RowId = d.RowGId, GroupId = d.GroupGId })
                            .ToArray();
                        this._RemoveItemsFromGraph(gTimeGraph, removeIds);
                    }
                    // RemoveItems:
                    else if (refreshGraph.RemoveItems != null)
                    {
                        this._RemoveItemsFromGraph(gTimeGraph, refreshGraph.RemoveItems);
                    }

                    // Add items:
                    if (refreshGraph.GraphData.GraphItems != null && refreshGraph.GraphData.GraphItems.Count > 0)
                    {
                        bool disableUpdate = (refreshGraph.ItemsMergeMode == GuiMergeMode.InsertOnly);
                        this._RefreshGraphItems(gTimeGraph, refreshGraph.GraphData.GraphItems, disableUpdate);
                    }

                    modifiedGraph = gTimeGraph;
                }
            }
            else if (refreshGraph.GridRowId != null)
            {   // Nemám data grafu, ale mám ID - půjde možná o Delete:


            }

            return modifiedGraph;
        }
        /// <summary>
        /// Metoda vrátí Dictionary obsahující všechny prvky daného grafu, které jsou <see cref="DataGraphItem"/>.
        /// Klíčem v Dictionary je <see cref="DataGraphItem.ItemGId"/>
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <returns></returns>
        protected Dictionary<GuiId, DataGraphItem> GetGraphItemDict(GTimeGraph gTimeGraph)
        {
            Dictionary<GuiId, DataGraphItem> result = new Dictionary<GuiId, DataGraphItem>();
            if (gTimeGraph != null)
            {
                foreach (var item in gTimeGraph.Items)
                {
                    DataGraphItem graphItem = item as DataGraphItem;
                    if (graphItem != null && graphItem.ItemGId != null && !result.ContainsKey(graphItem.ItemGId))
                        result.Add(graphItem.ItemGId, graphItem);
                }
            }
            return result;
        }
        #endregion
        #region Refresh prvků grafu
        /// <summary>
        /// Metoda přidá/aktualizuje/odebere daný prvek grafu z odpovídajícího grafu.
        /// </summary>
        /// <param name="refreshGraphItem"></param>
        /// <param name="repaintGraphDict"></param>
        public void RefreshGraphItem(GuiRefreshGraphItem refreshGraphItem, Dictionary<uint, GTimeGraph> repaintGraphDict = null)
        {
            if (refreshGraphItem == null) return;
            this.RefreshGraphItem(refreshGraphItem.GridItemId, refreshGraphItem.ItemData, repaintGraphDict);
        }
        /// <summary>
        /// Metoda přidá/aktualizuje/odebere daný prvek grafu z odpovídajícího grafu.
        /// </summary>
        /// <param name="guiGridItemId">ID prvku, smí být null - pak se jedná o Insert/Update a ID se odvodí z prvku grafu</param>
        /// <param name="guiGraphItem">Data prvku grafu, smí být null - pak se jedná o Delete</param>
        /// <param name="repaintGraphDict"></param>
        public void RefreshGraphItem(GuiGridItemId guiGridItemId, GuiGraphItem guiGraphItem, Dictionary<uint, GTimeGraph> repaintGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._RefreshGraphItem(guiGridItemId, guiGraphItem);
            _AddRepaintModifiedGraph(modifiedGraph, repaintGraphDict);
        }
        /// <summary>
        /// Metoda přidá/aktualizuje/odebere daný prvek grafu z odpovídajícího grafu.
        /// Vrací graf, pokud byl nalezen a změněn.
        /// Metoda z dodaného prvku <see cref="GuiRefreshGraphItem"/> vytvoří prvek grafu <see cref="DataGraphItem"/>, 
        /// prvek uloží do Dictionary <see cref="TimeGraphItemDict"/>,
        /// podle jeho řádku <see cref="DataGraphItem.RowGId"/> najde v Dictionary <see cref="TimeGraphDict"/> graf, a do něj vloží grafický prvek.
        /// Vrací referenci na zmíněný modifikovaný graf.
        /// </summary>
        /// <param name="guiGridItemId">ID prvku, smí být null - pak se jedná o Insert/Update a ID se odvodí z prvku grafu</param>
        /// <param name="guiGraphItem">Data prvku grafu, smí být null - pak se jedná o Delete</param>
        /// <param name="disableUpdate">Zákaz provedení akce Update: pokud prvek s určitím ItemId existuje v grafu i v dodaných datech, provádí se Update stávajícho prvku. Tento parametr může hodnotou true zakázat Update. Slouží při refreshi Grafu.</param>
        /// <returns></returns>
        private GTimeGraph _RefreshGraphItem(GuiGridItemId guiGridItemId, GuiGraphItem guiGraphItem, bool disableUpdate = false)
        {
            if (guiGridItemId == null && guiGraphItem == null) return null;

            GTimeGraph modifiedGraph = null;
            GTimeGraph gTimeGraph;
            if (this.TryGetGraph(guiGraphItem?.RowId ?? guiGridItemId?.RowId, out gTimeGraph))
            {
                if (this._RefreshGraphItem(gTimeGraph, guiGridItemId, guiGraphItem, disableUpdate, true))
                    modifiedGraph = gTimeGraph;
            }

            return modifiedGraph;
        }
        /// <summary>
        /// Metoda zajistí vytvoření řady prvků grafu (třída <see cref="DataGraphItem"/>) z dat o prvku (třída <see cref="GuiGraphItem"/>),
        /// dále pak přidání vytvořených prvků <see cref="DataGraphItem"/> do dodaného grafu, i do zdejší Dictionary <see cref="TimeGraphItemDict"/> a do <see cref="TimeGraphGroupDict"/>.
        /// Vrací true = došlo k přidání / false = nebyla změna.
        /// </summary>
        /// <param name="gTimeGraph">Cílový graf</param>
        /// <param name="guiGraphItems">Prkvy do grafu</param>
        /// <param name="disableUpdate">Zákaz provedení akce Update: pokud prvek s určitím ItemId existuje v grafu i v dodaných datech, provádí se Update stávajícho prvku. Tento parametr může hodnotou true zakázat Update. Slouží při refreshi Grafu.</param>
        /// <returns></returns>
        private bool _RefreshGraphItems(GTimeGraph gTimeGraph, IEnumerable<GuiGraphItem> guiGraphItems, bool disableUpdate = false)
        {
            if (gTimeGraph == null || guiGraphItems == null) return false;

            bool isChange = false;
            foreach (GuiGraphItem guiGraphItem in guiGraphItems)
            {
                bool oneChange = this._RefreshGraphItem(gTimeGraph, null, guiGraphItem, disableUpdate, true);
                if (!isChange && oneChange)
                    isChange = true;
            }
            return isChange;
        }
        /// <summary>
        /// Metoda najde dané prvky v daném grafu a odebere je jak z grafu, tak z Dictionary <see cref="TimeGraphItemDict"/>.
        /// Pokud prvek neexistuje v daném grafu, pak nedojde k chybě, ale prvek se neodebere (ani z grafu ani z Dictionary).
        /// Vrací true = došlo ke změně / false = nikoli.
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="removeItems"></param>
        /// <returns></returns>
        private bool _RemoveItemsFromGraph(GTimeGraph gTimeGraph, IEnumerable<GuiGridItemId> removeItems)
        {
            if (gTimeGraph == null || removeItems == null) return false;

            bool isChange = false;
            foreach (GuiGridItemId removeItem in removeItems)
            {
                bool oneChange = this._RefreshGraphItem(gTimeGraph, removeItem, null, false, false);
                if (!isChange && oneChange)
                    isChange = true;
            }
            return isChange;
        }
        /// <summary>
        /// Metoda zajistí vytvoření prvku grafu (třída <see cref="DataGraphItem"/>) z dat o prvku (třída <see cref="GuiGraphItem"/>),
        /// dále pak přidání prvku <see cref="DataGraphItem"/> do dodaného grafu, i do zdejší Dictionary <see cref="TimeGraphItemDict"/> a <see cref="TimeGraphGroupDict"/>.
        /// Vrací true = došlo ke změně / false = nebyla změna.
        /// Pokud je vráceno true, později (hromadně) se provede refresh grafu gTimeGraph.
        /// Toto je jediná metoda, která { přidává / aktualizuje / odebírá } prvky ze struktur grafu a z indexů !!!
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="guiGridItemId">ID prvku, smí být null - pak se jedná o Insert/Update a ID se odvodí z prvku grafu</param>
        /// <param name="guiGraphItem">Data prvku grafu, smí být null - pak se jedná o Delete</param>
        /// <param name="disableUpdate">Zákaz provedení akce Update: pokud prvek s určitím ItemId existuje v grafu i v dodaných datech, provádí se Update stávajícho prvku. Tento parametr může hodnotou true zakázat Update. Slouží při refreshi Grafu.</param>
        /// <param name="forceRemove">Pokud se provádí smazání prvku: Požadavek true = odebrat prvek z místních Dictionary i tehdy, když prvek není obsažen v grafu (nebo když graf je null)</param>
        /// <returns></returns>
        private bool _RefreshGraphItem(GTimeGraph gTimeGraph, GuiGridItemId guiGridItemId, GuiGraphItem guiGraphItem, bool disableUpdate, bool forceRemove)
        {
            if (gTimeGraph == null || (guiGridItemId == null && guiGraphItem == null)) return false;

            // Toto je jediná metoda, která { přidává / aktualizuje / odebírá } prvky ze struktur grafu a z indexů !!!
            bool isChange = false;
            DataGraphItem dataGraphItem;
            ITimeGraphItem graphItem;
            GuiId rowGId;
            GuiId itemGId;
            int itemId;
            if (guiGraphItem != null)
            {   // Insert / Update
                itemGId = guiGraphItem?.ItemId ?? guiGridItemId?.ItemId;
                if (itemGId != null)
                {   // Máme na vstupu nalezené ItemId prvku grafu:
                    itemId = this.GetId(itemGId);                // Najde/Vytvoří a vrátí Int32 klíč prvku. To že v případě neexistence se vygeneruje nové Id nám nevadí, protože by se tak jako tak generovalo o něco později.
                    if (!gTimeGraph.TryGetGraphItem(itemId, out graphItem))
                    {   // Prvek daného ID v grafu NEMÁME => jde o Insert:
                        dataGraphItem = DataGraphItem.CreateFrom(this, guiGraphItem);
                        if (dataGraphItem != null)
                        {
                            isChange = gTimeGraph.AddGraphItem(dataGraphItem);
                            this.ApplyCurrentSkinIndexTo(dataGraphItem);       // Nastavíme zdejší aktuální Skin, aby prvek barevně zapadl mezi své kolegy
                            if (isChange)
                            {   // Insert prvku: uložíme jej i do tabulkových indexů:
                                if (!this.TimeGraphItemDict.ContainsKey(dataGraphItem.ItemGId))
                                    this.TimeGraphItemDict.Add(dataGraphItem.ItemGId, dataGraphItem);
                                if (dataGraphItem.GroupGId != null)
                                    this.TimeGraphGroupDict.Add(dataGraphItem);
                            }
                        }
                    }
                    else if (!disableUpdate)
                    {   // Prvek daného ID v grafu MÁME => jde o Update hodnot stávajícího objektu z dat dodaného objektu (a Update není zakázaný):
                        dataGraphItem = graphItem as DataGraphItem;
                        if (dataGraphItem != null)
                        {
                            isChange = dataGraphItem.UpdateFrom(guiGraphItem);
                            this.ApplyCurrentSkinIndexTo(dataGraphItem);       // Nastavíme zdejší aktuální Skin, aby prvek barevně zapadl mezi své kolegy
                        }
                    }
                }
            }
            else if (guiGridItemId != null && guiGridItemId.ItemId != null)
            {   // Delete:
                itemGId = guiGridItemId.ItemId;
                this.TimeGraphItemDict.TryGetValue(itemGId, out dataGraphItem);// Prvek grafu z globálního indexu
                rowGId = (dataGraphItem?.RowGId ?? guiGridItemId.RowId);       // ID řádku: primárně z nalezeného prvku grafu, alternativně z ID
                if (gTimeGraph == null)
                    this.TimeGraphDict.TryGetValue(rowGId, out gTimeGraph);    // Graf, pokud nebyl explicitně dodán

                // Odebrat prvek z grafu:
                itemId = this.GetId(itemGId);
                if (gTimeGraph != null)
                    isChange = gTimeGraph.RemoveGraphItem(itemId, true);       // isChange říká, že byl změněn graf. Nemusí tedy reagovat na změnu indexů.

                // Odebrat prvek z indexů:
                if (isChange || forceRemove)
                {   // Pokud byl prvek reálně odebrán z grafu, anebo nebyl ale má se smazat z indexů povinně = bez ohledu na graf:
                    this.TimeGraphItemDict.RemoveIfExists(itemGId);
                    if (dataGraphItem != null && dataGraphItem.GroupGId != null)
                        this.TimeGraphGroupDict.Remove(dataGraphItem);
                }
            }
            return isChange;
        }
        /// <summary>
        /// Metoda najde graf pro dané ID řádku.
        /// Pokud ID řádku je null, vrátí false ale ne chybu.
        /// </summary>
        /// <param name="rowId"></param>
        /// <param name="gTimeGraph"></param>
        /// <returns></returns>
        protected bool TryGetGraph(GuiId rowId, out GTimeGraph gTimeGraph)
        {
            gTimeGraph = null;
            if (rowId == null) return false;
            return this.TimeGraphDict.TryGetValue(rowId, out gTimeGraph);
        }
        #endregion
        /// <summary>
        /// Zajistí provedení Refresh() na modifikovaném grafu (parametr):
        /// Buď je zadána Dictionary s grafy pro hromadný Refresh, pak aktuální graf do ní přidá;
        /// Anebo není Dictionary zadána, a pak provede Refresh na grafu ihned.
        /// Pokud je na vstupu graf = null, pak nic neřeší (to je situace, kdy graf nebyl modifikován).
        /// </summary>
        /// <param name="modifiedGraph"></param>
        /// <param name="repaintGraphDict"></param>
        private static void _AddRepaintModifiedGraph(GTimeGraph modifiedGraph, Dictionary<uint, GTimeGraph> repaintGraphDict)
        {
            if (modifiedGraph != null)
            {
                if (repaintGraphDict != null)
                {
                    if (!repaintGraphDict.ContainsKey(modifiedGraph.Id))
                        repaintGraphDict.Add(modifiedGraph.Id, modifiedGraph);
                }
                else
                {
                    modifiedGraph.Refresh();
                }
            }
        }
        #endregion
        #region Linky mezi položkami grafů
        /// <summary>
        /// Reference na koordinační objekt pro kreslení linek všech grafů v této tabulce, třída: <see cref="GTimeGraphLinkItem"/>.
        /// Tento prvek slouží jednotlivým grafům. Před dokončením incializace je null.
        /// </summary>
        public GTimeGraphLinkArray GraphLinkArray { get { return this.GTableRow?.GraphLinkArray; } }
        /// <summary>
        /// Metoda načte a předzpracuje informace o vztazích mezi prvky grafů (Linky)
        /// </summary>
        protected void LoadLinks()
        {
            this.GraphLinkPrevDict = new DictionaryList<int, GTimeGraphLinkItem>();
            this.GraphLinkNextDict = new DictionaryList<int, GTimeGraphLinkItem>();
            List<GuiGraphLink> graphLinks = this.GuiGrid.RowTable?.GraphLinks;
            if (graphLinks != null && graphLinks.Count > 0)
                this.AddGraphLinks(graphLinks);
        }
        /// <summary>
        /// Do soupisu linků přidá / aktualizuje nové položky.
        /// </summary>
        /// <param name="guiLinks"></param>
        public void UpdateGraphLinks(IEnumerable<GuiGraphLink> guiLinks)
        {
            if (guiLinks != null)
            {
                this.RemoveGraphLinks(guiLinks);
                this.AddGraphLinks(guiLinks, false);
            }
        }
        /// <summary>
        /// Metoda načte a předzpracuje informace o vztazích mezi prvky grafů (Linky).
        /// Akceptuje pouze ty vztahy, které mají naplněn typ <see cref="GuiGraphLink.LinkType"/>, a jejichž Prev a Next strany jsou naplněny, a nejsou identické.
        /// Tato metoda defaultně neprovádí odebrání stávajících záznamů, to si musí volající zajistit předem!
        /// Může o to požádat parametrem removeItems, ale ten zajistí odebrání jen těch prvků (z dopdaného seznamu), které se budou nvoě přidávat.
        /// Nezajistí se tím odebrání prvků, které mají <see cref="GuiGraphLink.LinkType"/> null nebo None!
        /// </summary>
        /// <param name="guiLinks">guiLinks</param>
        /// <param name="removeItems"></param>
        protected void AddGraphLinks(IEnumerable<GuiGraphLink> guiLinks, bool removeItems = false)
        {
            if (guiLinks == null) return;

            // Z instancí třídy GuiGraphLink vytvořím instance třídy GTimeGraphLink:
            GTimeGraphLinkItem[] links = guiLinks
                .Where(g => (g.LinkType.HasValue && g.LinkType.Value != GuiGraphItemLinkType.None && g.ItemIdPrev != null && g.ItemIdNext != null && g.ItemIdPrev != g.ItemIdNext))
                .Select(g => this.CreateGraphLink(g))
                .ToArray();

            // Odeberu stávající záznamy (z GraphLinkDict), které mají shodné klíče Prev a Next s těmi, které se budou zanedlouho přidávat:
            if (removeItems)
                // Tady předávám referenci na IEnumerable links, které ještě reálně není enumerováno!!!  Úmyslně! Proto, že v metodě RemoveGraphLinks() možná ani nedojde k jejímu reálnému použití (viz tam, druhý řádek)
                this.RemoveGraphLinks(links.Select(l => new Tuple<int, int>(l.ItemIdPrev, l.ItemIdNext)));

            // Objekt DictionaryList drží soupisy vztahů jak při pohledu zleva, tak zprava.
            // Je zajištěno, že vstupující seznam (links) obsahuje pouze ty objekty, které mají oboustranný vztah (Prev i Next je naplněno) mezi různými prvky (Prev != Next).
            // Následně můžeme najít vztahy pro libovolný prvek podle jeho Int32 klíče, ať už je to vztah "doleva" nebo "doprava", viz metoda SearchForGraphLink()
            this.GraphLinkPrevDict.AddRange(links, g => g.ItemIdPrev);
            this.GraphLinkNextDict.AddRange(links, g => g.ItemIdNext);
        }
        /// <summary>
        /// Metoda odebere všechny záznamy z <see cref="GraphLinkPrevDict"/> a <see cref="GraphLinkNextDict"/>, jejichž hodnoty <see cref="GTimeGraphLinkItem.ItemIdPrev"/> a <see cref="GTimeGraphLinkItem.ItemIdNext"/>
        /// se shodují s hodnotami dodaných záznamů.
        /// Pokud ale <see cref="GraphLinkPrevDict"/> a <see cref="GraphLinkNextDict"/> neobsahuje žádný prvek, pak se neprovádí nic (ani zahájení enumerace parametru).
        /// </summary>
        /// <param name="guiLinks"></param>
        protected void RemoveGraphLinks(IEnumerable<GuiGraphLink> guiLinks)
        {
            if (guiLinks == null) return;
            if (this.GraphLinkPrevDict.CountKeys == 0 && this.GraphLinkNextDict.CountKeys == 0) return; // Pokud není CO SMAZAT, nepotřebuji pracovat se vstupní kolekcí!

            // Vytvořím soupis dvojklíčů <Int32> z pole linků, kde jsou klíče Prev a Next ve formě <GuiId>:
            Tuple<int, int>[] twoKeys = guiLinks
                .Where(g => (g.ItemIdPrev != null && g.ItemIdNext != null && g.ItemIdPrev != g.ItemIdNext))
                .Select(g => new Tuple<int, int>(this.GetId(g.ItemIdPrev), this.GetId(g.ItemIdNext)))
                .ToArray();

            // Odeberu stávající záznamy (z GraphLinkDict), které mají shodné klíče Prev a Next:
            this.RemoveGraphLinks(twoKeys);
        }
        /// <summary>
        /// Metoda odebere všechny záznamy z <see cref="GraphLinkPrevDict"/> a <see cref="GraphLinkNextDict"/>, jejichž hodnoty <see cref="GTimeGraphLinkItem.ItemIdPrev"/> a <see cref="GTimeGraphLinkItem.ItemIdNext"/>
        /// se shodují s hodnotami Item1 a Item2 z dodaných prvků.
        /// Pokud ale <see cref="GraphLinkPrevDict"/> a <see cref="GraphLinkNextDict"/> neobsahuje žádný prvek, pak se neprovádí nic (ani zahájení enumerace parametru).
        /// </summary>
        /// <param name="twoKeys"></param>
        protected void RemoveGraphLinks(IEnumerable<Tuple<int, int>> twoKeys)
        {
            if (this.GraphLinkPrevDict.CountKeys == 0 && this.GraphLinkNextDict.CountKeys == 0) return;
            foreach (var twoKey in twoKeys)
            {
                this.GraphLinkPrevDict.RemoveAll((k, v) => (v.ItemIdPrev == twoKey.Item1 && v.ItemIdNext == twoKey.Item2));
                this.GraphLinkNextDict.RemoveAll((k, v) => (v.ItemIdPrev == twoKey.Item1 && v.ItemIdNext == twoKey.Item2));
            }
        }
        /// <summary>
        /// Najde a vrátí vztahy prvků pro režim a data dle argumentu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        void ITimeGraphLinkDataSource.CreateLinks(CreateAllLinksArgs args)
        {
            GTimeGraphLinkItem[] links = null;
            // Máme dvě Dictionary obsahující Linky: GraphLinkNextDict a GraphLinkPrevDict: mají stejný počet vět, 
            //  a liší se jen tím, že v jedné je klíčem hodnota Next a v druhé je klíčem Prev.
            // Projdu tedy GraphLinkNextDict.Values a všechny vhodné hodnoty dám do výsledku:
            if (args.LinksMode.HasFlag(GTimeGraphLinkMode.Allways))
            {
                links = this.GraphLinkNextDict.Values;
            }

            if (links != null && links.Length > 0)
            {
                bool asSCurve = (this.Config != null && this.Config.GuiEditShowLinkAsSCurve);
                LinkLineType defaultLineType = (asSCurve ? LinkLineType.SCurveHorizontal : LinkLineType.StraightLine);

                foreach (GTimeGraphLinkItem link in links)
                {
                    if (this._SearchLinkItemPrepareData(link, Direction.Positive, GGraphControlPosition.Group, defaultLineType))
                        args.Links.Add(link);
                }
            }
        }
        /// <summary>
        /// Metoda najde a vrátí soupis platných vztahů mezi prvky pro jeden daný prvek grafu.
        /// Pokud daný prvek nemá žádný vztah, vrací se null.
        /// Pokud prvek má vztahy, pak tyto vztahy mají korektně vepsané reference na vztažené prvky grafů
        /// </summary>
        /// <param name="currentItem">Výchozí prvek pro hledání vztahů; může to být prvek grupy i prvek jednotlivý</param>
        /// <param name="searchSidePrev">Hledej linky na straně Prev</param>
        /// <param name="searchSideNext">Hledej linky na straně Next</param>
        /// <param name="wholeTask">Hledej linky pro celý Task</param>
        /// <param name="defaultLineType">Výchozí tvar křivky dle konfigurace</param>
        /// <returns></returns>
        protected GTimeGraphLinkItem[] SearchForGraphLink(GTimeGraphItem currentItem, bool searchSidePrev, bool searchSideNext, bool wholeTask, LinkLineType defaultLineType)
        {
            Dictionary<uint, GTimeGraphItem> itemDict = new Dictionary<uint, GTimeGraphItem>();
            Dictionary<ulong, GTimeGraphLinkItem> linkDict = new Dictionary<ulong, GTimeGraphLinkItem>();
            if (currentItem != null)
            {
                if (searchSidePrev && this.GraphLinkNextDict.CountKeys > 0) this._SearchForGraphLink(currentItem, this.GraphLinkNextDict, Direction.Negative, itemDict, linkDict, wholeTask, defaultLineType);
                if (searchSideNext && this.GraphLinkPrevDict.CountKeys > 0) this._SearchForGraphLink(currentItem, this.GraphLinkPrevDict, Direction.Positive, itemDict, linkDict, wholeTask, defaultLineType);
            }
            return linkDict.Values.ToArray();
        }
        /// <summary>
        /// Metoda vyhledá vztahy daného výchozího prvku (graphItem) v dané instanci DictionaryList (graphLinkDict) na dané straně vztahů (targetSide).
        /// Pokud pro prvek najde nějaké vztahy, pak pro každý vztah:
        /// dohledá cílový prvek na dané straně vztahu a cílový prvek vepíše do vztahu, vepíše i zdrojový prvek do vztahu; 
        /// a vztah uloží do výsledné dictionary (resultLinkDict).
        /// Následně vyhodnotí, zda nalezený cílový prvek se bude "rekurzivně" scanovat, a pokud ano, pak jej zařadí do zpracování v interní smyčce.
        /// </summary>
        /// <param name="currentItem">Výchozí prvek pro hledání vztahů; může to být prvek grupy i prvek jednotlivý</param>
        /// <param name="graphLinkDict">Dictionary obsahující vztahy v potřebném směru</param>
        /// <param name="targetSide">Směr vztahu</param>
        /// <param name="scanItemDict">Sem se průběžně ukládají scanované prvky, aby nedošlo k zacyklení - vyjma prvního (ten se scanuje dvakrát, jednou Prev a podruhé Next)</param>
        /// <param name="resultLinkDict">Sem se ukládají nalezené vztahy, klíčem je jejich <see cref="GTimeGraphLinkItem.Key"/>; jde o průběžný výstup</param>
        /// <param name="wholeTask">Hledej linky pro celý Task</param>
        /// <param name="defaultLineType">Výchozí tvar křivky dle konfigurace</param>
        /// <returns></returns>
        private void _SearchForGraphLink(GTimeGraphItem currentItem, DictionaryList<int, GTimeGraphLinkItem> graphLinkDict, Direction targetSide,
            Dictionary<uint, GTimeGraphItem> scanItemDict, Dictionary<ulong, GTimeGraphLinkItem> resultLinkDict, bool wholeTask, LinkLineType defaultLineType)
        {
            if (currentItem == null || !currentItem.Group.IsShowLinks) return;           // Pokud daný prvek NPOVOLUJE práci s Linky, skončíme...

            Direction sourceSide = targetSide.Reverse();
            Queue<GTimeGraphItem> searchQueue = new Queue<GTimeGraphItem>();
            searchQueue.Enqueue(currentItem);
            bool testDuplicity = false;
            while (searchQueue.Count > 0)
            {
                GTimeGraphItem searchItem = searchQueue.Dequeue();
                if (testDuplicity && scanItemDict.ContainsKey(searchItem.Id)) continue;

                // Najdu vztahy z daného prvku (z jeho Group nebo z Item), v dodané Dictionary (která je pro směr Prev nebo Next):
                GGraphControlPosition position = GGraphControlPosition.None;
                GTimeGraphLinkItem[] linkList = _SearchForGraphLinkOne(searchItem, graphLinkDict, out position);
                if (linkList == null || linkList.Length == 0) continue;

                // Nějaké vztahy jsme našli, tak pokud ještě nejsou ve výsledné Dictionary (linkDict):
                //  tak do nich doplníme výchozí prvek (baseItem) a cílový prvek (do strany side), přidáme do linkDict, a možná cílový prvek zařadíme do fronty:
                foreach (GTimeGraphLinkItem link in linkList)
                {
                    if (resultLinkDict.ContainsKey(link.Key)) continue;    // Tenhle vztah už ve výstupní dictionary máme; ten přeskočíme.

                    // Do linku doplníme zdrojový i cílový prvek vztahu a tvar křivky:
                    GTimeGraphItem targetItem;
                    if (this._SearchLinkItemPrepareData(link, targetSide, position, defaultLineType, out targetItem))
                    {   // Pokud jsme našli target:
                        resultLinkDict.Add(link.Key, link);

                        // Podle podmínek zajistíme provedení rekurze = hledání dalších vztahů z cílového prvku tohoto vztahu:
                        if (wholeTask && link.GuiGraphLink != null && link.GuiGraphLink.RelationType.HasValue && link.GuiGraphLink.RelationType.Value == GuiGraphItemLinkRelation.OneLevel && !scanItemDict.ContainsKey(targetItem.Id))
                        {   // Daný cílový prvek si zařadíme do fronty práce, a v některém z dalších cyklů v této metodě jej zpracujeme:
                            searchQueue.Enqueue(targetItem);
                        }
                    }
                }

                // Uložím si prvek, který byl právě vyřešen, do dictionary:
                if (!scanItemDict.ContainsKey(searchItem.Id))
                    scanItemDict.Add(searchItem.Id, searchItem);

                // Další prvek ve frontě už budeme testovat na non-duplicitu:
                testDuplicity = true;
            }
        }
        /// <summary>
        /// Metoda najde a vrátí linky z daného prvku (nejprve hledá pro grupu, pak pro jednotlivý prvek), z dané instance DictionaryList.
        /// </summary>
        /// <param name="baseItem"></param>
        /// <param name="graphLinkDict"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private GTimeGraphLinkItem[] _SearchForGraphLinkOne(GTimeGraphItem baseItem, DictionaryList<int, GTimeGraphLinkItem> graphLinkDict, out GGraphControlPosition position)
        {
            position = GGraphControlPosition.None;
            if (baseItem == null || graphLinkDict == null || graphLinkDict.CountKeys == 0) return null;
            if (!baseItem.Group.IsShowLinks) return null;

            GTimeGraphLinkItem[] links;

            links = graphLinkDict[baseItem.Group.GroupId];
            if (links != null)
            {
                position = GGraphControlPosition.Group;
                return links;
            }

            links = graphLinkDict[baseItem.Item.ItemId];
            if (links != null)
            {
                position = GGraphControlPosition.Item;
                return links;
            }

            return null;
        }
        /// <summary>
        /// Pro dodaný link (=záznam o vztahu dvou prvků) dohledá právě ty dva prvky Prev a Next a vloží je do linku.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="targetSide"></param>
        /// <param name="position"></param>
        /// <param name="defaultLineType"></param>
        /// <returns></returns>
        private bool _SearchLinkItemPrepareData(GTimeGraphLinkItem link, Direction targetSide, GGraphControlPosition position, LinkLineType defaultLineType)
        {
            GTimeGraphItem targetItem;
            return this._SearchLinkItemPrepareData(link, targetSide, position, defaultLineType, out targetItem);
        }
        /// <summary>
        /// Pro dodaný link (=záznam o vztahu dvou prvků) dohledá právě ty dva prvky Prev a Next a vloží je do linku.
        /// </summary>
        /// <param name="link"></param>
        /// <param name="targetSide"></param>
        /// <param name="position"></param>
        /// <param name="defaultLineType"></param>
        /// <param name="targetItem"></param>
        /// <returns></returns>
        private bool _SearchLinkItemPrepareData(GTimeGraphLinkItem link, Direction targetSide, GGraphControlPosition position, LinkLineType defaultLineType, out GTimeGraphItem targetItem)
        {
            Direction sourceSide = targetSide.Reverse();

            int targetId = link.GetId(targetSide);
            targetItem = this._SearchGraphItemsForLink(targetId, position);
            if (targetItem == null) return false;            // Vztah nemá nalezen prvek na cílové straně vztahu; vztah přeskočíme.
            int sourceId = link.GetId(sourceSide);           // Na source straně vztahu nemusí být nutně prvek, který jsme hledali - může tam být jeho grupa! (anebo naopak)
            GTimeGraphItem sourceItem = this._SearchGraphItemsForLink(sourceId, position);
            link.SetItem(sourceSide, sourceItem);            // Prvek na zdrojové straně vztahu (buď ten, kde hledání začalo, anebo odpovídající prvek = jeho Grupa, pro kterou máme vztahy!
            link.SetItem(targetSide, targetItem);            // Prvek na cílové straně vztahu
            link.PrepareCurrentLine(defaultLineType);        // Aplikuje defaultní tvar z konfigurace

            return true;
        }
        /// <summary>
        /// Metoda najde a vrátí grafický prvek grafu <see cref="GTimeGraphItem"/> pro dané ID prvku a danou prioritu pozice.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private GTimeGraphItem _SearchGraphItemsForLink(int id, GGraphControlPosition position)
        {
            GId key = this.GetGId(id);
            if (key == null) return null;

            // a) Zkusíme pod daným klíčem hledat GRUPU:
            DataGraphItem[] graphItems = this.TimeGraphGroupDict[key];
            if (graphItems != null && graphItems.Length > 0)
                // => tak vrátím vizuální control grupy (pokud tedy grupa má více než jeden prvek):
                return _SearchGraphItemsForLink(graphItems[0], position);

            // b) Zkusíme pod daným klíčem hledat PRVEK:
            DataGraphItem graphItem;
            if (this.TimeGraphItemDict.TryGetValue(key, out graphItem))
                return _SearchGraphItemsForLink(graphItem, position);

            return null;
        }
        /// <summary>
        /// Metoda vrátí grafický prvek grafu z daného datového prvku grafu.
        /// Pokud datovým prvkem (iGraphItem) je jednotlivý prvek (tedy nikoli Grupa), a jako parametr "position" je dáno <see cref="GGraphControlPosition.Item"/>, 
        /// pak výstupem bude <see cref="GTimeGraphItem"/> danho prvku. Jinak výstupem bude prvek pro grupu.
        /// </summary>
        /// <param name="iGraphItem"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private GTimeGraphItem _SearchGraphItemsForLink(ITimeGraphItem iGraphItem, GGraphControlPosition position)
        {
            if (iGraphItem == null || iGraphItem.GControl == null) return null;
            switch (iGraphItem.GControl.Position)
            {
                case GGraphControlPosition.Group:
                    return iGraphItem.GControl;
                case GGraphControlPosition.Item:
                    return (position == GGraphControlPosition.Item ? iGraphItem.GControl : iGraphItem.GControl.Group.GControl);
            }
            return null;
        }
        /// <summary>
        /// Vytvoří a vrátí new instanci <see cref="GTimeGraphLinkItem"/> na základě dat <see cref="GuiGraphLink"/>.
        /// </summary>
        /// <param name="guiGraphLink"></param>
        /// <returns></returns>
        protected GTimeGraphLinkItem CreateGraphLink(GuiGraphLink guiGraphLink)
        {
            if (!guiGraphLink.LinkType.HasValue || guiGraphLink.LinkType.Value == GuiGraphItemLinkType.None || guiGraphLink.LinkType.Value == GuiGraphItemLinkType.Invisible) return null;
            bool linkCenter = (guiGraphLink.LinkType.Value == GuiGraphItemLinkType.PrevCenterToNextCenter);
            LinkLineType? lineShape = CreateLinkShape(guiGraphLink.LineShape, linkCenter);
            GTimeGraphLinkItem graphLink = new GTimeGraphLinkItem()
            {
                ItemIdPrev = this.GetId(guiGraphLink.ItemIdPrev),
                ItemIdNext = this.GetId(guiGraphLink.ItemIdNext),
                LinkCenter = linkCenter,
                LineShape = lineShape,
                LinkWidth = guiGraphLink.LinkWidth,
                LinkColorStandard = guiGraphLink.LinkColorStandard,
                LinkColorWarning = guiGraphLink.LinkColorWarning,
                LinkColorError = guiGraphLink.LinkColorError,
                GuiGraphLink = guiGraphLink
            };
            return graphLink;
        }
        /// <summary>
        /// Vrací typ tvaru čáry
        /// </summary>
        /// <param name="lineShape"></param>
        /// <param name="linkCenter"></param>
        /// <returns></returns>
        protected LinkLineType? CreateLinkShape(GuiLineShape? lineShape, bool linkCenter)
        {
            if (!lineShape.HasValue)
            {
                GuiProperties guiProperties = this.MainData.GuiData?.Properties;
                if (guiProperties != null)
                    lineShape = (linkCenter ? guiProperties.LineShapeCenter : guiProperties.LineShapeEndBegin);
            }
            if (lineShape.HasValue)
            {
                switch (lineShape.Value)
                {
                    case GuiLineShape.StraightLine: return LinkLineType.StraightLine;
                    case GuiLineShape.SCurveHorizontal: return LinkLineType.SCurveHorizontal;
                    case GuiLineShape.SCurveVertical: return LinkLineType.SCurveVertical;
                    case GuiLineShape.ZigZagHorizontal: return LinkLineType.ZigZagHorizontal;
                    case GuiLineShape.ZigZagVertical: return LinkLineType.ZigZagVertical;
                    case GuiLineShape.ZigZagOptimal: return LinkLineType.ZigZagOptimal;
                }
            }
            return null;
        }
        /// <summary>
        /// Soupis linků mezi prvky grafů v této tabulce, ze strany Prev.
        /// Klíč (Int32) odpovídá údaji <see cref="GuiGraphLink.ItemIdPrev"/> ze vztahu.
        /// Hodnota pak reprezentuje všechny vztahy, které vedou z prvku [klíč] na prvky na straně Next.
        /// </summary>
        protected DictionaryList<int, GTimeGraphLinkItem> GraphLinkPrevDict;
        /// <summary>
        /// Soupis linků mezi prvky grafů v této tabulce, ze strany Next.
        /// Klíč (Int32) odpovídá údaji <see cref="GuiGraphLink.ItemIdPrev"/> ze vztahu.
        /// Hodnota pak reprezentuje všechny vztahy, které vedou z prvku [klíč] na prvky na straně Next.
        /// </summary>
        protected DictionaryList<int, GTimeGraphLinkItem> GraphLinkNextDict;
        #endregion
        #region Textové údaje (popisky grafů) a ToolTipy
        /// <summary>
        /// Načte tabulky s texty i tooltipy
        /// </summary>
        protected void LoadTexts()
        {
            GuiGrid guiGrid = this.GuiGrid;

            // Texty:
            this.TableTextList = new List<Table>();
            this.TableTextRowDict = new Dictionary<GId, Row>();
            if (guiGrid.GraphTextTable != null && guiGrid.GraphTextTable.RowCount > 0)
                this.TableTextList.Add(Table.CreateFrom(guiGrid.GraphTextTable));

            // ToolTipy:
            this.TableToolTipList = new List<Table>();
            this.TableToolTipRowDict = new Dictionary<GId, Row>();
            if (guiGrid.GraphToolTipTable != null && guiGrid.GraphToolTipTable.RowCount > 0)
                this.TableToolTipList.Add(Table.CreateFrom(guiGrid.GraphToolTipTable));
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky TEXTŮ, obsahující informace pro daný prvek.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <returns></returns>
        protected Row GetTableTextRow(DataGraphItem graphItem)
        {
            if (graphItem == null) return null;
            List<Table> sourceTables = this.TableTextList;
            List<Table> panelTables = this.Panel.TableTextList;
            Dictionary<GId, Row> cacheDict = this.TableTextRowDict;
            return this.GetTableInfoRow(sourceTables, panelTables, cacheDict, graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.RowGId);
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky TOOLTIPŮ, obsahující informace pro daný prvek.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <returns></returns>
        protected Row GetTableToolTipRow(DataGraphItem graphItem)
        {
            if (graphItem == null) return null;
            List<Table> sourceTables = (this.TableToolTipList.Count > 0 ? this.TableToolTipList : this.TableTextList);
            List<Table> panelTables = (this.Panel.TableToolTipList != null && this.Panel.TableToolTipList.Count > 0 ? this.Panel.TableToolTipList : this.Panel.TableTextList);
            Dictionary<GId, Row> cacheDict = this.TableToolTipRowDict;
            return this.GetTableInfoRow(sourceTables, panelTables, cacheDict, graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.RowGId);
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro některý GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="sourceTables">Sada tabulek, kde lze najít potřebné texty, pochází přímo z konkrétní tabulky</param>
        /// <param name="panelTables">Sada tabulek pocházející z úrovně Panel</param>
        /// <param name="cacheDict">Dictionary, kde jsou pro konkrétní GId ukládány dříve již hledané údaje (včetně hodnoty NULL, pokud neexistuje).</param>
        /// <param name="gids">Jednotlivé klíče, pro které se má řádek v tabulce hledat</param>
        /// <returns></returns>
        protected Row GetTableInfoRow(List<Table> sourceTables, List<Table> panelTables, Dictionary<GId, Row> cacheDict, params GId[] gids)
        {
            foreach (GId gId in gids)
            {
                Row row = this.GetTableInfoRowForGId(sourceTables, panelTables, cacheDict, gId);
                if (row != null) return row;
            }
            return null;
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro daný GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="sourceTables">Sada tabulek, kde lze najít potřebné texty, pochází přímo z konkrétní tabulky</param>
        /// <param name="panelTables">Sada tabulek pocházející z úrovně Panel</param>
        /// <param name="cacheDict">Dictionary, kde jsou pro konkrétní GId ukládány dříve již hledané údaje (včetně hodnoty NULL, pokud neexistuje).</param>
        /// <param name="gId">Hledaný klíč</param>
        /// <returns></returns>
        protected Row GetTableInfoRowForGId(List<Table> sourceTables, List<Table> panelTables, Dictionary<GId, Row> cacheDict, GId gId)
        {
            Row row = null;
            if (gId == null) return row;

            // Nejprve hledáme v "cache":
            if (cacheDict.TryGetValue(gId, out row))
                return row;

            // V cache nic není - budeme hledat v kompletních datech = v soupisu tabulek na úrovni MainDataTable:
            if (sourceTables != null && sourceTables.Count > 0)
            {
                foreach (Table table in sourceTables)
                {
                    if (table.TryGetRow(gId, out row))
                        break;
                }
            }

            // Pokud jsme nenašli ve zdejší MainDataTable, můžeme se podívat i na úroveň Panelu:
            if (row == null && (panelTables != null && panelTables.Count > 0))
            {
                foreach (Table table in panelTables)
                {
                    if (table.TryGetRow(gId, out row))
                        break;
                }
            }

            // Co jsme našli, dáme do cache (pro příští hledání), i kdyby to bylo NULL, a vrátíme to:
            cacheDict.Add(gId, row);
            return row;
        }
        /// <summary>
        /// Tabulky s informacemi = popisky pro položky grafů.
        /// </summary>
        public List<Table> TableTextList { get; private set; }
        /// <summary>
        /// Tabulky s informacemi = ToolTipy pro položky grafů.
        /// </summary>
        public List<Table> TableToolTipList { get; private set; }
        /// <summary>
        /// Index textů = obsahuje GId, pro který se někdy hledal text, a k němu nalezený řádek.
        /// Může rovněž obsahovat NULL pro daný GId, to když nebyl nalezen řádek.
        /// </summary>
        public Dictionary<GId, Row> TableTextRowDict { get; private set; }
        /// <summary>
        /// Index ToolTipů = obsahuje GId, pro který se někdy hledal ToolTip, a k němu nalezený řádek.
        /// Může rovněž obsahovat NULL pro daný GId, to když nebyl nalezen řádek.
        /// </summary>
        public Dictionary<GId, Row> TableToolTipRowDict { get; private set; }
        #endregion
        #region ParentChilds - Vztahy mezi řádky
        /// <summary>
        /// Metoda připraví Childs řádky pro řádky Root, podle aktuálně viditelného času.
        /// Volá se z metod, které změní obsah grafů v této tabulce, po provedení všech změn.
        /// </summary>
        public void PrepareDynamicChilds(bool invalidateTable = false)
        {
            this.PrepareDynamicChilds(this.SynchronizedTime, true);
            if (invalidateTable)
                this.GTableRow.Invalidate();
        }
        /// <summary>
        /// Metoda připraví Childs řádky pro řádky Root, podle zadaného viditelného času
        /// </summary>
        /// <param name="timeRange">Viditelný časový interval</param>
        /// <param name="force">Povinně, bez ohledu na to že pro daný čas už bylo provedeno (=když máme nová data v grafech)</param>
        protected void PrepareDynamicChilds(TimeRange timeRange, bool force)
        {
            // Test, zda je nutno akci provádět, s ohledem na režim, zadaný čas a posledně zpracovaný čas:
            SearchChildInfo searchInfo = this.CurrentSearchChildInfo;
            TimeRange timeFrame = (searchInfo.IsVisibleTimeOnly ? timeRange : this.TotalTime);
            if (!PrepareCurrentChildRowsRequired(force, searchInfo, timeFrame, this._ChildRowsLastTimeRange)) return;

            // Dynamické řádky se vždy hledají v poli TableRow.DynamicChilds, buď v this tabulce, anebo v tabulce jiné:
            MainDataTable sourceTable = this.GetDynamicChildsSource(searchInfo);
            Row[] sourceRows = this.GetDynamicChildsRow(searchInfo, sourceTable);
            if (sourceRows == null) return;

            // Pro naše Root řádky budu hledat jejich Child řádky v sourceTable a sourceRows:
            Row[] rootRows = this.TableRow.TreeNodeRootRows;
            foreach (Row rootRow in rootRows)
                this.PrepareDynamicChildsOne(rootRow, searchInfo, timeFrame, sourceTable, sourceRows);
            
            this._ChildRowsLastTimeRange = timeFrame;
        }
        /// <summary>
        /// Vrátí true, pokud je nutno za daných okolností znovu vyhodnotit vztahy Parent - Child a určit Child prvky do parentRow.TreeNodeChilds
        /// </summary>
        /// <param name="force">Požadavek na povinné provedení</param>
        /// <param name="searchInfo">Režim vztahů, určuje parametry hledání</param>
        /// <param name="currentTimeRange">Aktuální viditelný čas</param>
        /// <param name="lastTimeRange">Posledně zpracovaný viditelný čas</param>
        /// <returns></returns>
        protected static bool PrepareCurrentChildRowsRequired(bool force, SearchChildInfo searchInfo, TimeRange currentTimeRange, TimeRange lastTimeRange)
        {
            if (force) return true;                        // Je-li to povinné, pak to udělat musíme.
            if (searchInfo.IsStatic) return false;         // Jelito statické (a není to povinné), tak to dělat nemusíme.
            if (currentTimeRange == null) return false;    // Aktuální čas není dodán, a jsme v dynamickém režimu, tak to dělat nemusíme.
            if (lastTimeRange == null) return true;        // Poslední čas není uložen, asi to ještě neběželo, musí se to provést.
            return (currentTimeRange != lastTimeRange);    // Pokud čas Last a Curr jsou odlišené, musí se to provést!
        }
        /// <summary>
        /// Metoda vrací tabulku <see cref="MainDataTable"/>, z níž máme vyhledat dynamické Child řádky.
        /// Může vrátit null.
        /// </summary>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        protected MainDataTable GetDynamicChildsSource(SearchChildInfo searchInfo)
        {
            if (searchInfo.IsStatic) return null;                    // Pouze statické řádky
            if (!searchInfo.IsInOtherTable) return this;             // Zdrojem není jiná tabulka => zdrojem tedy je this tabulka
            return this.IMainData.SearchTable(searchInfo.ChildRowsTableName);  // Vyhledám zdrojovu tabulku s Child řádky (nebo vrátím null)
        }
        /// <summary>
        /// Metoda vrací pole řádků Childs ze zdrojové tabulky sourceTable, podle předpisu pro vyhledávání
        /// </summary>
        /// <param name="searchInfo"></param>
        /// <param name="sourceTable"></param>
        /// <returns></returns>
        protected Row[] GetDynamicChildsRow(SearchChildInfo searchInfo, MainDataTable sourceTable)
        {
            if (sourceTable == null) return null;

            // Ve vlastní tabulce, anebo výhradně v DynamicChilds:
            if (!searchInfo.IsInOtherTable || searchInfo.IsInDynamicChildOnly) return sourceTable.TableRow.DynamicChilds.ToArray();

            // V cizí tabulce a s požadavkem na RootRowsOnly:
            if (searchInfo.IsInOtherTable && searchInfo.IsInOtherRootRowsOnly) return sourceTable.TableRow.TreeNodeRootRows;

            // Ve všech řádcích:
            return sourceTable.TableRow.Rows.ToArray();
        }
        /// <summary>
        /// Posledně platná hodnota času při detekci v metodě <see cref="PrepareDynamicChilds(TimeRange, bool)"/>
        /// </summary>
        private TimeRange _ChildRowsLastTimeRange;
        #region Dynamicky dohledané Child řádky z dodané zdrojové tabulky
        /// <summary>
        /// Metoda najde a připraví Child řádky pro daný Parent řádek, řádky hledá v dodané cizí tabulce, do daného root řádku ukládá jejich vhodné klony
        /// </summary>
        /// <param name="parentRow">Parent řádek z this tabulky</param>
        /// <param name="searchInfo">Režim vztahů, určuje parametry hledání</param>
        /// <param name="timeFrame">Časový interval</param>
        /// <param name="sourceTable">Zdrojová tabulka</param>
        /// <param name="sourceRows">Sada všech vhodných řádků ze zdrojové tabulky, mezi nimi mohou být i Childs k danému Parentu</param>
        protected void PrepareDynamicChildsOne(Row parentRow, SearchChildInfo searchInfo, TimeRange timeFrame, MainDataTable sourceTable, Row[] sourceRows)
        {
            if (parentRow == null) return;
            parentRow.TreeNode.DynamicChilds = null;
            GId recordGId = parentRow.RecordGId;

            // Nejprve si připravím Dictionary, obsahující zdrojové vazební prvky z Parent řádku, z this tabulky:
            Dictionary<GId, TimeRange> parentDataDict = this.GetSearchPairDataFromRow(parentRow, this, searchInfo, searchInfo.ParentIdType, timeFrame);

            // Nyní vyhledám Child řádky, které mají s Parentem něco společného (nějakou aktuálně viditelnou práci):
            //  (tady jde o řádky, pocházející z Other tabulky = před jejich duplikováním)
            Row[] childRows = sourceRows
                .Where(r => TestSearchPairDataInRow(r, sourceTable, searchInfo, searchInfo.ChildIdType, timeFrame, parentDataDict))
                .ToArray();
            if (childRows.Length == 0) return;

            // Nyní provedu synchronizaci dat z childs (=ostrá data) do jejich klonu, který bude vložen do parentRow.TreeNodeChilds:
            Dictionary<GId, Row> parentRowDict = this.GetDynamicParentChildDict(recordGId);
            List<Row> cloneRows = new List<Row>();
            foreach (Row childRow in childRows)
            {   // Child řádky ze Source tabulky:
                // Najdu / vytvořím klon řádku, odpovídající řádku Child z other tabulky, ale pokud v něm bude graf, pak prvky grafu klonovat nebudeme:
                Row cloneRow = this.GetChildCloneRow(parentRowDict, sourceTable, childRow);
                if (cloneRow != null)
                {   // Do klonu řádku vložím odpovídající prvky grafu.
                    // K tomu malé vysvětlení:
                    //  - řádek v proměnné sourceChild je ze zdrojové (other) tabulky, a obsahuje graf s kompletní a platnou sadou prvků
                    //  - řádek v proměnné cloneChild je vytvořený Clone z řádku sourceChild, jeho trvanlivost je dlouhodobá (od prvního vytvoření do zavření celého okna)
                    //  - řádek v proměnné cloneChild má obsahovat aktuálně platné prvky grafu z grafu v řádku sourceChild
                    //  - graf v řádku cloneChild po vytvoření neobsahuje nic, po přesunu časové osy obsahuje zastaralé údaje (prvky mimo aktuální čas)
                    //  - nyní musíme do řádku cloneChild nasynchronizovat prvky z grafu v řádku sourceChild:
                    this.SynchronizeChildGraphItems(parentDataDict, searchInfo, timeFrame, childRow, cloneRow);

                    cloneRows.Add(cloneRow);
                }
            }
            parentRow.TreeNode.DynamicChilds = cloneRows.ToArray();
        }
        /// <summary>
        /// Metoda najde prvky grafu daného řádku v daném období a vrátí jejich Dictionary.
        /// Hledá jen ty prvky, které mají platný požadovaný identifikátor (idType).
        /// Obecněji: metoda typicky vrátí seznam (Dictionary) prvků grafu v daném řádku, které jsou v daném časovém intervalu zobrazeny.
        /// Prvek grafu je zadán dodaným identifikátorem (idType), najdou se jeho výskyty v daném období a pokud jich bude více (typicky GroupId, DataId), budou jejich časy sečteny.
        /// Pokud identifikátorem je RowId, pak se do výstupu dostane pouze jeho GId, a jako Value je uveden zadaný čas timeFrame.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="dataTable"></param>
        /// <param name="searchInfo"></param>
        /// <param name="idType">Druh klíče</param>
        /// <param name="timeFrame"></param>
        /// <returns></returns>
        protected Dictionary<GId, TimeRange> GetSearchPairDataFromRow(Row row, MainDataTable dataTable, SearchChildInfo searchInfo, DataGraphItem.IdType idType, TimeRange timeFrame)
        {
            if (row == null || row.RecordGId == null || idType == DataGraphItem.IdType.None) return null;
            GTimeGraph graph;
            if (!dataTable.TimeGraphDict.TryGetValue(row.RecordGId, out graph)) return null;

            Dictionary<GId, TimeRange> resultDict = new Dictionary<GId, TimeRange>();

            // Jako podklad slouží celý řádek, nikoli jeho grafické prvky:
            if (idType == DataGraphItem.IdType.Row)
            {
                resultDict.Add(row.RecordGId, timeFrame);
                return resultDict;
            }

            // Jako podklad slouží nějaký identifikátor grafického prvku:
            foreach (ITimeGraphItem iItem in graph.VisibleGraphItems)
            {
                DataGraphItem gItem = (iItem as DataGraphItem);
                if (gItem == null) continue;
                GId gId = gItem.GetGId(idType);
                if (gId == null || gId.IsEmpty) continue;

                TimeRange itemTime = gItem.Time;
                if (!timeFrame.HasIntersect(itemTime)) continue;

                TimeRange groupTime;
                if (resultDict.TryGetValue(gId, out groupTime))
                    resultDict[gId] = (groupTime + itemTime);
                else
                    resultDict.Add(gId, itemTime);
            }

            return resultDict;
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný řádek má být dostupný jako Child řádek v daném čase
        /// </summary>
        /// <param name="childRow">Potenciální child řádek, který testujeme zda bude Child řádkem určitého Parenta</param>
        /// <param name="childTable">Datová tabulka, z níž pochází tento Child řádek</param>
        /// <param name="searchInfo">Režim vztahů, určuje parametry hledání</param>
        /// <param name="idType"></param>
        /// <param name="timeFrame"></param>
        /// <param name="parentDataDict">Prvky grafu v řádku Parent v daném čase, může být null</param>
        /// <returns></returns>
        protected bool TestSearchPairDataInRow(Row childRow, MainDataTable childTable, SearchChildInfo searchInfo, DataGraphItem.IdType idType, TimeRange timeFrame, Dictionary<GId, TimeRange> parentDataDict)
        {
            if (searchInfo.IsStatic) return true;
            if (parentDataDict == null || parentDataDict.Count == 0 || timeFrame == null) return false;

            // Najdeme prvky grafu v řádku Child v zadané době (anebo celý řádek Child, podle idType):
            Dictionary<GId, TimeRange> childDataDict = this.GetSearchPairDataFromRow(childRow, childTable, searchInfo, idType, timeFrame);
            if (childDataDict == null || childDataDict.Count == 0) return false;

            // Enumerovat budeme skrz kratší kolekci:
            bool shortIsParent = (parentDataDict.Count < childDataDict.Count);
            var shortDict = (shortIsParent ? parentDataDict : childDataDict);
            var longDict = (!shortIsParent ? parentDataDict : childDataDict);

            // Budeme testovat časový průsečík?
            bool testIntersect = searchInfo.IsParentChildIntersectTimeOnly;

            // A pokud najdeme alespoň jeden, který má stejný klíč a mají společný čas, pak vrátíme true:
            foreach (var shortItem in shortDict)
            {
                GId key = shortItem.Key;
                TimeRange longTime;
                if (longDict.TryGetValue(key, out longTime) && (!testIntersect || (testIntersect && longTime.HasIntersect(shortItem.Value))))
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Metoda vrací Dictionary, která obsahuje pro daný GId parenta jeho existující klony Child řádků.
        /// Využívá instanční proměnnou <see cref="OtherChildRowDict"/>, kde jsou tyto klony permanentně uloženy.
        /// Pokud pro daného parenta ještě neexistuje Dictionary, bude založena a uložena do paměti.
        /// </summary>
        /// <param name="parentGId"></param>
        /// <returns></returns>
        protected Dictionary<GId, Row> GetDynamicParentChildDict(GId parentGId)
        {
            if (this.OtherChildRowDict == null)
                this.OtherChildRowDict = new Dictionary<GId, Dictionary<GId, Row>>();

            // Pro daný GID parenta najdu nebo vytvořím jeho odpovídající Dictionary, která bude obsahovat jeho soukromé klony Child řádků:
            Dictionary<GId, Row> parentDict = this.OtherChildRowDict.GetAdd(parentGId, g => new Dictionary<GId, Row>());
            return parentDict;
        }
        /// <summary>
        /// Metoda vrátí Row, který je klonem dodaného childRow, jenž je umístěn v jiné tabulce než v this.
        /// Tato metoda vytvořený Child řádek nevkládá do this tabulky, 
        /// </summary>
        /// <param name="parentDict">Data z Parent řádku</param>
        /// <param name="sourceTable">Tabulka, která je zdrojem dat</param>
        /// <param name="sourceRow">Řádek, který se zde bude klonovat</param>
        /// <returns></returns>
        protected Row GetChildCloneRow(Dictionary<GId, Row> parentDict, MainDataTable sourceTable, Row sourceRow)
        {
            GId childGId = sourceRow.RecordGId;
            Row cloneRow = parentDict.GetAdd(childGId, g => this.OtherChildCreateCloneRow(sourceTable, sourceRow));
            return cloneRow;
        }
        /// <summary>
        /// Metoda vytvoří new instanci Row a překopíruje do ní data z dodaného řádku. Tento klon umístí do this tabulky, jako Child řádek.
        /// </summary>
        /// <param name="sourceTable">Tabulka, která je zdrojem dat</param>
        /// <param name="sourceRow"></param>
        /// <returns></returns>
        protected Row OtherChildCreateCloneRow(MainDataTable sourceTable, Row sourceRow)
        {
            TableRowCloneArgs cloneArgs = new TableRowCloneArgs() { CloneGraphItems = false, CloneRowTagItems = false };
            Row cloneRow = new Row(sourceRow, cloneArgs);                 // V rámci klonování řádku se u jeho grafu nebude provádět přenos položek grafů, ani TagItems
            cloneRow.Control = null;
            cloneRow.ParentRecordGId = GId.Empty;                         // Dynamické Child řádky mají ParentRecordGId = Empty
            GTimeGraph cloneGraph = SearchGraphInRow(cloneRow);
            if (cloneGraph != null) cloneGraph.DataSource = sourceTable;  // Zdrojem dat v klonovaném grafu je původní zdrojová tabulka (obsahuje texty, tooltipy...)
            this.TableRow.AddRow(cloneRow);
            return cloneRow;
        }
        /// <summary>
        /// Metoda zajistí překopírování prvků grafu z grafu v řádku sourceChild do grafu v řádku targetChild.
        /// Překopíruje pouze ty prvky, které odpovídají předpisu pro hledání v <see cref="SearchChildInfo"/> a aktuálním datům v Parent řádku (parentDataDict).
        /// </summary>
        /// <param name="parentDataDict"></param>
        /// <param name="searchInfo"></param>
        /// <param name="timeFrame"></param>
        /// <param name="sourceChild"></param>
        /// <param name="targetChild"></param>
        protected void SynchronizeChildGraphItems(Dictionary<GId, TimeRange> parentDataDict, SearchChildInfo searchInfo, TimeRange timeFrame, Row sourceChild, Row targetChild)
        {
            GTimeGraph sourceGraph = SearchGraphInRow(sourceChild);
            if (sourceGraph == null) return;
            GTimeGraph targetGraph = SearchGraphInRow(targetChild);
            if (targetGraph == null) return;

            // V cílovém grafu (targetGraph) může už být několik prvků grafu od posledního zobrazení; všechny skryjeme:
            targetGraph.ModifyGraphItems(i => i.IsVisible = false);

            // Nyní ze zdrojového grafu vyhledáme prvky grafu, které mají být synchronizovány:
            foreach (ITimeGraphItem sourceItem in sourceGraph.AllGraphItems)
            {
                if (!this.OtherChildFilterGraphItem(sourceItem, parentDataDict, searchInfo)) continue;

                // Prvek (sourceItem) má být viditelný v targetGraph:
                ITimeGraphItem targetItem = this.OtherChildSyncGraphItem(sourceItem, targetGraph, targetChild);
                if (targetItem != null)
                    targetItem.IsVisible = true;
            }
        }
        /// <summary>
        /// Metoda najde a vrátí graf <see cref="GTimeGraph"/> z dodaného řádku tabulky.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected static GTimeGraph SearchGraphInRow(Row row)
        {
            if (row == null) return null;
            if (row.BackgroundValueType == TableValueType.ITimeInteractiveGraph) return row.BackgroundValue as GTimeGraph;
            Cell cell = row.Cells.FirstOrDefault(c => c.ValueType == TableValueType.ITimeInteractiveGraph);
            return (cell != null ? cell.Value as GTimeGraph : null);
        }
        /// <summary>
        /// Metoda slouží jako filtr položek grafu v Child řádku, při klonování obsahu grafu z původního Child řádku do nově vytvářeného klonu tohoto grafu.
        /// Na vstupu je prvek grafu ze zdrojového grafu (který se zde testuje),
        /// dále soupis prvků z Parent grafu, které slouží jako základna pro filtrování,
        /// a pravidla pro vyhledání Child řádku (převzatá z Gui dat).
        /// Výstupem je true = daný vstupní prvek grafu se má do výstupu překopírovat; false = nemá.
        /// </summary>
        /// <param name="iItem"></param>
        /// <param name="parentDataDict"></param>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        protected bool OtherChildFilterGraphItem(ITimeGraphItem iItem, Dictionary<GId, TimeRange> parentDataDict, SearchChildInfo searchInfo)
        {
            // Máme-li provést filtrování položek grafu podle předpisů v SearchChildInfo, budeme potřebovat údaje o položce grafu typu Scheduler.DataGraphItem:
            DataGraphItem gItem = (iItem as DataGraphItem);
            if (gItem == null) return false;

            // Podle třídy prvku můžeme určit režim filtrování:
            GId itemGId = gItem.ItemGId;
            if (itemGId == null || itemGId.ClassId == 0) return true;          // Tohle je nouzová cesta. Prvky by měly mít ItemGId včetně čísla třídy. Nouzovka = zobrazit.
            ItemClassCopyBehavior copyBehavior = searchInfo.GetItemCopyBehavior(itemGId.ClassId);
            if (copyBehavior == ItemClassCopyBehavior.Always) return true;     // Konstantní hodnota, bez dalšího testování vzhledem k Parent datům
            if (copyBehavior == ItemClassCopyBehavior.None) return false;

            // Umíme zpracovat jen čtyři hodnoty (None, Always, ExistsPair, SynchronPair), ostatní chování neumíme:
            if (!(copyBehavior == ItemClassCopyBehavior.ExistsPair || copyBehavior == ItemClassCopyBehavior.SynchronPair)) return false;

            // Pro hodnoty ItemClassCopyBehavior.ExistsPair a SynchronPair musíme dohledat odpovídající prvek v Parent řádku:
            // Z prvku grafu přečteme jeho patřičné ID podle předpisu searchInfo.ChildIdType:
            GId gId = gItem.GetGId(searchInfo.ChildIdType);
            if (gId == null || gId.IsEmpty) return false;

            // Pokud takový prvek NEMÁME v parent datech, NEBUDEME jej kopírovat (poznámka: prvek může být v parentDataDict přítomen, a mít čas null, to řešíme později):
            TimeRange parentTime;
            if (!parentDataDict.TryGetValue(gId, out parentTime)) return false;

            // Máme tedy ověřeno, že Parent a Child řádek obsahují prvek grafu, který má v obou řádcích shodný daný identifikátor.

            // Pokud stačí, že prvky existují, ale NEMUSÍ mít společný časový průsečík, 
            //  pak nám postačuje už jen fakt, že oba řádky obsahují prvek se shodným ID, a testovaný prvek do grafu PŘIDÁME:
            if (copyBehavior == ItemClassCopyBehavior.ExistsPair) return true;

            // Prvky grafu (v Parent a Child řádku) musí mít společný časový průsečík:
            if (parentTime == null || gItem.Time == null) return false;
            TimeRange intersect = (parentTime * gItem.Time);
            return (intersect != null && intersect.Size.HasValue && intersect.Size.Value.Ticks > 0L);
        }
        /// <summary>
        /// Metoda zajistí synchronizaci prvku grafu sourceItem do cílového grafu targetGraph.
        /// Vrátí synchronizovaný prvek.
        /// </summary>
        /// <param name="sourceItem"></param>
        /// <param name="targetGraph"></param>
        /// <param name="targetRow"></param>
        /// <returns></returns>
        protected ITimeGraphItem OtherChildSyncGraphItem(ITimeGraphItem sourceItem, GTimeGraph targetGraph, Row targetRow)
        {
            DataGraphItem sourceData = sourceItem as DataGraphItem;
            if (sourceData == null) return null;

            // Najdeme existující prvek grafu v targetGraph, anebo vytvoříme klon prvku ze source prvku:
            DataGraphItem targetData;
            ITimeGraphItem targetItem;

            // Prvek grafu má svoje konstantní ItemGId (ve třídě DataGraphItem), k němuž se generuje ItemId pomocí this tabulky:
            int itemId = this.GetId(sourceData.ItemGId);
            if (targetGraph.TryGetGraphItem(itemId, out targetItem))
            {
                targetData = targetItem as DataGraphItem;
                if (targetData != null)
                {   // Synchronizace hodnot:
                    targetData.RowGId = targetRow.RecordGId;
                    targetData.GroupGId = sourceData.GroupGId;
                    targetData.DataGId = sourceData.DataGId;
                    targetData.Time = sourceData.Time;
                }
            }
            else
            {   // Klonování prvku grafu:
                targetData = DataGraphItem.CreateFrom(this, sourceData.GuiGraphItem);
                if (targetData != null)
                {   // Vstupní data jsou platná:
                    targetData.Time = sourceData.Time;
                    targetData.BehaviorMode = (sourceData.BehaviorMode & GraphItemBehaviorMode.AllEnabledForChildRows);
                    targetGraph.AddGraphItem(targetData);
                    targetItem = targetData;
                }
            }

            return targetItem;
        }
        /// <summary>
        /// Dictionary obsahující Child řádky k našim Root řádkům, pocházející z jiné tabulky.
        /// Tato Dictionary obsahuje klony řádků i klony grafů.
        /// </summary>
        protected Dictionary<GId, Dictionary<GId, Row>> OtherChildRowDict;
        #endregion
        #region Režim hledání Childs pro Parent - režim, analýza atd.
        /// <summary>
        /// Připraví v this tabulce data pro vyhledávání Child řádků
        /// </summary>
        private void _PrepareRowSearchChild()
        {
            this._CurrentSearchChildInfo = SearchChildInfo.CreateForProperties(this.GuiGrid.GridProperties);
        }
        /// <summary>
        /// Analyzovaný režim <see cref="GuiChildRowsEvaluateMode"/>.
        /// Property je autoinicializační, výchozí hodnota je <see cref="SearchChildInfo.Static"/>.
        /// </summary>
        protected SearchChildInfo CurrentSearchChildInfo
        {
            get { if (this._CurrentSearchChildInfo == null) this._CurrentSearchChildInfo = SearchChildInfo.Static; return this._CurrentSearchChildInfo; }
        }
        /// <summary>
        /// Proměnná pro <see cref="CurrentSearchChildInfo"/>
        /// </summary>
        private SearchChildInfo _CurrentSearchChildInfo;
        /// <summary>
        /// Třída obsahující zpracované informace z <see cref="GuiChildRowsEvaluateMode"/>
        /// </summary>
        protected class SearchChildInfo
        {
            /// <summary>
            /// Vrací new instanci pro dané zadání <see cref="GuiGridProperties"/>
            /// </summary>
            /// <param name="properties"></param>
            /// <returns></returns>
            public static SearchChildInfo CreateForProperties(GuiGridProperties properties)
            {
                if (properties == null) return SearchChildInfo.Static;

                return CreateForData(properties.ChildRowsEvaluate, properties.ChildRowsTableName, properties.ChildRowsCopyClassesMode);
            }
            /// <summary>
            /// Vrací new instanci pro daný režim
            /// </summary>
            /// <param name="mode">Režim</param>
            /// <param name="otherTable">Jiná zdrojová tabulka</param>
            /// <param name="copyClasses">Čísla tříd prvků, které se kopírují bez ohledu na párovost Child - Parent</param>
            /// <returns></returns>
            public static SearchChildInfo CreateForData(GuiChildRowsEvaluateMode? mode, string otherTable, string copyClasses)
            {
                bool hasOtherTable = !String.IsNullOrEmpty(otherTable);

                SearchChildInfo info = new SearchChildInfo();
                if (mode.HasValue && mode.Value != GuiChildRowsEvaluateMode.Static)
                {
                    GuiChildRowsEvaluateMode m = mode.Value;
                    info.Mode = m;
                    info.IsStatic = false;
                    info.ParentIdType = (m.HasFlag(GuiChildRowsEvaluateMode.OnParentItem) ? DataGraphItem.IdType.Item :
                                        (m.HasFlag(GuiChildRowsEvaluateMode.OnParentGroup) ? DataGraphItem.IdType.Group :
                                        (m.HasFlag(GuiChildRowsEvaluateMode.OnParentData) ? DataGraphItem.IdType.Data :
                                        (m.HasFlag(GuiChildRowsEvaluateMode.OnParentRow) ? DataGraphItem.IdType.Row : DataGraphItem.IdType.None))));
                    info.ChildIdType =  (m.HasFlag(GuiChildRowsEvaluateMode.ToChildItem) ? DataGraphItem.IdType.Item :
                                        (m.HasFlag(GuiChildRowsEvaluateMode.ToChildGroup) ? DataGraphItem.IdType.Group :
                                        (m.HasFlag(GuiChildRowsEvaluateMode.ToChildData) ? DataGraphItem.IdType.Data :
                                        (m.HasFlag(GuiChildRowsEvaluateMode.ToChildRow) ? DataGraphItem.IdType.Row : DataGraphItem.IdType.None))));
                    info.IsVisibleTimeOnly = m.HasFlag(GuiChildRowsEvaluateMode.VisibleTimeOnly);
                    info.IsParentChildIntersectTimeOnly = m.HasFlag(GuiChildRowsEvaluateMode.ParentChildIntersectTimeOnly);
                    info.IsInOtherTable = m.HasFlag(GuiChildRowsEvaluateMode.InOtherTable) || m.HasFlag(GuiChildRowsEvaluateMode.InOtherRootRowsOnly);
                    info.IsInOtherRootRowsOnly = m.HasFlag(GuiChildRowsEvaluateMode.InOtherRootRowsOnly);
                    info.IsInDynamicChildOnly = m.HasFlag(GuiChildRowsEvaluateMode.InDynamicChildOnly);
                }

                info.ChildRowsTableName = ((hasOtherTable && info.IsInOtherTable) ? otherTable.Trim() : null);
                info.CopyAllItemFromClasses = ParseCopyClasses(copyClasses);

                return info;
            }
            /// <summary>
            /// Obsahuje new instanci definující statickou vazbu
            /// </summary>
            public static SearchChildInfo Static { get { return new SearchChildInfo(); } }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            private SearchChildInfo()
            {
                this.Mode = GuiChildRowsEvaluateMode.Static;
                this.IsStatic = true;
                this.ParentIdType = DataGraphItem.IdType.None;
                this.ChildIdType = DataGraphItem.IdType.None;
                this.IsVisibleTimeOnly = false;
                this.IsParentChildIntersectTimeOnly = false;
                this.IsInOtherTable = false;
                this.IsInOtherRootRowsOnly = false;
                this.IsInDynamicChildOnly = false;
                this.CopyAllItemFromClasses = null;
            }
            /// <summary>
            /// Režim
            /// </summary>
            public GuiChildRowsEvaluateMode Mode { get; private set; }
            /// <summary>
            /// Zdrojová tabulka pro Child řádky, pokud v ChildRowsEvaluate je nastaven bit <see cref="GuiChildRowsEvaluateMode.InOtherTable"/>
            /// </summary>
            public string ChildRowsTableName { get; private set; }
            /// <summary>
            /// true pokud vztah je čistě statický
            /// </summary>
            public bool IsStatic { get; private set; }
            /// <summary>
            /// Druh identifikátoru v Parent prvku
            /// </summary>
            public DataGraphItem.IdType ParentIdType { get; private set; }
            /// <summary>
            /// Druh identifikátoru v Child prvku
            /// </summary>
            public DataGraphItem.IdType ChildIdType { get; private set; }
            /// <summary>
            /// true = zpracovat pouze viditelné období / false = brát všechno
            /// </summary>
            public bool IsVisibleTimeOnly { get; private set; }
            /// <summary>
            /// true = spárovat prvky pouze pokud mají časový průsečík / false = brát všechno
            /// </summary>
            public bool IsParentChildIntersectTimeOnly { get; private set; }
            /// <summary>
            /// true = Hledat Child řádky v jiné tabulce (její název je určen v property <see cref="GuiGridProperties.ChildRowsTableName"/>).
            /// Nalezený řádek z Child tabulky bude do this tabulky zkopírován (nebude do ní referencován) = vznikne new instance.
            /// Duplikování se provede i pro případný graf a jeho položky.
            /// </summary>
            public bool IsInOtherTable { get; private set; }
            /// <summary>
            /// true = Hledat Child řádky pouze mezi Root řádky v jiné tabulce (její název je určen v property <see cref="GuiGridProperties.ChildRowsTableName"/>).
            /// K tomuto bitu může i nemusí být nastaven bit <see cref="IsInOtherTable"/>
            /// </summary>
            public bool IsInOtherRootRowsOnly { get; private set; }
            /// <summary>
            /// true = Hledat Child řádky pouze mezi DynamicChild řádky.
            /// K tomuto bitu může i nemusí být nastaven bit <see cref="IsInOtherTable"/>
            /// </summary>
            public bool IsInDynamicChildOnly { get; private set; }
            /// <summary>
            /// Definice režimu kopírování prvků grafu podle čísla třídy
            /// </summary>
            protected Dictionary<int, ItemClassCopyBehavior> CopyAllItemFromClasses;
            /// <summary>
            /// Z textu <see cref="GuiGridProperties.ChildRowsCopyClassesMode"/> vytvoří a vrátí Dictionary, kde Key = číslo třídy a Value = režim kopírování
            /// </summary>
            /// <param name="copyClasses"></param>
            /// <returns></returns>
            protected static Dictionary<int, ItemClassCopyBehavior> ParseCopyClasses(string copyClasses)
            {
                Dictionary<int, ItemClassCopyBehavior> result = new Dictionary<int, ItemClassCopyBehavior>();
                if (!String.IsNullOrEmpty(copyClasses))
                {
                    KeyValuePair<string, string>[] items = copyClasses.ToKeyValues(";", ":", true, true);
                    foreach (KeyValuePair<string, string> item in items)
                    {
                        // Číslo třídy "0" je povolená hodnota a slouží jako default pro "všechny nezadané třídy":
                        int classNumber = 0;
                        if (String.IsNullOrEmpty(item.Key) || !Int32.TryParse(item.Key, out classNumber) || (item.Key != "0" && classNumber == 0)) continue;
                        ItemClassCopyBehavior behavior = ParseCopyClassesBehavior(item.Value);
                        if (result.ContainsKey(classNumber))
                            result[classNumber] = behavior;
                        else
                            result.Add(classNumber, behavior);
                    }
                }

                return result;
            }
            /// <summary>
            /// Metoda vrací režim kopírování prvků grafu <see cref="ItemClassCopyBehavior"/> pro daný text.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            protected static ItemClassCopyBehavior ParseCopyClassesBehavior(string value)
            {
                if (String.IsNullOrEmpty(value)) return ItemClassCopyBehavior.Default;
                string text = value.Trim().ToLower();
                switch (text)
                {
                    case "n":
                    case "none":
                        return ItemClassCopyBehavior.None;
                    case "a":
                    case "always":
                        return ItemClassCopyBehavior.Always;
                    case "e":
                    case "existspair":
                        return ItemClassCopyBehavior.ExistsPair;
                    case "s":
                    case "synchronpair":
                        return ItemClassCopyBehavior.SynchronPair;
                }
                return ItemClassCopyBehavior.Default;
            }
            /// <summary>
            /// Metoda vrátí režim kopírování prvku grafu pro danou třídu, podle definice kterou má graf vepsanou v <see cref="GuiGridProperties.ChildRowsCopyClassesMode"/>.
            /// Používá se tehdy, když Child řádky pro určitý Parent řádek se hledají v jiné tabulce (<see cref="IsInOtherTable"/>),
            /// a do aktuální tabulky se tedy přenáší duplikát řádku včetně duplikátu grafu. Pak lze řídit viditelnost zkopírovaných prvků grafu 
            /// na základě jejich vlastností i s ohledem na Parent řádek.
            /// </summary>
            /// <param name="classNumber"></param>
            /// <param name="defaultValue"></param>
            /// <returns></returns>
            public ItemClassCopyBehavior GetItemCopyBehavior(int classNumber, ItemClassCopyBehavior defaultValue = ItemClassCopyBehavior.Always)
            {
                if (this.CopyAllItemFromClasses == null) return defaultValue;
                ItemClassCopyBehavior behavior;
                if (classNumber != 0 && this.CopyAllItemFromClasses.TryGetValue(classNumber, out behavior)) return behavior;     // Pro konkrétní zadanou třídu
                if (this.CopyAllItemFromClasses.TryGetValue(0, out behavior)) return behavior;                                   // Definice pro "všechny ostatní třídy"
                return defaultValue;
            }
        }
        /// <summary>
        /// Režim kopírování prvků grafu v závislosti na čísle třídy
        /// </summary>
        protected enum ItemClassCopyBehavior
        {
            /// <summary>
            /// Nedefinováno, použije se Always
            /// </summary>
            Default = 0,
            /// <summary>
            /// "None" = nepřenášet nikdy
            /// </summary>
            None,
            /// <summary>
            /// "Always" = přenášet vždy
            /// </summary>
            Always,
            /// <summary>
            /// "ExistsPair" = přenášet, jen když v Parent řádku existuje shodný prvek bez ohledu na synchronní čas
            /// </summary>
            ExistsPair,
            /// <summary>
            /// "SynchronPair" = přenášet, jen když jsou synchronní časy (v Parent řádku existuje shodný prvek s časem společným s prvekm v Child řádku)
            /// </summary>
            SynchronPair
        }
        #endregion
        #endregion
        #region Podpora pro mezitabulkové interakce (kdy akce v jedné tabulce vyvolá jinou akci v jiné tabulce)
        /// <summary>
        /// Všechny interakce deklarované pro tuto tabulku. Zde nikdy není null.
        /// Pokud nejsou definovány (jsou null), pak zde je prázdné pole.
        /// </summary>
        public GuiGridInteraction[] Interactions
        {
            get
            {
                List<GuiGridInteraction> list = this.GuiGrid.GridProperties?.InteractionList;
                return (list != null ? list.ToArray() : new GuiGridInteraction[0]);
            }
        }
        #region Interakce, algoritmy na straně Source
        /// <summary>
        /// Obecná metoda, která má provést všechny nyní aktivní interakce této tabulky.
        /// </summary>
        internal void RunInteractionThisSource(GridInteractionRunInfo[] runInteractions, ref bool callRefresh)
        {
            if (runInteractions == null || runInteractions.Length == 0) return;
            runInteractions = this.GetInteractionsForCurrentState(runInteractions).ToArray();
            if (runInteractions == null || runInteractions.Length == 0) return;

            Row activeRow = this.TableRow.ActiveRow;
            Row[] checkedRows = this.TableRow.CheckedRows;
            bool isOnlyActivadedRow = (checkedRows.Length == 0);

            this.InteractionThisSource(runInteractions, activeRow, checkedRows, null, null);

            callRefresh = true;
        }
        /// <summary>
        /// Souhrn všech definic interakcí z <see cref="GuiGrid.GridProperties"/>
        /// </summary>
        protected List<GuiGridInteraction> AllInteractions { get { return this.GuiGrid?.GridProperties?.InteractionList; } }
        /// <summary>
        /// Metoda najde a vrátí pole, obsahující definice interakcí pro danou vstupní akci.
        /// Vrácené pole může být null (když neexistuje žádná definice, nebo když žádná existující definice se nehodí pro danou akci).
        /// Pokud vrácené pole není null, pak obsahuje přinejmenším jednu položku.
        /// </summary>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <returns></returns>
        private GridInteractionRunInfo[] GetInteractions(SourceActionType currentSourceAction)
        {
            List<GuiGridInteraction> interactionList = this.AllInteractions;
            if (interactionList == null || interactionList.Count == 0) return null;
            GridInteractionRunInfo[] runInteractions = interactionList
                .Where(i => _IsInteractionForAction(i, currentSourceAction))
                .Select(i => new GridInteractionRunInfo(i, currentSourceAction, null))
                .ToArray();

            // Podmíněné interakce = takové, které jsou aktivní pouze za určitého stavu ToolBaru:
            runInteractions = this.GetInteractionsForCurrentState(runInteractions).ToArray();

            return (runInteractions.Length > 0 ? runInteractions : null);
        }
        /// <summary>
        /// Vrátí true, pokud daná interakce má být použita pro danou zdrojovou akci
        /// </summary>
        /// <param name="guiGridInteraction"></param>
        /// <param name="currentSourceAction"></param>
        /// <returns></returns>
        private static bool _IsInteractionForAction(GuiGridInteraction guiGridInteraction, SourceActionType currentSourceAction)
        {
            if ((guiGridInteraction.SourceAction & currentSourceAction) == 0) return false;
            return true;
        }
        /// <summary>
        /// Metoda vrátí interakce platné jen pro aktuální stav dat (=ToolBaru a konfigurace).
        /// Zjistí, zda dané interakce obsahují podmínku, a pokud ano pak ji vyhodnotí.
        /// </summary>
        /// <param name="runInteractions"></param>
        /// <returns></returns>
        private GridInteractionRunInfo[] GetInteractionsForCurrentState(IEnumerable<GridInteractionRunInfo> runInteractions)
        {
            if (runInteractions == null) return null;
            if (!runInteractions.Any(i => i.GuiGridInteraction.IsConditional)) return runInteractions.ToArray();

            Dictionary<string, GuiToolbarItem> toolBarDict = this.IMainData.GuiData.ToolbarItems.Items
                .Where(t => (t.IsCheckable.HasValue && t.IsCheckable.Value))
                .GetDictionary(t => t.Name, true);

            List<GridInteractionRunInfo> runList = new List<GridInteractionRunInfo>();
            foreach (GridInteractionRunInfo runInfo in runInteractions)
            {
                if (IsInteractionForCurrentCondition(runInfo, toolBarDict))
                    runList.Add(runInfo);
            }

            return runList.ToArray();
        }
        /// <summary>
        /// Metoda vrací true, pokud se daná interakce má použít za stavu, kdy máme v tolbaru zaškrtnuté některé prvky
        /// </summary>
        /// <param name="runInfo"></param>
        /// <param name="toolBarDict"></param>
        /// <returns></returns>
        private bool IsInteractionForCurrentCondition(GridInteractionRunInfo runInfo, Dictionary<string, GuiToolbarItem> toolBarDict)
        {
            if (!runInfo.IsConditional || String.IsNullOrEmpty(runInfo.Conditions)) return true;        // Bez podmínky = vyhovuje, použije se.

            string[] conditions = runInfo.Conditions.Split(";, ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            // Pokud daný název podmínky odpovídá některému Toolbaru, a tento existuje a je zaškrtnutý, vrátíme true:
            foreach (string condition in conditions)
            {
                GuiToolbarItem toolBarItem;
                if (toolBarDict.TryGetValue(condition.Trim(), out toolBarItem) && toolBarItem.IsChecked.HasValue && toolBarItem.IsChecked.Value)
                    return true;
            }

            // Jsme v prostoru Scheduler: tady můžeme testovat i jiné zdroje stavů než jen ToolBar. 
            // ... Ale zatím to neděláme.

            return false;
        }
        /// <summary>
        /// Interakce mezi tabulkami, kde this tabulka je zdrojem interakce = zde došlo k akci Aktivace řádků:
        /// podle definic v this tabulce máme provést nějaké akce v cílových tabulkách.
        /// </summary>
        /// <param name="runInteractions">Definice interakcí, které se mají provést s danými řádky</param>
        /// <param name="activeRow">Aktivní řádek, kde došlo k akci</param>
        /// <param name="checkedRows">Aktuálně označené řádky v tabulce (Checked)</param>
        /// <param name="activeGraph">Aktivní graf</param>
        /// <param name="graphItems">Aktivní prvky grafů v této tabulce</param>
        private void InteractionThisSource(GridInteractionRunInfo[] runInteractions, Row activeRow, Row[] checkedRows, GTimeGraph activeGraph, DataGraphItem[] graphItems)
        {
            this.InteractionSelectorClear(runInteractions);
            this.InteractionRowFiltersPrepare(runInteractions);

            // Zjistíme, zda cílová strana bude vyžadovat znalost prvků grafů na zdrojové straně:
            TargetActionType targetFromSourceGraph = (TargetActionType.SearchSourceItemId | TargetActionType.SearchSourceGroupId | TargetActionType.SearchSourceDataId);
            // Na vstupu máme řadu definic interakcí, projdeme je a provedeme potřebné kroky:
            foreach (GridInteractionRunInfo runInteraction in runInteractions)
            {
                // Pokud interakce nemá definovanou cílovou akci (TargetAction je None), pak tuto interakci přeskočím:
                if (runInteraction.TargetAction == TargetActionType.None) continue;

                // Najdeme cílovou tabulku, ale pokud neexistuje, pak tuto interakci přeskočím (pokud je jméno target tabulky prázdné, pak target = this):
                MainDataTable targetTable = (!String.IsNullOrEmpty(runInteraction.TargetGridFullName) ? this.IMainData.SearchTable(runInteraction.TargetGridFullName) : this);
                if (targetTable == null) continue;
                
                // Pokud interakce má v cílové akci definovanou nějakou práci se zdrojovými prvky grafů, tak je musíme mít k dispozici:
                if (((runInteraction.TargetAction & targetFromSourceGraph) != 0) && graphItems == null)
                    graphItems = this.InteractionThisSourceGetGraphItems(activeRow, checkedRows);

                // Pokud interakce má na vstupu reflektovat pouze prvky grafů ve viditelném intervalu, řešíme to zde:
                DataGraphItem[] validGraphItems = this.InteractionThisSourceFilterItems(runInteraction, graphItems);

                // Odešleme do cílové tabulky požadavek na interakci:
                InteractionArgs args = new InteractionArgs(runInteraction, activeRow, checkedRows, activeGraph, validGraphItems);
                targetTable.InteractionThisTarget(args);
            }

            this.InteractionRowFiltersActivate(runInteractions);
        }
        /// <summary>
        /// Metoda vrátí dané prvky grafů: buď všechny, anebo pouze ty, které spadají do aktuálního viditelného času.
        /// Řídí to definice interakce, její <see cref="GuiGridInteraction.TargetAction"/>, hodnota <see cref="TargetActionType.SearchSourceVisibleTime"/>.
        /// </summary>
        /// <param name="runInteraction"></param>
        /// <param name="graphItems"></param>
        /// <returns></returns>
        private DataGraphItem[] InteractionThisSourceFilterItems(GridInteractionRunInfo runInteraction, DataGraphItem[] graphItems)
        {
            if (runInteraction == null || graphItems == null || graphItems.Length == 0) return graphItems;
            bool onlyVisibleTime = runInteraction.TargetAction.HasFlag(TargetActionType.SearchSourceVisibleTime);
            if (!onlyVisibleTime) return graphItems;
            TimeRange searchInTime = (onlyVisibleTime ? this.SynchronizedTime : null);
            if (searchInTime == null) return graphItems;

            DataGraphItem[] validGraphItems = graphItems
                .Where(i => searchInTime.HasIntersect(i.Time))
                .ToArray();
            return validGraphItems;
        }
        /// <summary>
        /// Tato metoda má za úkol provést odebrání příznaku Selected nebo Activated ze všech současně vybraných objektů, 
        /// pokud je to potřeba z hlediska dodaných definic interakcí.
        /// Potřeba je to tehdy, když:
        ///  - existuje definice interakce, která ma cíl označování prvků 
        ///     (její <see cref="GuiGridInteraction.TargetAction"/> obsahuje například <see cref="TargetActionType.SelectTargetItem"/>),
        ///  - a přitom tatáž definice nemá požadavek <see cref="TargetActionType.LeaveCurrentTarget"/> = předpokládá se označování po předešlém odznačení.
        /// </summary>
        /// <param name="runInteractions"></param>
        private void InteractionSelectorClear(GridInteractionRunInfo[] runInteractions)
        {
            if (runInteractions == null || runInteractions.Length == 0) return;

            bool clearSelected = runInteractions.Any(i => (i.TargetAction.HasFlag(TargetActionType.SelectTargetItem) && !i.TargetAction.HasFlag(TargetActionType.LeaveCurrentTarget)));
            if (clearSelected)
                this.MainControl.Selector.ClearSelected();

            bool clearActivated = runInteractions.Any(i => (i.TargetAction.HasFlag(TargetActionType.ActivateTargetItem) && !i.TargetAction.HasFlag(TargetActionType.LeaveCurrentTarget)));
            if (clearActivated)
                this.MainControl.Selector.ClearActivated();
        }
        /// <summary>
        /// Metoda zjistí, zda některé Target tabulky budou řešit Řádkový filtr, a pokud ano pak jej připraví:
        /// </summary>
        /// <param name="runInteractions"></param>
        private void InteractionRowFiltersPrepare(GridInteractionRunInfo[] runInteractions)
        {
            if (runInteractions == null || runInteractions.Length == 0) return;

            // Získám Dictionary, obsahující Distinct jména tabulek (TargetGridFullName), které jsou Target a kde akce TargetAction obsahuje FilterTargetRows:
            Dictionary<string, string> nameDict = runInteractions
                .Where(i => ((i.TargetAction & TargetActionType.FilterTargetRows) != 0))
                .Select(i => i.TargetGridFullName)
                .GetDictionary(s => s, true);

            // Příprava proběhne na všech tabulkách:
            foreach (MainDataTable table in this.IMainData.DataTables)
            {
                string fullName = table.TableName;
                bool active = (!String.IsNullOrEmpty(fullName) && nameDict.ContainsKey(fullName));
                if (active)
                    table.InteractionThisRowFilterPrepare(active);
            }
        }
        /// <summary>
        /// Metoda aktivuje Řádkový filtr v těch tabulkách, kde je připraven (=kde je aktivní)
        /// </summary>
        /// <param name="runInteractions"></param>
        private void InteractionRowFiltersActivate(GridInteractionRunInfo[] runInteractions)
        {
            foreach (MainDataTable table in this.IMainData.DataTables)
                table.InteractionThisRowFilterActivate();
        }
        /// <summary>
        /// Metoda vrátí souhrn všech prvků grafů z dodaných řádků
        /// </summary>
        /// <param name="activeRow"></param>
        /// <param name="checkedRows"></param>
        /// <returns></returns>
        private DataGraphItem[] InteractionThisSourceGetGraphItems(Row activeRow, Row[] checkedRows)
        {
            Dictionary<GId, DataGraphItem> graphItemDict = new Dictionary<GId, DataGraphItem>();

            List<Row> rows = new List<Row>();
            if (activeRow != null) rows.Add(activeRow);
            if (checkedRows != null) rows.AddRange(checkedRows);
            
            foreach (Row row in rows)
            {
                GTimeGraph gTimeGraph = this.InteractionGetGraphFromRow(row);
                if (gTimeGraph == null) continue;
                foreach (ITimeGraphItem iItem in gTimeGraph.VisibleGraphItems)
                {
                    DataGraphItem item = iItem as DataGraphItem;
                    if (item == null) continue;
                    if (!graphItemDict.ContainsKey(item.ItemGId))
                        graphItemDict.Add(item.ItemGId, item);
                }
            }

            return graphItemDict.Values.ToArray();
        }
        /// <summary>
        /// Metoda najde a vrátí graf z daného řádku.
        /// Graf může být buď v buňce, která patří do sloupce s grafem, anebo může být na pozadí.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private GTimeGraph InteractionGetGraphFromRow(Row row)
        {
            if (row == null) return null;
            if (row.BackgroundValueType == TableValueType.ITimeInteractiveGraph) return row.BackgroundValue as GTimeGraph;
            Cell cell = row.Cells.FirstOrDefault(c => c.ValueType == TableValueType.ITimeInteractiveGraph);
            if (cell == null) return null;
            return cell.Value as GTimeGraph;
        }
        #endregion
        #region Interakce, algoritmy na straně Target
        /// <summary>
        /// Vstupní metoda pro řešení interakce na straně tabulky Target.
        /// Nějaká zdrojová tabulka (typicky jiná než this) zaregistrovala akci uživatele, k této akci našla definici pro interakci, 
        /// a podle této definice má být zdejší tabulka (this) cílem této interakce.
        /// Zdrojová tabulka tedy připravila sadu dat, zabalila je do argumentu a zavolala tuto naší metodu.
        /// Zdejší tabulka má tedy rozklíčovat, co má provést jako reakci na danou akci uživatele.
        /// </summary>
        /// <param name="args"></param>
        protected void InteractionThisTarget(InteractionArgs args)
        {
            if (args == null | args.Interaction.TargetAction == TargetActionType.None) return;

            // Informace: pokud daná interakce provádí Select nebo Activate, pak předpokládá, že objekt this.MainControl.Selector je řádně připraven.
            //            To má na starosti metoda this.InteractionSelectorClear(), to nelze řešit až nyní, v rámci jedné z (mnoha) interakcí.

            // Jednotlivé zdroje dat a vyhledání jejich cílů, a provedení přiměřené reakce na zdrojovou akci:
            TargetActionType action = args.Interaction.TargetAction;

            // Interakce s takovou cílovou akcí, která vychází ze zdrojových prvků grafu (které se řeší v metodě InteractionThisTargetFrom())
            //  lze provést jen tehdy, když v argumentu byly předány prvky grafů (args.SourceGraphItems)!
            if (args.SourceGraphItems != null)
            {
                if ((action & TargetActionType.SearchSourceItemId) != 0)
                    this.InteractionThisTargetFrom(args, i => i.ItemGId);
                if ((action & TargetActionType.SearchSourceGroupId) != 0)
                    this.InteractionThisTargetFrom(args, i => i.GroupGId);
                if ((action & TargetActionType.SearchSourceDataId) != 0)
                    this.InteractionThisTargetFrom(args, i => i.DataGId);
                if ((action & TargetActionType.SearchSourceRowId) != 0)
                    this.InteractionThisTargetFrom(args, i => i.RowGId);
            }

            if ((action & TargetActionType.ActivateGraphSkin) != 0)
                this.InteractionThisProcessActivateGraphSkin(args);

        }
        /// <summary>
        /// Metoda najde ve vstupních grafických prvcích <see cref="InteractionArgs.SourceGraphItems"/> 
        /// patřičné klíče zadaných grafických prvků (pomocí keySelectoru),
        /// poté pro tyto klíče najde grafické prvky ve zdejším grafu - podle volby <see cref="GuiGridInteraction.TargetAction"/>,
        /// a pro tyto nalezené zdejší prvky provede akci definovanou tamtéž.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="keySelector"></param>
        protected void InteractionThisTargetFrom(InteractionArgs args, Func<DataGraphItem, GId> keySelector)
        {
            Dictionary<GId, DataGraphItem> sourceGIds = args.SourceGraphItems.GetDictionary(keySelector, true);   // Klíče (GId) ze zdrojových prvků, podle selectoru, distinct
            GRow[] targetRows = this.InteractionThisSearchRowBy(args, sourceGIds);                 // Najde řádky podle dodaných klíčů
            GTimeGraphGroup[] targetGroups = this.InteractionThisSearchGroupBy(args, sourceGIds);  // Najde grupy grafických prvků podle dodaných klíčů
            this.InteractionThisProcessAction(args, targetRows, targetGroups);
        }
        /// <summary>
        /// Metoda vrátí pole řádků tabulky <see cref="GRow"/>, které odpovídají vstupním datům.
        /// </summary>
        /// <param name="args">Data aktuální interakce</param>
        /// <param name="sourceGIds">Klíče (GId) ze zdrojových prvků</param>
        /// <returns></returns>
        protected GRow[] InteractionThisSearchRowBy(InteractionArgs args, Dictionary<GId, DataGraphItem> sourceGIds)
        {
            Dictionary<GId, GRow> rowDict = new Dictionary<GId, GRow>();

            TargetActionType action = args.Interaction.TargetAction;
            if ((action & TargetActionType.SearchTargetRowId) != 0)
                this.InteractionThisSearchRowInItems(sourceGIds, rowDict);

            return rowDict.Values.ToArray();
        }
        /// <summary>
        /// Metoda vrátí pole skupin grafických prvků <see cref="GTimeGraphGroup"/>, které odpovídají vstupním datům.
        /// </summary>
        /// <param name="args">Data aktuální interakce</param>
        /// <param name="sourceGIds">Klíče (GId) ze zdrojových prvků</param>
        /// <returns></returns>
        protected GTimeGraphGroup[] InteractionThisSearchGroupBy(InteractionArgs args, Dictionary<GId, DataGraphItem> sourceGIds)
        {
            Dictionary<GId, GTimeGraphGroup> groupDict = new Dictionary<GId, GTimeGraphGroup>();

            TargetActionType action = args.Interaction.TargetAction;
            if ((action & TargetActionType.SearchTargetItemId) != 0)
                this.InteractionThisSearchGroupInItems(sourceGIds, groupDict);
            if ((action & TargetActionType.SearchTargetGroupId) != 0)
                this.InteractionThisSearchGroupInGroups(sourceGIds, groupDict);
            if ((action & TargetActionType.SearchTargetDataId) != 0)
                this.InteractionThisSearchGroupInData(sourceGIds, groupDict);

            return groupDict.Values.ToArray();
        }
        /// <summary>
        /// Metoda se pokusí najít ve své evidenci grupy pro prvky s ItemId odpovídající daným identifikátorům GId, a přidat je do předané Dictionary.
        /// </summary>
        /// <param name="sourceGIds"></param>
        /// <param name="groupDict"></param>
        protected void InteractionThisSearchGroupInItems(Dictionary<GId, DataGraphItem> sourceGIds, Dictionary<GId, GTimeGraphGroup> groupDict)
        {
            foreach (GId gId in sourceGIds.Keys)
            {   // Na vstupu mám klíče ze zdrojového grafu, zde hledám grafické prvky shodného klíče,
                //  do výstupu dávám zdejší grupy:
                DataGraphItem item;
                if (!this.TimeGraphItemDict.TryGetValue(gId, out item)) continue;
                if (groupDict.ContainsKey(item.GroupGId)) continue;
                GTimeGraphGroup value = (item as ITimeGraphItem).GControl.Group;
                if (value != null)
                    groupDict.Add(item.GroupGId, value);
            }
        }
        /// <summary>
        /// Metoda se pokusí najít ve své evidenci grupy s GroupId odpovídající daným identifikátorům GId, a přidat je do předané Dictionary.
        /// </summary>
        /// <param name="sourceGIds"></param>
        /// <param name="groupDict"></param>
        protected void InteractionThisSearchGroupInGroups(Dictionary<GId, DataGraphItem> sourceGIds, Dictionary<GId, GTimeGraphGroup> groupDict)
        {
            foreach (GId gId in sourceGIds.Keys)
            {   // Na vstupu mám klíče ze zdrojového grafu, zde hledám grupy (grafických prvků) shodného klíče,
                //  do výstupu dávám zdejší grupy:
                if (groupDict.ContainsKey(gId)) continue;            // V cílové dictionary už máme grupu pro daný GID, už ji znovu hledat nemusíme
                DataGraphItem[] group;
                if (!this.TimeGraphGroupDict.TryGetValue(gId, out group)) continue;
                GTimeGraphGroup value = (group[0] as ITimeGraphItem).GControl.Group;
                if (value != null)
                    groupDict.Add(gId, value);
            }
        }
        /// <summary>
        /// Metoda se pokusí najít ve své evidenci grupy pro prvky s ItemId odpovídající daným identifikátorům GId, a přidat je do předané Dictionary.
        /// </summary>
        /// <param name="sourceGIds"></param>
        /// <param name="groupDict"></param>
        protected void InteractionThisSearchGroupInData(Dictionary<GId, DataGraphItem> sourceGIds, Dictionary<GId, GTimeGraphGroup> groupDict)
        {
            foreach (DataGraphItem item in this.TimeGraphItemDict.Values)
            {   // Pro hledání podle DataId zde nemám vhodnou Dictionary. 
                //  Jednak nečekám, že by se takhle hledalo často, a druhak těch dat zas není tolik.
                //  Proscanovat řádově tisíc záznamů v nějaké kolekci zabere řádově milisekundu.
                // Proto scanuji opačně než v předešlých případech = scanuji všechna svoje data (this.TimeGraphItemDict),
                //  a pokud jejich DataId je zadané a je obsaženo ve zdrojových datech (sourceGIds), pak grupu daného prvku dávám do výstupu:
                if (item.DataGId != null && sourceGIds.ContainsKey(item.DataGId) && item.GroupGId != null && !groupDict.ContainsKey(item.GroupGId))
                {
                    GTimeGraphGroup value = (item as ITimeGraphItem).GControl.Group;
                    if (value != null)
                        groupDict.Add(item.GroupGId, value);
                }
            }
        }
        /// <summary>
        /// Metoda se pokusí najít ve své evidenci řádky s RowId odpovídající daným identifikátorům GId, a přidat je do předané Dictionary.
        /// </summary>
        /// <param name="sourceGIds"></param>
        /// <param name="rowDict"></param>
        protected void InteractionThisSearchRowInItems(Dictionary<GId, DataGraphItem> sourceGIds, Dictionary<GId, GRow> rowDict)
        {
            foreach (GId gId in sourceGIds.Keys)
            {   // Na vstupu mám klíče ze zdrojového grafu, zde hledám řádky shodného klíče:
                if (rowDict.ContainsKey(gId)) continue;
                Row row;
                if (!this.TryGetRow(gId, out row)) continue;
                rowDict.Add(gId, row.Control);
            }
        }
        /// <summary>
        /// Metoda provede akci, požadovanou v <see cref="GuiGridInteraction.TargetAction"/> pro všechny grupy dodané v parametru targetGroups.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="targetRows"></param>
        /// <param name="targetGroups"></param>
        protected void InteractionThisProcessAction(InteractionArgs args, GRow[] targetRows, GTimeGraphGroup[] targetGroups)
        {
            bool hasRows = (targetRows != null && targetRows.Length > 0);
            bool hasGroups = (targetGroups != null && targetGroups.Length > 0);
            if (!hasRows && !hasGroups) return;            // Nemáme žádná data na vstupu

            TargetActionType action = args.Interaction.TargetAction;
            bool isSelect = ((action & TargetActionType.SelectTargetItem) != 0);
            bool isActivate = ((action & TargetActionType.ActivateTargetItem) != 0);
            bool isFilterItem = ((action & TargetActionType.FilterTargetItems) != 0);
            bool isFilterRow = ((action & TargetActionType.FilterTargetRows) != 0);

            if (hasRows)
            {
                foreach (GRow gRow in targetRows)
                {
                    if (isFilterRow) this.InteractionThisRowFilterAdd(gRow);
                }
            }
            if (hasGroups)
            {
                foreach (GTimeGraphGroup targetGroup in targetGroups)
                {
                    if (isSelect) targetGroup.GControl.IsSelected = true;
                    if (isActivate) targetGroup.GControl.IsActivated = true;
                    if (isFilterRow) this.InteractionThisRowFilterAdd(targetGroup);
                }
            }
        }
        /// <summary>
        /// Metoda provede interakci typu <see cref="TargetActionType.ActivateGraphSkin"/> v this tabulce.
        /// </summary>
        /// <param name="args"></param>
        protected void InteractionThisProcessActivateGraphSkin(InteractionArgs args)
        {
            int? skinIndex = args.RunInteraction.GetParameterInt32N(0);
            if (skinIndex.HasValue)
                this.SkinCurrentIndex = skinIndex;
        }
        /// <summary>
        /// Metoda v this instanci připraví pracovní řádkový filtr <see cref="InteractionRowFilterDict"/>;
        /// </summary>
        /// <param name="active"></param>
        protected void InteractionThisRowFilterPrepare(bool active)
        {
            this.TableRow.RemoveFilter(InteractionRowFilterName);
            this.InteractionRowFilterDict = null;
            if (active)
                this.InteractionRowFilterDict = new Dictionary<GId, GRow>();
        }
        /// <summary>
        /// Metoda přidá řádek, do něhož patří daná grupa grafu, do připravovaného řádkového filtru
        /// </summary>
        /// <param name="targetGroup"></param>
        protected void InteractionThisRowFilterAdd(GTimeGraphGroup targetGroup)
        {
            if (this.InteractionRowFilterDict == null || targetGroup == null) return;
            this.InteractionThisRowFilterAdd(targetGroup.GControl.SearchForParent(typeof(GRow)) as GRow);
        }
        /// <summary>
        /// Metoda přidá daný řádek do připravovaného řádkového filtru
        /// </summary>
        /// <param name="gRow"></param>
        protected void InteractionThisRowFilterAdd(GRow gRow)
        {
            if (this.InteractionRowFilterDict == null || gRow == null) return;
            GId rowGId = gRow.OwnerRow?.RecordGId;
            if (rowGId == null) return;
            if (this.InteractionRowFilterDict.ContainsKey(rowGId)) return;
            this.InteractionRowFilterDict.Add(rowGId, gRow);
        }
        /// <summary>
        /// Metoda v this instanci aplikuje pracovní řádkový filtr <see cref="InteractionRowFilterDict"/>, pokud je aktivní (není null)
        /// </summary>
        protected void InteractionThisRowFilterActivate()
        {
            if (this.InteractionRowFilterDict == null) return;
            this.GTableRow.ApplyRowFilter(InteractionRowFilterName, this.InteractionThisRowFilter);
            this.GTableRow.Refresh();
        }
        /// <summary>
        /// Filtr na řádky podle zdejší dictionary <see cref="InteractionRowFilterDict"/>.
        /// Tato metoda je použita jako "filter" v objektu <see cref="TableFilter"/>, který je do zdejší datové tabulky aplikován pro filtrování interakcí.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected bool InteractionThisRowFilter(Row row)
        {
            return (this.InteractionRowFilterDict != null && row != null && row.RecordGId != null && this.InteractionRowFilterDict.ContainsKey(row.RecordGId));
        }
        /// <summary>
        /// Jméno filtru tabulky pocházejícího z Interakcí
        /// </summary>
        protected const string InteractionRowFilterName = "InteractionRowFilter";
        /// <summary>
        /// Pracovní řádkový filtr pro přípravu interakcí
        /// </summary>
        protected Dictionary<GId, GRow> InteractionRowFilterDict;
        /// <summary>
        /// Aktuálně platný index Skinu pro grafické prvky.
        /// Setování hodnoty způsobéí změnu Skinu ve všech prvcích všech grafů.
        /// Po inicializaci je hodnota <see cref="SkinCurrentIndex"/> = null (nové pouze inicalizované prvky grafu mají aktivní skin 0).
        /// </summary>
        public int? SkinCurrentIndex
        {
            get { return this._SkinCurrentIndex; }
            set
            {
                if (value.HasValue)
                {
                    int skin = value.Value;
                    this.TimeGraphDict.Values.ForEachItem(graph => graph.ModifyGraphItems(item => _SetSkinCurrentIndex(item, skin)));
                    this.GTableRow.Refresh();
                }
                this._SkinCurrentIndex = value;
            }
        }
        /// <summary>
        /// Metoda zajistí vložení aktuálního SkinIndexu <see cref="SkinCurrentIndex"/> do daného prvku grafu <see cref="DataGraphItem.SkinCurrentIndex"/>.
        /// Používá se při vkládání nových / aktualizovaných prvků grafu do this tabulky, k tomu aby nový prvek měl shodnou barevnost a skin jako má celá tabulka.
        /// Jinak by prvek byl do tabulky vložen se skinem = 0.
        /// </summary>
        /// <param name="graphItem"></param>
        protected void ApplyCurrentSkinIndexTo(DataGraphItem graphItem)
        {
            if (graphItem != null && this.SkinCurrentIndex.HasValue)
                graphItem.SkinCurrentIndex = this.SkinCurrentIndex.Value;
        }
        /// <summary>
        /// Metoda do daného prvku grafu do jeho <see cref="DataGraphItem.SkinCurrentIndex"/> vloží danou hodnotu indexu.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="skinIndex"></param>
        protected void _SetSkinCurrentIndex(ITimeGraphItem item, int skinIndex)
        {
            DataGraphItem graphItem = item as DataGraphItem;
            if (graphItem != null)
                graphItem.SkinCurrentIndex = skinIndex;
        }
        private int? _SkinCurrentIndex;
        #endregion
        #region class InteractionArgs : Data, předávaná při interakci mezi tabulkami z tabulky Source do tabulky Target
        /// <summary>
        /// InteractionArgs : Data, předávaná při interakci mezi tabulkami z tabulky Source do tabulky Target
        /// </summary>
        protected class InteractionArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="runInteraction">Definice této jedné interakce</param>
            /// <param name="sourceActiveRow">Aktuální řádek ve zdrojové tabulce</param>
            /// <param name="sourceCheckedRows">Označené řádky ve zdrojové tabulce</param>
            /// <param name="activeGraph">Aktivní graf</param>
            /// <param name="sourceGraphItems">Aktivní prky grafů ve zdrojové tabulce (podle typu interakce jde o všechny prvky aktivních řádků, nebo o aktivní prvky v určitém grafu)</param>
            internal InteractionArgs(GridInteractionRunInfo runInteraction, Row sourceActiveRow, Row[] sourceCheckedRows, GTimeGraph activeGraph, DataGraphItem[] sourceGraphItems)
            {
                this.RunInteraction = runInteraction;
                this.SourceActiveRow = sourceActiveRow;
                this.SourceCheckedRows = sourceCheckedRows;
                this.ActiveGraph = activeGraph;
                this.SourceGraphItems = sourceGraphItems;
            }
            /// <summary>
            /// Data této jedné interakce
            /// </summary>
            internal GridInteractionRunInfo RunInteraction { get; private set; }
            /// <summary>
            /// Definice interakce
            /// </summary>
            public GuiGridInteraction Interaction { get { return this.RunInteraction.GuiGridInteraction; } }
            /// <summary>
            /// Aktuální řádek ve zdrojové tabulce
            /// </summary>
            public Row SourceActiveRow { get; private set; }
            /// <summary>
            /// Označené řádky ve zdrojové tabulce
            /// </summary>
            public Row[] SourceCheckedRows { get; private set; }
            /// <summary>
            /// Aktivní graf
            /// </summary>
            public GTimeGraph ActiveGraph { get; private set; }
            /// <summary>
            /// Aktivní prky grafů ve zdrojové tabulce (podle typu interakce jde o všechny prvky aktivních řádků, nebo o aktivní prvky v určitém grafu)
            /// </summary>
            public DataGraphItem[] SourceGraphItems { get; private set; }
        }
        #endregion
        #endregion
        #region Klávesnice - eventhandler z tabulky, řešení, vyvolání IHost
        /// <summary>
        /// Metoda načte a připraví si informace o tom, zda jsou aktivní některé klávesy, a které to jsou
        /// </summary>
        private void _PrepareActiveKeys()
        {
            this._ActiveKeyDict = new Dictionary<Keys, GuiKeyAction>();
            var activeKeys = this.GuiGrid.ActiveKeys;
            if (activeKeys == null || activeKeys.Count == 0) return;
            this._ActiveKeyDict = activeKeys.GetDictionary(k => k.KeyData, true);
        }
        /// <summary>
        /// Eventhandler události KeyboardKeyUp z tabulky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableRow_KeyboardKeyUp(object sender, GPropertyEventArgs<Table> e)
        {
            if (!this._ActiveKeyExists || e == null || e.InteractiveArgs == null || e.InteractiveArgs.KeyboardEventArgs == null) return;
            System.Windows.Forms.Keys keyData = e.InteractiveArgs.KeyboardEventArgs.KeyData;
            GuiKeyAction guiKeyAction;
            if (!this._ActiveKeyDict.TryGetValue(keyData, out guiKeyAction)) return;

            GuiId rowId = e.Value.ActiveRow?.RecordGId;
            this.IMainData.RunKeyAction(guiKeyAction, this, this.TableName, rowId);
        }
        /// <summary>
        /// true pokud máme nějaké aktivní klávesy
        /// </summary>
        private bool _ActiveKeyExists { get { return (this._ActiveKeyDict != null && this._ActiveKeyDict.Count > 0); } }
        /// <summary>
        /// Dictionary aktivních kláves
        /// </summary>
        private Dictionary<Keys, GuiKeyAction> _ActiveKeyDict;
        #endregion
        #region Otevření formuláře záznamu
        /// <summary>
        /// Obsluha události, kdy tabulka sama (řádek nebo statický vztah) chce otevírat záznam
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRow_OpenRecordForm(object sender, GPropertyEventArgs<GId> e)
        {
            this.RunOpenRecordForm(e.Value);
        }
        /// <summary>
        /// Tato metoda zajistí otevření formuláře daného záznamu.
        /// Pouze převolá odpovídající metodu v <see cref="MainData"/>.
        /// </summary>
        /// <param name="recordGId"></param>
        protected void RunOpenRecordForm(GId recordGId)
        {
            if (this.HasMainData)
            {
                GuiRequest request = new GuiRequest();
                request.Command = GuiRequest.COMMAND_OpenRecords;
                request.RecordsToOpen = new GuiId[] { recordGId };
                this.IMainData.CallAppHostFunction(request, RunOpenRecordFormResponse, null);
            }
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost OpenRecords
        /// </summary>
        /// <param name="response"></param>
        protected void RunOpenRecordFormResponse(AppHostResponseArgs response)
        {
            this.IMainData.ProcessAppHostResponse(response.GuiResponse);
        }
        #endregion
        #region Drag and Drop Přemísťování prvku grafu odněkud někam, včetně aplikační logiky
        /// <summary>
        /// Scheduler určuje souřadnici prvku v procesu Drag and Drop,
        /// v akci Move = prvek se pouze přesouvá pomocí myši, ale ještě nebyl nikam umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragDropMove(ItemDragDropArgs args)
        {
            GraphItemChangeInfo moveInfo = this.PrepareSchedulerDragDropInfo(args);
            args.BoundsFinalAbsolute = moveInfo.BoundsFinalAbsolute;
            args.ToolTipData.AnimationType = TooltipAnimationType.Instant;
            args.ToolTipData.TitleText = (moveInfo.IsChangeRow ? "Přemístění na jiný řádek" : "Přemístění v rámci řádku");
            args.ToolTipData.InfoText = "Čas: " + moveInfo.TimeRangeFinal.ToString();
        }
        /// <summary>
        /// Scheduler vyvolá aplikační logiku, která určí definitivní umístění prvku v procesu Drag and Drop,
        /// v akci Drop = prvek byl vizuálně umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragDropDrop(ItemDragDropArgs args)
        {
            // Tady by se měla volat metoda AppHost => aplikační funkce pro přepočet grafu:
            GraphItemChangeInfo moveInfo = this.PrepareSchedulerDragDropInfo(args);

            // GUI data musím vytvořit ještě před tím, než vyvolám ItemDragDropDropGuiResponse(moveInfo), protože tam se data mohou změnit!!!
            bool hasMainData = this.HasMainData;
            GuiGridItemId gridItemId = (hasMainData ? this.GetGridItemId(args) : null);
            GuiRequestGraphItemMove guiItemMoveData = (hasMainData ? this.PrepareRequestGraphItemMove(moveInfo) : null);
            GuiRequestCurrentState guiCurrentState = (hasMainData ? this.IMainData.CreateGuiCurrentState() : null);

            // Nejprve provedu vizuální přemístění na "grafický" cíl, to proto že aplikační funkce může:  a) neexistovat  b) dlouho trvat:
            this.ItemDragDropDropGuiResponse(moveInfo);

            // Následně vyvolám (asynchronní) spuštění aplikační funkce, která zajistí komplexní přepočty a vrátí nová data, 
            //  její response se řeší v metodě ItemDragDropDropAppResponse():
            if (hasMainData)
            {
                GuiRequest request = new GuiRequest();
                request.Command = GuiRequest.COMMAND_GraphItemMove;
                request.ActiveGraphItem = gridItemId;
                request.GraphItemMove = guiItemMoveData;
                request.CurrentState = guiCurrentState;
                this.IMainData.CallAppHostFunction(request, this.ItemDragDropDropAppResponse, TimeSpan.FromMilliseconds(1500));
            }
        }
        /// <summary>
        /// Metoda vrátí instanci <see cref="GraphItemChangeInfo"/> obsahující data na úrovni Scheduleru z dat Drag and Drop z úrovně GUI.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected GraphItemChangeInfo PrepareSchedulerDragDropInfo(ItemDragDropArgs args)
        {
            // Základní data bez modifikací:
            ITimeGraphItem item = ((args.Item != null) ? args.Item : args.GroupedItems[0]);
            TimeRange sourceTime = args.Group.Time;
            Rectangle targetBounds = args.BoundsTargetAbsolute;
            DateTime? targetTimeBegin = args.GetTimeForPosition(targetBounds.X);
            TimeRange targetTime = TimeRange.CreateFromBeginSize(targetTimeBegin.Value, sourceTime.Size.Value);

            GraphItemChangeInfo moveInfo = new GraphItemChangeInfo();
            moveInfo.DragDropArgs = args;
            moveInfo.MoveMode = GraphItemMoveMode.Move;
            moveInfo.DragItem = item;
            moveInfo.DragGroupGId = this.GetGId(moveInfo.DragGroupId);
            moveInfo.DragGroupItems = args.Group.Items.Where(i => i is DataGraphItem).Cast<DataGraphItem>().ToArray();
            moveInfo.DragAction = args.DragAction;
            moveInfo.SourceMousePoint = args.ActionPoint;
            moveInfo.SourceGraph = args.ParentGraph;
            moveInfo.SourceRow = this.GetGraphRowGid(args.ParentGraph);
            moveInfo.SourceTime = sourceTime;
            moveInfo.SourceBounds = args.BoundsOriginalAbsolute;
            moveInfo.AttachedSide = RangeSide.None;
            moveInfo.TargetGraph = args.TargetGraph;
            moveInfo.TargetRow = this.GetGraphRowGid(args.TargetGraph);
            moveInfo.TimeRangeFinal = targetTime;
            moveInfo.BoundsFinalAbsolute = targetBounds;
            moveInfo.GetTimeForPosition = args.GetTimeForPosition;
            moveInfo.GetPositionForTime = args.GetPositionForTime;
            moveInfo.GetRoundedTime = args.GetRoundedTime;

            // Modifikace dat pomocí magnetů:
            SchedulerConfig.MoveSnapInfo snapInfo = this.Config.GetMoveSnapForKeys(Control.ModifierKeys);        // Zajímají nás aktuálně stisknuté klávesy, ne args.ModifierKeys !
            this.IMainData.AdjustGraphItemDragMove(moveInfo, snapInfo);

            return moveInfo;
        }
        /// <summary>
        /// Metoda z dat v <see cref="GraphItemChangeInfo"/> (interní data Scheduleru) 
        /// vytvoří a vrátí new instanci třídy <see cref="GuiRequestGraphItemMove"/> (externí data, která se předávají do aplikační logiky).
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        protected GuiRequestGraphItemMove PrepareRequestGraphItemMove(GraphItemChangeInfo moveInfo)
        {
            GuiRequestGraphItemMove guiData = new GuiRequestGraphItemMove();
            guiData.MoveItems = moveInfo.DragGroupItems.Select(i => this.GetGridItemId(i)).ToArray();
            guiData.SourceRowId = new GuiGridRowId() { TableName = this.TableName, RowId = moveInfo.SourceRow };
            guiData.SourceRow = moveInfo.SourceRow;
            guiData.SourceTime = moveInfo.SourceTime;
            guiData.MoveFixedPoint = GetGuiSide(moveInfo.AttachedSide);
            guiData.TargetRowId = new GuiGridRowId() { TableName = this.TableName, RowId = moveInfo.TargetRow };
            guiData.TargetRow = moveInfo.TargetRow;
            guiData.TargetTime = moveInfo.TimeRangeFinal;
            return guiData;
        }
        /// <summary>
        /// Metoda provede vizuální přemístění prvků grafu na požadovanou cílovou pozici, na základě GUI dat.
        /// </summary>
        /// <param name="moveInfo"></param>
        protected void ItemDragDropDropGuiResponse(GraphItemChangeInfo moveInfo)
        {
            // 1) Proběhne změna na všech prvcích grupy (data.DragGroupItems):
            //   a) Změna jejich času: o daný offset (rozdíl času cílového - původního)
            //   b) Změna hodnoty ParentGId = příslušnost do řádku
            // 2) Pokud se mění řádek, pak:
            //   a) ze zdrojového grafu se prvky odeberou
            //   b) do cílového grafu se prvky přidají
            // 3) Zavolá se Refresh na oba grafy (pokud jsou dva)
            bool isChangeRow = moveInfo.IsChangeRow;
            bool isChangeTime = moveInfo.IsChangeTime;
            TimeSpan? timeOffset = moveInfo.ShiftTime;
            foreach (DataGraphItem item in moveInfo.DragGroupItems)
            {
                if (isChangeRow)
                {
                    moveInfo.SourceGraph.RemoveGraphItem(item);
                    item.RowGId = moveInfo.TargetRow;
                    moveInfo.TargetGraph.AddGraphItem(item);
                }
                if (isChangeTime)
                {
                    item.Time = item.Time.ShiftByTime(timeOffset.Value);
                }
            }
            moveInfo.SourceGraph.Refresh();
            if (isChangeRow)
                moveInfo.TargetGraph.Refresh();

        }
        /// <summary>
        /// Metoda, která obdrží odpovědi z aplikační funkce, a podle nich zajistí patřičné změny v tabulkách.
        /// </summary>
        /// <param name="response"></param>
        protected void ItemDragDropDropAppResponse(AppHostResponseArgs response)
        {
            if (response == null || response.GuiResponse == null) return;
            this.IMainData.ProcessAppHostResponse(response.GuiResponse);
        }
        /// <summary>
        /// Metoda vrátí hodnotu <see cref="GuiSide"/> z obdobné hodnoty typu <see cref="RangeSide"/>.
        /// </summary>
        /// <param name="rangeSide"></param>
        /// <returns></returns>
        protected static GuiSide GetGuiSide(RangeSide rangeSide)
        {
            switch (rangeSide)
            {
                case RangeSide.Begin: return GuiSide.Begin;
                case RangeSide.Prev: return GuiSide.Prev;
                case RangeSide.None: return GuiSide.None;
                case RangeSide.Next: return GuiSide.Next;
                case RangeSide.End: return GuiSide.End;
            }
            return GuiSide.End;
        }
        #endregion
        #region Resize - změna velikosti prvku grafu (přesunutí jeho Begin nebo End), včetně aplikační logiky
        /// <summary>
        /// Scheduler určuje souřadnici a hodnoty prvku v procesu Resize,
        /// v akci Move = prvek se pouze přesouvá pomocí myši, ale ještě nebyl nikam umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemResizeMove(ItemResizeArgs args)
        {
            GraphItemChangeInfo moveInfo = this.PrepareSchedulerResizeInfo(args);
            args.BoundsFinalAbsolute = moveInfo.BoundsFinalAbsolute;
            args.TimeRangeFinal = moveInfo.TimeRangeFinal;
            args.ToolTipData.AnimationType = TooltipAnimationType.Instant;
            args.ToolTipData.TitleText = (args.ResizeSide == RectangleSide.Left ? "Změna času zahájení" : (args.ResizeSide == RectangleSide.Right ? "Změna času dokončení" : "Změna času " + args.ResizeSide.ToString()));
            args.ToolTipData.InfoText = "Čas: " + moveInfo.TimeRangeFinal.ToString();
        }
        /// <summary>
        /// Scheduler vyvolá aplikační logiku, která určí definitivní umístění prvku v procesu Resize,
        /// v akci Drop = prvek byl vizuálně umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemResizeDrop(ItemResizeArgs args)
        {
            // Tady by se měla volat metoda AppHost => aplikační funkce pro přepočet grafu:
            GraphItemChangeInfo moveInfo = this.PrepareSchedulerResizeInfo(args);

            // GUI data musím vytvořit ještě před tím, než vyvolám ItemDragDropDropGuiResponse(moveInfo), protože tam se data mohou změnit!!!
            bool hasMainData = this.HasMainData;
            GuiGridItemId gridItemId = (hasMainData ? this.GetGridItemId(args) : null);
            GuiRequestGraphItemResize guiItemResizeData = (hasMainData ? this.PrepareRequestGraphItemResize(moveInfo) : null);
            GuiRequestCurrentState guiCurrentState = (hasMainData ? this.IMainData.CreateGuiCurrentState() : null);

            // Nejprve provedu vizuální přemístění na "grafický" cíl, to proto že aplikační funkce může:  a) neexistovat  b) dlouho trvat:
            this.ItemResizeDropGuiResponse(moveInfo);

            // Následně vyvolám (asynchronní) spuštění aplikační funkce, která zajistí komplexní přepočty a vrátí nová data, 
            //  její response se řeší v metodě ItemDragDropDropAppResponse():
            if (hasMainData)
            {
                GuiRequest request = new GuiRequest();
                request.Command = GuiRequest.COMMAND_GraphItemResize;
                request.ActiveGraphItem = gridItemId;
                request.GraphItemResize = guiItemResizeData;
                request.CurrentState = guiCurrentState;
                this.IMainData.CallAppHostFunction(request, this.ItemResizeDropAppResponse, TimeSpan.FromMilliseconds(1500));
            }
        }
        /// <summary>
        /// Metoda vrátí instanci <see cref="GraphItemChangeInfo"/> obsahující data na úrovni Scheduleru z dat Resize z úrovně GUI.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected GraphItemChangeInfo PrepareSchedulerResizeInfo(ItemResizeArgs args)
        {
            // Základní data bez modifikací:
            ITimeGraphItem item = ((args.Item != null) ? args.Item : args.GroupedItems[0]);
            Rectangle sourceBounds = args.BoundsOriginal;
            Rectangle targetBounds = args.BoundsTarget;
            
            GraphItemChangeInfo moveInfo = new GraphItemChangeInfo();
            moveInfo.ResizeArgs = args;
            moveInfo.MoveMode = (args.ResizeSide == RectangleSide.Left ? GraphItemMoveMode.ResizeBegin : GraphItemMoveMode.ResizeEnd);
            moveInfo.DragItem = item;
            moveInfo.DragGroupGId = this.GetGId(moveInfo.DragGroupId);
            moveInfo.DragGroupItems = args.Group.Items.Where(i => i is DataGraphItem).Cast<DataGraphItem>().ToArray();
            moveInfo.DragAction = args.ResizeAction;
            moveInfo.SourceMousePoint = args.ActionPoint;
            moveInfo.SourceGraph = args.Graph;
            moveInfo.SourceRow = this.GetGraphRowGid(args.Graph);
            moveInfo.SourceTime = args.Group.Time;
            moveInfo.SourceBounds = args.BoundsOriginalAbsolute;
            moveInfo.AttachedSide = RangeSide.None;
            moveInfo.TargetGraph = moveInfo.SourceGraph;
            moveInfo.TargetRow = moveInfo.SourceRow;
            moveInfo.TimeRangeFinal = args.TimeRangeTarget;
            moveInfo.BoundsFinalAbsolute = args.BoundsTargetAbsolute;
            moveInfo.GetTimeForPosition = args.GetTimeForPosition;
            moveInfo.GetPositionForTime = args.GetPositionForTime;
            moveInfo.GetRoundedTime = args.GetRoundedTime;

            // Modifikace dat pomocí magnetů:
            SchedulerConfig.MoveSnapInfo snapInfo = this.Config.GetMoveSnapForKeys(Control.ModifierKeys);        // Zajímají nás aktuálně stisknuté klávesy, ne args.ModifierKeys !
            this.IMainData.AdjustGraphItemDragMove(moveInfo, snapInfo);

            return moveInfo;
        }
        /// <summary>
        /// Metoda z dat v <see cref="GraphItemChangeInfo"/> (interní data Scheduleru) 
        /// vytvoří a vrátí new instanci třídy <see cref="GuiRequestGraphItemResize"/> (externí data, která se předávají do aplikační logiky).
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        protected GuiRequestGraphItemResize PrepareRequestGraphItemResize(GraphItemChangeInfo moveInfo)
        {
            GuiRequestGraphItemResize guiData = new GuiRequestGraphItemResize();
            guiData.ResizeItems = moveInfo.DragGroupItems.Select(i => this.GetGridItemId(i)).ToArray();
            guiData.SourceRow = moveInfo.SourceRow;
            var y = moveInfo.ResizeArgs.Group.CoordinateYLogical;
            guiData.SourceHeight = new GuiSingleRange(y.Begin, y.End);
            guiData.SourceTime = moveInfo.SourceTime;
            guiData.TargetHeight = guiData.SourceHeight;
            guiData.TargetTime = moveInfo.TimeRangeFinal;
            return guiData;
        }
        /// <summary>
        /// Metoda provede vizuální přemístění prvků grafu na požadovanou cílovou pozici, na základě GUI dat.
        /// </summary>
        /// <param name="moveInfo"></param>
        protected void ItemResizeDropGuiResponse(GraphItemChangeInfo moveInfo)
        {
            // Tato metoda nemusí dělat nic, protože změnu času v prvku Group zajistí sama komponenta Graph tím, 
            //  že nasetuje výsledné hodnoty z argumentu ItemResizeArgs (BoundsFinalAbsolute a TimeRangeFinal) do prvků grafu.
            ItemResizeArgs args = moveInfo.ResizeArgs;
            args.BoundsFinalAbsolute = moveInfo.BoundsFinalAbsolute;
            args.TimeRangeFinal = moveInfo.TimeRangeFinal;
        }
        /// <summary>
        /// Metoda, která obdrží odpovědi z aplikační funkce, a podle nich zajistí patřičné změny v tabulkách.
        /// </summary>
        /// <param name="response"></param>
        protected void ItemResizeDropAppResponse(AppHostResponseArgs response)
        {
            if (response == null || response.GuiResponse == null) return;
            this.IMainData.ProcessAppHostResponse(response.GuiResponse);
        }
        #endregion
        #region Drag and Drop Přemísťování řádků z this tabulky někam jinam, včetně aplikační logiky
        /// <summary>
        /// Připraví v this tabulce data pro Drag and Move řádků
        /// </summary>
        private void _PrepareRowDragMove()
        {
            this.DragMoveRows = DragMoveRowsInfo.CreateForProperties(this, this.GuiGrid.GridProperties);
            this.TableRow.AllowRowDragMove = this.DragMoveRows.DragMoveEnabled;
            if (this.DragMoveRows.DragMoveEnabled)
            {   // Pokud je povoleno provádět Drag and Move, zaregistruji si eventhandlery pro všechny patřičné události:
                this.TableRow.RowDragMoveSourceMode = this.DragMoveRows.DragMoveSourceRowMode;
                this.TableRow.TableRowDragStart += _TableRowDragStart;
                this.TableRow.TableRowDragMove += _TableRowDragMove;
                this.TableRow.TableRowDragDrop += _TableRowDragDrop;
            }
        }
        /// <summary>
        /// Eventhandler události Drag and Move, volaný při ZAČÁTKU přetahování řádků this tabulky na jiné místo.
        /// Eventhandler může omezit řádky, které uživatel vybral do procesu jako zdrojové.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TableRowDragStart(object sender, TableRowDragMoveArgs args)
        {
            args.DragRows = args.DragRows.Where(row => this.DragMoveRows.CanDragRow(row));
        }
        /// <summary>
        /// Eventhandler události Drag and Move, volaný při PRŮBĚHU přetahování řádků this tabulky na jiné místo.
        /// Eventhandler reaguje na prvek, nad kterým se právě nachází myš (=cíl přetahování),
        /// a podle pravidel <see cref="DragMoveRowsInfo"/> vyhodnocuje povolení / zákaz pro DragDrop na daném cíli.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TableRowDragMove(object sender, TableRowDragMoveArgs args)
        {
            IInteractiveItem activeItem;
            args.TargetEnabled = this.DragMoveRows.TryFindValidTarget(args.TargetItem, args.MouseCurrentAbsolutePoint, out activeItem);
            args.ActiveItem = activeItem;
        }
        /// <summary>
        /// Eventhandler události Drag Drop, volaný při DOKONČENÍ přetahování řádků při přetahování řádků this tabulky na jiné místo.
        /// Eventhandler by měl zajistit vyvolání aplikační logiky, včetně vyvolání funkce datového zdroje.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TableRowDragDrop(object sender, TableRowDragMoveArgs args)
        {
            if (!this.HasMainData) return;

            // Hodnoty v args sem přichází již připravené z metody _TableRowDragMove(), tedy obsahují údaje o schváleném cíli.
            // Anebo pokud vizuální cíl není povolen (dle údajů v this.DragMoveRows), pak nemá cenu volat AppHost, protože o daný cíl nemá zájem:
            if (!args.TargetEnabled) return;

            // Připravíme data o aktuálním stavu a o pohybu, a pak zavoláme hostitele IHost a předáme mu všechna ta data:

            // Vyhledat tabulku - řádek - graf - prvek:
            DragMoveRowsCurrentDataInfo dataInfo = DragMoveRowsCurrentDataInfo.CreateForTarget(args.ActiveItem, args.MouseCurrentAbsolutePoint, true);

            // Poskládat data pro volání IHost:
            GuiRequestRowDragMove rowDragMove = new GuiRequestRowDragMove();
            rowDragMove.SourceRows = args.DragRows.Select(r => this.GetGridRowId(r)).ToArray();
            rowDragMove.TargetRow = dataInfo.GuiGridRowId;
            rowDragMove.TargetItem = dataInfo.GuiGridItemId;
            rowDragMove.TargetGroup = dataInfo.GuiGridGroupItemIds;
            rowDragMove.TargetTime = dataInfo.Time;
            rowDragMove.TargetTimeRound = dataInfo.TimeRound;

            // Sestavit argument pro volání IHost:
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_RowDragDrop;
            request.CurrentState = this.IMainData.CreateGuiCurrentState();
            request.RowDragMove = rowDragMove;

            // Zavolat Host:
            this.IMainData.CallAppHostFunction(request, this.RowDragDropAppResponse, TimeSpan.FromMilliseconds(1500));
        }
        /// <summary>
        /// Metoda, která obdrží odpovědi z aplikační funkce, a podle nich zajistí patřičné změny v tabulkách.
        /// </summary>
        /// <param name="response"></param>
        protected void RowDragDropAppResponse(AppHostResponseArgs response)
        {
            if (response == null || response.GuiResponse == null) return;
            this.IMainData.ProcessAppHostResponse(response.GuiResponse);
        }
        /// <summary>
        /// Analyzovaný režim Drag and Move pro řádky this tabulky.
        /// Pochází z údajů v <see cref="GuiGridProperties.RowDragMoveToTarget"/>.
        /// Je vytvořen jedenkrát při načítání GUI, od té doby se nemění.
        /// </summary>
        protected DragMoveRowsInfo DragMoveRows { get; private set; }
        #region Třídy DragMoveRowsInfo a DragMoveRowsSourceInfo a DragMoveRowsOneTargetInfo : obsahují definice pravidel pro Drag and Move řádků
        /// <summary>
        /// Třída pro analýzu režimu Drag and Move pro řádky this tabulky.
        /// Pochází z údajů v <see cref="GuiGridProperties.RowDragMoveToTarget"/>.
        /// </summary>
        protected class DragMoveRowsInfo
        {
            #region Konstruktor, načtení, data
            /// <summary>
            /// Vrací new instanci pro dané zadání <see cref="GuiGridProperties"/>
            /// </summary>
            /// <param name="table">Tabulka s daty</param>
            /// <param name="properties">Zadání</param>
            /// <returns></returns>
            public static DragMoveRowsInfo CreateForProperties(MainDataTable table, GuiGridProperties properties)
            {
                if (properties == null) return DragMoveRowsInfo.Empty;
                return CreateForData(table, properties.RowDragMoveSource, properties.RowDragMoveToTarget);
            }
            /// <summary>
            /// Vrací new instanci pro daný režim
            /// </summary>
            /// <param name="table">Tabulka s daty</param>
            /// <param name="sourceText">Hodnota <see cref="GuiGridProperties.RowDragMoveSource"/></param>
            /// <param name="targetText">Hodnota <see cref="GuiGridProperties.RowDragMoveToTarget"/></param>
            /// <returns></returns>
            public static DragMoveRowsInfo CreateForData(MainDataTable table, string sourceText, string targetText)
            {
                DragMoveRowsInfo info = new DragMoveRowsInfo(table);
                info.SourceInfo = DragMoveRowsSourceInfo.CreateForData(sourceText);
                info.TargetList.AddRange(DragMoveRowsOneTargetInfo.CreateForData(targetText));
                return info;
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            private DragMoveRowsInfo(MainDataTable table)
            {
                this.TargetList = new List<DragMoveRowsOneTargetInfo>();
                this.MainTable = table;
            }
            /// <summary>
            /// Obsahuje new instanci definující statickou vazbu
            /// </summary>
            public static DragMoveRowsInfo Empty { get { return new DragMoveRowsInfo(null); } }
            /// <summary>
            /// Datová tabulka, která je vlastníkem this předpisu
            /// </summary>
            public MainDataTable MainTable { get; private set; }
            /// <summary>
            /// true = Drag and Move je povoleno
            /// </summary>
            public bool DragMoveEnabled { get { return (this.SourceInfo != null && this.SourceInfo.DragMoveEnabled); } }
            /// <summary>
            /// Režim výběru řádků tabulky (Označené / Aktivní)
            /// </summary>
            public TableRowDragMoveSourceMode DragMoveSourceRowMode { get { return (this.DragMoveEnabled ? this.SourceInfo.SourceRowMode : TableRowDragMoveSourceMode.None); } }
            /// <summary>
            /// Data definující zdroje pro Drag and Move = zde se definuje, jaké řádky a v jakém režimu se smějí v aktuální tabulce přesouvat pomocí Drag and Move
            /// </summary>
            protected DragMoveRowsSourceInfo SourceInfo { get; private set; }
            /// <summary>
            /// Seznam povolených cílů pro Drag and Move řádků this tabulky
            /// </summary>
            protected List<DragMoveRowsOneTargetInfo> TargetList { get; private set; }
            #endregion
            #region Vyhodnocení pro konkrétní zdrojový řádek a pro konkrétní cílový prvek
            /// <summary>
            /// Metoda vrátí true, pokud podle těchto pravidel je možno provádět Drag and Move pro daný řádek.
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            public bool CanDragRow(Row row)
            {
                return (this.DragMoveEnabled && this.SourceInfo.CanDragRow(row));
            }
            /// <summary>
            /// Metoda vrátí true, pokud aktuální cílový prvek může být cílem pro Drag and Move řádků.
            /// </summary>
            /// <param name="targetItem"></param>
            /// <param name="mouseCurrentAbsolutePoint"></param>
            /// <param name="activeItem"></param>
            /// <returns></returns>
            public bool TryFindValidTarget(IInteractiveItem targetItem, Point? mouseCurrentAbsolutePoint, out IInteractiveItem activeItem)
            {
                activeItem = null;
                if (this.TargetList.Count == 0)
                {   // Nemám dané povolené cíle => řádky lze přesouvat kamkoliv:
                    activeItem = targetItem;
                    return true;
                }

                // vyhledat tabulku - řádek - graf - prvek:
                DragMoveRowsCurrentDataInfo dataInfo = DragMoveRowsCurrentDataInfo.CreateForTarget(targetItem, mouseCurrentAbsolutePoint, false);

                foreach (var targetInfo in this.TargetList)
                {   // Máme definované povolené cíle, jmenovitě podle cílových tabulek => pokud určitá definice povoluje přesun na daný cíl, lze přesun povolit:
                    if (targetInfo.TryFindValidTarget(dataInfo))
                    {
                        activeItem = dataInfo.ActiveItem;
                        return true;
                    }
                }

                // Žádná definice cíle nepovoluje přesun do targetItem:
                return false;
            }
            #endregion
        }
        /// <summary>
        /// Definice, určující povolené řádky pro proces Drag and Move (na základě dat z <see cref="GuiGridProperties.RowDragMoveSource"/>)
        /// </summary>
        protected class DragMoveRowsSourceInfo
        {
            #region Konstruktor, načtení, data
            /// <summary>
            /// Vytvoří a vrátí prvek třídy <see cref="DragMoveRowsSourceInfo"/> z textu, který pochází z <see cref="GuiGridProperties.RowDragMoveSource"/>
            /// </summary>
            /// <param name="sourceText"></param>
            /// <returns></returns>
            public static DragMoveRowsSourceInfo CreateForData(string sourceText)
            {
                DragMoveRowsSourceInfo sourceinfo = new DragMoveRowsSourceInfo();

                // 1. Analýza textu:
                if (!String.IsNullOrEmpty(sourceText))
                {   // Klíčová slova: 
                    // "DragOnlyActiveRow" = Přesouvat se bude pouze řádek, který chytila myš. Ostatní označené řádky se přesouvat nebudou.;
                    // "DragActivePlusSelectedRows" = Přesouvat se budou řádky označené kliknutím plus řádek, který chytila myš. To je intuitivně nejvhodnější nastavení.;
                    // "DragOnlySelectedRows" = Přesouvat se budou pouze řádky označené kliknutím. Řádek, který chytila myš, se přesouvat nebude (tedy pokud není označen ikonkou).;
                    // "DragSelectedThenActiveRow" = Přesouvat se budou primárně řádky označené kliknutím (a ne aktivní). Ale pokud nejsou označeny žádné řádky, tak se přesune řádek, který chytila myš.
                    // Rozdíl od "DragActivePlusSelectedRows" je v tom, že tady se nebude přesouvat aktivní řádek (myší) pokud existují řádky označené (ikonkou).
                    // Pokud nebude zadaná žádná hodnota typu "Drag*", pak se nebude přesouvat nic.
                    // Pokud bude zadáno více hodnot typu "Drag*", pak platí první z nich.
                    // Typ řádku:
                    // "Root" = přesouvat pouze řádky na pozici Root ve stromu
                    // "Child" = přesouvat pouze řádky na pozici Child ve stromu
                    // "Master" = přesouvat pouze řádky Master (rozpoznává se v <see cref="GuiId"/> řádku, kde <see cref="GuiId.EntryId"/> musí být null);
                    // "Entry" = přesouvat pouze řádky Entry (rozpoznává se v <see cref="GuiId"/> řádku, kde <see cref="GuiId.EntryId"/> nesmí být null);
                    // "Class12345" = přesouvat pouze řádky dané třídy;
                    // "MasterClass12345" = pouze řádky Master z dané třídy;
                    // "EntryClass12345" = pouze řádky Entry z dané třídy;

                    string[] items = sourceText.Split(' ', ',');
                    TableRowDragMoveSourceMode sourceMode = TableRowDragMoveSourceMode.None;
                    bool rowRoot = false;
                    bool rowChild = false;
                    bool rowMaster = false;
                    bool rowEntry = false;
                    Dictionary<int, string> classDict = new Dictionary<int, string>();

                    foreach (string item in items)
                    {
                        string text = item.Trim();
                        if (text.Length == 0) continue;
                        switch (text)
                        {
                            case GuiGridProperties.RowDragSource_DragOnlyActiveRow:
                                if (sourceMode == TableRowDragMoveSourceMode.None)
                                    sourceMode = TableRowDragMoveSourceMode.OnlyActiveRow;
                                break;
                            case GuiGridProperties.RowDragSource_DragActivePlusSelectedRows:
                                if (sourceMode == TableRowDragMoveSourceMode.None)
                                    sourceMode = TableRowDragMoveSourceMode.ActivePlusSelectedRows;
                                break;
                            case GuiGridProperties.RowDragSource_DragOnlySelectedRows:
                                if (sourceMode == TableRowDragMoveSourceMode.None)
                                    sourceMode = TableRowDragMoveSourceMode.OnlySelectedRows;
                                break;
                            case GuiGridProperties.RowDragSource_DragSelectedThenActiveRow:
                                if (sourceMode == TableRowDragMoveSourceMode.None)
                                    sourceMode = TableRowDragMoveSourceMode.SelectedThenActiveRow;
                                break;

                            case GuiGridProperties.RowDragSource_Root:
                                rowRoot = true;
                                break;
                            case GuiGridProperties.RowDragSource_Child:
                                rowChild = true;
                                break;
                            case GuiGridProperties.RowDragSource_Master:
                                rowMaster = true;
                                break;
                            case GuiGridProperties.RowDragSource_Entry:
                                rowEntry = true;
                                break;

                            default:
                                string result = null;
                                int classNumber = 0;
                                ParsePrefixClass(text, GuiGridProperties.RowDragSource_ClassPrefix, "ME", ref result, ref classNumber);
                                ParsePrefixClass(text, GuiGridProperties.RowDragSource_MasterClassPrefix, "M", ref result, ref classNumber);
                                ParsePrefixClass(text, GuiGridProperties.RowDragSource_EntryClassPrefix, "E", ref result, ref classNumber);
                                if (result != null)
                                {   // Aktuální text začíná některým z klíčových slov, a obsahuje číslo. Máme to číslo a výsledek:
                                    if (!classDict.ContainsKey(classNumber))
                                        classDict.Add(classNumber, result);
                                    else
                                        classDict[classNumber] = classDict[classNumber] + result;
                                }
                                break;
                        }
                    }

                    // 2. Syntéza výsledku:
                    sourceinfo.DragMoveEnabled = (sourceMode != TableRowDragMoveSourceMode.None);
                    sourceinfo.SourceRowMode = sourceMode;
                    sourceinfo.RowRoot = ((!rowRoot && !rowChild) || rowRoot);           // Nezadaná hodnota (!rowRoot && !rowChild) = povoleno
                    sourceinfo.RowChild = ((!rowRoot && !rowChild) || rowChild);
                    sourceinfo.RowMaster = ((!rowMaster && !rowEntry) || rowMaster);
                    sourceinfo.RowEntry = ((!rowMaster && !rowEntry) || rowEntry);
                    sourceinfo.ClassDict.AddNewItems(classDict,
                        kvp => /* keySelector */    kvp.Key,
                        kvp => /* valueSelector */  new Tuple<bool, bool>(kvp.Value.Contains("M"), kvp.Value.Contains("E")));
                    /* Value v sourceinfo.ClassDict obsahuje Tuple, kde Item1 = akceptovat řádky Master, a Item2= akceptovat Entries */
                }

                return sourceinfo;
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            private DragMoveRowsSourceInfo()
            {
                this.DragMoveEnabled = false;
                this.SourceRowMode = TableRowDragMoveSourceMode.None;
                this.ClassDict = new Dictionary<int, Tuple<bool, bool>>();
            }

            /// <summary>
            /// true = Drag and Move je povoleno
            /// </summary>
            public bool DragMoveEnabled { get; private set; }

            /// <summary>
            /// Režim výběru řádků tabulky (Označené / Aktivní)
            /// </summary>
            public TableRowDragMoveSourceMode SourceRowMode { get; private set; }
            /// <summary>
            /// Je povoleno přenášet řádky typu Root
            /// </summary>
            protected bool RowRoot { get; private set; }
            /// <summary>
            /// Je povoleno přenášet řádky typu Child
            /// </summary>
            protected bool RowChild { get; private set; }
            /// <summary>
            /// Je povoleno přenášet řádky typu Master
            /// </summary>
            protected bool RowMaster { get; private set; }
            /// <summary>
            /// Je povoleno přenášet řádky typu Entry
            /// </summary>
            protected bool RowEntry { get; private set; }
            /// <summary>
            /// Dictionary obsahjící explicitně definované třídy, a příznak zda lze přenášet řádky typu Master (Value.Item1) a Entry (Value.Item2)
            /// </summary>
            protected Dictionary<int, Tuple<bool, bool>> ClassDict { get; private set; }
            #endregion
            #region Vyhodnocení pro konkrétní zdrojový řádek
            /// <summary>
            /// Metoda vrátí true, pokud podle těchto pravidel je možno provádět Drag and Move pro daný řádek.
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            public bool CanDragRow(Row row)
            {
                if (!this.DragMoveEnabled) return false;

                if (row.Table.IsTreeViewTable)
                {   // Tabulka má stromovou strukturu:
                    bool isRoot = row.TreeNode.IsRoot;
                    if (isRoot && !this.RowRoot) return false;       // Řádek je Root, a pravidla NEPOVOLUJÍ pracovat s Root řádky
                    if (!isRoot && !this.RowChild) return false;     // Řádek je Child, a pravidla NEPOVOLUJÍ pracovat s Child řádky
                }

                GId rowGId = row.RecordGId;
                if (rowGId == null) return true;                     // Pokud řádek nemá GId, pak další testy nemají význam, a řádek povolíme.

                bool isEntry = rowGId.EntryId.HasValue;
                if (!isEntry && !this.RowMaster) return false;       // Řádek je Master, a pravidla NEPOVOLUJÍ pracovat s Master řádky
                if (isEntry && !this.RowEntry) return false;         // Řádek je Entry, a pravidla NEPOVOLUJÍ pracovat s Entry řádky

                if (this.ClassDict.Count == 0) return true;          // Nejsou specifikovány explicitní třídy => řádek povolíme.

                Tuple<bool, bool> classInfo;
                if (!this.ClassDict.TryGetValue(rowGId.ClassId, out classInfo)) return false;      // Třída řádku není uvedena v povolených třídách => řádek NEpovolíme.
                if (!isEntry && !classInfo.Item1) return false;      // Řádek je Master, a pravidla konkrétní třídy NEPOVOLUJÍ pracovat s Master řádky
                if (isEntry && !classInfo.Item2) return false;       // Řádek je Entry, a pravidla konkrétní třídy NEPOVOLUJÍ pracovat s Entry řádky

                return true;
            }
            #endregion
        }
        /// <summary>
        /// Definice, určující povolené cíle pro proces Drag and Move (na základě dat z <see cref="GuiGridProperties.RowDragMoveToTarget"/>)
        /// </summary>
        protected class DragMoveRowsOneTargetInfo
        {
            #region Konstruktor, načtení, data
            /// <summary>
            /// Vytvoří a vrátí sadu prvků <see cref="DragMoveRowsOneTargetInfo"/> z textu, který pochází z <see cref="GuiGridProperties.RowDragMoveToTarget"/>
            /// </summary>
            /// <param name="targetText"></param>
            /// <returns></returns>
            public static List<DragMoveRowsOneTargetInfo> CreateForData(string targetText)
            {
                List<DragMoveRowsOneTargetInfo> targetList = new List<DragMoveRowsOneTargetInfo>();
                if (!String.IsNullOrEmpty(targetText))
                {
                    string[] items = targetText.Split(';');
                    foreach (string item in items)
                    {
                        DragMoveRowsOneTargetInfo target = DragMoveRowsOneTargetInfo.CreateForItem(item);
                        if (target != null)
                            targetList.Add(target);
                    }
                }
                return targetList;
            }
            /// <summary>
            /// Vrátí instanci <see cref="DragMoveRowsOneTargetInfo"/>, naplní ji parsováním textu (data), 
            /// podle pravidel dle <see cref="GuiGridProperties.RowDragMoveToTarget"/>
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public static DragMoveRowsOneTargetInfo CreateForItem(string data)
            {
                if (String.IsNullOrEmpty(data)) return null;

                // 1. Analýza textu:
                string[] items = data.Split(' ', ',');
                string fullTable = "";
                string valueRow = "";
                string valueTo = "";
                Dictionary<int, object> classDict = new Dictionary<int, object>();

                foreach (string item in items)
                {
                    string text = item.Trim();
                    if (text.Length == 0) continue;
                    switch (text)
                    {
                        case GuiGridProperties.RowDragTarget_RowRoot:
                            valueRow += "R";
                            break;
                        case GuiGridProperties.RowDragTarget_RowChild:
                            valueRow += "C";
                            break;
                        case GuiGridProperties.RowDragTarget_RowAny:
                            valueRow += "A";
                            break;
                        case GuiGridProperties.RowDragTarget_ToCell:
                            valueTo += "C";
                            break;
                        case GuiGridProperties.RowDragTarget_ToGraph:
                            valueTo += "G";
                            break;
                        case GuiGridProperties.RowDragTarget_ToItem:
                            valueTo += "I";
                            break;
                        default:
                            string result = null;
                            int classNumber = 0;
                            ParsePrefixClass(text, GuiGridProperties.RowDragTarget_ToItemClassPrefix, "OK", ref result, ref classNumber);
                            if (result != null)
                            {
                                if (classNumber != 0 && !classDict.ContainsKey(classNumber))
                                    classDict.Add(classNumber, null);
                            }
                            else if (fullTable.Length == 0)
                            {
                                fullTable = text;
                            }
                            break;
                    }
                }
                if (fullTable.Length == 0) return null;

                // 2. Syntéza výsledku:
                DragMoveRowsOneTargetInfo target = new DragMoveRowsOneTargetInfo();
                target.FullTableName = fullTable;
                target.IsTargetOnRowRoot = (valueRow.Length == 0 || valueRow.Contains("R") || valueRow.Contains("A"));
                target.IsTargetOnRowChild = (valueRow.Length == 0 || valueRow.Contains("C") || valueRow.Contains("A"));
                target.TargetObjectCell = (valueTo.Contains("C") || (valueTo.Length == 0 && classDict.Count == 0));
                target.TargetObjectGraph = (valueTo.Contains("G"));
                target.TargetObjectGraphItemAny = (valueTo.Contains("I"));
                target.TargetObjectGraphItemClassDict = classDict;

                return target;
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            private DragMoveRowsOneTargetInfo() { }
            /// <summary>
            /// Název cílové tabulky
            /// </summary>
            protected string FullTableName { get; private set; }
            /// <summary>
            /// Cíl může být na Root řádku (graf, nebo prvek grafu).
            /// Pokud může být cílem plocha řádku i mimo grafy, musí to být povoleno: <see cref="TargetObjectCell"/> = true.
            /// </summary>
            protected bool IsTargetOnRowRoot { get; private set; }
            /// <summary>
            /// Cíl může být na Child řádku (graf, nebo prvek grafu).
            /// Pokud může být cílem plocha řádku i mimo grafy, musí to být povoleno: <see cref="TargetObjectCell"/> = true.
            /// </summary>
            protected bool IsTargetOnRowChild { get; private set; }

            /// <summary>
            /// Cílem může být obecně kterákoli buňka řádku
            /// </summary>
            protected bool TargetObjectCell { get; private set; }
            /// <summary>
            /// Cílem může být graf, jakékoli jeho místo
            /// </summary>
            protected bool TargetObjectGraph { get; private set; }
            /// <summary>
            /// Cílem může být graf, pouze jeho prvek, jakékoli třídy
            /// </summary>
            protected bool TargetObjectGraphItemAny { get; private set; }
            /// <summary>
            /// Cílem může být graf, pouze jeho prvek, jehož třída je uvedena v této Dictionary
            /// </summary>
            protected Dictionary<int, object> TargetObjectGraphItemClassDict { get; private set; }
            #endregion
            #region Vyhodnocení pro konkrétní cílový prvek
            /// <summary>
            /// Metoda vrátí true, pokud aktuální cílový prvek může být cílem pro Drag and Move řádků.
            /// </summary>
            /// <param name="targetData">Komplexní popis cílového prvku</param>
            /// <returns></returns>
            public bool TryFindValidTarget(DragMoveRowsCurrentDataInfo targetData)
            {
                if (targetData == null || targetData.MainDataTable == null) return false;

                // Pokud nalezená tabulka má jiné FullName než je zde vyžadováno, pak skončíme:
                if (!String.Equals(targetData.MainDataTable.TableName, this.FullTableName)) return false;

                // Testuji požadavky na typ cílového řádku (Root / Child):
                if (!this.TestDataRowEnabled(targetData.Row)) return false;

                // Pokud target je prvek grafu, pak testuji zda je takový prvek povolen jako cíl:
                DataGraphItem testGraphItem = targetData.TestGraphItem;
                if (this.TestDataGraphItem(testGraphItem))
                {
                    targetData.ActiveItem = targetData.GTimeGraphItem;
                    return true;
                }

                // Pokud target je graf, pak testuji zda je povolen graf:
                GTimeGraph testGraph = targetData.GTimeGraph;
                if (this.TestDataGraph(testGraph))
                {
                    targetData.ActiveItem = testGraph;
                    return true;
                }

                // Pokud target je Cell, pak testuji zda je povolena buňka Cell:
                GCell testCell = targetData.GCell;
                if (this.TestDataCell(testCell))
                {
                    targetData.ActiveItem = testCell;
                    return true;
                }

                return false;
            }
            /// <summary>
            /// Vrátí true, pokud daný řádek smí být použit jako cílový pro Drag and Move - z hlediska Root/Child
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            protected bool TestDataRowEnabled(Row row)
            {
                if (row == null) return false;
                bool isRoot = row.TreeNode.IsRoot;
                if (isRoot && !this.IsTargetOnRowRoot) return false;
                if (!isRoot && !this.IsTargetOnRowChild) return false;
                return true;
            }
            /// <summary>
            /// Vrátí true, pokud daný prvek grafu smí být aktivním cílem pro Drag and Move
            /// </summary>
            /// <param name="testGraphItem"></param>
            /// <returns></returns>
            protected bool TestDataGraphItem(DataGraphItem testGraphItem)
            {
                if (testGraphItem == null) return false;
                if (this.TargetObjectGraphItemAny) return true;
                if (this.TargetObjectGraphItemClassDict == null || this.TargetObjectGraphItemClassDict.Count == 0) return false;
                if (testGraphItem.ItemGId != null && this.TargetObjectGraphItemClassDict.ContainsKey(testGraphItem.ItemGId.ClassId)) return true;
                if (testGraphItem.GroupGId != null && this.TargetObjectGraphItemClassDict.ContainsKey(testGraphItem.GroupGId.ClassId)) return true;
                if (testGraphItem.DataGId != null && this.TargetObjectGraphItemClassDict.ContainsKey(testGraphItem.DataGId.ClassId)) return true;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud daný graf smí být aktivním cílem pro Drag and Move
            /// </summary>
            /// <param name="testGraph"></param>
            /// <returns></returns>
            protected bool TestDataGraph(GTimeGraph testGraph)
            {
                if (testGraph == null) return false;
                return this.TargetObjectGraph;
            }
            /// <summary>
            /// Vrátí true, pokud daná buňka grafu smí být aktivním cílem pro Drag and Move
            /// </summary>
            /// <param name="testCell"></param>
            /// <returns></returns>
            protected bool TestDataCell(GCell testCell)
            {
                if (testCell == null) return false;
                return this.TargetObjectCell;
            }
            #endregion
        }
        /// <summary>
        /// Aktuální data o cíli procesu Drag and Move
        /// </summary>
        protected class DragMoveRowsCurrentDataInfo
        {
            #region Konstruktor a data
            /// <summary>
            /// Vytvoří, naplní a vrátí instanci <see cref="DragMoveRowsCurrentDataInfo"/>, která bude obsahovat veškeré dostupné údaje o cílo přetahování.
            /// Pokud bude zadána pozice myši, bude určen i cílový čas, pokud se myš pohybuje nad časovým grafem. 
            /// Toto není zapotřebí při prostém pohybu myši, ale je to vhodné při závěrečné akci Drop, kdy chceme předat i cílový čas do aplikační funkce.
            /// </summary>
            /// <param name="targetItem"></param>
            /// <param name="mouseAbsolutePoint"></param>
            /// <param name="withTime"></param>
            /// <returns></returns>
            public static DragMoveRowsCurrentDataInfo CreateForTarget(IInteractiveItem targetItem, Point? mouseAbsolutePoint = null, bool withTime = false)
            {
                DragMoveRowsCurrentDataInfo data = new DragMoveRowsCurrentDataInfo(targetItem);

                // Najdeme tabulku (GridTable), ve které se vyskytuje cílový prvek:
                data.GTable = InteractiveObject.SearchForItem(targetItem, true, typeof(GTable)) as GTable;
                if (data.GTable == null) return data;
                data.Table = data.GTable.DataTable;
                data.MainDataTable = data.Table.UserData as MainDataTable;
                if (data.MainDataTable == null) return data;
                data.FullTableName = data.MainDataTable.TableName;

                // Najdeme řádek, do kterého se snažíme přetáhnout data:
                data.GRowHeader = InteractiveObject.SearchForItem(targetItem, true, typeof(GRowHeader)) as GRowHeader;
                data.GCell = InteractiveObject.SearchForItem(targetItem, true, typeof(GCell)) as GCell;
                data.Row = (data.GRowHeader != null ? data.GRowHeader.OwnerRow : (data.GCell != null ? data.GCell.OwnerRow : null));

                // Najdeme graf:
                data.GTimeGraph = InteractiveObject.SearchForItem(targetItem, true, typeof(GTimeGraph)) as GTimeGraph;
                if (data.GTimeGraph != null && mouseAbsolutePoint.HasValue && withTime)
                {
                    Rectangle graphBounds = data.GTimeGraph.BoundsAbsolute;
                    Point mouseRelativePoint = mouseAbsolutePoint.Value.Sub(graphBounds.Location);
                    data.Time = data.GTimeGraph.GetTimeForPosition(mouseRelativePoint.X, AxisTickType.Pixel);
                    data.TimeRound = data.GTimeGraph.GetTimeForPosition(mouseRelativePoint.X, AxisTickType.StdTick);
                }

                // Najdeme prvek grafu:
                data.GTimeGraphItem = InteractiveObject.SearchForItem(targetItem, true, typeof(GTimeGraphItem)) as GTimeGraphItem;
                if (data.GTimeGraphItem != null)
                {
                    GGraphControlPosition position = data.GTimeGraphItem.Position;
                    data.GraphItemPosition = position;
                    switch (position)
                    {
                        case GGraphControlPosition.Item:
                            data.DataGraphItem = data.GTimeGraphItem.Item as DataGraphItem;
                            break;
                        case GGraphControlPosition.Group:
                            GTimeGraphGroup group = data.GTimeGraphItem.Item as GTimeGraphGroup;
                            if (group != null)
                                data.DataGraphGroupItems = group.Items.Where(i => i is DataGraphItem).Cast<DataGraphItem>().ToArray();
                            break;
                    }
                }

                return data;
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            /// <param name="targetItem"></param>
            private DragMoveRowsCurrentDataInfo(IInteractiveItem targetItem)
            {
                this.TargetItem = targetItem;
            }
            /// <summary>
            /// Fyzický cílový prvek, na který ukazuje myš
            /// </summary>
            public IInteractiveItem TargetItem { get; private set; }
            /// <summary>
            /// Grafická komponenta - Tabulka, do které patří prvek
            /// </summary>
            public GTable GTable { get; private set; }
            /// <summary>
            /// Datová tabulka, do které patří prvek
            /// </summary>
            public Table Table { get; private set; }
            /// <summary>
            /// Tabulka Scheduler odpovídající datové tabulce
            /// </summary>
            public MainDataTable MainDataTable { get; private set; }
            /// <summary>
            /// Plné jméno cílové tabulky v Scheduleru
            /// </summary>
            public string FullTableName { get; private set; }
            /// <summary>
            /// Záhlaví řádku, pokud je myš nad Headerem
            /// </summary>
            public GRowHeader GRowHeader { get; private set; }
            /// <summary>
            /// Konkrétní buňka, pokud je myš nad buňkou
            /// </summary>
            public GCell GCell { get; private set; }
            /// <summary>
            /// Datový řádek, na který ukazuje myš
            /// </summary>
            public Row Row { get; private set; }
            /// <summary>
            /// Časový graf, pokud je myš nad grafem
            /// </summary>
            public GTimeGraph GTimeGraph { get; private set; }
            /// <summary>
            /// Konkrétní čas na časovém grafu, nebo null
            /// </summary>
            public DateTime? Time { get; private set; }
            /// <summary>
            /// Zaokrouhlený čas na časovém grafu, nebo null
            /// </summary>
            public DateTime? TimeRound { get; private set; }
            /// <summary>
            /// Grafická komponenta prvku grafu, kam ukazuje myš.
            /// Toto může být grafický prvek konkrétní položky grafu (pozice <see cref="GraphItemPosition"/> = Item) anebo grafický prvek reprezentujcí celou grupu (pozice = Group).
            /// </summary>
            public GTimeGraphItem GTimeGraphItem { get; private set; }
            /// <summary>
            /// Pozice grafické komponenty = zda myš ukazuje na konkrétní prvek grafu, nebo na prostor grupy = spojnice mezi dvěma prvky grafu 
            /// </summary>
            public GGraphControlPosition? GraphItemPosition { get; private set; }
            /// <summary>
            /// Datový prvek grafu odpovídající cílovému prvku, na který je přímo ukázáno 
            /// (tj. když <see cref="GraphItemPosition"/> == <see cref="GGraphControlPosition.Item"/>).
            /// Pokud je cílem skupina, pak je zde null.
            /// </summary>
            public DataGraphItem DataGraphItem { get; private set; }
            /// <summary>
            /// Prvky cílové skupiny, pokud je ukázáno na skupinu = na prostor mezi prvky
            /// (tj. když <see cref="GraphItemPosition"/> == <see cref="GGraphControlPosition.Group"/>).
            /// </summary>
            public DataGraphItem[] DataGraphGroupItems { get; private set; }
            /// <summary>
            /// Testovací datový prvek. Obsahuje buď <see cref="DataGraphItem"/> (to když je nalezen konkrétní prvek) 
            /// anebo obsahuje první prvek z pole <see cref="DataGraphGroupItems"/> (to když je nalezena grupa).
            /// </summary>
            public DataGraphItem TestGraphItem
            {
                get
                {
                    if (this.DataGraphItem != null) return this.DataGraphItem;
                    if (this.DataGraphGroupItems != null && this.DataGraphGroupItems.Length > 0) return this.DataGraphGroupItems[0];
                    return null;
                }
            }
            /// <summary>
            /// ID řádku cíle
            /// </summary>
            public GuiGridRowId GuiGridRowId
            {
                get
                {
                    if (this.MainDataTable == null || this.Row == null) return null;
                    return this.MainDataTable.GetGridRowId(this.Row);
                }
            }
            /// <summary>
            /// Ukazatel (Pointer) na cílový prvek grafu <see cref="DataGraphItem"/>, na který je přímo ukázáno 
            /// (tj. když <see cref="GraphItemPosition"/> == <see cref="GGraphControlPosition.Item"/>).
            /// Pokud je cílem skupina, pak je zde null.
            /// <para/>
            /// Pozor, tato hodnota se dohledává On-Demand, je vhodno ji vyhodnotit jedenkrát a uchovat výsledek.
            /// </summary>
            public GuiGridItemId GuiGridItemId
            {
                get
                {
                    if (this.MainDataTable == null || this.DataGraphItem == null) return null;
                    return this.MainDataTable.GetGridItemId(this.DataGraphItem);
                }
            }
            /// <summary>
            /// ID všech prvků cílové skupiny, pokud je ukázáno na skupinu = na prostor mezi prvky
            /// (tj. když <see cref="GraphItemPosition"/> == <see cref="GGraphControlPosition.Group"/>).
            /// <para/>
            /// Pozor, tato hodnota se dohledává On-Demand, je vhodno ji vyhodnotit jedenkrát a uchovat výsledek.
            /// </summary>
            public GuiGridItemId[] GuiGridGroupItemIds
            {
                get
                {
                    if (this.MainDataTable == null || this.DataGraphGroupItems == null) return null;
                    return this.DataGraphGroupItems.Select(dgi => this.MainDataTable.GetGridItemId(dgi)).ToArray();
                }
            }
            /// <summary>
            /// Výsledek = cílový prvek, který je ochoten přijmout přemísťované prvky (řádky).
            /// Metoda sem může vložit referenci na objekt, který bude příjemcem procesu Drag and Move.
            /// </summary>
            public IInteractiveItem ActiveItem { get; set; }
            #endregion
        }
        /// <summary>
        /// Metoda detekuje klíčové slovo a jeho třídu.
        /// Pokud hodnota (text) = "MasterClass1188", a (keyWord) = "MasterClass", pak tato metoda rozpozná shodu,
        /// a zkusí detekovat číslo následující přímo za klíčovým slovem.
        /// Pokud číslo najde, tak hodnotu parametru (value) vloží do (result), a nalezené číslo do (classNumber).
        /// Pak tedy a do ref parametru (prefix) vloží "MasterClass" a do (classNumber) vloží číslo 1188.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="keyWord"></param>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <param name="classNumber"></param>
        protected static void ParsePrefixClass(string text, string keyWord, string value, ref string result, ref int classNumber)
        {
            int kl = keyWord.Length;
            if (text.Length < kl || text.Substring(0, kl) != keyWord) return;        // Zadaný text (value) NEzačíná daným klíčovým slovem.
            int number;
            if (!Int32.TryParse(text.Substring(kl), out number)) return;             // Za daným klíčovým slovem nejsou čísla
            result = value;
            classNumber = number;
        }
        #endregion
        #endregion
        #region Drag and Drop Interaktivní přidávání prvků pomocí myši (kreslení MousePaint)
        #region Definice LinkPairs z GridProperties = povolené linky (načtení z GridProperties.PaintLinkPairs, vyhodnocení pro konkrétní prvek grafu)
        /// <summary>
        /// Načte (připraví) podklady pro interaktivní zadávání dat
        /// </summary>
        protected void LoadPaint()
        {
            this.LoadPaintLink();
        }
        /// <summary>
        /// Načte (připraví) podklady pro interaktivní zadávání vztahů mezi prvky grafu
        /// </summary>
        protected void LoadPaintLink()
        {
            this.MousePaintDefinitionLinks = null;
            this._MousePaintCurrentReset();
            GuiMousePaintLink guiPaints = this.GuiGrid.GridProperties?.MousePaintLink;
            if (guiPaints == null || guiPaints.PaintLinkPairs == null || guiPaints.PaintLinkPairs.Count == 0) return;

            this.LoadPaintLinkPairs(guiPaints);
            this.LoadPaintLinkParam(guiPaints);
        }
        /// <summary>
        /// Načte (připraví) podklady pro interaktivní zadávání vztahů mezi prvky grafu - načte párové definice (Source-Target)
        /// </summary>
        protected void LoadPaintLinkPairs(GuiMousePaintLink guiPaints)
        {
            List<PaintLinkPair> result = new List<PaintLinkPair>();
            List<string> linkPairs = guiPaints.PaintLinkPairs;
            if (linkPairs != null && linkPairs.Count > 0)
            {
                foreach (string item in linkPairs)
                {
                    PaintLinkPair linkPair = PaintLinkPair.CreateFrom(item);
                    if (linkPair != null)
                        result.Add(linkPair);
                }
            }
            this.MousePaintDefinitionLinks = result.ToArray();
        }
        /// <summary>
        /// Načte (připraví) podklady pro interaktivní zadávání vztahů mezi prvky grafu - načte definici GUI
        /// </summary>
        private void LoadPaintLinkParam(GuiMousePaintLink guiPaints)
        {
            this.MousePaintDefinitionParam = new MousePaintLinkParam(guiPaints);
        }
        /// <summary>
        /// Vrátí true, pokud mezi zadanými prvky jsou některé, které povolují kreslit LinkLine z daného zdroje
        /// <para/>
        /// Pozor: tato metoda plní nalezená data (zdroje) do <see cref="MousePaintCurrentSourceLinks"/> a <see cref="MousePaintCurrentSourceIds"/>,
        /// a nuluje data v <see cref="MousePaintCurrentTargetLinks"/> a <see cref="MousePaintCurrentTargetIds"/> !
        /// </summary>
        /// <param name="guiIds"></param>
        /// <returns></returns>
        protected bool MousePaintLinkSourceAllowed(IEnumerable<GuiId> guiIds)
        {
            GuiId[] acceptIds;
            this.MousePaintCurrentSourceLinks = MousePaintLinkAllowed(this.MousePaintDefinitionLinks, guiIds, true, out acceptIds);
            this.MousePaintCurrentSourceIds = acceptIds;
            this.MousePaintCurrentTargetLinks = null;
            this.MousePaintCurrentTargetIds = null;
            return this.MousePaintCurrentSourceExists;
        }
        /// <summary>
        /// Vrátí true, pokud mezi zadanými prvky jsou některé, které povolují kreslit LinkLine do daného cíle.
        /// <para/>
        /// Pozor: tato metoda plní nalezená data (cíle) do <see cref="MousePaintCurrentTargetLinks"/> a <see cref="MousePaintCurrentTargetIds"/> !
        /// </summary>
        /// <param name="guiIds"></param>
        /// <returns></returns>
        protected bool MousePaintLinkTargetAllowed(IEnumerable<GuiId> guiIds)
        {
            GuiId[] acceptIds;
            this.MousePaintCurrentTargetLinks = MousePaintLinkAllowed(this.MousePaintCurrentSourceLinks, guiIds, false, out acceptIds);
            this.MousePaintCurrentTargetIds = acceptIds;
            return this.MousePaintCurrentTargetExists;
        }
        /// <summary>
        /// Metoda vrátí true, když zdroj a cíl pro akci MousePaint.Link existují a jsou shodné.
        /// </summary>
        /// <returns></returns>
        protected bool MousePaintLinkTargetEqualSource()
        {
            GuiId[] sourceIds = this.MousePaintCurrentSourceIds;
            GuiId[] targetIds = this.MousePaintCurrentTargetIds;
            if (sourceIds == null || targetIds == null) return false;          // Pokud některé pole je null, pak nejsou shodné
            if (sourceIds.Length != targetIds.Length) return false;            // Pokud se liší délka, pak nemohou být shodné (pole neobsahují duplicity)
            var differents = sourceIds.SyncDifferent(g => g, targetIds);       // Získám seznam obsahující GuiId, které jsou Differenc (tj. existují v jednom poli, ale ne v druhém)
            return (differents.Length == 0);                                   // Pokud NEJSOU žádné rozdíly, pak jsou obě pole SHODNÁ (bez ohledu na pořadí prvků v nich)
        }
        /// <summary>
        /// Metoda vyhledá povolené linky v daném poli <paramref name="linkPairs"/> pro dané prvky <paramref name="guiIds"/>, na straně Source/Target podle <paramref name="onSourceSide"/>.
        /// Vyhovující vstupní <see cref="GuiId"/> uloží do out <paramref name="acceptIds"/>.
        /// </summary>
        /// <param name="linkPairs"></param>
        /// <param name="guiIds"></param>
        /// <param name="onSourceSide"></param>
        /// <param name="acceptIds"></param>
        /// <returns></returns>
        protected PaintLinkPair[] MousePaintLinkAllowed(PaintLinkPair[] linkPairs, IEnumerable<GuiId> guiIds, bool onSourceSide, out GuiId[] acceptIds)
        {
            acceptIds = null;
            if (linkPairs == null || guiIds == null) return null;
            if (linkPairs == null || linkPairs.Length == 0) return null;

            Dictionary<GuiId, object> acceptDict = new Dictionary<GuiId, object>();
            List<PaintLinkPair> links = new List<PaintLinkPair>();
            foreach (PaintLinkPair linkPair in linkPairs)
            {
                bool linkEnabled = false;
                foreach (GuiId guiId in guiIds)
                {
                    if (linkPair.IsEnabled(guiId, onSourceSide))
                    {
                        linkEnabled = true;
                        if (!acceptDict.ContainsKey(guiId))
                            acceptDict.Add(guiId, null);
                    }
                }
                if (linkEnabled)
                    links.Add(linkPair);
            }

            acceptIds = acceptDict.Keys.ToArray();
            return links.ToArray();
        }
        #region class PaintLinkPair : Třída obsahující data z jedné položky GuiGridProperties.MousePaintLink
        /// <summary>
        /// Třída obsahující data z jedné položky <see cref="GuiGridProperties.MousePaintLink"/>:
        /// Typ prvku vlevo, Typ prvku vpravo.
        /// Instance dovoluje testovat, zda na daném prvku Source nebo Target je povoleno navázat Link.
        /// </summary>
        protected class PaintLinkPair
        {
            #region Konstrukce, proměnné, tvorba
            /// <summary>
            /// Vytvoří a vrátí instanci <see cref="PaintLinkPair"/> z dodaného textu.
            /// Text má typicky tvar: "C1190:E111", tedy pár: typ pozice (Class/Master/Entry) a číslo třídy.
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public static PaintLinkPair CreateFrom(string text)
            {
                if (String.IsNullOrEmpty(text)) return null;
                string[] items = text.Split(':', ';', '-', ',', '/');

                PaintLinkPair result = null;
                foreach (string item in items)
                {
                    ClassPosition position;
                    int cls;
                    if (TryParseText(item, out position, out cls))
                    {
                        if (result == null)
                        {
                            result = new PaintLinkPair();
                            result._SourcePosition = position;
                            result._SourceClass = cls;
                        }
                        else if (!result._TargetExists)
                        {
                            result._TargetPosition = position;
                            result._TargetClass = cls;
                        }
                    }
                }
                return result;
            }
            /// <summary>
            /// Z dodaného textu detekuje typ a číslo třídy.
            /// Na vstupu je např. " C 1190", na výstupu je true a out parametry jsou: type = "C", cls = 1190.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="position"></param>
            /// <param name="cls"></param>
            /// <returns></returns>
            private static bool TryParseText(string item, out ClassPosition position, out int cls)
            {
                position = ClassPosition.None;
                cls = 0;
                if (String.IsNullOrEmpty(item)) return false;
                string text = item.Trim().ToUpper();
                if (text.Length < 2) return false;
                string type = text.Substring(0, 1);
                position = (type == "C" ? ClassPosition.Class : (type == "M" ? ClassPosition.Master : (type == "E" ? ClassPosition.Entry : ClassPosition.None)));
                if (position == ClassPosition.None) return false;
                if (!Int32.TryParse(text.Substring(1), out cls)) return false;
                return (cls > 0);
            }
            /// <summary>
            /// Konstruktor je privátní
            /// </summary>
            private PaintLinkPair()
            {
                this._SourcePosition = ClassPosition.None;
                this._SourceClass = 0;
                this._TargetPosition = ClassPosition.None;
                this._TargetClass = 0;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string text = this._SourcePosition + this._SourceClass.ToString();
                if (this._TargetExists)
                    text += ":" + this._TargetPosition + this._TargetClass.ToString();
                return text;
            }
            private ClassPosition _SourcePosition;
            private int _SourceClass;
            private ClassPosition _TargetPosition;
            private int _TargetClass;
            private bool _TargetExists { get { return (_TargetPosition != ClassPosition.None && _TargetClass > 0); } }
            /// <summary>
            /// Pozice (Celá třída, Master, Entry)
            /// </summary>
            private enum ClassPosition { None, Class, Master, Entry }
            #endregion
            #region Test přípustnosti zdroje a cíle
            /// <summary>
            /// Vrátí true, pokud daný <see cref="GuiId"/> vyhovuje dané pozici a třídě definované zde jako Source menp Target, podle parametru <paramref name="onSourceSide"/>
            /// </summary>
            /// <param name="guiId"></param>
            /// <param name="onSourceSide"></param>
            /// <returns></returns>
            public bool IsEnabled(GuiId guiId, bool onSourceSide)
            {
                return (onSourceSide ? this.IsSourceEnabled(guiId) : this.IsTargetEnabled(guiId));
            }
            /// <summary>
            /// Vrátí true, pokud daný <see cref="GuiId"/> vyhovuje dané pozici a třídě definované zde jako Source
            /// </summary>
            /// <param name="guiId"></param>
            /// <returns></returns>
            public bool IsSourceEnabled(GuiId guiId)
            {
                return _IsEnabled(guiId, this._SourcePosition, this._SourceClass, true);
            }
            /// <summary>
            /// Vrátí true, pokud daný <see cref="GuiId"/> vyhovuje dané pozici a třídě definované zde jako Target
            /// </summary>
            /// <param name="guiId"></param>
            /// <returns></returns>
            public bool IsTargetEnabled(GuiId guiId)
            {
                return _IsEnabled(guiId, this._TargetPosition, this._TargetClass, this._TargetExists);
            }
            /// <summary>
            /// Vrátí true, pokud daný <see cref="GuiId"/> vyhovuje dané pozici a třídě
            /// </summary>
            /// <param name="guiId"></param>
            /// <param name="position"></param>
            /// <param name="classNumber"></param>
            /// <param name="testValues">true = Je požadován test hodnot / false = stačí když GId nebude null (má význam pouze pro Target, když _TargetExists je false)</param>
            /// <returns></returns>
            private bool _IsEnabled(GuiId guiId, ClassPosition position, int classNumber, bool testValues)
            { 
                if (guiId == null) return false;
                if (!testValues) return true;
                if (guiId.ClassId != this._SourceClass) return false;
                switch (this._SourcePosition)
                {
                    case ClassPosition.Class: return true;
                    case ClassPosition.Master: return (!guiId.EntryId.HasValue);
                    case ClassPosition.Entry: return (guiId.EntryId.HasValue);
                }
                return false;
            }
            #endregion
        }
        #endregion
        #region class MousePaintLinkParam : Třída pro přenos parametrů z GuiMousePaintLink do Scheduleru
        /// <summary>
        /// Třída pro přenos parametrů z <see cref="GuiMousePaintLink"/> do Scheduleru
        /// </summary>
        protected class MousePaintLinkParam : GuiMousePaintLink
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="guiPaints"></param>
            public MousePaintLinkParam(GuiMousePaintLink guiPaints)
            {
                this.PaintLineShape = GetValue(guiPaints.PaintLineShape, GuiLineShape.SCurveHorizontal);
                this.EnabledLineForeColor = GetValue(guiPaints.EnabledLineForeColor, Color.LightGreen);
                this.EnabledLineBackColor = GetValue(guiPaints.EnabledLineBackColor, Color.DimGray);
                this.EnabledLineWidth = GetValue(guiPaints.EnabledLineWidth, 5);
                this.EnabledLineEndingCap = GetValue(guiPaints.EnabledLineEndingCap, GuiLineEndingCap.ArrowAnchor);
                this.DisabledLineForeColor = GetValue(guiPaints.DisabledLineForeColor, Color.LightGray);
                this.DisabledLineBackColor = GetValue(guiPaints.DisabledLineBackColor, Color.Gray);
                this.DisabledLineWidth = GetValue(guiPaints.DisabledLineWidth, 3);
                this.DisabledLineEndingCap = GetValue(guiPaints.DisabledLineEndingCap, GuiLineEndingCap.Round);

                this.LineShape = GetEnum(this.PaintLineShape.Value, MousePaintObjectType.SCurveHorizontal);
                this.EnabledEndingCap = GetEnum(this.EnabledLineEndingCap.Value, System.Drawing.Drawing2D.LineCap.ArrowAnchor);
                this.DisabledEndingCap = GetEnum(this.DisabledLineEndingCap.Value, System.Drawing.Drawing2D.LineCap.Round);
            }
            private static T GetValue<T>(T? value, T defaultValue) where T : struct
            {
                return (value.HasValue ? value.Value : defaultValue);
            }

            private static TOut GetEnum<TInp, TOut>(TInp inpValue, TOut defaultValue) where TInp : struct where TOut : struct
            {
                string name = Enum.GetName(typeof(TInp), inpValue);
                TOut outValue;
                if (Enum.TryParse<TOut>(name, out outValue)) return outValue;
                return defaultValue;
            }
            /// <summary>
            /// Typ čáry
            /// </summary>
            public MousePaintObjectType LineShape { get; set; }
            /// <summary>
            /// Druh zakončení spojovací linky, pokud ukazuje na povolený cíl
            /// </summary>
            public System.Drawing.Drawing2D.LineCap EnabledEndingCap { get; set; }
            /// <summary>
            /// Druh zakončení spojovací linky, pokud ukazuje na NEpovolený cíl
            /// </summary>
            public System.Drawing.Drawing2D.LineCap DisabledEndingCap { get; set; }
            /// <summary>
            /// Přenese svoje hodnoty do daného kreslícího objektu, pro daný stav Enabled
            /// </summary>
            /// <param name="paintInfo"></param>
            /// <param name="isEnabled"></param>
            public void SetParamsTo(MousePaintInfo paintInfo, bool isEnabled)
            {
                paintInfo.ObjectType = this.LineShape;
                if (isEnabled)
                {
                    paintInfo.LineColor = this.EnabledLineForeColor.Value;
                    paintInfo.FillColor = this.EnabledLineBackColor.Value;
                    paintInfo.LineWidth = this.EnabledLineWidth.Value;
                    paintInfo.EndCap = this.EnabledEndingCap;
                }
                else
                {
                    paintInfo.LineColor = this.DisabledLineForeColor.Value;
                    paintInfo.FillColor = this.DisabledLineBackColor.Value;
                    paintInfo.LineWidth = this.DisabledLineWidth.Value;
                    paintInfo.EndCap = this.DisabledEndingCap;
                }
            }
        }
        #endregion
        /// <summary>
        /// Soupis všech údajů o povolení kreslit linky mezi prvky, pochází z <see cref="GuiGridProperties.MousePaintLink"/>
        /// </summary>
        private PaintLinkPair[] MousePaintDefinitionLinks;
        /// <summary>
        /// Definice GUI parametrů pro kreslení LinkLine
        /// </summary>
        private MousePaintLinkParam MousePaintDefinitionParam;
        /// <summary>
        /// Obsahuje true, pokud je obecně povoleno kreslení (tj. pokud máme platné definice vztahů v <see cref="MousePaintDefinitionLinks"/>)
        /// </summary>
        private bool MousePaintDefinitionExists { get { return (this.MousePaintDefinitionLinks != null && this.MousePaintDefinitionLinks.Length > 0); } }
        /// <summary>
        /// Aktuálně platné prvky z <see cref="MousePaintDefinitionLinks"/>, které povolují kreslení linky z aktuálního startu
        /// </summary>
        private PaintLinkPair[] MousePaintCurrentSourceLinks;
        /// <summary>
        /// Aktuální ID zdrojového prvku (start), odkud se kreslí
        /// </summary>
        private GuiId[] MousePaintCurrentSourceIds;
        /// <summary>
        /// Obsahuje true, pokud aktuálně je povoleno kreslení z určitého zdroje
        /// </summary>
        private bool MousePaintCurrentSourceExists { get { return (this.MousePaintCurrentSourceIds != null && this.MousePaintCurrentSourceIds.Length > 0); } }
        /// <summary>
        /// Aktuálně platné prvky z <see cref="MousePaintCurrentSourceLinks"/>, které povolují kreslení linky do aktuálního cíle
        /// </summary>
        private PaintLinkPair[] MousePaintCurrentTargetLinks;
        /// <summary>
        /// Aktuální ID cílového prvku (target), kam se kreslí
        /// </summary>
        private GuiId[] MousePaintCurrentTargetIds;
        /// <summary>
        /// Obsahuje true, pokud aktuálně je povoleno kreslení do určitého cíle
        /// </summary>
        private bool MousePaintCurrentTargetExists { get { return (this.MousePaintCurrentTargetIds != null && this.MousePaintCurrentTargetIds.Length > 0); } }
        /// <summary>
        /// Uvolní z paměti všechna provozní data MousePaint
        /// </summary>
        private void _MousePaintCurrentReset()
        {
            this.MousePaintCurrentSourceLinks = null;
            this.MousePaintCurrentSourceIds = null;
            this.MousePaintCurrentTargetLinks = null;
            this.MousePaintCurrentTargetIds = null;
        }
        #endregion
        #region Obsluha GUI eventů = testování povolení vytvářet linky, vizuální reakce na aktuální stav, vyvolání serverové akce Commit
        /// <summary>
        /// Aplikace zde zjistí, zda v daném bodě controlu je možno zahájit operaci MousePaint = uživatelsky nakreslit nějaký tvar, a následně jej převzít k dalšímu zpracování.
        /// Myš se nyní nachází na bodě, kde by uživatel mohl zmáčknout myš (nebo ji už dokonce právě nyní zmáčkl, 
        /// to když <see cref="GInteractiveMousePaintArgs.InteractiveChange"/> == <see cref="GInteractiveChangeState.LeftDown"/> nebo RightDown).
        /// </summary>
        /// <param name="e"></param>
        internal void InteractiveMousePaintProcessStart(GInteractiveMousePaintArgs e)
        {
            // Pokud není aktivní režim LinkLine (=povoluje se v Toolbaru, tlačítkem, které má GuiActionType.EnableMousePaintLinkLine), skončím hned:
            if (!this.MainData.MousePaintLinkLineActive) return;
            if (!this.MousePaintDefinitionExists) return;

            // Na základě grafického prvku e.CurrentItem najdu identifikátory DATOVÝCH prvků, které jsou tímto GUI prvkem reprezentovány:
            GuiGridItemId[] gridItemIds = GetGuiGridItems(e.CurrentItem, true);
            if (gridItemIds == null || gridItemIds.Length == 0) return;

            // Vyhledám prvky PaintLinkPair, které povolují MousePaint.LinkPair pro aktuální = zdrojový prvek:
            GuiId[] guiIds = GetGuiIds(gridItemIds, addRow: false);
            e.IsEnabled = this.MousePaintLinkSourceAllowed(guiIds);
            if (e.IsEnabled)
                e.CursorType = SysCursorType.Cross;
        }
        /// <summary>
        /// Aplikace zde zjistí, zda v daném bodě controlu je možno dokončit kreslení, tedy zda daný bod a prvek na něm je vhodným cílem.
        /// </summary>
        /// <param name="e"></param>
        internal void InteractiveMousePaintProcessTarget(GInteractiveMousePaintArgs e)
        {
            // Pokud není aktivní režim LinkLine (=povoluje se v Toolbaru, tlačítkem, které má GuiActionType.EnableMousePaintLinkLine), skončím hned:
            if (!this.MainData.MousePaintLinkLineActive) return;
            if (!this.MousePaintCurrentSourceExists) return;

            bool isEnabled = this.InteractiveMousePaintSearchTarget(e);

            // Hledáme jen tehdy, pokud cílový prvek leží v tabulce, která je naší tabulkou (nelze dávat LinkLine do cizí tabulky!):
            MainDataTable dataTable;
            if (Scheduler.MainData.InteractiveMousePaintTryGetTable(e.CurrentItem, out dataTable) && Object.ReferenceEquals(dataTable, this))
            {
                // Na základě grafického prvku e.CurrentItem (=aktuální cíl) najdu identifikátory DATOVÝCH prvků, které jsou tímto GUI prvkem reprezentovány:
                GuiGridItemId[] gridItemIds = GetGuiGridItems(e.CurrentItem, true);
                if (gridItemIds != null && gridItemIds.Length > 0)
                {
                    // Vyhledám prvky PaintLinkPair, které povolují MousePaint.LinkPair pro aktuální = cílový prvek:
                    GuiId[] guiIds = GetGuiIds(gridItemIds, addRow: false);
                    isEnabled = this.MousePaintLinkTargetAllowed(guiIds);
                    if (isEnabled)
                        isEnabled = !this.MousePaintLinkTargetEqualSource();
                }
            }

            // Omezím souřadnici cílového bodu (pro vykreslení linky) jen do prostoru this tabulky:
            Rectangle tableContentBounds = this.GTableRow.GetAbsoluteBoundsForArea(TableAreaType.RowData);
            e.PaintInfo.EndPoint = e.CurrentPoint.FitInto(tableContentBounds);

            // Nyní na základě povolení cíle nastavím zobrazení:
            this.MousePaintDefinitionParam.SetParamsTo(e.PaintInfo, isEnabled);
            e.CursorType = SysCursorType.Hand;
        }
        /// <summary>
        /// Metoda ověří, zda aktuální GUI cíl je povolený z hlediska pravidel.
        /// Pokud ano, vrací true a správně nastaví pole 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool InteractiveMousePaintSearchTarget(GInteractiveMousePaintArgs e)
        {
            // Pokud není aktivní režim LinkLine (=povoluje se v Toolbaru, tlačítkem, které má GuiActionType.EnableMousePaintLinkLine), skončím hned:
            if (!this.MainData.MousePaintLinkLineActive) return false;
            if (!this.MousePaintCurrentSourceExists) return false;

            bool isEnabled = false;

            // Hledáme jen tehdy, pokud cílový prvek leží v tabulce, která je naší tabulkou (nelze dávat LinkLine do cizí tabulky!):
            MainDataTable dataTable;
            if (Scheduler.MainData.InteractiveMousePaintTryGetTable(e.CurrentItem, out dataTable) && Object.ReferenceEquals(dataTable, this))
            {
                // Na základě grafického prvku e.CurrentItem (=aktuální cíl) najdu identifikátory DATOVÝCH prvků, které jsou tímto GUI prvkem reprezentovány:
                GuiGridItemId[] gridItemIds = GetGuiGridItems(e.CurrentItem, true);
                if (gridItemIds != null && gridItemIds.Length > 0)
                {
                    // Vyhledám prvky PaintLinkPair, které povolují MousePaint.LinkPair pro aktuální = cílový prvek:
                    GuiId[] guiIds = GetGuiIds(gridItemIds, addRow: false);
                    isEnabled = this.MousePaintLinkTargetAllowed(guiIds);
                    if (isEnabled)
                        isEnabled = !this.MousePaintLinkTargetEqualSource();
                }
            }

            return isEnabled;
        }
        /// <summary>
        /// Aplikace zde zajistí zpracování vykresleného tvaru MousePaint = tedy převezme si souřadnice a prvky, a vytvoří z nich nějaká data.
        /// </summary>
        /// <param name="e"></param>
        internal void InteractiveMousePaintProcessCommit(GInteractiveMousePaintArgs e)
        {
            if (!this.InteractiveMousePaintSearchTarget(e)) return;     // Není nalezen vhodný cíl

            // Máme nalezen zdrojový prvek, máme i cílový prvek, myš je uvolněna => zavoláme aplikaci:
            GuiRequestInteractiveDraw draw = new GuiRequestInteractiveDraw();
            draw.SourceItem = GetGridItemId(e.StartItem);
            draw.SourcePoint = e.StartPoint;
            draw.SourceBounds = GetGraphGroupAbsBounds(e.StartItem);
            draw.SourceTime = GetTimeOnGraph(e.StartItem, draw.SourcePoint);
            draw.TargetItem = GetGridItemId(e.CurrentItem);
            draw.TargetPoint = e.CurrentPoint;
            draw.TargetBounds = GetGraphGroupAbsBounds(e.CurrentItem);
            draw.TargetTime = GetTimeOnGraph(e.CurrentItem, draw.TargetPoint);

            // Sestavit argument pro volání IHost:
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_InteractiveDraw;
            request.CurrentState = this.IMainData.CreateGuiCurrentState();
            request.InteractiveDraw = draw;

            // Zavolat Host:
            this.IMainData.CallAppHostFunction(request, this.InteractiveMousePaintProcessResponse, TimeSpan.FromMilliseconds(1500));
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice prvku grafu
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Rectangle? GetGraphGroupAbsBounds(IInteractiveItem item)
        {
            GTimeGraphItem timeGraphItem = InteractiveObject.SearchForItem(item, true, typeof(GTimeGraphItem)) as GTimeGraphItem;
            if (timeGraphItem == null) return null;
            return timeGraphItem.BoundsAbsolute;
        }
        /// <summary>
        /// Metoda vrátí čas na grafu (graf bude vyhledán z dodaného prvku), čas odpovídající dané absolutní souřadnici
        /// </summary>
        /// <param name="item"></param>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        private DateTime? GetTimeOnGraph(IInteractiveItem item, Point? absolutePoint)
        {
            DateTime? time = null;
            // Najdeme graf:
            GTimeGraph graph = InteractiveObject.SearchForItem(item, true, typeof(GTimeGraph)) as GTimeGraph;
            if (graph != null && absolutePoint.HasValue)
            {
                Rectangle graphBounds = graph.BoundsAbsolute;
                Point relativePoint = absolutePoint.Value.Sub(graphBounds.Location);
                time = graph.GetTimeForPosition(relativePoint.X, AxisTickType.Pixel);
            }
            return time;
        }
        /// <summary>
        /// Metoda, která obdrží odpovědi z aplikační funkce, a podle nich zajistí patřičné změny v tabulkách.
        /// </summary>
        /// <param name="response"></param>
        protected void InteractiveMousePaintProcessResponse(AppHostResponseArgs response)
        {
            if (response == null || response.GuiResponse == null) return;
            this.IMainData.ProcessAppHostResponse(response.GuiResponse);
        }
        #endregion
        #endregion
        #region Kontextové menu k řádku, ke grafu, k jednotlivému prvku grafu
        /// <summary>
        /// Eventhandler události RightClick v prostoru buňky tabulky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void TableRow_ActiveCellRightClick(object sender, GPropertyEventArgs<Cell> args)
        {
            args.InteractiveArgs.ContextMenu = this.GetContextMenu(args.Value);
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném grafu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForGraph(ItemActionArgs args)
        {
            return this.GetContextMenu(args);
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném prvku grafu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForItem(ItemActionArgs args)
        {
            return this.GetContextMenu(args);
        }
        /// <summary>
        /// Vytvoří a vrátí kontextové menu pro danou buňku tabulky, ale bez časového grafu
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenu(Cell cell)
        {
            GuiContextMenuRunArgs menuRunArgs = new GuiContextMenuRunArgs();
            menuRunArgs.ContextItemId = new GuiGridItemId() { TableName = this.TableName, RowId = cell.Row.RecordGId };
            return this.IMainData.CreateContextMenu(menuRunArgs, null);
        }
        /// <summary>
        /// Vytvoří a vrátí kontextové menu pro daný prvek
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenu(ItemActionArgs args)
        {
            int x = args.Graph.BoundsInfo.GetRelPoint(args.ActionPoint.Value).X;
            GuiContextMenuRunArgs menuRunArgs = new GuiContextMenuRunArgs();
            menuRunArgs.ContextItemId = this.GetGridItemId(args);
            menuRunArgs.ClickTime = args.Graph.GetTimeForPosition(x);
            return this.IMainData.CreateContextMenu(menuRunArgs, null);
        }
        #endregion
        #region Implementace ITimeGraphDataSource: Zdroj dat pro grafy: tvorba textu, tooltipu, kontextové menu, podpora Drag and Drop
        /// <summary>
        /// Připraví text pro položku grafu
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareText(CreateTextArgs args)
        {
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
            if (graphItem == null) return;

            Row infoRow = this.GetTableTextRow(graphItem);
            if (infoRow == null) return;

            string textAll = "";                           // Kompletní text, víceřádkový (oddělovač = Cr+Lf)
            string textRow = "";                           // Text aktuálního řádku, střádáme jej dokud readRow je true
            bool readRow = true;                           // true = zkusme přidat další sloupce do řádku textRow; false = další sloupce nepřidávcat (ale můžeme hledat nový řádek)
            int heightRow = 0;                             // Výška aktuálního řádku, o to se sníží height
            int width = args.GraphItemSize.Width - 4;      // Šířka prostoru, který máme vyplnit textem; v průběhu metody se nezmění
            int height = args.GraphItemSize.Height - 4;    // Výška prostoru, který máme vyplnit textem; v průběhu metody se snižuje o vložené řádky
            string rowDelimiter = this.GuiGrid.GraphProperties.GraphTextRowDelimiter;
            bool hasRowDelimiter = !String.IsNullOrEmpty(rowDelimiter);                  // true = máme oddělovač řádků, výsledek může být víceřádkový
            foreach (Column column in infoRow.Table.Columns)
            {
                Cell cell = infoRow[column];
                if ((cell.ValueType == TableValueType.Text || cell.ValueType == TableValueType.TextRelation) && cell.Value != null)
                {   // Textový sloupec; může být neviditelný, může to být oddělovač řádků:
                    string value = cell.Value.ToString().Trim();
                    if (hasRowDelimiter && String.Equals(value, rowDelimiter))
                    {   // Sloupec obsahuje oddělovač řádků:
                        _GraphItemSumTextRows(ref textAll, ref textRow, ref height, ref heightRow);
                        if (height <= 0) break;
                        readRow = true;
                    }
                    else if (column.IsVisible && value.Length > 0 && readRow)
                    {   // Sloupec je viditelný, obsahuje něco neprázdného, a do našeho řádku textRow se má ještě něco přidávat:
                        textRow = (textRow.Length > 0 ? textRow + " " : "") + value;
                        Size size = args.MeasureString(textRow);
                        if (size.Height > heightRow)
                            heightRow = size.Height;       // Střádám si Max(Height)
                        if (size.Width > width)
                            readRow = false;               // Pokud jsme naplnili disponibilní šířku prostoru, pak další text do jednoho řádku už nepřidáme.
                    }
                }
                if (!readRow && (!hasRowDelimiter || height <= 0))   // Když už je řádek naplněn (co do šířky), a nelze očekávat že by se do textu vešel další řádek, skončíme.
                    break;
            }
            _GraphItemSumTextRows(ref textAll, ref textRow, ref height, ref heightRow);
            args.Text = textAll;
        }
        /// <summary>
        /// Metoda zajistí přidání textu řádku textRow do celkového textu textAll, a zpracování výšky textu.
        /// </summary>
        /// <param name="textAll"></param>
        /// <param name="textRow"></param>
        /// <param name="height"></param>
        /// <param name="heightRow"></param>
        private static void _GraphItemSumTextRows(ref string textAll, ref string textRow, ref int height, ref int heightRow)
        {
            if (textRow.Length > 0 && heightRow > 0 && (textAll.Length == 0 || heightRow <= height))
            {   // Máme nastřádaný text aktuálního řádku, a známe jeho výšku, a (výsledný text je zatím prázdný, anebo se do něj na výšku další řádek ještě vejde):
                //  => přidáme jej do textAll:
                textAll = (textAll.Length > 0 ? textAll + "\r\n" : "") + textRow;
                height -= heightRow;
            }
            textRow = "";
            heightRow = 0;
        }
        /// <summary>
        /// Připraví tooltip pro položku grafu.
        /// Text je připraven jako tabulka: obsahuje řádky oddělené NewLine, a sloupce oddělené Tab.
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareToolTip(CreateToolTipArgs args)
        {
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
            if (graphItem == null) return;

            Row infoRow = this.GetTableToolTipRow(graphItem);
            if (infoRow == null) return;

            GId recordGId = infoRow.RecordGId;
            args.ToolTipData.TitleText = (recordGId != null ? recordGId.ClassName : "INFORMACE O POLOŽCE");

            StringBuilder sb = new StringBuilder();
            sb.Append(args.TimeText);
            foreach (Column column in infoRow.Table.Columns)
            {
                if (!column.IsVisible) continue;
                Cell cell = infoRow[column];
                if (cell.ValueType == TableValueType.Text || cell.ValueType == TableValueType.TextRelation)
                    sb.AppendLine(column.Title + "\t" + cell.Value);
            }
            string text = sb.ToString();
            args.ToolTipData.InfoText = text;
            args.ToolTipData.InfoUseTabs = true;
        }
        /// <summary>
        /// Najde vztahy pro daný prvek
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemCreateLinks(CreateLinksArgs args)
        {
            bool wholeTask = GraphItemLinksForWholeTask(args.ItemEvent);
            bool asSCurve = (this.Config != null && this.Config.GuiEditShowLinkAsSCurve);
            LinkLineType defaultLineType = (asSCurve ? LinkLineType.SCurveHorizontal : LinkLineType.StraightLine);

            GTimeGraphItem currentItem = args.ItemControl ?? args.GroupControl;     // Na tomto prvku začne hledání. Může to být prvek konkrétní, anebo prvek grupy.
            args.Links = this.SearchForGraphLink(currentItem, args.SearchSidePrev, args.SearchSideNext, wholeTask, defaultLineType);
        }
        /// <summary>
        /// Metoda vrátí true, pokud pro daný typ události se podle konfigurace mají zobrazovat vztahy (Linky) pro všechny prvky tasku = celá sekvence,
        /// nebo false = jen nejbližší sousedi Prev a Next
        /// </summary>
        /// <param name="itemEvent"></param>
        /// <returns></returns>
        protected bool GraphItemLinksForWholeTask(CreateLinksItemEventType itemEvent)
        {
            if (this.Config == null) return false;
            switch (itemEvent)
            {
                case CreateLinksItemEventType.MouseOver: return this.Config.GuiEditShowLinkMouseWholeTask;
                case CreateLinksItemEventType.ItemSelected: return this.Config.GuiEditShowLinkSelectedWholeTask;
            }
            return false;
        }
        /// <summary>
        /// Uživatel dal doubleclick na plochu grafu
        /// </summary>
        /// <param name="args"></param>
        protected void GraphDoubleClick(ItemActionArgs args)
        {
            switch (this.GraphProperties.DoubleClickOnGraph)
            {
                case GuiDoubleClickAction.OpenForm:
                    Row row = args.Graph?.UserData as Row;
                    if (row != null)
                        this.RunOpenRecordForm(row.RecordGId);
                    break;
                case GuiDoubleClickAction.TimeZoom:
                    TimeRange time = args.Graph.AllGraphItems.Select(i => i.Time).TimeUnion();
                    if (time != null && time.IsFilled && time.IsReal)
                    {
                        time = time.ZoomToRatio(time.Center.Value, 1.2m);
                        this.SynchronizedTime = time;
                    }
                    break;
            }
        }
        /// <summary>
        /// Uživatel dal doubleclick na grafický prvek
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemDoubleClick(ItemActionArgs args)
        {
            if (args.ModifierKeys == Keys.Control || this.GraphProperties.DoubleClickOnGraphItem == GuiDoubleClickAction.OpenForm)
            {   // Akce typu Ctrl+DoubleClick na grafickém prvku si žádá otevření formuláře:
                DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
                if (graphItem != null)
                    this.RunOpenRecordForm(graphItem.RecordGId);
            }
            else if (this.GraphProperties.DoubleClickOnGraphItem == GuiDoubleClickAction.TimeZoom)
            {
                TimeRange time = args.Group.Time;
                if (time != null && time.IsFilled && time.IsReal)
                {
                    time = time.ZoomToRatio(time.Center.Value, 1.2m);
                    this.SynchronizedTime = time;
                }
                /*
                DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
                if (graphItem != null)
                {
                    TimeRange time = graphItem.Time;
                    if (graphItem.GroupGId != null)
                    {
                        DataGraphItem[] graphItems;
                        if (this.TimeGraphGroupDict.TryGetValue(graphItem.GroupGId, out graphItems))
                        {
                            time = graphItems.Select(g => g.Time).TimeUnion();
                        }
                    }
                    if (time != null && time.IsFilled && time.IsReal)
                    {
                        time = time.ZoomToRatio(time.Center.Value, 1.2m);
                        this.SynchronizedTime = time;
                    }
                }
                */
            }
        }
        /// <summary>
        /// Metoda sestaví ID pro daný řádek
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private GuiGridRowId GetGridRowId(Row row)
        {
            GuiGridRowId gridRowId = new GuiGridRowId();
            gridRowId.TableName = this.TableName;                   // Konstantní jméno FullName this tabulky (třída GuiGrid)
            if (row != null)
            {
                gridRowId.RowId = row.RecordGId;
            }
            return gridRowId;
        }

        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v prvku grafu, který se pokusí najít v daném interaktivním objektu.
        /// </summary>
        /// <param name="item">Interaktivní prvek</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(IInteractiveItem item)
        {
            if (item == null) return null;
            GTimeGraphItem timeGraphItem = InteractiveObject.SearchForItem(item, true, typeof(GTimeGraphItem)) as GTimeGraphItem;
            if (timeGraphItem == null) return null;
            GTable gTable = timeGraphItem.SearchForParent(typeof(GTable)) as GTable;     // Najdu vizuální tabulku, v níž daný prvek grafu bydlí
            if (gTable == null) return null;
            MainDataTable mainDataTable = gTable.DataTable.UserData as MainDataTable;    // Ve vizuální tabulce najdu její datový základ, a jeho UserData by měla být instance MainDataTable
            if (mainDataTable == null) return null;

            return GetGridItemId(timeGraphItem, mainDataTable.TableName);
        }
        /// <summary>
        /// Metoda vrátí <see cref="GuiGridItemId"/> pro daný prvek grafu.
        /// Pokud bylo kliknuto na grupu do prostoru mezi prvky, tedy na <see cref="GTimeGraphItem"/> typu Group, pak vrácený <see cref="GuiGridItemId"/> bude mít nevyplněné <see cref="GuiGridItemId.ItemId"/>.
        /// </summary>
        /// <param name="timeGraphItem"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(GTimeGraphItem timeGraphItem, string tableName)
        {
            if (timeGraphItem == null || String.IsNullOrEmpty(tableName)) return null;
            var items = timeGraphItem.GetDataItems(false);
            DataGraphItem graphItem = items[0] as DataGraphItem;     // Nikdy se nevrací 0 prvků, protože prvek grafu obsahuje vždy nejméně jeden datový prvek
            if (graphItem == null) return null;

            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = tableName;
            gridItemId.RowId = graphItem.RowGId;
            if (items.Length == 1)                                   // Pokud prvek timeGraphItem reprezentuje grupu (tedy aktivní bod je mezi konkrétními prvky grafu), pak Length je větší než 1, protože metoda timeGraphItem.GetDataItems(false); vrátila všechny prvky grupy.
                gridItemId.ItemId = graphItem.ItemGId;
            gridItemId.GroupId = graphItem.GroupGId;
            gridItemId.DataId = graphItem.DataGId;

            return gridItemId;
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném prvku grafu.
        /// </summary>
        /// <param name="graphItem">Prvek grafu</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(DataGraphItem graphItem)
        {
            return GetGridItemId(graphItem, this.TableName);
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném prvku grafu.
        /// </summary>
        /// <param name="graphItem">Prvek grafu</param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static GuiGridItemId GetGridItemId(DataGraphItem graphItem, string tableName)
        {
            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = tableName;
            if (graphItem != null)
            {   // Pokud mám prvek, pak do resultu vložím jeho GId (převedené na GuiId):
                gridItemId.RowId = graphItem.RowGId;                 // Parentem je GID řádku
                gridItemId.ItemId = graphItem.ItemGId;
                gridItemId.GroupId = graphItem.GroupGId;
                gridItemId.DataId = graphItem.DataGId;
            }
            return gridItemId;
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném interaktivním argumentu.
        /// </summary>
        /// <param name="args">Interaktivní argument</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(ItemActionArgs args)
        {
            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = this.TableName;                   // Konstantní jméno FullName this tabulky (třída GuiGrid)
            gridItemId.RowId = this.GetGraphRowGid(args.Graph);      // Z grafu najdu jeho řádek a jeho GId řádku, ten se (implicitně) převede na GuiId
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
            if (graphItem != null)
            {   // Pokud mám prvek, pak do resultu vložím jeho GId (převedené na GuiId):
                gridItemId.ItemId = graphItem.ItemGId;
                gridItemId.GroupId = graphItem.GroupGId;
                gridItemId.DataId = graphItem.DataGId;
            }
            return gridItemId;
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném interaktivním argumentu.
        /// </summary>
        /// <param name="args">Interaktivní argument</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(ItemInteractiveArgs args)
        {
            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = this.TableName;                   // Konstantní jméno FullName this tabulky (třída GuiGrid)
            gridItemId.RowId = this.GetGraphRowGid(args.Graph);      // Z grafu najdu jeho řádek a jeho GId řádku, ten se (implicitně) převede na GuiId
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
            if (graphItem != null)
            {   // Pokud mám prvek, pak do resultu vložím jeho GId (převedené na GuiId):
                gridItemId.ItemId = graphItem.ItemGId;
                gridItemId.GroupId = graphItem.GroupGId;
                gridItemId.DataId = graphItem.DataGId;
            }
            return gridItemId;
        }
        /// <summary>
        /// Metoda najde a vrátí grafický prvek zdejší třídy <see cref="DataGraphItem"/> pro daný interaktivní prvek, 
        /// uvedený v interaktivním argumentu <see cref="ItemArgs"/>.
        /// Najde tedy datový prvek grafu, odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected DataGraphItem GetActiveGraphItem(ItemArgs args)
        {
            if (args.Item != null && args.Item is DataGraphItem) return args.Item as DataGraphItem;
            if (args.Group != null && args.Group.Items != null && args.Group.Items.Length > 0 && args.Group.Items[0] is DataGraphItem) return args.Group.Items[0] as DataGraphItem;
            return null;
            //    int itemId = (args.Item != null ? args.Item.ItemId :
            //             (args.GroupedItems != null ?? args.GroupedItems[0]).ItemId;
            //if (itemId <= 0) return null;
            //return this.GetGraphItem(itemId);
        }
        /// <summary>
        /// Scheduler zde pomáhá určovat, zda jak a kam lze nebo nelze přemisťovat prvek grafu.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragDropAction(ItemDragDropArgs args)
        {
            switch (args.DragAction)
            {
                case DragActionType.DragThisStart:
                    // Tady toho není moc k řešení...
                    break;
                case DragActionType.DragThisMove:
                    // Řešíme zarovnávání časů (magnety) k sousedním prvkům atd, abych prvek přetahoval přiměřeně:
                    this.ItemDragDropMove(args);
                    break;
                case DragActionType.DragThisDrop:
                    // Voláme metodu AppHost => aplikační funkce pro přepočet grafu po přemístění prvku:
                    this.ItemDragDropDrop(args);
                    break;
                case DragActionType.DragThisEnd:
                    // Refresh na graf:
                    args.ParentGraph.Refresh();
                    break;
            }
        }
        /// <summary>
        /// Scheduler zde pomáhá určovat, zda jak a kam lze nebo nelze měnit velikost prvku grafu.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemResizeAction(ItemResizeArgs args)
        {
            switch (args.ResizeAction)
            {
                case DragActionType.DragThisStart:
                    // Tady toho není moc k řešení...
                    break;
                case DragActionType.DragThisMove:
                    // Řešíme zarovnávání časů (magnety) k sousedním prvkům atd, abych prvek upravoval přiměřeně:
                    this.ItemResizeMove(args);
                    break;
                case DragActionType.DragThisDrop:
                    // Voláme metodu AppHost => aplikační funkce pro přepočet grafu po změně velikosti prvku:
                    this.ItemResizeDrop(args);
                    break;
                case DragActionType.DragThisEnd:
                    // Refresh na graf:
                    args.Graph.Refresh();
                    break;
            }
        }
        void ITimeGraphDataSource.CreateText(CreateTextArgs args) { this.GraphItemPrepareText(args); }
        void ITimeGraphDataSource.CreateToolTip(CreateToolTipArgs args) { this.GraphItemPrepareToolTip(args); }
        void ITimeGraphDataSource.CreateLinks(CreateLinksArgs args) { this.GraphItemCreateLinks(args); }
        void ITimeGraphDataSource.GraphRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForGraph(args); }
        void ITimeGraphDataSource.GraphDoubleClick(ItemActionArgs args) { this.GraphDoubleClick(args); }
        void ITimeGraphDataSource.GraphLongClick(ItemActionArgs args) { }
        void ITimeGraphDataSource.ItemRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForItem(args); }
        void ITimeGraphDataSource.ItemDoubleClick(ItemActionArgs args) { this.GraphItemDoubleClick(args); }
        void ITimeGraphDataSource.ItemLongClick(ItemActionArgs args) { }
        void ITimeGraphDataSource.ItemChange(ItemChangeArgs args) { }
        void ITimeGraphDataSource.ItemDragDropAction(ItemDragDropArgs args) { this.ItemDragDropAction(args); }
        void ITimeGraphDataSource.ItemResizeAction(ItemResizeArgs args) { this.ItemResizeAction(args); }
        #endregion
        #region Implementace IDataGraphTableInternal: Přístup k vnitřním datům tabulky
        int IMainDataTableInternal.GetId(GId gId) { return this.GetId(gId); }
        GId IMainDataTableInternal.GetGId(int id) { return this.GetGId(id); }
        DataGraphItem IMainDataTableInternal.GetGraphItem(int id) { return this.GetGraphItem(id); }
        DataGraphItem IMainDataTableInternal.GetGraphItem(GId gId) { return this.GetGraphItem(gId); }
        #endregion
    }
    #region interface IMainDataTableInternal
    /// <summary>
    /// Rozhraní pro přístup k interním metodám třídy MainDataTable
    /// </summary>
    public interface IMainDataTableInternal
    {
        /// <summary>
        /// Metoda vrátí Int32 ID pro daný <see cref="GId"/>.
        /// Pro opakovaný požadavek na tentýž <see cref="GId"/> vrací shodnou hodnotu ID.
        /// Pro první požadavek na určitý <see cref="GId"/> vytvoří nový ID.
        /// Reverzní metoda je <see cref="GetGId(int)"/>.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        int GetId(GId gId);
        /// <summary>
        /// Pro daný ID vrátí <see cref="GId"/>, ale pouze pokud byl přidělen v metodě <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        GId GetGId(int id);
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        DataGraphItem GetGraphItem(int id);
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho GId
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        DataGraphItem GetGraphItem(GId gId);
    }
    #endregion
    #region class DataGraphItem : jedna položka grafu, implementuje ITimeGraphItem, je vykreslována v tabulce
    /// <summary>
    /// DataGraphItem : jedna položka grafu, implementuje ITimeGraphItem, je vykreslována v tabulce
    /// </summary>
    public class DataGraphItem : ITimeGraphItem
    {
        #region Konstrukce, načítání dat, proměné
        /// <summary>
        /// Metoda vytvoří a vrátí instanci položky grafu z dodaného řádku s daty.
        /// Neexistuje jiná cesta jak vytvořit <see cref="DataGraphItem"/>, než na základě dat v <see cref="GuiGraphItem"/>.
        /// </summary>
        /// <param name="graphTable"></param>
        /// <param name="guiGraphItem"></param>
        /// <returns></returns>
        public static DataGraphItem CreateFrom(MainDataTable graphTable, GuiGraphItem guiGraphItem)
        {
            if (guiGraphItem == null || guiGraphItem.Time == null) return null;     // Neplatný vstup vygeneruje null.

            IMainDataTableInternal iGraphTable = graphTable as IMainDataTableInternal;

            DataGraphItem item = new DataGraphItem(graphTable, guiGraphItem);
            item._ItemGId = guiGraphItem.ItemId;           // Mezi typy GuiId (=Green) a GId (GraphLibrary) existuje implicitní konverze.
            item._RowGId = guiGraphItem.RowId;             //  Takže do zdejších properties se vytvoří new instance GUid, obsahující stejná data jako vstupní GuiId.
            item._GroupGId = guiGraphItem.GroupId;         //  Další důsledek je ten, že zdejší data lze změnit = přemístit na jiný řádek, například.
            item._DataGId = guiGraphItem.DataId;
            item._Time = guiGraphItem.Time;                // Existuje implicitní konverze mezi typy TimeRange a GuiTimeRange.
            item._RatioStyle = GetRatioStyle(guiGraphItem.RatioStyle);
            item._BehaviorMode = guiGraphItem.BehaviorMode;

            // ID pro grafickou vrstvu: vygenerujeme Int32 klíč pro daný GId, za pomoci indexu uloženého v hlavní tabulce (iGraphTable):
            item._ItemId = iGraphTable.GetId(item.ItemGId);
            item._GroupId = iGraphTable.GetId(item.GroupGId);

            // Ostatní property jsou načítané dynamicky přímo z item._GuiGraphItem.

            // Resetuji SkinIndex na defaultní skin:
            item.SkinCurrentIndex = 0;

            return item;
        }
        /// <summary>
        /// Prvek si aktualizuje data z dodaného prvku.
        /// </summary>
        /// <param name="sourceItem"></param>
        public bool UpdateFrom(GuiGraphItem sourceItem)
        {
            // Tato hodnota je ukládaná do prvku přímo, protože se provádí její editace:
            if (sourceItem.Time != null) this._Time = sourceItem.Time;

            // Aktualizovat jednotlivé statické hodnoty:
            GuiGraphItem targetItem = this._GuiGraphItem;

            if (sourceItem.Layer != 0) targetItem.Layer = sourceItem.Layer;
            if (sourceItem.Level != 0) targetItem.Level = sourceItem.Level;
            if (sourceItem.Order != 0) targetItem.Order = sourceItem.Order;
            if (sourceItem.Height.HasValue) targetItem.Height = sourceItem.Height;
            if (sourceItem.Text != null) targetItem.Text = sourceItem.Text;
            if (sourceItem.ToolTip != null) targetItem.ToolTip = sourceItem.ToolTip;

            // Aktualizovat hodnoty Skinů, ale nikoli aktuální platný SkinIndex (to zajišťuje toolbar nebo jiná technika):
            if (sourceItem.SkinDefault != null) UpdateSkinFrom(targetItem.SkinDefault, sourceItem.SkinDefault);     // Defaultní skin
            if (sourceItem.SkinDict != null) UpdateSkinsFrom(targetItem, sourceItem);                               // Explicitní skiny

            // Tyto property budeme aktualizovat do zdejšího datového prvku GuiGraphItem _GuiGraphItem, 
            //   i do this.*, protože tyto hodnoty zde máme jako pracovní (=editovatelné):
            if (sourceItem.Time != null)
            {
                targetItem.Time = sourceItem.Time;
                this.Time = sourceItem.Time;
            }
            if (sourceItem.RatioStyle.HasValue)
            {
                targetItem.RatioStyle = sourceItem.RatioStyle;
                this._RatioStyle = GetRatioStyle(sourceItem.RatioStyle);
            }
            if (sourceItem.BehaviorMode != GraphItemBehaviorMode.None)
            {
                targetItem.BehaviorMode = sourceItem.BehaviorMode;
                this._BehaviorMode = sourceItem.BehaviorMode;
            }

            if (this._GControl != null) this._GControl.InvalidateBounds();

            // Zajistit invalidaci grafu:
            return true;
        }
        /// <summary>
        /// Zajistí aktualizaci skinů v this instanci daty z dodané instance, kde <see cref="GuiGraphItem.SkinDict"/> není null.
        /// </summary>
        /// <param name="targetItem"></param>
        /// <param name="sourceItem"></param>
        protected static void UpdateSkinsFrom(GuiGraphItem targetItem, GuiGraphItem sourceItem)
        {
            foreach (var pair in sourceItem.SkinDict)
            {   // Zajistíme, že v targetItem budou existovat klíče dodané v sourceItem, a budou mít zadané hodnoty.
                // Na druhou stranu klíče, které v sourceItem nejsou, nebudeme z targetItem odebírat.
                GuiGraphSkin sourceSkin = pair.Value;
                GuiGraphSkin targetSkin = targetItem.SkinDict.GetAdd(pair.Key, key => new GuiGraphSkin());
                UpdateSkinFrom(targetSkin, sourceSkin);
            }
        }
        /// <summary>
        /// Zajistí aktualizaci daného skinu
        /// </summary>
        /// <param name="targetSkin"></param>
        /// <param name="sourceSkin"></param>
        protected static void UpdateSkinFrom(GuiGraphSkin targetSkin, GuiGraphSkin sourceSkin)
        {
            if (!sourceSkin.DoRefreshSkin) return;         // Refresh je potlačen (to když volající strana ví, že nenaplnila data a nechce je měnit)
            targetSkin.IsVisible = sourceSkin.IsVisible;
            targetSkin.BackColor = GetUpdatedColor(sourceSkin.BackColor);
            targetSkin.HatchColor = GetUpdatedColor(sourceSkin.HatchColor);
            targetSkin.BackStyle = sourceSkin.BackStyle;
            targetSkin.LineColor = GetUpdatedColor(sourceSkin.LineColor);
            targetSkin.RatioBeginBackColor = GetUpdatedColor(sourceSkin.RatioBeginBackColor);
            targetSkin.RatioEndBackColor = GetUpdatedColor(sourceSkin.RatioEndBackColor);
            targetSkin.RatioLineColor = GetUpdatedColor(sourceSkin.RatioLineColor);
            targetSkin.RatioLineWidth = sourceSkin.RatioLineWidth;
            targetSkin.ImageBegin = GetUpdatedImage(sourceSkin.ImageBegin);
            targetSkin.ImageEnd = GetUpdatedImage(sourceSkin.ImageEnd);
        }
        /// <summary>
        /// Metoda vrátí danou barvu.
        /// Pokud je ale daná barva Empty, pak vrací null.
        /// </summary>
        /// <param name="updatedColor"></param>
        /// <returns></returns>
        protected static Color? GetUpdatedColor(Color? updatedColor)
        {
            if (!updatedColor.HasValue) return null;
            if (updatedColor.Value == Color.Empty) return null;
            return updatedColor;
        }
        /// <summary>
        /// Metoda vrátí daný styl.
        /// Pokud je ale daný styl Sphere, pak vrací null.
        /// </summary>
        /// <param name="updatedStyle"></param>
        /// <returns></returns>
        protected static System.Drawing.Drawing2D.HatchStyle? GetUpdatedStyle(System.Drawing.Drawing2D.HatchStyle? updatedStyle)
        {
            if (!updatedStyle.HasValue) return null;
            if (updatedStyle.Value == System.Drawing.Drawing2D.HatchStyle.Sphere) return null;
            return updatedStyle;
        }
        /// <summary>
        /// Metoda vrátí daný <see cref="GuiImage"/>. Pokud je not null, ale je Empty, vrací null.
        /// </summary>
        /// <param name="updatedImage"></param>
        /// <returns></returns>
        protected static GuiImage GetUpdatedImage(GuiImage updatedImage)
        {
            if (updatedImage == null) return null;
            if (updatedImage.IsEmpty) return null;
            return updatedImage;
        }
        /// <summary>
        /// Vrací hodnotu typu <see cref="TimeGraphElementRatioStyle"/> z hodnoty <see cref="Noris.LCS.Base.WorkScheduler.GuiRatioStyle"/>
        /// </summary>
        /// <param name="ratioStyle"></param>
        /// <returns></returns>
        protected static TimeGraphElementRatioStyle GetRatioStyle(GuiRatioStyle? ratioStyle)
        {
            if (ratioStyle.HasValue)
            {
                switch (ratioStyle.Value)
                {
                    case GuiRatioStyle.VerticalFill: return TimeGraphElementRatioStyle.VerticalFill;
                    case GuiRatioStyle.HorizontalFill : return TimeGraphElementRatioStyle.HorizontalFill;
                    case GuiRatioStyle.HorizontalInner: return TimeGraphElementRatioStyle.HorizontalInner;
                }
            }
            return TimeGraphElementRatioStyle.VerticalFill;
        }
        /// <summary>
        /// privátní konstruktor. Instanci lze založit pomocí metody <see cref="CreateFrom(MainDataTable, GuiGraphItem)"/>.
        /// </summary>
        private DataGraphItem(MainDataTable graphTable, GuiGraphItem guiGraphItem)
        {
            this._GraphTable = graphTable;
            this._GuiGraphItem = guiGraphItem;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Item: " + this._ItemGId.ToString() + "; Time: " + this._Time.ToString() + "; Height: " + this._GuiGraphItem.Height.ToString();
        }
        /// <summary>
        /// Vlastník = datová základna, instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        protected MainData MainData { get { return this.GraphTable.MainData; } }
        /// <summary>
        /// Vlastník prvku = celá tabulka
        /// </summary>
        private MainDataTable _GraphTable;
        /// <summary>
        /// Datový podklad tohoto prvku = data načtená ze systému a předaná v instanci <see cref="GuiGraphItem"/>
        /// </summary>
        private GuiGraphItem _GuiGraphItem;
        /// <summary>
        /// Vlastník prvku = graf
        /// </summary>
        private ITimeInteractiveGraph _OwnerGraph;
        /// <summary>
        /// Řádek, kde je prvek vykreslen
        /// </summary>
        private GId _RowGId;
        /// <summary>
        /// GId prvku, pochází z datového zdroje, obsahuje číslo třídy a záznamu
        /// </summary>
        private GId _ItemGId;
        /// <summary>
        /// GId skupiny, pochází z datového zdroje, obsahuje číslo třídy a záznamu
        /// </summary>
        private GId _GroupGId;
        /// <summary>
        /// GId datového záznamu, pochází z datového zdroje, obsahuje číslo třídy a záznamu
        /// </summary>
        private GId _DataGId;
        /// <summary>
        /// ID prvku, používá se v grafickém controlu
        /// </summary>
        private int _ItemId;
        /// <summary>
        /// ID skupiny, používá se v grafickém controlu
        /// </summary>
        private int _GroupId;
        /// <summary>
        /// Čas prvku
        /// </summary>
        private TimeRange _Time;
        /// <summary>
        /// Styl kreslení Ratio
        /// </summary>
        private TimeGraphElementRatioStyle _RatioStyle;
        /// <summary>
        /// Režim chování
        /// </summary>
        private GraphItemBehaviorMode _BehaviorMode;
        /// <summary>
        /// Vizuální control
        /// </summary>
        private GTimeGraphItem _GControl;
        /// <summary>
        /// Metoda se pokusí zajistit, aby existoval vizuální prvek <see cref="_GControl"/> (pokud dosud neexistuje), a aby měl napočtené korektní hodnoty.
        /// </summary>
        private void _CheckGControl()
        {
            if (this._GControl == null)
            {   // Prvek grafu ještě nemá vytvořen GControl = jde o řádek, který ještě nebyl kreslen.
                // Požádáme tedy jeho graf, aby si prověřil platnost svých dat:
                GTimeGraph graph = this._OwnerGraph as GTimeGraph;
                if (graph != null)
                    graph.CheckValid();
            }
        }
        #endregion
        #region Aplikační data - identifikátory atd
        /// <summary>
        /// Vlastník prvku grafu = tabulka s komplexními daty
        /// </summary>
        public MainDataTable GraphTable { get { return this._GraphTable; } }
        /// <summary>
        /// Datový podklad tohoto prvku = data načtená ze systému a předaná v instanci <see cref="GuiGraphItem"/>
        /// </summary>
        public GuiGraphItem GuiGraphItem { get { return this._GuiGraphItem; } }
        /// <summary>
        /// Veřejný identifikátor GRAFICKÉHO PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o záznam třídy Stav kapacit, nebo Pracovní jednotka.
        /// </summary>
        public GId ItemGId { get { return this._ItemGId; } }
        /// <summary>
        /// Veřejný identifikátor řádku, kam prvek patří (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o Kapacitní plánovací jednotku.
        /// </summary>
        public GId RowGId { get { return this._RowGId; } set { this._RowGId = value; } }
        /// <summary>
        /// Veřejný identifikátor SKUPINY PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o záznam třídy Paralelní průchod.
        /// </summary>
        public GId GroupGId { get { return this._GroupGId; } set { this._GroupGId = value; } }
        /// <summary>
        /// Veřejný identifikátor DATOVÉHO OBJEKTU: obsahuje číslo třídy a číslo záznamu.
        /// Může jít o Operaci výrobního příkazu.
        /// </summary>
        public GId DataGId { get { return this._DataGId; } set { this._DataGId = value; } }
        /// <summary>
        /// Veřejný identifikátor ZÁZNAMU K OTEVŘENÍ: obsahuje číslo třídy a číslo záznamu.
        /// Jako <see cref="RecordGId"/> se vrací nejvhodnější identifikátor, který má být otevřen po provedení Ctrl + DoubleClick na tomto prvku.
        /// Pokud je zadán <see cref="DataGId"/>, vrací se ten. Jako další se může vrátit <see cref="ItemGId"/> anebo <see cref="GroupGId"/>; v tomto pořadí.
        /// Ale nevrací se <see cref="RowGId"/> (to je řádek, nikoli prvek).
        /// Může být null, pokud nic z uvedeného není zadané.
        /// </summary>
        public GId RecordGId
        {
            get
            {
                if (this._DataGId != null) return this._DataGId;
                if (this._ItemGId != null) return this._ItemGId;
                if (this._GroupGId != null) return this._GroupGId;
                return null;
            }
        }
        /// <summary>
        /// Časový interval tohoto prvku
        /// </summary>
        public TimeRange Time { get { return this._Time; } set { this._Time = value; } }
        /// <summary>
        /// Orientace hodnoty Ratio: Vertical = odspodu nahoru, Horizontal = Zleva doprava
        /// </summary>
        public TimeGraphElementRatioStyle RatioStyle { get { return this._RatioStyle; } set { this._RatioStyle = value; } }
        /// <summary>
        /// Režim chování položky grafu (editovatelnost, texty, atd).
        /// </summary>
        public GraphItemBehaviorMode BehaviorMode { get { return this._BehaviorMode; } set { this._BehaviorMode = value; } }
        #endregion
        #region Variabilní čtení identifikátoru
        /// <summary>
        /// Vrátí identifikátor daného typu
        /// </summary>
        /// <param name="idType"></param>
        /// <returns></returns>
        public GId GetGId(IdType idType)
        {
            switch (idType)
            {
                case IdType.Item: return this.ItemGId;
                case IdType.Group: return this.GroupGId;
                case IdType.Data: return this.DataGId;
                case IdType.Row: return this.RowGId;
            }
            return null;
        }
        /// <summary>
        /// Druh identifikátoru
        /// </summary>
        public enum IdType
        {
            /// <summary>
            /// Žádný
            /// </summary>
            None = 0,
            /// <summary>
            /// ItemId
            /// </summary>
            Item,
            /// <summary>
            /// GroupId
            /// </summary>
            Group,
            /// <summary>
            /// DataId
            /// </summary>
            Data,
            /// <summary>
            /// RowId
            /// </summary>
            Row
        }
        #endregion
        #region SkinSet
        /// <summary>
        /// Index aktuálního Skinu. Výchozí hodnota = 0, ta odkazuje na defaultní skin.
        /// Lze setovat libovolnou numerickou hodnotu, tím se aktivuje daný skin. Skin pro novou hodnotu bude automaticky vytvořen jako prázdný.
        /// Čtení konkrétní hodnoty se provádí z explicitně deklarovaného skinu, a pokud v konkrétní property je null, pak se čte z defaultního skinu.
        /// Zápis hodnoty se provádí výhradně do aktuálního skinu (explicitní / defaultní).
        /// Je tak zajištěno, že bude existovat defaultní sada grafických hodnot (=defaultní skin) 
        /// plus libovolně široká řada explicitních skinů, které mohou přepisovat (tj. definovat vlastní) hodnotu jen u některé property.
        /// Aplikace deklaruje nejprve kompletní defaultní skin, a poté deklaruje potřebnou sadu skinů.
        /// <para/>
        /// Konkrétní skiny si aktivuje uživatel v GUI, typicky nějakým tlačítkem v toolbaru, které má definovanou akci <see cref="TargetActionType.ActivateGraphSkin"/>,
        /// s parametrem odpovídajícím číslu skinu.
        /// <para/>
        /// Skin ovlivňuje hodnoty v těchto properties:
        /// <see cref="GuiGraphItem.BackColor"/>, <see cref="GuiGraphItem.HatchColor"/>, <see cref="GuiGraphItem.LineColor"/>, 
        /// <see cref="GuiGraphItem.BackStyle"/>, <see cref="GuiGraphItem.RatioBeginBackColor"/>, <see cref="GuiGraphItem.RatioEndBackColor"/>, 
        /// <see cref="GuiGraphItem.RatioLineColor"/>, <see cref="GuiGraphItem.RatioLineWidth"/>, 
        /// <see cref="GuiGraphItem.ImageBegin"/>, <see cref="GuiGraphItem.ImageEnd"/>.
        /// </summary>
        public int SkinCurrentIndex
        {
            get { return this.GuiGraphItem.SkinCurrentIndex; }
            set { this.GuiGraphItem.SkinCurrentIndex = value; }
        }
        /// <summary>
        /// Prvek je viditelný?
        /// Hodnota je mj. setována na true/false v procesu klonování řádků a grafu, při hledání párových prvků grafu 
        /// v metodě <see cref="MainDataTable.SynchronizeChildGraphItems(Dictionary{GId, TimeRange}, MainDataTable.SearchChildInfo, TimeRange, Row, Row)"/>.
        /// Hodnota ale pochází i z <see cref="GuiGraphItem.IsVisible"/>
        /// </summary>
        public bool IsVisible
        {
            get
            {   // Výsledek IsVisible = (this._IsVisible && (this._GuiGraphItem.IsVisible ?? true))
                bool isVisible = this._IsVisible;
                if (isVisible)
                {
                    bool? guiVisible = this._GuiGraphItem.IsVisible;
                    if (guiVisible.HasValue)
                        isVisible = guiVisible.Value;
                }
                return isVisible;
            }
            set { this._IsVisible = value; }
        }
        private bool _IsVisible = true;
        #endregion
        #region Podpora pro kreslení a interaktivitu
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/> : <see cref="GTimeGraphItem.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag and Drop)</param>
        protected void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            this._GControl.DrawItem(e, boundsAbsolute, drawMode);
        }
        /// <summary>
        /// Vrátí efekt pro vykreslení prvku, pokud je Editovatelný
        /// </summary>
        /// <returns></returns>
        protected TimeGraphElementBackEffectStyle GetBackEffectEditable()
        {
            return this.GetBackEffect(this._GuiGraphItem.BackEffectEditable, this.GraphTable.GraphProperties.BackEffectEditable);
        }
        /// <summary>
        /// Vrátí efekt pro vykreslení prvku, pokud je Needitovatelný
        /// </summary>
        /// <returns></returns>
        protected TimeGraphElementBackEffectStyle GetBackEffectNonEditable()
        {
            return this.GetBackEffect(this._GuiGraphItem.BackEffectNonEditable, this.GraphTable.GraphProperties.BackEffectNonEditable);
        }
        /// <summary>
        /// Vrací efekt pro kreslení pozadí prvku grafu
        /// </summary>
        /// <param name="backEffectItem"></param>
        /// <param name="backEffectGraph"></param>
        /// <returns></returns>
        protected TimeGraphElementBackEffectStyle GetBackEffect(GuiGraphItemBackEffectStyle? backEffectItem, GuiGraphItemBackEffectStyle? backEffectGraph)
        {
            GuiGraphItemBackEffectStyle backEffect = (backEffectItem.HasValue ? backEffectItem.Value : (backEffectGraph.HasValue ? backEffectGraph.Value : GuiGraphItemBackEffectStyle.Default));
            switch (backEffect)
            {
                case GuiGraphItemBackEffectStyle.Flat: return TimeGraphElementBackEffectStyle.Flat;
                case GuiGraphItemBackEffectStyle.Pipe: return TimeGraphElementBackEffectStyle.Pipe;
                case GuiGraphItemBackEffectStyle.Simple: return TimeGraphElementBackEffectStyle.Simple;

            }
            return TimeGraphElementBackEffectStyle.Default;
        }
        #endregion
        #region ICloneable members
        object ICloneable.Clone()
        {
            DataGraphItem clone = CreateFrom(this._GraphTable, this._GuiGraphItem);
            return clone;
        }
        #endregion
        #region Explicitní implementace rozhraní ITimeGraphItem
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        bool ITimeGraphItem.IsVisible { get { return this.IsVisible; } set { this.IsVisible = value; } }
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.GroupId { get { return this._GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this._Time; } set { this._Time = value; } }
        int ITimeGraphItem.Layer { get { return this._GuiGraphItem.Layer; } }
        int ITimeGraphItem.Level { get { return this._GuiGraphItem.Level; } }
        int ITimeGraphItem.Order { get { return this._GuiGraphItem.Order; } }
        float? ITimeGraphItem.Height { get { return this._GuiGraphItem.Height; } }
        string ITimeGraphItem.Text { get { return this._GuiGraphItem.Text; } }
        string ITimeGraphItem.ToolTip { get { return this._GuiGraphItem.ToolTip; } }
        float? ITimeGraphItem.RatioBegin { get { return this._GuiGraphItem.RatioBegin; } }
        float? ITimeGraphItem.RatioEnd { get { return this._GuiGraphItem.RatioEnd; } }
        TimeGraphElementRatioStyle ITimeGraphItem.RatioStyle { get { return this.RatioStyle; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this.BehaviorMode; } }
        TimeGraphElementBackEffectStyle ITimeGraphItem.BackEffectEditable { get { return this.GetBackEffectEditable(); } }
        TimeGraphElementBackEffectStyle ITimeGraphItem.BackEffectNonEditable { get { return this.GetBackEffectNonEditable(); } }
        GTimeGraphItem ITimeGraphItem.GControl { get { this._CheckGControl(); return this._GControl; } set { this._GControl = value; } }
        // Následující properties se načítají i ze Skinu:
        Color? ITimeGraphItem.BackColor { get { return this._GuiGraphItem.BackColor; } }
        Color? ITimeGraphItem.HatchColor { get { return this._GuiGraphItem.HatchColor; } }
        Color? ITimeGraphItem.LineColor { get { return this._GuiGraphItem.LineColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._GuiGraphItem.BackStyle; } }
        Color? ITimeGraphItem.RatioBeginBackColor { get { return this._GuiGraphItem.RatioBeginBackColor; } }
        Color? ITimeGraphItem.RatioEndBackColor { get { return this._GuiGraphItem.RatioEndBackColor; } }
        Color? ITimeGraphItem.RatioLineColor { get { return this._GuiGraphItem.RatioLineColor; } }
        int? ITimeGraphItem.RatioLineWidth { get { return this._GuiGraphItem.RatioLineWidth; } }
        Image ITimeGraphItem.ImageBegin { get { return App.Resources.GetImage(this._GuiGraphItem.ImageBegin); } }
        Image ITimeGraphItem.ImageEnd { get { return App.Resources.GetImage(this._GuiGraphItem.ImageEnd); } }
        // Kreslení:
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
    #endregion
    #region class DataGraphProperties : vlastnosti tabulky, popis chování atd - načteno z dodaných dat
    /// <summary>
    /// DataGraphProperties : vlastnosti tabulky, popis chování atd - načteno z dodaných dat <see cref="Noris.LCS.Base.WorkScheduler.GuiGraphProperties"/>.
    /// Jedná se pouze o adapter: do sebe uloží referenci na <see cref="Noris.LCS.Base.WorkScheduler.GuiGraphProperties"/>, 
    /// a následně vygeneruje instanci <see cref="Asol.Tools.WorkScheduler.Components.Graph.TimeGraphProperties"/>, do které opíše data dodaná ze vstupního objektu.
    /// </summary>
    public class DataGraphProperties
    {
        #region Konstrukce, načtení
        /// <summary>
        /// Vytvoří a vrátí instanci DataGraphProperties, vloží do ní dodaná data.
        /// </summary>
        /// <param name="dataGraphTable">Vlastník = tabulka</param>
        /// <param name="guiProperties">Definice globálních vlastností</param>
        /// <param name="guiGraphProperties">Definice vlastností grafu</param>
        /// <returns></returns>
        public static DataGraphProperties CreateFrom(MainDataTable dataGraphTable, GuiProperties guiProperties, GuiGraphProperties guiGraphProperties)
        {
            return new DataGraphProperties(dataGraphTable, guiProperties, guiGraphProperties);
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        /// <param name="dataGraphTable">Vlastník = tabulka</param>
        /// <param name="guiMainProperties">Definice globálních vlastností</param>
        /// <param name="guiGraphProperties">Definice vlastností grafu</param>
        private DataGraphProperties(MainDataTable dataGraphTable, GuiProperties guiMainProperties, GuiGraphProperties guiGraphProperties)
        {
            this.MainDataTable = dataGraphTable;
            this.GuiMainProperties = guiMainProperties;
            this.GuiGraphProperties = guiGraphProperties;
        }
        /// <summary>
        /// Vlastník = tabulka
        /// </summary>
        protected MainDataTable MainDataTable { get; private set; }
        /// <summary>
        /// Vlastnosti globální
        /// </summary>
        protected GuiProperties GuiMainProperties { get; private set; }
        /// <summary>
        /// Vlastnosti grafu načtené z deklarace v <see cref="GuiData"/>
        /// </summary>
        protected GuiGraphProperties GuiGraphProperties { get; private set; }
        #endregion
        #region Public data
        /// <summary>
        /// Režim zobrazování času na ose X.
        /// Přihlíží se k hodnotě <see cref="GraphPosition"/>:
        /// pokud je <see cref="DataGraphPositionType.OnBackgroundLogarithmic"/> nebo <see cref="DataGraphPositionType.OnBackgroundProportional"/>,
        /// pak se vrací odpovídající <see cref="TimeGraphTimeAxisMode"/>.
        /// Pro pozici <see cref="GraphPosition"/> == <see cref="DataGraphPositionType.InLastColumn"/> se vrací hodnota z <see cref="TimeAxisMode"/>.
        /// </summary>
        public TimeGraphTimeAxisMode TimeAxisMode
        {
            get
            {
                switch (this.GraphPosition)
                {
                    case DataGraphPositionType.OnBackgroundLogarithmic: return TimeGraphTimeAxisMode.LogarithmicScale;
                    case DataGraphPositionType.OnBackgroundProportional: return TimeGraphTimeAxisMode.ProportionalScale;
                }
                return this.GuiGraphProperties.TimeAxisMode;
            }
        }
        /// <summary>
        /// Pozice grafu v tabulce
        /// </summary>
        public DataGraphPositionType GraphPosition { get { return this.GuiGraphProperties.GraphPosition; } }
        /// <summary>
        /// Reakce na DoubleClick v prostoru Časového grafu
        /// </summary>
        public GuiDoubleClickAction DoubleClickOnGraph { get { return (this.GuiMainProperties != null ? this.GuiMainProperties.DoubleClickOnGraph : GuiDoubleClickAction.None); } }
        /// <summary>
        /// Reakce na DoubleClick v prostoru Prvku na Časovém grafu
        /// </summary>
        public GuiDoubleClickAction DoubleClickOnGraphItem { get { return (this.GuiMainProperties != null ? this.GuiMainProperties.DoubleClickOnGraphItem : GuiDoubleClickAction.None); } }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Editovatelný.
        /// Pokud není zadán, použije se Default.
        /// </summary>
        public GuiGraphItemBackEffectStyle? BackEffectEditable { get { return this.GuiGraphProperties?.BackEffectEditable; } }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Needitovatelný.
        /// Pokud není zadán, použije se Default.
        /// </summary>
        public GuiGraphItemBackEffectStyle? BackEffectNonEditable { get { return this.GuiGraphProperties?.BackEffectNonEditable; } }
        /// <summary>
        /// Nejmenší šířka prvku grafu v pixelech. 
        /// Pokud by byla vypočtena šířka menší, bude zvětšena na tuto hodnotu - aby byl prvek grafu viditelný.
        /// Výchozí hodnota = 0, neprovádí se zvětšení, malé prvky (krátký čas na širokém měřítku) nejsou vidět.
        /// </summary>
        public int GraphItemMinPixelWidth { get { return (this.GuiGraphProperties?.GraphItemMinPixelWidth ?? 0); } }
        /// <summary>
        /// Určuje výchozí režim zobrazení spojovacích čar mezi prvky.
        /// </summary>
        public GTimeGraphLinkMode DefaultLinksMode { get { return (this.GuiGraphProperties != null ? ConvertTo(this.GuiGraphProperties.LinkMode) : GTimeGraphLinkMode.Default); } }
        /// <summary>
        /// Metoda vrátí <see cref="GTimeGraphLinkMode"/> pro daný <see cref="GuiGraphLinkMode"/>
        /// </summary>
        /// <param name="linkMode"></param>
        /// <returns></returns>
        protected static GTimeGraphLinkMode ConvertTo(GuiGraphLinkMode linkMode)
        {
            GTimeGraphLinkMode mode = GTimeGraphLinkMode.None;
            if (linkMode.HasFlag(GuiGraphLinkMode.MouseOver)) mode |= GTimeGraphLinkMode.MouseOver;
            if (linkMode.HasFlag(GuiGraphLinkMode.Selected)) mode |= GTimeGraphLinkMode.Selected;
            if (linkMode.HasFlag(GuiGraphLinkMode.Allways)) mode |= GTimeGraphLinkMode.Allways;
            return mode;
        }
        #endregion
        #region Převod konfiguračních dat z úrovně GuiGraphProperties (GUI) do úrovně TimeGraphProperties (Components.Graph)
        /// <summary>
        /// Vytvoří a vrátí definici pro graf v úrovni Graph, třída <see cref="TimeGraphProperties"/>, 
        /// z dat načtených z Scheduleru (this, <see cref="Noris.LCS.Base.WorkScheduler.GuiGraphProperties"/>).
        /// Zde dochází k fyzickému přenosu hodnot.
        /// </summary>
        /// <param name="isGraphInColumn"></param>
        /// <param name="initialValue"></param>
        /// <param name="maximalValue"></param>
        /// <returns></returns>
        internal TimeGraphProperties CreateTimeGraphProperties(bool isGraphInColumn, TimeRange initialValue, TimeRange maximalValue)
        {
            GuiGraphProperties guiProperties = this.GuiGraphProperties;
            TimeGraphProperties timeProperties = new TimeGraphProperties();
            timeProperties.TimeAxisMode = (isGraphInColumn ? TimeGraphTimeAxisMode.Standard : this.TimeAxisMode);
            timeProperties.InitialResizeMode = guiProperties.AxisResizeMode;
            timeProperties.TimeAxisBackColor = guiProperties.TimeAxisBackColor;
            timeProperties.InitialValue = initialValue;
            timeProperties.MaximalValue = maximalValue;
            timeProperties.InteractiveChangeMode = guiProperties.InteractiveChangeMode;
            timeProperties.TimeAxisVisibleTickLevel = (isGraphInColumn ? AxisTickType.BigLabel : AxisTickType.None);
            timeProperties.OneLineHeight = guiProperties.GraphLineHeight;
            timeProperties.OneLinePartialHeight = guiProperties.GraphLinePartialHeight;
            timeProperties.UpperSpaceLogical = guiProperties.UpperSpaceLogical;
            timeProperties.BottomMarginPixel = guiProperties.BottomMarginPixel;
            timeProperties.GraphItemMinPixelWidth = guiProperties.GraphItemMinPixelWidth;
            timeProperties.TotalHeightRange = new Int32NRange(guiProperties.TableRowHeightMin, guiProperties.TableRowHeightMax);
            timeProperties.LogarithmicRatio = (guiProperties.LogarithmicRatio.HasValue ? guiProperties.LogarithmicRatio.Value : 0.60f);
            timeProperties.LogarithmicGraphDrawOuterShadow = (guiProperties.LogarithmicGraphDrawOuterShadow.HasValue ? guiProperties.LogarithmicGraphDrawOuterShadow.Value : 0.20f);
            timeProperties.Opacity = guiProperties.Opacity;
            timeProperties.LinkColorStandard = guiProperties.LinkColorStandard;
            timeProperties.LinkColorWarning = guiProperties.LinkColorWarning;
            timeProperties.LinkColorError = guiProperties.LinkColorError;
            timeProperties.TimeAxisSegments = ConvertSegments(guiProperties.TimeAxisSegmentList);
            return timeProperties;
        }
        /// <summary>
        /// Převede pole <see cref="GuiTimeAxisSegment"/> na pole <see cref="GBaseAxis{TTick, TSize, TValue}.Segment"/>
        /// </summary>
        /// <param name="guiAxisSegments"></param>
        /// <returns></returns>
        private GTimeAxis.Segment[] ConvertSegments(IEnumerable<GuiTimeAxisSegment> guiAxisSegments)
        {
            if (guiAxisSegments == null) return null;
            List<GTimeAxis.Segment> segments = new List<GBaseAxis<DateTime?, TimeSpan?, TimeRange>.Segment>();
            foreach (GuiTimeAxisSegment guiAxisSegment in guiAxisSegments)
            {
                if (guiAxisSegment != null)
                    segments.Add(new GTimeAxis.Segment()
                    {
                        ValueRange = guiAxisSegment.TimeRange,
                        BackColor = guiAxisSegment.BackColor,
                        HeightRange = guiAxisSegment.HeightRange,
                        SizeRange = guiAxisSegment.SizeRange,
                        ToolTip = guiAxisSegment.ToolTip
                    });
            }
            return segments.ToArray();
        }
        #endregion
    }
    #endregion
    #region class GridInteractionRunInfo : informace pro spuštění konkrétní interakce za dané situace
    /// <summary>
    /// GridInteractionRunInfo : informace pro spuštění konkrétní interakce za dané situace
    /// </summary>
    internal class GridInteractionRunInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="guiGridInteraction">Definice interakce</param>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        public GridInteractionRunInfo(GuiGridInteraction guiGridInteraction, SourceActionType currentSourceAction)
        {
            this.GuiGridInteraction = guiGridInteraction;
            this.CurrentSourceAction = currentSourceAction;
            this.RunParameters = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="guiGridInteraction">Definice interakce</param>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="runParameters">Parametry předané z tlačítka toolbaru</param>
        public GridInteractionRunInfo(GuiGridInteraction guiGridInteraction, SourceActionType currentSourceAction, IEnumerable<string> runParameters)
        {
            this.GuiGridInteraction = guiGridInteraction;
            this.CurrentSourceAction = currentSourceAction;
            this.RunParameters = (runParameters != null ? runParameters.ToArray() : null);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "CurrentSourceAction: " + this.CurrentSourceAction.ToString() +
                "; Name: " + this.GuiGridInteraction.Name +
                "; Params: " + (this.RunParameters != null ? "(" + this.RunParameters.ToString(",") + ")" : "{Null}");
        }
        /// <summary>
        /// Definice interakce
        /// </summary>
        public GuiGridInteraction GuiGridInteraction { get; private set; }
        /// <summary>
        /// Aktuální akce, která spustila interakce
        /// </summary>
        public SourceActionType CurrentSourceAction { get; private set; }
        /// <summary>
        /// Parametry pro běh, předané při spuštění z Toolbaru
        /// </summary>
        public string[] RunParameters { get; private set; }

        /// <summary>
        /// Zdrojová akce, na kterou je tato interakce navázaná
        /// </summary>
        public SourceActionType SourceAction { get { return this.GuiGridInteraction.SourceAction; } }
        /// <summary>
        /// Cílová tabulka <see cref="GuiGrid"/>, kam bude akce odeslána.
        /// Pokud nebude zadáno, pak se buď tato interakce nepoužije, nebo se použije na Source tabulku, podle typu interakce.
        /// </summary>
        public string TargetGridFullName { get { return this.GuiGridInteraction.TargetGridFullName; } }
        /// <summary>
        /// Akce, kterou má provést cílová tabulka
        /// </summary>
        public TargetActionType TargetAction { get { return this.GuiGridInteraction.TargetAction; } }
        /// <summary>
        /// Podmínky dle nastavení Toolbaru, za kterých se má tato interakce provést.
        /// V aktuální verzi se podmínky mohou vázat pouze na stav <see cref="GuiToolbarItem.IsChecked"/> prkvů toolbaru <see cref="GuiToolbarItem"/>.
        /// Na prvek toolbaru se vážou přes jeho jméno <see cref="GuiBase.Name"/>.
        /// Pokud některá ze zde vyjmenovaných položek bude zaškrtnutá, bude tato interakce použita, a naopak.
        /// </summary>
        public string Conditions { get { return this.GuiGridInteraction.Conditions; } }
        /// <summary>
        /// true pokud tato interakce je podmíněná stavem Toolbarů
        /// </summary>
        public bool IsConditional { get { return !String.IsNullOrEmpty(this.Conditions); } }

        /// <summary>
        /// Vrátí parametr uložený na dané pozici: <see cref="RunParameters"/>[index]
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetParameterString(int index)
        {
            if (this.RunParameters == null || this.RunParameters.Length == 0 || index < 0 || index >= this.RunParameters.Length) return null;
            return this.RunParameters[index];
        }
        /// <summary>
        /// Vrátí parametr uložený na dané pozici: <see cref="RunParameters"/>[index] převedený na Int32, nebo vrátí null
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int? GetParameterInt32N(int index)
        {
            string text = this.GetParameterString(index);
            if (String.IsNullOrEmpty(text)) return null;
            int value;
            if (!Int32.TryParse(text, out value)) return null;
            return value;
        }
    }
    #endregion
}
