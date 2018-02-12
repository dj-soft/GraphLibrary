using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Djs.Common.Components
{
    public abstract class VirtualInteractiveContainer : InteractiveContainer, IInteractiveItem
    {
        public void Add(VirtualInteractiveItem item) { this.ChildList.Add(item); }
        public void AddRange(IEnumerable<VirtualInteractiveItem> items) { this.ChildList.AddRange(items); }
        public void AddRange(params VirtualInteractiveItem[] items) { this.ChildList.AddRange(items); }
        /// <summary>
        /// Invalidate ActualBounds on all virtual items
        /// </summary>
        public void InvalidateItems()
        {
            foreach (var item in this.VirtualItems)
                item.InvalidateActiveBounds();
        }
        /// <summary>
        /// Return a collection of items of type VirtualInteractiveItem
        /// </summary>
        protected IEnumerable<VirtualInteractiveItem> VirtualItems { get { return this.ChildList.OfType<VirtualInteractiveItem>(); } }
    }
    public abstract class VirtualInteractiveItem : InteractiveObject, IInteractiveItem
    {
        #region Virtual item memebers (constructor, VirtualBounds and ActiveBounds, validity check and calculators)
        public VirtualInteractiveItem(IVirtualConvertor virtualConvertor)
        {
            this._VirtualConvertor = virtualConvertor;
        }
        /// <summary>
        /// Virtual convertor
        /// </summary>
        public IVirtualConvertor VirtualConvertor { get { return this._VirtualConvertor; } set { this._VirtualConvertor = value; this.InvalidateActiveBounds(); } }
        private IVirtualConvertor _VirtualConvertor;
        /// <summary>
        /// Virtual bounds (logical value).
        /// It is basical value.
        /// </summary>
        public RectangleD VirtualBounds
        {
            get { return this._VirtualBounds; } 
            set { this._VirtualBounds = value; this.InvalidateActiveBounds(); }
        }
        private RectangleD _VirtualBounds;
        /// <summary>
        /// Store value to this._VirtualBounds from base.ActiveBounds (via this._VirtualConvertor).
        /// Set this._LastIdentity (from this._VirtualConvertor.Identity).
        /// </summary>
        private void _CalcVirtualBounds()
        {
            if (this._VirtualConvertor == null || !this._VirtualConvertor.ConvertIsReady) return;
            this._VirtualBounds = this._VirtualConvertor.ConvertToLogicalFromPixel(base.BoundsActive).Value;
            this._LastIdentity = this._VirtualConvertor.Identity;
        }
        /// <summary>
        /// Last identity of virtual convertor
        /// </summary>
        private IComparable _LastIdentity;
        /// <summary>
        /// true when this.ActiveBounds = this._VirtualConvertor.ConvertToPixelFromLogical(this.VirtualBounds)
        /// (work also with this._LastIdentity and this._VirtualConvertor.Identity)
        /// </summary>
        private bool _BoundsIsValid
        {
            get
            {
                if (this._VirtualConvertor == null || !this._VirtualConvertor.ConvertIsReady) return true;     // Without a convertor = valid
                IComparable currentIdentity = this._VirtualConvertor.Identity;
                if (currentIdentity == null) return true;
                if (this._LastIdentity == null) return false;
                return (this._LastIdentity.CompareTo(currentIdentity) == 0);
            }
        }
        /// <summary>
        /// Invalidate ActiveBounds
        /// </summary>
        public void InvalidateActiveBounds()
        {
            this._LastIdentity = null;
        }
        #endregion
    }
    /// <summary>
    /// Interface for object, which can convert logical to physical values
    /// </summary>
    public interface IVirtualConvertor
    {
        /// <summary>
        /// Return visible bounds (Pixel) from logical bounds (Value).
        /// Booth bounds are absolute.
        /// </summary>
        /// <param name="logicalBounds"></param>
        /// <returns></returns>
        Rectangle? ConvertToPixelFromLogical(RectangleD logicalBounds);
        /// <summary>
        /// Return visible point (Pixel) from logical point (Value).
        /// Booth bounds are absolute.
        /// </summary>
        /// <param name="logicalPoint"></param>
        /// <returns></returns>
        Point? ConvertToPixelFromLogical(PointD logicalPoint);
        /// <summary>
        /// Return visible size (Pixel) from logical size (Value).
        /// </summary>
        /// <param name="logicalSize"></param>
        /// <returns></returns>
        Size? ConvertToPixelFromLogical(SizeD logicalSize);
        /// <summary>
        /// Return logical bounds (Value) from visible bounds (Pixel).
        /// Booth bounds are absolute.
        /// </summary>
        /// <param name="pixelBounds"></param>
        /// <returns></returns>
        RectangleD? ConvertToLogicalFromPixel(Rectangle pixelBounds);
        /// <summary>
        /// Return logical point (Value) from visible point (Pixel).
        /// Booth bounds are absolute.
        /// </summary>
        /// <param name="pixelPoint"></param>
        /// <returns></returns>
        PointD? ConvertToLogicalFromPixel(Point pixelPoint);
        /// <summary>
        /// Return logical size (Value) from visible size (Pixel).
        /// </summary>
        /// <param name="pixelSize"></param>
        /// <returns></returns>
        SizeD? ConvertToLogicalFromPixel(Size pixelSize);
        /// <summary>
        /// true when convertor is ready to use
        /// </summary>
        bool ConvertIsReady { get; }
        /// <summary>
        /// Identity of scale and offsets for booth axis.
        /// </summary>
        IComparable Identity { get; }
    }
}
