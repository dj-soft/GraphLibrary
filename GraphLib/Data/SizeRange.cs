using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Data;
using System.Drawing;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Data
{
    #region DecimalNRange = BaseRange<Decimal?, Decimal?>
    /// <summary>
    /// DecimalNRange = rozmezí hodnot, kde Begin, End a Size jsou Decimal?
    /// </summary>
    public class DecimalNRange : BaseRange<Decimal?, Decimal?>
    {
        #region Constructors, Visualiser, Helper
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DecimalNRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public DecimalNRange(Decimal? begin, Decimal? end) : base(begin, end) { }
        /// <summary>
        /// Allways returns a new instance of SizeRange, containing empty values
        /// </summary>
        public static DecimalNRange Empty { get { return new DecimalNRange(); } }
        /// <summary>
        /// Allways returns a new instance of SizeRange, containing current values from this instance
        /// </summary>
        public DecimalNRange Clone { get { return new DecimalNRange(this.Begin, this.End); } }
        /// <summary>
        /// Create interval from begin and time (size). Booth must be defined.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static DecimalNRange CreateFromBeginSize(Decimal begin, Decimal size)
        {
            return new DecimalNRange(begin, begin + (decimal)size);
        }
        /// <summary>
        /// Create interval from time (duration) and end. Booth must be defined.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static DecimalNRange CreateFromSizeEnd(Decimal size, Decimal end)
        {
            return new DecimalNRange(end - (decimal)size, end);
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
        /// <summary>
        /// Override GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as DecimalNRange));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Help object: singleton empty instance, for access to base instantial methods
        /// </summary>
        protected static DecimalNRange Helper { get { if (((object)_Helper) == null) _Helper = new DecimalNRange(); return _Helper; } } private static DecimalNRange _Helper;
        #endregion
        #region Operators
        /// <summary>
        /// Průnik (Intersection) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DecimalNRange operator *(DecimalNRange a, DecimalNRange b)
        {
            Decimal? begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new DecimalNRange(begin, end);
        }
        /// <summary>
        /// Sloučení (Union) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DecimalNRange operator +(DecimalNRange a, DecimalNRange b)
        {
            Decimal? begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new DecimalNRange(begin, end);
        }
        /// <summary>
        /// Porovnání (EqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(DecimalNRange a, DecimalNRange b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání (NonEqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(DecimalNRange a, DecimalNRange b)
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
        public DecimalNRange ZoomToRatio(Decimal center, double ratio)
        {
            Decimal? begin, end;
            this.PrepareZoomToRatio(center, (decimal)ratio, out begin, out end);
            return new DecimalNRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public DecimalNRange ZoomToRatio(Decimal center, decimal ratio)
        {
            Decimal? begin, end;
            this.PrepareZoomToRatio(center, ratio, out begin, out end);
            return new DecimalNRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is specified and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public DecimalNRange ZoomToSize(Decimal center, decimal size)
        {
            Decimal? begin, end;
            this.PrepareZoomToSizeOnCenterPoint(center, size, out begin, out end);
            return new DecimalNRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified relative position.
        /// </summary>
        /// <param name="relativePivot"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public DecimalNRange ZoomToSize(double relativePivot, Decimal? size)
        {
            Decimal? begin, end;
            this.PrepareZoomToSizeOnRelativePivot((decimal)relativePivot, size, out begin, out end);
            return new DecimalNRange(begin, end);
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
        public static bool Equal(DecimalNRange a, DecimalNRange b)
        {
            return Helper.IsEqual(a, b);
        }
        #endregion
        #region Implementace abstraktní třídy
        /// <summary>
        /// Je Edge prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyEdge(decimal? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Je Size prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptySize(decimal? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Porovná Edge
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int CompareEdge(decimal? a, decimal? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        /// <summary>
        /// Porovná Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int CompareSize(decimal? a, decimal? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        /// <summary>
        /// Sečtení Edge + Size
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override decimal? Add(decimal? begin, decimal? size)
        {
            return ((begin.HasValue && size.HasValue) ? (decimal?)(begin.Value + size.Value) : (decimal?)null);
        }
        /// <summary>
        /// Odečtení Size = (Edge - Edge)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal? SubEdge(decimal? a, decimal? b)
        {
            return ((a.HasValue && b.HasValue) ? (decimal?)(a.Value - b.Value) : (decimal?)null);
        }
        /// <summary>
        /// Odečtení Edge = (Edge - Size)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal? SubSize(decimal? a, decimal? b)
        {
            return ((a.HasValue && b.HasValue) ? (decimal?)(a.Value - b.Value) : (decimal?)null);
        }
        /// <summary>
        /// Násobení velikosti Size = Size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public override decimal? Multiply(decimal? size, decimal ratio)
        {
            return ((size.HasValue) ? (decimal?)(size.Value * ratio) : (decimal?)null);
        }
        /// <summary>
        /// Dělení velikosti Ratio = Size / Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal Divide(decimal? a, decimal? b)
        {
            return ((a.HasValue && b.HasValue && b.Value != 0m) ? (a.Value / b.Value) : 0m);
        }
        /// <summary>
        /// Vizualizace Edge
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected override string TTickToText(decimal? tick)
        {
            return (tick.HasValue ? tick.Value.ToString() : "");
        }
        /// <summary>
        /// Vizualizace Size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected override string TSizeToText(decimal? size)
        {
            return (size.HasValue ? size.Value.ToString() : "");
        }
        #endregion
    }
    #endregion
    #region DecimalRange = BaseRange<Decimal, Decimal>
    /// <summary>
    /// <see cref="DecimalRange"/> = rozmezí hodnot, kde Begin, End a Size jsou Decimal
    /// </summary>
    public class DecimalRange : BaseRange<Decimal, Decimal>
    {
        #region Constructors, Visualiser, Helper
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DecimalRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public DecimalRange(Decimal begin, Decimal end) : base(begin, end) { }
        /// <summary>
        /// Allways returns a new instance of <see cref="DecimalRange"/>, containing empty values
        /// </summary>
        public static DecimalRange Empty { get { return new DecimalRange(); } }
        /// <summary>
        /// Allways returns a new instance of <see cref="DecimalRange"/>, containing current values from this instance
        /// </summary>
        public DecimalRange Clone { get { return new DecimalRange(this.Begin, this.End); } }
        /// <summary>
        /// Create interval from begin and time (size). Booth must be defined.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static DecimalRange CreateFromBeginSize(Decimal begin, Decimal size)
        {
            return new DecimalRange(begin, begin + (decimal)size);
        }
        /// <summary>
        /// Create interval from time (duration) and end. Booth must be defined.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static DecimalRange CreateFromSizeEnd(Decimal size, Decimal end)
        {
            return new DecimalRange(end - (decimal)size, end);
        }
        /// <summary>
        /// Contains a textual form of this interval
        /// </summary>
        public string Text
        {
            get
            {
                return this.Begin.ToString() + " ÷ " + this.End.ToString();
            }
        }
        /// <summary>
        /// Override GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as DecimalRange));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Help object: singleton empty instance, for access to base instantial methods
        /// </summary>
        protected static DecimalRange Helper { get { if (((object)_Helper) == null) _Helper = new DecimalRange(); return _Helper; } }
        private static DecimalRange _Helper;
        #endregion
        #region Operators
        /// <summary>
        /// Průnik (Intersection) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DecimalRange operator *(DecimalRange a, DecimalRange b)
        {
            Decimal begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new DecimalRange(begin, end);
        }
        /// <summary>
        /// Sloučení (Union) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DecimalRange operator +(DecimalRange a, DecimalRange b)
        {
            Decimal begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new DecimalRange(begin, end);
        }
        /// <summary>
        /// Porovnání (EqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(DecimalRange a, DecimalRange b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání (NonEqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(DecimalRange a, DecimalRange b)
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
        public DecimalRange ZoomToRatio(Decimal center, double ratio)
        {
            Decimal begin, end;
            this.PrepareZoomToRatio(center, (decimal)ratio, out begin, out end);
            return new DecimalRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public DecimalRange ZoomToRatio(Decimal center, decimal ratio)
        {
            Decimal begin, end;
            this.PrepareZoomToRatio(center, ratio, out begin, out end);
            return new DecimalRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is specified and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public DecimalRange ZoomToSize(Decimal center, decimal size)
        {
            Decimal begin, end;
            this.PrepareZoomToSizeOnCenterPoint(center, size, out begin, out end);
            return new DecimalRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified relative position.
        /// </summary>
        /// <param name="relativePivot"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public DecimalRange ZoomToSize(double relativePivot, Decimal size)
        {
            Decimal begin, end;
            this.PrepareZoomToSizeOnRelativePivot((decimal)relativePivot, size, out begin, out end);
            return new DecimalRange(begin, end);
        }
        /// <summary>
        /// Returns a date on relative position (where 0 = Begin, 1 = End). Center of interval is on position 0.5d.
        /// When this is not filled, return null.
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public Decimal GetValueAt(double relativePosition)
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
        public static bool Equal(DecimalRange a, DecimalRange b)
        {
            return Helper.IsEqual(a, b);
        }
        #endregion
        #region Abstract member override
        /// <summary>
        /// Je Edge prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyEdge(Decimal value)
        {
            return false;
        }
        /// <summary>
        /// Je Size prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptySize(Decimal value)
        {
            return false;
        }
        /// <summary>
        /// Porovná Edge
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int CompareEdge(Decimal a, Decimal b)
        {
            return a.CompareTo(b);
        }
        /// <summary>
        /// Porovná Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int CompareSize(Decimal a, Decimal b)
        {
            return a.CompareTo(b);
        }
        /// <summary>
        /// Sečtení Edge + Size
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override Decimal Add(Decimal begin, Decimal size)
        {
            return begin + size;
        }
        /// <summary>
        /// Odečtení Size = (Edge - Edge)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override Decimal SubEdge(Decimal a, Decimal b)
        {
            return a - b;
        }
        /// <summary>
        /// Odečtení Edge = (Edge - Size)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override Decimal SubSize(Decimal a, Decimal b)
        {
            return a - b;
        }
        /// <summary>
        /// Násobení velikosti Size = Size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public override Decimal Multiply(Decimal size, decimal ratio)
        {
            return size * ratio;
        }
        /// <summary>
        /// Dělení velikosti Ratio = Size / Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal Divide(Decimal a, Decimal b)
        {
            return ((b != 0m) ? (a / b) : 0m);
        }
        /// <summary>
        /// Vizualizace Edge
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected override string TTickToText(Decimal tick)
        {
            return tick.ToString();
        }
        /// <summary>
        /// Vizualizace Size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected override string TSizeToText(Decimal size)
        {
            return size.ToString();
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
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Int32NRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
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
        /// <summary>
        /// Override GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as Int32NRange));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Průnik (Intersection) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int32NRange operator *(Int32NRange a, Int32NRange b)
        {
            Int32? begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new Int32NRange(begin, end);
        }
        /// <summary>
        /// Sloučení (Union) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int32NRange operator +(Int32NRange a, Int32NRange b)
        {
            Int32? begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new Int32NRange(begin, end);
        }
        /// <summary>
        /// Porovnání (EqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Int32NRange a, Int32NRange b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání (NonEqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
        /// <param name="size"></param>
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
        /// <param name="size"></param>
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
        /// <summary>
        /// Je Edge prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyEdge(Int32? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Je Size prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptySize(Int32? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Porovná Edge
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int CompareEdge(Int32? a, Int32? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        /// <summary>
        /// Porovná Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int CompareSize(Int32? a, Int32? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        /// <summary>
        /// Sečtení Edge + Size
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override Int32? Add(Int32? begin, Int32? size)
        {
            return ((begin.HasValue && size.HasValue) ? (Int32?)(begin.Value + size.Value) : (Int32?)null);
        }
        /// <summary>
        /// Odečtení Size = (Edge - Edge)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override Int32? SubEdge(Int32? a, Int32? b)
        {
            return ((a.HasValue && b.HasValue) ? (Int32?)(a.Value - b.Value) : (Int32?)null);
        }
        /// <summary>
        /// Odečtení Edge = (Edge - Size)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override Int32? SubSize(Int32? a, Int32? b)
        {
            return ((a.HasValue && b.HasValue) ? (Int32?)(a.Value - b.Value) : (Int32?)null);
        }
        /// <summary>
        /// Násobení velikosti Size = Size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public override Int32? Multiply(Int32? size, decimal ratio)
        {
            return ((size.HasValue) ? (Int32?)(Math.Round((decimal)size.Value * ratio, 0)) : (Int32?)null);
        }
        /// <summary>
        /// Dělení velikosti Ratio = Size / Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal Divide(Int32? a, Int32? b)
        {
            return ((a.HasValue && b.HasValue && b.Value != 0) ? ((decimal)a.Value / (decimal)b.Value) : 0m);
        }
        /// <summary>
        /// Vizualizace Edge
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected override string TTickToText(Int32? tick)
        {
            return (tick.HasValue ? tick.Value.ToString() : "");
        }
        /// <summary>
        /// Vizualizace Size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
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
        /// Vrací new instanci z daného bodu středu a velikosti
        /// </summary>
        /// <param name="center">Souřadnice středu</param>
        /// <param name="size">Velikost intervalu</param>
        public static Int32Range CreateFromCenterSize(Int32 center, Int32 size)
        {
            int begin = center - (size / 2);
            return CreateFromBeginSize(begin, size);
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
        /// <summary>
        /// Override GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as Int32Range));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Průnik (Intersection) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int32Range operator *(Int32Range a, Int32Range b)
        {
            Int32 begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new Int32Range(begin, end);
        }
        /// <summary>
        /// Sloučení (Union) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int32Range operator +(Int32Range a, Int32Range b)
        {
            Int32 begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new Int32Range(begin, end);
        }
        /// <summary>
        /// Porovnání (EqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Int32Range a, Int32Range b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání (NonEqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
        #region Implementace abstraktní třídy
        /// <summary>
        /// Je Edge prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyEdge(int value) { return false; }
        /// <summary>
        /// Je Size prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptySize(int value) { return false; }
        /// <summary>
        /// Porovná Edge
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int CompareEdge(int a, int b) { return a.CompareTo(b); }
        /// <summary>
        /// Porovná Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int CompareSize(int a, int b) { return a.CompareTo(b); }
        /// <summary>
        /// Sečtení Edge + Size
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override int Add(int begin, int size) { return begin + size; }
        /// <summary>
        /// Odečtení Size = (Edge - Edge)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int SubEdge(int a, int b) { return a - b; }
        /// <summary>
        /// Odečtení Edge = (Edge - Size)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int SubSize(int a, int b) { return a - b; }
        /// <summary>
        /// Násobení velikosti Size = Size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public override int Multiply(int size, decimal ratio) { return (int)(Math.Round((decimal)size * ratio, 0)); }
        /// <summary>
        /// Dělení velikosti Ratio = Size / Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal Divide(int a, int b) { return a / b; }
        /// <summary>
        /// Vizualizace Edge
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected override string TTickToText(int tick) { return tick.ToString(); }
        /// <summary>
        /// Vizualizace Size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected override string TSizeToText(int size) { return size.ToString(); }
        #endregion
        #region Implicitní konverze z/na GuiTimeRange
        /// <summary>
        /// Implicitní konverze z <see cref="GuiInt32Range"/> na <see cref="Int32Range"/>.
        /// Pokud je na vstupu <see cref="GuiInt32Range"/> = null, pak na výstupu je <see cref="Int32Range"/> == null.
        /// </summary>
        /// <param name="guiInt32Range"></param>
        public static implicit operator Int32Range(GuiInt32Range guiInt32Range) { return (guiInt32Range != null ? new Int32Range(guiInt32Range.Begin, guiInt32Range.End) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="Int32Range"/> na <see cref="GuiInt32Range"/>.
        /// Pokud je na vstupu <see cref="Int32Range"/> = null, pak na výstupu je <see cref="GuiInt32Range"/> == null.
        /// </summary>
        /// <param name="int32Range"></param>
        public static implicit operator GuiInt32Range(Int32Range int32Range) { return (int32Range != null && int32Range.IsFilled ? new GuiInt32Range(int32Range.Begin, int32Range.End) : null); }
        #endregion
        #region Další služby
        /// <summary>
        /// Vrací this interval posunutý o shift, beze změny délky.
        /// </summary>
        /// <param name="shift"></param>
        /// <returns></returns>
        public Int32Range ShiftBy(Int32 shift)
        {
            return new Int32Range(this.Begin + shift, this.End + shift);
        }
        #endregion
        #region GetRectangle, FromRectangle
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x">Rozsah na ose X</param>
        /// <param name="y">Rozsah na ose </param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Int32Range x, Int32Range y)
        {
            return new Rectangle(x.Begin, y.Begin, x.Size, y.Size);
        }
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x">Rozsah na ose X</param>
        /// <param name="y">Rozsah na ose Y se převezme z tohoto Rectangle, z jeho souřadnic na ose Y</param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Int32Range x, Rectangle y)
        {
            return new Rectangle(x.Begin, y.Y, x.Size, y.Height);
        }
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x">Rozsah na ose X se převezme z tohoto Rectangle, z jeho souřadnic na ose X</param>
        /// <param name="y">Rozsah na ose </param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Rectangle x, Int32Range y)
        {
            return new Rectangle(x.X, y.Begin, x.Width, y.Size);
        }
        /// <summary>
        /// Vrací rectangle pro rozmezí hodnot X a Y
        /// </summary>
        /// <param name="x">Rozsah na ose X se převezme z tohoto Rectangle, z jeho souřadnic na ose X</param>
        /// <param name="y">Rozsah na ose Y se převezme z tohoto Rectangle, z jeho souřadnic na ose Y</param>
        /// <returns></returns>
        public static Rectangle GetRectangle(Rectangle x, Rectangle y)
        {
            return new Rectangle(x.X, y.Y, x.Width, y.Height);
        }
        /// <summary>
        /// Vrátí pozici daného Rectangle na ose X (pro orientation = Horizontal)
        /// nebo na ose Y (pro orientation = Vertical)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public static Int32Range CreateFromRectangle(Rectangle source, System.Windows.Forms.Orientation orientation)
        {
            switch (orientation)
            {
                case System.Windows.Forms.Orientation.Horizontal: return CreateFromBeginSize(source.X, source.Width);
                case System.Windows.Forms.Orientation.Vertical: return CreateFromBeginSize(source.Y, source.Height);
            }
            return Int32Range.Empty;
        }
        #endregion
    }
    #endregion
    #region DoubleRange = BaseRange<Double, Double>
    /// <summary>
    /// <see cref="DoubleRange"/> = rozmezí hodnot, kde Begin, End a Size jsou Double
    /// </summary>
    public class DoubleRange : BaseRange<Double, Double>
    {
        #region Constructors, Visualiser, Helper
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DoubleRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public DoubleRange(Double begin, Double end) : base(begin, end) { }
        /// <summary>
        /// Allways returns a new instance of <see cref="DoubleRange"/>, containing empty values
        /// </summary>
        public static DoubleRange Empty { get { return new DoubleRange(); } }
        /// <summary>
        /// Allways returns a new instance of <see cref="DoubleRange"/>, containing current values from this instance
        /// </summary>
        public DoubleRange Clone { get { return new DoubleRange(this.Begin, this.End); } }
        /// <summary>
        /// Create interval from begin and time (size). Booth must be defined.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static DoubleRange CreateFromBeginSize(Double begin, Double size)
        {
            return new DoubleRange(begin, begin + size);
        }
        /// <summary>
        /// Create interval from time (duration) and end. Booth must be defined.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static DoubleRange CreateFromSizeEnd(Double size, Double end)
        {
            return new DoubleRange(end - size, end);
        }
        /// <summary>
        /// Contains a textual form of this interval
        /// </summary>
        public string Text
        {
            get
            {
                return this.Begin.ToString() + " ÷ " + this.End.ToString();
            }
        }
        /// <summary>
        /// Override GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.HashCode;
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Helper.IsEqual(this, (obj as DoubleRange));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Help object: singleton empty instance, for access to base instantial methods
        /// </summary>
        protected static DoubleRange Helper { get { if (((object)_Helper) == null) _Helper = new DoubleRange(); return _Helper; } } private static DoubleRange _Helper;
        #endregion
        #region Operators
        /// <summary>
        /// Průnik (Intersection) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DoubleRange operator *(DoubleRange a, DoubleRange b)
        {
            Double begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new DoubleRange(begin, end);
        }
        /// <summary>
        /// Sloučení (Union) hodnot ze dvou objektů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DoubleRange operator +(DoubleRange a, DoubleRange b)
        {
            Double begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new DoubleRange(begin, end);
        }
        /// <summary>
        /// Porovnání (EqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(DoubleRange a, DoubleRange b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání (NonEqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(DoubleRange a, DoubleRange b)
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
        public DoubleRange ZoomToRatio(Double center, double ratio)
        {
            Double begin, end;
            this.PrepareZoomToRatio(center, (decimal)ratio, out begin, out end);
            return new DoubleRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public DoubleRange ZoomToRatio(Double center, decimal ratio)
        {
            Double begin, end;
            this.PrepareZoomToRatio(center, ratio, out begin, out end);
            return new DoubleRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is specified and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public DoubleRange ZoomToSize(Double center, Double size)
        {
            Double begin, end;
            this.PrepareZoomToSizeOnCenterPoint(center, size, out begin, out end);
            return new DoubleRange(begin, end);
        }
        /// <summary>
        /// Returns a date on relative position (where 0 = Begin, 1 = End). Center of interval is on position 0.5d.
        /// When this is not filled, return null.
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public Double GetValueAt(double relativePosition)
        {
            return this.GetValueAtRelativePosition((decimal)relativePosition);
        }
        #endregion
        #region Static services - Round and Equal
        /// <summary>
        /// Round specified Double value to nearest whole value in specified interval.
        /// In example, origin = 16518.354 round = 5.00, mode = Floor; result = 16515.000
        /// </summary>
        /// <param name="origin">Original Double</param>
        /// <param name="round">Round divisor (amount, to which will be original Double rounded)</param>
        /// <param name="mode">Round mode</param>
        /// <returns>Rounded Double</returns>
        public static Double RoundValue(Double origin, Double round, RoundMode mode)
        {
            Double result = origin;
            if (round > 0d)
            {
                Double count = origin / round;
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
        public static bool Equal(DoubleRange a, DoubleRange b)
        {
            return Helper.IsEqual(a, b);
        }
        #endregion
        #region Abstract member override
        /// <summary>
        /// Je Edge prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyEdge(Double value)
        {
            return false;
        }
        /// <summary>
        /// Je Size prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptySize(Double value)
        {
            return false;
        }
        /// <summary>
        /// Porovná Edge
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int CompareEdge(Double a, Double b)
        {
            return a.CompareTo(b);
        }
        /// <summary>
        /// Porovná Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override int CompareSize(Double a, Double b)
        {
            return a.CompareTo(b);
        }
        /// <summary>
        /// Sečtení Edge + Size
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override Double Add(Double begin, Double size)
        {
            return begin + size;
        }
        /// <summary>
        /// Odečtení Size = (Edge - Edge)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override Double SubEdge(Double a, Double b)
        {
            return a - b;
        }
        /// <summary>
        /// Odečtení Edge = (Edge - Size)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override Double SubSize(Double a, Double b)
        {
            return a - b;
        }
        /// <summary>
        /// Násobení velikosti Size = Size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public override Double Multiply(Double size, decimal ratio)
        {
            return size * (double)ratio;
        }
        /// <summary>
        /// Dělení velikosti Ratio = Size / Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal Divide(Double a, Double b)
        {
            return ((b != 0d) ? (decimal)(a / b) : 0m);
        }
        /// <summary>
        /// Vizualizace Edge
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected override string TTickToText(Double tick)
        {
            return tick.ToString();
        }
        /// <summary>
        /// Vizualizace Size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected override string TSizeToText(Double size)
        {
            return size.ToString();
        }
        #endregion
        #region Implicitní konverze z/na GuiTimeRange
        /// <summary>
        /// Implicitní konverze z <see cref="GuiDoubleRange"/> na <see cref="DoubleRange"/>.
        /// Pokud je na vstupu <see cref="GuiDoubleRange"/> = null, pak na výstupu je <see cref="DoubleRange"/> == null.
        /// </summary>
        /// <param name="guiSingleRange"></param>
        public static implicit operator DoubleRange(GuiDoubleRange guiSingleRange) { return (guiSingleRange != null ? new DoubleRange(guiSingleRange.Begin, guiSingleRange.End) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="DoubleRange"/> na <see cref="GuiDoubleRange"/>.
        /// Pokud je na vstupu <see cref="DoubleRange"/> = null, pak na výstupu je <see cref="GuiDoubleRange"/> == null.
        /// </summary>
        /// <param name="doubleRange"></param>
        public static implicit operator GuiDoubleRange(DoubleRange doubleRange) { return (doubleRange != null && doubleRange.IsFilled ? new GuiDoubleRange(doubleRange.Begin, doubleRange.End) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="GuiSingleRange"/> na <see cref="DoubleRange"/>.
        /// Pokud je na vstupu <see cref="GuiSingleRange"/> = null, pak na výstupu je <see cref="DoubleRange"/> == null.
        /// </summary>
        /// <param name="guiSingleRange"></param>
        public static implicit operator DoubleRange(GuiSingleRange guiSingleRange) { return (guiSingleRange != null ? new DoubleRange(guiSingleRange.Begin, guiSingleRange.End) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="DoubleRange"/> na <see cref="GuiSingleRange"/>.
        /// Pokud je na vstupu <see cref="DoubleRange"/> = null, pak na výstupu je <see cref="GuiSingleRange"/> == null.
        /// </summary>
        /// <param name="doubleRange"></param>
        public static implicit operator GuiSingleRange(DoubleRange doubleRange) { return (doubleRange != null && doubleRange.IsFilled ? new GuiSingleRange((float)(doubleRange.Begin), (float)(doubleRange.End)) : null); }
        #endregion
        #region Další služby
        /// <summary>
        /// Vrací this interval posunutý o shift, beze změny délky.
        /// </summary>
        /// <param name="shift"></param>
        /// <returns></returns>
        public DoubleRange ShiftBy(Double shift)
        {
            return new DoubleRange(this.Begin + shift, this.End + shift);
        }
        /// <summary>
        /// Obsahuje <see cref="Int32Range"/>, vytvořený ze zaokrouhleného počátku a zaokrouhleného konce.
        /// </summary>
        public Int32Range Int32RoundEnd { get { return new Int32Range(Round(this.Begin), Round(this.End)); } }
        /// <summary>
        /// Obsahuje <see cref="Int32Range"/>, vytvořený ze zaokrouhleného počátku a zaokrouhlené velikosti.
        /// </summary>
        public Int32Range Int32RoundSize { get { return Int32Range.CreateFromBeginSize(Round(this.Begin), Round(this.Size)); } }
        /// <summary>
        /// Zaokrouhlí hodnotu na Int32
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static Int32 Round(Double value)
        {
            return (Int32)(Math.Round(value, 0));
        }
        #endregion
    }
    #endregion
}
