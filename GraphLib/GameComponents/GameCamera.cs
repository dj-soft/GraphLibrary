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
    /// <summary>
    /// Standardní kamera
    /// </summary>
    public class GameCamera : IGameCamera
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="gameControl"></param>
        public GameCamera(GameControl gameControl)
        {
            this._GameControl = gameControl;
            this.Properties = new CameraProperties();
        }
        private GameControl _GameControl;
        /// <summary>
        /// Vlastnosti kamery
        /// </summary>
        public CameraProperties Properties { get; private set; }
        #endregion
        #region Kreslení
        /// <summary>
        /// Metoda převezme dodanou sadu bodů <see cref="Point3D"/>, 
        /// konvertuje je do 2D souřadnic dle vlastností kamery, 
        /// vytvoří z ní uzavřený objekt a ten vykreslí danou barvou.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="points"></param>
        public void FillArea(Color color, params Point3D[] points)
        {
            _FillArea(color, points);
        }
        /// <summary>
        /// Metoda převezme dodanou sadu bodů <see cref="Point3D"/>, 
        /// konvertuje je do 2D souřadnic dle vlastností kamery, 
        /// vytvoří z ní uzavřený objekt a ten vykreslí danou barvou.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="points"></param>
        public void FillArea(Color color, IEnumerable<Point3D> points)
        {
            _FillArea(color, points);
        }
        /// <summary>
        /// Vykreslí danou rovinnou plochu ohraničenou danou sadou bodů. Provede konverzi souřadného systému
        /// </summary>
        /// <param name="color"></param>
        /// <param name="points"></param>
        private void _FillArea(Color color, IEnumerable<Point3D> points)
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
                this.Graphics.FillPath(Skin.Brush(color), gPath);
            }
        }
        #endregion
        #region Graphics
        /// <summary>
        /// Grafika pro kreslení
        /// </summary>
        public Graphics Graphics { get; set; }
        /// <summary>
        /// Připraví kameru a její grafiku pro kreslení
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public IDisposable PrepareGraphics(Graphics graphics)
        {
            this.Graphics = graphics;
            return GPainter.GraphicsUseSmooth(this.Graphics);
        }
        #endregion
        #region Konverze 3D souřadnic z reálného světa do oka kamery


        /// <summary>
        /// Vrátí 2D souřadnici daného bodu 3D
        /// </summary>
        /// <param name="point3D"></param>
        /// <returns></returns>
        public PointF GetPoint2D(Point3D point3D)
        {
            var angleH = point3D.AngleH;
            var angleV = point3D.AngleV;
            var hypXYZ = point3D.HypXYZ;
            var angle3D = point3D.Angle;

            var vector = new Vector3D(point3D, angle3D, 50d);
            var newPoint1 = vector.GetPointAtDistance(200d);
            var newPoint2 = vector.GetPointMatrix(2d);

            var dist = point3D - this.Properties.Location;
            var angle = dist.Angle - this.Properties.Angle;




            return new PointF((float)point3D.X, (float)point3D.Y);


        }

        /// <summary>
        /// Průmětná rovina
        /// </summary>
        public Plane3D Plane3D { get; set; }

        #endregion
    }
    #region class CameraProperties : Vlastnosti kamery
    /// <summary>
    /// Vlastnosti kamery
    /// </summary>
    public class CameraProperties
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor: 
        /// Provede iniciaci souřadného systému kamery na defaultní hodnoty.
        /// <para/>
        /// Nativní souřadný systém si představujme jako : 
        /// X (vodorovně zleva doprava, kladné vpravo);
        /// Y (svislé, kladné nahoru);
        /// Z (vodorovné, od pozorovatele vpřed, kladné ve směru pohledu očí)
        /// </summary>
        public CameraProperties()
        {
            Location = new Point3D(0d, 0d, 0d);
            Angle = new Angle3D(0d, 0d, 1d);
            Zoom = 1d;
            Rotation = 0d;
        }
        /// <summary>
        /// Souřadnice kamery v prostoru.
        /// Výchozí hodnota je { X=0, Y=0, Z=0 }, tedy oko kamery se nachází v počátku souřadného systému.
        /// <para/>
        /// Nativní souřadný systém si představujme jako : 
        /// X (vodorovně zleva doprava, kladné vpravo);
        /// Y (svislé, kladné nahoru);
        /// Z (vodorovné, od pozorovatele vpřed, kladné ve směru pohledu očí)
        /// </summary>
        public Point3D Location { get { return _Location; } set { _Location = value; } }
        private Point3D _Location;
        /// <summary>
        /// Orientace kamery v prostoru.
        /// Výchozí hodnota je { X=0, Y=0, Z=1 }, tedy kamera míří přesně podél osy Z ve směru jejích kladných hodnot = dozadu.
        /// Tato výchozí hodnota 
        /// <para/>
        /// Nativní souřadný systém si představujme jako : 
        /// X (vodorovně zleva doprava, kladné vpravo);
        /// Y (svislé, kladné nahoru);
        /// Z (vodorovné, od pozorovatele vpřed, kladné ve směru pohledu očí)
        /// </summary>
        public Angle3D Angle { get { return _Angle; } set { _Angle = value; } }
        private Angle3D _Angle;
        /// <summary>
        /// Zoom kamery.
        /// Výchozí hodnota ke 1, tedy kamera má zoom rovný lidskému vnímání; 
        /// hodnoty směrem k 0 rozšiřují záběr až k hodnotě 0 = odpovídá záběru 180°, 
        /// kladné hodnoty větší než 1 vytváří teleobjektiv = dalekohled.
        /// </summary>
        public Double Zoom { get { return _Zoom; } set { _Zoom = value; } }
        private Double _Zoom;
        /// <summary>
        /// Rotace kamery vůči horizontu.
        /// Výchozí hodnota je 0, tedy vodorovná rovina kamery je shodná s rovinou XZ souřadného systému (standardní vodorovná rovina).
        /// Kladná hodnota úhlu natočí kameru proti směru hodinových ručiček, to se projeví tak, že reálné vodorovné linie budou zobrazeny otočeny tak, že jejich pravý okraj bude dole a levý nahoře.
        /// </summary>
        public Angle2D Rotation { get { return _Rotation; } set { _Rotation = value; } }
        private Angle2D _Rotation;
        #endregion
    }
    #endregion
    #region interface IGameCamera
    /// <summary>
    /// Předpis pro kameru
    /// </summary>
    public interface IGameCamera
    {
        /// <summary>
        /// Připraví kameru a její grafiku pro kreslení
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        IDisposable PrepareGraphics(Graphics graphics);


        /// <summary>
        /// Vrátí 2D souřadnici (odpovídající obrazovce = grafika) daného bodu 3D (odpovídající reálnému světu)
        /// </summary>
        /// <param name="point3D"></param>
        /// <returns></returns>
        PointF GetPoint2D(Point3D point3D);

    }
    #endregion
}
