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
            try
            {
                Noris.Clients.Win.Components.AsolDX.DxComponent.Init();
                Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.CompanyName = "DJsoft";
                Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.ApplicationName = "TestDevExpress";
                // Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.ConfigFileName = @"c:\ProgramData\Asseco Solutions\NorisWin32Clients\Settings.bin";
                string uhdPaint = Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.GetRawValue("Components", "UhdPaintEnabled");
                Noris.Clients.Win.Components.AsolDX.DxComponent.LogActive = true;         // I při spuštění v režimu Run, to kvůli TimeLogům

                Noris.Clients.Win.Components.AsolDX.DxComponent.UhdPaintEnabled = (uhdPaint != null && uhdPaint == "True");
 
                var moon10 = Noris.Clients.Win.Components.AsolDX.DxComponent.CreateBitmapImage("Images/Moon10.png");

                Noris.Clients.Win.Components.AsolDX.DxComponent.ApplicationStart(typeof(TestDevExpress.Forms.MainForm), moon10);
                // Noris.Clients.Win.Components.AsolDX.DxComponent.ApplicationStart(typeof(TestDevExpress.Forms.MainAppForm), moon10);
                
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
