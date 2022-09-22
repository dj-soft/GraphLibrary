using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

namespace DjSoft.Support.WinShutDown
{
	public class NetworkAdapter
	{

		internal NetworkAdapter(NetworkMonitor owner, string adapterName)
		{
			AdapterName = adapterName;
			IsMonitored = true;
			_Owner = owner;
			_DownloadCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", adapterName);
			_UploadCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", adapterName);
			_Samples = new List<Sample>();
			_AddSample();
		}
		public override string ToString() { return this.AdapterName; }
		private NetworkMonitor _Owner;
		private PerformanceCounter _DownloadCounter;
		private PerformanceCounter _UploadCounter;
		private List<Sample> _Samples;

		internal void Stop()
		{
			_DownloadCounter?.Dispose();
			_DownloadCounter = null;
			_UploadCounter?.Dispose();
			_UploadCounter = null;
		}
		internal void Update()
		{
			if (!IsMonitored) return;
			_AddSample();
		}
		/// <summary>
		/// Přidán nový vzorek. Pokud seznam obsahuje vzorky starší než 10 minut, odebere je.
		/// </summary>
		private void _AddSample()
		{
			lock (_Samples)
			{
				DateTime now = DateTime.Now;
				DateTime old = now.AddSeconds(-600d);                             // Vzorky s časem starším než 10 minut odeberu
				_Samples.RemoveAll(s => s.Time <= old);
				_Samples.Add(new Sample(now, _DownloadCounter, _UploadCounter));  // A přidám aktuální vzorek
			}
		}
		/// <summary>
		/// Poslední sampl
		/// </summary>
		private Sample _LastSample {  get { int i = _Samples.Count - 1; return _Samples[i]; } }
		/// <summary>
		/// Jméno adapteru
		/// </summary>
		public string AdapterName { get; private set; }
		/// <summary>
		/// Je tento adapter monitorovaný?
		/// </summary>
		public bool IsMonitored { get; set; }
		public long TotalDownloadBytes { get { return _LastSample.TotalDownloadBytes; } }
		public long TotalUploadBytes { get { return _LastSample.TotalUploadBytes; } }
		public double DownloadSpeedKBps { get { return _GetSpeedKBps(_Owner.AverageTimeSeconds, 0d, DirectionType.Download); } }
		public double UploadSpeedKBps { get { return _GetSpeedKBps(_Owner.AverageTimeSeconds, 0d, DirectionType.Upload); } }
		public double GetDownloadSpeedKBps(double lastSeconds) { return _GetSpeedKBps(lastSeconds, 0d, DirectionType.Download); }
		public double GetUploadSpeedKBps(double lastSeconds) { return _GetSpeedKBps(lastSeconds, 0d, DirectionType.Upload); }
		public double GetDownloadSpeedKBps(double beginSeconds, double endSeconds) { return _GetSpeedKBps(beginSeconds, endSeconds, DirectionType.Download); }
		public double GetUploadSpeedKBps(double beginSeconds, double endSeconds) { return _GetSpeedKBps(beginSeconds, endSeconds, DirectionType.Upload); }
		public double GetMaxSpeedKBps(double beginSeconds, double endSeconds) { return _GetSpeedKBps(beginSeconds, endSeconds, DirectionType.Max); }

