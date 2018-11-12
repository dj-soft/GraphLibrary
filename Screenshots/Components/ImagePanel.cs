using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Djs.Tools.Screenshots.Components
{
    public class ImagePanel : Panel
    {
        public ImagePanel()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
        public Image Image { get { return this._Image; }  set { this._Image = value; this.Invalidate(); } }
        private Image _Image;
        protected override void OnPaint(PaintEventArgs e)
        {
            // base.OnPaint(e);
            if (this._Image != null)
                e.Graphics.DrawImage(this._Image, this.ClientRectangle);
        }
    }
}
