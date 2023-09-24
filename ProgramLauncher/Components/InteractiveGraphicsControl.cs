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
            App.CurrentAppearanceChanged += _CurrentPaletteChanged;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            App.CurrentAppearanceChanged -= _CurrentPaletteChanged;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Po změně palety provedu překreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentPaletteChanged(object sender, EventArgs e)
        {
            this.Draw();
        }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky
        /// </summary>
        private ChildItems<InteractiveGraphicsControl, InteractiveItem> __DataItems;
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
            _PaintDataItems(e);

        }
        private void _PaintDataItems(PaintEventArgs e)
        {
            var mouseState = MouseState.CreateCurrent(this);
            using (PaintDataEventArgs pdea = new PaintDataEventArgs(e, mouseState, this))
            {
                foreach (var dataItem in DataItems)
                    dataItem.Paint(pdea);
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
            if (__CurrentDraggedItem != null)
                _MouseDragEnd(mouseState);
            else
                _MouseUp(mouseState);
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
            {   // Dříve bez tlačítka, nyní s tlačítkem (minuli jsme NouseDown):
                _MouseDown(mouseState);
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
            this.Draw();
        }
        /// <summary>
        /// Uvolnění tlačítka myši, nikoli v režimu MouseDrag = provádí se MouseClick, pokud máme nalezený Item
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseUp(MouseState mouseState)
        {
            // Řešení kliknutí nebo doubleclicku nebo MouseDragEnd:
            var currentItem = __CurrentMouseItem;
            if (currentItem != null)
                _MouseItemClick(mouseState, currentItem);

            // Řešení MouseUp
            __PreviousMouseDownState = __CurrentMouseDownState;      // Aktuální stav myši odzálohuji do Previous, kvůli případnému doubleclicku
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
        /// <summary>
        /// Pohyb myši když je stisknuté tlačítko = řeší začátek a průběh MouseDrag
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragMove(MouseState mouseState)
        { }
        private void _MouseDragEnd(MouseState mouseState)
        { }
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
        /// Aktuálně přemísťovaný prvek
        /// </summary>
        private InteractiveItem __CurrentDraggedItem;
        /// <summary>
        /// Myš se nachází nad Controlem
        /// </summary>
        private bool __MouseIsOnControl;
        private InteractiveItem __LastMouseItem;
        #endregion
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

        #region Public prvky - soupis aktivních prvků, definice layoutu, eventy
        /// <summary>
        /// Prvky k zobrazení a interaktivní práci
        /// </summary>
        public IList<InteractiveItem> DataItems { get { return __DataItems; } }
        public void AddItems(IEnumerable<InteractiveItem> items)
        {
            __DataItems.AddRange(items);
        }
        /// <summary>
        /// Definice layoutu pro prvky v tomto panelu. Jeden panel má jeden layout.
        /// </summary>
        public DataLayout DataLayout { get { return __DataLayout; } set { _ResetItemLayout(); __DataLayout = value; } }
        private DataLayout __DataLayout;
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
                var lastContentSize = base.ContentSize;              // base property neprovádí _CheckContentSize()
                var currentContentSize = InteractiveItem.RecalculateVirtualBounds(this.__DataItems, this.DataLayout);
                bool isContentSizeChanged = (currentContentSize != lastContentSize);
                __IsContentSizeValid = true;
                if (isContentSizeChanged)                            // Setování a event jen po reálné změně hodnoty
                {
                    this.ContentSize = currentContentSize;
                    this._RunContentSizeChanged();
                }
            }
        }
        /// <summary>
        /// Příznak, že aktuální hodnota <see cref="ContentSize"/> je platná z hlediska přítomných prvků a jejich layoutu
        /// </summary>
        private bool __IsContentSizeValid;
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

        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseEnter;
        protected virtual void OnInteractiveItemMouseEnter(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseEnter(InteractiveItemEventArgs args)
        {
            OnInteractiveItemMouseEnter(args);
            InteractiveItemMouseEnter?.Invoke(this, args);
        }

        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseLeave;
        protected virtual void OnInteractiveItemMouseLeave(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseLeave(InteractiveItemEventArgs args)
        {
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
