using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Data
{
    /// <summary>
    /// Array with two index and one value on intersect of this indexes.
    /// </summary>
    /// <typeparam name="TKey1"></typeparam>
    /// <typeparam name="TKey2"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class TypeArray<TKey1, TKey2, TValue>
    {
        public TypeArray()
        {
            this._Dictionary = new Dictionary<TKey1, Dictionary<TKey2, TValue>>();
        }
        private Dictionary<TKey1, Dictionary<TKey2, TValue>> _Dictionary;

        public void Clear()
        {
            this._Dictionary.Clear();
        }
        public int CountTotal { get { return this._Dictionary.Sum(d => d.Value.Count); } }
        
        public TValue this[TKey1 key1, TKey2 key2]
        {
            get
            {
                TValue value;
                this.TryGetValue(key1, key2, out value);
                return value;
            }
            set
            {
                Dictionary<TKey2, TValue> dict2;
                if (!this._Dictionary.TryGetValue(key1, out dict2))
                {
                    dict2 = new Dictionary<TKey2, TValue>();
                    this._Dictionary.Add(key1, dict2);
                }
                if (!dict2.ContainsKey(key2))
                    dict2.Add(key2, value);
                else
                    dict2[key2] = value;
            }
        }
        public bool ContainsKey(TKey1 key1, TKey2 key2)
        {
            TValue value;
            return this.TryGetValue(key1, key2, out value);
        }
        public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
        {
            value = default(TValue);
            Dictionary<TKey2, TValue> dict2;
            if (!this._Dictionary.TryGetValue(key1, out dict2)) return false;
            if (dict2.TryGetValue(key2, out value)) return false;
            return true;
        }
    }
}
