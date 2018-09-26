using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Data
{
    /// <summary>
    /// Třída, která dovoluje získávat pro daný aplikační klíč unikátní Int32 index, a následně pro daný Int32 index vrátí vstupní aplikační klíč.
    /// Použití: pokud máme část aplikace, která vyžaduje jednoznačné Int32 klíče, ale datová část aplikace má klíče jiného typu (GUID, UInt64, GId),
    /// pak je nutné zajistit konverzi klíčů oběma směry. K tomu lze použít <see cref="Index{TKey}"/>.
    /// Instance třídy <see cref="Index{TKey}"/> jednak dokáže získat Int32 hodnotu indexu pro daný <typeparamref name="TKey"/> aplikační klíč, 
    /// a rovněž dokáže vrátit <typeparamref name="TKey"/> aplikační klíč pro daný Int32 index.
    /// <para/>
    /// Instance třídy <see cref="Index{TKey}"/> dovoluje i tvorbu Int32 indexu v globálním rozsahu (jednoznačný Int32 index přes více nebo všechny instance).
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class Index<TKey>
    {
        #region Konstrukce a proměnné
        /// <summary>
        /// Vytvoří nový index, s rozsahem platnosti <see cref="IndexScopeType.Instance"/>
        /// </summary>
        public Index()
            : this(IndexScopeType.Instance)
        { }
        /// <summary>
        /// Vytvoří nový index, s daným rozsahem platnosti
        /// </summary>
        /// <param name="indexScope">Rozsah platnosti</param>
        public Index(IndexScopeType indexScope)
        {
            this._IndexInt = new Dictionary<int, TKey>();
            this._IndexKey = new Dictionary<TKey, int>();
            this._IndexScope = ((indexScope == IndexScopeType.Instance || indexScope == IndexScopeType.TKeyType || indexScope == IndexScopeType.Global) ? indexScope : IndexScopeType.Instance);
            this._IndexLast = 0;
            this._InstanceSyncLock = new object();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Index<int, " + typeof(TKey).Name + "; Count: " + this._IndexInt.Count.ToString();
        }
        private Dictionary<int, TKey> _IndexInt;
        private Dictionary<TKey, int> _IndexKey;
        private IndexScopeType _IndexScope;
        private int _IndexLast;
        private object _InstanceSyncLock;
        #endregion
        #region Public rozhraní: metody GetIndex(), GetKey(), Contains...(), TryGet...(), indexery
        /// <summary>
        /// Rozsah platnosti indexu (jednoznačnost indexu mezi různými instancemi <see cref="Index{TKey}"/>)
        /// </summary>
        public IndexScopeType IndexScope { get { return this._IndexScope; } }
        /// <summary>
        /// Pro daný <typeparamref name="TKey"/> klíč vrátí jeho Int32 index.
        /// Pokud v této instanci dosud klíč není, založí nový index a vrátí jej.
        /// Jednoznačnost nového indexu je dána rozsahem platnosti <see cref="IndexScope"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetIndex(TKey key)
        {
            int index;
            if (!this._IndexKey.TryGetValue(key, out index))
            {
                index = this._CreateNewIndex();
                this._AddIndexKey(index, key);
            }
            return index;
        }
        /// <summary>
        /// Pro daný Int32 index vrátí jeho <typeparamref name="TKey"/> klíč.
        /// Pokud index neexistuje, dojde k chybě (lze použít metody <see cref="TryGetKey(int, out TKey)"/> nebo <see cref="ContainsIndex(int)"/>).
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TKey GetKey(int index)
        {
            return this._IndexInt[index];
        }
        /// <summary>
        /// Pro daný Int32 index zkusí najít klíč
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryGetKey(int index, out TKey key)
        {
            return this._IndexInt.TryGetValue(index, out key);
        }
        /// <summary>
        /// Vrací true, pokud existuje záznam pro daný Int32 index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool ContainsIndex(int index)
        {
            return this._IndexInt.ContainsKey(index);
        }
        /// <summary>
        /// Vrací true, pokud existuje záznam pro daný <typeparamref name="TKey"/> klíč
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return this._IndexKey.ContainsKey(key);
        }
        /// <summary>
        /// Pro daný <typeparamref name="TKey"/> klíč vrátí jeho Int32 index.
        /// Pokud v této instanci dosud klíč není, založí nový index a vrátí jej.
        /// Jednoznačnost nového indexu je dána rozsahem platnosti <see cref="IndexScope"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int this[TKey key] { get { return this.GetIndex(key); } }
        /// <summary>
        /// Pro daný index vrátí jeho klíč.
        /// Pokud index neexistuje, dojde k chybě (lze použít metody <see cref="TryGetKey(int, out TKey)"/> nebo <see cref="ContainsIndex(int)"/>).
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TKey this[int index] { get { return this.GetKey(index); } }
        /// <summary>
        /// Do obou interních dictionary přidá index a jeho <typeparamref name="TKey"/> key
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
        private void _AddIndexKey(int index, TKey key)
        {
            this._IndexInt.Add(index, key);
            this._IndexKey.Add(key, index);
        }
        #endregion
        #region Smazání dat Clear(), Remove(); počet dat Count; enumerátory Indexes, Keys
        /// <summary>
        /// Smaže celý obsah indexu.
        /// Ale hodnotu pro příští index (přidělený v režimu <see cref="IndexScope"/> == <see cref="IndexScopeType.Instance"/>) neresetuje, takže další indexy budou mít nálsedující vyšší číslo.
        /// </summary>
        public void Clear()
        {
            this._IndexInt.Clear();
            this._IndexKey.Clear();
        }
        /// <summary>
        /// Odebere jednu položku dle daného indexu.
        /// Pokud neexistuje, k chybě nedojde,
        /// </summary>
        /// <param name="index"></param>
        public void RemoveIndex(int index)
        {
            TKey key;
            if (this._IndexInt.TryGetValue(index, out key))
            {
                this._IndexInt.Remove(index);
                if (this._IndexKey.ContainsKey(key))
                    this._IndexKey.Remove(key);
            }
        }
        /// <summary>
        /// Odebere jednu položku dle daného klíče <typeparamref name="TKey"/>.
        /// Pokud neexistuje, k chybě nedojde,
        /// </summary>
        /// <param name="key"></param>
        public void RemoveKey(TKey key)
        {
            int index;
            if (this._IndexKey.TryGetValue(key, out index))
            {
                this._IndexKey.Remove(key);
                if (this._IndexInt.ContainsKey(index))
                    this._IndexInt.Remove(index);
            }
        }
        /// <summary>
        /// Počet položek v indexu
        /// </summary>
        public int Count { get { return this._IndexInt.Count; } }
        /// <summary>
        /// Kolekce Int32 indexů
        /// </summary>
        public IEnumerable<int> Indexes { get { return this._IndexInt.Keys; } }
        /// <summary>
        /// Kolekce <typeparamref name="TKey"/> klíčů
        /// </summary>
        public IEnumerable<TKey> Keys { get { return this._IndexKey.Keys; } }
        #endregion
        #region Generátor indexu
        /// <summary>
        /// Vygeneruje a vrátí nový index pro nový prvek tohoto Indexu, podle jeho deklarovaného scope.
        /// </summary>
        /// <returns></returns>
        private int _CreateNewIndex()
        {
            switch (this._IndexScope)
            {
                case IndexScopeType.Global:
                    return _CreateNewIndexGlobal();             // static metoda
                case IndexScopeType.TKeyType:
                    return _CreateNewIndexTKeyType();           // static metoda
                case IndexScopeType.Instance:
                default:
                    return this._CreateNewIndexInstance();      // instanční metoda
            }
        }
        /// <summary>
        /// Vrátí nový index pro scope <see cref="IndexScopeType.Instance"/>
        /// </summary>
        /// <returns></returns>
        private int _CreateNewIndexInstance()
        {
            int index = 0;
            lock (this._InstanceSyncLock)
            {
                index = ++this._IndexLast;
            }
            return index;
        }
        /// <summary>
        /// Vrátí nový index pro scope <see cref="IndexScopeType.TKeyType"/>
        /// </summary>
        /// <returns></returns>
        private static int _CreateNewIndexTKeyType()
        {
            return Application.App.GetNextId(typeof(TKey));
        }
        /// <summary>
        /// Vrátí nový index pro scope <see cref="IndexScopeType.Global"/>
        /// </summary>
        /// <returns></returns>
        private static int _CreateNewIndexGlobal()
        {
            return Application.App.GetNextId(typeof(IndexScopeType));
        }
        #endregion
    }
    #region enum IndexScopeType : Rozsah platnosti generovaného indexu v třídě Index<TKey>
    /// <summary>
    /// IndexScopeType : Rozsah platnosti generovaného indexu v třídě <see cref="Index{TKey}"/>
    /// </summary>
    public enum IndexScopeType
    {
        /// <summary>
        /// Nezadáno, použije se <see cref="Instance"/>
        /// </summary>
        None,
        /// <summary>
        /// Unikátnost hodnoty indexu je zajištěna v rámci jedné instance <see cref="Index{TKey}"/>.
        /// Tzn. každá instance <see cref="Index{TKey}"/> generuje hodnoty index počínaje od 1.
        /// </summary>
        Instance,
        /// <summary>
        /// Unikátnost hodnoty indexu je zajištěna v rámci jednoho datového typu TKey instance <see cref="Index{TKey}"/>.
        /// Tzn. různé instance <see cref="Index{TKey}"/>, které pracují se shodným typem TKey a mají nastaven scope <see cref="TKeyType"/> generují hodnoty indexu v jedné společné řadě, počínaje od 1.
        /// </summary>
        TKeyType,
        /// <summary>
        /// Unikátnost hodnoty indexu je zajištěna globálně přes všechny instance <see cref="Index{TKey}"/>, které mají globální scope.
        /// Tzn. různé instance <see cref="Index{TKey}"/>, bez ohledu na typ TKey, pokud mají nastaven scope <see cref="Global"/>, generují hodnoty indexu v jedné společné řadě, počínaje od 1.
        /// </summary>
        Global
    }
    #endregion
}
