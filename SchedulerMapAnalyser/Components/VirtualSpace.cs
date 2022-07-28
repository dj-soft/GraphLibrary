using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace DjSoft.SchedulerMap.Analyser
{
    public class VirtualControl : Control
    {
        public VirtualControl()
        {
            VirtualSpace = new VirtualSpace(this);
            InitColors();
        }
        protected override void Dispose(bool disposing)
        {
            VirtualSpace.Dispose();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Virtuální prostor - výpočetní mechanismus pro oboustranný přepočet Virtuální - Fyzická souřadnice;
        /// včetně jeho řízení pomocí myši
        /// </summary>
        public VirtualSpace VirtualSpace { get; private set; }
        /// <summary>
        /// Zajistí nové vykreslení obsahu
        /// </summary>
        public virtual void RefreshContent()
        {
            this.Invalidate();
        }
        #region Implicitní barvy
        protected virtual void InitColors()
        {
            ColorOutlineNone = null;
            ColorOutlineOnMouse = Color.FromArgb(100, 182, 255, 0);
            ColorOutlineLeftDown = Color.FromArgb(100, 188, 66, 255);
            ColorOutlineRightDown = Color.FromArgb(100, 188, 66, 255);

            ColorOutlineNoneSelected = Color.FromArgb(220, 255, 216, 0);
            ColorOutlineOnMouseSelected = Color.FromArgb(220, 182, 255, 0);
            ColorOutlineLeftDownSelected = Color.FromArgb(200, 188, 66, 255);
            ColorOutlineRightDownSelected = Color.FromArgb(200, 188, 66, 255);
        }
        public Color? GetOutlineColor(VirtualItemBase item)
        {
            bool isNone = !item.Selected;
            switch (item.MouseState)
            {
                case ItemMouseState.None: return isNone ? ColorOutlineNone : ColorOutlineNoneSelected;
                case ItemMouseState.OnMouse: return isNone ? ColorOutlineOnMouse : ColorOutlineOnMouseSelected;
                case ItemMouseState.LeftDown: return isNone ? ColorOutlineLeftDown : ColorOutlineLeftDownSelected;
                case ItemMouseState.RightDown: return isNone ? ColorOutlineRightDown : ColorOutlineRightDownSelected;
            }
            return null;
        }
        /// <summary>
        /// Barva orámování bez Selectu za stavu myši <see cref="ItemMouseState.None"/>
        /// </summary>
        public Color? ColorOutlineNone { get; set; }
        /// <summary>
        /// Barva orámování Selectovaného prvku za stavu myši <see cref="ItemMouseState.None"/>
        /// </summary>
        public Color? ColorOutlineNoneSelected { get; set; }
        /// <summary>
        /// Barva orámování bez Selectu za stavu myši <see cref="ItemMouseState.OnMouse"/>
        /// </summary>
        public Color? ColorOutlineOnMouse { get; set; }
        /// <summary>
        /// Barva orámování Selectovaného prvku za stavu myši <see cref="ItemMouseState.OnMouse"/>
        /// </summary>
        public Color? ColorOutlineOnMouseSelected { get; set; }
        /// <summary>
        /// Barva orámování bez Selectu za stavu myši <see cref="ItemMouseState.LeftDown"/>
        /// </summary>
        public Color? ColorOutlineLeftDown { get; set; }
        /// <summary>
        /// Barva orámování Selectovaného prvku za stavu myši <see cref="ItemMouseState.LeftDown"/>
        /// </summary>
        public Color? ColorOutlineLeftDownSelected { get; set; }
        /// <summary>
        /// Barva orámování bez Selectu za stavu myši <see cref="ItemMouseState.RightDown"/>
        /// </summary>
        public Color? ColorOutlineRightDown { get; set; }
        /// <summary>
        /// Barva orámování Selectovaného prvku za stavu myši <see cref="ItemMouseState.RightDown"/>
        /// </summary>
        public Color? ColorOutlineRightDownSelected { get; set; }
        #endregion
    }

    /// <summary>
    /// Virtuální prostor - výpočetní mechanismus pro oboustranný přepočet Virtuální - Fyzická souřadnice;
    /// včetně jeho řízení pomocí myši
    /// </summary>
    public class VirtualSpace : IDisposable
    {
        internal VirtualSpace(VirtualControl owner)
        {
            _Owner = owner;
        }
        public void Dispose()
        {
            _Owner = null;
        }
        private VirtualControl _Owner;

        #region Myší interaktivita - posuny a Zoom
        public bool IsMouseDrag { get; private set; }
        public void MouseDown(Point currentPoint, MouseButtons buttons)
        { }
        public void MouseDrag(Point currentPoint, MouseButtons buttons)
        { }
        public void MouseUp(Point currentPoint)
        { }
        public void MouseWheel(Point currentPoint, int delta)
        { }
        /// <summary>
        /// Invaliduje 
        /// </summary>
        protected void Invalidate()
        {
            _Owner?.RefreshContent();
        }
        #endregion

        /// <summary>
        /// Metoda vrátí fyzické souřadnice odpovídající daným virtuálním souřadnicím, oříznuté na viditelnou oblast v rámci Owner controlu.
        /// Do výsledného Rectangle se nemá kreslit, protože by to vedlo k posunutí obsahu do viditelné oblasti 
        /// - na kreslení je třeba získat plné souřadnice pomocí metody <see cref="GetCurrentBounds(RectangleF, out bool)"/>.
        /// Zdejší výsledná hodnota slouží k identifikaci, zda prvek může být aktivní v určité oblasti.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public Rectangle? GetCurrentVisibleBounds(BoundsF virtualBounds)
        {
            if (virtualBounds.Width <= 0f || virtualBounds.Height <= 0f) return null;              // Neviditelná velikost
            Rectangle currentBounds = GetCurrentBounds(virtualBounds, out var isVisible);
            if (!isVisible) return null;                                                           // Celé v neviditelné oblasti
            Rectangle ownerBounds = this.CurrentOwnerBounds;
            Rectangle resultBounds = Rectangle.Intersect(currentBounds, ownerBounds);
            if (resultBounds.IsEmpty) return null;
            return resultBounds;
        }
        /// <summary>
        /// Metoda vrátí fyzické souřadnice odpovídající daným virtuálním souřadnicím.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public Rectangle GetCurrentBounds(BoundsF virtualBounds, out bool isVisible)
        {
            isVisible = true;
            return Rectangle.Round(virtualBounds.RectangleF);
        }
        /// <summary>
        /// Fyzické souřadnice controlu, do kterých je vykreslován obsah = <see cref="Control.ClientRectangle"/> našeho vlastníka
        /// </summary>
        public Rectangle CurrentOwnerBounds { get { return this._Owner.ClientRectangle; } }
    }
    /// <summary>
    /// Základní objekt pro prvky umístěné ve virtuálním prostoru
    /// </summary>
    public class VirtualItemBase : IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="virtualSpace"></param>
        public VirtualItemBase(VirtualControl virtualControl)
        {
            VirtualControl = virtualControl;
            VirtualBounds = new BoundsF();
        }
        /// <summary>
        /// Hostitelský control
        /// </summary>
        protected VirtualControl VirtualControl { get; private set; }
        /// <summary>
        /// Koordinátor virtuálních souřadnic
        /// </summary>
        protected VirtualSpace VirtualSpace { get { return VirtualControl.VirtualSpace; } }
        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            VirtualControl = null;
            VirtualBounds = null;
        }
        #region Virtuální pozice
        /// <summary>
        /// Virtuální souřadnice vztažené k celé pracovní ploše. 
        /// Tuto hodnotu nelze setovat, ale lze nastavovat její veškeré hodnoty
        /// </summary>
        public virtual BoundsF VirtualBounds { get; private set; }
        #endregion

        /// <summary>
        /// Vrstva prvku: čím vyšší hodnota, tím vyšší vrstva = prvek bude kreslen "nad" prvky s nižší vrstvou, a stejně tak bude i aktivní.
        /// Defaultní je 0.
        /// </summary>
        public virtual int Layer { get { return 0; } }
        /// <summary>
        /// Fyzické souřadnice celého prvku v aktuálním controlu.
        /// </summary>
        public Rectangle CurrentBounds { get { return VirtualSpace.GetCurrentBounds(VirtualBounds, out var _); } }
        /// <summary>
        /// Fyzické souřadnice viditelné části prvku v aktuálním controlu, nebo null když prvek není viditelný.
        /// Tedy tyto souřadnice jsou oříznuty do viditelné oblasti
        /// </summary>
        public Rectangle? CurrentVisibleBounds { get { return VirtualSpace.GetCurrentVisibleBounds(VirtualBounds); } }
        /// <summary>
        /// Obsahuje true, pokud tento prvek je nyní iditelný v rámci viditelné oblasti Ownera
        /// </summary>
        public bool IsVisibleInOwner { get { var currentVisibleBounds = VirtualSpace.GetCurrentVisibleBounds(VirtualBounds); return currentVisibleBounds.HasValue; } }
        /// <summary>
        /// Vrátí true, pokud daný fyzický bod leží na fyzických souřadnicích tohoto prvku ve viditelné oblasti Ownera
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        public bool IsVisibleOnCurrentPoint(Point currentPoint)
        {
            var currentVisibleBounds = VirtualSpace.GetCurrentVisibleBounds(VirtualBounds);
            return (currentVisibleBounds.HasValue && currentVisibleBounds.Value.Contains(currentPoint));
        }
        /// <summary>
        /// Stav prvku z hlediska myši
        /// </summary>
        public ItemMouseState MouseState { get; set; }
        /// <summary>
        /// Prvek je Selectován
        /// </summary>
        public bool Selected { get; set; }
        /// <summary>
        /// Aktuální barva orámování, vychází z hodnot <see cref="Selected"/> a <see cref="MouseState"/>
        /// </summary>
        public virtual Color? CurrentOutlineColor { get { return VirtualControl.GetOutlineColor(this); } }
    }


    /// <summary>
    /// Souřadnice typu Float se setovacími hodnotami X; Y; Width; Height; CenterX; CenterY
    /// </summary>
    public class BoundsF
    {
        #region Konstruktory a základní proměnné
        /// <summary>
        /// Konstruktor pro prázdný prvek
        /// </summary>
        public BoundsF()
        {
            _X = 0f;
            _Y = 0f;
            _Width = 0f;
            _Height = 0f;
        }
        /// <summary>
        /// Konstruktor pro dané základní hodnoty
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public BoundsF(float x, float y, float width, float height)
        {
            _X = x;
            _Y = y;
            _Width = width;
            _Height = height;
        }
        /// <summary>
        /// Konstruktor pro daný počátek a velikost
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="size"></param>
        public BoundsF(PointF origin, SizeF size)
        {
            _X = origin.X;
            _Y = origin.Y;
            _Width = size.Width;
            _Height = size.Height;
        }
        /// <summary>
        /// Konstruktor z daného <see cref="RectangleF"/>
        /// </summary>
        /// <param name="rectangleF"></param>
        public BoundsF(RectangleF rectangleF)
        {
            this.RectangleF = rectangleF;
        }
        /// <summary>
        /// Vytvoří novou instanci <see cref="BoundsF"/> pro daný střed a velikost
        /// </summary>
        /// <param name="centerX"></param>
        /// <param name="centerY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static BoundsF FromCenterSize(float centerX, float centerY, float width, float height)
        {
            BoundsF boundsF = new BoundsF();
            boundsF.Size = new SizeF(width, height);
            boundsF.Center = new PointF(centerX, centerY);
            return boundsF;
        }
        /// <summary>
        /// Vytvoří novou instanci <see cref="BoundsF"/> pro daný střed a velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static BoundsF FromCenterSize(PointF center, SizeF size)
        {
            BoundsF boundsF = new BoundsF();
            boundsF.Size = size;
            boundsF.Center = center;
            return boundsF;
        }
        /// <summary>
        /// Obsahuje prázdný prvek (vždy vrátí new instanci)
        /// </summary>
        public static BoundsF Empty { get { return new BoundsF(); } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{{ X: {_X}; Y: {_Y}; Width: {_Width}; Height: {_Height} }}";
        }
        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _X.GetHashCode() ^ _Y.GetHashCode() ^ _Width.GetHashCode() ^ _Height.GetHashCode();
        }
        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return IsEquals(this, obj as BoundsF);
        }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(BoundsF a, BoundsF b) { return BoundsF.IsEquals(a, b); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(BoundsF a, BoundsF b) { return (!BoundsF.IsEquals(a, b)); }
        /// <summary>
        /// Porovná dvě instance
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool IsEquals(BoundsF a, BoundsF b)
        {
            bool an = a is null;
            bool bn = b is null;
            if (an && bn) return true;
            if (an || bn) return false;
            return (a._X == b._X && a._Y == b._Y && a._Width == b._Width && a._Height == b._Height);
        }
        private float _X;
        private float _Y;
        private float _Width;
        private float _Height;
        #endregion
        #region Property
        /// <summary>
        /// Souřadnice počátku X
        /// </summary>
        public float X { get { return _X; } set { _X = value; } }
        /// <summary>
        /// Souřadnice středu X
        /// </summary>
        public float CenterX { get { return _X + HalfWidth; } set { _X = value - HalfWidth; } }
        /// <summary>
        /// Souřadnice konce X = vpravo
        /// </summary>
        public float Right { get { return _X + _Width; } set { _X = value - _Width; } }
        /// <summary>
        /// Souřadnice počátku Y
        /// </summary>
        public float Y { get { return _Y; } set { _Y = value; } }
        /// <summary>
        /// Souřadnice středu Y
        /// </summary>
        public float CenterY { get { return _Y + HalfHeight; } set { _Y = value - HalfHeight; } }
        /// <summary>
        /// Souřadnice konce Y = dole
        /// </summary>
        public float Bottom { get { return _Y + _Height; } set { _Y = value - _Height; } }
        /// <summary>
        /// Šířka. 
        /// Setování hodnoty ponechává pozici počátku (X, Y) beze změny, posouvá se tedy střed i konec.
        /// Existuje property <see cref="SizeCentered"/>, jejíž setování ponechává střed beze změny a nastaví velikost, posune tedy pozici počátku.
        /// </summary>
        public float Width { get { return _Width; } set { _Width = value; } }
        /// <summary>
        /// Výška. 
        /// Setování hodnoty ponechává pozici počátku (X, Y) beze změny, posouvá se tedy střed i konec.
        /// Existuje property <see cref="SizeCentered"/>, jejíž setování ponechává střed beze změny a nastaví velikost, posune tedy pozici počátku.
        /// </summary>
        public float Height { get { return _Height; } set { _Height = value; } }
        /// <summary>
        /// Rectangle
        /// </summary>
        public RectangleF RectangleF { get { return new RectangleF(_X, _Y, _Width, _Height); } set { _X = value.X; _Y = value.Y; _Width = value.Width; _Height = value.Height; } }
        /// <summary>
        /// Bod počátku = X, Y
        /// </summary>
        public PointF Origin { get { return new PointF(_X, _Y); } set { _X = value.X; _Y = value.Y; } }
        /// <summary>
        /// Bod středu = CenterX, CenterY
        /// </summary>
        public PointF Center { get { return new PointF(CenterX, CenterY); } set { CenterX = value.X; CenterY = value.Y; } }
        /// <summary>
        /// Bod konce = Righ, Bottom
        /// </summary>
        public PointF End { get { return new PointF(Right, Bottom); } set { Right = value.X; Bottom = value.Y; } }
        /// <summary>
        /// Velikost.
        /// Setování hodnoty ponechává pozici počátku (X, Y) beze změny, posouvá se tedy střed i konec.
        /// Existuje property <see cref="SizeCentered"/>, jejíž setování ponechává střed beze změny a nastaví velikost, posune tedy pozici počátku.
        /// </summary>
        public SizeF Size { get { return new SizeF(_Width, _Height); } set { _Width = value.Width; _Height = value.Height; } }
        /// <summary>
        /// Velikost.
        /// Setování hodnoty ponechává pozici středu (X, Y) beze změny, posouvá se tedy počátek i konec.
        /// </summary>
        public SizeF SizeCentered
        {
            get { return new SizeF(Width, Height); }
            set 
            {
                PointF center = this.Center;
                _Width = value.Width; 
                _Height = value.Height;
                this.Center = center;
            }
        }
        /// <summary>
        /// Nastaví současně střed a velikost
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        public void SetCenterSize(PointF center, SizeF size)
        {
            this.Size = size;
            this.Center = center;
        }
        /// <summary>
        /// true pokud je Empty = všechny souřadnice jsou 0
        /// </summary>
        public bool IsEmpty { get { return (_X == 0f && _Y == 0f && _Width == 0f && _Height == 0f); } }
        /// <summary>
        /// true pokud je reálně neviditelný = šířka nebo výška jsou nula nebo záporné
        /// </summary>
        public bool IsVoid { get { return (_Width <= 0f || _Height <= 0f); } }
        /// <summary>
        /// Půl šířky
        /// </summary>
        private float HalfWidth { get { return _Width / 2f; } }
        /// <summary>
        /// Půl výšky 
        /// </summary>
        private float HalfHeight { get { return _Height / 2f; } }
        #endregion
    }
}
