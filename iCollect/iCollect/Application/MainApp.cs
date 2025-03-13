using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.App.iCollect.Application
{
    /// <summary>
    /// Singleton hlavní aplikace
    /// </summary>
    internal class MainApp : IMainAppWorking
    {
        #region Singleton OnDemand
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
        /// <summary>
        /// Pracovní instance
        /// </summary>
        public static IMainAppWorking WorkingInstance { get { return _Instance; } }
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
        #region Události aplikace
        /// <summary>
        /// Událost po změně skinu
        /// </summary>
        public event EventHandler SkinChanged { add { _Instance.__SkinChanged += value; } remove { _Instance.__SkinChanged -= value; } }
        private event EventHandler __SkinChanged;
        void IMainAppWorking.RunSkinChangedEvent() { __SkinChanged?.Invoke(null, EventArgs.Empty); }
        #endregion
        #region Konfigurace běhu, style manager
        /// <summary>
        /// Konfigurace aplikace
        /// </summary>
        public static Settings Settings { get { return _Instance.__Settings; } }
        /// <summary>Konfigurace aplikace</summary>
        private Settings __Settings;
        /// <summary>
        /// Aktuální GUI vizuální styl. Vždy je načten z GUI. Setování vepíše hodnoty do GUI a následně vyvolá event o změně a uložení do Configu.
        /// </summary>
        public static DxVisualStyle CurrentVisualStyle { get { return _Instance.__DxStyleManager?.CurrentVisualStyle; } set { _Instance.__DxStyleManager.CurrentVisualStyle = value; } }
        /// <summary>
        /// Načte konfiguraci aplikace
        /// </summary>
        private void _LoadSettings()
        {
            __Settings = new Settings(AppConfigPath);
        }
        /// <summary>
        /// Vytvoří StyleManagera
        /// </summary>
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
        private const string AppDataFolder = "Data";
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
        /// Adresář pro hlavní datové soubory aplikace, dokumentový, jiný než EXE.  S možností zápisů.
        /// Typicky "c:\Users\Public\Documents\Asseco Solutions\ArchivingUtility\Trace" 
        /// Existenci je nutno zajistit externě, např. <see cref="PrepareAppPath(string)"/>.
        /// </summary>
        public static string AppDataPath
        {
            get
            {
                if (__AppDataPath is null)
                {
                    string appDataPath = "";
                    string appDataPath1 = AppWorkingPath;
                    if (!String.IsNullOrEmpty(appDataPath1))
                        appDataPath = System.IO.Path.Combine(appDataPath1, AppDataFolder);
                    __AppDataPath = appDataPath;
                }
                return __AppDataPath;
            }
        }
        private static string __AppDataPath = null;
        /// <summary>
        /// Adresář ve Windows registru pro <c>CurrentUser/Software/Company/Product/Config</c>
        /// </summary>
        public static WinRegFolder AppWinRegUserAppFolder
        {
            get { return WinRegFolder.CreateForProcessView(Microsoft.Win32.RegistryHive.CurrentUser, $"Software\\{AppCompanyName}\\{AppProductName}\\{AppConfigFolder}"); }
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
        /// Ověří, že adresář potřebný pro zadaný soubor existuje, a případně jej i vytvoří.
        /// </summary>
        /// <param name="fileName"></param>
        public static bool TryPrepareAppPathForFile(string fileName, bool testAccess = false)
        {
            string path = System.IO.Path.GetDirectoryName(fileName);
            return _TryPrepareAppPath(path, testAccess);
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
        #region Multimonitory
        /// <summary>
        /// Aktuální klíč konfigurace všech monitorů.
        /// <para/>
        /// String popisuje všechny aktuálně přítomné monitory, jejich příznak Primární, a jejich souřadnice.<br/>
        /// String lze použít jako klíč: obsahuje pouze písmena a číslice, nic více.<br/>
        /// Ze stringu nelze rozumně sestavit souřadnice monitorů, to ale není nutné. Cílem je krátký klíč.<br/>
        /// String slouží jako suffix klíče pro ukládání souřadnic uživatelských oken, aby bylo možno je ukládat pro jednotlivé konfigurace monitorů.
        /// Je vhodné ukládat souřadnice pracovních oken pro jejich restorování s klíčem aktuální konfigurace monitorů: 
        /// uživatel používající různé konfigurace monitorů očekává, že konkrétní okno se mu po otevření zobrazí na konkrétním místě v závislosti na tom, které monitory právě používá.
        /// </summary>
        public static string CurrentMonitorsKey { get { return _GetCurrentMonitorsKey(); } }
        /// <summary>
        /// Určí a vrátí Aktuální klíč konfigurace všech monitorů.
        /// </summary>
        /// <returns></returns>
        private static string _GetCurrentMonitorsKey()
        {
            string key = "";
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                key += getOneKey(screen);

            // key = "PaapjSzwz1371rm"
            return key;

            string getOneKey(System.Windows.Forms.Screen scr)
            {
                // one = "Paapj", anebo se zápornými čísly a s číselnými hodnotami: "Szwz1371rm"
                var bounds = scr.Bounds;
                string one = (scr.Primary ? "P" : "S") + getSiz(bounds.X) + getSiz(bounds.Y) + getSiz(bounds.Width) + getSiz(bounds.Height);
                return one;
            }

            string getSiz(int siz)
            {
                string prefix = (siz < 0 ? "z" : "");
                if (siz < 0) siz = -siz;
                switch (siz)
                {   // Standardní rozměry převedu na jednopísmeno:
                    case 0000: return prefix + "a";
                    case 0320: return prefix + "b";
                    case 0480: return prefix + "c";
                    case 0540: return prefix + "d";
                    case 0640: return prefix + "e";
                    case 0720: return prefix + "f";
                    case 0800: return prefix + "g";
                    case 0960: return prefix + "h";
                    case 1024: return prefix + "i";
                    case 1080: return prefix + "j";
                    case 1280: return prefix + "k";
                    case 1366: return prefix + "l";
                    case 1440: return prefix + "m";
                    case 1536: return prefix + "n";
                    case 1728: return prefix + "o";
                    case 1920: return prefix + "p";
                    case 2160: return prefix + "q";
                    case 2560: return prefix + "r";
                    case 2880: return prefix + "s";
                    case 3072: return prefix + "t";
                    case 3200: return prefix + "u";
                    case 3440: return prefix + "v";
                    case 3840: return prefix + "w";
                    case 4320: return prefix + "x";
                    case 4880: return prefix + "y";
                }
                // Nestandardní rozměry a pozice oken (X, Y) mimo std hodnoty budou numericky:
                return prefix + siz.ToString();
            }

        }
        #endregion    }
    }
    internal interface IMainAppWorking
    {
        void RunSkinChangedEvent();
    }
}