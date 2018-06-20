using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Asol.Tools.WorkScheduler.Data
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
        public static bool IsConvertibleTo(this Type currentType, Type expectedType)
        {
            if (currentType == expectedType) return true;
            Type[] genericArgumnets = expectedType.GetGenericArguments();
            if (expectedType.Name == "Nullable`1" && genericArgumnets != null && genericArgumnets.Length > 0)
            {
                expectedType = genericArgumnets[0];
                if (currentType == expectedType) return true;
            }

            string convert = currentType.Namespace + "." + currentType.Name + " => " + expectedType.Namespace + "." + expectedType.Name;
            switch (convert)
            {   // Co je převoditelné:
                case "System.Int16 => System.Int32":
                case "System.Int16 => System.Int64":
                case "System.Int32 => System.Int64":

                case "System.Int16 => System.Decimal":
                case "System.Int32 => System.Decimal":
                case "System.Int64 => System.Decimal":
                case "System.UInt16 => System.Decimal":
                case "System.UInt32 => System.Decimal":
                case "System.UInt64 => System.Decimal":

                case "System.Int16 => System.Single":
                case "System.Int32 => System.Single":
                case "System.Int64 => System.Single":
                case "System.UInt16 => System.Single":
                case "System.UInt32 => System.Single":
                case "System.UInt64 => System.Single":

                case "System.Int16 => System.Double":
                case "System.Int32 => System.Double":
                case "System.Int64 => System.Double":
                case "System.UInt16 => System.Double":
                case "System.UInt32 => System.Double":
                case "System.UInt64 => System.Double":

                    return true;
            }
            return false;
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
        /// <summary>
        /// Rozdělí daný string na pole polí, kdy lze zadat oddělovač řádků a oddělovač sloupců.
        /// </summary>
        /// <param name="text">Vstupující text. Obsahuje řádky, a v řádku obsahuje prvky oddělené daným stringem.</param>
        /// <param name="rowSeparator">Oddělovač řádků</param>
        /// <param name="itemSeparator">Oddělovač prvků v řádku</param>
        /// <param name="removeEmptyLines">Nenačítat prázdné řádky(</param>
        /// <param name="trimItems">Jednotlivé prvky ukládat Trim()</param>
        /// <returns></returns>
        public static string[][] ToTable(this string text, string rowSeparator, string itemSeparator, bool removeEmptyLines, bool trimItems)
        {
            List<string[]> result = new List<string[]>();
            string[] lines = text.Split(new string[] { rowSeparator }, (removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None));
            foreach (string line in lines)
            {
                string[] items = line.Split(new string[] { itemSeparator }, StringSplitOptions.None);
                if (trimItems)
                    result.Add(items.Select(s => s.Trim()).ToArray());
                else
                    result.Add(items);
            }
            return result.ToArray();
        }
        /// <summary>
        /// Rozdělí daný string na pole prvků KeyValuePair, kdy lze zadat oddělovač řádků a oddělovač Key a Value.
        /// </summary>
        /// <param name="text">Vstupující text. Obsahuje řádky, a v řádku obsahuje prvky oddělené daným stringem.</param>
        /// <param name="rowSeparator">Oddělovač řádků</param>
        /// <param name="itemSeparator">Oddělovač prvků v řádku</param>
        /// <param name="removeEmptyLines">Nenačítat prázdné řádky(</param>
        /// <param name="trimItems">Jednotlivé prvky ukládat Trim()</param>
        /// <returns></returns>
        public static KeyValuePair<string, string>[] ToKeyValues(this string text, string rowSeparator, string itemSeparator, bool removeEmptyLines, bool trimItems)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string,string>>();
            int isl = itemSeparator.Length;
            string[] lines = text.Split(new string[] { rowSeparator }, (removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None));
            foreach (string line in lines)
            {
                int pos = line.IndexOf(itemSeparator);
                if (pos < 0)                          // Bez oddělovače
                    result.Add(new KeyValuePair<string, string>(_ITrim(line, trimItems), null));
                else if (pos == 0)                    // Oddělovač na první pozici
                    result.Add(new KeyValuePair<string, string>("", _ITrim(line.Substring(isl), trimItems)));
                else if (pos < (line.Length - isl))   // Oddělovač je uprostřed
                    result.Add(new KeyValuePair<string, string>(_ITrim(line.Substring(0, pos), trimItems), _ITrim(line.Substring(pos + isl), trimItems)));
                else if (pos == (line.Length - isl))  // Oddělovač je na konci
                    result.Add(new KeyValuePair<string, string>(_ITrim(line.Substring(0, pos), trimItems), ""));
            }
            return result.ToArray();
        }
        private static string _ITrim(string value, bool trim)
        {
            return (value == null ? null : (trim ? value.Trim() : value));
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
        #region DataRow
        /// <summary>
        /// Vrátí obsah sloupce daného jména, typovaný na daný typ.
        /// Pokud řádek v daném sloupci obsahuje null, vrátí default(T).
        /// Pokud sloupec neexistuje nebo obsahuje hodnotu nepřeveditelnou na typ T, vyhodí chybu.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static T GetValue<T>(this DataRow row, string columnName)
        {
            if (row == null) throw new ArgumentNullException("row", "DataRow is null");
            if (String.IsNullOrEmpty(columnName)) throw new ArgumentNullException("columnName", "ColumnName is empty");
            if (!(row.Table.Columns.Contains(columnName))) throw new ArgumentException("Table <" + row.Table.TableName + "> does not contain column with name <" + columnName + ">.");
            if (row.IsNull(columnName)) return default(T);
            object value = row[columnName];
            return (T)GetValue<T>(value);
            // return (T)value;

            // Type currentType = value.GetType();
            // Type expectedType = typeof(T);
            // if (!currentType.IsConvertibleTo(expectedType)) throw new InvalidCastException("Value in column <" + columnName + "> [" + currentType.Name + "] is not convertible to type <" + expectedType.Name + ">.");
            // return (T)value;
        }
        /// <summary>
        /// Vrátí obsah sloupce daného jména, typovaný na daný typ.
        /// Pokud řádek v daném sloupci obsahuje null, vrátí default(T).
        /// Pokud sloupec neexistuje nebo obsahuje hodnotu nepřeveditelnou na typ T, vrátí default(T) ale nedojde k chybě.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static T TryGetValue<T>(this DataRow row, string columnName)
        {
            if (row == null) return default(T);
            if (String.IsNullOrEmpty(columnName)) throw new ArgumentNullException("columnName", "ColumnName is empty");
            if (!(row.Table.Columns.Contains(columnName))) return default(T);
            if (row.IsNull(columnName)) return default(T);
            object value = row[columnName];
            try
            {
                return (T)value;
            }
            catch { }
            return default(T);
        }
        /// <summary>
        /// Vrátí hodnotu v požadovaném typu.
        /// Používá explicitní přetypování.
        /// Může dojít k chybě.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object GetValue<T>(object value)
        {
            if (value == null) return default(T);

            Type sourceType = value.GetType();
            string sourceName = sourceType.Namespace + "." + sourceType.Name;
            Type targetType = typeof(T);
            string targetName = targetType.Namespace + "." + targetType.Name;
            if (sourceName == targetName) return value;
            string convert = targetName + " = " + sourceName;


            Int16 valueInt16;
            Int32 valueInt32;
            Int64 valueInt64;
            UInt16 valueUInt16;
            UInt32 valueUInt32;
            UInt64 valueUInt64;
            Single valueSingle;
            Double valueDouble;
            Decimal valueDecimal;
            String valueString;
            Char valueChar;
            Byte valueByte;
            SByte valueSByte;
            DateTime valueDateTime;
            TimeSpan valueTimeSpan;

            switch (convert)
            {   //   Cílový typ    = Zdrojový typ
                case "System.Int16 = System.Byte":
                    valueInt16 = (Int16)((Byte)value);
                    return valueInt16;
                case "System.Int16 = System.SByte":
                    valueInt16 = (Int16)((SByte)value);
                    return valueInt16;
                case "System.Int16 = System.Int32":
                    valueInt16 = (Int16)((Int32)value);
                    return valueInt16;
                case "System.Int16 = System.Int64":
                    valueInt16 = (Int16)((Int64)value);
                    return valueInt16;
                case "System.Int16 = System.UInt32":
                    valueInt16 = (Int16)((UInt32)value);
                    return valueInt16;
                case "System.Int16 = System.UInt64":
                    valueInt16 = (Int16)((UInt64)value);
                    return valueInt16;

                //   Cílový typ    = Zdrojový typ
                case "System.Int32 = System.Byte":
                    valueInt32 = (Int32)((Byte)value);
                    return valueInt32;
                case "System.Int32 = System.SByte":
                    valueInt32 = (Int32)((SByte)value);
                    return valueInt32;
                case "System.Int32 = System.Int16":
                    valueInt32 = (Int32)((Int16)value);
                    return valueInt32;
                case "System.Int32 = System.Int64":
                    valueInt32 = (Int32)((Int64)value);
                    return valueInt32;
                case "System.Int32 = System.UInt16":
                    valueInt32 = (Int32)((UInt16)value);
                    return valueInt32;
                case "System.Int32 = System.UInt64":
                    valueInt32 = (Int32)((UInt64)value);
                    return valueInt32;

                //   Cílový typ    = Zdrojový typ
                case "System.Int64 = System.Byte":
                    valueInt64 = (Int64)((Byte)value);
                    return valueInt64;
                case "System.Int64 = System.SByte":
                    valueInt64 = (Int64)((SByte)value);
                    return valueInt64;
                case "System.Int64 = System.Int16":
                    valueInt64 = (Int64)((Int16)value);
                    return valueInt64;
                case "System.Int64 = System.Int32":
                    valueInt64 = (Int64)((Int32)value);
                    return valueInt64;
                case "System.Int64 = System.UInt16":
                    valueInt64 = (Int64)((UInt16)value);
                    return valueInt64;
                case "System.Int64 = System.UInt32":
                    valueInt64 = (Int64)((UInt32)value);
                    return valueInt64;

                //   Cílový typ    = Zdrojový typ
                case "System.Single = System.Byte":
                    valueSingle = (Single)((Byte)value);
                    return valueSingle;
                case "System.Single = System.SByte":
                    valueSingle = (Single)((SByte)value);
                    return valueSingle;
                case "System.Single = System.Int16":
                    valueSingle = (Single)((Int16)value);
                    return valueSingle;
                case "System.Single = System.Int32":
                    valueSingle = (Single)((Int32)value);
                    return valueSingle;
                case "System.Single = System.Int64":
                    valueSingle = (Single)((Int64)value);
                    return valueSingle;
                case "System.Single = System.UInt16":
                    valueSingle = (Single)((UInt16)value);
                    return valueSingle;
                case "System.Single = System.UInt32":
                    valueSingle = (Single)((UInt32)value);
                    return valueSingle;
                case "System.Single = System.UInt64":
                    valueSingle = (Single)((UInt64)value);
                    return valueSingle;
                case "System.Single = System.Double":
                    valueSingle = (Single)((Double)value);
                    return valueSingle;
                case "System.Single = System.Decimal":
                    valueSingle = (Single)((Decimal)value);
                    return valueSingle;

                //   Cílový typ    = Zdrojový typ
                case "System.Double = System.Byte":
                    valueDouble = (Double)((Byte)value);
                    return valueDouble;
                case "System.Double = System.SByte":
                    valueDouble = (Double)((SByte)value);
                    return valueDouble;
                case "System.Double = System.Int16":
                    valueDouble = (Double)((Int16)value);
                    return valueDouble;
                case "System.Double = System.Int32":
                    valueDouble = (Double)((Int32)value);
                    return valueDouble;
                case "System.Double = System.Int64":
                    valueDouble = (Double)((Int64)value);
                    return valueDouble;
                case "System.Double = System.UInt16":
                    valueDouble = (Double)((UInt16)value);
                    return valueDouble;
                case "System.Double = System.UInt32":
                    valueDouble = (Double)((UInt32)value);
                    return valueDouble;
                case "System.Double = System.UInt64":
                    valueDouble = (Double)((UInt64)value);
                    return valueDouble;
                case "System.Double = System.Single":
                    valueDouble = (Double)((Single)value);
                    return valueDouble;
                case "System.Double = System.Decimal":
                    valueDouble = (Double)((Decimal)value);
                    return valueDouble;

                //   Cílový typ    = Zdrojový typ
                case "System.Decimal = System.Byte":
                    valueDecimal = (Decimal)((Byte)value);
                    return valueDecimal;
                case "System.Decimal = System.SByte":
                    valueDecimal = (Decimal)((SByte)value);
                    return valueDecimal;
                case "System.Decimal = System.Int16":
                    valueDecimal = (Decimal)((Int16)value);
                    return valueDecimal;
                case "System.Decimal = System.Int32":
                    valueDecimal = (Decimal)((Int32)value);
                    return valueDecimal;
                case "System.Decimal = System.Int64":
                    valueDecimal = (Decimal)((Int64)value);
                    return valueDecimal;
                case "System.Decimal = System.UInt16":
                    valueDecimal = (Decimal)((UInt16)value);
                    return valueDecimal;
                case "System.Decimal = System.UInt32":
                    valueDecimal = (Decimal)((UInt32)value);
                    return valueDecimal;
                case "System.Decimal = System.UInt64":
                    valueDecimal = (Decimal)((UInt64)value);
                    return valueDecimal;
                case "System.Decimal = System.Single":
                    valueDecimal = (Decimal)((Single)value);
                    return valueDecimal;
                case "System.Decimal = System.Double":
                    valueDecimal = (Decimal)((Double)value);
                    return valueDecimal;
            }

            if (targetName == "System.String")
                return value.ToString();

            return value;
        }
        #endregion
    }
}
