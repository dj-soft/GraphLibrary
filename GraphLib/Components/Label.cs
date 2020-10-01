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
        /// Barva písma dynamicky zadaná. Default je null: barva se bere ze standardní <see cref="GTextObject.TextColor"/>.
        /// Používá se pro dynamické změny, například při změně Focusu do containeru s tímto Labelem.
        /// </summary>
        public Color? TextColorDynamic { get { return __TextColorDynamic; } set { __TextColorDynamic = value; Invalidate(); } }
        private Color? __TextColorDynamic = null;
        /// <summary>
        /// Dynamické změny fontu proti defaultnímu.
        /// Autoinicializační property - lze rovnou napsat: TextObject.FontModifier.Bold = true;
        /// Používá se pro dynamické změny, například při změně Focusu do containeru s tímto Labelem.
        /// </summary>
        public FontModifierInfo FontDynamicModifier { get { if (__FontDynamicModifier == null) __FontDynamicModifier = FontModifierInfo.Empty; return __FontDynamicModifier; } set { __FontDynamicModifier = value; Invalidate(); } }
        private FontModifierInfo __FontDynamicModifier = null;
        /// <summary>
        /// Obsahuje true, pokud v this instanci máme použitý modifikátor fontu
        /// </summary>
        protected bool HasFontDynamicModifier { get { return (__FontDynamicModifier != null && !__FontDynamicModifier.IsEmpty); } }

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
                GPainter.DrawString(e.Graphics, text, this.CurrentFont, absoluteBounds, this.Alignment, color: this.CurrentTextColor);
        }
        /// <summary>
        /// Obsahuje aktuální barvu písma (<see cref="TextColorDynamic"/> nebo default <see cref="GTextObject.CurrentTextColor"/>)
        /// </summary>
        protected override Color CurrentTextColor
        {
            get
            {
                return __TextColorDynamic ?? base.CurrentTextColor;
            }
        }
        /// <summary>
        /// Obsahuje aktuální font = daný základem <see cref="GTextObject.CurrentFont"/> plus lokální modifikátor <see cref="FontDynamicModifier"/>.
        /// </summary>
        protected override FontInfo CurrentFont
        {
            get
            {
                FontInfo font = base.CurrentFont;
                if (HasFontDynamicModifier)
                    font = font.GetModifiedFont(__FontDynamicModifier);
                return font;
            }
        }
    }
}
