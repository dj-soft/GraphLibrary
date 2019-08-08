using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Data
{
    #region class SynchronizedPair : třída podporující tvorbu synchronizačních párů (Left, Right) podle společného klíče
    /// <summary>
    /// Třída, která podporuje provedení synchronizace dat na základě klíče, dat vlevo a dat vpravo.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SynchronizedPair<TKey, TValue>
    {
        /// <summary>
        /// Vytvoří a vrátí synchronizační pole, které obsahuje klíč (společný pro prvky vlevo i vpravo) a prvky vlevo a vpravo.
        /// </summary>
        /// <param name="keyGenerator">Generátor klíče z položky</param>
        /// <param name="itemsLeft">Položky na levé straně</param>
        /// <param name="itemsRight">Položky na pravé straně</param>
        /// <returns></returns>
        public static SynchronizedPair<TKey, TValue>[] CreateSynchron(Func<TValue, TKey> keyGenerator, IEnumerable<TValue> itemsLeft, IEnumerable<TValue> itemsRight)
        {
            Dictionary<TKey, SynchronizedPair<TKey, TValue>> syncDict = new Dictionary<TKey, SynchronizedPair<TKey, TValue>>();
            FillSyncDict(syncDict, itemsLeft, keyGenerator, SynchronizedSide.Left);
            FillSyncDict(syncDict, itemsRight, keyGenerator, SynchronizedSide.Right);
            return syncDict.Values.ToArray();
        }
        /// <summary>
        /// Naplní Dictionary z prvků na dané straně páru
        /// </summary>
        /// <param name="syncDict"></param>
        /// <param name="items"></param>
        /// <param name="keyGenerator"></param>
        /// <param name="targetSide"></param>
        protected static void FillSyncDict(Dictionary<TKey, SynchronizedPair<TKey, TValue>> syncDict, IEnumerable<TValue> items, Func<TValue, TKey> keyGenerator, SynchronizedSide targetSide)
        {
            if (items == null) return;
            foreach (TValue item in items)
            {
                TKey key = keyGenerator(item);
                var syncItem = syncDict.GetAdd(key, k => new SynchronizedPair<TKey, TValue>(k));
                syncItem.SetItem(targetSide, item);
            }
        }
        /// <summary>
        /// Konstruktor, klíč je povinný
        /// </summary>
        /// <param name="key"></param>
        protected SynchronizedPair(TKey key)
        {
            this.Key = key;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Key: {this.Key}; Left: {(this.HasItemLeft ? this.ItemLeft.ToString() : "NULL")}; Right: {(this.HasItemRight ? this.ItemRight.ToString() : "NULL")}";
        }
        /// <summary>
        /// Vloží prvek na danou stranu. Reaguje pouze na zadání Left nebo Right.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="item"></param>
        protected void SetItem(SynchronizedSide side, TValue item)
        {
            switch (side)
            {
                case SynchronizedSide.Left:
                    this.ItemLeft = item;
                    this.HasItemLeft = true;
                    break;
                case SynchronizedSide.Right:
                    this.ItemRight = item;
                    this.HasItemRight = true;
                    break;
            }
        }
        /// <summary>
        /// Společný klíč
        /// </summary>
        public TKey Key { get; private set; }
        /// <summary>
        /// Prvek vlevo
        /// </summary>
        public TValue ItemLeft { get; private set; }
        /// <summary>
        /// Prvek vpravo
        /// </summary>
        public TValue ItemRight { get; private set; }
        /// <summary>
        /// Existuje prvek vlevo
        /// </summary>
        public bool HasItemLeft { get; private set; }
        /// <summary>
        /// Existuje prvek vpravo
        /// </summary>
        public bool HasItemRight { get; private set; }
        /// <summary>
        /// Existují oba prvky
        /// </summary>
        public bool HasItemsBooth { get { return this.HasItemLeft && this.HasItemLeft; } }
        /// <summary>
        /// Na které straně jsou záznamy? Vlevo, Vpravo nebo Na obou stranách?
        /// </summary>
        public SynchronizedSide ItemOnSide
        {
            get
            {
                bool l = this.HasItemLeft;
                bool r = this.HasItemRight;
                return (l ? (r ? SynchronizedSide.Booth : SynchronizedSide.Left) : (r ? SynchronizedSide.Right : SynchronizedSide.None));
            }
        }
    }
    /// <summary>
    /// Strana, na které se nachází záznamy (vlevo / vpravo / obě / žádná)
    /// </summary>
    public enum SynchronizedSide
    {
        /// <summary>Nikde (nesmysl, co?)</summary>
        None,
        /// <summary>Vlevo</summary>
        Left,
        /// <summary>Vpravo</summary>
        Right,
        /// <summary>Na obou stranách</summary>
        Booth
    }
    #endregion
    #region Extenze tříd, využívající synchronizace pomocí třídy SynchronizedPair<TKey, TValue>
    /// <summary>
    /// Extenze tříd, využívající synchronizace pomocí třídy <see cref="SynchronizedPair{TKey, TValue}"/>
    /// </summary>
    public static class SynchronItemExtensions
    {
        #region Synchronizace
        /// <summary>
        /// Vrátí mergované pole prvků z kolekce this a z dodaných dalších kolekcí, kde mergování probíhá pomocí klíče, který je generován daným generátorem.
        /// Tato metoda se dá použít i bez předání dalších kolekcí k mergování, pak provede jen Dictinct v this kolekci.
        /// Mergované kolekce smí být null.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="collections"></param>
        /// <returns></returns>
        public static TValue[] SyncMerge<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, params IEnumerable<TValue>[] collections)
        {
            return SyncMerge<TKey, TValue>(items, keySelector, false, collections);
        }
        /// <summary>
        /// Vrátí mergované pole prvků z kolekce this a z dodaných dalších kolekcí, kde mergování probíhá pomocí klíče, který je generován daným generátorem.
        /// Tato metoda se dá použít i bez předání dalších kolekcí k mergování, pak provede jen Dictinct v this kolekci.
        /// Mergované kolekce smí být null.
        /// Tato varianta dovoluje řídit, který prvek <typeparamref name="TValue"/> bude předán do výstupu v situaci, když bude stejný klíč obsažen vícekrát: 
        /// <paramref name="mergeNew"/>: false = nechá ten "vlevo" (ten v this kolekci, nebo ten který se najde nejdříve), true = vezme ten nejposlednější.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="mergeNew"></param>
        /// <param name="collections"></param>
        /// <returns></returns>
        public static TValue[] SyncMerge<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, bool mergeNew, params IEnumerable<TValue>[] collections)
        {
            if (keySelector == null)
                throw new ArgumentNullException("keySelector", "Method <keySelector> given to SyncMerge() cannot be null.");

            Dictionary<TKey, TValue> syncDict = new Dictionary<TKey, TValue>();
            _SyncMerge(syncDict, items, keySelector, mergeNew);
            if (collections != null)
            {
                foreach (IEnumerable<TValue> collection in collections)
                    _SyncMerge(syncDict, collection, keySelector, mergeNew);
            }
            return syncDict.Values.ToArray();
        }
        /// <summary>
        /// Vrátí pole prvků, které nejsou v this kolekci a jsou jen v dodané kolekci <paramref name="newItems"/>.
        /// Pro určení shody používá klíč, který z prvku získá dodaný <paramref name="keySelector"/>.
        /// Mějme kolekci this obsahující záznamy { 1,2,3,4,5 } a kolekci <paramref name="newItems"/> obsahující záznamy { 4,5,6,7 }, pak výsledek bude kolekce { 6,7 } 
        /// = tedy to, co je v kolekci <paramref name="newItems"/> navíc nad rámec this.
        /// Vrácená kolekce obsahuje prvky ze seznamu newItems.
        /// Kolekce <paramref name="newItems"/> nesmí být null.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="newItems"></param>
        /// <returns></returns>
        public static TValue[] SyncOnlyNew<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, IEnumerable<TValue> newItems)
        {
            SynchronizedPair<TKey, TValue>[] syncItems = SynchronizedPair<TKey, TValue>.CreateSynchron(keySelector, items, newItems);
            return syncItems
                .Where(i => i.ItemOnSide == SynchronizedSide.Right)
                .Select(i => i.ItemRight)
                .ToArray();
        }
        /// <summary>
        /// Vrátí pole prvků, které jsou v this kolekci a současně jsou v dodané kolekci <paramref name="newItems"/>.
        /// Pro určení shody používá klíč, který z prvku získá dodaný <paramref name="keySelector"/>.
        /// Mějme kolekci this obsahující záznamy { 1,2,3,4,5 } a kolekci <paramref name="newItems"/> obsahující záznamy { 4,5,6,7 }, pak výsledek bude kolekce { 4,5 } 
        /// = tedy to, co je v kolekci this a přitom je v <paramref name="newItems"/>.
        /// Vrácená kolekce obsahuje prvky ze seznamu this.
        /// Kolekce <paramref name="newItems"/> smí být null, pak se vrací vstupní kolekce (this) jako new Array.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="newItems"></param>
        /// <returns></returns>
        public static TValue[] SyncOnlyBooth<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, IEnumerable<TValue> newItems)
        {
            if (newItems == null) return items.ToArray();

            SynchronizedPair<TKey, TValue>[] syncItems = SynchronizedPair<TKey, TValue>.CreateSynchron(keySelector, items, newItems);
            return syncItems
                .Where(i => i.ItemOnSide == SynchronizedSide.Booth)
                .Select(i => i.ItemRight)
                .ToArray();
        }
        /// <summary>
        /// Vrátí pole prvků, které jsou pouze v jedné z kolekcí (v this anebo v dodané kolekci <paramref name="newItems"/>), tedy vrátí rozdíly.
        /// Pro určení shody používá klíč, který z prvku získá dodaný <paramref name="keySelector"/>.
        /// Mějme kolekci this obsahující záznamy { 1,2,3,4,5 } a kolekci <paramref name="newItems"/> obsahující záznamy { 4,5,6,7 }, pak výsledek bude kolekce { 4,5 } 
        /// = tedy to, co je v kolekci this a přitom je v <paramref name="newItems"/>.
        /// Vrácená kolekce obsahuje prvky ze seznamu this.
        /// Kolekce <paramref name="newItems"/> smí být null, pak se vrací vstupní kolekce (this) jako new Array.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="newItems"></param>
        /// <returns></returns>
        public static TValue[] SyncDifferent<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, IEnumerable<TValue> newItems)
        {
            if (newItems == null) return items.ToArray();

            SynchronizedPair<TKey, TValue>[] syncItems = SynchronizedPair<TKey, TValue>.CreateSynchron(keySelector, items, newItems);
            return syncItems
               .Where(i => (i.ItemOnSide == SynchronizedSide.Left || i.ItemOnSide == SynchronizedSide.Right))
               .Select(i => (i.ItemOnSide == SynchronizedSide.Left ? i.ItemLeft : i.ItemRight))
               .ToArray();
        }
        /// <summary>
        /// Do dané Dictionary přidá záznamy z dodané kolekce
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="syncDict"></param>
        /// <param name="collection"></param>
        /// <param name="keySelector"></param>
        /// <param name="mergeNew"></param>
        private static void _SyncMerge<TKey, TValue>(Dictionary<TKey, TValue> syncDict, IEnumerable<TValue> collection, Func<TValue, TKey> keySelector, bool mergeNew)
        {
            if (collection == null) return;
            foreach (TValue item in collection)
            {
                TKey key = keySelector(item);
                syncDict.Add(key, item, mergeNew);
            }
        }
        #endregion
    }
    #endregion
}
