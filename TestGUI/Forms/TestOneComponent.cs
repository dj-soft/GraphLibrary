using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    public partial class TestOneComponent : Form
    {
        public TestOneComponent()
        {
            InitializeComponent();
            this.InitTrackBar();
            Application.App.TracePriority = Application.TracePriority.Priority2_Lowest;
        }
        private void _CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region Testy TrackBar
        protected void InitTrackBar()
        {
            this._TrackH = new GTrackBar() { Bounds = new Rectangle(50, 20, 96, 25), ValueTotal = new DecimalRange(0m, 100m), Value = 75m };
            this._TrackH.Layout.Orientation = Orientation.Horizontal;
            this._TrackH.Layout.TickCount = 5;
            this._TrackH.Layout.TickType = TrackBarTickType.None;
            this._TrackH.Layout.TrackLineType = TrackBarLineType.ColorBlendLine;

            this._TrackH.Layout.TrackLineType = TrackBarLineType.Solid;
            this._TrackH.Layout.TrackActiveBackColor = Color.FromArgb(240, Color.Green);
            this._TrackH.Layout.TrackInactiveBackColor = Color.FromArgb(240, Color.LightGray);

            this._TrackH.Layout.TrackPointerVisualSize = new Size(15, 15);
            this._TrackH.Layout.TrackBackColor = Color.LightBlue;
            this._TrackH.ValueChanging += _TrackH_ValueChanging;
            this._TrackH.ValueChanged += _TrackH_ValueChanged;
            this._TrackH.ValueRounder = TrackValueRound;
            this._Control.AddItem(_TrackH);
            PrepareToolTipFor(this._TrackH);

            this._TrackV = new GTrackBar() { Bounds = new Rectangle(320, 20, 47, 250), ValueTotal = new DecimalRange(0m, 100m), Value = 0m };
            this._TrackV.Layout.Orientation = Orientation.Vertical;
            this._TrackV.Layout.TickCount = 20;
            this._TrackV.Layout.TickType = TrackBarTickType.StandardDouble;
            this._TrackV.Layout.TrackLineType = TrackBarLineType.ColorBlendLine;
            this._TrackV.Layout.TrackInactiveBackColor = Color.FromArgb(192, Color.DimGray);
            this._TrackV.Layout.TrackPointerVisualSize = new Size(15, 15);
            this._TrackV.Layout.TrackBackColor = Color.LightYellow;
            this._TrackV.ValueChanging += _TrackV_ValueChanging;
            this._TrackV.ValueChanged += _TrackV_ValueChanged;
            this._Control.AddItem(_TrackV);
            PrepareToolTipFor(this._TrackV);
        }
        protected static decimal TrackValueRound(decimal value)
        {
            decimal r = Math.Round(value / 5m, 0);
            return 5m * r;
        }
        private void _TrackH_ValueChanging(object sender, GPropertyChangeArgs<decimal> e)
        {
            decimal value = this._TrackH.Value;
            string text = Math.Round(value, 2).ToString("0.00");
            this._Text1.Text = "ValueChanging H [" + (++_Count1).ToString() + "] = " + text;
            PrepareToolTipFor(this._TrackH);
        }
        private void _TrackV_ValueChanging(object sender, GPropertyChangeArgs<decimal> e)
        {
            decimal value = this._TrackV.Value;
            this._Text1.Text = "ValueChanging V [" + (++_Count1).ToString() + "] = " + Math.Round(value, 2).ToString("0.00");
            PrepareToolTipFor(this._TrackV);
        }
        private int _Count1 = 0;
        private void _TrackH_ValueChanged(object sender, GPropertyChangeArgs<decimal> e)
        {
            decimal value = this._TrackH.Value;
            this._Text2.Text = "ValueChanged H [" + (++_Count2).ToString() + "] = " + Math.Round(value, 2).ToString("0.00");
            PrepareToolTipFor(this._TrackH);
        }
        private void _TrackV_ValueChanged(object sender, GPropertyChangeArgs<decimal> e)
        {
            decimal value = this._TrackV.Value;
            this._Text2.Text = "ValueChanged V [" + (++_Count2).ToString() + "] = " + Math.Round(value, 2).ToString("0.00");
            PrepareToolTipFor(this._TrackV);
        }
        private int _Count2 = 0;
        private static void PrepareToolTipFor(GTrackBar trackBar)
        {
            decimal value = trackBar.Value;
            string text = Math.Round(value, 2).ToString("0.00");
            trackBar.ToolTipTitle = text + " mm";
            trackBar.ToolTipText = "Aktuální hodnota TrackBaru";
        }
        protected GTrackBar _TrackH;
        protected GTrackBar _TrackV;
        #endregion

        #region Testy GCtrlTest => ResizeControl

        protected void InitGComp()
        {
            this._TestV = new GCtrlTest() { Bounds = new Rectangle(25, 10, 150, 40), BackColor = Color.LightSkyBlue, ResizeSides = RectangleSide.Vertical, ShowResizeAllways = true };
            this._TestH = new GCtrlTest() { Bounds = new Rectangle(200, 60, 100, 70), BackColor = Color.LightSeaGreen, ResizeSides = RectangleSide.Horizontal, CanUpsideDown = true };
            this._TestB = new GCtrlTest() { Bounds = new Rectangle(25, 60, 150, 70), BackColor = Color.LightGoldenrodYellow, ResizeSides = RectangleSide.Vertical | RectangleSide.Horizontal, ShowResizeAllways = true };
            this._Control.AddItem(_TestV);
            this._Control.AddItem(_TestH);
            this._Control.AddItem(_TestB);
        }
        protected GCtrlTest _TestV;
        protected GCtrlTest _TestH;
        protected GCtrlTest _TestB;
        #endregion
    }
    #region Třída Testy GCtrlTest => pro testy ResizeControl
    public class GCtrlTest : InteractiveContainer, IResizeObject
    {
        public GCtrlTest()
        {
            this._ResizeControl = new ResizeControl(this);
        }
        private ResizeControl _ResizeControl;
        public RectangleSide ResizeSides { get { return this._ResizeControl.ResizeSides; } set { this._ResizeControl.ResizeSides = value; } }
        public bool ShowResizeAllways { get { return this._ResizeControl.ShowResizeAllways; } set { this._ResizeControl.ShowResizeAllways = value; } }
        public bool CanUpsideDown { get { return this._ResizeControl.CanUpsideDown; } set { this._ResizeControl.CanUpsideDown = value; } }
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._ResizeControl.Childs; } }

        void IResizeObject.SetBoundsResized(ResizeObjectArgs e)
        {
            Rectangle bounds = e.BoundsTarget;
            if (bounds.X < 5)
            {
                int r = bounds.Right;
                bounds.X = 5;
                bounds.Width = r - 5;
            }
            if (bounds.Right > (this.Parent.ClientSize.Width - 5))
                bounds.Width = this.Parent.ClientSize.Width - 5 - bounds.X;

            this.Bounds = bounds;
            this.Parent.Repaint();
        }
    }
    #endregion
}
