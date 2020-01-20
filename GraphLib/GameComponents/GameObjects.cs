using System;
using System.Collections;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    public class GameCube : GameItem
    {
        public GameCube()
        {
            Vertexes = new Point3D[8];
            SideColors = new Color?[6];
        }
        public Point3D[] Vertexes { get; private set; }
        public Color?[] SideColors { get; private set; }
        public Color SideColor { get; private set; }
        public override void Draw(GameCamera camera)
        {
            var p = Vertexes;
            camera.FillArea(SideColors[0] ?? SideColor, p[0], p[1], p[2], p[3]);
            camera.FillArea(SideColors[1] ?? SideColor, p[0], p[1], p[5], p[4]);
            camera.FillArea(SideColors[2] ?? SideColor, p[1], p[2], p[6], p[5]);
            camera.FillArea(SideColors[3] ?? SideColor, p[2], p[3], p[7], p[6]);
            camera.FillArea(SideColors[4] ?? SideColor, p[3], p[0], p[4], p[7]);
            camera.FillArea(SideColors[5] ?? SideColor, p[4], p[5], p[6], p[7]);
        }
    }
}
