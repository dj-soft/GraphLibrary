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
    public class GroupData : BaseData
    {


    }
    public class ApplicationData : BaseData
    {
    }
    public abstract class BaseData
    {
        public virtual string ImageName { get; set; }
        public virtual byte[] ImageContent { get; set; }
        public virtual Color BackColor { get; set; }
        public virtual Color ForeColor { get; set; }
        /// <summary>
        /// Vnější velikost objektu.
        /// Tyto velikosti jednotlivých objektů na sebe těsně navazují.
        /// Objekt by do této velikosti měl zahrnout i mezery (okraje) mezi sousedními objekty.
        /// Pokud konkrétní potomek neřeší výšku nebo šířku, může v dané hodnotě nechat 0.
        /// </summary>
        public virtual Size Size { get; set; }
        public virtual void Paint(PaintEventArgs e)
        { }
    }
}
