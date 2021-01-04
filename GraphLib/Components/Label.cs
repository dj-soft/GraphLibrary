using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class Label : běžný label u textů
    /// <summary>
    /// <see cref="Label"/> Vykreslovaný Label
    /// </summary>
    public class Label : TextObject
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Label()
        {
            this.BackgroundMode = DrawBackgroundMode.Transparent;
            this._Alignment = ContentAlignment.MiddleLeft;
        }
        #endregion
        #region Vizuální styl objektu
        /// <summary>
        /// Styl tohoto konkrétního textboxu. 
        /// Zahrnuje veškeré vizuální vlastnosti.
        /// Výchozí je null, pak se styl přebírá z <see cref="StyleParent"/>, anebo společný z <see cref="Styles.TextBox"/>.
        /// Prvním čtením této property se vytvoří new instance. Lze ji tedy kdykoliv přímo použít.
        /// <para/>
        /// Do této property se typicky vkládá new instance, která řeší vzhled jednoho konkrétního prvku.
        /// </summary>
        public LabelStyle Style { get { if (_Style == null) _Style = new LabelStyle(); return _Style; } set { _Style = null; } } private LabelStyle _Style;
        /// <summary>
        /// Obsahuje true tehdy, když <see cref="Style"/> je pro tento objekt deklarován.
        /// </summary>
        public bool HasStyle { get { return (_Style != null && !_Style.IsEmpty); } }
        /// <summary>
        /// Společný styl, deklarovaný pro více textboxů. 
        /// Zde je reference na tuto instanci. 
        /// Modifikace hodnot v této instanci se projeví ve všech ostatních textboxech, které ji sdílejí.
        /// Výchozí je null, pak se styl přebírá společný z <see cref="Styles.TextBox"/>.
        /// <para/>
        /// Do této property se typicky vkládá odkaz na instanci, která je primárně uložena jinde, a řeší vzhled ucelené skupiny prvků.
        /// </summary>
        public LabelStyle StyleParent { get; set; }
        /// <summary>
        /// Aktuální styl, nikdy není null. 
        /// Obsahuje <see cref="Style"/> ?? <see cref="StyleParent"/> ?? <see cref="Styles.TextBox"/>.
        /// </summary>
        protected ILabelStyle StyleCurrent { get { return (this._Style ?? this.StyleParent ?? Styles.Label); } }
        #endregion
        #region Výška řádku textu: defaultní, aktuální. Abstract overrides. Zajištění správné výšky objektu.
        /// <summary>
        /// Obsahuje výšku řádku textu, bez okrajů <see cref="TextBorderStyle.TextMargin"/> a bez borderu <see cref="TextBorderStyle.BorderType"/>.
        /// Pro aktuální instanci = pro její aktuální styl.
        /// </summary>
        public virtual int OneTextLineHeightCurrent { get { return Painter.GetOneTextLineHeight(this.StyleCurrent); } }
        /// <summary>
        /// Obsahuje výšku řádku textu, bez okrajů <see cref="TextBorderStyle.TextMargin"/> a bez borderu <see cref="TextBorderStyle.BorderType"/>.
        /// Pro defaultní instanci = pro výchozí styl.
        /// </summary>
        public static int OneTextLineHeightDefault { get { return Painter.GetOneTextLineHeight(Styles.Label); } }
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Umístění obsahu (textu) v rámci prostoru prvku
        /// </summary>
        public ContentAlignment Alignment { get { return _Alignment; } set { _Alignment = value; Invalidate(); } } private ContentAlignment _Alignment;
        #endregion
        #region Kreslení, Current vlastnosti
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
                Painter.DrawString(e.Graphics, text, this.FontCurrent, absoluteBounds, this.Alignment, color: this.TextColorCurrent);
        }
        /// <summary>
        /// Obsahuje aktuální barvu písma (získanou z kombinace stylů)
        /// </summary>
        protected override Color TextColorCurrent { get { return StyleCurrent.TextColor; } }
        /// <summary>
        /// Obsahuje aktuální font (získanou z kombinace stylů)
        /// </summary>
        protected override FontInfo FontCurrent { get { return StyleCurrent.Font; } }
        #endregion
    }
    #endregion
}
