using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    #region XDateTime
    /// <summary>
    /// eXtended DateTime.
    /// XDateTime can hold years in range +/- 1395× age of our Universe (+/- 19 215 358 410 114 years), 
    /// with an accuracy of 10 femtoseconds.
    /// </summary>
    public struct XDateTime
    {
        #region Public
        #region Constructors, Equals, ToString, variables
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="value"></param>
        public XDateTime(DateTime value)
            : this(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, (decimal)value.Millisecond / 1000m)
        {
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public XDateTime(long year, int month, int day)
        {
            this._DayTicks = _MergeDateFromParts(year, month, day);
            this._TimeTicks = 0;
            this._HasTime = false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public XDateTime(long year, int month, int day, int hour, int minute, int second)
        {
            this._DayTicks = _MergeDateFromParts(year, month, day);
            this._TimeTicks = _MergeTimeFromParts(hour, minute, second);
            this._HasTime = true;
        }
        /// <summary>
        /// Constructor for exact date and time
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour">Whole number of hour, in range 0÷23</param>
        /// <param name="minute">Whole number of minute, in range 0÷59</param>
        /// <param name="second">Whole number of second, in range 0÷59</param>
        /// <param name="fragments">Fragment of second, in range 0 (include) to 1 (exclude, e.g. 0.999999999999...) second.</param>
        public XDateTime(long year, int month, int day, int hour, int minute, int second, decimal fragments)
        {
            this._DayTicks = _MergeDateFromParts(year, month, day);
            this._TimeTicks = _MergeTimeFromParts(hour, minute, second, fragments);
            this._HasTime = true;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dayTicks"></param>
        internal XDateTime(long dayTicks)
        {
            this._DayTicks = dayTicks;
            this._TimeTicks = 0;
            this._HasTime = false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dayTicks"></param>
        /// <param name="timeTicks"></param>
        internal XDateTime(long dayTicks, ulong timeTicks)
        {
            this._DayTicks = dayTicks;
            this._TimeTicks = timeTicks;
            this._HasTime = true;
        }
        /// <summary>
        /// HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this._DayTicks.GetHashCode() ^ this._TimeTicks.GetHashCode() ^ this._HasTime.GetHashCode();
        }
        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            XDateTime other = (XDateTime)obj;
            return this == other;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.IsStandard)
            {
                string fmt = "yyyy-MM-dd";
                if (this.HasTime)
                    fmt += " HH:mm:ss";
                return this.DateTime.ToString(fmt);
            }

            long year; int month, day;
            _SplitDateToParts(this._DayTicks, out year, out month, out day);
            string text =
                (year < 0 ? -year : year).ToString() +
                (year < 0 ? " BC" : " AC") +
                "; " + month.ToString().PadLeft(2, '0') + "-" + day.ToString().PadLeft(2, '0') +
                (this.HasTime ? "; " + this.Time.ToString() : "");
            return text;
        }
        // Variables:
        private long _DayTicks;
        private bool _HasTime;
        private ulong _TimeTicks;
        #endregion
        #region Properties
        /// <summary>
        /// Date part as Int64 value, for internal use
        /// </summary>
        internal long TicksDate
        {
            get { return this._DayTicks; }
            // set { this._DayIndex = value; }
        }
        /// <summary>
        /// Time part as UInt64 value, for internal use
        /// </summary>
        internal ulong TicksTime
        {
            get { return this._TimeTicks; }
            // set { this._TimeIndex = value; }
        }
        /// <summary>
        /// Year, in range +/- 19 215 358 410 100.
        /// Yes, this is 19 trillion (US) = 19 billion (EN) years, when the Universe is approx. 13 772 000 000 years (to today).
        /// Thus, this XDateTime can contain 1395 such Universe ages.
        /// </summary>
        public long Year
        {
            get { long year; int month, day; _SplitDateToParts(this._DayTicks, out year, out month, out day); return year; }
            // set { long year; int month, day; _SplitDateToParts(this._DayIndex, out year, out month, out day); this._DayTicks = _MergeDateFromParts(value, month, day); }
        }
        /// <summary>
        /// Month, in range 1 ÷ 12
        /// </summary>
        public int Month
        {
            get { long year; int month, day; _SplitDateToParts(this._DayTicks, out year, out month, out day); return month; }
            // set { long year; int month, day; _SplitDateToParts(this._DayIndex, out year, out month, out day); this._DayTicks = _MergeDateFromParts(year, value, day); }
        }
        /// <summary>
        /// Day, in range max 1 ÷ 31 (according to specific month and year: 1 ÷ 28)
        /// </summary>
        public int Day
        {
            get { long year; int month, day; _SplitDateToParts(this._DayTicks, out year, out month, out day); return day; }
            // set { long year; int month, day; _SplitDateToParts(this._DayIndex, out year, out month, out day); this._DayTicks = _MergeDateFromParts(year, month, value); }
        }
        /// <summary>
        /// Hour, in range max 0 ÷ 23
        /// </summary>
        public int Hour
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeTicks, out hour, out minute, out second, out fragments); return hour; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(value, minute, second, fragments); this._HasTime = true; }
        }
        /// <summary>
        /// Minute, in range max 0 ÷ 59
        /// </summary>
        public int Minute
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeTicks, out hour, out minute, out second, out fragments); return minute; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(hour, value, second, fragments); this._HasTime = true; }
        }
        /// <summary>
        /// Second, in range max 0 ÷ 59
        /// </summary>
        public int Second
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeTicks, out hour, out minute, out second, out fragments); return second; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(hour, minute, value, fragments); this._HasTime = true; }
        }
        /// <summary>
        /// Fragment of second, in range 0.000 ÷ 0.99999999...
        /// Smallest time unit is 10 femtoseconds = 0.000 000 000 000 010 second.
        /// During this time (10 femtoseconds) ray of light travels approximately 3 µm (micrometers), a distance comparable to the diameter of a 10 viruses.[wiki]
        /// One second is divisible to 100 000 000 000 000 parts, but one second is defined as 
        /// duration of 9 192 631 770 periods of the radiation corresponding to the transition between the two hyperfine levels of the ground state of the caesium 133 atom.
        /// Thus one "tick" of atomic watch (with caesium 133) has 10878 tick in this property
        /// </summary>
        public decimal SecondFragment
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeTicks, out hour, out minute, out second, out fragments); return fragments; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(hour, minute, second, value); this._HasTime = true; }
        }
        /// <summary>
        /// Has time defined?
        /// </summary>
        public bool HasTime { get { return this._HasTime; } }
        /// <summary>
        /// Contain Time with SecondFragment != 0?
        /// </summary>
        public bool HasSecondFragment { get { return this._HasTime && ((this._TimeTicks % ONESECOND) != 0L); } }
        /// <summary>
        /// true, when this instance contain DateTime in range of .NET type DateTime
        /// </summary>
        public bool IsStandard { get { return _IsNetYear(this.Year); } }
        /// <summary>
        /// Is current year a leap-year?
        /// </summary>
        public bool IsLeap { get { return IsLeapYear(this.Year); } }
        /// <summary>
        /// Gets the day of the year represented by this instance.
        /// The day of the year, expressed as a value between 1 and 366.
        /// </summary>
        public int DayOfYear { get { return this._GetDayOfYear(); } }
        /// <summary>
        /// Gets the day of the week represented by this instance.
        /// A System.DayOfWeek enumerated constant that indicates the day of the week of this System.DateTime value.
        /// </summary>
        public DayOfWeek DayOfWeek { get { return this._GetDayOfWeek(); } }
        /// <summary>
        /// New instance, containig only date part, no time.
        /// </summary>
        public XDateTime Date { get { return new XDateTime(this._DayTicks); } }
        /// <summary>
        /// New instance, containig only time part (Days = 0).
        /// </summary>
        public XTimeSpan Time { get { return new XTimeSpan(this._TimeIndexZero); } }
        /// <summary>
        /// Time index (when _HasTime), or 0L (when not _HasTime)
        /// </summary>
        private ulong _TimeIndexZero { get { return (this._HasTime ? this._TimeTicks : 0L); } }
        /// <summary>
        /// Contain standard .NET DateTime instance
        /// </summary>
        public DateTime DateTime
        {
            get
            {
                if (!this.IsStandard)
                    throw new InvalidCastException("Value in XDateTime instance is out of range to be a convertible to DateTime.");

                long year; int month, day; 
                _SplitDateToParts(this._DayTicks, out year, out month, out day);
                int hour, minute, second; 
                decimal fragments; 
                _SplitTimeToParts(this._TimeTicks, out hour, out minute, out second, out fragments);
                return new DateTime((int)year, month, day, hour, minute, second, (int)(Math.Round(fragments * 1000m, 0)));
            }
        }
        /// <summary>
        /// true, when this instance is empty (day = 0, time = none)
        /// </summary>
        public bool IsEmpty { get { return (this._DayTicks == 0L && !this._HasTime && this._TimeTicks == 0L); } }
        /// <summary>
        /// A new empty XDateTime instance, Date = 01-01-0000, without time.
        /// </summary>
        public static XDateTime Empty { get { return new XDateTime(0L); } }
        /// <summary>
        /// A new XDateTime instance, that is set to the current date and time on this computer, expressed as the local time.
        /// </summary>
        public static XDateTime Now { get { return new XDateTime(DateTime.Now); } }
        #endregion
        #endregion
        #region operators
        /// <summary>
        /// Vrací true, pokud hodnota a je rovna b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(XDateTime a, XDateTime b)
        {
            return (a._DayTicks == b._DayTicks && a._TimeIndexZero == b._TimeIndexZero);
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je jiná než b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(XDateTime a, XDateTime b)
        {
            return (a._DayTicks != b._DayTicks || a._TimeIndexZero != b._TimeIndexZero);
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je menší než b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <(XDateTime a, XDateTime b)
        {
            return (a._DayTicks < b._DayTicks || (a._DayTicks == b._DayTicks && a._TimeIndexZero < b._TimeIndexZero));
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je větší než b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >(XDateTime a, XDateTime b)
        {
            return (a._DayTicks > b._DayTicks || (a._DayTicks == b._DayTicks && a._TimeIndexZero > b._TimeIndexZero));
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je menší nebo rovna b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <=(XDateTime a, XDateTime b)
        {
            return (a._DayTicks < b._DayTicks || (a._DayTicks == b._DayTicks && a._TimeIndexZero <= b._TimeIndexZero));
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je větší nebo rovna b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >=(XDateTime a, XDateTime b)
        {
            return (a._DayTicks > b._DayTicks || (a._DayTicks == b._DayTicks && a._TimeIndexZero >= b._TimeIndexZero));
        }
        /// <summary>
        /// Vrací čas a posunutý dopředu o čas b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static XDateTime operator +(XDateTime a, XTimeSpan time)
        {
            return a.Add(time);
        }
        /// <summary>
        /// Implicit conversion from DateTime to XDateTime
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator XDateTime(DateTime value)
        {
            if (value.Millisecond != 0) return new XDateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, (decimal)value.Millisecond / 1000m);
            if (value.Hour != 0 || value.Minute != 0 || value.Second != 0) return new XDateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
            return new XDateTime(value.Year, value.Month, value.Day);
        }
        /// <summary>
        /// Implicit conversion from XDateTime to DateTime.
        /// When XDateTime is not IsStandard, then InvalidCastException is thrown.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator DateTime(XDateTime value)
        {
            if (!value.IsStandard)
                throw new InvalidCastException("Value in XDateTime instance is out of range to be a convertible to DateTime.");

            long year; int month, day;
            _SplitDateToParts(value._DayTicks, out year, out month, out day);
            if (!value.HasTime)
                return new DateTime((int)year, month, day);

            int hour, minute, second; decimal fragments;
            _SplitTimeToParts(value._TimeTicks, out hour, out minute, out second, out fragments);
            if (!value.HasSecondFragment)
                return new DateTime((int)year, month, day, hour, minute, second);

            int milisec = (int)Math.Round((fragments / 1000m), 0);
            return new DateTime((int)year, month, day, hour, minute, second, milisec);
        }
        #endregion
        #region Convertors, check
        /// <summary>
        /// Rozdělí this čas na rok, měsíc, den
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        internal void SplitToParts(out long year, out int month, out int day)
        {
            _SplitDateToParts(this._DayTicks, out year, out month, out day);
        }
        /// <summary>
        /// Rozdělí this čas na rok, měsíc, den, čas
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        internal void SplitToParts(out long year, out int month, out int day, out int hour, out int minute, out int second, out decimal fragments)
        {
            _SplitDateToParts(this._DayTicks, out year, out month, out day);
            _SplitTimeToParts(this._TimeTicks, out hour, out minute, out second, out fragments);
        }
        /// <summary>
        /// Create a long (Int64) value from date parts
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        private static long _MergeDateFromParts(long year, int month, int day)
        {
            CheckDate(year, month, day);
            // Example: day = 5, month = 8, year = 781225; result = 5 + 31 * (8 + 12 * 781225) = 5 + 31 * (8 + 9374700) = 5 + 31 * (9374708) = 5 + 290615948 = 290615953
            bool isNegative = (year < 0);
            long tickDate = (long)day + DAYS * ((long)month + MONTH * (isNegative ? -year : year));
            return (isNegative ? -tickDate : tickDate);
        }
        /// <summary>
        /// Create date parts (Int32) from one long (Int64) value
        /// </summary>
        /// <param name="dayIndex"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        private static void _SplitDateToParts(long dayIndex, out long year, out int month, out int day)
        {
            bool isNegative = (dayIndex < 0);
            long v = (isNegative ? -dayIndex : dayIndex);  // from example in method _MergeDateFromParts(): 290615953
            day = (int)(v % DAYS);               // 290615953 / 31 = 9374708,161290323 => (mod) = (0,161290323 * 31) = 5 (day)
            v = v / DAYS;                        // 290615953 / 31 = 9374708,161290323 => (int) = 9374708
            month = (int)(v % MONTH);            //   9374708 / 12 = 781225,66.. => (mod) = (0,6666 * 12) = 8 (month)
            year = (long)(v / MONTH);            //   9374708 / 12 = 781225,66.. => (int) =  781225           (year)
            if (isNegative)
                year = -year;
        }
        /// <summary>
        /// Create time parts (Int32) from one long (Int64) value
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static ulong _MergeTimeFromParts(int hour, int minute, int second)
        {
            // Day has a 86400 seconds
            // TimeIndex = (seconds * 2 00000000000000) = (max) 172800 00000000000000
            // ulong.Max = 18446744073709551615                 184467 44073709551615
            // Fragments = 00000000000000 ÷ 99999999999999 = 0.000 000 000 000 00
            //                                                 mil mic nan pik fmt
            // Smallest time fragment = 10 femtoseconds
            // Fragment is multiplied by 100000000000000 and added to (seconds * 2 00000000000000)
            CheckTime(hour, minute, second, 0m);
            ulong sec = (ulong)(second + 60 * (minute + 60 * hour));
            return sec * ONESECOND;
        }
        /// <summary>
        /// Create time parts (Int32) from one long (Int64) value
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        /// <returns></returns>
        private static ulong _MergeTimeFromParts(int hour, int minute, int second, decimal fragments)
        {
            // Day has a 86400 seconds
            // TimeIndex = (seconds * 2 00000000000000) = (max) 172800 00000000000000
            // ulong.Max = 18446744073709551615                 184467 44073709551615
            // Fragments = 00000000000000 ÷ 99999999999999 = 0.000 000 000 000 00
            //                                                 mil mic nan pik fmt
            // Smallest time fragment = 10 femtoseconds
            // Fragment is multiplied by 100000000000000 and added to (seconds * 2 00000000000000)
            CheckTime(hour, minute, second, fragments);
            ulong sec = (ulong)(second + 60 * (minute + 60 * hour));
            ulong frg = (ulong)(fragments * FRAGMENT);
            return sec * ONESECOND + frg;
        }
        /// <summary>
        /// Create a long (Int64) value from time parts
        /// </summary>
        /// <param name="timeIndex"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        private static void _SplitTimeToParts(ulong timeIndex, out int hour, out int minute, out int second, out decimal fragments)
        {
            int t = (int)(timeIndex / ONESECOND);
            second = t % 60;
            t = t / 60;
            minute = t % 60;
            hour = t / 60;
            
            decimal frg = (decimal)timeIndex % ONESECOND;
            fragments = frg / FRAGMENT;
        }
        /// <summary>
        /// Check correctness of date parts (throw an ArgumentOutOfRangeException if parts are out of valid range)
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public static void CheckDate(long year, int month, int day)
        {
            string message;
            if (IsDateValid(year, month, day, out message)) return;
            throw new ArgumentOutOfRangeException(message);
        }
        /// <summary>
        /// Detect correctness of date parts. Return true/false. Set message for non-correct values.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsDateValid(long year, int month, int day, out string message)
        {
            message = null;
            if (year < -19215358410100L || year > 19215358410100L)
            {
                message = "Value of Year = " + year.ToString() + " is out of range { -19 215 358 410 100 ÷ +19 215 358 410 100 }.";
            }
            if (month >= 1 && month <= 12)
            {
                int dayCount = GetDayCountInMonth(year, month);
                if (!(day >= 1 && day <= dayCount))
                {
                    message = "Value of Day = " + day.ToString() + " is out of range { 1 ÷ " + dayCount.ToString() + " }, for year " + year.ToString() + " and month " + month.ToString() + ".";
                }
            }
            else
            {
                message = "Value of Month = " + month.ToString() + " is out of range { 1 ÷ 12 }.";
            }
            return (message == null);
        }
        /// <summary>
        /// Check correctness of time parts (throw an ArgumentOutOfRangeException if parts are out of valid range)
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        public static void CheckTime(int hour, int minute, int second, decimal fragments)
        {
            XTimeSpan.CheckTime(hour, minute, second, fragments);
        }
        /// <summary>
        ///  Detect correctness of time parts. Return true/false. Set message for non-correct values.
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsTimeValid(int hour, int minute, int second, decimal fragments, out string message)
        {
            return XTimeSpan.IsTimeValid(hour, minute, second, fragments, out message);
        }
        /// <summary>
        /// Number of months in one year (12)
        /// </summary>
        private const long MONTH = 12L;
        /// <summary>
        /// Max number of days in one month (31)
        /// </summary>
        private const long DAYS = 31L;
        /// <summary>
        /// Number of days in year (365.24219M)
        /// </summary>
        private const decimal YEARDAYS = 365.24219M;
        /// <summary>
        /// Number of seconds in one day
        /// </summary>
        private const long DAYSEC = 86400L;
        /// <summary>
        /// Size of one second in Time unit (ulong 100000000000000L)
        /// </summary>
        private const ulong ONESECOND = 100000000000000L;
        /// <summary>
        /// Size of fragment of one second in Time unit (decimal 100000000000000M)
        /// </summary>
        private const decimal FRAGMENT = 100000000000000M;
        /// <summary>
        /// Size of one day in Time unit (=DAYSEC * ONESECOND)
        /// </summary>
        private const ulong ONEDAY = DAYSEC * ONESECOND;
        #endregion
        #region Leap year, day count in month, day count in year, day in week, day in year
        /// <summary>
        /// Returns a day count in specified year and month (28 ÷ 31).
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public static int GetDayCountInMonth(long year, int month)
        {
            switch (month)
            {
                case 1:
                case 3:
                case 5:
                case 7:
                case 8:
                case 10:
                case 12:
                    return 31;
                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;
                case 2:
                    return (IsLeapYear(year) ? 29 : 28);
            }
            return 31;
        }
        /// <summary>
        /// Returns a day count in specified year (365 ÷ 366).
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public static int GetDayCountInYear(long year)
        {
            return (IsLeapYear(year) ? 366 : 365);
        }
        /// <summary>
        /// Returns an indication whether the specified year is a leap year.
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public static bool IsLeapYear(long year)
        {
            // Leap years from .NET library:
            if (_IsNetYear(year))
                return DateTime.IsLeapYear((int)year);

            // Years outside .NET range is leap by standard today formula:
            if ((year % 4L) != 0) return false;       // not fourth year (2012, 2016, 2020, and so on) => no leap year
            if ((year % 400L) == 0) return true;      // every 400-th year is leap (2000, 2400)
            if ((year % 100L) == 0) return false;     // every 100-th year (except 400-th year) is NOT leap (1900, 2100, 2200)
            return true;                              // every fourt year is leap (after eliminating 100-th year)
        }
        /// <summary>
        /// Return a day in year for this instance (in range 1 ÷ 366)
        /// </summary>
        /// <returns></returns>
        private int _GetDayOfYear()
        {
            long year; int month, day;
            _SplitDateToParts(this._DayTicks, out year, out month, out day);
            return _GetDayOfYear(year, month, day);
        }
        /// <summary>
        /// Return a day in year for specified y-m-d values (in range 1 ÷ 366)
        /// </summary>
        /// <returns></returns>
        private static int _GetDayOfYear(long year, int month, int day)
        {
            if (_IsNetYear(year))
                return (new DateTime((int)year, month, day)).DayOfYear;
            int dayCount = 0;
            for (int m = 1; m < month; m++)
                dayCount += GetDayCountInMonth(year, m);
            return dayCount + day;
        }
        /// <summary>
        /// Return a day in week for this instance
        /// </summary>
        /// <returns></returns>
        private DayOfWeek _GetDayOfWeek()
        {
            long year; int month, day;
            _SplitDateToParts(this._DayTicks, out year, out month, out day);
            return _GetDayOfWeek(year, month, day);
        }
        /// <summary>
        /// Return a day in week for specified y-m-d values
        /// </summary>
        /// <returns></returns>
        private static DayOfWeek _GetDayOfWeek(long year, int month, int day)
        {
            if (_IsNetYear(year))
                return (new DateTime((int)year, month, day)).DayOfWeek;
            // Simplification for years : 0BC..- / 10000AC..+: all years begin with Sunday:
            int doy = _GetDayOfYear(year, month, day) - 1;
            int dow = doy % 7;
            return (DayOfWeek)dow;               // DayOfWeek: Sunday = 0, Monday = 1, ..., Saturday = 6
        }
        /// <summary>
        /// Is specified year in range for .NET type DateTime?
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        private static bool _IsNetYear(long year)
        {
            return (year >= 1L && year <= 9999L);
        }
        /// <summary>
        /// Return true when days count is in .NET range for TimeSpan value
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        private static bool _IsNetDays(long days)
        {
            return (days < Int32.MaxValue && days > Int32.MinValue);
        }
        #endregion
        #region Day shift (Add, Sub)
        /// <summary>
        /// Vrátí nový <see cref="XDateTime"/> jako výsledek this + dodaný časový rozdíl <see cref="XTimeSpan"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public XDateTime Add(XTimeSpan time)
        {
            if (this.IsStandard && time.IsStandard)
                return new XDateTime(this.DateTime.Add(time.TimeSpan));

            // Add time:
            long dayShift = time.Days;                               // Days part of XTimeSpan can be a negative value. Negative timespan (for example) = -1 days + (18 hour 30 minute) = minus 5:30
            ulong timeTicks = this.TicksTime + time.TicksTime;       // Time part is part of time, a non negative value. Numeric range for one instance of Time is sufficient for 2 days time.
            if (timeTicks >= ONEDAY)                                 // XTimeSpan.TicksTime is non-negative number (UInt64), in range to 1 day (exclude), thus timeTicks can not be a negative number.
            {   // CarryOut = When time1 + time2 >= 24 hour:
                timeTicks -= ONEDAY;
                dayShift++;
            }

            // Add days to date (days + CarryOut from Time part):
            long dayTicks = this._DayTicks;
            if (dayShift != 0L)
                dayTicks = _DateShift(dayTicks, dayShift);

            // New instance:
            return new XDateTime(dayTicks, timeTicks);
        }
        /// <summary>
        /// Vrátí nový <see cref="XDateTime"/> jako výsledek this - dodaný časový rozdíl <see cref="XTimeSpan"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public XDateTime Subtract(XTimeSpan time)
        {
            if (this.IsStandard && time.IsStandard)
                return new XDateTime(this.DateTime.Subtract(time.TimeSpan));

            return this.Add(time.Negative);
        }
        /// <summary>
        /// Shift date by specified day shift.
        /// For years in .NET range use .NET methods.
        /// For years outside, for shift lower than 400 years calculate explicit years, for greater shifts calculate year = 365,24219 days.
        /// </summary>
        /// <param name="dayTicks"></param>
        /// <param name="dayShift"></param>
        private static long _DateShift(long dayTicks, long dayShift)
        {
            long year; int month, day;
            _SplitDateToParts(dayTicks, out year, out month, out day);
            _DateShift(ref year, ref month, ref day, dayShift);
            return _MergeDateFromParts(year, month, day);
        }
        /// <summary>
        /// Shift date parts (year, month, day) by specified day shift.
        /// For years in .NET range use .NET methods.
        /// For years outside, for shift lower than 400 years calculate explicit years, for greater shifts calculate year = 365,24219 days.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="dayShift"></param>
        private static void _DateShift(ref long year, ref int month, ref int day, long dayShift)
        {
            // No shift:
            if (dayShift == 0L) return;
            
            decimal yearShift = (decimal)dayShift / YEARDAYS;
            long yt = year + (long)(Math.Ceiling(yearShift));
            if (_IsNetYear(year) && _IsNetDays(dayShift) && _IsNetYear(yt))
            {   // .NET process:
                DateTime target = (new DateTime((int)year, month, day).AddDays((int)dayShift));
                year = target.Year;
                month = target.Month;
                day = target.Day;
                return;
            }

            // Outside .NET range:
            if (dayShift > 0L)
                _DateShiftForward(ref year, ref month, ref day, dayShift);
            else
                _DateShiftBackward(ref year, ref month, ref day, -dayShift);
        }
        private static void _DateShiftForward(ref long year, ref int month, ref int day, long shift)
        {
            // First month shift:
            int monthLength = GetDayCountInMonth(year, month);
            int toNewMonth = (monthLength - day) + 1;      // Number of days to begin of new month: for date 28.3. is value = (31 - 28 + 1) = 4. Result: day + toEnd = (28 + 4) = First of next month (32).
            if (shift < toNewMonth)
            {   // Small shift (within one month):
                day += (int)shift;
                shift = 0L;
            }
            else
            {   // Make shift only to first day of next month:
                _MonthShiftForward(ref year, ref monthLength, out monthLength);
                day = 1;
                shift -= toNewMonth;
            }
            if (shift == 0L) return;

            // First year shift:
            int yearLength = GetDayCountInYear(year);
            int dayNumber = _GetDayOfYear(year, month, day);
            int toNewYear = (yearLength - dayNumber) + 1;  // Number of days to begin of new year: for date 28.12. is value = 4
            if (shift >= (long)toNewYear)
            {   // Shift to 1.1. of next year:
                year++;
                month = 1;
                day = 1;
                shift -= toNewYear;
                dayNumber = 1;
                yearLength = GetDayCountInYear(year);
            }

            // Shift by whole years:
            if (shift >= (long)toNewYear)
            {
                if (shift > ((long)Math.Round(400m * YEARDAYS, 0)))
                {   // Shift greater than 400 years = shift date aproximatelly:
                    long years = ((long)Math.Truncate((decimal)shift / YEARDAYS));          // Number of whole years from number of days remaining to shift
                    year += years;
                    long days = (long)(Math.Round((decimal)years * YEARDAYS, 0));           // Value of years as number of days
                    shift -= days;
                    yearLength = GetDayCountInYear(year);
                }
                else
                {   // Shift to 400 years = shift exact:
                    while (shift >= yearLength)
                    {
                        shift -= yearLength;
                        year++;
                        yearLength = GetDayCountInYear(year);
                    }
                }
            }

            // Shift in last year by months:
            monthLength = GetDayCountInMonth(year, month);
            while (shift >= (long)monthLength)
            {
                shift -= monthLength;
                _MonthShiftForward(ref year, ref month, out monthLength);
            }

            // Shift in last year/month by remaining day:
            if (shift > 0L)
            {
                if (shift > (long)monthLength)
                    throw new InvalidOperationException("_DateShiftForward error: final day shift (" + shift.ToString() + ") exceeded monthLength (" + monthLength.ToString() + ")");
                day += (int)shift;
                if (day > monthLength)
                    throw new InvalidOperationException("_DateShiftForward error: final day " + day.ToString() + " (include shift = " + shift.ToString() + ") exceeded monthLength (" + monthLength.ToString() + ")");
            }
        }
        private static void _MonthShiftForward(ref long year, ref int month, out int monthLength)
        {
            _MonthShiftForward(ref year, ref month);
            monthLength = GetDayCountInMonth(year, month);
        }
        private static void _MonthShiftForward(ref long year, ref int month)
        {
            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }
        }
        private static void _DateShiftBackward(ref long year, ref int month, ref int day, long shift)
        {
            // First month shift:
            if (shift < day)
            {   // Within one month:
                day = day - (int)shift;
                shift = 0L;
            }
            else
            {   // To last day of previous month:
                shift -= day;
                _MonthShiftBackward(ref year, ref month, out day);
            }
            if (shift == 0L) return;

            // First year shift:
            int dayNumber = _GetDayOfYear(year, month, day);
            if (shift >= (long)dayNumber)
            {   // Shift to 12-31 of previous year:
                year--;
                month = 12;
                day = 31;
                shift -= dayNumber;
            }

            // Shift by whole years:
            int yearLength = GetDayCountInYear(year);
            if (shift >= yearLength)
            {
                if (shift > ((long)Math.Round(400m * YEARDAYS, 0)))
                {   // Shift greater than 400 years = shift date aproximatelly:
                    long years = ((long)Math.Truncate((decimal)shift / YEARDAYS));          // Number of whole years from number of days remaining to shift
                    year -= years;
                    long days = (long)(Math.Round((decimal)years * YEARDAYS, 0));           // Value of years as number of days
                    shift -= days;
                    yearLength = GetDayCountInYear(year);
                }
                else
                {   // Shift to 400 years = shift exact:
                    while (shift >= yearLength)
                    {
                        shift -= yearLength;
                        year--;
                        yearLength = GetDayCountInYear(year);
                    }
                }
            }

            // Shift in last year by months:
            int monthLength = GetDayCountInMonth(year, month);
            while (shift >= (long)monthLength)
            {
                shift -= monthLength;
                _MonthShiftBackward(ref year, ref month, out monthLength);
                day = monthLength;
            }

            // Shift in last year/month by remaining day:
            if (shift > 0L)
            {
                if (shift > (long)monthLength)
                    throw new InvalidOperationException("_DateShiftBackward error: final day shift (" + shift.ToString() + ") exceeded monthLength (" + monthLength.ToString() + ")");
                day -= (int)shift;
                if (day <= 0)
                    throw new InvalidOperationException("_DateShiftBackward error: final day " + day.ToString() + " (include shift = " + shift.ToString() + ") exceeded first day in month (1)");
            }
        }
        private static void _MonthShiftBackward(ref long year, ref int month, out int monthLength)
        {
            _MonthShiftBackward(ref year, ref month);
            monthLength = GetDayCountInMonth(year, month);
        }
        private static void _MonthShiftBackward(ref long year, ref int month)
        {
            month--;
            if (month <= 0)
            {
                month = 12;
                year--;
            }
        }
        /// <summary>
        /// Return a day difference between date 2 and date 1 (result = (d1 - d2).Days);
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        private static long _DateDiff(XDateTime d1, XDateTime d2)
        {
            long year1, year2;
            int month1, day1, month2, day2;
            d1.SplitToParts(out year1, out month1, out day1);
            d2.SplitToParts(out year2, out month2, out day2);
            return _DateDiff(year1, month1, day1, year2, month2, day2);
        }
        /// <summary>
        /// Return a day difference between date 2 and date 1 (result = ((year1, month1, day1) - (year2, month2, day2)).Days);
        /// </summary>
        /// <param name="year1"></param>
        /// <param name="month1"></param>
        /// <param name="day1"></param>
        /// <param name="year2"></param>
        /// <param name="month2"></param>
        /// <param name="day2"></param>
        /// <returns></returns>
        private static long _DateDiff(long year1, int month1, int day1, long year2, int month2, int day2)
        {
            // No difference:
            if (year1 == year2 && month1 == month2 && day1 == day2) return 0L;

            if (_IsNetYear(year1) && _IsNetYear(year2))
            {   // .NET process:
                DateTime d1 = new DateTime((int)year1, month1, day1);
                DateTime d2 = new DateTime((int)year2, month2, day2);
                return ((TimeSpan)(d2 - d1)).Days;
            }

            // Outside .NET range:
            int n1 = _GetDayOfYear(year1, month1, day1);             // Number of day 1 in year/month1
            int n2 = _GetDayOfYear(year2, month2, day2);             // Number of day 2 in year/month2

            bool neg = (year1 < year2 || (year1 == year2 && n1 < n2));         // Negative result (d1 - d2): when d1 < d2
            long yearL = (!neg ? year2 : year1);                     // Year, low
            long yearH = (!neg ? year1 : year2);                     // Year, high
            int dayL = (!neg ? n2 : n1);                             // Day, low
            int dayH = (!neg ? n1 : n2);                             // Day, high

            long dist = -dayL;                                       // Day in year (low), negative: as base number in expression: dist = (High - Low)
            long yearD = yearH - yearL;
            if (yearD <= 400L)
            {   // For maximal distance of 400 years:
                for (long y = yearL; y < yearH; y++)
                    dist += GetDayCountInYear(y);                    // Each year from Low (include) to High (exclude), exact number of day
            }
            else
            {   // For distance above 400 years:
                dist += (long)(Math.Round((decimal)yearD * YEARDAYS, 0));      // Total number of days, approximatelly (one year = 365.24219M days)
            }
            dist += dayH;

            return (neg ? -dist : dist);                             // For negative relation (when d1 < d2): return a negative distance.
        }
        #endregion
        #region Testy
        internal static void Test()
        {
            Data.XDateTime xdaad = new Data.XDateTime(781225, 8, 5, 18, 45, 00);
            long year = xdaad.Year;
            int month = xdaad.Month;
            int day = xdaad.Day;

            Data.XDateTime date = new Data.XDateTime(12450, 3, 28, 6, 45, 0);
            int testYear = 2450; //  (date.IsLeap ? 2000 : 2001);
            Data.XTimeSpan time2 = new Data.XTimeSpan(12, 0, 0, 0);
            Data.XDateTime date2 = date.Add(time2);

            Data.XTimeSpan time3 = new Data.XTimeSpan(50, 0, 0, 0);
            Data.XDateTime date3 = date.Add(time3);
            DateTime dt3 = new DateTime(testYear, date.Month, date.Day, date.Hour, date.Minute, date.Second).Add(TimeSpan.FromDays(time3.Days));

            Data.XTimeSpan time4 = new Data.XTimeSpan(-115425, 0, 0, 0);
            Data.XDateTime date4 = date.Add(time4);
            DateTime dt4 = new DateTime(2450, date.Month, date.Day, date.Hour, date.Minute, date.Second).Add(TimeSpan.FromDays(time4.Days));

            Data.XDateTime xd = new Data.XDateTime(2014, 4, 20, 18, 45, 00);
            Data.XDateTime xt = xd.Subtract(new Data.XTimeSpan(12, 0, 0));
        }
        #endregion
    }
    #endregion
    #region XTimeSpan
    /// <summary>
    /// eXtended TimeSpan.
    /// XTimeSpan can hold years in range +/- 1395× age of our Universe (+/- 19 215 358 410 114 years), 
    /// with an accuracy of 10 femtoseconds.
    /// </summary>
    public struct XTimeSpan
    {
        #region Public
        #region Constructors, Equals, ToString, variables
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="value"></param>
        public XTimeSpan(TimeSpan value)
            : this(value.Days, value.Hours, value.Minutes, value.Seconds, (decimal)value.Milliseconds / 1000m)
        { }
        /// <summary>
        /// Constructor for exact timespan
        /// </summary>
        /// <param name="hour">Whole number of hour, in range 0÷23</param>
        /// <param name="minute">Whole number of minute, in range 0÷59</param>
        /// <param name="second">Whole number of second, in range 0÷59</param>
        public XTimeSpan(int hour, int minute, int second)
        {
            this._DayCount = 0L;
            this._TicksTime = _MergeTimeFromParts(hour, minute, second);
        }
        /// <summary>
        /// Constructor for exact timespan
        /// </summary>
        /// <param name="days">Count of day</param>
        /// <param name="hour">Whole number of hour, in range 0÷23</param>
        /// <param name="minute">Whole number of minute, in range 0÷59</param>
        /// <param name="second">Whole number of second, in range 0÷59</param>
        public XTimeSpan(long days, int hour, int minute, int second)
        {
            this._DayCount = days;
            this._TicksTime = _MergeTimeFromParts(hour, minute, second);
        }
        /// <summary>
        /// Constructor for exact timespan
        /// </summary>
        /// <param name="days">Count of day</param>
        /// <param name="hour">Whole number of hour, in range 0÷23</param>
        /// <param name="minute">Whole number of minute, in range 0÷59</param>
        /// <param name="second">Whole number of second, in range 0÷59</param>
        /// <param name="fragments">Fragment of second, in range 0 (include) to 1 (exclude, e.g. 0.999999999999...) second.</param>
        public XTimeSpan(long days, int hour, int minute, int second, decimal fragments)
        {
            this._DayCount = days;
            this._TicksTime = _MergeTimeFromParts(hour, minute, second, fragments);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="timeTicks"></param>
        internal XTimeSpan(ulong timeTicks)
        {
            this._DayCount = 0L;
            this._TicksTime = timeTicks;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="days"></param>
        /// <param name="ticksTime"></param>
        internal XTimeSpan(long days, ulong ticksTime)
        {
            this._DayCount = days;
            this._TicksTime = ticksTime;
        }
        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this._DayCount.GetHashCode() ^ this._TicksTime.GetHashCode();
        }
        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            XTimeSpan other = (XTimeSpan)obj;
            return this == other;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.IsStandard)
                return this.TimeSpan.ToString();
            return "";
        }
        // Variables:
        private long _DayCount;
        private ulong _TicksTime;
        #endregion
        #region Properties
        /// <summary>
        /// Day count, in range +/- Int64 (this is 25 * 10to12) years
        /// </summary>
        public long Days
        {
            get { return this._DayCount; }
            // set { this._DayCount = value; }
        }
        /// <summary>
        /// Time part as UInt64 value, for internal use
        /// </summary>
        internal ulong TicksTime
        {
            get { return this._TicksTime; }
            // set { this._TimeIndex = value; }
        }
        /// <summary>
        /// Hour, in range max 0 ÷ 23
        /// </summary>
        public int Hours
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TicksTime, out hour, out minute, out second, out fragments); return hour; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(value, minute, second, fragments); this._HasTime = true; }
        }
        /// <summary>
        /// Minute, in range max 0 ÷ 59
        /// </summary>
        public int Minutes
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TicksTime, out hour, out minute, out second, out fragments); return minute; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(hour, value, second, fragments); this._HasTime = true; }
        }
        /// <summary>
        /// Second, in range max 0 ÷ 59
        /// </summary>
        public int Seconds
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TicksTime, out hour, out minute, out second, out fragments); return second; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(hour, minute, value, fragments); this._HasTime = true; }
        }
        /// <summary>
        /// Fragment of second, in range 0.000 ÷ 0.99999999...
        /// Smallest time unit is 10 femtoseconds = 0.000 000 000 000 010 second.
        /// During this time (10 femtoseconds) ray of light travels approximately 3 µm (micrometers), a distance comparable to the diameter of a 10 viruses.[wiki]
        /// One second is divisible to 100 000 000 000 000 parts, but one second is defined as 
        /// duration of 9 192 631 770 periods of the radiation corresponding to the transition between the two hyperfine levels of the ground state of the caesium 133 atom.
        /// Thus one "tick" of atomic watch (with caesium 133) has 10878 tick in this property
        /// </summary>
        public decimal SecondFragment
        {
            get { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TicksTime, out hour, out minute, out second, out fragments); return fragments; }
            // set { int hour, minute, second; decimal fragments; _SplitTimeToParts(this._TimeIndex, out hour, out minute, out second, out fragments); this._TimeIndex = _MergeTimeFromParts(hour, minute, second, value); this._HasTime = true; }
        }
        /// <summary>
        /// Contains Time part with SecondFragment != 0?
        /// </summary>
        public bool HasSecondFragment { get { return ((this._TicksTime % ONESECOND) != 0L); } }
        /// <summary>
        /// true, when this instance contain TimeSpan in range of .NET type TimeSpan
        /// </summary>
        public bool IsStandard { get { return _IsNetDays(this.Days); } }
        /// <summary>
        /// Contain a new instance of XTimeSpan, whose value is the negated value of this instance.
        /// The same numeric value as this instance, but with the opposite sign.
        /// </summary>
        public XTimeSpan Negative { get { return this.Negate(); } }
        /// <summary>
        /// Contain standard .NET TimeSpan instance
        /// </summary>
        public TimeSpan TimeSpan
        {
            get
            {
                if (!this.IsStandard)
                    throw new InvalidCastException("Value in XTimeSpan instance is out of range to be a convertible to TimeSpan.");

                long days = this._DayCount;
                int hour, minute, second;
                decimal fragments;
                _SplitTimeToParts(this._TicksTime, out hour, out minute, out second, out fragments);
                return new TimeSpan((int)days, hour, minute, second, (int)(Math.Round(fragments * 1000m, 0)));
            }
        }
        /// <summary>
        /// true, when this instance contain empty time (0d, 0h)
        /// </summary>
        public bool IsEmpty { get { return (this._DayCount == 0L && this._TicksTime == 0L); } }
        /// <summary>
        /// A new empty XTimeSpan instance: Days = 0, Time = 00:00:00.000000000
        /// </summary>
        public static XTimeSpan Empty { get { return new XTimeSpan(0L, 0L); } }
        #endregion
        #endregion
        #region operators
        /// <summary>
        /// Vrací true, pokud hodnota a je rovna b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(XTimeSpan a, XTimeSpan b)
        {
            return (a._DayCount == b._DayCount && a._TicksTime == b._TicksTime);
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je jiná než b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(XTimeSpan a, XTimeSpan b)
        {
            return (a._DayCount != b._DayCount || a._TicksTime != b._TicksTime);
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je menší než b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <(XTimeSpan a, XTimeSpan b)
        {
            return (a._DayCount < b._DayCount || (a._DayCount == b._DayCount && a._TicksTime < b._TicksTime));
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je větší než b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >(XTimeSpan a, XTimeSpan b)
        {
            return (a._DayCount > b._DayCount || (a._DayCount == b._DayCount && a._TicksTime > b._TicksTime));
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je menší nebo rovna b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <=(XTimeSpan a, XTimeSpan b)
        {
            return (a._DayCount < b._DayCount || (a._DayCount == b._DayCount && a._TicksTime <= b._TicksTime));
        }
        /// <summary>
        /// Vrací true, pokud hodnota a je větší nebo rovna b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >=(XTimeSpan a, XTimeSpan b)
        {
            return (a._DayCount > b._DayCount || (a._DayCount == b._DayCount && a._TicksTime >= b._TicksTime));
        }
        /// <summary>
        /// Implicit conversion from TimeSpan to XTimeSpan
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator XTimeSpan(TimeSpan value)
        {
            if (value.Milliseconds != 0) return new XTimeSpan(value.Days, value.Hours, value.Minutes, value.Seconds, (decimal)value.Milliseconds / 1000m);
            if (value.Days != 0) return new XTimeSpan(value.Days, value.Hours, value.Minutes, value.Seconds);
            return new XTimeSpan(value.Days, value.Hours, value.Minutes, value.Seconds);
        }
        /// <summary>
        /// Implicit conversion from XTimeSpan to TimeSpan.
        /// When XTimeSpan is not IsStandard, then InvalidCastException is thrown.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator TimeSpan(XTimeSpan value)
        {
            if (!value.IsStandard)
                throw new InvalidCastException("Value in XTimeSpan instance is out of range to be a convertible to TimeSpan.");

            long days = value._DayCount;
            int hour, minute, second; decimal fragments;
            _SplitTimeToParts(value._TicksTime, out hour, out minute, out second, out fragments);
            if (!value.HasSecondFragment)
                return new TimeSpan((int)days, hour, minute, second);

            int milisec = (int)Math.Round((fragments / 1000m), 0);
            return new TimeSpan((int)days, hour, minute, second, milisec);
        }
        #endregion
        #region Convertors, check
        /// <summary>
        /// Create time parts (UInt64, Int32) from this instance
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        internal void SplitToParts(out long days, out int hour, out int minute, out int second, out decimal fragments)
        {
            days = this._DayCount;
            _SplitTimeToParts(this._TicksTime, out hour, out minute, out second, out fragments);
        }
        /// <summary>
        /// Create a long (Int64) value from time parts
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static ulong _MergeTimeFromParts(int hour, int minute, int second)
        {
            // Day has a 86400 seconds
            // TimeIndex = (seconds * 2 00000000000000) = (max) 172800 00000000000000
            // ulong.Max = 18446744073709551615                 184467 44073709551615
            // Fragments = 00000000000000 ÷ 99999999999999 = 0.000 000 000 000 00
            //                                                 mil mic nan pik fmt
            // Smallest time fragment = 10 femtoseconds
            // Fragment is multiplied by 100000000000000 and added to (seconds * 2 00000000000000)
            CheckTime(hour, minute, second, 0m);
            ulong sec = (ulong)(second + 60 * (minute + 60 * hour));
            return sec * ONESECOND;
        }
        /// <summary>
        /// Create a long (Int64) value from time parts
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        /// <returns></returns>
        private static ulong _MergeTimeFromParts(int hour, int minute, int second, decimal fragments)
        {
            // Day has a 86400 seconds
            // TimeIndex = (seconds * 2 00000000000000) = (max) 172800 00000000000000
            // ulong.Max = 18446744073709551615                 184467 44073709551615
            // Fragments = 00000000000000 ÷ 99999999999999 = 0.000 000 000 000 00
            //                                                 mil mic nan pik fmt
            // Smallest time fragment = 10 femtoseconds
            // Fragment is multiplied by 100000000000000 and added to (seconds * 2 00000000000000)
            CheckTime(hour, minute, second, fragments);
            ulong sec = (ulong)(second + 60 * (minute + 60 * hour));
            ulong frg = (ulong)(fragments * FRAGMENT);
            return sec * ONESECOND + frg;
        }
        /// <summary>
        /// Create separate time parts (Int32) from one long (Int64) value
        /// </summary>
        /// <param name="timeIndex"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        private static void _SplitTimeToParts(ulong timeIndex, out int hour, out int minute, out int second, out decimal fragments)
        {
            int t = (int)(timeIndex / ONESECOND);
            second = t % 60;
            t = t / 60;
            minute = t % 60;
            hour = t / 60;

            decimal frg = (decimal)timeIndex % ONESECOND;
            fragments = frg / FRAGMENT;
        }
        /// <summary>
        /// Number of months in one year (12)
        /// </summary>
        private const long MONTH = 12L;
        /// <summary>
        /// Max number of days in one month (31)
        /// </summary>
        private const long DAYS = 31L;
        /// <summary>
        /// Number of seconds in one day
        /// </summary>
        private const long DAYSEC = 86400L;
        /// <summary>
        /// Size of one second in Time unit (ulong 100000000000000L)
        /// </summary>
        private const ulong ONESECOND = 100000000000000L;
        /// <summary>
        /// Size of fragment of one second in Time unit (decimal 100000000000000M)
        /// </summary>
        private const decimal FRAGMENT = 100000000000000M;
        /// <summary>
        /// Size of one day in Time unit (=DAYSEC * ONESECOND)
        /// </summary>
        private const ulong ONEDAY = DAYSEC * ONESECOND;
        /// <summary>
        /// Check correctness of time parts (throw an ArgumentOutOfRangeException if parts are out of valid range)
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        public static void CheckTime(int hour, int minute, int second, decimal fragments)
        {
            string message;
            if (IsTimeValid(hour, minute, second, fragments, out message)) return;
            throw new ArgumentOutOfRangeException(message);
        }
        /// <summary>
        ///  Detect correctness of time parts. Return true/false. Set message for non-correct values.
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="fragments"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsTimeValid(int hour, int minute, int second, decimal fragments, out string message)
        {
            message = null;
            if (!(hour >= 0 && hour <= 23)) message = "Value of Hour is out of range { 0 ÷ 23 }.";
            else if (!(minute >= 0 && minute <= 59)) message = "Value of Minute is out of range { 0 ÷ 59 }.";
            else if (!(second >= 0 && second <= 59)) message = "Value of Second is out of range { 0 ÷ 59 }.";
            else if (!(fragments >= 0m && fragments < 1m)) message = "Value of Fragments is out of range { 0 ÷ 1 }.";
            return (message == null);
        }
        /// <summary>
        /// Return true when days count is in .NET range for TimeSpan value
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        private static bool _IsNetDays(long days)
        {
            return (days < Int32.MaxValue && days > Int32.MinValue);
        }
        #endregion
        #region Time shift (Add, Subtract, Negate)
        /// <summary>
        /// Vrátí nový <see cref="XTimeSpan"/> jako výsledek this + dodaný časový rozdíl <see cref="XTimeSpan"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public XTimeSpan Add(XTimeSpan time)
        {
            long days = this.Days + time.Days;
            ulong ticksTime = this.TicksTime + time.TicksTime;
            if (ticksTime >= ONEDAY)
            {
                ticksTime -= ONEDAY;
                days++;
            }
            return new XTimeSpan(days, ticksTime);
        }
        /// <summary>
        /// Vrátí nový <see cref="XTimeSpan"/> jako výsledek this - dodaný časový rozdíl <see cref="XTimeSpan"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public XTimeSpan Subtract(XTimeSpan time)
        {
            return this.Add(time.Negative);
        }
        /// <summary>
        ///  Returns a XTimeSpan whose value is the negated value of this instance.
        ///  The same numeric value as this instance, but with the opposite sign.
        /// </summary>
        /// <returns></returns>
        public XTimeSpan Negate()
        {
            if (this.IsEmpty) return Empty;
            bool zeroTime = this._TicksTime == 0L;
            ulong timeTicks = (zeroTime ? 0L : (ONEDAY - this._TicksTime));
            long dayCount = -this._DayCount;
            if (!zeroTime) dayCount--;
            return new XTimeSpan(dayCount, timeTicks);
        }
        #endregion
    }
    #endregion
}
