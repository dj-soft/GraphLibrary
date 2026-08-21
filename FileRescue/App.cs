using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Djs.Tools.CopyFile
{
    /// <summary>
    /// Static aplikace
    /// </summary>
    internal class App
    {
        internal static Form MainForm { get; set; }
        internal static void ShowInfo(string text)
        {
            InvokeGui(() => System.Windows.Forms.MessageBox.Show(MainForm, text, "Informace", MessageBoxButtons.OK, MessageBoxIcon.Information));
        }
        internal static void ShowWarning(string text)
        {
            InvokeGui(() => System.Windows.Forms.MessageBox.Show(MainForm, text, "Upozornění", MessageBoxButtons.OK, MessageBoxIcon.Warning));
        }
        internal static void InvokeGui(Action action)
        {
            var mainForm = MainForm;
            if (mainForm != null && mainForm.IsHandleCreated && !mainForm.IsDisposed && mainForm.InvokeRequired)
                mainForm.Invoke(new Action(action));
            else
                action();
        }
    }
}
