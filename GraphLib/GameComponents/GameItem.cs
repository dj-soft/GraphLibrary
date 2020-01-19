using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    /// <summary>
    /// Jeden kterýkoli prvek vykreslovaný do <see cref="GameControl"/>
    /// </summary>
    public abstract class GameItem
    {
        public GameItem()
        {
            NeedDraw = true;
        }
        public abstract void Draw(Graphics graphics, GameCamera camera);
       
        public bool NeedDraw { get; set; }
    }


    public class GameItemRectangle : GameItem
    {
        public override void Draw(Graphics graphics, GameCamera camera)
        {
            camera.FillArea(graphics, Color.DarkGreen, this.Bounds.Point1, this.Bounds.Point2, this.Bounds.Point3, this.Bounds.Point4);
        }
        public Rectangle3D Bounds { get; set; }
    }
}
