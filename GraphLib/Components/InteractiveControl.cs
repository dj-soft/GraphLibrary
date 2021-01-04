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
    #region class InteractiveControl : Jediný používaný interaktivní WinForm control, který se používá pro zobrazení interaktivních dat
    /// <summary>
    /// InteractiveControl : Jediný používaný interaktivní WinForm control, který se používá pro zobrazení interaktivních dat
    /// </summary>
    public partial class InteractiveControl : ControlLayered, IInteractiveParent, IInteractiveHost
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InteractiveControl()
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
            this.InteractiveLock = new object();
            this._ItemsList = new List<IInteractiveItem>();
            this.SetStyle(ControlStyles.Selectable | ControlStyles.UserMouse | ControlStyles.ResizeRedraw, true);
            this._StopWatchInit();
            this._ToolTipInit();
            this._ProgressInit();
            this._DrawInit();
            this._KeyboardEventsInit();
            this._MouseEventsInit();
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
        /// <summary>
        /// Zde potomek deklaruje souhrn svých prvků, z nichž se bude vypočítávat obsazená velikost.
        /// Vrací obsah property <see cref="ItemsList"/>.
        /// </summary>
        protected override IEnumerable<IInteractiveItem> ChildItems { get { return this.ItemsList; } }
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
        /// Přidá jeden interaktivní control do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="ControlLayered.Draw()"/>
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(IInteractiveItem item)
        {
            this._AddItem(item);
        }
        /// <summary>
        /// Přidá dané interaktivní controly do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="ControlLayered.Draw()"/>
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(params IInteractiveItem[] items)
        {
            foreach (IInteractiveItem item in items)
                this._AddItem(item);
        }
        /// <summary>
        /// Přidá dané interaktivní controly do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="ControlLayered.Draw()"/>
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
        /// Přidá dané interaktivní controly do <see cref="Items"/>. Nespouští vykreslení controlu <see cref="ControlLayered.Draw()"/>
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
            this._FocusedItem = null;
            this._FocusedItemKeyTarget = null;
            this._FocusedItemPrevious = null;
            this._CurrentKeyboardState = GInteractiveChangeState.None;
        }
        #region Obsluha override metod (z WinForm.Control) pro Focus a Keyboard
        /// <summary>
        /// Řízení vstupuje do controlu.
        /// Tato událost nastává pouze při příchodu z jiného Controlu ve stejném Formu. Nenastává při přepínání oken, pokud this okno má aktivní stále stejný (=this) Control.
        /// Na rozdíl od toho událost GotFocus() nastává i při návratu Focusu do tohoto Controlu po přepnutí z jiné aplikace
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusEnter, () => this._OnEnter(e, ref userData), () => base.OnEnter(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Provede OnEnter
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnEnter(EventArgs e, ref object userData)
        {
            bool runFinal = false;
            IInteractiveItem item = _OnEnterSearchFirstItem();
            if (item != null)
            {   // Zajistí umístění Focusu do prvku, který měl Focus při Leave nebo který je první/poslední v pořadí TabIndex:
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "Enter", ""))
                {
                    this._InteractiveDrawInit(null);
                    this._ItemKeyboardExchange(null, item, false, ref userData);
                    runFinal = true;
                }
            }
            return runFinal;
        }
        /// <summary>
        /// Vrátí prvek (<see cref="IInteractiveItem"/>), do něhož má jít Focus při vstupu focusu do this Controlu.
        /// Akceptuje stisknutou myš i předešlý prvek, který byl aktivní před odchodem focusu z Controlu.
        /// </summary>
        /// <returns></returns>
        private IInteractiveItem _OnEnterSearchFirstItem()
        {
            // Pokud vstupujeme stiskem myši, pak vrátíme Last prvek, ale myš si může najít svůj prvek (na který klikla) a ten si pak aktivuje:
            bool isMouse = (Control.MouseButtons != MouseButtons.None);
            if (isMouse && _FocusedItemPrevious != null) return _FocusedItemPrevious;

            // Vstupujeme bez stisknuté myši => najdeme první nebo poslední (podle klávesy Shift) vhodný prvek s TabStop = true, a ten vrátíme:
            if (!isMouse)
            {
                bool isShift = (Control.ModifierKeys == Keys.Shift);
                Direction direction = (isShift ? Direction.Negative : Direction.Positive);
                IInteractiveItem nextFocusItem;
                if (InteractiveFocusManager.TryGetOuterFocusItem(this, direction, out nextFocusItem)) return nextFocusItem;
            }

            // Nouzová cesta (pro stisknutou myš anebo pro nenalezený interaktivní prvek) = aktivujeme posledně aktivní prvek:
            return _FocusedItemPrevious;
        }
        /// <summary>
        /// Focus vstupuje do controlu.
        /// Tato událost nastává při příchodu z jiného Controlu téhož okna, po doběhnutí OnEnter(); ale i při návratu Focusu do tohoto Controlu po přepnutí z jiné aplikace
        /// Při návratu řízení z jiného okna tedy proběhne pouze OnGotFocus(), bez předchozího OnEnter().
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusEnter, () => this._OnGotFocus(e, ref userData), () => base.OnGotFocus(e));
        }
        /// <summary>
        /// Provede OnGetFocus
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnGotFocus(EventArgs e, ref object userData)
        {
            return true;
        }
        /// <summary>
        /// Focus odchází z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusLeave, () => this._OnLostFocus(e, ref userData), () => base.OnLostFocus(e));
        }
        /// <summary>
        /// Akce OnLostFocus
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnLostFocus(EventArgs e, ref object userData)
        {
            return true;
        }
        /// <summary>
        /// Focus odchází z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardFocusLeave, () => this._OnLeave(e, ref userData), () => base.OnLeave(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Provede OnLostFocus
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnLeave(EventArgs e, ref object userData)
        {
            bool runFinal = false;
            this._FocusedItemPrevious = this._FocusedItem;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "Leave", ""))
            {
                this._InteractiveDrawInit(null);
                this._ItemKeyboardExchange(this._FocusedItem, null, true, ref userData);
                runFinal = true;
            }
            return runFinal;
        }
        /// <summary>
        /// Akce PreviewKeyDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyPreview, () => this._OnPreviewKeyDown(e, ref userData), () => base.OnPreviewKeyDown(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Provede OnPreviewKeyDown
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnPreviewKeyDown(PreviewKeyDownEventArgs e, ref object userData)
        {
            bool runFinal = false;
            IInteractiveItem item = _FocusedCurrentTarget;
            if (item != null)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "PreviewKeyDown", ""))
                {
                    this._InteractiveDrawInit(null);
                    this._ItemKeyboardCallEvent(item, GInteractiveChangeState.KeyboardKeyPreview, e, null, null, ref userData);
                    runFinal = true;
                }
            }
            return runFinal;
        }
        /// <summary>
        /// Akce KeyDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyDown, () => this._OnKeyDown(e, ref userData), () => base.OnKeyDown(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Provede OnKeyDown
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnKeyDown(KeyEventArgs e, ref object userData)
        {
            bool runFinal = false;
            this._MousePaintKeyPress(e);
            if (_OnKeyDownIsCancelProcess(e, ref userData))
            {
                runFinal = true;
            }
            IInteractiveItem item = _FocusedCurrentTarget;
            if (item != null)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "KeyDown", ""))
                {
                    this._InteractiveDrawInit(null);
                    this._ItemKeyboardCallEvent(item, GInteractiveChangeState.KeyboardKeyDown, null, e, null, ref userData);
                    runFinal = true;
                }
            }
            return runFinal;
        }
        /// <summary>
        /// Zjistí, zda aktuální kláves je Escape a aktuální stav je nějaký interaktivní proces (DragMove, DragFrame, Paint).
        /// Pokud ano, pak stav zruší a vrátí true = klávesa je vyřešena na úrovni Controlu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnKeyDownIsCancelProcess(KeyEventArgs e, ref object userData)
        {
            bool isProcessed = false;
            if (e.KeyCode == Keys.Escape && ((this._MouseDragState == MouseMoveDragState.DragMove && this._MouseDragMoveItem != null) || this._MouseDragState == MouseMoveDragState.DragFrame || this._MouseDragState == MouseMoveDragState.Paint))
            {   // When we have Dragged Item, and Escape is pressed, then perform Cancel for current Drag operation:
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "KeyDown_Escape_Cancel", ""))
                {
                    switch (this._MouseDragState)
                    {
                        case MouseMoveDragState.Paint:
                            this._MousePaintCancel(ref userData);
                            scope.AddItem("MousePaintCancel");
                            isProcessed = true;
                            break;
                        case MouseMoveDragState.DragMove:
                            this._MouseDragMoveCancel(ref userData);
                            scope.AddItem("MouseDragMoveCancel");
                            isProcessed = true;
                            break;
                        case MouseMoveDragState.DragFrame:
                            this._MouseDragFrameCancel(ref userData);
                            scope.AddItem("MouseDragFrameCancel");
                            isProcessed = true;
                            break;
                    }
                }
            }
            return isProcessed;
        }
        /// <summary>
        /// Akce KeyUp
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyUp, () => this._OnKeyUp(e, ref userData), () => base.OnKeyUp(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Provede OnKeyUp
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnKeyUp(KeyEventArgs e, ref object userData)
        {
            bool runFinal = false;
            IInteractiveItem item = _FocusedCurrentTarget;
            if (item != null)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "KeyUp", ""))
                {
                    this._InteractiveDrawInit(null);
                    this._ItemKeyboardCallEvent(item, GInteractiveChangeState.KeyboardKeyUp, null, e, null, ref userData);
                    runFinal = true;
                }
            }
            _FocusedItemKeyTarget = null;
            return runFinal;
        }
        /// <summary>
        /// Akce KeyPress
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.KeyboardKeyPress, () => this._OnKeyPress(e, ref userData), () => base.OnKeyPress(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Provede OnKeyPress
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnKeyPress(KeyPressEventArgs e, ref object userData)
        {
            bool runFinal = false;

            IInteractiveItem item = _FocusedCurrentTarget;
            if (item != null)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "KeyPress", ""))
                {
                    this._InteractiveDrawInit(null);
                    this._ItemKeyboardCallEvent(item, GInteractiveChangeState.KeyboardKeyPress, null, null, e, ref userData);
                    runFinal = true;
                }
            }
            return runFinal;
        }
        /// <summary>
        /// Prvek, který má aktuálně focus. Je nastaven po doběhnutí událostí <see cref="GInteractiveChangeState.KeyboardFocusEnter"/> a <see cref="GInteractiveChangeState.MouseEnter"/>.
        /// Setování hodnoty vyvolá metodu <see cref="SetFocusToItem(IInteractiveItem)"/> = korektně přenese focus do daného prvku.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IInteractiveItem FocusedItem { get { return this._FocusedItem; } set { SetFocusToItem(value); } }
        /// <summary>
        /// Prvek, který měl focus nedávno. Je nastaven po doběhnutí událostí <see cref="GInteractiveChangeState.KeyboardFocusLeave"/> a <see cref="GInteractiveChangeState.MouseLeave"/>, 
        /// v době kdy je do <see cref="IInteractiveHost.FocusedItem"/> vkládáno null.
        /// Slouží např. k obnově focusu při vrácení, nebo k určení, odkud přešel focus do určitého nového prvku.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IInteractiveItem FocusedItemPrevious { get { return this._FocusedItemPrevious; } }
        /// <summary>
        /// Zajistí nasměrování Focusu do prvního krajního prvku v daném směru, anebo do jeho nejbližšího Parenta, který pracuje s klávesnicí.
        /// </summary>
        /// <param name="direction"></param>
        public void SetFocusToOuterItem(Direction direction = Direction.Positive)
        {
            IInteractiveItem nextItem;
            if (InteractiveFocusManager.TryGetOuterFocusItem(this, direction, out nextItem))
                SetFocusToItem(nextItem);
        }
        /// <summary>
        /// Zajistí nasměrování Focusu do následujícího prvku v daném směru, anebo do jeho nejbližšího Parenta, který pracuje s klávesnicí.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="currentItem"></param>
        public void SetFocusToNextItem(Direction direction = Direction.Positive, IInteractiveItem currentItem = null)
        {
            if (currentItem == null) currentItem = FocusedItem;
            if (currentItem == null)
            {
                SetFocusToOuterItem(direction);
            }
            else
            {
                IInteractiveItem nextItem;
                if (InteractiveFocusManager.TryGetNextFocusItem(currentItem, direction, out nextItem))
                    SetFocusToItem(nextItem);
            }
        }
        /// <summary>
        /// Zajistí nasměrování Focusu do daného prvku (anebo do jeho nejbližšího Parenta, který pracuje s klávesnicí).
        /// Pokud je předán prvek null, pak provede LeaveFocus z aktuálního prvku, a focus se vizuálně ztratí (nebude prvek s klávesovým focusem).
        /// </summary>
        /// <param name="focusedItem"></param>
        public void SetFocusToItem(IInteractiveItem focusedItem)
        {
            // Pozor, pokud je nyní aktivní nějaká klávesa (tzn. jsme ve stavu KeyboardPreview, ..Down nebo ..Press), pak celý její proces (Press i Up) by měl doběhnout do PŮVODNÍHO prvku this.FocusedItem:
            if (_CurrentKeyboardState == GInteractiveChangeState.KeyboardKeyPreview || _CurrentKeyboardState == GInteractiveChangeState.KeyboardKeyDown || _CurrentKeyboardState == GInteractiveChangeState.KeyboardKeyPress)
                this._FocusedItemKeyTarget = FocusedItem;

            object userData = null;
            _ItemKeyboardExchange(_FocusedItem, focusedItem, true, ref userData);        // Tady proběhne KeyboardFocusLeave pro FocusedItem a poté KeyboardFocusEnter pro nový focusedItem.
        }
        #endregion
        #region Privátní výkonné metody pro podporu Focus a Keyboard
        /// <summary>
        /// Prvek s aktuálním focusem. Může být null.
        /// </summary>
        private IInteractiveItem _FocusedItem;
        /// <summary>
        /// Prvek, do kterého půjdou následující Keyboard eventy.
        /// Pokud je null (defaultní stav), půjdou události standardně do <see cref="_FocusedItem"/>.
        /// <para/>
        /// Používá se v době vynuceného přechodu focusu z prvku <see cref="_FocusedItemKeyTarget"/> do nového <see cref="_FocusedItem"/>, kdy tento přechod focusu je aktivován klávesou v původním prvku.
        /// Typicky: V TextBoxu 1 je deekován KeyDown = Enter, tím je vyvolána akce SetFocus... do TextBoxu 2 - ale stále je klávesa Enter stisknutá, přitom dojde ke změně focusu (Leave a Enter),
        /// ale nyní nechceme, aby událost KeyUp (Enter) došla do TextBoxu 2 = protože v něm neproběhl KeyDown, tato událost musí doběhnout do TextBoxu 1!
        /// <para/>
        /// Hodnota je nulována po dokončení KeyUp.
        /// </summary>
        private IInteractiveItem _FocusedItemKeyTarget;
        /// <summary>
        /// Do tohoto prvku půjdou klávesové eventy. Jde o prvek <see cref="_FocusedItemKeyTarget"/> (pokud je naplněn) anebo <see cref="_FocusedItem"/>. Může být NULL.
        /// </summary>
        private IInteractiveItem _FocusedCurrentTarget { get { return (_FocusedItemKeyTarget ?? _FocusedItem); } }
        /// <summary>
        /// true pokud <see cref="_FocusedItem"/> existuje (pak může dostávat klávesové eventy)
        /// </summary>
        private bool _FocusedItemExists { get { return (this._FocusedItem != null); } }
        /// <summary>
        /// Prvek s předchozím focusem. Může být null.
        /// </summary>
        private IInteractiveItem _FocusedItemPrevious;
        /// <summary>
        /// Vyvolá události prvků: KeyboardFocusLeave (pro itemPrev) a KeyboardFocusEnter (pro itemNext).
        /// Uloží dodaný prvek itemNext do 
        /// Set _KeyboardCurrentItem = gcItemPrev, when CanKeyboard.
        /// </summary>
        /// <param name="itemPrev"></param>
        /// <param name="itemNext"></param>
        /// <param name="forceLeave"></param>
        /// <param name="userData"></param>
        private void _ItemKeyboardExchange(IInteractiveItem itemPrev, IInteractiveItem itemNext, bool forceLeave, ref object userData)
        {
            // Změna klávesového focusu pošle událost KeyboardFocusLeave do prvku itemPrev a do jeho Parentů 
            //  až k parentovi, který je společný pro itemPrev i itemNext - ale do něj neposílá žádný event,
            //  a pak pošle event KeyboardFocusEnter do sestupujících Parentů k prvku itemNext a i do něj.

            // Cílový prvek může být "Non-Keyboard", zkusíme tedy najít jeho nejbližšího parenta, který umožňuje klávesovou aktivitu:
            itemNext = _ItemKeyboardSearchKeyboardInput(itemNext);

            // A dál je to jednoduché:
            bool existsPrev = (itemPrev != null && itemPrev.Is.KeyboardInput);
            bool existsNext = (itemNext != null && itemNext.Is.KeyboardInput);
            if (!existsPrev && !existsNext) return;                                                                    // booth is null (=paranoia)
            if (Object.ReferenceEquals((existsPrev ? itemPrev : null), (existsNext ? itemNext : null))) return;        // no change

            // Změna focusu se provede pokud je povinná (tedy včetně přechodu do itemNext = null), anebo pokud Next item existuje:
            if (forceLeave || existsNext)
            {
                var tree = InteractiveObject.SearchInteractiveItemsTree(itemPrev, itemNext);                           // Získáme přechodovu cestu z itemPrev nahoru přes společného parenta a pak dolů k itemNext
                foreach (var node in tree)
                {
                    if (node.Item2 is IInteractiveItem)
                    {
                        IInteractiveItem item = node.Item2 as IInteractiveItem;
                        switch (node.Item1)
                        {
                            case Direction.Negative:
                                // Prvky itemPrev a postupně jeho Parenti ke společnému Parentu (mimo něj):
                                this._ItemKeyboardCallEvent(item, GInteractiveChangeState.KeyboardFocusLeave, null, null, null, ref userData);
                                break;
                            case Direction.None:
                                // Toto je společný Parent, ten nedostane žádný event, protože pro něj se nic nemění!
                                break;
                            case Direction.Positive:
                                // Prvky Parenti k itemNext postupně až k itemNext:
                                this._ItemKeyboardCallEvent(item, GInteractiveChangeState.KeyboardFocusEnter, null, null, null, ref userData);
                                break;
                        }
                    }
                }

                this._FocusedItem = itemNext;
            }
        }
        /// <summary>
        /// Vrátí prvek (daný, nebo jeho parenta), jehož <see cref="InteractiveProperties.KeyboardInput"/> je true.
        /// Může vrátit null.
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
                // Tím vyloučím přechod na parenta, který je fyzický WinForm control InteractiveControl.
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
        /// <param name="userData"></param>
        private void _ItemKeyboardCallEvent(IInteractiveItem item, GInteractiveChangeState change, PreviewKeyDownEventArgs previewArgs, KeyEventArgs keyArgs, KeyPressEventArgs keyPressArgs, ref object userData)
        {
            this._CurrentKeyboardState = change;
            if (item.Is.KeyboardInput || (change == GInteractiveChangeState.KeyboardFocusEnter || change == GInteractiveChangeState.KeyboardFocusLeave))
            {
                GInteractiveChangeState realChange = change;
                GInteractiveState targetState = _GetStateAfterChange(realChange, item.Is.Enabled);
                BoundsInfo boundsInfo = BoundsInfo.CreateForChild(item);
                GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(boundsInfo, realChange, targetState, this.FindNewItemAtPoint, previewArgs, keyArgs, keyPressArgs);
                stateArgs.UserDragPoint = null;

                stateArgs.UserData = userData;
                item.AfterStateChanged(stateArgs);
                userData = stateArgs.UserData;

                Point? toolTipPoint = (stateArgs.HasToolTipData ? (Point?)(BoundsInfo.GetAbsoluteBounds(item).Location) : (Point?)null);
                this._InteractiveDrawStore(toolTipPoint, stateArgs);
            }
        }
        /// <summary>
        /// Aktuální stav klávesové aktivity = poslední, který se detekoval
        /// </summary>
        private GInteractiveChangeState _CurrentKeyboardState;
        #endregion
        #endregion
        #region Myš (události z WinForm Controlu, jejich řešení v InteractiveControl)
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
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.MouseEnter, () => this._OnMouseEnter(e, ref userData), () => base.OnMouseEnter(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseMove
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.MouseOver, () => this._OnMouseMove(e, ref userData), () => base.OnMouseMove(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.LeftDown, () => this._OnMouseDown(e, ref userData), () => base.OnMouseDown(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseUp
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.LeftUp, () => this._OnMouseUp(e, ref userData), () => base.OnMouseUp(e), () => { this._MouseDownReset(); this._InteractiveDrawRun(); });
        }
        /// <summary>
        /// Akce MouseWheel
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.WheelUp, () => this._OnMouseWheel(e, ref userData), () => base.OnMouseWheel(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Akce MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            object userData = null;
            this.InteractiveAction(GInteractiveChangeState.MouseLeave, () => this._OnMouseLeave(e, ref userData), () => base.OnMouseLeave(e), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Zajistí provedení téže akce, jako by myš vstoupila na control: Enter + Move, podle aktuálního stavu myši
        /// </summary>
        protected void OnMouseEnterMove(bool runDraw = false)
        {
            // Do této metody vstupuje řízení za různého stavu controlu, může být v podstatě i po skončení života controlu.
            // V takové situaci ale nechceme nic dělat:
            if (this.Disposing || this.IsDisposed) return;

            object userData = null;
            EventArgs arg1 = new EventArgs();
            MouseEventArgs arg2 = this.CreateMouseEventArgs();
            if (!runDraw)
                this.InteractiveAction(GInteractiveChangeState.MouseEnter, () => this._OnMouseEnter(arg1, ref userData), () => this._OnMouseMove(arg2, ref userData), null);
            else
                this.InteractiveAction(GInteractiveChangeState.MouseEnter, () => this._OnMouseEnter(arg1, ref userData), () => this._OnMouseMove(arg2, ref userData), () => this._InteractiveDrawRun());
        }
        /// <summary>
        /// Metoda vrátí argument typu <see cref="MouseEventArgs"/> pro this control, a aktuální stav myši, a explicitně dodaný počet Clicks a hodnotu Delta.
        /// </summary>
        /// <param name="clicks"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        protected MouseEventArgs CreateMouseEventArgs(int clicks = 0, int delta = 0)
        {
            MouseButtons mouseButtons = Control.MouseButtons;        // Stisknutá tlačítka myši
            Point mousePoint = Control.MousePosition;                // Souřadnice myši v koordinátech Screenu
            mousePoint = this.PointToClient(mousePoint);             //  -""- v koordinátech Controlu
            return new MouseEventArgs(mouseButtons, clicks, mousePoint.X, mousePoint.Y, delta);
        }
        /// <summary>
        /// Myš vstoupila na control
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private bool _OnMouseEnter(EventArgs e, ref object userData)
        {
            bool runFinal = true;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "MouseEnter", ""))
            {
                this._InteractiveDrawInit(null);
                this._MouseAllReset();
            }
            return runFinal;
        }
        /// <summary>
        /// Myš se pohybuje v prostoru controlu, možná bez stisknuté myši, možná se stisknutým tlačítkem...
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private bool _OnMouseMove(MouseEventArgs e, ref object userData)
        {
            bool runFinal = true;
            using (ITraceScope scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "MouseMove", ""))
            {
                this._InteractiveDrawInit(e);
                if (!this._MouseDownAbsolutePoint.HasValue && (e.Button == System.Windows.Forms.MouseButtons.Left || e.Button == System.Windows.Forms.MouseButtons.Right))
                {   // Nějak jsme zmeškali event MouseDown, a nyní máme event MouseMove bez připravených dat z MouseDown:
                    this._MouseFell(e, ref userData);
                    scope.AddItem("Missed: MouseFell!");
                }

                if (!this._MouseDownAbsolutePoint.HasValue)
                {   // Myš se pohybuje nad Controlem, ale žádný knoflík myši není zmáčknutý:
                    this._MouseOver(e, ref userData);
                    scope.Result = "MouseOver";
                }
                else
                {   // Myš má zmáčknutý nějaký čudlík, a pohybuje se => tam je možností vícero:
                    this._OnMouseDrag(e, ref userData, scope);
                }
            }
            return runFinal;
        }
        /// <summary>
        /// Myš je zmáčknutá a pohybuje se. Může to být Select, může to být Drag, může to být Nic...
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <param name="scope"></param>
        private void _OnMouseDrag(MouseEventArgs e, ref object userData, ITraceScope scope)
        {
            MouseMoveDragState dragState = this._GetMouseMoveDragState(e.Location);
            switch (dragState)
            {
                case MouseMoveDragState.Start:
                    // Nyní začíná pohyb myši mimo výchozí prostor, musíme rozhodnout co to bude:
                    this._MouseDragStartDetect(e, ref userData, scope);
                    break;
                case MouseMoveDragState.Paint:
                    this._MousePaintStep(e, ref userData);
                    scope.Result = "MousePaintStep";
                    break;
                case MouseMoveDragState.DragMove:
                    // Nyní probíhá rutinní Drag:
                    this._MouseDragMoveStep(e, ref userData);
                    scope.Result = "MouseDragMoveStep";
                    break;
                case MouseMoveDragState.DragFrame:
                    // Nyní probíhá rutinní Frame:
                    this._MouseDragFrameStep(e, ref userData);
                    scope.Result = "MouseDragFrameStep";
                    break;
                case MouseMoveDragState.CallItem:
                    this._MouseOver(e, ref userData);
                    break;
            }
        }
        /// <summary>
        /// Myš je zmáčknutá a pohybuje se. Může to být Select, může to být Drag, může to být Nic...
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <param name="scope"></param>
        private void _MouseDragStartDetect(MouseEventArgs e, ref object userData, ITraceScope scope)
        {
            ActivePositionInfo mouseCurrentItem = this._CurrentActiveItem;

            MouseMoveDragState dragMode = mouseCurrentItem.MouseDragStartDetect();
            switch (dragMode)
            {
                case MouseMoveDragState.Paint:
                    // Na tomto prvku začíná proces Paint; ale do této metody bychom se v tomto stavu dostávat asi neměli...
                    scope.Result = "MouseDragPaintBegin";
                    break;

                case MouseMoveDragState.DragMove:
                    // Našli jsme nějaký prvek, který je ochotný s sebou nechat vláčet (v jeho aktuálním stavu podporuje Drag and Drop):
                    // Myš se právě nyní pohnula z "Silent zone" (oblast okolo místa, kde byla myš zmáčknuta) => Drag and Drop začíná:
                    this._MouseDragState = MouseMoveDragState.DragMove;
                    this._MouseDragMoveBegin(e, ref userData);
                    this._MouseDragMoveStep(e, ref userData);
                    scope.Result = "MouseDragMoveBegin";
                    break;

                case MouseMoveDragState.DragFrame:
                    // Nalezený prvek podporuje Select pomocí Frame (tzn. má vlastnost <see cref="IInteractiveItem.IsSelectParent"/>)
                    this._MouseDragState = MouseMoveDragState.DragFrame;
                    this._MouseDragFrameBegin(e, ref userData);
                    this._MouseDragFrameStep(e, ref userData);
                    scope.Result = "MouseDragFrameBegin";
                    break;

                case MouseMoveDragState.CallItem:
                    this._MouseDragState = MouseMoveDragState.CallItem;
                    this._MouseOver(e, ref userData);
                    scope.Result = "MouseDragCallItem";
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

            // Pokud se control již dříve rozhodl o stavu, pak jej vrátíme bez dalšího zkoumání:
            MouseMoveDragState state = this._MouseDragState;
            if (state == MouseMoveDragState.Paint || state == MouseMoveDragState.DragMove || state == MouseMoveDragState.DragFrame || state == MouseMoveDragState.CallItem) return state;

            // Dosud nebylo o stavu rozhodnuto, detekujeme pozici myši vzhledem k výchozímu bodu:
            Rectangle? startBounds = this._MouseDragStartBounds;
            if (startBounds.HasValue && startBounds.Value.Contains(mousePoint))          // Pozice myši je stále uvnitř Silent zone => čekáme na nějaký větší pohyb
                return MouseMoveDragState.Wait;

            return MouseMoveDragState.Start;
        }
        /// <summary>
        /// Provede OnMouseDown
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnMouseDown(MouseEventArgs e, ref object userData)
        {
            bool runFinal = true;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "MouseDown", ""))
            {
                this._InteractiveDrawInit(e);
                this._MouseFell(e, ref userData);
            }
            return runFinal;
        }
        /// <summary>
        /// Provede OnMouseUp
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnMouseUp(MouseEventArgs e, ref object userData)
        {
            bool runFinal = true;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "MouseUp", ""))
            {
                this._InteractiveDrawInit(e);
                if (!this._CurrentMouseDragCanceled)
                {
                    switch (this._MouseDragState)
                    {
                        case MouseMoveDragState.Paint:
                            this._MousePaintDone(e, ref userData);
                            break;
                        case MouseMoveDragState.DragMove:
                            this._MouseDragMoveDone(e, ref userData);
                            break;
                        case MouseMoveDragState.DragFrame:
                            this._MouseDragFrameDone(e, ref userData);
                            break;
                        case MouseMoveDragState.CallItem:
                            this._MouseRaise(e, ref userData);
                            break;
                        default:
                            this._MouseRaise(e, ref userData);
                            break;
                    }
                }
            }
            return runFinal;
        }
        /// <summary>
        /// Provede OnMouseWheel
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnMouseWheel(MouseEventArgs e, ref object userData)
        {
            bool runFinal = true;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "MouseWheel", ""))
            {
                this._InteractiveDrawInit(e);
                this._MouseOneWheel(e, ref userData);
                this._OnMouseMove(e, ref userData);            // Poté, co se provedla akce MouseWheel nasimulujeme ještě akci MouseMove, protože se mohl pohnout obraz controlu
            }
            return runFinal;
        }
        /// <summary>
        /// Zajistí provedení téže akce, jako by myš opustila control
        /// </summary>
        private bool _OnMouseLeave(ref object userData)
        {
            bool runFinal = true;
            EventArgs e = new EventArgs();
            object usrData = userData;           // Vstupní ref parametr nelze použít jako ref parametr v lambda metodě:
            this.InteractiveAction(GInteractiveChangeState.MouseLeave, () => this._OnMouseLeave(e, ref usrData), null, () => this._InteractiveDrawRun());
            userData = usrData;
            return runFinal;
        }
        /// <summary>
        /// Provede OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private bool _OnMouseLeave(EventArgs e, ref object userData)
        {
            bool runFinal = true;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "MouseLeave", ""))
            {
                this._InteractiveDrawInit(null);
                this._MouseOver(null, ref userData);
                this._MouseAllReset();
                this._MouseDragStoreActiveItem(null);
            }
            return runFinal;
        }
        #endregion
        #region Řízení konkrétních aktivit myši, již rozčleněné; volání jednoduchých interaktivních metod (Over, Fell, Raise, Whell)
        /// <summary>
        /// Tato akce se volá výhradně když se myš pohybuje bez Drag and Drop = bez stisknutého tlačítka
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MouseOver(MouseEventArgs e, ref object userData)
        {
            ActivePositionInfo oldActiveItem = this._CurrentActiveItem;
            if (e != null)
            {   // Standardní pohyb myši nad Controlem:
                ActivePositionInfo newActiveItem = this._FindActivePositionAtPoint(e.Location, true);
                this._ItemMouseExchange(oldActiveItem, newActiveItem, this._MouseCurrentRelativePoint, ref userData);
                this._MousePaintMove(e, ref userData, newActiveItem);
                this._ToolTipMouseMove(this._MouseCurrentAbsolutePoint);
            }
            else
            {   // MouseLeave z tohoto Controlu:
                this._MouseCurrentRelativePoint = null;
                this._ItemMouseExchange(oldActiveItem, null, null, ref userData);
                this._ActivateCursor(SysCursorType.Default);
            }
        }
        /// <summary>
        /// Tato akce se provede po stisknutí myši v prostoru controlu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MouseFell(MouseEventArgs e, ref object userData)
        {
            MouseButtons mouseButtons = e.Button;
            bool mouseButtonsLeft = (mouseButtons == MouseButtons.Left);

            ActivePositionInfo oldActiveItem = this._CurrentActiveItem;
            ActivePositionInfo newActiveItem = this._FindActivePositionAtPoint(e.Location, false);
            newActiveItem.CurrentStateFill(e.Location);
            this._ItemMouseExchange(oldActiveItem, newActiveItem, this._MouseCurrentRelativePoint, ref userData);
            this._ItemKeyboardExchange(this._FocusedItem, newActiveItem.ActiveItem, false, ref userData);

            this._MouseDownAbsolutePoint = e.Location;
            this._MouseDownTime = DateTime.Now;
            this._MouseDownButtons = e.Button;
            this._MouseDragStartBounds = e.Location.CreateRectangleFromCenter(this._DragStartSize);
            this._MouseDragMoveItemOffset = null;
            this._ItemMouseCallStateChangedEvent(newActiveItem, GInteractiveChangeState.LeftDown, ref userData, newActiveItem.CurrentMouseRelativePoint);

            if (mouseButtonsLeft && newActiveItem.ItemIsSelectable)
                this._ItemMouseLeftDownUnSelect(newActiveItem, ref userData);

            this._MousePaintDown(e, ref userData, newActiveItem);
        }
        /// <summary>
        /// Voláno při zvednutí tlačítka myši (=konec kliknutí), v situaci kdy NEPROBÍHALA žádná akce "Drag and Drop", ani "Drag and Frame". 
        /// Jde o prosté kliknutí na místě.
        /// Zde se řeší: DoubleClick, LongClick, LeftClickSelect, Click, Kontextové menu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MouseRaise(MouseEventArgs e, ref object userData)
        {
            ActivePositionInfo oldActiveItem = this._CurrentActiveItem;
            MouseButtons mouseButtons = (this._MouseDownButtons.HasValue ? this._MouseDownButtons.Value : MouseButtons.None);
            bool mouseButtonsLeft = (mouseButtons == MouseButtons.Left);
            bool mouseButtonsRight = (mouseButtons == MouseButtons.Right);
            Keys modifierKeys = Control.ModifierKeys;
            if (oldActiveItem != null)
            {
                oldActiveItem.CurrentStateFill(e.Location);

                this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftUp, ref userData, oldActiveItem.CurrentMouseRelativePoint);
                if (oldActiveItem.CanDoubleClick && ActivePositionInfo.IsDoubleClick(this._MouseClickedItem, oldActiveItem))
                {   // Double click:
                    this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftDoubleClick, ref userData, oldActiveItem.CurrentMouseRelativePoint);
                }
                else if (oldActiveItem.CanLongClick && ActivePositionInfo.IsLongClick(this._MouseDownTime, oldActiveItem))
                {   // Long click:
                    this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftLongClick, ref userData, oldActiveItem.CurrentMouseRelativePoint);
                    if (mouseButtonsLeft || mouseButtonsRight)
                        this._ItemMouseCallContextMenu(oldActiveItem, GInteractiveChangeState.LeftLongClick, ref userData, oldActiveItem.CurrentMouseRelativePoint);
                }
                else if (oldActiveItem.CanClick)
                {   // Single click:
                    if (mouseButtonsLeft && oldActiveItem.ItemIsSelectable)
                        this._ItemMouseLeftClickSelect(oldActiveItem, ref userData, modifierKeys);
                    this._ItemMouseCallStateChangedEvent(oldActiveItem, GInteractiveChangeState.LeftClick, ref userData, oldActiveItem.CurrentMouseRelativePoint);
                    if (mouseButtonsRight)
                        this._ItemMouseCallContextMenu(oldActiveItem, GInteractiveChangeState.LeftClick, ref userData, oldActiveItem.CurrentMouseRelativePoint);
                }
                this._MouseClickedItem = oldActiveItem;
            }
        }
        /// <summary>
        /// Kolečko myši
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MouseOneWheel(MouseEventArgs e, ref object userData)
        {
            ActivePositionInfo oldActiveItem = this._CurrentActiveItem;
            ActivePositionInfo newActiveItem = this._FindActivePositionAtPoint(e.Location, false);
            newActiveItem.CurrentStateFill(e.Location);
            this._ItemMouseExchange(oldActiveItem, newActiveItem, this._MouseCurrentRelativePoint, ref userData);
            GInteractiveChangeState change = (e.Delta > 0 ? GInteractiveChangeState.WheelUp : GInteractiveChangeState.WheelDown);
            this._ItemMouseCallStateChangedEvent(newActiveItem, change, ref userData, newActiveItem.CurrentMouseRelativePoint, recurseToSolver: true);   // recurseToSolver = true => opakovat volání akce, dokud ji některý prvek v hierarchii nevyřeší.
        }
        #endregion
        #region Řízení procesu MouseDragMove = přesouvání prvku Drag and Drop
        private void _MouseDragMoveBegin(MouseEventArgs e, ref object userData)
        {
            // Relativní pozice myši v okamžiku MouseDown, nikoli aktuální pozice (ta už je přesunutá = mimo prostor _CurrentMouseDragStart):
            ActivePositionInfo mouseCurrentItem = this._CurrentActiveItem;
            Point downPoint = this._MouseDownAbsolutePoint.Value;
            this._MouseCurrentRelativePoint = _GetRelativePoint(downPoint, mouseCurrentItem);
            if (mouseCurrentItem.CanDrag)
            {
                mouseCurrentItem.CurrentStateFill(downPoint);
                this._MouseDragMoveItem = mouseCurrentItem;
                this._MouseDragMoveItemOriginBounds = mouseCurrentItem.ActiveItem.Bounds;
                var stateArgs = this._ItemMouseCallStateChangedEvent(mouseCurrentItem, GInteractiveChangeState.LeftDragMoveBegin, ref userData, this._MouseCurrentRelativePoint);
                if (stateArgs.UserDragPoint.HasValue)
                    this._UserDragPointOffset = stateArgs.UserDragPoint.Value.Sub(downPoint);
            }
            this._MouseDragMoveItemOffset = this._GetRelativePointToCurrentItem(downPoint);
            this._MouseDragStartBounds = null;
        }
        private void _MouseDragMoveStep(MouseEventArgs e, ref object userData)
        {
            this._CurrentTargetItem = this._FindActivePositionAtPoint(e.Location, false);
            ActivePositionInfo mouseSourceItem = this._CurrentActiveItem;
            ActivePositionInfo mouseTargetItem = this._CurrentTargetItem;
            if (this._MouseDragMoveItem != null && mouseSourceItem.CanDrag)
            {
                mouseSourceItem.CurrentStateFill(e.Location);
                Point? userDragPoint = null;
                if (this._UserDragPointOffset.HasValue)
                    userDragPoint = e.Location.Add(this._UserDragPointOffset.Value);
                this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, mouseSourceItem);
                Point shift = e.Location.Sub(this._MouseDownAbsolutePoint.Value);
                Rectangle dragToArea = this._MouseDragMoveItemOriginBounds.Value.ShiftBy(shift);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveStep, ref userData, this._MouseCurrentRelativePoint,
                    targetPosition: mouseTargetItem, dragToArea: dragToArea, userDragPoint: userDragPoint);
            }
        }
        private void _MouseDragMoveCancel(ref object userData)
        {
            ActivePositionInfo mouseSourceItem = this._CurrentActiveItem;
            if (this._MouseDragMoveItem != null && mouseSourceItem.CanDrag)
            {
                mouseSourceItem.CurrentStateFill(null);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveCancel, ref userData);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveEnd, ref userData);
                this._RepaintAllItems = true;
                this._MouseDragMoveItem = null;
            }
            this._MouseDragMoveItemOffset = null;          // Primární handler _MouseUp() vyvolá _MouseRaise(), namísto _MouseDragDone()!  V metodě _MouseDragDone() bude volána metoda _MouseDownReset().
            this._CurrentMouseDragCanceled = true;
        }
        private void _MouseDragMoveDone(MouseEventArgs e, ref object userData)
        {
            ActivePositionInfo mouseSourceItem = this._CurrentActiveItem;
            this._MouseCurrentRelativePoint = _GetRelativePoint(e.Location, mouseSourceItem);
            this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftUp, ref userData, this._MouseCurrentRelativePoint);
            if (this._MouseDragMoveItem != null && mouseSourceItem.CanDrag)
            {
                mouseSourceItem.CurrentStateFill(e.Location);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveDone, ref userData, this._MouseCurrentRelativePoint, targetPosition: this._CurrentTargetItem);
                this._ItemMouseCallStateChangedEvent(mouseSourceItem, GInteractiveChangeState.LeftDragMoveEnd, ref userData);
            }
            this._MouseDragMoveItem = null;
            this._CurrentTargetItem = null;
            this._MouseDragState = MouseMoveDragState.None;
        }
        private void _MouseDragStoreActiveItem(GInteractiveChangeStateArgs stateArgs)
        {
            IInteractiveItem activeItem = null;
            if (stateArgs != null)
            {   // Pokud interaktivní akce nemá nic společného s Drag and Drop, pak jako Aktivní Target prvek bude vždy NULL:
                DragActionType dragAction = _GetDragActionForState(stateArgs.ChangeState);
                activeItem = ((dragAction != DragActionType.None) ? stateArgs.DragMoveActiveItem : null);
            }
            bool isActive = (activeItem != null);

            // Pokud dosud máme v evidenci prvek _DragMoveActiveItem, a nově daný prvek je jiný, pak ten dosavadní deaktivujeme a zapomeneme na něj:
            if (this._DragMoveActiveItem != null && (activeItem == null || !Object.ReferenceEquals(this._DragMoveActiveItem, activeItem)))
            {
                if (this._DragMoveActiveItem.Is.ActiveTarget)
                {
                    this._DragMoveActiveItem.Is.ActiveTarget = false;
                    this._DragMoveActiveItem.Repaint();
                }
                this._DragMoveActiveItem = null;
            }

            this._DragMoveActiveItem = activeItem;
            if (this._DragMoveActiveItem != null && this._DragMoveActiveItem.Is.ActiveTarget != isActive)
            {
                this._DragMoveActiveItem.Is.ActiveTarget = isActive;
                this._DragMoveActiveItem.Repaint();
            }

            // Zapamatujeme si aktivní prvek pro příště:
            this._DragMoveActiveItem = activeItem;
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
        private ActivePositionInfo _MouseDragMoveItem { get; set; }
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
        /// Velikost oblasti <see cref="_MouseDragStartBounds"/> = "No-Drag zone", kde se ignoruje pohyb myši na začátku procesu Drag.
        /// Je nastaven na hodnotu <see cref="SystemInformation.DragSize"/>, ale může být upraven.
        /// </summary>
        private Size _DragStartSize { get; set; }
        /// <summary>
        /// Prvek, který má být vysvícen jako Aktivní cíl v procesu Drag and Move.
        /// Výchozí hodnota je null. Aplikační kód může určit potenciální cílový objekt pro Drop akci, a tento objekt vložit do této property.
        /// Control <see cref="InteractiveControl"/> následně pro tento prvek nastaví jeho hodnotu Active, a prvek by se pak měl zobrazit zvýrazněný.
        /// Je nastavena hodnota <see cref="IInteractiveItem.Is"/>.ActiveTarget = true / false a je zajištěn Repaint() prvku.
        /// </summary>
        private IInteractiveItem _DragMoveActiveItem { get; set; }
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
                case GInteractiveChangeState.KeyboardKeyPreview: return GInteractiveState.Enabled;
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
        /// Zajistí přechod mezi dosud aktivním prvkem (oldActiveItem) a nově aktivním prvkem (newActiveItem).
        /// Tedy zajistí vyvolání eventů MouseLeave a MouseEnter pro prvky, které to potřebují.
        /// Nakonec uloží (newActiveItem) do <see cref="_CurrentActiveItem"/>.
        /// </summary>
        /// <param name="oldActiveItem"></param>
        /// <param name="newActiveItem"></param>
        /// <param name="mouseRelativePoint"></param>
        /// <param name="userData"></param>
        private void _ItemMouseExchange(ActivePositionInfo oldActiveItem, ActivePositionInfo newActiveItem, Point? mouseRelativePoint, ref object userData)
        {
            List<ActivePositionInfo.ActiveItemInfo> leaveList, enterList;
            ActivePositionInfo.MapExchange(oldActiveItem, newActiveItem, out leaveList, out enterList);
            IInteractiveItem leaveItem = oldActiveItem?.ActiveItem;
            IInteractiveItem enterItem = newActiveItem?.ActiveItem;

            foreach (ActivePositionInfo.ActiveItemInfo activeItem in leaveList)
                this._CallItemStateChangedEventEnterLeave(activeItem, GInteractiveChangeState.MouseLeave, leaveItem, enterItem, ref userData);

            foreach (ActivePositionInfo.ActiveItemInfo activeItem in enterList)
                this._CallItemStateChangedEventEnterLeave(activeItem, GInteractiveChangeState.MouseEnter, leaveItem, enterItem, ref userData);

            if (newActiveItem != null && newActiveItem.CanOver)
                this._ItemMouseCallStateChangedEvent(newActiveItem, GInteractiveChangeState.MouseOver, ref userData, mouseRelativePoint);

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
        /// <param name="userData"></param>
        private void _CallItemStateChangedEventEnterLeave(ActivePositionInfo.ActiveItemInfo activeItem, GInteractiveChangeState change, IInteractiveItem leaveItem, IInteractiveItem enterItem, ref object userData)
        {
            bool isEnabled = activeItem.Item.Is.Enabled;
            GInteractiveChangeState realChange = this._GetStateForCurrentMouseButton(change, isEnabled);
            var targetState = _GetStateAfterChange(realChange, isEnabled);
            GInteractiveChangeStateArgs stateArgs = new GInteractiveChangeStateArgs(activeItem.BoundsInfo, realChange, targetState, this.FindNewItemAtPoint, leaveItem, enterItem);
            stateArgs.UserDragPoint = null;

            stateArgs.UserData = userData;
            activeItem.Item.AfterStateChanged(stateArgs);
            this._ItemMouseCallDragEvent(activeItem.Item, stateArgs);

            this._CallInteractiveStateChanged(stateArgs);
            this._InteractiveDrawStore(stateArgs);
            userData = stateArgs.UserData;
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
        /// <param name="userData"></param>
        /// <param name="mouseRelativePoint"></param>
        /// <param name="recurseToSolver"></param>
        /// <param name="targetPosition"></param>
        /// <param name="dragToArea"></param>
        /// <param name="userDragPoint"></param>
        /// <param name="fillArgs"></param>
        private GInteractiveChangeStateArgs _ItemMouseCallStateChangedEvent(ActivePositionInfo activePosition, GInteractiveChangeState change, ref object userData,
            Point? mouseRelativePoint = null, bool recurseToSolver = false, ActivePositionInfo targetPosition = null, Rectangle? dragToArea = null, Point? userDragPoint = null,
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
            stateArgs.UserData = userData;

            if (fillArgs != null)
                fillArgs(stateArgs);

            if (activePosition.HasItem)
            {
                activePosition.CallAfterStateChanged(stateArgs, ref recurseToSolver);
                this._ItemMouseCallDragEvent(activePosition.ActiveItem, stateArgs);
            }
            if (recurseToSolver)
                this._SolveStateChanged(stateArgs);
            this._FlowToolTipData = (stateArgs.HasToolTipData ? stateArgs.ToolTipData : null);
            this._MouseDragStoreActiveItem(stateArgs);
            this._CallInteractiveStateChanged(stateArgs);
            this._InteractiveDrawStore(stateArgs);
            this._StoreContextMenu(stateArgs);

            userData = stateArgs.UserData;
            return stateArgs;
        }
        /// <summary>
        /// Tato metoda může vyřešit některé interaktivní události na úrovni vlastního Controlu
        /// </summary>
        /// <param name="stateArgs"></param>
        private void _SolveStateChanged(GInteractiveChangeStateArgs stateArgs)
        {
            switch (stateArgs.ChangeState)
            {
                case GInteractiveChangeState.WheelDown:
                case GInteractiveChangeState.WheelUp:
                    this._SolveStateChangedMouseWheel(stateArgs);
                    break;
            }
        }
        /// <summary>
        /// Zajistí provedení Scrollu celého containeru na základě kolečka myši
        /// </summary>
        /// <param name="stateArgs"></param>
        private void _SolveStateChangedMouseWheel(GInteractiveChangeStateArgs stateArgs)
        {
            if (this.AutoScrollActive)
                this.AutoScrollDoScrollBy(Orientation.Vertical, stateArgs.ChangeState, stateArgs.ModifierKeys);
        }
        /// <summary>
        /// Úložiště pro data ToolTipu v rámci jedné WinForm události.
        /// Na začátku každé WinForm události se nuluje.
        /// Pokud jedna WinForm událost má více logických událostí v <see cref="InteractiveControl"/>, 
        /// pak se sem ukládají data ToolTipu po skončení jedné události,
        /// a odsud se načtou a vkládají do <see cref="GInteractiveChangeStateArgs.ToolTipData"/> do další události.
        /// Důsledkem je to, že pokud jedna <see cref="InteractiveControl"/> událost nastaví tooltip,
        /// pak další událost v <see cref="InteractiveControl"/> tento tooltip již nemusí řešit a tooltip je stále nastaven.
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
        /// Vytvoří a vrátí instanci <see cref="ActivePositionInfo"/>, která bude obsahovat plnou cestu k prvku, který je na dané absolutní souřadnici.
        /// Dovolí najít i prvky, které mají <see cref="InteractiveProperties.Enabled"/> = false.
        /// Tato metoda nebere ohled na aktuálně nalezený prvek (<see cref="_CurrentActiveItem"/>), ignoruje tedy vlastnost <see cref="InteractiveProperties.HoldMouse"/> prvku,
        /// který je nalezen jako aktivní v <see cref="_CurrentActiveItem"/>.
        /// Tato metoda se používá jako delegát "item searcher" (searchItemMethod) v konstruktoru argumentu <see cref="GInteractiveChangeStateArgs"/>,
        /// a slouží aplikačnímu kódu, proto se liší od běžné metody <see cref="_FindActivePositionAtPoint(Point, bool)"/>.
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <returns></returns>
        protected IInteractiveItem FindNewItemAtPoint(Point mouseAbsolutePoint, bool withDisabled)
        {
            ActivePositionInfo activePosition = ActivePositionInfo.FindItemAtPoint(this, this.ItemsList, null, mouseAbsolutePoint, withDisabled);
            return (activePosition.HasItem ? activePosition.ActiveItem : null);
        }
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="ActivePositionInfo"/>, která bude obsahovat plnou cestu k prvku, který je na dané absolutní souřadnici.
        /// Dovolí najít i prvky, které mají <see cref="InteractiveProperties.Enabled"/> = false.
        /// Tato metoda BERE ohled na aktuálně nalezený prvek (<see cref="_CurrentActiveItem"/>), a pokud má vlastnost <see cref="InteractiveProperties.HoldMouse"/> = true, 
        /// pak tomuto prvku dává přednost (pokud to lze).
        /// <para/>
        /// Tato metoda NEZMĚNÍ obsah <see cref="_CurrentActiveItem"/>, ale ZMĚNÍ obsah <see cref="_MouseCurrentRelativePoint"/>.
        /// </summary>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <returns></returns>
        private ActivePositionInfo _FindActivePositionAtPoint(Point mouseAbsolutePoint, bool withDisabled)
        {
            ActivePositionInfo activePosition = ActivePositionInfo.FindItemAtPoint(
                this, this.ItemsList, this._CurrentActiveItem, mouseAbsolutePoint, withDisabled,
                (this._ProgressItem.Is.Visible ? this._ProgressItem : null));

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
        private static Point? _GetRelativePoint(Point point, ActivePositionInfo activePosition)
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
        private ActivePositionInfo _CurrentActiveItem { get { return this.__MouseCurrentItem; } set { this.__MouseCurrentItem = value; } } private ActivePositionInfo __MouseCurrentItem;
        /// <summary>
        /// Aktuální cílový prvek při myší interakci typu Drag and Move.
        /// Jinak je null.
        /// </summary>
        private ActivePositionInfo _CurrentTargetItem { get { return this.__CurrentTargetItem; } set { this.__CurrentTargetItem = value; } } private ActivePositionInfo __CurrentTargetItem;
        /// <summary>
        /// Item, which was last clicked.
        /// </summary>
        private ActivePositionInfo _MouseClickedItem { get; set; }
        /// <summary>
        /// Coordinates of mouse, relative to current interactive item bounds.
        /// </summary>
        private Point? _MouseCurrentRelativePoint { get; set; }
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
            DrawRequest drawRequest = new DrawRequest(this._RepaintAllItems, this._NeedDrawFrameBounds, this._MousePaintNeedDraw, this._ToolTip, this._ProgressItem);
            drawRequest.Fill(this, this.ItemsList, this.PendingFullDraw, true);
            if (drawRequest.NeedAnyDraw)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "InteractiveDrawRun", ""))
                {
                    try
                    {
                        this._DrawState = InteractiveDrawState.InteractiveRepaint;
                        this.Draw(drawRequest);
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool RepaintAllItems { get { return this._RepaintAllItems; } set { if (value) this._RepaintAllItems = true; } }
        /// <summary>
        /// Any Interactive method or any items need repaint all items, after current (interactive) event.
        /// </summary>
        private bool _RepaintAllItems;
        #endregion
        #region Podpora pro interaktivní kreslení MousePaint (MouseDown + Drag + MouseUp) => vytváření nového obrazce na controlu
        /// <summary>
        /// Povoluje aktivity MousePaint = kreslení pomocí myši (MouseDown, Drag, MouseUp).
        /// Výchozí hodnota je false.
        /// Pokud aplikace nastaví true, pak by musí obsloužit události <see cref="MousePaintProcessStart"/>, <see cref="MousePaintProcessTarget"/>, 
        /// a aby vše mělo smysl, měla by zaregistrovat i událost <see cref="MousePaintProcessCommit"/>.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MousePaintEnabled { get; set; }
        /// <summary>
        /// Událost, kdy interaktivní control potřebuje informaci, zda na dané souřadnici a na daném prvku je možno zahájit akci MousePaint.
        /// Tato událost se volá pouze tehdy, když <see cref="MousePaintEnabled"/> je true, 
        /// volá se při každém pohybu myši (kvůli aktualizaci kurzoru, pak je <see cref="GInteractiveMousePaintArgs.InteractiveChange"/> == <see cref="GInteractiveChangeState.MouseOver"/>)
        /// a volá se i při stisknutí myši (vlastní start kreslení, pak stav je LeftDown nebo RightDown).
        /// V argumentu jsou předány informace o pozici myši a o prvku na pozici myši, a o akci (interaktivní stav).
        /// Pokud bude akce MousePaint v tomto handleru povolena, pak uživatel stisknutím myši začne vykreslovat objekt, a bude volán event <see cref="MousePaintProcessTarget"/>.
        /// V odpovědi je očekávána informace <see cref="GInteractiveMousePaintArgs.IsEnabled"/>, a vhodné je i nastavit kurzor <see cref="GInteractiveMousePaintArgs.CursorType"/>.
        /// </summary>
        public event GInteractiveMousePaintHandler MousePaintProcessStart;
        /// <summary>
        /// Událost, kdy interaktivní control potřebuje informaci, zda na dané souřadnici a na daném prvku je možno umístit cíl (Target) akce MousePaint.
        /// Tato událost se volá pouze tehdy, když <see cref="MousePaintEnabled"/> je true, když pro určitý výchozí bod (Start) byl volán 
        /// event <see cref="MousePaintProcessStart"/> a ten vrátil <see cref="GInteractiveMousePaintArgs.IsEnabled"/> = true.
        /// </summary>
        public event GInteractiveMousePaintHandler MousePaintProcessTarget;
        /// <summary>
        /// Událost, kdy interaktivní control dokončil akci MousePaint (nakreslení objektu z bodu Start do bodu End).
        /// Aplikační kód by si v eventhandleru této události měl převzít data a zpracovat z nich h,atatelný a trvale viditelný výsledek.
        /// </summary>
        public event GInteractiveMousePaintHandler MousePaintProcessCommit;
        /// <summary>
        /// Na controlu se stiskla klávesa, zjistíme zda by mohla povolit akci MousePaint
        /// </summary>
        /// <param name="e"></param>
        private void _MousePaintKeyPress(KeyEventArgs e)
        {
            if (!this._MousePaintIsEnabled) return;
            Point mousePoint = Control.MousePosition;
            ActivePositionInfo newActiveItem = this._FindActivePositionAtPoint(mousePoint, false);
            GInteractiveMousePaintArgs paintArgs = this._MousePaintStartIsEnabled(newActiveItem, GInteractiveChangeState.MouseOver);
            this._MousePaintShowCursorMove(paintArgs);
        }
        /// <summary>
        /// Uživatel pohybuje myší nad controlem, a pokud je povoleno kreslení pak tato metoda může změnit kurzor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <param name="newActiveItem"></param>
        private void _MousePaintMove(MouseEventArgs e, ref object userData, ActivePositionInfo newActiveItem)
        {
            if (!this._MousePaintIsEnabled) return;
            GInteractiveMousePaintArgs paintArgs = this._MousePaintStartIsEnabled(newActiveItem, GInteractiveChangeState.MouseOver);
            this._MousePaintShowCursorMove(paintArgs);
            this._MousePaintToolTipSet(e.Location, paintArgs, false);
        }
        /// <summary>
        /// Uživatel zmáčkl myš v určitém místě: zdejší metoda určí, zda můžeme přejít do stavu Draw
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <param name="newActiveItem"></param>
        private void _MousePaintDown(MouseEventArgs e, ref object userData, ActivePositionInfo newActiveItem)
        {
            if (!this._MousePaintIsEnabled) return;

            GInteractiveChangeState interactiveChange = (e.Button == MouseButtons.Left ? GInteractiveChangeState.LeftDown : GInteractiveChangeState.RightDown);
            GInteractiveMousePaintArgs paintArgs = this._MousePaintStartIsEnabled(newActiveItem, interactiveChange);
            // Zde kurzor neřešíme, protože: a) není Enabled => kurzor neměníme, b) je Enabled => pak předáme řízení do _MousePaintStep() 
            if (paintArgs == null || !paintArgs.IsEnabled) return;

            // Protože přecházíme do stavu MousePaint, pak musíme nastavit proměnné o stavu a jeho začátku:
            this._MouseDragState = MouseMoveDragState.Paint;
            this._MousePaintInteractiveMode = interactiveChange;
            this._MousePaintBeginPoint = e.Location;
            this._MousePaintBeginItem = newActiveItem;
            this._MousePaintTimeStart = DateTime.Now;

            // A aktuální point bereme nejen jako Start, ale i jako možný End:
            this._MousePaintStep(e, ref userData, newActiveItem);
        }
        /// <summary>
        /// Metoda je volána v každém kroku aktivního kreslení.
        /// Jsme v režimu <see cref="_MouseDragState"/> == <see cref="MouseMoveDragState.Paint"/>, tzn. kreslíme = myš je zmáčknutá, a pohybuje se.
        /// Metoda je volána i při začátku kreslení. Není volána při MouseUp.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MousePaintStep(MouseEventArgs e, ref object userData)
        {
            ActivePositionInfo newActiveItem = this._FindActivePositionAtPoint(e.Location, false);
            this._MousePaintStep(e, ref userData, newActiveItem);
        }
        /// <summary>
        /// Metoda je volána v každém kroku aktivního kreslení.
        /// Jsme v režimu <see cref="_MouseDragState"/> == <see cref="MouseMoveDragState.Paint"/>, tzn. kreslíme = myš je zmáčknutá, a pohybuje se.
        /// Metoda je volána i při začátku kreslení. Není volána při MouseUp.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        /// <param name="newActiveItem"></param>
        private void _MousePaintStep(MouseEventArgs e, ref object userData, ActivePositionInfo newActiveItem)
        {
            GInteractiveMousePaintArgs paintArgs = this._MousePaintTargetIsEnabled(newActiveItem);
            this._MousePaintShowCursorDrag(paintArgs);
            this._MousePaintToolTipSet(e.Location, paintArgs, true);
            this._MousePaintEndPoint = e.Location;
            this._MousePaintEndItem = newActiveItem;
        }
        /// <summary>
        /// Voláno v procesu MouseDrag:Cancel
        /// </summary>
        private void _MousePaintCancel(ref object userData)
        {
            if (this._MouseDragState != MouseMoveDragState.Paint) return;
            this._MousePaintEnd(true);
        }
        /// <summary>
        /// Metoda je volána při MouseUp na konci procesu kreslení.
        /// Jejím úkolem je vykreslený prvek commitovat (protože nebyl proveden Cancel).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MousePaintDone(MouseEventArgs e, ref object userData)
        {
            ActivePositionInfo newActiveItem = this._FindActivePositionAtPoint(e.Location, false);
            GInteractiveMousePaintArgs paintArgs = this._MousePaintTargetIsCommited(newActiveItem);
            this._MousePaintToolTipSet(e.Location, paintArgs, false);
            this._MousePaintEnd(true);
        }
        /// <summary>
        /// Voláno na konci procesu MousePaint, a to jak po Cancel, tak po Done.
        /// </summary>
        private void _MousePaintEnd(bool repaintAllItems)
        {
            this._MouseDragState = MouseMoveDragState.None;
            this._MousePaintInteractiveMode = null;
            this._MousePaintBeginPoint = null;
            this._MousePaintBeginItem = null;
            this._MousePaintEndPoint = null;
            this._MousePaintEndItem = null;
            this._MousePaintShowCursorMove(null);
            this._MousePaintInfo = null;
            this._MousePaintTimeStart = null;
            this._RepaintAllItems = repaintAllItems;
        }
        /// <summary>
        /// Metoda zjistí informace o povolení kreslit / začít kresbu pomocí myši na dané výchozí (Start) souřadnici.
        /// Metoda může vrátit null, což je rychlejší než vracet new empty argument.
        /// </summary>
        /// <returns></returns>
        private GInteractiveMousePaintArgs _MousePaintStartIsEnabled(ActivePositionInfo currentActiveItem, GInteractiveChangeState interactiveChange)
        {
            if (!this._MousePaintIsEnabled) return null;
            ToolTipData toolTipData = this._FlowToolTipData;
            GInteractiveMousePaintArgs paintArgs = new GInteractiveMousePaintArgs(interactiveChange, currentActiveItem, null, null, toolTipData);
            this.MousePaintProcessStart(this, paintArgs);
            this._MousePaintInfo = (paintArgs.IsEnabled ? paintArgs.PaintInfo : null);          // Eventhandler si mohl připravit data, pokud vrací true
            if (paintArgs.HasToolTipData)
                this._FlowToolTipData = paintArgs.ToolTipData;
            return paintArgs;
        }
        /// <summary>
        /// Metoda zjistí informace o povolení kreslit pomocí myši do dané cílové souřadnice.
        /// Metoda může vrátit null, což je rychlejší než vracet new empty argument.
        /// </summary>
        /// <returns></returns>
        private GInteractiveMousePaintArgs _MousePaintTargetIsEnabled(ActivePositionInfo currentActiveItem)
        {
            if (!this._MousePaintIsEnabled) return null;

            GInteractiveChangeState interactiveChange = this._MousePaintInteractiveMode ?? GInteractiveChangeState.LeftDown;
            ActivePositionInfo startActiveItem = this._MousePaintBeginItem;
            MousePaintInfo mousePaintInfo = this._MousePaintInfo;    // Objekt může již existovat
            if (mousePaintInfo == null) mousePaintInfo = MousePaintInfo.Default;
            ToolTipData toolTipData = this._FlowToolTipData;
            GInteractiveMousePaintArgs paintArgs = new GInteractiveMousePaintArgs(interactiveChange, currentActiveItem, startActiveItem, mousePaintInfo, toolTipData);
            this.MousePaintProcessTarget(this, paintArgs);
            if (paintArgs.PaintInfo != null)
                this._MousePaintInfo = paintArgs.PaintInfo;          // Eventhandler mohl objekt vyměnit, proto vždy uložíme aktuální referenci; ale null si nepřebereme.
            if (paintArgs.HasToolTipData)
                this._FlowToolTipData = paintArgs.ToolTipData;

            return paintArgs;
        }
        /// <summary>
        /// Metoda vyvolá eventhandler <see cref="MousePaintProcessCommit"/> včetně potřebných dat.
        /// </summary>
        /// <param name="currentActiveItem"></param>
        /// <returns></returns>
        private GInteractiveMousePaintArgs _MousePaintTargetIsCommited(ActivePositionInfo currentActiveItem)
        {
            if (this.MousePaintProcessCommit == null) return null;

            GInteractiveChangeState interactiveChange = this._MousePaintInteractiveMode ?? GInteractiveChangeState.LeftDown;
            ActivePositionInfo startActiveItem = this._MousePaintBeginItem;
            MousePaintInfo mousePaintInfo = this._MousePaintInfo;    // Objekt může již existovat
            if (mousePaintInfo == null) mousePaintInfo = MousePaintInfo.Default;
            ToolTipData toolTipData = this._FlowToolTipData;
            GInteractiveMousePaintArgs paintArgs = new GInteractiveMousePaintArgs(interactiveChange, currentActiveItem, startActiveItem, mousePaintInfo, toolTipData);
            this.MousePaintProcessCommit(this, paintArgs);
            if (paintArgs.HasToolTipData)
                this._FlowToolTipData = paintArgs.ToolTipData;

            return paintArgs;
        }
        /// <summary>
        /// Obsahuje true, když v aktuální situaci se může pracovat v režimu MousePaint (tj. když není blokované GUI, a <see cref="MousePaintEnabled"/> je true, 
        /// a jsou zadány eventhandlery <see cref="MousePaintProcessStart"/> a <see cref="MousePaintProcessTarget"/>).
        /// </summary>
        private bool _MousePaintIsEnabled { get { return (!this.IsGUIBlocked && this.MousePaintEnabled && this.MousePaintProcessStart != null && this.MousePaintProcessTarget != null); } }
        /// <summary>
        /// Zajistí zobrazení kurzoru pro kreslení v metodě MouseMove (tj. pohyb myši nad controlem, bez stisknutého tlačítka), podle dat v argumentu.
        /// Je voláno i při skončení MousePaint (po zvednutí myši nebo po Cancel), pak má nastavit původní kurzor.
        /// </summary>
        /// <param name="paintArgs"></param>
        private void _MousePaintShowCursorMove(GInteractiveMousePaintArgs paintArgs)
        {
            bool isPaint = (paintArgs != null ? paintArgs.IsEnabled : false);
            if (isPaint)
            {   // Máme mít kreslící kurzor:
                if (!this._MousePaintCursorBefore.HasValue)
                    this._MousePaintCursorBefore = this._CurrentCursorType;    // _CurrentCursorType.get nikdy nevrací null
                this._ActivateCursor(paintArgs.CursorType ?? SysCursorType.Cross);
            }
            else
            {   // Nemáme mít kreslící kurzor:
                if (this._MousePaintCursorBefore.HasValue)
                {
                    this._ActivateCursor(this._MousePaintCursorBefore);
                    this._MousePaintCursorBefore = null;
                }
            }
        }
        /// <summary>
        /// Zajistí zobrazení kurzoru pro kreslení v metodě MouseDrag (tj. pohyb myši se stisknutým tlačítkem), podle dat v argumentu.
        /// </summary>
        /// <param name="paintArgs"></param>
        private void _MousePaintShowCursorDrag(GInteractiveMousePaintArgs paintArgs)
        {
            if (paintArgs == null) return;

            // Zálohuji aktuální typ kurzoru, pokud je to třeba:
            if (!this._MousePaintCursorBefore.HasValue)
                this._MousePaintCursorBefore = this._CurrentCursorType;

            // Vykreslím požadovaný typ kurzoru, ale pouze pokud je v argumentu uveden, jinak nechám dosavadní:
            if (paintArgs.CursorType.HasValue)
                this._ActivateCursor(paintArgs.CursorType);
        }
        /// <summary>
        /// Zajistí zobrazení tooltipu v rámci akce MousePaint
        /// </summary>
        /// <param name="point"></param>
        /// <param name="paintArgs"></param>
        /// <param name="force"></param>
        private void _MousePaintToolTipSet(Point point, GInteractiveMousePaintArgs paintArgs, bool force)
        {
            if ((paintArgs.IsEnabled || force) && paintArgs.HasToolTipData)
                this._ToolTipSet(point, paintArgs.ToolTipData);
        }
        /// <summary>
        /// Metoda zajistí vykreslení obrazce kresleného myší
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="layer"></param>
        private void _MousePaintDraw(Graphics graphics, GInteractiveDrawLayer layer)
        {
            MousePaintInfo mousePaintInfo = this._MousePaintInfo;
            if (mousePaintInfo == null) return;
            if (!(mousePaintInfo.StartPoint.HasValue && mousePaintInfo.EndPoint.HasValue)) return;

            switch (mousePaintInfo.ObjectType)
            {
                case MousePaintObjectType.StraightLine:
                    this._MousePaintDrawLink(graphics, mousePaintInfo, LinkLineType.StraightLine);
                    break;
                case MousePaintObjectType.SCurveVertical:
                    this._MousePaintDrawLink(graphics, mousePaintInfo, LinkLineType.SCurveVertical);
                    break;
                case MousePaintObjectType.SCurveHorizontal:
                    this._MousePaintDrawLink(graphics, mousePaintInfo, LinkLineType.SCurveHorizontal);
                    break;
                case MousePaintObjectType.ZigZagHorizontal:
                    this._MousePaintDrawLink(graphics, mousePaintInfo, LinkLineType.ZigZagHorizontal);
                    break;
                case MousePaintObjectType.ZigZagVertical:
                    this._MousePaintDrawLink(graphics, mousePaintInfo, LinkLineType.ZigZagVertical);
                    break;
                case MousePaintObjectType.ZigZagOptimal:
                    this._MousePaintDrawLink(graphics, mousePaintInfo, LinkLineType.ZigZagOptimal);
                    break;
                case MousePaintObjectType.Rectangle:
                    this._MousePaintDrawRectangle(graphics, mousePaintInfo);
                    break;
                case MousePaintObjectType.Ellipse:
                    this._MousePaintDrawEllipse(graphics, mousePaintInfo);
                    break;
                case MousePaintObjectType.Image:
                    this._MousePaintDrawImage(graphics, mousePaintInfo);
                    break;
                case MousePaintObjectType.UserDraw:
                    this._MousePaintDrawUserDraw(graphics, mousePaintInfo);
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí vykreslení obrazce kresleného myší - tvar: <see cref="LinkLineType"/>
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="mousePaintInfo"></param>
        /// <param name="lineType"></param>
        private void _MousePaintDrawLink(Graphics graphics, MousePaintInfo mousePaintInfo, LinkLineType lineType)
        {
            float? treshold = _MousePaintGetTreshold(mousePaintInfo, lineType);
            using (var line = Painter.CreatePathLink(lineType, mousePaintInfo.StartPoint.Value, mousePaintInfo.EndPoint.Value, treshold))
                _MousePaintDrawPath(graphics, line, mousePaintInfo);
        }
        /// <summary>
        /// Vrátí treshold pro tvorbu čáry pro daný tvar čáry, daný typ zakončení a šířku.
        /// </summary>
        /// <param name="mousePaintInfo"></param>
        /// <param name="lineType"></param>
        /// <returns></returns>
        private float? _MousePaintGetTreshold(MousePaintInfo mousePaintInfo, LinkLineType lineType)
        {
            if (lineType == LinkLineType.StraightLine || lineType == LinkLineType.SCurveHorizontal || lineType == LinkLineType.SCurveVertical) return null;

            float coeff1 = _MousePaintGetLineCapCoeff(mousePaintInfo.StartCap);
            float coeff2 = _MousePaintGetLineCapCoeff(mousePaintInfo.EndCap);
            float coeff = (coeff1 > coeff2 ? coeff1 : coeff2);
            float width = mousePaintInfo.LineWidth;
            return 1.1f * coeff * width;
        }
        /// <summary>
        /// Vrátí koeficient délky daného zakončení čáry
        /// </summary>
        /// <param name="lineCap"></param>
        /// <returns></returns>
        private static float _MousePaintGetLineCapCoeff(LineCap lineCap)
        {
            switch (lineCap)
            {
                case LineCap.AnchorMask:
                case LineCap.DiamondAnchor:
                case LineCap.RoundAnchor:
                case LineCap.SquareAnchor:
                    return 2f;
                case LineCap.ArrowAnchor:
                    return 3f;
            }
            return 1f;
        }
        /// <summary>
        /// Metoda zajistí vykreslení obrazce kresleného myší - tvar: Rectangle
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="mousePaintInfo"></param>
        private void _MousePaintDrawRectangle(Graphics graphics, MousePaintInfo mousePaintInfo)
        {
            Rectangle? bounds = _MousePaintGetBounds(mousePaintInfo);
            if (!bounds.HasValue) return;
            if (mousePaintInfo.FillColor.HasValue)
                graphics.FillRectangle(Skin.Brush(mousePaintInfo.FillColor.Value), bounds.Value);
            if (mousePaintInfo.LineColor.HasValue && mousePaintInfo.LineWidth > 0)
                graphics.DrawRectangle(Skin.Pen(mousePaintInfo.LineColor.Value, mousePaintInfo.LineWidth), bounds.Value);
        }
        /// <summary>
        /// Metoda zajistí vykreslení obrazce kresleného myší - tvar: Ellipse
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="mousePaintInfo"></param>
        private void _MousePaintDrawEllipse(Graphics graphics, MousePaintInfo mousePaintInfo)
        {
            Rectangle? bounds = _MousePaintGetBounds(mousePaintInfo);
            if (!bounds.HasValue) return;
            if (mousePaintInfo.FillColor.HasValue)
                graphics.FillEllipse(Skin.Brush(mousePaintInfo.FillColor.Value), bounds.Value);
            if (mousePaintInfo.LineColor.HasValue && mousePaintInfo.LineWidth > 0)
                graphics.DrawEllipse(Skin.Pen(mousePaintInfo.LineColor.Value, mousePaintInfo.LineWidth), bounds.Value);
        }
        /// <summary>
        /// Metoda zajistí vykreslení obrazce kresleného myší - tvar: Image
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="mousePaintInfo"></param>
        private void _MousePaintDrawImage(Graphics graphics, MousePaintInfo mousePaintInfo)
        {
            Rectangle? bounds = _MousePaintGetBounds(mousePaintInfo);
            if (!bounds.HasValue) return;
            if (mousePaintInfo.FillColor.HasValue)
                graphics.FillEllipse(Skin.Brush(mousePaintInfo.FillColor.Value), bounds.Value);
            if (mousePaintInfo.LineColor.HasValue && mousePaintInfo.LineWidth > 0)
                graphics.DrawEllipse(Skin.Pen(mousePaintInfo.LineColor.Value, mousePaintInfo.LineWidth), bounds.Value);
            if (mousePaintInfo.Image != null)
                graphics.DrawImage(mousePaintInfo.Image, bounds.Value);
        }
        /// <summary>
        /// Metoda zajistí vykreslení obrazce kresleného myší - typ: UserDraw
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="mousePaintInfo"></param>
        private void _MousePaintDrawUserDraw(Graphics graphics, MousePaintInfo mousePaintInfo)
        {
            Rectangle? bounds = _MousePaintGetBounds(mousePaintInfo);
            if (!bounds.HasValue) return;
            // ???
        }
        /// <summary>
        /// Vykreslí do dané grafiky daný obrazec podle předpisu
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="path"></param>
        /// <param name="mousePaintInfo"></param>
        private void _MousePaintDrawPath(Graphics graphics, GraphicsPath path, MousePaintInfo mousePaintInfo)
        {
            Painter.DrawLinkPath(graphics, path, mousePaintInfo.LineColor, mousePaintInfo.FillColor, mousePaintInfo.LineWidth, mousePaintInfo.StartCap, mousePaintInfo.EndCap, setSmoothGraphics: true);
        }
        /// <summary>
        /// Vrací souřadnice prostoru bezi body <see cref="MousePaintInfo.StartPoint"/> a <see cref="MousePaintInfo.EndPoint"/>
        /// </summary>
        /// <param name="mousePaintInfo"></param>
        /// <returns></returns>
        private static Rectangle? _MousePaintGetBounds(MousePaintInfo mousePaintInfo)
        {
            if (mousePaintInfo == null || !mousePaintInfo.StartPoint.HasValue || !mousePaintInfo.EndPoint.HasValue) return null;
            Point p1 = mousePaintInfo.StartPoint.Value;
            Point p2 = mousePaintInfo.EndPoint.Value;
            int x = (p1.X < p2.X ? p1.X : p2.X);
            int w = (p1.X < p2.X ? p2.X - p1.X : p1.X - p2.X);
            int y = (p1.Y < p2.Y ? p1.Y : p2.Y);
            int h = (p1.Y < p2.Y ? p2.Y - p1.Y : p1.Y - p2.Y);
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Obsahuje true, když je aktivní proces MousePaint a control tedy bude vykreslovat obrazec kreslený myší
        /// </summary>
        private bool _MousePaintNeedDraw { get { return (this._MouseDragState == MouseMoveDragState.Paint); } }
        /// <summary>
        /// Typ kurzoru, který byl aktivní před vstupem do oblasti s povoleným kreslením
        /// </summary>
        private SysCursorType? _MousePaintCursorBefore { get; set; }
        /// <summary>
        /// Režim kreslení: levá / pravá myš (může mít pouze hodnotou null nebo GInteractiveChangeState.LeftDown / GInteractiveChangeState.RightDown)
        /// </summary>
        private GInteractiveChangeState? _MousePaintInteractiveMode { get; set; }
        /// <summary>
        /// Absolutní souřadnice bodu, kde byla stisknuta myš v režimu <see cref="MouseMoveDragState.Paint"/>
        /// </summary>
        private Point? _MousePaintBeginPoint { get; set; }
        /// <summary>
        /// Prvek, kde byla stisknuta myš a začala tak akce MousePaint
        /// </summary>
        private ActivePositionInfo _MousePaintBeginItem { get; set; }
        /// <summary>
        /// Absolutní souřadnice bodu, kde se nachází myš aktuálně, v režimu <see cref="MouseMoveDragState.Paint"/>
        /// </summary>
        private Point? _MousePaintEndPoint { get; set; }
        /// <summary>
        /// Prvek, kde se aktuálně nachází myš a v průběhu akce MousePaint
        /// </summary>
        private ActivePositionInfo _MousePaintEndItem { get; set; }
        /// <summary>
        /// Data určující parametry pro kreslení obrazce v režimu MousePaint.
        /// Data obecně určuje eventhandler <see cref="MousePaintProcessTarget"/>. 
        /// Tato data se následně používají při kreslení obrazce (spojovací linka, jiný obrazec, image) v metodě <see cref="_MousePaintDraw(Graphics, GInteractiveDrawLayer)"/>.
        /// </summary>
        private MousePaintInfo _MousePaintInfo { get; set; }
        /// <summary>
        /// Čas začátku kreslení myší
        /// </summary>
        private DateTime? _MousePaintTimeStart { get; set; }
        /// <summary>
        /// Počet sekund, po které se provádí kreslení myší
        /// </summary>
        private double _MousePaintTimeSeconds
        {
            get
            {
                if (!this._MousePaintTimeStart.HasValue) return 0d;
                TimeSpan time = DateTime.Now - this._MousePaintTimeStart.Value;
                return time.TotalSeconds;
            }
        }
        #endregion
        #region Podpora pro Kontextové menu
        /// <summary>
        /// Vyvolá událost pro získání kontextového menu
        /// </summary>
        /// <param name="gcItem"></param>
        /// <param name="change"></param>
        /// <param name="userData"></param>
        /// <param name="mouseRelativePoint"></param>
        private void _ItemMouseCallContextMenu(ActivePositionInfo gcItem, GInteractiveChangeState change, ref object userData, Point? mouseRelativePoint)
        {
            this._ItemMouseCallStateChangedEvent(this._CurrentActiveItem, GInteractiveChangeState.GetContextMenu, ref userData, this._MouseCurrentRelativePoint);
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
                    Rectangle bounds = stateArgs.BoundsInfo.CurrentItemAbsoluteBounds;
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
        { }
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
            DrawRequest request = new DrawRequest(false, this._NeedDrawFrameBounds, this._MousePaintNeedDraw, this._ToolTip, this._ProgressItem);
            if (request.NeedAnyDraw)
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "ProgressDrawRun", ""))
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
        /// Metoda je volána při MouseDown, při stisku LeftMouse, a pokud aktuální prvek má <see cref="ActivePositionInfo.ItemIsSelectable"/> == true.
        /// Metoda sama zjistí Modifier keys z <see cref="ActivePositionInfo.CurrentModifierKeys"/>, 
        /// a pokud NENÍ stisknutý Control, tak zruší stav IsSelect na všech aktuálně označených objektech.
        /// </summary>
        /// <param name="activeItem"></param>
        /// <param name="userData"></param>
        private void _ItemMouseLeftDownUnSelect(ActivePositionInfo activeItem, ref object userData)
        {
            bool leaveOther = activeItem.CurrentModifierKeys.HasFlag(Keys.Control);
            if (!leaveOther)
                this.Selector.ClearSelected();
        }
        /// <summary>
        /// Uživatel provedl LeftClick na prvku, který má nastaveno <see cref="InteractiveProperties.Selectable"/>, proto by měl být změněn stav <see cref="IInteractiveItem.IsSelected"/>.
        /// </summary>
        /// <param name="activeItem"></param>
        /// <param name="userData"></param>
        /// <param name="modifierKeys"></param>
        private void _ItemMouseLeftClickSelect(ActivePositionInfo activeItem, ref object userData, Keys modifierKeys)
        {
            this._ItemMouseCallStateChangedEvent(activeItem, GInteractiveChangeState.LeftClickSelect, ref userData, activeItem.CurrentMouseRelativePoint);
        }
        /// <summary>
        /// Zahájení označování Drag and Frame.
        /// Tato metoda se volá v situaci, kdy je detekován Drag proces (MouseDown a poté MouseMove) na prvku, který nepodporuje MouseDrag, ale podporuje SelectParent.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MouseDragFrameBegin(MouseEventArgs e, ref object userData)
        {
            // Relativní pozice myši v okamžiku MouseDown, nikoli aktuální pozice (ta už je mimo prostor _CurrentMouseDragStart):
            ActivePositionInfo mouseCurrentItem = this._CurrentActiveItem;
            Point downPoint = this._MouseDownAbsolutePoint.Value;
            this._MouseCurrentRelativePoint = _GetRelativePoint(this._MouseDownAbsolutePoint.Value, mouseCurrentItem);
            if (mouseCurrentItem.ItemIsSelectParent)
            {
                mouseCurrentItem.CurrentStateFill(downPoint);
                this._MouseDragFrameParentItem = mouseCurrentItem;
                var stateArgs = this._ItemMouseCallStateChangedEvent(mouseCurrentItem, GInteractiveChangeState.LeftDragFrameBegin, ref userData, this._MouseCurrentRelativePoint);
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
        /// <param name="userData"></param>
        private void _MouseDragFrameStep(MouseEventArgs e, ref object userData)
        {
            if (!this._MouseDownAbsolutePoint.HasValue) return;
            if (!this._MouseDragFrameActive) return;
            Rectangle frameBounds = DrawingExtensions.FromPoints(this._MouseDownAbsolutePoint.Value, e.Location);
            if (this._MouseDragFrameWorkArea.HasValue)
                frameBounds = Rectangle.Intersect(this._MouseDragFrameWorkArea.Value, frameBounds);

            this._MouseDragFrameCurrentBounds = frameBounds;

            Tuple<IInteractiveItem, Rectangle>[] items = ActivePositionInfo.FindItemsAtBounds(this, this.ItemsList, frameBounds,
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
        /// <param name="userData"></param>
        private void _MouseDragFrameCancel(ref object userData)
        {
            if (!this._MouseDragFrameActive) return;
            this._Selector.ClearFramed();
            this._MouseDragFrameActive = false;
        }
        /// <summary>
        /// Metoda je zavolána po skončení procesu Drag and Frame, jejím úkolem je označit vybrané prvky pomocí Selectoru, a odeslat jim zprávu.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="userData"></param>
        private void _MouseDragFrameDone(MouseEventArgs e, ref object userData)
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
            Painter.DrawFrameSelect(graphics, this._MouseDragFrameCurrentBounds.Value);
        }
        /// <summary>
        /// Aktivita procesu Drag and Frame: true po startu (<see cref="_MouseDragFrameBegin(MouseEventArgs, ref object)"/>), 
        /// false po cancelu (<see cref="_MouseDragFrameCancel(ref object)"/>) nebo po skončení (<see cref="_MouseDragFrameDone(MouseEventArgs, ref object)"/>).
        /// </summary>
        private bool _MouseDragFrameActive { get; set; }
        /// <summary>
        /// Prvek, který je Parentem aktuální akce Drag and Frame.
        /// </summary>
        private ActivePositionInfo _MouseDragFrameParentItem { get; set; }
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
            this.PrepareLayers("Standard", "Interactive", "Dynamic", "Overlay");
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
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "InteractiveControl", "OnPaintLayers", "", "Bounds: " + this.Bounds))
            {
                DrawRequest request = e.UserData as DrawRequest;
                scope.AddItem("e.UserData: " + ((request == null) ? "null => Full Draw" : "Explicit request"));
                scope.AddItem("PendingFullDraw: " + (this.PendingFullDraw ? "true => Full Draw" : "false"));

                bool createNewRequest = (request == null || this.PendingFullDraw);
                bool drawAllItems = (createNewRequest || (request != null || request.DrawAllItems));

                if (drawAllItems)
                    this.AutoScrollDetect();

                if (createNewRequest)
                {   // Není zadán explicitní požadavek (request) - pak tedy vykreslíme všechny prvky:
                    request = new DrawRequest(true, this._NeedDrawFrameBounds, this._MousePaintNeedDraw, this._ToolTip, this._ProgressItem);
                    request.Fill(this, this.ItemsList, true, false);
                }

                this._PaintLayerStandard(e, request, graphicsSize, scope);
                this._PaintLayerInteractive(e, request, graphicsSize, scope);
                this._PaintLayerDynamic(e, request, graphicsSize, scope);
                this._PaintLayerOverlay(e, request, graphicsSize, scope);

                this._PaintStopwatch(e);

                this._DrawState = InteractiveDrawState.Standard;
                this._RepaintAllItems = false;
            }
        }
        /// <summary>
        /// Metoda vykreslí vše do vrstvy 0 = Standard
        /// </summary>
        /// <param name="e"></param>
        /// <param name="request"></param>
        /// <param name="graphicsSize"></param>
        /// <param name="scope"></param>
        private void _PaintLayerStandard(LayeredPaintEventArgs e, DrawRequest request, Size graphicsSize, ITraceScope scope)
        {
            if (request.NeedStdDraw || request.DrawAllItems)
            {
                if (request.DrawAllItems)
                    base.OnPaintLayers(e);
                Graphics graphics = e.GetGraphicsForLayer(0, true);
                this.CallDrawStandardLayer(graphics);
                this._PaintItems(graphics, graphicsSize, request.StandardItems, GInteractiveDrawLayer.Standard);
                scope.AddItem("Layer Standard, Items: " + request.StandardItems.Count.ToString());
            }
        }
        /// <summary>
        /// Metoda vykreslí vše do vrstvy 1 = Interactive
        /// </summary>
        /// <param name="e"></param>
        /// <param name="request"></param>
        /// <param name="graphicsSize"></param>
        /// <param name="scope"></param>
        private void _PaintLayerInteractive(LayeredPaintEventArgs e, DrawRequest request, Size graphicsSize, ITraceScope scope)
        {
            if (request.NeedIntDraw)
            {
                Graphics graphics = e.GetGraphicsForLayer(1, true);
                this._PaintItems(graphics, graphicsSize, request.InteractiveItems, GInteractiveDrawLayer.Interactive);
                scope.AddItem("Layer Interactive, Items: " + request.InteractiveItems.Count.ToString());
            }
        }
        /// <summary>
        /// Metoda vykreslí vše do vrstvy 2 = Dynamic.
        /// Tato vrstva obsahuje to, co kreslí myš (=FrameBounds + MousePaint).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="request"></param>
        /// <param name="graphicsSize"></param>
        /// <param name="scope"></param>
        private void _PaintLayerDynamic(LayeredPaintEventArgs e, DrawRequest request, Size graphicsSize, ITraceScope scope)
        {
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
                if (request.DrawMousePaint)
                {
                    this._MousePaintDraw(graphics, GInteractiveDrawLayer.Dynamic);
                }
            }
        }
        /// <summary>
        /// Metoda vykreslí vše do vrstvy 3 = Overlay
        /// </summary>
        /// <param name="e"></param>
        /// <param name="request"></param>
        /// <param name="graphicsSize"></param>
        /// <param name="scope"></param>
        private void _PaintLayerOverlay(LayeredPaintEventArgs e, DrawRequest request, Size graphicsSize, ITraceScope scope)
        {
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
        /// Aktuální typ kurzoru.
        /// Čtení hodnoty: nikdy nevrací null, ve výchozím stavu vracá Default.
        /// Setování hodnoty: vložení hodnoty null nezmění hodnotu.
        /// </summary>
        private SysCursorType? _CurrentCursorType
        {
            get { return (this.__CurrentCursorType ?? SysCursorType.Default); }
            set { this.__CurrentCursorType = value; }
        }
        private SysCursorType? __CurrentCursorType;
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
            if (this.InteractiveProcessing)
            {   // Chtěli bychom Refresh, ale běží nám Interactive proces => nelze dát Refresh, musí to počkat až skončí interaktivita:
                this._PendingRefresh = true;
            }
            else
            {   // Neběží nám žádný Interactive proces => zavoláme Refresh, a ještě před tím shodíme případný příznak čekajícího Refreshe:
                this._PendingRefresh = false;
                base.Refresh();
            }
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
            /// <param name="drawMousePaint"></param>
            /// <param name="toolTipItem"></param>
            /// <param name="progressItem"></param>
            public DrawRequest(bool drawAllItems, bool drawFrameSelect, bool drawMousePaint, ToolTipItem toolTipItem, ProgressItem progressItem)
                : this()
            {
                this.DrawAllItems = drawAllItems;
                this.DrawFrameSelect = drawFrameSelect;
                this.DrawMousePaint = drawMousePaint;
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
            /// Obsahuje true, pokud je požadavek na vykreslení objektu tvořeného myší (kreslí se do vrstvy Dynamic)
            /// </summary>
            public bool DrawMousePaint { get; private set; }
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
            public bool NeedDynDraw { get { return (this.DynamicItems.Count > 0 || this.DrawFrameSelect || this.DrawMousePaint); } }
            /// <summary>
            /// true when need any draw (Standard, Interactive, Dynamic, ToolTip)
            /// </summary>
            public bool NeedAnyDraw { get { return (this.NeedStdDraw || this.NeedIntDraw || this.NeedDynDraw || this.DrawToolTip || this.DrawProgress); } }
            /// <summary>
            /// Do this objektu naplní prvky k vykreslení (prvky typu IInteractiveItem) z dodaného seznamu, 
            /// prvky zatřídí do soupisů dle vrstev (this.StandardItems, InteractiveItems, DynamicItems).
            /// Pokud daný prvek obsahuje nějaké Childs, pak rekurzivně vyvolá tutéž metodu i pro Childs tohoto prvku.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="items">Prvky k vykreslení</param>
            /// <param name="drawAllItems">true = vykreslit všechny prvky</param>
            /// <param name="interactive">true = provádí se interaktivní vykreslení</param>
            internal void Fill(InteractiveControl parent, IEnumerable<IInteractiveItem> items, bool drawAllItems, bool interactive)
            {
                BoundsInfo boundsInfo = BoundsInfo.CreateForParent(parent);
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

                    // Prvek vložíme do BoundsInfo, aby nám mohl počítat jeho souřadnice:
                    boundsInfo.CurrentItem = item;

                    // Do kterých vrstev budeme kreslit prvek:
                    GInteractiveDrawLayer itemLayers = this.GetLayersToDrawItem(item, parentLayers);

                    // Pokud prvek nebude ani zčásti viditelný (to nám řekne BoundsInfo), tak ho do Draw nebudeme dávat:
                    if (!NeedDrawCurrentItem(boundsInfo, itemLayers)) continue;

                    // Přidat prvek do seznamů pro patřičné vrstvy:
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
                            BoundsInfo boundsInfoChilds = boundsInfo.CurrentChildsBoundsInfo;      // Souřadný systém, který platí uvnitř aktuálního prvku "item" = ten platí pro všechny jeho Childs prvky
                            this.FillFromItems(boundsInfoChilds, item, childItems, itemLayers);    // Čistokrevná rekurze
                        }
                    }

                    if (item.NeedDrawOverChilds)
                        this.AddItemToLayers(boundsInfo, item, itemLayers, true);
                }

                if (parent is IAutoScrollContainer)
                    this.AddScrollItemsToLayers(boundsInfo, parent as IAutoScrollContainer);
            }
            /// <summary>
            /// Určí, zda prvek <see cref="BoundsInfo.CurrentItem"/> bude třeba vykreslit nebo ne.
            /// </summary>
            /// <param name="boundsInfo"></param>
            /// <param name="itemLayers"></param>
            /// <returns></returns>
            private bool NeedDrawCurrentItem(BoundsInfo boundsInfo, GInteractiveDrawLayer itemLayers)
            {
                // Kdo je ve viditelné oblasti, měl by být vykreslen:
                if (boundsInfo.CurrentItemAbsoluteIsVisible) return true;

                // Prvek není ve viditelné oblasti, ale může to být dynamický nebo interaktivní => tyto prvky zařadíme do kreslení vždy:
                //  není jich mnoho, vykreslování to nezatíží, a mohou být kresleny do libovolných míst:
                GInteractiveDrawLayer forceLayers = (GInteractiveDrawLayer)(itemLayers & (GInteractiveDrawLayer.Dynamic | GInteractiveDrawLayer.Interactive));
                return (forceLayers != GInteractiveDrawLayer.None);
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
            /// <summary>
            /// Zajistí přidání Scrollbarů do seznamu objektů k vykreslení do vrstvy <see cref="GInteractiveDrawLayer.Standard"/>.
            /// Metoda je volána na konci zpracování celého objektu (tedy všech jeho Childs, i DrawOverChilds), 
            /// takže Scrollbary budou kresleny úplně nahoru v ose Z.
            /// </summary>
            /// <param name="boundsInfo"></param>
            /// <param name="scrollContainer"></param>
            private void AddScrollItemsToLayers(BoundsInfo boundsInfo, IAutoScrollContainer scrollContainer)
            {
                var scrollBars = scrollContainer?.ScrollBars;
                if (scrollBars == null) return;
                foreach (var scrollBar in scrollBars)
                {
                    boundsInfo.CurrentItem = scrollBar;
                    this.StandardItems.Add(new DrawRequestItem(boundsInfo, scrollBar, GInteractiveDrawLayer.Standard, false));
                }
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
                this.AbsoluteOrigin = boundsInfo.AbsolutePhysicalOriginPoint;
                this.AbsoluteBounds = boundsInfo.CurrentItemAbsoluteBounds;
                this.AbsoluteVisibleBounds = boundsInfo.CurrentItemAbsoluteVisibleBounds;
                this.AbsoluteVisibleClip = boundsInfo.AbsolutePhysicalVisibleBounds;
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
            Rectangle bounds = size.AlignTo(this.ClientItemsRectangle, ContentAlignment.BottomRight).Enlarge(0, 0, -1, -1);
            Graphics graphics = e.GetGraphicsCurrent();
            Color backColor = Color.FromArgb(160, Color.LightSkyBlue);
            Color foreColor = Color.FromArgb(210, Color.Black);
            GraphicsPath gp = Painter.CreatePathRoundRectangle(bounds, 2, 2);

            using (Painter.GraphicsUseSmooth(graphics))
            {
                using (Brush b = Skin.CreateBrushForBackground(bounds, Orientation.Horizontal, GInteractiveState.Enabled, backColor))
                using (Pen p = new Pen(foreColor))
                {
                    graphics.FillPath(b, gp);
                    graphics.DrawPath(p, gp);
                }

                Painter.DrawString(graphics, info, FontInfo.Status, bounds, ContentAlignment.MiddleCenter, foreColor);
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

                AnimationRequest animationRequest = new AnimationRequest();

                try
                {
                    // Zeptáme se ToolTipu, zda má potřebu nějaké animace:
                    if (this._ToolTipNeedTick)
                    {   // Pošleme do ToolTipu Tick, on nám vrátí true, pokud potřebuje překreslit:
                        animationRequest.NeedDrawToolTip = this._ToolTip.AnimateTick();
                    }

                    // Jakákoli jiná animace:
                    if (this._AnimationNeedTick)
                    {
                        lock (_AnimatorTickList)
                        {
                            if (this._AnimationNeedTick)
                            {
                                AnimationArgs args = this._AnimatorTick();
                                if (args.RedrawAll)
                                    animationRequest.NeedDrawAllItems = true;
                                if (args.RedrawItemsCount > 0)
                                    animationRequest.ReDrawItems = args.RedrawItems;
                            }
                        }
                    }
                }
                catch (Exception) { }

                // Zkusíme přejít do Suspend režimu (pokud všichni animátoři mají hotovo):
                this._BackThreadTrySuspend();

                // Kreslení:
                if (animationRequest.IsValid)
                    this._BackThreadRunDraw(animationRequest);

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
        /// <param name="animationRequest"></param>
        private void _BackThreadRunDraw(AnimationRequest animationRequest)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action<AnimationRequest>(this._BackThreadRunDrawGui), animationRequest);
            else
                this._BackThreadRunDrawGui(animationRequest);
        }
        /// <summary>
        /// Vyvolá překreslení tohoto controlu, a to buď kompletně celý objekt anebo pouze vrstu ToolTip, Progress Animace.
        /// Běží v GUI threadu.
        /// </summary>
        /// <param name="animationRequest"></param>
        private void _BackThreadRunDrawGui(AnimationRequest animationRequest)
        {
            if (this._BackThreadRunDrawGuiProcess) return;
            try
            {
                this._BackThreadRunDrawGuiProcess = true;
                DrawRequest drawRequest = new DrawRequest(animationRequest.NeedDrawAllItems, this._NeedDrawFrameBounds, this._MousePaintNeedDraw, this._ToolTip, null);
                if (animationRequest.NeedDrawAllItems)
                    drawRequest.Fill(this, this.ItemsList, true, false);
                else if (animationRequest.HasItems)
                    drawRequest.Fill(this, this.ItemsList, false, false);  // V metodě pro myší redraw se dává poslední argument (interactive) = true...
                drawRequest.InteractiveMode = true;
                this.Draw(drawRequest);
            }
            catch (Exception) { }
            finally
            {
                this._BackThreadRunDrawGuiProcess = false;
            }
        }
        /// <summary>
        /// true = právě probíhá výkon v metodě <see cref="_BackThreadRunDrawGui(AnimationRequest)"/>, nebudeme spouštět její další instanci
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
        /// <summary>
        /// Nastřádaná data pro animační Draw
        /// </summary>
        private class AnimationRequest
        {
            public bool NeedDrawToolTip { get; set; }
            public bool NeedDrawAllItems { get; set; }
            public IInteractiveItem[] ReDrawItems { get; set; }
            public bool HasItems { get { return (this.ReDrawItems != null && this.ReDrawItems.Length > 0); } }
            public bool IsValid { get { return (this.NeedDrawToolTip || this.NeedDrawAllItems || this.HasItems); } }
        }
        #endregion
        #region Animace
        /// <summary>
        /// Metoda přidá danou funkci do seznamu animátorů.
        /// Daná funkce bude volána 1x za 40 milisekund (25x za sekundu).
        /// Funkce má zařídit svoji animaci a vrátit informaci, co dál.
        /// Tato metoda vrací ID animační akce, pomocí kterého je možno tuto animaci externě odvolat, metodou <see cref="AnimationStop(int)"/>.
        /// </summary>
        /// <param name="animatorAction"></param>
        public int AnimationStart(Func<AnimationArgs, AnimationResult> animatorAction)
        {
            if (animatorAction == null) return 0;
            AnimatorInfo animatorInfo = new AnimatorInfo(animatorAction);
            lock (this._AnimatorTickList)
            {
                this._AnimatorTickList.Add(animatorInfo);
            }
            this._BackThreadResume();
            return animatorInfo.Id;
        }
        /// <summary>
        /// Zastaví animaci podle jejího ID. 
        /// ID je přiděleno v <see cref="AnimationStart(Func{AnimationArgs, AnimationResult})"/> a vráceno tam.
        /// </summary>
        /// <param name="animationId"></param>
        public void AnimationStop(int animationId)
        {
            if (animationId <= 0 && this._AnimatorTickList.Count == 0) return;
            lock (this._AnimatorTickList)
            {
                this._AnimatorTickList.RemoveAll(i => i.Id == animationId);
            }
        }
        /// <summary>
        /// Vrací true, pokud animátor má běhat; false pokud není důvod provádět animaci
        /// </summary>
        private bool _AnimationNeedTick { get { return (this._AnimatorTickList.Count > 0); } }
        /// <summary>
        /// Metoda provede všechny zaregistrované animační procedury. 
        /// Vrátí výsledek animace = zda je třeba provést překreslení a kterých objektů.
        /// </summary>
        /// <returns></returns>
        private AnimationArgs _AnimatorTick()
        {
            AnimationArgs args = new AnimationArgs();
            for (int i = 0; i < this._AnimatorTickList.Count; i++)
            {
                AnimatorInfo animatorInfo = this._AnimatorTickList[i];
                AnimationResult result = ((animatorInfo != null) ? animatorInfo.DoTick(args) : AnimationResult.Stop);  // Defenzivně = Stop
                if (result.HasFlag(AnimationResult.DrawAll) && !args.RedrawAll)
                    // Daná animační akce vyžaduje DrawAll => zajistíme to:
                    args.RedrawAll = true;
                if (result.HasFlag(AnimationResult.Stop))
                {   // Daná animační akce již skončila => odeberu si ji ze svého seznamu:
                    this._AnimatorTickList.RemoveAt(i);
                    i--;
                }
            }
            return args;
        }
        /// <summary>
        /// Inicializace systému Animátor
        /// </summary>
        private void _AnimatorInit()
        {
            this._AnimatorTickList = new List<AnimatorInfo>();
        }
        /// <summary>
        /// Pole aktivních animátorů
        /// </summary>
        private List<AnimatorInfo> _AnimatorTickList;
        /// <summary>
        /// Třída zachycující data jednoho žadatele o AnimationTick
        /// </summary>
        private class AnimatorInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="animatorAction"></param>
            public AnimatorInfo(Func<AnimationArgs, AnimationResult> animatorAction)
            {
                this.Action = animatorAction;
                this.Id = ++_LastId;
                this.TickCount = 0L;
            }
            /// <summary>
            /// ID animační akce
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// ID nedávné akce
            /// </summary>
            private static int _LastId = 0;
            /// <summary>
            /// Akce animátora
            /// </summary>
            public Func<AnimationArgs, AnimationResult> Action { get; private set; }
            /// <summary>
            /// Počet dosud odeslaných Ticků, před vyvoláním <see cref="Action"/> se navýší o 1.
            /// Aplikační kód může ve své výkonné animační metodě tuto hodnotu změnit, při příštím vyvolání animační metody dostane hodnotu (+1).
            /// Příklad: animační metoda při jejím vyvolání dostane <see cref="TickCount"/> = 50, a změní ji na 0.
            /// Při příštím volání animační metody bude <see cref="TickCount"/> = 1 (= 0 + 1).
            /// </summary>
            public long TickCount { get; set; }
            /// <summary>
            /// Metoda provede jeden Tick animátoru
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            internal AnimationResult DoTick(AnimationArgs args)
            {
                if (this.Action == null) return AnimationResult.Stop;
                args.TickCount = this.TickCount + 1;
                AnimationResult result = this.Action(args);              // Animační metoda může hodnotu TickCount změnit!
                this.TickCount = args.TickCount;
                return result;
            }
        }
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
                object userData = null;
                this._OnMouseLeave(ref userData);
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
                        this.OnMouseEnterMove();
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
        protected AnimationResult BlockGuiAnimatorTick(AnimationArgs args)
        {
            AnimationResult result = this._BlockGuiAnimatorTick();
            this.BlockGuiAnimatorRunning = (result != AnimationResult.Stop);
            return result;
        }
        /// <summary>
        /// Fyzické provedení jednoho kroku animace BlockGui
        /// </summary>
        /// <returns></returns>
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
                return AnimationResult.DrawAll;
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
                font.SizeRatio = (i == 0 ? 1.75f : 1.45f);
                BlockedGuiTextInfo textInfo = new BlockedGuiTextInfo(text, font);
                textInfo.TextSize = Painter.MeasureString(graphics, text, font);
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
            Rectangle area = new Rectangle(new Point(0, 0), this.ClientItemsSize);
            Color areaColor = Skin.BlockedGui.AreaColor.ApplyOpacity(opacityRatio);
            graphics.FillRectangle(Skin.Brush(areaColor), area);

            // Okno s textem?
            if (this.HasBlockedGuiMessage)
            {
                this.BlockedGuiCheckTexts(graphics, area);

                // Vykreslit pozadí:
                Rectangle bounds = this.BlockedGuiMsgBackgroundBounds.Value;
                Color backColor = Skin.BlockedGui.TextBackColor.ApplyOpacity(opacityRatio);
                Painter.DrawAreaBase(graphics, bounds, backColor, Orientation.Horizontal, GInteractiveState.MouseOver);

                // Vykreslit texty jednotlivých řádků:
                Color textColor = Skin.BlockedGui.TextInfoForeColor.ApplyOpacity(opacityRatio);
                foreach (BlockedGuiTextInfo text in this.BlockedGuiMsgTextInfos)
                    Painter.DrawString(graphics, text.Text, text.Font, text.TextBounds, ContentAlignment.MiddleCenter, textColor);
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
        /// <param name="action1">Akce 1, provádí se pouze pokud aktuálně neběží jiná interaktivní akce a pokud není blokováno GUI. Typicky je to volání zdejší výkonné metody.
        /// Jejím výstupem je true = má se provádět finální akce / false = má se vynechat.</param>
        /// <param name="action2">Akce 2, provádí se vždy. Typicky je to volání base metody.</param>
        /// <param name="final">Akce finální, volá se i po chybě v akcích 1 a 2.</param>
        protected void InteractiveAction(GInteractiveChangeState changeState, Func<bool> action1, Action action2, Action final = null)
        {
            lock (this.InteractiveLock)
            {
                bool runFinal = true;
                try
                {
                    this._FlowToolTipData = null;
                    bool isProcessing = this.InteractiveProcessing;
                    bool isBlocked = this.IsGUIBlocked;
                    using (new InteractiveProcessingScope(this, changeState))
                    {
                        if (!isBlocked && !isProcessing)
                        {   // Akce 1 se provádí jen na neblokovaném GUI, které aktuálně nezpracovává jinou akci:
                            if (action1 != null)
                                runFinal = action1();
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
                    if (runFinal && final != null)
                        final();
                }
            }
        }
        /// <summary>
        /// Obsahuje false v běžném (mrtvém) stavu, obsahuje true pokud právě probíhá obsluha jakékoli interaktivní události
        /// </summary>
        protected bool InteractiveProcessing
        {
            get { return this._InteractiveProcessing; }
            set
            {
                bool oldValue = this._InteractiveProcessing;
                bool newValue = value;
                if (newValue == oldValue)
                {
                    this._InteractiveProcessing = newValue;
                    if (oldValue == true && this.PendingRefresh)
                        this.Refresh();
                }
            }
        }
        private bool _InteractiveProcessing;
        /// <summary>
        /// Akce, která se právě zpracovává
        /// </summary>
        protected GInteractiveChangeState? InteractiveProcessingAction { get; set; }
        /// <summary>
        /// Obsahuje false po proběhnutí metody Refresh(), obsahuje true poté, kdy byl Refresh() požadován ale nebyl proveden -
        /// protože při požadavku na něj byl stav <see cref="InteractiveProcessing"/> = true.
        /// Pak se Refresh provádí ihned po uvolnění <see cref="InteractiveProcessing"/> na false.
        /// Pozor, nastavit lze pouze na true! Resetování na false se provádí jinde.
        /// </summary>
        protected bool PendingRefresh
        {
            get { return this._PendingRefresh; }
            set { if (value && !this._PendingRefresh) this._PendingRefresh = true; }
        }
        private bool _PendingRefresh;
        /// <summary>
        /// Třída zajišťující scope zpracování jedné události v controlu <see cref="InteractiveControl"/>.
        /// Po dobu tohoto scope je nastaveno <see cref="InteractiveControl.InteractiveProcessing"/> = true.
        /// </summary>
        protected class InteractiveProcessingScope : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="control"></param>
            public InteractiveProcessingScope(InteractiveControl control) : this(control, GInteractiveChangeState.None)
            { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="control"></param>
            /// <param name="state"></param>
            public InteractiveProcessingScope(InteractiveControl control, GInteractiveChangeState state)
            {
                this._Control = control;
                this._OldProcessing = control.InteractiveProcessing;
                this._OldProcessingAction = control.InteractiveProcessingAction;
                this._Control.InteractiveProcessing = true;
                this._Control.InteractiveProcessingAction = state;
            }
            private InteractiveControl _Control;
            private bool _OldProcessing;
            private GInteractiveChangeState? _OldProcessingAction;
            void IDisposable.Dispose()
            {
                this._Control.InteractiveProcessingAction = this._OldProcessingAction;
                this._Control.InteractiveProcessing = this._OldProcessing;
                this._Control = null;
            }
        }
        /// <summary>
        /// Objekt sloužící jako zámek pro interaktivní procesy
        /// </summary>
        protected object InteractiveLock;
        #endregion
        #region Implementace IInteractiveParent : on totiž InteractiveControl je umístěn jako Parent ve svých IInteractiveItem
        UInt32 IInteractiveParent.Id { get { return 0; } }
        InteractiveControl IInteractiveParent.Host { get { return this; } }
        IInteractiveParent IInteractiveParent.Parent { get { return null; } set { } }
        IEnumerable<IInteractiveItem> IInteractiveParent.Childs { get { return this.ChildItems; } }
        Size IInteractiveParent.ClientSize { get { return this.ClientSize; } }
        void IInteractiveParent.Repaint() { this.Repaint(); }
        void IInteractiveParent.Repaint(GInteractiveDrawLayer repaintLayers) { this.Repaint(repaintLayers); }
        #endregion
        #region Implementace IInteractiveHost
        IInteractiveItem IInteractiveHost.FocusedItem { get { return this._FocusedItem; } set { this._FocusedItem = value; } }
        IInteractiveItem IInteractiveHost.FocusedItemPrevious { get { return this._FocusedItemPrevious; } set { this._FocusedItemPrevious = value; } }
        void IInteractiveHost.SetFocusToItem(IInteractiveItem focusedItem) { SetFocusToItem(focusedItem); }
        void IInteractiveHost.SetAutoScrollContainerToItem(IInteractiveItem item) { SetAutoScrollContainerToItem(item); }
        #endregion
    }
    #region class AnimationArgs : Data pro jeden animační krok v jednom objektu
    /// <summary>
    /// AnimationArgs : Data pro jeden animační krok v jednom objektu
    /// </summary>
    public class AnimationArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AnimationArgs()
        {
            this._RedrawAll = false;
            this._RedrawItems = new Dictionary<uint, IInteractiveItem>();
        }
        /// <summary>
        /// Počet uplynulých ticků v rámci jednoho aktuálního animátoru.
        /// Systém tuto hodnotu udržuje platnou (tj. před odesláním ticku tuto hodnotu navýší), a do každého animátoru posílá odpovídající hodnotu.
        /// Pokud animátor hodnotu změní, bude uložena do příštího ticku, před ním bude zase inkrementovaná a odeslaná do animátoru.
        /// </summary>
        public long TickCount { get; set; }
        /// <summary>
        /// Požadavek na překreslení celého controlu. Lze nastavit pouze na true; nelze shodit na false.
        /// </summary>
        public bool RedrawAll
        {
            get { return this._RedrawAll; }
            set { this._RedrawAll |= value; }
        }
        private bool _RedrawAll;
        /// <summary>
        /// Počet prvků k překreslení = souhrn ze všech animací
        /// </summary>
        public int RedrawItemsCount { get { return this._RedrawItems.Count; } }
        /// <summary>
        /// Pole prvků, které se mají překreslit jako výsledek animačního kroku
        /// </summary>
        public IInteractiveItem[] RedrawItems { get { return this._RedrawItems.Values.ToArray(); } }
        private Dictionary<uint, IInteractiveItem> _RedrawItems;
        /// <summary>
        /// Přidá jeden daný prvek do pole prvků k překreslení
        /// </summary>
        /// <param name="item"></param>
        public void AddRedrawItem(IInteractiveItem item)
        {
            if (item != null && !this._RedrawItems.ContainsKey(item.Id))
                this._RedrawItems.Add(item.Id, item);
        }
    }
    #endregion
    #region enum MouseMoveDragState
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
        /// Myš je zmáčknuta a je v režimu Draw = kreslení. Tento režim je aktivní od okamžiku MouseDown, nečeká se na pohyb mimo startovní prostor.
        /// Režim Draw je podmíněn stavem controlu i stavem konkrétního prvku, kde došlo ke stisknutí myši.
        /// V tomto stavu se již provádí vykreslování obrazce z bodu MouseDown do bodu MouseCurrent.
        /// </summary>
        Paint,
        /// <summary>
        /// Myš je zmáčknutá, ale její souřadnice jsou uvnitř prostoru, v němž se malé pohyby myši ignorují
        /// </summary>
        Wait,
        /// <summary>
        /// Myš je zmáčknutá, pohybuje se, a právě nyní se dostala mimo prostor odpovídající hodnotě <see cref="Wait"/>.
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
        DragFrame,
        /// <summary>
        /// Myš je zmáčknutá a pohybuje se, již je mimo startovní prostor,
        /// prvek nepodporuje Mouse: Paint ani Drag ani Frame, bude dostávat události MouseMove a MouseUp
        /// </summary>
        CallItem
    }
    #endregion
    #endregion
    #region class ActivePositionInfo : Pracovní třída pro vyhledání prvku <see cref="IInteractiveItem"/> a seznamu jeho parentů
    /// <summary>
    /// <see cref="ActivePositionInfo"/> : Pracovní třída pro vyhledání prvku <see cref="IInteractiveItem"/> a seznamu jeho parentů
    /// </summary>
    internal class ActivePositionInfo
    {
        #region Konstruktor, základní proměnné
        private ActivePositionInfo(Point mouseAbsolutePoint)
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
        /// Na první pozici [0] je první prvek nejblíže <see cref="InteractiveControl"/>,
        /// na poslední pozici je nejvyšší prvek.
        /// </summary>
        public ActiveItemInfo[] Items { get; private set; }
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
        protected ActiveItemInfo Item { get; private set; }
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
        /// Pozice myši v absolutních souřadnicích controlu <see cref="InteractiveControl"/>
        /// </summary>
        public Point MouseAbsolutePoint { get; protected set; }
        /// <summary>
        /// Čas prvního eventu (když byla instance <see cref="ActivePositionInfo"/> vytvořena)
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
            if (foundIndex < 0) return MouseMoveDragState.CallItem;            // Nenašli jsme ani prvek pro Drag, ani pro Frame => budeme posílat prvku události MouseMove a MouseUp.

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
        /// <param name="parent"></param>
        /// <param name="items"></param>
        /// <param name="prevItem"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="priorityItems"></param>
        /// <returns></returns>
        public static ActivePositionInfo FindItemAtPoint(InteractiveControl parent, List<IInteractiveItem> items, ActivePositionInfo prevItem, Point mouseAbsolutePoint, bool withDisabled, params IInteractiveItem[] priorityItems)
        {
            return _FindItemAtPoint(parent, items, prevItem, mouseAbsolutePoint, withDisabled, priorityItems);
        }
        /// <summary>
        /// Returns a new GCurrentItem object for topmost interactive item on specified point
        /// Prefered current item above other items.
        /// Accept disabled items (by parameter withDisabled).
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="itemList"></param>
        /// <param name="prevItem"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="priorityItems"></param>
        /// <returns></returns>
        private static ActivePositionInfo _FindItemAtPoint(InteractiveControl parent, List<IInteractiveItem> itemList, ActivePositionInfo prevItem, Point mouseAbsolutePoint, bool withDisabled, IInteractiveItem[] priorityItems)
        {
            ActivePositionInfo currItem = new ActivePositionInfo(mouseAbsolutePoint);
            List<IInteractiveItem> items = _CreateJoinItems(itemList, priorityItems);    // Tady vznikne new instance Listu !   Tady se NEPŘIDÁVÁJÍ AutoScroll Srollbary. To až později v metodě _AddAutoScrollItemsToList().
            ActiveItemInfo[] holdItems = ((prevItem != null && prevItem.HasItem) ? prevItem.Items : null);
            currItem._FindItemAtPoint(parent, items, mouseAbsolutePoint, withDisabled, holdItems);
            return currItem;
        }
        /// <summary>
        /// Vrátí seznam položek první úrovně k scanování
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="priorityItems"></param>
        /// <returns></returns>
        private static List<IInteractiveItem> _CreateJoinItems(List<IInteractiveItem> itemList, IInteractiveItem[] priorityItems)
        {
            List<IInteractiveItem> joinList = new List<IInteractiveItem>();
            if (itemList != null && itemList.Count > 0)
                joinList.AddRange(itemList);
            if (priorityItems != null && priorityItems.Length > 0)
                joinList.AddRange(priorityItems.Where(p => p != null));
            return joinList;
        }
        /// <summary>
        /// Hledá prvek, který je aktivní na dané souřadnici v daném seznamu prvků.
        /// Prvek musí být viditelný, musí být (enabled nebo se hledají i non-enabled prvky), musí být aktivní na dané souřadnici.
        /// Metoda prohledává stejným způsobem i Child prvky nalezeného prvku, pokud existují.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="items"></param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="withDisabled"></param>
        /// <param name="holdItems"></param>
        /// <returns></returns>
        private void _FindItemAtPoint(InteractiveControl parent, List<IInteractiveItem> items, Point mouseAbsolutePoint, bool withDisabled, ActiveItemInfo[] holdItems)
        {
            List<ActiveItemInfo> foundList = new List<ActiveItemInfo>();
            Dictionary<uint, IInteractiveItem> scanDict = new Dictionary<uint, IInteractiveItem>();
            Queue<ActiveItemInfo> holdQueue = (holdItems == null ? null : new Queue<ActiveItemInfo>(holdItems));   // Fronta "přidržených" prvků (od posledně)
            BoundsInfo boundsInfo = BoundsInfo.CreateForParent(parent);
            bool run = true;
            _AddAutoScrollItemsToList(parent, items);
            while (run)
            {
                run = false;
                bool isFound = false;
                IInteractiveItem foundItem = null;
                if (holdQueue != null && holdQueue.Count > 0)
                {   // Máme hledat nejprve v "přidržených" prvcích?
                    ActiveItemInfo fip = holdQueue.Dequeue();
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
                    foundList.Add(new ActiveItemInfo(this, foundItem, boundsInfo));

                    // Pokud prvek má Childs, projdeme je rovněž, hned v další smyčce:
                    items = _CreateChildItemsForItem(foundItem);
                    if (items != null && items.Count > 0)
                    {
                        // Vyměníme souřadný systém za systém platný v rámci daného Itemu jakožto containeru:
                        boundsInfo = boundsInfo.CurrentChildsBoundsInfo;
                        run = true;
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
        /// Metoda sestaví a vrátí List obsahující všechny Child prvky daného interaktivního prvku.
        /// Tato metoda tedy vytváří new instanci Listu.
        /// Pokud daný container implementuje <see cref="IAutoScrollContainer"/> a to je aktivní, 
        /// pak do vygenerovaného seznamu prvků přidá i jeho Scrollbary.
        /// Může vrátit null.
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        private static List<IInteractiveItem> _CreateChildItemsForItem(IInteractiveItem container)
        {
            IEnumerable<IInteractiveItem> childs = container.Childs;
            // Tady si dovolím nepřesnost: pokud prvek (container) nemá prvky Childs, pak nebudu ani testovat, zda má nastavený AutoScroll.
            // Neměl by mít, protože ten je aktivní jen když nějaký jeho Child prvek přesahuje z fyzického vizuálního prostoru - a protože není, tak nepřesahuje:
            if (childs == null) return null;
            List<IInteractiveItem> items = childs.ToList();
            _AddAutoScrollItemsToList(container, items);
            return items;
        }
        /// <summary>
        /// Metoda zjistí, zda daný container implementuje <see cref="IAutoScrollContainer"/>, 
        /// a pokud ano a pokud je aktivní, pak do daného Listu <paramref name="items"/> přidá jeho Scrollbary <see cref="IAutoScrollContainer.ScrollBars"/>.
        /// Tady se skutečně do předaného Listu PŘIDÁVAJÍ nové prvky!
        /// Předaný List <paramref name="items"/> tedy nikdy nesmí být tentýž, který je obsažen jako permanentní ve vizuální vrstvě!
        /// </summary>
        /// <param name="container"></param>
        /// <param name="items"></param>
        private static void _AddAutoScrollItemsToList(object container, List<IInteractiveItem> items)
        {
            if (container != null && container is IAutoScrollContainer)
            {
                IAutoScrollContainer autoScrollContainer = container as IAutoScrollContainer;
                if (autoScrollContainer.AutoScrollActive)
                    items.AddRange(autoScrollContainer.ScrollBars);
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
        private bool _IsCycled(IInteractiveItem foundItem, Dictionary<uint, IInteractiveItem> scanDict, List<ActiveItemInfo> foundList)
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
        private static bool _TryFindItemInList(List<IInteractiveItem> items, BoundsInfo boundsInfo, Point mouseAbsolutePoint, bool withDisabled, IInteractiveItem preferredItem, out IInteractiveItem foundItem)
        {
            Point mouseRelativeVirtualPoint = boundsInfo.GetRelativePoint(mouseAbsolutePoint, false);   // Pro prvky, které jsou OnPhysicalBounds (typicky Scrollbary od AutoScrollContaineru)
            Point mouseRelativePhysicalPoint = boundsInfo.GetRelativePoint(mouseAbsolutePoint, true);   // Pro běžné prvky, na virtuálních souřadnicích

            for (int idx = items.Count - 1; idx >= 0; idx--)
            {   // Hledáme v poli prvků od konce = vizuálně od nejvýše vykresleného prvku:
                IInteractiveItem item = items[idx];
                if ((preferredItem == null || Object.ReferenceEquals(preferredItem, item))
                    && item.Is.Visible
                    && (withDisabled || item.Is.Enabled)
                    && (item.Is.OnPhysicalBounds ? item.IsActiveAtPoint(mouseRelativePhysicalPoint) : item.IsActiveAtPoint(mouseRelativeVirtualPoint)))
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
        public static void MapExchange(ActivePositionInfo prev, ActivePositionInfo next, out List<ActiveItemInfo> leaveList, out List<ActiveItemInfo> enterList)
        {
            leaveList = new List<ActiveItemInfo>();
            enterList = new List<ActiveItemInfo>();

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
        /// <param name="parent"></param>
        /// <param name="items"></param>
        /// <param name="frameBounds"></param>
        /// <param name="filterScan"></param>
        /// <param name="filterAccept"></param>
        /// <returns></returns>
        public static Tuple<IInteractiveItem, Rectangle>[] FindItemsAtBounds(InteractiveControl parent, List<IInteractiveItem> items, Rectangle frameBounds, Func<IInteractiveItem, Rectangle, bool> filterScan, Func<IInteractiveItem, Rectangle, bool> filterAccept)
        {
            BoundsInfo boundsInfo = BoundsInfo.CreateForParent(parent);

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
                    Rectangle currentItemBounds = currentBoundsInfo.CurrentItemAbsoluteVisibleBounds;
                    if (!frameBounds.IntersectsWith(currentItemBounds)) continue;

                    if ((!hasFilterScan || (hasFilterScan && filterScan(currentItem, currentItemBounds))))
                    {   // Scanovat prvek do hloubky:
                        IEnumerable<IInteractiveItem> currentChilds = currentItem.Childs;
                        if (currentChilds != null)
                            scanQueue.Enqueue(new Tuple<BoundsInfo, IEnumerable<IInteractiveItem>>(currentBoundsInfo.CurrentChildsBoundsInfo, currentChilds));
                    }

                    if (!hasFilterAccept || (hasFilterAccept && filterAccept(currentItem, currentItemBounds)))
                        // Akceptovat prvek do výběru:
                        resultList.Add(new Tuple<IInteractiveItem, Rectangle>(currentItem, currentItemBounds));
                }
            }
            return resultList.ToArray();
        }
        #endregion
        #region class ActiveItemInfo : Informace o jednom nalezeném prvku a jeho souřadném systému
        /// <summary>
        /// <see cref="ActiveItemInfo"/> : Informace o jednom nalezeném prvku a jeho souřadném systému
        /// </summary>
        internal class ActiveItemInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="item"></param>
            /// <param name="boundsInfo"></param>
            public ActiveItemInfo(ActivePositionInfo owner, IInteractiveItem item, BoundsInfo boundsInfo)
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
            protected ActivePositionInfo Owner { get; private set; }
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
            public Rectangle ItemAbsBounds { get { return this.BoundsInfo.CurrentItemAbsoluteBounds; } }
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
        public static bool IsDoubleClick(ActivePositionInfo lastItem, ActivePositionInfo currItem)
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
        public static bool IsLongClick(DateTime? downTime, ActivePositionInfo currItem)
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
        internal void CallAfterStateChanged(GInteractiveChangeStateArgs stateArgs, ref bool recurseToSolver)
        {
            int i = this.Count - 1;
            while (i >= 0)
            {   // Akci budu (možná) volat pro všechny prvky, dokud:
                //  a) není požadováno recurseToSolver
                //  b) je nastaveno, že prvek akci vyřešil:
                this.Items[i].Item.AfterStateChanged(stateArgs);
                if (recurseToSolver && stateArgs.ActionIsSolved) recurseToSolver = false;     // Pokud je požadováno řešení i do hloubky, a zde bylo vyřešeno, pak požadavek shodím na false
                if (!recurseToSolver) break;     // Pokud už není požadavek na rekurzní řešení, skončíme.
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
