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

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class DataGraphTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// <summary>
    /// DataGraphTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// </summary>
    public class DataGraphTable : IDataGraphTableInternal, ITimeGraphDataSource
    {
        #region Konstrukce, postupné vkládání dat z tabulek, včetně finalizace
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mainData"></param>
        /// <param name="tableName"></param>
        public DataGraphTable(MainData mainData, string tableName, DataDeclaration dataDeclaration)
        {
            this.MainData = mainData;
            this.TableName = tableName;
            this.DataDeclaration = dataDeclaration;
            this.DataGraphProperties = DataGraphProperties.CreateFrom(this, this.DataDeclaration.Data);
            this._TableRow = null;
            this._TableInfoList = new List<Table>();
            this._GIdIndex = new Index<GId>(IndexScopeType.TKeyType);
            this._GraphItemDict = new Dictionary<GId, DataGraphItem>();
        }
        /// <summary>
        /// Vlastník = instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        internal MainData MainData { get; private set; }
        /// <summary>
        /// Vlastník přetypovaný na IMainDataInternal
        /// </summary>
        protected IMainDataInternal IMainData { get { return (this.MainData as IMainDataInternal); } }
        /// <summary>
        /// Verze dat, do které patří tato tabulka.
        /// </summary>
        public Int32? DataId { get { return (this.DataDeclaration != null ? (Int32?)this.DataDeclaration.DataId : (Int32?)null); } }
        /// <summary>
        /// Cílový prostor v panelu <see cref="SchedulerPanel"/> pro tuto položku deklarace
        /// </summary>
        public DataTargetType Target { get { return (this.DataDeclaration != null ? this.DataDeclaration.Target : DataTargetType.None); } }
        /// <summary>
        /// Název této tabulky
        /// </summary>
        public string TableName { get; private set; }
        /// <summary>
        /// Deklarace dat pro tuto tabulku.
        /// Pokud je null, je to způsobené nekonzistencí dat (je předán obsah tabulky, ale její jméno není uvedeno v deklaraci).
        /// </summary>
        public DataDeclaration DataDeclaration { get; private set; }
        /// <summary>
        /// Vlastnosti tabulky, načtené z DataDeclaration
        /// </summary>
        public DataGraphProperties DataGraphProperties { get; private set; }
        /// <summary>
        /// Přidá další data, dodaná ve formě serializované <see cref="DataTable"/> do this tabulky
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tableType"></param>
        public void AddTable(string data, DataTableType tableType)
        {
            string text = WorkSchedulerSupport.Decompress(data);
            DataTable dataTable = WorkSchedulerSupport.TableDeserialize(text);
            switch (tableType)
            {
                case DataTableType.Row:
                    this.AddTableRow(dataTable);
                    break;
                case DataTableType.Graph:
                    this.AddTableGraph(dataTable);
                    break;
                case DataTableType.Rel:
                    this.AddTableRel(dataTable);
                    break;
                case DataTableType.Item:
                    this.AddTableItem(dataTable);
                    break;
            }
        }
        /// <summary>
        /// Metoda vloží data řádků.
        /// Lze vložit pouze jednu tabulku; další pokus o vložení skončí chybou.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void AddTableRow(DataTable dataTable)
        {
            if (this.TableRow != null)
                throw new GraphLibDataException("Duplicitní zadání dat typu Row pro tabulku <" + this.TableName + ">.");
            this._TableRow = Table.CreateFrom(dataTable);
            this._TableRow.OpenRecordForm += _TableRow_OpenRecordForm;
            if (this.TableRow.AllowPrimaryKey)
                this.TableRow.HasPrimaryIndex = true;
        }
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
        /// Metoda přidá data grafických prvků.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void AddTableGraph(DataTable dataTable)
        {
            WorkSchedulerSupport.CheckTable(dataTable, WorkSchedulerSupport.DATA_TABLE_GRAPH_STRUCTURE);
            foreach (DataRow row in dataTable.Rows)
            {   // Grafické řádky se vytvářejí přímo z DataRow, nepotřebujeme kvůli nim konvertovat DataTable na Table:
                DataGraphItem dataGraphItem = DataGraphItem.CreateFrom(this, row);
                this.AddGraphItem(dataGraphItem);
            }
        }
        protected void AddTableRel(DataTable dataTable)
        {
            // Doplnit strukturu a načítání vztahů
        }
        /// <summary>
        /// Provede uložení dat typu Item = textové informace o položce grafu.
        /// Tabulka musí umožnit <see cref="Table.AllowPrimaryKey"/>.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void AddTableItem(DataTable dataTable)
        {
            Table table = Table.CreateFrom(dataTable);
            if (!table.AllowPrimaryKey)
                throw new GraphLibDataException("Data typu Item pro tabulku <" + this.TableName + "> nepodporují PrimaryKey.");
            table.HasPrimaryIndex = true;
            this._TableInfoList.Add(table);
        }
        /// <summary>
        /// Finalizuje dosud načtená data. Další data se již načítat nebudou.
        /// </summary>
        internal void LoadFinalise()
        {
            if (this.DataDeclaration == null || this.TableRow == null)
                return;

            using (var scope = App.Trace.Scope(TracePriority.Priority3_BellowNormal, "DataGraphTable", "LoadFinalise", ""))
            {
                this.CreateGraphs();
                this.FillGraphItems();
            }
        }
        /// <summary>
        /// Vrátí true, pokud this tabulka odpovídá dané verzi dat a názvu tabulky.
        /// </summary>
        /// <param name="dataId"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        internal bool EqualsId(int? dataId, string tableName)
        {
            if (!String.Equals(this.TableName, tableName, StringComparison.InvariantCulture)) return false;
            return (this.DataId == dataId);
        }
        #endregion
        #region Tvorba a modifikace grafů
        /// <summary>
        /// Metoda vytvoří grafy do položek tabulky řádků
        /// </summary>
        protected void CreateGraphs()
        {
            if (!this.IsTableRowWithGraph) return;

            switch (this.DataGraphProperties.GraphPosition.Value)
            {
                case DataGraphPositionType.InLastColumn:
                    this.CreateGraphLastColumn();
                    break;
                case DataGraphPositionType.OnBackgroundProportional:
                case DataGraphPositionType.OnBackgroundLogarithmic:
                    this.CreateGraphBackground();
                    break;
            }
        }
        /// <summary>
        /// Připraví do tabulky <see cref="TableRow"/> nový sloupec pro graf, nastaví vlastnosti sloupce i grafu,
        /// a do každého řádku této tabulky vloží (do tohoto nového sloupce) nový <see cref="GTimeGraph"/>.
        /// </summary>
        protected void CreateGraphLastColumn()
        {
            Column graphColumn = new Column("__time__graph__");

            graphColumn.ColumnProperties.AllowColumnResize = true;
            graphColumn.ColumnProperties.AllowColumnSortByClick = false;
            graphColumn.ColumnProperties.AutoWidth = true;
            graphColumn.ColumnProperties.ColumnContent = ColumnContentType.TimeGraph;
            graphColumn.ColumnProperties.IsVisible = true;
            graphColumn.ColumnProperties.WidthMininum = 250;

            graphColumn.GraphParameters = new TimeGraphProperties();
            graphColumn.GraphParameters.TimeAxisMode = TimeGraphTimeAxisMode.Standard;
            graphColumn.GraphParameters.TimeAxisVisibleTickLevel = AxisTickType.StdTick;
            graphColumn.GraphParameters.InitialResizeMode = AxisResizeContentMode.ChangeScale;
            graphColumn.GraphParameters.InitialValue = this.CreateInitialTimeRange();
            graphColumn.GraphParameters.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;

            this.TableRow.Columns.Add(graphColumn);
            this.TableRowGraphColumn = graphColumn;

            this.AddTimeGraphToRows();
        }
        /// <summary>
        /// Připraví do tabulky <see cref="TableRow"/> data (nastavení) pro graf, který se zobrazuje na pozadí,
        /// a do každého řádku této tabulky vloží (do property <see cref="Table.BackgroundValue"/>) nový <see cref="GTimeGraph"/>.
        /// </summary>
        protected void CreateGraphBackground()
        {
            this.TableRow.GraphParameters = new TimeGraphProperties();
            this.TableRow.GraphParameters.TimeAxisMode = this.TimeAxisMode;
            this.TableRow.GraphParameters.TimeAxisVisibleTickLevel = AxisTickType.BigTick;

            this.AddTimeGraphToRows();
        }
        /// <summary>
        /// Metoda zajistí, že všechny řádky v tabulce <see cref="TableRow"/> budou mít korektně vytvořený graf,
        /// a to buď ve sloupci <see cref="TableRowGraphColumn"/>, anebo jako <see cref="Row.BackgroundValue"/>.
        /// Pokud již graf je vytvořen, nebude vytvářet nový.
        /// </summary>
        protected void AddTimeGraphToRows()
        {
            Column graphColumn = this.TableRowGraphColumn;
            foreach (Row row in this.TableRow.Rows)
                this.AddTimeGraphToRow(row, graphColumn);
        }
        /// <summary>
        /// Metoda zajistí, že daný řádek bude mít korektně vytvořený graf,
        /// a to buď ve sloupci <see cref="TableRowGraphColumn"/>, anebo jako <see cref="Row.BackgroundValue"/>.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="graphColumn"></param>
        protected void AddTimeGraphToRow(Row row, Column graphColumn)
        {
            GId rowGId = row.RecordGId;
            if (graphColumn != null)
            {
                Cell graphCell = row[graphColumn];
                if (graphCell.ValueType != TableValueType.ITimeInteractiveGraph)
                    graphCell.Value = this.CreateGTimeGraph(rowGId, true);
            }
            else
            {
                if (row.BackgroundValueType != TableValueType.ITimeInteractiveGraph)
                    row.BackgroundValue = this.CreateGTimeGraph(rowGId, false);
            }
        }
        /// <summary>
        /// Vrátí časový interval, který se má zobrazit jako výchozí v grafu.
        /// </summary>
        /// <returns></returns>
        protected TimeRange CreateInitialTimeRange()
        {
            DateTime now = DateTime.Now;
            int dow = (now.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)now.DayOfWeek) - 1);
            DateTime begin = new DateTime(now.Year, now.Month, now.Day).AddDays(-dow);
            DateTime end = begin.AddDays(7d);
            double add = 6d;
            return new TimeRange(begin.AddHours(-add), end.AddHours(add));
        }
        /// <summary>
        /// Vytvoří a vrátí new instanci grafu (třída <see cref="GTimeGraph"/>), kompletně připravenou k práci, ale bez položek (ty se dodávají později).
        /// Do instance grafu se vepíše její <see cref="GTimeGraph.GraphId"/> = index odpovídající danému GId řádku (parametr "rowGId").
        /// </summary>
        /// <param name="rowGId"></param>
        /// <param name="isFullInteractive"></param>
        /// <returns></returns>
        protected GTimeGraph CreateGTimeGraph(GId rowGId, bool isFullInteractive)
        {
            GTimeGraph graph = new GTimeGraph();
            graph.DataSource = this;
            graph.GraphId = this.GetId(rowGId);
            return graph;
        }
        /// <summary>
        /// Metoda zajistí vložení všech načtených položek grafů do odpovídajících grafů v tabulce TableRow.
        /// </summary>
        protected void FillGraphItems()
        {
            this.FillGraphItems(this.GraphItems);
        }
        /// <summary>
        /// Metoda zajistí vložení zadaných položek grafů do odpovídajících grafů v tabulce TableRow.
        /// </summary>
        protected void FillGraphItems(IEnumerable<DataGraphItem> graphItems)
        {
            foreach (var graphItem in graphItems)
                this.FillGraphItem(graphItem);
        }
        /// <summary>
        /// Metoda zajistí vložení dané položky graf do odpovídajícího grafu v tabulce TableRow.
        /// </summary>
        protected void FillGraphItem(DataGraphItem graphItem)
        {
            GTimeGraph timeGraph;
            if (!this.TryGetGraphForItem(graphItem, out timeGraph)) return;
            timeGraph.ItemList.Add(graphItem);
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit objekt <see cref="GTimeGraph"/> pro položku grafu dle parametru.
        /// Vyhledá řádek v tabulce <see cref="TableRow"/> podle <see cref="DataGraphItem.ParentGId"/>,
        /// a v řádku najde a vrátí graf podle režimu zobrazení grafu: buď z Value posledního columnu, nebo z <see cref="Row.BackgroundValue"/>
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="timeGraph"></param>
        /// <returns></returns>
        protected bool TryGetGraphForItem(DataGraphItem graphItem, out GTimeGraph timeGraph)
        {
            timeGraph = null;
            Row row;
            if (graphItem.ParentGId == null || !this.TableRow.TryGetRowOnPrimaryKey(graphItem.ParentGId, out row)) return false;
            switch (this.GraphPosition)
            {
                case DataGraphPositionType.InLastColumn:
                    if (this.TableRowGraphColumn != null)
                        timeGraph = row[this.TableRowGraphColumn].Value as GTimeGraph;
                    break;
                case DataGraphPositionType.OnBackgroundLogarithmic:
                case DataGraphPositionType.OnBackgroundProportional:
                    timeGraph = row.BackgroundValue as GTimeGraph;
                    break;
            }
            return (timeGraph != null);
        }
        /// <summary>
        /// Obsahuje true, pokud this tabulka má zobrazit graf
        /// </summary>
        protected bool IsTableRowWithGraph
        {
            get
            {
                DataGraphPositionType graphPosition = this.GraphPosition;
                return (graphPosition == DataGraphPositionType.InLastColumn ||
                        graphPosition == DataGraphPositionType.OnBackgroundProportional ||
                        graphPosition == DataGraphPositionType.OnBackgroundLogarithmic);
            }
        }
        /// <summary>
        /// Režim časové osy v grafu, podle zadání v deklaraci
        /// </summary>
        protected TimeGraphTimeAxisMode TimeAxisMode
        {
            get
            {
                DataGraphPositionType graphPosition = this.GraphPosition;
                switch (graphPosition)
                {
                    case DataGraphPositionType.InLastColumn: return TimeGraphTimeAxisMode.Standard;
                    case DataGraphPositionType.OnBackgroundProportional: return TimeGraphTimeAxisMode.ProportionalScale;
                    case DataGraphPositionType.OnBackgroundLogarithmic: return TimeGraphTimeAxisMode.LogarithmicScale;
                }
                return TimeGraphTimeAxisMode.Default;
            }
        }
        /// <summary>
        /// Pozice grafu. Obsahuje None, pokud graf není definován.
        /// </summary>
        protected DataGraphPositionType GraphPosition
        {
            get
            {
                if (this.TableRow == null || this.DataDeclaration == null || this.DataGraphProperties == null) return DataGraphPositionType.None;
                DataGraphPositionType? gp = this.DataGraphProperties.GraphPosition;
                return (gp.HasValue ? gp.Value : DataGraphPositionType.None);
            }
        }
        /// <summary>
        /// Sloupec hlavní tabulky, který zobrazuje graf při umístění <see cref="DataGraphPositionType.InLastColumn"/>
        /// </summary>
        protected Column TableRowGraphColumn { get; private set; }
        #endregion
        #region Data - tabulka s řádky, prvky grafů, vztahů, položky s informacemi
        /// <summary>
        /// Tabulka s řádky.
        /// Tato tabulka je zobrazována.
        /// </summary>
        public Table TableRow { get { return this._TableRow; } }
        protected Table _TableRow;
        /// <summary>
        /// Data položek všech grafů (=ze všech řádků) tabulky <see cref="TableRow"/>
        /// </summary>
        public IEnumerable<DataGraphItem> GraphItems { get { return this._GraphItemDict.Values; } }
        /// <summary>
        /// Index pro obousměrnou konverzi Int32 - GId
        /// </summary>
        protected Index<GId> _GIdIndex;
        /// <summary>
        /// Dictionary pro vyhledání prvku grafu podle jeho GId. Primární úložiště položek grafů.
        /// </summary>
        protected Dictionary<GId, DataGraphItem> _GraphItemDict;
        #endregion
        #region Textové informace pro položky grafů - tabulka TableInfoList a její obsluha
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro daný prvek.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <returns></returns>
        protected Row GetTableInfoRow(DataGraphItem graphItem)
        {
            if (graphItem == null) return null;
            return this.GetTableInfoRow(graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.ParentGId);
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro některý GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="gids"></param>
        /// <returns></returns>
        protected Row GetTableInfoRow(params GId[] gids)
        {
            foreach (GId gId in gids)
            {
                Row row = this.GetTableInfoRowForGId(gId);
                if (row != null) return row;
            }
            return null;
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro daný GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected Row GetTableInfoRowForGId(GId gId)
        {
            if (gId == null) return null;

            foreach (Table table in this._TableInfoList)
            {
                Row row;
                if (table.TryGetRowOnPrimaryKey(gId, out row))
                    return row;
            }
            return null;
        }
        /// <summary>
        /// Tabulky s informacemi = popisky pro položky grafů.
        /// </summary>
        public List<Table> TableInfoList { get { return this._TableInfoList; } }
        protected List<Table> _TableInfoList;
        #endregion
        #region Správa indexů GId a objektů grafů
        /// <summary>
        /// Metoda vrátí Int32 ID pro daný <see cref="GId"/>.
        /// Pro opakovaný požadavek na tentýž <see cref="GId"/> vrací shodnou hodnotu ID.
        /// Pro první požadavek na určitý <see cref="GId"/> vytvoří nový ID.
        /// Reverzní metoda je <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected int GetId(GId gId)
        {
            if (gId == null) return 0;
            return this._GIdIndex.GetIndex(gId);
        }
        /// <summary>
        /// Pro daný ID vrátí <see cref="GId"/>, ale pouze pokud byl přidělen v metodě <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected GId GetGId(int id)
        {
            if (id == 0) return null;
            GId gId;
            if (!this._GIdIndex.TryGetKey(id, out gId)) return null;
            return gId;
        }
        /// <summary>
        /// Metoda uloží danou položku grafu do interního úložiště <see cref="_GraphItemDict"/>.
        /// </summary>
        /// <param name="dataGraphItem"></param>
        /// <returns></returns>
        protected void AddGraphItem(DataGraphItem dataGraphItem)
        {
            if (dataGraphItem == null || dataGraphItem.ItemGId == null) return;
            GId gId = dataGraphItem.ItemGId;
            if (!this._GraphItemDict.ContainsKey(gId))
                this._GraphItemDict.Add(gId, dataGraphItem);
        }
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
            if (!this._GraphItemDict.TryGetValue(gId, out dataGraphItem)) return null;
            return dataGraphItem;
        }
        #region Explicitní implementace IDataGraphTableInternal
        int IDataGraphTableInternal.GetId(GId gId) { return this.GetId(gId); }
        GId IDataGraphTableInternal.GetGId(int id) { return this.GetGId(id); }
        DataGraphItem IDataGraphTableInternal.GetGraphItem(int id) { return this.GetGraphItem(id); }
        DataGraphItem IDataGraphTableInternal.GetGraphItem(GId gId) { return this.GetGraphItem(gId); }
        #endregion
        #endregion
        #region Komunikace s hlavním zdrojem dat (MainData)
        /// <summary>
        /// Tato metoda zajistí otevření formuláře daného záznamu.
        /// Pouze převolá odpovídající metodu v <see cref="MainData"/>.
        /// </summary>
        /// <param name="recordGId"></param>
        protected void RunOpenRecordForm(GId recordGId)
        {
            if (this.MainData != null)
                this.IMainData.RunOpenRecordForm(recordGId);
        }
        #endregion
        #region Implementace ITimeGraphDataSource: Zdroj dat pro grafy
        /// <summary>
        /// Připraví text pro položku grafu
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareText(CreateTextArgs args)
        {
            DataGraphItem graphItem = this.GetActionGraphItem(args);
            if (graphItem == null) return;

            Row infoRow = this.GetTableInfoRow(graphItem);
            if (infoRow == null) return;

            string text = "";
            int width = args.GraphItemSize.Width - 4;
            foreach (Column column in infoRow.Table.Columns)
            {
                if (!column.ColumnProperties.IsVisible) continue;
                Cell cell = infoRow[column];
                if (cell.ValueType == TableValueType.Text && cell.Value != null)
                {
                    bool isEmpty = (text.Length == 0);
                    string test = text + (isEmpty ? "" : " ") + cell.Value.ToString();
                    if (isEmpty)
                        text = test;
                    else
                    {
                        Size size = args.MeasureString(test);
                        if (size.Width > width)
                            break;
                        text = test;
                    }
                }
                args.Text = text;
            }
        }
        /// <summary>
        /// Připraví tooltip pro položku grafu.
        /// Text je připraven jako tabulka: obsahuje řádky oddělené NewLine, a sloupce oddělené Tab.
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareToolTip(CreateToolTipArgs args)
        {
            DataGraphItem graphItem = this.GetActionGraphItem(args);
            if (graphItem == null) return;

            Row infoRow = this.GetTableInfoRow(graphItem);
            if (infoRow == null) return;

            GId recordGId = infoRow.RecordGId;
            args.ToolTipData.TitleText = (recordGId != null ? recordGId.ClassName : "INFORMACE O POLOŽCE");

            StringBuilder sb = new StringBuilder();
            foreach (Column column in infoRow.Table.Columns)
            {
                if (!column.ColumnProperties.IsVisible) continue;
                Cell cell = infoRow[column];
                if (cell.ValueType == TableValueType.Text)
                    sb.AppendLine(column.ColumnProperties.Title + "\t" + cell.Value);
            }
            string text = sb.ToString();
            args.ToolTipData.InfoText = text;
            args.ToolTipData.InfoUseTabs = true;
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném grafu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForGraph(ItemActionArgs args)
        {
            return this.IMainData.CreateContextMenu(null, args);
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném prvku
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForItem(ItemActionArgs args)
        {
            DataGraphItem graphItem = this.GetActionGraphItem(args);                               // Prvek, na nějž se kliklo
            return this.IMainData.CreateContextMenu(graphItem, args);
        }
        /// <summary>
        /// Uživatel dal doubleclick na grafický prvek
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemDoubleClick(ItemActionArgs args)
        {
            if (args.ModifierKeys == Keys.Control)
            {   // Akce typu Ctrl+DoubleClick na grafickém prvku si žádá otevření formuláře:
                DataGraphItem graphItem = this.GetActionGraphItem(args);
                if (graphItem != null)
                    this.RunOpenRecordForm(graphItem.DataGId);
            }
        }
        /// <summary>
        /// Metoda najde a vrátí grafický prvek zdejší třídy <see cref="DataGraphItem"/> pro daný interaktivní prvek.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected DataGraphItem GetActionGraphItem(ItemArgs args)
        {
            int itemId = (args.CurrentItem != null ? args.CurrentItem.ItemId : (args.GroupedItems.Length > 0 ? args.GroupedItems[0].ItemId : 0));
            if (itemId <= 0) return null;
            return this.GetGraphItem(itemId);
        }
        protected void ItemDragItemStart(ItemDragDropArgs args)
        {
        }
        /// <summary>
        /// Scheduler zde pomáhá určovat, zda jak a kam lze nebo nelze přemisťovat prvek grafu.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragItemMove(ItemDragDropArgs args)
        {
        }
        protected void ItemDragItemDrop(ItemDragDropArgs args)
        {
        }

        void ITimeGraphDataSource.CreateText(CreateTextArgs args) { this.GraphItemPrepareText(args); }
        void ITimeGraphDataSource.CreateToolTip(CreateToolTipArgs args) { this.GraphItemPrepareToolTip(args); }
        void ITimeGraphDataSource.GraphRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForGraph(args); }
        void ITimeGraphDataSource.ItemRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForItem(args); }
        void ITimeGraphDataSource.ItemDoubleClick(ItemActionArgs args) { this.GraphItemDoubleClick(args); }
        void ITimeGraphDataSource.ItemLongClick(ItemActionArgs args) { }
        void ITimeGraphDataSource.ItemChange(ItemChangeArgs args) { }
        void ITimeGraphDataSource.ItemDragItemStart(ItemDragDropArgs args) { this.ItemDragItemStart(args); }
        void ITimeGraphDataSource.ItemDragItemMove(ItemDragDropArgs args) { this.ItemDragItemMove(args); }
        void ITimeGraphDataSource.ItemDragItemDrop(ItemDragDropArgs args) { this.ItemDragItemDrop(args); }
        #endregion
    }
    /// <summary>
    /// Rozhraní pro přístup k interním metodám třídy DataGraphTable
    /// </summary>
    public interface IDataGraphTableInternal
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
        /// </summary>
        /// <param name="graphTable"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static DataGraphItem CreateFrom(DataGraphTable graphTable, DataRow row)
        {
            if (row == null) return null;

            DataGraphItem item = new DataGraphItem(graphTable);
            // Struktura řádku: parent_rec_id int; parent_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; back_color string; join_back_color string; data string
            item._ParentGId = GetGId(row, "parent");
            item._ItemGId = GetGId(row, "item");
            item._GroupGId = GetGId(row, "group");
            item._DataGId = GetGId(row, "data");
            item._Layer = row.GetValue<Int32>("layer");
            item._Level = row.GetValue<Int32>("level");
            item._Order = 0;
            item._Height = row.GetValue<float>("height");
            item._Time = new TimeRange(row.GetValue<DateTime?>("time_begin"), row.GetValue<DateTime?>("time_end"));
            item._BackColor = MainData.GetColor(row.GetValue<string>("back_color"));
            item._BorderColor = null;
            item._BackStyle = null;
            item._LinkBackColor = MainData.GetColor(row.GetValue<string>("join_back_color"));
            item._LoadData(row.GetValue<string>("data"));

            // ID pro grafickou vrstvu:
            IDataGraphTableInternal iGraphTable = graphTable as IDataGraphTableInternal;
            item._ItemId = iGraphTable.GetId(item.ItemGId);
            item._GroupId = iGraphTable.GetId(item.GroupGId);

            return item;
        }
        /// <summary>
        /// Metoda rozebere string "data" na KeyValues a z nich naplní další nepovinné prvky.
        /// </summary>
        /// <param name="data"></param>
        protected void _LoadData(string data)
        {
            this._LoadDataDefault();
            if (String.IsNullOrEmpty(data)) return;

            // data mají formát: "key: value; key:value; key: value", 
            // například: "EditMode: ResizeTime + ResizeHeight + MoveToAnotherTime; BackStyle: Percent50; BorderColor: Black"
            var items = data.ToKeyValues(";", ":", true, true);
            foreach (var item in items)
            {
                string key = item.Key; // .ToLower();
                switch (key)
                {
                    case WorkSchedulerSupport.DATA_GRAPHITEM_EDITMODE:
                    case "editmode":
                    case "edit_mode":
                        this._BehaviorMode = Scheduler.MainData.GetBehaviorMode(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_GRAPHITEM_BACKSTYLE:
                    case "backstyle":
                    case "back_style":
                        // this._BackStyle = Scheduler.MainData.GetHatchStyle(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_GRAPHITEM_BORDERCOLOR:
                    case "bordercolor":
                    case "border_color":
                        this._BorderColor = Scheduler.MainData.GetColor(item.Value);
                        break;
                        // ...a další klíče a hodnoty mohou následovat:
                }
            }
        }
        /// <summary>
        /// Naplní defaultní hodnoty podle čísla třídy prvku
        /// </summary>
        protected void _LoadDataDefault()
        {
            int classNumber = (this.ItemGId.ClassId >= 0 ? this.ItemGId.ClassId : -this.ItemGId.ClassId);
            switch (classNumber)        // Číslo třídy prvku grafu
            {
                case GreenClasses.PlanUnitCCl:             // Stav kapacit
                    this._BackStyle = System.Drawing.Drawing2D.HatchStyle.Percent50;
                    this._BehaviorMode = GraphItemBehaviorMode.None | GraphItemBehaviorMode.ShowToolTipFadeIn;
                    break;
                case GreenClasses.PlanUnitCUnit:           // Jednotka práce
                    this._BackStyle = null;
                    this._BehaviorMode = GraphItemBehaviorMode.DefaultWorkTime | GraphItemBehaviorMode.DefaultText;
                    break;
                case GreenClasses.ProductOrderOperation:   // Operace VP
                    this._BackStyle = null;
                    this._BehaviorMode = GraphItemBehaviorMode.None | GraphItemBehaviorMode.ShowToolTipFadeIn;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Metoda z daného řádku načte hodnoty pro číslo třídy a číslo záznamu, a z nich vrátí <see cref="GId"/>.
        /// Jako název (parametr name) dostává základ jména dvojice sloupců, které obsahují třídu a záznam.
        /// Například pro dvojici sloupců "parent_rec_id" a "parent_class_id" se jako name předává "parent".
        /// Pokud sloupce neexistují, dojde k chybě.
        /// Pokud obsahují null, vrací se null.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static GId GetGId(DataRow row, string name)
        {
            Int32? classId = row.GetValue<Int32?>(name + "_class_id");
            Int32? recordId = row.GetValue<Int32?>(name + "_rec_id");
            if (!(classId.HasValue && recordId.HasValue)) return null;
            return new GId(classId.Value, recordId.Value);
        }
        /// <summary>
        /// privátní konstruktor. Instanci lze založit pomocí metody <see cref="CreateFrom(DataGraphTable, DataRow)"/>.
        /// </summary>
        private DataGraphItem(DataGraphTable graphTable)
        {
            this._GraphTable = graphTable;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Item: " + this._ItemGId.ToString() + "; Time: " + this._Time.ToString() + "; Height: " + this._Height.ToString();
        }
        /// <summary>
        /// Vlastník = datová základna, instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        protected MainData MainData { get { return this.GraphTable.MainData; } }
        /// <summary>
        /// Vlastník prvku = celá tabulka
        /// </summary>
        private DataGraphTable _GraphTable;
        private ITimeInteractiveGraph _OwnerGraph;
        private GId _ParentGId;
        private GId _ItemGId;
        private GId _GroupGId;
        private GId _DataGId;
        private int _ItemId;
        private int _GroupId;
        private int _Layer;
        private int _Level;
        private int _Order;
        private float _Height;
        private GraphItemBehaviorMode _BehaviorMode;
        private TimeRange _Time;
        private Color? _BackColor;
        private System.Drawing.Drawing2D.HatchStyle? _BackStyle;
        private Color? _BorderColor;
        private Color? _LinkBackColor;
        private GTimeGraphItem _GControl;
        #endregion
        #region Aplikační data - identifikátory atd
        /// <summary>
        /// Vlastník prvku grafu = tabulka s komplexními daty
        /// </summary>
        public DataGraphTable GraphTable { get { return this._GraphTable; } }
        /// <summary>
        /// Veřejný identifikátor MAJITELE PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o Kapacitní plánovací jednotku.
        /// </summary>
        public GId ParentGId { get { return this._ParentGId; } }
        /// <summary>
        /// Veřejný identifikátor GRAFICKÉHO PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o záznam třídy Stav kapacit, nebo Pracovní jednotka.
        /// </summary>
        public GId ItemGId { get { return this._ItemGId; } }
        /// <summary>
        /// Veřejný identifikátor SKUPINY PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o záznam třídy Paralelní průchod.
        /// </summary>
        public GId GroupGId { get { return this._GroupGId; } }
        /// <summary>
        /// Veřejný identifikátor DATOVÉHO OBJEKTU: obsahuje číslo třídy a číslo záznamu.
        /// Může jít o Operaci výrobního příkazu.
        /// </summary>
        public GId DataGId { get { return this._DataGId; } }
        /// <summary>
        /// Číslo grafické vrstvy (Z-order).
        /// </summary>
        public int Layer { get { return this._Layer; } }
        /// <summary>
        /// Číslo grafické hladiny (Y-group).
        /// </summary>
        public int Level { get { return this._Level; } }
        /// <summary>
        /// Číslo pořadí (sub-Y-group)
        /// </summary>
        public int Order { get { return this._Order; } }
        /// <summary>
        /// Logická výška grafického prvku, 1=normální jednotková výška
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        public GraphItemBehaviorMode EditMode { get { return this._BehaviorMode; } }
        /// <summary>
        /// Časový interval tohoto prvku
        /// </summary>
        public TimeRange Time { get { return this._Time; } }
        /// <summary>
        /// Barva pozadí prvku
        /// </summary>
        public Color? BackColor { get { return this._BackColor; } }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// </summary>
        public System.Drawing.Drawing2D.HatchStyle? BackStyle { get { return this._BackStyle; } }
        /// <summary>
        /// Barva okrajů prvku
        /// </summary>
        public Color? BorderColor { get { return this._BorderColor; } }
        /// <summary>
        /// Barva spojovací linky prvků
        /// </summary>
        public Color? LinkBackColor { get { return this._LinkBackColor; } }

        #endregion
        #region Podpora pro kreslení a interaktivitu
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/> : <see cref="GTimeGraphItem.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        protected void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            this._GControl.DrawItem(e, boundsAbsolute, drawMode);
        }
        #endregion
        #region Explicitní implementace rozhraní ITimeGraphItem
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.GroupId { get { return this._GroupId; } }
        int ITimeGraphItem.Layer { get { return this._Layer; } }
        int ITimeGraphItem.Level { get { return this._Level; } }
        int ITimeGraphItem.Order { get { return this._Order; } }
        float ITimeGraphItem.Height { get { return this._Height; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this._BehaviorMode; } }
        TimeRange ITimeGraphItem.Time { get { return this._Time; } }
        Color? ITimeGraphItem.BackColor { get { return this._BackColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._BackStyle; } }
        Color? ITimeGraphItem.BorderColor { get { return this._BorderColor; } }
        Color? ITimeGraphItem.LinkBackColor { get { return this._LinkBackColor; } }
        GTimeGraphItem ITimeGraphItem.GControl { get { return this._GControl; } set { this._GControl = value; } }
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
    #endregion
    #region class DataGraphProperties : vlastnosti tabulky, popis chování atd - načteno z dodaných dat
    /// <summary>
    /// DataGraphProperties : vlastnosti tabulky, popis chování atd - načteno z dodaných dat
    /// </summary>
    public class DataGraphProperties
    {
        #region Konstrukce, načtení
        /// <summary>
        /// Vytvoří a vrátí instanci DataGraphProperties,vloží do ní dodaná data.
        /// </summary>
        /// <param name="dataGraphTable"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataGraphProperties CreateFrom(DataGraphTable dataGraphTable, string data)
        {
            DataGraphProperties dataGraphProperties = new DataGraphProperties(dataGraphTable);
            dataGraphProperties.LoadData(data);
            return dataGraphProperties;
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        /// <param name="dataGraphTable"></param>
        private DataGraphProperties(DataGraphTable dataGraphTable)
        {
            this.DataGraphTable = dataGraphTable;
        }
        /// <summary>
        /// Načte data do this objektu z datového stringu
        /// </summary>
        /// <param name="data">Obsahuje formát: "GraphPosition: LastColumn; LineHeight: 16; MaxHeight: 320"</param>
        protected void LoadData(string data)
        {
            if (data == null) return;
            var items = data.ToKeyValues(";", ":", true, true);
            foreach (var item in items)
            {
                switch (item.Key)
                {
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_POSITION:
                        this.GraphPosition = MainData.GetGraphPosition(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_LINE_HEIGHT:
                        this.GraphLineHeight = MainData.GetInt32N(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_MIN_HEIGHT:
                        this.RowLineHeightMin = MainData.GetInt32N(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_MAX_HEIGHT:
                        this.RowLineHeightMax = MainData.GetInt32N(item.Value);
                        break;
                }
            }
        }
        /// <summary>
        /// Vlastník = tabulka
        /// </summary>
        protected DataGraphTable DataGraphTable { get; private set; }
        #endregion
        #region Public data
        /// <summary>
        /// Pozice grafu v tabulce
        /// </summary>
        public DataGraphPositionType? GraphPosition { get; private set; }
        /// <summary>
        /// Výška jednotky v grafu, v pixelech
        /// </summary>
        public int? GraphLineHeight { get; private set; }
        /// <summary>
        /// Výška řádku v tabulce minimální, v pixelech
        /// </summary>
        public int? RowLineHeightMin { get; private set; }
        /// <summary>
        /// Výška řádku v tabulce maximální, v pixelech
        /// </summary>
        public int? RowLineHeightMax { get; private set; }

        #endregion
    }
    #endregion
}
