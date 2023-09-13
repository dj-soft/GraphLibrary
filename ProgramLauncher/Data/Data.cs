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


    }
    public class BaseData
    {
        public string ImageName { get; set; }
        public byte[] ImageContent { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
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
