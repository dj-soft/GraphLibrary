using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;
using DjSoft.Tools.ProgramLauncher.Components;
using DjSoft.Tools.ProgramLauncher.Data;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    #region class PageData : Jedna stránka s daty, je zobrazena Tabem v levém bloku, popisuje celý obsah v pravé velké části
    /// <summary>
    /// Jedna stránka s daty, je zobrazena Tabem v levém bloku, popisuje celý obsah v pravé velké části
    /// </summary>
    public class PageData : BaseData
    {
        public PageData()
        {
            Groups = new List<GroupData>();
        }
        public override string ToString()
        {
            return this.Title;
        }
        /// <summary>
        /// Počet evidovaných aplikací
        /// </summary>
        public int ApplicationsCount { get { return this.Groups.Sum(g => g.Applications.Count); } }
        public List<GroupData> Groups { get; private set; }
        public void CreateInteractiveItems(List<InteractiveItem> interactiveItems)
        {
            foreach (var group in this.Groups)
            {
                interactiveItems.Add(group.CreateInteractiveItem());
                foreach (var appl in group.Applications)
                    interactiveItems.Add(appl.CreateInteractiveItem());
            }
        }
        #region Kontextové menu
        /// <summary>
        /// Nabídne kontextové menu pro danou aplikaci
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="applicationData"></param>
        internal void RunContextMenu(MouseState mouseState, ApplicationData applicationData)
        {
            bool hasItem = (applicationData != null);
            var menuItems = new List<IMenuItem>();
            menuItems.Add(new DataMenuItem() { Text = "Spustit", Image = Properties.Resources.media_playback_start_3_22, UserData = "Run", Enabled = hasItem });
            menuItems.Add(new DataMenuItem() { Text = "Spustit jako správce", Image = Properties.Resources.media_seek_forward_3_22, UserData = "RunAs", Enabled = hasItem });
            menuItems.Add(new DataMenuItem() { Text = "Odstranit", Image = Properties.Resources.delete_22, UserData = "Delete", Enabled = hasItem });
            menuItems.Add(new DataMenuItem() { Text = "Upravit", Image = Properties.Resources.edit_3_22, UserData = "Edit", Enabled = hasItem });
            menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Separator });
            menuItems.Add(new DataMenuItem() { Text = "Nový zástupce", Image = Properties.Resources.document_new_3_22, UserData = "NewApp" });
            menuItems.Add(new DataMenuItem() { Text = "Nová skupina", Image = Properties.Resources.edit_remove_3_22, UserData = "NewGroup" });

            App.SelectFromMenu(menuItems, doContextMenu, mouseState.LocationAbsolute);

            // Provede vybranou akci z kontextového menu
            void doContextMenu(IMenuItem menuItem)
            {
                if (menuItem.UserData is string code)
                {
                    switch (code)
                    {
                        case "Run":
                            applicationData.RunNewProcess(false);
                            break;
                        case "RunAs":
                            applicationData.RunNewProcess(true);
                            break;
                    }
                }
            }
        }
        #endregion
        #region Tvorba výchozích dat - namísto prázdných
        /// <summary>
        /// Zajistí, že v daném seznamu bude alespoň jeden prvek, a že první prvek nebude prázdný.
        /// </summary>
        /// <param name="programPages"></param>
        internal static void CheckNotVoid(List<PageData> programPages)
        {
            if (programPages.Count == 0)
                programPages.Add(PageData.CreateInitialPageData());
            GroupData.CheckNotVoid(programPages[0].Groups);
        }
        /// <summary>
        /// Vytvoří a vrátí defaultní prvek
        /// </summary>
        /// <returns></returns>
        public static PageData CreateInitialPageData()
        {
            PageData pageData = new PageData();
            pageData.Title = "Výchozí";
            pageData.ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\klickety.png";
            return pageData;
        }
        #endregion
    }
    #endregion
    #region class GroupData : Jedna skupina dat, je zobrazena v pravé velké části, a je reprezentována vodorovným titulkem přes celou šířku, obsahuje sadu aplikací
    /// <summary>
    /// Jedna skupina dat, je zobrazena v pravé velké části, a je reprezentována vodorovným titulkem přes celou šířku, obsahuje sadu aplikací
    /// </summary>
    public class GroupData : BaseData
    {
        public GroupData()
        {
            Applications = new List<ApplicationData>();
        }

        public List<ApplicationData> Applications { get; private set; }

        #region Tvorba výchozích dat - namísto prázdných
        /// <summary>
        /// Zajistí, že v daném seznamu bude alespoň jeden prvek, a že první prvek nebude prázdný.
        /// </summary>
        /// <param name="programGroups"></param>
        internal static void CheckNotVoid(List<GroupData> programGroups)
        {
            if (programGroups.Count == 0)
                programGroups.Add(GroupData.CreateInitialGroupData());
            ApplicationData.CheckNotVoid(programGroups[0].Applications);
        }
        /// <summary>
        /// Vytvoří a vrátí defaultní prvek
        /// </summary>
        /// <returns></returns>
        public static GroupData CreateInitialGroupData()
        {
            GroupData groupData = new GroupData();
            groupData.Title = "Základní skupina v nabídce";
            return groupData;
        }
        #endregion
    }
    #endregion
    #region class ApplicationData : Jeden prvek nabídky, konkrétní aplikace = spustitelný cíl
    /// <summary>
    /// Jeden prvek nabídky, konkrétní aplikace = spustitelný cíl
    /// </summary>
    public class ApplicationData : BaseData
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Title: {Title}; Executable: {ExecutableFileName}";
        }
        public string ExecutableFileName { get; set; }
        public string ExecutableWorkingDirectory { get; set; }
        public string ExecutableArguments { get; set; }
        public bool ExecuteInAdminMode { get; set; }
        public bool OpenMaximized { get; set; }
        public bool OnlyOneInstance { get; set; }

        #region Spouštění aplikace
        /// <summary>
        /// Spustí / aktivuje daný proces
        /// </summary>
        public void RunApplication()
        {
            try
            {
                //   if (OnlyOneInstance && _TryActivateProcess()) return;
                App.MainForm.StatusLabelApplicationRunText = this.Title;
                App.MainForm.StatusLabelApplicationRunImage = ImageKindType.MediaForward;
                _RunNewProcess(null);
            }
            catch (Exception exc)
            {
                App.ShowMessage(exc.Message, MessageBoxIcon.Error);
            }
            finally
            {
                App.MainForm.StatusLabelApplicationRunText = null;
                App.MainForm.StatusLabelApplicationRunImage = null;
            }
        }
        /// <summary>
        /// Spustí new proces, volitelně lze specifikovat Admin mode
        /// </summary>
        /// <param name="executeInAdminMode"></param>
        public void RunNewProcess(bool? executeInAdminMode = null)
        {
            try
            {
                //   if (OnlyOneInstance && _TryActivateProcess()) return;
                App.MainForm.StatusLabelApplicationRunText = this.Title;
                App.MainForm.StatusLabelApplicationRunImage = ImageKindType.MediaForward;
                _RunNewProcess(executeInAdminMode);
            }
            catch (Exception exc)
            {
                App.ShowMessage(exc.Message, MessageBoxIcon.Error);
            }
            finally
            {
                App.MainForm.StatusLabelApplicationRunText = null;
                App.MainForm.StatusLabelApplicationRunImage = null;
            }
        }
        private bool _TryActivateProcess()
        {
            var allProcesses = System.Diagnostics.Process.GetProcesses();
            var myProcesses = allProcesses.Where(p => p.MainModule.FileName == ExecutableFileName).ToArray();

            return false;
        }
        private void _RunNewProcess(bool? executeInAdminMode)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = ExecutableFileName,
                Arguments = ExecutableArguments,
                WindowStyle = (OpenMaximized ? System.Diagnostics.ProcessWindowStyle.Maximized : System.Diagnostics.ProcessWindowStyle.Normal),
                UseShellExecute = true
            };

            bool adminMode = executeInAdminMode ?? ExecuteInAdminMode;
            if (adminMode) psi.Verb = "runas";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = psi;
            process.Start();
        }
        #endregion
        #region Tvorba výchozích dat - namísto prázdných
        /// <summary>
        /// Zajistí, že v daném seznamu bude alespoň nějaký výchozí prvek.
        /// </summary>
        /// <param name="programApplications"></param>
        internal static void CheckNotVoid(List<ApplicationData> programApplications)
        {
            if (programApplications.Count == 0)
                programApplications.AddRange(ApplicationData.CreateInitialApplicationsData());
        }
        /// <summary>
        /// Vytvoří a vrátí sadu defaultních prvků
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<ApplicationData> CreateInitialApplicationsData()
        {
            var list = new List<ApplicationData>();

            list.Add(new ApplicationData()
            {
                Title = "Windows",
                Description = "Průzkumník",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\wine.png",
                OpenMaximized = true,
                Adress = new Point(0, 0),
                ExecutableFileName = @"c:\Windows\explorer.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Wordpad",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\abiword.png",
                Adress = new Point(1, 0),
                ExecutableFileName = @"c:\Windows\write.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "MS DOS user",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\evilvte.png",
                ExecuteInAdminMode = false,
                OpenMaximized = true,
                Adress = new Point(0, 1),
                ExecutableFileName = @"c:\Windows\System32\cmd.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "MS DOS Admin",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\evilvte.png",
                ExecuteInAdminMode = true,
                Adress = new Point(0, 2),
                ExecutableFileName = @"c:\Windows\System32\cmd.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Libre Office",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\distributions-solaris.png",
                Adress = new Point(1, 1),
                OnlyOneInstance = true,
                ExecutableFileName = @"c:\Program Files (x86)\OpenOffice 4\program\soffice.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Dokument",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\gpe-tetris.png",
                Adress = new Point(2, 0),
                ExecutableFileName = @"d:\Dokumenty\Vyděšený svišť.png"
            });

            return list;
        }
        #endregion
    }
    #endregion
    #region class BaseData : Bázová třída pro data, účastní se zobrazování (obsahuje dostatečná data pro zobrazení)
    /// <summary>
    /// Bázová třída pro data, účastní se zobrazování (obsahuje dostatečná data pro zobrazení)
    /// </summary>
    public class BaseData
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Title: {Title}";
        }
        /// <summary>
        /// Hlavní titulek
        /// </summary>
        public virtual string Title { get; set; }
        /// <summary>
        /// Podtitulek
        /// </summary>
        public virtual string Description { get; set; }
        /// <summary>
        /// Obsah Tooltipu
        /// </summary>
        public virtual string ToolTip { get; set; }
        /// <summary>
        /// Jméno obrázku
        /// </summary>
        public virtual string ImageFileName { get; set; }
        /// <summary>
        /// Adresa prvku v controlu
        /// </summary>
        public virtual Point Adress { get; set; }
        /// <summary>
        /// Barva pozadí, smí obsahovat Alpha kanál, smí být null
        /// </summary>
        public virtual Color? BackColor { get; set; }
        /// <summary>
        /// Vytvoří a vrátí new instanci interaktivního prvku <see cref="InteractiveDataItem"/> = potomek základního prvku <see cref="InteractiveItem"/>
        /// </summary>
        /// <returns></returns>
        public virtual InteractiveItem CreateInteractiveItem()
        {
            return new InteractiveDataItem(this);
        }
    }
    /// <summary>
    /// Potomek vizuálního = interaktivního prvku vytvořený nad daty <see cref="BaseData"/>.
    /// </summary>
    public class InteractiveDataItem : InteractiveItem
    {
        public InteractiveDataItem(BaseData data)
        {
            __Data = data;
            __CellBackColor = ColorSet.CreateAllColors(data.BackColor);
            UserData = data;
        }
        private BaseData __Data;
        public override string MainTitle { get { return __Data.Title; } set { __Data.Title = value; } }
        public override string Description { get { return __Data.Description; } set { __Data.Description = value; } }
        public override Point Adress { get { return __Data.Adress; } set { __Data.Adress = value; } }
        public override string ImageName { get { return __Data.ImageFileName; } set { __Data.ImageFileName = value; } }
        public override ColorSet CellBackColor { get { return __CellBackColor; } set { __CellBackColor = value; } } private ColorSet __CellBackColor;
    }
    #endregion
}

