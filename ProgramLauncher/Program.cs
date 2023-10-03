using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
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
            if (_TryCreateMutex(out Mutex appMutex))
                _RunNewProcess(args, appMutex);
            else
                _ActivateSingleProcess();
        }
        /// <summary>
        /// Metoda se pokusí vytvořit Mutex pro zdejší aplikaci = takový mutex může být ve Windows jen jeden.
        /// Pokud tedy tato aplikace byla spuštěna již dříve, nový mutex se nevytvoří (už jej má předchozí instance) a vrátí se false.
        /// </summary>
        /// <returns></returns>
        private static bool _TryCreateMutex(out Mutex appMutex)
        {
            appMutex = new Mutex(true, SingleProcess.MutexName, out bool createdNew);

            // Máme new mutex => jsme první aplikace!
            if (createdNew) return true;

            // Nebyl vytvořen nový => již existuje => pokusíme se aktivovat aplikaci jinak
            appMutex.TryDispose();
            appMutex = null;
            return false;
        }
        /// <summary>
        /// Metoda provede standardní start aplikace.
        /// </summary>
        /// <param name="args"></param>
        private static void _RunNewProcess(string[] args, Mutex appMutex = null)
        {
            App.Start(args, appMutex);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            App.Exit();
        }
        /// <summary>
        /// Aktivuje primární proces v situaci, kdy this proces je druhý spouštěný (chceme řešit Singleton Process)
        /// </summary>
        private static void _ActivateSingleProcess()
        {
            if (_TryActivateProcessMainWindow()) return;

            SingleProcess.SendShowMeWmMessage();
        }
        /// <summary>
        /// Pokud metoda najde Windows proces odpovídající aktuální aplikaci, pak jej reaktivuje a vrátí true.
        /// </summary>
        /// <returns></returns>
        private static bool _TryActivateProcessMainWindow()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            string applicationName = currentProcess.MainModule.FileName;
            int currentProcessId = currentProcess.Id;
            string arguments = null;
            // Vyhledej proces stejný jako já, včetně procesů bez oken, ale musí být s jiným ID než mám já:
            var myProcess = App.SearchForProcess(applicationName, arguments, null, true, p => p.Id != currentProcessId);
            if (myProcess is null) return false;

            bool isActivated = App.ActivateWindowsProcess(myProcess);
            return isActivated;
        }
    }
}
