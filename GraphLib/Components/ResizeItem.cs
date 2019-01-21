using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Prvek, který slouží jiným vizuálním prvkům jako "Resizer" = aktivní hrana, která dovoluje změnit některou souřadnici prvku
    /// </summary>
    public class ResizeItem : InteractiveObject, IInteractiveItem
    {
        public ResizeItem()
            :base()
        {
            this._Side = RectangleSide.None;
            this.InteractivePadding = new Padding(2);
        }
        public override Rectangle Bounds
        {
            get
            {
                Size parentSize = this.ParentSize;
                int size = (this.HasMouse ? 3 : 1);
                switch (this.Side)
                {
                    case RectangleSide.Left: return new Rectangle(0, 0, size, parentSize.Height);
                    case RectangleSide.Right: return new Rectangle(parentSize.Width - size, 0, size, parentSize.Height);
                    case RectangleSide.Top: return new Rectangle(0, 0, parentSize.Width, size);
                    case RectangleSide.Bottom: return new Rectangle(0, parentSize.Height - size, parentSize.Width, size);
                }
                return new Rectangle(0, 0, 0, 0);
            }
            set { }
        }
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        public RectangleSide Side { get { return this._Side; } set { this._Side = value; this.Repaint(); } } private RectangleSide _Side;
        protected Size ParentSize { get { IInteractiveItem iParent = this.IParent; return (iParent != null ? iParent.Bounds.Size : new Size(10, 10)); } }
        protected IInteractiveItem IParent { get { return (this.Parent as IInteractiveItem); } }

        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseEnter(e);
            e.RequiredCursorType = SysCursorType.VSplit;
        }
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            e.RequiredCursorType = SysCursorType.Default;
        }
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Color backColor = this.BackColor;
            if (!this.HasMouse) backColor = backColor.SetOpacity(96);

            GPainter.DrawAreaBase(e.Graphics, absoluteBounds, backColor, Orientation.Vertical, this.InteractiveState);

            // base.Draw(e, absoluteBounds, absoluteVisibleBounds, drawMode);

            IInteractiveItem iii = this.Parent as IInteractiveItem;
            var bounds = iii.Bounds;
        }
    }
}
