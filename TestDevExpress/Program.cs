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
                DxComponent.Settings.ProductName = "TestDevExpress";

                // DxComponent.Settings.ConfigFileName = @"c:\ProgramData\Asseco Solutions\NorisWin32Clients\Settings.bin";
                DxComponent.TempDirectorySuffix = "Asseco Solutions\\TestDevExpress\\Temp";
                var td = DxComponent.TempDirectoryName;

                // Nastavíme Config skin a budeme sledovat změny aktivního skinu a ukládat jej do configu:
                var styleListener = new SkinConfigStyle();
                styleListener.ActivateConfigStyle();
                styleListener.RefreshDxFontForZoom();

                string uhdPaint = DxComponent.Settings.GetRawValue("Components", DxComponent.UhdPaintEnabledCfgName);
                DxComponent.UhdPaintEnabled = (uhdPaint != null && uhdPaint == "True");

                string logActive = DxComponent.Settings.GetRawValue("Components", DxComponent.LogActiveCfgName);
                DxComponent.LogActive = (logActive != null && logActive == "True");

                string logActivitiesKind = DxComponent.Settings.GetRawValue("Components", DxComponent.LogActivitiesKindCfgName);
                Int64 logActivities = 0;
                bool hasActivities = (!String.IsNullOrEmpty(logActivitiesKind) && Int64.TryParse(logActivitiesKind, out logActivities));
                DxComponent.LogActivities = (hasActivities ? (LogActivityKind)logActivities: LogActivityKind.Default);

                string notCaptureWindows = DxComponent.Settings.GetRawValue("Components", DxComponent.ExcludeFromCaptureContentCfgName);
                DxComponent.ExcludeFromCaptureContent = (notCaptureWindows != null && notCaptureWindows == "True");

                var moon10 = DxComponent.CreateBitmapImage("Images/Moon10.png");

                bool isImages = DxComponent.ApplicationArgumentsContains("images");
                bool isTabView = !DxComponent.ApplicationArgumentsContains("oldform");             // Pokud v parametrech NENÍ "oldform", pak pouštím tabované hlavní okno MainAppForm, pro oldform pouštím starý MainForm
                bool isScroll = System.Windows.Forms.Control.IsKeyLocked(Keys.Scroll);             // ScrolLock to otočí, pro jeden konkrétní běh
                if (isScroll) isTabView = !isTabView;

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
            public override int? ZoomPercent 
            {
                get 
                {
                    string zoomText = DxComponent.Settings.GetRawValue("UserSettings", "UsedZoomPercent");
                    if (!String.IsNullOrEmpty(zoomText) && Int32.TryParse(zoomText, out int zoomPc))
                    {
                        zoomPc = (zoomPc < 50 ? 50 : (zoomPc > 200 ? 200 : zoomPc));
                        return zoomPc;
                    }
                    return null;
                }
                set
                {
                    if (value.HasValue)
                    {
                        int zoomPc = value.Value;
                        zoomPc = (zoomPc < 50 ? 50 : (zoomPc > 200 ? 200 : zoomPc));
                        DxComponent.Settings.SetRawValue("UserSettings", "UsedZoomPercent", zoomPc.ToString());
                    }
                }
            }

            protected override void OnZoomPercentChanged()
            {
                RefreshDxFontForZoom();
            }
            public void RefreshDxFontForZoom()
            {
                base.OnZoomPercentChanged();

                var currentFont = DevExpress.XtraEditors.WindowsFormsSettings.DefaultFont;
                float emSize = (float)(8.25m * DxComponent.Zoom);
                DevExpress.XtraEditors.WindowsFormsSettings.DefaultFont = new System.Drawing.Font(currentFont.FontFamily, emSize, currentFont.Style);
            }
        }
    }
}
