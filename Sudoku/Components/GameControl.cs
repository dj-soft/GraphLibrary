using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    public class GameControl : AnimatedControl
    {
        public GameControl()
        {
            this.Animator.AddMotion(300, Animator.TimeMode.SlowStartSlowEnd, 0d, _AnimeBgr, Color.FromArgb(240, 240, 250), Color.FromArgb(200, 200, 250), null);
            this.Animator.AddMotion(200, Animator.TimeMode.Cycling, 0d, _AnimePoint, new Point(50,20), new Point(400, 80), null);
        }
        private void _AnimeBgr(Animator.Motion motion)
        {
            if (motion.IsCurrentValueChanged)
                this.BackColor = (Color)motion.CurrentValue;
        }
        private void _AnimePoint(Animator.Motion motion)
        {
            if (motion.IsCurrentValueChanged)
                this._Point = (Point)motion.CurrentValue;
        }

        protected override void DoPaintStandard(PaintEventArgs e)
        {
            if (_Point.HasValue)
                e.Graphics.FillRectangle(Brushes.DarkGreen, new Rectangle(_Point.Value, new Size(10, 10)));
        }
        private Point? _Point = null;
    }
}
