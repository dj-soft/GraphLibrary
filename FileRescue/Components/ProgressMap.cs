using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Djs.Tools.CopyFile.Components
{
    internal class ProgressMap : Control
    {
        public ProgressMap()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;
            this.Paint += _Paint;
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(this.ClientRectangle, System.Drawing.Color.LightBlue, System.Drawing.Color.DarkBlue, 90f))
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
        }
    }
}
