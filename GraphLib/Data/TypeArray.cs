﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    /// <summary>
    /// Pole, které má dva indexy a v jejich průsečíku jednu hodnotu.
    /// Ve výchozím stavu pole neobsahuje žádnou hodnotu.
    /// Čtení hodnoty z buňky, která dosud nebyla naplněna, vrací default(<typeparamref name="TValue"/>).
    /// </summary>
    /// <typeparam name="TKey1"></typeparam>
    /// <typeparam name="TKey2"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class TypeArray<TKey1, TKey2, TValue>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TypeArray()
        {
            this._Dictionary = new Dictionary<TKey1, Dictionary<TKey2, TValue>>();
        }
        private Dictionary<TKey1, Dictionary<TKey2, TValue>> _Dictionary;
        /// <summary>
        /// Odstraní všechny prvky
        /// </summary>
        public void Clear()
        {
            this._Dictionary.Clear();
        }
        /// <summary>
        /// Počet prvků v celém poli
        /// </summary>
        public int CountTotal { get { return this._Dictionary.Sum(d => d.Value.Count); } }
        /// <summary>
        /// Přístup k hodnotě pole.
        /// Pokus o přístup {get} k dosud neexistujícímu prvku nezpůsobí chybu, vrací default(<typeparamref name="TValue"/>).
        /// Existenci hodnoty pro dané klíče lze zjistit pomocí metody <see cref="ContainsKey(TKey1, TKey2)"/>.
        /// Zápis hodnoty {set} do dosud neexistujícího prvku nezpůsobí chybu, prvek korektně uloží.
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vrátí true, pokud pro dané klíče již existuje uložená hodnota.
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey1 key1, TKey2 key2)
        {
            TValue value;
            return this.TryGetValue(key1, key2, out value);
        }
        /// <summary>
        /// Pokusí se získat hodnotu pro dané klíče.
        /// Pokud dosud neexistuje, vrací false; a out hodnota value je = default(<typeparamref name="TValue"/>).
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
        {
            value = default(TValue);
            Dictionary<TKey2, TValue> dict2;
            if (!this._Dictionary.TryGetValue(key1, out dict2)) return false;
            if (dict2.TryGetValue(key2, out value)) return false;
            return true;
        }
        /// <summary>
        /// Pole všech dosud uložených hodnot, bez klíčů = jen uložené hodnoty
        /// </summary>
        public TValue[] Values
        {
            get
            {
                List<TValue> result = new List<TValue>();
                foreach (var items in this._Dictionary.Values)
                    result.AddRange(items.Values);
                return result.ToArray();
            }
        }
        /// <summary>
        /// Pole všech dosud uložených hodnot, včetně klíčů.
        /// Ve výstupní instanci Tuple je uloženo: Item1 = Key1, Item2 = Key2, Item3 = Value.
        /// </summary>
        public Tuple<TKey1, TKey2, TValue>[] KeyValues
        {
            get
            {
                List<Tuple<TKey1, TKey2, TValue>> result = new List<Tuple<TKey1, TKey2, TValue>>();
                foreach (var items in this._Dictionary)
                    result.AddRange(items.Value.Select(i => new Tuple<TKey1, TKey2, TValue>(items.Key, i.Key, i.Value)));
                return result.ToArray();
            }
        }
    }
}
