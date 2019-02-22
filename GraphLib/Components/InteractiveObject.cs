using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;

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
        /// Libovolný název tohoto controlu. Není povinné jej zadávat. Nemusí být jednoznačný. Nemá žádná pravidla co do obsahu.
        /// Je na aplikaci, jak jej naplní a jak jej bude vyhodnocovat.
        /// </summary>
        public virtual string Name { get; set; }
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
        /// Textem vyjádřená cesta ke všem Parentům, pro kontroly
        /// </summary>
        public string AllParentsPath
        {
            get
            {
                string result = "";
                IInteractiveParent item = this;
                while (item != null)
                {
                    result = item.GetType().Name + (result.Length == 0 ? "" : " => " + result);
                    item = item.Parent;
                }
                return result;
            }
        }
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
        public virtual Padding? InteractivePadding
        {
            get { return this.__ActivePadding; }
            set { this.__ActivePadding = value; }
        }
        /// <summary>
        /// Vnitřní okraj mezi <see cref="Bounds"/> a prostorem, v němž se kreslí <see cref="Childs"/> prvky.
        /// Typicky se používá pro vykreslení okraje this prvku (Border), do něhož se Child prvky nikdy nekreslí.
        /// Kladné hodnoty reprezentují reálný okraj, záporné hodnoty by mohly povolit přesah <see cref="Childs"/> prvků ven z parenta (z this prvku).
        /// </summary>
        public virtual Padding? ClientBorder 
        {
            get { return this.__ClientBorder; }
            set { this.__ClientBorder = value; }
        }
        /// <summary>
        /// Aktuálně použitá barva pozadí.
        /// Pokud <see cref="BackColorUser"/> bude null, použije se <see cref="DefaultBackColor"/>, což je Skin.Control.BackColor.
        /// Sem nelze setovat null.
        /// </summary>
        public virtual Color BackColor 
        {
            get { return (this.BackColorUser.HasValue ? this.BackColorUser.Value : this.DefaultBackColor) ; }
            set { this.BackColorUser = value; } 
        }
        /// <summary>
        /// Aktuálně vložená barva pozadí.
        /// Zadaná hodnota má přednost před <see cref="DefaultBackColor"/>.
        /// Lze vložit null, pak jako <see cref="BackColor"/> bude figurovat <see cref="DefaultBackColor"/>.
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
        protected virtual Color DefaultBackColor { get { return Skin.Control.ControlBackColor; } }
        /// <summary>
        /// Libovolný popisný údaj, na funkci nemá vliv.
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// Libovolný datový údaj, na funkci nemá vliv.
        /// </summary>
        public object UserData { get; set; }
        /// <summary>
        /// Z-Order pro tuto položku.
        /// </summary>
        public ZOrder ZOrder { get { return this.__ZOrder; } set { this.__ZOrder = value; } } private ZOrder __ZOrder = ZOrder.Standard;
        /// <summary>
        /// Zajistí vykreslení this prvku <see cref="Repaint()"/>, včetně překreslení Host controlu <see cref="GInteractiveControl"/>.
        /// </summary>
        public virtual void Refresh()
        {
            this.Repaint();
            GInteractiveControl host = this.Host;
            if (host != null) host.Refresh();
        }
        /// <summary>
        /// Režim pro kreslení prvku v době Drag and Drop.
        /// </summary>
        protected DragDrawGhostMode DragDrawGhostMode
        {
            get
            {
                if (this.Is.DrawDragMoveGhostInteractive) return DragDrawGhostMode.DragWithGhostOnInteractive;
                if (this.Is.DrawDragMoveGhostStandard) return DragDrawGhostMode.DragWithGhostAtOriginal;
                return DragDrawGhostMode.DragOnlyStandard; 
            }
        }
        /// <summary>
        /// Relativní souřadnice this prvku v rámci parenta.
        /// Toto jsou souřadnice objektu v Interaktivní vrstvě v době procesu Drag and Drop.
        /// Třída <see cref="InteractiveObject"/> tuto property implementuje, ale nevyužívá; to nechává na potomstvu.
        /// </summary>
        protected virtual Rectangle? BoundsInteractive { get { return this._BoundsInteractive; } set { this._BoundsInteractive = value; } } private Rectangle? _BoundsInteractive;
        /// <summary>
        /// Obsahuje true, pokud this prvek má nad sebou myš (nebo některý jeho Child)
        /// </summary>
        public virtual bool HasMouse { get { return this._HasMouse; } set { this._HasMouse = value; } } private bool _HasMouse;
        /// <summary>
        /// Obsahuje true, pokud this prvek má klávesový focus.
        /// </summary>
        public virtual bool HasFocus { get { return this._HasFocus; } set { this._HasFocus = value; } } private bool _HasFocus;
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
            bool isChange = (oldBounds != newBounds);
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, this.GetType().Name, "SetBounds", "", "OldBounds: " + oldBounds, "NewBounds: " + newBounds))
            {
                if (isChange)
                {
                    this._Bounds = bounds;
                    this.CallActionsAfterBoundsChanged(oldBounds, newBounds, ref actions, eventSource);
                }
            }
            return isChange;
        }
        /// <summary>
        /// Vyvolá patřičné akce po změně <see cref="Bounds"/>.
        /// Vždy zavolá: <see cref="SetBoundsAfterChange(Rectangle, Rectangle, ref ProcessAction, EventSourceType)"/>; 
        /// a pak podle akcí specifikovaných v parametr actions: 
        /// <see cref="SetBoundsRecalcInnerData(Rectangle, Rectangle, ref ProcessAction, EventSourceType)"/> a
        /// <see cref="SetBoundsPrepareInnerItems(Rectangle, Rectangle, ref ProcessAction, EventSourceType)"/>, poté
        /// <see cref="CallBoundsChanged(Rectangle, Rectangle, EventSourceType)"/> a
        /// <see cref="CallDrawRequest(EventSourceType)"/>.
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
            if (IsAction(actions, ProcessAction.CallDraw) && this.Host != null)
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Je voláno po změně souřadnic <see cref="Bounds"/>, z metody <see cref="SetBounds(Rectangle, ProcessAction, EventSourceType)"/>, bez dalších podmínke (tj. vždy po změně).
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetBoundsAfterChange(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Je voláno po změně souřadnic <see cref="Bounds"/>, z metody <see cref="SetBounds(Rectangle, ProcessAction, EventSourceType)"/>, pokud je specifikována akce <see cref="ProcessAction.RecalcInnerData"/>.
        /// Účelem je přepočítat data závislí na souřadnicích.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetBoundsRecalcInnerData(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Je voláno po změně souřadnic <see cref="InteractiveObject.Bounds"/>, z metody <see cref="InteractiveObject.SetBounds(Rectangle, ProcessAction, EventSourceType)"/>, pokud je specifikována akce <see cref="ProcessAction.PrepareInnerItems"/>.
        /// Účelem je přepočítat souřadnice vnořených závislých prvků.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Souřadnice this prvku převedené do absolutní hodnoty.
        /// </summary>
        public virtual Rectangle BoundsAbsolute
        {
            get
            {
                BoundsInfo boundsInfo = BoundsInfo.CreateForChild(this);
                return boundsInfo.CurrentAbsBounds;
            }
        }
        /// <summary>
        /// Komplexní informace o souřadném systému a dalších náležitostech this objektu:
        /// <see cref="BoundsInfo.IsVisible"/>; <see cref="BoundsInfo.IsEnabled"/>; <see cref="BoundsInfo.CurrentIsVisible"/>; <see cref="BoundsInfo.CurrentIsEnabled"/>; 
        /// </summary>
        public virtual BoundsInfo BoundsInfo { get { return BoundsInfo.CreateForChild(this); } }
        /// <summary>
        /// Velikost prostoru pro klienty = <see cref="IInteractiveItem.Bounds"/>.Sub(<see cref="IInteractiveItem.ClientBorder"/>).Size
        /// </summary>
        protected virtual Size ClientSize { get { return this.Bounds.Sub(this.ClientBorder).Size; } }
        /// <summary>
        /// Private accessor for Bounds value.
        /// Setting a value calls BoundsInvalidate().
        /// </summary>
        protected Rectangle _Bounds
        {
            get { return this.__Bounds; }
            set { this.__Bounds = value; }
        }
        private Rectangle __Bounds;
        private Padding? __ClientBorder;
        private Padding? __ActivePadding;
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
            this.Repaint();
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
        /// Souřadnice je v koordinátech Parenta prvku, je tedy srovnatelná s <see cref="IInteractiveItem.Bounds"/>.
        /// Pokud prvek má nepravidelný tvar, musí testovat tento tvar v této své metodě explicitně.
        /// </summary>
        /// <param name="relativePoint">Bod, který testujeme, v koordinátech srovnatelných s <see cref="IInteractiveItem.Bounds"/></param>
        /// <returns></returns>
        protected virtual Boolean IsActiveAtPoint(Point relativePoint)
        {
            Rectangle bounds = this.Bounds.Add(this.InteractivePadding);       // Relativní souřadnice this prvku, zvětšené o interaktivní přesah
            return bounds.Contains(relativePoint);
        }
        /// <summary>
        /// Vrstvy, do nichž se běžně má vykreslovat tento objekt.
        /// Tato hodnota se v metodě <see cref="InteractiveObject.Repaint()"/> použije jako informace, do kterých vrstev se má prvek překreslit.
        /// Vrstva <see cref="GInteractiveDrawLayer.Standard"/> je běžná pro normální kreslení;
        /// vrstva <see cref="GInteractiveDrawLayer.Interactive"/> se používá při Drag and Drop;
        /// vrstva <see cref="GInteractiveDrawLayer.Dynamic"/> se používá pro kreslení linek mezi prvky nad vrstvou při přetahování.
        /// Vrstvy lze kombinovat.
        /// Vrstva <see cref="GInteractiveDrawLayer.None"/> je přípustná:  prvek se nekreslí, ale je přítomný a interaktivní.
        /// </summary>
        protected virtual GInteractiveDrawLayer StandardDrawToLayer { get { return GInteractiveDrawLayer.Standard; } }
        /// <summary>
        /// Vrstvy, do nichž se aktuálně (tj. v nejbližším kreslení) bude vykreslovat tento objekt.
        /// Po vykreslení se sem ukládá None, tím se šetří čas na kreslení (nekreslí se nezměněné prvky).
        /// </summary>
        protected virtual GInteractiveDrawLayer RepaintToLayers { get; set; }
        /// <summary>
        /// Pokud je zde true, pak v procesu kreslení prvku je po standardním vykreslení this prvku <see cref="Draw(GInteractiveDrawArgs, Rectangle, Rectangle)"/> 
        /// a po standardním vykreslení všech <see cref="Childs"/> prvků ještě vyvolána metoda <see cref="DrawOverChilds(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/> pro this prvek.
        /// </summary>
        protected virtual bool NeedDrawOverChilds { get; set; }
        /// <summary>
        /// Zajistí, že this prvek bude standardně vykreslen do své standardní vrstvy <see cref="StandardDrawToLayer"/>,
        /// včetně všech svých <see cref="Childs"/>, 
        /// volitelně bude provedeno vykreslení Parent objektu.
        /// </summary>
        protected virtual void Repaint()
        {
            this.Repaint(this.StandardDrawToLayer);
        }
        /// <summary>
        /// Zajistí, že this prvek bude standardně vykreslen do daných vrstev, 
        /// včetně všech svých <see cref="Childs"/>, 
        /// volitelně bude provedeno vykreslení Parent objektu.
        /// </summary>
        /// <param name="repaintLayers"></param>
        protected virtual void Repaint(GInteractiveDrawLayer repaintLayers)
        {
            if (repaintLayers == GInteractiveDrawLayer.None || this.Host == null) return;

            // this.Host.AddItemToDraw(this, repaintLayers);

            this.RepaintToLayers = repaintLayers;
            if (this.HasParent)
            {
                RepaintParentMode repaintParent = this.RepaintParent;
                if (repaintParent == RepaintParentMode.Always || (repaintParent == RepaintParentMode.OnBackColorAlpha && this.BackColor.A < 255))
                    this.Parent.Repaint();       // původně Repaint(repaintLayers) : chyby v kreslení Ghost, výrazně pomalé.
            }
        }
        /// <summary>
        /// Volba, zda metoda <see cref="Repaint()"/> způsobí i vyvolání metody <see cref="Parent"/>.<see cref="IInteractiveParent.Repaint()"/>.
        /// </summary>
        protected virtual RepaintParentMode RepaintParent { get { return RepaintParentMode.None; } }
        /// <summary>
        /// Obsahuje true, pokud this objekt je nyní přemisťován akcí DragMove 
        /// (<see cref="InteractiveState"/> je <see cref="GInteractiveState.LeftDrag"/> nebo <see cref="GInteractiveState.RightDrag"/>)
        /// </summary>
        protected bool IsDragged { get { return this.HasInteractiveStateFlag(GInteractiveState.FlagDrag); } }
        /// <summary>
        /// Obsahuje true, pokud na this objektu začal výběr DragFrame 
        /// (<see cref="InteractiveState"/> je <see cref="GInteractiveState.LeftFrame"/> nebo <see cref="GInteractiveState.RightFrame"/>)
        /// </summary>
        protected bool IsFrameParent { get { return this.IsInInteractiveState(GInteractiveState.LeftFrame, GInteractiveState.RightFrame); } }
        /// <summary>
        /// true when this has mouse (CurrentState is MouseOver, LeftDown, RightDown, LeftDrag or RightDrag)
        /// </summary>
        protected bool IsMouseActive { get { return this.IsInInteractiveState(GInteractiveState.MouseOver, GInteractiveState.LeftDown, GInteractiveState.RightDown, GInteractiveState.LeftDrag, GInteractiveState.RightDrag, GInteractiveState.LeftFrame, GInteractiveState.RightFrame); } }
        /// <summary>
        /// true when this has mouse down (CurrentState is LeftDown, RightDown, LeftDrag or RightDrag)
        /// </summary>
        protected bool IsMouseDown { get { return this.IsInInteractiveState(GInteractiveState.LeftDown, GInteractiveState.RightDown, GInteractiveState.LeftDrag, GInteractiveState.RightDrag, GInteractiveState.LeftFrame, GInteractiveState.RightFrame); } }
        /// <summary>
        /// Vrátí true, pokud <see cref="InteractiveState"/> má některou ze zadaných hodnot. 
        /// Porovnává se přesná rovnost, nikoli částečná shoda příznaků, na to existuje metoda <see cref="HasInteractiveStateFlag(GInteractiveState)"/>.
        /// </summary>
        /// <param name="states"></param>
        /// <returns></returns>
        protected bool IsInInteractiveState(params GInteractiveState[] states)
        {
            if (states == null || states.Length == 0) return false;
            GInteractiveState currentState = this.InteractiveState;
            return states.Any(state => (state == currentState));
        }
        /// <summary>
        /// Vrátí true, pokud <see cref="InteractiveState"/> má nahozený některý příznak (bit) z dodaných.
        /// Stav má jednotlivé příznaky, z nichž se skládá.
        /// Tato metoda netestuje exaktní shodu, na to existuje metoda <see cref="IsInInteractiveState(GInteractiveState[])"/>.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected bool HasInteractiveStateFlag(GInteractiveState state)
        {
            return ((this.InteractiveState & state) != 0);                     // Jde o příznaky [Flags], takže mě stačí shoda jednoho bitu
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
            bool isEnabled = this.Is.Enabled;
            if (!isEnabled) return;

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardFocusEnter:
                    this._HasFocus = true;
                    this.AfterStateChangedFocusEnter(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.KeyboardKeyPress:
                    this.AfterStateChangedKeyPress(e);
                    break;
                case GInteractiveChangeState.KeyboardFocusLeave:
                    this._HasFocus = false;
                    this.AfterStateChangedFocusLeave(e);
                    this.Repaint();
                    break;

                case GInteractiveChangeState.MouseEnter:
                    this._HasMouse = true;
                    this.AfterStateChangedMouseEnter(e);
                    this.PrepareToolTip(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.MouseOver:
                    this.AfterStateChangedMouseOver(e);
                    break;
                case GInteractiveChangeState.LeftDown:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftUp:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.MouseLeave:
                    this._HasMouse = false;
                    this.AfterStateChangedMouseLeave(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftClick:
                    this.AfterStateChangedLeftClick(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftClickSelect:
                    this.IsSelectedTryToggle(e.ModifierKeys.HasFlag(Keys.Control));
                    this.AfterStateChangedLeftClickSelected(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftDoubleClick:
                    this.AfterStateChangedLeftDoubleClick(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftLongClick:
                    this.AfterStateChangedLeftLongClick(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.RightClick:
                    this.AfterStateChangedRightClick(e);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.WheelUp:
                case GInteractiveChangeState.WheelDown:
                    this.AfterStateChangedWheel(e);
                    break;
                case GInteractiveChangeState.LeftDragFrameBegin:
                case GInteractiveChangeState.RightDragFrameBegin:
                    this.AfterStateChangedDragFrameBegin(e);
                    break;
                case GInteractiveChangeState.LeftDragFrameSelect:
                case GInteractiveChangeState.RightDragFrameSelect:
                    this.AfterStateChangedDragFrameSelect(e);
                    break;
                case GInteractiveChangeState.LeftDragFrameDone:
                case GInteractiveChangeState.RightDragFrameDone:
                    this.AfterStateChangedDragFrameDone(e);
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
        /// Přípravu tooltipu je vhodnější provést v metodě <see cref="InteractiveObject.PrepareToolTip(GInteractiveChangeStateArgs)"/>, ta je volaná hned poté.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseOver.
        /// Základní třída nedělá vůbec nic.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedMouseOver(GInteractiveChangeStateArgs e) { }
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
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftClickSelected
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedLeftClickSelected(GInteractiveChangeStateArgs e) { }
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
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = WheelUp i WhellDown
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedWheel(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftDragFrameBegin i RightDragFrameBegin.
        /// Tato metoda se volá pro objekt, na němž akce DragFrame začíná (objekt má <see cref="InteractiveProperties.SelectParent"/> == true),
        /// a nyní na něm byla zmáčknutá myš a začíná se označovat oblast výběru.
        /// Objekt může omezit rozsah oblasti tak, že nastaví do argumentu e hodnotu <see cref="GInteractiveChangeStateArgs.DragFrameWorkArea"/> na požadovanou oblast.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedDragFrameBegin(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftDragFrameSelect i RightDragFrameSelect.
        /// Metoda se volá do jednotlivých objektů, které by měly být selectovány.
        /// Objekt sám si nic nastavovat nemusí, to řeší control.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedDragFrameSelect(GInteractiveChangeStateArgs e) { }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftDragFrameDone i RightDragFrameDone.
        /// Tato metoda se volá pro objekt, na němž akce DragFrame začala (dříve proběhla akce <see cref="GInteractiveChangeState.LeftDragFrameBegin"/> 
        /// nebo <see cref="GInteractiveChangeState.RightDragFrameBegin"/>).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void AfterStateChangedDragFrameDone(GInteractiveChangeStateArgs e) { }
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
        /// Třída <see cref="InteractiveObject"/> vyvolá metodu <see cref="Draw(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/>.
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected virtual void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            this.Draw(e, absoluteBounds, absoluteVisibleBounds, DrawItemMode.Standard);
        }
        /// <summary>
        /// Tato metoda je volaná pro prvek, který má nastaveno <see cref="NeedDrawOverChilds"/> == true, poté když tento prvek byl vykreslen, a následně byly vykresleny jeho <see cref="Childs"/>.
        /// Umožňuje tedy kreslit "nad" svoje <see cref="Childs"/> (tj. počmárat je).
        /// Tento postup se používá typicky jen pro zobrazení překryvného textu přes <see cref="Childs"/> prvky, které svůj text nenesou.
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected virtual void DrawOverChilds(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds) { }
        /// <summary>
        /// Metoda pro standardní vykreslení prvku.
        /// Bázová třída <see cref="InteractiveObject"/> v této metodě pouze vykreslí svůj prostor barvou pozadí <see cref="InteractiveObject.BackColor"/>.
        /// Pokud je předán režim kreslení drawMode, obsahující příznak <see cref="DrawItemMode.Ghost"/>, pak je barva pozadí modifikována tak, že její Alpha je 75% původního Alpha.
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected virtual void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Color backColor = this.BackColor;
            if (drawMode.HasFlag(DrawItemMode.Ghost))
                backColor = backColor.CreateTransparent(0.75f);
            e.Graphics.FillRectangle(Skin.Brush(backColor), absoluteBounds);
        }
        /// <summary>
        /// Metoda pro vykreslení prvku "OverChilds".
        /// Bázová třída <see cref="InteractiveObject"/> v této metodě nedělá nic.
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice pro kreslení (pomáhá řešit Drag and Drop procesy)</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected virtual void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        { }
        #endregion
        #region Obecná podpora potomků 
        #region Práce s hodnotami ProcessAction (protected static) : IsAction(), AddActions(), RemoveActions(), LeaveOnlyActions()
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
        #endregion
        #region Práce s hodnotami EventSourceType (protected static) : IsEventSource(); podpora pro RepaintItems(IEnumerable)
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
        #region Další metody typicky používané v Controlech
        /// <summary>
        /// Vrací výsledek matrice 2 x 2
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t0">Vstup 0</param>
        /// <param name="t1">Vstup 1</param>
        /// <param name="value00">Výstup pro 0 = false; 1 = false</param>
        /// <param name="value01">Výstup pro 0 = false; 1 = true</param>
        /// <param name="value10">Výstup pro 0 = true; 1 = false</param>
        /// <param name="value11">Výstup pro 0 = true; 1 = true</param>
        /// <returns></returns>
        public static T GetMatrix<T>(bool t0, bool t1, T value00, T value01, T value10, T value11)
        {
            return (t0 ? (t1 ? value11 : value10) : (t1 ? value01 : value00));
        }

        #endregion
        #endregion
        #region Podpora pro hledání parentů: SearchForParent(), SearchForItem()
        /// <summary>
        /// Metoda vyhledá nejbližšího parenta daného typu.
        /// Může vrátit null.
        /// Sebe sama netestuje!
        /// </summary>
        /// <param name="parentType">Typ objektu, který hledáme.</param>
        /// <returns></returns>
        public IInteractiveParent SearchForParent(Type parentType)
        {
            return SearchForParent(this, parentType);
        }
        /// <summary>
        /// Metoda vyhledá nejbližšího parenta daného typu.
        /// Může vrátit null.
        /// Sebe sama netestuje!
        /// </summary>
        /// <param name="item">První prvek v řadě, bude se prohledávat jeho hierarchie směrem k Parent</param>
        /// <param name="parentType">Typ objektu, který hledáme.</param>
        /// <returns></returns>
        public static IInteractiveParent SearchForParent(IInteractiveItem item, Type parentType)
        {
            return SearchForItem(item, false, i => (i.GetType() == parentType));
        }
        /// <summary>
        /// Metoda vyhledá nejbližšího parenta, vyhovujícího danému filtru.
        /// Může vrátit null.
        /// Sebe sama netestuje!
        /// </summary>
        /// <param name="filter">Filtrační podmínka</param>
        /// <returns></returns>
        public IInteractiveParent SearchForParent(Func<IInteractiveParent,bool> filter)
        {
            return SearchForItem(this, false, filter);
        }
        /// <summary>
        /// Metoda vyhledá nejbližšího parenta, vyhovujícího danému filtru.
        /// Může vrátit null.
        /// Sebe sama netestuje!
        /// </summary>
        /// <param name="item">První prvek v řadě, bude se prohledávat jeho hierarchie směrem k Parent</param>
        /// <param name="filter">Filtrační podmínka</param>
        /// <returns></returns>
        public static IInteractiveParent SearchForParent(IInteractiveItem item, Func<IInteractiveParent, bool> filter)
        {
            return SearchForItem(item, false, filter);
        }
        /// <summary>
        /// Metoda vyhledá nejbližší prvek, který je daného typu.
        /// Sebe sama testuje, pokud parametr (withCurrent) je true; 
        /// anebo netestuje (pokud withCurrent je false), pak se chová jako metoda <see cref="SearchForParent(IInteractiveItem, Func{IInteractiveParent, bool})"/>.
        /// Může vrátit null.
        /// </summary>
        /// <param name="item">První prvek v řadě, bude se prohledávat jeho hierarchie směrem k Parent</param>
        /// <param name="withCurrent">Testovat i sebe? false = ne (hledám parenty), true = ano (i prvek item může vyhovovat)</param>
        /// <param name="itemType">Typ objektu, který hledáme.</param>
        /// <returns></returns>
        public static IInteractiveParent SearchForItem(IInteractiveItem item, bool withCurrent, Type itemType)
        {
            return SearchForItem(item, withCurrent, i => (i.GetType() == itemType));
        }
        /// <summary>
        /// Metoda vyhledá nejbližší prvek, vyhovujícího danému filtru.
        /// Sebe sama testuje, pokud parametr (withCurrent) je true; 
        /// anebo netestuje (pokud withCurrent je false), pak se chová jako metoda <see cref="SearchForParent(IInteractiveItem, Func{IInteractiveParent, bool})"/>.
        /// Může vrátit null.
        /// </summary>
        /// <param name="item">První prvek v řadě, bude se prohledávat jeho hierarchie směrem k Parent</param>
        /// <param name="withCurrent">Testovat i sebe? false = ne (hledám parenty), true = ano (i prvek item může vyhovovat)</param>
        /// <param name="filter">Filtrační podmínka</param>
        /// <returns></returns>
        public static IInteractiveParent SearchForItem(IInteractiveItem item, bool withCurrent, Func<IInteractiveParent, bool> filter)
        {
            if (item == null) return null;
            IInteractiveParent testItem = (withCurrent ? item : item.Parent);  // První prvek, který budeme testovat
            bool testCycle = !withCurrent;                                     // Test zacyklení v první smyčce budeme provádět jen tehdy, když v první smyčce řešíme Parenta, ale ne withCurrent
            while (testItem != null)
            {
                if (!testCycle) testCycle = true;                              // Pokud jsme v první smyčce a netestujeme zde zacyklení, pak nastavíme true pro další smyčku.
                else if (Object.ReferenceEquals(testItem, item)) return null;  // Protože máme testovat zacyklení, provedeme to zde...
                if (filter(testItem)) return testItem;                         // Shoda filtru => Úspěch
                testItem = testItem.Parent;                                    // Jdeme o level dál ve směru k Parentovi
            }
            return null;                                                       // Neúspěch
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
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
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
        #region Interaktivní vlastnosti
        /// <summary>
        /// Aktuální stav tohoto objektu po dokončení aktuálního eventu (ne před ním)
        /// </summary>
        public GInteractiveState InteractiveState { get { return (this.Is.Enabled ? this._InteractiveState : GInteractiveState.Disabled); } protected set { this._InteractiveState = value; } }
        private GInteractiveState _InteractiveState;
        /// <summary>
        /// Vyvolá háček OnInteractiveStateChanged a event InteractiveStateChanged
        /// </summary>
        /// <param name="args"></param>
        protected void CallInteractiveStateChanged(GPropertyChangeArgs<GInteractiveState> args)
        {
            this.OnInteractiveStateChanged(args);
            if (this.InteractiveStateChanged != null)
                this.InteractiveStateChanged(this, args);
        }
        /// <summary>
        /// Háček při změně hodnoty <see cref="InteractiveObject.InteractiveState"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnInteractiveStateChanged(GPropertyChangeArgs<GInteractiveState> args)
        { }
        /// <summary>
        /// Událost při změně hodnoty <see cref="InteractiveObject.InteractiveState"/>
        /// </summary>
        public event GPropertyChangedHandler<GInteractiveState> InteractiveStateChanged;
        /// <summary>
        /// Událost před změnou interaktivního stavu
        /// </summary>
        public event GInteractiveChangeStateHandler InteractiveStateChange;
        /// <summary>
        /// Tato hodnota vyjadřuje výběr prvků ze strany hostitele, k další editaci / Copy and Paste / atd.
        /// Hostitel tuto hodnotu nastavuje tehdy, když <see cref="InteractiveProperties.Selectable"/> je true, jinak ne.
        /// Hodnota je evidována centrálně v instanci <see cref="GInteractiveControl.Selector"/>.
        /// Nastavením této hodnoty se nemění hodnota <see cref="IsSelected"/> jiných prvků, ty se ponechávají beze změny.
        /// Pokud volající chce, může ostatní prvky odselectovat použitím objektu Host.Selector a jeho metody <see cref="Selector.ClearSelected()"/>.
        /// <para/>
        /// Tato hodnota neobsahuje stav Framování (v procesu Drag and Frame), ten je k dispozici v <see cref="IsFramed"/>.
        /// </summary>
        public virtual bool IsSelected
        {
            get { var host = this.Host; return (host != null ? host.Selector.IsSelected(this) : false); }
            set
            {
                var host = this.Host;
                if (host == null) return;
                bool oldValue = host.Selector.IsSelected(this);
                bool newValue = value;
                if (oldValue == newValue) return;
                ((ISelectorInternal)host.Selector).SetSelectedValue(this, value);
                this.CallIsSelectedChanged(new Data.GPropertyChangeArgs<bool>(oldValue, newValue, EventSourceType.ApplicationCode));
                this.Repaint();
            }
        }
        /// <summary>
        /// Metoda zjistí, zda prvek umožňuje změnit stav <see cref="IsSelected"/>, a pokud ano pak jej změní (vyvolá metodu <see cref="IsSelectedToggle(bool?)"/>.
        /// Metoda změní stav <see cref="IsSelected"/> tehdy, když (this.Is.Selectable and this.Is.Enabled) je true.
        /// </summary>
        /// <param name="leaveOtherSelected"></param>
        public void IsSelectedTryToggle(bool? leaveOtherSelected = null)
        {
            if (this.Is.Selectable && this.Is.Enabled)
                this.IsSelectedToggle(leaveOtherSelected);
        }
        /// <summary>
        /// Metoda změní stav Selected na tomto prvku, stejně jako by na prvku kliknul uživatel levou myší.
        /// To, zda se mají ponechat ostatní dosud selectované prvky v jejcih stavu, nebo nikoliv, definuje parametr leaveOtherSelected:
        /// a) pokud je null, pak se volba odvodí od stavu klávesy CTRL (jak je běžné): stisknutá klávesa = ostatní selectované se ponechají;
        /// b) pokud leaveOtherSelected má hodnotu true, pak se ostatní selectované prvky ponechají;
        /// b) pokud leaveOtherSelected má hodnotu false, pak se ostatní selectované prvky nejprve odselectují.
        /// </summary>
        public void IsSelectedToggle(bool? leaveOtherSelected = null)
        {
            if (this.Host != null)
            {
                bool leaveOther = (leaveOtherSelected.HasValue ? leaveOtherSelected.Value : Control.ModifierKeys.HasFlag(Keys.Control));
                this.Host.Selector.ChangeSelection(this, leaveOther);
            }
        }
        /// <summary>
        /// Vyvolá háček OnIsSelectedChanged a event IsSelectedChanged
        /// </summary>
        /// <param name="args"></param>
        protected void CallIsSelectedChanged(GPropertyChangeArgs<bool> args)
        {
            this.OnIsSelectedChanged(args);
            if (this.IsSelectedChanged != null)
                this.IsSelectedChanged(this, args);
        }
        /// <summary>
        /// Háček při změně hodnoty <see cref="InteractiveObject.IsSelected"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIsSelectedChanged(Data.GPropertyChangeArgs<bool> args)
        { }
        /// <summary>
        /// Událost při změně hodnoty <see cref="IsSelected"/>
        /// </summary>
        public event GPropertyChangedHandler<bool> IsSelectedChanged;
        /// <summary>
        /// Je aktuálně zarámován (pro budoucí selectování)?
        /// Zarámovaný prvek (v procesu hromadného označování myší SelectFrame) má <see cref="IsFramed"/> = true, ale hodnotu <see cref="IsSelected"/> má beze změn.
        /// Teprve na konci procesu SelectFrame se pro dotčené objekty (které mají <see cref="IsFramed"/> = true) nastaví i <see cref="IsSelected"/> = true.
        /// </summary>
        public virtual bool IsFramed
        {
            get { var host = this.Host; return (host != null ? host.Selector.IsFramed(this) : false); }
            set
            {
                var host = this.Host;
                if (host == null) return;
                bool oldValue = host.Selector.IsFramed(this);
                bool newValue = value;
                if (oldValue == newValue) return;
                ((ISelectorInternal)host.Selector).SetFramedValue(this, value);
                this.CallIsFramedChanged(new Data.GPropertyChangeArgs<bool>(oldValue, newValue, EventSourceType.ApplicationCode));
                this.Repaint();
            }
        }
        /// <summary>
        /// Vyvolá háček OnIsFramedChanged a event IsFramedChanged
        /// </summary>
        /// <param name="args"></param>
        protected void CallIsFramedChanged(Data.GPropertyChangeArgs<bool> args)
        {
            this.OnIsFramedChanged(args);
            if (this.IsFramedChanged != null)
                this.IsFramedChanged(this, args);
        }
        /// <summary>
        /// Háček při změně hodnoty <see cref="IsFramed"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIsFramedChanged(Data.GPropertyChangeArgs<bool> args)
        { }
        /// <summary>
        /// Událost při změně hodnoty <see cref="IsFramed"/>
        /// </summary>
        public event GPropertyChangedHandler<bool> IsFramedChanged;
        /// <summary>
        /// Je prvek "Aktivován" (nějakou aplikační akcí)?
        /// Aktivovaný prvek není ani Selectovaný, ani Framovaný. Změna <see cref="IsSelected"/> okolních prvků nijak nezmění <see cref="IsActivated"/>.
        /// Aktivovaný prvek se v podstatě permanentně zobrazuje jako výraznější než okolní prvky, například proto, že je problematický nebo odpovídá nějakému jinému zadání.
        /// </summary>
        public virtual bool IsActivated
        {
            get { var host = this.Host; return (host != null ? host.Selector.IsActivated(this) : false); }
            set
            {
                var host = this.Host;
                if (host == null) return;
                bool oldValue = host.Selector.IsActivated(this);
                bool newValue = value;
                if (oldValue == newValue) return;
                ((ISelectorInternal)host.Selector).SetActivatedValue(this, value);
                this.CallIsActivatedChanged(new Data.GPropertyChangeArgs<bool>(oldValue, newValue, EventSourceType.ApplicationCode));
                this.Repaint();
            }
        }
        /// <summary>
        /// Vyvolá háček OnIsActivatedChanged a event IsActivatedChanged
        /// </summary>
        /// <param name="args"></param>
        protected void CallIsActivatedChanged(Data.GPropertyChangeArgs<bool> args)
        {
            this.OnIsActivatedChanged(args);
            if (this.IsActivatedChanged != null)
                this.IsActivatedChanged(this, args);
        }
        /// <summary>
        /// Háček při změně hodnoty <see cref="IsActivated"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIsActivatedChanged(Data.GPropertyChangeArgs<bool> args)
        { }
        /// <summary>
        /// Událost při změně hodnoty <see cref="IsActivated"/>
        /// </summary>
        public event GPropertyChangedHandler<bool> IsActivatedChanged;
        #endregion
        #region Boolean repository
        /// <summary>
        /// All interactive boolean properties
        /// </summary>
        public InteractiveProperties Is { get { if (this._Is == null) this._Is = new InteractiveProperties(); return this._Is; } protected set { this._Is = value; } } private InteractiveProperties _Is;
        #endregion
        #region IInteractiveItem + IInteractiveParent members
        Rectangle IInteractiveItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        Rectangle? IInteractiveItem.BoundsInteractive { get { return this.BoundsInteractive; } }
        Padding? IInteractiveItem.InteractivePadding { get { return this.InteractivePadding; } set { this.InteractivePadding = value; } }
        Padding? IInteractiveItem.ClientBorder { get { return this.ClientBorder; } set { this.ClientBorder = value; } }
        IEnumerable<IInteractiveItem> IInteractiveItem.Childs { get { return this.Childs; } }
        InteractiveProperties IInteractiveItem.Is { get { return this.Is; } }
        Boolean IInteractiveItem.IsSelected { get { return this.IsSelected; } set { this.IsSelected = value; } }
        Boolean IInteractiveItem.IsFramed { get { return this.IsFramed; } set { this.IsFramed = value; } }
        Boolean IInteractiveItem.IsActivated { get { return this.IsActivated; } set { this.IsActivated = value; } }
        ZOrder IInteractiveItem.ZOrder { get { return this.ZOrder; } }
        GInteractiveDrawLayer IInteractiveItem.StandardDrawToLayer { get { return this.StandardDrawToLayer; } }
        GInteractiveDrawLayer IInteractiveItem.RepaintToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        bool IInteractiveItem.NeedDrawOverChilds { get { return this.NeedDrawOverChilds; } }
        Boolean IInteractiveItem.IsActiveAtPoint(Point relativePoint) { return this.IsActiveAtPoint(relativePoint); }
        void IInteractiveItem.AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            if (this.InteractiveStateChange != null)
                this.InteractiveStateChange(this, e);

            GInteractiveState oldState = this.InteractiveState;
            this.InteractiveState = e.TargetState;
            this.CurrentMouseRelativePoint = e.MouseRelativePoint;
            this.AfterStateChanged(e);
            GInteractiveState newState = this.InteractiveState;

            this.CallInteractiveStateChanged(new GPropertyChangeArgs<GInteractiveState>(oldState, newState, EventSourceType.InteractiveChanged));
        }
        GInteractiveState IInteractiveItem.InteractiveState { get { return this.InteractiveState; } }
        void IInteractiveItem.DragAction(GDragActionArgs e) { this.DragAction(e); }
        void IInteractiveItem.Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds) { this.Draw(e, absoluteBounds, absoluteVisibleBounds); }
        void IInteractiveItem.DrawOverChilds(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds) { this.DrawOverChilds(e, absoluteBounds, absoluteVisibleBounds); }

        UInt32 IInteractiveParent.Id { get { return this._Id; } }
        GInteractiveControl IInteractiveParent.Host { get { return this.Host; } }
        IInteractiveParent IInteractiveParent.Parent { get { return this.Parent; } set { this.Parent = value; } }
        Size IInteractiveParent.ClientSize { get { return this.ClientSize; } }
        void IInteractiveParent.Repaint() { this.Repaint(); }
        void IInteractiveParent.Repaint(GInteractiveDrawLayer repaintLayers) { this.Repaint(repaintLayers); }
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
        /// <summary>
        /// ID posledně přidělené nějakému prvku
        /// </summary>
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
        /// Ghost, kreslený v procesu Drag and Drop (buď na původní pozici, nebo na pozici přesouvané)
        /// </summary>
        Ghost = 2,
        /// <summary>
        /// Toto vykreslování se provádí v procesu Drag and Drop.
        /// Ale teprve ostatní hodnoty konkrétně určují, 
        /// </summary>
        InDragProcess = 0x010,
        /// <summary>
        /// Prvek je vykreslován do svých vlastních souřadnic <see cref="InteractiveObject.Bounds"/>.
        /// Tato hodnota je nastavena pouze tehdy, když je nastavena hodnota <see cref="InDragProcess"/>, 
        /// a aktuálně se provádí vykreslení do vrstvy <see cref="GInteractiveDrawLayer.Standard"/>.
        /// Při vykreslování mimo proces Drag and Drop se tato hodnota nenastavuje (i přesto, že se kreslí do originálních souřadnic a do standardní vrstvy).
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
        /// </summary>
        public InteractiveDragObject() : base()
        {
            this.Is.DrawDragMoveGhostStandard = true;
            this.Is.DrawDragMoveGhostInteractive = false;
            this.Is.MouseDragMove = true;
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
                    this._IsDragEnabledCurrent = this.Is.MouseDragMove;
                    if (this._IsDragEnabledCurrent)
                    {
                        this.BoundsDragOrigin = this.Bounds;
                        this.DragThisStart(e, this.Bounds);
                        this.Repaint(this.DragDrawToLayers);
                    }
                    break;
                case DragActionType.DragThisMove:
                    if (this._IsDragEnabledCurrent && e.DragToRelativeBounds.HasValue)
                    {
                        this.BoundsDragTarget = e.DragToRelativeBounds.Value;
                        this.DragThisOverPoint(e, e.DragToRelativeBounds.Value);
                        this.Repaint(this.DragDrawToLayers);
                    }
                    break;
                case DragActionType.DragThisCancel:
                    if (this._IsDragEnabledCurrent && this.BoundsDragOrigin.HasValue && this.BoundsDragOrigin.Value != this.Bounds)
                    {
                        this.SetBounds(e.DragToRelativeBounds.Value, ProcessAction.DragValueActions, EventSourceType.InteractiveChanging | EventSourceType.BoundsChange);
                        this.Repaint(this.DragDrawToLayers);
                    }
                    break;
                case DragActionType.DragThisDrop:
                    if (this._IsDragEnabledCurrent && this.BoundsDragTarget.HasValue)
                    {
                        this.DragThisDropToPoint(e, this.BoundsDragTarget.Value);
                        this.Repaint(this.DragDrawToLayers);
                    }
                    break;
                case DragActionType.DragThisEnd:
                    this.BoundsDragOrigin = null;
                    this.BoundsDragTarget = null;
                    this._IsDragEnabledCurrent = false;
                    this.Repaint();
                    this.DragThisEnd(e);
                    this.DragThisOverEndFinal(e);
                    break;
            }
        }
        /// <summary>
        /// Metoda provádí úklid vnitřních dat procesu Drag and Drop po jeho skončení.
        /// Metoda je volána v průběhu akce <see cref="DragActionType.DragThisEnd"/>, po proběhnutí metody <see cref="DragThisEnd(GDragActionArgs)"/>.
        /// </summary>
        protected void DragThisOverEndFinal(GDragActionArgs e)
        {
            this.DragDropTargetItem = null;
            this.DragDropDrawStandardOpacity = null;
            this.DragDropDrawInteractiveOpacity = null;
        }
        /// <summary>
        /// Hodnota převzatá z <see cref="InteractiveProperties.MouseDragMove"/> v době, kdy začala akce <see cref="DragActionType.DragThisStart"/>.
        /// Je platná až do akce <see cref="DragActionType.DragThisEnd"/>.
        /// Při další akci Drag bude znovu vyhodnocena.
        /// </summary>
        private bool _IsDragEnabledCurrent;
        /// <summary>
        /// Volá se na začátku procesu přesouvání, pro aktivní objekt.
        /// Bázová třída už má uloženy výchozí souřadnice objektu do <see cref="BoundsDragOrigin"/>.
        /// Bázová metoda <see cref="DragThisStart(GDragActionArgs, Rectangle)"/> nic nedělá.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected virtual void DragThisStart(GDragActionArgs e, Rectangle targetRelativeBounds)
        { }
        /// <summary>
        /// Volá se v procesu přesouvání, pro aktivní objekt.
        /// Bázová třída v době volání této metody má uložené cílové souřadnice (dle parametru targetRelativeBounds) v proměnné <see cref="BoundsDragTarget"/>.
        /// Potomek může tuto souřadnici v této metodě změnit, a upravenou ji vložit do <see cref="BoundsDragTarget"/>.
        /// Anebo může zavolat base.DragThisOverBounds(e, upravená souřadnice), bázová metoda tuto upravenou hodnotu opět uloží do <see cref="BoundsDragTarget"/>.
        /// Bázovou metodu <see cref="InteractiveDragObject.DragThisOverPoint(GDragActionArgs, Rectangle)"/> ale obecně není nutno volat.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected virtual void DragThisOverPoint(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.BoundsDragTarget = targetRelativeBounds;
        }
        /// <summary>
        /// Volá se při ukončení Drag and Drop, při akci <see cref="DragActionType.DragThisDrop"/>, pro aktivní objekt (=ten který je přesouván).
        /// Bázová metoda <see cref="InteractiveDragObject.DragThisDropToPoint(GDragActionArgs, Rectangle)"/> vepíše předané souřadnice (parametr targetRelativeBounds) 
        /// do this.Bounds pomocí metody <see cref="InteractiveObject.SetBounds(Rectangle, ProcessAction, EventSourceType)"/>.
        /// Pokud potomek chce modifikovat cílové souřadnice, stačí změnit hodnotu parametru targetRelativeBounds.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected virtual void DragThisDropToPoint(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.SetBounds(targetRelativeBounds, ProcessAction.DragValueActions, EventSourceType.InteractiveChanged | EventSourceType.BoundsChange);
        }
        /// <summary>
        /// Je voláno po skončení přetahování, ať už skončilo OK (=Drop) nebo Escape (=Cancel).
        /// Účelem je provést úklid aplikačních dat po skončení přetahování.
        /// Bázová třída InteractiveDragObject nedělá nic.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DragThisEnd(GDragActionArgs e)
        { }
        /// <summary>
        /// Souřadnice (Bounds) tohoto objektu, platné před začátkem procesu Drag and Drop.
        /// Jde o souřadnice relativní, obdobně jako <see cref="InteractiveObject.Bounds"/>.
        /// Mimo proces Drag and Drop je zde null.
        /// </summary>
        protected virtual Rectangle? BoundsDragOrigin { get; set; }
        /// <summary>
        /// Souřadnice (Bounds) tohoto objektu, kde se aktuálně nachází v procesu Drag and Drop.
        /// Jde o souřadnice relativní, obdobně jako <see cref="InteractiveObject.Bounds"/>.
        /// Mimo proces Drag and Drop je zde null.
        /// <para/>
        /// Tato hodnota se reálně propisuje do property <see cref="InteractiveObject.BoundsInteractive"/>.
        /// </summary>
        protected virtual Rectangle? BoundsDragTarget { get { return this.BoundsInteractive; } set { this.BoundsInteractive = value; } }
        /// <summary>
        /// Vrstvy, které se mají překreslovat v době procesu Drag and Drop.
        /// </summary>
        protected virtual GInteractiveDrawLayer DragDrawToLayers { get { return (GInteractiveDrawLayer.Standard | GInteractiveDrawLayer.Interactive); } }
        /// <summary>
        /// Sem si instance může uložit referenci na objekt, do kterého by měla být vložena po Dropnutí v procesu Drag and Drop.
        /// Hodnota je nastavena na null na konci akce <see cref="DragActionType.DragThisEnd"/>, po proběhnutí metody <see cref="DragThisEnd(GDragActionArgs)"/>.
        /// </summary>
        protected virtual IInteractiveItem DragDropTargetItem { get; set; }
        /// <summary>
        /// Sem si instance může uložit hodnotu Opacity, kterou bude vykreslována do vrstvy Standard v procesu Drag and Drop.
        /// Pokud je nastavena hodnota 0, pak se kreslení do Standard vrstvy neprovádí.
        /// Pokud je nastavena hodnota větší než 0, pak se provádí kreslení s nastavením grafiky na patřičnou transparentnost (s použitím ColorMatrix).
        /// Pokud je zde hodnota null, pak se kreslení provádí, ale grafika se nijak nemodifikuje (stejně jako při hodnotě 255).
        /// Hodnota je nastavena na null na konci akce <see cref="DragActionType.DragThisEnd"/>, po proběhnutí metody <see cref="DragThisEnd(GDragActionArgs)"/>.
        /// </summary>
        protected virtual int? DragDropDrawStandardOpacity { get; set; }
        /// <summary>
        /// Sem si instance může uložit hodnotu Opacity, kterou bude vykreslována do vrstvy Interactive v procesu Drag and Drop.
        /// Pokud je nastavena hodnota 0, pak se kreslení do Interactive vrstvy neprovádí.
        /// Pokud je nastavena hodnota větší než 0, pak se provádí kreslení s nastavením grafiky na patřičnou transparentnost (s použitím ColorMatrix).
        /// Pokud je zde hodnota null, pak se kreslení provádí, ale grafika se nijak nemodifikuje (stejně jako při hodnotě 255).
        /// Hodnota je nastavena na null na konci akce <see cref="DragActionType.DragThisEnd"/>, po proběhnutí metody <see cref="DragThisEnd(GDragActionArgs)"/>.
        /// </summary>
        protected virtual int? DragDropDrawInteractiveOpacity { get; set; }
        #endregion
        #region Draw
        /// <summary>
        /// Metoda řeší kreslení prvku, který může být v procesu Drag and Drop
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            Rectangle absoluteBoundsDraw;
            DrawItemMode drawMode;
            int? drawOpacity;
            if (this.PrepareDrawDataByDragData(e.DrawLayer, absoluteBounds, out absoluteBoundsDraw, out drawMode, out drawOpacity))
            {
                using (PrepareGraphicsOpacity(e, drawOpacity))
                {
                    this.Draw(e, absoluteBoundsDraw, absoluteVisibleBounds, drawMode);
                }
            }
        }
        /// <summary>
        /// Kreslení "OverChilds"
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void DrawOverChilds(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            Rectangle absoluteBoundsDraw;
            DrawItemMode drawMode;
            int? drawOpacity;
            if (this.PrepareDrawDataByDragData(e.DrawLayer, absoluteBounds, out absoluteBoundsDraw, out drawMode, out drawOpacity))
            {
                using (PrepareGraphicsOpacity(e, drawOpacity))
                {
                    this.DrawOverChilds(e, absoluteBoundsDraw, drawMode);
                }
            }
        }
        /// <summary>
        /// Tato metoda určí souřadnice, kam se má objekt vykreslit, a režim kreslení (Standard, Ghost), pro aktuální situaci objektu.
        /// Reaguje na Drag and Drop, řeší souřadnice objektu v procesu Drag and Drop, bere v potaz styl objektu DragDrawGhostOriginal / DragDrawGhostInteractive.
        /// Výsledné absolutní souřadnice pro kreslení a reřim kreslení dává do out parametrů.
        /// Vrací true = má se kreslit / false = nemá se kreslit.
        /// <para/>
        /// Bázová třída  řídí vykreslení do souřadnic výchozích (BoundsDragOrigin) a do souřadnic Dragging (BoundsDragTarget), 
        /// do vrstev Standard a Interactive.
        /// <para/>
        /// Pokud chce potomek kreslit prvek v době přetahování jinam, může tuto metodu přepsat.
        /// </summary>
        /// <param name="currentLayer">Vrstva, která se nyní kreslí</param>
        /// <param name="absoluteBoundsItem">Vstup: Absolutní souřadnice prvku, běžná (odvozená od <see cref="IInteractiveItem.Bounds"/>)</param>
        /// <param name="absoluteBoundsDraw">Výstup: Absolutní souřadnice, kam se bude prvek vykreslovat (souvisí s procesem Drag and Drop)</param>
        /// <param name="drawMode">Režim kreslení prvku v aktuální situaci</param>
        /// <param name="drawOpacity"></param>
        protected virtual bool PrepareDrawDataByDragData(GInteractiveDrawLayer currentLayer, Rectangle absoluteBoundsItem, out Rectangle absoluteBoundsDraw, out DrawItemMode drawMode, out int? drawOpacity)
        {
            bool ghostInOriginalBounds = this.Is.DrawDragMoveGhostStandard;
            bool ghostInDraggedBounds = this.Is.DrawDragMoveGhostInteractive;
            absoluteBoundsDraw = Rectangle.Empty;
            drawMode = DrawItemMode.InDragProcess;
            drawOpacity = null;
            bool runDraw = true;

            if (this.IsDragged && this.BoundsDragOrigin.HasValue)
            {   // Aktuálně PROBÍHÁ Drag and Drop:
                if (currentLayer == GInteractiveDrawLayer.Standard)
                {   // Nyní kreslíme do vrstvy Standard, tedy kreslíme do výchozích souřadnic BoundsDragOrigin:
                    absoluteBoundsDraw = BoundsInfo.GetAbsoluteBoundsInContainer(this.Parent, this.BoundsDragOrigin.Value);
                    drawMode |= DrawItemMode.OriginalBounds;
                    drawOpacity = this.DragDropDrawStandardOpacity;
                    if (ghostInOriginalBounds)
                        // Máme styl DragDrawGhostOriginal, takže na originální souřadnice (tj. do standardní vrstvy) máme vykreslit Ghost:
                        drawMode |= DrawItemMode.Ghost;
                    else if (ghostInDraggedBounds)
                        // Máme styl DragDrawGhostInteractive, takže na originální souřadnice (tj. do standardní vrstvy) máme vykreslit Standard:
                        drawMode |= DrawItemMode.Standard;
                    else
                        // Pokud není specifikován ani jeden styl (DragDrawGhostOriginal ani DragDrawGhostInteractive),
                        //  pak v procesu Drag and Drop nebude do standardní vrstvy kresleno nic (objekt se skutečně ihned odsouvá jinam).
                        //  Objekt bude kreslen jako Standard pouze do vrstvy Interactive, na souřadnice Target.
                        runDraw = false;
                }
                else if (currentLayer == GInteractiveDrawLayer.Interactive)
                {   // Nyní kreslíme do vrstvy Interactive, tedy kreslíme do cílových souřadnic BoundsDragTarget:
                    absoluteBoundsDraw = BoundsInfo.GetAbsoluteBoundsInContainer(this.Parent, this.BoundsDragTarget.Value);
                    drawMode |= DrawItemMode.DraggedBounds;
                    drawOpacity = this.DragDropDrawInteractiveOpacity;
                    if (ghostInDraggedBounds)
                        // Máme styl DragDrawGhostInteractive, takže na cílové souřadnice (tj. do interaktivní vrstvy) máme vykreslit Ghost:
                        drawMode |= DrawItemMode.Ghost;
                    else
                        // Jinak (pro styl DragDrawGhostOriginal nebo pro nezadaný styl) budeme na cílové souřadnice (tj. do interaktivní vrstvy) vykreslovat Standard:
                        drawMode |= DrawItemMode.Standard;
                }
                else
                    // Do jiných vrstev nebudeme kreslit nic:
                    runDraw = false;
            }
            else if (currentLayer != GInteractiveDrawLayer.Standard)
            {   // Prvek sám sice není předmětem Drag and Drop, ale nejspíš jeho Parent ano, protože nyní probíhá kreslení do Interaktivní vrstvy (jiná asi ne):
                absoluteBoundsDraw = this.BoundsAbsolute;
                absoluteBoundsDraw = BoundsInfo.GetAbsoluteBoundsInContainer(this.Parent, this.Bounds, currentLayer);
                drawMode = DrawItemMode.Ghost;
            }
            else
            {   // Aktuálně NEPROBÍHÁ Drag and Drop, a prvek se kreslí do standardní vrstvy => jde o dočista normální kreslení:
                absoluteBoundsDraw = absoluteBoundsItem;
                drawMode = DrawItemMode.Standard;
            }

            // Pokud je specifikována průhlednost pro aktuální vrstvu == 0 (a záporné hodnoty), pak se nic kreslit nebude:
            if (drawOpacity.HasValue && drawOpacity.Value <= 0)
                runDraw = false;

            return runDraw;
        }
        /// <summary>
        /// Metoda na základě hodnoty "drawOpacity" nastaví ColorMatrix.Opacity pro danou e.Graphics
        /// </summary>
        /// <param name="e"></param>
        /// <param name="drawOpacity"></param>
        /// <returns></returns>
        protected virtual IDisposable PrepareGraphicsOpacity(GInteractiveDrawArgs e, int? drawOpacity)
        {
            if (!drawOpacity.HasValue) return null;
            return GPainter.GraphicsUseOpacity(e.Graphics, drawOpacity.Value);
        }
        #endregion
    }
    #endregion
    #region class InteractiveValue : abstract ancestor for iteractive objects with Value and ValueRange properties
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
        /// Setting value to this property does not call any event (action = <seealso cref="Asol.Tools.WorkScheduler.Components.ProcessAction.SilentValueActions"/>)
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
        /// <param name="oldValueRange">Old value range, before change</param>
        /// <param name="newValueRange">New value range. Use this value rather than this.Value</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected virtual void SetValueRangeAfterChange(TRange oldValueRange, TRange newValueRange, ref ProcessAction actions, EventSourceType eventSource)
        { }
        /// <summary>
        /// Rozsah hodnot
        /// </summary>
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
