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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentType"></param>
        /// <param name="expectedType"></param>
        /// <returns></returns>
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
        /// Items can be trimmed, empty rows can be removed from text, by parameters.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="removeEmptyLines"></param>
        /// <param name="trimRows"></param>
        /// <returns></returns>
        public static string[] ToLines(this string text, bool removeEmptyLines = false, bool trimRows = false)
        {
            string[] rows = new string[0];
            if (text != null)
            {
                text = text
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");
                rows = text.Split(new char[] { '\r' }, (removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None));
                if (trimRows)
                    rows = rows.Select(i => i.Trim()).ToArray();
            }
            return rows;
        }
        /// <summary>
        /// Rozdělí daný string na pole polí, při použití standardních oddělovačů formátu CSV: 
        /// sloupce jsou odděleny TAB, řádky jsou odděleny Cr a/nebo Lf.
        /// </summary>
        /// <param name="text">Vstupující text. Obsahuje řádky, a v řádku obsahuje prvky oddělené daným stringem.</param>
        /// <returns></returns>
        public static string[][] ToTableCsv(this string text)
        {
            return text.ToTable("\r\n", "\t", false, false);
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
            if (text != null)
            {
                string[] lines = text.Split(new string[] { rowSeparator }, (removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None));
                foreach (string line in lines)
                {
                    string[] items = line.Split(new string[] { itemSeparator }, StringSplitOptions.None);
                    if (trimItems)
                        result.Add(items.Select(s => s.Trim()).ToArray());
                    else
                        result.Add(items);
                }
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
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            if (text != null)
            {
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
            }
            return result.ToArray();
        }
        private static string _ITrim(string value, bool trim)
        {
            return (value == null ? null : (trim ? value.Trim() : value));
        }
        /// <summary>
        /// Vrátí true, pokud daný text obsahuje pouze povolené znaky z druhého parametru.
        /// Pokud je daný text (nebo povolené znaky) null nebo empty, vrací false.
        /// Povolené znaky nesmí obsahovat duplicitu, jinak dojde k chybě.
        /// Jakýkoli jeden nepovolený znak v textu vede k výsledku false.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="enabledChars"></param>
        /// <returns></returns>
        public static bool ContainsOnlyEnabledChars(this string text, string enabledChars)
        {
            if (String.IsNullOrEmpty(text)) return false;
            if (String.IsNullOrEmpty(enabledChars)) return false;
            Dictionary<char, char> enabledDict = enabledChars.ToCharArray().ToDictionary(c => c);
            return text.ToCharArray().All(c => enabledDict.ContainsKey(c));         // All()
        }
        /// <summary>
        /// Vrátí true, pokud daný text obsahuje alespoň jeden ze zadaných znaků z druhého parametru.
        /// Pokud je daný text (nebo povolené znaky) null nebo empty, vrací false.
        /// Povolené znaky nesmí obsahovat duplicitu, jinak dojde k chybě.
        /// Jakýkoli jeden hledaný znak v textu vede k výsledku true.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="enabledChars"></param>
        /// <returns></returns>
        public static bool ContainsAnyFromChars(this string text, string enabledChars)
        {
            if (String.IsNullOrEmpty(text)) return false;
            if (String.IsNullOrEmpty(enabledChars)) return false;
            Dictionary<char, char> enabledDict = enabledChars.ToCharArray().ToDictionary(c => c);
            return text.ToCharArray().Any(c => enabledDict.ContainsKey(c));         // Any()
        }
        /// <summary>
        /// Vrátí true, pokud daný text vypadá jako číslo.
        /// Číslo nemá obsahovat mezery ani jiné znaky kromě číslic.
        /// Volitelně smí obsahovat na první pozici pomlčku = mínus, a volitelně smí obsahovat jednu desetinnou tečku.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="enableNegative"></param>
        /// <param name="enableDecimal"></param>
        /// <returns></returns>
        public static bool ContainsOnlyNumeric(this string text, bool enableNegative = false, bool enableDecimal = false)
        {
            if (String.IsNullOrEmpty(text)) return false;
            string test = text;
            if (test[0] == '-')
            {   // Záporné číslo: pro nepovolené vrátím false, nebo odeberu mínus na počátku:
                if (!enableNegative) return false;
                test = test.Substring(1);
                if (test.Length == 0) return false;
                if (test.Contains('-')) return false;
            }
            int indexOfDot = test.IndexOf('.');
            if (indexOfDot >= 0)
            {   // Desetinné číslo: pro nepovolené vrátím false, nebo odeberu první nalezenou tečku:
                if (!enableDecimal) return false;
                test = test.Remove(indexOfDot, 1);
                if (test.Length == 0) return false;
                if (test.Contains('.')) return false;
            }
            return ContainsAnyFromChars(test, "0123456789");
        }
        #endregion
        #region DateTime, TimeSpan
        /// <summary>
        /// Metoda vrátí dané datum (this), kde daná část (a části nižší) jsou nahrazeny hodnotou 0.
        /// Tedy pokud part = <see cref="DateTimePart.Minutes"/>, pak výsledek obsahuje hodiny, ale místo minut už je 0 (a rovněž sekundy a milisekundy jsou 0).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="part">Odříznutá část. Smí být pouze: Miliseconds, Seconds, Minutes, Hours. Jiné jsou ignorovány.</param>
        /// <returns></returns>
        public static DateTime TrimPart(this DateTime value, DateTimePart part)
        {
            DateTimeKind vk = value.Kind;
            long ticks = value.Ticks;
            int dy = value.Year;
            int dm = value.Month;
            int dd = value.Day;
            int th = value.Hour;
            int tm = value.Minute;
            int ts = value.Second;
            int tf = value.Millisecond;
            switch (part)
            {
                case DateTimePart.Hours: return new DateTime(dy, dm, dd, 0, 0, 0, 0, vk);
                case DateTimePart.Minutes: return new DateTime(dy, dm, dd, th, 0, 0, 0, vk);
                case DateTimePart.Seconds: return new DateTime(dy, dm, dd, th, tm, 0, 0, vk);
                case DateTimePart.Miliseconds: return new DateTime(dy, dm, dd, th, tm, ts, 0, vk);
            }
            return new DateTime(ticks, vk);
        }
        /// <summary>
        /// Vrátí první den daného úseku. Vrácený čas = 00:00.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public static DateTime FirstDayOf(this DateTime value, DateTimePart part)
        {
            DateTimeKind vk = value.Kind;
            long ticks = value.Ticks;
            int dy = value.Year;
            int dm = value.Month;
            int dd = value.Day;
            int dw = (int)value.DayOfWeek;       // Pondělí = 1, Sobota = 6, Neděle = 0
            if (dw == 0) dw = 7;                 // Pondělí = 1, Sobota = 6, Neděle = 7
            switch (part)
            {
                case DateTimePart.Day:
                    return new DateTime(dy, dm, dd, 0, 0, 0, 0, vk);
                case DateTimePart.Week:
                    if (dw == 1)
                        return new DateTime(dy, dm, dd, 0, 0, 0, 0, vk);
                    return new DateTime(dy, dm, dd, 0, 0, 0, 0, vk).AddDays(1 - dw);
                case DateTimePart.Decade:
                    dd = 1 + 10 * ((dd - 1) / 10);    // Pro dny {1-10} = 1, pro {11-20} = 11, pro {21-30} = 21, pro {31} = 31
                    if (dd >= 30) dd = 21;            // Čtvrtá dekáda (den 31) není, přidává se k dekádě {21-31}
                    return new DateTime(dy, dm, dd, 0, 0, 0, 0, vk);
                case DateTimePart.Month:
                    return new DateTime(dy, dm, 1, 0, 0, 0, 0, vk);
                case DateTimePart.Quarter:
                    dm = 1 + 3 * ((dm - 1) / 3);
                    return new DateTime(dy, dm, 1, 0, 0, 0, 0, vk);
                case DateTimePart.HalfYear:
                    dm = 1 + 6 * ((dm - 1) / 6);
                    return new DateTime(dy, dm, 1, 0, 0, 0, 0, vk);
                case DateTimePart.Year:
                    return new DateTime(dy, 1, 1, 0, 0, 0, 0, vk);
            }
            return new DateTime(dy, dm, dd, 0, 0, 0, 0, vk);
        }
        /// <summary>
        /// Metoda vrací hodnotu, vypočtenou zaokrouhlením this data na daný časový úsek (round).
        /// Časový úsek má být nejvýše roven 1 dni, proto se metoda jmenuje <see cref="RoundTime(DateTime, TimeSpan)"/>.
        /// <para/>
        /// Například hodnotu {2018-09-25 10:14:36.245} pro round = 30 minut zaokrouhlí na {2018-09-25 10:00:00.000}.
        /// Například hodnotu {2018-09-25 10:14:36.245} pro round = 5 minut zaokrouhlí na {2018-09-25 10:15:00.000}.
        /// Například hodnotu {2018-09-25 23:12:45.740} pro round = 2 hodiny zaokrouhlí na {2018-09-26 00:00:00.000}.
        /// <para/>
        /// Zaokrouhlovací úsek je možno získat pomocí (extension) metody TimeSpan.GetRoundTimeBase().
        /// </summary>
        /// <param name="value"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public static DateTime RoundTime(this DateTime value, TimeSpan round)
        {
            if (round.Days > 0) round = TimeSpan.FromDays(1d);
            DateTime date = value.Date;
            TimeSpan time = value.TimeOfDay;
            int mod = (int)((time.Ticks * 2L) / round.Ticks);        // Dvojnásobek počtu podílů: zadaný čas / zaokrouhlovací jednotka
            if ((mod % 2) == 1) mod++;                               // Tady jsem se vyhnul zaokrouhlování
            long roundTicks = (mod / 2) * round.Ticks;               // Zaokrouhlený zadaný čas, v počtu ticků
            TimeSpan roundTime = TimeSpan.FromTicks(roundTicks);
            return date + roundTime;
        }
        /// <summary>
        /// Metoda vrátí nejbližší vyšší rozumný časový úsek pro zaokrouhlování času, pro aktuální (this) časový úsek.
        /// Aktuální úsek může být vypočten například matematicky, a mít "nezarovnanou" hodnotu, například 03:41:18.458.
        /// Výstupní čas bude nejbližší větší časový úsek, lidsky pochopitelný, například 06:00:00.000.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TimeSpan GetRoundTimeBase(this TimeSpan value)
        {
            if (value.TotalDays >= 1d) return TimeSpan.FromDays(1d);
            double hours = value.TotalHours;
            if (hours > 1d)
            {
                if (hours > 12d) return TimeSpan.FromHours(24d);
                if (hours > 6d) return TimeSpan.FromHours(12d);
                if (hours > 3d) return TimeSpan.FromHours(6d);
                return TimeSpan.FromHours(3d);
            }

            double minutes = value.TotalMinutes;
            if (minutes > 1d)
            {
                if (minutes > 30d) return TimeSpan.FromMinutes(60d);
                if (minutes > 15d) return TimeSpan.FromMinutes(30d);
                if (minutes > 10d) return TimeSpan.FromMinutes(15d);
                if (minutes > 5d) return TimeSpan.FromMinutes(10d);
                return TimeSpan.FromMinutes(5d);
            }

            double seconds = value.TotalSeconds;
            if (seconds > 1d)
            {
                if (seconds > 30d) return TimeSpan.FromSeconds(60d);
                if (seconds > 15d) return TimeSpan.FromSeconds(30d);
                if (seconds > 10d) return TimeSpan.FromSeconds(15d);
                if (seconds > 5d) return TimeSpan.FromSeconds(10d);
                return TimeSpan.FromSeconds(5d);
            }

            double milliSeconds = value.TotalMilliseconds;
            if (milliSeconds > 1d)
            {
                if (milliSeconds > 500d) return TimeSpan.FromMilliseconds(1000d);
                if (milliSeconds > 100d) return TimeSpan.FromMilliseconds(500d);
                if (milliSeconds > 50d) return TimeSpan.FromMilliseconds(100d);
                if (milliSeconds > 10d) return TimeSpan.FromMilliseconds(50d);
                if (milliSeconds > 5d) return TimeSpan.FromMilliseconds(10d);
                return TimeSpan.FromMilliseconds(5d);
            }

            return TimeSpan.FromMilliseconds(1d);
        }
        /// <summary>
        /// Vrátí dané datum a čas zformátované pro uživatele.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToUser(this DateTime value, DateTimeFormat format = DateTimeFormat.Default)
        {
            var dtfi = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat;
            string text = "";

            if (format.HasFlag(DateTimeFormat.DateLong))
                text = value.ToString(dtfi.LongDatePattern);
            else if (format.HasFlag(DateTimeFormat.DateShort))
                text = value.ToString(dtfi.ShortDatePattern.Replace("dd","d").Replace("MM","M"));

            string space = (text.Length == 0 ? "" : " ");
            if (format.HasFlag(DateTimeFormat.Seconds) || (format.HasFlag(DateTimeFormat.NonZero) && value.Second != 0))
                text += space + value.ToString("H:mm:ss");
            else if (format.HasFlag(DateTimeFormat.Time))
                text += space + value.ToString("H:mm");

            return text;
        }
        #endregion
        #region Enum
        /// <summary>
        /// Vrátí true, pokud this hodnota (value) obsahuje alespoň jeden nahozený flag z dané sady (flags).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static bool HasAnyFlag<T>(this T value, T flags) where T : IConvertible
        {
            IFormatProvider p = System.Globalization.NumberFormatInfo.CurrentInfo;
            int v = value.ToInt32(p);
            int f = flags.ToInt32(p);
            return ((f & v) != 0);
        }
        /// <summary>
        /// Vrátí true, pokud this hodnota (value) obsahuje alespoň jeden nahozený bit z dané hodnoty (flags).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static bool HasAnyFlag(this int value, int flags)
        {
            return ((value & flags) != 0);
        }
        /// <summary>
        /// Vrátí hodnotu enumu, kde budou shozeny dané flagy.
        /// Výsledek je nutno konvertovat z Int32 na daný enum.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static int RemoveFlags<T>(this T value, T flags) where T : IConvertible
        {
            IFormatProvider p = System.Globalization.NumberFormatInfo.CurrentInfo;
            int v = value.ToInt32(p);
            int f = flags.ToInt32(p);
            int r = v & (Int32.MaxValue ^ f);        // Požadované bity (flags) budou mít hodnotu 0, ostatní zůstanou beze změny
            return r;
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
        /// <summary>
        /// Vrátí nejbližší sousední prvek z kolekce.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="currentItem"></param>
        /// <param name="nearestSide">Směr pro hledání</param>
        /// <returns></returns>
        public static T GetNearestItem<T>(this IEnumerable<T> collection, T currentItem, Data.Direction nearestSide = Direction.Positive)
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
        /// <summary>
        /// Metoda dostává dvě pole prvků stejného typu: this = collectionNew = nové prvky, a collectionOld = staré prvky.
        /// Dále dostává selector klíče (který z prvku pole najde a vrátí jeho unique klíč.
        /// Metoda z těchto dat najde prvky, které jsou nové (tj. jsou v poli collectionNew, ale nejsou v collectionOld), a ty vloží do out pole newItems,
        /// a pak najde prvky, které jsou staré (tj. jsou v poli collectionOld, ale už nejsou v collectionNew), a ty vloží do out pole oldItems.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="collectionNew"></param>
        /// <param name="collectionOld"></param>
        /// <param name="keySelector"></param>
        /// <param name="newItems"></param>
        /// <param name="oldItems"></param>
        public static void GetDifferentialArray<T, TKey>(this IEnumerable<T> collectionNew, IEnumerable<T> collectionOld, Func<T, TKey> keySelector, out T[] newItems, out T[] oldItems)
        {
            Dictionary<TKey, T> newDict = collectionNew.GetDictionary(keySelector, true);
            Dictionary<TKey, T> oldDict = collectionOld.GetDictionary(keySelector, true);

            newItems = newDict.GetNewValues(oldDict);
            oldItems = oldDict.GetNewValues(newDict);
        }
        /// <summary>
        /// Metoda vrátí souhrn prvků, které jsou přítomny v this (currentValues), ale nejsou přítomny v předchozím stavu (oldValues).
        /// Porovnání se provádí pomocí klíče v Dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="currentValues"></param>
        /// <param name="oldValues"></param>
        /// <returns></returns>
        public static TValue[] GetNewValues<TKey, TValue>(this Dictionary<TKey, TValue> currentValues, Dictionary<TKey, TValue> oldValues)
        {
            TValue[] newValues = null;
            if (currentValues != null)
            {
                if (oldValues != null)
                    newValues = currentValues.Where(kvp => !oldValues.ContainsKey(kvp.Key)).Select(kvp => kvp.Value).ToArray();
                else
                    newValues = currentValues.Values.ToArray();
            }
            return newValues;
        }
        /// <summary>
        /// Metoda vrací Dictionary z daných dat, s pomocí keySelectoru.
        /// Umožňuje řídit přeskakování duplicit.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection">Kolekce</param>
        /// <param name="keySelector">Funkce pro získání klíče ze záznamu</param>
        /// <param name="skipDuplicityItems">true = pokud je klíč ve vstupním poli vícekrát, pak ve výstupní Dictionary bude jen jednou (ten první výskyt), ale nedojde k chybě. Hodnota false = při duplicitě dojde k chybě.</param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(this IEnumerable<TValue> collection, Func<TValue, TKey> keySelector, bool skipDuplicityItems)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            if (collection != null)
            {
                foreach (TValue value in collection)
                {
                    TKey key = keySelector(value);
                    if (!skipDuplicityItems)
                        dictionary.Add(key, value);             // Pokud daný klíč už je v Dictionary, dojde k chybě.
                    else if (!dictionary.ContainsKey(key))
                        dictionary.Add(key, value);             // Přeskočit duplicitní položky: přidáváme položku jen když dosud nemáme klíč.
                }
            }
            return dictionary;
        }
        /// <summary>
        /// Metoda z this dictionary odebere všechny výskyty daných klíčů.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static void RemoveKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
        {
            if (dictionary == null || dictionary.Count == 0 || keys == null) return;
            foreach (TKey key in keys)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary.Remove(key);
                    if (dictionary.Count == 0)
                        return;
                }
            }
        }
        /// <summary>
        /// Metoda z this dictionary odebere všechny položky, které vyhovují danému filtru
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="filter">Filtr: dostane hodnotu klíče, vrací: true = odebrat z Dictionary, false = ponechat</param>
        /// <returns></returns>
        public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Func<TKey, TValue, bool> filter)
        {
            if (dictionary == null || dictionary.Count == 0 || filter == null) return;
            KeyValuePair<TKey, TValue>[] pairs = dictionary.ToArray();  // Pole prvků, přítomných v Dictionary, musí se získat do izolovaného pole,
            foreach (KeyValuePair<TKey, TValue> pair in pairs)          //  protože tento soupis klíčů budeme enumerovat, a přitom z Dictionary budeme odebírat prvky,
            {                                                           //  což by způsobilo chybu pokud bychom enumerovali přímo Dictionary.
                if (filter(pair.Key, pair.Value))
                {
                    dictionary.Remove(pair.Key);                        // Odebereme záznam, pro jehož klíč bylo vráceno true z filtru.
                    if (dictionary.Count == 0)
                        return;
                }
            }
        }
        /// <summary>
        /// Metoda do this Dictionary přidá nové prvky z pole items (tj. ty, které tam dosud nejsou), na základě klíče prvku, vytvořeného pomocí funkce (keySelector).
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        public static void AddNewItems<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TValue> items, Func<TValue, TKey> keySelector)
        {
            if (dictionary == null || items == null || keySelector == null) return;
            foreach (TValue item in items)
            {
                if (item == null) continue;
                TKey key = keySelector(item);
                if (key == null) continue;
                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, item);
            }
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
        /// <summary>
        /// Opačný směr
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Data.Direction Reverse(this Data.Direction direction)
        {
            switch (direction)
            {
                case Data.Direction.Positive: return Data.Direction.Negative;
                case Data.Direction.Negative: return Data.Direction.Positive;
            }
            return Data.Direction.None;
        }
        /// <summary>
        /// Číselná hodnota směru Positive = +1; Negative = -1;
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static int NumericValue(this Data.Direction direction)
        {
            switch (direction)
            {
                case Data.Direction.Positive: return 1;
                case Data.Direction.Negative: return -1;
            }
            return 0;
        }
        /// <summary>
        /// Porovnání
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="other"></param>
        /// <returns></returns>
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
            // String valueString;
            // Char valueChar;
            Byte valueByte;
            SByte valueSByte;
            // DateTime valueDateTime;
            // TimeSpan valueTimeSpan;

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
                case "System.UInt16 = System.Byte":
                    valueUInt16 = (UInt16)((Byte)value);
                    return valueUInt16;
                case "System.UInt16 = System.SByte":
                    valueUInt16 = (UInt16)((SByte)value);
                    return valueUInt16;
                case "System.UInt16 = System.Int16":
                    valueUInt16 = (UInt16)((Int16)value);
                    return valueUInt16;
                case "System.UInt16 = System.Int32":
                    valueUInt16 = (UInt16)((Int32)value);
                    return valueUInt16;
                case "System.UInt16 = System.Int64":
                    valueUInt16 = (UInt16)((Int64)value);
                    return valueUInt16;
                case "System.UInt16 = System.UInt32":
                    valueUInt16 = (UInt16)((UInt32)value);
                    return valueUInt16;
                case "System.UInt16 = System.UInt64":
                    valueUInt16 = (UInt16)((UInt64)value);
                    return valueUInt16;

                //   Cílový typ    = Zdrojový typ
                case "System.UInt32 = System.Byte":
                    valueUInt32 = (UInt32)((Byte)value);
                    return valueUInt32;
                case "System.UInt32 = System.SByte":
                    valueUInt32 = (UInt32)((SByte)value);
                    return valueUInt32;
                case "System.UInt32 = System.Int16":
                    valueUInt32 = (UInt32)((Int16)value);
                    return valueUInt32;
                case "System.UInt32 = System.Int32":
                    valueUInt32 = (UInt32)((Int32)value);
                    return valueUInt32;
                case "System.UInt32 = System.Int64":
                    valueUInt32 = (UInt32)((Int64)value);
                    return valueUInt32;
                case "System.UInt32 = System.UInt16":
                    valueUInt32 = (UInt32)((UInt16)value);
                    return valueUInt32;
                case "System.UInt32 = System.UInt64":
                    valueUInt32 = (UInt32)((UInt64)value);
                    return valueUInt32;

                //   Cílový typ    = Zdrojový typ
                case "System.UInt64 = System.Byte":
                    valueUInt64 = (UInt64)((Byte)value);
                    return valueUInt64;
                case "System.UInt64 = System.SByte":
                    valueUInt64 = (UInt64)((SByte)value);
                    return valueUInt64;
                case "System.UInt64 = System.Int16":
                    valueUInt64 = (UInt64)((Int16)value);
                    return valueUInt64;
                case "System.UInt64 = System.Int32":
                    valueUInt64 = (UInt64)((Int32)value);
                    return valueUInt64;
                case "System.UInt64 = System.Int64":
                    valueUInt64 = (UInt64)((Int64)value);
                    return valueUInt64;
                case "System.UInt64 = System.UInt16":
                    valueUInt64 = (UInt64)((UInt16)value);
                    return valueUInt64;
                case "System.UInt64 = System.UInt32":
                    valueUInt64 = (UInt64)((UInt32)value);
                    return valueUInt64;

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

                //   Cílový typ    = Zdrojový typ
                case "System.Byte = System.Int16":
                    valueByte = (Byte)((Int16)value);
                    return valueByte;
                case "System.Byte = System.UInt16":
                    valueByte = (Byte)((UInt16)value);
                    return valueByte;
                case "System.Byte = System.Int32":
                    valueByte = (Byte)((Int16)value);
                    return valueByte;
                case "System.Byte = System.UInt32":
                    valueByte = (Byte)((UInt16)value);
                    return valueByte;

                //   Cílový typ    = Zdrojový typ
                case "System.SByte = System.Int16":
                    valueSByte = (SByte)((Int16)value);
                    return valueSByte;
                case "System.SByte = System.UInt16":
                    valueSByte = (SByte)((UInt16)value);
                    return valueSByte;
                case "System.SByte = System.Int32":
                    valueSByte = (SByte)((Int16)value);
                    return valueSByte;
                case "System.SByte = System.UInt32":
                    valueSByte = (SByte)((UInt16)value);
                    return valueSByte;

            }

            if (targetName == "System.String")
                return value.ToString();

            if (targetName == "System.Char")
            {
                string text = value.ToString();
                if (text.Length > 0) return text[0];
                return '\0';
            }

            return value;
        }
        #endregion
    }
    #region enumy pro DataExtensions
    /// <summary>
    /// Část údaje DateTime
    /// </summary>
    public enum DateTimePart
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Milisekundy
        /// </summary>
        Miliseconds,
        /// <summary>
        /// Sekundy
        /// </summary>
        Seconds,
        /// <summary>
        /// Minuty
        /// </summary>
        Minutes,
        /// <summary>
        /// Hodiny
        /// </summary>
        Hours,
        /// <summary>
        /// Den
        /// </summary>
        Day,
        /// <summary>
        /// Týden
        /// </summary>
        Week,
        /// <summary>
        /// Dekáda
        /// </summary>
        Decade,
        /// <summary>
        /// Měsíc
        /// </summary>
        Month,
        /// <summary>
        /// Čtvrtletí
        /// </summary>
        Quarter,
        /// <summary>
        /// Pololetí
        /// </summary>
        HalfYear,
        /// <summary>
        /// Rok
        /// </summary>
        Year
    }
    /// <summary>
    /// Formátování data a času pro uživatele
    /// </summary>
    [Flags]
    public enum DateTimeFormat
    {
        /// <summary>
        /// Nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Datová část krátká (24.12.2018)
        /// </summary>
        DateShort = 0x10,
        /// <summary>
        /// Datová část dlouhá (24.prosince 2018).
        /// Tento příznak lze přidat k jakékoli hodnotě, vždy zajistí dlouhé datum (i při souběhu s <see cref="DateShort"/>).
        /// </summary>
        DateLong = 0x20,
        /// <summary>
        /// Čas = Hodiny:minuty bez sekund (14:45)
        /// </summary>
        Time = 0x08,
        /// <summary>
        /// Čas včetně sekund, sekundy i když jsou 00 (14:45:00)
        /// </summary>
        Seconds = 0x02,
        /// <summary>
        /// Čas včetně sekund, sekundy jen když nejsou 00 (14:45:30)
        /// </summary>
        NonZero = 0x01,
        /// <summary>
        /// Krátké datum + čas včetně sekund (24.12.2018 14:45:00)
        /// </summary>
        FullDateShortTimeSeconds = DateShort | Time | Seconds,
        /// <summary>
        /// Krátké datum + čas, sekundy jen nenulové (24.12.2018 14:45:30)
        /// </summary>
        Default = DateShort | Time | NonZero

    }
    #endregion
}
