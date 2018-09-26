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
    #region class MainDataTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// <summary>
    /// MainDataTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// </summary>
    public class MainDataTable : IMainDataTableInternal, ITimeGraphDataSource
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
        /// Instance <see cref="GuiGrid"/>, která tvoří datový základ této tabulky
        /// </summary>
        internal GuiGrid GuiGrid { get; private set; }
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
            this.LoadDataGraphProperties();
            this.LoadDataPrepareIndex();
            this.LoadDataLoadRow();
            this.LoadDataCreateGraphs();
            this.LoadDataLoadGraphItems();
            this.LoadDataLoadTexts();
        }
        #endregion
        #region Vlastnosti grafů a další property
        /// <summary>
        /// Načte vlastnosti grafů z <see cref="GuiGraphProperties"/> do <see cref="DataGraphProperties"/>.
        /// </summary>
        protected void LoadDataGraphProperties()
        {
            this.DataGraphProperties = DataGraphProperties.CreateFrom(this, this.GuiGrid.GraphProperties);
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
            get { return ((this.TableRow != null && this.DataGraphProperties != null) ? this.DataGraphProperties.GraphPosition : DataGraphPositionType.None); }
        }
        /// <summary>
        /// Vlastnosti tabulky, načtené z DataDeclaration
        /// </summary>
        public DataGraphProperties DataGraphProperties { get; private set; }
        #endregion
        #region Index pro oboustrannou konverzi GId <=> Int32
        /// <summary>
        /// Připraví index pro převody GId - Int32
        /// </summary>
        protected void LoadDataPrepareIndex()
        {
            this.GIdIntIndex = new Index<GId>();
        }
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
            return this.GIdIntIndex.GetIndex(gId);
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
            if (!this.GIdIntIndex.TryGetKey(id, out gId)) return null;
            return gId;
        }
        /// <summary>
        /// Index pro obousměrnou konverzi Int32 - GId
        /// </summary>
        protected Index<GId> GIdIntIndex { get; set; }
        #endregion
        #region TableRow
        /// <summary>
        /// Načte tabulku s řádky
        /// </summary>
        protected void LoadDataLoadRow()
        {
            this.TableRow = Table.CreateFrom(this.GuiGrid.Rows.DataTable);
            this.TableRow.OpenRecordForm += _TableRow_OpenRecordForm;
            if (this.TableRow.AllowPrimaryKey) this.TableRow.HasPrimaryIndex = true;
        }
        /// <summary>
        /// Tabulka s řádky.
        /// Tato tabulka je zobrazována.
        /// </summary>
        public Table TableRow { get; private set; }
        #endregion
        #region Grafy a položky grafů
        /// <summary>
        /// Do tabulky s řádky vytvoří grafy do všech řádků, zatím prázdné
        /// </summary>
        protected void LoadDataCreateGraphs()
        {
            this.TimeGraphDict = new Dictionary<GId, GTimeGraph>();
            if (this.TableRow == null) return;
            DataGraphPositionType graphPosition = this.GraphPosition;
            if (graphPosition == DataGraphPositionType.None) return;
            this.LoadDataPrepareTableForGraphs(graphPosition);

            foreach (Row row in this.TableRow.Rows)
            {
                GId rowGid = row.RecordGId;
                if (rowGid == null) continue;

                GTimeGraph gTimeGraph = this.LoadDataCreateGTimeGraph(row, graphPosition);
                if (!this.TimeGraphDict.ContainsKey(rowGid))
                    this.TimeGraphDict.Add(rowGid, gTimeGraph);
            }
        }
        /// <summary>
        /// Metoda připraví tabulku <see cref="TableRow"/> na vkládání grafů daného typu
        /// </summary>
        /// <param name="graphPosition"></param>
        protected void LoadDataPrepareTableForGraphs(DataGraphPositionType graphPosition)
        {
            TimeGraphProperties graphProperties = new TimeGraphProperties();
            if (graphPosition == DataGraphPositionType.InLastColumn)
            {
                Column graphColumn = new Column("__time__graph__");

                graphColumn.ColumnProperties.AllowColumnResize = true;
                graphColumn.ColumnProperties.AllowColumnSortByClick = false;
                graphColumn.ColumnProperties.AutoWidth = true;
                graphColumn.ColumnProperties.ColumnContent = ColumnContentType.TimeGraphSynchronized;
                graphColumn.ColumnProperties.IsVisible = true;
                graphColumn.ColumnProperties.WidthMininum = 250;

                graphProperties.TimeAxisMode = TimeGraphTimeAxisMode.Standard;
                graphProperties.TimeAxisVisibleTickLevel = AxisTickType.StdTick;
                graphProperties.InitialResizeMode = this.DataGraphProperties.AxisResizeMode;       // AxisResizeContentMode.ChangeScale;
                graphProperties.InitialValue = this.MainControl.SynchronizedTime.Value;
                graphProperties.MaximalValue = this.MainData.GuiData.Properties.TotalTimeRange;
                graphProperties.InteractiveChangeMode = this.DataGraphProperties.InteractiveChangeMode;
                graphProperties.Opacity = this.DataGraphProperties.Opacity;
                graphColumn.GraphParameters = graphProperties;

                this.TableRow.Columns.Add(graphColumn);
                this.TableRowGraphColumn = graphColumn;
            }
            else
            {
                graphProperties.TimeAxisMode = this.TimeAxisMode;
                graphProperties.TimeAxisVisibleTickLevel = AxisTickType.BigTick;
                graphProperties.Opacity = this.DataGraphProperties.Opacity;
                this.TableRow.GraphParameters = graphProperties;
            }
        }
        /// <summary>
        /// Metoda vytvoří nový <see cref="GTimeGraph"/> pro daný řádek a pozici, umístí jej do řádku, a graf vrátí.
        /// Graf zatím neobsahuje položky.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="graphPosition"></param>
        /// <returns></returns>
        protected GTimeGraph LoadDataCreateGTimeGraph(Row row, DataGraphPositionType graphPosition)
        {
            GTimeGraph gTimeGraph = new GTimeGraph();
            gTimeGraph.DataSource = this;
            gTimeGraph.GraphId = this.GetId(row.RecordGId);

            ITimeInteractiveGraph iTimeGraph = gTimeGraph as ITimeInteractiveGraph;

            if (graphPosition == DataGraphPositionType.InLastColumn)
            {
                iTimeGraph.TimeAxisConvertor = this.TableRowGraphColumn.ColumnHeader.TimeConvertor;
                Cell graphCell = row[this.TableRowGraphColumn];
                graphCell.Value = gTimeGraph;
            }
            else
            {
                iTimeGraph.TimeAxisConvertor = this.GGrid.SynchronizedTimeConvertor;
                row.BackgroundValue = gTimeGraph;
            }

            return gTimeGraph;
        }
        /// <summary>
        /// Načte a zapracuje prvky grafů:
        /// Z dat v <see cref="GuiGrid"/> načte jednotlivé položky grafů <see cref="GuiGraphItem"/>, vytvoří z nich vizuální prvky <see cref="DataGraphItem"/>
        /// a tyto prvky uloží jednak do dictionary <see cref="TimeGraphItemDict"/>, a jednak do jednotlivých grafů v <see cref="TimeGraphDict"/>, podle GIDu řádku.
        /// </summary>
        protected void LoadDataLoadGraphItems()
        {
            GuiGrid guiGrid = this.GuiGrid;

            this.TimeGraphItemDict = new Dictionary<GId, DataGraphItem>();
            if (guiGrid.GraphItems != null)
            {
                foreach (GuiGraphTable guiGraphTable in guiGrid.GraphItems)
                {
                    if (guiGraphTable == null || guiGraphTable.Count == 0) continue;
                    foreach (GuiGraphItem guiGraphItem in guiGraphTable.GraphItems)
                    {
                        DataGraphItem dataGraphItem = DataGraphItem.CreateFrom(this, guiGraphItem);
                        if (dataGraphItem == null) continue;

                        if (!this.TimeGraphItemDict.ContainsKey(dataGraphItem.ItemGId))
                            this.TimeGraphItemDict.Add(dataGraphItem.ItemGId, dataGraphItem);

                        GTimeGraph gTimeGraph;
                        if (this.TimeGraphDict.TryGetValue(dataGraphItem.ParentGId, out gTimeGraph))
                            gTimeGraph.ItemList.Add(dataGraphItem);
                    }
                }
            }
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
        /// Sloupec hlavní tabulky, který zobrazuje graf při umístění <see cref="DataGraphPositionType.InLastColumn"/>
        /// </summary>
        protected Column TableRowGraphColumn { get; private set; }
        #endregion
        #region Textové údaje (popisky grafů)
        /// <summary>
        /// Načte tabulky s texty
        /// </summary>
        protected void LoadDataLoadTexts()
        {
            this.TableTextList = new List<Table>();
            this.TableTextRowDict = new Dictionary<GId, Row>();
            GuiGrid guiGrid = this.GuiGrid;
            if (guiGrid.GraphTexts == null || guiGrid.GraphTexts.Count == 0) return;

            foreach (GuiTable guiTable in guiGrid.GraphTexts)
            {
                if (guiTable == null || guiTable.DataTable == null) continue;
                Table table = Table.CreateFrom(guiTable.DataTable);
                if (!table.AllowPrimaryKey)
                    throw new GraphLibDataException("Data v tabulce textů «" + guiTable.FullName + "." + table.TableName + "» nepodporují PrimaryKey.");
                table.HasPrimaryIndex = true;
                if (table.RowsCount > 0)
                    this.TableTextList.Add(table);
            }
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky textů, obsahující textové informace pro daný prvek.
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
            Row row = null;
            if (gId == null) return row;

            // Nejprve hledáme v "cache":
            if (this.TableTextRowDict.TryGetValue(gId, out row))
                return row;

            // V cache nic není - budeme hledat v kompletních datech:
            foreach (Table table in this.TableTextList)
            {
                if (table.TryGetRowOnPrimaryKey(gId, out row))
                    break;
            }

            // Co jsme našli, dáme do cache (pro příští hledání), a vrátíme:
            this.TableTextRowDict.Add(gId, row);
            return row;
        }
        /// <summary>
        /// Tabulky s informacemi = popisky pro položky grafů.
        /// </summary>
        public List<Table> TableTextList { get; private set; }
        /// <summary>
        /// Index textů = obsahuje GId, pro který se někdy hledal text, a k němu nalezený řádek.
        /// Může rovněž obsahovat NULL pro daný GId, to když nebyl nalezen řádek.
        /// </summary>
        public Dictionary<GId, Row> TableTextRowDict { get; private set; }
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
                this.IMainData.CallAppHostFunction(request, null);
            }
        }
        #endregion
        #region Přemísťování prvku odněkud někam, včetně aplikační logiky
        /// <summary>
        /// Scheduler určuje souřadnici prvku v procesu Drag and Drop,
        /// v akci Move = prvek se pouze přesouvá pomocí myši, ale ještě nebyl nikam umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragDropMove(ItemDragDropArgs args)
        {
            DragSchedulerData data = this.PrepareDragSchedulerData(args);
            args.DragToAbsoluteBounds = data.TargetBounds;
            args.ToolTipData.AnimationType = TooltipAnimationType.Instant;
            args.ToolTipData.TitleText = (data.IsChangeRow ? "Přemístění na jiný řádek" : "Přemístění v rámci řádku");
            args.ToolTipData.InfoText = "Čas: " + data.TargetTime.ToString();
        }
        /// <summary>
        /// Scheduler vyvolá aplikační logiku, která určí definitivní umístění prvku v procesu Drag and Drop,
        /// v akci Drop = prvek byl vizuálně umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragDropDrop(ItemDragDropArgs args)
        {
            // Tady by se měla volat metoda AppHost => aplikační funkce pro přepočet grafu:
            DragSchedulerData data = this.PrepareDragSchedulerData(args);
            GuiRequestGraphItemMove guiData = this.PrepareRequestGraphItemMove(data);

            // Nejprve provedu vizuální přemístění na "grafický" cíl, to proto že aplikační funkce může:  a) neexistovat  b) dlouho trvat:
            this.ItemDragDropDropGuiResponse(data);

            // Následně vyvolám (asynchronní) spuštění aplikační funkce, která zajistí komplexní přepočty a vrátí nová data, 
            //  její response se řeší v metodě ItemDragDropDropAppResponse():
            if (this.HasMainData)
            {
                GuiRequest request = new GuiRequest();
                request.Command = GuiRequest.COMMAND_GraphItemMove;
                request.GraphItemMove = guiData;
                this.IMainData.CallAppHostFunction(request, this.ItemDragDropDropAppResponse);
            }
        }
        /// <summary>
        /// Metoda provede přemístění prvků grafu na požadovanou cílovou pozici, na základě GUI dat.
        /// </summary>
        /// <param name="data"></param>
        protected void ItemDragDropDropGuiResponse(DragSchedulerData data)
        {
            // 1) Proběhne změna na všech prvcích grupy (data.DragGroupItems):
            //   a) Změna jejich času: o daný offset (rozdíl času cílového - původního)
            //   b) Změna hodnoty ParentGId = příslušnost do řádku
            // 2) Pokud se mění řádek, pak:
            //   a) ze zdrojového grafu se prvky odeberou
            //   b) do cílového grafu se prvky přidají
            // 3) Zavolá se Refresh na oba grafy (pokud jsou dva)
            bool isChangeRow = data.IsChangeRow;
            bool isChangeTime = data.IsChangeTime;
            TimeSpan? timeOffset = data.ShiftTime;
            foreach (DataGraphItem item in data.DragGroupItems)
            {
                if (isChangeRow)
                {
                    data.SourceGraph.ItemList.Remove(item);
                    item.ParentGId = data.TargetRow;
                    data.TargetGraph.ItemList.Add(item);
                }
                if (isChangeTime)
                {
                    item.Time = item.Time.ShiftByTime(timeOffset.Value);
                }
            }
            data.SourceGraph.Refresh();
            if (isChangeRow)
                data.TargetGraph.Refresh();

        }
        /// <summary>
        /// Metoda, která obdrží odpovědi z aplikační funkce, a podle nich zajistí patřičné změny v tabulkách.
        /// </summary>
        /// <param name="response"></param>
        protected void ItemDragDropDropAppResponse(AppHostResponseArgs response)
        { }
        /// <summary>
        /// Metoda vrátí instanci <see cref="DragSchedulerData"/> obsahující data na úrovni Scheduleru z dat Drag and Drop z úrovně GUI.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected DragSchedulerData PrepareDragSchedulerData(ItemDragDropArgs args)
        {
            DragSchedulerData data = new DragSchedulerData();
            data.DragGroupGId = this.GetGId(args.Group.GroupId);
            data.DragGroupItems = args.Group.Items.Where(i => i is DataGraphItem).Cast<DataGraphItem>().ToArray();
            data.SourceGraph = args.ParentGraph;
            data.SourceRow = this.GetGraphRowGid(args.ParentGraph);
            data.SourceTime = args.Group.Time;
            data.SourceBounds = args.OriginalAbsoluteBounds;
            data.TargetGraph = args.TargetGraph;
            data.TargetRow = this.GetGraphRowGid(args.TargetGraph);

            // Umístění cíle, časová/místní příchylnost k původní hodnotě:
            Rectangle sourceBounds = data.SourceBounds;
            Rectangle targetBounds = args.DragToAbsoluteBounds.Value;
            int distX = (targetBounds.X - sourceBounds.X);
            int absDX = ((distX < 0) ? -distX : distX);
            bool isChangeTime = true;
            if (!data.IsChangeRow)
            {   // Ve stejném řádku:
                if (absDX < 5) isChangeTime = false;
                targetBounds.Y = sourceBounds.Y;
            }
            else
            {   // V jiném řádku:
                if (absDX < 15) isChangeTime = false;
                
            }

            // Odvodit cílový čas nebo korigovat cílovou souřadnici:
            if (isChangeTime)
            {   // Pokud je reálně požadována změna času:
                DateTime? begin = args.GetTimeForPosition(targetBounds.X);
                data.TargetTime = TimeRange.CreateFromBeginSize(begin.Value, data.SourceTime.Size.Value);
            }
            else
            {   // Není tu změna času:
                data.TargetTime = data.SourceTime.Clone;
                targetBounds.X = sourceBounds.X;
            }
            data.TargetBounds = targetBounds;

            return data;
        }
        /// <summary>
        /// Metoda z dat v <see cref="DragSchedulerData"/> (interní data Scheduleru) 
        /// vytvoří a vrátí new instanci třídy <see cref="GuiRequestGraphItemMove"/> (externí data, která se předávají do aplikační logiky).
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected GuiRequestGraphItemMove PrepareRequestGraphItemMove(DragSchedulerData data)
        {
            GuiRequestGraphItemMove guiData = new GuiRequestGraphItemMove();
            guiData.MoveItems = data.DragGroupItems.Select(i => this.GetGridItemId(i)).ToArray();
            guiData.SourceTime = data.SourceTime;
            guiData.TargetRow = data.TargetRow;
            guiData.TargetTime = data.TargetTime;
            return guiData;
        }
        /// <summary>
        /// Analyzovaná data na úrovni Scheduleru, pro akce při přemísťování prvku na úrovni GUI
        /// </summary>
        protected class DragSchedulerData
        {
            /// <summary>
            /// GId grupy, která se přemisťuje.
            /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
            /// </summary>
            public GId DragGroupGId { get; set; }
            /// <summary>
            /// Jednotlivé prvky grupy, které jsou její součástí a mají se přemístit.
            /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
            /// </summary>
            public DataGraphItem[] DragGroupItems { get; set; }
            /// <summary>
            /// Graf, v němž byl umístěn prvek na začátku.
            /// Může být tentýž, jako cílový (<see cref="TargetGraph"/>).
            /// </summary>
            public GTimeGraph SourceGraph { get; set; }
            /// <summary>
            /// Řádek, na němž byl umístěn prvek na začátku.
            /// Může být tentýž, jako cílový (<see cref="TargetRow"/>).
            /// </summary>
            public GId SourceRow { get; set; }
            /// <summary>
            /// Graf, kam má být prvek přemístěn.
            /// </summary>
            public GTimeGraph TargetGraph { get; set; }
            /// <summary>
            /// Cílový řádek, kam má být prvek přemístěn.
            /// </summary>
            public GId TargetRow { get; set; }
            /// <summary>
            /// Původní čas prvku před přemístěním
            /// </summary>
            public TimeRange SourceTime { get; set; }
            /// <summary>
            /// Cílový čas prvku po přemístění
            /// </summary>
            public TimeRange TargetTime { get; set; }
            /// <summary>
            /// Absolutní souřadnice prvku před přemístěním
            /// </summary>
            public Rectangle SourceBounds { get; set; }
            /// <summary>
            /// Absolutní souřadnice prvku po přemístění
            /// </summary>
            public Rectangle TargetBounds { get; set; }
            /// <summary>
            /// Obsahuje true, pokud dochází ke změně řádku
            /// </summary>
            public bool IsChangeRow { get { return (this.SourceRow != null && this.TargetRow != null && this.SourceRow != this.TargetRow); } }
            /// <summary>
            /// Obsahuje true, pokud dochází ke změně času
            /// </summary>
            public bool IsChangeTime { get { return (this.SourceTime != null && this.TargetTime != null && this.SourceTime != this.TargetTime); } }
            /// <summary>
            /// Posun času: target = source + ShiftTime.Value; ale pokud <see cref="IsChangeTime"/> je false, pak zde je null.
            /// </summary>
            public TimeSpan? ShiftTime { get { return (this.IsChangeTime ? (TimeSpan?)(this.TargetTime.Begin.Value - this.SourceTime.Begin.Value) : (TimeSpan?)null); } }
        }
        #endregion
        #region Implementace ITimeGraphDataSource: Zdroj dat pro grafy: tvorba textu, tooltipu, kontextové menu, podpora Drag and Drop
        /// <summary>
        /// Připraví text pro položku grafu
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareText(CreateTextArgs args)
        {
            DataGraphItem graphItem = this.GetActiveGraphItem(args);
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
            DataGraphItem graphItem = this.GetActiveGraphItem(args);
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
            GuiGridItemId gridItemId = this.GetGridItemId(args);
            return this.IMainData.CreateContextMenu(gridItemId);
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném prvku grafu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForItem(ItemActionArgs args)
        {
            GuiGridItemId gridItemId = this.GetGridItemId(args);
            return this.IMainData.CreateContextMenu(gridItemId);
        }
        /// <summary>
        /// Uživatel dal doubleclick na grafický prvek
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemDoubleClick(ItemActionArgs args)
        {
            if (args.ModifierKeys == Keys.Control)
            {   // Akce typu Ctrl+DoubleClick na grafickém prvku si žádá otevření formuláře:
                DataGraphItem graphItem = this.GetActiveGraphItem(args);
                if (graphItem != null)
                    this.RunOpenRecordForm(graphItem.RecordGId);
            }
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném interaktivním argumentu.
        /// </summary>
        /// <param name="args">Interaktivní argument</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(ItemActionArgs args)
        {
            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = this.GuiGrid.FullName;            // Konstantní jméno FullName this tabulky (třída GuiGrid)
            gridItemId.RowId = this.GetGraphRowGid(args.Graph);      // Z grafu najdu jeho řádek a jeho GId řádku, ten se (implicitně) převede na GuiId
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde prvek odpovídající args.CurrentItem, nebo args.GroupedItems[0]
            if (graphItem != null)
            {   // Pokud mám prvek, pak do resultu vložím jeho GId (převedené na GuiId):
                gridItemId.ItemId = graphItem.ItemGId;
                gridItemId.GroupId = graphItem.GroupGId;
                gridItemId.DataId = graphItem.DataGId;
            }
            return gridItemId;
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném prvku grafu.
        /// </summary>
        /// <param name="graphItem">Prvek grafu</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(DataGraphItem graphItem)
        {
            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = this.GuiGrid.FullName;            // Konstantní jméno FullName this tabulky (třída GuiGrid)
            if (graphItem != null)
            {   // Pokud mám prvek, pak do resultu vložím jeho GId (převedené na GuiId):
                gridItemId.RowId = graphItem.ParentGId;              // Parentem je GID řádku
                gridItemId.ItemId = graphItem.ItemGId;
                gridItemId.GroupId = graphItem.GroupGId;
                gridItemId.DataId = graphItem.DataGId;
            }
            return gridItemId;
        }
        /// <summary>
        /// Metoda najde a vrátí grafický prvek zdejší třídy <see cref="DataGraphItem"/> pro daný interaktivní prvek, 
        /// uvedený v interaktivním argumentu <see cref="ItemArgs"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected DataGraphItem GetActiveGraphItem(ItemArgs args)
        {
            int itemId = (args.CurrentItem != null ? args.CurrentItem.ItemId : (args.GroupedItems.Length > 0 ? args.GroupedItems[0].ItemId : 0));
            if (itemId <= 0) return null;
            return this.GetGraphItem(itemId);
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
                    // Tady by se mělo řešit umístění (targetBounds) na ose Y, abych prvek přetahoval přiměřeně:
                    this.ItemDragDropMove(args);
                    break;
                case DragActionType.DragThisDrop:
                    // Tady by se měla volat metoda AppHost => aplikační funkce pro přepočet grafu:
                    this.ItemDragDropDrop(args);
                    break;
                case DragActionType.DragThisEnd:
                    // 
                    args.ParentGraph.Refresh();
                    break;
            }
        }
        void ITimeGraphDataSource.CreateText(CreateTextArgs args) { this.GraphItemPrepareText(args); }
        void ITimeGraphDataSource.CreateToolTip(CreateToolTipArgs args) { this.GraphItemPrepareToolTip(args); }
        void ITimeGraphDataSource.GraphRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForGraph(args); }
        void ITimeGraphDataSource.ItemRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForItem(args); }
        void ITimeGraphDataSource.ItemDoubleClick(ItemActionArgs args) { this.GraphItemDoubleClick(args); }
        void ITimeGraphDataSource.ItemLongClick(ItemActionArgs args) { }
        void ITimeGraphDataSource.ItemChange(ItemChangeArgs args) { }
        void ITimeGraphDataSource.ItemDragDropAction(ItemDragDropArgs args) { this.ItemDragDropAction(args); }
        #endregion
        #region Implementace IDataGraphTableInternal: Přístup k vnitřním datům tabulky
        int IMainDataTableInternal.GetId(GId gId) { return this.GetId(gId); }
        GId IMainDataTableInternal.GetGId(int id) { return this.GetGId(id); }
        DataGraphItem IMainDataTableInternal.GetGraphItem(int id) { return this.GetGraphItem(id); }
        DataGraphItem IMainDataTableInternal.GetGraphItem(GId gId) { return this.GetGraphItem(gId); }
        #endregion
    }
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
            if (guiGraphItem == null) return null;
            IMainDataTableInternal iGraphTable = graphTable as IMainDataTableInternal;

            DataGraphItem item = new DataGraphItem(graphTable, guiGraphItem);
            // Struktura řádku: parent_rec_id int; parent_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; back_color string; join_back_color string; data string
            item._ItemGId = guiGraphItem.ItemId;           // Mezi typy GuiId (=Green) a GId (GraphLibrary) existuje implicitní konverze.
            item._ParentGId = guiGraphItem.ParentRowId;    //  Takže do zdejších properties se vytvoří new instance GUid, obsahující stejná data jako vstupní GuiId.
            item._GroupGId = guiGraphItem.GroupId;         //  Další důsledek je ten, že zdejší data lze změnit = přemístit na jiný řádek, například.
            item._DataGId = guiGraphItem.DataId;
            item._Time = guiGraphItem.Time;                // Existuje implicitní konverze mezi typy TimeRange a GuiTimeRange.
            // ID pro grafickou vrstvu: vygenerujeme Int32 klíč pro daný GId, za pomoci indexu uloženého v hlavní tabulce (iGraphTable):
            item._ItemId = iGraphTable.GetId(item.ItemGId);
            item._GroupId = iGraphTable.GetId(item.GroupGId);

            // Ostatní property jsou načítané přímo z item._GuiGraphItem.

            return item;
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
        private GId _ParentGId;
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
        /// Vizuální control
        /// </summary>
        private GTimeGraphItem _GControl;
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
        /// Veřejný identifikátor MAJITELE PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o Kapacitní plánovací jednotku.
        /// </summary>
        public GId ParentGId { get { return this._ParentGId; } set { this._ParentGId = value; } }
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
        /// Veřejný identifikátor ZÁZNAMU K OTEVŘENÍ: obsahuje číslo třídy a číslo záznamu.
        /// Jako <see cref="RecordGId"/> se vrací nejvhodnější identifikátor, který má být otevřen po provedení Ctrl + DoubleClick na tomto prvku.
        /// Pokud je zadán <see cref="DataGId"/>, vrací se ten. Jako další se může vrátit <see cref="ItemGId"/> anebo <see cref="GroupGId"/>; v tomto pořadí.
        /// Ale nevrací se <see cref="ParentGId"/> (to je řádek, nikoli prvek).
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
        #endregion
        #region Explicitní implementace rozhraní ITimeGraphItem
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.GroupId { get { return this._GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this._Time; } set { this._Time = value; } }
        int ITimeGraphItem.Layer { get { return this._GuiGraphItem.Layer; } }
        int ITimeGraphItem.Level { get { return this._GuiGraphItem.Level; } }
        int ITimeGraphItem.Order { get { return this._GuiGraphItem.Order; } }
        float ITimeGraphItem.Height { get { return this._GuiGraphItem.Height; } }
        string ITimeGraphItem.Text { get { return this._GuiGraphItem.Text; } }
        string ITimeGraphItem.ToolTip { get { return this._GuiGraphItem.ToolTip; } }
        Color? ITimeGraphItem.BackColor { get { return this._GuiGraphItem.BackColor; } }
        Color? ITimeGraphItem.LineColor { get { return this._GuiGraphItem.LineColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._GuiGraphItem.BackStyle; } }
        float? ITimeGraphItem.RatioBegin { get { return this._GuiGraphItem.RatioBegin; } }
        float? ITimeGraphItem.RatioEnd { get { return this._GuiGraphItem.RatioEnd; } }
        Color? ITimeGraphItem.RatioBackColor { get { return this._GuiGraphItem.RatioBackColor; } }
        Color? ITimeGraphItem.RatioLineColor { get { return this._GuiGraphItem.RatioLineColor; } }
        int? ITimeGraphItem.RatioLineWidth { get { return this._GuiGraphItem.RatioLineWidth; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this._GuiGraphItem.BehaviorMode; } }
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
        /// Vytvoří a vrátí instanci DataGraphProperties, vloží do ní dodaná data.
        /// </summary>
        /// <param name="dataGraphTable">Vlastník = tabulka</param>
        /// <param name="guiGraphProperties">Definice vlastností grafu</param>
        /// <returns></returns>
        public static DataGraphProperties CreateFrom(MainDataTable dataGraphTable, GuiGraphProperties guiGraphProperties)
        {
            DataGraphProperties dataGraphProperties = new DataGraphProperties(dataGraphTable, guiGraphProperties);
            return dataGraphProperties;
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        /// <param name="dataGraphTable">Vlastník = tabulka</param>
        /// <param name="guiGraphProperties">Definice vlastností grafu</param>
        private DataGraphProperties(MainDataTable dataGraphTable, GuiGraphProperties guiGraphProperties)
        {
            this.MainDataTable = dataGraphTable;
            this.GuiGraphProperties = guiGraphProperties;
        }
        /// <summary>
        /// Vlastník = tabulka
        /// </summary>
        protected MainDataTable MainDataTable { get; private set; }
        /// <summary>
        /// Vlastnosti grafu načtené z deklarace v <see cref="GuiData"/>
        /// </summary>
        protected GuiGraphProperties GuiGraphProperties { get; private set; }
        #endregion
        #region Public data
        /// <summary>
        /// Režim zobrazování času na ose X
        /// </summary>
        public TimeGraphTimeAxisMode TimeAxisMode { get { return this.GuiGraphProperties.TimeAxisMode; } }
        /// <summary>
        /// Režim chování při změně velikosti: zachovat měřítko a změnit hodnotu End, nebo zachovat hodnotu End a změnit měřítko?
        /// </summary>
        public AxisResizeContentMode AxisResizeMode { get { return this.GuiGraphProperties.AxisResizeMode; } }
        /// <summary>
        /// Možnosti uživatele změnit zobrazený rozsah anebo měřítko
        /// </summary>
        public AxisInteractiveChangeMode InteractiveChangeMode { get { return this.GuiGraphProperties.InteractiveChangeMode; } }
        /// <summary>
        /// Pozice grafu v tabulce
        /// </summary>
        public DataGraphPositionType GraphPosition { get { return this.GuiGraphProperties.GraphPosition; } }
        /// <summary>
        /// Výška jednotky v grafu, v pixelech
        /// </summary>
        public int GraphLineHeight { get { return this.GuiGraphProperties.GraphLineHeight; } }
        /// <summary>
        /// Výška řádku v tabulce minimální, v pixelech
        /// </summary>
        public int TableRowHeightMin { get { return this.GuiGraphProperties.TableRowHeightMin; } }
        /// <summary>
        /// Výška řádku v tabulce maximální, v pixelech
        /// </summary>
        public int TableRowHeightMax { get { return this.GuiGraphProperties.TableRowHeightMax; } }
        /// <summary>
        /// Logaritmická časová osa: Rozsah lineární části grafu uprostřed logaritmické časové osy.
        /// Implicitní hodnota (pokud není zadáno jinak) = 0.60f, povolené rozmezí od 0.40f po 0.90f.
        /// </summary>
        public float? LogarithmicRatio { get { return this.GuiGraphProperties.LogarithmicRatio; } }
        /// <summary>
        /// Logaritmická časová osa: vykreslovat vystínování oblastí s logaritmickým měřítkem osy (tedy ty levé a pravé okraje, kde již neplatí lineární měřítko).
        /// Zde se zadává hodnota 0 až 1, která reprezentuje úroven vystínování těchto okrajů.
        /// Hodnota 0 = žádné stínování, hodnota 1 = krajní pixel je zcela černý. 
        /// Implicitní hodnota (pokud není zadáno jinak) = 0.20f.
        /// </summary>
        public float? LogarithmicGraphDrawOuterShadow { get { return this.GuiGraphProperties.LogarithmicGraphDrawOuterShadow; } }
        /// <summary>
        /// Průhlednost prvků grafu při běžném vykreslování.
        /// Má hodnotu null (průhlednost se neaplikuje), nebo 0 ÷ 255. 
        /// Hodnota 255 má stejný význam jako null = plně viditelný graf. 
        /// Hodnota 0 = zcela neviditelné prvky (ale fyzicky jsou přítomné).
        /// Výchozí hodnota = null.
        /// </summary>
        public int? Opacity { get { return this.GuiGraphProperties.Opacity; } }
        #endregion
    }
    #endregion
}
