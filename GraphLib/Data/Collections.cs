using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Security;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Data
{
    #region CollectIdx<T> kolekce dat s dvojitým indexem podle item.Id a item.Key
    /// <summary>
    /// Kolekce dat s dvojitým indexem podle int item.Id a string item.Key.
    /// Hodnota klíče string Key je case-insensitive.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CollectIdx<T> : IEnumerable<T>, IDisposable where T : IIdKey
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public CollectIdx()
        {
            this.DisableKeyLogic = false;
            this.DataId = new EDictionary<int, T>();
            this.DataKey = new EDictionary<string, T>();
        }
        /// <summary>
        /// Konstruktor.
        /// Umožní zakázat tvorbu indexu podle string Key, když chceme povolit více shodných Key.
        /// Pak nelze podle Key vyhledávat.
        /// </summary>
        /// <param name="disableKeyLogic">Zákaz indexování podle string Key</param>
        public CollectIdx(bool disableKeyLogic)
        {
            this.DisableKeyLogic = disableKeyLogic;
            this.DataId = new EDictionary<int, T>();
            if (!disableKeyLogic)
                this.DataKey = new EDictionary<string, T>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Collection of types:" + typeof(T).Name + "; ItemCount=" + this.DataId.Count + "; KeyCount=" + (DisableKeyLogic ? "disabled" : this.DataKey.Count.ToString());
        }
        /// <summary>
        /// Zákaz indexování podle string Key. Nastavit lze pouze v konstruktoru.
        /// </summary>
        public bool DisableKeyLogic { get; protected set; }
        /// <summary>
        /// Hlavní Dictionary, obsahuje všechny položky, dle int Id
        /// </summary>
        protected EDictionary<int, T> DataId { get; private set; }
        /// <summary>
        /// Vedlejší Dictinary, obsahuje pouze ty položky, které mají string Key.
        /// Používá se jen tehdy, pokud není zakázaná Key logika (this.DisableKeyLogic).
        /// </summary>
        protected EDictionary<string, T> DataKey { get; private set; }
        #endregion
        #region Imitace Dictionary a Listu
        /// <summary>
        /// Počet vět
        /// </summary>
        public int Count { get { return this.DataId.Count; } }
        /// <summary>
        /// Přidá větu
        /// </summary>
        /// <param name="value"></param>
        public void Add(T value)
        {
            this.DataId.Add(value.Id, value);
            string k = FormatThisKey(value.Key);
            if (!String.IsNullOrEmpty(k))
                this.DataKey.Add(k, value);
        }
        /// <summary>
        /// Přidá řadu vět
        /// </summary>
        /// <param name="values"></param>
        public void Add(params T[] values)
        {
            foreach (T value in values)
                this.Add(value);
        }
        /// <summary>
        /// Přidá pole vět
        /// </summary>
        /// <param name="list"></param>
        public void AddRange(IEnumerable<T> list)
        {
            foreach (T value in list)
                this.Add(value);
        }
        /// <summary>
        /// Test, zda kolekce již obsahuje větu (podle klíče Id)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsKey(T value)
        {
            return this.DataId.ContainsKey(value.Id);
        }
        /// <summary>
        /// Test, zda kolekce již obsahuje větu (podle klíče Id)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsKey(int id)
        {
            return this.DataId.ContainsKey(id);
        }
        /// <summary>
        /// Test, zda kolekce již obsahuje větu (podle klíče Key)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            string k = FormatThisKey(key);
            if (k == null) return false;
            return this.DataKey.ContainsKey(k);
        }
        /// <summary>
        /// Zkusí najít větu podle klíče Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(int id, out T value)
        {
            return this.DataId.TryGetValue(id, out value);
        }
        /// <summary>
        /// Zkusí najít větu podle klíče Key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out T value)
        {
            string k = FormatThisKey(key);
            if (k == null)
            {
                value = default(T);
                return false;
            }
            return this.DataKey.TryGetValue(k, out value);
        }
        /// <summary>
        /// Vrátí větu podle klíče Id.
        /// Pokud neexistuje, vyhodí chybu.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T this[int id]
        {
            get { return this.DataId[id]; }
        }
        /// <summary>
        /// Vrátí větu podle klíče Key.
        /// Pokud neexistuje, vyhodí chybu.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T this[string key]
        {
            get
            {
                string k = FormatThisKey(key);
                if (k == null) return default(T);
                return this.DataKey[k];
            }
        }
        /// <summary>
        /// Odebere větu podle klíče Id
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Remove(T value)
        {
            return this.Remove(value.Id);
        }
        /// <summary>
        /// Odebere větu podle klíče Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Remove(int id)
        {
            T value;
            if (this.TryGetValue(id, out value))
            {
                this.DataId.Remove(id);
                if (!String.IsNullOrEmpty(value.Key) && this.DataKey.ContainsKey(value.Key))
                    this.DataKey.Remove(value.Key);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Odebere větu podle klíče Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            string k = FormatThisKey(key);
            T value;
            if (!String.IsNullOrEmpty(k) && this.DataKey.TryGetValue(k, out value))
            {
                this.DataKey.Remove(k);
                if (this.DataId.ContainsKey(value.Id))
                    this.DataId.Remove(value.Id);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Vyprázdní celou kolekci
        /// </summary>
        public void Clear()
        {
            this.DataId.Clear();
            if (!this.DisableKeyLogic)
                this.DataKey.Clear();
        }
        #endregion
        #region Přidané funkce
        /// <summary>
        /// Vrátí seznam položek, netříděný (v pořadí přidávání).
        /// Seznam je nově vytvořený objekt, který není závislý na interních datech CollectIdx.
        /// </summary>
        public List<T> List
        {
            get
            {
                List<T> result = new List<T>(this.DataId.Values);
                return result;
            }
        }
        /// <summary>
        /// Vrátí seznam položek, tříděný dle hodnoty Key
        /// </summary>
        public List<T> SortedList
        {
            get
            {
                List<T> result = new List<T>(this.DataId.Values);
                result.Sort(delegate(T a, T b) { return String.Compare(a.Key, b.Key, true); });
                return result;
            }
        }
        /// <summary>
        /// Formátuje klíč (vrací null, anebo key.Trim().ToLower();)
        /// Vrací null, pokud je zakázaná logika Key (DisableKeyLogic je true).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string FormatThisKey(string key)
        {
            if (this.DisableKeyLogic) return null;
            if (string.IsNullOrEmpty(key)) return null;
            return key.Trim().ToLower();
        }
        /// <summary>
        /// Formátuje klíč (vrací null, anebo key.Trim().ToLower();)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string FormatKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            return key.Trim().ToLower();
        }
        #endregion
        #region IEnumerable<T> + IEnumerable Members. Dovolí snadno enumerovat seznamem.
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.DataId.Values.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.DataId.Values.GetEnumerator();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.Clear();
        }
        void IDisposable.Dispose()
        {
            this.Dispose();
        }
        #endregion
    }
    #endregion
    #region Interface, který garantuje přítomnost property int Id a string Key.
    /// <summary>
    /// Interface, který garantuje přítomnost property int Id a string Key.
    /// Používá se pro objekty, které mohou být ukládány do dvojindexové kolekce CollectIdx.
    /// </summary>
    public interface IIdKey
    {
        /// <summary>
        /// Id prvku, numerické
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Klíč prvku, stringový
        /// </summary>
        string Key { get; }
    }
    #endregion
    #region EDictionary : Dictionary, která poskytuje eventy při změnách
    /// <summary>
    /// EDictionary : adaptér na Dictionary.
    /// Poskytuje eventy při změnách.
    /// Obsahuje neviditelnou cache na poslední nalezenou hodnotu - pro urychlení opakovaného hledání téhož klíče.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class EDictionary<TKey, TValue> : IEnumerable<TValue>, IEnumerable
    {
        #region Konstrukce
        /// <summary>
        /// Úložiště Diuctionary
        /// </summary>
        protected Dictionary<TKey, TValue> Dict;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "EDictionary<" + typeof(TKey).GetType().Name + ", " + typeof(TValue).GetType().Name + ">; ItemCount=" + this.Dict.Count.ToString();
        }
        #endregion
        #region Eventy
        /// <summary>
        /// Event volaný před přidáním nové položky. 
        /// Handler může přidání zakázat. Pak se položka tiše nepřidá. 
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EDictionaryEventBeforeHandler ItemAddBefore;
        /// <summary>
        /// Event volaný po přidání nové položky. 
        /// </summary>
        public event EDictionaryEventAfterHandler ItemAddAfter;
        /// <summary>
        /// Event volaný před vrácením nalezené položky. 
        /// Handler může vrácení položky zakázat. Pak se položka nevrátí, jako by se nenašla. V metodě this[] get dojde k výjimce Key not exists. V metodě TryGetValue se vrátí false.
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EDictionaryEventBeforeHandler ItemGetBefore;
        /// <summary>
        /// Event volaný po vrácení nalezené položky. 
        /// </summary>
        public event EDictionaryEventAfterHandler ItemGetAfter;
        /// <summary>
        /// Event volaný před vložením existující položky v metodě this[] set. 
        /// Handler může vložení zakázat. Pak se položka tiše nevloží. 
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EDictionaryEventBeforeHandler ItemSetBefore;
        /// <summary>
        /// Event volaný po vložení existující položky v metodě this[] set. 
        /// </summary>
        public event EDictionaryEventAfterHandler ItemSetAfter;
        /// <summary>
        /// Event volaný před odebráním existující položky.
        /// Occurs before a item has been removed.
        /// Eventhandler can Cancel this remove process (item then remaining in Dictionary).
        /// If exception is need, then it must thrown eventhandler.
        /// </summary>
        public event EDictionaryEventBeforeHandler ItemRemoveBefore;
        /// <summary>
        /// Event volaný po odebrání existující položky.
        /// </summary>
        public event EDictionaryEventAfterHandler ItemRemoveAfter;
        /// <summary>
        /// Event volaný před změnou existující položky.
        /// Occurs before a item an change is occured.
        /// Eventhandler can Cancel this remove process (item then remaining in Dictionary).
        /// If exception is need, then it must thrown eventhandler.
        /// </summary>
        public event EDictionaryEventBeforeHandler DictionaryChangeBefore;
        /// <summary>
        /// Event volaný po změně existující položky.
        /// </summary>
        public event EDictionaryEventAfterHandler DictionaryChangeAfter;

        /// <summary>
        /// Delegate for eventhandlers for EDictionary events Before-Action (with Cancel property)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void EDictionaryEventBeforeHandler(object sender, EDictionaryBeforeEventArgs args);
        /// <summary>
        /// Delegate for eventhandlers for EDictionary events After-Action (without Cancel property)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void EDictionaryEventAfterHandler(object sender, EDictionaryAfterEventArgs args);
        /// <summary>
        /// Data for events in EList Before class.
        /// </summary>
        public class EDictionaryBeforeEventArgs : EDictionaryEventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            public EDictionaryBeforeEventArgs(CollectionChangeType changeType) : base(changeType) { this.Cancel = false; }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="value"></param>
            public EDictionaryBeforeEventArgs(CollectionChangeType changeType, TValue value) : base(changeType, value) { this.Cancel = false; }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public EDictionaryBeforeEventArgs(CollectionChangeType changeType, TKey key, TValue value) : base(changeType, key, value) { this.Cancel = false; }
            /// <summary>
            /// Eventhandler může zakázat operaci
            /// </summary>
            public bool Cancel { get; set; }
        }
        /// <summary>
        /// Data for events in EList After class.
        /// </summary>
        public class EDictionaryAfterEventArgs : EDictionaryEventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            public EDictionaryAfterEventArgs(CollectionChangeType changeType) : base(changeType) { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="value"></param>
            public EDictionaryAfterEventArgs(CollectionChangeType changeType, TValue value) : base(changeType, value) { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public EDictionaryAfterEventArgs(CollectionChangeType changeType, TKey key, TValue value) : base(changeType, key, value) { }
        }
        /// <summary>
        /// Data for events in EDictionary class
        /// </summary>
        public class EDictionaryEventArgs : EventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            public EDictionaryEventArgs(CollectionChangeType changeType)
            {
                this.ChangeType = changeType;
                this.Key = default(TKey);
                this.Value = default(TValue);
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="value"></param>
            public EDictionaryEventArgs(CollectionChangeType changeType, TValue value)
            {
                this.ChangeType = changeType;
                this.Key = default(TKey);
                this.Value = value;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public EDictionaryEventArgs(CollectionChangeType changeType, TKey key, TValue value)
            {
                this.ChangeType = changeType;
                this.Key = key;
                this.Value = value;
            }
            /// <summary>
            /// Type of change
            /// </summary>
            public CollectionChangeType ChangeType { get; protected set; }
            /// <summary>
            /// Key for data
            /// </summary>
            public TKey Key { get; protected set; }
            /// <summary>
            /// Value for data
            /// </summary>
            public TValue Value { get; protected set; }
        }
        #endregion
        #region Protected podpora eventů
        /// <summary>
        /// Vrací true, pokud lze přidat data
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual bool CanAddItem(TValue value)
        {
            bool result = true;
            if (this.ItemAddBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Add, value);
                this.ItemAddBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Add, value);
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Vrací true, pokud lze přidat data
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual bool CanAddItem(TKey key, TValue value)
        {
            bool result = true;
            if (this.ItemAddBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Add, key, value);
                this.ItemAddBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Add, key, value);
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Háček při přidání hodnoty
        /// </summary>
        /// <param name="value"></param>
        protected virtual void OnAddItemAfter(TValue value)
        {
            if (this.ItemAddAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Add, value);
                this.ItemAddAfter(this, args);
            }
            if (this.DictionaryChangeAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Add, value);
                this.DictionaryChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Háček při přidání hodnoty
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnAddItemAfter(TKey key, TValue value)
        {
            if (this.ItemAddAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Add, key, value);
                this.ItemAddAfter(this, args);
            }
            if (this.DictionaryChangeAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Add, key, value);
                this.DictionaryChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Zjistí, zda lze získat položku
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual bool CanGetItem(TKey key, TValue value)
        {
            bool result = true;
            if (this.ItemGetBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Add, key, value);
                this.ItemGetBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Háček po získání hodnoty
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnGetItemAfter(TKey key, TValue value)
        {
            if (this.ItemGetAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Add, key, value);
                this.ItemGetAfter(this, args);
            }
        }
        /// <summary>
        /// Zjistí, zda lze vepsat položku
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual bool CanSetItem(TKey key, TValue value)
        {
            bool result = true;
            if (this.ItemSetBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Set, key, value);
                this.ItemSetBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Set, key, value);
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Háček po vepsání hodnoty
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnSetItemAfter(TKey key, TValue value)
        {
            if (this.ItemSetAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Set, key, value);
                this.ItemSetAfter(this, args);
            }
            if (this.DictionaryChangeAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Set, key, value);
                this.DictionaryChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Zjistí, zda lze odebrat položku
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual bool CanRemoveItem(TKey key)
        {
            bool result = true;
            if (this.ItemRemoveBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Remove, key, default(TValue));
                this.ItemRemoveBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Remove, key, default(TValue));
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Zjistí, zda lze odebrat položku
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual bool CanRemoveItem(TKey key, TValue value)
        {
            bool result = true;
            if (this.ItemRemoveBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Remove, key, value);
                this.ItemRemoveBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(CollectionChangeType.Remove, key, value);
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Háček po odebrání hodnoty
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnRemoveItemAfter(TKey key, TValue value)
        {
            if (this.ItemRemoveAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Remove, key, value);
                this.ItemRemoveAfter(this, args);
            }
            if (this.DictionaryChangeAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(CollectionChangeType.Remove, key, value);
                this.DictionaryChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Zjistí, zda lze změnit obsah Dictionary
        /// </summary>
        /// <param name="changeType"></param>
        /// <returns></returns>
        protected virtual bool CanChangeDictionary(CollectionChangeType changeType)
        {
            bool result = true;
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(changeType);
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Zjistí, zda lze změnit obsah Dictionary
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual bool CanChangeDictionary(CollectionChangeType changeType, TKey key, TValue value)
        {
            bool result = true;
            if (this.DictionaryChangeBefore != null)
            {
                EDictionaryBeforeEventArgs args = new EDictionaryBeforeEventArgs(changeType, key, value);
                this.DictionaryChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Háček volaný po změně obsahu Dictionary
        /// </summary>
        /// <param name="changeType"></param>
        protected virtual void OnChangeDictionaryAfter(CollectionChangeType changeType)
        {
            if (this.DictionaryChangeAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(changeType);
                this.DictionaryChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Háček volaný po změně obsahu Dictionary
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnChangeDictionaryAfter(CollectionChangeType changeType, TKey key, TValue value)
        {
            if (this.DictionaryChangeAfter != null)
            {
                EDictionaryAfterEventArgs args = new EDictionaryAfterEventArgs(changeType, key, value);
                this.DictionaryChangeAfter(this, args);
            }
        }
        #endregion
        #region Public Dictionary members (adapter to Dictionary)

        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        /// class that is empty, has the default initial capacity, and uses the default
        /// equality comparer for the key type.
        /// </summary>
        public EDictionary()
        {
            this.Dict = new Dictionary<TKey, TValue>();
            this.LastValueReset();
        }
        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        /// class that contains elements copied from the specified System.Collections.Generic.IDictionary&lt;TKey,TValue&gt;
        /// and uses the default equality comparer for the key type.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">dictionary is null</exception>
        /// <exception cref="System.ArgumentException">dictionary contains one or more duplicate keys</exception>
        public EDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.Dict = new Dictionary<TKey, TValue>(dictionary);
            this.LastValueReset();
        }
        /// <summary>
        ///     Initializes a new instance of the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     class that is empty, has the default initial capacity, and uses the specified
        ///     System.Collections.Generic.IEqualityComparer&lt;T&gt;.
        ///
        /// Parameters:
        ///   comparer:
        ///     The System.Collections.Generic.IEqualityComparer&lt;T&gt; implementation to use
        ///     when comparing keys, or null to use the default System.Collections.Generic.EqualityComparer&lt;T&gt;
        ///     for the type of the key.
        /// </summary>
        public EDictionary(IEqualityComparer<TKey> comparer)
        {
            this.Dict = new Dictionary<TKey, TValue>(comparer);
        }
        /// <summary>
        ///     Initializes a new instance of the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     class that is empty, has the specified initial capacity, and uses the default
        ///     equality comparer for the key type.
        ///
        /// Parameters:
        ///   capacity:
        ///     The initial number of elements that the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     can contain.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     capacity is less than 0.
        /// </summary>
        public EDictionary(int capacity)
        {
            this.Dict = new Dictionary<TKey, TValue>(capacity);
        }
        /// <summary>
        ///     Initializes a new instance of the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     class that contains elements copied from the specified System.Collections.Generic.IDictionary&lt;TKey,TValue&gt;
        ///     and uses the specified System.Collections.Generic.IEqualityComparer&lt;T&gt;.
        ///
        /// Parameters:
        ///   dictionary:
        ///     The System.Collections.Generic.IDictionary&lt;TKey,TValue&gt; whose elements are
        ///     copied to the new System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        ///   comparer:
        ///     The System.Collections.Generic.IEqualityComparer&lt;T&gt; implementation to use
        ///     when comparing keys, or null to use the default System.Collections.Generic.EqualityComparer&lt;T&gt;
        ///     for the type of the key.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     dictionary is null.
        ///
        ///   System.ArgumentException:
        ///     dictionary contains one or more duplicate keys.
        /// </summary>
        public EDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            this.Dict = new Dictionary<TKey, TValue>(dictionary, comparer);
        }
        /// <summary>
        ///     Initializes a new instance of the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     class that is empty, has the specified initial capacity, and uses the specified
        ///     System.Collections.Generic.IEqualityComparer&lt;T&gt;.
        ///
        /// Parameters:
        ///   capacity:
        ///     The initial number of elements that the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     can contain.
        ///
        ///   comparer:
        ///     The System.Collections.Generic.IEqualityComparer&lt;T&gt; implementation to use
        ///     when comparing keys, or null to use the default System.Collections.Generic.EqualityComparer&lt;T&gt;
        ///     for the type of the key.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     capacity is less than 0.
        /// </summary>
        public EDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.Dict = new Dictionary<TKey, TValue>(comparer);
        }
        /// <summary>
        ///     Gets the System.Collections.Generic.IEqualityComparer&lt;T&gt; that is used to
        ///     determine equality of keys for the dictionary.
        ///
        /// Returns:
        ///     The System.Collections.Generic.IEqualityComparer&lt;T&gt; generic interface implementation
        ///     that is used to determine equality of keys for the current System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     and to provide hash values for the keys.
        /// </summary>
        public IEqualityComparer<TKey> Comparer
        { get { return this.Dict.Comparer; } }
        /// <summary>
        ///     Gets the number of key/value pairs contained in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Returns:
        ///     The number of key/value pairs contained in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        /// </summary>
        public int Count
        { get { return this.Dict.Count; } }
        /// <summary>
        ///     Gets a collection containing the keys in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Returns:
        ///     A System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.KeyCollection containing
        ///     the keys in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys
        { get { return this.Dict.Keys; } }
        /// <summary>
        ///     Gets a collection containing the values in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Returns:
        ///     A System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.ValueCollection containing
        ///     the values in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values
        { get { return this.Dict.Values; } }
        /// <summary>
        ///     Gets or sets the value associated with the specified key.
        ///
        /// Parameters:
        ///   key:
        ///     The key of the value to get or set.
        ///
        /// Returns:
        ///     The value associated with the specified key. If the specified key is not
        ///     found, a get operation throws a System.Collections.Generic.KeyNotFoundException,
        ///     and a set operation creates a new element with the specified key.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     key is null.
        ///
        ///   System.Collections.Generic.KeyNotFoundException:
        ///     The property is retrieved and key does not exist in the collection.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                // Cache:
                if (this.LastValueIsForKey(key))
                {
                    if (this.LastFound)
                    {
                        if (this.CanGetItem(key, this.LastValue))
                        {
                            this.OnGetItemAfter(key, this.LastValue);
                            return this.LastValue;
                        }
                        // Key + Value is not allowed:
                        throw new KeyNotFoundException("Key " + key.ToString() + " not allowed in DictionaryEvnt.");
                    }
                    // Not found (in cache):
                    throw new KeyNotFoundException("Key " + key.ToString() + " not found in DictionaryEvnt.");
                }

                // Search:
                TValue value;
                bool found = this.Dict.TryGetValue(key, out value);
                if (found)
                {
                    if (this.CanGetItem(key, value))
                    {   // Found and Enable:
                        this.LastValueSet(key, found, value);
                        this.OnGetItemAfter(key, value);
                        return value;
                    }
                    // Key + Value is not allowed:
                    throw new KeyNotFoundException("Key " + key.ToString() + " not allowed in DictionaryEvnt.");
                }
                else
                {   // Not found:
                    this.LastValueSet(key, found, value);
                    throw new KeyNotFoundException("Key " + key.ToString() + " not found in DictionaryEvnt.");
                }
            }
            set
            {
                if (this.CanAddItem(key, value))
                {
                    this.Dict[key] = value;
                    this.OnAddItemAfter(key, value);
                }
                this.LastValueReset();
            }
        }
        /// <summary>
        ///     Adds the specified key and value to the dictionary.
        ///
        /// Parameters:
        ///   key:
        ///     The key of the element to add.
        ///
        ///   value:
        ///     The value of the element to add. The value can be null for reference types.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     key is null.
        ///
        ///   System.ArgumentException:
        ///     An element with the same key already exists in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            if (this.CanAddItem(key, value))
            {
                this.Dict.Add(key, value);
                this.OnAddItemAfter(key, value);
            }

            if (this.LastValueIsForKey(key))
                this.LastValueReset();
        }
        /// <summary>
        ///     Removes all keys and values from the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        /// </summary>
        public void Clear()
        {
            if (this.CanChangeDictionary(CollectionChangeType.RemoveAll))
            {
                this.Dict.Clear();
                this.OnChangeDictionaryAfter(CollectionChangeType.RemoveAll);
            }
            this.LastValueReset();
        }
        /// <summary>
        ///     Determines whether the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     contains the specified key.
        ///
        /// Parameters:
        ///   key:
        ///     The key to locate in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Returns:
        ///     true if the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt; contains an
        ///     element with the specified key; otherwise, false.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     key is null.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            if (this.LastValueIsForKey(key))
            {   // Cache:
                return this.LastFound;
            }
            TValue value;
            bool found = this.Dict.TryGetValue(key, out value);
            this.LastValueSet(key, found, value);
            return found;
        }
        /// <summary>
        ///     Determines whether the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     contains a specific value.
        ///
        /// Parameters:
        ///   value:
        ///     The value to locate in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///     The value can be null for reference types.
        ///
        /// Returns:
        ///     true if the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt; contains an
        ///     element with the specified value; otherwise, false.
        /// </summary>
        public bool ContainsValue(TValue value)
        {
            return this.Dict.ContainsValue(value);
        }
        /// <summary>
        ///     Returns an enumerator that iterates through the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Returns:
        ///     A System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.Enumerator structure
        ///     for the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        /// </summary>
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return this.Dict.GetEnumerator();
        }
        /// <summary>
        ///     Implements the System.Runtime.Serialization.ISerializable interface and returns
        ///     the data needed to serialize the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     instance.
        ///
        /// Parameters:
        ///   info:
        ///     A System.Runtime.Serialization.SerializationInfo object that contains the
        ///     information required to serialize the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     instance.
        ///
        ///   context:
        ///     A System.Runtime.Serialization.StreamingContext structure that contains the
        ///     source and destination of the serialized stream associated with the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;
        ///     instance.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     info is null.
        /// </summary>
        [SecurityCritical]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            this.Dict.GetObjectData(info, context);
        }
        /// <summary>
        ///     Implements the System.Runtime.Serialization.ISerializable interface and raises
        ///     the deserialization event when the deserialization is complete.
        ///
        /// Parameters:
        ///   sender:
        ///     The source of the deserialization event.
        ///
        /// Exceptions:
        ///   System.Runtime.Serialization.SerializationException:
        ///     The System.Runtime.Serialization.SerializationInfo object associated with
        ///     the current System.Collections.Generic.Dictionary&lt;TKey,TValue&gt; instance is
        ///     invalid.
        /// </summary>
        public virtual void OnDeserialization(object sender)
        { }
        /// <summary>
        ///     Removes the value with the specified key from the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Parameters:
        ///   key:
        ///     The key of the element to remove.
        ///
        /// Returns:
        ///     true if the element is successfully found and removed; otherwise, false.
        ///     This method returns false if key is not found in the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt;.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     key is null.
        /// </summary>
        public bool Remove(TKey key)
        {
            bool removed = false;
            
            TValue value;
            if (this.LastValueIsForKey(key))
                value = this.LastValue;
            else
                this.Dict.TryGetValue(key, out value);

            if (this.CanRemoveItem(key, value))
            {
                if (this.LastValueIsForKey(key))
                    this.LastValueReset();

                removed = this.Dict.Remove(key);
                this.OnRemoveItemAfter(key, value);
            }
            return removed;
        }
        /// <summary>
        ///     Gets the value associated with the specified key.
        ///
        /// Parameters:
        ///   key:
        ///     The key of the value to get.
        ///
        ///   value:
        ///     When this method returns, contains the value associated with the specified
        ///     key, if the key is found; otherwise, the default value for the type of the
        ///     value parameter. This parameter is passed uninitialized.
        ///
        /// Returns:
        ///     true if the System.Collections.Generic.Dictionary&lt;TKey,TValue&gt; contains an
        ///     element with the specified key; otherwise, false.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     key is null.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            // Cache:
            if (this.LastValueIsForKey(key))
            {
                value = this.LastValue;
                if (this.CanGetItem(key, value))
                {
                    this.OnGetItemAfter(key, value);
                    return this.LastFound;
                }
                // Key + Value is not allowed:
                value = default(TValue);
                return false;
            }
            
            // Dictionary:
            bool found = this.Dict.TryGetValue(key, out value);
            if (this.CanGetItem(key, value))
            {
                this.OnGetItemAfter(key, value);
                this.LastValueSet(key, found, value);
                return this.LastFound;
            }
            // Key + Value is not allowed:
            value = default(TValue);
            return false;
        }
        #endregion
        #region Neviditelná cache na LastFind položku
        /// <summary>
        /// Cache: reset (po provedení změny v Dictionary.Key = po metodě Add, Remove, Clear).
        /// </summary>
        protected void LastValueReset()
        {
            this.LastExists = false;
            this.LastKey = default(TKey);
            this.LastFound = false;
            this.LastValue = default(TValue);
        }
        /// <summary>
        /// Cache: data existují, jsou uložena a platná
        /// </summary>
        protected bool LastExists;
        /// <summary>
        /// Cache: Klíč posledně hledané položky. Informace o tom, zda byla nalezena je v this.LastFind, nalezená hodnota je v this.LastValue
        /// </summary>
        protected TKey LastKey;
        /// <summary>
        /// Cache: Výsledek posledního hledání klíče this.LastKey
        /// </summary>
        protected bool LastFound;
        /// <summary>
        /// Cache: Posledně nalezená položka (=data) dle klíče LastKey
        /// </summary>
        protected TValue LastValue;
        /// <summary>
        /// Detekce: hledali jsme posledně danou hodnotu? Neřeším s jakým výsledkem (ten je v this.LastFind, hodnota je v this.LastValue).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected bool LastValueIsForKey(TKey key)
        {
            if (key == null) return false;
            return (this.LastExists && key.Equals(this.LastKey));
        }
        /// <summary>
        /// Detekce: máme v cache nalezenou hodnotu pro hledání daného klíče?
        /// Tzn. posledně jsme hledali tentýž klíč, a data jsme našli. Můžeme vrátit LastValue.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected bool LastValueIsFindForKey(TKey key)
        {
            return (this.LastExists && key.Equals(this.LastKey) && this.LastFound);
        }
        /// <summary>
        /// Cache: uloží si hledaný klíč, výsledek a hodnotu pro příští hledání
        /// </summary>
        /// <param name="key"></param>
        /// <param name="found"></param>
        /// <param name="value"></param>
        protected void LastValueSet(TKey key, bool found, TValue value)
        {
            this.LastExists = true;
            this.LastKey = key;
            this.LastFound = found;
            this.LastValue = value;
        }
        #endregion
        #region IEnumerable, IEnumerable<TValue> Members Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Dict.Values.GetEnumerator();
        }
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return this.Dict.Values.GetEnumerator();
        }
        #endregion
    }
    #endregion
    #region EList : List, který poskytuje eventy při změnách
    /// <summary>
    /// EList : adaptér na List.
    /// Poskytuje eventy při změnách.
    /// Pokud datový typ implementuje interface <see cref="IIdKey"/>, pak indexer může pracovat i s hodnotou string <see cref="IIdKey.Key"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EList<T> : IEnumerable<T>, IEnumerable
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        protected List<T> List;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "EList<" + typeof(T).GetType().NsName() + ">; ItemCount=" + this.List.Count.ToString();
        }
        #endregion
        #region Events
        /// <summary>
        /// Event volaný před přidáním nové položky. 
        /// Handler může přidání zakázat. Pak se položka tiše nepřidá. 
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EListEventBeforeHandler ItemAddBefore;
        /// <summary>
        /// Event volaný po přidání nové položky. 
        /// </summary>
        public event EListEventAfterHandler ItemAddAfter;
        /// <summary>
        /// Event volaný před vrácením nalezené položky. 
        /// Handler může vrácení položky zakázat. Pak se položka nevrátí, jako by se nenašla. V metodě this[] get dojde k výjimce Key not exists.
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EListEventBeforeHandler ItemGetBefore;
        /// <summary>
        /// Event volaný před vložením existující položky v metodě this[] set. 
        /// Handler může vložení zakázat. Pak se položka tiše nevloží. 
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EListEventBeforeHandler ItemSetBefore;
        /// <summary>
        /// Event volaný po vložení položky (this[] = item).
        /// </summary>
        public event EListEventAfterHandler ItemSetAfter;
        /// <summary>
        /// Event volaný před smazáním existující položky. 
        /// Handler může smazání zakázat. Pak se položka tiše nesmaže. 
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EListEventBeforeHandler ItemRemoveBefore;
        /// <summary>
        /// Event volaný po smazání existující položky. 
        /// </summary>
        public event EListEventAfterHandler ItemRemoveAfter;
        /// <summary>
        /// Event volaný před jakoukoli změnou položek (kromě Get).
        /// Tento event je volán i před změnami typu Sort, Reverse, Clear, které nemají svůj speciální handler.
        /// Handler může akci zakázat. Pak se tiše nic nestane. 
        /// Pokud je třeba vyvolat výjimku, pak ji musí vyvolat handler.
        /// </summary>
        public event EListEventBeforeHandler ListChangeBefore;
        /// <summary>
        /// Event volaný po smazání existující položky. 
        /// </summary>
        public event EListEventAfterHandler ListChangeAfter;

        /// <summary>
        /// Delegate for eventhandlers for EList events Before-Action (with Cancel property)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void EListEventBeforeHandler(object sender, EListBeforeEventArgs args);
        /// <summary>
        /// Delegate for eventhandlers for EList events After-Action (without Cancel property)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void EListEventAfterHandler(object sender, EListAfterEventArgs args);
        /// <summary>
        /// Data for events in EList Before class.
        /// </summary>
        public class EListBeforeEventArgs : EListEventArgs 
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            public EListBeforeEventArgs(CollectionChangeType changeType) : base(changeType) { this.Cancel = false; }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="item"></param>
            public EListBeforeEventArgs(CollectionChangeType changeType, T item) : base(changeType, item) { this.Cancel = false; }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="index"></param>
            public EListBeforeEventArgs(CollectionChangeType changeType, Int32? index) : base(changeType, index) { this.Cancel = false; }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="item"></param>
            /// <param name="index"></param>
            public EListBeforeEventArgs(CollectionChangeType changeType, T item, Int32? index) : base(changeType, item, index) { this.Cancel = false; }
            /// <summary>
            /// Eventhandler can Cancel current action
            /// </summary>
            public bool Cancel { get; set; }
        }
        /// <summary>
        /// Data for events in EList After class.
        /// </summary>
        public class EListAfterEventArgs : EListEventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            public EListAfterEventArgs(CollectionChangeType changeType) : base(changeType) { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="item"></param>
            public EListAfterEventArgs(CollectionChangeType changeType, T item) : base(changeType, item) { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="index"></param>
            public EListAfterEventArgs(CollectionChangeType changeType, Int32? index) : base(changeType, index) { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="item"></param>
            /// <param name="index"></param>
            public EListAfterEventArgs(CollectionChangeType changeType, T item, Int32? index) : base(changeType, item, index) { }
        }
        /// <summary>
        /// Data for events in EList class
        /// </summary>
        public class EListEventArgs : EventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            public EListEventArgs(CollectionChangeType changeType)
            {
                this.ChangeType = changeType;
                this.Item = default(T);
                this.Index = null;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="item"></param>
            public EListEventArgs(CollectionChangeType changeType, T item)
            {
                this.ChangeType = changeType;
                this.Item = item;
                this.Index = null;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="index"></param>
            public EListEventArgs(CollectionChangeType changeType, Int32? index)
            {
                this.ChangeType = changeType;
                this.Item = default(T);
                this.Index = index;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="changeType"></param>
            /// <param name="item"></param>
            /// <param name="index"></param>
            public EListEventArgs(CollectionChangeType changeType, T item, Int32? index)
            {
                this.ChangeType = changeType;
                this.Item = item;
                this.Index = index;
            }
            /// <summary>
            /// Typ změny, o kterou se jedná
            /// </summary>
            public CollectionChangeType ChangeType { get; protected set; }
            /// <summary>
            /// Data, o která se jedná
            /// </summary>
            public T Item { get; protected set; }
            /// <summary>
            /// Pozice dat, o kterou se jedná
            /// </summary>
            public Int32? Index { get; protected set; }
        }
        #endregion
        #region Protected podpora eventů
        /// <summary>
        /// Mohu přidat daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual bool CanAddItem(T item)
        {
            bool result = true;
            if (this.ItemAddBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Add, item);
                this.ItemAddBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.ListChangeBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Add, item);
                this.ListChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Mohu přidat daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual bool CanAddItem(T item, Int32? index)
        {
            bool result = true;
            if (this.ItemAddBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Add, item, index);
                this.ItemAddBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.ListChangeBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Add, item, index);
                this.ListChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Po přidání daného prvku
        /// </summary>
        /// <param name="item"></param>
        protected virtual void OnAddItemAfter(T item)
        {
            if (this.ItemAddAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Add, item);
                this.ItemAddAfter(this, args);
            }
            if (this.ListChangeAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Add, item);
                this.ListChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Po přidání daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        protected virtual void OnAddItemAfter(T item, Int32? index)
        {
            if (this.ItemAddAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Add, item, index);
                this.ItemAddAfter(this, args);
            }
            if (this.ListChangeAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Add, item, index);
                this.ListChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Mohu získat (číst) daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual bool CanGetItem(T item, Int32? index)
        {
            bool result = true;
            if (this.ItemGetBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Get, item, index);
                this.ItemGetBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Po načtení daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        protected virtual void OnGetItemAfter(T item, Int32? index)
        {
        }
        /// <summary>
        /// Mohu vložit (zapsat) daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual bool CanSetItem(T item, Int32? index)
        {
            bool result = true;
            if (this.ItemSetBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Set, item, index);
                this.ItemSetBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.ListChangeBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Set, item, index);
                this.ListChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Po zapsání daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        protected virtual void OnSetItemAfter(T item, Int32? index)
        {
            if (this.ItemSetAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Set, item, index);
                this.ItemSetAfter(this, args);
            }
            if (this.ListChangeAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Set, item, index);
                this.ListChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Mohu odebrat (remove) daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual bool CanRemoveItem(T item, Int32? index)
        {
            bool result = true;
            if (this.ItemRemoveBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Remove, item, index);
                this.ItemRemoveBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            if (this.ListChangeBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(CollectionChangeType.Remove, item, index);
                this.ListChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Po odebrání daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        protected virtual void OnRemoveItemAfter(T item, Int32? index)
        {
            if (this.ItemRemoveAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Remove, item, index);
                this.ItemRemoveAfter(this, args);
            }
            if (this.ListChangeAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(CollectionChangeType.Remove, item, index);
                this.ListChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Mohu změnit seznam, daným způsobem?
        /// </summary>
        /// <param name="changeType"></param>
        /// <returns></returns>
        protected virtual bool CanChangeList(CollectionChangeType changeType)
        {
            bool result = true;
            if (this.ListChangeBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(changeType);
                this.ListChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Mohu změnit seznam, daným způsobem?
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual bool CanChangeList(CollectionChangeType changeType, T item, Int32? index)
        {
            bool result = true;
            if (this.ListChangeBefore != null)
            {
                EListBeforeEventArgs args = new EListBeforeEventArgs(changeType, item, index);
                this.ListChangeBefore(this, args);
                if (args.Cancel)
                    result = false;
            }
            return result;
        }
        /// <summary>
        /// Po změně seznamu
        /// </summary>
        /// <param name="changeType"></param>
        protected virtual void OnChangeListAfter(CollectionChangeType changeType)
        {
            if (this.ListChangeAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(changeType);
                this.ListChangeAfter(this, args);
            }
        }
        /// <summary>
        /// Po změně seznamu
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        protected virtual void OnChangeListAfter(CollectionChangeType changeType, T item, Int32? index)
        {
            if (this.ListChangeAfter != null)
            {
                EListAfterEventArgs args = new EListAfterEventArgs(changeType, item, index);
                this.ListChangeAfter(this, args);
            }
        }
        #endregion
        #region Public List members (adapter to List)
        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.List&lt;T&gt; class that is empty and has the default initial capacity.
        /// </summary>
        public EList()
        {
            this.List = new List<T>();
            this.Init();
        }
        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.List&lt;T&gt; class
        /// that contains elements copied from the specified collection and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exceptions>System.ArgumentNullException: collection is null.</exceptions>
        public EList(IEnumerable<T> collection)
        {
            this.List = new List<T>(collection);
            this.Init();
        }
        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.List&lt;T&gt; class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <remarks>Exceptions: System.ArgumentOutOfRangeException: capacity is less than 0.</remarks>
        public EList(int capacity)
        {
            this.List = new List<T>(capacity);
            this.Init();
        }
        /// <summary>
        /// Společná inicializace
        /// </summary>
        protected void Init()
        {
            Type iIdKey = typeof(IIdKey);
            this.TypeOfT = typeof(T);
            this.TypeOfTImplementsIIdKey = (this.TypeOfT.FindInterfaces((t, p) => (t == iIdKey), null).Length > 0);
        }
        /// <summary>
        /// Informace o typu <typeparamref name="T"/>
        /// </summary>
        protected Type TypeOfT { get; private set; }
        /// <summary>
        /// true pokud typ <typeparamref name="T"/> implementuje interface <see cref="IIdKey"/>.
        /// </summary>
        protected bool TypeOfTImplementsIIdKey { get; private set; }
        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity { get { return this.List.Capacity; } set { this.List.Capacity = value; } }
        /// <summary>
        /// Gets the number of elements actually contained in the <see cref="List{T}"/>
        /// </summary>
        public int Count { get { return this.List.Count; } }
        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                T item = this.List[index];
                if (this.CanGetItem(item, index))
                {
                    this.OnGetItemAfter(item, index);
                    return item;
                }
                throw new System.ArgumentOutOfRangeException("Item at index [" + index.ToString() + "] can not be retrieved.");
            }
            set
            {
                if (this.CanSetItem(value, index))
                {
                    this.List[index] = value;
                    this.OnSetItemAfter(value, index);
                }
            }
        }
        /// <summary>
        /// Gets or sets the element at the specified key.
        /// Je použitelné pouze tehdy, když typ <typeparamref name="T"/> implementuje <see cref="IIdKey"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T this[string key]
        {
            get
            {
                if (!this.TypeOfTImplementsIIdKey)
                    throw new InvalidOperationException("Type " + this.TypeOfT.Name + " do not implement interface IIdKey, you can not use indexer EList[string]!");

                int index = this.List.FindIndex(i => ((i as IIdKey).Key == key));
                if (index < 0)
                    throw new System.ArgumentOutOfRangeException("Item with key [" + key + "] does not exists.");

                T item = this.List[index];
                if (this.CanGetItem(item, index))
                {
                    this.OnGetItemAfter(item, index);
                    return item;
                }
                throw new System.ArgumentOutOfRangeException("Item with key [{key}] can not be retrieved.");
            }
        }
        /// <summary>
        /// Adds an object to the end of the System.Collections.Generic.List&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to be added to the end of the System.Collections.Generic.List&lt;T&gt;. The value can be null for reference types.</param>
        public void Add(T item)
        {
            if (this.CanAddItem(item))
            {
                this.List.Add(item);
                this.OnAddItemAfter(item, this.List.Count - 1);
            }
        }
        /// <summary>
        /// Adds the elements of the specified collection to the end of the System.Collections.Generic.List&lt;T&gt;.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the System.Collections.Generic.List&lt;T&gt;. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public void AddRange(IEnumerable<T> collection)
        {
            if (this.CanChangeList(CollectionChangeType.AddRange))
            {
                foreach (T item in collection)
                    this.Add(item);
                this.OnChangeListAfter(CollectionChangeType.AddRange);
            }
        }
        /// <summary>
        /// Returns a read-only System.Collections.Generic.IList&lt;T&gt; wrapper for the current collection.
        /// </summary>
        /// <returns>A System.Collections.ObjectModel.ReadOnlyCollection&lt;T&gt; that acts as a read-only wrapper around the current System.Collections.Generic.List&lt;T&gt;.</returns>
        public ReadOnlyCollection<T> AsReadOnly() { return this.List.AsReadOnly(); }
        /// <summary>
        /// Searches the entire sorted System.Collections.Generic.List&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <returns>
        /// The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
        /// if item is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is
        /// no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
        /// </returns>
        public int BinarySearch(T item) { return this.List.BinarySearch(item); }
        /// <summary>
        /// Searches the entire sorted System.Collections.Generic.List&lt;T&gt; for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing elements.-or-null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.</param>
        /// <returns>
        /// The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
        /// if item is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is
        /// no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
        /// </returns>
        public int BinarySearch(T item, IComparer<T> comparer) { return this.List.BinarySearch(item, comparer); }
        /// <summary>
        /// Searches a range of elements in the sorted System.Collections.Generic.List&lt;T&gt;
        ///     for an element using the specified comparer and returns the zero-based index
        ///     of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count">The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        ///     elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.</param>
        /// <returns></returns>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) { return this.List.BinarySearch(index, count, item, comparer); }
        /// <summary>
        /// Removes all elements from the System.Collections.Generic.List&lt;T&gt;.
        /// For each removed element will call events CanRemoveItem and OnRemoveItemAfter.
        /// Before Clear is called event CanChangeList(Clear), at end is called event OnChangeListAfter(Clear).
        /// </summary>
        public void Clear()
        {
            if (this.CanChangeList(CollectionChangeType.Clear))
            {
                this._RemoveOnRange(0, this.Count, null);  // With all events !!!
                this.List.Clear();                         // paranoia
                this.OnChangeListAfter(CollectionChangeType.Clear);
            }
        }
        /// <summary>
        /// Determines whether an element is in the System.Collections.Generic.List&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value can be null for reference types.</param>
        /// <returns></returns>
        public bool Contains(T item) { return this.List.Contains(item); }
        /// <summary>
        /// Converts the elements in the current System.Collections.Generic.List&lt;T&gt; to another type, and returns a list containing the converted elements.
        /// </summary>
        /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
        /// <param name="converter">A System.Converter&lt;TInput,TOutput&gt; delegate that converts each element from one type to another type.</param>
        /// <returns></returns>
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) { return this.List.ConvertAll(converter); }
        /// <summary>
        /// Copies the entire System.Collections.Generic.List&lt;T&gt; to a compatible one-dimensional array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from System.Collections.Generic.List&lt;T&gt;. The System.Array must have zero-based indexing.</param>
        public void CopyTo(T[] array) { this.List.CopyTo(array); }
        /// <summary>
        /// Copies the entire System.Collections.Generic.List&lt;T&gt; to a compatible one-dimensionalarray, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from System.Collections.Generic.List&lt;T&gt;. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex) { this.List.CopyTo(array, arrayIndex); }
        /// <summary>
        /// Copies a range of elements from the System.Collections.Generic.List&lt;T&gt; to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="index">The zero-based index in the source System.Collections.Generic.List&lt;T&gt; at which copying begins.</param>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from System.Collections.Generic.List&lt;T&gt;. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void CopyTo(int index, T[] array, int arrayIndex, int count) { this.List.CopyTo(index, array, arrayIndex, count); }
        /// <summary>
        /// Determines whether the System.Collections.Generic.List&lt;T&gt; contains elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements to search for.</param>
        /// <returns></returns>
        public bool Exists(Predicate<T> match) { return this.List.Exists(match); }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        /// </summary>
        /// <param name="match"> The System.Predicate&lt;T&gt; delegate that defines the conditions of the element to search for.</param>
        /// <returns></returns>
        public T Find(Predicate<T> match) { return this.List.Find(match); }
        //
        // Summary:
        //     Retrieves all the elements that match the conditions defined by the specified
        //     predicate.
        //
        // Parameters:
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
        //     to search for.
        //
        // Returns:
        //     A System.Collections.Generic.List&lt;T&gt; containing all the elements that match
        //     the conditions defined by the specified predicate, if found; otherwise, an
        //     empty System.Collections.Generic.List&lt;T&gt;.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        public List<T> FindAll(Predicate<T> match) { return this.List.FindAll(match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the zero-based index of the first occurrence within
        //     the entire System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The zero-based index of the first occurrence of an element that matches the
        //     conditions defined by match, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        public int FindIndex(Predicate<T> match) { return this.List.FindIndex(match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the zero-based index of the first occurrence within
        //     the range of elements in the System.Collections.Generic.List&lt;T&gt; that extends
        //     from the specified index to the last element.
        //
        // Parameters:
        //   startIndex:
        //     The zero-based starting index of the search.
        //
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The zero-based index of the first occurrence of an element that matches the
        //     conditions defined by match, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        public int FindIndex(int startIndex, Predicate<T> match) { return this.List.FindIndex(startIndex, match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the zero-based index of the first occurrence within
        //     the range of elements in the System.Collections.Generic.List&lt;T&gt; that starts
        //     at the specified index and contains the specified number of elements.
        //
        // Parameters:
        //   startIndex:
        //     The zero-based starting index of the search.
        //
        //   count:
        //     The number of elements in the section to search.
        //
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The zero-based index of the first occurrence of an element that matches the
        //     conditions defined by match, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        //     is less than 0.-or-startIndex and count do not specify a valid section in
        //     the System.Collections.Generic.List&lt;T&gt;.
        public int FindIndex(int startIndex, int count, Predicate<T> match) { return this.List.FindIndex(startIndex, count, match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the last occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The last element that matches the conditions defined by the specified predicate,
        //     if found; otherwise, the default value for type T.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        public T FindLast(Predicate<T> match) { return this.List.FindLast(match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the zero-based index of the last occurrence within
        //     the entire System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The zero-based index of the last occurrence of an element that matches the
        //     conditions defined by match, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        public int FindLastIndex(Predicate<T> match) { return this.List.FindLastIndex(match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the zero-based index of the last occurrence within
        //     the range of elements in the System.Collections.Generic.List&lt;T&gt; that extends
        //     from the first element to the specified index.
        //
        // Parameters:
        //   startIndex:
        //     The zero-based starting index of the backward search.
        //
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The zero-based index of the last occurrence of an element that matches the
        //     conditions defined by match, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        public int FindLastIndex(int startIndex, Predicate<T> match) { return this.List.FindLastIndex(startIndex, match); }
        //
        // Summary:
        //     Searches for an element that matches the conditions defined by the specified
        //     predicate, and returns the zero-based index of the last occurrence within
        //     the range of elements in the System.Collections.Generic.List&lt;T&gt; that contains
        //     the specified number of elements and ends at the specified index.
        //
        // Parameters:
        //   startIndex:
        //     The zero-based starting index of the backward search.
        //
        //   count:
        //     The number of elements in the section to search.
        //
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        //     to search for.
        //
        // Returns:
        //     The zero-based index of the last occurrence of an element that matches the
        //     conditions defined by match, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        //     is less than 0.-or-startIndex and count do not specify a valid section in
        //     the System.Collections.Generic.List&lt;T&gt;.
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) { return this.List.FindLastIndex(startIndex, count, match); }
        //
        // Summary:
        //     Performs the specified action on each element of the System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   action:
        //     The System.Action&lt;T&gt; delegate to perform on each element of the System.Collections.Generic.List&lt;T&gt;.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     action is null.
        public void ForEach(Action<T> action) { this.List.ForEach(action); }
        //
        // Summary:
        //     Returns an enumerator that iterates through the System.Collections.Generic.List&lt;T&gt;.
        //
        // Returns:
        //     A System.Collections.Generic.List&lt;T&gt;.Enumerator for the System.Collections.Generic.List&lt;T&gt;.
        public List<T>.Enumerator GetEnumerator() { return this.List.GetEnumerator(); }
        //
        // Summary:
        //     Creates a shallow copy of a range of elements in the source System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   index:
        //     The zero-based System.Collections.Generic.List&lt;T&gt; index at which the range
        //     starts.
        //
        //   count:
        //     The number of elements in the range.
        //
        // Returns:
        //     A shallow copy of a range of elements in the source System.Collections.Generic.List&lt;T&gt;.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-count is less than 0.
        //
        //   System.ArgumentException:
        //     index and count do not denote a valid range of elements in the System.Collections.Generic.List&lt;T&gt;.
        public List<T> GetRange(int index, int count) { return this.List.GetRange(index, count); }
        //
        // Summary:
        //     Searches for the specified object and returns the zero-based index of the
        //     first occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        // Returns:
        //     The zero-based index of the first occurrence of item within the entire System.Collections.Generic.List&lt;T&gt;,
        //     if found; otherwise, –1.
        public int IndexOf(T item) { return this.List.IndexOf(item); }
        //
        // Summary:
        //     Searches for the specified object and returns the zero-based index of the
        //     first occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        //     that extends from the specified index to the last element.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        //   index:
        //     The zero-based starting index of the search. 0 (zero) is valid in an empty
        //     list.
        //
        // Returns:
        //     The zero-based index of the first occurrence of item within the range of
        //     elements in the System.Collections.Generic.List&lt;T&gt; that extends from index
        //     to the last element, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        public int IndexOf(T item, int index) { return this.List.IndexOf(item, index); }
        //
        // Summary:
        //     Searches for the specified object and returns the zero-based index of the
        //     first occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        //     that starts at the specified index and contains the specified number of elements.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        //   index:
        //     The zero-based starting index of the search. 0 (zero) is valid in an empty
        //     list.
        //
        //   count:
        //     The number of elements in the section to search.
        //
        // Returns:
        //     The zero-based index of the first occurrence of item within the range of
        //     elements in the System.Collections.Generic.List&lt;T&gt; that starts at index and
        //     contains count number of elements, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        //     is less than 0.-or-index and count do not specify a valid section in the
        //     System.Collections.Generic.List&lt;T&gt;.
        public int IndexOf(T item, int index, int count) { return this.List.IndexOf(item, index, count); }
        //
        // Summary:
        //     Inserts an element into the System.Collections.Generic.List&lt;T&gt; at the specified
        //     index.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which item should be inserted.
        //
        //   item:
        //     The object to insert. The value can be null for reference types.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-index is greater than System.Collections.Generic.List&lt;T&gt;.Count.
        public void Insert(int index, T item)
        {
            if (this.CanAddItem(item, index))
            {
                this.List.Insert(index, item);
                this.OnAddItemAfter(item, index);
            }
        }
        //
        // Summary:
        //     Inserts the elements of a collection into the System.Collections.Generic.List&lt;T&gt;
        //     at the specified index.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which the new elements should be inserted.
        //
        //   collection:
        //     The collection whose elements should be inserted into the System.Collections.Generic.List&lt;T&gt;.
        //     The collection itself cannot be null, but it can contain elements that are
        //     null, if type T is a reference type.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     collection is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-index is greater than System.Collections.Generic.List&lt;T&gt;.Count.
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (this.CanChangeList(CollectionChangeType.AddRange, default(T), index))
            {
                this.List.InsertRange(index, collection);
                this.OnChangeListAfter(CollectionChangeType.AddRange, default(T), index);
            }
        }
        //
        // Summary:
        //     Searches for the specified object and returns the zero-based index of the
        //     last occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        // Returns:
        //     The zero-based index of the last occurrence of item within the entire the
        //     System.Collections.Generic.List&lt;T&gt;, if found; otherwise, –1.
        public int LastIndexOf(T item) { return this.List.LastIndexOf(item); }
        //
        // Summary:
        //     Searches for the specified object and returns the zero-based index of the
        //     last occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        //     that extends from the first element to the specified index.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        //   index:
        //     The zero-based starting index of the backward search.
        //
        // Returns:
        //     The zero-based index of the last occurrence of item within the range of elements
        //     in the System.Collections.Generic.List&lt;T&gt; that extends from the first element
        //     to index, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        public int LastIndexOf(T item, int index) { return this.List.LastIndexOf(item, index); }
        //
        // Summary:
        //     Searches for the specified object and returns the zero-based index of the
        //     last occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        //     that contains the specified number of elements and ends at the specified
        //     index.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        //   index:
        //     The zero-based starting index of the backward search.
        //
        //   count:
        //     The number of elements in the section to search.
        //
        // Returns:
        //     The zero-based index of the last occurrence of item within the range of elements
        //     in the System.Collections.Generic.List&lt;T&gt; that contains count number of elements
        //     and ends at index, if found; otherwise, –1.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        //     is less than 0.-or-index and count do not specify a valid section in the
        //     System.Collections.Generic.List&lt;T&gt;.
        public int LastIndexOf(T item, int index, int count) { return this.List.LastIndexOf(item, index, count); }
        //
        // Summary:
        //     Removes the first occurrence of a specific object from the System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   item:
        //     The object to remove from the System.Collections.Generic.List&lt;T&gt;. The value
        //     can be null for reference types.
        //
        // Returns:
        //     true if item is successfully removed; otherwise, false. This method also
        //     returns false if item was not found in the System.Collections.Generic.List&lt;T&gt;.
        public bool Remove(T item)
        {
            bool result = false;
            if (this.CanRemoveItem(item, null))
            {
                result = this.List.Remove(item);
                this.OnRemoveItemAfter(item, null);
            }
            return result;
        }
        //
        // Summary:
        //     Removes the all the elements that match the conditions defined by the specified
        //     predicate.
        //
        // Parameters:
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
        //     to remove.
        //
        // Returns:
        //     The number of elements removed from the System.Collections.Generic.List&lt;T&gt;
        //     .
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        public int RemoveAll(Predicate<T> match)
        {
            int result = -1;
            if (this.CanChangeList(CollectionChangeType.RemoveAll))
            {
                this._RemoveOnRange(0, this.Count, match);
                this.OnChangeListAfter(CollectionChangeType.RemoveAll);
            }
            return result;
        }
        //
        // Summary:
        //     Removes the element at the specified index of the System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the element to remove.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-index is equal to or greater than System.Collections.Generic.List&lt;T&gt;.Count.
        public void RemoveAt(int index)
        {
            this._RemoveOnRange(index, 1, null);
        }
        //
        // Summary:
        //     Removes a range of elements from the System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   index:
        //     The zero-based starting index of the range of elements to remove.
        //
        //   count:
        //     The number of elements to remove.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-count is less than 0.
        //
        //   System.ArgumentException:
        //     index and count do not denote a valid range of elements in the System.Collections.Generic.List&lt;T&gt;.
        public void RemoveRange(int index, int count)
        {
            if (this.CanChangeList(CollectionChangeType.RemoveAll))
            {
                this._RemoveOnRange(index, count, null);
                this.OnChangeListAfter(CollectionChangeType.RemoveAll);
            }
        }
        //
        // Summary:
        //     Removes a range of elements from the System.Collections.Generic.List&lt;T&gt;.
        //
        // Parameters:
        //   index:
        //     The zero-based starting index of the range of elements to remove.
        //
        //   count:
        //     The number of elements to remove.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-count is less than 0.
        //
        //   System.ArgumentException:
        //     index and count do not denote a valid range of elements in the System.Collections.Generic.List&lt;T&gt;.
        public void RemoveRange(int index, int count, Predicate<T> match)
        {
            if (this.CanChangeList(CollectionChangeType.RemoveAll))
            {
                this._RemoveOnRange(index, count, match);
                this.OnChangeListAfter(CollectionChangeType.RemoveAll);
            }
        }
        /// <summary>
        /// Remove items at specified indexes (index + count), with match predicate.
        /// Remove items in reverse order (from last position to first = index).
        /// For each removed item call events CanRemoveItem and OnRemoveItemAfter.
        /// </summary>
        /// <param name="index">Index of first item to remove, base 0. If index is negative, then it affect number of remove items.</param>
        /// <param name="count">Number of items to remove (or test to remove). Real count of removed items can be smaller, by filter (Predicate). Zero or negative value = no remove.</param>
        /// <param name="match"></param>
        private void _RemoveOnRange(int index, int count, Predicate<T> match)
        {
            if (count < 0) return;
            bool noFilter = (match == null);
            int i = index + count - 1;                     // Last index of item to remove
            if (i >= this.Count) i = this.Count - 1;       // Real last index
            while (i >= index && i >= 0)                   // Only items with [i] >= index (and (i >= 0))
            {
                T item = this.List[i];
                if (noFilter || match(item))
                {
                    if (this.CanRemoveItem(item, i))
                    {
                        this.List.RemoveAt(i);
                        this.OnRemoveItemAfter(item, i);
                    }
                }
                i--;
            }
        }
        //
        // Summary:
        //     Reverses the order of the elements in the entire System.Collections.Generic.List&lt;T&gt;.
        public void Reverse()
        {
            if (this.CanChangeList(CollectionChangeType.ChangeOrder))
            {
                this.List.Reverse();
                this.OnChangeListAfter(CollectionChangeType.ChangeOrder);
            }
        }
        //
        // Summary:
        //     Reverses the order of the elements in the specified range.
        //
        // Parameters:
        //   index:
        //     The zero-based starting index of the range to reverse.
        //
        //   count:
        //     The number of elements in the range to reverse.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-count is less than 0.
        //
        //   System.ArgumentException:
        //     index and count do not denote a valid range of elements in the System.Collections.Generic.List&lt;T&gt;.
        public void Reverse(int index, int count)
        {
            if (this.CanChangeList(CollectionChangeType.ChangeOrder))
            {
                this.List.Reverse(index, count);
                this.OnChangeListAfter(CollectionChangeType.ChangeOrder);
            }
        }
        //
        // Summary:
        //     Sorts the elements in the entire System.Collections.Generic.List&lt;T&gt; using
        //     the default comparer.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     The default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default cannot
        //     find an implementation of the System.IComparable&lt;T&gt; generic interface or
        //     the System.IComparable interface for type T.
        public void Sort()
        {
            if (this.CanChangeList(CollectionChangeType.ChangeOrder))
            {
                this.List.Sort();
                this.OnChangeListAfter(CollectionChangeType.ChangeOrder);
            }
        }
        //
        // Summary:
        //     Sorts the elements in the entire System.Collections.Generic.List&lt;T&gt; using
        //     the specified System.Comparison&lt;T&gt;.
        //
        // Parameters:
        //   comparison:
        //     The System.Comparison&lt;T&gt; to use when comparing elements.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     comparison is null.
        //
        //   System.ArgumentException:
        //     The implementation of comparison caused an error during the sort. For example,
        //     comparison might not return 0 when comparing an item with itself.
        public void Sort(Comparison<T> comparison)
        {
            if (this.CanChangeList(CollectionChangeType.ChangeOrder))
            {
                this.List.Sort(comparison);
                this.OnChangeListAfter(CollectionChangeType.ChangeOrder);
            }
        }
        //
        // Summary:
        //     Sorts the elements in the entire System.Collections.Generic.List&lt;T&gt; using
        //     the specified comparer.
        //
        // Parameters:
        //   comparer:
        //     The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        //     elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        //     cannot find implementation of the System.IComparable&lt;T&gt; generic interface
        //     or the System.IComparable interface for type T.
        //
        //   System.ArgumentException:
        //     The implementation of comparer caused an error during the sort. For example,
        //     comparer might not return 0 when comparing an item with itself.
        public void Sort(IComparer<T> comparer)
        {
            if (this.CanChangeList(CollectionChangeType.ChangeOrder))
            {
                this.List.Sort(comparer);
                this.OnChangeListAfter(CollectionChangeType.ChangeOrder);
            }
        }
        //
        // Summary:
        //     Sorts the elements in a range of elements in System.Collections.Generic.List&lt;T&gt;
        //     using the specified comparer.
        //
        // Parameters:
        //   index:
        //     The zero-based starting index of the range to sort.
        //
        //   count:
        //     The length of the range to sort.
        //
        //   comparer:
        //     The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        //     elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-count is less than 0.
        //
        //   System.ArgumentException:
        //     index and count do not specify a valid range in the System.Collections.Generic.List&lt;T&gt;.-or-The
        //     implementation of comparer caused an error during the sort. For example,
        //     comparer might not return 0 when comparing an item with itself.
        //
        //   System.InvalidOperationException:
        //     comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        //     cannot find implementation of the System.IComparable&lt;T&gt; generic interface
        //     or the System.IComparable interface for type T.
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (this.CanChangeList(CollectionChangeType.ChangeOrder))
            {
                this.List.Sort(index, count, comparer);
                this.OnChangeListAfter(CollectionChangeType.ChangeOrder);
            }
        }
        //
        // Summary:
        //     Copies the elements of the System.Collections.Generic.List&lt;T&gt; to a new array.
        //
        // Returns:
        //     An array containing copies of the elements of the System.Collections.Generic.List&lt;T&gt;.
        public T[] ToArray() { return this.List.ToArray(); }
        //
        // Summary:
        //     Sets the capacity to the actual number of elements in the System.Collections.Generic.List&lt;T&gt;,
        //     if that number is less than a threshold value.
        public void TrimExcess() { this.List.TrimExcess(); }
        //
        // Summary:
        //     Determines whether every element in the System.Collections.Generic.List&lt;T&gt;
        //     matches the conditions defined by the specified predicate.
        //
        // Parameters:
        //   match:
        //     The System.Predicate&lt;T&gt; delegate that defines the conditions to check against
        //     the elements.
        //
        // Returns:
        //     true if every element in the System.Collections.Generic.List&lt;T&gt; matches the
        //     conditions defined by the specified predicate; otherwise, false. If the list
        //     has no elements, the return value is true.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     match is null.
        public bool TrueForAll(Predicate<T> match) { return this.List.TrueForAll(match); }
        #endregion
        #region Silent methods (does not call any events)
        /// <summary>
        /// Adds the elements of the specified collection to the end of the System.Collections.Generic.List&lt;T&gt;.
        /// Doeas not call any events.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the System.Collections.Generic.List&lt;T&gt;. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public void AddRangeSilent(IEnumerable<T> collection)
        {
            this.List.AddRange(collection);
        }
        /// <summary>
        /// Removes all elements from the System.Collections.Generic.List&lt;T&gt;.
        /// Does not call any events!!!
        /// </summary>
        public void ClearSilent()
        {
            this.List.Clear();
        }
        #endregion
        #region IEnumerable Members
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return this.List.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return this.List.GetEnumerator(); }
        #endregion
    }
    /// <summary>
    /// Typ změny
    /// </summary>
    public enum CollectionChangeType
    {
        /// <summary>Neurčeno</summary>
        None,
        /// <summary>Konkrétní akce</summary>
        Add,
        /// <summary>Konkrétní akce</summary>
        AddRange,
        /// <summary>Konkrétní akce</summary>
        Get,
        /// <summary>Konkrétní akce</summary>
        Set,
        /// <summary>Konkrétní akce</summary>
        Remove,
        /// <summary>Konkrétní akce</summary>
        RemoveAll,
        /// <summary>Konkrétní akce</summary>
        ChangeOrder,
        /// <summary>Konkrétní akce</summary>
        Clear
    }
    #endregion
    #region DictionaryList : Dictionary, jehož Value je List hodnot TValue (=pro jeden klíč Key může uchovat více hodnot Value)
    /// <summary>
    /// DictionaryList : Dictionary, jehož Value je List hodnot TValue (=pro jeden klíč Key může uchovat více hodnot Value)
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictionaryList<TKey, TValue>
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DictionaryList()
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="capacity"></param>
        public DictionaryList(int capacity)
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>(capacity);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="comparer"></param>
        public DictionaryList(IEqualityComparer<TKey> comparer)
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>(comparer);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="keySelector"></param>
        public DictionaryList(Func<TValue, TKey> keySelector)
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>();
            this._KeySelector = keySelector;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keySelector"></param>
        public DictionaryList(IEnumerable<TValue> values, Func<TValue, TKey> keySelector)
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>();
            this._KeySelector = keySelector;
            this.AddRange(values);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keySelector"></param>
        public DictionaryList(IEnumerable<Tuple<TKey, TValue>> values, Func<TValue, TKey> keySelector = null)
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>();
            this._KeySelector = keySelector;
            this.AddRange(values);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keySelector"></param>
        public DictionaryList(IEnumerable<KeyValuePair<TKey, TValue>> values, Func<TValue, TKey> keySelector = null)
        {
            this._Dictionary = new Dictionary<TKey, List<TValue>>();
            this._KeySelector = keySelector;
            this.AddRange(values);
        }
        /// <summary>
        /// Ověří, že je k dispozici metoda <see cref="_KeySelector"/>.
        /// </summary>
        private void _KeySelectorCheck()
        {
            if (this.HasKeySelector) return;
            throw new GraphLibCodeException("Nelze použít DictionaryList bez zadané funkce keySelector.");
        }
        private Dictionary<TKey, List<TValue>> _Dictionary;
        private Func<TValue, TKey> _KeySelector;
        #endregion
        #region Vkládání, smazání, získání dat
        /// <summary>
        /// true pokud má k dispozici KeySelector
        /// </summary>
        public bool HasKeySelector { get { return (this._KeySelector != null); } }
        /// <summary>
        /// Počet klíčů.
        /// Tato hodnota je zjištěna ihned = jde o počet záznamů v interní Dictionary.
        /// Na rozdíl od toho hodnota <see cref="Count"/> je pomalejší.
        /// </summary>
        public int CountKeys { get { return this._Dictionary.Count; } }
        /// <summary>
        /// Počet hodnot.
        /// Tato hodnota je zjištěna postupným procházením všech klíčů a sčítáním počtu jejich položek.
        /// Na rozdíl od toho hodnota <see cref="CountKeys"/> je vrácena zcela ihned.
        /// </summary>
        public int Count { get { return this._Dictionary.Sum(kvp => kvp.Value.Count); } }
        /// <summary>
        /// Smaže vše
        /// </summary>
        public void Clear() { this._Dictionary.Clear(); }
        /// <summary>
        /// Přidá jednu položku. její klíč si odvodí sám.
        /// Instance <see cref="DictionaryList{TKey, TValue}"/> musí být vytvořena tak, že obsahuje keySelector (vhodným konstruktorem).
        /// </summary>
        /// <param name="value"></param>
        public void Add(TValue value)
        {
            this._KeySelectorCheck();
            this._Add(this._KeySelector(value), value);
        }
        /// <summary>
        /// Přidá jednu položku s daným klíčem.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            this._Add(key, value);
        }
        /// <summary>
        /// Přidá sadu položek.
        /// Instance <see cref="DictionaryList{TKey, TValue}"/> musí být vytvořena tak, že obsahuje keySelector (vhodným konstruktorem).
        /// </summary>
        /// <param name="values">položky</param>
        public void AddRange(IEnumerable<TValue> values)
        {
            this._KeySelectorCheck();
            if (values == null) return;
            foreach (TValue value in values)
                this._Add(this._KeySelector(value), value);
        }
        /// <summary>
        /// Přidá sadu položek.
        /// Položky obsahují klíče i hodnoty.
        /// </summary>
        /// <param name="values">položky</param>
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            if (values == null) return;
            foreach (KeyValuePair<TKey, TValue> value in values)
                this._Add(value.Key, value.Value);
            
        }
        /// <summary>
        /// Přidá sadu položek.
        /// Položky obsahují klíče (v <see cref="Tuple{T1, T2}.Item1"/>) i hodnoty (v <see cref="Tuple{T1, T2}.Item2"/>).
        /// </summary>
        /// <param name="values">položky</param>
        public void AddRange(IEnumerable<Tuple<TKey, TValue>> values)
        {
            if (values == null) return;
            foreach (Tuple<TKey, TValue> value in values)
                this._Add(value.Item1, value.Item2);
        }
        /// <summary>
        /// Přidá sadu položek.
        /// Pro získání klíče použije dodaný keySelector (i kdyby instance <see cref="DictionaryList{TKey, TValue}"/> byla vytvořena včetně svého keySelectoru).
        /// </summary>
        /// <param name="values">položky</param>
        /// <param name="keySelector"></param>
        public void AddRange(IEnumerable<TValue> values, Func<TValue, TKey> keySelector)
        {
            if (values == null) return;
            foreach (TValue value in values)
                this._Add(keySelector(value), value);
        }
        /// <summary>
        /// Přidá jednu položku s daným klíčem.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void _Add(TKey key, TValue value)
        {
            if (key == null)
                throw new GraphLibCodeException("Klíč pro DictionaryList je null.");
            List<TValue> list;
            if (!this._Dictionary.TryGetValue(key, out list))
            {
                list = new List<TValue>();
                this._Dictionary.Add(key, list);
            }
            list.Add(value);
        }
        /// <summary>
        /// Odebere data pro daný klíč = všechny uložené záznamy
        /// </summary>
        /// <param name="key"></param>
        public void RemoveKey(TKey key)
        {
            if (this._Dictionary.ContainsKey(key))
                this._Dictionary.Remove(key);
        }
        /// <summary>
        /// Odebere danou hodnotu.
        /// Instance <see cref="DictionaryList{TKey, TValue}"/> musí být vytvořena tak, že obsahuje keySelector (vhodným konstruktorem).
        /// </summary>
        /// <param name="value"></param>
        public void Remove(TValue value)
        {
            this._KeySelectorCheck();
            this._Remove(this._KeySelector(value), value, null);
        }
        /// <summary>
        /// Odebere danou hodnotu.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Remove(TKey key, TValue value)
        {
            this._Remove(key, value, null);
        }
        /// <summary>
        /// Odebere danou hodnotu z daného klíče.
        /// Pokud pro daný klíč nezbyde žádná hodnota, odebere i klíč.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="equalitySelector"></param>
        private void _Remove(TKey key, TValue value, Func<TValue, TValue, bool> equalitySelector)
        {
            List<TValue> list;
            if (this._Dictionary.TryGetValue(key, out list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    TValue item = list[i];
                    bool isEqual = (equalitySelector != null ? equalitySelector(item, value) : Object.ReferenceEquals(item, value));
                    if (isEqual)
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                }
                if (list.Count == 0)
                    this._Dictionary.Remove(key);
            }
        }
        /// <summary>
        /// Metoda odebere všechny prvky vyhovující dané podmínce.
        /// </summary>
        /// <param name="predicate"></param>
        public void RemoveAll(Func<TKey, TValue, bool> predicate)
        {
            var keys = this._Dictionary.Keys.ToArray();    // Vytvořím new array klíčů, protože nelze Dictionary současně enumerovat (foreach) a současně z ní odebírat (Remove(key))!
            foreach (var key in keys)
            {
                var list = this._Dictionary[key];
                for (int i = 0; i < list.Count; i++)
                {
                    var value = list[i];
                    if (predicate(key, value))
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                }
                if (list.Count == 0)
                    this._Dictionary.Remove(key);
            }
        }
        /// <summary>
        /// Počet záznamů pro daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int CountValues(TKey key)
        {
            List<TValue> list;
            if (this._Dictionary.TryGetValue(key, out list)) return list.Count;
            return 0;
        }
        /// <summary>
        /// Vrátí hodnoty pro daný klíč.
        /// Pokud pro daný klíč neobsahuje žádné hodnoty, vrací se null.
        /// Nelze setovat (pro přidání existují metody Add a AddRange).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue[] this[TKey key]
        {
            get
            {
                List<TValue> list;
                if (this._Dictionary.TryGetValue(key, out list)) return list.ToArray();
                return null;
            }
        }
        /// <summary>
        /// Pole všech hodnot v této instanci (bez ohledu na klíče)
        /// </summary>
        public TValue[] Values
        {
            get
            {
                List<TValue> list = new List<TValue>();
                foreach (var items in this._Dictionary.Values)
                    list.AddRange(items);
                return list.ToArray();
            }
        }
        /// <summary>
        /// Pole všech hodnot v této instanci, včetně klíčů.
        /// Výstupní instance Tuple obsahuje: Item1 = Key, Item2 = Value.
        /// </summary>
        public Tuple<TKey, TValue>[] KeyValues
        {
            get
            {
                List<Tuple<TKey, TValue>> list = new List<Tuple<TKey, TValue>>();
                foreach (var kvp in this._Dictionary)
                    list.AddRange(kvp.Value.Select(i => new Tuple<TKey, TValue>(kvp.Key, i)));
                return list.ToArray();
            }
        }
        /// <summary>
        /// Vrací true, když this instance obsahuje data pro daný klíč.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return this._Dictionary.ContainsKey(key);
        }
        /// <summary>
        /// Zkusí najít data pro daný klíč.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue[] values)
        {
            values = null;
            List<TValue> list;
            if (!this._Dictionary.TryGetValue(key, out list)) return false;
            values = list.ToArray();
            return true;
        }
        #endregion
    }
    #endregion
    #region Collection : Předek pro typové kolekce, read-only
    /// <summary>
    /// Collection : Ancestor for typed, public read-only collections.
    /// External code can not Add or Remove item, nor call Clear. 
    /// Can only enumerate through collection, can read Count and read specific item from index, but cannot set an new item to existing index.
    /// Descendant can do all via protected acces to List of items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Collection<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Collection()
        {
            this.ItemList = new List<T>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="items"></param>
        public Collection(IEnumerable<T> items)
        {
            this.ItemList = new List<T>(items);
        }
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count { get { return this.ItemList.Count; } }
        /// <summary>
        /// Přístup k prvku
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] { get { return this.ItemList[index]; } }
        /// <summary>
        /// Prvky
        /// </summary>
        protected List<T> ItemList;
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.ItemList.GetEnumerator();
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.ItemList.GetEnumerator();
        }
    }
    #endregion
}
