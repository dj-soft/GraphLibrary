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

            GameCube cube = new GameCube();
            cube.Vertexes[0] = new Point3D(100d, 100d, 200d);
            cube.Vertexes[1] = new Point3D(150d, 100d, 200d);
            cube.Vertexes[2] = new Point3D(150d, 150d, 200d);
            cube.Vertexes[3] = new Point3D(100d, 150d, 200d);
            cube.Vertexes[4] = new Point3D(100d, 100d, 250d);
            cube.Vertexes[5] = new Point3D(150d, 100d, 250d);
            cube.Vertexes[6] = new Point3D(150d, 150d, 250d);
            cube.Vertexes[7] = new Point3D(100d, 150d, 250d);
            cube.SideColors[0] = Color.FromArgb(160, 220, 160);
            cube.SideColors[1] = Color.FromArgb(160, 220, 180);
            cube.SideColors[2] = Color.FromArgb(160, 200, 180);
            cube.SideColors[3] = Color.FromArgb(160, 200, 160);
            cube.SideColors[4] = Color.FromArgb(160, 220, 160);
            cube.SideColors[5] = Color.FromArgb(120, 180, 140);

            this._GameControl.GameItems.Add(cube);
        }
    }
}
