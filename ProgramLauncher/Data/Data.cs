using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class GroupData : BaseData
    {


    }
    public class ApplicationData : BaseData
    {
    }
    public abstract class BaseData
    {
        public virtual string ImageName { get; set; }
        public virtual byte[] ImageContent { get; set; }
        public virtual Color BackColor { get; set; }
        public virtual Color ForeColor { get; set; }
        /// <summary>
        /// Souřadnice ve virtuálním prostoru
        /// </summary>
        public virtual Point VirtualLocation { get; set; }
        /// <summary>
        /// Vnější velikost objektu.
        /// Tyto velikosti jednotlivých objektů na sebe těsně navazují.
        /// Objekt by do této velikosti měl zahrnout i mezery (okraje) mezi sousedními objekty.
        /// Pokud konkrétní potomek neřeší výšku nebo šířku, může v dané hodnotě nechat 0.
        /// </summary>
        public virtual Size Size { get; set; }

        public virtual void Paint(PaintDataEventArgs e)
        {
            var bounds = new Rectangle(this.VirtualLocation, this.Size);
            e.Graphics.SetClip(bounds);

            var mousePoint = e.MouseState.LocationNative;
            bool hasMouse = bounds.Contains(mousePoint);
            Color color = (hasMouse ? Color.FromArgb(128, 240, 200, 240) : Color.FromArgb(64, 216, 216, 216));

            var innerBounds = GetInnerBounds(bounds, 2, 2);

            using (var borderPath = GetRoundedRectangle(innerBounds, 8))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Výplň pod border:
                using (var brush = new SolidBrush(color))
                    e.Graphics.FillPath(brush, borderPath);

                // Border:
                e.Graphics.DrawPath(Pens.Gray, borderPath);

                // Zvýraznit pozici myši:
                if (hasMouse)
                {
                    e.Graphics.SetClip(innerBounds);
                    using (GraphicsPath mousePath = new GraphicsPath())
                    {
                        var mouseBounds = new Rectangle(mousePoint.X - 24, mousePoint.Y - 16, 48, 32);
                        mousePath.AddEllipse(mouseBounds);
                        using (System.Drawing.Drawing2D.PathGradientBrush pgb = new PathGradientBrush(mousePath))       // points
                        {
                            pgb.CenterPoint = mousePoint;
                            pgb.CenterColor = Color.FromArgb(220, 240, 180, 240);
                            pgb.SurroundColors = new Color[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent };
                            e.Graphics.FillPath(pgb, mousePath);
                        }
                    }
                }

                // Vykreslit Image:
                e.Graphics.ResetClip();
                e.Graphics.SmoothingMode = SmoothingMode.None;
                var image = DjSoft.Tools.ProgramLauncher.Properties.Resources.system_settings_2_48;
                var imageBounds = new Rectangle(bounds.Location, image.Size);
                e.Graphics.DrawImage(image, imageBounds);
            }

            e.Graphics.ResetClip();
        }

        #region Kreslící support
        protected Rectangle GetInnerBounds(Rectangle bounds, int dx, int dy)
        {
            int dw = dx + dx;
            int dh = dy + dy;
            if (bounds.Width <= dw || bounds.Height <= dh) return Rectangle.Empty;
            return new Rectangle(bounds.X + dx, bounds.Y + dy, bounds.Width - dw, bounds.Height - dh);
        }
        protected GraphicsPath GetRoundedRectangle(Rectangle bounds, int round)
        {
            GraphicsPath gp = new GraphicsPath();

            int minDim = (bounds.Width < bounds.Height ? bounds.Width : bounds.Height);
            int minRound = minDim / 3;
            if (round >= minRound) round = minRound;
            if (round <= 1)
            {   // Malý prostor nebo malý Round => bude to Rectangle
                gp.AddRectangle(bounds);
            }
            else
            {   // Máme bounds = vnější prostor, po něm jdou linie
                // a roundBounds = vnitřní prostor, určuje souřadnice začátku oblouku (Round):
                var roundBounds = GetInnerBounds(bounds, round, round);
                gp.AddLine(roundBounds.Left, bounds.Top, roundBounds.Right, bounds.Top);                                                                       // Horní rovná linka zleva doprava, její Left a Right jsou z Round souřadnic
                gp.AddBezier(roundBounds.Right, bounds.Top, bounds.Right, bounds.Top, bounds.Right, bounds.Top, bounds.Right, roundBounds.Top);                // Pravý horní oblouk doprava a dolů
                gp.AddLine(bounds.Right, roundBounds.Top, bounds.Right, roundBounds.Bottom);                                                                   // Pravá rovná linka zhora dolů
                gp.AddBezier(bounds.Right, roundBounds.Bottom, bounds.Right, bounds.Bottom, bounds.Right, bounds.Bottom, roundBounds.Right, bounds.Bottom);    // Pravý dolní oblouk dolů a doleva
                gp.AddLine(roundBounds.Right, bounds.Bottom, roundBounds.Left, bounds.Bottom);                                                                 // Dolní rovná linka zprava doleva
                gp.AddBezier(roundBounds.Left, bounds.Bottom, bounds.Left, bounds.Bottom, bounds.Left, bounds.Bottom, bounds.Left, roundBounds.Bottom);        // Levý dolní oblouk doleva a nahoru
                gp.AddLine(bounds.Left, roundBounds.Bottom, bounds.Left, roundBounds.Top);                                                                     // Levá rovná linka zdola nahoru
                gp.AddBezier(bounds.Left, roundBounds.Top, bounds.Left, bounds.Top, bounds.Left, bounds.Top, roundBounds.Left, bounds.Top);                    // Levý horní oblouk nahoru a doprava
                gp.CloseFigure();
            }
            return gp;
        }
        #endregion
    }
    /// <summary>
    /// Argument pro kreslení dat
    /// </summary>
    public class PaintDataEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="mouseState"></param>
        /// <param name="virtualContainer"></param>
        public PaintDataEventArgs(PaintEventArgs e, Components.MouseState mouseState, Components.IVirtualContainer virtualContainer)
        {
            __Graphics = new WeakReference<Graphics>(e.Graphics);
            __ClipRectangle = e.ClipRectangle;
            __MouseState = mouseState;
            __VirtualContainer = virtualContainer;
        }
        private WeakReference<Graphics> __Graphics;
        private Rectangle __ClipRectangle;
        private Components.MouseState __MouseState;
        private Components.IVirtualContainer __VirtualContainer;
        void IDisposable.Dispose()
        {
            __Graphics = null;
        }
        /// <summary>
        /// Gets the graphics used to paint. <br/>
        /// The System.Drawing.Graphics object used to paint. The System.Drawing.Graphics object provides methods for drawing objects on the display device.
        /// </summary>
        public Graphics Graphics { get { return (__Graphics.TryGetTarget(out var graphics) ? graphics : null); } }
        /// <summary>
        /// Gets the rectangle in which to paint. <br/>
        /// The System.Drawing.Rectangle in which to paint.
        /// </summary>
        public Rectangle ClipRectangle { get { return __ClipRectangle; } }
        /// <summary>
        /// Pozice a stav myši
        /// </summary>
        public Components.MouseState MouseState { get { return __MouseState; } }
        /// <summary>
        /// Virtuální kontejner, do kterého je kresleno
        /// </summary>
        public Components.IVirtualContainer VirtualContainer { get { return __VirtualContainer; } }
    }
}
