using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using DjSoft.Tools.ProgramLauncher.Components;
using DjSoft.Tools.ProgramLauncher.Data;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
        public PageSetData() : this(NewIdMode.New) { }
        /// <summary>
        /// Konstruktor pro daný režim
        /// </summary>
        /// <param name="mode"></param>
        protected PageSetData(NewIdMode mode)
        {
            SetNewId(mode, ref __NewId, ref __NewUniqueId);
            __Pages = new ChildItems<PageSetData, PageData>(this);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <param name="asBackupClone">Požadavek true = klon má být zálohou aktuálního prvku (=shodné ID) / false = jde o uživatelskou kopii (nové ID)</param>
        /// <returns></returns>
        public PageSetData Clone(bool asBackupClone)
        {
            NewIdMode mode = (asBackupClone ? NewIdMode.Clone : NewIdMode.Copy);                             // asBackupClone: true = pro zálohování v UndoRedo
            PageSetData clone = new PageSetData(mode);
            this.FillClone(clone, mode);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone">Cílový objekt, do kterého opisujeme data</param>
        /// <param name="mode">Režim přidělení nového ID a UniqueID</param>
        protected override void FillClone(BaseData clone, NewIdMode mode)
        {
            base.FillClone(clone, mode);
            if (clone is PageSetData pageSetClone)
            {
                pageSetClone.__Pages.Clear();
                pageSetClone.__Pages.AddRange(this.Pages.Select(p => p.Clone(mode == NewIdMode.Clone)));      // Pokud mode = Clone, pak asBackupClone = true
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
            int y = 0;
            foreach (var page in this.Pages)                                   // Pořadí stránek v poli určuje vizuální pořadí v controlu
            {
                page.RelativeAdress = new Point(0, y);                         // Relativní pozice stránky;
                page.OffsetAdress = new Point(0, 0);                           // Offset stránky je vždy 0
                y = page.Adress.Y + 1;                                         // A od toho se odvodí počáteční adresa pro další stránku.
                interactiveItems.Add(page.CreateInteractiveItem());
            }
            return interactiveItems;
        }
        /// <summary>
        /// ID pro nově vytvářený prvek.
        /// Klonovaný prvek nedostává nové ID, ale přebírá ID svého originálu.
        /// </summary>
        private static int __NewId = 0;
        /// <summary>
        /// Unikátní ID pro nově vytvářený prvek.
        /// Při klonování se 
        /// </summary>
        private static int __NewUniqueId = 0;
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
        /// Provede editaci daného prvku
        /// </summary>
        /// <param name="menuItem"></param>
        public void RunEditAction(MouseState mouseState, InteractiveGraphicsControl panel, BaseData areaData, BaseData itemData)
        {
            // Kompletní data si odzálohuji ještě před tím, než se začnou provádět změny. Pak je možná dám do UndoRedo containeru:
            PageSetData pageSetClone = this.Clone(true);

            bool isEdited = false;
            if (itemData is PageData pageData)
            {
                isEdited = pageData.EditData(mouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleEditPage, pageData.Title));
            }
            else if (itemData is GroupData groupData)
            {
                isEdited = groupData.EditData(mouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleEditGroup, groupData.Title));
            }
            else if (itemData is ApplicationData applicationData)
            {
                isEdited = applicationData.EditData(mouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleEditApplication, applicationData.Title));
            }

            if (isEdited)
            {
                App.Settings.SetChanged("PageSet");
                App.UndoRedo.Add(pageSetClone);
            }
        }
        /// <summary>
        /// Provede vybranou akci z kontextového menu
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RunContextMenuAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            // Kompletní data si odzálohuji ještě před tím, než se začnou provádět změny. Pak je možná dám do UndoRedo containeru:
            PageSetData pageSetClone = actionInfo.PageSetData.Clone(true);

            bool isEdited = false;
            switch (actionType)
            {
                case DataItemActionType.MovePage:
                    break;
                case DataItemActionType.NewPage:
                case DataItemActionType.EditPage:
                case DataItemActionType.CopyPage:
                case DataItemActionType.DeletePage:
                    isEdited = PageSetData.RunEditAction(actionType, actionInfo);
                    break;
                case DataItemActionType.MoveGroup:
                    break;
                case DataItemActionType.NewGroup:
                case DataItemActionType.EditGroup:
                case DataItemActionType.CopyGroup:
                case DataItemActionType.DeleteGroup:
                    isEdited = PageData.RunEditAction(actionType, actionInfo);
                    break;
                case DataItemActionType.MoveApplication:
                    break;
                case DataItemActionType.NewApplication:
                case DataItemActionType.EditApplication:
                case DataItemActionType.CopyApplication:
                case DataItemActionType.DeleteApplication:
                    isEdited = GroupData.RunEditAction(actionType, actionInfo);
                    break;
                case DataItemActionType.RunApplication:
                case DataItemActionType.RunApplicationAsAdmin:
                    isEdited = ApplicationData.RunEditAction(actionType, actionInfo);
                    break;
            }

            if (isEdited)
            {
                App.Settings.SetChanged("PageSet");
                App.UndoRedo.Add(pageSetClone);
            }
        }
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje stránky
        /// </summary>
        /// <param name="menuItem"></param>
        public static bool RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var pageSetData = actionInfo.PageSetData;

            var pageData = (actionInfo.ItemData as PageData);
            bool hasPageData = pageData != null;
            bool isAppend = false;
            bool isUpdated = false;
            PageData reArrangeTargetPage = null;
            int reArrangeTargetY = -1;
            switch (actionType)
            {
                case DataItemActionType.NewPage:
                    pageData = new PageData();
                    isAppend = pageData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.EditFormTitleNewPage);
                    break;
                case DataItemActionType.EditPage:
                    if (hasPageData)
                        isUpdated = pageData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleEditPage, pageData.Title));
                    break;
                case DataItemActionType.CopyPage:
                    pageData = pageData.Clone(false);
                    isAppend = pageData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleClone, pageData.Title));
                    reArrangeTargetPage = pageData;                            // Tento řádek zajistí, že kopie bude na původní pozici, a originál se odsune o +1 na Y ose...
                    break;
                case DataItemActionType.DeletePage:
                    if (hasPageData)
                    {
                        PageSetData parentSet = pageData.ParentSet;
                        isUpdated = parentSet.Pages.Remove(pageData);
                        pageSetData.ReArrangePages();                          // Po odebrání prvku ostatní prvky srovnat, odebere se tak mezera po odebraném prvku
                    }
                    break;
            }

            if (isAppend)
            {
                pageSetData.Pages.Add(pageData);
                pageSetData.ReArrangePages(reArrangeTargetPage);
                isUpdated = true;
            }

            return isUpdated;
        }
        #endregion
        #region Přesun prvku na jinou pozici
        /// <summary>
        /// Vstupní bod pro provedení akce Přesun prvku na jinou pozici
        /// </summary>
        /// <param name="beginMouseState"></param>
        /// <param name="endMouseState"></param>
        /// <param name="panel"></param>
        /// <param name="areaData"></param>
        /// <param name="itemData"></param>
        public void MoveItem(MouseState beginMouseState, MouseState endMouseState, InteractiveGraphicsControl panel, BaseData areaData, BaseData itemData)
        {
            // Kompletní data si odzálohuji ještě před tím, než se začnou provádět změny. Pak je možná dám do UndoRedo containeru:
            PageSetData pageSetClone = this.Clone(true);

            bool isEdited = false;
            if (areaData is PageSetData pageSetData && itemData is PageData pageItemData)
                isEdited = pageSetData.MovePage(pageItemData, beginMouseState, endMouseState, panel);
            else if (areaData is PageData pageAreaData)
            {
                if (itemData is GroupData groupItemData)
                    isEdited = pageAreaData.MoveGroup(groupItemData, beginMouseState, endMouseState, panel);
                else if (itemData is ApplicationData applicationItemData)
                    isEdited = pageAreaData.MoveApplication(applicationItemData, beginMouseState, endMouseState, panel);
            }

            if (isEdited)
            {
                App.Settings.SetChanged("PageSet");
                App.UndoRedo.Add(pageSetClone);
            }
        }
        /// <summary>
        /// Přesune stránku na nové místo
        /// </summary>
        /// <param name="pageData"></param>
        /// <param name="beginMouseState"></param>
        /// <param name="endMouseState"></param>
        /// <param name="panel"></param>
        public bool MovePage(PageData pageData, MouseState beginMouseState, MouseState endMouseState, InteractiveGraphicsControl panel)
        {
            // Pokud nevím, jakou Y adresu mám nastavit, tak nedělám nic:
            if (endMouseState.InteractiveCell is null) return false;

            // Pokud není změna pozice, pak není žádná akce:
            var newAdress = new Point(0, endMouseState.InteractiveCell.Adress.Y);
            if (newAdress == pageData.RelativeAdress) return false;

            pageData.RelativeAdress = newAdress;
            ReArrangePages(pageData);
            return true;
        }
        /// <summary>
        /// Zajistí validní uspořádání stránek na ose Y. Umožní umístit stránku <paramref name="targetPage"/> na cílovou Y pozici <paramref name="targetY"/>.
        /// </summary>
        /// <param name="targetPage"></param>
        /// <param name="targetY"></param>
        public void ReArrangePages(PageData targetPage = null)
        {
            bool targetExists = (targetPage != null);
            int targetY = (targetExists ? targetPage.RelativeAdress.Y : 0);

            ReArrangeItems(this.Pages, setAdress, 0, targetPage, targetY, BaseData.CompareByRelativeAdressY);


            // Vepíše do daného prvku adresu RelativeAdress = (x,y)
            void setAdress(PageData pgData, int y)
            {
                pgData.RelativeAdress = new Point(0, y);
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
        public PageData() : this(NewIdMode.New) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        protected PageData(NewIdMode mode)
        {
            SetNewId(mode, ref __NewId, ref __NewUniqueId);
            __Groups = new ChildItems<PageData, GroupData>(this);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <param name="asBackupClone">Požadavek true = klon má být zálohou aktuálního prvku (=shodné ID) / false = jde o uživatelskou kopii (nové ID)</param>
        /// <returns></returns>
        public PageData Clone(bool asBackupClone)
        {
            NewIdMode mode = (asBackupClone ? NewIdMode.Clone : NewIdMode.Copy);                             // asBackupClone: true = pro zálohování v UndoRedo
            PageData clone = new PageData(mode);
            this.FillClone(clone, mode);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone">Cílový objekt, do kterého opisujeme data</param>
        /// <param name="mode">Režim přidělení nového ID a UniqueID</param>
        protected override void FillClone(BaseData clone, NewIdMode mode)
        {
            base.FillClone(clone, mode);
            if (clone is PageData pageClone)
            {
                pageClone.__Groups.Clear();
                pageClone.__Groups.AddRange(this.Groups.Select(g => g.Clone(mode == NewIdMode.Clone)));       // Pokud mode = Clone, pak asBackupClone = true
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
        /// Sada, do které tato stránka patří
        /// </summary>
        public PageSetData ParentSet { get { return __Parent; } }
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
        /// ID pro nově vytvářený prvek.
        /// Klonovaný prvek nedostává nové ID, ale přebírá ID svého originálu.
        /// </summary>
        private static int __NewId = 0;
        /// <summary>
        /// Unikátní ID pro nově vytvářený prvek.
        /// Při klonování se 
        /// </summary>
        private static int __NewUniqueId = 0;
        #endregion
        #region Tvorba interaktivních prvků pro tuto stránku - grupy a jejich aplikace, výpočet ofsetu adresy a tedy absolutní adresy
        /// <summary>
        /// Vytvoří a vrátí pole svých Child prvků.
        /// </summary>
        /// <returns></returns>
        public override List<InteractiveItem> CreateInteractiveItems()
        {
            List<InteractiveItem> interactiveItems = new List<InteractiveItem>();
            int y = 0;
            foreach (var group in this.Groups)                                 // Pořadí grup v poli určuje vizuální pořadí v controlu
            {
                group.RelativeAdress = new Point(0, 0);                        // Grupa vždy začíná relativně na nule, 
                group.OffsetAdress = new Point(0, y);                          // K tomu se přidá Offset
                y = group.Adress.Y + 1;                                        // A od toho se odvodí počáteční offset pro aplikace.
                interactiveItems.Add(group.CreateInteractiveItem());

                int maxBottom = 0;
                foreach (var appl in group.Applications)
                {
                    appl.OffsetAdress = new Point(0, y);
                    interactiveItems.Add(appl.CreateInteractiveItem());
                    int appBottom = appl.Adress.Y + 1;
                    if (maxBottom < appBottom) maxBottom = appBottom;          // Střádám si maxBottom = nejvyšší Bottom z aplikací dané grupy
                }
                y = maxBottom;
            }
            return interactiveItems;
        }
        /// <summary>
        /// Metoda najde a vrátí grupu, do které by měl patřit prostor uvedený v <paramref name="actionInfo"/>.
        /// Současně určí relativní adresu buňky v rámci grupy, protože vstupní adresa v <paramref name="actionInfo"/> je adresou absolutní vzhledem k Page, 
        /// ale pro případné umístění nového prvku <see cref="ApplicationData"/> je třeba mít adresu relativní v rámci grupy.
        /// </summary>
        /// <param name="actionInfo"></param>
        /// <param name="createNew"></param>
        /// <param name="relativeAdress"></param>
        /// <returns></returns>
        public GroupData SearchForGroup(ContextActionInfo actionInfo, bool createNew, out Point? relativeAdress)
        {
            return SearchForGroup(actionInfo.MouseState.InteractiveCell.Adress, createNew, out relativeAdress);
        }
        /// <summary>
        /// Metoda najde a vrátí grupu, do které by měl patřit prostor uvedený v <paramref name="actionInfo"/>.
        /// Současně určí relativní adresu buňky v rámci grupy, protože vstupní adresa v <paramref name="actionInfo"/> je adresou absolutní vzhledem k Page, 
        /// ale pro případné umístění nového prvku <see cref="ApplicationData"/> je třeba mít adresu relativní v rámci grupy.
        /// </summary>
        /// <param name="actionInfo"></param>
        /// <param name="createNew"></param>
        /// <param name="relativeAdress"></param>
        /// <returns></returns>
        public GroupData SearchForGroup(Point adress, bool createNew, out Point? relativeAdress)
        {
            GroupData groupData = null;
        
            foreach (var group in this.Groups)
            {
                // Pokud už mám grupu, a testovaná grupa má počátek Y (absolutní logická pozice, nikoli pixely) větší než zadaná hodnota,
                // tak skončím hledání a akceptuji poslední nalezenou grupu:
                if (groupData != null && group.Adress.Y > adress.Y) break;
                groupData = group;
            }

            // Pokud jsme grupu nenašli:
            if (groupData is null)
            {   // ... a pokud ji nemáme vytvářet => vrátím null a bez určení relativní adresy:
                if (!createNew)
                {
                    relativeAdress = null;
                    return null;
                }
                // Vytvořím new grupu:
                groupData = new GroupData() { Title = App.Messages.EditDataNewDefaultGroupTitle };
                relativeAdress = new Point(adress.X, 0);
                this.Groups.Add(groupData);
                return groupData;
            }

            // Mám grupu: určím relativní adresu:
            int cellY = adress.Y - (groupData.Adress.Y + 1);
            relativeAdress = new Point(adress.X, cellY);
            return groupData;
        }
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje grupy
        /// </summary>
        /// <param name="menuItem"></param>
        public static bool RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var pageData = actionInfo.AreaData as PageData;
            bool hasPageData = (pageData != null);
            if (!hasPageData) return false;

            var groupData = (actionInfo.ItemData as GroupData);
            bool hasGroupData = (groupData != null);
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
                        isUpdated = groupData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleEditGroup, groupData.Title));
                    break;
                case DataItemActionType.CopyGroup:
                    groupData = groupData.Clone(false);
                    isAppend = groupData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleClone, groupData.Title));
                    break;
                case DataItemActionType.DeleteGroup:
                    if (hasGroupData)
                    {
                        PageData parentPage = groupData.ParentPage;
                        isUpdated = parentPage.Groups.Remove(groupData);
                    }
                    break;
            }

            if (isAppend)
            {
                int y = 0;
                groupData.Adress = new Point(0, y);
                pageData.Groups.Add(groupData);
                isUpdated = true;
            }

            return isUpdated;
        }
        #endregion
        #region Přesun prvku na jinou pozici
        public bool MoveGroup(GroupData groupData, MouseState beginMouseState, MouseState endMouseState, InteractiveGraphicsControl panel)
        {

            return false;
        }
        /// <summary>
        /// Přemístí daný prvek (<paramref name="applicationData"/>) z jeho dosavadní polohy <paramref name="beginMouseState"/> do cílové polohy <paramref name="endMouseState"/>.
        /// Řeší přemístění uvnitř grupy i do jiné grupy.
        /// Nelze přemístit na jinou stránku.
        /// </summary>
        /// <param name="applicationData"></param>
        /// <param name="beginMouseState"></param>
        /// <param name="endMouseState"></param>
        /// <param name="panel"></param>
        /// <returns></returns>
        public bool MoveApplication(ApplicationData applicationData, MouseState beginMouseState, MouseState endMouseState, InteractiveGraphicsControl panel)
        {
            bool isChanged = false;

            var beginGroup = applicationData?.ParentGroup;
            if (beginGroup is null) return isChanged;

            var endGroup = SearchForGroup(endMouseState.InteractiveCell.Adress, false, out var endRelativeAdress);

            _MoveApplicationCorrectAdress(beginGroup, endMouseState, ref endGroup, ref endRelativeAdress);

            bool isBetweenGroup = !Object.ReferenceEquals(beginGroup, endGroup);
            if (isBetweenGroup)
            {
                beginGroup.Applications.Remove(applicationData);
                endGroup.Applications.AddWhenNotContains(applicationData);
                beginGroup.ReArrangeApplications();
                isChanged = true;
            }

            var newAdress = getValidAdress(endRelativeAdress);
            if (newAdress != applicationData.RelativeAdress)
                isChanged = true;

            if (isChanged)
            {
                applicationData.RelativeAdress = newAdress;
                endGroup.ReArrangeApplications(applicationData);
            }

            return isChanged;

            // Vrátí platnou adresu (tedy s hodnotami X i Y ne-zápornými):
            Point getValidAdress(Point? adress)
            {
                int x = adress?.X ?? 0;
                if (x < 0) x = 0;
                int y = adress?.Y ?? 0;
                if (y < 0) y = 0;
                return new Point(x, y);
            }
        }
        /// <summary>
        /// Koriguje cílový prostor pro situaci, kdy je jako cíl vybrán GroupHeader, anebo poslední řada prvků a její dolní okraj
        /// </summary>
        /// <param name="beginGroup"></param>
        /// <param name="endMouseState"></param>
        /// <param name="endGroup"></param>
        /// <param name="endRelativeAdress"></param>
        private void _MoveApplicationCorrectAdress(GroupData beginGroup, MouseState endMouseState, ref GroupData endGroup, ref Point? endRelativeAdress)
        {
            if (endGroup is null) endGroup = beginGroup;
            if (!endRelativeAdress.HasValue) endRelativeAdress = new Point(0, endGroup.ApplicationsMaxPoint.Y + 1);

            var endCellBounds = endMouseState.InteractiveCell.VirtualBounds;

            if (endRelativeAdress.Value.Y < 0)
            {   // Pokud jsem přesunul prvek přímo na záhlaví grupy, pak mám Y = -1...  Co s tím?
                endRelativeAdress = new Point(endRelativeAdress.Value.X, 0);

                // a) Pokud prvek pod koncovou myší je skutečně GroupData, pak určím, zda myš je na Y souřadnici v horní nebo dolní polovině Headeru:
                bool isBottomHalf = true;
                if (endCellBounds.Height > 6)
                {
                    int endCenter = endCellBounds.Top + (endCellBounds.Height / 2);
                    isBottomHalf = endMouseState.LocationVirtual.Y > endCenter;          // true pokud myš je v dolní polovině headeru, false když jsme nahoře
                }

                // Pokud jsme v dolní polovině, pak prostě v endRelativeAdress nechám hodnotu Y = 0, a skončím:
                if (isBottomHalf) return;

                // Jsme v horní polovině Headeru: realizujeme přemístění do předešlé grupy (pokud existuje), do nového dolního řádku:
                var prevGroup = endGroup.ParentPage.GetNearGroup(endGroup, -1);
                // Pokud před danou grupou na offsetu -1 už není žádná grupa, pak jsme na první grupě a necháme ji tak včetně indexu Y = 0:
                if (prevGroup is null) return;

                endGroup = prevGroup;
                endRelativeAdress = new Point(endRelativeAdress.Value.X, endGroup.ApplicationsMaxPoint.Y + 1);
                return;
            }

            // Jsem v běžném prostoru aplikací dané grupy. 
            // Pokud jsem v poslední řadě (Y) a v dolní třetině cílového prvku, a prvek existuje, pak prvek přesunu do další (=nové) dolní řady:
            var endGroupMaxPoint = endGroup.ApplicationsMaxPoint;
            if (endRelativeAdress.Value.Y < endGroupMaxPoint.Y) return;        // Cílová adresa má Y menším než poslední řada = nebudu nic korigovat.

            // Pokud cílový prostor neobsahuje žádnou aplikaci (Item), pak do buňky klidně přesunu Target prvek = je tam pro něj místo:
            if (endMouseState.InteractiveCell.Items is null || endMouseState.InteractiveCell.Items.Length == 0) return;

            // Cílová buňka je v poslední řadě aplikací dané grupy, a je tam nějaká aplikace (je obsazeno).
            // Pokud je myš v dolní třetině buňky, pak Target prvek umístím do nové další řady dolů => dám Y = +1:
            if (endCellBounds.Height > 6)
            {
                int endTertia = endCellBounds.Top + (2 * endCellBounds.Height / 3);  // Pozice Y dolní třetiny cílové buňky
                bool isBottomTertia = endMouseState.LocationVirtual.Y >= endTertia;
                if (isBottomTertia)
                {
                    endRelativeAdress = new Point(endRelativeAdress.Value.X, endGroupMaxPoint.Y + 1);
                    return;
                }
            }
        }
        /// <summary>
        /// Vrátí grupu sousední k grupě dané v rámci this stránky.
        /// Sousední = vzdálená o <paramref name="offset"/> od dané grupy <paramref name="groupData"/>.
        /// Může vrátit null.
        /// </summary>
        /// <param name="groupData"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public GroupData GetNearGroup(GroupData groupData, int offset)
        {
            if (groupData is null) return null;
            int index = this.Groups.IndexOf(groupData);
            if (index < 0) return null;
            int nearIndex = index + offset;
            if (nearIndex < 0 ||  nearIndex >= this.Groups.Count) return null;
            return this.Groups[nearIndex];
        }
        /// <summary>
        /// Zajistí validní uspořádání skupin na ose Y. Umožní umístit stránku <paramref name="targetPage"/> na cílovou Y pozici <paramref name="targetY"/>.
        /// </summary>
        /// <param name="targetPage"></param>
        /// <param name="targetY"></param>
        public void ReArrangeGroups(GroupData targetGroup = null)
        {
            bool targetExists = (targetGroup != null);
            int targetY = (targetExists ? targetGroup.RelativeAdress.Y : 0);

            ReArrangeItems(this.Groups, setAdress, 0, targetGroup, targetY, BaseData.CompareByRelativeAdressY);


            // Vepíše do daného prvku adresu RelativeAdress = (x,y)
            void setAdress(GroupData grpData, int y)
            {
                grpData.RelativeAdress = new Point(0, y);
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
        public GroupData() : this(NewIdMode.New) { }
        /// <summary>
        /// Konstruktor pro daný režim
        /// </summary>
        /// <param name="mode"></param>
        protected GroupData(NewIdMode mode)
        {
            SetNewId(mode, ref __NewId, ref __NewUniqueId);
            __Applications = new ChildItems<GroupData, ApplicationData>(this);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <param name="asBackupClone">Požadavek true = klon má být zálohou aktuálního prvku (=shodné ID) / false = jde o uživatelskou kopii (nové ID)</param>
        /// <returns></returns>
        public GroupData Clone(bool asBackupClone)
        {
            NewIdMode mode = (asBackupClone ? NewIdMode.Clone : NewIdMode.Copy);                             // asBackupClone: true = pro zálohování v UndoRedo
            GroupData clone = new GroupData(mode);
            this.FillClone(clone, mode);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone">Cílový objekt, do kterého opisujeme data</param>
        /// <param name="mode">Režim přidělení nového ID a UniqueID</param>
        protected override void FillClone(BaseData clone, NewIdMode mode)
        {
            base.FillClone(clone, mode);
            if (clone is GroupData groupClone)
            {
                groupClone.__Applications.Clear();
                groupClone.__Applications.AddRange(this.Applications.Select(a => a.Clone(mode == NewIdMode.Clone)));      // Pokud mode = Clone, pak asBackupClone = true
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
        /// Stránka, do které tato skupina patří
        /// </summary>
        public PageData ParentPage { get { return __Parent; } }
        /// <summary>
        /// Můj parent
        /// </summary>
        PageData IChildOfParent<PageData>.Parent { get { return __Parent; } set { __Parent = value; } } private PageData __Parent;
        /// <summary>
        /// ID pro nově vytvářený prvek.
        /// Klonovaný prvek nedostává nové ID, ale přebírá ID svého originálu.
        /// </summary>
        private static int __NewId = 0;
        /// <summary>
        /// Unikátní ID pro nově vytvářený prvek.
        /// Při klonování se 
        /// </summary>
        private static int __NewUniqueId = 0;
        /// <summary>
        /// Obsahuje nejvyšší adresu X a Y ze všech aplikací.
        /// </summary>
        public Point ApplicationsMaxPoint
        {
            get
            {
                int maxX = 0;
                int maxY = 0;
                this.Applications.ForEachExec(a => { var p = a.RelativeAdress; if (p.X > maxX) maxX = p.X; if (p.Y > maxY) maxY = p.Y; });
                return new Point(maxX, maxY);
            }
        }
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje stránky
        /// </summary>
        /// <param name="menuItem"></param>
        public static bool RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
        {
            var pageData = actionInfo.AreaData as PageData;
            bool hasPageData = (pageData != null);
            if (!hasPageData) return false;

            var applicationData = (actionInfo.ItemData as ApplicationData);
            bool hasApplicationData = (applicationData != null);
            bool isAppend = false;
            bool isUpdated = false;
            switch (actionType)
            {
                case DataItemActionType.NewApplication:
                    applicationData = new ApplicationData();
                    isAppend = applicationData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.EditFormTitleNewApplication);
                    break;
                case DataItemActionType.EditApplication:
                    if (hasApplicationData)
                        isUpdated = applicationData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleEditApplication, applicationData.Title));
                    break;
                case DataItemActionType.CopyApplication:
                    applicationData = applicationData.Clone(false);
                    isAppend = applicationData.EditData(actionInfo.MouseState.LocationAbsolute, App.Messages.Format(App.Messages.EditFormTitleClone, applicationData.Title));
                    break;
                case DataItemActionType.DeleteApplication:
                    if (hasApplicationData)
                    {
                        GroupData parentGroup = applicationData.ParentGroup;
                        isUpdated = parentGroup.Applications.Remove(applicationData);
                    }
                    break;
            }

            if (isAppend)
            {
                GroupData parentGroup = pageData.SearchForGroup(actionInfo, true, out var relativeAdress);
                applicationData.Adress = relativeAdress.Value;
                parentGroup.Applications.Add(applicationData);
                parentGroup.ReArrangeApplications(applicationData);
                isUpdated = true;
            }

            return isUpdated;
        }
        /// <summary>
        /// Zajistí validní uspořádání aplikací na ose X i Y.
        /// </summary>
        /// <param name="targetApplication"></param>
        public void ReArrangeApplications(ApplicationData targetApplication = null)
        {
            bool targetExists = (targetApplication != null);
            int targetX = (targetExists ? targetApplication.RelativeAdress.X : 0);
            int targetY = (targetExists ? targetApplication.RelativeAdress.Y : 0);
            var rows = this.Applications.CreateDictionaryArray(a => a.RelativeAdress.Y).ToList();
            rows.Sort((a, b) => a.Key.CompareTo(b.Key));
            int y = 0;
            bool targetFound = false;
            foreach (var row in rows)
            {
                int rowY = row.Key;
                if (targetExists && rowY == targetY)
                {   // Aktuálně zpracovávám řádek, který obsahuje prvek 'targetApplication' => tento řádek řeším specificky:
                    ReArrangeItems(row.Value, setAdress, targetItem: targetApplication, targetValue: targetX);
                    targetFound = true;
                }
                else
                    // Běžný řádek: prostě očísluje souřadnici X od 0 do poslední:
                    ReArrangeItems(row.Value, setAdress);

                // Další row bude mít y +1:
                y++;
            }

            if (targetExists && !targetFound)                                            // Pokud jsme target prvek nenašli, ačkoliv je zadán:
                setAdress(targetApplication, 0);                                         // Umístíme cílový objekt (targetItem) na nový řádek, na pozici X = 0


            // Setřídit aplikace musím explicitně:
            this.Applications.Sort(BaseData.CompareByRelativeAdressYX);

            // Vepíše do daného prvku adresu RelativeAdress = (x,y)
            void setAdress(ApplicationData appData, int x)
            {
                appData.RelativeAdress = new Point(x, y);
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
        public ApplicationData() : this(NewIdMode.New) { }
        /// <summary>
        /// Konstruktor pro daný režim
        /// </summary>
        /// <param name="mode"></param>
        protected ApplicationData(NewIdMode mode)
        {
            SetNewId(mode, ref __NewId, ref __NewUniqueId);
        }
        /// <summary>
        /// Vytvoří a vrátí klon this instance z pohledu uživatelských dat.
        /// Z hlediska dočasných dat uložených navíc je to new instance.
        /// </summary>
        /// <param name="asBackupClone">Požadavek true = klon má být zálohou aktuálního prvku (=shodné ID) / false = jde o uživatelskou kopii (nové ID)</param>
        /// <returns></returns>
        public ApplicationData Clone(bool asBackupClone)
        {
            NewIdMode mode = (asBackupClone ? NewIdMode.Clone : NewIdMode.Copy);                             // asBackupClone: true = pro zálohování v UndoRedo
            ApplicationData clone = new ApplicationData(mode);
            this.FillClone(clone, mode);
            return clone;
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// </summary>
        /// <param name="clone">Cílový objekt, do kterého opisujeme data</param>
        /// <param name="mode">Režim přidělení nového ID a UniqueID</param>
        protected override void FillClone(BaseData clone, NewIdMode mode)
        {
            base.FillClone(clone, mode);
            if (clone is ApplicationData applicationClone)
            {
                applicationClone.ExecutableFileName = this.ExecutableFileName;
                applicationClone.ExecutableWorkingDirectory = this.ExecutableWorkingDirectory;
                applicationClone.ExecutableArguments = this.ExecutableArguments;
                applicationClone.ExecuteInAdminMode = this.ExecuteInAdminMode;
                applicationClone.OpenMaximized = this.OpenMaximized;
                applicationClone.OnlyOneInstance = this.OnlyOneInstance;
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + $"; Executable: '{ExecutableFileName}'";
        }
        /// <summary>
        /// Druh layoutu, z něhož se čerpá.
        /// </summary>
        public override DataLayoutKind? LayoutKind { get { return DataLayoutKind.Applications; } set { } }
        /// <summary>
        /// Grupa, do které tato aplikace patří
        /// </summary>
        public GroupData ParentGroup { get { return __Parent; } }
        /// <summary>
        /// Můj parent
        /// </summary>
        GroupData IChildOfParent<GroupData>.Parent { get { return __Parent; } set { __Parent = value; } } private GroupData __Parent;
        /// <summary>
        /// ID pro nově vytvářený prvek.
        /// Klonovaný prvek nedostává nové ID, ale přebírá ID svého originálu.
        /// </summary>
        private static int __NewId = 0;
        /// <summary>
        /// Unikátní ID pro nově vytvářený prvek.
        /// Při klonování se 
        /// </summary>
        private static int __NewUniqueId = 0;
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
        public string ExecutableWorkingDirectory { get { return __ExecutableWorkingDirectory; } set { __ExecutableWorkingDirectory = ValidateExecutableFileName(value); } } private string __ExecutableWorkingDirectory;
        [PropertyName("Arguments")]
        public string ExecutableArguments { get; set; }
        [PropertyName("AdminMode")]
        public bool ExecuteInAdminMode { get; set; }
        [PropertyName("Maximized")]
        public bool OpenMaximized { get; set; }
        [PropertyName("OneInstance")]
        public bool OnlyOneInstance { get; set; }
        /// <summary>
        /// Metoda vrátí validní název spustitelného souboru = odstraní krajní uvozovky
        /// </summary>
        /// <param name="executableFileName"></param>
        /// <returns></returns>
        public static string ValidateExecutableFileName(string executableFileName)
        {
            if (String.IsNullOrEmpty(executableFileName)) return "";
            executableFileName = executableFileName.Trim();
            if (executableFileName.StartsWith("\"") && executableFileName.EndsWith("\"") && executableFileName.Where(c => c == '"').Count() == 2)
            {   // Text začíná uvozovkou a končí uvozovkou a uvnitř není žádná jiná:
                if (executableFileName.Length <= 2) return "";
                return executableFileName.Substring(1, executableFileName.Length - 2).Trim();
            }
            return executableFileName;
        }
        #endregion
        #region Provedení editační akce pro některý z mých Child prvků
        /// <summary>
        /// Provede vybranou akci pro svoje stránky
        /// </summary>
        /// <param name="menuItem"></param>
        public static bool RunEditAction(DataItemActionType actionType, ContextActionInfo actionInfo)
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

            return false;
        }
        #endregion
        #region Spouštění aplikace
        /// <summary>
        /// Spustí / aktivuje daný proces
        /// </summary>
        public void RunApplication()
        {
            if (!_IsValidToRun()) return;

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
            if (!_IsValidToRun()) return;

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
        /// Metoda ověří, zda this aplikaci je možno spustit. Pokud ano, vrátí true. 
        /// Pokud nelze aplikaci spustit, může oznámit chybu (pokud <paramref name="silent"/> je false), a vrátí false
        /// </summary>
        /// <param name="silent"></param>
        /// <returns></returns>
        private bool _IsValidToRun(bool silent = false)
        {
            var fileName = _CurrentFileName;
            bool fileIsFilled = !String.IsNullOrEmpty(fileName);
            bool uriValid = System.Uri.TryCreate(fileName, UriKind.Absolute, out var uri);
            bool fileExists = fileIsFilled && uriValid && (!uri.IsFile || (uri.IsFile && System.IO.File.Exists(fileName)));
            if (fileExists) return true;
            if (!silent)
            {
                if (!fileIsFilled)
                    App.ShowMessage(App.Messages.ExecutableFileIsNotSpecified, MessageBoxIcon.Error);
                else if (!fileExists)
                    App.ShowMessage(App.Messages.ExecutableFileIsNotExists, MessageBoxIcon.Error);
            }
            return false;
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
            panel.AddCell(ControlType.TextBox, App.Messages.EditDataTitleText, nameof(Title), x1, y, w1); y += s2;
            panel.AddCell(ControlType.TextBox, App.Messages.EditDataDescriptionText, nameof(Description), x1, y, w1); y += s2;
            panel.AddCell(ControlType.MemoBox, App.Messages.EditDataToolTipText, nameof(ToolTipText), x2, y0, w2, 58);
            panel.AddCell(ControlType.FileBox, App.Messages.EditDataExecutableFileNameText, nameof(ExecutableFileName), x1, y, w3, validator: ValidateExecutableFileName); y += s2;
            panel.AddCell(ControlType.FileBox, App.Messages.EditDataExecutableWorkingDirectory, nameof(ExecutableWorkingDirectory), x1, y, w3, validator: ValidateExecutableFileName); y += s2;
            panel.AddCell(ControlType.TextBox, App.Messages.EditDataExecutableArgumentsText, nameof(ExecutableArguments), x1, y, w3); y += s2;
            panel.AddCell(ControlType.FileBox, App.Messages.EditDataImageFileNameText, nameof(ImageFileName), x1, y, w3); y += s1;

            int x = x1 + 4;
            int w4 = 175;
            int dx = 180;
            panel.AddCell(ControlType.CheckBox, App.Messages.EditDataExecuteInAdminModeText, nameof(ExecuteInAdminMode), x, y, w4); x += dx;
            panel.AddCell(ControlType.CheckBox, App.Messages.EditDataOnlyOneInstanceText, nameof(OnlyOneInstance), x, y, w4); x += dx;
            panel.AddCell(ControlType.CheckBox, App.Messages.EditDataOpenMaximizedText, nameof(OpenMaximized), x, y, w4); y += s1;

            if (panel.TryGetControl(nameof(ImageFileName), out var imageFilePanel) && imageFilePanel is DFileBox dFileBox)
                dFileBox.DefaultPath = App.Settings.DefaultImagePath;

            panel.Buttons = new DialogButtonType[] { DialogButtonType.Ok, DialogButtonType.Cancel };
            panel.BackColor = Color.AntiqueWhite;

            panel.DataObject = this;
            return panel;
        }
        /// <summary>
        /// Metoda je volaná poté, uživatel editoval data v panelu a potvrdil je OK.
        /// Metoda dostává tentýž panel <see cref="DataControlPanel"/>, který vytvořila metoda <see cref="BaseData.CreateEditPanel()"/>,
        /// a může si z panelu přečíst data nad rámec standardních hodnot, která jsou do datového objektu vepsána standardně.
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        protected override void AcceptedEditPanel(DataControlPanel panel)
        {
            // Pokud uživatel vybral soubor s ikonou z nějakého adresáře pomocí buttonu, pak si tento adresář uložíme do Settings:
            if (panel.TryGetControl(nameof(ImageFileName), out var imageFilePanel) && imageFilePanel is DFileBox dFileBox)
            {
                string userSelectedPath = dFileBox.UserSelectedPath;
                if (!String.IsNullOrEmpty(userSelectedPath))
                    App.Settings.DefaultImagePath = userSelectedPath;
            }
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
        public virtual string ToolTipText { get; set; }
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
        public virtual Point Adress { get { return RelativeAdress.Add(OffsetAdress); } set { RelativeAdress = value.Add(OffsetAdress, true); } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().Name}: '{Title}' [Id:{Id}, UniqueId:{UniqueId}]";
        }
        /// <summary>
        /// Do dodaného objektu <paramref name="clone"/> opíše svoje hodnoty, které jsou permanentní.
        /// Neopisuje hodnoty nad rámec toho, například v klonovaném objektu ponechává původní <see cref="InteractiveItem"/>, tedy nejspíš null (po konstruktoru).
        /// </summary>
        /// <param name="clone">Cílový objekt, do kterého opisujeme data</param>
        /// <param name="mode">Režim přidělení nového ID a UniqueID</param>
        protected virtual void FillClone(BaseData clone, NewIdMode mode)
        {
            clone.Title = this.Title;
            clone.Description = this.Description;
            clone.ToolTipText = this.ToolTipText;
            clone.ImageFileName = this.ImageFileName;
            clone.LayoutKind = this.LayoutKind;
            clone.RelativeAdress = this.RelativeAdress;
            clone.BackColor = this.BackColor;

            if (mode == NewIdMode.Clone)
            {
                clone.__Id = this.__Id;
            }
        }
        #endregion
        #region Komparátory a Arranger
        /// <summary>
        /// Zajistí, že objekty v dodané kolekci budou mít vloženu postupně navyšovanou hodnotu do své pozice.
        /// A že explicitně daný objekt <paramref name="targetItem"/> bude mít vloženou hodnotu explicitně požadovanou <paramref name="targetValue"/> (anebo poslední +1).
        /// <para/>
        /// Metoda obecně slouží k "zařazení" dodaného objektu <paramref name="targetItem"/> na určitou pozici, přičemž prvky v dodané kolekci <paramref name="items"/>
        /// nacházející se před touto pozicí budou mít svoji pozici danou počínaje <paramref name="valueBegin"/> (nebo 0), dodaný prvek bude mít pozici dodanou 
        /// a prvky následující budou mít pozici navazující.
        /// <para/>
        /// Tedy ještě jinými slovy: zadaný target prvek <paramref name="targetItem"/> bude umístěn na pozici do celé kolekce na danoé místo, okolní prvky budou na pozici před a za touto pozicí.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="items"></param>
        /// <param name="setValue">Metoda, která do daného prvku vepíše danou pozici. Metoda sama bude vědět, do které property se poice vepisuje a jakým způsobem. Typicky se z dodané pozice vytvoří Point do RelativeAdress.</param>
        /// <param name="valueBegin"></param>
        /// <param name="targetItem">Cílový objekt, smí být null</param>
        /// <param name="targetValue">Pozice cílového prvku. Pokud prvek <paramref name="targetItem"/> bude null, nepoužije se.</param>
        /// <param name="comparison"></param>
        protected static void ReArrangeItems<TItem>(IEnumerable<TItem> items, Action<TItem, int> setValue, int valueBegin = 0, TItem targetItem = null, int targetValue = 0, Comparison<TItem> comparison = null) where TItem : class
        {
            if (items is null) return;

            bool targetExists = (targetItem != null);
            bool targetFound = false;
            int value = valueBegin;
            foreach (var item in items)
            {
                if (targetExists && Object.ReferenceEquals(item, targetItem)) continue;  // Cílový objekt (targetItem) řešíme jinak, v jeho původní pozici jej přeskakuji.
                if (targetExists && value == targetValue)
                {   // Na tuto pozici (targetValue) chci umístit cílový objekt 'targetItem':
                    setValue(targetItem, value);                                         // Umístíme cílový objekt na aktuální hodnotu, a poznamenáme si, že je OK:
                    targetFound = true;
                    value++;                                                             // Průběžný objekt 'item' bude až za target pozicí.
                }
                setValue(item, value);                                                   // Průběžný objekt bude na průběžně navyšované pozici
                value++;
            }
            if (targetExists && !targetFound)                                            // Pokud jsme target prvek nenašli, ačkoliv je zadán:
                setValue(targetItem, value);                                             // Umístíme cílový objekt (targetItem) za poslední průběžnou pozici

            if (comparison != null)
            {   // Setřídíme, když je čím:
                if (items is List<TItem> list)
                    list.Sort(comparison);
                else if (items is ISortableList<TItem> iSortableList)
                    iSortableList.Sort(comparison);
            }
        }
        /// <summary>
        /// Komparátor dle <see cref="RelativeAdress"/>.X ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByRelativeAdressX(BaseData a, BaseData b)
        {
            return a.RelativeAdress.X.CompareTo(b.RelativeAdress.X);
        }
        /// <summary>
        /// Komparátor dle <see cref="RelativeAdress"/>.Y ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByRelativeAdressY(BaseData a, BaseData b) 
        {
            return a.RelativeAdress.Y.CompareTo(b.RelativeAdress.Y);
        }
        /// <summary>
        /// Komparátor dle <see cref="RelativeAdress"/>.Y ASC, X ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByRelativeAdressYX(BaseData a, BaseData b)
        {
            int cmp = a.RelativeAdress.Y.CompareTo(b.RelativeAdress.Y);
            if (cmp == 0) cmp = a.RelativeAdress.X.CompareTo(b.RelativeAdress.X);
            return cmp;
        }

        #endregion
        #region Id a UniqueId
        /// <summary>
        /// ID tohoto objektu. Pokud this objekt vznikl klonováním z jiného objektu, pak je zde uloženo ID zdroje klonu. Klonům se nepřiděluje nové ID.
        /// Pokud objekt vzniká jako Kopie, pak má nové ID.
        /// <para/>
        /// <u>Rozdíl mezi Klonem a Kopií:</u><br/>
        /// Klon se používá při zálohování stavu do UndoRedo containeru, a klonovaný objekt má charakter "Zálohy původního objektu". Ten má shodné ID jako zdroj.<br/>
        /// Kopie se používá tehdy, když uživatel interaktivně vytváří nový prvek jako kopii nějakého prvku proto, aby v něm něco změnil. a používal oba objekty vedle sebe. Kopie má nové číslo ID.
        /// </summary>
        public int Id { get { return __Id; } }
        /// <summary>
        /// Unikátní ID objektu. Při klonování se vždy přidělí nové, tím je možno odlišit nový klon od původního objektu. Bez ohledu na druh klonování / kopírování (viz informace u <see cref="Id"/>).
        /// </summary>
        public int UniqueId { get { return __UniqueId; } }
        /// <summary>
        /// ID tohoto objektu. Pokud this objekt vznikl klonováním z jiného objektu, pak je zde uloženo ID zdroje klonu. Klonům se nepřiděluje nové ID.
        /// Pokud objekt vzniká jako Kopie, pak má nové ID.
        /// <para/>
        /// <u>Rozdíl mezi Klonem a Kopií:</u><br/>
        /// Klon se používá při zálohování stavu do UndoRedo containeru, a klonovaný objekt má charakter "Zálohy původního objektu". Ten má shodné ID jako zdroj.<br/>
        /// Kopie se používá tehdy, když uživatel interaktivně vytváří nový prvek jako kopii nějakého prvku proto, aby v něm něco změnil. a používal oba objekty vedle sebe. Kopie má nové číslo ID.
        /// </summary>
        protected int __Id = 0;
        /// <summary>
        /// Unikátní ID objektu. Při klonování se vždy přidělí nové, tím je možno odlišit nový klon od původního objektu. Bez ohledu na druh klonování / kopírování (viz informace u <see cref="Id"/>).
        /// </summary>
        protected int __UniqueId = 0;
        /// <summary>
        /// Do this instance vloží nové ID podle daného režimu, s použitím hodnot ID v parametrech (statické fields v konkrétní třídě potomka)
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="id"></param>
        /// <param name="uniqueId"></param>
        protected void SetNewId(NewIdMode mode, ref int id, ref int uniqueId)
        {
            switch (mode)
            {
                case NewIdMode.New:
                    this.__Id = ++id;
                    this.__UniqueId = ++uniqueId;
                    break;
                case NewIdMode.Copy:
                    this.__Id = ++id;
                    this.__UniqueId = ++uniqueId;
                    break;
                case NewIdMode.Clone:
                    // Hodnotu Id do nové instance (clone) opisuje metoda FillClone(BaseData clone, NewIdMode mode) pro (mode == NewIdMode.Clone) !
                    this.__UniqueId = ++uniqueId;
                    break;
            }
        }
        /// <summary>
        /// Režim přidělování ID
        /// </summary>
        protected enum NewIdMode
        {
            /// <summary>
            /// Nový prvek: obě ID budou nově přidělená
            /// </summary>
            New,
            /// <summary>
            /// Kopie prvku: obě ID budou nově přidělená
            /// </summary>
            Copy,
            /// <summary>
            /// Záložní klon: ID bude shodné (podle něj najdeme zdrojový prvek), UniqueID bude nové, abycom poznali generaci klonu
            /// </summary>
            Clone
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
        public InteractiveItem InteractiveItem { get; protected set; }
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
                var dataControlPanel = this.CreateEditPanel();
                form.DataControl = dataControlPanel;
                form.Text = formTitle ?? this.Title;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = startPoint ?? Control.MousePosition;
                form.ShowDialog(App.MainForm);
                result = (form.DialogResult == DialogResult.OK);
                if (result) this.AcceptedEditPanel(dataControlPanel);
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
            int s3 = 58;
            int w1 = 220;
            int w2 = 320;
            int w3 = 550;

            int y = y0;
            panel.AddCell(ControlType.TextBox, App.Messages.EditDataTitleText, nameof(Title), x1, y, w1); y += s2;
            panel.AddCell(ControlType.TextBox, App.Messages.EditDataDescriptionText, nameof(Description), x1, y, w1); y += s2;
            panel.AddCell(ControlType.MemoBox, App.Messages.EditDataToolTipText, nameof(ToolTipText), x2, y0, w2, s3);
            panel.AddCell(ControlType.FileBox, App.Messages.EditDataImageFileNameText, nameof(ImageFileName), x1, y, w3); y += s2;
            var colorControl = panel.AddCell(ControlType.ColorBox, App.Messages.EditDataBackColorText, nameof(BackColor), x1, y, w3);
            y = colorControl.Bottom;

            panel.Buttons = new DialogButtonType[] { DialogButtonType.Ok, DialogButtonType.Cancel };
            panel.BackColor = Color.AntiqueWhite;

            panel.DataObject = this;
            return panel;
        }
        /// <summary>
        /// Metoda je volaná poté, uživatel editoval data v panelu a potvrdil je OK.
        /// Metoda dostává tentýž panel <see cref="DataControlPanel"/>, který vytvořila metoda <see cref="BaseData.CreateEditPanel()"/>,
        /// a může si z panelu přečíst data nad rámec standardních hodnot, která jsou do datového objektu vepsána standardně.
        /// </summary>
        /// <param name="dataControl"></param>
        /// <returns></returns>
        protected virtual void AcceptedEditPanel(DataControlPanel dataControl) { }
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
            UserData = data;
        }
        private BaseData __Data;
        public override string MainTitle { get { return __Data.Title; } set { } }
        public override string Description { get { return __Data.Description; } set { } }
        public override string ToolTipText { get { return (!String.IsNullOrEmpty(__Data.ToolTipText) ? __Data.ToolTipText : __Data.Description); } set { } }
        public override Point Adress { get { return __Data.Adress; } set { } }
        public override string ImageName { get { return __Data.ImageFileName; } set { } }
        public override Color? BackColor { get { return __Data.BackColor; } set { } }
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
        #region Část Settings, která ukládá a načítá vlastní data o stránkách, grupách a aplikacích = ProgramPages; plus DefaultImagePath
        /// <summary>
        /// Sada všech stránek.
        /// Seznam stránek má vždy alespoň jednu stránku, obsahující jednu grupu a výchozí aplikace.
        /// <para/>
        /// Hodnotu lze setovat, to se provádí při kroku Undo/Redo.
        /// Setování vyvolá event <see cref="Changed"/>, ale nezapisuje se do UndoRedo containeru.
        /// </summary>
        [PersistingEnabled(false)]
        public PageSetData PageSet { get { return _GetPageSet(); } set { _PageSet = value; App.Settings.SetChanged("PageSet"); } }
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
        /// <summary>
        /// Defaultní (posledně použitý) adresář, kde uživatel aktivně vyhledal soubor s ikonou pomocí buttonu
        /// </summary>
        public string DefaultImagePath { get; set; }
        #endregion
    }
}

