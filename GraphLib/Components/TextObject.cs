using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GTextObject : obecný předek prvků, které zobrazují jeden text
    /// <summary>
    /// Bázová třída pro Label, Textbox atd = jeden rámeček s jedním prvkem textu, fontem a barvou textu
    /// </summary>
    public abstract class GTextObject : InteractiveObject
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTextObject()
        {
            this.BackgroundMode = DrawBackgroundMode.Transparent;
            this.Alignment = ContentAlignment.MiddleLeft;
        }
        /// <summary>
        /// Vykreslí text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.DrawBackground(e, absoluteBounds, absoluteVisibleBounds, drawMode);     // Background
            this.DrawText(e, absoluteBounds, absoluteVisibleBounds, drawMode);           // Text
            this.DrawBorder(e, absoluteBounds, absoluteVisibleBounds, drawMode);         // Rámeček
        }
        /// <summary>
        /// Vykreslí pozadí. 
        /// Bázová metoda <see cref="GTextObject.DrawBackground(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/>
        /// vyvolá prosté kreslení pozadí <see cref="InteractiveObject.Draw(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            base.Draw(e, absoluteBounds, absoluteVisibleBounds, drawMode);               // Background
        }
        /// <summary>
        /// Vykreslí text.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawText(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        { }
        /// <summary>
        /// Vykreslí rámeček. 
        /// Bázová metoda <see cref="GTextObject.DrawBorder(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/> nekeslí nic.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBorder(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
        }

        /// <summary>
        /// Vykreslovaný text
        /// </summary>
        public string Text { get { return _Text; } set { _Text = value; Invalidate(); } }
        private string _Text;


        /// <summary>
        /// Barva písma základní.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? TextColor { get { return __TextColor ?? TextColorDefault; } set { __TextColor = value; Invalidate(); } }
        private Color? __TextColor = null;
        /// <summary>
        /// Defaultní barva písma.
        /// </summary>
        protected virtual Color TextColorDefault { get { return Skin.Control.ControlTextColor; } }
        /// <summary>
        /// Aktuálně platný typ písma. Při čtení má vždy hodnotu (nikdy není null).
        /// Dokud není explicitně nastavena hodnota, vrací se hodnota <see cref="FontDefault"/>.
        /// Lze setovat konkrétní explicitní hodnotu, anebo hodnotu null = tím se resetuje na barvu defaultní <see cref="FontDefault"/>.
        /// </summary>
        public FontInfo Font { get { return __Font ?? FontDefault; } set { __Font = value; Invalidate(); } }
        private FontInfo __Font;
        /// <summary>
        /// Defaultní písmo
        /// </summary>
        protected virtual FontInfo FontDefault { get { return FontInfo.Default; } }
        /// <summary>
        /// Obsahuje aktuální font = daný základem <see cref="Font"/> plus modifikátor <see cref="FontModifier"/>.
        /// </summary>
        protected FontInfo CurrentFont
        {
            get
            {
                FontInfo font = Font;
                if (HasFontModifier)
                    font = font.GetModifiedFont(__FontModifier);
                return font;
            }
        }
        /// <summary>
        /// Změny fontu proti defaultnímu.
        /// Autoinicializační property - lze rovnou napsat: TextObject.FontModifier.Bold = true;
        /// </summary>
        public FontModifierInfo FontModifier { get { if (__FontModifier == null) __FontModifier = FontModifierInfo.Empty; return __FontModifier; } set { __FontModifier = value; Invalidate(); } }
        private FontModifierInfo __FontModifier = null;
        /// <summary>
        /// Obsahuje true, pokud v this instanci máme použitý modifikátor fontu
        /// </summary>
        protected bool HasFontModifier { get { return (__FontModifier != null && !__FontModifier.IsEmpty); } }

        /// <summary>
        /// Umístění obsahu (textu) v rámci prostoru prvku
        /// </summary>
        public ContentAlignment Alignment { get { return _Alignment; } set { _Alignment = value; Invalidate(); } }
        private ContentAlignment _Alignment;
        /// <summary>
        /// Je tento prvek Visible?
        /// </summary>
        public bool Visible { get { return this.Is.Visible; } set { this.Is.Visible = value; } }
        /// <summary>
        /// Je tento prvek Enabled?
        /// Do prvku, který NENÍ Enabled, nelze vstoupit Focusem (ani provést DoubleClick ani na ikoně / overlay).
        /// </summary>
        public bool Enabled { get { return this.Is.Enabled; } set { this.Is.Enabled = value; } }
        /// <summary>
        /// Je tento prvek ReadOnly?
        /// Do prvku, který JE ReadOnly, lze vstoupit Focusem, lze provést DoubleClick včetně ikony / overlay.
        /// Ale nelze prvek editovat, a má vzhled prvku který není Enabled (=typicky má šedou barvu a nereaguje vizuálně na myš).
        /// </summary>
        public bool ReadOnly { get { return this.Is.ReadOnly; } set { this.Is.ReadOnly = value; } }
    }
    #endregion
}
