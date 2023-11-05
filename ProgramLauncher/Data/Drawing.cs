using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    #region struct RectangleExt : Souřadnice prvku snadno ukotvitelné nejen Left a Top (jako Rectangle) ale i Right a Bottom a Center.
    /// <summary>
    /// Souřadnice.
    /// <para/>
    /// V jednom každém směru (X, Y) může mít zadánu jednu až dvě souřadnice tak, aby bylo možno získat reálnou souřadnici v parent prostoru:
    /// Například: Left a Width, nebo Left a Right, nebo Width a Right, nebo jen Width. Tím se řeší různé ukotvení.
    /// <para/>
    /// Po zadání hodnot lze získat konkrétní souřadnice metodou <see cref="GetBounds"/>
    /// </summary>
    public struct RectangleExt
    {
        public RectangleExt(int? left, int? width, int? right, int? top, int? height, int? bottom)
        {
            Left = left;
            Width = width;
            Right = right;

            Top = top;
            Height = height;
            Bottom = bottom;
        }
        /// <summary>
        /// Souřadnice Left, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k levé hraně.
        /// Pokud je null, pak je vázaný k pravé hraně nebo na střed.
        /// </summary>
        public int? Left { get; set; }
        /// <summary>
        /// Souřadnice Top, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k horní hraně.
        /// Pokud je null, pak je vázaný k dolní hraně nebo na střed.
        /// </summary>
        public int? Top { get; set; }
        /// <summary>
        /// Souřadnice Right, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k pravé hraně.
        /// Pokud je null, pak je vázaný k levé hraně nebo na střed.
        /// </summary>
        public int? Right { get; set; }
        /// <summary>
        /// Souřadnice Bottom, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k dolní hraně.
        /// Pokud je null, pak je vázaný k horní hraně nebo na střed.
        /// </summary>
        public int? Bottom { get; set; }
        /// <summary>
        /// Pevná šířka, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou šířku.
        /// Pokud je null, pak je vázaný k pravé i levé hraně a má šířku proměnnou.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Pevná výška, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou výšku.
        /// Pokud je null, pak je vázaný k horní i dolní hraně a má výšku proměnnou.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Textem vyjádřený obsah this prvku
        /// </summary>
        public string Text { get { return $"Left: {Left}, Width: {Width}, Right: {Right}, Top: {Top}, Height: {Height}, Bottom: {Bottom}"; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text;
        }
        /// <summary>
        /// Metoda vrátí konkrétní souřadnice v daném prostoru parenta.
        /// Při tom jsou akceptovány plovoucí souřadnice.
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <returns></returns>
        public Rectangle GetBounds(Rectangle parentBounds)
        {
            if (!IsValid)
                throw new InvalidOperationException($"Neplatně zadané souřadnice v {nameof(RectangleExt)}: {Text}");

            var rectangleExt = this;
            getBound(Left, Width, Right, parentBounds.Left, parentBounds.Right, out int x, out int w);
            getBound(Top, Height, Bottom, parentBounds.Top, parentBounds.Bottom, out int y, out int h);
            return new Rectangle(x, y, w, h);

            void getBound(int? defB, int? defS, int? defE, int parentB, int parentE, out int begin, out int size)
            {
                bool hasB = defB.HasValue;
                bool hasS = defS.HasValue;
                bool hasE = defE.HasValue;

                if (hasB && hasS && !hasE)
                {   // Mám Begin a Size a nemám End     => standardně jako Rectangle:
                    begin = parentB + defB.Value;
                    size = defS.Value;
                }
                else if (hasB && !hasS && hasE)
                {   // Mám Begin a End a nemám Size     => mám pružnou šířku:
                    begin = parentB + defB.Value;
                    size = parentE - defE.Value - begin;
                }
                else if (!hasB && hasS && hasE)
                {   // Mám Size a End a nemám Begin     => jsem umístěn od konce:
                    int end = parentE - defE.Value;
                    size = defS.Value;
                    begin = end - size;
                }
                else if (!hasB && hasS && !hasE)
                {   // Mám Size a nemám Begin ani End   => jsem umístěn Center:
                    int center = parentB + ((parentE - parentB) / 2);
                    size = defS.Value;
                    begin = center - (size / 2);
                }
                else
                {   // Nesprávné zadání:
                    throw new InvalidOperationException($"Chyba v datech {nameof(RectangleExt)}: {rectangleExt.ToString()}");
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je zcela nezadaný = prázdný.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // Pokud i jen jediná hodnota je zadaná, pak vrátím false = objekt NENÍ prázdný:
                return !(Left.HasValue || Width.HasValue || Right.HasValue || Top.HasValue || Height.HasValue || Bottom.HasValue);
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je korektně zadaný, a může mít kladný vnitřní prostor
        /// </summary>
        public bool IsValid
        {
            get
            {
                return isValid(Left, Width, Right) && isValid(Top, Height, Bottom);

                // Je zadaná sada hodnot platná?
                bool isValid(int? begin, int? size, int? end)
                {
                    bool hasB = begin.HasValue;
                    bool hasS = size.HasValue;
                    bool hasE = end.HasValue;

                    return ((hasB && hasS && !hasE)                  // Mám Begin a Size a nemám End     => standardně jako Rectangle
                         || (hasB && !hasS && hasE)                  // Mám Begin a End a nemám Size     => mám pružnou šířku
                         || (!hasB && hasS && hasE)                  // Mám Size a End a nemám Begin     => jsem umístěn od konce
                         || (!hasB && hasS && !hasE));               // Mám Size a nemám Begin ani End   => jsem umístěn Center
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je korektně zadaný, a může mít kladný vnitřní prostor
        /// </summary>
        public bool HasContent
        {
            get
            {
                return IsValid && hasSize(Width) && hasSize(Height);

                // Je daná hodnota kladná nebo null ? (pro Null předpokládáme, že se dopočítá kladné číslo z Parent rozměru)
                bool hasSize(int? size)
                {
                    return (!size.HasValue || (size.HasValue && size.Value > 0));
                }
            }
        }
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
        public static ColorHSV FromArgb(int alpha, int red, int green, int blue)
        {
            ColorHSV colorHSV = new ColorHSV();
            colorHSV.Color = Color.FromArgb(alpha, red, green, blue);
            return colorHSV;
        }
        public static ColorHSV FromArgb(int red, int green, int blue)
        {
            ColorHSV colorHSV = new ColorHSV();
            colorHSV.Color = Color.FromArgb(red, green, blue);
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
}
