using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace DjSoft.SchedulerMap.Analyser
{
    /// <summary>
    /// Vizuální Control, s podporou pro virtualizaci prostoru (Zoomování a posouvání obsahu), 
    /// a s instancí <see cref="VirtualSpace"/> pro přepočty souřadnic
    /// </summary>
    public class VirtualControl : Control
    {
        #region Konstruktor, instance VirtualSpace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public VirtualControl()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.UserMouse, true);
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
        #endregion
        #region Virtuální prvky umístěné v tomto controlu
        /// <summary>
        /// Kolekce všech virtuálních prvků, které mohou být zobrazeny v this controlu.
        /// </summary>
        protected virtual IEnumerable<VirtualItemBase> ItemsAll { get { return null; } }
        #endregion
        #region Implicitní barvy a další hodnoty
        /// <summary>
        /// Inicializace výchozích hodnot barev typu Outline
        /// </summary>
        protected virtual void InitColors()
        {
            ColorOutlineNone = null;
            ColorOutlineOnMouse = Color.FromArgb(64, 248, 64, 255);
            ColorOutlineLeftDown = Color.FromArgb(192, 248, 64, 255);
            ColorOutlineRightDown = Color.FromArgb(140, 248, 64, 255);

            ColorOutlineNoneSelected = Color.FromArgb(192, 255, 255, 128);
            ColorOutlineOnMouseSelected = Color.FromArgb(220, 255, 191, 127);
            ColorOutlineLeftDownSelected = Color.FromArgb(220, 255, 144, 33);
            ColorOutlineRightDownSelected = Color.FromArgb(220, 255, 179, 104);
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
        /// <summary>
        /// Vrátí velikost okraje Outline pro daný zoom. 
        /// Outline margin je logaritmický, pro malé Zoom je 1px, pro zoom 1 je 2px, pro velké Zoomy 4px.
        /// </summary>
        /// <param name="zoom">Logaritmický Zoom v rozsahu 0.01 - 100</param>
        /// <returns></returns>
        public static float GetOutlineMargin(float zoom)
        {
            if (zoom < 0.50f) return 1f;
            if (zoom < 1.25f) return 2f;
            if (zoom < 3.125f) return 3f;
            if (zoom < 7.812f) return 4f;
            if (zoom < 19.53f) return 5f;
            if (zoom < 48.83) return 6f;
            return 7f;
        }
        /// <summary>
        /// Vrátí velikost okraje Border pro daný zoom. 
        /// Outline margin je logaritmický, pro malé Zoom je 1px, pro zoom 1 je 2px, pro velké Zoomy 4px.
        /// </summary>
        /// <param name="zoom">Logaritmický Zoom v rozsahu 0.01 - 100</param>
        /// <returns></returns>
        public static float GetBorderMargin(float zoom)
        {
            if (zoom < 0.40f) return 1f;
            if (zoom < 1.00f) return 2f;
            if (zoom < 2.50f) return 3f;
            if (zoom < 6.25f) return 4f;
            if (zoom < 15.65f) return 5f;
            if (zoom < 39.0) return 6f;
            if (zoom < 95.0) return 7f;
            return 8f;
        }
        /// <summary>
        /// Vrátí velikost písma pro daný zoom.
        /// Vstupní Zoom je Lineární, v rozsahu 0 - 200, kde 100 = 1:1.
        /// Výstupní velikost písma odpovídá dané základní velikosti, zoomu a zdravému rozumu.
        /// </summary>
        /// <param name="zoomLinear">Lineární Zoom v rozsahu 0 - 200, kde 100 = 1:1</param>
        /// <param name="baseSize">Základní velikost písma pro Zoom 100, default = null odpovídá 8.5f, přípustný rozsah = 4 až 24</param>
        /// <returns></returns>
        public static float GetFontSizeEm(float zoomLinear, float? baseSize = null)
        {
            float size = VirtualSpace.Align((baseSize ?? 8.5f), 4f, 24f);
            float ratio = ((zoomLinear - 100f) / 100f);                                            // Vyjadřuje základnu lineární změny velikosti: -1 = největší zmenšení;   0 = 1:1;   +1 = největší zvětšení
            float zoom = 1f + (ratio > 0f ? ZoomToFontRatioHigh : ZoomToFontRatioLow) * ratio;     // Vyjadřuje změnu základní velikosti fontu: 1 = beze změny, <1 = zmenšení,  >1 = zvětšení
            float emSize = VirtualSpace.Align((size * zoom), 4f, 64f);
            return (float)Math.Round(emSize, 1);
        }
        /// <summary>
        /// Přepočtová konstanta mezi lineárním zoomem a velikostí písma, určuje poměr zmenšení písma při zmenšení Zoomu, <br/>
        /// <u>pro oblast Zoomu MENŠÍ než 100 (Linear).</u><br/>
        /// Čím menší hodnota, tím menší vliv má změna Zoomu na změnu velikosti písma.
        /// Větší hodnota <see cref="ZoomToFontRatioLow"/> způsobí větší změny velikosti písma při změně Zoomu.
        /// <para/>
        /// Hodnota 1.5 dává vcelku dobrou reakci, <br/>
        /// hodnota 1 dává poměrně velká písmena při hodně zmenšených objektech = nepřirozeně velké písmo (pomalá reakce na Zoom).<br/>
        /// hodnota 3 dává rychlou reakci (při zmenšování motivu se písmo zmenšuje rychle a brzy je nečitelné).<br/>
        /// hodnota 0 nereaguje na Zoom.
        /// </summary>
        private static float ZoomToFontRatioLow { get { return 1.5f; } }         // optimálně 1.5f
        /// <summary>
        /// Přepočtová konstanta mezi lineárním zoomem a velikostí písma, určuje poměr zvětšení písma při zvětšení Zoomu, <br/>
        /// <u>pro oblast Zoomu VĚTŠÍ než 100 (Linear).</u><br/>
        /// Čím menší hodnota, tím menší vliv má změna Zoomu na změnu velikosti písma.
        /// Větší hodnota <see cref="ZoomToFontRatioHigh"/> způsobí větší změny velikosti písma při změně Zoomu.
        /// <para/>
        /// Hodnota 3 dává vcelku dobrou reakci, <br/>
        /// hodnota 5 dává přiměřenou reakci (písmo se zvětšuje úměrně motivu).<br/>
        /// hodnota 8 dává velkou reakci (při zvětšování motivu se písmo zvětšuje rychle, zbytečně vyplňuje prostor a to není účelem Zoomu).<br/>
        /// hodnota 1 dává poměrně menší písmena při hodně zvětšených objektech = nepřirozeně malé písmo (pomalá reakce na Zoom).<br/>
        /// hodnota 0 nereaguje na Zoom.
        /// </summary>
        private static float ZoomToFontRatioHigh { get { return 4.0f; } }         // optimálně 4.0f
        #endregion
    }
    /// <summary>
    /// Bázová třída pro objekty, reprezentující prvky umístěné ve virtuálním controlu <see cref="VirtualControl"/>, spolupracuje s jeho virtuálním prostorem <see cref="VirtualSpace"/>.
    /// </summary>
    public class VirtualItemBase : IVisualItem, IDisposable
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="virtualSpace"></param>
        public VirtualItemBase(VirtualControl virtualControl)
        {
            VirtualHost = virtualControl;
            VirtualBounds = new BoundsF();
        }
        /// <summary>
        /// Hostitelský control
        /// </summary>
        protected VirtualControl VirtualHost { get; private set; }
        /// <summary>
        /// Koordinátor virtuálních souřadnic
        /// </summary>
        protected VirtualSpace VirtualSpace { get { return VirtualHost.VirtualSpace; } }
        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            VirtualHost = null;
            VirtualBounds = null;
        }
        /// <summary>
        /// Invaliduje hostitelský Control
        /// </summary>
        protected virtual void InvalidateControl()
        {
            VirtualHost?.Invalidate();
        }
        #endregion
        #region Virtuální pozice
        /// <summary>
        /// Virtuální souřadnice vztažené k celé pracovní ploše. 
        /// Tuto hodnotu nelze setovat, ale lze nastavovat její veškeré hodnoty
        /// </summary>
        public virtual BoundsF VirtualBounds { get; private set; }
        #endregion
        #region Podpora pro interface IVisualItem - ale nikoli jeho plná implementace
        /// <summary>
        /// Vrstva prvku: čím vyšší hodnota, tím vyšší vrstva = prvek bude kreslen "nad" prvky s nižší vrstvou, a stejně tak bude i aktivní.
        /// Defaultní je 0.
        /// </summary>
        public virtual int Layer { get { return 0; } }
        /// <summary>
        /// Aktuální [Logaritmický] Zoom (= pro nativní přepočet velikosti Fyzická = Zoom * Virtuální)
        /// </summary>
        public float Zoom { get { return (float)VirtualSpace.Zoom; } }
        /// <summary>
        /// Aktuální [Lineární] Zoom (= pro jiné výpočty založené na Zoomu)
        /// </summary>
        public float ZoomLinear { get { return (float)VirtualSpace.ZoomLinear; } }
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
        /// Sem je nastaveno true/false v okamžiku vyhodnocené viditelnosti prvku.
        /// </summary>
        public bool CurrentlyIsVisible { get; set; }
        /// <summary>
        /// Vrátí true, pokud daný fyzický bod leží na fyzických souřadnicích tohoto prvku ve viditelné oblasti Ownera
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        public virtual bool IsVisibleOnCurrentPoint(Point currentPoint)
        {
            var currentVisibleBounds = VirtualSpace.GetCurrentVisibleBounds(VirtualBounds);
            return (currentVisibleBounds.HasValue && currentVisibleBounds.Value.Contains(currentPoint));
        }
        public virtual bool IsActiveOnCurrentPoint(Point currentPoint)
        {
            var currentBounds = this.CurrentVisibleBounds;
            return (currentBounds.HasValue && currentBounds.Value.Contains(currentPoint));
        }

        /// <summary>
        /// Stav prvku z hlediska myši
        /// </summary>
        public ItemMouseState MouseState
        {
            get { return _MouseState; }
            set
            {
                if (value != _MouseState)
                {
                    _MouseState = value;
                    InvalidateControl();
                }
            }
        }
        private ItemMouseState _MouseState;
        /// <summary>
        /// Prvek je Selectován
        /// </summary>
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                if (value != _Selected)
                {
                    _Selected = value;
                    InvalidateControl();
                }
            }
        }
        private bool _Selected;
        /// <summary>
        /// Aktuální barva orámování, vychází z hodnot <see cref="Selected"/> a <see cref="MouseState"/>
        /// </summary>
        public virtual Color? CurrentOutlineColor { get { return VirtualHost.GetOutlineColor(this); } }
        /// <summary>
        /// Vykreslí sebe jako prvek do grafiky
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnPaintItem(PaintEventArgs e) { }
        /// <summary>
        /// Vykreslí svoje linky, pokud dosud nejsou v <paramref name="paintedLinks"/>.
        /// Vykreslené linky přidává do dictionary - aby se nekreslily opakovaně (tj. z druhé strany).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="paintedLinks"></param>
        public virtual void OnPaintLinks(PaintEventArgs e, Dictionary<long, MapLink> paintedLinks) { }
        #endregion
    }
    /// <summary>
    /// Virtuální prostor - výpočetní mechanismus pro oboustranný přepočet Virtuální - Fyzická souřadnice;
    /// včetně jeho řízení pomocí myši
    /// </summary>
    public class VirtualSpace : IDisposable
    {
        #region Konstruktor, proměnné, základní hodnoty
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        internal VirtualSpace(VirtualControl owner)
        {
            _Owner = owner;
            _CoordInit();
            _MouseInit();
        }
        /// <summary>
        /// Disposee
        /// </summary>
        public void Dispose()
        {
            _MouseDispose();
            _Owner = null;
        }
        /// <summary>
        /// Owner
        /// </summary>
        private VirtualControl _Owner;
        /// <summary>
        /// Kurzor v Owner controlu, lze setovat
        /// </summary>
        private Cursor OwnerCursor { get { return _Owner?.Cursor; } set { if (_Owner != null) _Owner.Cursor = value; } }
        /// <summary>
        /// Fyzické souřadnice controlu, do kterých je vykreslován obsah = <see cref="Control.ClientRectangle"/> našeho vlastníka
        /// </summary>
        public Rectangle OwnerClientRectangle { get { return this._Owner.ClientRectangle; } }
        /// <summary>
        /// Souřadnice středu viditelné oblasti v Owneru ( v <see cref="OwnerClientRectangle"/> )
        /// </summary>
        private PointF CurrentClientCenter { get { SizeF s = OwnerClientRectangle.Size; return new PointF(s.Width / 2f, s.Height / 2f); } }
        /// <summary>
        /// Invaliduje Ownera
        /// </summary>
        protected void OwnerInvalidate()
        {
            _Owner?.RefreshContent();
        }
        #endregion
        #region Virtuální a fyzické souřadnice
        /// <summary>
        /// Inicializace koordinátového systému
        /// </summary>
        private void _CoordInit()
        {
            _Zoom = 1d;
            _ZoomF = 1f;
            _CurrentZeroMouseShift = 30;
            _ZoomMouseStep = 5d;
            _CurrentZeroX = 0f;
            _CurrentZeroY = 0f;
        }
        /// <summary>
        /// Vrátí virtuální oblast odpovídající dané fyzické (pixelové) oblasti.
        /// </summary>
        /// <param name="currentRectangle"></param>
        /// <returns></returns>
        public RectangleF GetVirtualRectangle(RectangleF currentRectangle)
        {
            var zoom = _ZoomF;
            return new RectangleF((currentRectangle.X - _CurrentZeroX) / zoom, (currentRectangle.Y - _CurrentZeroY) / zoom, currentRectangle.Width / zoom, currentRectangle.Height / zoom);
        }
        /// <summary>
        /// Vrátí fyzickou (pixelovou) oblast odpovídající dané virtuální oblasti.
        /// </summary>
        /// <param name="virtualRectangle"></param>
        /// <returns></returns>
        public RectangleF GetCurrentRectangle(RectangleF virtualRectangle)
        {
            var zoom = _ZoomF;
            return new RectangleF(_CurrentZeroX + (virtualRectangle.X * zoom), _CurrentZeroY + (virtualRectangle.Y * zoom), virtualRectangle.Width * zoom, virtualRectangle.Height * zoom);
        }
        /// <summary>
        /// Vrátí virtuální souřadnici odpovídající dané fyzické (pixelové) souřadnici.
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        public PointF GetVirtualPoint(PointF currentPoint)
        {
            var zoom = _ZoomF;
            return new PointF((currentPoint.X - _CurrentZeroX) / zoom, (currentPoint.Y - _CurrentZeroY) / zoom);
        }
        /// <summary>
        /// Vrátí fyzickou (pixelovou) souřadnici odpovídající dané virtuální souřadnici.
        /// </summary>
        /// <param name="virtualPoint"></param>
        /// <returns></returns>
        public PointF GetCurrentPoint(PointF virtualPoint)
        {
            var zoom = _ZoomF;
            return new PointF(_CurrentZeroX + (virtualPoint.X * zoom), _CurrentZeroY + (virtualPoint.Y * zoom));
        }
        /// <summary>
        /// Vrátí virtuální velikost odpovídající dané fyzické (pixelové) velikosti.
        /// </summary>
        /// <param name="currentSize"></param>
        /// <returns></returns>
        public SizeF GetVirtualSize(SizeF currentSize)
        {
            var zoom = _ZoomF;
            return new SizeF(currentSize.Width / zoom, currentSize.Height / zoom);
        }
        /// <summary>
        /// Vrátí fyzickou (pixelovou) velikost odpovídající dané virtuální velikosti.
        /// </summary>
        /// <param name="virtualSize"></param>
        /// <returns></returns>
        public SizeF GetCurrentSize(SizeF virtualSize)
        {
            var zoom = _ZoomF;
            return new SizeF(virtualSize.Width * zoom, virtualSize.Height * zoom);
        }
       
        private void _RunCoordinateChanged()
        {
            OwnerInvalidate();
            OnCoordinateChanged();
            CoordinateChanged?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnCoordinateChanged() { }
        /// <summary>
        /// Událost, kterou <see cref="VirtualSpace"/> vyvolá po každé změně souřadného systému = po změně <see cref="Zoom"/> i po změně koordinátů 
        /// </summary>
        public event EventHandler CoordinateChanged;

        #endregion
        #region Určení fyzických souřadnic v rámci prostoru Ownera včetně průniku a viditelnosti
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
            Rectangle ownerBounds = this.OwnerClientRectangle;
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
            return Rectangle.Round(GetCurrentRectangle(virtualBounds.RectangleF));
        }
        #endregion
        #region Souřadnice počátku
        /// <summary>
        /// Souřadnice počátku = toho bodu, kde je na viditelném controlu zobrazována virtuální souřadnice { 0/0 }.
        /// Pokud máme dán virtuální bod { X=20, Y=10 } a <see cref="Zoom"/> = 2.0f, a <see cref="CurrentZero"/> = { X=50, Y=16 };
        /// pak tento bod bude promítnut do viditelného controlu do jeho souřadnice: { X = (50 + 2.0f * 20) = 90px, Y = (16 + 2.0f * 10) = 36px }.
        /// </summary>
        public PointF CurrentZero 
        {
            get { return new PointF(_CurrentZeroX, _CurrentZeroY); }
            set 
            {
                if (_CurrentZeroX == value.X && _CurrentZeroY == value.Y) return;
                _CurrentZeroX = value.X;
                _CurrentZeroY = value.Y;
                _RunCoordinateChanged();
            }
        }
        /// <summary>
        /// Krok posunu pomocí kolečka myši, ve fyzických pixelech.
        /// Výchozí hodnota = 30; přípustný rozsah hodnot: 10 až 400.
        /// </summary>
        public int CurrentZeroMouseShift
        {
            get { return _CurrentZeroMouseShift; }
            set { _CurrentZeroMouseShift = Align(value, 10, 400); }
        }
        /// <summary>
        /// Krok posunu pomocí kolečka myši, ve fyzických pixelech
        /// </summary>
        private int _CurrentZeroMouseShift;
        /// <summary>
        /// Fyzické (pixelové) souřadnice toho bodu, kde je na viditelném controlu zobrazována virtuální souřadnice { 0/0 }, v ose X.
        /// Pokud máme dán virtuální bod { X=20, Y=10 } a <see cref="Zoom"/> = 2.0f, a <see cref="_CurrentZeroX"/> = 50;
        /// pak tento bod bude promítnut do viditelného controlu do jeho souřadnice: (50 + 2.0f * 20) = 90px.
        /// </summary>
        private float _CurrentZeroX;
        /// <summary>
        /// Fyzické (pixelové) souřadnice toho bodu, kde je na viditelném controlu zobrazována virtuální souřadnice { 0/0 }, v ose Y.
        /// Pokud máme dán virtuální bod { X=20, Y=10 } a <see cref="Zoom"/> = 2.0f, a <see cref="_CurrentZeroY"/> = 16;
        /// pak tento bod bude promítnut do viditelného controlu do jeho souřadnice: (16 + 2.0f * 10) = 36px.
        /// </summary>
        private float _CurrentZeroY;
        #endregion
        #region Zoom nativní i lineární
        /// <summary>
        /// Aktuální Zoom, výchozí = 1f. Nikdy není nula ani záporné. <br/>
        /// Menší hodnota = menší objekty, např. <see cref="Zoom"/> = 0.5f způsobí, že objekt s virtuální šířkou 100 bude mít vizuální šířku 50px.<br/>
        /// Větší hodnota = větší objekty, např. <see cref="Zoom"/> = 3.0f způsobí, že objekt s virtuální šířkou 100 bude mít vizuální šířku 300px.
        /// <para/>
        /// Reálně se pohybuje mezi <see cref="ZoomMin"/> (0.01f) až <see cref="ZoomMax"/> (100f).
        /// Při vložení hodnoty mimo daný rozsah je akceptovaný <see cref="Zoom"/> do tohoto rozmezí zarovnán.
        /// Po změně hodnoty se volá event <see cref="CoordinateChanged"/>.
        /// Při změně Zoomu pomocí této property se nemění pozice středu aktuálně viditelné oblasti = stejné jako změna Zoomu na teleobjektivu.
        /// </summary>
        public double Zoom
        {
            get { return _Zoom; }
            set { _SetZoom(CurrentClientCenter, value, true); }
        }
        /// <summary>
        /// Aktuální Zoom na lineární stupnici, výchozí = 100f. Nikdy není záporné. <br/>
        /// Rozsah této hodnoty je 0 až 200, kde 100 odpovídá měřítku 1:1, tedy hodnotě <see cref="Zoom"/> = 1f.
        /// Posun této hodnoty o stejné číslo, např. o 10 : z 50 na 60 způsobí opticky podobnou změnu zoomu jako při změně o stejných 10 z 150 na 160.
        /// Hodnotu <see cref="Zoom"/> je třeba měnit logaritmicky, kdežto <see cref="ZoomLinear"/> se mění lineárně.
        /// <para/>
        /// Reálně se pohybuje mezi <see cref="ZoomLinearMin"/> (0f) až <see cref="ZoomLinearMax"/> (200f).
        /// Při vložení hodnoty mimo daný rozsah je akceptovaný <see cref="ZoomLinear"/> do tohoto rozmezí zarovnán.
        /// Po změně hodnoty se volá event <see cref="CoordinateChanged"/>.
        /// Při změně Zoomu pomocí této property se nemění pozice středu aktuálně viditelné oblasti = stejné jako změna Zoomu na teleobjektivu.
        /// </summary>
        public double ZoomLinear
        {
            get { return GetLinearZoom(_Zoom); }
            set { _SetZoom(CurrentClientCenter, GetNativeZoom(value), true); }
        }
        /// <summary>
        /// Lineární krok změny Zoomu při otáčení Ctrl+Mouse, v rozsahu 2 - 20, výchozí = 5
        /// </summary>
        public double ZoomMouseStep
        {
            get { return _ZoomMouseStep; }
            set { _ZoomMouseStep = (value < 2d ? 2d : (value > 20d ? 20d : value)); }
        }
        private double _ZoomMouseStep;
        /// <summary>
        /// Vrací lineární hodnotu Zoomu pro danou hodnotu nativní (logaritmickou)
        /// </summary>
        /// <param name="nativeZoom">Nativní Zoom, v rozsahu 0.01 - 1.00 - 100.00 </param>
        /// <returns></returns>
        private static double GetLinearZoom(double nativeZoom)
        {
            nativeZoom = GetAlignedNativeZoom(nativeZoom);
            return Math.Log(nativeZoom * ConvertZoomDiv) * ConvertZoomExp;
        }
        /// <summary>
        /// Vrací nativní (logaritmickou) hodnotu Zoomu pro danou hodnotu lineární
        /// </summary>
        /// <param name="linearZoom">Lineární Zoom, v rozsahu 0 - 100 - 200 </param>
        /// <returns></returns>
        private static double GetNativeZoom(double linearZoom)
        {
            linearZoom = GetAlignedLinearZoom(linearZoom);
            return Math.Exp(linearZoom / ConvertZoomExp) / ConvertZoomDiv;
        }
        private const double ConvertZoomExp = 21.71472409516d;
        private const double ConvertZoomDiv = 100.0d;
        #region Matematika přepočtu Lineární <=> Logaritmický Zoom
        /*
        Lineární hodnoty     :  0      50      100      150        200
        Logaritmické hodnoty : 0.010  0.100   1.000    10.000     100.000
        Vzorec Lin => Log    : Logaritmic = ( Exp ( Linear / 21.71472409516d ) / 100d )
        Vzore  Log => Lin    : Linear     = ( Log ( Logaritmic * 100 ) * 21.71472409516d )

Linear	Log
0	0,010
5	0,013
10	0,016
15	0,020
20	0,025
25	0,032
30	0,040
35	0,050
40	0,063
45	0,079
50	0,100
55	0,126
60	0,158
65	0,200
70	0,251
75	0,316
80	0,398
85	0,501
90	0,631
95	0,794
100	1,000
105	1,259
110	1,585
115	1,995
120	2,512
125	3,162
130	3,981
135	5,012
140	6,310
145	7,943
150	10,000
155	12,589
160	15,849
165	19,953
170	25,119
175	31,623
180	39,811
185	50,119
190	63,096
195	79,433
200	100,000
        */
        #endregion

        /// <summary>
        /// Zarovná a vrátí nativní (logaritmický) Zoom do povolených hranic, které jsou <see cref="ZoomMin"/> a <see cref="ZoomMax"/>
        /// </summary>
        /// <param name="nativeZoom"></param>
        /// <returns></returns>
        private static double GetAlignedNativeZoom(double nativeZoom) { return Align(nativeZoom, ZoomMin, ZoomMax); }
        /// <summary>
        /// Zarovná a vrátí lineární Zoom do povolených hranic, které jsou <see cref="ZoomMin"/> a <see cref="ZoomMax"/>
        /// </summary>
        /// <param name="linearZoom"></param>
        /// <returns></returns>
        private static double GetAlignedLinearZoom(double linearZoom) { return Align(linearZoom, ZoomLinearMin, ZoomLinearMax); }
        /// <summary>
        /// Vrátí danou hodnotu zarovnanou do daných mezí
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static T Align<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (min.CompareTo(max) > 0) throw new ArgumentException($"Align() error: Min {min} musí být menší než Max {max} !");
            if (value.CompareTo(min) < 0) value = min;
            if (value.CompareTo(max) > 0) value = max;
            return value;
        }
        /// <summary>
        /// Nastaví Zoom, při změně hodnoty volitelně volá <see cref="_RunCoordinateChanged"/>, vrací příznak změny
        /// </summary>
        /// <param name="fixedCurrentCenter">Bod středu (jehož obsah se nemá měnit), ve fyzických pixelových hodnotách, například <see cref="CurrentClientCenter"/></param>
        /// <param name="zoom">Nová hodnota Zoomu</param>
        /// <param name="callEvent">Volat událost <see cref="CoordinateChanged"/></param>
        /// <returns></returns>
        private void _SetZoom(PointF fixedCurrentCenter, double zoom, bool callEvent)
        {
            zoom = GetAlignedNativeZoom(zoom);
            if (zoom == _Zoom) return;

            var fixedVirtualPoint = GetVirtualPoint(fixedCurrentCenter);

            _Zoom = zoom;
            _ZoomF = (float)zoom;

            var shiftedCurrentCenter = GetCurrentPoint(fixedVirtualPoint);
            _CurrentZeroX = _CurrentZeroX - (shiftedCurrentCenter.X - fixedCurrentCenter.X);
            _CurrentZeroY = _CurrentZeroY - (shiftedCurrentCenter.Y - fixedCurrentCenter.Y);

            if (callEvent)
                _RunCoordinateChanged();
        }
        /// <summary>
        /// Minimální Zoom nativní (logaritmický)
        /// </summary>
        public const double ZoomMin = 0.01d;
        /// <summary>
        /// Maximální Zoom nativní (logaritmický)
        /// </summary>
        public const double ZoomMax = 100.0d;
        /// <summary>
        /// Minimální Zoom lineární
        /// </summary>
        public const double ZoomLinearMin = 0d;
        /// <summary>
        /// Maximální Zoom lineární
        /// </summary>
        public const double ZoomLinearMax = 200.0d;
        /// <summary>
        /// Aktuální Zoom, výchozí = 1d, nikdy není nula ani záporné. Reálně se pohybuje mezi 0.01 až 100.
        /// </summary>
        private double _Zoom;
        /// <summary>
        /// Aktuální Zoom, výchozí = 1f, nikdy není nula ani záporné. Reálně se pohybuje mezi 0.01 až 100.
        /// </summary>
        private float _ZoomF;
        #endregion
        #region Myší interaktivita - primární eventhandlery
        /// <summary>
        /// Napojení událostí myši na Owneru do zdejších handlerů
        /// </summary>
        private void _MouseInit()
        {
            VirtualControl owner = _Owner;
            if (owner != null)
            {
                owner.MouseMove += _MouseMove;
                owner.MouseDown += _MouseDown;
                owner.MouseUp += _MouseUp;
                owner.MouseWheel += _MouseWheel;
            }
        }
        /// <summary>
        /// Napojení událostí myši na Owneru od zdejších handlerů
        /// </summary>
        private void _MouseDispose()
        {
            VirtualControl owner = _Owner;
            if (owner != null)
            {
                owner.MouseDown -= _MouseDown;
                owner.MouseMove -= _MouseMove;
                owner.MouseUp -= _MouseUp;
                owner.MouseWheel -= _MouseWheel;
            }
        }
        /// <summary>
        /// Událost: uživatel zmáčkl myš
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            MouseDragDone = false;
            if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Alt)
                _MouseDragInit(e.Location);
        }
        /// <summary>
        /// Událost: uživatel pohnul myší
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            MouseDragDone = false;
            if (MouseDragActive) // e.Button == MouseButtons.Left && MouseDownPoint.HasValue)
            {
                var point = e.Location;
                if (e.Button == MouseButtons.None)
                {   // Chybějící MouseUp (typicky v Debugování):
                    _MouseDragEnd(point, false);
                }
                else
                {
                    if (MouseDownSilentBounds.HasValue)
                    {   // Čekáme na překročení hranice "tichého prostoru":
                        if (!MouseDownSilentBounds.Value.Contains(point))
                            _MouseDragBegin(point);
                    }
                    if (MouseDragProcess)
                        _MouseDragMove(point);
                }
            }
        }
        /// <summary>
        /// Událost: uživatel zvedl myš
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            // Tady nesmím měnit MouseDragDone !!!
            if (MouseDragActive)
                _MouseDragEnd(e.Location, true);
        }
        /// <summary>
        /// Událost: uživatel točí kolečkem myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.Alt)
                MouseWheelZoom(e.Location, e.Delta);
            else
                MouseWheelMove(e.Location, e.Delta);
        }
        #endregion
        #region Přesouvání prostoru pomocí myši
        /// <summary>
        /// Obsahuje true v době, kdy je aktivní MouseDrag = počínaje zaájením procesu Drag (po stisku levé myši s klávesou Alt a po opuštění výchozího bodu), 
        /// až do zvednutí myši včetně = tedy i v procesech MouseUp v uživatelském Controlu.
        /// <para/>
        /// Na false se změní po nejbližším dalším pohybu myši.
        /// </summary>
        public bool IsMouseDrag { get { return MouseDragProcess || MouseDragDone; } }
        /// <summary>
        /// Vyvolá metodu <see cref="OnMouseDragBegin"/> a event <see cref="MouseDragBegin"/>
        /// </summary>
        private void _RunMouseDragBegin()
        {
            OnMouseDragBegin();
            MouseDragBegin?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Provede se při zahájení procesu Drag
        /// </summary>
        protected virtual void OnMouseDragBegin() { }
        /// <summary>
        /// Proběhne při zahájení procesu Drag
        /// </summary>
        public event EventHandler MouseDragBegin;
        /// <summary>
        /// Vyvolá metodu <see cref="OnMouseDragEnd"/> a event <see cref="MouseDragEnd"/>
        /// </summary>
        private void _RunMouseDragEnd()
        {
            OnMouseDragEnd();
            MouseDragEnd?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Provede se při ukončení procesu Drag poté, kdy byl zahájen
        /// </summary>
        protected virtual void OnMouseDragEnd() { }
        /// <summary>
        /// Proběhne při ukončení procesu Drag poté, kdy byl zahájen
        /// </summary>
        public event EventHandler MouseDragEnd;
        /// <summary>
        /// Akce: uživatel zmáčkl levou myš při klávese Alt: mohl by začít přetahovat prostor
        /// </summary>
        /// <param name="point"></param>
        private void _MouseDragInit(Point point)
        {
            MouseDragActive = true;
            Size size = SilentAreaSize;
            MouseDownPoint = point;
            Point location = new Point(point.X - size.Width / 2, point.Y - size.Height / 2);
            MouseDownSilentBounds = new Rectangle(location, size);
        }
        /// <summary>
        /// Akce: uživatel má zmáčknutou levou myš s klávesou Alt a pohnul myší: začíná proces přetahování prostoru. 
        /// Tady neřešíme vlastní pohyb, ale jeho začátek.
        /// </summary>
        /// <param name="point"></param>
        private void _MouseDragBegin(Point point)
        {
            MouseDownSilentBounds = null;
            MouseDragProcess = true;
            OwnerCursorBeforeDrag = OwnerCursor;
            OwnerCursor = Cursors.Hand;
            MouseDragZero = new PointF(_CurrentZeroX, _CurrentZeroY);
            _RunMouseDragBegin();
        }
        /// <summary>
        /// Akce: uživatel přetahuje prostor
        /// </summary>
        /// <param name="point"></param>
        private void _MouseDragMove(Point point)
        {
            float currentZeroX = MouseDragZero.Value.X + (float)(point.X - MouseDownPoint.Value.X);
            float currentZeroY = MouseDragZero.Value.Y + (float)(point.Y - MouseDownPoint.Value.Y);
            if (currentZeroX == _CurrentZeroX && currentZeroY == _CurrentZeroY) return;
            _CurrentZeroX = currentZeroX;
            _CurrentZeroY = currentZeroY;
            _RunCoordinateChanged();
        }
        /// <summary>
        /// Akce: uživatel uvolnil myš a ukončuje přetahování prostoru
        /// </summary>
        /// <param name="point"></param>
        /// <param name="isMouseUp"></param>
        private void _MouseDragEnd(Point point, bool isMouseUp)
        {
            if (MouseDragProcess) _RunMouseDragEnd();

            if (OwnerCursorBeforeDrag != null)
            {
                OwnerCursor = OwnerCursorBeforeDrag;
                OwnerCursorBeforeDrag = null;
            }
            MouseDownSilentBounds = null;
            MouseDownPoint = null;
            MouseDragZero = null;
            MouseDragDone = (isMouseUp ? MouseDragProcess : false);
            MouseDragProcess = false;
            MouseDragActive = false;
        }
        /// <summary>
        /// Měli bychom měnit Zoom podle otáčení kolečkem myši, nad daným bodem v daném směru
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="delta"></param>
        private void MouseWheelZoom(Point currentPoint, int delta)
        {
            double oldZoomNative = this.Zoom;
            double zoomStep = this.ZoomMouseStep;
            double oldZoomLinear = this.ZoomLinear;
            double newZoomLinear = GetAlignedLinearZoom(oldZoomLinear + (delta < 0 ? -zoomStep : zoomStep));
            double newZoomNative = GetNativeZoom(newZoomLinear);
            if (newZoomNative == oldZoomNative) return;

            this._SetZoom(currentPoint, newZoomNative, true);
        }
        /// <summary>
        /// Měli bychom měnit pozici souřadného systému podle otáčení kolečkem myši, nad daným bodem v daném směru
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="delta"></param>
        private void MouseWheelMove(Point currentPoint, int delta)
        {
            float step = (delta < 0 ? -_CurrentZeroMouseShift : _CurrentZeroMouseShift);
            _CurrentZeroY = _CurrentZeroY + step;
            _RunCoordinateChanged();
        }
        /// <summary>
        /// Bod kde byla stisknuta myš. 
        /// Má hodnotu počínaje stiskem levé myši při klávese Alt, po celou dobu Drag, až do MouseUp.
        /// </summary>
        private Point? MouseDownPoint;
        /// <summary>
        /// Souřadnice okolo bodu <see cref="MouseDownPoint"/>, v nichž nebudeme provádět Drag (=malá zóna kolem bodu, aby Drag nastal až po větším pohybu).
        /// Má hodnotu počínaje stiskem levé myši při klávese Alt, jen po dobu očekávání pohybu. Při detekci většího pohybu začíná Drag a tato hodnota je nulována.
        /// </summary>
        private Rectangle? MouseDownSilentBounds;
        /// <summary>
        /// Obsahuje true v době, kdy je / může být aktivní MouseDrag = po stisku levé myši s klávesou Alt, okamžitě, až do zvednutí myši (tj. i v procesu čekání na začátku).
        /// </summary>
        private bool MouseDragActive;
        /// <summary>
        /// Obsahuje true v době, kdy probíhá MouseDrag = po stisku levé myši s klávesou Alt, a po opuštění prostoru <see cref="MouseDownSilentBounds"/>, do zvednutí myši.
        /// </summary>
        private bool MouseDragProcess;
        /// <summary>
        /// Souřadnice <see cref="_CurrentZeroX"/>, <see cref="_CurrentZeroY"/> platné v době počátku procesu Drag, 
        /// slouží k určení postupně posouvaných souřadnic v procesu Drag
        /// </summary>
        private PointF? MouseDragZero;
        /// <summary>
        /// Obsahuje true v době, kdy probíhá MouseUp, který ale právě teď dokončil proces MouseDrag.
        /// Význam má tato property pro uživatelský Control, který si ze svých vlastních důvodů hlídá event MouseUp.
        /// Pokud v události MouseUp je aktivní příznak <see cref="VirtualSpace.WasMouseDrag"/> (anebo <see cref="VirtualSpace.IsMouseDrag"/>), 
        /// pak by uživatelský Control neměl provádět svoji aktivní reakci na MouseUp, protože tato akce má význam "Končí Drag" a nikoli MouseClick.
        /// <para/>
        /// Nejbližší následující pohyb nebo jiná akce myši tento příznak shodí na false.
        /// </summary>
        private bool MouseDragDone;
        /// <summary>
        /// Kurzor vlastníka před tím, než začneme proces Drag
        /// </summary>
        private Cursor OwnerCursorBeforeDrag;
        /// <summary>
        /// Velikost oblasti <see cref="MouseDownSilentBounds"/>, je daná systémem
        /// </summary>
        private static Size SilentAreaSize
        {
            get
            {
                if (!_SilentAreaSize.HasValue)
                    _SilentAreaSize = System.Windows.Forms.SystemInformation.DragSize;
                return _SilentAreaSize.Value;
            }
        }
        private static Size? _SilentAreaSize;
        #endregion
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
    #region interface IVisualItem : Obecný interface pro grafický prvek, enum ItemMouseState
    /// <summary>
    /// Obecný interface pro grafický prvek
    /// </summary>
    public interface IVisualItem
    {
        /// <summary>
        /// Vrstva prvku: čím vyšší hodnota, tím vyšší vrstva = prvek bude kreslen "nad" prvky s nižší vrstvou, a stejně tak bude i aktivní
        /// </summary>
        int Layer { get; }
        /// <summary>
        /// Aktuální [Logaritmický] Zoom (= pro nativní přepočet velikosti Fyzická = Zoom * Virtuální)
        /// </summary>
        float Zoom { get; }
        /// <summary>
        /// Fyzické souřadnice celého prvku v aktuálním controlu.
        /// </summary>
        Rectangle CurrentBounds { get; }
        /// <summary>
        /// Fyzické souřadnice viditelné části prvku v aktuálním controlu, nebo null když prvek není viditelný.
        /// Tedy tyto souřadnice jsou oříznuty do viditelné oblasti
        /// </summary>
        Rectangle? CurrentVisibleBounds { get; }
        /// <summary>
        /// Je prvek aktuálně viditelný v rámci svého Owner controlu ?
        /// </summary>
        bool IsVisibleInOwner { get; }
        /// <summary>
        /// Sem je nastaveno true/false v okamžiku vyhodnocené viditelnosti prvku.
        /// </summary>
        bool CurrentlyIsVisible { get; set; }
        /// <summary>
        /// Je prvek aktivní na dané fyzické souřadnici?
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        bool IsActiveOnCurrentPoint(Point currentPoint);
        /// <summary>
        /// Stav prvku z hlediska myši
        /// </summary>
        ItemMouseState MouseState { get; set; }
        /// <summary>
        /// Prvek je Selectován
        /// </summary>
        bool Selected { get; set; }
        /// <summary>
        /// Aktuální barva orámování, vychází z hodnot <see cref="Selected"/> a <see cref="MouseState"/>
        /// </summary>
        Color? CurrentOutlineColor { get; }
        /// <summary>
        /// Vykreslí sebe jako prvek do grafiky
        /// </summary>
        /// <param name="e"></param>
        void OnPaintItem(PaintEventArgs e);
        /// <summary>
        /// Vykreslí svoje linky, pokud dosud nejsou v <paramref name="paintedLinks"/>.
        /// Vykreslené linky přidává do dictionary - aby se nekreslily opakovaně (tj. z druhé strany).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="paintedLinks"></param>
        void OnPaintLinks(PaintEventArgs e, Dictionary<long, MapLink> paintedLinks);
    }
    /// <summary>
    /// Stav prvku z hlediska myši
    /// </summary>
    public enum ItemMouseState
    {
        /// <summary>
        /// Myš není na prvku
        /// </summary>
        None,
        /// <summary>
        /// Myš je nad prvkem
        /// </summary>
        OnMouse,
        /// <summary>
        /// Myš má zmáčknuté levé ouško
        /// </summary>
        LeftDown,
        /// <summary>
        /// Myš má zmáčknuté pravé ouško
        /// </summary>
        RightDown
    }
    #endregion

}
