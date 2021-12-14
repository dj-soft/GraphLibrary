using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDevExpress.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress
{
    #region class PanelDevExpressSkin : panel, který dovoluje automaticky reagovat na skin DevExpress
    /// <summary>
    /// AsolPanel : panel, který dovoluje automaticky reagovat na skin DevExpress (mění <see cref="Control.BackColor"/>).
    /// </summary>
    public class AsolPanel : ContainerControl
    {
        #region Konstruktor, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AsolPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            DevExpressSkinEnabled = true;
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DevExpressSkinEnabled = false;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Při vykreslení panelu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }
        #endregion
        #region DevExpress
        /// <summary>
        /// Obsahuje true, pokud this panel je napojen na DevExpress skin
        /// </summary>
        public bool DevExpressSkinEnabled
        {
            get { return _DevExpressSkinEnabled; }
            set
            {
                if (_DevExpressSkinEnabled)
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged -= DevExpressSkinChanged;
                _DevExpressSkinEnabled = value;
                if (_DevExpressSkinEnabled)
                {
                    DevExpressSkinLoad();
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpressSkinChanged;
                }
            }
        }
        private bool _DevExpressSkinEnabled;
        /// <summary>
        /// Provede se po změně DevExpress Skinu (event je volán z <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/> : <see cref="DevExpress.LookAndFeel.UserLookAndFeel.StyleChanged"/>)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpressSkinChanged(object sender, EventArgs e)
        {
            DevExpressSkinLoad();
        }
        /// <summary>
        /// Načte aktuální hodnoty DevExpress Skinu do this controlu
        /// </summary>
        private void DevExpressSkinLoad()
        {
            var skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
            var skin = DevExpress.Skins.SkinManager.Default.GetSkin(DevExpress.Skins.SkinProductId.Common, skinName);
            using (this.ScopeSuspendParentLayout())
            {
                this.BackColor = skin.GetSystemColor(SystemColors.ControlLight);
                this.ForeColor = skin.GetSystemColor(SystemColors.ControlText);
            }
        }
        #endregion
    }
    #endregion
    #region class DrawingExtensions : Extensions metody pro grafické třídy (z namespace System.Drawing)

    /*

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
            a = (a < 0 ? 0 : (a > 255 ? 255 : 0));
            return Color.FromArgb(a, root.R, root.G, root.B);
        }
        #endregion
        #region Rectangle
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
        /// Vrátí bod uprostřed this Rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static PointF Center(this RectangleF rectangle)
        {
            return new PointF(rectangle.X + rectangle.Width / 2f, rectangle.Y + rectangle.Height / 2f);
        }

        #endregion
    }
    */
    #endregion
    #region class AsolSamplePanel : panel zobrazující nějaký obrazec pro demostraci rozměrů panelu a jeho reakci na resize/relocation
    /// <summary>
    /// AsolSamplePanel : panel zobrazující nějaký obrazec pro demostraci rozměrů panelu a jeho reakci na resize/relocation
    /// </summary>
    internal class AsolSamplePanel : AsolPanel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AsolSamplePanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _CenterColor = Color.DarkViolet;
            _SurroundColor = Color.DarkBlue;
            _SurroundColorByParent = true;
        }
        /// <summary>
        /// Barva uprostřed
        /// </summary>
        public Color CenterColor { get { return _CenterColor; } set { _CenterColor = value; Invalidate(); } }
        private Color _CenterColor;
        /// <summary>
        /// Barva na okraji. Bude se akceptovat pouze tehdy, když <see cref="SurroundColorByParent"/> je false.
        /// </summary>
        public Color SurroundColor { get { return _SurroundColor; } set { _SurroundColor = value; Invalidate(); } }
        private Color _SurroundColor;
        /// <summary>
        /// Barva okraje 1px
        /// </summary>
        public Color? BorderColor { get { return _BorderColor; } set { _BorderColor = value; Invalidate(); } }
        private Color? _BorderColor;
        /// <summary>
        /// Barva na okraji se má převzít z barvy parenta.
        /// Default = true
        /// </summary>
        public bool SurroundColorByParent { get { return _SurroundColorByParent; } set { _SurroundColorByParent = value; Invalidate(); } }
        private bool _SurroundColorByParent;
        /// <summary>
        /// Typ tvaru
        /// </summary>
        public ShapeType Shape { get { return _Shape; } set { _Shape = value; Invalidate(); } }
        private ShapeType _Shape;
        /// <summary>
        /// Typy tvarů
        /// </summary>
        public enum ShapeType { Rhombus, Star4, Star8AcuteAngles, Star8ObtuseAngles }
        /// <summary>
        /// Vykreslení obsahu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            var size = this.ClientSize;
            if (size.Width <= 2 || size.Height <= 2) return;

            e.Graphics.Clear(this.BackColor);
            PointF[] points = GetPoints(_Shape, out PointF centerPoint);
            if (points is null) return;

            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddPolygon(points);
                using (System.Drawing.Drawing2D.PathGradientBrush pgb = new System.Drawing.Drawing2D.PathGradientBrush(path))
                {
                    pgb.CenterColor = _CenterColor;
                    pgb.CenterPoint = centerPoint;
                    var sc = (this.SurroundColorByParent && this.Parent != null ? this.Parent.BackColor : this._SurroundColor);
                    pgb.SurroundColors = new Color[] { sc };
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    e.Graphics.FillPath(pgb, path);
                }
                var borderColor = BorderColor;
                if (borderColor.HasValue)
                {
                    Rectangle bounds = this.ClientRectangle;
                    bounds.Width -= 1;
                    bounds.Height -=1;
                    Pen pen = DxComponent.PaintGetPen(borderColor.Value);
                    e.Graphics.DrawRectangle(pen, bounds);
                }
            }
        }
        /// <summary>
        /// Vrátí souřadnice vrcholů daného tvaru a určí jeho střed, vše v aktuálním prostoru <see cref="Control.ClientSize"/>
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="centerPoint"></param>
        /// <returns></returns>
        protected PointF[] GetPoints(ShapeType shape, out PointF centerPoint)
        {
            float w = this.ClientSize.Width - 1f;
            float h = this.ClientSize.Height - 1f;

            float s1 = 0f;        // Relativní vzdálenost bodů 1 a 7 od okraje prvku
            float s2 = 0f;        // Relativní vzdálenost bodů 2 a 6 od středu prvku
            float s4 = 0.5f;      // Střed prvku
            switch (shape)
            {
                case ShapeType.Rhombus:
                    break;
                case ShapeType.Star4:
                    s2 = 0.130f;
                    break;
                case ShapeType.Star8AcuteAngles:
                    s1 = 0.333f;
                    s2 = 0.100f;
                    break;
                case ShapeType.Star8ObtuseAngles:
                    s1 = 0.125f;
                    s2 = 0.250f;
                    break;
            }

            float q1 = s1;
            float q2 = s4 - s2;
            float q4 = s4;
            float q6 = s4 + s2;
            float q7 = 1f - s1;
            float x0 = 0f;

            float x1 = q1 * w;
            float x2 = q2 * w;
            float x4 = q4 * w;
            float x6 = q6 * w;
            float x7 = q7 * w;
            float x8 = w;

            float y0 = 0f;
            float y1 = q1 * h;
            float y2 = q2 * h;
            float y4 = q4 * h;
            float y6 = q6 * h;
            float y7 = q7 * h;
            float y8 = h;

            PointF[] points = null;
            centerPoint = new PointF(x4, y4);
            switch (shape)
            {
                case ShapeType.Rhombus:
                    points = new PointF[]
                    {
                        new PointF(x0,y4), new PointF(x4,y0),
                        new PointF(x8,y4), new PointF(x4,y8),
                        new PointF(x0,y4)
                    };
                    break;
                case ShapeType.Star4:
                    points = new PointF[]
                    {
                        new PointF(x0,y4), new PointF(x2,y2), new PointF(x4,y0), new PointF(x6,y2),
                        new PointF(x8,y4), new PointF(x6,y6), new PointF(x4,y8), new PointF(x2,y6),
                        new PointF(x0,y4)
                    };
                    break;
                case ShapeType.Star8AcuteAngles:
                case ShapeType.Star8ObtuseAngles:
                    points = new PointF[]
                    {
                        new PointF(x0,y4), new PointF(x1,y2), new PointF(x0,y0), new PointF(x2,y1),
                        new PointF(x4,y0), new PointF(x6,y1), new PointF(x8,y0), new PointF(x7,y2),
                        new PointF(x8,y4), new PointF(x7,y6), new PointF(x8,y8), new PointF(x6,y7),
                        new PointF(x4,y8), new PointF(x2,y7), new PointF(x0,y8), new PointF(x1,y6),
                        new PointF(x0,y4)
                    };
                    break;

            }

            return points;
        }
    }
    #endregion
    #region PanelResize : panel pro zobrazení posloupnosti (Logu) událostí WinForm
    /// <summary>
    /// PanelResize : panel pro zobrazení posloupnosti (Logu) událostí WinForm
    /// </summary>
    internal class PanelResize : Panel
    {
        #region Konstruktor
        public PanelResize()
        {
            _CurrentSide = Side.None;
            Name = "PanelResize";
            SetStyle(ControlStyles.ResizeRedraw, true);

            _TextInfo = new TextBox()
            {
                Name = "TextInfo",
                BackColor = Color.FromArgb(255, 220, 255, 235),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                ReadOnly = true,
                Font = SystemFonts.StatusFont
            };
            _TextInfo.SizeChanged += _TextInfo_SizeChanged;
            _TextInfo.Resize += _TextInfo_Resize;
            _ResetButton = new Button()
            {
                Name = "ResetButton",
                Text = "Reset"
            };
            _ResetButton.SizeChanged += _ResetButton_SizeChanged;
            _ResetButton.Resize += _ResetButton_Resize;
            _ResetButton.Click += _ResetButton_Click;
            this.SizeChanged += PanelResize_SizeChanged;
            this.Resize += PanelResize_Resize;
            this.Controls.Add(_TextInfo);
            this.Controls.Add(_ResetButton);
            this.SetTextInfoPosition();
            this.ResetLog();
            AddLog($"Initialized values: PanelResize.Bounds: {RectangleToString(Bounds)}, TextInfo.Bounds: {RectangleToString(_TextInfo.Bounds)}, ResetButton.Bounds: {RectangleToString(_ResetButton.Bounds)}");
            OldSize = this.Size;
        }
        private void PanelResize_SizeChanged(object sender, EventArgs e)
        {
            AddLog($"Event PanelResize.SizeChanged, Size: {SizeToString(this.Size)}");
        }
        private void PanelResize_Resize(object sender, EventArgs e)
        {
            AddLog($"Event PanelResize.Resize, Size: {SizeToString(this.Size)}");
        }
        private void _TextInfo_SizeChanged(object sender, EventArgs e)
        {
            AddLog($"Event TextInfo.SizeChanged, Size: {SizeToString(_TextInfo.Size)}");
        }
        private void _TextInfo_Resize(object sender, EventArgs e)
        {
            AddLog($"Event TextInfo.Resize, Size: {SizeToString(_TextInfo.Size)}");
        }
        private void _ResetButton_SizeChanged(object sender, EventArgs e)
        {
            AddLog($"Event ResetButton.SizeChanged, Size: {SizeToString(_ResetButton.Size)}");
        }
        private void _ResetButton_Resize(object sender, EventArgs e)
        {
            AddLog($"Event ResetButton.Resize, Size: {SizeToString(_ResetButton.Size)}");
        }
        private void _ResetButton_Click(object sender, EventArgs e)
        {
            this.ResetLog();
        }
        private TextBox _TextInfo;
        private Button _ResetButton;
        #endregion
        #region Práce s textem
        protected void ResetLog()
        {
            CurrentText = "Log událostí (RightClick resetuje tento log):" + Environment.NewLine;
            LogIndex = 0;
            LogLastTime = DateTime.Now;
        }
        protected void AddLog(string text)
        {
            if (text == null || text.Length == 0) return;
            DateTime now = DateTime.Now;
            TimeSpan delay = now - LogLastTime;
            if (delay.TotalMilliseconds > 200d) CurrentText = CurrentText + Environment.NewLine;          // Časová mezera více než 200ms = nový odstavec
            string time = now.ToString("HH:mm:ss.fff");
            CurrentText = CurrentText + (++LogIndex) + ". " + time + "  " + text + Environment.NewLine;
            LogLastTime = now;
        }
        protected int LogIndex;
        protected DateTime LogLastTime;
        protected string CurrentText { get { return _TextInfo.Text; } set { _TextInfo.Text = value; } }
        #endregion
        #region Interaktivita - MouseMove, MouseClick, PaintSide...
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            CurrentSide = GetMouseSide(e.Location);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            CurrentSide = Side.None;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            ClickOnSide(CurrentSide, (e.Button == MouseButtons.Right ? -10 : 10));
        }
        protected void ClickOnSide(Side side, int change)
        {
            if (side == Side.None) return;

            Rectangle oldBounds = this.Bounds;
            int left = oldBounds.Left;
            int top = oldBounds.Top;
            int right = oldBounds.Right;
            int bottom = oldBounds.Bottom;
            int inc = change;
            switch (side)
            {
                case Side.Left: left -= inc; break;
                case Side.Top: top -= inc; break;
                case Side.Right: right += inc; break;
                case Side.Bottom: bottom += inc; break;
                default: return;
            }
            Rectangle newBounds = Rectangle.FromLTRB(left, top, right, bottom);
            if (this.Parent != null)
            {
                Rectangle clientBounds = this.Parent.ClientRectangle;
                int margin = 3;
                clientBounds.X += margin;
                clientBounds.Y += margin;
                clientBounds.Width -= (2 * margin);
                clientBounds.Height -= (2 * margin);
                newBounds = Rectangle.Intersect(newBounds, clientBounds);
            }
            if (newBounds != oldBounds)
            {
                /*  RŮZNÉ TESTY
                if (side == Side.Left)
                {   // U Left side otestuji vložení Location a Size zvlášť:
                    this.Location = newBounds.Location;
                    this.Size = newBounds.Size;
                }
                if (side == Side.Top)
                {
                    this.Bounds = new Rectangle(newBounds.Location, oldBounds.Size);
                }
                else
                {   // Ostatní nasetuji najednou:
                    this.Bounds = newBounds;
                }
                */
                this.Bounds = newBounds;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            OnPaintSide(e, CurrentSide);
        }
        protected void OnPaintSide(PaintEventArgs e, Side side)
        {
            if (side == Side.None) return;

            Rectangle bounds = this.ClientRectangle;
            Rectangle area = Rectangle.Empty;
            int dist = 2;
            int dist2 = 4;
            int size = 4;
            switch (side)
            {
                case Side.Left: area = new Rectangle(bounds.X + dist, bounds.Y + dist, size, bounds.Height - dist2); break;
                case Side.Top: area = new Rectangle(bounds.X + dist, bounds.Y + dist, bounds.Width - dist2, size); break;
                case Side.Right: area = new Rectangle(bounds.Right - 1 - dist - size, bounds.Y + dist, size, bounds.Height - dist2); break;
                case Side.Bottom: area = new Rectangle(bounds.X + dist, bounds.Bottom - 1 - dist - size, bounds.Width - dist2, size); break;
                default: break;
            }
            if (area.Width <= 0 || area.Height <= 0) return;

            e.Graphics.FillRectangle(SystemBrushes.HotTrack, area);
        }
        /// <summary>
        /// Aktuální strana, u které se nachází myš
        /// </summary>
        protected Side CurrentSide { get { return _CurrentSide; } set { if (value != _CurrentSide) { _CurrentSide = value; Invalidate(); } } }
        private Side _CurrentSide;
        /// <summary>
        /// Vrátí stranu aktuálního <see cref="Control.ClientRectangle"/>, ke které je myš nejblíže.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        protected Side GetMouseSide(Point point, int maxDistance = 15)
        {
            Rectangle bounds = this.ClientRectangle;
            if (!bounds.Contains(point)) return Side.None;
            int dl = point.X - bounds.X;
            int dt = point.Y - bounds.Y;
            int dr = bounds.Right - point.X;
            int db = bounds.Bottom - point.Y;
            int ac = (maxDistance < 3 ? 3 : maxDistance);
            if (dl < ac && dl < dt && dl < db && dl < dr) return Side.Left;
            if (dt < ac && dt < dl && dt < dr && dt < db) return Side.Top;
            if (dr < ac && dr < dt && dr < db && dr < dl) return Side.Right;
            if (db < ac && db < dr && db < dl && db < dt) return Side.Bottom;
            return Side.None;
        }
        protected enum Side { None, Left, Top, Right, Bottom }
        #endregion
        #region Resize
        protected override void OnLayout(LayoutEventArgs levent)
        {
            AddLog($"Layout Control: {(levent.AffectedControl?.Name ?? "None")}, Property: {levent.AffectedProperty}; NewValue: {GetObjectValue(levent.AffectedControl, levent.AffectedProperty)}");
            AddLog($"Layout base method() call...");
            base.OnLayout(levent);
            AddLog($"Layout base method() ends.");
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            Size befSize = this.Size;

            AddLog($"SizeChanged From: {SizeToString(OldSize)}, To: {SizeToString(Size)}; ");
            AddLog($"SizeChanged base method() call...");
            base.OnSizeChanged(e);
            AddLog($"SizeChanged base method() ends.");

            Size aftSize = this.Size;
            if (aftSize != befSize)
            { /* ke změně nedojde, base.OnSizeChanged(e) už souřadnice nemění */ }
            if (aftSize != OldSize)
            {
                SetTextInfoPosition();
                OldSize = aftSize;
            }
            AddLog($"SizeChanged End.");
        }
        protected static string GetObjectValue(object control, string propertyName)
        {
            if (control is null || String.IsNullOrEmpty(propertyName)) return "none";
            Type type = control.GetType();
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            object value = null;
            try
            {
                var propertyInfo = type.GetProperty(propertyName, flags);
                if (propertyInfo != null) value = propertyInfo.GetValue(control);
                else
                {
                    var fieldInfo = type.GetField(propertyName, flags);
                    if (fieldInfo != null) value = fieldInfo.GetValue(control);
                }
            }
            catch { value = null; }
            if (value is null) return "NULL";
            if (value is Size) return SizeToString((Size)value);
            if (value is Rectangle) return RectangleToString((Rectangle)value);
            return value.ToString();
        }
        /// <summary>
        /// Umístí panel <see cref="_TextInfo"/> doprostřed this s okraji 20px
        /// </summary>
        protected void SetTextInfoPosition()
        {
            Rectangle bounds = this.ClientRectangle;
            int d1 = 20; int d2 = 2 * d1;
            bounds.X += d1; bounds.Y += d1; bounds.Width -= d2; bounds.Height -= d2;

            AddLog($"SetTextInfoPosition() into bounds: {RectangleToString(bounds)} ...");

            bool visible = (bounds.Width > 120 && bounds.Height > 30);
            if (visible)
            {
                Rectangle textBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width - 90, bounds.Height);
                if (_TextInfo.Bounds != textBounds) _TextInfo.Bounds = textBounds;
                Rectangle buttonBounds = new Rectangle(bounds.Right - 85, bounds.Bottom - 30, 80, 30);
                if (_ResetButton.Bounds != buttonBounds) _ResetButton.Bounds = buttonBounds;

                if (!_TextInfo.Visible) _TextInfo.Visible = true;
                if (!_ResetButton.Visible) _ResetButton.Visible = true;
            }
            else
            {
                if (_TextInfo.Visible) _TextInfo.Visible = false;
                if (_ResetButton.Visible) _ResetButton.Visible = false;
            }
            System.Threading.Thread.Sleep(10);
            AddLog($"SetTextInfoPosition() End.");
        }
        protected static string SizeToString(Size size) { return $"W:{size.Width},H:{size.Height}"; }
        protected static string RectangleToString(Rectangle bounds) { return $"X:{bounds.X},Y:{bounds.Y},W:{bounds.Width},H:{bounds.Height}"; }
        private Size OldSize;
        #endregion
        #region ZJIŠTĚNÉ VÝSLEDKY = OPSANÝ LOG + KOMENTÁŘE
        /*   OPSANÝ LOG = UDÁLOSTI PŘI JEDNÉ ZMĚNĚ ROZMĚRŮ

41. 13:07:41.736  SizeChanged From: W:831,H:318, To: W:1382,H:872; 
42. 13:07:41.740  SizeChanged base method() call...
43. 13:07:41.742  Layout Control: PanelResize, Property: Bounds; NewValue: X:60,Y:60,W:1382,H:872
44. 13:07:41.744  Layout base method() call...
45. 13:07:41.745  Layout base method() ends.
46. 13:07:41.749  Event PanelResize.SizeChanged, Size: W:1382,H:872
47. 13:07:41.751  SizeChanged base method() ends.
48. 13:07:41.759  SetTextInfoPosition() into bounds: X:20,Y:20,W:1338,H:828 ...
49. 13:07:41.766  Event TextInfo.SizeChanged, Size: W:1248,H:828
50. 13:07:41.770  Layout Control: TextInfo, Property: Bounds; NewValue: X:20,Y:20,W:1248,H:828
51. 13:07:41.772  Layout base method() call...
52. 13:07:41.774  Layout base method() ends.
53. 13:07:41.779  Layout Control: TextInfo, Property: Bounds; NewValue: X:20,Y:20,W:1248,H:828
54. 13:07:41.781  Layout base method() call...
55. 13:07:41.785  Layout base method() ends.
56. 13:07:41.787  Layout Control: ResetButton, Property: Bounds; NewValue: X:1273,Y:818,W:80,H:30
57. 13:07:41.809  Layout base method() call...
58. 13:07:41.814  Layout base method() ends.
59. 13:07:41.826  SetTextInfoPosition() End.
60. 13:07:41.830  SizeChanged End.

        */
        /*   KOMENTÁŘE = PRŮBĚH UDÁLOSTÍ VE WinForms :

        Z nějakého důvodu dojde ke změně velikosti controlu X
        Je spuštěna metoda:                                                       protected override void OnSizeChanged(EventArgs e)
                                                                                  {   // V době volání už je nastavena velikost controlu na nově platnou hodnotu!
        V ní se má vyvolat bázová metoda                                              base.OnSizeChanged(e);
                                                                                      {   // Následující provádí WinForm.Control sám:
        Odtud se vyvolá override metoda                                                   protected override void OnLayout(LayoutEventArgs levent)
                                                                                          {   
        Která dostává v parametrech                                                           levent.AffectedControl = control X; levent.AffectedProperty = "Bounds"
        Metoda OnLayout() skončí, vracíme se do base.OnSizeChanged(e);                    }
        Odtud (z base.OnSizeChanged(e)) se invokuje event SizeChanged:                    X.SizeChanged.Invoke();
        Nyní bázová metoda base.OnSizeChanged(e) končí.                               }

        Poté kód v override void OnSizeChanged(EventArgs e) provádí
        cokoliv dalšího s Child controly (Text, Button)
           = typicky mění jejich souřadnice:                                          this.Text.Bounds = new Rectangle(20, 20, 150, 80);
        Ihned po vložení nové souřadnice do Child prvku (po změně) 
        je vyvolána obdobná sekvence jako tady, ale v metodách child třídy.           {
        Pokud my máme zaregistrovaný eventhandler _Text_SizeChanged
           pro this.Text.SizeChanged, pak se provede nyní:                                ===> _Text_SizeChanged() { instance Text už má nové souřadnice }
        Po doběhnutí eventhandleru se vyvolá naše override OnLayout()                     protected override void OnLayout(LayoutEventArgs levent)
                                                                                          {   
        Která dostává v parametrech                                                           levent.AffectedControl = this.Text; levent.AffectedProperty = "Bounds"
                                                                                          }
        Tahle metoda OnLayout()  se někdy zavolá dvakrát!                             }
        Pokud nasetuji = změním souřadnice dalšího controlu, 
          vyvolá se naše override OnLayout() i pro ten další control.                 this.Button.Bounds = new ... { jeho event, náš OnLayout; }
        Pokud vložím do našeho controlu souřadnice, které nemění jeho velikost:
          - nevyvolá se jeho event SizeChanged
        
        A pak teprve skončí naše override metoda OnSizeChanged()                  }


        DALŠÍ INFORMACE:
          - Pokud pro daný control změním pouze jeho pozici    (setuji Location = new Point(10,10), pak se nevyvolá ani OnResize() a ani OnLayout();
          - Pokud pro daný control změním pouze jeho velikost  (setuji Size = new Size(100,50), pak se vyvolá OnResize() a OnLayout() s levent.AffectedProperty = "Bounds"
          - Pokud do controlu nasetuji Bounds = (změna Location, ale nezměněná Size), pak se nevyvolá nic = jako při změně property Location = ...

        */
        #endregion
    }
    #endregion
    #region class TextItem
    /// <summary>
    /// Text a odpovídající objekt
    /// </summary>
    internal class TextItem
    {
        public override string ToString()
        {
            return this.Text;
        }
        public string Text { get; set; }
        public object Item { get; set; }
    }
    #endregion
    #region class EList<T> : List s přidanými eventy o změnách: ItemAdded, ItemRemoved, CountChanged
    /// <summary>
    /// EList : List s přidanými eventy o změnách: ItemAdded, ItemRemoved, CountChanged
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EList<T> : IEnumerable<T>, IList<T>
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public EList()
        {
            _List = new List<T>();
        }
        private List<T> _List;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Type t = typeof(T);
            return $"EList<{t.FullName}>; Count: {Count}";
        }
        #endregion
        #region Eventy
        /// <summary>
        /// Event vyvolaný po každé změně počtu prvků.
        /// Při hromadných změnách (<see cref="AddRange(IEnumerable{T})"/>, <see cref="Clear()"/>) je volán jedenkrát po dokončení změn.
        /// </summary>
        public event EventHandler CountChanged;
        /// <summary>
        /// Po změně počtu prvků
        /// </summary>
        private void _CountChanged()
        {
            OnCountChanged();
            CountChanged?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Virtuální metoda volaná po změně počtu prvků v Listu
        /// </summary>
        protected virtual void OnCountChanged() { }
        /// <summary>
        /// Event volaný ihned po vložení prvku. V době eventu je prvek již v kolekci. 
        /// Event je volán pro každý vkládáný prvek právě jednou. 
        /// Teprve poté je volán event <see cref="CountChanged"/>.
        /// </summary>
        public event EventHandler<TEventArgs<T>> ItemAdded;
        /// <summary>
        /// Po přidání prvků do Listu
        /// </summary>
        /// <param name="item"></param>
        private void _ItemAdded(T item)
        {
            OnItemAdded(item);
            ItemAdded?.Invoke(this, new TEventArgs<T>(item));
        }
        /// <summary>
        /// Virtuální metoda volaná po přidání prvku do Listu
        /// </summary>
        /// <param name="item"></param>
        protected virtual void OnItemAdded(T item) { }
        /// <summary>
        /// Event volaný těsně před odebráním prvku z kolekce. V době eventu je prvek ještě v kolekci.
        /// Event je volán pro každý odebíraný prvek právě jednou. 
        /// Teprve poté je volán event <see cref="CountChanged"/>.
        /// </summary>
        public event EventHandler<TEventArgs<T>> ItemRemoved;
        /// <summary>
        /// Před odebráním prvku z Listu
        /// </summary>
        /// <param name="item"></param>
        private void _ItemRemoved(T item)
        {
            OnItemRemoved(item);
            ItemRemoved?.Invoke(this, new TEventArgs<T>(item));
        }
        /// <summary>
        /// Virtuální metoda volaná před odebráním prvku z Listu
        /// </summary>
        /// <param name="item"></param>
        protected virtual void OnItemRemoved(T item) { }
        #endregion
        #region Implementace IList a IEnumerable
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count => _List.Count;
        /// <summary>
        /// Je Read-Only?
        /// </summary>
        public bool IsReadOnly => ((IList<T>)_List).IsReadOnly;
        /// <summary>
        /// Prvek na daném indexu.
        /// <para/>
        /// Setování vyvolá event <see cref="ItemRemoved"/> pro stávající prvek na daném indexu, 
        /// pak je prvek na tomto indexu nahrazen dodaným novým prvkem, 
        /// a pak je vyvolán event <see cref="ItemAdded"/> pro nový prvek.
        /// Event <see cref="CountChanged"/> není vyvolán.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return _List[index]; }
            set
            {
                T oldItem = _List[index];
                if (oldItem != null) _ItemRemoved(oldItem);
                _List[index] = value;
                _ItemAdded(value);
            }
        }
        /// <summary>
        /// Index daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            return _List.IndexOf(item);
        }
        /// <summary>
        /// Vloží prvek na daný index. 
        /// Po vložení prvku je volán event <see cref="ItemAdded"/> a poté <see cref="CountChanged"/>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            _List.Insert(index, item);
            _ItemAdded(item);
            _CountChanged();
        }
        /// <summary>
        /// Odebere prvek na daném indexu.
        /// Před jeho odebráním je volán event <see cref="ItemRemoved"/> a poté <see cref="CountChanged"/>.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            T item = _List[index];
            _ItemRemoved(item);
            _List.RemoveAt(index);
            _CountChanged();
        }
        /// <summary>
        /// Přidá nový prvek do seznamu na konec.
        /// Po přidání prvku je volán event <see cref="ItemAdded"/> a poté <see cref="CountChanged"/>
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            _List.Add(item);
            _ItemAdded(item);
            _CountChanged();
        }
        /// <summary>
        /// Vyprázdní celou kolekci.
        /// Před vyprázdněním je volán event <see cref="ItemRemoved"/> pro každý prvek v kolekci.
        /// Po vyprázdnění je volán event <see cref="CountChanged"/> (pokud došlo ke změně <see cref="Count"/>).
        /// </summary>
        public void Clear()
        {
            if (ItemRemoved != null)
            {   // Pokud není přítomen eventhandler ItemRemoved, nebudeme interní seznam iterovat a pokoušet se jej zbytečně volat!
                foreach (T item in _List)
                    _ItemRemoved(item);
            }
            int count = this.Count;
            _List.Clear();
            if (this.Count != count)
                _CountChanged();
        }
        /// <summary>
        /// Vrátí true pokud daný prvek je v kolekci
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return _List.Contains(item);
        }
        /// <summary>
        /// Provede kopírování z this do daného pole
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _List.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Odebere daný prvek
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            _ItemRemoved(item);
            bool result = _List.Remove(item);
            _CountChanged();
            return result;
        }
        /// <summary>
        /// Vrací enumerátor
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _List.GetEnumerator();
        }
        /// <summary>
        /// Vrací enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _List.GetEnumerator();
        }
        #endregion
        #region Nad rámec interface IList - lze libovolně přidávat přímé metody třídy List<T>
        /// <summary>
        /// Přidá sadu prvků.
        /// Po přidání každého prvku je volán event <see cref="ItemAdded"/>.
        /// Po přidání všech prvků do kolekce je volán event <see cref="CountChanged"/> (pokud došlo ke změně <see cref="Count"/>).
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            int count = this.Count;
            if (ItemAdded != null)
            {   // Pokud máme zadaný eventhandler ItemAdded, musíme ho volat per item:
                foreach (T item in items)
                {
                    _List.Add(item);
                    _ItemAdded(item);
                }
            }
            else
            {   // Nemáme event => přidáme všechny položky najednou:
                _List.AddRange(items);
            }
            if (this.Count != count)
                _CountChanged();
        }
        /// <summary>
        /// Vrátí true pokud existuje nějaký prvek vyhovující dané podmínce
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public bool Exists(Predicate<T> match) { return _List.Exists(match); }
        /// <summary>
        /// Najde první prvek vyhovující dané podmínce
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public T Find(Predicate<T> match) { return _List.Find(match); }
        /// <summary>
        /// Najde index prvku vyhovujícího dané podmínce
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public int FindIndex(Predicate<T> match) { return _List.FindIndex(match); }
        /// <summary>
        /// Najde index prvku vyhovujícího dané podmínce, počínaje daným indexem
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public int FindIndex(int startIndex, Predicate<T> match) { return _List.FindIndex(startIndex, match); }
        /// <summary>
        /// Najde index prvku vyhovujícího dané podmínce, počínaje daným indexem, pouze v daném počtu prvků
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public int FindIndex(int startIndex, int count, Predicate<T> match) { return _List.FindIndex(startIndex, count, match); }
        /// <summary>
        /// Setřídí prvky v Listu
        /// </summary>
        public void Sort() { _List.Sort(); }
        /// <summary>
        /// Setřídí prvky v Listu
        /// </summary>
        /// <param name="comparer"></param>
        public void Sort(IComparer<T> comparer) { _List.Sort(comparer); }
        /// <summary>
        /// Setřídí prvky v Listu
        /// </summary>
        /// <param name="comparison"></param>
        public void Sort(Comparison<T> comparison) { _List.Sort(comparison); }
        /// <summary>
        /// Vrátí pole prvků z Listu
        /// </summary>
        /// <returns></returns>
        public T[] ToArray() { return _List.ToArray(); }
        #endregion
    }
    #endregion
    #region class Range<T> : Třída obsahující read-only hodnoty Begin a End
    /// <summary>
    /// <see cref="Range{T}"/> : Třída obsahující read-only hodnoty Begin a End libovolného druhu
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Range<T>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public Range(T begin, T end) { _Begin = begin; _End = end; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Range Begin: {Begin}, End: {End}";
        }
        /// <summary>
        /// Počátek, včetně
        /// </summary>
        public T Begin { get { return _Begin; } } private readonly T _Begin;
        /// <summary>
        /// Konec, mimo
        /// </summary>
        public T End { get { return _End; } } private readonly T _End;
    }
    #endregion
}
