using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Příklady použití Drag&Drop a Clipboard funkčnosti
    /// </summary>
    class ClipboardExamples
    {
        static void MainTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Spuštění upraveného formuláře s Clipboard podporou
            Application.Run(new ShortcutDropForm());
        }
    }

    /// <summary>
    /// Příklady kódu pro práci s Clipboardem
    /// </summary>
    public class ClipboardHelper
    {
        /// <summary>
        /// Zkontroluj, zda Clipboard obsahuje .lnk soubory
        /// </summary>
        public static bool HasShortcutsInClipboard()
        {
            try
            {
                if (!Clipboard.ContainsFileDropList())
                    return false;

                var files = Clipboard.GetFileDropList();
                foreach (string file in files)
                {
                    if (file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Získej seznam .lnk souborů z Clipboardu
        /// </summary>
        public static List<string> GetShortcutsFromClipboard()
        {
            var shortcuts = new List<string>();

            try
            {
                if (!Clipboard.ContainsFileDropList())
                    return shortcuts;

                var files = Clipboard.GetFileDropList();
                foreach (string file in files)
                {
                    if (file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        shortcuts.Add(file);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba při čtení Clipboardu: {ex.Message}");
            }

            return shortcuts;
        }

        /// <summary>
        /// Zkopíruj .lnk soubor do Clipboardu (pro testování)
        /// </summary>
        public static void CopyShortcutToClipboard(string lnkFilePath)
        {
            try
            {
                var files = new System.Collections.Specialized.StringCollection();
                files.Add(lnkFilePath);
                Clipboard.SetFileDropList(files);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba: {ex.Message}");
            }
        }

        /// <summary>
        /// Vyčisti Clipboard
        /// </summary>
        public static void ClearClipboard()
        {
            try
            {
                Clipboard.Clear();
            }
            catch
            {
                // Ignore
            }
        }
    }
}
