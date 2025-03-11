using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.App.iCollect
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Vytvoří se singleton App;
            // V Sigletonu se zavolá Start;
            // Tím se rozsvítí Main okno aplikace;
            DjSoft.App.iCollect.Application.App.Start();
        }
    }
}
