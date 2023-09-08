using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    public class EditablePanel : BufferedControl
    {
        public EditablePanel()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserMouse | ControlStyles.UserPaint, true);
            __MousePoints = new List<Point>();
        }
        protected override void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.AliceBlue);
            // e.Graphics.FillRectangle(Brushes.AliceBlue, new RectangleF(5, 5, 120, 80));

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
    }
}
