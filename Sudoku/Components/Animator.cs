using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Components
{
    internal class Animator
    {
        public Animator(System.Windows.Forms.Control owner)
        {
            this.__Owner = new WeakReference<System.Windows.Forms.Control>(owner);
        }
        private System.Windows.Forms.Control _Owner
        {
            get 
            {
                var wr = __Owner;
                if (wr is null || !wr.TryGetTarget(out var owner)) return null;
                return owner;
            }
        }
        private WeakReference<System.Windows.Forms.Control> __Owner;
    }
}
