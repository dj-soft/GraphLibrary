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
            if (e.KeyCode == Keys.Escape && ((this._MouseDragState == MouseMoveDragState.DragMove && this._MouseDragMoveItem != null) || this._MouseDragState == MouseMoveDragState.DragFrame))
            {   // When we have Dragged Item, and Escape is pressed, then perform Cancel for current Drag operation:
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "KeyDown_DragCancel", ""))
                {
                    switch (this._MouseDragState)
                    {
                        case MouseMoveDragState.DragMove:
                            this._MouseDragMoveCancel();
                            this._InteractiveDrawRun();
                            break;
                        case MouseMoveDragState.DragFrame:
                            this._MouseDragFrameCancel();
                            this._InteractiveDrawRun();
                            break;
                    }
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
                GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(true, item, realChange, _GetStateAfterChange(realChange, item.IsEnabled), this.FindNewItemAtPoint, previewArgs, keyArgs, keyPressArgs);
                stateArgs.UserDragPoint = null;

                item.AfterStateChanged(stateArgs);

                Point? toolTipPoint = (stateArgs.HasToolTipData ? (Point?)(BoundsInfo.GetAbsoluteBounds(item).Location) : (Point?)null);
                this._InteractiveDrawStore(toolTipPoint, stateArgs);
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
            this._MouseDragMoveItem = null;
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
            using (ITraceScope scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseMove", ""))
            {
                try
                {
                    this._InteractiveDrawInit(e);
                    if (!this._MouseDownAbsolutePoint.HasValue && (e.Button == System.Windows.Forms.MouseButtons.Left || e.Button == System.Windows.Forms.MouseButtons.Right))
                    {   // Nějak jsme zmeškali event MouseDown, a nyní máme event MouseMove bez připravených dat z MouseDown:
                        this._MouseFell(e);
                        scope.AddItem("Missed: MouseFell!");
                    }

                    if (!this._MouseDownAbsolutePoint.HasValue)
                    {   // Myš se pohybuje nad Controlem, ale žádný knoflík myši není zmáčknutý:
                        this._MouseOver(e);
                        scope.Result = "MouseOver";
                    }
                    else
                    {   // Myš má zmáčknutý nějaký čudlík, a pohybuje se => tam je možností vícero:
                        this._OnMouseDrag(e, scope);
                    }
                }
                finally
                {
                    this._InteractiveDrawRun();
                }
            }
        }
        /// <summary>
        /// Myš je zmáčknutá a pohybuje se. Může to být Select, může to být Drag, může to být Nic...
        /// </summary>
        /// <param name="e"></param>
        /// <param name="scope"></param>
        private void _OnMouseDrag(MouseEventArgs e, ITraceScope scope)
        {
            MouseMoveDragState dragState = this._GetMouseMoveDragState(e.Location);
            switch (dragState)
            {
                case MouseMoveDragState.Start:
                    // Nyní začíná pohyb myši mimo výchozí prostor, musíme rozhodnout co to bude:
                    this._MouseDragStartDetect(e, scope);
                    break;

                case MouseMoveDragState.DragMove:
                    // Nyní probíhá rutinní Drag:
                    this._MouseDragMoveStep(e);
                    scope.Result = "MouseDragMoveStep";
                    break;

                case MouseMoveDragState.DragFrame:
                    // Nyní probíhá rutinní Frame:
                    this._MouseDragFrameStep(e);
                    scope.Result = "MouseDragFrameStep";
                    break;
            }
        }
        /// <summary>
        /// Myš je zmáčknutá a pohybuje se. Může to být Select, může to být Drag, může to být Nic...
        /// </summary>
        /// <param name="e"></param>
        /// <param name="scope"></param>
        private void _MouseDragStartDetect(MouseEventArgs e, ITraceScope scope)
        {
            if (this._MouseCurrentItem.CanFrameSelect)
            {   // Aktuální prvek podporuje Select pomocí Frame (tzn. má vlastnost <see cref="IInteractiveItem.IsSelectParent"/>)
                this._MouseDragState = MouseMoveDragState.DragFrame;
                this._MouseDragFrameBegin(e);
                this._MouseDragFrameStep(e);
                scope.Result = "MouseDragFrameBegin";
            }
            else if (this._MouseCurrentItem.SearchForDraggableItem())
            {   // Našli jsme nějaký prvek, který je ochotný s sebou nechat vláčet (podporuje Drag & Drop):
                // Myš se právě nyní pohnula z "Silent zone" (oblast okolo místa, kde byla myš zmáčknuta) => Drag & Drop začíná:
                this._MouseDragState = MouseMoveDragState.DragMove;
                this._MouseDragMoveBegin(e);
                this._MouseDragMoveStep(e);
                scope.Result = "MouseDragMoveBegin";
            }
            else
            {   // Neexistuje prvek vhodný pro Drag & Drop => nastavíme stav Cancel:
                this._MouseDragState = MouseMoveDragState.None;
                this._MouseDragMoveItemOffset = null;
                this._CurrentMouseDragCanceled = true;
            }
        }
        /// <summary>
        /// Metoda určí, v jakém stavu je nyní proces Drag na základě aktuálního pohybu myši.
        /// Reaguje na hodnoty: <see cref="_CurrentMouseDragCanceled"/>, <see cref="_MouseDragStartBounds"/> a na pozici myši v parametru mousePoint.
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        protected MouseMoveDragState _GetMouseMoveDragState(Point mousePoint)
        {
            // Pokud byl dán Cancel, pak bez ohledu na další vracím None:
            if (this._CurrentMouseDragCanceled) return MouseMoveDragState.None;          // Proces Drag & Drop probíhal, ale byl stornován (klávesou Escape)

            // Pokud control již rozhodl o stavu, pak jej vrátíme bez dalšího zkoumání:
            MouseMoveDragState state = this._MouseDragState;
            if (state == MouseMoveDragState.DragMove || state == MouseMoveDragState.DragFrame) return state;

            // Dosud nebylo o stavu rozhodnuto, detekujeme pozici myši vzhledem k výchozímu bodu:
            Rectangle? startBounds = this._MouseDragStartBounds;
            if (startBounds.HasValue && startBounds.Value.Contains(mousePoint))          // Pozice myši je stále uvnitř Silent zone => čekáme na nějaký větší pohyb
                return MouseMoveDragState.Wait;

            return MouseMoveDragState.Start;
        }
        /// <summary>
        /// Stavy procesu Drag na základě pohybu myši a stavu controlu
        /// </summary>
        protected enum MouseMoveDragState
        {
            /// <summary>
            /// Aktuální situace nemá nic společného s MouseDrag, typicky: myš není zmáčknutá
            /// </summary>
            None,
            /// <summary>
            /// Myš je zmáčknutá, ale její souřadnice jsou uvnitř prostoru v němž se malé pohybi myši ignorují
            /// </summary>
            Wait,
            /// <summary>
            /// Myš je zmáčknutá, pohybuje se a právě nyní se dostala mimo prostor odpovídající hodnotě <see cref="Wait"/>.
            /// Aplikace nyní musí určit, zda se jedná o akci <see cref="DragMove"/>, nebo 
            /// </summary>
            Start,
            /// <summary>
            /// Myš je zmáčknutá a pohybuje se, již je mimo startovní prostor,
            /// a aplikace se rozhodla pro přetahování určitého objektu pomocí myši (DragMove : Drag & Drop).
            /// </summary>
            DragMove,
            /// <summary>
            /// Myš je zmáčknutá a pohybuje se, již je mimo startovní prostor,
            /// a aplikace se rozhodla pro rámování části prostoru pomocí myši a následné selectování vhodných objektů.
            /// </summary>
            DragFrame
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
                    else if (this._MouseDragState == MouseMoveDragState.DragMove)
                        this._MouseDragMoveDone(e);
                    else if (this._MouseDragState == MouseMoveDragState.DragFrame)
                        this._MouseDragFrameDone(e);
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
        #region Řízení konkrétních aktivit myši, již rozčleněné; volání jednoduchých interaktivních metod (Over, Fell, Raise, Whell)
        /// <summary>
        /// Tato akce se volá výhradně když se myš pohybuje bez Drag & Drop = bez stisknutého tlačítka
        /// </summary>
        /// <param name="e"></param>
        private void _MouseOver(MouseEventArgs e)
        {
            if (e != null)
            {
                GActivePosition gci = this.FindActivePositionAtPoint(e.Location, true);
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
            GActivePosition gci = this.FindActivePositionAtPoint(e.Location, false);

            this._ItemMouseExchange(this._MouseCurrentItem, gci, this._MouseCurrentRelativePoint);
            this._ItemKeyboardExchange(this._KeyboardCurrentItem, gci.ActiveItem, false);

            this._MouseDownAbsolutePoint = e.Location;
            this._MouseDownTime = DateTime.Now;
            this._MouseDownButtons = e.Button;
            this._MouseDragStartBounds = e.Location.CreateRectangleFromCenter(this._DragStartSize);
            this._MouseDragMoveItemOffset = null;
            this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDown, this._MouseCurrentRelativePoint, null);
        }
        private void _MouseRaise(MouseEventArgs e)
        {
            if (this._MouseCurrentItem != null)
            {
                this._MouseCurrentItem.CurrentTime = DateTime.Now;
                this._MouseCurrentRelativePoint = this._GetRelativePointToCurrentItem(e.Location);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftUp, this._MouseCurrentRelativePoint, null);
                if (this._MouseCurrentItem.CanDoubleClick && GActivePosition.IsDoubleClick(this._MouseClickedItem, this._MouseCurrentItem))
                {
                    this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDoubleClick, this._MouseCurrentRelativePoint, null);
                }
                else if (this._MouseCurrentItem.CanLongClick && GActivePosition.IsLongClick(this._MouseDownTime, this._MouseCurrentItem))
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
        private void _MouseOneWheel(MouseEventArgs e)
        {
            GActivePosition gci = this.FindActivePositionAtPoint(e.Location, false);
            this._ItemMouseExchange(this._MouseCurrentItem, gci, this._MouseCurrentRelativePoint);
            GInteractiveChangeState change = (e.Delta > 0 ? GInteractiveChangeState.WheelUp : GInteractiveChangeState.WheelDown);
            this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, change, this._MouseCurrentRelativePoint, null, true);   // ,,, true => opakovat volání akce, dokud ji některý prvek v hierarchii nevyřeší.
        }
        #endregion
        #region Řízení procesu MouseDragMove = přesouvání prvku Drag & Drop
        private void _MouseDragMoveBegin(MouseEventArgs e)
        {
            // Relativní pozice myši v okamžiku MouseDown, nikoli aktuální pozice (ta už je mimo prostor _CurrentMouseDragStart):
            this._MouseCurrentRelativePoint = _GetRelativePoint(this._MouseDownAbsolutePoint.Value, this._MouseCurrentItem);
            if (this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.CurrentTime = DateTime.Now; 
                this._MouseDragMoveItem = this._MouseCurrentItem;
                this._MouseDragMoveItemOriginBounds = this._MouseCurrentItem.ActiveItem.Bounds;
                Point? userDragPoint = null;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMoveBegin, this._MouseCurrentRelativePoint, null, ref userDragPoint);
                if (userDragPoint.HasValue)
                    this._UserDragPointOffset = userDragPoint.Value.Sub(this._MouseDownAbsolutePoint.Value);
            }
            this._MouseDragMoveItemOffset = this._GetRelativePointToCurrentItem(this._MouseDownAbsolutePoint.Value);
            this._MouseDragStartBounds = null;
        }
        private void _MouseDragMoveStep(MouseEventArgs e)
        {
            if (this._MouseDragMoveItem != null && this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.CurrentTime = DateTime.Now;
                Point? userDragPoint = null;
                if (this._UserDragPointOffset.HasValue)
                    userDragPoint = e.Location.Add(this._UserDragPointOffset.Value);
                this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, this._MouseCurrentItem);
                Point shift = e.Location.Sub(this._MouseDownAbsolutePoint.Value);
                Rectangle dragToBounds = this._MouseDragMoveItemOriginBounds.Value.ShiftBy(shift);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMoveStep, this._MouseCurrentRelativePoint, dragToBounds, ref userDragPoint);
            }
        }
        private void _MouseDragMoveCancel()
        {
            if (this._MouseDragMoveItem != null && this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.CurrentTime = DateTime.Now;
                Point? userDragPoint = null;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMoveCancel, null, null, ref userDragPoint);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMoveEnd, null, null);
                this._RepaintAllItems = true;
                this._MouseDragMoveItem = null;
            }
            this._MouseDragMoveItemOffset = null;              // on primary handler _MouseUp() will be called _MouseRaise(), instead of _MouseDragDone()!  In _MouseDragDone() will be called _MouseDownReset().
            this._CurrentMouseDragCanceled = true;
        }
        private void _MouseDragMoveDone(MouseEventArgs e)
        {
            this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, this._MouseCurrentItem);
            this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftUp, this._MouseCurrentRelativePoint, null);
            if (this._MouseDragMoveItem != null && this._MouseCurrentItem.CanDrag)
            {
                this._MouseCurrentItem.CurrentTime = DateTime.Now;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMoveDone, this._MouseCurrentRelativePoint, null);
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragMoveEnd, null, null);
            }
            this._MouseDragMoveItem = null;
            this._MouseDragState = MouseMoveDragState.None;
            this._MouseDownReset();
        }
        /// <summary>
        /// Stav procesu MouseDrag
        /// </summary>
        private MouseMoveDragState _MouseDragState { get; set; }
        /// <summary>
        /// Prostor okolo bodu <see cref="_MouseDownAbsolutePoint"/>, který reprezentuje "mrtvou zónu":
        /// Pokud se stiskne myš, pak se vygeneruje tato souřadnice <see cref="_MouseDragStartBounds"/>. 
        /// Pokud se nadále myš drží v této souřadnici, nebere se to jako pohyb myši (nevolají se eventy MouseDrag).
        /// Pokud je ale myš stisknutá, a její souřadnice již vyběhnou z tohoto prostoru, bere se to jako proces Drag.
        /// </summary>
        private Rectangle? _MouseDragStartBounds { get; set; }
        /// <summary>
        /// Prvek, který je aktuálně přetahován (DragMove):
        /// Sekvence událostí je: _MouseDragBegin(), _MouseDragMove() _MouseDragCancel() nebo _MouseDragEnd().
        /// </summary>
        private GActivePosition _MouseDragMoveItem { get; set; }
        /// <summary>
        /// Původní souřadnice <see cref="IInteractiveItem.Bounds"/> prvku, který je aktuálně přetahován <see cref="_MouseDragMoveItem"/>.
        /// Tato hodnota je předávána do eventů MouseDragMove.
        /// Hodnota je vložena v události MouseDragBegin.
        /// </summary>
        private Rectangle? _MouseDragMoveItemOriginBounds { get; set; }
        /// <summary>
        /// Offset (=relativní vzdálenost pohybu) mezi pozicí myši na začátku procesu DragMove a její aktuální pozicí.
        /// Používá se pro výpočet cílové souřadnice prvku v procesu DragMove.
        /// </summary>
        private Point? _MouseDragMoveItemOffset { get; set; }
        /// <summary>
        /// Obsahuje true po požadavku Cancel v procesu DragMove.
        /// Nastavuje se v <see cref="_MouseDragMoveCancel"/>, resetuje se v <see cref="_MouseDownReset"/>.
        /// Pokud je true, pak událost <see cref="OnMouseMove(MouseEventArgs)"/> neřeší nic okolo procesu DragMove.
        /// Na konci procesu Drag se nevola MouseDragDone, ale jen MouseRaise.
        /// </summary>
        private bool _CurrentMouseDragCanceled { get; set; }
        /// <summary>
        /// Offset of UserDragPoint (from event DragBegin) relative to _CurrentMouseDownLocation (in control coordinates).
        /// This offset will be added to MouseLocation (in control coordinates) during drag, and sent to DragMove events.
        /// </summary>
        private Point? _UserDragPointOffset { get; set; }
        /// <summary>
        /// Prvek, který je Parentem aktuální akce DragFrame.
        /// </summary>
        private GActivePosition _MouseDragFrameItem { get; set; }

        #endregion
        #region Řízení procesu MouseDragFrame = výběr prvků zarámováním, včetně vykreslení
        private void _MouseDragFrameBegin(MouseEventArgs e)
        {
            // Relativní pozice myši v okamžiku MouseDown, nikoli aktuální pozice (ta už je mimo prostor _CurrentMouseDragStart):
            this._MouseCurrentRelativePoint = _GetRelativePoint(this._MouseDownAbsolutePoint.Value, this._MouseCurrentItem);
            if (this._MouseCurrentItem.CanFrameSelect)
            {
                this._MouseCurrentItem.CurrentTime = DateTime.Now;
                this._MouseDragFrameItem = this._MouseCurrentItem;
                Rectangle? frameWorkArea;
                this._ItemMouseCallStateChangedEvent(this._MouseCurrentItem, GInteractiveChangeState.LeftDragFrameBegin, this._MouseCurrentRelativePoint, out frameWorkArea);
                this._MouseDragFrameWorkArea = frameWorkArea;
            }
            this._MouseDragStartBounds = null;
        }
        private void _MouseDragFrameStep(MouseEventArgs e)
        {
            if (!this._MouseDownAbsolutePoint.HasValue) return;
            Rectangle frameBounds = DrawingExtensions.FromPoints(this._MouseDownAbsolutePoint.Value, e.Location);
            if (this._MouseDragFrameWorkArea.HasValue)
                frameBounds = Rectangle.Intersect(this._MouseDragFrameWorkArea.Value, frameBounds);

            this._MouseDragFrameCurrentBounds = frameBounds;

            Tuple<IInteractiveItem, Rectangle>[] items = GActivePosition.FindItemsAtBounds(this.ClientSize, this.ItemsList, frameBounds,
                _MouseDragFrameFilterScan, 
                _MouseDragFrameFilterAccept
                );

            if (items.Length > 0)
            {

            }
        }
        /// <summary>
        /// Metoda vrací true, pokud daný prvek může být scanován co do jeho Childs prvků
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemAbsoluteBounds"></param>
        /// <returns></returns>
        private bool _MouseDragFrameFilterScan(IInteractiveItem item, Rectangle itemAbsoluteBounds)
        {
            return (item.IsVisible && item.IsEnabled);
        }
        /// <summary>
        /// Metoda vrací true, pokud daný prvek má být akceptován do výstupního pole DragFrame
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemAbsoluteBounds"></param>
        /// <returns></returns>
        private bool _MouseDragFrameFilterAccept(IInteractiveItem item, Rectangle itemAbsoluteBounds)
        {
            return (item.IsVisible && item.IsEnabled && item.IsSelectable);
        }
        private void _MouseDragFrameCancel()
        {

        }
        /// <summary>
        /// Obsahuje true, pokud se má kreslit FrameBounds = oblast selectování (<see cref="_MouseDragFrameCurrentBounds"/>).
        /// </summary>
        private bool _NeedDrawFrameBounds { get { return (this._MouseDragFrameCurrentBounds.HasValue && this._MouseDragFrameCurrentBounds.Value.HasPixels()); } }
        /// <summary>
        /// Zajistí vykreslení oblasti FrameSelect
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="layer"></param>
        private void _PaintFrameBounds(Graphics graphics, GInteractiveDrawLayer layer)
        {
            graphics.DrawRectangle(Pens.Black, this._MouseDragFrameCurrentBounds.Value);
        }
        private void _MouseDragFrameDone(MouseEventArgs e)
        {


            this._MouseDragFrameCurrentBounds = null;
            this._MouseDragState = MouseMoveDragState.None;
            this._MouseDownReset();
        }
        /// <summary>
        /// Souřadnice prostoru, do něhož má být omezen proces DragFrame.
        /// Prostor deklaruje prvek Parent na začátku procesu DragFrame ve své události .
        /// Následný proces DragFrame pak ořezává reálně zadaný prostor selectovaný pohybem myši pouze do této oblasti.
        /// Může být null, pak nebude prostor omezen.
        /// </summary>
        private Rectangle? _MouseDragFrameWorkArea { get; set; }
        /// <summary>
        /// Souřadnice prostoru, který je aktuálně zarámován v režimu DragFrame.
        /// Měl by být vykreslen do Interactive vrstvy grafiky.
        /// Pokud je null, pak se nekreslí.
        /// </summary>
        private Rectangle? _MouseDragFrameCurrentBounds { get; set; }


        #endregion
        #region Metody pro volání interaktivních metod na prvcích IInteractiveItem.AfterStateChanged() atd
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

                    case GInteractiveChangeState.LeftDragMoveBegin:
                    case GInteractiveChangeState.RightDragMoveBegin:
                        return (right ? GInteractiveChangeState.RightDragMoveBegin : GInteractiveChangeState.LeftDragMoveBegin);
                    case GInteractiveChangeState.LeftDragMoveStep:
                    case GInteractiveChangeState.RightDragMoveStep:
                        return (right ? GInteractiveChangeState.RightDragMoveStep : GInteractiveChangeState.LeftDragMoveStep);
                    case GInteractiveChangeState.LeftDragMoveCancel:
                    case GInteractiveChangeState.RightDragMoveCancel:
                        return (right ? GInteractiveChangeState.RightDragMoveCancel : GInteractiveChangeState.LeftDragMoveCancel);
                    case GInteractiveChangeState.LeftDragMoveDone:
                    case GInteractiveChangeState.RightDragMoveDone:
                        return (right ? GInteractiveChangeState.RightDragMoveDone : GInteractiveChangeState.LeftDragMoveDone);
                    case GInteractiveChangeState.LeftDragMoveEnd:
                    case GInteractiveChangeState.RightDragMoveEnd:
                        return (right ? GInteractiveChangeState.RightDragMoveEnd : GInteractiveChangeState.LeftDragMoveEnd);

                    case GInteractiveChangeState.LeftDragFrameBegin:
                    case GInteractiveChangeState.RightDragFrameBegin:
                        return (right ? GInteractiveChangeState.RightDragFrameBegin : GInteractiveChangeState.LeftDragFrameBegin);
                    case GInteractiveChangeState.LeftDragFrameSelect:
                    case GInteractiveChangeState.RightDragFrameSelect:
                        return (right ? GInteractiveChangeState.RightDragFrameSelect : GInteractiveChangeState.LeftDragFrameSelect);
                    case GInteractiveChangeState.LeftDragFrameDone:
                    case GInteractiveChangeState.RightDragFrameDone:
                        return (right ? GInteractiveChangeState.RightDragFrameDone : GInteractiveChangeState.LeftDragFrameDone);
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
                case GInteractiveChangeState.LeftDragMoveBegin: return GInteractiveState.LeftDrag;
                case GInteractiveChangeState.LeftDragMoveStep: return GInteractiveState.LeftDrag;
                case GInteractiveChangeState.LeftDragMoveCancel: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDragMoveDone: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDragMoveEnd: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDragFrameBegin: return GInteractiveState.LeftFrame;
                case GInteractiveChangeState.LeftDragFrameSelect: return GInteractiveState.LeftFrame;
                case GInteractiveChangeState.LeftDragFrameDone: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftUp: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftLongClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.LeftDoubleClick: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDown: return GInteractiveState.RightDown;
                case GInteractiveChangeState.RightDragMoveBegin: return GInteractiveState.RightDrag;
                case GInteractiveChangeState.RightDragMoveStep: return GInteractiveState.RightDrag;
                case GInteractiveChangeState.RightDragMoveCancel: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDragMoveDone: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDragMoveEnd: return GInteractiveState.MouseOver;
                case GInteractiveChangeState.RightDragFrameBegin: return GInteractiveState.RightFrame;
                case GInteractiveChangeState.RightDragFrameSelect: return GInteractiveState.RightFrame;
                case GInteractiveChangeState.RightDragFrameDone: return GInteractiveState.MouseOver;
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
                case GInteractiveChangeState.LeftDragMoveBegin: return DragActionType.DragThisStart;
                case GInteractiveChangeState.LeftDragMoveStep: return DragActionType.DragThisMove;
                case GInteractiveChangeState.LeftDragMoveCancel: return DragActionType.DragThisCancel;
                case GInteractiveChangeState.LeftDragMoveDone: return DragActionType.DragThisDrop;
                case GInteractiveChangeState.LeftDragMoveEnd: return DragActionType.DragThisEnd;
                case GInteractiveChangeState.RightDragMoveBegin: return DragActionType.DragThisStart;
                case GInteractiveChangeState.RightDragMoveStep: return DragActionType.DragThisMove;
                case GInteractiveChangeState.RightDragMoveCancel: return DragActionType.DragThisCancel;
                case GInteractiveChangeState.RightDragMoveDone: return DragActionType.DragThisDrop;
                case GInteractiveChangeState.RightDragMoveEnd: return DragActionType.DragThisEnd;
            }
            return DragActionType.None;
        }
        /// <summary>
        /// Call events MouseLeave and MouseEnter when neccessary
        /// </summary>
        /// <param name="gcItemPrev"></param>
        /// <param name="gcItemNext"></param>
        private void _ItemMouseExchange(GActivePosition gcItemPrev, GActivePosition gcItemNext)
        {
            this._ItemMouseExchange(gcItemPrev, gcItemNext, null);
        }
        /// <summary>
        /// Call events MouseLeave and MouseEnter when neccessary
        /// </summary>
        /// <param name="gcItemPrev"></param>
        /// <param name="gcItemNext"></param>
        private void _ItemMouseExchange(GActivePosition gcItemPrev, GActivePosition gcItemNext, Point? mouseRelativePoint)
        {
            List<IInteractiveItem> leaveList, enterList;
            GActivePosition.MapExchange(gcItemPrev, gcItemNext, out leaveList, out enterList);

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
        private void _ItemMouseCallStateChangedEvent(GActivePosition gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, Rectangle? dragToArea)
        {
            Point? userDragPoint = null;
            Rectangle? frameWorkArea;
            this._ItemMouseCallStateChangedEvent(gcItem, change, mouseRelativePoint, dragToArea, false, ref userDragPoint, out frameWorkArea);
        }
        /// <summary>
        /// Call interactive event for specified item and change.
        /// </summary>
        /// <param name="gcItem"></param>
        /// <param name="state"></param>
        private void _ItemMouseCallStateChangedEvent(GActivePosition gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, out Rectangle? frameWorkArea)
        {
            Rectangle? dragToArea = null;
            Point? userDragPoint = null;
            this._ItemMouseCallStateChangedEvent(gcItem, change, mouseRelativePoint, dragToArea, false, ref userDragPoint, out frameWorkArea);
        }
        /// <summary>
        /// Call interactive event for specified item and change.
        /// </summary>
        /// <param name="gcItem"></param>
        /// <param name="state"></param>
        private void _ItemMouseCallStateChangedEvent(GActivePosition gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, Rectangle? dragToArea, bool recurseToSolver)
        {
            Point? userDragPoint = null;
            Rectangle? frameWorkArea;
            this._ItemMouseCallStateChangedEvent(gcItem, change, mouseRelativePoint, dragToArea, recurseToSolver, ref userDragPoint, out frameWorkArea);
        }
        /// <summary>
        /// Call interactive event for specified item and change.
        /// </summary>
        /// <param name="gcItem">Current item under mouse</param>
        /// <param name="change">Change of state, independently on MouseButton (i.e. LeftDown, in situation where is pressed Right Mouse button). Real change state is detected in this method, with _GetStateForCurrentButton() method.</param>
        private void _ItemMouseCallStateChangedEvent(GActivePosition gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, Rectangle? dragToArea, ref Point? userDragPoint)
        {
            Rectangle? frameWorkArea;
            this._ItemMouseCallStateChangedEvent(gcItem, change, mouseRelativePoint, dragToArea, false, ref userDragPoint, out frameWorkArea);
        }
        /// <summary>
        /// Metoda zavolá klíčovou událost <see cref="IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro aktivní prvek dle parametru "item".
        /// Podle potřeby (podle typu akce) vyvolá i událost <see cref="IInteractiveItem.DragAction(GDragActionArgs)"/> .
        /// Je použito pouze pro většinu eventů.
        /// </summary>
        /// <param name="gcItem">Current item under mouse</param>
        /// <param name="change">Change of state, independently on MouseButton (i.e. LeftDown, in situation where is pressed Right Mouse button). Real change state is detected in this method, with _GetStateForCurrentButton() method.</param>
        private void _ItemMouseCallStateChangedEvent(GActivePosition gcItem, GInteractiveChangeState change, Point? mouseRelativePoint, Rectangle? dragToArea, 
            bool recurseToSolver, ref Point? userDragPoint, out Rectangle? frameWorkArea)
        {
            frameWorkArea = null;
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, gcItem.IsEnabled);
            GInteractiveState state = (gcItem.HasItem ? _GetStateAfterChange(realChange, gcItem.ActiveItem.IsEnabled) : GInteractiveState.Disabled);
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(gcItem.HasItem, gcItem.ActiveItem, realChange, state, this.FindNewItemAtPoint, this._MouseCurrentAbsolutePoint, mouseRelativePoint, this._MouseDragMoveItemOriginBounds, dragToArea);
            stateArgs.UserDragPoint = userDragPoint;

            if (gcItem.HasItem)
            {
                gcItem.CallAfterStateChanged(stateArgs, recurseToSolver);
                this._ItemMouseCallDragEvent(gcItem.ActiveItem, stateArgs);
                frameWorkArea = stateArgs.DragFrameWorkArea;
            }

            this._CallInteractiveStateChanged(stateArgs);

            this._InteractiveDrawStore(stateArgs);

            userDragPoint = stateArgs.UserDragPoint;
        }
        /// <summary>
        /// Metoda zavolá klíčovou událost <see cref="IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro aktivní prvek dle parametru "item".
        /// Podle potřeby (podle typu akce) vyvolá i událost <see cref="IInteractiveItem.DragAction(GDragActionArgs)"/> .
        /// Je použito pouze pro eventy MouseEnter a MouseLeave.
        /// </summary>
        /// <param name="gci"></param>
        /// <param name="item"></param>
        /// <param name="change"></param>
        private void _ItemMouseCallStateChangedEvent(GActivePosition gci, IInteractiveItem item, GInteractiveChangeState change)
        {
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, item.IsEnabled);
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(true, item, realChange, _GetStateAfterChange(realChange, item.IsEnabled), this.FindNewItemAtPoint);
            stateArgs.UserDragPoint = null;

            item.AfterStateChanged(stateArgs);
            this._ItemMouseCallDragEvent(item, stateArgs);

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
            this._MouseDragState = MouseMoveDragState.None;
            this._MouseDownAbsolutePoint = null;
            this._MouseDownTime = null;
            this._MouseDownButtons = null;
            this._MouseDragStartBounds = null;
            this._MouseDragMoveItemOffset = null;
            this._CurrentMouseDragCanceled = false;
            this._MouseDragMoveItemOriginBounds = null;
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
        /// Vytvoří a vrátí instanci <see cref="GActivePosition"/>, která bude obsahovat plnou cestu k prvku, který je na dané absolutní souřadnici.
        /// Dovolí najít i prvky, které mají <see cref="IInteractiveItem.IsEnabled"/> = false.
        /// Tato metoda nebere ohled na aktuálně nalezený prvek (<see cref="_MouseCurrentItem"/>), ignoruje tedy vlastnost <see cref="IInteractiveItem.HoldMouse"/> prvku,
        /// který je nalezen jako aktivní v <see cref="_MouseCurrentItem"/>.
        /// Tato metoda se používá jako delegát "item searcher" (searchItemMethod) v konstruktoru argumentu <see cref="GInteractiveChangeStateArgs"/>,
        /// a slouží aplikačnímu kódu, proto se liší od běžné metody <see cref="FindActivePositionAtPoint(Point, bool)"/>.
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <returns></returns>
        protected IInteractiveItem FindNewItemAtPoint(Point mouseAbsolutePoint, bool withDisabled)
        {
            GActivePosition activePosition = GActivePosition.FindItemAtPoint(this.ClientSize, this.ItemsList, null, mouseAbsolutePoint, withDisabled);
            return (activePosition.HasItem ? activePosition.ActiveItem : null);
        }
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="GActivePosition"/>, která bude obsahovat plnou cestu k prvku, který je na dané absolutní souřadnici.
        /// Dovolí najít i prvky, které mají <see cref="IInteractiveItem.IsEnabled"/> = false.
        /// Tato metoda BERE ohled na aktuálně nalezený prvek (<see cref="_MouseCurrentItem"/>), a pokud má vlastnost <see cref="IInteractiveItem.HoldMouse"/> = true, 
        /// pak tomuto prvku dává přednost (pokud to lze).
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <returns></returns>
        protected GActivePosition FindActivePositionAtPoint(Point mouseAbsolutePoint, bool withDisabled)
        {
            GActivePosition activePosition = (this._ProgressItem.IsVisible
                ? GActivePosition.FindItemAtPoint(this.ClientSize, this.ItemsList, this._MouseCurrentItem, mouseAbsolutePoint, withDisabled, this._ProgressItem)
                : GActivePosition.FindItemAtPoint(this.ClientSize, this.ItemsList, this._MouseCurrentItem, mouseAbsolutePoint, withDisabled));

            this._MouseCurrentRelativePoint = _GetRelativePoint(mouseAbsolutePoint, activePosition);
            return activePosition;
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
        protected static Point? _GetRelativePoint(Point point, GActivePosition gCurrentItem)
        {
            return (gCurrentItem == null ? (Point?)null : (Point?)gCurrentItem.GetRelativePointToCurrentItem(point));
        }
        #endregion
        #region Myší proměnné
        /// <summary>
        /// Coordinates of mouse, in control coordinates (=Absolute), current coordinates (in current event).
        /// </summary>
        private Point? _MouseCurrentAbsolutePoint { get; set; }
        /// <summary>
        /// Time in which were mouse down in MouseDown event (for LongClick detection)
        /// </summary>
        private DateTime? _MouseDownTime { get; set; }
        /// <summary>
        /// Coordinates of mouse, in control coordinates (=Absolute), where mouse-down event occurs.
        /// </summary>
        private Point? _MouseDownAbsolutePoint { get; set; }
        /// <summary>
        /// Buttons which were down in MouseDown event
        /// </summary>
        private MouseButtons? _MouseDownButtons { get; set; }
        /// <summary>
        /// Current item with any mouse-interaction, can be null
        /// </summary>
        private GActivePosition _MouseCurrentItem { get; set; }
        /// <summary>
        /// Item, which was last clicked.
        /// </summary>
        private GActivePosition _MouseClickedItem { get; set; }
        /// <summary>
        /// Coordinates of mouse, relative to current interactive item bounds.
        /// </summary>
        private Point? _MouseCurrentRelativePoint { get; set; }

        /// <summary>
        /// Velikost oblasti <see cref="_MouseDragStartBounds"/> = "No-Drag zone", kde se ignoruje pohyb myši na začátku procesu Drag.
        /// Je nastaven na hodnotu <see cref="SystemInformation.DragSize"/>, ale může být upraven.
        /// </summary>
        private Size _DragStartSize { get; set; }
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
            DrawRequest request = new DrawRequest(this._RepaintAllItems, this._NeedDrawFrameBounds, this._ToolTip, this._ProgressItem);
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
        #region Podpora pro ToolTip
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
        #region Podpora pro Progress
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
            DrawRequest request = new DrawRequest(false, this._NeedDrawFrameBounds, this._ToolTip, this._ProgressItem);
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
                    request = new DrawRequest(true, this._NeedDrawFrameBounds, this._ToolTip, this._ProgressItem);
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
                    Graphics graphics2 = e.GraphicsForLayer(2);
                    if (request.DynamicItems.Count > 0)
                    {
                        this._PaintItems(graphics2, request.DynamicItems, GInteractiveDrawLayer.Dynamic);
                        scope.AddItem("Layer Dynamic, Items: " + request.DynamicItems.Count.ToString());
                    }
                    if (request.DrawFrameSelect)
                    {
                        this._PaintFrameBounds(graphics2, GInteractiveDrawLayer.Dynamic);
                    }
                }

                if (this._ProgressItem.IsVisible || this._ToolTip.NeedDraw)
                {
                    e.CopyContentOfLayer(e.ValidLayer, 3);
                    Graphics graphics3 = e.GraphicsForLayer(3);
                    if (this._ProgressItem.IsVisible)
                        this._ProgressItem.Draw(graphics3);
                    if (this._ToolTip.NeedDraw)
                        this._ToolTip.Draw(graphics3);
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
            public DrawRequest(bool drawAllItems, bool drawFrameSelect, ToolTipItem toolTipItem, ProgressItem progressItem)
                : this()
            {
                this.DrawAllItems = drawAllItems;
                this.DrawFrameSelect = drawFrameSelect;
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
            /// Obsahuje true, pokud je požadavek na vykreslení FrameSelect obdélníku (kreslín se do vrstvy Dynamic)
            /// </summary>
            public bool DrawFrameSelect { get; private set; }
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
            /// <summary>
            /// Obsahuje true, pokud je požadavek na kreslení do vrstvy Standard.
            /// Tedy máme nějaké prvky v poli <see cref="StandardItems"/>.
            /// </summary>
            public bool NeedStdDraw { get { return (this.StandardItems.Count > 0); } }
            /// <summary>
            /// Obsahuje true, pokud je požadavek na kreslení do vrstvy Interactive.
            /// Tedy máme nějaké prvky v poli <see cref="InteractiveItems"/>.
            /// </summary>
            public bool NeedIntDraw { get { return (this.InteractiveItems.Count > 0); } }
            /// <summary>
            /// Obsahuje true, pokud je požadavek na kreslení do vrstvy Dynamic.
            /// Tedy máme nějaké prvky v poli <see cref="DynamicItems"/>, anebo je nastaveno <see cref="DrawFrameSelect"/> == true.
            /// </summary>
            public bool NeedDynDraw { get { return (this.DynamicItems.Count > 0 || this.DrawFrameSelect); } }
            /// <summary>
            /// true when need any draw (Standard, Interactive, Dynamic, ToolTip)
            /// </summary>
            public bool NeedAnyDraw { get { return (this.NeedStdDraw || this.NeedIntDraw || this.NeedDynDraw || this.DrawToolTip || this.DrawProgress); } }
            /// <summary>
            /// Do this objektu naplní prvky k vykreslení (prvky typu IInteractiveItem) z dodaného seznamu, 
            /// prvky zatřídí do soupisů dle vrstev (this.StandardItems, InteractiveItems, DynamicItems).
            /// Pokud daný prvek obsahuje nějaké Childs, pak rekurzivně vyvolá tutéž metodu i pro Childs tohoto prvku.
            /// </summary>
            /// <param name="clientSize"></param>
            /// <param name="parent"></param>
            /// <param name="items">Prvky k vykreslení</param>
            /// <param name="drawAllItems">true = vykreslit všechny prvky</param>
            /// <param name="interactive">true = provádí se interaktivní vykreslení</param>
            internal void Fill(Size clientSize, IInteractiveParent parent, IEnumerable<IInteractiveItem> items, bool drawAllItems, bool interactive)
            {
                // Tady se bude používat BoundsSpider !!!
                BoundsInfo boundsInfo = BoundsInfo.CreateForParent(clientSize);
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "DrawRequest", "Fill", ""))
                {
                    this.InteractiveMode = interactive;
                    this.FillFromItems(boundsInfo, parent, items, GInteractiveDrawLayer.None);
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
            /// <param name="boundsInfo">Souřadný systém, který platí pro prvky z parametru "items"</param>
            /// <param name="parent">Parent prvků</param>
            /// <param name="items">Prvky k vykreslení</param>
            /// <param name="parentLayers">Vrstvy k vykreslení</param>
            private void FillFromItems(BoundsInfo boundsInfo, IInteractiveParent parent, IEnumerable<IInteractiveItem> items, GInteractiveDrawLayer parentLayers)
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
                    boundsInfo.CurrentItem = item;

                    // Přidat prvek do seznamů pro patřičné vrstvy:
                    GInteractiveDrawLayer itemLayers = this.GetLayersToDrawItem(item, parentLayers);
                    this.AddItemToLayers(boundsInfo, item, itemLayers, false);
                    item.RepaintToLayers = GInteractiveDrawLayer.None;

                    // Pokud má prvek potomstvo, vyřešíme i to:
                    //  Child items budou zkontrolovány i tehdy, když jejich Parent prvek není kreslen, to je základní princip Layered Control!
                    IEnumerable<IInteractiveItem> childs = item.Childs;        // Získání Childs prvků voláme jen 1x pro jedno kreslení Draw
                    if (childs != null)
                    {
                        IInteractiveItem[] childItems = childs.ToArray();
                        if (childItems.Length > 0)
                        {
                            BoundsInfo boundsInfoChilds = boundsInfo.CurrentChildsSpider;          // Souřadný systém, který platí uvnitř aktuálního prvku "item" = ten platí pro všechny jeho Childs prvky
                            this.FillFromItems(boundsInfoChilds, item, childItems, itemLayers);    // Čistokrevná rekurze
                        }
                    }

                    if (item.NeedDrawOverChilds)
                        this.AddItemToLayers(boundsInfo, item, itemLayers, true);
                }
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
            /// <param name="boundsInfo">Souřadný systém</param>
            /// <param name="item">Prvek ke kreslení</param>
            /// <param name="addToLayers">Vrstvy, kam se má vykreslit</param>
            /// <param name="drawOverChilds">true = režim kreslení OverChilds</param>
            private void AddItemToLayers(BoundsInfo boundsInfo, IInteractiveItem item, GInteractiveDrawLayer addToLayers, bool drawOverChilds)
            {
                if (addToLayers.HasFlag(GInteractiveDrawLayer.Standard))
                    this.StandardItems.Add(new DrawRequestItem(boundsInfo, item, GInteractiveDrawLayer.Standard, drawOverChilds));
                if (addToLayers.HasFlag(GInteractiveDrawLayer.Interactive))
                    this.InteractiveItems.Add(new DrawRequestItem(boundsInfo, item, GInteractiveDrawLayer.Interactive, drawOverChilds));
                if (addToLayers.HasFlag(GInteractiveDrawLayer.Dynamic))
                    this.DynamicItems.Add(new DrawRequestItem(boundsInfo, item, GInteractiveDrawLayer.Dynamic, drawOverChilds));
            }
        }
        /// <summary>
        /// Data pro jedno vykreslení jednoho interaktivního prvku
        /// </summary>
        protected class DrawRequestItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="boundsInfo">Souřadný systém</param>
            /// <param name="item">Prvek ke kreslení</param>
            /// <param name="layer">Vrstva pro kreslení</param>
            /// <param name="isDrawOverChilds">true = režim kreslení OverChilds</param>
            public DrawRequestItem(BoundsInfo boundsInfo, IInteractiveItem item, GInteractiveDrawLayer layer, bool isDrawOverChilds)
            {
                this.Item = item;
                this.AbsoluteOrigin = boundsInfo.AbsOrigin;
                this.AbsoluteBounds = boundsInfo.CurrentAbsBounds;
                this.AbsoluteVisibleBounds = boundsInfo.CurrentAbsVisibleBounds;
                this.AbsoluteVisibleClip = boundsInfo.AbsVisibleBounds;
                this.Layer = layer;
                this.IsDrawOverChilds = isDrawOverChilds;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Item: " + this.Item.ToString() + "; AbsoluteBounds: " + this.AbsoluteBounds.ToString();
            }
            /// <summary>
            /// Prvek, který se bude vykreslovat
            /// </summary>
            public IInteractiveItem Item { get; private set; }
            /// <summary>
            /// Absolutní souřadnice počátku, k nimž se vztahují relativní souřadnice (Bounds) this itemu
            /// </summary>
            public Point AbsoluteOrigin { get; private set; }
            /// <summary>
            /// Absolutní souřadnice prvku <see cref="Item"/>, bez oříznutí
            /// </summary>
            public Rectangle AbsoluteBounds { get; private set; }
            /// <summary>
            /// Absolutní souřadnice prvku <see cref="Item"/>, oříznuté do viditelné oblasti
            /// </summary>
            public Rectangle AbsoluteVisibleBounds { get; private set; }
            /// <summary>
            /// Souřadnice prostoru (absolutní = v koordinátech Controlu), v nichž může být tento item viditelný = prostor pro Clip grafiky.
            /// Jde o Intersect hodnot AbsoluteClientBounds ze všech Parentů, tedy "viditelný průnik", do něhož se má tento prvek zobrazit.
            /// Zde není zohledněna hodnota Item.Bounds: prvek Item se může vykreslit i mimo svoje souřadnice, když chce, ale vždy jen v prostoru daném jeho Parenty.
            /// Tedy: Může se pohybovat ve svém parentu (i mimo svoje Bounds), ale nesmí vyběhnout z parenta.
            /// </summary>
            public Rectangle AbsoluteVisibleClip { get; private set; }
            /// <summary>
            /// Vykreslovaná vrstva
            /// </summary>
            public GInteractiveDrawLayer Layer { get; private set; }
            /// <summary>
            /// Obsahuje true, pokud tento požadavek je určen pro volání metody <see cref="IInteractiveItem.DrawOverChilds(GInteractiveDrawArgs)"/>.
            /// Pokud je false, má se volat standardní metoda <see cref="IInteractiveItem.Draw(GInteractiveDrawArgs, Rectangle, Rectangle)"/>.
            /// </summary>
            public bool IsDrawOverChilds { get; private set; }
            /// <summary>
            /// Zajistí vykreslení prvku a návazné operace s prvkem
            /// </summary>
            /// <param name="e"></param>
            public void Draw(GInteractiveDrawArgs e)
            {
                if (!this.IsDrawOverChilds)
                {   // Standardní kreslení:

                    // Prvek vykreslíme:
                    this.Item.Draw(e, this.AbsoluteBounds, this.AbsoluteVisibleBounds);

                    // V prvku resetujeme požadavek na kreslení do aktuální vrstvy:
                    e.ResetLayerFlagForItem(this.Item);
                }
                else
                {   // Kreslení OverChilds:
                    this.Item.DrawOverChilds(e, this.AbsoluteBounds, this.AbsoluteVisibleBounds);
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
            DrawRequest request = new DrawRequest(false, this._NeedDrawFrameBounds, this._ToolTip, null);
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
        GInteractiveControl IInteractiveParent.Host { get { return this; } }
        IInteractiveParent IInteractiveParent.Parent { get { return null; } set { } }
        GInteractiveStyles IInteractiveParent.Style { get { return GInteractiveStyles.None; } }
        Size IInteractiveParent.ClientSize { get { return this.ClientSize; } }
        void IInteractiveParent.Repaint() { this.Repaint(); }
        #endregion
    }
    #endregion
    #region class GActivePosition : Pracovní třída pro vyhledání prvku <see cref="IInteractiveItem"/> a seznamu jeho parentů
    /// <summary>
    /// GActivePosition : Pracovní třída pro vyhledání prvku <see cref="IInteractiveItem"/> a seznamu jeho parentů
    /// </summary>
    public class GActivePosition
    {
        #region Konstruktor, základní proměnné
        private GActivePosition(Point mouseAbsolutePoint)
        {
            this.Items = null;
            this.Count = 0;
            this.Item = null;
            this.HasItem = false;
            this.MouseAbsolutePoint = mouseAbsolutePoint;
            this.BeginTime = DateTime.Now;
            this.CurrentTime = this.BeginTime;
        }
        /// <summary>
        /// Soupis všech prvků, nalezených v této instanci.
        /// Na první pozici [0] je první prvek nejblíže <see cref="GInteractiveControl"/>,
        /// na poslední pozici je nejvyšší prvek.
        /// </summary>
        public GActiveItem[] Items { get; private set; }
        /// <summary>
        /// Počet prvků v poli <see cref="Items"/> (Count = 0, i kdyby pole bylo null)
        /// </summary>
        public int Count { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud je nějaký prvek nalezen.
        /// Pak je tento prvek dostupný v <see cref="ActiveItem"/>.
        /// </summary>
        public bool HasItem { get; protected set; }
        /// <summary>
        /// Nalezený prvek, kompletní údaje
        /// </summary>
        protected GActiveItem Item { get; private set; }
        /// <summary>
        /// Nalezený prvek <see cref="IInteractiveItem"/>
        /// </summary>
        public IInteractiveItem ActiveItem { get { return (this.HasItem ? this.Item.Item : null); } }
        /// <summary>
        /// Souřadnice prvku <see cref="Item"/> v absolutních koordinátech Controlu (nebo nulll, když <see cref="HasItem"/> je false).
        /// </summary>
        public Rectangle? ActiveItemAbsBounds { get { return (this.HasItem ? (Rectangle?)this.Item.ItemAbsBounds : (Rectangle?)null); } }
        /// <summary>
        /// Pozice myši v absolutních souřadnicích controlu <see cref="GInteractiveControl"/>
        /// </summary>
        public Point MouseAbsolutePoint { get; protected set; }
        /// <summary>
        /// Čas prvního eventu (když byla instance <see cref="GActivePosition"/> vytvořena)
        /// </summary>
        public DateTime BeginTime { get; protected set; }
        /// <summary>
        /// Čas právě probíhajícího eventu
        /// </summary>
        public DateTime CurrentTime { get; set; }
        #endregion
        #region Property from this.CurrentItem.Style, IsEnabled, IsVisible
        /// <summary>
        /// true pokud aktuální prvek má nastaveno (<see cref="IInteractiveItem.IsSelectParent"/> == true), 
        /// tzn. je to prvek, který umožňuje provádět "na sobě" výběr svých Childs pomocí MouseFrame selectování
        /// </summary>
        internal bool CanFrameSelect { get { return (this.HasItem && this.ActiveItem.IsSelectParent); } }
        /// <summary>
        /// Metoda najde v this objektu prvek, který podporuje Drag & Drop.
        /// Hledá počínaje od prvku <see cref="ActiveItem"/> (navrchu), a pokud ten nepodporuje Drag & Drop, tak hledá směrem k jeho Parentům.
        /// Pokud takový najde, pak vrací true.
        /// Pokud najde vhodný prvek na pozici "nižší" než je aktuální prvek <see cref="ActiveItem"/>, tak tento "aktivuje" (a poté vrátí true).
        /// Aktivace = od této chvíle bude aktivním prvkem ten, který podporuje Drag & Drop, a ne prvky "vyšší".
        /// </summary>
        /// <returns></returns>
        internal bool SearchForDraggableItem()
        {
            if (!this.HasItem) return false;
            int lastIndex = this.Count - 1;
            int foundIndex = -1;
            for (int i = lastIndex; i >= 0; i--)
            {
                if (this.Items[i].Item.IsEnabled && this.Items[i].HasStyle(GInteractiveStyles.Drag))
                {
                    foundIndex = i;
                    break;
                }
            }
            if (foundIndex < 0) return false;

            if (foundIndex < lastIndex)
            {   // Nalezený prvek není ten aktuální, je o něco "níže" pod aktuálním => musíme jej aktivovat:
                int count = foundIndex + 1;
                this.Item = this.Items[foundIndex];
                this.Items = this.Items.Take(count).ToArray();
                this.Count = count;
                this.HasItem = true;
            }
            return true;
        }
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
        public bool IsEnabled { get { return (this.HasItem && this.ActiveItem.IsEnabled); } }
        /// <summary>
        /// Current item is not null and IsVisible?
        /// </summary>
        public bool IsVisible { get { return (this.HasItem && this.ActiveItem.IsVisible); } }
        private bool _HasStyle(GInteractiveStyles style)
        {
            return (this.HasItem && ((this.ActiveItem.Style & style) != 0));
        }
        #endregion
        #region Základní metody: FindItemAtPoint(), MapExchange()
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point.
        /// Prefered current item above other items.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <returns></returns>
        public static GActivePosition FindItemAtPoint(Size hostSize, List<IInteractiveItem> items, GActivePosition prevItem, Point mouseAbsolutePoint, bool withDisabled, params IInteractiveItem[] priorityItems)
        {
            return _FindItemAtPoint(hostSize, items, prevItem, mouseAbsolutePoint, withDisabled, priorityItems);
        }
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point
        /// Prefered current item above other items.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <returns></returns>
        private static GActivePosition _FindItemAtPoint(Size hostSize, List<IInteractiveItem> itemList, GActivePosition prevItem, Point mouseAbsolutePoint, bool withDisabled, IInteractiveItem[] priorityItems)
        {
            GActivePosition currItem = new GActivePosition(mouseAbsolutePoint);
            IInteractiveItem[] items = _CreateJoinItems(itemList, priorityItems);
            GActiveItem[] holdItems = ((prevItem != null && prevItem.HasItem) ? prevItem.Items : null);
            currItem._FindItemAtPoint(hostSize, items, mouseAbsolutePoint, withDisabled, holdItems);
            return currItem;
        }
        /// <summary>
        /// Vrátí seznam položek první úrovně k scanování
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="priorityItems"></param>
        /// <returns></returns>
        private static IInteractiveItem[] _CreateJoinItems(List<IInteractiveItem> itemList, IInteractiveItem[] priorityItems)
        {
            List<IInteractiveItem> joinList = new List<IInteractiveItem>();
            if (itemList != null && itemList.Count > 0)
                joinList.AddRange(itemList);
            if (priorityItems != null && priorityItems.Length > 0)
                joinList.AddRange(priorityItems);
            return itemList.ToArray();
        }
        /// <summary>
        /// Hledá prvek, který je aktivní na dané souřadnici v daném seznamu prvků.
        /// Prvek musí být viditelný, musí být (enabled nebo se hledají i non-enabled prvky), musí být aktivní na dané souřadnici.
        /// Metoda prohledává stejným způsobem i Child prvky nalezeného prvku, pokud existují.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="holdList"></param>
        /// <returns></returns>
        private void _FindItemAtPoint(Size hostSize, IInteractiveItem[] items, Point mouseAbsolutePoint, bool withDisabled, GActiveItem[] holdItems)
        {
            List<GActiveItem> foundList = new List<GActiveItem>();
            Dictionary<uint, IInteractiveItem> scanDict = new Dictionary<uint, IInteractiveItem>();
            Queue<GActiveItem> holdQueue = (holdItems == null ? null : new Queue<GActiveItem>(holdItems));   // Fronta "přidržených" prvků (od posledně)
            BoundsInfo boundsInfo = BoundsInfo.CreateForParent(hostSize);
            bool run = true;
            while (run)
            {
                run = false;
                bool isFound = false;
                IInteractiveItem foundItem = null;
                if (holdQueue != null && holdQueue.Count > 0)
                {   // Máme hledat nejprve v "přidržených" prvcích?
                    GActiveItem fip = holdQueue.Dequeue();
                    if (fip.Item.HoldMouse)
                    {
                        isFound = _TryFindItemInList(items, boundsInfo, mouseAbsolutePoint, withDisabled, fip.Item, out foundItem);
                        if (!isFound)
                            holdQueue.Clear();
                    }
                }
                if (!isFound)
                {   // Hledáme mimo "přidržené" prvky, pouze podle souřadnice myši:
                    isFound = _TryFindItemInList(items, boundsInfo, mouseAbsolutePoint, withDisabled, null, out foundItem);
                    holdQueue = null;                      // Možná bychom měli zapomenout na prvky, které se měly "přidržet"?
                }
               
                if (isFound)
                {   // Našli jsme prvek:
                    // Pokud jsme se zacyklili => skončíme chybou:
                    if (_IsCycled(foundItem, scanDict, foundList))
                        throw new GraphLibCodeException("Při hledání vnořených vizuálních prvků v GActiveItem došlo k zacyklení položek v seznamech IInteractiveItem.");

                    // Informace o nalezeném prvku:
                    boundsInfo.CurrentItem = foundItem;
                    foundList.Add(new GActiveItem(this, foundItem, boundsInfo));

                    // Pokud prvek má Childs, projdeme je rovněž, hned v další smyčce:
                    // Nemusíme řešit posun souřadnic myši, jedeme stále v souřadnicích absolutních:
                    IEnumerable<IInteractiveItem> childs = foundItem.Childs;
                    if (childs != null)
                    {
                        items = childs.ToArray();
                        if (items.Length > 0)
                        {
                            run = true;
                            boundsInfo = boundsInfo.CurrentChildsSpider;
                        }
                    }
                }
            }

            if (foundList != null)
            {
                int count = foundList.Count;
                if (count > 0)
                {
                    this.Items = foundList.ToArray();
                    this.Count = count;
                    this.Item = foundList[count - 1];
                    this.HasItem = true;
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek (foundItem) najde v scanDict podle kladného Id, anebo jej najde podle <see cref="Object.ReferenceEquals(object, object)"/> v foundList.
        /// Pokud má prvek kladné Id a v scanDict, pak jej do scanDict přidá a vrací false (pak neřeší hledání v foundList).
        /// </summary>
        /// <param name="foundItem"></param>
        /// <param name="scanDict"></param>
        /// <param name="foundList"></param>
        /// <returns></returns>
        private bool _IsCycled(IInteractiveItem foundItem, Dictionary<uint, IInteractiveItem> scanDict, List<GActiveItem> foundList)
        {
            if (foundItem.Id > 0)
            {
                if (scanDict.ContainsKey(foundItem.Id)) return true;
                scanDict.Add(foundItem.Id, foundItem);
                return false;
            }
            return foundList.Any(i => Object.ReferenceEquals(i.Item, foundItem));
        }
        /// <summary>
        /// Hledá v daném poli od konce prvek, který je aktivní na dané absolutní souřadnici.
        /// Pokud je zadán preferovaný prvek (preferredItem), kontroluje pouze tento prvek (pokud existuje, a vyhoví podmínkám).
        /// prvek musí být <see cref="IInteractiveItem.IsVisible"/>, musí být <see cref="IInteractiveItem.IsEnabled"/> nebo musí být povoleno vyhledání i Disabled prvků,
        /// a prvek musí být interaktivní na dané souřadnici.
        /// Výstup: true pokud je nalezeno (pak je nalezený prvek uložen v out parametru foundItem).
        /// </summary>
        /// <param name="items"></param>
        /// <param name="boundsInfo"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="preferredItem"></param>
        /// <param name="foundItem"></param>
        /// <returns></returns>
        private static bool _TryFindItemInList(IInteractiveItem[] items, BoundsInfo boundsInfo, Point mouseAbsolutePoint, bool withDisabled, IInteractiveItem preferredItem, out IInteractiveItem foundItem)
        {
            Point mouseRelativePoint = mouseAbsolutePoint.Sub(boundsInfo.AbsOrigin);

            for (int idx = items.Length - 1; idx >= 0; idx--)
            {   // Hledáme v poli prvků od konce = vizuálně od nejvýše vykresleného prvku:
                IInteractiveItem item = items[idx];
                if ((preferredItem == null || Object.ReferenceEquals(preferredItem, item))
                    && item.IsVisible
                    && (withDisabled || item.IsEnabled)
                    && item.IsActiveAtPoint(mouseRelativePoint))
                {   // Daný prvek vyhovuje => máme hotovo:
                    foundItem = item;
                    return true;
                }
            }
            foundItem = null;
            return false;
        }
        /// <summary>
        /// Metoda vytvoří dvě přechodová pole při změně aktivního prvku z "prev" (pole "leaveList") do prvku "next" (pole "enterList").
        /// Volající aplikace pak má postupně zavolat metody Leave na prvcích z pole "leaveList", a metody Enter na prvcích z pole "enterList".
        /// Tato metoda projde hierarchii prvků <see cref="IInteractiveItem"/> v objektu "prev" i "next" a určí, 
        /// do kterého bodu jsou hierarchie společné (ty do výstupních polí nedává), a do výstupních polí vloží prvky, které jsou rozdílné.
        /// Výstupní pole "leaveList" obsahuje prvky od posledního (v seznamu prev.Items) směrem k prvnímu, který je odlišný od prvků v poli next.Items.
        /// Výstupní pole "enterList" obsahuje prvky od prvního odlišného (v seznamu next.Items) směrem k poslednímu.
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <param name="leaveList"></param>
        /// <param name="enterList"></param>
        public static void MapExchange(GActivePosition prev, GActivePosition next, out List<IInteractiveItem> leaveList, out List<IInteractiveItem> enterList)
        {
            leaveList = new List<IInteractiveItem>();
            enterList = new List<IInteractiveItem>();

            if (prev == null && next == null) return;

            // Najde nejvyšší index obou polí (prev.FoundItemList and next.FoundItemList), který obsahuje identický objekt:
            int prevCnt = (prev == null ? 0 : prev.Count);
            int nextCnt = (next == null ? 0 : next.Count);
            int commonCnt = (prevCnt < nextCnt ? prevCnt : nextCnt); // Počet prvků obou polí, kde lze hledat identické prvky
            int commonObjectIdx = -1;
            for (int i = 0; i < commonCnt; i++)
            {
                if (Object.ReferenceEquals(prev.Items[i].Item, next.Items[i].Item))
                    commonObjectIdx = i;         // Na indexu [i] je shodný prvek v obou polích: budeme hledat na dalším indexu...
                else
                    break;                       // Na indexu [i] už jsou odlišné prvky: končíme hledání
            }

            int prevLastIdx = prevCnt - 1;       // Poslední index v poli "prev"
            int nextLastIdx = nextCnt - 1;       // Poslední index v poli "next"

            // Pokud index posledního společného prvku je roven poslednímu indexu v obou polích, pak oba prvky ("prev" i "next") obsahují identickou sekvenci:
            if (commonObjectIdx >= 0 && prevLastIdx == commonObjectIdx && nextLastIdx == commonObjectIdx)
                return;                          // Oba výstupní seznamy (leaveList and enterList) zůstanou prázdné, protože nedošlo k žádné změně z "prev" do "next" pole

            int firstDiffIdx = commonObjectIdx + 1;   // Index prvního prvku, který je odlišný
            // Událost MouseLeave se volá od posledního prvku v poli "prev" až po prvek na indexu "firstDiffIdx":
            for (int i = prevLastIdx; i >= firstDiffIdx; i--)
                leaveList.Add(prev.Items[i].Item);

            // Událost MouseEnter se volá od prvku na indexu "firstDiffIdx" až do posledního prvku v poli "next":
            for (int i = firstDiffIdx; i <= nextLastIdx; i++)
                enterList.Add(next.Items[i].Item);
        }
        /// <summary>
        /// Vrátí relativní bod (v koordinátech <see cref="ActiveItemAbsBounds"/>) pro daný absolutní bod
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public Point? GetRelativePointToCurrentItem(Point absolutePoint)
        {
            return (this.HasItem ? (Point?)absolutePoint.Sub(this.ActiveItemAbsBounds.Value.Location) : (Point?)null);
        }
        #endregion
        #region Vyhledání sady prvků
        /// <summary>
        /// Metoda najde a vrátí prvky, které se nacházejí na daných souřadnicích, a které vyhovují daným filtrům.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="frameBounds"></param>
        /// <param name="filterScan"></param>
        /// <param name="filterAccept"></param>
        /// <returns></returns>
        public static Tuple<IInteractiveItem, Rectangle>[] FindItemsAtBounds(Size hostSize, List<IInteractiveItem> items, Rectangle frameBounds, Func<IInteractiveItem, Rectangle, bool> filterScan, Func<IInteractiveItem, Rectangle, bool> filterAccept)
        {
            BoundsInfo boundsInfo = BoundsInfo.CreateForParent(hostSize);

            bool hasFilterScan = (filterScan != null);
            bool hasFilterAccept = (filterAccept != null);

            List<Tuple<IInteractiveItem, Rectangle>> resultList = new List<Tuple<IInteractiveItem, Rectangle>>();
            Dictionary<uint, IInteractiveItem> scanDict = new Dictionary<uint, IInteractiveItem>();
            Queue<Tuple<BoundsInfo, IEnumerable<IInteractiveItem>>> scanQueue = new Queue<Tuple<BoundsInfo, IEnumerable<IInteractiveItem>>>();
            scanQueue.Enqueue(new Tuple<BoundsInfo, IEnumerable<IInteractiveItem>>(boundsInfo, items));
            while (scanQueue.Count > 0)
            {
                Tuple<BoundsInfo, IEnumerable<IInteractiveItem>> workItem = scanQueue.Dequeue();
                BoundsInfo currentBoundsInfo = workItem.Item1;
                IEnumerable<IInteractiveItem> currentItemList = workItem.Item2;
                if (currentItemList == null) continue;

                foreach (IInteractiveItem currentItem in currentItemList)
                {
                    if (scanDict.ContainsKey(currentItem.Id)) continue;
                    scanDict.Add(currentItem.Id, currentItem);

                    currentBoundsInfo.CurrentItem = currentItem;
                    Rectangle currentItemBounds = currentBoundsInfo.CurrentAbsBounds;
                    if (!frameBounds.IntersectsWith(currentItemBounds)) continue;

                    if ((!hasFilterScan || (hasFilterScan && filterScan(currentItem, currentItemBounds))))
                    {
                        IEnumerable<IInteractiveItem> currentChilds = currentItem.Childs;
                        if (currentChilds != null)
                            scanQueue.Enqueue(new Tuple<BoundsInfo, IEnumerable<IInteractiveItem>>(currentBoundsInfo.CurrentChildsSpider, currentChilds));
                    }

                    if (!hasFilterAccept || (hasFilterAccept && filterAccept(currentItem, currentItemBounds)))
                        resultList.Add(new Tuple<IInteractiveItem, Rectangle>(currentItem, currentItemBounds));
                }
            }
            return resultList.ToArray();
        }
        #endregion
        #region class GActiveItem : Informace o jednom nalezeném prvku a jeho souřadném systému
        /// <summary>
        /// GActiveItem : Informace o jednom nalezeném prvku a jeho souřadném systému
        /// </summary>
        public class GActiveItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="boundsInfo"></param>
            public GActiveItem(IInteractiveItem item, BoundsInfo boundsInfo)
            {
                this.Owner = null;
                this.Item = item;
                this.BoundsInfo = boundsInfo;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="item"></param>
            /// <param name="boundsInfo"></param>
            public GActiveItem(GActivePosition owner, IInteractiveItem item, BoundsInfo boundsInfo)
            {
                this.Owner = owner;
                this.Item = item;
                this.BoundsInfo = boundsInfo;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Item.ToString();
            }
            /// <summary>
            /// Vlastník, ten má uloženy další hodnoty
            /// </summary>
            protected GActivePosition Owner { get; private set; }
            /// <summary>
            /// Nalezený prvek
            /// </summary>
            public IInteractiveItem Item { get; private set; }
            /// <summary>
            /// Souřadný systém nalezeného prvku <see cref="Item"/>
            /// </summary>
            public BoundsInfo BoundsInfo { get; private set; }
            /// <summary>
            /// Souřadnice prvku <see cref="Item"/> v absolutních koordinátech Controlu
            /// </summary>
            public Rectangle ItemAbsBounds { get { return this.BoundsInfo.CurrentAbsBounds; } }
            /// <summary>
            /// Vrací true, pokud zdejší prvek má nastaven daný styl
            /// </summary>
            /// <param name="style"></param>
            /// <returns></returns>
            public bool HasStyle(GInteractiveStyles style)
            {
                return ((this.Item.Style & style) != 0);
            }
        }
        #endregion
        #region Static services
        /// <summary>
        /// return true, when items lastItem and currItem are equal, can doubleclick, 
        /// and time between events is in limit to doubleclick.
        /// </summary>
        /// <param name="lastItem"></param>
        /// <param name="currItem"></param>
        /// <returns></returns>
        public static bool IsDoubleClick(GActivePosition lastItem, GActivePosition currItem)
        {
            if (lastItem == null || currItem == null) return false;
            if (!lastItem.HasItem || !currItem.HasItem) return false;
            if (!currItem.CanDoubleClick) return false;
            if (!Object.ReferenceEquals(lastItem.ActiveItem, currItem.ActiveItem)) return false;
            TimeSpan time = currItem.CurrentTime.Subtract(lastItem.BeginTime);
            return IsDoubleClick(lastItem.MouseAbsolutePoint, currItem.MouseAbsolutePoint, time);
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
        public static bool IsLongClick(DateTime? downTime, GActivePosition currItem)
        {
            DateTime now = DateTime.Now;
            if (!downTime.HasValue || currItem == null || !currItem.HasItem) return false;
            if (!currItem.CanLongClick) return false;
            TimeSpan time = currItem.CurrentTime.Subtract(downTime.Value);
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
        /// Vyvolá metodu <see cref="IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro aktivní prvek <see cref="ActiveItem"/>.
        /// Pokud je zadáno "recurseToSolver" = true, pak po proběhnutí metody testuje hodnotu 
        /// </summary>
        /// <param name="stateArgs"></param>
        /// <param name="recurseToSolver"></param>
        internal void CallAfterStateChanged(GInteractiveChangeStateArgs stateArgs, bool recurseToSolver)
        {
            int i = this.Count - 1;
            while (i >= 0)
            {   // Akci budu (možná) volat pro všechny prvky, dokud:
                //  a) není požadováno recurseToSolver
                //  b) je nastaveno, že prvek akci vyřešil:
                this.Items[i].Item.AfterStateChanged(stateArgs);
                if (!recurseToSolver || stateArgs.ActionIsSolved)
                    break;
                i--;
            }
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
