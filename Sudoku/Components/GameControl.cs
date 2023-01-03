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
            this.Animator.AddMotion(100, Animator.TimeMode.Cycling, 0d, _AnimeBgr, Color.LightBlue, Color.DarkBlue, null);
        }
        private void _AnimeBgr(Animator.Motion motion)
        {
            if (motion.IsCurrentValueChanged)
                this.BackColor = (Color)motion.CurrentValue;
        }
    }
}
