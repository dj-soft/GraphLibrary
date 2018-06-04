using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    public partial class TestFormNew : Form
    {
        public TestFormNew()
        {
            InitializeComponent();
            this.Size = new Size(1020, 920);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.InitGControl();
        }
        protected override void OnShown(EventArgs e)
        {
            this.ControlsPosition();
        }
        private void InitGControl()
        {
            this.GControl.BackColor = Color.LightBlue;
            this.GControl.ResizeControl += new EventHandler(GControl_ResizeControl);
            this.GControl.DrawStandardLayer += GControl_DrawStandardLayer;

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

            this._TabContainer = new TabContainer() { TabHeaderMode = ShowTabHeaderMode.Always | ShowTabHeaderMode.CollapseItem };
            this._TabContainer.AddTabItem(new GScrollBar(), "První scrollbar");
            this._TabContainer.AddTabItem(new GScrollBar(), "Druhý scrollbar");
            this._TabContainer.AddTabItem(new GScrollBar(), "Třetí scrollbar");
            this.GControl.AddItem(this._TabContainer);

            /*
            this._TabHeaderH = new TabHeader() { Bounds = new Rectangle(0, 160, 950, 32), Position = RectangleSide.Top };
            this._TabHeaderH.TabItemPaintBackGround += _TabHeaderH_TabItemPaintBackGround;
            var headerH1 = this._TabHeaderH.AddHeader("První stránka", Asol.Tools.WorkScheduler.Components.IconStandard.ObjectFlipVertical32);
            var headerH2 = this._TabHeaderH.AddHeader("Druhá stránka", Asol.Tools.WorkScheduler.Components.IconStandard.ObjectFlipHorizontal32);
            var headerH3 = this._TabHeaderH.AddHeader("Třetí...");
            var headerH4 = this._TabHeaderH.AddHeader("Vodorovný scrollbar");
            headerH4.ToolTipText = "Aktivace této stránky aktivuje Vodorovný scrollbar.";
            headerH4.Control = this._ScrollBarH;
            headerH4.BackColor = Color.Violet.Morph(Color.Yellow, 0.50f);
            var headerH5 = this._TabHeaderH.AddHeader("Str.5", Asol.Tools.WorkScheduler.Components.IconStandard.GoDown);
            this._TabHeaderH.ActiveHeaderItem = headerH3;
            this.GControl.AddItem(this._TabHeaderH);

            this._TabHeaderV = new TabHeader() { Bounds = new Rectangle(0, 160, 950, 32), Position = RectangleSide.Left };
            this._TabHeaderV.HeaderSizeRange = new Int32Range(180, 180);
            this._TabHeaderV.ActiveItemChanged += _TabHeaderV_ActiveItemChanged;
            var headerV1 = this._TabHeaderV.AddHeader("Plan", "Plan items", Asol.Tools.WorkScheduler.Components.IconStandard.GoDown);
            headerV1.ToolTipText = "Položky ve stavu Zaplánováno";
            var headerV2 = this._TabHeaderV.AddHeader("Product", "Product orders", Asol.Tools.WorkScheduler.Components.IconStandard.EditUndo);
            headerV2.ToolTipText = "Existující výrobní příkazy";
            var headerV3 = this._TabHeaderV.AddHeader("Invalid", "Invalid items");
            headerV3.ToolTipText = "Chybné položky";
            // this._TabHeaderV.ActiveHeaderItem = headerV1;
            this.GControl.AddItem(this._TabHeaderV);
            */
            this.ControlsPosition();
        }
        private void _TabHeaderH_TabItemPaintBackGround(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(Asol.Tools.WorkScheduler.Components.IconLibrary.BackSand, e.ClipRectangle);
        }
        private void HeaderH2_TabHeaderPaintBackGround(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(Asol.Tools.WorkScheduler.Components.IconLibrary.BackSand, e.ClipRectangle);
        }
        private void _TabHeaderV_ActiveItemChanged(object sender, GPropertyChangeArgs<TabHeader.TabItem> e)
        {
            if (e.OldValue != null && e.OldValue.Key == "Plan")
                e.OldValue.Text = "Plan items";
            if (e.NewValue != null && e.NewValue.Key == "Plan")
                e.NewValue.Text = "Položky plánu";

        }
        private void GControl_DrawStandardLayer(object sender, PaintEventArgs e)
        {
            // this._DrawArea(e);
        }
        private void _DrawArea(PaintEventArgs e)
        {
            Point center = this.ClientRectangle.Center();
            Rectangle area = center.CreateRectangleFromCenter(430);
            GPainter.DrawRectangle(e.Graphics, area, Color.LightGreen);
            FontInfo fontInfo = FontInfo.MessageBox;

            int h = 32;
            Rectangle areaT = new Rectangle(area.X, area.Y, area.Width, h);
            Rectangle areaR = new Rectangle(area.Right - h, area.Y, h, area.Height);
            Rectangle areaB = new Rectangle(area.X, area.Bottom - h, area.Width, h);
            Rectangle areaL = new Rectangle(area.X, area.Y, h, area.Height);

            GPainter.DrawString(e.Graphics, areaB, "Normal => Normal => Normal => Normal", Color.Black, fontInfo, ContentAlignment.MiddleCenter, MatrixTransformationType.NoTransform);
            GPainter.DrawString(e.Graphics, areaL, "Rotate90 => Rotate90 => Rotate90 => Rotate90", Color.Black, fontInfo, ContentAlignment.MiddleCenter, MatrixTransformationType.Rotate90);
            GPainter.DrawString(e.Graphics, areaT, "Rotate180 => Rotate180 => Rotate180 => Rotate180", Color.Black, fontInfo, ContentAlignment.MiddleCenter, MatrixTransformationType.Rotate180);
            GPainter.DrawString(e.Graphics, areaR, "Rotate270 => Rotate270 => Rotate270 => Rotate270", Color.Black, fontInfo, ContentAlignment.MiddleCenter, MatrixTransformationType.Rotate270);
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
        private TabHeader _TabHeaderH;
        private TabHeader _TabHeaderV;
        private TabContainer _TabContainer;

        protected void ControlsPosition()
        {
            Size size = this.GControl.Size;
            int dx = 60;
            if (size.Width < (3 * dx))
                dx = size.Width / 3;
            int dw = size.Width - (2 * dx);

            int axisBottom = 60;
            if (this._TimeAxis != null)
            {
                Rectangle oldBounds = this._TimeAxis.Bounds;
                Rectangle newBounds = new Rectangle(dx - 10, oldBounds.Y, dw, oldBounds.Height);
                this._TimeAxis.Bounds = newBounds;
                axisBottom = newBounds.Bottom;
            }
            if (this._SizeAxis != null)
            {
                Rectangle oldBounds = this._SizeAxis.Bounds;
                Rectangle newBounds = new Rectangle(dx + 10, oldBounds.Y, dw, oldBounds.Height);
                this._SizeAxis.Bounds = newBounds;
                axisBottom = newBounds.Bottom;
            }
            if (this._Splitter != null)
            {
                this._Splitter.Refresh();
            }

            int scrollHeight = GScrollBar.DefaultSystemBarHeight;
            int scrollWidth = GScrollBar.DefaultSystemBarWidth;
            int headerHeight = 32;
            int top = 30;
            int bottom = size.Height - 2;
            int y = top;
            if (this._ScrollBarV != null)
            {
                Rectangle bounds = new Rectangle(size.Width - 2 - scrollWidth, y, scrollWidth, bottom - scrollHeight - y);
                this._ScrollBarV.Bounds = bounds;
                bottom = bounds.Bottom;
                y = bounds.Bottom;
            }
            if (this._ScrollBarH != null)
            {
                Rectangle bounds = new Rectangle(2, y, size.Width - 2 - scrollWidth - 2, scrollHeight);
                this._ScrollBarH.Bounds = bounds;
                y = bounds.Y - headerHeight;
            }
            if (this._TabHeaderH != null)
            {
                Rectangle bounds = new Rectangle(2, y - 3, size.Width - 2 - scrollWidth - 2, headerHeight);
                this._TabHeaderH.Bounds = bounds;
                bottom = bounds.Y;
            }
            if (this._TabHeaderV != null)
            {
                Rectangle bounds = new Rectangle(2, top, headerHeight, bottom - top);
                this._TabHeaderV.Bounds = bounds;
            }
            if (this._TabContainer != null)
            {
                Rectangle bounds = new Rectangle(2, axisBottom, 640, bottom - 42);
                this._TabContainer.Bounds = bounds;
            }
        }
    }
}
