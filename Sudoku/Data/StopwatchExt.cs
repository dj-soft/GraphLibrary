using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Data
{
    /// <summary>
    /// Přesná časomíra s možností milisekund na více desetinných míst
    /// </summary>
    public class StopwatchExt : System.Diagnostics.Stopwatch
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public StopwatchExt() { }
        /// <summary>
        /// Konstruktor s možností startu
        /// </summary>
        /// <param name="started"></param>
        public StopwatchExt(bool started)
            : base()
        {
            if (started) this.Start();
        }
        /// <summary>
        /// Obsahuje aktuální čas od startu controlu, v milisekundách, na 3 desetinná místa.
        /// </summary>
        public double ElapsedMilisecs
        {
            get { return _GetMilisecs(0L, this.ElapsedTicks, 3); }
        }
        /// <summary>
        /// Vrátí čas uplynulý od daného startu do teď, bez zaokrouhlování = na 6 desetinných míst (nanosekundy)
        /// </summary>
        /// <param name="startTicks"></param>
        /// <returns></returns>
        public double GetMilisecs(long startTicks)
        {
            return _GetMilisecs(startTicks, this.ElapsedTicks, null);
        }
        /// <summary>
        /// Vrátí čas uplynulý od daného startu do daného času, bez zaokrouhlování = na 6 desetinných míst (nanosekundy)
        /// </summary>
        /// <param name="startTicks"></param>
        /// <param name="stopTicks"></param>
        /// <returns></returns>
        public double GetMilisecs(long startTicks, long stopTicks)
        {
            return _GetMilisecs(startTicks, stopTicks, null);
        }
        /// <summary>
        /// Vrátí čas uplynulý od daného startu do teď, s daným zaokrouhlením
        /// </summary>
        /// <param name="startTicks"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public double GetMilisecsRound(long startTicks, int round)
        {
            return _GetMilisecs(startTicks, this.ElapsedTicks, round);
        }
        /// <summary>
        /// Vrátí čas uplynulý od daného startu do daného času, s daným zaokrouhlením
        /// </summary>
        /// <param name="startTicks"></param>
        /// <param name="stopTicks"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public double GetMilisecsRound(long startTicks, long stopTicks, int round)
        {
            return _GetMilisecs(startTicks, stopTicks, round);
        }
        /// <summary>
        /// Vrátí čas uplynulý od daného startu do daného času, s daným zaokrouhlením
        /// </summary>
        /// <param name="startTicks"></param>
        /// <param name="stopTicks"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        private double _GetMilisecs(long startTicks, long stopTicks, int? round)
        {
            double ticks = (double)(stopTicks - startTicks);
            double freq = (double)System.Diagnostics.Stopwatch.Frequency;
            double milisecs = 1000d * ticks / freq;
            if (round.HasValue && round.Value >= 0 && round.Value <= 6)
                milisecs = Math.Round(milisecs, round.Value);
            return milisecs;
        }
    }
}
