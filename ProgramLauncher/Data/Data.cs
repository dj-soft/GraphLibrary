using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;
using DjSoft.Tools.ProgramLauncher.Components;
using DjSoft.Tools.ProgramLauncher.Data;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using System.Drawing.Text;

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
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Pages; } set { } }
        /// <summary>
        /// Počet evidovaných aplikací
        /// </summary>
        public int ApplicationsCount { get { return this.Groups.Sum(g => g.Applications.Count); } }
        public List<GroupData> Groups { get; private set; }
        public void CreateInteractiveItems(List<InteractiveItem> interactiveItems)
        {
            int y = 0;
            foreach (var group in this.Groups)
            {
                group.OffsetAdress = new Point(0, y);
                interactiveItems.Add(group.CreateInteractiveItem());
                y = group.Adress.Y + 1;

                int applMaxY = 0;
                foreach (var appl in group.Applications)
                {
                    appl.OffsetAdress = new Point(0, y);
                    interactiveItems.Add(appl.CreateInteractiveItem());
                    int applB = appl.Adress.Y + 1;
                    if (applMaxY < applB) applMaxY = applB;
                }
            }
        }
        #region Kontextové menu pro správu stránek = přidání / editace / odebrání celé stránky
        /// <summary>
        /// Nabídne kontextové menu pro správu stránek (přidání / editace / odebrání celé stránky)
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="dataInfo"></param>
        /// <param name="pages"></param>
        internal static void RunPageContextMenu(MouseState mouseState, BaseData dataInfo, List<PageData> pages)
        {
            var menuItems = new List<IMenuItem>();

            var pageData = dataInfo as PageData;
            bool hasPage = (pageData  != null);
            if (hasPage)
            {
                menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.Format(App.Messages.AppContextMenuTitlePage, pageData.Title) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuEditText, ToolTip = App.Messages.AppContextMenuEditApplicationToolTip, Image = Properties.Resources.edit_4_22, UserData = new ContextMenuUserData(ContextMenuActionType.EditPage, dataInfo, null, pages) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRemoveText, ToolTip = App.Messages.AppContextMenuRemoveApplicationToolTip, Image = Properties.Resources.archive_remove_22, UserData = new ContextMenuUserData(ContextMenuActionType.DeletePage, dataInfo, null, pages) });
            }
            if (!hasPage)
            {
                menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.AppContextMenuTitlePages });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuNewPageText, ToolTip = App.Messages.AppContextMenuNewPageToolTip, Image = Properties.Resources.document_new_3_22, UserData = new ContextMenuUserData(ContextMenuActionType.NewGroup, null, null, pages) });
            }

            App.SelectFromMenu(menuItems, _RunContextMenuAction, mouseState.LocationAbsolute);
        }
        #endregion
        #region Kontextové menu uvnitř stránky = správa skupin a aplikací
        /// <summary>
        /// Nabídne kontextové menu pro danou aplikaci / grupu
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="dataInfo"></param>
        /// <param name="pageData"></param>
        internal void RunApplicationContextMenu(MouseState mouseState, BaseData dataInfo, PageData pageData)
        {
            var menuItems = new List<IMenuItem>();

            var applicationData = dataInfo as ApplicationData;
            bool hasApplication = (applicationData != null);
            if (hasApplication)
            {
                // menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Separator });
                menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.Format(App.Messages.AppContextMenuTitleApplication, applicationData.Title) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRunText, ToolTip = App.Messages.AppContextMenuRunToolTip, Image = Properties.Resources.media_playback_start_3_22, UserData = new ContextMenuUserData(ContextMenuActionType.RunApplication, dataInfo, pageData, null) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRunAsText, ToolTip = App.Messages.AppContextMenuRunAsToolTip, Image = Properties.Resources.media_seek_forward_3_22, UserData = new ContextMenuUserData(ContextMenuActionType.RunApplicationAsAdmin, dataInfo, pageData, null) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuEditText, ToolTip = App.Messages.AppContextMenuEditApplicationToolTip, Image = Properties.Resources.edit_4_22, UserData = new ContextMenuUserData(ContextMenuActionType.EditApplication, dataInfo, pageData, null) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRemoveText, ToolTip = App.Messages.AppContextMenuRemoveApplicationToolTip, Image = Properties.Resources.archive_remove_22, UserData = new ContextMenuUserData(ContextMenuActionType.DeleteApplication, dataInfo, pageData, null) });
            }

            var groupData = dataInfo as GroupData;
            bool hasGroup = (groupData != null);
            if (hasGroup)
            {
                menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.Format(App.Messages.AppContextMenuTitleGroup, groupData.Title) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuEditText, ToolTip = App.Messages.AppContextMenuEditApplicationToolTip, Image = Properties.Resources.edit_4_22, UserData = new ContextMenuUserData(ContextMenuActionType.EditGroup, dataInfo, pageData, null) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRemoveText, ToolTip = App.Messages.AppContextMenuRemoveApplicationToolTip, Image = Properties.Resources.archive_remove_22, UserData = new ContextMenuUserData(ContextMenuActionType.DeleteGroup, dataInfo, pageData, null) });
            }
            if (!hasApplication && !hasGroup)
            {
                menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.AppContextMenuTitleApplications });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuNewApplicationText, ToolTip = App.Messages.AppContextMenuNewApplicationToolTip, Image = Properties.Resources.archive_insert_3_22, UserData = new ContextMenuUserData(ContextMenuActionType.NewApplication, null, pageData, null) });
                menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuNewGroupText, ToolTip = App.Messages.AppContextMenuNewGroupToolTip, Image = Properties.Resources.insert_horizontal_rule_22, UserData = new ContextMenuUserData(ContextMenuActionType.NewGroup, null, pageData, null) });
            }

            App.SelectFromMenu(menuItems, _RunContextMenuAction, mouseState.LocationAbsolute);
        }
        /// <summary>
        /// Provede vybranou akci z kontextového menu
        /// </summary>
        /// <param name="menuItem"></param>
        private static void _RunContextMenuAction(IMenuItem menuItem)
        {
            if (menuItem.UserData is ContextMenuUserData contextData)
            {
                switch (contextData.Action)
                {
                    case ContextMenuActionType.RunApplication:
                        (contextData.Data as ApplicationData).RunNewProcess(false);
                        break;
                    case ContextMenuActionType.RunApplicationAsAdmin:
                        (contextData.Data as ApplicationData).RunNewProcess(true);
                        break;
                    case ContextMenuActionType.EditApplication:
                        using (var form = new DialogForm())
                        {
                            form.ShowDialog();
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// Balíček s daty pro akce kontextového menu
        /// </summary>
        private class ContextMenuUserData
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="action"></param>
            /// <param name="data"></param>
            public ContextMenuUserData(ContextMenuActionType action, BaseData data, PageData pageData, List<PageData> pages)
            {
                Action = action;
                Data = data;
                PageData = pageData;
                Pages = pages;
            }
            /// <summary>
            /// Druh akce
            /// </summary>
            public ContextMenuActionType Action { get; private set; }
            /// <summary>
            /// Prvek
            /// </summary>
            public BaseData Data { get; private set; }
            /// <summary>
            /// Stránka na které se akce provádí
            /// </summary>
            public PageData PageData { get; private set; }
            /// <summary>
            /// Kompletní soupis stránek
            /// </summary>
            public List<PageData> Pages { get; private set; }
        }
        /// <summary>
        /// Akce v kontextovém menu
        /// </summary>
        private enum ContextMenuActionType
        {
            None,
            NewPage,
            EditPage,
            DeletePage,
            NewGroup,
            EditGroup,
            DeleteGroup,
            NewApplication,
            EditApplication,
            DeleteApplication,
            RunApplication,
            RunApplicationAsAdmin
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
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Groups; } set { } }
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
        #region Data o aplikaci
        [PropertyName("File")]
        public string ExecutableFileName { get; set; }
        [PropertyName("WorkingDirectory")]
        public string ExecutableWorkingDirectory { get; set; }
        [PropertyName("Arguments")]
        public string ExecutableArguments { get; set; }
        [PropertyName("AdminMode")]
        public bool ExecuteInAdminMode { get; set; }
        [PropertyName("Maximized")]
        public bool OpenMaximized { get; set; }
        [PropertyName("OneInstance")]
        public bool OnlyOneInstance { get; set; }
        [PersistingEnabled(false)]
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Applications; } set { } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Title: {Title}; Executable: {ExecutableFileName}";
        }
        #endregion
        #region Spouštění aplikace
        /// <summary>
        /// Spustí / aktivuje daný proces
        /// </summary>
        public void RunApplication()
        {
            try
            {
                App.MainForm.StatusLabelApplicationRunText = this.Title;
                App.MainForm.StatusLabelApplicationRunImage = ImageKindType.MediaForward;
                if (!_TryActivateProcess())
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
        /// <summary>
        /// Pokusí se najít a aktivovat zdejší proces, pokud this má nastaveno <see cref="OnlyOneInstance"/> = spustit jen jednu instanci.
        /// Primárně se proces hledá v <see cref="_LastProcess"/>, anebo podle <see cref="__LastProcessId"/>.
        /// Tedy pokud byl spuštěn this instancí programu, pak ji najde pokud běží.
        /// <para/>
        /// Pokud ale cílová aplikace byla spuštěna někým jiným, pak ji zkusí najít podle shody jména aplikace a argumentů.
        /// </summary>
        /// <returns></returns>
        private bool _TryActivateProcess()
        {
            bool result = false;
            if (!OnlyOneInstance) return false;

            Process myProcess = null;
            try
            {
                bool hasLastProcess = false;
                if (this._LastProcess != null)             // Tady už je zajištěno, že můj proces žije
                {
                    myProcess = this._LastProcess;
                    hasLastProcess = true;
                }

                if (myProcess is null)
                {
                    myProcess = App.SearchForProcess(_CurrentFileName, _CurrentArguments, __LastProcessId);
                }

                if (myProcess != null && !myProcess.HasExited && myProcess.MainWindowHandle != IntPtr.Zero)
                {
                    if (!hasLastProcess)
                        _StoreRunningProcess(myProcess);
                    App.ActivateWindowsProcess(myProcess);
                    result = true;
                }
            }
            catch (Exception exc)
            { }
            return result;
        }
        /// <summary>
        /// SPustí nový proces pro zdejší aplikaci
        /// </summary>
        /// <param name="executeInAdminMode"></param>
        private void _RunNewProcess(bool? executeInAdminMode)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = _CurrentFileName,
                WorkingDirectory = _CurrentWorkingDirectory,
                Arguments = _CurrentArguments,
                WindowStyle = (OpenMaximized ? System.Diagnostics.ProcessWindowStyle.Maximized : System.Diagnostics.ProcessWindowStyle.Normal),
                UseShellExecute = true
            };

            bool adminMode = executeInAdminMode ?? ExecuteInAdminMode;
            if (adminMode) psi.Verb = "runas";

            Process process = new System.Diagnostics.Process();
            process.StartInfo = psi;
            process.Start();
            _StoreRunningProcess(process);
        }
        /// <summary>
        /// Uloží do this instance informace o daném procesu, který odpovídá zdejší definici.
        /// </summary>
        /// <param name="process"></param>
        private void _StoreRunningProcess(Process process)
        {
            __LastProcessId = process.Id;
            __LastProcess = process;
        }
        /// <summary>
        /// Uloží do this instance informace o daném procesu, který odpovídá zdejší definici.
        /// </summary>
        /// <param name="process"></param>
        private void _ResetRunningProcess(Process process)
        {
            __LastProcessId = null;
            __LastProcess = null;
        }
        /// <summary>
        /// Jméno spouštěného souboru, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentFileName { get { return _GetCurrentName(ExecutableFileName); } }
        /// <summary>
        /// Jméno provozního adresáře, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentWorkingDirectory { get { return _GetCurrentName(ExecutableWorkingDirectory); } }
        /// <summary>
        /// Text argumentů, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentArguments { get { return _GetCurrentName(ExecutableArguments); } }
        /// <summary>
        /// Jméno ikony, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentImageFileName { get { return _GetCurrentName(ImageFileName); } }
        /// <summary>
        /// Vrátí zadaný string, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string _GetCurrentName(string file)
        {
            if (String.IsNullOrEmpty(file)) return "";
            if (!file.Contains("%")) return file;

            string full = System.Environment.ExpandEnvironmentVariables(file);
            return full;
        }
        /// <summary>
        /// Poslední známé ID procesu
        /// </summary>
        private int? __LastProcessId;
        /// <summary>
        /// Uložená data o procesu, když jsme jej sami spustili anebo detekovali spuštěný
        /// </summary>
        [PersistingEnabled(false)]
        private Process _LastProcess
        {
            get 
            {
                if (__LastProcess != null)
                {
                    try
                    {
                        if (__LastProcess.HasExited)
                            __LastProcess = null;
                        else if (__LastProcess.MainWindowHandle == IntPtr.Zero)
                            __LastProcess = null;
                    }
                    catch
                    {
                        __LastProcess = null;
                    }
                }
                return __LastProcess;
            }
            set { __LastProcess = value; }
        }
        /// <summary>
        /// Uložená data o procesu, když jsme jej sami spustili anebo detekovali spuštěný
        /// </summary>
        private Process __LastProcess;
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
                RelativeAdress = new Point(0, 0),
                ExecutableFileName = @"c:\Windows\explorer.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Wordpad",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\abiword.png",
                RelativeAdress = new Point(1, 0),
                ExecutableFileName = @"c:\Windows\write.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "MS DOS user",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\evilvte.png",
                ExecuteInAdminMode = false,
                OpenMaximized = true,
                RelativeAdress = new Point(0, 1),
                ExecutableFileName = @"c:\Windows\System32\cmd.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "MS DOS Admin",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\evilvte.png",
                ExecuteInAdminMode = true,
                RelativeAdress = new Point(0, 2),
                ExecutableFileName = @"c:\Windows\System32\cmd.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Libre Office",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\distributions-solaris.png",
                RelativeAdress = new Point(1, 1),
                OnlyOneInstance = true,
                ExecutableFileName = @"c:\Program Files (x86)\OpenOffice 4\program\soffice.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Dokument",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\gpe-tetris.png",
                RelativeAdress = new Point(2, 0),
                ExecutableFileName = @"d:\Dokumenty\Vyděšený svišť.png"
            });

            list.Add(new ApplicationData()
            {
                Title = "Poznámkový blok",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\abiword.png",
                OnlyOneInstance = true,
                RelativeAdress = new Point(2, 2),
                ExecutableFileName = @"%windir%\system32\notepad.exe"
            });

            list.Add(new ApplicationData()
            {
                Title = "Remote Notebook",
                ExecutableFileName = @"%windir%\system32\mstsc.exe",
                ExecutableArguments = @"D:\Windows\Složka\Asseco\David NB.rdp",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\firefox_alt.png",
                OnlyOneInstance = true,
                RelativeAdress = new Point(0, 3),
            });

            list.Add(new ApplicationData()
            {
                Title = "Remote Desktop",
                ExecutableFileName = @"%windir%\system32\mstsc.exe",
                ExecutableArguments = @"D:\Windows\Složka\Asseco\David ASOL.rdp",
                ImageFileName = @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\firefox_alt.png",
                OnlyOneInstance = true,
                RelativeAdress = new Point(1, 3),
            });




            // %windir%\system32\notepad.exe
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
        /// Druh layoutu, podle něhož se tento prvek kreslí.
        /// Konkrétní layout je získán z <see cref="InteractiveGraphicsControl.GetLayout(DataLayoutKind)"/>, kam se předává zdejší <see cref="LayoutKind"/>.
        /// Pokud je null, přebírá se z hostitelského panelu <see cref="InteractiveGraphicsControl.DefaultLayoutKind"/>.
        /// </summary>
        public virtual DataLayoutKind? LayoutKind { get; set; }
        /// <summary>
        /// Adresa prvku v controlu (Page a Group) nebo v relativně grupě (Aplikace).
        /// Posun relativní adresy je v <see cref="OffsetAdress"/>.
        /// </summary>
        public virtual Point RelativeAdress { get; set; }
        /// <summary>
        /// Posun adresy prvku <see cref="RelativeAdress"/> vůči počátku prostoru. 
        /// Typicky se plní jen do aplikace a do grupy, řeší postupný posun souřadnice Y.
        /// </summary>
        public virtual Point? OffsetAdress { get; set; }
        /// <summary>
        /// Absolutní pozice prvku, určená z <see cref="RelativeAdress"/> + <see cref="OffsetAdress"/>.
        /// </summary>
        public virtual Point Adress { get { return RelativeAdress.GetShiftedPoint(OffsetAdress); } set { RelativeAdress = value.GetShiftedPoint(OffsetAdress, true); } }
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
    #endregion
    #region class InteractiveDataItem : Potomek vizuálního (= interaktivního) prvku, vytvořený nad daty "BaseData"
    /// <summary>
    /// Potomek vizuálního (= interaktivního) prvku, vytvořený nad daty <see cref="BaseData"/>.
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
        private ColorSet __CellBackColor;
        public override string MainTitle { get { return __Data.Title; } set { __Data.Title = value; } }
        public override string Description { get { return __Data.Description; } set { __Data.Description = value; } }
        public override Point Adress { get { return __Data.Adress; } set { __Data.Adress = value; } }
        public override string ImageName { get { return __Data.ImageFileName; } set { __Data.ImageFileName = value; } }
        public override ColorSet CellBackColor { get { return __CellBackColor; } set { __CellBackColor = value; } }
        public override DataLayoutKind? LayoutKind { get { return __Data.LayoutKind; } set { __Data.LayoutKind = value; } }
    }
    #endregion
    #region class WinApi : metody WinApi pro práci s okny cizích procesů
    /// <summary>
    /// WinApi : metody WinApi pro práci s okny cizích procesů
    /// </summary>
    public class WinApi
    {
        /// <summary>
        /// Metoda přesune do popředí dané okno = typicky hlavní okno aplikace.
        /// Parametrem <paramref name="restoreFromMinimized"/> lze vyžádat akci, kdy Minimalizované okno je změněno na Normalizované.
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <param name="restoreFromMinimized"></param>
        public static void SetWindowToForeground(IntPtr windowHandle, bool restoreFromMinimized = false)
        {
            SetForegroundWindow(windowHandle);
            if (restoreFromMinimized)
            {
                var windowState = _GetWindowPlacement(windowHandle);
                if (windowState.showCmd == ShowWindowCommands.Minimized)
                {
                    windowState.showCmd = ShowWindowCommands.Normal;
                    SetWindowState(windowHandle, windowState);
                }
            }

        }

        public static void SetWindowState(IntPtr windowHandle, ShowWindowCommands state, Rectangle normalPosition)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            placement.flags = 0;
            placement.showCmd = state;
            placement.rcNormalPosition = normalPosition;
            SetWindowPlacement(windowHandle, ref placement);
        }
        public static void SetWindowState(IntPtr windowHandle, WINDOWPLACEMENT placement)
        {
            SetWindowPlacement(windowHandle, ref placement);
        }

        public static void SetPlacement(IntPtr windowHandle, WINDOWPLACEMENT placement)
        {
            if (placement.length == 0)
                return;

            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            placement.flags = 0;
            SetWindowPlacement(windowHandle, ref placement);
        }

        private static WINDOWPLACEMENT _GetWindowPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }
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

