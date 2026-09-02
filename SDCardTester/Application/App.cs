using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.SDCardTester
{
    internal class App
    {
        #region Singleton
        private static App Instance
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
                        }
                    }
                }
                return __Instance;
            }
        }
        private static App __Instance;
        private static Object __Locker = new Object();
        private App()
        {
            
        }
        #endregion
        #region Obecný servis
        public static System.Windows.Forms.Form MainForm
        {
            get
            {
                var forms = System.Windows.Forms.Application.OpenForms
                    .OfType<System.Windows.Forms.Form>()
                    .Where(f => f != null && !f.IsDisposed && f.IsHandleCreated)
                    .ToArray();
                if (forms.Length == 0) return null;

                var visibleForm = forms.FirstOrDefault(f => f.Visible);
                if (visibleForm is null) visibleForm = forms[0];

                return visibleForm;
            }
        }
        public static void RunInGui(Action action)
        {
            var mainForm = MainForm;
            if (mainForm != null && mainForm.InvokeRequired)
            {
                mainForm.Invoke(action);
            }
            else
            {
                action();
            }
        }
        #endregion
        #region MessageBoxy
        /// <summary>
        /// Zobrazí informaci v MessageBoxu. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        public static void ShowInfo(string message, string caption = "Information")
        {
            RunInGui(() => System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information) );
        }
        /// <summary>
        /// Zobrazí varování v MessageBoxu. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        public static void ShowWarning(string message, string caption = "Warning")
        {
            RunInGui(() => System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning) );
        }
        /// <summary>
        /// Zobrazí chybovou hlášku v MessageBoxu. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        public static void ShowError(string message, string caption = "Error")
        {
            RunInGui(() => System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error) );
        }
        #endregion
    }
}
