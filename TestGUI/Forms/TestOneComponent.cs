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
            this._TestV = new GCtrlTest() { Bounds = new Rectangle(25, 10, 150, 40), BackColor = Color.LightSkyBlue, ResizeSides = RectangleSide.Vertical, ShowResizeAllways = true };
            this._TestH = new GCtrlTest() { Bounds = new Rectangle(200, 60, 100, 70), BackColor = Color.LightSeaGreen, ResizeSides = RectangleSide.Horizontal, CanUpsideDown = true };
            this._TestB = new GCtrlTest() { Bounds = new Rectangle(25, 60, 150, 70), BackColor = Color.LightGoldenrodYellow, ResizeSides = RectangleSide.Vertical | RectangleSide.Horizontal, ShowResizeAllways = true };
            this._Control.AddItem(_TestV);
            this._Control.AddItem(_TestH);
            this._Control.AddItem(_TestB);
        }
        protected GCtrlTest _TestV;
        protected GCtrlTest _TestH;
        protected GCtrlTest _TestB;
    }
    public class GCtrlTest : InteractiveContainer, IResizeObject
    {
        public GCtrlTest()
        {
            this._ResizeControl = new ResizeControl(this);
        }
        private ResizeControl _ResizeControl;
        public RectangleSide ResizeSides { get { return this._ResizeControl.ResizeSides; } set { this._ResizeControl.ResizeSides = value; } }
        public bool ShowResizeAllways { get { return this._ResizeControl.ShowResizeAllways; } set { this._ResizeControl.ShowResizeAllways = value; } }
        public bool CanUpsideDown { get { return this._ResizeControl.CanUpsideDown; } set { this._ResizeControl.CanUpsideDown = value; } }
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._ResizeControl.Childs; } }

        void IResizeObject.SetBoundsResized(Rectangle bounds, RectangleSide changedSide, DragActionType action)
        {
            if (bounds.X < 5)
            {
                int r = bounds.Right;
                bounds.X = 5;
                bounds.Width = r - 5;
            }
            if (bounds.Right > (this.Parent.ClientSize.Width - 5))
                bounds.Width = this.Parent.ClientSize.Width - 5 - bounds.X;

            this.Bounds = bounds;
            this.Parent.Repaint();
        }
    }
}
