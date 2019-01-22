using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Kontroler pro tvorbu, zobrazení a řízení prvků <see cref="ResizeItem"/>, které zajišťují interaktivní změnu velikosti jakéhokoli prvku <see cref="IInteractiveItem"/>.
    /// </summary>
    public class ResizeControl
    {
        #region Konstruktor a privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="interactiveOwner"></param>
        public ResizeControl(InteractiveObject interactiveOwner)
            :this(interactiveOwner, RectangleSide.None)
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="interactiveOwner"></param>
        /// <param name="resizeSides"></param>
        public ResizeControl(InteractiveObject interactiveOwner, RectangleSide resizeSides)
        {
            this._InteractiveOwner = interactiveOwner;
            this._IResizeObject = interactiveOwner as IResizeObject;
            this._ShowResizeAllways = false;
            this._CanUpsideDown = false;
            this._ChildList = new List<ResizeItem>();
            this._InactiveColor = Skin.Splitter.InactiveColor;
            this._MouseOnParentColor = Skin.Splitter.MouseOnParentColor;
            this._MouseOverColor = Skin.Splitter.MouseOverColor;
            this._MouseDownColor = Skin.Splitter.MouseDownColor;

            this.ResizeSides = resizeSides;
        }
        private InteractiveObject _InteractiveOwner;
        private IResizeObject _IResizeObject;
        private RectangleSide _ResizeSides;
        private bool _ShowResizeAllways;
        private bool _CanUpsideDown;
        private Color _InactiveColor;
        private Color _MouseOnParentColor;
        private Color _MouseOverColor;
        private Color _MouseDownColor;
        #endregion
        #region Public properties
        /// <summary>
        /// Strany, kde se bude nabízet možnost Resize. Výchozí hodnota je None = žádná strana, pokud není použit konstruktor s parametrem <see cref="RectangleSide"/>.
        /// </summary>
        public RectangleSide ResizeSides { get { return _ResizeSides; } set { _SetResizeSides(value); } }
        /// <summary>
        /// Obsahuje true = Zobrazovat prvky Resize vždy (tj. i bez přítomnosti myši); false (default) = bez myši nezobrazovat, pouze po najetí myši na control
        /// </summary>
        public bool ShowResizeAllways { get { return _ShowResizeAllways; } set { _ShowResizeAllways = value; this.Repaint(); } }
        /// <summary>
        /// Obsahuje true = při přemísťování hrany prvku může tato hrana přejít přes protilehlou hranu, pak dojde k převrácení prvku naruby.
        /// Default = false, tzn. nejmenší výška a šířka prvku je jeden pixel.
        /// </summary>
        public bool CanUpsideDown { get { return _CanUpsideDown; } set { _CanUpsideDown = value; } }
        /// <summary>
        /// Barva prvků v neaktivním stavu
        /// </summary>
        public Color InactiveColor { get { return _InactiveColor; } set { _InactiveColor = value; this.Repaint(); } }
        /// <summary>
        /// Barva prvků v stavu, kdy parent má myš
        /// </summary>
        public Color MouseOnParentColor { get { return _MouseOnParentColor; } set { _MouseOnParentColor = value; this.Repaint(); } }
        /// <summary>
        /// Barva prvků v stavu, kdy prvek sám má myš, ale není zmáčknutá
        /// </summary>
        public Color MouseOverColor { get { return _MouseOverColor; } set { _MouseOverColor = value; this.Repaint(); } }
        /// <summary>
        /// Barva prvků v stavu, kdy prvek sám má myš, a tato je zmáčknutá
        /// </summary>
        public Color MouseDownColor { get { return _MouseDownColor; } set { _MouseDownColor = value; this.Repaint(); } }
        /// <summary>
        /// Pole vizuálních potomků. Odpovídá volbě v <see cref="ResizeSides"/>. 
        /// Vizuální parent jej může rovnou vracet jako svoje Childs, anebo je může ke svým Childs přidat (na konec).
        /// </summary>
        public IEnumerable<IInteractiveItem> Childs { get { return this._ChildList; } }
        /// <summary>
        /// true pokud this obsahuje nějaké <see cref="Childs"/>.
        /// </summary>
        public bool HasChilds { get { return (this._ChildList.Count > 0); } }
        /// <summary>
        /// Zajistí repaint pro hlavní vizuální prvek
        /// </summary>
        public void Repaint()
        {
            ((IInteractiveItem)this._InteractiveOwner).Repaint();
        }
        #endregion
        #region Interaktivita = změna rozměru Owner prvku
        /// <summary>
        /// Tato metoda řídí proces Resize svého controlu s pomocí konkrétního <see cref="ResizeItem"/> prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="resizeItem"></param>
        internal void DragItemAction(GDragActionArgs e, ResizeItem resizeItem)
        {
            switch (e.DragAction)
            {
                case DragActionType.DragThisStart:
                    this._DragItemStart(e, resizeItem);
                    break;
                case DragActionType.DragThisMove:
                    this._DragItemMove(e, resizeItem);
                    break;
                case DragActionType.DragThisCancel:
                    this._DragItemCancel(e, resizeItem);
                    break;
                case DragActionType.DragThisDrop:
                    this._DragItemDrop(e, resizeItem);
                    break;
                case DragActionType.DragThisEnd:
                    this._DragItemEnd();
                    break;
            }
        }
        private void _DragItemStart(GDragActionArgs e, ResizeItem resizeItem)
        {
            this._DragOwnerBoundsOriginal = this._InteractiveOwner.Bounds;
            this._DragOwnerMouseOriginal = e.MouseDownAbsolutePoint;
            this._DragOwnerItemSide = resizeItem.Side;
            this._DragOwnerCall(e, this._DragOwnerBoundsOriginal.Value);
        }
        private void _DragItemMove(GDragActionArgs e, ResizeItem resizeItem)
        {
            Rectangle? bounds = this._DragItemGetBounds(e.MouseCurrentAbsolutePoint);
            if (bounds.HasValue)
            {
                this._DragOwnerBoundsTarget = bounds;
                this._DragOwnerCall(e, bounds.Value);
            }
        }
        private void _DragItemCancel(GDragActionArgs e, ResizeItem resizeItem)
        {
            if (this._DragOwnerBoundsOriginal.HasValue)
                this._DragOwnerCall(e, this._DragOwnerBoundsOriginal.Value);
        }
        private void _DragItemDrop(GDragActionArgs e, ResizeItem resizeItem)
        {
            if (this._DragOwnerBoundsTarget.HasValue)
                this._DragOwnerCall(e, this._DragOwnerBoundsTarget.Value);
        }
        private void _DragItemEnd()
        {
            this._DragOwnerBoundsOriginal = null;
            this._DragOwnerMouseOriginal = null;
            this._DragOwnerItemSide = null;
        }
        /// <summary>
        /// Metoda vrací počet pixelů distance mezi danou hranou Rectangle a daným bodem
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="side"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private int? _DragItemGetDistance(Rectangle bounds, RectangleSide side, Point point)
        {
            switch (side)
            {
                case RectangleSide.Left: return point.X - bounds.X;
                case RectangleSide.Top: return point.Y - bounds.Y;
                case RectangleSide.Right: return point.X - bounds.Right;
                case RectangleSide.Bottom: return point.Y - bounds.Bottom;
            }
            return null;
        }
        private Rectangle? _DragItemGetBounds(Point? mousePoint)
        {
            if (!mousePoint.HasValue || !this._DragOwnerBoundsOriginal.HasValue || !this._DragOwnerMouseOriginal.HasValue || !this._DragOwnerItemSide.HasValue) return null;
            bool canUpdsideDown = this.CanUpsideDown;
            Rectangle bounds = this._DragOwnerBoundsOriginal.Value;
            int l = bounds.Left;
            int t = bounds.Top;
            int r = bounds.Right;
            int b = bounds.Bottom;
            Point distance = mousePoint.Value.Sub(this._DragOwnerMouseOriginal.Value);
            switch (this._DragOwnerItemSide.Value)
            {
                case RectangleSide.Left:
                    l = l + distance.X;
                    if (!canUpdsideDown && l > (r - 1))
                        l = r - 1;
                    break;
                case RectangleSide.Top:
                    t = t + distance.Y;
                    if (!canUpdsideDown && t > (b - 1))
                        t = b - 1;
                    break;
                case RectangleSide.Right:
                    r = r + distance.X;
                    if (!canUpdsideDown && r < (l + 1))
                        r = l + 1;
                    break;
                case RectangleSide.Bottom:
                    b = b + distance.Y;
                    if (!canUpdsideDown && b < (t + 1))
                        b = t + 1;
                    break;
            }
            int x = (l < r ? l : r);
            int w = (l < r ? r - l : l - r);
            int y = (t < b ? t : b);
            int h = (t < b ? b - t : t - b);
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Nastaví dané souřadnice do Owner prvku. Pokud to prvek umožňuje, předá i další informace.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        private void _DragOwnerCall(GDragActionArgs e, Rectangle boundsTarget)
        {
            if (!this._DragOwnerBoundsOriginal.HasValue) return;

            Rectangle boundsOriginal = this._DragOwnerBoundsOriginal.Value;    // Souřadnice výchozí, před začátkem procesu Resize
            Rectangle boundsCurrent = this._InteractiveOwner.Bounds;           // Souřadnice nynější, před jejich změnou aktuálním krokem Resize
            DragActionType action = e.DragAction;
            if (this._IResizeObject != null)
            {   // a) varianta přes interface IResizeObject a jeho metodu SetBoundsResized():
                RectangleSide side = RectangleSide.None;
                // Najdeme reálné strany, kde došlo ke změně souřadnice proti původní souřadnici:
                //  (ono při pohybu myši NAHORU na LEVÉ straně sice máme pohyb, ale nemáme změnu Bounds)
                //  (a dále při povolení UpsideDown můžeme sice pohybovat PRAVÝM resizerem doleva, ale nakonec měníme LEFT i RIGHT souřadnici)
                side = (boundsOriginal.Left   != boundsTarget.Left   ? RectangleSide.Left   : RectangleSide.None)
                     | (boundsOriginal.Top    != boundsTarget.Top    ? RectangleSide.Top    : RectangleSide.None)
                     | (boundsOriginal.Right  != boundsTarget.Right  ? RectangleSide.Right  : RectangleSide.None)
                     | (boundsOriginal.Bottom != boundsTarget.Bottom ? RectangleSide.Bottom : RectangleSide.None);
                
                if (side != RectangleSide.None || action == DragActionType.DragThisCancel)
                {
                    ResizeObjectArgs args = new ResizeObjectArgs(e, boundsOriginal, boundsCurrent, boundsTarget, side);
                    this._IResizeObject.SetBoundsResized(args);
                }
            }
            else if (boundsTarget != boundsCurrent || action == DragActionType.DragThisCancel)
            {   // b) varianta s prostým setováním Bounds do prvku (tehdy, když prvek nemá interface IResizeObject):
                this._InteractiveOwner.Bounds = boundsTarget;
                ((IInteractiveItem)this._InteractiveOwner).Parent.Repaint();
            }
        }
        /// <summary>
        /// Souřadnice prvku <see cref="_InteractiveOwner"/> Bounds (=tedy relativní) v okamžiku zahájení procesu Resize
        /// </summary>
        private Rectangle? _DragOwnerBoundsOriginal;
        /// <summary>
        /// Pozice myši absolutní v okamžiku zahájení proces Resize
        /// </summary>
        private Point? _DragOwnerMouseOriginal;
        /// <summary>
        /// Strana, která se přetahuje
        /// </summary>
        private RectangleSide? _DragOwnerItemSide;
        /// <summary>
        /// Souřadnice prvku Bounds výsledné
        /// </summary>
        private Rectangle? _DragOwnerBoundsTarget;
        #endregion
        #region Jednotlivé prvky
        /// <summary>
        /// Nastaví viditelné strany, aktualizuje soupis Child prvků
        /// </summary>
        /// <param name="resizeSides"></param>
        private void _SetResizeSides(RectangleSide resizeSides)
        {
            RectangleSide sides = RectangleSide.None;
            this._ChildList.Clear();
            // Pořadí: v pořadí tchto řádků budou prvky vykreslovány (odspodu nahoru) a interaktivní (odshora dolů):
            this._SetResizeSide(resizeSides, RectangleSide.Top, ref sides, ref this._ItemTop);
            this._SetResizeSide(resizeSides, RectangleSide.Bottom, ref sides, ref this._ItemBottom);
            this._SetResizeSide(resizeSides, RectangleSide.Left, ref sides, ref this._ItemLeft);
            this._SetResizeSide(resizeSides, RectangleSide.Right, ref sides, ref this._ItemRight);
            bool isChange = (this._ResizeSides != sides);
            this._ResizeSides = sides;
            this._InteractiveOwner.InteractivePadding = _GetOwnerPadding(sides);    // Owner objekt musí mít o něco větší interaktivní prostor, aby bylo možno aktivovat patřičné ResizeItem
            // Po změně překreslit:
            if (isChange) this.Repaint();
        }
        /// <summary>
        /// Připraví jeden prvek <see cref="ResizeItem"/> pro jednu stranu "currentSide"
        /// </summary>
        /// <param name="requestSides">Požadované strany</param>
        /// <param name="currentSide">Strana tohoto konkrétního prvku</param>
        /// <param name="realSides">Souhrn viditelných stran</param>
        /// <param name="item">Vizuální prvek</param>
        private void _SetResizeSide(RectangleSide requestSides, RectangleSide currentSide, ref RectangleSide realSides, ref ResizeItem item)
        {
            bool isVisible = ((requestSides & currentSide) != 0);
            if (!isVisible)
            {   // Nemá být vidět:
                if (item != null) item.Is.Visible = false;
            }
            else
            {   // Má být vidět:
                if (item == null)
                {
                    item = new ResizeItem(this, currentSide, this._InteractiveOwner);
                }
                item.Is.Visible = true;
                realSides |= currentSide;
                this._ChildList.Add(item);
            }
        }
        /// <summary>
        /// Metoda vrací okraje pro <see cref="InteractiveObject.InteractivePadding"/> pro dané aktivní strany
        /// </summary>
        /// <param name="sides"></param>
        /// <returns></returns>
        private Padding _GetOwnerPadding(RectangleSide sides)
        {
            Padding padding = new Padding(
                (sides.HasFlag(RectangleSide.Left) ? 3 : 0),
                (sides.HasFlag(RectangleSide.Top) ? 3 : 0),
                (sides.HasFlag(RectangleSide.Right) ? 3 : 0),
                (sides.HasFlag(RectangleSide.Bottom) ? 3 : 0)
                );
            return padding;
        }
        private ResizeItem _ItemLeft;
        private ResizeItem _ItemTop;
        private ResizeItem _ItemRight;
        private ResizeItem _ItemBottom;
        private List<ResizeItem> _ChildList;
        #endregion
    }
    /// <summary>
    /// Prvek, který slouží jiným vizuálním prvkům jako "Resizer" = aktivní hrana, která dovoluje změnit některou souřadnici prvku
    /// </summary>
    public class ResizeItem : InteractiveDragObject  /* InteractiveObject */, IInteractiveItem
    {
        #region Konstrukce a vnitřní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="resizeControl">Kontroler Resize</param>
        /// <param name="side">Strana</param>
        /// <param name="parent">Vizuální parent</param>
        public ResizeItem(ResizeControl resizeControl, RectangleSide side, IInteractiveItem parent)
            :base()
        {
            this._ResizeControl = resizeControl;
            this._Side = side;
            this._IParent = parent;
            this._Orientation = (((side & RectangleSide.Horizontal) != 0) ? System.Windows.Forms.Orientation.Horizontal : System.Windows.Forms.Orientation.Vertical);
            this.Parent = parent;
            this.InteractivePadding = (this._Orientation == Orientation.Horizontal ? new Padding(0, 3, 0, 3) : new Padding(3, 0, 3, 0));
        }
        /// <summary>
        /// Metoda vrací okraje pro <see cref="InteractiveObject.InteractivePadding"/> pro danou stranu <see cref="ResizeItem.Side"/>
        /// </summary>
        /// <param name="sides"></param>
        /// <returns></returns>
        private Padding? _GetResizeItemPadding(RectangleSide sides)
        {
            switch (sides)
            {
                case RectangleSide.Left: return new Padding(3, 0, 2, 0);
                case RectangleSide.Top: return new Padding(0, 3, 0, 2);
                case RectangleSide.Right: return new Padding(2, 0, 3, 0);
                case RectangleSide.Bottom: return new Padding(0, 2, 0, 3);
            }
            return null;
        }
        private ResizeControl _ResizeControl;
        private RectangleSide _Side;
        private IInteractiveItem _IParent;
        private Orientation _Orientation;
        #endregion
        #region Public properties
        /// <summary>
        /// Souřadnice prvku jsou dány v zásadě jeho parentem, a závisí na nastavení straně prvku a stavu.
        /// Souřadnice nelze setovat, nic se nezmění.
        /// </summary>
        public override Rectangle Bounds
        {
            get
            {
                Size parentSize = this.ParentSize;
                int size = this.GetCurrentValue(Skin.Splitter.InactiveSize, Skin.Splitter.MouseOnParentSize, Skin.Splitter.MouseOverSize, Skin.Splitter.MouseDownSize);
                switch (this.Side)
                {
                    case RectangleSide.Left: return new Rectangle(0, 0, size, parentSize.Height);
                    case RectangleSide.Right: return new Rectangle(parentSize.Width - size, 0, size, parentSize.Height);
                    case RectangleSide.Top: return new Rectangle(0, 0, parentSize.Width, size);
                    case RectangleSide.Bottom: return new Rectangle(0, parentSize.Height - size, parentSize.Width, size);
                }
                return new Rectangle(0, 0, 0, 0);
            }
            set { }
        }
        /// <summary>
        /// Strana, kde je tento prvek kreslen
        /// </summary>
        public RectangleSide Side { get { return this._Side; } }
        /// <summary>
        /// Orientace this prvku: Vertical = pro levou a pravou stranu; Horizontal = pro horní a dolní stranu
        /// </summary>
        public Orientation Orientation { get { return this._Orientation; } }
        #endregion
        #region Protected overrides a support metody
        /// <summary>
        /// Velikost parent objektu
        /// </summary>
        protected Size ParentSize { get { return this._IParent.Bounds.Size; } }
        /// <summary>
        /// Při repaint this prvku provést repaint i pro parenta
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        /// <summary>
        /// Metoda vrátí přiměřenou hodnotu k aktuálnímu stavu this objektu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="valueInactive">Hodnota, když this objekt ani parent nejsou aktivní</param>
        /// <param name="valueMouseParent">Hodnota, když parent má nad sebou myš, ale this prvek ještě ne</param>
        /// <param name="valueMouseOver">Hodnota, když this prvek má nad sebou myš</param>
        /// <param name="valueMouseDown">Hodnota, když this prvek má zmáčknutou levou myš</param>
        /// <returns></returns>
        protected T GetCurrentValue<T>(T valueInactive, T valueMouseParent, T valueMouseOver, T valueMouseDown)
        {
            if (this.IsMouseDown) return valueMouseDown;
            if (this.IsMouseActive) return valueMouseOver;
            if (this.ParentHasMouse) return valueMouseParent;
            return valueInactive;
        }
        /// <summary>
        /// Obsahuje true, když parent vizuální objekt má myš
        /// </summary>
        protected bool ParentHasMouse { get { return (this._IParent.InteractiveState.HasAnyFlag(GInteractiveState.MouseOver | GInteractiveState.FlagDown)); } }
        #endregion
        #region Interaktivita = myš a Drag
        /// <summary>
        /// Interakce: po vstupu myši - nastavit kurzor
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseEnter(e);
            e.RequiredCursorType = (this.Orientation == Orientation.Vertical ? SysCursorType.VSplit : SysCursorType.HSplit);
        }
        /// <summary>
        /// Interakce: po odchodu myši - nastavit kurzor
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            e.RequiredCursorType = SysCursorType.Default;
        }
        /// <summary>
        /// Interakce: Drag and Drop - předání do controleru
        /// </summary>
        /// <param name="e"></param>
        protected override void DragAction(GDragActionArgs e)
        {
            // Nepoužíváme base podporu : base.DragAction(e);
            this._ResizeControl.DragItemAction(e, this);
        }
        #endregion
        #region Kreslení
        /// <summary>
        /// Kreslení prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            if (!this._ResizeControl.ShowResizeAllways && !this.ParentHasMouse) return;
            if (this.Orientation == Orientation.Vertical)
                this.DrawVertical(e, absoluteBounds, absoluteVisibleBounds, drawMode);
            else
                this.DrawHorizontal(e, absoluteBounds, absoluteVisibleBounds, drawMode);
        }
        /// <summary>
        /// Kreslení prvku VERTICAL
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected void DrawVertical(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Rectangle beginBounds = new Rectangle(absoluteBounds.X, absoluteBounds.Y, 1, absoluteBounds.Height);
            Rectangle centerBounds = new Rectangle(absoluteBounds.X + 1, absoluteBounds.Y, absoluteBounds.Width - 2, absoluteBounds.Height);
            Rectangle endBounds = new Rectangle(absoluteBounds.Right - 1, absoluteBounds.Y, 1, absoluteBounds.Height);
            this.DrawResizer(e, beginBounds, centerBounds, endBounds);
        }
        /// <summary>
        /// Kreslení prvku HORIZONAL
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected void DrawHorizontal(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Rectangle beginBounds = new Rectangle(absoluteBounds.X, absoluteBounds.Y, absoluteBounds.Width, 1);
            Rectangle centerBounds = new Rectangle(absoluteBounds.X, absoluteBounds.Y + 1, absoluteBounds.Width, absoluteBounds.Height - 2);
            Rectangle endBounds = new Rectangle(absoluteBounds.X, absoluteBounds.Bottom - 1, absoluteBounds.Width, 1);
            this.DrawResizer(e, beginBounds, centerBounds, endBounds);
        }
        /// <summary>
        /// Kreslení prvku v rámci daných souřadnic
        /// </summary>
        /// <param name="e"></param>
        /// <param name="beginBounds"></param>
        /// <param name="centerBounds"></param>
        /// <param name="endBounds"></param>
        protected void DrawResizer(GInteractiveDrawArgs e, Rectangle beginBounds, Rectangle centerBounds, Rectangle endBounds)
        {
            Color baseColor = this.GetCurrentValue(this._ResizeControl.InactiveColor, this._ResizeControl.MouseOnParentColor, this._ResizeControl.MouseOverColor, this._ResizeControl.MouseDownColor);
            Color lightColor = Skin.Modifiers.GetColor3DBorderLight(baseColor);
            Color darkColor = Skin.Modifiers.GetColor3DBorderDark(baseColor);

            if (centerBounds.Height > 0)
                e.Graphics.FillRectangle(Skin.Brush(baseColor), centerBounds);
            if (this.IsMouseDown)
            {   // MouseDown => Begin prostor bude tmavší, End prostor bude světlejší:
                e.Graphics.FillRectangle(Skin.Brush(darkColor), beginBounds);
                e.Graphics.FillRectangle(Skin.Brush(lightColor), endBounds);
            }
            else
            {   // Bez MouseDown:
                e.Graphics.FillRectangle(Skin.Brush(lightColor), beginBounds);
                e.Graphics.FillRectangle(Skin.Brush(darkColor), endBounds);
            }
        }
        #endregion
    }
    #region interface IResizeObject + class ResizeObjectArgs
    /// <summary>
    /// Interface pro objekt, který dovoluje být resizován pomocí <see cref="ResizeControl"/>, a chce dostávat rozšíření informace o procesu Resize.
    /// Objekt musí implementovat metodu <see cref="SetBoundsResized(Rectangle, RectangleSide, DragActionType)"/>
    /// </summary>
    public interface IResizeObject
    {
        /// <summary>
        /// Objekt dostává souřadnice, které by měl mít po provedení interaktivního Resize.
        /// Dostává i další údaje (která strana se pohybuje, a jakým procesem).
        /// <para/>
        /// Pozor, objekt by si měl sám zajistit provedení Parent.Repaint() !!! Jinak bude poškozena grafika.
        /// </summary>
        /// <param name="args">Data o procesu Resize (pomocí Drag and Drop)</param>
        void SetBoundsResized(ResizeObjectArgs args);
    }
    /// <summary>
    /// Argumenty pro proces Resize
    /// </summary>
    public class ResizeObjectArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsSource"></param>
        /// <param name="boundsCurrent"></param>
        /// <param name="boundsTarget"></param>
        /// <param name="side"></param>
        public ResizeObjectArgs(GDragActionArgs e, Rectangle boundsSource, Rectangle boundsCurrent, Rectangle boundsTarget, RectangleSide side)
        {
            this.DragArgs = e;
            this.BoundsOriginal = boundsSource;
            this.BoundsCurrent = boundsCurrent;
            this.BoundsTarget = boundsTarget;
            this.ChangedSide = side;
        }
        /// <summary>
        /// Kompletní argument Drag and Drop
        /// </summary>
        public GDragActionArgs DragArgs { get; private set; }
        /// <summary>
        /// Souřadnice objektu výchozí, v okamžiku startu.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsOriginal { get; private set; }
        /// <summary>
        /// Souřadnice objektu aktuální, v průběhu resize, před provedením aktuálního kroku.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsCurrent { get; private set; }
        /// <summary>
        /// Souřadnice objektu cílová, odvozená pouze od pozice myši.
        /// Souřadnice je relativní, odpovídající Item.Bounds.
        /// </summary>
        public Rectangle BoundsTarget { get; private set; }
        /// <summary>
        /// Strana prvku, která se pohybuje
        /// </summary>
        public RectangleSide ChangedSide { get; private set; }
        /// <summary>
        /// Kompletní data o interaktivní akci
        /// </summary>
        public GInteractiveChangeStateArgs ChangeArgs { get { return this.DragArgs.ChangeArgs; } }
        /// <summary>
        /// Typ akce (start, pohyb, cancel, ukončení)
        /// </summary>
        public DragActionType ResizeAction { get { return this.DragArgs.DragAction; } }
    }
    #endregion
}
