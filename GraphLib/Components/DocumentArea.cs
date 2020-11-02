using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Prostor pro dokument
    /// </summary>
    public class GDocumentArea : VirtualInteractiveContainer, IInteractiveItem, IVirtualConvertor
    {
        #region Constructor, public properties and events
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GDocumentArea()
        {
            this.DocumentSize = new SizeD(210m, 297m);
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseProperties);

            GVirtualMovableItem gea = new GVirtualMovableItem(this) { VirtualBounds = new Rectangle(0, 0, 100, 50), BackColor = Color.Red };
            this.AddItem(gea);
        }
        #endregion
        #region Bounds
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, without any conditions (even if action is None).
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsAfterChange(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            this._SetMaximalBounds();
        }
        #endregion
        #region Interactive property and methods
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            if (e.ChangeState == GInteractiveChangeState.LeftDragMoveStep)
            { }
        }
        #endregion
        #region Draw to Graphic
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="e"></param>
        protected override void PaintBackground(GInteractiveDrawArgs e)
        {
            base.PaintBackground(e);
        }
        /// <summary>
        /// Draw current size axis to specified graphic.
        /// Coordinates are this.Area.
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
            {
                try
                {
                    e.Graphics.SetClip(absoluteBounds);
                    this._PaintBackground(e.Graphics, absoluteBounds);
                    this.PaintSheet(e.Graphics, absoluteBounds);
                    base.Draw(e, absoluteBounds, absoluteVisibleBounds);
                }
                finally
                {
                    e.Graphics.ResetClip();
                }
            }
        }
        private void _PaintBackground(Graphics graphics, Rectangle bounds)
        {
            graphics.FillRectangle(Brushes.DarkSlateGray, bounds);
        }
        #endregion
        #region IVirtualConvertor Members => protected abstract method for convert logical to/from pixel
        /// <summary>
        /// Return visible bounds (in Pixels) from logical bounds (Value).
        /// Returned Bounds are relative to Axis parent coordinates.
        /// </summary>
        /// <param name="logicalBounds"></param>
        /// <returns></returns>
        protected virtual Rectangle? ConvertToPixelFromLogical(RectangleD logicalBounds)
        {
            if (!this.ConvertIsReady) return null;
            Int32? l = this.SizeAxisHorizontal.CalculatePixelRelativeForTick(logicalBounds.X);
            Int32? r = this.SizeAxisHorizontal.CalculatePixelRelativeForTick(logicalBounds.Right);
            Int32? t = this.SizeAxisVertical.CalculatePixelRelativeForTick(logicalBounds.Y);
            Int32? b = this.SizeAxisVertical.CalculatePixelRelativeForTick(logicalBounds.Bottom);
            return ((l.HasValue && r.HasValue && t.HasValue && b.HasValue) ? (Rectangle?)(new Rectangle(l.Value, t.Value, (r.Value - l.Value), (b.Value - t.Value))) : (Rectangle?)null);
        }
        /// <summary>
        /// Return visible point (in Pixels) from logical point (Value).
        /// Returned Bounds are relative to Axis parent coordinates.
        /// </summary>
        /// <param name="logicalPoint"></param>
        /// <returns></returns>
        protected virtual Point? ConvertToPixelFromLogical(PointD logicalPoint)
        {
            if (!this.ConvertIsReady) return null;
            Int32? l = this.SizeAxisHorizontal.CalculatePixelRelativeForTick(logicalPoint.X);
            Int32? t = this.SizeAxisVertical.CalculatePixelRelativeForTick(logicalPoint.Y);
            return ((l.HasValue && t.HasValue) ? (Point?)(new Point(l.Value, t.Value)) : (Point?)null);
        }
        /// <summary>
        /// Return visible size (in Pixel) from logical size (Value).
        /// </summary>
        /// <param name="logicalSize"></param>
        /// <returns></returns>
        protected virtual Size? ConvertToPixelFromLogical(SizeD logicalSize)
        {
            if (!this.ConvertIsReady) return null;
            Int32? w = this.SizeAxisHorizontal.CalculatePixelDistanceForTSize(logicalSize.Width);
            Int32? h = this.SizeAxisVertical.CalculatePixelDistanceForTSize(logicalSize.Height);
            return ((w.HasValue && h.HasValue) ? (Size?)(new Size(w.Value, h.Value)) : (Size?)null);
        }

        /// <summary>
        /// Return logical bounds (Value) from visible bounds (Pixel).
        /// Input Bounds are relative to Axis parent coordinates.
        /// </summary>
        /// <param name="pixelBounds"></param>
        /// <returns></returns>
        protected virtual RectangleD? ConvertToLogicalFromPixel(Rectangle pixelBounds)
        {
            if (!this.ConvertIsReady) return null;
            Point p1 = pixelBounds.Location;
            Point p2 = new Point(pixelBounds.Right, pixelBounds.Bottom);
            Decimal? l = this.SizeAxisHorizontal.CalculateTickForPixelRelative(p1);
            Decimal? r = this.SizeAxisHorizontal.CalculateTickForPixelRelative(p2);
            Decimal? t = this.SizeAxisVertical.CalculateTickForPixelRelative(p1);
            Decimal? b = this.SizeAxisVertical.CalculateTickForPixelRelative(p2);
            return ((l.HasValue && r.HasValue && t.HasValue && b.HasValue) ? (RectangleD?)(new RectangleD(l.Value, t.Value, (r.Value - l.Value), (b.Value - t.Value))) : (RectangleD?)null);
        }
        /// <summary>
        /// Return logical point (Value) from visible point (Pixel).
        /// Input Point is relative to Axis parent coordinates.
        /// </summary>
        /// <param name="pixelPoint"></param>
        /// <returns></returns>
        protected virtual PointD? ConvertToLogicalFromPixel(Point pixelPoint)
        {
            if (!this.ConvertIsReady) return null;
            Decimal? l = this.SizeAxisHorizontal.CalculateTickForPixelRelative(pixelPoint);
            Decimal? t = this.SizeAxisVertical.CalculateTickForPixelRelative(pixelPoint);
            return ((l.HasValue && t.HasValue) ? (PointD?)(new PointD(l.Value, t.Value)) : (PointD?)null);
        }
        /// <summary>
        /// Return logical size (Value) from visible size (Pixel).
        /// </summary>
        /// <param name="pixelSize"></param>
        /// <returns></returns>
        protected virtual SizeD? ConvertToLogicalFromPixel(Size pixelSize)
        {
            if (!this.ConvertIsReady) return null;
            Decimal? w = this.SizeAxisHorizontal.CalculateTSizeFromPixelDistance(pixelSize.Width);
            Decimal? h = this.SizeAxisVertical.CalculateTSizeFromPixelDistance(pixelSize.Height);
            return ((w.HasValue && h.HasValue) ? (SizeD?)(new SizeD(w.Value, h.Value)) : (SizeD?)null);
        }
        /// <summary>
        /// true when convertor is ready to use
        /// </summary>
        protected virtual bool ConvertIsReady
        {
            get
            {
                if (this._SizeAxisHorizontal == null || !this._SizeAxisHorizontal.IsValid) return false;
                if (this._SizeAxisVertical == null || !this._SizeAxisVertical.IsValid) return false;
                return true;
            }
        }
        /// <summary>
        /// Identity of scale and offsets for booth axis.
        /// </summary>
        protected virtual IComparable ConvertIdentity
        {
            get
            {
                if (!this.ConvertIsReady) return null;
                return this.SizeAxisHorizontal.Identity + ";" + this.SizeAxisVertical.Identity;
            }
        }

        /// <summary>
        /// Reference to horizontal axis, for Convert values and pixel
        /// </summary>
        public GSizeAxis SizeAxisHorizontal { get { return this._SizeAxisHorizontal; } set { this._SizeAxisHorizontal = value; } } private GSizeAxis _SizeAxisHorizontal;
        /// <summary>
        /// Reference to vertical axis, for Convert values and pixel
        /// </summary>
        public GSizeAxis SizeAxisVertical { get { return this._SizeAxisVertical; } set { this._SizeAxisVertical = value; } } private GSizeAxis _SizeAxisVertical;
       
        Rectangle? IVirtualConvertor.ConvertToPixelFromLogical(RectangleD logicalBounds) { return this.ConvertToPixelFromLogical(logicalBounds); }
        Point? IVirtualConvertor.ConvertToPixelFromLogical(PointD logicalPoint) { return this.ConvertToPixelFromLogical(logicalPoint); }
        Size? IVirtualConvertor.ConvertToPixelFromLogical(SizeD logicalSize) { return this.ConvertToPixelFromLogical(logicalSize); }
        RectangleD? IVirtualConvertor.ConvertToLogicalFromPixel(Rectangle pixelBounds) { return this.ConvertToLogicalFromPixel(pixelBounds); }
        PointD? IVirtualConvertor.ConvertToLogicalFromPixel(Point pixelPoint) { return this.ConvertToLogicalFromPixel(pixelPoint); }
        SizeD? IVirtualConvertor.ConvertToLogicalFromPixel(Size pixelSize) { return this.ConvertToLogicalFromPixel(pixelSize); }
        bool IVirtualConvertor.ConvertIsReady  { get { return this.ConvertIsReady; } }
        IComparable IVirtualConvertor.Identity { get { return this.ConvertIdentity; } }
        #endregion
        #region Document: Size, Bounds, PaintSheet
        /// <summary>
        /// Size of document in logical units (milimeter)
        /// </summary>
        public SizeD DocumentSize { get { return this._DocumentSize; } set { this._SetDocumentSize(value); } } private SizeD _DocumentSize;
        /// <summary>
        /// Bounds of document in logical units (milimeter), Location = {0,0}, Size = this.DocumentSize
        /// </summary>
        public RectangleD DocumentBounds { get { return this._DocumentBounds; } set { this._SetDocumentBounds(value); } } private RectangleD _DocumentBounds;
        /// <summary>
        /// Maximal visible bounds in logical units (milimeter), on current scale
        /// </summary>
        public RectangleD MaximalBounds { get { return this._MaximalBounds; } } private RectangleD _MaximalBounds;

        private void _SetDocumentSize(SizeD size)
        {
            this._DocumentSize = size;
            this.DocumentBounds = new RectangleD(PointD.Empty, size); 
        }
        private void _SetDocumentBounds(RectangleD bounds)
        {
            this._DocumentBounds = bounds;
            this._SetMaximalBounds();
        }
        /// <summary>
        /// Calculate maximal logical bounds to this.MaximalBounds = bounds for current DocumentBounds (+small border = 15mm) and current Bounds (its ratio).
        /// </summary>
        private void _SetMaximalBounds()
        {
            if (this.DocumentSize.Width <= 0m || this.DocumentSize.Height <= 0m || this.Bounds.Width <= 0 || this.Bounds.Height <= 0) return;

            // Maximal logical bounds by document + 15mm, ratio Width/Height:
            RectangleD maxBounds = this._DocumentBounds;
            maxBounds.Inflate(10m, 10m);
            decimal docRatio = maxBounds.Width / maxBounds.Height;

            RectangleD pixBounds = this.Bounds;
            decimal pixRatio = pixBounds.Width / pixBounds.Height;

            if (docRatio < pixRatio)
            {   // Document.Width < Bounds.Width (by ratio W:H), must enlarge width to height * pixRatio:
                decimal width = pixRatio * maxBounds.Height;
                maxBounds.Inflate((width - maxBounds.Width) / 2m, 0m);
            }
            else
            {   // Document.Height < Bounds.Height (by ratio W:H)
                decimal height = maxBounds.Width / pixRatio;
                maxBounds.Inflate(0m, (height - maxBounds.Height) / 2m);
            }

            if (this._MaximalBounds == null || maxBounds != this._MaximalBounds)
            {   // After change: store + trigger event:
                this._MaximalBounds = maxBounds;
                this._OnMaximalBoundsChanged();
            }
        }
        /// <summary>
        /// Event triggered after change value of property MaximalBounds
        /// </summary>
        public event EventHandler MaximalBoundsChanged;
        private void _OnMaximalBoundsChanged()
        {
            this.OnMaximalBoundsChanged();
            if (this.MaximalBoundsChanged != null)
                this.MaximalBoundsChanged(this, EventArgs.Empty);
        }
        /// <summary>
        /// Po změně Bounds
        /// </summary>
        protected virtual void OnMaximalBoundsChanged()
        { }
        /// <summary>
        /// Vykreslí list
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        protected void PaintSheet(Graphics graphics, Rectangle bounds)
        {
            RectangleD documentL = this.DocumentBounds;

            RectangleD shadowL = documentL;
            shadowL.Move(2.5m, 2.5m);
            Rectangle? shadowP = this.ConvertToPixelFromLogical(shadowL);
            if (!shadowP.HasValue) return;
            graphics.FillRectangle(Brushes.Black, shadowP.Value);

            Rectangle? documentP = this.ConvertToPixelFromLogical(documentL);
            if (!documentP.HasValue) return;
            graphics.FillRectangle(Brushes.White, documentP.Value);


#warning DOPLNIT OBRÁZKY :
            /*
            Image img = IconLib.Image("zoom-out-5", 32);
            if (img != null)
                graphics.DrawImage(img, new Point(260, 300));

            img = IconLib.Image("zoom-out-5", 16);
            if (img != null)
                graphics.DrawImage(img, new Point(310, 300));

            img = IconLib.Image("zoom-out-5", 40);
            if (img != null)
                graphics.DrawImage(img, new Point(370, 300));

            img = IconLib.Image("lock-6", 20);
            if (img != null)
                graphics.DrawImage(img, new Point(415, 300));
            img = IconLib.Image("lock-6", 30);
            if (img != null)
                graphics.DrawImage(img, new Point(450, 300));
            img = IconLib.Image("zoom-fit-best-2", 30);
            if (img != null)
                graphics.DrawImage(img, new Rectangle(new Point(500, 300), new Size(30,30)));
            */
        }
        #endregion
    }
}
