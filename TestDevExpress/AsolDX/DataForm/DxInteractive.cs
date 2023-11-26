// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using DevExpress.Utils.Drawing;
using DevExpress.Utils.Extensions;
using DevExpress.XtraCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxInteractivePanel : interaktivní grafický panel
    /// <summary>
    /// <see cref="DxInteractivePanel"/> : interaktivní grafický panel.
    /// Řeší grafické vykreslení prvků a řeší interaktivitu myši a klávesnice.
    /// Pro fyzické vykreslení obsahu prvku volá jeho vlastní metodu, to neřeší panel.
    /// </summary>
    public class DxInteractivePanel : DxBufferedGraphicPanel
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxInteractivePanel()
        {
            __DataItems = new ChildItems<DxInteractivePanel, IInteractiveItem>(this);
            __DataItems.CollectionChanged += __DataItems_CollectionChanged;
            _InitInteractivity();
        }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky
        /// </summary>
        private ChildItems<DxInteractivePanel, IInteractiveItem> __DataItems;
        #endregion
        #region Public prvky - soupis aktivních prvků, eventy
        /// <summary>
        /// Prvky k zobrazení a interaktivní práci.
        /// Lze setovat, tím se dosavadní prvky zahodí a vloží se prvky z dodaného pole (ale instance dodaného pole se sem neukládá).
        /// </summary>
        public IList<IInteractiveItem> DataItems
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
        public void AddItems(IEnumerable<IInteractiveItem> items)
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
        /// Prověří velikost obsahu podle aktuálního layoutu a případně zajistí překreslení obsahu <see cref="DxBufferedGraphicPanel.Draw()"/>.
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

        /// <summary>
        /// Uživatel kliknul (levou nebo pravou myší) v prostoru Controlu, kde není žádný prvek.
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveAreaClick;
        protected virtual void OnInteractiveAreaClick(InteractiveItemEventArgs args) { }
        private void _RunInteractiveAreaClick(InteractiveItemEventArgs args)
        {
            OnInteractiveAreaClick(args);
            InteractiveAreaClick?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel vstoupil myší do konkrétního prvku
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseEnter;
        protected virtual void OnInteractiveItemMouseEnter(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseEnter(InteractiveItemEventArgs args)
        {
            this.CursorType = this.CursorTypeMouseOn;
            OnInteractiveItemMouseEnter(args);
            InteractiveItemMouseEnter?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel odešel myší z konkrétního prvku
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseLeave;
        protected virtual void OnInteractiveItemMouseLeave(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseLeave(InteractiveItemEventArgs args)
        {
            this.CursorType = CursorTypes.Default;
            OnInteractiveItemMouseLeave(args);
            InteractiveItemMouseLeave?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel kliknul (levou nebo pravou myší) v prostoru Controlu, kde je konkrétní prvek.
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveItemClick;
        protected virtual void OnInteractiveItemClick(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemClick(InteractiveItemEventArgs args)
        {
            OnInteractiveItemClick(args);
            InteractiveItemClick?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel provedl DragAndDrop proces, nyní právě ukončil proces.
        /// </summary>
        public event EventHandler<InteractiveDragItemEventArgs> InteractiveItemDragAndDropEnd;
        protected virtual void OnInteractiveItemDragAndDropEnd(InteractiveDragItemEventArgs args) { }
        private void _RunInteractiveItemDragAndDropEnd(InteractiveDragItemEventArgs args)
        {
            OnInteractiveItemDragAndDropEnd(args);
            InteractiveItemDragAndDropEnd?.Invoke(this, args);
        }

        #endregion

    }

    #endregion


    public interface IInteractiveItem : IChildOfParent<DxInteractivePanel>
    { }

    #region MouseState : stav myši v rámci interaktivních prvků
    /// <summary>
    /// Stav myši
    /// </summary>
    public class MouseState
    {
        /// <summary>
        /// Vrátí aktuální stav myši pro daný Control
        /// </summary>
        /// <param name="control"></param>
        /// <param name="isLeave"></param>
        /// <returns></returns>
        public static MouseState CreateCurrent(Control control, bool? isLeave = null)
        {
            DateTime time = DateTime.Now;
            Point locationAbsolute = Control.MousePosition;
            MouseButtons buttons = Control.MouseButtons;
            Keys modifierKeys = Control.ModifierKeys;
            Point locationNative = control.PointToClient(locationAbsolute);
            // Pokud isLeave je true, pak jsme volání z MouseLeave a jsme tedy mimo Control:
            bool isOnControl = (isLeave.HasValue && isLeave.Value ? false : control.ClientRectangle.Contains(locationNative));
            Point locationVirtual = locationNative;
            if (control is DxVirtualPanel virtualControl) locationVirtual = virtualControl.GetVirtualPoint(locationNative);
            return new MouseState(time, locationNative, locationVirtual, locationAbsolute, buttons, modifierKeys, isOnControl);
        }
        /// <summary>
        /// Vrátí stav myši pro dané hodnoty
        /// </summary>
        /// <param name="time"></param>
        /// <param name="LocationControl">Souřadnice myši v koordinátech controlu (nativní prostor)</param>
        /// <param name="locationVirtual">Souřadnice myši v koordinátech virtuálního prostoru vrámci Controlu</param>
        /// <param name="locationAbsolute">Souřadnice myši v absolutních koordinátech (<see cref="Control.MousePosition"/>)</param>
        /// <param name="buttons">Stisknuté buttony</param>
        /// <param name="modifierKeys">Stav kláves Control, Alt, Shift</param>
        /// <param name="isOnControl">true pokud myš se nachází fyzicky nad Controlem</param>
        public MouseState(DateTime time, Point LocationControl, Point locationVirtual, Point locationAbsolute, MouseButtons buttons, Keys modifierKeys, bool isOnControl)
        {
            __Time = time;
            __LocationControl = LocationControl;
            __LocationVirtual = locationVirtual;
            __LocationAbsolute = locationAbsolute;
            __Buttons = buttons;
            __ModifierKeys = modifierKeys;
            __IsOnControl = isOnControl;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Buttons: {__Buttons}; LocationNative: {__LocationControl}";
        }
        private DateTime __Time;
        private Point __LocationControl;
        private Point __LocationVirtual;
        private Point __LocationAbsolute;
        private MouseButtons __Buttons;
        private Keys __ModifierKeys;
        private bool __IsOnControl;
        private IInteractiveItem __InteractiveItem;
        /// <summary>
        /// Čas akce myši; důležité pro případný doubleclick
        /// </summary>
        public DateTime Time { get { return __Time; } }
        /// <summary>
        /// Souřadnice myši v koordinátech controlu (nativní prostor)
        /// </summary>
        public Point LocationControl { get { return __LocationControl; } }
        /// <summary>
        /// Souřadnice myši ve virtuálním prostoru  koordinátech controlu (nativní prostor)
        /// </summary>
        public Point LocationVirtual { get { return __LocationVirtual; } }
        /// <summary>
        /// Souřadnice myši v koordinátech absolutních (Screen)
        /// </summary>
        public Point LocationAbsolute { get { return __LocationAbsolute; } }
        /// <summary>
        /// Stav buttonů myši
        /// </summary>
        public MouseButtons Buttons { get { return __Buttons; } }
        /// <summary>
        /// Stav kláves Control, Alt, Shift
        /// </summary>
        public Keys ModifierKeys { get { return __ModifierKeys; } }
        /// <summary>
        /// Ukazatel myši se nachází nad controlem?
        /// </summary>
        public bool IsOnControl { get { return __IsOnControl; } }
        /// <summary>
        /// Prvek na pozici myši
        /// </summary>
        public IInteractiveItem InteractiveItem { get { return __InteractiveItem; } set { __InteractiveItem = value; } }
    }
    #endregion
    #region RectangleExt : Souřadnice prvku snadno ukotvitelné nejen Left a Top (jako Rectangle) ale i Right a Bottom a Center.
    /// <summary>
    /// <see cref="RectangleExt"/>: Souřadnice prvku snadno ukotvitelné nejen Left a Top (jako Rectangle) ale i Right a Bottom a Center.
    /// <para/>
    /// V jednom každém směru (X, Y) může mít zadánu jednu až dvě souřadnice tak, aby bylo možno získat reálnou souřadnici v parent prostoru:
    /// Například: Left a Width, nebo Left a Right, nebo Width a Right, nebo jen Width. Tím se řeší různé ukotvení.
    /// <para/>
    /// Po zadání hodnot lze získat konkrétní souřadnice metodou <see cref="GetBounds"/>
    /// </summary>
    public struct RectangleExt
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="width"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="height"></param>
        /// <param name="bottom"></param>
        public RectangleExt(int? left, int? width, int? right, int? top, int? height, int? bottom)
        {
            Left = left;
            Width = width;
            Right = right;

            Top = top;
            Height = height;
            Bottom = bottom;
        }
        /// <summary>
        /// Souřadnice Left, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k levé hraně.
        /// Pokud je null, pak je vázaný k pravé hraně nebo na střed.
        /// </summary>
        public int? Left { get; set; }
        /// <summary>
        /// Souřadnice Top, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k horní hraně.
        /// Pokud je null, pak je vázaný k dolní hraně nebo na střed.
        /// </summary>
        public int? Top { get; set; }
        /// <summary>
        /// Souřadnice Right, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k pravé hraně.
        /// Pokud je null, pak je vázaný k levé hraně nebo na střed.
        /// </summary>
        public int? Right { get; set; }
        /// <summary>
        /// Souřadnice Bottom, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k dolní hraně.
        /// Pokud je null, pak je vázaný k horní hraně nebo na střed.
        /// </summary>
        public int? Bottom { get; set; }
        /// <summary>
        /// Pevná šířka, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou šířku.
        /// Pokud je null, pak je vázaný k pravé i levé hraně a má šířku proměnnou.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Pevná výška, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou výšku.
        /// Pokud je null, pak je vázaný k horní i dolní hraně a má výšku proměnnou.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Textem vyjádřený obsah this prvku
        /// </summary>
        public string Text { get { return $"Left: {Left}, Width: {Width}, Right: {Right}, Top: {Top}, Height: {Height}, Bottom: {Bottom}"; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text;
        }
        /// <summary>
        /// Metoda vrátí konkrétní souřadnice v daném prostoru parenta.
        /// Při tom jsou akceptovány plovoucí souřadnice.
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <returns></returns>
        public Rectangle GetBounds(Rectangle parentBounds)
        {
            if (!IsValid)
                throw new InvalidOperationException($"Neplatně zadané souřadnice v {nameof(RectangleExt)}: {Text}");

            var rectangleExt = this;
            getBound(Left, Width, Right, parentBounds.Left, parentBounds.Right, out int x, out int w);
            getBound(Top, Height, Bottom, parentBounds.Top, parentBounds.Bottom, out int y, out int h);
            return new Rectangle(x, y, w, h);

            void getBound(int? defB, int? defS, int? defE, int parentB, int parentE, out int begin, out int size)
            {
                bool hasB = defB.HasValue;
                bool hasS = defS.HasValue;
                bool hasE = defE.HasValue;

                if (hasB && hasS && !hasE)
                {   // Mám Begin a Size a nemám End     => standardně jako Rectangle:
                    begin = parentB + defB.Value;
                    size = defS.Value;
                }
                else if (hasB && !hasS && hasE)
                {   // Mám Begin a End a nemám Size     => mám pružnou šířku:
                    begin = parentB + defB.Value;
                    size = parentE - defE.Value - begin;
                }
                else if (!hasB && hasS && hasE)
                {   // Mám Size a End a nemám Begin     => jsem umístěn od konce:
                    int end = parentE - defE.Value;
                    size = defS.Value;
                    begin = end - size;
                }
                else if (!hasB && hasS && !hasE)
                {   // Mám Size a nemám Begin ani End   => jsem umístěn Center:
                    int center = parentB + ((parentE - parentB) / 2);
                    size = defS.Value;
                    begin = center - (size / 2);
                }
                else
                {   // Nesprávné zadání:
                    throw new InvalidOperationException($"Chyba v datech {nameof(RectangleExt)}: {rectangleExt.ToString()}");
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je zcela nezadaný = prázdný.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // Pokud i jen jediná hodnota je zadaná, pak vrátím false = objekt NENÍ prázdný:
                return !(Left.HasValue || Width.HasValue || Right.HasValue || Top.HasValue || Height.HasValue || Bottom.HasValue);
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je korektně zadaný, a může mít kladný vnitřní prostor
        /// </summary>
        public bool IsValid
        {
            get
            {
                return isValid(Left, Width, Right) && isValid(Top, Height, Bottom);

                // Je zadaná sada hodnot platná?
                bool isValid(int? begin, int? size, int? end)
                {
                    bool hasB = begin.HasValue;
                    bool hasS = size.HasValue;
                    bool hasE = end.HasValue;

                    return ((hasB && hasS && !hasE)                  // Mám Begin a Size a nemám End     => standardně jako Rectangle
                         || (hasB && !hasS && hasE)                  // Mám Begin a End a nemám Size     => mám pružnou šířku
                         || (!hasB && hasS && hasE)                  // Mám Size a End a nemám Begin     => jsem umístěn od konce
                         || (!hasB && hasS && !hasE));               // Mám Size a nemám Begin ani End   => jsem umístěn Center
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je korektně zadaný, a může mít kladný vnitřní prostor
        /// </summary>
        public bool HasContent
        {
            get
            {
                return IsValid && hasSize(Width) && hasSize(Height);

                // Je daná hodnota kladná nebo null ? (pro Null předpokládáme, že se dopočítá kladné číslo z Parent rozměru)
                bool hasSize(int? size)
                {
                    return (!size.HasValue || (size.HasValue && size.Value > 0));
                }
            }
        }
    }
    #endregion
}
