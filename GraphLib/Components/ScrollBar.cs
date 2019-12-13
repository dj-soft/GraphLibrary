using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Scroll bar
    /// </summary>
    public class GScrollBar : InteractiveObject, IScrollBarPaintData, IInteractiveItem
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GScrollBar(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GScrollBar()
        {
            this._ValueRatioSmallChange = Skin.ScrollBar.SmallStepRatio;
            this._ValueRatioBigChange = Skin.ScrollBar.BigStepRatio;
            this.ChildItemsInit();
        }
        /// <summary>
        /// Viszualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Orientation.ToString() + " " + base.ToString();
        }
        #endregion
        #region Bounds, Orientation, Value a ValueTotal: data, volání Set metod
        #region Bounds, Orientation
        /// <summary>
        /// Oriantace scrollbaru
        /// </summary>
        public Orientation Orientation
        {
            get { return this._Orientation; }
            set
            {
                this.SetOrientation(value, ProcessAction.ChangeAll, EventSourceType.BoundsChange | EventSourceType.ApplicationCode);
            }
        } private Orientation _Orientation = Orientation.Horizontal;
        /// <summary>
        /// Výchozí šířka svislého scrollbaru (je rovna <see cref="System.Windows.Forms.SystemInformation.VerticalScrollBarWidth"/>)
        /// </summary>
        public static int DefaultSystemBarWidth { get { return SystemInformation.VerticalScrollBarWidth; } }
        /// <summary>
        /// Výchozí výška vodorovného scrollbaru (je rovna <see cref="System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight"/>)
        /// </summary>
        public static int DefaultSystemBarHeight { get { return SystemInformation.HorizontalScrollBarHeight; } }
        #endregion
        #region Value, ValueTotal
        /// <summary>
        /// Logická hodnota.
        /// Lze ji chápat jako první a poslední pixel dokumentu, který bude zobrazen jako první ve viditelné oblasti Controlu.
        /// </summary>
        public DecimalNRange Value
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.ChangeAll, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logická hodnota.
        /// Lze ji chápat jako první a poslední pixel dokumentu, který bude zobrazen jako první ve viditelné oblasti Controlu.
        /// Silent = vložení hodnoty do této property nespustí událost ValueChanged.
        /// </summary>
        protected DecimalNRange ValueSilent
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.SilentValueActions, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logická hodnota.
        /// Lze ji chápat jako první a poslední pixel dokumentu, který bude zobrazen jako první ve viditelné oblasti Controlu.
        /// Drag = vložení hodnoty do této property nevede k přepočtu vnitřních souřadnic = pozice thumbu, protože ta je právě zdrojem akce.
        /// </summary>
        protected DecimalNRange ValueDrag
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.DragValueActions, EventSourceType.ValueChanging | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logická velikost.
        /// Lze ji chápat jako celkový rozsah pixelů dokumentu, který je zobrazován ve viditelné oblasti Controlu.
        /// </summary>
        public DecimalNRange ValueTotal
        {
            get { return this._ValueTotal; }
            set { this.SetValueTotal(value, ProcessAction.ChangeAll, EventSourceType.ValueRangeChange | EventSourceType.ApplicationCode); }
        }
        private DecimalNRange _Value;
        private DecimalNRange _ValueTotal;
        private bool _ValueIsValid;
        /// <summary>
        /// Poměrná hodnota pro malý posun, tj. když je kliknuto na horní/dolní šipku.
        /// Představuje poměr z viditelné velikosti dokumentu (<see cref="Value"/>), o kolik se posune zobrazený obsah.
        /// Default = 0.10m
        /// </summary>
        public decimal? ValueRatioSmallChange
        {
            get { return this._ValueRatioSmallChange; }
            set { this._ValueRatioSmallChange = (value < 0m ? 0.001m : (value > 1m ? 1m : value)); }
        } private decimal? _ValueRatioSmallChange;
        /// <summary>
        /// Aktuální absolutní hodnota malého posunu ( = ValueRatioSmallChange * Value.Size)
        /// </summary>
        protected decimal ValueRatioSmallChangeCurrent { get { return this.GetValueStep(this._ValueRatioSmallChange, 0.10m); } }
        /// <summary>
        /// Poměrná hodnota pro velký posun, tj. když je kliknuto na horní/dolní pole.
        /// Představuje poměr z viditelné velikosti dokumentu (<see cref="Value"/>), o kolik se posune zobrazený obsah.
        /// Default = 0.90m
        /// </summary>
        public decimal? ValueRatioBigChange
        {
            get { return this._ValueRatioBigChange; }
            set { this._ValueRatioBigChange = (value < 0m ? 0.002m : (value > 1m ? 1m : value)); }
        } private decimal? _ValueRatioBigChange;
        /// <summary>
        /// Aktuální absolutní hodnota velkého posunu ( = ValueRatioBigChange * Value.Size)
        /// </summary>
        protected decimal ValueRatioBigChangeCurrent { get { return this.GetValueStep(this._ValueRatioBigChange, 0.90m); } }
        /// <summary>
        /// Vrátí hodnotu kroku pro aktuální <see cref="Value"/> a daný poměr ratio.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected decimal GetValueStep(decimal? ratio, decimal defaultValue)
        {
            decimal step = 10m;
            if (this._Value != null && this._Value.IsFilled && this._Value.Size.Value > 0m)
            {
                decimal r = (ratio.HasValue ? ratio.Value : defaultValue);
                r = (r < 0m ? 0.001m : (r > 1m ? 1m : r));
                step = r * this._Value.Size.Value;
            }
            return step;
        }
        #endregion
        #region LoadFrom : načtení sady dat jedním voláním z jednoho zdroje
        /// <summary>
        /// Do this scrollbaru naplní hodnoty do ValueTotal, Value, IsEnabled a Bounds
        /// </summary>
        /// <param name="data">Data pro Scrollbar</param>
        /// <param name="bounds">Souřadnice scrollbaru</param>
        /// <param name="silent">true = bez volání eventhandlerů</param>
        public void LoadFrom(IScrollBarData data, Rectangle? bounds, bool silent)
        {
            if (silent)
            {
                using (this.SuppressEvents())
                {
                    this._LoadFrom(data, bounds);
                }
            }
            else
            {
                this._LoadFrom(data, bounds);
            }
        }
        /// <summary>
        /// Do this scrollbaru naplní hodnoty do ValueTotal, Value, IsEnabled a Bounds
        /// </summary>
        /// <param name="data"></param>
        /// <param name="bounds"></param>
        private void _LoadFrom(IScrollBarData data, Rectangle? bounds)
        {
            if (bounds.HasValue)
                this.Bounds = bounds.Value;

            if (data != null)
            {
                int dataSize = data.DataSize;
                int dataBegin = data.DataBegin;
                int visualSize = data.VisualSize;

                int addSpace = data.DataSizeAddSpace;
                if (addSpace < 0)
                {   // Tady se realizuje definice: "záporné číslo v DataSizeAddSpace značí počet pixelů v datové oblasti, kde budou zobrazena poslední datové pixely"
                    addSpace = visualSize + addSpace;      // Pokud addSpace = -50 a visualSize = 200, značí to že chci na poslední stránce vidět 50px dat nahoře a pod tím 150px prázdného prostoru
                    if (addSpace < 10)                     // Pokud to ale přeženu, zobrazím alespoň 10px dat
                        addSpace = 10;
                }
                if (addSpace < 0) addSpace = 0;            // Jistota
                if (addSpace > (visualSize - 10)) addSpace = visualSize - 10;
                dataSize += addSpace;

                this.ValueTotal = new DecimalNRange(0, dataSize);
                this.Value = new DecimalNRange(dataBegin, dataBegin + visualSize);
                this.Is.Enabled = (dataSize > visualSize);
            }
        }
        #endregion
        #region Set*() methods, Detect*() methods
        /// <summary>
        /// Provede se po změně this.Bounds, bez dalších podmínek
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsAfterChange(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {   // Změna Bounds nikdy nezmění Value. Vždy provede detekci orientace.
            this.DetectOrientation(LeaveOnlyActions(actions, ProcessAction.CallChangedEvents), eventSource);
        }
        /// <summary>
        /// Je voláno po změně Bounds, z metody SetBound(), pokud je specifikováno PrepareInnerItems.
        /// Přepočte souřadnice SubItems.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.ChildItemsCalculate(true);
        }
        /// <summary>
        /// Vloží danou orientaci, zavolá ChildItemsCalculate()...
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        internal void SetOrientation(Orientation orientation, ProcessAction actions, EventSourceType eventSource)
        {
            Orientation oldOrientation = this._Orientation;
            Orientation newOrientation = orientation;
            if (oldOrientation == newOrientation) return;  // No change = no reactions.

            this._Orientation = newOrientation;
            this.ChildItemsCalculate(true);

            if (IsAction(actions, ProcessAction.RecalcInnerData))
                this.ChildItemsCalculate(true);
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallOrientationChanged(oldOrientation, newOrientation, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Přizpůsobí this.OrientationCurrent podle this.OrientationUser a this.VisibleRelativeBounds
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        internal void DetectOrientation(ProcessAction actions, EventSourceType eventSource)
        {
            Orientation oldOrientation = this._Orientation;
            Orientation newOrientation = this.GetValidOrientation();
            if (newOrientation == oldOrientation) return;

            this._Orientation = newOrientation;

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallOrientationChanged(oldOrientation, newOrientation, eventSource);

            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Vrátí vhodnou orientaci pro aktuální Bounds.
        /// </summary>
        /// <returns></returns>
        internal Orientation GetValidOrientation()
        {
            Size size = this.Bounds.Size;
            return (size.Width >= size.Height ? System.Windows.Forms.Orientation.Horizontal : System.Windows.Forms.Orientation.Vertical);       // Autodetect Orientation
        }
        /// <summary>
        /// Uloží danou hodnotu do this._Value.
        /// Provede požadované akce (ValueAlign, ScrollDataValidate, InnerBoundsReset, OnValueChanged).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected void SetValue(DecimalNRange value, ProcessAction actions, EventSourceType eventSource)
        {
            if (value == null) return;
            DecimalNRange oldValue = this._Value;
            DecimalNRange newValue = value;
            if (IsAction(actions, ProcessAction.RecalcValue))
                newValue = this.ValueAlign(newValue);

            if (oldValue != null && newValue == oldValue) return;    // No change = no reactions.

            this._Value = newValue;

            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.ChildItemsReset();
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueChanged(oldValue, newValue, eventSource);
            if (IsAction(actions, ProcessAction.CallChangingEvents))
                this.CallValueChanging(oldValue, newValue, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Uloží danou hodnotu do this._ValueTotal.
        /// Provede požadované akce (ValueAlign, ScrollDataValidate, InnerBoundsReset, OnValueChanged).
        /// </summary>
        /// <param name="valueTotal"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected void SetValueTotal(DecimalNRange valueTotal, ProcessAction actions, EventSourceType eventSource)
        {
            if (valueTotal == null) return;
            DecimalNRange oldValueTotal = this._ValueTotal;
            DecimalNRange newValueTotal = valueTotal;
            if (oldValueTotal != null && newValueTotal == oldValueTotal) return;    // No change = no reactions.

            this._ValueTotal = newValueTotal;

            if (IsAction(actions, ProcessAction.RecalcValue))
                this.SetValue(this._Value, LeaveOnlyActions(actions, ProcessAction.RecalcValue, ProcessAction.PrepareInnerItems, ProcessAction.CallChangedEvents), eventSource);
            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.ChildItemsReset();
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueTotalChanged(oldValueTotal, newValueTotal, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Zarovná danou hodnotu do rozmezí  <see cref="ValueTotal"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected DecimalNRange ValueAlign(DecimalNRange value)
        {
            DecimalNRange valueTotal = this.ValueTotal;
            if (value == null || valueTotal == null) return value;
            if (!value.IsFilled || !valueTotal.IsFilled) return value;
            if (value.Size.Value <= 0m) return value;
            if (valueTotal.Size.Value <= 0m) return value;

            decimal size = value.Size.Value;              // Align must preserve (size) of value!
            decimal begin = value.Begin.Value;
            if (begin < valueTotal.Begin.Value)
                begin = valueTotal.Begin.Value;
            if (begin + size > valueTotal.End.Value)
            {
                begin = valueTotal.End.Value - size;
                if (begin < valueTotal.Begin.Value)
                {   // Only on case when value.Size is greater than valueTotal.Size is allowed to change Begin and Size by valueTotal:
                    begin = valueTotal.Begin.Value;
                    size = valueTotal.Size.Value;
                }
            }

            return DecimalNRange.CreateFromBeginSize(begin, size);
        }
        #endregion
        #region DoScroll()
        /// <summary>
        /// Posune aktuální obsah o danou hodnotu.
        /// </summary>
        /// <param name="change"></param>
        /// <param name="modifierKeys"></param>
        public void DoScrollBy(GInteractiveChangeState change, Keys modifierKeys = Keys.None)
        {
            bool isBigStep = (modifierKeys == Keys.Shift);
            decimal relativeChange = (isBigStep ? this.ValueRatioBigChangeCurrent : this.ValueRatioSmallChangeCurrent);
            if (change == GInteractiveChangeState.WheelUp) relativeChange = -relativeChange;
            this.DoScrollBy(relativeChange);
        }
        /// <summary>
        /// Posune aktuální obsah o danou hodnotu.
        /// </summary>
        /// <param name="relativeChange"></param>
        public void DoScrollBy(decimal relativeChange)
        {
            decimal begin = this._Value.Begin.Value + relativeChange;
            DecimalNRange value = DecimalNRange.CreateFromBeginSize(begin, this._Value.Size.Value);
            this.SetValue(value, ProcessAction.ChangeAll, EventSourceType.InteractiveChanged);

            this.Repaint();
        }
        #endregion
        #region Raise events (ValueChanged, ValueRangeChanged, ScaleChanged, ScaleRangeChanged, AreaChanged, DrawRequest)
        /// <summary>
        /// Vyvolá metodu OnValueChanging() a event ValueChanging
        /// </summary>
        protected void CallValueChanging(DecimalNRange oldValue, DecimalNRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<DecimalNRange> args = new GPropertyChangeArgs<DecimalNRange>(oldValue, newValue, eventSource);
            this.OnValueChanging(args);
            if (!this.IsSuppressedEvent && this.ValueChanging != null)
                this.ValueChanging(this, args);
        }
        /// <summary>
        /// Háček volaný v průběhu interaktivní změny hodnoty <see cref="Value"/>
        /// </summary>
        protected virtual void OnValueChanging(GPropertyChangeArgs<DecimalNRange> args) { }
        /// <summary>
        /// Event volaný v průběhu interaktivní změny hodnoty <see cref="Value"/>
        /// </summary>
        public event GPropertyChangedHandler<DecimalNRange> ValueChanging;

        /// <summary>
        /// Vyvolá metodu OnValueChanged() a event ValueChanged
        /// </summary>
        protected void CallValueChanged(DecimalNRange oldValue, DecimalNRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<DecimalNRange> args = new GPropertyChangeArgs<DecimalNRange>(oldValue, newValue, eventSource);
            this.OnValueChanged(args);
            if (!this.IsSuppressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
        }
        /// <summary>
        /// Háček volaný po skončení změny hodnoty <see cref="Value"/>
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<DecimalNRange> args) { }
        /// <summary>
        /// Event volaný po skončení změny hodnoty <see cref="Value"/>
        /// </summary>
        public event GPropertyChangedHandler<DecimalNRange> ValueChanged;

        /// <summary>
        /// Vyvolá metodu OnValueTotalChanged() a event ValueTotalChanged
        /// </summary>
        protected void CallValueTotalChanged(DecimalNRange oldValue, DecimalNRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<DecimalNRange> args = new GPropertyChangeArgs<DecimalNRange>(oldValue, newValue, eventSource);
            this.OnValueTotalChanged(args);
            if (!this.IsSuppressedEvent && this.ValueTotalChanged != null)
                this.ValueTotalChanged(this, args);
        }
        /// <summary>
        /// Háček volaný po skončení změny hodnoty <see cref="ValueTotal"/>
        /// </summary>
        protected virtual void OnValueTotalChanged(GPropertyChangeArgs<DecimalNRange> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChangedHandler<DecimalNRange> ValueTotalChanged;

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
        /// Occured after change Scale value
        /// </summary>
        protected virtual void OnOrientationChanged(GPropertyChangeArgs<Orientation> args) { }
        /// <summary>
        /// Event on this.Scale changes
        /// </summary>
        public event GPropertyChangedHandler<Orientation> OrientationChanged;

        /// <summary>
        /// Call method OnUserDraw() and event UserDraw
        /// </summary>
        protected void CallUserDraw(GUserDrawArgs e)
        {
            this.OnUserDraw(e);
            if (!this.IsSuppressedEvent && this.UserDraw != null)
                this.UserDraw(this, e);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnUserDraw(GUserDrawArgs e) { }
        /// <summary>
        /// Event on this.DrawRequest occured
        /// </summary>
        public event GUserDrawHandler UserDraw;

        #endregion
        #region Convert Value to/from Pixel
        /// <summary>
        /// Return SizeRange in pixels from value (booth in absolute coordinates)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="roundToInt"></param>
        /// <returns></returns>
        private DecimalNRange _GetPixelFromValue(DecimalNRange value, bool roundToInt)
        {
            decimal begin = _GetPixelFromValue(value.Begin.Value, roundToInt);
            decimal size = _GetPixelDistanceFromValue(value.Size.Value, roundToInt);
            return DecimalNRange.CreateFromBeginSize(begin, size);
        }
        /// <summary>
        /// Return SizeRange in pixels from value (booth in absolute coordinates)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private DecimalNRange _GetValueFromPixel(DecimalNRange value)
        {
            decimal begin = _GetValueFromPixel(value.Begin.Value);
            decimal size = _GetValueDistanceFromPixel(value.Size.Value);
            return DecimalNRange.CreateFromBeginSize(begin, size);
        }
        /// <summary>
        /// Return absolute pixel for specified absolute value.
        /// Result can be rounded or unrounded, ba parameter roundToInt.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="roundToInt"></param>
        /// <returns></returns>
        private decimal _GetPixelFromValue(decimal value, bool roundToInt)
        {
            if (!this._IsCalcReady) return 0;
            decimal pixel = (value - this._ValueBegin.Value) / this.ChildDataScale.Value + this._PixelBegin.Value;
            return (roundToInt ? Math.Round(pixel, 0) : pixel);
        }
        /// <summary>
        /// Return absolute value for specified absolute pixel.
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        private decimal _GetValueFromPixel(decimal pixel)
        {
            if (!this._IsCalcReady) return 0m;
            decimal value = (pixel - this._PixelBegin.Value) * this.ChildDataScale.Value + this._ValueBegin.Value;
            return value;
        }
        /// <summary>
        /// Return pixel distance for specified value distance.
        /// Result can be rounded or unrounded, ba parameter roundToInt.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="roundToInt"></param>
        /// <returns></returns>
        private decimal _GetPixelDistanceFromValue(decimal value, bool roundToInt)
        {
            if (!this._IsCalcReady) return 0m;
            decimal distance = value / this.ChildDataScale.Value;
            return (roundToInt ? Math.Round(distance, 0) : distance);
        }
        /// <summary>
        /// Return value distance for specified pixel distance.
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        private decimal _GetValueDistanceFromPixel(decimal pixel)
        {
            if (!this._IsCalcReady) return 0m;
            decimal distance = pixel * this.ChildDataScale.Value;
            return distance;
        }
        /// <summary>
        /// true when calculator is ready (is filled _ValueTotal, _PixelTotal and _Scale)
        /// </summary>
        private bool _IsCalcReady
        {
            get
            {
                return (this._ValueTotal != null && this._ValueTotal.IsFilled &&
                        this.ChildDataTotal != null && this.ChildDataTotal.IsFilled &&
                        this.ChildDataScale.HasValue && this.ChildDataScale.Value > 0m);
            }
        }
        private decimal? _ValueBegin { get { return this._ValueTotal.Begin; } }
        private decimal? _PixelBegin { get { return this.ChildDataTrack.Begin; } }
        /// <summary>
        /// Scale = value / pixel (numbers of mm to 1 pixel)
        /// </summary>
        private decimal? ChildDataScale;
        #endregion
        #endregion
        #region Výpočet souřadnic jednotlivých prvků ScrollBaru; ChildPixels data calculator
        /// <summary>
        /// Zajistí platnost souřadnic všech SubItemů
        /// </summary>
        /// <returns></returns>
        protected bool ChildItemsCalculate(bool force)
        {
            if (this.IsChildItemsValid && !force) return true;

            switch (this._Orientation)
            {
                case System.Windows.Forms.Orientation.Horizontal:
                    this.ChildItemsCalculateH();
                    break;
                case System.Windows.Forms.Orientation.Vertical:
                    this.ChildItemsCalculateV();
                    break;
            }
            this.Items = null;
            return this.IsChildItemsValid;
        }
        /// <summary>
        /// Vypočte souřadnice všech SubItemů pro orientaci Horizontal
        /// </summary>
        protected void ChildItemsCalculateH()
        {
            Rectangle r = base.Bounds;
            int buttonLength;
            if (!this.ChildPixelsCalculate(0, r.Width, r.Height, out buttonLength))
                return;

            int begin = 0;
            int size = r.Height;

            DecimalNRange dataTotal = this.ChildDataTotal;
            this.ChildItemAllArea.Bounds = new Rectangle((int)dataTotal.Begin.Value, begin, (int)dataTotal.Size.Value, size);
            this.ChildItemDataArea.Bounds = new Rectangle((int)dataTotal.Begin.Value, begin, (int)dataTotal.Size.Value, size);

            if (buttonLength > 0)
            {
                this.ChildItemMinArrow.Bounds = new Rectangle(0, begin, buttonLength, size);
                this.ChildItemMaxArrow.Bounds = new Rectangle(r.Width - buttonLength, begin, buttonLength, size);
            }

            if (this._ValueIsValid)
            {
                DecimalNRange dataThumb = this.ChildDataThumb;
                this.ChildItemMinArea.Bounds = new Rectangle((int)dataTotal.Begin.Value, begin, (int)(dataThumb.Begin.Value - dataTotal.Begin.Value), size);
                this.ChildItemMaxArea.Bounds = new Rectangle((int)dataThumb.End.Value, begin, (int)(dataTotal.End.Value - dataThumb.End.Value), size);
                this.ChildItemThumb.Bounds = new Rectangle((int)dataThumb.Begin.Value, begin, (int)dataThumb.Size.Value, size);
            }
            this.ChildItemMinArrow.ImageType = LinearShapeType.LeftArrow;
            this.ChildItemThumb.ImageType = LinearShapeType.HorizontalLines;
            this.ChildItemMaxArrow.ImageType = LinearShapeType.RightArrow;
            this.ChildItemThumb.DragCursorType = SysCursorType.NoMoveHoriz;

            this.IsChildItemsValid = true;
        }
        /// <summary>
        /// Vypočte souřadnice všech SubItemů pro orientaci Vertical
        /// </summary>
        protected void ChildItemsCalculateV()
        {
            Rectangle r = base.Bounds;
            int buttonLength;
            if (!this.ChildPixelsCalculate(0, r.Height, r.Width, out buttonLength))
                return;

            int begin = 0;
            int size = r.Width;

            DecimalNRange dataTotal = this.ChildDataTotal;
            this.ChildItemAllArea.Bounds = new Rectangle(begin, (int)dataTotal.Begin.Value, size, (int)dataTotal.Size.Value);
            this.ChildItemDataArea.Bounds = new Rectangle(begin, (int)dataTotal.Begin.Value, size, (int)dataTotal.Size.Value);

            if (buttonLength > 0)
            {
                this.ChildItemMinArrow.Bounds = new Rectangle(begin, 0, size, buttonLength);
                this.ChildItemMaxArrow.Bounds = new Rectangle(begin, r.Height - buttonLength, size, buttonLength);
            }

            if (this._ValueIsValid)
            {
                DecimalNRange dataThumb = this.ChildDataThumb;
                this.ChildItemMinArea.Bounds = new Rectangle(begin, (int)dataTotal.Begin.Value, size, (int)(this.ChildDataValue.Center.Value - dataTotal.Begin.Value));
                this.ChildItemMaxArea.Bounds = new Rectangle(begin, (int)this.ChildDataValue.Center.Value, size, (int)(dataTotal.End.Value - this.ChildDataValue.Center.Value));
                this.ChildItemThumb.Bounds = new Rectangle(begin, (int)dataThumb.Begin.Value, size, (int)dataThumb.Size.Value);
            }
            this.ChildItemMinArrow.ImageType = LinearShapeType.UpArrow;
            this.ChildItemThumb.ImageType = LinearShapeType.VerticalLines;
            this.ChildItemMaxArrow.ImageType = LinearShapeType.DownArrow;
            this.ChildItemThumb.DragCursorType = SysCursorType.NoMoveVert;

            this.IsChildItemsValid = true;
        }
        /// <summary>
        /// Invaliduje relativní souřadnice v ChildItems.
        /// </summary>
        protected void ChildItemsReset()
        {
            this.IsChildItemsValid = false;
        }
        /// <summary>
        /// true pokud ChildItems obsahuje platné relativní souřadnice (podle hodnot Value, ValueTotal a Bounds)
        /// </summary>
        protected bool IsChildItemsValid { get; private set; }
        /// <summary>
        /// Vypočítá data pro aktuální hodnoty <see cref="ValueTotal"/>, <see cref="Value"/>, a danou velikost prvku (parametry length a width).
        /// Vypočtená data ukládá do <see cref="ChildDataTotal"/>, <see cref="ChildDataTrack"/>, <see cref="ChildDataValue"/> a <see cref="ChildDataThumb"/>.
        /// </summary>
        /// <param name="begin">Pixel, kde aktuálně začíná ScrollBar (podle orientace: pro Horizontal = Left, Vertical = Top)</param>
        /// <param name="length">Délka ScrollBaru v aktivním směru (Horizontal = Width, Vertical = Height)</param>
        /// <param name="width">Šířka ScrollBaru v neaktivním směru (Horizontal = Height, Vertical = Width)</param>
        /// <param name="buttonLength">Out: velikost buttonů Min a Max v aktivním směru (Horizontal = Width, Vertical = Height)</param>
        /// <returns>true = OK</returns>
        private bool ChildPixelsCalculate(int begin, int length, int width, out int buttonLength)
        {
            this.ChildPixelsReset();
            buttonLength = 0;
            DecimalNRange value = this._Value;
            DecimalNRange valueTotal = this._ValueTotal;
            if (valueTotal == null || !valueTotal.IsFilled) return false;
            this._ValueIsValid = (value != null && value.IsFilled && value.Size > 0m);
            if (length < 10) return false;
            buttonLength = ChildPixelsCalculateMinMaxLength(length, width);                        // Délka buttonu Min a Max
            this.ChildDataTotal = DecimalNRange.CreateFromBeginSize(begin + buttonLength, length - (2 * buttonLength));
            this.ChildDataTrack = this.ChildDataTotal.Clone;
            this.ChildDataScale = valueTotal.Size.Value / this.ChildDataTrack.Size.Value;
            if (this._ValueIsValid)
            {   // Hodnota Value je korektní (její Size je větší než 0):
                this.ChildDataValue = this._GetPixelFromValue(value, false);
                // Minimální délka thumbu (v aktivním směru, v pixelech):
                decimal minThumbSize = (this.ChildDataTotal.Size.Value < 36m ? this.ChildDataTotal.Size.Value / 3m : 12m);
                if (this.ChildDataValue.Size.Value >= minThumbSize)
                {   // Vypočtená velikost Thumbu je vyhovující => nemusíme řešit Offset ani zvětšovat Thumb (_PixelThumb), ani zmenšovat _PixelTrack:
                    this.ChildDataThumbOffset = 0m;
                    this.ChildDataThumb = this.ChildDataValue.Clone;
                }
                else
                {   // Vypočtená velikost Thumbu je příliš malá, musíme Thumb zvětšit na minimální velikost, a vyřešit ty vzniklé disproporce:
                    this.ChildDataThumbOffset = (minThumbSize - this.ChildDataValue.Size.Value) / 2m;
                    this.ChildDataTrack = new DecimalNRange(this.ChildDataTotal.Begin.Value + this.ChildDataThumbOffset, this.ChildDataTotal.End.Value - this.ChildDataThumbOffset);
                    this.ChildDataScale = valueTotal.Size.Value / this.ChildDataTrack.Size.Value;
                    this.ChildDataValue = this._GetPixelFromValue(value, false);
                    this.ChildDataThumb = new DecimalNRange(this.ChildDataValue.Begin.Value - this.ChildDataThumbOffset, this.ChildDataValue.End.Value + this.ChildDataThumbOffset);
                }
                this.ChildItemMinArrow.Is.Enabled = true;
                this.ChildItemMaxArrow.Is.Enabled = true;
            }
            else
            {   // Hodnota Value je nesprávná (její Size je 0 nebo záporná):
                //  V tomto případě se nebude vykreslovat Thumb a ani datové oblasti
                this.ChildDataValue = new DecimalNRange(0m, 0m);
                this.ChildDataThumbOffset = 0m;
                this.ChildDataThumb = this.ChildDataValue.Clone;
                this.ChildItemMinArrow.Is.Enabled = false;
                this.ChildItemMaxArrow.Is.Enabled = false;
            }
            // Zaokrouhlit ChildDataThumb na Int32:
            decimal thumbBegin = this.ChildDataThumb.Begin.Value;
            decimal roundBegin = Math.Round(this.ChildDataThumb.Begin.Value, 0);
            decimal roundSize = Math.Round(this.ChildDataThumb.Size.Value, 0);
            this.ChildDataThumb = DecimalNRange.CreateFromBeginSize(roundBegin, roundSize);

            // Hezký úmysl, ale musely by se předělat metody CalculateBoundsInteractiveDragH() a CalculateBoundsInteractiveDragV() tak, aby ... co vlastně? No, jednoduše to nefungovalo.
            // this._PixelThumbOffset += (thumbBegin - roundBegin);    

            return true;
        }
        /// <summary>
        /// Metoda vrátí délku buttonu Min a Max v aktivním směru. Může být 0.
        /// </summary>
        /// <param name="length">Délka ScrollBaru v aktivním směru (Horizontal = Width, Vertical = Height)</param>
        /// <param name="width">Šířka ScrollBaru v neaktivním směru (Horizontal = Height, Vertical = Width)</param>
        /// <returns></returns>
        protected int ChildPixelsCalculateMinMaxLength(int length, int width)
        {
            if (width < 12) return 0;
            if (width > 32 && length > 128) return 32;
            int maxLength = (int)(length / 5);
            if (width > maxLength) return maxLength;
            return width;
        }
        /// <summary>
        /// Nuluje veškeré výsledky výpočtů souřadnic
        /// </summary>
        private void ChildPixelsReset()
        {
            this.ChildDataTotal = null;
            this.ChildDataTrack = null;
            this.ChildDataValue = null;
            this.ChildDataThumb = null;
            this.ChildDataThumbOffset = 0m;
            this.ChildDataScale = 0m;

            this.ChildItemMinArrow.Bounds = Rectangle.Empty;
            this.ChildItemMinArea.Bounds = Rectangle.Empty;
            this.ChildItemThumb.Bounds = Rectangle.Empty;
            this.ChildItemMaxArea.Bounds = Rectangle.Empty;
            this.ChildItemMaxArrow.Bounds = Rectangle.Empty;

            this.ChildItemMinArrow.Is.Enabled = false;
            this.ChildItemMaxArrow.Is.Enabled = false;
        }
        /// <summary>
        /// Rozmezí pixelů celé oblasti DataArea, nad touto oblastí se může pohybovat Thumb.
        /// Oblast neobsahuje buttony Min a Max. Jde o rozmezí pixelů v relativních souřadnicích, v ose ve které je Scrollbar aktivní.
        /// Není určeno pro kalkulace, pouze pro interaktivitu a kreslení.
        /// Obsahuje Integer čísla bez desetinné části.
        /// </summary>
        private DecimalNRange ChildDataTotal;
        /// <summary>
        /// Range of pixel of theoretical thumb position.
        /// Theoretical value, for calculations.
        /// Real thumb can be bigger.
        /// Decimal numbers (with fractions).
        /// </summary>
        private DecimalNRange ChildDataValue;
        /// <summary>
        /// Range of pixel of real thumb position.
        /// When theoretical value in this._Pixel has .Size small than 8 pixel, then _PixelThumb has size 8 pixel, and is "shifted" = centered around this._Pixel.
        /// Not for calculations, only for InnerArea (interactivity and paint).
        /// Integer numbers (as decimal, but without fractions).
        /// </summary>
        private DecimalNRange ChildDataThumb;
        /// <summary>
        /// Area for scrolling of theoretical thumb (this._Pixel).
        /// When theoretical value in this._Pixel has .Size small than 8 pixel, and _PixelThumb is inflate, then usable area for track is smaller (about this inflate).
        /// Theoretical value, for calculations.
        /// Decimal numbers (with fractions).
        /// </summary>
        private DecimalNRange ChildDataTrack;
        /// <summary>
        /// Offset = (this._Pixel.Begin - this._PixelThumb.Begin).
        /// For interactive change (Thumb.Dragging), where are from physical coordinates of thumb (_PixelThumb) calculated theoretical position (_Pixel).
        /// Calculations: _Pixel.Begin = (_PixelThumb.Begin + Offset); _PixelThumb.Begin = (_Pixel.Begin + Offset).
        /// It also includes the difference of rounding (in the same sense).
        /// </summary>
        private decimal ChildDataThumbOffset;
        #endregion
        #region Child items, Init, Filter and Cache, Type-properties
        /// <summary>
        /// Vrací pole Child prvků
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetSubItems(); } }
        /// <summary>
        /// Vytvoří všechny Child items
        /// </summary>
        protected void ChildItemsInit()
        {
            this.ChildItemDict = new Dictionary<ChildItemType, ChildItem>();
            // Na pořadí záleží, v něm se budou prvky vykreslovat:
            this.ChildItemDict.Add(ChildItemType.AllArea, new ChildItem(this, ChildItemType.AllArea, false));
            this.ChildItemDict.Add(ChildItemType.Data, new ChildItem(this, ChildItemType.Data, false));
            this.ChildItemDict.Add(ChildItemType.MinArea, new ChildItem(this, ChildItemType.MinArea, true));
            this.ChildItemDict.Add(ChildItemType.MaxArea, new ChildItem(this, ChildItemType.MaxArea, true));
            this.ChildItemDict.Add(ChildItemType.MinArrow, new ChildItem(this, ChildItemType.MinArrow, true));
            this.ChildItemDict.Add(ChildItemType.MaxArrow, new ChildItem(this, ChildItemType.MaxArrow, true));
            this.ChildItemDict.Add(ChildItemType.Thumb, new ChildItem(this, ChildItemType.Thumb, true));
            this.ChildItemDict.Add(ChildItemType.CenterThumb, new ChildItem(this, ChildItemType.CenterThumb, false));
        }
        /// <summary>
        /// Child Item, který představuje celou plochu ScrollBaru
        /// </summary>
        protected ChildItem ChildItemAllArea { get { return this.ChildItemDict[ChildItemType.AllArea]; } }
        /// <summary>
        /// Child Item, který představuje datovou plochu ScrollBaru (bez MinArrow a MaxArrow)
        /// </summary>
        protected ChildItem ChildItemDataArea { get { return this.ChildItemDict[ChildItemType.Data]; } }
        /// <summary>
        /// Child Item, který představuje MinArrow (malý button Down/Left)
        /// </summary>
        protected ChildItem ChildItemMinArrow { get { return this.ChildItemDict[ChildItemType.MinArrow]; } }
        /// <summary>
        /// Child Item, který představuje plochu pro "PageDown" / "PageLeft"
        /// </summary>
        protected ChildItem ChildItemMinArea { get { return this.ChildItemDict[ChildItemType.MinArea]; } }
        /// <summary>
        /// Child Item, který představuje tlačítko pro manuální přesun na Scrollbaru, jeho velikost je úměrná aktuální hodnotě <see cref="Value"/>.
        /// </summary>
        protected ChildItem ChildItemThumb { get { return this.ChildItemDict[ChildItemType.Thumb]; } }
        /// <summary>
        /// Child Item, který představuje plochu "PageUp" / "PageRight"
        /// </summary>
        protected ChildItem ChildItemMaxArea { get { return this.ChildItemDict[ChildItemType.MaxArea]; } }
        /// <summary>
        /// Child Item, který představuje MaxArrow (malý button Up/Right)
        /// </summary>
        protected ChildItem ChildItemMaxArrow { get { return this.ChildItemDict[ChildItemType.MaxArrow]; } }
        /// <summary>
        /// Child Item, který představuje střed thumb na Scrollbar (plocha pro image nebo jinou grafiku), má konstantní velikost
        /// </summary>
        protected ChildItem ChildItemCenterThumb { get { return this.ChildItemDict[ChildItemType.CenterThumb]; } }
        /// <summary>
        /// Vrací pole všech aktivních prvků SubItem
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<InteractiveObject> GetSubItems()
        {
            if (this.Items == null)
            {
                this.Items = this.ChildItemDict.Values.Where(i => i.Is.Enabled).ToArray();
            }
            return this.Items;
        }
        /// <summary>
        /// Index of all Child items
        /// </summary>
        protected Dictionary<ChildItemType, ChildItem> ChildItemDict;
        /// <summary>
        /// Array of currently visibled Child items (in current Style), cached
        /// </summary>
        protected InteractiveObject[] Items;
        #region class ChildItem : class for child items in ScrollBar (functional interactive areas), enum ChildItemType : type of specific Child item
        /// <summary>
        /// ChildItem : class for child items in ScrollBar (functional interactive areas)
        /// </summary>
        protected class ChildItem : InteractiveObject
        {
            /// <summary>
            /// Konstruktor prvku
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="itemType"></param>
            /// <param name="isEnabled"></param>
            public ChildItem(GScrollBar owner, ChildItemType itemType, bool isEnabled)
            {
                this.Parent = owner;
                this.ItemType = itemType;
                this.ImageType = LinearShapeType.None;
                this.OverCursorType = null;
                this.DragCursorType = null;
                this.Is.MouseDragMove = (itemType == ChildItemType.Thumb);
                this.Is.HoldMouse = false;
                this.Is.Enabled = isEnabled;
            }
            /// <summary>
            /// Vizualizace Child prvku
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.ItemType.ToString() + ": " + this.Bounds.ToString() + "; Owner: " + this.Owner.ToString();
            }
            /// <summary>
            /// Owner of this SubItem = an ScrollBar
            /// </summary>
            public GScrollBar Owner { get { return this.Parent as GScrollBar; } }
            /// <summary>
            /// Type of SubItem
            /// </summary>
            public ChildItemType ItemType { get; private set; }
            /// <summary>
            /// Type of image on this item, in current Orientation. None for items without image.
            /// </summary>
            public LinearShapeType ImageType { get; set; }
            /// <summary>
            /// Cursor type in MouseOver state for this SubItem
            /// </summary>
            public SysCursorType? OverCursorType { get; set; }
            /// <summary>
            /// Cursor type in MouseDrag state for this SubItem
            /// </summary>
            public SysCursorType? DragCursorType { get; set; }
            /// <summary>
            /// This SubItem can be dragged?
            /// </summary>
            public bool CanDrag { get { return (this.Is.Enabled && this.ItemType == ChildItemType.Thumb); } }
            /// <summary>
            /// Is this SubItem currently active SubItem of Owner?
            /// </summary>
            public bool IsActiveChild { get { return (this.ItemType == this.Owner.ActiveChildType); } }
            /// <summary>
            /// Interactive State of this item
            /// </summary>
            public GInteractiveState ItemState { get { return (this.IsActiveChild ? this.InteractiveState : (this.Owner.Is.Enabled ? GInteractiveState.Enabled : GInteractiveState.Disabled)); } }
            /// <summary>
            /// true when this is dragged (CurrentState is LeftDrag or RightDrag)
            /// </summary>
            public new bool IsDragged { get { return base.IsDragged; } }
            /// <summary>
            /// true when this has mouse (CurrentState is MouseOver, LeftDown, RightDown, LeftDrag or RightDrag)
            /// </summary>
            public new bool IsMouseActive { get { return base.IsMouseActive; } }
            /// <summary>
            /// true when this has mouse down (CurrentState is LeftDown, RightDown, LeftDrag or RightDrag)
            /// </summary>
            public new bool IsMouseDown { get { return base.IsMouseDown; } }
            /// <summary>
            /// Current change of logical value after click on this SubItem
            /// </summary>
            public decimal CurrentChangeValue
            {
                get
                {
                    decimal smallChange = this.Owner.ValueRatioSmallChangeCurrent;
                    decimal bigChange = this.Owner.ValueRatioBigChangeCurrent;
                    switch (this.ItemType)
                    {
                        case ChildItemType.MinArrow: return -smallChange;
                        case ChildItemType.MinArea: return -bigChange;
                        case ChildItemType.MaxArea: return bigChange;
                        case ChildItemType.MaxArrow: return smallChange;
                    }
                    return 0m;
                }
            }
            /// <summary>
            /// Called after any interactive change value of State
            /// </summary>
            /// <param name="e"></param>
            protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
            {   // Předám interaktivitu svému Owneru = ScrollBaru:
                this.Owner.AfterStateChanged(e, this);
            }
            /// <summary>
            /// Metoda má vykreslit obsah this prvku.
            /// </summary>
            /// <param name="e">Kreslící argument</param>
            /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
            /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
            protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
            {
                // ChildItem se nevykresluje sám, je vykreslen v rámci celého Scrollbaru, pomocí GPainteru.
                //   base.Draw(e);
            }
        }
        /// <summary>
        /// ChildItemType : druh položky ve ScrollBaru
        /// </summary>
        protected enum ChildItemType : int
        {
            /// <summary>
            /// None
            /// </summary>
            None = 0,
            /// <summary>
            /// MinArrow
            /// </summary>
            MinArrow,
            /// <summary>
            /// MinArea
            /// </summary>
            MinArea,
            /// <summary>
            /// Thumb
            /// </summary>
            Thumb,
            /// <summary>
            /// MaxArea
            /// </summary>
            MaxArea,
            /// <summary>
            /// MaxArrow
            /// </summary>
            MaxArrow,
            /// <summary>
            /// Data
            /// </summary>
            Data,
            /// <summary>
            /// AllArea
            /// </summary>
            AllArea,
            /// <summary>
            /// CenterThumb
            /// </summary>
            CenterThumb
        }
        #endregion
        #endregion
        #region Interaktivita ScrollBaru
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            this.AfterStateChanged(e, null);
        }
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        /// <param name="childItem"></param>
        protected void AfterStateChanged(GInteractiveChangeStateArgs e, ChildItem childItem)
        {
            if (!this.Is.Enabled) return;

            ChildItemType itemType = (childItem == null ? ChildItemType.None : childItem.ItemType);
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseEnter:
                    // Mouse can Enter to main item = this (childItem != null), or to child item (childItem != null):
                    this.ActiveChild = childItem;
                    this.Repaint();
                    if (childItem != null)
                        e.RequiredCursorType = childItem.OverCursorType;
                    break;
                case GInteractiveChangeState.MouseLeave:
                    // Mouse can Leave from main item = this (childItem != null), or from child item (childItem != null):
                    this.ActiveChild = null;
                    this.Repaint();
                    if (childItem != null)
                        e.RequiredCursorType = SysCursorType.Default;
                    break;
                case GInteractiveChangeState.MouseOver:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftDown:
                    if (childItem != null && childItem.Is.Enabled && (itemType == ChildItemType.MinArrow || itemType == ChildItemType.MinArea || itemType == ChildItemType.MaxArea || itemType == ChildItemType.MaxArrow))
                        this.CalculateBoundsInteractiveClick(childItem);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftUp:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftClick:
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftDragMoveBegin:
                    if (childItem != null && childItem.CanDrag)
                    {
                        this.ActiveChild = childItem;
                        this.Repaint();
                        e.RequiredCursorType = childItem.DragCursorType;
                        e.UserDragPoint = childItem.Bounds.Location;
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveStep:
                    if (this.ActiveChild != null && this.ActiveChild.CanDrag && e.UserDragPoint.HasValue)
                    {
                        switch (this.Orientation)
                        {
                            case System.Windows.Forms.Orientation.Horizontal:
                                this.CalculateBoundsInteractiveDragH(this.ActiveChild, e.UserDragPoint.Value);
                                break;
                            case System.Windows.Forms.Orientation.Vertical:
                                this.CalculateBoundsInteractiveDragV(this.ActiveChild, e.UserDragPoint.Value);
                                break;
                        }
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveCancel:
                    break;
                case GInteractiveChangeState.LeftDragMoveDone:
                    if (this.ActiveChild != null && this.ActiveChild.CanDrag)
                    {
                        e.RepaintAllItems = true;
                        this.ChildItemsReset();
                        e.RequiredCursorType = (childItem.OverCursorType.HasValue ? childItem.OverCursorType.Value : SysCursorType.Default);
                    }
                    break;
                case GInteractiveChangeState.WheelUp:
                case GInteractiveChangeState.WheelDown:
                    this.DoScrollBy(e.ChangeState, e.ModifierKeys);
                    e.ActionIsSolved = true;
                    break;
                default:
                    GInteractiveChangeState s = e.ChangeState;
                    break;
            }
        }
        /// <summary>
        /// Vypočte nové souřadnice a hodnoty po LeftClick na daný ChildItem (MinArrow, MinArea, MaxArea, MaxArrow)
        /// </summary>
        /// <param name="subItem"></param>
        protected void CalculateBoundsInteractiveClick(ChildItem subItem)
        {
            if (this._Value == null || !this._Value.IsFilled) return;
            decimal change = subItem.CurrentChangeValue;
            if (change == 0m) return;
            decimal begin = this._Value.Begin.Value + change;
            DecimalNRange value = DecimalNRange.CreateFromBeginSize(begin, this._Value.Size.Value);
            this.SetValue(value, ProcessAction.ChangeAll, EventSourceType.InteractiveChanged);
        }
        /// <summary>
        /// Vypočte nové souřadnice a hodnoty v procesu Drag na Thumb prvku, pro Horizontal ScrollBar
        /// </summary>
        /// <param name="subItem"></param>
        /// <param name="point"></param>
        protected void CalculateBoundsInteractiveDragH(ChildItem subItem, Point point)
        {
            Rectangle bounds = subItem.Bounds;
            int x = this.CalculateInteractiveThumbBegin(point.X);
            if (x != bounds.X)
            {
                bounds.X = x;
                subItem.Bounds = bounds;
                this.Repaint();
            }
        }
        /// <summary>
        /// Vypočte nové souřadnice a hodnoty v procesu Drag na Thumb prvku, pro Vertical ScrollBar
        /// </summary>
        /// <param name="subItem"></param>
        /// <param name="point"></param>
        protected void CalculateBoundsInteractiveDragV(ChildItem subItem, Point point)
        {
            Rectangle bounds = subItem.Bounds;
            int y = this.CalculateInteractiveThumbBegin(point.Y);
            if (y != bounds.Y)
            {
                bounds.Y = y;
                subItem.Bounds = bounds;
                this.Repaint();
            }
        }
        /// <summary>
        /// Return new logical relative position for begin position (X or Y) of Thomb item, by specified DragPoint value.
        /// </summary>
        /// <param name="thumbDragged"></param>
        /// <returns></returns>
        protected int CalculateInteractiveThumbBegin(int thumbDragged)
        {
            decimal thumb = (decimal)thumbDragged;

            if ((thumb + this.ChildDataThumb.Size.Value) > this.ChildDataTotal.End.Value) thumb = (this.ChildDataTotal.End.Value - this.ChildDataThumb.Size.Value);
            if (thumb < this.ChildDataTotal.Begin.Value) thumb = this.ChildDataTotal.Begin.Value;

            decimal pixel = thumb + this.ChildDataThumbOffset;
            decimal begin = this._GetValueFromPixel(pixel);
            this.ValueDrag = DecimalNRange.CreateFromBeginSize(begin, this._Value.Size.Value);

            return (int)thumb;
        }
        /// <summary>
        /// Typ aktivního Child prvku
        /// </summary>
        protected ChildItemType ActiveChildType { get { return (this.ActiveChild != null ? this.ActiveChild.ItemType : ChildItemType.None); } }
        /// <summary>
        /// Aktivní prvek Child
        /// </summary>
        protected ChildItem ActiveChild { get; set; }
        #endregion
        #region Draw + implementace IScrollBarPaintData
        /// <summary>
        /// Draw this scrollbar
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            this.CurrentDrawLayer = e.DrawLayer;
            if (this.ChildItemsCalculate(false))
                GPainter.DrawScrollBar(e.Graphics, absoluteBounds, this as IScrollBarPaintData);
            else
                GPainter.DrawAreaBase(e.Graphics, absoluteBounds, Skin.ScrollBar.BackColorArea, this.Orientation);
        }
        /// <summary>
        /// Defaultní barva BackColor
        /// </summary>
        public override Color BackColorDefault { get { return Skin.ScrollBar.BackColorArea; } }
        /// <summary>
        /// Vyvolá událost <see cref="UserDraw"/> pro uživatelské kreslení pozadí ScrollBaru
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        protected void UserDataDraw(Graphics graphics, Rectangle bounds)
        {
            if (this.UserDraw != null)
            {
                using (GPainter.GraphicsClip(graphics, bounds))
                {
                    try
                    {
                        GUserDrawArgs e = new GUserDrawArgs(graphics, this.CurrentDrawLayer, bounds);
                        this.CallUserDraw(e);
                    }
                    catch { }
                }
            }
        }
        /// <summary>
        /// Aktuálně krelená vrstva
        /// </summary>
        protected GInteractiveDrawLayer CurrentDrawLayer { get; set; }
        #region Implementace IScrollBarPaintData : podpora pro univerzální vykreslení ScrollBaru
        Orientation IScrollBarPaintData.Orientation { get { return this.Orientation; } }
        bool IScrollBarPaintData.IsEnabled { get { return this.Is.Enabled; } }
        Rectangle IScrollBarPaintData.ScrollBarBounds { get { return this.ChildItemAllArea.Bounds; } }
        Color IScrollBarPaintData.ScrollBarBackColor { get { return this.BackColor.Value; } }
        Rectangle IScrollBarPaintData.MinButtonBounds { get { return this.ChildItemMinArrow.Bounds; } }
        GInteractiveState IScrollBarPaintData.MinButtonState { get { return this.ChildItemMinArrow.ItemState; } }
        Rectangle IScrollBarPaintData.DataAreaBounds { get { return this.ChildItemDataArea.Bounds; } }
        Rectangle IScrollBarPaintData.MinAreaBounds { get { return this.ChildItemMinArea.Bounds; } }
        GInteractiveState IScrollBarPaintData.MinAreaState { get { return this.ChildItemMinArea.ItemState; } }
        Rectangle IScrollBarPaintData.MaxAreaBounds { get { return this.ChildItemMaxArea.Bounds; } }
        GInteractiveState IScrollBarPaintData.MaxAreaState { get { return this.ChildItemMaxArea.ItemState; } }
        Rectangle IScrollBarPaintData.MaxButtonBounds { get { return this.ChildItemMaxArrow.Bounds; } }
        GInteractiveState IScrollBarPaintData.MaxButtonState { get { return this.ChildItemMaxArrow.ItemState; } }
        Rectangle IScrollBarPaintData.ThumbButtonBounds { get { return this.ChildItemThumb.Bounds; } }
        GInteractiveState IScrollBarPaintData.ThumbButtonState { get { return this.ChildItemThumb.ItemState; } }
        Rectangle IScrollBarPaintData.ThumbImageBounds { get { return this.ChildItemCenterThumb.Bounds; } }
        void IScrollBarPaintData.UserDataDraw(Graphics graphics, Rectangle bounds) { this.UserDataDraw(graphics, bounds); }
        #endregion
        #endregion
    }
    #region interface IScrollBarData : Podklady pro nastavení ScrollBaru
    /// <summary>
    /// Podklady pro nastavení ScrollBaru
    /// </summary>
    public interface IScrollBarData
    {
        /// <summary>
        /// První pixel v datech, který je aktuálně zobrazován na začátku viditelné výseče (=první čitelný řádek dokumentu)
        /// </summary>
        int DataBegin { get; }
        /// <summary>
        /// Počet pixelů celkem v datech přítomných (=délka celého dokumentu)
        /// </summary>
        int DataSize { get; }
        /// <summary>
        /// Počet pixelů reálně zobrazených v aktuální velikosti (=viditelná výseč dokumentu)
        /// </summary>
        int VisualSize { get; }
        /// <summary>
        /// Přídavek k hodnotě DataSize (=celková velikost dat) v pixelech, který bude předáván do ScrollBaru.
        /// Důvod tohoto přídavku: při čistých výpočtech (bez přídavku) bude datový obsah scrollován zcela přesně do vizuálního prostoru, 
        /// a když Scrollbar dojede na konec prostoru (dolů/doprava), pak obsah bude zobrazen zcela přesně (dole/vpravo bude poslední pixel obsahu).
        /// To je sice matematicky správně, ale vizuálně (ergonomicky) není zřejmé, že "dál už nic není".
        /// Proto se běžně scrollbar chová tak, že při odscrollování na konec se "za koncem dat" zobrazí několik pixelů prázdného prostoru.
        /// Defaultně se zobrazuje 45 pixelů.
        /// Hodnota 0 pak zobrazuje "matematicky správně".
        /// Záporná hodnota v DataSizeOverhead zařídí, že daný bude zobrazen počet pixelů dat nahoře/vlevo:
        /// např. -40 zajistí, že při posunu scrollbaru na konec dráhy se zobrazí nahoře 40 pixelů dat, a celý zbytek bude prázdný.
        /// </summary>
        int DataSizeAddSpace { get; }
    }
    #endregion
}
