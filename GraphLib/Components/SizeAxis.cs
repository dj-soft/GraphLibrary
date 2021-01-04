using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GSizeAxis 
    /// <summary>
    /// GSizeAxis : 
    /// </summary>
    public class GSizeAxis 
        : BaseAxis<Decimal?, Decimal?, DecimalNRange>
    {
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GSizeAxis(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GSizeAxis() : base() { }
        /// <summary>
        /// Initial value for new axis
        /// </summary>
        protected override DecimalNRange InitialValue
        {
            get { return new DecimalNRange(0m, 1m); }
        }
        /// <summary>
        /// Returns a new instance of TValue for specified begin and end of interval.
        /// </summary>
        /// <param name="begin">Value of Begin interval</param>
        /// <param name="end">Value of End interval</param>
        /// <returns></returns>
        protected override DecimalNRange GetValue(Decimal? begin, Decimal? end)
        {
            return new DecimalNRange(begin, end);
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
        protected override decimal? GetAxisUnits(decimal? interval)
        {
            return interval;
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
        protected override decimal? GetAxisSize(decimal units)
        {
            return units;
        }
        /// <summary>
        /// Returns a string representation of value of Tick, using string Format from ArrangementItem for this Tick.
        /// Typically return tick.ToString(format), for real TTick type.
        /// </summary>
        /// <param name="tick">Value of Tick</param>
        /// <param name="format">Format string on ArrangementItem of this Tick</param>
        /// <returns></returns>
        protected override string GetTickText(decimal? tick, string format)
        {
            if (!tick.HasValue) return "";
            decimal value = (tick.Value < 0m ? -tick.Value : tick.Value);
            string minus =  (tick.Value < 0m ? "-" : "");
            decimal divisor = 1m;
            string units = "";
            if (value == 0m) { divisor = 1m; units = " "; }
            else if (value <= 0.000010m) { divisor = 0.000000001m; units = " n"; }
            else if (value <= 0.010000m) { divisor = 0.000001000m; units = " u"; }
            else if (value <= 10.00000m) { divisor = 0.001000000m; units = " m"; }
            else if (value <= 10.00000m) { divisor = 1.000000000m; units = " m"; }
            else if (value <= 10000.00m) { divisor = 1000.000m; units = " k"; }
            else if (value <= 10000000m) { divisor = 1000000m; units = " M"; }
            else { divisor = 1000000000m; units = " G"; }
            value = value / divisor;
            string axisUnit = this.AxisUnit;
            if (!String.IsNullOrEmpty(axisUnit)) units += axisUnit.Trim();

            return minus + value.ToString() + units;
        }
        /// <summary>
        /// Returns specified value (value on axis, TTick: for example DateTime) rounded to an interval (TSize: for example TimeSpan) with RoundMode.
        /// For example on TimeAxis: when value is 15.2.2016 14:35:16.165; and interval is 00:15:00.000, then result RoundValue is 15.2.2016 14:30:00 (for RoundMode = Math).
        /// </summary>
        /// <param name="value">Value (Tick) for round</param>
        /// <param name="interval">Interval on which will be Tick rounded</param>
        /// <param name="roundMode">Mode for round</param>
        /// <returns></returns>
        protected override decimal? RoundTickToInterval(decimal? value, decimal? interval, RoundMode roundMode)
        {
            return (value.HasValue && interval.HasValue ? (decimal?)DecimalNRange.RoundValue(value.Value, interval.Value, roundMode) : (decimal?)null);
        }
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
            //                                         pixel      minitick    větší    číslovaný  formát   mezníkový  formát      Původní dělitel, jednotka
            //                                     pixelSize, stdTickSize, bigTickSize, stdLabelSize, stdLabelFormat, bigLabelSize, bigLabelFormat, outerLabelFormat
            this.AddArrangementOne(new ArrangementOne(0.000000m, 0.000001m, 0.000005m, 0.000010m, "#.000000", 0.000100m, "", "", this));   // 10.00m, "nm", this));
            this.AddArrangementOne(new ArrangementOne(0.000001m, 0.000005m, 0.000010m, 0.000020m, "#.000000", 0.000100m, "", "", this));   // 20.00m, "nm", this));
            this.AddArrangementOne(new ArrangementOne(0.000001m, 0.000010m, 0.000050m, 0.000100m, "#.000000", 0.001000m, "", "", this));   // 100.0m, "nm", this));
            this.AddArrangementOne(new ArrangementOne(0.000001m, 0.000050m, 0.000100m, 0.000200m, "#.000000", 0.001000m, "", "", this));   // 200.0m, "nm", this));
            this.AddArrangementOne(new ArrangementOne(0.000010m, 0.000100m, 0.000500m, 0.001000m, "#.000000", 0.010000m, "", "", this));   // 1.000m, "um", this));
            this.AddArrangementOne(new ArrangementOne(0.000010m, 0.000500m, 0.001000m, 0.002000m, "#.000000", 0.010000m, "", "", this));   // 2.000m, "um", this));
            this.AddArrangementOne(new ArrangementOne(0.000100m, 0.001000m, 0.005000m, 0.010000m, "#.000000", 0.100000m, "", "", this));   // 10.00m, "um", this));
            this.AddArrangementOne(new ArrangementOne(0.000100m, 0.005000m, 0.010000m, 0.020000m, "#.000000", 0.100000m, "", "", this));   // 20.00m, "um", this));
            this.AddArrangementOne(new ArrangementOne(0.001000m, 0.010000m, 0.050000m, 0.100000m, "#.000000", 1.000000m, "", "", this));   // 100.0m, "um", this));
            this.AddArrangementOne(new ArrangementOne(0.001000m, 0.050000m, 0.100000m, 0.200000m, "#.000000", 1.000000m, "", "", this));   // 200.0m, "um", this));
            this.AddArrangementOne(new ArrangementOne(0.010000m, 0.100000m, 0.500000m, 1.000000m, "#.000000", 10.00000m, "", "", this));   // 1.000m, "mm", this));
            this.AddArrangementOne(new ArrangementOne(0.010000m, 0.500000m, 1.000000m, 2.000000m, "#.000000", 10.00000m, "", "", this));   // 2.000m, "mm", this));
            this.AddArrangementOne(new ArrangementOne(0.100000m, 1.000000m, 5.000000m, 10.00000m, "#.000000", 100.0000m, "", "", this));   //  1.000m, "cm", this));
            this.AddArrangementOne(new ArrangementOne(0.100000m, 5.000000m, 10.00000m, 20.00000m, "#.000000", 100.0000m, "", "", this));   // 2.000m, "cm", this));
            this.AddArrangementOne(new ArrangementOne(1.000000m, 10.00000m, 50.00000m, 100.0000m, "#.000000", 1000.000m, "", "", this));   // 10.00m, "cm", this));
            this.AddArrangementOne(new ArrangementOne(1.000000m, 50.00000m, 100.0000m, 200.0000m, "#.000000", 1000.000m, "", "", this));   // 20.00m, "cm", this));
            this.AddArrangementOne(new ArrangementOne(10.00000m, 100.0000m, 500.0000m, 1000.000m, "#.000000", 10000.00m, "", "", this));   // 1.0000m, "m", this));
            this.AddArrangementOne(new ArrangementOne(10.00000m, 500.0000m, 1000.000m, 2000.000m, "#.000000", 10000.00m, "", "", this));   // 2.0000m, "m", this));
            this.AddArrangementOne(new ArrangementOne(100.0000m, 1000.000m, 5000.000m, 10000.00m, "#.000000", 100000.0m, "", "", this));   // 10.000m, "m", this));
            this.AddArrangementOne(new ArrangementOne(100.0000m, 5000.000m, 10000.00m, 20000.00m, "#.000000", 100000.0m, "", "", this));   // 20.000m, "m", this));
            this.AddArrangementOne(new ArrangementOne(1000.000m, 10000.00m, 50000.00m, 100000.0m, "#.000000", 1000000m, "", "", this));   // 100.000m, "m", this));
            this.AddArrangementOne(new ArrangementOne(1000.000m, 50000.00m, 100000.0m, 200000.0m, "#.000000", 1000000m, "", "", this));   // 200.000m, "m", this));
            this.AddArrangementOne(new ArrangementOne(10000.00m, 100000.0m, 500000.0m, 1000000.0m, "#.000000", 10000000m, "", "", this));   // 1.000m, "km", this));
            this.AddArrangementOne(new ArrangementOne(10000.00m, 500000.0m, 1000000m, 2000000.0m, "#.000000", 10000000m, "", "", this));   // 2.000m, "km", this));
        }

        /// <summary>
        /// Unit added to ticks label.
        /// For example, when axis show values 0.015, then axis format value as "15 m" and add AxisUnit, then can show for example "15 mm" or "15 mA".
        /// For value == 15000.00 wil show value as "15 k" + AxisUnit, can show for example: "15 km", "15 kV"...
        /// </summary>
        public string AxisUnit { get; set; }
        /// <summary>
        /// Prepare layout for Tooltip in case, when ToolTip will be showed.
        /// Is called after e.ToolTipData.InfoText is prepared (contain valid text).
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            base.PrepareToolTip(e);
            e.ToolTipData.TitleText = "Size informations";
            e.ToolTipData.AnimationType = TooltipAnimationType.Instant;
        }
    }
    #endregion
}
