using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
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
        private void _ItemList_ItemRemoveAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { this.InvalidateItemList(); }
        /// <summary>
        /// Eventhandler události: do <see cref="ItemList"/> byla přidána položka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemList_ItemAddAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { this.InvalidateItemList(); }
        /// <summary>
        /// Invaliduje platnost položek
        /// </summary>
        protected void InvalidateItemList()
        {
            this.IsValidItems = false;
        }
        /// <summary>
        /// Příznak platnosti položek
        /// </summary>
        private bool IsValidItems;
        /// <summary>
        /// Metoda zajistí provedení kontroly platnosti všech vnitřních dat, podle toho která kontrola a přepočet je zapotřebí.
        /// </summary>
        protected void CheckValid()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "CheckValid", ""))
            {
                this.CheckValidTimeAxis();
                this.CheckValidGroupList();
                this.CheckValidVisibleList();
            }
        }
        #endregion
        #region Bounds : invalidace vhodných částí grafu
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
        }
        #endregion
        #region GUI grafu
        /// <summary>
        /// Parametry pro tento graf.
        /// Buď jsou uloženy přímo zde jako explicitní, nebo jsou načteny z parenta, nebo jsou použity defaultní.
        /// Nikdy nevrací null.
        /// Lze setovat parametry, nebo null.
        /// </summary>
        public TimeGraphParameters GraphParameters
        {
            get
            {
                TimeGraphParameters gp = this._GraphParameters;
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
        private TimeGraphParameters _SearchParentGraphParameters()
        {
            TimeGraphParameters gp = null;
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
        private TimeGraphParameters _GetDefaultGraphParameters()
        {
            if (this._GraphParametersDefault == null)
                this._GraphParametersDefault = TimeGraphParameters.Default;
            return this._GraphParametersDefault;
        }
        private TimeGraphParameters _GraphParameters;
        private TimeGraphParameters _GraphParametersDefault;
        #endregion
        #region TimeAxis : Kontrola platnosti, paměť Identity časové osy
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální hodnoty časové osy <see cref="_TimeConvertor"/>.
        /// Pokud zdejší data jsou vypočítaná pro identický stav časové osy, nechá data beze změn, 
        /// jinak přenačte data osy a invaliduje seznam viditelných dat : <see cref="InvalidateVisibleList()"/>.
        /// </summary>
        protected void CheckValidTimeAxis()
        {
            string identity = this.TimeAxisIdentityCurrent;
            if (String.Equals(identity, this.TimeAxisIdentityPrevious)) return;
            this.RecalculateTimeAxis();
            this.TimeAxisIdentityPrevious = identity;
        }
        /// <summary>
        /// Přenačte do sebe soupis odpovídajících dat z <see cref="_TimeConvertor"/>, 
        /// a invaliduje seznam viditelných dat : <see cref="InvalidateVisibleList()"/>.
        /// </summary>
        protected void RecalculateTimeAxis()
        {
            this.TimeAxisTicks = this._TimeConvertor.Ticks.Where(t => t.TickType == AxisTickType.BigLabel || t.TickType == AxisTickType.StdLabel || t.TickType == AxisTickType.BigTick).ToArray();
            this.TimeAxisBegin = this._TimeConvertor.GetPixel(this._TimeConvertor.VisibleTime.Begin);

            this.InvalidateVisibleList();
        }
        /// <summary>
        /// Obsahuje pole vybraných Ticků z časové osy, protože tyto Ticky se kreslí do grafu.
        /// Obsahuje pouze ticky typu: <see cref="AxisTickType.BigLabel"/>, <see cref="AxisTickType.StdLabel"/>, <see cref="AxisTickType.BigTick"/>.
        /// </summary>
        protected VisualTick[] TimeAxisTicks;
        /// <summary>
        /// Relativní pozice X počátku časové osy.
        /// </summary>
        protected int TimeAxisBegin { get; set; }
        /// <summary>
        /// Identita časové osy, pro kterou byly naposledy přepočítány hodnoty v <see cref="VisibleList"/>.
        /// </summary>
        protected string TimeAxisIdentityPrevious { get; set; }
        /// <summary>
        /// Identita časové osy aktuální, získaná z <see cref="_TimeConvertor"/>.
        /// </summary>
        protected string TimeAxisIdentityCurrent { get { return (this._TimeConvertor != null ? this._TimeConvertor.Identity : null); } }
        /// <summary>
        /// Reference na aktuální TimeConvertor
        /// </summary>
        private ITimeConvertor _TimeConvertor;
        #endregion
        #region GroupList : Seskupování položek z this.ItemList do skupin GTimeGraphGroup, setřídění těchto skupin podle vrstev a hladin na logické ose Y
        /// <summary>
        /// Invaliduje data LogicalY.
        /// Volá se po změnách v poli položek, a po změnách dat v položkách.
        /// Není nutné volat po změnách časové osy, protože zoom ani posun nezmění pozice Y jednotlivých položek.
        /// </summary>
        protected void InvalidateGroupList()
        {
            this.GroupListIsValid = false;
        }
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální logické souřadnice Y.
        /// Pokud jsou neplatné, znovu vytvoří pole <see cref="GroupList"/> a vypočítá logické souřadnice Y.
        /// </summary>
        protected void CheckValidGroupList()
        {
            if (this.GroupList != null && this.IsValidItems && this.GroupListIsValid) return;
            this.RecalculateGroupList();
        }
        /// <summary>
        /// Vypočítá logické souřadnice Y pro všechny položky pole <see cref="ItemList"/>
        /// </summary>
        protected void RecalculateGroupList()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateLogY", ""))
            {
                int layers = 0;
                int levels = 0;
                int groups = 0;
                int items = this.ItemList.Count;

                this.GroupList = new List<List<GTimeGraphGroup>>();
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
                        this.RecalculateGroupListOneLevel(levelGroup, layerUsing, (levelGroup.Key < 0), layerGroupList, layerUsedLogicalY, ref groups);
                    }
                    usedLogicalY.MergeWith(layerUsedLogicalY);

                    this.GroupList.Add(layerGroupList);
                }

                this.CalculateYPrepare(usedLogicalY);

                this.IsValidItems = true;
                this.GroupListIsValid = true;
                this.InvalidateVisibleList();

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
        protected void RecalculateGroupListOneLevel(IEnumerable<ITimeGraphItem> items, PointArray<DateTime, IntervalArray<float>> layerUsing, bool isDownward, List<GTimeGraphGroup> layerGroupList, Interval<float> layerUsedLogicalY, ref int groups)
        {
            float searchFrom = (isDownward ? layerUsedLogicalY.Begin : layerUsedLogicalY.End);
            float nextSearch = searchFrom;

            // Grafické prvky seskupíme podle ITimeGraphItem.GroupId:
            //  více prvků se shodným GroupId tvoří jeden logický celek, tyto prvky jsou vykresleny ve společné linii, nemíchají se s prvky s jiným GroupId.
            // Jedna GroupId reprezentuje například jednu výrobní operaci (nebo přesněji její paralelní průchod), například dva týdny práce;
            //  kdežto jednotlivé položky ITimeGraphItem reprezentují jednotlivé pracovní časy, například jednotlivé směny.
            List<GTimeGraphGroup> groupList = new List<GTimeGraphGroup>();
            IEnumerable<IGrouping<int, ITimeGraphItem>> groupArray = items.GroupBy(i => i.GroupId);
            foreach (IGrouping<int, ITimeGraphItem> group in groupArray)
                groupList.Add(new GTimeGraphGroup(group));                     // Jedna instance GTimeGraphGroup obsahuje jeden nebo více pracovních časů

            // Setřídíme prvky GTimeGraphGroup podle času jejich počátku:
            if (groupList.Count > 1)
                groupList.Sort((a, b) => GTimeGraphGroup.ItemsRecalculateLogicalYCompare(a, b));
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
        protected List<List<GTimeGraphGroup>> GroupList { get; set; }
        /// <summary>
        /// true pokud jsou platné aktuálně vypočtené logické souřadnice Y v poli <see cref="GroupList"/>
        /// </summary>
        protected bool GroupListIsValid { get; set; }
        #endregion
        #region VisibleList : výpočty fyzických pixelových souřadnic prvků na ose X a Y pro grupy GTimeGraphGroup i pro jednotlivé prvky ITimeGraphItem
        /// <summary>
        /// Invaliduje platnost dat v <see cref="VisibleList"/>.
        /// Volá se po změně na časové ose a po přepočtech LogicalY.
        /// </summary>
        protected void InvalidateVisibleList()
        {
            this.IsValidVisibleList = false;
        }
        /// <summary>
        /// Prověří platnost zdejších dat s ohledem na aktuální fyzické pixelové souřadnice X a Y.
        /// Pokud jsou neplatné, znovu vytvoří pole <see cref="VisibleList"/> a patřičné souřadnice vypočte.
        /// </summary>
        protected void CheckValidVisibleList()
        {
            if (this.IsValidVisibleList) return;
            this.RecalculateVisibleList();
        }
        /// <summary>
        /// Vrací true, pokud data v seznamu <see cref="VisibleList"/> jsou platná.
        /// Zohledňuje i stav <see cref="VisibleList"/>, <see cref="VisibleListLastWidth"/>, <see cref="GraphParameters"/>: <see cref="TimeGraphParameters.TimeAxisMode"/>.
        /// Hodnotu lze nastavit, ale i když se vloží true, může se vracet false (pokud výše uvedené není platné).
        /// </summary>
        protected bool IsValidVisibleList
        {
            get
            {
                if (this.VisibleList == null) return false;
                if (!this.VisibleListLastWidth.HasValue || this.ClientSize.Width != this.VisibleListLastWidth.Value) return false;
                if (!this.VisibleListLastTimeAxisMode.HasValue || this.GraphParameters.TimeAxisMode != this.VisibleListLastTimeAxisMode.Value) return false;
                return this._IsValidVisibleList;
            }
            set
            {
                this._IsValidVisibleList = value;
            }
        }
        private bool _IsValidVisibleList;
        /// <summary>
        /// Naplní korektní data do pole <see cref="VisibleList"/> a vypočte patřičné pixelové souřadnice.
        /// </summary>
        protected void RecalculateVisibleList()
        {
            this.VisibleList = new List<List<GTimeGraphGroup>>();
            ITimeConvertor timeConvertor = this._TimeConvertor;
            if (timeConvertor == null) return;
            TimeGraphTimeAxisMode timeAxisMode = this.GraphParameters.TimeAxisMode;

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateVisibleList", ""))
            {
                int layers = 0;
                int groups = 0;
                int items = 0;

                foreach (List<GTimeGraphGroup> layerList in this.GroupList)
                {   // Jedna vizuální vrstva za druhou:
                    List<GTimeGraphGroup> visibleItems = new List<GTimeGraphGroup>();
                    foreach (GTimeGraphGroup group in layerList)
                    {   // Jeden prvek (GTimeGraphGroup) za druhým:
                        switch (timeAxisMode)
                        {
                            case TimeGraphTimeAxisMode.ProportionalScale:
                                this.RecalculateVisibleListOneGroupProportional(group, visibleItems, ref groups, ref items);
                                break;
                            case TimeGraphTimeAxisMode.LogarithmicScale:
                                this.RecalculateVisibleListOneGroupLogarithmic(group, visibleItems, ref groups, ref items);
                                break;
                            case TimeGraphTimeAxisMode.Standard:
                            default:
                                this.RecalculateVisibleListOneGroupStandard(group, visibleItems, ref groups, ref items);
                                break;
                        }
                    }

                    if (visibleItems.Count > 0)
                    {
                        this.VisibleList.Add(visibleItems);
                        layers++;
                    }
                }

                this.VisibleListLastWidth = this.ClientSize.Width;
                this.VisibleListLastTimeAxisMode = timeAxisMode;
                this.IsValidVisibleList = true;

                scope.AddItem("Visual Layers Count: " + layers.ToString());
                scope.AddItem("Visual Groups Count: " + groups.ToString());
                scope.AddItem("Visual Items Count: " + items.ToString());
            }
        }
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.Standard"/>
        /// </summary>
        /// <param name="group">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="visibleItems">Výstupní seznam, do něhož se vkládají viditelné prvky</param>
        /// <param name="groups">Počet viditelných prvků group, pro statistiku</param>
        /// <param name="items">Počet zpracovaných prvků typu <see cref="ITimeGraphItem"/>, pro statistiku</param>
        protected void RecalculateVisibleListOneGroupStandard(GTimeGraphGroup group, List<GTimeGraphGroup> visibleItems, ref int groups, ref int items)
        {
            ITimeConvertor timeConvertor = this._TimeConvertor;
            if (group.IsValidRealTime && timeConvertor.VisibleTime.HasIntersect(group.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                groups++;
                Int32Range y = this.CalculatorYGetRange(group.LogicalY);
                Int32Range x = timeConvertor.GetPixelRange(group.Time);
                group.VirtualBounds = Int32Range.GetRectangle(x, y);

                foreach (ITimeGraphItem item in group.Items)
                {
                    items++;

                    x = timeConvertor.GetPixelRange(item.Time);
                    item.VirtualBounds = Int32Range.GetRectangle(x, y);
                }

                visibleItems.Add(group);
            }
        }
        protected void RecalculateVisibleListOneGroupProportional(GTimeGraphGroup group, List<GTimeGraphGroup> visibleItems, ref int groups, ref int items)
        {
            ITimeConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            if (group.IsValidRealTime && timeConvertor.VisibleTime.HasIntersect(group.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                groups++;
                Int32Range y = this.CalculatorYGetRange(group.LogicalY);
                Int32Range x = timeConvertor.GetProportionalPixelRange(group.Time, size);
                group.VirtualBounds = Int32Range.GetRectangle(x, y);

                foreach (ITimeGraphItem item in group.Items)
                {
                    items++;

                    x = timeConvertor.GetProportionalPixelRange(item.Time, size);
                    item.VirtualBounds = Int32Range.GetRectangle(x, y);
                }

                visibleItems.Add(group);
            }
        }
        protected void RecalculateVisibleListOneGroupLogarithmic(GTimeGraphGroup group, List<GTimeGraphGroup> visibleItems, ref int groups, ref int items)
        {
            ITimeConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            float proportionalRatio = this.GraphParameters.LogarithmicRatio;
            // Pozor: režim Logarithmic zajistí, že zobrazeny budou VŠECHNY prvky, takže prvky nefiltrujeme s ohledem na jejich čas : VisibleTime.HasIntersect() !
            if (group.IsValidRealTime)
            {
                groups++;
                Int32Range y = this.CalculatorYGetRange(group.LogicalY);
                Int32Range x = timeConvertor.GetLogarithmicPixelRange(group.Time, size, proportionalRatio);
                group.VirtualBounds = Int32Range.GetRectangle(x, y);

                foreach (ITimeGraphItem item in group.Items)
                {
                    items++;

                    x = timeConvertor.GetLogarithmicPixelRange(item.Time, size, proportionalRatio);
                    item.VirtualBounds = Int32Range.GetRectangle(x, y);
                }

                visibleItems.Add(group);
            }
        }
        /// <summary>
        /// Seznam všech aktuálně viditelných prvků v grafu.
        /// Seznam má dvojitou úroveň: v první úrovni jsou vizuální vrstvy (od spodní po vrchní), 
        /// v druhé úrovni jsou pak jednotlivé prvky <see cref="GTimeGraphGroup"/> k vykreslení.
        /// </summary>
        protected List<List<GTimeGraphGroup>> VisibleList { get; set; }
        /// <summary>
        /// Hodnota Bounds.Width, pro kterou byly naposledy přepočítávány prvky pole <see cref="VisibleList"/>.
        /// Po změně souřadnic se provádí invalidace.
        /// </summary>
        protected int? VisibleListLastWidth { get; set; }
        /// <summary>
        /// Hodnota <see cref="GraphParameters"/>: <see cref="TimeGraphParameters.TimeAxisMode"/>, pro kterou jsou platné hodnoty ve <see cref="VisibleList"/>.
        /// Po změně <see cref="GraphParameters"/>: <see cref="TimeGraphParameters.TimeAxisMode"/> dojde k přepočtu dat v tomto seznamu.
        /// </summary>
        protected TimeGraphTimeAxisMode? VisibleListLastTimeAxisMode { get; set; }
        #endregion
        #region Kalkulátor souřadnic X : přepočet z DateTime na pixel s pomocí časové osy a režimu

        #endregion
        #region Kalkulátor souřadnic Y : výška grafu a přepočty souřadnice Y z logické (float, zdola nahoru) do fyzických pixelů (int, zhora dolů)
        /// <summary>
        /// Instance objektu, jehož výšku může graf změnit i číst pro korektní přepočty svých vnitřních souřadnic.
        /// Typicky se sem vkládá řádek grafu, instance třídy <see cref="Row"/>.
        /// Graf nikdy nepracuje se šířkou parenta <see cref="IVisualParent.ClientWidth"/>.
        /// </summary>
        public IVisualParent VisualParent { get { return this._VisualParent; } set { this._VisualParent = value; this.InvalidateVisibleList(); } }
        /// <summary>
        /// Aktuální výška dat celého grafu, v pixelech
        /// </summary>
        public int GraphPixelHeight
        {
            get
            {
                this.CheckValidGroupList();
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

            this.InvalidateVisibleList();
        }
        /// <summary>
        /// Metoda zajistí zarovnání výšky grafu (v pixelech) do patřičného rozmezí.
        /// Využívá: rozmezí <see cref="GraphParameters"/>: <see cref="TimeGraphParameters.TotalHeightRange"/>, hodnoty Skin.Graph.TotalHeightMin a TotalHeightMax;
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
        #region Draw : vykreslení grafu
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
        /// Metoda umožní udělat něco s pozadím grafu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this.GraphParameters.TimeAxisMode == TimeGraphTimeAxisMode.LogarithmicScale)
                this.DrawBackgroundLogarithmic(e, boundsAbsolute);
        }
        /// <summary>
        /// Metoda umožní udělat něco s pozadím grafu, který má logaritmickou osu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected virtual void DrawBackgroundLogarithmic(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Color color1 = Color.FromArgb(0, 0, 0, 0);
            Color color2 = Color.FromArgb(48, 0, 0, 0);
            int width = (int)(((1f - this.GraphParameters.LogarithmicRatio) / 2f) * (float)boundsAbsolute.Width);

            Rectangle leftBounds = new Rectangle(boundsAbsolute.X, boundsAbsolute.Y, width, boundsAbsolute.Height);
            Rectangle leftBoundsG = leftBounds; leftBoundsG.X = leftBoundsG.X - 1;              // To je úchylka WinFormů
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(leftBoundsG, color2, color1, 00f))
            {
                e.Graphics.FillRectangle(lgb, leftBounds);
            }

            Rectangle rightBounds = new Rectangle(boundsAbsolute.Right - width, boundsAbsolute.Y, width, boundsAbsolute.Height);
            Rectangle rightBoundsG = rightBounds; rightBoundsG.X = rightBoundsG.X - 1;          // To je úchylka WinFormů
            using (System.Drawing.Drawing2D.LinearGradientBrush rgb = new System.Drawing.Drawing2D.LinearGradientBrush(rightBoundsG, color2, color1, 180f))
            {
                e.Graphics.FillRectangle(rgb, rightBounds);
            }
        }
        /// <summary>
        /// Prepare this.ItemDrawArgs for subsequent Draw operations (prepare, store new Graphics and boundsAbsolute, and current _TimeConvertor)
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
        /// Draw all ticks
        /// </summary>
        protected void DrawTicks()
        {
            if (!this.GraphParameters.TimeAxisTickIsVisible) return;

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "PaintGrid", ""))
            {
                int x;
                int x0 = this.ItemDrawArgs.GraphBoundsAbsolute.X + this.TimeAxisBegin;
                int y1 = this.ItemDrawArgs.GraphBoundsAbsolute.Top;
                int y2 = this.ItemDrawArgs.GraphBoundsAbsolute.Bottom - 1;

                foreach (VisualTick tick in this.TimeAxisTicks)
                {
                    x = x0 + tick.RelativePixel;
                    switch (tick.TickType)
                    {
                        case AxisTickType.BigLabel:
                            this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 2f, System.Drawing.Drawing2D.DashStyle.Solid);
                            break;

                        case AxisTickType.StdLabel:
                            this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 1f, System.Drawing.Drawing2D.DashStyle.Solid);
                            break;

                        case AxisTickType.BigTick:
                            this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 1f, System.Drawing.Drawing2D.DashStyle.Dot);
                            break;
                    }
                }
            }
        }
        protected void DrawItems()
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "PaintItems", ""))
            {
                int layers = 0;
                int groups = 0;
                int items = 0;

                foreach (List<GTimeGraphGroup> layerList in this.VisibleList)
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
        protected TimeGraphItemDrawArgs ItemDrawArgs;
        #endregion
        #region ITimeGraph members
        ITimeConvertor ITimeGraph.TimeConvertor { get { return this._TimeConvertor; } set { this._TimeConvertor = value; this.InvalidateVisibleList(); } }
        int ITimeGraph.UnitHeight { get { return this.GraphParameters.OneLineHeight.Value; } } 
        void ITimeGraph.DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute) { this.DrawContentTimeGraph(e, boundsAbsolute); }
        #endregion
    }
    #region class GTimeGraphGroup : Group of one or more ITimeGraphItem, with summary Time and maximal Height from items
    /// <summary>
    /// GTimeGraphGroup : Group of one or more ITimeGraphItem, with summary Time and maximal Height from items
    /// </summary>
    public class GTimeGraphGroup : ITimeGraphItem
    {
        #region Constructors - from IEnumerable<ITimeGraphItem> items
        public GTimeGraphGroup()
        {
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
            this._FirstItem = null;
        }
        /// <summary>
        /// Konstruktor s předáním skupiny položek, s výpočtem jejich sumárního časového intervalu a výšky
        /// </summary>
        /// <param name="items"></param>
        public GTimeGraphGroup(IEnumerable<ITimeGraphItem> items)
            : this()
        {
            this._Items = items.ToArray();
            float height = 0f;
            DateTime? begin = null;
            DateTime? end = null;
            foreach (ITimeGraphItem item in this.Items)
            {
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height > height) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
            }
            this._Height = height;
            this._Time = new TimeRange(begin, end);
            this._IsValidRealTime = ((height > 0f) && (begin.HasValue && end.HasValue && end.Value > begin.Value));
        }
        public override string ToString()
        {
            return "Time: " + this.Time.ToString() +
                "; Height: " + this.Height.ToString() +
                "; UseSpace: " + (this.LogicalY == null ? "none" : this.LogicalY.ToString());
        }
        #endregion
        #region Private members
        private int _ItemId;
        private ITimeGraphItem _FirstItem;
        private ITimeGraphItem[] _Items;
        private float _Height;
        private TimeRange _Time;
        private bool _IsValidRealTime;
        private Interval<float> _LogicalY;
        private Rectangle _VirtualBounds;
        private Rectangle _Bounds;
        #endregion
        #region Public properties, Draw()
        /// <summary>
        /// All items in this Group. Always has at least one item.
        /// </summary>
        public ITimeGraphItem[] Items { get { return this._Items; } }
        /// <summary>
        /// Count of items in Items array
        /// </summary>
        public int ItemCount { get { return this._Items.Length; } }
        /// <summary>
        /// Logical height of this item. Only postive Height is seen as Real.
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// Summary time of all items.
        /// Only positive time is seen as real (End is higher than Begin).
        /// </summary>
        public TimeRange Time { get { return this._Time; } }
        /// <summary>
        /// true when this is real item: has positive Height and its Time.End is higher (not equal!) to Time.Begin
        /// </summary>
        internal bool IsValidRealTime { get { return this._IsValidRealTime; } }
        /// <summary>
        /// Allocated logical space on the Y axis (not pixels). Value of 1 is standard logical unit of height.
        /// </summary>
        public Interval<float> LogicalY
        {
            get { return this._LogicalY; }
            set
            {
                this._LogicalY = value.ValueClone;
                this.Items.ForEachItem(i => i.LogicalY = this._LogicalY);
            }
        }
        /// <summary>
        /// Virtual bounds in pixels, where X axis is same as Bounds, but Y axis is reverted (Virtual Y has 0 at bottom, in contrast to WinForm Y which has 0 at top)
        /// </summary>
        public Rectangle VirtualBounds { get { return this._VirtualBounds; } set { this._VirtualBounds = value; } }
        /// <summary>
        /// Relative bounds in pixels, in standard bounds coordinates as WinForm control
        /// </summary>
        public Rectangle Bounds { get { return this._Bounds; } set { this._Bounds = value; } }
        /// <summary>
        /// Draw this group
        /// </summary>
        /// <param name="drawArgs">All data and support for drawing</param>
        public void Draw(TimeGraphItemDrawArgs drawArgs)
        {
            if (!this.IsValidRealTime || this._FirstItem.Layer < 0 || this.ItemCount <= 1) return;
            drawArgs.FillRectangle(this.VirtualBounds, Color.FromArgb(160, Color.Gray), -1, -1, -1, -1);
        }
        /// <summary>
        /// Compare two instance by Order ASC, Time.Begin ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int ItemsRecalculateLogicalYCompare(GTimeGraphGroup a, GTimeGraphGroup b)
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
        Interval<float> ITimeGraphItem.LogicalY { get { return this.LogicalY; } set { this.LogicalY = value; } }
        Rectangle ITimeGraphItem.VirtualBounds { get { return this.VirtualBounds; } set { this.VirtualBounds = value; } }
        Rectangle ITimeGraphItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        void ITimeGraphItem.Draw(TimeGraphItemDrawArgs drawArgs) { this.Draw(drawArgs); }
        #endregion
    }
    #endregion
    public class GTimeGraphItem : ITimeGraphItem
    {
        #region Public members
        public GTimeGraphItem()
        {
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
        }
        /// <summary>
        /// ID of this item
        /// </summary>
        public Int32 ItemId { get { return this._ItemId; } } private Int32 _ItemId;
        /// <summary>
        /// Visual layer.
        /// Items are drawed from lowest layer to highest.
        /// Items on different layers can be drawed one over another, items on same layer is drawed on different Y coordinate.
        /// </summary>
        public Int32 Layer { get; set; }
        /// <summary>
        /// Visual level.
        /// Items are positioned to visual level from bottom (logical Y = 0) up.
        /// Items of level 1 began at topmost coordinate of items from level 0, and so on.
        /// </summary>
        public Int32 Level { get; set; }
        /// <summary>
        /// Order of item. Items in same Order are stored to graph on order their Time.Begin, item with higher Order are stored after store all items with lower Order.
        /// </summary>
        public Int32 Order { get; set; }
        /// <summary>
        /// Group of items for one logical unit (has items in more rows, or more items in one row).
        /// Items from one group in same row has same Y coordinate.
        /// Items from one group in another rows is "fixed" together.
        /// </summary>
        public Int32 GroupId { get; set; }
        /// <summary>
        /// Time of this item
        /// </summary>
        public virtual TimeRange Time { get; set; }
        /// <summary>
        /// Height of item, where value 1.0 = ITimeGraph.UnitHeight
        /// </summary>
        public float Height { get; set; }
        
        public Color? BackColor { get; set; }
        public Color? BorderColor { get; set; }
        public Color? TextColor { get; set; }
        public string[] Captions { get; set; }
        public string ToolTip { get; set; }
        #endregion
        #region Protected members - VirtualBounds, LogicalY, Draw()
        /// <summary>
        /// Virtual bounds in pixels, where X axis is same as Bounds, but Y axis is reverted (Virtual Y has 0 at bottom, in contrast to WinForm Y which has 0 at top)
        /// </summary>
        protected Rectangle VirtualBounds { get; set; }
        /// <summary>
        /// Relative bounds in pixels, in standard bounds coordinates as WinForm control
        /// </summary>
        protected Rectangle Bounds { get; set; }
        /// <summary>
        /// Logical coordinates on Y axis in Graph
        /// </summary>
        protected virtual Interval<float> LogicalY { get; set; }
        /// <summary>
        /// true if this item has positive Height and Time
        /// </summary>
        protected bool IsValidRealTime { get { return (this.Time != null && this.Time.IsFilled && this.Time.IsReal); } }
        /// <summary>
        /// Draw this item
        /// </summary>
        protected virtual void Draw(TimeGraphItemDrawArgs drawArgs)
        {
            if (!this.IsValidRealTime) return;
            Rectangle bounds = this.VirtualBounds;
            if (this.Layer >= 0)
            {
             //   bounds.Y = bounds.Y + 1;
             //   bounds.Height = bounds.Height - 1;
            }
            if (bounds.Width < 1) bounds.Width = 1;
            int w = bounds.Width;
            if (w <= 2)
            {
                drawArgs.FillRectangle(bounds, this.BorderColor);
            }
            else
            {
                drawArgs.FillRectangle(bounds, this.BackColor);
                drawArgs.BorderRectangle(bounds, this.BorderColor);
            }
        }
        #endregion
        #region explicit ITimeGraphItem members
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.Layer { get { return this.Layer; } }
        int ITimeGraphItem.Level { get { return this.Level; } }
        int ITimeGraphItem.Order { get { return this.Order; } }
        int ITimeGraphItem.GroupId { get { return this.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this.Time; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        Interval<float> ITimeGraphItem.LogicalY { get { return this.LogicalY; } set { this.LogicalY = value; } }
        Rectangle ITimeGraphItem.VirtualBounds { get { return this.VirtualBounds; } set { this.VirtualBounds = value; } }
        Rectangle ITimeGraphItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        void ITimeGraphItem.Draw(TimeGraphItemDrawArgs drawArgs) { this.Draw(drawArgs); }
        #endregion
    }
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
        /// Layer: Vizuální vrstva. Prvky z různých vrstev jsou kresleny "přes sebe" = mohou se překrývat.
        /// Nižší hodnota je kreslena dříve.
        /// Například: záporná hodnota Layer reprezentuje "podklad" který se needituje.
        /// </summary>
        Int32 Layer { get; }
        /// <summary>
        /// Level: Hladina. Prvky v jedné hladině jsou kresleny do společného vodorovného pásu, 
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
        /// GroupId: číslo skupiny. Prvky se shodným GroupId budou vykreslovány do společného "rámce", 
        /// a pokud mezi jednotlivými prvky <see cref="ITimeGraphItem"/> se shodným <see cref="GroupId"/> bude na ose X nějaké volné místo,
        /// nebude mezi nimi vykreslován žádný "cizí" prvek.
        /// </summary>
        Int32 GroupId { get; }
        /// <summary>
        /// Časový interval tohoto prvku
        /// </summary>
        TimeRange Time { get; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.GraphParameters"/>: <see cref="TimeGraphParameters.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        float Height { get; }
        /// <summary>
        /// 
        /// Logical coordinates on Y axis in Graph
        /// </summary>
        Interval<float> LogicalY { get; set; }
        /// <summary>
        /// Virtual bounds in pixels, where X axis is same as Bounds, but Y axis is reverted (Virtual Y has 0 at bottom, in contrast to WinForm Y which has 0 at top)
        /// </summary>
        Rectangle VirtualBounds { get; set; }
        /// <summary>
        /// Relative bounds in pixels, in standard bounds coordinates as WinForm control
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Draw this item
        /// </summary>
        /// <param name="drawArgs">All data and support for drawing</param>
        void Draw(TimeGraphItemDrawArgs drawArgs);
    }
    /// <summary>
    /// Interface, který umožní pracovat s časovou osou
    /// </summary>
    public interface ITimeConvertor
    {
        /// <summary>
        /// Identita časového a vizuálního prostoru
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
    #endregion
    #region class TimeGraphItemDrawArgs : třída pro podporu vykreslování položek grafu
    public class TimeGraphItemDrawArgs : IDisposable
    {
        #region Constructor, private variables
        public TimeGraphItemDrawArgs(GInteractiveControl host)
        {
            this._Host = host;
        }
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
        /// Layer, currently drawed.
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get { return this._DrawArgs.DrawLayer; } }
        /// <summary>
        /// Whole GInteractiveDrawArgs
        /// </summary>
        public GInteractiveDrawArgs InteractiveDrawArgs { get { return this._DrawArgs; } }
        /// <summary>
        /// Absolute bounds of Graph
        /// </summary>
        public Rectangle GraphBoundsAbsolute { get { return this._GraphBoundsAbsolute; } }
        /// <summary>
        /// Time convertor for X axis
        /// </summary>
        public ITimeConvertor TimeConvertor { get { return this._TimeConvertor; } }
        #endregion
        #region Draw support
        /// <summary>
        /// Default color for fill rectangle
        /// </summary>
        public Color DefaultBackColor { get { return this._Host.DefaultBackColor; } set { this._Host.DefaultBackColor = value; } }
        /// <summary>
        /// Default color for border rectangle
        /// </summary>
        public Color DefaultBorderColor { get { return this._Host.DefaultBorderColor; } set { this._Host.DefaultBorderColor = value; } }
        /// <summary>
        /// Fill rectangle (convert VirtualBounds to real bounds), with color (or DefaultBackColor).
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="backColor"></param>
        public void FillRectangle(Rectangle virtualBounds, Color? backColor)
        {
            this._FillRectangle(virtualBounds, backColor, false, 0, 0, 0, 0);
        }
        /// <summary>
        /// Fill rectangle (convert VirtualBounds to real bounds), with color (or DefaultBackColor).
        /// Real bounds are enlarged (Rectangle.Enlarge()) by specified values for each edge: positive value produce greater bounds, negative value smaller bounds.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="backColor"></param>
        /// <param name="enlargeL"></param>
        /// <param name="enlargeT"></param>
        /// <param name="enlargeR"></param>
        /// <param name="enlargeB"></param>
        public void FillRectangle(Rectangle virtualBounds, Color? backColor, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            this._FillRectangle(virtualBounds, backColor, true, enlargeL, enlargeT, enlargeR, enlargeB);
        }
        private void _FillRectangle(Rectangle virtualBounds, Color? backColor, bool enlarge, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            Rectangle bounds = this.GetBounds(virtualBounds);
            if (enlarge)
                bounds = bounds.Enlarge(enlargeL, enlargeT, enlargeR, enlargeB);
            if (this._IsBoundsVisible(bounds))
                this._Host.FillRectangle(this.Graphics, bounds, (backColor.HasValue ? backColor.Value : this.DefaultBackColor));
        }
        /// <summary>
        /// Draw Border around Virtual bounds, with specified color (or DefaultBorderColor).
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="borderColor"></param>
        public void BorderRectangle(Rectangle virtualBounds, Color? borderColor)
        {
            this._BorderRectangle(virtualBounds, borderColor, false, 0, 0, 0, 0);
        }
        /// <summary>
        /// Draw Border around Virtual bounds, with specified color (or DefaultBorderColor).
        /// Real bounds are enlarged (Rectangle.Enlarge()) by specified values for each edge: positive value produce greater bounds, negative value smaller bounds.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="borderColor"></param>
        /// <param name="enlargeL"></param>
        /// <param name="enlargeT"></param>
        /// <param name="enlargeR"></param>
        /// <param name="enlargeB"></param>
        public void BorderRectangle(Rectangle virtualBounds, Color? borderColor, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            this._BorderRectangle(virtualBounds, borderColor, true, enlargeL, enlargeT, enlargeR, enlargeB);
        }
        private void _BorderRectangle(Rectangle virtualBounds, Color? borderColor, bool enlarge, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            Rectangle bounds = this.GetBounds(virtualBounds);
            bounds = bounds.Enlarge(enlargeL, enlargeT, enlargeR - 1, enlargeB - 1);     // Shring Width and Height by 1 pixel is standard for draw Border into (!) area.
            if (this._IsBoundsVisible(bounds))
            {
                Color color = (borderColor.HasValue ? borderColor.Value : this.DefaultBorderColor);
                Pen pen = Skin.Pen(color);
                this.Graphics.DrawRectangle(pen, bounds);
            }
        }
        /// <summary>
        /// Return absolute WinForm bounds for specified Virtual Bounds.
        /// Returned bounds can be outside of visible bounds of Graph.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public Rectangle GetBounds(Rectangle virtualBounds)
        {
            int graphB = this._GraphBoundsAbsolute.Bottom - 4;
            int graphX = this._GraphBoundsAbsolute.X;
            return new Rectangle(graphX + virtualBounds.X, graphB - virtualBounds.Y, virtualBounds.Width, virtualBounds.Height);
        }
        /// <summary>
        /// Return true when specified item Virtual bounds is (whole or partially) visible in current Graph (in GraphBoundsAbsolute).
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool IsVirtualBoundsVisible(Rectangle virtualBounds)
        {
            Rectangle bounds = this._GetBounds(virtualBounds);
            return this._IsBoundsVisible(bounds);
        }
        /// <summary>
        /// Return true when specified item Absolute bounds is (whole or partially) visible in current Graph (in GraphBoundsAbsolute).
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool IsBoundsVisible(Rectangle bounds)
        {
            return this._IsBoundsVisible(bounds);
        }
        /// <summary>
        /// Return absolute WinForm bounds for specified Virtual Bounds.
        /// Returned bounds can be outside of visible bounds of Graph.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        private Rectangle _GetBounds(Rectangle virtualBounds)
        {
            int graphB = this._GraphBoundsAbsolute.Bottom - 4;
            int graphX = this._GraphBoundsAbsolute.X;
            return new Rectangle(graphX + virtualBounds.X, graphB - virtualBounds.Y, virtualBounds.Width, virtualBounds.Height);
        }
        /// <summary>
        /// Return true when specified item absolute bounds is (whole or partially) visible in current Graph (in GraphBoundsAbsolute).
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private bool _IsBoundsVisible(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            Rectangle graphBounds = this.GraphBoundsAbsolute;
            return !(
                    bounds.Right <= graphBounds.Left ||
                    bounds.Bottom <= graphBounds.Top ||
                    bounds.Left >= graphBounds.Right ||
                    bounds.Top >= graphBounds.Bottom);
        }
        /// <summary>
        /// Draw one line using standard Pen, 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="dashStyle"></param>
        public void DrawLine(int x1, int y1, int x2, int y2, Color color, float width, System.Drawing.Drawing2D.DashStyle dashStyle)
        {
            Pen pen = Skin.Pen(color, width, dashStyle);
            this.Graphics.DrawLine(pen, x1, y1, x2, y2);
        }
        private void _DrawSupportDispose()
        {
        }
        #endregion
    }
    #endregion
    #region class TimeGraphParameters : třída obsahující vlastnosti vykreslovaného grafu
    /// <summary>
    /// TimeGraphParameters : třída obsahující vlastnosti vykreslovaného grafu
    /// </summary>
    public class TimeGraphParameters
    {
        /// <summary>
        /// Defaultní nastavení
        /// </summary>
        public static TimeGraphParameters Default { get { return new TimeGraphParameters(); } }
        /// <summary>
        /// Defaultní konstruktor
        /// </summary>
        public TimeGraphParameters()
        {
            this._TimeAxisMode = TimeGraphTimeAxisMode.Standard;
            this._TimeAxisTickIsVisible = true;
            this._OneLineHeight = Skin.Graph.LineHeight;
            this._LogarithmicRatio = 0.60f;
        }
        /// <summary>
        /// Režim zobrazování času na ose X
        /// </summary>
        public TimeGraphTimeAxisMode TimeAxisMode { get { return this._TimeAxisMode; } set { this._TimeAxisMode = value; } } private TimeGraphTimeAxisMode _TimeAxisMode;
        /// <summary>
        /// true pokud mají být v grafu zobrazovány časové linky (Ticks).
        /// Pro graf s režimem osy <see cref="TimeAxisMode"/> == <see cref="TimeGraphTimeAxisMode.Standard"/> 
        /// jsou souřadnice značek převzaty z jejich dat napřímo.
        /// Pro graf s režimem osy <see cref="TimeAxisMode"/> == <see cref="TimeGraphTimeAxisMode.ProportionalScale"/> 
        /// jsou souřadnice značek přepočteny do aktuálního prostoru.
        /// Pro graf s režimem osy <see cref="TimeAxisMode"/> == <see cref="TimeGraphTimeAxisMode.LogarithmicScale"/> 
        /// nejsou časové značky nikdy vykreslovány.
        /// </summary>
        public bool TimeAxisTickIsVisible
        {
            get
            {
                TimeGraphTimeAxisMode timeAxisMode = this.TimeAxisMode;
                if (timeAxisMode == TimeGraphTimeAxisMode.LogarithmicScale) return false;          // Tento typ grafu (LogarithmicScale) nikdy nemá TimeAxisTick
                return this._TimeAxisTickIsVisible;
            }
            set
            {
                this._TimeAxisTickIsVisible = value;
            }
        }
        private bool _TimeAxisTickIsVisible;
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
        /// Rozsah lineární části grafu uprostřed logaritmické časové osy.
        /// Default = 0.60f, povolené rozmezí od 0.40f po 0.90f.
        /// </summary>
        public float LogarithmicRatio
        {
            get { return this._LogarithmicRatio; }
            set { float v = value; this._LogarithmicRatio = (v < 0.4f ? 0.4f : (v > 0.9f ? 0.9f : v)); }
        }
        private float _LogarithmicRatio;

    }
    #endregion
}
