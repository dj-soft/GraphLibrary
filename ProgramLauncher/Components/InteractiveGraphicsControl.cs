using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DjSoft.Tools.ProgramLauncher.Data;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    /// <summary>
    /// Interaktivní control s optimalizovaným vykreslením grafiky
    /// </summary>
    public class InteractiveGraphicsControl : GraphicsControl
    {
        #region Konstrukce a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InteractiveGraphicsControl()
        {
            __DataItems = new ChildItems<InteractiveGraphicsControl, InteractiveItem>(this);
            __DataItems.CollectionChanged += __DataItems_CollectionChanged;
            _InitInteractivity();
            App.CurrentAppearanceChanged += _CurrentAppearanceChanged;
            App.CurrentLayoutSetChanged += _CurrentLayoutSetChanged;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            App.CurrentLayoutSetChanged -= _CurrentLayoutSetChanged;
            App.CurrentAppearanceChanged -= _CurrentAppearanceChanged;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Po změně palety provedu překreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentAppearanceChanged(object sender, EventArgs e)
        {
            this.Draw();
        }
        /// <summary>
        /// Po změně layoutu provedu překreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentLayoutSetChanged(object sender, EventArgs e)
        {
            _ResetItemLayout();
            this.Draw();
        }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky
        /// </summary>
        private ChildItems<InteractiveGraphicsControl, InteractiveItem> __DataItems;
        #endregion
        #region Interaktivita
        #region Interaktivita nativní = eventy controlu
        /// <summary>
        /// Inicializace nativních eventů myši
        /// </summary>
        private void _InitInteractivity()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseLeave += _MouseLeave;
            this.KeyDown += _KeyDown;

            this._MouseDragReset();

            this.CursorTypeMouseDrag = CursorTypes.SizeAll;
            this.CursorTypeMouseOn = CursorTypes.Hand;
            this.CursorTypeMouseFrame = CursorTypes.Cross;
        }
        /// <summary>
        /// Nativní event MouseEnter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseEnter(object sender, EventArgs e)
        {
            var mouseState = _CreateMouseState();
            _MouseMove(mouseState);
        }
        /// <summary>
        /// Nativní event MouseMove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            var mouseState = _CreateMouseState();
            _MouseMove(mouseState);
        }
        /// <summary>
        /// Nativní event MouseDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            var mouseState = _CreateMouseState();
            _MouseDown(mouseState);
        }
        /// <summary>
        /// Nativní event MouseUp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            var mouseState = _CreateMouseState();
            switch (__CurrentMouseDragState)
            {
                case MouseDragProcessState.MouseDragItem:
                    _MouseDragEnd(mouseState);
                    break;
                case MouseDragProcessState.MouseFrameArea:
                    _MouseFrameEnd(mouseState);
                    break;
                case MouseDragProcessState.Cancelled:
                    _MouseDragReset();
                    break;
                default:
                    _MouseClickUp(mouseState);
                    break;
            }
            _MouseUpEnd(mouseState);
        }
        /// <summary>
        /// Nativní event MouseLeave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            var mouseState = _CreateMouseState(true);
            _MouseMoveNone(mouseState, false);
        }
        /// <summary>
        /// Nativní event KeyDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _KeyDown(object sender, KeyEventArgs e)
        {
            if (__CurrentMouseDragState == MouseDragProcessState.MouseDragItem || __CurrentMouseDragState == MouseDragProcessState.MouseFrameArea)
                _MouseDragKeyDown(e);
        }
        #endregion
        #region Získání informace o pozici myši, o prvku pod myší a o adrese pod myší
        /// <summary>
        /// Vytvoří a korektně naplní objekt <see cref="MouseState"/> a vrátí jej
        /// </summary>
        /// <returns></returns>
        private MouseState _CreateMouseState(bool? isLeave = null)
        {
            var mouseState = MouseState.CreateCurrent(this);
            _RefreshMouseState(mouseState);
            return mouseState;
        }
        /// <summary>
        /// Znovu najde a naplní do daného objektu <see cref="MouseState"/> nynější 
        /// interaktivní prvek <see cref="MouseState.InteractiveItem"/> a pozici Cell <see cref="MouseState.InteractiveCell"/>
        /// podle aktuální pozice myši v prvku uložené.
        /// </summary>
        /// <returns></returns>
        private void _RefreshMouseState(MouseState mouseState)
        {
            if (mouseState.IsOnControl)
            {
                mouseState.InteractiveItem = _GetMouseItem(mouseState);
                if (mouseState.Buttons != MouseButtons.None)
                    mouseState.InteractiveCell = _GetMouseCell(mouseState);
            }
        }
        /// <summary>
        /// Najde nejvyšší aktivní prvek pro danou pozici myši
        /// </summary>
        /// <param name="mouseState"></param>
        /// <returns></returns>
        private InteractiveItem _GetMouseItem(MouseState mouseState)
        {
            Point virtualPoint = this.GetVirtualPoint(mouseState.LocationControl);
            var items = __DataItems;
            int count = items.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (items[i].IsActiveOnVirtualPoint(virtualPoint))
                    return items[i];
            }
            return null;
        }
        /// <summary>
        /// Najde cílovou buňku pro danou souřadnici myši. Tato buňka může být určena i pro pozici, kde není žádný interaktivní prvek.
        /// </summary>
        /// <param name="mouseState"></param>
        /// <returns></returns>
        private InteractiveMap.Cell _GetMouseCell(MouseState mouseState)
        {
            var interactiveMap = __InteractiveMap;
            if (interactiveMap is null) return null;

            Point virtualPoint = this.GetVirtualPoint(mouseState.LocationControl);
            return interactiveMap.GetCellAtPoint(virtualPoint, true);
        }
        #endregion
        #region Interaktivita logicky řízená
        /// <summary>
        /// Nativní event MouseMove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMove(MouseState mouseState)
        {
            bool lastNone = (__CurrentMouseButtons == MouseButtons.None);
            bool currNone = (mouseState.Buttons == MouseButtons.None);

            if (lastNone && currNone)
            {   // Stále pohyb bez stisknutého tlačítke:
                _MouseMoveNone(mouseState);
            }
            else if (lastNone && !currNone)
            {   // Dříve bez tlačítka, nyní s tlačítkem (minuli jsme MouseDown):
                _MouseDown(mouseState);
                _MouseDragMove(mouseState);
            }
            else if (!currNone)
            {   // Nyní s tlačítkem
                _MouseDragMove(mouseState);
            }
        }
        /// <summary>
        /// Pohyb myši bez stisknutého tlačítka
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseMoveNone(MouseState mouseState)
        {
            _MouseMoveNone(mouseState, true);
        }
        /// <summary>
        /// Pohyb myši bez stisknutého tlačítka
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="isOnControl"></param>
        private void _MouseMoveNone(MouseState mouseState, bool isOnControl)
        {
            bool isChange = _MouseMoveCurrentExchange(mouseState, mouseState.InteractiveItem, InteractiveState.MouseOn, isOnControl);
            bool useMouseTrack = true;
            if (useMouseTrack || isChange)
                this.Draw();
        }
        /// <summary>
        /// Stisk tlačítka myši
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDown(MouseState mouseState)
        {
            _MouseMoveCurrentExchange(mouseState, mouseState.InteractiveItem, InteractiveState.MouseDown, true);
            __CurrentMouseDownState = mouseState;
            __CurrentMouseButtons = mouseState.Buttons;
            _MouseDragDown(mouseState, mouseState.InteractiveItem);

            this.Draw();
        }
        /// <summary>
        /// Uvolnění tlačítka myši, nikoli v režimu MouseDrag = provádí se MouseClick, pokud máme nalezený Item nebo na ploše Area
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseClickUp(MouseState mouseState)
        {
            // Odlišení kliknutí nebo doubleclicku (zde ale nejsme v režimu DragAndDrop, to si řeší _MouseUp => _MouseDragEnd):
            var currentItem = __CurrentMouseItem;
            if (currentItem is null)
                _MouseAreaClick(mouseState);
            else
                _MouseItemClick(mouseState, currentItem);

            // Řešení MouseUp
            __PreviousMouseDownState = __CurrentMouseDownState;      // Aktuální stav myši odzálohuji do Previous, kvůli případnému doubleclicku

            // Ukončení Mouse DragAndDrop, které ani nezačalo (proto jsme tady):
            _MouseDragReset();
        }
        /// <summary>
        /// Po provedení MouseUp jak v režimu Click, tak i Drag.
        /// Najde prvek aktuálně pod myší se nacházející a zajistí <see cref="_MouseMoveCurrentExchange(MouseState, InteractiveItem, InteractiveState, bool)"/> a <see cref="Draw"/>.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseUpEnd(MouseState mouseState)
        {
            __CurrentMouseDownState = null;                          // Aktuálně nemáme myš Down
            __CurrentMouseButtons = MouseButtons.None;               // Ani žádný button

            // Znovu najdeme prvek pod myší:
            _RefreshMouseState(mouseState);
            _MouseMoveCurrentExchange(mouseState, mouseState.InteractiveItem, InteractiveState.MouseOn, mouseState.IsOnControl);

            // Vykreslíme:
            this.Draw();
        }
        /// <summary>
        /// Dokončení myšokliku = uvolnění tlačítka myši, když nebyl proces Drag, a pod myší je prvek.
        /// Zde se detekuje Click/DoubleClick.
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="currentItem"></param>
        private void _MouseItemClick(MouseState mouseState, InteractiveItem currentItem)
        {
            // Click se volá v době MouseUp, ale v procesu Click nás zajímá mj. tlačítka myši v době MouseDown,
            //  proto do eventu posílám objekt __CurrentMouseDownState (stav myši v době MouseDown) a nikoli currentItem (ten už má Buttons = None):
            _RunInteractiveItemClick(new InteractiveItemEventArgs(currentItem, __CurrentMouseDownState));

            currentItem.InteractiveState = InteractiveState.Enabled;
        }
        /// <summary>
        /// Dokončení myšokliku = uvolnění tlačítka myši, když nebyl proces Drag, a pod myší není žádný prvek.
        /// Zde se detekuje Click/DoubleClick.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseAreaClick(MouseState mouseState)
        {
            // Click se volá v době MouseUp, ale v procesu Click nás zajímá mj. tlačítka myši v době MouseDown,
            //  proto do eventu posílám objekt __CurrentMouseDownState (stav myši v době MouseDown) a nikoli currentItem (ten už má Buttons = None):
            _RunInteractiveAreaClick(new InteractiveItemEventArgs(null, __CurrentMouseDownState));
        }
        /// <summary>
        /// Vyřeší výměnu prvku pod myší (dosavadní prvek je v instanční proměnné <see cref="__CurrentMouseItem"/>,
        /// nový je v parametru <paramref name="currentMouseItem"/>).
        /// Řeší detekci změny, vložení správného interaktivního stavu do <see cref="InteractiveItem.InteractiveState"/>, 
        /// uložení nového do <see cref="__CurrentMouseItem"/>
        /// a vrací true když jde o změnu.
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="currentMouseItem"></param>
        /// <param name="currentState"></param>
        /// <param name="isOnControl"></param>
        /// <returns></returns>
        private bool _MouseMoveCurrentExchange(MouseState mouseState, InteractiveItem currentMouseItem, InteractiveState currentState, bool isOnControl)
        {
            // Pozice myši nad controlem:
            bool lastOnControl = __MouseIsOnControl;
            bool changedOnControl = (isOnControl != lastOnControl);
            __MouseIsOnControl = isOnControl;

            InteractiveItem lastMouseItem = __CurrentMouseItem;
            bool lastExists = (lastMouseItem != null);
            bool currentExists = (currentMouseItem != null);

            if (!lastExists && !currentExists) return changedOnControl;         // Stále mimo prvky

            if (!lastExists && currentExists)
            {   // Ze žádného prvku na nový prvek:
                currentMouseItem.InteractiveState = currentState;
                __CurrentMouseItem = currentMouseItem;
                _RunInteractiveItemMouseEnter(new InteractiveItemEventArgs(currentMouseItem, mouseState));
                return true;
            }

            if (lastExists && !currentExists)
            {   // Z dosavadního prvku na žádný prvek:
                lastMouseItem.InteractiveState = InteractiveState.Enabled;
                _RunInteractiveItemMouseLeave(new InteractiveItemEventArgs(lastMouseItem, mouseState));
                __CurrentMouseItem = null;
                return true;
            }

            // Z dosavadních podmínek je jisté, že máme oba prvky (lastExists && currentExists).
            // Pokud jsou stejné, pohybujeme se nad stále týmž prvkem:
            if (Object.ReferenceEquals(lastMouseItem, currentMouseItem))
            {
                if (currentMouseItem.InteractiveState != currentState)
                {   // Je na něm změna stavu:
                    currentMouseItem.InteractiveState = currentState;
                    return true;
                }
                // Prvek je stejný, ani nemá změnu stavu:
                return changedOnControl;
            }

            // Změna prvku z dosavadního na nový:
            lastMouseItem.InteractiveState = InteractiveState.Enabled;
            _RunInteractiveItemMouseLeave(new InteractiveItemEventArgs(lastMouseItem, mouseState));

            currentMouseItem.InteractiveState = currentState;
            __CurrentMouseItem = currentMouseItem;
            _RunInteractiveItemMouseEnter(new InteractiveItemEventArgs(currentMouseItem, mouseState));

            return true;
        }
        /// <summary>
        /// Aktuální tlačítka myši, zde je i None v době pohybu myši bez tlačítek
        /// </summary>
        private MouseButtons __CurrentMouseButtons;
        /// <summary>
        /// Stav myši (tlačítko a souřadnice) v okamžiku MouseDown při aktuálním stavu MouseDown, pro řízení MouseDrag
        /// </summary>
        private MouseState __CurrentMouseDownState;
        /// <summary>
        /// Stav myši v předchozím MouseDown, pro detekci DoubleClick
        /// </summary>
        private MouseState __PreviousMouseDownState;
        /// <summary>
        /// Aktuální prvek pod myší, s ním se pracuje
        /// </summary>
        private InteractiveItem __CurrentMouseItem;
        /// <summary>
        /// Myš se nachází nad Controlem
        /// </summary>
        private bool __MouseIsOnControl;
        #endregion
        #region Interaktivní proces DragAndDrop
        /// <summary>
        /// Volá se v okamžiku MouseDown a slouží k uložení dat pro případné budoucí zahájení procesu Mouse DragAndDrop
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragDown(MouseState mouseState, InteractiveItem mouseItem)
        {
            __CurrentMouseDragState = MouseDragProcessState.BeginZone;
            __DragVirtualBeginPoint = mouseState;
            __DragVirtualBeginZone = mouseState.LocationControl.GetRectangleFromCenter(6, 6);
            __DragVirtualCurrentPoint = mouseState;
            __MouseDragCurrentDataItem = mouseItem;
        }
        /// <summary>
        /// Pohyb myši když je stisknuté tlačítko = řeší začátek a průběh DragAndDrop
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragMove(MouseState mouseState)
        {
            if (__CurrentMouseDragState == MouseDragProcessState.BeginZone)
            {   // Jsme v procesu čekání na výraznější pohyb myši = odtrhnutí od bodu MouseDown:
                if (!__DragVirtualBeginZone.HasValue || !__DragVirtualBeginZone.Value.Contains(mouseState.LocationControl))
                {   // Začíná Drag:
                    if (!EnabledDrag)
                    {   // Drag není povolen => rovnou přejdeme do stavu Cancelled:
                        _MouseMoveCurrentExchange(mouseState, null, InteractiveState.Enabled, true);
                        __CurrentMouseDragState = MouseDragProcessState.Cancelled;
                        this.Draw();
                    }
                    else if (__MouseDragCurrentDataItem != null)
                    {   // Pokud pod myší je prvek, a pokud se nechá Dragovat:
                        __CurrentMouseDragState = MouseDragProcessState.MouseDragItem;
                        this.CursorType = this.CursorTypeMouseDrag;
                    }
                    else
                    {   // Začal proces Drag, ale není tam prvek = budeme Framovat?
                        __CurrentMouseDragState = MouseDragProcessState.MouseFrameArea;
                        this.CursorType = this.CursorTypeMouseFrame;
                    }
                    __DragVirtualBeginZone = null;
                    __PreviousMouseDownState = null;               // Toto je podklad pro DoubleClick, a ten po Drag neplatí
                }
            }
            // Předchozí blok mohl nastavit aktuální režim __CurrentMouseDragState, proto až nyní následuje switch:
            switch (__CurrentMouseDragState)
            {
                case MouseDragProcessState.MouseDragItem:
                    // Probíhá DragAndDrop: najdeme cílový prvek:
                    __DragVirtualCurrentPoint = mouseState;
                    __MouseDragTargetDataItem = _GetMouseItem(mouseState);
                    this.Draw();
                    break;
                case MouseDragProcessState.MouseFrameArea:
                    // Probíhá MouseFrame: zapíšeme cílový bod a vykreslíme:
                    __DragVirtualCurrentPoint = mouseState;
                    this.Draw();
                    break;
            }
        }
        /// <summary>
        /// Volá se při KeyDown za stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseDragItem"/> nebo <see cref="MouseDragProcessState.MouseFrameArea"/>.
        /// Pokud je stisknut Escape, pak rušíme Drag = nastavíme <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.Cancelled"/> a vyvoláme Draw.
        /// </summary>
        /// <param name="e"></param>
        private void _MouseDragKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && 
                (__CurrentMouseDragState == MouseDragProcessState.BeginZone || 
                 __CurrentMouseDragState == MouseDragProcessState.MouseDragItem ||
                 __CurrentMouseDragState == MouseDragProcessState.MouseFrameArea))
            {
                __CurrentMouseDragState = MouseDragProcessState.Cancelled;
                this.CursorType = CursorTypes.Default;
                this.Draw();
            }
        }
        /// <summary>
        /// Volá se při události MouseUp při stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseDragItem"/>,
        /// tedy když reálně probíhá Mouse DragAndDrop a nyní je dokončováno = Drop.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragEnd(MouseState mouseState)
        {
            if (EnabledDrag)
            {

            }

            _MouseDragReset();
        }
        /// <summary>
        /// Volá se při události MouseUp při stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseFrameArea"/>,
        /// tedy když reálně probíhá Mouse Frame a nyní je dokončováno = FrameSelect.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseFrameEnd(MouseState mouseState)
        {
            if (EnabledDrag)
            {

            }

            _MouseDragReset();
        }
        /// <summary>
        /// Resetuje veškeré příznaky procesu DragAndDrop
        /// </summary>
        private void _MouseDragReset()
        {
            __CurrentMouseDragState = MouseDragProcessState.None;
            __DragVirtualBeginPoint = null;
            __DragVirtualBeginZone = null;
            __DragVirtualCurrentPoint = null;
            __MouseDragCurrentDataItem = null;

            this.CursorType = CursorTypes.Default;
            this.Draw();
        }
        private MouseDragProcessState __CurrentMouseDragState;
        private MouseState __DragVirtualBeginPoint;
        private Rectangle? __DragVirtualBeginZone;
        private MouseState __DragVirtualCurrentPoint;
        /// <summary>
        /// Aktuálně přemísťovaný prvek
        /// </summary>
        private InteractiveItem __CurrentDraggedItem;
        /// <summary>
        /// Prvek, který byl pod myší když začal proces DragAndDrop a je tedy přesouván na jinou pozici
        /// </summary>
        private InteractiveItem __MouseDragCurrentDataItem;
        /// <summary>
        /// Prvek, který je nyní pod myší při přesouvání v procesu DragAndDrop, je tedy cílovým prvkem. Nikdy nejde o <see cref="__MouseDragCurrentDataItem"/>.
        /// </summary>
        private InteractiveItem __MouseDragTargetDataItem;
        /// <summary>
        /// Fyzický stav procesu DragAndDrop
        /// </summary>
        private enum MouseDragProcessState
        {
            None,
            BeginZone,
            MouseDragItem,
            MouseFrameArea,
            Cancelled
        }
        #endregion
        #endregion
        #region Kreslení
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        public override void Draw()
        {
            this._CheckContentSize();
            base.Draw();
        }
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        /// <param name="drawRectangle">
        /// Informace pro kreslící program o rozsahu překreslování.
        /// Nemusí nutně jít o grafický prostor, toto je pouze informace předáváná z parametru metody Draw() do handleru PaintToBuffer().
        /// V servisní třídě se nikdy nepoužije ve významu grafického prostoru.
        /// </param>
        public override void Draw(Rectangle drawRectangle)
        {
            this._CheckContentSize();
            base.Draw(drawRectangle);
        }
        /// <summary>
        /// Systémové kreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            _PaintMousePoints(e);
            _PaintAllDataItems(e);
        }
        /// <summary>
        /// Vykreslí všechny interaktivní prvky v základní vrstvě
        /// </summary>
        /// <param name="e"></param>
        private void _PaintAllDataItems(PaintEventArgs e)
        {
            var mouseState = _CreateMouseState();
            using (PaintDataEventArgs pdea = new PaintDataEventArgs(e, mouseState, this))
            {
                foreach (var dataItem in DataItems)
                {
                    dataItem.Paint(pdea);
                }
                if (this.__MouseDragCurrentDataItem != null && __CurrentMouseDragState == MouseDragProcessState.MouseDragItem)
                {
                    var dragShift = new Point(__DragVirtualCurrentPoint.LocationControl.X - this.__DragVirtualBeginPoint.LocationControl.X, __DragVirtualCurrentPoint.LocationControl.Y - this.__DragVirtualBeginPoint.LocationControl.Y);
                    pdea.MouseDragState = MouseDragState.MouseDragActiveCurrent;
                    pdea.MouseDragCurrentBounds = this.__MouseDragCurrentDataItem.VirtualBounds.Value.GetShiftedRectangle(dragShift);
                    this.__MouseDragCurrentDataItem.Paint(pdea);
                }
            }
        }
        /// <summary>
        /// Reálná barva pozadí, nelze setovat.
        /// Buď je zde barva explicitní <see cref="BackColorUser"/>, anebo barva z aktuální palety <see cref="App.CurrentAppearance"/>.
        /// </summary>
        public override Color BackColor { get { return App.CurrentAppearance.WorkspaceColor.Morph(BackColorUser); } set { } }
        /// <summary>
        /// Explicitní barva pozadí.
        /// Zadaná bůže používat Alfa kanál (= průhlednost), pak pod touto barvou bude prosvítat barva pozadí dle palety.
        /// Lze zadat null = žádná extra barva, čistě barva dle palety.
        /// </summary>
        public Color? BackColorUser { get { return __BackColorUser; } set { __BackColorUser = value; this.Draw(); } } private Color? __BackColorUser;
        /// <summary>
        /// Typ kurzoru aktuálně zobrazený
        /// </summary>
        public CursorTypes CursorType { get { return __CursorType; } set { __CursorType = value; this.Cursor = App.GetCursor(value); } } private CursorTypes __CursorType;
        /// <summary>
        /// Typ kurzoru, který bude aktivován po najetí myší na aktivní prvek
        /// </summary>
        public CursorTypes CursorTypeMouseOn { get { return __CursorTypeMouseOn; } set { __CursorTypeMouseOn = value; } } private CursorTypes __CursorTypeMouseOn;
        /// <summary>
        /// Typ kurzoru, který bude aktivován v procesu MouseDragDrop pro konkrétní prvek
        /// </summary>
        public CursorTypes CursorTypeMouseDrag { get { return __CursorTypeMouseDrag; } set { __CursorTypeMouseDrag = value; } } private CursorTypes __CursorTypeMouseDrag;
        /// <summary>
        /// Typ kurzoru, který bude aktivován v procesu MouseFrame
        /// </summary>
        public CursorTypes CursorTypeMouseFrame { get { return __CursorTypeMouseFrame; } set { __CursorTypeMouseFrame = value; } } private CursorTypes __CursorTypeMouseFrame;
        #endregion

        #region Hrátky - myší ocásek
        private bool MousePointsActive = false;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (MousePointsActive)
            {
                var mousePoints = _MousePoints;
                var point = e.Location;
                int maxCount = 120;
                int lstCount = mousePoints.Count;
                if (lstCount > maxCount)
                    mousePoints.RemoveRange(0, lstCount - maxCount);
                mousePoints.Add(point);
                this.Draw();
            }
        }
        private void _PaintMousePoints(PaintEventArgs e)
        {
            if (!MousePointsActive) return;

            var mousePoints = _MousePoints;
            int lstCount = mousePoints.Count;
            if (lstCount > 0)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

                int alpha = 0;
                Color color = Color.FromArgb(alpha, Color.BlueViolet);
                using (Pen pen = new Pen(color))
                {
                    var lastPoint = mousePoints[0];
                    for (int i = 1; i < lstCount; i++)
                    {
                        var currPoint = mousePoints[i];

                        if (alpha < 255)
                        {
                            alpha++;
                            pen.Color = Color.FromArgb(alpha, Color.BlueViolet);
                        }

                        e.Graphics.DrawLine(pen, lastPoint, currPoint);
                        lastPoint = currPoint;
                    }
                }
            }
        }
        private List<Point> _MousePoints
        {
            get
            {
                if (__MousePoints is null) __MousePoints = new List<Point>();
                return __MousePoints;
            }
        }
        private List<Point> __MousePoints;

        #endregion

        #region Layout prvků
        /// <summary>
        /// Druh výchozího layoutu pro prvky v tomto panelu. Jeden panel má jeden základní layout, konkrétní prvek může definovat svůj vlastní layout.
        /// </summary>
        public DataLayoutKind DefaultLayoutKind { get { return __DefaultLayoutKind; } set { __DefaultLayoutKind = value; _ResetItemLayout(); } } private DataLayoutKind __DefaultLayoutKind;
        /// <summary>
        /// Výchozí layoutu pro prvky v tomto panelu. Jeden panel má jeden základní layout, konkrétní prvek může definovat svůj vlastní layout.
        /// </summary>
        public ItemLayoutInfo DefaultLayout { get { return GetLayout(); } }
        /// <summary>
        /// Metoda vrátí konkrétní layout daného druhu, z právě aktuální sady <see cref="App.CurrentLayoutSet"/>.
        /// Pokud na vstupu bude <paramref name="layoutKind"/> = null, pak se vyhledá základní layout dle <see cref="DefaultLayoutKind"/>.
        /// </summary>
        /// <param name="layoutKind"></param>
        /// <returns></returns>
        public ItemLayoutInfo GetLayout(DataLayoutKind? layoutKind = null) { return App.CurrentLayoutSet.GetLayout(layoutKind ?? DefaultLayoutKind); }
        #endregion
        #region Přepočet layoutu celé sady zdejších prvků = InteractiveMap
        /// <summary>
        /// Metoda vypočítá layout všech zdejších prvků = určí jejich <see cref="InteractiveItem.VirtualBounds"/> a uloží si výslednou celkovou mapu prostoru (obshauje logické pozice buněk, jejich virtuální souřdnice a obsah).
        /// Ta pak slouží jako zdroj pro <see cref="InteractiveGraphicsControl.ContentSize"/> a pro proces Mouse DragAndDrop.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="dataLayout"></param>
        /// <returns></returns>
        private void _RecalculateVirtualBounds()
        {
            __InteractiveMap = RecalculateVirtualBounds(this.__DataItems, this.DefaultLayout);
        }
        /// <summary>
        /// Metoda vypočítá layout všech prvků v dodané kolekci = určí jejich <see cref="InteractiveItem.VirtualBounds"/> a vrátí celkovou mapu prostoru (obshauje logické pozice buněk, jejich virtuální souřdnice a obsah).
        /// Ta pak slouží jako zdroj pro <see cref="InteractiveGraphicsControl.ContentSize"/> a pro proces Mouse DragAndDrop.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="dataLayout"></param>
        /// <returns></returns>
        public static InteractiveMap RecalculateVirtualBounds(IEnumerable<InteractiveItem> items, ItemLayoutInfo dataLayout)
        {
            // Výstupní mapa prvků:
            var standardSize = dataLayout?.CellSize ?? new Size(120, 24);
            InteractiveMap currentMap = new InteractiveMap(standardSize);

            if (items.IsEmpty()) return currentMap;
            items.ForEachExec(i => i.VirtualBounds = null);                              // Resetujeme všechny buňky

            var rows = items.CreateDictionaryArray(i => i.Adress.Y);                     // Dictionary, kde Key = Adress.Y ... tedy index řádku, a Value = pole prvků DataItemBase, které se na tomto řádku nacházejí.

            // Sem budu dávat řádky, které mají v některém svém prvku šířku CellSize = -1 => jde o prvky, které mají mít šířku přes celý řádek, a my musíme nejprve určit šířku celého Content z ostatních řádků:
            //  Jde o List obsahující Tuple, kde Item1 = pozice Y celého řádku, a Item2 = výška celého řádku, a Item3 = pole prvků DataItemBase v tomto řádku (na stejné pozici Adress.Y).
            var springRows = new List<RecalculateSpringInfo>();

            // V první fázi zpracuji souřadnice řádků, které mají všechny exaktní šířku buňky (CellSize.Width >= 0),
            //  a ostatní řádky (obsahující buňky se zápornou šířkou) si odložím do springRows do druhé fáze:
            int adressXLast = items.Max(i => i.Adress.X);                                // Nejvyší pozice X, slouží k nouzovému výpočtu šířky
            int adressYLast = rows.Keys.Max();
            int virtualTop = 0;                                                          // Průběžná souřadnice Top pro aktuálně řešený řádek, průběžně se navyšuje o výšku řádku
            for (int adressY = 0; adressY <= adressYLast; adressY++)
            {   // Řádky se zápornou souřadnicí Y neřeším (budou null).
                // Takto vyřeším i čísla řádků (adressY), na kterých není žádný prvek = jim započítám prázdný prostor na ose Y !!!
                int rowHeight = 0;
                if (rows.TryGetValue(adressY, out var row))
                {   // Na tomto řádku jsou prvky:
                    rowHeight = row.Max(r => r.CellSize.Height);                         // Nejvyšší buňka určuje výšku celého řádku

                    bool hasSpring = row.Any(i => i.CellSize.Width < 0);
                    if (!hasSpring)
                    {   // Všechny prvky mají určenou šířku => zpracujeme je:
                        recalculateVirtualBoundsRow(row, rowHeight, adressY);
                    }
                    else
                    {   // Pokud tento řádek má nějaký prvek se zápornou šířkou prvku (=Spring), pak jej nezpracujeme nyní, ale později = ve druhé fázi:
                        springRows.Add(new RecalculateSpringInfo(adressY, virtualTop, rowHeight, row));
                    }
                }
                else
                {   // Na řádku (adressY) není ani jeden prvek: započítám prázdnou výšku:
                    rowHeight = standardSize.Height;
                }
                virtualTop += rowHeight;
            }

            // V druhé fázi zpracuji řádky Spring, s ohledem na dosud získanou maximální šířku currentMap.ContentSize.Width:
            if (springRows.Count > 0)
            {
                foreach (var springRow in springRows)
                {   // Z uložených dat každého jednotlivého Spring řádku obnovím pozici Y (proměnná virtualTop se sdílí do metody recalculateVirtualBoundsRow()),
                    virtualTop = springRow.VirtualY;
                    // A pak pošlu řádek typu Spring i s jeho výškou ke zpracování.
                    //    Zpracování najde prvky typu Spring a přidělí jim šířku disponibilní do celkové šířky layoutu virtualRight:
                    // Proto se musely nejprve zpracovat řádky obsahující výhradně Fixed prvky (=určily nám sumární pevnou šířku),
                    //    a až poté Spring řádky (pro určení šířky Spring prvků využijeme disponibilní šířku z celého řádku)
                    recalculateVirtualBoundsRow(springRow.Items, springRow.Height, springRow.AdressY);
                }
            }

            return currentMap;


            // Vyřeší souřadnici X ve všech prvcích jednoho řádku:
            void recalculateVirtualBoundsRow(InteractiveItem[] recalcRow, int height, int adrY)
            {
                var columns = recalcRow.CreateDictionaryArray(i => i.Adress.X);          // Klíčem je pozice X. Value je pole prvků DataItemBase[] na stejné adrese X.

                // Určím součet pevných šířek sloupců, a součet Spring šířek (záporné hodnoty):
                int fixedWidth = 0;                                                      // Sumární šířka sloupců s konkrétní šířkou
                int springWidth = 0;                                                     // Sumární šířka sloupců s spring šířkou (jejich záporné hodnoty mohou vyjadřovat % váhu, s jakou se podělí o disponibilní prostor)
                for (int columnX = 0; columnX <= adressXLast; columnX++)
                {
                    int cellWidth = getCellWidth(columns, columnX, false, out var _);    // Pokud výstupem je kladné číslo, pak máme přinejmenším jeden prvek s kladnou šířkou; pokud je jich víc, pak je vrácena Max šířka
                    if (cellWidth >= 0) fixedWidth += cellWidth;
                    else springWidth += cellWidth;
                }
                bool hasSpring = (springWidth < 0);                                      // Pokud nějaký kterýkoli sloupec měl zápornou šířku, pak máme Spring sloupce!
                decimal? springWithRatio = null;                                         // Tady v případě potřeby bude ratio, přepočítávající šířku Spring do disponibilní šířky, OnDemand.

                // Nyní do všech prvků všech sloupců vepíšu jejich šířku a tedy kompletní VirtualBounds:
                int virtualX = 0;
                for (int columnX = 0; columnX <= adressXLast; columnX++)
                {   // Sloupce se zápornou souřadnicí X neřeším (budou null).
                    // Takto vyřeším i čísla sloupců (adressX), na kterých není žádný prvek = jim započítám prázdný defaultní prostor na ose X !!!
                    Point adress = new Point(columnX, adrY);                             // Logická adresa buňky
                    int cellWidth = getCellWidth(columns, columnX, true, out var column);// Pokud výstupem je kladné číslo, pak máme přinejmenším jeden prvek s kladnou šířkou; pokud je jich víc, pak je vrácena Max šířka
                    if (column != null)
                    {   // Na souřadnici X (logická adresa) máme nějaké prvky:
                        if (cellWidth < 0)
                        {   // Sloupec je typu Spring: vypočteme jeho aktuální reálnou šířku:
                            if (!springWithRatio.HasValue)
                            {   // Pokud dosud nebyl určen koeficient přepočtu šířky na virtuální pixely (springWithRatio),
                                //  tak nyní určím poměr pro přepočet disponibilní šířky pro Spring sloupce na jednotku jejich šířky:
                                int virtualRight = currentMap.ContentSize.Width;         // Dosud maximální virtuální souřadnice Right
                                // Pokud by všechny řádky obsahovaly prvek typu Spring, pak currentMap.ContentSize bude == 0, protože pro žádný řádek se nevyvolala metoda recalculateVirtualBoundsRow().
                                // Proto nyní určím šířku celkovou podle standardní velikosti buňky a maximální adresy X (počet prvků * šířka z obecného layoutu):
                                if (virtualRight <= 0)
                                    virtualRight = (adressXLast + 1) * (standardSize.Width);

                                // Přepočtový koeficient mezi springWidth (suma Spring šířek) => volný prostor šířky (virtualRight - fixedWidth):
                                springWithRatio = (springWidth >= 0 ? 0m : ((decimal)(virtualRight - fixedWidth)) / (decimal)springWidth);
                            }

                            // Fyzická šířka v pixelech pro prvek typu Spring:
                            cellWidth = (int)(Math.Round((springWithRatio.Value * (decimal)cellWidth), 0));
                            if (cellWidth < 24) cellWidth = 24;
                        }

                        // Nyní víme vše potřebné a do všech prvků této buňky vložíme jejich VirtualBounds:
                        var virtualBounds = new Rectangle(virtualX, virtualTop, cellWidth, height);
                        foreach (var item in column)
                            item.VirtualBounds = virtualBounds;

                        // A vložím souřadnici (logickou i virtuální) do mapy:
                        currentMap.Add(adress, virtualBounds, column);
                    }
                    else
                    {   // Na souřadnici X (logická adresa) není žádný prvek:
                        // Pokud v tomto řádku neexistují Spring sloupce, pak vložím prázdnou souřadnici do mapy:
                        if (!hasSpring)
                        {
                            var virtualBounds = new Rectangle(virtualX, virtualTop, cellWidth, height);
                            currentMap.Add(adress, virtualBounds, null);
                        }
                        // Pokud v řádku máme Spring sloupce, pak prázdné souřadnice jsou vyplněny Spring sloupci a nemůžeme je tedy obsadit prázdnou buňkou v mapě.
                    }
                    virtualX += cellWidth;
                }
            }


            // Určí a vrátí šířku dané buňky (ze všech v ní přítomných prvků); kladná = Fixed  |  záporná = Spring.
            // Pro pozice (columnIndex) na které nejsou žádné buňky vrací šířku z DataLayout = size.Value.Width
            int getCellWidth(Dictionary<int, InteractiveItem[]> cells, int columnIndex, bool emptyHasStandardWidth, out InteractiveItem[] cell)
            {
                if (cells.TryGetValue(columnIndex, out cell))
                {   // Na tomto sloupci (=buňka) jsou přítomny nějaké prvky:
                    int cellFixedWidth = cell.Select(i => i.CellSize.Width).Max();      // Max šířka ze všech prvků: nejvyšší kladná určuje Fixní šířku
                    int cellSpringWidth = cell.Select(i => i.CellSize.Width).Min();     // Min šířka ze všech prvků: nejmenší záporná určuje Spring šířku
                    return ((cellFixedWidth > 0) ? cellFixedWidth : cellSpringWidth);   // Kladná šířka = Fixed má přednost před zápornou = Spring (jde o souběh více prvků v jedné buňce) = kladná šířka Fixed se vloží i do VirtualBounds sousední buňky Spring
                }

                // Na sloupci (adressX) není ani jeden prvek: započítám prázdnou šířku z layoutu:
                return (emptyHasStandardWidth ? standardSize.Width : 0);
            }
        }
        /// <summary>
        /// Dočasná úschovna pro řádky obsahující buňky, z nichž některá je Spring
        /// </summary>
        private class RecalculateSpringInfo
        {
            public RecalculateSpringInfo(int adressY, int virtualY, int height, InteractiveItem[] items)
            {
                this.AdressY = adressY;
                this.VirtualY = virtualY;
                this.Height = height;
                this.Items = items;
            }
            /// <summary>
            /// Logická adresa Y = pořadové číslo řádku v layoutu
            /// </summary>
            public int AdressY { get; private set; }
            /// <summary>
            /// Pixelová souřadnice Y ve virtuálním prostoru
            /// </summary>
            public int VirtualY { get; private set; }
            /// <summary>
            /// Pixelová výška řádku
            /// </summary>
            public int Height { get; private set; }
            /// <summary>
            /// Všechny prvky v tomto řádku, souhrn ze všech sloupců
            /// </summary>
            public InteractiveItem[] Items { get; private set; }
        }
        #endregion
        #region Public prvky - soupis aktivních prvků, eventy
        /// <summary>
        /// Prvky k zobrazení a interaktivní práci.
        /// Lze setovat, tím se dosavadní prvky zahodí a vloží se prvky z dodaného pole (ale instance dodaného pole se sem neukládá).
        /// </summary>
        public IList<InteractiveItem> DataItems
        { 
            get { return __DataItems; } 
            set 
            {
                __DataItems.Clear();
                if (value != null)
                    __DataItems.AddRange(value);
            } 
        }
        /// <summary>
        /// Přidá sadu interaktivních prvků
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(IEnumerable<InteractiveItem> items)
        {
            __DataItems.AddRange(items);
        }
        /// <summary>
        /// Zruší platnost layoutu jednotlivých prvků přítomných v <see cref="DataItems"/>
        /// </summary>
        public virtual void ResetItemLayout()
        {
            _ResetItemLayout();
        }
        /// <summary>
        /// Událost volaná po změně kolekce <see cref="DataItems"/>. Zajistí invalidaci příznaku platnosti <see cref="ContentAlignment"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void __DataItems_CollectionChanged(object sender, EventArgs e)
        {
            _ResetItemLayout();
        }
        /// <summary>
        /// Metoda zneplatní příznak platné hodnoty <see cref="ContentSize"/> (volá se po přidání / odebrání prvku a po změně layoutu).
        /// Jen nastaví <see cref="__IsContentSizeValid"/> = false. Následně se musí vyhodnotit tato hodnota, viz <see cref="_CheckContentSize()"/>.
        /// To se má volat před kreslením v metodách <see cref="Draw"/> na začátku.
        /// </summary>
        private void _ResetItemLayout()
        {
            __IsContentSizeValid = false;
        }
        /// <summary>
        /// Prověří velikost obsahu podle aktuálního layoutu a případně zajistí překreslení obsahu <see cref="Draw()"/>.
        /// </summary>
        public void RefreshContentSize()
        {
            if (_CheckContentSize(true))
                this.Draw();
        }
        /// <summary>
        /// Metoda zajistí, že velikost <see cref="ContentSize"/> bude platná (bude odpovídat souhrnu velikosti prvků).
        /// Vrátí true pokud došlo reálně ke změně velikosti obsahu.
        /// </summary>
        private bool _CheckContentSize(bool force = false)
        {
            bool isChanged = false;
            if (force || !__IsContentSizeValid)
            {
                var lastContentSize = base.ContentSize;              // base property neprovádí _CheckContentSize() = zdejší metodu!!! Jinak bychom se zacyklili.
                this._RecalculateVirtualBounds();
                var currentContentSize = __InteractiveMap.ContentSize;
                bool isContentSizeChanged = (currentContentSize != lastContentSize);
                __IsContentSizeValid = true;
                if (isContentSizeChanged)                            // Setování a event jen po reálné změně hodnoty
                {
                    this.ContentSize = currentContentSize;           // Setování this volá base, ale nemá žádnou další logiku.
                    this._RunContentSizeChanged();
                    isChanged = true;
                }
            }
            return isChanged;
        }
        /// <summary>
        /// Příznak, že aktuální hodnota <see cref="ContentSize"/> je platná z hlediska přítomných prvků a jejich layoutu
        /// </summary>
        private bool __IsContentSizeValid;
        /// <summary>
        /// Mapa obsahující logické buňky, virtuální adresy a prvky v těchto buňkách.
        /// </summary>
        public InteractiveMap InteractiveMap { get { return __InteractiveMap; } } InteractiveMap __InteractiveMap;
        /// <summary>
        /// Potřebná velikost obsahu. 
        /// Výchozí je null = control zobrazuje to, co je vidět, a nikdy nepoužívá Scrollbary.
        /// Lze setovat hodnotu = velikost zobrazených dat, pak se aktivuje virtuální režim se zobrazením výřezu.
        /// Při změně hodnoty se nenuluje souřadnice počátku <see cref="CurrentWindow"/>, změna velikosti obsahu jej tedy nutně nemusí přesunout na počátek.
        /// </summary>
        public override Size? ContentSize { get { _CheckContentSize(); return base.ContentSize; } set { base.ContentSize = value; } }
        /// <summary>
        /// Událost vyvolaná po změně velikosti <see cref="ContentSize"/>.
        /// </summary>
        public event EventHandler ContentSizeChanged;
        /// <summary>
        /// Metoda vyvolaná po změně velikosti <see cref="ContentSize"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnContentSizeChanged(EventArgs args) { }
        /// <summary>
        /// Vyvolá <see cref="OnContentSizeChanged(EventArgs)"/> a event <see cref="ContentSizeChanged"/>
        /// </summary>
        private void _RunContentSizeChanged()
        {
            EventArgs args = EventArgs.Empty;
            OnContentSizeChanged(args);
            ContentSizeChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Je povoleno provádět ClickItem
        /// </summary>
        public bool EnabledClick { get; set; }
        /// <summary>
        /// Je povoleno provádět DragAndDrop
        /// </summary>
        public bool EnabledDrag { get; set; }

        public event EventHandler<InteractiveItemEventArgs> InteractiveAreaClick;
        protected virtual void OnInteractiveAreaClick(InteractiveItemEventArgs args) { }
        private void _RunInteractiveAreaClick(InteractiveItemEventArgs args)
        {
            OnInteractiveAreaClick(args);
            InteractiveAreaClick?.Invoke(this, args);
        }

        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseEnter;
        protected virtual void OnInteractiveItemMouseEnter(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseEnter(InteractiveItemEventArgs args)
        {
            this.CursorType = this.CursorTypeMouseOn;
            OnInteractiveItemMouseEnter(args);
            InteractiveItemMouseEnter?.Invoke(this, args);
        }

        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseLeave;
        protected virtual void OnInteractiveItemMouseLeave(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseLeave(InteractiveItemEventArgs args)
        {
            this.CursorType = CursorTypes.Default;
            OnInteractiveItemMouseLeave(args);
            InteractiveItemMouseLeave?.Invoke(this, args);
        }

        public event EventHandler<InteractiveItemEventArgs> InteractiveItemClick;
        protected virtual void OnInteractiveItemClick(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemClick(InteractiveItemEventArgs args)
        {
            OnInteractiveItemClick(args);
            InteractiveItemClick?.Invoke(this, args);
        }
        #endregion
    }
    #region class InteractiveMap = mapa interaktivních prvků ve Virtual souřadnicích
    /// <summary>
    /// <see cref="InteractiveMap"/> = mapa interaktivních prvků ve Virtual souřadnicích
    /// </summary>
    public class InteractiveMap
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="standardSize"></param>
        public InteractiveMap(Size standardSize)
        {
            __StandardSize = standardSize;
            __Cells = new List<Cell>();
            __VirtualRight = 0;
            __VirtualBottom = 0;
        }
        /// <summary>
        /// Přidá další buňku pro danou logickou a virtuální souřadnici
        /// </summary>
        /// <param name="adress"></param>
        /// <param name="virtualBounds"></param>
        /// <param name="items"></param>
        public void Add(Point adress, Rectangle virtualBounds, InteractiveItem[] items)
        {
            int right = virtualBounds.Right;
            int bottom = virtualBounds.Bottom;
            if (__VirtualRight < right) __VirtualRight = right;
            if (__VirtualBottom < bottom) __VirtualBottom = bottom;

            __Cells.Add(new Cell(adress, virtualBounds, items, false));
        }
        /// <summary>
        /// Smaže obsah mapy
        /// </summary>
        public void Clear()
        {
            __Cells.Clear();
            __VirtualRight = 0;
            __VirtualBottom = 0;
        }
        /// <summary>
        /// Najde a vrátí buňku na dané virtuální souřadnici. Může vráit null.
        /// </summary>
        /// <param name="virtualPoint">Souřadnice místa, jehož obsah (buňku) hledáme</param>
        /// <param name="includeVoidArea">Pokud na daném místě neexistuje buňka, urči virtuální buňku (typicky pro přesun na nové místo nebo pro vložení nového prvku do prázdného místa)</param>
        /// <returns></returns>
        public Cell GetCellAtPoint(Point virtualPoint, bool includeVoidArea)
        {
            // Nejdřív hledám existující buňku v našem prostoru:
            bool isInContentSize = (virtualPoint.X >= 0 && virtualPoint.X < __VirtualRight && virtualPoint.Y >= 0 && virtualPoint.Y < __VirtualBottom);
            if (isInContentSize)
            {
                var cell = __Cells.FirstOrDefault(c => c.VirtualBounds.Contains(virtualPoint));
                if (cell != null) return cell;
            }

            // Pokud nemám hledat Void prostor, končíme:
            if (!includeVoidArea) return null;

            // Najdu buňky v mém řádku (napravo + nalevo), a v mém sloupci (nahoru + dolů),
            //  pomohou mi možná určit jednu sadu souřadnic
            var myRowCells = __Cells.Where(c => c.VirtualBounds.Top <= virtualPoint.Y && c.VirtualBounds.Bottom > virtualPoint.Y).ToArray();
            var myColumnCells = __Cells.Where(c => c.VirtualBounds.Left <= virtualPoint.X && c.VirtualBounds.Right > virtualPoint.X).ToArray();




            return null;
        }
        /// <summary>
        /// Obsazené buňky a jejich data
        /// </summary>
        public IList<Cell> Cells { get { return __Cells; } }
        /// <summary>
        /// Standardní velikost buňky, slouží k určení buněk mimo obsazený prostor
        /// </summary>
        private Size __StandardSize;
        /// <summary>
        /// Obsazené buňky
        /// </summary>
        private List<Cell> __Cells;
        /// <summary>
        /// Dosud nejvyšší obsazená pozice Right
        /// </summary>
        private int __VirtualRight;
        /// <summary>
        /// Dosud nejvyšší obsazená pozice Bottom
        /// </summary>
        private int __VirtualBottom;
        /// <summary>
        /// Aktuální velikost obsahu
        /// </summary>
        public Size ContentSize { get { return new Size(__VirtualRight, __VirtualBottom); } }
        /// <summary>
        /// Jedna buňka v aktivní ploše
        /// </summary>
        public class Cell
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="adress"></param>
            /// <param name="virtualBounds"></param>
            /// <param name="items"></param>
            public Cell(Point adress, Rectangle virtualBounds, InteractiveItem[] items, bool isOutsideCell)
            {
                __Adress = adress;
                __VirtualBounds = virtualBounds;
                __Items = items;
                __IsOutsideCell = isOutsideCell;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Adress: {Adress}; VirtualBounds: {VirtualBounds}; IsVoidCell: {IsOutsideCell}";
            }
            /// <summary>
            /// Logická adresa buňky, není závislá na Layoutu
            /// </summary>
            public Point Adress { get { return __Adress; } } private Point __Adress;
            /// <summary>
            /// Virtuální souřadnice buňky, kde se právě nachází
            /// </summary>
            public Rectangle VirtualBounds { get { return __VirtualBounds; } } private Rectangle __VirtualBounds;
            /// <summary>
            /// Interaktivní prvky na této pozici
            /// </summary>
            public InteractiveItem[] Items { get { return __Items; } } private InteractiveItem[] __Items;
            /// <summary>
            /// Buňka leží mimo dosavadní prostor buněk.
            /// </summary>
            public bool IsOutsideCell { get { return __IsOutsideCell; } } private bool __IsOutsideCell;
        }
    }
    #endregion
    #region Třídy argumentů
    /// <summary>
    /// Data pro události s prvek <see cref="InteractiveItem"/>
    /// </summary>
    public class InteractiveItemEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        public InteractiveItemEventArgs(InteractiveItem item, MouseState mouseState)
        {
            this.Item = item;
            this.MouseState = mouseState;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Item: {Item}";
        }
        /// <summary>
        /// Prvek
        /// </summary>
        public InteractiveItem Item { get; private set; }
        /// <summary>
        /// Stav myši
        /// </summary>
        public MouseState MouseState { get; private set; }
    }
    #endregion
}
