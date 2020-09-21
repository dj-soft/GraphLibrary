using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GLabel : běžný label u textů

    #endregion
    /// <summary>
    /// Vykreslovaný Label
    /// </summary>
    public class GLabel : GTextObject
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GLabel()
        {
            this.BackgroundMode = DrawBackgroundMode.Transparent;
        }
        /// <summary>
        /// Vykreslí text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void DrawText(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            string text = this.Text;
            if (!String.IsNullOrEmpty(text))
                GPainter.DrawString(e.Graphics, text, this.CurrentFont, absoluteBounds, this.Alignment, color: this.TextColor.Value);
        }
    }
}
