using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// P
    /// </summary>
    public class DxBufferedGraphic : DxPanelControl
    {
        public DxBufferedGraphic()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
            this.ResizeRedraw = true;
            this.LogActive = false;
        }
        public bool LogActive { get; set; }
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;
            base.OnInvalidated(e);
            if (LogActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.OnInvalidated; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;

            // base.OnPaintBackground(e);
            // e.Graphics.Clear(Color.LightGreen);
            e.Graphics.FillRectangle(Brushes.LightSkyBlue, new Rectangle(24, 48, 120, 20));

            if (LogActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.OnPaintBackground; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;

            // base.OnPaint(e);
            // e.Graphics.Clear(Color.LightSalmon);
            var size = this.Size;
            e.Graphics.DrawRectangle(System.Drawing.Pens.Violet, new System.Drawing.Rectangle(1, 1, size.Width - 3, size.Height - 3));
            
            if (LogActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.OnPaint; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }

    }
}
