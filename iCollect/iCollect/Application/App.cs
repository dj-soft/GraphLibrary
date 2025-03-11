using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.App.iCollect.Application
{
    internal class App
    {
        #region Singeton
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static App _Instance
        {
            get
            {
                if (__Instance is null)
                {
                    lock (__Locker)
                    {
                        if (__Instance is null)
                        {
                            __Instance = new App();
                            __Instance._Init();
                        }
                    }
                }
                return __Instance;
            }
        }
        private static App __Instance;
        private static object __Locker = new object();
        #endregion
        #region Inicializace
        private App()
        { }
        private void _Init()
        {
            _LoadSettings();
            _CreateStyleManager();
        }
        #endregion
        #region Start
        public static void Start()
        {
            _Instance._Start();
        }
        private void _Start()
        {
            _PrepareGui();

            __MainAppForm = new MainAppForm();
            System.Windows.Forms.Application.Run(__MainAppForm);
            __MainAppForm = null;
        }
        private void _PrepareGui()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            System.Threading.Thread.CurrentThread.Name = "GUI thread";
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;
        }
        /// <summary>
        /// Main okno aplikace
        /// </summary>
        public static MainAppForm MainAppForm { get { return _Instance.__MainAppForm; } }
        private MainAppForm __MainAppForm;
        #endregion
        #region Konfigurace běhu, adresář, style manager
        /// <summary>
        /// Konfigurace aplikace
        /// </summary>
        public static Settings Settings { get { return _Instance.__Settings; } }
        /// <summary>Konfigurace aplikace</summary>
        private Settings __Settings;
        /// <summary>Adresář konfigurace aplikace</summary>
        private string __AppWorkingPath;
        /// <summary>
        /// Načte konfiguraci aplikace
        /// </summary>
        private void _LoadSettings()
        {
            __Settings = new Settings();
        }


        private void _CreateStyleManager()
        {
            __DxStyleManager = new DxStyleManager();
        }
        private DxStyleManager __DxStyleManager;
        #endregion
    }
}
