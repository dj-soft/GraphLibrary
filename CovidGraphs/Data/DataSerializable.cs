using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Djs.Tools.CovidGraphs.Data
{
    /// <summary>
    /// Bázová třída pro data. Implementuje možnosti zápisu a čtení ze souboru/streamu.
    /// </summary>
    public class DataSerializable
    {
        #region Load & Save
        protected static StreamReader CreateReadStream(string fileName)
        {
            return new StreamReader(fileName, DataSerializable.Encoding);
        }
        protected static StreamWriter CreateWriteStream(string fileName)
        {
            return new StreamWriter(fileName, false, DataSerializable.Encoding);
        }
        /// <summary>
        /// Do streamu vepíše daný text v aktuálním kódování
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="line"></param>
        protected static void SaveLine(Stream stream, string line)
        {
            byte[] data = DataSerializable.Encoding.GetBytes(line);
            stream.Write(data, 0, data.Length);
            stream.WriteByte(0x0D);
            stream.WriteByte(0x0A);
        }
        /// <summary>
        /// Ze streamu načte řádek. Pokud je stream na konci, vrací null.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected static string LoadLine(Stream stream)
        {
            if (!stream.CanRead || stream.Position >= stream.Length) return null;
            List<byte> buffer = new List<byte>();
            while (stream.Position < stream.Length)
            {
                int b = stream.ReadByte();
                if (b < 0) break;
                if (b == 0x0A) continue;                     // LF vynechávám
                if (b == 0x0D && buffer.Count > 0) break;    // CR pokud už mám data ukončuje čtení
                buffer.Add((byte)b);
            }
            byte[] data = buffer.ToArray();
            return DataSerializable.Encoding.GetString(data);
        }
        /// <summary>
        /// Vytvoří text "name value"
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string CreateLine(string name, string value)
        {
            string result = name + NAMEITEMS_SEPARATOR + value;
            return result;
        }
        /// <summary>
        /// Vytvoří text "name key1:value1,key2:value2 ..."
        /// </summary>
        /// <param name="name"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected static string CreateLine(string name, List<KeyValuePair<string, string>> list)
        {
            string result = name + NAMEITEMS_SEPARATOR;
            foreach (var pair in list)
                result += CreatePair(pair.Key, pair.Value, true);
            result = result.Substring(0, result.Length - ITEMS_SEPARATOR.Length);            // Odeberu poslední oddělovač
            return result;
        }
        /// <summary>
        /// Vrátí string: "Key:value,"
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string CreatePair(string key, bool value, bool addSeparator)
        {
            return CreatePair(key, (value ? "Yes" : "No"), addSeparator);
        }
        /// <summary>
        /// Vrátí string: "Key:value,"
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string CreatePair(string key, int value, bool addSeparator)
        {
            return CreatePair(key, value.ToString(), addSeparator);
        }
        /// <summary>
        /// Vrátí string: "Key:value,"
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string CreatePair(string key, string value, bool addSeparator)
        {
            return key + KEYVALUE_SEPARATOR + value + (addSeparator ? ITEMS_SEPARATOR : "");
        }
        /// <summary>
        /// mezera
        /// </summary>
        protected const string NAMEITEMS_SEPARATOR = " ";
        /// <summary>
        /// :
        /// </summary>
        protected const string KEYVALUE_SEPARATOR = ":";
        /// <summary>
        /// ,
        /// </summary>
        protected const string ITEMS_SEPARATOR = ",";
        internal static Encoding Encoding { get { return Encoding.UTF8; } }
        /// <summary>
        /// Vrátí text vycházející ze vstupního jména (souboru), v němž jsou nepovolené znaky nahrazeny daným textem, což může být i prázdný string.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="replaceText"></param>
        /// <returns></returns>
        protected static string CreateValidFileName(string name, string replaceText = "_")
        {
            if (String.IsNullOrEmpty(name)) return "";

            Dictionary<char, char> invalidChars = System.IO.Path.GetInvalidFileNameChars().ToDictionary<char, char>(c => c);
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if (!invalidChars.ContainsKey(c))
                    sb.Append(c);
                else
                    sb.Append(replaceText);
                if (sb.Length >= 128) break;
            }
            return sb.ToString();
        }
        #endregion
        #region Serializace a pole hodnot
        /// <summary>
        /// Z dodaného textu řádku separuje název a hodnotu za ním
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string LoadNameValueFromString(string line, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(line)) return "";

            value = "";
            int pos = line.IndexOf(NAMEITEMS_SEPARATOR);
            if (pos < 0) return line;                          // Bez mezery: celý řádek je Name, a neřeším List

            string name = line.Substring(0, pos);
            value = line.Substring(pos + 1);                   // S mezerou: máme jméno, a za mezerou očekáváme hodnotu
            return name;
        }
        /// <summary>
        /// Z dodaného textu řádku separuje název a seznam klíčů a hodnot
        /// </summary>
        /// <param name="line"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected static string LoadNameValuesFromString(string line, out List<KeyValuePair<string, string>> list)
        {
            list = null;
            string name = LoadNameValueFromString(line, out string value);
            if (name == null) return name;                    // Pokud není jméno, pak i list je null

            list = new List<KeyValuePair<string, string>>();
            if (String.IsNullOrEmpty(value)) return name;     // Pokud máme jméno, ale nemáme hodnotu (za mezerou), pak list bude prázdný seznam, ale ne null

            list = LoadValuesFromString(value);               // S mezerou: máme jméno, a za mezerou očekáváme list
            return name;
        }
        /// <summary>
        /// Z dodaného textu řádku separuje název a seznam klíčů a hodnot
        /// </summary>
        /// <param name="line"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected static List<KeyValuePair<string, string>> LoadValuesFromString(string line)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

            string[] items = line.Split(new string[] { ITEMS_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {   // Páry oddělené čárkami
                int pos = item.IndexOf(KEYVALUE_SEPARATOR);
                if (pos < 0)
                    list.Add(new KeyValuePair<string, string>(item, ""));
                else
                    list.Add(new KeyValuePair<string, string>(item.Substring(0, pos), item.Substring(pos + 1)));
            }

            return list;
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static string GetValue(List<KeyValuePair<string, string>> list, string name, string defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static bool GetValue(List<KeyValuePair<string, string>> list, string name, bool defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static bool? GetValue(List<KeyValuePair<string, string>> list, string name, bool? defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static int GetValue(List<KeyValuePair<string, string>> list, string name, int defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static int? GetValue(List<KeyValuePair<string, string>> list, string name, int? defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static long GetValue(List<KeyValuePair<string, string>> list, string name, long defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static long? GetValue(List<KeyValuePair<string, string>> list, string name, long? defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static DateTime GetValue(List<KeyValuePair<string, string>> list, string name, DateTime defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static DateTime? GetValue(List<KeyValuePair<string, string>> list, string name, DateTime? defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static System.Drawing.Color GetValue(List<KeyValuePair<string, string>> list, string name, System.Drawing.Color defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static System.Drawing.Color? GetValue(List<KeyValuePair<string, string>> list, string name, System.Drawing.Color? defValue)
        {
            if (!TryFindValue(list, name, out string text)) return defValue;
            return GetValue(text, defValue);
        }
        /// <summary>
        /// Zkusí najít položku daného jména a její hodnotu
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        protected static bool TryFindValue(List<KeyValuePair<string, string>> list, string name, out string text)
        {
            text = null;
            int index = list.FindIndex(kvp => kvp.Key == name);
            bool found = (index >= 0);
            if (found) text = list[index].Value;
            return found;
        }
        #endregion
        #region Jednoduché konverze pro serializaci: GetSerial() = do XML,  GetValue() = z XML
        /// <summary>
        /// Převede string hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(string value)
        {
            if (value == null) return "";
            if (value.Contains('\r')) value = value.Replace("\r", "«r»");
            if (value.Contains('\n')) value = value.Replace("\n", "«n»");
            if (value.Contains('\t')) value = value.Replace("\t", "«t»");
            if (value.Contains(';')) value = value.Replace(";", "«s»");
            if (value.Contains(':')) value = value.Replace(":", "«d»");
            if (value.Contains(',')) value = value.Replace(",", "«c»");
            return value;
        }
        /// <summary>
        /// Převede serialtext na string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static string GetValue(string text, string defValue)
        {
            if (text == null) return defValue;
            if (text.Contains("«r»")) text = text.Replace("«r»", "\r");
            if (text.Contains("«n»")) text = text.Replace("«n»", "\n");
            if (text.Contains("«t»")) text = text.Replace("«t»", "\t");
            if (text.Contains("«s»")) text = text.Replace("«s»", ";");
            if (text.Contains("«d»")) text = text.Replace("«d»", ":");
            if (text.Contains("«c»")) text = text.Replace("«c»", ",");
            return text;
        }
        /// <summary>
        /// Převede string hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(bool value)
        {
            return GetSerial((bool?)value);
        }
        /// <summary>
        /// Převede serialtext na bool hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static bool GetValue(string text, bool defValue)
        {
            bool? result = GetValue(text, (bool?)null);
            return (result ?? defValue);
        }
        /// <summary>
        /// Převede string hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(bool? value)
        {
            if (!value.HasValue) return SerialNull;
            return (value.Value ? "True" : "False");
        }
        /// <summary>
        /// Převede serialtext na bool? hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static bool? GetValue(string text, bool? defValue)
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            if (text == SerialNull) return defValue;
            string value = text.ToLower();
            if (value == "true" || value == "y" || value == "a" || value == "yes" || value == "ano" || value == "1") return true;
            if (value == "false" || value == "n" || value == "n" || value == "no" || value == "ne" || value == "0") return false;
            return defValue;
        }
        /// <summary>
        /// Převede int hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(int value)
        {
            return GetSerial((int?)value);
        }
        /// <summary>
        /// Převede serialtext na int hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static int GetValue(string text, int defValue)
        {
            int? result = GetValue(text, (int?)null);
            return (result ?? defValue);
        }
        /// <summary>
        /// Převede int hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(int? value)
        {
            if (!value.HasValue) return SerialNull;
            return value.Value.ToString();
        }
        /// <summary>
        /// Převede serialtext na int hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static int? GetValue(string text, int? defValue)
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            if (text == SerialNull) return defValue;
            int value;
            if (!Int32.TryParse(text, out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Převede int hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(long value)
        {
            return GetSerial((long?)value);
        }
        /// <summary>
        /// Převede serialtext na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static long GetValue(string text, long defValue)
        {
            long? result = GetValue(text, (long?)null);
            return (result ?? defValue);
        }
        /// <summary>
        /// Převede int hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(long? value)
        {
            if (!value.HasValue) return SerialNull;
            return value.Value.ToString();
        }
        /// <summary>
        /// Převede serialtext na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static long? GetValue(string text, long? defValue)
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            if (text == SerialNull) return defValue;
            long value;
            if (!Int64.TryParse(text, out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Převede DateTime hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(DateTime value)
        {
            return GetSerial((DateTime?)value);
        }
        /// <summary>
        /// Převede serialtext na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static DateTime GetValue(string text, DateTime defValue)
        {
            DateTime? result = GetValue(text, (DateTime?)null);
            return (result ?? defValue);
        }
        /// <summary>
        /// Převede DateTime hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(DateTime? value)
        {
            if (!value.HasValue) return SerialNull;
            return value.Value.ToString("yyyyMMdd.HHmmssfff");
            // "yyyyMMdd.HHmmssfff"
        }
        /// <summary>
        /// Převede serialtext na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static DateTime? GetValue(string text, DateTime? defValue)
        {
            // "yyyyMMdd.HHmmssfff"
            if (String.IsNullOrEmpty(text)) return defValue;
            if (text == SerialNull) return defValue;
            if (text.Length != 18) return defValue;
            if (Int32.TryParse(text.Substring(0, 4), out int dy) &&
                Int32.TryParse(text.Substring(4, 2), out int dm) &&
                Int32.TryParse(text.Substring(6, 2), out int dd) &&
                Int32.TryParse(text.Substring(9, 2), out int th) &&
                Int32.TryParse(text.Substring(11, 2), out int tm) &&
                Int32.TryParse(text.Substring(13, 2), out int ts) &&
                Int32.TryParse(text.Substring(15, 3), out int tf))
            {
                if (dy >= 1000 && dy < 2500 &&
                    dm >= 1 && dm < 13 &&
                    dd >= 1 && dd < 32 &&
                    th >= 0 && th < 24 &&
                    tm >= 0 && tm < 60 &&
                    ts >= 0 && ts < 60 &&
                    tf >= 0 && tf < 999 &&
                    dd <= DateTime.DaysInMonth(dy, dm))
                {
                    return new DateTime(dy, dm, dd, th, tm, ts, tf);
                }
            }
            return defValue;
        }
        /// <summary>
        /// Převede DateTime hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(System.Drawing.Color value)
        {
            return GetSerial((System.Drawing.Color?)value);
        }
        /// <summary>
        /// Převede serialtext na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static System.Drawing.Color GetValue(string text, System.Drawing.Color defValue)
        {
            System.Drawing.Color? result = GetValue(text, (System.Drawing.Color?)null);
            return (result ?? defValue);
        }
        /// <summary>
        /// Převede DateTime hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerial(System.Drawing.Color? value)
        {
            if (!value.HasValue) return SerialNull;
            var color = value.Value;
            return color.A.ToString("000") + "." + value.Value.R.ToString("000") + "." + value.Value.G.ToString("000") + "." + value.Value.B.ToString("000");
            //  "AAA.RRR.GGG.BBB"
        }
        /// <summary>
        /// Převede serialtext na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static System.Drawing.Color? GetValue(string text, System.Drawing.Color? defValue)
        {
            //  "AAA.RRR.GGG.BBB"
            if (String.IsNullOrEmpty(text)) return defValue;
            if (text == SerialNull) return defValue;
            if (text.Length != 15) return defValue;
            if (Int32.TryParse(text.Substring(0, 3), out int ca) &&
                Int32.TryParse(text.Substring(4, 3), out int cr) &&
                Int32.TryParse(text.Substring(8, 3), out int cg) &&
                Int32.TryParse(text.Substring(12, 3), out int cb))
            {
                if (ca >= 0 && ca < 256 &&
                    cr >= 0 && cr < 256 &&
                    cg >= 0 && cg < 256 &&
                    cb >= 0 && cb < 256)
                {
                    return System.Drawing.Color.FromArgb(ca, cr, cg, cb);
                }
            }
            return defValue;
        }
        /// <summary>
        /// Převede Enum hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerialEnum<TEnum>(TEnum value) where TEnum : struct
        {
            return GetSerialEnum((TEnum?)value);
        }
        /// <summary>
        /// Převede serialtext na Enum hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static TEnum GetValueEnum<TEnum>(string text, TEnum defValue) where TEnum : struct
        {
            TEnum? result = GetValueEnum(text, (TEnum?)null);
            return (result ?? defValue);
        }
        /// <summary>
        /// Převede Enum hodnotu do serialtextu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetSerialEnum<TEnum>(TEnum? value) where TEnum : struct
        {
            if (!value.HasValue) return SerialNull;
            return Enum.GetName(typeof(TEnum), value);
        }
        /// <summary>
        /// Převede serialtext na Enum hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static TEnum? GetValueEnum<TEnum>(string text, TEnum? defValue) where TEnum : struct
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            if (text == SerialNull) return defValue;
            if (Enum.TryParse<TEnum>(text, out TEnum value)) return value;
            return defValue;
        }



        protected const string SerialNull = "N";
        #endregion
    }
}
