using System;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Entry point aplikace - spuštění formuláře s Drag&Drop
    /// </summary>
    static class Program2
    {
        static void MainTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Spuštění formuláře
            Application.Run(new ShortcutDropForm());
        }
    }
}
