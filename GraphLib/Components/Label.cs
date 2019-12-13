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
                GPainter.DrawString(e.Graphics, text, this.Font, absoluteBounds, this.Alignment, color: this.ForeColor);
        }
    }
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
        public string Text { get; set; }

        /// <summary>
        /// Aktuální barva písma. Při čtení má vždy hodnotu (nikdy není null).
        /// Dokud není explicitně nastavena hodnota, vrací se hodnota <see cref="ForeColorDefault"/>.
        /// Lze setovat konkrétní explicitní hodnotu, anebo hodnotu null = tím se resetuje na barvu defaultní <see cref="ForeColorDefault"/>.
        /// </summary>
        public Color? ForeColor
        {
            get { return this.__ForeColor ?? this.ForeColorDefault; }
            set { this.__ForeColor = value; }
        }
        private Color? __ForeColor;
        /// <summary>
        /// Defaultní písmo
        /// </summary>
        public virtual Color ForeColorDefault { get { return Skin.Control.ControlTextColor; } }
        /// <summary>
        /// Aktuálně platný typ písma. Při čtení má vždy hodnotu (nikdy není null).
        /// Dokud není explicitně nastavena hodnota, vrací se hodnota <see cref="FontDefault"/>.
        /// Lze setovat konkrétní explicitní hodnotu, anebo hodnotu null = tím se resetuje na barvu defaultní <see cref="FontDefault"/>.
        /// </summary>
        public FontInfo Font
        {
            get { return this.__Font ?? this.FontDefault; }
            set { this.__Font = value; }
        }
        private FontInfo __Font;
        /// <summary>
        /// Defaultní písmo
        /// </summary>
        public virtual FontInfo FontDefault { get { return FontInfo.Default; } }



        /// <summary>
        /// Umístění obsahu (textu) v rámci prostoru prvku
        /// </summary>
        public ContentAlignment Alignment { get; set; }
        /// <summary>
        /// Je tento prvek Visible?
        /// </summary>
        public bool Visible { get { return this.Is.Visible; } set { this.Is.Visible = value; } }
        /// <summary>
        /// Je tento prvek Enabled?
        /// </summary>
        public bool Enabled { get { return this.Is.Enabled; } set { this.Is.Enabled = value; } }
    }
    #endregion
}
