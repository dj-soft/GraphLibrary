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
            var brush = (bounds.Contains(e.MouseState.LocationNative) ? Brushes.LightGoldenrodYellow : Brushes.LightBlue);
            e.Graphics.FillRectangle(brush, bounds);
        }
    }

    public class PaintDataEventArgs : EventArgs, IDisposable
    {
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
