using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace Djs.Tools.WebDownloader
{
    public class App
    {
        #region Singleton
        /// <summary>
        /// Soukromý přístup k singletonu
        /// </summary>
        protected static App Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_InstanceLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new App();
                        }
                    }
                }
                return _Instance;
            }
        }
        private App()
        {
            this._AppPath = Path.GetDirectoryName(this.GetType().Assembly.Location);
            this._ConfigPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Djs", "WebDownloader");
            this._Config = new Config(Path.Combine(this._ConfigPath, "config." + Config.EXTENSION));
        }
        private static App _Instance;
        private static object _InstanceLock = new object();
        #endregion
        #region Adresáře
        /// <summary>
        /// Adresář, kde je spuštěna aplikace
        /// </summary>
        public static string AppPath { get { return Instance._AppPath; } } private string _AppPath;
        /// <summary>
        /// Adresář, kde je ukládána konfigurace (=Aplikační data/Djs/WebDownloader) : jako config.ini, tak soubory s uloženými adresami.
        /// Tento adresář uživatel nemůže změnit.
        /// </summary>
        public static string ConfigPath { get { return Instance._ConfigPath; } } private string _ConfigPath;
        /// <summary>
        /// Zajistí, že daný adresář bude existovat.
        /// Pokud to nelze, hodí chybu AppException.
        /// </summary>
        /// <param name="path"></param>
        public static void CreatePath(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new AppException("App.CreatePath: Není zadán adresář, který má existovat.");
            if (Directory.Exists(path)) return;
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception exc)
            {
                throw new AppException("App.CreatePath: Zadaný adresář " + path + " neexistuje a není možno jej vytvořit, chyba:" + exc.Message, exc);
            }
            if (Directory.Exists(path)) return;
            throw new AppException("App.CreatePath: Zadaný adresář " + path + " neexistuje a není možno jej vytvořit.");
        }
        #endregion
        #region Konfigurace
        /// <summary>
        /// Uživatelova konfigurace
        /// </summary>
        public static Config Config { get { return Instance._Config; } } private Config _Config;
        #endregion
        #region MainForm
        /// <summary>
        /// Reference na Main formulář
        /// </summary>
        public static Form MainForm
        {
            get { return Instance._MainForm; }
            set
            {
                if (value != null)
                {
                    value.ShowInTaskbar = true;
                    Instance._GuiThread = Thread.CurrentThread;
                    Instance._GuiThread.Name = "GuiThread";
                }
                Instance._MainForm = value;
            }
        }
        private Form _MainForm;
        private Thread _GuiThread;
        #endregion
        #region Run
        /// <summary>
        /// Provede danou akci v obálce try, chybu hlásí dialogem.
        /// </summary>
        /// <param name="action"></param>
        public static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (AppException exc)
            {
                Dialogs.Error(exc.Message);
            }
            catch (Exception exc)
            {
                Dialogs.Error("Došlo k chybě: " + exc.Message + Environment.NewLine + exc.StackTrace);
            }
        }
        #endregion
    }
    #region AppException
    public class AppException : Exception
    {
        public AppException(string message) : base(message) { }
        public AppException(string message, Exception innerException) : base(message, innerException) { }
    }
    #endregion
}
