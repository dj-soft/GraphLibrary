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
    /// MainDataTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, grafy, položky grafů, vztahy mezi položkami grafů a popisky položek grafů.
    /// Tvoří základ pro jeden vizuální objekt <see cref="GTable"/>.
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
            this.LoadDataGraphProperties();
            this.LoadDataPrepareIndex();
            this.LoadDataLoadRow();
            this.LoadDataCreateGraphs();
            this.LoadDataLoadGraphItems();
            this.LoadDataLoadLinks();
            this.LoadDataLoadTexts();
        }
        #endregion
        #region Zpracování odpovědi z GuiResponseGraph: aktualizace vlastností grafů, přidání nových prvků do grafů, odebrání prvků grafu
        /// <summary>
        /// Aktualizuje obsah daného grafu (podle jeho řádky).
        /// </summary>
        /// <param name="updateGraph"></param>
        /// <param name="refreshGraphDict"></param>
        public void UpdateGraph(GuiResponseGraph updateGraph, Dictionary<uint, GTimeGraph> refreshGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._UpdateGraph(updateGraph);
            _RefreshModifiedGraph(modifiedGraph, refreshGraphDict);
        }
        /// <summary>
        /// Metoda z dodaného prvku <see cref="GuiResponseGraph"/> aktualizuje data odpovídajícího grafu <see cref="DataGraphItem"/>.
        /// Vrací referenci na zmíněný modifikovaný graf.
        /// </summary>
        /// <param name="updateGraph"></param>
        /// <returns></returns>
        private GTimeGraph _UpdateGraph(GuiResponseGraph updateGraph)
        {
            GTimeGraph modifiedGraph = null;

            if (updateGraph == null) return modifiedGraph;

            GId rowGId = updateGraph.RowId;
            GTimeGraph gTimeGraph;
            if (this.TimeGraphDict.TryGetValue(rowGId, out gTimeGraph))
            {
                gTimeGraph.UpdateGraphData(updateGraph);

                if (updateGraph.ResetGraphItems)
                    this._RemoveItemsFromGraph(gTimeGraph, updateGraph.RemoveItems);

                this._AddGraphItems(gTimeGraph, updateGraph.GraphItems);

                modifiedGraph = gTimeGraph;
            }
            return modifiedGraph;
        }
        #region Přidání grafických prvků
        /// <summary>
        /// Přidá daný prvek jako nový do odpovídajícího grafu (podle jeho řádky).
        /// </summary>
        /// <param name="addItem"></param>
        /// <param name="refreshGraphDict"></param>
        public void AddGraphItem(GuiGraphBaseItem addItem, Dictionary<uint, GTimeGraph> refreshGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._AddGraphItem(addItem);
            _RefreshModifiedGraph(modifiedGraph, refreshGraphDict);
        }
        /// <summary>
        /// Metoda z dodaného prvku <see cref="GuiGraphBaseItem"/> vytvoří prvek grafu <see cref="DataGraphItem"/>, 
        /// prvek uloží do Dictionary <see cref="TimeGraphItemDict"/>,
        /// podle jeho řádku <see cref="DataGraphItem.RowGId"/> najde v Dictionary <see cref="TimeGraphDict"/> graf, a do něj vloží grafický prvek.
        /// Vrací referenci na zmíněný modifikovaný graf.
        /// </summary>
        /// <param name="addItem"></param>
        /// <returns></returns>
        private GTimeGraph _AddGraphItem(GuiGraphBaseItem addItem)
        {
            GTimeGraph modifiedGraph = null;

            if (addItem == null) return modifiedGraph;

            GId rowGId = addItem.RowId;
            GTimeGraph gTimeGraph;
            if (this.TimeGraphDict.TryGetValue(rowGId, out gTimeGraph))
            {
                if (this._AddGraphItem(gTimeGraph, addItem))
                    modifiedGraph = gTimeGraph;
            }
            return modifiedGraph;
        }
        /// <summary>
        /// Metoda zajistí vytvoření řady prvků grafu (třída <see cref="DataGraphItem"/>) z dat o prvku (třída <see cref="GuiGraphBaseItem"/>),
        /// dále pak přidání vytvořených prvků <see cref="DataGraphItem"/> do dodaného grafu, i do zdejší Dictionary <see cref="TimeGraphItemDict"/> a do <see cref="TimeGraphGroupDict"/>.
        /// Vrací true = došlo k přidání / false = nebyla změna.
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="addItems"></param>
        /// <returns></returns>
        private bool _AddGraphItems(GTimeGraph gTimeGraph, IEnumerable<GuiGraphBaseItem> addItems)
        {
            bool isChange = false;
            if (gTimeGraph == null || addItems == null) return isChange;

            foreach (GuiGraphBaseItem addItem in addItems)
            {
                bool oneChange = this._AddGraphItem(gTimeGraph, addItem);
                if (!isChange && oneChange)
                    isChange = true;
            }

            return isChange;
        }
        /// <summary>
        /// Metoda zajistí vytvoření prvku grafu (třída <see cref="DataGraphItem"/>) z dat o prvku (třída <see cref="GuiGraphBaseItem"/>),
        /// dále pak přidání prvku <see cref="DataGraphItem"/> do dodaného grafu, i do zdejší Dictionary <see cref="TimeGraphItemDict"/> a <see cref="TimeGraphGroupDict"/>.
        /// Vrací true = došlo k přidání / false = nebyla změna.
        /// </summary>
        /// <param name="gTimeGraph"></param>
        /// <param name="addItem"></param>
        /// <returns></returns>
        private bool _AddGraphItem(GTimeGraph gTimeGraph, GuiGraphBaseItem addItem)
        {
            bool isChange = false;
            if (gTimeGraph == null || addItem == null || addItem.ItemId == null) return isChange;

            DataGraphItem dataGraphItem = DataGraphItem.CreateFrom(this, addItem);
            if (dataGraphItem == null) return false;

            isChange = gTimeGraph.AddGraphItem(dataGraphItem);
            if (isChange)
            {
                if (!this.TimeGraphItemDict.ContainsKey(dataGraphItem.ItemGId))
                    this.TimeGraphItemDict.Add(dataGraphItem.ItemGId, dataGraphItem);
                if (dataGraphItem.GroupGId != null)
                    this.TimeGraphGroupDict.Add(dataGraphItem);
            }
            
            return isChange;
        }
        #endregion
        #region Odebrání grafických prvků
        /// <summary>
        /// Odebere dané prvky z grafů v patřičné řádce v this tabulce.
        /// </summary>
        /// <param name="removeItems"></param>
        /// <param name="refreshGraphDict"></param>
        public void RemoveGraphItems(IEnumerable<GuiGridItemId> removeItems, Dictionary<uint, GTimeGraph> refreshGraphDict = null)
        {
            foreach (GuiGridItemId removeItem in removeItems)
                this._RemoveGraphItem(removeItem, refreshGraphDict);
        }
        /// <summary>
        /// Odebere daný prvek z grafu v patřičné řádce v this tabulce.
        /// </summary>
        /// <param name="removeItem"></param>
        /// <param name="refreshGraphDict"></param>
        public void RemoveGraphItem(GuiGridItemId removeItem, Dictionary<uint, GTimeGraph> refreshGraphDict = null)
        {
            this._RemoveGraphItem(removeItem, refreshGraphDict);
        }
        /// <summary>
        /// Odebere daný prvek z grafu v patřičné řádce v this tabulce.
        /// </summary>
        /// <param name="removeItem"></param>
        /// <param name="refreshGraphDict"></param>
        private void _RemoveGraphItem(GuiGridItemId removeItem, Dictionary<uint, GTimeGraph> refreshGraphDict)
        {
            GTimeGraph modifiedGraph = this._RemoveGraphItem(removeItem);
            _RefreshModifiedGraph(modifiedGraph, refreshGraphDict);
        }
        /// <summary>
        /// Metoda najde daný prvek v this tabulce a odebere jej.
        /// Najde odpovídající graf k tomuto prvku, a z tohoto grafu tento prvek odebere.
        /// Modifikovaný graf vrátí.
        /// Pokud nebylo co modifikovat, vrací null.
        /// </summary>
        /// <param name="removeItem"></param>
        /// <returns></returns>
        private GTimeGraph _RemoveGraphItem(GuiGridItemId removeItem)
        {
            if (removeItem == null || removeItem.ItemId == null) return null;

            // Najdu prvek:
            GId itemGId = removeItem.ItemId;
            DataGraphItem dataGraphItem;
            this.TimeGraphItemDict.TryGetValue(itemGId, out dataGraphItem);

            // Najdu graf:
            GId rowGId = (dataGraphItem != null ? dataGraphItem.RowGId : (GId)removeItem.RowId);
            GTimeGraph graph;
            this.TimeGraphDict.TryGetValue(rowGId, out graph);

            // Odeberu a skončím:
            bool isRemoved = this._RemoveItemFromGraph(graph, itemGId, true);
            return (isRemoved ? graph : null);
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
            bool isChange = false;
            if (gTimeGraph == null || removeItems == null) return isChange;

            foreach (GuiGridItemId removeItem in removeItems)
            {
                bool oneChange = this._RemoveItemFromGraph(gTimeGraph, removeItem);
                if (!isChange && oneChange)
                    isChange = true;
            }

            return isChange;
        }
        /// <summary>
        /// Metoda najde daný prvek v daném grafu a odebere jej jak z grafu, tak z Dictionary <see cref="TimeGraphItemDict"/>.
        /// Pokud prvek neexistuje v daném grafu, pak nedojde k chybě, ale prvek se neodebere (ani z grafu ani z Dictionary).
        /// Vrací true = došlo ke změně / false = nikoli.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="removeItem"></param>
        /// <returns></returns>
        private bool _RemoveItemFromGraph(GTimeGraph graph, GuiGridItemId removeItem)
        {
            if (graph == null || removeItem == null || removeItem.ItemId == null) return false;
            return this._RemoveItemFromGraph(graph, removeItem.ItemId, false);
        }
        /// <summary>
        /// Metoda odstraní daný prvek z grafu. Vrací true = obsah grafu je změněn.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="itemGId"></param>
        /// <param name="forceRemove">Požadavek true = odebrat prvek z místních Dictionary i tehdy, když prvek není obsažen v grafu (nebo když graf je null)</param>
        /// <returns></returns>
        private bool _RemoveItemFromGraph(GTimeGraph graph, GId itemGId, bool forceRemove)
        {
            if (itemGId == null) return false;
            bool isRemoved = ((graph != null) ? graph.RemoveGraphItem(this.GetId(itemGId), true) : false);
            if (isRemoved || forceRemove)
            {
                DataGraphItem item;
                if (this.TimeGraphItemDict.TryGetValue(itemGId, out item))
                {
                    this.TimeGraphItemDict.Remove(itemGId);
                    if (item.GroupGId != null)
                        this.TimeGraphGroupDict.Remove(item);
                }
            }
            return isRemoved;
        }
        #endregion
        /// <summary>
        /// Zajistí provedení Refresh() na modifikovaném grafu (parametr):
        /// Buď je zadána Dictionary s grafy pro hromadný Refresh, pak aktuální graf do ní přidá;
        /// Anebo není Dictionary zadána, a pak provede Refresh na grafu ihned.
        /// Pokud je na vstupu graf = null, pak nic neřeší (to je situace, kdy graf nebyl modifikován).
        /// </summary>
        /// <param name="modifiedGraph"></param>
        /// <param name="refreshGraphDict"></param>
        private static void _RefreshModifiedGraph(GTimeGraph modifiedGraph, Dictionary<uint, GTimeGraph> refreshGraphDict)
        {
            if (modifiedGraph != null)
            {
                if (refreshGraphDict != null)
                {
                    if (!refreshGraphDict.ContainsKey(modifiedGraph.Id))
                        refreshGraphDict.Add(modifiedGraph.Id, modifiedGraph);
                }
                else
                {
                    modifiedGraph.Refresh();
                }
            }
        }
        #endregion
        #region Vlastnosti grafů a další property
        /// <summary>
        /// Načte vlastnosti grafů z <see cref="GuiGraphProperties"/> do <see cref="DataGraphProperties"/>.
        /// </summary>
        protected void LoadDataGraphProperties()
        {
            this.DataGraphProperties = DataGraphProperties.CreateFrom(this, this.GuiGrid.GraphProperties);
            this.TableName = this.GuiGrid.FullName;
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
        #region TableRow + TagItems
        /// <summary>
        /// Načte tabulku s řádky <see cref="TableRow"/>: sloupce, řádky, filtr
        /// </summary>
        protected void LoadDataLoadRow()
        {
            var tagItems = this.CreateTagArray();
            this.TableRow = Table.CreateFrom(this.GuiGrid.Rows.DataTable, tagItems);
            this.TableRow.OpenRecordForm += _TableRow_OpenRecordForm;
            this.TableRow.UserData = this;
            if (this.TableRow.AllowPrimaryKey) this.TableRow.HasPrimaryIndex = true;
        }
        /// <summary>
        /// Tabulka s řádky.
        /// Tato tabulka je zobrazována.
        /// </summary>
        public Table TableRow { get; private set; }
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
                this.FillGTableEventHandlers();
                return this.GTableRow;
            }
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
        /// Metoda zajistí navázání eventhandlerů v this třídě do grafické komponenty <see cref="GTable"/> <see cref="GTableRow"/>.
        /// </summary>
        protected void FillGTableEventHandlers()
        {
            Table table = this.TableRow;
            table.ActiveRowChanged += _TableRowActiveRowChanged;
            table.CheckedRowChanged += _TableRowCheckedRowChanged;
            // table.LostFocus +=

        }
        /// <summary>
        /// Eventhandler události "Byl aktivován řádek"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRowActiveRowChanged(object sender, GPropertyChangeArgs<Row> e)
        {
            // Aktivace řádku: pokud v tabulce existují označené řádky (CheckedRows), pak prostý pohyb aktivního řádku nic neznamená:
            Row[] checkedRows = this.TableRow.CheckedRows;
            if (checkedRows.Length > 0) return;

            this.InteractionThisSourceActiveRows(new Row[] { e.NewValue });
        }
        /// <summary>
        /// Eventhandler události "Byl označen řádek"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRowCheckedRowChanged(object sender, GObjectPropertyChangeArgs<Row, bool> e)
        {
            Row[] checkedRows = this.TableRow.CheckedRows;
            this.InteractionThisSourceActiveRows(checkedRows);
        }
        /// <summary>
        /// Grafická komponenta reprezentující data z <see cref="TableRow"/>.
        /// </summary>
        protected GTable GTableRow { get; private set; }
        /// <summary>
        /// Metoda vrátí pole štítků <see cref="TagItem"/>, načtených z <see cref="GuiGrid"/>.
        /// </summary>
        /// <returns></returns>
        protected KeyValuePair<GId, TagItem>[] CreateTagArray()
        {
            List<GuiTagItem> tagItemList = this.GuiGrid?.Rows?.RowTags?.TagItemList;
            return CreateTagArray(tagItemList);
        }
        /// <summary>
        /// Metoda vrátí pole štítků <see cref="TagItem"/>, načtených z <see cref="GuiGrid"/>.
        /// </summary>
        /// <returns></returns>
        protected static KeyValuePair<GId, TagItem>[] CreateTagArray(IEnumerable<GuiTagItem> guiTagItems)
        {
            if (guiTagItems == null) return null;
            return guiTagItems
                .Select(gti => CreateTagItem(gti))
                .ToArray();
        }
        /// <summary>
        /// Metoda vrátí párový údaj KeyValuePair, který obsahuje v Key = ID řádku, a ve Value = data štítku <see cref="TagItem"/>.
        /// </summary>
        /// <param name="guiTagItem"></param>
        /// <returns></returns>
        protected static KeyValuePair<GId, TagItem> CreateTagItem(GuiTagItem guiTagItem)
        {
            TagItem tagItem = new TagItem()
            {
                Text = guiTagItem.TagText,
                BackColor = guiTagItem.BackColor,
                CheckedBackColor = guiTagItem.BackColorChecked,
                BorderColor = null,
                TextColor = null,
                Size = null,
                Visible = true,
                Checked = false,
                UserData = guiTagItem.UserData
            };
            GId rowGId = guiTagItem.RowId;
            return new KeyValuePair<GId, TagItem>(rowGId, tagItem);
        }
        #endregion
        #region Podpora pro mezitabulkové interakce (kdy akce v jedné tabulce vyvolá jinou akci v jiné tabulce)
        /// <summary>
        /// Interakce mezi tabulkami, kde this tabulka je zdrojem interakce = zde došlo k akci Aktivace řádků:
        /// podle definic v this tabulce máme provést nějaké akce v cílových tabulkách.
        /// </summary>
        /// <param name="activeRows"></param>
        protected void InteractionThisSourceActiveRows(Row[] activeRows)
        {
            // Z aktivních řádků (parametr) si z jejcih grafů na pozadí načteme všechny jednotlivé prvky grafů do společné Dictionary:
            Dictionary<GId, DataGraphItem> graphItemDict = new Dictionary<GId, DataGraphItem>();
            foreach (Row row in activeRows)
            {
                if (row.BackgroundValueType != TableValueType.ITimeInteractiveGraph) continue;
                GTimeGraph gTimeGraph = row.BackgroundValue as GTimeGraph;
                if (gTimeGraph == null) continue;
                foreach (ITimeGraphItem iItem in gTimeGraph.GraphItems)
                {
                    DataGraphItem item = iItem as DataGraphItem;
                    if (item == null) continue;
                    if (!graphItemDict.ContainsKey(item.ItemGId))
                        graphItemDict.Add(item.ItemGId, item);
                }
            }

            // Najdeme cílovou tabulku, a v ní provedeme Selectování grafických prvků odpovídající Grupám, které mají shodné ID (GroupId) jako naše ItemId:
            MainDataTable targetTable = this.IMainData.SearchTable(@"Data\pages\MainPage\mainPanel\GridCenter");
            if (targetTable != null)
                targetTable.InteractionThisTargetSelectGraphItemsGroups(graphItemDict.Keys, false);
        }
        /// <summary>
        /// Interakce mezi tabulkami, this tabulka je cílem interakce = zde máme provést Selectování položek grafů:
        /// </summary>
        /// <param name="groupIds"></param>
        /// <param name="leaveSelect"></param>
        protected void InteractionThisTargetSelectGraphItemsGroups(IEnumerable<GId> groupIds, bool leaveSelect)
        {
            if (!leaveSelect) this.MainControl.Selector.ClearSelected();

            foreach (GId groupId in groupIds)
            {
                DataGraphItem[] group;
                if (!this.TimeGraphGroupDict.TryGetValue(groupId, out group)) continue;
                (group[0] as ITimeGraphItem).GControl.Group.GControl.IsSelected = true;
            }
        }
        #endregion
        #region Grafy a položky grafů
        /// <summary>
        /// Do tabulky s řádky vytvoří grafy do všech řádků, zatím prázdné
        /// </summary>
        protected void LoadDataCreateGraphs()
        {
            this.TimeGraphDict = new Dictionary<GId, GTimeGraph>();
            this.TimeGraphItemDict = new Dictionary<GId, DataGraphItem>();
            this.TimeGraphGroupDict = new DictionaryList<GId, DataGraphItem>(g => g.GroupGId);

            if (this.TableRow == null) return;
            DataGraphPositionType graphPosition = this.GraphPosition;
            if (graphPosition == DataGraphPositionType.None) return;
            this.LoadDataPrepareTableForGraphs(graphPosition);

            Dictionary<GId, GuiGraph> guiGraphDict = this.LoadDataLoadGuiGraphDict();
            foreach (Row row in this.TableRow.Rows)
            {
                GId rowGid = row.RecordGId;
                if (rowGid == null) continue;

                GTimeGraph gTimeGraph = this.LoadDataCreateOneGTimeGraph(row, graphPosition, guiGraphDict);
                if (!this.TimeGraphDict.ContainsKey(rowGid))
                    this.TimeGraphDict.Add(rowGid, gTimeGraph);
            }
        }
        /// <summary>
        /// Metoda připraví tabulku <see cref="TableRow"/> na vkládání grafů daného typu (podle zadané pozice).
        /// Tzn. v případě, kdy pozice je <see cref="DataGraphPositionType.InLastColumn"/>, tak bude vytvořen a patřičně nastaven nový sloupec pro graf 
        /// (reference na sloupec je uložena do <see cref="TableRowGraphColumn"/>),
        /// a do vhodného umístění je vložena instance vlastností grafu <see cref="TimeGraphProperties"/>.
        /// </summary>
        /// <param name="graphPosition"></param>
        protected void LoadDataPrepareTableForGraphs(DataGraphPositionType graphPosition)
        {
            bool isGraphInColumn = (graphPosition == DataGraphPositionType.InLastColumn);
            TimeGraphProperties graphProperties = this.DataGraphProperties.CreateTimeGraphProperties(isGraphInColumn, this.MainControl.SynchronizedTime.Value, this.MainData.GuiData.Properties.TotalTimeRange);
            if (isGraphInColumn)
            {
                Column graphColumn = new Column("__time__graph__");

                graphColumn.ColumnProperties.AllowColumnResize = true;
                graphColumn.ColumnProperties.AllowColumnSortByClick = false;
                graphColumn.ColumnProperties.AutoWidth = true;
                graphColumn.ColumnProperties.ColumnContent = ColumnContentType.TimeGraphSynchronized;
                graphColumn.ColumnProperties.IsVisible = true;
                graphColumn.ColumnProperties.WidthMininum = 250;
                graphColumn.GraphParameters = graphProperties;

                this.TableRow.Columns.Add(graphColumn);
                this.TableRowGraphColumn = graphColumn;
            }
            else
            {
                this.TableRow.GraphParameters = graphProperties;
            }
        }
        /// <summary>
        /// Metoda vytvoří Dictionary, obsahující předpřipravená data grafů z GUI pro jednotlivé řádky.
        /// GUI data mohou/nemusí obsahovat data jednotlivých grafů, v <see cref="GuiGrid.Graphs"/>.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<GId, GuiGraph> LoadDataLoadGuiGraphDict()
        {
            Dictionary<GId, GuiGraph> guiGraphDict = new Dictionary<GId, GuiGraph>();
            List<GuiGraph> graphs = this.GuiGrid.Graphs;
            if (graphs != null && graphs.Count > 0)
                guiGraphDict = graphs
                    .Where(g => g.RowId != null)
                    .GetDictionary(g => { GId rowGId = g.RowId; return rowGId; }, true);
            return guiGraphDict;
        }
        /// <summary>
        /// Metoda vytvoří nový <see cref="GTimeGraph"/> pro daný řádek a pozici, umístí jej do řádku, a graf vrátí.
        /// Graf zatím neobsahuje položky.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="graphPosition"></param>
        /// <param name="guiGraphDict">Data grafů načtená z GUI</param>
        /// <returns></returns>
        protected GTimeGraph LoadDataCreateOneGTimeGraph(Row row, DataGraphPositionType graphPosition, Dictionary<GId, GuiGraph> guiGraphDict)
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

            // Naplníme do grafu data dodaná z GUI vrstvy, pokud nějaká data dodaná byla:
            GuiGraph guiGraph;
            if (guiGraphDict != null && guiGraphDict.TryGetValue(row.RecordGId, out guiGraph))
            {
                gTimeGraph.UpdateGraphData(guiGraph);
                this._AddGraphItems(gTimeGraph, guiGraph.GraphItems);
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

            if (guiGrid.GraphItems != null)
            {
                foreach (GuiGraphTable guiGraphTable in guiGrid.GraphItems)
                {
                    if (guiGraphTable == null || guiGraphTable.Count == 0) continue;
                    foreach (GuiGraphItem guiGraphItem in guiGraphTable.GraphItems)
                        this._AddGraphItem(guiGraphItem);
                }
            }
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
            GTimeGraphItem graphItem = item as GTimeGraphItem;
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
        /// DictionaryList pro uchování informací o grupách grafických prvků.
        /// Klíčem je tedy Int32 GroupId (pokud prvek má zadanou grupu), a hodnotou je seznam všech prvků v dané grupě.
        /// </summary>
        protected DictionaryList<GId, DataGraphItem> TimeGraphGroupDict { get; private set; }
        /// <summary>
        /// Sloupec hlavní tabulky, který zobrazuje graf při umístění <see cref="DataGraphPositionType.InLastColumn"/>
        /// </summary>
        protected Column TableRowGraphColumn { get; private set; }
        #endregion
        #region Linky mezi položkami grafů
        /// <summary>
        /// Metoda načte a předzpracuje informace o vztazích mezi prvky grafů (Linky)
        /// </summary>
        protected void LoadDataLoadLinks()
        {
            this.GraphLinkPrevDict = new DictionaryList<int, GTimeGraphLinkItem>();
            this.GraphLinkNextDict = new DictionaryList<int, GTimeGraphLinkItem>();
            if (this.GuiGrid.GraphLinks != null && this.GuiGrid.GraphLinks.Count > 0)
                this.AddGraphLinks(this.GuiGrid.GraphLinks.LinkList);
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

            // Odeberu stávající záznamy (z GraphLinkDict), které mají shodné klíče Prev a Next s těmiu, které se budou zanedlouho přidávat:
            if (removeItems)
                this.RemoveGraphLinks(links.Select(l => new Tuple<int, int>(l.ItemIdPrev, l.ItemIdNext)));       // Tady předávám refeenci na IEnumerable, které ještě reálně není enumerováno!!!  Úmyslně! 

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
            if (this.GraphLinkPrevDict.CountKeys == 0 && this.GraphLinkNextDict.CountKeys == 0) return;

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
        /// Metoda najde a vrátí soupis platných vztahů mezi prvky pro jeden daný prvek grafu.
        /// Pokud daný prvek nemá žádný vztah, vrací se null.
        /// Pokud prvek má vztahy, pak tyto vztahy mají korektně vepsané reference na vztažené prvky grafů
        /// </summary>
        /// <param name="currentItem">Výchozí prvek pro hledání vztahů; může to být prvek grupy i prvek jednotlivý</param>
        /// <param name="searchSidePrev">Hledej linky na straně Prev</param>
        /// <param name="searchSideNext">Hledej linky na straně Next</param>
        /// <param name="wholeTask">Hledej linky pro celý Task</param>
        /// <param name="asSCurve">Nastav linky "Jako S křivky"</param>
        /// <returns></returns>
        protected GTimeGraphLinkItem[] SearchForGraphLink(GTimeGraphItem currentItem, bool searchSidePrev, bool searchSideNext, bool wholeTask, bool? asSCurve)
        {
            Dictionary<uint, GTimeGraphItem> itemDict = new Dictionary<uint, GTimeGraphItem>();
            Dictionary<ulong, GTimeGraphLinkItem> linkDict = new Dictionary<ulong, GTimeGraphLinkItem>();
            if (currentItem != null)
            {
                if (searchSidePrev && this.GraphLinkNextDict.CountKeys > 0) this._SearchForGraphLink(currentItem, this.GraphLinkNextDict, Direction.Negative, itemDict, linkDict, wholeTask, asSCurve);
                if (searchSideNext && this.GraphLinkPrevDict.CountKeys > 0) this._SearchForGraphLink(currentItem, this.GraphLinkPrevDict, Direction.Positive, itemDict, linkDict, wholeTask, asSCurve);
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
        /// <param name="asSCurve">Nastav linky "Jako S křivky"</param>
        /// <returns></returns>
        private void _SearchForGraphLink(GTimeGraphItem currentItem, DictionaryList<int, GTimeGraphLinkItem> graphLinkDict, Direction targetSide,
            Dictionary<uint, GTimeGraphItem> scanItemDict, Dictionary<ulong, GTimeGraphLinkItem> resultLinkDict, bool wholeTask, bool? asSCurve)
        {
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

                    // Najdeme zdrojový i cílový prvek vztahu:
                    int targetId = link.GetId(targetSide);
                    GTimeGraphItem targetItem = this._SearchGraphItemsForLink(targetId, position);
                    if (targetItem == null) continue;                // Vztah nemá nalezen prvek na cílové straně vztahu; vztah přeskočíme.
                    int sourceId = link.GetId(sourceSide);           // Na source straně vztahu nemusí být nutně prvek, který jsme hledali - může tam být jeho grupa! (anebo naopak)
                    GTimeGraphItem sourceItem = this._SearchGraphItemsForLink(sourceId, position);
                    link.SetItem(sourceSide, sourceItem);            // Prvek na zdrojové straně vztahu (buď ten, kde hledání začalo, anebo odpovídající prvek = jeho Grupa, pro kterou máme vztahy!
                    link.SetItem(targetSide, targetItem);            // Prvek na cílové straně vztahu
                    _AdjustLinkType(link, asSCurve);                 // Zajistí prohození typu linku podle konfigurace (proměnná asSCurve)
                    resultLinkDict.Add(link.Key, link);

                    // Podle podmínek zajistíme provedení rekurze = hledání dalších vztahů z cílového prvku tohoto vztahu:
                    if (wholeTask && link.GuiGraphLink != null && link.GuiGraphLink.RelationType.HasValue && link.GuiGraphLink.RelationType.Value == GuiGraphItemLinkRelation.OneLevel && !scanItemDict.ContainsKey(targetItem.Id))
                    {   // Daný cílový prvek si zařadíme do fronty práce, a v některém z dalších cyklů v této metodě jej zpracujeme:
                        searchQueue.Enqueue(targetItem);
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
        /// Upraví typ linku podle proměnné
        /// </summary>
        /// <param name="link"></param>
        /// <param name="asSCurve"></param>
        private void _AdjustLinkType(GTimeGraphLinkItem link, bool? asSCurve)
        {
            if (link != null && link.LinkType.HasValue && asSCurve.HasValue)
            {
                if (link.LinkType.Value == GuiGraphItemLinkType.PrevEndToNextBeginLine && asSCurve.Value)
                    link.LinkType = GuiGraphItemLinkType.PrevEndToNextBeginSCurve;
                else if (link.LinkType.Value == GuiGraphItemLinkType.PrevEndToNextBeginSCurve && !asSCurve.Value)
                    link.LinkType = GuiGraphItemLinkType.PrevEndToNextBeginLine;
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
            if (baseItem != null && graphLinkDict != null && graphLinkDict.CountKeys > 0)
            {
                int id;
                GTimeGraphLinkItem[] links;

                position = GGraphControlPosition.Group;
                id = baseItem.Group.GroupId;
                links = graphLinkDict[id];
                if (links != null) return links;

                position = GGraphControlPosition.Item;
                id = baseItem.Item.ItemId;
                links = graphLinkDict[id];
                if (links != null) return links;
            }
            position = GGraphControlPosition.None;
            return null;
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
            if (iGraphItem == null) return null;
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
            GTimeGraphLinkItem graphLink = new GTimeGraphLinkItem()
            {
                ItemIdPrev = this.GetId(guiGraphLink.ItemIdPrev),
                ItemIdNext = this.GetId(guiGraphLink.ItemIdNext),
                LinkType = guiGraphLink.LinkType,
                LinkWidth = guiGraphLink.LinkWidth,
                LinkColorStandard = guiGraphLink.LinkColorStandard,
                LinkColorWarning = guiGraphLink.LinkColorWarning,
                LinkColorError = guiGraphLink.LinkColorError,
                GuiGraphLink = guiGraphLink
            };
            return graphLink;
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
            return this.GetTableInfoRow(graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.RowGId);
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
            GraphItemDragMoveInfo moveInfo = this.PrepareDragSchedulerMoveInfo(args);
            args.DragToAbsoluteBounds = moveInfo.TargetBounds;
            args.ToolTipData.AnimationType = TooltipAnimationType.Instant;
            args.ToolTipData.TitleText = (moveInfo.IsChangeRow ? "Přemístění na jiný řádek" : "Přemístění v rámci řádku");
            args.ToolTipData.InfoText = "Čas: " + moveInfo.TargetTime.ToString();
        }
        /// <summary>
        /// Scheduler vyvolá aplikační logiku, která určí definitivní umístění prvku v procesu Drag and Drop,
        /// v akci Drop = prvek byl vizuálně umístěn.
        /// </summary>
        /// <param name="args"></param>
        protected void ItemDragDropDrop(ItemDragDropArgs args)
        {
            // Tady by se měla volat metoda AppHost => aplikační funkce pro přepočet grafu:
            GraphItemDragMoveInfo moveInfo = this.PrepareDragSchedulerMoveInfo(args);

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
                this.IMainData.CallAppHostFunction(request, this.ItemDragDropDropAppResponse);
            }
        }
        /// <summary>
        /// Metoda provede vizuální přemístění prvků grafu na požadovanou cílovou pozici, na základě GUI dat.
        /// </summary>
        /// <param name="moveInfo"></param>
        protected void ItemDragDropDropGuiResponse(GraphItemDragMoveInfo moveInfo)
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
            GuiResponse guiResponse = response.GuiResponse;

            this.IMainData.ProcessResponse(guiResponse);
        }
        /// <summary>
        /// Metoda vrátí instanci <see cref="GraphItemDragMoveInfo"/> obsahující data na úrovni Scheduleru z dat Drag and Drop z úrovně GUI.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected GraphItemDragMoveInfo PrepareDragSchedulerMoveInfo(ItemDragDropArgs args)
        {
            // Základní data bez modifikací:
            ITimeGraphItem item = ((args.Item != null) ? args.Item : args.GroupedItems[0]);
            TimeRange sourceTime = args.Group.Time;
            Rectangle sourceBounds = args.OriginalAbsoluteBounds;
            Rectangle targetBounds = args.DragToAbsoluteBounds.Value;
            DateTime? targetTimeBegin = args.GetTimeForPosition(targetBounds.X);
            TimeRange targetTime = TimeRange.CreateFromBeginSize(targetTimeBegin.Value, sourceTime.Size.Value);

            GraphItemDragMoveInfo moveInfo = new GraphItemDragMoveInfo();
            moveInfo.DragItemId = item.ItemId;
            moveInfo.DragGroupId = item.GroupId;
            moveInfo.DragLevel = item.Level;
            moveInfo.DragLayer = item.Layer;
            moveInfo.DragGroupGId = this.GetGId(moveInfo.DragGroupId);
            moveInfo.DragGroupItems = args.Group.Items.Where(i => i is DataGraphItem).Cast<DataGraphItem>().ToArray();
            moveInfo.DragAction = args.DragAction;
            moveInfo.SourceMousePoint = args.ActionPoint;
            moveInfo.SourceGraph = args.ParentGraph;
            moveInfo.SourceRow = this.GetGraphRowGid(args.ParentGraph);
            moveInfo.SourceTime = sourceTime;
            moveInfo.SourceBounds = sourceBounds;
            moveInfo.AttachSide = RangeSide.Begin;
            moveInfo.TargetGraph = args.TargetGraph;
            moveInfo.TargetRow = this.GetGraphRowGid(args.TargetGraph);
            moveInfo.TargetTime = targetTime;
            moveInfo.TargetBounds = targetBounds;
            moveInfo.GetTimeForPosition = args.GetTimeForPosition;
            moveInfo.GetPositionForTime = args.GetPositionForTime;
            moveInfo.GetRoundedTime = args.GetRoundedTime;

            // Modifikace dat pomocí magnetů:
            this.IMainData.AdjustGraphItemDragMove(moveInfo);

            return moveInfo;
        }
        /// <summary>
        /// Metoda z dat v <see cref="GraphItemDragMoveInfo"/> (interní data Scheduleru) 
        /// vytvoří a vrátí new instanci třídy <see cref="GuiRequestGraphItemMove"/> (externí data, která se předávají do aplikační logiky).
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        protected GuiRequestGraphItemMove PrepareRequestGraphItemMove(GraphItemDragMoveInfo moveInfo)
        {
            GuiRequestGraphItemMove guiData = new GuiRequestGraphItemMove();
            guiData.MoveItems = moveInfo.DragGroupItems.Select(i => this.GetGridItemId(i)).ToArray();
            guiData.SourceRow = moveInfo.SourceRow;
            guiData.SourceTime = moveInfo.SourceTime;
            guiData.MoveFixedPoint = GetGuiSide(moveInfo.AttachSide);
            guiData.TargetRow = moveInfo.TargetRow;
            guiData.TargetTime = moveInfo.TargetTime;
            return guiData;
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
        #region Implementace ITimeGraphDataSource: Zdroj dat pro grafy: tvorba textu, tooltipu, kontextové menu, podpora Drag and Drop
        /// <summary>
        /// Připraví text pro položku grafu
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareText(CreateTextArgs args)
        {
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
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
            DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
            if (graphItem == null) return;

            Row infoRow = this.GetTableInfoRow(graphItem);
            if (infoRow == null) return;

            GId recordGId = infoRow.RecordGId;
            args.ToolTipData.TitleText = (recordGId != null ? recordGId.ClassName : "INFORMACE O POLOŽCE");

            StringBuilder sb = new StringBuilder();
            sb.Append(args.TimeText);
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
        /// Najde vztahy pro daný prvek
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemCreateLinks(CreateLinksArgs args)
        {
            bool wholeTask = (args.ItemEvent == CreateLinksItemEventType.MouseOver && this.Config != null && this.Config.GuiEditShowLinkWholeTask);
            bool asSCurve = (this.Config != null && this.Config.GuiEditShowLinkAsSCurve);

            GTimeGraphItem currentItem = args.ItemControl ?? args.GroupControl;     // Na tomto prvku začne hledání. Může to být prvek konkrétní, anebo prvek grupy.
            args.Links = this.SearchForGraphLink(currentItem, args.SearchSidePrev, args.SearchSideNext, wholeTask, asSCurve);
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
                DataGraphItem graphItem = this.GetActiveGraphItem(args); // Najde datový prvek grafu odpovídající buď konkrétnímu prvku, nebo najde první prvek grupy
                if (graphItem != null)
                    this.RunOpenRecordForm(graphItem.RecordGId);
            }
        }
        /// <summary>
        /// Metoda vytvoří, naplní a vrátí identifikátor prvku <see cref="GuiGridItemId"/>, podle údajů v daném prvku grafu.
        /// </summary>
        /// <param name="graphItem">Prvek grafu</param>
        /// <returns></returns>
        private GuiGridItemId GetGridItemId(DataGraphItem graphItem)
        {
            GuiGridItemId gridItemId = new GuiGridItemId();
            gridItemId.TableName = this.TableName;                   // Konstantní jméno FullName this tabulky (třída GuiGrid)
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
            int itemId = (args.Item ?? args.GroupedItems[0]).ItemId;
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
        void ITimeGraphDataSource.CreateLinks(CreateLinksArgs args) { this.GraphItemCreateLinks(args); }
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
        public static DataGraphItem CreateFrom(MainDataTable graphTable, GuiGraphBaseItem guiGraphItem)
        {
            if (guiGraphItem == null) return null;
            IMainDataTableInternal iGraphTable = graphTable as IMainDataTableInternal;

            DataGraphItem item = new DataGraphItem(graphTable, guiGraphItem);
            // Struktura řádku: parent_rec_id int; parent_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; back_color string; join_back_color string; data string
            item._ItemGId = guiGraphItem.ItemId;           // Mezi typy GuiId (=Green) a GId (GraphLibrary) existuje implicitní konverze.
            item._RowGId = guiGraphItem.RowId;             //  Takže do zdejších properties se vytvoří new instance GUid, obsahující stejná data jako vstupní GuiId.
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
        /// privátní konstruktor. Instanci lze založit pomocí metody <see cref="CreateFrom(MainDataTable, GuiGraphBaseItem)"/>.
        /// </summary>
        private DataGraphItem(MainDataTable graphTable, GuiGraphBaseItem guiGraphItem)
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
        private GuiGraphBaseItem _GuiGraphItem;
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
        public GuiGraphBaseItem GuiGraphItem { get { return this._GuiGraphItem; } }
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
        Color? ITimeGraphItem.RatioBeginBackColor { get { return this._GuiGraphItem.RatioBeginBackColor; } }
        Color? ITimeGraphItem.RatioEndBackColor { get { return this._GuiGraphItem.RatioEndBackColor; } }
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
        /// <param name="guiGraphProperties">Definice vlastností grafu</param>
        /// <returns></returns>
        public static DataGraphProperties CreateFrom(MainDataTable dataGraphTable, GuiGraphProperties guiGraphProperties)
        {
            return new DataGraphProperties(dataGraphTable, guiGraphProperties);
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
}
