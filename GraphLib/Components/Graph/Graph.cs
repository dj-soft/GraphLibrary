using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Components.Graph
{
    // Prvek GTimeGraph je tak líný, jako by to odkoukal od GGridu a GTable.
    // Na veškeré vstupní změny reaguje líně, jenom si poznamená: "tohle nebo tamto je od teď neplatné"
    //  Například: po změně prvků v poli this.ItemList si jen nulluje pole this._GroupList
    //  anebo po změně výšky Bounds.Height nastaví příznak this.GroupItemsYValid = false.
    // Následně teprve když instance GTimeGraph má něco ze sebe vydat (typicky když má vrátit pole Childs), 
    //  tak s hrůzou zjistí, co vše není platné, a honem to dopočítá.
    // Další infromace k instanci GTimeGraph : jde o plnohodnotný Container, jehož Child prvky jsou prvky typu GTimeGraphGroup.
    // Tyto prvky mají určenou svoji souřadnici na ose Y v rámci grafu GTimeGraph.
    // A prvky GTimeGraphGroup reprezentují jednu skupinu vstupních prvků ITimeGraphItem (skupinu, která mí shodné GraphId).
    // A aby jednotlivé vstupní datové prvky grafu (=implementující ITimeGraphItem) nemusely samy být grafické prvky, 
    //  pak v nich je vložen jeden standardní Grafický prvek typu GTimeGraphItem (v property ITimeGraphItem.GControl).

    /*     Chování s ohledem na prostor grafu (Bounds.Size):

         a) Pokud dojde ke změně šířky (this.Bounds.Width), detekuje to časová osa (CheckValidTimeAxis() na základě TimeAxisIdentity, kterážto obsahuje šířku osy)
              Důsledkem toho bude invalidace VisibleGroups a následný přepočet obsahu VisibleGroupList
         b) Pokud dojde ke změně výšky (this.Bounds.Height), detekuje to koordinát Y (CheckValidCoordinateY() na základě 


    */

    /// <summary>
    /// Graf na časové ose
    /// </summary>
    public class GTimeGraph : InteractiveContainer, ITimeInteractiveGraph, ICloneable
    {
        #region Konstrukce, pole položek Items
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GTimeGraph(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTimeGraph()
            : base()
        {
            this._Init(null);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTimeGraph(GuiGraph guiGraph)
            : base()
        {
            this._Init(guiGraph);
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="guiGraph"></param>
        private void _Init(GuiGraph guiGraph)
        {
            this._ValidityLock = new object();
            this._ItemDict = new Dictionary<int, ITimeGraphItem>();
            this._GuiGraph = (guiGraph != null ? guiGraph.GetDefinitionData() : new GuiGraph());
            this.Is.SelectParent = true;
        }
        /// <summary>
        /// ID tohoto grafu. Hodnotu nastavuje aplikační kód dle své potřeby, hodnota je vkládána do identifikátorů odesílaných do handlerů událostí v grafu.
        /// Graf sám o sobě tuto hodnotu nepotřebuje a ani nenastavuje.
        /// </summary>
        public int GraphId { get; set; }
        /// <summary>
        /// Zdroj dat, nepovinný
        /// </summary>
        private ITimeGraphDataSource _DataSource;
        /// <summary>
        /// Zámek pro validaci/invalidaci
        /// </summary>
        private object _ValidityLock;
        #endregion
        #region Parametry tohoto grafu (GraphParameters): lokální nebo z parenta nebo defaultní
        /// <summary>
        /// Parametry pro tento graf.
        /// Buď jsou uloženy přímo zde jako explicitní, nebo jsou načteny z parenta, nebo jsou použity defaultní.
        /// Nikdy nevrací null.
        /// Lze setovat parametry, nebo null.
        /// </summary>
        public TimeGraphProperties CurrentGraphProperties
        {
            get
            {
                TimeGraphProperties gp = this._GraphProperties;
                if (gp == null)
                    gp = this._SearchParentGraphParameters();
                if (gp == null)
                    gp = this._GetDefaultGraphParameters();
                return gp;
            }
            set
            {
                this._GraphProperties = value;
            }
        }
        /// <summary>
        /// Metoda se pokusí najít parametry pro kreslení grafu ve svém parentovi, což může být GCell (pak hledám Column), 
        /// nebo je parentem GRow (pak hledám Table).
        /// </summary>
        /// <returns></returns>
        private TimeGraphProperties _SearchParentGraphParameters()
        {
            TimeGraphProperties gp = null;
            IInteractiveParent parent = this.Parent;
            if (parent != null)
            {
                if (parent is Grid.GCell)
                {   // Graf je umístěn v buňce => hledáme GraphParameters v sloupci a poté v tabulce:
                    Grid.GCell gCell = parent as Grid.GCell;
                    Column column = gCell.OwnerColumn;
                    if (column != null)
                    {
                        gp = column.GraphParameters;
                        if (gp == null && column.HasTable)
                            gp = column.Table.GraphParameters;
                    }
                }
                else if (parent is Grid.GRow)
                {   // Graf je umístěn v řádku => hledáme GraphParameters v tabulce:
                    Grid.GRow gRow = parent as Grid.GRow;
                    Table table = gRow.OwnerTable;
                    if (table != null)
                        gp = table.GraphParameters;
                }
            }
            return gp;
        }
        /// <summary>
        /// Metoda vrátí defaultní parametry grafu
        /// </summary>
        /// <returns></returns>
        private TimeGraphProperties _GetDefaultGraphParameters()
        {
            if (this._GraphPropertiesDefault == null)
                this._GraphPropertiesDefault = TimeGraphProperties.Default;
            return this._GraphPropertiesDefault;
        }
        private TimeGraphProperties _GraphProperties;
        private TimeGraphProperties _GraphPropertiesDefault;
        #endregion
        #region Data samotného grafu, napojená na GuiGraph
        /// <summary>
        /// Metoda do this grafu <see cref="GTimeGraph"/> vloží nová GUI definiční data grafu.
        /// Tato metoda nenačítá prvky grafu z <see cref="GuiGraph.GraphItems"/>!
        /// </summary>
        /// <param name="guiGraph"></param>
        public void UpdateGraphData(GuiGraph guiGraph)
        {
            this._GuiGraph = (guiGraph != null ? guiGraph.GetDefinitionData() : new GuiGraph());
            this._ReloadGraphProperties();
        }
        /// <summary>
        /// Přenačte do sebe (do <see cref="_GraphProperties"/>) vlastnosti grafu z <see cref="GuiGraphProperties"/>.
        /// </summary>
        private void _ReloadGraphProperties()
        {
            GuiGraphProperties guiGraphProperties = this.GuiGraphProperties;
            this._GraphProperties = (guiGraphProperties != null ? new TimeGraphProperties(guiGraphProperties) : null);
        }
        /// <summary>
        /// Definuje vlastnosti tohoto konkrétního grafu.
        /// Běžně bývá null (výchozí hodnota), pak se vlastnosti grafu přebírají z nadřízeného prvku (typicky z tabulky).
        /// </summary>
        public GuiGraphProperties GuiGraphProperties { get { return this._GuiGraph.GraphProperties; } set { this._GuiGraph.GraphProperties = value; this._ReloadGraphProperties(); } }
        /// <summary>
        /// Barva pozadí.
        /// Pokud není zadána, pak je pozadí grafu průhledné = vykresluje se do svého Parent prvku.
        /// </summary>
        public Color? BackgroundColor { get { return this._GuiGraph.BackgroundColor; } set { this._GuiGraph.BackgroundColor = value; } }
        /// <summary>
        /// Podbarvení počátku grafu.
        /// Podbarvení je postupné (LinearGradientBrush) : na počátku grafu je barva <see cref="BeginShadowColor"/>,
        /// podbarvení má šířku <see cref="BeginShadowArea"/> a plynule přechází do barvy pozadí grafu <see cref="BackgroundColor"/>.
        /// </summary>
        public Color? BeginShadowColor { get { return this._GuiGraph.BeginShadowColor; } set { this._GuiGraph.BeginShadowColor = value; } }
        /// <summary>
        /// Poměrná část šířky grafu, která je podbarvena pro zvýrazněnéí počátku.
        /// </summary>
        public float? BeginShadowArea { get { return this._GuiGraph.BeginShadowArea; } set { this._GuiGraph.BeginShadowArea = value; } }
        /// <summary>
        /// Ikona na počátku grafu.
        /// Je zobrazena v levé části grafu, ve středu jeho výšky.
        /// Typicky vyjadřuje problém na počátku.
        /// </summary>
        public GuiImage BeginImage { get { return this._GuiGraph.BeginImage; } set { this._GuiGraph.BeginImage = value; } }
        /// <summary>
        /// Podbarvení počátku grafu.
        /// Podbarvení je postupné (LinearGradientBrush) : na počátku grafu je barva <see cref="EndShadowColor"/>,
        /// podbarvení má šířku <see cref="EndShadowArea"/> a plynule přechází do barvy pozadí grafu <see cref="BackgroundColor"/>.
        /// </summary>
        public Color? EndShadowColor { get { return this._GuiGraph.EndShadowColor; } set { this._GuiGraph.EndShadowColor = value; } }
        /// <summary>
        /// Poměrná část šířky grafu, která je podbarvena pro zvýrazněnéí počátku.
        /// </summary>
        public float? EndShadowArea { get { return this._GuiGraph.EndShadowArea; } set { this._GuiGraph.EndShadowArea = value; } }
        /// <summary>
        /// Ikona na počátku grafu.
        /// Je zobrazena v levé části grafu, ve středu jeho výšky.
        /// Typicky vyjadřuje problém na počátku.
        /// </summary>
        public GuiImage EndImage { get { return this._GuiGraph.EndImage; } set { this._GuiGraph.EndImage = value; } }
        /// <summary>
        /// Podkladová data pro GUI
        /// </summary>
        private GuiGraph _GuiGraph;
        #endregion
        #region Prvky grafu : GraphItems, AddGraphItem(), RemoveGraphItem()
        /// <summary>
        /// Vloží daný prvek do this grafu.
        /// Při duplicitě klíče ItemId hlásí chybu (pokud není zadáno ignoreDuplicity = true).
        /// Vrací true = prvek byl vložen, nebo false = prvek již existoval a neměla se hlásit chyba.
        /// </summary>
        /// <param name="graphItem">Položka</param>
        /// <param name="ignoreDuplicity">Ignorovat duplicity</param>
        public bool AddGraphItem(ITimeGraphItem graphItem, bool ignoreDuplicity = false)
        {
            bool isAdded = false;
            bool result = this._AddGraphItem(graphItem, ignoreDuplicity, ref isAdded, true);
            return result;
        }
        /// <summary>
        /// Vloží daný prvek do this grafu.
        /// Při duplicitě klíče ItemId hlásí chybu (pokud není zadáno ignoreDuplicity = true).
        /// </summary>
        /// <param name="graphItems">Položky</param>
        /// <param name="ignoreDuplicity">Ignorovat duplicity</param>
        public void AddGraphItems(IEnumerable<ITimeGraphItem> graphItems, bool ignoreDuplicity = false)
        {
            if (graphItems == null) return;
            bool isAdded = false;
            graphItems.ForEachItem(i => this._AddGraphItem(i, ignoreDuplicity, ref isAdded, false));
            if (isAdded)
                this.Invalidate(InvalidateItems.AllGroups);
        }
        /// <summary>
        /// Fyzicky vloží daný prvek do this grafu
        /// </summary>
        /// <param name="graphItem">Položka</param>
        /// <param name="ignoreDuplicity">Ignorovat duplicity</param>
        /// <param name="isAdded">Nastaví se na true po skutečné změně Dictionary <see cref="_ItemDict"/></param>
        /// <param name="callInvalidate">Zavolat Invalidaci. Snažme se ji volat jen jednou, po posledním prvku, pokud to víme.</param>
        private bool _AddGraphItem(ITimeGraphItem graphItem, bool ignoreDuplicity, ref bool isAdded, bool callInvalidate)
        {
            if (graphItem == null) return false;
            if (this._ItemDict.ContainsKey(graphItem.ItemId))
            {
                if (ignoreDuplicity) return false;
                throw new GraphLibDataException("Do jednoho grafu nelze vložit více položek se stejným klíčem ITimeGraphItem.ItemId: " + graphItem.ItemId.ToString() + ".");
            }
            graphItem.OwnerGraph = this;
            this._ItemDict.Add(graphItem.ItemId, graphItem);
            isAdded = true;
            if (callInvalidate)
                this.Invalidate(InvalidateItems.AllGroups);
            return true;
        }
        /// <summary>
        /// Metoda zkusí najít prvek daného ID
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="graphItem"></param>
        /// <returns></returns>
        public bool TryGetGraphItem(int itemId, out ITimeGraphItem graphItem)
        {
            return this._ItemDict.TryGetValue(itemId, out graphItem);
        }
        /// <summary>
        /// Odebere z grafu daný prvek (podle jeho ItemId).
        /// Pokud daný klíč neexistuje, hlásí chybu (pokud není zadáno ignoreMissing = true).
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="ignoreMissing">Ignorovat chybějící prvek podle daného klíče</param>
        public bool RemoveGraphItem(ITimeGraphItem graphItem, bool ignoreMissing = false)
        {
            bool isRemoved = false;
            return this._RemoveGraphItem(graphItem, ignoreMissing, ref isRemoved, true);
        }
        /// <summary>
        /// Odebere z grafu prvek s daným klíčem ItemId.
        /// Pokud daný klíč neexistuje, hlásí chybu (pokud není zadáno ignoreMissing = true).
        /// </summary>
        /// <param name="itemId">ItemId prvku</param>
        /// <param name="ignoreMissing">Ignorovat chybějící prvek podle daného klíče</param>
        public bool RemoveGraphItem(int itemId, bool ignoreMissing = false)
        {
            bool isRemoved = false;
            return this._RemoveGraphItem(itemId, ignoreMissing, ref isRemoved, true);
        }
        /// <summary>
        /// Odebere z grafu dané prvky (podle jejich ItemId).
        /// Pokud daný klíč neexistuje, hlásí chybu (pokud není zadáno ignoreMissing = true).
        /// </summary>
        /// <param name="graphItems">Prvky grafu</param>
        /// <param name="ignoreMissing">Ignorovat chybějící prvek podle daného klíče</param>
        public void RemoveGraphItems(IEnumerable<ITimeGraphItem> graphItems, bool ignoreMissing = false)
        {
            if (graphItems == null) return;
            bool isRemoved = false;
            graphItems.ForEachItem(i => this._RemoveGraphItem(i, ignoreMissing, ref isRemoved, false));
            if (isRemoved)
                this.Invalidate(InvalidateItems.AllGroups);         // Invalidaci dáme až po poslední položce, pokud byla nějaká změna
        }
        /// <summary>
        /// Odebere z grafu prvky s danými klíči ItemId.
        /// Pokud daný klíč neexistuje, hlásí chybu (pokud není zadáno ignoreMissing = true).
        /// </summary>
        /// <param name="itemIds">ItemId prvky</param>
        /// <param name="ignoreMissing">Ignorovat chybějící prvek podle daného klíče</param>
        public void RemoveGraphItems(IEnumerable<int> itemIds, bool ignoreMissing = false)
        {
            if (itemIds == null) return;
            bool isRemoved = false;
            itemIds.ForEachItem(i => this._RemoveGraphItem(i, ignoreMissing, ref isRemoved, false));
            if (isRemoved)
                this.Invalidate(InvalidateItems.AllGroups);         // Invalidaci dáme až po poslední položce, pokud byla nějaká změna
        }
        /// <summary>
        /// Fyzicky odebere daný prvek z this grafu
        /// </summary>
        /// <param name="graphItem">Prvek, jehož ItemId bude použito jako klíč prvku, který se bude odebírat</param>
        /// <param name="ignoreMissing">true = ignorovat chybějící prvek (nehlásit chybu)</param>
        /// <param name="isRemoved">Nastaví se na true po skutečné změně Dictionary <see cref="_ItemDict"/></param>
        /// <param name="callInvalidate">Zavolat Invalidaci. Snažme se ji volat jen jednou, po posledním prvku, pokud to víme.</param>
        private bool _RemoveGraphItem(ITimeGraphItem graphItem, bool ignoreMissing, ref bool isRemoved, bool callInvalidate)
        {
            if (graphItem == null) return false;
            return this._RemoveGraphItem(graphItem.ItemId, ignoreMissing, ref isRemoved, callInvalidate);
        }
        /// <summary>
        /// Fyzicky odebere daný prvek z this grafu
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="ignoreMissing">true = ignorovat chybějící prvek (nehlásit chybu)</param>
        /// <param name="isRemoved">Nastaví se na true po skutečné změně Dictionary <see cref="_ItemDict"/></param>
        /// <param name="callInvalidate">Zavolat Invalidaci. Snažme se ji volat jen jednou, po posledním prvku, pokud to víme.</param>
        private bool _RemoveGraphItem(int itemId, bool ignoreMissing, ref bool isRemoved, bool callInvalidate)
        {
            ITimeGraphItem graphItem;
            if (!this._ItemDict.TryGetValue(itemId, out graphItem))
            {
                if (ignoreMissing) return false;
                throw new GraphLibDataException("Z grafu nelze odebrat prvek s klíčem ITimeGraphItem.ItemId: " + itemId.ToString() + ", prvek v něm není přítomen.");
            }
            this._ItemDict.Remove(itemId);
            isRemoved = true;
            graphItem.OwnerGraph = null;
            if (callInvalidate)
                this.Invalidate(InvalidateItems.AllGroups);
            return true;
        }
        /// <summary>
        /// Počet všech prvků grafu (tj. včetně neviditelných)
        /// </summary>
        public int ItemCount { get { return this._ItemDict.Count; } }
        /// <summary>
        /// Všechny prvky this grafu, včetně neviditelných = těch, které mají <see cref="ITimeGraphItem.IsVisible"/> == false
        /// </summary>
        public IEnumerable<ITimeGraphItem> AllGraphItems { get { return this._ItemDict.Values; } }
        /// <summary>
        /// Všechny viditelné prvky this grafu
        /// </summary>
        public IEnumerable<ITimeGraphItem> VisibleGraphItems { get { return this._ItemDict.Values.Where(i => i.IsVisible); } }
        /// <summary>
        /// Metoda provede danou akci pro každý prvek grafu (i pro invisible), a poté zajistí kompletní invalidaci dat
        /// </summary>
        /// <param name="action"></param>
        public void ModifyGraphItems(Action<ITimeGraphItem> action)
        {
            if (action != null)
            {
                var graphItems = this._ItemDict.Values.ToArray();
                foreach (ITimeGraphItem iItem in graphItems)
                    action(iItem);
            }
            this.Invalidate(InvalidateItems.AllGroups);
        }
        /// <summary>
        /// Index prvků
        /// </summary>
        private Dictionary<int, ITimeGraphItem> _ItemDict;
        #endregion
        #region AllGroupList : Seskupování položek z this.ItemList do skupin GTimeGraphGroup, setřídění těchto skupin podle vrstev a hladin na logické ose Y
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální logické souřadnice Y.
        /// Pokud jsou neplatné, znovu vytvoří pole <see cref="AllGroupList"/> a vypočítá logické souřadnice Y.
        /// Naplní hodnoty: <see cref="GTimeGraphGroup.CoordinateYLogical"/> a <see cref="GTimeGraphGroup.CoordinateYVirtual"/>, 
        /// připraví kalkulátor <see cref="CalculatorY"/>.
        /// </summary>
        protected void CheckValidAllGroupList()
        {
            if (this._AllGroupList == null)
                this.RecalculateAllGroupList();
        }
        /// <summary>
        /// Vypočítá logické souřadnice Y pro všechny položky pole <see cref="VisibleGraphItems"/>.
        /// Naplní hodnoty: <see cref="GTimeGraphGroup.CoordinateYLogical"/> a <see cref="GTimeGraphGroup.CoordinateYVirtual"/>, 
        /// připraví kalkulátor <see cref="CalculatorY"/>.
        /// </summary>
        protected void RecalculateAllGroupList()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateLogY", ""))
            {
                int layers = 0;
                int levels = 0;
                int groups = 0;
                int items = this.ItemCount;

                lock (this._ValidityLock)
                {
                    Interval<float> totalLogicalY = new Interval<float>(0f, 0f, true);
                    float minimalFragmentHeight = 1f;

                    // Připravíme si soupis (elementů dle) vrstev, které obsahují nějaký element s výškou Height == null (=jejich výška se určí podle výšky ostatních vrstev):
                    List<Tuple<int, int, ITimeGraphItem[]>> layerDependendGroups = new List<Tuple<int, int, ITimeGraphItem[]>>();

                    // Vytvoříme oddělené skupiny prvků, podle jejich příslušnosti do grafické vrstvy (ITimeGraphItem.Layer), vzestupně:
                    List<IGrouping<int, ITimeGraphItem>> layerGroups = this.VisibleGraphItems.GroupBy(i => i.Layer).ToList();
                    layers = layerGroups.Count;
                    if (layers > 1)
                        layerGroups.Sort((a, b) => a.Key.CompareTo(b.Key));        // Vrstvy setřídit podle Key = ITimeGraphItem.Layer, vzestupně

                    // Připravím si finální pole, obsahující jednotlivé vrstvy elementů grafu:
                    GTimeGraphGroup[][] allGroups = new GTimeGraphGroup[layers][];

                    int l = 0;
                    foreach (IGrouping<int, ITimeGraphItem> layerGroup in layerGroups)
                    {   // Hodnota Layer pro tuto skupinu. 
                        // Jedna vrstva (Layer) je ekvivalentní jedné grafické vrstvě, položky z různých vrstev jsou kresleny jedna přes druhou.
                        int layer = layerGroup.Key;

                        // Zkonkrétním prvky této vrstvy (IEnumerable) do Array:
                        ITimeGraphItem[] layerList = layerGroup.ToArray();

                        if (layerList.Any(i => !i.Height.HasValue))
                        {   // Pokud v této vrstvě existuje alespoň jeden prvek, jehož Height je NULL, pak tuto vrstvu budu řešit jinak a později:
                            //  Item1 = číslo vrstvy ITimeGraphItem.Layer;  Item2 = [index] do pole allGroups;  Item3 = pole prvků dané vrstvy:
                            layerDependendGroups.Add(new Tuple<int, int, ITimeGraphItem[]>(layer, l, layerList));
                        }
                        else
                        {   // Všechny prvky ve vrstvě mají specifikovánu výšku Height:
                            // Každá VRSTVA (layerGroup) má svoje vlastní pole využití prostoru, toto pole je společné pro všechny ITimeGraphItem.Level
                            PointArray<DateTime, IntervalArray<float>> layerUsing = new PointArray<DateTime, IntervalArray<float>>();

                            // V rámci jedné vrstvy: další grupování jejích prvků podle jejich hodnoty ITimeGraphItem.Level, vzestupně:
                            List<IGrouping<int, ITimeGraphItem>> levelGroups = layerGroup.GroupBy(i => i.Level).ToList();
                            if (levelGroups.Count > 1)
                                levelGroups.Sort((a, b) => a.Key.CompareTo(b.Key));    // Hladiny setřídit podle Key = ITimeGraphItem.Level, vzestupně
                            levels += levelGroups.Count;

                            // Hladina (Level) má význam "vodorovného pásu" pro více prvků stejné hladiny.
                            // Záporné hladiny jsou kresleny dolů (jako záporné hodnoty na ose Y).

                            // Nyní zpracuji grafické prvky dané vrstvy (layerGroup) po jednotlivých skupinách za hladiny Level (levelGroups),
                            // vypočtu jejich logické souřadnice Y a přidám je do ItemGroupList:
                            Interval<float> layerUsedLogicalY = new Interval<float>(0f, 0f, true); // Pole použitých souřadnic na ose Y pro celou VRSTVU
                            List<GTimeGraphGroup> layerGroupList = new List<GTimeGraphGroup>();    // Sumární pole elementů za jednu vrstvu
                            foreach (IGrouping<int, ITimeGraphItem> levelGroup in levelGroups)
                            {
                                layerUsing.Clear();
                                this.RecalculateElementsInLevel(levelGroup, layerUsing, (levelGroup.Key < 0), layerGroupList, layerUsedLogicalY, ref minimalFragmentHeight, ref groups);
                            }
                            totalLogicalY.MergeWith(layerUsedLogicalY);

                            allGroups[l] = layerGroupList.ToArray();
                        }
                        l++;
                    }

                    if (layerDependendGroups.Count > 0)
                    {   // Pokud jsme našli nějakou vrstvu, jejíž prvky mají Height == NULL, pak reálná výška těchto prvků bude rovna celé použité výšce v grafu:
                        // Výška prvků grafu bude v rozmezí 0 (nejdeme do záporných hodnot) až totalLogicalY.End, a pokud totalLogicalY.End je menší než 1, pak 1.0:
                        float topY = (totalLogicalY.End < 1f ? 1f : totalLogicalY.End);
                        foreach (var layerDependendGroup in layerDependendGroups)
                        {
                            //  Item1 = číslo vrstvy ITimeGraphItem.Layer;  Item2 = [index] do pole allGroups;  Item3 = pole prvků dané vrstvy:
                            int layer = layerDependendGroup.Item1;
                            l = layerDependendGroup.Item2;
                            ITimeGraphItem[] layerList = layerDependendGroup.Item3;
                            List<GTimeGraphGroup> layerGroupList = this.RecalculateDependentElements(layerList, topY, layer);
                            allGroups[l] = layerGroupList.ToArray();
                        }
                    }

                    this._AllGroupList = allGroups;

                    this.SetMinimalFragmentHeight(minimalFragmentHeight);
                    this.Invalidate(InvalidateItems.CoordinateX | InvalidateItems.CoordinateYVirtual);

                    this.CalculatorY.Prepare(totalLogicalY, this.CurrentLineLogicalHeight);
                    this.RecalculateCoordinateYVirtual();
                }

                scope.AddItem("Layers Count: " + layers.ToString());
                scope.AddItem("Levels Count: " + levels.ToString());
                scope.AddItem("Groups Count: " + groups.ToString());
                scope.AddItem("Items Count: " + items.ToString());
            }
        }
        /// <summary>
        /// Zpracuje grafické prvky jedné vrstvy a jedné hladiny (z prvků <see cref="ITimeGraphItem"/> z pole items, do prvků <see cref="GTimeGraphGroup"/> do pole layerGroupList.
        /// Vstupní prvky seskupí podle hodnoty <see cref="ITimeGraphItem.GroupId"/> do skupin, pro každou skupinu najde její pozici na ose X (její datum počátku a konce), 
        /// určí její výšku na ose Y, a v objektu layerUsing najde vhodnou logickou pozici na ose Y, kde nová skupina nebude v konfliktu s jinou již zadanou skupinou.
        /// <para/>
        /// Do této metody vstupují pouze elementy grafu, které mají definovanou výšku prvku v <see cref="ITimeGraphItem.Height"/>
        /// </summary>
        /// <param name="items">Jednotlivé grafické prvky, které budeme zpracovávat</param>
        /// <param name="layerUsing">Objekt, který řeší využití 2D plochy, kde ve směru X je hodnota typu DateTime, a ve směru Y je pole intervalů typu float</param>
        /// <param name="isDownward">Směr využití na ose Y: true = hledáme volé místo směrem dolů, false = nahoru</param>
        /// <param name="layerGroupList">Výstupní pole, do něhož se ukládají prvky typu <see cref="GTimeGraphGroup"/>, které v sobě zahrnují jeden nebo více prvků <see cref="ITimeGraphItem"/> se shodnou hodnotou <see cref="ITimeGraphItem.GroupId"/></param>
        /// <param name="layerUsedLogicalY">Sumární interval využití osy Y</param>
        /// <param name="minimalFragmentHeight">Nejmenší logická výška zlomku prvku, počítáno z prvků jejichž výška je kladná, z desetinné části (například z výšky 2.25 se akceptuje 0.25).</param>
        /// <param name="groups">Počet skupin, průběžné počitadlo</param>
        protected void RecalculateElementsInLevel(IEnumerable<ITimeGraphItem> items, PointArray<DateTime, IntervalArray<float>> layerUsing, bool isDownward, List<GTimeGraphGroup> layerGroupList, Interval<float> layerUsedLogicalY, ref float minimalFragmentHeight, ref int groups)
        {
            List<GTimeGraphGroup> groupList = CreateTimeGroupList(items);
            groups += groupList.Count;

            // Hlavním úkolem nyní je určit logické souřadnice Y pro každou skupinu prvků GTimeGraphGroup,
            //  vycházíme přitom z jejího časového intervalu a její výšky,
            // a tuto skupinu zařazujeme do volného prostoru v instanci layerUsing:
            float searchFrom = (isDownward ? layerUsedLogicalY.Begin : layerUsedLogicalY.End);
            float nextSearch = searchFrom;
            foreach (GTimeGraphGroup group in groupList)
            {
                if (group.IsValidRealTime)
                {   // Grupa je reálná (výška je kladná, a čas je buďto kladný, anebo nulový a obsahuje ikonu zobrazovanou v daném čase):
                    // Instance layerUsing je PointArray pro: Point = DateTime, a Value = pole intervalů typu float.
                    // Což znamená: instance obsahuje prvky, kde klíčem (na ose X) je datum, kdy se mění využití daného prostoru, 
                    // a hodnotou na této X souřadnici je využití prostoru počínaje tímto datem.
                    // Využití prostoru (obsazení na ose Y) reprezentuje IntervalArray, což je pole intervalů, kde interval má Begin a End, 
                    // v tomto rozmezí osy Y je prostor obsazen.

                    // Nejprve získáme pole obsahující datum změny (souřadnice X) a využití prostoru (souřadnice Y), pro daný časový interval:
                    TimeRange time = group.Time;
                    var intervalAllItems = layerUsing.Search(time.Begin.Value, time.End.Value, true);
                    var intervalWorkItems = intervalAllItems.GetRange(0, intervalAllItems.Count - 1);        // poslední prvek odeberu, nezajímá mě (reprezentuje situaci na ose Y v čase našeho konce)

                    // Provedeme sumarizaci využití prostoru na souřadnici Y:
                    // Což má význam: Máme pole využití prostoru Y v různých časových okamžicích (intervalWorkItems), 
                    //  ale nás zajímá jejich souhrn => abych v souhrnu tom našel kontinuální prostor pro náš nový požadavek:
                    IntervalArray<float> summary = (intervalWorkItems.Count > 1 ? IntervalArray<float>.Summary(intervalWorkItems.Select(i => i.Value.Value)) : intervalWorkItems[0].Value.Value);

                    // Výška prvku (je kladná, to zajišťuje úvodní podmínka (group.IsValidRealTime)):
                    float groupHeight = group.Height.Value;

                    // Střádání hodnoty minimalFragmentHeight:
                    float fragmentHeight = groupHeight % 1f;
                    if (fragmentHeight > 0f && fragmentHeight < minimalFragmentHeight) minimalFragmentHeight = fragmentHeight;

                    // Negativní level bude hledat negativní velikost (dolů):
                    float size = (isDownward ? -groupHeight : groupHeight);

                    // Nyní v sumáři využitého místa (summary) vyhledáme nejbližší volný prostor s přinejmenším požadovanou velikostí:
                    Interval<float> useSpace = summary.SearchForSpace(searchFrom, size, (a, b) => (a + b));

                    // Nalezený logický prostor Y vložíme do grupy:
                    group.CoordinateYLogical = useSpace.ValueClone;

                    // Nalezený logický prostor (useSpace) vepíšeme do všech prvků na časové ose (patří do layerUsing):
                    intervalWorkItems.ForEachItem(pni => pni.Value.Value.Add(useSpace));

                    // Keep the summary values for next Level:
                    if (isDownward && useSpace.Begin < nextSearch)
                        nextSearch = useSpace.Begin;
                    else if (!isDownward && useSpace.End > nextSearch)
                        nextSearch = useSpace.End;

                }
                else
                {   // Nereálné položky (Time or Height je nula nebo záporné):
                    group.CoordinateYLogical = new Interval<float>(searchFrom, searchFrom);
                }
                layerGroupList.Add(group);
            }

            // Current Level has use this part of layerUsing:
            if (isDownward && nextSearch < layerUsedLogicalY.Begin)
                layerUsedLogicalY.Begin = nextSearch; // RoundLogicalY(nextSearch, isDownward);
            else if (!isDownward && nextSearch > layerUsedLogicalY.End)
                layerUsedLogicalY.End = RoundLogicalY(nextSearch, isDownward);
        }
        /// <summary>
        /// Metoda vygeneruje a vrátí pole <see cref="GTimeGraphGroup"/>, do logické výšky všech prvků vepíše rozsah 0 až <paramref name="topY"/>.
        /// </summary>
        /// <param name="layerList">Prvky</param>
        /// <param name="topY">Souřadnice Y Top logická</param>
        /// <param name="layer">Číslo vrstvy</param>
        /// <returns></returns>
        private List<GTimeGraphGroup> RecalculateDependentElements(ITimeGraphItem[] layerList, float topY, int layer)
        {
            // Vstupní prvky sgrupujeme podle GroupID:
            List<GTimeGraphGroup> groupList = CreateTimeGroupList(layerList);

            // Do každé grupy vepíšu její logické souřadnice Y:
            foreach (GTimeGraphGroup group in groupList)
            {
                if (group.Height.HasValue)
                    throw new GraphLibDataException("GraphItem.Height disagreement: on Layer " + layer.ToString() + ", a combination of NULL and NOT NULL Height elements is not permitted.");
                group.CoordinateYLogical = new Interval<float>(0f, topY);
            }

            return groupList;
        }
        /// <summary>
        /// Z dodaných prvků <see cref="ITimeGraphItem"/> vytvoří a vrátí pole prvků <see cref="GTimeGraphGroup"/> podle pravidel pro prvky grafu.
        /// Klíčem pro tvorbu <see cref="GTimeGraphGroup"/> je <see cref="ITimeGraphItem.GroupId"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private List<GTimeGraphGroup> CreateTimeGroupList(IEnumerable<ITimeGraphItem> items)
        {
            // Grafické prvky seskupíme podle ITimeGraphItem.GroupId:
            //  více prvků se shodným GroupId tvoří jeden logický celek, tyto prvky jsou vykresleny ve společné linii, nemíchají se s prvky s jiným GroupId.
            // Jedna GroupId reprezentuje například jednu výrobní operaci (nebo přesněji její paralelní průchod), například dva týdny práce;
            //  kdežto jednotlivé položky ITimeGraphItem reprezentují jednotlivé pracovní časy, například jednotlivé směny.
            List<GTimeGraphGroup> groupList = new List<GTimeGraphGroup>();     // Výsledné pole prvků GTimeGraphGroup
            List<ITimeGraphItem> groupsItems = new List<ITimeGraphItem>();     // Sem vložíme prvky ITimeGraphItem, které mají GroupId nenulové, odsud budeme generovat grupy...
            bool acceptZeroTime = (this.GraphItemMinPixelWidth > 0);
            // a) Položky bez GroupId:
            foreach (ITimeGraphItem item in items)
            {
                if (item.GroupId == 0)
                    groupList.Add(new GTimeGraphGroup(this, acceptZeroTime, item));      // Jedna instance GTimeGraphGroup obsahuje jeden pracovní čas
                else
                    groupsItems.Add(item);
            }

            // b) Položky, které mají GroupId nenulové, podle něj seskupíme:
            IEnumerable<IGrouping<int, ITimeGraphItem>> groupArray = groupsItems.GroupBy(i => (i.GroupId != 0 ? i.GroupId : i.ItemId));
            foreach (IGrouping<int, ITimeGraphItem> group in groupArray)
                groupList.Add(new GTimeGraphGroup(this, acceptZeroTime, group));         // Jedna instance GTimeGraphGroup obsahuje jeden nebo více pracovních časů

            // Setřídíme prvky GTimeGraphGroup podle jejich Order a podle času jejich počátku:
            if (groupList.Count > 1)
                groupList.Sort((a, b) => GTimeGraphGroup.CompareOrderTimeAsc(a, b));
            return groupList;
        }
        /// <summary>
        /// Zarovná logickou hodnotu y na nejbližší celé číslo (dolů/nahoru) po dokončení rekalkulace jedné hladiny.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="isDownward"></param>
        /// <returns></returns>
        protected static float RoundLogicalY(float y, bool isDownward)
        {
            float ya = (y < 0f ? -y : y);
            if ((ya % 1f) == 0f) return (isDownward ? -ya : ya);
            return (float)(isDownward ? -Math.Ceiling((double)ya) : Math.Ceiling((double)ya));
        }
        /// <summary>
        /// Metoda převezme hodnotu nejmenší nalezené výšky prvku v rámci grafu.
        /// Uloží ji do <see cref="CurrentMinimalFragmentHeight"/>, 
        /// a na jejím základě vybere jednotkovu logickou výšku do <see cref="CurrentLineLogicalHeight"/>.
        /// Tuto hodnotu (<see cref="CurrentLineLogicalHeight"/>) je třeba následně předat do kalkulátoru Y, 
        /// do jeho metody <see cref="PositionCalculatorInfo.Prepare(Interval{float}, int)"/> jako druhý parametr.
        /// </summary>
        /// <param name="minimalFragmentHeight">Nejmenší logická výška zlomku prvku, počítáno z prvků jejichž výška je kladná, z desetinné části (například z výšky 2.25 se akceptuje 0.25).</param>
        protected void SetMinimalFragmentHeight(float minimalFragmentHeight)
        {
            this.CurrentMinimalFragmentHeight = ((minimalFragmentHeight > 0f && minimalFragmentHeight < 1f) ? minimalFragmentHeight : 1f);
            bool hasPartial = (this.CurrentMinimalFragmentHeight < 1f);
            this.CurrentLineLogicalHeight = (!hasPartial ? this.CurrentGraphProperties.OneLineHeight.Value : this.CurrentGraphProperties.OneLinePartialHeight.Value);
        }
        /// <summary>
        /// Aktuálně nalezená nejmenší výška zlomku prvku, počítáno z prvků jejichž výška je kladná, 
        /// z desetinné části (například z výšky 2.25 se akceptuje 0.25).
        /// </summary>
        protected float CurrentMinimalFragmentHeight { get; set; }
        /// <summary>
        /// Aktuálně platná výška (v pixelech) pro jednu logickou jednotku výšky prvku.
        /// Odpovídá hodnotě z konfigurace <see cref="TimeGraphProperties.OneLineHeight"/> nebo <see cref="TimeGraphProperties.OneLinePartialHeight"/>,
        /// vybírá se podle toho, zd v aktuálním grafu se vyskytují zlomkové prvky (které mají desetinnou složku výšky), např. mají výšku 0.500, nebo 1.500 nebo 3.33333 atd.
        /// </summary>
        protected int CurrentLineLogicalHeight { get; set; }
        /// <summary>
        /// Metoda projde všechny prvky <see cref="GTimeGraphGroup"/> v poli <see cref="AllGroupList"/>, a pro každý prvek provede danou akci.
        /// </summary>
        /// <param name="action"></param>
        protected void AllGroupScan(Action<GTimeGraphGroup> action)
        {
            foreach (GTimeGraphGroup[] layer in this.AllGroupList)
                foreach (GTimeGraphGroup group in layer)
                    action(group);
        }
        /// <summary>
        /// Seznam všech skupin prvků k zobrazení v grafu.
        /// Seznam má dvojitou úroveň: v první úrovni jsou vizuální vrstvy (od spodní po vrchní), 
        /// v druhé úrovni jsou pak jednotlivé prvky <see cref="GTimeGraphGroup"/> k vykreslení.
        /// </summary>
        protected GTimeGraphGroup[][] AllGroupList { get { this.CheckValidAllGroupList(); return this._AllGroupList; } } private GTimeGraphGroup[][] _AllGroupList;
        #endregion
        #region CalculatorY = Kalkulátor souřadnic Y : výška grafu a přepočty souřadnice Y z logické (float, zdola nahoru) do fyzických pixelů (int, zhora dolů)
        /// <summary>
        /// Kalkulátor souřadnic na ose Y
        /// </summary>
        protected PositionCalculatorInfo CalculatorY { get { if (this._CalculatorY == null) this._CalculatorY = new PositionCalculatorInfo(this); return this._CalculatorY; } } private PositionCalculatorInfo _CalculatorY;
        /// <summary>
        /// Instance objektu, jehož výšku může graf změnit i číst pro korektní přepočty svých vnitřních souřadnic.
        /// Typicky se sem vkládá řádek grafu, instance třídy <see cref="Row"/>.
        /// Graf nikdy nepracuje se šířkou parenta <see cref="IVisualParent.ClientWidth"/>.
        /// </summary>
        public IVisualParent VisualParent { get { return this._VisualParent; } set { this._VisualParent = value; this.Invalidate(InvalidateItems.CoordinateYReal); } } private IVisualParent _VisualParent;
        /// <summary>
        /// Aktuální výška dat celého grafu, v pixelech
        /// </summary>
        public int GraphPixelHeight
        {
            get
            {
                this.CheckValidAllGroupList();
                return this.CalculatorY.TotalPixelSize;
            }
        }
        /// <summary>
        /// Třída, která v sobě zapouzdřuje data a výpočty pro převod souřadnic na ose Y
        /// </summary>
        protected class PositionCalculatorInfo
        {
            #region Veřejné rozhraní
            /// <summary>
            /// Konstuktor
            /// </summary>
            /// <param name="owner"></param>
            public PositionCalculatorInfo(GTimeGraph owner)
            {
                this._Owner = owner;
            }
            /// <summary>
            /// Připraví výpočty pro nově zadané rozmezí logických hodnot
            /// </summary>
            /// <param name="totalLogicalRange">Rozsah logických hodnot na ose</param>
            /// <param name="lineLogicalHeight">Stanovená výška jedné logické jednotky, v pixelech</param>
            public void Prepare(Interval<float> totalLogicalRange, int lineLogicalHeight)
            {
                this._IsPrepared = false;
                this._TotalLogicalRange = totalLogicalRange;
                this._LineLogicalHeight = lineLogicalHeight;
                if (totalLogicalRange == null) return;

                float logBegin = (totalLogicalRange.Begin < 0f ? totalLogicalRange.Begin : 0f);
                float upperSpace = this._GraphProperties.UpperSpaceLogical;
                upperSpace = (upperSpace < 0f ? 0f : (upperSpace > 2f ? 2f : upperSpace));
                float logEnd = (totalLogicalRange.End > 1f ? totalLogicalRange.End : 1f) + upperSpace;
                float logSize = logEnd - logBegin;

                // Výška dat grafu v pixelech, zarovnaná do patřičných mezí:
                int pixelData = (int)(Math.Ceiling(logSize * (float)lineLogicalHeight)); // Tolik pixelů bychom potřebovali pro data
                int pixelSize = this._AlignTotalPixelSize(pixelData);                    // Tolik pixelů máme k dipozici po zarovnání do rozmezí do TimeGraphProperties.TotalHeightRange
                this._TotalPixelSize = pixelSize;

                if (pixelData > pixelSize)       // Pokud potřebujeme pro graf více prostoru (pixelData), než nám graf poskytuje (pixelSize),
                    pixelData = pixelSize;       //  tak musíme kalkulátor nastavit na tu menší hodnotu = pixelSize.
                // Výpočty kalkulátoru, invalidace VisibleList:
                this._Calculator_Offset = logBegin;
                this._Calculator_Scale = (float)pixelData / logSize;

                this._Owner.Invalidate(InvalidateItems.CoordinateYReal);
                this._IsPrepared = true;
            }
            /// <summary>
            /// Metoda vrátí rozsah hodnot ve virtuálním formátu, pro zadané logické souřadnice.
            /// Virtuální formát je v pixelech, ale hodnota 0 odpovídá DOLNÍMU okraji grafu.
            /// Je to proto, aby se grafy nemusely přepočítávat při změně výšky grafu: 0 je stále dole.
            /// Výstup má Begin = horní souřadnice Y, End = dolní souřadnice Y na virtuální ose Y.
            /// Kdežto ve WinForm reprezentaci je nula nahoře...
            /// </summary>
            /// <param name="logicalRange"></param>
            /// <returns></returns>
            public Int32Range GetVirtualRange(Interval<float> logicalRange)
            {
                float end = this._GetVirtualPosition(logicalRange.End);        // Pro větší logické hodnoty (logicalRange.End) vrací větší virtuální souřadnici, End = kde nahoře prvek "začíná"
                float size = this._Calculator_Scale * (logicalRange.End - logicalRange.Begin);     // Velikost prvku v pixelech, měla by být kladná
                return Int32Range.CreateFromBeginSize((int)Math.Round(end, 0), (int)Math.Round(size, 0));   // Výstup má Begin = horní souřadnice Y, End = dolní souřadnice Y na virtuální ose Y
            }
            /// <summary>
            /// Metoda vrátí virtuální souřadnici v pixelech pro zadanou logickou souřadnici.
            /// Vrácená hodnota je rovna (GraphPixelHeight - 1) pro logicalY = this.UsedLogicalY.Begin (logický začátek osy Y je dole = ve Windows grafice větší souřadnice Y).
            /// Pro logickou hodnotu this.UsedLogicalY.End je vrácen pixel = 0 (logický kladný konec osy je nahoře = ve Windows grafice menší souřadnice Y).
            /// </summary>
            /// <param name="logicalValue"></param>
            /// <returns></returns>
            public int GetVirtualPixel(float logicalValue)
            {
                float pixelY = this._GetVirtualPosition(logicalValue);
                return (int)Math.Round(pixelY, 0);
            }
            /// <summary>
            /// Metoda vrátí reálnou souřadnici pro danou virtuální souřadnici a reálnou výšku.
            /// Virtuální formát je v pixelech, ale hodnota 0 odpovídá DOLNÍMU okraji grafu.
            /// Reálný formát je v pixelech, ale hodnota 0 odpovídá HORNÍMU okraji grafu.
            /// Výstup má Begin = horní souřadnice Y = Top, End = dolní souřadnice Y = Bottom (ve WinForm souřadnicích)
            /// Proto se převod opírá o reálnou velikost (realSize) = Height
            /// </summary>
            /// <param name="virtualRange"></param>
            /// <param name="realSize"></param>
            /// <returns></returns>
            public Int32Range GetRealRange(Int32Range virtualRange, int realSize)
            {
                int bottomMargin = this._GraphProperties.BottomMarginPixel;
                int begin = realSize - bottomMargin - virtualRange.Begin;
                int size = virtualRange.Size;
                return Int32Range.CreateFromBeginSize(begin, size);
            }
            /// <summary>
            /// Výška grafu v pixelech
            /// </summary>
            public int TotalPixelSize { get { return this._TotalPixelSize; } }
            /// <summary>
            /// Obsahuje true poté, kdy kalkulátor prošel přípravou.
            /// </summary>
            public bool IsPrepared { get { return this._IsPrepared; } }
            #endregion
            #region Privátní metody a proměnné
            /// <summary>
            /// Metoda vrátí virtuální souřadnici jako float, pro zadanou logickou souřadnici.
            /// Vrácená hodnota je rovna (GraphPixelHeight - 1) pro logicalY = this.UsedLogicalY.Begin (logický začátek osy Y je dole = ve Windows grafice větší souřadnice Y).
            /// Pro logickou hodnotu this.UsedLogicalY.End je vrácen pixel = 0 (logický kladný konec osy je nahoře = ve Windows grafice menší souřadnice Y).
            /// </summary>
            /// <param name="logicalY"></param>
            /// <returns></returns>
            private float _GetVirtualPosition(float logicalY)
            {
                float result = (this._Calculator_Scale * (logicalY - this._Calculator_Offset));
                return (result < 0f ? 0f : result);
            }
            /// <summary>
            /// Metoda zajistí zarovnání výšky grafu (v pixelech) do patřičného rozmezí.
            /// Využívá: rozmezí <see cref="CurrentGraphProperties"/>: <see cref="TimeGraphProperties.TotalHeightRange"/>, hodnoty Skin.Graph.TotalHeightMin a TotalHeightMax;
            /// a dále využívá objekt <see cref="VisualParent"/> a jeho <see cref="IVisualParent.ClientHeight"/>
            /// </summary>
            /// <param name="size"></param>
            /// <returns></returns>
            private int _AlignTotalPixelSize(int size)
            {
                int result = size;

                Int32NRange range = this._GraphProperties.TotalHeightRange;
                if (range != null && range.IsReal)
                    result = range.Align(result).Value;
                else
                {
                    int min = Skin.Graph.TotalHeightMin;
                    int max = Skin.Graph.TotalHeightMax;
                    result = (result < min ? min : (result > max ? max : result));
                }

                IVisualParent visualParent = this._VisualParent;
                if (visualParent != null)
                {
                    visualParent.ClientHeight = result;
                    result = visualParent.ClientHeight;
                }

                return result;
            }
            /// <summary>
            /// Majitel = graf
            /// </summary>
            private GTimeGraph _Owner;
            /// <summary>
            /// Vlastnosti grafu
            /// </summary>
            private TimeGraphProperties _GraphProperties { get { return this._Owner.CurrentGraphProperties; } }
            /// <summary>
            /// Instance objektu, jehož výšku může graf změnit i číst pro korektní přepočty svých vnitřních souřadnic.
            /// Typicky se sem vkládá řádek grafu, instance třídy <see cref="Row"/>.
            /// Graf nikdy nepracuje se šířkou parenta <see cref="IVisualParent.ClientWidth"/>.
            /// </summary>
            private IVisualParent _VisualParent { get { return this._Owner.VisualParent; } }
            /// <summary>
            /// true po úspěšné přípravě, false pokud objekt není připraven
            /// </summary>
            private bool _IsPrepared;
            /// <summary>
            /// Aktuálně použité rozmezí logických souřadnic na ose Y
            /// </summary>
            private Interval<float> _TotalLogicalRange;
            /// <summary>
            /// Aktuálně platná výška jedné logické jednotky, v pixelech
            /// </summary>
            private int _LineLogicalHeight;
            /// <summary>
            /// Úložiště hodnoty Aktuální výška dat celého grafu, v pixelech
            /// </summary>
            private int _TotalPixelSize;
            /// <summary>
            /// Offset pro kalkulátor Logical to Pixel Y
            /// </summary>
            private float _Calculator_Offset;
            /// <summary>
            /// Koeficient pro kalkulátor Logical to Pixel Y
            /// </summary>
            private float _Calculator_Scale;
            #endregion
        }
        #endregion
        #region CoordinateYVirtual = Souřadnice Y virtuální : je v pixelech, ve standardním matematickém chápání osy Y: hodnota Y = 0 je na pozici Bottom a kladné hodnoty jdou nahoru
        /// <summary>
        /// Metoda prověří platnost virtuálních souřadnic Y ve všech grupách <see cref="AllGroupList"/>.
        /// </summary>
        protected void CheckValidCoordinateYVirtual()
        {
            if (!this.IsValidCoordinateYVirtual)
                this.RecalculateCoordinateYVirtual();
        }
        /// <summary>
        /// Metoda do všech položek v poli <see cref="AllGroupList"/> vypočítá VirtualY souřadnici a vloží ji do <see cref="GTimeGraphGroup.CoordinateYVirtual"/>.
        /// Tato metoda musí proběhnout až po kompletním zmapování souřadnic LogicalY <see cref="GTimeGraphGroup.CoordinateYLogical"/>
        /// a po provedení přípravy kalkulátoru Y (<see cref="PositionCalculatorInfo.Prepare(Interval{float}, int)"/>, 
        /// protože teprve po této přípravě může být kalkulátor použit pro výpočty <see cref="PositionCalculatorInfo.GetVirtualRange(Interval{float})"/>.
        /// Není tedy možno vypočítat současně <see cref="GTimeGraphGroup.CoordinateYLogical"/> a hned poté <see cref="GTimeGraphGroup.CoordinateYVirtual"/>.
        /// </summary>
        protected void RecalculateCoordinateYVirtual()
        {
            PositionCalculatorInfo calculatorY = this.CalculatorY;
            this.AllGroupScan(groupItem => groupItem.CoordinateYVirtual = calculatorY.GetVirtualRange(groupItem.CoordinateYLogical));
            this._IsValidCoordinateYVirtual = true;
        }
        /// <summary>
        /// true pokud jsou platné souřadnice CoordinateYVirtual v grupách
        /// </summary>
        protected bool IsValidCoordinateYVirtual { get { return this._IsValidCoordinateYVirtual; } } private bool _IsValidCoordinateYVirtual;
        #endregion
        #region CoordinateYReal = Souřadnice Y reálná : je v pixelech, v koordinátech Windows.Forms: hodnota Y = 0 je na pozici Top, větší čísla jdou dolů
        /// <summary>
        /// Metoda prověří platnost reálných vizuálních souřadnic Y ve všech grupách <see cref="AllGroupList"/>, 
        /// s ohledem na výšku grafu <see cref="InteractiveObject.Bounds"/>.Height (oproti téže hodnotě uložené při poslední takové rekalkulaci v <see cref="ValidatedHeight"/>).
        /// </summary>
        protected void CheckValidCoordinateYReal()
        {
            if (this.ValidatedHeight <= 0 || this.ValidatedHeight != this.Bounds.Height)
                this.RecalculateCoordinateYReal();
        }
        /// <summary>
        /// Provede přepočet souřadnic Y Real = <see cref="GTimeGraphGroup.CoordinateYReal"/> ve všech grupách <see cref="AllGroupList"/>
        /// </summary>
        protected void RecalculateCoordinateYReal()
        {
            PositionCalculatorInfo calculatorY = this.CalculatorY;
            int height = this.Bounds.Height;
            this.AllGroupScan(groupItem => groupItem.CoordinateYReal = calculatorY.GetRealRange(groupItem.CoordinateYVirtual, height));
            this._ValidatedHeight = height;
            this.Invalidate(InvalidateItems.Bounds);
        }
        /// <summary>
        /// Výška this grafu, pro kterou byly naposledy přepočteny souřadnice Y v poli <see cref="AllGroupList"/>.
        /// </summary>
        protected int ValidatedHeight { get { return this._ValidatedHeight; } } private int _ValidatedHeight;
        #endregion
        #region TimeAxis = časová osa (X) grafu : paměť Identity časové osy, paměť Ticků na ose, instance ITimeConvertor
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální hodnoty časové osy <see cref="_TimeConvertor"/>.
        /// Pokud zdejší data jsou vypočítaná pro identický stav časové osy, nechá data beze změn, 
        /// jinak přenačte data osy a invaliduje seznam viditelných dat : <see cref="Invalidate(InvalidateItems)"/> pro hodnotu <see cref="InvalidateItems.CoordinateX"/>.
        /// </summary>
        protected void CheckValidTimeAxis()
        {
            if (!String.Equals(this.TimeAxisIdentity, this.ValidatedAxisIdentity))
                this.RecalculateTimeAxis();
        }
        /// <summary>
        /// Přenačte do sebe soupis odpovídajících dat z <see cref="_TimeConvertor"/>, 
        /// a invaliduje seznam viditelných dat : <see cref="Invalidate(InvalidateItems)"/> pro hodnotu <see cref="InvalidateItems.CoordinateX"/>.
        /// </summary>
        protected void RecalculateTimeAxis()
        {
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            if (timeConvertor == null) return;
            this._TimeAxisBegin = timeConvertor.FirstPixel;

            VisualTick[] ticks = timeConvertor.Ticks;
            if (ticks != null) ticks = ticks.Where(t => t.TickType == AxisTickType.BigLabel || t.TickType == AxisTickType.StdLabel || t.TickType == AxisTickType.BigTick).ToArray();
            else ticks = new VisualTick[0];
            this._TimeAxisTicks = ticks;

            this._ValidatedAxisIdentity = this.TimeAxisIdentity;
            this.Invalidate(InvalidateItems.CoordinateX);
        }
        /// <summary>
        /// Obsahuje pole vybraných Ticků z časové osy, protože tyto Ticky se kreslí do grafu.
        /// Obsahuje pouze ticky typu: <see cref="AxisTickType.BigLabel"/>, <see cref="AxisTickType.StdLabel"/>, <see cref="AxisTickType.BigTick"/>.
        /// </summary>
        protected VisualTick[] TimeAxisTicks { get { return this._TimeAxisTicks; } }
        private VisualTick[] _TimeAxisTicks;
        /// <summary>
        /// Relativní pozice X počátku časové osy.
        /// </summary>
        protected int TimeAxisBegin { get { return this._TimeAxisBegin; } }
        private int _TimeAxisBegin;
        /// <summary>
        /// Identita časové osy, pro kterou byly naposledy přepočítány hodnoty v <see cref="VisibleGroupList"/>.
        /// </summary>
        protected string ValidatedAxisIdentity { get { return this._ValidatedAxisIdentity; } }
        private string _ValidatedAxisIdentity;
        /// <summary>
        /// Identita časové osy aktuální, získaná z <see cref="_TimeConvertor"/>.
        /// </summary>
        protected string TimeAxisIdentity { get { return (this._TimeConvertor != null ? this._TimeConvertor.Identity : null); } }
        /// <summary>
        /// Reference na aktuální TimeConvertor
        /// </summary>
        private ITimeAxisConvertor _TimeConvertor;
        #endregion
        #region Souřadnice X : kontrolní a přepočtové metody souřadnic na ose X, algoritmy Native, Proportional, Logarithmic; určení Visible prvků do VisibleGroupList
        /// <summary>
        /// Metoda prověří platnost souřadnic X ve všech grupách <see cref="AllGroupList"/> i v jejich vnořených Items,
        /// s ohledem na aktuální časovou osu a na režim grafu <see cref="TimeGraphProperties.TimeAxisMode"/> a na rozměr grafu (Width).
        /// </summary>
        internal void CheckValidCoordinateX()
        {
            if (!this.IsValidCoordinateX)
                this.RecalculateCoordinateX();
        }
        /// <summary>
        /// Metoda přepočte všechny souřadnice X ve všech grupách <see cref="AllGroupList"/> i v jejich vnořených Items.
        /// Prvky, které jsou i jen zčásti viditelné, uloží do <see cref="_Childs"/>.
        /// </summary>
        protected void RecalculateCoordinateX()
        {
            if (this._TimeConvertor == null) return;
            TimeGraphTimeAxisMode timeAxisMode = this.CurrentGraphProperties.TimeAxisMode;

            lock (this._ValidityLock)
            {
                List<GTimeGraphGroup> visibleGroupList = new List<GTimeGraphGroup>();

                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateVisibleList", ""))
                {
                    int offsetX = this._TimeConvertor.FirstPixel;
                    int[] counters = new int[3];
                    foreach (GTimeGraphGroup[] layerList in this.AllGroupList)
                    {   // Jedna vizuální vrstva za druhou:
                        counters[0]++;
                        switch (timeAxisMode)
                        {
                            case TimeGraphTimeAxisMode.ProportionalScale:
                                foreach (GTimeGraphGroup groupItem in layerList)
                                    this.RecalculateCoordinateXProportional(visibleGroupList, groupItem, offsetX, counters);
                                break;
                            case TimeGraphTimeAxisMode.LogarithmicScale:
                                foreach (GTimeGraphGroup groupItem in layerList)
                                    this.RecalculateCoordinateXLogarithmic(visibleGroupList, groupItem, offsetX, counters);
                                break;
                            case TimeGraphTimeAxisMode.Standard:
                            default:
                                foreach (GTimeGraphGroup groupItem in layerList)
                                    this.RecalculateCoordinateXStandard(visibleGroupList, groupItem, offsetX, counters);
                                break;
                        }
                    }

                    this._ValidatedWidth = this.ClientSize.Width;
                    this._ValidatedAxisMode = timeAxisMode;
                    this._IsValidCoordinateX = true;

                    scope.AddValues(counters, "Visual Layers Count: ", "Visual Groups Count: ", "Visual Items Count: ");
                }
                this._VisibleGroupList = visibleGroupList;
            }
            this.Invalidate(InvalidateItems.Bounds);
        }
        /// <summary>
        /// Vrací true, pokud data v seznamu <see cref="VisibleGroupList"/> jsou platná.
        /// Zohledňuje i stav <see cref="VisibleGroupList"/>, <see cref="ValidatedWidth"/>, <see cref="CurrentGraphProperties"/>: <see cref="TimeGraphProperties.TimeAxisMode"/>.
        /// Hodnotu lze nastavit, ale i když se vloží true, může se vracet false (pokud výše uvedené není platné).
        /// </summary>
        protected bool IsValidCoordinateX
        {
            get
            {
                if (this._VisibleGroupList == null) return false;
                if (!this._ValidatedWidth.HasValue || this.ClientSize.Width != this._ValidatedWidth.Value) return false;
                if (!this._ValidatedAxisMode.HasValue || this.CurrentGraphProperties.TimeAxisMode != this._ValidatedAxisMode.Value) return false;
                return this._IsValidCoordinateX;
            }
        } private bool _IsValidCoordinateX;
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.Standard"/>
        /// </summary>
        /// <param name="visibleGroupList">Seznam viditelných prvků</param>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="offsetX">Ofset na ose X = posun prvků</param>
        /// <param name="counters">Počitadla</param>
        protected void RecalculateCoordinateXStandard(List<GTimeGraphGroup> visibleGroupList, GTimeGraphGroup groupItem, int offsetX, int[] counters)
        {
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            int minWidth = this.GraphItemMinPixelWidth;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetProportionalPixelRange(t, size), offsetX, minWidth, ref counters[2]);

            if (groupItem.IsValidRealTime && timeConvertor.Value.HasIntersect(groupItem.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                counters[1]++;
                visibleGroupList.Add(groupItem);
            }
        }
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.ProportionalScale"/>
        /// </summary>
        /// <param name="visibleGroupList">Seznam viditelných prvků</param>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="offsetX">Ofset na ose X = posun prvků</param>
        /// <param name="counters">Počitadla</param>
        protected void RecalculateCoordinateXProportional(List<GTimeGraphGroup> visibleGroupList, GTimeGraphGroup groupItem, int offsetX, int[] counters)
        {
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            int minWidth = this.GraphItemMinPixelWidth;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetProportionalPixelRange(t, size), offsetX, minWidth, ref counters[2]);

            if (groupItem.IsValidRealTime && timeConvertor.Value.HasIntersect(groupItem.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                counters[1]++;
                visibleGroupList.Add(groupItem);
            }
        }
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.LogarithmicScale"/>
        /// </summary>
        /// <param name="visibleGroupList">Seznam viditelných prvků</param>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="offsetX">Ofset na ose X = posun prvků</param>
        /// <param name="counters">Počitadla</param>
        protected void RecalculateCoordinateXLogarithmic(List<GTimeGraphGroup> visibleGroupList, GTimeGraphGroup groupItem, int offsetX, int[] counters)
        {
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            int minWidth = this.GraphItemMinPixelWidth;
            float proportionalRatio = this.CurrentGraphProperties.LogarithmicRatio;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetLogarithmicPixelRange(t, size, proportionalRatio), offsetX, minWidth, ref counters[2]);

            // Pozor: režim Logarithmic zajistí, že zobrazeny budou VŠECHNY prvky, takže prvky nefiltrujeme s ohledem na jejich čas : VisibleTime.HasIntersect() !
            if (groupItem.IsValidRealTime)
            {   // ... ale prvek musí mít kladný čas od Begin do End:
                counters[1]++;
                visibleGroupList.Add(groupItem);
            }
        }
        /// <summary>
        /// Hodnota <see cref="TimeGraphProperties.GraphItemMinPixelWidth"/> načtená z <see cref="CurrentGraphProperties"/>, anebo 0
        /// </summary>
        protected int GraphItemMinPixelWidth
        {
            get
            {
                var properties = this.CurrentGraphProperties;
                return (properties != null ? properties.GraphItemMinPixelWidth : 0);
            }
        }
            
        /// <summary>
        /// Seznam všech aktuálně viditelných prvků v grafu.
        /// Seznam má jednoduchou úroveň (na rozdíl od <see cref="AllGroupList"/>), ale prvky obsahuje ve správném pořadí = odspodu nahoru.
        /// Tento seznam lze použít jako přímý zdroj pro pole <see cref="_Childs"/>.
        /// </summary>
        protected List<GTimeGraphGroup> VisibleGroupList { get { this.CheckValidCoordinateX(); return this._VisibleGroupList; } } private List<GTimeGraphGroup> _VisibleGroupList;
        /// <summary>
        /// Hodnota Bounds.Width, pro kterou byly naposledy přepočítávány prvky pole <see cref="VisibleGroupList"/>.
        /// Po změně souřadnic se provádí invalidace.
        /// </summary>
        protected int? ValidatedWidth { get { return this._ValidatedWidth; } } private int? _ValidatedWidth;
        /// <summary>
        /// Hodnota <see cref="CurrentGraphProperties"/>: <see cref="TimeGraphProperties.TimeAxisMode"/>, pro kterou jsou platné hodnoty ve <see cref="VisibleGroupList"/>.
        /// Po změně <see cref="CurrentGraphProperties"/>: <see cref="TimeGraphProperties.TimeAxisMode"/> dojde k přepočtu dat v tomto seznamu.
        /// </summary>
        protected TimeGraphTimeAxisMode? ValidatedAxisMode { get { return this._ValidatedAxisMode; } } private TimeGraphTimeAxisMode? _ValidatedAxisMode;
        #endregion
        #region Bounds : souřadnice viditelných prvků
        /// <summary>
        /// Zajistí platnost souřadnic Bounds v grupách a jejich items
        /// </summary>
        protected void CheckValidBounds()
        {
            if (!this.IsValidBounds)
                this.RecalculateBounds();
        }
        /// <summary>
        /// Provede přepočet souřadnic Bounds v grupách a jejich items
        /// </summary>
        protected void RecalculateBounds()
        {
            foreach (GTimeGraphGroup groupItem in this.VisibleGroupList)
                groupItem.PrepareBounds();
            this._IsValidBounds = true;
        }
        /// <summary>
        /// true pokud jsou platné souřadnice Bounds v grupách a jejich items
        /// </summary>
        protected bool IsValidBounds { get { return this._IsValidBounds; } } private bool _IsValidBounds;
        #endregion
        #region Komunikace s datovým zdrojem: Caption, ToolTip, Link, DoubleClick, LongClick, Drag and Drop, Resize
        /// <summary>
        /// Metoda získá text, který se bude vykreslovat do prvku
        /// </summary>
        /// <param name="args">Data pro získání textu</param>
        /// <returns></returns>
        internal string GraphItemGetCaptionText(CreateTextArgs args)
        {
            string text = null;
            if (this.HasDataSource)
            {
                this.DataSource.CreateText(args);
                text = args.Text;
            }
            else
            {
                ITimeGraphItem item = ((args.Item != null) ? args.Item : args.GroupedItems[0]);
                text = item.Time.Text;
            }
            return text;
        }
        /// <summary>
        /// Metoda připraví tooltip pro daný prvek
        /// </summary>
        /// <param name="args">Kompletní data</param>
        internal void GraphItemPrepareToolTip(CreateToolTipArgs args)
        {
            ITimeGraphItem item = ((args.Item != null) ? args.Item : args.GroupedItems[0]);
            if (item == null) return;
            bool isNone = item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowToolTipNone);
            if (isNone) return;

            bool isFadeIn = item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowToolTipFadeIn);
            bool isImmediatelly = item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowToolTipImmediatelly);

            if (!isFadeIn && !isImmediatelly) return;

            ToolTipData toolTipData = args.InteractiveArgs.ToolTipData;         // Vytvoří se new instance
            string infoText;
            string eol = Environment.NewLine;
            string timeText = args.TimeText;
            if (!String.IsNullOrEmpty(item.ToolTip))
            {
                infoText = item.ToolTip;
                bool useTabs = (infoText.Contains("\t"));
                infoText = (useTabs ? timeText : timeText.Replace("\t", " ")) + infoText;

                toolTipData.TitleText = (!String.IsNullOrEmpty(item.Text) ? item.Text : "Informace");
                toolTipData.InfoText = infoText;
                toolTipData.InfoUseTabs = true;
            }
            else if (this.HasDataSource)
            {
                this.DataSource.CreateToolTip(args);
                // Tady mohla teoreticky vzniknout new instance toolTipData!
            }
            else
            {
                toolTipData.TitleText = "Tooltip " + args.Position.ToString();
                toolTipData.InfoText = timeText + 
                    "ItemId:\t" + item.ItemId + eol +
                    "Layer:\t" + item.Layer.ToString();
                toolTipData.InfoUseTabs = true;
            }

            if (args.InteractiveArgs.ToolTipIsValid)
            {
                toolTipData = args.InteractiveArgs.ToolTipData;      // V "args.InteractiveArgs.ToolTipData" může být new instance!
                if (isImmediatelly)
                {
                    toolTipData.AnimationType = TooltipAnimationType.Instant;
                }
                else if (isFadeIn)
                {
                    bool hasMouseLinks = this.GraphLinkArray.CurrentLinksMode.HasFlag(GTimeGraphLinkMode.MouseOver);   // Linky: odložíme o malý okamžik rozsvícení okna ToolTipu, aby nejdříve byly na chvilku vidět jen Linky
                    infoText = args.InteractiveArgs.ToolTipData.InfoText;
                    toolTipData.AnimationWaitBeforeTime = TimeSpan.FromMilliseconds(hasMouseLinks ? 650 : 150);
                    toolTipData.AnimationFadeInTime = TimeSpan.FromMilliseconds(hasMouseLinks ? 350 : 250);
                    toolTipData.AnimationShowTime = TimeSpan.FromMilliseconds(100 * infoText.Length);                  // 1 sekunda na přečtení 10 znaků
                    toolTipData.AnimationFadeOutTime = TimeSpan.FromMilliseconds(10 * infoText.Length);
                }
            }
        }
        /// <summary>
        /// Metoda zajistí zpracování události RightClick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="args">Kompletní data</param>
        internal void GraphItemRightClick(ItemActionArgs args)
        {
            if (!this.HasDataSource) return;

            this.DataSource.ItemRightClick(args);
            if (args.ContextMenu != null && args.ContextMenu.Items.Count > 0)
                this.GraphItemShowContextMenu(args);
        }
        /// <summary>
        /// Rozsvítí dané kontextové menu v přiměřené pozici
        /// </summary>
        /// <param name="args">Kompletní data</param>
        protected void GraphItemShowContextMenu(ItemActionArgs args)
        {
            this.GraphItemShowContextMenu(args.InteractiveArgs, args.ContextMenu);
        }
        /// <summary>
        /// Rozsvítí dané kontextové menu v přiměřené pozici
        /// </summary>
        /// <param name="args">Interaktivní argument</param>
        /// <param name="contextMenu">Kontextové menu</param>
        protected void GraphItemShowContextMenu(GInteractiveChangeStateArgs args, System.Windows.Forms.ToolStripDropDownMenu contextMenu)
        {
            var host = this.Host;
            if (host != null)
            {
                Point point = this.GetPointForMenu(args);
                contextMenu.Show(host, point, System.Windows.Forms.ToolStripDropDownDirection.BelowRight);
            }
        }
        /// <summary>
        /// Vrátí referenční bod, u kterého by se mělo rozsvítit kontextové menu
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected Point GetPointForMenu(GInteractiveChangeStateArgs e)
        {
            if (e.MouseAbsolutePoint.HasValue) return e.MouseAbsolutePoint.Value.Add(-20, 5);
            if (e.ExistsItem)
            {
                Rectangle absBounds = BoundsInfo.GetAbsoluteBounds(e.CurrentItem);
                return new Point(absBounds.X, absBounds.Bottom);
            }
            return System.Windows.Forms.Control.MousePosition;
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftDoubleCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="args">Kompletní data</param>
        internal void GraphItemLeftDoubleClick(ItemActionArgs args)
        {
            if (!this.HasDataSource) return;
            this.DataSource.ItemDoubleClick(args);
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftLongCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="args">Kompletní data</param>
        internal void GraphItemLeftLongClick(ItemActionArgs args)
        {
            if (!this.HasDataSource) return;
            this.DataSource.ItemLongClick(args);
        }
        /// <summary>
        /// Zavolá datový zdroj pro řízení akce Drag and Drop
        /// </summary>
        /// <param name="args"></param>
        internal void DragDropGroupCallSource(ItemDragDropArgs args)
        {
            if (!this.HasDataSource) return;
            this.DataSource.ItemDragDropAction(args);
        }
        /// <summary>
        /// Zavolá datový zdroj pro řízení akce Resize
        /// </summary>
        /// <param name="args"></param>
        internal void ResizeGroupCallSource(ItemResizeArgs args)
        {
            if (!this.HasDataSource) return;
            this.DataSource.ItemResizeAction(args);
        }
        /// <summary>
        /// true pokud máme datový zdroj
        /// </summary>
        protected bool HasDataSource { get { return (this._DataSource != null); } }
        /// <summary>
        /// Datový zdroj grafu
        /// </summary>
        public ITimeGraphDataSource DataSource { get { return this._DataSource; } set { this._DataSource = value; } }
        #endregion
        #region Interaktivita vlastního grafu (prostor bez prvků)
        /// <summary>
        /// Pravá myš na ploše grafu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedRightClick(GInteractiveChangeStateArgs e)
        {
            if (!this.HasDataSource) return;

            ItemActionArgs args = new ItemActionArgs(e, this, null, null, GGraphControlPosition.None);
            this.DataSource.GraphRightClick(args);
            if (args.ContextMenu != null)
                this.GraphItemShowContextMenu(e, args.ContextMenu);
        }
        /// <summary>
        /// DoubleClick myší na ploše grafu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftDoubleClick(GInteractiveChangeStateArgs e)
        {
            if (!this.HasDataSource) return;

            ItemActionArgs args = new ItemActionArgs(e, this, null, null, GGraphControlPosition.None);
            this.DataSource.GraphDoubleClick(args);
            if (args.ContextMenu != null)
                this.GraphItemShowContextMenu(e, args.ContextMenu);
        }
        /// <summary>
        /// LongClick myší na ploše grafu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftLongClick(GInteractiveChangeStateArgs e)
        {
            if (!this.HasDataSource) return;

            ItemActionArgs args = new ItemActionArgs(e, this, null, null, GGraphControlPosition.None);
            this.DataSource.GraphLongClick(args);
            if (args.ContextMenu != null)
                this.GraphItemShowContextMenu(e, args.ContextMenu);
        }
        #endregion
        #region Podpora pro Selectování DragFrame
        /// <summary>
        /// Začíná proces Drag and Frame
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedDragFrameBegin(GInteractiveChangeStateArgs e)
        {
            Rectangle dragFrameWorkArea = this.BoundsAbsolute;
            this.DragFrameWorkAreaModifyByTable(e, ref dragFrameWorkArea);
            e.DragFrameWorkArea = dragFrameWorkArea;
        }
        /// <summary>
        /// Modifikuje prostor Drag and Frame podle tabulky
        /// </summary>
        /// <param name="e"></param>
        /// <param name="dragFrameWorkArea"></param>
        protected void DragFrameWorkAreaModifyByTable(GInteractiveChangeStateArgs e, ref Rectangle dragFrameWorkArea)
        {
            Grid.GTable table = this.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
            if (table == null) return;
            Rectangle tableRowArea = table.GetAbsoluteBoundsForArea(Grid.TableAreaType.RowData);
            dragFrameWorkArea = new Rectangle(dragFrameWorkArea.X, tableRowArea.Y, dragFrameWorkArea.Width, tableRowArea.Height);
        }
        #endregion
        #region Linky grafu : koordinační objekt GTimeGraphLinkArray
        /// <summary>
        /// Reference na koordinační objekt pro kreslení linek grafu, třída: <see cref="GTimeGraphLinkItem"/>.
        /// Jednotlivé prvky grafu si mohou získávat svoje linky ke kreslení (podle svého stavu MouseOver, IsSelected atd).
        /// Vykreslování linek grafu ale není řízeno z jednotlivého prvku grafu <see cref="GTimeGraphItem"/>, 
        /// ale centrálně - právě z této instance <see cref="GraphLinkArray"/>.
        /// Tento koordinační objekt je jeden buď pro jeden graf (=všechny prvky grafu), anebo je jeden pro více grafů v jedné tabulce.
        /// Vytváření instance <see cref="GTimeGraphLinkArray"/> (a tedy i dohledání režimu) je řízeno právě v této property <see cref="GraphLinkArray"/>.
        /// <para/>
        /// Jednotlivé prvky grafu tedy mohou kdykoliv do této property vkládat nebo odebírat linky, ale nemají si své linky vykreslovat.
        /// Instance třídy <see cref="GTimeGraphLinkArray"/> do této property je nalezena / vytvořena OnDemand.
        /// </summary>
        public GTimeGraphLinkArray GraphLinkArray
        {
            get
            {
                if (this._GraphLinkArray == null)
                {   // Dosud nemáme referenci na GTimeGraphLinkArray:
                    // Podíváme se, zda máme tabulku Grid.GTable, a převezmeme její objekt:
                    Grid.GTable gTable = this.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
                    if (gTable != null)
                    {   // Použijeme sdílenou instanci. Objekt GTable si ji sám vytvoří a zařadí do svých Childs:
                        this._GraphLinkArray = gTable.GraphLinkArray;
                    }
                    else
                    {   // Nemáme k dispozici GTable: musíme si instanci GTimeGraphLinkArray vytvořit sami pro sebe:
                        this._GraphLinkArray = new GTimeGraphLinkArray(this);
                        this.GraphLinkArrayIsOnGraph = true;
                        this.Invalidate(InvalidateItems.Childs);
                    }
                }
                return this._GraphLinkArray;
            }
        }
        /// <summary>
        /// true pokud máme vytvořenou svoji zdejší instanci <see cref="GraphLinkArray"/> = výhradně pro tento graf.
        /// Pak bychom ji měli vkládat do našich Childs.
        /// false = instance neexistuje, anebo to není naše instance, nebudeme ji dávat do Childs.
        /// </summary>
        protected bool GraphLinkArrayIsOnGraph { get; private set; }
        /// <summary>
        /// Instance prvku <see cref="Graph.GTimeGraphLinkArray"/>, ať už je naše nebo cizí
        /// </summary>
        private GTimeGraphLinkArray _GraphLinkArray;
        #endregion
        #region Child items a kompletní validace
        /// <summary>
        /// Child prvky grafu = položky grafu, výhradně typu <see cref="GTimeGraphGroup"/>.
        /// Před vrácením soupisu proběhne jeho validace.
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckValid(); return this._Childs; } }
        private List<IInteractiveItem> _Childs;
        /// <summary>
        /// Metoda zajistí provedení kontroly platnosti všech vnitřních dat, podle toho která kontrola a přepočet je zapotřebí.
        /// </summary>
        internal void CheckValid()
        {
            if (this.IsValidAll) return;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "CheckValid", ""))
            {
                lock (this._ValidityLock)
                {
                    this.CheckValidAllGroupList();
                    this.CheckValidTimeAxis();
                    this.CheckValidCoordinateYVirtual();
                    this.CheckValidCoordinateYReal();
                    this.CheckValidCoordinateX();
                    this.CheckValidBounds();
                    this.CheckValidChildList();
                    this.IsValidAll = true;
                }
            }
        }
        /// <summary>
        /// Metoda prověří platnost položek v poli <see cref="_Childs"/>.
        /// </summary>
        protected void CheckValidChildList()
        {
            if (this._Childs == null)
                this.RecalculateChildList();
        }
        /// <summary>
        /// Provede korektní naplnění pole <see cref="_Childs"/> všemi prvky, které mají být viditelné a interaktivní v rámci this grafu.
        /// </summary>
        protected void RecalculateChildList()
        {
            this._Childs = new List<IInteractiveItem>();
            foreach (GTimeGraphGroup groupItem in this.VisibleGroupList)
                this._Childs.Add(groupItem.GControl);
            if (this.GraphLinkArrayIsOnGraph)
                this._Childs.Add(this._GraphLinkArray);
        }
        #endregion
        #region Draw : vykreslení grafu
        /// <summary>
        /// Systémové kreslení grafu
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            this.BoundsAbsoluteDrawed = absoluteBounds;
            this.DrawBackground(e, absoluteBounds);
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "Draw", ""))
            {
                // e.GraphicsClipWith(absoluteBounds);
                this.DrawTicks(e, absoluteBounds);
                // Vykreslení jednotlivých položek grafu neřídí graf, ale systém. 
                // Bude postupně volat kreslení všech mých Child items, což jsou GTimeGraphGroup.GControl, bude volat jejich metodu Draw(GInteractiveDrawArgs).
                // Tato metoda (v třídě GTimeGraphItem) vyvolá kreslící metodu svého Ownera: ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute).
                // Owner může kreslit podle svého uvážení, anebo může vyvolat metodu this.GControl.Draw(GInteractiveDrawArgs, Rectangle), která vykreslí prvek standardně.
            }
        }
        /// <summary>
        /// Metoda umožní udělat něco s pozadím grafu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            Color? backColor = this.BackgroundColor;
            if (backColor.HasValue)
                e.Graphics.FillRectangle(Skin.Brush(backColor.Value), absoluteBounds);

            bool forceShadow = (this.CurrentGraphProperties.TimeAxisMode == TimeGraphTimeAxisMode.LogarithmicScale);
            this.DrawBackground(e, absoluteBounds, forceShadow);
        }
        /// <summary>
        /// Metoda umožní udělat něco s pozadím grafu.
        /// Vykreslí se šedý nebo jinak barevný přechod na logaritmických okrajích, a ikonky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="forceShadow">Pokud obsahuje true = Stíny na okrajích vykreslit povinně, jde o Logaritmický graf</param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle absoluteBounds, bool forceShadow)
        {
            this.DrawBackgroundColor(e, absoluteBounds, forceShadow, this.BackgroundColor);
            this.DrawBackgroundShadow(e, absoluteBounds, forceShadow, this.BeginShadowColor, this.BeginShadowArea, RectangleSide.Left);
            this.DrawBackgroundShadow(e, absoluteBounds, forceShadow, this.EndShadowColor, this.EndShadowArea, RectangleSide.Right);
            this.DrawBackgroundImage(e, absoluteBounds, forceShadow, this.BeginImage, RectangleSide.Left);
            this.DrawBackgroundImage(e, absoluteBounds, forceShadow, this.EndImage, RectangleSide.Right);
        }
        /// <summary>
        /// Vykreslí pozadí pod grafem danou barvou, pokud je daná
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="forceShadow"></param>
        /// <param name="backgroundColor"></param>
        protected void DrawBackgroundColor(GInteractiveDrawArgs e, Rectangle absoluteBounds, bool forceShadow, Color? backgroundColor)
        {
            if (!backgroundColor.HasValue) return;
            e.Graphics.FillRectangle(Skin.Brush(backgroundColor.Value), absoluteBounds);
        }
        /// <summary>
        /// Vykreslí stínování okraje grafu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="forceShadow"></param>
        /// <param name="shadowColor"></param>
        /// <param name="shadowArea"></param>
        /// <param name="side"></param>
        protected void DrawBackgroundShadow(GInteractiveDrawArgs e, Rectangle absoluteBounds, bool forceShadow, Color? shadowColor, float? shadowArea, RectangleSide side)
        {
            if (!forceShadow && !shadowColor.HasValue) return;
            Color? edgeColor = GetBackgroundShadowEdgeColor(shadowColor, this.CurrentGraphProperties.LogarithmicGraphDrawOuterShadow);
            if (!edgeColor.HasValue) return;
            int width = GetBackgroundShadowWidth(absoluteBounds, shadowArea, this.CurrentGraphProperties.LogarithmicRatio);
            Rectangle shadowBounds = GetBackgroundShadowBounds(absoluteBounds, width, side);
            if (shadowBounds.Width > 0)
            {
                using (Brush brush = GetBackgroundShadowBrush(shadowBounds, edgeColor.Value, side))
                {
                    if (brush != null)
                        e.Graphics.FillRectangle(brush, shadowBounds);
                }
            }
        }
        /// <summary>
        /// Metoda vrátí úplně krajovou barvu, která se má použít pro vykreslení podkreslení stínu pod grafem
        /// </summary>
        /// <param name="shadowColor"></param>
        /// <param name="shadowRatio"></param>
        /// <returns></returns>
        protected static Color? GetBackgroundShadowEdgeColor(Color? shadowColor, float shadowRatio)
        {
            if (shadowColor.HasValue) return shadowColor;
            if (shadowRatio <= 0.0f) return null;
            int alpha = (int)(255f * shadowRatio);                   // Úroveň stínování 0-1 => 0-255
            return Color.FromArgb(alpha, 0, 0, 0);                   // Barva na okraji grafu = černá, s danou průhledností
        }
        /// <summary>
        /// Metoda vrátí šířku prostoru pro kreslení stínování
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="shadowArea"></param>
        /// <param name="logarithmicRatio"></param>
        /// <returns></returns>
        protected static int GetBackgroundShadowWidth(Rectangle absoluteBounds, float? shadowArea, float logarithmicRatio)
        {
            int width = absoluteBounds.Width;
            float widthRatio = ((shadowArea.HasValue) ? shadowArea.Value : ((1f - logarithmicRatio) / 2f));
            if (widthRatio <= 0f) return 0;
            if (widthRatio >= 1f) return width;
            return (int)(Math.Round((widthRatio * (float)width), 0));
        }
        /// <summary>
        /// Metoda vrátí souřadnice, kam se bude vykreslovat stín
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="width"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        protected static Rectangle GetBackgroundShadowBounds(Rectangle absoluteBounds, int width, RectangleSide side)
        {
            int x = absoluteBounds.X;
            int y = absoluteBounds.Y;
            int r = absoluteBounds.Right;
            int h = absoluteBounds.Height;
            switch (side)
            {
                case RectangleSide.Left: return new Rectangle(x, y, width, h);
                case RectangleSide.Right: return new Rectangle(r - width, y, width, h);
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vrátí Brush pro vykreslení stínovaného okraje
        /// </summary>
        /// <param name="shadowBounds"></param>
        /// <param name="edgeColor"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        protected static Brush GetBackgroundShadowBrush(Rectangle shadowBounds, Color edgeColor, RectangleSide side)
        {
            Rectangle brushBounds = shadowBounds.Enlarge(1);         // To už je taková úchylka WinFormů, že LinearGradientBrush není úplně přesný.
            Color transparentColor = Color.FromArgb(0, 0, 0, 0);
            switch (side)
            {
                case RectangleSide.Left: return new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, edgeColor, transparentColor, 00f);
                case RectangleSide.Right: return new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, edgeColor, transparentColor, 180f);
            }
            return null;
        }
        /// <summary>
        /// Vykreslí danou ikonu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="forceShadow"></param>
        /// <param name="guiImage"></param>
        /// <param name="side"></param>
        protected void DrawBackgroundImage(GInteractiveDrawArgs e, Rectangle absoluteBounds, bool forceShadow, GuiImage guiImage, RectangleSide side)
        {
            Image image = Application.App.ResourcesApp.GetImage(guiImage);
            if (image == null) return;
            Rectangle imageBounds = GetBackgroundImageBounds(absoluteBounds, image, side);
            if (imageBounds.Width > 0)
                e.Graphics.DrawImage(image, imageBounds);
        }
        /// <summary>
        /// Metoda vrátí souřadnice, kam se bude vykreslovat ikona
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="image"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        protected static Rectangle GetBackgroundImageBounds(Rectangle absoluteBounds, Image image, RectangleSide side)
        {
            if (image == null) return Rectangle.Empty;
            Size imageSize = image.Size;
            Rectangle innerBounds = absoluteBounds.Enlarge(-2);
            switch (side)
            {
                case RectangleSide.Left: return imageSize.AlignTo(innerBounds, ContentAlignment.MiddleLeft);
                case RectangleSide.Right: return imageSize.AlignTo(innerBounds, ContentAlignment.MiddleRight);
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Vykreslí všechny Ticky = časové značky, pokud se mají kreslit.
        /// </summary>
        protected void DrawTicks(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            AxisTickType tickLevel = this.CurrentGraphProperties.TimeAxisVisibleTickLevel;
            if (tickLevel == AxisTickType.None) return;
            VisualTick[] timeAxisTicks = this.TimeAxisTicks;
            if (timeAxisTicks == null) return;
            int tickLevelN = (int)tickLevel;

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "PaintGrid", ""))
            {
                int x;
                int x0 = absoluteBounds.X + this.TimeAxisBegin;
                int y1 = absoluteBounds.Top;
                int y2 = absoluteBounds.Bottom - 1;

                foreach (VisualTick tick in timeAxisTicks)
                {
                    if (((int)tick.TickType) < tickLevelN) continue;

                    x = x0 + tick.RelativePixel;
                    GPainter.DrawAxisTick(e.Graphics, tick.TickType, x, y1, x, y2, Skin.Graph.TimeAxisTickMain, Skin.Graph.TimeAxisTickSmall, true);
                }
            }
        }
        /// <summary>
        /// Pokud se překresluje graf, má se překreslit i jeho Parent
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        /// Průhlednost prvků grafu při běžném vykreslování.
        /// Má hodnotu null (neaplikuje se), nebo 0 ÷ 255. 
        /// Hodnota 255 má stejný význam jako null = plně viditelný graf. 
        /// Hodnota 0 = zcela neviditelné prvky (ale fyzicky jsou přítomné).
        /// Výchozí hodnota = null.
        public int? GraphOpacity
        {
            get { return this.CurrentGraphProperties.Opacity; }
        }
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorStandard { get { return this.CurrentGraphProperties.LinkColorStandard; } }
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorWarning { get { return this.CurrentGraphProperties.LinkColorWarning; } }
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
        public Color? LinkColorError { get { return this.CurrentGraphProperties.LinkColorError; } }
        /// <summary>
        /// Souřadnice absolutní, kam byl graf naposledy kreslen. Slouží jako OuterBounds pro pozicování textů jednotlivých prvků.
        /// Hodnota je uložena do proměnné v okamžiku každého kreslení grafu, a její čtení je tedy okamžité na rozdíl 
        /// od property <see cref="InteractiveObject.BoundsAbsolute"/>, která se vždy napočítává od Root parenta.
        /// </summary>
        public Rectangle? BoundsAbsoluteDrawed { get; protected set; }
        #endregion
        #region Invalidace je řešená jedním vstupním bodem
        /// <summary>
        /// Zajistí vykreslení this prvku Repaint(), včetně překreslení Host controlu <see cref="GInteractiveControl"/>.
        /// </summary>
        public override void Refresh()
        {
            this.Invalidate(InvalidateItems.AllGroups);
            base.Refresh();
        }
        /// <summary>
        /// Invaliduje dané prvky grafu, a automaticky přidá i prvky na nich závislé.
        /// Invalidace typu <see cref="InvalidateItems.Repaint"/> se nepřidává automaticky, tu musí volající specifikovat explicitně.
        /// </summary>
        /// <param name="items"></param>
        internal void Invalidate(InvalidateItems items)
        {
            lock (this._ValidityLock)
            {
                if ((items & InvalidateItems.AllGroups) != 0)
                {
                    this._AllGroupList = null;
                    items |= (InvalidateItems.CoordinateX | InvalidateItems.CoordinateYReal);
                }
                if ((items & InvalidateItems.CoordinateX) != 0)
                {
                    this._IsValidCoordinateX = false;
                    items |= (InvalidateItems.Bounds | InvalidateItems.Childs);
                }
                if ((items & InvalidateItems.CoordinateYVirtual) != 0)
                {
                    this._IsValidCoordinateYVirtual = false;
                    items |= (InvalidateItems.CoordinateYReal);
                }
                if ((items & InvalidateItems.CoordinateYReal) != 0)
                {
                    this._ValidatedHeight = 0;
                    items |= (InvalidateItems.Bounds | InvalidateItems.Childs);
                }
                if ((items & InvalidateItems.Bounds) != 0)
                {
                    this._IsValidBounds = false;
                    items |= InvalidateItems.Childs;
                }
                if ((items & InvalidateItems.Childs) != 0)
                {
                    this._Childs = null;
                    this.IsValidAll = false;
                    // O invalidaci Repaint si musí volající explicitně požádat:
                    // items |= InvalidateItems.Repaint;
                }
                if ((items & InvalidateItems.Repaint) != 0)
                {
                    this.Repaint();
                }
            }
        }
        /// <summary>
        /// true = po komplexním průchodu metodou <see cref="CheckValid()"/>, false po jakékoli invalidaci
        /// </summary>
        protected bool IsValidAll { get { return (this._IsValidAll && this._IsValidInternal); } set { this._IsValidAll = value; } }
        private bool _IsValidInternal
        {
            get
            {
                if (this._AllGroupList == null) return false;
                if (!String.Equals(this.TimeAxisIdentity, this.ValidatedAxisIdentity)) return false;
                if (!this.IsValidCoordinateYVirtual) return false;
                if (this.ValidatedHeight <= 0 || this.ValidatedHeight != this.Bounds.Height) return false;
                if (!this.IsValidCoordinateX) return false;
                if (!this.IsValidBounds) return false;
                if (this._Childs == null) return false;
                return true;
            }
        }
        private bool _IsValidAll;
        /// <summary>
        /// Prvky grafu, které budou invalidovány
        /// </summary>
        [Flags]
        internal enum InvalidateItems : int
        {
            /// <summary>
            /// Nic
            /// </summary>
            None = 0,
            /// <summary>
            /// Souřadnice na ose X
            /// </summary>
            CoordinateX = 1,
            /// <summary>
            /// Souřadnice na ose Y, ve virtuálních koordinátech.
            /// Invalidace virtuálních souřadnic má smysl tehdy, když se změní nějaké parametry, na jejichž základě pracuje kalkulátor Y,
            /// což je například <see cref="TimeGraphProperties.OneLineHeight"/>, nebo <see cref="TimeGraphProperties.OneLinePartialHeight"/>, nebo <see cref="TimeGraphProperties.TotalHeightRange"/>, atd.
            /// Tedy hodnoty, které mohou ovlivnit určení jednotkové výšky grafu, nebo celkové výšky grafu v pixelech, 
            /// a z toho důvodu tedy ovlivní změnu přepočtu logických jednotek na pixely.
            /// </summary>
            CoordinateYVirtual = CoordinateX << 1,
            /// <summary>
            /// Souřadnice na ose Y, v reálných koordinátech.
            /// Invalidace reálných souřadnic se provádí po změně výšky grafu, protože je nutno určit aktuální pozici položek grafu v pixelech Windows.Forms.
            /// Rovněž se má invalidovat po změně <see cref="TimeGraphProperties.BottomMarginPixel"/>, tedy odsazení dolního okraje obsahu grafu od jeho fyzického okraje.
            /// </summary>
            CoordinateYReal = CoordinateYVirtual << 1,
            /// <summary>
            /// Seznam všech grup.
            /// Po této invalidaci se nově načtou všechny položky z pole položek grafů, provede se jejich třídění a grupování, nápočet logických hodnot Y a veškeré navazující výpočty.
            /// </summary>
            AllGroups = CoordinateYReal << 1,
            /// <summary>
            /// Souřadnice Bounds
            /// </summary>
            Bounds = AllGroups << 1,
            /// <summary>
            /// Pole vizuálních Child prvků grafu.
            /// </summary>
            Childs = AllGroups << 1,
            /// <summary>
            /// Překreslení
            /// </summary>
            Repaint = Childs << 1
        }
        #endregion
        #region Konverze času a pixelů
        /// <summary>
        /// Metoda vrátí čas, odpovídající dané relativní souřadnici X.
        /// Výsledný čas může zaokrouhlit podle daného požadavku "tickType".
        /// </summary>
        /// <param name="relativePositionX">Souřadnice X, relativně k počátku osy</param>
        /// <param name="roundTickType">Režim zaokrouhlení (vzhledem k aktuálnímu rozlišení osy)</param>
        /// <returns></returns>
        public DateTime? GetTimeForPosition(int relativePositionX, AxisTickType roundTickType)
        {
            DateTime? time = this.GetTimeForPosition(relativePositionX);
            if (time.HasValue && roundTickType != AxisTickType.None)
                time = this.GetRoundedTime(time.Value, roundTickType);
            return time;
        }
        /// <summary>
        /// Metoda vrátí čas, odpovídající dané relativní souřadnici X.
        /// </summary>
        /// <param name="relativePositionX">Souřadnice X, relativně k počátku osy</param>
        /// <returns></returns>
        public DateTime? GetTimeForPosition(int relativePositionX)
        {
            if (this._TimeConvertor == null) return null;
            int pixel = relativePositionX - this._TimeConvertor.FirstPixel;
            int targetSize = this.Bounds.Width;
            switch (this.CurrentGraphProperties.TimeAxisMode)
            {
                case TimeGraphTimeAxisMode.ProportionalScale:
                    return this._TimeConvertor.GetProportionalTime(pixel, targetSize);
                case TimeGraphTimeAxisMode.LogarithmicScale:
                    return this._TimeConvertor.GetLogarithmicTime(pixel, targetSize, this.CurrentGraphProperties.LogarithmicRatio);
                case TimeGraphTimeAxisMode.Standard:
                default:
                    return this._TimeConvertor.GetProportionalTime(pixel, targetSize);
            }
        }
        /// <summary>
        /// Metoda vrátí relativní souřadnici X, odpovídající danému času.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int? GetPositionForTime(DateTime time)
        {
            if (this._TimeConvertor == null) return null;
            int targetSize = this.Bounds.Width;
            double axisPixel = 0d;
            switch (this.CurrentGraphProperties.TimeAxisMode)
            {
                case TimeGraphTimeAxisMode.ProportionalScale:
                    axisPixel = this._TimeConvertor.GetProportionalPixel(time, targetSize);
                    break;
                case TimeGraphTimeAxisMode.LogarithmicScale:
                    axisPixel = this._TimeConvertor.GetLogarithmicPixel(time, targetSize, this.CurrentGraphProperties.LogarithmicRatio);
                    break;
                case TimeGraphTimeAxisMode.Standard:
                default:
                    axisPixel = this._TimeConvertor.GetProportionalPixel(time, targetSize);
                    break;
            }
            return this._TimeConvertor.FirstPixel + (int)(Math.Round(axisPixel, 0));
        }
        /// <summary>
        /// Metoda vrátí dané datum zaokrouhlené na vhodné jednotky na aktuální časové ose.
        /// </summary>
        /// <param name="time">Dané datum, které se bude zaokrouhlovat</param>
        /// <param name="roundTickType">Režim zaokrouhlení (vzhledem k aktuálnímu rozlišení osy)</param>
        public DateTime? GetRoundedTime(DateTime time, AxisTickType roundTickType)
        {
            if (this._TimeConvertor == null) return null;
            return this._TimeConvertor.GetRoundedTime(time, roundTickType);
        }
        #endregion
        #region Podpora pro aplikační vrstvu
        /// <summary>
        /// Metoda najde a vrátí čas nejbližší danému časovému bodu ze všech svých skupin prvků.
        /// Metoda prochází svoje grupy prvků, a každý prvek předá danému timeSelectoru.
        /// Ten může danou grupu prověřit, a pokud nevyhovuje pak vrátí null. 
        /// Pokud grupa vyhovuje, pak z ní vybere patřičné datum (typicky <see cref="GTimeGraphGroup.Time"/>.Begin nebo End) a vrátí jej.
        /// Tato metoda následně prověří, zda vrácené datum spadá do časového okna timeWindow (pokud není zadáno, pak akceptuje všechna data).
        /// Vyhovující datum si poznamená, včetně jeho vzdálenosti od časového bodu timePoint.
        /// Nakonec z vyhovujících časů vrátí ten, který je nejblíže bodu timePoint.
        /// Může vrátit null, pokud se nic nenajde (nejsou grupy, nebo timeSelector nikdy nic nevrátil, nebo výsledek timeSelectoru nespadá do timeWindow).
        /// </summary>
        /// <param name="timeSelector">Funkce, která z dodané grupy prvků vrátí její čas. Pokud vrátí null, prvek se dále neakceptuje.</param>
        /// <param name="timePoint">Časový okamžik, ke kterému měříme vzdálenost</param>
        /// <param name="timeWindow">Volitelně časové okno, v němž musí být obsažen čas prvku vrácený z metody timeSelector, aby byl akceptován. Hodnota null = akceptujeme vše.</param>
        /// <returns></returns>
        public DateTime? SearchNearTime(Func<GTimeGraphGroup, DateTime?> timeSelector, DateTime timePoint, TimeRange timeWindow = null)
        {
            _SearchNearItem nearItem = this._SearchNearGroupTime(timeSelector, timePoint, timeWindow);
            return (nearItem != null ? (DateTime?)nearItem.DateTime : (DateTime?)null);
        }
        /// <summary>
        /// Metoda najde a vrátí čas nejbližší danému časovému bodu ze všech svých skupin prvků.
        /// Metoda prochází svoje grupy prvků, a každý prvek předá danému timeSelectoru.
        /// Ten může danou grupu prověřit, a pokud nevyhovuje pak vrátí null. 
        /// Pokud grupa vyhovuje, pak z ní vybere patřičné datum (typicky <see cref="GTimeGraphGroup.Time"/>.Begin nebo End) a vrátí jej.
        /// Tato metoda následně prověří, zda vrácené datum spadá do časového okna timeWindow (pokud není zadáno, pak akceptuje všechna data).
        /// Vyhovující datum si poznamená, včetně jeho vzdálenosti od časového bodu timePoint.
        /// Nakonec z vyhovujících časů vrátí ten, který je nejblíže bodu timePoint.
        /// Může vrátit null, pokud se nic nenajde (nejsou grupy, nebo timeSelector nikdy nic nevrátil, nebo výsledek timeSelectoru nespadá do timeWindow).
        /// </summary>
        /// <param name="timeSelector">Funkce, která z dodané grupy prvků vrátí její čas. Pokud vrátí null, prvek se dále neakceptuje.</param>
        /// <param name="timePoint">Časový okamžik, ke kterému měříme vzdálenost</param>
        /// <param name="nearGroup">Výstupní proměnná pro uložení grupy, která má ten nejbližší čas</param>
        /// <param name="timeWindow">Volitelně časové okno, v němž musí být obsažen čas prvku vrácený z metody timeSelector, aby byl akceptován. Hodnota null = akceptujeme vše.</param>
        /// <returns></returns>
        public DateTime? SearchNearTime(Func<GTimeGraphGroup, DateTime?> timeSelector, DateTime timePoint, out GTimeGraphGroup nearGroup, TimeRange timeWindow = null)
        {
            nearGroup = null;
            _SearchNearItem nearItem = this._SearchNearGroupTime(timeSelector, timePoint, timeWindow);
            if (nearItem == null) return null;
            nearGroup = nearItem.Group;
            return nearItem.DateTime;
        }
        /// <summary>
        /// Metoda najde a vrátí nejbližší grupu a její čas pro dané zadáníze všech svých skupin prvků.
        /// Metoda prochází svoje grupy prvků, a každý prvek předá danému timeSelectoru.
        /// Ten může danou grupu prověřit, a pokud nevyhovuje pak vrátí null. 
        /// Pokud grupa vyhovuje, pak z ní vybere patřičné datum (typicky <see cref="GTimeGraphGroup.Time"/>.Begin nebo End) a vrátí jej.
        /// Tato metoda následně prověří, zda vrácené datum spadá do časového okna timeWindow (pokud není zadáno, pak akceptuje všechna data).
        /// Vyhovující datum si poznamená, včetně jeho vzdálenosti od časového bodu timePoint.
        /// Nakonec z vyhovujících časů vrátí ten, který je nejblíže bodu timePoint.
        /// Může vrátit null, pokud se nic nenajde (nejsou grupy, nebo timeSelector nikdy nic nevrátil, nebo výsledek timeSelectoru nespadá do timeWindow).
        /// </summary>
        /// <param name="timeSelector">Funkce, která z dodané grupy prvků vrátí její čas. Pokud vrátí null, prvek se dále neakceptuje.</param>
        /// <param name="timePoint">Časový okamžik, ke kterému měříme vzdálenost</param>
        /// <param name="timeWindow">Volitelně časové okno, v němž musí být obsažen čas prvku vrácený z metody timeSelector, aby byl akceptován. Hodnota null = akceptujeme vše.</param>
        /// <returns></returns>
        private _SearchNearItem _SearchNearGroupTime(Func<GTimeGraphGroup, DateTime?> timeSelector, DateTime timePoint, TimeRange timeWindow)
        {
            List<_SearchNearItem> timeList = new List<_SearchNearItem>();
            bool hasWindow = (timeWindow != null);
            this.AllGroupScan(group =>
            {
                DateTime? dateTime = timeSelector(group);
                if (dateTime.HasValue)
                {
                    bool accept = (hasWindow ? timeWindow.Contains(dateTime) : true);
                    if (accept)
                    {
                        TimeSpan timeSpan = (dateTime.Value < timePoint ? (timePoint - dateTime.Value) : (dateTime.Value - timePoint));
                        timeList.Add(new _SearchNearItem(group, dateTime.Value, timeSpan));
                    }
                }
            });
            int count = timeList.Count;
            if (count == 0) return null;
            if (count > 1)
                timeList.Sort(_SearchNearItem.CompareByTimeSpan);
            return timeList[0];
        }
        /// <summary>
        /// Třída pro uchování mezivýsledků v metodě <see cref="_SearchNearGroupTime(Func{GTimeGraphGroup, DateTime?}, DateTime, TimeRange)"/>
        /// </summary>
        private class _SearchNearItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="group"></param>
            /// <param name="dateTime"></param>
            /// <param name="timeSpan"></param>
            public _SearchNearItem(GTimeGraphGroup group, DateTime dateTime, TimeSpan timeSpan)
            {
                this.Group = group;
                this.DateTime = dateTime;
                this.TimeSpan = timeSpan;
            }
            /// <summary>
            /// Grupa
            /// </summary>
            public GTimeGraphGroup Group { get; private set; }
            /// <summary>
            /// Její určený čas (typicky <see cref="GTimeGraphGroup.Time"/>.Begin nebo End)
            /// </summary>
            public DateTime DateTime { get; private set; }
            /// <summary>
            /// Vzdálenost času <see cref="DateTime"/> od cílového bodu
            /// </summary>
            public TimeSpan TimeSpan { get; private set; }
            /// <summary>
            /// Komparátor pro třídění TimeSpan ASC
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static int CompareByTimeSpan(_SearchNearItem a, _SearchNearItem b)
            {
                return a.TimeSpan.CompareTo(b.TimeSpan);
            }
        }
        #endregion
        #region Obecná static podpora pro grafy
        /// <summary>
        /// Vrací true, pokud se v daném režimu chování a za daného stavu má zobrazovat Caption
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="state"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        internal static bool IsCaptionVisible(GraphItemBehaviorMode mode, GInteractiveState state, bool isActive)
        {
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionNone)) return false;
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionAllways)) return true;
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionInSelected) && isActive) return true;
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionInMouseOver))
                return ((state & (GInteractiveState.FlagOver | GInteractiveState.FlagDown | GInteractiveState.FlagDrag | GInteractiveState.FlagFrame)) != 0);
            return false;
        }
        #endregion
        #region ICloneable members, GetGraphClone()
        object ICloneable.Clone()
        {
            return this.GetGraphClone(null);
        }
        /// <summary>
        /// Vrací klon grafu, který může obsahovat podmnožinu prvků danou filtrem.
        /// Klon grafu obsahuje prvky, které jsou vytvořeny klonováním prvků zdejších.
        /// </summary>
        /// <param name="cloneArgs">Data pro klonování</param>
        /// <returns></returns>
        protected GTimeGraph GetGraphClone(TableRowCloneArgs cloneArgs)
        {
            GTimeGraph gTimeGraph = new GTimeGraph(this._GuiGraph);

            bool isAdded = false;
            if (cloneArgs == null || cloneArgs.CloneGraphItems)
            {
                foreach (ITimeGraphItem sourceItem in this._ItemDict.Values)
                {
                    if (cloneArgs == null || cloneArgs.CloneGraphsFilter == null || cloneArgs.CloneGraphsFilter(sourceItem))
                    {
                        ITimeGraphItem targetItem = ((ICloneable)sourceItem.Clone()) as ITimeGraphItem;
                        gTimeGraph._AddGraphItem(targetItem, false, ref isAdded, false);
                    }
                }
            }
            if (isAdded)
                gTimeGraph.Invalidate(InvalidateItems.AllGroups);
            return gTimeGraph;
        }
        #endregion
        #region ITimeInteractiveGraph members
        ITimeAxisConvertor ITimeInteractiveGraph.TimeAxisConvertor { get { return this._TimeConvertor; } set { this._TimeConvertor = value; this.Invalidate(InvalidateItems.CoordinateX); } }
        IVisualParent ITimeInteractiveGraph.VisualParent { get { return this.VisualParent; } set { this.VisualParent = value; } }
        GTimeGraph ITimeInteractiveGraph.GetGraphClone(TableRowCloneArgs cloneArgs) { return this.GetGraphClone(cloneArgs); }
        #endregion
    }
    #region class TimeGraphProperties : třída obsahující vlastnosti vykreslovaného grafu
    /// <summary>
    /// TimeGraphProperties : třída obsahující vlastnosti vykreslovaného grafu
    /// </summary>
    public class TimeGraphProperties
    {
        /// <summary>
        /// Defaultní nastavení
        /// </summary>
        public static TimeGraphProperties Default { get { return new TimeGraphProperties(); } }
        /// <summary>
        /// Defaultní konstruktor
        /// </summary>
        public TimeGraphProperties()
        {
            this._TimeAxisMode = TimeGraphTimeAxisMode.Standard;
            this._InitialResizeMode = AxisResizeContentMode.ChangeScale;
            this._InteractiveChangeMode = AxisInteractiveChangeMode.All;
            this._TimeAxisVisibleTickLevel = AxisTickType.BigTick;
            this._UpperSpaceLogical = 1f;
            this._BottomMarginPixel = 1;
            this._TotalHeightRange = new Int32NRange(10, 300);
            this._OneLineHeight = Skin.Graph.LineHeight;
            this._LogarithmicRatio = 0.60f;
            this._LogarithmicGraphDrawOuterShadow = 0.20f;
            this._Opacity = null;
            this._TextStringFormat = StringFormatFlags.NoWrap;
        }
        /// <summary>
        /// Konstruktor s daty dle dodaného GUI objektu
        /// </summary>
        public TimeGraphProperties(GuiGraphProperties guiGraphProperties)
            : this()
        {
            if (guiGraphProperties != null)
            {
                this._TimeAxisMode = guiGraphProperties.TimeAxisMode;
                this._InitialResizeMode = guiGraphProperties.AxisResizeMode;
                this._InteractiveChangeMode = guiGraphProperties.InteractiveChangeMode;
                this._TimeAxisVisibleTickLevel = (guiGraphProperties.GraphPosition == DataGraphPositionType.InLastColumn ? AxisTickType.BigLabel : AxisTickType.None);
                this._OneLineHeight = guiGraphProperties.GraphLineHeight;
                this._OneLinePartialHeight = guiGraphProperties.GraphLinePartialHeight;
                this._UpperSpaceLogical = guiGraphProperties.UpperSpaceLogical;
                this._BottomMarginPixel = guiGraphProperties.BottomMarginPixel;
                this._TotalHeightRange = new Int32NRange(guiGraphProperties.TableRowHeightMin, guiGraphProperties.TableRowHeightMax);
                this._TextStringFormat = (guiGraphProperties.TextWordWrap ? StringFormatFlags.NoClip : StringFormatFlags.NoWrap);    // NoWrap: vykreslí každé písmenko, neřeže podle slabik,  NoClip: vykreslí každou slabiku (ale ne po písmenkách),  LineLimit: vykreslí všechno nebo nic (když se text nevejde, nevykreslí se nic)

                if (guiGraphProperties.LogarithmicRatio.HasValue)
                    this._LogarithmicRatio = guiGraphProperties.LogarithmicRatio.Value;
                if (guiGraphProperties.LogarithmicGraphDrawOuterShadow.HasValue)
                    this._LogarithmicGraphDrawOuterShadow = guiGraphProperties.LogarithmicGraphDrawOuterShadow.Value;
                this._Opacity = guiGraphProperties.Opacity;

                this._LinkColorStandard = guiGraphProperties.LinkColorStandard;
                this._LinkColorWarning = guiGraphProperties.LinkColorWarning;
                this._LinkColorError= guiGraphProperties.LinkColorError;
            }
        }
        /// <summary>
        /// Režim zobrazování času na ose X
        /// </summary>
        public TimeGraphTimeAxisMode TimeAxisMode { get { return this._TimeAxisMode; } set { this._TimeAxisMode = value; } }
        private TimeGraphTimeAxisMode _TimeAxisMode;
        /// <summary>
        /// Režim chování při změně velikosti: zachovat měřítko a změnit hodnotu End, nebo zachovat hodnotu End a změnit měřítko?
        /// </summary>
        public AxisResizeContentMode? InitialResizeMode { get { return this._InitialResizeMode; } set { this._InitialResizeMode = value; } }
        private AxisResizeContentMode? _InitialResizeMode;
        /// <summary>
        /// Barva pozadí časové osy, defaultní.
        /// Časová osa může mít definované časové segmenty, viz property <see cref="TimeAxisSegments"/>, ty mohou mít jinou barvu.
        /// </summary>
        public Color? TimeAxisBackColor { get { return this._TimeAxisBackColor; } set { this._TimeAxisBackColor = value; } }
        private Color? _TimeAxisBackColor;
        /// <summary>
        /// Výchozí zobrazovaná hodnota
        /// </summary>
        public TimeRange InitialValue { get { return this._InitialValue; } set { this._InitialValue = value; } } private TimeRange _InitialValue;
        /// <summary>
        /// Maximální dosažitelná hodnota (aby nám uživatel neodroloval úplně mimoň)
        /// </summary>
        public TimeRange MaximalValue { get { return this._MaximalValue; } set { this._MaximalValue = value; } } private TimeRange _MaximalValue;
        /// <summary>
        /// Možnosti uživatele změnit zobrazený rozsah anebo měřítko
        /// </summary>
        public virtual AxisInteractiveChangeMode? InteractiveChangeMode { get { return this._InteractiveChangeMode; } set { this._InteractiveChangeMode = value; } } private AxisInteractiveChangeMode? _InteractiveChangeMode;
        /// <summary>
        /// Hladina ticků, které se budou v grafu zobrazovat.
        /// None = žádné.
        /// Ostatní hodnoty značí, že daná hodnota a hodnoty větší budou zobrazovány.
        /// Výchozí hodnota je BigTick.
        /// Pro graf s režimem osy <see cref="TimeAxisMode"/> == <see cref="TimeGraphTimeAxisMode.Standard"/> 
        /// jsou souřadnice značek převzaty z jejich dat napřímo.
        /// Pro graf s režimem osy <see cref="TimeAxisMode"/> == <see cref="TimeGraphTimeAxisMode.ProportionalScale"/> 
        /// jsou souřadnice značek přepočteny do aktuálního prostoru.
        /// Pro graf s režimem osy <see cref="TimeAxisMode"/> == <see cref="TimeGraphTimeAxisMode.LogarithmicScale"/> 
        /// nejsou časové značky nikdy vykreslovány.
        /// </summary>
        public AxisTickType TimeAxisVisibleTickLevel
        {
            get
            {
                TimeGraphTimeAxisMode timeAxisMode = this.TimeAxisMode;
                if (timeAxisMode == TimeGraphTimeAxisMode.LogarithmicScale) return AxisTickType.None;  // Tento typ grafu (LogarithmicScale) nikdy nemá TimeAxisTick
                return this._TimeAxisVisibleTickLevel;
            }
            set
            {
                this._TimeAxisVisibleTickLevel = value;
            }
        }
        private AxisTickType _TimeAxisVisibleTickLevel;
        /// <summary>
        /// Fyzická výška jedné logické linky grafu v pixelech.
        /// Určuje, tedy kolik pixelů bude vysoký prvek, jehož logická výška <see cref="ITimeGraphItem.Height"/> = 1.0f.
        /// Výchozí hodnota je 20.
        /// Pokud graf obsahuje více položek pro jeden časový úsek (a položky jsou ze stejné vrstvy <see cref="ITimeGraphItem.Layer"/>, pak tyto prvky jsou kresleny nad sebe.
        /// Výška grafu pak bude součtem výšky těchto prvků (=logická výška), násobená výškou <see cref="OneLineHeight"/> nebo <see cref="OneLinePartialHeight"/> (pixely na jednotku výšky prvku).
        /// Hodnota <see cref="OneLineHeight"/> platí pro řádky, v nichž se vyskytují pouze prvky s celočíselnou logickou výškou.
        /// Pro řádky, kde se vyskytne výška prvku desetinná, se použije údaj <see cref="OneLinePartialHeight"/>.
        /// <para/>
        /// Lze vložit hodnotu null, pak se bude vracet defaultní výška (<see cref="Skin.Graph"/>.LineHeight)
        /// Čtení hodnoty nikdy nevrací null, vždy lze pracovat s GraphLineHeight.Value.
        /// </summary>
        public int? OneLineHeight
        {
            get { return (this._OneLineHeight.HasValue ? this._OneLineHeight.Value : Skin.Graph.LineHeight); }
            set
            {
                if (value != null)
                    this._OneLineHeight = (value < 5 ? 5 : (value > 500 ? 500 : value));
                else
                    this._OneLineHeight = null;
            }
        }
        private int? _OneLineHeight;
        /// <summary>
        /// Fyzická výška jedné logické linky grafu v pixelech, pro řádky obsahující prvky s logickou výškou <see cref="ITimeGraphItem.Height"/> desetinnou.
        /// V takových řádcích je vhodné použít větší hodnotu výšky logické linky, aby byly lépe viditelné prvky s malou výškou (např. výška prvku 0.25).
        /// Výchozí hodnota (=hodnota poté, kdy je zadáno null) je 2 * <see cref="OneLineHeight"/>.
        /// </summary>
        public int? OneLinePartialHeight
        {
            get { return (this._OneLinePartialHeight.HasValue ? this._OneLinePartialHeight.Value : 2 * this.OneLineHeight); }
            set
            {
                if (value != null)
                    this._OneLinePartialHeight = (value < 10 ? 10 : (value > 500 ? 500 : value));
                else
                    this._OneLinePartialHeight = null;
            }
        }
        private int? _OneLinePartialHeight;
        /// <summary>
        /// Horní okraj = prostor nad nejvyšším prvkem grafu, který by měl být zobrazen jako prázdný, tak aby bylo vidět že nic dalšího už není.
        /// V tomto prostoru (těsně pod souřadnicí Top) se provádí Drag and Drop prvků.
        /// Hodnota je zadána v logických jednotkách, tedy v počtu standardních linek.
        /// Výchozí hodnota = 1.0 linka, nelze zadat zápornou hodnotu.
        /// </summary>
        public float UpperSpaceLogical { get { return this._UpperSpaceLogical; } set { this._UpperSpaceLogical = (value < 0f ? 0f : value); } } private float _UpperSpaceLogical = 1f;
        /// <summary>
        /// Dolní okraj = mezera pod dolním okrajem nejnižšího prvku grafu k dolnímu okraji controlu, v pixelech.
        /// Výchozí hodnota = 1 pixel, nelze zadat zápornou hodnotu.
        /// </summary>
        public int BottomMarginPixel { get { return this._BottomMarginPixel; } set { this._BottomMarginPixel = (value < 0 ? 0 : value); } } private int _BottomMarginPixel = 1;
        /// <summary>
        /// Nejmenší šířka prvku grafu v pixelech. 
        /// Pokud by byla vypočtena šířka menší, bude zvětšena na tuto hodnotu - aby byl prvek grafu viditelný.
        /// Výchozí hodnota = 0, neprovádí se zvětšení, malé prvky (krátký čas na širokém měřítku) nejsou vidět.
        /// </summary>
        public int GraphItemMinPixelWidth { get { return this._GraphItemMinPixelWidth; } set { this._GraphItemMinPixelWidth = (value < 0 ? 0 : value); } } private int _GraphItemMinPixelWidth = 0;
        /// <summary>
        /// Rozmezí výšky celého grafu, v pixelech.
        /// Výchozí hodnota je null, pak se použije rozmezí <see cref="Skin.Graph"/>.DefaultTotalHeightMin až <see cref="Skin.Graph"/>.DefaultTotalHeightMax
        /// </summary>
        public Int32NRange TotalHeightRange
        {
            get { return this._TotalHeightRange; }
            set { this._TotalHeightRange = value; }
        }
        private Int32NRange _TotalHeightRange;
        /// <summary>
        /// Logaritmická časová osa: Rozsah lineární části grafu uprostřed logaritmické časové osy.
        /// Default = 0.60f, povolené rozmezí od 0.40f po 0.90f.
        /// </summary>
        public float LogarithmicRatio
        {
            get { return this._LogarithmicRatio; }
            set { float v = value; this._LogarithmicRatio = (v < 0.4f ? 0.4f : (v > 0.9f ? 0.9f : v)); }
        }
        private float _LogarithmicRatio;
        /// <summary>
        /// Logaritmická časová osa: vykreslovat vystínování oblastí s logaritmickým měřítkem osy 
        /// (tedy ty levé a pravé okraje, kde již neplatí lineární měřítko).
        /// Zde se zadává hodnota 0 až 1, která reprezentuje úroveň vystínování těchto okrajů.
        /// Hodnota 0.0 = žádné stínování, hodnota 1.0 = krajní pixel je zcela černý. 
        /// Default hodnota = 0.20f.
        /// </summary>
        public float LogarithmicGraphDrawOuterShadow
        {
            get { return this._LogarithmicGraphDrawOuterShadow; }
            set { float v = value; this._LogarithmicGraphDrawOuterShadow = (v < 0.0f ? 0.0f : (v > 1.0f ? 1.0f : v)); }
        }
        private float _LogarithmicGraphDrawOuterShadow;
        /// <summary>
        /// Průhlednost prvků grafu při běžném vykreslování.
        /// Má hodnotu null (neaplikuje se), nebo 0 ÷ 255. 
        /// Hodnota 255 má stejný význam jako null = plně viditelný graf. 
        /// Hodnota 0 = zcela neviditelné prvky (ale fyzicky jsou přítomné).
        /// Výchozí hodnota = null.
        /// </summary>
        public int? Opacity
        {
            get { return this._Opacity; }
            set
            {
                if (value.HasValue)
                {
                    int v = value.Value;
                    this._Opacity = (v < 0 ? 0 : (v > 255 ? 255 : v));
                }
                else
                {
                    this._Opacity = null;
                }
            }
        }
        private int? _Opacity;
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorStandard { get { return this._LinkColorStandard; } set { this._LinkColorStandard = value; } }
        private Color? _LinkColorStandard;
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorWarning { get { return this._LinkColorWarning; } set { this._LinkColorWarning = value; } }
        private Color? _LinkColorWarning;
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
        public Color? LinkColorError { get { return this._LinkColorError; } set { this._LinkColorError = value; } }
        private Color? _LinkColorError;
        /// <summary>
        /// Segmenty časové osy, které mají jinou barvu pozadí než je základní barva, a mohou obsahovat přídavný ToolTip
        /// </summary>
        public GTimeAxis.Segment[] TimeAxisSegments { get { return this._TimeAxisSegments; } set { this._TimeAxisSegments = value; } }
        private GTimeAxis.Segment[] _TimeAxisSegments;
        /// <summary>
        /// Pravidla pro vykreslování textu v prvcích grafu
        /// </summary>
        public StringFormatFlags TextStringFormat { get { return this._TextStringFormat; } set { this._TextStringFormat = value; } }
        private StringFormatFlags _TextStringFormat;
    }
    #endregion
    #region Interface ITimeInteractiveGraph, ITimeGraph, ITimeGraphItem; enum TimeGraphAxisXMode
    /// <summary>
    /// Deklarace grafu, který má časovou osu a je interaktivní
    /// </summary>
    public interface ITimeInteractiveGraph : IInteractiveItem
    {
        /// <summary>
        /// Reference na objekt, který provádí časové konverze pro tento graf.
        /// Instanci do této property plní ten, kdo ji zná.
        /// </summary>
        ITimeAxisConvertor TimeAxisConvertor { get; set; }
        /// <summary>
        /// Reference na objekt, který dovoluje grafu ovlivnit velikost svého parenta.
        /// Po přepočtu výšky grafu může graf chtít nastavit výšku (i šířku?) svého hostitele tak, aby bylo zobrazeno vše, co je třeba.
        /// </summary>
        IVisualParent VisualParent { get; set; }
        /// <summary>
        /// Vrací klon grafu, který může obsahovat podmnožinu prvků danou filtrem.
        /// Klon grafu obsahuje prvky, které jsou vytvořeny klonováním prvků zdejších.
        /// </summary>
        /// <param name="cloneArgs">Data pro klonování</param>
        /// <returns></returns>
        GTimeGraph GetGraphClone(TableRowCloneArgs cloneArgs);
    }
    /// <summary>
    /// Deklarace grafu, který má časovou osu a není interaktivní
    /// </summary>
    public interface ITimeGraph
    {
        /// <summary>
        /// Reference na objekt, který provádí časové konverze pro tento graf.
        /// Instanci do této property plní ten, kdo ji zná.
        /// </summary>
        ITimeAxisConvertor TimeConvertor { get; set; }
        /// <summary>
        /// Height (in pixels) for one unit of GTimeItem.Height
        /// </summary>
        int UnitHeight { get; }
        /// <summary>
        /// Draw content of graph
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute);
    }
    /// <summary>
    /// Předpis rozhraní pro prvky grafu
    /// </summary>
    public interface ITimeGraphItem : ICloneable
    {
        /// <summary>
        /// Graf, v němž je prvek umístěn. Hodnotu vkládá sám graf v okamžiku vložení prvku / odebrání prvku z kolekce.
        /// </summary>
        ITimeInteractiveGraph OwnerGraph { get; set; }
        /// <summary>
        /// Prvek je viditelný?
        /// Hodnotu nastavuje Graf a Tabulka při procesu klonování řádku a grafu do tabulky, a filtrování řádků podle párových Parent řádků.
        /// </summary>
        bool IsVisible { get; set; }
        /// <summary>
        /// Jednoznačný identifikátor prvku
        /// </summary>
        Int32 ItemId { get; }
        /// <summary>
        /// GroupId: číslo skupiny. Prvky se shodným GroupId budou vykreslovány do společného "rámce", 
        /// a pokud mezi jednotlivými prvky <see cref="ITimeGraphItem"/> se shodným <see cref="GroupId"/> bude na ose X nějaké volné místo,
        /// nebude mezi nimi vykreslován žádný "cizí" prvek.
        /// </summary>
        Int32 GroupId { get; }
        /// <summary>
        /// Časový interval tohoto prvku
        /// </summary>
        TimeRange Time { get; set; }
        /// <summary>
        /// Layer: Vizuální vrstva. Prvky z různých vrstev jsou kresleny "přes sebe" = mohou se překrývat.
        /// Nižší hodnota je kreslena dříve.
        /// Například: záporná hodnota Layer reprezentuje "podklad" který se needituje.
        /// </summary>
        Int32 Layer { get; }
        /// <summary>
        /// Level: Vizuální hladina. Prvky v jedné hladině jsou kresleny do společného vodorovného pásu, 
        /// další prvky ve vyšší hladině jsou všechny zase vykresleny ve svém odděleném pásu (nad tímto nižším pásem). 
        /// Nespadnou do prvků nižšího pásu i když by v něm bylo volné místo.
        /// </summary>
        Int32 Level { get; }
        /// <summary>
        /// Order: pořadí prvku při výpočtech souřadnic Y před vykreslováním. 
        /// Prvky se stejným Order budou tříděny vzestupně podle data počátku <see cref="Time"/>.Begin.
        /// </summary>
        Int32 Order { get; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.CurrentGraphProperties"/>: <see cref="TimeGraphProperties.OneLineHeight"/> nebo <see cref="TimeGraphProperties.OneLinePartialHeight"/>, 
        /// podle toho zda graf obsahuje jen celočíselné výšky, nebo i zlomkové výšky.
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// <para/>
        /// Lze explicitně vložit hodnotu NULL, pak jde o prvek, jehož výška je určena dynamicky podle výšky celého grafu.
        /// Takový prvek se může používat například pro vykreslení pracovní doby, svátků, atd.
        /// V jedné vrstvě prvků <see cref="Layer"/> mohou být pouze prvky s explicitní výškou (kde <see cref="Height"/> má hodnotu)
        /// anebo jenom výšky bez hodnoty, nelze ale v jedné vrstvě <see cref="Layer"/> kombinovat oba typy. To logicky nedává smysl.
        /// </summary>
        float? Height { get; }
        /// <summary>
        /// Text pro zobrazení uvnitř tohoto prvku.
        /// Pokud je null, bude se hledat v tabulce textů.
        /// Z databáze se načítá ze sloupce: "text", je NEPOVINNÝ.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// ToolTip pro zobrazení u tohoto tohoto prvku.
        /// Pokud je null, bude se hledat v tabulce textů.
        /// Z databáze se načítá ze sloupce: "tooltip", je NEPOVINNÝ.
        /// </summary>
        string ToolTip { get; }
        /// <summary>
        /// Barva pozadí prvku.
        /// Pokud bude null, pak prvek nebude mít vyplněný svůj prostor (obdélník). Může mít vykreslené okraje (barva <see cref="LineColor"/>).
        /// Anebo může mít kreslené Ratio (viz property <see cref="RatioBegin"/>, <see cref="RatioEnd"/>, 
        /// <see cref="RatioBeginBackColor"/>, <see cref="RatioEndBackColor"/>, <see cref="RatioLineColor"/>, <see cref="RatioLineWidth"/>).
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Barva písma
        /// </summary>
        Color? TextColor { get; }
        /// <summary>
        /// Barva šrafování prvku, kreslená stylem <see cref="BackStyle"/>.
        /// Prvek nejprve vykreslí svoje pozadí barvou <see cref="BackColor"/>, 
        /// a pokud má definovaný styl <see cref="BackStyle"/>, pak přes toto pozadí vykreslí ještě daný styl (šrafování, jiné překrytí) touto barvou.
        /// Pokud bude definován styl <see cref="BackStyle"/> a nebude daná barva <see cref="HatchColor"/>,
        /// použije se barva <see cref="LineColor"/>.
        /// </summary>
        Color? HatchColor { get; }
        /// <summary>
        /// Barva linek ohraničení prvku.
        /// Pokud je null, pak prvek nemá ohraničení pomocí linky (Border).
        /// </summary>
        Color? LineColor { get; }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// </summary>
        System.Drawing.Drawing2D.HatchStyle? BackStyle { get; }
        /// <summary>
        /// Poměrná hodnota "nějakého" splnění v rámci prvku, na jeho počátku.
        /// Běžně se vykresluje jako poměrná část prvku, měřeno odspodu, která symbolizuje míru "naplnění" daného úseku.
        /// Část Ratio má tvar lichoběžníku, a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// <para/>
        /// Pro zjednodušení zadávání: pokud je naplněno <see cref="RatioBegin"/>, ale v <see cref="RatioEnd"/> je null, 
        /// pak vykreslovací algoritmus předpokládá hodnotu End stejnou jako Begin. To znamená, že pro "obdélníkové" ratio stačí naplnit jen <see cref="RatioBegin"/>.
        /// Ale opačně to neplatí.
        /// </summary>
        float? RatioBegin { get; }
        /// <summary>
        /// Poměrná hodnota "nějakého" splnění v rámci prvku, na jeho konci.
        /// Běžně se vykresluje jako poměrná část prvku, měřeno odspodu, která symbolizuje míru "naplnění" daného úseku.
        /// Část Ratio má tvar lichoběžníku, a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// <para/>
        /// Pro zjednodušení zadávání: pokud je naplněno <see cref="RatioBegin"/>, ale v <see cref="RatioEnd"/> je null, 
        /// pak vykreslovací algoritmus předpokládá hodnotu End stejnou jako Begin. To znamená, že pro "obdélníkové" ratio stačí naplnit jen <see cref="RatioBegin"/>.
        /// Ale opačně to neplatí.
        /// </summary>
        float? RatioEnd { get; }
        /// <summary>
        /// Styl kreslení Ratio: Vertical = odspodu nahoru, Horizontal = Zleva doprava
        /// </summary>
        TimeGraphElementRatioStyle RatioStyle { get; }
        /// Barva pozadí prvku, kreslená v části Ratio, na straně času Begin.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Tato barva se použije buď jako Solid color pro celý prvek v části Ratio, 
        /// anebo jako počáteční barva na souřadnici X = čas Begin při výplni Linear, 
        /// a to tehdy, pokud je zadána i barva <see cref="RatioEndBackColor"/> (ta reprezentuje barvu na souřadnici X = čas End).
        /// Z databáze se načítá ze sloupce: "ratio_begin_back_color", je NEPOVINNÝ.
        Color? RatioBeginBackColor { get; }
        /// <summary>
        /// Barva pozadí prvku, kreslená v části Ratio, na straně času End.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Tato barva se použije jako koncová barva (na souřadnici X = čas End) v lineární výplni prostoru Ratio,
        /// kde počáteční barva výplně (na souřadnici X = čas Begin) je dána v <see cref="RatioBeginBackColor"/>.
        /// Z databáze se načítá ze sloupce: "ratio_end_back_color", je NEPOVINNÝ.
        /// </summary>
        Color? RatioEndBackColor { get; }
        /// <summary>
        /// Barva linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Touto barvou je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// </summary>
        Color? RatioLineColor { get; }
        /// <summary>
        /// Šířka linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Čárou této šířky je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// </summary>
        int? RatioLineWidth { get; }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího začátku.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude první ve směru času.
        /// </summary>
        Image ImageBegin { get; }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího konce.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude poslední ve směru času.
        /// </summary>
        Image ImageEnd { get; }
        /// <summary>
        /// Režim chování položky grafu (editovatelnost, texty, atd).
        /// </summary>
        GraphItemBehaviorMode BehaviorMode { get; }
        /// <summary>
        /// Zarovnání textu v prvku grafu
        /// </summary>
        ExtendedContentAlignment TextPosition { get; }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Editovatelný
        /// </summary>
        TimeGraphElementBackEffectStyle BackEffectEditable { get; }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Needitovatelný
        /// </summary>
        TimeGraphElementBackEffectStyle BackEffectNonEditable { get; }
        /// <summary>
        /// Vizuální prvek, který v sobě zahrnuje jak podporu pro vykreslování, tak podporu interaktivity.
        /// A přitom to nevyžaduje od třídy, která fyzicky implementuje <see cref="ITimeGraphItem"/>.
        /// Aplikační kód (implementační objekt <see cref="ITimeGraphItem"/> se o tuto property nemusí starat. Rozhodně ji nemá vlastními silami generovat.
        /// Řídící mechanismus sem vloží v případě potřeby new instanci.
        /// Implementátor pouze poskytuje úložiště pro tuto instanci.
        /// </summary>
        GTimeGraphItem GControl { get; set; }
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/> : <see cref="GTimeGraphItem.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag and Drop)</param>
        void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, DrawItemMode drawMode);
    }
    /// <summary>
    /// Tvar položky grafu
    /// </summary>
    public enum TimeGraphElementShape
    {
        // VAROVÁNÍ : Změna názvu jednotlivých enumů je zásadní změnou, která se musí promítnout i do konstant ve WorkSchedulerSupport a to jak zde, tak v Greenu.
        //            Hodnoty se z Greenu předávají v textové formě, a tady v GUI se z textu získávají parsováním (Enum.TryParse()) !
        
        /// <summary>
        /// Výchozí
        /// </summary>
        Default = 0,
        /// <summary>
        /// Obdélník
        /// </summary>
        Rectangle
    }
    /// <summary>
    /// Styl výplně pozadí prvku grafu
    /// </summary>
    public enum TimeGraphElementBackEffectStyle
    {
        /// <summary>
        /// Standardní = mírně prohnutý nahoru (tj. lehký barevný přechod)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Plochý (tj. bez barevného přechodu), ale s naznačenými 3D okraji (vlevo a nahoře světlé, vpravo a dole tmavé)
        /// </summary>
        Flat,
        /// <summary>
        /// Výrazně trubkovitý tvar (tj. uprostřed světlejší)
        /// </summary>
        Pipe,
        /// <summary>
        /// Jednoduchý = bez barevných efektů a bez 3D okrajů.
        /// Pokud nebude určena barva LineColor pak nebude mít okraje žádné, a pokud LineColor bude zadáno pak bude okraj prostá čára bez efektů.
        /// </summary>
        Simple
    }
    /// <summary>
    /// Styl vykreslení části Ratio v prvku grafu
    /// </summary>
    public enum TimeGraphElementRatioStyle
    {
        /// <summary>
        /// Nevykreslený
        /// </summary>
        None = 0,
        /// <summary>
        /// Svislé plnění odspodu nahoru
        /// </summary>
        VerticalFill,
        /// <summary>
        /// Vodorovné plnění zleva doprava, přes celou výšku elementu
        /// </summary>
        HorizontalFill,
        /// <summary>
        /// Vodorovné plnění zleva doprava, o 1-3 vnořené dovnitř elementu
        /// </summary>
        HorizontalInner
    }
    #endregion
    #region Interface ITimeGraphDataSource a příslušné třídy argumentů
    /// <summary>
    /// Deklarace zdroje dat pro graf
    /// </summary>
    public interface ITimeGraphDataSource
    {
        /// <summary>
        /// Připraví text do prvku
        /// </summary>
        /// <param name="args"></param>
        void CreateText(CreateTextArgs args);
        /// <summary>
        /// Připraví ToolTip pro prvek
        /// </summary>
        /// <param name="args"></param>
        void CreateToolTip(CreateToolTipArgs args);
        /// <summary>
        /// Najde vztahy pro daný prvek
        /// </summary>
        /// <param name="args"></param>
        void CreateLinks(CreateLinksArgs args);
        /// <summary>
        /// Vyřeší RightClick na grafu
        /// </summary>
        /// <param name="args"></param>
        void GraphRightClick(ItemActionArgs args);
        /// <summary>
        /// Vyřeší DoubleClick na grafu
        /// </summary>
        /// <param name="args"></param>
        void GraphDoubleClick(ItemActionArgs args);
        /// <summary>
        /// Vyřeší DoubleClick na grafu
        /// </summary>
        /// <param name="args"></param>
        void GraphLongClick(ItemActionArgs args);
        /// <summary>
        /// Vyřeší RightClick na prvku
        /// </summary>
        /// <param name="args"></param>
        void ItemRightClick(ItemActionArgs args);
        /// <summary>
        /// Vyřeší Double Click na prvku
        /// </summary>
        /// <param name="args"></param>
        void ItemDoubleClick(ItemActionArgs args);
        /// <summary>
        /// Vyřeší Long Click na prvku
        /// </summary>
        /// <param name="args"></param>
        void ItemLongClick(ItemActionArgs args);
        /// <summary>
        /// Řeší změnu prvku
        /// </summary>
        /// <param name="args"></param>
        void ItemChange(ItemChangeArgs args);
        /// <summary>
        /// Řeší Drag and Drop
        /// </summary>
        /// <param name="args"></param>
        void ItemDragDropAction(ItemDragDropArgs args);
        /// <summary>
        /// Řeší Resize
        /// </summary>
        /// <param name="args"></param>
        void ItemResizeAction(ItemResizeArgs args);
    }
    #region class CreateTextArgs : Argumenty pro tvobu textu (Caption)
    /// <summary>
    /// CreateTextArgs : Argumenty pro tvobu textu (Caption)
    /// </summary>
    public class CreateTextArgs : ItemArgs
    {
        #region Konstrukce a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="e"></param>
        /// <param name="fontInfo"></param>
        /// <param name="group"></param>
        /// <param name="dataItem"></param>
        /// <param name="position"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="boundsVisibleAbsolute"></param>
        public CreateTextArgs(GTimeGraph graph, GInteractiveDrawArgs e, FontInfo fontInfo, GTimeGraphGroup group, ITimeGraphItem dataItem, GGraphControlPosition position, Rectangle boundsAbsolute, Rectangle boundsVisibleAbsolute)
            : base(graph, group, dataItem, position)
        {
            this._DrawArgs = e;
            this._FontInfo = fontInfo;
            this._BoundsAbsolute = boundsAbsolute;
            this._BoundsVisibleAbsolute = boundsVisibleAbsolute;
        }
        private GInteractiveDrawArgs _DrawArgs;
        private FontInfo _FontInfo;
        private Rectangle _BoundsAbsolute;
        private Rectangle _BoundsVisibleAbsolute;
        private string _Text;
        #endregion
        #region Public data a metody
        /// <summary>
        /// Velikost prostoru pro text v grafickém prvku.
        /// Nemusí být celý ve viditelném prostoru.
        /// </summary>
        public Size GraphItemSize { get { return this._BoundsAbsolute.Size; } }
        /// <summary>
        /// Písmo, kterým bude text zobrazen.
        /// </summary>
        public FontInfo Font { get { return this._FontInfo; } }
        /// <summary>
        /// Metoda vrátí rozměry, které bude potřebovat zadaný text v aktuálním zobrazení a fontu.
        /// Metoda neukládá dodaný text do <see cref="CreateTextArgs.Text"/>, to musí zajistit datový zdroj.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Size MeasureString(string text)
        {
            return GPainter.MeasureString(this._DrawArgs.Graphics, text, this.Font);
        }
        /// <summary>
        /// Text, který se bude zobrazovat. Datový zdroj sem vloží vhodný text.
        /// </summary>
        public string Text { get { return this._Text; } set { this._Text = value; } }
        #endregion
    }
    #endregion
    #region class CreateToolTipArgs : Argument obsahující data pro přípravu tooltipu pro určitý prvek
    /// <summary>
    /// CreateToolTipArgs : Argument obsahující data pro přípravu tooltipu pro určitý prvek
    /// </summary>
    public class CreateToolTipArgs : ItemInteractiveArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="graph"></param>
        /// <param name="timeText"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public CreateToolTipArgs(GInteractiveChangeStateArgs e, GTimeGraph graph, GTimeGraphGroup group, string timeText, ITimeGraphItem data, GGraphControlPosition position)
            : base(e, graph, group, data, position)
        {
            this.TimeText = timeText;
        }
        /// <summary>
        /// Text popisující čas prvku, měl by se vložit na začátek textu tooltipu
        /// </summary>
        public string TimeText { get; private set; }
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { return this.InteractiveArgs.ToolTipData; } set { this.InteractiveArgs.ToolTipData = value; } }
    }
    #endregion
    #region class CreateLinksArgs : Argument obsahující data pro vyhledání vztahů pro určitý prvek
    /// <summary>
    /// CreateLinksArgs : Argument obsahující data pro vyhledání vztahů pro určitý prvek
    /// </summary>
    public class CreateLinksArgs : ItemArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        /// <param name="itemEvent">Druh události, pro který se Linky hledají</param>
        public CreateLinksArgs(GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position, CreateLinksItemEventType itemEvent)
            : base(graph, group, data, position)
        {
            this.SearchSidePrev = true;
            this.SearchSideNext = true;
            this.ItemEvent = itemEvent;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        /// <param name="itemEvent">Druh události, pro který se Linky hledají</param>
        /// <param name="searchSidePrev">Hledej linky na straně Prev;</param>
        /// <param name="searchSideNext">Hledej linky na straně Next;</param>
        public CreateLinksArgs(GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position, CreateLinksItemEventType itemEvent,
            bool searchSidePrev, bool searchSideNext)
            : base(graph, group, data, position)
        {
            this.SearchSidePrev = searchSidePrev;
            this.SearchSideNext = searchSideNext;
            this.ItemEvent = itemEvent;
        }
        /// <summary>
        /// Hledej linky na straně Prev;
        /// Výchozí hodnota = true
        /// </summary>
        public bool SearchSidePrev { get; private set; }
        /// <summary>
        /// Hledej linky na straně Next;
        /// Výchozí hodnota = true
        /// </summary>
        public bool SearchSideNext { get; private set; }
        /// <summary>
        /// Druh události, pro který se Linky hledají
        /// </summary>
        public CreateLinksItemEventType ItemEvent { get; private set; }
        /// <summary>
        /// Seznam vztahů pro daný prvek
        /// </summary>
        public GTimeGraphLinkItem[] Links { get; set; }
    }
    /// <summary>
    /// Typ události, pro kterou se mají vytvářet Linky
    /// </summary>
    public enum CreateLinksItemEventType
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None = 0,
        /// <summary>
        /// MouseOver
        /// </summary>
        MouseOver,
        /// <summary>
        /// Item Selected
        /// </summary>
        ItemSelected
    }
    #endregion
    #region class ItemDragDropArgs : Argument obsahující data pro Drag and Drop
    /// <summary>
    /// ItemDragDropArgs : Argument obsahující data pro Drag and Drop
    /// </summary>
    public class ItemDragDropArgs : ItemInteractiveArgs
    {
        #region Konstruktor, základní properties
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dragArgs"></param>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        /// <param name="targetAbsoluteBounds"></param>
        public ItemDragDropArgs(GDragActionArgs dragArgs, GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position, Rectangle? targetAbsoluteBounds)
            : base(dragArgs.ChangeArgs, graph, group, data, position)
        {
            this._DragArgs = dragArgs;
            this._ItemBoundsInfo = dragArgs.BoundsInfo;                   // Od teď je možno používat absolutní i relativní souřadnice
            this.BoundsOriginal = (dragArgs.DragOriginRelativeBounds.HasValue ? dragArgs.DragOriginRelativeBounds.Value : Rectangle.Empty);
            this.BoundsTargetAbsolute = (targetAbsoluteBounds.HasValue ? targetAbsoluteBounds.Value : Rectangle.Empty);    // Tady proběhne konverze Abs => Rel

            this.TargetIsValid = true;
        }
        /// <summary>
        /// Vstupní argument akce Drag and Drop
        /// </summary>
        private GDragActionArgs _DragArgs;
        /// <summary>
        /// Souřadný systém prvku grafu (Group) - pro přepočty souřadnic relativních / absolutních
        /// </summary>
        private BoundsInfo _ItemBoundsInfo;
        /// <summary>
        /// false = lze zadávat data do properties "WriteInit", true = už to nejde
        /// </summary>
        private bool _IsFinalised;
        /// <summary>
        /// Pokud <see cref="IsFinalised"/> je false, proběhne bez chyby. Pokud je true, dojde k chybě "Nelze nastavit data po inicializaci..."
        /// </summary>
        private void _CheckSet()
        {
            if (!this._IsFinalised) return;
            throw new GraphLibCodeException("Nelze nastavovat vlastnosti v instanci ItemDragDropArgs poté, kdy proběhla její finalizace.");
        }
        /// <summary>
        /// Argumenty akce Drag and Drop
        /// </summary>
        public GDragActionArgs DragArgs { get { return this._DragArgs; } }
        /// <summary>
        /// Typ aktuální akce Drag and Drop
        /// </summary>
        public DragActionType DragAction { get { return this._DragArgs.DragAction; } }
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { return this._DragArgs.ToolTipData; } }
        /// <summary>
        /// Absolutní souřadnice myši, kde se nachází nyní.
        /// Může být null pouze při akci <see cref="DragAction"/> == <see cref="DragActionType.DragThisCancel"/>.
        /// </summary>
        public Point? MouseCurrentAbsolutePoint { get { return this._DragArgs.MouseCurrentAbsolutePoint; } }
        /// <summary>
        /// Souřadnice objektu výchozí, v okamžiku startu procesu Resize.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsOriginal { get; private set; }
        /// <summary>
        /// Souřadnice objektu aktuální, v průběhu resize, před provedením aktuálního kroku.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsCurrent { get; private set; }
        /// <summary>
        /// Souřadnice objektu cílová, odvozená pouze od pozice myši.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsTarget { get; private set; }
        #endregion
        #region Absolutní souřadnice
        /// <summary>
        /// Souřadnice objektu výchozí, v okamžiku startu procesu Resize.
        /// Souřadnice je absolutní.
        /// </summary>
        public Rectangle BoundsOriginalAbsolute { get { return GetBoundsAbsolute(this.BoundsOriginal).Value; } private set { this.BoundsOriginal = GetBoundsRelative(value).Value; } }
        /// <summary>
        /// Souřadnice objektu cílová, odvozená pouze od pozice myši.
        /// Souřadnice je absolutní.
        /// </summary>
        public Rectangle BoundsTargetAbsolute { get { return GetBoundsAbsolute(this.BoundsTarget).Value; } private set { this.BoundsTarget = GetBoundsRelative(value).Value; } }
        /// <summary>
        /// Souřadnice objektu finální, potvrzená aplikací, tato hodnota se použije do prvku.
        /// Souřadnice je absolutní.
        /// </summary>
        public Rectangle? BoundsFinalAbsolute { get { return GetBoundsAbsolute(this.BoundsFinal); } set { this.BoundsFinal = GetBoundsRelative(value); } }
        /// <summary>
        /// Vrátí absolutní souřadnice pro dané relativní hodnoty
        /// </summary>
        /// <param name="boundsRelative"></param>
        /// <returns></returns>
        public Rectangle? GetBoundsAbsolute(Rectangle? boundsRelative)
        {
            return this.ItemBoundsInfo.GetAbsoluteBounds(boundsRelative);
        }
        /// <summary>
        /// Vrátí relativní souřadnice pro dané absolutní hodnoty
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <returns></returns>
        public Rectangle? GetBoundsRelative(Rectangle? boundsAbsolute)
        {
            return this.ItemBoundsInfo.GetRelativeBounds(boundsAbsolute);
        }
        /// <summary>
        /// Souřadný systém aktuálního prvku (Grupy v rámci Grafu), lze jej použít pro převody relativních a absolutních souřadnic
        /// </summary>
        public BoundsInfo ItemBoundsInfo { get { return this._ItemBoundsInfo; } }
        #endregion
        #region Properties WriteInit
        /// <summary>
        /// Obsahuje false po iniciaci. V tomto stavu lze vkládat hodnoty do všech "WriteInit" properties:
        /// <see cref="ParentGraph"/>, <see cref="ParentTable"/> (označeny "WriteInit").
        /// Po vložení všech hodnot se nastaví <see cref="IsFinalised"/> na true.
        /// Poté již nelze setovat data do "WriteInit" properties, a nelze vložit hodnotu false do property <see cref="IsFinalised"/>.
        /// </summary>
        public bool IsFinalised { get { return this._IsFinalised; } set { if (!this._IsFinalised) this._IsFinalised = value; } }
        /// <summary>
        /// Graf, v němž je nyní aktuální prvek grafu uložen (=Parent) (nikoli ten, nad kterým se zrovna přemisťuje).
        /// "WriteInit" property.
        /// </summary>
        public GTimeGraph ParentGraph { get { return this._ParentGraph; } set { this._CheckSet(); this._ParentGraph = value; } } private GTimeGraph _ParentGraph;
        /// <summary>
        /// Tabulka, v které je nyní aktuální prvek grafu uložen (=Parent) (nikoli ten, nad kterým se zrovna přemisťuje).
        /// "WriteInit" property.
        /// </summary>
        public Grid.GTable ParentTable { get { return this._ParentTable; } set { this._CheckSet(); this._ParentTable = value; } } private Grid.GTable _ParentTable;
        /// <summary>
        /// Interaktivní cílový prvek, nad nímž se nyní nachází ukazatel myši = na tento prvek "by se aktuální prvek grafu přemístil".
        /// </summary>
        public IInteractiveItem TargetItem { get { return this._TargetItem; } set { this._TargetItem = value; } } private IInteractiveItem _TargetItem;
        /// <summary>
        /// Cílový graf, nad nímž se nyní nachází ukazatel myši = do tohoto grafu "by se aktuální prvek grafu přemístil".
        /// </summary>
        public GTimeGraph TargetGraph { get { return this._TargetGraph; } set { this._TargetGraph = value; } } private GTimeGraph _TargetGraph;
        /// <summary>
        /// Cílová tabulka, nad níž se nyní nachází ukazatel myši = do této tabulky "by se aktuální prvek grafu přemístil".
        /// </summary>
        public Grid.GTable TargetTable { get { return this._TargetTable; } set { this._TargetTable = value; } } private Grid.GTable _TargetTable;
        #endregion
        #region Dopočítané vstupní proměnné
        /// <summary>
        /// true = cíl (Target) je na "domácím" grafu (Parent)
        /// </summary>
        public bool IsOnSameGraph { get { return (this.TargetGraph != null && Object.ReferenceEquals(this.ParentGraph, this.TargetGraph)); } }
        /// <summary>
        /// true = cíl (Target) je na "cizím" grafu (existující graf, jiný než Parent)
        /// </summary>
        public bool IsOnOtherGraph { get { return (this.TargetGraph != null && !Object.ReferenceEquals(this.ParentGraph, this.TargetGraph)); } }
        /// <summary>
        /// true = cíl (Target) je na "domácí" tabulce (Parent)
        /// </summary>
        public bool IsOnSameTable { get { return (this.TargetTable != null && Object.ReferenceEquals(this.ParentTable, this.TargetTable)); } }
        /// <summary>
        /// true = cíl (Target) je na "cizí" tabulce (existující tabulka, jiná než Parent)
        /// </summary>
        public bool IsOnOtherTable { get { return (this.TargetTable != null && !Object.ReferenceEquals(this.ParentTable, this.TargetTable)); } }
        /// <summary>
        /// Typ cíle
        /// </summary>
        public ItemDragTargetType TargetType
        {
            get
            {
                ItemDragTargetType targetType =
                    (this.TargetGraph != null ? (Object.ReferenceEquals(this.ParentGraph, this.TargetGraph) ? ItemDragTargetType.OnSameGraph : ItemDragTargetType.OnOtherGraph) : ItemDragTargetType.None) |
                    (this.TargetTable != null ? (Object.ReferenceEquals(this.ParentTable, this.TargetTable) ? ItemDragTargetType.OnSameTable : ItemDragTargetType.OnOtherTable) : ItemDragTargetType.None);
                if (targetType == ItemDragTargetType.None && this.TargetItem != null) targetType = ItemDragTargetType.OnItem;
                return targetType;
            }
        }
        /// <summary>
        /// Prostor (absolutní souřadnice) okolo cílového prvku (Target).
        /// </summary>
        public Rectangle? HomeAbsoluteBounds
        {
            get
            {
                Rectangle? homeBounds = null;

                if (this.TargetTable != null)
                    homeBounds = this.TargetTable.GetAbsoluteBoundsForArea(Grid.TableAreaType.RowData);

                if (this.TargetGraph != null)
                {
                    Rectangle graphBounds = this.TargetGraph.BoundsAbsolute;
                    if (homeBounds.HasValue)
                    {
                        Int32Range x = Int32Range.CreateFromRectangle(graphBounds, System.Windows.Forms.Orientation.Horizontal);
                        Int32Range y = Int32Range.CreateFromRectangle(homeBounds.Value, System.Windows.Forms.Orientation.Vertical);
                        homeBounds = Int32Range.GetRectangle(x, y);
                    }
                    else
                    {
                        homeBounds = graphBounds;
                    }
                }

                if (!homeBounds.HasValue && this.TargetItem != null)
                {
                    BoundsInfo boundsInfo = BoundsInfo.CreateForChild(this.TargetItem);
                    homeBounds = boundsInfo.CurrentItemAbsoluteBounds;
                }

                return homeBounds;
            }
        }
        #endregion
        #region Podpůrné metody
        /// <summary>
        /// Metoda vyhledá objekty <see cref="TargetItem"/>, <see cref="TargetGraph"/>, <see cref="TargetTable"/> pro danou absolutní souřadnici.
        /// </summary>
        /// <param name="targetAbsolutePoint">Absolutní souřadnice, která má být target</param>
        public void SearchForTargets(Point targetAbsolutePoint)
        {
            IInteractiveItem item = this._DragArgs.FindItemAtPoint(targetAbsolutePoint);
            this.TargetItem = item;
            this.TargetGraph = InteractiveObject.SearchForItem(item, true, typeof(GTimeGraph)) as GTimeGraph;
            this.TargetTable = InteractiveObject.SearchForItem(item, true, typeof(Grid.GTable)) as Grid.GTable;
        }
        /// <summary>
        /// Metoda vrátí čas, odpovídající dané absolutní souřadnici X
        /// </summary>
        /// <param name="absolutePositionX">Souřadnice X absolutní, odpovídá například pozici myši <see cref="MouseCurrentAbsolutePoint"/></param>
        /// <returns></returns>
        public DateTime? GetTimeForPosition(int absolutePositionX)
        {
            return this.GetTimeForPosition(absolutePositionX, AxisTickType.None);
        }
        /// <summary>
        /// Metoda vrátí čas, odpovídající dané absolutní souřadnici X
        /// </summary>
        /// <param name="absolutePositionX">Souřadnice X absolutní, odpovídá například pozici myši <see cref="MouseCurrentAbsolutePoint"/></param>
        /// <param name="roundTickType">Druh zaokrouhlení času</param>
        /// <returns></returns>
        public DateTime? GetTimeForPosition(int absolutePositionX, AxisTickType roundTickType)
        {
            int relativePositionX = this.DragArgs.BoundsInfo.GetRelativePoint(new Point(absolutePositionX, 0)).X;
            return this.Graph.GetTimeForPosition(relativePositionX, roundTickType);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnici X, odpovídající danému času.
        /// Vstupem je datum a čas, výstupem absolutní souřadnice X.
        /// </summary>
        /// <param name="time">Datum a čas, jehož pozici hledáme</param>
        /// <returns>Absoutní souřadnice X</returns>
        public int? GetPositionForTime(DateTime time)
        {
            int? relativePositionX = this.Graph.GetPositionForTime(time);
            if (!relativePositionX.HasValue) return null;
            return this.DragArgs.BoundsInfo.GetAbsolutePoint(new Point(relativePositionX.Value, 0)).X;
        }
        /// <summary>
        /// Metoda vrátí dané datum zaokrouhlené na vhodné jednotky na aktuální časové ose.
        /// </summary>
        /// <param name="time">Daný přesný čas</param>
        /// <param name="tickType">Druh zaokrouhlení, odpovídá typu značky na časové ose</param>
        public DateTime? GetRoundedTime(DateTime time, AxisTickType tickType)
        {
            return this.Graph.GetRoundedTime(time, tickType);
        }
        #endregion
        #region Výstupní proměnné
        /// <summary>
        /// Souřadnice objektu finální, potvrzená aplikací, tato hodnota se použije do prvku.
        /// Výchozí hodnota = <see cref="BoundsTarget"/>, ale aplikace ji může změnit.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle? BoundsFinal { get; set; }
        /// <summary>
        /// Vyjadřuje platnost cílové souřadnice (pozice Drag and Drop) pro aktuální prvek.
        /// Hodnota 1 je výchozí a značí ANO, prvek se na dané místo může přemístit, hodnota menší než 1 = nemůže.
        /// Hodnota má vliv na způsob vykreslení prvku a vyjadřuje úroveň "viditelnosti": 
        /// 0 = prvek je neviditelný, např. 0.75 = prvek je viditelný na 75%, 1.00 = prvek je plně viditelný (řídí se úroveň Alpha kanálu).
        /// Stačilo by true / false, ale svět není jen binární :-)
        /// </summary>
        public decimal TargetValidityRatio { get; set; }
        /// <summary>
        /// Vyjadřuje hodnotu Opacity odpovídající hodnotě <see cref="TargetValidityRatio"/>:
        /// Pro Ratio = 1.0 (a vyšší) obsahuje 255, pro Ratio = 0.0 (a nižší) obsahuje 0.
        /// </summary>
        public int? TargetValidityOpacity { get { decimal tvr = this.TargetValidityRatio; return (tvr <= 0.0m ? 0 : (tvr >= 1.0m ? 255 : (int)(Math.Round(255m * tvr, 0)))); } }
        /// <summary>
        /// Platnost cílové souřadnice (pozice Drag and Drop) pro aktuální prvek.
        /// Obsahuje true, pokud <see cref="TargetValidityRatio"/> obsahuje 1.0 a více, obsahuje false pro menší hodnotu.
        /// Setování do <see cref="TargetIsValid"/> vloží do <see cref="TargetValidityRatio"/> hodnotu 1.0 (pro true) nebo 0.333 (pro false).
        /// </summary>
        public bool TargetIsValid { get { return (this.TargetValidityRatio >= 1.0m); } set { this.TargetValidityRatio = (value ? 1.0m : 0.333m); } }
        /// <summary>
        /// Objekt, na který reálně má být přemisťovaný prvek "upuštěn" = jeho Child by měl být.
        /// Ve výchozím stavu je nastaveno na <see cref="TargetItem"/>, ale může být změněno.
        /// </summary>
        public IInteractiveItem TargetDropItem { get; set; }
        #endregion
    }
    /// <summary>
    /// Umístění cílového prostoru pro prvek grafu
    /// </summary>
    [Flags]
    public enum ItemDragTargetType
    {
        /// <summary>Nic</summary>
        None = 0,
        /// <summary>Na tentýž prvek</summary>
        OnItem = 0x0001,
        /// <summary>Na tentýž graf</summary>
        OnSameGraph = 0x0010,
        /// <summary>Na jiný graf</summary>
        OnOtherGraph = 0x0020,
        /// <summary>Na nějaký graf</summary>
        OnGraph = OnSameGraph | OnOtherGraph,
        /// <summary>Na stejnou tabulku</summary>
        OnSameTable = 0x0100,
        /// <summary>Na jinou tabulku</summary>
        OnOtherTable = 0x0200,
        /// <summary>Na nějakou tabulku</summary>
        OnTable = OnSameTable | OnOtherTable
    }
    #endregion
    #region class ItemResizeArgs : Argument obsahující data pro Resize
    /// <summary>
    /// ItemResizeArgs : Argument obsahující data pro Resize
    /// </summary>
    public class ItemResizeArgs : ItemInteractiveArgs
    {
        #region Konstruktor, základní properties
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="resizeArgs"></param>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        /// <param name="itemBoundsInfo"></param>
        /// <param name="timeRangeTarget"></param>
        public ItemResizeArgs(ResizeObjectArgs resizeArgs, GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position, BoundsInfo itemBoundsInfo, TimeRange timeRangeTarget)
            : base(resizeArgs.ChangeArgs, graph, group, data, position)
        {
            this._ResizeArgs = resizeArgs;
            this._ItemBoundsInfo = itemBoundsInfo;
            this._TimeRangeTarget = timeRangeTarget;
            this._AbsOrigin = itemBoundsInfo.AbsolutePhysicalOriginPoint;
            this.BoundsFinal = resizeArgs.BoundsTarget;
            this.TimeRangeFinal = timeRangeTarget;
        }
        /// <summary>
        /// Vstupní argument akce Resize
        /// </summary>
        private ResizeObjectArgs _ResizeArgs;
        /// <summary>
        /// Souřadný systém prvku grafu
        /// </summary>
        private BoundsInfo _ItemBoundsInfo;
        /// <summary>
        /// Absolutní souřadnice počátku prostoru relativních hodnot Bounds = počáteční souřadnice počátku grafu, v němž se aktuální prvek pohybuje
        /// </summary>
        private Point _AbsOrigin;
        /// <summary>
        /// Časový interval cílový
        /// </summary>
        private TimeRange _TimeRangeTarget;
        /// <summary>
        /// Absolutní souřadnice myši, kde se nachází nyní.
        /// Může být null pouze při akci <see cref="ResizeAction"/> == <see cref="DragActionType.DragThisCancel"/>.
        /// </summary>
        public Point? MouseCurrentAbsolutePoint { get { return this._ResizeArgs.DragArgs.MouseCurrentAbsolutePoint; } }
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { return this._ResizeArgs.ToolTipData; } set { this._ResizeArgs.ToolTipData = value; } }
        /// <summary>
        /// Hrana prvku, která je přemísťována
        /// </summary>
        public RectangleSide ResizeSide { get { return this._ResizeArgs.ChangedSide; } }
        /// <summary>
        /// Typ akce (start, pohyb, cancel, ukončení)
        /// </summary>
        public DragActionType ResizeAction { get { return this._ResizeArgs.ResizeAction; } }
        /// <summary>
        /// Souřadnice objektu výchozí, v okamžiku startu procesu Resize.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsOriginal { get { return this._ResizeArgs.BoundsOriginal; } }
        /// <summary>
        /// Souřadnice objektu aktuální, v průběhu resize, před provedením aktuálního kroku.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsCurrent { get { return this._ResizeArgs.BoundsCurrent; } }
        /// <summary>
        /// Souřadnice objektu cílová, odvozená pouze od pozice myši.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsTarget { get { return this._ResizeArgs.BoundsTarget; } }
        /// <summary>
        /// Časový interval prvku cílový, odvozená pouze od pozice myši.
        /// </summary>
        public TimeRange TimeRangeTarget { get { return this._TimeRangeTarget; } }
        #endregion
        #region Absolutní souřadnice
        /// <summary>
        /// Souřadnice objektu výchozí, v okamžiku startu procesu Resize.
        /// Souřadnice je absolutní.
        /// </summary>
        public Rectangle BoundsOriginalAbsolute { get { return GetBoundsAbsolute(this.BoundsOriginal).Value; } }
        /// <summary>
        /// Souřadnice objektu cílová, odvozená pouze od pozice myši.
        /// Souřadnice je absolutní.
        /// </summary>
        public Rectangle BoundsTargetAbsolute { get { return GetBoundsAbsolute(this.BoundsTarget).Value; } }
        /// <summary>
        /// Souřadnice objektu finální, potvrzená aplikací, tato hodnota se použije do prvku.
        /// Souřadnice je absolutní.
        /// </summary>
        public Rectangle? BoundsFinalAbsolute { get { return GetBoundsAbsolute(this.BoundsFinal); } set { this.BoundsFinal = GetBoundsRelative(value); } }
        /// <summary>
        /// Vrátí absolutní souřadnice pro dané relativní hodnoty
        /// </summary>
        /// <param name="boundsRelative"></param>
        /// <returns></returns>
        public Rectangle? GetBoundsAbsolute(Rectangle? boundsRelative)
        {
            return this.ItemBoundsInfo.GetAbsoluteBounds(boundsRelative);
        }
        /// <summary>
        /// Vrátí relativní souřadnice pro dané absolutní hodnoty
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <returns></returns>
        public Rectangle? GetBoundsRelative(Rectangle? boundsAbsolute)
        {
            return this.ItemBoundsInfo.GetRelativeBounds(boundsAbsolute);
        }
        /// <summary>
        /// Souřadný systém aktuálního prvku (Grupy v rámci Grafu), lze jej použít pro převody relativních a absolutních souřadnic
        /// </summary>
        public BoundsInfo ItemBoundsInfo { get { return this._ItemBoundsInfo; } }
        #endregion
        #region Podpůrné metody
        /// <summary>
        /// Metoda vrátí čas, odpovídající dané absolutní souřadnici X
        /// </summary>
        /// <param name="absolutePositionX">Souřadnice X absolutní, odpovídá například pozici myši <see cref="MouseCurrentAbsolutePoint"/></param>
        /// <returns></returns>
        public DateTime? GetTimeForPosition(int absolutePositionX)
        {
            return this.GetTimeForPosition(absolutePositionX, AxisTickType.None);
        }
        /// <summary>
        /// Metoda vrátí čas, odpovídající dané absolutní souřadnici X
        /// </summary>
        /// <param name="absolutePositionX">Souřadnice X absolutní, odpovídá například pozici myši <see cref="MouseCurrentAbsolutePoint"/></param>
        /// <param name="roundTickType">Druh zaokrouhlení času</param>
        /// <returns></returns>
        public DateTime? GetTimeForPosition(int absolutePositionX, AxisTickType roundTickType)
        {
            int relativePositionX = this._ItemBoundsInfo.GetRelativePoint(new Point(absolutePositionX, 0)).X;
            return this.Graph.GetTimeForPosition(relativePositionX, roundTickType);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnici X, odpovídající danému času.
        /// Vstupem je datum a čas, výstupem absolutní souřadnice X.
        /// </summary>
        /// <param name="time">Datum a čas, jehož pozici hledáme</param>
        /// <returns>Absoutní souřadnice X</returns>
        public int? GetPositionForTime(DateTime time)
        {
            int? relativePositionX = this.Graph.GetPositionForTime(time);
            if (!relativePositionX.HasValue) return null;
            return this._ItemBoundsInfo.GetAbsolutePoint(new Point(relativePositionX.Value, 0)).X;
        }
        /// <summary>
        /// Metoda vrátí dané datum zaokrouhlené na vhodné jednotky na aktuální časové ose.
        /// </summary>
        /// <param name="time">Daný přesný čas</param>
        /// <param name="tickType">Druh zaokrouhlení, odpovídá typu značky na časové ose</param>
        public DateTime? GetRoundedTime(DateTime time, AxisTickType tickType)
        {
            return this.Graph.GetRoundedTime(time, tickType);
        }
        #endregion
        #region Výstupní proměnné
        /// <summary>
        /// Souřadnice objektu finální, potvrzená aplikací, tato hodnota se použije do prvku.
        /// Výchozí hodnota = <see cref="BoundsTarget"/>, ale aplikace ji může změnit.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle? BoundsFinal { get; set; }
        /// <summary>
        /// Časový interval prvku finální, potvrzený aplikací, tato hodnota se použije do prvku.
        /// Výchozí hodnota = <see cref="TimeRangeTarget"/>, ale aplikace ji může změnit.
        /// </summary>
        public TimeRange TimeRangeFinal { get; set; }
        #endregion
    }
    #endregion
    #region class ItemActionArgs : Argument obsahující data prosté akce
    /// <summary>
    /// ItemActionArgs : Argument obsahující data prosté akce
    /// </summary>
    public class ItemActionArgs : ItemInteractiveArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public ItemActionArgs(GInteractiveChangeStateArgs e, GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
            : base(e, graph, group, data, position)
        { }
        /// <summary>
        /// Kontextové menu, které se má v místě kliknutí rozsvítit. Toto menu vytváří datový zdroj jako reakci na probíhající akci (typicky RightClick).
        /// </summary>
        public System.Windows.Forms.ToolStripDropDownMenu ContextMenu { get; set; }
    }
    #endregion
    #region class ItemChangeArgs : Argument obsahující data akce se změnou prvku (přesun)
    /// <summary>
    /// ItemChangeArgs : Argument obsahující data akce se změnou prvku (přesun)
    /// </summary>
    public class ItemChangeArgs
    {

    }
    #endregion
    #region class ItemInteractiveArgs : Bázová třída pro všechny argumenty interaktivních metod, které jsou postaveny nad grupou prvků grafu a nad jedním prvek z této grupy
    /// <summary>
    /// ItemInteractiveArgs : Bázová třída pro všechny argumenty interaktivních metod, které jsou postaveny nad grupou prvků grafu a nad jedním prvek z této grupy
    /// </summary>
    public abstract class ItemInteractiveArgs : ItemArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public ItemInteractiveArgs(GInteractiveChangeStateArgs e, GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
            : base(graph, group, data, position)
        {
            this.InteractiveArgs = e;
        }
        /// <summary>
        /// Interaktivní argument
        /// </summary>
        public GInteractiveChangeStateArgs InteractiveArgs { get; private set; }
        /// <summary>
        /// Typ interaktivní akce
        /// </summary>
        public GInteractiveChangeState ActionType { get { return this.InteractiveArgs.ChangeState; } }
        /// <summary>
        /// Absolutní pozice myši v okamžiku vzniku akce
        /// </summary>
        public Point? ActionPoint { get { return this.InteractiveArgs.MouseAbsolutePoint; } }
        /// <summary>
        /// Modifier keys v době vzniku akce (Ctrl, Shift, Alt)
        /// </summary>
        public System.Windows.Forms.Keys ModifierKeys { get { return this.InteractiveArgs.ModifierKeys; } }
    }
    #endregion
    #region class ItemArgs : Bázová třída pro všechny argumenty, které jsou postaveny nad grupou prvků grafu a nad jedním prvkem z této grupy
    /// <summary>
    /// ItemArgs : Bázová třída pro všechny argumenty, které jsou postaveny nad grupou prvků grafu a nad jedním prvkem z této grupy
    /// </summary>
    public abstract class ItemArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public ItemArgs(GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            this.Graph = graph;
            this.Group = group;
            this.Item = (position == GGraphControlPosition.Item ? data :
                        (position == GGraphControlPosition.Group ? group.Items[0] : null));
            this.Position = position;
        }
        /// <summary>
        /// Graf, v němž došlo k události
        /// </summary>
        public GTimeGraph Graph { get; protected set; }
        /// <summary>
        /// Grupa položek.
        /// Nikdy není null, každá událost se týká grupy nebo prvku (a každý prvek patří do grupy).
        /// </summary>
        public GTimeGraphGroup Group { get; protected set; }
        /// <summary>
        /// Vizuální control grupy, reprezentuje spojovací linii mezi fyzickými prvky grafu.
        /// Nikdy není null.
        /// </summary>
        public GTimeGraphItem GroupControl { get { return this.Group?.GControl; } }
        /// <summary>
        /// Přímo ten prvek, jehož se týká akce (na který bylo kliknuto).
        /// Může být null, pokud se akce týká výhradně spojovacího prvku mezi fyzickými prvky grafu = když bylo kliknuto na "spojovací linii mezi prvky".
        /// Pak je třeba vyhodnotit prvky v <see cref="GroupedItems"/> = všechny prvky grupy.
        /// </summary>
        public ITimeGraphItem Item { get; protected set; }
        /// <summary>
        /// Vizuální control prvku <see cref="Item"/>, může být null.
        /// </summary>
        public GTimeGraphItem ItemControl { get { return this.Item?.GControl; } }
        /// <summary>
        /// Skupina prvků, jejíhož člena se akce týká, nebo jejíž spojovací linie se akce týká.
        /// Nikdy není null, vždy obsahuje alespoň jeden prvek.
        /// </summary>
        public ITimeGraphItem[] GroupedItems { get { return this.Group?.Items; } }
        /// <summary>
        /// Typ prvku, kterého se akce týká (Item / Group).
        /// </summary>
        public GGraphControlPosition Position { get; protected set; }
    }
    #endregion
    #endregion
}
