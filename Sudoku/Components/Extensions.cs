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
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shiftByX"></param>
        /// <param name="shiftByY"></param>
        /// <returns></returns>
        public static Point ShiftBy(this Point point, int shiftByX, int shiftByY)
        {
            return new Point(point.X + shiftByX, point.Y + shiftByY);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shiftByX"></param>
        /// <param name="shiftByY"></param>
        /// <returns></returns>
        public static PointF ShiftBy(this PointF point, float shiftByX, float shiftByY)
        {
            return new PointF(point.X + shiftByX, point.Y + shiftByY);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static Point ShiftBy(this Point point, Point shiftBy)
        {
            return new Point(point.X + shiftBy.X, point.Y + shiftBy.Y);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static PointF ShiftBy(this PointF point, PointF shiftBy)
        {
            return new PointF(point.X + shiftBy.X, point.Y + shiftBy.Y);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static Point ShiftBy(this Point point, Size shiftBy)
        {
            return new Point(point.X + shiftBy.Width, point.Y + shiftBy.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static PointF ShiftBy(this PointF point, SizeF shiftBy)
        {
            return new PointF(point.X + shiftBy.Width, point.Y + shiftBy.Height);
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
        /// <param name="bounds"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static RectangleF Zoom(this RectangleF bounds, float zoom)
        {
            return new RectangleF(zoom * bounds.X, zoom * bounds.Y, zoom * bounds.Width, zoom * bounds.Height);
        }
        /// <summary>
        /// Vrátí this point vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <param name="zoomX">Měřítko ve směru X</param>
        /// <param name="zoomY">Měřítko ve směru Y</param>
        /// <returns></returns>
        public static Point Zoom(this Point point, float zoomX, float zoomY)
        {
            return Point.Round(Zoom((PointF)point, zoomX, zoomY));
        }
        /// <summary>
        /// Vrátí this point vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <param name="zoomX">Měřítko ve směru X</param>
        /// <param name="zoomY">Měřítko ve směru Y</param>
        /// <returns></returns>
        public static PointF Zoom(this PointF point, float zoomX, float zoomY)
        {
            return new PointF(zoomX * point.X, zoomY * point.Y);
        }
        /// <summary>
        /// Vrátí this Size vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoomX">Měřítko ve směru X</param>
        /// <param name="zoomY">Měřítko ve směru Y</param>
        /// <returns></returns>
        public static Size Zoom(this Size size, float zoomX, float zoomY)
        {
            return Size.Round(Zoom((SizeF)size, zoomX, zoomY));
        }
        /// <summary>
        /// Vrátí this SizeF vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoomX">Měřítko ve směru X</param>
        /// <param name="zoomY">Měřítko ve směru Y</param>
        /// <returns></returns>
        public static SizeF Zoom(this SizeF size, float zoomX, float zoomY)
        {
            return new SizeF(zoomX * size.Width, zoomY * size.Height);
        }
        /// <summary>
        /// Vrátí this Rectangle vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="zoomX">Měřítko ve směru X</param>
        /// <param name="zoomY">Měřítko ve směru Y</param>
        /// <returns></returns>
        public static Rectangle Zoom(this Rectangle bounds, float zoomX, float zoomY)
        {
            return Rectangle.Round(Zoom((RectangleF)bounds, zoomX, zoomY));
        }
        /// <summary>
        /// Vrátí this RectangleF vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="zoomX">Měřítko ve směru X</param>
        /// <param name="zoomY">Měřítko ve směru Y</param>
        /// <returns></returns>
        public static RectangleF Zoom(this RectangleF bounds, float zoomX, float zoomY)
        {
            return new RectangleF(zoomX * bounds.X, zoomY * bounds.Y, zoomX * bounds.Width, zoomY * bounds.Height);
        }

        /// <summary>
        /// Vrátí new <see cref="RectangleF"/>, vytvořený okolo středu dodaného <see cref="RectangleF"/>, s velikostí upravenou Zoomem z původní velikosti.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static RectangleF ZoomCenter(this RectangleF bounds, float zoom)
        {
            var center = bounds.GetCenter();
            var size = bounds.Size.Zoom(zoom);
            return center.GetBoundsFromCenter(size);
        }
        /// <summary>
        /// Vrací souřadnici středu daného prostoru
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static PointF GetCenter(this RectangleF bounds)
        {
            return new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
        }
        /// <summary>
        /// Vrací souřadnici prostoru <see cref="RectangleF"/>, vytvořenou z dodaného středu okolo něhož umístí dodanou velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF GetBoundsFromCenter(this PointF center, SizeF size)
        {
            PointF location = new PointF(center.X - size.Width / 2f, center.Y - size.Height / 2f);
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrací souřadnici prostoru <see cref="RectangleF"/>, vytvořenou z dodaného středu okolo něhož umístí dodanou velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static RectangleF GetBoundsFromCenter(this PointF center, float width, float height)
        {
            PointF location = new PointF(center.X - width / 2f, center.Y - height / 2f);
            return new RectangleF(location.X, location.Y, width, height);
        }

        /// <summary>
        /// Vrátí this velikost zarovanou do daného prostoru s daným <paramref name="alignment"/>.
        /// Pokud <paramref name="shrinkToFit"/> je true, pak danou velikost nejprve zmenší na velikost prostoru (v každé souřadnici zvlášť, bez zachování poměru stran).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="shrinkMode"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment, ShrinkSizeMode shrinkMode = ShrinkSizeMode.None)
        {
            float x = 0f;
            float y = 0f;
            float w = size.Width;
            float h = size.Height;

            float bx = bounds.X;
            float by = bounds.Y;
            float bw = bounds.Width;
            float bh = bounds.Height;

            switch (shrinkMode)
            {
                case ShrinkSizeMode.ShrinkWithPreserveRatio:
                    if (bw > 0f && bh > 0f)
                    {   // Cílový prostor je reálný:
                        float rw = bw / w;                 // Poměr zmenšení šířky
                        float rh = bh / h;                 // Poměr zmenšení výšky
                        float r = (rw < rh ? rw : rh);     // Ten menší poměr
                        if (r < 1f)
                        {   // Změna velikosti cílového prostoru bude pro obě osy stejným poměrem, a provede se pouze pokud bude menší než 1:
                            w = r * w;
                            h = r * h;
                        }
                    }
                    else
                    {   // Cílový prostor je nulový:
                        w = 0f;
                        h = 0f;
                    }
                    break;
                case ShrinkSizeMode.ShrinkToFit:
                    if (w > bw) w = bw;
                    if (h > bh) h = bh;
                    break;
            }

            float dw = bw - w;
            float dh = bh - h;
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
            return new RectangleF(bx + x, by + y, w, h);
        }
        #endregion
        #region IEnumerable
        /// <summary>
        /// Metoda vyhledá první prvek kolekce vyhovující danému filtru, vrátí true = nalezeno (pak je prvek v out <paramref name="found"/>) nebo flse = nenalezeno.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="filter"></param>
        /// <param name="found"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Metoda vytvoří kolekci obsahující páry (DataPair) ze dvou vstupních kolekcí: this kolekce a druhá dodaná.
        /// Vstupní prvky z první kolekce (this) budou v property <see cref="DataPair{T}.Item1"/> (pokud by na vstupu byly opakované, pak na výstupu budou DISTINCT).
        /// Vstupní prvky z druhé kolekce (<paramref name="items2"/>) budou v property <see cref="DataPair{T}.Item2"/> , a to buď v páru u odpovídající položky s prvkemz první kolekce, nebo single.
        /// <para/>
        /// Párování provádí funkce <paramref name="pairComparer"/>, a pokud není dodána, 
        /// pak podle předvolby <paramref name="useReferenceEquals"/> buď metoda <see cref="Object.ReferenceEquals(object, object)"/>
        /// anebo <see cref="object.Equals(object, object)"/>.
        /// <para/>
        /// Výstupní pole nikdy není null, může mít 0 prvků.
        /// Výstupní prvky mají buď jeden prvek (1 nebo 2) anebo mají oba prvky, ale nikdy nejsou prázdné.
        /// <para/>
        /// Pokud je identický prvek opakovaně v prvním poli, pak ve výstupním poli bude jen jeho první výskyt.
        /// Pokud je identický prvek opakovaně v druhém poli, pak v páru k prvnímu prvku bude jen první výskyt z druhého pole.
        /// Pokud takový prvek v prvním poli nebyl, bude ve výstupu jako Single v Item2, ale pouze jeho první výskyt.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items1"></param>
        /// <param name="items2"></param>
        /// <param name="pairComparer">Specifická metoda pro porovnání dvou prvků, zda "jdou do páru"</param>
        /// <param name="useReferenceEquals">Pokud není dodána párovací funkce <paramref name="pairComparer"/>, 
        /// pak true = použije se <see cref="Object.ReferenceEquals(object, object)"/>;
        /// nebo false = použije se <see cref="Object.Equals(object, object)"/>;</param>
        /// <returns></returns>
        public static DataPair<T>[] GetPairs<T>(this IEnumerable<T> items1, IEnumerable<T> items2, Func<T, T, bool> pairComparer = null, bool useReferenceEquals = false)
        {
            List<DataPair<T>> result = new List<DataPair<T>>();
            bool hasComparer = (pairComparer != null);

            if (items1 != null)
            {   // Items1 => Item1
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
            {   // Items2 => Item2
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
                if (hasComparer) return (pairComparer(item1, item2));
                if (useReferenceEquals) return Object.ReferenceEquals(item1, item2); 
                return Object.Equals(item1, item2);
            }
        }
        #endregion
    }
    #region class DataPair
    /// <summary>
    /// Pár dvou údajů shodného typu
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataPair<T>
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text1 = HasItem1 ? (Item1?.ToString() ?? "NULL") : "None";
            string text2 = HasItem2 ? (Item2?.ToString() ?? "NULL") : "None";
            return $"Item1: {text1}  |  Item2: {text2}";
        }
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
    #endregion
    #region Obecné enumy
    /// <summary>
    /// Režim zmenšení velikosti objektu do cílového prostoru
    /// </summary>
    public enum ShrinkSizeMode
    {
        /// <summary>
        /// Nezmenšovat = objektu bude přesahovat
        /// </summary>
        None,
        /// <summary>
        /// Zmenšit tak, aby se objekt vešel, a zachovat jeho poměr stran
        /// </summary>
        ShrinkWithPreserveRatio,
        /// <summary>
        /// Zmenšit rozměr Width nebo Height nezávisle
        /// </summary>
        ShrinkToFit
    }
    #endregion
}
