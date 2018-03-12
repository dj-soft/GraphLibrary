using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Djs.Common.Components
{
    #region interface IInteractiveItem
    /// <summary>
    /// Define properties and methods for an object, which is interactive on InteractiveControl.
    /// </summary>
    public interface IInteractiveItem
    {
        /// <summary>
        /// Unique ID of object. Is assigned in constructor and is unchanged for whole life of instance.
        /// </summary>
        UInt32 Id { get; }
        /// <summary>
        /// Host control, which is GrandParent of all items.
        /// Only one real WinForm control.
        /// </summary>
        GInteractiveControl Host { get; set; }
        /// <summary>
        /// Parent of this item (its host). Can be null.
        /// Parent is typically an IInteractiveContainer.
        /// Item hosted directly on InteractiveControl has Parent = null.
        /// </summary>
        IInteractiveItem Parent { get; set; }
        /// <summary>
        /// An array of child items of this item. 
        /// Child items are collection of another IInteractiveItem, which coordinates are based in this item. 
        /// This is: where this.ActiveBounds.Location is {200, 100} and child.ActiveBounds.Location is {10, 40}, then child is on Point {210, 140}.
        /// </summary>
        IEnumerable<IInteractiveItem> Childs { get; }
        /// <summary>
        /// Coordinates of this item in their Parent client area, where is Item basically VISIBLE.
        /// This is relative bounds within my Parent.
        /// Appropriate absolute bounds can be calculated via (extension) method IInteractiveItem.GetAbsoluteVisibleBounds(), 
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Coordinates of this item in their Parent client area, where is Item basically ACTIVE.
        /// This is relative bounds within my Parent.
        /// Appropriate absolute bounds can be calculated via (extension) method IInteractiveItem.GetAbsoluteVisibleBounds(), 
        /// </summary>
        Rectangle BoundsActive { get; }
        /// <summary>
        /// Coordinates of area, where CHILD ITEMS of this item have their origin.
        /// This is relative bounds within my Parent, where this item is visible.
        /// Appropriate absolute bounds can be calculated via (extension) method IInteractiveItem.GetAbsoluteVisibleBounds(), 
        /// </summary>
        Rectangle BoundsClient { get; }
        /// <summary>
        /// Interactive style
        /// </summary>
        GInteractiveStyles Style { get; }
        /// <summary>
        /// Is item currently interactive?
        /// When true, then item can be found by mouse, and can be activate by mouse events.
        /// </summary>
        Boolean IsInteractive { get; }
        /// <summary>
        /// Is item currently visible?
        /// When true, then item can be Drawed.
        /// </summary>
        Boolean IsVisible { get; }
        /// <summary>
        /// When true, then item can be interactive (and can be Drawed).
        /// When false, then item can NOT be interactive, but can be Drawed.
        /// Is valid only when IsVisible is true. If IsVisible is false, then item can not be nor visible, nor active.
        /// </summary>
        Boolean IsEnabled { get; }
        /// <summary>
        /// Hold a mouse attention.
        /// When a item is drawed to Interactive layer (in MouseOver, MouseDrag and in other active states), this is: above other subitem, 
        /// then is advisable "hold mouse attention" for this item before other items.
        /// But when active item is drawed under other items, then hold mouse attention is not recommended 
        /// (in example for back area of movable item, before its grips).
        /// </summary>
        Boolean HoldMouse { get; }
        /// <summary>
        /// Order for this item
        /// </summary>
        ZOrder ZOrder { get; }
        /// <summary>
        /// Repaint item to layers after current operation. 
        /// Layers are not combinable. 
        /// Layer None is for invisible, but active items (item then has IsVisible = true, IsEnabled = true, but will not be really drawed).
        /// </summary>
        GInteractiveDrawLayer StandardDrawToLayer { get; }
        /// <summary>
        /// Repaint item to this layers after current operation. Layers are combinable. Layer None can be specified (item will be not repainted).
        /// </summary>
        GInteractiveDrawLayer RepaintToLayers { get; set; }
        /// <summary>
        /// Any Interactive method or any items need repaint all items, after current (interactive) event.
        /// This value can be only set to true. Set to false does not change value of this property.
        /// This value is directly writed to Host control
        /// </summary>
        Boolean RepaintAllItems { get; set; }
        /// <summary>
        /// Return true, when item is active at specified point.
        /// Point is in absolute coordinates (on Control), as in property ActiveBounds.
        /// Item can be interactive on standard rectangle (ActiveBounds), and when "can be active", then algorithm will be call this method IsActiveAtPoint(), as test for activity for specific pixel.
        /// Item can be really active on multiple rectangles, or on inner ellipse, or any GraphicsPath...
        /// </summary>
        /// <param name="point">Point in Control (=this.Host) coordinates, for which are test performed (typically MousePoint)</param>
        /// <returns></returns>
        Boolean IsActiveAtRelativePoint(Point point);
        /// <summary>
        /// This method is called after any interactive change
        /// </summary>
        /// <param name="e"></param>
        void AfterStateChanged(GInteractiveChangeStateArgs e);
        /// <summary>
        /// this method is called on each drag action
        /// </summary>
        /// <param name="e"></param>
        void DragAction(GDragActionArgs e);
        /// <summary>
        /// Called to draw content of this item, and all its Childs.
        /// Parent does not call directly Draw() method for my Childs, this is my responsibility.
        /// </summary>
        /// <param name="e"></param>
        void Draw(GInteractiveDrawArgs e);
    }
    #endregion
    #region interface IDrawItem
    /// <summary>
    /// Interface, definující přítomnost metody, která zajistí vykreslení obsahu libovolného prvku.
    /// </summary>
    public interface IDrawItem
    {
        void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute);
    }
    #endregion
    #region class InteractiveProperties : Bit storage for Interactive Properties of IInteractiveItem objects.
    /// <summary>
    /// InteractiveProperties : Bit storage for Interactive Properties of IInteractiveItem objects.
    /// </summary>
    public class InteractiveProperties : BitStorage
    {
        /// <summary>
        /// Interactive?
        /// </summary>
        public bool Interactive { get { return this.GetBitValue(BitInteractive); } set { this.SetBitValue(BitInteractive, value); } }
        /// <summary>
        /// Visible?
        /// </summary>
        public bool Visible { get { return this.GetBitValue(BitVisible); } set { this.SetBitValue(BitVisible, value); } }
        /// <summary>
        /// Enabled?
        /// </summary>
        public bool Enabled { get { return this.GetBitValue(BitEnabled); } set { this.SetBitValue(BitEnabled, value); } }
        /// <summary>
        /// Hold mouse?
        /// </summary>
        public bool HoldMouse { get { return this.GetBitValue(BitHoldMouse); } set { this.SetBitValue(BitHoldMouse, value); } }
        /// <summary>
        /// Selected?
        /// </summary>
        public bool Selected { get { return this.GetBitValue(BitSelected); } set { this.SetBitValue(BitSelected, value); } }
        /// <summary>
        /// Suppressed events?
        /// </summary>
        public bool SuppressEvents { get { return this.GetBitValue(BitSuppressEvents); } set { this.SetBitValue(BitSuppressEvents, value); } }
        /// <summary>
        /// Default value for new instances
        /// </summary>
        protected override uint DefaultValue
        {
            get { return BitInteractive | BitVisible | BitEnabled ; }
        }
        public const UInt32 BitInteractive = 0x0001;
        public const UInt32 BitVisible = 0x0002;
        public const UInt32 BitEnabled = 0x0004;
        public const UInt32 BitHoldMouse = 0x0008;
        public const UInt32 BitSelected = 0x0010;
        public const UInt32 BitSuppressEvents = 0x0040;
    }
    #endregion
    #region class InteractiveItemExtensions : Extensions for IInteractiveItem
    /// <summary>
    /// InteractiveItemExtensions : Extensions for IInteractiveItem
    /// </summary>
    public static class InteractiveItemExtensions
    {
        #region IInteractiveItem
        /// <summary>
        /// Returns absolute visible bounds of this item (in Host control), via its parents hierarchy
        /// (this.Bounds + this.Parent.GetRelativeClientArea().Location + ... + this.Parent.Parent.Parent....GetRelativeClientArea().Location)
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteVisibleBounds(this IInteractiveItem item)
        {
            Rectangle bounds = item.Bounds;
            Point origin = item.GetAbsoluteOriginPoint();
            return bounds.Add(origin);
        }
        /// <summary>
        /// Store this.Bounds = (absoluteVisibleBounds - this.Parent.AbsoluteVisibleBounds)
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="absoluteVisibleBounds">Value in absolute coordinates</param>
        /// <returns></returns>
        public static void SetAbsoluteVisibleBounds(this IInteractiveItem item, Rectangle absoluteVisibleBounds)
        {
            Point origin = item.GetAbsoluteOriginPoint();
            item.Bounds = absoluteVisibleBounds.Sub(origin);
        }
        /// <summary>
        /// Returns absolute visible bounds of this item (in Host control), via its parents hierarchy
        /// (this.Bounds + this.Parent.GetRelativeClientArea().Location + ... + this.Parent.Parent.Parent....GetRelativeClientArea().Location)
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="bounds">explicitly specified bounds, in same coordinates as this.Bounds</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBounds(this IInteractiveItem item, Rectangle bounds)
        {
            Point origin = item.GetAbsoluteOriginPoint();
            return bounds.Add(origin);
        }
        /// <summary>
        /// Returns absolute visible bounds of this item (in Host control), via its parents hierarchy
        /// (this.Bounds + this.Parent.GetRelativeClientArea().Location + ... + this.Parent.Parent.Parent....GetRelativeClientArea().Location)
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="bounds">explicitly specified bounds, in same coordinates as this.Bounds</param>
        /// <returns></returns>
        public static Rectangle? GetAbsoluteBounds(this IInteractiveItem item, Rectangle? bounds)
        {
            if (!bounds.HasValue) return null;
            Point origin = item.GetAbsoluteOriginPoint();
            return bounds.Value.Add(origin);
        }
        /// <summary>
        /// Returns relative visible point of this item (in Host control), via its parents hierarchy
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="absolutePoint">explicitly specified point, in coordinates of Host (absolute)</param>
        /// <returns></returns>
        public static Point GetRelativePoint(this IInteractiveItem item, Point absolutePoint)
        {
            Point origin = item.GetAbsoluteOriginPoint();
            return absolutePoint.Sub(origin);
        }
        /// <summary>
        /// Returns relative visible point of this item (in Host control), via its parents hierarchy
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="absolutePoint">explicitly specified bounds, in coordinates of Host (absolute)</param>
        /// <returns></returns>
        public static Point? GetRelativePoint(this IInteractiveItem item, Point? absolutePoint)
        {
            if (!absolutePoint.HasValue) return null;
            Point origin = item.GetAbsoluteOriginPoint();
            return absolutePoint.Value.Sub(origin);
        }
        /// <summary>
        /// Returns relative visible bounds of this item (in Host control), via its parents hierarchy
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="absoluteBounds">explicitly specified bounds, in coordinates of Host (absolute)</param>
        /// <returns></returns>
        public static Rectangle GetRelativeBounds(this IInteractiveItem item, Rectangle absoluteBounds)
        {
            Point origin = item.GetAbsoluteOriginPoint();
            return absoluteBounds.Sub(origin);
        }
        /// <summary>
        /// Returns relative visible bounds of this item (in Host control), via its parents hierarchy
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <param name="absoluteBounds">explicitly specified bounds, in coordinates of Host (absolute)</param>
        /// <returns></returns>
        public static Rectangle? GetRelativeBounds(this IInteractiveItem item, Rectangle? absoluteBounds)
        {
            if (!absoluteBounds.HasValue) return null;
            Point origin = item.GetAbsoluteOriginPoint();
            return absoluteBounds.Value.Sub(origin);
        }
        /// <summary>
        /// Returns absolute client area bounds = this.GetAbsoluteVisibleBounds(), reduced by this.ClientBorder
        /// (=item.Bounds.GetClientBounds(item.ClientBorder))
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteClientArea(this IInteractiveItem item)
        {
            Point location = item.GetAbsoluteOriginPoint();
            Rectangle boundsClient = item.BoundsClient;
            return boundsClient.Add(location);
        }
        /// <summary>
        /// Returns absolute coordinates of area (in this.Host), in which this item is active.
        /// If this item has no parent, then return { 0, 0, Host.Width, Host.Height }.
        /// Otherwise return absolute coordinates of this.Parent client area.
        /// When item.Bounds has Location { 0, 0 }, then its absolute location (on Host area) will be on result.Location.
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteInteractiveArea(this IInteractiveItem item)
        {
            Point location = item.GetAbsoluteOriginPoint();
            Rectangle boundsActive = item.BoundsActive;
            return boundsActive.Add(location);
        }
        /// <summary>
        /// Returns absolute coordinates of area, in which this item is contained.
        /// If this item has no parent, then return { 0, 0, Host.Width, Host.Height }.
        /// Otherwise return absolute coordinates of this.Parent client area.
        /// When item.Bounds has Location { 0, 0 }, then its absolute location (on Host area) will be on result.Location.
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteOriginArea(this IInteractiveItem item)
        {
            Point location = item.GetAbsoluteOriginPoint();
            Size size = (item.Parent != null ? item.Parent.BoundsClient.Size : item.Host.Size);
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Returns absolute coordinates of point, which is "origin" for this relative coordinates.
        /// If this item has no parent, then return {0,0}.
        /// Otherwise return absolute coordinates of this.Parent client area.
        /// When item.Bounds has Location { 0, 0 }, then its absolute location (on Host area) will be on result point.
        /// </summary>
        /// <param name="item">current IInteractiveItem item</param>
        /// <returns></returns>
        public static Point GetAbsoluteOriginPoint(this IInteractiveItem item)
        {
            int x = 0;
            int y = 0;
            if (item != null)
            {
                IInteractiveItem i = item;
                while (i.Parent != null)
                {
                    i = i.Parent;
                    Rectangle parentBounds = i.BoundsClient;
                    x += parentBounds.X;
                    y += parentBounds.Y;
                }
            }
            return new Point(x, y);
        }
        /// <summary>
        /// Ensure paint for this item to its standard layer
        /// </summary>
        /// <param name="item"></param>
        public static void Repaint(this IInteractiveItem item)
        {
            if (item != null)
                item.RepaintToLayers = item.StandardDrawToLayer;
        }
        #endregion
    }
    #endregion
    #region Delegates and EventArgs
    /// <summary>
    /// Delegate for handlers of interactive event in GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GInteractiveChangeStateHandler(object sender, GInteractiveChangeStateArgs e);
    /// <summary>
    /// Delegate for handlers of drawing event in GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GInteractiveDrawHandler(object sender, GInteractiveDrawArgs e);
    /// <summary>
    /// Delegate for handlers of property value changed event in GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GPropertyChanged<T>(object sender, GPropertyChangeArgs<T> e);
    /// <summary>
    /// Delegate for handlers for Drawing into User Area
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GUserDrawHandler(object sender, GUserDrawArgs e);
    /// <summary>
    /// Data for handlers of event of change value on any property in GInteractiveControl
    /// </summary>
    public class GPropertyChangeArgs<T> : EventArgs
    {
        public GPropertyChangeArgs(EventSourceType sourceType, T oldvalue, T newValue)
        {
            this.SourceType = sourceType;
            this.OldValue = oldvalue;
            this.NewValue = newValue;
            this.CorrectValue = newValue;
            this.Cancel = false;
        }
        /// <summary>
        /// Specifies the source that caused this change
        /// </summary>
        public EventSourceType SourceType { get; private set; }
        /// <summary>
        /// Hodnota platná před změnou
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Hodnota platná po změně
        /// </summary>
        public T NewValue { get; private set; }
        /// <summary>
        /// Hodnota odpovídající aplikační logice, hodnotu nastavuje eventhandler.
        /// Výchozí hodnota je NewValue.
        /// Komponenta by na tuto korigovanou hodnotu měla reagovat.
        /// </summary>
        public T CorrectValue { get; set; }
        /// <summary>
        /// Požadavek aplikačního kódu na zrušení této změny = ponechat OldValue.
        /// Výchozí hodnota je false.
        /// </summary>
        public bool Cancel { get; set; }
        /// <summary>
        /// true pokud hodnota CorrectValue je odlišná od OldValue, false pokud jsou shodné.
        /// Pokud typ hodnoty není IComparable, pak se vrací true vždy.
        /// </summary>
        public bool IsChangeValue
        {
            get
            {
                IComparable a = this.OldValue as IComparable;
                IComparable b = this.CorrectValue as IComparable;
                if (a == null || b == null) return true;
                return (a.CompareTo(b) != 0);
            }
        }
        /// <summary>
        /// Výsledná hodnota (pokud je Cancel == true, pak OldValue, jinak CorrectValue).
        /// </summary>
        public T ResultValue { get { return (this.Cancel ? this.OldValue : this.CorrectValue); } }
    }
    /// <summary>
    /// Data for handlers of interactive event in GInteractiveControl
    /// </summary>
    public class GInteractiveChangeStateArgs : EventArgs
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="existsItem">true, when CurrentItem is found. Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.</param>
        /// <param name="currentItem">Active item. Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.</param>
        /// <param name="changeState">Type of event (change of status)</param>
        /// <param name="targetState">New state of item (after this event, not before it).</param>
        public GInteractiveChangeStateArgs(bool existsItem, IInteractiveItem currentItem, GInteractiveChangeState changeState, GInteractiveState targetState, Func<Point, bool, IInteractiveItem> searchItemMethod)
        {
            this.ExistsItem = existsItem;
            this.CurrentItem = currentItem;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.MouseAbsolutePoint = null;
            this.MouseRelativePoint = null;
            this.DragOriginBounds = null;
            this.DragToBounds = null;
            this.KeyboardPreviewArgs = null;
            this.KeyboardEventArgs = null;
            this.KeyboardPressEventArgs = null;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="existsItem">true, when CurrentItem is found. Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.</param>
        /// <param name="currentItem">Active item. Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.</param>
        /// <param name="changeState">Type of event (change of status)</param>
        /// <param name="targetState">New state of item (after this event, not before it).</param>
        /// <param name="mouseRelativePoint">Coordinate of mouse relative to CurrentItem.ActiveBounds.Location. Can be a null (in case when ExistsItem is false).</param>
        /// <param name="dragOriginBounds">Original area before current Drag operacion begun (in DragMove events)</param>
        /// <param name="dragToBounds">Target area during Drag operation (in DragMove event)</param>
        public GInteractiveChangeStateArgs(bool existsItem, IInteractiveItem currentItem, GInteractiveChangeState changeState, GInteractiveState targetState, Func<Point, bool, IInteractiveItem> searchItemMethod, Point? mouseAbsolutePoint, Point? mouseRelativePoint, Rectangle? dragOriginBounds, Rectangle? dragToBounds)
        {
            this.ExistsItem = existsItem;
            this.CurrentItem = currentItem;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.MouseAbsolutePoint = mouseAbsolutePoint;
            this.MouseRelativePoint = mouseRelativePoint;
            this.DragOriginBounds = dragOriginBounds;
            this.DragToBounds = dragToBounds;
            this.KeyboardPreviewArgs = null;
            this.KeyboardEventArgs = null;
            this.KeyboardPressEventArgs = null;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="existsItem">true, when CurrentItem is found. Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.</param>
        /// <param name="currentItem">Active item. Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.</param>
        /// <param name="changeState">Type of event (change of status)</param>
        /// <param name="targetState">New state of item (after this event, not before it).</param>
        /// <param name="previewArgs">Keyboard Preview Data</param>
        /// <param name="keyArgs">Keyboard Events Data</param>
        /// <param name="keyPressArgs">Keyboard KeyPress data</param>
        public GInteractiveChangeStateArgs(bool existsItem, IInteractiveItem currentItem, GInteractiveChangeState changeState, GInteractiveState targetState, Func<Point, bool, IInteractiveItem> searchItemMethod, PreviewKeyDownEventArgs previewArgs, KeyEventArgs keyArgs, KeyPressEventArgs keyPressArgs)
        {
            this.ExistsItem = existsItem;
            this.CurrentItem = currentItem;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.MouseAbsolutePoint = null;
            this.MouseRelativePoint = null;
            this.DragOriginBounds = null;
            this.DragToBounds = null;
            this.KeyboardPreviewArgs = previewArgs;
            this.KeyboardEventArgs = keyArgs;
            this.KeyboardPressEventArgs = keyPressArgs;
        }
        #endregion
        #region Input properties (read-only)
        /// <summary>
        /// true, when CurrentItem is found.
        /// Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.
        /// </summary>
        public bool ExistsItem { get; protected set; }
        /// <summary>
        /// Active item.
        /// Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.
        /// </summary>
        public IInteractiveItem CurrentItem { get; protected set; }
        /// <summary>
        /// Type of event (change of status)
        /// </summary>
        public GInteractiveChangeState ChangeState { get; protected set; }
        /// <summary>
        /// New state of item (after this event, not before it).
        /// </summary>
        public GInteractiveState TargetState { get; protected set; }
        /// <summary>
        /// Method for search for IInteractiveItem at specified Absolute point
        /// </summary>
        protected Func<Point, bool, IInteractiveItem> SearchItemMethod;
        /// <summary>
        /// Coordinate of mouse on coordinates of Control.
        /// Can be a null (in keyboard actions).
        /// </summary>
        public Point? MouseAbsolutePoint { get; protected set; }
        /// <summary>
        /// Coordinate of mouse relative to CurrentItem.Bounds.Location.
        /// Can be a null (in case when ExistsItem is false or in keyboard actions).
        /// </summary>
        public Point? MouseRelativePoint { get; protected set; }
        /// <summary>
        /// Origin area (Bounds of current item) before current Drag operation begun (in DragMove event)
        /// </summary>
        public Rectangle? DragOriginBounds { get; protected set; }
        /// <summary>
        /// Target area during Drag operation (in DragMove event) calculated from DragOriginBounds and mouse move (without limitations).
        /// Real target bounds can be other than this unlimited bounds.
        /// </summary>
        public Rectangle? DragToBounds { get; protected set; }
        /// <summary>
        /// Keyboard Preview data
        /// </summary>
        public PreviewKeyDownEventArgs KeyboardPreviewArgs { get; protected set; }
        /// <summary>
        /// Keyboard events data
        /// </summary>
        public KeyEventArgs KeyboardEventArgs { get; protected set; }
        /// <summary>
        /// Keyboard KeyPress data
        /// </summary>
        public KeyPressEventArgs KeyboardPressEventArgs { get; protected set; }
        #endregion
        #region Find Item at location (explicit, current)
        /// <summary>
        /// Item, which BoundAbsolute is on (this.MouseCurrentAbsolutePoint).
        /// </summary>
        public IInteractiveItem ItemAtCurrentMousePoint { get { return this._ItemAtCurrentMousePointGet(); } }
        /// <summary>
        /// Returns _ItemAtCurrentMousePointResult (on first calling perform search).
        /// </summary>
        /// <returns></returns>
        private IInteractiveItem _ItemAtCurrentMousePointGet()
        {
            if (!this._ItemAtCurrentMousePointSearched)
            {
                if (this.MouseAbsolutePoint.HasValue)
                    this._ItemAtCurrentMousePointResult = this.FindItemAtPoint(this.MouseAbsolutePoint.Value);
                this._ItemAtCurrentMousePointSearched = true;
            }
            return this._ItemAtCurrentMousePointResult;
        }
        /// <summary>
        /// Item at MouseCurrentAbsolutePoint, after search
        /// </summary>
        private IInteractiveItem _ItemAtCurrentMousePointResult;
        /// <summary>
        /// Search for _ItemAtCurrentMousePointResult was processed?
        /// </summary>
        private bool _ItemAtCurrentMousePointSearched;
        /// <summary>
        /// This method returns first top-most object, which BoundAbsolute is on specified point.
        /// Can return a null (when at point is not object, only host container).
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint)
        {
            return this.FindItemAtPoint(absolutePoint, false);
        }
        /// <summary>
        /// This method returns first top-most object, which BoundAbsolute is on specified point.
        /// Can return a null (when at point is not object, only host container).
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint, bool withDisabled)
        {
            if (this.SearchItemMethod == null) return null;
            return this.SearchItemMethod(absolutePoint, withDisabled);
        }
        #endregion
        #region Results (from event to control)
        /// <summary>
        /// User defined point during Drag operation.
        /// User (an IInteractiveItem) can set any point in event LeftDragBegin / RightDragBegin;
        /// then GInteractiveControl will be calculated appropriate moved point during Drag, 
        /// and this "dragged" point coordinates are stored to this property (UserDragPoint) before call event LeftDragMove / RightDragMove.
        /// When user in event DragBegin does not set any location (null value), then in event DragMove will be in this property null value.
        /// For other events this property does not have any meaning.
        /// </summary>
        public Point? UserDragPoint { get; set; }
        /// <summary>
        /// Required Cursor type. Null = default. Control detect change from curent state, CursorType can be set to required value everytime.
        /// </summary>
        public SysCursorType? RequiredCursorType { get; set; }
        /// <summary>
        /// After this event is need to Draw all items on control, not only current item.
        /// This value can be set only to true. Attempt to set this property to false does not change value from true to false, a value true will remaining in property.
        /// </summary>
        public bool RepaintAllItems { get { return this._RepaintAllItems; } set { if (value) this._RepaintAllItems = true; } } private bool _RepaintAllItems;
        /// <summary>
        /// Data for tooltip (autoinitializing property)
        /// </summary>
        public ToolTipData ToolTipData { get { if (this._ToolTipData == null) this._ToolTipData = new ToolTipData(); return this._ToolTipData; } set { this._ToolTipData = value; } } private ToolTipData _ToolTipData;
        /// <summary>
        /// true when has data for ToolTip, false when has not.
        /// </summary>
        internal bool HasToolTipData { get { return (this._ToolTipData != null); } } 
        /// <summary>
        /// true when this.ToolTipData has valid data for drawing tooltip
        /// </summary>
        public bool ToolTipIsValid { get { return (this._ToolTipData != null && this._ToolTipData.IsValid); } }
        #endregion
    }
    /// <summary>
    /// Data for handlers of drag item events (drag process, drag drop; drag this object on another object, or drag another object on this object)
    /// </summary>
    public class GDragActionArgs : EventArgs
    {
        #region Constructors
        public GDragActionArgs(GInteractiveChangeStateArgs changeArgs, DragActionType dragAction, Point mouseDownAbsolutePoint, Point? mouseCurrentAbsolutePoint)
        {
            this._ChangeArgs = changeArgs;
            this.DragAction = dragAction;
            this.MouseDownAbsolutePoint = mouseDownAbsolutePoint;
            this.MouseCurrentAbsolutePoint = mouseCurrentAbsolutePoint;
        }
        private GInteractiveChangeStateArgs _ChangeArgs;
        #endregion
        #region Input properties (read-only)
        /// <summary>
        /// Type of event (change of status)
        /// </summary>
        public GInteractiveChangeState ChangeState { get { return this._ChangeArgs.ChangeState; } }
        /// <summary>
        /// New state of item (after this event, not before it).
        /// </summary>
        public GInteractiveState TargetState { get { return this._ChangeArgs.TargetState; } }
        /// <summary>
        /// Origin area (Bounds of current item) before current Drag operation begun (in DragMove event)
        /// </summary>
        public Rectangle? DragOriginBounds { get { return this._ChangeArgs.DragOriginBounds; } }
        /// <summary>
        /// Target area during Drag operation (in DragMove event) calculated from DragOriginBounds and mouse move (without limitations).
        /// Real target bounds can be other than this unlimited bounds.
        /// </summary>
        public Rectangle? DragToBounds { get { return this._ChangeArgs.DragToBounds; } }

        /// <summary>
        /// Type of action (drag this object, or drag another object over this object)
        /// </summary>
        public DragActionType DragAction { get; protected set; }
        /// <summary>
        /// Origin coordinate of mouse, where was MouseDown registered, on coordinates of Control.
        /// Can not be a null value.
        /// </summary>
        public Point MouseDownAbsolutePoint { get; protected set; }
        /// <summary>
        /// Current coordinate of mouse on coordinates of Control.
        /// Can be a null value, when DragAction = DragThisCancel!
        /// </summary>
        public Point? MouseCurrentAbsolutePoint { get; protected set; }
        #endregion
        #region Find Item at location (explicit, current)
        /// <summary>
        /// Item, which BoundAbsolute is on (this.MouseCurrentAbsolutePoint).
        /// </summary>
        public IInteractiveItem ItemAtCurrentMousePoint { get { return this._ChangeArgs.ItemAtCurrentMousePoint; } }
        /// <summary>
        /// This method returns first top-most object, which BoundAbsolute is on specified point.
        /// Can return a null (when at point is not object, only host container).
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint)
        {
            return this._ChangeArgs.FindItemAtPoint(absolutePoint);
        }
        /// <summary>
        /// This method returns first top-most object, which BoundAbsolute is on specified point.
        /// Can return a null (when at point is not object, only host container).
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint, bool withDisabled)
        {
            return this._ChangeArgs.FindItemAtPoint(absolutePoint, withDisabled);
        }
        #endregion
        #region Results (from event to control)
        /// <summary>
        /// Required Cursor type. Null = default. Control detect change from curent state, CursorType can be set to required value everytime.
        /// Object can set cursor by own state.
        /// </summary>
        public SysCursorType? RequiredCursorType { get { return this._ChangeArgs.RequiredCursorType; } set { this._ChangeArgs.RequiredCursorType = value; } }
        /// <summary>
        /// Object can enable / disable dragging.
        /// </summary>
        public bool? DragDisable { get; set; }
        #endregion
    }
    /// <summary>
    /// Data for handlers of drawing event in GInteractiveControl
    /// </summary>
    public class GInteractiveDrawArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="graphics">An Graphics object to draw on</param>
        /// <param name="drawLayer">Layer, currently drawed.</param>
        public GInteractiveDrawArgs(Graphics graphics, GInteractiveDrawLayer drawLayer)
        {
            this.Graphics = graphics;
            this.DrawLayer = drawLayer;
        }
        /// <summary>
        /// An Graphics object to draw on
        /// </summary>
        public Graphics Graphics { get; private set; }
        /// <summary>
        /// Layer, currently drawed.
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get; private set; }
        /// <summary>
        /// Bounds, to which is Graphics clipped.
        /// Is equal to all Parent AbsoluteBounds intersection.
        /// </summary>
        public Rectangle ClipBounds { get; set; }
        /// <summary>
        /// Returns intersection between this.ClipBounds (this is in absolute coordinates) with specified absoluteBounds.
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <returns></returns>
        public Rectangle GetClip(Rectangle absoluteBounds)
        {
            Rectangle clip = this.ClipBounds;
            clip.Intersect(absoluteBounds);
            return clip;
        }
        /// <summary>
        /// Create intersect between specified absoluteBounds (any user area) and this.ClipBounds (owner area for current item),
        /// and this intersection apply to this.Graphics.
        /// </summary>
        /// <param name="absoluteBounds"></param>
        public void GraphicsClipWith(Rectangle absoluteBounds)
        {
            Rectangle clip = this.GetClip(absoluteBounds);
            this.Graphics.SetClip(clip);
        }
        /// <summary>
        /// Create intersect between specified absoluteBounds (any user area) and this.ClipBounds (owner area for current item),
        /// and this intersection apply to this.Graphics.
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="onlyForLayer"></param>
        public void GraphicsClipWith(Rectangle absoluteBounds, GInteractiveDrawLayer onlyForLayer)
        {
            if (this.DrawLayer == onlyForLayer)
            {
            Rectangle clip = this.GetClip(absoluteBounds);
            this.Graphics.SetClip(clip);
            }
        }
    }
    /// <summary>
    /// Argument for UserDraw event
    /// </summary>
    public class GUserDrawArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="graphics">An Graphics object to draw on</param>
        /// <param name="drawLayer">Layer, currently drawed</param>
        /// <param name="userAbsoluteBounds">Area to draw (VisibleAbsoluteBounds), where item is drawed, in coordinates absolute to Host control (GInteractiveControl)</param>
        public GUserDrawArgs(Graphics graphics, GInteractiveDrawLayer drawLayer, Rectangle userAbsoluteBounds)
        {
            this.Graphics = graphics;
            this.DrawLayer = drawLayer;
            this.UserAbsoluteBounds = userAbsoluteBounds;
        }
        /// <summary>
        /// An Graphics object to draw on.
        /// Graphics is clipped to this.UserAbsoluteBounds, thus cannot draw outside this area.
        /// </summary>
        public Graphics Graphics { get; private set; }
        /// <summary>
        /// Layer, currently drawed.
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get; private set; }
        /// <summary>
        /// Area to draw (VisibleAbsoluteBounds), where item is drawed, in coordinates absolute to Host control (GInteractiveControl).
        /// Graphics is clipped to this.UserAbsoluteBounds, thus cannot draw outside this area.
        /// </summary>
        public Rectangle UserAbsoluteBounds { get; private set; }
    }
    #endregion
    #region Enums ZOrder, GInteractiveStyles, GInteractiveState, GInteractiveChangeState, GInteractiveDrawLayer, DragResponseType, ProcessAction, EventSourceType
    /// <summary>
    /// Z-Order position of item.
    /// Order from bottom to top is: OnBackground - BelowStandard - Standard - AboveStandard - OnTop.
    /// </summary>
    public enum ZOrder : int
    {
        /// <summary>
        /// Standard position
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Above all items in Standard order
        /// </summary>
        AboveStandard = 10,
        /// <summary>
        /// On top over all other order
        /// </summary>
        OnTop = 20,
        /// <summary>
        /// Below all items in Standard order
        /// </summary>
        BelowStandard = -10,
        /// <summary>
        /// On background below all other order
        /// </summary>
        OnBackground = -20
    }
    /// <summary>
    /// Interactive styles of an interactive object.
    /// Specifies action to be taken on object.
    /// </summary>
    [Flags]
    public enum GInteractiveStyles : int
    {
        None = 0,
        /// <summary>
        /// Area is active for mouse move
        /// </summary>
        Mouse = 0x0001,
        /// <summary>
        /// Area can be clicked (left, right)
        /// </summary>
        Click = 0x0002,
        /// <summary>
        /// Area can be double-clicked (left, right)
        /// </summary>
        DoubleClick = 0x0004,
        /// Area can be long-clicked (left, right) (MouseDown - long pause - MouseUp)
        /// </summary>
        LongClick = 0x0004,
        /// <summary>
        /// Call event MouseOver for MouseMove for each pixel (none = call only MouseEnter and MouseLeave)
        /// </summary>
        CallMouseOver = 0x0010,
        /// <summary>
        /// Area can be dragged
        /// </summary>
        Drag = 0x0020,
        /// <summary>
        /// Enables move of item
        /// </summary>
        DragMove = 0x0100,
        /// <summary>
        /// Enables resize of item in X axis
        /// </summary>
        DragResizeX = 0x0200,
        /// <summary>
        /// Enables resize of item in Y axis
        /// </summary>
        DragResizeY = 0x0400,
        /// <summary>
        /// Item can be selected
        /// </summary>
        Select = 0x1000,
        /// <summary>
        /// Item can be dragged only in selected state
        /// </summary>
        DragOnlySelected = 0x2000,
        /// <summary>
        /// During drag and resize operation: Draw ghost image as Interactive layer (=Ghost is moved with mouse on Interactive layer, original image is on Standard layer)
        /// Without values (DragDrawGhostInteractive and DragDrawGhostOriginal) is during Drag operation item drawed to Interactive layer, and Original bounds are empty (no draw to Standard layer)
        /// </summary>
        DragDrawGhostInteractive = 0x4000,
        /// <summary>
        /// During drag and resize operation: Draw ghost image into Standard layer (=Standard image of control is moved with mouse on Interactive layer, Ghost image is painted on Standard layer)
        /// Without values (DragDrawGhostInteractive and DragDrawGhostOriginal) is during Drag operation item drawed to Interactive layer, and Original bounds are empty (no draw to Standard layer)
        /// </summary>
        DragDrawGhostOriginal = 0x8000,
        /// <summary>
        /// Can accept an keyboard input.
        /// Note: There is no need to set the KeyboardInput flag to accept the cancellation of Drag action (which is: Escape key during Drag)
        /// </summary>
        KeyboardInput = 0x00010000,
        /// <summary>
        /// Enables resize of item in X and Y axis
        /// </summary>
        DragResize = DragResizeX | DragResizeY | Drag,
        /// <summary>
        /// Enables move and resize of item
        /// </summary>
        DragMoveResize = DragMove | DragResizeX | DragResizeY | Drag,
        /// <summary>
        /// Standard Mouse = Mouse | Click | LongClick | DoubleClick | Drag | Select. 
        /// Not contain CallMouseOver.
        /// </summary>
        StandardMouseInteractivity = Mouse | Click | LongClick | DoubleClick | Drag | Select,
        /// <summary>
        /// All Mouse = StandardMouseInteractivity + CallMouseOver (= Mouse | Click | LongClick | DoubleClick | Drag | Select | CallMouseOver).
        /// </summary>
        AllMouseInteractivity = StandardMouseInteractivity | CallMouseOver,
        /// <summary>
        /// StandardKeyboardInetractivity = StandardMouseInteractivity except Drag + KeyboardInput (= Mouse | Click | LongClick | DoubleClick | Select | KeyboardInput)
        /// </summary>
        StandardKeyboardInteractivity = Mouse | Click | LongClick | DoubleClick | Select | KeyboardInput
    }
    /// <summary>
    /// State of item by mouse activity. Not change of state (eg. Click, DragBegin and so on), only static status (Dragging, MouseDown, MouseOver, ...)
    /// </summary>
    public enum GInteractiveState
    {
        /// <summary>
        /// Not defined - do not use in real algorithm
        /// </summary>
        None = 0,
        /// <summary>
        /// Enabled, without mouse, but ready to activate
        /// </summary>
        Enabled,
        /// <summary>
        /// Disabled, not mouse-active
        /// </summary>
        Disabled,
        /// <summary>
        /// With mouse over
        /// </summary>
        MouseOver,
        /// <summary>
        /// With left mouse button down (not dragging)
        /// </summary>
        LeftDown,
        /// <summary>
        /// With right mouse button down (not dragging)
        /// </summary>
        RightDown,
        /// <summary>
        /// Dragging by left mouse button
        /// </summary>
        LeftDrag,
        /// <summary>
        /// Dragging by right mouse button
        /// </summary>
        RightDrag
    }
    /// <summary>
    /// State and Change of state by mouse activity, this is: static state and change of state from one static state to another.
    /// </summary>
    public enum GInteractiveChangeState
    {
        None = 0,
        MouseEnter,
        /// <summary>
        /// Is called only for item with style containing CallMouseOver !
        /// </summary>
        MouseOver,
        MouseOverDisabled,
        MouseLeave,
        MouseEnterSubItem,
        MouseLeaveSubItem,
        LeftDown,
        LeftDragBegin,
        /// <summary>
        /// Called on every pixel during Left-MouseDrag action.
        /// </summary>
        LeftDragMove,
        /// <summary>
        /// Called on Escape key during Left-MouseDrag action.
        /// Item must be positioned on Original location.
        /// Original Bounds are present in args.
        /// After this event will be called event LeftDragEnd (immediatelly), but does not call LeftUp event.
        /// </summary>
        LeftDragCancel,
        /// <summary>
        /// Called on MouseUp on Commit after Left-MouseDrag action.
        /// Item is now moved to new location.
        /// After this event will be called event LeftDragEnd (immediatelly), but does not call LeftUp event.
        /// </summary>
        LeftDragDone,
        /// <summary>
        /// Called on MouseUp after Left-MouseDrag action.
        /// Is called after LeftDragDone (=OK) and after LeftDragCancel (=Cancel).
        /// </summary>
        LeftDragEnd,
        LeftUp,
        LeftClick,
        LeftLongClick,
        LeftDoubleClick,
        RightDown,
        RightDragBegin,
        /// <summary>
        /// Called on every pixel during Right-MouseDrag action.
        /// </summary>
        RightDragMove,
        /// <summary>
        /// Called on Escape key during Right-MouseDrag action.
        /// Item must be positioned on Original location.
        /// Original Bounds are present in args.
        /// After this event will be called event RightDragEnd (immediatelly), but does not call RightUp event.
        /// </summary>
        RightDragCancel,
        /// <summary>
        /// Called on MouseUp on Commit after Right-MouseDrag action.
        /// Item is now moved to new location.
        /// After this event will be called event RightDragEnd (immediatelly), but does not call RightUp event.
        /// </summary>
        RightDragDone,
        /// <summary>
        /// Called on MouseUp after Right-MouseDrag action.
        /// Is called after RightDragDone (=OK) and after RightDragCancel (=Cancel).
        /// </summary>
        RightDragEnd,
        RightUp,
        RightClick,
        RightLongClick,
        RightDoubleClick,
        KeyboardFocusEnter,
        KeyboardPreviewKeyDown,
        KeyboardKeyDown,
        KeyboardKeyUp,
        KeyboardKeyPress,
        KeyboardFocusLeave,
        WheelUp,
        WheelDown
    }
    /// <summary>
    /// Layers to draw
    /// </summary>
    [Flags]
    public enum GInteractiveDrawLayer : int
    {
        /// <summary>
        /// This layer is not drawed. Objects on this layer are not visible.
        /// </summary>
        None = 0,
        /// <summary>
        /// Standard layer (static image)
        /// </summary>
        Standard = 1,
        /// <summary>
        /// Interactive layer (image from static layer, during drag operation)
        /// </summary>
        Interactive = 2,
        /// <summary>
        /// Dynamic layer (lines above standard and interactive layers)
        /// </summary>
        Dynamic = 4
    }
    /// <summary>
    /// Action durig drag one object over another objects
    /// </summary>
    public enum DragActionType : int
    {
        None,
        DragThisStart,
        DragThisMove,
        DragThisCancel,
        DragThisDrop,
        DragThisEnd,
        DragAnotherMove,
        DragAnotherDrop
    }
    /// <summary>
    /// Type of response to drag event on object
    /// </summary>
    public enum DragResponseType
    {
        /// <summary>
        /// No response
        /// </summary>
        None,
        /// <summary>
        /// Response after DragEnd
        /// </summary>
        AfterDragEnd,
        /// <summary>
        /// Response on each change in DragMove
        /// </summary>
        InDragMove
    }
    /// <summary>
    /// Action to be taken during SetValue() method
    /// </summary>
    [Flags]
    public enum ProcessAction
    {
        None = 0,
        /// <summary>
        /// Recalc and store new Value
        /// </summary>
        RecalcValue = 0x0001,
        /// <summary>
        /// Recalc and store new Scale
        /// </summary>
        RecalcScale = 0x0002,
        /// <summary>
        /// Recalc Inner Data (Arrangement, CurrentSet from Scale on Axis; InnerBounds on ScrollBar; and so on)
        /// </summary>
        RecalcInnerData = 0x0004,
        /// <summary>
        /// Prepare Inner Items (Ticks, SubItems) from Value and other data (Arrangement, CurrentSet on Axis; SubItems on ScrollBar; and so on)
        /// </summary>
        PrepareInnerItems = 0x0008,
        /// <summary>
        /// Call all events about current changing of property (during Drag process)
        /// </summary>
        CallChangingEvents = 0x0010,
        /// <summary>
        /// Call all events about change of property
        /// </summary>
        CallChangedEvents = 0x0020,
        /// <summary>
        /// Call event for Synchronize slave objects
        /// </summary>
        CallSynchronizeSlave = 0x0100,
        /// <summary>
        /// Call Draw events
        /// </summary>
        CallDraw = 0x0200,
        /// <summary>
        /// Take all actions
        /// </summary>
        All = 0xFFFF,

        /// <summary>
        /// Combined value for Silent SetBounds: (RecalcValue | PrepareInnerItems), not CallChangedEvents nor CallDraw
        /// </summary>
        SilentBoundActions = RecalcValue | PrepareInnerItems,
        /// <summary>
        /// Combined value for Silent SetValue: (RecalcValue | RecalcInnerData | PrepareInnerItems), not CallChangedEvents nor CallDraw
        /// </summary>
        SilentValueActions = RecalcValue | RecalcScale | RecalcInnerData | PrepareInnerItems,
        /// <summary>
        /// Action during Drag an value interactively. 
        /// Contain only RecalcValue and CallChangingEvents action (RecalcValue = Align value to ValueRange, CallChangingEvents = change is in process).
        /// Do not contain actions: RecalcValue nor RecalcInnerData, PrepareInnerItems, CallDraw.
        /// </summary>
        DragValueActions = RecalcValue | CallChangingEvents,
        /// <summary>
        /// Combined value for Silent SetValue: (RecalcValue | RecalcInnerData | PrepareInnerItems) | CallChangedEvents, not RecalcInnerData + PrepareInnerItems
        /// </summary>
        SilentValueDrawActions = SilentValueActions | CallDraw
    }
    /// <summary>
    /// Specifies the source that caused this change
    /// </summary>
    [Flags]
    public enum EventSourceType
    {
        None=0,
        /// <summary>
        /// Change to Value property directly (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueChanging = 0x0001,
        /// <summary>
        /// Change to Value property directly (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueChange = 0x0002,
        /// <summary>
        /// Change to ValueRange property (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueRangeChange = 0x0010,
        /// <summary>
        /// Change to Scale property (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueScaleChange = 0x0020,
        /// <summary>
        /// Change to ScaleRange property (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueScaleRangeChange = 0x0040,
        /// <summary>
        /// Change of control visual size
        /// </summary>
        BoundsChange = 0x0100,
        /// <summary>
        /// Change of control Orientation
        /// </summary>
        OrientationChange = 0x0200,
        /// <summary>
        /// Application code is source of change (set new value to property)
        /// </summary>
        ApplicationCode = 0x1000,
        /// <summary>
        /// Interactive action is source of change (user was entered / dragged new value).
        /// Change of value is still in process (Drag). After DragEnd will be sent event with source = InteractiveChanged.
        /// </summary>
        InteractiveChanging = 0x1000,
        /// <summary>
        /// Interactive action is source of change (user was entered / dragged new value)
        /// </summary>
        InteractiveChanged = 0x2000
    }
    #endregion
}
