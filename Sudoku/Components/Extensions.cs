using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DjSoft.Games.Animated
{
    public static class Extensions
    {
        #region Point, Size, Rectangle
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftByX"></param>
        /// <param name="shiftByY"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle bounds, int shiftByX, int shiftByY)
        {
            return new Rectangle(bounds.X + shiftByX, bounds.Y + shiftByY, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftByX"></param>
        /// <param name="shiftByY"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF bounds, float shiftByX, float shiftByY)
        {
            return new RectangleF(bounds.X + shiftByX, bounds.Y + shiftByY, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle bounds, Point shiftBy)
        {
            return new Rectangle(bounds.X + shiftBy.X, bounds.Y + shiftBy.Y, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF bounds, PointF shiftBy)
        {
            return new RectangleF(bounds.X + shiftBy.X, bounds.Y + shiftBy.Y, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle bounds, Size shiftBy)
        {
            return new Rectangle(bounds.X + shiftBy.Width, bounds.Y + shiftBy.Height, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF bounds, SizeF shiftBy)
        {
            return new RectangleF(bounds.X + shiftBy.Width, bounds.Y + shiftBy.Height, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this point vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Point Zoom(this Point point, float zoom)
        {
            return Point.Round(Zoom((PointF)point, zoom));
        }
        /// <summary>
        /// Vrátí this point vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static PointF Zoom(this PointF point, float zoom)
        {
            return new PointF(zoom * point.X, zoom * point.Y);
        }
        /// <summary>
        /// Vrátí this Size vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Size Zoom(this Size size, float zoom)
        {
            return Size.Round(Zoom((SizeF)size, zoom));
        }
        /// <summary>
        /// Vrátí this SizeF vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static SizeF Zoom(this SizeF size, float zoom)
        {
            return new SizeF(zoom * size.Width, zoom * size.Height);
        }
        /// <summary>
        /// Vrátí this Rectangle vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Rectangle Zoom(this Rectangle bounds, float zoom)
        {
            return Rectangle.Round(Zoom((RectangleF)bounds, zoom));
        }
        /// <summary>
        /// Vrátí this RectangleF vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static RectangleF Zoom(this RectangleF bounds, float zoom)
        {
            return new RectangleF(zoom * bounds.X, zoom * bounds.Y, zoom * bounds.Width, zoom * bounds.Height);
        }

        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment, bool shrinkToFit = false)
        {
            float x = 0f;
            float y = 0f;
            float w = size.Width;
            float h = size.Height;
            if (shrinkToFit)
            {
                if (w > bounds.Width) w = bounds.Width;
                if (h > bounds.Height) h = bounds.Height;
            }
            float dw = bounds.Width - w;
            float dh = bounds.Height - h;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x = dw / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x = dw;
                    break;
                case ContentAlignment.MiddleLeft:
                    y = dh / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x = dw / 2f;
                    y = dh / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x = dw;
                    y = dh / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y = dh;
                    break;
                case ContentAlignment.BottomCenter:
                    x = dw / 2f;
                    y = dh;
                    break;
                case ContentAlignment.BottomRight:
                    x = dw;
                    y = dh;
                    break;
            }
            return new RectangleF(bounds.X + x, bounds.Y + y, w, h);
        }
        #endregion
        #region IEnumerable
        public static DataPair<T>[] GetPairs<T>(this IEnumerable<T> items1, IEnumerable<T> items2, Func<T, T, bool> comparer = null, bool useReferenceEquals = false)
        {
            List<DataPair<T>> result = new List<DataPair<T>>();
            bool hasComparer = (comparer != null);

            if (items1 != null)
            {   // Item1
                foreach (var item1 in items1)
                {   // Každý prvek 1
                    if (!result.Any(p => isEquals(p.Item1, item1)))
                    {   // Pokud dosud tentýž prvek není v result.Item1, tak přidáme new DataPair a dáme do Item1 nalezený prvek:
                        // Tedy v result.Item1 bude vstupní prvek jen 1x, i kdyby na vstupu byl opakovaně:
                        var pair = new DataPair<T>() { Item1 = item1 };
                        result.Add(pair);
                    }
                }
            }

            if (items2 != null)
            {   // Item2
                foreach (var item2 in items2)
                {   // Každý prvek 2
                    if (TryFindFirst(result, p => isEquals(p.Item1, item2), out var foundPair))
                    {   // Pro prvek 2 jsem našel Pair, kde tento prvek je na pozici 1 = máme úplný pár:
                        // Prvek do pozice Item2 dám jen tehdy, když tam dosud žádný není (nepodporujeme "Výměnu manželek" v již kompletním páru):
                        if (!foundPair.HasItem2) foundPair.Item2 = item2;
                    }
                    else
                    {   // Prvek Item2 jsme na straně Item1 nenašli.
                        // Pokud jej nenajdeme ani na straně Item2 (tam by mohl být jako Single pár z dřívějšího výskytu), tak jej na stranu Item2 přidáme:
                        if (!result.Any(p => isEquals(p.Item2, item2)))
                        {   // Pokud dosud tentýž prvek není v result.Item1, tak přidáme new DataPair a dáme do Item1 nalezený prvek:
                            var pair = new DataPair<T>() { Item2 = item2 };
                            result.Add(pair);
                        }
                    }
                }
            }

            return result.ToArray();

            bool isEquals(T item1, T item2)
            {
                if (hasComparer) return (comparer(item1, item2));
                if (useReferenceEquals) return Object.ReferenceEquals(item1, item2); 
                return Object.Equals(item1, item2);
            }
        }

        public static bool TryFindFirst<T>(this IEnumerable<T> items, Func<T, bool> filter, out T found)
        {
            if (!(items is null))
            {
                bool hasFilter = (filter != null);
                foreach (var i in items)
                {
                    if (!hasFilter || filter(i))
                    {
                        found = i;
                        return true;
                    }
                }
            }
            found = default(T);
            return false;
        }
        #endregion
    }
    /// <summary>
    /// Pár dvou údajů shodného typu
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataPair<T>
    {
        /// <summary>
        /// Prvek 1. Pokud není setován, pak <see cref="HasItem1"/> je false, po setování jakékoli hodnoty je <see cref="HasItem1"/> = true.
        /// </summary>
        public T Item1 { get { return __Item1; } set { __Item1 = value; __HasItem1 = true; } } private T __Item1;
        /// <summary>
        /// Obsahuje true pokud byla setována nějaká hodnota do <see cref="Item1"/>, i kdyby null.
        /// </summary>
        public bool HasItem1 { get { return __HasItem1; } } private bool __HasItem1 = false;
        /// <summary>
        /// Prvek 2. Pokud není setován, pak <see cref="HasItem2"/> je false, po setování jakékoli hodnoty je <see cref="HasItem2"/> = true.
        /// </summary>
        public T Item2 { get { return __Item2; } set { __Item2 = value; __HasItem2 = true; } } private T __Item2;
        /// <summary>
        /// Obsahuje true pokud byla setována nějaká hodnota do <see cref="Item2"/>, i kdyby null.
        /// </summary>
        public bool HasItem2 { get { return __HasItem2; } } private bool __HasItem2 = false;
    }
}
