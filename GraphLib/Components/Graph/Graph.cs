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
            this.IsSelectParent = true;
        }
        /// <summary>
        /// Všechny prvky grafu (časové úseky)
        /// </summary>
        public EList<ITimeGraphItem> ItemList { get { return this._ItemList; } } private EList<ITimeGraphItem> _ItemList;
        /// <summary>
        /// ID tohoto grafu. Hodnotu nastavuje aplikační kód dle své potřeby, hodnota je vkládána do identifikátorů odesílaných do handlerů událostí v grafu.
        /// Graf sám o sobě tuto hodnotu nepotřebuje.
        /// </summary>
        public int GraphId { get; set; }
        /// <summary>
        /// Eventhandler události: do <see cref="ItemList"/> byla přidána položka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemList_ItemAddAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { args.Item.OwnerGraph = this; this.Invalidate(InvalidateItems.AllGroups); }
        /// <summary>
        /// Eventhandler události: z <see cref="ItemList"/> byla odebrána položka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ItemList_ItemRemoveAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { args.Item.OwnerGraph = null; this.Invalidate(InvalidateItems.AllGroups); }
        /// <summary>
        /// Zdroj dat, nepovinný
        /// </summary>
        private ITimeGraphDataSource _DataSource;
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
                int offsetX = this._TimeConvertor.FirstPixel;
                int[] counters = new int[3];
                foreach (List<GTimeGraphGroup> layerList in this.AllGroupList)
                {   // Jedna vizuální vrstva za druhou:
                    counters[0]++;
                    switch (timeAxisMode)
                    {
                        case TimeGraphTimeAxisMode.ProportionalScale:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateCoordinateXProportional(groupItem, offsetX, counters);
                            break;
                        case TimeGraphTimeAxisMode.LogarithmicScale:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateCoordinateXLogarithmic(groupItem, offsetX, counters);
                            break;
                        case TimeGraphTimeAxisMode.Standard:
                        default:
                            foreach (GTimeGraphGroup groupItem in layerList)
                                this.RecalculateCoordinateXStandard(groupItem, offsetX, counters);
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
        protected void RecalculateCoordinateXStandard(GTimeGraphGroup groupItem, int offsetX, int[] counters)
        {
            if (!groupItem.IsValidRealTime) return;
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetProportionalPixelRange(t, size), offsetX, ref counters[2]);

            if (timeConvertor.Value.HasIntersect(groupItem.Time))
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
        protected void RecalculateCoordinateXProportional(GTimeGraphGroup groupItem, int offsetX, int[] counters)
        {
            if (!groupItem.IsValidRealTime) return;
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetProportionalPixelRange(t, size), offsetX, ref counters[2]);

            if (timeConvertor.Value.HasIntersect(groupItem.Time))
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
        protected void RecalculateCoordinateXLogarithmic(GTimeGraphGroup groupItem, int offsetX, int[] counters)
        {
            if (!groupItem.IsValidRealTime) return;
            ITimeAxisConvertor timeConvertor = this._TimeConvertor;
            int size = this.Bounds.Width;
            float proportionalRatio = this.GraphParameters.LogarithmicRatio;
            groupItem.PrepareCoordinateX(t => timeConvertor.GetLogarithmicPixelRange(t, size, proportionalRatio), offsetX, ref counters[2]);

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
        #region Komunikace s datovým zdrojem: Caption, ToolTip, DoubleClick, LongClick, Drag and Drop
        /// <summary>
        /// Metoda získá text, který se bude vykreslovat do prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="group"></param>
        /// <param name="dataItem"></param>
        /// <param name="position"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="boundsVisibleAbsolute"></param>
        /// <returns></returns>
        internal string GraphItemGetCaptionText(GInteractiveDrawArgs e, FontInfo fontInfo, GTimeGraphGroup group, ITimeGraphItem dataItem, GGraphControlPosition position, Rectangle boundsAbsolute, Rectangle boundsVisibleAbsolute)
        {
            string text = null;
            if (this.HasDataSource)
            {
                CreateTextArgs args = new CreateTextArgs(this, e, fontInfo, group, dataItem, position, boundsAbsolute, boundsVisibleAbsolute);
                this.DataSource.CreateText(args);
                text = args.Text;
            }
            else
            {
                text = dataItem.Time.Text;
            }
            return text;
        }
        /// <summary>
        /// Metoda připraví tooltip pro daný prvek
        /// </summary>
        /// <param name="e"></param>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        internal void GraphItemPrepareToolTip(GInteractiveChangeStateArgs e, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            if (data == null) return;
            bool isNone = data.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowToolTipNone);
            if (isNone) return;

            bool isFadeIn = data.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowToolTipFadeIn);
            bool isImmediatelly = data.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowToolTipImmediatelly);
            if (!isFadeIn && !isImmediatelly) return;

            if (this.HasDataSource)
            {
                CreateToolTipArgs args = new CreateToolTipArgs(e, this, group, data, position);
                this.DataSource.CreateToolTip(args);
            }
            else
            {
                e.ToolTipData.TitleText = "Tooltip " + position.ToString();
                string eol = Environment.NewLine;
                e.ToolTipData.InfoText = "ItemId: " + data.ItemId + eol +
                    "Layer: " + data.Layer.ToString();
            }

            string text = (e.HasToolTipData ? e.ToolTipData.InfoText : null);
            if (text != null)
            {
                if (isImmediatelly)
                {
                    e.ToolTipData.AnimationType = TooltipAnimationType.Instant;
                }
                else if (isFadeIn)
                {
                    e.ToolTipData.AnimationFadeInTime = TimeSpan.FromMilliseconds(100);
                    e.ToolTipData.AnimationShowTime = TimeSpan.FromMilliseconds(100 * text.Length);     // 1 sekunda na přečtení 10 znaků
                    e.ToolTipData.AnimationFadeOutTime = TimeSpan.FromMilliseconds(10 * text.Length);
                }
            }
        }
        /// <summary>
        /// Metoda zajistí zpracování události RightClick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        internal void GraphItemRightClick(GInteractiveChangeStateArgs e, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            if (!this.HasDataSource) return;

            ItemActionArgs args = new ItemActionArgs(e, this, group, data, position);
            this.DataSource.ItemRightClick(args);
            if (args.ContextMenu != null && args.ContextMenu.Items.Count > 0)
                this.GraphItemShowContextMenu(e, args.ContextMenu);
        }
        /// <summary>
        /// Rozsvítí dané kontextové menu v přiměřené pozici
        /// </summary>
        /// <param name="e"></param>
        /// <param name="contextMenu"></param>
        protected void GraphItemShowContextMenu(GInteractiveChangeStateArgs e, System.Windows.Forms.ToolStripDropDownMenu contextMenu)
        {
            var host = this.Host;
            if (host == null) return;

            Point point = this.GetPointForMenu(e);
            contextMenu.Show(host, point, System.Windows.Forms.ToolStripDropDownDirection.BelowRight);
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
        /// <param name="e"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        internal void GraphItemLeftDoubleClick(GInteractiveChangeStateArgs e, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            if (!this.HasDataSource) return;
            ItemActionArgs args = new ItemActionArgs(e, this, group, data, position);
            this.DataSource.ItemDoubleClick(args);
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftLongCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        internal void GraphItemLeftLongClick(GInteractiveChangeStateArgs e, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            if (!this.HasDataSource) return;
            ItemActionArgs args = new ItemActionArgs(e, this, group, data, position);
            this.DataSource.ItemLongClick(args);
        }
        internal void DragDropGroupCallSource(ItemDragDropArgs args)
        {
            if (!this.HasDataSource) return;
            this.DataSource.ItemDragDropAction(args);
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
        #endregion
        #region Podpora pro Selectování DragFrame
        protected override void AfterStateChangedDragFrameBegin(GInteractiveChangeStateArgs e)
        {
            Rectangle dragFrameWorkArea = this.BoundsAbsolute;
            this.DragFrameWorkAreaModifyByTable(e, ref dragFrameWorkArea);
            e.DragFrameWorkArea = dragFrameWorkArea;
        }
        protected void DragFrameWorkAreaModifyByTable(GInteractiveChangeStateArgs e, ref Rectangle dragFrameWorkArea)
        {
            Grid.GTable table = this.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
            if (table == null) return;
            Rectangle tableRowArea = table.GetAbsoluteBoundsForArea(Grid.TableAreaType.RowData);
            dragFrameWorkArea = new Rectangle(dragFrameWorkArea.X, tableRowArea.Y, dragFrameWorkArea.Width, tableRowArea.Height);
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
        /// Průhlednost prvků grafu při běžném vykreslování.
        /// Má hodnotu null (neaplikuje se), nebo 0 ÷ 255. 
        /// Hodnota 255 má stejný význam jako null = plně viditelný graf. 
        /// Hodnota 0 = zcela neviditelné prvky (ale fyzicky jsou přítomné).
        /// Výchozí hodnota = null.
        public int? GraphOpacity
        {
            get { return this.GraphParameters.Opacity; }
        }
        #endregion
        #region Invalidace je řešená jedním vstupním bodem
        /// <summary>
        /// Zajistí vykreslení this prvku <see cref="Repaint()"/>, včetně překreslení Host controlu <see cref="GInteractiveControl"/>.
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
        #region Obecná static podpora pro grafy
        /// <summary>
        /// Vrací true, pokud se v daném režimu chování a za daného stavu má zobrazovat Caption
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        internal static bool IsCaptionVisible(GraphItemBehaviorMode mode, GInteractiveState state, bool isSelected)
        {
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionNone)) return false;
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionAllways)) return true;
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionInSelected) && isSelected) return true;
            if (mode.HasFlag(GraphItemBehaviorMode.ShowCaptionInMouseOver))
                return ((state & (GInteractiveState.FlagOver | GInteractiveState.FlagDown | GInteractiveState.FlagDrag | GInteractiveState.FlagFrame)) != 0);
            return false;
        }
        #endregion
        #region ITimeInteractiveGraph members
        ITimeAxisConvertor ITimeInteractiveGraph.TimeAxisConvertor { get { return this._TimeConvertor; } set { this._TimeConvertor = value; this.Invalidate(InvalidateItems.CoordinateX); } }
        int ITimeInteractiveGraph.UnitHeight { get { return this.GraphParameters.OneLineHeight.Value; } }
        IVisualParent ITimeInteractiveGraph.VisualParent { get { return this.VisualParent; } set { this.VisualParent = value; } }
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
            this._TimeAxisVisibleTickLevel = AxisTickType.BigTick;
            this._OneLineHeight = Skin.Graph.LineHeight;
            this._LogarithmicRatio = 0.60f;
            this._LogarithmicGraphDrawOuterShadow = 0.20f;
            this._Opacity = null;
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
        /// Výchozí zobrazovaná hodnota
        /// </summary>
        public TimeRange InitialValue { get { return this._InitialValue; } set { this._InitialValue = value; } } private TimeRange _InitialValue;
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
        /// V tomto prostoru (těsně pod souřadnicí Top) se provádí Drag and Drop prvků.
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
        /// Height (in pixels) for one unit of GTimeItem.Height
        /// </summary>
        int UnitHeight { get; }
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
    public interface ITimeGraphItem
    {
        /// <summary>
        /// Graf, v němž je prvek umístěn. Hodnotu vkládá sám graf v okamžiku vložení prvku / odebrání prvku z kolekce.
        /// </summary>
        ITimeInteractiveGraph OwnerGraph { get; set; }
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
        /// <see cref="Height"/> * <see cref="GTimeGraph.GraphParameters"/>: <see cref="TimeGraphProperties.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        float Height { get; }
        /// <summary>
        /// Barva pozadí prvku.
        /// Pokud bude null, pak prvek nebude mít vyplněný svůj prostor (obdélník). Může mít vykreslené okraje (barva <see cref="LineColor"/>).
        /// Anebo může mít kreslené Ratio (viz property <see cref="RatioBegin"/>, <see cref="RatioEnd"/>, 
        /// <see cref="RatioBackColor"/>, <see cref="RatioLineColor"/>, <see cref="RatioLineWidth"/>).
        /// </summary>
        Color? BackColor { get; }
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
        /// Barva pozadí prvku, kreslená v části Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Z databáze se načítá ze sloupce: "ratio_back_color", je NEPOVINNÝ.
        /// </summary>
        Color? RatioBackColor { get; }
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
        /// Režim chování položky grafu (editovatelnost, texty, atd).
        /// </summary>
        GraphItemBehaviorMode BehaviorMode { get; }
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

        Default = 0,
        Rectangle
    }
    #endregion
    #region Interface ITimeGraphDataSource a příslušné třídy argumentů
    /// <summary>
    /// Deklarace zdroje dat pro graf
    /// </summary>
    public interface ITimeGraphDataSource
    {
        void CreateText(CreateTextArgs args);
        void CreateToolTip(CreateToolTipArgs args);
        void GraphRightClick(ItemActionArgs args);
        void ItemRightClick(ItemActionArgs args);
        void ItemDoubleClick(ItemActionArgs args);
        void ItemLongClick(ItemActionArgs args);
        void ItemChange(ItemChangeArgs args);
        void ItemDragDropAction(ItemDragDropArgs args);
    }
    #region class CreateTextArgs : 
    /// <summary>
    /// Argumenty pro tvobu textu (Caption)
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
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public CreateToolTipArgs(GInteractiveChangeStateArgs e, GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
            : base(e, graph, group, data, position)
        { }
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { return this.InteractiveArgs.ToolTipData; } set { this.InteractiveArgs.ToolTipData = value; } }
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
            this._AbsOrigin = (dragArgs.BoundsInfo != null ? (Point?)dragArgs.BoundsInfo.AbsOrigin : (Point?)null);
            this.DragToAbsoluteBounds = targetAbsoluteBounds;             // Musí se provést až po vložení hodnoty do this._AbsOrigin.
            this.TargetIsValid = true;
        }
        /// <summary>
        /// Vstupní argument akce Drag and Drop
        /// </summary>
        private GDragActionArgs _DragArgs;
        /// <summary>
        /// Aktuální souřadnice cílová v průběhu akce Drag and Drop, relativní koordináty.
        /// Jedná se o souřadnice odpovídající pohybu myši; prvek sám může svoje cílové souřadnice modifikovat s ohledem na svoje vlastní pravidla.
        /// </summary>
        private Rectangle? _DragToRelativeBounds;
        /// <summary>
        /// Absolutní souřadnice počátku relativního prostoru = offset mezi <see cref="DragToRelativeBounds"/> a <see cref="DragToAbsoluteBounds"/>.
        /// </summary>
        private Point? _AbsOrigin;
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
        /// Typ aktuální akce
        /// </summary>
        public DragActionType DragAction { get { return this._DragArgs.DragAction; } }
        /// <summary>
        /// Absolutní souřadnice myši, kde se nachází nyní.
        /// Může být null pouze při akci <see cref="DragAction"/> == <see cref="DragActionType.DragThisCancel"/>.
        /// </summary>
        public Point? MouseCurrentAbsolutePoint { get { return this._DragArgs.MouseCurrentAbsolutePoint; } }
        /// <summary>
        /// Souřadnice prvku cílová v průběhu akce Drag and Drop, relativní koordináty.
        /// Souřadnice je ve výchozím stavu nastavena systémem - podle pohybu myši, ale aplikační kód ji může změnit na hodnotu, kam se má prvek skutečně přemístit.
        /// Aplikační kód může stejně tak pracovat i s absolutní souřadnicí <see cref="DragToAbsoluteBounds"/>.
        /// Pro akci <see cref="DragAction"/> == <see cref="DragActionType.DragThisCancel"/> je null, jinak obsahuje hodnotu.
        /// Výchozí hodnota je k dispozici v <see cref="DragArgs"/>, tam ji měnit nelze.
        /// </summary>
        public Rectangle? DragToRelativeBounds { get { return this._DragToRelativeBounds; } set { this._DragToRelativeBounds = value; } }
        /// <summary>
        /// Souřadnice prvku cílová v průběhu akce Drag and Drop, absolutní koordináty. 
        /// Souřadnice je ve výchozím stavu nastavena systémem - podle pohybu myši, ale aplikační kód ji může změnit na hodnotu, kam se má prvek skutečně přemístit.
        /// Aplikační kód může stejně tak pracovat i s relativní souřadnicí <see cref="DragToRelativeBounds"/>.
        /// Pro akci <see cref="DragAction"/> == <see cref="DragActionType.DragThisCancel"/> je null, jinak obsahuje hodnotu.
        /// Výchozí hodnota je k dispozici v <see cref="DragArgs"/>, tam ji měnit nelze.
        /// </summary>
        public Rectangle? DragToAbsoluteBounds { get { return this._DragToRelativeBounds.Add(this._AbsOrigin); } set { this._DragToRelativeBounds = value.Sub(this._AbsOrigin); } }
        #endregion
        #region Properties WriteInit
        /// <summary>
        /// Obsahuje false po iniciaci. V tomto stavu lze vkládat hodnoty do všech "WriteInit" properties:
        /// <see cref="ParentGraph"/>, <see cref="ParentTable"/>, <see cref="TargetItem"/>, <see cref="TargetGraph"/>, <see cref="TargetTable"/> (označeny "WriteInit").
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
        /// "WriteInit" property.
        /// </summary>
        public IInteractiveItem TargetItem { get { return this._TargetItem; } set { this._CheckSet(); this._TargetItem = value; } } private IInteractiveItem _TargetItem;
        /// <summary>
        /// Cílový graf, nad nímž se nyní nachází ukazatel myši = do tohoto grafu "by se aktuální prvek grafu přemístil".
        /// "WriteInit" property.
        /// </summary>
        public GTimeGraph TargetGraph { get { return this._TargetGraph; } set { this._CheckSet(); this._TargetGraph = value; } } private GTimeGraph _TargetGraph;
        /// <summary>
        /// Cílová tabulka, nad níž se nyní nachází ukazatel myši = do této tabulky "by se aktuální prvek grafu přemístil".
        /// "WriteInit" property.
        /// </summary>
        public Grid.GTable TargetTable { get { return this._TargetTable; } set { this._CheckSet(); this._TargetTable = value; } } private Grid.GTable _TargetTable;
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
                    homeBounds = boundsInfo.CurrentAbsBounds;
                }

                return homeBounds;
            }
        }
        #endregion
        #region Výstupní proměnné
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
    [Flags]
    public enum ItemDragTargetType
    {
        None = 0,
        OnItem = 0x0001,
        OnSameGraph = 0x0010,
        OnOtherGraph = 0x0020,
        OnGraph = OnSameGraph | OnOtherGraph,
        OnSameTable = 0x0100,
        OnOtherTable = 0x0200,
        OnTable = OnSameTable | OnOtherTable
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
        public ItemInteractiveArgs(GInteractiveChangeStateArgs e, GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
            : base(graph, group, data, position)
        {
            this.InteractiveArgs = e;
        }
        /// <summary>
        /// Interaktivní argument
        /// </summary>
        protected GInteractiveChangeStateArgs InteractiveArgs { get; private set; }
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
    #region class ItemArgs : Bázová třída pro všechny argumenty, které jsou postaveny nad grupou prvků grafu a nad jedním prvek z této grupy
    /// <summary>
    /// ItemArgs : Bázová třída pro všechny argumenty, které jsou postaveny nad grupou prvků grafu a nad jedním prvek z této grupy
    /// </summary>
    public abstract class ItemArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="group"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public ItemArgs(GTimeGraph graph, GTimeGraphGroup group, ITimeGraphItem data, GGraphControlPosition position)
        {
            this.Graph = graph;
            this.Group = group;
            this.CurrentItem = (position == GGraphControlPosition.Item ? data : null);
            this.Position = position;
        }
        /// <summary>
        /// Graf, v němž došlo k události
        /// </summary>
        public GTimeGraph Graph { get; protected set; }
        /// <summary>
        /// Grupa položek
        /// </summary>
        public GTimeGraphGroup Group { get; protected set; }
        /// <summary>
        /// Přímo ten prvek, jehož se týká akce (na který bylo kliknuto).
        /// Může být null, pokud se akce týká skupiny prvků = bylo kliknuto na "spojovací linii mezi prvky".
        /// Pak je třeba vyhodnotit prvky v <see cref="GroupedItems"/>.
        /// </summary>
        public ITimeGraphItem CurrentItem { get; protected set; }
        /// <summary>
        /// Skupina prvků, jejíhož člena se akce týká, nebo jejíž spojovací linie se akce týká.
        /// Nikdy není null, vždy obsahuje alespoň jeden prvek.
        /// </summary>
        public ITimeGraphItem[] GroupedItems { get { return this.Group.Items; } }
        /// <summary>
        /// Typ prvku, kterého se akce týká (Item / Group).
        /// </summary>
        public GGraphControlPosition Position { get; protected set; }
    }
    #endregion
    #endregion
}