namespace DjSoft.Tools.ProgramLauncher
{
    partial class Settings
    {
        #region Část Settings, která ukládá a načítá vlastní data o stránkách, grupách a aplikacích = ProgramPages
        /// <summary>
        /// Jednotlivé stránky. Jedna každá stránka je zobrazena jako Tab v levém bloku, a popisuje celý obsah (grupy a v nich aplikace) v pravé velké části.
        /// Seznam stránek má vždy alespoň jednu stránku, obsahující jednu grupu a výchozí aplikace.
        /// </summary>
        [PersistingEnabled(false)]
        public List<PageData> ProgramPages { get { return _ProgramPagesGet(); } }
        private List<PageData> _ProgramPagesGet()
        {
            if (_ProgramPages == null) _ProgramPages = new List<PageData>();
            PageData.CheckNotVoid(_ProgramPages);
            return _ProgramPages;
        }
        /// <summary>
        /// Dictionary obsahující data jednotlivých stránek (uvnitř nich jsou grupy a v grupách aplikace)
        /// </summary>
        [PropertyName("program_groups")]
        private List<PageData> _ProgramPages { get; set; }
        /*
        /// <summary>
        /// Uloží dodanou pozici formuláře do Settings pro aktuální / obecnou konfiguraci monitorů.<br/>
        /// Dodanou pozici <paramref name="positionData"/> uloží pod daným jménem <paramref name="settingsName"/>, 
        /// a dále pod jménem rozšířeným o kód aktuálně přítomných monitorů <see cref="Monitors.CurrentMonitorsKey"/>.
        /// <para/>
        /// Důvodem je to, že při pozdějším načítání se primárně načte pozice okna pro aktuálně platnou sestavu přítomných monitorů <see cref="Monitors.CurrentMonitorsKey"/>.
        /// Pak bude okno restorováno do posledně známé pozice na konkrétním monitoru.<br/>
        /// Pokud pozice daného okna <paramref name="settingsName"/> pro aktuální konfiguraci monitorů nebude nalezena,
        /// pak se vyhledá pozice posledně známá bez ohledu na konfiguraci monitoru. Viz <see cref="FormPositionLoad(string)"/>.
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="positionData"></param>
        public void FormPositionSave(string settingsName, string positionData)
        {
            if (String.IsNullOrEmpty(settingsName)) return;

            var positions = _FormPositionsGetDictionary();
            string key;

            key = _FormPositionGetKey(settingsName, false);
            positions.StoreValue(key, positionData);

            key = _FormPositionGetKey(settingsName, true);
            positions.StoreValue(key, positionData);

            this.SetChanged();
        }
        /// <summary>
        /// Zkusí najít pozici pro formulář daného jména a aktuální / nebo obecnou konfiguraci monitorů.
        /// Může vrátit null když nenajde uloženou pozici.<br/>
        /// Metoda neřeší obsah vracených dat a tedy ani správnost souřadnic, jde čistě o string který si řeší volající.<br/>
        /// Zdejší metoda jen reaguje na aktuální konfiguraci monitorů.
        /// </summary>
        /// <param name="settingsName"></param>
        /// <returns></returns>
        public string FormPositionLoad(string settingsName)
        {
            if (String.IsNullOrEmpty(settingsName)) return null;

            var positions = _FormPositionsGetDictionary();
            string key;

            key = _FormPositionGetKey(settingsName, true);
            if (positions.TryGetValue(key, out var positionData1)) return positionData1;

            key = _FormPositionGetKey(settingsName, false);
            if (positions.TryGetValue(key, out var positionData2)) return positionData2;

            return null;
        }
        /// <summary>
        /// Vrátí klíč pro pozici formuláře
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="withMonitorsKey"></param>
        /// <returns></returns>
        private static string _FormPositionGetKey(string settingsName, bool withMonitorsKey)
        {
            return settingsName + (withMonitorsKey ? " at " + Monitors.CurrentMonitorsKey : "");
        }
        /// <summary>
        /// Vrátí dictionary obsahující data s pozicemi formulářů
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> _FormPositionsGetDictionary()
        {
            if (_FormPositions is null)
                _FormPositions = new Dictionary<string, string>();
            return _FormPositions;
        }
        /// <summary>
        /// Dictionary obsahující data s pozicemi formulářů
        /// </summary>
        [PropertyName("form_positions")]
        private Dictionary<string, string> _FormPositions { get; set; }
        */
        #endregion
    }
}

