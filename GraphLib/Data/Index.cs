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
    /// Instance třídy <see cref="Index{TKey}"/> jednak dokáže získat Int32 hodnotu indexu pro daný <see cref="TKey"/> aplikační klíč, 
    /// a rovněž dokáže vrátit <see cref="TKey"/> aplikační klíč pro daný Int32 index.
    /// <para/>
    /// Instance třídy <see cref="Index{TKey}"/> dovoluje i tvorbu Int32 indexu v globálním rozsahu (jednoznačný Int32 index přes více nebo všechny instance).
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class Index<TKey>
    {
        public Index()
        {
            this._IndexInt = new Dictionary<int, TKey>();
            this._IndexKey = new Dictionary<TKey, int>();
        }
        private Dictionary<int, TKey> _IndexInt;
        private Dictionary<TKey, int> _IndexKey;

        public int GetIndex(TKey key)
        {
            int index;
            if (!this._IndexKey.TryGetValue(key, out index))
            {
                index = _CreateNewIndex();
                this._AddIndexKey(index, key);
            }
            return index;
        }
        public TKey GetKey(int index)
        { }
        public bool TryGetKey(int index, out TKey key)
        { }
        public bool ContainsIndex(int index)
        { }
        public bool ContainsKey(TKey key)
        { }
    }
}
