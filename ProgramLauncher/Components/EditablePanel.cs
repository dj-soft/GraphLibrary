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
            DataItems = new List<BaseData>();
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

                var lastPoint = __MousePoints[0];
                for (int i = 1; i < lstCount; i++)
                {
                    var currPoint = __MousePoints[i];
                    e.Graphics.DrawLine(Pens.BlueViolet, lastPoint, currPoint);
                    lastPoint = currPoint;
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

        public List<BaseData> DataItems { get; set; }
    }
}
