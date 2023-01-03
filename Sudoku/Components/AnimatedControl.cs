using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    public class AnimatedControl : Control
    {
        public AnimatedControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            __Animator = new Animator(this);
        }
        protected override void Dispose(bool disposing)
        {
            __Animator.AnimatorTimerStop = true;
            __Animator = null;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Animátor
        /// </summary>
        public Animator Animator { get { return __Animator; } } private Animator __Animator;

    }
}
