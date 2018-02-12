using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Djs.Common.Data;
using Djs.Common.Components;

namespace Djs.Common.TestGUI
{
    public partial class TestFormNew : Form
    {
        public TestFormNew()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.InitGControl();
        }
        private void InitGControl()
        {
            this.GControl.BackColor = Color.LightBlue;
            this.GControl.ResizeControl += new EventHandler(GControl_ResizeControl);

            this._TimeAxis = new GTimeAxis() { Bounds = new Rectangle(60, 30, 950, 45), Orientation = AxisOrientation.Top, Value = new TimeRange(DateTime.Now.Subtract(TimeSpan.FromDays(4)), DateTime.Now) };
            this._TimeAxis.BackColor = Color.LightSkyBlue;
            this._TimeAxis.ScaleLimit = new SizeRange(0.01m, 50m);
            this.GControl.AddItem(this._TimeAxis);

            this._SizeAxis = new GSizeAxis() { Bounds = new Rectangle(60, 100, 950, 45), Orientation = AxisOrientation.Top, Value = new SizeRange(0m, 210m), ValueLimit = new SizeRange(-210m, 420m) };
            this._SizeAxis.BackColor = Color.LightSalmon;
            this._SizeAxis.ScaleLimit = new SizeRange(0.05m, 20m);
            this.GControl.AddItem(this._SizeAxis);

            this._Splitter = new GSplitter() { Bounds = new Rectangle(60, 80, 950, 7), Orientation = Orientation.Horizontal, DragResponse = DragResponseType.InDragMove, LinkedItemPrevMinSize = 15, LinkedItemNextMinSize = 15, Value = 80, SplitterVisibleWidth = 2, SplitterActiveOverlap = 3, IsResizeToLinkItems = true };
            this._Splitter.LinkedItemPrev = this._TimeAxis;
            this._Splitter.LinkedItemNext = this._SizeAxis;
            this.GControl.AddItem(this._Splitter);

            this._ScrollBarH = new GScrollBar() { Bounds = new Rectangle(0, 200, 950, 28), ValueTotal = new SizeRange(0m, 1000m), Value = new SizeRange(200m, 400m) };
            this._ScrollBarH.UserDraw += new GUserDrawHandler(_ScrollBar_UserDraw);
            this.GControl.AddItem(this._ScrollBarH);

            this._ScrollBarV = new GScrollBar() { Bounds = new Rectangle(960, 0, 28, 300), ValueTotal = new SizeRange(0m, 1000m), Value = new SizeRange(200m, 400m) };
            this._ScrollBarV.UserDraw += new GUserDrawHandler(_ScrollBarV_UserDraw);
            this.GControl.AddItem(this._ScrollBarV);

            this.ControlsPosition();
        }
        void GControl_ResizeControl(object sender, EventArgs e)
        {
            this.ControlsPosition();
        }
        void _ScrollBarV_UserDraw(object sender, GUserDrawArgs e)
        {
            Rectangle target = e.UserAbsoluteBounds;
            target = target.Enlarge(1, 1, 0, 0);
            e.Graphics.DrawImage(IconLibrary.BackSand, target);
        }
        void _ScrollBar_UserDraw(object sender, GUserDrawArgs e)
        {
            Rectangle r = e.UserAbsoluteBounds;
            int tick = 0;
            for (int x = r.X + 1; x < (r.Right - 3); x += 8)
            {
                bool is5tick = ((tick++ % 5) == 0);
                e.Graphics.DrawLine((is5tick ? Pens.Gray : Pens.LightGray), x, r.Top + (is5tick ? 3 : 6), x, r.Bottom - ( is5tick ? 3 : 6));
            }
        }
        private GTimeAxis _TimeAxis;
        private GSplitter _Splitter;
        private GSizeAxis _SizeAxis;
        private GScrollBar _ScrollBarH;
        private GScrollBar _ScrollBarV;
        protected void ControlsPosition()
        {
            Size size = this.GControl.Size;
            int dx = 60;
            if (size.Width < (3 * dx))
                dx = size.Width / 3;
            int dw = size.Width - (2 * dx);

            if (this._TimeAxis != null)
            {
                Rectangle oldBounds = this._TimeAxis.Bounds;
                Rectangle newBounds = new Rectangle(dx - 10, oldBounds.Y, dw, oldBounds.Height);
                this._TimeAxis.Bounds = newBounds;
            }
            if (this._SizeAxis != null)
            {
                Rectangle oldBounds = this._SizeAxis.Bounds;
                Rectangle newBounds = new Rectangle(dx + 10, oldBounds.Y, dw, oldBounds.Height);
                this._SizeAxis.Bounds = newBounds;
            }
            if (this._Splitter != null)
            {
                this._Splitter.Refresh();
            }

            int scrollHeight = GScrollBar.DefaultSystemBarHeight;
            int scrollWidth = GScrollBar.DefaultSystemBarWidth;
            if (this._ScrollBarH != null)
            {
                Rectangle bounds = new Rectangle(2, size.Height - 30 - scrollHeight, size.Width - 2 - scrollWidth - 2, scrollHeight);
                this._ScrollBarH.Bounds = bounds;
            }
            if (this._ScrollBarV != null)
            {
                Rectangle bounds = new Rectangle(size.Width - 2 - scrollWidth, 30, scrollWidth, size.Height - 30 - 30 - scrollHeight);
                this._ScrollBarV.Bounds = bounds;
            }
        }
    }
}
