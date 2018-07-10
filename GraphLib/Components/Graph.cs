using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;

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
        /// Naplní hodnoty: <see cref="GTimeGraphGroup.CoordinateYLogical"/> a <see cref="GTimeGraphGroup.CoordinateYVirtual"/>, 
        /// připraví kalkulátor <see cref="CalculatorY"/>.
        /// </summary>
        protected void CheckValidAllGroupList()
        {
            if (this._AllGroupList == null)
                this.RecalculateAllGroupList();
        }
        /// <summary>
        /// Vypočítá logické souřadnice Y pro všechny položky pole <see cref="ItemList"/>.
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
                int items = this.ItemList.Count;

                this._AllGroupList = new List<List<GTimeGraphGroup>>();
                Interval<float> totalLogicalY = new Interval<float>(0f, 0f, true);

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
                    totalLogicalY.MergeWith(layerUsedLogicalY);

                    this._AllGroupList.Add(layerGroupList);
                }
                this.Invalidate(InvalidateItems.CoordinateX | InvalidateItems.CoordinateYVirtual);

                this.CalculatorY.Prepare(totalLogicalY);

                this.RecalculateCoordinateYVirtual();

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
                    group.CoordinateYLogical = useSpace;

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
        /// Metoda projde všechny prvky <see cref="GTimeGraphGroup"/> v poli <see cref="AllGroupList"/>, a pro každý prvek provede danou akci.
        /// </summary>
        /// <param name="action"></param>
        protected void AllGroupScan(Action<GTimeGraphGroup> action)
        {
            foreach (List<GTimeGraphGroup> layer in this.AllGroupList)
                foreach (GTimeGraphGroup group in layer)
                    action(group);
        }
        /// <summary>
        /// Seznam všech skupin prvků k zobrazení v grafu.
        /// Seznam má dvojitou úroveň: v první úrovni jsou vizuální vrstvy (od spodní po vrchní), 
        /// v druhé úrovni jsou pak jednotlivé prvky <see cref="GTimeGraphGroup"/> k vykreslení.
        /// </summary>
        protected List<List<GTimeGraphGroup>> AllGroupList { get { this.CheckValidAllGroupList(); return this._AllGroupList; } } private List<List<GTimeGraphGroup>> _AllGroupList;
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
            /// <param name="totalLogicalRange"></param>
            public void Prepare(Interval<float> totalLogicalRange)
            {
                this._IsPrepared = false;
                this._TotalLogicalRange = totalLogicalRange;
                if (totalLogicalRange == null) return;

                float logBegin = (totalLogicalRange.Begin < 0f ? totalLogicalRange.Begin : 0f);
                float logEnd = (totalLogicalRange.End > 1f ? totalLogicalRange.End : 1f) + this._GraphProperties.UpperSpaceLogical;
                float logSize = logEnd - logBegin;

                // Výška dat grafu v pixelech, zarovnaná do patřičných mezí:
                int pixelSize = this._AlignTotalPixelSize((int)(Math.Ceiling(logSize * (float)this._GraphProperties.OneLineHeight.Value)));
                this._TotalPixelSize = pixelSize;

                // Výpočty kalkulátoru, invalidace VisibleList:
                this._Calculator_Offset = logBegin;
                this._Calculator_Scale = (float)pixelSize / logSize;

                this._Owner.Invalidate(InvalidateItems.CoordinateYReal);
                this._IsPrepared = true;
            }
            /// <summary>
            /// Metoda vrátí rozsah hodnot ve virtuálním formátu, pro zadané logické souřadnice.
            /// Virtuální formát je v pixelech, ale hodnota 0 odpovídá dolnímu okraji grafu.
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
            /// Využívá: rozmezí <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TotalHeightRange"/>, hodnoty Skin.Graph.TotalHeightMin a TotalHeightMax;
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
            private TimeGraphProperties _GraphProperties { get { return this._Owner.GraphParameters; } }
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
        /// a po provedení přípravy kalkulátoru Y (<see cref="PositionCalculatorInfo.Prepare(Interval{float})"/>, 
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
            ITimeConvertor timeConvertor = this._TimeConvertor;
            if (timeConvertor == null) return;
            this._TimeAxisBegin = timeConvertor.GetPixel(timeConvertor.VisibleTime.Begin);
            this._TimeAxisTicks = timeConvertor.Ticks.Where(t => t.TickType == AxisTickType.BigLabel || t.TickType == AxisTickType.StdLabel || t.TickType == AxisTickType.BigTick).ToArray();
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
        private ITimeConvertor _TimeConvertor;
        #endregion
        #region Souřadnice X : kontrolní a přepočtové metody souřadnic na ose X, algoritmy Native, Proportional, Logarithmic; určení Visible prvků do VisibleGroupList
        /// <summary>
        /// Metoda prověří platnost souřadnic X ve všech grupách <see cref="AllGroupList"/> i v jejich vnořených Items,
        /// s ohledem na aktuální časovou osu a na režim grafu <see cref="TimeGraphProperties.TimeAxisMode"/> a na rozměr grafu (Width).
        /// </summary>
        protected void CheckValidCoordinateX()
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
            this._VisibleGroupList = new List<GTimeGraphGroup>();
            if (this._TimeConvertor == null) return;
            TimeGraphTimeAxisMode timeAxisMode = this.GraphParameters.TimeAxisMode;

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "ItemsRecalculateVisibleList", ""))
            {
                int[] counters = new int[3];
                foreach (List<GTimeGraphGroup> layerList in this.AllGroupList)
                {   // Jedna vizuální vrstva za druhou:
                    counters[0]++;
                    switch (timeAxisMode)
                    {
                        case TimeGraphTimeAxisMode.ProportionalScale:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateCoordinateXProportional(groupItem, counters);
                            break;
                        case TimeGraphTimeAxisMode.LogarithmicScale:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateCoordinateXLogarithmic(groupItem, counters);
                            break;
                        case TimeGraphTimeAxisMode.Standard:
                        default:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateCoordinateXStandard(groupItem, counters);
                            break;
                    }
                }

                this._ValidatedWidth = this.ClientSize.Width;
                this._ValidatedAxisMode = timeAxisMode;
                this._IsValidCoordinateX = true;

                scope.AddValues(counters, "Visual Layers Count: ", "Visual Groups Count: ", "Visual Items Count: ");
            }
            this.Invalidate(InvalidateItems.Bounds);
        }
        /// <summary>
        /// Vrací true, pokud data v seznamu <see cref="VisibleGroupList"/> jsou platná.
        /// Zohledňuje i stav <see cref="VisibleGroupList"/>, <see cref="ValidatedWidth"/>, <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TimeAxisMode"/>.
        /// Hodnotu lze nastavit, ale i když se vloží true, může se vracet false (pokud výše uvedené není platné).
        /// </summary>
        protected bool IsValidCoordinateX
        {
            get
            {
                if (this._VisibleGroupList == null) return false;
                if (!this._ValidatedWidth.HasValue || this.ClientSize.Width != this._ValidatedWidth.Value) return false;
                if (!this._ValidatedAxisMode.HasValue || this.GraphParameters.TimeAxisMode != this._ValidatedAxisMode.Value) return false;
                return this._IsValidCoordinateX;
            }
        } private bool _IsValidCoordinateX;
        /// <summary>
        /// Metoda připraví data pro jeden grafický prvek typu <see cref="GTimeGraphGroup"/> pro aktuální stav časové osy grafu, 
        /// v režimu <see cref="TimeGraphTimeAxisMode.Standard"/>
        /// </summary>
        /// <param name="groupItem">Jedna ucelená skupina grafických prvků <see cref="ITimeGraphItem"/></param>
        /// <param name="visibleItems">Výstupní seznam, do něhož se vkládají viditelné prvky</param>
        /// <param name="groupCount">Počet viditelných prvků group, pro statistiku</param>
        /// <param name="itemsCount">Počet zpracovaných prvků typu <see cref="ITimeGraphItem"/>, pro statistiku</param>
        protected void RecalculateCoordinateXStandard(GTimeGraphGroup groupItem, int[] counters)
        {
            if (!groupItem.IsValidRealTime) return;
            ITimeConvertor timeConvertor = this._TimeConvertor;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetPixelRange(t), ref counters[2]);

            if (timeConvertor.VisibleTime.HasIntersect(groupItem.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                counters[1]++;
                this._VisibleGroupList.Add(groupItem);
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
        protected void RecalculateCoordinateXProportional(GTimeGraphGroup groupItem, int[] counters)
        {
            if (!groupItem.IsValidRealTime) return;
            ITimeConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetProportionalPixelRange(t, size), ref counters[2]);

            if (timeConvertor.VisibleTime.HasIntersect(groupItem.Time))
            {   // Prvek je alespoň zčásti viditelný v časovém okně:
                counters[1]++;
                this._VisibleGroupList.Add(groupItem);
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
        protected void RecalculateCoordinateXLogarithmic(GTimeGraphGroup groupItem, int[] counters)
        {
            if (!groupItem.IsValidRealTime) return;
            ITimeConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            float proportionalRatio = this.GraphParameters.LogarithmicRatio;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetLogarithmicPixelRange(t, size, proportionalRatio), ref counters[2]);

            // Pozor: režim Logarithmic zajistí, že zobrazeny budou VŠECHNY prvky, takže prvky nefiltrujeme s ohledem na jejich čas : VisibleTime.HasIntersect() !
            counters[1]++;
            this._VisibleGroupList.Add(groupItem);
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
        /// Hodnota <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TimeAxisMode"/>, pro kterou jsou platné hodnoty ve <see cref="VisibleGroupList"/>.
        /// Po změně <see cref="GraphParameters"/>: <see cref="TimeGraphProperties.TimeAxisMode"/> dojde k přepočtu dat v tomto seznamu.
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
                this.CheckValidTimeAxis();
                this.CheckValidCoordinateYVirtual();
                this.CheckValidCoordinateYReal();
                this.CheckValidCoordinateX();
                this.CheckValidBounds();
                this.CheckValidChildList();
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
        }
        #endregion
        #region Podpora pro získávání dat - Caption, ToolTip
        internal string GetCaptionText(GInteractiveDrawArgs e, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position, Rectangle boundsAbsolute, Rectangle boundsVisibleAbsolute)
        {
            return data.Time.ToString();
        }
        internal void PrepareToolTip(GInteractiveChangeStateArgs e, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            e.ToolTipData.TitleText = "Tooltip " + position.ToString();
            string eol = Environment.NewLine;
            e.ToolTipData.InfoText = "ItemId: " + data.ItemId + eol +
                "Layer: " + data.Layer.ToString();
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
            this.DrawBackground(e, absoluteBounds);
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GTimeGraph", "Draw", ""))
            {
                e.GraphicsClipWith(absoluteBounds);
                this.DrawTicks(e, absoluteBounds);
                // Vykreslení jednotlivých položek grafu neřídí graf, ale systém. 
                // Bude postupně volat kreslení všech mých Child items, což jsou GTimeGraphGroup.GControl, bude volat jejich metodu Draw(GInteractiveDrawArgs).
                // Tato metoda (v třídě GTimeGraphControl) vyvolá kreslící metodu svého Ownera: ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute).
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
            switch (this.GraphParameters.TimeAxisMode)
            {
                case TimeGraphTimeAxisMode.LogarithmicScale:
                    this.DrawBackgroundLogarithmic(e, absoluteBounds);
                    break;
            }
        }
        /// <summary>
        /// Metoda umožní udělat něco s pozadím grafu, který má logaritmickou osu.
        /// Vykreslí se šedý přechod na logaritmických okrajích.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        protected virtual void DrawBackgroundLogarithmic(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            float shadow = this.GraphParameters.LogarithmicGraphDrawOuterShadow;
            if (shadow <= 0f) return;
            int alpha = (int)(255f * shadow);
            Color color1 = Color.FromArgb(0, 0, 0, 0);
            Color color2 = Color.FromArgb(alpha, 0, 0, 0);
            int width = (int)(((1f - this.GraphParameters.LogarithmicRatio) / 2f) * (float)absoluteBounds.Width);

            Rectangle leftBounds = new Rectangle(absoluteBounds.X, absoluteBounds.Y, width, absoluteBounds.Height);
            Rectangle leftBoundsG = leftBounds.Enlarge(1, 0, 0, 1);                      // To je úchylka WinFormů
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(leftBoundsG, color2, color1, 00f))
            {
                e.Graphics.FillRectangle(lgb, leftBounds);
            }

            Rectangle rightBounds = new Rectangle(absoluteBounds.Right - width, absoluteBounds.Y, width, absoluteBounds.Height);
            Rectangle rightBoundsG = rightBounds.Enlarge(1, 0, 0, 1);                    // To je úchylka WinFormů
            using (System.Drawing.Drawing2D.LinearGradientBrush rgb = new System.Drawing.Drawing2D.LinearGradientBrush(rightBoundsG, color2, color1, 180f))
            {
                e.Graphics.FillRectangle(rgb, rightBounds);
            }
        }
        /// <summary>
        /// Vykreslí všechny Ticky = časové značky, pokud se mají kreslit.
        /// </summary>
        protected void DrawTicks(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            AxisTickType tickLevel = this.GraphParameters.TimeAxisVisibleTickLevel;
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
        #endregion
        #region Invalidace je řešená jedním vstupním bodem
        /// <summary>
        /// Invaliduje dané prvky grafu, a automaticky přidá i prvky na nich závislé.
        /// Invalidace typu <see cref="InvalidateItems.Repaint"/> se nepřidává automaticky, tu musí volající specifikovat explicitně.
        /// </summary>
        /// <param name="items"></param>
        protected void Invalidate(InvalidateItems items)
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
                // O invalidaci Repaint si musí volající explicitně požádat:
                // items |= InvalidateItems.Repaint;
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
            /// <summary>
            /// Souřadnice na ose X
            /// </summary>
            CoordinateX = 1,
            /// <summary>
            /// Souřadnice na ose Y, ve virtuálních koordinátech.
            /// Invalidace virtuálních souřadnic má smysl tehdy, když se změní nějaké parametry, na jejichž základě pracuje kalkulátor Y,
            /// což je například <see cref="TimeGraphProperties.OneLineHeight"/>, nebo <see cref="TimeGraphProperties.TotalHeightRange"/>, atd.
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
            Repaint = Childs << 1
        }
        #endregion
        #region ITimeInteractiveGraph members
        ITimeConvertor ITimeInteractiveGraph.TimeConvertor { get { return this._TimeConvertor; } set { this._TimeConvertor = value; this.Invalidate(InvalidateItems.CoordinateX); } }
        int ITimeInteractiveGraph.UnitHeight { get { return this.GraphParameters.OneLineHeight.Value; } }
        IVisualParent ITimeInteractiveGraph.VisualParent { get { return this.VisualParent; } set { this.VisualParent = value; } }
        #endregion
    }
    #region class GTimeGraphGroup : skupina jednoho nebo více prvků ITimeGraphItem, obsahující sumární čas Time a Max(Height) z položek
    /// <summary>
    /// GTimeGraphGroup : GTimeGraphGroup : skupina jednoho nebo více prvků ITimeGraphItem, obsahující sumární čas Time a Max(Height) z položek
    /// </summary>
    public class GTimeGraphGroup : ITimeGraphItem
    {
        #region Konstruktory; řízená tvorba GTimeGraphControl pro GTimeGraphGroup i pro jednotlivé položky ITimeGraphItem
        /// <summary>
        /// Konstruktor
        /// </summary>
        private GTimeGraphGroup(GTimeGraph parent)
        {
            this._ParentGraph = parent;
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
            this._FirstItem = null;
            this._PrepareGControlGroup(parent);                           // Připravím GUI prvek pro sebe = pro grupu, jeho parentem je vlastní graf
        }
        /// <summary>
        /// Konstruktor s předáním jediné položky
        /// </summary>
        /// <param name="items"></param>
        public GTimeGraphGroup(GTimeGraph parent, ITimeGraphItem item)
            : this(parent)
        {
            this._PrepareGControlItem(item);                              // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
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
                this._PrepareGControlItem(item);                          // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height > height) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
            }
            this._Store(begin, end, height);
        }
        /// <summary>
        /// Metoda vytvoří grafický control třídy <see cref="GTimeGraphControl"/> (<see cref="ITimeGraphItem.GControl"/>) pro this grupu.
        /// </summary>
        /// <param name="parent">Parent prvku, GUI container, některý vyšší GUI prvek (typicky <see cref="GTimeGraph"/>).</param>
        private void _PrepareGControlGroup(IInteractiveParent parent)
        {
            if (this.GControl != null) return;
            this.GControl = new GTimeGraphControl(this, parent, this, GGraphControlPosition.Group);          // GUI prvek (GTimeGraphControl) dostává data (=this) a dostává vizuálního parenta (parent)
        }
        /// <summary>
        /// Metoda vytvoří grafický control třídy <see cref="GTimeGraphControl"/> (<see cref="ITimeGraphItem.GControl"/>) pro daný datový grafický prvek (item).
        /// </summary>
        /// <param name="item">Datový prvek grafu</param>
        private void _PrepareGControlItem(ITimeGraphItem item)
        {
            item.GControl = new GTimeGraphControl(item, this.GControl, this, GGraphControlPosition.Item);    // GUI prvek (GTimeGraphControl) dostává data (=item) a dostává vizuálního parenta (this.GControl)
            this.GControl.AddItem(item.GControl);                         // Náš hlavní GUI prvek (ten od grupy) si přidá další svůj Child prvek
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
                "; UseSpace: " + (this.CoordinateYLogical == null ? "none" : this.CoordinateYLogical.ToString());
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
        #region Souřadnice prvku na ose X i Y (logické, virtuální, reálné)
        /// <summary>
        /// Metoda připraví souřadnice this grupy na ose X, včetně jejích grafických items
        /// </summary>
        /// <param name="timeConvert"></param>
        /// <param name="coordinateYVirtual"></param>
        /// <param name="itemsCount"></param>
        public void PrepareCoordinateX(Func<TimeRange, Int32Range> timeConvert, ref int itemsCount)
        {
            Int32Range groupX = timeConvert(this.Time);
            this.GControl.CoordinateX = groupX;
            foreach (ITimeGraphItem item in this.Items)
            {
                itemsCount++;
                Int32Range absX = timeConvert(item.Time);                                // Vrací souřadnici X v koordinátech grafu
                int relBegin = absX.Begin - groupX.Begin;
                Int32Range relX = Int32Range.CreateFromBeginSize(relBegin, absX.Size);   // Získáme souřadnici X relativní k prvku Group, který je Parentem daného item
                item.GControl.CoordinateX = relX;
            }
            this.InvalidateBounds();
        }
        /// <summary>
        /// Metoda připraví reálné souřadnice Bounds do this grupy a jejích grafických items.
        /// Metoda může být volána opakovaně, sama si určí kdy je třeba něco měnit.
        /// </summary>
        public void PrepareBounds()
        {
            if (this.IsValidBounds) return;

            Int32Range groupY = this.CoordinateYVisual; // CoordinateYReal;
            this.GControl.Bounds = Int32Range.GetRectangle(this.GControl.CoordinateX, groupY);

            // Child prvky mají svoje souřadnice (Bounds) relativní k this prvku (který je jejich parentem), proto mají Y souřadnici { 0 až this.Y.Size }:
            Int32Range itemY = new Int32Range(0, groupY.Size);
            foreach (ITimeGraphItem item in this.Items)
                item.GControl.Bounds = Int32Range.GetRectangle(item.GControl.CoordinateX, itemY);

            this._IsValidBounds = true;
        }
        protected Int32Range CoordinateYVisual
        {
            get
            {
                Int32Range yReal = this.CoordinateYReal;
                int yDMax = (yReal.Size / 3);
                int yDiff = this._FirstItem.Layer;
                yDiff = ((yDiff < 0) ? 0 : (yDiff > yDMax ? yDMax : yDiff));
                if (yDiff != 0)
                    yReal = new Int32Range(yReal.Begin + 1 + yDiff, yReal.End - yDiff);
                return yReal;
            }
        }

        /// <summary>
        /// Invaliduje platnost souřadnic Bounds
        /// </summary>
        protected void InvalidateBounds()
        {
            this._IsValidBounds = false;
        }
        /// <summary>
        /// true pokud Bounds tohoto prvku i vnořených prvků jsou platné.
        /// </summary>
        protected bool IsValidBounds { get { return _IsValidBounds; } } private bool _IsValidBounds;
        /// <summary>
        /// Logická souřadnice tohoto prvku na ose Y. Souřadnice 0 odpovídá hodnotě 0 na ose Y, kladná čísla jsou fyzicky nahoru, záporná jsou povolená a jdou dolů.
        /// Jednotka je logická (nikoli pixely): prvek s výškou 1 je standardně vysoký.
        /// Vedle toho existují souřadné systémy Virtual (v pixelech, odspodu) a Real (v pixelech, od horního okraje).
        /// </summary>
        public Interval<float> CoordinateYLogical { get { return this._CoordinateYLogical; } set { this._CoordinateYLogical = value; this.InvalidateBounds(); } } private Interval<float> _CoordinateYLogical;
        /// <summary>
        /// Virtuální souřadnice na ose Y. Jednotkou jsou pixely.
        /// Hodnota 0 odpovídá pixelu úplně dole na grafu, tj. jako na matematické ose: Y jde odspodu nahoru.
        /// </summary>
        public Int32Range CoordinateYVirtual { get { return this._CoordinateYVirtual; } set { this._CoordinateYVirtual = value; this.InvalidateBounds(); } } private Int32Range _CoordinateYVirtual;
        /// <summary>
        /// Reálné souřadnice na ose Y. Jednotkou jsou pixely.
        /// Hodnota 0 odpovídá pixelu na souřadnici Bounds.Top, tj. jako ve Windows.Forms: Y jde odshora dolů.
        /// </summary>
        public Int32Range CoordinateYReal { get { return this._CoordinateYReal; } set { this._CoordinateYReal = value; this.InvalidateBounds(); } } private Int32Range _CoordinateYReal;
        #endregion
        #region Public prvky
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
        #endregion
        #region Childs, Interaktivita, Draw()
        /// <summary>
        /// Metoda zajistí přípravu ToolTipu pro daný prvek (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        internal void PrepareToolTip(GInteractiveChangeStateArgs e, ITimeGraphItem data, GGraphControlPosition position)
        {
            this._ParentGraph.PrepareToolTip(e, this, data, position);
        }
        /// <summary>
        /// Vykreslí tuto grupu. Kreslí pouze pokud obsahuje více než 1 prvek, a pokud vrstva <see cref="ITimeGraphItem.Layer"/> je nula nebo kladná (pro záporné vrstvy se nekreslí).
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        public void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (!this.IsValidRealTime || this._FirstItem.Layer < 0 || this.ItemCount <= 1) return;

            Color? backColor = this.LinkBackColor;
            if (!backColor.HasValue)
                // Nemáme explicitně danou barvu linky => odvodíme ji z barvy pozadí prvku + morphing:
                backColor = (this.BackColor.HasValue ? this.BackColor.Value : Skin.Graph.ElementBackColor).Morph(Skin.Graph.ElementLinkBackColor);
            backColor = Color.FromArgb(128, backColor.Value);
            Color? borderColor = backColor;

            Rectangle boundsLink = boundsAbsolute.Enlarge(-1, -2, -1, -2);
            GPainter.DrawEffect3D(e.Graphics, boundsLink, backColor.Value, System.Windows.Forms.Orientation.Horizontal, this.GControl.InteractiveState, force3D: false);
        }
        /// <summary>
        /// Metoda volaná pro vykreslování obsahu "Přes Child prvky"
        /// </summary>
        /// <param name="e"></param>
        public void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            Rectangle boundsVisibleAbsolute = Rectangle.Intersect(e.AbsoluteVisibleClip, boundsAbsolute);
            string text = this._ParentGraph.GetCaptionText(e, this, this, GGraphControlPosition.Group, boundsAbsolute, boundsVisibleAbsolute);
            Color foreColor = this.GControl.BackColor.Contrast();
            GPainter.DrawString(e.Graphics, boundsAbsolute, text, foreColor, FontInfo.CaptionBold, ContentAlignment.MiddleCenter);
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
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
    #endregion
    #region class GTimeGraphControl : vizuální a interaktivní control, který se vkládá do implementace ITimeGraphItem
    /// <summary>
    /// GTimeGraphControl : vizuální a interaktivní control, který se vkládá do implementace ITimeGraphItem.
    /// Tento prvek je zobrazován ve dvou režimech: buď jako přímý child prvek vizuálního grafu, pak reprezentuje grupu prvků (i kdyby grupa měla jen jeden prvek),
    /// anebo jako child prvek této grupy, pak reprezentuje jeden konkrétní prvek grafu (GraphItem).
    /// </summary>
    public class GTimeGraphControl : InteractiveDragObject, IOwnerProperty<ITimeGraphItem>
    {
        #region Konstruktor, privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="data">Prvek grafu, ten v sobě obsahuje data</param>
        /// <param name="parent">Parent prvku, GUI container</param>
        public GTimeGraphControl(ITimeGraphItem data, IInteractiveParent parent, GTimeGraphGroup group, GGraphControlPosition position)
            : base()
        {
            this._Owner = data;
            this._Parent = parent;
            this._Group = group;
            this._Position = position;
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
        /// Grupa, která slouží jako vazba na globální data
        /// </summary>
        private GTimeGraphGroup _Group;
        /// <summary>
        /// Pozice tohoto prvku
        /// </summary>
        private GGraphControlPosition _Position;
        #endregion
        #region Veřejná data
        /// <summary>
        /// Souřadnice na ose X. Jednotkou jsou pixely.
        /// Tato osa je společná jak pro virtuální, tak pro reálné souřadnice.
        /// Hodnota 0 odpovídá prvnímu viditelnému pixelu vlevo.
        /// </summary>
        public Int32Range CoordinateX { get; set; }
        /// <summary>
        /// Barva pozadí tohoto prvku
        /// </summary>
        public override Color BackColor
        {
            get
            {
                if (this.BackColorUser.HasValue) return this.BackColorUser.Value;
                if (this._Owner != null && this._Owner.BackColor.HasValue) return this._Owner.BackColor.Value;
                return Skin.Graph.ElementBackColor;
            }
            set { }
        }
        public Color BorderColor
        {
            get
            {
                if (this.BorderColorUser.HasValue) return this.BorderColorUser.Value;
                if (this._Owner != null && this._Owner.BorderColor.HasValue) return this._Owner.BorderColor.Value;
                return Skin.Graph.ElementBorderColor;
            }
        }
        public Color? BorderColorUser { get; set; }
        #endregion
        #region Child prvky: přidávání, kolekce
        /// <summary>
        /// Child prvky, může být null (pro <see cref="GControl"/> v roli controlu jednotlivého <see cref="ITimeGraphItem"/>), 
        /// nebo může obsahovat vnořené prvky (pro <see cref="GControl"/> v roli controlu skupiny <see cref="GTimeGraphGroup"/>).
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._Childs; } }
        /// <summary>
        /// Přidá vnořený objekt
        /// </summary>
        /// <param name="child"></param>
        public void AddItem(IInteractiveItem child)
        {
            if (this._Childs == null)
                this._Childs = new List<Components.IInteractiveItem>();
            this._Childs.Add(child);
        }
        private List<IInteractiveItem> _Childs;
        #endregion
        #region Interaktivita
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            this._Group.PrepareToolTip(e, this._Owner, this._Position);
        }
        #endregion
        #region Kreslení prvku
        /// <summary>
        /// Vykreslí this prvek
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag & Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this._Owner.Draw(e, absoluteBounds, drawMode);
        }
        /// <summary>
        /// Vykreslování "Přes Child prvky": pokud this prvek vykresluje Grupu, pak ano!
        /// </summary>
        protected override bool NeedDrawOverChilds { get { return (this._Position == GGraphControlPosition.Group); } set { } }
        /// <summary>
        /// Metoda volaná pro vykreslování "Přes Child prvky": převolá se grupa.
        /// </summary>
        /// <param name="e"></param>
        protected override void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (this._Position == GGraphControlPosition.Group)
                this._Group.DrawOverChilds(e, boundsAbsolute, drawMode);
        }
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        public void DrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            this.DrawItem(e, boundsAbsolute, drawMode, null);
        }
        /// <summary>
        /// Metoda je volaná pro vykreslení prvku.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/>.<see cref="GTimeGraphControl.Draw(TimeGraphItemDrawArgs)"/> Draw
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        /// <param name="backColor">Explicitně definovaná barva pozadí</param>
        /// <param name="borderColor">Explicitně definovaná barva okraje</param>
        /// <param name="enlargeBounds">Změna rozměru Bounds ve všech směrech</param>
        public void DrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode, int? enlargeBounds)
        {
            if (boundsAbsolute.Height <= 0 || boundsAbsolute.Width < 0) return;
            if (enlargeBounds.HasValue)
                boundsAbsolute = boundsAbsolute.Enlarge(enlargeBounds.Value);
            if (boundsAbsolute.Width < 1)
                boundsAbsolute.Width = 1;

            Color backColor = this.BackColor;
            Color borderColor = this.BorderColor;
            if (boundsAbsolute.Width <= 2)
            {
                e.Graphics.FillRectangle(Skin.Brush(borderColor), boundsAbsolute);
            }
            else
            {
                System.Drawing.Drawing2D.HatchStyle? backStyle = this._Owner.BackStyle;
                if (backStyle.HasValue)
                {
                    using (System.Drawing.Drawing2D.HatchBrush hb = new System.Drawing.Drawing2D.HatchBrush(backStyle.Value, backColor, Color.Transparent))
                    {
                        e.Graphics.FillRectangle(hb, boundsAbsolute);
                    }
                }
                else
                {
                    e.Graphics.FillRectangle(Skin.Brush(backColor), boundsAbsolute);
                }

                e.Graphics.DrawRectangle(Skin.Pen(borderColor), boundsAbsolute);
            }
        }
        /// <summary>
        /// Volba, zda metoda <see cref="Repaint()"/> způsobí i vyvolání metody <see cref="Parent"/>.<see cref="IInteractiveParent.Repaint"/>.
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        #endregion
    }
    /// <summary>
    /// Pozice GUI controlu pro prvek grafu
    /// </summary>
    public enum GGraphControlPosition
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Control pro grupu
        /// </summary>
        Group,
        /// <summary>
        /// Control pro konkrétní instanci <see cref="ITimeGraphItem"/>
        /// </summary>
        Item
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
        /// Horní okraj = prostor nad nejvyšším prvkem grafu, který by měl být zobrazen jako prázdný, tak aby bylo vidět že nic dalšího už není.
        /// V tomto prostoru (těsně pod souřadnicí Top) se provádí Drag & Drop prvků.
        /// Hodnota je zadána v logických jednotkách, tedy v počtu standardních linek.
        /// Výchozí hodnota = 1.0 linka, nelze zadat zápornou hodnotu.
        /// </summary>
        public float UpperSpaceLogical { get { return this._UpperSpaceLogical; } set { this._UpperSpaceLogical = (value < 0f ? 0f : value); } }
        private float _UpperSpaceLogical = 1f;
        /// <summary>
        /// Dolní okraj = mezera pod dolním okrajem nejnižšího prvku grafu k dolnímu okraji controlu, v pixelech.
        /// Výchozí hodnota = 1 pixel, nelze zadat zápornou hodnotu.
        /// </summary>
        public int BottomMarginPixel { get { return this._BottomMarginPixel; } set { this._BottomMarginPixel = (value < 0 ? 0 : value); } } private int _BottomMarginPixel = 1;
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
    public interface ITimeInteractiveGraph : IInteractiveItem
    {
        /// <summary>
        /// Reference na objekt, který provádí časové konverze pro tento graf.
        /// Instanci do této property plní ten, kdo ji zná.
        /// </summary>
        ITimeConvertor TimeConvertor { get; set; }
        /// <summary>
        /// Reference na objekt, který dovoluje grafu ovlivnit velikost svého parenta.
        /// Po přepočtu výšky grafu může graf chtít nastavit výšku (i šířku?) svého hostitele tak, aby bylo zobrazeno vše, co je třeba.
        /// </summary>
        IVisualParent VisualParent { get; set; }
        /// <summary>
        /// Height (in pixels) for one unit of GTimeItem.Height
        /// </summary>
        int UnitHeight { get; }
    }
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
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/> : <see cref="GTimeGraphControl.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, DrawItemMode drawMode);
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
