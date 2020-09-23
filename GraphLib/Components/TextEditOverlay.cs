using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Rozhraní pro třídy, které do <see cref="GTextEdit"/> vykreslí Overlay
    /// </summary>
    public interface ITextEditOverlay
    {
        /// <summary>
        /// Instance overlay zde může modifikovat vnitřní prostor pro text (=zmenšit <see cref="GTextEditDrawArgs.InnerBounds"/>) 
        /// a deklarovat výhradní prostor pro overlay (do <see cref="GTextEditDrawArgs.OverlayBounds"/>).
        /// Provede se to voláním metody <see cref="GTextEditDrawArgs.SetOverlayBounds(Rectangle?, Rectangle?)"/>.
        /// </summary>
        /// <param name="drawArgs"></param>
        void DetectOverlayBounds(GTextEditDrawArgs drawArgs);
        /// <summary>
        /// Vykreslí Overlay
        /// </summary>
        /// <param name="drawArgs"></param>
        void DrawOverlay(GTextEditDrawArgs drawArgs);
        /// <summary>
        /// Overlay zjistí, zda bylo kliknuto na něj (vrátí true).
        /// </summary>
        /// <param name="textEdit"></param>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        bool IsClicked(GTextEdit textEdit, Point mousePoint);
    }
    /// <summary>
    /// Přídavné vykreslení k <see cref="GTextEdit"/>, vykresluje linku podtržení u vztahu
    /// </summary>
    public class TextEditOverlayRelationLine : ITextEditOverlay
    {
        /// <summary>
        /// Defaultní konstruktor
        /// </summary>
        public TextEditOverlayRelationLine() { }
        /// <summary>
        /// Konstruktor s daty
        /// </summary>
        /// <param name="isRelationToDocument"></param>
        /// <param name="isRelationInGrid"></param>
        /// <param name="lineColor"></param>
        public TextEditOverlayRelationLine(bool isRelationToDocument, bool isRelationInGrid, Color? lineColor = null)
        {
            this.IsRelationToDocument = isRelationToDocument;
            this.IsRelationInGrid = isRelationInGrid;
            this.LineColor = lineColor;
        }
        /// <summary>
        /// Vztah na dokument = jiná barva
        /// </summary>
        public bool IsRelationToDocument { get; set; }
        /// <summary>
        /// Vykreslení v Gridu = nechává 1px rezervu
        /// </summary>
        public bool IsRelationInGrid { get; set; }
        /// <summary>
        /// Barva linky, default = null podle hodnoty <see cref="IsRelationToDocument"/> a definice Skinu
        /// </summary>
        public Color? LineColor { get; set; }
        /// <summary>
        /// Detekce prostoru Overlay
        /// </summary>
        /// <param name="drawArgs"></param>
        void ITextEditOverlay.DetectOverlayBounds(GTextEditDrawArgs drawArgs) { }
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="drawArgs"></param>
        void ITextEditOverlay.DrawOverlay(GTextEditDrawArgs drawArgs)
        {
            GPainter.DrawRelationLine(drawArgs.Graphics, drawArgs.InnerBounds, this.IsRelationToDocument, this.IsRelationInGrid, color: LineColor);
        }
        /// <summary>
        /// Jsme kliknut?
        /// </summary>
        /// <param name="textEdit"></param>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        bool ITextEditOverlay.IsClicked(GTextEdit textEdit, Point mousePoint) { return false; }
    }
    /// <summary>
    /// Přídavné vykreslení k <see cref="GTextEdit"/>, vykresluje ikonku u vztahu, vpravo
    /// </summary>
    public class TextEditOverlayRelationIcon : ITextEditOverlay
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TextEditOverlayRelationIcon() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="isRelationToDocument"></param>
        public TextEditOverlayRelationIcon(bool isRelationToDocument)
        {
            this.IsRelationToDocument = isRelationToDocument;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="image"></param>
        public TextEditOverlayRelationIcon(Image image)
        {
            this.Image = image;
        }
        /// <summary>
        /// Vztah na dokument = jiná defaultní ikonka
        /// </summary>
        public bool IsRelationToDocument { get; set; }
        /// <summary>
        /// Obrázek ikony, default = null
        /// </summary>
        public Image Image { get; set; }
        /// <summary>
        /// Detekce prostoru Overlay
        /// </summary>
        /// <param name="drawArgs"></param>
        void ITextEditOverlay.DetectOverlayBounds(GTextEditDrawArgs drawArgs)
        {
            int size = drawArgs.InnerBounds.Height - 2;
            if (size > 24) size = 24;
            Rectangle overlayBounds = new Rectangle(drawArgs.InnerBounds.Right - size, drawArgs.InnerBounds.Top, size, size);
            Rectangle textBounds = new Rectangle(drawArgs.InnerBounds.X, drawArgs.InnerBounds.Y, drawArgs.InnerBounds.Width - size, drawArgs.InnerBounds.Height);
            drawArgs.SetOverlayBounds(overlayBounds, textBounds);
        }
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="drawArgs"></param>
        void ITextEditOverlay.DrawOverlay(GTextEditDrawArgs drawArgs)
        {
            if (!drawArgs.OverlayBounds.HasValue) return;
            Rectangle overlayBounds = drawArgs.OverlayBounds.Value;
            Image image = this.Image;
            if (image == null) image = (!IsRelationToDocument ? Skin.TextBox.IconRelationRecord : Skin.TextBox.IconRelationDocument);
            if (drawArgs.HasFocus || drawArgs.InteractiveState.HasFlag(GInteractiveState.FlagOver))
                GPainter.DrawImage(drawArgs.Graphics, overlayBounds, image, drawArgs.InteractiveState);
            else
                GPainter.DrawImage(drawArgs.Graphics, overlayBounds, image, 0.45f);
        }
        /// <summary>
        /// Jsme kliknut?
        /// </summary>
        /// <param name="textEdit"></param>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        bool ITextEditOverlay.IsClicked(GTextEdit textEdit, Point mousePoint) { return false; }

    }
}
