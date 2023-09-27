using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
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
}
