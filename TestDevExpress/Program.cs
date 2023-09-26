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
                DxComponent.Init();
                DxComponent.Settings.CompanyName = "Asseco Solutions";
                DxComponent.Settings.ApplicationName = "TestDevExpress";

                // DxComponent.Settings.ConfigFileName = @"c:\ProgramData\Asseco Solutions\NorisWin32Clients\Settings.bin";
                DxComponent.TempDirectorySuffix = "Asseco Solutions\\TestDevExpress\\Temp";
                var td = DxComponent.TempDirectoryName;

                // Nastavíme Config skin a budeme sledovat změny aktivního skinu a ukládat jej do configu:
                var styleListener = new SkinConfigStyle();
                styleListener.ActivateConfigStyle();

                string uhdPaint = DxComponent.Settings.GetRawValue("Components", "UhdPaintEnabled");
                DxComponent.LogActive = true;         // I při spuštění v režimu Run, to kvůli TimeLogům

                DxComponent.UhdPaintEnabled = (uhdPaint != null && uhdPaint == "True");

                var moon10 = DxComponent.CreateBitmapImage("Images/Moon10.png");

                bool isImages = DxComponent.ApplicationArgumentsContains("images");
                bool isTabView = DxComponent.ApplicationArgumentsContains("tabview");
                var appFormType = (isImages ? typeof(TestDevExpress.Forms.ImagePickerForm) :
                                  (isTabView ? typeof(TestDevExpress.Forms.MainAppForm) :
                                  typeof(TestDevExpress.Forms.MainForm)));
                DxComponent.ApplicationStart(appFormType, moon10);

                DxComponent.UnregisterListener(styleListener);
            }
            finally
            {
                DxComponent.Done();
            }
        }
        private static object TryRun(Delegate d)
        {
            d.DynamicInvoke();

            return null;
        }

        /// <summary>
        /// Listener změny skinu a jeho napojení na <see cref="DxComponent.Settings"/>
        /// </summary>
        private class SkinConfigStyle : DxStyleToConfigListener
        {
            public override string SkinName
            { 
                get { return DxComponent.Settings.GetRawValue("UserSettings", "UsedSkinName"); }
                set { DxComponent.Settings.SetRawValue("UserSettings", "UsedSkinName", value); }
            }
            public override bool SkinCompact
            {
                get { return DxComponent.Settings.GetRawValue("UserSettings", "UsedSkinCompact", "N") == "A"; }
                set { DxComponent.Settings.SetRawValue("UserSettings", "UsedSkinCompact", value ? "A" : "N"); }
            }
            public override string PaletteName
            {
                get { return DxComponent.Settings.GetRawValue("UserSettings", "UsedPaletteName"); }
                set { DxComponent.Settings.SetRawValue("UserSettings", "UsedPaletteName", value); }
            }
        }
    }
}
