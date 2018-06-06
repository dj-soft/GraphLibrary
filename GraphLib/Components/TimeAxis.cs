using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GTimeAxis : Interactive object for TimeAxis control
    /// <summary>
    /// GTimeAxis : Interactive object for TimeAxis control
    /// </summary>
    public class GTimeAxis
        : GBaseAxis<DateTime?, TimeSpan?, TimeRange>, ITimeConvertor
    {
        #region Konstruktory, Obecné overrides osy
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GTimeAxis(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTimeAxis() : base() { }
        /// <summary>
        /// Initial value for new axis
        /// </summary>
        protected override TimeRange InitialValue
        {
            get { DateTime now = DateTime.Now; return new TimeRange(now.Date, now.Date.AddDays(1d)); }
        }
        /// <summary>
        /// Returns a new instance of TValue for specified begin and end of interval.
        /// </summary>
        /// <param name="begin">Value of Begin interval</param>
        /// <param name="end">Value of End interval</param>
        /// <returns></returns>
        protected override TimeRange GetValue(DateTime? begin, DateTime? end)
        {
            return new TimeRange(begin, end);
        }
        /// <summary>
        /// Returns a decimal number of units for specified interval.
        /// This is reverse method for GetAxisSize().
        /// For example: 
        /// on SizeAxis (in milimeters = Decimal, Decimal) returns (decimal)interval.
        /// on TimeAxis (in time = DateTime, TimeSpan) returns (decimal)interval.TotalSeconds.
        /// And so on...
        /// </summary>
        /// <param name="interval">Size of interval, which number of units must be returned</param>
        /// <returns></returns>
        protected override decimal? GetAxisUnits(TimeSpan? interval)
        {
            return (interval.HasValue ? (decimal?)interval.Value.TotalSeconds : (decimal?)null);
        }
        /// <summary>
        /// Returns a TSize value, corresponding to specified units.
        /// This is reverse method for GetAxisUnits().
        /// For example: 
        /// on SizeAxis (in milimeters = Decimal, Decimal) returns (decimal)interval.
        /// on TimeAxis (in time = DateTime, TimeSpan) returns TimeSpan.FromSeconds((double)units).
        /// And so on...
        /// </summary>
        /// <param name="units">Number of units, from which must be returned an Size of interval</param>
        /// <returns></returns>
        protected override TimeSpan? GetAxisSize(decimal units)
        {
            return TimeSpan.FromSeconds((double)units);
        }
        /// <summary>
        /// Returns a string representation of value of Tick, using string Format from ArrangementItem for this Tick.
        /// Typically return tick.ToString(format), for real TTick type.
        /// </summary>
        /// <param name="tick">Value of Tick</param>
        /// <param name="format">Format string on ArrangementItem of this Tick</param>
        /// <returns></returns>
        protected override string GetTickText(DateTime? tick, string format)
        {
            return (tick.HasValue ? tick.Value.ToString(format) : "");
        }
        /// <summary>
        /// Returns specified value (value on axis, TTick: for example DateTime) rounded to an interval (TSize: for example TimeSpan) with RoundMode.
        /// For example on TimeAxis: when value is 15.2.2016 14:35:16.165; and interval is 00:15:00.000, then result RoundValue is 15.2.2016 14:30:00 (for RoundMode = Math).
        /// </summary>
        /// <param name="value">Value (Tick) for round</param>
        /// <param name="interval">Interval on which will be Tick rounded</param>
        /// <param name="roundMode">Mode for round</param>
        /// <returns></returns>
        protected override DateTime? RoundTickToInterval(DateTime? value, TimeSpan? interval, RoundMode roundMode)
        {
            return (value.HasValue && interval.HasValue ? (DateTime?)TimeRange.RoundDateTime(value.Value, interval.Value, roundMode) : (DateTime?)null);
        }
        /// <summary>
        /// Explicitly round tick of one TickType of one Arrangement to real value on axis.
        /// Descendant can apply an logical rounding for specified tick (for example, when intervalSize is 2 days, then rounding is to day of month: 1,3,5,7,9... and not to 2,4,6,8...
        /// Base class returns tick without a rounding.
        /// </summary>
        /// <param name="tick">Tick, already rounded from method RoundTickToInterval()</param>
        /// <param name="arrangementOne">Current arrangement of axis (=definition for all ticks type)</param>
        /// <param name="axisTickType">Current tick type on Current arrangement</param>
        /// <param name="axisValue">Value on whole axis</param>
        /// <param name="intervalSize">Size of interval on current arrangement for current tick type</param>
        /// <returns></returns>
        protected override DateTime? RoundTickToLine(DateTime? tick, GBaseAxis<DateTime?, TimeSpan?, TimeRange>.ArrangementOne arrangementOne, AxisTickType axisTickType, TimeRange axisValue, TimeSpan? intervalSize)
        {
            if (arrangementOne.BigLabelItem.Interval.Value.TotalDays == 2d && axisTickType == AxisTickType.BigLabel)
            {
                int dayInMonth = tick.Value.Day;
                if ((dayInMonth % 2) == 0)
                    tick = tick.Value.AddDays(1d);
            }
            return tick;
        }
        #endregion
        #region Příprava měřítek pro časovou osu
        /// <summary>
        /// Axis class here declared items (ArrangementOne) for use in Axis.
        /// Each individual ArrangementOne contains specification for range of scale on axis, declare distance of axis ticks by tick types (pixel, small, standard, big...) 
        /// and contains format strings for ticks.
        /// Each ArrangementOne must contain several ArrangementItem, each Item for one Tick type.
        /// Many of ArrangementOne is stored in one ArrangementSet (this.Arrangement).
        /// In one time is active only one ArrangementOne (with few ArrangementItem for axis ticks). This one ArrangementOne is valid for small range of Scale.
        /// As the Scale is changed, then ArrangementSet select other ArrangementOne, appropriate for new Scale (containing other definition of its Items), 
        /// and this behavior accomodate Axis visual representation (ticks, labels) to changed Scale.
        /// When Scale is not changed, only change Begin (or End) of Value, then previous selected ArrangementOne is not changed.
        /// <para></para>
        /// InitAxisArrangement() method declare many of ArrangementOne, each with definiton for AxisTickType, and send this ArrangementOne to AddArrangementOne() method, for example:
        /// </summary>
        protected override void InitAxisArrangement()
        {
            // Format string:
            string dmyyhm = "d.M.yyyy H:mm";
         // string my = "M/yy";
         // string y = "yy";
            string yy = "yyyy";
            string dmyy = "d.M.yyyy";
            string dm = "d.M.";
            string dmh = "d.M. H:mm";
            string hms = "H:mm:ss";
            string hm = "H:mm";
            string ms = "m:ss";
            string msf = "m:ss.f";
         // string msff = "m:ss.ff";
            string msfff = "m:ss.fff";
            string sf = "s.f";
            string sff = "s.ff";
            string sfff = "s.fff";

            // TimeSpan specified in order (as unit on standard ruler) for:
            //                                                   pixel,                          milimeters,                     5milimeters,                    centimeter,                         10centimeter:
            //                                          pixelTickSize                   regularTickSize                 significantTickSize             subTitleSize          subTitleFormat  titleSize                titleFormat initialFormat axisCycle
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.Zero,                  TimeSpan.Zero,                  TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(2),   sfff, TimeSpan.FromMilliseconds(10), msfff, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.Zero,                  TimeSpan.Zero,                  TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(5),   sfff, TimeSpan.FromMilliseconds(10), msfff, dmyyhm, AxisCycle_Day, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(5),   TimeSpan.FromMilliseconds(10),  sff, TimeSpan.FromMilliseconds(100), msf, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(2),   TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(20),  sff, TimeSpan.FromMilliseconds(100), msf, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(5),   TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(50),  sff, TimeSpan.FromMilliseconds(100), msf, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(1),   TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(50),  TimeSpan.FromMilliseconds(100), sf, TimeSpan.FromSeconds(1), hms, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(20),  TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200), sf, TimeSpan.FromSeconds(1), hms, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(50),  TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), sf, TimeSpan.FromSeconds(1), hms, dmyyhm, AxisCycle_Day, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1),        ms, TimeSpan.FromSeconds(10), hms, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(2),        ms, TimeSpan.FromSeconds(10), hms, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(10),  TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(5),        ms, TimeSpan.FromSeconds(30), hms, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(5),        TimeSpan.FromSeconds(10),       ms, TimeSpan.FromMinutes(1), hm, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2),        TimeSpan.FromSeconds(10),       TimeSpan.FromSeconds(15),       ms, TimeSpan.FromMinutes(1), hm, dmyyhm, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5),        TimeSpan.FromSeconds(10),       TimeSpan.FromSeconds(30),       ms, TimeSpan.FromMinutes(2), hm, dmyyhm, AxisCycle_Day, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5),        TimeSpan.FromSeconds(15),       TimeSpan.FromMinutes(1),        hm, TimeSpan.FromMinutes(5), hm, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(10),       TimeSpan.FromMinutes(1),        TimeSpan.FromMinutes(2),        hm, TimeSpan.FromMinutes(10), hm, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(30),       TimeSpan.FromMinutes(1),        TimeSpan.FromMinutes(5),        hm, TimeSpan.FromMinutes(10), hm, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(60),       TimeSpan.FromMinutes(5),        TimeSpan.FromMinutes(10),       hm, TimeSpan.FromHours(1), hm, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(10),       TimeSpan.FromMinutes(1),        TimeSpan.FromMinutes(5),        TimeSpan.FromMinutes(15),       hm, TimeSpan.FromHours(1), hm, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(10),       TimeSpan.FromMinutes(5),        TimeSpan.FromMinutes(10),       TimeSpan.FromMinutes(30),       hm, TimeSpan.FromHours(1), hm, dmyy, AxisCycle_Day, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(10),       TimeSpan.FromMinutes(5),        TimeSpan.FromMinutes(15),       TimeSpan.FromHours(1),          hm, TimeSpan.FromHours(6), dmh, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromSeconds(10),       TimeSpan.FromMinutes(10),       TimeSpan.FromMinutes(30),       TimeSpan.FromHours(2),          hm, TimeSpan.FromHours(12), dmh, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMinutes(1),        TimeSpan.FromMinutes(15),       TimeSpan.FromMinutes(60),       TimeSpan.FromHours(3),          hm, TimeSpan.FromDays(1), dmyy, dmyy, AxisCycle_Day, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMinutes(2),        TimeSpan.FromHours(1),          TimeSpan.FromHours(3),          TimeSpan.FromHours(6),          hm, TimeSpan.FromDays(1), dmyy, dmyy, AxisCycle_Day, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMinutes(5),        TimeSpan.FromHours(1),          TimeSpan.FromHours(6),          TimeSpan.FromHours(12),         dmh, TimeSpan.FromDays(2), dmyy, dmyy, AxisCycle_Week, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMinutes(10),       TimeSpan.FromHours(2),          TimeSpan.FromHours(12),         TimeSpan.FromDays(1),           dm, TimeSpan.FromDays(7), dmyy, dmyy, AxisCycle_Week, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMinutes(15),       TimeSpan.FromHours(3),          TimeSpan.FromHours(24),         TimeSpan.FromDays(2),           dm, TimeSpan.FromDays(14), dmyy, dmyy, AxisCycle_Month, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromMinutes(30),       TimeSpan.FromHours(12),         TimeSpan.FromDays(1),           TimeSpan.FromDays(7),           dm, TimeSpan.FromDays(31), dmyy, dmyy, AxisCycle_Month, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromHours(1),          TimeSpan.FromHours(12),         TimeSpan.FromDays(2),           TimeSpan.FromDays(14),          dm, TimeSpan.FromDays(31), dmyy, dmyy, AxisCycle_Month, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromHours(3),          TimeSpan.FromHours(84),         TimeSpan.FromDays(7),           TimeSpan.FromDays(31),          dm, TimeSpan.FromDays(60), dmyy, dmyy, AxisCycle_Month, this));

            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromHours(12),         TimeSpan.FromDays(7),           TimeSpan.FromDays(31),          TimeSpan.FromDays(92),          dm, TimeSpan.FromDays(180), dmyy, dmyy, AxisCycle_Month, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromDays(2),           TimeSpan.FromDays(31),          TimeSpan.FromDays(91),          TimeSpan.FromDays(183),         dm, TimeSpan.FromDays(366), dmyy, dmyy, AxisCycle_Month, this));
            this.Arrangements.AddOne(new ArrangementOne(TimeSpan.FromDays(7),           TimeSpan.FromDays(91),          TimeSpan.FromDays(183),         TimeSpan.FromDays(366),         yy, TimeSpan.FromDays(732), dmyy, dmyy, AxisCycle_Month, this));
        }
        protected const string AxisCycle_Day = "AxisCycle.Day";
        protected const string AxisCycle_Week = "AxisCycle.Week";
        protected const string AxisCycle_Month = "AxisCycle.Month";
        #endregion
        #region Tooltip pro časovou osu
        /// <summary>
        /// Prepare layout for Tooltip in case, when ToolTip will be showed.
        /// Is called after e.ToolTipData.InfoText is prepared (contain valid text).
        /// </summary>
        /// <param name="e"></param>
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            base.PrepareToolTip(e);
            e.ToolTipData.TitleText = "Time informations";

            switch (this.AxisState)
            {
                case AxisInteractiveState.MouseOver:
                    e.ToolTipData.Icon = IconStandard.ViewPimCalendar32;
                    break;
                case AxisInteractiveState.DragMove:
                    e.ToolTipData.Icon = IconStandard.ObjectFlipHorizontal32;
                    break;
                case AxisInteractiveState.DragZoom:
                    e.ToolTipData.Icon = IconStandard.ZoomFitBest32;
                    break;
                default:
                    e.ToolTipData.Icon = IconStandard.ViewPimCalendar32;
                    break;
            }
        }
        protected override string PrepareToolTipText(DateTime? value)
        {
            if (!value.HasValue) return "";
            string text = value.Value.ToString("dddd") + Environment.NewLine +
                          value.Value.ToString();
            return text;
        }
        #endregion
        #region ITimeConvertor members + jejich obsluha
        string ITimeConvertor.Identity
        { 
            get 
            {
                string value = ToIdentity(this.Value.Begin) + "÷" + ToIdentity(this.Value.End);
                string size = ((int)this.PixelSize).ToString();
                return value + "; " + size;
            }
        }
        TimeRange ITimeConvertor.VisibleTime { get { return this.Value; } }
        VisualTick[] ITimeConvertor.Ticks { get { return this.TickList.ToArray(); } }
        int ITimeConvertor.GetPixel(DateTime? time)
        {
            int begin = (int)this.PixelFirst;
            int? pixel = this.CalculatePixelLocalForTick(time);
            return begin + (pixel.HasValue ? pixel.Value : 0);
        }
        event GPropertyChangedHandler<TimeRange> ITimeConvertor.VisibleTimeChanged { add { this._VisibleTimeChanged += value; } remove { this._VisibleTimeChanged -= value; } }
        protected static string ToIdentity(DateTime? value)
        {
            if (value.HasValue)
                return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return "NULL";
        }
        /// <summary>
        /// Je voláno v průběhu změny hodnoty <see cref="GBaseAxis{TTick, TSize, TValue}.Value"/>
        /// </summary>
        /// <param name="args"></param>
        protected override void OnValueChanging(GPropertyChangeArgs<TimeRange> args)
        {
            base.OnValueChanging(args);
            this.CallVisibleTimeChanged(args);
        }
        /// <summary>
        /// Je voláno po změně hodnoty <see cref="GBaseAxis{TTick, TSize, TValue}.Value"/>
        /// </summary>
        /// <param name="args"></param>
        protected override void OnValueChanged(GPropertyChangeArgs<TimeRange> args)
        {
            base.OnValueChanged(args);
            this.CallVisibleTimeChanged(args);
        }
        /// <summary>
        /// Vyvolá event <see cref="_VisibleTimeChanged"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void CallVisibleTimeChanged(GPropertyChangeArgs<TimeRange> args)
        {
            if (this._VisibleTimeChanged != null)
                this._VisibleTimeChanged(this, args);
        }
        private event GPropertyChangedHandler<TimeRange> _VisibleTimeChanged;
        #endregion
    }
    #endregion
}
