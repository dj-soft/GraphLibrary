using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    public partial class TestOneComponent : Form
    {
        public TestOneComponent()
        {
            InitializeComponent();
            this.InitGComp();
        }

        private void _CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected void InitGComp()
        {
            this._Test = new GCtrlTest() { Bounds = new Rectangle(25, 10, 150, 40), BackColor = Color.LightSkyBlue };
            this._Control.AddItem(_Test);
        }
        protected GCtrlTest _Test;
    }
    public class GCtrlTest : InteractiveContainer
    {
        public GCtrlTest()
        {
            this._ResizeLeft = new ResizeItem() { Side = RectangleSide.Left, BackColor = Color.Navy };
            this._ResizeRight = new ResizeItem() { Side = RectangleSide.Right, BackColor = Color.Navy };
            this.AddItems(this._ResizeLeft, this._ResizeRight);
        }
        private ResizeItem _ResizeLeft;
        private ResizeItem _ResizeRight;
    }
}
