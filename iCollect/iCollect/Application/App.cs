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
        { }
        #endregion
        #region Start
        public static void Start()
        {
            _Instance._Start();
        }
        private void _Start()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            System.Threading.Thread.CurrentThread.Name = "GUI thread";
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;

            System.Windows.Forms.Application.Run(new MainApp());
        }
        #endregion
    }
}
