using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestDevExpress
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Application.Run(new TestDevExpress.Forms.MainForm());
            Application.Run(new TestDevExpress.Forms.ImagePickerForm());

            // Application.Run(new TestDevExpress.Forms.GraphForm());

            // Application.Run(new TestDevExpress.Forms.MdiParentForm());

        }
    }
}
