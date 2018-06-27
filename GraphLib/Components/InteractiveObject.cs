using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class InteractiveObject : abstract ancestor for all common interactive items
    /// <summary>
    /// InteractiveObject : abstraktní předek všech běžně používaných grafických prvků.
    /// Může mít své vlastní Child (<see cref="Childs"/>).
    /// </summary>
    public abstract class InteractiveObject : IInteractiveItem, IInteractiveParent
    {
        #region Public properties
        /// <summary>
        /// Jednoznačné ID
        /// </summary>
        public UInt32 Id { get { return this._Id; } }
        /// <summary>
        /// Grafický prvek GInteractiveControl, který je hostitelem tohoto prvku nebo jeho Parenta.
        /// Může být null, pokud prvek this ještě není nikam přidán.
        /// </summary>
        protected GInteractiveControl Host
        {
            get
            {
                IInteractiveParent item = this;
                for (int t = 0; t < 200; t++)
                {
                    if (item is GInteractiveControl) return item as GInteractiveControl;
                    if (item.Parent == null) return null;
                    item = item.Parent;
                }
                return null;
            }
            // set
            // {
            //     this._Host = value;
            // }
        }
        /// <summary>
        /// Parent tohoto objektu. Parentem je buď jiný prvek IInteractiveItem (jako Container), anebo přímo GInteractiveControl.
        /// Může být null, v době kdy prvek ještě není přidán do parent containeru.
        /// </summary>
        protected IInteractiveParent Parent { get; set; }
        /// <summary>
        /// true = mám parenta
        /// </summary>
        protected bool HasParent { get { return (this.Parent != null); } }
        /// <summary>
        /// An array of sub-items in this item
        /// </summary>
        protected virtual IEnumerable<IInteractiveItem> Childs { get { return null; } }

        /// <summary>
        /// Souřadnice tohoto prvku v rámci jeho Parenta.
        /// Přepočet na absolutní souřadnice provádí (extension) metoda IInteractiveItem.GetAbsoluteVisibleBounds().
        /// Vložení hodnoty do této property způsobí veškeré zpracování akcí (<see cref="ProcessAction.All"/>).
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
            set
            {
                if (this.GetType().Name == "GRow")
                { }
                this.__AbsoluteInteractiveBounds = value;
            }
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
        /// Použitá barva pozadí.
        /// Pokud <see cref="BackColorUser"/> bude null, použije se <see cref="DefaultBackColor"/>.
        /// Sem nelze setovat null.
        /// </summary>
        public virtual Color BackColor 
        {
            get { return (this.BackColorUser.HasValue ? this.BackColorUser.Value : this.DefaultBackColor) ; }
            set { this.BackColorUser = value; } 
        }
        /// <summary>
        /// Aktuálně vložená barva pozadí.
        /// Lze vložit null, pak jako BackColor bude figurovat <see cref="DefaultBackColor"/>.
        /// </summary>
        public Color? BackColorUser
        {
            get { return this._BackColorUser; }
            set { this._BackColorUser = value; this.Repaint(); }
        }
        private Color? _BackColorUser = null;
        /// <summary>
        /// Defaultní barva pozadí. Potomek může přepsat na svoji dle Skinu
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Skin.Control.BackColor; } }
        /// <summary>
        /// Libovolný popisný údaj
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// Libovolný datový údaj
        /// </summary>
        public object UserData { get; set; }
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
            GInteractiveControl host = this.Host;
            if (host != null) host.Refresh();
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
            GPropertyChangeArgs<Rectangle> args = new GPropertyChangeArgs<Rectangle>(oldValue, newValue, eventSource);
            this.OnBoundsChanged(args);
            if (!this.IsSuppressedEvent && this.BoundsChanged != null)
                this.BoundsChanged(this, args);
        }
        /// <summary>
        /// Occured after Bounds changed
        /// </summary>
        protected virtual void OnBoundsChanged(GPropertyChangeArgs<Rectangle> args) { }
        /// <summary>
        /// Event on this.Bounds changes
        /// </summary>
        public event GPropertyChangedHandler<Rectangle> BoundsChanged;

        /// <summary>
        /// Call method OnDrawRequest() and event DrawRequest.
        /// Set: this.RepaintToLayers = this.StandardDrawToLayer;
        /// </summary>
        protected void CallDrawRequest(EventSourceType eventSource)
        {
            this.RepaintToLayers = this.StandardDrawToLayer;
            this.OnDrawRequest();
            if (!this.IsSuppressedEvent && this.DrawRequest != null)
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
        /// Zajistí, že this prvek bude standardně vykreslen včetně všech svých <see cref="Childs"/>.
        /// </summary>
        /// <param name="item"></param>
        protected virtual void Repaint()
        {
            this.RepaintToLayers = this.StandardDrawToLayer;
            if (this.HasParent)
            {
                RepaintParentMode repaintParent = this.RepaintParent;
                if ((repaintParent == RepaintParentMode.OnBackColorAlpha && this.BackColor.A < 255) || repaintParent == RepaintParentMode.Always)
                    this.Parent.Repaint();
            }
        }
        /// <summary>
        /// Volba, zda metoda <see cref="Repaint()"/> způsobí i vyvolání metody <see cref="Parent"/>.<see cref="IInteractiveParent.Repaint"/>.
        /// </summary>
        protected virtual RepaintParentMode RepaintParent { get { return RepaintParentMode.None; } }
        /// <summary>
        /// Current (new) state of item (after this event, not before it).
        /// </summary>
        public GInteractiveState InteractiveState { get { return (this.IsEnabled ? this._InteractiveState : GInteractiveState.Disabled); } protected set { this._InteractiveState = value; } } private GInteractiveState _InteractiveState;
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
            GInteractiveState state = this.InteractiveState;
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
                    this.Repaint();
                    this.AfterStateChangedFocusEnter(e);
                    break;
                case GInteractiveChangeState.KeyboardKeyPress:
                    this.AfterStateChangedKeyPress(e);
                    break;
                case GInteractiveChangeState.KeyboardFocusLeave:
                    this._HasFocus = false;
                    this.Repaint();
                    this.AfterStateChangedFocusLeave(e);
                    break;

                case GInteractiveChangeState.MouseEnter:
                    this.Repaint();
                    this.AfterStateChangedMouseEnter(e);
                    this.PrepareToolTip(e);
                    break;
                case GInteractiveChangeState.LeftDown:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftUp:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.MouseLeave:
                    this.Repaint();
                    this.AfterStateChangedMouseLeave(e);
                    break;
                case GInteractiveChangeState.LeftClick:
                    this.Repaint();
                    this.AfterStateChangedLeftClick(e);
                    break;
                case GInteractiveChangeState.LeftDoubleClick:
                    this.Repaint();
                    this.AfterStateChangedLeftDoubleClick(e);
                    break;
                case GInteractiveChangeState.LeftLongClick:
                    this.Repaint();
                    this.AfterStateChangedLeftLongClick(e);
                    break;
                case GInteractiveChangeState.RightClick:
                    this.Repaint();
                    this.AfterStateChangedRightClick(e);
                    break;
            }
        }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = KeyboardFocusEnter
        /// Hodnota v <see cref="HasFocus"/> je nyní true.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = KeyboardKeyPress
        /// Hodnota v <see cref="HasFocus"/> je nyní true.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = KeyboardFocusLeave
        /// Hodnota v <see cref="HasFocus"/> je nyní false.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseEnter
        /// Přípravu tooltipu je vhodnější provést v metodě <see cref="PrepareToolTip(GInteractiveChangeStateArgs)"/>, ta je volaná hned poté.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftClick
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftDoubleClick
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedLeftDoubleClick(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftLongClick
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedLeftLongClick(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = RightClick
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedRightClick(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volána v události MouseEnter, a jejím úkolem je připravit data pro ToolTip.
        /// Metoda je volána poté, kdy byla volána metoda <see cref="AfterStateChangedMouseEnter"/>.
        /// Zobrazení ToolTipu zajišťuje jádro.
        /// Bázová třída InteractiveObject zde nedělá nic.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void PrepareToolTip(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Tato metoda je volána při každé Drag akci
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DragAction(GDragActionArgs e) { }
        /// <summary>
        /// Výchozí metoda pro kreslení prvku, volaná z jádra systému.
        /// Třída <see cref="InteractiveObject"/> vyvolá metodu <see cref="Draw(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void Draw(GInteractiveDrawArgs e)
        {
            this.Draw(e, this.BoundsAbsolute, DrawItemMode.Standard);
        }
        /// <summary>
        /// Metoda pro standardní vykreslení prvku.
        /// Bázová třída <see cref="InteractiveObject"/> v této metodě pouze vykreslí svůj prostor barvou pozadí <see cref="BackColor"/>.
        /// Pokud je předán režim kreslení drawMode, obsahující příznak <see cref="DrawItemMode.Ghost"/>, pak je barva pozadí modifikována tak, že její Alpha je 75% původního Alpha.
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice pro kreslení (pomáhá řešit Drag & Drop procesy)</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag & Drop procesy)</param>
        protected virtual void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            Color backColor = this.BackColor;
            if (drawMode.HasFlag(DrawItemMode.Ghost))
                backColor = backColor.CreateTransparent(0.75f);
            e.Graphics.FillRectangle(Skin.Brush(backColor), boundsAbsolute);
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
        /// <summary>
        /// Zajistí překreslení všech dodaných prvků
        /// </summary>
        /// <param name="items"></param>
        protected static void RepaintItems(params IInteractiveItem[] items)
        {
            foreach (IInteractiveItem item in items)
                if (item != null)
                    item.Repaint();
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
        protected bool IsSuppressedEvent { get { return this.Is.SuppressEvents; } set { this.Is.SuppressEvents = value; } }
        /// <summary>
        /// On constructor is stacked value from owner.IsSupressedEvent, and then is set to true.
        /// On Dispose is returned original value from stack to owner.IsSupressedEvent.
        /// </summary>
        protected class SuppressEventClass : IDisposable
        {
            public SuppressEventClass(InteractiveObject owner)
            {
                this._Owner = owner;
                this.OldSupressedEventValue = (owner != null ? owner.IsSuppressedEvent : false);
                if (this._Owner != null)
                    this._Owner.IsSuppressedEvent = true;
            }
            private InteractiveObject _Owner;
            private bool OldSupressedEventValue;
            void IDisposable.Dispose()
            {
                if (this._Owner != null)
                    this._Owner.IsSuppressedEvent = this.OldSupressedEventValue;
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
        #region IInteractiveItem + IInteractiveParent members
        // UInt32 IInteractiveItem.Id { get { return this._Id; } }
        // GInteractiveControl IInteractiveItem.Host { get { return this.Host; } set { this.Host = value; } }
        // IInteractiveParent IInteractiveItem.Parent { get { return this.Parent; } set { this.Parent = value; } }
        IEnumerable<IInteractiveItem> IInteractiveItem.Childs { get { return this.Childs; } }
        Rectangle IInteractiveItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        Padding? IInteractiveItem.ActivePadding { get { return this.ActivePadding; } set { this.ActivePadding = value; } }
        Rectangle? IInteractiveItem.AbsoluteInteractiveBounds { get { return this.AbsoluteInteractiveBounds; } set { this.AbsoluteInteractiveBounds = value; } }
        Boolean IInteractiveItem.IsInteractive { get { return this.IsInteractive; } }
        Boolean IInteractiveItem.IsVisible { get { return this.IsVisible; } set { this.IsVisible = value; } }
        Boolean IInteractiveItem.IsEnabled { get { return this.IsEnabled; } }
        Boolean IInteractiveItem.HoldMouse { get { return this.IsHoldMouse; } }
        ZOrder IInteractiveItem.ZOrder { get { return this.ZOrder; } }
        GInteractiveDrawLayer IInteractiveItem.StandardDrawToLayer { get { return this.StandardDrawToLayer; } }
        GInteractiveDrawLayer IInteractiveItem.RepaintToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        Boolean IInteractiveItem.IsActiveAtAbsolutePoint(Point absolutePoint) { return this.IsActiveAtAbsolutePoint(absolutePoint); }
        void IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            this.InteractiveState = e.TargetState;
            this.CurrentMouseRelativePoint = e.MouseRelativePoint;
            this.AfterStateChanged(e);
        }
        void IInteractiveItem.DragAction(GDragActionArgs e) { this.DragAction(e); }
        void IInteractiveItem.Draw(GInteractiveDrawArgs e) { this.Draw(e); }

        UInt32 IInteractiveParent.Id { get { return this._Id; } }
        GInteractiveControl IInteractiveParent.Host { get { return this.Host; } }
        IInteractiveParent IInteractiveParent.Parent { get { return this.Parent; } set { this.Parent = value; } }
        GInteractiveStyles IInteractiveParent.Style { get { return this.Style; } }
        Rectangle IInteractiveParent.BoundsClient { get { return this.BoundsClient; } }
        void IInteractiveParent.Repaint() { this.Repaint(); }
        #endregion
        #region Basic members
        /// <summary>
        /// ToString() = "Type.Name #Id"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = this.GetType().Name;
            if (this.Tag != null)
                text += "; Tag: " + this.Tag.ToString();
            else
                text += "; Id: " + this.Id.ToString();
            text += "; Bounds: " + this.Bounds.ToString();
            return text;
        }
        /// <summary>
        /// Jednoznačné ID tohoto objektu
        /// </summary>
        protected UInt32 _Id = ++LastId;
        protected static UInt32 LastId = 0;
        /// <summary>
        /// Returns unique ID for new IInteractiveItem object
        /// </summary>
        /// <returns></returns>
        public static UInt32 GetNextId() { return ++LastId; }
        #endregion
    }
    /// <summary>
    /// Režim kreslení prvku
    /// </summary>
    [Flags]
    public enum DrawItemMode
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None = 0,
        /// <summary>
        /// Standardní plné vykreslení
        /// </summary>
        Standard = 1,
        /// <summary>
        /// Ghost, kreslený v procesu Drag & Drop (buď na původní pozici, nebo na pozici přesouvané)
        /// </summary>
        Ghost = 2,
        /// <summary>
        /// Toto vykreslování se provádí v procesu Drag & Drop.
        /// Ale teprve ostatní hodnoty konkrétně určují, 
        /// </summary>
        InDragProcess = 0x010,
        /// <summary>
        /// Prvek je vykreslován do svých vlastních souřadnic <see cref="InteractiveObject.Bounds"/>.
        /// Tato hodnota je nastavena pouze tehdy, když je nastavena hodnota <see cref="InDragProcess"/>, 
        /// a aktuálně se provádí vykreslení do vrstvy <see cref="GInteractiveDrawLayer.Standard"/>.
        /// Při vykreslování mimo proces Drag & Drop se tato hodnota nenastavuje (i přesto, že se kreslí do originálních souřadnic a do standardní vrstvy).
        /// </summary>
        OriginalBounds = 0x100,
        /// <summary>
        /// Prvek je vykreslován do souřadnic, na kterých je aktuálně přesouván <see cref="InteractiveDragObject.BoundsDragTarget"/>.
        /// Tato hodnota tedy signalizuje vykreslování objektu "na cestě", bez ohledu na to, zda se "na cestě" vykresluje prvek standardně nebo jako ghost.
        /// Tato hodnota je nastavena pouze tehdy, když je nastavena hodnota <see cref="InDragProcess"/>, 
        /// a aktuálně se provádí vykreslení do vrstvy <see cref="GInteractiveDrawLayer.Interactive"/>.
        /// </summary>
        DraggedBounds = 0x200
    }
    #endregion
    #region class InteractiveDragObject : předek pro běžné objekty, které podporují přesouvání
    /// <summary>
    /// InteractiveDragObject : předek pro běžné objekty, které podporují přesouvání.
    /// Tato třída sama implementuje Drag pro aktuální objekt včetně podpory pro cílový prostor (Drop)
    /// </summary>
    public abstract class InteractiveDragObject : InteractiveObject, IInteractiveItem
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor.
        /// Do stylu tohoto objektu přidá hodnotu <see cref="GInteractiveStyles.DragDrawGhostOriginal"/>,
        /// která provede to, že objekt je při procesu Drag & Drop vykreslen do původní pozice jako Ghost, a do Drag pozice jako Standard.
        /// </summary>
        public InteractiveDragObject() : base()
        {
            this.Style |= GInteractiveStyles.DragDrawGhostOriginal;
        }
        /// <summary>
        /// Konstruktor.
        /// Do stylu tohoto objektu přidá dodanou hodnotu stylu.
        /// Je tedy na potomkovi, jaký styl kreslení si zadá.
        /// Vhodné hodnoty jsou:
        /// <see cref="GInteractiveStyles.DragDrawGhostOriginal"/> = ghost je na originální souřadnici a standard objekt se přesouvá;
        /// <see cref="GInteractiveStyles.DragDrawGhostInteractive"/> = ghost se přesouvá, a na originální souřadnici se stále nachází standard objekt;
        /// <see cref="GInteractiveStyles.None"/> = 
        /// </summary>
        /// <param name="dragStyles"
        public InteractiveDragObject(GInteractiveStyles dragStyles) : base()
        {
            this.Style |= dragStyles;
        }
        #endregion
        #region Interactivity - Dragging
        /// <summary>
        /// Převzetí aktivity od InteractiveObject ve věci Drag
        /// </summary>
        /// <param name="e"></param>
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
                        this.BoundsDragTarget = e.DragToBounds.Value;
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
        /// Hodnota převzatá z <see cref="CanDrag"/> v době, kdy začala akce <see cref="DragActionType.DragThisStart"/>.
        /// Je platná až do akce <see cref="DragActionType.DragThisEnd"/>.
        /// Při další akci Drag bude znovu vyhodnocena.
        /// </summary>
        private bool _CanDragCurrent;
        /// <summary>
        /// Property má vrátit true, pokud prvek (this) může být za své aktuální situace přetahován.
        /// Tato hodnota je načtena v době akce <see cref="DragActionType.DragThisStart"/>.
        /// Samosebou není jisto, kam bude přetahován.
        /// Bázová třída <see cref="InteractiveDragObject"/> vrací true.
        /// </summary>
        protected virtual bool CanDrag { get { return true; } }
        /// <summary>
        /// Volá se v procesu přesouvání, pro aktivní objekt.
        /// Bázová třída v době volání této metody má uložené cílové souřadnice (dle parametru targetRelativeBounds) v proměnné <see cref="BoundsDragTarget"/>.
        /// Potomek může tuto souřadnici v této metodě změnit, a upravenou ji vložit do <see cref="BoundsDragTarget"/>.
        /// Anebo může zavolat base.DragThisOverBounds(e, upravená souřadnice), bázová metoda tuto upravenou hodnotu opět uloží do <see cref="BoundsDragTarget"/>.
        /// Bázovou metodu <see cref="InteractiveDragObject.DragThisOverBounds(GDragActionArgs, Rectangle)"/> ale obecně není nutno volat.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected virtual void DragThisOverBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.BoundsDragTarget = targetRelativeBounds;
        }
        /// <summary>
        /// Volá se při ukončení Drag & Drop, při akci <see cref="DragActionType.DragThisDrop"/>, pro aktivní objekt (=ten který je přesouván).
        /// Bázová metoda <see cref="InteractiveDragObject.DragThisDropToBounds(GDragActionArgs, Rectangle)"/> vepíše předané souřadnice (parametr boundsTarget) 
        /// do this.Bounds pomocí metody <see cref="InteractiveObject.SetBounds(Rectangle, ProcessAction, EventSourceType)"/>.
        /// Pokud potomek chce modifikovat souřadnice, stačí změnit hodnotu parametru boundsTarget.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        protected virtual void DragThisDropToBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            this.SetBounds(boundsTarget, ProcessAction.DragValueActions, EventSourceType.InteractiveChanged | EventSourceType.BoundsChange);
        }
        /// <summary>
        /// Je voláno po skončení přetahování, ať už skončilo OK (=Drop) nebo Escape (=Cancel).
        /// Účelem je provést úklid aplikačních dat po skončení přetahování.
        /// Bázová třída InteractiveDragObject nedělá nic.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DragThisOverEnd(GDragActionArgs e)
        { }
        /// <summary>
        /// Souřadnice (Bounds) tohoto objektu, platné před začátkem procesu Drag & Drop.
        /// Jde o souřadnice relativní, obdobně jako <see cref="InteractiveObject.Bounds"/>.
        /// Do těchto souřadnic je objekt v době Drag & Drop vykreslován jako Ghost, pokud styl obsahuje <see cref="GInteractiveStyles.DragDrawGhostOriginal"/>.
        /// Mimo proces Drag & Drop je zde null.
        /// </summary>
        protected virtual Rectangle? BoundsDragOrigin { get; set; }
        /// <summary>
        /// Souřadnice (Bounds) tohoto objektu, kde se aktuálně nachází v procesu Drag & Drop.
        /// Jde o souřadnice relativní, obdobně jako <see cref="InteractiveObject.Bounds"/>.
        /// Do těchto souřadnic je objekt v době Drag & Drop vykreslován jako Ghost, pokud styl obsahuje <see cref="GInteractiveStyles.DragDrawGhostInteractive"/>,
        /// anebo jako standardní objektu pokud styl obsahuje <see cref="GInteractiveStyles.DragDrawGhostOriginal"/>.
        /// Mimo proces Drag & Drop je zde null.
        /// </summary>
        protected virtual Rectangle? BoundsDragTarget { get; set; }
        /// <summary>
        /// Vrstvy, které se mají překreslovat v době procesu Drag & Drop.
        /// </summary>
        protected virtual GInteractiveDrawLayer DragDrawToLayers { get { return (GInteractiveDrawLayer.Standard | GInteractiveDrawLayer.Interactive); } }
        #endregion
        #region Draw
        /// <summary>
        /// Metoda řeší kreslení prvku, který může být v procesu Drag & Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void Draw(GInteractiveDrawArgs e)
        {
            if (!this.IsDragged || !this.BoundsDragOrigin.HasValue)
                this.Draw(e, this.BoundsAbsolute, DrawItemMode.Standard);
            else
                this.DrawOnDragging(e);
        }
        /// <summary>
        /// Tato metoda zajistí vykreslení this objektu v době, kdy probíhá jeho Dragging.
        /// Bázová třída InteractiveDragObject v této metodě reaguje na styl <see cref="InteractiveObject.Style"/>, 
        /// a podle vlastností stylu <see cref="GInteractiveStyles.DragDrawGhostOriginal"/> a <see cref="GInteractiveStyles.DragDrawGhostInteractive"/>
        /// řídí vykreslení do souřadnic výchozích (BoundsDragOrigin) a do souřadnic Dragging (BoundsDragTarget), 
        /// do vrstev Standard a Interactive, pomocí volání metody <see cref="InteractiveObject.Draw(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>.
        /// Pokud chce potomek kreslit prvek v době přetahování jinak, může přepsat tuto metodu <see cref="DrawOnDragging(GInteractiveDrawArgs)"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DrawOnDragging(GInteractiveDrawArgs e)
        {
            bool ghostInOriginalBounds = ((this.Style & GInteractiveStyles.DragDrawGhostOriginal) != 0);
            bool ghostInDraggedBounds = ((this.Style & GInteractiveStyles.DragDrawGhostInteractive) != 0);
            GInteractiveDrawLayer currentLayer = e.DrawLayer;
            DrawItemMode drawMode = DrawItemMode.InDragProcess;

            Rectangle boundsAbsolute;
            if (currentLayer == GInteractiveDrawLayer.Standard)
            {   // Nyní kreslíme do vrstvy Standard, tedy kreslíme do výchozích souřadnic BoundsDragOrigin:
                boundsAbsolute = this.GetAbsoluteBounds(this.BoundsDragOrigin.Value);
                drawMode |= DrawItemMode.OriginalBounds;
                if (ghostInOriginalBounds)
                    // Máme styl DragDrawGhostOriginal, takže na originální souřadnice (tj. do standardní vrtsvy) máme vykreslit Ghost:
                    this.Draw(e, boundsAbsolute, (drawMode | DrawItemMode.Ghost));
                else if (ghostInDraggedBounds)
                    // Máme styl DragDrawGhostInteractive, takže na originální souřadnice (tj. do standardní vrtsvy) máme vykreslit Standard:
                    this.Draw(e, boundsAbsolute, (drawMode | DrawItemMode.Standard));
                // Pokud není specifikován ani jeden styl (DragDrawGhostOriginal ani DragDrawGhostInteractive),
                //  pak v procesu Drag & Drop nebude do standardní vrstvy kresleno nic (objekt se skutečně ihned odsouvá jinam).
                //  Objekt bude kreslen jako Standard do vrstvy Interactive na souřadnice Target.
            }
            else if (currentLayer == GInteractiveDrawLayer.Interactive)
            {   // Nyní kreslíme do vrstvy Interactive, tedy kreslíme do cílových souřadnic BoundsDragTarget:
                boundsAbsolute = this.GetAbsoluteBounds(this.BoundsDragTarget.Value);
                drawMode |= DrawItemMode.DraggedBounds;
                if (ghostInDraggedBounds)
                    // Máme styl DragDrawGhostInteractive, takže na cílové souřadnice (tj. do interaktivní vrstvy) máme vykreslit Ghost:
                    this.Draw(e, boundsAbsolute, (drawMode | DrawItemMode.Ghost));
                else
                    // Jinak (pro styl DragDrawGhostOriginal nebo pro nezadaný styl) budeme na cílové souřadnice (tj. do interaktivní vrstvy) vykreslovat Standard:
                    this.Draw(e, boundsAbsolute, (drawMode | DrawItemMode.Standard));
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
        /// Setting value to this property does not call any event (action = <seealso cref="Asol.Tools.WorkScheduler.Components.ProcessAction.SilentValueActions"SilentValueActions/>)
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
            GPropertyChangeArgs<TValue> args = new GPropertyChangeArgs<TValue>(oldValue, newValue, eventSource);
            this.OnValueChanged(args);
            if (!this.IsSuppressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
        }
        /// <summary>
        /// Occured after Value changed
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<TValue> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChangedHandler<TValue> ValueChanged;

        /// <summary>
        /// Call method OnValueRangeChanged() and event ValueChanged
        /// </summary>
        protected void CallValueRangeChanged(TRange oldValue, TRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TRange> args = new GPropertyChangeArgs<TRange>(oldValue, newValue, eventSource);
            this.OnValueRangeChanged(args);
            if (!this.IsSuppressedEvent && this.ValueRangeChanged != null)
                this.ValueRangeChanged(this, args);
        }
        /// <summary>
        /// Occured after ValueRange changed
        /// </summary>
        protected virtual void OnValueRangeChanged(GPropertyChangeArgs<TRange> args) { }
        /// <summary>
        /// Event on this.ValueRange changes
        /// </summary>
        public event GPropertyChangedHandler<TRange> ValueRangeChanged;


        #endregion
    }
    #endregion
}
