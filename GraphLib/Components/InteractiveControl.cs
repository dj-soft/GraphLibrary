using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GInteractiveControl : Jediný používaný interaktivní WinForm control, který se používá pro zobrazení interaktivních dat
    /// <summary>
    /// GInteractiveControl : Jediný používaný interaktivní WinForm control, který se používá pro zobrazení interaktivních dat
    /// </summary>
    public partial class GInteractiveControl : GControlLayered, IInteractiveParent
    {
        #region Konstruktor
        public GInteractiveControl()
        {
            this.Init();
        }
        protected override void OnResizeControl()
        {
            base.OnResizeControl();
            this._ProgressItem.SetPosition();
        }
        #endregion
        #region Init, Dispose, ItemsList
        private void Init()
        {
            this._ItemsList = new List<IInteractiveItem>();
            this.SetStyle(ControlStyles.Selectable | ControlStyles.UserMouse, true);
            this._StopWatchInit();
            this._ToolTipInit();
            this._ProgressInit();
            this._DrawInit();
            this._KeyboardEventsInit();
            this._MouseEventsInit();
            this._DrawSupportInit();
            this._BackThreadInit();
        }
        protected override void OnAfterDisposed()
        {
            base.OnAfterDisposed();
            this._BackThreadDone();
        }
        protected List<IInteractiveItem> ItemsList
        {
            get
            {
                if (this._ItemsList == null)
                    this._ItemsList = new List<IInteractiveItem>();
                return this._ItemsList;
            }
        }
        private List<IInteractiveItem> _ItemsList;
        #endregion
        #region Focus (Enter, GotFocus, LostFocus, Leave) a Keyboard (PreviewKeyDown, KeyDown, KeyUp, KeyPress)
        private void _KeyboardEventsInit()
        {
            this._KeyboardCurrentItem = null;
        }
        #region Obsluha override metod (z WinForm.Control) pro Focus a Keyboard
        protected override void OnEnter(EventArgs e)
        {
            this._OnEnter(e);
            base.OnEnter(e);
        }
        private void _OnEnter(EventArgs e)
        {
            if (this._KeyboardLeavedItem != null)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "Enter", ""))
                {
                    try
                    {
                        this._InteractiveDrawInit(null);
                        this._ItemKeyboardExchange(null, this._KeyboardLeavedItem, false);
                    }
                    finally
                    {
                        this._InteractiveDrawRun();
                    }
                }
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            this._OnGotFocus(e);
            base.OnGotFocus(e);
        }
        private void _OnGotFocus(EventArgs e)
        { }
        protected override void OnLostFocus(EventArgs e)
        {
            this._OnLostFocus(e);
            base.OnLostFocus(e);
        }
        private void _OnLostFocus(EventArgs e)
        { }
        protected override void OnLeave(EventArgs e)
        {
            this._OnLeave(e);
            base.OnLeave(e);
        }
        private void _OnLeave(EventArgs e)
        {
            this._KeyboardLeavedItem = this._KeyboardCurrentItem;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "Leave", ""))
            {
                try
                {
                    this._InteractiveDrawInit(null);
                    this._ItemKeyboardExchange(this._KeyboardCurrentItem, null, true);
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            this._OnPreviewKeyDown(e);
            base.OnPreviewKeyDown(e);
        }
        private void _OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (this._KeyboardCurrentItemCanKeyboard)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "PreviewKeyDown", ""))
                {
                    try
                    {
                        this._InteractiveDrawInit(null);
                        this._ItemKeyboardCallEvent(this._KeyboardCurrentItem, GInteractiveChangeState.KeyboardPreviewKeyDown, e, null, null);
                    }
                    finally
                    {
                        this._InteractiveDrawRun();
                    }
                }
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            this._OnKeyDown(e);
            base.OnKeyDown(e);
        }
        private void _OnKeyDown(KeyEventArgs e)
        {
            if (this._MouseDraggedItem != null && e.KeyCode == Keys.Escape)
            {   // When we have Dragged Item, and Escape is pressed, then perform Cancel for current Drag operation:
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "KeyDown_DragCancel", ""))
                {
                    this._MouseDragCancel();
                    this._InteractiveDrawRun();
                }
            }
            else if (this._KeyboardCurrentItemCanKeyboard)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "KeyDown", ""))
                {
                    try
                    {
                        this._InteractiveDrawInit(null);
                        this._ItemKeyboardCallEvent(this._KeyboardCurrentItem, GInteractiveChangeState.KeyboardKeyDown, null, e, null);
                    }
                    finally
                    {
                        this._InteractiveDrawRun();
                    }
                }
            }
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            this._OnKeyUp(e);
            base.OnKeyUp(e);
        }
        private void _OnKeyUp(KeyEventArgs e)
        {
            if (this._KeyboardCurrentItemCanKeyboard)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "KeyUp", ""))
                {
                    try
                    {
                        this._InteractiveDrawInit(null);
                        this._ItemKeyboardCallEvent(this._KeyboardCurrentItem, GInteractiveChangeState.KeyboardKeyUp, null, e, null);
                    }
                    finally
                    {
                        this._InteractiveDrawRun();
                    }
                }
            }
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            this._OnKeyPress(e);
            base.OnKeyPress(e);
        }
        private void _OnKeyPress(KeyPressEventArgs e)
        {
            if (this._KeyboardCurrentItemCanKeyboard)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "KeyPress", ""))
                {
                    try
                    {
                        this._InteractiveDrawInit(null);
                        this._ItemKeyboardCallEvent(this._KeyboardCurrentItem, GInteractiveChangeState.KeyboardKeyPress, null, null, e);
                    }
                    finally
                    {
                        this._InteractiveDrawRun();
                    }
                }
            }
        }
        #endregion
        #region Privátní výkonné metody pro podporu Focus a Keyboard
        /// <summary>
        /// Call events KeyboardFocusLeave and KeyboardFocusEnter when neccessary.
        /// Set _KeyboardCurrentItem = gcItemPrev, when CanKeyboard.
        /// </summary>
        /// <param name="itemPrev"></param>
        /// <param name="itemNext"></param>
        private void _ItemKeyboardExchange(IInteractiveItem itemPrev, IInteractiveItem itemNext, bool forceLeave)
        {
            // We solve Keyboard focus change.
            // This change is other than MouseEnter and MouseLeave action, which is "recursively from parent to child".
            // Keyboard focus changes are one level, and focus can be changed only from and to item, which accept keyboard actions (CanKeyboard).

            // itemPrev is always item with keyboard activity (or is null).
            // itemNext can be an item with mouse activity, but no keyboard activity. We must search for nearest item with keyboard activity in item and its Parent:
            itemNext = _ItemKeyboardSearchKeyboardInput(itemNext);

            // Keyboard focus change is simpliest:
            bool existsPrev = (itemPrev != null && itemPrev.Style.HasFlag(GInteractiveStyles.KeyboardInput));
            bool existsNext = (itemNext != null && itemNext.Style.HasFlag(GInteractiveStyles.KeyboardInput));
            if (!existsPrev && !existsNext) return;                                      // booth is null (=paranoia)
            if (Object.ReferenceEquals((existsPrev ? itemPrev : null), (existsNext ? itemNext : null))) return;        // no change

            // Exchange focus is only when new item is different from previous item, and new item can accept keyboard input:
            if (forceLeave || existsNext)
            {
                if (existsPrev)
                    this._ItemKeyboardCallEvent(itemPrev, GInteractiveChangeState.KeyboardFocusLeave, null, null, null);

                if (existsNext)
                    this._ItemKeyboardCallEvent(itemNext, GInteractiveChangeState.KeyboardFocusEnter, null, null, null);

                this._KeyboardCurrentItem = itemNext;
            }
        }
        /// <summary>
        /// Vrátí prvek (daný, nebo jeho parenta), jehož <see cref="IInteractiveParent.Style"/> obsahuje <see cref="GInteractiveStyles.KeyboardInput"/>.
        /// Can return null.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private IInteractiveItem _ItemKeyboardSearchKeyboardInput(IInteractiveItem item)
        {
            Dictionary<uint, object> scanned = new Dictionary<uint, object>();
            while (item != null)
            {
                if (scanned.ContainsKey(item.Id))
                    return null;
                scanned.Add(item.Id, null);
                if (item != null && item.Style.HasFlag(GInteractiveStyles.KeyboardInput))
                    return item;
                if (item.Parent == null)
                    return null;
                // Přejdu na parenta daného prvku, ale jen pokud je to IInteractiveItem.
                // Tím vyloučím přechod na parenta, který je fyzický WinForm control GInteractiveControl.
                item = item.Parent as IInteractiveItem;
                if (item == null)
                    return null;
            }
            return null;
        }
        /// <summary>
        /// Call item.AfterStateChanged() method with correct argument
        /// </summary>
        /// <param name="item"></param>
        /// <param name="change"></param>
        private void _ItemKeyboardCallEvent(IInteractiveItem item, GInteractiveChangeState change, PreviewKeyDownEventArgs previewArgs, KeyEventArgs keyArgs, KeyPressEventArgs keyPressArgs)
        {
            if ((item.Style & GInteractiveStyles.KeyboardInput) != 0)
            {
                GInteractiveChangeState realChange = change;
                GInteractiveChangeStateArgs e = new GInteractiveChangeStateArgs(true, item, realChange, _GetStateAfterChange(realChange, item.IsEnabled), this.FindIItemAtPoint, previewArgs, keyArgs, keyPressArgs);
                e.UserDragPoint = null;

                item.AfterStateChanged(e);

                Point? toolTipPoint = (e.HasToolTipData ? (Point?)item.GetAbsoluteVisibleBounds().Location : (Point?)null);
                this._InteractiveDrawStore(toolTipPoint, e);
            }
        }
        /// <summary>
        /// Current item with any keyboard-interaction, can be null
        /// </summary>
        private IInteractiveItem _KeyboardCurrentItem;
        /// <summary>
        /// true when _KeyboardCurrentItem exists and can get keyboard actions
        /// </summary>
        private bool _KeyboardCurrentItemCanKeyboard { get { return (this._KeyboardCurrentItem != null); } }
        /// <summary>
        /// An item with any keyboard-interaction, from which is Leaved this control. Is re-activated in Enter control event. Can be null
        /// </summary>
        private IInteractiveItem _KeyboardLeavedItem;
        #endregion
        #endregion
        #region Myš (události z WinForm Controlu, jejich řešení v GInteractiveControl)
        private void _MouseEventsInit()
        {
            this._MouseCurrentItem = null;
            this._MouseDraggedItem = null;
            this._DragStartSize = SystemInformation.DragSize;
        }
        #region Obsluha override metod (z WinForm.Control) pro myš
        protected override void OnMouseEnter(EventArgs e)
        {
            this._OnMouseEnter(e);
            base.OnMouseEnter(e);
        }
        private void _OnMouseEnter(EventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseEnter", ""))
            {
                try
                {
                    this._InteractiveDrawInit(null);
                    this._MouseAllReset();
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            this._OnMouseMove(e);
            base.OnMouseMove(e);
        }
        private void _OnMouseMove(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseMove", ""))
            {
                try
                {
                    this._InteractiveDrawInit(e);
                    if (!this._MouseDownAbsolutePoint.HasValue && (e.Button == System.Windows.Forms.MouseButtons.Left || e.Button == System.Windows.Forms.MouseButtons.Right))
                    {
                        this._MouseFell(e);                              // We missed MouseDown event, and now have MoseMove event without prepared data from MouseDown...
                        scope.AddItem("Missed: MouseFell!");
                    }

                    if (!this._MouseDownAbsolutePoint.HasValue)
                    {
                        this._MouseOver(e);                              // Mouse move above control, with none button pressed
                        scope.Result = "MouseOver";
                    }

                    else if (this._MouseCurrentItem.CanDrag)             // Mouse (any button) is pressed, and item enable dragging
                    {
                        if (!this._CurrentMouseDragCanceled)
                        {
                            bool isDragBegin = this._MouseDragStartBounds.HasValue && !this._MouseDragStartBounds.Value.Contains(e.Location);
                            if (isDragBegin)
                            {
                                this._MouseDragBegin(e);                 // Mouse is now first dragged out from "silent zone" (_CurrentMouseDragStart) = Drag started
                                scope.Result = "MouseDragBeginMove";
                            }
                            if (!this._MouseDragStartBounds.HasValue)
                            {
                                this._MouseDragMove(e);                  // Mouse is rutinly dragged to another point...
                                if (!isDragBegin)
                                    scope.Result = "MouseDragMove";
                            }
                        }
                    }
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            this._OnMouseDown(e);
            base.OnMouseDown(e);
        }
        private void _OnMouseDown(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseDown", ""))
            {
                try
                {
                    this._InteractiveDrawInit(e);
                    this._MouseFell(e);
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            this._OnMouseUp(e);
            base.OnMouseUp(e);
        }
        private void _OnMouseUp(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseUp", ""))
            {
                try
                {
                    this._InteractiveDrawInit(e);
                    if (this._CurrentMouseDragCanceled)
                        this._MouseDownReset();
                    else if (this._CurrentMouseDragOffset.HasValue)
                        this._MouseDragDone(e);
                    else
                        this._MouseRaise(e);
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            this._OnMouseWheel(e);
            base.OnMouseWheel(e);
        }
        private void _OnMouseWheel(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseWheel", ""))
            {
                try
                {
                    this._InteractiveDrawInit(e);
                    this._MouseOneWheel(e);
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            this._OnMouseLeave(e);
            base.OnMouseLeave(e);
        }
        private void _OnMouseLeave(EventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseLeave", ""))
            {
                try
                {
                    this._InteractiveDrawInit(null);
                    this._MouseOver(null);
                    this._MouseAllReset();
                    this._InteractiveDrawRun();
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        #endregion
        #region Řízení konkrétních aktivit myši, již rozčleněné s podporou Drag & Drop, volání základních interaktivních metod
        private void _MouseOver(MouseEventArgs e)
        {
            if (e != null)
            {
                GCurrentItem gci = this.FindItemAtPoint(e.Location, true);
                this._ItemMouseExchange(this._MouseCurrentItem, gci, this._MouseCurrentRelativePoint);
                this._ToolTipMouseMove(this._MouseCurrentAbsolutePoint);
            }
            else
            {
                this._MouseCurrentRelativePoint = null;
                this._ItemMouseExchange(this._MouseCurrentItem, null, null);
                this._ActivateCursor(SysCursorType.Default);
            }
        }
        private void _MouseFell(MouseEventArgs e)
        {
            GCurrentItem gci = this.FindItemAtPoint(e.Location);

            this._ItemMouseExchange(this._MouseCurrentItem, gci, this._MouseCurrentRelativePoint);
            this._ItemKeyboardExchange(this._KeyboardCurrentItem, gci.Item, false);

            this._MouseDownAbsolutePoint = e.Location;
            this._MouseDownTime = DateTime.Now;
            this._MouseDownButtons = e.Button;
            this._MouseDragStartBounds = e.Location.CreateRectangleFromCenter(this._DragStartSize);
            this._CurrentMouseDragOffset = null;
            this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDown, this._MouseCurrentRelativePoint, null);
        }
        private void _MouseRaise(MouseEventArgs e)
        {
            if (this._MouseCurrentItem != null)
            {
                this._MouseCurrentItem.EventCurrentTime = DateTime.Now;
                this._MouseCurrentRelativePoint = this._GetRelativePointToCurrentItem(e.Location);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftUp, this._MouseCurrentRelativePoint, null);
                if (this._MouseCurrentItem.CanDoubleClick && GCurrentItem.IsDoubleClick(this._MouseClickedItem, this._MouseCurrentItem))
                {
                    this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDoubleClick, this._MouseCurrentRelativePoint, null);
                }
                else if (this._MouseCurrentItem.CanLongClick && GCurrentItem.IsLongClick(this._MouseDownTime, this._MouseCurrentItem))
                {
                    this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftLongClick, this._MouseCurrentRelativePoint, null);
                }
                else if (this._MouseCurrentItem.CanClick)
                {
                    this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftClick, this._MouseCurrentRelativePoint, null);
                }
                this._MouseClickedItem = this._MouseCurrentItem;
            }
            this._MouseDownReset();
        }
        private void _MouseDragBegin(MouseEventArgs e)
        {
            // Relative position of mouse on MouseDown, not current position (after mouse moved out from _CurrentMouseDragStart)
            this._MouseCurrentRelativePoint = _GetRelativePoint(this._MouseDownAbsolutePoint.Value, this._MouseCurrentItem);
            if (this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.EventCurrentTime = DateTime.Now; 
                this._MouseDraggedItem = this._MouseCurrentItem;
                this._MouseDraggedItemOriginBounds = this._MouseCurrentItem.Item.Bounds;
                Point? userDragPoint = null;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragBegin, this._MouseCurrentRelativePoint, null, ref userDragPoint);
                if (userDragPoint.HasValue)
                    this._UserDragPointOffset = userDragPoint.Value.Sub(this._MouseDownAbsolutePoint.Value);
            }
            this._CurrentMouseDragOffset = this._GetRelativePointToCurrentItem(this._MouseDownAbsolutePoint.Value);
            this._MouseDragStartBounds = null;
        }
        private void _MouseDragMove(MouseEventArgs e)
        {
            if (this._MouseDraggedItem != null && this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.EventCurrentTime = DateTime.Now;
                Point? userDragPoint = null;
                if (this._UserDragPointOffset.HasValue)
                    userDragPoint = e.Location.Add(this._UserDragPointOffset.Value);
                this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, this._MouseCurrentItem);
                Point shift = e.Location.Sub(this._MouseDownAbsolutePoint.Value);
                Rectangle dragToBounds = this._MouseDraggedItemOriginBounds.Value.ShiftBy(shift);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMove, this._MouseCurrentRelativePoint, dragToBounds, ref userDragPoint);
            }
        }
        private void _MouseDragCancel()
        {
            if (this._MouseDraggedItem != null && this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.EventCurrentTime = DateTime.Now;
                Point? userDragPoint = null;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragCancel, null, null, ref userDragPoint);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragEnd, null, null);
                this._RepaintAllItems = true;
                this._MouseDraggedItem = null;
            }
            this._CurrentMouseDragOffset = null;              // on primary handler _MouseUp() will be called _MouseRaise(), instead of _MouseDragDone()!  In _MouseDragDone() will be called _MouseDownReset().
            this._CurrentMouseDragCanceled = true;
        }
        private void _MouseDragDone(MouseEventArgs e)
        {
            this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, this._MouseCurrentItem);
            this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftUp, this._MouseCurrentRelativePoint, null);
            if (this._MouseDraggedItem != null && this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.EventCurrentTime = DateTime.Now;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragDone, this._MouseCurrentRelativePoint, null);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragEnd, null, null);
            }
            this._MouseDraggedItem = null;
            this._MouseDownReset();
        }
        private void _MouseOneWheel(MouseEventArgs e)
        {
            GCurrentItem gci = this.FindItemAtPoint(e.Location);
            this._ItemMouseExchange(this._MouseCurrentItem, gci, this._MouseCurrentRelativePoint);
            GInteractiveChangeState change = (e.Delta > 0 ? GInteractiveChangeState.WheelUp : GInteractiveChangeState.WheelDown);
            this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, change, this._MouseCurrentRelativePoint, null);
        }
        #endregion
        #region Podpůrné metody pro akce myši
        /// <summary>
        /// Return change state for current mousebutton (_CurrentMouseDownButtons), for change specified for left button.
        /// When no button pressed, or state is not button-dependent, then unchanged state is returned.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private GInteractiveChangeState _GetStateForCurrentMouseButton(GInteractiveChangeState state, bool isEnabled)
        {
            return _GetStateForMouseButton(state, this._MouseDownButtons, isEnabled);
        }
        /// <summary>
        /// Return change state for current mousebutton (_CurrentMouseDownButtons), for change specified for left button.
        /// When no button pressed, or state is not button-dependent, then unchanged state is returned.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static GInteractiveChangeState _GetStateForMouseButton(GInteractiveChangeState state, MouseButtons? mouseDownButtons, bool isEnabled)
        {
            if (state == GInteractiveChangeState.MouseOver)
            {
                return (isEnabled ? GInteractiveChangeState.MouseOver : GInteractiveChangeState.MouseOverDisabled);
            }

            if (mouseDownButtons.HasValue)
            {
                bool right = (mouseDownButtons.Value == System.Windows.Forms.MouseButtons.Right);
                switch (state)
                {
                    case GInteractiveChangeState.LeftDown:
                    case GInteractiveChangeState.RightDown:
                        return (right ? GInteractiveChangeState.RightDown : GInteractiveChangeState.LeftDown);
                    case GInteractiveChangeState.LeftDragBegin:
                    case GInteractiveChangeState.RightDragBegin:
                        return (right ? GInteractiveChangeState.RightDragBegin : GInteractiveChangeState.LeftDragBegin);
                    case GInteractiveChangeState.LeftDragMove:
                    case GInteractiveChangeState.RightDragMove:
                        return (right ? GInteractiveChangeState.RightDragMove : GInteractiveChangeState.LeftDragMove);
                    case GInteractiveChangeState.LeftDragCancel:
                    case GInteractiveChangeState.RightDragCancel:
                        return (right ? GInteractiveChangeState.RightDragCancel : GInteractiveChangeState.LeftDragCancel);
                    case GInteractiveChangeState.LeftDragDone:
                    case GInteractiveChangeState.RightDragDone:
                        return (right ? GInteractiveChangeState.RightDragDone : GInteractiveChangeState.LeftDragDone);
                    case GInteractiveChangeState.LeftDragEnd:
                    case GInteractiveChangeState.RightDragEnd:
                        return (right ? GInteractiveChangeState.RightDragEnd : GInteractiveChangeState.LeftDragEnd);
                    case GInteractiveChangeState.LeftUp:
                    case GInteractiveChangeState.RightUp:
                        return (right ? GInteractiveChangeState.RightUp : GInteractiveChangeState.LeftUp);
                    case GInteractiveChangeState.LeftClick:
                    case GInteractiveChangeState.RightClick:
                        return (right ? GInteractiveChangeState.RightClick : GInteractiveChangeState.LeftClick);
                    case GInteractiveChangeState.LeftLongClick:
                    case GInteractiveChangeState.RightLongClick:
                        return (right ? GInteractiveChangeState.RightLongClick : GInteractiveChangeState.LeftLongClick);
                    case GInteractiveChangeState.LeftDoubleClick:
                    case GInteractiveChangeState.RightDoubleClick:
                        return (right ? GInteractiveChangeState.RightDoubleClick : GInteractiveChangeState.LeftDoubleClick);
                }
            }
            return state;
        }
        /// <summary>
        /// Return a state (static) after specified change (dynamic event)
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        private static GInteractiveState _GetStateAfterChange(GInteractiveChangeState change, bool isEnabled)
        {
            if (!isEnabled) return GInteractiveState.Disabled;
            switch (change)
            {
                case GInteractiveChangeState.None: return GInteractiveState.Enabled;
                case GInteractiveChangeState.MouseEnter: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.MouseOver: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.MouseOverDisabled: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.MouseLeave: return GInteractiveState.Enabled;
                case GInteractiveChangeState.LeftDown: return GInteractiveState.LeftDown;
                case GInteractiveChangeState.LeftDragBegin: return GInteractiveState.LeftDrag;
                case GInteractiveChangeState.LeftDragMove: return GInteractiveState.LeftDrag;
                case GInteractiveChangeState.LeftDragCancel: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDragDone: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDragEnd: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftUp: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftLongClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDoubleClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDown: return GInteractiveState.RightDown;
                case GInteractiveChangeState.RightDragBegin: return GInteractiveState.RightDrag;
                case GInteractiveChangeState.RightDragMove: return GInteractiveState.RightDrag;
                case GInteractiveChangeState.RightDragCancel: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDragDone: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDragEnd: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightUp: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightLongClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDoubleClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.KeyboardFocusEnter: return GInteractiveState.Enabled;
                case GInteractiveChangeState.KeyboardPreviewKeyDown: return GInteractiveState.Enabled;
                case GInteractiveChangeState.KeyboardKeyDown: return GInteractiveState.Enabled;
                case GInteractiveChangeState.KeyboardKeyUp: return GInteractiveState.Enabled;
                case GInteractiveChangeState.KeyboardFocusLeave: return GInteractiveState.Enabled;
                case GInteractiveChangeState.WheelUp: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.WheelDown: return GInteractiveState.MouseOver;
            }
            return GInteractiveState.Enabled;
        }
        /// <summary>
        /// Returns an action for GInteractiveChangeState value.
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        private static DragActionType _GetDragActionForState(GInteractiveChangeState change)
        {
            switch (change)
            {
                case GInteractiveChangeState.LeftDragBegin: return DragActionType.DragThisStart;
                case GInteractiveChangeState.LeftDragMove: return DragActionType.DragThisMove;
                case GInteractiveChangeState.LeftDragCancel: return DragActionType.DragThisCancel;
                case GInteractiveChangeState.LeftDragDone: return DragActionType.DragThisDrop;
                case GInteractiveChangeState.LeftDragEnd: return DragActionType.DragThisEnd;
                case GInteractiveChangeState.RightDragBegin: return DragActionType.DragThisStart;
                case GInteractiveChangeState.RightDragMove: return DragActionType.DragThisMove;
                case GInteractiveChangeState.RightDragCancel: return DragActionType.DragThisCancel;
                case GInteractiveChangeState.RightDragDone: return DragActionType.DragThisDrop;
                case GInteractiveChangeState.RightDragEnd: return DragActionType.DragThisEnd;
            }
            return DragActionType.None;
        }
        /// <summary>
        /// Call events MouseLeave and MouseEnter when neccessary
        /// </summary>
        /// <param name="gcItemPrev"></param>
        /// <param name="gcItemNext"></param>
        private void _ItemMouseExchange(GCurrentItem gcItemPrev, GCurrentItem gcItemNext)
        {
            this._ItemMouseExchange(gcItemPrev, gcItemNext, null);
        }
        /// <summary>
        /// Call events MouseLeave and MouseEnter when neccessary
        /// </summary>
        /// <param name="gcItemPrev"></param>
        /// <param name="gcItemNext"></param>
        private void _ItemMouseExchange(GCurrentItem gcItemPrev, GCurrentItem gcItemNext, Point? mouseRelativePoint)
        {
            List<IInteractiveItem> leaveList, enterList;
            GCurrentItem.MapExchange(gcItemPrev, gcItemNext, out leaveList, out enterList);

            foreach (IInteractiveItem item in leaveList)
                this._ItemMouseCallStateChangedEvent(gcItemPrev, item, GInteractiveChangeState.MouseLeave);

            foreach (IInteractiveItem item in enterList)
                this._ItemMouseCallStateChangedEvent(gcItemNext, item, GInteractiveChangeState.MouseEnter);

            if (gcItemNext != null && gcItemNext.CanOver)
                this._ItemMouseCallStateChangedEvent(gcItemNext, GInteractiveChangeState.MouseOver, mouseRelativePoint, null);

            this._MouseCurrentItem = gcItemNext;
        }
        /// <summary>
        /// Call interactive event for specified item and change.
        /// </summary>
        /// <param name="gcItem"></param>
        /// <param name="state"></param>
        private void _ItemMouseCallStateChangedEvent(GCurrentItem gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, Rectangle? dragToArea)
        {
            Point? userDragPoint = null;
            this._ItemMouseCallStateChangedEvent(gcItem, change, mouseRelativePoint, dragToArea, ref userDragPoint);
        }
        /// <summary>
        /// Call interactive event for specified item and change.
        /// </summary>
        /// <param name="gcItem">Current item under mouse</param>
        /// <param name="change">Change of state, independently on MouseButton (i.e. LeftDown, in situation where is pressed Right Mouse button). Real change state is detected in this method, with _GetStateForCurrentButton() method.</param>
        private void _ItemMouseCallStateChangedEvent(GCurrentItem gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, Rectangle? dragToArea, ref Point? userDragPoint)
        {
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, gcItem.IsEnabled);
            GInteractiveState state = (gcItem.ExistsItem ? _GetStateAfterChange(realChange, gcItem.Item.IsEnabled) : GInteractiveState.Disabled);
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(gcItem.ExistsItem, gcItem.Item, realChange, state, this.FindIItemAtPoint, this._MouseCurrentAbsolutePoint, mouseRelativePoint, this._MouseDraggedItemOriginBounds, dragToArea);
            stateArgs.UserDragPoint = userDragPoint;

            if (gcItem.ExistsItem)
            {
                gcItem.Item.AfterStateChanged(stateArgs);
                this._ItemMouseCallDragEvent(gcItem.Item, stateArgs);
            }

            this._CallInteractiveStateChanged(stateArgs);

            this._InteractiveDrawStore(stateArgs);

            userDragPoint = stateArgs.UserDragPoint;
        }
        /// <summary>
        /// Call interactive event for specified item and change.
        /// Is used for MouseEnter and MouseLeave events only.
        /// </summary>
        /// <param name="gci"></param>
        /// <param name="item"></param>
        /// <param name="change"></param>
        private void _ItemMouseCallStateChangedEvent(GCurrentItem gci, IInteractiveItem item, GInteractiveChangeState change)
        {
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, item.IsEnabled);
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(true, item, realChange, _GetStateAfterChange(realChange, item.IsEnabled), this.FindIItemAtPoint);
            stateArgs.UserDragPoint = null;

            item.AfterStateChanged(stateArgs);
            this._ItemMouseCallDragEvent(item, stateArgs);           // probably does not call DragAction(), because ChangeState is MouseEnter and MouseLeave.

            this._CallInteractiveStateChanged(stateArgs);
            this._InteractiveDrawStore(stateArgs);
        }
        /// <summary>
        /// Detect Dragging states and call item.DragAction() method.
        /// </summary>
        /// <param name="stateArgs"></param>
        private void _ItemMouseCallDragEvent(IInteractiveItem item, GInteractiveChangeStateArgs stateArgs)
        {
            DragActionType dragAction = _GetDragActionForState(stateArgs.ChangeState);
            if (dragAction == DragActionType.None) return;

            if (this._MouseDownAbsolutePoint.HasValue && (this._MouseCurrentAbsolutePoint.HasValue || dragAction == DragActionType.DragThisCancel || dragAction == DragActionType.DragThisEnd))
            {
                GDragActionArgs dragArgs = new GDragActionArgs(stateArgs, dragAction, this._MouseDownAbsolutePoint.Value, this._MouseCurrentAbsolutePoint);
                item.DragAction(dragArgs);
            }
            else
            {   // Null value in _MouseDownAbsolutePoint or _MouseCurrentAbsolutePoint (except DragThisCancel)!

            }
        }
        /// <summary>
        /// Reset mouse variables used in MouseDown state.
        /// Call this method after MouseUp, Click, DragEnd.
        /// </summary>
        private void _MouseDownReset()
        {
            this._MouseDownAbsolutePoint = null;
            this._MouseDownTime = null;
            this._MouseDownButtons = null;
            this._MouseDragStartBounds = null;
            this._CurrentMouseDragOffset = null;
            this._CurrentMouseDragCanceled = false;
            this._MouseDraggedItemOriginBounds = null;
            this._UserDragPointOffset = null;
        }
        /// <summary>
        /// Reset all mouse variables.
        /// Call this method after MouseLeave.
        /// </summary>
        private void _MouseAllReset()
        {
            this._MouseDownReset();
            this._MouseCurrentItem = null;
            this._MouseCurrentRelativePoint = null;
            this._CurrentCursorType = null;
        }
        /// <summary>
        /// Search and return an topmost interactive item on specified point.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="point"></param>
        /// <param name="withDisabled"></param>
        /// <returns></returns>
        protected IInteractiveItem FindIItemAtPoint(Point point, bool withDisabled)
        {
            GCurrentItem gci = GCurrentItem.FindItemAtPoint(this.ItemsList, null, point, withDisabled);
            return (gci.ExistsItem ? gci.Item : null);
        }
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected GCurrentItem FindItemAtPoint(Point point)
        {
            return this.FindItemAtPoint(point, false);
        }
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected GCurrentItem FindItemAtPoint(Point point, bool withDisabled)
        {
            GCurrentItem gci = (this._ProgressItem.IsVisible
                ? GCurrentItem.FindItemAtPoint(this.ItemsList, this._MouseCurrentItem, point, withDisabled, this._ProgressItem)
                : GCurrentItem.FindItemAtPoint(this.ItemsList, this._MouseCurrentItem, point, withDisabled));

            this._MouseCurrentRelativePoint = _GetRelativePoint(point, gci);
            return gci;
        }
        /// <summary>
        /// Return relative position of specified point to CurrentItem.ActiveArea.Location.
        /// I.e. for CurrentItem.ActiveArea.Location = {100,70} and point = {121,75} returns value {21,5} (= point - CurrentItem.ActiveArea.Location).
        /// When CurrentItem not exists return null.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected Point? _GetRelativePointToCurrentItem(Point point)
        {
            return _GetRelativePoint(point, this._MouseCurrentItem);
        }
        /// <summary>
        /// Return relative position of specified point to specified GCurrentItem (its CurrentItem.ActiveBounds.Location).
        /// I.e. for CurrentItem.ActiveArea.Location = {100,70} and point = {121,75} returns value {21,5} (= point - CurrentItem.ActiveArea.Location).
        /// When CurrentItem not exists return null.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected static Point? _GetRelativePoint(Point point, GCurrentItem gCurrentItem)
        {
            return (gCurrentItem == null ? (Point?)null : (Point?)gCurrentItem.GetRelativePointToCurrentItem(point));
        }
        #endregion
        #region Myší proměnné
        /// <summary>
        /// Coordinates of mouse, in control coordinates (=Absolute), current coordinates (in current event).
        /// </summary>
        private Point? _MouseCurrentAbsolutePoint;
        /// <summary>
        /// Time in which were mouse down in MouseDown event (for LongClick detection)
        /// </summary>
        private DateTime? _MouseDownTime;
        /// <summary>
        /// Coordinates of mouse, in control coordinates (=Absolute), where mouse-down event occurs.
        /// </summary>
        private Point? _MouseDownAbsolutePoint;
        /// <summary>
        /// Buttons which were down in MouseDown event
        /// </summary>
        private MouseButtons? _MouseDownButtons;
        /// <summary>
        /// Area around MouseDown.Point, in which is MouseMove ignored.
        /// Is set in MouseDown handler, is tested in MouseMove handler. When mouse location (during MouseMove) is in this area, then is not called MouseDrag event.
        /// When Mouse location is out of this area, then MouseDrag begin.
        /// Is reset at end of MouseDragBegin handler.
        /// </summary>
        private Rectangle? _MouseDragStartBounds;
        /// <summary>
        /// Current item with any mouse-interaction, can be null
        /// </summary>
        private GCurrentItem _MouseCurrentItem;
        /// <summary>
        /// Current item, which is dragged (from _MouseDragBegin(), through _MouseDragMove() to _MouseDragCancel() or _MouseDragEnd().
        /// </summary>
        private GCurrentItem _MouseDraggedItem;
        /// <summary>
        /// Item, which was last clicked.
        /// </summary>
        private GCurrentItem _MouseClickedItem;
        /// <summary>
        /// Origin Bounds of Current Item, from where is dragged.
        /// Is present in args to all Mouse events.
        /// Is set in MouseDragBegin handler, is store to args, and is reset at end of MouseDown actions (in _MouseDownReset() method).
        /// </summary>
        private Rectangle? _MouseDraggedItemOriginBounds;
        
        /// <summary>
        /// Coordinates of mouse, relative to current interactive item bounds.
        /// </summary>
        private Point? _MouseCurrentRelativePoint;
        /// <summary>
        /// Offset (=relative distance) between Mouse Location (during Drag Begin event) and current item location (its Bounds).
        /// Is stored in MouseDragBegin event. Is reset in _MouseFell() and _MouseDownReset(), 
        /// is used for calculation of DragToBounds value during MouseDragMove event.
        /// </summary>
        private Point? _CurrentMouseDragOffset;
        /// <summary>
        /// true after Cancel during MouseDrag. 
        /// Set in _MouseDragCancel(), reset in _MouseDownReset() methods.
        /// When true, then _MouseMove() does not call MouseDragBegin and MouseDragMove handlers.
        /// When true, then _MouseUp() does not call _MouseDragDone(), but call _MouseRaise() handler.
        /// </summary>
        private bool _CurrentMouseDragCanceled;
        /// <summary>
        /// Offset of UserDragPoint (from event DragBegin) relative to _CurrentMouseDownLocation (in control coordinates).
        /// This offset will be added to MouseLocation (in control coordinates) during drag, and sent to DragMove events.
        /// </summary>
        private Point? _UserDragPointOffset;
        /// <summary>
        /// Size for calculation "No-Drag zone", where MouseMove (with Mouse-Down) is ignored, before MouseDragStart is called.
        /// Is equal do SystemInformation.DragSize, but can be varied.
        /// </summary>
        private Size _DragStartSize;
        #endregion
        #endregion
        #region Přenesení informací z interaktivních metod do překreslení části controlu (vizuální reakce na interaktivní události)
        /// <summary>
        /// Inicializace proměnných (před interaktivními akcemi) pro zachycení požadavků interaktivních akcí na následné překreslení controlu
        /// </summary>
        private void _InteractiveDrawInit(MouseEventArgs e)
        {
            this.StopwatchStart();
            this._MouseCurrentAbsolutePoint = (e == null ? (Point?)null : (Point?)e.Location);
            foreach (IInteractiveItem item in this.Items)
            {
                item.RepaintToLayers = GInteractiveDrawLayer.None;
            }
            this._MouseCursorType = null;
            this._DrawState = InteractiveDrawState.InteractiveEvent;
        }
        /// <summary>
        /// Uložení požadavků na překreslení controlu po provedení jedné interaktivní události
        /// </summary>
        /// <param name="e"></param>
        private void _InteractiveDrawStore(GInteractiveChangeStateArgs e)
        {
            this._InteractiveDrawStore(this._MouseCurrentAbsolutePoint, e);
        }
        /// <summary>
        /// Uložení požadavků na překreslení controlu po provedení jedné interaktivní události
        /// </summary>
        /// <param name="toolTipPoint"></param>
        /// <param name="e"></param>
        private void _InteractiveDrawStore(Point? toolTipPoint, GInteractiveChangeStateArgs e)
        {
            this._ToolTipSet(toolTipPoint, e);
            // Pokud jde po sobě více interaktivních událostí, pak typ kurzoru přebíráme z poslední z nich, která kurzor deklarovala:
            // Typická sekvence je: Item1.MouseLeave (Cursor = Default), Item2.MouseEnter (Cursor = VSplit).
            if (e.RequiredCursorType.HasValue)
                this._MouseCursorType = e.RequiredCursorType;
            if (e.RepaintAllItems)
                this.RepaintAllItems = true;
        }
        /// <summary>
        /// Vyvolání Draw pro zachycené požadavky po dokončení interaktivních událostí
        /// </summary>
        private void _InteractiveDrawRun()
        {
            DrawRequest request = new DrawRequest(this._RepaintAllItems, this._ToolTip, this._ProgressItem);
            request.Fill(this.ClientSize, this, this.ItemsList, this.PendingFullDraw, true);
            if (request.NeedAnyDraw)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "InteractiveDrawRun", ""))
                {
                    try
                    {
                        this._DrawState = InteractiveDrawState.InteractiveRepaint;
                        this.Draw(request);
                    }
                    finally
                    {
                        this.StopwatchStop();
                        this._ActivateCursor(this._MouseCursorType);
                        this._DrawState = InteractiveDrawState.Standard;
                    }
                }
            }
            else
            {
                this.StopwatchDiscard();
                this._ActivateCursor(this._MouseCursorType);
            }
            this._MouseCursorType = null;
            this._DrawState = InteractiveDrawState.Standard;
        }
        /// <summary>
        /// Required cursor type.
        /// </summary>
        private SysCursorType? _MouseCursorType;
        /// <summary>
        /// Any Interactive method or any items need repaint all items, after current (interactive) event.
        /// This value can be only set to true. Set to false does not change value of this property.
        /// </summary>
        public bool RepaintAllItems { get { return this._RepaintAllItems; } set { if (value) this._RepaintAllItems = true; } }
        /// <summary>
        /// Any Interactive method or any items need repaint all items, after current (interactive) event.
        /// </summary>
        private bool _RepaintAllItems;
        #endregion
        #region Support for ToolTip
        /// <summary>
        /// Initialize Tooltip object
        /// </summary>
        private void _ToolTipInit()
        {
            this._ToolTip = new ToolTipItem(this);
            this._ToolTip.TimerDrawRequest += new EventHandler(_ToolTip_TimerDrawRequest);
        }
        /// <summary>
        /// Handler for event ToolTipItem.TimerDrawRequest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _ToolTip_TimerDrawRequest(object sender, EventArgs e)
        {
        }
        /// <summary>
        /// Store definitions for tooltip from data object (TooltipData) to visual object (ToolTipItem)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="e">Data from interactive events</param>
        private void _ToolTipSet(Point? point, GInteractiveChangeStateArgs e)
        {
            ToolTipData toolTipData = (e.HasToolTipData ? e.ToolTipData : null);
            this._ToolTipSet(point, toolTipData);
        }
        /// <summary>
        /// Store definitions for tooltip from data object (TooltipData) to visual object (ToolTipItem)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="toolTipData">ToolTip data</param>
        private void _ToolTipSet(Point? point, ToolTipData toolTipData)
        {
            this._ToolTip.ToolTipSet(point, toolTipData);
            if (this._ToolTipNeedAnimation)
                this._BackThreadResume();
        }
        /// <summary>
        /// Called on every MouseMove event, store current Absolute mouse point to ToolTip.
        /// ToolTip can detect "hover" state, "fade-out" during "show" state etc.
        /// </summary>
        /// <param name="point"></param>
        private void _ToolTipMouseMove(Point? point)
        {
            if (point.HasValue)
                this._ToolTip.MouseLocationCurrent = point;
        }
        /// <summary>
        /// Reset (=Hide) current Tooltip
        /// </summary>
        private void _TimerReset()
        {
            this._ToolTip.ToolTipClear();
        }
        private ToolTipItem _ToolTip;
        /// <summary>
        /// true when tooltip needs animation
        /// </summary>
        private bool _ToolTipNeedAnimation { get { return (this._ToolTip != null && this._ToolTip.NeedAnimation); } }
        #endregion
        #region Support for Progress
        /// <summary>
        /// Data ukazatele postupu
        /// </summary>
        public ProgressData ProgressData { get { return this._ProgressItem.ProgressData; } }
        private void _ProgressInit()
        {
            this._ProgressItem = new ProgressItem(this);

            // this._ProgressItem.ProgressData.BackColor = Color.DarkGreen;
            this._ProgressItem.ProgressData.Ratio = 0.387f;
            this._ProgressItem.ProgressData.InfoCurrent = "Pokoušíme se startovat";
            // this._ProgressItem.ProgressData.Opacity = 240;
            // this._ProgressItem.IsVisible = true;
        }
        private void _ProgressDrawRun()
        {
            DrawRequest request = new DrawRequest(false, this._ToolTip, this._ProgressItem);
            if (request.NeedAnyDraw)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "ProgressDrawRun", ""))
                {
                    try
                    {
                        this._DrawState = InteractiveDrawState.InteractiveRepaint;
                        this.Draw(request);
                    }
                    finally
                    {
                        this._DrawState = InteractiveDrawState.Standard;
                    }
                }
            }
        }
        private ProgressItem _ProgressItem;
        #endregion
        #region Draw
        protected override bool CanDraw { get { return (this._DrawState == InteractiveDrawState.Standard || this._DrawState == InteractiveDrawState.InteractiveRepaint); } }
        /// <summary>
        /// Initialize Draw subsystem.
        /// </summary>
        private void _DrawInit()
        {
            this.LayerCount = 4;          // [0] = Standard;  [1] = Dynamic;  [2] = Interactive;  [3] = ToolTip, Progress Window and Animations
        }
        /// <summary>
        /// Main paint process (standard and interactive)
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintLayers(LayeredPaintEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "OnPaintLayers", "", "Bounds: " + this.Bounds))
            {
                DrawRequest request = e.UserData as DrawRequest;
                scope.AddItem("e.UserData: " + ((request == null) ? "null => Draw " : "Explicit request"));
                if (request == null)
                {   // Explicit request not specified, we will draw all items:
                    request = new DrawRequest(true, this._ToolTip, this._ProgressItem);
                    request.Fill(this.ClientSize, this, this.ItemsList, true, false);
                }

                if (request.NeedStdDraw || request.DrawAllItems)
                {
                    if (request.DrawAllItems)
                        base.OnPaintLayers(e);
                    Graphics graphics = e.GraphicsForLayer(0);
                    this.CallDrawStandardLayer(graphics);
                    this._PaintItems(e.GraphicsForLayer(0), request.StandardItems, GInteractiveDrawLayer.Standard);
                    scope.AddItem("Layer Standard, Items: " + request.StandardItems.Count.ToString());
                }
                if (request.NeedIntDraw)
                {
                    e.CopyContentOfLayer(e.ValidLayer, 1);
                    this._PaintItems(e.GraphicsForLayer(1), request.InteractiveItems, GInteractiveDrawLayer.Interactive);
                    scope.AddItem("Layer Interactive, Items: " + request.InteractiveItems.Count.ToString());
                }
                if (request.NeedDynDraw)
                {
                    e.CopyContentOfLayer(e.ValidLayer, 2);
                    this._PaintItems(e.GraphicsForLayer(2), request.DynamicItems, GInteractiveDrawLayer.Dynamic);
                    scope.AddItem("Layer Dynamic, Items: " + request.DynamicItems.Count.ToString());
                }

                if (this._ProgressItem.IsVisible || this._ToolTip.NeedDraw)
                {
                    e.CopyContentOfLayer(e.ValidLayer, 3);
                    if (this._ProgressItem.IsVisible)
                        this._ProgressItem.Draw(e.GraphicsForLayer(3));
                    if (this._ToolTip.NeedDraw)
                        this._ToolTip.Draw(e.GraphicsForLayer(3));
                    scope.AddItem("Layer ToolTip");
                }

                this._PaintStopwatch(e);

                this._DrawState = InteractiveDrawState.Standard;
                this._RepaintAllItems = false;
            }
        }
        /// <summary>
        /// Vykreslí prvky (items) do dané grafiky (graphics), která reprezentuje danou vrstvu (drawLayer).
        /// Paint all items to specified Graphics.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="items"></param>
        /// <param name="drawLayer"></param>
        private void _PaintItems(Graphics graphics, IEnumerable<DrawRequestItem> items, GInteractiveDrawLayer drawLayer)
        {
            if (items != null)
            {
                graphics.ResetTransform();
                graphics.ResetClip();
                GInteractiveDrawArgs e = new GInteractiveDrawArgs(graphics, drawLayer);
                foreach (DrawRequestItem item in items)
                {
                    e.AbsoluteVisibleClip = item.AbsoluteVisibleClip;
                    if (e.IsStandardLayer) graphics.SetClip(item.AbsoluteVisibleClip);
                    item.Draw(e);
                    if (e.IsStandardLayer) graphics.ResetClip();
                }
            }
        }
        /// <summary>
        /// Activate required cursor, when has value, and is changed from current cursor (see <see cref="_CurrentCursorType">_CurrentCursorType</see>).
        /// </summary>
        /// <param name="cursorType"></param>
        private void _ActivateCursor(SysCursorType? cursorType)
        {
            if (cursorType.HasValue && (!this._CurrentCursorType.HasValue || (this._CurrentCursorType.HasValue && this._CurrentCursorType != cursorType.Value)))
            {
                this.Cursor = SysCursors.GetCursor(cursorType.Value);
                this._CurrentCursorType = cursorType;
            }
        }
        /// <summary>
        /// Current cursor type. Null = default.
        /// </summary>
        private SysCursorType? _CurrentCursorType;
        private InteractiveDrawState _DrawState = InteractiveDrawState.Standard;
        private enum InteractiveDrawState { Standard = 0, InteractiveEvent, InteractiveRepaint }
        /// <summary>
        /// Zajistí překreslení všech prvků
        /// </summary>
        protected virtual void Repaint()
        {
            this._RepaintAllItems = true;
            this.Invalidate();
        }
        /// <summary>
        /// Volá se po vykreslení pozadí do standardní vrstvy.
        /// Zavolá háček <see cref="OnDrawStandardLayer"/> a event <see cref="DrawStandardLayer"/>
        /// </summary>
        /// <param name="graphics"></param>
        protected void CallDrawStandardLayer(Graphics graphics)
        {
            this.OnDrawStandardLayer(graphics);
            if (this.DrawStandardLayer != null)
                this.DrawStandardLayer(this, new PaintEventArgs(graphics, this.ClientRectangle));
        }
        /// <summary>
        /// Háček pro potomky volaný v procesu kreslení standardní vrstvy
        /// </summary>
        /// <param name="graphics"></param>
        protected virtual void OnDrawStandardLayer(Graphics graphics)
        { }
        /// <summary>
        /// Událost volaná v procesu kreslení standardní vrstvy
        /// </summary>
        public event PaintEventHandler DrawStandardLayer;


        #region class DrawRequest + DrawRequestItem
        /// <summary>
        /// Class for analyze items to repaint/draw
        /// </summary>
        protected class DrawRequest
        {
            public DrawRequest()
            {
                this.ProcessedItems = new Dictionary<uint, IInteractiveItem>();
                this.StandardItems = new List<DrawRequestItem>();
                this.InteractiveItems = new List<DrawRequestItem>();
                this.DynamicItems = new List<DrawRequestItem>();
                this.DrawAllItems = false;
            }
            public DrawRequest(bool drawAllItems, ToolTipItem toolTipItem, ProgressItem progressItem)
                : this()
            {
                this.DrawAllItems = drawAllItems;
                this.DrawToolTip = (toolTipItem != null && toolTipItem.NeedDraw);
                this.DrawProgress = (progressItem != null && progressItem.IsVisible);
            }
            
            /// <summary>
            /// Prvky, které jsme již zařadili
            /// </summary>
            protected Dictionary<UInt32, IInteractiveItem> ProcessedItems;
            /// <summary>
            /// Prvky vykreslované do vrstvy Standard
            /// </summary>
            internal List<DrawRequestItem> StandardItems { get; private set; }
            /// <summary>
            /// Prvky vykreslované do vrstvy Interactive
            /// </summary>
            internal List<DrawRequestItem> InteractiveItems { get; private set; }
            /// <summary>
            /// Prvky vykreslované do vrstvy Dynamic
            /// </summary>
            internal List<DrawRequestItem> DynamicItems { get; private set; }
            /// <summary>
            /// true when need draw all items
            /// </summary>
            public bool DrawAllItems { get; private set; }
            /// <summary>
            /// true when ToolTip need draw
            /// </summary>
            public bool DrawToolTip { get; private set; }
            /// <summary>
            /// true when Progress need draw
            /// </summary>
            public bool DrawProgress { get; private set; }
            /// <summary>
            /// true when draw in Interactive mode
            /// </summary>
            public bool InteractiveMode { get; set; }
            public bool NeedStdDraw { get { return (this.StandardItems.Count > 0); } }
            public bool NeedIntDraw { get { return (this.InteractiveItems.Count > 0); } }
            public bool NeedDynDraw { get { return (this.DynamicItems.Count > 0); } }
            /// <summary>
            /// true when need any draw (Standard, Interactive, Dynamic, ToolTip)
            /// </summary>
            public bool NeedAnyDraw { get { return (this.NeedStdDraw || this.NeedIntDraw || this.NeedDynDraw || this.DrawToolTip || this.DrawProgress); } }
            /// <summary>
            /// Do this objektu naplní prvky k vykreslení (prvky typu IInteractiveItem) z dodaného seznamu, 
            /// prvky zatřídí do soupisů dle vrstev (this.StandardItems, InteractiveItems, DynamicItems).
            /// Pokud daný prvek obsahuje nějaké Childs, pak rekurzivně vyvolá tutéž metodu i pro Childs tohoto prvku.
            /// </summary>
            /// <param name="absoluteVisibleClip">Absolutní souřadnice prostoru (absolutní = v koordinátech Controlu), v nichž může být item viditelný = prostor pro Clip grafiky</param>
            /// <param name="items">Prvky k vykreslení</param>
            /// <param name="drawAllItems">true = vykreslit všechny prvky</param>
            /// <param name="interactive">true = provádí se interaktivní vykreslení</param>
            internal void Fill(Size clientSize, IInteractiveParent parent, IEnumerable<IInteractiveItem> items, bool drawAllItems, bool interactive)
            {
                // Tady se bude používat BoundsSpider !!!
                qqq;
                BoundsSpider boundsSpider = BoundsSpider.CreateForParent(clientSize);
                Rectangle absoluteVisibleClip = new Rectangle(0, 0, clientSize.Width, clientSize.Height);
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "DrawRequest", "Fill", ""))
                {
                    this.InteractiveMode = interactive;
                    this.FillFromItems(new Point(0, 0), absoluteVisibleClip, boundsSpider, parent, items, GInteractiveDrawLayer.None);
                    scope.AddItem("StandardItems.Count: " + this.StandardItems.Count.ToString());
                    scope.AddItem("InteractiveItems.Count: " + this.InteractiveItems.Count.ToString());
                    scope.AddItem("DynamicItems.Count: " + this.DynamicItems.Count.ToString());
                }
            }
            /// <summary>
            /// Do this objektu naplní prvky k vykreslení (prvky typu IInteractiveItem) z dodaného seznamu, 
            /// prvky zatřídí do soupisů dle vrstev (this.StandardItems, InteractiveItems, DynamicItems).
            /// Pokud daný prvek obsahuje nějaké Childs, pak rekurzivně vyvolá tutéž metodu i pro Childs tohoto prvku.
            /// </summary>
            /// <param name="absoluteItemOffset">Absolutní souřadnice počátku (absolutní = v koordinátech Controlu), k nimž se vztahují souřadnice item.Bounds</param>
            /// <param name="absoluteVisibleClip">Absolutní souřadnice prostoru (absolutní = v koordinátech Controlu), v nichž může být item viditelný = prostor pro Clip grafiky</param>
            /// <param name="parent">Parent prvků</param>
            /// <param name="items">Prvky k vykreslení</param>
            /// <param name="parentLayers">Vrstvy k vykreslení</param>
            private void FillFromItems(Point absoluteItemOffset, Rectangle absoluteVisibleClip, BoundsSpider boundsSpider, IInteractiveParent parent, IEnumerable<IInteractiveItem> items, GInteractiveDrawLayer parentLayers)
            {
                foreach (IInteractiveItem item in items)
                {
                    // Doplníme Parenta:
                    if (item.Parent == null) item.Parent = parent;

                    // Pokud prvek není Visible, nebudeme jej dávat k vykreslení, a to ani jeho Childs:
                    if (!item.IsVisible) continue;

                    // Abychom se nezacyklili = jeden prvek smí být vidět jen jedenkrát:
                    if (this.ProcessedItems.ContainsKey(item.Id)) continue;
                    this.ProcessedItems.Add(item.Id, item);

                    // Prvek vložíme do Spidera, aby nám mohl počítat jeho souřadnice:
                    boundsSpider.CurrentItem = item;

                    // Přidat prvek do seznamů pro patřičné vrstvy:
                    GInteractiveDrawLayer itemLayers = this.GetLayersToDrawItem(item, parentLayers);
                    this.AddItemToLayers(item, itemLayers, absoluteItemOffset, absoluteVisibleClip, boundsSpider, false);
                    item.RepaintToLayers = GInteractiveDrawLayer.None;

                    // Pokud má prvek potomstvo, vyřešíme i to:
                    //  Child items budou zkontrolovány i tehdy, když jejich Parent prvek není kreslen, to je základní princip Layered Control!
                    IEnumerable<IInteractiveItem> childs = item.Childs;        // get_Childs() method we call only once during Draw.
                    if (childs != null)
                    {
                        IInteractiveItem[] childItems = childs.ToArray();
                        if (childItems.Length > 0)
                        {
                            BoundsSpider childSpider = boundsSpider.CurrentChildsSpider;
                            this.FillFromChilds(absoluteItemOffset, absoluteVisibleClip, childSpider, item, childItems, itemLayers);
                        }
                    }

                    if (item.NeedDrawOverChilds)
                        this.AddItemToLayers(item, itemLayers, absoluteItemOffset, absoluteVisibleClip, boundsSpider, true);
                }
            }
            /// <summary>
            /// Zajistí přidání Child items (od daného parenta, z daného soupisu) do this seznamů
            /// </summary>
            /// <param name="absoluteItemOffset">Absolutní souřadnice počátku, k nimž se vztahují souřadnice this itemu</param>
            /// <param name="absoluteVisibleClip">Souřadnice prostoru (absolutní = v koordinátech Controlu), v nichž může být item viditelný = prostor pro Clip grafiky</param>
            /// <param name="parent">Prvek, který je Parentem daných Childs</param>
            /// <param name="childs">prvky, které mají být vykresleny v rámci tohoto parenta</param>
            private void FillFromChilds(Point absoluteItemOffset, Rectangle absoluteVisibleClip, BoundsSpider childSpider, IInteractiveItem parent, IInteractiveItem[] childs, GInteractiveDrawLayer itemLayers)
            {
                if (parent == null || childs == null) return;

                // Relativní souřadnice (v rámci prvku item, který je parentem daných Childs), v nichž se tyto Child items vykreslují:
                Rectangle relativeBoundsClient = parent.BoundsClient;

                // Absolutní souřadnice bodu, který je výchozím bodem pro souřadnice Childs.Bounds:
                Point absoluteChildsOffset = absoluteItemOffset.Add(relativeBoundsClient.Location);

                // Absolutní souřadnice prostoru BoundsClient = v tomto absolutním prostoru by byly Child items zobrazovány, když by nebyly nijak omezeny:
                Rectangle itemAbsoluteBounds = new Rectangle(absoluteChildsOffset, relativeBoundsClient.Size);

                // Absolutní souřadnice zobrazitelného prostoru = Intersect viditelných prostorů všech parentů * aktuální item:
                Rectangle absoluteChildsVisibleClip = Rectangle.Intersect(absoluteVisibleClip, itemAbsoluteBounds);

                // Čistokrevná rekurze:
                this.FillFromItems(absoluteChildsOffset, absoluteChildsVisibleClip, childSpider, parent, childs, itemLayers);
            }
            /// <summary>
            /// Vrací vrstvy, do kterých má být vykreslen daný prvek, s přihlédnutím k tomu, do jakých vrstev se kreslí jeho parent,
            /// a s ohledem na this.InteractiveMode a this.DrawAllItems
            /// </summary>
            /// <param name="item"></param>
            /// <param name="parentLayers"></param>
            /// <returns></returns>
            private GInteractiveDrawLayer GetLayersToDrawItem(IInteractiveItem item, GInteractiveDrawLayer parentLayers)
            {
                bool interactive = this.InteractiveMode;
                bool drawAllItems = this.DrawAllItems;
                GInteractiveDrawLayer itemLayers = 
                    ((interactive & !drawAllItems) ? item.RepaintToLayers :
                    ((interactive & drawAllItems) ? (item.StandardDrawToLayer | item.RepaintToLayers) :
                     item.StandardDrawToLayer));

                // Pokud item se kreslit nemusí nikam (itemLayers je None), ale jeho parent se už někam kreslí (parentLayers něco obsahuje), 
                //  pak i item (jako Child od toho Parenta) se musí kreslit do týchž vrstev jako Parent,
                //  protože jinak by tam namísto prvku item byla díra!
                return (itemLayers | parentLayers);
            }
            /// <summary>
            /// Zařadí daný prvek (item) do soupisů pro vykreslování pro zadané vrstvy (addToLayers).
            /// </summary>
            /// <param name="item"></param>
            /// <param name="addToLayers"></param>
            /// <param name="absoluteItemOffset"></param>
            /// <param name="absoluteVisibleClip"></param>
            /// <param name="drawOverChilds"></param>
            private void AddItemToLayers(IInteractiveItem item, GInteractiveDrawLayer addToLayers, Point absoluteItemOffset, Rectangle absoluteVisibleClip, BoundsSpider boundsSpider, bool drawOverChilds)
            {
                if (addToLayers.HasFlag(GInteractiveDrawLayer.Standard))
                    this.StandardItems.Add(new DrawRequestItem(absoluteItemOffset, absoluteVisibleClip, item, drawOverChilds));
                if (addToLayers.HasFlag(GInteractiveDrawLayer.Interactive))
                    this.InteractiveItems.Add(new DrawRequestItem(absoluteItemOffset, absoluteVisibleClip, item, drawOverChilds));
                if (addToLayers.HasFlag(GInteractiveDrawLayer.Dynamic))
                    this.DynamicItems.Add(new DrawRequestItem(absoluteItemOffset, absoluteVisibleClip, item, drawOverChilds));
            }
        }
        /// <summary>
        /// Data pro jedno vykreslení jednoho interaktivního prvku
        /// </summary>
        protected class DrawRequestItem
        {
            public DrawRequestItem(Point absoluteItemOffset, Rectangle absoluteVisibleClip, IInteractiveItem item, bool isDrawOverChilds)
            {
                this.AbsoluteItemOffset = absoluteItemOffset;
                this.AbsoluteVisibleClip = absoluteVisibleClip;
                this.Item = item;
                this.IsDrawOverChilds = isDrawOverChilds;
            }
            public override string ToString()
            {
                return "Item: " + this.Item.ToString() + "; BoundsAbsolute: " + this.Item.GetAbsoluteVisibleBounds().ToString();
            }
            /// <summary>
            /// Absolutní souřadnice počátku, k nimž se vztahují relativní souřadnice (Bounds) this itemu
            /// </summary>
            public Point AbsoluteItemOffset { get; private set; }
            /// <summary>
            /// Souřadnice prostoru (absolutní = v koordinátech Controlu), v nichž může být tento item viditelný = prostor pro Clip grafiky.
            /// Jde o Intersect hodnot AbsoluteClientBounds ze všech Parentů, tedy "viditelný průnik", do něhož se má tento prvek zobrazit.
            /// Zde není zohledněna hodnota Item.Bounds: prvek Item se může vykreslit i mimo svoje souřadnice, když chce, ale vždy jen v prostoru daném jeho Parenty.
            /// Tedy: Může se pohybovat ve svém parentu (i mimo svoje Bounds), ale nesmí vyběhnout z parenta.
            /// </summary>
            public Rectangle AbsoluteVisibleClip { get; private set; }
            /// <summary>
            /// Prvek, který se bude vykreslovat
            /// </summary>
            public IInteractiveItem Item { get; private set; }
            /// <summary>
            /// Obsahuje true, pokud tento požadavek je určen pro volání metody <see cref="IInteractiveItem.DrawOverChilds(GInteractiveDrawArgs)"/>.
            /// Pokud je false, má se volat standardní metoda <see cref="IInteractiveItem.Draw(GInteractiveDrawArgs)"/>.
            /// </summary>
            public bool IsDrawOverChilds { get; private set; }
            /// <summary>
            /// Zajistí vykreslení prvku a návazné operace s prvkem
            /// </summary>
            /// <param name="e"></param>
            public void Draw(GInteractiveDrawArgs e)
            {
                if (this.Item is GGrid || this.Item is Grid.GTable)
                { }

                if (!this.IsDrawOverChilds)
                {   // Standardní kreslení:
                    // Do prvku nastavíme absolutní souřadnice, kde je vykreslen - včetně oříznutí, jakožto jeho interaktivní souřadnice (kde je prvek interaktivní):
                    if (this.Item.IsInteractive)
                    {
                        Rectangle absoluteBounds = this.Item.Bounds.Add(this.AbsoluteItemOffset);             // Absolutní Bounds prvku Item
                        Rectangle absoluteInteractiveBounds = absoluteBounds.Add(this.Item.InteractivePadding);    // Absolutní interaktivní souřadnice
                        this.Item.AbsoluteInteractiveBounds = Rectangle.Intersect(absoluteInteractiveBounds, this.AbsoluteVisibleClip);  // Clip na zobrazené souřadnice
                    }

                    // Prvek vykreslíme:
                    this.Item.Draw(e);

                    // V prvku resetujeme požadavek na kreslení do aktuální vrstvy:
                    e.ResetLayerFlagForItem(this.Item);
                }
                else
                {   // Kreslení OverChilds:
                    this.Item.DrawOverChilds(e);
                }
            }
        }
        #endregion
        #endregion
        #region Draw support
        private void _DrawSupportInit()
        {
            this.DefaultBackColor = Color.LightBlue;
            this.DefaultBorderColor = Color.Black;
        }
        /// <summary>
        /// Common SolidBrush object
        /// </summary>
        public SolidBrush SolidBrush
        {
            get
            {
                if (this._SolidBrush == null)
                    this._SolidBrush = new SolidBrush(Color.White);
                return this._SolidBrush;
            }
        }
        /// <summary>
        /// Common Pen object
        /// </summary>
        public Pen Pen
        {
            get
            {
                if (this._Pen == null)
                    this._Pen = new System.Drawing.Pen(Color.Black);
                return this._Pen;
            }
        }
        /// <summary>
        /// Default color for fill rectangle
        /// </summary>
        public new Color DefaultBackColor { get; set; }
        /// <summary>
        /// Default color for border rectangle
        /// </summary>
        public Color DefaultBorderColor { get; set; }
        /// <summary>
        /// Vyplní daný prostor (absolutní souřadnice) danou barvou (default = this.DefaultBackColor).
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="backColor">Barva pozadí</param>
        public void FillRectangle(Graphics graphics, Rectangle absoluteBounds, Color? backColor)
        {
            this._FillRectangle(graphics, absoluteBounds, backColor, false, 0, 0, 0, 0);
        }
        /// <summary>
        /// Vyplní daný prostor (absolutní souřadnice) danou barvou (default = this.DefaultBackColor).
        /// Dané souřadnice mohou být zvětšené o dané hodnoty pro jednotlivé hrany: kladné číslo zvětší prostor, záporné zmenší.
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="backColor">Barva pozadí</param>
        /// <param name="enlargeL">Rozšíření doleva</param>
        /// <param name="enlargeT">Rozšíření nahoru</param>
        /// <param name="enlargeR">Rozšíření doprava</param>
        /// <param name="enlargeB">Rozšíření dolů</param>
        public void FillRectangle(Graphics graphics, Rectangle absoluteBounds, Color? backColor, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            this._FillRectangle(graphics, absoluteBounds, backColor, true, enlargeL, enlargeT, enlargeR, enlargeB);
        }
        /// <summary>
        /// Vyplní daný prostor (absolutní souřadnice) danou barvou (default = this.DefaultBackColor).
        /// Dané souřadnice mohou být zvětšené o dané hodnoty pro jednotlivé hrany: kladné číslo zvětší prostor, záporné zmenší.
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="backColor">Barva pozadí</param>
        /// <param name="enlarge">Použít dané rozšíření</param>
        /// <param name="enlargeL">Rozšíření doleva</param>
        /// <param name="enlargeT">Rozšíření nahoru</param>
        /// <param name="enlargeR">Rozšíření doprava</param>
        /// <param name="enlargeB">Rozšíření dolů</param>
        private void _FillRectangle(Graphics graphics, Rectangle bounds, Color? backColor, bool enlarge, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            if (enlarge)
                bounds = bounds.Enlarge(enlargeL, enlargeT, enlargeR, enlargeB);

            this.SolidBrush.Color = (backColor.HasValue ? backColor.Value : this.DefaultBackColor);
            graphics.FillRectangle(this.SolidBrush, bounds);
        }
        /// <summary>
        /// Vykreslí ohraničení daného prostoru (absolutní souřadnice) danou barvou (default = this.DefaultBorderColor).
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="borderColor">Barva okrajů</param>
        public void BorderRectangle(Graphics graphics, Rectangle absoluteBounds, Color? borderColor)
        {
            this._BorderRectangle(graphics, absoluteBounds, borderColor, false, 0, 0, 0, 0);
        }
        /// <summary>
        /// Vykreslí ohraničení daného prostoru (absolutní souřadnice) danou barvou (default = this.DefaultBorderColor).
        /// Dané souřadnice mohou být zvětšené o dané hodnoty pro jednotlivé hrany: kladné číslo zvětší prostor, záporné zmenší.
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="borderColor">Barva okrajů</param>
        /// <param name="enlargeL">Rozšíření doleva</param>
        /// <param name="enlargeT">Rozšíření nahoru</param>
        /// <param name="enlargeR">Rozšíření doprava</param>
        /// <param name="enlargeB">Rozšíření dolů</param>
        public void BorderRectangle(Graphics graphics, Rectangle absoluteBounds, Color? borderColor, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            this._BorderRectangle(graphics, absoluteBounds, borderColor, true, enlargeL, enlargeT, enlargeR, enlargeB);
        }
        private void _BorderRectangle(Graphics graphics, Rectangle bounds, Color? borderColor, bool enlarge, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            bounds = bounds.Enlarge(enlargeL, enlargeT, enlargeR - 1, enlargeB - 1);     // Shrink Width and Height by 1 pixel is standard for draw Border into (!) area.
            this._ResetPen(borderColor);
            graphics.DrawRectangle(this.Pen, bounds);
        }
        /// <summary>
        /// Vykreslí čáru mezi danými souřadnicemi, v dané barvě a šířce, s daným stylem.
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="dashStyle"></param>
        public void DrawLine(Graphics graphics, int x1, int y1, int x2, int y2, Color color, float width, System.Drawing.Drawing2D.DashStyle dashStyle)
        {
            Pen pen = this.Pen;
            pen.Width = width;
            pen.Color = color;
            pen.DashStyle = dashStyle;
            graphics.DrawLine(pen, x1, y1, x2, y2);
        }
        /// <summary>
        /// Vykreslí ohraničení (Border) okolo daných souřadnic, v dané barvě a stylu.
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="color">Barva okrajů</param>
        /// <param name="linesType">Typ vykreslení okrajů</param>
        public void DrawBorder(Graphics graphics, Rectangle absoluteBounds, Color color, BorderLinesType linesType)
        {
            this.DrawBorder(graphics, absoluteBounds, color, linesType, false);
        }
        /// <summary>
        /// Vykreslí ohraničení (Border) okolo daných souřadnic, v dané barvě a stylu. Linka má šířku 1 px.
        /// Border se kreslí od souřadnice absoluteBounds.X po souřadnici (absoluteBounds.Right - 1), protože to je poslední pixel daného obdélníku.
        /// Obdobně na ose Y.
        /// Tato metoda (a další metody v této třídě) používají ke kreslení objekty (Pen, Brush), 
        /// které jsou instancované na třídě GInteractiveControl, proto je jejich použití velice rychlé.
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice v koordinátech Controlu</param>
        /// <param name="color">Barva okrajů</param>
        /// <param name="linesType">Typ vykreslení okrajů</param>
        /// <param name="drawAsInnerCell">Vykreslovat border jako vnitřní buňku tabulky = pokud jsou čáry Dotted nebo Solid, pak nekreslit Left a Top hranu. V režimu 3D jsou kresleny všechny hrany. Pokud je false, pak kreslí všechny hrany bez ohledu na typ čáry (kromě None, samosebou).</param>
        public void DrawBorder(Graphics graphics, Rectangle absoluteBounds, Color color, BorderLinesType linesType, bool drawAsInnerCell)
        {
            int x0 = absoluteBounds.X;
            int x1 = absoluteBounds.Right - 1;
            int y0 = absoluteBounds.Y;
            int y1 = absoluteBounds.Bottom - 1;

            // Pořadí kreslení (Right, Bottom, Top, Left) určuje, jakou barvou budou vykresleny rohové pixely v režimu 3D.
            // Příprava pera DrawBorderPreparePen() zohledňuje stranu a nastavení okrajů, určuje barvu, styl a Dotted offset:
            if (DrawBorderPreparePen(RectangleSide.Right, absoluteBounds.Location, color, linesType, drawAsInnerCell))
                graphics.DrawLine(this.Pen, x1, y0, x1, y1);
            if (DrawBorderPreparePen(RectangleSide.Bottom, absoluteBounds.Location, color, linesType, drawAsInnerCell))
                graphics.DrawLine(this.Pen, x0, y1, x1, y1);
            if (DrawBorderPreparePen(RectangleSide.Top, absoluteBounds.Location, color, linesType, drawAsInnerCell))
                graphics.DrawLine(this.Pen, x0, y0, x1, y0);
            if (DrawBorderPreparePen(RectangleSide.Left, absoluteBounds.Location, color, linesType, drawAsInnerCell))
                graphics.DrawLine(this.Pen, x0, y0, x0, y1);
        }
        /// <summary>
        /// Metoda připraví this.Pen pro kreslení borderu, pro danou stranu obdélníku, daný typ borderu, barvu a detaily.
        /// Vrací true = má se kreslit, false = nemá se kreslit.
        /// </summary>
        /// <param name="side">Strana, pro kterou chystáme pero</param>
        /// <param name="point">Souřadnice počátku prostoru, ovlivní offset (DashOffset) pro pero se stylem DashStyle = DashStyle.Dot;</param>
        /// <param name="color">Základní barva</param>
        /// <param name="linesType">Typ ohraničení</param>
        /// <param name="drawAsInnerCell">Vykreslovat border jako vnitřní buňku tabulky = pokud jsou čáry Dotted nebo Solid, pak nekreslit Left a Top hranu. V režimu 3D jsou kresleny všechny hrany. Pokud je false, pak kreslí všechny hrany bez ohledu na typ čáry (kromě None, samosebou).</param>
        /// <returns></returns>
        protected bool DrawBorderPreparePen(RectangleSide side, Point point, Color color, BorderLinesType linesType, bool drawAsInnerCell)
        {
            bool isDotted = false;
            bool isSolid = false;
            bool is3DSunken = false;
            bool is3DRisen = false;
            int offset = 0;
            switch (side)
            {
                case RectangleSide.Top:
                case RectangleSide.Bottom:
                    isDotted = linesType.HasFlag(BorderLinesType.HorizontalDotted);
                    isSolid = linesType.HasFlag(BorderLinesType.HorizontalSolid);
                    is3DSunken = linesType.HasFlag(BorderLinesType.Horizontal3DSunken);
                    is3DRisen = linesType.HasFlag(BorderLinesType.Horizontal3DRisen);
                    offset = point.X;
                    break;
                case RectangleSide.Right:
                case RectangleSide.Left:
                    isDotted = linesType.HasFlag(BorderLinesType.VerticalDotted);
                    isSolid = linesType.HasFlag(BorderLinesType.VerticalSolid);
                    is3DSunken = linesType.HasFlag(BorderLinesType.Vertical3DSunken);
                    is3DRisen = linesType.HasFlag(BorderLinesType.Vertical3DRisen);
                    offset = point.Y;
                    break;
            }

            // Pokud se border nemá kreslit žádný:
            if (!(isDotted || isSolid || is3DSunken || is3DRisen)) return false;

            // Pokud border je obyčejný (nikoli 3D), a má se kreslit jako vnitřní buňka tabulky, a zde máme připravit stranu Top nebo Left, pak nekreslíme nic:
            if ((isDotted || isSolid) && drawAsInnerCell && (side == RectangleSide.Top || side == RectangleSide.Left)) return false;

            // Nastavíme vlastnosti pera:
            Pen pen = this.Pen;
            pen.Width = 1f;
            // Teoreticky mohou být nastaveny všechny flagy, ale prakticky reagujeme jen na první z nich:
            if (isDotted)
            {
                pen.Color = color;
                pen.DashStyle = DashStyle.Dot;
                // Tento řádek zajišťuje, že za sebou jdoucí buňky vykreslené tímto perem budou mít korektně navazující tečky 
                //  bez ohledu na to, zda začínají na sudém nebo lichém pixelu:
                pen.DashOffset = (offset % 2);
                return true;
            }
            else if (isSolid)
            {
                pen.Color = color;
                pen.DashStyle = DashStyle.Solid;
                return true;
            }
            else if (is3DSunken || is3DRisen)
            {
                pen.Color = DrawBorderGet3DColor(side, color, is3DSunken);
                pen.DashStyle = DashStyle.Solid;
                return true;
            }

            return false;
        }
        /// <summary>
        /// Vrací barvu pro linku na okraji (Border), pro 3D zobrazení.
        /// Simuluje tak 3D efekt pomocí světlejší / tmavší barvy.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="color"></param>
        /// <param name="is3DSunken">true pokud máme simulovat efekt Sunken (jakoby potopený dolů), false pokud jde o efekt Risen (jakoby vystupující nahoru)</param>
        /// <returns></returns>
        protected Color DrawBorderGet3DColor(RectangleSide side, Color color, bool is3DSunken)
        {
            float ratio = Skin.Modifiers.Effect3DRatio;
            Color dark = Skin.Modifiers.Effect3DDark;
            Color light = Skin.Modifiers.Effect3DLight;
            switch (side)
            {
                case RectangleSide.Left:
                case RectangleSide.Top:
                    // Pro Sunken efekt: tmavší, pro Risen efekt: světlejší odstín:
                    return ((is3DSunken) ? color.Morph(dark, ratio) : color.Morph(light, ratio));
                case RectangleSide.Right:
                case RectangleSide.Bottom:
                    // Pro Sunken efekt: světlejší, pro Risen efekt: tmavší odstín:
                    return ((is3DSunken) ? color.Morph(light, ratio) : color.Morph(dark, ratio));
            }
            return color;
        }
        private void _ResetPen(Color? color)
        {
            Pen pen = this.Pen;
            pen.Color = (color.HasValue ? color.Value : this.DefaultBorderColor);
            if (pen.Width != 1f) pen.Width = 1f;
            if (pen.DashStyle != DashStyle.Solid) pen.DashStyle = DashStyle.Solid;
        }
        private SolidBrush _SolidBrush;
        private Pen _Pen;
        private void _DrawSupportDispose()
        {
            if (this._SolidBrush != null)
                this._SolidBrush.Dispose();
            this._SolidBrush = null;
        }
        #endregion
        #region StopWatch
        /// <summary>
        /// Initialize Stopwatch
        /// </summary>
        private void _StopWatchInit()
        {
            this._StopwatchQueue = new Queue<decimal>();
            this._StopwatchFrequency = (decimal)System.Diagnostics.Stopwatch.Frequency;
            this.Stopwatch = new System.Diagnostics.Stopwatch();
        }
        /// <summary>
        /// Start new time-measure
        /// </summary>
        protected void StopwatchStart()
        {
            this.Stopwatch.Restart();
        }
        /// <summary>
        /// Stop current time-measure and save results
        /// </summary>
        protected void StopwatchStop()
        {
            if (this.Stopwatch.IsRunning)
            {
                this.Stopwatch.Stop();
                decimal time = (decimal)this.Stopwatch.ElapsedTicks / this._StopwatchFrequency;
                while (this._StopwatchQueue.Count >= 50)
                    this._StopwatchQueue.Dequeue();
                this._StopwatchQueue.Enqueue(time);
                this.StopwatchLastTime = time;
            }
        }
        /// <summary>
        /// Stop current time-measure and discard results
        /// </summary>
        protected void StopwatchDiscard()
        {
            if (this.Stopwatch.IsRunning)
            {
                this.Stopwatch.Stop();
            }
        }
        /// <summary>
        /// Paint stopwatch info to control
        /// </summary>
        /// <param name="e"></param>
        private void _PaintStopwatch(LayeredPaintEventArgs e)
        {
            decimal avt = this.StopwatchAverageTime;
            if (avt <= 0m) return;

            e.CopyContentOfLayer(e.ValidLayer, 3);

            decimal fps = Math.Round(1m / avt, 1);
            string info = "     " + fps.ToString("### ##0.0").Trim() + " fps";
            Size size = new System.Drawing.Size(90, 20);
            Rectangle bounds = size.AlignTo(this.ClientRectangle, ContentAlignment.BottomRight).Enlarge(0, 0, -1, -1);
            Graphics graphics = e.GraphicsForLayer(e.ValidLayer);
            Color backColor = Color.FromArgb(48, Color.LightSkyBlue);
            Color foreColor = Color.FromArgb(96, Color.Black);
            GraphicsPath gp = GPainter.CreatePathRoundRectangle(bounds, 2, 2);

            using (GPainter.GraphicsUseSmooth(graphics))
            {
                using (Brush b = Skin.CreateBrushForBackground(bounds, Orientation.Horizontal, GInteractiveState.Enabled, backColor))
                using (Pen p = new Pen(foreColor))
                {
                    graphics.FillPath(b, gp);
                    graphics.DrawPath(p, gp);
                }

                GPainter.DrawString(graphics, bounds, info, foreColor, FontInfo.Status, ContentAlignment.MiddleCenter);
            }
        }
        /// <summary>
        /// Time from last measure, in seconds
        /// </summary>
        protected decimal StopwatchLastTime { get; private set; }
        /// <summary>
        /// Average time from last (max 20) measure, in seconds
        /// </summary>
        protected decimal StopwatchAverageTime { get { return (this._StopwatchQueue.Count == 0 ? 0m : this._StopwatchQueue.Average()); } }
        private Queue<decimal> _StopwatchQueue;
        private decimal _StopwatchFrequency;
        protected System.Diagnostics.Stopwatch Stopwatch { get; private set; }
        #endregion
        #region Background Thread: animations, tooltip fader...
        /// <summary>
        /// Initiate background thread
        /// </summary>
        /// <remarks>Run only in GUI thread</remarks>
        private void _BackThreadInit()
        {
            this._BackThreadStop = false;
            this._BackThreadActive = false;
            this._BackSemaphore = new System.Threading.AutoResetEvent(false);
            this._BackThread = new System.Threading.Thread(this._BackThreadLoop);
            this._BackThread.IsBackground = true;
            this._BackThread.Name = "BackThreadControl";
            this._BackThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            this._BackThread.Start();
        }
        /// <summary>
        /// Stop background thread
        /// </summary>
        /// <remarks>Can run in boot thread (GUI and BackThread)</remarks>
        private void _BackThreadDone()
        {
            this._BackThreadStop = true;
            this._BackSemaphore.Set();
        }
        /// <summary>
        /// Ensure start of background thread animations.
        /// When background thread is active, do nothing.
        /// When is inactive, then activate this immediatelly.
        /// </summary>
        /// <remarks>Can run in boot thread (GUI and BackThread)</remarks>
        private void _BackThreadResume()
        {
            if (!this._BackThreadActive)
            {
                this._BackThreadActive = true;
                this._BackSemaphore.Set();
            }
        }
        /// <summary>
        /// Background thread main loop
        /// </summary>
        /// <remarks>Run only in BackThread thread</remarks>
        private void _BackThreadLoop()
        {
            while (!this._BackThreadStop)
            {
                this._BackSemaphore.WaitOne(this._BackThreadInterval);
                if (this._BackThreadActive)
                    this._BackThreadRun();
            }
        }
        /// <summary>
        /// Background thread Active-state action, is called once per 40 milisec (=25 / sec)
        /// </summary>
        /// <remarks>Run only in BackThread thread</remarks>
        private void _BackThreadRun()
        {
            bool needDraw = false;
            
            if (this._ToolTipNeedAnimation)
            {   // ToolTip needs animation: call its AnimateStep() method, this method returns true when ToolTip need new Draw:
                bool needDrawToolTip = this._ToolTip.AnimateTick();
                needDraw |= needDrawToolTip;
            }

            // Another animations:



            if (needDraw)
                this._BackThreadRunDraw();
        }
        /// <summary>
        /// Invoke GUI thread, call method for drawing ToolTip: _BackThreadRunDrawGui()
        /// </summary>
        /// <remarks>Can run in boot thread (GUI and BackThread)</remarks>
        private void _BackThreadRunDraw()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(this._BackThreadRunDrawGui));
            else
                this._BackThreadRunDrawGui();
        }
        /// <summary>
        /// Call Draw() for this control, for only ToolTip object.
        /// </summary>
        /// <remarks>Run only in GUI thread</remarks>
        private void _BackThreadRunDrawGui()
        {
            DrawRequest request = new DrawRequest(false, this._ToolTip, null);
            request.InteractiveMode = true;
            this.Draw(request);
        }
        /// <summary>
        /// Thread running in background for this control
        /// </summary>
        private System.Threading.Thread _BackThread;
        /// <summary>
        /// Semaphore for sleep and wake-up for background thread
        /// </summary>
        private System.Threading.AutoResetEvent _BackSemaphore;
        /// <summary>
        /// Interval for wake-up in main _BackThreadLoop: active = 40 milisec, inactive = 1 sec
        /// </summary>
        private TimeSpan _BackThreadInterval { get { return TimeSpan.FromMilliseconds(this._BackThreadActive ? 40d : 1000d); } }
        /// <summary>
        /// true = Back thread is active = run _BackThreadRun method once per 40 miliseconds.
        /// false = inactive thread, wait in main loop for 1 seconds before test activity...
        /// </summary>
        private bool _BackThreadActive;
        /// <summary>
        /// true = Back thread has been exited
        /// </summary>
        private bool _BackThreadStop;
        #endregion
        #region Public property and events
        /// <summary>
        /// Interactive items.
        /// Any collection can be stored.
        /// Set of value trigger this.Draw().
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<IInteractiveItem> Items
        {
            get { return this.ItemsList; }
            set { this._ItemsList.Clear(); this.AddItems(value); }
        }
        /// <summary>
        /// Add one interactive item.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(IInteractiveItem item)
        {
            this._AddItem(item);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public void AddItems(params IInteractiveItem[] items)
        {
            foreach (IInteractiveItem item in items)
                this._AddItem(item);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public void AddItems(IEnumerable<IInteractiveItem> items)
        {
            if (items != null)
            {
                foreach (IInteractiveItem item in items)
                    this._AddItem(item);
            }
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        private void _AddItem(IInteractiveItem item)
        {
            if (item != null)
            {
                item.Parent = this;
                this.ItemsList.Add(item);
            }
        }
        private void _CallInteractiveStateChanged(GInteractiveChangeStateArgs e)
        {
            this.OnInteractiveStateChanged(e);
            if (this.InteractiveStateChanged != null)
                this.InteractiveStateChanged(this, e);
        }
        protected virtual void OnInteractiveStateChanged(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Event after change of interactive state
        /// </summary>
        public event GInteractiveChangeStateHandler InteractiveStateChanged;
        #endregion
        #region Implementace IInteractiveParent : on totiž GInteractiveControl je umístěn jako Parent ve svých IInteractiveItem
        UInt32 IInteractiveParent.Id { get { return 0; } }
        GInteractiveControl IInteractiveParent.Host { get { return null; } }
        IInteractiveParent IInteractiveParent.Parent { get { return null; } set { } }
        GInteractiveStyles IInteractiveParent.Style { get { return GInteractiveStyles.None; } }
        Rectangle IInteractiveParent.BoundsClient { get { return this.ClientRectangle; } }
        void IInteractiveParent.Repaint() { this.Repaint(); }
        #endregion
    }
    #endregion
    #region class GCurrentItem : Working class for search current IInteractiveItem (and list of its Parents)
    /// <summary>
    /// GCurrentItem : Working class for search current IInteractiveItem (and list of its Parents)
    /// </summary>
    public class GCurrentItem
    {
        #region Constructor, basic properties
        private GCurrentItem(Point mousePoint)
        {
            this.FoundItemList = new List<FindItemPoint>();
            this.MousePoint = mousePoint;
            this.ItemBeginTime = DateTime.Now;
            this.EventCurrentTime = this.ItemBeginTime;
        }
        /// <summary>
        /// List of chain found items from root up to CurrentItem.
        /// At first index is item at root. CurrentItem is last item of this list.
        /// </summary>
        public List<FindItemPoint> FoundItemList { get; protected set; }
        /// <summary>
        /// true when any item is found
        /// </summary>
        public bool ExistsItem { get { return (this.FoundItemList.Count > 0); } }
        /// <summary>
        /// Found item
        /// </summary>
        public IInteractiveItem Item { get { return (this.ExistsItem ? this.FoundItemList[this.FoundItemList.Count - 1].Item : null);} }
        /// <summary>
        /// Absolute coordinates of item in coordinate system of Control
        /// </summary>
        public Point? CurrentItemLocation { get { return (this.ExistsItem ? (Point?)this.FoundItemList[this.FoundItemList.Count - 1].ItemAbsoluteLocation : (Point?)null); } }
        /// <summary>
        /// Relative coordinates of mouse in coordinate system of CurrentItem
        /// </summary>
        public Point? CurrentItemMouseRelativePoint { get { return (this.ExistsItem ? (Point?)this.FoundItemList[this.FoundItemList.Count - 1].RelativeMousePoint : (Point?)null); } }
        public Point MousePoint { get; protected set; }
        /// <summary>
        /// Time when GCurrentItem was created (=first event of this item)
        /// </summary>
        public DateTime ItemBeginTime { get; protected set; }
        /// <summary>
        /// Time of current event
        /// </summary>
        public DateTime EventCurrentTime { get; set; }
        #endregion
        #region Property from this.CurrentItem.Style, IsEnabled, IsVisible
        /// <summary>
        /// Style of current item contain Mouse?
        /// </summary>
        public bool CanMouse { get { return _HasStyle(GInteractiveStyles.Mouse); } }
        /// <summary>
        /// Style of current item contain Click?
        /// </summary>
        public bool CanClick { get { return _HasStyle(GInteractiveStyles.Click); } }
        /// <summary>
        /// Style of current item contain LongClick?
        /// </summary>
        public bool CanLongClick { get { return _HasStyle(GInteractiveStyles.LongClick); } }
        /// <summary>
        /// Style of current item contain DoubleClick?
        /// </summary>
        public bool CanDoubleClick { get { return _HasStyle(GInteractiveStyles.DoubleClick); } }
        /// <summary>
        /// Style of current item contain Drag and is Enabled?
        /// </summary>
        public bool CanDrag { get { return _HasStyle(GInteractiveStyles.Drag) && this.IsEnabled; } }
        /// <summary>
        /// Style of current item contain CallMouseOver?
        /// </summary>
        public bool CanOver { get { return _HasStyle(GInteractiveStyles.CallMouseOver); } }
        /// <summary>
        /// Style of current item contain KeyboardInput?
        /// </summary>
        public bool CanKeyboard { get { return _HasStyle(GInteractiveStyles.KeyboardInput); } }
        /// <summary>
        /// Current item is not null and IsEnabled?
        /// </summary>
        public bool IsEnabled { get { return (this.ExistsItem && this.Item.IsEnabled); } }
        /// <summary>
        /// Current item is not null and IsVisible?
        /// </summary>
        public bool IsVisible { get { return (this.ExistsItem && this.Item.IsVisible); } }
        private bool _HasStyle(GInteractiveStyles style)
        {
            return (this.ExistsItem && ((this.Item.Style & style) != 0));
        }
        #endregion
        #region FindItem, MapExchange
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point.
        /// Prefered current item above other items.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static GCurrentItem FindItemAtPoint(List<IInteractiveItem> list, GCurrentItem prevItem, Point point, bool withDisabled, params IInteractiveItem[] priorityItems)
        {
            return _FindItemAtPoint(list, prevItem, point, withDisabled, priorityItems);
        }
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point
        /// Prefered current item above other items.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private static GCurrentItem _FindItemAtPoint(List<IInteractiveItem> list, GCurrentItem prevItem, Point point, bool withDisabled, IInteractiveItem[] priorityItems)
        {
            GCurrentItem currItem = new GCurrentItem(point);

            if (priorityItems != null && priorityItems.Length > 0)
            {
                list = new List<IInteractiveItem>(list);
                list.AddRange(priorityItems);
            }

            // Search for current item: when have an existing current item (currentItem) then search for its FoundItemList preferred:
            IInteractiveItem[] items = list.ToArray();
            List<FindItemPoint> foundList = new List<FindItemPoint>();
            if (prevItem != null && prevItem.ExistsItem)
                foundList = _TryFindChildItemAtPoint(items, point, withDisabled, prevItem.FoundItemList);
            else
                foundList = _TryFindChildItemAtPoint(items, point, withDisabled, null);

            currItem.FoundItemList.AddRange(foundList);

            return currItem;
        }
        /// <summary>
        /// Search for item at specified point in list of items:
        /// item must be visible, must be active at point, and can be enabled, by parametrer withDisabled.
        /// If satisfactory item is IInteractiveContainer, then search continue in its ItemList.
        /// Lists are searched in order from last item to first.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="absolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="resultList"></param>
        /// <returns></returns>
        private static List<FindItemPoint> _TryFindChildItemAtPoint(IInteractiveItem[] items, Point absolutePoint, bool withDisabled, List<FindItemPoint> preferList)
        {
            List<FindItemPoint> resultList = new List<FindItemPoint>();
            Queue<FindItemPoint> preferredQueue = (preferList == null ? null : new Queue<FindItemPoint>(preferList));       // Chain of preferred items
            bool run = true;
            while (run)
            {
                run = false;
                bool isFound = false;
                IInteractiveItem foundItem = null;
                if (preferredQueue != null && preferredQueue.Count > 0)
                {   // Máme hledat nejprve v preferovaných prvcích?
                    FindItemPoint fip = preferredQueue.Dequeue();
                    if (fip.Item.HoldMouse)
                    {
                        isFound = _TryFindIndexOfItem(items, absolutePoint, withDisabled, fip.Item, out foundItem);
                        if (!isFound)
                            preferredQueue.Clear();
                    }
                }
                if (!isFound)
                    // Hledáme mimo preferované prvky, pouze podle souřadnice myši:
                    isFound = _TryFindIndexOfItem(items, absolutePoint, withDisabled, null, out foundItem);
               
                if (isFound)
                {   // Našli jsme prvek:
                    // Pokud jsme se zacyklili => skončíme chybou:
                    if (resultList.Any(iii => Object.ReferenceEquals(iii.Item, foundItem)))
                        throw new GraphLibCodeException("Při hledání vnořených vizuálních prvků v GCurrentItem došlo k zacyklení položek v seznamech IInteractiveItem.");

                    // Určíme relativní pozici myši vůči prvku, to se někdy hodí:
                    Point itemAbsolutePoint = foundItem.GetAbsoluteVisibleBounds().Location;
                    Point relativeMousePoint = absolutePoint.Sub(itemAbsolutePoint);                         // Relativní souřadnici pouze vložíme do FindItemPoint, ale pro hledání používáme stále absolutní souřadnici...
                    resultList.Add(new FindItemPoint(foundItem, itemAbsolutePoint, relativeMousePoint));

                    // Pokud prvek má Childs, projdeme je taky v další smyčce.
                    // Nemusíme řešit posun relativních souřadnic myši, jedeme stále v absolutních:
                    IEnumerable<IInteractiveItem> childs = foundItem.Childs;
                    if (childs != null)
                    {
                        items = childs.ToArray();
                        run = (items.Length > 0);
                    }
                }
            }
            return resultList;
        }
        /// <summary>
        /// Hledá v daném poli od konce prvek, který je aktivní na dané absolutní souřadnici.
        /// Pokud je zadán preferovaný prvek (preferredItem), kontroluje pouze tento prvek (pokud existuje, a vyhoví podmínkám).
        /// prvek musí být <see cref="IInteractiveItem.IsVisible"/>, musí být <see cref="IInteractiveItem.IsEnabled"/> nebo musí být povoleno vyhledání i Disabled prvků,
        /// a prvek musí být interaktivní na dané souřadnici.
        /// Výstup: true pokud je nalezeno (pak je nalezený prvek uložen v out parametru foundItem).
        /// </summary>
        /// <param name="list"></param>
        /// <param name="absolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="preferredItem"></param>
        /// <param name="foundItem"></param>
        /// <returns></returns>
        private static bool _TryFindIndexOfItem(IInteractiveItem[] items, Point absolutePoint, bool withDisabled, IInteractiveItem preferredItem, out IInteractiveItem foundItem)
        {
            for (int idx = items.Length - 1; idx >= 0; idx--)
            {   // Hledáme v poli prvků od konce = vizuálně od nejvýše vykresleného prvku:
                IInteractiveItem item = items[idx];
                if ((preferredItem == null || Object.ReferenceEquals(preferredItem, item)) 
                    && item.IsVisible 
                    && (withDisabled || item.IsEnabled) 
                    && item.IsActiveAtAbsolutePoint(absolutePoint))
                {   // Daný prvek vyhovuje => máme hotovo:
                    foundItem = item;
                    return true;
                }
            }
            foundItem = null;
            return false;
        }
        /// <summary>
        /// Return a relative point to current item
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point? GetRelativePointToCurrentItem(Point point)
        {
            return (this.ExistsItem ? (Point?)point.Sub(this.CurrentItemLocation.Value) : (Point?)null);
        }
        /// <summary>
        /// Create maps for calling events MouseLeave, MouseLeaveSubItem, MouseEnter, MouseEnterSubItem.
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <param name="leaveList"></param>
        /// <param name="enterList"></param>
        public static void MapExchange(GCurrentItem prev, GCurrentItem next, out List<IInteractiveItem> leaveList, out List<IInteractiveItem> enterList)
        {
            leaveList = new List<IInteractiveItem>();
            enterList = new List<IInteractiveItem>();

            if (prev == null && next == null) return;

            // Search for topmost index in booth arrays (prev.FoundItemList and next.FoundItemList), where is identical object:
            int prevCnt = (prev == null ? 0 : prev.FoundItemList.Count);
            int nextCnt = (next == null ? 0 : next.FoundItemList.Count);
            int commonCnt = (prevCnt < nextCnt ? prevCnt : nextCnt);           // Common count of both list (smallest Count from prev and next lists)
            int commonObjectIdx = -1;
            for (int i = 0; i < commonCnt; i++)
            {
                if (Object.ReferenceEquals(prev.FoundItemList[i].Item, next.FoundItemList[i].Item))
                    commonObjectIdx = i;         // On index (i) is equal object in list prev and next: we search for topmost index...
                else
                    break;                       // On index (i) is in list prev and next another object: we ending this search.
            }

            int prevLastIdx = prevCnt - 1;       // last index in prev list
            int nextLastIdx = nextCnt - 1;       // last index in next list

            // Is commonObjectIdx at last position in booth list? This is: booth lists (prev and next) contains equal sequence of identical items:
            if (commonObjectIdx >= 0 && prevLastIdx == commonObjectIdx && nextLastIdx == commonObjectIdx)
                return;                          // Booth (out) lists (leaveList and enterList) remaing empty, because there is no change from prev item to next item.

            int firstDiffIdx = commonObjectIdx + 1;
            // MouseLeave items from last index in prev, to item at [commonObjectIdx+1] (fill "MouseLeave" list):
            for (int i = prevLastIdx; i >= firstDiffIdx; i--)
                leaveList.Add(prev.FoundItemList[i].Item);

            // MouseEnter items in next, from [commonObjectIdx+1] to last index (fill "MouseEnter" list):
            for (int i = firstDiffIdx; i <= nextLastIdx; i++)
                enterList.Add(next.FoundItemList[i].Item);
        }
        #region class FindItemPoint : Informace o jednom nalezeném prvku a relativní poloze myši
        /// <summary>
        /// FindItemPoint : Informace o jednom nalezeném prvku a relativní poloze myši
        /// </summary>
        public class FindItemPoint
        {
            public FindItemPoint(IInteractiveItem item, Point itemLocation, Point relativeMousePoint)
            {
                this.Item = item;
                this.ItemAbsoluteLocation = itemLocation;
                this.RelativeMousePoint = relativeMousePoint;
            }
            /// <summary>
            /// Nalezený prvek
            /// </summary>
            public IInteractiveItem Item { get; private set; }
            /// <summary>
            /// Souřadnice prvku v Controlu, absolutní
            /// </summary>
            public Point ItemAbsoluteLocation { get; private set; }
            /// <summary>
            /// Souřadnice myši v prvku, relativně (tedy bod RelativeMousePoint 0,0 odpovídá poloze myši v levém horním rohu prvku Item)
            /// </summary>
            public Point RelativeMousePoint { get; private set; }
        }
        #endregion
        #endregion
        #region Static services
        /// <summary>
        /// Return true, when specified GCurrentItem is not null and its Item exists
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool HasItem(GCurrentItem item)
        {
            return (item != null && item.ExistsItem);
        }
        /// <summary>
        /// Return true, when is change between prev a next.
        /// I.e. when exact one of (prev, next) has item (HasItem()), or prev and next contain different item.
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static bool IsExchange(GCurrentItem prev, GCurrentItem next)
        {
            bool phi = GCurrentItem.HasItem(prev);
            bool nhi = GCurrentItem.HasItem(next);
            if (phi && nhi) return !Object.ReferenceEquals(prev.Item, next.Item);       // from item A to item B
            if (!phi && !nhi) return false;            // from None to None
            return true;                               // from object to none, or from none to object
        }
        /// <summary>
        /// return true, when items lastItem and currItem are equal, can doubleclick, 
        /// and time between events is in limit to doubleclick.
        /// </summary>
        /// <param name="lastItem"></param>
        /// <param name="currItem"></param>
        /// <returns></returns>
        public static bool IsDoubleClick(GCurrentItem lastItem, GCurrentItem currItem)
        {
            if (lastItem == null || currItem == null) return false;
            if (!lastItem.ExistsItem || !currItem.ExistsItem) return false;
            if (!currItem.CanDoubleClick) return false;
            if (!Object.ReferenceEquals(lastItem.Item, currItem.Item)) return false;
            TimeSpan time = currItem.EventCurrentTime.Subtract(lastItem.ItemBeginTime);
            return IsDoubleClick(lastItem.MousePoint, currItem.MousePoint, time);
        }
        /// <summary>
        /// Return true, when second mouse click (at point2), which is (time) after first click (at point1) is DoubleClick.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static bool IsDoubleClick(Point point1, Point point2, TimeSpan time)
        {
            if (time > DoubleClickTime) return false;
            Rectangle area = point1.CreateRectangleFromCenter(DoubleClickSize);
            return area.Contains(point2);
        }
        /// <summary>
        /// Return true, when time between MouseDown and MouseUp is long to be a LongClidk event.
        /// </summary>
        /// <param name="downTime"></param>
        /// <param name="currItem"></param>
        /// <returns></returns>
        public static bool IsLongClick(DateTime? downTime, GCurrentItem currItem)
        {
            DateTime now = DateTime.Now;
            if (!downTime.HasValue || currItem == null || !currItem.ExistsItem) return false;
            if (!currItem.CanLongClick) return false;
            TimeSpan time = currItem.EventCurrentTime.Subtract(downTime.Value);
            return IsLongClick(time);
        }
        /// <summary>
        /// Return true, when time between MouseDown and MouseUp is long to be a LongClidk event.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static bool IsLongClick(TimeSpan time)
        {
            if (time < LongClickTime) return false;
            return true;
        }
        /// <summary>
        /// DoubleClickTime (SystemInformation.DoubleClickTime as TimeSpan)
        /// </summary>
        public static TimeSpan DoubleClickTime
        {
            get
            {
                if (!_DoubleClickTime.HasValue)
                    _DoubleClickTime = TimeSpan.FromMilliseconds(SystemInformation.DoubleClickTime);
                return _DoubleClickTime.Value;
            }
        }
        /// <summary>
        /// DoubleClickTime (SystemInformation.DoubleClickTime as TimeSpan)
        /// </summary>
        public static TimeSpan LongClickTime
        {
            get
            {
                if (!_LongClickTime.HasValue)
                    _LongClickTime = TimeSpan.FromMilliseconds(2 * SystemInformation.MouseHoverTime);
                return _LongClickTime.Value;
            }
        }
        /// <summary>
        /// DoubleClickSize
        /// </summary>
        public static Size DoubleClickSize
        {
            get
            {
                if (!_DoubleClickOffset.HasValue)
                    _DoubleClickOffset = SystemInformation.DoubleClickSize;
                return _DoubleClickOffset.Value;
            }
        }
        private static TimeSpan? _DoubleClickTime;
        private static TimeSpan? _LongClickTime;
        private static Size? _DoubleClickOffset;
        #endregion
    }
    #endregion
}
