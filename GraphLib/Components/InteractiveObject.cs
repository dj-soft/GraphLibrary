using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Djs.Common.Components
{
    #region class InteractiveObject : abstract ancestor for all common interactive items
    /// <summary>
    /// InteractiveObject : abstract ancestor for all common interactive items
    /// (optionally with SubItems = any number of small areas, typically control points)
    /// </summary>
    public abstract class InteractiveObject : IInteractiveItem
    {
        #region Public properties
        /// <summary>
        /// Host of this control, which is GrandParent of all items.
        /// </summary>
        public GInteractiveControl Host { get { return (this._Host != null ? this._Host : (this.Parent != null ? this.Parent.Host : null)); } set { this._Host = value; } } private GInteractiveControl _Host;
        /// <summary>
        /// Parent of this item (its host). Can be null.
        /// Parent is typically an IInteractiveContainer.
        /// </summary>
        public IInteractiveItem Parent { get; protected set; }
        /// <summary>
        /// An array of sub-items in this item
        /// </summary>
        protected virtual IEnumerable<IInteractiveItem> Childs { get { return null; } }

        /// <summary>
        /// Coordinates of this item in their Parent client area.
        /// This is relative bounds within my Parent, where this item is visible.
        /// Appropriate absolute bounds can be calculated via (extension) method IInteractiveItem.GetAbsoluteVisibleBounds().
        /// Setting a new value into this property caused calling All ProcesActions (see method SetBounds()).
        /// </summary>
        public virtual Rectangle Bounds 
        {
            get { return this._Bounds; }
            set { this.SetBounds(value, ProcessAction.All, EventSourceType.BoundsChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Coordinates of this item in their Parent client area.
        /// This is relative bounds within my Parent, where this item is visible.
        /// Appropriate absolute bounds can be calculated via (extension) method IInteractiveItem.GetAbsoluteVisibleBounds().
        /// Setting a new value into this property caused calling only ProcesActions: RecalcValue and PrepareInnerItems, not call All ProcesActions (see method SetBounds()).
        /// </summary>
        public virtual Rectangle BoundsSilent
        {
            get { return this._Bounds; }
            set { this.SetBounds(value, ProcessAction.SilentBoundActions, EventSourceType.BoundsChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Přídavek k this.Bounds, který určuje přesah aktivity tohoto prvku do jeho okolí.
        /// Kladné hodnoty v Padding zvětšují aktivní plochu nad rámec this.Bounds, záporné aktivní plochu zmenšují.
        /// Aktivní souřadnice prvku tedy jsou this.Bounds.Add(this.ActivePadding), kde Add() je extension metoda.
        /// </summary>
        public virtual Padding? ActivePadding
        {
            get { return this.__ActivePadding; }
            set
            {
                this.__ActivePadding = value;
                this.BoundsInvalidate();
            }
        }
        /// <summary>
        /// Absolutní souřadnice (vzhledem k Controlu Host), na kterých je tento prvek aktivní.
        /// Souřadnice ve výchozím stavu určuje proces vykreslování, kdy jsou určeny jak offset souřadnic (absolutní počátek parenta),
        /// tak je určen Intersect viditelných oblastí ze všech parentů = tím je dán vizuální Clip, do něhož se prvek promítá.
        /// Tato hodnota je při vykreslování uložena do this.AbsoluteInteractiveBounds.
        /// Následně při testech interaktivity (hledání prvku pod myší) je tato souřadnice využívána.
        /// Prvek sám může ve své metodě Draw() nastavit hodnotu AbsoluteInteractiveBounds jinak, nebo může nastavit null = prvek není aktivní.
        /// Tyto souřadnice by neměly být dopočítávány, prostě jsou uloženy a testovány.
        /// </summary>
        public virtual Rectangle? AbsoluteInteractiveBounds
        {
            get { return this.__AbsoluteInteractiveBounds; }
            set { this.__AbsoluteInteractiveBounds = value; }
        }
        /// <summary>
        /// Inner border between this Bounds and Childs area.
        /// Client area is (Bounds.Left + ClientBorder.Left, Bounds.Top + ClientBorder.Top, Bounds.Width - ClientBorder.Horizontal, Bounds.Height - ClientBorder.Vertical)
        /// </summary>
        public virtual Padding ClientBorder 
        {
            get { return this.__ClientBorder; }
            set
            {
                this.__ClientBorder = value;
                this.BoundsInvalidate();
            }
        }

        /// <summary>
        /// Back color
        /// </summary>
        public virtual Color BackColor 
        {
            get { return this._BackColor; }
            set { this._BackColor = value; } 
        }
        protected Color _BackColor = Skin.Control.BackColor;

        /// <summary>
        /// Order for this item
        /// </summary>
        public ZOrder ZOrder { get { return this.__ZOrder; } set { this.__ZOrder = value; } } private ZOrder __ZOrder = ZOrder.Standard;
        /// <summary>
        /// Refresh whole control
        /// </summary>
        public virtual void Refresh()
        {
            this.Repaint();
            if (this.Host != null)
                this.Host.Refresh();
        }
        /// <summary>
        /// Interactive style
        /// </summary>
        protected virtual GInteractiveStyles Style { get { return this._Style; } set { this._Style = value; } } private GInteractiveStyles _Style = GInteractiveStyles.StandardMouseInteractivity;
        /// <summary>
        /// true when this object has keyboard focus
        /// </summary>
        protected virtual bool HasFocus { get { return this._HasFocus; } set { this._HasFocus = value; } } private bool _HasFocus;

        /// <summary>
        /// Mode for drawing a ghost during Drag operation. Is converted from/to this.Style value.
        /// </summary>
        protected DragDrawGhostMode DragDrawGhostMode
        {
            get
            {
                GInteractiveStyles style = this.Style;
                if (style.HasFlag(GInteractiveStyles.DragDrawGhostInteractive)) return DragDrawGhostMode.DragWithGhostOnInteractive;
                if (style.HasFlag(GInteractiveStyles.DragDrawGhostOriginal)) return DragDrawGhostMode.DragWithGhostAtOriginal;
                return DragDrawGhostMode.DragOnlyStandard; 
            }
            set 
            {
                int storage = (int)this.Style;
                bool ghostInteractive = (value == DragDrawGhostMode.DragWithGhostOnInteractive);
                storage = BitStorage.SetBitValue(storage, (int)GInteractiveStyles.DragDrawGhostInteractive, ghostInteractive);
                bool ghostAtStandard = (value == DragDrawGhostMode.DragWithGhostAtOriginal);
                storage = BitStorage.SetBitValue(storage, (int)GInteractiveStyles.DragDrawGhostOriginal, ghostAtStandard);
                this.Style = (GInteractiveStyles)storage;
            }
        }
        #endregion
        #region Bounds support
        /// <summary>
        /// Set this._Bounds = bounds (when new value is not equal to current value: bounds != this._Bounds).
        /// If actions contain CallChangedEvents, then call CallBoundsChanged() method.
        /// If actions contain CallDraw, then call CallDrawRequest() method.
        /// return true = was change, false = not change (bounds == this._Bounds).
        /// </summary>
        /// <param name="bounds">New bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        public virtual bool SetBounds(Rectangle bounds, ProcessAction actions, EventSourceType eventSource)
        {
            Rectangle oldBounds = this._Bounds;
            Rectangle newBounds = bounds;
            if (oldBounds == newBounds) return false;

            this._Bounds = bounds;
            this.CallActionsAfterBoundsChanged(oldBounds, newBounds, ref actions, eventSource);
            return true;
        }
        /// <summary>
        /// Call all actions after Bounds chaged.
        /// Call: allways: SetBoundsAfterChange(); by actions: SetBoundsRecalcInnerData(); SetBoundsPrepareInnerItems(); CallBoundsChanged(); CallDrawRequest();
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void CallActionsAfterBoundsChanged(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.SetBoundsAfterChange(oldBounds, newBounds, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.RecalcInnerData))
                this.SetBoundsRecalcInnerData(oldBounds, newBounds, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallBoundsChanged(oldBounds, newBounds, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, without any conditions (even if action is None).
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetBoundsAfterChange(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, only when action RecalcInnerData is specified.
        /// Recalculate SubItems bounds after change this.Bounds.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetBoundsRecalcInnerData(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, only when action PrepareInnerItems is specified.
        /// Recalculate SubItems bounds after change this.Bounds.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Coordinates of this item (this.Bounds) in absolute value. This is: relative on Host control.
        /// </summary>
        public virtual Rectangle BoundsAbsolute
        {
            get { return this.GetAbsoluteVisibleBounds(); }
            protected set { this.SetAbsoluteVisibleBounds(value); }
        }
        /// <summary>
        /// Relative coordinates for Child items. Is relative to this.Parent, this is similarly to this.Bounds.
        /// BoundsActive = Bounds + (to inner) ClientBorder
        /// </summary>
        protected virtual Rectangle BoundsClient { get { this.BoundsCheck(); return this.__BoundsClient.Value; } }
        /// <summary>
        /// Relative size of area for Child items.
        /// </summary>
        protected virtual Size ClientSize { get { this.BoundsCheck(); return this.__BoundsClient.Value.Size; } }
        /// <summary>
        /// Invalidate private cache dependent on _Bounds value.
        /// </summary>
        protected void BoundsInvalidate()
        {
            this.__BoundsClient = null;
        }
        /// <summary>
        /// Ensure valid values in __BoundsActive and __BoundsClient.
        /// </summary>
        protected void BoundsCheck()
        {
            if (!this.__BoundsClient.HasValue)
                this.__BoundsClient = this.__Bounds.Sub(this.__ClientBorder);
        }
        /// <summary>
        /// Private accessor for Bounds value.
        /// Setting a value calls BoundsInvalidate().
        /// </summary>
        protected Rectangle _Bounds
        {
            get { return this.__Bounds; }
            set { this.__Bounds = value; this.BoundsInvalidate(); }
        }
        private Rectangle __Bounds;
        private Rectangle? __BoundsClient;
        private Padding __ClientBorder;
        private Padding? __ActivePadding;
        private Rectangle? __AbsoluteInteractiveBounds;
        #endregion
        #region Events and virtual methods

        /// <summary>
        /// Call method OnBoundsChanged() and event BoundsChanged
        /// </summary>
        protected void CallBoundsChanged(Rectangle oldValue, Rectangle newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Rectangle> args = new GPropertyChangeArgs<Rectangle>(eventSource, oldValue, newValue);
            this.OnBoundsChanged(args);
            if (!this.IsSupressedEvent && this.BoundsChanged != null)
                this.BoundsChanged(this, args);
        }
        /// <summary>
        /// Occured after Bounds changed
        /// </summary>
        protected virtual void OnBoundsChanged(GPropertyChangeArgs<Rectangle> args) { }
        /// <summary>
        /// Event on this.Bounds changes
        /// </summary>
        public event GPropertyChanged<Rectangle> BoundsChanged;

        /// <summary>
        /// Call method OnDrawRequest() and event DrawRequest.
        /// Set: this.RepaintToLayers = this.StandardDrawToLayer;
        /// </summary>
        protected void CallDrawRequest(EventSourceType eventSource)
        {
            this.RepaintToLayers = this.StandardDrawToLayer;
            this.OnDrawRequest();
            if (!this.IsSupressedEvent && this.DrawRequest != null)
                this.DrawRequest(this, EventArgs.Empty);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnDrawRequest() { }
        /// <summary>
        /// Event on this.DrawRequest occured
        /// </summary>
        public event EventHandler DrawRequest;

        #endregion
        #region Protected, virtual properties (for IInteractiveItem support)
        /// <summary>
        /// Vrátí true, pokud daný prvek je aktivní na dané souřadnici.
        /// Souřadnice je v koordinátech Controlu, tedy z hlediska prvku jde o souřadnici srovnatelnou s AbsoluteInteractiveBounds.
        /// Pokud prvek má nepravidelný tvar, musí testovat tento tvar v této své metodě explicitně.
        /// </summary>
        /// <param name="absolutePoint">Point v Controlu (=this.Host), v jeho souřadném systému</param>
        /// <returns></returns>
        protected virtual Boolean IsActiveAtAbsolutePoint(Point absolutePoint)
        {
            Rectangle? aib = this.AbsoluteInteractiveBounds;
            if ((!aib.HasValue) || (!aib.Value.HasPixels())) return false;

            return aib.Value.Contains(absolutePoint);
        }
        /// <summary>
        /// Repaint item to layers after current operation. Layers are not combinable. Layer None is for invisible, but active items.
        /// </summary>
        protected virtual GInteractiveDrawLayer StandardDrawToLayer { get { return GInteractiveDrawLayer.Standard; } }
        /// <summary>
        /// Repaint item to this layers after current operation. Layers are combinable. Layer None is permissible (no repaint).
        /// </summary>
        protected virtual GInteractiveDrawLayer RepaintToLayers { get; set; }
        /// <summary>
        /// Current (new) state of item (after this event, not before it).
        /// </summary>
        protected GInteractiveState CurrentState { get; set; }
        /// <summary>
        /// true when this is dragged (CurrentState is LeftDrag or RightDrag)
        /// </summary>
        protected bool IsDragged { get { return this.IsInInteractiveState(GInteractiveState.LeftDrag, GInteractiveState.RightDrag); } }
        /// <summary>
        /// true when this has mouse (CurrentState is MouseOver, LeftDown, RightDown, LeftDrag or RightDrag)
        /// </summary>
        protected bool IsMouseActive { get { return this.IsInInteractiveState(GInteractiveState.MouseOver, GInteractiveState.LeftDown, GInteractiveState.RightDown, GInteractiveState.LeftDrag, GInteractiveState.RightDrag); } }
        /// <summary>
        /// true when this has mouse down (CurrentState is LeftDown, RightDown, LeftDrag or RightDrag)
        /// </summary>
        protected bool IsMouseDown { get { return this.IsInInteractiveState(GInteractiveState.LeftDown, GInteractiveState.RightDown, GInteractiveState.LeftDrag, GInteractiveState.RightDrag); } }
        /// <summary>
        /// Returns true when this.CurrentState is any from specified states.
        /// </summary>
        /// <param name="states"></param>
        /// <returns></returns>
        protected bool IsInInteractiveState(params GInteractiveState[] states)
        {
            if (states == null || states.Length == 0) return false;
            GInteractiveState state = this.CurrentState;
            return (states.Any(s => s == state));
        }
        /// <summary>
        /// Coordinate of mouse relative to CurrentItem.Area.Location.
        /// Can be a null.
        /// </summary>
        protected Point? CurrentMouseRelativePoint { get; set; }
        /// <summary>
        /// Called after any interactive change value of State.
        /// Base class (InteractiveObject) call methods for ChangeState: MouseEnter, MouseLeave, LeftClick.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            bool isEnabled = this.IsEnabled;
            if (!IsEnabled) return;

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardFocusEnter:
                    this._HasFocus = true;
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    this.AfterStateChangedFocusEnter(e);
                    break;
                case GInteractiveChangeState.KeyboardKeyPress:
                    this.AfterStateChangedKeyPress(e);
                    break;
                case GInteractiveChangeState.KeyboardFocusLeave:
                    this._HasFocus = false;
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    this.AfterStateChangedFocusLeave(e);
                    break;

                case GInteractiveChangeState.MouseEnter:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    this.AfterStateChangedMouseEnter(e);
                    break;
                case GInteractiveChangeState.LeftDown:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftUp:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.MouseLeave:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    this.AfterStateChangedMouseLeave(e);
                    break;
                case GInteractiveChangeState.LeftClick:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    this.AfterStateChangedLeftClick(e);
                    break;
            }
        }
        /// <summary>
        /// Is called from InteractiveObject.AfterStateChanged() for ChangeState = KeyboardFocusEnter.
        /// Value in this.HasFocus is now true.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        { }
        /// <summary>
        /// Is called from InteractiveObject.AfterStateChanged() for ChangeState = KeyboardKeyPress.
        /// Value in this.HasFocus is now true.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e)
        { }
        /// <summary>
        /// Is called from InteractiveObject.AfterStateChanged() for ChangeState = KeyboardFocusLeave.
        /// Value in this.HasFocus is now false.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        { }
        /// <summary>
        /// Is called from InteractiveObject.AfterStateChanged() for ChangeState = MouseEnter.
        /// Here can be set ToolTip: e.ToolTipData.InfoText = "text", e.ToolTipData.ShapeType = TooltipShapeType.Rectangle;, and so on
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        { }
        /// <summary>
        /// Is called from InteractiveObject.AfterStateChanged() for ChangeState = MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        { }
        /// <summary>
        /// Is called from InteractiveObject.AfterStateChanged() for ChangeState = LeftClick
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        { }
        /// <summary>
        /// this method is called on each drag action
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DragAction(GDragActionArgs e) { }
        /// <summary>
        /// Called to draw content of this item.
        /// Base class (InteractiveItem) in this method call but method DrawStandard() or DrawAsGhost().
        /// </summary>
        /// <param name="e"></param>
        protected virtual void Draw(GInteractiveDrawArgs e)
        {
            this.DrawStandard(e, this.BoundsAbsolute);
        }
        /// <summary>
        /// Draw this item in standard mode.
        /// Is called when this.IsDragged is false, or during Drag operation by style (DragDrawGhostOriginal, DragDrawGhostInteractive) and currently drawed layer.
        /// Base class draw rectangle with BackColor into BoundsAbsolute.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected virtual void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            e.Graphics.FillRectangle(Skin.Brush(this.BackColor), boundsAbsolute);
        }
        #endregion
        #region Common support: modify value of ProcessAction: IsAction(), AddActions(), RemoveActions(), LeaveOnlyActions()
        /// <summary>
        /// Returns true, when (action) contains any from specified values (actions).
        /// Returns false, when none.
        /// </summary>
        /// <param name="action">Current action</param>
        /// <param name="actions">List of tested actions</param>
        /// <returns></returns>
        protected static bool IsAction(ProcessAction action, params ProcessAction[] actions)
        {
            if (actions == null || actions.Length == 0) return false;
            foreach (ProcessAction one in actions)
            {
                if ((action & one) != 0) return true;
            }
            return false;
        }
        /// <summary>
        /// Returns actions, to which is added specified values.
        /// Returns: (actions OR (add[0] OR add[1] OR add[2] OR ...)).
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="add"></param>
        /// <returns></returns>
        protected static ProcessAction AddActions(ProcessAction actions, params ProcessAction[] add)
        {
            int sum = 0;
            foreach (ProcessAction one in add)
                sum |= (int)one;
            return (ProcessAction)(actions | (ProcessAction)sum);
        }
        /// <summary>
        /// Returns actions, from which is removed specified values.
        /// Returns: (actions AND (Action.All NOR (remove[0] OR remove[1] OR remove[2] OR ...))).
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="remove"></param>
        /// <returns></returns>
        protected static ProcessAction RemoveActions(ProcessAction actions, params ProcessAction[] remove)
        {
            int sum = 0;
            foreach (ProcessAction one in remove)
                sum |= (int)one;
            return (ProcessAction)(actions & (ProcessAction.All ^ (ProcessAction)sum));
        }
        /// <summary>
        /// Returns actions, in which is leaved only specified values (when they was present in input actions).
        /// Returns: (actions AND (leave[0] OR leave[1] OR leave[2] OR ...)).
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="leave"></param>
        /// <returns></returns>
        protected static ProcessAction LeaveOnlyActions(ProcessAction actions, params ProcessAction[] leave)
        {
            int sum = 0;
            foreach (ProcessAction one in leave)
                sum |= (int)one;
            return (ProcessAction)(actions & (ProcessAction)sum);
        }
        /// <summary>
        /// Returns true, when (source) contains any from specified values (sources).
        /// Returns false, when none.
        /// </summary>
        /// <param name="source">Current EventSource</param>
        /// <param name="sources">List of tested EventSources</param>
        /// <returns></returns>
        protected static bool IsEventSource(EventSourceType source, params EventSourceType[] sources)
        {
            if (sources == null || sources.Length == 0) return false;
            foreach (EventSourceType one in sources)
            {
                if ((source & one) != 0) return true;
            }
            return false;
        }
        #endregion
        #region Suppress events
        /// <summary>
        /// Suppress calling all events from this object.
        /// Returns an IDisposable object, use it in using() pattern. At end of using will be returned original value to this.IsSupressedEvent.
        /// </summary>
        /// <returns></returns>
        public IDisposable SuppressEvents()
        {
            return new SuppressEventClass(this);
        }
        /// <summary>
        /// Is now suppressed all events?
        /// </summary>
        protected bool IsSupressedEvent { get { return this.Is.SuppressEvents; } set { this.Is.SuppressEvents = value; } }
        /// <summary>
        /// On constructor is stacked value from owner.IsSupressedEvent, and then is set to true.
        /// On Dispose is returned original value from stack to owner.IsSupressedEvent.
        /// </summary>
        protected class SuppressEventClass : IDisposable
        {
            public SuppressEventClass(InteractiveObject owner)
            {
                this._Owner = owner;
                this.OldSupressedEventValue = (owner != null ? owner.IsSupressedEvent : false);
                if (this._Owner != null)
                    this._Owner.IsSupressedEvent = true;
            }
            private InteractiveObject _Owner;
            private bool OldSupressedEventValue;
            void IDisposable.Dispose()
            {
                if (this._Owner != null)
                    this._Owner.IsSupressedEvent = this.OldSupressedEventValue;
                this._Owner = null;
            }
        }
        #endregion
        #region InteractiveStyle - properties
        /// <summary>
        /// true when this.Style contains KeyboardInput
        /// </summary>
        protected bool StyleHasKeyboard { get { return this.Style.HasFlag(GInteractiveStyles.KeyboardInput); } }
        /// <summary>
        /// true when this.Style contains Drag
        /// </summary>
        protected bool StyleHasDrag { get { return this.Style.HasFlag(GInteractiveStyles.Drag); } }
        #endregion
        #region Boolean repository
        /// <summary>
        /// Is Interactive?
        /// </summary>
        public virtual bool IsInteractive { get { return this.Is.Interactive; } set { this.Is.Interactive = value; } }
        /// <summary>
        /// Is Visible?
        /// </summary>
        public virtual bool IsVisible { get { return this.Is.Visible; } set { this.Is.Visible = value; } }
        /// <summary>
        /// Is Enabled?
        /// </summary>
        public virtual bool IsEnabled { get { return this.Is.Enabled; } set { this.Is.Enabled = value; } }
        /// <summary>
        /// Is HoldMouse?
        /// </summary>
        public virtual bool IsHoldMouse { get { return this.Is.HoldMouse; } set { this.Is.HoldMouse = value; } }
        /// <summary>
        /// All interactive boolean properties
        /// </summary>
        protected InteractiveProperties Is { get { if (this._Is == null) this._Is = new InteractiveProperties(); return this._Is; } } private InteractiveProperties _Is;
        #endregion
        #region IInteractiveItem members
        UInt32 IInteractiveItem.Id { get { return this.Id; } }
        GInteractiveControl IInteractiveItem.Host { get { return this.Host; } set { this.Host = value; } }
        IInteractiveItem IInteractiveItem.Parent { get { return this.Parent; } set { this.Parent = value; } }
        IEnumerable<IInteractiveItem> IInteractiveItem.Childs { get { return this.Childs; } }
        Rectangle IInteractiveItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        Padding? IInteractiveItem.ActivePadding { get { return this.ActivePadding; } set { this.ActivePadding = value; } }
        Rectangle? IInteractiveItem.AbsoluteInteractiveBounds { get { return this.AbsoluteInteractiveBounds; } set { this.AbsoluteInteractiveBounds = value; } }
        Rectangle IInteractiveItem.BoundsClient { get { return this.BoundsClient; } }
        GInteractiveStyles IInteractiveItem.Style { get { return this.Style; } }
        Boolean IInteractiveItem.IsInteractive { get { return this.IsInteractive; } }
        Boolean IInteractiveItem.IsVisible { get { return this.IsVisible; } }
        Boolean IInteractiveItem.IsEnabled { get { return this.IsEnabled; } }
        Boolean IInteractiveItem.HoldMouse { get { return this.IsHoldMouse; } }
        ZOrder IInteractiveItem.ZOrder { get { return this.ZOrder; } }
        GInteractiveDrawLayer IInteractiveItem.StandardDrawToLayer { get { return this.StandardDrawToLayer; } }
        GInteractiveDrawLayer IInteractiveItem.RepaintToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        Boolean IInteractiveItem.IsActiveAtAbsolutePoint(Point absolutePoint) { return this.IsActiveAtAbsolutePoint(absolutePoint); }
        void IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            this.CurrentState = e.TargetState;
            this.CurrentMouseRelativePoint = e.MouseRelativePoint;
            this.AfterStateChanged(e);
        }
        void IInteractiveItem.DragAction(GDragActionArgs e) { this.DragAction(e); }
        void IInteractiveItem.Draw(GInteractiveDrawArgs e) { this.Draw(e); }
        #endregion
        #region Basic members
        /// <summary>
        /// ToString() = "Type.Name #Id"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetType().Name + " #" + this.Id.ToString() + ", Bounds: " + this.Bounds.ToString() + "; Parent: " + (this.Parent != null ? this.Parent.ToString() : (this.Host != null ? this.Host.ToString() : "null"));
        }
        /// <summary>
        /// Unique ID of this object
        /// </summary>
        protected UInt32 Id = ++LastId;
        protected static UInt32 LastId = 0;
        /// <summary>
        /// Returns unique ID for new IInteractiveItem object
        /// </summary>
        /// <returns></returns>
        public static UInt32 GetNextId() { return ++LastId; }
        #endregion
    }
    #endregion
    #region class InteractiveDragObject : ancestor for all draggable items
    /// <summary>
    /// InteractiveDragObject : ancestor for all draggable items.
    /// This class itself implement dragging for current object, with support for target object.
    /// </summary>
    public abstract class InteractiveDragObject : InteractiveObject, IInteractiveItem
    {
        public InteractiveDragObject()
        {
            this.Style |= GInteractiveStyles.DragDrawGhostOriginal;
        }
        #region Interactivity - Dragging
        protected override void DragAction(GDragActionArgs e)
        {
            switch (e.DragAction)
            {
                case DragActionType.DragThisStart:
                    this._CanDragCurrent = this.CanDrag;
                    if (this._CanDragCurrent)
                    {
                        this.BoundsDragOrigin = this.Bounds;
                        this.RepaintToLayers = this.DragDrawToLayers;
                    }
                    break;
                case DragActionType.DragThisMove:
                    if (this._CanDragCurrent && e.DragToBounds.HasValue)
                    {
                        this.DragThisOverBounds(e, e.DragToBounds.Value);
                        this.RepaintToLayers = this.DragDrawToLayers;
                    }
                    break;
                case DragActionType.DragThisCancel:
                    if (this._CanDragCurrent && this.BoundsDragOrigin.HasValue && this.BoundsDragOrigin.Value != this.Bounds)
                    {
                        this.SetBounds(e.DragToBounds.Value, ProcessAction.DragValueActions, EventSourceType.InteractiveChanging | EventSourceType.BoundsChange);
                        this.RepaintToLayers = this.DragDrawToLayers;
                    }
                    break;
                case DragActionType.DragThisDrop:
                    if (this._CanDragCurrent && this.BoundsDragTarget.HasValue)
                    {
                        this.DragThisDropToBounds(e, this.BoundsDragTarget.Value);
                        this.RepaintToLayers = this.DragDrawToLayers;
                    }
                    break;
                case DragActionType.DragThisEnd:
                    this.BoundsDragOrigin = null;
                    this.BoundsDragTarget = null;
                    this._CanDragCurrent = false;
                    this.RepaintToLayers = this.DragDrawToLayers;
                    this.DragThisOverEnd(e);
                    break;
            }
        }
        /// <summary>
        /// Value from this.CanDrag in DragActionType.DragThisStart action.
        /// Is valid upto action DragThisEnd, on next DragThisStart action will be again evaluated from CanDrag)
        /// </summary>
        private bool _CanDragCurrent;
        /// <summary>
        /// Contain true, when this item can be dragged in its current state.
        /// Yet it is not known where will be dragged.
        /// The value is determined by the DragThisStart action.
        /// The base class (InteractiveDragObject) allways returns true.
        /// </summary>
        protected virtual bool CanDrag { get { return true; } }
        /// <summary>
        /// Volá se v procesu přesouvání, pro aktivní objekt.
        /// Bázová třída ulož předané souřadnice do this.BoundsDragTarget
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected virtual void DragThisOverBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.BoundsDragTarget = targetRelativeBounds;
        }
        /// <summary>
        /// Is called on DragDrop process for dragged (=active) object.
        /// Base class InteractiveDragObject set specified (boundsTarget) into this.Bounds (with ProcessAction = DragValueActions and EventSourceType = (InteractiveChanged | BoundsChange).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        protected virtual void DragThisDropToBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            this.SetBounds(boundsTarget, ProcessAction.DragValueActions, EventSourceType.InteractiveChanged | EventSourceType.BoundsChange);
        }
        /// <summary>
        /// Je voláno po skončení přetahování, ať už skončilo OK (=Drop) nebo Escape (=Cancel).
        /// Účelem je provést úklid po skončení přetahování.
        /// Bázová třída InteractiveDragObject nedělá nic.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DragThisOverEnd(GDragActionArgs e)
        { }
        /// <summary>
        /// Original Bounds of this object in time of begin of dragging.
        /// Its drawed as Ghost on this coordinates, when DragWithGhost is true.
        /// </summary>
        protected Rectangle? BoundsDragOrigin { get; set; }
        /// <summary>
        /// Target Bounds of this object during dragging.
        /// Its drawed as Ghost on this coordinates, when DragWithGhost is true.
        /// </summary>
        protected Rectangle? BoundsDragTarget { get; set; }
        protected GInteractiveDrawLayer DragDrawToLayers { get { return (GInteractiveDrawLayer.Standard | GInteractiveDrawLayer.Interactive); } }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
            if (!this.IsDragged || !this.BoundsDragOrigin.HasValue)
            {
                this.DrawStandard(e, this.BoundsAbsolute);
            }
            else
            {
                this.DrawOnDragging(e);
            }
        }
        /// <summary>
        /// Tato metoda zajistí vykreslení this objektu v době, kdy probíhá jeho Dragging.
        /// Bázová třída InteractiveDragObject v této metodě reaguje na styl (this.Style), 
        /// a podle vlastností stylu DragDrawGhostOriginal a DragDrawGhostInteractive 
        /// řídí vykreslení do souřadnic výchozích (BoundsDragOrigin) a do souřadnic Dragging (BoundsDragTarget), 
        /// do vrstev Standard a Interactive, pomocí volání metod DrawStandard() a DrawAsGhost().
        /// Pokud chce potomek kreslit prvek v době přetahování jinak, může přepsat tuto metodu.
        /// Pokud potomek chce jen specificky kreslit objekt v době přetahování, má naplnit metody DrawStandard() a DrawAsGhost().
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DrawOnDragging(GInteractiveDrawArgs e)
        {
            bool ghostInOriginalBounds = ((this.Style & GInteractiveStyles.DragDrawGhostOriginal) != 0);
            bool ghostInDraggedBounds = ((this.Style & GInteractiveStyles.DragDrawGhostInteractive) != 0);
            GInteractiveDrawLayer currentLayer = e.DrawLayer;

            // We are now in Drag process:
            Rectangle bounds;
            if (currentLayer == GInteractiveDrawLayer.Standard)
            {   // Into Standard layer will be drawed item allways in its Original bounds:
                bounds = this.GetAbsoluteBounds(this.BoundsDragOrigin.Value);
                if (ghostInOriginalBounds)
                {   // Draw ghost on original bounds (in Standard layer):
                    this.DrawAsGhost(e, bounds);
                }
                else if (ghostInDraggedBounds)
                {   // Draw ghost to Interactive layer => into Standard layer we draw Standard image:
                    this.DrawStandard(e, bounds);
                }
                // else: Draw standard image to Interactive layer only, with no image in standard layer.
            }
            else if (currentLayer == GInteractiveDrawLayer.Interactive)
            {   // Into Interactive layer will be drawed image (ghost or standard type) in current Bounds:
                bounds = this.GetAbsoluteBounds(this.BoundsDragTarget.Value);
                if (ghostInDraggedBounds)
                {   // Draw ghost to Interactive layer:
                    this.DrawAsGhost(e, bounds);
                }
                else
                {   // Draw ghost on original bounds, or draw no ghost => draw Standard image into Iteractive layer:
                    this.DrawStandard(e, bounds);
                }
            }
        }
        /// <summary>
        /// Draw this item as ghost.
        /// Is called when this.IsDragged is true = during Drag operation by style (DragDrawGhostOriginal, DragDrawGhostInteractive) and currently drawed layer.
        /// Base class draw rectangle with BackColor with Alpha-channel = 64, into BoundsAbsolute.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DrawAsGhost(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(192, this.BackColor)))
            {
                e.Graphics.FillRectangle(brush, boundsAbsolute);
            }
        }
        #endregion

    }
    #endregion
    #region class InteractiveValue : abstract ancestor for iteractive objects with Value and ValieRange properties
    /// <summary>
    /// InteractiveValue : abstract ancestor for iteractive objects with Value and ValueRange properties
    /// </summary>
    public abstract class InteractiveValue<TValue, TRange> : InteractiveObject
        where TValue : IEquatable<TValue>
        where TRange : IEquatable<TRange>
    {
        #region Value and ValueRange properties, SetValue() and SetValueRange() methods, protected aparatus
        /// <summary>
        /// Current value of this object
        /// </summary>
        public virtual TValue Value
        {
            get { return this.GetValue(this._Value, this._ValueRange); }
            set { this.SetValue(value, ProcessAction.All, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Current value of this object.
        /// Setting value to this property does not call any event (action = <seealso cref="Djs.Common.Components.ProcessAction.SilentValueActions"SilentValueActions/>)
        /// </summary>
        public virtual TValue ValueSilent
        {
            get { return this.GetValue(this._Value, this._ValueRange); }
            set { this.SetValue(value, ProcessAction.SilentValueActions, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Set this._Value = value (when new value is not equal to current value: value != this._Value).
        /// If actions contain CallChangedEvents, then call CallValueChanged() method.
        /// If actions contain CallDraw, then call CallDrawRequest() method.
        /// return true = was change, false = not change (bounds == this._Bounds).
        /// </summary>
        /// <param name="value">New value</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        public virtual bool SetValue(TValue value, ProcessAction actions, EventSourceType eventSource)
        {
            TValue oldValue = this._Value;
            TValue newValue = this.GetValue(value, this._ValueRange);          // New value aligned to current ValueRange
            if (IsEqual(newValue, oldValue)) return false;                     // No change

            this._Value = value;
            this.CallActionsAfterValueChanged(oldValue, newValue, ref actions, eventSource);
            return true;
        }
        /// <summary>
        /// Call all actions after Value chaged.
        /// Call: allways: SetValueAfterChange(); by actions: SetValueRecalcInnerData(); SetValuePrepareInnerItems(); CallValueChanged(); CallDrawRequest();
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void CallActionsAfterValueChanged(TValue oldValue, TValue newValue, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.SetValueAfterChange(oldValue, newValue, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.RecalcInnerData))
                this.SetValueRecalcInnerData(oldValue, newValue, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.SetValuePrepareInnerItems(oldValue, newValue, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueChanged(oldValue, newValue, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Is called after Value change, from SetValue() method, without any conditions (even if action is None).
        /// </summary>
        /// <param name="oldValue">Old value, before change</param>
        /// <param name="newValue">New value. Use this value rather than this.Value</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetValueAfterChange(TValue oldValue, TValue newValue, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Is called after Value change, from SetValue() method, only when action RecalcInnerData is specified.
        /// Recalculate SubItems value after change this.Value.
        /// </summary>
        /// <param name="oldValue">Old value, before change</param>
        /// <param name="newValue">New value. Use this value rather than this.Value</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetValueRecalcInnerData(TValue oldValue, TValue newValue, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Is called after Value change, from SetValue() method, only when action PrepareInnerItems is specified.
        /// Recalculate SubItems value after change this.Value.
        /// </summary>
        /// <param name="oldValue">Old value, before change</param>
        /// <param name="newValue">New value. Use this value rather than this.Value</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetValuePrepareInnerItems(TValue oldValue, TValue newValue, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Field for Value store
        /// </summary>
        protected TValue _Value;

        /// <summary>
        /// Current limits (Range) for Value in this object
        /// </summary>
        public virtual TRange ValueRange
        {
            get { return this._ValueRange; }
            set { this.SetValueRange(value, ProcessAction.All, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Set this._Value = value (when new value is not equal to current value: value != this._Value).
        /// If actions contain CallChangedEvents, then call CallValueChanged() method.
        /// If actions contain CallDraw, then call CallDrawRequest() method.
        /// return true = was change, false = not change (bounds == this._Bounds).
        /// </summary>
        /// <param name="valueRange">New value range</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        public virtual bool SetValueRange(TRange valueRange, ProcessAction actions, EventSourceType eventSource)
        {
            TRange oldValueRange = this._ValueRange;
            TRange newValueRange = valueRange;
            if (((object)oldValueRange == null) && ((object)newValueRange == null)) return false;            // No change (null == null)
            if (((object)oldValueRange != null) && oldValueRange.Equals(newValueRange)) return false;        // No change (oldValue == newValue)

            this._ValueRange = valueRange;

            // Apply new range to current value, and new value is different from old value, then modify value:
            TValue oldValue = this._Value;
            TValue newValue = this.GetValue(oldValue, this._ValueRange);                                  // New value aligned to new ValueRange
            if (!IsEqual(newValue, oldValue))
            {
                this.SetValue(newValue, actions, eventSource);
                actions = RemoveActions(actions, ProcessAction.CallDraw);
            }

            // Actions for ValueRange:
            this.CallActionsAfterValueRangeChanged(oldValueRange, newValueRange, ref actions, eventSource);
            return true;
        }
        /// <summary>
        /// Call all actions after ValueRange chaged.
        /// Call: allways: SetValueAfterChange(); by actions: SetValueRecalcInnerData(); SetValuePrepareInnerItems(); CallValueChanged(); CallDrawRequest();
        /// </summary>
        /// <param name="oldValueRange">Old value range</param>
        /// <param name="newValueRange">New value range</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void CallActionsAfterValueRangeChanged(TRange oldValueRange, TRange newValueRange, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.SetValueRangeAfterChange(oldValueRange, newValueRange, ref actions, eventSource);
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueRangeChanged(oldValueRange, newValueRange, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Is called after ValueRange change, from SetValueRange() method, without any conditions (even if action is None).
        /// </summary>
        /// <param name="oldValue">Old value range, before change</param>
        /// <param name="newValue">New value range. Use this value rather than this.Value</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetValueRangeAfterChange(TRange oldValueRange, TRange newValueRange, ref ProcessAction actions, EventSourceType eventSource)
        { }
        protected TRange _ValueRange;

        /// <summary>
        /// Return a value limited to range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        protected abstract TValue GetValue(TValue value, TRange range);
        /// <summary>
        /// Returns true, when newValue == oldValue
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <returns></returns>
        protected virtual bool IsEqual(TValue newValue, TValue oldValue)
        {
            if (((object)oldValue == null) && ((object)newValue == null)) return true;             // No change, null == null
            if ((object)oldValue != null) return oldValue.Equals(newValue);
            if ((object)newValue != null) return newValue.Equals(oldValue);
            return true;
        }
        /// <summary>
        /// Returns true, when newRange == oldRange
        /// </summary>
        /// <param name="newRange"></param>
        /// <param name="oldRange"></param>
        /// <returns></returns>
        protected virtual bool IsEqual(TRange newRange, TRange oldRange)
        {
            if (((object)oldRange == null) && ((object)newRange == null)) return true;             // No change, null == null
            if ((object)oldRange != null) return oldRange.Equals(newRange);
            if ((object)newRange != null) return newRange.Equals(oldRange);
            return true;
        }
        #endregion
        #region Events and virtual methods

        /// <summary>
        /// Call method OnValueChanged() and event ValueChanged
        /// </summary>
        protected void CallValueChanged(TValue oldValue, TValue newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TValue> args = new GPropertyChangeArgs<TValue>(eventSource, oldValue, newValue);
            this.OnValueChanged(args);
            if (!this.IsSupressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
        }
        /// <summary>
        /// Occured after Value changed
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<TValue> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChanged<TValue> ValueChanged;

        /// <summary>
        /// Call method OnValueRangeChanged() and event ValueChanged
        /// </summary>
        protected void CallValueRangeChanged(TRange oldValue, TRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TRange> args = new GPropertyChangeArgs<TRange>(eventSource, oldValue, newValue);
            this.OnValueRangeChanged(args);
            if (!this.IsSupressedEvent && this.ValueRangeChanged != null)
                this.ValueRangeChanged(this, args);
        }
        /// <summary>
        /// Occured after ValueRange changed
        /// </summary>
        protected virtual void OnValueRangeChanged(GPropertyChangeArgs<TRange> args) { }
        /// <summary>
        /// Event on this.ValueRange changes
        /// </summary>
        public event GPropertyChanged<TRange> ValueRangeChanged;


        #endregion
    }
    #endregion
}
