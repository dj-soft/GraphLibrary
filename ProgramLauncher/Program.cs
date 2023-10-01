using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DjSoft.Tools.ProgramLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool isStarted = _IsReactivatedMyProcess();
            if (!isStarted)
                _RunNewProcess(args);
        }
        /// <summary>
        /// Pokud metoda najde Windows proces odpovídající aktuální aplikaci, pak jej reaktivuje a vrátí true.
        /// </summary>
        /// <returns></returns>
        private static bool _IsReactivatedMyProcess()
        {
            string applicationName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string arguments = null;
            var myProcess = App.SearchForProcess(applicationName, arguments);
            if (myProcess is null) return false;
            App.ActivateWindowsProcess(myProcess);
            return true;
        }
        /// <summary>
        /// Metoda provede standardní start aplikace.
        /// </summary>
        /// <param name="args"></param>
        private static void _RunNewProcess(string[] args)
        { 
            App.Start(args);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            App.Exit();
        }
    }
}
