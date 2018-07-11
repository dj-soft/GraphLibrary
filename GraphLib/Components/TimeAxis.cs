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
        int ITimeConvertor.GetPixel(DateTime? time) { return this.GetPixel(time); }
        Int32Range ITimeConvertor.GetPixelRange(TimeRange timeRange) { return this.GetPixelRange(timeRange); }
        int ITimeConvertor.GetProportionalPixel(DateTime? time, int targetSize) { return this.GetProportionalPixel(time, targetSize); }
        Int32Range ITimeConvertor.GetProportionalPixelRange(TimeRange timeRange, int targetSize) { return this.GetProportionalPixelRange(timeRange, targetSize); }
        int ITimeConvertor.GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio) { return this.GetLogarithmicPixel(time, targetSize, proportionalRatio); }
        Int32Range ITimeConvertor.GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio) { return this.GetLogarithmicPixelRange(timeRange, targetSize, proportionalRatio); }
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
        #region Výpočty časových os
        /// <summary>
        /// Metoda vrátí pozici pixelu přesně odpovídající danému času na aktuální ose, včetně započtení pozice počátku osy <see cref="GBaseAxis{TTick, TSize, TValue}.PixelFirst"/>.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <returns></returns>
        protected int GetPixel(DateTime? time)
        {
            decimal result = this._GetPosition(time);
            return (int)(Math.Round(result, 0));
        }
        protected Int32Range GetPixelRange(TimeRange timeRange)
        {
            decimal begin = this._GetPosition(timeRange.Begin);
            decimal end = this._GetPosition(timeRange.End);
            return Int32Range.CreateFromBeginSize((int)Math.Round(begin, 0), (int)Math.Round((end - begin), 0));
        }
        /// <summary>
        /// Metoda vrátí decimal pozici pixelu přesně odpovídající danému času na aktuální ose, včetně započtení pozice počátku osy <see cref="GBaseAxis{TTick, TSize, TValue}.PixelFirst"/>.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <returns></returns>
        protected decimal _GetPosition(DateTime? time)
        {
            decimal begin = this.PixelFirst;
            decimal? axisPosition = this.CalculatePositionLocalForTick(time);
            return begin + (axisPosition.HasValue ? axisPosition.Value : 0m);
        }

        /// <summary>
        /// Metoda vrátí pozici pixelu, odpovídající danému času na aktuální časové ose, 
        /// přepočtenou lineárně do cílového prostoru dle parametru "targetSize".
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        protected int GetProportionalPixel(DateTime? time, int targetSize)
        {
            decimal result = this._GetProportionalPosition(time, targetSize);
            return (int)(Math.Round(result, 0));
        }
        protected Int32Range GetProportionalPixelRange(TimeRange timeRange, int targetSize)
        {
            decimal begin = this._GetProportionalPosition(timeRange.Begin, targetSize);
            decimal end = this._GetProportionalPosition(timeRange.End, targetSize);
            return Int32Range.CreateFromBeginSize((int)Math.Round(begin, 0), (int)Math.Round((end - begin), 0));
        }
        /// <summary>
        /// Metoda vrátí decimal pozici pixelu, odpovídající danému času na aktuální časové ose, 
        /// přepočtenou lineárně do cílového prostoru dle parametru "targetSize".
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        protected decimal _GetProportionalPosition(DateTime? time, int targetSize)
        {
            if (targetSize <= 0) return 0m;
            decimal axisSize = this.PixelSize;
            if (axisSize <= 0m) return 0m;

            decimal? axisPosition = this.CalculatePositionLocalForTick(time);
            decimal size = (decimal)targetSize;
            if (!axisPosition.HasValue) return 0m;
            decimal result = size * ((axisPosition.Value) / axisSize);
            return result;
        }

        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pozici na logaritmické časové ose, kde střední část prostoru (dle parametru "targetSize") je proporcionální (její velikost je dána hodnotou "proportionalRatio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "targetSize" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        protected int GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio)
        {
            decimal result = this._GetLogarithmicPosition(time, targetSize, proportionalRatio);
            return (int)(Math.Round(result, 0));
        }
        protected Int32Range GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio)
        {
            decimal begin = this._GetLogarithmicPosition(timeRange.Begin, targetSize, proportionalRatio);
            decimal end = this._GetLogarithmicPosition(timeRange.End, targetSize, proportionalRatio);
            return Int32Range.CreateFromBeginSize((int)Math.Round(begin, 0), (int)Math.Round((end - begin), 0));
        }
        /// <summary>
        /// Vrátí decimal pozici pixelu, na kterém se nachází daný čas.
        /// Vrací pozici na logaritmické časové ose, kde střední část prostoru (dle parametru "targetSize") je proporcionální (její velikost je dána hodnotou "proportionalRatio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "targetSize" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor v pixelech, do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "targetSize", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        protected decimal _GetLogarithmicPosition(DateTime? time, int targetSize, float proportionalRatio)
        {
            if (targetSize <= 0) return 0m;
            decimal axisSize = this.PixelSize;
            if (axisSize <= 0m) return 0m;

            decimal? axisPosition = this.CalculatePositionLocalForTick(time);
            if (!axisPosition.HasValue) return 0m;

            // Tady teprve začíná logaritmický algoritmus:
            decimal size = (decimal)targetSize;
            decimal result = 0m;
            decimal proportional = (proportionalRatio < 0.4f ? 0.4m : (proportionalRatio > 0.9f ? 0.9m : (decimal)proportionalRatio));
            SizeRange linearRange = SizeRange.CreateFromBeginSize(((1m - proportional) / 2) * size, proportional * size);
            decimal targetPixelRatio = (axisPosition.Value / axisSize);   // Pozice daného time na časové ose: 
            if (targetPixelRatio >= 0m && targetPixelRatio <= 1m)
            {   // Hodnoty 0-1 jsou "uvnitř" = v lineární části:
                result = linearRange.Begin.Value + (targetPixelRatio * linearRange.Size.Value);
            }
            else
            {   // Ostatní hodnoty targetPixelRatio jsou v logaritmické části na začátku nebo na konci:
                bool isPositive = (targetPixelRatio > 1m);                // true = jsme napravo (targetPixelRatio je větší), false = jsme nalevo
                decimal distance = (isPositive ? targetPixelRatio - 1m: -targetPixelRatio);   // Vzdálenost hodnoty targetPixelRatio od odpovídajcí hranice lineárního úseku, hodnota začíná lehce nad nulou (nula nikdy není) a jde do kladného nekonečna
                decimal logdist = 1m - (1m / (1m + distance));            // Výsledná hodnota v rozsahu (0 až 1), odpovídající distance v rosahu (0 až +nekonečno)
                result = (isPositive ?
                            linearRange.End.Value + (logdist * (size - linearRange.End.Value)) :
                            linearRange.Begin.Value - (logdist * linearRange.Begin.Value));
            }
            return result;
        }
        #endregion
    }
    #endregion
    #region Interface ITimeConvertor
    /// <summary>
    /// ITimeConvertor : Interface, který umožní pracovat s časovou osou
    /// </summary>
    public interface ITimeConvertor
    {
        /// <summary>
        /// Identita časového a vizuálního prostoru.
        /// Časový prostor popisuje rozmezí času (Begin a End) s maximální přesností.
        /// Vizuální prostor popisuje počet pixelů velikosti osy (pro osu Horizontal = Width), ale nikoli její pixel počátku (Left).
        /// </summary>
        string Identity { get; }
        /// <summary>
        /// Aktuálně zobrazený interval data a času
        /// </summary>
        TimeRange VisibleTime { get; }
        /// <summary>
        /// Obsahuje všechny aktuální ticky na časové ose.
        /// </summary>
        VisualTick[] Ticks { get; }
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <returns></returns>
        int GetPixel(DateTime? time);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek na aktuální časové ose.
        /// </summary>
        /// <param name="timeRange"></param>
        /// <returns></returns>
        Int32Range GetPixelRange(TimeRange timeRange);
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pixel pro jinou velikost prostoru, než jakou má aktuální TimeAxis, kdy cílová velikost je dána parametrem targetSize.
        /// Jinými slovy: pokud na reálné časové ose máme zobrazeno rozmezí (numerický příklad): 40 - 80,
        /// pak <see cref="GetProportionalPixel(DateTime?, int)"/> pro hodnotu time = 50 a targetSize = 100 vrátí hodnotu 25.
        /// Proč? Protože: požadovaná hodnota 50 se nachází na pozici 0.25 časové osy (40 - 80), a odpovídající pozice v cílovém prostoru (100 pixelů) je 25.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        int GetProportionalPixel(DateTime? time, int targetSize);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek v daném cílovém prostoru.
        /// </summary>
        /// <param name="timeRange"></param>
        /// <returns></returns>
        Int32Range GetProportionalPixelRange(TimeRange timeRange, int targetSize);
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pixel na logaritmické časové ose, kde střední část prostoru (z parametru "size") je proporcionální (její velikost je dána hodnotou "ratio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "size" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        int GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek v daném cílovém prostoru, v logaritmickém měřítku.
        /// Vrací pixel na logaritmické časové ose, kde střední část prostoru (z parametru "size") je proporcionální (její velikost je dána hodnotou "ratio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "size" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="timeRange"></param>
        /// <param name="targetSize">Cílový prostor, do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        Int32Range GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio);

        /// <summary>
        /// Event vyvolaný po každé změně hodnoty <see cref="VisibleTime"/>
        /// </summary>
        event GPropertyChangedHandler<TimeRange> VisibleTimeChanged;
    }
    #endregion
}
