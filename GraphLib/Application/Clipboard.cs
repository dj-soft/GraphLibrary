using Asol.Tools.WorkScheduler.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler
{
    /// <summary>
    /// Obálka nad Windows Clipboardem. Chyby hlásí uživateli.
    /// </summary>
    public class WinClipboard
    {
        public static bool ContainsText()
        {
            return System.Windows.Forms.Clipboard.ContainsText();
        }
        public static string GetText()
        {
            if (!System.Windows.Forms.Clipboard.ContainsText()) return null;
            return System.Windows.Forms.Clipboard.GetText();
        }
        public static void SetText(string text)
        {
            App.TryRun(() => { System.Windows.Forms.Clipboard.Clear(); System.Windows.Forms.Clipboard.SetText(text); }, true);
        }
        public static bool ContainsFileDropList()
        {
            return System.Windows.Forms.Clipboard.ContainsFileDropList();
        }
        public static string[] GetFileDropList()
        {
            if (!System.Windows.Forms.Clipboard.ContainsFileDropList()) return null;
            var fileList = System.Windows.Forms.Clipboard.GetFileDropList();
            int length = fileList?.Count ?? 0;
            string[] files = new string[length];
            if (fileList != null) fileList.CopyTo(files, 0);
            return files;
        }
    }
}
