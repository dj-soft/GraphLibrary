using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    // Prvek GTimeGraph je tak líný, jako by to odkoukal od GGridu a GTable.
    // Na veškeré vstupní změny reaguje líně, jenom si poznamená: "tohle nebo tamto je od teď neplatné"
    //  Například: po změně prvků v poli this.ItemList si jen nulluje pole this._GroupList
    //  anebo po změně výšky Bounds.Height nastaví příznak this.GroupItemsYValid = false.
    // Následně teprve když instance GTimeGraph má něco ze sebe vydat (typicky když má vrátit pole Childs), 
    //  tak s hrůzou zjistí, co vše není platné, a honem to dopočítá.
    // Další infromace k instanci GTimeGraph : jde o plnohodnotný Container, jehož Child prvky jsou prvky typu GTimeGraphGroup.
    // Tyto prvky mají určenou svoji souřadnici na ose Y v rámci grafu GTimeGraph.

    /*     Chování s ohledem na prostor grafu (Bounds.Size):

         a) Pokud dojde ke změně šířky (this.Bounds.Width), detekuje to časová osa (CheckValidTimeAxis() na základě TimeAxisIdentity, kterážto obsahuje šířku osy)
              Důsledkem toho bude invalidace VisibleGroups a následný přepočet obsahu VisibleGroupList
         b) Pokud dojde ke změně výšky (this.Bounds.Height), detekuje to koordinát Y (CheckValidCoordinateY() na základě 








    */


    /// <summary>
    /// Graf na časové ose
    /// </summary>
    public class GTimeGraph : InteractiveContainer, ITimeInteractiveGraph
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
        {
            this._ItemList = new EList<ITimeGraphItem>();
            this._ItemList.ItemAddAfter += new EList<ITimeGraphItem>.EListEventAfterHandler(_ItemList_ItemAddAfter);
            this._ItemList.ItemRemoveAfter += new EList<ITimeGraphItem>.EListEventAfterHandler(_ItemList_ItemRemoveAfter);
        }
        /// <summary>
        /// Všechny prvky grafu (časové úseky)
        /// </summary>
        public EList<ITimeGraphItem> ItemList { get { return this._ItemList; } } private EList<ITimeGraphItem> _ItemList;
        /// <summary>
        /// Eventhandler události: z <see cref="ItemList"/> byla odebrána položka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemList_ItemRemoveAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { this.Invalidate(InvalidateItems.AllGroups); }
        /// <summary>
        /// Eventhandler události: do <see cref="ItemList"/> byla přidána položka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemList_ItemAddAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { this.Invalidate(InvalidateItems.AllGroups); }
        #endregion
        #region GUI grafu : GraphParameters
        /// <summary>
        /// Parametry pro tento graf.
        /// Buď jsou uloženy přímo zde jako explicitní, nebo jsou načteny z parenta, nebo jsou použity defaultní.
        /// Nikdy nevrací null.
        /// Lze setovat parametry, nebo null.
        /// </summary>
        public TimeGraphProperties GraphParameters
        {
            get
            {
                TimeGraphProperties gp = this._GraphParameters;
                if (gp == null)
                    gp = this._SearchParentGraphParameters();
                if (gp == null)
                    gp = this._GetDefaultGraphParameters();
                return gp;
            }
            set
            {
                this._GraphParameters = value;
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
            if (this._GraphParametersDefault == null)
                this._GraphParametersDefault = TimeGraphProperties.Default;
            return this._GraphParametersDefault;
        }
        private TimeGraphProperties _GraphParameters;
        private TimeGraphProperties _GraphParametersDefault;
        #endregion
        #region AllGroupList : Seskupování položek z this.ItemList do skupin GTimeGraphGroup, setřídění těchto skupin podle vrstev a hladin na logické ose Y
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální logické souřadnice Y.
        /// Pokud jsou neplatné, znovu vytvoří pole <see cref="AllGroupList"/> a vypočítá logické souřadnice Y.
        /// </summary>
        protected void CheckValidAllGroupList()
        {
            if (this._AllGroupList == null)
                this.RecalculateAllGroupList();
        }
        /// <summary>
        /// Vypočítá logické souřadnice Y pro všechny položky pole <see cref="ItemList"/>
        /// </summary>
        protected void RecalculateAllGroupList()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateLogY", ""))
            {
                int layers = 0;
                int levels = 0;
                int groups = 0;
                int items = this.ItemList.Count;

                this._AllGroupList = new List<List<GTimeGraphGroup>>();
                Interval<float> usedLogicalY = new Interval<float>(0f, 0f, true);

                // Vytvoříme oddělené skupiny prvků, podle jejich příslušnosti do grafické vrstvy (ITimeGraphItem.Layer), vzestupně:
                List<IGrouping<int, ITimeGraphItem>> layerGroups = this.ItemList.GroupBy(i => i.Layer).ToList();
                if (layerGroups.Count > 1)
                    layerGroups.Sort((a, b) => a.Key.CompareTo(b.Key));        // Vrstvy setřídit podle Key = ITimeGraphItem.Layer, vzestupně
                layers = layerGroups.Count;

                foreach (IGrouping<int, ITimeGraphItem> layerGroup in layerGroups)
                {   // Každá vrstva (layerGroup) má svoje vlastní pole využití prostoru, toto pole je společné pro všechny ITimeGraphItem.Level
                    PointArray<DateTime, IntervalArray<float>> layerUsing = new PointArray<DateTime, IntervalArray<float>>();

                    // Hodnota Layer pro tuto skupinu. 
                    // Jedna vrstva Layer je ekvivalentní jedné grafické vrstvě, položky z různých vrstev jsou kresleny jedna přes druhou.
                    int layer = layerGroup.Key;

                    // V rámci jedné vrstvy: další grupování jejích prvků podle jejich hodnoty ITimeGraphItem.Level, vzestupně:
                    List<IGrouping<int, ITimeGraphItem>> levelGroups = layerGroup.GroupBy(i => i.Level).ToList();
                    if (levelGroups.Count > 1)
                        levelGroups.Sort((a, b) => a.Key.CompareTo(b.Key));    // Hladiny setřídit podle Key = ITimeGraphItem.Level, vzestupně
                    levels += levelGroups.Count;

                    // Hladina (Level) má význam "vodorovného pásu" pro více prvků stejné hladiny.
                    // Záporné hladiny jsou kresleny dolů (jako záporné hodnoty na ose Y).

                    // Nyní zpracuji grafické prvky dané vrstvy (layerGroup) po jednotlivých skupinách za hladiny Level (levelGroups),
                    // vypočtu jejich logické souřadnice Y a přidám je do ItemGroupList:
                    Interval<float> layerUsedLogicalY = new Interval<float>(0f, 0f, true);
                    List<GTimeGraphGroup> layerGroupList = new List<GTimeGraphGroup>();
                    foreach (IGrouping<int, ITimeGraphItem> levelGroup in levelGroups)
                    {
                        layerUsing.Clear();
                        this.RecalculateAllGroupListOneLevel(levelGroup, layerUsing, (levelGroup.Key < 0), layerGroupList, layerUsedLogicalY, ref groups);
                    }
                    usedLogicalY.MergeWith(layerUsedLogicalY);

                    this._AllGroupList.Add(layerGroupList);
                }
                this.Invalidate(InvalidateItems.VisibleGroups);

                this.CalculateYPrepare(usedLogicalY);

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
        /// </summary>
        /// <param name="items">Jednotlivé grafické prvky, které budeme zpracovávat</param>
        /// <param name="layerUsing">Objekt, který řeší využití 2D plochy, kde ve směru X je hodnota typu DateTime, a ve směru Y je pole intervalů typu float</param>
        /// <param name="isDownward">Směr využití na ose Y: true = hledáme volé místo směrem dolů, false = nahoru</param>
        /// <param name="layerGroupList">Výstupní pole, do něhož se ukládají prvky typu <see cref="GTimeGraphGroup"/>, které v sobě zahrnují jeden nebo více prvků <see cref="ITimeGraphItem"/> se shodnou hodnotou <see cref="ITimeGraphItem.GroupId"/></param>
        /// <param name="layerUsedLogicalY">Sumární interval využití osy Y</param>
        /// <param name="groups">Počet skupin, průběžné počitadlo</param>
        protected void RecalculateAllGroupListOneLevel(IEnumerable<ITimeGraphItem> items, PointArray<DateTime, IntervalArray<float>> layerUsing, bool isDownward, List<GTimeGraphGroup> layerGroupList, Interval<float> layerUsedLogicalY, ref int groups)
        {
            float searchFrom = (isDownward ? layerUsedLogicalY.Begin : layerUsedLogicalY.End);
            float nextSearch = searchFrom;

            // Grafické prvky seskupíme podle ITimeGraphItem.GroupId:
            //  více prvků se shodným GroupId tvoří jeden logický celek, tyto prvky jsou vykresleny ve společné linii, nemíchají se s prvky s jiným GroupId.
            // Jedna GroupId reprezentuje například jednu výrobní operaci (nebo přesněji její paralelní průchod), například dva týdny práce;
            //  kdežto jednotlivé položky ITimeGraphItem reprezentují jednotlivé pracovní časy, například jednotlivé směny.
            List<GTimeGraphGroup> groupList = new List<GTimeGraphGroup>();     // Výsledné pole prvků GTimeGraphGroup
            List<ITimeGraphItem> groupsItems = new List<ITimeGraphItem>();     // Sem vložíme prvky ITimeGraphItem, které mají GroupId nenulové, odsud budeme generovat grupy...

            // a) Položky bez GroupId:
            foreach (ITimeGraphItem item in items)
            {
                if (item.GroupId == 0)
                    groupList.Add(new GTimeGraphGroup(this, item));            // Jedna instance GTimeGraphGroup obsahuje jeden pracovní čas
                else
                    groupsItems.Add(item);
            }

            // b) Položky, které mají GroupId nenulové, podle něj seskupíme:
            IEnumerable<IGrouping<int, ITimeGraphItem>> groupArray = groupsItems.GroupBy(i => (i.GroupId != 0 ? i.GroupId : i.ItemId));
            foreach (IGrouping<int, ITimeGraphItem> group in groupArray)
                groupList.Add(new GTimeGraphGroup(this, group));               // Jedna instance GTimeGraphGroup obsahuje jeden nebo více pracovních časů

            // Setřídíme prvky GTimeGraphGroup podle jejich Order a podle času jejich počátku:
            if (groupList.Count > 1)
                groupList.Sort((a, b) => GTimeGraphGroup.CompareOrderTimeAsc(a, b));
            groups += groupList.Count;

            // Hlavním úkolem nyní je určit logické souřadnice Y pro každou skupinu prvků GTimeGraphGroup,
            //  vycházíme přitom z jejího časového intervalu a její výšky,
            // a tuto skupinu zařazujeme do volného prostoru v instanci layerUsing:
            foreach (GTimeGraphGroup group in groupList)
            {
                if (group.IsValidRealTime)
                {   // Grupa je reálná (časy a výška jsou kladné):
                    // Instance layerUsing je PointArray pro Point = DateTime, a Value = pole intervalů typu float.
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

                    // Negativní level bude hledat negativní velikost (dolů):
                    float size = (isDownward ? -group.Height : group.Height);

                    // Nyní v sumáři využitého místa (summary) vyhledáme nejbližší volný prostor s přinejmenším požadovanou velikostí:
                    Interval<float> useSpace = summary.SearchForSpace(searchFrom, size, (a, b) => (a + b));

                    // Nalezený logický prostor Y vložíme do grupy:
                    group.LogicalY = useSpace;

                    // Nalezený logický prostor (useSpace) vepíšeme do všech prvků na časové ose (patří do layerUsing):
                    intervalWorkItems.ForEachItem(pni => pni.Value.Value.Add(useSpace));

                    // Keep the summary values for next Level:
                    if (isDownward && useSpace.Begin < nextSearch)
                        nextSearch = useSpace.Begin;
                    else if (!isDownward && useSpace.End > nextSearch)
                        nextSearch = useSpace.End;

                }
                else
                {   // Nereálné položky (Time or Height je nla nebo záporné):
                    group.LogicalY = new Interval<float>(searchFrom, searchFrom);
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
        /// Seznam všech skupin prvků k zobrazení v grafu.
        /// Seznam má dvojitou úroveň: v první úrovni jsou vizuální vrstvy (od spodní po vrchní), 
        /// v druhé úrovni jsou pak jednotlivé prvky <see cref="GTimeGraphGroup"/> k vykreslení.
        /// </summary>
        protected List<List<GTimeGraphGroup>> AllGroupList { get { this.CheckValidAllGroupList(); return this._AllGroupList; } } private List<List<GTimeGraphGroup>> _AllGroupList;
        #endregion
        #region Souřadnice Y : kontrolní a přepočtové metody
        /// <summary>
        /// Metoda prověří platnost vizuálních souřadnic Y ve všech grupách <see cref="AllGroupList"/>, s ohledem na výšku grafu <see cref="InteractiveObject.Bounds"/>.Height 
        /// v porovnání s 
        /// </summary>
        protected void CheckValidCoordinateY()
        {
            if (this.ValidatedHeight <= 0 || this.Bounds.Height != this.ValidatedHeight)
                this.RecalculateCoordinateY();
        }
        /// <summary>
        /// Provede přepočet souřadnic Y ve všech grupách <see cref="AllGroupList"/>
        /// </summary>
        protected void RecalculateCoordinateY()
        {
            foreach (List<GTimeGraphGroup> layer in this.AllGroupList)
                foreach (GTimeGraphGroup group in layer)
                    this.RecalculateCoordinateY(group);
        }
        /// <summary>
        /// Určí reálné souřadnice Y prvku <see cref="GTimeGraphGroup"/>
        /// </summary>
        /// <param name="group"></param>
        protected void RecalculateCoordinateY(GTimeGraphGroup group)
        {
            group.GControl.VirtualBounds
        }
        /// <summary>
        /// Výška this grafu, pro kterou byly naposledy přepočteny souřadnice Y v poli <see cref="AllGroupList"/>.
        /// </summary>
        protected int ValidatedHeight { get { return this._ValidatedHeight; } } private int _ValidatedHeight;

        /// <summary>
        /// Příznak, že hodnoty Y souřadnice Bounds v prvcích this.GroupList jsou platné.
        /// Jde o WinForm koordináty, tedy jde o reálné souřadnice.
        /// Logické souřadnice jsou platné již od vytvoření instancí <see cref="GTimeGraphGroup"/>.
        /// </summary>
        protected bool GroupItemsYValid { get { return this._GroupItemsYValid; } }
        private bool _GroupItemsYValid;

        #endregion
        #region TimeAxis : osa X grafu - Kontrola platnosti, paměť Identity časové osy
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální hodnoty časové osy <see cref="_TimeConvertor"/>.
        /// Pokud zdejší data jsou vypočítaná pro identický stav časové osy, nechá data beze změn, 
        /// jinak přenačte data osy a invaliduje seznam viditelných dat : <see cref="Invalidate(InvalidateItems)"/> pro hodnotu <see cref="InvalidateItems.VisibleGroups"/>.
        /// </summary>
        protected void CheckValidTimeAxis()
        {
            if (!String.Equals(this.TimeAxisIdentity, this.ValidatedAxisIdentity))
                this.RecalculateTimeAxis();
        }
        /// <summary>
        /// Přenačte do sebe soupis odpovídajících dat z <see cref="_TimeConvertor"/>, 
        /// a invaliduje seznam viditelných dat : <see cref="Invalidate(InvalidateItems)"/> pro hodnotu <see cref="InvalidateItems.VisibleGroups"/>.
        /// </summary>
        protected void RecalculateTimeAxis()
        {
            this._TimeAxisBegin = this._TimeConvertor.GetPixel(this._TimeConvertor.VisibleTime.Begin);
            this._TimeAxisTicks = this._TimeConvertor.Ticks.Where(t => t.TickType == AxisTickType.BigLabel || t.TickType == AxisTickType.StdLabel || t.TickType == AxisTickType.BigTick).ToArray();
            this._ValidatedAxisIdentity = this.TimeAxisIdentity;

            this.Invalidate(InvalidateItems.VisibleGroups);
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
        private ITimeConvertor _TimeConvertor;
        #endregion
        #region VisibleGroupList : výpočty fyzických pixelových souřadnic prvků na ose X a Y pro grupy GTimeGraphGroup i pro jednotlivé prvky ITimeGraphItem
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální fyzické pixelové souřadnice X a Y.
        /// Pokud jsou neplatné, znovu vytvoří pole <see cref="VisibleGroupList"/> a patřičné souřadnice vypočte.
        /// </summary>
        protected void CheckValidVisibleList()
        {
            if (!this.IsValidVisibleList)
                this.RecalculateVisibleList();
        }
        /// <summary>
        /// Vrací true, pokud data v seznamu <see cref="VisibleGroupList"/> jsou platná.
        /// Zohledňuje i stav <see cref="VisibleGroupList"/>, <see cref="ValidatedWidth"/>, <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TimeAxisMode"/>.
        /// Hodnotu lze nastavit, ale i když se vloží true, může se vracet false (pokud výše uvedené není platné).
        /// </summary>
        protected bool IsValidVisibleList
        {
            get
            {
                if (this.VisibleGroupList == null) return false;
                if (!this.ValidatedWidth.HasValue || this.ClientSize.Width != this.ValidatedWidth.Value) return false;
                if (!this.ValidatedAxisMode.HasValue || this.GraphParameters.TimeAxisMode != this.ValidatedAxisMode.Value) return false;
                return this._IsValidVisibleList;
            }
        } private bool _IsValidVisibleList;
        /// <summary>
        /// Naplní korektní data do pole <see cref="VisibleGroupList"/> a vypočte patřičné pixelové souřadnice.
        /// </summary>
        protected void RecalculateVisibleList()
        {
            this._VisibleGroupList = new List<List<GTimeGraphGroup>>();
            ITimeConvertor timeConvertor = this._TimeConvertor;
            if (timeConvertor == null) return;
            TimeGraphTimeAxisMode timeAxisMode = this.GraphParameters.TimeAxisMode;

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateVisibleList", ""))
            {
                int layerCount = 0;
                int groupCount = 0;
                int itemsCount = 0;

                foreach (List<GTimeGraphGroup> layerList in this.AllGroupList)
                {   // Jedna vizuální vrstva za druhou:
                    List<GTimeGraphGroup> visibleItems = new List<GTimeGraphGroup>();
                    switch (timeAxisMode)
                    {
                        case TimeGraphTimeAxisMode.ProportionalScale:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateVisibleListOneGroupProportional(groupItem, visibleItems, ref groupCount, ref itemsCount);
                            break;
                        case TimeGraphTimeAxisMode.LogarithmicScale:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateVisibleListOneGroupLogarithmic(groupItem, visibleItems, ref groupCount, ref itemsCount);
                            break;
                        case TimeGraphTimeAxisMode.Standard:
                        default:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateVisibleListOneGroupStandard(groupItem, visibleItems, ref groupCount, ref itemsCount);
                            break;
                    }
                    
                    if (visibleItems.Count > 0)
                    {
                        this.VisibleGroupList.Add(visibleItems);
                        layerCount++;
                    }
                }

                this._ValidatedWidth = this.ClientSize.Width;
                this._ValidatedAxisMode = timeAxisMode;
                this._IsValidVisibleList = true;

                scope.AddItem("Visual Layers Count: " + layerCount.ToString());
                scope.AddItem("Visual Groups Count: " + groupCount.ToString());
                scope.AddItem("Visual Items Count: " + itemsCount.ToString());
            }
        }
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.Standard"/>
        /// </summary>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="visibleItems">Výstupní seznam, do něhož se vkládají viditelné prvky</param>
        /// <param name="groupCount">Počet viditelných prvků group, pro statistiku</param>
        /// <param name="itemsCount">Počet zpracovaných prvků typu <see cref="ITimeGraphItem"/>, pro statistiku</param>
        protected void RecalculateVisibleListOneGroupStandard(GTimeGraphGroup groupItem, List<GTimeGraphGroup> visibleItems, ref int groupCount, ref int itemsCount)
        {
            ITimeConvertor timeConvertor = this._TimeConvertor;
            if (groupItem.IsValidRealTime && timeConvertor.VisibleTime.HasIntersect(groupItem.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                groupCount++;
                Int32Range y = this.CalculatorYGetRange(groupItem.LogicalY);
                Int32Range x = timeConvertor.GetPixelRange(groupItem.Time);
                groupItem.GControl.VirtualBounds = Int32Range.GetRectangle(x, y);

                foreach (ITimeGraphItem item in groupItem.Items)
                {
                    itemsCount++;

                    x = timeConvertor.GetPixelRange(item.Time);
                    item.GControl.VirtualBounds = Int32Range.GetRectangle(x, y);
                }

                visibleItems.Add(groupItem);
            }
        }
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.ProportionalScale"/>
        /// </summary>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="visibleItems">Výstupní seznam, do něhož se vkládají viditelné prvky</param>
        /// <param name="groupCount">Počet viditelných prvků group, pro statistiku</param>
        /// <param name="itemsCount">Počet zpracovaných prvků typu <see cref="ITimeGraphItem"/>, pro statistiku</param>
        protected void RecalculateVisibleListOneGroupProportional(GTimeGraphGroup groupItem, List<GTimeGraphGroup> visibleItems, ref int groupCount, ref int itemsCount)
        {
            ITimeConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            if (groupItem.IsValidRealTime && timeConvertor.VisibleTime.HasIntersect(groupItem.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                groupCount++;
                Int32Range y = this.CalculatorYGetRange(groupItem.LogicalY);
                Int32Range x = timeConvertor.GetProportionalPixelRange(groupItem.Time, size);
                groupItem.GControl.VirtualBounds = Int32Range.GetRectangle(x, y);

                foreach (ITimeGraphItem item in groupItem.Items)
                {
                    itemsCount++;

                    x = timeConvertor.GetProportionalPixelRange(item.Time, size);
                    item.GControl.VirtualBounds = Int32Range.GetRectangle(x, y);
                }

                visibleItems.Add(groupItem);
            }
        }
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.LogarithmicScale"/>
        /// </summary>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="visibleItems">Výstupní seznam, do něhož se vkládají viditelné prvky</param>
        /// <param name="groupCount">Počet viditelných prvků group, pro statistiku</param>
        /// <param name="itemsCount">Počet zpracovaných prvků typu <see cref="ITimeGraphItem"/>, pro statistiku</param>
        protected void RecalculateVisibleListOneGroupLogarithmic(GTimeGraphGroup groupItem, List<GTimeGraphGroup> visibleItems, ref int groupCount, ref int itemsCount)
        {
            ITimeConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            float proportionalRatio = this.GraphParameters.LogarithmicRatio;
            // Pozor: režim Logarithmic zajistí, že zobrazeny budou VŠECHNY prvky, takže prvky nefiltrujeme s ohledem na jejich čas : VisibleTime.HasIntersect() !
            if (groupItem.IsValidRealTime)
            {
                groupCount++;
                Int32Range y = this.CalculatorYGetRange(groupItem.LogicalY);
                Int32Range x = timeConvertor.GetLogarithmicPixelRange(groupItem.Time, size, proportionalRatio);
                groupItem.GControl.VirtualBounds = Int32Range.GetRectangle(x, y);

                foreach (ITimeGraphItem item in groupItem.Items)
                {
                    itemsCount++;

                    x = timeConvertor.GetLogarithmicPixelRange(item.Time, size, proportionalRatio);
                    item.GControl.VirtualBounds = Int32Range.GetRectangle(x, y);
                }

                visibleItems.Add(groupItem);
            }
        }
        /// <summary>
        /// Seznam všech aktuálně viditelných prvků v grafu.
        /// Seznam má dvojitou úroveň: v první úrovni jsou vizuální vrstvy (od spodní po vrchní), 
        /// v druhé úrovni jsou pak jednotlivé prvky <see cref="GTimeGraphGroup"/> k vykreslení.
        /// </summary>
        protected List<List<GTimeGraphGroup>> VisibleGroupList { get { return this._VisibleGroupList; } } private List<List<GTimeGraphGroup>> _VisibleGroupList;
        /// <summary>
        /// Hodnota Bounds.Width, pro kterou byly naposledy přepočítávány prvky pole <see cref="VisibleGroupList"/>.
        /// Po změně souřadnic se provádí invalidace.
        /// </summary>
        protected int? ValidatedWidth { get { return this._ValidatedWidth; } } private int? _ValidatedWidth;
        /// <summary>
        /// Hodnota <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TimeAxisMode"/>, pro kterou jsou platné hodnoty ve <see cref="VisibleGroupList"/>.
        /// Po změně <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TimeAxisMode"/> dojde k přepočtu dat v tomto seznamu.
        /// </summary>
        protected TimeGraphTimeAxisMode? ValidatedAxisMode { get { return this._ValidatedAxisMode; } } private TimeGraphTimeAxisMode? _ValidatedAxisMode;
        #endregion
        #region Kalkulátor souřadnic Y : výška grafu a přepočty souřadnice Y z logické (float, zdola nahoru) do fyzických pixelů (int, zhora dolů)
        /// <summary>
        /// Instance objektu, jehož výšku může graf změnit i číst pro korektní přepočty svých vnitřních souřadnic.
        /// Typicky se sem vkládá řádek grafu, instance třídy <see cref="Row"/>.
        /// Graf nikdy nepracuje se šířkou parenta <see cref="IVisualParent.ClientWidth"/>.
        /// </summary>
        public IVisualParent VisualParent { get { return this._VisualParent; } set { this._VisualParent = value; this.Invalidate(InvalidateItems.VisibleGroups); } }
        /// <summary>
        /// Aktuální výška dat celého grafu, v pixelech
        /// </summary>
        public int GraphPixelHeight
        {
            get
            {
                this.CheckValidAllGroupList();
                return this._GraphPixelHeight;
            }
        }
        /// <summary>
        /// Metoda vypočte potřebnou výšku grafu v pixelech, určí reálnou výšku s pomocí <see cref="VisualParent"/>,
        /// a pro tuto reálnou výšku připraví potřebné přepočtové koeficienty.
        /// Následně lze volat metodu <see cref="CalculatorYGetPixel(float)"/>, která určuje pozici fyzického pixelu na základě jeho logické souřadnice Y.
        /// Tato varianta uloží dodanou hodnotu (parametr usedLogicalY) do <see cref="UsedLogicalY"/> a následně ji použije.
        /// </summary>
        /// <param name="usedLogicalY"></param>
        protected void CalculateYPrepare(Interval<float> usedLogicalY)
        {
            if (usedLogicalY == null) return;
            this.UsedLogicalY = usedLogicalY;
            this._CalculateYPrepare(usedLogicalY);
        }
        /// <summary>
        /// Metoda vypočte potřebnou výšku grafu v pixelech, určí reálnou výšku s pomocí <see cref="VisualParent"/>,
        /// a pro tuto reálnou výšku připraví potřebné přepočtové koeficienty.
        /// Tato varianta použije hodnotu <see cref="UsedLogicalY"/>.
        /// Následně lze volat metodu <see cref="CalculatorYGetPixel(float)"/>, která určuje pozici fyzického pixelu na základě jeho logické souřadnice Y.
        /// </summary>
        protected void CalculateYPrepare()
        {
            this._CalculateYPrepare(this.UsedLogicalY);
        }
        /// <summary>
        /// Metoda vypočte potřebnou výšku grafu v pixelech, určí reálnou výšku s pomocí <see cref="VisualParent"/>,
        /// a pro tuto reálnou výšku připraví potřebné přepočtové koeficienty.
        /// Tato varianta pracuje s dodanou hodnotou usedLogicalY, kterou nikam neukládá.
        /// Následně lze volat metodu <see cref="CalculatorYGetPixel(float)"/>, která určuje pozici fyzického pixelu na základě jeho logické souřadnice Y.
        /// </summary>
        /// <param name="usedLogicalY"></param>
        protected void _CalculateYPrepare(Interval<float> usedLogicalY)
        {
            if (usedLogicalY == null) return;

            float logBegin = (usedLogicalY.Begin < 0f ? usedLogicalY.Begin : 0f);
            float logEnd = (usedLogicalY.End > 1f ? usedLogicalY.End : 1f);
            float logSize = logEnd - logBegin;

            // Výška dat grafu v pixelech, zarovnaná do patřičných mezí:
            int pixelSize = this._CalculateYAlignHeight((int)(Math.Ceiling(logSize * (float)this.GraphParameters.OneLineHeight.Value)));
            this._GraphPixelHeight = pixelSize;

            // Výpočty kalkulátoru, invalidace VisibleList:
            this._CalculatorY_Offset = logBegin;
            this._CalculatorY_Scale = (float)pixelSize / logSize;

            this.Invalidate(InvalidateItems.VisibleGroups);
        }
        /// <summary>
        /// Metoda zajistí zarovnání výšky grafu (v pixelech) do patřičného rozmezí.
        /// Využívá: rozmezí <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TotalHeightRange"/>, hodnoty Skin.Graph.TotalHeightMin a TotalHeightMax;
        /// a dále využívá objekt <see cref="VisualParent"/> a jeho <see cref="IVisualParent.ClientHeight"/>
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        private int _CalculateYAlignHeight(int height)
        {
            int result = height;

            Int32NRange range = this.GraphParameters.TotalHeightRange;
            if (range != null && range.IsReal)
                result = range.Align(result).Value;
            else
            {
                int min = Skin.Graph.TotalHeightMin;
                int max = Skin.Graph.TotalHeightMax;
                result = (result < min ? min : (result > max ? max : result));
            }

            IVisualParent visualParent = this.VisualParent;
            if (visualParent != null)
            {
                visualParent.ClientHeight = result;
                result = visualParent.ClientHeight;
            }

            return result;
        }
        /// <summary>
        /// Metoda vrátí rozsah na ose Y (ve virtuálním formátu), jehož logické souřadnice jsou zadány.
        /// Virtuální formát je v pixelech, ale hodnota 0 odpovídá dolnímu okraji grafu.
        /// Je to proto, aby se grafy nemusely přepočítávat při změně výšky grafu: 0 je stále dole.
        /// Kdežto ve WinForm reprezentaci je nula nahoře...
        /// </summary>
        /// <param name="logicalY"></param>
        /// <returns></returns>
        protected Int32Range CalculatorYGetRange(Interval<float> logicalY)
        {
            float end = this.CalculatorYGetPosition(logicalY.End);
            float size = this._CalculatorY_Scale * (logicalY.End - logicalY.Begin);
            return Int32Range.CreateFromBeginSize((int)Math.Round(end, 0), (int)Math.Round(size, 0));
        }
        /// <summary>
        /// Metoda vrátí souřadnici Y v pixelech pro zadanou logickou souřadnici Y.
        /// Vrácená hodnota je rovna (GraphPixelHeight - 1) pro logicalY = this.UsedLogicalY.Begin (logický začátek osy Y je dole = ve Windows grafice větší souřadnice Y).
        /// Pro logickou hodnotu this.UsedLogicalY.End je vrácen pixel = 0 (logický kladný konec osy je nahoře = ve Windows grafice menší souřadnice Y).
        /// </summary>
        /// <param name="logicalY"></param>
        /// <returns></returns>
        protected int CalculatorYGetPixel(float logicalY)
        {
            float pixelY = this.CalculatorYGetPosition(logicalY);
            return (int)Math.Round(pixelY, 0);
        }
        /// <summary>
        /// Metoda vrátí souřadnici Y jako float, pro zadanou logickou souřadnici Y.
        /// Vrácená hodnota je rovna (GraphPixelHeight - 1) pro logicalY = this.UsedLogicalY.Begin (logický začátek osy Y je dole = ve Windows grafice větší souřadnice Y).
        /// Pro logickou hodnotu this.UsedLogicalY.End je vrácen pixel = 0 (logický kladný konec osy je nahoře = ve Windows grafice menší souřadnice Y).
        /// </summary>
        /// <param name="logicalY"></param>
        /// <returns></returns>
        protected float CalculatorYGetPosition(float logicalY)
        {
            float result = (this._CalculatorY_Scale * (logicalY - this._CalculatorY_Offset));
            return (result < 0f ? 0f : result);
        }
        /// <summary>
        /// Aktuálně použité rozmezí logických souřadnic na ose Y
        /// </summary>
        protected Interval<float> UsedLogicalY;
        /// <summary>
        /// Úložiště hodnoty Aktuální výška dat celého grafu, v pixelech
        /// </summary>
        private int _GraphPixelHeight;
        /// <summary>
        /// Offset pro kalkulátor Logical to Pixel Y
        /// </summary>
        private float _CalculatorY_Offset;
        /// <summary>
        /// Koeficient pro kalkulátor Logical to Pixel Y
        /// </summary>
        private float _CalculatorY_Scale;
        /// <summary>
        /// Vizuální parent, jehož výšku (<see cref="IVisualParent.ClientHeight"/>) můžeme nastavovat
        /// </summary>
        private IVisualParent _VisualParent;
        #endregion
        #region Invalidace je řešená jedním vstupním bodem
        /// <summary>
        /// Invaliduje dané prvky grafu, a automaticky přidá i prvky na nich závislé.
        /// </summary>
        /// <param name="items"></param>
        protected void Invalidate(InvalidateItems items)
        {
            if ((items & InvalidateItems.AllGroups) != 0)
            {
                this._AllGroupList = null;
                items |= InvalidateItems.VisibleGroups;
            }

            if ((items & InvalidateItems.XCoord) != 0)
            {
                
            }
            if ((items & InvalidateItems.YCoord) != 0)
            {
                this._ValidatedHeight = 0;
            }

            if ((items & InvalidateItems.VisibleGroups) != 0)
            {
                this._IsValidVisibleList = false;
                items |= InvalidateItems.Repaint;
            }

            if ((items & InvalidateItems.Repaint) != 0)
            {
                this.Repaint();
            }
        }
        /// <summary>
        /// Prvky grafu, které budou invalidovány
        /// </summary>
        [Flags]
        protected enum InvalidateItems : int
        {
            None = 0,
            XCoord = 1,
            YCoord = XCoord << 1,
            AllGroups = YCoord << 1,
            VisibleGroups = AllGroups << 1,
            Childs = VisibleGroups << 1,
            Repaint = Childs << 1
        }
        #endregion
        #region Child items a kompletní validace
        /// <summary>
        /// Child prvky grafu = položky grafu, výhradně typu <see cref="GTimeGraphGroup"/>.
        /// Před vrácením soupisu proběhne jeho validace.
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckValid(); return this._Childs; } } private List<IInteractiveItem> _Childs;
        /// <summary>
        /// Metoda zajistí provedení kontroly platnosti všech vnitřních dat, podle toho která kontrola a přepočet je zapotřebí.
        /// </summary>
        protected void CheckValid()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "CheckValid", ""))
            {
                this.CheckValidAllGroupList();
                this.CheckValidCoordinateY();
                this.CheckValidTimeAxis();
                this.CheckValidVisibleList();
                this.CheckValidChildList();
            }
        }
        protected void CheckValidChildList()
        {
            qqq;
        }
        #endregion
        #region Draw : vykreslení grafu
        /// <summary>
        /// Systémové kreslení grafu
        /// </summary>
        /// <param name="e"></param>
        protected override void Draw(GInteractiveDrawArgs e)
        {
            Rectangle boundsAbsolute = this.BoundsAbsolute;
            this.DrawBackground(e, boundsAbsolute);
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "Draw", ""))
            {
                this.CheckValid();
                e.GraphicsClipWith(boundsAbsolute);
                this.DrawTicks();
            }
        }
        /// <summary>
        /// Metoda umožní udělat něco s pozadím grafu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            switch (this.GraphParameters.TimeAxisMode)
            {
                case TimeGraphTimeAxisMode.LogarithmicScale:
                    this.DrawBackgroundLogarithmic(e, boundsAbsolute);
                    break;
            }
        }
        /// <summary>
        /// Metoda umožní udělat něco s pozadím grafu, který má logaritmickou osu.
        /// Vykreslí se šedý přechod na logaritmických okrajích.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected virtual void DrawBackgroundLogarithmic(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            float shadow = this.GraphParameters.LogarithmicGraphDrawOuterShadow;
            if (shadow <= 0f) return;
            int alpha = (int)(255f * shadow);
            Color color1 = Color.FromArgb(0, 0, 0, 0);
            Color color2 = Color.FromArgb(alpha, 0, 0, 0);
            int width = (int)(((1f - this.GraphParameters.LogarithmicRatio) / 2f) * (float)boundsAbsolute.Width);

            Rectangle leftBounds = new Rectangle(boundsAbsolute.X, boundsAbsolute.Y, width, boundsAbsolute.Height);
            Rectangle leftBoundsG = leftBounds.Enlarge(1, 0, 0, 1);                      // To je úchylka WinFormů
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(leftBoundsG, color2, color1, 00f))
            {
                e.Graphics.FillRectangle(lgb, leftBounds);
            }

            Rectangle rightBounds = new Rectangle(boundsAbsolute.Right - width, boundsAbsolute.Y, width, boundsAbsolute.Height);
            Rectangle rightBoundsG = rightBounds.Enlarge(1, 0, 0, 1);                    // To je úchylka WinFormů
            using (System.Drawing.Drawing2D.LinearGradientBrush rgb = new System.Drawing.Drawing2D.LinearGradientBrush(rightBoundsG, color2, color1, 180f))
            {
                e.Graphics.FillRectangle(rgb, rightBounds);
            }
        }


        // Následující by se mělo zrušit, včetně třídy TimeGraphItemDrawArgs


        /// <summary>
        /// Vykreslení obsahu grafu: vykreslí pozadí (pokud je zapotřebí), následně Ticky (pokud se mají kreslit) a poté vykreslí jednotlivé prvky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected virtual void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawContentPrepareArgs(e, boundsAbsolute);
            this.DrawBackground(e, boundsAbsolute);
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "DrawContent", ""))
            {
                this.Bounds = new Rectangle(0, 0, boundsAbsolute.Width, boundsAbsolute.Height);
                this.CheckValid();
                e.GraphicsClipWith(boundsAbsolute);
                this.DrawTicks();
                this.DrawItems();
            }
        }
        /// <summary>
        /// Metoda připraví objekt <see cref="ItemDrawArgs"/> pro následující operace kreslení grafu.
        /// Metoda do objektu vloží aktuální souřadnice <see cref="InteractiveObject.Bounds"/> (pro přepočet souřadnic osy Y z Virtual na WinForm).
        /// Uloží i TimeConvertor.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void DrawContentPrepareArgs(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this.ItemDrawArgs == null)
                this.ItemDrawArgs = new TimeGraphItemDrawArgs(this.Host);
            this.ItemDrawArgs.Prepare(e, boundsAbsolute, this._TimeConvertor);
        }
        /// <summary>
        /// Vykreslí všechny Ticky = časové značky, pokud se mají kreslit.
        /// </summary>
        protected void DrawTicks()
        {
            AxisTickType tickLevel = this.GraphParameters.TimeAxisVisibleTickLevel;
            if (tickLevel == AxisTickType.None) return;
            int tickLevelN = (int)tickLevel;

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "PaintGrid", ""))
            {
                int x;
                int x0 = this.ItemDrawArgs.GraphBoundsAbsolute.X + this.TimeAxisBegin;
                int y1 = this.ItemDrawArgs.GraphBoundsAbsolute.Top;
                int y2 = this.ItemDrawArgs.GraphBoundsAbsolute.Bottom - 1;

                foreach (VisualTick tick in this.TimeAxisTicks)
                {
                    if (((int)tick.TickType) < tickLevelN) continue;

                    x = x0 + tick.RelativePixel;
                    GPainter.DrawAxisTick(this.ItemDrawArgs.Graphics, tick.TickType, x, y1, x, y2, Skin.Graph.TimeAxisTickMain, Skin.Graph.TimeAxisTickSmall, true);

                    //switch (tick.TickType)
                    //{
                    //    case AxisTickType.BigLabel:
                    //        this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 2f, System.Drawing.Drawing2D.DashStyle.Solid);
                    //        break;

                    //    case AxisTickType.StdLabel:
                    //        this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 1f, System.Drawing.Drawing2D.DashStyle.Solid);
                    //        break;

                    //    case AxisTickType.BigTick:
                    //        this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 1f, System.Drawing.Drawing2D.DashStyle.Dot);
                    //        break;
                    //}
                }
            }
        }
        /// <summary>
        /// Metoda vykreslí všechny prvky grafu.
        /// Prvky se kreslí po vrstvách, a pouze ty prvky které jsou viditelné.
        /// Používá se datový seznam <see cref="VisibleGroupList"/>.
        /// </summary>
        protected void DrawItems()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "PaintItems", ""))
            {
                int layers = 0;
                int groups = 0;
                int items = 0;

                foreach (List<GTimeGraphGroup> layerList in this.VisibleGroupList)
                {
                    layers++;
                    foreach (GTimeGraphGroup group in layerList)
                    {
                        groups++;
                        group.Draw(this.ItemDrawArgs);
                        foreach (ITimeGraphItem item in group.Items)
                        {
                            items++;
                            item.Draw(this.ItemDrawArgs);
                        }
                    }
                }

                scope.AddItem("Layers drawed: " + layers.ToString());
                scope.AddItem("Groups drawed: " + groups.ToString());
                scope.AddItem("Item drawed: " + items.ToString());
            }
        }
        /// <summary>
        /// Instance objektu <see cref="TimeGraphItemDrawArgs"/> pro vykreslování prvků grafu, pro přepočty koordinátů na ose Y.
        /// </summary>
        protected TimeGraphItemDrawArgs ItemDrawArgs { get; set; }
        #endregion
        #region ITimeGraph + ITimeInteractiveGraph members
        ITimeConvertor ITimeGraph.TimeConvertor { get { return this._TimeConvertor; } set { this._TimeConvertor = value; this.Invalidate(InvalidateItems.VisibleGroups); } }
        int ITimeGraph.UnitHeight { get { return this.GraphParameters.OneLineHeight.Value; } } 
        void ITimeGraph.DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute) { this.DrawContentTimeGraph(e, boundsAbsolute); }
        #endregion
    }
    #region class GTimeGraphGroup : skupina jednoho nebo více prvků ITimeGraphItem, obsahující sumární čas Time a Max(Height) z položek
    /// <summary>
    /// GTimeGraphGroup : GTimeGraphGroup : skupina jednoho nebo více prvků ITimeGraphItem, obsahující sumární čas Time a Max(Height) z položek
    /// </summary>
    public class GTimeGraphGroup : ITimeGraphItem
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        private GTimeGraphGroup(GTimeGraph parent)
        {
            this._ParentGraph = parent;
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
            this._FirstItem = null;
            _PrepareGControl(this, parent);                // Připravím GUI prvek pro sebe = pro grupu, jeho parentem je vlastní graf
        }
        /// <summary>
        /// Konstruktor s předáním jediné položky
        /// </summary>
        /// <param name="items"></param>
        public GTimeGraphGroup(GTimeGraph parent, ITimeGraphItem item)
            : this(parent)
        {
            _PrepareGControl(item, this.GControl);         // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
            this._FirstItem = item;
            this._Items = new ITimeGraphItem[] { item };
            this._Store(item.Time.Begin, item.Time.End, item.Height);
        }
        /// <summary>
        /// Konstruktor s předáním skupiny položek, s výpočtem jejich sumárního časového intervalu a výšky
        /// </summary>
        /// <param name="items"></param>
        public GTimeGraphGroup(GTimeGraph parent, IEnumerable<ITimeGraphItem> items)
            : this(parent)
        {
            this._Items = items.ToArray();
            DateTime? begin = null;
            DateTime? end = null;
            float height = 0f;
            foreach (ITimeGraphItem item in this.Items)
            {
                _PrepareGControl(item, this.GControl);     // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height > height) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
            }
            this._Store(begin, end, height);
        }
        /// <summary>
        /// Metoda zajistí, že prvek (item) bude mít svůj grafický control (<see cref="ITimeGraphItem.GControl"/>).
        /// Tato metoda tedy připravuje prvek třídy <see cref="GTimeGraphControl"/> jak pro sebe (pro grupu), tak i pro jednotlivé položky grafu (<see cref="ITimeGraphItem"/>).
        /// </summary>
        /// <param name="item">Prvek grafu, ten v sobě obsahuje data</param>
        /// <param name="parent">Parent prvku, GUI container</param>
        private static void _PrepareGControl(ITimeGraphItem item, IInteractiveParent parent)
        {
            if (item != null && item.GControl == null)
                item.GControl = new GTimeGraphControl(item, parent);
        }
        /// <summary>
        /// Zadané údaje vloží do <see cref="Time"/> a <see cref="Height"/>, vypočte hodnotu <see cref="IsValidRealTime"/>.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="height"></param>
        private void _Store(DateTime? begin, DateTime? end, float height)
        {
            this._Time = new TimeRange(begin, end);
            this._Height = height;
            this._IsValidRealTime = ((height > 0f) && (begin.HasValue && end.HasValue && end.Value > begin.Value));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Time: " + this.Time.ToString() +
                "; Height: " + this.Height.ToString() +
                "; UseSpace: " + (this.LogicalY == null ? "none" : this.LogicalY.ToString());
        }
        /// <summary>
        /// Parent této grupy položek = graf
        /// </summary>
        private GTimeGraph _ParentGraph;
        #endregion
        #region Privátní proměnné
        private int _ItemId;
        private ITimeGraphItem _FirstItem;
        private ITimeGraphItem[] _Items;
        private float _Height;
        private TimeRange _Time;
        private bool _IsValidRealTime;
        #endregion
        #region Souřadnice prvku
        /// <summary>
        /// Logické rozmezí tohoto prvku na ose Y. Souřadnice 0 odpovídá hodnotě 0 na ose Y, kladná čísla jsou fyzicky nahoru, záporná jsou povolená a jdou dolů.
        /// Jednotka je logická, nikoli pixely. Přepočet na pixely probíhá jinde.
        /// </summary>
        public Interval<float> LogicalY { get; set; }
        /// <summary>
        /// Virtuální souřadnice na ose Y. Jednotkou jsou pixely.
        /// Hodnota 0 odpovídá pixelu úplně dole na grafu, tj. jako na matematické ose: Y jde odspodu nahoru.
        /// </summary>
        public Int32Range CoordinateYVirtual { get; set; }
        /// <summary>
        /// Reálné souřadnice na ose Y. Jednotkou jsou pixely.
        /// Hodnota 0 odpovídá pixelu na souřadnici Bounds.Top, tj. jako ve Windows.Forms: Y jde odshora dolů.
        /// </summary>
        public Int32Range CoordinateYReal { get; set; }

        #endregion
        #region Public prvky, Draw()
        /// <summary>
        /// Pole všech základních prvků <see cref="ITimeGraphItem"/> zahrnutých v tomto objektu.
        /// Pole má vždy nejméně jeden prvek.
        /// První prvek tohoto pole <see cref="_FirstItem"/> je nositelem některých klíčových informací.
        /// </summary>
        public ITimeGraphItem[] Items { get { return this._Items; } }
        /// <summary>
        /// Počet prvků pole <see cref="Items"/>
        /// </summary>
        public int ItemCount { get { return this._Items.Length; } }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.GraphParameters"/>: <see cref="TimeGraphProperties.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        public GraphItemEditMode EditMode { get { return this._FirstItem.EditMode; } }
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        public Color? BackColor { get { return this._FirstItem.BackColor; } }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// </summary>
        public System.Drawing.Drawing2D.HatchStyle? BackStyle { get { return this._FirstItem.BackStyle; } }
        /// <summary>
        /// Barva spojovací linky mezi prvky jedné skupiny.
        /// Default = null = kreslí se barvou <see cref="BackColor"/>, která je morfována na 50% do barvy DimGray a zprůhledněna na 50%.
        /// </summary>
        public Color? LinkBackColor { get { return this._FirstItem.LinkBackColor; } }
        /// <summary>
        /// Barva okraje (ohraničení) prvku.
        /// </summary>
        public Color? BorderColor { get { return this._FirstItem.BorderColor; } }
        /// <summary>
        /// Souhrnný čas všech prvků v této skupině. Je vypočten při vytvoření prvku.
        /// Pouze prvek, jehož čas je kladný (End je vyšší než Begin) je zobrazován.
        /// </summary>
        public TimeRange Time { get { return this._Time; } }
        /// <summary>
        /// Obsahuje true, když tento prvek je vhodné zobrazovat (má kladný čas i výšku).
        /// </summary>
        internal bool IsValidRealTime { get { return this._IsValidRealTime; } }
        /// <summary>
        /// Vizuální prvek, který v sobě zahrnuje jak podporu pro vykreslování, tak podporu interaktivity.
        /// A přitom to nevyžaduje od třídy, která fyzicky implementuje <see cref="ITimeGraphItem"/>.
        /// Aplikační kód (implementační objekt <see cref="ITimeGraphItem"/> se o tuto property nemusí starat, řídící mechanismus sem vloží v případě potřeby new instanci.
        /// Implementátor pouze poskytuje úložiště pro tuto instanci.
        /// </summary>
        public GTimeGraphControl GControl { get; set; }
        /// <summary>
        /// Logický prostor alokovaný na ose Y.
        /// Standardní prvek má výšku == 1.0f.
        /// </summary>
        public Interval<float> LogicalY
        {
            get { return this.GControl.LogicalY; }
            set
            {
                Interval<float> logicalY = value.ValueClone;
                this.GControl.LogicalY = logicalY;
                this.Items.ForEachItem(i => i.GControl.LogicalY = logicalY);
            }
        }
        /// <summary>
        /// Vykreslí tuto grupu. Kreslí pouze pokud obsahuje více než 1 prvek, a pokud vrstva <see cref="ITimeGraphItem.Layer"/> je nula nebo kladná (pro záporné vrstvy se nekreslí).
        /// Vykreslí spojovací linii.
        /// </summary>
        /// <param name="drawArgs">All data and support for drawing</param>
        public void Draw(TimeGraphItemDrawArgs drawArgs)
        {
            if (!this.IsValidRealTime || this._FirstItem.Layer < 0 || this.ItemCount <= 1) return;
            Color? backColor = this.LinkBackColor;
            if (!backColor.HasValue)
                // Nemáme explicitně danou barvu linky => odvodíme ji z barvy pozadí prvku + morphing:
                backColor = (this.BackColor.HasValue ? this.BackColor.Value : Skin.Graph.ElementBackColor).Morph(Skin.Graph.ElementLinkBackColor);
            backColor = Color.FromArgb(128, backColor.Value);
            Color? borderColor = backColor;
            this.GControl.Draw(drawArgs, backColor, borderColor, - 1);
        }
        /// <summary>
        /// Porovná dvě instance <see cref="GTimeGraphGroup"/> podle <see cref="ITimeGraphItem.Order"/> ASC, <see cref="ITimeGraphItem.Time"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrderTimeAsc(GTimeGraphGroup a, GTimeGraphGroup b)
        {
            int cmp = a._FirstItem.Order.CompareTo(b._FirstItem.Order);
            if (cmp == 0)
                cmp = TimeRange.CompareByBeginAsc(a.Time, b.Time);
            return cmp;
        }
        #endregion
        #region explicit ITimeGraphItem members
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.Layer { get { return this._FirstItem.Layer; } }
        int ITimeGraphItem.Level { get { return this._FirstItem.Level; } }
        int ITimeGraphItem.Order { get { return this._FirstItem.Order; } }
        int ITimeGraphItem.GroupId { get { return this._FirstItem.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this.Time; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        GraphItemEditMode ITimeGraphItem.EditMode { get { return this.EditMode; } }
        Color? ITimeGraphItem.BackColor { get { return this.BackColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this.BackStyle; } }
        Color? ITimeGraphItem.LinkBackColor { get { return this.LinkBackColor; } }
        Color? ITimeGraphItem.BorderColor { get { return this.BorderColor; } }
        GTimeGraphControl ITimeGraphItem.GControl { get { return this.GControl; } set { this.GControl = value; } }
        void ITimeGraphItem.Draw(TimeGraphItemDrawArgs drawArgs) { this.Draw(drawArgs); }
        #endregion
    }
    #endregion
    #region class GTimeGraphControl : vizuální a interaktivní control, který se vkládá do implementace ITimeGraphItem
    /// <summary>
    /// GTimeGraphControl : vizuální a interaktivní control, který se vkládá do implementace ITimeGraphItem.
    /// Tento prvek je zobrazován ve dvou režimech: buď jako přímý child prvek vizuálního grafu, pak reprezentuje grupu prvků (i kdyby grupa měla jen jeden prvek),
    /// anebo jako child prvek této grupy, pak reprezentuje jeden konkrétní prvek grafu (GraphItem).
    /// </summary>
    public class GTimeGraphControl : InteractiveObject, IOwnerProperty<ITimeGraphItem>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner">Prvek grafu, ten v sobě obsahuje data</param>
        /// <param name="parent">Parent prvku, GUI container</param>
        public GTimeGraphControl(ITimeGraphItem owner, IInteractiveParent parent)
        {
            this._Owner = owner;
            this._Parent = parent;
        }
        /// <summary>
        /// Vlastník tohoto grafického prvku = datový prvek grafu
        /// </summary>
        ITimeGraphItem IOwnerProperty<ITimeGraphItem>.Owner { get { return this._Owner; } set { this._Owner = value; } } private ITimeGraphItem _Owner;
        /// <summary>
        /// Parent tohoto grafického prvku = GUI prvek, v němž je tento grafický prvek hostován
        /// </summary>
        private IInteractiveParent _Parent;
        /// <summary>
        /// Souřadnice na ose X. Jednotkou jsou pixely.
        /// Tato osa je společná jak pro virtuální, tak pro reálné souřadnice.
        /// Hodnota 0 odpovídá prvnímu viditelnému pixelu vlevo.
        /// </summary>
        public Int32Range CoordinateX { get; set; }
        /// <summary>
        /// Virtuální souřadnice prvku na ose Y, v pixelech, ale s obráceným významem hodnoty Y:
        /// 0 je dolní pixel buňky, kladné číslo Y jde nahoru, záporné číslo se nevyskytuje. Důvodem je korektní chování při zvětšování výšky (na ose Y), 
        /// kdy chceme mít prvek grafu vždy stejně vzdálený od souřadnice Bottom (jak se na slušný graf sluší), a ne že bude fixně viset od souřadnice Top (jak to dělají Windows).
        /// Souřadnice X je korektní, odpovídá Bounds.X.
        /// Konverzi virtuálních souřadnic na fyzické provádí vykreslovací objekt třídy <see cref="TimeGraphItemDrawArgs"/>.
        /// </summary>
        public Rectangle xxx_VirtualBounds { get; set; }
        /// <summary>
        /// true když čas je kladný a výška rovněž
        /// </summary>
        protected bool xxx_IsValidRealTime { get { return (this._Owner != null && this._Owner.Time != null && this._Owner.Time.IsFilled && this._Owner.Time.IsReal && (this.LogicalY.End > this.LogicalY.Begin)); } }
        /// <summary>
        /// Metoda je volaná pro vykreslení prvku.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/>.<see cref="GTimeGraphControl.Draw(TimeGraphItemDrawArgs)"/> Draw
        /// </summary>
        /// <param name="drawArgs">Veškerá podpora pro přepočty souřadnic a pro kreslení prvku grafu</param>
        public void Draw(TimeGraphItemDrawArgs drawArgs)
        {
            this.Draw(drawArgs, null, null, null);
        }
        /// <summary>
        /// Metoda je volaná pro vykreslení prvku.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/>.<see cref="GTimeGraphControl.Draw(TimeGraphItemDrawArgs)"/> Draw
        /// </summary>
        /// <param name="drawArgs">Veškerá podpora pro přepočty souřadnic a pro kreslení prvku grafu</param>
        /// <param name="backColor">Explicitně definovaná barva pozadí</param>
        /// <param name="borderColor">Explicitně definovaná barva okraje</param>
        /// <param name="enlargeBounds">Změna rozměru Bounds ve všech směrech</param>
        public void Draw(TimeGraphItemDrawArgs drawArgs, Color? backColor, Color? borderColor, int? enlargeBounds)
        {
            if (!this.IsValidRealTime) return;

            Rectangle boundsAbsolute = drawArgs.GetBoundsAbsolute(this.VirtualBounds, 1).Enlarge(0, -1, 0, 0);
            if (enlargeBounds.HasValue)
            {
                boundsAbsolute = boundsAbsolute.Enlarge(enlargeBounds.Value);
                if (boundsAbsolute.Width < 1)
                    boundsAbsolute.Width = 1;
            }
            int w = boundsAbsolute.Width;

            if (!backColor.HasValue)
                backColor = (this._Owner.BackColor.HasValue ? this._Owner.BackColor.Value : Skin.Graph.ElementBackColor);

            System.Drawing.Drawing2D.HatchStyle? backStyle = this._Owner.BackStyle;

            if (!borderColor.HasValue)
                borderColor = (this._Owner.BorderColor.HasValue ? this._Owner.BorderColor.Value : backColor.Value.Morph(Color.Black, 0.60f));

            if (boundsAbsolute.Width <= 2)
            {
                drawArgs.Graphics.FillRectangle(Skin.Brush(borderColor.Value), boundsAbsolute);
            }
            else
            {
                if (backStyle.HasValue)
                {
                    using (System.Drawing.Drawing2D.HatchBrush hb = new System.Drawing.Drawing2D.HatchBrush(backStyle.Value, backColor.Value, Color.Transparent))
                    {
                        drawArgs.Graphics.FillRectangle(hb, boundsAbsolute);
                    }
                }
                else
                {
                    drawArgs.Graphics.FillRectangle(Skin.Brush(backColor.Value), boundsAbsolute);
                }

                drawArgs.Graphics.DrawRectangle(Skin.Pen(borderColor.Value), boundsAbsolute);
            }
        }
    }
    #endregion
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
            this._TimeAxisVisibleTickLevel = AxisTickType.BigTick;
            this._OneLineHeight = Skin.Graph.LineHeight;
            this._LogarithmicRatio = 0.60f;
            this._LogarithmicGraphDrawOuterShadow = 0.20f;
        }
        /// <summary>
        /// Režim zobrazování času na ose X
        /// </summary>
        public TimeGraphTimeAxisMode TimeAxisMode { get { return this._TimeAxisMode; } set { this._TimeAxisMode = value; } } private TimeGraphTimeAxisMode _TimeAxisMode;
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
        /// Jedna logická linka odpovídá výšce <see cref="ITimeGraphItem.Height"/> = 1.0f.
        /// Pokud graf obsahuje více položek pro jeden časový úsek (a položky jsou ze stejné vrstvy <see cref="ITimeGraphItem.Layer"/>, pak tyto prvky jsou kresleny nad sebe.
        /// Výška grafu pak bude součtem výšky těchto prvků (=logická výška), násobená výškou <see cref="OneLineHeight"/> (pixely na jednotku výšky prvku).
        /// <para/>
        /// Lze vložit hodnotu null, pak se bude vracet defaultní výška (<see cref="Skin.Graph.LineHeight"/>).
        /// Čtení hodnoty nikdy nevrací null, vždy lze pracovat s GraphLineHeight.Value.
        /// </summary>
        public int? OneLineHeight
        {
            get { return (this._OneLineHeight.HasValue ? this._OneLineHeight.Value : Skin.Graph.LineHeight); }
            set
            {
                int oldValue = this.OneLineHeight.Value;
                if (value != null)
                    this._OneLineHeight = (value < 5 ? 5 : (value > 500 ? 500 : value));
                else
                    this._OneLineHeight = null;
                int newValue = this.OneLineHeight.Value;
            }
        }
        private int? _OneLineHeight;
        /// <summary>
        /// Rozmezí výšky celého grafu, v pixelech.
        /// Výchozí hodnota je null, pak se použije rozmezí <see cref="Skin.Graph.DefaultTotalHeightMin"/> až <see cref="Skin.Graph.DefaultTotalHeightMax"/>
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
        /// Logaritmická časová osa: vykreslovat vystínování oblastí s logaritmickým měřítkem osy (tedy ty levé a pravé okraje, kde již neplatí lineární měřítko).
        /// Zde se zadává hodnota 0 až 1, která reprezentuje úroven vystínování těchto okrajů.
        /// Hodnota 0 = žádné stínování, hodnota 1 = krajní pixel je zcela černý. Default hodnota = 0.20f.
        /// </summary>
        public float LogarithmicGraphDrawOuterShadow
        {
            get { return this._LogarithmicGraphDrawOuterShadow; }
            set { float v = value; this._LogarithmicGraphDrawOuterShadow = (v < 0.0f ? 0.0f : (v > 1.0f ? 1.0f : v)); }
        }
        private float _LogarithmicGraphDrawOuterShadow;

    }
    #endregion
    #region Interface ITimeGraph, ITimeGraphItem, ITimeConvertor; enum TimeGraphAxisXMode
    public interface ITimeInteractiveGraph : ITimeGraph, IInteractiveItem
    { }
    public interface ITimeGraph
    {
        /// <summary>
        /// Reference na objekt, který provádí časové konverze pro tento graf.
        /// Instanci do této property plní ten, kdo ji zná.
        /// </summary>
        ITimeConvertor TimeConvertor { get; set; }
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
    public interface ITimeGraphItem
    {
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
        /// Časový interval tohoto prvku
        /// </summary>
        TimeRange Time { get; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.GraphParameters"/>: <see cref="TimeGraphProperties.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        float Height { get; }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        GraphItemEditMode EditMode { get; }
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// </summary>
        System.Drawing.Drawing2D.HatchStyle? BackStyle { get; }
        /// <summary>
        /// Barva spojovací linky mezi prvky jedné skupiny.
        /// Default = null = kreslí se barvou <see cref="BackColor"/>, která je morfována na 50% do barvy DimGray a zprůhledněna na 50%.
        /// </summary>
        Color? LinkBackColor { get; }
        /// <summary>
        /// Barva okraje (ohraničení) prvku.
        /// </summary>
        Color? BorderColor { get; }
        /// <summary>
        /// Vizuální prvek, který v sobě zahrnuje jak podporu pro vykreslování, tak podporu interaktivity.
        /// A přitom to nevyžaduje od třídy, která fyzicky implementuje <see cref="ITimeGraphItem"/>.
        /// Aplikační kód (implementační objekt <see cref="ITimeGraphItem"/> se o tuto property nemusí starat, řídící mechanismus sem vloží v případě potřeby new instanci.
        /// Implementátor pouze poskytuje úložiště pro tuto instanci.
        /// </summary>
        GTimeGraphControl GControl { get; set; }
        /// <summary>
        /// Metoda je volaná pro vykreslení prvku.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/> : <see cref="GTimeGraphControl.Draw(TimeGraphItemDrawArgs)"/>
        /// </summary>
        /// <param name="drawArgs">Veškerá podpora pro přepočty souřadnic a pro kreslení prvku grafu</param>
        void Draw(TimeGraphItemDrawArgs drawArgs);
    }
    /// <summary>
    /// Interface, který umožní pracovat s časovou osou
    /// </summary>
    public interface ITimeConvertor
    {
        /// <summary>
        /// Identita časového a vizuálního prostoru.
        /// Časový prostor popisuje rozmezí času (Begin a End) s maximální přesností.
        /// Vizuální prostor popisuje počet pixelů velikosti osy (pro osu Horizontal = Width), ale nikoli její pixel počátku (Left).
        /// </summary>
        string Identity { get; }
        /// <summary>
        /// Aktuálně zobrazený interval data a času
        /// </summary>
        TimeRange VisibleTime { get; }
        /// <summary>
        /// Obsahuje všechny aktuální ticky na časové ose.
        /// </summary>
        VisualTick[] Ticks { get; }
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <returns></returns>
        int GetPixel(DateTime? time);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek na aktuální časové ose.
        /// </summary>
        /// <param name="timeRange"></param>
        /// <returns></returns>
        Int32Range GetPixelRange(TimeRange timeRange);
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pixel pro jinou velikost prostoru, než jakou má aktuální TimeAxis, kdy cílová velikost je dána parametrem targetSize.
        /// Jinými slovy: pokud na reálné časové ose máme zobrazeno rozmezí (numerický příklad): 40 - 80,
        /// pak <see cref="GetProportionalPixel(DateTime?, int)"/> pro hodnotu time = 50 a targetSize = 100 vrátí hodnotu 25.
        /// Proč? Protože: požadovaná hodnota 50 se nachází na pozici 0.25 časové osy (40 - 80), a odpovídající pozice v cílovém prostoru (100 pixelů) je 25.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        int GetProportionalPixel(DateTime? time, int targetSize);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek v daném cílovém prostoru.
        /// </summary>
        /// <param name="timeRange"></param>
        /// <returns></returns>
        Int32Range GetProportionalPixelRange(TimeRange timeRange, int targetSize);
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pixel na logaritmické časové ose, kde střední část prostoru (z parametru "size") je proporcionální (její velikost je dána hodnotou "ratio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "size" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        int GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek v daném cílovém prostoru, v logaritmickém měřítku.
        /// Vrací pixel na logaritmické časové ose, kde střední část prostoru (z parametru "size") je proporcionální (její velikost je dána hodnotou "ratio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "size" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="timeRange"></param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        Int32Range GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio);

        /// <summary>
        /// Event vyvolaný po každé změně hodnoty <see cref="VisibleTime"/>
        /// </summary>
        event GPropertyChangedHandler<TimeRange> VisibleTimeChanged;
    }
    /// <summary>
    /// Režim přepočtu DateTime na osu X.
    /// </summary>
    public enum TimeGraphTimeAxisMode
    {
        /// <summary>
        /// Výchozí = podle vlastníka (sloupce, nebo tabulky).
        /// </summary>
        Default = 0,
        /// <summary>
        /// Standardní režim, kdy graf má osu X rovnou 1:1 k prvku TimeAxis.
        /// Využívá se v situaci, kdy prvky grafu jsou kresleny přímo pod TimeAxis.
        /// </summary>
        Standard,
        /// <summary>
        /// Proporcionální režim, kdy graf vykresluje ve své ploše stejný časový úsek jako TimeAxis,
        /// ale graf má jinou šířku v pixelech než časová osa (a tedy může mít i jiný počátek = souřadnici Bounds.X.
        /// Pak se pro přepočet hodnoty DateTime na hodnotu pixelu na ose X nepoužívá přímo TimeConverter, ale prostý přepočet vzdáleností.
        /// </summary>
        ProportionalScale,
        /// <summary>
        /// Logaritmický režim, kdy graf dovolí vykreslit všechny prvky grafu bez ohledu na to, že jejich pozice X (datum) je mimo rozsah TimeAxis.
        /// Vykreslování probíhá tak, že střední část grafu (typicky 60%) zobrazuje prvky proporcionálně (tj. lineárně) k časovému oknu,
        /// a okraje (vlevo a vpravo) zobrazují prvky ležící mimo časové okno, jejichž souřadnice X je určena logaritmicky.
        /// Na souřadnici X = 0 (úplně vlevo v grafu) se zobrazují prvky, jejichž Begin = mínus nekonečno,
        /// a na X = Right (úplně vpravo v grafu) se zobrazují prvky, jejichž End = plus nekonečno.
        /// </summary>
        LogarithmicScale
    }
    /// <summary>
    /// Editovatelnost položky grafu.
    /// </summary>
    [Flags]
    public enum GraphItemEditMode
    {
        /// <summary>
        /// Bez pohybu
        /// </summary>
        None = 0,
        /// <summary>
        /// Lze změnit délku času (roztáhnout šířku pomocí přesunutí začátku nebo konce)
        /// </summary>
        ResizeTime = 0x01,
        /// <summary>
        /// Lze změnit výšku = obsazený prostor v grafu (roztáhnout výšku)
        /// </summary>
        ResizeHeight = 0x02,
        /// <summary>
        /// Lze přesunout položku grafu na ose X = čas doleva / doprava
        /// </summary>
        MoveToAnotherTime = 0x10,
        /// <summary>
        /// Lze přesunout položku grafu na ose Y = na jiný řádek tabulky
        /// </summary>
        MoveToAnotherRow = 0x20,
        /// <summary>
        /// Lze přesunout položku grafu do jiné tabulky
        /// </summary>
        MoveToAnotherTable = 0x40,
        /// <summary>
        /// Defaultní pro pracovní čas = <see cref="ResizeTime"/> | <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/>
        /// </summary>
        DefaultWorkTime = ResizeTime | MoveToAnotherTime | MoveToAnotherRow
    }
    /// <summary>
    /// Tvar položky grafu
    /// </summary>
    public enum TimeGraphElementShape
    {
        Default = 0,
        Rectangle
    }
    #endregion


    //    zrušit:
    #region class TimeGraphItemDrawArgs : třída pro podporu vykreslování položek grafu
    /// <summary>
    /// TimeGraphItemDrawArgs : třída pro podporu vykreslování položek grafu.
    /// Tato třída v sobě uchovává absolutní souřadnici grafu ve WinForm koordinátech, a standardní kreslící argument <see cref="GInteractiveDrawArgs"/>.
    /// </summary>
    public class TimeGraphItemDrawArgs : IDisposable
    {
        #region Constructor, private variables
        /// <summary>
        /// Konstruktor s předáním reference na control, který je parentem grafu.
        /// </summary>
        /// <param name="host"></param>
        public TimeGraphItemDrawArgs(GInteractiveControl host)
        {
            this._Host = host;
        }
        /// <summary>
        /// Připraví data do tohoto argumentu.
        /// </summary>
        /// <param name="drawArgs"></param>
        /// <param name="graphBoundsAbsolute"></param>
        /// <param name="timeConvertor"></param>
        internal void Prepare(GInteractiveDrawArgs drawArgs, Rectangle graphBoundsAbsolute, ITimeConvertor timeConvertor)
        {
            this._DrawArgs = drawArgs;
            this._GraphBoundsAbsolute = graphBoundsAbsolute;
            this._TimeConvertor = timeConvertor;
        }
        private GInteractiveControl _Host;
        private GInteractiveDrawArgs _DrawArgs;
        private Rectangle _GraphBoundsAbsolute;
        private ITimeConvertor _TimeConvertor;
        void IDisposable.Dispose()
        {
            this._DrawSupportDispose();
        }
        #endregion
        #region Public properties
        /// <summary>
        /// An Graphics object to draw on
        /// </summary>
        public Graphics Graphics { get { return this._DrawArgs.Graphics; } }

        /// <summary>
        /// Absolute bounds of Graph
        /// </summary>
        public Rectangle GraphBoundsAbsolute { get { return this._GraphBoundsAbsolute; } }
        #endregion
        #region Draw support
        /// <summary>
        /// Vrátí absolutní souřadnice v koordinátech Windows.Forms.Control z dodaných souřadnic Virtuálních.
        /// Vrácené souřadnice mohou být mimo souřadnice grafu (pak budou oříznuty prostřednictvím Graphics.Clip).
        /// </summary>
        /// <param name="virtualBounds">Virtuální souřadnice prvku</param>
        /// <param name="minimalWidth">Požadavek na minimální šířku prvku</param>
        /// <returns></returns>
        public Rectangle GetBoundsAbsolute(Rectangle virtualBounds, int minimalWidth)
        {
            int graphB = this._GraphBoundsAbsolute.Bottom - 1;
            int graphX = this._GraphBoundsAbsolute.X;
            Rectangle boundsAbsolute = new Rectangle(graphX + virtualBounds.X, graphB - virtualBounds.Y, virtualBounds.Width, virtualBounds.Height);
            if (minimalWidth > 0 && boundsAbsolute.Width < minimalWidth)
                boundsAbsolute.Width = minimalWidth;
            return boundsAbsolute;
        }
        private void _DrawSupportDispose()
        {
        }
        #endregion
    }
    #endregion

}
