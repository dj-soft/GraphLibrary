using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GLabel : běžný label u textů
    /// <summary>
    /// Vykreslovaný Label
    /// </summary>
    public class GLabel : GTextObject
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GLabel()
        {
            this.BackgroundMode = DrawBackgroundMode.Transparent;
            this._Alignment = ContentAlignment.MiddleLeft;
        }
        #endregion
        #region Vzhled objektu
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
        #region Public vlastnosti
        /// <summary>
        /// Obsahuje výšku řádku textu, optimální pro výšku jednořádkového labelu
        /// </summary>
        public int TextLineHeight { get { return FontManagerInfo.GetFontHeight(this.FontCurrent); } }
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
                GPainter.DrawString(e.Graphics, text, this.FontCurrent, absoluteBounds, this.Alignment, color: this.TextColorCurrent);
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
     
        

    }
    #endregion
}
