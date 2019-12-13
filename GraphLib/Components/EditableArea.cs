using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region GVirtualMovableItem : item with VirtualBounds and IVirtualConvertor, whose position and size can interactively changed by the user (by dragging with area or edge marks).
    /// <summary>
    /// GVirtualMovableItem : editable area, whose position and size can interactively changed by the user (by dragging with area or edge marks).
    /// Is Virtual: has 
    /// </summary>
    public class GVirtualMovableItem : GMovableItem, IInteractiveItem
    {
        #region Constructor + Standard bounds overrides
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="virtualConvertor"></param>
        public GVirtualMovableItem(IVirtualConvertor virtualConvertor)
        {
            this._VirtualConvertor = virtualConvertor;
        }
        /// <summary>
        /// Visual bounds of this item. On this coordinates is item displayed.
        /// Item is active on ActiveBounds, which is by 2pixel greater.
        /// </summary>
        public override Rectangle Bounds
        {
            get { this.CheckBounds(ProcessAction.None, EventSourceType.BoundsChange | EventSourceType.BoundsChange); return base.Bounds; }
            set { base.Bounds = value; this.RecalculateVirtualBounds(); }
        }
        #endregion
        #region Virtual bounds support (convertor, recalculate)
        /// <summary>
        /// Virtual convertor
        /// </summary>
        public IVirtualConvertor VirtualConvertor
        {
            get { return this._VirtualConvertor; }
            set { this._VirtualConvertor = value; this.InvalidateActiveBounds(); }
        }
        /// <summary>
        /// Virtual bounds (logical value).
        /// It is basical value.
        /// </summary>
        public RectangleD VirtualBounds
        {
            get { return this._VirtualBounds; }
            set { this._VirtualBounds = value; this.InvalidateActiveBounds(); }
        }
        private RectangleD _VirtualBounds;
        /// <summary>
        /// Ensure this.Bounds is valid for VirtualBounds and VirtualConvertor.
        /// If current Bounds is different from (VirtualBounds × VirtualConvertor), then store correct value to base.Bounds from this.VirtualBounds via this.VirtualConvertor.
        /// </summary>
        protected void CheckBounds(ProcessAction actions, EventSourceType sourceType)
        {
            if (this.IsVirtualValid) return;

            Rectangle? bounds = this._VirtualConvertor.ConvertToPixelFromLogical(this._VirtualBounds);
            if (bounds.HasValue)
            {
                this._LastIdentity = this._VirtualConvertor.Identity;
                this.SetBounds(bounds.Value, actions, sourceType);
            }
        }
        /// <summary>
        /// Store value to this._VirtualBounds from base.ActiveBounds (via this._VirtualConvertor).
        /// Set this._LastIdentity (from this._VirtualConvertor.Identity).
        /// </summary>
        private void RecalculateVirtualBounds()
        {
            if (this._VirtualConvertor == null || !this._VirtualConvertor.ConvertIsReady) return;
            this._VirtualBounds = this._VirtualConvertor.ConvertToLogicalFromPixel(base.Bounds).Value;
            this._LastIdentity = this._VirtualConvertor.Identity;
        }
        /// <summary>
        /// true when curernt this.VirtualConvertor Identity is equal as last cached Identity.
        /// </summary>
        protected bool IsVirtualValid
        {
            get
            {
                if (this._VirtualConvertor == null || !this._VirtualConvertor.ConvertIsReady) return true;     // Without a convertor = valid
                IComparable currentIdentity = this._VirtualConvertor.Identity;
                if (currentIdentity == null) return true;
                if (this._LastIdentity == null) return false;
                return (this._LastIdentity.CompareTo(currentIdentity) == 0);
            }
        }
        /// <summary>
        /// Invalidate ActiveBounds
        /// </summary>
        protected void InvalidateActiveBounds()
        {
            this._LastIdentity = null;
        }
        /// <summary>
        /// Last identity of virtual convertor
        /// </summary>
        private IComparable _LastIdentity;
        /// <summary>
        /// Virtual convertor
        /// </summary>
        private IVirtualConvertor _VirtualConvertor;
        #endregion
    }
    #endregion
    #region GMovableItem : item, whose position and size can interactively changed by the user (by dragging with area or edge marks).
    /// <summary>
    /// GMovableItem : item, whose position and size can interactively changed by the user (by dragging with area or edge marks).
    /// </summary>
    public class GMovableItem : InteractiveDragObject, IInteractiveItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GMovableItem()
        {
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseOverProperties
                      | InteractiveProperties.Bit.MouseDragResizeX
                      | InteractiveProperties.Bit.MouseDragResizeY
                      | InteractiveProperties.Bit.DrawDragMoveGhostStandard);
                
            this.BackColor = Skin.Button.BackColor;
            this.ChildItemsInit();
        }
        #region Dragging - Move and Resize
        /// <summary>
        /// Mode for drawing a ghost during Drag operation
        /// </summary>
        public DragDrawGhostMode DragDrawGhost { get { return this.DragDrawGhostMode; } }
        /// <summary>
        /// Bounds of this object in time of begin of dragging.
        /// Its drawed as Ghost on this coordinates, when DragWithGhost is true.
        /// </summary>
        protected Rectangle? OriginalBounds { get; set; }
        /// <summary>
        /// Item can be interactive moved
        /// </summary>
        protected bool CanItemMove { get { return this.Is.MouseDragMove; } }
        /// <summary>
        /// Item can be interactive resized on X axis
        /// </summary>
        protected bool CanItemResizeX { get { return this.Is.MouseDragResizeX; } }
        /// <summary>
        /// Item can be interactive resized on Y axis
        /// </summary>
        protected bool CanItemResizeY { get { return this.Is.MouseDragResizeY; } }
        /// <summary>
        /// Item can be interactive resized on X and Y axis
        /// </summary>
        protected bool CanItemResize { get { return (this.Is.MouseDragResizeX && this.Is.MouseDragResizeY); } }
        /// <summary>
        /// Item can be interactive selected
        /// </summary>
        protected bool CanSelect { get { return this.Is.Selectable; } }
        /// <summary>
        /// Item can be dragged (move and resize) only after selection
        /// </summary>
        protected bool CanDragOnlySelected { get { return false; } }
        #endregion
        #region Child items, Init, Filter and Cache, Type-properties
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, only when action PrepareInnerItems is specified.
        /// Recalculate SubItems bounds after change this.Bounds.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            int s = ACTIVE_ITEM_SIZE;
            int x0 = 0;
            int x1 = newBounds.Width / 2;
            int x2 = newBounds.Width - 1;
            int y0 = 0;
            int y1 = newBounds.Height / 2;
            int y2 = newBounds.Height - 1;

            this.ItemArea.SetRelativeBounds(x0, y0, newBounds.Size);
            this.ItemBorder.SetRelativeBounds(x0, y0, newBounds.Size);
            this.ItemTopLeft.SetRelativeBounds(x0, y0, s);
            this.ItemTop.SetRelativeBounds(x1, y0, s);
            this.ItemTopRight.SetRelativeBounds(x2, y0, s);
            this.ItemRight.SetRelativeBounds(x2, y1, s);
            this.ItemBottomRight.SetRelativeBounds(x2, y2, s);
            this.ItemBottom.SetRelativeBounds(x1, y2, s);
            this.ItemBottomLeft.SetRelativeBounds(x0, y2, s);
            this.ItemLeft.SetRelativeBounds(x0, y1, s);
        }
        /// <summary>
        /// Child items = subitems
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetSubItems(); } }
        /// <summary>
        /// Create all Child items
        /// </summary>
        protected void ChildItemsInit()
        {
            this.ChildItemDict = new Dictionary<ChildItemType, ChildItem>();
            int ao = ACTIVE_BORDER;
            this.ChildItemDict.Add(ChildItemType.Area, new ChildItem(this, ChildItemType.Area, SysCursorType.Default, SysCursorType.SizeAll, ao));
            this.ChildItemDict.Add(ChildItemType.Border, new ChildItem(this, ChildItemType.Border, SysCursorType.Default, SysCursorType.Default, ao));
            this.ChildItemDict.Add(ChildItemType.TopLeft, new ChildItem(this, ChildItemType.TopLeft, SysCursorType.SizeNWSE, SysCursorType.SizeNWSE, ao));
            this.ChildItemDict.Add(ChildItemType.Top, new ChildItem(this, ChildItemType.Top, SysCursorType.SizeNS, SysCursorType.SizeNS, ao));
            this.ChildItemDict.Add(ChildItemType.TopRight, new ChildItem(this, ChildItemType.TopRight, SysCursorType.SizeNESW, SysCursorType.SizeNESW, ao));
            this.ChildItemDict.Add(ChildItemType.Right, new ChildItem(this, ChildItemType.Right, SysCursorType.SizeWE, SysCursorType.SizeWE, ao));
            this.ChildItemDict.Add(ChildItemType.BottomRight, new ChildItem(this, ChildItemType.BottomRight, SysCursorType.SizeNWSE, SysCursorType.SizeNWSE, ao));
            this.ChildItemDict.Add(ChildItemType.Bottom, new ChildItem(this, ChildItemType.Bottom, SysCursorType.SizeNS, SysCursorType.SizeNS, ao));
            this.ChildItemDict.Add(ChildItemType.BottomLeft, new ChildItem(this, ChildItemType.BottomLeft, SysCursorType.SizeNESW, SysCursorType.SizeNESW, ao));
            this.ChildItemDict.Add(ChildItemType.Left, new ChildItem(this, ChildItemType.Left, SysCursorType.SizeWE, SysCursorType.SizeWE, ao));
        }
        /// <summary>
        /// Returns a (cached) array of currently active items
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<InteractiveObject> GetSubItems()
        {
            if (this.Items == null)
            {
                this.Items = this.ChildItemDict.Values.Where(i => i.Is.Enabled).ToArray();
            }
            return this.Items;
        }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemArea { get { return this.ChildItemDict[ChildItemType.Area]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemBorder { get { return this.ChildItemDict[ChildItemType.Border]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemTopLeft { get { return this.ChildItemDict[ChildItemType.TopLeft]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemTop { get { return this.ChildItemDict[ChildItemType.Top]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemTopRight { get { return this.ChildItemDict[ChildItemType.TopRight]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemRight { get { return this.ChildItemDict[ChildItemType.Right]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemBottomRight { get { return this.ChildItemDict[ChildItemType.BottomRight]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemBottom { get { return this.ChildItemDict[ChildItemType.Bottom]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemBottomLeft { get { return this.ChildItemDict[ChildItemType.BottomLeft]; } }
        /// <summary>Konkrétní Child prvek</summary>
        protected ChildItem ItemLeft { get { return this.ChildItemDict[ChildItemType.Left]; } }
        /// <summary>
        /// Is enabled SubItem of specified type in current Style?
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        protected bool IsEnabledChildItemType(ChildItemType itemType)
        {
            bool rx = this.CanItemResizeX;
            bool ry = this.CanItemResizeY;
            bool ra = this.CanItemResize;
            switch (itemType)
            {
                case ChildItemType.Area: return true;
                case ChildItemType.Border: return false;
                case ChildItemType.TopLeft: return ra;
                case ChildItemType.Top: return ry;
                case ChildItemType.TopRight: return ra;
                case ChildItemType.Right: return rx;
                case ChildItemType.BottomRight: return ra;
                case ChildItemType.Bottom: return ry;
                case ChildItemType.BottomLeft: return ra;
                case ChildItemType.Left: return rx;
            }
            return false;
        }
        /// <summary>
        /// Is visible SubItem of specified type in current Style and State?
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        protected bool IsVisibleChildItemType(ChildItemType itemType)
        {
            bool e = this.IsEnabledChildItemType(itemType);
            bool s = this.IsSelected;
            bool o = this.CanDragOnlySelected;
            bool v = (this.InteractiveState == GInteractiveState.MouseOver);
            bool d = (this.InteractiveState == GInteractiveState.LeftDown || this.InteractiveState == GInteractiveState.RightDown);
            bool g = (this.InteractiveState == GInteractiveState.LeftDrag || this.InteractiveState == GInteractiveState.RightDrag);
            bool m = (v || d || g);

            switch (itemType)
            {
                case ChildItemType.Area: return true;
                case ChildItemType.Border: return (s || (!o && m));                    // Border is visible: only when Selected, or (!DragOnlySelected and with mouse)
                case ChildItemType.TopLeft:
                case ChildItemType.Top:
                case ChildItemType.TopRight:
                case ChildItemType.Right:
                case ChildItemType.BottomRight:
                case ChildItemType.Bottom:
                case ChildItemType.BottomLeft:
                case ChildItemType.Left: return (e && ((s && m) || (!o && m)));        // Grip: when enabled and ((selected and mouse) or (!DragOnlySelected and with mouse))
            }
            return false;
        }
        /// <summary>
        /// Dictionary of all ChildItem items
        /// </summary>
        protected Dictionary<ChildItemType, ChildItem> ChildItemDict;
        /// <summary>
        /// Array of currently visibled ChildItem items (in current Style), cached
        /// </summary>
        protected InteractiveObject[] Items;
        /// <summary>
        /// 2 = Active border (EditableSubItem.ActiveOverhead)
        /// </summary>
        protected const int ACTIVE_BORDER = 2;
        /// <summary>
        /// 5 = Active item size (size of grip)
        /// </summary>
        protected const int ACTIVE_ITEM_SIZE = 5;
        #region class ChildItem : class for child items in MovableArea (functional interactive areas), enum ChildItemType : type of specific Child item
        /// <summary>
        /// ChildItem : class for child items in MovableArea (functional interactive areas)
        /// </summary>
        protected class ChildItem : InteractiveObject
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="itemType"></param>
            /// <param name="overCursorType"></param>
            /// <param name="dragCursorType"></param>
            /// <param name="activeOverhead"></param>
            public ChildItem(GMovableItem owner, ChildItemType itemType, SysCursorType? overCursorType, SysCursorType? dragCursorType, int activeOverhead)
            {
                this.Parent = owner;
                this.ItemType = itemType;
                this.OverCursorType = overCursorType;
                this.DragCursorType = dragCursorType;
                this.InteractivePadding = new Padding(activeOverhead);
                this.Is.GetEnabled = this._GetEnabled;
                this.Is.GetVisible = this._GetVisible;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.ItemType.ToString() + ": " + this.Bounds.ToString() + "; Owner: " + this.Owner.ToString();
            }
            /// <summary>
            /// Owner of this Subitem = an EditableArea
            /// </summary>
            public GMovableItem Owner { get { return this.Parent as GMovableItem; } }
            /// <summary>
            /// Type of SubItem
            /// </summary>
            public ChildItemType ItemType { get; private set; }
            /// <summary>
            /// Cursor type in MouseOver state for this SubItem
            /// </summary>
            public SysCursorType? OverCursorType { get; private set; }
            /// <summary>
            /// Cursor type in MouseDrag state for this SubItem
            /// </summary>
            public SysCursorType? DragCursorType { get; private set; }
            /// <summary>
            /// Is this SubItem Enabled by Owner.Style ?
            /// </summary>
            private bool _GetEnabled(bool value)
            {
                return this.Owner.IsEnabledChildItemType(this.ItemType);
            }
            /// <summary>
            /// Is this SubItem Visible by Owner.Style and Owner.State ?
            /// </summary>
            private bool _GetVisible(bool value)
            {
                return this.Owner.IsVisibleChildItemType(this.ItemType);
            }
            /// <summary>
            /// Hold a mouse attention.
            /// When a item is drawed to Interactive layer (in MouseOver, MouseDrag and in other active states), this is: above other subitem, 
            /// then is advisable "hold mouse attention" for this item before other items.
            /// But when active item is drawed under other items, then hold mouse attention is not recommended 
            /// (in example for back area of movable item, before its grips).
            /// </summary>
            public bool HoldMouse { get { return (this.ItemType != ChildItemType.Area); } set { } }
            /// <summary>
            /// Can drag this SubItem
            /// </summary>
            public bool CanDrag { get { return (this.Is.Enabled); } }
            /// <summary>
            /// Is this SubItem currently active SubItem of Owner?
            /// </summary>
            public bool IsCurrentItem { get { return (this.ItemType == this.Owner.ActiveChildType); } }
            /// <summary>
            /// Interactive State of Owner item
            /// </summary>
            public GInteractiveState ItemState { get { return (this.IsCurrentItem ? this.Owner.InteractiveState : GInteractiveState.None); } }
            /// <summary>
            /// Store new value to this.ActivePoint = { x, y } and this.Bounds from { ActivePoint, size }.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="size"></param>
            internal void SetRelativeBounds(int x, int y, Size size)
            {
                this.ActivePoint = new Point(x, y);
                Rectangle rvb = new Rectangle(x, y, size.Width, size.Height);
                this.SetBounds(rvb, ProcessAction.None, EventSourceType.BoundsChange | EventSourceType.InteractiveChanged);
            }
            /// <summary>
            /// Store new value to this.ActivePoint = { x, y } and this.Bounds from { ActivePoint, size }.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="size"></param>
            internal void SetRelativeBounds(int x, int y, int size)
            {
                this.ActivePoint = new Point(x, y);
                int half = size / 2;
                Rectangle rvb = new Rectangle(x - half, y - half, size, size);
                this.SetBounds(rvb, ProcessAction.None, EventSourceType.BoundsChange | EventSourceType.InteractiveChanged);
            }
            /// <summary>
            /// Active point of this Child Item
            /// </summary>
            internal Point ActivePoint { get; private set; }
            /// <summary>
            /// Draw a editable clips of EditableArea
            /// </summary>
            /// <param name="e"></param>
            public void DrawChild(GInteractiveDrawArgs e)
            {
                this.DrawItem(e);
            }
            /// <summary>
            /// Physically draw this ChildItem on given Graphics.
            /// </summary>
            /// <param name="e"></param>
            protected virtual void DrawItem(GInteractiveDrawArgs e)
            {
                if (!this.Is.Visible) return;

                Rectangle bounds;
                switch (this.ItemType)
                {
                    case ChildItemType.Area:
                        break;
                    case ChildItemType.Border:
                        bounds = this.BoundsAbsolute.Enlarge(0, 0, -1, -1);
                        e.Graphics.DrawRectangle(Pens.Black, bounds);
                        break;
                    case ChildItemType.Left:
                    case ChildItemType.TopLeft:
                    case ChildItemType.Top:
                    case ChildItemType.TopRight:
                    case ChildItemType.Right:
                    case ChildItemType.BottomRight:
                    case ChildItemType.Bottom:
                    case ChildItemType.BottomLeft:
                        bounds = this.BoundsAbsolute;
                        e.Graphics.FillRectangle(GPainter.InteractiveClipBrushForState(this.ItemState), bounds);
                        break;
                }
            }
            /// <summary>
            /// Called after any interactive change value of State
            /// </summary>
            /// <param name="e"></param>
            protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
            {
                this.Owner.AfterStateChanged(e, this);
            }
            /// <summary>
            /// Called to draw content of this item.
            /// This instance (GScrollbar.ChildItem) is not drawed by system, but as part of Scrollbar.
            /// </summary>
            /// <param name="e">Data pro kreslení</param>
            /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
            /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
            protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
            {
                // This instance (GMovableItem.ChildItem) is not drawed by system, but as part of GMovableItem:
                //   base.Draw(e);
            }
        }
        /// <summary>
        /// ChildItemType : type of specific Child item
        /// </summary>
        protected enum ChildItemType : int
        {
            /// <summary>Konkrétní typ prvku</summary>
            None = 0,
            /// <summary>Konkrétní typ prvku</summary>
            Area,
            /// <summary>Konkrétní typ prvku</summary>
            Border,
            /// <summary>Konkrétní typ prvku</summary>
            TopLeft,
            /// <summary>Konkrétní typ prvku</summary>
            Top,
            /// <summary>Konkrétní typ prvku</summary>
            TopRight,
            /// <summary>Konkrétní typ prvku</summary>
            Right,
            /// <summary>Konkrétní typ prvku</summary>
            BottomRight,
            /// <summary>Konkrétní typ prvku</summary>
            Bottom,
            /// <summary>Konkrétní typ prvku</summary>
            BottomLeft,
            /// <summary>Konkrétní typ prvku</summary>
            Left
        }
        #endregion
        #endregion
        #region Interactivity
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            this.AfterStateChanged(e, null);
        }
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        /// <param name="childItem"></param>
        protected void AfterStateChanged(GInteractiveChangeStateArgs e, ChildItem childItem)
        {
            if (childItem != null)
            {
                this.InteractiveState = e.TargetState;
            }
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseEnter:
                    // Mouse can Enter to main item = this (childItem != null), or to child item (childItem != null):
                    this.ActiveChild = childItem;
                    this.Repaint();
                    if (childItem != null)
                        e.RequiredCursorType = childItem.OverCursorType;
                    break;
                case GInteractiveChangeState.MouseLeave:
                    // Mouse can Leave from main item = this (childItem != null), or from child item (childItem != null):
                    this.ActiveChild = null;
                    this.Repaint();
                    if (childItem != null)
                        e.RequiredCursorType = SysCursorType.Default;
                    else
                        e.RepaintAllItems = true;
                    break;
                case GInteractiveChangeState.MouseOver:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftDragMoveBegin:
                    if (childItem != null && childItem.CanDrag)
                    {
                        this.BoundsDragOrigin = this.Bounds;
                        this.ActiveChild = childItem;
                        this.Repaint(GInteractiveDrawLayer.Standard | GInteractiveDrawLayer.Interactive);
                        e.RepaintAllItems = true;
                        e.RequiredCursorType = childItem.DragCursorType;
                        e.UserDragPoint = this.Bounds.Location.Add(childItem.ActivePoint);
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveStep:
                    if (this.ActiveChild != null && this.ActiveChild.CanDrag && e.UserDragPoint.HasValue)
                    {
                        this.Repaint( /* GInteractiveDrawLayer.Standard | */ GInteractiveDrawLayer.Interactive);
                        Point currentPoint = e.UserDragPoint.Value;
                        this.CalculateNewBounds(this.ActiveChild, e.UserDragPoint.Value);
                        
                        Rectangle newBounds = this.Bounds;
                        Rectangle oldBounds = (this.BoundsDragOrigin.HasValue ? this.BoundsDragOrigin.Value : newBounds);
                        this.CallBoundsChanged(oldBounds, newBounds, EventSourceType.InteractiveChanging | EventSourceType.BoundsChange);
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveCancel:

                    if (this.BoundsDragOrigin.HasValue)
                    {
                        Rectangle oldBounds = this.Bounds;
                        Rectangle newBounds = this.BoundsDragOrigin.Value;
                        this.SetBounds(newBounds, ProcessAction.PrepareInnerItems, EventSourceType.InteractiveChanged);
                        this.Repaint();
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveDone:
                    if (this.ActiveChild != null && this.ActiveChild.CanDrag)
                    {
                        Rectangle newBounds = this.Bounds;
                        Rectangle oldBounds = (this.BoundsDragOrigin.HasValue ? this.BoundsDragOrigin.Value : newBounds);
                        this.CallBoundsChanged(oldBounds, newBounds, EventSourceType.InteractiveChanged | EventSourceType.BoundsChange);
                        this.Repaint();
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveEnd:
                    this.BoundsDragOrigin = null;
                    if (childItem != null)
                        e.RequiredCursorType = childItem.OverCursorType;
                    e.RepaintAllItems = true;
                    break;
                default:
                    var s = e.ChangeState;
                    break;
            }
        }
        /// <summary>
        /// Calculate new bounds during interactive moving or resizing
        /// </summary>
        /// <param name="subItem"></param>
        /// <param name="mousePoint"></param>
        protected void CalculateNewBounds(ChildItem subItem, Point mousePoint)
        {
            Rectangle originalBounds = this.BoundsDragOrigin.Value;
            Rectangle draggedBounds = originalBounds;
            Point begin = originalBounds.Location;
            Point end = originalBounds.End();
            bool ctrl = Control.ModifierKeys == Keys.Control;
            switch (subItem.ItemType)
            {
                case ChildItemType.Area:
                case ChildItemType.Border:
                    if (ctrl)
                    {
                        Point d = begin.Sub(mousePoint);
                        if (Math.Abs(d.X) >= Math.Abs(d.Y))
                            mousePoint = new Point(mousePoint.X, originalBounds.Y);
                        else
                            mousePoint = new Point(originalBounds.X, mousePoint.Y);
                    }
                    draggedBounds = new Rectangle(mousePoint, originalBounds.Size);
                    break;
                case ChildItemType.TopLeft:
                    draggedBounds = DrawingExtensions.FromDim(mousePoint.X, end.X, mousePoint.Y, end.Y);
                    break;
                case ChildItemType.Top:
                    draggedBounds = DrawingExtensions.FromDim(begin.X, end.X, mousePoint.Y, end.Y);
                    break;
                case ChildItemType.TopRight:
                    draggedBounds = DrawingExtensions.FromDim(begin.X, mousePoint.X, mousePoint.Y, end.Y);
                    break;
                case ChildItemType.Right:
                    draggedBounds = DrawingExtensions.FromDim(begin.X, mousePoint.X, begin.Y, end.Y);
                    break;
                case ChildItemType.BottomRight:
                    draggedBounds = DrawingExtensions.FromDim(begin.X, mousePoint.X, begin.Y, mousePoint.Y);
                    break;
                case ChildItemType.Bottom:
                    draggedBounds = DrawingExtensions.FromDim(begin.X, end.X, begin.Y, mousePoint.Y);
                    break;
                case ChildItemType.BottomLeft:
                    draggedBounds = DrawingExtensions.FromDim(mousePoint.X, end.X, begin.Y, mousePoint.Y);
                    break;
                case ChildItemType.Left:
                    draggedBounds = DrawingExtensions.FromDim(mousePoint.X, end.X, begin.Y, end.Y);
                    break;
            }

            this.SetBounds(draggedBounds, ProcessAction.PrepareInnerItems, EventSourceType.InteractiveChanging);
        }
        /// <summary>
        /// Current active child item type
        /// </summary>
        protected ChildItemType ActiveChildType { get { return (this.ActiveChild != null ? this.ActiveChild.ItemType : ChildItemType.None); } }
        /// <summary>
        /// Current active child item
        /// </summary>
        protected ChildItem ActiveChild { get; set; }
        #endregion
        #region Draw
        /// <summary>
        /// Draw this item in standard mode
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            bool isGhost = (drawMode.HasFlag(DrawItemMode.Ghost));
            if (!isGhost)
                this.DrawStandardBackground(e, absoluteBounds);
            else
                this.DrawGhostBackground(e, absoluteBounds);

            bool isTarget = (drawMode.HasFlag(DrawItemMode.DraggedBounds));
            if (isTarget)
                this.DrawChildItems(e);
        }
        /// <summary>
        /// Draw a background of EditableArea in standard mode
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds">Absolute bounds</param>
        protected virtual void DrawStandardBackground(GInteractiveDrawArgs e, Rectangle bounds)
        {
            Point? point = ((this.InteractiveState == GInteractiveState.MouseOver) ? this.CurrentMouseRelativePoint : (Point?)null);
            GPainter.DrawAreaBase(e.Graphics, bounds, this.BackColor.Value, Orientation.Horizontal, this.InteractiveState, point, null);
            GPainter.DrawString(e.Graphics, "GEditableArea", FontInfo.Default, bounds, ContentAlignment.MiddleCenter, Color.Black);
        }
        /// <summary>
        /// Draw a background of EditableArea in Ghost mode
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds">Absolute bounds</param>
        protected virtual void DrawGhostBackground(GInteractiveDrawArgs e, Rectangle bounds)
        {
            GPainter.DrawAreaBase(e.Graphics, bounds, Color.FromArgb(128, this.BackColor.Value), Orientation.Horizontal, GInteractiveState.None, null, null);
        }
        /// <summary>
        /// Draw a editable clips of EditableArea
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DrawChildItems(GInteractiveDrawArgs e)
        {
            // GPainter.DrawButtonBase(e.Graphics, this.Bounds, Color.GreenYellow, this.CurrentState, Orientation.Horizontal, point, null);

            if (this.IsSelected)
            {
                this.DrawSelection(e);
            }

            if (this.InteractiveState == GInteractiveState.MouseOver || this.InteractiveState == GInteractiveState.LeftDown || this.InteractiveState == GInteractiveState.LeftDrag)
            {
                this.ItemBorder.DrawChild(e);
                this.ItemLeft.DrawChild(e);
                this.ItemRight.DrawChild(e);
                this.ItemTop.DrawChild(e);
                this.ItemBottom.DrawChild(e);
                this.ItemTopLeft.DrawChild(e);
                this.ItemTopRight.DrawChild(e);
                this.ItemBottomRight.DrawChild(e);
                this.ItemBottomLeft.DrawChild(e);
            }
        }
        /// <summary>
        /// Draw a selection frame
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DrawSelection(GInteractiveDrawArgs e)
        {
            Rectangle bounds = this.BoundsAbsolute;
            e.Graphics.DrawRectangle(Pens.Magenta, bounds);
        }
        #endregion
    }
    /// <summary>
    /// Mode for draw standard and/or ghost image during drag operation
    /// </summary>
    public enum DragDrawGhostMode
    {
        /// <summary>
        /// As DragOnlyStandard
        /// </summary>
        None,
        /// <summary>
        /// Standard image is on its original bounds, into interactive layer is drawed only ghost
        /// </summary>
        DragWithGhostOnInteractive,
        /// <summary>
        /// Standard image is dragged on interactive layer, on original bounds is drawed ghost image
        /// </summary>
        DragWithGhostAtOriginal,
        /// <summary>
        /// Standard image is dragged on interactive layer, on original bounds is nothing
        /// </summary>
        DragOnlyStandard
    }
    #endregion
}
