using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;

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

                // Nastavíme Config skin a budeme sledovat změny aktivního skinu a ukládat jej do configu:
                StyleChangedToConfigListener styleListener = new StyleChangedToConfigListener();
                styleListener.ActivateConfigStyle();
                styleListener.ActivateListener();

                string uhdPaint = Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.GetRawValue("Components", "UhdPaintEnabled");
                Noris.Clients.Win.Components.AsolDX.DxComponent.LogActive = true;         // I při spuštění v režimu Run, to kvůli TimeLogům

                Noris.Clients.Win.Components.AsolDX.DxComponent.UhdPaintEnabled = (uhdPaint != null && uhdPaint == "True");

                var moon10 = Noris.Clients.Win.Components.AsolDX.DxComponent.CreateBitmapImage("Images/Moon10.png");

                bool isImages = Noris.Clients.Win.Components.AsolDX.DxComponent.ApplicationArguments.Any(a => a.IndexOf("images", StringComparison.InvariantCultureIgnoreCase) >= 0);
                if (isImages)
                    Noris.Clients.Win.Components.AsolDX.DxComponent.ApplicationStart(typeof(TestDevExpress.Forms.ImagePickerForm), moon10);
                else
                    Noris.Clients.Win.Components.AsolDX.DxComponent.ApplicationStart(typeof(TestDevExpress.Forms.MainForm), moon10);
                // Noris.Clients.Win.Components.AsolDX.DxComponent.ApplicationStart(typeof(TestDevExpress.Forms.MainAppForm), moon10);

                Noris.Clients.Win.Components.AsolDX.DxComponent.UnregisterListener(styleListener);
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

        private class StyleChangedToConfigListener : IListenerStyleChanged
        { 
            public StyleChangedToConfigListener()
            {
                this.ConfigSkinName = Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.GetRawValue("UserSettings", "UsedSkinName");
            }
            public void ActivateConfigStyle()
            {
                this.ConfigSkinName = Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.GetRawValue("UserSettings", "UsedSkinName");
                string skinName = ConfigSkinName ?? "Seven Classic";
                DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skinName;
            }
            public void ActivateListener()
            {
                Noris.Clients.Win.Components.AsolDX.DxComponent.RegisterListener(this);
            }
            void IListenerStyleChanged.StyleChanged()
            {
                var skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName;
                if (!String.Equals(skinName, ConfigSkinName))
                {
                    Noris.Clients.Win.Components.AsolDX.DxComponent.Settings.SetRawValue("UserSettings", "UsedSkinName", skinName);
                    ConfigSkinName = skinName;
                }
            }
            /// <summary>
            /// Jméno skinu v konfiguraci
            /// </summary>
            public string ConfigSkinName { get; private set; }
        }
    }
}
