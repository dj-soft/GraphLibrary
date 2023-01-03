using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    internal class GameControl : Control
    {
        public GameControl()
        {
            __Animator = new Animator(this);
        }
        private Animator __Animator;
    }
}
