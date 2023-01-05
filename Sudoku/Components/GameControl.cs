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
            this.LayeredGraphics.UseBackgroundLayer = true;
            this.LayeredGraphics.UseStandardLayer = true;
        }
        protected override void DoPaintStandard(LayeredPaintEventArgs e)
        {
        }
    }
}
