using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RES = Noris.LCS.Base.WorkScheduler.Resources;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Application;

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
            this._TimeAxis.ScaleLimit = new DecimalNRange(0.01m, 50m);
            this.GControl.AddItem(this._TimeAxis);

            this._SizeAxis = new GSizeAxis() { Bounds = new Rectangle(60, 100, 950, 45), Orientation = AxisOrientation.Top, Value = new DecimalNRange(0m, 210m), ValueLimit = new DecimalNRange(-210m, 420m) };
            this._SizeAxis.BackColor = Color.LightSalmon;
            this._SizeAxis.ScaleLimit = new DecimalNRange(0.05m, 20m);
            this.GControl.AddItem(this._SizeAxis);

            this._Splitter = new GSplitter() { Bounds = new Rectangle(60, 80, 950, 7), Orientation = Orientation.Horizontal, DragResponse = DragResponseType.InDragMove, LinkedItemPrevMinSize = 15, LinkedItemNextMinSize = 15, Value = 80, SplitterVisibleWidth = 2, SplitterActiveOverlap = 3, IsResizeToLinkItems = true };
            this._Splitter.LinkedItemPrev = this._TimeAxis;
            this._Splitter.LinkedItemNext = this._SizeAxis;
            this.GControl.AddItem(this._Splitter);

            this._ScrollBarH = new GScrollBar() { Bounds = new Rectangle(0, 200, 950, 28), ValueTotal = new DecimalNRange(0m, 1000m), Value = new DecimalNRange(200m, 400m), BackColor = Color.DimGray, Tag = "Vodorovný ScrollBar dole" };
            this._ScrollBarH.UserDraw += new GUserDrawHandler(_ScrollBar_UserDraw);
            this.GControl.AddItem(this._ScrollBarH);

            this._ScrollBarV = new GScrollBar() { Bounds = new Rectangle(960, 0, 28, 300), ValueTotal = new DecimalNRange(0m, 1000m), Value = new DecimalNRange(200m, 400m), Tag = "Svislý ScrollBar vpravo" };
            this._ScrollBarV.UserDraw += new GUserDrawHandler(_ScrollBarV_UserDraw);
            this.GControl.AddItem(this._ScrollBarV);

            this._Track = new Components.TrackBar() { Bounds = new Rectangle(20, 65, 150, 30), Value = 0.333m };
            this._Track.Layout.Orientation = Orientation.Horizontal;
            this.GControl.AddItem(this._Track);

            this._TabContainer = new GTabContainer() { TabHeaderMode = ShowTabHeaderMode.Always | ShowTabHeaderMode.CollapseItem, TabHeaderPosition = RectangleSide.Bottom };
            GScrollBar dataControl;
            dataControl = new GScrollBar() { Orientation = Orientation.Horizontal, ValueTotal = new DecimalNRange(0, 1000), Value = new DecimalNRange(160, 260), BackColor = Color.LightCyan, Tag = "Přepínací ScrollBar na straně 1" };
            this._TabContainer.AddTabItem(dataControl, "První scrollbar", image: App.ResourcesApp.GetImage(RES.Images.Small16.BulletBluePng));
            dataControl = new GScrollBar() { Orientation = Orientation.Horizontal, ValueTotal = new DecimalNRange(0, 1000), Value = new DecimalNRange(840, 860), Tag = "Přepínací ScrollBar na straně 2" };
            this._TabContainer.AddTabItem(dataControl, "Druhý scrollbar", image: App.ResourcesApp.GetImage(RES.Images.Small16.BulletGreenPng));
            dataControl = new GScrollBar() { Orientation = Orientation.Horizontal, ValueTotal = new DecimalNRange(0, 1000), Value = new DecimalNRange(450, 850), Tag = "Přepínací ScrollBar na straně 3" };
            this._TabContainer.AddTabItem(dataControl, "Třetí scrollbar", image: App.ResourcesApp.GetImage(RES.Images.Small16.BulletOrangePng));
            this.GControl.AddItem(this._TabContainer);
            
            this.ControlsPosition();
        }
        private void _TabHeaderH_TabItemPaintBackGround(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(App.ResourcesApp.GetImage(RES.Images.Actions.CodeVariablePng), e.ClipRectangle);
        }
        private void HeaderH2_TabHeaderPaintBackGround(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(App.ResourcesApp.GetImage(RES.Images.Actions.CodeClassPng), e.ClipRectangle);
        }
        private void _TabHeaderV_ActiveItemChanged(object sender, GPropertyChangeArgs<GTabPage> e)
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

            GPainter.DrawString(e.Graphics, "Normal => Normal => Normal => Normal", fontInfo, areaB, ContentAlignment.MiddleCenter, Color.Black, transformation: MatrixTransformationType.NoTransform);
            GPainter.DrawString(e.Graphics, "Rotate90 => Rotate90 => Rotate90 => Rotate90", fontInfo, areaL, ContentAlignment.MiddleCenter, Color.Black, transformation: MatrixTransformationType.Rotate90);
            GPainter.DrawString(e.Graphics, "Rotate180 => Rotate180 => Rotate180 => Rotate180", fontInfo, areaT, ContentAlignment.MiddleCenter, Color.Black, transformation: MatrixTransformationType.Rotate180);
            GPainter.DrawString(e.Graphics, "Rotate270 => Rotate270 => Rotate270 => Rotate270", fontInfo, areaR, ContentAlignment.MiddleCenter, Color.Black, transformation: MatrixTransformationType.Rotate270);
        }
        void GControl_ResizeControl(object sender, EventArgs e)
        {
            this.ControlsPosition();
        }
        void _ScrollBarV_UserDraw(object sender, GUserDrawArgs e)
        {
            Rectangle target = e.UserAbsoluteBounds;
            target = target.Enlarge(1, 1, 0, 0);
            e.Graphics.DrawImage(App.ResourcesApp.GetImage(RES.Images.Actions.CodeContextPng), target);
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
        private Components.TrackBar _Track;
        private GTabContainer _TabContainer;

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

            this._Track.Bounds = new Rectangle(20, axisBottom + 5, 150, 30);

            // Datový prostor:
            int areaTop = axisBottom + 40;
            Rectangle dataArea = new Rectangle(2, areaTop + 4, size.Width - 2 - 2 - scrollWidth, size.Height - 3 - scrollHeight - areaTop - 4);

            int top = 30;
            int bottom = size.Height - 2;
            int y = top;
            if (this._ScrollBarV != null)
            {   // Svislý scrollbar vpravo:
                this._ScrollBarV.Bounds = new Rectangle(dataArea.Right, dataArea.Y, scrollWidth, dataArea.Height);
            }
            if (this._ScrollBarH != null)
            {   // Vodorovný scrollbar dole:
                this._ScrollBarH.Bounds = new Rectangle(dataArea.X, dataArea.Bottom, dataArea.Width, scrollHeight);
            }
            if (this._TabContainer != null)
            {   // TabContainer vyplňuje prostor dataArea:
                this._TabContainer.Bounds = new Rectangle(dataArea.X, dataArea.Y, dataArea.Width - 2, dataArea.Height - 2);
            }
        }
    }
}
