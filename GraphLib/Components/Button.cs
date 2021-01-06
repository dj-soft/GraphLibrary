using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Obyčejné tlačítko
    /// </summary>
    public class Button : TextObject
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Button()
        {
            this.BackgroundMode = DrawBackgroundMode.Solid;
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseOverProperties
                      | InteractiveProperties.Bit.KeyboardInput
                      | InteractiveProperties.Bit.TabStop);
        }
        #endregion
        #region Vizuální styl objektu
        /// <summary>
        /// Styl tohoto konkrétního buttonu. 
        /// Zahrnuje veškeré vizuální vlastnosti.
        /// Výchozí je null, pak se styl přebírá z <see cref="StyleParent"/>, anebo společný z <see cref="Styles.Button"/>.
        /// Prvním čtením této property se vytvoří new instance. Lze ji tedy kdykoliv přímo použít.
        /// <para/>
        /// Do této property se typicky vkládá new instance, která řeší vzhled jednoho konkrétního prvku.
        /// </summary>
        public ButtonStyle Style { get { if (_Style == null) _Style = new ButtonStyle(); return _Style; } set { _Style = null; } }
        private ButtonStyle _Style;
        /// <summary>
        /// Obsahuje true tehdy, když <see cref="Style"/> je pro tento objekt deklarován.
        /// </summary>
        public bool HasStyle { get { return (_Style != null && !_Style.IsEmpty); } }
        /// <summary>
        /// Společný styl, deklarovaný pro více buttonů.
        /// Zde je reference na tuto instanci. 
        /// Modifikace hodnot v této instanci se projeví ve všech ostatních textboxech, které ji sdílejí.
        /// Výchozí je null, pak se styl přebírá společný z <see cref="Styles.Button"/>.
        /// <para/>
        /// Do této property se typicky vkládá odkaz na instanci, která je primárně uložena jinde, a řeší vzhled ucelené skupiny prvků.
        /// </summary>
        public ButtonStyle StyleParent { get; set; }
        /// <summary>
        /// Aktuální styl, nikdy není null. 
        /// Obsahuje <see cref="Style"/> ?? <see cref="StyleParent"/> ?? <see cref="Styles.Button"/>.
        /// </summary>
        protected IButtonStyle StyleCurrent { get { return (this._Style ?? this.StyleParent ?? Styles.Button); } }
        #endregion
        #region Public vlastnosti

        #endregion
        #region Kreslení, Current vlastnosti
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            var style = this.StyleCurrent;
            var borderType = style.BorderType;
            int borderWidth = Painter.GetBorderWidth(borderType);
            var interactiveState = this.InteractiveState;
            var backColor = style.GetBorderColor(interactiveState);
            var borderColor = style.GetBorderColor(interactiveState);
            Rectangle backBounds = absoluteBounds.Enlarge(-borderWidth);
            Painter.DrawAreaBase(e.Graphics, backBounds, backColor, System.Windows.Forms.Orientation.Horizontal, interactiveState);
            Painter.DrawBorder(e.Graphics, absoluteBounds, borderColor, borderType, interactiveState);
        }
        protected override Color TextColorCurrent
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        protected override FontInfo FontCurrent
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
