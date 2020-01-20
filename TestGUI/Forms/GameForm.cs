using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.GameComponents;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    [IsMainForm("Testování GameControlu", 999)]
    public partial class GameForm : Form
    {
        public GameForm()
        {
            InitializeComponent();
            InitGame();
        }

        protected void InitGame()
        {
            this._GameControl.CameraProperties.Location = new Point3D(0d, 0d, 0d);
            this._GameControl.CameraProperties.Angle = new Angle3D(1d, 0d, 1d);

            GameCube cube1 = new GameCube();
            cube1.Vertexes[0] = new Point3D(100d, 100d, 200d);
            cube1.Vertexes[1] = new Point3D(150d, 100d, 200d);
            cube1.Vertexes[2] = new Point3D(150d, 150d, 200d);
            cube1.Vertexes[3] = new Point3D(100d, 150d, 200d);
            cube1.Vertexes[4] = new Point3D(200d, 150d, 250d);
            cube1.Vertexes[5] = new Point3D(250d, 150d, 250d);
            cube1.Vertexes[6] = new Point3D(250d, 200d, 250d);
            cube1.Vertexes[7] = new Point3D(200d, 200d, 250d);
            cube1.SideColors[0] = Color.FromArgb(160, 220, 160);
            cube1.SideColors[1] = Color.FromArgb(160, 220, 180);
            cube1.SideColors[2] = Color.FromArgb(160, 200, 180);
            cube1.SideColors[3] = Color.FromArgb(160, 200, 160);
            cube1.SideColors[4] = Color.FromArgb(160, 220, 160);
            cube1.SideColors[5] = Color.FromArgb(120, 180, 140);

            this._GameControl.GameItems.Add(cube1);


            GameCube cube2 = new GameCube();
            cube2.Vertexes[0] = new Point3D(300d, 100d, 200d);
            cube2.Vertexes[1] = new Point3D(350d, 100d, 200d);
            cube2.Vertexes[2] = new Point3D(350d, 200d, 200d);
            cube2.Vertexes[3] = new Point3D(300d, 150d, 200d);
            cube2.Vertexes[4] = new Point3D(400d, 120d, 250d);
            cube2.Vertexes[5] = new Point3D(450d, 120d, 250d);
            cube2.Vertexes[6] = new Point3D(450d, 220d, 250d);
            cube2.Vertexes[7] = new Point3D(400d, 200d, 250d);
            cube2.SideColors[0] = Color.FromArgb(180, 120, 160);
            cube2.SideColors[1] = Color.FromArgb(160, 120, 180);
            cube2.SideColors[2] = Color.FromArgb(160, 100, 180);
            cube2.SideColors[3] = Color.FromArgb(180, 100, 160);
            cube2.SideColors[4] = Color.FromArgb(160, 120, 160);
            cube2.SideColors[5] = Color.FromArgb(120, 100, 180);

            this._GameControl.GameItems.Add(cube2);

        }
    }
}
