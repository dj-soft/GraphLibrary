using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.Components
{
    #region interface IInteractiveItem a IInteractiveParent
    /// <summary>
    /// Definuje vlastnosti třídy, která může být interaktivní a vykreslovaná v InteractiveControlu
    /// </summary>
    public interface IInteractiveItem : IInteractiveParent
    {
        /// <summary>
        /// Jednoznačné ID of object. je přiřazeno v konstruktoru a nemění se po celý život instance.
        /// </summary>
        /// UInt32 Id { get; }
        /// <summary>
        /// Reference na Hosta, což je GrandParent všech prvků.
        /// </summary>
        /// GInteractiveControl Host { get; set; }
        /// <summary>
        /// Parent tohoto prvku. Může být null, pokud this je hostován přímo v controlu GInteractiveControl.
        /// Parent je typicky typu IInteractiveContainer.
        /// </summary>
        /// IInteractiveParent Parent { get; set; }
        /// <summary>
        /// Relativní souřadnice this prvku v rámci parenta.
        /// Absolutní souřadnice mohou být určeny pomocí extension metody IInteractiveItem.GetAbsoluteVisibleBounds().
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Přídavek k this.Bounds, který určuje přesah aktivity tohoto prvku do jeho okolí.
        /// <para/>
        /// Kladné hodnoty v Padding zvětšují aktivní plochu nad rámec this.Bounds, záporné aktivní plochu zmenšují.
        /// <para/>
        /// Aktivní souřadnice prvku tedy jsou this.Bounds.Add(this.ActivePadding), kde Add() je extension metoda.
        /// </summary>
        Padding? InteractivePadding { get; set; }
        /// <summary>
        /// Okraje (dovnitř this prvku od <see cref="Bounds"/>), uvnitř těchto okrajů se nachází prostor pro klientské prvky.
        /// </summary>
        Padding? ClientBorder { get; set; }
        /// <summary>
        /// Pole mých vlastních potomků. Jejich Parentem je this.
        /// Jejich souřadnice jsou relativní ke zdejšímu souřadnému systému.
        /// This is: where this.ActiveBounds.Location is {200, 100} and child.ActiveBounds.Location is {10, 40}, then child is on Point {210, 140}.
        /// </summary>
        IEnumerable<IInteractiveItem> Childs { get; }
        /// <summary>
        /// Souřadnice prostoru, který je vyhrazen pro <see cref="Childs"/> prvky obsažené v this prvku.
        /// Souřadnice jsou relativní vzhledem <see cref="Bounds"/>.
        /// </summary>
        /// Rectangle BoundsClient { get; }
        /// <summary>
        /// Interaktivní styl = dostupné chování objektu
        /// </summary>
        /// GInteractiveStyles Style { get; }
        /// <summary>
        /// Is item currently interactive?
        /// When true, then item can be found by mouse, and can be activate by mouse events.
        /// </summary>
        Boolean IsInteractive { get; }
        /// <summary>
        /// Is item currently visible?
        /// When true, then item can be Drawed.
        /// </summary>
        Boolean IsVisible { get; set; }
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
        /// Libovolný popisný údaj
        /// </summary>
        object Tag { get; set; }
        /// <summary>
        /// Libovolný datový údaj
        /// </summary>
        object UserData { get; set; }
        /// <summary>
        /// Obsahuje vrstvy, do nichž se tento objekt kreslí standardně.
        /// Tuto hodnotu vloží metoda <see cref="InteractiveObject.Repaint()"/> do <see cref="RepaintToLayers"/>.
        /// Vrstva <see cref="GInteractiveDrawLayer.None"/> není vykreslována (objekt je tedy vždy neviditelný), ale na rozdíl od <see cref="IsVisible"/> je takový objekt interaktivní.
        /// </summary>
        GInteractiveDrawLayer StandardDrawToLayer { get; }
        /// <summary>
        /// Obsahuje všechny vrstvy grafiky, do kterých chce být tento prvek právě nyní (při nejbližším kreslení) vykreslován.
        /// Po vykreslení je nastaveno na <see cref="GInteractiveDrawLayer.None"/>.
        /// </summary>
        GInteractiveDrawLayer RepaintToLayers { get; set; }
        /// <summary>
        /// Vrátí true, pokud daný prvek je aktivní na dané souřadnici.
        /// Souřadnice je v koordinátech Parenta prvku, je tedy srovnatelná s <see cref="IInteractiveItem.Bounds"/>.
        /// Pokud prvek má nepravidelný tvar, musí testovat tento tvar v této své metodě explicitně.
        /// </summary>
        /// <param name="relativePoint">Bod, který testujeme, v koordinátech srovnatelných s <see cref="IInteractiveItem.Bounds"/></param>
        /// <returns></returns>
        Boolean IsActiveAtPoint(Point relativePoint);
        /// <summary>
        /// Tato metoda je volaná po každé interaktivní změně na prvku.
        /// </summary>
        /// <param name="e"></param>
        void AfterStateChanged(GInteractiveChangeStateArgs e);
        /// <summary>
        /// Tato metoda je volaná postupně pro jednotlivé fáze akce Drag & Drop.
        /// </summary>
        /// <param name="e"></param>
        void DragAction(GDragActionArgs e);
        /// <summary>
        /// Metoda je volaná pro vykreslení obsahu tohoto prvku.
        /// Pokud prvek má nějaké potomstvo (Childs), pak se this prvek nestará o jejich vykreslení, to zajistí jádro.
        /// Jádro detekuje naše <see cref="Childs"/>, a postupně volá jejich vykreslení (od prvního po poslední).
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds);
        /// <summary>
        /// Pokud je zde true, pak v procesu kreslení prvku je po standardním vykreslení this prvku <see cref="Draw(GInteractiveDrawArgs, Rectangle, Rectangle)"/> 
        /// a po standardním vykreslení všech <see cref="Childs"/> prvků ještě vyvolána metoda <see cref="DrawOverChilds(GInteractiveDrawArgs)"/> pro this prvek.
        /// </summary>
        bool NeedDrawOverChilds { get; }
        /// <summary>
        /// Tato metoda je volaná pro prvek, který má nastaveno <see cref="NeedDrawOverChilds"/> == true, poté když tento prvek byl vykreslen, a následně byly vykresleny jeho <see cref="Childs"/>.
        /// Umožňuje tedy kreslit "nad" svoje <see cref="Childs"/> (tj. počmárat je).
        /// Tento postup se používá typicky jen pro zobrazení překryvného textu přes <see cref="Childs"/> prvky, které svůj text nenesou.
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        void DrawOverChilds(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds);
    }
    public interface IInteractiveParent
    {
        /// <summary>
        /// Jednoznačné ID prvku. Je přiřazeno v konstruktoru a nemění se po celý život instance.
        /// </summary>
        UInt32 Id { get; }
        /// <summary>
        /// Reference na Hosta, což je GrandParent všech prvků.
        /// </summary>
        GInteractiveControl Host { get; }
        /// <summary>
        /// Parent tohoto prvku. Může to být i přímo control GInteractiveControl.
        /// Pouze v případě, kdy this je <see cref="GInteractiveControl"/>, pak <see cref="Parent"/> je null.
        /// </summary>
        IInteractiveParent Parent { get; set; }
        /// <summary>
        /// Velikost prostoru pro Childs prvky
        /// </summary>
        Size ClientSize { get; }
        /// <summary>
        /// Interaktivní styl = dostupné chování objektu
        /// </summary>
        GInteractiveStyles Style { get; }
        /// <summary>
        /// Zajistí vykreslení sebe a svých Childs
        /// </summary>
        void Repaint();
    }
    /// <summary>
    /// Interface, který umožní child prvku číst a měnit rozměry některého sého hostitele.
    /// Technicky to nemusí být jeho přímý Parent ve smyslu vztahu Parent - Child, může to být i Parent jeho Parenta.
    /// Praktické využití je mezi grafem umístěným v buňce Gridu, kde jeho IVisualParent může být řádek (a nikoli buňka).
    /// Interface pak dovoluje grafu požádat o změnu výšky řádku (setováním <see cref="IVisualParent.ClientHeight"/>, 
    /// kdy na to řádek grafu reaguje nastavením své výšky podle svých pravidel (minimální a maximální povolená výška řádku).
    /// Následně si graf načte zpět výšku parenta (již s korekcemi) a této výšce přizpůsobí svoje vnitřní souřadnice.
    /// </summary>
    public interface IVisualParent
    {
        /// <summary>
        /// Šířka klientského prosturu uvnitř parenta
        /// </summary>
        int ClientWidth { get; set; }
        /// <summary>
        /// Výška klientského prosturu uvnitř parenta
        /// </summary>
        int ClientHeight { get; set; }
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
    /// Delegát pro handlery události, kdy došlo k nějaké akci na určitém objektu v GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GPropertyEvent<T>(object sender, GPropertyEventArgs<T> e);
    /// <summary>
    /// Delegát pro handlery události, kdy došlo ke změně hodnoty na GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GPropertyChangedHandler<T>(object sender, GPropertyChangeArgs<T> e);
    /// <summary>
    /// Delegate for handlers for Drawing into User Area
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GUserDrawHandler(object sender, GUserDrawArgs e);
    /// <summary>
    /// Data pro eventhandler navázaný na událost na určitém objektu v GInteractiveControl
    /// </summary>
    public class GPropertyEventArgs<T> : EventArgs
    {
        public GPropertyEventArgs(T value, EventSourceType eventSource = EventSourceType.InteractiveChanged, GInteractiveChangeStateArgs interactiveArgs = null)
        {
            this.Value = value;
            this.EventSource = eventSource;
            this.InteractiveArgs = interactiveArgs;
            this.Cancel = false;
        }
        /// <summary>
        /// Objekt, kde došlo k události
        /// </summary>
        public T Value { get; private set; }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSourceType EventSource { get; private set; }
        /// <summary>
        /// Data o interaktivní události
        /// </summary>
        public GInteractiveChangeStateArgs InteractiveArgs { get; private set; }
        /// <summary>
        /// Obsahuje true pokud <see cref="InteractiveArgs"/> obsahuje data.
        /// </summary>
        public bool HasInteractiveArgs { get { return (this.InteractiveArgs != null); } }
        /// <summary>
        /// Požadavek aplikačního kódu na zrušení návazností této akce
        /// Výchozí hodnota je false.
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// Data pro eventhandler navázaný na změnu nějaké hodnoty v GInteractiveControl
    /// </summary>
    public class GPropertyChangeArgs<T> : EventArgs
    {
        public GPropertyChangeArgs(T oldvalue, T newValue, EventSourceType eventSource)
        {
            this.OldValue = oldvalue;
            this.NewValue = newValue;
            this.EventSource = eventSource;
            this.CorrectValue = newValue;
            this.Cancel = false;
        }
        /// <summary>
        /// Hodnota platná před změnou
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Hodnota platná po změně
        /// </summary>
        public T NewValue { get; private set; }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSourceType EventSource { get; private set; }
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
    /// Data pro handlery interaktivních událostí v GInteractiveControl
    /// </summary>
    public class GInteractiveChangeStateArgs : EventArgs
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="existsItem">true, when CurrentItem is found. Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.</param>
        /// <param name="currentItem">Active item. Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.</param>
        /// <param name="changeState">Type of event (change of status)</param>
        /// <param name="targetState">New state of item (after this event, not before it).</param>
        public GInteractiveChangeStateArgs(bool existsItem, IInteractiveItem currentItem, GInteractiveChangeState changeState, GInteractiveState targetState, Func<Point, bool, IInteractiveItem> searchItemMethod)
               : this()
        {
            this.ExistsItem = existsItem;
            this.CurrentItem = currentItem;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="existsItem">true, when CurrentItem is found. Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.</param>
        /// <param name="currentItem">Active item. Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.</param>
        /// <param name="changeState">Type of event (change of status)</param>
        /// <param name="targetState">New state of item (after this event, not before it).</param>
        /// <param name="mouseRelativePoint">Coordinate of mouse relative to CurrentItem.ActiveBounds.Location. Can be a null (in case when ExistsItem is false).</param>
        /// <param name="dragOriginBounds">Original area before current Drag operacion begun (in DragMove events)</param>
        /// <param name="dragToBounds">Target area during Drag operation (in DragMove event)</param>
        public GInteractiveChangeStateArgs(bool existsItem, IInteractiveItem currentItem, GInteractiveChangeState changeState, GInteractiveState targetState, Func<Point, bool, IInteractiveItem> searchItemMethod, Point? mouseAbsolutePoint, Point? mouseRelativePoint, Rectangle? dragOriginBounds, Rectangle? dragToBounds)
              : this()
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
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="existsItem">true, when CurrentItem is found. Whereby CurrentItem is interface (i.e. can be a struct), then test for CurrentItem == null is not possible.</param>
        /// <param name="currentItem">Active item. Item is found in hierarchy of IInteractiveItem and all its Childs, this is last Child found.</param>
        /// <param name="changeState">Type of event (change of status)</param>
        /// <param name="targetState">New state of item (after this event, not before it).</param>
        /// <param name="previewArgs">Keyboard Preview Data</param>
        /// <param name="keyArgs">Keyboard Events Data</param>
        /// <param name="keyPressArgs">Keyboard KeyPress data</param>
        public GInteractiveChangeStateArgs(bool existsItem, IInteractiveItem currentItem, GInteractiveChangeState changeState, GInteractiveState targetState, Func<Point, bool, IInteractiveItem> searchItemMethod, PreviewKeyDownEventArgs previewArgs, KeyEventArgs keyArgs, KeyPressEventArgs keyPressArgs)
            : this()
        {
            this.ExistsItem = existsItem;
            this.CurrentItem = currentItem;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.KeyboardPreviewArgs = previewArgs;
            this.KeyboardEventArgs = keyArgs;
            this.KeyboardPressEventArgs = keyPressArgs;
        }
        /// <summary>
        /// Konstruktor pouze pro inicializaci proměnných
        /// </summary>
        protected GInteractiveChangeStateArgs()
        {
            this.ExistsItem = false;
            this.CurrentItem = null;
            this.ChangeState = GInteractiveChangeState.None;
            this.TargetState = GInteractiveState.None;
            this.SearchItemMethod = null;
            this.MouseAbsolutePoint = null;
            this.MouseRelativePoint = null;
            this.DragOriginBounds = null;
            this.DragToBounds = null;
            this.ModifierKeys = System.Windows.Forms.Control.ModifierKeys;
            this.KeyboardPreviewArgs = null;
            this.KeyboardEventArgs = null;
            this.KeyboardPressEventArgs = null;
            this.ActionIsSolved = false;
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
        /// Stav kláves v okamžiku události, včetně události myši
        /// </summary>
        public System.Windows.Forms.Keys ModifierKeys { get; protected set; }
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
        #region Výstupy - z EventHandleru do Controlu
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
        /// Změnit kurzor na tento typ. null = beze změny.
        /// </summary>
        public SysCursorType? RequiredCursorType { get; set; }
        /// <summary>
        /// Po tomto eventu se má překreslit úplně celý Control.
        /// Lze nastavit pouze na hodnotu true; pokud je jednou true, pak už nelze shodit na false.
        /// Není to ale ideální řešení, protože jeden maličký control nemůže vědět, jak velký je rozsah toho, co chce překreslit.
        /// Smysl to dává typicky při změně jazyka, změně barevné palety a Zoomu, atd.
        /// Optimální je dát Repaint() na tom prvku, který se má znovu vykreslit. Prvek sám si může tuto událost zpracovat (overridovat metodu Repaint()) a zajistit Repaint i pro své sousedící prvky.
        /// </summary>
        public bool RepaintAllItems { get { return this._RepaintAllItems; } set { if (value) this._RepaintAllItems = true; } } private bool _RepaintAllItems;
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { if (this._ToolTipData == null) this._ToolTipData = new ToolTipData(); return this._ToolTipData; } set { this._ToolTipData = value; } } private ToolTipData _ToolTipData;
        /// <summary>
        /// Obsahuje true pokud je přítomen objekt <see cref="ToolTipData"/>. Ten je přítomen po jeho vložení nebo po jeho použití. Ve výchozím stavu je false.
        /// </summary>
        internal bool HasToolTipData { get { return (this._ToolTipData != null); } }
        /// <summary>
        /// Obsahuje true pokud <see cref="ToolTipData"/> obsahuje platná data pro vykreslení tooltipu.
        /// </summary>
        public bool ToolTipIsValid { get { return (this._ToolTipData != null && this._ToolTipData.IsValid); } }
        /// <summary>
        /// Metoda by měla nastavit true, pokud danou operaci vyřeší.
        /// Při akci typu <see cref="GInteractiveChangeState.WheelUp"/> a <see cref="GInteractiveChangeState.WheelDown"/> se testuje, zda <see cref="ActionIsSolved"/> je true.
        /// Pokud není, pak se stejná událost pošle i do Parent objektů.
        /// </summary>
        public bool ActionIsSolved { get; set; }
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
    /// Data pro obsluhu kreslení prvků ve třídě GInteractiveControl
    /// </summary>
    public class GInteractiveDrawArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphics">Instance třídy Graphics pro kreslení</param>
        /// <param name="drawLayer">Vrstva, která se bude kreslit do této vrstvy</param>
        public GInteractiveDrawArgs(Graphics graphics, GInteractiveDrawLayer drawLayer)
        {
            this.Graphics = graphics;
            this.DrawLayer = drawLayer;
            this.IsStandardLayer = (drawLayer == GInteractiveDrawLayer.Standard);
            this._ResetLayerFlag = ((int)drawLayer ^ 0xFFFF);
        }
        /// <summary>
        /// Instance třídy Graphics pro kreslení
        /// </summary>
        public Graphics Graphics { get; private set; }
        /// <summary>
        /// Vrstva, která se bude kreslit do této vrstvy
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get; private set; }
        /// <summary>
        /// true pokud se pro tuto vrstvu má používat Clip = jde o Standard vrstvu.
        /// Pro ostatní vrstvy se bude jakýkoli pokus Clip grafiky ignorovat.
        /// </summary>
        public bool IsStandardLayer { get; private set; }
        /// <summary>
        /// Prostor, do kterého je oříznut výstup grafiky (Clip) pro kreslení aktuálního prvku.
        /// Jedná se o absolutní souřadnice v rámci Controlu Host.
        /// Jde o prostor, který je průnikem prostorů všech Parentů daného prvku = tady do tohoto prostoru se prvek smí vykreslit, aniž by "utekl ze svého parenta" někam mimo.
        /// Tento prostor tedy typicky neobsahuje Clip na souřadnice kresleného prvku (Item.Bounds).
        /// </summary>
        public Rectangle AbsoluteVisibleClip { get; set; }
        /// <summary>
        /// Obsahuje true, pokud aktuální AbsoluteVisibleClip obsahuje prázdný prostor (jeho Width nebo Height == 0).
        /// Pokud tedy IsVisibleClipEmpty je true, pak nemá smysl provádět jakékoli kreslení pomocí grafiky, protože nebude nic vidět.
        /// </summary>
        public bool IsVisibleClipEmpty { get { Rectangle c = this.AbsoluteVisibleClip; return (c.Width == 0 || c.Height == 0); } }
        /// <summary>
        /// Pro daný prvek v jeho RepaintToLayers zruší požadavek na kreslení do vrstvy, která se právě zde kreslí.
        /// Volá se typicky po dokončení Draw().
        /// </summary>
        /// <param name="item"></param>
        public void ResetLayerFlagForItem(IInteractiveItem item)
        {
            item.RepaintToLayers = (GInteractiveDrawLayer)((int)item.RepaintToLayers & this._ResetLayerFlag);
        }
        private int _ResetLayerFlag;
        /// <summary>
        /// Vrátí průsečík aktuálního this.AbsoluteVisibleClip (jde o absolutní souřadnice viditelného prostoru pro kreslení aktuálního prvku)
        /// s daným prostorem (absoluteBounds).
        /// Tato metoda vrací průsečík i pro jiné než Standard layers.
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <returns></returns>
        public Rectangle GetClip(Rectangle absoluteBounds)
        {
            Rectangle clip = this.AbsoluteVisibleClip;
            clip.Intersect(absoluteBounds);
            return clip;
        }
        /// <summary>
        /// Ořízne plochu, do které bude kreslit aktuální Graphics, jen na průsečík aktuálního this.ClipBounds s danými souřadnicemi.
        /// Bez zadání parametru permanent bude toto oříznuté platné jen do dalšího zavolání této metody, pak se oříznutí může změnit.
        /// Tato metoda provádí Clip pouze tehdy, když se kreslí do Standard layer (když this.IsStandardLayer je true).
        /// </summary>
        /// <param name="absoluteBounds">Absolutní souřadnice výřezu.</param>
        public void GraphicsClipWith(Rectangle absoluteBounds)
        {
            this._GraphicsClipWith(absoluteBounds, false);
        }
        /// <summary>
        /// Ořízne plochu, do které bude kreslit aktuální Graphics, jen na průsečík aktuálního this.ClipBounds s danými souřadnicemi.
        /// Tato metoda provádí Clip pouze tehdy, když se kreslí do Standard layer (když this.IsStandardLayer je true).
        /// Parametr permanent říká, zda toto oříznutí má být bráno jako trvalé pro aktuální kreslený prvek, tím se ovlivní chování po nějakém následujícím volání téže metody.
        /// Příklad (pro jednoduchost v 1D hodnotách):
        /// Mějme souřadnice hosta { 0 - 100 }, pro aktuální control je systémem nastaven ClipBounds { 50 - 80 }, což je pozice jeho Parenta.
        /// Následně voláme GraphicsClipWith() pro oblast { 60 - 70 } s parametrem permanent = false, následné kreslení se provede jen do oblasti { 60 - 70 }.
        /// Pokud poté voláme GraphicsClipWith() pro oblast { 50 - 65 } s parametrem permanent = false, pak se následné kreslení provede do oblasti { 50 - 65 }.
        /// Jiná situace je, pokud první volání GraphicsClipWith() pro oblast { 60 - 70 } provedeme s parametrem permanent = true. Následné kreslení proběhne do oblasti { 60 - 70 }, to je logické.
        /// Pokud ale po tomto prvním Clipu s permanent = true voláme druhý Clip pro oblast { 50 - 65 }, provede se druhý clip proti výsledku prvního clipu (neboť ten je permanentní), a výsledek bude { 60 - 65 }.
        /// </summary>
        /// <param name="absoluteBounds">Absolutní souřadnice clipu</param>
        /// <param name="permanent">Toto oříznutí je trvalé pro aktuální prvek</param>
        public void GraphicsClipWith(Rectangle absoluteBounds, bool permanent)
        {
            this._GraphicsClipWith(absoluteBounds, permanent);
        }
        /// <summary>
        /// Ořízne plochu, do které bude kreslit aktuální Graphics, jen na průsečík aktuálního this.ClipBounds s danými souřadnicemi.
        /// Tato metoda provádí Clip pouze tehdy, když se kreslí do Standard layer (když this.IsStandardLayer je true).
        /// Parametr permanent říká, zda toto oříznutí má být bráno jako trvalé pro aktuální kreslený prvek (=uloží výsledek do AbsoluteVisibleClip),
        /// tím se ovlivní chování při jakémkoli následujícím volání téže metody.
        /// </summary>
        /// <param name="absoluteBounds">Absolutní souřadnice clipu</param>
        /// <param name="permanent">Toto oříznutí je trvalé pro aktuální prvek</param>
        private void _GraphicsClipWith(Rectangle absoluteBounds, bool permanent)
        {
            if (!this.IsStandardLayer) return;

            Rectangle clip = this.GetClip(absoluteBounds);
            if (permanent)
                this.AbsoluteVisibleClip = clip;
            this.Graphics.SetClip(clip);
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
        InteractiveChanging = 0x2000,
        /// <summary>
        /// Interactive action is source of change (user was entered / dragged new value)
        /// </summary>
        InteractiveChanged = 0x4000
    }
    /// <summary>
    /// Režim překreslení objektu Parent při překreslení objektu this.
    /// V naprosté většině případů není při překreslení this objektu zapotřebí překreslovat objekt Parent.
    /// Exitují výjimky, typicky pokud aktuální objekt používá průhlednost (ve své BackColor nebo jinou cestou ne-kreslené pozadí), 
    /// pak může být zapotřebí vykreslit nejprve Parent objekt, a teprve na něj this objekt.
    /// </summary>
    public enum RepaintParentMode
    {
        /// <summary>
        /// Překreslení this objektu nevyžaduje předchozí vykreslení Parenta
        /// </summary>
        None,
        /// <summary>
        /// Překreslení this objektu vyžaduje nejprve vykreslení Parenta, pokud this.BackColor má hodnotu A menší než 255 (tzn. naše pozadí je trochu nebo úplně průhledné)
        /// </summary>
        OnBackColorAlpha,
        /// <summary>
        /// Překreslení this objektu bezpodmínečně vyžaduje nejprve vykreslení Parenta
        /// </summary>
        Always
    }
    #endregion
    #region Vizuální styly : interface IVisualMember, class VisualStyle, enum BorderLinesType
    /// <summary>
    /// Prvek s vizuálním stylem, šířkou a výškou
    /// </summary>
    public interface IVisualMember
    {
        /// <summary>
        /// Aktuální vizuální styl pro tento prvek. Nesmí být null.
        /// Prvek sám ve své implementaci může kombinovat svůj vlastní styl se styly svých parentů, ve vhodném pořadí.
        /// Prvek by měl pro získání aktuálního stylu využívat metodu:
        /// VisualStyle.CreateFrom(this.VisualStyle, this.Parent.VisualStyle, this.Parent.Parent.VisualStyle, ...);
        /// Tato metoda nikdy nevrací null, vždy vrátí new instanci, v níž jsou sečtené první NotNull hodnoty z dodané sekvence stylů 
        /// = tím je zajištěna "dědičnost" hodnoty z prapředka, v kombinaci s možností zadání detailního stylu v detailním prvku.
        /// </summary>
        VisualStyle Style { get; }
    }
    /// <summary>
    /// Vizuální styl: shrnuje sadu vizuálních údajů pro vykreslení prvku, a umožňuje je kombinovat v řetězci parentů
    /// </summary>
    public class VisualStyle
    {
        /// <summary>
        /// Vytvoří a vrátí new instanci VisualStyle, v níž budou jednotlivé property naplněny hodnotami z dodaných instancí.
        /// Slouží k vyhodnocení řetězce od explicitních údajů (zadaných do konkrétního prvku) až po defaultní (zadané např. v konfiguraci).
        /// Dodané instance se vyhodnocují v pořadá od první do poslední, hodnoty null se přeskočí.
        /// Logika: hodnota do každé jednotlivé property výsledné instance se převezme z nejbližšího dodaného objektu, kde tato hodnota není null.
        /// </summary>
        /// <param name="styles"></param>
        /// <returns></returns>
        public static VisualStyle CreateFrom(params VisualStyle[] styles)
        {
            VisualStyle result = new VisualStyle();
            foreach (VisualStyle style in styles)
                result._AddFrom(style);
            return result;
        }
        /// <summary>
        /// Do this instance vloží potřebné hodnoty z dodané instance.
        /// Dodaná instance může být null, pak se nic neprovádí.
        /// Plní se jen takové property v this, které obsahují null.
        /// </summary>
        /// <param name="style"></param>
        private void _AddFrom(VisualStyle style)
        {
            if (style != null)
            {
                if (this.Font == null) this.Font = style.Font;
                if (!this.ContentAlignment.HasValue) this.ContentAlignment = style.ContentAlignment;
                if (!this.BackColor.HasValue) this.BackColor = style.BackColor;
                if (!this.TextColor.HasValue) this.TextColor = style.TextColor;
                if (!this.SelectedBackColor.HasValue) this.SelectedBackColor = style.SelectedBackColor;
                if (!this.SelectedTextColor.HasValue) this.SelectedTextColor = style.SelectedTextColor;
                if (!this.ActiveBackColor.HasValue) this.ActiveBackColor = style.ActiveBackColor;
                if (!this.ActiveTextColor.HasValue) this.ActiveTextColor = style.ActiveTextColor;
                if (!this.BorderColor.HasValue) this.BorderColor = style.BorderColor;
                if (!this.BorderLines.HasValue) this.BorderLines = style.BorderLines;

            }
        }
        /// <summary>
        /// Informace o fontu
        /// </summary>
        public FontInfo Font { get; set; }
        /// <summary>
        /// Zarovnání obsahu
        /// </summary>
        public ContentAlignment? ContentAlignment { get; set; }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud není Selected, a není to aktivní položka (řádek tabulky), prostě běžný prvek (řádek)
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva textu v prvku (řádek, buňka) pokud není Selected, a není to aktivní položka (řádek tabulky), prostě běžný prvek (řádek)
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud je Selected, a není to aktivní položka (řádek tabulky)
        /// </summary>
        public Color? SelectedBackColor { get; set; }
        /// <summary>
        /// Barva textu v prvku (řádek, buňka) pokud je Selected, a není to aktivní položka (řádek tabulky)
        /// </summary>
        public Color? SelectedTextColor { get; set; }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud je tento prvek aktivní (řádek je vybraný) a v jeho controlu je focus.
        /// Po odchodu focusu z tohoto prvku je barva prvku změněna na 50% směrem k barvě BackColor nebo SelectedBackColor.
        /// </summary>
        public Color? ActiveBackColor { get; set; }
        /// <summary>
        /// Barva písma v prvku (řádek, buňka) pokud je tento prvek aktivní (řádek je vybraný) a v jeho controlu je focus.
        /// Po odchodu focusu z tohoto prvku je barva prvku změněna na 50% směrem k barvě TextColor nebo SelectedTextColor.
        /// </summary>
        public Color? ActiveTextColor { get; set; }
        /// <summary>
        /// Barva okrajů prvku.
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Styl linek okrajů prvku
        /// </summary>
        public BorderLinesType? BorderLines { get; set; }

    }
    /// <summary>
    /// Typ čáry při kreslení Borders, hodnoty lze sčítat
    /// </summary>
    [Flags]
    public enum BorderLinesType
    {
        /// <summary>Žádné</summary>
        None = 0,

        /// <summary>Vodorovné = tečkovaná čára</summary>
        HorizontalDotted = 1,
        /// <summary>Vodorovné = plná čára</summary>
        HorizontalSolid = HorizontalDotted << 1,
        /// <summary>Vodorovné = plná čára s barevným 3D efektem Sunken (jakoby potopený dolů)</summary>
        Horizontal3DSunken = HorizontalSolid << 1,
        /// <summary>Vodorovné = plná čára s barevným 3D efektem Risen (jakoby vystupující nahoru)</summary>
        Horizontal3DRisen = Horizontal3DSunken << 1,

        /// <summary>Svislé = tečkovaná čára</summary>
        VerticalDotted = Horizontal3DRisen << 1,
        /// <summary>Svislé = plná čára</summary>
        VerticalSolid = VerticalDotted << 1,
        /// <summary>Svislé = plná čára s barevným 3D efektem Sunken (jakoby potopený dolů)</summary>
        Vertical3DSunken = VerticalSolid << 1,
        /// <summary>Svislé = plná čára s barevným 3D efektem Risen (jakoby vystupující nahoru)</summary>
        Vertical3DRisen = Vertical3DSunken << 1,

        /// <summary>Obě čáry tečkované, bez 3D efektu</summary>
        AllDotted = HorizontalDotted | VerticalDotted,
        /// <summary>Obě čáry plné, bez 3D efektu</summary>
        AllSolid = HorizontalSolid | VerticalSolid,
        /// <summary>Obě čáry s barevným 3D efektem Sunken (jakoby potopený dolů)</summary>
        All3DSunken = Horizontal3DSunken | Vertical3DSunken,
        /// <summary>Obě čáry s barevným 3D efektem Risen (jakoby vystupující nahoru)</summary>
        All3DRisen = Horizontal3DRisen | Vertical3DRisen
    }
    #endregion
}
