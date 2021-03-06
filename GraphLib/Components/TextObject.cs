﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class TextObject : obecný předek prvků, které zobrazují jeden text
    /// <summary>
    /// Bázová třída pro Label, Textbox atd = jeden rámeček s jedním prvkem textu, fontem a barvou textu
    /// </summary>
    public abstract class TextObject : InteractiveObject
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TextObject()
        {
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
        /// Bázová metoda <see cref="TextObject.DrawBackground(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/>
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
        /// Bázová metoda <see cref="TextObject.DrawBorder(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/> nekeslí nic.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBorder(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
        }
        /// <summary>
        /// Vykreslovaný text.
        /// Pokud bude vložena hodnota null, bude se číst jako prázdný string.
        /// </summary>
        public virtual string Text { get { return _Text ?? ""; } set { _Text = value; Invalidate(); } } private string _Text = "";
        /// <summary>
        /// Zde potomek deklaruje barvu písma
        /// </summary>
        protected abstract Color TextColorCurrent { get; }
        /// <summary>
        /// Zde potomek deklaruje typ písma
        /// </summary>
        protected abstract FontInfo FontCurrent { get; }
    }
    #endregion
}
