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
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;
using System.Runtime.CompilerServices;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    #region class PageSetData : Kompletní set všech stránek
    /// <summary>
    /// PageSetData : Kompletní set všech stránek
    /// </summary>
    public class PageSetData : BaseData
    {
        #region Public data a základní tvorba
        /// <summary>
        /// Konstruktor
        /// </summary>
        public PageSetData()
        {
            __Pages = new ChildItems<PageSetData, PageData>(this);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <returns></returns>
        public PageSetData Clone()
        {
            PageSetData clone = new PageSetData();
            this.FillClone(clone);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone"></param>
        protected override void FillClone(BaseData clone)
        {
            base.FillClone(clone);
            if (clone is PageSetData pageSetData)
            {
                pageSetData.__Pages.Clear();
                pageSetData.__Pages.AddRange(this.Pages.Select(p => p.Clone()));
            }
        }
        /// <summary>
        /// Sada stránek v tomto setu
        /// </summary>
        [PersistingEnabled(false)]
        public ChildItems<PageSetData, PageData> Pages { get { CheckNotVoid(); return __Pages; } } private ChildItems<PageSetData, PageData> __Pages;
        /// <summary>
        /// Druh layoutu, z něhož se čerpá.
        /// </summary>
        [PersistingEnabled(false)]
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.None; } set { } }
        /// <summary>
        /// Počet stránek v tomto setu
        /// </summary>
        [PersistingEnabled(false)]
        public int PagesCount { get { return Pages.Count; } }
        /// <summary>
        /// Počet aplikací v tomto setu
        /// </summary>
        [PersistingEnabled(false)]
        public int ApplicationsCount { get { return Pages.Sum(p => p.ApplicationsCount); } }
        /// <summary>
        /// Vytvoří a vrátí pole svých Child prvků.
        /// </summary>
        /// <returns></returns>
        public override List<InteractiveItem> CreateInteractiveItems()
        {
            List<InteractiveItem> interactiveItems = new List<InteractiveItem>();
            foreach (var page in this.Pages)
                interactiveItems.Add(page.CreateInteractiveItem());
            return interactiveItems;
        }
        #endregion
        #region Podpora de/serializace
        /// <summary>
        /// Metoda je volána před zahájením procesu Serializace = data budou uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeStart()
        {
            // Před uložením dat do Configu naplním pole _Pages daty z ChildListu Pages,
            _Pages = Pages.ToArray();
            // ... a po dokončení serializace v metodě OnPersistSerializeDone() pole _Pages zahodíme!
        }
        /// <summary>
        /// Metoda je volána po ukončení procesu Serializace = data byla uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeDone()
        {
            _Pages = null;
        }
        /// <summary>
        /// Metoda je volána po ukončení procesu Deserializace = data byla načtena z nějakého Configu.
        /// Potomek může reagovat = provést nějaké dopočty a finalizaci...
        /// </summary>
        protected override void OnPersistDeserializeDone()
        {
            // Po ukončení deserializace jsou data načtena v poli _Pages: převezmeme je a dáme do ChildListu Pages a pole zrušíme:
            this.__Pages.Clear();
            if (this._Pages != null) this.__Pages.AddRange(this._Pages);
            this._Pages = null;
        }
        /// <summary>
        /// Slouží výhradně pro persistenci
        /// </summary>
        [PropertyName("Pages")]
        private PageData[] _Pages { get; set; }
        #endregion
        #region Kontextové menu - tvorba, aktivace, zpracování akce z položky
        /// <summary>
        /// Nabídne kontextové menu pro danou oblast a daný prvek v této oblasti.
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="areaData">Zde je předán buď celý <see cref="PageSetData"/> pro kontextové menu typu Stránky, anebo jedna konkrétní stránka <see cref="PageData"/> pokud je menu aktivní pro ni</param>
        /// <param name="itemData">Prvek, na který bylo klimnuto, nebo null = do prostoru mimo prvky</param>
        public void ShowContextMenu(MouseState mouseState, InteractiveGraphicsControl panel, BaseData areaData, BaseData itemData)
        {
            // Sestavím kontextové menu podle situace:
            var menuItems = new List<IMenuItem>();

            ContextActionInfo actionInfo = new ContextActionInfo(mouseState, panel, this, areaData, itemData);

            if (areaData is PageSetData pageSetData)
            {   // Kliknutí na seznamu stránek
                if (itemData is PageData pageItem)
                {   // Na konkrétní stránce:
                    menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.Format(App.Messages.AppContextMenuTitlePage, pageItem.Title) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuEditText, ToolTip = App.Messages.AppContextMenuEditApplicationToolTip, Image = Properties.Resources.edit_4_22, UserData = new ContextMenuItemInfo(DataItemActionType.EditPage, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuCopyText, ToolTip = App.Messages.AppContextMenuCopyPageToolTip, Image = Properties.Resources.edit_copy_4_22, UserData = new ContextMenuItemInfo(DataItemActionType.CopyPage, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRemoveText, ToolTip = App.Messages.AppContextMenuRemoveApplicationToolTip, Image = Properties.Resources.archive_remove_22, UserData = new ContextMenuItemInfo(DataItemActionType.DeletePage, actionInfo) });
                }
                else
                {   // Mimo konkrétní stránku:
                    menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.AppContextMenuTitlePages });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuNewPageText, ToolTip = App.Messages.AppContextMenuNewPageToolTip, Image = Properties.Resources.document_new_3_22, UserData = new ContextMenuItemInfo(DataItemActionType.NewPage, actionInfo) });
                }
            }

            else if (areaData is PageData pageArea)
            {   // Kliknutí na obsahu stránky (grupy, aplikace):
                if (itemData is GroupData groupData)
                {   // Na titulku grupy:
                    menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.Format(App.Messages.AppContextMenuTitleGroup, groupData.Title) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuEditText, ToolTip = App.Messages.AppContextMenuEditApplicationToolTip, Image = Properties.Resources.edit_4_22, UserData = new ContextMenuItemInfo(DataItemActionType.EditGroup, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuCopyText, ToolTip = App.Messages.AppContextMenuCopyGroupToolTip, Image = Properties.Resources.edit_copy_4_22, UserData = new ContextMenuItemInfo(DataItemActionType.CopyGroup, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRemoveText, ToolTip = App.Messages.AppContextMenuRemoveApplicationToolTip, Image = Properties.Resources.archive_remove_22, UserData = new ContextMenuItemInfo(DataItemActionType.DeleteGroup, actionInfo) });
                }
                else if (itemData is ApplicationData applicationData)
                {   // Na konkrétní aplikaci:
                    menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.Format(App.Messages.AppContextMenuTitleApplication, applicationData.Title) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRunText, ToolTip = App.Messages.AppContextMenuRunToolTip, Image = Properties.Resources.media_playback_start_3_22, UserData = new ContextMenuItemInfo(DataItemActionType.RunApplication, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRunAsText, ToolTip = App.Messages.AppContextMenuRunAsToolTip, Image = Properties.Resources.media_seek_forward_3_22, UserData = new ContextMenuItemInfo(DataItemActionType.RunApplicationAsAdmin, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuEditText, ToolTip = App.Messages.AppContextMenuEditApplicationToolTip, Image = Properties.Resources.edit_4_22, UserData = new ContextMenuItemInfo(DataItemActionType.EditApplication, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuCopyText, ToolTip = App.Messages.AppContextMenuCopyApplicationToolTip, Image = Properties.Resources.edit_copy_4_22, UserData = new ContextMenuItemInfo(DataItemActionType.CopyApplication, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuRemoveText, ToolTip = App.Messages.AppContextMenuRemoveApplicationToolTip, Image = Properties.Resources.archive_remove_22, UserData = new ContextMenuItemInfo(DataItemActionType.DeleteApplication, actionInfo) });
                }
                else
                {   // Ve volné ploše:
                    menuItems.Add(new DataMenuItem() { ItemType = MenuItemType.Header, Text = App.Messages.AppContextMenuTitleApplications });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuNewApplicationText, ToolTip = App.Messages.AppContextMenuNewApplicationToolTip, Image = Properties.Resources.archive_insert_3_22, UserData = new ContextMenuItemInfo(DataItemActionType.NewApplication, actionInfo) });
                    menuItems.Add(new DataMenuItem() { Text = App.Messages.AppContextMenuNewGroupText, ToolTip = App.Messages.AppContextMenuNewGroupToolTip, Image = Properties.Resources.insert_horizontal_rule_22, UserData = new ContextMenuItemInfo(DataItemActionType.NewGroup, actionInfo) });
                }
            }

            App.SelectFromMenu(menuItems, _RunContextMenuAction, mouseState.LocationAbsolute);
        }
        /// <summary>
        /// Provede vybranou akci z kontextového menu
        /// </summary>
        /// <param name="menuItem"></param>
        private static void _RunContextMenuAction(IMenuItem menuItem)
        {
            if (menuItem.UserData is ContextMenuItemInfo itemInfo)
                RunContextMenuAction(itemInfo.ActionType, itemInfo.ActionInfo);
        }
        /// <summary>
        /// Balíček s daty pro akce konkrétní položky kontextového menu
        /// </summary>
        private class ContextMenuItemInfo
        {
            public ContextMenuItemInfo(DataItemActionType actionType, ContextActionInfo actionInfo)
            {
                this.ActionType = actionType;
                this.ActionInfo = actionInfo;
            }
            /// <summary>
            /// Druh akce
            /// </summary>
            public DataItemActionType ActionType { get; private set; }
            /// <summary>
            /// Kompletní data o prvku a controlu
            /// </summary>
            public ContextActionInfo ActionInfo { get; private set; }
        }
        /// <summary>
        /// Provede vybranou akci z kontextového menu
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RunContextMenuAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            switch (actionType)
            {
                case DataItemActionType.MovePage:
                    break;
                case DataItemActionType.NewPage:
                case DataItemActionType.EditPage:
                case DataItemActionType.CopyPage:
                case DataItemActionType.DeletePage:
                    PageSetData.RunEditAction(actionType, actionInfo);
                    break;
                case DataItemActionType.MoveGroup:
                    break;
                case DataItemActionType.NewGroup:
                case DataItemActionType.EditGroup:
                case DataItemActionType.CopyGroup:
                case DataItemActionType.DeleteGroup:
                    PageData.RunEditAction(actionType, actionInfo);
                    break;
                case DataItemActionType.MoveApplication:
                    break;
                case DataItemActionType.NewApplication:
                case DataItemActionType.EditApplication:
                case DataItemActionType.CopyApplication:
                case DataItemActionType.DeleteApplication:
                    GroupData.RunEditAction(actionType, actionInfo);
                    break;
                case DataItemActionType.RunApplication:
                case DataItemActionType.RunApplicationAsAdmin:
                    ApplicationData.RunEditAction(actionType, actionInfo);
                    break;
            }
        }
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje stránky
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var pageData = (actionInfo.ItemData as PageData);
            bool hasPageData = pageData != null;
            bool isAppend = false;
            bool isUpdated = false;
            switch (actionType)
            {
                case DataItemActionType.NewPage:
                    pageData = new PageData();
                    isAppend = pageData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.EditFormTitleNewPage);
                    break;
                case DataItemActionType.EditPage:
                    if (hasPageData)
                        isUpdated = pageData.EditData(actionInfo.MouseState.LocationAbsolute);
                    break;
                case DataItemActionType.CopyPage:
                    pageData = pageData.Clone();
                    isAppend = pageData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleClone, pageData.Title));
                    break;
                case DataItemActionType.DeletePage:

                    break;
            }

            var pageSetData = actionInfo.PageSetData;
            if (isAppend)
            {
                int y = (pageSetData.Pages.Count == 0 ? 0 : pageSetData.Pages.Max(p => p.Adress.Y) + 1);
                pageData.Adress = new Point(0, y);
                pageSetData.Pages.Add(pageData);
                isUpdated = true;
            }
            if (isUpdated)
            {
                App.Settings.SetChanged();
            }
        }
        #endregion
        #region Tvorba výchozích dat - namísto prázdných
        /// <summary>
        /// Zajistí, že v daném seznamu bude alespoň jeden prvek, a že první prvek nebude prázdný.
        /// </summary>
        /// <param name="programGroups"></param>
        public void CheckNotVoid()
        {
            if (__Pages is null) __Pages = new ChildItems<PageSetData, PageData>(this);
            if (__Pages.Count > 0 ) return;

            __Pages.AddRange(CreateInitialData().Pages);
        }
        /// <summary>
        /// Vytvoří a vrátí defaultní prvek
        /// </summary>
        /// <returns></returns>
        public static PageSetData CreateInitialData()
        {
            PageSetData pageSet = new PageSetData();

            var page0 = new PageData() { Title = "Hobby", RelativeAdress = new Point(0, 0), ImageFileName = getImage("button-green") };
            pageSet.__Pages.Add(page0);                             // Musím používat field __Pages, protože použitím property Pages bych se zacyklil...

            var group00 = new GroupData() { Title = "Aplikace Windows", RelativeAdress = new Point(0, 0), ImageFileName = getImage("rectagle_purple") };
            page0.Groups.Add(group00);
            group00.Applications.Add(getApp("Windows", "Průzkumník", "wine", @"c:\Windows\explorer.exe", "", 0, 0));
            group00.Applications.Add(getApp("Wordpad", "Jednoduchý text", "abiword", @"c:\Windows\write.exe", "", 1, 0));
            group00.Applications.Add(getApp("MS DOS user", "Příkazový řádek", "evilvte", @"c:\Windows\System32\cmd.exe", "", 0, 1));
            group00.Applications.Add(getApp("MS DOS admin", "Příkazový řádek v režimu Admin", "evilvte", @"c:\Windows\System32\cmd.exe", "", 0, 2, executeInAdminMode: true));
            group00.Applications.Add(getApp("Libre Office", "Lepší text", "distributions-solaris", @"c:\Program Files (x86)\OpenOffice 4\program\soffice.exe", "", 1, 1));
            group00.Applications.Add(getApp("Obrázek Svišť", "Vyděšený svišť", "gpe-tetris", @"d:\Dokumenty\Vyděšený svišť.png", "", 2, 0));
            group00.Applications.Add(getApp("Poznámkový blok", "Základní text", "abiword", @"%windir%\system32\notepad.exe", "", 2, 2, onlyOneInstance: true));
            group00.Applications.Add(getApp("RDP NTB", "Vzdálená plocha na Notebook", "firefox_alt", @"%windir%\system32\mstsc.exe", @"D:\Windows\Složka\Asseco\David NB.rdp", 0, 3));
            group00.Applications.Add(getApp("RDP PC", "Vzdálená plocha na Desktop", "firefox_alt", @"%windir%\system32\mstsc.exe", @"D:\Windows\Složka\Asseco\David ASOL.rdp", 1, 3));

            var group01 = new GroupData() { Title = "Aplikace David", RelativeAdress = new Point(0, 0), ImageFileName = getImage("rectagle_blue") };
            page0.Groups.Add(group01);
            group01.Applications.Add(getApp("SD Cards", "Tester SD cards", "wine", @"C:\DavidPrac\VsProjects\SDCardTester\SDCardTester\bin\Debug\Djs.SDCardTester.exe", "", 0, 0));
            group01.Applications.Add(getApp("Stopky", "Hodinky", "wine", @"C:\CSharp\Stopky\Stopky\bin\Debug\Stopky.exe", "", 1, 0));
            group01.Applications.Add(getApp("Vypínač", "Vypne PC", "wine", @"C:\DavidPrac\VsProjects\WinShutDown\Exe\Djs.WinShutDown.exe", "", 2, 0));

            return pageSet;

            ApplicationData getApp(string title, string description, string imageSampleName, string exeFile, string arguments, int x, int y, bool openMaximized = false, bool onlyOneInstance = false, bool executeInAdminMode = false)
            {
                var app = new ApplicationData()
                {
                    Title = title,
                    Description = description,
                    ImageFileName = getImage(imageSampleName),
                    ExecutableFileName = exeFile,
                    ExecutableArguments = arguments,
                    RelativeAdress = new Point(x, y),
                    OpenMaximized = openMaximized,
                    OnlyOneInstance = onlyOneInstance,
                    ExecuteInAdminMode = executeInAdminMode
                };
                return app;
            }

            string getImage(string name)
            {
                return @"C:\DavidPrac\VsProjects\ProgramLauncher\ProgramLauncher\Pics\samples\" + name + ".png";
            }
        }
        #endregion
    }
    #endregion
    #region class PageData : Jedna stránka s daty, je zobrazena Tabem v levém bloku, popisuje celý obsah v pravé velké části
    /// <summary>
    /// Jedna stránka s daty, je zobrazena Tabem v levém bloku, popisuje celý obsah v pravé velké části
    /// </summary>
    public class PageData : BaseData, IChildOfParent<PageSetData>
    {
        #region Public data a základní tvorba
        /// <summary>
        /// Konstruktor
        /// </summary>
        public PageData()
        {
            __Groups = new ChildItems<PageData, GroupData>(this);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <returns></returns>
        public PageData Clone()
        {
            PageData clone = new PageData();
            this.FillClone(clone);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone"></param>
        protected override void FillClone(BaseData clone)
        {
            base.FillClone(clone);
            if (clone is PageData pageData)
            {
                pageData.__Groups.Clear();
                pageData.__Groups.AddRange(this.Groups.Select(g => g.Clone()));
            }
        }
        /// <summary>
        /// Sada skupin v této stránce
        /// </summary>
        [PersistingEnabled(false)]
        public ChildItems<PageData, GroupData> Groups { get { CheckNotVoid(); return __Groups; } } private ChildItems<PageData, GroupData> __Groups;
        /// <summary>
        /// Kontrola platnosti dat v seznamu <see cref="__Groups"/>
        /// </summary>
        protected void CheckNotVoid()
        {
            if (__Groups is null) __Groups = new ChildItems<PageData, GroupData>(this);
        }
        /// <summary>
        /// Druh layoutu, z něhož se čerpá.
        /// </summary>
        [PersistingEnabled(false)]
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Pages; } set { } }
        /// <summary>
        /// Můj parent
        /// </summary>
        PageSetData IChildOfParent<PageSetData>.Parent { get { return __Parent; } set { __Parent = value; } } private PageSetData __Parent;
        /// <summary>
        /// Počet skupin v této stránce
        /// </summary>
        [PersistingEnabled(false)]
        public int GroupsCount { get { return Groups.Count; } }
        /// <summary>
        /// Počet aplikací v této stránce
        /// </summary>
        [PersistingEnabled(false)]
        public int ApplicationsCount { get { return Groups.Sum(g => g.ApplicationsCount); } }
        /// <summary>
        /// Vytvoří a vrátí pole svých Child prvků.
        /// </summary>
        /// <returns></returns>
        public override List<InteractiveItem> CreateInteractiveItems()
        {
            List<InteractiveItem> interactiveItems = new List<InteractiveItem>();
            int y = 0;
            foreach (var group in this.Groups)
            {
                group.OffsetAdress = new Point(0, y);
                interactiveItems.Add(group.CreateInteractiveItem());
                y = group.Adress.Y + 1;

                int maxBottom = 0;
                foreach (var appl in group.Applications)
                {
                    appl.OffsetAdress = new Point(0, y);
                    interactiveItems.Add(appl.CreateInteractiveItem());
                    int appBottom = appl.Adress.Y + 1;
                    if (maxBottom < appBottom) maxBottom = appBottom;
                }
                y = maxBottom;
            }
            return interactiveItems;
        }
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje grupy
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var pageData = actionInfo.AreaData as PageData;
            bool hasPageData = pageData != null;
            if (!hasPageData) return;

            PageSetData pageSetClone = actionInfo.PageSetData.Clone();

            var groupData = (actionInfo.ItemData as GroupData);
            bool hasGroupData = groupData != null;
            bool isAppend = false;
            bool isUpdated = false;
            switch (actionType)
            {
                case DataItemActionType.NewGroup:
                    groupData = new GroupData();
                    isAppend = groupData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.EditFormTitleNewGroup);
                    break;
                case DataItemActionType.EditGroup:
                    if (hasGroupData)
                        isUpdated = groupData.EditData(actionInfo.MouseState.LocationAbsolute);
                    break;
                case DataItemActionType.CopyGroup:
                    groupData = groupData.Clone();
                    isAppend = groupData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleClone, groupData.Title));
                    break;
                case DataItemActionType.DeleteGroup:
                    isUpdated = pageData.Groups.Remove(groupData);
                    break;
            }

            if (isAppend)
            {
                int y = 0;
                groupData.Adress = new Point(0, y);
                pageData.Groups.Add(groupData);
                isUpdated = true;
            }
            if (isUpdated)
            {
                App.Settings.SetChanged();
                App.UndoRedo.Add(pageSetClone);
            }
        }
        #endregion
        #region Podpora de/serializace
        /// <summary>
        /// Metoda je volána před zahájením procesu Serializace = data budou uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeStart()
        {
            // Před uložením dat do Configu naplním pole _Groups daty z ChildListu Groups,
            _Groups = Groups.ToArray();
            // ... a po dokončení serializace v metodě OnPersistSerializeDone() pole _Groups zahodíme!
        }
        /// <summary>
        /// Metoda je volána po ukončení procesu Serializace = data byla uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeDone()
        {
            _Groups = null;
        }
        /// <summary>
        /// Metoda je volána po ukončení procesu Deserializace = data byla načtena z nějakého Configu.
        /// Potomek může reagovat = provést nějaké dopočty a finalizaci...
        /// </summary>
        protected override void OnPersistDeserializeDone()
        {
            // Po ukončení deserializace jsou data načtena v poli _Pages: převezmeme je a dáme do ChildListu Pages a pole zrušíme:
            this.__Groups.Clear();
            if (this._Groups != null) this.__Groups.AddRange(this._Groups);
            this._Groups = null;
        }
        /// <summary>
        /// Slouží výhradně pro persistenci
        /// </summary>
        [PropertyName("Groups")]
        private GroupData[] _Groups { get; set; }
        #endregion
    }
    #endregion
    #region class GroupData : Jedna skupina dat, je zobrazena v pravé velké části, a je reprezentována vodorovným titulkem přes celou šířku, obsahuje sadu aplikací
    /// <summary>
    /// Jedna skupina dat, je zobrazena v pravé velké části, a je reprezentována vodorovným titulkem přes celou šířku, obsahuje sadu aplikací
    /// </summary>
    public class GroupData : BaseData, IChildOfParent<PageData>
    {
        #region Public data a základní tvorba
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GroupData()
        {
            __Applications = new ChildItems<GroupData, ApplicationData>(this);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <returns></returns>
        public GroupData Clone()
        {
            GroupData clone = new GroupData();
            this.FillClone(clone);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone"></param>
        protected override void FillClone(BaseData clone)
        {
            base.FillClone(clone);
            if (clone is GroupData groupData)
            {
                groupData.__Applications.Clear();
                groupData.__Applications.AddRange(this.Applications.Select(a => a.Clone()));
            }
        }
        /// <summary>
        /// Seznam aplikací v této grupě
        /// </summary>
        [PersistingEnabled(false)]
        public ChildItems<GroupData, ApplicationData> Applications { get { CheckNotVoid(); return __Applications; } } private ChildItems<GroupData, ApplicationData> __Applications;
        /// <summary>
        /// Kontrola platnosti dat v seznamu <see cref="__Groups"/>
        /// </summary>
        protected void CheckNotVoid()
        {
            if (__Applications is null) __Applications = new ChildItems<GroupData, ApplicationData>(this);
        }
        /// <summary>
        /// Druh layoutu, z něhož se čerpá.
        /// </summary>
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Groups; } set { } }
        /// <summary>
        /// Počet aplikací v této grupě
        /// </summary>
        [PersistingEnabled(false)]
        public int ApplicationsCount { get { return Applications.Count; } }
        /// <summary>
        /// Můj parent
        /// </summary>
        PageData IChildOfParent<PageData>.Parent { get { return __Parent; } set { __Parent = value; } } private PageData __Parent;
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje stránky
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var applicationData = (actionInfo.ItemData as ApplicationData);
            bool hasApplicationData = applicationData != null;
            bool isUpdated = false;
            switch (actionType)
            {
                case DataItemActionType.NewApplication:
                    applicationData = new ApplicationData();
                    isUpdated = applicationData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.EditFormTitleNewApplication);
                    break;
                case DataItemActionType.EditApplication:
                    if (hasApplicationData)
                        isUpdated = applicationData.EditData(actionInfo.MouseState.LocationAbsolute);
                    break;
                case DataItemActionType.CopyApplication:
                    applicationData = applicationData.Clone();
                    isUpdated = applicationData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleClone, applicationData.Title));
                    break;
                case DataItemActionType.DeleteApplication:
                    break;
            }
        }
        #endregion
        #region Podpora de/serializace
        /// <summary>
        /// Metoda je volána před zahájením procesu Serializace = data budou uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeStart()
        {
            // Před uložením dat do Configu naplním pole _Groups daty z ChildListu Groups,
            _Applications = Applications.ToArray();
            // ... a po dokončení serializace v metodě OnPersistSerializeDone() pole _Groups zahodíme!
        }
        /// <summary>
        /// Metoda je volána po ukončení procesu Serializace = data byla uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeDone()
        {
            _Applications = null;
        }
        /// <summary>
        /// Metoda je volána po ukončení procesu Deserializace = data byla načtena z nějakého Configu.
        /// Potomek může reagovat = provést nějaké dopočty a finalizaci...
        /// </summary>
        protected override void OnPersistDeserializeDone()
        {
            // Po ukončení deserializace jsou data načtena v poli _Pages: převezmeme je a dáme do ChildListu Pages a pole zrušíme:
            this.__Applications.Clear();
            if (this._Applications != null) this.__Applications.AddRange(this._Applications);
            this._Applications = null;
        }
        /// <summary>
        /// Slouží výhradně pro persistenci
        /// </summary>
        [PropertyName("Applications")]
        private ApplicationData[] _Applications { get; set; }
        #endregion
    }
    #endregion
    #region class ApplicationData : Jeden prvek nabídky, konkrétní aplikace = spustitelný cíl
    /// <summary>
    /// Jeden prvek nabídky, konkrétní aplikace = spustitelný cíl
    /// </summary>
    public class ApplicationData : BaseData, IChildOfParent<GroupData>
    {
        #region Public data a základní tvorba
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ApplicationData()
        { }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <returns></returns>
        public ApplicationData Clone()
        {
            ApplicationData clone = new ApplicationData();
            this.FillClone(clone);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone"></param>
        protected override void FillClone(BaseData clone)
        {
            base.FillClone(clone);
            if (clone is  ApplicationData applicationData)
            {
                applicationData.ExecutableFileName = this.ExecutableFileName;
                applicationData.ExecutableWorkingDirectory = this.ExecutableWorkingDirectory;
                applicationData.ExecutableArguments = this.ExecutableArguments;
                applicationData.ExecuteInAdminMode = this.ExecuteInAdminMode;
                applicationData.OpenMaximized = this.OpenMaximized;
                applicationData.OnlyOneInstance = this.OnlyOneInstance;
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().Name}: '{Title}'; Executable: '{ExecutableFileName}'";
        }
        /// <summary>
        /// Druh layoutu, z něhož se čerpá.
        /// </summary>
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Applications; } set { } }
        /// <summary>
        /// Můj parent
        /// </summary>
        GroupData IChildOfParent<GroupData>.Parent { get { return __Parent; } set { __Parent = value; } } private GroupData __Parent;
        #endregion
        #region Podpora de/serializace
        /// <summary>
        /// Metoda je volána před zahájením procesu Serializace = data budou uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeStart() { }
        /// <summary>
        /// Metoda je volána po ukončení procesu Serializace = data byla uložena do nějakého Configu.
        /// </summary>
        protected override void OnPersistSerializeDone() { }
        /// <summary>
        /// Metoda je volána po ukončení procesu Deserializace = data byla načtena z nějakého Configu.
        /// Potomek může reagovat = provést nějaké dopočty a finalizaci...
        /// </summary>
        protected override void OnPersistDeserializeDone() { }
        #endregion
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
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje stránky
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var applicationData = (actionInfo.ItemData as ApplicationData);
            bool hasApplicationData = applicationData != null;
            switch (actionType)
            {
                case DataItemActionType.RunApplication:
                    if (hasApplicationData)
                        applicationData.RunNewProcess(false);
                    break;
                case DataItemActionType.RunApplicationAsAdmin:
                    if (hasApplicationData)
                        applicationData.RunNewProcess(true);
                    break;
            }
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
            catch (Exception)
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
        private string _CurrentFileName { get { return GetCurrentFilePath(ExecutableFileName); } }
        /// <summary>
        /// Jméno provozního adresáře, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentWorkingDirectory { get { return GetCurrentFilePath(ExecutableWorkingDirectory); } }
        /// <summary>
        /// Text argumentů, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentArguments { get { return GetCurrentFilePath(ExecutableArguments); } }
        /// <summary>
        /// Jméno ikony, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        [PersistingEnabled(false)]
        private string _CurrentImageFileName { get { return GetCurrentFilePath(ImageFileName); } }
        /// <summary>
        /// Vrátí zadaný string, kde jsou jména proměnných jako %WINDIR% nahrazena aktuálními hodnotami
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetCurrentFilePath(string file)
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
        #region Podpora pro editaci v DataForm
        /// <summary>
        /// Vytvoří a vrátí Edit panel pro this aplikaci
        /// </summary>
        /// <returns></returns>
        protected override DataControlPanel CreateEditPanel()
        {
            var panel = new DataControlPanel();
            int x1 = 0;
            int x2 = 230;
            int y0 = 20;
            int s1 = 22;
            int s2 = 38;
            int w1 = 220;
            int w2 = 320;
            int w3 = 550;

            int y = y0;
            panel.AddCell(ControlType.TextBox, "Titulek", nameof(Title), x1, y, w1); y += s2;
            panel.AddCell(ControlType.TextBox, "Popisek", nameof(Description), x1, y, w1); y += s2;
            panel.AddCell(ControlType.MemoBox, "Nápověda", nameof(ToolTip), x2, y0, w2, 58);
            panel.AddCell(ControlType.FileBox, "Aplikace", nameof(ExecutableFileName), x1, y, w3); y += s2;
            panel.AddCell(ControlType.TextBox, "Argumenty", nameof(ExecutableArguments), x1, y, w3); y += s2;
            panel.AddCell(ControlType.FileBox, "Obrázek", nameof(ImageFileName), x1, y, w3); y += s1;

            int x = x1 + 4;
            int w4 = 160;
            int dx = 170;
            panel.AddCell(ControlType.CheckBox, "Admin mode", nameof(ExecuteInAdminMode), x, y, w4); x += dx;
            panel.AddCell(ControlType.CheckBox, "Single Instance", nameof(OnlyOneInstance), x, y, w4); x += dx;
            panel.AddCell(ControlType.CheckBox, "Maximized", nameof(OpenMaximized), x, y, w4); y += s1;

            panel.Buttons = new DialogButtonType[] { DialogButtonType.Ok, DialogButtonType.Cancel };
            panel.BackColor = Color.AntiqueWhite;

            panel.DataObject = this;
            return panel;
        }
        #endregion
    }
    #endregion
    #region class BaseData : Bázová třída pro data, účastní se zobrazování (obsahuje dostatečná data pro zobrazení)
    /// <summary>
    /// Bázová třída pro data, účastní se zobrazování (obsahuje dostatečná data pro zobrazení)
    /// </summary>
    public class BaseData : IXmlPersistNotify
    {
        #region Základní společná data
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
        /// Barva pozadí, smí obsahovat Alpha kanál, smí být null
        /// </summary>
        public virtual Color? BackColor { get; set; }
        /// <summary>
        /// Posun adresy prvku <see cref="RelativeAdress"/> vůči počátku prostoru. 
        /// Typicky se plní jen do aplikace a do grupy, řeší postupný posun souřadnice Y.
        /// </summary>
        [PersistingEnabled(false)]
        public virtual Point? OffsetAdress { get; set; }
        /// <summary>
        /// Absolutní pozice prvku, určená z <see cref="RelativeAdress"/> + <see cref="OffsetAdress"/>.
        /// </summary>
        [PersistingEnabled(false)]
        public virtual Point Adress { get { return RelativeAdress.GetShiftedPoint(OffsetAdress); } set { RelativeAdress = value.GetShiftedPoint(OffsetAdress, true); } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().Name}: '{Title}'";
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone"></param>
        protected virtual void FillClone(BaseData clone)
        {
            clone.Title = this.Title;
            clone.Description = this.Description;
            clone.ToolTip = this.ToolTip;
            clone.ImageFileName = this.ImageFileName;
            clone.LayoutKind = this.LayoutKind;
            clone.RelativeAdress = this.RelativeAdress;
            clone.BackColor = this.BackColor;
        }
        #endregion
        #region Podpora pro interaktivní kreslení a práci - tvorba instance třídy InteractiveItem
        /// <summary>
        /// Vytvoří a vrátí pole svých Child prvků.
        /// Některé třídy vrací null, když nemají potomky.
        /// </summary>
        /// <returns></returns>
        public virtual List<InteractiveItem> CreateInteractiveItems() { return null; }
        /// <summary>
        /// Vytvoří a vrátí new instanci interaktivního prvku <see cref="InteractiveDataItem"/> = potomek základního prvku <see cref="InteractiveItem"/>
        /// </summary>
        /// <returns></returns>
        public virtual InteractiveItem CreateInteractiveItem()
        {
            this.InteractiveItem = new InteractiveDataItem(this);
            return this.InteractiveItem;
        }
        /// <summary>
        /// Interaktivní prvek, naposledy vytvořený a vrácený z metody <see cref="CreateInteractiveItem()"/>
        /// </summary>
        [PersistingEnabled(false)]
        protected InteractiveItem InteractiveItem { get; set; }
        /// <summary>
        /// Zajistí znovuvykreslení interaktvního prvku
        /// </summary>
        public void RefreshInteractiveItem()
        {
            InteractiveItem?.Refresh();
        }
        #endregion
        #region Podpora pro serializaci
        /// <summary>
        /// Stav v procesu persistování (Load/Save)
        /// </summary>
        [PersistingEnabled(false)]
        XmlPersistState IXmlPersistNotify.XmlPersistState 
        {
            get { return __XmlPersistState; }
            set
            {
                var oldValue = __XmlPersistState;
                var newValue = value;
                if (oldValue != XmlPersistState.LoadBegin && newValue == XmlPersistState.LoadBegin) this.OnPersistDeserializeStart();
                if (oldValue != XmlPersistState.LoadDone && newValue == XmlPersistState.LoadDone) this.OnPersistDeserializeDone();
                if (oldValue != XmlPersistState.SaveBegin && newValue == XmlPersistState.SaveBegin) this.OnPersistSerializeStart();
                if (oldValue != XmlPersistState.SaveDone && newValue == XmlPersistState.SaveDone) this.OnPersistSerializeDone();
                __XmlPersistState = newValue;
            }
        }
        private XmlPersistState __XmlPersistState;
        /// <summary>
        /// Metoda je volána před zahájením procesu Deserializace = data budou načtena z nějakého Configu.
        /// Potomek může reagovat = provést nějaké přípravy...
        /// </summary>
        protected virtual void OnPersistDeserializeStart() { }
        /// <summary>
        /// Metoda je volána po ukončení procesu Deserializace = data byla načtena z nějakého Configu.
        /// Potomek může reagovat = provést nějaké dopočty a finalizaci...
        /// </summary>
        protected virtual void OnPersistDeserializeDone() { }
        /// <summary>
        /// Metoda je volána před zahájením procesu Serializace = data budou uložena do nějakého Configu.
        /// Potomek může reagovat = provést nějaké přípravy dat...
        /// </summary>
        protected virtual void OnPersistSerializeStart() { }
        /// <summary>
        /// Metoda je volána po ukončení procesu Serializace = data byla uložena do nějakého Configu.
        /// Potomek může reagovat = zrušit přípravy provedené před Serializací...
        /// </summary>
        protected virtual void OnPersistSerializeDone() { }
        #endregion
        #region Podpora pro standardní WinForm editaci obsahu pomocí okna DialogForm a datového panelu DataControlPanel
        /// <summary>
        /// Metoda vytvoří okno <see cref="DialogForm"/>, vloží do něj data aktuálního prvku vytvořená v metodě <see cref="CreateEditPanel"/> a okno otevře.
        /// Po ukončení editace v okně refreshuje odpovídající panel a vrátí true pokud editace má být uložena.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="formTitle"></param>
        /// <returns></returns>
        public virtual bool EditData(Point? startPoint = null, string formTitle = null)
        {
            bool result = false;
            using (var form = new DialogForm())
            {
                form.DataControl = this.CreateEditPanel();
                form.Text = formTitle ?? this.Title;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = startPoint ?? Control.MousePosition;
                form.ShowDialog(App.MainForm);
                result = (form.DialogResult == DialogResult.OK);
            }
            this.RefreshInteractiveItem();
            return result;
        }
        /// <summary>
        /// Metoda vytvoří a vrátí panel <see cref="DataControlPanel"/> obsahující jednotlivé controly tohoto atového objektu
        /// </summary>
        /// <returns></returns>
        protected virtual DataControlPanel CreateEditPanel()
        {
            var panel = new DataControlPanel();
            int x1 = 0;
            int x2 = 230;
            int y0 = 20;
            int s1 = 22;
            int s2 = 38;
            int w1 = 220;
            int w2 = 320;
            int w3 = 550;

            int y = y0;
            panel.AddCell(ControlType.TextBox, "Titulek", nameof(Title), x1, y, w1); y += s2;
            panel.AddCell(ControlType.TextBox, "Popisek", nameof(Description), x1, y, w1); y += s2;
            panel.AddCell(ControlType.MemoBox, "Nápověda", nameof(ToolTip), x2, y0, w2, 58);
            panel.AddCell(ControlType.FileBox, "Obrázek", nameof(ImageFileName), x1, y, w3); y += s1;

            panel.Buttons = new DialogButtonType[] { DialogButtonType.Ok, DialogButtonType.Cancel };
            panel.BackColor = Color.AntiqueWhite;

            panel.DataObject = this;
            return panel;
        }
        #endregion
    }
    /// <summary>
    /// Balíček s daty pro akce kontextového menu
    /// </summary>
    public class ContextActionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        public ContextActionInfo(MouseState mouseState, InteractiveGraphicsControl panel, PageSetData pageSetData, BaseData areaData, BaseData itemData)
        {
            MouseState = mouseState;
            Panel = panel;
            PageSetData = pageSetData;
            AreaData = areaData;
            ItemData = itemData;
        }
        /// <summary>
        /// Stav myši. Obsahuje i prvek, nad kterým se akce děje, i buňku mapy.
        /// </summary>
        public MouseState MouseState { get; private set; }
        /// <summary>
        /// Vizuální panel
        /// </summary>
        public InteractiveGraphicsControl Panel { get; private set; }
        /// <summary>
        /// Set stránek, kompletní instance
        /// </summary>
        public PageSetData PageSetData { get; private set; }
        /// <summary>
        /// Prvek reprezentující prostor, v němž se akce provádí (jeho Child prvky se mění)
        /// </summary>
        public BaseData AreaData { get; private set; }
        /// <summary>
        /// Prvek reprezentující konkrétní prvek, jehož se akce týká. Může být null.
        /// </summary>
        public BaseData ItemData { get; private set; }
    }
    /// <summary>
    /// Akce v kontextovém menu
    /// </summary>
    public enum DataItemActionType
    {
        None,
        NewPage,
        MovePage,
        EditPage,
        CopyPage,
        DeletePage,
        NewGroup,
        MoveGroup,
        EditGroup,
        CopyGroup,
        DeleteGroup,
        NewApplication,
        MoveApplication,
        EditApplication,
        CopyApplication,
        DeleteApplication,
        RunApplication,
        RunApplicationAsAdmin
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
        /// <summary>
        /// Druh layoutu, z něhož se čerpá.
        /// </summary>
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
        /// Sada všech stránek.
        /// Seznam stránek má vždy alespoň jednu stránku, obsahující jednu grupu a výchozí aplikace.
        /// </summary>
        [PersistingEnabled(false)]
        public PageSetData PageSet { get { return _GetPageSet(); } }
        /// <summary>
        /// Vrátí data sady stránek. Zajistí jejich případnou tvorbu a naplnění.
        /// </summary>
        /// <returns></returns>
        private PageSetData _GetPageSet()
        {
            if (_PageSet == null) _PageSet = new PageSetData();
            _PageSet.CheckNotVoid();
            return _PageSet;
        }
        /// <summary>
        /// Dictionary obsahující data jednotlivých stránek (uvnitř nich jsou grupy a v grupách aplikace)
        /// </summary>
        [PropertyName("program_set")]
        private PageSetData _PageSet { get; set; }
        #endregion
    }
}

