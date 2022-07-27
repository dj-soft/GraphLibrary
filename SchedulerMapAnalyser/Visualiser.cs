using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace DjSoft.SchedulerMap.Analyser
{
    /// <summary>
    /// Vizualizer mapy
    /// </summary>
    public class Visualiser : Control
    {
        public Visualiser()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.UserMouse, true);
        }
        #region Data
        public MapSegment MapSegment 
        {
            get { return _MapSegment; }
            set { _MapSegment = value; InvalidateData(); }

        }
        private MapSegment _MapSegment;
        protected void InvalidateData()
        { }
        protected void CheckData()
        { }

        #endregion
        public void ActivateMapItem(int? itemId)
        {
            if (_MapSegment is null) return;
            if (!_MapSegment.IsLoaded) Task.Factory.StartNew(_LoadMapSegment);

        }
        private void _LoadMapSegment()
        { }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawRectangle(Pens.Black, new Rectangle(40, 60, 200, 120));
        }
    }
    internal class VisualItem
    {

    }
}
