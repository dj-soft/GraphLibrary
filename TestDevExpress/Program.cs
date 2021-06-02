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

            // TryRun(new Action(() => System.Windows.Forms.MessageBox.Show("xxx")));

            try
            {
                Noris.Clients.Win.Components.AsolDX.DxComponent.Init();
                Noris.Clients.Win.Components.AsolDX.DxComponent.LogActive = true;         // I při spuštění v režimu Run, to kvůli TimeLogům

                //  Application.Run(new TestDevExpress.Forms.MainForm());

                // Application.Run(new TestDevExpress.Forms.DataForm());

                 Application.Run(new TestDevExpress.Forms.RibbonForm());

                // Application.Run(new TestDevExpress.Forms.ImagePickerForm());

                // Application.Run(new TestDevExpress.Forms.GraphForm());

                // Application.Run(new TestDevExpress.Forms.MdiParentForm());
            }
            finally
            {
                Noris.Clients.Win.Components.AsolDX.DxComponent.Done();
            }
        }

        private static object TryRun(Delegate d)
        {
            d.DynamicInvoke();

            return null;
        }
    }
}
