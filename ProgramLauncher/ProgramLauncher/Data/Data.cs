using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class ApplicationData : BaseData
    {
    }
    public class GroupData : BaseData
    {
    }
    public class BaseData
    {
        public virtual void Paint(PaintEventArgs e)
        { }
    }
}
