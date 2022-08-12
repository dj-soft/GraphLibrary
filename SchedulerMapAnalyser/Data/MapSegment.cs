using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.SchedulerMap.Analyser
{
    /// <summary>
    /// Segment plánu, obsahuje pole prvků a řídí tvorbu linků mezi prvky a přístup k datům.
    /// Segment neřídí analýzu.
    /// </summary>
    public class MapSegment
    {
        #region Konstruktor, proměnné, základní stavové property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MapSegment(string fileName, bool onlyProcessed)
        {
            this.MapItems = new Dictionary<int, MapItem>();
            this.MapLinks = new Dictionary<long, MapLink>();
            this.StringHeap = new StringHeap();

            this.FileName = fileName;
            this.OnlyProcessedItems = onlyProcessed;
            this._DataState = LoadingState.None;
        }
        /// <summary>
        /// Vrátí true pokud this instance je platná pro daný soubor a volbu.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="onlyProcessed"></param>
        /// <returns></returns>
        public bool IsValidFor(string fileName, bool onlyProcessed)
        {
            return (String.Equals(this.FileName, fileName, StringComparison.InvariantCultureIgnoreCase) && (this.OnlyProcessedItems == onlyProcessed));
        }
        /// <summary>
        /// Jméno souboru
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// Zpracovávat jen prvky, které prošly procesem (dle mapy)
        /// </summary>
        public bool OnlyProcessedItems { get; private set; }
        /// <summary>
        /// Požadavek na storno procesu, lze nastavit kdykoliv, ve vhodném okamžiku bude akceptováno.
        /// </summary>
        public bool Cancel { get; set; }
        #endregion
        #region Načtení dat, simulace zacyklení
        /// <summary>
        /// true po načtení dat
        /// </summary>
        public bool IsLoaded { get { return (DataState == LoadingState.Loaded); } }
        /// <summary>
        /// true v době načítání dat
        /// </summary>
        public bool IsLoading { get { return (DataState == LoadingState.Loading); } }
        /// <summary>
        /// Stav dat
        /// </summary>
        public LoadingState DataState 
        {
            get { return _DataState; }
            private set
            {
                var oldState = _DataState;
                var newState = value;
                if (newState != oldState)
                {
                    _DataState = newState;
                    DataStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private LoadingState _DataState;
        /// <summary>
        /// Event volaný při změně <see cref="DataState"/>
        /// </summary>
        public event EventHandler DataStateChanged;
        /// <summary>
        /// Stavy dat v objektu
        /// </summary>
        public enum LoadingState { None, Loading, Cancelled, Loaded }
        /// <summary>
        /// Načte data, postup odesílá do <paramref name="progressAction"/>.
        /// </summary>
        /// <param name="progressAction"></param>
        public void LoadData(Action<ProgressArgs> progressAction = null)
        {
            string fileName = this.FileName;
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("Není zadané jméno souboru.");
            var fileInfo = new System.IO.FileInfo(fileName);
            if (!fileInfo.Exists) throw new ArgumentNullException($"Soubor jména '{fileName}' neexistuje.");

            if (Cancel) return;

            this.Clear();

            this.DataState = LoadingState.Loading;
            DoProgressAction(progressAction, ProgressState.Starting, $"Načítám data ze souboru '{fileName}'...", 0m);

            int i = 0;
            int rowId = 0;
            decimal fileLength = fileInfo.Length;
            decimal filePosition = 0m;
            foreach (var line in System.IO.File.ReadLines(fileName))
            {
                if (Cancel) break;
                filePosition += (decimal)(line.Length + 2);
                this.AddLine(ref rowId, line);

                // Jednou za 100 řádků pošlu progres typu Loading:
                if (((i++) % 100) == 0) DoProgressAction(progressAction, ProgressState.Loading, $"Načteno {Format(ItemsCount)} položek...", (filePosition / fileLength));
            }
            // Na konci pošlu výsledný progres typu Loading:
            DoProgressAction(progressAction, ProgressState.Loading, $"Načteno {Format(ItemsCount)} položek...", 1m);

            this.DataState = (!Cancel ? LoadingState.Loaded : LoadingState.Cancelled);
            DoProgressAction(progressAction, (Cancel ? ProgressState.Cancelled : ProgressState.Done), $"Zpracováno {Format(rowId)} řádků souboru, získáno {Format(ItemsCount)} položek mapy.", 1m);
        }
        /// <summary>
        /// Počet simulovaných zacyklení v datech
        /// </summary>
        public int SimulatedCycleCount { get; private set; }
        /// <summary>
        /// Do dat přidej simulaci zacyklení do požadovaného cílového počtu
        /// </summary>
        /// <param name="simulatedCycleCount"></param>
        /// <param name="progressAction"></param>
        /// <param name="statusAction"></param>
        public void SimulatedCycleCreate(int simulatedCycleCount, Action<ProgressArgs> progressAction = null)
        {
            if (!IsLoaded) return;

            if (simulatedCycleCount <= 0) return;
            if (simulatedCycleCount > 20) simulatedCycleCount = 20;  // Víc jich prostě nedáme

            int currentCount = SimulatedCycleCount;                  // Tolik simulovaných zacyklení máme v datech nyní
            int requestCount = simulatedCycleCount;                  // Tolik je jich požadováno
            if (requestCount == currentCount) return;                // Nebudeme přidávat.
            if (requestCount < currentCount)                         // Odebírat neumíme:
                throw new InvalidOperationException($"MapSegment.SimulatedCycleCreate() nemůže odebrat simulované zacyklení: obsahuje jich {currentCount}, je požadováno {requestCount}. Je třeba data znovu načíst a poté vytvořit simulaci zacyklení.");

            DoProgressAction(progressAction, ProgressState.Starting, $"Vytvářím zacyklení v datech...", 0m);

            var firstItems = FirstItems;
            var firstCount = firstItems.Length;
            if (firstCount == 0)
            {
                DoProgressAction(progressAction, ProgressState.Done, $"Nejsou žádné FirstItems, nebude žádné zacyklení.", 0m);
                return;
            }

            // Vytvořím zacyklení:
            List<MapItem> cycleItems = new List<MapItem>();
            int firstIndex = firstCount / 2 - 5;
            if (firstIndex < 0) firstIndex = 0;
            int count = requestCount;
            for (int i = 0; i < count; i++)
            {
                if (i < currentCount) continue;                      // Toto zacyklení jsme už provedli posledně, opakovaně jej dělat nebudeme.

                int index = firstIndex + i;
                if (index >= firstCount) break;                      // Došly nám záznamy

                var firstItem = firstItems[index];
                decimal ratio = (decimal)(i + 1) / (decimal)count;
                bool isCycled = _CycleSimulationOne(firstItem, ratio, progressAction);
                if (isCycled)
                    currentCount++;                                  // Vytvořeno => počet aktuální zvýšíme, na závěr tuto hodnotu uložíme do SimulatedCycleCount
                else
                    count++;                                         // Pokud jsem pro aktuální prvek nepřidal zacyklení, tak budu zkoušet o jeden další prvek víc...
            }

            DoProgressAction(progressAction, ProgressState.Done, $"Vytvářím zacyklení - hotovo.", 0m);

            SimulatedCycleCount = currentCount;
        }
        /// <summary>
        /// Přidá zacyklení do stromu vycházejícího z daného First prvku:
        /// </summary>
        /// <param name="firstItem"></param>
        private bool _CycleSimulationOne(MapItem firstItem, decimal ratio, Action<ProgressArgs> progressAction = null)
        {
            DoProgressAction(progressAction, ProgressState.Working, $"Přidávám zacyklení do stromu k prvku {firstItem}", ratio);

            // Zacyklení přidám do druhého prvku (z posledního prvku), to proto abych First item nechal jako First (když bych do něj přidal nějaký Prev prvek, už by nebyl First)
            var nextLinks = firstItem.NextLinks;
            var secondItem = (nextLinks is null || nextLinks.Length == 0 ? null : nextLinks[0].NextItem);
            if (secondItem is null)
            {
                DoProgressAction(progressAction, ProgressState.Working, $" - nelze přidat zacyklení, prvek FirstItem je solitér: {firstItem}", null);
                return false;
            }

            // Najdu vhodný Poslední prvek:
            var allItems = secondItem.GetAllNextItems();
            var lastItem = allItems.Where(i => i.IsLast).FirstOrDefault();
            if (lastItem is null)
            {
                DoProgressAction(progressAction, ProgressState.Working, $" - nelze přidat zacyklení, nenašli jsme žádný LastItem - asi už tu zacyklení máme: {firstItem}", null);
                return false;
            }

            // Pokud by Druhá == Poslední (tj. tam kde bych chtěl přidat zacyklovací vztah), ohlásím nemožnost:
            if (secondItem.ItemId == lastItem.ItemId)
            {
                DoProgressAction(progressAction, ProgressState.Working, $" - nelze přidat zacyklení, zvolený LastItem == SecondItem (jde o příliš krátký řetězec)", null);
                return false;
            }

            // Zajistím zacyklení Last => Second:
            secondItem.AddPrevLink(lastItem.ItemId);
            lastItem.AddNextLink(secondItem.ItemId);
            DoProgressAction(progressAction, ProgressState.Working, $" - přidáno zacyklení: Last: {lastItem.TextShort} => Second: {secondItem.TextShort}.", null);

            return true;
        }
        /// <summary>
        /// Formátuje číslo do tvaru "### ### ##0" (Trim)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string Format(int value)
        {
            return value.ToString("### ### ##0").Trim();
        }
        /// <summary>
        /// Vloží nový prvek z dodaného řádku mapy.
        /// </summary>
        /// <param name="rowId"></param>
        /// <param name="line"></param>
        private void AddLine(ref int rowId, string line)
        {
            rowId++;
            if (rowId == 1) return;                                       // Titulkový řádek

            if (String.IsNullOrEmpty(line)) return;

            var cells = line.Split('\t');
            int count = cells.Length;
            if (count < 11) return;

            MapItem mapItem = new MapItem(this, rowId, cells);
            if (mapItem.ItemId == 0) return;
            if (OnlyProcessedItems && mapItem.Sequence == "??") return;   // Prvek nezprocesovaný v mapě

            if (MapItems.ContainsKey(mapItem.ItemId))
                throw new ArgumentException($"Vstupní soubor obsahuje duplicitní položku '{mapItem.ItemIdText}'");

            MapItems.Add(mapItem.ItemId, mapItem);
        }
        /// <summary>
        /// zavolá progress akci a převezme výsledný Cancel
        /// </summary>
        /// <param name="progressAction"></param>
        /// <param name="state"></param>
        /// <param name="text"></param>
        /// <param name="ratio"></param>
        private void DoProgressAction(Action<ProgressArgs> progressAction, ProgressState state, string text, decimal? ratio)
        {
            if (progressAction != null)
            {
                var args = new ProgressArgs(state, text, ratio);
                progressAction(args);
                if (args.Cancel) this.Cancel = true;
            }
        }
        #endregion
        #region Data segmentu = jednotlivé MapItem a vztahy MapLink

        /// <summary>
        /// Počet prvků celkem
        /// </summary>
        public int ItemsCount { get { return MapItems.Count; } }
        /// <summary>
        /// Pole prvních prvků. Netříděné.
        /// </summary>
        public MapItem[] Items
        {
            get
            {
                var items = MapItems.Values.ToArray();
                return items;
            }
        }
        /// <summary>
        /// Pole prvních prvků, setříděné podle <see cref="MapItem.CompareByItemId"/>
        /// </summary>
        public MapItem[] FirstItems
        {
            get
            {
                var items = MapItems.Values.Where(i => i.IsFirst).ToList();
                items.Sort((a, b) => MapItem.CompareByItemId(a, b));
                return items.ToArray();
            }
        }
        /// <summary>
        /// Pole posledních prvků, setříděné podle <see cref="MapItem.CompareByItemId"/>
        /// </summary>
        public MapItem[] LastItems
        {
            get
            {
                var items = MapItems.Values.Where(i => i.IsLast).ToList();
                items.Sort((a, b) => MapItem.CompareByItemId(a, b));
                return items.ToArray();
            }
        }
        /// <summary>
        /// Pole prvních Main prvků, setříděné podle <see cref="MapItem.CompareByItemId"/>
        /// </summary>
        public MapItem[] FirstMainItems
        {
            get
            {
                var firstMainItems = new Dictionary<int, MapItem>();
                var firstItems = FirstItems;
                foreach (var firstItem in firstItems)
                {
                    if (firstItem.IsMainItem)
                    {
                        if (!firstMainItems.ContainsKey(firstItem.ItemId))
                            firstMainItems.Add(firstItem.ItemId, firstItem);
                    }
                    else
                    {
                        var nextMainItems = firstItem.MainNextItems;
                        foreach (var nextMainItem in nextMainItems)
                        {
                            if (!firstMainItems.ContainsKey(nextMainItem.ItemId))
                                firstMainItems.Add(nextMainItem.ItemId, nextMainItem);
                        }
                    }
                }
                var result = firstMainItems.Values.ToList();
                result.Sort((a, b) => MapItem.CompareByItemId(a, b));
                return result.ToArray();
            }
        }
        /// <summary>
        /// Pole prvků již vyřešených
        /// </summary>
        public MapItem[] ItemsProcessed { get { return MapItems.Values.Where(i => i.IsProcessed).ToArray(); } }
        /// <summary>
        /// Počet prvků již vyřešených
        /// </summary>
        public int ItemsProcessedCount { get { return ItemsProcessed.Length; } }
        /// <summary>
        /// Pole prvků čekajících za zpracování
        /// </summary>
        public MapItem[] ItemsWaiting { get { return MapItems.Values.Where(i => !i.IsProcessed).ToArray(); } }
        /// <summary>
        /// Počet prvků čekajících za zpracování
        /// </summary>
        public int ItemsWaitingCount { get { return ItemsWaiting.Length; } }
        /// <summary>
        /// Dictionary všech prvků
        /// </summary>
        public Dictionary<int, MapItem> MapItems { get; private set; }
        /// <summary>
        /// Pole vztahů mez dvěma prvky <see cref="MapItem"/>, klíčem je složený klíč obou prvků, viz metoda 
        /// </summary>
        public Dictionary<long, MapLink> MapLinks { get; private set; }
        public StringHeap StringHeap { get; private set; }
        #endregion
        #region Práce s daty segmentu - Vyhledání prvků, vyhledání a tvorba linků
        /// <summary>
        /// Resetuje všechny prvky = budou uvedeny do stavu před zahájením analýzy
        /// </summary>
        public void ResetItems()
        {
            foreach (var item in MapItems.Values)
                item.ResetItem();
        }
        /// <summary>
        /// Uvolní všechny prvky
        /// </summary>
        public void Clear()
        {
            this.DataState = LoadingState.None;
            MapItems.Clear();
            MapLinks.Clear();
            StringHeap.Clear();
            SimulatedCycleCount = 0;
        }
        /// <summary>
        /// Zkusí najít prvek daného ID
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetMapItem(int itemId, out MapItem item)
        {
            if (MapItems.TryGetValue(itemId, out item)) return true;
            return false;
        }
        /// <summary>
        /// Vyhledá prvky pro dané hodnoty <see cref="MapItem.ItemId"/> a nalezené prvky (ne null) vrátí jako pole.
        /// pokud na vstupu je null, pak vrátí null.
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        public MapItem[] GetMapItems(IEnumerable<int> itemIds)
        {
            if (itemIds is null) return null;
            var items = new List<MapItem>();
            foreach (var itemId in itemIds)
            {
                if (MapItems.TryGetValue(itemId, out var item))
                    items.Add(item);
            }
            return items.ToArray();
        }
        /// <summary>
        /// Najde nebo vytvoří a vrátí pole vztahů pro dané pole ID záznamů vztahů <paramref name="itemIds"/>,
        /// a kde je dodaný konstantní prvek každého vztahu <paramref name="prevItem"/> nebo <paramref name="nextItem"/>.
        /// Tak lze vytvořit pole vztahů z Prev i z Next prvku.
        /// </summary>
        /// <param name="prevId"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        public MapLink[] GetMapLinks(int prevId, int nextId)
        {
            return _GetMapLinks(null, prevId, nextId);
        }
        /// <summary>
        /// Najde nebo vytvoří a vrátí pole vztahů pro dané pole ID záznamů vztahů <paramref name="itemIds"/>,
        /// a kde je dodaný konstantní prvek každého vztahu <paramref name="prevItem"/> nebo <paramref name="nextItem"/>.
        /// Tak lze vytvořit pole vztahů z Prev i z Next prvku.
        /// </summary>
        /// <param name="prevIds"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        public MapLink[] GetMapLinks(IEnumerable<int> prevIds, int nextId)
        {
            return _GetMapLinks(prevIds, 0, nextId);
        }
        /// <summary>
        /// Najde nebo vytvoří a vrátí pole vztahů pro dané pole ID záznamů vztahů <paramref name="itemIds"/>,
        /// a kde je dodaný konstantní prvek každého vztahu <paramref name="prevItem"/> nebo <paramref name="nextItem"/>.
        /// Tak lze vytvořit pole vztahů z Prev i z Next prvku.
        /// </summary>
        /// <param name="prevId"></param>
        /// <param name="nextIds"></param>
        /// <returns></returns>
        public MapLink[] GetMapLinks(int prevId, IEnumerable<int> nextIds)
        {
            return _GetMapLinks(nextIds, prevId, 0);
        }
        /// <summary>
        /// Najde nebo vytvoří a vrátí pole vztahů pro dané pole ID záznamů vztahů <paramref name="itemIds"/>,
        /// a kde je dodaný konstantní prvek každého vztahu <paramref name="prevItem"/> nebo <paramref name="nextItem"/>.
        /// Tak lze vytvořit pole vztahů z Prev i z Next prvku.
        /// </summary>
        /// <param name="itemIds"></param>
        /// <param name="prevItem"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        private MapLink[] _GetMapLinks(IEnumerable<int> itemIds, int prevId, int nextId)
        {
            var mapLinks = new List<MapLink>();
            bool prevFix = prevId > 0;
            bool nextFix = nextId > 0;
            if (prevFix && nextFix)
                AddMapLinksTo(prevId, nextId, mapLinks);                                      // Mám Prev i Next, řešíme pouze jednu konstantní propojku
            else if (prevFix || nextFix)
            {
                foreach (var itemId in itemIds)
                {
                    if (prevFix && !nextFix) AddMapLinksTo(prevId, itemId, mapLinks);         // Mám Prev ale ne Next, doplním jako Next nalezený vztažený
                    else if (!prevFix && nextFix) AddMapLinksTo(itemId, nextId, mapLinks);    // Mám Next ale ne Prev, doplním jako Prev nalezený vztažený
                }
            }
            return mapLinks.ToArray();
        }
        /// <summary>
        /// Pokud najde/vytvoří propojku pro dva dané prvky, pak tuto propojku přidá do daného pole.
        /// </summary>
        /// <param name="prevId"></param>
        /// <param name="nextId"></param>
        /// <param name="mapLinks"></param>
        private void AddMapLinksTo(int prevId, int nextId, List<MapLink> mapLinks)
        {
            var link = GetMapLink(prevId, nextId);
            if (link != null)
                mapLinks.Add(link);
        }
        /// <summary>
        /// Najde nebo vytvoří propojku <see cref="MapLink"/> pro dané dva prvky <paramref name="prevId"/> a <paramref name="nextId"/>.
        /// Propojky jsou klíčovány pomocí klíčů prvků a evidovány v Dictionary <see cref="MapLinks"/>, proto vždy existuje jen jedna instance propojky pro dva prvky <see cref="MapItem"/>.
        /// </summary>
        /// <param name="prevId"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        public MapLink GetMapLink(int prevId, int nextId)
        {
            if (prevId <= 0 || nextId <= 0) return null;
            var linkId = ConvertPairIdToLinkId(prevId, nextId);
            if (!MapLinks.TryGetValue(linkId, out var mapLink))
            {
                bool hasPrevItem = TryGetMapItem(prevId, out var prevItem);
                bool hasNextItem = TryGetMapItem(nextId, out var nextItem);
                if (hasPrevItem && hasNextItem)
                {
                    mapLink = new MapLink(linkId, prevItem, nextItem);
                    MapLinks.Add(linkId, mapLink);
                }
            }
            return mapLink;
        }
        /// <summary>
        /// Najde nebo vytvoří propojku <see cref="MapLink"/> pro dané dva prvky <paramref name="prevItem"/> a <paramref name="nextItem"/>.
        /// Propojky jsou klíčovány pomocí klíčů prvků a evidovány v Dictionary <see cref="MapLinks"/>, proto vždy existuje jen jedna instance propojky pro dva prvky <see cref="MapItem"/>.
        /// </summary>
        /// <param name="prevItem"></param>
        /// <param name="nextItem"></param>
        /// <returns></returns>
        public MapLink GetMapLink(MapItem prevItem, MapItem nextItem)
        {
            if (prevItem is null || nextItem is null) return null;
            var linkId = ConvertPairIdToLinkId(prevItem.ItemId, nextItem.ItemId);
            if (!MapLinks.TryGetValue(linkId, out var mapLink))
            {
                mapLink = new MapLink(linkId, prevItem, nextItem);
                MapLinks.Add(linkId, mapLink);
            }
            return mapLink;
        }
        #endregion
        #region Konvertory ID
        /// <summary>
        /// Pro dvě hodnoty <see cref="MapItem.ItemId"/> (ve smyslu Prev, Next) vrátí LinkId obsahující obě hodnoty.
        /// </summary>
        /// <param name="prevId"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        public static long ConvertPairIdToLinkId(int prevId, int nextId)
        {
            return OffsetPx * (long)prevId + (long)nextId;
        }
        /// <summary>
        /// Pro danou kolekci stringových Id ve tvaru "Ax12345" nebo "Tc4567" vrátí pole odpovídajících Int32 hodnot
        /// </summary>
        /// <param name="idsText"></param>
        /// <returns></returns>
        public static int[] ConvertIdToInt(string[] idsText)
        {
            if (idsText is null) return null;
            var ids = idsText
                .Select(i => ConvertIdToInt(i))
                .Where(i => i > 0)
                .ToArray();
            if (ids.Length == 0) return null;
            return ids;
        }
        /// <summary>
        /// Pro dané stringové Id ve tvaru "Ax12345" nebo "Tc4567" vrátí odpovídající Int32 hodnotu
        /// </summary>
        /// <param name="idText"></param>
        /// <returns></returns>
        public static int ConvertIdToInt(string idText)
        {
            if (String.IsNullOrEmpty(idText) || idText.Length < 3) return 0;
            string prefix = idText.Substring(0, 2);
            int value = (prefix == "Ax" ? OffsetAx : (prefix == "Tc" ? OffsetTc : 0));
            if (value == 0) return 0;
            string number = idText.Substring(2);
            if (!Int32.TryParse(number, out var n)) return 0;
            return value + n;
        }
        /// <summary>
        /// Pro danou Int hodnotu Id vrátí vizuální hodnotu stringového Id ve tvaru "Ax12345" nebo "Tc4567"
        /// </summary>
        /// <param name="idInt"></param>
        /// <returns></returns>
        public static string ConvertIdToText(int idInt)
        {
            if (idInt < OffsetAx) return "";
            if (idInt < OffsetTc) return "Ax" + (idInt - OffsetAx).ToString();
            if (idInt < OffsetMx) return "Tc" + (idInt - OffsetTc).ToString();
            return "";
        }
        private const int OffsetAx = 100000000;
        private const int OffsetTc = 200000000;
        private const int OffsetMx = 300000000;
        private const long OffsetPx = 1000000000L;
        #endregion
    }
    #region class MapItem
    public class MapItem
    {
        #region Tvorba prvku
        public MapItem(MapSegment segment, int rowId, string[] cells)
        {
            _Segment = segment;
            _RowId = rowId;

            // Pořadí prvků:
            // LocalID	Owner	Type	Previous	Next	Qty	Time	Sequence	GlobalID	Value	Descriptions
            //    0      1       2        3          4       5   6       7           8           9       10  11  12
            int count = cells.Length;

            _ItemId = MapSegment.ConvertIdToInt(cells[0]);
            _OwnerId = MapSegment.ConvertIdToInt(cells[1]);
            _Type = segment.StringHeap.GetId(cells[2]);
            _PrevIds = MapSegment.ConvertIdToInt(SplitItems(cells[3]));
            _NextIds = MapSegment.ConvertIdToInt(SplitItems(cells[4]));
            _Qty = segment.StringHeap.GetId(cells[5]);
            _Time = segment.StringHeap.GetId(cells[6]);
            _Sequence = segment.StringHeap.GetId(cells[7]);
            _Value = segment.StringHeap.GetId(cells[9]);
            _Description1 = (count >= 11 ? segment.StringHeap.GetId(cells[10]) : 0);
            _Description2 = (count >= 12 ? segment.StringHeap.GetId(cells[11]) : 0);
            _Description3 = (count >= 13 ? segment.StringHeap.GetId(cells[12]) : 0);

            // Sekundární:
            var itemType = GetMapItemType(cells[2]);
            _ItemType = itemType;
            _IsByProduct = (itemType == MapItemType.IncrementByRealByProductSuitable || itemType == MapItemType.IncrementByPlanByProductSuitable || itemType == MapItemType.IncrementByRealByProductDissonant || itemType == MapItemType.IncrementByPlanByProductDissonant);
            _IsMainItem = (itemType == MapItemType.IncrementByRealSupplierOrder
                || itemType == MapItemType.OperationReal || itemType == MapItemType.OperationPlan
                || itemType == MapItemType.IncrementByRealProductOrder || itemType == MapItemType.IncrementByPlanProductOrder 
                || itemType == MapItemType.DecrementByPlanEnquiry || itemType == MapItemType.DecrementByRealEnquiry);
        }
        private static string[] SplitItems(string cell)
        {
            if (String.IsNullOrEmpty(cell)) return null;

            if (_Separator == null) _Separator = new string[] { ", " };

            return cell.Split(_Separator, StringSplitOptions.RemoveEmptyEntries);
        }
        private static string[] _Separator;
        public override string ToString()
        {
            return TextLong;
        }
        /// <summary>
        /// Krátký text : ItemIdText Type "Description1"
        /// </summary>
        public string TextShort { get { return $"{ItemIdText} {Type} \"{Description1}\""; } }
        /// <summary>
        /// Dlouhý text : ItemIdText Type PrevInfo NextInfo "Description1"
        /// </summary>
        public string TextLong { get { return $"ItemId: {ItemIdText}; Type: {Type}; Prev: {PrevInfo}; Next: {NextInfo}; Info: {Description1}"; } }
        #endregion
        #region Data prvku základní
        private MapSegment _Segment;
        public int RowId { get { return _RowId; } }
        private int _RowId;
        public int ItemId { get { return _ItemId; } }
        private int _ItemId;
        public string ItemIdText { get { return MapSegment.ConvertIdToText(_ItemId); } }
        public int OwnerId { get { return _OwnerId; } }
        private int _OwnerId;
        public string OwnerIdText { get { return MapSegment.ConvertIdToText(_OwnerId); } }
        public string Type { get { return _Segment.StringHeap.GetText(_Type); } }
        private int _Type;
        public int[] PrevIds { get { return _PrevIds; } }
        private int[] _PrevIds;
        public int[] NextIds { get { return _NextIds; } }
        private int[] _NextIds;
        public string Qty { get { return _Segment.StringHeap.GetText(_Qty); } }
        private int _Qty;
        public string Time { get { return _Segment.StringHeap.GetText(_Time); } }
        private int _Time;
        public string Sequence { get { return _Segment.StringHeap.GetText(_Sequence); } }
        private int _Sequence;
        public string Value { get { return _Segment.StringHeap.GetText(_Value); } }
        private int _Value;
        public string Description1 { get { return _Segment.StringHeap.GetText(_Description1); } }
        private int _Description1;
        public string Description2 { get { return _Segment.StringHeap.GetText(_Description2); } }
        private int _Description2;
        public string Description3 { get { return _Segment.StringHeap.GetText(_Description3); } }
        private int _Description3;
        #endregion
        #region Data prvku podpůrná, komparátory
        private string PrevInfo { get { return (IsFirst ? "IsFirst" : PrevIds.Length.ToString() + " items"); } }
        private string NextInfo { get { return (IsLast ? "IsLast" : NextIds.Length.ToString() + " items"); } }
        /// <summary>
        /// true pokud prvek je PRVNÍ v řadě
        /// </summary>
        public bool IsFirst { get { return this.PrevIds is null; } }
        /// <summary>
        /// true pokud prvek je POSLEDNÍ v řadě
        /// </summary>
        public bool IsLast { get { return this.NextIds is null; } }
        /// <summary>
        /// Komparátor podle <see cref="ItemId"/>: nejprve dává Ax, poté Tc; pak podle hodnoty.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByItemId(MapItem a, MapItem b)
        {
            return a.ItemId.CompareTo(b.ItemId);
        }
        #endregion
        #region Podpora pro analýzu
        /// <summary>
        /// Příznak, zda prvek byl zacyklen; výchozí je null = nevíme.
        /// </summary>
        public bool? IsCycled { get; set; }
        /// <summary>
        /// Typ prvku <see cref="Type"/> ondemand převedený na enum
        /// </summary>
        internal MapItemType ItemType { get { return _ItemType; } } private MapItemType _ItemType;
        /// <summary>
        /// Jde o ByProduct?
        /// Tedy typ prvku je: <see cref="MapItemType.IncrementByRealByProductSuitable"/> nebo <see cref="MapItemType.IncrementByPlanByProductSuitable "/> 
        /// nebo <see cref="MapItemType.IncrementByRealByProductDissonant"/> nebo <see cref="MapItemType.IncrementByPlanByProductDissonant"/>
        /// </summary>
        public bool IsByProduct { get { return _IsByProduct; } } private bool _IsByProduct;
        /// <summary>
        /// Tento prvek je Main, tedy bude zobrazován ve zjednodušené mapě?
        /// Tedy typ prvku je: <see cref="MapItemType.IncrementByRealSupplierOrder"/> 
        /// nebo <see cref="MapItemType.OperationReal "/> nebo <see cref="MapItemType.OperationPlan"/> 
        /// nebo <see cref="MapItemType.IncrementByRealProductOrder "/> nebo <see cref="MapItemType.IncrementByPlanProductOrder"/> 
        /// nebo <see cref="MapItemType.DecrementByPlanEnquiry"/> nebo <see cref="MapItemType.DecrementByRealEnquiry"/>
        /// </summary>
        public bool IsMainItem { get { return _IsMainItem; } } private bool _IsMainItem;
        /// <summary>
        /// Prev vztahy.
        /// Ve vztahu je jako <see cref="MapLink.NextItem"/> prvek this a ve vztahu <see cref="MapLink.PrevItem"/> je prvek vlevo.
        /// Toto pole může být null, pokud this prvek je první = má <see cref="IsFirst"/> = true.
        /// Pokud není null, pak typicky obsahuje nějaký prvek, tyto prvky vždy mají naplněné obě instance (<see cref="MapLink.PrevItem"/> i <see cref="MapLink.NextItem"/>).
        /// </summary>
        public MapLink[] PrevLinks
        {
            get
            {
                if (!IsFirst && _PrevLinks is null)
                    _PrevLinks = _Segment.GetMapLinks(_PrevIds, ItemId);
                return _PrevLinks;
            }
        }
        private MapLink[] _PrevLinks;
        /// <summary>
        /// Předchozí prvky
        /// </summary>
        public MapItem[] PrevItems { get { return _Segment.GetMapItems(_PrevIds); } }
        /// <summary>
        /// Následující prvky
        /// </summary>
        public MapItem[] NextItems { get { return _Segment.GetMapItems(_NextIds); } }
        /// <summary>
        /// Next vztahy.
        /// Ve vztahu je jako <see cref="MapLink.PrevItem"/> prvek this a ve vztahu <see cref="MapLink.NextItem"/> je prvek vpravo.
        /// Toto pole může být null, pokud this prvek je poslední = má <see cref="IsLast"/> = true.
        /// Pokud není null, pak typicky obsahuje nějaký prvek, tyto prvky vždy mají naplněné obě instance (<see cref="MapLink.PrevItem"/> i <see cref="MapLink.NextItem"/>).
        /// </summary>
        public MapLink[] NextLinks
        {
            get
            {
                if (!IsLast && _NextLinks is null)
                    _NextLinks = _Segment.GetMapLinks(ItemId, _NextIds);
                return _NextLinks;
            }
        }
        private MapLink[] _NextLinks;
        /// <summary>
        /// Pole prvků typu Main ve směru Prev (vlevo) z this prvku.
        /// Toto pole může být prázdné, pokud this v daném směru už neexistuje žádný Main prvek, ale nebude null.
        /// </summary>
        public MapItem[] MainPrevItems { get { return GetOtherItems(this, Side.Prev, add => (add.ItemId != this.ItemId && add.IsMainItem), scan => (scan.ItemId == this.ItemId || !scan.IsMainItem)); } }
        /// <summary>
        /// Pole prvků typu Main ve směru Prev (vlevo) z this prvku.
        /// Toto pole může být prázdné, pokud this v daném směru už neexistuje žádný Main prvek, ale nebude null.
        /// </summary>
        public MapItem[] MainNextItems { get { return GetOtherItems(this, Side.Next, add => (add.ItemId != this.ItemId && add.IsMainItem), scan => (scan.ItemId == this.ItemId || !scan.IsMainItem)); } }
        /// <summary>
        /// Obsahuje všechny prvky počínaje z daného prvku včetně ve směru Prev. Pokud dojde k zacyklení ve vstupních datech, nezacyklí se.
        /// </summary>
        /// <param name="firstItem"></param>
        /// <returns></returns>
        public MapItem[] GetAllPrevItems() { return GetOtherItems(this, Side.Prev, add => true, scan => true); }

            /*
            Dictionary<int, MapItem> allDict = new Dictionary<int, MapItem>();
            Queue<MapItem> queue = new Queue<MapItem>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                // Vyzvednu prvek ke zpracování, ale pokud jej už mám ve výsledku, pak ho tam nedám a ani neřeším jeho Next prvky = ochrana před zacyklením:
                var item = queue.Dequeue();
                if (allDict.ContainsKey(item.ItemId)) continue;
                allDict.Add(item.ItemId, item);

                // Prvek jsme dosud neměli - detekuji jeho Prev prvky a podmíněně je přidám do fronty ke zpracování:
                var prevLinks = item.PrevLinks;
                if (prevLinks is null || prevLinks.Length == 0) continue;
                foreach (var prevLink in prevLinks)
                {   // Zařadím Prev prvky do dalšího kola zpracování
                    if (!prevLink.IsLinkDisconnected && prevLink.PrevItem != null && !allDict.ContainsKey(prevLink.PrevItem.ItemId))
                        queue.Enqueue(prevLink.PrevItem);
                }
            }
            return allDict.Values.ToArray();
            */

        /// <summary>
        /// Obsahuje všechny prvky počínaje z daného prvku včetně ve směru Next. Pokud dojde k zacyklení ve vstupních datech, nezacyklí se.
        /// </summary>
        /// <param name="firstItem"></param>
        /// <returns></returns>
        public MapItem[] GetAllNextItems() { return GetOtherItems(this, Side.Next, add => true, scan => true); }

            /*
            Dictionary<int, MapItem> allDict = new Dictionary<int, MapItem>();
            Queue<MapItem> queue = new Queue<MapItem>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                // Vyzvednu prvek ke zpracování, ale pokud jej už mám ve výsledku, pak ho tam nedám a ani neřeším jeho Next prvky = ochrana před zacyklením:
                var item = queue.Dequeue();
                if (allDict.ContainsKey(item.ItemId)) continue;
                allDict.Add(item.ItemId, item);

                // Prvek jsme dosud neměli - detekuji jeho Next prvky a podmíněně je přidám do fronty ke zpracování:
                var nextLinks = item.NextLinks;
                if (nextLinks is null || nextLinks.Length == 0) continue;
                foreach (var nextLink in nextLinks)
                {   // Zařadím Next prvky do dalšího kola zpracování
                    if (!nextLink.IsLinkDisconnected && nextLink.NextItem != null && !allDict.ContainsKey(nextLink.NextItem.ItemId))
                        queue.Enqueue(nextLink.NextItem);
                }
            }
            return allDict.Values.ToArray();
            */

        /// <summary>
        /// Vrátí patřičné sousední prvky od daného výchozího prvku
        /// </summary>
        /// <param name="rootItem"></param>
        /// <param name="side"></param>
        /// <param name="addToResultFunc"></param>
        /// <param name="scanOtherItemsFunc"></param>
        /// <returns></returns>
        private static MapItem[] GetOtherItems(MapItem rootItem, Side side, Func<MapItem, bool> addToResultFunc, Func<MapItem, bool> scanOtherItemsFunc)
        {
            List<MapItem> result = new List<MapItem>();                                  // Toto je náš výsledek
            Dictionary<int, MapItem> allScan = new Dictionary<int, MapItem>();           // Tyto prvky jsme už prošli (to abychom se při nalezené jiné boční cesty anebo zacyklení neopakovali)
            Queue<MapItem> queue = new Queue<MapItem>();                                 // Tyto prvky teprve budeme procházet (fronta práce)

            // Začneme a pojedeme tak dlouho, dokud bude co dělat:
            queue.Enqueue(rootItem);
            while (queue.Count > 0)
            {
                // Vyzvednu prvek ke zpracování, ale pokud jej už mám ve výsledku, pak ho tam nedám a ani neřeším jeho Prev/Next prvky = ochrana před zacyklením:
                var item = queue.Dequeue();
                if (allScan.ContainsKey(item.ItemId)) continue;
                allScan.Add(item.ItemId, item);

                // Tento prvek vidíme prvně; chceme ho dát do výsledného soupisu?
                if (addToResultFunc(item)) result.Add(item);

                // Prvek jsme tu dosud neměli - zjistím, zda mám zpracovávat i jeho Prev/Next prvky, a podmíněně je přidám do fronty ke zpracování:
                if (!scanOtherItemsFunc(item)) continue;

                // Vezmu vztahy na sousední prvky v požadovaném směru:
                var otherLinks = (side == Side.Prev ? item.PrevLinks : (side == Side.Next ? item.NextLinks: null));
                if (otherLinks is null || otherLinks.Length == 0) continue;
                foreach (var otherLink in otherLinks)
                {   // Zařadím Prev/Next prvky do dalšího kola zpracování
                    if (otherLink.IsLinkDisconnected) continue;
                    var otherItem = (side == Side.Prev ? otherLink.PrevItem : (side == Side.Next ? otherLink.NextItem : null));
                    if (otherItem != null && !allScan.ContainsKey(otherItem.ItemId))
                        queue.Enqueue(otherItem);
                }
            }
            return result.ToArray();
        }
        /// <summary>
        /// Strana
        /// </summary>
        private enum Side { None, Prev, Next }
        /// <summary>
        /// Obsahuje true, pokud všechny vztahy mezi Prev prvky v <see cref="PrevLinks"/> a this prvkem jsou zpracované = mají <see cref="MapLink.IsLinkProcessed"/> = true;
        /// Pokud this prvek nemá žádné Prev prvky (zdejší <see cref="IsFirst"/> je true), pak <see cref="AllPrevLinksIsProcessed"/> je true.
        /// </summary>
        public bool AllPrevLinksIsProcessed
        {
            get
            {
                if (IsFirst) return true;
                var prevLinks = PrevLinks;
                return (prevLinks is null || prevLinks.Length == 0 || prevLinks.All(l => l.IsLinkProcessed || l.IsLinkDisconnected));
            }
        }
        /// <summary>
        /// Obsahuje true, pokud všechny vztahy mezi this prvkem a Next prvky v <see cref="NextLinks"/> jsou zpracované = mají <see cref="MapLink.IsLinkProcessed"/> = true;
        /// Pokud this prvek nemá žádné Next prvky (zdejší <see cref="IsLast"/> je true), pak <see cref="AllNextLinksIsProcessed"/> je true.
        /// </summary>
        public bool AllNextLinksIsProcessed
        {
            get
            {
                if (IsLast) return true;
                var nextLinks = NextLinks;
                return (nextLinks is null || nextLinks.Length == 0 || nextLinks.All(l => l.IsLinkProcessed));
            }
        }
        /// <summary>
        /// Resetuje tento prvek = bude uveden do stavu před zahájením analýzy
        /// </summary>
        public void ResetItem()
        {
            ResultSequence = 0;
        }
        /// <summary>
        /// Obsahuje true u položky, která již má přidělenou výslednou sekvenci ve <see cref="ResultSequence"/>
        /// </summary>
        public bool IsProcessed { get { return (ResultSequence > 0); } }
        /// <summary>
        /// Zadané výsledné pořadí. Je vloženo v procesu analýzy, je resetováno metodu <see cref="ResetItem"/>.
        /// Hodnota 0 = před zahájením analýzy.
        /// První prvek má +1, jeho nejbližší Next má 2, atd...
        /// </summary>
        public int ResultSequence { get; set; }
        /// <summary>
        /// Aktuálně očekávané pořadí = Max(z Prev prvků z jejich <see cref="ExpectedSequence"/>) + 1.
        /// Pokud Prev prvky nejsou (my jsme First), pak je zde 1;
        /// Pokud žádný z Prev prvků nemá určenou svoji sekvenci, pak je zde 0.
        /// Pokud alespoň jeden Prev prvek má určenou svoji sekvenci, pak je zde Max(Prev) + 1.
        /// První prvek (který má <see cref="IsFirst"/> = true) má hodnotu <see cref="ExpectedSequence"/> = 1.
        /// </summary>
        public int ExpectedSequence
        {
            get
            {
                if (IsFirst) return 1;

                // Pokud do this prvku už někdo vepsal hodnotu ResultSequence, pak ji akceptujeme:
                if (IsProcessed) return ResultSequence;

                // Pokud ne, pak získáme naši sekvenci z Prev prvků:
                int seqence = SearchExpectedSequence(this);
                return seqence;
            }
        }
        private static int SearchExpectedSequence(MapItem topItem)
        {
            int sequence = 0;                    // Průběžně určená sekvence prvku topItem (výstup metody) u nalezených finálních hodnot
            int distance = 0;                    // Vzdálenost zpracovávaných prvků od prvku topItem (jednotlivé vrstvy předchozích prvků)
            Dictionary<int, MapItem> workDict = new Dictionary<int, MapItem>();
            Queue<MapItem> currentQueue = new Queue<MapItem>();
            Queue<MapItem> nextQueue = new Queue<MapItem>();

            workDict.Add(topItem.ItemId, topItem);
            currentQueue.Enqueue(topItem);
            distance++;
            while (true)
            {
                var item = currentQueue.Dequeue();
                var prevLinks = item.PrevLinks;
                if (prevLinks != null && prevLinks.Length > 0)
                {
                    foreach (var prevLink in prevLinks)
                    {
                        if (prevLink.IsLinkDisconnected) continue;

                        var prevItem = prevLink.PrevItem;
                        if (prevItem != null)
                        {
                            if (prevItem.ResultSequence < 0)
                            {   // Předchozí prvek dosud nemá napočtenou sekvenci? Pokusíme se ji určit výpočtem tady.
                                // Toto je nouzová varianta pro neřešené zacyklené prvky, protože algoritmus nápočtu sekvence postupuje dopředně,
                                // a hodnotu ExpectedSequence načítá jen z těch prvků, jejichž všechny Prev prvky už jsou zpracované!
                                if (!workDict.ContainsKey(prevItem.ItemId))
                                {   // Dosud neznámý prvek:
                                    workDict.Add(prevItem.ItemId, prevItem);
                                    nextQueue.Enqueue(prevItem);
                                }
                            }
                            else
                            {   // Když předchozí prvek už má finálně určenou sekvenci, nemusím procházet jeho zdroje,
                                // ale rovnou si načtu jeho finální sekvenci a určím naši min sekvenci (=sekvence prev prvku plus distance mezi naším prvkem a prvkem prev):
                                int minResultSequence = prevItem.ResultSequence + distance;
                                if (minResultSequence > sequence) sequence = minResultSequence;
                            }
                        }
                    }
                }
                if (currentQueue.Count == 0)
                {   // Doleva pokračujeme po ucelených vlnách, kdy v jedné vlně (currentQueue) máme prvky jedné předchozí hladiny,
                    //  a v následující vlně (nextQueue) jsou jejich vlastní Prev prvky (pouze distinct prostřednictvím workDict).
                    if (nextQueue.Count == 0) break;

                    // Tudy projdeme jen tehdy, když v Prev prvcích byl nalezen některý, který dosud nebyl zpracovaný:
                    distance++;                            // Vzdálenost "vlny" od výchozího prvku
                    currentQueue = nextQueue;
                    nextQueue = new Queue<MapItem>();
                }
            }

            // Vyhodnocení napočítané finální sekvence (sequence) určené z předchozích zpracovaných prvků, a z nejhlubší vlny:
            if (distance > sequence) sequence = distance;

            return sequence;
        }
        /// <summary>
        /// Metoda přeevede typ prvku ze stringu na enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static MapItemType GetMapItemType(string type)
        {
            if (String.IsNullOrEmpty(type)) return MapItemType.None;

            switch (type)
            {
                case "IncrementByRealSupplierOrder": return MapItemType.IncrementByRealSupplierOrder;
                case "IncrementByPlanSupplierOrder": return MapItemType.IncrementByPlanSupplierOrder;
                case "IncrementByProposalReceipt": return MapItemType.IncrementByProposalReceipt;
                case "DecrementByProposalRequisition": return MapItemType.DecrementByProposalRequisition;
                case "IncrementByPlanStockTransfer": return MapItemType.IncrementByPlanStockTransfer;
                case "DecrementByRealComponent": return MapItemType.DecrementByRealComponent;
                case "DecrementByPlanComponent": return MapItemType.DecrementByPlanComponent;
                case "IncrementByRealByProductSuitable": return MapItemType.IncrementByRealByProductSuitable;
                case "IncrementByPlanByProductSuitable": return MapItemType.IncrementByPlanByProductSuitable;
                case "IncrementByRealByProductDissonant": return MapItemType.IncrementByRealByProductDissonant;
                case "IncrementByPlanByProductDissonant": return MapItemType.IncrementByPlanByProductDissonant;
                case "IncrementByRealProductOrder": return MapItemType.IncrementByRealProductOrder;
                case "IncrementByPlanProductOrder": return MapItemType.IncrementByPlanProductOrder;
                case "DecrementByRealEnquiry": return MapItemType.DecrementByRealEnquiry;
                case "DecrementByPlanEnquiry": return MapItemType.DecrementByPlanEnquiry;
            }

            if (type.StartsWith("OpVP: ")) return MapItemType.OperationPlan;
            if (type.StartsWith("OpSTPV: ")) return MapItemType.OperationReal;

            return MapItemType.None;
        }
        #endregion
        #region Podpora pro vizualizaci
        /// <summary>
        /// Vizuální prvek
        /// </summary>
        internal MapVisualItem VisualItem { get; set; }
        #endregion
        #region Modifikace
        /// <summary>
        /// Přidá dané ID do pole Prev
        /// </summary>
        /// <param name="prevId"></param>
        public void AddPrevLink(int prevId)
        {
            _AddLink(ref _PrevIds, ref _PrevLinks, prevId);
        }
        /// <summary>
        /// Přidá dané ID do pole Next
        /// </summary>
        /// <param name="nextId"></param>
        public void AddNextLink(int nextId)
        {
            _AddLink(ref _NextIds, ref _NextLinks, nextId);
        }
        /// <summary>
        /// Přidá dané ID do daného pole Prev nebo Next, nuluje dané pole linků
        /// </summary>
        /// <param name="itemIds"></param>
        /// <param name="itemLinks"></param>
        /// <param name="itemId"></param>
        private void _AddLink(ref int[] itemIds, ref MapLink[] itemLinks, int itemId)
        {
            if (itemId <= 0) return;
            List<int> ids = new List<int>();
            if (itemIds != null) ids.AddRange(itemIds);
            if (!ids.Contains(itemId)) ids.Add(itemId);

            itemIds = ids.ToArray();             // Tato proměnná je instanční proměnná _PrevIds nebo _NextIds
            itemLinks = null;                    // Pole linků se dopočítá OnDemand, jde o instanční proměnnou _PrevLinks nebo _NextLinks
        }
        #endregion
    }
    /// <summary>
    /// Druh prvku podle jeho typu
    /// </summary>
    internal enum MapItemType : int
    {
        /// <summary>0: Není určeno</summary>
        None = 0,
        /// <summary>1: Toto není změna</summary>
        NotChange = 1,
        /// <summary>2: Zvýšení zásoby příjmem z existující objednávky od dodavatele</summary>
        IncrementByRealSupplierOrder = 2,
        /// <summary>3: Zvýšení zásoby příjmem z návrhu objednávky od dodavatele</summary>
        IncrementByPlanSupplierOrder = 3,
        /// <summary>4: Zvýšení zásoby příjmem z existujícího výrobního příkazu</summary>
        IncrementByRealProductOrder = 4,
        /// <summary>5: Zvýšení zásoby příjmem z návrhu výrobního příkazu</summary>
        IncrementByPlanProductOrder = 5,
        /// <summary>6: Zvýšení zásoby příjmem z vedlejšího produktu existujícího výrobního příkazu, použitelný materiál/polotovar</summary>
        IncrementByRealByProductSuitable = 6,
        /// <summary>7: Zvýšení zásoby příjmem z vedlejšího produktu návrhu výrobního příkazu, použitelný materiál/polotovar</summary>
        IncrementByPlanByProductSuitable = 7,
        /// <summary>8: Zvýšení zásoby příjmem z vedlejšího produktu existujícího výrobního příkazu, nepoužitelný materiál/polotovar</summary>
        IncrementByRealByProductDissonant = 8,
        /// <summary>9: Zvýšení zásoby příjmem z vedlejšího produktu návrhu výrobního příkazu, nepoužitelný materiál/polotovar</summary>
        IncrementByPlanByProductDissonant = 9,
        /// <summary>10: Zvýšení návrhem na příjem (obecný, který se má specifikovat na hodnotu: IncrementByPlanProductOrder nebo IncrementByPlanOrder nebo IncrementByPlanStockTransfer)</summary>
        IncrementByProposalReceipt = 10,
        /// <summary>11: Snížení zásoby výdejem do reálné poptávky</summary>
        DecrementByRealEnquiry = 11,
        /// <summary>12: Snížení zásoby výdejem do plánované poptávky (obchodní plán)</summary>
        DecrementByPlanEnquiry = 12,
        /// <summary>13: Snížení zásoby výdejem do komponenty reálného výrobního příkazu</summary>
        DecrementByRealComponent = 13,
        /// <summary>14: Snížení zásoby výdejem do komponenty plánovaného výrobního příkazu</summary>
        DecrementByPlanComponent = 14,
        /// <summary>15: Snížení zásoby návrhem na výdej do jiného skladu</summary>
        DecrementByProposalRequisition = 15,
        /// <summary>16: Příjem ze skladové objednávky, zajištěné vlastní výrobou</summary>
        IncrementByOrderProduction = 16,
        /// <summary>17: Příjem z návrhu převodu z jiného skladu</summary>
        IncrementByPlanStockTransfer = 17,

        /// <summary>Operace Výrobního příkazu</summary>
        OperationReal,
        /// <summary>Operace Návrhu výrobního příkazu</summary>
        OperationPlan
    }
    #endregion
    #region class MapLink
    public class MapLink
    {
        public MapLink(long linkId, MapItem prevItem, MapItem nextItem)
        {
            this.LinkId = linkId;
            this.PrevItem = prevItem;
            this.NextItem = nextItem;
        }
        public override string ToString()
        {
            return $"Prev: {PrevItem.TextShort} => Next: {NextItem.TextShort}";
        }
        public readonly long LinkId;
        public readonly MapItem PrevItem;
        public readonly MapItem NextItem;
        /// <summary>
        /// true po zpracování vztahu
        /// </summary>
        public bool IsLinkProcessed { get; set; }
        /// <summary>
        /// true když tento vztah je přerušen z pohledu logiky plánu.
        /// Takový vztah se při plánování musí ignorovat, jinak mohou být cesty plánu zacyklené!!!
        /// </summary>
        public bool IsLinkDisconnected { get; set; }
    }
    #endregion
    #region class StringHeap
    /// <summary>
    /// Paměťově úsporné úložiště stringů. Každý unikátní string ukládá jen jednou.
    /// Funguje jako úschovna na nádraží: vložíme sem String a dostaneme jeho Int32 ID a to si uschováme.
    /// Když pak chceme původní String, získáme ho z dodaného Int32 ID.
    /// <para/>
    /// Úspora vzniká tehdy, když uchováváme stejný String z vícero míst: 
    /// pak neuschováváme více instancí stejného Stringu, ale více hodnot typu Int32, jejichž stejná hodnota reprezentuje shodný String.
    /// Cílové property typu String mají get { } a set { } metody, 
    /// které používají zdejší metody <see cref="StringHeap.GetId(string)"/> a <see cref="StringHeap.GetText(int)"/>, 
    /// a pak mají privátní field typu Int32, kam fyzicky ukládají / čtou ID svého Stringu.
    /// </summary>
    public class StringHeap
    {
        public StringHeap()
        {
            _Values = new Dictionary<int, string>();
            _Index = new Dictionary<string, int>();
            _LastId = 0;
        }
        /// <summary>
        /// Vrátí ID pro daný text
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetId(string value)
        {
            if (value is null) return -1;
            if (value.Length == 0) return 0;
            if (!_Index.TryGetValue(value, out var id))
            {
                id = ++_LastId;
                _Values.Add(id, value);
                _Index.Add(value, id);
            }
            return id;
        }
        /// <summary>
        /// Vrátí text pro daný ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetText(int id)
        {
            if (id < 0) return null;
            if (id == 0) return "";
            if (!_Values.TryGetValue(id, out var text)) return null;
            return text;
        }
        /// <summary>
        /// Uvolní všechny prvky
        /// </summary>
        public void Clear()
        {
            _Values.Clear();
            _Index.Clear();
            _LastId = 0;
        }
        private int _LastId;
        private Dictionary<int, string> _Values;
        private Dictionary<string, int> _Index;
        private class StringItem
        {
            public StringItem(string value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return this.Value;
            }
            public readonly string Value;
        }
    }
    #endregion
    #region class ProgressArgs a enum
    /// <summary>
    /// Data o postupu
    /// </summary>
    public class ProgressArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="state"></param>
        /// <param name="text"></param>
        /// <param name="ratio"></param>
        public ProgressArgs(ProgressState state, string text, decimal? ratio = null)
        {
            this.State = state;
            this.Text = text;
            this.Ratio = ratio;
            this.Cancel = false;
        }
        /// <summary>
        /// Stav akce
        /// </summary>
        public ProgressState State { get; private set; }
        /// <summary>
        /// Textová informace
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Poměr postupu v rozsahu 0-1. Null když progres nemá určeno ratio.
        /// </summary>
        public decimal? Ratio { get; private set; }
        /// <summary>
        /// Požadavek na zastavení
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// Stav progresu
    /// </summary>
    public enum ProgressState
    {
        /// <summary>
        /// Nic se neděje
        /// </summary>
        None,
        /// <summary>
        /// Zahajuji akci
        /// </summary>
        Starting,
        /// <summary>
        /// Načítám data
        /// </summary>
        Loading,
        /// <summary>
        /// Zpracovávám data
        /// </summary>
        Working,
        /// <summary>
        /// Došlo k chybě
        /// </summary>
        Error,
        /// <summary>
        /// Akce zrušena
        /// </summary>
        Cancelled,
        /// <summary>
        /// Akce dokončena
        /// </summary>
        Done
    }
    #endregion
}
