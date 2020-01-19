using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    public class GameCamera
    {
        public GameCamera(GameControl control)
        { }
        public void DrawItem(Graphics graphics, GameItem gameItem)
        {
            gameItem.Draw(graphics, this);
        }
        /// <summary>
        /// Vrátí 2D souřadnici daného bodu 3D
        /// </summary>
        /// <param name="point3D"></param>
        /// <returns></returns>
        public PointF GetPoint2D(Point3D point3D)
        {
            return new PointF((float)point3D.X, (float)point3D.Y);
        }

        public void FillArea(Graphics graphics, Color color, params Point3D[] points)
        {
            using (GraphicsPath gPath = new GraphicsPath(FillMode.Winding))
            {
                PointF? last = null;
                foreach (Point3D point in points)
                {
                    PointF curr = GetPoint2D(point);
                    if (last.HasValue)
                        gPath.AddLine(last.Value, curr);
                    last = curr;
                }
                gPath.CloseFigure();
                graphics.FillPath(Skin.Brush(color), gPath);
            }
        }
    }
}
