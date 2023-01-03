using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Data
{
    public class StopwatchExt : System.Diagnostics.Stopwatch
    {
        public StopwatchExt() { }
        public StopwatchExt(bool started)
            : base()
        {
            if (started) this.Start();
        }
        public double ElapsedMilisecs
        {
            get { return _GetMilisecs(0L, this.ElapsedTicks, 3); }
        }
        public double GetMilisecs(long startTicks)
        {
            return _GetMilisecs(startTicks, this.ElapsedTicks, null);
        }
        public double GetMilisecs(long startTicks, long stopTicks)
        {
            return _GetMilisecs(startTicks, stopTicks, null);
        }
        public double GetMilisecsRound(long startTicks, int round)
        {
            return _GetMilisecs(startTicks, this.ElapsedTicks, round);
        }
        public double GetMilisecsRound(long startTicks, long stopTicks, int round)
        {
            return _GetMilisecs(startTicks, stopTicks, round);
        }
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
