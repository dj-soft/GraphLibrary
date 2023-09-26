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
            var mouseState = MouseState.CreateCurrent(this);
            _MouseMove(mouseState);
        }
        /// <summary>
        /// Nativní event MouseMove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            var mouseState = MouseState.CreateCurrent(this);
            _MouseMove(mouseState);
        }
        /// <summary>
        /// Nativní event MouseDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            var mouseState = MouseState.CreateCurrent(this);
            _MouseDown(mouseState);
        }
        /// <summary>
        /// Nativní event MouseUp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            var mouseState = MouseState.CreateCurrent(this);
            if (__CurrentMouseDragState == MouseDragProcessState.MouseDragItem)
                _MouseDragEnd(mouseState);
            else
                _MouseUp(mouseState);
            _MouseUpEnd(mouseState);
        }
        /// <summary>
        /// Nativní event MouseLeave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            var mouseState = MouseState.CreateCurrent(this);
            _MouseMoveNone(mouseState, null, false);
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
            var mouseItem = _GetMouseItem(mouseState);
            _MouseMoveNone(mouseState, mouseItem, true);
        }
        /// <summary>
        /// Pohyb myši bez stisknutého tlačítka
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="mouseItem"></param>
        /// <param name="isOnControl"></param>
        private void _MouseMoveNone(MouseState mouseState, InteractiveItem mouseItem, bool isOnControl)
        {
            bool isChange = _MouseMoveCurrentExchange(mouseState, mouseItem, InteractiveState.MouseOn, isOnControl);
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
            var mouseItem = _GetMouseItem(mouseState);
            _MouseMoveCurrentExchange(mouseState, mouseItem, InteractiveState.MouseDown, true);
            __CurrentMouseDownState = mouseState;
            __CurrentMouseButtons = mouseState.Buttons;
            _MouseDragDown(mouseState, mouseItem);

            this.Draw();
        }
        /// <summary>
        /// Uvolnění tlačítka myši, nikoli v režimu MouseDrag = provádí se MouseClick, pokud máme nalezený Item
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseUp(MouseState mouseState)
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
            var mouseItem = _GetMouseItem(mouseState);
            _MouseMoveCurrentExchange(mouseState, mouseItem, InteractiveState.MouseOn, mouseState.IsOnControl);

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
        private void _MouseAreaClick(MouseState mouseState)
        {
            // Click se volá v době MouseUp, ale v procesu Click nás zajímá mj. tlačítka myši v době MouseDown,
            //  proto do eventu posílám objekt __CurrentMouseDownState (stav myši v době MouseDown) a nikoli currentItem (ten už má Buttons = None):
            _RunInteractiveAreaClick(new InteractiveItemEventArgs(null, __CurrentMouseDownState));
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
            return interactiveMap.GetCellAtPoint(virtualPoint);
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
        private InteractiveItem __LastMouseItem;
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
                    if (__MouseDragCurrentDataItem != null)
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

            if (__CurrentMouseDragState == MouseDragProcessState.MouseDragItem)
            {   // Probíhá DragAndDrop: najdeme cílový prvek:
                __DragVirtualCurrentPoint = mouseState;
                __MouseDragTargetDataItem = _GetMouseItem(mouseState);
                this.Draw();
            }
            if (__CurrentMouseDragState == MouseDragProcessState.MouseFrameArea)
            {   // Probíhá MouseFrame: zapíšeme cílový bod a vykreslíme:
                __DragVirtualCurrentPoint = mouseState;
                this.Draw();
            }
        }
        /// <summary>
        /// Volá se při události MouseUp při stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseDragItem"/>,
        /// tedy když reálně probíhá Mouse DragAndDrop.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragEnd(MouseState mouseState)
        {
            var targetCell = _GetMouseCell(mouseState);


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
            MouseFrameArea
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
            var mouseState = MouseState.CreateCurrent(this);
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
        #region Public prvky - soupis aktivních prvků, eventy
        /// <summary>
        /// Prvky k zobrazení a interaktivní práci
        /// </summary>
        public IList<InteractiveItem> DataItems { get { return __DataItems; } }
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
        /// Metoda zajistí, že velikost <see cref="ContentSize"/> bude platná (bude odpovídat souhrnu velikosti prvků)
        /// </summary>
        private void _CheckContentSize()
        {
            if (!__IsContentSizeValid)
            {
                var lastContentSize = base.ContentSize;              // base property neprovádí _CheckContentSize() = zdejší metodu!!! Jinak bychom se zacyklili.
                __InteractiveMap = InteractiveItem.RecalculateVirtualBounds(this.__DataItems, this.DefaultLayout);
                var currentContentSize = __InteractiveMap.ContentSize;
                bool isContentSizeChanged = (currentContentSize != lastContentSize);
                __IsContentSizeValid = true;
                if (isContentSizeChanged)                            // Setování a event jen po reálné změně hodnoty
                {
                    this.ContentSize = currentContentSize;           // Setování this volá base, ale nemá žádnou další logiku.
                    this._RunContentSizeChanged();
                }
            }
        }
        /// <summary>
        /// Příznak, že aktuální hodnota <see cref="ContentSize"/> je platná z hlediska přítomných prvků a jejich layoutu
        /// </summary>
        private bool __IsContentSizeValid;
        /// <summary>
        /// Mapa obsahující logické buňky, virtuální adresy a prvky v těchto buňkách.
        /// </summary>
        private InteractiveMap __InteractiveMap;
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
}
