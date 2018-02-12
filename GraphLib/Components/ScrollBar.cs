using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Djs.Common.Data;

namespace Djs.Common.Components
{
    /// <summary>
    /// Scroll bar
    /// </summary>
    public class GScrollBar : InteractiveObject, IInteractiveItem
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public GScrollBar()
        {
            this.ChildItemsInit();
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
        #region Bounds, Value and ValueTotal: properties, call Set methods
        #region Bounds, Orientation
        /// <summary>
        /// Oriantation of scrollbar
        /// </summary>
        public Orientation Orientation
        {
            get { return this._Orientation; }
            set
            {
                this.SetOrientation(value, ProcessAction.All, EventSourceType.BoundsChange | EventSourceType.ApplicationCode);
            }
        } private Orientation _Orientation = Orientation.Horizontal;
        /// <summary>
        /// The default width, in pixels, of the vertical scroll bar ( = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth).
        /// </summary>
        public static int DefaultSystemBarWidth { get { return SystemInformation.VerticalScrollBarWidth; } }
        /// <summary>
        /// The default height, in pixels, of the horizontal scroll bar ( = System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight).
        /// </summary>
        public static int DefaultSystemBarHeight { get { return SystemInformation.HorizontalScrollBarHeight; } }
        #endregion
        #region Value, ValueTotal
        /// <summary>
        /// Logical value: position of first visible pixel of document in visible part
        /// </summary>
        public SizeRange Value
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.All, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logical value: position of first visible pixel of document in visible part.
        /// Silent = setting an value to this property do not trigger event ValueChanged.
        /// </summary>
        protected SizeRange ValueSilent
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.SilentValueActions, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logical value: position of first visible pixel of document in visible part.
        /// Drag = setting an value to this property do not reset inner bounds (InnerBoundsReset())
        /// </summary>
        protected SizeRange ValueDrag
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.DragValueActions, EventSourceType.ValueChanging | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Logical value: total size of document (Begin, End)
        /// </summary>
        public SizeRange ValueTotal
        {
            get { return this._ValueTotal; }
            set { this.SetValueTotal(value, ProcessAction.All, EventSourceType.ValueRangeChange | EventSourceType.ApplicationCode); }
        }
        private SizeRange _Value;
        private SizeRange _ValueTotal;
        /// <summary>
        /// Value for small change (by clicking to Up/Down arrow).
        /// Value is ratio of current this.Value.Size.
        /// Default = 0.10m.
        /// Value is aligned to range 0.001 to 1.000 (include).
        /// </summary>
        public decimal? ValueRatioSmallChange
        {
            get { return this._ValueRatioSmallChange; }
            set { this._ValueRatioSmallChange = (value < 0m ? 0.001m : (value > 1m ? 1m : value)); }
        } private decimal? _ValueRatioSmallChange = 0.10m;
        /// <summary>Current small step for current Value and ValueRatioSmallChange ( = ValueRatioSmallChange * Value.Size)</summary>
        protected decimal ValueRatioSmallChangeCurrent { get { return this.GetValueStep(this._ValueRatioSmallChange, 0.10m); } }
        /// <summary>
        /// Value for big change (by clicking to Up/Down area).
        /// Value is ratio of current this.Value.Size.
        /// Default = 0.90m.
        /// Value is aligned to range 0.002 to 1.000 (include).
        /// </summary>
        public decimal? ValueRatioBigChange
        {
            get { return this._ValueRatioBigChange; }
            set { this._ValueRatioBigChange = (value < 0m ? 0.002m : (value > 1m ? 1m : value)); }
        } private decimal? _ValueRatioBigChange = 0.90m;
        /// <summary>Current big step for current Value and ValueRatioBigChange ( = ValueRatioBigChange * Value.Size)</summary>
        protected decimal ValueRatioBigChangeCurrent { get { return this.GetValueStep(this._ValueRatioBigChange, 0.90m); } }
        /// <summary>
        /// Returns step for this.Value and specified ratio
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
        #region Set*() methods, Detect*() methods
        /// <summary>
        /// Is called after change of this.Bounds, without another conditions
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected override void SetBoundsAfterChange(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {   // Change of Bounds never does change Value.
            // Allways (without test of requested actions) does DetectOrientation():
            this.DetectOrientation(LeaveOnlyActions(actions, ProcessAction.CallChangedEvents), eventSource);
        }
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, only when action PrepareInnerItems is specified.
        /// Recalculate SubItems bounds after change this.Bounds.
        /// </summary>
        /// <param name="oldBounds">Old bounds, before change</param>
        /// <param name="newBounds">New bounds. Use this value rather than this.Bounds</param>
        /// <param name="actions">Actions to do</param>
        /// <param name="eventSource">Source of this event</param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.ChildItemsCalculate(true);
        }
        /// <summary>
        /// Store specified value to this.Orientation, call ChildItemsCalculate()...
        /// </summary>
        /// <param name="bounds"></param>
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
        /// Accomodate this.OrientationCurrent to this.OrientationUser and this.VisibleRelativeBounds
        /// </summary>
        /// <param name="raiseOnDrawRequest">Raise the DrawRequest event and method when OrientationCurrent is changed</param>
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
        /// Returns a valid orientation for current VisibleRelativeBounds.
        /// </summary>
        /// <returns></returns>
        internal Orientation GetValidOrientation()
        {
            Size size = this.Bounds.Size;
            return (size.Width >= size.Height ? System.Windows.Forms.Orientation.Horizontal : System.Windows.Forms.Orientation.Vertical);       // Autodetect Orientation
        }
        /// <summary>
        /// Store specified value to this._Value.
        /// Perform optional _AlignValue, ScrollDataValidate, InnerBoundsReset, OnValueChanged.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="align"></param>
        /// <param name="scroll"></param>
        /// <param name="callInnerReset"></param>
        /// <param name="callEvents"></param>
        protected void SetValue(SizeRange value, ProcessAction actions, EventSourceType eventSource)
        {
            if (value == null) return;
            SizeRange oldValue = this._Value;
            SizeRange newValue = value;
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
        /// Store specified value to this._Value.
        /// Perform optional _AlignValue, ScrollDataValidate, InnerBoundsReset, OnValueChanged.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="align"></param>
        /// <param name="scroll"></param>
        /// <param name="callInnerReset"></param>
        /// <param name="callEvents"></param>
        protected void SetValueTotal(SizeRange valueTotal, ProcessAction actions, EventSourceType eventSource)
        {
            if (valueTotal == null) return;
            SizeRange oldValueTotal = this._ValueTotal;
            SizeRange newValueTotal = valueTotal;
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
        /// Align specified value to this.ValueTotal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected SizeRange ValueAlign(SizeRange value)
        {
            SizeRange valueTotal = this.ValueTotal;
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

            return SizeRange.CreateFromBeginSize(begin, size);
        }
        #endregion
        #region Raise events (ValueChanged, ValueRangeChanged, ScaleChanged, ScaleRangeChanged, AreaChanged, DrawRequest)
        /// <summary>
        /// Call method OnValueChanging() and event ValueChanging
        /// </summary>
        protected void CallValueChanging(SizeRange oldValue, SizeRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<SizeRange> args = new GPropertyChangeArgs<SizeRange>(eventSource, oldValue, newValue);
            this.OnValueChanging(args);
            if (!this.IsSupressedEvent && this.ValueChanging != null)
                this.ValueChanging(this, args);
        }
        /// <summary>
        /// Occured during interactive changing Value value
        /// </summary>
        protected virtual void OnValueChanging(GPropertyChangeArgs<SizeRange> args) { }
        /// <summary>
        /// Event on this.Value interactive changing
        /// </summary>
        public event GPropertyChanged<SizeRange> ValueChanging;

        /// <summary>
        /// Call method OnValueChanged() and event ValueChanged
        /// </summary>
        protected void CallValueChanged(SizeRange oldValue, SizeRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<SizeRange> args = new GPropertyChangeArgs<SizeRange>(eventSource, oldValue, newValue);
            this.OnValueChanged(args);
            if (!this.IsSupressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
        }
        /// <summary>
        /// Occured after change Value value
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<SizeRange> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChanged<SizeRange> ValueChanged;

        /// <summary>
        /// Call method OnValueTotalChanged() and event ValueTotalChanged
        /// </summary>
        protected void CallValueTotalChanged(SizeRange oldValue, SizeRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<SizeRange> args = new GPropertyChangeArgs<SizeRange>(eventSource, oldValue, newValue);
            this.OnValueTotalChanged(args);
            if (!this.IsSupressedEvent && this.ValueTotalChanged != null)
                this.ValueTotalChanged(this, args);
        }
        /// <summary>
        /// Occured after change Value value
        /// </summary>
        protected virtual void OnValueTotalChanged(GPropertyChangeArgs<SizeRange> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChanged<SizeRange> ValueTotalChanged;

        /// <summary>
        /// Call method OnOrientationChanged() and event OrientationChanged
        /// </summary>
        protected void CallOrientationChanged(Orientation oldValue, Orientation newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Orientation> args = new GPropertyChangeArgs<Orientation>(eventSource, oldValue, newValue);
            this.OnOrientationChanged(args);
            if (!this.IsSupressedEvent && this.OrientationChanged != null)
                this.OrientationChanged(this, args);
        }
        /// <summary>
        /// Occured after change Scale value
        /// </summary>
        protected virtual void OnOrientationChanged(GPropertyChangeArgs<Orientation> args) { }
        /// <summary>
        /// Event on this.Scale changes
        /// </summary>
        public event GPropertyChanged<Orientation> OrientationChanged;

        /// <summary>
        /// Call method OnUserDraw() and event UserDraw
        /// </summary>
        protected void CallUserDraw(GUserDrawArgs e)
        {
            this.OnUserDraw(e);
            if (!this.IsSupressedEvent && this.UserDraw != null)
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
        #region ChildItems relative bounds calculator; ChildPixels data calculator
        /// <summary>
        /// Ensure validity for InnerBounds (when IsInnerAreaValid = false)
        /// </summary>
        /// <returns></returns>
        protected bool ChildItemsCalculate(bool force)
        {
            if (this.IsChildItemsValid && !force) return true;        // If all is valid and not need Force, then OK
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
        /// Calculate inner area for horizontal orientation
        /// </summary>
        protected void ChildItemsCalculateH()
        {
            Rectangle r = base.Bounds;
            int buttonLength;
            if (!this.ChildPixelsCalculate(0, r.Width, r.Height, out buttonLength))
                return;

            int begin = 0;
            int size = r.Height - 1;
            int areaEnd = r.Width - 1 - buttonLength;
            int current = 0;
            this.ChildItemMinArrow.Bounds = new Rectangle(current, begin, buttonLength, size);
            current = areaEnd;
            this.ChildItemMaxArrow.Bounds = new Rectangle(current, begin, buttonLength, size);
            this.ChildItemDataArea.Bounds = new Rectangle((int)this.ChildDataTotal.Begin.Value, begin, (int)this.ChildDataTotal.Size.Value, size);
            this.ChildItemAllArea.Bounds = new Rectangle((int)this.ChildDataTotal.Begin.Value, begin, (int)this.ChildDataTotal.Size.Value, size);
            this.ChildItemMinArea.Bounds = new Rectangle((int)this.ChildDataTotal.Begin.Value, begin, (int)(this.ChildDataThumb.Begin.Value - this.ChildDataTotal.Begin.Value), size);
            this.ChildItemMaxArea.Bounds = new Rectangle((int)this.ChildDataThumb.End.Value, begin, (int)(this.ChildDataTotal.End.Value - this.ChildDataThumb.End.Value), size);
            this.ChildItemThumb.Bounds = new Rectangle((int)this.ChildDataThumb.Begin.Value, begin, (int)this.ChildDataThumb.Size.Value, size);

            this.ChildItemMinArrow.ImageType = LinearShapeType.LeftArrow;
            this.ChildItemThumb.ImageType = LinearShapeType.HorizontalLines;
            this.ChildItemMaxArrow.ImageType = LinearShapeType.RightArrow;
            this.ChildItemThumb.DragCursorType = SysCursorType.NoMoveHoriz;

            // this.SubItemCenterThumb.RelativeBoundsSet(new Rectangle(thumbBegin + (thumbEnd - thumbBegin - size) / 2, begin, size, size));

            this.IsChildItemsValid = true;
        }
        /// <summary>
        /// Calculate inner area for vertical orientation
        /// </summary>
        protected void ChildItemsCalculateV()
        {
            Rectangle r = base.Bounds;
            int buttonLength;
            if (!this.ChildPixelsCalculate(0, r.Height, r.Width, out buttonLength))
                return;

            int begin = 0;
            int size = r.Width - 1;
            int areaEnd = r.Height - 1 - buttonLength;
            int current = 0;
            this.ChildItemMinArrow.Bounds = new Rectangle(begin, current, size, buttonLength);
            current = areaEnd;
            this.ChildItemMaxArrow.Bounds = new Rectangle(begin, current, size, buttonLength);
            this.ChildItemDataArea.Bounds = new Rectangle(begin, (int)this.ChildDataTotal.Begin.Value, size, (int)this.ChildDataTotal.Size.Value);
            this.ChildItemAllArea.Bounds = new Rectangle(begin, (int)this.ChildDataTotal.Begin.Value, size, (int)this.ChildDataTotal.Size.Value);
            this.ChildItemMinArea.Bounds = new Rectangle(begin, (int)this.ChildDataTotal.Begin.Value, size, (int)(this.ChildDataValue.Center.Value - this.ChildDataTotal.Begin.Value));
            this.ChildItemMaxArea.Bounds = new Rectangle(begin, (int)this.ChildDataValue.Center.Value, size, (int)(this.ChildDataTotal.End.Value - this.ChildDataValue.Center.Value));
            this.ChildItemThumb.Bounds = new Rectangle(begin, (int)this.ChildDataThumb.Begin.Value, size, (int)this.ChildDataThumb.Size.Value);

            this.ChildItemMinArrow.ImageType = LinearShapeType.UpArrow;
            this.ChildItemThumb.ImageType = LinearShapeType.VerticalLines;
            this.ChildItemMaxArrow.ImageType = LinearShapeType.DownArrow;
            this.ChildItemThumb.DragCursorType = SysCursorType.NoMoveVert;

            // this.SubItemCenterThumb.RelativeBoundsSet(new Rectangle(thumbBegin + (thumbEnd - thumbBegin - size) / 2, begin, size, size));

            this.IsChildItemsValid = true;
        }
        /// <summary>
        /// Invalidate relative bounds in ChildItems.
        /// </summary>
        protected void ChildItemsReset()
        {
            this.IsChildItemsValid = false;
        }
        /// <summary>
        /// true when ChildItems contains valid relative bounds (by current Value, ValueTotal and Bounds); false when not valid.
        /// </summary>
        protected bool IsChildItemsValid { get; private set; }

        /// <summary>
        /// Calculate data for current ValueTotal, Value, and active visual Length in pixels.
        /// Calculate data into SizeRange to ChildDataTotal, ChildDataTrack, ChildDataValue and ChildDataThumb.
        /// </summary>
        /// <param name="begin">Relative pixel (in active direction: Horizontal = Left, Vertical = Top), where active Scrollbar begins</param>
        /// <param name="length">Length of ScrollBar in active direction (Horizontal = Width, Vertical = Height)</param>
        /// <param name="width">Thick of ScrollBar in non-active direction (Horizontal = Height, Vertical = Width)</param></param>
        /// <param name="buttonLength">Out: Size for Min and Max buttons in active direction (Horizontal = Width, Vertical = Height)</param>
        /// <returns>true = OK</returns>
        private bool ChildPixelsCalculate(int begin, int length, int width, out int buttonLength)
        {
            this.ChildPixelsReset();
            buttonLength = 0;
            if (this._ValueTotal == null || !this._ValueTotal.IsFilled) return false;
            if (this._Value == null || !this._Value.IsFilled || this._Value.Size <= 0m) return false;
            if (length < 10) return false;

            buttonLength = (length > 25 ? ((length > (5 * width)) ? width : length / 5) : 0);
            this.ChildDataTotal = SizeRange.CreateFromBeginSize(begin + buttonLength, length - (2 * buttonLength));
            this.ChildDataTrack = this.ChildDataTotal.Clone;
            this.ChildDataScale = this._ValueTotal.Size.Value / this.ChildDataTrack.Size.Value;
            this.ChildDataValue = this._GetPixelFromValue(this._Value, false);
            decimal minThumbSize = (this.ChildDataTotal.Size.Value < 24m ? this.ChildDataTotal.Size.Value / 3m : 8m);
            if (this.ChildDataValue.Size.Value >= minThumbSize)
            {   // Thumb size is correct => no offset, no inflate _PixelThumb, no shrink _PixelTrack:
                this.ChildDataThumbOffset = 0m;
                this.ChildDataThumb = this.ChildDataValue.Clone;
            }
            else
            {   // Thumbs theoretical size is smaller than 8px, we must inflate thumb to 8px (or 1/3 of _PixelTotal.Size):
                this.ChildDataThumbOffset = (minThumbSize - this.ChildDataValue.Size.Value) / 2m;
                this.ChildDataTrack = new SizeRange(this.ChildDataTotal.Begin.Value + this.ChildDataThumbOffset, this.ChildDataTotal.End.Value - this.ChildDataThumbOffset);
                this.ChildDataScale = this._ValueTotal.Size.Value / this.ChildDataTrack.Size.Value;
                this.ChildDataValue = this._GetPixelFromValue(this._Value, false);
                this.ChildDataThumb = new SizeRange(this.ChildDataValue.Begin.Value - this.ChildDataThumbOffset, this.ChildDataValue.End.Value + this.ChildDataThumbOffset);
            }

            // Round this.ChildDataThumb to Int32:
            decimal thumbBegin = this.ChildDataThumb.Begin.Value;
            decimal roundBegin = Math.Round(this.ChildDataThumb.Begin.Value, 0);
            decimal roundSize = Math.Round(this.ChildDataThumb.Size.Value, 0);
            this.ChildDataThumb = SizeRange.CreateFromBeginSize(roundBegin, roundSize);
            // this._PixelThumbOffset += (thumbBegin - roundBegin);    // Hezký úmysl, ale musely by se předělat metody CalculateBoundsInteractiveDragH() a CalculateBoundsInteractiveDragV() tak, aby ... co vlastně? No, jednoduše to nefungovalo.

            return true;
        }
        private void ChildPixelsReset()
        {
            this.ChildDataTotal = null;
            this.ChildDataTrack = null;
            this.ChildDataValue = null;
            this.ChildDataThumb = null;
            this.ChildDataThumbOffset = 0m;
            this.ChildDataScale = 0m;
        }
        /// <summary>
        /// Range of pixel of whole scrollable area (area for thumb move).
        /// Not for calculations, only for InnerArea (interactivity and paint).
        /// Integer numbers (as decimal, but without fractions).
        /// </summary>
        private SizeRange ChildDataTotal;
        /// <summary>
        /// Range of pixel of theoretical thumb position.
        /// Theoretical value, for calculations.
        /// Real thumb can be bigger.
        /// Decimal numbers (with fractions).
        /// </summary>
        private SizeRange ChildDataValue;
        /// <summary>
        /// Range of pixel of real thumb position.
        /// When theoretical value in this._Pixel has .Size small than 8 pixel, then _PixelThumb has size 8 pixel, and is "shifted" = centered around this._Pixel.
        /// Not for calculations, only for InnerArea (interactivity and paint).
        /// Integer numbers (as decimal, but without fractions).
        /// </summary>
        private SizeRange ChildDataThumb;
        /// <summary>
        /// Area for scrolling of theoretical thumb (this._Pixel).
        /// When theoretical value in this._Pixel has .Size small than 8 pixel, and _PixelThumb is inflate, then usable area for track is smaller (about this inflate).
        /// Theoretical value, for calculations.
        /// Decimal numbers (with fractions).
        /// </summary>
        private SizeRange ChildDataTrack;
        /// <summary>
        /// Offset = (this._Pixel.Begin - this._PixelThumb.Begin).
        /// For interactive change (Thumb.Dragging), where are from physical coordinates of thumb (_PixelThumb) calculated theoretical position (_Pixel).
        /// Calculations: _Pixel.Begin = (_PixelThumb.Begin + Offset); _PixelThumb.Begin = (_Pixel.Begin + Offset).
        /// It also includes the difference of rounding (in the same sense).
        /// </summary>
        private decimal ChildDataThumbOffset;
        #endregion
        #region Convert Value to/from Pixel
        /// <summary>
        /// Return SizeRange in pixels from value (booth in absolute coordinates)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="roundToInt"></param>
        /// <returns></returns>
        private SizeRange _GetPixelFromValue(SizeRange value, bool roundToInt)
        {
            decimal begin = _GetPixelFromValue(value.Begin.Value, roundToInt);
            decimal size = _GetPixelDistanceFromValue(value.Size.Value, roundToInt);
            return SizeRange.CreateFromBeginSize(begin, size);
        }
        /// <summary>
        /// Return SizeRange in pixels from value (booth in absolute coordinates)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="roundToInt"></param>
        /// <returns></returns>
        private SizeRange _GetValueFromPixel(SizeRange value)
        {
            decimal begin = _GetValueFromPixel(value.Begin.Value);
            decimal size = _GetValueDistanceFromPixel(value.Size.Value);
            return SizeRange.CreateFromBeginSize(begin, size);
        }
        /// <summary>
        /// Return absolute pixel for specified absolute value.
        /// Result can be rounded or unrounded, ba parameter roundToInt.
        /// </summary>
        /// <param name="value"></param>
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
        /// <param name="data"></param>
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
        #region Child items, Init, Filter and Cache, Type-properties
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetSubItems(); } }
        /// <summary>
        /// Create all Child items
        /// </summary>
        protected void ChildItemsInit()
        {
            this.ChildItemDict = new Dictionary<ChildItemType, ChildItem>();
            // Order of items is liable, in this order will be Childs drawed:
            this.ChildItemDict.Add(ChildItemType.AllArea, new ChildItem(this, ChildItemType.AllArea));
            this.ChildItemDict.Add(ChildItemType.Data, new ChildItem(this, ChildItemType.Data));
            this.ChildItemDict.Add(ChildItemType.MinArea, new ChildItem(this, ChildItemType.MinArea, true));
            this.ChildItemDict.Add(ChildItemType.MaxArea, new ChildItem(this, ChildItemType.MaxArea, true));
            this.ChildItemDict.Add(ChildItemType.MinArrow, new ChildItem(this, ChildItemType.MinArrow, true));
            this.ChildItemDict.Add(ChildItemType.MaxArrow, new ChildItem(this, ChildItemType.MaxArrow, true));
            this.ChildItemDict.Add(ChildItemType.Thumb, new ChildItem(this, ChildItemType.Thumb, true));
            this.ChildItemDict.Add(ChildItemType.CenterThumb, new ChildItem(this, ChildItemType.CenterThumb));
        }
        /// <summary>
        /// Child Item, representing whole area of ScrollBar
        /// </summary>
        protected ChildItem ChildItemAllArea { get { return this.ChildItemDict[ChildItemType.AllArea]; } }
        /// <summary>
        /// Child Item, representing data area of ScrollBar (without MinArrow a MaxArrow)
        /// </summary>
        protected ChildItem ChildItemDataArea { get { return this.ChildItemDict[ChildItemType.Data]; } }
        /// <summary>
        /// Child Item, representing MinArrow (small button Down/Left)
        /// </summary>
        protected ChildItem ChildItemMinArrow { get { return this.ChildItemDict[ChildItemType.MinArrow]; } }
        /// <summary>
        /// Child Item, representing flat area for "PageDown" / "PageLeft"
        /// </summary>
        protected ChildItem ChildItemMinArea { get { return this.ChildItemDict[ChildItemType.MinArea]; } }
        /// <summary>
        /// Child Item, representing draggable thumb on Scrollbar, which size is relative to current value
        /// </summary>
        protected ChildItem ChildItemThumb { get { return this.ChildItemDict[ChildItemType.Thumb]; } }
        /// <summary>
        /// Child Item, representing flat area for "PageUp" / "PageRight"
        /// </summary>
        protected ChildItem ChildItemMaxArea { get { return this.ChildItemDict[ChildItemType.MaxArea]; } }
        /// <summary>
        /// Child Item, representing MaxArrow (small button Up/Right)
        /// </summary>
        protected ChildItem ChildItemMaxArrow { get { return this.ChildItemDict[ChildItemType.MaxArrow]; } }
        /// <summary>
        /// Child Item, representing center of thumb on Scrollbar (for image or another graphic), has constant size
        /// </summary>
        protected ChildItem ChildItemCenterThumb { get { return this.ChildItemDict[ChildItemType.CenterThumb]; } }
        /// <summary>
        /// Returns a (cached) array of currently active items
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<InteractiveObject> GetSubItems()
        {
            if (this.Items == null || this.LastStyle != this.Style)
            {
                this.Items = this.ChildItemDict.Values.Where(i => i.IsEnabled).ToArray();
                this.LastStyle = this.Style;
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
        /// <summary>
        /// Interactive style, for which is filtered and cached Child items from ChildItemDict.Values into this.Items
        /// </summary>
        protected GInteractiveStyles LastStyle = GInteractiveStyles.None;
        #region class ChildItem : class for child items in ScrollBar (functional interactive areas), enum ChildItemType : type of specific Child item
        /// <summary>
        /// ChildItem : class for child items in ScrollBar (functional interactive areas)
        /// </summary>
        protected class ChildItem : InteractiveObject
        {
            public ChildItem(GScrollBar owner, ChildItemType itemType)
            {
                this.Parent = owner;
                this.ItemType = itemType;
                this.ImageType = LinearShapeType.None;
                this.OverCursorType = null;
                this.DragCursorType = null;
                this.IsHoldMouse = false;
            }
            public ChildItem(GScrollBar owner, ChildItemType itemType, bool isEnabled)
                : this(owner, itemType)
            {
                this.IsEnabled = isEnabled;
            }
            public ChildItem(GScrollBar owner, ChildItemType itemType, bool isEnabled, SysCursorType? dragCursorType)
                : this(owner, itemType)
            {
                this.IsEnabled = isEnabled;
                this.DragCursorType = dragCursorType;
            }
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
            public bool CanDrag { get { return (this.IsEnabled && this.ItemType == ChildItemType.Thumb); } }
            /// <summary>
            /// Is this SubItem currently active SubItem of Owner?
            /// </summary>
            public bool IsActiveChild { get { return (this.ItemType == this.Owner.ActiveChildType); } }
            /// <summary>
            /// Interactive State of this item
            /// </summary>
            public GInteractiveState ItemState { get { return (this.IsActiveChild ? this.CurrentState : (this.Parent.IsEnabled ? GInteractiveState.Enabled : GInteractiveState.Disabled)); } }
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
            {
                this.Owner.AfterStateChanged(e, this);
            }
            /// <summary>
            /// Called to draw content of this item.
            /// This instance (GScrollbar.ChildItem) is not drawed by system, but as part of Scrollbar.
            /// </summary>
            /// <param name="e"></param>
            protected override void Draw(GInteractiveDrawArgs e)
            {
                // This instance (GScrollbar.ChildItem) is not drawed by system, but as part of Scrollbar:
                //   base.Draw(e);
            }
        }
        /// <summary>
        /// ChildItemType : type of specific Child item
        /// </summary>
        protected enum ChildItemType : int
        {
            None = 0,
            MinArrow,
            MinArea,
            Thumb,
            MaxArea,
            MaxArrow,
            Data,
            AllArea,
            CenterThumb
        }
        #endregion
        #endregion
        #region Interactivity
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
        protected void AfterStateChanged(GInteractiveChangeStateArgs e, ChildItem childItem)
        {
            if (!this.IsEnabled) return;

            ChildItemType it = (childItem == null ? ChildItemType.None : childItem.ItemType);
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseEnter:
                    // Mouse can Enter to main item = this (childItem != null), or to child item (childItem != null):
                    this.ActiveChild = childItem;
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    if (childItem != null)
                        e.RequiredCursorType = childItem.OverCursorType;
                    break;
                case GInteractiveChangeState.MouseLeave:
                    // Mouse can Leave from main item = this (childItem != null), or from child item (childItem != null):
                    this.ActiveChild = null;
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    if (childItem != null)
                        e.RequiredCursorType = SysCursorType.Default;
                    break;
                case GInteractiveChangeState.MouseOver:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftDown:
                    if (childItem != null && childItem.IsEnabled && (it == ChildItemType.MinArrow || it == ChildItemType.MinArea || it == ChildItemType.MaxArea || it == ChildItemType.MaxArrow))
                        this.CalculateBoundsInteractiveClick(childItem);
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftUp:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftClick:
                    this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftDragBegin:
                    if (childItem != null && childItem.CanDrag)
                    {
                        this.ActiveChild = childItem;
                        this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                        e.RequiredCursorType = childItem.DragCursorType;
                        e.UserDragPoint = childItem.Bounds.Location;
                    }
                    break;
                case GInteractiveChangeState.LeftDragMove:
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
                case GInteractiveChangeState.LeftDragCancel:
                    break;
                case GInteractiveChangeState.LeftDragDone:
                    if (this.ActiveChild != null && this.ActiveChild.CanDrag)
                    {
                        e.RepaintAllItems = true;
                        this.ChildItemsReset();
                        e.RequiredCursorType = (childItem.OverCursorType.HasValue ? childItem.OverCursorType.Value : SysCursorType.Default);
                    }
                    break;
                default:
                    GInteractiveChangeState s = e.ChangeState;
                    break;
            }
        }
        /// <summary>
        /// Calculate new bounds and values after LeftClick on subItem (MinArrow, MinArea, MaxArea, MaxArrow)
        /// </summary>
        /// <param name="subItem"></param>
        protected void CalculateBoundsInteractiveClick(ChildItem subItem)
        {
            if (this._Value == null || !this._Value.IsFilled) return;
            decimal change = subItem.CurrentChangeValue;
            if (change == 0m) return;
            decimal begin = this._Value.Begin.Value + change;
            SizeRange value = SizeRange.CreateFromBeginSize(begin, this._Value.Size.Value);
            this.SetValue(value, ProcessAction.All, EventSourceType.InteractiveChanged);
        }
        /// <summary>
        /// Calculate new bounds and values during interactive moving (Drag) of Thumb item, on Horizontal scrollbar
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
                this.RepaintToLayers = GInteractiveDrawLayer.Standard;
            }
        }
        /// <summary>
        /// Calculate new bounds and values during interactive moving (Drag) of Thumb item, on Vertical scrollbar
        /// </summary>
        /// <param name="subItem"></param>
        /// <param name="p"></param>
        protected void CalculateBoundsInteractiveDragV(ChildItem subItem, Point point)
        {
            Rectangle bounds = subItem.Bounds;
            int y = this.CalculateInteractiveThumbBegin(point.Y);
            if (y != bounds.Y)
            {
                bounds.Y = y;
                subItem.Bounds = bounds;
                this.RepaintToLayers = GInteractiveDrawLayer.Standard;
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
            this.ValueDrag = SizeRange.CreateFromBeginSize(begin, this._Value.Size.Value);

            return (int)thumb;
        }
        protected ChildItemType ActiveChildType { get { return (this.ActiveChild != null ? this.ActiveChild.ItemType : ChildItemType.None); } }
        protected ChildItem ActiveChild { get; set; }
        #endregion
        #region Draw
        /// <summary>
        /// Draw this scrollbar
        /// </summary>
        /// <param name="e"></param>
        protected override void Draw(GInteractiveDrawArgs e)
        {
            e.Graphics.FillRectangle(Skin.Brush(Skin.ScrollBar.BackColorArea), this.BoundsAbsolute);
            if (!this.ChildItemsCalculate(false)) return;

            bool scrollBarIsEnabled = this.IsEnabled;

            this.DrawOneBack(e.Graphics, this.ChildItemAllArea, scrollBarIsEnabled);
            if (this.ChildItemMinArea.IsMouseActive)
                this.DrawOneBase(e.Graphics, this.ChildItemMinArea, scrollBarIsEnabled);
            if (this.ChildItemMaxArea.IsMouseActive)
                this.DrawOneBase(e.Graphics, this.ChildItemMaxArea, scrollBarIsEnabled);
            this.DrawUserData(e.Graphics, e.DrawLayer, this.ChildItemDataArea, scrollBarIsEnabled);
            this.DrawOneButton(e.Graphics, this.ChildItemMinArrow, 0, scrollBarIsEnabled);
            if (scrollBarIsEnabled)
                this.DrawOneButton(e.Graphics, this.ChildItemThumb, 1, scrollBarIsEnabled);
            this.DrawOneButton(e.Graphics, this.ChildItemMaxArrow, 0, scrollBarIsEnabled);
        }
        protected void DrawOneBack(Graphics graphics, ChildItem subItem, bool scrollBarIsEnabled)
        {
            Rectangle bounds = subItem.GetAbsoluteVisibleBounds();
            this.Host.FillRectangle(graphics, bounds, Skin.ScrollBar.BackColorArea);
        }
        protected void DrawOneBase(Graphics graphics, ChildItem subItem, bool scrollBarIsEnabled)
        {
            if (subItem.IsEnabled)
            {
                Rectangle bounds = subItem.GetAbsoluteVisibleBounds();
                GPainter.DrawAreaBase(graphics, bounds, Skin.ScrollBar.BackColorArea, subItem.ItemState, this.Orientation, null, null);
            }
        }
        protected void DrawOneButton(Graphics graphics, ChildItem subItem, int roundCorner, bool scrollBarIsEnabled)
        {
            if (subItem.IsEnabled)
            {
                Rectangle bounds = subItem.GetAbsoluteVisibleBounds();
                if (subItem.IsMouseDown)
                    bounds = bounds.ShiftBy(1, 1);

                GPainter.DrawButtonBase(graphics, bounds, Skin.ScrollBar.BackColorButton, subItem.ItemState, this.Orientation, roundCorner, null, null);
                if (subItem.ImageType != LinearShapeType.None)
                {
                    GraphicSetting graphicSetting;
                    GraphicsPath imagePath = GPainter.CreatePathLinearShape(subItem.ImageType, bounds, 2, out graphicSetting);
                    if (imagePath != null)
                    {
                        GInteractiveState state = (scrollBarIsEnabled ? subItem.ItemState : GInteractiveState.Disabled);
                        Color foreColor = Skin.GetForeColor(Skin.ScrollBar.TextColorButton, state);
                        using (GPainter.GraphicsUse(graphics, graphicSetting))
                        {
                            graphics.DrawPath(Skin.Pen(foreColor), imagePath);
                        }
                    }
                }
            }
        }
        protected void DrawUserData(Graphics graphics, GInteractiveDrawLayer drawLayer, ChildItem subItem, bool scrollBarIsEnabled)
        {
            Region clip = graphics.Clip;
            try
            {
                Rectangle bounds = subItem.GetAbsoluteVisibleBounds();
                graphics.SetClip(bounds);
                GUserDrawArgs e = new GUserDrawArgs(graphics, drawLayer, bounds);
                this.CallUserDraw(e);
            }
            finally
            {
                if (clip.IsInfinite(graphics))
                    graphics.ResetClip();
                else
                    graphics.SetClip(clip, CombineMode.Replace);
            }
        }
        #endregion
    }
}
