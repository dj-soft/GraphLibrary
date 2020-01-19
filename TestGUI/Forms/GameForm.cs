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
            this._GameControl.GameItems.Add(new GameItemRectangle()
            {
                Bounds = new Rectangle3D
                (
                     new Point3D(100d, 110d, 0d),
                     new Point3D(130d, 200d, 0d),
                     new Point3D(230d, 210d, 0d),
                     new Point3D(210d, 100d, 0d)
                )
            });
        }
    }
}
