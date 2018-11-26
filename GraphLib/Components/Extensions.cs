using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Extensions metody pro grafické třídy (z namespace System.Drawing)
    /// </summary>
    public static class DrawingExtensions
    {
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
            a = (a < 0 ? 0 : (a > 255 ? 255 : 0));
            return Color.FromArgb(a, root.R, root.G, root.B);
        }
        #endregion
        #region Point, PointF: Add/Sub
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static Point Add(this Point basePoint, Point addPoint)
        {
            return new Point(basePoint.X + addPoint.X, basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint + (addpoint X, Y)
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addX"></param>
        /// <param name="addY"></param>
        /// <returns></returns>
        public static Point Add(this Point basePoint, int addX, int addY)
        {
            return new Point(basePoint.X + addX, basePoint.Y + addY);
        }
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static PointF Add(this PointF basePoint, PointF addPoint)
        {
            return new PointF(basePoint.X + addPoint.X, basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static PointF Add(this Point basePoint, PointF addPoint)
        {
            return new PointF((float)basePoint.X + addPoint.X, (float)basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static Point Sub(this Point basePoint, Point subPoint)
        {
            return new Point(basePoint.X - subPoint.X, basePoint.Y - subPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - (subpoint X, Y)
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subX"></param>
        /// <param name="subY"></param>
        /// <returns></returns>
        public static Point Sub(this Point basePoint, int subX, int subY)
        {
            return new Point(basePoint.X - subX, basePoint.Y - subY);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static PointF Sub(this PointF basePoint, PointF subPoint)
        {
            return new PointF(basePoint.X - subPoint.X, basePoint.Y - subPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static PointF Sub(this Point basePoint, PointF subPoint)
        {
            return new PointF((float)basePoint.X - subPoint.X, (float)basePoint.Y - subPoint.Y);
        }
        #endregion
        #region Size, SizeF, Rectangle, RectangleF: zooming
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlarge">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, float enlarge)
        { return new SizeF(size.Width + 2f * enlarge, size.Height + 2f * enlarge); }
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlargeWidth">Coefficient X.</param>
        /// <param name="enlargeHeight">Coefficient Y.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, float enlargeWidth, float enlargeHeight)
        { return new SizeF(size.Width + 2f * enlargeWidth, size.Height + 2f * enlargeHeight); }
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlarge">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, SizeF enlarge)
        { return new SizeF(size.Width + 2f * enlarge.Width, size.Height + 2f * enlarge.Height); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduce">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, float reduce)
        { return new SizeF(size.Width - 2f * reduce, size.Height - 2f * reduce); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduceWidth">Coefficient X.</param>
        /// <param name="reduceHeight">Coefficient Y.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, float reduceWidth, float reduceHeight)
        { return new SizeF(size.Width - 2f * reduceWidth, size.Height - 2f * reduceHeight); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduce">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, SizeF reduce)
        { return new SizeF(size.Width - 2f * reduce.Width, size.Height - 2f * reduce.Height); }

        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, decimal zoom)
        { return (new SizeF(size.Width * (float)zoom, size.Height * (float)zoom)); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, float zoom)
        { return new SizeF(size.Width * zoom, size.Height * zoom); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, double zoom)
        { return new SizeF(size.Width * (float)zoom, size.Height * (float)zoom); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, decimal ratio)
        { return new SizeF(size.Width / (float)ratio, size.Height / (float)ratio); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, float ratio)
        { return new SizeF(size.Width / ratio, size.Height / ratio); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, double ratio)
        { return new SizeF(size.Width / (float)ratio, size.Height / (float)ratio); }

        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, decimal zoomX, decimal zoomY)
        { return (new SizeF(size.Width * (float)zoomX, size.Height * (float)zoomX)); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, float zoomX, float zoomY)
        { return new SizeF(size.Width * zoomX, size.Height * zoomY); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, double zoomX, double zoomY)
        { return new SizeF(size.Width * (float)zoomX, size.Height * (float)zoomY); }

        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF ShrinkTo(this SizeF size, SizeF shrinkTo)
        { return new SizeF(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height); }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti a současně zachovala svůj původní poměr stran.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <param name="preserveRatio">Zachovat poměr stran</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF ShrinkTo(this SizeF size, SizeF shrinkTo, bool preserveRatio)
        {
            if (size.Width <= shrinkTo.Width && size.Height <= shrinkTo.Height)
                return size;

            if (size.Width == 0 || size.Height == 0)
                preserveRatio = false;

            if (!preserveRatio)
                return new SizeF(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height);

            if (shrinkTo.Width <= 0 || shrinkTo.Height <= 0)
                return SizeF.Empty;

            decimal shrinkX = (decimal)shrinkTo.Width / (decimal)size.Width;
            decimal shrinkY = (decimal)shrinkTo.Height / (decimal)size.Height;
            decimal shrink = (shrinkX < shrinkY ? shrinkX : shrinkY);

            return new SizeF((float)((decimal)size.Width * shrink), (float)((decimal)size.Height * shrink));
        }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static Size ShrinkTo(this Size size, Size shrinkTo)
        { return new Size(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height); }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti a současně zachovala svůj původní poměr stran.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <param name="preserveRatio">Zachovat poměr stran</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static Size ShrinkTo(this Size size, Size shrinkTo, bool preserveRatio)
        {
            if (size.Width <= shrinkTo.Width && size.Height <= shrinkTo.Height)
                return size;

            if (size.Width == 0 || size.Height == 0)
                preserveRatio = false;

            if (!preserveRatio)
                return new Size(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height);

            if (shrinkTo.Width <= 0 || shrinkTo.Height <= 0)
                return Size.Empty;

            decimal shrinkX = (decimal)shrinkTo.Width / (decimal)size.Width;
            decimal shrinkY = (decimal)shrinkTo.Height / (decimal)size.Height;
            decimal shrink = (shrinkX < shrinkY ? shrinkX : shrinkY);

            return new Size((int)((decimal)size.Width * shrink), (int)((decimal)size.Height * shrink));
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
            r.X = r.X - left;
            r.Y = r.Y - top;
            r.Width = r.Width + left + right;
            r.Height = r.Height + top + bottom;
            return r;
        }
        /// <summary>
        /// Create a new RectangleF, which is current rectangle enlarged by size specified for each side.
        /// For example: this Rectangle {50, 10, 30, 20} .Enlarge(1, 1, 1, 1) will be after: {49, 9, 32, 22}.
        /// For example: this Rectangle {50, 10, 30, 20} .Enlarge(0, 0, -1, -1) will be after: {50, 10, 29, 19}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static RectangleF Enlarge(this RectangleF r, float left, float top, float right, float bottom)
        {
            r.X = r.X - left;
            r.Y = r.Y - top;
            r.Width = r.Width + left + right;
            r.Height = r.Height + top + bottom;
            return r;
        }

        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Size Min(this Size one, Size other)
        {
            return new Size((one.Width < other.Width ? one.Width : other.Width),
                             (one.Height < other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Size Max(this Size one, Size other)
        {
            return new Size((one.Width > other.Width ? one.Width : other.Width),
                             (one.Height > other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou zarovnány do mezí Min - Max.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Size MinMax(this Size one, Size min, Size max)
        {
            return new Size((one.Width < min.Width ? min.Width : (one.Width > max.Width ? max.Width : one.Width)),
                             (one.Height < min.Height ? min.Height : (one.Height > max.Height ? max.Height : one.Height)));
        }

        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SizeF Min(this SizeF one, SizeF other)
        {
            return new SizeF((one.Width < other.Width ? one.Width : other.Width),
                             (one.Height < other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SizeF Max(this SizeF one, SizeF other)
        {
            return new SizeF((one.Width > other.Width ? one.Width : other.Width),
                             (one.Height > other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou zarovnány do mezí Min - Max.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static SizeF MinMax(this SizeF one, SizeF min, SizeF max)
        {
            return new SizeF((one.Width < min.Width ? min.Width : (one.Width > max.Width ? max.Width : one.Width)),
                             (one.Height < min.Height ? min.Height : (one.Height > max.Height ? max.Height : one.Height)));
        }
        #endregion
        #region Size: AlignTo
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment, bool cropSize)
        {
            SizeF realSize = sizeF;
            if (cropSize)
                realSize = sizeF.ShrinkTo(bounds.Size, false);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <param name="preserveRatio"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment, bool cropSize, bool preserveRatio)
        {
            SizeF realSize = sizeF;
            if (cropSize)
                realSize = sizeF.ShrinkTo(bounds.Size, preserveRatio);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment, bool cropSize)
        {
            Size realSize = size;
            if (cropSize)
                realSize = realSize.ShrinkTo(bounds.Size, false);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <param name="preserveRatio"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment, bool cropSize, bool preserveRatio)
        {
            Size realSize = size;
            if (cropSize)
                realSize = realSize.ShrinkTo(bounds.Size, preserveRatio);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment)
        {
            Size size = Size.Ceiling(sizeF);
            return size.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment)
        {
            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width - size.Width;
            int h = bounds.Height - size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += w / 2;
                    break;
                case ContentAlignment.TopRight:
                    x += w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += h / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += w / 2;
                    y += h / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x += w;
                    y += h / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y += h;
                    break;
                case ContentAlignment.BottomCenter:
                    x += w / 2;
                    y += h;
                    break;
                case ContentAlignment.BottomRight:
                    x += w;
                    y += h;
                    break;
            }
            return new Rectangle(new Point(x, y), size);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Point pivot, ContentAlignment alignment, Size addSize)
        {
            Rectangle bounds = AlignTo(sizeF, pivot, alignment);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Point pivot, ContentAlignment alignment, Size addSize)
        {
            Rectangle bounds = AlignTo(size, pivot, alignment);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Point pivot, ContentAlignment alignment)
        {
            Size size = Size.Ceiling(sizeF);
            return size.AlignTo(pivot, alignment);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Point pivot, ContentAlignment alignment)
        {
            int x = pivot.X;
            int y = pivot.Y;
            int w = size.Width;
            int h = size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x -= w / 2;
                    break;
                case ContentAlignment.TopRight:
                    x -= w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y -= h / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= w / 2;
                    y -= h / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x -= w;
                    y -= h / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y -= h;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= w / 2;
                    y -= h;
                    break;
                case ContentAlignment.BottomRight:
                    x -= w;
                    y -= h;
                    break;
            }
            return new Rectangle(new Point(x, y), size);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Width
        /// </summary>
        /// <param name="size"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Size ZoomToWidth(this Size size, int width)
        {
            if (size.Width <= 0) return size;
            double ratio = (double)width / (double)size.Width;
            return ZoomByRatio(size, ratio);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Size ZoomToHeight(this Size size, int height)
        {
            if (size.Height <= 0) return size;
            double ratio = (double)height / (double)size.Height;
            return ZoomByRatio(size, ratio);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static Size ZoomByRatio(this Size size, double ratio)
        {
            int width = (int)(Math.Round(ratio * (double)size.Width, 0));
            int height = (int)(Math.Round(ratio * (double)size.Height, 0));
            return new Size(width, height);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratioWidth"></param>
        /// <param name="ratioHeight"></param>
        /// <returns></returns>
        public static Size ZoomByRatio(this Size size, double ratioWidth, double ratioHeight)
        {
            int width = (int)(Math.Round(ratioWidth * (double)size.Width, 0));
            int height = (int)(Math.Round(ratioHeight * (double)size.Height, 0));
            return new Size(width, height);
        }
        #endregion
        #region SizeF: AlignTo
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment, bool cropSize)
        {
            SizeF realSize = size;
            if (cropSize)
            {
                if (realSize.Width > bounds.Width)
                    realSize.Width = bounds.Width;
                if (realSize.Height > bounds.Height)
                    realSize.Height = bounds.Height;
            }
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment)
        {
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width - size.Width;
            float h = bounds.Height - size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += w / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x += w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += h / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += w / 2f;
                    y += h / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x += w;
                    y += h / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y += h;
                    break;
                case ContentAlignment.BottomCenter:
                    x += w / 2f;
                    y += h;
                    break;
                case ContentAlignment.BottomRight:
                    x += w;
                    y += h;
                    break;
            }
            return new RectangleF(new PointF(x, y), size);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, PointF pivot, ContentAlignment alignment, SizeF addSize)
        {
            RectangleF bounds = AlignTo(size, pivot, alignment);
            return new RectangleF(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, PointF pivot, ContentAlignment alignment)
        {
            float x = pivot.X;
            float y = pivot.Y;
            float w = size.Width;
            float h = size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x -= w / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x -= w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y -= h / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= w / 2f;
                    y -= h / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x -= w;
                    y -= h / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y -= h;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= w / 2f;
                    y -= h;
                    break;
                case ContentAlignment.BottomRight:
                    x -= w;
                    y -= h;
                    break;
            }
            return new RectangleF(new PointF(x, y), size);
        }
        #endregion
        #region Rectangle: FromPoints, FromDim, FromCenter, End, GetVisualRange, GetSide, GetPoint
        /// <summary>
        /// Vrátí Rectangle, který je natažený mezi dvěma body, přičemž vzájemná pozice oněch dvou bodů může být libovolná.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static Rectangle FromPoints(this Point point1, Point point2)
        {
            int l = (point1.X < point2.X ? point1.X : point2.X);
            int t = (point1.Y < point2.Y ? point1.Y : point2.Y);
            int r = (point1.X > point2.X ? point1.X : point2.X);
            int b = (point1.Y > point2.Y ? point1.Y : point2.Y);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static Rectangle FromDim(int x1, int x2, int y1, int y2)
        {
            int l = (x1 < x2 ? x1 : x2);
            int t = (y1 < y2 ? y1 : y2);
            int r = (x1 > x2 ? x1 : x2);
            int b = (y1 > y2 ? y1 : y2);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// Pokud je vzdálenost mezi x1÷x2 nebo y1÷y2 menší než minDist, pak zachová vzdálenost minDist,
        /// a to tak, že v odpovídajícím směru upraví souřadnici x2 nebo y2. 
        /// Jako x2/y2 by tedy měla být zadána ta "pohyblivější".
        /// </summary>
        /// <returns></returns>
        public static Rectangle FromDim(int x1, int x2, int y1, int y2, int minDist)
        {
            // Úprava souřadnic minDist (kladné číslo) a x2,y2:
            if (minDist < 0) minDist = -minDist;
            if (x2 >= x1 && x2 - x1 < minDist)
                x2 = x1 + minDist;
            else if (x2 < x1 && x1 - x2 < minDist)
                x2 = x1 - minDist;
            if (y2 >= y1 && y2 - y1 < minDist)
                y2 = y1 + minDist;
            else if (y2 < y1 && y1 - y2 < minDist)
                y2 = y1 - minDist;

            int l = (x1 < x2 ? x1 : x2);
            int t = (y1 < y2 ? y1 : y2);
            int r = (x1 > x2 ? x1 : x2);
            int b = (y1 > y2 ? y1 : y2);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, Size size)
        {
            Point location = new Point((point.X - size.Width / 2), (point.Y - size.Height / 2));
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, int width, int height)
        {
            Point location = new Point((point.X - width / 2), (point.Y - height / 2));
            return new Rectangle(location, new Size(width, height));
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, int size)
        {
            Point location = new Point((point.X - size / 2), (point.Y - size / 2));
            return new Rectangle(location, new Size(size, size));
        }
        /// <summary>
        /// Vrátí Rectangle ve velikosti (Size) this, postavený okolo daného středu
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Size size, Point point)
        {
            Point location = new Point((point.X - size.Width / 2), (point.Y - size.Height / 2));
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Vrátí bod uprostřed this Rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point Center(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
        }
        /// <summary>
        /// Vrátí bod na konci this Rectangle (opak Location) : (X + Width - 1, Y + Height - 1)
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point End(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width - 1, rectangle.Y + rectangle.Height - 1);
        }
        /// <summary>
        /// Vrátí souřadnici z this rectangle dle požadované strany.
        /// Pokud je zadána hodnota Top, Right, Bottom nebo Left, pak vrací příslušnou souřadnici.
        /// Pokud je zadána hodnota None nebo nějaký součet stran, pak vrací null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static Int32? GetSide(this Rectangle rectangle, RectangleSide edge)
        {
            switch (edge)
            {
                case RectangleSide.Top:
                    return rectangle.Top;
                case RectangleSide.Right:
                    return rectangle.Right;
                case RectangleSide.Bottom:
                    return rectangle.Bottom;
                case RectangleSide.Left:
                    return rectangle.Left;
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí určitý bod v daném Rectangle.
        /// Při nesprávném zadání strany může vrátit null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Point? GetPoint(this Rectangle rectangle, ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft: return GetPoint(rectangle, RectangleSide.TopLeft);
                case ContentAlignment.TopCenter: return GetPoint(rectangle, RectangleSide.TopCenter);
                case ContentAlignment.TopRight: return GetPoint(rectangle, RectangleSide.TopRight);

                case ContentAlignment.MiddleLeft: return GetPoint(rectangle, RectangleSide.MiddleLeft);
                case ContentAlignment.MiddleCenter: return GetPoint(rectangle, RectangleSide.MiddleCenter);
                case ContentAlignment.MiddleRight: return GetPoint(rectangle, RectangleSide.MiddleRight);

                case ContentAlignment.BottomLeft: return GetPoint(rectangle, RectangleSide.BottomLeft);
                case ContentAlignment.BottomCenter: return GetPoint(rectangle, RectangleSide.BottomCenter);
                case ContentAlignment.BottomRight: return GetPoint(rectangle, RectangleSide.BottomRight);
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí určitý bod v daném Rectangle.
        /// Při nesprávném zadání strany může vrátit null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Point? GetPoint(this Rectangle rectangle, RectangleSide position)
        {
            int? x = (position.HasFlag(RectangleSide.Left) ? rectangle.X :
                     (position.HasFlag(RectangleSide.Right) ? rectangle.Right :
                     (position.HasFlag(RectangleSide.CenterX) ? (rectangle.X + rectangle.Width / 2) : (int?)null)));
            int? y = (position.HasFlag(RectangleSide.Top) ? rectangle.Y :
                     (position.HasFlag(RectangleSide.Bottom) ? rectangle.Bottom :
                     (position.HasFlag(RectangleSide.CenterY) ? (rectangle.Y + rectangle.Height / 2) : (int?)null)));
            if (!(x.HasValue && y.HasValue)) return null;
            return new Point(x.Value, y.Value);
        }
        /// <summary>
        /// Vrátí rozsah { Begin, End } z this rectangle na požadované ose (orientaci).
        /// Pokud je zadána hodnota axis = <see cref="Orientation.Horizontal"/>, pak je vrácen <see cref="Int32Range"/> s hodnotami X, Width, Right.
        /// Pokud je zadána hodnota axis = <see cref="Orientation.Vertical"/>, pak je vrácen <see cref="Int32Range"/> s hodnotami Y, Height, Bottom.
        /// Jinak se vrací null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static Int32Range GetVisualRange(this Rectangle rectangle, Orientation axis)
        {
            switch (axis)
            {
                case Orientation.Horizontal: return new Int32Range(rectangle.X, rectangle.Right);
                case Orientation.Vertical: return new Int32Range(rectangle.Y, rectangle.Bottom);
            }
            return null;
        }
        #endregion
        #region RectangleF: FromPoints, FromDim, FromCenter
        /// <summary>
        /// Vrátí RectangleF, který je natažený mezi dvěma body, přičemž vzájemná pozice oněch dvou bodů může být libovolná.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static RectangleF FromPoints(this PointF point1, PointF point2)
        {
            float l = (point1.X < point2.X ? point1.X : point2.X);
            float t = (point1.Y < point2.Y ? point1.Y : point2.Y);
            float r = (point1.X > point2.X ? point1.X : point2.X);
            float b = (point1.Y > point2.Y ? point1.Y : point2.Y);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static RectangleF FromDim(float x1, float x2, float y1, float y2)
        {
            float l = (x1 < x2 ? x1 : x2);
            float t = (y1 < y2 ? y1 : y2);
            float r = (x1 > x2 ? x1 : x2);
            float b = (y1 > y2 ? y1 : y2);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// Pokud je vzdálenost mezi x1÷x2 nebo y1÷y2 menší než minDist, pak zachová vzdálenost minDist,
        /// a to tak, že v odpovídajícím směru upraví souřadnici x2 nebo y2. 
        /// Jako x2/y2 by tedy měla být zadána ta "pohyblivější".
        /// </summary>
        /// <returns></returns>
        public static RectangleF FromDim(float x1, float x2, float y1, float y2, float minDist)
        {
            // Úprava souřadnic minDist (kladné číslo) a x2,y2:
            if (minDist < 0f) minDist = -minDist;
            if (x2 >= x1 && x2 - x1 < minDist)
                x2 = x1 + minDist;
            else if (x2 < x1 && x1 - x2 < minDist)
                x2 = x1 - minDist;
            if (y2 >= y1 && y2 - y1 < minDist)
                y2 = y1 + minDist;
            else if (y2 < y1 && y1 - y2 < minDist)
                y2 = y1 - minDist;

            float l = (x1 < x2 ? x1 : x2);
            float t = (y1 < y2 ? y1 : y2);
            float r = (x1 > x2 ? x1 : x2);
            float b = (y1 > y2 ? y1 : y2);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, SizeF size)
        {
            PointF location = new PointF((point.X - size.Width / 2f), (point.Y - size.Height / 2f));
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, float width, float height)
        {
            PointF location = new PointF((point.X - width / 2f), (point.Y - height / 2f));
            return new RectangleF(location, new SizeF(width, height));
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, float size)
        {
            PointF location = new PointF((point.X - size / 2f), (point.Y - size / 2f));
            return new RectangleF(location, new SizeF(size, size));
        }
        /// <summary>
        /// Vrátí RectangleF ve velikosti (Size) this, postavený okolo daného středu
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this SizeF size, PointF point)
        {
            PointF location = new PointF((point.X - size.Width / 2f), (point.Y - size.Height / 2f));
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrátí bod uprostřed this RectangleF
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <returns></returns>
        public static PointF Center(this RectangleF rectangleF)
        {
            return new PointF(rectangleF.X + rectangleF.Width / 2f, rectangleF.Y + rectangleF.Height / 2f);
        }
        /// <summary>
        /// Vrátí bod na konci this RectangleF (opak Location)
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <returns></returns>
        public static PointF End(this RectangleF rectangleF)
        {
            return new PointF(rectangleF.X + rectangleF.Width - 1f, rectangleF.Y + rectangleF.Height - 1f);
        }
        #endregion
        #region RectangleF: RelativePoint, AbsolutePoint
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu (absolutePoint) vzhledem k this (RectangleF).
        /// Relativní pozice je v rozmezí 0 (na souřadnici Left nebo Top) až 1 (na souřadnici Right nebo Bottom).
        /// Relativní pozice může být menší než 0 (vlevo nebo nad this), nebo větší než 1 (vpravo nebo pod this).
        /// Tedy hodnoty 0 a 1 jsou na hraně this, hodnoty mezi 0 a 1 jsou uvnitř this, a hodnoty mimo jsou mimo this.
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public static PointF GetPointFRelative(this RectangleF rectangleF, PointF absolutePoint)
        {
            return new PointF(
                (float)_GetRelative(rectangleF.X, rectangleF.Right, absolutePoint.X),
                (float)_GetRelative(rectangleF.Y, rectangleF.Bottom, absolutePoint.Y));
        }
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu 
        /// vzhledem k bodům begin (relativní pozice = 0) a end (relativní pozice = 1).
        /// Pokud mezi body begin a end je vzdálenost 0, pak vrací 0.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="absolute"></param>
        /// <returns></returns>
        private static decimal _GetRelative(float begin, float end, float absolute)
        {
            decimal offset = (decimal)(absolute - begin);
            decimal size = (decimal)(end - begin);
            if (size == 0m) return 0m;
            return offset / size;
        }
        /// <summary>
        /// Vrátí souřadnice bodu, který v this rectangle odpovídá dané relativní souřadnici.
        /// Relativní souřadnice vyjadřuje pozici bodu: hodnota 0=na pozici Left nebo Top, hodnota 1=na pozici Right nebo Bottom.
        /// Vrácený bod je vyjádřen v reálných (absolutních) hodnotách odpovídajících rectanglu this.
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        public static PointF GetPointFAbsolute(this RectangleF rectangleF, PointF relativePoint)
        {
            return new PointF(
                (float)_GetAbsolute(rectangleF.X, rectangleF.Right, (decimal)relativePoint.X),
                (float)_GetAbsolute(rectangleF.Y, rectangleF.Bottom, (decimal)relativePoint.Y));
        }
        /// <summary>
        /// Vrátí absolutní pozici daného relativního bodu 
        /// vzhledem k bodům begin (relativní pozice = 0) a end (relativní pozice = 1).
        /// Pokud mezi body begin a end je vzdálenost 0, pak vrací begin.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        private static float _GetAbsolute(float begin, float end, decimal relative)
        {
            decimal size = (decimal)(end - begin);
            if (size == 0m) return begin;
            return begin + (float)(relative * size);
        }

        private static float _GetBeginFromRelative(float fix, float size, decimal relative)
        {
            return fix - (float)((decimal)size * relative);
        }
        #endregion
        #region RectangleF: MoveEdge, MovePoint
        /// <summary>
        /// Vrátí RectangleF, který vytvoří z this RectangleF, když jeho hranu (edge) posune na novou souřadnici
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="side"></param>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public static RectangleF MoveEdge(this RectangleF rectangleF, RectangleSide side, float dimension)
        {
            float x1 = rectangleF.X;
            float x2 = rectangleF.Right;
            float y1 = rectangleF.Y;
            float y2 = rectangleF.Bottom;
            switch (side)
            {
                case RectangleSide.Top:
                    return FromDim(x1, x2, dimension, y2);
                case RectangleSide.Right:
                    return FromDim(x1, dimension, y1, y2);
                case RectangleSide.Bottom:
                    return FromDim(x1, x2, y1, dimension);
                case RectangleSide.Left:
                    return FromDim(dimension, x2, y1, y2);
            }
            return rectangleF;
        }
        /// <summary>
        /// Vrátí PointF, který leží na daném rohu this RectangleF
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="corner"></param>
        /// <returns></returns>
        public static PointF GetPoint(this RectangleF rectangleF, RectangleCorner corner)
        {
            float x1 = rectangleF.X;
            float x2 = rectangleF.Right;
            float y1 = rectangleF.Y;
            float y2 = rectangleF.Bottom;
            switch (corner)
            {
                case RectangleCorner.LeftTop:
                    return new PointF(x1, y1);
                case RectangleCorner.TopRight:
                    return new PointF(x2, y1);
                case RectangleCorner.RightBottom:
                    return new PointF(x2, y2);
                case RectangleCorner.BottomLeft:
                    return new PointF(x1, y2);
            }
            return PointF.Empty;
        }
        /// <summary>
        /// Vrátí RectangleF, který vytvoří z this RectangleF, když jeho bod (corner) posune na nové souřadnice
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="corner"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF MovePoint(this RectangleF rectangleF, RectangleCorner corner, PointF point)
        {
            switch (corner)
            {
                case RectangleCorner.LeftTop:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.RightBottom), point);
                case RectangleCorner.TopRight:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.BottomLeft), point);
                case RectangleCorner.RightBottom:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.LeftTop), point);
                case RectangleCorner.BottomLeft:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.TopRight), point);
            }
            return rectangleF;
        }
        #endregion
        #region Rectangle, RectangleF: GetArea(), SummaryRectangle(), ShiftBy()
        /// <summary>
        /// Vrací true, pokud this Rectangle má obě velikosti (Width i Height) kladné, a tedy obsahuje nějaký reálný pixel ke kreslení.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static bool HasPixels(this Rectangle r)
        {
            return (r.Width > 0 && r.Height > 0);
        }
        /// <summary>
        /// Vrací plochu daného Rectangle
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static int GetArea(this Rectangle r)
        {
            return r.Width * r.Height;
        }
        /// <summary>
        /// Vrací plochu daného Rectangle
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static float GetArea(this RectangleF r)
        {
            return r.Width * r.Height;
        }
        /// <summary>
        /// Vrátí orientaci tohoto prostoru podle poměru šířky a výšky. Pokud šířka == výšce, pak vrací Horizontal.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this Rectangle r)
        {
            return (r.Width >= r.Height ? Orientation.Horizontal : Orientation.Vertical);
        }
        /// <summary>
        /// Returns a Orientation of this Rectangle. When Width is equal or greater than Height, then returns Horizontal. Otherwise returns Vertica orientation.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this RectangleF r)
        {
            return (r.Width >= r.Height ? Orientation.Horizontal : Orientation.Vertical);
        }
        /// <summary>
        /// Metoda vrátí vzdálenost daného bodu od nejbližšího bodu daného rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static int GetOuterDistance(this Rectangle bounds, Point point)
        {
            int x = point.X;
            int y = point.Y;
            int l = bounds.X;
            int t = bounds.Y;
            int r = bounds.Right;
            int b = bounds.Bottom;

            string q = ((x < l) ? "0" : ((x < r) ? "1" : "2")) + ((y < t) ? "0" : ((y < b) ? "1" : "2"));        // Kvadrant "00" = vlevo nad, "11" = uvnitř, "02" = vlevo pod, atd...
            int dx = 0;
            int dy = 0;
            switch (q)
            {
                case "00":        // Vlevo, Nad
                    dx = l - x;
                    dy = t - y;
                    break;
                case "01":        // Vlevo, Uvnitř
                    dx = l - x;
                    break;
                case "02":        // Vlevo, Pod
                    dx = l - x;
                    dy = y - b;
                    break;
                case "10":        // Uvnitř, Nad
                    dy = t - y;
                    break;
                case "11":        // Uvnitř, Uvnitř
                    break;
                case "12":        // Uvnitř, Pod
                    dy = y - b;
                    break;
                case "20":        // Vpravo, Nad
                    dx = x - r;
                    dy = t - y;
                    break;
                case "21":        // Vpravo, Uvnitř
                    dx = x - r;
                    break;
                case "22":        // Vpravo, Pod
                    dx = x - r;
                    dy = y - b;
                    break;
            }
            if (dy == 0) return dx;
            if (dx == 0) return dy;
            int d = (int)Math.Ceiling(Math.Sqrt((double)(dx * dx + dy * dy)));
            return d;
        }
        /// <summary>
        /// Vrací Rectangle, který je souhrnem všech zadaných Rectangle.
        /// Akceptuje i neviditelné Rectangle (který má Width nebo Height nula nebo záporné), i z nich střádá jejich souřadnice.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryRectangle(params Rectangle[] items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, false);
        }
        /// <summary>
        /// Vrací Rectangle, který je souhrnem viditelných Rectangle.
        /// Viditelný = ten který má Width a Height kladné.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryVisibleRectangle(params Rectangle[] items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, true);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// Akceptuje i neviditelné Rectangle (který má Width nebo Height nula nebo záporné), i z nich střádá jejich souřadnice.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryRectangle(IEnumerable<Rectangle> items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, false);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem viditelných Rectangle.
        /// Viditelný = ten který má Width a Height kladné.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryVisibleRectangle(IEnumerable<Rectangle> items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, true);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        private static Rectangle _SummaryRectangle(IEnumerable<Rectangle> items, bool onlyVisible)
        {
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            bool empty = true;
            foreach (Rectangle item in items)
            {
                if (onlyVisible && (item.Width <= 0 || item.Height <= 0)) continue;

                if (empty)
                {
                    l = item.Left;
                    t = item.Top;
                    r = item.Right;
                    b = item.Bottom;
                    empty = false;
                }
                else
                {
                    if (l > item.Left) l = item.Left;
                    if (t > item.Top) t = item.Top;
                    if (r < item.Right) r = item.Right;
                    if (b < item.Bottom) b = item.Bottom;
                }
            }
            return Rectangle.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Vrací Rectangle, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryRectangle(params Rectangle?[] items)
        {
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            bool empty = true;
            foreach (Rectangle? itemN in items)
            {
                if (itemN.HasValue)
                {
                    Rectangle item = itemN.Value;
                    if (empty)
                    {
                        l = item.Left;
                        t = item.Top;
                        r = item.Right;
                        b = item.Bottom;
                        empty = false;
                    }
                    else
                    {
                        if (l > item.Left) l = item.Left;
                        if (t > item.Top) t = item.Top;
                        if (r < item.Right) r = item.Right;
                        if (b < item.Bottom) b = item.Bottom;
                    }
                }
            }
            return (!empty ? (Rectangle?)Rectangle.FromLTRB(l, t, r, b) : (Rectangle?)null);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF SummaryRectangle(IEnumerable<RectangleF> items)
        {
            float l = 0f;
            float t = 0f;
            float r = 0f;
            float b = 0f;
            bool empty = true;
            foreach (RectangleF item in items)
            {
                if (empty)
                {
                    l = item.Left;
                    t = item.Top;
                    r = item.Right;
                    b = item.Bottom;
                    empty = false;
                }
                else
                {
                    if (l > item.Left) l = item.Left;
                    if (t > item.Top) t = item.Top;
                    if (r < item.Right) r = item.Right;
                    if (b < item.Bottom) b = item.Bottom;
                }
            }
            return RectangleF.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryRectangle(params RectangleF?[] items)
        {
            float l = 0f;
            float t = 0f;
            float r = 0f;
            float b = 0f;
            bool empty = true;
            foreach (RectangleF? itemN in items)
            {
                if (itemN.HasValue)
                {
                    RectangleF item = itemN.Value;
                    if (empty)
                    {
                        l = item.Left;
                        t = item.Top;
                        r = item.Right;
                        b = item.Bottom;
                        empty = false;
                    }
                    else
                    {
                        if (l > item.Left) l = item.Left;
                        if (t > item.Top) t = item.Top;
                        if (r < item.Right) r = item.Right;
                        if (b < item.Bottom) b = item.Bottom;
                    }
                }
            }
            return (!empty ? (RectangleF?)RectangleF.FromLTRB(l, t, r, b) : (RectangleF?)null);
        }
        /// <summary>
        /// Vrátí Rectangle, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Vrátí Rectangle, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrátí Rectangle, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Vrátí RectangleF, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Sub(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X - x, r.Y - y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, Point? point)
        {
            return (r.HasValue && point.HasValue ? (Rectangle?)(new Rectangle(r.Value.Location.Add(point.Value), r.Value.Size)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle plus point (=new Rectangle?(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, int x, int y)
        {
            return (r.HasValue ? (Rectangle?)(new Rectangle(r.Value.X + x, r.Value.Y + y, r.Value.Width, r.Value.Height)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle minus point (=new Rectangle?(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, Point? point)
        {
            return (r.HasValue && point.HasValue ? (Rectangle?)(new Rectangle(r.Value.Location.Sub(point.Value), r.Value.Size)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle minus point (=new Rectangle?(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, int x, int y)
        {
            return (r.HasValue ? (Rectangle?)(new Rectangle(r.Value.X - x, r.Value.Y - y, r.Value.Width, r.Value.Height)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a RectangleF, which is this rectangle plus point (=new RectangleF(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF Add(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF Add(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a RectangleF, which is this rectangle minus point (=new RectangleF(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF Sub(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Sub(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF Sub(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X - x, r.Y - y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrátí nový Rectangle, který má stejnou pozici středu (Center), ale je otočený o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle Swap(this Rectangle r)
        {
            Point center = Center(r);
            Size size = Swap(r.Size);
            return center.CreateRectangleFromCenter(size);
        }
        /// <summary>
        /// Vrátí danou velikost otočenou o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Size Swap(this Size size)
        {
            return new Size(size.Height, size.Width);
        }

        /// <summary>
        /// Vrátí nový Rectangle, který má stejnou pozici středu (Center), ale je otočený o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static RectangleF Swap(this RectangleF r)
        {
            PointF center = Center(r);
            SizeF size = Swap(r.Size);
            return center.CreateRectangleFromCenter(size);
        }
        /// <summary>
        /// Vrátí danou velikost otočenou o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static SizeF Swap(this SizeF size)
        {
            return new SizeF(size.Height, size.Width);
        }
        #endregion
        #region Rectangle: FitInto()
        /// <summary>
        /// Zajistí, že this souřadnice budou umístěny do daného prostoru (disponibleBounds).
        /// Pokud daný prostor je menší, než velikost this, pak velikost this může být zmenšena, anebo this může přesahovat doprava/dolů, podle parametru shrinkToFit
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <param name="shrinkToFit">true = pokud this má větší velikost než disponibleBounds, pak this bude zmenšeno / false = pak this bude přečnívat doprava / dolů.</param>
        /// <returns></returns>
        public static Rectangle FitInto(this Rectangle bounds, Rectangle disponibleBounds, bool shrinkToFit)
        {
            int dx = disponibleBounds.X;
            int dy = disponibleBounds.Y;
            int dw = disponibleBounds.Width;
            int dh = disponibleBounds.Height;
            int dr = dx + dw;
            int db = dy + dh;

            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;

            if (x < dx) x = dx;
            if ((x + w) > dr)
            {
                x = dr - w;
                if (x < dx)
                {
                    x = dx;
                    if (shrinkToFit)
                        w = dw;
                }
            }

            if (y < dy) y = dy;
            if ((y + h) > db)
            {
                y = db - h;
                if (y < dy)
                {
                    y = dy;
                    if (shrinkToFit)
                        h = dh;
                }
            }

            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Zajistí, že this souřadnice budou umístěny do daného prostoru (disponibleBounds).
        /// Pokud daný prostor je menší, než velikost this, pak velikost this může být zmenšena, anebo this může přesahovat doprava/dolů, podle parametru shrinkToFit
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <param name="shrinkToFit">true = pokud this má větší velikost než disponibleBounds, pak this bude zmenšeno / false = pak this bude přečnívat doprava / dolů.</param>
        /// <returns></returns>
        public static RectangleF FitInto(this RectangleF bounds, RectangleF disponibleBounds, bool shrinkToFit)
        {
            float dx = disponibleBounds.X;
            float dy = disponibleBounds.Y;
            float dw = disponibleBounds.Width;
            float dh = disponibleBounds.Height;
            float dr = dx + dw;
            float db = dy + dh;

            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width;
            float h = bounds.Height;

            if (x < dx) x = dx;
            if ((x + w) > dr)
            {
                x = dr - w;
                if (x < dx)
                {
                    x = dx;
                    if (shrinkToFit)
                        w = dw;
                }
            }

            if (y < dy) y = dy;
            if ((y + h) > db)
            {
                y = db - h;
                if (y < dy)
                {
                    y = dy;
                    if (shrinkToFit)
                        h = dh;
                }
            }

            return new RectangleF(x, y, w, h);
        }
        #endregion
        #region Padding
        /// <summary>
        /// Returns true, when this Padding all values are Zero.
        /// </summary>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static bool IsZero(this Padding padding)
        {
            return (padding.Top == 0 && padding.Left == 0 && padding.Right == 0 && padding.Bottom == 0);
        }
        #endregion
        #region GInteractiveState
        /// <summary>
        /// Vrací true, pokud interaktivní stav je jeden z: MouseOver, LeftDown, LeftDrag, RightDown, RightDrag.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsMouseActive(this GInteractiveState state)
        {
            GInteractiveState anyState = GInteractiveState.FlagOver | GInteractiveState.FlagDown | GInteractiveState.FlagDown | GInteractiveState.FlagDrag | GInteractiveState.FlagFrame;
            return ((state & anyState) != 0);
        }
        /// <summary>
        /// Vrací true, pokud interaktivní stav je jeden z: LeftDown, RightDown.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsMouseDown(this GInteractiveState state)
        {
            GInteractiveState anyState = GInteractiveState.FlagDown;
            return ((state & anyState) != 0);
        }
        #endregion
        #region Rectangle a Padding: Rectangle.Add(), Rectangle.Sub(Padding)
        /// <summary>
        /// Vrací vnitřní prostor v this Rectangle, po odečtení daných okrajů.
        /// Pokud okraje (padding) jsou null nebo prázdné, pak vrací výchozí souřadnice.
        /// Jinak vrací: (this.Left + padding.Left, this.Top + padding.Top, this.Width - padding.Horizontal, this.Height - padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle bounds, Padding? padding)
        {
            if (!padding.HasValue) return bounds;
            return Sub(bounds, padding.Value);
        }
        /// <summary>
        /// Vrací vnitřní prostor v this Rectangle, po odečtení daných okrajů.
        /// Pokud okraje (padding) jsou prázdné, pak vrací výchozí souřadnice.
        /// Jinak vrací: (this.Left + padding.Left, this.Top + padding.Top, this.Width - padding.Horizontal, this.Height - padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle bounds, Padding padding)
        {
            if (padding.All == 0) return bounds;
            int l = bounds.Left + padding.Left;
            int t = bounds.Top + padding.Top;
            int w = bounds.Width - padding.Horizontal;
            if (w < 0) w = 0;
            int h = bounds.Height - padding.Vertical;
            if (h < 0) h = 0;
            return new Rectangle(l, t, w, h);
        }
        /// <summary>
        /// Vrací vnější prostor okolo this.Rectangle, po přičtení daných okrajů.
        /// Pokud okraje (padding) jsou null nebo prázdné, pak vrací výchozí souřadnice.
        /// Jinak vrací: (this.Left - padding.Left, this.Top - padding.Top, this.Width + padding.Horizontal, this.Height + padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle bounds, Padding? padding)
        {
            if (!padding.HasValue) return bounds;
            return Add(bounds, padding.Value);
        }
        /// <summary>
        /// Vrací vnější prostor okolo this.Rectangle, po přičtení daných okrajů.
        /// Pokud okraje (padding) jsou prázdné, pak vrací výchozí souřadnice.
        /// Jinak vrací: (this.Left - padding.Left, this.Top - padding.Top, this.Width + padding.Horizontal, this.Height + padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle bounds, Padding padding)
        {
            if (padding.All == 0) return bounds;
            int l = bounds.Left - padding.Left;
            int t = bounds.Top - padding.Top;
            int w = bounds.Width + padding.Horizontal;
            if (w < 0) w = 0;
            int h = bounds.Height + padding.Vertical;
            if (h < 0) h = 0;
            return new Rectangle(l, t, w, h);
        }
        #endregion
        #region Size a Padding: Size.Add(), Size.Sub(Padding)
        /// <summary>
        /// Vrací vnitřní velikost v this Size, po odečtení daných okrajů.
        /// Pokud okraje (padding) jsou null nebo prázdné, pak vrací výchozí velikost.
        /// Jinak vrací: (this.Width - padding.Horizontal, this.Height - padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size Sub(this Size size, Padding? padding)
        {
            if (!padding.HasValue) return size;
            return Sub(size, padding.Value);
        }
        /// <summary>
        /// Vrací vnitřní velikost v this Size, po odečtení daných okrajů.
        /// Pokud okraje (padding) jsou prázdné, pak vrací výchozí velikost.
        /// Jinak vrací: (this.Width - padding.Horizontal, this.Height - padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size Sub(this Size size, Padding padding)
        {
            if (padding.All == 0) return size;
            int w = size.Width - padding.Horizontal;
            if (w < 0) w = 0;
            int h = size.Height - padding.Vertical;
            if (h < 0) h = 0;
            return new Size(w, h);
        }
        /// <summary>
        /// Vrací vnitřní velikost v this Size, po odečtení daných okrajů.
        /// Pokud okraje (padding) jsou prázdné, pak vrací výchozí velikost.
        /// Jinak vrací: (this.Width - horizontal, this.Height - vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        /// <returns></returns>
        public static Size Sub(this Size size, int horizontal, int vertical)
        {
            int w = size.Width - horizontal;
            if (w < 0) w = 0;
            int h = size.Height - vertical;
            if (h < 0) h = 0;
            return new Size(w, h);
        }
        /// <summary>
        /// Vrací vnější velikost okolo this Size, po přičtení daných okrajů.
        /// Pokud okraje (padding) jsou null nebo prázdné, pak vrací výchozí velikost.
        /// Jinak vrací: (this.Width + padding.Horizontal, this.Height + padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size Add(this Size size, Padding? padding)
        {
            if (!padding.HasValue) return size;
            return Add(size, padding.Value);
        }
        /// <summary>
        /// Vrací vnější velikost okolo this Size, po přičtení daných okrajů.
        /// Pokud okraje (padding) jsou prázdné, pak vrací výchozí velikost.
        /// Jinak vrací: (this.Width + padding.Horizontal, this.Height + padding.Vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size Add(this Size size, Padding padding)
        {
            if (padding.All == 0) return size;
            int w = size.Width + padding.Horizontal;
            if (w < 0) w = 0;
            int h = size.Height + padding.Vertical;
            if (h < 0) h = 0;
            return new Size(w, h);
        }
        /// <summary>
        /// Vrací vnitřní velikost v this Size, po odečtení daných okrajů.
        /// Pokud okraje (padding) jsou prázdné, pak vrací výchozí velikost.
        /// Jinak vrací: (this.Width + horizontal, this.Height + vertical).
        /// Pokud by výsledná šířka nebo výška byla záporná, pak použije hodnotu 0.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        /// <returns></returns>
        public static Size Add(this Size size, int horizontal, int vertical)
        {
            int w = size.Width + horizontal;
            if (w < 0) w = 0;
            int h = size.Height + vertical;
            if (h < 0) h = 0;
            return new Size(w, h);
        }
        #endregion
        #region Font: Modify
        /// <summary>
        /// Returns a new Font from current font, with only modified FontStyle.
        /// </summary>
        /// <param name="font">Source font</param>
        /// <param name="fontStyle">Style for new font</param>
        /// <returns></returns>
        public static Font Modify(this Font font, FontStyle fontStyle)
        {
            return new Font(font, fontStyle);
        }
        /// <summary>
        /// Returns a new Font from current font, with modified Size (parameter double sizeChange).
        /// Size is modified by ratio from original font (new Size = sizeChange * old Size).
        /// </summary>
        /// <param name="font">Source font</param>
        /// <param name="sizeChange">Ratio of change fontsize: new font size = sizeChange * old font size</param>
        /// <returns></returns>
        public static Font Modify(this Font font, double sizeChange)
        {
            return new Font(font.Name, (float)(sizeChange * (double)font.Size), font.Style, font.Unit, font.GdiCharSet);
        }
        /// <summary>
        /// Returns a new Font from current font, with modified Size (parameter double sizeChange) and FontStyle.
        /// Size is modified by ratio from original font (new Size = sizeChange * old Size).
        /// </summary>
        /// <param name="font">Source font</param>
        /// <param name="sizeChange">Ratio of change fontsize: new font size = sizeChange * old font size</param>
        /// <param name="fontStyle">Style for new font</param>
        /// <returns></returns>
        public static Font Modify(this Font font, double sizeChange, FontStyle fontStyle)
        {
            return new Font(font.Name, (float)(sizeChange * (double)font.Size), fontStyle, font.Unit, font.GdiCharSet);
        }

        /// <summary>
        /// Returns a new Font from current font, with new Size (parameter float fontEmSize).
        /// Size is modified by ratio from original font (new Size = sizeChange * old Size).
        /// </summary>
        /// <param name="font">Source font</param>
        /// <param name="fontEmSize">The em-size, in points, of the new font.</param>
        /// <returns></returns>
        public static Font Modify(this Font font, float fontEmSize)
        {
            return new Font(font.Name, fontEmSize, font.Style, font.Unit, font.GdiCharSet);
        }
        /// <summary>
        /// Returns a new Font from current font, with modified Size (parameter double sizeChange) and FontStyle.
        /// Size is modified by ratio from original font (new Size = sizeChange * old Size).
        /// </summary>
        /// <param name="font">Source font</param>
        /// <param name="fontEmSize">The em-size, in points, of the new font.</param>
        /// <param name="fontStyle">Style for new font</param>
        /// <returns></returns>
        public static Font Modify(this Font font, float fontEmSize, FontStyle fontStyle)
        {
            return new Font(font.Name, fontEmSize, fontStyle, font.Unit, font.GdiCharSet);
        }
        #endregion
        #region PreviewKeyDownEventArgs
        /// <summary>
        /// Returns action for control by key in PreviewKeyDownEventArgs argument.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static InteractivePositionAction GetInteractiveAction(this PreviewKeyDownEventArgs args)
        {
            switch (args.KeyCode)
            {
                case Keys.Home: return _GetInteractiveAction(args, InteractivePositionAction.Home, InteractivePositionAction.None, InteractivePositionAction.FirstRow);
                case Keys.PageUp: return _GetInteractiveAction(args, InteractivePositionAction.PageUp, InteractivePositionAction.None, InteractivePositionAction.TopOfPage);
                case Keys.Up: return _GetInteractiveAction(args, InteractivePositionAction.RowUp, InteractivePositionAction.None, InteractivePositionAction.ScrollRowUp);
                case Keys.Down: return _GetInteractiveAction(args, InteractivePositionAction.RowDown, InteractivePositionAction.None, InteractivePositionAction.ScrollRowDown);
                case Keys.PageDown: return _GetInteractiveAction(args, InteractivePositionAction.PageDown, InteractivePositionAction.None, InteractivePositionAction.EndOfPage);     // (same value as Keys.Next)
                case Keys.End: return _GetInteractiveAction(args, InteractivePositionAction.End, InteractivePositionAction.None, InteractivePositionAction.LastRow);
                case Keys.Left: return _GetInteractiveAction(args, InteractivePositionAction.Left, InteractivePositionAction.None, InteractivePositionAction.GroupLeft);
                case Keys.Right: return _GetInteractiveAction(args, InteractivePositionAction.Right, InteractivePositionAction.None, InteractivePositionAction.GroupRight);
                case Keys.Tab: return _GetInteractiveAction(args, InteractivePositionAction.NextControl, InteractivePositionAction.PrevControl, InteractivePositionAction.None);
            }
            return InteractivePositionAction.None;
        }
        /// <summary>
        /// Returns one of action by modifiers key
        /// </summary>
        /// <param name="args"></param>
        /// <param name="actionNone"></param>
        /// <param name="actionShift"></param>
        /// <param name="actionControl"></param>
        /// <returns></returns>
        private static InteractivePositionAction _GetInteractiveAction(PreviewKeyDownEventArgs args, InteractivePositionAction actionNone, InteractivePositionAction actionShift, InteractivePositionAction actionControl)
        {
            if (args.Modifiers == Keys.None) return actionNone;
            if (args.Modifiers == Keys.Control) return actionControl;
            if (args.Modifiers == Keys.Shift) return actionShift;
            return InteractivePositionAction.None;
        }
        #endregion
    }
    #region enum RectangleSide, RectangleCorner, InteractivePositionAction
    /// <summary>
    /// Vyjádření názvu hrany na objektu Rectangle (Horní, Vpravo, Dolní, Vlevo).
    /// Enum povoluje sčítání hodnot, ale různé funkce nemusejí sečtené hodnoty akceptovat (z důvodu jejich logiky).
    /// </summary>
    [Flags]
    public enum RectangleSide
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo svislá na ose X</summary>
        Left = 0x01,
        /// <summary>Střed na ose X</summary>
        CenterX = 0x02,
        /// <summary>Vpravo svislá na ose X</summary>
        Right = 0x04,
        /// <summary>Horní vodorovná na ose Y</summary>
        Top = 0x10,
        /// <summary>Střed na ose Y</summary>
        CenterY = 0x20,
        /// <summary>Dolní vodorovná na ose Y</summary>
        Bottom = 0x40,
        /// <summary>Vodorovné = Top + Bottom</summary>
        Horizontal = Top | Bottom,
        /// <summary>Svislé = Left + Right</summary>
        Vertical = Left | Right,
        /// <summary>
        /// Prostřední bod
        /// </summary>
        Center = CenterX | CenterY,
        /// <summary>
        /// Horní levý bod
        /// </summary>
        TopLeft = Top | Left,
        /// <summary>
        /// Horní prostřední bod
        /// </summary>
        TopCenter = Top | CenterX,
        /// <summary>
        /// Horní pravý bod
        /// </summary>
        TopRight = Top | Right,
        /// <summary>
        /// Střední levý bod
        /// </summary>
        MiddleLeft = CenterY | Left,
        /// <summary>
        /// Úplně střední bod (X i Y)
        /// </summary>
        MiddleCenter = CenterY | CenterX,
        /// <summary>
        /// Střední pravý bod
        /// </summary>
        MiddleRight = CenterY | Right,
        /// <summary>
        /// Dolní levý bod
        /// </summary>
        BottomLeft = Bottom | Left,
        /// <summary>
        /// Dolní prostřední bod
        /// </summary>
        BottomCenter = Bottom | CenterX,
        /// <summary>
        /// Dolní pravý roh
        /// </summary>
        BottomRight = Bottom | Right,
        /// <summary>Všechny</summary>
        All = Left | Top | Right | Bottom
    }
    /// <summary>
    /// Vyjádření názvu rohu na objektu Rectangle (Vlevo nahoře, Vpravo nahoře, ...)
    /// </summary>
    public enum RectangleCorner
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo nahoře</summary>
        LeftTop,
        /// <summary>Vpravo nahoře</summary>
        TopRight,
        /// <summary>Vpravo dole</summary>
        RightBottom,
        /// <summary>Vlevo dole</summary>
        BottomLeft
    }
    /// <summary>
    /// Interaktivní změna pozice v controlu, daná klávesami (nikoli myší)
    /// </summary>
    public enum InteractivePositionAction
    {
        /// <summary>Není změna</summary>
        None,
        /// <summary>Ctrl + Home = Jdi na úplně první řádek, na první sloupec</summary>
        FirstRow,
        /// <summary>Ctrl + PageUp = Jdi na první viditelný řádek, a pokud jsi na něm nechoď nikam jinam, nescrolluj</summary>
        TopOfPage,
        /// <summary>PageUp = jdi o stránku výše, ale v rámci stránky zůstaň na relativně podobné pozici jaká byla výchozí</summary>
        PageUp,
        /// <summary>Ctrl + Cursor Up = posuň obsah o jeden řádek dolů (tzn. zobrazí se více toho, co je nahoře), ale neměň aktivní řádek, dokud je ve viditelné oblasti. Teprve až by se aktivní řádek dostal mimo viditelnou oblast, pak aktivuj řádek o jedna nižší = úplně dole.</summary>
        ScrollRowUp,
        /// <summary>Cursor Up = jdi na předchozí řádek (-1), scrolluj pokud je potřeba</summary>
        RowUp,
        /// <summary>Cursor Down = jdi na následující řádek (+1), scrolluj pokud je potřeba</summary>
        RowDown,
        /// <summary>Ctrl + Cursor Down = posuň obsah o jeden řádek nahoru (tzn. zobrazí se více toho, co je dole), ale neměň aktivní řádek, dokud je ve viditelné oblasti. Teprve až by se aktivní řádek dostal mimo viditelnou oblast, pak aktivuj řádek o jedna vyšší = úplně nahoře.</summary>
        ScrollRowDown,
        /// <summary>PageDown = jdi o stránku níže, ale v rámci stránky zůstaň na relativně podobné pozici jaká byla výchozí</summary>
        PageDown,
        /// <summary>Ctrl + PageDown = Jdi na poslední viditelný řádek, a pokud jsi na něm nechoď nikam jinam, nescrolluj</summary>
        EndOfPage,
        /// <summary>Ctrl + End = Jdi na úplně poslední řádek, na poslední sloupec</summary>
        LastRow,
        /// <summary>Mouse wheel Up = scrolluj nahoru = zobraz na první pozici řádek -NN, ale neměň aktivní řádek, ten může klidně vyjet mimo viditelnou oblast.</summary>
        WheelUp,
        /// <summary>Mouse wheel Down = scrolluj dolů = zobraz na první pozici řádek +NN, ale neměň aktivní řádek, ten může klidně vyjet mimo viditelnou oblast.</summary>
        WheelDown,
        /// <summary>Home = Jdi na první sloupec</summary>
        Home,
        /// <summary>Ctrl+Left = Jdi na předchozí slovo, doleva</summary>
        GroupLeft,
        /// <summary>Left = Jdi na předchozí buňku / písmeno doleva</summary>
        Left,
        /// <summary>Right = Jdi na následující buňku / písmeno doprava</summary>
        Right,
        /// <summary>Ctrl+Right = Jdi na následující slovo, doprava</summary>
        GroupRight,
        /// <summary>End = Jdi na poslední sloupec, doprava</summary>
        End,
        /// <summary>Shift + Tab = Jdi na předchozí Control (doleva)</summary>
        PrevControl,
        /// <summary>Tab = Jdi na následující Control (doprava)</summary>
        NextControl
    }
    #endregion
    #region class SysCursors, enum SysCursorType
    /// <summary>
    /// Práce s objekty Cursor
    /// </summary>
    internal class SysCursors
    {
        public static Cursor GetCursor(SysCursorType cursorType)
        {
            switch (cursorType)
            {
                case SysCursorType.AppStarting: return Cursors.AppStarting;
                case SysCursorType.Arrow: return Cursors.Arrow;
                case SysCursorType.Cross: return Cursors.Cross;
                case SysCursorType.Default: return Cursors.Default;
                case SysCursorType.Hand: return Cursors.Hand;
                case SysCursorType.Help: return Cursors.Help;
                case SysCursorType.HSplit: return Cursors.HSplit;
                case SysCursorType.IBeam: return Cursors.IBeam;
                case SysCursorType.No: return Cursors.No;
                case SysCursorType.NoMove2D: return Cursors.NoMove2D;
                case SysCursorType.NoMoveHoriz: return Cursors.NoMoveHoriz;
                case SysCursorType.NoMoveVert: return Cursors.NoMoveVert;
                case SysCursorType.PanEast: return Cursors.PanEast;
                case SysCursorType.PanNE: return Cursors.PanNE;
                case SysCursorType.PanNorth: return Cursors.PanNorth;
                case SysCursorType.PanNW: return Cursors.PanNW;
                case SysCursorType.PanSE: return Cursors.PanSE;
                case SysCursorType.PanSouth: return Cursors.PanSouth;
                case SysCursorType.PanSW: return Cursors.PanSW;
                case SysCursorType.PanWest: return Cursors.PanWest;
                case SysCursorType.SizeAll: return Cursors.SizeAll;
                case SysCursorType.SizeNESW: return Cursors.SizeNESW;
                case SysCursorType.SizeNS: return Cursors.SizeNS;
                case SysCursorType.SizeNWSE: return Cursors.SizeNWSE;
                case SysCursorType.SizeWE: return Cursors.SizeWE;
                case SysCursorType.UpArrow: return Cursors.UpArrow;
                case SysCursorType.VSplit: return Cursors.VSplit;
                case SysCursorType.WaitCursor: return Cursors.WaitCursor;

                // case SysCursorType.ExtCrossDoc: return Pics.IconLibraryMin.Cross_r_doc;
            }
            return Cursors.Default;
        }
    }
    /// <summary>
    /// Druh kurzoru.
    /// Větší část hodnot odpovídá systémovým kurzorům třídy System.Windows.Forms.Cursors
    /// Konkrétní objekt Cursor lze získat statickou metodou třídy SysCursors.GetCursor(SysCursorType cursorType)
    /// </summary>
    public enum SysCursorType
    {
        /// <summary>No change (žádná změna)</summary>
        Null = 0,
        /// <summary>cursor that appears when an application starts.</summary>
        AppStarting,
        /// <summary>arrow cursor.</summary>
        Arrow,
        /// <summary>crosshair cursor.</summary>
        Cross,
        /// <summary>default cursor, which is usually an arrow cursor.</summary>
        Default,
        /// <summary>hand cursor, typically used when hovering over a Web link.</summary>
        Hand,
        /// <summary>Help cursor, which is a combination of an arrow and a question mark.</summary>
        Help,
        /// <summary>cursor that appears when the mouse is positioned over a horizontal splitter bar.</summary>
        HSplit,
        /// <summary>I-beam cursor, which is used to show where the text cursor appears when the mouse is clicked.</summary>
        IBeam,
        /// <summary>cursor that indicates that a particular region is invalid for the current operation.</summary>
        No,
        /// <summary>cursor that appears during wheel operations when the mouse is not moving, but the window can be scrolled in both a horizontal and vertical direction.</summary>
        NoMove2D,
        /// <summary>cursor that appears during wheel operations when the mouse is not moving, but the window can be scrolled in a horizontal direction.</summary>
        NoMoveHoriz,
        /// <summary>cursor that appears during wheel operations when the mouse is not moving, but the window can be scrolled in a vertical direction.</summary>
        NoMoveVert,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally to the right.</summary>
        PanEast,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically upward and to the right.</summary>
        PanNE,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling vertically in an upward direction.</summary>
        PanNorth,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically upward and to the left.</summary>
        PanNW,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically downward and to the right.</summary>
        PanSE,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling vertically in a downward direction.</summary>
        PanSouth,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically downward and to the left.</summary>
        PanSW,
        /// <summary>cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally to the left.</summary>
        PanWest,
        /// <summary>four-headed sizing cursor, which consists of four joined arrows that point north, south, east, and west.</summary>
        SizeAll,
        /// <summary>two-headed diagonal (northeast/southwest) sizing cursor.</summary>
        SizeNESW,
        /// <summary>two-headed vertical (north/south) sizing cursor.</summary>
        SizeNS,
        /// <summary>two-headed diagonal (northwest/southeast) sizing cursor.</summary>
        SizeNWSE,
        /// <summary>two-headed horizontal (west/east) sizing cursor.</summary>
        SizeWE,
        /// <summary>up arrow cursor, typically used to identify an insertion point.</summary>
        UpArrow,
        /// <summary>cursor that appears when the mouse is positioned over a vertical splitter bar.</summary>
        VSplit,
        /// <summary>wait cursor, typically an hourglass shape.</summary>
        WaitCursor,
        /// <summary>Ne-systémový kurzor: kříž s obdélníkem vpravo dole, pro insert objektu.</summary>
        ExtCrossDoc,
        /// <summary>
        /// Nejde o standardní kurzor, ale o požadavek na kurzor typu NoMoveHoriz nebo NoMoveVert nebo NoMove2D, podle aktuálního typu přesunu.
        /// </summary>
        NoMoveAuto
    }
    #endregion
}
