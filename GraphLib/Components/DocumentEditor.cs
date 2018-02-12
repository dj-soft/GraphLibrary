using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Djs.Common.Data;

namespace Djs.Common.Components
{
    public class GDocumentEditor : GInteractiveControl
    {
        public GDocumentEditor()
        {
            this.ComponentsInit();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            this.ComponentLayout();
            base.OnSizeChanged(e);
        }
        #region Components
        protected void ComponentsInit()
        {
            this.DocumentArea = new GDocumentArea() { DocumentSize = new SizeD(149, 210) };
            this.AxisH = new GSizeAxis() { Orientation = AxisOrientation.Top };
            this.SplitterH = new GSplitter() { Orientation = Orientation.Horizontal, SplitterActiveOverlap = 2, ValueRange = new Int32Range(18, 60) };
            this.ScrollH = new GScrollBar() { Orientation = Orientation.Horizontal, ValueTotal = new SizeRange(-25m, 235m), Value = new SizeRange(50, 200) };
            this.AxisV = new GSizeAxis() { Orientation = AxisOrientation.LeftDown };
            this.SplitterV = new GSplitter() { Orientation = Orientation.Vertical, SplitterActiveOverlap = 2, ValueRange = new Int32Range(30, 90) };
            this.ScrollV = new GScrollBar() { Orientation = Orientation.Vertical, ValueTotal = new SizeRange(-25m, 315m), Value = new SizeRange(50, 250) };

            this.SplitterHPos = 30;
            this.SplitterVPos = 50;

            this.DocumentArea.SizeAxisHorizontal = this.AxisH;
            this.DocumentArea.SizeAxisVertical = this.AxisV;
            this.ComponentLayout();
            this.AcceptMaximalBoundsFromDocument(true);

            this.SplitterH.ValueChanged += new GPropertyChanged<int>(SplitterH_LocationChanging);
            this.SplitterV.ValueChanged += new GPropertyChanged<int>(SplitterV_LocationChanging);
            this.AxisH.ValueChanged += new GPropertyChanged<SizeRange>(AxisH_ValueChanged);
            this.AxisV.ValueChanged += new GPropertyChanged<SizeRange>(AxisV_ValueChanged);
            this.ScrollH.ValueChanged += new GPropertyChanged<SizeRange>(ScrollH_ValueChanged);
            this.ScrollV.ValueChanged += new GPropertyChanged<SizeRange>(ScrollV_ValueChanged);
            this.DocumentArea.MaximalBoundsChanged += new EventHandler(DocumentArea_MaximalBoundsChanged);

            this.AddItems(DocumentArea, AxisH, ScrollH, AxisV, ScrollV, SplitterH, SplitterV);

            this.Draw();
        }
        /// <summary>
        /// Calculate coordinates for all components
        /// </summary>
        private void ComponentLayout()
        {
            Rectangle ea = this.ClientRectangle;
            if (this.SplitterH == null || this.SplitterV == null) return;
            int sx = this.SplitterVPos;
            int sy = this.SplitterHPos;
            int er = ea.Right;
            int ew = er - sx;
            int eb = ea.Bottom;
            int eh = eb - sy;

            int sw = 14;
            this.AxisH.Bounds = new Rectangle(sx, ea.Y, ew - sw, sy - ea.Y - 2);
            this.SplitterH.BoundsNonActive = new Int32Range(sx, er);
            this.ScrollH.Bounds = new Rectangle(sx, ea.Bottom - sw, ew - sw, sw);

            this.AxisV.Bounds = new Rectangle(ea.X, sy, sx - ea.X - 2, eh - sw);
            this.SplitterV.BoundsNonActive = new Int32Range(sy, eb);
            this.ScrollV.Bounds = new Rectangle(ea.Right - sw, sy, sw, eh - sw);

            this.DocumentArea.Bounds = new Rectangle(sx, sy, ew - sw - 1, eh - sw - 1);
        }
        /// <summary>
        /// Read .MaximalBounds from DocumentArea, and create from its value for ValueMax to Axis and Scroll.
        /// </summary>
        private void AcceptMaximalBoundsFromDocument()
        {
            this.AcceptMaximalBoundsFromDocument(false);
        }
        /// <summary>
        /// Read .MaximalBounds from DocumentArea, and create from its value for ValueMax to Axis and Scroll.
        /// </summary>
        private void AcceptMaximalBoundsFromDocument(bool force)
        {
            if (this._SuppressEvents) return;
            using (_SuppressedEvents.Scope(this))
            {
                RectangleD max = this.DocumentArea.MaximalBounds;
                if (max.Width <= 0m || max.Height <= 0m) return;

                SizeRange maxH = new SizeRange(max.Left, max.Right);
                this.AxisH.ValueLimit = maxH.Clone;
                this.ScrollH.ValueTotal = maxH.Clone;

                SizeRange maxV = new SizeRange(max.Top, max.Bottom);
                this.AxisV.ValueLimit = maxV.Clone;
                this.ScrollV.ValueTotal = maxV.Clone;

                SizeRange value;
                if (MustChangeValue(this.AxisH.Value, maxH, force, out value)) this.AxisH.Value = value;
                if (MustChangeValue(this.ScrollH.Value, maxH, force, out value)) this.ScrollH.Value = value;
                if (MustChangeValue(this.AxisV.Value, maxV, force, out value)) this.AxisV.Value = value;
                if (MustChangeValue(this.ScrollV.Value, maxV, force, out value)) this.ScrollV.Value = value;
            }
        }
        /// <summary>
        /// Return true, when currentValue must be change, to fit to specified maxValue.
        /// Then store modified value to out newValue.
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        private bool MustChangeValue(SizeRange currentValue, SizeRange maxValue, bool force, out SizeRange newValue)
        {
            newValue = null;
            if (!currentValue.IsFilled || !maxValue.IsFilled) return false;
            if (force)
            {
                newValue = maxValue.Clone;
                return true;
            }
            newValue = currentValue * maxValue;
            return (newValue != currentValue);
        }
        private void SplitterH_LocationChanging(object sender, GPropertyChangeArgs<int> e)
        {
            this.ComponentLayout();
        }
        private void SplitterV_LocationChanging(object sender, GPropertyChangeArgs<int> e)
        {
            this.ComponentLayout();
        }
        private void AxisH_ValueChanged(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            if (this._SuppressEvents) return;
            using (_SuppressedEvents.Scope(this))
            {
                this.AxisV.Scale = this.AxisH.Scale;
                this.ScrollH.Value = this.AxisH.Value;
                this.ScrollV.Value = this.AxisV.Value;
            }
            this.Draw();
        }
        private void AxisV_ValueChanged(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            if (this._SuppressEvents) return;
            using (_SuppressedEvents.Scope(this))
            {
                this.AxisH.Scale = this.AxisV.Scale;
                this.ScrollV.Value = this.AxisV.Value;
                this.ScrollH.Value = this.AxisH.Value;
            }
            this.Draw();
        }
        private void ScrollH_ValueChanged(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            if (this._SuppressEvents) return;
            using (_SuppressedEvents.Scope(this))
            {
                this.AxisH.Value = this.ScrollH.Value;
            }
            this.Draw();
        }
        private void ScrollV_ValueChanged(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            if (this._SuppressEvents) return;
            using (_SuppressedEvents.Scope(this))
            {
                this.AxisV.Value = this.ScrollV.Value;
            }
            this.Draw();
        }
        private void DocumentArea_MaximalBoundsChanged(object sender, EventArgs e)
        {
            if (this._SuppressEvents) return;
            this.AcceptMaximalBoundsFromDocument();
            this.Draw();
        }
        protected GDocumentArea DocumentArea;
        protected GSizeAxis AxisH;
        protected GSplitter SplitterH;
        protected GScrollBar ScrollH;
        protected GSizeAxis AxisV;
        protected GSplitter SplitterV;
        protected GScrollBar ScrollV;
        /// <summary>
        /// Position of horizontal splitter (under horizontal axis)
        /// </summary>
        protected int SplitterHPos { get { return this.SplitterH.Value; } set { this.SplitterH.Value = value; } }
        /// <summary>
        /// Position of vertical splitter (after vertical axis)
        /// </summary>
        protected int SplitterVPos { get { return this.SplitterV.Value; } set { this.SplitterV.Value = value; } }
        #region suppress events during change a value, where this change trigger next (recursive) events
        private bool _SuppressEvents = false;
        private class _SuppressedEvents : IDisposable
        {
            public static IDisposable Scope(GDocumentEditor editor)
            {
                return new _SuppressedEvents(editor);
            }
            private _SuppressedEvents(GDocumentEditor editor)
            {
                this._Editor = editor;
                this._SuppressEventsValue = this._Editor._SuppressEvents;
                this._Editor._SuppressEvents = true;
            }
            GDocumentEditor _Editor;
            bool _SuppressEventsValue; 
            void IDisposable.Dispose()
            {
                this._Editor._SuppressEvents = this._SuppressEventsValue;
                this._Editor = null;
            }
        }
        #endregion
        #endregion
    }
}
