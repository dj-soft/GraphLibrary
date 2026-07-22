using System;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Entry point aplikace - spuštění formuláře s Drag&Drop
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Spuštění formuláře
            Application.Run(new ShortcutDropForm());
        }
    }
}
