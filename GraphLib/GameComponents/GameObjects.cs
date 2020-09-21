using System;
using System.Collections;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    /// <summary>
    /// Kostka
    /// </summary>
    public class GameCube : GameItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GameCube()
        {
            Vertexes = new Point3D[8];
            SideColors = new Color?[6];
        }
        /// <summary>
        /// Vrcholové body, je jich 8
        /// </summary>
        public Point3D[] Vertexes { get; private set; }
        /// <summary>
        /// Boční barvy, je jich 6
        /// </summary>
        public Color?[] SideColors { get; private set; }
        /// <summary>
        /// Výchozí boční barva
        /// </summary>
        public Color SideColor { get; private set; }
        /// <summary>
        /// Vykreslí objekt do kamery
        /// </summary>
        /// <param name="camera"></param>
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
