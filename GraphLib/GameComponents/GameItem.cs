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
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GameItem()
        {
            NeedDraw = true;
        }
        /// <summary>
        /// V této metodě bude prvek vykreslen do kamery
        /// </summary>
        /// <param name="camera"></param>
        public abstract void Draw(GameCamera camera);
        /// <summary>
        /// True pokud je prvek změněn a potřebuje překreslit
        /// </summary>
        public bool NeedDraw { get; set; }
    }
    /// <summary>
    /// Čtyřúhelník
    /// </summary>
    public class GameItemRectangle : GameItem
    {
        /// <summary>
        /// V této metodě bude prvek vykreslen do kamery
        /// </summary>
        /// <param name="camera"></param>
        public override void Draw(GameCamera camera)
        {
            camera.FillArea(Color.DarkGreen, this.Bounds.Point1, this.Bounds.Point2, this.Bounds.Point3, this.Bounds.Point4);
        }
        /// <summary>
        /// Souřadnice
        /// </summary>
        public Rectangle3D Bounds { get; set; }
    }
}
