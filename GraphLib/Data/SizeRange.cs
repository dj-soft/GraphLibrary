using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Data;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Data
{
    #region SizeRange = BaseRange<Decimal?, Decimal?>
    /// <summary>
    /// Size range (Begin, End), with Decimal? Ticks and Decimal? Size
    /// </summary>
    public class SizeRange : BaseRange<Decimal?, Decimal?>
    {
        #region Constructors, Visualiser, Helper
        public SizeRange() : base() { }
        public SizeRange(Decimal? begin, Decimal? end) : base(begin, end) { }
        /// <summary>
        /// Allways returns a new instance of SizeRange, containing empty values
        /// </summary>
        public static SizeRange Empty { get { return new SizeRange(); } }
        /// <summary>
        /// Allways returns a new instance of SizeRange, containing current values from this instance
        /// </summary>
        public SizeRange Clone { get { return new SizeRange(this.Begin, this.End); } }
        /// <summary>
        /// Create interval from begin and time (size). Booth must be defined.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static SizeRange CreateFromBeginSize(Decimal begin, Decimal size)
        {
            return new SizeRange(begin, begin + (decimal)size);
        }
        /// <summary>
        /// Create interval from time (duration) and end. Booth must be defined.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static SizeRange CreateFromSizeEnd(Decimal size, Decimal end)
        {
            return new SizeRange(end - (decimal)size, end);
        }
        /// <summary>
        /// Contains a textual form of this interval
        /// </summary>
        public string Text
        {
            get
            {
                if (this.IsFilled)
                    return this.Begin.Value.ToString() + " ÷ " + this.End.Value.ToString();
                if (this.HasOnlyBegin)
                    return this.Begin.Value.ToString() + " ÷ ???";
                if (this.HasOnlyEnd)
                    return "??? ÷ " + this.End.Value.ToString();
                return "???";
            }
        }
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as SizeRange));
        }
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Help object: singleton empty instance, for access to base instantial methods
        /// </summary>
        protected static SizeRange Helper { get { if (((object)_Helper) == null) _Helper = new SizeRange(); return _Helper; } } private static SizeRange _Helper;
        #endregion
        #region Operators
        public static SizeRange operator *(SizeRange a, SizeRange b)
        {
            Decimal? begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new SizeRange(begin, end);
        }
        public static SizeRange operator +(SizeRange a, SizeRange b)
        {
            Decimal? begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new SizeRange(begin, end);
        }
        public static bool operator ==(SizeRange a, SizeRange b)
        {
            return Helper.IsEqual(a, b);
        }
        public static bool operator !=(SizeRange a, SizeRange b)
        {
            return !Helper.IsEqual(a, b);
        }
        #endregion
        #region Public methods - Zoom
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public SizeRange ZoomToRatio(Decimal center, double ratio)
        {
            Decimal? begin, end;
            this.PrepareZoomToRatio(center, (decimal)ratio, out begin, out end);
            return new SizeRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public SizeRange ZoomToRatio(Decimal center, decimal ratio)
        {
            Decimal? begin, end;
            this.PrepareZoomToRatio(center, ratio, out begin, out end);
            return new SizeRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is specified and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public SizeRange ZoomToSize(Decimal center, decimal size)
        {
            Decimal? begin, end;
            this.PrepareZoomToSizeOnCenterPoint(center, size, out begin, out end);
            return new SizeRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified relative position.
        /// </summary>
        /// <param name="relativePivot"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public SizeRange ZoomToSize(double relativePivot, Decimal? size)
        {
            Decimal? begin, end;
            this.PrepareZoomToSizeOnRelativePivot((decimal)relativePivot, size, out begin, out end);
            return new SizeRange(begin, end);
        }
        /// <summary>
        /// Returns a date on relative position (where 0 = Begin, 1 = End). Center of interval is on position 0.5d.
        /// When this is not filled, return null.
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public Decimal? GetValueAt(double relativePosition)
        {
            return this.GetValueAtRelativePosition((decimal)relativePosition);
        }
        #endregion
        #region Static services - Round and Equal
        /// <summary>
        /// Round specified Decimal value to nearest whole value in specified interval.
        /// In example, origin = 16518.354 round = 5.00, mode = Floor; result = 16515.000
        /// </summary>
        /// <param name="origin">Original Decimal</param>
        /// <param name="round">Round divisor (amount, to which will be original Decimal rounded)</param>
        /// <param name="mode">Round mode</param>
        /// <returns>Rounded Decimal</returns>
        public static Decimal RoundValue(Decimal origin, Decimal round, RoundMode mode)
        {
            Decimal result = origin;
            if (round > 0m)
            {
                Decimal count = origin / round;
                switch (mode)
                {
                    case RoundMode.Floor:
                        count = Math.Floor(count);
                        break;
                    case RoundMode.Math:
                        count = Math.Round(count, 0, MidpointRounding.AwayFromZero);
                        break;
                    case RoundMode.Ceiling:
                        count = Math.Ceiling(count);
                        break;
                }
                result = count * round;
            }
            return result;
        }
        /// <summary>
        /// Return true, when two instance has equal values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Equal(SizeRange a, SizeRange b)
        {
            return Helper.IsEqual(a, b);
        }
        #endregion
        #region Abstract member override
        public override bool IsEmptyEdge(decimal? value)
        {
            return (!value.HasValue);
        }
        public override bool IsEmptySize(decimal? value)
        {
            return (!value.HasValue);
        }
        public override int CompareEdge(decimal? a, decimal? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        public override int CompareSize(decimal? a, decimal? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        public override decimal? Add(decimal? begin, decimal? size)
        {
            return ((begin.HasValue && size.HasValue) ? (decimal?)(begin.Value + size.Value) : (decimal?)null);
        }
        public override decimal? SubEdge(decimal? a, decimal? b)
        {
            return ((a.HasValue && b.HasValue) ? (decimal?)(a.Value - b.Value) : (decimal?)null);
        }
        public override decimal? SubSize(decimal? a, decimal? b)
        {
            return ((a.HasValue && b.HasValue) ? (decimal?)(a.Value - b.Value) : (decimal?)null);
        }
        public override decimal? Multiply(decimal? size, decimal ratio)
        {
            return ((size.HasValue) ? (decimal?)(size.Value * ratio) : (decimal?)null);
        }
        public override decimal Divide(decimal? a, decimal? b)
        {
            return ((a.HasValue && b.HasValue && b.Value != 0m) ? (a.Value / b.Value) : 0m);
        }
        protected override string TTickToText(decimal? tick)
        {
            return (tick.HasValue ? tick.Value.ToString() : "");
        }
        protected override string TSizeToText(decimal? size)
        {
            return (size.HasValue ? size.Value.ToString() : "");
        }
        #endregion
    }
    #endregion
    #region Int32NRange = BaseRange<Int32?, Int32?>
    /// <summary>
    /// Int32NRange = rozmezí hodnot, kde Begin, End a Size jsou Int32?
    /// </summary>
    public class Int32NRange : BaseRange<Int32?, Int32?>
    {
        #region Konstruktory, vizualizace, Helper objekt
        public Int32NRange() : base() { }
        public Int32NRange(Int32? begin, Int32? end) : base(begin, end) { }
        /// <summary>
        /// Vrací new instanci obsahující prázdné hodnoty
        /// </summary>
        public static Int32NRange Empty { get { return new Int32NRange(); } }
        /// <summary>
        /// Vrací new instanci obsahující kopii aktuálních hodnot z this instance
        /// </summary>
        public Int32NRange Clone { get { return new Int32NRange(this.Begin, this.End); } }
        /// <summary>
        /// Vrací new instanci z daného počátku a délky
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static Int32NRange CreateFromBeginSize(Int32 begin, Int32 size)
        {
            return new Int32NRange(begin, begin + (Int32)size);
        }
        /// <summary>
        /// Vrací new instanci z dané délky a konce
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static Int32NRange CreateFromSizeEnd(Int32 size, Int32 end)
        {
            return new Int32NRange(end - (Int32)size, end);
        }
        /// <summary>
        /// Obsahuje textovou podobu this intervalu
        /// </summary>
        public string Text
        {
            get
            {
                if (this.IsFilled)
                    return this.Begin.Value.ToString() + " ÷ " + this.End.Value.ToString();
                if (this.HasOnlyBegin)
                    return this.Begin.Value.ToString() + " ÷ ???";
                if (this.HasOnlyEnd)
                    return "??? ÷ " + this.End.Value.ToString();
                return "???";
            }
        }
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as Int32NRange));
        }
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Help object: singleton empty instance, for access to base instantial methods
        /// </summary>
        protected static Int32NRange Helper { get { if (((object)_Helper) == null) _Helper = new Int32NRange(); return _Helper; } } private static Int32NRange _Helper;
        #endregion
        #region Operators
        public static Int32NRange operator *(Int32NRange a, Int32NRange b)
        {
            Int32? begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new Int32NRange(begin, end);
        }
        public static Int32NRange operator +(Int32NRange a, Int32NRange b)
        {
            Int32? begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new Int32NRange(begin, end);
        }
        public static bool operator ==(Int32NRange a, Int32NRange b)
        {
            return Helper.IsEqual(a, b);
        }
        public static bool operator !=(Int32NRange a, Int32NRange b)
        {
            return !Helper.IsEqual(a, b);
        }
        #endregion
        #region Public methods - Zoom
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Int32NRange ZoomToRatio(Int32 center, double ratio)
        {
            Int32? begin, end;
            this.PrepareZoomToRatio(center, (decimal)ratio, out begin, out end);
            return new Int32NRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Int32NRange ZoomToRatio(Int32 center, decimal ratio)
        {
            Int32? begin, end;
            this.PrepareZoomToRatio(center, ratio, out begin, out end);
            return new Int32NRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is specified and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public Int32NRange ZoomToSize(Int32 center, Int32 size)
        {
            Int32? begin, end;
            this.PrepareZoomToSizeOnCenterPoint(center, size, out begin, out end);
            return new Int32NRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified relative position.
        /// </summary>
        /// <param name="relativePivot"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Int32NRange ZoomToSize(double relativePivot, Int32? size)
        {
            Int32? begin, end;
            this.PrepareZoomToSizeOnRelativePivot((decimal)relativePivot, size, out begin, out end);
            return new Int32NRange(begin, end);
        }
        /// <summary>
        /// Returns a date on relative position (where 0 = Begin, 1 = End). Center of interval is on position 0.5d.
        /// When this is not filled, return null.
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public Int32? GetValueAt(double relativePosition)
        {
            return this.GetValueAtRelativePosition((decimal)relativePosition);
        }
        #endregion
        #region Static services - Round, Equal, HasIntersect, Compare
        /// <summary>
        /// Round specified Decimal value to nearest whole value in specified interval.
        /// In example, origin = 16518.354 round = 5.00, mode = Floor; result = 16515.000
        /// </summary>
        /// <param name="origin">Original Decimal</param>
        /// <param name="round">Round divisor (amount, to which will be original Decimal rounded)</param>
        /// <param name="mode">Round mode</param>
        /// <returns>Rounded Decimal</returns>
        public static Int32 RoundValue(Int32 origin, Int32 round, RoundMode mode)
        {
            Int32 result = origin;
            if (round > 0m)
            {
                Decimal count = (Decimal)origin / (Decimal)round;
                switch (mode)
                {
                    case RoundMode.Floor:
                        count = Math.Floor(count);
                        break;
                    case RoundMode.Math:
                        count = Math.Round(count, 0, MidpointRounding.AwayFromZero);
                        break;
                    case RoundMode.Ceiling:
                        count = Math.Ceiling(count);
                        break;
                }
                result = (Int32)(Math.Round(count * (Decimal)round, 0));
            }
            return result;
        }
        /// <summary>
        /// Return true, when two instance has equal values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Equal(Int32NRange a, Int32NRange b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Returns true, when two intervals have any positive intersection.
        /// Return false when one or booth intervals are negative, or when one interval is outside other.
        /// </summary>
        /// <param name="range1Begin"></param>
        /// <param name="range1End"></param>
        /// <param name="range2Begin"></param>
        /// <param name="range2End"></param>
        /// <returns></returns>
        public static bool HasIntersect(Int32? range1Begin, Int32? range1End, Int32? range2Begin, Int32? range2End)
        {
            if (Compare(range1End, range1Begin) < 0) return false;             // range1 has end < begin, there can not be any intersect
            if (Compare(range2End, range2Begin) < 0) return false;             // range2 has end < begin, there can not be any intersect
            if (Compare(range2End, range1Begin) <= 0) return false;            // range2.end is before or at range1.begin : range 2 is whole before range 1, no intersect
            if (Compare(range1End, range2Begin) <= 0) return false;            // range2.begin is after or at range1.end  : range 2 is whole after range 1, no intersect
            return true;
        }
        /// <summary>
        /// Returns true, when two intervals have any positive intersection.
        /// Return false when one or booth intervals are negative, or when one interval is outside other.
        /// </summary>
        /// <param name="range1Begin"></param>
        /// <param name="range1End"></param>
        /// <param name="range2Begin"></param>
        /// <param name="range2End"></param>
        /// <returns></returns>
        public static bool HasIntersect(Int32 range1Begin, Int32 range1End, Int32 range2Begin, Int32 range2End)
        {
            if (Compare(range1End, range1Begin) < 0) return false;             // range1 has end < begin, there can not be any intersect
            if (Compare(range2End, range2Begin) < 0) return false;             // range2 has end < begin, there can not be any intersect
            if (Compare(range2End, range1Begin) <= 0) return false;            // range2.end is before or at range1.begin : range 2 is whole before range 1, no intersect
            if (Compare(range1End, range2Begin) <= 0) return false;            // range2.begin is after or at range1.end  : range 2 is whole after range 1, no intersect
            return true;
        }
        /// <summary>
        /// Compare (a - b). 
        /// Returns -1 when a is small than b (or a is null and b is not null).
        /// Returns 0 when a is equal to b (or a is null and b is null).
        /// Returns +1 when a is greater than b (or a is not null and b is null).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(Int32? a, Int32? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        /// <summary>
        /// Compare two Int32 values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(Int32 a, Int32 b)
        {
            return a.CompareTo(b);
        }
        #endregion
        #region Abstract member override
        public override bool IsEmptyEdge(Int32? value)
        {
            return (!value.HasValue);
        }
        public override bool IsEmptySize(Int32? value)
        {
            return (!value.HasValue);
        }
        public override int CompareEdge(Int32? a, Int32? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        public override int CompareSize(Int32? a, Int32? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        public override Int32? Add(Int32? begin, Int32? size)
        {
            return ((begin.HasValue && size.HasValue) ? (Int32?)(begin.Value + size.Value) : (Int32?)null);
        }
        public override Int32? SubEdge(Int32? a, Int32? b)
        {
            return ((a.HasValue && b.HasValue) ? (Int32?)(a.Value - b.Value) : (Int32?)null);
        }
        public override Int32? SubSize(Int32? a, Int32? b)
        {
            return ((a.HasValue && b.HasValue) ? (Int32?)(a.Value - b.Value) : (Int32?)null);
        }
        public override Int32? Multiply(Int32? size, decimal ratio)
        {
            return ((size.HasValue) ? (Int32?)(Math.Round((decimal)size.Value * ratio, 0)) : (Int32?)null);
        }
        public override decimal Divide(Int32? a, Int32? b)
        {
            return ((a.HasValue && b.HasValue && b.Value != 0) ? ((decimal)a.Value / (decimal)b.Value) : 0m);
        }
        protected override string TTickToText(Int32? tick)
        {
            return (tick.HasValue ? tick.Value.ToString() : "");
        }
        protected override string TSizeToText(Int32? size)
        {
            return (size.HasValue ? size.Value.ToString() : "");
        }
        #endregion
    }
    #endregion
    #region Int32Range = BaseRange<Int32, Int32>
    /// <summary>
    /// Int32Range = rozmezí hodnot, kde Begin, End a Size jsou Int32
    /// </summary>
    public class Int32Range : BaseRange<Int32, Int32>
    {
        #region Konstruktory, vizualizace, Helper objekt
        /// <summary>
        /// Vytvoří new instanci, prázdnou
        /// </summary>
        public Int32Range() : base() { }
        /// <summary>
        /// Vytvoří new instanci, s danými hodnotami
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public Int32Range(int begin, int end) : base(begin, end) { }
        /// <summary>
        /// Vrací new instanci obsahující prázdné hodnoty
        /// </summary>
        public static Int32Range Empty { get { return new Int32Range(); } }
        /// <summary>
        /// Vrací new instanci obsahující kopii aktuálních hodnot z this instance
        /// </summary>
        public Int32Range Clone { get { return new Int32Range(this.Begin, this.End); } }
        /// <summary>
        /// Vrací new instanci z daného počátku a délky
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static Int32Range CreateFromBeginSize(Int32 begin, Int32 size)
        {
            return new Int32Range(begin, begin + (Int32)size);
        }
        /// <summary>
        /// Vrací new instanci z dané délky a konce
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static Int32Range CreateFromSizeEnd(Int32 size, Int32 end)
        {
            return new Int32Range(end - (Int32)size, end);
        }
        /// <summary>
        /// Obsahuje textovou podobu this intervalu
        /// </summary>
        public string Text
        {
            get
            {
                return this.Begin.ToString() + " ÷ " + this.End.ToString();
            }
        }
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as Int32Range));
        }
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Help object: singleton empty instance, for access to base instantial methods
        /// </summary>
        protected static Int32Range Helper { get { if (((object)_Helper) == null) _Helper = new Int32Range(); return _Helper; } } private static Int32Range _Helper;
        #endregion
        #region Operátory *   +   ==   !=
        public static Int32Range operator *(Int32Range a, Int32Range b)
        {
            Int32 begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new Int32Range(begin, end);
        }
        public static Int32Range operator +(Int32Range a, Int32Range b)
        {
            Int32 begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new Int32Range(begin, end);
        }
        public static bool operator ==(Int32Range a, Int32Range b)
        {
            return Helper.IsEqual(a, b);
        }
        public static bool operator !=(Int32Range a, Int32Range b)
        {
            return !Helper.IsEqual(a, b);
        }
        #endregion
        #region Komparátory nad rámec BaseRange
        /// <summary>
        /// Vrátí true, pokud this interval je umístěn zcela v daném rozmezí (parametr range).
        /// Tedy this je menší nebo nanejvýš stejně veliký jak range, a jeho Begin i End jsou v daném rozmezí.
        /// Pokud velikost (Size) intervalu this nebo intervalu range je menší než 0, pak vrací false.
        /// </summary>
        /// <param name="range">Rozsah, do něhož se má this interval vejít</param>
        /// <returns></returns>
        public bool IsAllWithin(Int32Range range)
        {
            if (this.Size < 0 || range == null || range.Size < 0) return false;
            return (range.Begin >= this.Begin && range.End <= this.End);
        }
        /// <summary>
        /// Vrátí true, pokud this interval je umístěn částečně v daném rozmezí (parametr range).
        /// Tedy když průnik obou intervalů má velikost větší než 0.
        /// Pokud velikost (Size) intervalu this nebo intervalu range je menší než 0, pak vrací false.
        /// </summary>
        /// <param name="range">Rozsah, se kterým se má this interval překrývat</param>
        /// <returns></returns>
        public bool IsPartlyWithin(Int32Range range)
        {
            if (this.Size < 0 || range == null || range.Size < 0) return false;
            Int32Range result = this * range;
            return (result.Size > 0);
        }
        #endregion
        #region Static services - Round, Equal, HasIntersect, Compare
        /// <summary>
        /// Round specified Decimal value to nearest whole value in specified interval.
        /// In example, origin = 16518.354 round = 5.00, mode = Floor; result = 16515.000
        /// </summary>
        /// <param name="origin">Original Decimal</param>
        /// <param name="round">Round divisor (amount, to which will be original Decimal rounded)</param>
        /// <param name="mode">Round mode</param>
        /// <returns>Rounded Decimal</returns>
        public static Int32 RoundValue(Int32 origin, Int32 round, RoundMode mode)
        {
            Int32 result = origin;
            if (round > 0m)
            {
                Decimal count = (Decimal)origin / (Decimal)round;
                switch (mode)
                {
                    case RoundMode.Floor:
                        count = Math.Floor(count);
                        break;
                    case RoundMode.Math:
                        count = Math.Round(count, 0, MidpointRounding.AwayFromZero);
                        break;
                    case RoundMode.Ceiling:
                        count = Math.Ceiling(count);
                        break;
                }
                result = (Int32)(Math.Round(count * (Decimal)round, 0));
            }
            return result;
        }
        /// <summary>
        /// Return true, when two instance has equal values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Equal(Int32Range a, Int32Range b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Returns true, when two intervals have any positive intersection.
        /// Return false when one or booth intervals are negative, or when one interval is outside other.
        /// </summary>
        /// <param name="range1Begin"></param>
        /// <param name="range1End"></param>
        /// <param name="range2Begin"></param>
        /// <param name="range2End"></param>
        /// <returns></returns>
        public static bool HasIntersect(Int32 range1Begin, Int32 range1End, Int32 range2Begin, Int32 range2End)
        {
            if (range1End < range1Begin) return false;             // range1 has end < begin, there can not be any intersect
            if (range2End < range2Begin) return false;             // range2 has end < begin, there can not be any intersect
            if (range2End <= range1Begin) return false;            // range2.end is before or at range1.begin : range 2 is whole before range 1, no intersect
            if (range1End <= range2Begin) return false;            // range2.begin is after or at range1.end  : range 2 is whole after range 1, no intersect
            return true;
        }
        #endregion
        #region GetRectangle
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Int32Range x, Int32Range y)
        {
            return new Rectangle(x.Begin, y.Begin, x.Size, y.Size);
        }
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Int32Range x, Rectangle y)
        {
            return new Rectangle(x.Begin, y.Y, x.Size, y.Height);
        }
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Rectangle x, Int32Range y)
        {
            return new Rectangle(x.X, y.Begin, x.Width, y.Size);
        }
        #endregion
        #region Implementace abstraktní třídy
        public override int Add(int begin, int size) { return begin + size; }
        public override int CompareEdge(int a, int b) { return a.CompareTo(b); }
        public override int CompareSize(int a, int b) { return a.CompareTo(b); }
        public override decimal Divide(int a, int b) { return a / b; }
        public override bool IsEmptyEdge(int value) { return false; }
        public override bool IsEmptySize(int value) { return false; }
        public override int Multiply(int size, decimal ratio) { return (int)(Math.Round((decimal)size * ratio, 0)); }
        public override int SubEdge(int end, int size) { return end - size; }
        public override int SubSize(int a, int b) { return a - b; }
        protected override string TSizeToText(int size) { return size.ToString(); }
        protected override string TTickToText(int tick) { return tick.ToString(); }
        #endregion
    }
    #endregion
}
