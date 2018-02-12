using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Data
{
    #region class BaseRange : base class for all interval classes
    /// <summary>
    /// Base class for Range (=Interval).
    /// Has basically two properties: Begin and End; plus Size = distance from Begin to End.
    /// Basic abstract class has two Type-parameters: TEdge is type of Edge values (Begin and End), 
    /// and TSize is type of value of distance between Begin and End.
    /// </summary>
    /// <typeparam name="TEdge">TEdge is type of Edge values (Begin and End)</typeparam>
    /// <typeparam name="TSize">TSize is type of Size value (distance between Begin and End)</typeparam>
    public abstract class BaseRange<TEdge, TSize>
    {
        #region Constructors, public properties
        public BaseRange()
        {
            this.Begin = default(TEdge);
            this.End = default(TEdge);
        }
        public BaseRange(TEdge begin, TEdge end)
        {
            this.Begin = begin;
            this.End = end;
        }
        /// <summary>
        /// Hashcode from Begin and End
        /// </summary>
        protected int HashCode
        {
            get
            {
                int b = this.Begin.GetHashCode();
                int e = this.End.GetHashCode();
                return (b << 16) | e;
            }
        }
        /// <summary>
        /// Visualisation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.TTickToText(this.Begin) + " ÷ " + this.TTickToText(this.End);
        }
        /// <summary>
        /// Begin of interval
        /// </summary>
        public TEdge Begin { get; protected set; }
        /// <summary>
        /// End of interval
        /// </summary>
        public TEdge End { get; protected set; }
        /// <summary>
        /// Contain center point between Begin and End.
        /// </summary>
        public TEdge Center { get { return Add(Begin, Multiply(Size, 0.5m)); } }
        /// <summary>
        /// Contains distance from Begin to End. Positive distance is where End is bigger than Begin.
        /// </summary>
        public TSize Size { get { return this.SubSize(this.End, this.Begin); } }
        /// <summary>
        /// true, when Begin has value. Value of End is not tested.
        /// </summary>
        public bool HasBegin { get { return (!this.IsEmptyEdge(this.Begin)); } }
        /// <summary>
        /// true, when End has value. Value of Begin is not tested.
        /// </summary>
        public bool HasEnd { get { return (!this.IsEmptyEdge(this.End)); } }
        /// <summary>
        /// true when Begin and End has not values (booth is empty)
        /// </summary>
        public bool IsEmpty { get { return (!this.HasBegin && !this.HasEnd); } }
        /// <summary>
        /// true when Begin and End has values (booth is filled)
        /// </summary>
        public bool IsFilled { get { return (this.HasBegin && this.HasEnd); } }
        /// <summary>
        /// true, when Begin has value ane End has not value
        /// </summary>
        public bool HasOnlyBegin { get { return (this.HasBegin && !this.HasEnd); } }
        /// <summary>
        /// true, when End has value ane Begin has not value
        /// </summary>
        public bool HasOnlyEnd { get { return (!this.HasBegin && this.HasEnd); } }
        /// <summary>
        /// Contains true, when this is filled, and Begin is smaller or equal to End.<para/>
        /// Contains true, when this is empty, or half-filled (has only begin or end).<para/>
        /// Contains false, when this is filled, but Begin is greater than End.
        /// </summary>
        public bool IsReal { get { return (this.IsFilled ? (this.CompareEdge(this.Begin, this.End) <= 0) : true); } }
        /// <summary>
        /// Contains true, only when this is filled, and Begin is equal to End.<para/>
        /// Contains false, when this is empty, or half-filled, or (this is filled and Begin is not equal to End).
        /// </summary>
        public bool IsPoint { get { return (this.IsFilled ? (this.CompareEdge(this.Begin, this.End) == 0) : false); } }
        #endregion
        #region Public support
        /// <summary>
        /// Returns true, when specified value is in this range (include Begin, include End).
        /// Return false, when is outside, or when this is not filled.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(TEdge value)
        {
            if (!this.IsFilled) return false;

            int bec = this.CompareEdge(this.Begin, this.End);
            if (bec > 0) return false;                               // Begin > End : nothing can be between!
            int bvc = this.CompareEdge(this.Begin, value);
            if (bvc > 0) return false;                               // Begin > value : value is before Begin.
            int vec = this.CompareEdge(value, this.End);
            if (vec > 0) return false;                               // value > End : value is after End.
            return true;
        }
        /// <summary>
        /// Returns true, when specified value is in this range (include Begin, include End).
        /// Return false, when is outside, or when this is not filled.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool HasIntersect(BaseRange<TEdge, TSize> range)
        {
            if (!this.IsFilled) return false;
            if (range == null || !range.IsFilled) return false;

            int bec = this.CompareEdge(this.Begin, this.End);
            if (bec > 0) return false;                               // This : Begin > End : nothing can be between!
            bec = range.CompareEdge(range.Begin, range.End);
            if (bec > 0) return false;                               // Other: Begin > End : nothing can be between!

            int bvc = this.CompareEdge(this.Begin, range.End);       // When (this.Begin - range.End) >= 0 (=> (this.Begin >= range.End) then range ends before or at this begin
            if (bvc >= 0) return false;                              //  => no intersect.
            int vec = this.CompareEdge(range.Begin, this.End);       // When (range.Begin - this.End) >= 0 (=> (range.Begin >= this.End) then range begins after or at this end 
            if (vec >= 0) return false;                              //  => no intersect.

            return true;
        }
        #endregion
        #region Protected methods for Zoom support
        /// <summary>
        /// Returns a new instance created from current instance, which Size is (ratio * this.Size) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        protected void PrepareZoomToRatio(TEdge center, decimal ratio, out TEdge begin, out TEdge end)
        {
            if (!this.IsFilled)
            {
                begin = default(TEdge);
                end = default(TEdge);
            }
            else if (ratio == 1.0m)
            {
                begin = this.Begin;
                end = this.End;
            }
            else
            {
                TSize size = this.Multiply(this.Size, ratio);                            // result size = ratio * current size
                TSize distance = this.Multiply(this.SubSize(center, this.Begin), ratio); // (distance from new begin to center) = ratio * (distance from current begin to center) 
                begin = this.SubEdge(center, distance);
                end = this.Add(begin, size);
            }
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Size is specified, and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        protected void PrepareZoomToSizeOnCenterPoint(TEdge center, TSize size, out TEdge begin, out TEdge end)
        {
            if (!this.IsFilled)
            {
                begin = default(TEdge);
                end = default(TEdge);
            }
            else if (this.IsEqualSize(this.Size, size))
            {
                begin = this.Begin;
                end = this.End;
            }
            else
            {
                decimal ratio = this.Divide(this.SubSize(center, this.Begin), this.Size);// Relative position (=ratio) of point "center" on current Size
                begin = this.SubEdge(center, this.Multiply(size, ratio));                // New Begin = point "center" minus (center position on new Time)
                end = this.Add(begin, size);
            }
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified relative position.
        /// </summary>
        /// <param name="relativePivot"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        protected void PrepareZoomToSizeOnRelativePivot(decimal relativePivot, TSize size, out TEdge begin, out TEdge end)
        {
            if (!this.IsFilled)
            {
                begin = default(TEdge);
                end = default(TEdge);
            }
            else if (this.IsEqualSize(this.Size, size))
            {
                begin = this.Begin;
                end = this.End;
            }
            else
            {
                TEdge center = this.GetValueAtRelativePosition(relativePivot);
                TSize distance = this.Multiply(size, relativePivot);
                begin = this.SubEdge(center, distance);
                end = this.Add(begin, size);
            }
        }
        #endregion
        #region Support for operators: combining of two instances, equals
        /// <summary>
        /// Prepare values begin and end for Union of two intervals: a and b.
        /// Union values is: Min(begins) and Max(End), when booth range (a and b) is not IsEmpty.
        /// When one of intervals (a or b) is empty, then union values (out begin, out end) is equal to values begin and end from other, non-empty interval.
        /// When booth intervals (a and b) is empty, then union values (out begin, out end) is empty = default(TEdge).
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="a">Interval A</param>
        /// <param name="b">Interval B</param>
        /// <param name="begin">Out value for Begin for Union interval</param>
        /// <param name="end">Out value for End for Union interval</param>
        public void PrepareUnion(BaseRange<TEdge, TSize> a, BaseRange<TEdge, TSize> b, out TEdge begin, out TEdge end)
        {
            bool isPrepared = PrepareValuesFrom(a, b, out begin, out end);
            if (!isPrepared)
            {   // isPrepared == false => booth range (a and b) are not empty:
                begin = MinEdge(a.Begin, b.Begin);
                end = MaxEdge(a.End, b.End);
            }
        }
        /// <summary>
        /// Prepare values begin and end for Intersect of two intervals: a and b.
        /// Intersect values is: Max(begins) and Min(End), when booth range (a and b) is not IsEmpty.
        /// When result is empty (the two intervals has not valid intersect), then intersect values (out begin, out end) is empty = default(TEdge).
        /// When one of intervals (a or b) is empty, then intersect values (out begin, out end) is equal to values begin and end from other, non-empty interval.
        /// When booth intervals (a and b) is empty, then intersect values (out begin, out end) is empty = default(TEdge).
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="a">Interval A</param>
        /// <param name="b">Interval B</param>
        /// <param name="begin">Out value for Begin for Intersect interval</param>
        /// <param name="end">Out value for End for Intersect interval</param>
        public void PrepareIntersect(BaseRange<TEdge, TSize> a, BaseRange<TEdge, TSize> b, out TEdge begin, out TEdge end)
        {
            bool isPrepared = PrepareValuesFrom(a, b, out begin, out end);
            if (!isPrepared)
            {   // false = booth range (a and b) are not empty:
                begin = MaxEdge(a.Begin, b.Begin);
                end = MinEdge(a.End, b.End);
                
                // Intersect: when Begin > End, then Intersect is empty:
                int ic = this.CompareEdge(begin, end);
                if (ic > 0)
                {
                    begin = default(TEdge);
                    end = default(TEdge);
                }
            }
        }
        /// <summary>
        /// Prepare values begin and end for Align of two intervals: dataValue and rangeValue.
        /// Align ensure, that result value (out begin, end) will be IN rangeValue = will be shifted into range (when input dataValue is outside), with preserve it Size.
        /// When Size of dataValue is greater than Size of rangeValue, then result will be Size reduced to rangeValue.Size.
        /// When rangeValue will have only one edge (only Begin or only End), then result Size is not changed.
        /// When dataValue is half-filled, then only filled edge will be aligned.
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="dataValue">Data value. If rangeValue will be empty or not limiting, then result will be contain values from this Data value.</param>
        /// <param name="rangeValue">Limits for dataValue. Can be null or empty, or half-filled. If rangeValue is not real, will be ignored.</param>
        /// <param name="begin">Out value for Begin for Aligned dataValue interval</param>
        /// <param name="end">Out value for End for Aligned dataValue interval</param>
        public void PrepareAlign(BaseRange<TEdge, TSize> dataValue, BaseRange<TEdge, TSize> rangeValue, out TEdge begin, out TEdge end)
        {
            this.PrepareAlign(dataValue, rangeValue, false, out begin, out end);
        }
        /// <summary>
        /// Prepare values begin and end for Align of two intervals: dataValue and rangeValue.
        /// Align ensure, that result value (out begin, end) will be IN rangeValue = will be shifted into range (when input dataValue is outside), with preserve it Size.
        /// When Size of dataValue is greater than Size of rangeValue, then result will be Size reduced to rangeValue.Size.
        /// When rangeValue will have only one edge (only Begin or only End), then result Size is not changed.
        /// When dataValue is half-filled, then only filled edge will be aligned.
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="dataValue">Data value. If rangeValue will be empty or not limiting, then result will be contain values from this Data value.</param>
        /// <param name="rangeValue">Limits for dataValue. Can be null or empty, or half-filled. If rangeValue is not real, will be ignored.</param>
        /// <param name="preserveSize">Preserve oldValue.Size, when is it possibly</param>
        /// <param name="begin">Out value for Begin for Aligned dataValue interval</param>
        /// <param name="end">Out value for End for Aligned dataValue interval</param>
        public void PrepareAlign(BaseRange<TEdge, TSize> dataValue, BaseRange<TEdge, TSize> rangeValue, bool preserveSize, out TEdge begin, out TEdge end)
        {
            // if Range is filled, but not real, then ignore range:
            if (rangeValue != null & !rangeValue.IsReal)
                rangeValue = null;

            // Detect null or empty in booth or one value:
            // When dataValue is empty, results data are from rangeValue. When rangeValue is empty, results data are from dataValue. When booth are empty, results are empty values.
            bool isPrepared = PrepareValuesFrom(dataValue, rangeValue, out begin, out end);
            if (!isPrepared)
            {   // Have booth values (dataValue and rangeValue), and range is Real (non-negative Size):
                if (!preserveSize || !dataValue.IsFilled || !dataValue.IsReal)
                {   // No preserve data size, or data is not filled or is not real = simply create Max(Begin) and Min(End):
                    begin = MaxEdge(dataValue.Begin, rangeValue.End);
                    end = MinEdge(dataValue.End, rangeValue.End);
                }
                else
                {   // Preserve dataValue.Size during Alignement:
                    TSize size = dataValue.Size;                     // Size is non-negative (data is filled and Real)
                    begin = dataValue.Begin;
                    end = dataValue.End;
                    if (dataValue.HasBegin && rangeValue.HasBegin)
                    {   // Have booth Begin:
                        int bc = CompareEdge(dataValue.Begin, rangeValue.Begin);
                        if (bc < 0)
                        {   // Data.Begin < Range.Begin:
                            begin = rangeValue.Begin;
                            end = Add(begin, size);
                        }
                    }

                    if (dataValue.HasEnd && rangeValue.HasEnd)
                    {   // Have booth Begin:
                        int ec = CompareEdge(dataValue.End, rangeValue.End);
                        if (ec > 0)
                        {   // Data.End > Range.End:
                            end = rangeValue.End;
                            begin = SubEdge(end, size);

                            // Begin can be now lower than Range.Begin!
                            if (dataValue.HasBegin && rangeValue.HasBegin)
                            {   // Have booth Begin:
                                int bc = CompareEdge(dataValue.Begin, rangeValue.Begin);
                                if (bc < 0)
                                {   // Data.Begin < Range.Begin, after Data.End was shifted do Range.End:
                                    begin = rangeValue.Begin;
                                    // End value will not changed!
                                    // This is the case, where Range.Size is smaller than Data.Size, and we must "Align" big Data into small Range.
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Prepare data (out begin, out end) from newValue (contains "explicit" values), and oldValue (contains "default" values).
        /// When explicit values (newValue.Begin and/or End, individually) are filled, then this new values are accepted to out values.
        /// When explicit values are not filled, then old values are accepted to out values.
        /// When booth values are not filled, then out values are default (empty).
        /// Thus: 
        /// out begin = (newValue.HasBegin ? newValue.Begin : oldValue.Begin);
        /// out end = (newValue.HasEnd ? newValue.End : oldValue.End);
        /// This method accept null as input values for oldValue an newValue.
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <param name="begin">Out value for result Begin</param>
        /// <param name="end">Out value for result End</param>
        public void PrepareApplyNewValue(BaseRange<TEdge, TSize> newValue, BaseRange<TEdge, TSize> oldValue, out TEdge begin, out TEdge end)
        {
            this.PrepareApplyNewValue(newValue, oldValue, true, out begin, out end);
        }
        /// <summary>
        /// Prepare data (out begin, out end) from newValue (contains "explicit" values), and oldValue (contains "default" values).
        /// When explicit values (newValue.Begin and/or End, individually) are filled, then this new values are accepted to out values.
        /// When explicit values are not filled, then old values are accepted to out values.
        /// When booth values are not filled, then out values are default (empty).
        /// Thus: 
        /// out begin = (newValue.HasBegin ? newValue.Begin : oldValue.Begin);
        /// out end = (newValue.HasEnd ? newValue.End : oldValue.End);
        /// This method accept null as input values for oldValue an newValue.
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="newValue">New range. Its values will be accepted, if are not empty. If the value (begin, End) is empty, it is taken from oldValue.</param>
        /// <param name="oldValue">Old range. Its values are accepted, when newValue is empty.</param>
        /// <param name="preserveSize">Preserve oldValue.Size, when is it possibly</param>
        /// <param name="begin">Out value for result Begin</param>
        /// <param name="end">Out value for result End</param>
        public void PrepareApplyNewValue(BaseRange<TEdge, TSize> newValue, BaseRange<TEdge, TSize> oldValue, bool preserveSize, out TEdge begin, out TEdge end)
        {
            if (!preserveSize)
            {   // Not preserve size = simply accept non-empty values:
                begin = (newValue != null && newValue.HasBegin ? newValue.Begin : (oldValue != null && oldValue.HasBegin ? oldValue.Begin : default(TEdge)));
                end = (newValue != null && newValue.HasEnd ? newValue.End : (oldValue != null && oldValue.HasEnd ? oldValue.End : default(TEdge)));
            }
            else
            {   // Preserve old size, when new value has one edge empty:
                if (newValue != null && !newValue.IsEmpty)
                {   // New value has values:
                    if (oldValue != null && !oldValue.IsEmpty)
                    {   // Old value has values:
                        TSize size = oldValue.Size;
                        if (newValue.IsFilled)
                        {   // new value has Begin and End:
                            begin = newValue.Begin;
                            end = newValue.End;
                        }
                        else if (newValue.HasOnlyBegin)
                        {   // new value has only Begin => new End is new Begin + old Size:
                            begin = newValue.Begin;
                            end = (oldValue.IsFilled ? Add(begin, size) : oldValue.End);
                        }
                        else if (newValue.HasOnlyEnd)
                        {   // new value has only End => new Begin is new End - old Size:
                            end = newValue.End;
                            begin = (oldValue.IsFilled ? SubEdge(end, size) : oldValue.Begin);
                        }
                        else
                        {   // Only for compiler (newValue is not empty => then IsFilled or HasOnlyBegin or HasOnlyEnd, other option not exists):
                            begin = oldValue.Begin;
                            end = oldValue.End;
                        }
                    }
                    else
	                {   // Old value is null or empty:
                        begin = newValue.Begin;
                        end = newValue.End;
                    }
                }
                else if (oldValue != null)
                {   // New value is noll or empty, old value is not null = we accept old values:
                    begin = oldValue.Begin;
                    end = oldValue.End;
                }
                else
                {
                    begin = default(TEdge);
                    end = default(TEdge);
                }

                // TSize size = 

            }
        }
        /// <summary>
        /// Prepare data (out begin, out end) from value (contains Begin and End) and new Size, and optional pivotRatio.
        /// When value is null or empty, return empty values.
        /// When value is half-filled, then returns fix edge plus/minus size.
        /// When value is Filled, then shrink/expand range to accomodate new Size, with respect to PivotPoint.
        /// PivotPoint is relative position on Range (0=begin, 1=End, any other values are on this "axis"), which is fixed.
        /// When PivotPoint is null, then fixed point is center (as if PivotPoint == 0.5).
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="value">Old value of range</param>
        /// <param name="newSize">New Size</param>
        /// <param name="pivotRatio">The relative position of the point in the range, the value will not be changed. 0=Begin, 1=End. Another values (negative, or any positive) is allowed. Default (for null) is 0.5m</param>
        /// <param name="begin">Out value for result Begin</param>
        /// <param name="end">Out value for result End</param>
        public void PrepareChangeSize(BaseRange<TEdge, TSize> value, TSize newSize, decimal? pivotRatio, out TEdge begin, out TEdge end)
        {
            if (value == null || value.IsEmpty)
            {
                begin = default(TEdge);
                end = default(TEdge);
            }
            else if (value.HasOnlyBegin || value.IsPoint)
            {
                begin = value.Begin;
                end = Add(begin, newSize);
            }
            else if (value.HasOnlyEnd)
            {
                end = value.End;
                begin = SubEdge(end, newSize);
            }
            else
            {
                decimal position = (pivotRatio.HasValue ? pivotRatio.Value : 0.5m);
                TEdge pivotPoint = value.GetPoint(position);         // Pivot point = fixed value (same point on old value and on new value)
                TSize sizeA = Multiply(newSize, position);           // sizeA = from new (searched) Begin point to fixed Pivot point
                begin = SubEdge(pivotPoint, sizeA);                  // New Begin = Pivot point minus sizeA 
                end = Add(begin, newSize);
            }
        }
        /// <summary>
        /// Prepare data (out begin, out end) from value (contains Begin and End) and new Size, and optional pivotRatio.
        /// When value is null or empty, return empty values.
        /// When value is half-filled, then returns fix edge plus/minus size.
        /// When value is Filled, then shrink/expand range to accomodate new Size, with respect to PivotPoint.
        /// PivotPoint is relative position on Range (0=begin, 1=End, any other values are on this "axis"), which is fixed.
        /// When PivotPoint is null, then fixed point is center (as if PivotPoint == 0.5).
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="value">Old value of range</param>
        /// <param name="pivotPoint">Fixed point = Pivot point (same point on old value and on new value)</param>
        /// <param name="newSize">New Size</param>
        /// <param name="begin">Out value for result Begin</param>
        /// <param name="end">Out value for result End</param>
        public void PrepareChangeSize(BaseRange<TEdge, TSize> value, TEdge pivotPoint, TSize newSize, out TEdge begin, out TEdge end)
        {
            if (value == null || value.IsEmpty)
            {
                begin = default(TEdge);
                end = default(TEdge);
            }
            else if (value.HasOnlyBegin || value.IsPoint)
            {
                begin = value.Begin;
                end = Add(begin, newSize);
            }
            else if (value.HasOnlyEnd)
            {
                end = value.End;
                begin = SubEdge(end, newSize);
            }
            else
            {
                decimal? position = value.GetRelativePositionAtValue(pivotPoint);
                if (!position.HasValue)
                {
                    begin = value.Begin;
                    end = value.End;
                }
                else
                {
                    TSize sizeA = Multiply(newSize, position.Value);     // sizeA = from new (searched) Begin point to fixed Pivot point
                    begin = SubEdge(pivotPoint, sizeA);                  // New Begin = Pivot point minus sizeA 
                    end = Add(begin, newSize);
                }
            }
        }
        /// <summary>
        /// Prepare data (out begin, out end) from value (contains Begin and End) and new Size, and optional pivotRatio.
        /// When value is null or empty, return empty values.
        /// When value is half-filled, then returns fix edge plus/minus size.
        /// When value is Filled, then shrink/expand range to accomodate new Size, with respect to PivotPoint.
        /// PivotPoint is relative position on Range (0=begin, 1=End, any other values are on this "axis"), which is fixed.
        /// When PivotPoint is null, then fixed point is center (as if PivotPoint == 0.5).
        /// <para/>
        /// Remarks: these methods (Prepare*(*, out begin, out end)) are here as instantial methods, although we be a static methods (but on generic class this can not be).
        /// For this reasons this methods ignore values in this instance, and all data is received by parametrs.
        /// Next remark: abstract generic class can not create (via standard procedures) new instance of descendant class (yes, here is reflection of input parameters and CreateInstance methods).
        /// And therefore is resulting data prepared to out parameters (begin, end), and create a new instance is a task for descendant itself.
        /// But this methods are public (not protected), for any other classes (non-descendants), which can prepare data (begin, end) via its own procedures, 
        /// and as result create real *Range instance with other methods.
        /// </summary>
        /// <param name="value">Old value of range</param>
        /// <param name="pivotPoint">Fixed point = Pivot point (same point on old value and on new value)</param>
        /// <param name="ratio">Ratio for new Size (New Size = Old Size * Ratio)</param>
        /// <param name="begin">Out value for result Begin</param>
        /// <param name="end">Out value for result End</param>
        public void PrepareChangeSize(BaseRange<TEdge, TSize> value, TEdge pivotPoint, double ratio, out TEdge begin, out TEdge end)
        {
            TSize newSize = this.Multiply(value.Size, (decimal)ratio);
            this.PrepareChangeSize(value, pivotPoint, newSize, out begin, out end);
        }
        /// <summary>
        /// Returns a "point" on "axis", by its relative position.
        /// For position = 0 return Begin, for position = 1 return End, other points are proportionally calculated. 
        /// Value of position can be negative or greater than 1.
        /// When this is not filled or is not real, then return an empty point.
        /// When this.Begin == this.End, then return an empty point.
        /// </summary>
        /// <param name="position">The relative positions of the corresponding point. For position = 0 return Begin, for position = 1 return End, other points are proportionally calculated. Value of position can be negative or greater than 1.</param>
        /// <returns></returns>
        public TEdge GetPoint(decimal position)
        {
            if (!this.IsFilled || !this.IsReal) return default(TEdge);
            if (this.IsPoint) return default(TEdge);

            // Outer limits:
            if (position == 0m) return this.Begin;
            if (position == 1m) return this.End;

            // Proportional values:
            TSize distance = Multiply(this.Size, position);
            return Add(this.Begin, distance);
        }
        /// <summary>
        /// Returns a relative position of specified point in range.
        /// For point == Begin return 0, for point == End return 1. Point can be inside or outside range.
        /// When this is not filled or is not real, then return an null value.
        /// When this.Begin == this.End, then return an null value.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public decimal? GetRelativePositionAtValue(TEdge point)
        {
            if (!this.IsFilled || !this.IsReal) return null;
            if (this.IsPoint) return null;

            // Outer limits:
            if (CompareEdge(point, this.Begin) == 0) return 0m;
            if (CompareEdge(point, this.End) == 0) return 1m;

            // Proportional values:
            TSize distance = SubSize(point, this.Begin);
            return Divide(distance, this.Size);
        }
        /// <summary>
        /// Returns a Value on relative position (where 0 = Begin, 1 = End). Center of interval is on position 0.5d.
        /// When this object is not filled, returns a default value of Edge type (null, zero, empty).
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public TEdge GetValueAtRelativePosition(decimal relativePosition)
        {
            if (!this.IsFilled) return default(TEdge);
            TSize distance = this.Multiply(this.Size, relativePosition);
            return this.Add(this.Begin, distance);
        }
        /// <summary>
        /// Align specified value into this range.
        /// When this is not real (IsReal == false : Begin is greater than End), then return empty value (default).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public TEdge Align(TEdge value)
        {
            return (this.IsReal ? MinEdge(MaxEdge(value, this.Begin), this.End) : default(TEdge));
        }
        /// <summary>
        /// Prepare values begin and end for next evaluating.
        /// When one range (a or b) is null or empty, then prepare data (Begin and End) from other range and returns true (data are prepared).
        /// When booth ranges (a and b) are null or empty, then prepare data (Begin and End) as Empty (default) and returns true (data are prepared).
        /// When booth ranges (a and b) are not empty (Filled, or half-filled), then data (Begin and End) leave Empty (default) and returns false (data are not prepared, another evaluation algorithm is need).
        /// </summary>
        /// <param name="a">Interval A</param>
        /// <param name="b">Interval B</param>
        /// <param name="begin">Out value for Begin</param>
        /// <param name="end">Out value for End</param>
        protected bool PrepareValuesFrom(BaseRange<TEdge, TSize> a, BaseRange<TEdge, TSize> b, out TEdge begin, out TEdge end)
        {
            bool aEmpty = (a == null || a.IsEmpty);
            bool bEmpty = (b == null || b.IsEmpty);
            begin = default(TEdge);
            end = default(TEdge);
            bool result = false;
            if (aEmpty && bEmpty)
            {
                result = true;
            }
            else if (aEmpty)
            {
                begin = b.Begin;
                end = b.Begin;
                result = true;
            }
            else if (bEmpty)
            {
                begin = a.Begin;
                end = a.Begin;
                result = true;
            }
            else
            {   // Booth (a and b) contain any data, results data are not prepared:
                result = false;
            }
            return result;
        }
        /// <summary>
        /// Returns true, when booth instance has equal data
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool IsEqual(BaseRange<TEdge, TSize> a, BaseRange<TEdge, TSize> b)
        {
            bool ae = ((object)a == null || a.IsEmpty);
            bool be = ((object)b == null || b.IsEmpty);
            if (ae && be) return true;                               // Booth is null or empty
            else if (ae || be) return false;                         // Only one is null or empty

            int bc = this.CompareEdge(a.Begin, b.Begin);             // bc = a.Begin - b.Begin
            int ec = this.CompareEdge(a.End, b.End);                 // ec = a.End   - b.End
            return (bc == 0 && ec == 0);
        }
        /// <summary>
        /// Return true when Edge A and Edge B has equal value
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsEqualEdge(TEdge a, TEdge b) { return (this.CompareEdge(a, b) == 0); }
        /// <summary>
        /// Return true when Size A and Size B has equal value
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsEqualSize(TSize a, TSize b) { return (this.CompareSize(a, b) == 0); }
        /// <summary>
        /// Returns a Min value of TEdge type from (params) array of values. Empty values is skipped.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected TEdge MinEdge(params TEdge[] values)
        {
            TEdge result = default(TEdge);
            bool test = false;
            foreach (TEdge value in values)
            {
                if (!IsEmptyEdge(value) && (!test || (test && CompareEdge(value, result) < 0)))
                {
                    result = value;
                    if (!test) test = true;
                }
            }
            return result;
        }
        /// <summary>
        /// Returns a Max value of TEdge type from (params) array of values. Empty values is skipped.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected TEdge MaxEdge(params TEdge[] values)
        {
            TEdge result = default(TEdge);
            bool test = false;
            foreach (TEdge value in values)
            {
                if (!IsEmptyEdge(value) && (!test || (test && CompareEdge(value, result) > 0)))
                {
                    result = value;
                    if (!test) test = true;
                }
            }
            return result;
        }
        #endregion
        #region Public abstract layer for convertions between Edge and Size values
        /// <summary>
        /// Return true when Edge value is empty (or null).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract bool IsEmptyEdge(TEdge value);
        /// <summary>
        /// Return true when Size value is empty (or null).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract bool IsEmptySize(TSize value);
        /// <summary>
        /// Return (EdgeA.CompareTo(EdgeB) = (A - B).
        /// When a or b is empty, then empty value is less than any non-null value. Two empty values are equal.
        /// When a is equal to b, return 0.
        /// When a is less than b, return -1.
        /// When a is greater than b, return +1.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public abstract int CompareEdge(TEdge a, TEdge b);
        /// <summary>
        /// Return (SizeA.CompareTo(SizeB) = (A - B). 
        /// When a or b is empty, then empty value is less than any non-null value. Two empty values are equal.
        /// When a is equal to b, return 0.
        /// When a is less than b, return -1.
        /// When a is greater than b, return +1.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public abstract int CompareSize(TSize a, TSize b);
        /// <summary>
        /// Return point "End" for specified "Begin" and "Size" (return begin + size).
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public abstract TEdge Add(TEdge begin, TSize size);
        /// <summary>
        /// Return point "begin" for specified "End" and "Size" (return end - size).
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public abstract TEdge SubEdge(TEdge end, TSize size);
        /// <summary>
        /// Return distance between (a - b). Positive distance is when a is bigger than b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public abstract TSize SubSize(TEdge a, TEdge b);
        /// <summary>
        /// Return TSIze = size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public abstract TSize Multiply(TSize size, decimal ratio);
        /// <summary>
        /// Return decimal ratio of (a / b)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public abstract decimal Divide(TSize a, TSize b);
        /// <summary>
        /// Return text for specified value of tick
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected abstract string TTickToText(TEdge tick);
        /// <summary>
        /// Return text for specified value of size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected abstract string TSizeToText(TSize size);
        #endregion
    }
    #endregion
    #region class Vector
    /// <summary>
    /// Base class for Vector.
    /// Has basically two properties: Point and Direction.
    /// </summary>
    /// <typeparam name="TPoint">TPoint is type of Point value</typeparam>
    public abstract class BaseVector<TPoint>
    {
        #region Constructors, public properties
        public BaseVector()
        {
            this.Point = default(TPoint);
            this.Direction = Direction.None;
        }
        public BaseVector(TPoint point, Direction direction)
        {
            this.Point = point;
            this.Direction = direction;
        }
        /// <summary>
        /// Hashcode from Point and Direction
        /// </summary>
        protected int HashCode
        {
            get
            {
                int b = this.Point.GetHashCode();
                int e = this.Direction.GetHashCode();
                return (b << 16) | e;
            }
        }
        /// <summary>
        /// Point of vector
        /// </summary>
        public TPoint Point { get; protected set; }
        /// <summary>
        /// Direction of vector
        /// </summary>
        public Data.Direction Direction { get; protected set; }
        /// <summary>
        /// true when Point is Empty and Direction is None
        /// </summary>
        public bool IsEmpty { get { return (this.IsEmptyPoint(this.Point) && this.Direction == Data.Direction.None); } }
        /// <summary>
        /// true when Point is not Empty and Direction is other than None
        /// </summary>
        public bool IsFilled { get { return (!this.IsEmptyPoint(this.Point) && this.Direction != Data.Direction.None); } }
        #endregion
        #region Support for operators - combining of two instances, equals
        /// <summary>
        /// Prepare values begin and end for Intersect (Range) of two Vectors: a and b.
        /// </summary>
        /// <param name="a">Vector A</param>
        /// <param name="b">Vector B</param>
        /// <param name="begin">Out value for Begin for Intersect interval</param>
        /// <param name="end">Out value for End for Intersect interval</param>
        protected void PrepareIntersect(BaseVector<TPoint> a, BaseVector<TPoint> b, out TPoint begin, out TPoint end)
        {
            bool ae = (a == null || a.IsEmpty);
            bool be = (b == null || b.IsEmpty);
            begin = default(TPoint);
            end = default(TPoint);
            if (!ae && !be)
            {
                int pc = this.ComparePoint(a.Point, b.Point);        // pc = a.Point - b.Point
                if (pc <= 0 && a.Direction == Data.Direction.Positive && b.Direction == Data.Direction.Negative)
                {   // a.Point <= b.Point AND a.Dir == Positive AND b.Dir = Negative:
                    begin = a.Point;
                    end = b.Point;
                }
                else if (pc > 0 && a.Direction == Data.Direction.Positive && b.Direction == Data.Direction.Negative)
                {   // a.Point >= b.Point AND a.Dir == Negative AND b.Dir = Positive:
                    begin = b.Point;
                    end = a.Point;
                }
            }
        }
        /// <summary>
        /// Returns true, when booth instance has equal data, or when booth instance is (null or empty).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsEqual(BaseVector<TPoint> a, BaseVector<TPoint> b)
        {
            bool equal = false;
            bool ae = (a == null || a.IsEmpty);
            bool be = (b == null || b.IsEmpty);
            if (ae && be)
            {
                equal = true;
            }
            else if (ae || be)
            {
                equal = false;
            }
            else
            {
                int pc = this.ComparePoint(a.Point, b.Point);         // pc = a.Point - b.Point
                int dc = a.Direction.CompareToDirection(b.Direction); // dc = a.Direction - b.Direction
                equal = (pc == 0 && dc == 0);
            }
            return equal;
        }
        /// <summary>
        /// Return true when Point A and Point B has equal value
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsEqualEdge(TPoint a, TPoint b) { return (this.ComparePoint(a, b) == 0); }
        #endregion
        #region Protected abstract layer for convertions between Edge and Size values
        /// <summary>
        /// Return true when Point value is empty (or null).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract bool IsEmptyPoint(TPoint value);
        /// <summary>
        /// Return (PointA.CompareTo(PointB) = (A - B).
        /// When a or b is empty, then empty value is less than any non-null value. Two empty values are equal.
        /// When a is equal to b, return 0.
        /// When a is less than b, return -1.
        /// When a is greater than b, return +1.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected abstract int ComparePoint(TPoint a, TPoint b);
        #endregion
    }
    #endregion
    #region enums Direction, RoundMode
    /// <summary>
    /// Direction on any axis (Positive, Negative, None).
    /// Has extensions method: Reverse(), NumericValue(), CompareToDirection().
    /// </summary>
    public enum Direction
    {
        None,
        /// <summary>
        /// To higher values (positive numbers, future times)
        /// </summary>
        Positive,
        /// <summary>
        /// No lower values (negative numbers, history times)
        /// </summary>
        Negative
    }
    /// <summary>
    /// Time rounding mode
    /// </summary>
    public enum RoundMode
    {
        /// <summary>
        /// Always to the next lower value
        /// </summary>
        Floor,
        /// <summary>
        /// To nearest lower or higher value, from half interval
        /// </summary>
        Math,
        /// <summary>
        /// Always to the next higher value
        /// </summary>
        Ceiling
    }
    #endregion
}
