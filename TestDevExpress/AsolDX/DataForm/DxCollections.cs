// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region ChildItems<TItem> : kde Parent = přímo List
    /// <summary>
    /// Třída reprezentující List, jehož jednotlivé prvky (Childs) mají vztah na this instanci jako svého Parenta.
    /// Tento soupis <see cref="ChildItems{TItem}"/> uvedený vztah aktivně udržuje.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class ChildItems<TItem> : IList<TItem>, IEnumerable<TItem>, ISortableList<TItem>
        where TItem : class, IChildOfParent<ChildItems<TItem>>
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ChildItems()
        {
            __List = new List<TItem>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="capacity"></param>
        public ChildItems(int capacity)
        {
            __List = new List<TItem>(capacity);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="items"></param>
        public ChildItems(IEnumerable<TItem> items)
        {
            _SetParents(items);
            __List = new List<TItem>(items);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {this.__List.Count}; ItemType: '{typeof(TItem).FullName}'";
        }
        /// <summary>
        /// List prvků
        /// </summary>
        private List<TItem> __List;
        /// <summary>
        /// Do všech prvků vloží parenta
        /// </summary>
        /// <param name="items"></param>
        private void _SetParents(IEnumerable<TItem> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                        item.Parent = this;
                }
            }
        }
        /// <summary>
        /// Do daného prvku vloží parenta
        /// </summary>
        /// <param name="item"></param>
        private void _SetParent(TItem item)
        {
            if (item != null)
                item.Parent = this;
        }
        /// <summary>
        /// Z daného prvku odebere parenta
        /// </summary>
        /// <param name="item"></param>
        private void _RemoveParent(TItem item)
        {
            if (item != null)
                item.Parent = null;
        }
        #endregion
        #region Přidané funkce a události
        /// <summary>
        /// Přidá dodané prvky
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<TItem> items)
        {
            _SetParents(items);
            __List.AddRange(items);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Přidá dodané prvky
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(params TItem[] items)
        {
            _SetParents(items);
            __List.AddRange(items);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Odebere všechny položky vyhovující dané podmínce
        /// </summary>
        /// <param name="predicate"></param>
        public void RemoveAll(Func<TItem, bool> predicate)
        {
            int count = 0;
            ((IList<TItem>)this.__List).RemoveWhere(predicate, i => { _RemoveParent(i); count++; });         // Bez explicitního přetypování je __List ambiguous mezi generickým a nongenerickým... :-(
            if (count > 0)
                _RunCollectionChanged();
        }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// Při provedení změn na více prvcích (<see cref="AddRange(IEnumerable{TItem})"/>, <see cref="RemoveAll(Func{TItem, bool})"/>) je událost volána jedenkrát, až po dokončení změn.
        /// </summary>
        public event EventHandler CollectionChanged;
        /// <summary>
        /// Metoda volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollectionChanged(EventArgs e) { }
        /// <summary>
        /// Zavolá metody <see cref="OnCollectionChanged(EventArgs)"/> a event <see cref="CollectionChanged"/>.
        /// </summary>
        private void _RunCollectionChanged()
        {
            var args = EventArgs.Empty;
            OnCollectionChanged(args);
            CollectionChanged?.Invoke(this, args);
        }
        #endregion
        #region Interfaces IList, IEnumerable
        /// <summary>
        /// Prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TItem this[int index] { get { return __List[index]; } set { _SetParent(value); __List[index] = value; } }
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count { get { return __List.Count; } }
        /// <summary>
        /// Je ReadOnly
        /// </summary>
        public bool IsReadOnly { get { return false; } }
        /// <summary>
        /// Přidá prvek
        /// </summary>
        /// <param name="item"></param>
        public void Add(TItem item)
        {
            _SetParent(item);
            __List.Add(item);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Smaže celou kolekci
        /// </summary>
        public void Clear()
        {
            int count = __List.Count;
            __List.ForEach(i => _RemoveParent(i));
            __List.Clear();
            if (count > 0) _RunCollectionChanged();
        }
        /// <summary>
        /// Obsahuje daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TItem item)
        {
            return __List.Contains(item);
        }
        /// <summary>
        /// Přidá daný prvek do this kolekce, pokud tam dosud prvek není.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool AddWhenNotContains(TItem item)
        {
            if (Contains(item)) return false;
            Add(item);
            return true;
        }
        /// <summary>
        /// Kopíruje obsah do daného pole
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(TItem[] array, int arrayIndex)
        {
            __List.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Index prvku
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(TItem item)
        {
            return __List.IndexOf(item);
        }
        /// <summary>
        /// Vloží prvek na danou pozici
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, TItem item)
        {
            _SetParent(item);
            __List.Insert(index, item);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Odebere daný prvek
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(TItem item)
        {
            bool isRemoved = __List.Remove(item);
            if (isRemoved)
            {
                _RemoveParent(item);
                _RunCollectionChanged();
            }
            return isRemoved;
        }
        /// <summary>
        /// Odebere prvek z indexu
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            var item = __List[index];
            __List.RemoveAt(index);
            _RemoveParent(item);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Setřídí pole podle <paramref name="comparison"/>
        /// </summary>
        /// <param name="comparison"></param>
        public void Sort(Comparison<TItem> comparison) { __List.Sort(comparison); }
        /// <summary>
        /// Setřídí pole podle <paramref name="comparer"/>
        /// </summary>
        /// <param name="comparer"></param>
        public void Sort(IComparer<TItem> comparer) { __List.Sort(comparer); }
        /// <summary>
        /// Vrátí enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return __List.GetEnumerator();
        }
        /// <summary>
        /// Vrátí enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return __List.GetEnumerator();
        }
        #endregion
    }
    #endregion
    #region ChildItems<TParent, TItem> : kde Parent = zvenku dodaný majitel
    /// <summary>
    /// Třída reprezentující List, jehož jednotlivé prvky (Childs) mají vztah na svého externě dodaného Parenta.
    /// Tento soupis <see cref="ChildItems{TParent, TItem}"/> uvedený vztah aktivně udržuje.
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public class ChildItems<TParent, TItem> : IList<TItem>, IEnumerable<TItem>, ISortableList<TItem>
        where TParent : class
        where TItem : class, IChildOfParent<TParent>
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public ChildItems(TParent parent)
        {
            __Parent = parent;
            __List = new List<TItem>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="capacity"></param>
        public ChildItems(TParent parent, int capacity)
        {
            __Parent = parent;
            __List = new List<TItem>(capacity);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="items"></param>
        public ChildItems(TParent parent, IEnumerable<TItem> items)
        {
            __Parent = parent;
            _SetParents(items);
            __List = new List<TItem>(items);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {this.__List.Count}; ItemType: '{typeof(TItem).FullName}'";
        }
        /// <summary>
        /// Parent, navazujeme jej do prvků
        /// </summary>
        private TParent __Parent;
        /// <summary>
        /// List prvků
        /// </summary>
        private List<TItem> __List;
        /// <summary>
        /// Do všech prvků vloží parenta
        /// </summary>
        /// <param name="items"></param>
        private void _SetParents(IEnumerable<TItem> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                        item.Parent = __Parent;
                }
            }
        }
        /// <summary>
        /// Do daného prvku vloží parenta
        /// </summary>
        /// <param name="item"></param>
        private void _SetParent(TItem item)
        {
            if (item != null)
            {
                item.Parent = __Parent;
                _RunItemAdded(item);
            }
        }
        /// <summary>
        /// Z daného prvku odebere parenta
        /// </summary>
        /// <param name="item"></param>
        private void _RemoveParent(TItem item)
        {
            if (item != null)
            {
                item.Parent = null;
                _RunItemRemoved(item);
            }
        }
        #endregion
        #region Přidané funkce a události
        /// <summary>
        /// Přidá dodané prvky
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<TItem> items)
        {
            _SetParents(items);
            __List.AddRange(items);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Přidá dodané prvky
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(params TItem[] items)
        {
            _SetParents(items);
            __List.AddRange(items);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Odebere všechny položky vyhovující dané podmínce
        /// </summary>
        /// <param name="predicate"></param>
        public void RemoveAll(Func<TItem, bool> predicate)
        {
            int count = 0;
            ((IList<TItem>)this.__List).RemoveWhere(predicate, i => { _RemoveParent(i); count++; });         // Bez explicitního přetypování je __List ambiguous mezi generickým a nongenerickým... :-(
            if (count > 0)
                _RunCollectionChanged();
        }

        /// <summary>
        /// Zavolá metody <see cref="OnCollectionChanged(EventArgs)"/> a event <see cref="CollectionChanged"/>.
        /// </summary>
        private void _RunCollectionChanged()
        {
            var args = EventArgs.Empty;
            OnCollectionChanged(args);
            CollectionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollectionChanged(EventArgs e) { }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// Při provedení změn na více prvcích (<see cref="AddRange(IEnumerable{TItem})"/>, <see cref="RemoveAll(Func{TItem, bool})"/>) je událost volána jedenkrát, až po dokončení změn.
        /// </summary>
        public event EventHandler CollectionChanged;
        /// <summary>
        /// Zavolá metody <see cref="OnItemAdded(TEventArgs{TItem})"/> a event <see cref="ItemAdded"/>.
        /// </summary>
        private void _RunItemAdded(TItem item)
        {
            var args = new TEventArgs<TItem>(item);
            OnItemAdded(args);
            ItemAdded?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná poté, kdy se přidal konkrétní prvek do kolekce (Add).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemAdded(TEventArgs<TItem> e) { }
        /// <summary>
        /// Událost volaná poté, kdy se přidal konkrétní prvek do kolekce (Add).
        /// </summary>
        public event EventHandler<TEventArgs<TItem>> ItemAdded;
        /// <summary>
        /// Zavolá metody <see cref="OnItemRemoved(TEventArgs{TItem})"/> a event <see cref="ItemRemoved"/>.
        /// </summary>
        private void _RunItemRemoved(TItem item)
        {
            var args = new TEventArgs<TItem>(item);

            OnItemRemoved(args);
            ItemRemoved?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná poté, kdy se odebral konkrétní prvek z kolekce (Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemRemoved(TEventArgs<TItem> e) { }
        /// <summary>
        /// Událost volaná poté, kdy se odebral konkrétní prvek z kolekce (Remove).
        /// </summary>
        public event EventHandler<TEventArgs<TItem>> ItemRemoved;
        #endregion
        #region Interfaces IList, IEnumerable
        /// <summary>
        /// Prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TItem this[int index] { get { return __List[index]; } set { _SetParent(value); __List[index] = value; } }
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count { get { return __List.Count; } }
        /// <summary>
        /// Je ReadOnly
        /// </summary>
        public bool IsReadOnly { get { return false; } }
        /// <summary>
        /// Přidá prvek
        /// </summary>
        /// <param name="item"></param>
        public void Add(TItem item)
        {
            _SetParent(item);
            __List.Add(item);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Smaže celou kolekci
        /// </summary>
        public void Clear()
        {
            int count = __List.Count;
            __List.ForEach(i => _RemoveParent(i));
            __List.Clear();
            if (count > 0) _RunCollectionChanged();
        }
        /// <summary>
        /// Obsahuje daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TItem item)
        {
            return __List.Contains(item);
        }
        /// <summary>
        /// Přidá daný prvek do this kolekce, pokud tam dosud prvek není.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool AddWhenNotContains(TItem item)
        {
            if (Contains(item)) return false;
            Add(item);
            return true;
        }
        /// <summary>
        /// Kopíruje obsah do daného pole
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(TItem[] array, int arrayIndex)
        {
            __List.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Index prvku
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(TItem item)
        {
            return __List.IndexOf(item);
        }
        /// <summary>
        /// Vloží prvek na danou pozici
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, TItem item)
        {
            _SetParent(item);
            __List.Insert(index, item);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Odebere daný prvek
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(TItem item)
        {
            bool isRemoved = __List.Remove(item);
            if (isRemoved)
            {
                _RemoveParent(item);
                _RunCollectionChanged();
            }
            return isRemoved;
        }
        /// <summary>
        /// Odebere prvek z indexu
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            var item = __List[index];
            __List.RemoveAt(index);
            _RemoveParent(item);
            _RunCollectionChanged();
        }
        /// <summary>
        /// Setřídí pole podle <paramref name="comparison"/>
        /// </summary>
        /// <param name="comparison"></param>
        public void Sort(Comparison<TItem> comparison) { __List.Sort(comparison); }
        /// <summary>
        /// Setřídí pole podle <paramref name="comparer"/>
        /// </summary>
        /// <param name="comparer"></param>
        public void Sort(IComparer<TItem> comparer) { __List.Sort(comparer); }
        /// <summary>
        /// Vrátí enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return __List.GetEnumerator();
        }
        /// <summary>
        /// Vrátí enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return __List.GetEnumerator();
        }
        #endregion
    }
    #endregion
    #region ChildDictionary<TParent, TKey, TValue> : Dictionary s vyhledáváním podle klíče, s prvky které mají vztah na Parenta
    /// <summary>
    /// <see cref="ChildDictionary{TParent, TKey, TValue}"/> : Dictionary s vyhledáváním podle klíče, s prvky které mají vztah na Parenta. 
    /// Generuje event o změně kolekce.
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ChildDictionary<TParent, TKey, TValue> : IDictionary<TKey, TValue>, IEnumerable<TValue>
        where TParent : class
        where TValue : class, IChildOfParent<TParent>
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public ChildDictionary(TParent parent)
        {
            __Parent = parent;
            __Dictionary = new Dictionary<TKey, TValue>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="capacity"></param>
        public ChildDictionary(TParent parent, int capacity)
        {
            __Parent = parent;
            __Dictionary = new Dictionary<TKey, TValue>(capacity);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="comparer"></param>
        public ChildDictionary(TParent parent, IEqualityComparer<TKey> comparer)
        {
            __Parent = parent;
            __Dictionary = new Dictionary<TKey, TValue>(comparer);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="dictionary"></param>
        public ChildDictionary(TParent parent, IDictionary<TKey, TValue> dictionary)
        {
            __Parent = parent;
            __Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public ChildDictionary(TParent parent, int capacity, IEqualityComparer<TKey> comparer)
        {
            __Parent = parent;
            __Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="dictionary"></param>
        /// <param name="comparer"></param>
        public ChildDictionary(TParent parent, IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            __Parent = parent;
            __Dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {this.__Dictionary.Count}; KeyType: '{typeof(TKey).FullName}' ItemType: '{typeof(TValue).FullName}'";
        }
        /// <summary>
        /// Parent, navazujeme jej do prvků
        /// </summary>
        private TParent __Parent;
        /// <summary>
        /// Dictionary prvků
        /// </summary>
        private Dictionary<TKey, TValue> __Dictionary;
        /// <summary>
        /// Do všech prvků vloží parenta
        /// </summary>
        /// <param name="items"></param>
        private void _SetParents(IEnumerable<TValue> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                        item.Parent = __Parent;
                }
            }
        }
        /// <summary>
        /// Do daného prvku vloží parenta.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void _SetParent(TKey key, TValue value)
        {
            if (value != null)
            {
                value.Parent = __Parent;
                _RunItemAdded(key, value);
            }
        }
        /// <summary>
        /// Z daných prvků odebere parenta
        /// </summary>
        /// <param name="items"></param>
        private void _RemoveParents(IEnumerable<TValue> items)
        {
            if (items != null)
                items.ForEachExec(i => _RemoveParent(i));
        }
        /// <summary>
        /// Z daného prvku odebere parenta
        /// </summary>
        /// <param name="item"></param>
        private void _RemoveParent(TValue item)
        {
            if (item != null)
            {
                item.Parent = null;
                _RunItemRemoved(item);
            }
        }
        #endregion
        #region Přidané funkce a události
        /// <summary>
        /// Zavolá metody <see cref="OnCollectionChanged(EventArgs)"/> a event <see cref="CollectionChanged"/>.
        /// </summary>
        private void _RunCollectionChanged()
        {
            var args = EventArgs.Empty;
            OnCollectionChanged(args);
            CollectionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná poté, kdy se přidal nebo odebral nějaký prvek do/z kolekce (Add/Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollectionChanged(EventArgs e) { }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal nebo odebral nějaký prvek do/z kolekce (Add/Remove).
        /// </summary>
        public event EventHandler CollectionChanged;
        /// <summary>
        /// Zavolá metody <see cref="OnItemAdded(TEventArgs{Tuple{TKey, TValue}})"/> a event <see cref="ItemAdded"/>.
        /// </summary>
        private void _RunItemAdded(TKey key, TValue value)
        {
            TEventArgs<Tuple<TKey, TValue>> args = new TEventArgs<Tuple<TKey, TValue>>(new Tuple<TKey, TValue>(key, value));
            OnItemAdded(args);
            ItemAdded?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná poté, kdy se přidal konkrétní prvek do kolekce (Add).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemAdded(TEventArgs<Tuple<TKey, TValue>> e) { }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal konkrétní prvek do kolekce (Add).
        /// </summary>
        public event EventHandler<TEventArgs<Tuple<TKey, TValue>>> ItemAdded;
        /// <summary>
        /// Zavolá metody <see cref="OnItemRemoved(TEventArgs{TValue})"/> a event <see cref="ItemRemoved"/>.
        /// </summary>
        private void _RunItemRemoved(TValue item)
        {
            TEventArgs<TValue> args = new TEventArgs<TValue>(item);

            OnItemRemoved(args);
            ItemRemoved?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná poté, kdy se odebral konkrétní prvek z kolekce (Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemRemoved(TEventArgs<TValue> e) { }
        /// <summary>
        /// Obecná událost volaná poté, kdy se odebral konkrétní prvek z kolekce (Remove).
        /// </summary>
        public event EventHandler<TEventArgs<TValue>> ItemRemoved;
        #endregion
        #region Implementace
        public bool ContainsKey(TKey key) 
        {
            return __Dictionary.ContainsKey(key);
        }
        public void Add(TKey key, TValue value)
        {
            _SetParent(key, value);
            __Dictionary.Add(key, value);
            _RunCollectionChanged();
        }
        public bool Remove(TKey key)
        {
            bool isRemoved = false;
            if (__Dictionary.TryGetValue(key, out var value))
            {
                _RemoveParent(value);
                isRemoved = __Dictionary.Remove(key);
                if (isRemoved) _RunCollectionChanged();
            }
            return isRemoved;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            return __Dictionary.TryGetValue(key, out value);
        }
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _SetParent(item.Key, item.Value);
            __Dictionary.Add(item.Key, item.Value);
            _RunCollectionChanged();
        }
        public void Clear()
        {
            if (__Dictionary.Count > 0)
            {
                _RemoveParents(__Dictionary.Values);
                __Dictionary.Clear();
                _RunCollectionChanged();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) { return __Dictionary.ContainsKey(item.Key); }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)__Dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) { return this.Remove(item.Key); }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return __Dictionary.GetEnumerator(); }
        public ICollection<TKey> Keys { get { return __Dictionary.Keys; } }
        public ICollection<TValue> Values { get { return __Dictionary.Values; } }
        public int Count { get { return __Dictionary.Count; } }
        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)__Dictionary).IsReadOnly;

        public TValue this[TKey key] 
        {
            get => ((IDictionary<TKey, TValue>)__Dictionary)[key]; 
            set => ((IDictionary<TKey, TValue>)__Dictionary)[key] = value;
        }

        IEnumerator IEnumerable.GetEnumerator() { return __Dictionary.Values.GetEnumerator(); }
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() { return __Dictionary.Values.GetEnumerator(); }
        #endregion
    }
    #endregion
    #region BiDictionary : Dictionary, kde primárním klíčem je String a sekundárním klíčem je UInt64
    /// <summary>
    /// <see cref="BiDictionary{TValue}"/> : Dictionary, kde primárním klíčem je String a sekundárním klíčem je UInt64. 
    /// Value je generická.
    /// <para/>
    /// Použití: tehdy, když pro vygenerovaný String klíč chceme uložit data, a současně pro ně chceme vytvořit unique UInt64, 
    /// který budeme používat jako lokální klíč pro data.<br/>
    /// Primární klíč String dokážeme vyhledat anebo založit nový záznam.
    /// Při zakládání nového záznamu mu vygenerujeme nový UInt64 klíč.
    /// Tento vygenerovaný klíč následně můžeme uložit v aplikaci a použít jako kratší verzi klíče.
    /// </summary>
    public class BiDictionary<TValue>
    {
        #region Konstruktor, proměnné, potřebné třídy pro uložení dvou klíčů a hodnot, komparátory
        /// <summary>
        /// Konstruktor
        /// </summary>
        public BiDictionary()
        {
            __ComparerKey = new ComparerKey();
            __DictionaryKey = new Dictionary<BiKeyValue, BiKeyValue>(__ComparerKey);
            __ComparerId = new ComparerId();
            __DictionaryId = new Dictionary<BiKeyValue, BiKeyValue>(__ComparerId);
            __LastId = 0UL;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {this.Count}; ValueType: '{typeof(TValue).FullName}'";
        }
        /// <summary>
        /// Instance komparátoru typu <see cref="BiKeyValue"/>, podle <see cref="BiKeyValue.Key"/>
        /// </summary>
        private ComparerKey __ComparerKey;
        /// <summary>
        /// Dictionary, kde klíčem budiž <see cref="BiKeyValue.Key"/>, neboť <see cref="IEqualityComparer{BiKey}"/> je instance třídy <see cref="ComparerKey"/> uložená v <see cref="__ComparerKey"/>
        /// </summary>
        private Dictionary<BiKeyValue, BiKeyValue> __DictionaryKey;
        /// <summary>
        /// Instance komparátoru typu <see cref="BiKeyValue"/>, podle <see cref="BiKeyValue.Id"/>
        /// </summary>
        private ComparerId __ComparerId;
        /// <summary>
        /// Dictionary, kde klíčem budiž <see cref="BiKeyValue.Id"/>, neboť <see cref="IEqualityComparer{BiKey}"/> je instance třídy <see cref="ComparerId"/> uložená v <see cref="__ComparerId"/>
        /// </summary>
        private Dictionary<BiKeyValue, BiKeyValue> __DictionaryId;
        /// <summary>
        /// Posledně přidělený číselný klíč
        /// </summary>
        private ulong __LastId;
        /// <summary>
        /// Třída komparátoru podle <see cref="BiKeyValue.Key"/>.
        /// Dostane Dvojkíč a vrátí HashCode podle jeho stringového klíče, a porovná dva záznamy podle jejich stringových klíčů.
        /// </summary>
        private class ComparerKey : IEqualityComparer<BiKeyValue>
        {
            int IEqualityComparer<BiKeyValue>.GetHashCode(BiKeyValue biKey)
            {
                return biKey?.Key?.GetHashCode() ?? 0;
            }
            bool IEqualityComparer<BiKeyValue>.Equals(BiKeyValue x, BiKeyValue y)
            {
                return String.Equals(x.Key, y.Key);
            }
        }
        /// <summary>
        /// Třída komparátoru podle <see cref="BiKeyValue.Id"/>.
        /// Dostane Dvojkíč a vrátí HashCode podle jeho číselného klíče, a porovná dva záznamy podle jejich číselných klíčů.
        /// </summary>
        private class ComparerId : IEqualityComparer<BiKeyValue>
        {
            int IEqualityComparer<BiKeyValue>.GetHashCode(BiKeyValue biKey)
            {
                return biKey?.Id.GetHashCode() ?? 0;
            }
            bool IEqualityComparer<BiKeyValue>.Equals(BiKeyValue x, BiKeyValue y)
            {
                return x.Id == y.Id;
            }
        }
        /// <summary>
        /// Úložiště dvou klíčů a hodnoty
        /// </summary>
        private class BiKeyValue : KeyValue
        {
            /// <summary>
            /// Simple konstruktor jen pro Key, pro test existence
            /// </summary>
            /// <param name="key"></param>
            /// <param name="id"></param>
            /// <param name="value"></param>
            public BiKeyValue(string key, ulong id, TValue value)
            {
                this.Key = key;
                this.Id = id;
                this.Value = value;
            }
            /// <summary>
            /// Simple konstruktor jen pro Key, pro test existence
            /// </summary>
            /// <param name="key"></param>
            public BiKeyValue(string key)
            {
                this.Key = key;
            }
            /// <summary>
            /// Simple konstruktor jen pro Id, pro test existence
            /// </summary>
            /// <param name="id"></param>
            public BiKeyValue(ulong id)
            {
                this.Id = id;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Key: '{Key}'; Id: {Id}; Value: {Value}";
            }
            /// <summary>
            /// Stringový primární klíč
            /// </summary>
            public readonly string Key;
            /// <summary>
            /// Číselný pracovní klíč
            /// </summary>
            public readonly ulong Id;
            /// <summary>
            /// Uložená data
            /// </summary>
            public TValue Value;
            /// <summary>
            /// Key
            /// </summary>
            string KeyValue.Key { get { return  this.Key; } }
            /// <summary>
            /// Id
            /// </summary>
            TValue KeyValue.Value { get { return this.Value; } }
        }
        #endregion
        #region Přidání a vyhledání a odebrání prvku
        /// <summary>
        /// Vyprázdní vše
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            __DictionaryId.Clear();
            __DictionaryKey.Clear();
            __LastId = 0UL;
        }
        /// <summary>
        /// Metoda přidá (pokud dosud není) anebo přepíše data pro daný klíč (pokud jej již máme), a vrátí ID tohoto klíče (v obou případech).
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ulong Store(string key, TValue value)
        {
            if (key == null) return 0UL;

            if (!__DictionaryKey.TryGetValue(new BiKeyValue(key), out var data))
            {   // Vložit data:
                //  Vygeneruji new Id, vytvořím jednu new instanci BiKeyValue s dodaným Key a nově vygenerovaným Id a dodanou Value;
                //  Tuto jednu instanci BiKeyValue vložím do obou Dictionary jako Key i Value,
                //  ale protože každá Dictionary používá jiný IEqualityComparer, tak bude možno instanci najít jak podle Key, tak podle Id.
                // A po nalezení instance (podle Key nebo Id) bude jako Value získána kompletní sada { Key + Id + Value },
                //  takže nejen že pro Key najdu Value, ale pro Key určím i Id...
                ulong id = ++__LastId;
                data = new BiKeyValue(key, id, value);
                __DictionaryKey.Add(data, data);
                __DictionaryId.Add(data, data);
                return id;
            }
            else
            {   // Přepsat data:
                // Tohle je jeden z důvodů celé konstrukce: pokud najdu instanci 'BiKeyValue data',
                //  pak vím, že tutéž instanci třídy mám uloženou jak v __DictionaryKey tak i v __DictionaryId;
                //  a přepsáním Value v instanci získané z __DictionaryKey se tatáž změna promítne i do instance __DictionaryId:
                // Přitom neměním Key ani Id, takže Dictionary jsou konzistentní:
                data.Value = value;
                return data.Id;
            }
        }
        /// <summary>
        /// Obsahuje daný klíč?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) { return key != null &&  __DictionaryKey.ContainsKey(new BiKeyValue(key)); }
        /// <summary>
        /// Obsahuje daný Id?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsId(ulong id) { return id > 0UL && __DictionaryId.ContainsKey(new BiKeyValue(id)); }
        /// <summary>
        /// Najdeme data pro daný klíč?
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out TValue value)
        {
            if (key != null && __DictionaryKey.TryGetValue(new BiKeyValue(key), out var data))
            {
                value = data.Value;
                return true;
            }
            value = default;
            return false;
        }
        /// <summary>
        /// Najdeme data pro daný klíč?
        /// Pokud ano, pak do out <paramref name="id"/> předáme i jejich Id.
        /// Pokud nenajdeme, pak do out <paramref name="id"/> vložíme 0UL (a výstup bude false).
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out TValue value, out ulong id)
        {
            if (key != null && __DictionaryKey.TryGetValue(new BiKeyValue(key), out var data))
            {
                value = data.Value;
                id = data.Id;
                return true;
            }
            value = default;
            id = 0UL;
            return false;
        }
        /// <summary>
        /// Najdeme data pro daný Id?
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(ulong id, out TValue value)
        {
            if (id > 0UL && __DictionaryId.TryGetValue(new BiKeyValue(id), out var data))
            {
                value = data.Value;
                return true;
            }
            value = default;
            return false;
        }
        /// <summary>
        /// Odebere prvek daného klíče
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            if (key == null) return false;

            bool result = false;
            var biKey = new BiKeyValue(key);
            if (__DictionaryKey.TryGetValue(biKey, out var data))    // biKey obshauje jen Key, __DictionaryKey pracuje s klíčem podle Key
            {
                __DictionaryKey.Remove(biKey);                       // Odebereme Key z __DictionaryKey;
                if (__DictionaryId.ContainsKey(data))                // data obsahují nejen Key, ale i Id (a Value)
                    __DictionaryId.Remove(data);                     // Odebereme z __DictionaryId záznam podle klíče Id, který je obsažen v 'data'
                result = true;
            }
            return result;
        }
        /// <summary>
        /// Odebere prvek daného Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Remove(ulong id)
        {
            if (id == 0UL) return false;

            bool result = false;
            var biId = new BiKeyValue(id);
            if (__DictionaryId.TryGetValue(biId, out var data))      // biId obshauje jen Id, __DictionaryId pracuje s klíčem podle Id
            {
                __DictionaryId.Remove(biId);                         // Odebereme Id z __DictionaryId;
                if (__DictionaryKey.ContainsKey(data))               // data obsahují nejen Id, ale i Key (a Value)
                    __DictionaryKey.Remove(data);                    // Odebereme z __DictionaryKey záznam podle klíče Key, který je obsažen v 'data'
                result = true;
            }
            return result;
        }
        #endregion
        #region Indexery
        /// <summary>
        /// Vrátí nebo uloží hodnotu pro daný klíč.
        /// Pokud hodnota neexistuje, vrátí default pro <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[string key]
        {
            get { this.TryGetValue(key, out var value); return value; }
            set { this.Store(key, value); }
        }
        /// <summary>
        /// Vrátí hodnotu pro daný Id.
        /// Pokud hodnota neexistuje, vrátí default pro <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TValue this[ulong id]
        {
            get { this.TryGetValue(id, out var value); return value; }
        }
        #endregion
        #region Kolekce
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count { get { return this.__DictionaryKey.Count; } }
        /// <summary>
        /// Kolekce klíčů
        /// </summary>
        public IEnumerable<String> Keys { get { return this.__DictionaryKey.Values.Select(b => b.Key); } }
        /// <summary>
        /// Kolekce identifikátorů
        /// </summary>
        public IEnumerable<UInt64> Ids { get { return this.__DictionaryKey.Values.Select(b => b.Id); } }
        /// <summary>
        /// Kolekce hodnot
        /// </summary>
        public IEnumerable<TValue> Values { get { return this.__DictionaryKey.Values.Select(b => b.Value); } }
        /// <summary>
        /// Kolekce klíčů a hodnot
        /// </summary>
        public IEnumerable<KeyValue> KeyValues { get { return this.__DictionaryKey.Values; } }
        /// <summary>
        /// Předpis pro typ obsahující klíč a hodnotu
        /// </summary>
        public interface KeyValue
        {
            /// <summary>
            /// Klíč
            /// </summary>
            string Key { get; }
            /// <summary>
            /// Hodnota
            /// </summary>
            TValue Value { get; }
        }
        #endregion
    }
    #endregion
    #region interface IChildOfParent<TParent> a ISortableList<TItem>
    /// <summary>
    /// Předpis pro typ, který eviduje Parenta své instance
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    public interface IChildOfParent<TParent>
    {
        /// <summary>
        /// Parent instance
        /// </summary>
        TParent Parent { get; set; }
    }
    /// <summary>
    /// Předpis pro kolekci, která dokáže setřídit své prvky
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public interface ISortableList<TItem>
    {
        /// <summary>
        /// Setřídí soupis podle <paramref name="comparer"/>
        /// </summary>
        /// <param name="comparer"></param>
        void Sort(IComparer<TItem> comparer);
        /// <summary>
        /// Setřídí soupis podle <paramref name="comparison"/>
        /// </summary>
        /// <param name="comparison"></param>
        void Sort(Comparison<TItem> comparison);
    }
    #endregion
}
