using Noris.LCS.Base.WorkScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    #region TimeRange = BaseRange<DateTime?, TimeSpan?>
    /// <summary>
    /// Time range (Begin, End), with DateTime? Ticks and TimeSpan? Size
    /// </summary>
    public class TimeRange : BaseRange<DateTime?, TimeSpan?>
    {
        #region Constructors, Visualiser, Helper
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TimeRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public TimeRange(DateTime? begin, DateTime? end) : base(begin, end) { }
        /// <summary>
        /// Allways returns a new instance of SizeRange, containing empty values
        /// </summary>
        public static TimeRange Empty { get { return new TimeRange(); } }
        /// <summary>
        /// Allways returns a new instance of SizeRange, containing current values from this instance
        /// </summary>
        public TimeRange Clone { get { return new TimeRange(this.Begin, this.End); } }
        /// <summary>
        /// Create interval from begin and time (size). Booth must be defined.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="size"></param>
        public static TimeRange CreateFromBeginSize(DateTime begin, TimeSpan size)
        {
            return new TimeRange(begin, begin + size);
        }
        /// <summary>
        /// Create interval from time (duration) and end. Booth must be defined.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="end"></param>
        public static TimeRange CreateFromSizeEnd(TimeSpan size, DateTime end)
        {
            return new TimeRange(end - size, end);
        }
        /// <summary>
        /// Contains a textual form of this interval
        /// </summary>
        public string Text
        {
            get
            {
                if (this.IsFilled)
                {
                    if (this.Begin.Value.Date == this.End.Value.Date)
                    {
                        return ToTextYD(this.Begin.Value) + " " + ToTextT(this.Begin.Value) + " ÷ " + ToTextT(this.End.Value);
                    }
                    else
                    {
                        return ToTextYDT(this.Begin.Value) + " ÷ " + ToTextYDT(this.End.Value);
                    }
                }
                if (this.HasOnlyBegin)
                {
                    return ToTextYDT(this.Begin.Value) + " ÷ ???";
                }
                if (this.HasOnlyEnd)
                {
                    return "??? ÷ " + ToTextYDT(this.End.Value);
                }
                return "???";
            }
        }
        /// <summary>
        /// Identity string: obsahuje kompletní opis hodnot (včetně roků až po milisekundy)
        /// </summary>
        public string Identity { get { return ToIdentity(this.Begin) + "÷" + ToIdentity(this.End); } }
        /// <summary>
        /// Vrátí string obsahující identity daného času
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToIdentity(DateTime? value)
        {
            if (value.HasValue)
                return value.Value.ToString("yyyyMMdd HHmmss.fff");
            return "NULL";
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
            return Helper.IsEqual(this, (obj as TimeRange));
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
        protected static TimeRange Helper { get { if (((object)_Helper) == null) _Helper = new TimeRange(); return _Helper; } } private static TimeRange _Helper;
        #endregion
        #region Operators
        /// <summary>
        /// Násobení dvou intervalů = výsledkem je průnik (=společný čas)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static TimeRange operator *(TimeRange a, TimeRange b)
        {
            DateTime? begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Sčítání dvou intervalů = výsledkem je souhrn (=od menšího Begin po větší End)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static TimeRange operator +(TimeRange a, TimeRange b)
        {
            DateTime? begin, end;
            Helper.PrepareUnion(a, b, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Porovnání dvou intervalů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(TimeRange a, TimeRange b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání NOT dvou intervalů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(TimeRange a, TimeRange b)
        {
            return !Helper.IsEqual(a, b);
        }
        #endregion
        #region Implicitní konverze z/na GuiTimeRange
        /// <summary>
        /// Implicitní konverze z <see cref="GuiTimeRange"/> na <see cref="TimeRange"/>.
        /// Pokud je na vstupu <see cref="GuiTimeRange"/> = null, pak na výstupu je <see cref="GId"/> == null.
        /// </summary>
        /// <param name="guiTimeRange"></param>
        public static implicit operator TimeRange(GuiTimeRange guiTimeRange) { return (guiTimeRange != null ? new TimeRange(guiTimeRange.Begin, guiTimeRange.End) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="TimeRange"/> na <see cref="GuiTimeRange"/>.
        /// Pokud je na vstupu <see cref="TimeRange"/> = null, pak na výstupu je <see cref="GuiId"/> == null.
        /// </summary>
        /// <param name="timeRange"></param>
        public static implicit operator GuiTimeRange(TimeRange timeRange) { return (timeRange != null && timeRange.IsFilled ? new GuiTimeRange(timeRange.Begin.Value, timeRange.End.Value) : null); }
        #endregion
        #region Public methods - Zoom, Shift
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public TimeRange ZoomToRatio(DateTime center, double ratio)
        {
            DateTime? begin, end;
            this.PrepareZoomToRatio(center, (decimal)ratio, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public TimeRange ZoomToRatio(DateTime center, decimal ratio)
        {
            DateTime? begin, end;
            this.PrepareZoomToRatio(center, ratio, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is specified and center of zooming is on specified date.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public TimeRange ZoomToSize(DateTime center, TimeSpan size)
        {
            DateTime? begin, end;
            this.PrepareZoomToSizeOnCenterPoint(center, size, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Returns a new instance created from current instance, which Time is (ratio * this.Time) and center of zooming is on specified relative position.
        /// </summary>
        /// <param name="relativePivot"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public TimeRange ZoomToSize(double relativePivot, TimeSpan? size)
        {
            DateTime? begin, end;
            this.PrepareZoomToSizeOnRelativePivot((decimal)relativePivot, size, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Vrátí new instanci, která vychází z dat v this, ale je o (time) posunutá.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimeRange ShiftByTime(TimeSpan time)
        {
            DateTime? begin = (this.Begin.HasValue ? (DateTime?)(this.Begin + time) : (DateTime?)null);
            DateTime? end = (this.End.HasValue ? (DateTime?)(this.End + time) : (DateTime?)null);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Returns a date on relative position (where 0 = Begin, 1 = End). Center of interval is on position 0.5d.
        /// When this is not filled, return null.
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public DateTime? GetValueAt(double relativePosition)
        {
            return this.GetValueAtRelativePosition((decimal)relativePosition);
        }
        #endregion
        #region Static services (ToText, Round and Equal)
        #region ToText
        /// <summary>
        /// Return time in format containing date with year, but without time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextYD(DateTime time)
        {
            string fmt = "d.M.yyyy";
            return time.ToString(fmt);
        }
        /// <summary>
        /// Return time in format containing date with year, and time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextYDT(DateTime time)
        {
            string fmt = "d.M.yyyy " + _GetTimeFmt(time);
            return time.ToString(fmt);
        }
        /// <summary>
        /// Return time in format containing date without year, and time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextDT(DateTime time)
        {
            string fmt = "d.M. " + _GetTimeFmt(time);
            return time.ToString(fmt);
        }
        /// <summary>
        /// Return time in format containing only time (seconds only when not zero).
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextT(DateTime time)
        {
            string fmt = _GetTimeFmt(time);
            return time.ToString(fmt);
        }
        /// <summary>
        /// Return time in format containing only time with seconds, miliseconds only when not zero.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextTS(DateTime time)
        {
            string fmt = _GetTimeFmt(time, true);
            return time.ToString(fmt);
        }
        /// <summary>
        /// Return time in format containing date without year, and without time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextD(DateTime time)
        {
            string fmt = "d.M. ";
            return time.ToString(fmt);
        }
        /// <summary>
        /// Return day in week as text in CurrentCulture, full name of day (Monday, pondělí, lundi, etc).
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTextDow(DateTime time)
        {
            return ToTextDow(time, true);
        }
        /// <summary>
        /// Return day in week as text in CurrentCulture, full name of day or abbreviated (Monday/Mon, pondělí/po, lundi/lun., etc).
        /// </summary>
        /// <param name="time"></param>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public static string ToTextDow(DateTime time, bool fullName)
        {
            return time.ToString((fullName ? "dddd" : "ddd"), System.Globalization.CultureInfo.CurrentCulture);
        }
        /// <summary>
        /// Return time format for time, dependent on existency of milliseconds and seconds.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private static string _GetTimeFmt(DateTime time)
        {
            return _GetTimeFmt(time, false);
        }
        /// <summary>
        /// Return time format for time, dependent on existency of milliseconds and seconds.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="withSeconds"></param>
        /// <returns></returns>
        private static string _GetTimeFmt(DateTime time, bool withSeconds)
        {
            if (time.Millisecond != 0) return "H:mm:ss:fff";
            if (withSeconds || time.Second != 0) return "H:mm:ss";
            return "H:mm";
        }
        #endregion
        #region Round
        /// <summary>
        /// Round specified DateTime value to nearest whole value in specified interval.
        /// In example, origin = 2014-03-20 14:56:21, round = FromMinutes(15), mode = Floor; result = 2014-03-20 14:45:00
        /// </summary>
        /// <param name="origin">Original DateTime</param>
        /// <param name="round">Round divisor (amount of time, to which will be original DateTime rounded)</param>
        /// <param name="mode">Round mode</param>
        /// <returns>Rounded DateTime</returns>
        public static DateTime RoundDateTime(DateTime origin, TimeSpan round, RoundMode mode)
        {
            DateTime result = origin;
            double rDays = round.TotalDays;
            if (round.Ticks <= 0L)
            {   // No round:
                result = origin;
            }
            else if (rDays < 1D)
            {	// Round time (hour:min:sec):
                double seconds = origin.TimeOfDay.TotalSeconds;                // Time in seconds (i.e. 17.9.2008 16:25:40 = 59140)
                seconds = _RoundToDivisor(seconds, round.TotalSeconds, mode);  // Round seconds to divisor, i.e. 15 min = 900 sec: 59140 => 59400 = 16:30:00
                result = origin.Date.AddSeconds(seconds);                      // Rounded time = Date (without Time) + rounded seconds, i.e. 17.9.2008 + 59400 sec = 16:30:00
            }
            else if (rDays < 7D)
            {	// Round date to days:
                double day = origin.TimeOfDay.TotalDays;                       // Time as day fragment (time 18:00 = 18/24 = 0.75d)
                day = _RoundToDivisor(day, 1d, mode);                          // Round day fragment (TimeOfDay) to 0 or 1, by mode
                result = origin.Date.AddDays(day);
            }
            else if (rDays <= 14D)
            {	// Round date to week (to Monday):
                DateTime monday = _GetMonday(origin);                          // Nearest lower (or equal) Monday to origin DateTime
                double dow = ((TimeSpan)(origin - monday)).TotalDays;          // Day in week as number: Monday 00:00 = 0.00; Sunday 00:00 = 6.00; Sunday 18:00 = 6.75d, and so on... (count of whole day + time in day, beginning Monday, 00:00)
                dow = _RoundToDivisor(dow, 7d, mode);                          // Value 0.0d or 7.0d
                result = monday.Date.AddDays(dow);
            }
            else if (rDays <= 31d)
            {	// Round date to first day of Month:
                DateTime first = new DateTime(origin.Year, origin.Month, 1);   // First day of specified year and month
                double day = ((TimeSpan)(origin - first)).TotalDays;           // Current day (and time) in month, as number
                double count = ((TimeSpan)(first.AddMonths(1) - first)).TotalDays;   // Count of day in specified month
                day = _RoundToDivisor(day, count, mode);                       // Value 0.0d or (count)
                result = (day == 0d ? first : first.AddMonths(1));
            }
            else if (rDays <= 92d)
            {	// Round date to first day of Quartale:
                int month = origin.Month;
                month = ((month - 1) / 3) * 3 + 1;
                DateTime first = new DateTime(origin.Year, month, 1);          // First day of specified year and quartale month
                double day = ((TimeSpan)(origin - first)).TotalDays;           // Current day (and time) in quartale, as number
                double count = ((TimeSpan)(first.AddMonths(3) - first)).TotalDays;   // Count of day in specified quartale
                day = _RoundToDivisor(day, count, mode);                       // Value 0.0d or (count)
                result = (day == 0d ? first : first.AddMonths(3));
            }
            else if (rDays <= 182d)
            {	// Round date to 1.1. or to 1.7. (half-year):
                int month = origin.Month;
                month = ((month - 1) / 6) * 6 + 1;
                DateTime first = new DateTime(origin.Year, month, 1);          // First day of specified year and half-year month
                double day = ((TimeSpan)(origin - first)).TotalDays;           // Current day (and time) in half-year, as number
                double count = ((TimeSpan)(first.AddMonths(6) - first)).TotalDays;   // Count of day in specified half-year
                day = _RoundToDivisor(day, count, mode);                       // Value 0.0d or (count)
                result = (day == 0d ? first : first.AddMonths(6));
            }
            else if (rDays <= 366d)
            {	// Round date to first day of Year:
                DateTime first = new DateTime(origin.Year, 1, 1);              // First day of specified year
                double day = ((TimeSpan)(origin - first)).TotalDays;           // Current day (and time) in month, as number
                double count = ((TimeSpan)(first.AddYears(1) - first)).TotalDays;   // Count of day in specified year
                day = _RoundToDivisor(day, count, mode);                       // Value 0.0d or (count)
                result = (day == 0d ? first : first.AddYears(1));
            }
            else
            {   // Round date to first day of n-th Years:
                DateTime first = new DateTime(origin.Year, 1, 1);              // First day of specified year
                double day = ((TimeSpan)(origin - first)).TotalDays;           // Current day (and time) in month, as number
                double count = ((TimeSpan)(first.AddYears(1) - first)).TotalDays;   // Count of day in specified year
                day = _RoundToDivisor(day, count, mode);                       // Value 0.0d or (count)
                result = (day == 0d ? first : first.AddYears(1));
            }
            return result;
        }
        /// <summary>
        /// Return new DateTime as original + timespan, with rules as RoundDateTime()
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static DateTime RoundAddTime(DateTime origin, TimeSpan timeSpan)
        {
            DateTime result = origin;
            double rDays = timeSpan.TotalDays;
            if (timeSpan.Ticks <= 0L)
            {   // No round:
                result = origin;
            }
            else if (rDays < 1D)
            {	// Round time (hour:min:sec):
                result = origin.Add(timeSpan);
            }
            else if (rDays < 7D)
            {	// Round date to days:
                result = origin.AddDays(timeSpan.Days);
            }
            else if (rDays <= 14D)
            {	// Round date to week (to Monday):
                result = origin.AddDays(7);
            }
            else if (rDays <= 31d)
            {	// Round date to first day of Month:
                result = origin.AddMonths(1);
            }
            else if (rDays <= 92d)
            {	// Round date to first day of Quartale:
                result = origin.AddMonths(3);
            }
            else if (rDays <= 182d)
            {	// Round date to first day of Half-year:
                result = origin.AddMonths(6);
            }
            else if (rDays <= 366d)
            {	// Round date to first day of Month:
                result = origin.AddYears(1);
            }
            else
            {   // Round to years:
                int years = (int)Math.Round(timeSpan.TotalDays / 366d, 0);
                result = origin.AddYears(years);
            }
            return result;
        }
        /// <summary>
        /// Round specified value to divisor, with mode.
        /// Examples:
        /// _RoundToDivisor(156, 100, Math) = 200;
        /// _RoundToDivisor(1645, 100, Ceiling) = 1700;
        /// _RoundToDivisor(1645, 300, Ceiling) = 1800;
        /// _RoundToDivisor(59140, 900, Math) = 59400     (time in seconds: 16:25:40, 15 minute, Math = 16:30:00)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="divisor"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private static double _RoundToDivisor(double value, double divisor, RoundMode mode)
        {
            double result = value;
            if (divisor > 0d)
            {
                double count = value / divisor;
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
                result = count * divisor;
            }
            return result;
        }
        /// <summary>
        /// Returns datetime, which is nearest lower (or equal) monday to specified DateTime.
        /// For example for 27.3.2014 16:30 (thursday) return 21.3.2014 00:00.
        /// For 21.3.2014 00:00 return 21.3.2014 00:00.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static DateTime _GetMonday(DateTime date)
        {
            int dow = _GetDayOfWeek(date);
            return date.Date.AddDays(-dow);
        }
        /// <summary>
        /// Return number of day in week in CZ conventions, where Monday (pondělí) is first day of week.
        /// Number is in .NET conventions, where first item is zero, thus Monday = 0, Tuesday = 1, ..., Sunday = 6.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static int _GetDayOfWeek(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday: return 0;
                case DayOfWeek.Tuesday: return 1;
                case DayOfWeek.Wednesday: return 2;
                case DayOfWeek.Thursday: return 3;
                case DayOfWeek.Friday: return 4;
                case DayOfWeek.Saturday: return 5;
                case DayOfWeek.Sunday: return 6;
            }
            return -1;
        }
        #endregion
        #region Equal
        /// <summary>
        /// Return true, when two instance has equal values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Equal(TimeRange a, TimeRange b)
        {
            return Helper.IsEqual(a, b);
        }
        #endregion
        #endregion
        #region Abstract member override
        /// <summary>
        /// Je Edge prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyEdge(DateTime? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Je Size prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptySize(TimeSpan? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Porovná Edge
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int CompareEdge(DateTime? a, DateTime? b)
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
        public override int CompareSize(TimeSpan? a, TimeSpan? b)
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
        public override DateTime? Add(DateTime? begin, TimeSpan? size)
        {
            return ((begin.HasValue && size.HasValue) ? (DateTime?)(begin.Value + size.Value) : (DateTime?)null);
        }
        /// <summary>
        /// Odečtení Size = (Edge - Edge)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override DateTime? SubEdge(DateTime? a, TimeSpan? b)
        {
            return ((a.HasValue && b.HasValue) ? (DateTime?)(a.Value - b.Value) : (DateTime?)null);
        }
        /// <summary>
        /// Odečtení Edge = (Edge - Size)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override TimeSpan? SubSize(DateTime? a, DateTime? b)
        {
            return ((a.HasValue && b.HasValue) ? (TimeSpan?)(a.Value - b.Value) : (TimeSpan?)null);
        }
        /// <summary>
        /// Násobení velikosti Size = Size * ratio
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public override TimeSpan? Multiply(TimeSpan? size, decimal ratio)
        {
            return ((size.HasValue) ? (TimeSpan?)(TimeSpan.FromSeconds(size.Value.TotalSeconds * (double)ratio)) : (TimeSpan?)null);
        }
        /// <summary>
        /// Dělení velikosti Ratio = Size / Size
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public override decimal Divide(TimeSpan? a, TimeSpan? b)
        {
            return ((a.HasValue && b.HasValue && b.Value.Ticks != 0L) ? (((decimal)a.Value.Ticks) / ((decimal)b.Value.Ticks)) : 0m);
        }
        /// <summary>
        /// Vizualizace Edge
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        protected override string TTickToText(DateTime? tick)
        {
            return (tick.HasValue ? tick.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : "");
        }
        /// <summary>
        /// Vizualizace Size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected override string TSizeToText(TimeSpan? size)
        {
            return (size.HasValue ? size.Value.ToString() : "");
        }
        #endregion
        #region Comparators
        /// <summary>
        /// Porovná dvě instance <see cref="TimeRange"/> podle hodnoty <see cref="BaseRange{TEdge, TSize}.Begin"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByBeginAsc(TimeRange a, TimeRange b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            DateTime? timeA = a.Begin;
            DateTime? timeB = b.Begin;
            if (!timeA.HasValue && !timeB.HasValue) return 0;
            if (!timeA.HasValue) return -1;
            if (!timeB.HasValue) return 1;

            return timeA.Value.CompareTo(timeB.Value);
        }
        #endregion
    }
    #endregion
    #region TimeVector
    /// <summary>
    /// Time vector (Time, direction)
    /// </summary>
    public class TimeVector : BaseVector<DateTime?>
    {
        #region Properties, constructor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TimeVector() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        public TimeVector(DateTime? point, Direction direction) : base(point, direction) { }
        /// <summary>
        /// Allways returns a new instance of TimeVector, containing empty values
        /// </summary>
        public static TimeVector Empty { get { return new TimeVector(); } }
        /// <summary>
        /// Allways returns a new instance of TimeVector, containing current values from this instance
        /// </summary>
        public TimeVector Clone { get { return new TimeVector(this.Point, this.Direction); } }
        /// <summary>
        /// Allways returns a new instance of TimeVector, containing reverse values from this instance
        /// </summary>
        public TimeVector Reverse { get { return new TimeVector(this.Point, this.Direction.Reverse()); } }
        /// <summary>
        /// Contains a textual form of this interval
        /// </summary>
        public string Text { get { return this.Point.ToString() + "; " + this.Direction.ToString(); } }
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
            return Helper.IsEqual(this, (obj as TimeVector));
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
        protected static TimeVector Helper { get { if (((object)_Helper) == null) _Helper = new TimeVector(); return _Helper; } } private static TimeVector _Helper;
        #endregion
        #region Operators
        /// <summary>
        /// Return a TimeRange between two vector, if this vector is opposite and time between veectors is positive (or zero).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static TimeRange operator *(TimeVector a, TimeVector b)
        {
            DateTime? begin, end;
            Helper.PrepareIntersect(a, b, out begin, out end);
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Porovnání (EqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(TimeVector a, TimeVector b)
        {
            return Helper.IsEqual(a, b);
        }
        /// <summary>
        /// Porovnání (NonEqualValue) dvou objektů z hlediska hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(TimeVector a, TimeVector b)
        {
            return !Helper.IsEqual(a, b);
        }
        #endregion
        #region Abstract member override
        /// <summary>
        /// Je Point prázdné?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override bool IsEmptyPoint(DateTime? value)
        {
            return (!value.HasValue);
        }
        /// <summary>
        /// Porovná dvě hodnoty Point
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int ComparePoint(DateTime? a, DateTime? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return 1;
            if (b.HasValue) return -1;
            return 0;
        }
        #endregion
    }
    #endregion
}
