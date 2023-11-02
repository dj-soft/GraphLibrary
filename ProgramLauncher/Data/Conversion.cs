using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// Provádí konverze do konkrétního výstupního typu
    /// </summary>
    public static class Conversion
    {
        #region Konverze do cílového typu
        public static Boolean ToBoolean(object value)
        {
            if (value is null) return false;
            if (value is Boolean b) return b;
            if (value is Byte i8) return (i8 != (Byte)0);
            if (value is Int16 i16) return (i16 != 0);
            if (value is Int32 i32) return (i32 != 0);
            if (value is String text) return (text.Trim().ToLower().IsAnyFrom("a", "y", "ano", "yes", "1", "true"));
            return false;
        }
        public static String ToString(object value)
        {
            if (value is null) return null;
            if (value is Int16 i16) return Convertor.Int16ToString(i16);
            if (value is Int32 i32) return Convertor.Int32ToString(i32);
            if (value is Single sng) return Convertor.SingleToString(sng);
            if (value is Double dbl) return Convertor.DoubleToString(dbl);
            if (value is Decimal dec) return Convertor.DecimalToString(dec);
            if (value is String text) return text;
            return value.ToString();
        }
        public static Int16 ToInt16(object value)
        {
            if (value is null) return (short)0;
            if (value is Int16 i16) return i16;
            if (value is Int32 i32) return (Int16)i32;
            if (value is String text) return (Int16)Convertor.StringToInt16(text);
            return (short)0;
        }
        public static Int32 ToInt32(object value)
        {
            if (value is null) return 0;
            if (value is Int16 i16) return (Int32)i16;
            if (value is Int32 i32) return i32;
            if (value is String text) return (Int32)Convertor.StringToInt32(text);
            return 0;
        }
        public static Single ToSingle(object value)
        {
            if (value is null) return 0f;
            if (value is Int16 i16) return (Single)i16;
            if (value is Int32 i32) return (Single)i32;
            if (value is Single sng) return sng;
            if (value is Double dbl) return (Single)dbl;
            if (value is Decimal dec) return (Single)dec;
            if (value is String text) return (Single)Convertor.StringSingle(text);
            return 0f;
        }
        public static Double ToDouble(object value)
        {
            if (value is null) return 0d;
            if (value is Int16 i16) return (Double)i16;
            if (value is Int32 i32) return (Double)i32;
            if (value is Single sng) return (Double)sng;
            if (value is Double dbl) return dbl;
            if (value is Decimal dec) return (Double)dec;
            if (value is String text) return (Double)Convertor.StringToDouble(text);
            return 0f;
        }
        public static Decimal ToDecimal(object value)
        {
            if (value is null) return 0m;
            if (value is Int16 i16) return (Decimal)i16;
            if (value is Int32 i32) return (Decimal)i32;
            if (value is Single sng) return (Decimal)sng;
            if (value is Double dbl) return (Decimal)dbl;
            if (value is Decimal dec) return dec;
            if (value is String text) return (Decimal)Convertor.StringToDecimal(text);
            return 0m;
        }
        public static DateTime ToDateTime(object value)
        {
            if (value is null) return DateTime.MinValue;
            if (value is DateTime dt) return dt;
            if (value is String text) return (DateTime)Convertor.StringToDateTime(text);
            return DateTime.MinValue;
        }

        public static System.Drawing.Color? ToColorN(object value)
        {
            if (value is null) return null;
            if (value is Color color) return color;
            if (value is String text) return (Color?)Convertor.StringToColor(text);
            if (value is Int32 i32) return Color.FromArgb(i32);
            return null;
        }
        public static object ToType(object value, Type targetType)
        {
            var typeName = targetType.FullName;
            switch (typeName)
            {
                case "System.Boolean": return ToBoolean(value);
                case "System.String": return ToString(value);
                case "System.Int16": return ToInt16(value);
                case "System.Int32": return ToInt32(value);
                case "System.Single": return ToSingle(value);
                case "System.Double": return ToDouble(value);
                case "System.Decimal": return ToDecimal(value);
                case "System.DateTime": return ToDateTime(value);
                case "System.Drawing.Color": return ToColorN(value);
            }
            return value;
        }
        #endregion
    }
}
