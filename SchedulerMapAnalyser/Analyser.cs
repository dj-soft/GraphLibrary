using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerMapAnalyser
{
    public class Analyser
    {
        #region Konstrukce a režie
        public Analyser()
        {
            _MapSegment = new MapSegment();
            _ShowInfoText = new StringBuilder();
        }
        public string File { get; set; }
        /// <summary>
        /// Zpracovávat jen prvky, které prošly procesem (dle mapy)
        /// </summary>
        public bool OnlyProcessedItems { get { return _MapSegment.OnlyProcessedItems; } set { _MapSegment.OnlyProcessedItems = value; } }
        /// <summary>
        /// Simuluj zacyklení dat pro daný počet položek. Minimum = 0 = bez zacyklení, maximum = 9
        /// </summary>
        public int CycleSimulation { get { return _MapSegment.CycleSimulation; } set { _MapSegment.CycleSimulation = value; } }
        /// <summary>
        /// Akce, kam se bude posílat text o progresu
        /// </summary>
        public Action<string> ShowInfo { get; set; }
        /// <summary>
        /// Asynchronně aktualizovaný a přebíraný status (analyzer sem kdykoliv píše, ale neinformuje o změně; volající si čas od času text přečte a vloží do status baru)
        /// </summary>
        public string StatusInfo { get; set; }
        /// <summary>
        /// Požadavek na storno procesu, lze nastavit kdykoliv, ve vhodném okamžiku bude akceptováno.
        /// </summary>
        public bool Cancel { get; set; }
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
            string file = this.File;
            _LoadItems(file);
            _SimulateCycling();
            _BreakCycles();
            _AnalyseItems();
            _CancelRun = Cancel;
            // _ShowDone();
            _ClearMemory();
            if (_CancelRun) _ShowInfoLine($"PŘERUŠENO UŽIVATELEM !");
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
            _MapSegment.Clear();
            GC.Collect();
        }
        private string Format(int value)
        {
            return value.ToString("### ### ##0").Trim();
        }
        MapSegment _MapSegment;
        #endregion
        #region Načtení a simulace zacyklení dat
        private void _LoadItems(string file)
        {
            if (String.IsNullOrEmpty(file)) throw new ArgumentNullException("Není zadané jméno souboru.");
            if (!System.IO.File.Exists(file)) throw new ArgumentNullException($"Soubor jména '{file}' neexistuje.");

            if (Cancel) return;

            _ShowInfoLine($"Načítám data ze souboru '{file}'...");
            int rowId = 0;
            foreach (var line in System.IO.File.ReadLines(file))
            {
                if (Cancel) break;
                _MapSegment.AddLine(ref rowId, line);
                StatusInfo = $"Načteno {Format(_MapSegment.ItemsCount)} položek...";
            }
            _ShowInfoLine($"Zpracováno {Format(rowId)} řádků souboru, získáno {Format(_MapSegment.ItemsCount)} položek mapy.");
        }
        /// <summary>
        /// Do dat přidej simulaci zacyklení, pokud je to žádáno
        /// </summary>
        private void _SimulateCycling()
        {
            int cycleSimulation = this.CycleSimulation;
            if (cycleSimulation <= 0) return;
            if (cycleSimulation > 9) cycleSimulation = 9;

            _ShowInfoLine($"Vytvářím zacyklení v datech...");

            var firstItems = _MapSegment.FirstItems;
            var firstCount = firstItems.Length;
            if (firstCount == 0)
            {
                _ShowInfoLine($"Nejsou žádné FirstItems, nebude žádné zacyklení.");
                return;
            }

            // Vytvořím zacyklení:
            List<MapItem> cycleItems = new List<MapItem>();
            int firstIndex = firstCount / 2 - 5;
            if (firstIndex < 0) firstIndex = 0;
            for (int i = 0; i < cycleSimulation; i++)
            {
                int index = firstIndex + i;
                if (index >= firstCount) break;
                var firstItem = firstItems[index];
                bool isCycled = _CycleSimulationOne(firstItem);
                if (!isCycled) cycleSimulation++;                    // Pokud jsem pro aktuální prvek nepřidal zacyklení, tak budu zkoušet o jeden další prvek víc...
            }
        }
        /// <summary>
        /// Přidá zacyklení do stromu vycházejícího z daného First prvku:
        /// </summary>
        /// <param name="firstItem"></param>
        private bool _CycleSimulationOne(MapItem firstItem)
        {
            _ShowInfoLine($"Přidávám zacyklení do stromu k prvku {firstItem}");

            // Zacyklení přidám do druhého prvku (z posledního prvku), to proto abych First item nechal jako First (když bych do něj přidal nějaký Prev prvek, už by nebyl First)
            var nextLinks = firstItem.NextLinks;
            var secondItem = (nextLinks is null || nextLinks.Length == 0 ? null : nextLinks[0].NextItem);
            if (secondItem is null)
            {
                _ShowInfoLine($" - nelze přidat zacyklení, prvek FirstItem je solitér: {firstItem}");
                return false;
            }

            // Najdu vhodný Poslední prvek:
            var allItems = _GetAllItems(secondItem);
            var lastItem = allItems.Where(i => i.IsLast).FirstOrDefault();
            if (lastItem is null)
            {
                _ShowInfoLine($" - nelze přidat zacyklení, nenašli jsme žádný LastItem - asi už tu zacyklení máme: {firstItem}");
                return false;
            }

            // Pokud by Druhá == Poslední (tj. tam kde bych chtěl přidat zacyklovací vztah), ohlásím nemožnost:
            if (secondItem.ItemId == lastItem.ItemId)
            {
                _ShowInfoLine($" - nelze přidat zacyklení, zvolený LastItem == SecondItem (jde o příliš krátký řetězec)");
                return false;
            }

            // Zajistím zacyklení Last => Second:
            secondItem.AddPrevLink(lastItem.ItemId);
            lastItem.AddNextLink(secondItem.ItemId);
            _ShowInfoLine($" - přidáno zacyklení: Last: {lastItem.TextShort} => Second: {secondItem.TextShort}.");


            var es = secondItem.ExpectedSequence;


            return true;
        }
        /// <summary>
        /// Metoda vrátí všechny prvky počínaje z daného prvku včetně ve směru Next. Pokud dojde k zacyklení ve vstupních datech, nezacyklí se.
        /// </summary>
        /// <param name="firstItem"></param>
        /// <returns></returns>
        private MapItem[] _GetAllItems(MapItem firstItem)
        {
            Dictionary<int, MapItem> allDict = new Dictionary<int, MapItem>();
            Queue<MapItem> queue = new Queue<MapItem>();
            queue.Enqueue(firstItem);
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
        }
        #endregion
        #region Přerušení zacyklení položek
        /// <summary>
        /// Vyhledá a ohlásí a přeruší zacyklení
        /// </summary>
        private void _BreakCycles()
        {
            if (Cancel) return;

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
            PathSpiderLine path = new PathSpiderLine(item, ignoreByProducts: true);
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
            public PathSpiderLine(MapItem rootItem, int maxLinks = 0, bool ignoreByProducts = false)
            {
                MaxLinks = maxLinks;
                IgnoreByProducts = ignoreByProducts;
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
            /// Vynechávat ByProducts
            /// </summary>
            private bool IgnoreByProducts;
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
                        // Bude následovat další cyklus na nižším indexu, tam přejdeme na sousendí cestu nebo zase půjdeme dolů...
                    }
                }
                return false;
            }
            private void AddLastNode(MapItem mapItem)
            {
                var node = new PathSpiderNode(mapItem, MaxLinks, IgnoreByProducts);
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
            public PathSpiderNode(MapItem mapItem, int maxLinks = 0, bool ignoreByProducts = false)
            {
                this.MapItem = mapItem;
                this.NextLinks = mapItem.NextLinks;
                if (ignoreByProducts && this.NextLinks != null)
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
            if (Cancel) return;

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

            _ShowInfoLine($"Analýza dokončena.");
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
    #region class MapSegment
    /// <summary>
    /// Segment plánu, obsahuje pole prvků a řídí tvorbu linků mezi prvky a přístup k datům.
    /// Segment neřídí analýzu.
    /// </summary>
    public class MapSegment
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MapSegment()
        {
            this.MapItems = new Dictionary<int, MapItem>();
            this.MapLinks = new Dictionary<long, MapLink>();
            this.StringHeap = new StringHeap();
        }
        /// <summary>
        /// Vloží nový prvek z dodaného řádku mapy.
        /// </summary>
        /// <param name="rowId"></param>
        /// <param name="line"></param>
        public void AddLine(ref int rowId, string line)
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
        /// Zpracovávat jen prvky, které prošly procesem (dle mapy)
        /// </summary>
        public bool OnlyProcessedItems { get; set; }
        /// <summary>
        /// Simuluj zacyklení dat pro daný počet položek. Minimum = 0 = bez zacyklení, maximum = 9
        /// </summary>
        public int CycleSimulation { get; set; }
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
        /// Pole prvních prvků, setřídění podle <see cref="MapItem.CompareByItemId"/>
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
        /// Pole posledních prvků, setřídění podle <see cref="MapItem.CompareByItemId"/>
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
        /// Počet prvků celkem
        /// </summary>
        public int ItemsCount { get { return MapItems.Count; } }
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
            MapItems.Clear();
            MapLinks.Clear();
            StringHeap.Clear();
        }
        #region Vyhledání prvků, vyhledání a tvorba linků
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
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        public MapItem[] GetMapItems(IEnumerable<int> itemIds)
        {
            var items = new List<MapItem>();
            if (itemIds != null)
            {
                foreach (var itemId in itemIds)
                {
                    if (MapItems.TryGetValue(itemId, out var item))
                        items.Add(item);
                }
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
                    mapLink = new MapLink(prevItem, nextItem);
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
                mapLink = new MapLink(prevItem, nextItem);
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
    #endregion
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
        public int RowId { get { return _RowId; } } private int _RowId;
        public int ItemId { get { return _ItemId; } } private int _ItemId;
        public string ItemIdText { get { return MapSegment.ConvertIdToText(_ItemId); } }
        public int OwnerId { get { return _OwnerId; } } private int _OwnerId;
        public string OwnerIdText { get { return MapSegment.ConvertIdToText(_OwnerId); } }
        public string Type { get { return _Segment.StringHeap.GetText(_Type); } } private int _Type;
        public int[] PrevIds { get { return _PrevIds; } } private int[] _PrevIds;
        public int[] NextIds { get { return _NextIds; } } private int[] _NextIds;
        public string Qty { get { return _Segment.StringHeap.GetText(_Qty); } } private int _Qty;
        public string Time { get { return _Segment.StringHeap.GetText(_Time); } } private int _Time;
        public string Sequence { get { return _Segment.StringHeap.GetText(_Sequence); } } private int _Sequence;
        public string Value { get { return _Segment.StringHeap.GetText(_Value); } } private int _Value;
        public string Description1 { get { return _Segment.StringHeap.GetText(_Description1); } } private int _Description1;
        public string Description2 { get { return _Segment.StringHeap.GetText(_Description2); } } private int _Description2;
        public string Description3 { get { return _Segment.StringHeap.GetText(_Description3); } } private int _Description3;
        #endregion
        #region Data prvku podpůrná, komparátory
        private string PrevInfo { get { return (IsFirst ? "IsFirst" : PrevIds.Length.ToString() + " items"); } }
        private string NextInfo { get { return (IsLast ? "IsLast" : NextIds.Length.ToString() + " items"); } }
        public bool IsFirst { get { return this.PrevIds is null; } }
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
        /// Jde o ByProduct?
        /// </summary>
        public bool IsByProduct
        {
            get
            {
                string type = this.Type;
                return type.StartsWith("IncrementByRealByProduct") || type.StartsWith("IncrementByPlanByProduct");
            }
        }
        /// <summary>
        /// Prev vztahy.
        /// Ve vztahu je jako <see cref="MapLink.NextItem"/> prvek this a ve vztahu <see cref="MapLink.PrevItem"/> je prvek vpravo.
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
        private static int SearchExpectedSequenceBlbe(MapItem topItem)
        {
            int sequence = 1;
            Dictionary<int, MapItem> workDict = new Dictionary<int, MapItem>();
            Queue<MapItem> currentQueue = new Queue<MapItem>();
            Queue<MapItem> nextQueue = new Queue<MapItem>();

            workDict.Add(topItem.ItemId, topItem);
            currentQueue.Enqueue(topItem);
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
                            if (workDict.ContainsKey(prevItem.ItemId))
                            {   // Zacyklení!
                                sequence = 0;
                                break;
                            }
                            else
                            {   // Dosud neznámý prvek:
                                workDict.Add(prevItem.ItemId, prevItem);
                                nextQueue.Enqueue(prevItem);
                            }
                        }
                    }
                    if (sequence == 0) break;
                }
                if (currentQueue.Count == 0)
                {
                    if (nextQueue.Count == 0) break;

                    sequence++;
                    currentQueue = nextQueue;
                    nextQueue = new Queue<MapItem>();
                }
            }

            return sequence;
        }
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
    #endregion
    #region class MapLink
    public class MapLink
    {
        public MapLink(MapItem prevItem, MapItem nextItem)
        {
            this.PrevItem = prevItem;
            this.NextItem = nextItem;
        }
        public override string ToString()
        {
            return $"Prev: {PrevItem.TextShort} => Next: {NextItem.TextShort}";
        }
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
}
