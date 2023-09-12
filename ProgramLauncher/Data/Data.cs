using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class ApplicationData : BaseData
    {
    }
    public class GroupData : BaseData
    {
        public string ImageName { get; set; }
        public byte[] ImageContent { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }


    }
    public class BaseData
    {
        public virtual void Paint(PaintEventArgs e)
        { }
    }
}
