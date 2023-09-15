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

namespace DjSoft.Tools.ProgramLauncher
{
    public static class Extensions
    {
        #region Kreslící support
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
        /// Vrátí new instanci <see cref="Rectangle"/>, která vychází z this a je posunutá o danou pozici
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Rectangle GetShiftedRectangle(this Rectangle bounds, Point offset)
        {
            return new Rectangle(bounds.X + offset.X, bounds.Y + offset.Y, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která vychází z this a je posunutá o danou pozici
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <returns></returns>
        public static Rectangle GetShiftedRectangle(this Rectangle bounds, int offsetX, int offsetY)
        {
            return new Rectangle(bounds.X + offsetX, bounds.Y + offsetY, bounds.Width, bounds.Height);
        }

        /// <summary>
        /// Vrátí new instanci <see cref="Rectangle"/>, která má daný střed a velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle GetRectangleFromCenter(this Point center, Size size)
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
        public static Rectangle GetRectangleFromCenter(this Point center, int width, int height)
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
        #endregion
        #region Graphics - FountainFill, Draw
        /// <summary>
        /// Do this <paramref name="Graphics"/> vyplní dané souřadnice <paramref name="bounds"/> danou barvou <paramref name="color"/> v daném interakticním stavu <paramref name="interactiveState"/>.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="interactiveState"></param>
        public static void FountainFill(this Graphics graphics, Rectangle bounds, Color color, InteractiveState interactiveState = InteractiveState.None)
        {
            _GetFountainFillDirection(interactiveState, out float morph, out FountainDirection direction);
            FountainFill(graphics, bounds, color, morph, direction);
        }
        public static void FountainFill(this Graphics graphics, GraphicsPath path, Color color, InteractiveState interactiveState = InteractiveState.None)
        {
            _GetFountainFillDirection(interactiveState, out float morph, out FountainDirection direction);
            FountainFill(graphics, path, color, morph, direction);
        }
        public static void FountainFill(this Graphics graphics, Rectangle bounds, Color color, float morph, FountainDirection direction)
        {
            _GetFountainFillColors(color, morph, out Color colorBegin, out Color colorEnd);
            using (var brush = CreateLinearGradientBrush(bounds, colorBegin, colorEnd, direction))
                graphics.FillRectangle(brush, bounds);
        }
        public static void FountainFill(this Graphics graphics, GraphicsPath path, Color color, float morph, FountainDirection direction)
        {
            Rectangle bounds = path.GetBounds().GetOuterBounds();
            _GetFountainFillColors(color, morph, out Color colorBegin, out Color colorEnd);
            using (var brush = CreateLinearGradientBrush(bounds, colorBegin, colorEnd, direction))
                graphics.FillPath(brush, path);
        }
        /// <summary>
        /// Pro zadaný interaktivní stav <paramref name="interactiveState"/> určí hodnoty <paramref name="morph"/> a <paramref name="direction"/>
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <param name="morph"></param>
        /// <param name="direction"></param>
        private static void _GetFountainFillDirection(InteractiveState interactiveState, out float morph, out FountainDirection direction)
        {
            switch (interactiveState)
            {
                case InteractiveState.MouseOn:
                    morph = 0.10f;
                    direction = FountainDirection.ToDown;
                    return;
                case InteractiveState.MouseDown:
                    morph = -0.20f;
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
        private static void _GetFountainFillColors(Color color, float morph, out Color colorBegin, out Color colorEnd)
        {
            colorBegin = color.ChangeColor(morph);
            colorEnd = color.ChangeColor(-morph);
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
            None,
            ToDown,
            ToUp,
            ToRight,
            ToLeft
        }
        
        public static void DrawText(this Graphics graphics, string text, RectangleF bounds, TextAppearance textAppearance, InteractiveState interactiveState = InteractiveState.None)
        {
            var font = App.GetFont(textAppearance);
            var brush = App.GetBrush(textAppearance.TextColors, interactiveState);
            graphics.SetForText();
            graphics.DrawString(text, font, brush, bounds);
        }
        public static void DrawText(this Graphics graphics, string text, RectangleF bounds, Color color, FontType? fontType = null, float? emSize = null, FontStyle? fontStyle = null)
        {
            var font = App.GetFont(fontType, emSize, fontStyle);
            var brush = App.GetBrush(color);
            graphics.SetForText();
            graphics.DrawString(text, font, brush, bounds);
        }
        public static void SetForText(this Graphics graphics)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
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
        public static Color Morph(this Color root, Color other, float morph)
        {
            if (morph == 0f) return root;
            float a = root.A;
            float r = GetMorph(root.R, other.R, morph);
            float g = GetMorph(root.G, other.G, morph);
            float b = GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
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
        public static Color Morph(this Color root, Color other)
        {
            if (other.A == 0) return root;
            float morph = ((float)other.A) / 255f;
            float a = root.A;
            float r = GetMorph(root.R, other.R, morph);
            float g = GetMorph(root.G, other.G, morph);
            float b = GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí složku barvy vzniklou morphingem = interpolací.
        /// </summary>
        /// <param name="root">Výchozí složka</param>
        /// <param name="other">Cílová složka</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        private static float GetMorph(float root, float other, float morph)
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
        #region IEnumerable
        /// <summary>
        /// Pro každýá prvek this kolekce provede danou akci. I pro null prvky.
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
        #endregion
    }
}
