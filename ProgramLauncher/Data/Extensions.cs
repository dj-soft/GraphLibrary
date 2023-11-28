using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static DjSoft.Tools.ProgramLauncher.App;
using System.Drawing.Imaging;
using System.Windows.Forms;
using DjSoft.Tools.ProgramLauncher.Components;

namespace DjSoft.Tools.ProgramLauncher
{
    public static class Extensions
    {
        #region Rectangle a Point
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která vychází z this a je zmenšená (dovnitř) na každé straně o dané pixely
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public static Rectangle GetInnerBounds(this Rectangle bounds, int dx, int dy)
        {
            int dw = dx + dx;
            int dh = dy + dy;
            if (bounds.Width <= dw || bounds.Height <= dh) return Rectangle.Empty;
            return new Rectangle(bounds.X + dx, bounds.Y + dy, bounds.Width - dw, bounds.Height - dh);
        }
        /// <summary>
        /// Vrátí instanci <see cref="GraphicsPath"/> (pozor: <see cref="IDisposable"/>), která vychází z this a reprezentuje obdélník se zaoblenými vrcholy
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public static GraphicsPath GetRoundedRectanglePath(this Rectangle bounds, int round)
        {
            GraphicsPath gp = new GraphicsPath();

            int minDim = (bounds.Width < bounds.Height ? bounds.Width : bounds.Height);
            int minRound = minDim / 3;
            if (round >= minRound) round = minRound;
            if (round <= 1)
            {   // Malý prostor nebo malý Round => bude to Rectangle
                gp.AddRectangle(bounds);
            }
            else
            {   // Máme bounds = vnější prostor, po něm jdou linie
                // a roundBounds = vnitřní prostor, určuje souřadnice začátku oblouku (Round):
                var roundBounds = GetInnerBounds(bounds, round, round);
                gp.AddLine(roundBounds.Left, bounds.Top, roundBounds.Right, bounds.Top);                                                                       // Horní rovná linka zleva doprava, její Left a Right jsou z Round souřadnic
                gp.AddBezier(roundBounds.Right, bounds.Top, bounds.Right, bounds.Top, bounds.Right, bounds.Top, bounds.Right, roundBounds.Top);                // Pravý horní oblouk doprava a dolů
                gp.AddLine(bounds.Right, roundBounds.Top, bounds.Right, roundBounds.Bottom);                                                                   // Pravá rovná linka zhora dolů
                gp.AddBezier(bounds.Right, roundBounds.Bottom, bounds.Right, bounds.Bottom, bounds.Right, bounds.Bottom, roundBounds.Right, bounds.Bottom);    // Pravý dolní oblouk dolů a doleva
                gp.AddLine(roundBounds.Right, bounds.Bottom, roundBounds.Left, bounds.Bottom);                                                                 // Dolní rovná linka zprava doleva
                gp.AddBezier(roundBounds.Left, bounds.Bottom, bounds.Left, bounds.Bottom, bounds.Left, bounds.Bottom, bounds.Left, roundBounds.Bottom);        // Levý dolní oblouk doleva a nahoru
                gp.AddLine(bounds.Left, roundBounds.Bottom, bounds.Left, roundBounds.Top);                                                                     // Levá rovná linka zdola nahoru
                gp.AddBezier(bounds.Left, roundBounds.Top, bounds.Left, bounds.Top, bounds.Left, bounds.Top, roundBounds.Left, bounds.Top);                    // Levý horní oblouk nahoru a doprava
                gp.CloseFigure();
            }
            return gp;
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Point"/>, která vychází z this a je posunutá o danou pozici <paramref name="offset"/>.
        /// Pokud <paramref name="offset"/> je null, vrací přímo vstupní hodnotu.
        /// Lze specifikovat <paramref name="negative"/> = true, pak bude posun záporný.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Point Add(this Point point, Point? offset, bool negative = false)
        {
            if (!offset.HasValue) return point;
            if (!negative) return new Point(point.X + offset.Value.X, point.Y + offset.Value.Y);
            return new Point(point.X - offset.Value.X, point.Y - offset.Value.Y);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Point"/>, která vychází z this a je posunutá o danou pozici
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <returns></returns>
        public static Point Add(this Point bounds, int offsetX, int offsetY)
        {
            return new Point(bounds.X + offsetX, bounds.Y + offsetY);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která vychází z this a je posunutá o danou pozici <paramref name="offset"/>.
        /// Pokud <paramref name="offset"/> je null, vrací přímo vstupní hodnotu.
        /// Lze specifikovat <paramref name="negative"/> = true, pak bude posun záporný.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle bounds, Point? offset, bool negative = false)
        {
            if (!offset.HasValue) return bounds;
            if (!negative) return new Rectangle(bounds.X + offset.Value.X, bounds.Y + offset.Value.Y, bounds.Width, bounds.Height);
            return new Rectangle(bounds.X - offset.Value.X, bounds.Y - offset.Value.Y, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která vychází z this a je posunutá o danou pozici
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle bounds, int offsetX, int offsetY)
        {
            return new Rectangle(bounds.X + offsetX, bounds.Y + offsetY, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která má daný střed a velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point center, Size size)
        {
            return new Rectangle(center.X - (size.Width / 2), center.Y - (size.Height / 2), size.Width, size.Height);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která má daný střed a velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point center, int width, int height)
        {
            return new Rectangle(center.X - (width / 2), center.Y - (height / 2), width, height);
        }
        /// <summary>
        /// Vrátí true, pokud this <see cref="Rectangle"/> má kladnou šířku i výšku
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static bool HasContent(this Rectangle bounds)
        {
            return (bounds.Width > 0 && bounds.Height > 0);
        }
        /// <summary>
        /// Vrátí true, pokud this <see cref="Size"/> má kladnou šířku i výšku
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool HasContent(this Size size)
        {
            return (size.Width > 0 && size.Height > 0);
        }
        /// <summary>
        /// Vrátí počet pixelů daného prostoru. Pokud bude některý rozměr záporný, může se vrátit 0 podle <paramref name="negativeAsZero"/>.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="negativeAsZero"></param>
        /// <returns></returns>
        public static int GetPixelsCount(this Rectangle bounds, bool negativeAsZero = false)
        {
            return GetPixelsCount(bounds.Size, negativeAsZero);
        }
        /// <summary>
        /// Vrátí počet pixelů daného prostoru. Pokud bude některý rozměr záporný, může se vrátit 0 podle <paramref name="negativeAsZero"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="negativeAsZero"></param>
        /// <returns></returns>
        public static int GetPixelsCount(this Size size, bool negativeAsZero = false)
        {
            int w = size.Width;
            int h = size.Height;
            if ((w < 0 || h < 0) && negativeAsZero) return 0;
            return w * h;
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(-1) vrátí hodnotu: {51, 11, 28, 18}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="all">Změna aplikovaná na všechny strany</param>
        public static Rectangle Enlarge(this Rectangle r, int all)
        {
            return r.Enlarge(all, all, all, all);
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(1, 1, 1, 1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(0, 0, -1, -1) vrátí hodnotu: {50, 10, 29, 19}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="left">Zvětšení doleva (zmenší X a zvětší Width)</param>
        /// <param name="top">Zvětšení nahoru (zmenší Y a zvětší Height)</param>
        /// <param name="right">Zvětšení doprava (zvětší Width)</param>
        /// <param name="bottom">Zvětšení dolů (zvětší Height)</param>
        public static Rectangle Enlarge(this Rectangle r, int left, int top, int right, int bottom)
        {
            int x = r.X - left;
            int y = r.Y - top;
            int w = r.Width + left + right;
            int h = r.Height + top + bottom;
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která plně pokrývá danou float oblast <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static Rectangle GetOuterBounds(this RectangleF bounds)
        {
            int l = (int)Math.Floor(bounds.Left);
            int t = (int)Math.Floor(bounds.Top);
            int r = (int)Math.Ceiling(bounds.Right);
            int b = (int)Math.Ceiling(bounds.Bottom);
            return Rectangle.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Metoda najde nejbližší monitor k dané souřadnici, a danou souřadnici zarovná do souřadnic tohoto monitoru
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="canShrink"></param>
        /// <param name="isMaximizedBounds"></param>
        /// <returns></returns>
        public static Rectangle AlignToNearestMonitor(this Rectangle bounds, bool canShrink = false, bool isMaximizedBounds = false)
        {
            Rectangle testBounds = bounds;
            // true = budeme nejprve souřadnice bounds zmenšovat, protože vstupní bounds jsou Maximized, a ty přesahují o 8px hranice monitoru na všechny 4 strany!!!
            bool isShrinked = (isMaximizedBounds && bounds.Width >= 30 && bounds.Height >= 30);
            if (isShrinked) testBounds = testBounds.Enlarge(-12);
            var monitorBounds = Monitors.GetNearestMonitorBounds(testBounds);
            var alignedBounds = AlignToBounds(testBounds, monitorBounds, canShrink);
            // Pokud jsme vstupní bounds zmenšili, tak nyní je zpátky zvětšíme (aby byly opět Maximized):
            if (isShrinked) alignedBounds = alignedBounds.Enlarge(12);
            return alignedBounds;
        }
        /// <summary>
        /// Metoda zajistí zarovnání aktuálních souřadnic (this) do daných vnějších souřadnic.
        /// Nejprve souřadnici <paramref name="bounds"/> přesune do vnitřní části <paramref name="outerBounds"/> tak, aby se nezměnila velikost <paramref name="bounds"/>.
        /// Pak pokud bude <paramref name="canShrink"/> = true, pak souřadnice může i v případě potřeby zmenšit (Width, Height) tak, aby nepřesahovaly ven.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="outerBounds"></param>
        /// <param name="canShrink"></param>
        /// <returns></returns>
        public static Rectangle AlignToBounds(this Rectangle bounds, Rectangle outerBounds, bool canShrink = false)
        {
            // Souřadnice, s nimiž budu pohybovat:
            int left = bounds.Left;
            int top = bounds.Top;
            int right = bounds.Right;
            int bottom = bounds.Bottom;
            int width = right - left;
            int height = bottom - top;

            // Odsuneme zprava doleva, a zdola nahoru:
            if (right > outerBounds.Right)
            {
                right = outerBounds.Right;
                left = right - width;
            }
            if (bottom > outerBounds.Bottom)
            {
                bottom = outerBounds.Bottom;
                top = bottom - height;
            }

            // Odsuneme zleva doprava, a shora dolů:
            if (left < outerBounds.Left)
            {
                left = outerBounds.Left;
                right = left + width;
            }
            if (top < outerBounds.Top)
            {
                top = outerBounds.Top;
                bottom = top + height;
            }

            // Pokud můžu zmenšovat, tak až nakonec:
            if (canShrink)
            {
                if (right > outerBounds.Right)
                    width = outerBounds.Right - left;

                if (bottom > outerBounds.Bottom)
                    height = outerBounds.Bottom - top;
            }

            // Hotovo:
            return new Rectangle(left, top, width, height);
        }
        /// <summary>
        /// Metoda určí vzájemný vztah dvou Rectangle:
        /// Pokud nemají společný prostor, pak určí jejich nejmenší vzdálenost do out <paramref name="distance"/>;
        /// Pokud mají společný prostor, pak určí jeho souřadnice do out <paramref name="commonBounds"/>.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <param name="commonBounds"></param>
        /// <returns></returns>
        public static void DetectRelation(this Rectangle bounds, Rectangle target, out int? distance, out Rectangle? commonBounds)
        {
            _DetectRelation(bounds, target, out distance, out commonBounds);
        }
        /// <summary>
        /// Metoda vrátí nejmenší vzdálenost mezi dvěma prostory.
        /// Pokud prostory na sebe navazují, vrací 0.
        /// Pokud se prostory překrývají, vrací null: pak je možno použít metodu <see cref="GetCommonBounds(Rectangle, Rectangle)"/>.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int? GetDistanceTo(this Rectangle bounds, Rectangle target)
        {
            _DetectRelation(bounds, target, out int? distance, out Rectangle? _);
            return distance;
        }
        /// <summary>
        /// Metoda vrátí společný prostor, který mají prostor this a <paramref name="target"/>.
        /// Pokud nemají společný prostor, vrátí null.
        /// Společný prostor nemají ani tehdy, když konec (např. Right) jednoho prostoru je roven počátku druhého (Left).
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Rectangle? GetCommonBounds(this Rectangle bounds, Rectangle target)
        {
            _DetectRelation(bounds, target, out int? _, out Rectangle? commonBounds);
            return commonBounds;
        }
        /// <summary>
        /// Metoda určí vzájemný vztah dvou Rectangle:
        /// Pokud nemají společný prostor, pak určí jejich nejmenší vzdálenost;
        /// Pokud mají společný prostor, pak určí jeho souřadnice.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static void _DetectRelation(Rectangle bounds, Rectangle target, out int? distance, out Rectangle? commonBounds)
        {
            distance = null;
            commonBounds = null;

            // Záporné velikosti nebudeme řešit:
            if (bounds.Width < 0 || bounds.Height < 0 || target.Width < 0 || target.Height < 0) return;

            // Zjistíme, zda target leží v ose X na pozici Před (včetně dotyk vlevo) nebo Přes nebo Za (včetně dotyk vpravo):
            var posX = getPosition(bounds.Left, bounds.Right, target.Left, target.Right, out int? distX, out int? innerLeft, out int? innerRight);
            var posY = getPosition(bounds.Top, bounds.Bottom, target.Top, target.Bottom, out int? distY, out int? innerTop, out int? innerBottom);
            var pos = posX + posY;

            // Nyní mám určenou vzájemnou pozici v obou směrech (X i Y), vyřešíme tedy matici 3x3:
            switch (pos)
            {
                case "BB":             // X: Before;  Y: Before  =>  Vlevo nahoře
                case "BA":             // X: Before;  Y: After   =>  Vlevo dole
                case "AB":             // X: After;   Y: Before  =>  Vpravo nahoře
                case "AA":             // X: After;   Y: After   =>  Vpravo dole
                    distance = getHypotenuse(distX.Value, distY.Value);
                    break;
                case "BO":             // X: Before;  Y: Over    =>  Nalevo
                case "AO":             // X: After;   Y: Over    =>  Napravo
                    distance = distX.Value;
                    break;
                case "OB":             // X: Over;    Y: Before  =>  Nad
                case "OA":             // X: Over;    Y: After   =>  Pod
                    distance = distY.Value;
                    break;
                case "OO":             // X: Over;    Y: Over    =>  Přes
                    commonBounds = Rectangle.FromLTRB(innerLeft.Value, innerTop.Value, innerRight.Value, innerBottom.Value);
                    break;
            }

            // Určí pozici dvou intervalů,
            //  a pokud bude Before či After, pak určí vzdálenost Dist (kladná nebo nula);
            //  pokud bude Over pak určí pozici innerBegin (větší Begin) a innerEnd (menší End)
            string getPosition(int boundsBegin, int boundsEnd, int targetBegin, int targetEnd, out int? dist, out int? innerBegin, out int? innerEnd)
            {
                
                if (targetEnd <= boundsBegin) 
                { 
                    dist = boundsBegin - targetEnd; 
                    innerBegin = null; 
                    innerEnd = null; 
                    return "B";                  // Before
                }

                if (targetBegin >= boundsEnd) 
                {
                    dist = targetBegin - boundsEnd; 
                    innerBegin = null; 
                    innerEnd = null; 
                    return "A";                  // After
                }

                dist = null;
                innerBegin = (boundsBegin > targetBegin ? boundsBegin : targetBegin);
                innerEnd = (boundsEnd < targetEnd ? boundsEnd : targetEnd);
                return "O";                      // Over
            }

            // Vrátí délku přepony nad dvěma odvěsnami v pravoúhlém trojúhelníku
            int getHypotenuse(int pendantA, int pendantB)
            {
                double hypotenuse = Math.Sqrt(pendantA * pendantA + pendantB * pendantB);
                return (int)Math.Round(hypotenuse, 0);
            }
        }
        /// <summary>
        /// Vrátí daný bod v this prostoru
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Point GetPoint(this Rectangle bounds, RectanglePointPosition position)
        { 
            switch (position)
            {
                case RectanglePointPosition.TopLeft: return new Point(bounds.Left, bounds.Top);
                case RectanglePointPosition.TopCenter: return new Point(center(bounds.X, bounds.Width), bounds.Top);
                case RectanglePointPosition.TopRight: return new Point(bounds.Right, bounds.Top);
                case RectanglePointPosition.CenterLeft: return new Point(bounds.Left, center(bounds.Y, bounds.Height));
                case RectanglePointPosition.Center: return new Point(center(bounds.X, bounds.Width), center(bounds.Y, bounds.Height));
                case RectanglePointPosition.CenterRight: return new Point(bounds.Right, center(bounds.Y, bounds.Height));
                case RectanglePointPosition.BottomLeft: return new Point(bounds.Left, bounds.Bottom);
                case RectanglePointPosition.BottomCenter: return new Point(center(bounds.X, bounds.Width), bounds.Bottom);
                case RectanglePointPosition.BottomRight: return new Point(bounds.Right, bounds.Bottom);
            }
            throw new ArgumentException($"Rectangle.GetCenter() error: argument 'position' is invalid: {position}");

            int center(int begin, int size) { return begin + (size / 2); }
        }
        /// <summary>
        /// Metoda vezme this velikost a umístí ji do zadaného prostoru do daného místa.
        /// Volitelně lze řídit, zda zmenšit this velikost, pokud cílový prostor <paramref name="targetBounds"/> je menší, podle parametru <paramref name="cropToTarget"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="targetBounds"></param>
        /// <param name="contentAlignment"></param>
        /// <param name="cropToTarget"></param>
        /// <returns></returns>
        public static RectangleF AlignContentTo(this SizeF size, RectangleF targetBounds, ContentAlignment contentAlignment, bool cropToTarget = true)
        {
            float sw = size.Width;
            float sh = size.Height;
            float tw = targetBounds.Width;
            float th = targetBounds.Height;
            if (cropToTarget)
            {
                if (sw > tw) sw = tw;
                if (sh > th) sh = th;
            }
            float ax = 0f;
            float ay = 0f;
            float dw = tw - sw;
            float dh = th - sh;
            switch (contentAlignment) 
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    ax = dw / 2f;
                    break;
                case ContentAlignment.TopRight:
                    ax = dw;
                    break;
                case ContentAlignment.MiddleLeft:
                    ay = dh / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    ax = dw / 2f;
                    ay = dh / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    ax = dw;
                    ay = dh / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    ay = dh;
                    break;
                case ContentAlignment.BottomCenter:
                    ax = dw / 2f;
                    ay = dh;
                    break;
                case ContentAlignment.BottomRight:
                    ax = dw;
                    ay = dh;
                    break;
            }
            return new RectangleF(targetBounds.X + ax, targetBounds.Y + ay, sw, sh);
        }
        #endregion
        #region Graphics - FountainFill, Draw
        /// <summary>
        /// Do this <paramref name="Graphics"/> vyplní dané souřadnice <paramref name="bounds"/> danou barvou <paramref name="color"/> v daném interakticním stavu <paramref name="interactiveState"/>.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="interactiveState"></param>
        public static void FountainFill(this Graphics graphics, Rectangle bounds, Color color, Components.InteractiveState interactiveState = Components.InteractiveState.Enabled, ColorSet colorSet = null, float? alpha = null)
        {
            _GetFountainFillDirection(interactiveState, colorSet, out float morph, out FountainDirection direction);
            FountainFill(graphics, bounds, color, morph, direction, alpha);
        }
        public static void FountainFill(this Graphics graphics, GraphicsPath path, Color color, Components.InteractiveState interactiveState = Components.InteractiveState.Enabled, ColorSet colorSet = null, float? alpha = null)
        {
            _GetFountainFillDirection(interactiveState, colorSet, out float morph, out FountainDirection direction);
            FountainFill(graphics, path, color, morph, direction, alpha);
        }
        public static void FountainFill(this Graphics graphics, Rectangle bounds, Color color, float morph, FountainDirection direction, float? alpha = null)
        {
            if (morph != 0f && direction != FountainDirection.None)
            {
                _GetFountainFillColors(color, morph, out Color colorBegin, out Color colorEnd, alpha);
                using (var brush = CreateLinearGradientBrush(bounds, colorBegin, colorEnd, direction))
                    graphics.FillRectangle(brush, bounds);
            }
            else
            {   // Bez 3D efektu:
                FillRectangle(graphics, bounds, color, alpha);
            }
        }
        public static void FountainFill(this Graphics graphics, GraphicsPath path, Color color, float morph, FountainDirection direction, float? alpha = null)
        {
            if (morph != 0f && direction != FountainDirection.None)
            {
                Rectangle bounds = path.GetBounds().GetOuterBounds();
                _GetFountainFillColors(color, morph, out Color colorBegin, out Color colorEnd, alpha);
                using (var brush = CreateLinearGradientBrush(bounds, colorBegin, colorEnd, direction))
                    graphics.FillPath(brush, path);
            }
            else
            {   // Bez 3D efektu:
                FillPath(graphics, path, color, alpha);
            }
        }
        /// <summary>
        /// Pro zadaný interaktivní stav <paramref name="interactiveState"/> určí hodnoty <paramref name="morph"/> a <paramref name="direction"/>
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <param name="colorSet"></param>
        /// <param name="morph"></param>
        /// <param name="direction"></param>
        private static void _GetFountainFillDirection(Components.InteractiveState interactiveState, ColorSet colorSet, out float morph, out FountainDirection direction)
        {
            switch (interactiveState)
            {
                case Components.InteractiveState.MouseOn:
                    morph = colorSet?.MouseOn3DMorph ?? 0.10f;
                    direction = FountainDirection.ToDown;
                    return;
                case Components.InteractiveState.MouseDown:
                    morph = colorSet?.MouseDown3DMorph ?? -0.20f;
                    direction = FountainDirection.ToDown;
                    return;
            }
            morph = 0f;
            direction = FountainDirection.None;
        }
        /// <summary>
        /// Určí barvu počátku <paramref name="colorBegin"/> a barvu konce <paramref name="colorEnd"/>
        /// pro zadanou barvu základní <paramref name="color"/>, a daný koeficient změny <paramref name="morph"/>.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="morph"></param>
        /// <param name="colorBegin"></param>
        /// <param name="colorEnd"></param>
        private static void _GetFountainFillColors(Color color, float morph, out Color colorBegin, out Color colorEnd, float? alpha = null)
        {
            colorBegin = color.ChangeColor(morph).GetAlpha(alpha);
            colorEnd = color.ChangeColor(-morph).GetAlpha(alpha);
        }
        /// <summary>
        /// Vytvoří a vrátí new instanci <see cref="LinearGradientBrush"/> pro daný prostor, barvu počátku a konce a daný směr orientace.
        /// Objekt je třeba Disposovat po použití, není recyklovaný (proto je metoda Create*() a nikoli Get*() )
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="colorBegin"></param>
        /// <param name="colorEnd"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static LinearGradientBrush CreateLinearGradientBrush(Rectangle bounds, Color colorBegin, Color colorEnd, FountainDirection direction)
        {
            switch (direction)
            {
                case FountainDirection.ToDown:
                    bounds = bounds.Enlarge(0, 1, 0, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, colorBegin, colorEnd, LinearGradientMode.Vertical);
                case FountainDirection.ToLeft:
                    bounds = bounds.Enlarge(1, 0, 1, 0);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, colorBegin, colorEnd, LinearGradientMode.Horizontal);
                case FountainDirection.ToUp:
                    bounds = bounds.Enlarge(0, 1, 0, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, colorBegin, colorEnd, LinearGradientMode.Vertical);
                case FountainDirection.ToRight:
                default:
                    bounds = bounds.Enlarge(1, 0, 1, 0);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, colorBegin, colorEnd, LinearGradientMode.Horizontal);
            }
        }
        /// <summary>
        /// Směr
        /// </summary>
        public enum FountainDirection
        {
            /// <summary>
            /// Bez přechodového efektu = SolidBrush
            /// </summary>
            None,
            /// <summary>
            /// Zeshora dolů: nahoře je Color1, dole je Color2
            /// </summary>
            ToDown,
            /// <summary>
            /// Zdola nahoru: dole je Color1, nahoře je Color2
            /// </summary>
            ToUp,
            /// <summary>
            /// Zleva doprava: vlevo je Color1, vpravo je Color2
            /// </summary>
            ToRight,
            /// <summary>
            /// Zprava doleva: vpravo je Color1, vlevo je Color2
            /// </summary>
            ToLeft
        }
        /// <summary>
        /// Do aktuální grafiky vyplní daný prostor danou barvou
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        public static void FillRectangle(this Graphics graphics, RectangleF bounds, Color color, float? alpha = null)
        {
            graphics.FillRectangle(App.GetBrush(color, alpha), bounds);
        }
        public static void FillPath(this Graphics graphics, GraphicsPath path, Color color, float? alpha = null)
        {
            graphics.FillPath(App.GetBrush(color, alpha), path);
        }
        /// <summary>
        /// Do aktuální grafiky vyplní daný prostor odpovídajícím stylem interaktivního pozadí
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <param name="alpha"></param>
        public static void FillInteractiveBackArea(this Graphics graphics, Rectangle bounds, ColorSet colorSet, InteractiveState interactiveState, float? alpha = null)
        {
            var color = colorSet.GetColor(interactiveState);
            if (!color.HasValue) color = colorSet.GetColor(InteractiveState.Enabled);

            if (color.HasValue)
            {
                _GetFountainFillDirection(interactiveState, colorSet,out float morph, out FountainDirection direction);
                FountainFill(graphics, bounds, color.Value, morph, direction, alpha);
            }
        }
        public static void DrawText(this Graphics graphics, string text, RectangleF bounds, TextAppearance textAppearance, Components.InteractiveState interactiveState = Components.InteractiveState.Enabled, float? alpha = null, ContentAlignment? contentAlignment = null)
        {
            if (String.IsNullOrEmpty(text)) return;

            var brush = App.GetBrush(textAppearance.TextColors, interactiveState, alpha);
            if (brush is null) return;

            var textounds = bounds;
            var font = App.GetFont(textAppearance, interactiveState);
            var stringFormat = App.GetStringFormatFor(contentAlignment);

            //string test = "Abcde fghij klmno pqrst uvwxy Abcde fghij klmno pqrst uvwxy.";
            //var size0 = graphics.MeasureString(test, font);
            //var size1 = graphics.MeasureString(test, font, 80);
            //var size2 = graphics.MeasureString(test, font, 80, stringFormat);
            //var size3 = graphics.MeasureString(test, font, 80, stringFormat);


            if (contentAlignment.HasValue)
            {
                var size = graphics.MeasureString(text, font, (int)(bounds.Width - 2f));
                if (size.Height > 25f)
                { }
                var alignedBounds = size.AlignContentTo(bounds, contentAlignment.Value);
            }

            graphics.SetForText();
            graphics.DrawString(text, font, brush, textounds, stringFormat);
        }
        public static void DrawText(this Graphics graphics, string text, RectangleF bounds, Color color, SystemFontType? fontType = null, float? emSize = null, FontStyle? fontStyle = null, float? alpha = null, ContentAlignment? contentAlignment = null)
        {
            var brush = App.GetBrush(color.GetAlpha(alpha));
            if (brush is null) return;

            var font = App.GetFont(fontType, emSize, fontStyle);
            var stringFormat = App.GetStringFormatFor(contentAlignment);
            if (contentAlignment.HasValue)
                bounds = graphics.MeasureString(text, font, (int)(bounds.Width - 2f), stringFormat).AlignContentTo(bounds, contentAlignment.Value);

            graphics.SetForText();
            graphics.DrawString(text, font, brush, bounds);
        }
        public static void SetForText(this Graphics graphics)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;             // UHD: OK
            graphics.SmoothingMode = SmoothingMode.AntiAlias;                                                // UHD: OK
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        public static void DrawImage(this Graphics graphics, Image image, RectangleF bounds, float? alpha = null)
        {
            if (image is null) return;
            if (alpha.HasValue && alpha.Value <= 0f) return;
            if (alpha.HasValue && alpha.Value < 1f) _DrawImageAlpha(graphics, image, bounds, alpha.Value);
            else graphics.DrawImage(image, bounds);
        }
        private static void _DrawImageAlpha(Graphics graphics, Image image, RectangleF bounds, float alpha)
        {
            // Initialize the color matrix.
            // Note the value 0.8 in row 4, column 4.
            float[][] matrixItems =
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, alpha, 0},
                new float[] {0, 0, 0, 0, 1}
            };
            ColorMatrix colorMatrix = new ColorMatrix(matrixItems);

            // Create an ImageAttributes object and set its color matrix.
            ImageAttributes imageAtt = new ImageAttributes();
            imageAtt.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            // Now draw the semitransparent bitmap image.
            graphics.DrawImage(image, Rectangle.Round(bounds), 0f, 0f, image.Width, image.Height, GraphicsUnit.Pixel, imageAtt);
        }
        #endregion
        #region Color - Shift, Change, Morph, Contrast ...
        #region Color: Shift
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shift">Posunutí barvy</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shift)
        {
            float r = (float)root.R + shift;
            float g = (float)root.G + shift;
            float b = (float)root.B + shift;
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shiftR">Posunutí barvy pro složku R</param>
        /// <param name="shiftG">Posunutí barvy pro složku G</param>
        /// <param name="shiftB">Posunutí barvy pro složku B</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shiftR, float shiftG, float shiftB)
        {
            float r = (float)root.R + shiftR;
            float g = (float)root.G + shiftG;
            float b = (float)root.B + shiftB;
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shiftA">Posunutí barvy pro složku A</param>
        /// <param name="shiftR">Posunutí barvy pro složku R</param>
        /// <param name="shiftG">Posunutí barvy pro složku G</param>
        /// <param name="shiftB">Posunutí barvy pro složku B</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shiftA, float shiftR, float shiftG, float shiftB)
        {
            float a = (float)root.A + shiftA;
            float r = (float)root.R + shiftR;
            float g = (float)root.G + shiftG;
            float b = (float)root.B + shiftB;
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shift">Posunutí barvy ve struktuře Color: jednotlivé složky nesou offset, kde hodnota 128 odpovídá posunu 0</param>
        /// <returns></returns>
        public static Color Shift(this Color root, Color shift)
        {
            float r = (float)(root.R + shift.R - 128);
            float g = (float)(root.G + shift.G - 128);
            float b = (float)(root.B + shift.B - 128);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací barvu dle daných složek, přičemž složky (a,r,g,b) omezuje do rozsahu 0 - 255.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static Color GetColor(float a, float r, float g, float b)
        {
            int ac = (a < 0f ? 0 : (a > 255f ? 255 : (int)a));
            int rc = (r < 0f ? 0 : (r > 255f ? 255 : (int)r));
            int gc = (g < 0f ? 0 : (g > 255f ? 255 : (int)g));
            int bc = (b < 0f ? 0 : (b > 255f ? 255 : (int)b));
            return Color.FromArgb(ac, rc, gc, bc);
        }
        #endregion
        #region Color: Change
        /// <summary>
        /// Změní barvu.
        /// Změna (Change) není posun (Shift): shift přičítá / odečítá hodnotu, ale změna hodnotu mění koeficientem.
        /// Pokud je hodnota složky například 170 a koeficient změny 0.25, pak výsledná hodnota je +25% od výchozí hodnoty směrem k maximu (255): 170 + 0.25 * (255 - 170).
        /// Obdobně změna dolů -70% z hodnoty 170 dá výsledek 170 - 0.7 * (170).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="change">Změna složek</param>
        /// <returns></returns>
        public static Color ChangeColor(this Color root, float change)
        {
            float r = ChangeCC(root.R, change);
            float g = ChangeCC(root.G, change);
            float b = ChangeCC(root.B, change);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Změní barvu.
        /// Změna (Change) není posun (Shift): shift přičítá / odečítá hodnotu, ale změna hodnotu mění koeficientem.
        /// Pokud je hodnota složky například 170 a koeficient změny 0.25, pak výsledná hodnota je +25% od výchozí hodnoty směrem k maximu (255): 170 + 0.25 * (255 - 170).
        /// Obdobně změna dolů -70% z hodnoty 170 dá výsledek 170 - 0.7 * (170).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="changeR">Změna složky R</param>
        /// <param name="changeG">Změna složky R</param>
        /// <param name="changeB">Změna složky R</param>
        /// <returns></returns>
        public static Color ChangeColor(this Color root, float changeR, float changeG, float changeB)
        {
            float r = ChangeCC(root.R, changeR);
            float g = ChangeCC(root.G, changeG);
            float b = ChangeCC(root.B, changeB);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrátí složku změněnou koeficientem.
        /// </summary>
        /// <param name="colorComponent"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        private static float ChangeCC(int colorComponent, float change)
        {
            float result = (float)colorComponent;
            if (change > 0f)
            {
                result = result + (change * (255f - result));
            }
            else if (change < 0f)
            {
                result = result - (-change * result);
            }
            return result;
        }
        #endregion
        #region Color: Morph
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr (morph): 0=vrací se výchozí barva (this).
        /// Poměr (morph): 1=vrací se barva cílová (other).
        /// Poměr může být i větší než 1 (pak je výsledek ještě za cílovou barvou other),
        /// anebo může být záporný (pak výsledkem je barva na opačné straně než je other).
        /// Hodnota Alpha (=opacity = průhlednost) kanálu se přebírá z this barvy a Morphingem se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        public static Color Morph(this Color root, Color? other, float morph)
        {
            if (!other.HasValue || morph == 0f) return root;
            return _Morph(root, other.Value, morph);
        }
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr morph zde není zadán explicitně, ale je dán hodnotou Alpha kanálu v barvě other (kde 0 odpovídá morph = 0, a 255 odpovídá 1).
        /// Jinými slovy, barva this se transformuje do barvy other natolik výrazně, jak výrazně je barva other viditelná (neprůhledná).
        /// Nelze tedy provádět Morph na opačnou stranu (morph nebude nikdy záporné) ani s přesahem za cílovou barvu (morph nebude nikdy vyšší než 1).
        /// Poměr (Alpha kanál barvy other): 0=vrací se výchozí barva (this).
        /// Poměr (Alpha kanál barvy other): 255=vrací se barva cílová (other).
        /// Poměr tedy nemůže být menší než 0 nebo větší než 1 (255).
        /// Hodnota Alpha výsledné barvy (=opacity = průhlednost) se přebírá z Alpha kanálu this barvy, a tímto Morphingem se nijak nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <returns></returns>
        public static Color Morph(this Color root, Color? other)
        {
            if (!other.HasValue || other.Value.A == 0) return root;
            float morph = ((float)other.Value.A) / 255f;
            return _Morph(root, other.Value, morph);
        }
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr (morph): 0=vrací se výchozí barva (this).
        /// Poměr (morph): 1=vrací se barva cílová (other).
        /// Poměr může být i větší než 1 (pak je výsledek ještě za cílovou barvou other),
        /// anebo může být záporný (pak výsledkem je barva na opačné straně než je other).
        /// Hodnota Alpha (=opacity = průhlednost) kanálu se přebírá z this barvy a Morphingem se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        public static Color? Morph(this Color? root, Color? other, float morph)
        {
            if (!root.HasValue || morph >= 1f) return other;
            if (!other.HasValue || morph <= 0f) return root;
            return _Morph(root.Value, other.Value, morph);
        }
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr morph zde není zadán explicitně, ale je dán hodnotou Alpha kanálu v barvě other (kde 0 odpovídá morph = 0, a 255 odpovídá 1).
        /// Jinými slovy, barva this se transformuje do barvy other natolik výrazně, jak výrazně je barva other viditelná (neprůhledná).
        /// Nelze tedy provádět Morph na opačnou stranu (morph nebude nikdy záporné) ani s přesahem za cílovou barvu (morph nebude nikdy vyšší než 1).
        /// Poměr (Alpha kanál barvy other): 0=vrací se výchozí barva (this).
        /// Poměr (Alpha kanál barvy other): 255=vrací se barva cílová (other).
        /// Poměr tedy nemůže být menší než 0 nebo větší než 1 (255).
        /// Hodnota Alpha výsledné barvy (=opacity = průhlednost) se přebírá z Alpha kanálu this barvy, a tímto Morphingem se nijak nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <returns></returns>
        public static Color? Morph(this Color? root, Color? other)
        {
            if (!root.HasValue || other.Value.A >= 255) return other;
            if (!other.HasValue || other.Value.A == 0) return root;
            float morph = ((float)other.Value.A) / 255f;
            return _Morph(root.Value, other.Value, morph);
        }
        /// <summary>
        /// Vrátí barvu Morph mezi <paramref name="root"/> a <paramref name="other"/> v poměru <paramref name="morph"/> (0 až 1).
        /// Pro hodnoty 0 a menší vrátí <paramref name="root"/>, pro hodnoty 1 a vyšší vrátí <paramref name="other"/>.
        /// Mezilehlé hodnoty <paramref name="morph"/> lineárně interpoluje.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="other"></param>
        /// <param name="morph"></param>
        /// <returns></returns>
        private static Color _Morph(Color root, Color other, float morph)
        {
            if (morph <= 0f) return root;
            if (morph >= 1f) return other;
            float a = root.A;
            float r = _GetMorph(root.R, other.R, morph);
            float g = _GetMorph(root.G, other.G, morph);
            float b = _GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí složku barvy vzniklou morphingem = interpolací.
        /// </summary>
        /// <param name="root">Výchozí složka</param>
        /// <param name="other">Cílová složka</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        private static float _GetMorph(float root, float other, float morph)
        {
            float dist = other - root;
            return root + morph * dist;
        }
        #endregion
        #region Color: Contrast
        /// <summary>
        /// Vrátí kontrastní barvu černou nebo bílou k barvě this.
        /// Tato metoda vrací barvu černou nebo bílou, která je dobře viditelná na pozadí dané barvy (this).
        /// Tato metoda pracuje s fyziologickým jasem každé složky barvy zvlášť (například složka G se jeví jasnější než B, složka R má svůj jas někde mezi nimi).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <returns></returns>
        public static Color Contrast(this Color root)
        {
            // Vypočítám souhrnný jas všech složek se zohledněním koeficientu jejich barevného jasu:
            float rgb = 1.0f * (float)root.R +
                        1.4f * (float)root.G +
                        0.7f * (float)root.B;
            return (rgb >= 395 ? Color.Black : Color.White);      // Součet složek je 0 až 790.5, střed kontrastu je 1/2 = 395
        }
        /// <summary>
        /// Vrátí barvu, která je kontrastní vůči barvě this.
        /// Kontrastní barva leží o dané množství barvy směrem k protilehlé barvě (vždy na opačnou stranu od hodnoty 128), v každé složce zvlášť.
        /// Například ke složce s hodnotou 160 je kontrastní barvou o 32 hodnota (160-32) = 128, k hodnotě 100 o 32 je kontrastní (100+32) = 132.
        /// Tedy kontrastní barva k barvě ((rgb: 64,96,255), contrast=32) je barva: rgb(96,128,223) = (64+32, 96+32, 255-32).
        /// Složka A se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="contrast">Míra kontrastu</param>
        /// <returns></returns>
        public static Color Contrast(this Color root, int contrast)
        {
            float a = root.A;
            float r = GetContrast(root.R, contrast);
            float g = GetContrast(root.G, contrast);
            float b = GetContrast(root.B, contrast);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí barvu, která je kontrastní vůči barvě this.
        /// Kontrastní barva leží o dané množství barvy směrem k protilehlé barvě (vždy na opačnou stranu od hodnoty 128), v každé složce zvlášť.
        /// Například ke složce s hodnotou 160 je kontrastní barvou o 32 hodnota (160-32) = 128, k hodnotě 100 o 32 je kontrastní (100+32) = 132.
        /// Tedy kontrastní barva k barvě ((rgb: 64,96,255), contrast=32) je barva: rgb(96,128,223) = (64+32, 96+32, 255-32).
        /// Složka A se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="contrastR">Míra kontrastu ve složce R</param>
        /// <param name="contrastG">Míra kontrastu ve složce G</param>
        /// <param name="contrastB">Míra kontrastu ve složce B</param>
        /// <returns></returns>
        public static Color Contrast(this Color root, int contrastR, int contrastG, int contrastB)
        {
            float a = root.A;
            float r = GetContrast(root.R, contrastR);
            float g = GetContrast(root.G, contrastG);
            float b = GetContrast(root.B, contrastB);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací kontrastní složku
        /// </summary>
        /// <param name="root"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        private static float GetContrast(int root, int contrast)
        {
            return (root <= 128 ? root + contrast : root - contrast);
        }
        #endregion
        #region Color: GrayScale
        /// <summary>
        /// Vrátí danou barvu odbarvenou do černo-šedo-bílé stupnice.
        /// Hodnotu Alpha ponechává.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <returns></returns>
        public static Color GrayScale(this Color root)
        {
            // Vypočítám souhrnný jas všech složek se zohledněním koeficientu jejich barevného jasu:
            float rgb = 1.0f * (float)root.R +
                        1.4f * (float)root.G +
                        0.7f * (float)root.B;              // Součet složek je 0 až 790.5;
            int g = (int)(Math.Round((255f * (rgb / 790.5f)), 0));
            return Color.FromArgb(root.A, g, g, g);
        }
        /// <summary>
        /// Vrátí danou barvu odbarvenou do černo-šedo-bílé stupnice s daným poměrem odbarvení.
        /// Hodnotu Alpha ponechává.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="ratio">Poměr odbarvení</param>
        /// <returns></returns>
        public static Color GrayScale(this Color root, float ratio)
        {
            Color gray = root.GrayScale();
            return root.Morph(gray, ratio);
        }
        #endregion
        #region Color: Opacity
        /// <summary>
        /// Vrátí danou barvu this, do které aplikuje danou průhlednost (nad rámec vlastní průhlednosti barvy).
        /// </summary>
        /// <param name="root"></param>
        /// <param name="alpha">Alfa kanál: 0=zcela průhledná, 1=původní barva beze změny, null = beze změny</param>
        /// <returns></returns>
        public static Color GetAlpha(this Color root, float? alpha)
        {
            if (!alpha.HasValue || alpha.Value >= 1f) return root;             // Beze změny
            if (alpha.Value < 0f) alpha = 0f;
            int a = (int)(Math.Round((float)root.A * alpha.Value, 0));         // Smíchám kanál A s hodnotou alpha, obě hodnoty mají význam 0=průhledné sklo ... max (255 nebo 1.0f) = zcela plná barva
            return Color.FromArgb(a, root.R, root.G, root.B);
        }
        /// <summary>
        /// Do dané barvy (this) vloží danou hodnotu Alpha (parametr opacity), výsledek vrátí.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacity">Průhlednost v hodnotě 0-255 (nebo null = neměnit)</param>
        /// <returns></returns>
        public static Color SetOpacity(this Color root, Int32? opacity)
        {
            if (!opacity.HasValue) return root;
            int alpha = (opacity.Value < 0 ? 0 : (opacity.Value > 255 ? 255 : opacity.Value));
            return Color.FromArgb(alpha, root);
        }
        /// <summary>
        /// Do dané barvy (this) vloží danou hodnotu Alpha (parametr opacityRatio), výsledek vrátí.
        /// Hodnota opacityRatio : Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacityRatio">Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)</param>
        /// <returns></returns>
        public static Color SetOpacity(this Color root, float? opacityRatio)
        {
            if (!opacityRatio.HasValue) return root;
            return SetOpacity(root, (int)(255f * opacityRatio.Value));
        }
        /// <summary>
        /// Na danou barvu aplikuje všechny dodané hodnoty průhlednosti, při akceptování i původní průhlednosti.
        /// Aplikování se provádní vzájemným násobením hodnoty (opacity/255), což je poměr (ratio) průhlednosti.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacities"></param>
        /// <returns></returns>
        public static Color ApplyOpacity(this Color root, params Int32?[] opacities)
        {
            float alpha = _GetColorRatio(root.A);
            foreach (Int32? opacity in opacities)
            {
                if (opacity.HasValue)
                    alpha = alpha * _GetColorRatio(opacity.Value);
            }
            return SetOpacity(root, (int)(Math.Round(255f * alpha, 0)));
        }
        /// <summary>
        /// Vrací ratio { 0.00 až 1.00 } z hodnoty { 0 až 255 }.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float _GetColorRatio(int value)
        {
            if (value < 0) return 0f;
            if (value >= 255) return 1f;
            return (float)value / 255f;
        }
        /// <summary>
        /// Na danou barvu aplikuje všechny dodané hodnoty průhlednosti, při akceptování i původní průhlednosti.
        /// Aplikování se provádní vzájemným násobením hodnoty (ratio), což je poměr (ratio) průhlednosti.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="ratios"></param>
        /// <returns></returns>
        public static Color ApplyOpacity(this Color root, params float?[] ratios)
        {
            float alpha = _GetColorRatio(root.A);
            foreach (float? ratio in ratios)
            {
                if (ratio.HasValue)
                    alpha = alpha * _GetColorRatio(ratio.Value);
            }
            return SetOpacity(root, (int)(Math.Round(255f * alpha, 0)));
        }
        /// <summary>
        /// Zarovná dané ratio do rozmezí { 0.00 až 1.00 }.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static float _GetColorRatio(float ratio)
        {
            return (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));
        }
        /// <summary>
        /// Metoda vrátí novou instanci barvy this, kde její Alpha je nastavena na daný poměr (transparent) původní hodnoty.
        /// Tedy zadáním například: <see cref="Color.BlueViolet"/>.<see cref="CreateTransparent(Color, float)"/>(0.75f) 
        /// dojde k vytvoření a vrácení barvy s hodnotou Alpha = 75% = 192, od barvy BlueViolet (která je #FF8A2BE2), tedy výsledek bude #C08A2BE2.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static Color CreateTransparent(this Color root, float alpha)
        {
            int a = (int)(((float)root.A) * alpha);
            a = (a < 0 ? 0 : (a > 255 ? 255 : a));
            return Color.FromArgb(a, root.R, root.G, root.B);
        }
        #endregion
        #endregion
        #region Control
        /// <summary>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato metoda <see cref="IsVisibleInternal(Control)"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static bool IsVisibleInternal(this Control control)
        {
            if (control is null) return false;
            var getState = control.GetType().GetMethod("GetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic);
            if (getState is null) return false;
            object visible = getState.Invoke(control, new object[] { (int)0x02  /*STATE_VISIBLE*/  });
            return (visible is bool ? (bool)visible : false);
        }
        #endregion
        #region IEnumerable
        /// <summary>
        /// Pro každý prvek this kolekce provede danou akci. I pro null prvky.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="action"></param>
        public static void ForEachExec<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (items != null)
            {
                foreach (var item in items)
                    action(item);
            }
        }
        /// <summary>
        /// Vrátí true, pokud this kolekce je null anebo neobsahuje žádný prvek.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static bool IsEmpty<T>(this IEnumerable<T> items)
        {
            if (items is null) return true;
            return !(items.Any());
        }
        /// <summary>
        /// Metoda najde a vrátí první prvek dané kolekce, který vyhovuje danému filtru. Vrací true = nalezeno.
        /// Pokud kolekce je prázdná, vrací false.
        /// Pokud filtr je null a kolekce není prázdná, pak vrátí první prvek.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="filter"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        public static bool TryFindFirst<T>(this IEnumerable<T> items, Predicate<T> filter, out T found)
        {
            if (!items.IsEmpty())
            {
                if (filter is null) { found = items.First(); return true; }
                foreach (var item in items)
                {
                    if (filter(item))
                    {
                        found = item;
                        return true;
                    }
                }
            }
            found = default;
            return false;
        }
        /// <summary>
        /// Z this kolekce vytvoří Dictionary s klíčem vybraným z prvku pomocí dodaného <paramref name="keySelector"/>.
        /// Pokud bude <paramref name="ignoreDuplicity"/> = true, pak případné duplicitní prvky budou ignorovány.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="ignoreDuplicity"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector, bool ignoreDuplicity = false)
        {
            if (keySelector is null) throw new ArgumentNullException($"CreateDictionary() error: 'keySelector' can not be null.");
            if (items is null) return null;
            var result = new Dictionary<TKey, TValue>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (result.ContainsKey(key))
                {
                    if (!ignoreDuplicity)
                        throw new ArgumentException($"CreateDictionary() error: key '{key}' is duplicate, and 'ignoreDuplicity' is not enabled.");
                }
                else
                {
                    result.Add(key, item);
                }
            }
            return result;
        }
        /// <summary>
        /// Z this kolekce vytvoří Dictionary s klíčem vybraným z prvku pomocí dodaného <paramref name="keySelector"/> a hodnotou získanou pomocí <paramref name="valueSelector"/>.
        /// Pokud bude <paramref name="ignoreDuplicity"/> = true, pak případné duplicitní prvky budou ignorovány.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="valueSelector"></param>
        /// <param name="ignoreDuplicity"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue, TItems>(this IEnumerable<TItems> items, Func<TItems, TKey> keySelector, Func<TItems, TValue> valueSelector, bool ignoreDuplicity = false)
        {
            if (keySelector is null) throw new ArgumentNullException($"CreateDictionary() error: 'keySelector' can not be null.");
            if (valueSelector is null) throw new ArgumentNullException($"CreateDictionary() error: 'valueSelector' can not be null.");
            if (items is null) return null;
            var result = new Dictionary<TKey, TValue>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (result.ContainsKey(key))
                {
                    if (!ignoreDuplicity)
                        throw new ArgumentException($"CreateDictionary() error: key '{key}' is duplicate, and 'ignoreDuplicity' is not enabled.");
                }
                else
                {
                    result.Add(key, valueSelector(item));
                }
            }
            return result;
        }
        /// <summary>
        /// Z this kolekce vytvoří Dictionary s klíčem vybraným z prvku pomocí dodaného <paramref name="keySelector"/>.
        /// Value ve výstupní Dictionary je pole (Array) prvků, které jsou přítomny ve vstupní kolekci a mají shodný klíč. Tato varianta tedy nemusí řešit duplicitu.
        /// Pole má vždy nejméně jeden prvek.
        /// <para/>
        /// Metodu tedy lze použít jako alternativu k Grupování kolekce.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue[]> CreateDictionaryArray<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException($"CreateDictionaryArray() error: 'keySelector' can not be null.");
            if (items is null) return null;

            // Pracovní Dictionary, má jako Value prvek typu List (nikoliv Array):
            var dictionary = new Dictionary<TKey, List<TValue>>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (!dictionary.TryGetValue(key, out var list))
                {
                    list = new List<TValue>();
                    dictionary.Add(key, list);
                }
                list.Add(item);
            }

            // Výstupní Dictionary, kde Value převedu z List na Array:
            var result = dictionary.CreateDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
            return result;
        }
        /// <summary>
        /// Z this kolekce vytvoří Dictionary s klíčem vybraným z prvku pomocí dodaného <paramref name="keySelector"/> a hodnotou získanou pomocí <paramref name="valueSelector"/>.
        /// Value ve výstupní Dictionary je pole (Array) prvků, které jsou přítomny ve vstupní kolekci a mají shodný klíč. Tato varianta tedy nemusí řešit duplicitu.
        /// Pole má vždy nejméně jeden prvek.
        /// <para/>
        /// Metodu tedy lze použít jako alternativu k Grupování kolekce.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="items"></param>
        /// <param name="keySelector"></param>
        /// <param name="ignoreDuplicity"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue[]> CreateDictionaryArray<TKey, TValue, TItems>(this IEnumerable<TItems> items, Func<TItems, TKey> keySelector, Func<TItems, TValue> valueSelector)
        {
            if (keySelector is null) throw new ArgumentNullException($"CreateDictionaryArray() error: 'keySelector' can not be null.");
            if (valueSelector is null) throw new ArgumentNullException($"CreateDictionaryArray() error: 'valueSelector' can not be null.");
            if (items is null) return null;

            // Pracovní Dictionary, má jako Value prvek typu List (nikoliv Array):
            var dictionary = new Dictionary<TKey, List<TValue>>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (!dictionary.TryGetValue(key, out var list))
                {
                    list = new List<TValue>();
                    dictionary.Add(key, list);
                }
                list.Add(valueSelector(item));
            }

            // Výstupní Dictionary, kde Value převedu z List na Array:
            var result = dictionary.CreateDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
            return result;
        }
        /// <summary>
        /// Přidá nebo přepíše danou hodnotu do this Dictionary pod daný klíč.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void StoreValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
        /// <summary>
        /// Přidá nebo přepíše danou hodnotu do this Dictionary pod klíč, který z hodnoty určí daný <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="value"></param>
        /// <param name="keySelector"></param>
        public static void StoreValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value, Func<TValue, TKey> keySelector)
        {
            var key = keySelector(value);
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
        /// <summary>
        /// Metoda zajistí smazání prvků vyhovujících dodanému filtru <paramref name="predicate"/>, a tyto odstraňované prvky odešle do dané akce <paramref name="actionOnRemoved"/>.
        /// Akce může zajistit např. uvolnění vztahů odebíraného prvku na další objekty.
        /// <para/>
        /// Pokud <paramref name="list"/> je null, nedělá se nic.
        /// Pokud <paramref name="predicate"/> je null, pak se smažou všechny prvky.
        /// Pokud <paramref name="actionOnRemoved"/> je null, smažou se patřičné prvky a nic se nevolá.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        /// <param name="actionOnRemoved"></param>
        public static void RemoveAll<T>(this List<T> list, Predicate<T> predicate, Action<T> actionOnRemoved)
        {
            if (list is null || list.Count == 0) return;
            bool hasPredicate = (predicate != null);
            bool hasActionOnRemoved = (actionOnRemoved != null);

            if (!hasPredicate)
            {   // Bez filtru jde prostě o Clear s provedením požadované akce pro každý prvek:
                if (hasActionOnRemoved)
                {
                    foreach (var item in list)
                        actionOnRemoved(item);
                }
                list.Clear();
            }
            else
            {   // Budeme filtrovat:
                list.RemoveAll(item =>
                {   // Filtr 'predicate' neposílám přímo, ale provedu i něco navíc:
                    bool toRemove = predicate(item);
                    if (toRemove) actionOnRemoved(item);   // Pokud se prvek bude odebírat, tak zavolám akci...
                    return toRemove;
                });
            }
        }
        #endregion
        #region Drobnosti
        /// <summary>
        /// Vrátí true, pokud this hodnota je rovna některé ze zadaných
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="testValue"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool IsAnyFrom<T>(this T testValue, params T[] values) where T : IComparable
        {
            return values.Any(v => testValue.CompareTo(v) == 0);
        }
        /// <summary>
        /// Pokud je dodán objekt, provede na něm <see cref="IDisposable.Dispose()"/>. Chyby neřeší, topí je.
        /// </summary>
        /// <param name="disposable"></param>
        public static void TryDispose(this IDisposable disposable)
        {
            if (disposable != null)
            {
                try { disposable.Dispose(); }
                catch { }
            }
        }
        #endregion
    }

    #region Interface IMenuItem, jeho implementace
    /// <summary>
    /// Základní implementace interface <see cref="IMenuItem"/>
    /// </summary>
    public class DataMenuItem : IMenuItem
    {
        public  DataMenuItem()
        {
            ItemType = MenuItemType.Button;
            Enabled = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ItemType}: {Text}";
        }
        /// <summary>
        /// Vytvoří a vrátí prvek typu Separátor
        /// </summary>
        /// <returns></returns>
        public static DataMenuItem CreateSeparator() { return new DataMenuItem() { ItemType = MenuItemType.Separator }; }
        /// <summary>
        /// Vytvoří a vrátí prvek typu Separátor
        /// </summary>
        /// <returns></returns>
        public static DataMenuItem CreateHeader(string text) { return new DataMenuItem() { ItemType = MenuItemType.Header, Text = text}; }

        public virtual string Text { get; set; }
        public virtual string ToolTip { get; set; }
        public virtual MenuItemType ItemType { get; set; }
        public virtual Image Image { get; set; }
        public virtual bool Enabled { get; set; }
        public virtual FontStyle? FontStyle { get; set; }
        public virtual object ToolItem { get; set; }
        public virtual object UserData { get; set; }
    }
    /// <summary>
    /// Předpis pro prvky, které mohou být nabízeny v menu
    /// </summary>
    public interface IMenuItem
    {
        string Text { get; }
        string ToolTip { get; }
        MenuItemType ItemType { get; }
        Image Image { get; }
        bool Enabled { get; }
        FontStyle? FontStyle { get; }
        /// <summary>
        /// Vizuální objekt menu (typicky <see cref="ToolStripItem"/>), uložený sem po jeho vytvoření.
        /// Aplikace se o tuto hodnotu nemá starat.
        /// Slouží k provedení Refreshe v metodě <see cref="App.RefreshMenuItem(IMenuItem)"/>
        /// </summary>
        object ToolItem { get; set; }
        /// <summary>
        /// Libovolná data uživatele, systém se o ně nestará. Aplikace si zde ukládá význam prvku - aby mohla správně reagovat na kliknutí.
        /// </summary>
        object UserData { get; set; }
    }
    /// <summary>
    /// Typ prvku v menu
    /// </summary>
    public enum MenuItemType
    {
        Default = 0,
        Header,
        Button,
        Separator
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Pozice bodu v Rectangle
    /// </summary>
    public enum RectanglePointPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    #endregion
}
