using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Logování událostí v komponentách.
    /// Rychlé, aktivní implicitně jen v Debug režimu.
    /// </summary>
    public class Log
    {
        #region Singleton, konstruktor
        /// <summary>
        /// Instance
        /// </summary>
        protected static Log Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (__Locker)
                    {
                        if (__Instance == null)
                            __Instance = new Log();
                    }
                }
                return __Instance;
            }
        }

        private static Log __Instance;
        private static object __Locker = new object();
        private Log()
        {
            _Active = System.Diagnostics.Debugger.IsAttached;
            _SpaceAfterMicroseconds = 1000000L;
            _Builder = new StringBuilder();
            _Stopwatch = new System.Diagnostics.Stopwatch();
            _StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency;
            _Line = 0;
            _Stopwatch.Start();
        }
        private bool _Active;
        private long _SpaceAfterMicroseconds;
        private long _LastTime;
        private StringBuilder _Builder;
        private System.Diagnostics.Stopwatch _Stopwatch;
        private decimal _StopwatchFrequency;
        private int _Line;
        #endregion
        #region Public
        public static string Text { get { return Instance._Text; } }
        public static void AddInfo(Type type, string method, params object[] infos) { Instance._Add(INFO, type, method, infos); }
        public static void AddException(Type type, string method, Exception exc, params object[] infos) { Instance._AddException(type, method, exc, infos); }
        #endregion
        #region Private
        private string _Text
        {
            get
            {
                if (!_Active) return "Log.Active = false";
                lock (_Builder)
                {
                    return _Builder.ToString();
                }
            }
        }
        private void _Add(string level, Type type, string method, object[] infos)
        {
            if (!_Active) return;
            lock (_Builder)
            {
                _AddRowLocked(_Builder, level, type.Name, method, null, infos);
            }
        }
        private void _AddException(Type type, string method, Exception exc, object[] infos)
        {
            if (!_Active) return;
            lock (_Builder)
            {
                _AddRowLocked(_Builder, EXCEPTION, type.Name, method, null, infos);
                int depth = 0;
                char[] eols = new char[] { '\r', '\n' };
                while (exc != null)
                {
                    string[] stacks = exc.StackTrace.Split(eols, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                    _AddRowLocked(_Builder, EXCEPTION, exc.GetType().Name, (depth == 0 ? "Top" : "Inner" + (++depth).ToString()), null, stacks);
                    exc = exc.InnerException;
                }
            }
        }
        private void _AddRowLocked(StringBuilder builder, string level, string type, string method, long? startDelta, IEnumerable<object> infos)
        {
            int line = ++_Line;
            long currentTicks = _Stopwatch.ElapsedTicks;
            string tab = "\t";
            DateTime now = DateTime.Now;
            if (line == 1)
            {
                _Builder.AppendLine("Line;Time;Total;Delta;Level;Type;Method;Info".Replace(";", tab));
                _Builder.AppendLine(";" + now.ToString(DATE_FORMAT) + ";[microsec];[microsec];V1.0;;;".Replace(";", tab));
                _LastTime = currentTicks;
            }

            if (_SpaceAfterMicroseconds > 0L)
            {
                long pause = _GetMicroseconds(currentTicks - _LastTime);               // Pauza od posledního zápisu v mikrosekundách
                if (pause >= _SpaceAfterMicroseconds)
                    builder.AppendLine();
            }

            builder.Append(line.ToString());
            builder.Append(tab + now.ToString(TIME_FORMAT));
            builder.Append(tab + _GetMicroseconds(currentTicks).ToString());
            long deltaTicks = currentTicks - (startDelta ?? _LastTime);
            builder.Append(tab + _GetMicroseconds(deltaTicks).ToString());
            builder.Append(tab + level);
            builder.Append(tab + type);
            builder.Append(tab + method);

            if (infos != null)
            {
                foreach (var info in infos)
                    builder.Append(tab + (info == null ? "NULL" : info.ToString()));
            }
            builder.AppendLine();

            _LastTime = _Stopwatch.ElapsedTicks;
        }
        private long _GetMicroseconds(long ticks)
        {
            decimal microseconds = 1000000m * (decimal)ticks / _StopwatchFrequency;
            return (long)Math.Round(microseconds, 0);
        }
        private const string DATE_FORMAT = "yyyy-MM-dd";
        private const string TIME_FORMAT = "HH:mm:ss.fff";
        private const string INFO = "I";
        private const string WARNING = "W";
        private const string USRERROR = "E";
        private const string SYSERROR = "S";
        private const string EXCEPTION = "X";
        private const string BEGIN = "B";
        private const string END = "E";
        #endregion


    }
}
