using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
            InitInteractive();
        }
        private void InitInteractive()
        {
            // this._GInteractiveControl.PaintToBuffer += new PaintEventHandler(_GInteractiveControl_PaintToBuffer); 

            this._ActiveList = new List<IInteractiveItem>();
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(20, 210, 60, 30), Color = Color.CornflowerBlue, Angle = 90f });
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(60, 250, 60, 30), Color = Color.LightGreen, Angle = 90f });
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(40, 290, 60, 30), Color = Color.PaleGoldenrod, Angle = 90f });
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(80, 330, 60, 30), Color = Color.PaleVioletRed, Angle = 90f });

            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(140, 255, 60, 30), Color = Color.PaleVioletRed, Angle = 90f });
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(180, 330, 60, 30), Color = Color.CornflowerBlue, Angle = 90f });
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(160, 295, 60, 30), Color = Color.LightGreen, Angle = 90f });
            this._ActiveList.Add(new TestActiveItem() { Bounds = new Rectangle(200, 210, 60, 30), Color = Color.PaleGoldenrod, Angle = 90f });

            this._TimeAxis = new GTimeAxis();
            DateTime now = DateTime.Now;
            DateTime begin = (new DateTime(now.Year, now.Month, now.Day)).AddDays(1 - ((int)now.DayOfWeek));
            DateTime end = begin.AddDays(2);
            this._TimeAxis.Value = new TimeRange(begin, end);
            this._TimeAxis.BoundsSilent = new Rectangle(10, 170, 500, 40);
            this._TimeAxis.BackColor = Color.LightCyan;
            this._TimeAxis.BackColor3DEffect = 0.2f;
            this._ActiveList.Add(this._TimeAxis);

            this._Splitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, Bounds = new Rectangle(5, 20, 200, 5), SplitterActiveOverlap = 3, Value = 60, DragResponse = DragResponseType.InDragMove, LinkedItemPrev = this._TimeAxis };
            // this._Splitter.LocationChanging += new EventHandler(_Splitter_LocationChanging);
            this._ActiveList.Add(this._Splitter);

            this._TimeAxisPos();

            this._SizeAxisH = new GSizeAxis();
            this._SizeAxisH.ValueLimit = new DecimalNRange(-100m, 1000m);
            this._SizeAxisH.Value = new DecimalNRange(0m, 200m);
            this._SizeAxisH.Bounds = new Rectangle(10, 300, 500, 25);
            this._SizeAxisH.Orientation = AxisOrientation.Top;
            this._SizeAxisH.BackColor = Color.FromArgb(248, 252, 255);
            this._SizeAxisH.BackColor3DEffect = 0.2f;
            this._ActiveList.Add(this._SizeAxisH);

            this._SizeAxisV = new GSizeAxis();
            this._SizeAxisV.ValueLimit = new DecimalNRange(-100m, 1000m);
            this._SizeAxisV.Value = new DecimalNRange(0m, 200m);
            this._SizeAxisV.Bounds = new Rectangle(5, 5, 25, 500);
            this._SizeAxisV.Orientation = AxisOrientation.LeftDown;
            this._SizeAxisV.BackColor = Color.FromArgb(248, 252, 255);
            this._SizeAxisV.BackColor3DEffect = 0.1f;
            this._ActiveList.Add(this._SizeAxisV);
            /*
            this._SizeAxisV.SynchronizeMode = AxisSynchronizeMode.Scale;
            this._SizeAxisH.SynchronizedSlaveAxisList.Add(this._SizeAxisV);
            this._SizeAxisH.SynchronizeMode = AxisSynchronizeMode.Scale;
            this._SizeAxisV.SynchronizedSlaveAxisList.Add(this._SizeAxisH);
            */
            this._SizeAxisPos();


            this._ScrollBarH = new GScrollBar() { Bounds = new Rectangle(2, 200, 400, 12), ValueTotal = new DecimalNRange(0, 1000), Value = new DecimalNRange(400, 600) };
            this._ActiveList.Add(this._ScrollBarH);
            this._ScrollBarV = new GScrollBar() { Bounds = new Rectangle(400, 2, 12, 400), ValueTotal = new DecimalNRange(0, 1000), Value = new DecimalNRange(800, 900) };
            this._ActiveList.Add(this._ScrollBarV);
            this._ScrollBarPos();


            this.MovableArea1 = new MovableItem() { Bounds = new Rectangle(400, 95, 90, 25), BackColor = Color.CadetBlue };
            this._ActiveList.Add(this.MovableArea1);
            this.MovableArea2 = new MovableItem() { Bounds = new Rectangle(500, 95, 100, 25), BackColor = Color.LightYellow };
            this._ActiveList.Add(this.MovableArea2);
            this.MovableArea3 = new MovableItem() { Bounds = new Rectangle(400, 125, 90, 30), BackColor = Color.LimeGreen };
            this._ActiveList.Add(this.MovableArea3);
            this.MovableArea4 = new MovableItem() { Bounds = new Rectangle(500, 125, 100, 30) };
            this._ActiveList.Add(this.MovableArea4);

            
            this.ContainerArea1 = new InteractiveContainer() { Bounds = new Rectangle(280, 160, 300, 180), BackColor = Color.Salmon };
            this._ActiveList.Add(this.ContainerArea1);

            this.ContainerArea1.AddItem(new MovableItem() { Bounds = new Rectangle(5, 5, 70, 20), BackColor = Color.DarkOrange });   // , DragDrawGhost = DragDrawGhostMode.DragOnlyStandard
            this.ContainerArea1.AddItem(new MovableItem() { Bounds = new Rectangle(105, 5, 70, 20), BackColor = Color.DarkBlue });   // , DragDrawGhost = DragDrawGhostMode.DragWithGhostAtOriginal 
            this.ContainerArea1.AddItem(new MovableItem() { Bounds = new Rectangle(5, 40, 70, 20), BackColor = Color.DarkGreen });   // , DragDrawGhost = DragDrawGhostMode.DragWithGhostOnInteractive
            this.ContainerArea1.AddItem(new MovableItem() { Bounds = new Rectangle(105, 40, 70, 20), BackColor = Color.DarkGreen }); // , DragDrawGhost = DragDrawGhostMode.DragWithGhostOnInteractive 


            this._GInteractiveControl.Items = this._ActiveList;
            this._GInteractiveControl.ResizeControl += new EventHandler(_GInteractiveControl_ResizeBeforeDraw);
        }
        void _Splitter_LocationChanging(object sender, EventArgs e)
        {
            this._TimeAxisPos();
        }
        void _GInteractiveControl_ResizeBeforeDraw(object sender, EventArgs e)
        {
            this._TimeAxisPos();
            this._SizeAxisPos();
            this._ScrollBarPos();
        }
        void _TimeAxisPos()
        {
            Rectangle gic = this._GInteractiveControl.ClientItemsRectangle;
            int splitY = this._Splitter.Value;
            Rectangle tax = new Rectangle(gic.Left + 60, 10, gic.Width - 80, splitY - 12);
            this._TimeAxis.Bounds = tax;           // set value to Bounds ensure RepaintAll items
            this._Splitter.BoundsNonActive = new Int32NRange(tax.Left, tax.Right);
//                 Rectangle(tax.Left, tax.Bottom + 25, tax.Width, 5);
        }
        void _SizeAxisPos()
        {
            Rectangle gic = this._GInteractiveControl.ClientItemsRectangle;
            Rectangle tax = new Rectangle(gic.Left + 60, gic.Bottom - 45, gic.Width - 80, 25);
            this._SizeAxisH.Bounds = tax;           // set value to Bounds ensure RepaintAll items
            tax = new Rectangle(gic.Left + 5, 10, 55, gic.Height - 30);
            this._SizeAxisV.Bounds = tax;           // set value to Bounds ensure RepaintAll items
        }
        private void _ScrollBarPos()
        {
            Rectangle gic = this._GInteractiveControl.ClientItemsRectangle;
            this._ScrollBarH.Bounds = new Rectangle(gic.X, gic.Bottom - 15, gic.Width - 15, 15);
            this._ScrollBarV.Bounds = new Rectangle(gic.Right - 15, gic.Y, 15, gic.Height - 15);
        }
        protected List<IInteractiveItem> _ActiveList;
        protected GTimeAxis _TimeAxis;
        protected GSizeAxis _SizeAxisV;
        protected GSizeAxis _SizeAxisH;
        protected GScrollBar _ScrollBarH;
        protected GScrollBar _ScrollBarV;
        protected MovableItem MovableArea1;
        protected MovableItem MovableArea2;
        protected MovableItem MovableArea3;
        protected MovableItem MovableArea4;

        protected InteractiveContainer ContainerArea1;

        protected Asol.Tools.WorkScheduler.Components.GSplitter _Splitter;
        private void button1_Click(object sender, EventArgs e)
        {
            this._SizeAxisH.Value = new DecimalNRange(100, 500);
            this._SizeAxisV.Value = new DecimalNRange(100, 500);
            this._GInteractiveControl.Draw();
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = this._AxisEnabledCheck.Checked;
            this._TimeAxis.Is.Enabled = enabled;
            this._SizeAxisH.Is.Enabled = enabled;
            this._SizeAxisV.Is.Enabled = enabled;
        }
    }
    public class TestActiveItem : InteractiveDragObject, IInteractiveItem
    {
        public TestActiveItem()
        {
            this.Is.DrawDragMoveGhostInteractive = true;
        }
        protected Rectangle? OriginalBounds { get; set; }
        public Color Color { get; set; }
        public float Angle { get; set; }
        public Point? MouseCenter { get; set; }
        #region Interactive
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            e.ToolTipData = null;
            string toolText = null;
            if (e.MouseRelativePoint.HasValue && this.Color == Color.LightGreen)
                toolText = "MousePosition = " + e.MouseRelativePoint.Value.Add(this.Bounds.Location).ToString();
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseEnter:
                    this.Angle = 270f;
                    this.Repaint();
                    e.ToolTipData.InfoText = toolText;
                    break;
                case GInteractiveChangeState.MouseOver:
                    this.MouseCenter = e.MouseRelativePoint;
                    this.Repaint();
                    e.ToolTipData.InfoText = toolText;
                    break;
                case GInteractiveChangeState.LeftDragMoveBegin:
                    this.MouseCenter = null;
                    this.Angle = 0f;
                    this.OriginalBounds = this.Bounds;
                    e.RepaintAllItems = true;
                    this.Repaint();
                    e.RequiredCursorType = SysCursorType.SizeAll;
                    e.ToolTipData.InfoText = toolText;
                    break;
                case GInteractiveChangeState.LeftDragMoveStep:
                    this.Bounds = e.DragMoveToBounds.Value;
                    this.Repaint();
                    e.ToolTipData.InfoText = toolText;
                    break;
                case GInteractiveChangeState.LeftDragMoveCancel:
                    if (this.OriginalBounds.HasValue)
                    {
                        Rectangle oldBounds = this.Bounds;
                        Rectangle newBounds = this.OriginalBounds.Value;
                        this.SetBounds(newBounds, ProcessAction.PrepareInnerItems, EventSourceType.InteractiveChanged);
                        this.Repaint();
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveDone:
                    this.Angle = 270f;
                    this.OriginalBounds = null;
                    e.RepaintAllItems = true;
                    e.RequiredCursorType = SysCursorType.Default;
                    e.ToolTipData.InfoText = toolText;
                    // this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
                case GInteractiveChangeState.LeftDragMoveEnd:
                    this.OriginalBounds = null;
                    e.RequiredCursorType = null;
                    e.RepaintAllItems = true;
                    break;
                case GInteractiveChangeState.RightClick:
                    this.Color = Color.Gray;
                    this.Repaint();
                    break;
                case GInteractiveChangeState.MouseLeave:
                    this.MouseCenter = null;
                    this.Angle = 90f;
                    this.MouseCenter = null;
                    e.RepaintAllItems = true;
                    // this.RepaintToLayers = GInteractiveDrawLayer.Standard;
                    break;
            }
        }
        #endregion
        #region Draw
        /// <summary>
        /// Vykreslí prvek
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag & Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            bool isGhost = (drawMode.HasFlag(DrawItemMode.Ghost));
            if (!isGhost)
                GPainter.DrawButtonBase(e.Graphics, absoluteBounds, new DrawButtonArgs() { BackColor = this.Color, InteractiveState = this.InteractiveState, MouseTrackPoint = this.MouseCenter });
            else
                this._DrawNative(e, absoluteBounds);
        }
        
        private void _DrawNative(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            if (this.MouseCenter.HasValue)
            {
                Rectangle r = absoluteBounds;
                Point[] pts = new Point[]
                    {
                        new Point(r.Left, r.Top), 
                        new Point(r.Right, r.Top),
                        new Point(r.Right, r.Bottom), 
                        new Point(r.Left, r.Bottom), 
                        new Point(r.Left, r.Top)
                    };
                using (System.Drawing.Drawing2D.PathGradientBrush p = new System.Drawing.Drawing2D.PathGradientBrush(pts))
                {
                    p.CenterPoint = new PointF(r.X + this.MouseCenter.Value.X, r.Y + this.MouseCenter.Value.Y);
                    p.CenterColor = Color.White;
                    p.SurroundColors = new Color[] { this.Color, this.Color, this.Color, this.Color, this.Color };
                    e.Graphics.FillRectangle(p, absoluteBounds);
                }
            }
            else if (this.IsDragged)
            {
                if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                {   // Kreslím objekt, který je přemísťován, ale do spodní vrstvy = jeho původní umístění:
                    using (System.Drawing.SolidBrush b = new SolidBrush(Color.FromArgb(96, this.Color)))
                    {
                        e.Graphics.FillRectangle(b, this.OriginalBounds.Value);
                    }
                }
                else
                {   // Kreslím objekt, který je přemísťován, do horní = interaktivní vrstvy = jeho nové umístění:
                    using (System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(absoluteBounds, this.Color, Color.White, this.Angle))
                    {
                        e.Graphics.FillRectangle(b, absoluteBounds);
                    }
                }
            }
            else
            {   // Normální objekt:
                using (System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(absoluteBounds, this.Color, Color.White, this.Angle))
                {
                    e.Graphics.FillRectangle(b, absoluteBounds);
                }
            }
        }
        #endregion
    }
}
