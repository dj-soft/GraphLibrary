using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Djs.Tools.WebDownloader
{
    public class Dialogs
    {
        public static void Info(string message)
        {
            MessageBox.Show(App.MainForm, message, "Informace", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static void Warning(string message)
        {
            MessageBox.Show(App.MainForm, message, "Varování", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static void Error(string message)
        {
            MessageBox.Show(App.MainForm, message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
