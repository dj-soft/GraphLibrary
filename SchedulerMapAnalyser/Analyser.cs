using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.SchedulerMap.Analyser
{
    public class Analyser
    {
        #region Konstrukce a režie
        public Analyser()
        {
            _ShowInfoText = new StringBuilder();
        }
        /// <summary>
        /// Data segmentu. Před spuštěním analýzy musí být instance mapy setována do této property.
        /// Nemusí ale mít načtená data, je vhodnější nechat načtení na analyzeru, protože umí obsloužit progress a cancel.
        /// Při startu analýzy jsou data přenesena do interní proměnné a celý proces analýzy běží v takto akceptovaných datech. Setování jiné instance následně nemá vliv.
        /// Nicméně používání shodné instance i pro příští běhy zajistí sdílení dat.
        /// </summary>
        public MapSegment MapSegment { get; set; }
        /// <summary>
        /// Simuluj zacyklení dat pro daný počet položek. Minimum = 0 = bez zacyklení, maximum = 9
        /// </summary>
        public int CycleSimulation { get; set; }
        /// <summary>
        /// Do procesu brát i Vedlejší prodkuty
        /// </summary>
        public bool ScanByProduct { get; set; }
        /// <summary>
        /// Akce, kam se bude posílat text o progresu
        /// </summary>
        public Action<string> ShowInfo { get; set; }
        /// <summary>
        /// Asynchronně aktualizovaný a přebíraný status (analyzer sem kdykoliv píše, ale neinformuje o změně; volající si čas od času text přečte a vloží do status baru)
        /// </summary>
        public string StatusInfo { get; private set; }
        /// <summary>
        /// Asynchronně aktualizovaný a přebíraný ukazatel pokroku (analyzer sem kdykoliv píše, ale neinformuje o změně; volající si čas od času text přečte a vloží do status baru)
        /// </summary>
        public decimal StatusProgressRatio { get; private set; }
        /// <summary>
        /// Požadavek na storno procesu, lze nastavit kdykoliv, ve vhodném okamžiku bude akceptováno.
        /// </summary>
        public bool Cancel 
        {
            get { return _Cancel; }
            set { _Cancel = value; if (MapSegment != null) MapSegment.Cancel = value; if (_MapSegment != null) _MapSegment.Cancel = value; }
        }
        private bool _Cancel;
        public void Run()
        {
            try
            {
                _Run();
            }
            catch (Exception exc)
            {
                _ShowInfoLine($"EXCEPTION {exc.Message}");
                _ShowInfoLine(exc.StackTrace);
            }
        }
        private void _Run()
        {
            this.Cancel = false;
            _CheckMapSegment();
            _BreakCycles();
            _AnalyseItems();
            _CancelRun = Cancel;
            // _ShowDone();
            _ClearMemory();
            if (_CancelRun) _ShowInfoLine($"PŘERUŠENO UŽIVATELEM !");
        }

        private void _ProgressAction(string text, decimal? progressRatio)
        {
            _ShowInfoLine(text);
            if (progressRatio.HasValue) StatusProgressRatio = progressRatio.Value;
        }
        private void _StatusAction(string text, decimal? progressRatio)
        {
            StatusInfo = text;
            if (progressRatio.HasValue) StatusProgressRatio = progressRatio.Value;
        }
        /// <summary>
        /// Kompletní text do progress textu do metody <see cref="ShowInfo"/>
        /// </summary>
        private StringBuilder _ShowInfoText;
        private void _ShowInfoLine(string line)
        {
            if (line is null) return;

            if (_ShowInfoText.Length > 128000)
            {   // Zahodíme starší texty:
                string backup = _ShowInfoText.ToString().Substring(_ShowInfoText.Length - 16000);
                _ShowInfoText.Clear();
                _ShowInfoText.Append(backup);
            }

            string text = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line;
            _ShowInfoText.AppendLine(text);
            _ShowInfo(_ShowInfoText.ToString());
        }
        private void _ShowInfo(string info)
        {
            ShowInfo?.Invoke(info);
        }
        private void _ShowDone()
        {
            if (!Cancel)
            {
                string text = _ShowInfoText.ToString();
                for (int t = 0; t < 480; t++)
                {
                    if (Cancel) break;
                    text += ".";
                    _ShowInfo(text);
                    System.Threading.Thread.Sleep(60);
                }
            }
        }
        /// <summary>
        /// Požadavek na storno procesu přišel v době běhu procesu?
        /// </summary>
        private bool _CancelRun;
        private void _ClearMemory()
        {
            // _MapSegment.Clear();
            GC.Collect();
        }
        private string Format(int value)
        {
            return value.ToString("### ### ##0").Trim();
        }
        MapSegment _MapSegment;
        #endregion
        #region Příprava dat segmentu (načtení a simulace zacyklení)
        /// <summary>
        /// Zajistí převzetí segmentu z <see cref="MapSegment"/> do privátního <see cref="_MapSegment"/>, kontrolu a zajištěné načtení dat a zajistí i počet simulací cyklu.
        /// </summary>
        private void _CheckMapSegment()
        {
            _MapSegment = MapSegment;
            if (_MapSegment is null) throw new ArgumentNullException("Do Analyseru nebyla předána instance datového segmentu 'MapSegment'.");
            _MapSegment.Cancel = false;

            if (!_MapSegment.IsLoaded || _MapSegment.SimulatedCycleCount > this.CycleSimulation)
                _MapSegment.LoadData(_ProgressAction, _StatusAction);
            
            if (_MapSegment.SimulatedCycleCount < this.CycleSimulation)
                _MapSegment.SimulatedCycleCreate(this.CycleSimulation, _ProgressAction);

            _MapSegment.ResetItems();
        }
        #endregion
        #region Přerušení zacyklení položek
        /// <summary>
        /// Vyhledá a ohlásí a přeruší zacyklení
        /// </summary>
        private void _BreakCycles()
        {
            if (Cancel || !_MapSegment.IsLoaded) return;

            MapItem[] items;

            // Nejprve zpracuji prvky, které jsou IsFirst: v nezacyklených datech to je optimální cesta (začnu prvním prvkem a cestou doprava najdu všechny větve i zacyklení):
            items = this._MapSegment.FirstItems;
            _BreakCycles(items, "Výchozí prvky");

            // Mohlo by se stát, že některé prvky jsme neřešili, protože jsou v zacyklených oblastech (nemáme jejich první prvek), dohledám je a zpracuji nyní:
            items = this._MapSegment.Items.Where(i => !i.IsCycled.HasValue).ToArray();
            _BreakCycles(items, "Cyklické prvky");

            // Cancel tady platí pouze na hledání cyklu:
            if (Cancel)
            {
                _ShowInfoLine($" - Vyhledávání zacyklení přerušeno.");
                Cancel = false;
            }
        }
        private void _BreakCycles(MapItem[] items, string prefix)
        {
            if (Cancel) return;
            if (items is null || items.Length == 0) return;

            int count = items.Length;
            _ShowInfoLine($"Vyhledávám zacyklení - {prefix} počet prvků: {Format(count)}");
            for (int i = 0; i < count; i++)
            {
                if (Cancel) break;

                var item = items[i];
                if (!item.IsCycled.HasValue)
                {
                    string infoPrefix = Format(i + 1) + "/" + Format(count) + ": ";
                    _BreakCyclesOne(item, infoPrefix);
                }
            }

        }
        private void _BreakCyclesOne(MapItem item, string infoPrefix = "")
        {
            if (Cancel) return;

            // Zkratka:
            if (item.IsLast)
            {
                item.IsCycled = false;
                return;
            }

            // Akce:
            bool scanByProducts = ScanByProduct;
            PathSpiderLine path = new PathSpiderLine(item, 0, scanByProducts);
            int loops = 0;
            while (true)
            {
                if (Cancel) break;

                // Výpis textu až když tahle akce už stojí za zmínku:
                _BreakCyclesOneShowProgress(infoPrefix, item, path, ref loops);

                if (path.IsCycled(out int startCycle))
                {
                    string text = path.GetTextLong(startCycle);
                    this._ShowInfoLine("Nalezeno zacyklení:");
                    this._ShowInfoLine(text);

                    path.BreakCycle(startCycle);
                }
                else
                {
                    path.LastNode.MapItem.IsCycled = false;
                    if (!path.DoNextStep())
                        break;
                }
            }
        }

        private void _BreakCyclesOneShowProgress(string infoPrefix, MapItem item, PathSpiderLine path, ref int loops)
        {
            loops++;
            if (loops == 5000)
                _ShowInfoLine($" - {infoPrefix}{item.TextLong}");
            if ((loops % 10) == 0)
                StatusInfo = $"{Format(loops)} : Hledám zacyklení v prvku: {infoPrefix}{item.TextLong}, délka cesty: {path.NodeCount }";
        }

        private class PathSpiderLine
        {
            public PathSpiderLine(MapItem rootItem, int maxLinks = 0, bool scanByProducts = false)
            {
                MaxLinks = maxLinks;
                ScanByProducts = scanByProducts;
                Path = new List<PathSpiderNode>();
                CurrentNodeIndex = 0;
                AddLastNode(rootItem);
            }
            public override string ToString()
            {
                return TextShort;
            }
            public string TextShort { get { return _GetText(false, null, null); } }
            public string TextLong { get { return _GetText(true, null, null); } }
            public string GetTextLong(int startCycle) { return _GetText(true, startCycle, null); }
            /// <summary>
            /// Vrátí požadovaný text
            /// </summary>
            /// <param name="isLong"></param>
            /// <param name="startCycle"></param>
            /// <param name="endCycle"></param>
            /// <returns></returns>
            private string _GetText(bool isLong, int? startCycle, int? endCycle)
            {
                StringBuilder sb = new StringBuilder();
                var path = Path;
                bool hasStart = (startCycle.HasValue);
                bool hasEnd = (endCycle.HasValue);
                int last = CurrentNodeIndex;
                int start = (startCycle.HasValue ? startCycle.Value : 0);
                int end = (endCycle.HasValue ? endCycle.Value : last);
                start = (start < 0 ? 0 : (start > last ? last : start));
                end = (end < 0 ? 0 : (end > last ? last : end));
                string pos = "Root";
                for (int i = start; i <= end; i++)
                {
                    var node = path[i];
                    var mapItem = node.MapItem;
                    if (isLong)
                    {
                        string text = "[" + i.ToString("000") + "] " + pos.PadLeft(7) + "   :   " + mapItem.TextLong + "\r\n";
                        sb.Append(text);
                        pos = (node.CurrentIndex < 0 ? "   " : ((node.CurrentIndex + 1).ToString().PadLeft(3)) + "/") + node.NextLinksCount.ToString().PadLeft(3);
                    }
                    else
                    {
                        string text = $" => {mapItem.ItemIdText}";
                        sb.Append(text);
                    }
                }
                string result = sb.ToString();
                if (result.Length > 4 && !isLong)
                    result = result.Substring(4);
                return result;
            }
            /// <summary>
            /// Max počet sousedních linků
            /// </summary>
            private int MaxLinks;
            /// <summary>
            /// Scanovat i ByProducts
            /// </summary>
            private bool ScanByProducts;
            /// <summary>
            /// Sekvence nodů. Pozor, pole se nezmenšuje při odebírání, jen se posouvá <see cref="CurrentNodeIndex"/>!
            /// </summary>
            private List<PathSpiderNode> Path;
            /// <summary>
            /// Index prvku, na kterém se nachází Top prvek v poli <see cref="Path"/>.
            /// </summary>
            private int CurrentNodeIndex;
            /// <summary>
            /// Index poslední existující pozice v poli <see cref="Path"/>. Nejde o index posledního aktivního prvku (to je <see cref="CurrentNodeIndex"/>).
            /// Tato hodnota slouží pro optimalizaci práce s polem prvků, aby se pole nemuselo stále zmenšovat a prodlužovat 
            /// - ponechává se největší dosažená délka, a nevyužité pozice obsahují null..
            /// </summary>
            private int LastItemIndex { get { return NodeCount - 1; } }
            /// <summary>
            /// Počet nodů
            /// </summary>
            public int NodeCount { get { return Path.Count; } }
            /// <summary>
            /// Udělej krok na další prvek, vrátí true. Pokud není další prvek, vrátí false.
            /// </summary>
            /// <returns></returns>
            public bool DoNextStep()
            {
                // Další krok musí udělat ten prvek nejblíže konci, a pokud ten už nemůže další krok udělat, tak se odstraní; 
                //  následně se podobně zpracuje jeho předchozí krok = nyní nový poslední:
                var path = Path;
                int last = CurrentNodeIndex;
                for (int i = last; i >= 0; i--)
                {   // Jdeme od konce
                    var node = path[i];
                    if (node.DoNextStep())
                    {   // Pokud tento prvek může udělat další krok, udělá jej a je to OK:
                        var nextItem = node.CurrentNextItem;
                        if (nextItem != null)
                            AddLastNode(nextItem);
                        return true;
                    }
                    else if (i == 0)
                    {   // Tento prvek [i] nemůže udělat další krok, a je to Root prvek = skončíme:
                        return false;
                    }
                    else
                    {   // Tento prvek [i] nemůže udělat další krok, a je to nějaký Next prvek (nikoli Root) = odeberu jej:
                        RemoveLastNode();
                        // Bude následovat další cyklus na nižším indexu, tam přejdeme na sousední cestu nebo zase půjdeme dolů...
                    }
                }
                return false;
            }
            private void AddLastNode(MapItem mapItem)
            {
                var node = new PathSpiderNode(mapItem, MaxLinks, ScanByProducts);
                if (CurrentNodeIndex >= LastItemIndex)
                {
                    Path.Add(node);
                    CurrentNodeIndex = LastItemIndex;
                }
                else
                {
                    CurrentNodeIndex++;
                    Path[CurrentNodeIndex] = node;
                }
            }
            private void RemoveLastNode()
            {
                Path[CurrentNodeIndex] = null;
                CurrentNodeIndex--;
            }
            /// <summary>
            /// Zajistí, že prvky ZA daným indexem budou odříznuty, a že aktuální index <see cref="CurrentNodeIndex"/> bude na dané hodnotě.
            /// </summary>
            /// <param name="currentNodeIndex"></param>
            private void TrimToIndex(int currentNodeIndex)
            {
                int lastIndex = this.LastItemIndex;
                int index = (currentNodeIndex < 0 ? 0 : (currentNodeIndex > lastIndex ? lastIndex : currentNodeIndex));
                this.CurrentNodeIndex = index;
                for (int i = (index + 1); i <= lastIndex; i++)
                    Path[i] = null;
            }
            public bool IsCycled(out int startCycle)
            {
                startCycle = -1;

                // Zacyklení je, když na poslední pozici je stejný prvek, jako je někde před tím. Tedy nikoliv, když jediný prvek má nějaký vztah sám na sebe...
                // Takže Line s jedním prvkem zacyklena být nemůže:
                int currentNodeIndex = this.CurrentNodeIndex;
                if (currentNodeIndex <= 0) return false;

                var lastId = LastNode.MapItem.ItemId;
                var path = Path;
                for (int i = currentNodeIndex - 1; i >= 0; i--)
                {   // Jdeme od předposlední položky směrem k začátku:
                    // To proto, že hledáme ID poslední položky, proto hledáme od předposlední:
                    var node = path[i];
                    if (node.MapItem.ItemId == lastId)
                    {
                        startCycle = i;
                        break;
                    }
                }
                if (startCycle < 0) return false;
                return true;                    // Formulace kódu je kvůli breakpointu při zacyklení
            }
            /// <summary>
            /// Úkolem je zlomit zacyklení ve vhodném místě
            /// </summary>
            /// <param name="startCycle"></param>
            public void BreakCycle(int startCycle)
            {
                int currentNodeIndex = this.CurrentNodeIndex;
                int count = currentNodeIndex - startCycle + 1;
                var cycleItems = this.Path.GetRange(startCycle, count);

                var breakLink = SearchBreakLink(cycleItems, 
                    "IncrementByPlanByProductDissonant", "IncrementByRealByProductDissonant", 
                    "IncrementByPlanByProductSuitable", "IncrementByRealByProductSuitable",
                    "DecrementByPlanEnquiry", "DecrementByRealEnquiry",
                    "IncrementByOrderProduction", "IncrementByPlanStockTransfer");

                if (breakLink != null)
                {
                    breakLink.IsLinkDisconnected = true;
                    int index = this.Path.FindIndex(n => Object.ReferenceEquals(n.CurrentNextLink, breakLink));
                    if (index >= 0)
                        this.TrimToIndex(index);
                }

                /*
        NotChange = 1,
        /// <summary>2: Zvýšení zásoby příjmem z existující objednávky od dodavatele</summary>
        [EditStyleDisplayValue("Příjem z existující objednávky od dodavatele")]
        IncrementByRealSupplierOrder = 2,
        /// <summary>3: Zvýšení zásoby příjmem z návrhu objednávky od dodavatele</summary>
        [EditStyleDisplayValue("Příjem z návrhu objednávky od dodavatele")]
        IncrementByPlanSupplierOrder = 3,
		/// <summary>4: Zvýšení zásoby příjmem z existujícího výrobního příkazu</summary>
        [EditStyleDisplayValue("Příjem z existujícího výrobního příkazu")]
        IncrementByRealProductOrder = 4,
		/// <summary>5: Zvýšení zásoby příjmem z návrhu výrobního příkazu</summary>
        [EditStyleDisplayValue("Příjem z návrhu výrobního příkazu")]
        IncrementByPlanProductOrder = 5,
		/// <summary>6: Zvýšení zásoby příjmem z vedlejšího produktu existujícího výrobního příkazu, použitelný materiál/polotovar</summary>
        [EditStyleDisplayValue("Příjem vedlejšího produktu z existujícího výrobního příkazu, použitelný materiál/polotovar")]
        IncrementByRealByProductSuitable = 6,
		/// <summary>7: Zvýšení zásoby příjmem z vedlejšího produktu návrhu výrobního příkazu, použitelný materiál/polotovar</summary>
        [EditStyleDisplayValue("Příjem vedlejšího produktu z návrhu výrobního příkazu, použitelný materiál/polotovar")]
        IncrementByPlanByProductSuitable = 7,
		/// <summary>8: Zvýšení zásoby příjmem z vedlejšího produktu existujícího výrobního příkazu, nepoužitelný materiál/polotovar</summary>
        [EditStyleDisplayValue("Příjem vedlejšího produktu z existujícího výrobního příkazu, nepoužitelný materiál/polotovar")]
        IncrementByRealByProductDissonant = 8,
		/// <summary>9: Zvýšení zásoby příjmem z vedlejšího produktu návrhu výrobního příkazu, nepoužitelný materiál/polotovar</summary>
        [EditStyleDisplayValue("Příjem vedlejšího produktu z návrhu výrobního příkazu, nepoužitelný materiál/polotovar")]
        IncrementByPlanByProductDissonant = 9,
        /// <summary>10: Zvýšení návrhem na příjem (obecný, který se má specifikovat na hodnotu: IncrementByPlanProductOrder nebo IncrementByPlanOrder nebo IncrementByPlanStockTransfer)</summary>
        [EditStyleDisplayValue("Návrh na příjem")]
        IncrementByProposalReceipt = 10,
		/// <summary>11: Snížení zásoby výdejem do reálné poptávky</summary>
        [EditStyleDisplayValue("Výdej do reálné poptávky")]
        DecrementByRealEnquiry = 11,
		/// <summary>12: Snížení zásoby výdejem do plánované poptávky (obchodní plán)</summary>
        [EditStyleDisplayValue("Výdej do poptávky obchodního plánu")]
        DecrementByPlanEnquiry = 12,
		/// <summary>13: Snížení zásoby výdejem do komponenty reálného výrobního příkazu</summary>
        [EditStyleDisplayValue("Výdej komponenty do reálného výrobního příkazu")]
        DecrementByRealComponent = 13,
        /// <summary>14: Snížení zásoby výdejem do komponenty plánovaného výrobního příkazu</summary>
        [EditStyleDisplayValue("Výdej komponenty do reálného plánovaného příkazu")]
        DecrementByPlanComponent = 14,
		/// <summary>15: Snížení zásoby návrhem na výdej do jiného skladu</summary>
        [EditStyleDisplayValue("Návrh na výdej do jiného skladu")]
        DecrementByProposalRequisition = 15,
		/// <summary>16: Příjem ze skladové objednávky, zajištěné vlastní výrobou</summary>
        [EditStyleDisplayValue("Příjem ze skladové objednávky, zajištěné vlastní výrobou")]
        IncrementByOrderProduction = 16,
        /// <summary>17: Příjem z návrhu převodu z jiného skladu</summary>
        [EditStyleDisplayValue("Příjem z návrhu převodu z jiného skladu")]
        IncrementByPlanStockTransfer = 17
                */

            }
            /// <summary>
            /// Najde Link, který by bylo vhodné přerušit. Hledá v dané cestě prvek daného typu.
            /// Konkrétně najde prvek prvního ze zadaných typů, a vrátí jeho NextLink na navazující prvek v dané sekvenci.
            /// </summary>
            /// <param name="cycleItems"></param>
            /// <param name="types"></param>
            /// <returns></returns>
            private MapLink SearchBreakLink(List<PathSpiderNode> cycleItems, params string[] types)
            {
                if (cycleItems is null || cycleItems.Count == 0) return null;

                foreach (var type in types)
                {   // Postupně budu hledat první prvek daného typu, jeden typ po druhém:
                    var node = cycleItems.FirstOrDefault(n =>
                    {   // Vezmu Node, jeho aktuální NextLink, otestuji jeho PrevItem.Type a případně vracím ten NextLink:
                        var mapLink = n.CurrentNextLink;
                        return (mapLink != null && mapLink.PrevItem.Type == type);
                    });
                    if (node != null) return node.CurrentNextLink;
                }

                return cycleItems[0].CurrentNextLink;
            }
            /// <summary>
            /// Node na začátku seznamu vlevo. Nemění se.
            /// </summary>
            public PathSpiderNode RootNode { get { return Path[0]; } }
            /// <summary>
            /// Node na konci seznamu vpravo. Na začátku je zde <see cref="RootNode"/>.
            /// </summary>
            public PathSpiderNode LastNode { get { return Path[CurrentNodeIndex]; } }
        }
        private class PathSpiderNode
        {
            public PathSpiderNode(MapItem mapItem, int maxLinks = 0, bool scanByProducts = false)
            {
                this.MapItem = mapItem;
                this.NextLinks = mapItem.NextLinks;
                if (!scanByProducts && this.NextLinks != null)
                {
                    int byProductCount = this.NextLinks.Count(l => l.NextItem.IsByProduct);
                    if (byProductCount > 0 && byProductCount < this.NextLinks.Length)
                        this.NextLinks = this.NextLinks.Where(l => !l.NextItem.IsByProduct).ToArray();
                }
                this.NextLinksCount = (this.NextLinks?.Length ?? 0);
                this.CurrentIndex = -1;
                if (maxLinks > 0 && NextLinksCount > maxLinks) NextLinksCount = maxLinks;
            }
            public override string ToString()
            {
                string text = this.MapItem.TextLong;
                var next = this.CurrentNextItem;
                if (next != null)
                    text += $" => [{CurrentIndex}] {next.TextLong}";
                return text;
            }
            public MapItem MapItem { get; private set; }
            public MapLink[] NextLinks { get; private set; }
            public int NextLinksCount { get; private set; }
            /// <summary>
            /// Aktuální index: výchozí je -1, to je tehdy když tento node je aktivní prvek a je posledním prvkem v cestě.
            /// Pokud je zde 0, pak tento prvek už odkazuje na Next prvek první v seznamu.
            /// </summary>
            public int CurrentIndex { get; private set; }
            /// <summary>
            /// Prvek <see cref="MapLink.MapLink"/> z pole <see cref="NextLinks"/> na pozici [<see cref="CurrentIndex"/>]
            /// </summary>
            public MapLink CurrentNextLink
            {
                get
                {
                    int count = NextLinksCount;
                    if (count > 0)
                    {
                        int index = CurrentIndex;
                        if (index >= 0 && index < count)
                            return NextLinks[index];
                    }
                    return null;
                }
            } /// <summary>
              /// Prvek <see cref="MapLink.NextItem"/> z pole <see cref="NextLinks"/> na pozici [<see cref="CurrentIndex"/>]
              /// </summary>
            public MapItem CurrentNextItem { get { return CurrentNextLink?.NextItem; } }
            /// <summary>
            /// Přejde na další prvek v seznamu <see cref="NextLinks"/> a vrátí true.
            /// Pokud na další pozici nebude žádný prvek, pak vrátí false.
            /// </summary>
            public bool DoNextStep()
            {
                int count = NextLinksCount;
                while (CurrentIndex < count)
                {
                    CurrentIndex++;
                    var nextLink = CurrentNextLink;
                    if (nextLink != null && !nextLink.IsLinkDisconnected)
                    {
                        var nextItem = CurrentNextItem;
                        if (nextItem != null) return true;
                        // Musím projít i prvky již prověřené, protože k zacyklení může dojít i na delší cestě!    if (nextItem != null && !nextItem.IsCycled.HasValue) return true;
                    }
                }
                return false;
            }
        }
        #endregion
        #region Analýza dat
        /// <summary>
        /// Analýza dat
        /// </summary>
        private void _AnalyseItems()
        {
            if (Cancel || !_MapSegment.IsLoaded) return;

            _MapSegment.ResetItems();

            Queue<MapItem> queue = new Queue<MapItem>();
            Dictionary<int, MapItem> workDict = new Dictionary<int, MapItem>();
            _ShowInfoLine($"Začíná analýza...");
            AddToQueue(queue, _MapSegment.FirstItems);
            int round = 0;

            _LogNextRound(ref round, queue.Count);
            while (queue.Count > 0)
            {
                if (Cancel) break;

                var item = queue.Dequeue();
                _AnalyseItem(item, queue, workDict);
                if (queue.Count == 0)
                    _PrepareNextItemsToQueue(queue, workDict, ref round);
                if (queue.Count == 0)
                    _AnalyseNonProcessedItems();
            }
            var lastItems = _MapSegment.LastItems;
            var lastProcItems = lastItems.Where(i => i.Sequence != "??").ToArray();

            _ShowInfoLine($"Analýza {(!Cancel ? "dokončena" : "PŘERUŠENA")}.");
        }
        /// <summary>
        /// Analýza jedné položky a zařazení jejích přímých Next prvků do fronty
        /// </summary>
        /// <param name="currItem"></param>
        /// <param name="queue"></param>
        /// <param name="workDict"></param>
        private void _AnalyseItem(MapItem currItem, Queue<MapItem> queue, Dictionary<int, MapItem> workDict)
        {
            if (Cancel) return;

            _AnalyseStepCount++;
            this.StatusInfo = $"Step {Format(_AnalyseStepCount)}; MapItem: {currItem.TextLong}";

            if (currItem.IsFirst)
                currItem.ResultSequence = 1;

            var nextLinks = currItem.NextLinks;
            if (nextLinks is null || nextLinks.Length == 0) return;

            foreach (var nextLink in nextLinks)
            {   // Nepleťme si nextLink (vztah z currItem do nextItem) právě s prvkem nextItem !!!
                if (nextLink.IsLinkDisconnected) continue;

                var nextItem = nextLink.NextItem;
                bool nextItemIsProcessed = nextItem.AllPrevLinksIsProcessed;   // Výchozí stav: Next prvek má všechny své Prev vztahy zpracované?
                nextLink.IsLinkProcessed = true;                               // Tento řádek může změnit hodnotu nextItem.AllPrevLinksIsProcessed...
                if (!nextItemIsProcessed)                                      // Pokud na začátku neměl Next prvek všechny své Prev vztahy zpracované:
                {
                    if (nextItem.AllPrevLinksIsProcessed)                      //  .. a pokud nyní už je má (byl zpracován poslední Prev vztah pro Next prvek):
                    {
                        nextItem.ResultSequence = nextItem.ExpectedSequence;   //  .. nyní lze určit platnou hodnotu ResultSequence pro Next prvek
                        AddToQueue(queue, nextItem);                           //  .. a dáme prvek přímo do fronty k dalšímu zpracování
                    }
                    else
                        AddToWork(workDict, nextLink.NextItem);                // Prvek Next ještě nemá všechny Prev vztahy vyřešené: dáme ho do řady rozpracovaných prvků.
                }
            }
        }
        /// <summary>
        /// Pokusí se připravit prvky do fronty k dalšímu zpracování ze soupisu rozpracovaných prvků
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="workDict"></param>
        /// <param name="round"></param>
        private void _PrepareNextItemsToQueue(Queue<MapItem> queue, Dictionary<int, MapItem> workDict, ref int round)
        {
            _ShowInfoLine($"Dokončen cyklus {round}; vyřešeno položek: {Format(_MapSegment.ItemsProcessedCount)} / {Format(_MapSegment.ItemsCount)}");

            int workCount = workDict.Count;
            var preparedItems = workDict.Values.Where(i => i.AllPrevLinksIsProcessed).ToList();
            preparedItems.Sort((a, b) => MapItem.CompareByItemId(a, b));

            foreach (var preparedItem in preparedItems)
            {
                workDict.Remove(preparedItem.ItemId);
                if (!preparedItem.IsProcessed)
                    queue.Enqueue(preparedItem);
            }

            // Našli jsme něco k práci v dalším kole?
            int nextCount = queue.Count;
            if (nextCount > 0)
            {
                _ShowInfoLine($"Příprava pro další cyklus {(round + 1)}; nachystáno položek: {Format(workCount)}, do dalšího kola: {Format(preparedItems.Count)}, ke zpracování: {Format(nextCount)}");
                _LogNextRound(ref round, nextCount);
                return;
            }

            // 


                _ShowInfoLine($"Příprava pro další cyklus {(round + 1)}; nachystáno položek: {Format(workCount)}, do dalšího kola: {Format(preparedItems.Count)}, vše je zpracováno.");
        }
        /// <summary>
        /// Závěrečná analýza nezpracovaných prvků
        /// </summary>
        private void _AnalyseNonProcessedItems()
        {
            var nonProcessedItems = _MapSegment.ItemsWaiting;
            int count = nonProcessedItems.Length;
            if (count == 0) return;

            _ShowInfoLine($"Analýza nezpracovaných položek, počet: {Format(count)}");
            for (int i = 0; i < count; i++)
            {
                if (Cancel) break;
                if (i > 500)
                {
                    _ShowInfoLine($" ... a další, celkem {Format(count)}...");
                    break;
                }
                var currentItem = nonProcessedItems[i];
                _ShowInfoLine($"Položka {Format(i)} / {Format(count)}: {currentItem.TextLong}");
            }
        }
        private int _AnalyseStepCount;
        #endregion
        #region Přidávání...
        private void _LogNextRound(ref int round, int count)
        {
            round++;
            _ShowInfoLine($"Start cyklu {round}; počet položek: {Format(count)}");
        }
        /// <summary>
        /// Do fronty <paramref name="queue"/> přidá platné prvky z kolekce <paramref name="items"/>.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="items"></param>
        private void AddToQueue(Queue<MapItem> queue, IEnumerable<MapItem> items)
        {
            if (items is null) return;
            foreach (var item in items)
                AddToQueue(queue, item);
        }
        /// <summary>
        /// Do fronty <paramref name="queue"/> přidá platné prvky z kolekce <paramref name="item"/>.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="item"></param>
        private void AddToQueue(Queue<MapItem> queue, MapItem item)
        {
            if (item != null)
                queue.Enqueue(item);
        }
        /// <summary>
        /// Do seznamu ke zpracování <paramref name="workDict"/> přidá platné prvky z kolekce <paramref name="items"/>, pokud tam dosud nejsou.
        /// </summary>
        /// <param name="workDict"></param>
        /// <param name="items"></param>
        private void AddToWork(Dictionary<int, MapItem> workDict, IEnumerable<MapItem> items)
        {
            if (items is null) return;
            foreach (var item in items)
                AddToWork(workDict, item);
        }
        /// <summary>
        /// Do seznamu ke zpracování <paramref name="workDict"/> přidá platný prvek <paramref name="item"/>, pokud tam dosud není.
        /// </summary>
        /// <param name="workDict"></param>
        /// <param name="item"></param>
        private void AddToWork(Dictionary<int, MapItem> workDict, MapItem item)
        {
            if (item != null && !workDict.ContainsKey(item.ItemId))
                workDict.Add(item.ItemId, item);
        }
        #endregion
    }
}
