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
using Noris.Clients.Win.Components.AsolDX.InternalPersistor;
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
        /// Z dodané kolekce vytvoří Dictionary. Umožní ignorovat duplicity klíčů.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="ignoreDuplicity"></param>
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
        #endregion
        #region Align
        /// <summary>
        /// Vrátí danou hodnotu zarovnanou do mezí min, max
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
            int maxval = max.CompareTo(value);   // pokud value je větší než max, vracím max:
            if (maxval <= 0) return max;
            return value;
        }
        #endregion
    }
}
