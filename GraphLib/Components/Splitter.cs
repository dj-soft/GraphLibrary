using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Splitter (Horizontal, Vertical)
    /// </summary>
    public class GSplitter : InteractiveObject, IInteractiveItem
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GSplitter(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GSplitter()
        {
            this._SplitterVisibleWidth = 2;
            this._SplitterActiveOverlap = 1;
            this._DragResponse = DragResponseType.InDragMove;
            this._IsResizeToLinkItems = true;
            this.Is.DragEnabled = true;
        }
        /// <summary>
        /// ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Orientation.ToString() + " " + base.ToString();
        }
        #endregion
        #region Private variables
        private Orientation _Orientation = Orientation.Horizontal;
        private int _Location;
        private int? _LocationOriginal;
        private Int32NRange __LocationLimit;
        private int _SplitterVisibleWidth;
        private int _SplitterActiveOverlap;
        private bool _IsResizeToLinkItems;
        private DragResponseType _DragResponse;
        private IInteractiveItem _LinkedItemPrev;
        private IInteractiveItem _LinkedItemNext;
        private int? _LinkedItemPrevMinSize;
        private int? _LinkedItemNextMinSize;
        private bool _SetSplitterInProgress;

        private bool _IsHorizontal { get { return (this._Orientation == System.Windows.Forms.Orientation.Horizontal); } }
        #endregion
        #region Public property and events
        /// <summary>
        /// Coordinates of this item in their Parent client area.
        /// This is relative bounds within my Parent, where this item is visible.
        /// Appropriate absolute bounds can be calculated via (extension) method IInteractiveItem.GetAbsoluteVisibleBounds().
        /// Setting a new value into this property caused calling All ProcesActions (see method SetBounds()).
        /// </summary>
        public override Rectangle Bounds
        {
            get { return base.Bounds; }
            set { this.SetSplitter(value, null, null, null, null, null, null, DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Part of Bounds for inactive direction.
        /// For Horizontal splitter = Int32Range(Bounds.Left, Bounds.Right);
        /// For Vertical splitter = Int32Range(Bounds.Top, Bounds.Bottom);
        /// Setting a new value is correct only when IsResizeToLinkItems is false. 
        /// Otherwise will be BoundsNonActive still depend on Linked items (LinkedItemPrev, LinkedItemNext) and its Bounds.
        /// </summary>
        public Int32NRange BoundsNonActive
        {
            get
            {
                Rectangle rb = base.Bounds;
                switch (this.Orientation)
                {
                    case System.Windows.Forms.Orientation.Horizontal: return new Int32NRange(rb.Left, rb.Right);
                    case System.Windows.Forms.Orientation.Vertical: return new Int32NRange(rb.Top, rb.Bottom);
                }
                return Int32NRange.Empty;
            }
            set
            {
                Rectangle rb = base.Bounds;
                if (value != null)
                {
                    int l = rb.Left;
                    int t = rb.Top;
                    int r = rb.Right;
                    int b = rb.Bottom;
                    switch (this.Orientation)
                    {
                        case System.Windows.Forms.Orientation.Horizontal:
                            if (value.HasBegin) l = value.Begin.Value;
                            if (value.HasEnd) r = value.End.Value;
                            break;
                        case System.Windows.Forms.Orientation.Vertical:
                            if (value.HasBegin) t = value.Begin.Value;
                            if (value.HasEnd) b = value.End.Value;
                            break;
                    }
                    this._Bounds = new Rectangle(l, t, (r - l), (b - t));      // Do not call any event!
                }
            }
        }
        /// <summary>
        /// Set this._Bounds = bounds (when new value is not equal to current value: bounds != this._Bounds).
        /// If actions contain CallChangedEvents, then call CallBoundsChanged() method.
        /// If actions contain CallDraw, then call CallDrawRequest() method.
        /// return true = was change, false = not change (bounds == this._Bounds).
        /// </summary>
        /// <param name="bounds">New bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        public override bool SetBounds(Rectangle bounds, ProcessAction actions, EventSourceType eventSource)
        {
            return this.SetSplitter(bounds, null, null, null, null, null, null, DragResponseType.AfterDragEnd, actions, eventSource);
        }
        /// <summary>
        /// Logical location of splitter. In coordinates of Parent object (or Host control, when Parent is null). Represent a center of Bounds on axis by Orientation.
        /// </summary>
        public int Value
        {
            get { return this._GetValue(this._Location, this._Orientation); }
            set { this.SetSplitter(null, value, null, null, null, null, null, DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Pozice splitteru - logická = ve směru jeho aktivního pohybu, v souřadnicích jeho Parenta.
        /// Reprezentuje střed vizuální pozice.
        /// Nastavení hodnoty do této property nespouští žádné návazné eventy, takže nedojde k případnému zacyklení událostí v komplexnějším scénáři.
        /// </summary>
        public int ValueSilent
        {
            get { return this._GetValue(this._Location, this._Orientation); }
            set { this.SetSplitter(null, value, null, null, null, null, null, 
                DragResponseType.AfterDragEnd, ProcessAction.SilentValueActions, EventSourceType.BoundsChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logical location of splitter. In coordinates of Parent object (or Host control, when Parent is null). Represent a center of Bounds on axis by Orientation.
        /// Drag = setting an value to this property do not reset inner bounds (InnerBoundsReset())
        /// </summary>
        protected int ValueDrag
        {
            get { return this._GetValue(this._Location, this._Orientation); }
            set { this.SetSplitter(null, value, null, null, null, null, null, DragResponseType.InDragMove, ProcessAction.DragValueActions, EventSourceType.BoundsChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Range for Location value (Min and Max value, in Begin and End properties). Null = without limits.
        /// In coordinates of Parent object (or Host control, when Parent is null) = as VisibleRelativeBounds.
        /// <para/>
        /// Default value = null (no limit);
        /// </summary>
        public Int32NRange ValueRange
        {
            get { return this.__LocationLimit; }
            set { this.SetSplitter(null, null, null, null, null, value, null, DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Orientation of splitter.
        /// </summary>
        public Orientation Orientation
        {
            get { return this._Orientation; }
            set { this.SetSplitter(null, null, value, null, null, null, null, DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Viditelná šíře splitteru: pro Vertikální splitter = Width, pro Horizontální splitter = Height.
        /// Nastavení hodnoty 0 způsobí, že splitter nebude zobrazován, ale stále může být aktivní (pokud má <see cref="SplitterActiveOverlap"/> kladné).
        /// <para/>
        /// Default value = 2;
        /// </summary>
        public int SplitterVisibleWidth
        {
            get { return (this._SplitterVisibleWidth < 0 ? 0 : this._SplitterVisibleWidth); }
            set { this.SetSplitter(null, null, null, value, null, null, null, DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Počet pixelů viditelné části splitteru, které jsou zobrazeny PŘED pixelem, který reprezentuje hodnotu <see cref="Value"/>.
        /// Obsahuje <see cref="SplitterVisibleWidth"/> / 2, nebo 0.
        /// </summary>
        public int SplitterVisibleWidthBefore { get { int w = this._SplitterVisibleWidth; return (w > 0 ? w / 2 : 0); } }
        /// <summary>
        /// Počet pixelů viditelné části splitteru, které jsou zobrazeny ZA pixelem, který reprezentuje hodnotu <see cref="Value"/>.
        /// Obsahuje <see cref="SplitterVisibleWidth"/> - <see cref="SplitterVisibleWidthBefore"/>, nebo 0.
        /// </summary>
        public int SplitterVisibleWidthAfter { get { int w = this._SplitterVisibleWidth; return (w > 0 ? w - (w / 2) : 0); } }
        /// <summary>
        /// Overhead of active zone of splitter over the Visible bounds. (Splitter is active in area bigger than VisibleBounds).
        /// Overhead value is used for booth active sides, for example ActiveOverhead = 2px on Vertical splitter cause, 
        /// that ActiveBounds.Left = (VisibleBounds.Left - 2) and ActiveBounds.Width = (VisibleBounds.Width + 4).
        /// For Visible splitter (Invisible = false) must be positive value or zero.
        /// For Invisible splitter must be positive value, otherwise splitter will be inactive!
        /// <para/>
        /// Default value = 1;
        /// </summary>
        public int SplitterActiveOverlap
        {
            get { return this._SplitterActiveOverlap; }
            set { this.SetSplitter(null, null, null, null, value, null, null, DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// true when Bounds of this Splitter in non-active dimension is calculated from linked items () = true / or is only by appliacation code = false.
        /// </summary>
        public bool IsResizeToLinkItems
        {
            get { return this._IsResizeToLinkItems; }
            set { this._IsResizeToLinkItems = value; this.SetSplitter(DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Type of response to Drag event to item ItemBefore and ItemAfter.
        /// Splitter itself can ensure appropriate changes to interactive item Prev a Next this splitter.
        /// Splitter can resize this linked items during Drag operation, or after Drag ends.
        /// </summary>
        public DragResponseType DragResponse 
        { 
            get { return this._DragResponse; }
            set { this._DragResponse = value; this.SetSplitter(DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Item before splitter (on Left or Top side), which is automatically resized during/after this spltter Location changes.
        /// </summary>
        public IInteractiveItem LinkedItemPrev
        {
            get { return this._LinkedItemPrev; }
            set { this._LinkedItemPrev = value; this.SetSplitter(DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// ItemPrev minimal size (for Horizontal layout = Width, for Vertical layout = Height). Null = default = 10px (when ItemPrev is not null).
        /// </summary>
        public int? LinkedItemPrevMinSize
        {
            get { return this._LinkedItemPrevMinSize; }
            set { this._LinkedItemPrevMinSize = value; this.SetSplitter(DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Coordinate of End for LinkedItemPrev (for Horizontal splitter = this.Bounds.Top, for Vertical splitter = this.Bounds.Left).
        /// LinkedItemPrev has its End on this position.
        /// </summary>
        public int LinkedItemPrevCoordEnd
        {
            get
            {
                int location = this.Value;
                int begin = location - this.SplitterVisibleWidth / 2;          // Begin of Splitter = End for LinkedItemPrev
                int end = begin + this.SplitterVisibleWidth;                   // End of Splitter = Begin for LinkedItemNext
                return begin;
            }
        }
        /// <summary>
        /// Item after splitter (on Right or Bottom side), which is automatically resized during/after this spltter Location changes.
        /// </summary>
        public IInteractiveItem LinkedItemNext
        {
            get { return this._LinkedItemNext; }
            set { this._LinkedItemNext = value; this.SetSplitter(DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// ItemNext minimal size (for Horizontal layout = Width, for Vertical layout = Height). Null = default = 10px (when ItemNext is not null).
        /// </summary>
        public int? LinkedItemNextMinSize
        {
            get { return this._LinkedItemNextMinSize; }
            set { this._LinkedItemNextMinSize = value; this.SetSplitter(DragResponseType.AfterDragEnd); }
        }
        /// <summary>
        /// Coordinate of Begin for LinkedItemNext (for Horizontal splitter = this.Bounds.Bottom, for Vertical splitter = this.Bounds.Right)
        /// LinkedItemNext has its Begin on this position.
        /// </summary>
        public int LinkedItemNextCoordBegin
        {
            get
            {
                int location = this.Value;
                int begin = location - this.SplitterVisibleWidth / 2;          // Begin of Splitter = End for LinkedItemPrev
                int end = begin + this.SplitterVisibleWidth;                   // End of Splitter = Begin for LinkedItemNext
                return end;
            }
        }
        /// <summary>
        /// Set this.Bounds by this.Location and this., 
        /// and resize adjoining items (ItemPrev and ItemNext) to correct position according by this splitter.
        /// </summary>
        public override void Refresh()
        {
            this.SetSplitter(DragResponseType.AfterDragEnd);
            base.Refresh();
        }
        #endregion
        #region LoadFrom : načtení klíčových dat = neaktivní souřadnice (=X pro vodorovný splitter, Y pro svislý) + aktivní hodnota (=Y pro vodorovný splitter, X pro svislý) 
        /// <summary>
        /// LoadFrom : načtení klíčových dat = neaktivní souřadnice (=X pro vodorovný splitter, Y pro svislý) + aktivní hodnota (=Y pro vodorovný splitter, X pro svislý).
        /// Vloží do sebe dodané hodnoty jedním voláním, umožní potlačit eventy změn
        /// Toto volání nemění orientaci splitteru.
        /// </summary>
        /// <param name="bounds">Souřadnice, typicky objektu který je tímto splitterem řízen, převezmou se z nich souřadnice v neaktivním směru</param>
        /// <param name="value">Hodnota splitteru</param>
        /// <param name="silent">true = nevyvolávat eventy</param>
        public void LoadFrom(Rectangle bounds, int value, bool silent)
        {
            Rectangle splitterBounds = CreateSplitetrBoundsFromAnyBounds(bounds, this.Orientation, value);
            this._LoadFrom(value, this.Orientation, this.SplitterVisibleWidth, this.SplitterActiveOverlap, splitterBounds, silent);
        }
        /// <summary>
        /// LoadFrom : načtení klíčových dat = neaktivní souřadnice (=X pro vodorovný splitter, Y pro svislý) + aktivní hodnota (=Y pro vodorovný splitter, X pro svislý).
        /// Vloží do sebe dodané hodnoty jedním voláním, umožní potlačit eventy změn.
        /// Toto volání nemění orientaci splitteru.
        /// Tato varianta si hodnotu Value odvodí z dodaných souřadnic (bounds), z jejich odpovídající strany (side).
        /// Jako side lze zadat pouze hodnoty: Top, Right, Bottom, Left. Pokud bude zadaná kombinace, nebude určena hodnota Value, a nastaví se pouze 
        /// </summary>
        /// <param name="nearBounds">Souřadnice, typicky objektu který je tímto splitterem řízen, Ze souřadnic se převezmou hodnoty v neaktivním směru, a hodnota na dané straně.</param>
        /// <param name="side">Strana souřadnic (bounds), na které je this splitter přilepený</param>
        /// <param name="silent">true = nevyvolávat eventy</param>
        /// <param name="addWidth">Při výpočtu Value z dodaných souřadnic bounds odsadit i o odpovídající část šířky Splitteru <see cref="SplitterVisibleWidth"/>, default = true</param>
        public void LoadFrom(Rectangle nearBounds, RectangleSide side, bool silent, bool addWidth = true)
        {
            int value = this.GetValueFromNearBounds(nearBounds, side, addWidth);
            Rectangle splitterBounds = CreateSplitetrBoundsFromAnyBounds(nearBounds, this.Orientation, value);
            this._LoadFrom(value, this.Orientation, this.SplitterVisibleWidth, this.SplitterActiveOverlap, splitterBounds, silent);
        }
        /// <summary>
        /// Vrátí hodnotu, která by měla být Value pro tento Splitter, určenou ze souřadnic sousedního objektu.
        /// Ze sousedního objektu najde hodnotu souřadnice pro danou stranu (Left, Top, atd).
        /// Hodnotu posune o půl-šířku Splitetru (pokud parametr addWidth je true a šířka <see cref="SplitterVisibleWidth"/> je kladná):
        /// Pro strany Left a Top vrací value - <see cref="SplitterVisibleWidthBefore"/>.
        /// Pro strany Right a Bottom vrací value + <see cref="SplitterVisibleWidthAfter"/>.
        /// </summary>
        /// <param name="nearBounds"></param>
        /// <param name="side"></param>
        /// <param name="addWidth"></param>
        /// <returns></returns>
        protected int GetValueFromNearBounds(Rectangle nearBounds, RectangleSide side, bool addWidth)
        {
            Int32? value = nearBounds.GetSide(side);
            if (!value.HasValue) return this.Value;

            if (addWidth && this.SplitterVisibleWidth > 0)
            {
                switch (side)
                {
                    case RectangleSide.Left:
                    case RectangleSide.Top:
                        return value.Value - this.SplitterVisibleWidthBefore;
                    case RectangleSide.Right:
                    case RectangleSide.Bottom:
                        return value.Value + this.SplitterVisibleWidthAfter;
                }
            }
            return value.Value;
        }
        /// <summary>
        /// Vrátí souřadnice pro LoadFrom
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static Rectangle CreateSplitetrBoundsFromAnyBounds(Rectangle bounds, Orientation orientation, Int32 value)
        {
            switch (orientation)
            {
                case Orientation.Horizontal:
                    return new Rectangle(bounds.X, value, bounds.Width, 0);
                case Orientation.Vertical:
                    return new Rectangle(value, bounds.Y, 0, bounds.Height);
            }
            return bounds;
        }
        /// <summary>
        /// Načte souřadnice
        /// </summary>
        /// <param name="value"></param>
        /// <param name="orientation"></param>
        /// <param name="splitterVisibleWidth"></param>
        /// <param name="splitterActiveOverlap"></param>
        /// <param name="bounds"></param>
        /// <param name="silent"></param>
        private void _LoadFrom(int? value, Orientation? orientation, int? splitterVisibleWidth, int? splitterActiveOverlap, Rectangle? bounds, bool silent)
        {
            if (silent)
            {
                using (this.SuppressEvents())
                {
                    this._LoadFromRun(value, orientation, splitterVisibleWidth, splitterActiveOverlap, bounds, silent);
                }
            }
            else
            {
                this._LoadFromRun(value, orientation, splitterVisibleWidth, splitterActiveOverlap, bounds, silent);
            }
        }
        private void _LoadFromRun(int? value, Orientation? orientation, int? splitterVisibleWidth, int? splitterActiveOverlap, Rectangle? bounds, bool silent)
        {
            this.SetSplitter(bounds, value, orientation, splitterVisibleWidth, splitterActiveOverlap, null, null, DragResponseType.None, ProcessAction.All, EventSourceType.ApplicationCode);
        }
        #endregion
        #region SetSplitter() and support
        /// <summary>
        /// Calculate new values for Splitter, from all current values (Re-Apply all values).
        /// Returns true, when any change occured.
        /// </summary>
        /// <param name="moveType"></param>
        /// <returns></returns>
        internal bool SetSplitter(DragResponseType moveType)
        {
            return this.SetSplitter(null, null, null, null, null, null, null, moveType, ProcessAction.All, EventSourceType.BoundsChange | EventSourceType.ApplicationCode);
        }
        /// <summary>
        /// Calculate new values for Splitter, from all current values (Re-Apply all values).
        /// Returns true, when any change occured.
        /// </summary>
        /// <param name="moveType"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        /// <returns></returns>
        internal bool SetSplitter(DragResponseType moveType, ProcessAction actions, EventSourceType eventSource)
        {
            return this.SetSplitter(null, null, null, null, null, null, null, moveType, actions, eventSource);
        }
        /// <summary>
        /// Calculate new values for Splitter, from specified values.
        /// Specified values can be null (all, all except one, or none). Method accept new values (from parameter) or current values (from this instance).
        /// Returns true, when any change occured.
        /// </summary>
        /// <param name="bounds">new bounds (null = leave curernt Bounds)</param>
        /// <param name="location"></param>
        /// <param name="orientation"></param>
        /// <param name="splitterVisibleWidth"></param>
        /// <param name="splitterActiveOverlap"></param>
        /// <param name="locationLimit"></param>
        /// <param name="moveType"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal bool SetSplitter(Rectangle? bounds, Int32? location, Orientation? orientation, Int32? splitterVisibleWidth, Int32? splitterActiveOverlap, Int32NRange locationLimit, int? locationOriginal, 
            DragResponseType moveType)
        {
            ProcessAction action = ProcessAction.All;            // Contains actions CallChangedEvents and CallChangingEvents. We will now "reset" unappropriated bit:
            if (moveType == DragResponseType.InDragMove)
                action = (ProcessAction)BitStorage.SetBitValue((int)action, (int)ProcessAction.CallChangedEvents, false);        // During "Move" do not call "CallChangedEvents"!
            else if (moveType == DragResponseType.AfterDragEnd)
                action = (ProcessAction)BitStorage.SetBitValue((int)action, (int)ProcessAction.CallChangingEvents, false);       // On End of "Move" do not call "CallChangingEvents"!

            return this.SetSplitter(bounds, location, orientation, splitterVisibleWidth, splitterActiveOverlap, locationLimit, locationOriginal,
                moveType, action, EventSourceType.BoundsChange | EventSourceType.ApplicationCode);
        }
        /// <summary>
        /// Calculate new values for Splitter, from specified values.
        /// Specified values can be null (all, all except one, or none). Method accept new values (from parameter) or current values (from this instance).
        /// Returns true, when any change occured.
        /// </summary>
        /// <param name="bounds">new bounds (null = leave curernt Bounds)</param>
        /// <param name="location"></param>
        /// <param name="orientation"></param>
        /// <param name="splitterVisibleWidth"></param>
        /// <param name="splitterActiveOverlap"></param>
        /// <param name="locationLimit"></param>
        /// <param name="moveType"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal bool SetSplitter(Rectangle? bounds, Int32? location, Orientation? orientation, Int32? splitterVisibleWidth, Int32? splitterActiveOverlap, Int32NRange locationLimit, int? locationOriginal, 
            DragResponseType moveType, ProcessAction actions, EventSourceType eventSource)
        {
            bool anyChange = false;
            bool isInProgress = this._SetSplitterInProgress;
            try
            {
                if (!isInProgress)
                {   // Suppress recursively calling SetSplitter() from any Change events!
                    this._SetSplitterInProgress = true;

                    Rectangle boundsOld = this.Bounds;
                    Rectangle? boundsNew = bounds;

                    Int32 locationOld = (locationOriginal.HasValue ? locationOriginal.Value : this.Value);
                    Int32? locationNew = location;

                    Orientation orientationOld = this.Orientation;
                    Orientation? orientationNew = orientation;

                    Int32 splitterVisibleWidthOld = this.SplitterVisibleWidth;
                    Int32? splitterVisibleWidthNew = splitterVisibleWidth;

                    Int32 splitterActiveOverlapOld = this.SplitterActiveOverlap;
                    Int32? splitterActiveOverlapNew = splitterActiveOverlap;

                    Int32NRange locationLimitOld = this.ValueRange;
                    Int32NRange locationLimitNew = locationLimit;

                    ProcessAction actionsNoDraw = RemoveActions(actions, ProcessAction.CallDraw);     // Do not call Draw action (will be called on end)

                    // Change of Limits can cause subsequent change on Bounds:
                    bool locationLimitChange = !Int32NRange.Equal(locationLimitOld, locationLimitNew);
                    if (locationLimitChange)
                    {
                        this.__LocationLimit = locationLimitNew;
                        if (IsAction(actions, ProcessAction.CallChangedEvents))
                            this.CallLocationLimitChanged(locationLimitOld, locationLimitNew, eventSource);
                    }

                    if (boundsNew.HasValue)
                    {   // New bounds: calculate new Orientation and new Location, without Alignment:
                        orientationNew = boundsNew.Value.GetOrientation();
                        Point center = boundsNew.Value.Center();
                        locationNew = (orientationNew.Value == System.Windows.Forms.Orientation.Horizontal ? center.Y : center.X);
                    }
                    if (!orientationNew.HasValue) orientationNew = orientationOld;
                    locationNew = this._GetValue(((locationNew.HasValue) ? locationNew.Value : locationOld), orientationNew.Value);
                    if (!splitterVisibleWidthNew.HasValue) splitterVisibleWidthNew = splitterVisibleWidthOld;
                    if (splitterVisibleWidthNew.Value < 0) splitterVisibleWidthNew = 0;


                    // New bounds (location are now aligned to Limit and LinkedItems; boundsNew accept all relevant values = width, LinkedItems -> inactive Bounds):
                    boundsNew = this._GetBoundsFromLocation(locationNew.Value, orientationNew.Value, boundsNew, splitterVisibleWidthNew.Value);

                    // ActiveOverhead from splitterActiveOverlap:
                    if (!splitterActiveOverlapNew.HasValue) splitterActiveOverlapNew = splitterActiveOverlapOld;
                    Padding activeOverheadNew = this._GetActiveOverhead(orientationNew.Value, splitterActiveOverlapNew.Value);

                    // What is really changed?
                    bool boundsChange = (boundsNew.Value != boundsOld);
                    bool locationChanged = (locationNew.Value != locationOld);
                    bool orientationChanged = (orientationNew.Value != orientationOld);
                    bool splitterVisibleWidthChanged = (splitterVisibleWidthNew.Value != splitterVisibleWidthOld);
                    bool splitterActiveOverlapChanged = (splitterActiveOverlapNew.Value != splitterActiveOverlapOld);

                    // Store new values, without any actions:
                    this._Bounds = boundsNew.Value;              // Silent set to variable, do not trigger any action (only invalidate BoundsActive)
                    this._Location = locationNew.Value;
                    this._Orientation = orientationNew.Value;
                    this._SplitterVisibleWidth = splitterVisibleWidthNew.Value;
                    this._SplitterActiveOverlap = splitterActiveOverlapNew.Value;
                    this.InteractivePadding = activeOverheadNew;

                    // Akce v době provádění změny = posunu:
                    if (IsAction(actions, ProcessAction.CallChangingEvents))
                    {
                        if (locationChanged)
                        {
                            locationNew = this.CallValueChanging(locationOld, locationNew.Value, eventSource);
                            locationChanged = (locationNew.Value != locationOld);
                            this._Location = locationNew.Value;
                        }
                    }

                    // Akce po dokončení změny:
                    if (IsAction(actions, ProcessAction.CallChangedEvents))
                    {
                        if (boundsChange)
                            this.CallActionsAfterBoundsChanged(boundsOld, boundsNew.Value, ref actionsNoDraw, eventSource);

                        if (locationChanged)
                        {
                            this.CallValueChanged(locationOld, locationNew.Value, eventSource);
                            locationChanged = (locationNew.Value != locationOld);
                            this._Location = locationNew.Value;
                        }

                        if (orientationChanged)
                            this.CallOrientationChanged(orientationOld, orientationNew.Value, eventSource);

                        if (splitterVisibleWidthChanged)
                            this.CallSplitterVisibleWidthChanged(splitterVisibleWidthOld, splitterVisibleWidthNew.Value, eventSource);

                        if (splitterActiveOverlapChanged)
                            this.CallSplitterActiveOverlapChanged(splitterActiveOverlapOld, splitterActiveOverlapNew.Value, eventSource);
                    }

                    // Move Linked items (Prev and Next) by moveType and this.DragResponse:
                    bool moveLinkedItems = this._SetLinkedItemsBounds(moveType);

                    // Call Draw, when need:
                    anyChange = (boundsChange || locationChanged || orientationChanged || splitterVisibleWidthChanged || moveLinkedItems);
                    if (anyChange && (IsAction(actions, ProcessAction.CallDraw)))
                        this.CallDrawRequest(eventSource);
                }
            }
            finally
            {
                this._SetSplitterInProgress = isInProgress;
            }
            return anyChange;
        }
        /// <summary>
        /// Return a location, aligned into range LocationRange and into ItemPrevMinSize ÷ ItemNextMinSize.
        /// </summary>
        /// <param name="point">Location of Splitter</param>
        /// <param name="orientation">Orientation of Splitter</param>
        /// <returns></returns>
        private int _GetValue(Point point, Orientation orientation)
        {
            int location = (orientation == System.Windows.Forms.Orientation.Horizontal ? point.Y : point.X);
            return this._GetValue(location, orientation);
        }
        /// <summary>
        /// Return a location, aligned into range LocationRange and into ItemPrevMinSize ÷ ItemNextMinSize.
        /// </summary>
        /// <param name="location">Location of Splitter</param>
        /// <param name="orientation">Orientation of Splitter</param>
        /// <returns></returns>
        private int _GetValue(int location, Orientation orientation)
        {
            int result = location;

            // Align to LocationRange:
            if (this.__LocationLimit != null && this.__LocationLimit.IsReal)
                result = this.__LocationLimit.Align(result).Value;            // When Range.IsReal, and value (ie. "result") is not null, then Range.Align() return a non-null value.

            // Align by ItemPrevMinSize and ItemNextMinSize:
            if (this.LinkedItemNext != null && this.LinkedItemNextMinSize.HasValue && this.LinkedItemNextMinSize.Value > 0)
            {
                int maxNextLocation = (this.Orientation == System.Windows.Forms.Orientation.Horizontal ? this.LinkedItemNext.Bounds.Bottom : this.LinkedItemPrev.Bounds.Right) - this.LinkedItemNextMinSize.Value;
                if (result > maxNextLocation) result = maxNextLocation;
            }
            if (this.LinkedItemPrev != null && this.LinkedItemPrevMinSize.HasValue && this.LinkedItemPrevMinSize.Value > 0)
            {   // Value of ItemPrevMinSize has greater priority, we accept it after ItemNextMinSize:
                int minPrevLocation = (this.Orientation == System.Windows.Forms.Orientation.Horizontal ? this.LinkedItemPrev.Bounds.Top : this.LinkedItemPrev.Bounds.Left) + this.LinkedItemPrevMinSize.Value;
                if (result < minPrevLocation) result = minPrevLocation;
            }

            return result;
        }
        /// <summary>
        /// From location, orientation and width create coordinates in active axis (Horizontal: Y axis, Vertical: X axis).
        /// From this.Bounds or (IsResizeToLinkItems and LinkedItemPrev + LinkedItemNext) create coordinates in inactive axis.
        /// Returns a new Bounds for this Splitter.
        /// Note, input value of location must be aligned before calliong this method.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="orientation"></param>
        /// <param name="bounds"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private Rectangle _GetBoundsFromLocation(int location, Orientation orientation, Rectangle? bounds, int width)
        {
            Rectangle boundsOld = this.Bounds;

            Rectangle? linkedSum = null;
            if (this.IsResizeToLinkItems && (this._LinkedItemPrev != null || this._LinkedItemNext != null))
                linkedSum = DrawingExtensions.SummaryRectangle(
                    (this._LinkedItemPrev != null ? (Rectangle?)this._LinkedItemPrev.Bounds : (Rectangle?)null),
                    (this._LinkedItemNext != null ? (Rectangle?)this._LinkedItemNext.Bounds : (Rectangle?)null));
            if (!linkedSum.HasValue)
                linkedSum = bounds;

            Int32NRange active = new Int32NRange(location - (width / 2), width);
            switch (orientation)
            {
                case System.Windows.Forms.Orientation.Horizontal:
                    Int32NRange inactiveX = (linkedSum.HasValue ? new Int32NRange(linkedSum.Value.X, linkedSum.Value.Width) : new Int32NRange(boundsOld.X, boundsOld.Width));
                    return new Rectangle(inactiveX.Begin.Value, active.Begin.Value, inactiveX.End.Value, active.End.Value);
                case System.Windows.Forms.Orientation.Vertical:
                    Int32NRange inactiveY = (linkedSum.HasValue ? new Int32NRange(linkedSum.Value.Y, linkedSum.Value.Height) : new Int32NRange(boundsOld.Y, boundsOld.Height));
                    return new Rectangle(active.Begin.Value, inactiveY.Begin.Value, active.End.Value, inactiveY.End.Value);
            }
            return boundsOld;
        }
        /// <summary>
        /// Returns a new ActiveOverhead (Padding) for specified orientation and splitterActiveOverlap.
        /// ActiveOverlap is added in active dimension (for Horizontal orientation: to Y axis, for Vertical orientation: to X axis).
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="orientation"></param>
        /// <param name="splitterActiveOverlap"></param>
        /// <returns></returns>
        private Padding _GetActiveOverhead(Orientation orientation, int splitterActiveOverlap)
        {
            int activeOverlap = (splitterActiveOverlap < 0 ? 0 : splitterActiveOverlap);
            switch (orientation)
            {
                case System.Windows.Forms.Orientation.Horizontal:
                    return new Padding(0, activeOverlap, 0, activeOverlap);
                case System.Windows.Forms.Orientation.Vertical:
                    return new Padding(activeOverlap, 0, activeOverlap, 0);
            }
            return Padding.Empty;
        }
        /// <summary>
        /// Set Bounds for _LinkedItemPrev and _LinkedItemNext by this.Orientation and this.Bounds.
        /// Returns true when any change occured.
        /// </summary>
        /// <param name="moveType">Type of current move during drag</param>
        private bool _SetLinkedItemsBounds(DragResponseType moveType)
        {
            if (this.DragResponse == DragResponseType.None) return false;
            if (moveType == DragResponseType.InDragMove && this.DragResponse == DragResponseType.AfterDragEnd) return false;   // current state == InDragMove, but DragResponse == AfterEnd => Linked items can not be updated now...

            Rectangle bounds = this.Bounds;
            bool isChange = false;
            switch (this.Orientation)
            {
                case System.Windows.Forms.Orientation.Horizontal:
                    if (this._LinkedItemPrev != null)
                    {
                        Rectangle oldBounds = this._LinkedItemPrev.Bounds;
                        Rectangle newBounds = new Rectangle(oldBounds.X, oldBounds.Y, oldBounds.Width, bounds.Top - oldBounds.Top);
                        if (oldBounds != newBounds)
                        {
                            this._LinkedItemPrev.Bounds = newBounds;
                            isChange |= true;
                        }
                    }
                    if (this._LinkedItemNext != null)
                    {
                        Rectangle oldBounds = this._LinkedItemNext.Bounds;
                        Rectangle newBounds = new Rectangle(oldBounds.X, bounds.Bottom, oldBounds.Width, oldBounds.Bottom - bounds.Bottom);
                        if (oldBounds != newBounds)
                        {
                            this._LinkedItemNext.Bounds = newBounds;
                            isChange |= true;
                        }
                    }
                    break;

                case System.Windows.Forms.Orientation.Vertical:
                    if (this._LinkedItemPrev != null)
                    {
                        Rectangle oldBounds = this._LinkedItemPrev.Bounds;
                        Rectangle newBounds = new Rectangle(oldBounds.X, oldBounds.Y, bounds.Left - oldBounds.Left, oldBounds.Height);
                        if (oldBounds != newBounds)
                        {
                            this._LinkedItemPrev.Bounds = newBounds;
                            isChange |= true;
                        }
                    }
                    if (this._LinkedItemNext != null)
                    {
                        Rectangle oldBounds = this._LinkedItemNext.Bounds;
                        Rectangle newBounds = new Rectangle(bounds.Right, oldBounds.Y, oldBounds.Right - bounds.Right, oldBounds.Height);
                        if (oldBounds != newBounds)
                        {
                            this._LinkedItemNext.Bounds = newBounds;
                            isChange |= true;
                        }
                    }
                    break;

            }
            if (isChange && this.HasParent)
                this.Parent.Repaint();

            return isChange;
        }
        #endregion
        #region Public events and virtual methods
        /// <summary>
        /// Zavolá metody <see cref="OnValueChanging"/> a eventhandler <see cref="ValueChanging"/>.
        /// </summary>
        protected int CallValueChanging(int oldValue, int newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<int> args = new GPropertyChangeArgs<int>(oldValue, newValue, eventSource);
            this.OnValueChanging(args);
            if (!this.IsSuppressedEvent && this.ValueChanging != null)
                this.ValueChanging(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Metoda prováděná při změně hodnoty <see cref="Value"/>, před voláním eventhandleru
        /// </summary>
        protected virtual void OnValueChanging(GPropertyChangeArgs<int> args) { }
        /// <summary>
        /// Event volaný při změně hodnoty <see cref="Value"/>
        /// </summary>
        public event GPropertyChangedHandler<int> ValueChanging;

        /// <summary>
        /// Call method OnLocationChanged() and event LocationChanged
        /// </summary>
        protected int CallValueChanged(int oldValue, int newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<int> args = new GPropertyChangeArgs<int>(oldValue, newValue, eventSource);
            this.OnValueChanged(args);
            if (!this.IsSuppressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Occured after change Location value
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<int> args) { }
        /// <summary>
        /// Event on this.Location changes
        /// </summary>
        public event GPropertyChangedHandler<int> ValueChanged;

        /// <summary>
        /// Call method OnOrientationChanged() and event OrientationChanged
        /// </summary>
        protected void CallOrientationChanged(Orientation oldValue, Orientation newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Orientation> args = new GPropertyChangeArgs<Orientation>(oldValue, newValue, eventSource);
            this.OnOrientationChanged(args);
            if (!this.IsSuppressedEvent && this.OrientationChanged != null)
                this.OrientationChanged(this, args);
        }
        /// <summary>
        /// Occured after change Orientation value
        /// </summary>
        protected virtual void OnOrientationChanged(GPropertyChangeArgs<Orientation> args) { }
        /// <summary>
        /// Event on this.Orientation changes
        /// </summary>
        public event GPropertyChangedHandler<Orientation> OrientationChanged;

        /// <summary>
        /// Call method OnLocationRangeChanged() and event LocationRangeChanged
        /// </summary>
        protected void CallLocationLimitChanged(Int32NRange oldValue, Int32NRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Int32NRange> args = new GPropertyChangeArgs<Int32NRange>(oldValue, newValue, eventSource);
            this.OnLocationLimitChanged(args);
            if (!this.IsSuppressedEvent && this.LocationLimitChanged != null)
                this.LocationLimitChanged(this, args);
        }
        /// <summary>
        /// Occured after change LocationRange value
        /// </summary>
        protected virtual void OnLocationLimitChanged(GPropertyChangeArgs<Int32NRange> args) { }
        /// <summary>
        /// Event on this.LocationRange changes
        /// </summary>
        public event GPropertyChangedHandler<Int32NRange> LocationLimitChanged;

        /// <summary>
        /// Call method OnVisibleWideChanged() and event VisibleWideChanged
        /// </summary>
        protected void CallSplitterVisibleWidthChanged(int oldValue, int newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<int> args = new GPropertyChangeArgs<int>(oldValue, newValue, eventSource);
            this.OnSplitterVisibleWidthChanged(args);
            if (!this.IsSuppressedEvent && this.SplitterVisibleWidthChanged != null)
                this.SplitterVisibleWidthChanged(this, args);
        }
        /// <summary>
        /// Occured after change Location value
        /// </summary>
        protected virtual void OnSplitterVisibleWidthChanged(GPropertyChangeArgs<int> args) { }
        /// <summary>
        /// Event on this.Location changes
        /// </summary>
        public event GPropertyChangedHandler<int> SplitterVisibleWidthChanged;

        /// <summary>
        /// Call method OnActiveOverheadChanged() and event ActiveOverheadChanged
        /// </summary>
        protected void CallSplitterActiveOverlapChanged(int oldValue, int newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<int> args = new GPropertyChangeArgs<int>(oldValue, newValue, eventSource);
            this.OnSplitterActiveOverlapChanged(args);
            if (!this.IsSuppressedEvent && this.SplitterActiveOverlapChanged != null)
                this.SplitterActiveOverlapChanged(this, args);
        }
        /// <summary>
        /// Occured after change ActiveOverhead value
        /// </summary>
        protected virtual void OnSplitterActiveOverlapChanged(GPropertyChangeArgs<int> args) { }
        /// <summary>
        /// Event on this.ActiveOverhead changes
        /// </summary>
        public event GPropertyChangedHandler<int> SplitterActiveOverlapChanged;
        #endregion
        #region Interactivity
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            if (!this.Is.Visible) return;

            bool repaintParent = false;
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseEnter:
                    e.RequiredCursorType = (_IsHorizontal ? SysCursorType.HSplit : SysCursorType.VSplit);
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;

                case GInteractiveChangeState.LeftDragMoveBegin:
                    this._LocationOriginal = this.Value;
                    e.UserDragPoint = new Point(this.Value, this.Value);
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftDragMoveStep:
                    if (e.UserDragPoint.HasValue)
                    {
                        int location = this._GetValue(e.UserDragPoint.Value, this.Orientation);
                        // e.RepaintAllItems =  ... 
                        repaintParent = this.SetSplitter(null, location, null, null, null, null, null, 
                            DragResponseType.InDragMove, ProcessAction.DragValueActions, EventSourceType.ValueChanging | EventSourceType.ApplicationCode);
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveCancel:
                    if (this._LocationOriginal.HasValue)
                    {
                        int location = this._GetValue(this._LocationOriginal.Value, this.Orientation);
                        // e.RepaintAllItems =  ...
                        repaintParent = this.SetSplitter(null, location, null, null, null, null, null, DragResponseType.AfterDragEnd);
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveDone:
                    // To call "ValueChanged" events we must accept "_LocationOriginal" as LocationOriginal:
                    // e.RepaintAllItems =  ...
                    repaintParent = this.SetSplitter(null, this.Value, null, null, null, null, this._LocationOriginal, DragResponseType.AfterDragEnd);
                    break;
                case GInteractiveChangeState.LeftDragMoveEnd:
                    this._LocationOriginal = null;
                    break;
                case GInteractiveChangeState.MouseLeave:
                    e.RequiredCursorType = SysCursorType.Default;
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
            }

            if (repaintParent)
            {
                if (this.Parent != null)
                    this.Parent.Repaint();
                else
                    e.RepaintAllItems = true;
            }
        }
        #endregion
        #region Draw
        /// <summary>
        /// Vykreslí Splitter
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            if (!this.Is.Visible || this._SplitterVisibleWidth <= 0) return;
 
            Size size = absoluteBounds.Size;
            if (size.Width <= 0 || size.Height <= 0) return;

            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
            {
                if (this.InteractiveState == GInteractiveState.LeftDrag)
                    this._Draw(e, absoluteBounds, this._GetCurrentShadowBrush);
                else if (this.InteractiveState == GInteractiveState.MouseOver)
                    this._Draw(e, absoluteBounds, this._GetCurrentMouseBrush);
                else
                    this._Draw(e, absoluteBounds, this._GetCurrentStandardBrush);
            }
            if (e.DrawLayer == GInteractiveDrawLayer.Interactive && this.InteractiveState == GInteractiveState.LeftDrag)
            {
                this._Draw(e, absoluteBounds, this._GetCurrentInteractiveBrush);
            }
        }
        private void _Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Func<Rectangle, Brush> brushCreator)
        {
            using (Brush b = brushCreator(absoluteBounds))
            {
                e.Graphics.FillRectangle(b, absoluteBounds);
            }
        }
        private Brush _GetCurrentStandardBrush(Rectangle absoluteBounds)
        {
            float angle = (this._IsHorizontal ? -90f : 180f);
            System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(absoluteBounds, Color.DimGray, Color.LightGray, angle);
            b.SetSigmaBellShape(0.4f);
            return b;
        }
        private Brush _GetCurrentMouseBrush(Rectangle absoluteBounds)
        {
            float angle = (this._IsHorizontal ? -90f : 180f) + 180f;
            System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(absoluteBounds, Color.Gray, Color.White, angle);
            b.SetSigmaBellShape(0.5f, 0.25f);
            return b;
        }
        private Brush _GetCurrentInteractiveBrush(Rectangle absoluteBounds)
        {
            float angle = (this._IsHorizontal ? -90f : 180f);
            System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(absoluteBounds, Color.Gray, Color.LightYellow, angle);
            b.SetSigmaBellShape(0.5f);
            return b;
        }
        private Brush _GetCurrentShadowBrush(Rectangle absoluteBounds)
        {
            float angle = (this._IsHorizontal ? -90f : 180f);
            System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(absoluteBounds, Color.Gray, Color.White, angle);
            b.SetSigmaBellShape(0.5f, 0.25f);
            return b;
        }
        #endregion
    }
    /// <summary>
    /// Mode for Autoresize of Splitter by adjacent items in non-active direction
    /// </summary>
    public enum SplitterResizeMode
    {
        None,
        Union,
        Intersection
    }
}