		private double _GetSpeedKBps(double beginSeconds, double endSeconds, DirectionType direction)
		{
			DateTime lastTime = _LastSample.Time;

			// Požadované časy mají být kladné sekundy, a begin má být větší než end:
			if (beginSeconds < 0d) beginSeconds = -beginSeconds;
			if (endSeconds < 0d) endSeconds = -endSeconds;
			if (beginSeconds < endSeconds)
            {	// Exchange time:
				var seconds = beginSeconds;
				beginSeconds = endSeconds;
				endSeconds = seconds;
			}

			// Čas počátečního a koncového samplu:
			DateTime beginTime = lastTime.AddSeconds(-beginSeconds);
			DateTime endTime = lastTime.AddSeconds(-endSeconds);

			// Najdu první a poslední sample:
			Sample beginSample = null;
			Sample endSample = null;
			lock (_Samples)
			{
				foreach (var sample in _Samples)
				{
					if (sample.Time >= beginTime)
                    {
						if (beginSample is null) beginSample = sample;
						if (sample.Time <= endTime) endSample = sample;
					}
					if (sample.Time >= endTime) break;
				}
			}

			// Doba mezi samply (Begin -> End), počet byte mezi samply:
			if (beginSample is null || endSample is null) return 0d;
			TimeSpan time = endSample.Time - beginSample.Time;
			decimal timeSeconds = (decimal)time.TotalSeconds;
			if (timeSeconds <= 0m) return 0d;

			long downloadBytes = (endSample.TotalDownloadBytes - beginSample.TotalDownloadBytes);
			long uploadBytes = (endSample.TotalUploadBytes - beginSample.TotalUploadBytes);
			long maxBytes = (downloadBytes > uploadBytes) ? downloadBytes : uploadBytes;
			long bytesCount = (direction == DirectionType.Download ? downloadBytes :
							  (direction == DirectionType.Upload ? uploadBytes :
							  (direction == DirectionType.Max ? maxBytes : 0L)));

			return (double)((((decimal)bytesCount) / 1024m) / timeSeconds);
		}
		/// <summary>
		/// Jeden sample
		/// </summary>
		private class Sample
        {
			/// <summary>
			/// Konstruktor
			/// </summary>
			/// <param name="now"></param>
			/// <param name="downloadCounter"></param>
			/// <param name="uploadCounter"></param>
			public Sample(DateTime? now, PerformanceCounter downloadCounter, PerformanceCounter uploadCounter)
            {
				Time = now ?? DateTime.Now;
				TotalDownloadBytes = downloadCounter.NextSample().RawValue;
				TotalUploadBytes = uploadCounter.NextSample().RawValue;
			}
			/// <summary>
			/// Vizualizace samplu
			/// </summary>
			/// <returns></returns>
            public override string ToString()
            {
				return "Time: " + this.Time.ToString("HH:mm:ss.fff") +
					"; TotalDownloadBytes: " + TotalDownloadBytes.ToString("### ### ### ### ##0").Trim() +
					"; TotalUploadBytes: " + TotalUploadBytes.ToString("### ### ### ### ##0").Trim();
			}
            /// <summary>
            /// Čas samplu
            /// </summary>
            public DateTime Time { get; private set; }
			/// <summary>
			/// Hodnota čítače Download
			/// </summary>
			public long TotalDownloadBytes { get; private set; }
			/// <summary>
			/// Hodnota čítače Upload
			/// </summary>
			public long TotalUploadBytes { get; private set; }
		}
		private enum DirectionType { None, Download, Upload, Max }
	}
	/// <summary>
	/// Monitor
	/// </summary>
	public class NetworkMonitor
	{
		/// <summary>
		/// Konstruktor
		/// </summary>
		public NetworkMonitor(bool started = false, double? monitorTickMiliseconds = null)
		{
			_LoadAdapters();

			double interval = 1000d;
			if (monitorTickMiliseconds.HasValue)
				interval = (monitorTickMiliseconds.Value < 100d ? 100d : (monitorTickMiliseconds.Value > 5000d ? 5000d : monitorTickMiliseconds.Value));
			_Timer = new Timer(interval);
			_Timer.Elapsed += new ElapsedEventHandler(_TimerElapsed);
			if (started) _Timer.Enabled = true;
		}
		/// <summary>
		/// Časovač mření
		/// </summary>
		private Timer _Timer;
		/// <summary>
		/// Načte přítomné adaptery
		/// </summary>
		private void _LoadAdapters()
		{
			List<NetworkAdapter> adapters = new List<NetworkAdapter>();
			PerformanceCounterCategory pcNetworkInterface = new PerformanceCounterCategory("Network Interface");
			foreach (string adapterName in pcNetworkInterface.GetInstanceNames())
			{
				if (adapterName != "MS TCP Loopback interface")
					adapters.Add(new NetworkAdapter(this, adapterName));
			}
			Adapters = adapters.ToArray();
		}
		/// <summary>
		/// Nalezené adaptery
		/// </summary>
		public NetworkAdapter[] Adapters { get; private set; }
		/// <summary>
		/// Interval měření síťového provozu v milisekundách, povolený rozsah 100 - 5000. Kratší interval = přesnější hodnoty + vyšší zátěž. Výchozí = 1000 milisekund.
		/// </summary>
		public double MonitorTickMiliseconds { get { return _Timer.Interval; } set { _Timer.Interval = (value < 100d ? 100d : (value > 5000d ? 5000d : value)); } }
		/// <summary>
		/// Čas (sekund) pro výpočet průměrné rychlosti adapteru
		/// </summary>
		public double AverageTimeSeconds { get { return _AverageTimeSeconds; } set { _AverageTimeSeconds = (value < 5d ? 5d : (value > 600d ? 600d : value)); } } private double _AverageTimeSeconds;
		/// <summary>
		/// Obsahuje true, pokud monitoring je aktivní. Lze setovat.
		/// </summary>
		public bool IsActive { get { return _Timer.Enabled; } set { _Timer.Enabled = value; } }
		private void _TimerElapsed(object sender, ElapsedEventArgs e)
		{
			_Timer.Enabled = false;
			lock (Adapters)
			{
				foreach (var adapter in Adapters)
					adapter.Update();
			}
			_Timer.Enabled = true;
		}

		public double GetMaxSpeedKBps(double lastSeconds)
		{
			return GetMaxSpeedKBps(lastSeconds, 0d);
		}

		public double GetMaxSpeedKBps(double beginSeconds, double endSeconds)
		{
			double maxSpeed = 0d;
			lock (Adapters)
			{
				foreach (var adapter in Adapters)
				{
					double max = adapter.GetMaxSpeedKBps(beginSeconds, endSeconds);
					if (maxSpeed < max) maxSpeed = max;
				}
			}
			return maxSpeed;
		}
		/// <summary>
		/// Zastaví všechno sledování
		/// </summary>
		public void Stop()
		{
			_Timer.Enabled = false;
			lock (Adapters)
			{
				foreach (var adapter in Adapters)
					adapter.Stop();
			}
			Adapters = new NetworkAdapter[0];
		}
	}
}
