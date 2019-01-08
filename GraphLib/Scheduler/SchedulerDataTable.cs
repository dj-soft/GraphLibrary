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
            this.LoadDataLoadParentChild();
            this.LoadDataLoadLinks();
            this.LoadDataLoadTexts();
            this.LoadDataLoadToolTips();
        }
        /// <summary>
        /// Metoda zajistí přípravu dat této tabulky poté, kdy projdou přípravou všechny tabulky systému.
        /// V této metodě se tedy this tabulka může datové základny dotazovat i na jiné tabulky.
        /// V rámci konstruktoru a při načítání dat to není možné.
        /// </summary>
        internal void PrepareAfterLoad()
        {
            this.LoadDataSearchChildRows();
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

                this._UpdateGraphItems(gTimeGraph, updateGraph.GraphItems);

                modifiedGraph = gTimeGraph;
            }
            return modifiedGraph;
        }
        #region Přidání / Aktualizace grafických prvků
        /// <summary>
        /// Přidá daný prvek jako nový do odpovídajícího grafu (podle jeho řádky).
        /// </summary>
        /// <param name="updateItem"></param>
        /// <param name="refreshGraphDict"></param>
        public void UpdateGraphItem(GuiGraphBaseItem updateItem, Dictionary<uint, GTimeGraph> refreshGraphDict = null)
        {
            GTimeGraph modifiedGraph = this._UpdateGraphItem(updateItem);
            _RefreshModifiedGraph(modifiedGraph, refreshGraphDict);
        }
        /// <summary>
        /// Metoda z dodaného prvku <see cref="GuiGraphBaseItem"/> vytvoří prvek grafu <see cref="DataGraphItem"/>, 
        /// prvek uloží do Dictionary <see cref="TimeGraphItemDict"/>,
        /// podle jeho řádku <see cref="DataGraphItem.RowGId"/> najde v Dictionary <see cref="TimeGraphDict"/> graf, a do něj vloží grafický prvek.
        /// Vrací referenci na zmíněný modifikovaný graf.
        /// </summary>
        /// <param name="updateItem"></param>
        /// <returns></returns>
        private GTimeGraph _UpdateGraphItem(GuiGraphBaseItem updateItem)
        {
            GTimeGraph modifiedGraph = null;

            if (updateItem == null) return modifiedGraph;

            GId rowGId = updateItem.RowId;
            GTimeGraph gTimeGraph;
            if (this.TimeGraphDict.TryGetValue(rowGId, out gTimeGraph))
            {
                if (this._UpdateGraphItem(gTimeGraph, updateItem))
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
        /// <param name="updateItems"></param>
        /// <returns></returns>
        private bool _UpdateGraphItems(GTimeGraph gTimeGraph, IEnumerable<GuiGraphBaseItem> updateItems)
        {
            bool isChange = false;
            if (gTimeGraph == null || updateItems == null) return isChange;

            foreach (GuiGraphBaseItem updateItem in updateItems)
            {
                bool oneChange = this._UpdateGraphItem(gTimeGraph, updateItem);
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
        /// <param name="updateItem"></param>
        /// <returns></returns>
        private bool _UpdateGraphItem(GTimeGraph gTimeGraph, GuiGraphBaseItem updateItem)
        {
            bool isChange = false;
            if (gTimeGraph == null || updateItem == null || updateItem.ItemId == null) return isChange;

            DataGraphItem dataGraphItem;

            // Pokud pro daný datový prvek už máme grafický prvek, pak jej aktualizujeme:
            int itemId = this.GetId(updateItem.ItemId);    // Najde/Vytvoří a vrátí Int32 klíč prvku. To že v případě neexistence se vygeneruje nové Id nám nevadí, protože by se tak jako tak generovalo o něco později.
            ITimeGraphItem graphItem;
            if (gTimeGraph.TryGetGraphItem(itemId, out graphItem))
            {
                dataGraphItem = graphItem as DataGraphItem;
                if (dataGraphItem != null)
                {
                    isChange = dataGraphItem.UpdateFrom(updateItem);
                    return isChange;                       // Pokud došlo ke změně obsahu prvku grafu, vracím true => později (hromadně) se provede refresh grafu gTimeGraph.
                }
            }

            // Pokud jej nemáme, pak jej vložíme jako nový:
            dataGraphItem = DataGraphItem.CreateFrom(this, updateItem);
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
        /// Aktuální synchronizovaný časový interval
        /// </summary>
        protected TimeRange SynchronizedTime { get { return this.IMainData.SynchronizedTime; } set { this.IMainData.SynchronizedTime = value; } }
        /// <summary>
        /// Celkový časový interval <see cref="GuiProperties.TotalTimeRange"/>
        /// </summary>
        protected TimeRange TotalTime { get { return this.IMainData?.GuiData?.Properties?.TotalTimeRange; } }
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
        /// <summary>
        /// Obsahuje true, pokud tato tabulka má aktuálně focus
        /// </summary>
        public bool HasFocus { get { return this.GTableRow.HasFocus; } }
        #endregion
        #region TableRow + TagItems
        /// <summary>
        /// Načte tabulku s řádky <see cref="TableRow"/>: sloupce, řádky, filtr
        /// </summary>
        protected void LoadDataLoadRow()
        {
            var tagItems = this.CreateTagArray();
            this.TableRow = Table.CreateFrom(this.GuiGrid.Rows.DataTable, tagItems);
            this.TableRow.CalculateBoundsForAllRows = true;
            this.TableRow.OpenRecordForm += _TableRow_OpenRecordForm;
            this.TableRow.UserData = this;
            if (this.TableRow.AllowPrimaryKey) this.TableRow.HasPrimaryIndex = true;
            this._CurrentSearchChildInfo = SearchChildInfo.CreateForProperties(this.GuiGrid.GridProperties);
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
            GuiGridInteraction[] interactions = this.GetInteractions(sourceAction);
            if (interactions == null) return;
            this.InteractionThisSource(interactions, activeRow, checkedRows, null, null);
        }
        /// <summary>
        /// Eventhandler události "Byl označen řádek"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRowCheckedRowChanged(object sender, GObjectPropertyChangeArgs<Row, bool> e)
        {
            GuiGridInteraction[] interactions = this.GetInteractions(SourceActionType.TableRowChecked);
            if (interactions == null) return;
            Row checkedRow = e.CurrentObject;
            Row[] checkedRows = this.TableRow.CheckedRows;
            this.InteractionThisSource(interactions, checkedRow, checkedRows, null, null);
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
                this.PrepareCurrentChildRows(e.NewValue, false);
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
        #region ParentChilds - Vztahy mezi řádky
        /// <summary>
        /// Metoda připraví Childs řádky pro řádky Root, podle aktuálně viditelného času.
        /// Volá se z metod, které změní obsah grafů v této tabulce, po provedení všech změn.
        /// </summary>
        public void PrepareCurrentChildRows()
        {
            this.PrepareCurrentChildRows(this.SynchronizedTime, true);
        }
        /// <summary>
        /// Metoda načte a zpracuje data o vztazích Parent-Childs.
        /// Volá se jedenkrát při načítání řádků.
        /// Zatím není potřeba tato data donačítat později.
        /// </summary>
        protected void LoadDataLoadParentChild()
        {
            List<GuiParentChild> guiParentChilds = this.GuiGrid.ParentChilds;
            if (guiParentChilds == null) return;

            // Nejprve si z prostých identifikátorů párů { Klíč Parent :: Klíč Child } složím datové komplety:
            //  a) Dictionary, kde Key: Child; a kde Value = Tuple, jehož Item1 = řádek Child, a Item2 = pole řádků Parent:
            Dictionary<GuiId, Tuple<Row, List<Row>>> childDataDict = new Dictionary<GuiId, Tuple<Row, List<Row>>>();
            //  b) Dictionary, kde Key: Parent; a Value = List všech jemu podřízených řádků Childs
            Dictionary<GuiId, List<Row>> parentDict = new Dictionary<GuiId, List<Row>>();

            Row parentRow, childRow;
            foreach (GuiParentChild guiParentChild in guiParentChilds)
            {
                // Child řádek musíme mít vždy, bez něj to nemá smysl:
                if (guiParentChild.Child == null || guiParentChild.Child.IsEmpty) continue;
                if (!this.TableRow.TryFindRow(guiParentChild.Child, out childRow, false)) continue;

                // Parent řádek být může a nemusí:
                bool hasParentRow = this.TableRow.TryFindRow(guiParentChild.Parent, out parentRow, false);

                // a1) Zajistím evidenci Child řádků:
                var childData = childDataDict.GetAdd(guiParentChild.Child, key => new Tuple<Row, List<Row>>(childRow, new List<Row>()));
                // a2) Zajistím přidání Parenta (pokud existuje) do řádku v (childDataDict) k danému Child řádku (childData):
                if (hasParentRow)
                    childData.Item2.Add(parentRow);

                // b) Zajistím přidání Child řádku do seznamu (parentDict) do řádků Parenta, kde Parent může být Empty:
                GuiId parentKey = (hasParentRow ? guiParentChild.Parent : GuiId.Empty);
                parentDict
                    .GetAdd(parentKey, key => new List<Row>())
                    .Add(childRow);
            }

            // Poté zpracuji řádky Childs: jejich soupis Parentů:
            foreach (Tuple<Row, List<Row>> childData in childDataDict.Values)
            {
                childRow = childData.Item1;
                childRow.ParentChildMode = RowParentChildMode.DynamicChild;
                childRow.TreeNodeParents = childData.Item2.ToArray();
            }

            // Uložím si Dictionary parentů, bude se používat pro dohledání Child prvků konkrétního Parenta:
            this.TreeNodeChildDict = parentDict;
        }
        /// <summary>
        /// Metoda se pokusí najít Child řádky pro aktuální časový interval
        /// </summary>
        protected void LoadDataSearchChildRows()
        {
            this.PrepareCurrentChildRows(this.MainData.GuiData.Properties?.InitialTimeRange, true);
        }
        /// <summary>
        /// Metoda připraví Childs řádky pro řádky Root, podle zadaného viditelného času
        /// </summary>
        /// <param name="timeRange">Viditelný časový interval</param>
        /// <param name="force">Povinně, bez ohledu na to že pro daný čas už bylo provedeno (=když máme nová data v grafech)</param>
        protected void PrepareCurrentChildRows(TimeRange timeRange, bool force)
        {
            // Test, zda je nutno akci provádět, s ohledem na režim, zadaný čas a posledně zpracovaný čas:
            SearchChildInfo searchInfo = this.CurrentSearchChildInfo;
            TimeRange timeFrame = (searchInfo.IsVisibleTimeOnly ? timeRange : this.TotalTime);
            if (!PrepareCurrentChildRowsRequired(force, searchInfo, timeFrame, this._ChildRowsLastTimeRange)) return;

            // Určuji vztahy Parent - Child, podle daného režimu, pro tyto naše Root řádky:
            Row[] rootRows = this.TableRow.TreeNodeRootRows;

            // Existují dva režimy: a) hledat ve vlastních řádcích, b) hledat v jiné tabulce
            if (!searchInfo.IsInOtherTable)
            {   // a) Hledat Childs řádky ve vlastních řádcích:
                if (this.TreeNodeChildDict == null || this.TreeNodeChildDict.Count == 0) return;   // Pokud nemám definované vlastní Child řádky, skončím.
                Dictionary<GId, Row> visibleRowDict = new Dictionary<GId, Row>();                  // Toto jsou Child řádky, které jsou aktuálně viditelné (jejich Parent je Expanded a řádky jsou viditelné)
                foreach (Row rootRow in rootRows)
                    this.PrepareCurrentChildRowsFor(rootRow, this, searchInfo, timeFrame, visibleRowDict);   // Pro daný root najdu odpovídající Childs
            }
            else
            {   // Hledat Child řádky v cizí tabulce:
                MainDataTable otherTable = null;
                if (!String.IsNullOrEmpty(searchInfo.ChildRowsTableName))
                    otherTable = this.IMainData.SearchTable(searchInfo.ChildRowsTableName);        // Vyhledám zdrojovu tabulku s Child řádky (anebo skončím)
                if (otherTable == null) return;
                // Jako Child řádky z other tabulky vezmu buď pouze Root řádky, anebo všechny:
                Row[] otherRows = (searchInfo.IsInOtherRootRowsOnly ? otherTable.TableRow.TreeNodeRootRows : otherTable.TableRow.Rows.ToArray());
                if (otherRows.Length > 0)
                {
                    foreach (Row rootRow in rootRows)
                        this.PrepareOtherChildRowsFor(rootRow, searchInfo, timeFrame, otherTable, otherRows);// Komplexní zpracování Child řádků z cizí tabulky
                }
            }

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
        /// Posledně platná hodnota času při detekci v metodě <see cref="PrepareCurrentChildRows(TimeRange, bool)"/>
        /// </summary>
        private TimeRange _ChildRowsLastTimeRange;
        #region Dynamicky dohledané Child řádky z vlastní tabulky
        /// <summary>
        /// Metoda najde a připraví Child řádky pro daný Parent řádek
        /// </summary>
        /// <param name="parentRow"></param>
        /// <param name="dataTable"></param>
        /// <param name="searchInfo">Režim vztahů, určuje parametry hledání</param>
        /// <param name="timeFrame"></param>
        /// <param name="visibleRowDict"></param>
        protected void PrepareCurrentChildRowsFor(Row parentRow, MainDataTable dataTable, SearchChildInfo searchInfo, TimeRange timeFrame, Dictionary<GId, Row> visibleRowDict)
        {
            if (parentRow == null) return;
            parentRow.TreeNodeChilds = null;
            GId recordGId = parentRow.RecordGId;
            if (this.TreeNodeChildDict == null || recordGId == null || recordGId.IsEmpty) return;

            // Najdeme řádky, které pro daného Parenta mohou hrát roli Child prvků:
            Dictionary<GId, Row> childDict = new Dictionary<GId, Row>();
            List<Row> childList;
            GuiId parentGuiId = recordGId;
            if (this.TreeNodeChildDict.TryGetValue(parentGuiId, out childList))
                childDict.AddNewItems(childList, r => r.RecordGId);

            if (!searchInfo.IsStatic)
            {   // Druhá část hledání (pro Parenta = Empty) se provádí jen v Dynamickém režimu:
                parentGuiId = GuiId.Empty;
                if (this.TreeNodeChildDict.TryGetValue(parentGuiId, out childList))
                    childDict.AddNewItems(childList, r => r.RecordGId);
            }

            // Daný řádek (parentRow) nemá žádnou šanci mít nějaké childs:
            if (childDict.Count == 0) return;

            // Pro daný řádek máme několik položek (v childDict), které jsou/mohou být Childs.
            //    Pokud je vztah Dynamický, pak zjistíme konkrétní možnosti vztahů (=z parentRow si načteme směrodatné klíče a jejich časové úseky):
            Dictionary<GId, TimeRange> parentDataDict = this.PrepareCurrentRowGetItems(parentRow, dataTable, searchInfo, searchInfo.ParentIdType, timeFrame);

            // Určíme řádky, které mají společnou práci s Parentem:
            parentRow.TreeNodeChilds = childDict.Values.Where(r => PrepareCurrentChildRowsFilter(parentDataDict, r, dataTable, searchInfo, searchInfo.ChildIdType, timeFrame)).ToArray();

            // Pokud tento řádek je Expanded, a obsahuje nějaký Child z řádků, které jsou Expanded někde jinde,
            //    pak tento řádek zavřeme:
            this.PrepareCurrentChildCollapse(parentRow, visibleRowDict);

            // A rekurzivně i pro řádky v parentRow.TreeNodeChilds, pouze pokud je to Static:
            if (searchInfo.IsStatic)
            {
                foreach (Row childRow in parentRow.TreeNodeChilds)
                    this.PrepareCurrentChildRowsFor(childRow, dataTable, searchInfo, timeFrame, visibleRowDict);
            }
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
        protected Dictionary<GId, TimeRange> PrepareCurrentRowGetItems(Row row, MainDataTable dataTable, SearchChildInfo searchInfo, DataGraphItem.IdType idType, TimeRange timeFrame)
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
        /// Metoda zajistí, že daný Root bude Collapsed, pokud by obsahoval některé z již viditelných Childs.
        /// <para/>
        /// Jinými slovy: tahle metoda zajišťuje následující chování:
        /// * Pokud mám Table s řádky s grafy a s takovými Child řádky, které se vyhodnocují dynamicky (tzn. pod Root řádkem se jako 
        /// Child řádky zobrazují ty, které mají s Root řádkem nějakou společnou práci);
        /// * A pokud pod dvěma různými Root řádky lze teoreticky zobrazit jeden identický Child řádek, což technicky vylučujeme,
        /// takže při Expand nodu A proběhne Collapse nodu B (viz metoda <see cref="Row.TreeNodeExpand()"/>, vyvolávající metodu Row._TreeNodeCollapseOtherParents());
        /// * A v tabulce posunu časovou osu tak, aby některé Root řádky ukazovaly jen několik málo prvků grafu;
        /// * A nyní otevřu více Root prvků, což může jít protože jejich Child řádky jsou vzájemně nekonfliktní;
        /// - a teď to přijde:
        /// * Mám tedy viditelné Root řádky ve stavu Expandend, zobrazující různé Child řádky;
        /// * Začnu posouvat časovou osu tam, kde je více prvků grafů, a k nim se dynamicky dohledají další a další Child řádky;
        /// * A protože Root řádky jsou Expanded, pak Child řádky se přidávají do otevřených nodů - ale neprobíhá tam metoda <see cref="Row.TreeNodeExpand()"/>;
        /// * Takže nejspíš by došlo k tomu, že jeden konkrétní Child řádek by se dostal pod dva (nebo více) Root řádků současně !!! (=chyba)
        /// <para/>
        /// Proto zdejší metoda využívá postupně načítaného soupisu viditelných řádků Child (visibleChildDict), 
        /// a pokud by další řádek chtěl zobrazit některý z již viditelných Child řádků, pak tento Root řádek bude zavřen (Collapse).
        /// </summary>
        /// <param name="rootRow"></param>
        /// <param name="visibleChildDict"></param>
        protected void PrepareCurrentChildCollapse(Row rootRow, Dictionary<GId, Row> visibleChildDict)
        {
            if (!(rootRow.TreeNodeHasChilds && rootRow.TreeNodeIsExpanded)) return;     // Bez Childs anebo Collapsed node: neřeším.

            // Pokud daný rootRow obsahuje nějaké Childs z těch, které už máme zobrazené (visibleRowDict), tak rootRow zavřeme a skončíme:
            bool containsChilds = false;
            if (visibleChildDict.Count > 0)
            {
                rootRow.TreeNodeScan(
                    (row, level) =>
                    {   // Tady vidíme každý prvek, včetně rootu (tam je ale level == 0):
                        if (level > 0 && visibleChildDict.ContainsKey(row.RecordGId))
                            // Zajímá nás, zda node je roven některému z již viditelných:
                            containsChilds = true;
                    },
                    row =>
                    {   // Ptáme se, zda pokračovat ve scanování Child prvků daného node:
                        // Ano pokud je otevřený:
                        return row.TreeNodeIsExpanded;
                    }
                    );
            }
            if (containsChilds)
            {   // Aktuální řádek rootRow je Expanded, a zobrazoval by některý Child, který už je zobrazen jinde => řádek zavřeme a skončíme:
                rootRow.TreeNodeCollapse();
                return;
            }

            // Protože rootRow nám zůstal otevřený, pak to znamená, že obsahuje nekonfliktní ChildNodes;
            //  tak do visibleRowDict je přidáme, abychom příště prověřovali všechny (tj. i nyní přidané) viditelné Childs:
            // Použiju téměř identickou sekvenci jako před chvílí, ale Child nody budu do Dictionary přidávat:
            rootRow.TreeNodeScan(
                (row, level) =>
                {   // Tady vidíme každý prvek, včetně rootu (tam je ale level == 0):
                    if (level > 0 && !visibleChildDict.ContainsKey(row.RecordGId))
                        visibleChildDict.Add(row.RecordGId, row);
                },
                row =>
                {   // Ptáme se, zda pokračovat ve scanování Child prvků daného node:
                    // Ano pokud je otevřený:
                    return row.TreeNodeIsExpanded;
                }
                );
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný řádek má být dostupný jako Child řádek v daném čase
        /// </summary>
        /// <param name="parentDataDict">Prvky grafu v řádku Parent v daném čase, může být null</param>
        /// <param name="childRow">Potenciální child řádek, který testujeme zda bude Child řádkem určitého Parenta</param>
        /// <param name="childTable"></param>
        /// <param name="searchInfo">Režim vztahů, určuje parametry hledání</param>
        /// <param name="idType"></param>
        /// <param name="timeFrame"></param>
        /// <returns></returns>
        protected bool PrepareCurrentChildRowsFilter(Dictionary<GId, TimeRange> parentDataDict, Row childRow, MainDataTable childTable, SearchChildInfo searchInfo, DataGraphItem.IdType idType, TimeRange timeFrame)
        {
            if (searchInfo.IsStatic) return true;
            if (parentDataDict == null || parentDataDict.Count == 0 || timeFrame == null) return false;

            // Najdeme prvky grafu v řádku Child v zadané době (anebo celý řádek Child, podle idType):
            Dictionary<GId, TimeRange> childDataDict = this.PrepareCurrentRowGetItems(childRow, childTable, searchInfo, idType, timeFrame);
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
        /// Dictionary, kde Key = klíč Parent řádků a Value = seznam všech potenciálních Child řádků
        /// </summary>
        protected Dictionary<GuiId, List<Row>> TreeNodeChildDict { get; private set; }
        #endregion
        #region Dynamicky dohledané Child řádky z cizí tabulky
        /// <summary>
        /// Metoda najde a připraví Child řádky pro daný Parent řádek, řádky hledá v dodané cizí tabulce, do daného root řádku ukládá jejich vhodné klony
        /// </summary>
        /// <param name="parentRow">Parent řádek z this tabulky</param>
        /// <param name="searchInfo">Režim vztahů, určuje parametry hledání</param>
        /// <param name="timeFrame">Časový interval</param>
        /// <param name="otherTable">Zdrojová tabulka</param>
        /// <param name="otherRows">Sada řádků z jiné tabulky, mezi nimi mohou být Childs k danému Parentu</param>
        protected void PrepareOtherChildRowsFor(Row parentRow, SearchChildInfo searchInfo, TimeRange timeFrame, MainDataTable otherTable, Row[] otherRows)
        {
            if (parentRow == null) return;
            parentRow.TreeNodeChilds = null;
            GId recordGId = parentRow.RecordGId;

            // Nejprve si připravím Dictionary, obsahující zdrojové vazební prvky z Parent řádku, z this tabulky:
            Dictionary<GId, TimeRange> parentDataDict = this.PrepareCurrentRowGetItems(parentRow, this, searchInfo, searchInfo.ParentIdType, timeFrame);

            // Nyní vyhledám Child řádky, které mají s Parentem něco společného (nějakou aktuálně viditelnou práci):
            //  (tady jde o řádky, pocházející z Other tabulky = před jejich duplikováním)
            Row[] childs = otherRows
                .Where(r => PrepareCurrentChildRowsFilter(parentDataDict, r, otherTable, searchInfo, searchInfo.ChildIdType, timeFrame))
                .ToArray();

            // Pokud není žádný, skončíme:
            if (childs.Length == 0) return;

            // Nyní provedu synchronizaci dat z childs (=ostrá data) do jejich klonu, který bude vložen do parentRow.TreeNodeChilds:
            Dictionary<GId, Row> parentDict = this.OtherChildGetParentDict(recordGId);
            List<Row> currentChilds = new List<Row>();
            foreach (Row sourceChild in childs)
            {   // Child řádky z Other tabulky:
                // Majdu / vytvořím klon řádku, odpovídající řádku Child z other tabulky, ale pokud v něm bude graf, pak prvky grafu klonovat nebudeme:
                Row cloneChild = this.OtherChildGetCloneRow(parentDict, otherTable, sourceChild);
                if (cloneChild != null)
                {   // Do klonu řádku vložím odpovídající prvky grafu.
                    // K tomu malé vysvětlení:
                    //  - řádek v proměnné sourceChild je ze zdrojové (other) tabulky, a obsahuje graf s kompletní a platnou sadou prvků
                    //  - řádek v proměnné cloneChild je vytvořený Clone z řádku sourceChild, jeho trvanlivost je dlouhodobá (od prvního vytvoření do zavření celého okna)
                    //  - řádek v proměnné cloneChild má obsahovat aktuálně platné prvky grafu z grafu v řádku sourceChild
                    //  - graf v řádku cloneChild po vytvoření neobsahuje nic, po přesunu časové osy obsahuje zastaralé údaje (prvky mimo aktuální čas)
                    //  - nyní musíme do řádku cloneChild nasynchronizovat prvky z grafu v řádku sourceChild:
                    this.OtherChildSyncGraphItems(parentDataDict, searchInfo, timeFrame, sourceChild, cloneChild);

                    currentChilds.Add(cloneChild);
                }
            }
            parentRow.TreeNodeChilds = currentChilds.ToArray();
        }
        /// <summary>
        /// Metoda vrací Dictionary, která obsahuje pro daný GId parenta jeho existující klony Child řádků.
        /// Využívá instanční proměnnou <see cref="OtherChildRowDict"/>, kde jsou tyto klony permanentně uloženy.
        /// Pokud pro daného parenta ještě neexistuje Dictionary, bude založena a uložena do paměti.
        /// </summary>
        /// <param name="parentGId"></param>
        /// <returns></returns>
        protected Dictionary<GId, Row> OtherChildGetParentDict(GId parentGId)
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
        protected Row OtherChildGetCloneRow(Dictionary<GId, Row> parentDict, MainDataTable sourceTable, Row sourceRow)
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
            cloneRow.ParentChildMode = RowParentChildMode.Child;
            cloneRow.Control = null;
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
        protected void OtherChildSyncGraphItems(Dictionary<GId, TimeRange> parentDataDict, SearchChildInfo searchInfo, TimeRange timeFrame, Row sourceChild, Row targetChild)
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
                targetData.Time = sourceData.Time;
                targetData.BehaviorMode = (sourceData.BehaviorMode & GraphItemBehaviorMode.AllEnabledForChildRows);
                targetGraph.AddGraphItem(targetData);
                targetItem = targetData;
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
        internal void RunInteractionThisSource(GuiGridInteraction[] interactions, ref bool callRefresh)
        {
            if (interactions == null || interactions.Length == 0) return;
            interactions = this.GetInteractionsForCurrentState(interactions).ToArray();
            if (interactions == null || interactions.Length == 0) return;

            Row activeRow = this.TableRow.ActiveRow;
            Row[] checkedRows = this.TableRow.CheckedRows;
            bool isOnlyActivadedRow = (checkedRows.Length == 0);

            this.InteractionThisSource(interactions, activeRow, checkedRows, null, null);

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
        /// <param name="sourceAction"></param>
        /// <returns></returns>
        protected GuiGridInteraction[] GetInteractions(SourceActionType sourceAction)
        {
            List<GuiGridInteraction> interactionList = this.AllInteractions;
            if (interactionList == null || interactionList.Count == 0) return null;
            GuiGridInteraction[] interactions = interactionList.Where(i => ((int)(i.SourceAction & sourceAction) != 0)).ToArray();

            // Podmíněné interakce = takové, které jsou aktivní pouze za určitého stavu ToolBaru:
            interactions = this.GetInteractionsForCurrentState(interactions).ToArray();

            return (interactions.Length > 0 ? interactions : null);
        }
        /// <summary>
        /// Metoda vrátí interakce platné jen pro aktuální stav dat (=ToolBaru a konfigurace).
        /// Zjistí, zda dané interakce obsahují podmínku, a pokud ano pak ji vyhodnotí.
        /// </summary>
        /// <param name="interactions"></param>
        /// <returns></returns>
        protected IEnumerable<GuiGridInteraction> GetInteractionsForCurrentState(IEnumerable<GuiGridInteraction> interactions)
        {
            if (interactions == null || !interactions.Any(i => i.IsConditional)) return interactions;

            Dictionary<string, GuiToolbarItem> toolBarDict = this.IMainData.GuiData.ToolbarItems.Items
                .Where(t => (t.IsCheckable.HasValue && t.IsCheckable.Value))
                .GetDictionary(t => t.Name, true);

            List<GuiGridInteraction> interactionList = new List<GuiGridInteraction>();
            foreach (GuiGridInteraction interaction in interactions)
            {
                if (IsInteractionForCurrentCondition(interaction, toolBarDict))
                    interactionList.Add(interaction);
            }

            return interactionList.ToArray();
        }
        /// <summary>
        /// Metoda vrací true, pokud se daná interakce má použít za stavu, kdy máme v tolbaru zaškrtnuté některé prvky
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="toolBarDict"></param>
        /// <returns></returns>
        protected bool IsInteractionForCurrentCondition(GuiGridInteraction interaction, Dictionary<string, GuiToolbarItem> toolBarDict)
        {
            if (!interaction.IsConditional || String.IsNullOrEmpty(interaction.Conditions)) return true;        // Bez podmínky = vyhovuje, použije se.

            string[] conditions = interaction.Conditions.Split(";, ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

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
        /// <param name="interactions">Definice interakcí, které se mají provést s danými řádky</param>
        /// <param name="activeRow">Aktivní řádek, kde došlo k akci</param>
        /// <param name="checkedRows">Aktuálně označené řádky v tabulce (Checked)</param>
        /// <param name="activeGraph">Aktivní graf</param>
        /// <param name="graphItems">Aktivní prvky grafů v této tabulce</param>
        protected void InteractionThisSource(GuiGridInteraction[] interactions, Row activeRow, Row[] checkedRows, GTimeGraph activeGraph, DataGraphItem[] graphItems)
        {
            this.InteractionSelectorClear(interactions);
            this.InteractionRowFiltersPrepare(interactions);

            // Zjistíme, zda cílová strana bude vyžadovat znalost prvků grafů na zdrojové straně:
            TargetActionType targetFromSourceGraph = (TargetActionType.SearchSourceItemId | TargetActionType.SearchSourceGroupId | TargetActionType.SearchSourceDataId);
            // Na vstupu máme řadu definic interakcí, projdeme je a provedeme potřebné kroky:
            foreach (GuiGridInteraction interaction in interactions)
            {
                // Pokud interakce nemá definovaný cíl (tabulku) nebo cílovou akci (TargetAction je None), pak tuto interakci přeskočím:
                if (String.IsNullOrEmpty(interaction.TargetGridFullName) || interaction.TargetAction == TargetActionType.None) continue;

                // Najdeme cílovou tabulku, ale pokud neexistuje, pak tuto interakci přeskočím:
                MainDataTable targetTable = this.IMainData.SearchTable(interaction.TargetGridFullName);
                if (targetTable == null) continue;
                
                // Pokud interakce má v cílové akci definovanou nějakou práci se zdrojovými prvky grafů, tak je musíme mít k dispozici:
                if (((interaction.TargetAction & targetFromSourceGraph) != 0) && graphItems == null)
                    graphItems = this.InteractionThisSourceGetGraphItems(activeRow, checkedRows);

                // Pokud interakce má na vstupu reflektovat pouze prvky grafů ve viditelném intervalu, řešíme to zde:
                DataGraphItem[] validGraphItems = this.InteractionThisSourceFilterItems(interaction, graphItems);

                // Odešleme do cílové tabulky požadavek na interakci:
                InteractionArgs args = new InteractionArgs(interaction, activeRow, checkedRows, activeGraph, validGraphItems);
                targetTable.InteractionThisTarget(args);
            }

            this.InteractionRowFiltersActivate(interactions);
        }
        /// <summary>
        /// Metoda vrátí dané prvky grafů: buď všechny, anebo pouze ty, které spadají do aktuálního viditelného času.
        /// Řídí to definice interakce, její <see cref="GuiGridInteraction.TargetAction"/>, hodnota <see cref="TargetActionType.SearchSourceVisibleTime"/>.
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="graphItems"></param>
        /// <returns></returns>
        protected DataGraphItem[] InteractionThisSourceFilterItems(GuiGridInteraction interaction, DataGraphItem[] graphItems)
        {
            if (interaction == null || graphItems == null || graphItems.Length == 0) return graphItems;
            bool onlyVisibleTime = interaction.TargetAction.HasFlag(TargetActionType.SearchSourceVisibleTime);
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
        /// <param name="interactions"></param>
        protected void InteractionSelectorClear(GuiGridInteraction[] interactions)
        {
            if (interactions == null || interactions.Length == 0) return;

            bool clearSelected = interactions.Any(i => (i.TargetAction.HasFlag(TargetActionType.SelectTargetItem) && !i.TargetAction.HasFlag(TargetActionType.LeaveCurrentTarget)));
            if (clearSelected)
                this.MainControl.Selector.ClearSelected();

            bool clearActivated = interactions.Any(i => (i.TargetAction.HasFlag(TargetActionType.ActivateTargetItem) && !i.TargetAction.HasFlag(TargetActionType.LeaveCurrentTarget)));
            if (clearActivated)
                this.MainControl.Selector.ClearActivated();
        }
        /// <summary>
        /// Metoda zjistí, zda některé Target tabulky budou řešit Řádkový filtr, a pokud ano pak jej připraví:
        /// </summary>
        /// <param name="interactions"></param>
        protected void InteractionRowFiltersPrepare(GuiGridInteraction[] interactions)
        {
            if (interactions == null || interactions.Length == 0) return;

            // Získám Dictionary, obsahující Distinct jména tabulek (TargetGridFullName), které jsou Target a kde akce TargetAction obsahuje FilterTargetRows:
            Dictionary<string, string> nameDict = interactions
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
        /// <param name="interactions"></param>
        protected void InteractionRowFiltersActivate(GuiGridInteraction[] interactions)
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
        protected DataGraphItem[] InteractionThisSourceGetGraphItems(Row activeRow, Row[] checkedRows)
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
        protected GTimeGraph InteractionGetGraphFromRow(Row row)
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
            if ((action & TargetActionType.SearchSourceItemId) != 0)
                this.InteractionThisTargetFrom(args, i => i.ItemGId);
            if ((action & TargetActionType.SearchSourceGroupId) != 0)
                this.InteractionThisTargetFrom(args, i => i.GroupGId);
            if ((action & TargetActionType.SearchSourceDataId) != 0)
                this.InteractionThisTargetFrom(args, i => i.DataGId);
            if ((action & TargetActionType.SearchSourceRowId) != 0)
                this.InteractionThisTargetFrom(args, i => i.RowGId);
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
                if (!this.TableRow.TryGetRowOnPrimaryKey(gId, out row)) continue;
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
            /// <param name="interaction">Definice této jedné interakce</param>
            /// <param name="sourceActiveRow">Aktuální řádek ve zdrojové tabulce</param>
            /// <param name="sourceCheckedRows">Označené řádky ve zdrojové tabulce</param>
            /// <param name="activeGraph">Aktivní graf</param>
            /// <param name="sourceGraphItems">Aktivní prky grafů ve zdrojové tabulce (podle typu interakce jde o všechny prvky aktivních řádků, nebo o aktivní prvky v určitém grafu)</param>
            public InteractionArgs(GuiGridInteraction interaction, Row sourceActiveRow, Row[] sourceCheckedRows, GTimeGraph activeGraph, DataGraphItem[] sourceGraphItems)
            {
                this.Interaction = interaction;
                this.SourceActiveRow = sourceActiveRow;
                this.SourceCheckedRows = sourceCheckedRows;
                this.ActiveGraph = activeGraph;
                this.SourceGraphItems = sourceGraphItems;
            }
            /// <summary>
            /// Definice této jedné interakce
            /// </summary>
            public GuiGridInteraction Interaction { get; private set; }
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
                {
                    this.TimeGraphDict.Add(rowGid, gTimeGraph);
                    gTimeGraph.UserData = this;
                }
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
                this._UpdateGraphItems(gTimeGraph, guiGraph.GraphItems);
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
                        this._UpdateGraphItem(guiGraphItem);
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
        /// Reference na koordinační objekt pro kreslení linek všech grafů v této tabulce, třída: <see cref="GTimeGraphLinkItem"/>.
        /// Tento prvek slouží jednotlivým grafům.
        /// </summary>
        public GTimeGraphLinkArray GraphLinkArray { get { return this.GTableRow.GraphLinkArray; } }
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
        #region Textové údaje (popisky grafů) a ToolTipy
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
        /// Načte tabulky s tooltipy
        /// </summary>
        protected void LoadDataLoadToolTips()
        {
            this.TableToolTipList = new List<Table>();
            this.TableToolTipRowDict = new Dictionary<GId, Row>();
            GuiGrid guiGrid = this.GuiGrid;
            if (guiGrid.GraphToolTips == null || guiGrid.GraphToolTips.Count == 0) return;

            foreach (GuiTable guiTable in guiGrid.GraphToolTips)
            {
                if (guiTable == null || guiTable.DataTable == null) continue;
                Table table = Table.CreateFrom(guiTable.DataTable);
                if (!table.AllowPrimaryKey)
                    throw new GraphLibDataException("Data v tabulce tooltipů «" + guiTable.FullName + "." + table.TableName + "» nepodporují PrimaryKey.");
                table.HasPrimaryIndex = true;
                if (table.RowsCount > 0)
                    this.TableToolTipList.Add(table);
            }
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
            Dictionary<GId, Row> cacheDict = this.TableTextRowDict;
            return this.GetTableInfoRow(sourceTables, cacheDict, graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.RowGId);
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
            Dictionary<GId, Row> cacheDict = this.TableToolTipRowDict;
            return this.GetTableInfoRow(sourceTables, cacheDict, graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.RowGId);
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro některý GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="sourceTables">Sada tabulek, kde lze najít potřebné texty</param>
        /// <param name="cacheDict">Dictionary, kde jsou pro konkrétní GId ukládány dříve již hledané údaje (včetně hodnoty NULL, pokud neexistuje).</param>
        /// <param name="gids">Jednotlivé klíče, pro které se má řádek v tabulce hledat</param>
        /// <returns></returns>
        protected Row GetTableInfoRow(List<Table> sourceTables, Dictionary<GId, Row> cacheDict, params GId[] gids)
        {
            foreach (GId gId in gids)
            {
                Row row = this.GetTableInfoRowForGId(sourceTables, cacheDict, gId);
                if (row != null) return row;
            }
            return null;
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro daný GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="sourceTables">Sada tabulek, kde lze najít potřebné texty</param>
        /// <param name="cacheDict">Dictionary, kde jsou pro konkrétní GId ukládány dříve již hledané údaje (včetně hodnoty NULL, pokud neexistuje).</param>
        /// <param name="gId">Hledaný klíč</param>
        /// <returns></returns>
        protected Row GetTableInfoRowForGId(List<Table> sourceTables, Dictionary<GId, Row> cacheDict, GId gId)
        {
            Row row = null;
            if (gId == null) return row;

            // Nejprve hledáme v "cache":
            if (cacheDict.TryGetValue(gId, out row))
                return row;

            // V cache nic není - budeme hledat v kompletních datech = v soupisu tabulek:
            foreach (Table table in sourceTables)
            {
                if (table.TryGetRowOnPrimaryKey(gId, out row))
                    break;
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
                this.IMainData.CallAppHostFunction(request, null, null);
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
                this.IMainData.CallAppHostFunction(request, this.ItemDragDropDropAppResponse, TimeSpan.FromMilliseconds(1500));
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
            SchedulerConfig.MoveSnapInfo snapInfo = this.Config.GetMoveSnapForKeys(Control.ModifierKeys);        // Zajímají nás aktuálně stisknuté klávesy, ne args.ModifierKeys !
            this.IMainData.AdjustGraphItemDragMove(moveInfo, snapInfo);

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
                if (cell.ValueType == TableValueType.Text && cell.Value != null)
                {   // Textový sloupec; může být neviditelný, může to být oddělovač řádků:
                    string value = cell.Value.ToString().Trim();
                    if (hasRowDelimiter && String.Equals(value, rowDelimiter))
                    {   // Sloupec obsahuje oddělovač řádků:
                        _GraphItemSumTextRows(ref textAll, ref textRow, ref height, ref heightRow);
                        if (height <= 0) break;
                        readRow = true;
                    }
                    else if (column.ColumnProperties.IsVisible && value.Length > 0 && readRow)
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
            bool wholeTask = GraphItemLinksForWholeTask(args.ItemEvent);
            bool asSCurve = (this.Config != null && this.Config.GuiEditShowLinkAsSCurve);

            GTimeGraphItem currentItem = args.ItemControl ?? args.GroupControl;     // Na tomto prvku začne hledání. Může to být prvek konkrétní, anebo prvek grupy.
            args.Links = this.SearchForGraphLink(currentItem, args.SearchSidePrev, args.SearchSideNext, wholeTask, asSCurve);
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
            item._ItemGId = guiGraphItem.ItemId;           // Mezi typy GuiId (=Green) a GId (GraphLibrary) existuje implicitní konverze.
            item._RowGId = guiGraphItem.RowId;             //  Takže do zdejších properties se vytvoří new instance GUid, obsahující stejná data jako vstupní GuiId.
            item._GroupGId = guiGraphItem.GroupId;         //  Další důsledek je ten, že zdejší data lze změnit = přemístit na jiný řádek, například.
            item._DataGId = guiGraphItem.DataId;
            item._Time = guiGraphItem.Time;                // Existuje implicitní konverze mezi typy TimeRange a GuiTimeRange.
            item._BehaviorMode = guiGraphItem.BehaviorMode;

            // ID pro grafickou vrstvu: vygenerujeme Int32 klíč pro daný GId, za pomoci indexu uloženého v hlavní tabulce (iGraphTable):
            item._ItemId = iGraphTable.GetId(item.ItemGId);
            item._GroupId = iGraphTable.GetId(item.GroupGId);

            // Ostatní property jsou načítané přímo z item._GuiGraphItem.

            return item;
        }
        /// <summary>
        /// Prvek si aktualizuje data z dodaného prvku.
        /// </summary>
        /// <param name="updateItem"></param>
        public bool UpdateFrom(GuiGraphBaseItem updateItem)
        {
            // Tato hodnota je ukládaná do prvku přímo, protože se provádí její editace:
            if (updateItem.Time != null) this._Time = updateItem.Time;

            // Aktualizovat jednotlivé statické hodnoty:
            GuiGraphBaseItem currentItem = this._GuiGraphItem;

            if (updateItem.Layer != 0) currentItem.Layer = updateItem.Layer;
            if (updateItem.Level != 0) currentItem.Level = updateItem.Level;
            if (updateItem.Order != 0) currentItem.Order = updateItem.Order;
            if (updateItem.Height > 0f) currentItem.Height = updateItem.Height;
            if (updateItem.Text != null) currentItem.Text = updateItem.Text;
            if (updateItem.ToolTip != null) currentItem.ToolTip = updateItem.ToolTip;
            if (updateItem.BackColor.HasValue) currentItem.BackColor = GetUpdatedColor(updateItem.BackColor);
            if (updateItem.LineColor.HasValue) currentItem.LineColor = GetUpdatedColor(updateItem.LineColor);
            if (updateItem.RatioBegin.HasValue) currentItem.RatioBegin = updateItem.RatioBegin;
            if (updateItem.RatioEnd.HasValue) currentItem.RatioEnd = updateItem.RatioEnd;
            if (updateItem.RatioBeginBackColor.HasValue) currentItem.RatioBeginBackColor = GetUpdatedColor(updateItem.RatioBeginBackColor);
            if (updateItem.RatioEndBackColor.HasValue) currentItem.RatioEndBackColor = GetUpdatedColor(updateItem.RatioEndBackColor);
            if (updateItem.RatioLineColor.HasValue) currentItem.RatioLineColor = GetUpdatedColor(updateItem.RatioLineColor);
            if (updateItem.RatioLineWidth.HasValue) currentItem.RatioLineWidth = updateItem.RatioLineWidth;
            if (updateItem.HatchColor.HasValue) currentItem.HatchColor = GetUpdatedColor(updateItem.HatchColor);
            if (updateItem.HatchColor.HasValue) currentItem.BackStyle = updateItem.BackStyle;      // Úmyslná změna: pokud je předána hodnota HatchColor, přebírám i BackStyle.
            if (updateItem.ImageBegin != null) currentItem.ImageBegin = GetUpdatedImage(updateItem.ImageBegin);
            if (updateItem.ImageEnd != null) currentItem.ImageEnd = GetUpdatedImage(updateItem.ImageEnd);

            // Tyto property budeme aktualizovat do zdejšího datového prvku GuiGraphBaseItem _GuiGraphItem, 
            //   i do this.*, protože tyto hodnoty zde máme jako pracovní (=editovatelné):
            if (updateItem.Time != null)
            {
                currentItem.Time = updateItem.Time;
                this.Time = updateItem.Time;
            }
            if (updateItem.BehaviorMode != GraphItemBehaviorMode.None)
            {
                currentItem.BehaviorMode = updateItem.BehaviorMode;
                this._BehaviorMode = updateItem.BehaviorMode;
            }

            // Zajistit invalidaci grafu:
            return true;
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
        public GuiGraphBaseItem GuiGraphItem { get { return this._GuiGraphItem; } }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible { get { return this._IsVisible; } set { this._IsVisible = value; } } private bool _IsVisible = true;
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
        float ITimeGraphItem.Height { get { return this._GuiGraphItem.Height; } }
        string ITimeGraphItem.Text { get { return this._GuiGraphItem.Text; } }
        string ITimeGraphItem.ToolTip { get { return this._GuiGraphItem.ToolTip; } }
        Color? ITimeGraphItem.BackColor { get { return this._GuiGraphItem.BackColor; } }
        Color? ITimeGraphItem.HatchColor { get { return this._GuiGraphItem.HatchColor; } }
        Color? ITimeGraphItem.LineColor { get { return this._GuiGraphItem.LineColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._GuiGraphItem.BackStyle; } }
        float? ITimeGraphItem.RatioBegin { get { return this._GuiGraphItem.RatioBegin; } }
        float? ITimeGraphItem.RatioEnd { get { return this._GuiGraphItem.RatioEnd; } }
        Color? ITimeGraphItem.RatioBeginBackColor { get { return this._GuiGraphItem.RatioBeginBackColor; } }
        Color? ITimeGraphItem.RatioEndBackColor { get { return this._GuiGraphItem.RatioEndBackColor; } }
        Color? ITimeGraphItem.RatioLineColor { get { return this._GuiGraphItem.RatioLineColor; } }
        int? ITimeGraphItem.RatioLineWidth { get { return this._GuiGraphItem.RatioLineWidth; } }
        Image ITimeGraphItem.ImageBegin { get { return App.Resources.GetImage(this._GuiGraphItem.ImageBegin); } }
        Image ITimeGraphItem.ImageEnd { get { return App.Resources.GetImage(this._GuiGraphItem.ImageEnd); } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this.BehaviorMode; } }
        GTimeGraphItem ITimeGraphItem.GControl { get { this._CheckGControl(); return this._GControl; } set { this._GControl = value; } }
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
