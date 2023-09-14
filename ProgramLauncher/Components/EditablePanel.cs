using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DjSoft.Tools.ProgramLauncher.Data;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    public class EditablePanel : BufferedControl
    {
        public EditablePanel()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserMouse | ControlStyles.UserPaint, true);
            __MousePoints = new List<Point>();
            __Items = new ChildItems<EditablePanel, DataItemBase>(this);
        }
        protected override void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.AliceBlue);
            _PaintMousePoints(e);
            _PaintDataItems(e);

        }
        private void _PaintMousePoints(PaintEventArgs e)
        {
            int lstCount = __MousePoints.Count;
            if (lstCount > 0)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

                int alpha = 0;
                Color color = Color.FromArgb(alpha, Color.BlueViolet);
                using (Pen pen = new Pen(color))
                {
                    var lastPoint = __MousePoints[0];
                    for (int i = 1; i < lstCount; i++)
                    {
                        var currPoint = __MousePoints[i];

                        if (alpha < 255)
                        {
                            alpha++;
                            pen.Color = Color.FromArgb(alpha, Color.BlueViolet);
                        }

                        e.Graphics.DrawLine(pen, lastPoint, currPoint);
                        lastPoint = currPoint;
                    }
                }
            }
        }
        private void _PaintDataItems(PaintEventArgs e)
        {
            var mouseState = MouseState.CreateCurrent(this);
            using (PaintDataEventArgs pdea = new PaintDataEventArgs(e, mouseState, this))
            {
                foreach (var dataItem in DataItems)
                    dataItem.Paint(pdea);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var point = e.Location;
            int maxCount = 120;
            int lstCount = __MousePoints.Count;
            if (lstCount > maxCount)
                __MousePoints.RemoveRange(0, lstCount - maxCount);
            __MousePoints.Add(point);
            this.Draw();
        }
        private List<Point> __MousePoints;
        private ChildItems<EditablePanel, DataItemBase> __Items;
        public IList<DataItemBase> DataItems { get { return __Items; } }

        /// <summary>
        /// Definice layoutu pro prvky v tomto panelu. Jeden panel má jeden layout.
        /// </summary>
        public DataLayout DataLayout { get { return __DataLayout; } set { __DataLayout = value; } }
        private DataLayout __DataLayout;
    }
}
