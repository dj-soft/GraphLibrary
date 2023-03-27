// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Drawing.Drawing2D;
using DevExpress.Pdf.Native;
using DevExpress.XtraPdfViewer;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.Layout;
using System.Diagnostics;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Extensions metody pro datové třídy
    /// </summary>
    public static class DataExtensions
    {
        #region IEnumerable
        /// <summary>
        /// Do daného Listu přidá dané hodnoty.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="list"></param>
        /// <param name="values"></param>
        public static void AddItems<TValue>(this List<TValue> list, params TValue[] values)
        {
            if (list != null && values != null && values.Length > 0)
                list.AddRange(values);
        }
        /// <summary>
        /// Do daného fronty Queue postupně přidá dané hodnoty.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="queue"></param>
        /// <param name="items"></param>
        public static void Enqueue<TValue>(this Queue<TValue> queue, IEnumerable<TValue> items)
        {
            if (queue != null && items != null)
                items.ForEachExec(i => queue.Enqueue(i));
        }
        /// <summary>
        /// Z dodané kolekce vytvoří Dictionary. Umožní ignorovat duplicity klíčů.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector">Funkce, která vybere klíč ze záznamu</param>
        /// <param name="ignoreDuplicity">Pokud je zadáno true, pak duplicitní klíče budou ignorovány, nebudou způsobovat chybu.</param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, bool ignoreDuplicity = false)
        {
            if (items == null) return null;
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
            foreach (TValue item in items)
            {
                if (item == null) continue;
                TKey key = keySelector(item);
                if (key == null) continue;
                if (result.ContainsKey(key))
                {
                    if (ignoreDuplicity) continue;
                    throw new System.ArgumentException($"An element with the same key [{key}] already exists in the System.Collections.Generic.Dictionary.");
                }
                result.Add(key, item);
            }
            return result;
        }
        /// <summary>
        /// Z dodané kolekce vytvoří Dictionary. Umožní ignorovat duplicity klíčů!
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items">Záznamy</param>
        /// <param name="keySelector">Funkce, která vybere klíč ze záznamu</param>
        /// <param name="valueSelector">Funkce, která vybere hodnotu ze záznamu</param>
        /// <param name="ignoreDuplicity">Pokud je zadáno true, pak duplicitní klíče budou ignorovány, nebudou způsobovat chybu.</param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> CreateDictionary<TItem, TKey, TValue>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector, bool ignoreDuplicity = false)
        {
            if (items == null) return null;
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
            foreach (TItem item in items)
            {
                if (item == null) continue;
                TKey key = keySelector(item);
                if (key == null) continue;
                if (result.ContainsKey(key))
                {
                    if (ignoreDuplicity) continue;
                    throw new System.ArgumentException($"An element with the same key [{key}] already exists in the System.Collections.Generic.Dictionary.");
                }
                TValue value = valueSelector(item);
                result.Add(key, value);
            }
            return result;
        }
        /// <summary>
        /// Pro každý prvek this kolekce provede danou akci
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="action"></param>
        public static void ForEachExec<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (items == null || action == null) return;
            foreach (T item in items)
                action(item);
        }
        /// <summary>
        /// Sloučí dané prvky do jednoho stringu.
        /// Mezi prvky vkládá daný oddělovač. Oddělovač nebude vložen před první prvek ani za poslední prvek.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">Kolekce prvků</param>
        /// <param name="delimiter">Oddělovač, default = EOL</param>
        /// <param name="convertor">Funkce, která z prvku typu <typeparamref name="T"/> vrátí string, default = ToString()</param>
        /// <param name="skipNull">Požadavek na ignorování prvků s hodnotou NULL: false (default) = bude uveden prázdný řádek; true = nebude uveden ani prázdný řádek (a do konvertoru <paramref name="convertor"/> nebude posílán NULL prvek)</param>
        public static string ToOneString<T>(this IEnumerable<T> items, string delimiter = "\r\n", Func<T, string> convertor = null, bool skipNull = false)
        {
            StringBuilder sb = new StringBuilder();
            if (items != null)
            {
                bool hasConverter = (convertor != null);
                if (delimiter == null) delimiter = "\r\n";
                bool prefixDelimiter = false;
                foreach (T item in items)
                {
                    if (item == null && skipNull) continue;

                    string text = (hasConverter ? convertor(item) : (item?.ToString() ?? ""));
                    if (prefixDelimiter)
                        sb.Append(delimiter);
                    else
                        prefixDelimiter = true;

                    sb.Append(text);
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// V dané kolekci najde a vrátí prvek, jehož klíč je dán v poli klíčů <paramref name="keys"/>.
        /// Hledá v pořadí zadaných klíčů, nikoli v pořadí prvků v kolekci.
        /// <para/>
        /// Pokud tedy zadané klíče jsou: X, A; a kolekce obsahuje prvky s klíči A až Z, včetně X, pak vrátí prvek s klíčem X (protože klíč X je zadán dříve než A).
        /// Pokud by ale pro zadané klíče X, A kolekce obsahovala prvky s klíči A až D (mimo X), pak bude vrácen prvek s klíčem A (protože prvek s klíčem X v kolekci nebude nalezen).
        /// <para/>
        /// Slouží tedy k nalezení prioritních záznamů, s možností hledání náhradních záznamů pokud prioritní nebudou nalezeny.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static TItem SearchFirst<TItem, TKey>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, params TKey[] keys) where TKey : IEquatable<TKey>
        {
            if (items != null && keys.Length > 0)
            {
                var dictionary = items.CreateDictionary(keySelector, true);
                foreach (var key in keys)
                {
                    if (dictionary.TryGetValue(key, out var value))
                        return value;
                }
            }
            return default;
        }
        /// <summary>
        /// V dané kolekci najde první prvek vyhovující filtru (nebo pokud není dán filtr, pak najde první prvek) a vrátí true = existuje.
        /// Prvek uloží do out parametru <paramref name="found"/>.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items"></param>
        /// <param name="predicate"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        public static bool TryGetFirst<TItem>(this IEnumerable<TItem> items, Func<TItem, bool> predicate, out TItem found)
        {
            found = default;
            bool result = false;
            if (items != null)
            {
                bool hasPredicate = (predicate != null);
                foreach (var item in items)
                {
                    if (!hasPredicate || predicate(item))
                    {
                        found = item;
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// V dané kolekci najde poslední prvek vyhovující filtru (nebo pokud není dán filtr, pak najde první prvek) a vrátí true = existuje.
        /// Prvek uloží do out parametru <paramref name="found"/>.
        /// <para/>
        /// Tato metoda akceptuje na vstupu IList a k enumeraci používá standardní přístup k prvkům pomocí indexeru se sestupným indexem. 
        /// Najde tedy "První vyhovující prvek při procházení od posledního prvku směrem k indexu [0]".
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items"></param>
        /// <param name="predicate"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        public static bool TryGetLast<TItem>(this IList<TItem> items, Func<TItem, bool> predicate, out TItem found)
        {
            found = default;
            bool result = false;
            if (items != null)
            {
                bool hasPredicate = (predicate != null);
                int i = items.Count - 1;
                while (i >= 0)
                {
                    var item = items[i];
                    if (!hasPredicate || predicate(item))
                    {
                        found = item;
                        result = true;
                        break;
                    }
                    i--;
                }
            }
            return result;
        }
        /// <summary>
        /// Metoda vrací true, pokud this kolekce obsahuje duplictní hodnoty.
        /// Metoda vrátí true ihned po nalezení první duplicity, neřeší počet duplicit ani duplicitní záznamy.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static bool ContainsDuplicityKey<TItem, TKey>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector) where TKey : IEquatable<TKey>
        {
            if (items == null) return false;
            Dictionary<TKey, TItem> keys = new Dictionary<TKey, TItem>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (keys.ContainsKey(key)) return true;
                keys.Add(key, item);
            }
            return false;
        }
        /// <summary>
        /// Metoda vrací true, pokud this kolekce obsahuje duplictní hodnoty, nalezené duplicity vyhledává a vrací v out parametru <paramref name="duplicities"/>.
        /// Pokud vrací false, pak <paramref name="duplicities"/> je null.
        /// Pokud vrátí true, pak v <paramref name="duplicities"/> je pole, jehož prvky jsou Tuple, kde Item1 je klíč duplicity a Item2 je pole prvků s tímto duplicitním klíčem.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="duplicities"></param>
        /// <returns></returns>
        public static bool TryFoundDuplicityItems<TItem, TKey>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, out Tuple<TKey, TItem[]>[] duplicities) where TKey : IEquatable<TKey>
        {
            bool hasDuplicity = false;
            duplicities = null;
            if (items == null) return false;

            // V podstatě grupování podle klíče:
            Dictionary<TKey, List<TItem>> keys = new Dictionary<TKey, List<TItem>>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (!keys.TryGetValue(key, out var list))
                {
                    list = new List<TItem>();
                    keys.Add(key, list);
                }
                list.Add(item);
                if (!hasDuplicity && list.Count > 1) hasDuplicity = true;
            }
            if (!hasDuplicity) return false;

            // Máme duplicity, sestavíme pole, jehož prvky budou Tuple, kde Item1 je klíč duplicity a Item2 je pole prvků s tímto duplicitním klíčem:
            duplicities = keys
                .Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => new Tuple<TKey, TItem[]>(kvp.Key, kvp.Value.ToArray()))
                .ToArray();
            return true;
        }
        /// <summary>
        /// Metoda vrátí ten prvek z dané kolekce, který má největší hodnotu klíče. Klíč z daného prvku vybírá dodaný <paramref name="keySelector"/>. 
        /// Vrácený klíč musí být <see cref="IComparable"/>, jinak nelze určovat porovnání větší / menší.
        /// <para/>
        /// Typicky dostane kolekci prvků, z nich jako klíč vybírá např. datum zpracování. Metoda jedenkrát projde celou kolekci, pro každý prvek určí klíč, a střádá prvek s největší hodnotou klíče.
        /// Při shodě (kdy více prvků má stejný klíč) vybere první z nich.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static TItem TopMost<TItem, TKey>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector) where TKey : IComparable
        {
            TItem topItem = default;
            if (items != null)
            {
                bool isEmpty = true;
                TKey topKey = default;                     // Pro první položku se nepoužívá porování s touto hodnotou, takže výchozí hodnota klíče (default) neovlivní výběr TopMost
                foreach (var item in items)
                {
                    TKey key = keySelector(item);
                    if (isEmpty || (keySelector(item).CompareTo(topKey) > 0))               // Tady se řídí podmínka First:TopMost : ( > 0). Kdybych chtěl Last:TopMost, tak je podmínka " >= 0)". Kdybych chtěl LowMost, je podmínka " < 0" ...
                    {   // První prvek beru vždy, další prvky jen když jejich klíč je větší než dosavadní Top klíč:
                        topKey = key;
                        topItem = item;
                    }
                    isEmpty = false;
                }
            }
            return topItem;
        }
        /// <summary>
        /// Metoda vrátí pole, obsahující linearizovaný vstupní strom = tedy situace, 
        /// kde na vstupu je kolekce nodů, a každý node má v některé své property obsaženou kolekci svých Child nodů stejného typu.
        /// Výstupem metody je pole obsahující Item1 = level, kde 0 = Root (vstupní prvky) a hodnoty +1 jsou jeho vlastní Childs.
        /// Prvek Item2 obsahuje konkrétní Node.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items"></param>
        /// <param name="childsSelector"></param>
        /// <returns></returns>
        public static Tuple<int, TItem>[] Flatten<TItem>(this IEnumerable<TItem> items, Func<TItem, IEnumerable<TItem>> childsSelector) where TItem : class
        {
            List<Tuple<int, TItem>> result = new List<Tuple<int, TItem>>();
            AddLevel(items, 0, childsSelector, result);
            return result.ToArray();

            // Lokální funkce, která každý vstupní prvek z 'levelItems' vloží s danou hladinou 'level' do sumarizovaného lineárního výstupu 'levelResults',
            // a ihned po vložení prvku otestuje, zda prvek má Childs (získá je pomocí 'levelChildsSelector'), a pokud Child prvky existují, zajistí jejich zařazení do výstupu pomocí čisté rekurze sebe sama.
            // Zacyklení je hlídáno pomocí kontroly hladiny 'level', která nemá překročit 120 vnoření.
            void AddLevel(IEnumerable<TItem> levelItems, int level, Func<TItem, IEnumerable<TItem>> levelChildsSelector, List<Tuple<int, TItem>> levelResults)
            {
                if (levelItems == null) return;
                foreach (var levelItem in levelItems)
                {
                    levelResults.Add(new Tuple<int, TItem>(level, levelItem));
                    if (levelItem != null && level < 120)
                    {
                        var levelChilds = levelChildsSelector(levelItem);
                        if (levelChilds != null)
                            AddLevel(levelChilds, level + 1, levelChildsSelector, levelResults);
                    }
                }
            }
        }
        /// <summary>
        /// Z dodané kolekce odebere všechny prvky, které vyhoví danému filtru.
        /// Pokud je zadaná akce <paramref name="onRemove"/>, tak tato akce je provedena ihned po odebrání prvku z listu.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items">Seznam prvků</param>
        /// <param name="predicate">Podmínka pro odebrání, nesmí být null</param>
        /// <param name="onRemove">Akce volaná po odebrání ze seznamu, smí být null</param>
        public static void RemoveWhere<TItem>(this System.Collections.IList items, Func<TItem, bool> predicate, Action<TItem> onRemove = null)
        {
            if (items == null || items.Count == 0) return;
            bool hasRemoveAction = (onRemove != null);
            for (int index = 0; index < items.Count; index++)
            {
                object item = items[index];
                if (item is TItem tItem && predicate(tItem))
                {
                    items.RemoveAt(index);
                    if (hasRemoveAction) onRemove(tItem);
                    index--;
                }
            }
        }
        /// <summary>
        /// Z dodané kolekce odebere všechny prvky, které vyhoví danému filtru
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items">Seznam prvků</param>
        /// <param name="predicate">Podmínka pro odebrání, nesmí být null</param>
        /// <param name="onRemove">Akce volaná po odebrání ze seznamu, smí být null</param>
        public static void RemoveWhere<TItem>(this System.Collections.Generic.IList<TItem> items, Func<TItem, bool> predicate, Action<TItem> onRemove = null)
        {
            if (items == null || items.Count == 0) return;
            bool hasRemoveAction = (onRemove != null);
            for (int index = 0; index < items.Count; index++)
            {
                TItem item = items[index];
                if (predicate(item))
                {
                    items.RemoveAt(index);
                    if (hasRemoveAction) onRemove(item);
                    index--;
                }
            }
        }
        /// <summary>
        /// Vrátí dodané prvky (this) v jiném náhodném pořadí (promíchá je jako karty v balíčku).
        /// Lze specifikovat počet promíchání (zvýšit náhodné rozmístění)
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items"></param>
        /// <param name="shuffles"></param>
        /// <returns></returns>
        public static TItem[] Shuffle<TItem>(this System.Collections.Generic.IList<TItem> items, int shuffles = 0)
        {
            if (items is null) return null;
            var result = items.ToArray();
            int length = result.Length;
            if (length < 2) return result;
            Random rand = new Random();
            int loops = (2 + (shuffles <= 1 ? 1 : (shuffles > 100 ? 100 : shuffles))) * length;
            for (int c = 0; c < loops; c++)
            {
                int i0 = rand.Next(length);
                int i1 = rand.Next(length);
                if (i0 == i1) continue;
                var item0 = result[i0];
                var item1 = result[i1];
                result[i0] = item1;
                result[i1] = item0;
            }
            return result;
        }
        /// <summary>
        /// Metoda z dodané kolekce prvků <paramref name="items"/> vytvoří grupy a ty setřídí.
        /// Z každého prvku určí klíč cílové grupy pomocí selectoru <paramref name="groupKeySelector"/>.
        /// Z každého prvku určí hodnotu pro třídění grupy pomocí selectoru <paramref name="sortValueSelector"/>.
        /// Protože hodnotu pro třídění grupy generujeme z každého prvku vstupního pole, pak tyto hodnoty se mohou pro jednotlivé prvky lišit 
        /// (příklad: prvek 1 generuje klíč grupy A a třídící hodnotu 10, prvek 2 generuje klíč grupy A a třídící hodnotu 20).
        /// Proto se třídící hodnota pro grupu agreguje pomocí <paramref name="sortValueAggregator"/>, viz tam.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TGroup"></typeparam>
        /// <typeparam name="TSort"></typeparam>
        /// <param name="items">Prvky</param>
        /// <param name="groupKeySelector">Selector určí hodnotu klíče grupy z prvku</param>
        /// <param name="sortValueSelector">Selector určí hodnotu pro třídění grupy z prvku</param>
        /// <param name="sortValueAggregator">
        /// Agregátor sloučí dvě hodnoty pro třídění grupy, kde parametr 1 je stávající hodnota grupy, parametr 2 je nová hodnota z prvku, výsledek je výsledná hodnota pro třídění grupy.
        /// Může se tak jako třídící hodnota brát Max() z přítomných jednotlivých hodnot.
        /// </param>
        /// <returns></returns>
        public static Tuple<TGroup, TItem[]>[] CreateSortedGroups<TItem, TGroup, TSort>(this IEnumerable<TItem> items, Func<TItem, TGroup> groupKeySelector, Func<TItem, TSort> sortValueSelector, Func<TSort, TSort, TSort> sortValueAggregator)
            where TSort : IComparable
        {
            var valueDict = new Dictionary<TGroup, SortedGroupItem<TSort, TItem>>();

            // 1. Nastřádat dané hodnoty a klíče:
            foreach (var item in items)
            {
                var key = groupKeySelector(item);
                var itemSort = sortValueSelector(item);
                SortedGroupItem<TSort, TItem> groupItems;
                if (valueDict.TryGetValue(key, out groupItems))
                {   // Pro tuto grupu už záznam máme, musíme vyřešit agregaci třídící hodnoty:
                    groupItems.Sort = sortValueAggregator(groupItems.Sort, itemSort);
                }
                else
                {   // Pro tuto grupu dosud záznam nemáme:
                    groupItems = new SortedGroupItem<TSort, TItem>() { Sort = itemSort };
                    valueDict.Add(key, groupItems);
                }
                groupItems.Items.Add(item);
            }
            // Nyní máme v 'valueDict' jednotlivé grupy (klíč v Dictionary 'valueDict'), ke grupě máme data v Dictionary.Values, kde je agregovaná hodnota Sort, a nastřádané (nijak netříděné) prvky dané grupy Items.

            // 2. Vytvoříme List, aby bylo co třídit:
            var valueList = valueDict.Select(kvp => new Tuple<TGroup, TSort, List<TItem>>(kvp.Key, kvp.Value.Sort, kvp.Value.Items)).ToList();
            valueList.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            // Nyní máme setříděný List 'valueList' prvků Tuple, kde Item1 = klíč grupy, Item2 = hodnota třídění, Item3 = List prvků grupy.

            // 3. Vytvoříme finální pole:
            var result = valueList.Select(g => new Tuple<TGroup, TItem[]>(g.Item1, g.Item3.ToArray())).ToArray();
            return result;
        }
        /// <summary>
        /// Pomocná třída pro metodu <see cref="CreateSortedGroups{TItem, TGroup, TSort}(IEnumerable{TItem}, Func{TItem, TGroup}, Func{TItem, TSort}, Func{TSort, TSort, TSort})"/>
        /// </summary>
        /// <typeparam name="TSort"></typeparam>
        /// <typeparam name="TItem"></typeparam>
        private class SortedGroupItem<TSort, TItem>
        {
            public SortedGroupItem()
            {
                Items = new List<TItem>();
            }
            public TSort Sort;
            public List<TItem> Items;
        }
        #endregion
        #region Range
        /// <summary>
        /// Metoda vrátí List obsahující prvky viditelné v daném rozsahu.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="array"></param>
        /// <param name="visibleSelector"></param>
        /// <param name="visibleRange"></param>
        /// <returns></returns>
        public static List<TItem> GetVisibleItems<TItem>(this IEnumerable<TItem> array, Func<TItem, Int32Range> visibleSelector, Int32Range visibleRange)
        {
            if (array is null || visibleSelector is null) return null;                             // Nepředané hodnoty mají důsledek null
            if (visibleRange is null || visibleRange.Size <= 0) return new List<TItem>();          // Prázdné viditelné okno vrátí prázdný List, ale ne null = to je přípustný stav
            List<TItem> result = array
                .Where(i => Int32Range.HasIntersect(visibleSelector(i), visibleRange))
                .ToList();
            return result;
        }
        #endregion
        #region Dictionary
        /// <summary>
        /// Do this Dictionary přidá nebo aktualizuje záznam pro daný klíč.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Store<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null || key == null) return;
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
        /// <summary>
        /// V this Dictionary najde hodnotu pro daný klíč a vrátí ji.
        /// Pokud ji nenajde, pak ji dodaným <paramref name="valueGenerator"/> vytvoří, uloží do Dictionary a vrátí.
        /// Volitelně aplikuje mezivláknový zámek.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="valueGenerator"></param>
        /// <param name="applyLock"></param>
        /// <returns></returns>
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueGenerator, bool applyLock = false)
        {
            TValue value;
            if (dictionary == null || key == null) return default;
            if (dictionary.TryGetValue(key, out value)) return value;

            if (!applyLock)
            {
                value = valueGenerator();
                dictionary.Add(key, value);
                return value;
            }

            lock (dictionary)
            {
                if (!dictionary.TryGetValue(key, out value))
                {
                    value = valueGenerator();
                    dictionary.Add(key, value);
                }
            }
            return value;
        }
        #endregion
        #region Int a bity
        /// <summary>
        /// Vrátí true pokud dané číslo má pouze jeden bit s hodnotou 1.
        /// Vrací tedy true pro čísla: 1, 2, 4, 8, 16, 32, 64, 128...
        /// Vrací false pro čísla: 0, 3, 5, 6, 7, 9, 10, 11, ...
        /// Nepoužívejme pro záporá čísla.
        /// <para/>
        /// Metoda je primárně určena pro ověření, zda (int)hodnota enumu typu [Flags] je základní = jednobitová, anebo zda je kombinovaná z více bitů.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool HasOneBit(this int value)
        {
            int count = 0;
            for (int c = 0; c < 31; c++)
            {
                if ((value & 1) == 1) count++;
                if (count > 1) break;
                value = value >> 1;
            }

            return count == 1;
        }
        /// <summary>
        /// Vrátí true, pokud this hodnota má stejně nastavený daný bit, jako daná old hodnota.
        /// Používá se pro detekci změny v enumech typu Flags.
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static bool HasEqualsBit(this Enum newValue, Enum oldValue, Enum bit)
        {
            bool oldBit = oldValue.HasFlag(bit);
            bool newBit = newValue.HasFlag(bit);
            return (oldBit == newBit);
        }
        #endregion
        #region String
        /// <summary>
        /// Z dodaného textu (this) odebere všechny znaky dodané v poli <paramref name="removeChars"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="removeChars"></param>
        /// <returns></returns>
        public static string RemoveChars(this string text, char[] removeChars)
        {
            if (text == null || text.Length == 0) return text;
            if (removeChars is null || removeChars.Length == 0) return text;
            foreach (char removeChar in removeChars)
            {
                if (text.IndexOf(removeChar) >= 0)
                {
                    text = text.Replace(removeChar.ToString(), "");
                    if (text.Length == 0) break;
                }
            }
            return text;
        }
        #endregion
        #region Align
        /// <summary>
        /// Vrátí danou hodnotu zarovnanou do mezí min, max.
        /// Obě meze jsou včetně, tedy i max: pokud max = 100, pak může být vrácena hodnota 100.
        /// Pokud na vstupu je max menší než min, pak se vrátí min (i když je větší než max), to je chybou zadavatele.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static T Align<T>(this T value, T min, T max) where T : IComparable<T>
        {
            int minmax = min.CompareTo(max);     // Pokud min je větší nebo rovno max, vracím rovnou min:
            if (minmax >= 0) return min;
            int minval = min.CompareTo(value);   // Pokud value je menší než min, vracím min:
            if (minval >= 0) return min;
            int maxval = max.CompareTo(value);   // Pokud value je větší než max, vracím max:
            if (maxval <= 0) return max;
            return value;
        }
        #endregion
        #region IComparable
        /// <summary>
        /// Metoda vrátí true, pokud dodaná hodnota <paramref name="testValue"/> odpovídá některé ze zadaných hodnot.
        /// Jde tedy o náhradu výrazu: "testValue = values[0] || testValue = values[1] || testValue = values[2] || testValue = values[3] ..."
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="testValue"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool IsAnyOf<T>(T testValue, params T[] values) where T : IComparable
        {
            if (values == null || values.Length == 0) return false;
            return (values.Any(v => testValue.CompareTo(v) == 0));
        }
        #endregion
    }
}
