using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Djs.Tools.WebDownloader
{
    #region WebData
    /// <summary>
    /// Bázová třída pro data. Implementuje možnosti zápisu a čtení ze souboru/streamu.
    /// </summary>
    public class WebData
    {
        #region Load & Save
        /// <summary>
        /// Do streamu vepíše daný text v aktuálním kódování
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="line"></param>
        protected static void SaveLine(Stream stream, string line)
        {
            byte[] data = WebData.Encoding.GetBytes(line);
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
            return WebData.Encoding.GetString(data);
        }
        /// <summary>
        /// Vytvoří text "name key1:value1,key2:value2 ..."
        /// </summary>
        /// <param name="name"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected static string CreateLine(string name, List<KeyValuePair<string, string>> list)
        {
            string result = name + " ";
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
        /// Z dodaného textu řádku separuje název a seznam klíčů a hodnot
        /// </summary>
        /// <param name="line"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected static string LoadFromString(string line, out List<KeyValuePair<string, string>> list)
        {
            list = null;
            if (string.IsNullOrEmpty(line)) return "";
            list = new List<KeyValuePair<string, string>>();
            int pos = line.IndexOf(" ");
            if (pos < 0) return line;                // Bez mezery: celý řádek je Name, a neřeším List
            string name = line.Substring(0, pos);

            list = LoadFromString(line.Substring(pos + 1));

            return name;
        }
        /// <summary>
        /// Z dodaného textu řádku separuje název a seznam klíčů a hodnot
        /// </summary>
        /// <param name="line"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected static List<KeyValuePair<string, string>> LoadFromString(string line)
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
            KeyValuePair<string, string> kvp = list.FirstOrDefault(kv => kv.Key == name);
            if (kvp.Key != name) return defValue;
            return kvp.Value;
        }
        /// <summary>
        /// Převede text na string hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static string GetValue(string text, string defValue)
        {
            if (text == null) return defValue;
            return text;
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí int hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static int GetValue(List<KeyValuePair<string, string>> list, string name, int defValue)
        {
            KeyValuePair<string, string> kvp = list.FirstOrDefault(kv => kv.Key == name);
            if (kvp.Key != name) return defValue;
            int value;
            if (!Int32.TryParse(kvp.Value, out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Převede text na int hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static int GetValue(string text, int defValue)
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            int value;
            if (!Int32.TryParse(text, out value)) return defValue;
            return value;
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static long GetValue(List<KeyValuePair<string, string>> list, string name, long defValue)
        {
            KeyValuePair<string, string> kvp = list.FirstOrDefault(kv => kv.Key == name);
            if (kvp.Key != name) return defValue;
            long value;
            if (!Int64.TryParse(kvp.Value, out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Převede text na long hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static long GetValue(string text, long defValue)
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            long value;
            if (!Int64.TryParse(text, out value)) return defValue;
            return value;
        }
        /// <summary>
        /// V dodaném seznamu hodnot najde klíč a vrátí bool hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static bool GetValue(List<KeyValuePair<string, string>> list, string name, bool defValue)
        {
            KeyValuePair<string, string> kvp = list.FirstOrDefault(kv => kv.Key == name);
            if (kvp.Key != name) return defValue;
            string value = kvp.Value.ToLower();
            if (value == "y" || value == "a" || value == "yes" || value == "ano" || value == "1") return true;
            if (value == "n" || value == "n" || value == "no" || value == "ne" || value == "0") return false;
            return defValue;
        }
        /// <summary>
        /// Převede text na bool hodnotu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        protected static bool GetValue(string text, bool defValue)
        {
            if (String.IsNullOrEmpty(text)) return defValue;
            string value = text.ToLower();
            if (value == "y" || value == "a" || value == "yes" || value == "ano" || value == "1") return true;
            if (value == "n" || value == "n" || value == "no" || value == "ne" || value == "0") return false;
            return defValue;
        }
        /// <summary>
        /// :
        /// </summary>
        protected const string KEYVALUE_SEPARATOR = ":";
        /// <summary>
        /// ,
        /// </summary>
        protected const string ITEMS_SEPARATOR = ",";
        internal static Encoding Encoding { get { return Encoding.UTF8; } }
        #endregion
    }
    #endregion
}
