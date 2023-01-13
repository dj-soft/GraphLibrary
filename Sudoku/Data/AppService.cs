using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Animated
{
    public class AppService
    {
        #region Singleton, konstruktor, inicializace
        /// <summary>
        /// Instance singletonu
        /// </summary>
        protected static AppService Current
        {
            get
            {
                if (__Current is null)
                {
                    lock (__Lock)
                    {
                        if (__Current is null)
                        {
                            __Current = new AppService();
                            __Current._Init();
                        }
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Private konstruktor. 
        /// Jeho metody nesmí použít <see cref="Current"/>.
        /// </summary>
        private AppService()
        {

        }
        /// <summary>
        /// Private inicializer. 
        /// Jeho metody mohou použít ty části <see cref="Current"/>, které již byly inicializovány.
        /// </summary>
        private void _Init()
        {
            __CurrentUserName = System.Environment.UserName;

            bool isDiagnosticActive = System.Diagnostics.Debugger.IsAttached;
            if (!isDiagnosticActive)
                isDiagnosticActive = (__CurrentUserName == "david.janacek" || __CurrentUserName == "David");
            __IsDiagnosticActive = isDiagnosticActive;


        }
        private static AppService __Current;
        private static object __Lock = new object();
        #endregion
        #region Public static + private instance
        public static string CurrentUserName { get { return Current.__CurrentUserName; } } private string __CurrentUserName;
        public static bool IsDiagnosticActive { get { return Current.__IsDiagnosticActive; } set { Current.__IsDiagnosticActive = value; } } private bool __IsDiagnosticActive;
        #endregion
    }
}
