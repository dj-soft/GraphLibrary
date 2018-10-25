using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Trace : třída pro zapisování údajů do trace
    /// </summary>
    public class Trace
    {
        #region Public: TraceInfo, Flush, End
        /// <summary>
        /// Zapíše Informaci
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void Info(string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(TracePriority.Priority5_Normal))
                this._TraceWrite(null, LEVEL_INFO, "", type, method, result, false, items);
        }
        /// <summary>
        /// Zapíše Informaci
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void Info(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(priority))
                this._TraceWrite(null, LEVEL_INFO, "", type, method, result, false, items);
        }
        /// <summary>
        /// Zapíše Informaci, ihned
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void InfoNow(string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(TracePriority.Priority5_Normal))
                this._TraceWrite(null, LEVEL_INFO, "", type, method, result, true, items);
        }
        /// <summary>
        /// Zapíše Informaci, ihned
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void InfoNow(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(priority))
                this._TraceWrite(null, LEVEL_INFO, "", type, method, result, true, items);
        }
        /// <summary>
        /// Zapíše Warning
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void Warning(string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(TracePriority.Priority5_Normal))
                this._TraceWrite(null, LEVEL_WARNING, "", type, method, result, false, items);
        }
        /// <summary>
        /// Zapíše Warning
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void Warning(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(priority))
                this._TraceWrite(null, LEVEL_WARNING, "", type, method, result, false, items);
        }
        /// <summary>
        /// Zapíše Warning, ihned
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void WarningNow(string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(TracePriority.Priority5_Normal))
                this._TraceWrite(null, LEVEL_WARNING, "", type, method, result, true, items);
        }
        /// <summary>
        /// Zapíše Warning, ihned
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void WarningNow(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(priority))
                this._TraceWrite(null, LEVEL_WARNING, "", type, method, result, true, items);
        }
        /// <summary>
        /// Zapíše Error
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void Error(string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(TracePriority.Priority5_Normal))
                this._TraceWrite(null, LEVEL_ERROR, "", type, method, result, true, items);
        }
        /// <summary>
        /// Zapíše Error
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public void Error(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (App.TraceForPriority(priority))
                this._TraceWrite(null, LEVEL_ERROR, "", type, method, result, true, items);
        }
        /// <summary>
        /// Provede Flush na výstupní soubor
        /// </summary>
        public void Flush()
        {
            this._TraceFlush();
        }
        /// <summary>
        /// Write into trace new event for End of application, perform Flush on stream, and really close trace file.
        /// </summary>
        public void End()
        {
            this._TraceEnd();
        }
        /// <summary>
        /// Plný název trace souboru
        /// </summary>
        public string File { get { return this._TraceFile; } }
        #endregion
        #region Exception
        /// <summary>
        /// Zapíše Exception + informace
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="items"></param>
        public void Exception(Exception exc, params string[] items)
        {
            if (exc == null) return;

            this._TraceWrite(null, LEVEL_EXCEPTION, "", exc.GetType().NsName(), exc.Message, "Exception", true, items);
            Exception ex = exc;
            while (ex != null)
            {
                string[] stack = _ExceptionGetStack(ex.StackTrace);
                this._TraceWrite(null, LEVEL_EXCEPTION_INFO, "", exc.GetType().NsName(), exc.Message, "StackTrace", true, stack);
                ex = ex.InnerException;
            }
        }
        private static string[] _ExceptionGetStack(string stackTrace)
        {
            string[] rows = stackTrace.ToLines(true, true);
            return rows;
        }
        #endregion
        #region Scope
        /// <summary>
        /// Otevře Scope (dá Begin), jehož Dispose zapíše párový End
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public ITraceScope Scope(string type, string method, string result, params string[] items)
        {
            bool isReal = App.TraceForPriority(TracePriority.Priority5_Normal);
            return TraceScope.GetScope(this, false, isReal, type, method, result, items);
        }
        /// <summary>
        /// Otevře Scope (dá Begin), jehož Dispose zapíše párový End
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public ITraceScope Scope(TracePriority priority, string type, string method, string result, params string[] items)
        {
            bool isReal = App.TraceForPriority(priority);
            return TraceScope.GetScope(this, false, isReal, type, method, result, items);
        }
        /// <summary>
        /// Otevře Scope (dá Begin), ihned, jehož Dispose zapíše párový End
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public ITraceScope ScopeNow(string type, string method, string result, params string[] items)
        {
            bool isReal = App.TraceForPriority(TracePriority.Priority5_Normal);
            return TraceScope.GetScope(this, true, isReal, type, method, result, items);
        }
        /// <summary>
        /// Otevře Scope (dá Begin), ihned, jehož Dispose zapíše párový End
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public ITraceScope ScopeNow(TracePriority priority, string type, string method, string result, params string[] items)
        {
            bool isReal = App.TraceForPriority(priority);
            return TraceScope.GetScope(this, true, isReal, type, method, result, items);
        }
        /// <summary>
        /// Write Begin line into trace, set (out int) scope number and (out long) tick value on Begin line. 
        /// This values are stored in scope object, and will be send as parameters to End line method _TraceWriteScopeEnd().
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="tick"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="direct"></param>
        /// <param name="items"></param>
        protected void _TraceWriteScopeBegin(out int scope, out long tick, string type, string method, string result, bool direct, string[] items)
        {
            scope = 0;
            tick = 0L;
            if (!this._TracePrepare()) return;
            scope = ++this._LastScope;
            this._TraceWrite(null, LEVEL_SCOPE_BEGIN, scope.ToString(), type, method, result, direct, items);
            tick = this._TraceLastTick;
        }
        /// <summary>
        /// Write End line into trace, from scope.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="tick"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="direct"></param>
        /// <param name="items"></param>
        protected void _TraceWriteScopeEnd(int scope, long tick, string type, string method, string result, bool direct, string[] items)
        {
            this._TraceWrite(tick, LEVEL_SCOPE_END, scope.ToString(), type, method, result, direct, items);
        }
        #region class TraceScope : ITraceScope
        /// <summary>
        /// Třída, která slouží jako obálka jednoho Scope v Trace datech.
        /// </summary>
        public class TraceScope : ITraceScope, IDisposable
        {
            internal static TraceScope GetScope(Trace trace, bool direct, bool isReal, string type, string method, string result, params string[] items)
            {
                TraceScope scope = new TraceScope(trace, direct, isReal, type, method, result, items);
                scope._WriteBegin();
                return scope;
            }
            private TraceScope(Trace trace, bool direct, bool isReal, string type, string method, string result, string[] items)
            {
                this._Trace = trace;
                this._Direct = direct;
                this._IsReal = isReal;
                this._Type = type;
                this._Method = method;
                this._Result = result;
                this._ItemsBegin = items;
                this._ItemsEnd = new List<string>();
            }
            private Trace _Trace;
            private bool _Direct;
            private bool _IsReal;
            private int _Scope;
            private long _Tick;
            private string _Type;
            private string _Method;
            private string _Result;
            private string[] _ItemsBegin;
            private List<string> _ItemsEnd;
            private void _WriteBegin()
            {
                if (this._IsReal)
                {
                    int scope;
                    long tick;
                    this._Trace._TraceWriteScopeBegin(out scope, out tick, this._Type, this._Method, this._Result, this._Direct, this._ItemsBegin);
                    this._Scope = scope;
                    this._Tick = tick;
                }
                this._ItemsBegin = null;
                this._StartTime = DateTime.Now;
            }
            private void _WriteEnd()
            {
                if (this._IsReal)
                    this._Trace._TraceWriteScopeEnd(this._Scope, this._Tick, this._Type, this._Method, this._Result, this._Direct, this._ItemsEnd.ToArray());
            }
            private void _AddValues<T>(T[] values, params string[] labels)
            {
                if (values == null || labels == null) return;
                int valueCount = values.Length;
                int labelCount = labels.Length;
                int count = (valueCount < labelCount ? valueCount : labelCount);
                if (count == 0) return;
                for (int i = 0; i < count; i++)
                    this._ItemsEnd.Add(labels[i] + (values[i] != null ? values[i].ToString() : ""));
            }
            private TimeSpan _GetElapsedTime()
            {
                return (DateTime.Now - this._StartTime);
            }
            private DateTime _StartTime;
            void ITraceScope.AddItem(string item) { this._ItemsEnd.Add(item); }
            void ITraceScope.AddItems(IEnumerable<string> items) { this._ItemsEnd.AddRange(items); }
            void ITraceScope.AddItems(params string[] items) { this._ItemsEnd.AddRange(items); }
            void ITraceScope.AddValues<T>(T[] values, params string[] labels) { this._AddValues(values, labels); }
            string ITraceScope.Type { get { return this._Type; } }
            string ITraceScope.Method { get { return this._Method; } }
            string ITraceScope.Result { get { return this._Result; } set { this._Result = value; } }
            TimeSpan ITraceScope.ElapsedTime { get { return this._GetElapsedTime(); } }
            void IDisposable.Dispose() { this._WriteEnd(); }
        }
        #endregion
        #endregion
        #region Private: init, prepare, write, flush, end. Variables.
        private void _TraceInit()
        {
            this._TraceFile = null;
        }
        /// <summary>
        /// If trace is prepared, then write one row into this.
        /// Can call Flush() by "direct" parameter.
        /// Store current time into _TraceLastTick.
        /// </summary>
        /// <param name="lastTick">-1 = do not write to column Microsec. Null = calculate time for Microsec column from this._TraceLastTick. Any positive value = calculate time for Microsec column from this value.</param>
        /// <param name="level">Level of information, use constants LEVEL_*</param>
        /// <param name="scope">Scope info</param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="direct"></param>
        /// <param name="items"></param>
        private void _TraceWrite(long? lastTick, string level, string scope, string type, string method, string result, bool direct, string[] items)
        {
            if (!this._TracePrepare()) return;

            this._TraceWriteData(lastTick, level, scope, type, method, result, items);
            if (direct)
                this._TraceFlush();
            this._TraceLastTick = this._TraceStopWatch.ElapsedTicks;
        }
        private bool _TracePrepare()
        {
            if (!this._TraceIsActive && !this._TraceIsDisable)
            {
                try
                {
                    this._LastLine = 0;
                    this._LastScope = 0;
                    DateTime now = DateTime.Now;
                    string path = App.GetAppLocalDataPath("Trace");
                    string name = this._TraceSearchNewFile(path, "Trace-" + now.ToString("yyyy-MM-dd"), ".csv");
                    string file = System.IO.Path.Combine(path, name);
                    System.IO.StreamWriter stream = null;
                    if (System.IO.File.Exists(file))
                    {
                        this._TraceAnalyse(file);
                        stream = new System.IO.StreamWriter(file, true, Encoding.UTF8, 4096);
                        stream.AutoFlush = false;
                    }
                    else
                    {
                        stream = new System.IO.StreamWriter(file, false, Encoding.UTF8, 4096);
                        stream.AutoFlush = false;
                        stream.WriteLine(TraceTitle);
                    }
                    this._TraceStream = stream;
                    this._TraceFile = file;
                    this._TraceStopWatch = new System.Diagnostics.Stopwatch();
                    this._TraceStopWatch.Start();
                    this._TraceFrequency = (decimal)System.Diagnostics.Stopwatch.Frequency;

                    bool withDebugger = System.Diagnostics.Debugger.IsAttached;
                    this._TraceWriteSys(LEVEL_RUN, (withDebugger ? "Debug" : "Run"));
                    this._TraceLastTick = this._TraceStopWatch.ElapsedTicks;
                }
                catch (Exception)
                {
                    this._TraceEnd();
                    this._TraceIsDisable = true;
                }
            }

            return this._TraceIsActive;
        }

        private string _TraceSearchNewFile(string path, string name, string extension)
        {
            string mask = name + "_???" + extension;
            string numb = "001";
            List<string> files = System.IO.Directory.GetFiles(path, mask).ToList();
            int count = files.Count;
            if (count > 0)
            {
                List<string> names = files.Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToList();
                names.Sort((a, b) => String.Compare(b, a, true));
                string lastName = names[0];
                string suffix = (lastName.Length > (name.Length + 1) ? lastName.Substring(name.Length + 1) : "");
                int value;
                if (Int32.TryParse(suffix, out value) && (value > 0 && value < 998))
                    numb = (value + 1).ToString("000");
            }
            return name + "_" + numb + extension;
        }
        private void _TraceEnd()
        {
            if (this._TraceIsActive)
            {
                this._TraceWriteSys(LEVEL_EXIT);
                this._TraceFlush();
            }
            if (this._TraceStream != null)
            {
                this._SynchronizedTraceStream.Close();
                this._SynchronizedTraceStream.Dispose();
                this._TraceStream = null;
            }

            this._TraceStream = null;
            this._TraceFile = null;
            this._TraceStopWatch = null;
            this._TraceFrequency = 0m;
            this._TraceLastTick = 0L;

            this._TraceIsDisable = false;
        }
        private void _TraceWriteSys(string level, params string[] items)
        {
            this._TraceWriteData(-1L, level, "", "----------", "----------", "----------", items);
        }
        /// <summary>
        /// Write row to trace.
        /// </summary>
        /// <param name="lastTick">-1 = do not write to column Microsec. Null = calculate time for Microsec column from this._TraceLastTick. Any positive value = calculate time for Microsec column from this value.</param>
        /// <param name="level">Level of information, use constants LEVEL_*</param>
        /// <param name="scope">Scope info</param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        private void _TraceWriteData(long? lastTick, string level, string scope, string type, string method, string result, params string[] items)
        {
            DateTime now = DateTime.Now;
            string microsec = "";
            if (!lastTick.HasValue || (lastTick.HasValue && lastTick.Value > 0))
            {   // When lastTick is null, then use this._TraceLastTick; else use value of lastTick:
                long tick = this._TraceStopWatch.ElapsedTicks - ((lastTick.HasValue) ? lastTick.Value : this._TraceLastTick);
                if (tick > 0L)
                {
                    decimal ms = 1000000m * (decimal)tick / this._TraceFrequency;
                    if (ms < (decimal)(UInt64.MaxValue))
                    {
                        UInt64 time = (UInt64)Math.Round(ms, 0);
                        microsec = time.ToString();
                    }
                }
            }

            System.Threading.Thread thread = System.Threading.Thread.CurrentThread;
            string threadName = thread.Name;
            string threadId = " #" + thread.ManagedThreadId.ToString("X4");
            string threadText = (!String.IsNullOrEmpty(threadId) ? threadName + threadId : "[Thread " + threadId + "]");
            
            int line = ++this._LastLine;
            string tab = "\t";
            StringBuilder sb = new StringBuilder();
            sb.Append(line.ToString());
            sb.Append(tab + now.Date.ToString("yyyy-MM-dd"));
            sb.Append(tab + now.ToString("HH:mm:ss.fff"));
            sb.Append(tab + microsec);
            sb.Append(tab + _TraceFormatText(threadText));
            sb.Append(tab + _TraceFormatText(level));
            sb.Append(tab + _TraceFormatText(scope));
            sb.Append(tab + _TraceFormatText(type));
            sb.Append(tab + _TraceFormatText(method));
            sb.Append(tab + _TraceFormatText(result));
            if (items != null)
            {
                foreach (string item in items)
                    sb.Append(tab + _TraceFormatText(item));
            }

            try
            {
                this._SynchronizedTraceStream.WriteLine(sb.ToString());
            }
            catch { }
        }
        private static string _TraceFormatText(string item)
        {
            if (item == null) return "";
            return item
                    .Replace("\t", " ")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
        private void _TraceFlush()
        {
            if (this._TraceStream != null)
            {
                this._SynchronizedTraceStream.Flush();
            }
        }
        /// <summary>
        /// Scan existing trace file for last Line and last Pair value, store its into 
        /// </summary>
        /// <param name="file"></param>
        private void _TraceAnalyse(string file)
        {
            using (var reader = new System.IO.StreamReader(file, true))
            {
                this._TraceAnalyse(reader);
                reader.Close();
            }
        }
        /// <summary>
        /// Scan existing trace file (within Stream) for last Line and last Pair value, store its into 
        /// </summary>
        /// <param name="reader"></param>
        private void _TraceAnalyse(System.IO.StreamReader reader)
        {
            if (reader.BaseStream == null) return;
            if (!reader.BaseStream.CanSeek) return;

            long chunk = 4096L;
            long lenght = reader.BaseStream.Length;
            long offset = lenght - chunk;
            if (offset < 0L) offset = 0L;
            int? scanToLine = null;

            while (true)
            {
                reader.DiscardBufferedData();
                reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
                if (offset > 0L)
                    reader.ReadLine();                               // Partial row, not readed from row.begin, discard it
                
                int? firstLine = null;                               // First line number, readed in this chunk
                while (!reader.EndOfStream) // && reader.pos reader.BaseStream.Position < scanTo)
                {   // Read lines from offset, and analyse its:
                    int? line = this._TraceAnalyseLine(reader.ReadLine());
                    if (line.HasValue && scanToLine.HasValue && line.Value >= scanToLine.Value)
                        break;
                    if (line.HasValue && !firstLine.HasValue)
                        firstLine = line;
                }
                if (offset == 0L || (this._LastLine > 0L && this._LastScope > 0L))
                    break;

                // Scan previous chunk:
                offset -= chunk;
                if (offset < 0) offset = 0;
                scanToLine = firstLine;
            }
        }
        private int? _TraceAnalyseLine(string row)
        {
            int? result = null;
            if (String.IsNullOrEmpty(row)) return result;
            string[] items = row.Split('\t');
            if (items.Length < 7) return result;
            int value;
            if (!String.IsNullOrEmpty(items[0]) && Int32.TryParse(items[0], out value))
            {   // Max(Line):
                if (value > this._LastLine) this._LastLine = value;
                result = value;
            }
            if (!String.IsNullOrEmpty(items[6]) && Int32.TryParse(items[5], out value))
            {   // Max(Scope):
                if (value > this._LastScope) this._LastScope = value;
            }
            return result;
        }
        /// <summary>
        /// Titulkový řádek trace
        /// </summary>
        public static string TraceTitle { get { return "Line;Date;Time;Microsec;Thread;Level;Scope;Object;Method;Result;Info".Replace(";", "\t"); } }
        private bool _TraceIsActive { get { return (!this._TraceIsDisable && this._TraceStream != null); } }
        private bool _TraceIsDisable;
        private string _TraceFile;
        /// <summary>
        /// Stream do trace souboru.
        /// Kód nesmí pracovat s tímto streamem, musí si vyžádat multi-threadově synchronizovaný objekt pomocí metody _GetSynchronizedTraceStream().
        /// </summary>
        private System.IO.StreamWriter _TraceStream;
        /// <summary>
        /// Obsahuje thread-safe obálku nad _TraceStream. Pro práci se streamem se má používat tato obálka, nehrozí u ní konflikt multi-threading pokusu o zápisy.
        /// Nepoužívat v Dispose bloku.
        /// </summary>
        private System.IO.TextWriter _SynchronizedTraceStream { get { return System.IO.StreamWriter.Synchronized(this._TraceStream); } }
        private int _LastLine;
        private int _LastScope;
        private System.Diagnostics.Stopwatch _TraceStopWatch;
        private decimal _TraceFrequency;
        private long _TraceLastTick;
        #endregion
        #region Constants
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_RUN = "Run";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_EXIT = "Exit";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_INFO = "Info";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_WARNING = "Warn";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_ERROR = "Error";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_EXCEPTION = "Exception";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_EXCEPTION_INFO = "ExceptionInfo";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_SCOPE_BEGIN = "Begin";
        /// <summary>Konkrétní konstanta do sloupce Level</summary>
        protected const string LEVEL_SCOPE_END = "End";
        #endregion
    }
    #region enum TraceValue
    /// <summary>
    /// Priorita zápisu do trace.
    /// Pokud není uvedena, bere se hodnota Normal.
    /// </summary>
    public enum TracePriority : int
    {
        /// <summary>
        /// Tyto události se nikdy do trace nezapíšou.
        /// To není konina, volat zápis do trace s prioritou "None", to může být klidně hodnota nějaké vyhodnocené proměnné...
        /// Nezáleží na tom, jaké bude nastavení App.TracePriority.
        /// </summary>
        Priority0_None = 0,
        /// <summary>
        /// Zapíše se jen v režimu "TimeDebug", když ladíme výkon grafiky.
        /// Takových událostí může být tisíce za jednu sekundu!!!
        /// </summary>
        Priority1_ElementaryTimeDebug = 1,
        /// <summary>
        /// Priorita těsně nad TimeDebug, trace obsahuje poměrně dost zápisů.
        /// </summary>
        Priority2_Lowest = 2,
        /// <summary>
        /// Priorita události lehce pod normální, trace bude obsahovat o něco více zápisů než normálně.
        /// </summary>
        Priority3_BellowNormal = 3,
        /// <summary>
        /// Standardní priorita pro události.
        /// </summary>
        Priority5_Normal = 5,
        /// <summary>
        /// Událost s touto prioritou se do trace dostane i když se do trace nebudou dostávat normální zápisy.
        /// </summary>
        Priority7_AboveNormal = 7,
        /// <summary>
        /// Událost s touto prioritou se do trace dostane vždy, nejde ji vypnout jakýmkoliv nastavením App.TracePriority.
        /// </summary>
        Priority9_Allways = 10
    }
    #endregion
    #region interface ITraceScope 
    /// <summary>
    /// Interface for trace scope. User can read few values of Scope, and can modify Result info an add texts into Info columns.
    /// </summary>
    public interface ITraceScope : IDisposable
    {
        /// <summary>
        /// Add new string info to current scope. 
        /// This info will be writed into End row for this scope.
        /// </summary>
        /// <param name="item"></param>
        void AddItem(string item);
        /// <summary>
        /// Add new string informations (more items) to current scope. 
        /// This info will be writed into End row for this scope.
        /// </summary>
        /// <param name="items"></param>
        void AddItems(IEnumerable<string> items);
        /// <summary>
        /// Add new string informations (more items) to current scope. 
        /// This info will be writed into End row for this scope.
        /// </summary>
        /// <param name="items"></param>
        void AddItems(params string[] items);
        /// <summary>
        /// Do aktuálního scope přidá sadu údajů, které sestaví z dodaných hodnot (values), které opatří odpovídajícím popiskem (labels).
        /// Jinými slovy, do scope přidá položky: {label[i]}{values[i]}, kde [i] je index 0 až poslední prvek (který je obsažen v obou polích).
        /// Je na zodpovědnosti volajícího, aby předal dostatek labelů k zadanému počtu hodnot.
        /// </summary>
        /// <typeparam name="T">Typ hodnot</typeparam>
        /// <param name="values">Hodnoty</param>
        /// <param name="labels">Popisky</param>
        void AddValues<T>(T[] values, params string[] labels);
        /// <summary>
        /// Info in column Type. Read-only.
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Info in column Method. Read-only.
        /// </summary>
        string Method { get; }
        /// <summary>
        /// Can change (get and set) info in column Result.
        /// Other columns are not editable.
        /// </summary>
        string Result { get; set; }
        /// <summary>
        /// Time elapsed from begin of scope
        /// </summary>
        TimeSpan ElapsedTime { get; }
    }
    #endregion
}
