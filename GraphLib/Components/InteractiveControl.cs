using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GInteractiveControl : Jediný používaný interaktivní WinForm control, který se používá pro zobrazení interaktivních dat
    /// <summary>
    /// GInteractiveControl : Jediný používaný interaktivní WinForm control, který se používá pro zobrazení interaktivních dat
    /// </summary>
    public partial class GInteractiveControl : GControlLayered, IInteractiveParent
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GInteractiveControl()
        {
            this.Init();
        }
        /// <summary>
        /// Akce Resize
        /// </summary>
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
            this.SetStyle(ControlStyles.Selectable | ControlStyles.UserMouse | ControlStyles.ResizeRedraw, true);
            this._StopWatchInit();
            this._ToolTipInit();
            this._ProgressInit();
            this._DrawInit();
            this._KeyboardEventsInit();
            this._MouseEventsInit();
            this._DrawSupportInit();
            this._AnimatorInit();
            this._BackThreadInit();
        }
        /// <summary>
        /// Po dokončení Dispose
        /// </summary>
        protected override void OnAfterDisposed()
        {
            base.OnAfterDisposed();
            this._BackThreadDone();
        }
        /// <summary>
        /// Souhrn prvků umístěných přímo na Controlu
        /// </summary>
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
        #region Items, přidávání controlů
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
        /// Přidá jeden interaktivní control do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="GControlLayered.Draw()"/>
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(IInteractiveItem item)
        {
            this._AddItem(item);
        }
        /// <summary>
        /// Přidá dané interaktivní controly do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="GControlLayered.Draw()"/>
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(params IInteractiveItem[] items)
        {
            foreach (IInteractiveItem item in items)
                this._AddItem(item);
        }
        /// <summary>
        /// Přidá dané interaktivní controly do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="GControlLayered.Draw()"/>
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(IEnumerable<IInteractiveItem> items)
        {
            if (items != null)
            {
                foreach (IInteractiveItem item in items)
                    this._AddItem(item);
            }
        }
        /// <summary>
        /// Přidá dané interaktivní controly do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="GControlLayered.Draw()"/>
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
        /// <summary>
        /// Háček volaný při změně stavu, pro potomky
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnInteractiveStateChanged(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Event after change of interactive state
        /// </summary>
        public event GInteractiveChangeStateHandler InteractiveStateChanged;
        #endregion
        #region Focus (Enter, GotFocus, LostFocus, Leave) a Keyboard (PreviewKeyDown, KeyDown, KeyUp, KeyPress)
        private void _KeyboardEventsInit()
        {
            this._KeyboardCurrentItem = null;
        }
        #region Obsluha override metod (z WinForm.Control) pro Focus a Keyboard
        /// <summary>
        /// Focus vstupuje do controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusEnter, () => this._OnEnter(e), () => base.OnEnter(e));
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
        /// <summary>
        /// Focus vstupuje do controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusEnter, () => this._OnGotFocus(e), () => base.OnGotFocus(e));
        }
        private void _OnGotFocus(EventArgs e)
        { }
        /// <summary>
        /// Focus odchází z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusLeave, () => this._OnLostFocus(e), () => base.OnLostFocus(e));
        }
        private void _OnLostFocus(EventArgs e)
        { }
        /// <summary>
        /// Focus odchází z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusLeave, () => this._OnLeave(e), () => base.OnLeave(e));
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
        /// <summary>
        /// Akce PreviewKeyDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardPreviewKeyDown, () => this._OnPreviewKeyDown(e), () => base.OnPreviewKeyDown(e));
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
        /// <summary>
        /// Akce KeyDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyDown, () => this._OnKeyDown(e), () => base.OnKeyDown(e));
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
        /// <summary>
        /// Akce KeyUp
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyUp, () => this._OnKeyUp(e), () => base.OnKeyUp(e));
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
        /// <summary>
        /// Akce KeyPress
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyPress, () => this._OnKeyPress(e), () => base.OnKeyPress(e));
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
        /// <param name="forceLeave"></param>
        private void _ItemKeyboardExchange(IInteractiveItem itemPrev, IInteractiveItem itemNext, bool forceLeave)
        {
            // We solve Keyboard focus change.
            // This change is other than MouseEnter and MouseLeave action, which is "recursively from parent to child".
            // Keyboard focus changes are one level, and focus can be changed only from and to item, which accept keyboard actions (CanKeyboard).

            // itemPrev is always item with keyboard activity (or is null).
            // itemNext can be an item with mouse activity, but no keyboard activity. We must search for nearest item with keyboard activity in item and its Parent:
            itemNext = _ItemKeyboardSearchKeyboardInput(itemNext);

            // Keyboard focus change is simpliest:
            bool existsPrev = (itemPrev != null && itemPrev.Is.KeyboardInput);
            bool existsNext = (itemNext != null && itemNext.Is.KeyboardInput);
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
        /// Vrátí prvek (daný, nebo jeho parenta), jehož <see cref="InteractiveProperties.KeyboardInput"/> je true.
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
                if (item != null && item.Is.KeyboardInput)
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
        /// Zavolá obsluhu item.AfterStateChanged() s patřičnými daty, pocházejícími z klávesnicové události.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="change"></param>
        /// <param name="previewArgs"></param>
        /// <param name="keyArgs"></param>
        /// <param name="keyPressArgs"></param>
        private void _ItemKeyboardCallEvent(IInteractiveItem item, GInteractiveChangeState change, PreviewKeyDownEventArgs previewArgs, KeyEventArgs keyArgs, KeyPressEventArgs keyPressArgs)
        {
            if (item.Is.KeyboardInput)
            {
                GInteractiveChangeState realChange = change;
                GInteractiveState targetState = _GetStateAfterChange(realChange, item.Is.Enabled);
                BoundsInfo boundsInfo = BoundsInfo.CreateForChild(item);
                GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(boundsInfo, realChange, targetState, this.FindNewItemAtPoint, previewArgs, keyArgs, keyPressArgs);
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
            this._CurrentActiveItem = null;
            this._MouseDragMoveItem = null;
            this._DragStartSize = SystemInformation.DragSize;
        }
        #region Obsluha override metod (z WinForm.Control) pro myš
        /// <summary>
        /// Akce MouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.MouseEnter, () => this._OnMouseEnter(e), () => base.OnMouseEnter(e));
        }
        /// <summary>
        /// Akce MouseMove
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.MouseOver, () => this._OnMouseMove(e), () => base.OnMouseMove(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.LeftDown, () => this._OnMouseDown(e), () => base.OnMouseDown(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseUp
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.LeftUp, () => this._OnMouseUp(e), () => base.OnMouseUp(e), () => { this._MouseDownReset(); this._InteractiveDrawRun(); });
        }
        /// <summary>
        /// Akce MouseWheel
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.WheelUp, () => this._OnMouseWheel(e), () => base.OnMouseWheel(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            this.InteractiveAction(GInteractiveChangeState.MouseLeave, () => this._OnMouseLeave(e), () => base.OnMouseLeave(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Zajistí provedení téže akce, jako by myš vstoupila na control: Enter + Move, podle aktuálního stavu myši
        /// </summary>
        private void _OnMouseEnterMove()
        {
            // Do této metody vstupuje řízení za různého stavu controlu, může být v podstatě i po skončení života controlu.
            // V takové situaci ale nechceme nic dělat:
            if (this.Disposing || this.IsDisposed) return;

            this._OnMouseEnter(new EventArgs());

            MouseButtons mouseButtons = Control.MouseButtons;        // Stisknutá tlačítka myši
            Point mousePoint = Control.MousePosition;                // Souřadnice myši v koordinátech Screenu
            mousePoint = this.PointToClient(mousePoint);             //  -""- v koordinátech Controlu
            MouseEventArgs mouseEventArgs = new MouseEventArgs(mouseButtons, 0, mousePoint.X, mousePoint.Y, 0);
            this._OnMouseMove(mouseEventArgs);
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
        private void _OnMouseMove(MouseEventArgs e)
        {
            using (ITraceScope scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseMove", ""))
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
            GActivePosition mouseCurrentItem = this._CurrentActiveItem;

            MouseMoveDragState dragMode = mouseCurrentItem.MouseDragStartDetect();
            switch (dragMode)
            {
                case MouseMoveDragState.DragMove:
                    // Našli jsme nějaký prvek, který je ochotný s sebou nechat vláčet (v jeho aktuálním stavu podporuje Drag and Drop):
                    // Myš se právě nyní pohnula z "Silent zone" (oblast okolo místa, kde byla myš zmáčknuta) => Drag and Drop začíná:
                    this._MouseDragState = MouseMoveDragState.DragMove;
                    this._MouseDragMoveBegin(e);
                    this._MouseDragMoveStep(e);
                    scope.Result = "MouseDragMoveBegin";
                    break;

                case MouseMoveDragState.DragFrame:
                    // Nalezený prvek podporuje Select pomocí Frame (tzn. má vlastnost <see cref="IInteractiveItem.IsSelectParent"/>)
                    this._MouseDragState = MouseMoveDragState.DragFrame;
                    this._MouseDragFrameBegin(e);
                    this._MouseDragFrameStep(e);
                    scope.Result = "MouseDragFrameBegin";
                    break;

                default:
                    // Neexistuje prvek vhodný pro Drag and Drop => nastavíme stav Cancel:
                    this._MouseDragState = MouseMoveDragState.None;
                    this._MouseDragMoveItemOffset = null;
                    this._CurrentMouseDragCanceled = true;
                    break;

            }
        }
        /// <summary>
        /// Metoda určí, v jakém stavu je nyní proces Drag na základě aktuálního pohybu myši.
        /// Reaguje na hodnoty: <see cref="_CurrentMouseDragCanceled"/>, <see cref="_MouseDragStartBounds"/> a na pozici myši v parametru mousePoint.
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        private MouseMoveDragState _GetMouseMoveDragState(Point mousePoint)
        {
            // Pokud byl dán Cancel, pak bez ohledu na další vracím None:
            if (this._CurrentMouseDragCanceled) return MouseMoveDragState.None;          // Proces Drag and Drop probíhal, ale byl stornován (klávesou Escape)

            // Pokud control již rozhodl o stavu, pak jej vrátíme bez dalšího zkoumání:
            MouseMoveDragState state = this._MouseDragState;
            if (state == MouseMoveDragState.DragMove || state == MouseMoveDragState.DragFrame) return state;

            // Dosud nebylo o stavu rozhodnuto, detekujeme pozici myši vzhledem k výchozímu bodu:
            Rectangle? startBounds = this._MouseDragStartBounds;
            if (startBounds.HasValue && startBounds.Value.Contains(mousePoint))          // Pozice myši je stále uvnitř Silent zone => čekáme na nějaký větší pohyb
                return MouseMoveDragState.Wait;

            return MouseMoveDragState.Start;
        }
        private void _OnMouseDown(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseDown", ""))
            {
                this._InteractiveDrawInit(e);
                this._MouseFell(e);
            }
        }
        private void _OnMouseUp(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseUp", ""))
            {
                this._InteractiveDrawInit(e);
                if (this._CurrentMouseDragCanceled) { }
                else if (this._MouseDragState == MouseMoveDragState.DragMove) this._MouseDragMoveDone(e);
                else if (this._MouseDragState == MouseMoveDragState.DragFrame) this._MouseDragFrameDone(e);
                else this._MouseRaise(e);
            }
        }
        private void _OnMouseWheel(MouseEventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseWheel", ""))
            {
                this._InteractiveDrawInit(e);
                this._MouseOneWheel(e);
                this._OnMouseMove(e);            // Poté, co se provedla akce MouseWheel nasimulujeme ještě akci MouseMove, protože se mohl pohnout obraz controlu
            }
        }
        /// <summary>
        /// Zajistí provedení téže akce, jako by myš opustila control
        /// </summary>
        private void _OnMouseLeave()
        {
            EventArgs e = new EventArgs();
            this.InteractiveAction(GInteractiveChangeState.MouseLeave, () => this._OnMouseLeave(e), null, () => this._InteractiveDrawRun());
        }
        private void _OnMouseLeave(EventArgs e)
        {
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "MouseLeave", ""))
            {
                this._InteractiveDrawInit(null);
                this._MouseOver(null);
                this._MouseAllReset();
            }
        }
        #endregion
        #region Řízení konkrétních aktivit myši, již rozčleněné; volání jednoduchých interaktivních metod (Over, Fell, Raise, Whell)
        /// <summary>
        /// Tato akce se volá výhradně když se myš pohybuje bez Drag and Drop = bez stisknutého tlačítka
        /// </summary>
        /// <param name="e"></param>
        private void _MouseOver(MouseEventArgs e)
        {
            GActivePosition oldActiveItem = this._CurrentActiveItem;
            if (e != null)
            {   // Standarndí pohyb myši nad Controlem:
                GActivePosition newActiveItem = this.FindActivePositionAtPoint(e.Location, true);
                this._ItemMouseExchange(oldActiveItem, newActiveItem, this._MouseCurrentRelativePoint);
                this._ToolTipMouseMove(this._MouseCurrentAbsolutePoint);
            }
            else
            {   // MouseLeave z tohoto Controlu:
                this._MouseCurrentRelativePoint = null;
                this._ItemMouseExchange(oldActiveItem, null, null);
                this._ActivateCursor(SysCursorType.Default);
            }
        }
        private void _MouseFell(MouseEventArgs e)
        {
            MouseButtons mouseButtons = e.Button;
            bool mouseButtonsLeft = (mouseButtons == MouseButtons.Left);

            GActivePosition oldActiveItem = this._CurrentActiveItem;
            GActivePosition newActiveItem = this.FindActivePositionAtPoint(e.Location, false);
            newActiveItem.CurrentStateFill(e.Location);
            this._ItemMouseExchange(oldActiveItem, newActiveItem, this._MouseCurrentRelativePoint);
            this._ItemKeyboardExchange(this._KeyboardCurrentItem, newActiveItem.ActiveItem, false);

            this._MouseDownAbsolutePoint = e.Location;
            this._MouseDownTime = DateTime.Now;
            this._MouseDownButtons = e.Button;
            this._MouseDragStartBounds = e.Location.CreateRectangleFromCenter(this._DragStartSize);
            this._MouseDragMoveItemOffset = null;
            this._ItemMouseCallStateChangedEvent(newActiveItem, GInteractiveChangeState.LeftDown, newActiveItem.CurrentMouseRelativePoint);

            if (mouseButtonsLeft && newActiveItem.ItemIsSelectable)
                this._ItemMouseLeftDownUnSelect(newActiveItem);
        }
        /// <summary>
        /// Voláno při zvednutí tlačítka myši (=konec kliknutí), v situaci kdy NEPROBÍHALA žádná akce "Drag and Drop", ani "Drag and Frame". 
        /// Jde o prosté kliknutí na místě.
        /// Zde se řeší: DoubleClick, LongClick, LeftClickSelect, Click, Kontextové menu.
        /// </summary>
        /// <param name="e"></param>
        private void _MouseRaise(MouseEventArgs e)
        {
            GActivePosition oldActiveItem = this._CurrentActiveItem;
            MouseButtons mouseButtons = (this._MouseDownButtons.HasValue ? this._MouseDownButtons.Value : MouseButtons.None);
            bool mouseButtonsLeft = (mouseButtons == MouseButtons.Left);
            bool mouseButtonsRight = (mouseButtons == MouseButtons.Right);
            Keys modifierKeys = Control.ModifierKeys;
            if (oldActiveItem != null)
            {
                oldActiveItem.CurrentStateFill(e.Location);

                this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftUp, oldActiveItem.CurrentMouseRelativePoint);
                if (oldActiveItem.CanDoubleClick && GActivePosition.IsDoubleClick(this._MouseClickedItem, oldActiveItem))
                {   // Double click:
                    this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftDoubleClick, oldActiveItem.CurrentMouseRelativePoint);
                }
                else if (oldActiveItem.CanLongClick && GActivePosition.IsLongClick(this._MouseDownTime, oldActiveItem))
                {   // Long click:
                    this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftLongClick, oldActiveItem.CurrentMouseRelativePoint);
                    if (mouseButtonsLeft || mouseButtonsRight)
                        this._ItemMouseCallContextMenu(oldActiveItem, GInteractiveChangeState.LeftLongClick, oldActiveItem.CurrentMouseRelativePoint);
                }
                else if (oldActiveItem.CanClick)
                {   // Single click:
                    if (mouseButtonsLeft && oldActiveItem.ItemIsSelectable)
                        this._ItemMouseLeftClickSelect(oldActiveItem, modifierKeys);
                    this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftClick, oldActiveItem.CurrentMouseRelativePoint);
                    if (mouseButtonsRight)
                        this._ItemMouseCallContextMenu(oldActiveItem, GInteractiveChangeState.LeftClick, oldActiveItem.CurrentMouseRelativePoint);
                }
                this._MouseClickedItem = oldActiveItem;
            }
        }
        /// <summary>
        /// Kolečko myši
        /// </summary>
        /// <param name="e"></param>
        private void _MouseOneWheel(MouseEventArgs e)
        {
            GActivePosition oldActiveItem = this._CurrentActiveItem;
            GActivePosition newActiveItem = this.FindActivePositionAtPoint(e.Location, false);
            newActiveItem.CurrentStateFill(e.Location);
            this._ItemMouseExchange(oldActiveItem, newActiveItem, this._MouseCurrentRelativePoint);
            GInteractiveChangeState change = (e.Delta > 0 ? GInteractiveChangeState.WheelUp : GInteractiveChangeState.WheelDown);
            this._ItemMouseCallStateChangedEvent(newActiveItem, change, newActiveItem.CurrentMouseRelativePoint, recurseToSolver: true);   // recurseToSolver = true => opakovat volání akce, dokud ji některý prvek v hierarchii nevyřeší.
        }
        #endregion
        #region Řízení procesu MouseDragMove = přesouvání prvku Drag and Drop
        private void _MouseDragMoveBegin(MouseEventArgs e)
        {
            // Relativní pozice myši v okamžiku MouseDown, nikoli aktuální pozice (ta už je přesunutá = mimo prostor _CurrentMouseDragStart):
            GActivePosition mouseCurrentItem = this._CurrentActiveItem;
            Point downPoint = this._MouseDownAbsolutePoint.Value;
            this._MouseCurrentRelativePoint = _GetRelativePoint(downPoint, mouseCurrentItem);
            if (mouseCurrentItem.CanDrag)
            {
                mouseCurrentItem.CurrentStateFill(downPoint);
                this._MouseDragMoveItem = mouseCurrentItem;
                this._MouseDragMoveItemOriginBounds = mouseCurrentItem.ActiveItem.Bounds;
                var stateArgs = this._ItemMouseCallStateChangedEvent(mouseCurrentItem, GInteractiveChangeState.LeftDragMoveBegin, this._MouseCurrentRelativePoint);
                if (stateArgs.UserDragPoint.HasValue)
                    this._UserDragPointOffset = stateArgs.UserDragPoint.Value.Sub(downPoint);
            }
            this._MouseDragMoveItemOffset = this._GetRelativePointToCurrentItem(downPoint);
            this._MouseDragStartBounds = null;
        }
        private void _MouseDragMoveStep(MouseEventArgs e)
        {
            this._CurrentTargetItem = this.FindActivePositionAtPoint(e.Location, false);
            GActivePosition mouseSourceItem = this._CurrentActiveItem;
            GActivePosition mouseTargetItem = this._CurrentTargetItem;
            if (this._MouseDragMoveItem != null && mouseSourceItem.CanDrag)
            {
                mouseSourceItem.CurrentStateFill(e.Location);
                Point? userDragPoint = null;
                if (this._UserDragPointOffset.HasValue)
                    userDragPoint = e.Location.Add(this._UserDragPointOffset.Value);
                this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, mouseSourceItem);
                Point shift = e.Location.Sub(this._MouseDownAbsolutePoint.Value);
                Rectangle dragToArea = this._MouseDragMoveItemOriginBounds.Value.ShiftBy(shift);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveStep, this._MouseCurrentRelativePoint,
                    targetPosition: mouseTargetItem, dragToArea: dragToArea, userDragPoint: userDragPoint);
            }
        }
        private void _MouseDragMoveCancel()
        {
            GActivePosition mouseSourceItem = this._CurrentActiveItem;
            if (this._MouseDragMoveItem != null && mouseSourceItem.CanDrag)
            {
                mouseSourceItem.CurrentStateFill(null);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveCancel);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveEnd);
                this._RepaintAllItems = true;
                this._MouseDragMoveItem = null;
            }
            this._MouseDragMoveItemOffset = null;              // on primary handler _MouseUp() will be called _MouseRaise(), instead of _MouseDragDone()!  In _MouseDragDone() will be called _MouseDownReset().
            this._CurrentMouseDragCanceled = true;
        }
        private void _MouseDragMoveDone(MouseEventArgs e)
        {
            GActivePosition mouseSourceItem = this._CurrentActiveItem;
            this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, mouseSourceItem);
            this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftUp, this._MouseCurrentRelativePoint);
            if (this._MouseDragMoveItem != null && mouseSourceItem.CanDrag)
            {
                mouseSourceItem.CurrentStateFill(e.Location);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveDone, this._MouseCurrentRelativePoint, targetPosition: this._CurrentTargetItem);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveEnd);
            }
            this._MouseDragMoveItem = null;
            this._CurrentTargetItem = null;
            this._MouseDragState = MouseMoveDragState.None;
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
        #endregion
        #region Metody pro volání interaktivních metod na prvcích IInteractiveItem.AfterStateChanged() atd
        /// <summary>
        /// Return change state for current mousebutton (_CurrentMouseDownButtons), for change specified for left button.
        /// When no button pressed, or state is not button-dependent, then unchanged state is returned.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="isEnabled"></param>
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
        /// <param name="mouseDownButtons"></param>
        /// <param name="isEnabled"></param>
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
        /// <param name="isEnabled"></param>
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
                case GInteractiveChangeState.LeftClickSelect: return GInteractiveState.MouseOver;
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
        /// <param name="oldActiveItem"></param>
        /// <param name="newActiveItem"></param>
        private void _ItemMouseExchange(GActivePosition oldActiveItem, GActivePosition newActiveItem)
        {
            this._ItemMouseExchange(oldActiveItem, newActiveItem, null);
        }
        /// <summary>
        /// Zajistí přechod mezi dosud aktivním prvkem (oldActiveItem) a nově aktivním prvkem (newActiveItem).
        /// Tedy zajistí vyvolání eventů MouseLeave a MouseEnter pro prvky, které to potřebují.
        /// Nakonec uloží (newActiveItem) do <see cref="_CurrentActiveItem"/>.
        /// </summary>
        /// <param name="oldActiveItem"></param>
        /// <param name="newActiveItem"></param>
        /// <param name="mouseRelativePoint"></param>
        private void _ItemMouseExchange(GActivePosition oldActiveItem, GActivePosition newActiveItem, Point? mouseRelativePoint)
        {
            List<GActivePosition.GActiveItem> leaveList, enterList;
            GActivePosition.MapExchange(oldActiveItem, newActiveItem, out leaveList, out enterList);
            IInteractiveItem leaveItem = oldActiveItem?.ActiveItem;
            IInteractiveItem enterItem = newActiveItem?.ActiveItem;

            foreach (GActivePosition.GActiveItem activeItem in leaveList)
                this._CallItemStateChangedEventEnterLeave(activeItem, GInteractiveChangeState.MouseLeave, leaveItem, enterItem);

            foreach (GActivePosition.GActiveItem activeItem in enterList)
                this._CallItemStateChangedEventEnterLeave(activeItem, GInteractiveChangeState.MouseEnter, leaveItem, enterItem);

            if (newActiveItem != null && newActiveItem.CanOver)
                this._ItemMouseCallStateChangedEvent(newActiveItem, GInteractiveChangeState.MouseOver, mouseRelativePoint);

            this._CurrentActiveItem = newActiveItem;
        }
        /// <summary>
        /// Metoda zavolá klíčovou událost <see cref="IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro aktivní prvek dle parametru "item", 
        /// je voláni při změně aktivního prvku (<see cref="GInteractiveChangeState.MouseLeave"/> a <see cref="GInteractiveChangeState.MouseEnter"/>).
        /// Podle potřeby (podle typu akce) vyvolá i událost <see cref="IInteractiveItem.DragAction(GDragActionArgs)"/>.
        /// </summary>
        /// <param name="activeItem"></param>
        /// <param name="change"></param>
        /// <param name="leaveItem">Prvek, který opouštíme</param>
        /// <param name="enterItem">Prvek, do kterého vstupujeme</param>
        private void _CallItemStateChangedEventEnterLeave(GActivePosition.GActiveItem activeItem, GInteractiveChangeState change, IInteractiveItem leaveItem, IInteractiveItem enterItem)
        {
            bool isEnabled = activeItem.Item.Is.Enabled;
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, isEnabled);
            var targetState = _GetStateAfterChange(realChange, isEnabled);
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(activeItem.BoundsInfo, realChange, targetState, this.FindNewItemAtPoint, leaveItem, enterItem);
            stateArgs.UserDragPoint = null;

            activeItem.Item.AfterStateChanged(stateArgs);
            this._ItemMouseCallDragEvent(activeItem.Item, stateArgs);

            this._CallInteractiveStateChanged(stateArgs);
            this._InteractiveDrawStore(stateArgs);
        }
        /// <summary>
        /// Metoda zavolá klíčovou událost <see cref="IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro aktivní prvek dle parametru "item", po události s myší.
        /// Podle potřeby (podle typu akce) vyvolá i událost <see cref="IInteractiveItem.DragAction(GDragActionArgs)"/> .
        /// Je použito pouze pro většinu myších eventů.
        /// </summary>
        /// <param name="activePosition">Aktuální prvek, jehož se akce týká</param>
        /// <param name="change">Změna stavu, ale nezávislá na konkrétním myším buttonu (Left / Right).
        /// Typicky se zadává změna "na levém buttonu", například <see cref="GInteractiveChangeState.LeftDown"/>.
        /// Tato metoda pak určí skutečně stisknuté tlačítko myši (je uloženo v <see cref="_MouseDownButtons"/>) 
        /// a použije reálnou hodnotu, například <see cref="GInteractiveChangeState.RightDown"/>.
        /// Využívá k tomu metodu <see cref="_GetStateForCurrentMouseButton(GInteractiveChangeState, bool)"/>.</param>
        /// <param name="mouseRelativePoint"></param>
        /// <param name="recurseToSolver"></param>
        /// <param name="targetPosition"></param>
        /// <param name="dragToArea"></param>
        /// <param name="userDragPoint"></param>
        /// <param name="fillArgs"></param>
        private GInteractiveChangeStateArgs _ItemMouseCallStateChangedEvent(GActivePosition activePosition, GInteractiveChangeState change,
            Point? mouseRelativePoint = null, bool recurseToSolver = false, GActivePosition targetPosition = null, Rectangle? dragToArea = null, Point? userDragPoint = null,
            Action<GInteractiveChangeStateArgs> fillArgs = null)
        {
            bool isEnabled = activePosition.IsEnabled;
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, isEnabled);
            GInteractiveState state = (activePosition.HasItem ? _GetStateAfterChange(realChange, isEnabled) : GInteractiveState.Disabled);
            BoundsInfo boundsInfo = activePosition.ActiveItemBoundsInfo;
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(boundsInfo, realChange, state,
                this.FindNewItemAtPoint, activePosition.MouseAbsolutePoint, mouseRelativePoint,
                this._MouseDragMoveItemOriginBounds, dragToArea, targetPosition);
            stateArgs.ToolTipData = this._FlowToolTipData;
            stateArgs.UserDragPoint = userDragPoint;

            if (fillArgs != null)
                fillArgs(stateArgs);

            if (activePosition.HasItem)
            {
                activePosition.CallAfterStateChanged(stateArgs, recurseToSolver);
                this._ItemMouseCallDragEvent(activePosition.ActiveItem, stateArgs);
            }
            this._FlowToolTipData = (stateArgs.HasToolTipData ? stateArgs.ToolTipData : null);

            this._CallInteractiveStateChanged(stateArgs);
            this._InteractiveDrawStore(stateArgs);
            this._StoreContextMenu(stateArgs);

            return stateArgs;
        }
        /// <summary>
        /// Úložiště pro data ToolTipu v rámci jedné WinForm události.
        /// Na začátku každé WinForm události se nuluje.
        /// Pokud jedna WinForm událost má více logických událostí v <see cref="GInteractiveControl"/>, 
        /// pak se sem ukládají data ToolTipu po skončení jedné události,
        /// a odsud se načtou a vkládají do <see cref="GInteractiveChangeStateArgs.ToolTipData"/> do další události.
        /// Důsledkem je to, že pokud jedna <see cref="GInteractiveControl"/> událost nastaví tooltip,
        /// pak další událost v <see cref="GInteractiveControl"/> tento tooltip již nemusí řešit a tooltip je stále nastaven.
        /// </summary>
        private ToolTipData _FlowToolTipData;
        /// <summary>
        /// Pokud aktuální akce se týká procesu Drag and Move, pak vyvolá patřičnou akci na prvku.
        /// </summary>
        /// <param name="item"></param>
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
            this._MouseDragFrameActive = false;
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
            this._CurrentActiveItem = null;
            this._MouseCurrentRelativePoint = null;
            this._CurrentCursorType = null;
        }
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="GActivePosition"/>, která bude obsahovat plnou cestu k prvku, který je na dané absolutní souřadnici.
        /// Dovolí najít i prvky, které mají <see cref="InteractiveProperties.Enabled"/> = false.
        /// Tato metoda nebere ohled na aktuálně nalezený prvek (<see cref="_CurrentActiveItem"/>), ignoruje tedy vlastnost <see cref="InteractiveProperties.HoldMouse"/> prvku,
        /// který je nalezen jako aktivní v <see cref="_CurrentActiveItem"/>.
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
        /// Dovolí najít i prvky, které mají <see cref="InteractiveProperties.Enabled"/> = false.
        /// Tato metoda BERE ohled na aktuálně nalezený prvek (<see cref="_CurrentActiveItem"/>), a pokud má vlastnost <see cref="InteractiveProperties.HoldMouse"/> = true, 
        /// pak tomuto prvku dává přednost (pokud to lze).
        /// <para/>
        /// Tato metoda NEZMĚNÍ obsah <see cref="_CurrentActiveItem"/>, ale ZMĚNÍ obsah <see cref="_MouseCurrentRelativePoint"/>.
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <returns></returns>
        protected GActivePosition FindActivePositionAtPoint(Point mouseAbsolutePoint, bool withDisabled)
        {
            GActivePosition activePosition = (this._ProgressItem.Is.Visible
                ? GActivePosition.FindItemAtPoint(this.ClientSize, this.ItemsList, this._CurrentActiveItem, mouseAbsolutePoint, withDisabled, this._ProgressItem)
                : GActivePosition.FindItemAtPoint(this.ClientSize, this.ItemsList, this._CurrentActiveItem, mouseAbsolutePoint, withDisabled));

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
            return _GetRelativePoint(point, this._CurrentActiveItem);
        }
        /// <summary>
        /// Return relative position of specified point to specified GCurrentItem (its CurrentItem.ActiveBounds.Location).
        /// I.e. for CurrentItem.ActiveArea.Location = {100,70} and point = {121,75} returns value {21,5} (= point - CurrentItem.ActiveArea.Location).
        /// When CurrentItem not exists return null.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="activePosition"></param>
        /// <returns></returns>
        protected static Point? _GetRelativePoint(Point point, GActivePosition activePosition)
        {
            return (activePosition == null ? (Point?)null : (Point?)activePosition.GetRelativePointToCurrentItem(point));
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
        /// Aktuální výchozí prvek při jakékoli myší interakci.
        /// Pokud je to interakce se stisknutou myší, pak je zde prvek platný v době stisknutí myši.
        /// </summary>
        private GActivePosition _CurrentActiveItem { get { return this.__MouseCurrentItem; } set { this.__MouseCurrentItem = value; } } private GActivePosition __MouseCurrentItem;
        /// <summary>
        /// Aktuální cílový prvek při myší interakci typu Drag and Move.
        /// Jinak je null.
        /// </summary>
        private GActivePosition _CurrentTargetItem { get { return this.__CurrentTargetItem; } set { this.__CurrentTargetItem = value; } } private GActivePosition __CurrentTargetItem;
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
            this._ShowContextMenu();
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
        #region Podpora pro Kontextové menu
        /// <summary>
        /// Vyvolá událost pro získání kontextového menu
        /// </summary>
        /// <param name="gcItem"></param>
        /// <param name="change"></param>
        /// <param name="mouseRelativePoint"></param>
        private void _ItemMouseCallContextMenu(GActivePosition gcItem, GInteractiveChangeState change, Point? mouseRelativePoint)
        {
            this._ItemMouseCallStateChangedEvent(this._CurrentActiveItem, GInteractiveChangeState.GetContextMenu, this._MouseCurrentRelativePoint);
        }
        /// <summary>
        /// Pokud se v argumentu <see cref="GInteractiveChangeStateArgs"/> nachází kontextové menu, uschová si jej pro pozdější použití.
        /// </summary>
        /// <param name="stateArgs"></param>
        private void _StoreContextMenu(GInteractiveChangeStateArgs stateArgs)
        {
            // Kontextové menu může v podstatě definovat kterákoli událost, a kterákoli její část (typicky: RightClick + GetContextMenu).
            // Tedy tato metoda _StoreContextMenu() může být volána v rámci jedné Win události i dvakrát.
            // V této metodě máme za úkol uložit menu pro jeho zobrazení, až přijde vhodný čas. 
            // A protože menu může být nanejvýše jedno, bude střádat to posledně zadané, ale nikoli null:
            if (this._ContextMenu != null && stateArgs.ContextMenu == null) return;

            this._ContextMenu = stateArgs.ContextMenu;
            if (this._ContextMenu != null)
            {
                if (stateArgs.MouseAbsolutePoint.HasValue)
                {
                    Point point = stateArgs.MouseAbsolutePoint.Value;
                    this._ContextMenuMousePoint = point.Add(-20, 5);
                }
                if (!this._ContextMenuMousePoint.HasValue && stateArgs.BoundsInfo != null)
                {
                    Rectangle bounds = stateArgs.BoundsInfo.CurrentAbsBounds;
                    this._ContextMenuMousePoint = new Point(bounds.X, bounds.Bottom);
                }
            }
        }
        /// <summary>
        /// Metoda má za úkol zobrazit kontextové menu, pokud je nějaké uchováno.
        /// Metoda se vyvolá vždy po dokončení interaktivní akce, až po skončení překreslení controlu.
        /// </summary>
        private void _ShowContextMenu()
        {
            if (this._ContextMenu == null) return;

            var host = this;
            if (host == null) return;

            if (this._ContextMenuMousePoint.HasValue)
                this._ContextMenu.Show(host, this._ContextMenuMousePoint.Value, System.Windows.Forms.ToolStripDropDownDirection.BelowRight);
            else
                this._ContextMenu.Show(Control.MousePosition, System.Windows.Forms.ToolStripDropDownDirection.BelowRight);

            this._ContextMenu = null;
            this._ContextMenuMousePoint = null;
        }
        private ToolStripDropDownMenu _ContextMenu { get { return this.__ContextMenu; } set { this.__ContextMenu = value; } }
        private ToolStripDropDownMenu __ContextMenu;
        private Point? _ContextMenuMousePoint;
        #endregion
        #region Podpora pro ToolTip
        /// <summary>
        /// Initialize Tooltip object
        /// </summary>
        private void _ToolTipInit()
        {
            this._ToolTip = new ToolTipItem(this);
            this._ToolTip.TimerDrawRequest += _ToolTip_TimerDrawRequest;
        }
        /// <summary>
        /// Handler for event ToolTipItem.TimerDrawRequest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ToolTip_TimerDrawRequest(object sender, GPropertyEventArgs<float> args)
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
            if (e.ChangeState == GInteractiveChangeState.MouseOver && toolTipData == null) return; // Pokud je akce MouseOver a tooltip není zadán, není to důvod jej zhasnout.
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
            if (this._ToolTipNeedTick)
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
        private bool _ToolTipNeedTick { get { return (this._ToolTip != null && this._ToolTip.NeedAnimation); } }
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
        #region Selector, a obecně Selectování pomocí myši (LeftClick) a Označování (Drag and Frame)
        /// <summary>
        /// Instance objektu třídy <see cref="Selector"/>, řeší výběr prvků.
        /// </summary>
        public Selector Selector
        {
            get
            {
                if (this._Selector == null)
                    this._Selector = new Selector();
                return this._Selector;
            }
        }
        private Selector _Selector;
        /// <summary>
        /// Metoda je volána při MouseDown, při stisku LeftMouse, a pokud aktuální prvek má <see cref="GActivePosition.ItemIsSelectable"/> == true.
        /// Metoda sama zjistí Modifier keys z <see cref="GActivePosition.CurrentModifierKeys"/>, 
        /// a pokud NENÍ stisknutý Control, tak zruší stav IsSelect na všech aktuálně označených objektech.
        /// </summary>
        /// <param name="activeItem"></param>
        private void _ItemMouseLeftDownUnSelect(GActivePosition activeItem)
        {
            bool leaveOther = activeItem.CurrentModifierKeys.HasFlag(Keys.Control);
            if (!leaveOther)
                this.Selector.ClearSelected();
        }
        /// <summary>
        /// Uživatel provedl LeftClick na prvku, který má nastaveno <see cref="InteractiveProperties.Selectable"/>, proto by měl být změněn stav <see cref="IInteractiveItem.IsSelected"/>.
        /// </summary>
        /// <param name="activeItem"></param>
        /// <param name="modifierKeys"></param>
        private void _ItemMouseLeftClickSelect(GActivePosition activeItem, Keys modifierKeys)
        {
            this._ItemMouseCallStateChangedEvent(activeItem, GInteractiveChangeState.LeftClickSelect, activeItem.CurrentMouseRelativePoint);
        }
        /// <summary>
        /// Zahájení označování Drag and Frame.
        /// Tato metoda se volá v situaci, kdy je detekován Drag proces (MouseDown a poté MouseMove) na prvku, který nepodporuje MouseDrag, ale podporuje SelectParent.
        /// </summary>
        /// <param name="e"></param>
        private void _MouseDragFrameBegin(MouseEventArgs e)
        {
            // Relativní pozice myši v okamžiku MouseDown, nikoli aktuální pozice (ta už je mimo prostor _CurrentMouseDragStart):
            GActivePosition mouseCurrentItem = this._CurrentActiveItem;
            Point downPoint = this._MouseDownAbsolutePoint.Value;
            this._MouseCurrentRelativePoint = _GetRelativePoint(this._MouseDownAbsolutePoint.Value, mouseCurrentItem);
            if (mouseCurrentItem.ItemIsSelectParent)
            {
                mouseCurrentItem.CurrentStateFill(downPoint);
                this._MouseDragFrameParentItem = mouseCurrentItem;
                var stateArgs = this._ItemMouseCallStateChangedEvent(mouseCurrentItem, GInteractiveChangeState.LeftDragFrameBegin, this._MouseCurrentRelativePoint);
                this._MouseDragFrameWorkArea = stateArgs.DragFrameWorkArea;
            }

            // V Selectoru: zrušit dosavadní Framed (to je jen pro jistotu):
            this.Selector.ClearFramed();
            // ... a pokud NENÍ stisknut CTRL, pak zrušit i prvky Selected:
            Keys modifierKeys = Control.ModifierKeys;
            bool leaveOther = modifierKeys.HasFlag(Keys.Control);
            if (!leaveOther)
                this.Selector.ClearSelected();

            this._MouseDragFrameActive = true;
            this._MouseDragStartBounds = null;
        }
        /// <summary>
        /// Jeden krok v procesu Drag and Frame = selectování rámečkem.
        /// Určí se prostor, který je označen; najde se prvky, které v něm leží (jsou dostupné, selectovatelné, a označené alespoň z 25%);
        /// a následně se prvky označí jako IsFramed (pomocí Selectoru).
        /// </summary>
        /// <param name="e"></param>
        private void _MouseDragFrameStep(MouseEventArgs e)
        {
            if (!this._MouseDownAbsolutePoint.HasValue) return;
            if (!this._MouseDragFrameActive) return;
            Rectangle frameBounds = DrawingExtensions.FromPoints(this._MouseDownAbsolutePoint.Value, e.Location);
            if (this._MouseDragFrameWorkArea.HasValue)
                frameBounds = Rectangle.Intersect(this._MouseDragFrameWorkArea.Value, frameBounds);

            this._MouseDragFrameCurrentBounds = frameBounds;

            Tuple<IInteractiveItem, Rectangle>[] items = GActivePosition.FindItemsAtBounds(this.ClientSize, this.ItemsList, frameBounds,
                (i, b) => _MouseDragFrameFilterScan(i, b, frameBounds),
                (i, b) => _MouseDragFrameFilterAccept(i, b, frameBounds)
                );

            this.Selector.SetFramedItems(items.Select(i => i.Item1));
        }
        /// <summary>
        /// Metoda vrací true, pokud daný prvek může být scanován co do jeho Childs prvků
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemAbsoluteVisibleBounds"></param>
        /// <param name="frameBounds"></param>
        /// <returns></returns>
        private bool _MouseDragFrameFilterScan(IInteractiveItem item, Rectangle itemAbsoluteVisibleBounds, Rectangle frameBounds)
        {
            return (item.Is.Visible && item.Is.Enabled);
        }
        /// <summary>
        /// Metoda vrací true, pokud daný prvek má být akceptován do výstupního pole DragFrame
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemAbsoluteVisibleBounds"></param>
        /// <param name="frameBounds"></param>
        /// <returns></returns>
        private bool _MouseDragFrameFilterAccept(IInteractiveItem item, Rectangle itemAbsoluteVisibleBounds, Rectangle frameBounds)
        {
            if (!(item.Is.Visible && item.Is.Enabled && item.Is.Selectable)) return false;
            int itemPixels = itemAbsoluteVisibleBounds.GetArea();
            Rectangle selectedBounds = Rectangle.Intersect(itemAbsoluteVisibleBounds, frameBounds);
            int selectedPixels = selectedBounds.GetArea();
            float selectedRatio = (itemPixels <= 0 ? 1f : ((float)selectedPixels / (float)itemPixels));
            return (selectedRatio >= 0.25f);
        }
        /// <summary>
        /// Po stisku Escape při Drag and Frame
        /// </summary>
        private void _MouseDragFrameCancel()
        {
            if (!this._MouseDragFrameActive) return;
            this._Selector.ClearFramed();
            this._MouseDragFrameActive = false;
        }
        /// <summary>
        /// Metoda je zavolána po skončení procesu Drag and Frame, jejím úkolem je označit vybrané prvky pomocí Selectoru, a odeslat jim zprávu.
        /// </summary>
        /// <param name="e"></param>
        private void _MouseDragFrameDone(MouseEventArgs e)
        {
            if (this._MouseDragFrameActive)
            {
                this.Selector.MoveFramedToSelected();
            }

            this._MouseDragFrameCurrentBounds = null;
            this._MouseDragState = MouseMoveDragState.None;

            this.Repaint();
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
            if (!this._MouseDragFrameActive) return;
            GPainter.DrawFrameSelect(graphics, this._MouseDragFrameCurrentBounds.Value);
        }
        /// <summary>
        /// Aktivita procesu Drag and Frame: true po startu (<see cref="_MouseDragFrameBegin(MouseEventArgs)"/>), 
        /// false po cancelu (<see cref="_MouseDragFrameCancel()"/>) nebo po skončení (<see cref="_MouseDragFrameDone(MouseEventArgs)"/>).
        /// </summary>
        private bool _MouseDragFrameActive { get; set; }
        /// <summary>
        /// Prvek, který je Parentem aktuální akce Drag and Frame.
        /// </summary>
        private GActivePosition _MouseDragFrameParentItem { get; set; }
        /// <summary>
        /// Souřadnice prostoru, do něhož má být omezen proces Drag and Frame.
        /// Prostor deklaruje prvek Parent na začátku procesu Drag and Frame ve své události .
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
        #region DrawItems, zařazování controlů k vykreslení
        /// <summary>
        /// Pro budoucí rozvoj
        /// </summary>
        /// <param name="control"></param>
        /// <param name="drawToLayers"></param>
        public void AddItemToDraw(IInteractiveItem control, GInteractiveDrawLayer drawToLayers)
        {
            // Nemám zatím chuť to předělávat, je toho dost a dost...:
            //if (control == null || drawToLayers == GInteractiveDrawLayer.None) return;
            //if (drawToLayers.HasFlag(GInteractiveDrawLayer.Standard)) this._AddItemToDraw(control, this._DrawItemStandard);
            //if (drawToLayers.HasFlag(GInteractiveDrawLayer.Interactive)) this._AddItemToDraw(control, this._DrawItemInteractive);
            //if (drawToLayers.HasFlag(GInteractiveDrawLayer.Dynamic)) this._AddItemToDraw(control, this._DrawItemDynamic);
        }
        private void _AddItemToDraw(IInteractiveItem control, Dictionary<uint, IInteractiveItem> targetDict)
        {
            if (!targetDict.ContainsKey(control.Id))
                targetDict.Add(control.Id, control);
        }
        private Dictionary<uint, IInteractiveItem> _DrawItemStandard;
        private Dictionary<uint, IInteractiveItem> _DrawItemInteractive;
        private Dictionary<uint, IInteractiveItem> _DrawItemDynamic;
        #endregion
        #region Draw
        /// <summary>
        /// true = můžeme kreslit?
        /// </summary>
        protected override bool CanDraw { get { return (this._DrawState == InteractiveDrawState.Standard || this._DrawState == InteractiveDrawState.InteractiveRepaint); } }
        /// <summary>
        /// Inicializuje subsystém Draw
        /// </summary>
        private void _DrawInit()
        {
            this.LayerCount = 4;          // [0] = Standard;  [1] = Dynamic;  [2] = Interactive;  [3] = ToolTip, Progress Window and Animations
            this._DrawItemStandard = new Dictionary<uint, IInteractiveItem>();
            this._DrawItemInteractive = new Dictionary<uint, IInteractiveItem>();
            this._DrawItemDynamic = new Dictionary<uint, IInteractiveItem>();
        }
        /// <summary>
        /// Main paint process (standard and interactive)
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintLayers(LayeredPaintEventArgs e)
        {
            Size graphicsSize = e.GraphicsSize;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "GInteractiveControl", "OnPaintLayers", "", "Bounds: " + this.Bounds))
            {
                DrawRequest request = e.UserData as DrawRequest;
                scope.AddItem("e.UserData: " + ((request == null) ? "null => Full Draw" : "Explicit request"));
                scope.AddItem("PendingFullDraw: " + (this.PendingFullDraw ? "true => Full Draw" : "false"));
                if (request == null || this.PendingFullDraw)
                {   // Explicit request not specified, we will draw all items:
                    request = new DrawRequest(true, this._NeedDrawFrameBounds, this._ToolTip, this._ProgressItem);
                    request.Fill(this.ClientSize, this, this.ItemsList, true, false);
                }

                if (request.NeedStdDraw || request.DrawAllItems)
                {
                    if (request.DrawAllItems)
                        base.OnPaintLayers(e);
                    Graphics graphics = e.GetGraphicsForLayer(0, true);
                    this.CallDrawStandardLayer(graphics);
                    this._PaintItems(graphics, graphicsSize, request.StandardItems, GInteractiveDrawLayer.Standard);
                    scope.AddItem("Layer Standard, Items: " + request.StandardItems.Count.ToString());
                }
                if (request.NeedIntDraw)
                {
                    Graphics graphics = e.GetGraphicsForLayer(1, true);
                    this._PaintItems(graphics, graphicsSize, request.InteractiveItems, GInteractiveDrawLayer.Interactive);
                    scope.AddItem("Layer Interactive, Items: " + request.InteractiveItems.Count.ToString());
                }
                if (request.NeedDynDraw)
                {
                    Graphics graphics = e.GetGraphicsForLayer(2, true);
                    if (request.DynamicItems.Count > 0)
                    {
                        this._PaintItems(graphics, graphicsSize, request.DynamicItems, GInteractiveDrawLayer.Dynamic);
                        scope.AddItem("Layer Dynamic, Items: " + request.DynamicItems.Count.ToString());
                    }
                    if (request.DrawFrameSelect)
                    {
                        this._PaintFrameBounds(graphics, GInteractiveDrawLayer.Dynamic);
                    }
                }

                if (this.HasBlockedGuiMessage || this._ProgressItem.Is.Visible || this._ToolTip.NeedDraw)
                {
                    Graphics graphics = e.GetGraphicsForLayer(3, true);
                    if (this.IsGUIBlocked)
                    {
                        if (this.HasBlockedGuiMessage)
                            this.BlockedGuiDraw(graphics, e.GraphicsSize);
                    }
                    else
                    {
                        if (this._ProgressItem.Is.Visible)
                            this._ProgressItem.Draw(graphics);
                        if (this._ToolTip.NeedDraw)
                            this._ToolTip.Draw(graphics);
                    }
                    scope.AddItem("Layer BlockGui + Progress + ToolTip");
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
        /// <param name="graphics">Grafika, do níž se kreslí</param>
        /// <param name="size">Velikost prostoru pro kreslení</param>
        /// <param name="items"></param>
        /// <param name="drawLayer"></param>
        private void _PaintItems(Graphics graphics, Size size, IEnumerable<DrawRequestItem> items, GInteractiveDrawLayer drawLayer)
        {
            if (items != null)
            {
                graphics.ResetTransform();
                graphics.ResetClip();
                GInteractiveDrawArgs e = new GInteractiveDrawArgs(graphics, size, drawLayer);
                if (e.IsStandardLayer)
                {   // Standardní vrstva: před kreslením se provede Clip:
                    foreach (DrawRequestItem item in items)
                    {
                        e.GraphicsClipWith(item.AbsoluteVisibleClip);
                        item.Draw(e);
                        e.ResetClip();
                    }
                }
                else
                {   // Ostatní vrstvy: clip se implicitně neprovádí:
                    foreach (DrawRequestItem item in items)
                    {
                        item.Draw(e);
                        if (e.HasClip)
                            e.ResetClip();
                    }
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
        /// <param name="repaintLayers"></param>
        protected virtual void Repaint(GInteractiveDrawLayer repaintLayers)
        {
            this.Repaint();
        }
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
        /// <summary>
        /// Metoda zajistí překreslení celého tohoto controlu.
        /// Pokud ale v této době probíhá nějaká interaktivní akce (typicky myšovitá), pak se nevyvolá tento Refresh(), protože by docházelo k blikální.
        /// Na konci interaktivní operace se provádí překreslení, a to tedy zajistí překreslení objektu, které je požadováno zde.
        /// </summary>
        public override void Refresh()
        {
            if (this.InteractiveProcessing) return;
            base.Refresh();
        }
        #region class DrawRequest + DrawRequestItem
        /// <summary>
        /// Class for analyze items to repaint/draw
        /// </summary>
        protected class DrawRequest
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            private DrawRequest()
            {
                this.ProcessedItems = new Dictionary<uint, IInteractiveItem>();
                this.StandardItems = new List<DrawRequestItem>();
                this.InteractiveItems = new List<DrawRequestItem>();
                this.DynamicItems = new List<DrawRequestItem>();
                this.DrawAllItems = false;
            }
            /// <summary>
            /// Konstruktor s daty
            /// </summary>
            /// <param name="drawAllItems"></param>
            /// <param name="drawFrameSelect"></param>
            /// <param name="toolTipItem"></param>
            /// <param name="progressItem"></param>
            public DrawRequest(bool drawAllItems, bool drawFrameSelect, ToolTipItem toolTipItem, ProgressItem progressItem)
                : this()
            {
                this.DrawAllItems = drawAllItems;
                this.DrawFrameSelect = drawFrameSelect;
                this.DrawToolTip = (toolTipItem != null && toolTipItem.NeedDraw);
                this.DrawProgress = (progressItem != null && progressItem.Is.Visible);
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
            /// Obsahuje true, pokud je požadavek na vykreslení FrameSelect obdélníku (kreslí se do vrstvy Dynamic)
            /// </summary>
            public bool DrawFrameSelect { get; private set; }
            /// <summary>
            /// Obsahuje true, pokud je požadavek na vykreslení ToolTipu
            /// </summary>
            public bool DrawToolTip { get; private set; }
            /// <summary>
            /// Obsahuje true, pokud je požadavek na vykreslení Progressu
            /// </summary>
            public bool DrawProgress { get; private set; }
            /// <summary>
            /// Obsahuje true, pokud je požadavek na vykreslení v Interactive režimu
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
                    if (!item.Is.Visible) continue;

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
            /// Obsahuje true, pokud tento požadavek je určen pro volání metody <see cref="IInteractiveItem.DrawOverChilds(GInteractiveDrawArgs, Rectangle, Rectangle)"/>.
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
        #region Pomocné metody a objekty pro vykreslování, postupně utlumit
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
        /// <param name="bounds">Souřadnice v koordinátech Controlu</param>
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
            if (!App.IsDeveloperMode) return;

            decimal avt = this.StopwatchAverageTime;
            if (avt <= 0m) return;

            decimal fps = Math.Round(1m / avt, 1);
            string info = "     " + fps.ToString("### ##0.0").Trim() + " fps";
            Size size = new System.Drawing.Size(90, 20);
            Rectangle bounds = size.AlignTo(this.ClientRectangle, ContentAlignment.BottomRight).Enlarge(0, 0, -1, -1);
            Graphics graphics = e.GetGraphicsCurrent();
            Color backColor = Color.FromArgb(240, Color.LightSkyBlue);
            Color foreColor = Color.FromArgb(240, Color.Black);
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
        /// <summary>
        /// Stopky
        /// </summary>
        protected System.Diagnostics.Stopwatch Stopwatch { get; private set; }
        #endregion
        #region Background Thread: animace, tooltip fader...
        /// <summary>
        /// Inicializace threadu Background.
        /// Volá se v rámci inicializace celého controlu.
        /// Threadu Background je živý stále, až do Dispose.
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
        /// Zastaví thread Background.
        /// Volá se pouze v Dispose.
        /// </summary>
        /// <remarks>Can run in boot thread (GUI and BackThread)</remarks>
        private void _BackThreadDone()
        {
            this._BackThreadStop = true;
            this._BackSemaphore.Set();
        }
        /// <summary>
        /// Nastartuje thread Background do plného režimu, kdy se volá Tick po 40ms.
        /// Pošle signál do threadu nas pozadí, aby se provedla první animační akce.
        /// Pokud ale je thread Background v plném režimu, neprovede nic (aby se nezaneslo rušení do plynulé animace = tzb. Jitter).
        /// Metodu lze volat z threadu na pozadí i z GUI threadu.
        /// </summary>
        private void _BackThreadResume()
        {
            if (!this._BackThreadActive)
            {
                this._BackThreadActive = true;
                this._BackSemaphore.Set();
            }
        }
        /// <summary>
        /// Metoda uvede thread Background do ospalého režimu, kdy se volá Tick po 1000ms.
        /// Metodu lze zavolat kdykoliv, sama si zjistí, zda neexistuje nějaký aktivní zdroj animace.
        /// </summary>
        private void _BackThreadTrySuspend()
        {
            if (this._BackThreadActive)
            {
                bool activeToolTip = this._ToolTipNeedTick;
                bool activeAnimation = this._AnimationNeedTick;
                if (!activeToolTip && !activeAnimation)
                    this._BackThreadActive = false;
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
            if (this._BackThreadProcess) return;           // Pokud jsme ještě nestihli obsloužit poslední Tick, nezačneme řešit nový...

            try
            {
                this._BackThreadProcess = true;

                bool needDraw = false;
                bool drawItems = false;

                try
                {
                    // Zeptáme se ToolTipu, zda má potřebu nějaké animace:
                    if (this._ToolTipNeedTick)
                    {   // Pošleme do ToolTipu Tick, on nám vrátí true, pokud potřebuje překreslit:
                        bool needDrawToolTip = this._ToolTip.AnimateTick();
                        needDraw |= needDrawToolTip;
                    }

                    // Jakákoli jiná animace:
                    if (this._AnimationNeedTick)
                    {
                        bool needDrawAnimation = this._AnimatorTick();
                        needDraw |= needDrawAnimation;
                        drawItems = needDrawAnimation;
                    }
                }
                catch (Exception) { }

                // Zkusíme přejít do Suspend režimu (pokud všichni animátoři mají hotovo):
                this._BackThreadTrySuspend();

                // Kreslení:
                if (needDraw)
                    this._BackThreadRunDraw(drawItems);

            }
            finally
            {
                this._BackThreadProcess = false;
            }
        }
        /// <summary>
        /// true = právě probíhá výkon v metodě <see cref="_BackThreadRun()"/>, nebudeme spouštět její další instanci
        /// </summary>
        private bool _BackThreadProcess;
        /// <summary>
        /// Vyvolá překreslení tohoto controlu, a to buď kompletně celý objekt anebo pouze vrstu ToolTip, Progress Animace.
        /// Tato metoda převoává GUI thread, může se tedy spouštět z threadu Background.
        /// </summary>
        /// <param name="drawItems">true = vykreslit i běžné prvky (=naplnit request pro kreslení)</param>
        private void _BackThreadRunDraw(bool drawItems)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action<bool>(this._BackThreadRunDrawGui), drawItems);
            else
                this._BackThreadRunDrawGui(drawItems);
        }
        /// <summary>
        /// Vyvolá překreslení tohoto controlu, a to buď kompletně celý objekt anebo pouze vrstu ToolTip, Progress Animace.
        /// Běží v GUI threadu.
        /// </summary>
        /// <param name="drawItems">true = vykreslit i běžné prvky (=naplnit request pro kreslení)</param>
        private void _BackThreadRunDrawGui(bool drawItems)
        {
            if (this._BackThreadRunDrawGuiProcess) return;
            try
            {
                this._BackThreadRunDrawGuiProcess = true;
                DrawRequest request = new DrawRequest(this.RepaintAllItems, this._NeedDrawFrameBounds, this._ToolTip, null);
                if (drawItems)
                    request.Fill(this.ClientSize, this, this.ItemsList, this.RepaintAllItems, false);
                request.InteractiveMode = true;
                this.Draw(request);
            }
            catch (Exception) { }
            finally
            {
                this._BackThreadRunDrawGuiProcess = false;
            }
        }
        /// <summary>
        /// true = právě probíhá výkon v metodě <see cref="_BackThreadRunDrawGui(bool)"/>, nebudeme spouštět její další instanci
        /// </summary>
        private bool _BackThreadRunDrawGuiProcess;
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
        #region Animace
        /// <summary>
        /// Metoda přidá danou funkci do seznamu animátorů.
        /// Daná funkce bude volána 1x za 40 milisekund (25x za sekundu).
        /// Funkce má zařídit svoji animaci a vrátit informaci, co dál.
        /// </summary>
        /// <param name="animatorTick"></param>
        public void AnimationStart(Func<AnimationResult> animatorTick)
        {
            if (animatorTick == null) return;
            this._AnimatorTickList.Add(animatorTick);
            this._BackThreadResume();
        }
        /// <summary>
        /// Vrací true, pokud animátor má běhat; false pokud není důvod provádět animaci
        /// </summary>
        private bool _AnimationNeedTick { get { return (this._AnimatorTickList.Count > 0); } }
        /// <summary>
        /// Metoda provede všechny zaregistrované animační procedury. 
        /// Vrátí true = je třeba provést překreslení.
        /// </summary>
        /// <returns></returns>
        private bool _AnimatorTick()
        {
            bool needDraw = false;
            for (int i = 0; i < this._AnimatorTickList.Count; i++)
            {
                Func<AnimationResult> animatorTick = this._AnimatorTickList[i];
                AnimationResult result = (animatorTick != null ? animatorTick() : AnimationResult.Stop);
                if (result.HasFlag(AnimationResult.Draw) && !needDraw)
                    // Daná animační akce vyžaduje Draw => zajistíme to:
                    needDraw = true;
                if (result.HasFlag(AnimationResult.Stop))
                {   // Daná animační akce již skončila => odeberu si ji ze svého seznamu:
                    this._AnimatorTickList.RemoveAt(i);
                    i--;
                }
            }
            return needDraw;
        }
        /// <summary>
        /// Inicializace systému Animátor
        /// </summary>
        private void _AnimatorInit()
        {
            this._AnimatorTickList = new List<Func<AnimationResult>>();
        }
        /// <summary>
        /// Pole aktivních animátorů
        /// </summary>
        private List<Func<AnimationResult>> _AnimatorTickList;
        #endregion
        #region Blokování GUI, vykreslení blokovaného GUI; podpora pro zavírání okna
        /// <summary>
        /// Metoda provede zablokování GUI, s daným Timeoutem (tzn. za tento čas bude GUI automaticky uvolněno).
        /// Pokud je specifikována zpráva (message), pak GUI viditelně zešedne a uprostřed je zobrazena tato hláška.
        /// </summary>
        /// <param name="blockTime"></param>
        /// <param name="message">Zpráva zobrazená uživateli po dobu blokování GUI.
        /// Zpráva může obsahovat více řádků, oddělené CrLf.
        /// První řádek bude zobrazen výrazně (jako titulek), další řádky standardně.
        /// </param>
        internal void BlockGUI(TimeSpan blockTime, string message = null)
        {
            if (this.IsGUIBlocked) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<TimeSpan, string>(this.BlockGUI), blockTime, message);
            }
            else
            {
                this._OnMouseLeave();
                this.BlockedGuiCursor = this.Cursor;
                this.BlockedTimeEnd = DateTime.Now + blockTime;
                this.BlockedGuiMessage = message;
                this.Cursor = Cursors.WaitCursor;
                this.IsGUIBlocked = true;
                if (this.HasBlockedGuiMessage)
                {   // Pokud je blokování GUI definováno včetně textu "message", pak zahájíme animaci BlockGui:
                    this.BlockGuiAnimatorInit(blockTime);
                    this.BlockGuiAnimatorRunning = true;
                    this.AnimationStart(this.BlockGuiAnimatorTick);
                    // this.Refresh();
                }
            }
        }
        /// <summary>
        /// Metoda ukončí blokování GUI.
        /// Blokování GUI končí buď přijetím response z aplikace, anebo uplynutím timeoutu.
        /// </summary>
        internal void ReleaseGUI()
        {
            if (!this.IsGUIBlocked) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(this.ReleaseGUI));
            }
            else
            {
                if (this.IsGUIBlocked)
                {
                    if (this.BlockGuiAnimatorRunning && this.BlockGuiOpacityRatio.HasValue && this.BlockGuiOpacityRatio.Value > 0f)
                    {   // Pokud nám svítí blokovací záclona, tak ji dáme urychleně zhasnout:
                        this.BlockGuiAnimatorFinish();
                    }
                    else
                    {   // Záclonka nesvítí => rovnou skončíme blokaci:
                        bool hasBlockedGuiMessage = this.HasBlockedGuiMessage;
                        this.IsGUIBlocked = false;
                        this.Cursor = (this.BlockedGuiCursor != null ? this.BlockedGuiCursor : Cursors.Default);
                        this.BlockedTimeEnd = null;
                        this.BlockedGuiMessage = null;
                        this.BlockedGuiCursor = null;
                        this.BlockedGuiMsgTextInfos = null;
                        this.BlockedGuiMsgBackgroundBounds = null;
                        this._OnMouseEnterMove();
                        if (hasBlockedGuiMessage)
                            this.Refresh();
                        this.BlockGuiAnimatorRunning = false;
                    }
                }
            }
        }
        /// <summary>
        /// Metoda provede inicializaci animátoru BlockGui pro daný čas timeoutu
        /// </summary>
        /// <param name="totalTime"></param>
        protected void BlockGuiAnimatorInit(TimeSpan totalTime)
        {
            this.BlockGuiOpacityRatio = 0f;

            if (this.BlockGuiAnimator == null)
                this.BlockGuiAnimator = new AnimationControl<float>();
            this.BlockGuiAnimator.Clear();

            // Animace BlockGui má následující průběh:
            // 1. První čas 0 až 1,0 sekundy se neděje nic, BlockGui nesvítí.
            //    Význam je psychologický: krátkoběžící funkce stihnou skončit dříve, a není nutno uživateli blikat před nosem.
            TimeSpan time1 = TimeSpan.FromMilliseconds(1000d);
            this.BlockGuiAnimator.AddPause(time1);

            // 2. Následuje čas dlouhý 0.720 sekundy (= 18 cyklů po 40ms), během něhož se pomocí animace rozsvítí GUI z 0 na 100%:
            TimeSpan time2 = TimeSpan.FromMilliseconds(720d);
            int count = (int)(time2.TotalMilliseconds / 40d);
            for (float c = 1; c <= count; c++)
                this.BlockGuiAnimator.AddStep((c == count) ? 1f : ((float)c / (float)count));

            TimeSpan time4 = TimeSpan.FromMilliseconds(3000d);

            // 3. Následuje čas, kdy je GUI viditelně blokováno po dobu (téměř) celého timeoutu, s odečtením fází 1, 2 a 4:
            TimeSpan time3 = totalTime - time1 - time2 - time4;
            if (time3.Ticks > 0L)
                this.BlockGuiAnimator.AddPause(time3);

            // 4. Poslední fáze je postupné zhasnutí BlockGui, která trvá 3 sekundy, a po jejímž konci vyprší Timeout a odblokuje se GUI:
            count = (int)(time2.TotalMilliseconds / 40d);
            for (float c = 1; c <= count; c++)
                this.BlockGuiAnimator.AddStep((c == count) ? 0f : 1f - ((float)c / (float)count));

            // Animátor nastavíme na začátek cyklu:
            this.BlockGuiAnimator.Rewind();
        }
        /// <summary>
        /// Metoda přeprogramuje animátor BlockGui tak, aby ze současného stavu opacity poměrně rychle, ale plynule zhasl
        /// </summary>
        protected void BlockGuiAnimatorFinish()
        {
            // Kroky uzpůsobíme tak, aby z plného stavu (opacity = 1.0f) se dostal do 0 za 0.72 sec, z nižšího stavu přiměřeně rychleji:
            float opacity = (this.BlockGuiOpacityRatio.HasValue ? this.BlockGuiOpacityRatio.Value : 0.5f);
            List<float> steps = new List<float>();

            while (true)
            {
                if (opacity <= 0f)
                {
                    steps.Add(0f);
                    break;
                }
                steps.Add(opacity);
                opacity -= 0.07f;
            }
            this.BlockGuiAnimator.StoreSteps(steps);
        }
        /// <summary>
        /// Metoda animace jednoho kroku v animátoru BlockGui
        /// </summary>
        /// <returns></returns>
        protected AnimationResult BlockGuiAnimatorTick()
        {
            AnimationResult result = this._BlockGuiAnimatorTick();
            this.BlockGuiAnimatorRunning = (result != AnimationResult.Stop);
            return result;
        }
        private AnimationResult _BlockGuiAnimatorTick()
        {
            if (this.BlockGuiAnimator == null) return AnimationResult.Stop;    // Chybový stav

            // Pokud došlo k ukončení blokace GUI (vnějším zásahem = typicky doběhnutím funkce před stanoveným Timeoutem),
            //  pak nejbližší animační krok (tj. tento) tuto animaci zastaví:
            if (!this.IsGUIBlocked) return AnimationResult.Stop;

            float ratio;
            if (this.BlockGuiAnimator.Tick(out ratio))
            {   // Pokud animátor obsahuje data, která vedou k animaci (=float ratio):
                this.BlockGuiOpacityRatio = ratio;
                return AnimationResult.Draw;
            }

            // Animátor nemá data: animace nebude, a možná bude konec animace = uplynul stanovený timeout:
            if (this.BlockGuiAnimator.IsEnd)
            {
                this.BlockGuiOpacityRatio = 0f;
                this.ReleaseGUI();
                return AnimationResult.Stop;
            }

            // Není ani animační krok, ani konec animace => je prostě jen čekání:
            return AnimationResult.None;
        }
        /// <summary>
        /// Zajistí platnost dat v <see cref="BlockedGuiMsgTextInfos"/> a <see cref="BlockedGuiMsgBackgroundBounds"/>:
        /// vypočítá souřadnice jednotlivých řádků textů dle textu v <see cref="BlockedGuiMessage"/> i celého prostoru hlášky.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="area"></param>
        protected void BlockedGuiCheckTexts(Graphics graphics, Rectangle area)
        {
            if (this.BlockedGuiMsgTextInfos != null && this.BlockedGuiMsgBackgroundBounds != null) return;
            if (String.IsNullOrEmpty(this.BlockedGuiMessage)) return;

            List<BlockedGuiTextInfo> lines = new List<BlockedGuiTextInfo>();
            string[] texts = this.BlockedGuiMessage.ToLines(true, true);
            int maxW = 0;
            int sumH = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                string text = texts[i];
                FontInfo font = (i == 0 ? FontInfo.CaptionBoldBig : FontInfo.DefaultBoldBig);
                font.RelativeSize = (i == 0 ? 175 : 145);
                BlockedGuiTextInfo textInfo = new BlockedGuiTextInfo(text, font);
                textInfo.TextSize = GPainter.MeasureString(graphics, text, font);
                lines.Add(textInfo);
                if (textInfo.TextSize.Width > maxW) maxW = textInfo.TextSize.Width;
                sumH += textInfo.TextSize.Height;
            }

            int bw = maxW + 40;
            int minBw = 30 * area.Width / 100;
            if (bw < minBw) bw = minBw;
            int maxBw = 80 * area.Width / 100;
            if (bw > maxBw) bw = maxBw;

            int bh = sumH + 20;
            int minBh = 5 * area.Height / 100;
            if (bh < minBh) bh = minBh;
            int maxBh = 65 * area.Height / 100;
            if (bh > maxBh) bh = maxBh;

            // Pokud výška prostoru pro Textové pole je menší než 15% výšky controlu, pak jeho šířka bude rovna celé šířce controlu (=> malý pruh, široký přes celé okno):
            int linBh = 15 * area.Height / 100;
            if (bh <= linBh)
                bw = area.Width;

            Size totalSize = new Size(bw, bh);
            Rectangle backBounds = totalSize.AlignTo(area, ContentAlignment.MiddleCenter);

            Size contentSize = new Size(maxW, sumH);
            Rectangle contentBounds = contentSize.AlignTo(backBounds, ContentAlignment.MiddleCenter);
            int x = contentBounds.X;
            int y = contentBounds.Y;
            int w = maxW;
            foreach (BlockedGuiTextInfo textInfo in lines)
            {
                int h = textInfo.TextSize.Height;
                textInfo.TextBounds = new Rectangle(x, y, w, h);
                y += h;
            }

            this.BlockedGuiMsgTextInfos = lines.ToArray();
            this.BlockedGuiMsgBackgroundBounds = backBounds;
        }
        /// <summary>
        /// Metoda zajistí vykreslení blokovaného GUI
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="size"></param>
        protected void BlockedGuiDraw(Graphics graphics, Size size)
        {
            float? opacityRatio = this.BlockGuiOpacityRatio;
            if (!opacityRatio.HasValue || opacityRatio.Value <= 0f) return;

            // Celý prostor překreslit šedou záclonou:
            Rectangle area = new Rectangle(new Point(0, 0), this.ClientRectangle.Size);
            Color areaColor = Skin.BlockedGui.AreaColor.ApplyOpacity(opacityRatio);
            graphics.FillRectangle(Skin.Brush(areaColor), area);

            // Okno s textem?
            if (this.HasBlockedGuiMessage)
            {
                this.BlockedGuiCheckTexts(graphics, area);

                // Vykreslit pozadí:
                Rectangle bounds = this.BlockedGuiMsgBackgroundBounds.Value;
                Color backColor = Skin.BlockedGui.TextBackColor.ApplyOpacity(opacityRatio);
                GPainter.DrawAreaBase(graphics, bounds, backColor, Orientation.Horizontal, GInteractiveState.MouseOver);

                // Vykreslit texty jednotlivých řádků:
                Color textColor = Skin.BlockedGui.TextInfoForeColor.ApplyOpacity(opacityRatio);
                foreach (BlockedGuiTextInfo text in this.BlockedGuiMsgTextInfos)
                    GPainter.DrawString(graphics, text.TextBounds, text.Text, textColor, text.Font, ContentAlignment.MiddleCenter);
            }
        }
        /// <summary>
        /// BlockGui animátor běží?
        /// </summary>
        protected bool BlockGuiAnimatorRunning { get; private set; }
        /// <summary>
        /// Ratio pro řízení Opacity v rozhraní BlockedGui:
        /// 0f = není vidět ... až ... 1f = má plné cílové hodnoty Alpha kanálu
        /// </summary>
        protected float? BlockGuiOpacityRatio { get; private set; }
        /// <summary>
        /// Instance animátoru pro rozsvícení a zhasnutí "okna", které blokuje GUI.
        /// </summary>
        protected AnimationControl<float> BlockGuiAnimator { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud GUI tohoto controlu je aktuálně blokováno.
        /// </summary>
        internal bool IsGUIBlocked { get; private set; }
        /// <summary>
        /// Text hlášky zobrazené po dobu blokace GUI
        /// </summary>
        protected string BlockedGuiMessage { get; private set; }
        /// <summary>
        /// true pokud je aktuálně blokované GUI <see cref="IsGUIBlocked"/> a je zadána hláška k zobrazení <see cref="BlockedGuiMessage"/>.
        /// </summary>
        protected bool HasBlockedGuiMessage { get { return this.IsGUIBlocked && !String.IsNullOrEmpty(this.BlockedGuiMessage); } }
        /// <summary>
        /// Kurzor platný před zahájením blokace GUI
        /// </summary>
        protected Cursor BlockedGuiCursor { get; private set; }
        /// <summary>
        /// Datum a čas, kdy končí blokování GUI vlivem timeoutu
        /// </summary>
        protected DateTime? BlockedTimeEnd { get; private set; }
        /// <summary>
        /// Jednotlivé řádky textu pro BlockedGUI message
        /// </summary>
        protected BlockedGuiTextInfo[] BlockedGuiMsgTextInfos { get; private set; }
        /// <summary>
        /// Souřadnice textu BlockedGUI message
        /// </summary>
        protected Rectangle? BlockedGuiMsgBackgroundBounds { get; private set; }
        /// <summary>
        /// Informace o textu, fontu, velikosti a umístění
        /// </summary>
        protected class BlockedGuiTextInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="text"></param>
            /// <param name="font"></param>
            public BlockedGuiTextInfo(string text, FontInfo font)
            {
                this.Text = text;
                this.Font = font;
            }
            /// <summary>
            /// Text
            /// </summary>
            public string Text { get; private set; }
            /// <summary>
            /// Font
            /// </summary>
            public FontInfo Font { get; private set; }
            /// <summary>
            /// Velikost textu v daném fontu
            /// </summary>
            public Size TextSize { get; set; }
            /// <summary>
            /// Souřadnice textu
            /// </summary>
            public Rectangle TextBounds { get; set; }
        }
        /// <summary>
        /// Zajistí zavření okna
        /// </summary>
        internal void CloseForm()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(this.CloseForm));
            }
            else
            {
                Form form = this.FindForm();
                if (form != null)
                    form.Close();
            }
        }
        #endregion
        #region Provádění interaktivního procesu, příznaky aktuálního zpracování interaktivního procesu
        /// <summary>
        /// Metoda zajistí provedení daných akcí pro danou změnu stavu.
        /// Současně poskytuje error handling (try - catch - trace).
        /// Akce action1 se provede jen tehdy, pokud na začátku není zpracováván žádný proces (<see cref="InteractiveProcessing"/> je false).
        /// Akce action2 se provede vždy.
        /// </summary>
        /// <param name="changeState"></param>
        /// <param name="action1">Akce 1, provádí se pouze pokud aktuálně neběží jiná interaktivní akce a pokud není blokováno GUI. Typicky je to volání zdejší výkonné metody.</param>
        /// <param name="action2">Akce 2, provádí se vždy. Typicky je to volání base metody.</param>
        /// <param name="final">Akce finální, volá se i po chybě v akcích 1 a 2.</param>
        protected void InteractiveAction(GInteractiveChangeState changeState, Action action1, Action action2, Action final = null)
        {
            try
            {
                this._FlowToolTipData = null;
                bool isProcessing = this.InteractiveProcessing;
                bool isBlocked = this.IsGUIBlocked;
                using (new InteractiveProcessingScope(this, changeState))
                {
                    if (!isBlocked && !isProcessing)
                    {   // Akce 1 se provádí jen na neblokovaném GUI, které aktuálně nezpracovává jinou akci:
                        if (action1 != null) action1();
                    }
                    if (action2 != null) action2();
                }
            }
            catch (Exception exc)
            {
                App.Trace.Exception(exc, "InteractiveAction error; changeState = " + changeState.ToString());
            }
            finally
            {
                if (final != null)
                    final();
            }
        }
        /// <summary>
        /// Obsahuje false v běžném (mrtvém) stavu, obsahuje true pokud právě probíhá obsluha jakékoli interaktivní události
        /// </summary>
        protected bool InteractiveProcessing { get { return this._InteractiveProcessing; } set { this._InteractiveProcessing = value; } } private bool _InteractiveProcessing;
        /// <summary>
        /// Akce, která se právě zpracovává
        /// </summary>
        protected GInteractiveChangeState? InteractiveProcessingAction { get; set; }
        /// <summary>
        /// Třída zajišťující scope zpracování jedné události v controlu <see cref="GInteractiveControl"/>.
        /// Po dobu tohoto scope je nastaveno <see cref="GInteractiveControl.InteractiveProcessing"/> = true.
        /// </summary>
        protected class InteractiveProcessingScope : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="control"></param>
            public InteractiveProcessingScope(GInteractiveControl control) : this(control, GInteractiveChangeState.None)
            { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="control"></param>
            /// <param name="state"></param>
            public InteractiveProcessingScope(GInteractiveControl control, GInteractiveChangeState state)
            {
                this._Control = control;
                this._OldProcessing = control.InteractiveProcessing;
                this._OldProcessingAction = control.InteractiveProcessingAction;
                this._Control.InteractiveProcessing = true;
                this._Control.InteractiveProcessingAction = state;
            }
            private GInteractiveControl _Control;
            private bool _OldProcessing;
            private GInteractiveChangeState? _OldProcessingAction;
            void IDisposable.Dispose()
            {
                this._Control.InteractiveProcessingAction = this._OldProcessingAction;
                this._Control.InteractiveProcessing = this._OldProcessing;
                this._Control = null;
            }
        }
        #endregion
        #region Implementace IInteractiveParent : on totiž GInteractiveControl je umístěn jako Parent ve svých IInteractiveItem
        UInt32 IInteractiveParent.Id { get { return 0; } }
        GInteractiveControl IInteractiveParent.Host { get { return this; } }
        IInteractiveParent IInteractiveParent.Parent { get { return null; } set { } }
        Size IInteractiveParent.ClientSize { get { return this.ClientSize; } }
        void IInteractiveParent.Repaint() { this.Repaint(); }
        void IInteractiveParent.Repaint(GInteractiveDrawLayer repaintLayers) { this.Repaint(repaintLayers); }
        #endregion
    }
    /// <summary>
    /// Stavy procesu Drag na základě pohybu myši a stavu controlu
    /// </summary>
    internal enum MouseMoveDragState
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
        /// a aplikace se rozhodla pro přetahování určitého objektu pomocí myši (DragMove : Drag and Drop).
        /// </summary>
        DragMove,
        /// <summary>
        /// Myš je zmáčknutá a pohybuje se, již je mimo startovní prostor,
        /// a aplikace se rozhodla pro rámování části prostoru pomocí myši a následné selectování vhodných objektů.
        /// </summary>
        DragFrame
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
            this.CurrentStateFill(mouseAbsolutePoint);
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
        /// Souřadný systém aktivního prvku <see cref="ActiveItem"/>.
        /// Souřadný systém obsahuje údaje o souřadnici prvku relativní i absolutní.
        /// Pokud <see cref="HasItem"/> je false, pak <see cref="ActiveItemBoundsInfo"/> je null.
        /// </summary>
        public BoundsInfo ActiveItemBoundsInfo { get { return (this.HasItem ? this.Item.BoundsInfo : null); } }
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
        #endregion
        #region CurrentState
        /// <summary>
        /// Metoda naplní hodnoty <see cref="CurrentMouseRelativePoint"/>, <see cref="CurrentTime"/>, <see cref="CurrentModifierKeys"/>.
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        internal void CurrentStateFill(Point? mouseAbsolutePoint)
        {
            this.CurrentMouseRelativePoint = (mouseAbsolutePoint.HasValue ? this.GetRelativePointToCurrentItem(mouseAbsolutePoint.Value) : (Point?)null);
            this.CurrentTime = DateTime.Now;
            this.CurrentModifierKeys = Control.ModifierKeys;
        }
        /// <summary>
        /// Aktuální relativní pozice myši
        /// </summary>
        public Point? CurrentMouseRelativePoint { get; protected set; }
        /// <summary>
        /// Čas právě probíhajícího eventu
        /// </summary>
        public DateTime CurrentTime { get; protected set; }
        /// <summary>
        /// Aktuálně stisknuté modifikační klávesy
        /// </summary>
        public Keys CurrentModifierKeys { get; protected set; }
        #endregion
        #region Property from this.CurrentItem.Style, IsEnabled, IsVisible
        /// <summary>
        /// Obsahuje true pokud aktuální prvek má nastaveno (<see cref="InteractiveProperties.Selectable"/> == true), 
        /// tzn. je to prvek, který umožňuje provádět změnu <see cref="IInteractiveItem.IsSelected"/> pomocí LeftClick
        /// </summary>
        internal bool ItemIsSelectable { get { return (this.HasItem && this.ActiveItem.Is.Selectable); } }
        /// <summary>
        /// Obsahuje true pokud aktuální prvek má nastaveno (<see cref="InteractiveProperties.SelectParent"/> == true), 
        /// tzn. je to prvek, který umožňuje provádět "na sobě" výběr svých Childs pomocí MouseFrame selectování
        /// </summary>
        internal bool ItemIsSelectParent { get { return (this.HasItem && this.ActiveItem.Is.SelectParent); } }
        /// <summary>
        /// Najde a vrátí režim, kterým se bude řešit aktuálně začínající proces MouseDrag.
        /// Najde nejbližší prvek v hierarchii, který je ochoten provádět nějakou vhodnou akci.
        /// </summary>
        /// <returns></returns>
        internal MouseMoveDragState MouseDragStartDetect()
        {
            MouseMoveDragState result = MouseMoveDragState.None;
            if (!this.HasItem) return result;

            // a) najít ve směru k Parentům vhodná prvek, který je ochotný něco dělat:
            int lastIndex = this.Count - 1;
            int foundIndex = -1;
            for (int i = lastIndex; i >= 0; i--)
            {   // Projdu prvky od toho navrchu (poslední index v this.Items) směrem k Parentům:
                IInteractiveItem item = this.Items[i].Item;
                if (item.Is.Enabled)
                {
                    if (item.Is.MouseDragMove)
                        result = MouseMoveDragState.DragMove;
                    else if (item.Is.SelectParent)
                        result = MouseMoveDragState.DragFrame;
                }
                if (result != MouseMoveDragState.None)
                {   // Něco jsme našli:
                    foundIndex = i;
                    break;
                }
            }
            if (foundIndex < 0) return result;

            // Našli jsme prvek na indexu (foundIndex), který je ochoten provádět akci (result):

            // b) pokud nalezený prvek je hlouběji v hierarchii (tj. není to Top prvek, ale některý z jeho Parentů) => pak nalezený prvek musíme aktivovat:
            if (foundIndex < lastIndex)
            {
                int count = foundIndex + 1;
                this.Item = this.Items[foundIndex];
                this.Items = this.Items.Take(count).ToArray();
                this.Count = count;
                this.HasItem = true;
            }

            return result;
        }
        /// <summary>
        /// Style of current item contain Mouse?
        /// </summary>
        public bool CanMouse { get { return (this.HasItem && this.ActiveItem.Is.MouseActive); } }
        /// <summary>
        /// Style of current item contain Click?
        /// </summary>
        public bool CanClick { get { return (this.HasItem && this.ActiveItem.Is.MouseClick); } }
        /// <summary>
        /// Style of current item contain LongClick?
        /// </summary>
        public bool CanLongClick { get { return (this.HasItem && this.ActiveItem.Is.MouseLongClick); } }
        /// <summary>
        /// Style of current item contain DoubleClick?
        /// </summary>
        public bool CanDoubleClick { get { return (this.HasItem && this.ActiveItem.Is.MouseDoubleClick); } }
        /// <summary>
        /// Style of current item contain Drag and is Enabled?
        /// </summary>
        public bool CanDrag { get { return (this.HasItem && this.ActiveItem.Is.MouseDragMove && this.IsEnabled); } }
        /// <summary>
        /// Style of current item contain CallMouseOver?
        /// </summary>
        public bool CanOver { get { return (this.HasItem && this.ActiveItem.Is.MouseMoveOver); } }
        /// <summary>
        /// Style of current item contain KeyboardInput?
        /// </summary>
        public bool CanKeyboard { get { return (this.HasItem && this.ActiveItem.Is.KeyboardInput); } }
        /// <summary>
        /// Current item is not null and IsEnabled?
        /// </summary>
        public bool IsEnabled { get { return (this.HasItem && this.ActiveItem.Is.Enabled); } }
        /// <summary>
        /// Current item is not null and IsVisible?
        /// </summary>
        public bool IsVisible { get { return (this.HasItem && this.ActiveItem.Is.Visible); } }
        #endregion
        #region Základní metody: FindItemAtPoint(), MapExchange()
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point.
        /// Prefered current item above other items.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="hostSize"></param>
        /// <param name="items"></param>
        /// <param name="prevItem"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="priorityItems"></param>
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
        /// <param name="hostSize"></param>
        /// <param name="itemList"></param>
        /// <param name="prevItem"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="priorityItems"></param>
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
        /// <param name="hostSize"></param>
        /// <param name="items"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="holdItems"></param>
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
                    if (fip.Item.Is.HoldMouse)
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
        /// prvek musí být <see cref="InteractiveProperties.Visible"/>, musí být <see cref="InteractiveProperties.Enabled"/> nebo musí být povoleno vyhledání i Disabled prvků,
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
                    && item.Is.Visible
                    && (withDisabled || item.Is.Enabled)
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
        /// <para/>
        /// Výstupní pole "leaveList" obsahuje prvky od posledního (v seznamu prev.Items) směrem k prvnímu, který je odlišný od prvků v poli next.Items.
        /// Výstupní pole "enterList" obsahuje prvky od prvního odlišného (v seznamu next.Items) směrem k poslednímu.
        /// <para/>
        /// Pokud jsou oba vstupní objekt (prev a next) rovny null, pak oba výstupní seznamy (leaveList i enterList) obsahují 0 prvků, ale nikdy nejsou null.
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <param name="leaveList"></param>
        /// <param name="enterList"></param>
        public static void MapExchange(GActivePosition prev, GActivePosition next, out List<GActiveItem> leaveList, out List<GActiveItem> enterList)
        {
            leaveList = new List<GActiveItem>();
            enterList = new List<GActiveItem>();

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
                leaveList.Add(prev.Items[i]);

            // Událost MouseEnter se volá od prvku na indexu "firstDiffIdx" až do posledního prvku v poli "next":
            for (int i = firstDiffIdx; i <= nextLastIdx; i++)
                enterList.Add(next.Items[i]);
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
        /// <param name="hostSize"></param>
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
                    if (!currentItem.Is.Visible || !currentItem.Is.Enabled) continue;

                    currentBoundsInfo.CurrentItem = currentItem;
                    Rectangle currentItemBounds = currentBoundsInfo.CurrentAbsVisibleBounds;
                    if (!frameBounds.IntersectsWith(currentItemBounds)) continue;

                    if ((!hasFilterScan || (hasFilterScan && filterScan(currentItem, currentItemBounds))))
                    {   // Scanovat prvek do hloubky:
                        IEnumerable<IInteractiveItem> currentChilds = currentItem.Childs;
                        if (currentChilds != null)
                            scanQueue.Enqueue(new Tuple<BoundsInfo, IEnumerable<IInteractiveItem>>(currentBoundsInfo.CurrentChildsSpider, currentChilds));
                    }

                    if (!hasFilterAccept || (hasFilterAccept && filterAccept(currentItem, currentItemBounds)))
                        // Akceptovat prvek do výběru:
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
