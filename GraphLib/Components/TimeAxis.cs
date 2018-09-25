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
        : GBaseAxis<DateTime?, TimeSpan?, TimeRange>, ITimeAxisConvertor
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
        public GTimeAxis() : base()
        {
            this.TimeAxisConvertor = new TimeAxisConvertor(this);
        }
        /// <summary>
        /// Instance TimeAxis konvertoru, navázaná na this
        /// </summary>
        protected TimeAxisConvertor TimeAxisConvertor;
        /// <summary>
        /// Initial value for new axis
        /// </summary>
        protected override TimeRange InitialValue
        {
            get { DateTime now = DateTime.Now; return new TimeRange(now.Date, now.Date.AddDays(1d)); }
        }
        /// <summary>
        /// Identita této osy (hodnota + velikost v pixelech)
        /// </summary>
        public override string Identity
        {
            get
            {
                string value = this.Value.Identity;
                string size = ((int)this.PixelSize).ToString();
                return value + "; " + size;
            }
        }
        /// <summary>
        /// Pozice prvního pixelu
        /// </summary>
        public int FirstPixel { get { return (int)this.PixelFirst; } }
        /// <summary>
        /// Délka osy v pixelech
        /// </summary>
        public int SizeInPixel { get { return (int)this.PixelSize; } }
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
        #region ITimeAxisConvertor members + jejich obsluha
        string ITimeAxisConvertor.Identity { get { return this.Identity; } }
        TimeRange ITimeAxisConvertor.Value { get { return this.Value; } set { this.Value = value; } }
        VisualTick[] ITimeAxisConvertor.Ticks { get { return this.TickList.ToArray(); } }
        int ITimeAxisConvertor.FirstPixel { get { return (int)this.PixelFirst; } }
        Double ITimeAxisConvertor.GetProportionalPixel(DateTime? time, int targetSize) { return this.TimeAxisConvertor.GetProportionalPixel(time, targetSize); }
        DoubleRange ITimeAxisConvertor.GetProportionalPixelRange(TimeRange timeRange, int targetSize) { return this.TimeAxisConvertor.GetProportionalPixelRange(timeRange, targetSize); }
        DateTime? ITimeAxisConvertor.GetProportionalTime(int pixel, int targetSize) { return this.TimeAxisConvertor.GetProportionalTime(pixel, targetSize); }
        Double ITimeAxisConvertor.GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio) { return this.TimeAxisConvertor.GetLogarithmicPixel(time, targetSize, proportionalRatio); }
        DoubleRange ITimeAxisConvertor.GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio) { return this.TimeAxisConvertor.GetLogarithmicPixelRange(timeRange, targetSize, proportionalRatio); }
        DateTime? ITimeAxisConvertor.GetLogarithmicTime(int pixel, int targetSize, float proportionalRatio) { return this.TimeAxisConvertor.GetLogarithmicTime(pixel, targetSize, proportionalRatio); }
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
    #region class TimeAxisConvertor : jednoduchá třída, která provádí výpočty potřebné pro interface ITimeAxisConvertor na časové ose
    /// <summary>
    /// TimeAxisConvertor : jednoduchá třída, která provádí výpočty potřebné pro interface ITimeAxisConvertor na časové ose
    /// </summary>
    public class TimeAxisConvertor : ITimeAxisConvertor
    {
        #region Konstruktor a privátní proměnné
        /// <summary>
        /// Konstruktor pro samostatný objekt
        /// </summary>
        public TimeAxisConvertor()
        {
            this._TimeAxis = null;
        }
        /// <summary>
        /// Konstruktor pro konvertor provázaný s TimeAxis
        /// </summary>
        /// <param name="timeAxis"></param>
        public TimeAxisConvertor(GTimeAxis timeAxis)
        {
            this._TimeAxis = timeAxis;
        }
        /// <summary>
        /// Konstruktor pro konvertor provázaný se synchronizerem hodnoty
        /// </summary>
        /// <param name="valueSynchronizer"></param>
        public TimeAxisConvertor(ValueTimeRangeSynchronizer valueSynchronizer)
        {
            this._ValueSynchronizer = valueSynchronizer;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            TimeRange value = this.Value;
            string text = (this._HasTimeAxis ? "From TimeAxis: " + value.ToString() :
                          (this._HasValueSynchronizer ? "From Synchronizer: " + value.ToString() :
                          (this._HasValue ? "From Value: " + value.ToString() : "Value is NULL")));
            return text;
        }
        /// <summary>
        /// Lokální hodnota
        /// </summary>
        private TimeRange _Value;
        /// <summary>
        /// Časová osa
        /// </summary>
        private GTimeAxis _TimeAxis;
        /// <summary>
        /// Synchronizer hodnoty
        /// </summary>
        private ValueTimeRangeSynchronizer _ValueSynchronizer;
        /// <summary>
        /// true pokud máme <see cref="_TimeAxis"/>
        /// </summary>
        private bool _HasTimeAxis { get { return (this._TimeAxis != null); } }
        /// <summary>
        /// true pokud máme <see cref="_ValueSynchronizer"/>
        /// </summary>
        private bool _HasValueSynchronizer { get { return (this._ValueSynchronizer != null); } }
        /// <summary>
        /// true pokud máme přímo zadanou hodnotu v <see cref="_Value"/>
        /// </summary>
        private bool _HasValue { get { return (this._Value != null); } }
        #endregion
        #region Public prvky, implicitní implementace ITimeAxisConvertor
        /// <summary>
        /// Aktuálně zobrazený interval data a času
        /// </summary>
        public TimeRange Value
        {
            get
            {
                if (this._HasTimeAxis) return this._TimeAxis.Value;
                if (this._HasValueSynchronizer) return this._ValueSynchronizer.Value;
                return this._Value;
            }
            set
            {
                if (this._HasTimeAxis) this._TimeAxis.Value = value;
                else if (this._HasValueSynchronizer) this._ValueSynchronizer.Value = value;
                else this._Value = value;
            }
        }
        /// <summary>
        /// Identita časového a vizuálního prostoru.
        /// Časový prostor popisuje rozmezí času (Begin a End) s maximální přesností.
        /// Vizuální prostor popisuje počet pixelů velikosti osy (pro osu Horizontal = Width), ale nikoli její pixel počátku (Left).
        /// </summary>
        public string Identity
        {
            get
            {
                if (this._HasTimeAxis) return this._TimeAxis.Identity;
                TimeRange value = this.Value;
                return (value != null ? value.Identity : "");
            }
        }
        /// <summary>
        /// Obsahuje všechny aktuální ticky na časové ose.
        /// </summary>
        public VisualTick[] Ticks
        {
            get { return (this._HasTimeAxis ? this._TimeAxis.Ticks.ToArray() : null); }
        }
        /// <summary>
        /// Pozice prvního pixelu
        /// </summary>
        public int FirstPixel
        {
            get { return (this._HasTimeAxis ? this._TimeAxis.FirstPixel : 0); }
        }
        /// <summary>
        /// Vrátí pozici daného času (v pixelech) na časové ose, proporcionálně přepočítanou pro danou cílovou velikost osy
        /// </summary>
        /// <param name="time">Zadaný vstupní čas</param>
        /// <param name="targetSize">Velikost výstupního prostoru v pixelech</param>
        /// <returns></returns>
        public Double GetProportionalPixel(DateTime? time, int targetSize) { return this.GetProportionalPoint(time, targetSize); }
        /// <summary>
        /// Vrátí rozsah daného časového úseku (v pixelech) na časové ose, proporcionálně přepočítanou pro danou cílovou velikost osy
        /// </summary>
        /// <param name="timeRange">Zadaný vstupní časový interval</param>
        /// <param name="targetSize">Velikost výstupního prostoru v pixelech</param>
        /// <returns></returns>
        public DoubleRange GetProportionalPixelRange(TimeRange timeRange, int targetSize) { return (timeRange != null ? new DoubleRange(this.GetProportionalPoint(timeRange.Begin, targetSize), this.GetProportionalPoint(timeRange.End, targetSize)) : null); }
        /// <summary>
        /// Vrátí datum, které odpovídá danému pixelu, na proporcionální časové ose.
        /// </summary>
        /// <param name="pixel">Relativní pixel, jehož čas hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        public DateTime? GetProportionalTime(int pixel, int targetSize) { return this.GetProportionalValue(pixel, targetSize); }
        /// <summary>
        /// Vrátí pozici daného času (v pixelech) na časové ose, logaritmicky přepočítanou pro danou cílovou velikost osy a definovanou proporcionální střední část
        /// </summary>
        /// <param name="time">Zadaný vstupní čas</param>
        /// <param name="targetSize">Velikost výstupního prostoru v pixelech</param>
        /// <param name="proportionalRatio"></param>
        /// <returns></returns>
        public Double GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio) { return this.GetLogarithmicPoint(time, targetSize, proportionalRatio); }
        /// <summary>
        /// Vrátí pozici daného časového úseku (v pixelech) na časové ose, logaritmicky přepočítanou pro danou cílovou velikost osy a definovanou proporcionální střední část
        /// </summary>
        /// <param name="timeRange">Zadaný vstupní časový interval</param>
        /// <param name="targetSize">Velikost výstupního prostoru v pixelech</param>
        /// <param name="proportionalRatio"></param>
        /// <returns></returns>
        public DoubleRange GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio) { return (timeRange != null ? new DoubleRange(this.GetLogarithmicPoint(timeRange.Begin, targetSize, proportionalRatio), this.GetLogarithmicPoint(timeRange.End, targetSize, proportionalRatio)) : null); }
        /// <summary>
        /// Vrátí datum, které odpovídá danému pixelu, na logaritmické časové ose.
        /// </summary>
        /// <param name="pixel">Relativní pixel, jehož čas hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        public DateTime? GetLogarithmicTime(int pixel, int targetSize, float proportionalRatio) { return this.GetLogarithmicValue(pixel, targetSize, proportionalRatio); }
        #endregion
        #region Vnitřní výpočtové metody GetProportionalPoint() a GetLogarithmicPoint()
        /// <summary>
        /// Vrací relativní vzdálenost (v pixelech) zadaného času od toho pixelu, kde začíná osa, při přepočtu velikosti osy na daný cílový počet pixelů.
        /// Tady parametr time je hledaný čas, a parametr targetSize určuje aktuální délku osy (na tuto délku se rozpočítá hodnota <see cref="Value"/>).
        /// </summary>
        /// <param name="time">Zadaný vstupní čas</param>
        /// <param name="targetSize">Velikost výstupního prostoru v pixelech</param>
        /// <returns></returns>
        protected Double GetProportionalPoint(DateTime? time, Double targetSize)
        {
            if (!this.IsValid(time, targetSize)) return 0d;

            TimeRange value = this.Value;
            Double axisSeconds = value.Size.Value.TotalSeconds;
            Double timeSeconds = (time.Value - value.Begin.Value).TotalSeconds;
            return targetSize * (timeSeconds / axisSeconds);
        }
        /// <summary>
        /// Vrátí relativní pozici (v pixelech) zadaného času na logaritmické časové ose, kde střední proporcionální část má danou velikost (proportionalRatio).
        /// Ona ta hodnota de facto není logaritmická, ale hyperbolická, 
        /// s tím že pro vstupní hodnotu času plus/mínus nekonečno se výstupní hodnota pixelu limitně blíží konci/začátku daného prostoru v pixelech.
        /// Jinak řečeno: pro time s rokem 0001 je výstup roven 0.0m, a pro rok 9999 je výstup roven targetSize.
        /// Ve výstupním prostoru (0 až targetSize) je uprostřed část, která zobrazuje čas lineárně; tato část obsazuje poměrný díl (proportionalRatio) celého prostoru (targetSize).
        /// Do tohoto prostoru se lineárně promítají vstupní časy (time), které leží v rozmezí aktuální hodnoty <see cref="Value"/>.
        /// </summary>
        /// <param name="time">Zadaný vstupní čas</param>
        /// <param name="targetSize">Velikost výstupního prostoru v pixelech</param>
        /// <param name="proportionalRatio">Poměrná část fyzického prostoru osy (targetSize), ležící uprostřed, ve které se zobrazují časy osy (Value) lineárně. Krajní části osy jsou logaritmické.</param>
        /// <returns></returns>
        protected Double GetLogarithmicPoint(DateTime? time, Double targetSize, float proportionalRatio)
        {
            if (!this.IsValid(time, targetSize)) return 0d;

            Double pointRatio = this.GetProportionalPoint(time, 1d);      // Pozice cílového data v jednotkách { 0 ÷ 1 }, kde 1 == konec viditelné osy; hodnota záporná = vlevo před lineární částí, hodnota větší než 1 = vpravo za lineární částí

            // Tady teprve začíná logaritmický algoritmus:
            Double result;
            Double linearRatio = (proportionalRatio < 0.4f ? 0.4d : (proportionalRatio > 0.9f ? 0.9d : (Double)proportionalRatio));        // Lineární část osy, relativní, poměrná část v rozmezí (0.4 - 0.9)
            DoubleRange linearRange = DoubleRange.CreateFromBeginSize(((1d - linearRatio) / 2d) * targetSize, linearRatio * targetSize);   // Lineární oblast osy, v pixelech, Od-Do (např. 40 ÷ 160)
            if (pointRatio >= 0d && pointRatio <= 1d)
            {   // Pozice cílového bodu v rozsahu { 0 ÷ 1 } jsou "uvnitř viditelné" osy = zobrazí se ve střední lineární části:
                result = linearRange.Begin + (pointRatio * linearRange.Size);
            }
            else
            {   // Ostatní pozice cílového bodu jsou mimo "viditelnou část" = zobrazí se v logaritmické části (na začátku nebo na konci prostoru):
                bool isPositive = (pointRatio > 1d);                              // true = jsme napravo (targetPixelRatio je větší), false = jsme nalevo
                Double distance = (isPositive ? pointRatio - 1d : -pointRatio);   // Vzdálenost hodnoty pointRatio od odpovídajcí hranice lineárního úseku, hodnota "distance" vždy začíná lehce nad nulou (nula nikdy není) a jde do kladného nekonečna
                Double distanceLog = 1d - (1d / (1d + distance));                 // Výsledná "logaritmická" hodnota v rozsahu (0 až 1), odpovídající distance v rosahu (0 až +nekonečno) (nepravý Logaritmus = Hyperbola)
                result = (isPositive ?
                            linearRange.End + (distanceLog * (targetSize - linearRange.End)) :
                            linearRange.Begin - (distanceLog * linearRange.Begin));
            }
            return result;
        }
        /// <summary>
        /// Vrací true, pokud daná data a hodnota osy <see cref="Value"/> dávají možnost počítat souřadnice.
        /// To vyžaduje, aby čas time měl hodnotu, velikost targetSize aby byla kladná, a hodnota <see cref="Value"/> měla zadán Begin a End, kde Begin je menší než End (nikoli rovno).
        /// </summary>
        /// <param name="time"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        protected bool IsValid(DateTime? time, Double targetSize)
        {
            if (!time.HasValue || targetSize <= 0d) return false;
            TimeRange value = this.Value;
            if (value == null || !value.IsFilled || value.End.Value <= value.Begin.Value) return false;
            return true;
        }
        #endregion
        #region Vnitřní výpočtové metody GetProportionalTime() a GetLogarithmicTime()
        /// <summary>
        /// Vrátí datum, které odpovídá danému pixelu, na proporcionální časové ose.
        /// </summary>
        /// <param name="pixel">Relativní pixel, jehož čas hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        protected DateTime? GetProportionalValue(int pixel, int targetSize)
        {
            if (!this.IsValid(pixel, targetSize)) return null;

            TimeRange value = this.Value;
            if (pixel == 0) return value.Begin;
            if (pixel == targetSize) return value.End;

            // Relativní pozice daného pixelu vzhledem k celkové velikosti (pixel / targetSize) převedená na počet sekund:
            Double totalSeconds = value.Size.Value.TotalSeconds;
            Double pixelPoint = (Double)pixel;
            Double pixelSize = (Double)targetSize;
            Double pixelSeconds = (totalSeconds * pixelPoint / pixelSize);

            // Přesný nezaokrouhlený čas:
            DateTime timeRaw = value.Begin.Value.Add(TimeSpan.FromSeconds(pixelSeconds));

            // Zaokrouhlení na "pixelovou" hodnotu času:
            //  Co to je? Pokud budu mít na časové ose zobrazen interval 5 dní + 4% (=449280 sekund),
            //            a osa má velikost 1074 pixelů, pak to značí, že jeden pixel reprezentuje (449280 / 1074) = 418,32402.... sekundy.
            //            Pokud první pixel má hodnotu t0 = Begin = {2018-09-24 12:00:00}, 
            //            pak na pixelu 87 je přesná hodnota času = t1 = (t0 + 36394,189944134078212290502793296) = (t0 + 10h 06m 34.000s).
            //            Na sousedním pixelu 88 je čas = t1 = (t0 + 36812,513966480446927374301675978) = (t0 + 10h 13m 32,5139664...s).
            //  Namísto času 10:06:34 bude lepší zobrazit zaokrouhlený čas 10:05:00, namísto 10:13:32.513 bude lepší 10:15:00.
            TimeSpan timeOnePixel = TimeSpan.FromSeconds(totalSeconds / pixelSize / 2d);     // Čas na jeden půlpixel, dle příkladu (418.32402 / 2) = 209.16201.... sekundy
            TimeSpan timeRoundPixel = timeOnePixel.GetRoundTimeBase();                       // Zaokrouhlovací základna, pro 209 sekund = 300 sekund
            return timeRaw.RoundTime(timeRoundPixel);                                        // Zaokrouhlí čas na pětiminuty
        }
        protected DateTime? GetLogarithmicValue(int pixel, int targetSize, float proportionalRatio)
        {
            if (!this.IsValid(pixel, targetSize)) return null;

            TimeRange value = this.Value;
            if (pixel == 0) return value.Begin;
            if (pixel == targetSize) return value.End;

            // Relativní pozice daného pixelu vzhledem k celkové velikosti (pixel / targetSize) převedená na počet sekund:
            Double pixelSeconds = value.Size.Value.TotalSeconds * (((Double)pixel) / ((Double)targetSize));
            DateTime time = value.Begin.Value.Add(TimeSpan.FromSeconds(pixelSeconds));
            TimeSpan round = TimeSpan.FromSeconds(60d);
            return time.RoundTime(round);
        }
        /// <summary>
        /// Vrací true, pokud daná data a hodnota osy <see cref="Value"/> dávají možnost počítat souřadnice.
        /// To vyžaduje, aby pixel měl hodnotu, velikost targetSize aby byla kladná, a hodnota <see cref="Value"/> měla zadán Begin a End, kde Begin je menší než End (nikoli rovno).
        /// </summary>
        /// <param name="pixel"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        protected bool IsValid(int? pixel, Double targetSize)
        {
            if (!pixel.HasValue || targetSize <= 0d) return false;
            TimeRange value = this.Value;
            if (value == null || !value.IsFilled || value.End.Value <= value.Begin.Value) return false;
            return true;
        }

        #endregion
    }
    #endregion
    #region interface ITimeAxisConvertor : Interface, který umožní pracovat s časovou osou
    /// <summary>
    /// ITimeAxisConvertor : Interface, který umožní pracovat s časovou osou
    /// </summary>
    public interface ITimeAxisConvertor
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
        TimeRange Value { get; set; }
        /// <summary>
        /// Obsahuje všechny aktuální ticky na časové ose.
        /// </summary>
        VisualTick[] Ticks { get; }
        /// <summary>
        /// Obsahuje první pixel, na kterém se nachází počátční hodnota osy.
        /// </summary>
        int FirstPixel { get; }
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pixel pro jinou velikost prostoru, než jakou má aktuální TimeAxis, kdy cílová velikost je dána parametrem targetSize.
        /// Jinými slovy: pokud na reálné časové ose máme zobrazeno rozmezí (numerický příklad): 40 - 80,
        /// pak tato metoda pro hodnotu time = 50 a targetSize = 100 vrátí hodnotu 25.
        /// Proč? Protože: požadovaná hodnota 50 se nachází na pozici 0.25 časové osy (40 - 80), a odpovídající pozice v cílovém prostoru (100 pixelů) je 25.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        Double GetProportionalPixel(DateTime? time, int targetSize);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek v daném cílovém prostoru.
        /// </summary>
        /// <param name="timeRange">Časový úsek, jehož prostor hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        DoubleRange GetProportionalPixelRange(TimeRange timeRange, int targetSize);
        /// <summary>
        /// Vrátí datum, které odpovídá danému pixelu, na proporcionální časové ose.
        /// </summary>
        /// <param name="pixel">Relativní pixel, jehož čas hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <returns></returns>
        DateTime? GetProportionalTime(int pixel, int targetSize);
        /// <summary>
        /// Vrátí relativní pixel, na kterém se nachází daný čas.
        /// Vrací pixel na logaritmické časové ose, kde střední část prostoru (z parametru "size") je proporcionální (její velikost je dána hodnotou "ratio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "size" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="time">Čas, jehož pozici hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        Double GetLogarithmicPixel(DateTime? time, int targetSize, float proportionalRatio);
        /// <summary>
        /// Vrátí pozici, na které se nachází daný časový úsek v daném cílovém prostoru, v logaritmickém měřítku.
        /// Vrací pixel na logaritmické časové ose, kde střední část prostoru (z parametru "size") je proporcionální (její velikost je dána hodnotou "ratio"),
        /// a okrajové části jsou logaritmické, takže do daného prostoru "size" se promítnou úplně všechny časy, jen v těch okrajových částech budou zahuštěné.
        /// </summary>
        /// <param name="timeRange">Časový úsek, jehož prostor hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        DoubleRange GetLogarithmicPixelRange(TimeRange timeRange, int targetSize, float proportionalRatio);
        /// <summary>
        /// Vrátí datum, které odpovídá danému pixelu, na logaritmické časové ose.
        /// </summary>
        /// <param name="pixel">Relativní pixel, jehož čas hledáme</param>
        /// <param name="targetSize">Cílový prostor (v počtu pixelů), do něhož máme promítnout viditelný prostor na ose</param>
        /// <param name="proportionalRatio">Relativní část prostoru "size", v němž je čas proporcionální (lineární). Povolené hodnoty jsou 0.4 až 0.9</param>
        /// <returns></returns>
        DateTime? GetLogarithmicTime(int pixel, int targetSize, float proportionalRatio);

    }
    #endregion
}
