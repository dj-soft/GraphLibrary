using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    /// <summary>
    /// Class for persist an value(s) to/from string
    /// </summary>
    public class DataPersistValue
    {
        #region Private (constructor, values, constants)
        private DataPersistValue()
        {
            this._Name = null;
            this._Values = new List<string>();
        }
        private string _Name;
        private List<string> _Values;
        private void _Clear()
        {
            this._Name = null;
            this._Values.Clear();
        }
        private string _Get(int index) { return ((index >= 0 && index < this.ValueCount) ? this._Values[index] : ""); }
        private const char DELIMITER = '¤';
        private const string NULL = "×Null×";
        /// <summary>
        /// Search in (ref text) for (search) expression (typically: any delimiter).
        /// If (search) is found, then all before (search) is returned as result of this method, and (ref text) is cut on found position.
        /// When (cutAfter) is false, then resulting (ref text) has begin with (search) expression, when (cutAfter) is true, then resulting (ref text) begun after (search) expression.
        /// When (search) is not found, then returned value is null and (ref text) is unchanged.
        /// When (search) is found at index [0] and (cutAfter) is false, then returned value is "" (=non null) and (ref text) is unchanged.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="search"></param>
        /// <param name="cutAfter"></param>
        /// <returns></returns>
        private static string _CutOf(ref string text, string search, bool cutAfter)
        {
            string result = null;
            if (!String.IsNullOrEmpty(text) && !String.IsNullOrEmpty(search))
            {
                int index = text.IndexOf(search);
                if (index >= 0)
                {
                    result = text.Substring(0, index);
                    text = text.Substring(index + (cutAfter ? search.Length : 0));
                }
            }
            return result;
        }
        private static void _Replace(ref string text, char search, char replace)
        {
            if (text.Contains(search))
                text = text.Replace(search, replace);
        }
        private static void _Replace(ref string text, string search, string replace)
        {
            if (text.Contains(search))
                text = text.Replace(search, replace);
        }
        private const char TEMP_CHAR = (char)1234;
        #endregion
        #region Public constructors, properties
        public DataPersistValue(string name)
        {
            this._Name = name;
            this._Values = new List<string>();
        }
        public DataPersistValue(string name, params object[] values)
        {
            this._Name = name;
            this._Values = new List<string>();
            this.AddRange(values);
        }
        /// <summary>
        /// Return a new DataPersistValue instance from 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DataPersistValue FromPersist(string value)
        {
            DataPersistValue dpv = new DataPersistValue();
            dpv.PersistentValue = value;
            return dpv;
        }
        /// <summary>
        /// User name of this persistent
        /// </summary>
        public string Name { get { return this._Name; } set { this._Name = value; } }
        /// <summary>
        /// Get the number of actually stored values
        /// </summary>
        public int ValueCount { get { return this._Values.Count; } }
        /// <summary>
        /// Returns true, when current instance has specified name, and at least specified number of values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool IsValid(string name, int count)
        {
            if (name != null && !String.Equals(this.Name, name, StringComparison.CurrentCultureIgnoreCase)) return false;
            if (this.ValueCount < count) return false;
            return true;
        }
        /// <summary>
        /// A new empty persistor
        /// </summary>
        public static DataPersistValue Empty { get { return new DataPersistValue(); } }
        /// <summary>
        /// String value of all values.
        /// </summary>
        public string PersistentValue
        {
            get
            {
                string text = "";
                if (!String.IsNullOrEmpty(this.Name))
                    text += DataPersistValue.ToPersist(this.Name);
                text += "{";
                for (int i = 0; i < this.ValueCount; i++)
                    text += (i == 0 ? "" : DELIMITER.ToString()) + this._Values[i];
                text += "}";
                return text;
            }
            set
            {
                this._Clear();

                if (!String.IsNullOrEmpty(value))
                {
                    string text = value.Trim();
                    string name = _CutOf(ref text, "{", false);
                    int length = text.Length;
                    if (name != null && length >= 2 && text[0] == '{' && text[length - 1] == '}')
                    {
                        text = text.Substring(1, length - 2);
                        this._Name = name;
                        this._Values = text.Split(DELIMITER).ToList();
                    }
                }
            }
        }
        #endregion
        #region AddRange(), Add(), GetType()
        /// <summary>
        /// Add a new values
        /// </summary>
        /// <param name="value"></param>
        public void AddRange(params object[] values)
        {
            foreach (object value in values)
            {
                if (value == null)
                    this._Values.Add(NULL);
                else
                {
                    string name = value.GetType().NsName();
                    switch (name)
                    {
                        case "System.String":
                            this.Add((String)value);
                            break;
                        case "System.Boolean":
                            this.Add((Boolean)value);
                            break;
                        case "System.Int16":
                            this.Add((Int16)value);
                            break;
                        case "System.Int32":
                            this.Add((Int32)value);
                            break;
                        case "System.Int64":
                            this.Add((Int64)value);
                            break;
                        default:
                            if (value is DataPersistValue)
                            {
                                DataPersistValue dpv = value as DataPersistValue;
                                this.Add(dpv);
                            }
                            else if (value is IDataPersistent)
                            {
                                IDataPersistent idp = value as IDataPersistent;
                                this.Add(idp);
                            }
                            else
                            {
                                this.Add(value.ToString());
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(string value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a string value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetString(int index) { return DataPersistValue.ToString(this._Get(index)); }
        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(bool value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a boolean value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool GetBoolean(int index) { return DataPersistValue.ToBoolean(this._Get(index)); }

        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(short value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a short value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public short GetInt16(int index) { return DataPersistValue.ToInt16(this._Get(index)); }
        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(int value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a int value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetInt32(int index) { return DataPersistValue.ToInt32(this._Get(index)); }
        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(long value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a long value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public long GetInt64(int index) { return DataPersistValue.ToInt64(this._Get(index)); }

        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(short? value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a short value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public short? GetInt16N(int index) { return DataPersistValue.ToInt16(this._Get(index)); }
        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(int? value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a int value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int? GetInt32N(int index) { return DataPersistValue.ToInt32(this._Get(index)); }
        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(long? value) { this._Values.Add(DataPersistValue.ToPersist(value)); }
        /// <summary>
        /// Return a long value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public long? GetInt64N(int index) { return DataPersistValue.ToInt64(this._Get(index)); }

        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(DataPersistValue value) { this.Add((value == null ? null : value.PersistentValue)); }
        /// <summary>
        /// Return a DataPersistValue value from specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DataPersistValue GetDataPersist(int index) { return DataPersistValue.FromPersist(this._Get(index)); }
        /// <summary>
        /// Add a new value
        /// </summary>
        /// <param name="value"></param>
        public void Add(IDataPersistent value) { this.Add((value == null ? null : value.PersistentValue)); }

        #endregion
        #region Convertors
        public static string ToPersist(string value)
        {
            if (value == null) return NULL;
            string text = value;

            _Replace(ref text, '&', TEMP_CHAR);
            _Replace(ref text, ";", "&sc,");
            _Replace(ref text, "\t", "&tab,");
            _Replace(ref text, "\r", "&cr,");
            _Replace(ref text, "\n", "&lf,");
            _Replace(ref text, TEMP_CHAR.ToString(), "&amp,");

            return text;
        }
        public static string ToString(string persist)
        {
            if (persist == null) return null;
            if (persist == NULL) return null;
            string text = persist;

            _Replace(ref text, "&amp,", TEMP_CHAR.ToString());
            _Replace(ref text, "&lf,", "\n");
            _Replace(ref text, "&cr,", "\r");
            _Replace(ref text, "&tab,", "\t");
            _Replace(ref text, "&sc,", ";");
            _Replace(ref text, TEMP_CHAR, '&');
            
            return text;
        }
        public static string ToPersist(bool value)
        {
            return (value ? "Y" : "N");
        }
        public static bool ToBoolean(string persist)
        {
            if (!String.IsNullOrEmpty(persist) && ("AaYy1".IndexOf(persist[0]) >= 0)) return true;
            return false;
        }
        public static string ToPersist(short value)
        {
            return value.ToString();
        }
        public static short ToInt16(string persist)
        {
            short value;
            if (!String.IsNullOrEmpty(persist) && Int16.TryParse(persist, out value)) return value;
            return (short)0;
        }
        public static string ToPersist(int value)
        {
            return value.ToString();
        }
        public static int ToInt32(string persist)
        {
            int value;
            if (!String.IsNullOrEmpty(persist) && Int32.TryParse(persist, out value)) return value;
            return 0;
        }
        public static string ToPersist(long value)
        {
            return value.ToString();
        }
        public static long ToInt64(string persist)
        {
            long value;
            if (!String.IsNullOrEmpty(persist) && Int64.TryParse(persist, out value)) return value;
            return 0L;
        }
        public static string ToPersist(short? value)
        {
            return (value.HasValue ? value.Value.ToString() : "");
        }
        public static short? ToInt16N(string persist)
        {
            short value;
            if (!String.IsNullOrEmpty(persist) && Int16.TryParse(persist, out value)) return value;
            return (short?)null;
        }
        public static string ToPersist(int? value)
        {
            return (value.HasValue ? value.Value.ToString() : "");
        }
        public static int? ToInt32N(string persist)
        {
            int value;
            if (!String.IsNullOrEmpty(persist) && Int32.TryParse(persist, out value)) return value;
            return (int?)null;
        }
        public static string ToPersist(long? value)
        {
            return (value.HasValue ? value.Value.ToString() : "");
        }
        public static long? ToInt64N(string persist)
        {
            long value;
            if (!String.IsNullOrEmpty(persist) && Int64.TryParse(persist, out value)) return value;
            return (long?)null;
        }

        #endregion
    }
    /// <summary>
    /// Interface for DataPersistent technology
    /// </summary>
    public interface IDataPersistent
    {
        string PersistentValue { get; set; }
    }
}
