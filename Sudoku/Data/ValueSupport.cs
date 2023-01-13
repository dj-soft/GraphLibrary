using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Data
{
    #region class ValueSupport : Podpůrné metody pro hodnoty
    /// <summary>
    /// Podpůrné metody pro hodnoty
    /// </summary>
    public static class ValueSupport
    {
        #region Get
        /// <summary>
        /// Metoda prověří, zda dodané hodnoty jsou přípustné hodnoty do animátoru.
        /// Pokud ne, dojde k chybě.
        /// Pokud ano, bude do out <paramref name="valueType"/> vložen typ hodnoty.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <param name="valueType"></param>
        public static void CheckValues(object startValue, object endValue, out SupportedValueType valueType)
        {
            valueType = SupportedValueType.None;

            // Typ hodnot?
            SupportedValueType svt = ValueSupport.GetValueType(startValue);
            SupportedValueType evt = ValueSupport.GetValueType(endValue);

            // Nesmí být Null:
            if (svt == SupportedValueType.Null || evt == SupportedValueType.Null)
                throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' nesmí být null.");

            // Musí být shodné:
            if (svt != evt)
                throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' musí být obě stejného typu. Aktuálně 'startValue': {startValue.GetType().Name}, a 'endValue': {endValue.GetType().Name}.");

            // Nesmí být Other:
            if (svt == SupportedValueType.Other)
                throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' musí být jen určitých typů. Aktuální typ '{startValue.GetType().Name}' není podporován, není připravena metoda pro interpolaci hodnoty.");

            valueType = svt;
        }
        /// <summary>
        /// Vrátí enumerační typ hodnoty, pod kterým je podporována v třídě <see cref="ValueSupport"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SupportedValueType GetValueType(object value)
        {
            if (value is null) return SupportedValueType.Null;

            // Podporované typy:
            string typeName = value.GetType().FullName;
            switch (typeName)
            {
                case "System.Int16": return SupportedValueType.Int16;
                case "System.Int32": return SupportedValueType.Int32;
                case "System.Int64": return SupportedValueType.Int64;
                case "System.Single": return SupportedValueType.Single;
                case "System.Double": return SupportedValueType.Double;
                case "System.Decimal": return SupportedValueType.Decimal;
                case "System.DateTime": return SupportedValueType.DateTime;
                case "System.Drawing.Point": return SupportedValueType.Point;
                case "System.Drawing.Size": return SupportedValueType.Size;
                case "System.Drawing.Color": return SupportedValueType.Color;
            }
            return SupportedValueType.Other;
        }
        #endregion
        #region IsEqual
        /// <summary>
        /// Vrátí true, pokud dvě dodané hodnoty jsou shodné.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static bool IsEqualValues(object oldValue, object newValue, SupportedValueType valueType)
        {
            switch (valueType)
            {
                case SupportedValueType.Int16: return ((Int16)oldValue == (Int16)newValue);
                case SupportedValueType.Int32: return ((Int32)oldValue == (Int32)newValue);
                case SupportedValueType.Int64: return ((Int64)oldValue == (Int64)newValue);
                case SupportedValueType.Single: return ((Single)oldValue == (Single)newValue);
                case SupportedValueType.Double: return ((Double)oldValue == (Double)newValue);
                case SupportedValueType.Decimal: return ((Decimal)oldValue == (Decimal)newValue);
                case SupportedValueType.Point: return ((Point)oldValue == (Point)newValue);
                case SupportedValueType.Size: return ((Size)oldValue == (Size)newValue);
                case SupportedValueType.Color: return ValueSupport.IsEqualColors((Color)oldValue, (Color)newValue);
            }
            throw new ArgumentException($"Nelze provést výpočet vyhodnocení IsEqualValues pro typ hodnoty 'valueType' = '{valueType}'.");
        }
        public static bool IsEqualColors(Color oldValue, Color newValue)
        {
            return (oldValue.A == newValue.A &&
                    oldValue.R == newValue.R &&
                    oldValue.G == newValue.G &&
                    oldValue.B == newValue.B);
        }
        #endregion
        #region Morph
        /// <summary>
        /// Metoda vrátí hodnotu daného typu na dané pozici mezi hodnotami Start a End.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="morphRatio"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        public static object MorphValue(object startValue, double morphRatio, object endValue)
        {
            ValueSupport.CheckValues(startValue, endValue, out SupportedValueType valueType);
            return MorphValue(valueType, startValue, morphRatio, endValue);
        }
        /// <summary>
        /// Metoda vrátí hodnotu daného typu na dané pozici mezi hodnotami Start a End.
        /// </summary>
        /// <param name="valueType"></param>
        /// <param name="startValue"></param>
        /// <param name="morphRatio"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        public static object MorphValue(SupportedValueType valueType, object startValue, double morphRatio, object endValue)
        {
            switch (valueType)
            {
                case SupportedValueType.Int16: return ValueSupport.MorphValueInt16((Int16)startValue, morphRatio, (Int16)endValue);
                case SupportedValueType.Int32: return ValueSupport.MorphValueInt32((Int32)startValue, morphRatio, (Int32)endValue);
                case SupportedValueType.Int64: return ValueSupport.MorphValueInt64((Int64)startValue, morphRatio, (Int64)endValue);
                case SupportedValueType.Single: return ValueSupport.MorphValueSingle((Single)startValue, morphRatio, (Single)endValue);
                case SupportedValueType.Double: return ValueSupport.MorphValueDouble((Double)startValue, morphRatio, (Double)endValue);
                case SupportedValueType.Decimal: return ValueSupport.MorphValueDecimal((Decimal)startValue, morphRatio, (Decimal)endValue);
                case SupportedValueType.DateTime: return ValueSupport.MorphValueDateTime((DateTime)startValue, morphRatio, (DateTime)endValue);
                case SupportedValueType.Point: return ValueSupport.MorphValuePoint((Point)startValue, morphRatio, (Point)endValue);
                case SupportedValueType.Size: return ValueSupport.MorphValueSize((Size)startValue, morphRatio, (Size)endValue);
                case SupportedValueType.Color: return ValueSupport.MorphValueColor((Color)startValue, morphRatio, (Color)endValue);
            }
            throw new ArgumentException($"Nelze provést výpočet MorphValue pro typ hodnoty 'valueType' = '{valueType}'.");
        }
        public static Byte MorphValueByte(Byte startValue, double morphRatio, Byte endValue)
        {
            var diffValue = (int)(Math.Round(morphRatio * (int)(endValue - startValue), 0));
            var resultValue = startValue + diffValue;
            if (resultValue < 0) return (Byte)0;
            if (resultValue > 255) return (Byte)255;
            return (Byte)resultValue;
        }
        public static Int16 MorphValueInt16(Int16 startValue, double morphRatio, Int16 endValue)
        {
            var diffValue = (Int16)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            var resultValue = startValue + diffValue;
            if (resultValue < Int16.MinValue) return Int16.MinValue;
            if (resultValue > Int16.MaxValue) return Int16.MaxValue;
            return (Int16)resultValue;
        }
        public static Int32 MorphValueInt32(Int32 startValue, double morphRatio, Int32 endValue)
        {
            var diffValue = (Int32)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static Int64 MorphValueInt64(Int64 startValue, double morphRatio, Int64 endValue)
        {
            var diffValue = (Int64)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static Single MorphValueSingle(Single startValue, double morphRatio, Single endValue)
        {
            var diffValue = (Single)(morphRatio * (double)(endValue - startValue));
            return startValue + diffValue;
        }
        public static Double MorphValueDouble(Double startValue, double morphRatio, Double endValue)
        {
            var diffValue = (Double)(morphRatio * (double)(endValue - startValue));
            return startValue + diffValue;
        }
        public static Decimal MorphValueDecimal(Decimal startValue, double morphRatio, Decimal endValue)
        {
            var diffValue = (Decimal)morphRatio * (endValue - startValue);
            return startValue + diffValue;
        }
        public static DateTime MorphValueDateTime(DateTime startValue, double morphRatio, DateTime endValue)
        {
            TimeSpan diffValue = TimeSpan.FromSeconds(morphRatio * ((TimeSpan)(endValue - startValue)).TotalSeconds);
            return startValue + diffValue;
        }
        public static Point MorphValuePoint(Point startValue, double morphRatio, Point endValue)
        {
            int x = MorphValueInt32(startValue.X, morphRatio, endValue.X);
            int y = MorphValueInt32(startValue.Y, morphRatio, endValue.Y);
            return new Point(x, y);
        }
        public static Size MorphValueSize(Size startValue, double morphRatio, Size endValue)
        {
            int width = MorphValueInt32(startValue.Width, morphRatio, endValue.Width);
            int height = MorphValueInt32(startValue.Height, morphRatio, endValue.Height);
            return new Size(width, height);
        }
        public static Color MorphValueColor(Color startValue, double morphRatio, Color endValue)
        {
            byte a = MorphValueByte(startValue.A, morphRatio, endValue.A);
            byte r = MorphValueByte(startValue.R, morphRatio, endValue.R);
            byte g = MorphValueByte(startValue.G, morphRatio, endValue.G);
            byte b = MorphValueByte(startValue.B, morphRatio, endValue.B);
            return Color.FromArgb(a, r, g, b);
        }
        #endregion
    }
    #endregion
    #region struct ColorHSV : Barva v režimu HSV
    /// <summary>
    /// Barva v režimu HSV
    /// </summary>
    public struct ColorHSV
    {
        /// <summary>
        /// Alfa kanál v rozsahu 0.00 ÷ 1.00 : 0.00 = neviditelná průhledná jako sklo / 1.00 = plná naprosto neprůhledná
        /// </summary>
        public double Alpha { get; set; }
        /// <summary>
        /// Odstín v rozsahu 0.0 ÷ 360.0°
        /// </summary>
        public double Hue { get; set; }
        /// <summary>
        /// Saturace v rozsahu 0.00 ÷ 1.00
        /// </summary>
        public double Saturation { get; set; }
        /// <summary>
        /// Světlost v rozsahu 0.00 ÷ 1.00
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ColorHSV: [Alpha:{Alpha:F3}; Hue:{Hue:F1}°; Saturation:{Saturation:F3}; Value:{Value:F3}; ]";
        }
        /// <summary>
        /// Systémová barva
        /// </summary>
        public Color Color
        {
            get
            {
                int a = Convert.ToInt32(255d * Alpha);
                var hue = Hue;
                var saturation = Saturation;
                var value = Value;

                int hi = Convert.ToInt32(Math.Floor(hue / 60d)) % 6;
                double f = hue / 60d - Math.Floor(hue / 60d);

                value = value * 255d;
                int v = Convert.ToInt32(value);
                int p = Convert.ToInt32(value * (1 - saturation));
                int q = Convert.ToInt32(value * (1 - f * saturation));
                int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

                switch (hi)
                {
                    case 0: return Color.FromArgb(a, v, t, p);
                    case 1: return Color.FromArgb(a, q, v, p);
                    case 2: return Color.FromArgb(a, p, v, t);
                    case 3: return Color.FromArgb(a, p, q, v);
                    case 4: return Color.FromArgb(a, t, p, v);
                    default: return Color.FromArgb(a, v, p, q);
                }
            }
            set
            {
                var color = value;

                double max = Math.Max(color.R, Math.Max(color.G, color.B));
                double min = Math.Min(color.R, Math.Min(color.G, color.B));

                this.Alpha = (double)color.A / 255d;
                this.Hue = color.GetHue();
                this.Saturation = (max == 0) ? 0 : 1d - (1d * min / max);
                this.Value = max / 255d;
            }
        }
        public static ColorHSV FromHSV(double hue, double saturation, double value)
        {
            return FromAHSV(1d, hue, saturation, value);
        }
        public static ColorHSV FromAHSV(double alpha, double hue, double saturation, double value)
        {
            ColorHSV colorHSV = new ColorHSV();
            colorHSV.Alpha = _Align(alpha, 0d, 1d);
            colorHSV.Hue = _Align(hue, 0d, 360d);
            colorHSV.Saturation = _Align(saturation, 0d, 1d);
            colorHSV.Value = _Align(value, 0d, 1d);
            return colorHSV;
        }
        public static ColorHSV FromColor(Color color)
        {
            ColorHSV colorHSV = new ColorHSV();
            colorHSV.Color = color;
            return colorHSV;
        }


        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        private static double _Align(double value, double min, double max) { return (value > max ? max : (value < min ? min : value)); }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Typ hodnoty
    /// </summary>
    public enum SupportedValueType
    {
        None,
        Null,
        Other,
        Int16,
        Int32,
        Int64,
        Single,
        Double,
        Decimal,
        DateTime,
        Point,
        Size,
        Color
    }
    #endregion
}
