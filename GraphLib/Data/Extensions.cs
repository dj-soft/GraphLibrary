using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Data
{
    /// <summary>
    /// Extensions for Data classes
    /// </summary>
    public static class DataExtensions
    {
        #region Type
        /// <summary>
        /// Returns "Namespace.Name" of this Type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string NsName(this Type type)
        {
            return type.Namespace + "." + type.Name;
        }
        #endregion
        #region String
        /// <summary>
        /// Returns an array of string as rows from this text.
        /// Items are without trimming, include empty rows from text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string[] ToLines(this string text)
        {
            return ToLines(text, false, false);
        }
        /// <summary>
        /// Returns an array of string as rows from this text.
        /// Items can be trimmed, empty rows can be removed from text, by parameters.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="removeEmptyLines"></param>
        /// <param name="trimRows"></param>
        /// <returns></returns>
        public static string[] ToLines(this string text, bool removeEmptyLines, bool trimRows)
        {
            text = text
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");
            string[] rows = text.Split(new char[] { '\r' }, (removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None));
            if (trimRows)
                rows = rows.Select(i => i.Trim()).ToArray();
            return rows;
        }
        #endregion
        #region IEnumerable
        /// <summary>
        /// Provede danou akci pro každý prvek kolekce
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="action"></param>
        public static void ForEachItem<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T item in collection)
                action(item);
        }
        /// <summary>
        /// Vrátí novou kolekci vzniklou z dodané kolekce, a přidáním dodaných prvků
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="appendItems"></param>
        /// <returns></returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> collection, params T[] appendItems)
        {
            List<T> result = new List<T>(collection);
            result.AddRange(appendItems);
            return result;
        }
        public static T GetNearestItem<T>(this IEnumerable<T> collection, T currentItem)
        {
            return GetNearestItem(collection, currentItem, Direction.Positive);
        }
        public static T GetNearestItem<T>(this IEnumerable<T> collection, T currentItem, Data.Direction nearestSide)
        {
            if (collection == null) return default(T);

            // Jedním průchodem najdu prvek currentItem, přitom si zaeviduji prvek těsně před ním, a zaregistruji i prvek těsně po něm:
            bool hasPrevItem = false;
            T prevItem = default(T);
            bool hasNextItem = false;
            T nextItem = default(T);

            bool isFound = false;
            foreach (T item in collection)
            {
                if (isFound)
                {
                    hasNextItem = true;
                    nextItem = item;
                    break;
                }
                if (Object.ReferenceEquals(item, currentItem))
                {
                    isFound = true;
                }
                else
                {
                    hasPrevItem = true;
                    prevItem = item;
                }
            }
            // Pokud nemám nic (buď jsem nenašel hledaný prvek, nebo sice mám hledaný prvek, ale ten okolo sebe nemá žádný jiný prvek), vracím null:
            if ((!isFound) || (!hasPrevItem && !hasNextItem)) return default(T);

            // Pokud mám prvek jen na jedné straně (next nebo prev), vracím ten nalezený:
            if (!hasPrevItem && hasNextItem) return nextItem;
            if (hasPrevItem && !hasNextItem) return prevItem;

            // Máme oba okolní prvky, vrátím ten který má přednost podle parametru nearestSide:
            return ((nearestSide == Direction.Positive) ? nextItem : prevItem);
        }
        #endregion
        #region List<T>
        /// <summary>
        /// Returns last item in list, by list.Count. Or return default(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static T LastOrDefaultInList<T>(this List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[list.Count - 1];
        }
        #endregion
        #region Data.Direction
        public static Data.Direction Reverse(this Data.Direction direction)
        {
            switch (direction)
            {
                case Data.Direction.Positive: return Data.Direction.Negative;
                case Data.Direction.Negative: return Data.Direction.Positive;
            }
            return Data.Direction.None;
        }
        public static int NumericValue(this Data.Direction direction)
        {
            switch (direction)
            {
                case Data.Direction.Positive: return 1;
                case Data.Direction.Negative: return -1;
            }
            return 0;
        }
        public static int CompareToDirection(this Data.Direction direction, Data.Direction other)
        {
            int a = direction.NumericValue();
            int b = other.NumericValue();
            return a.CompareTo(b);
        }
        #endregion
   }
}
