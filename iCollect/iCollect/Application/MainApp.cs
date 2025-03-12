using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.App.iCollect.Application
{
    /// <summary>
    /// Singleton hlavní aplikace
    /// </summary>
    internal class MainApp
    {
        #region Singeton
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static MainApp _Instance
        {
            get
            {
                if (__Instance is null)
                {
                    lock (__Locker)
                    {
                        if (__Instance is null)
                        {
                            __Instance = new MainApp();
                            __Instance._Init();
                        }
                    }
                }
                return __Instance;
            }
        }
        private static MainApp __Instance;
        private static object __Locker = new object();
        #endregion
        #region Inicializace
        private MainApp()
        { }
        private void _Init()
        {
            _PrepareGui();
            _LoadSettings();
            _CreateStyleManager();
        }
        #endregion
        #region Start
        public static void Start()
        {
            _Instance._Start();
        }
        private void _Start()
        {
            __MainAppForm = new MainAppForm();
            System.Windows.Forms.Application.Run(__MainAppForm);
            __MainAppForm = null;
        }
        private void _PrepareGui()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            System.Threading.Thread.CurrentThread.Name = "GUI thread";
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;
        }
        /// <summary>
        /// Main okno aplikace
        /// </summary>
        public static MainAppForm MainAppForm { get { return _Instance.__MainAppForm; } }
        private MainAppForm __MainAppForm;
        #endregion
        #region Konfigurace běhu, style manager
        /// <summary>
        /// Konfigurace aplikace
        /// </summary>
        public static Settings Settings { get { return _Instance.__Settings; } }
        /// <summary>Konfigurace aplikace</summary>
        private Settings __Settings;
        /// <summary>
        /// Načte konfiguraci aplikace
        /// </summary>
        private void _LoadSettings()
        {
            __Settings = new Settings(AppConfigPath);
        }


        private void _CreateStyleManager()
        {
            __DxStyleManager = new DxVisualStyleManager();

        }
        private DxVisualStyleManager __DxStyleManager;
        #endregion
        #region Adresáře a registry aplikace
        private const string AppCompanyName = "DjSoft";
        private const string AppProductName = "iCollect";
        private const string AppConfigFolder = "Config";
        private const string AppTraceFolder = "Trace";
        private const string AppWinRegWorkingPathName = "WorkingPath";
        /// <summary>
        /// Adresář umístění EXE souboru aplikace. Typicky je ReadOnly.
        /// </summary>
        public static string AppExePath
        {
            get
            {
                if (__AppExePath is null)
                    __AppExePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                return __AppExePath;
            }
        }
        private static string __AppExePath = null;
        /// <summary>
        /// Pracovní adresář pro soubory aplikace, dokumentový, jiný než EXE.  S možností zápisů.
        /// Typicky "c:\Users\Public\Documents\Asseco Solutions\ArchivingUtility" 
        /// Existenci je nutno zajistit externě, např. <see cref="PrepareAppPath(string)"/>.
        /// </summary>
        public static string AppWorkingPath
        {
            get
            {
                if (__AppWorkingPath is null)
                    __AppWorkingPath = _SearchForAppWorkingPath();
                return __AppWorkingPath;
            }
        }
        private static string __AppWorkingPath = null;
        /// <summary>
        /// Adresář pro konfigurační soubory aplikace, dokumentový, jiný než EXE.  S možností zápisů.
        /// Typicky "c:\Users\Public\Documents\Asseco Solutions\ArchivingUtility\Config" 
        /// Existenci je nutno zajistit externě, např. <see cref="PrepareAppPath(string)"/>.
        /// </summary>
        public static string AppConfigPath
        {
            get
            {
                if (__AppConfigPath is null)
                {
                    string appConfigPath = "";
                    string appDataPath1 = AppWorkingPath;
                    if (!String.IsNullOrEmpty(appDataPath1))
                        appConfigPath = System.IO.Path.Combine(appDataPath1, AppConfigFolder);
                    __AppConfigPath = appConfigPath;
                }
                return __AppConfigPath;
            }
        }
        private static string __AppConfigPath = null;
        /// <summary>
        /// Adresář pro trace soubory aplikace, dokumentový, jiný než EXE.  S možností zápisů.
        /// Typicky "c:\Users\Public\Documents\Asseco Solutions\ArchivingUtility\Trace" 
        /// Existenci je nutno zajistit externě, např. <see cref="PrepareAppPath(string)"/>.
        /// </summary>
        public static string AppTracePath
        {
            get
            {
                if (__AppTracePath is null)
                {
                    string appTracePath = "";
                    string appDataPath1 = AppWorkingPath;
                    if (!String.IsNullOrEmpty(appDataPath1))
                        appTracePath = System.IO.Path.Combine(appDataPath1, AppTraceFolder);
                    __AppTracePath = appTracePath;
                }
                return __AppTracePath;
            }
        }
        private static string __AppTracePath = null;
        /// <summary>
        /// Adresář ve Windows registru pro <c>CurrentUser/Software/Company/Product/Config</c>
        /// </summary>
        public static WinRegFolder AppWinRegUserAppFolder
        {
            get
            {
                return WinRegFolder.CreateForProcessView(Microsoft.Win32.RegistryHive.CurrentUser, $"Software\\{AppCompanyName}\\{AppProductName}\\{AppConfigFolder}");
            }
        }
        /// <summary>
        /// Metoda připraví trace adresář (jméno, existenci, vyčištění) a vrátí jméno souboru s daným prefixem v tomto adresáři.
        /// </summary>
        /// <param name="namePrefix"></param>
        /// <param name="addTimeToName">Do jména trace souboru vkládat i čas?</param>
        /// <returns></returns>
        public static string GetTraceFile(string namePrefix, bool addTimeToName)
        {
            string fileName = null;
            var tracePath = MainApp.AppTracePath;
            if (!String.IsNullOrEmpty(tracePath) && MainApp.TryPrepareAppPath(tracePath))
            {
                int maxCount = (System.Diagnostics.Debugger.IsAttached ? 14 : 120);                          // U vývojáře ponechám 14 trace souborů, u produkce 120 trace souborů
                MainApp.ClearAppPathOldFiles(tracePath, "*.csv", TimeSpan.FromDays(90), maxCount - 1);       // Smažu starší než 90 dnů. Smažu tolik, aby po přidání jednoho nově přidávaného trace byl počet 120 (nebo 14).
                string mask = (addTimeToName ? "yyyy-MM-dd HH-mm-ss" : "yyyy-MM-dd");
                string time = DateTime.Now.ToString(mask);
                string traceName = namePrefix + time + ".csv";
                fileName = System.IO.Path.Combine(tracePath, traceName);                                     // Soubor nevytváříme
            }
            return fileName;
        }
        /// <summary>
        /// Ověří, že daný adresář existuje, a případně jej i vytvoří.
        /// </summary>
        /// <param name="path"></param>
        public static bool TryPrepareAppPath(string path, bool testAccess = false)
        {
            return _TryPrepareAppPath(path, testAccess);
        }
        /// <summary>
        /// Ověří, že daný adresář existuje, a případně jej i vytvoří.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="testAccess"></param>
        private static bool _TryPrepareAppPath(string path, bool testAccess)
        {
            bool isValid = false;
            if (String.IsNullOrEmpty(path)) return isValid;
            if (System.IO.Directory.Exists(path))
            {   // Test dostupnosti:
                isValid = (!testAccess || (testAccess && isAccessible(path)));
                return isValid;
            }

            string parentPath = System.IO.Path.GetDirectoryName(path);
            bool isParentValid = _TryPrepareAppPath(parentPath, false);        // Rekurze. Pokud by parentPath bylo prázdné nebo existovala, pak se nic neděje. Pokud i ta bude mít Parenta, pak rekurze...
            if (!isParentValid) return false;

            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch 
            {
                return false; 
            }
            finally
            { }

            if (!System.IO.Directory.Exists(path)) return false;               // Nepodařilo se vytvořit

            // Test dostupnosti:
            isValid = (!testAccess || (testAccess && isAccessible(path)));
            return isValid;


            // Test dostupnosti
            bool isAccessible(string accessPath)
            {
                bool result = false;
                try
                {
                    string name = "_check_" + Guid.NewGuid().ToString().Replace("-", "_") + ".tmp";
                    string fullName = System.IO.Path.Combine(accessPath, name);
                    string saveContent = DateTime.Now.ToString("d.MMMM yyyy; HH:mm:ss.fff");
                    System.IO.File.WriteAllText(fullName, saveContent);
                    string readContent = System.IO.File.ReadAllText(fullName);
                    System.IO.File.Delete(fullName);
                    result = String.Equals(saveContent, readContent);
                }
                catch { }
                return result;
            }
        }
        /// <summary>
        /// Vyhledá a vrátí základní adresář pro aplikaci
        /// </summary>
        /// <returns></returns>
        private static string _SearchForAppWorkingPath()
        {
            string path = null;

            // 1. Pokud najdu adresář v WinReg, a ten existuje, pak jej vezmu:
            string winRegPath = WinReg.ReadString(AppWinRegUserAppFolder, AppWinRegWorkingPathName, "");
            if (!String.IsNullOrEmpty(winRegPath) && TryPrepareAppPath(winRegPath, true)) return winRegPath;

            // 2. Zkusíme vytvořit adresář v některé systémové složce:
            if (tryPath(Environment.SpecialFolder.CommonDocuments)) return path;
            if (tryPath(Environment.SpecialFolder.CommonApplicationData)) return path;
            if (tryPath(Environment.SpecialFolder.CommonPrograms)) return path;
            if (tryPath(Environment.SpecialFolder.LocalApplicationData)) return path;
            if (tryPath(Environment.SpecialFolder.MyDocuments)) return path;
            if (tryPath(Environment.SpecialFolder.Personal)) return path;

            // Nedá se:
            return "";

            // Zkusí připravit aplikační adresář v dané lokalitě
            bool tryPath(Environment.SpecialFolder folderType)
            {
                string testPath = System.IO.Path.Combine(Environment.GetFolderPath(folderType), AppCompanyName, AppProductName);
                if (TryPrepareAppPath(testPath, true))
                {   // Máme adresář => zapíšu si jej do registru, abych jej příště použil přednostně:
                    WinReg.WriteString(AppWinRegUserAppFolder, AppWinRegWorkingPathName, testPath);
                    path = testPath;
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Z dodaného adresáře smaže všechny soubory, jejichž datum založení nebo modifikace je starší než daný interval.
        /// Neřeší SubDirs.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mask"></param>
        /// <param name="time"></param>
        /// <param name="maxCount"></param>
        public static void ClearAppPathOldFiles(string path, string mask, TimeSpan time, int maxCount)
        {
            if (String.IsNullOrEmpty(path)) return;
            if (!System.IO.Directory.Exists(path)) return;

            var pathInfo = new System.IO.DirectoryInfo(path);
            if (!pathInfo.Exists) return;
            var fileInfos = pathInfo.GetFiles(mask, System.IO.SearchOption.TopDirectoryOnly);
            if (fileInfos.Length == 0) return;

            var files = fileInfos.Select(file => new Tuple<DateTime, System.IO.FileInfo>((file.LastWriteTimeUtc > file.CreationTimeUtc ? file.LastWriteTimeUtc : file.CreationTimeUtc), file)).ToList();
            files.Sort((a, b) => DateTime.Compare(b.Item1, a.Item1));         // ORDER BY Time DESC => na první pozici bude nejnovější.

            var oldDate = DateTime.UtcNow.Subtract(time);                     // Pokud Now = 20.2.2025 a time = 90 dnů, pak oldDate = 20.11.2024 (zhruba).
            for (int i = 0; i < files.Count; i++)
            {   // Na indexu 0 jsou nejnovější soubory.
                var fileDate = files[i].Item1;
                bool toDelete = (fileDate <= oldDate) || i >= maxCount;       // Smažu všechny starší než oldDate, a smažu ty, které jsou v pořadí nadpočetné (na konci seznamu jsou ty starší soubory)
                if (toDelete)
                {
                    var fileInfo = files[i].Item2;
                    try { fileInfo.Delete(); }
                    catch { }
                }
            }
        }
        #endregion
    }
}
