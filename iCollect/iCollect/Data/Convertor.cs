using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Drawing;

namespace DjSoft.App.iCollect.Data
{
    /// <summary>
    /// Convertor : Knihovna statických konverzních metod mezi simple typy a stringem
    /// </summary>
    public static class Convertor
    {
        #region Sada krátkých metod pro serializaci a deserializaci Simple typů (jsou vyjmenované v TypeLibrary._SimpleTypePrepare())
        #region System types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BooleanToString(object value)
        {
            return ((Boolean)value ? "true" : "false");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="textTrue"></param>
        /// <param name="textFalse"></param>
        /// <returns></returns>
        public static string BooleanToString(object value, string textTrue, string textFalse)
        {
            return ((Boolean)value ? textTrue : textFalse);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToBoolean(string text)
        {
            return (!String.IsNullOrEmpty(text) && (text.ToLower() == "true" || text == "1" || text.ToLower() == "a" || text.ToLower() == "y"));
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ByteToString(object value)
        {
            return ((Byte)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Byte)0;
            Int32 value;
            if (!Int32.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (Byte)0;
            Byte b = (Byte)(value & 0x00FF);
            return b;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SByteToString(object value)
        {
            return ((SByte)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (SByte)0;
            SByte value;
            if (!SByte.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (SByte)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int16ToString(object value)
        {
            return ((Int16)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int16)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int16 value;
            if (!Int16.TryParse(text, style, _Nmfi, out value)) return (Int16)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int32ToString(object value)
        {
            return ((Int32)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int32 value;
            if (!Int32.TryParse(text, style, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int64ToString(object value)
        {
            return ((Int64)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int64)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int64 value;
            if (!Int64.TryParse(text, style, _Nmfi, out value)) return (Int64)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string IntPtrToString(object value)
        {
            return ((IntPtr)value).ToInt64().ToString("G");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (IntPtr)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int64 int64;
            if (!Int64.TryParse(text, style, _Nmfi, out int64)) return (IntPtr)0;
            return new IntPtr(int64);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt16ToString(object value)
        {
            return ((UInt16)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt16)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt16 value;
            if (!UInt16.TryParse(text, style, _Nmfi, out value)) return (UInt16)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt32ToString(object value)
        {
            return ((UInt32)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt32)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt32 value;
            if (!UInt32.TryParse(text, style, _Nmfi, out value)) return (UInt32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt64ToString(object value)
        {
            return ((UInt64)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt64)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt64 value;
            if (!UInt64.TryParse(text, style, _Nmfi, out value)) return (UInt64)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UIntPtrToString(object value)
        {
            return ((UIntPtr)value).ToUInt64().ToString("G");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UIntPtr)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt64 uint64;
            if (!UInt64.TryParse(text, style, _Nmfi, out uint64)) return (UIntPtr)0;
            return new UIntPtr(uint64);
        }
        /// <summary>
        /// Vrátí styl pro konverzi textu na číslo, detekuje a řeší HEX prefixy 0x a &amp;h.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static NumberStyles _StringToHexStyle(ref string text)
        {
            var style = NumberStyles.Any;
            if (text.Length > 2)
            {
                string prefix = text.Substring(0, 2).ToLower();
                if (prefix == "0x" || prefix == "&h")
                {
                    text = text.Substring(2);
                    style = NumberStyles.HexNumber;
                }
            }
            return style;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleToString(object value)
        {
            Single number = (Single)value;
            string text = number.ToString("N", _Nmfi);
            if ((number % 1f) == 0f)
            {
                int dot = text.IndexOf('.');
                if (dot > 0)
                    text = text.Substring(0, dot);
            }
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DoubleToString(object value)
        {
            Double number = (Double)value;
            string text = number.ToString("N", _Nmfi);
            if ((number % 1d) == 0d)
            {
                int dot = text.IndexOf('.');
                if (dot > 0)
                    text = text.Substring(0, dot);
            }
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDouble(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Double)0;
            Double value;
            if (!Double.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (Double)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DecimalToString(object value)
        {
            Decimal number = (Decimal)value;
            string text = number.ToString("N", _Nmfi);
            if ((number % 1m) == 0m)
            {
                int dot = text.IndexOf('.');
                if (dot > 0)
                    text = text.Substring(0, dot);
            }
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDecimal(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Decimal)0;
            Decimal value;
            if (!Decimal.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (Decimal)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GuidToString(object value)
        {
            return ((Guid)value).ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToGuid(string text)
        {
            if (String.IsNullOrEmpty(text)) return Guid.Empty;
            Guid value;
            if (!Guid.TryParse(text, out value)) return Guid.Empty;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CharToString(object value)
        {
            return ((Char)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToChar(string text)
        {
            if (String.IsNullOrEmpty(text)) return Char.MinValue;
            Char value;
            if (!Char.TryParse(text, out value)) return Char.MinValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string StringToString(object value)
        {
            if (value == null) return null;
            return value as string;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToString(string text)
        {
            if (text == null) return null;
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeToString(object value)
        {
            DateTime dateTime = (DateTime)value;
            if (dateTime.Millisecond == 0 && dateTime.Second == 0)
                return dateTime.ToString("D", _Dtfi);
            return dateTime.ToString("F", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            DateTime value;
            if (DateTime.TryParseExact(text, "D", _Dtfi, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParseExact(text, "F", _Dtfi, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParse(text, _Dtfi, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out value))
                return value;

            return DateTime.MinValue;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeToSerial(object value)
        {
            DateTime dateTime = (DateTime)value;
            string u = (dateTime.Kind == DateTimeKind.Utc ? "U" : (dateTime.Kind == DateTimeKind.Local ? "L" : ""));
            bool h = (dateTime.TimeOfDay.Ticks > 0L);
            bool s = (h && dateTime.Second > 0);
            bool f = (h && dateTime.Millisecond > 0);
            string format = (f ? "yyyyMMddHHmmssfff" : (s ? "yyyyMMddHHmmss" : (h ? "yyyyMMddHHmm" : "yyyyMMdd")));
            return u + dateTime.ToString(format);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object SerialToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            string serial = text.Trim();
            if (serial.Length < 8) return DateTime.MinValue;
            DateTimeKind kind = _GetDateTimeKind(ref serial);
            int length = serial.Length;
            if (length < 8) return DateTime.MinValue;
            int dy = _GetInt(serial, 0, 4, 100);
            int dm = _GetInt(serial, 4, 2, 1);
            int dd = _GetInt(serial, 6, 2, 1);
            if (dm < 1 || dm > 12 || dd < 1 || dd > 31) return DateTime.MinValue;

            try
            {
                if (length == 8) return new DateTime(dy, dm, dd, 0, 0, 0, kind);

                int th = _GetInt(serial, 8, 2, 0);
                int tm = _GetInt(serial, 10, 2, 0);
                int ts = _GetInt(serial, 12, 2, 0);
                if (th < 0 || th > 23 || tm < 0 || tm > 59 || ts < 0 || ts > 59) return new DateTime(dy, dm, dd, 0, 0, 0, kind);

                int tf = _GetInt(serial, 14, 3, 0);
                return new DateTime(dy, dm, dd, th, tm, ts, tf, kind);
            }
            catch { }
            return DateTime.MinValue;
        }
        /// <summary>
        /// Vrátí typ času podle prefixu
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        private static DateTimeKind _GetDateTimeKind(ref string serial)
        {
            switch (serial[0])
            {
                case 'U':
                    serial = serial.Substring(1);
                    return DateTimeKind.Utc;
                case 'L':
                    serial = serial.Substring(1);
                    return DateTimeKind.Local;
            }
            return DateTimeKind.Unspecified;
        }
        /// <summary>
        /// Vrátí číslo ze substringu
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="begin"></param>
        /// <param name="length"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        private static int _GetInt(string serial, int begin, int length, int defValue)
        {
            if (serial.Length < (begin + length)) return defValue;
            int value;
            if (!Int32.TryParse(serial.Substring(begin, length), out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeOffsetToString(object value)
        {
            DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset.ToString("F", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDateTimeOffset(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTimeOffset.MinValue;
            DateTimeOffset value;
            if (!DateTimeOffset.TryParseExact(text, "D", _Dtfi, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out value)) return DateTimeOffset.MinValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string TimeSpanToString(object value)
        {
            return ((TimeSpan)value).ToString("G", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToTimeSpan(string text)
        {
            if (String.IsNullOrEmpty(text)) return TimeSpan.Zero;
            TimeSpan value;
            if (!TimeSpan.TryParse(text, _Dtfi, out value)) return TimeSpan.Zero;
            return value;
        }
        #endregion
        #region Object to/from, Type
        /// <summary>
        /// Z objektu detekuje jeho typ a pak podle tohoto typu převede hodnotu na string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ObjectToString(object value)
        {
            if (value == null) return "";
            string typeName = value.GetType().FullName;
            switch (typeName)
            {
                case "System.Boolean": return BooleanToString(value);
                case "System.Byte": return ByteToString(value);
                case "System.SByte": return SByteToString(value);
                case "System.Int16": return Int16ToString(value);
                case "System.Int32": return Int32ToString(value);
                case "System.Int64": return Int64ToString(value);
                case "System.UInt16": return UInt16ToString(value);
                case "System.UInt32": return UInt32ToString(value);
                case "System.UInt64": return UInt64ToString(value);
                case "System.Single": return SingleToString(value);
                case "System.Double": return DoubleToString(value);
                case "System.Decimal": return DecimalToString(value);
                case "System.DateTime": return DateTimeToString(value);
                case "System.TimeSpan": return TimeSpanToString(value);
                case "System.Char": return CharToString(value);
                case "System.DateTimeOffset": return DateTimeOffsetToString(value);
                case "System.Guid": return GuidToString(value);
                case "System.Drawing.Color": return ColorToString(value);
                case "System.Drawing.Point": return PointToString(value);
                case "System.Drawing.PointF": return PointFToString(value);
                case "System.Drawing.Rectangle": return RectangleToString(value);
                case "System.Drawing.RectangleF": return RectangleFToString(value);
                case "System.Drawing.Size": return SizeToString(value);
                case "System.Drawing.SizeF": return SizeFToString(value);
            }
            return value.ToString();
        }
        /// <summary>
        /// Daný string převede na hodnotu požadovaného typu. Pokud není zadán typ, vrátí null.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object StringToObject(string text, Type type)
        {
            if (type == null) return null;
            string typeName = type.FullName;
            switch (typeName)
            {
                case "System.Boolean": return StringToBoolean(text);
                case "System.Byte": return StringToByte(text);
                case "System.SByte": return StringToSByte(text);
                case "System.Int16": return StringToInt16(text);
                case "System.Int32": return StringToInt32(text);
                case "System.Int64": return StringToInt64(text);
                case "System.UInt16": return StringToUInt16(text);
                case "System.UInt32": return StringToUInt32(text);
                case "System.UInt64": return StringToUInt64(text);
                case "System.Single": return StringToSingle(text);
                case "System.Double": return StringToDouble(text);
                case "System.Decimal": return StringToDecimal(text);
                case "System.DateTime": return StringToDateTime(text);
                case "System.TimeSpan": return StringToTimeSpan(text);
                case "System.Char": return StringToChar(text);
                case "System.DateTimeOffset": return StringToDateTimeOffset(text);
                case "System.Guid": return StringToGuid(text);
                case "System.Drawing.Color": return StringToColor(text);
                case "System.Drawing.Point": return StringToPoint(text);
                case "System.Drawing.PointF": return StringToPointF(text);
                case "System.Drawing.Rectangle": return StringToRectangle(text);
                case "System.Drawing.RectangleF": return StringToRectangleF(text);
                case "System.Drawing.Size": return StringToSize(text);
                case "System.Drawing.SizeF": return StringToSizeF(text);
            }
            return null;
        }
        /// <summary>
        /// Vrátí String pro daný Type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string TypeToString(Type type)
        {
            string name = type.FullName;
            if (name.StartsWith("System."))
            {
                string sysName = name.Substring(7);
                if (sysName.IndexOf(".") < 0) return sysName;        // Z typů "System.DateTime" vrátím jen "DateTime"
            }
            if (name.StartsWith("System.Drawing."))
            {
                string sysName = name.Substring(15);
                if (sysName.IndexOf(".") < 0) return sysName;        // Z typů "System.Drawing.RectangleF" vrátím jen "RectangleF"
            }
            return name;
        }
        /// <summary>
        /// Převede text na Type. Pokud nelze určit Type, vrátí null, ale ne chybu.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Type StringToType(string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            text = _NormalizeTypeName(text);
            if (text.IndexOf(".") < 0)
                text = "System." + text;
            Type type = null;
            try { type = Type.GetType(text, false, true); }
            catch { type = null; }
            return type;
        }
        /// <summary>
        /// Vrátí plný StringName pro daný zjednodušený název typu
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string _NormalizeTypeName(string text)
        {
            text = text.Trim();
            string key = text.ToLower();
            switch (key)
            {   // Tady řeším "zjednodušené názvy typů" a vracím ".NET názvy typů"; nemusím prefixovat "System." :
                case "bool": return "System.Boolean";
                case "guid": return "System.Guid";
                case "short": return "System.Int16";
                case "int": return "System.Int32";
                case "long": return "System.Int64";
                case "ushort": return "System.UInt16";
                case "uint": return "System.UInt32";
                case "ulong": return "System.UInt64";
                case "numeric": return "System.Decimal";
                case "float": return "System.Single";
                case "double": return "System.Double";                 // Tenhle a další nejsou nutné, protože to řeší parametr "ignoreCase" v Type.GetType()
                case "decimal": return "System.Decimal";
                case "number": return "System.Decimal";
                case "text": return "System.String";
                case "varchar": return "System.String";
                case "char": return "System.String";                   // Toto je změna typu !!!
                case "date": return "System.DateTime";
                case "time": return "System.DateTime";
                case "color": return "System.Drawing.Color";
                case "point": return "System.Drawing.Point";
                case "pointf": return "System.Drawing.PointF";
                case "rectangle": return "System.Drawing.Rectangle";
                case "rectanglef": return "System.Drawing.RectangleF";
                case "size": return "System.Drawing.Size";
                case "sizef": return "System.Drawing.SizeF";
            }
            if (key.StartsWith("numeric_")) return "Decimal";   // Jakékoli "numeric_19_6" => Decimal
            return text;
        }
        #endregion
        #region Nullable types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int32NToString(object value)
        {
            Int32? v = (Int32?)value;
            return (v.HasValue ? v.Value.ToString() : "null");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt32N(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32?)null;
            if (text.ToLower().Trim() == "null") return (Int32?)null;
            Int32 value;
            if (!Int32.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (Int32?)null;
            return (Int32?)value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleNToString(object value)
        {
            Single? v = (Single?)value;
            return (v.HasValue ? SingleToString(v.Value) : "null");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSingleN(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single?)null;
            if (text.ToLower().Trim() == "null") return (Single?)null;
            Single value;
            if (!Single.TryParse(text, NumberStyles.Any, _Nmfi, out value)) return (Single?)null;
            return (Single?)value;
        }
        #endregion
        #region Drawing Types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ColorToString(object value)
        {
            if (value is KnownColor)
            {
                KnownColor knownColor = (KnownColor)value;
                return System.Enum.GetName(typeof(KnownColor), knownColor);
            }
            if (!(value is Color))
                return "";
            Color data = (Color)value;
            if (data.IsKnownColor)
                return System.Enum.GetName(typeof(KnownColor), data.ToKnownColor());
            if (data.IsNamedColor)
                return data.Name;
            if (data.IsSystemColor)
                return "System." + data.ToString();
            if (data.A < 255)
                return ("#" + data.A.ToString("X2") + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
            return ("#" + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToColor(string text)
        {
            if (String.IsNullOrEmpty(text)) return Color.Empty;
            string t = text.Trim();                      // Jméno "Orchid", nebo hexa #806040 (RGB), nebo 0xD02000 (RGB), nebo hexa "#FF808040" (ARGB) nebo 0x40C0C0FF (ARGB).
            if (t.Length == 7 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 6)))
                return StringRgbToColor(t.Substring(1, 6));
            if (t.Length == 8 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 6)))
                return StringRgbToColor(t.Substring(2, 6));
            if (t.Length == 9 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 8)))
                return StringARgbToColor(t.Substring(1, 8));
            if (t.Length == 10 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 8)))
                return StringARgbToColor(t.Substring(2, 8));
            return StringNameToColor(t);
        }
        /// <summary>
        /// Z dodané barvy vrátí hexadecimální formát ve formě "#RRGGBB".
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ColorToXmlString(Color data)
        {
            if (data.A < 255)
                return ("#" + data.A.ToString("X2") + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
            return ("#" + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
        }
        /// <summary>
        /// Z deklarace barvy ve formě "#RRGGBB" v hexadecimálním formátu vrátí odpovídající barvu.
        /// Barva bude mít hodnotu Alpha = 255.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Color XmlStringToColor(string text)
        {
            if (String.IsNullOrEmpty(text)) return Color.Empty;
            string t = text.Trim();                      // Jméno "Orchid", nebo hexa #806040 (RGB), nebo 0xD02000 (RGB), nebo hexa "#FF808040" (ARGB) nebo 0x40C0C0FF (ARGB).
            if (t.Length == 7 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 6)))
                return StringRgbToColor(t.Substring(1, 6));
            if (t.Length == 8 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 6)))
                return StringRgbToColor(t.Substring(2, 6));
            if (t.Length == 9 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 8)))
                return StringARgbToColor(t.Substring(1, 8));
            if (t.Length == 10 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 8)))
                return StringARgbToColor(t.Substring(2, 8));
            return StringNameToColor(t);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Color StringNameToColor(string name)
        {
            KnownColor known;
            if (System.Enum.TryParse<KnownColor>(name, out known))
                return Color.FromKnownColor(known);

            try
            {
                return Color.FromName(name);
            }
            catch
            { }
            return Color.Empty;
        }
        /// <summary>
        /// Konkrétní konvertor z hodnoty "RRGGBB" na Color, kde RR, GG, BB je hexadecimální číslo
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static Color StringRgbToColor(string t)
        {
            int r = HexadecimalToInt32(t.Substring(0, 2));
            int g = HexadecimalToInt32(t.Substring(2, 2));
            int b = HexadecimalToInt32(t.Substring(4, 2));
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Konkrétní konvertor z hodnoty "AARRGGBB" na Color, kde aa, RR, GG, BB je hexadecimální číslo
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static Color StringARgbToColor(string t)
        {
            int a = HexadecimalToInt32(t.Substring(0, 2));
            int r = HexadecimalToInt32(t.Substring(2, 2));
            int g = HexadecimalToInt32(t.Substring(4, 2));
            int b = HexadecimalToInt32(t.Substring(6, 2));
            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string PointToString(object value, char delimiter = ';')
        {
            Point data = (Point)value;
            return $"{data.X}{delimiter}{data.Y}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToPoint(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return Point.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 2) return Point.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            return new Point(x, y);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string PointFToString(object value, char delimiter = ';')
        {
            PointF data = (PointF)value;
            return $"{data.X.ToString("N", _Nmfi)}{delimiter}{data.Y.ToString("N", _Nmfi)}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToPointF(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return PointF.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 2) return PointF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            return new PointF(x, y);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string RectangleToString(object value, char delimiter = ';')
        {
            Rectangle data = (Rectangle)value;
            return $"{data.X}{delimiter}{data.Y}{delimiter}{data.Width}{delimiter}{data.Height}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToRectangle(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return Rectangle.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 4) return Rectangle.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            int w = StringInt32(items[2]);
            int h = StringInt32(items[3]);
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string RectangleFToString(object value, char delimiter = ';')
        {
            RectangleF data = (RectangleF)value;
            return $"{data.X.ToString("N", _Nmfi)}{delimiter}{data.Y.ToString("N", _Nmfi)}{delimiter}{data.Width.ToString("N", _Nmfi)}{delimiter}{data.Height.ToString("N", _Nmfi)}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToRectangleF(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return RectangleF.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 4) return RectangleF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            Single w = StringSingle(items[2]);
            Single h = StringSingle(items[3]);
            return new RectangleF(x, y, w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string SizeToString(object value, char delimiter = ';')
        {
            Size data = (Size)value;
            return $"{data.Width}{delimiter}{data.Height}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToSize(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return Size.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 2) return Size.Empty;
            int w = StringInt32(items[0]);
            int h = StringInt32(items[1]);
            return new Size(w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string SizeFToString(object value, char delimiter = ';')
        {
            SizeF data = (SizeF)value;
            return $"{data.Width.ToString("N", _Nmfi)}{delimiter}{data.Height.ToString("N", _Nmfi)}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToSizeF(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return SizeF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return SizeF.Empty;
            Single w = StringSingle(items[0]);
            Single h = StringSingle(items[1]);
            return new SizeF(w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FontStyleToString(object value)
        {
            FontStyle fontStyle = (FontStyle)value;
            bool b = ((fontStyle & FontStyle.Bold) != 0);
            bool i = ((fontStyle & FontStyle.Italic) != 0);
            bool s = ((fontStyle & FontStyle.Strikeout) != 0);
            bool u = ((fontStyle & FontStyle.Underline) != 0);
            string result = (b ? "B" : "") + (i ? "I" : "") + (s ? "S" : "") + (u ? "U" : "");
            if (result.Length > 0) return result;
            return "R";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToFontStyle(string text)
        {
            if (String.IsNullOrEmpty(text)) return FontStyle.Regular;
            FontStyle result = (text.Contains("B") ? FontStyle.Bold : FontStyle.Regular) |
                               (text.Contains("I") ? FontStyle.Italic : FontStyle.Regular) |
                               (text.Contains("S") ? FontStyle.Strikeout : FontStyle.Regular) |
                               (text.Contains("U") ? FontStyle.Underline : FontStyle.Regular);
            return result;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FontToString(object value)
        {
            if (value == null) return "";
            Font font = (Font)value;
            return font.Name + ";" + SingleToString(font.SizeInPoints) + ";" + FontStyleToString(font.Style) + ";" + ByteToString(font.GdiCharSet);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToFont(string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            string[] items = text.Split(';');
            if (items.Length != 4) return null;
            float emSize = (float)StringToSingle(items[1]);
            FontStyle fontStyle = (FontStyle)StringToFontStyle(items[2]);
            byte gdiCharSet = (byte)StringToByte(items[3]);
            Font result = new Font(items[0], emSize, fontStyle, GraphicsUnit.Point, gdiCharSet);
            return result;
        }
        #endregion
        #region User types : je vhodnější persistovat je pomocí interface IXmlSerializer (pomocí property string IXmlSerializer.XmlSerialData { get; set; } )
        #endregion
        #region Enum types
        /// <summary>
        /// Vrátí název dané hodnoty enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EnumToString<T>(T value)
        {
            return Enum.GetName(typeof(T), value);
        }
        /// <summary>
        /// Vrátí hodnotu enumu daného typu z daného stringu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T StringToEnum<T>(string text) where T : struct
        {
            T value;
            if (Enum.TryParse<T>(text, out value))
                return value;
            return default(T);
        }
        /// <summary>
        /// Vrátí hodnotu enumu daného typu z daného stringu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="defaultValue">Defaultní hodnota</param>
        /// <returns></returns>
        public static T StringToEnum<T>(string text, T defaultValue) where T : struct
        {
            T value;
            if (Enum.TryParse<T>(text, out value))
                return value;
            return defaultValue;
        }
        #endregion
        #region Helpers
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Int32 StringInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Single StringSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        /// <summary>
        /// Vrátí Int32 ekvivalent daného hexadecimálního čísla.
        /// Hexadecimální číslo nesmí obsahovat prefix ani mezery, pouze hexadecimální znaky ("0123456789abcdefABCDEF").
        /// Délka textu je relativně libovolná (v rozsahu Int32, jinak dojde k přetečení).
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Int32 HexadecimalToInt32(string hexa)
        {
            Int64 value = HexadecimalToInt64(hexa);
            if (value > (Int64)(Int32.MaxValue) || value < (Int64)(Int32.MinValue))
                throw new OverflowException("Hexadecimal value " + hexa + " exceeding range for Int32 number.");
            return (Int32)value;
        }
        /// <summary>
        /// Vrátí Int64 ekvivalent daného hexadecimálního čísla.
        /// Hexadecimální číslo nesmí obsahovat prefix ani mezery, pouze hexadecimální znaky ("0123456789abcdefABCDEF").
        /// Délka textu je relativně libovolná (v rozsahu Int64, jinak dojde k přetečení).
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Int64 HexadecimalToInt64(string hexa)
        {
            int result = 0;
            if (hexa == null || hexa.Length == 0 || !ContainOnlyHexadecimals(hexa)) return result;
            int len = hexa.Length;
            int cfc = 1;
            for (int u = (len - 1); u >= 0; u--)
            {
                char c = hexa[u];
                switch (c)
                {
                    case '0':
                        break;
                    case '1':
                        result += cfc;
                        break;
                    case '2':
                        result += 2 * cfc;
                        break;
                    case '3':
                        result += 3 * cfc;
                        break;
                    case '4':
                        result += 4 * cfc;
                        break;
                    case '5':
                        result += 5 * cfc;
                        break;
                    case '6':
                        result += 6 * cfc;
                        break;
                    case '7':
                        result += 7 * cfc;
                        break;
                    case '8':
                        result += 8 * cfc;
                        break;
                    case '9':
                        result += 9 * cfc;
                        break;
                    case 'a':
                    case 'A':
                        result += 10 * cfc;
                        break;
                    case 'b':
                    case 'B':
                        result += 11 * cfc;
                        break;
                    case 'c':
                    case 'C':
                        result += 12 * cfc;
                        break;
                    case 'd':
                    case 'D':
                        result += 13 * cfc;
                        break;
                    case 'e':
                    case 'E':
                        result += 14 * cfc;
                        break;
                    case 'f':
                    case 'F':
                        result += 15 * cfc;
                        break;
                }
                cfc = cfc * 16;
            }
            return result;
        }
        /// <summary>
        /// Vrací true, když text obsahuje pouze hexadecimální znaky
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ContainOnlyHexadecimals(string text)
        {
            return ContainOnlyChars(text, "0123456789abcdefABCDEF");
        }
        /// <summary>
        /// Vrací true, když text obsahuje pouze povolené znaky ze seznamu (chars)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static bool ContainOnlyChars(string text, string chars)
        {
            if (text == null) return false;
            foreach (char c in text)
            {
                // Pokud písmeno c (ze vstupního textu) není obsaženo v seznamu povolených písmen, pak vrátíme false (text obsahuje jiné znaky než dané):
                if (!chars.Contains(c)) return false;
            }
            return true;
        }
        /// <summary>
        /// Z daného řetězce (text) odkrojí a vrátí část, která se nachází před delimiterem.
        /// Dany text (ref) zkrátí, bude obsahovat část za delimiterem.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string StringCutOff(ref string text, string delimiter)
        {
            if (text == null) return null;
            if (text.Length == 0) return "";
            string result;
            if (String.IsNullOrEmpty(delimiter))
                throw new ArgumentNullException("delimiter", "Parametr metody Convertor.StringCutOff(«delimiter») nemůže být prázdný.");
            int len = delimiter.Length;
            int at = text.IndexOf(delimiter);
            if (at < 0)
            {
                result = text;
                text = "";
            }
            else if (at == 0)
            {
                result = "";
                text = (at + len >= text.Length ? "" : text.Substring(at + len));
            }
            else
            {
                result = text.Substring(0, at);
                text = (at + len >= text.Length ? "" : text.Substring(at + len));
            }
            return result;
        }
        #endregion
        #endregion
        #region Konverze enumů na základě názvu hodnoty (Enum přes String na jiný Enum)
        /// <summary>
        /// Vstupní hodnotu enumu konvertuje do výstupní odpovídající stejnojmenné hodnoty.
        /// Konvertuje přes string, nikoli přes numerickou hodnotu. Enumy tedy musí mít shodné názvy prvků.
        /// </summary>
        /// <typeparam name="TInp"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inp"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TOut ConvertEnum<TInp, TOut>(TInp inp, TOut defaultValue)
            where TInp : struct
            where TOut : struct
        {
            string name = inp.ToString();
            if (Enum.TryParse<TOut>(name, out TOut value1)) return value1;
            if (Enum.TryParse<TOut>(name, true, out TOut value2)) return value2;
            return defaultValue;
        }
        /// <summary>
        /// Vstupní hodnotu enumu konvertuje do výstupní odpovídající stejnojmenné hodnoty.
        /// Konvertuje přes string, nikoli přes numerickou hodnotu. Enumy tedy musí mít shodné názvy prvků.
        /// <para/>
        /// Ošetřena NULL hodnota na vstupu: pokud je na vstupu NULL, pak výstup je automaticky NULL. Nikoli <paramref name="defaultValue"/>.
        /// Pokud vstupní hodnota nebude nalezena ve výstupním Enumu, pak výstupem bude <paramref name="defaultValue"/>. Ta může být předána jako null, pak i tehdy bude výstupem null.
        /// </summary>
        /// <typeparam name="TInp"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inp"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TOut? ConvertEnum<TInp, TOut>(TInp? inp, TOut? defaultValue)
            where TInp : struct
            where TOut : struct
        {
            if (!inp.HasValue) return null;

            string name = inp.Value.ToString();
            if (Enum.TryParse<TOut>(name, out TOut value1)) return value1;
            if (Enum.TryParse<TOut>(name, true, out TOut value2)) return value2;
            return defaultValue;
        }
        /// <summary>
        /// Vstupní hodnotu danou stringem konvertuje do hodnoty enumu <typeparamref name="TOut"/> odpovídající stejnojmenné hodnoty.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="text"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TOut ConvertEnum<TOut>(string text, TOut defaultValue)
            where TOut : struct
        {
            if (!String.IsNullOrEmpty(text))
            {
                string name = text.Trim();
                if (Enum.TryParse<TOut>(name, out TOut value1)) return value1;
                if (Enum.TryParse<TOut>(name, true, out TOut value2)) return value2;
            }
            return defaultValue;
        }
        #endregion
        #region Static konstruktor
        static Convertor()
        { _PrepareFormats(); }
        #endregion
        #region FormatInfo
        static void _PrepareFormats()
        {
            _Dtfi = new System.Globalization.DateTimeFormatInfo();
            _Dtfi.LongDatePattern = "yyyy-MM-dd HH:mm";                   // Pattern pro formátování písmenem D, musí být nastaveno před nastavením patternu FullDateTimePattern
            _Dtfi.FullDateTimePattern = "yyyy-MM-dd HH:mm:ss.fff";        // Pattern pro formátování písmenem F

            _Nmfi = new System.Globalization.NumberFormatInfo();
            _Nmfi.NumberDecimalDigits = 4;
            _Nmfi.NumberDecimalSeparator = ".";
            _Nmfi.NumberGroupSeparator = "";
        }
        static System.Globalization.DateTimeFormatInfo _Dtfi;
        static System.Globalization.NumberFormatInfo _Nmfi;
        #endregion
    }
    /// <summary>
    /// Konvertor obrázků ikon
    /// </summary>
    public static class ImageConvertor
    {
        /// <summary>
        /// Načte zadaný soubor a vytvoří z něj Image.
        /// Může zajistit zmenšení na zadanou velikost.
        /// Image poté konvertuje na binární string a ten vrací.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static string FileToBinary(string fileName, int? maxSize = null)
        {
            using (Image image = Bitmap.FromFile(fileName))
            {
                return ImageToBinary(image, maxSize);
            }
        }
        /// <summary>
        /// Konvertuje dodaný Image na binární string.
        /// Může zajistit zmenšení na zadanou velikost.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static string ImageToBinary(Image image, int? maxSize = null)
        {
            string binary = null;
            if (maxSize.HasValue && maxSize.Value >= 8)
            {
                var newImage = ScaleImageToSize(image, maxSize.Value, out var isNewImage);
                binary = ConvertImageToBinary(newImage);
                if (isNewImage)
                    // Pokud konvertor změnil velikost, pak nový upravený Image (newImage) dám Dispose nyní:
                    // Pokud se ale nevytvářel nový Image, pak vstupní Image nebude Disposován zde, ale o jeho Dispose se postará ten, kdo jej vytvořil.
                    newImage.Dispose();
            }
            else
            {
                binary = ConvertImageToBinary(image);
            }
            return binary;
        }
        /// <summary>
        /// Načte zadaný soubor a vytvoří z něj Image.
        /// Může zajistit zmenšení na zadanou velikost.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static Image FileToImage(string fileName, int? maxSize = null)
        {
            Image image = Bitmap.FromFile(fileName);
            if (maxSize.HasValue && maxSize.Value >= 8)
            {
                var newImage = ScaleImageToSize(image, maxSize.Value, out var isNewImage);
                if (isNewImage)
                {   // Pokud konvertor změnil velikost, pak původní načtený Image dám Dispose a vracím ten upravený:
                    image.Dispose();
                    return newImage;
                }
            }
            return image;
        }
        /// <summary>
        /// Z binárního stringu vrátí Image
        /// </summary>
        /// <param name="binary"></param>
        /// <returns></returns>
        public static Image ConvertBinaryToImage(string binary)
        {
            var buffer = System.Convert.FromBase64String(binary);
            using (var memoryStream = new System.IO.MemoryStream(buffer))
            {
                return Bitmap.FromStream(memoryStream);
            }
        }
        /// <summary>
        /// Vrátí serializovatelný string, obsahující dodaný Image.
        /// Tato metoda neprovádí žádné změny dodaného Image. Ukládá formát PNG.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string ConvertImageToBinary(Image image)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                var buffer = memoryStream.ToArray();
                return System.Convert.ToBase64String(buffer);
            }
        }
        /// <summary>
        /// Umožní upravit velikost dodané Image na zadanou maximální. Na výstupu informuje, že došlo k reálné výměně image (to pokud out <paramref name="isNewImage"/> je true).
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxSize"></param>
        /// <param name="isNewImage"></param>
        /// <returns></returns>
        public static Image ScaleImageToSize(Image image, int maxSize, out bool isNewImage)
        {
            isNewImage = false;
            var imageSize = image.Size;
            int currentSize = (imageSize.Width > imageSize.Height ? imageSize.Width : imageSize.Height);     // Větší hodnota z Width | Height
            if (currentSize <= maxSize) return image;                                                        // Aktuální rozměr Image je menší než požadovaný => nebudu Image měnit

            isNewImage = true;
            var scaleSize = imageSize.ZoomToTarget(maxSize);
            var newImage = new Bitmap(image, scaleSize);
            return newImage;
        }
    }
}
