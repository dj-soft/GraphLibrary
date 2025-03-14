using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

using XBars = DevExpress.XtraBars;
using XRibbon = DevExpress.XtraBars.Ribbon;
using XEditors = DevExpress.XtraEditors;
using XUtils = DevExpress.Utils;

using DjSoft.App.iCollect.Data;
using DjSoft.App.iCollect.Properties;
using DjSoft.App.iCollect.Application;
using DevExpress.XtraBars;

namespace DjSoft.App.iCollect.Components.Ribbon
{
    public class DjRibbonControl : XRibbon.RibbonControl
    {
        #region Inicializace
        public DjRibbonControl()
        {
            _InitializeSystemProperties();
            _InitializeBarManager();
            _InitializeCustomize();
            _InitializeEvents();
        }
        /// <summary>
        /// Nastaví základní systémové vlastnosti Ribbonu.
        /// </summary>
        private void _InitializeSystemProperties()
        {
            AllowKeyTips = true;
            ButtonGroupsLayout = XBars.ButtonGroupsLayout.TwoRows;
            RibbonStyle = XRibbon.RibbonControlStyle.Office2019;
            CommandLayout = XRibbon.CommandLayout.Simplified;
            DrawGroupCaptions = XUtils.DefaultBoolean.True;
            DrawGroupsBorderMode = XUtils.DefaultBoolean.True;
            GalleryAnimationLength = 300;
            GroupAnimationLength = 300;
            ItemAnimationLength = 300;
            ItemsVertAlign = XUtils.VertAlignment.Center;                      // Svislé zarovnání prvků, když mám 1 až 2 malé buttony v třířádkovém Ribbonu
            OptionsAnimation.PageCategoryShowAnimation = XUtils.DefaultBoolean.True;
            RibbonCaptionAlignment = XRibbon.RibbonCaptionAlignment.Center;
            SearchItemShortcut = new XBars.BarShortcut(System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F);
            ShowDisplayOptionsMenuButton = XUtils.DefaultBoolean.False;        //  True;
            ShowExpandCollapseButton = XUtils.DefaultBoolean.True;
            ShowMoreCommandsButton = XUtils.DefaultBoolean.True;
            ShowPageHeadersMode = XRibbon.ShowPageHeadersMode.Show;
            ShowSearchItem = true;
            
            OptionsExpandCollapseMenu.EnableExpandCollapseMenu = XUtils.DefaultBoolean.False;
            OptionsCustomizationForm.AllowToolbarCustomization = false;
            RibbonDisplayOptionsMenu.AllowRibbonQATMenu = false;

            ShowApplicationButton = XUtils.DefaultBoolean.False;
            ToolTipController = DjToolTipController.DjDefaultController;
            ToolbarLocation = XRibbon.RibbonQuickAccessToolbarLocation.Hidden;

            Margin = new System.Windows.Forms.Padding(2);
            MdiMergeStyle = XRibbon.RibbonMdiMergeStyle.Default;

            AllowMinimizeRibbon = false;    // Povolit minimalizaci Ribbonu? Pak ale nejde vrátit :-(
            AllowCustomization = false;     // Hodnota true povoluje (na pravé myši) otevřít okno Customizace Ribbonu, a to v Greenu nepodporujeme
            ShowQatLocationSelector = true; // Hodnota true povoluje změnu umístění Ribbonu

            AllowGlyphSkinning = false;     // Nikdy ne true!
            ShowItemCaptionsInQAT = true;

            RemoveItemsShortcuts();
            Visible = true;
        }
        /// <summary>
        /// Nastaví standardní vlastnosti do BarManageru
        /// </summary>
        private void _InitializeBarManager()
        {
            XRibbon.RibbonBarManager barManager = this.Manager;
            barManager.AllowMoveBarOnToolbar = true;
            barManager.AllowQuickCustomization = true;
            barManager.AllowShowToolbarsPopup = true;
            barManager.PopupMenuAlignment = XBars.PopupMenuAlignment.Left;
            barManager.PopupShowMode = XBars.PopupShowMode.Classic;
            barManager.ShowScreenTipsInMenus = true;
            barManager.ShowScreenTipsInToolbars = true;
            barManager.ShowShortcutInScreenTips = true;
            barManager.ToolTipAnchor = DevExpress.XtraBars.ToolTipAnchor.Cursor;

            barManager.UseAltKeyForMenu = true;
        }
        /// <summary>
        /// Nastaví další customizaci Ribbonu
        /// </summary>
        private void _InitializeCustomize()
        {
            this.EmptyAreaImageOptions.Image = Properties.Resources.gpe_tetris_48;
        }
        /// <summary>
        /// Nastavení evntů
        /// </summary>
        private void _InitializeEvents()
        {
            this.ItemClick += _DjRibbonControl_ItemClick;
        }

        private void _DjRibbonControl_ItemClick(object sender, XBars.ItemClickEventArgs e)
        {
            
        }

        /// <summary>
        /// Projde všechny Itemy v tomto Ribbonu, a pokud mají klávesovou zkratku, pak jí odebere (deaktivuje).<br/>
        /// Účelem je uvolnit veškeré klávesové zkratky (tedy implicitní DevExpress) pro použití z aplikace (tedy Nephrite).<br/>
        /// Ukázkou je Ctrl+F, která je defaultně přiřazena pro RibbonSearch, ale obecně ji chceme použít pro Fulltext.<br/>
        /// Volá se při inicializaci Ribbonu.
        /// </summary>
        protected void RemoveItemsShortcuts()
        {
            this.Items.Clear();

            foreach (XBars.BarItem item in this.Items)
            {
                item.Visibility = XBars.BarItemVisibility.Never;
                if (item.ItemShortcut != null && item.ItemShortcut.IsExist)
                    item.ItemShortcut = null;
            }
        }
        #endregion
        #region Tvorba prvků BarItem++ Ribbonu
        public static IDjRibbonItem CreateBarItem(DjRibbonItemType itemType, string name, string text, string toolTipTitle, string toolTipText, Image image, 
            XRibbon.RibbonItemStyles? style = null, Action<IDjRibbonItem> initializer = null)
        {
            IDjRibbonItem item = null;
            switch (itemType)
            {
                case DjRibbonItemType.Label:
                    var label = new DjRibbonLabel();
                    applyBarItemParams(label);
                    item = label;
                    break;

                case DjRibbonItemType.Button:
                    var button = new DjRibbonButton();
                    applyBarItemParams(button);
                    button.ButtonStyle = XBars.BarButtonStyle.Default;
                    item = button;
                    break;

                case DjRibbonItemType.Menu:
                    var menu = new DjRibbonMenuButton();
                    applyBarItemParams(menu);
                    item = menu;
                    break;

                case DjRibbonItemType.SkinDropDownButton:
                    var skinDropDownButton = new DjRibbonSkinDropDownButton();
                    applyBarItemParams(skinDropDownButton);
                    item = skinDropDownButton;
                    break;

                case DjRibbonItemType.SkinPaletteDropDownButton:
                    var skinPaletteDropDownButton = new DjRibbonSkinPaletteDropDownButton();
                    applyBarItemParams(skinPaletteDropDownButton);
                    item = skinPaletteDropDownButton;
                    break;

            }

            if (initializer != null && item != null)
                initializer(item);
            return item;


            void applyBarItemParams(XBars.BarItem barItem)
            {
                barItem.Name = name;
                barItem.Caption = text;
                barItem.RibbonStyle = style ?? XRibbon.RibbonItemStyles.Large;
                barItem.ImageOptions.Image = image;
                barItem.SuperTip = DjSuperToolTip.Create(toolTipTitle, toolTipText, text);
            }
        }
        #endregion
        public DjRibbonPage AddPage(string name, string text)
        {
            DjRibbonPage page = new DjRibbonPage() { Name = name, Text = text };
            this.Pages.Add(page);
            page.DjRibbon = this;
            return page;
        }
    }
    public class DjStatusControl : XRibbon.RibbonStatusBar
    {
        public DjStatusControl() 
        {
            this.Visible = true;
            this.Dock = System.Windows.Forms.DockStyle.Bottom;
        }
    }
    public class DjRibbonPage : XRibbon.RibbonPage
    {
        public DjRibbonGroup AddGroup(string name, string text)
        {
            DjRibbonGroup group = new DjRibbonGroup() { Name = name, Text = text };
            this.Groups.Add(group);
            return group;
        }
        public DjRibbonControl DjRibbon { get; set; }
    }
    public class DjRibbonGroup : XRibbon.RibbonPageGroup
    {
        public IDjRibbonItem AddItem(DjRibbonItemType itemType)
        {
            return AddItem(itemType, null, null, null, null, null);
        }
        public IDjRibbonItem AddItem(DjRibbonItemType itemType, string name, string text, string toolTipTitle, string toolTipText, Image image,
            XRibbon.RibbonItemStyles? style = null, Action<IDjRibbonItem> initializer = null)
        {
            var iItem = DjRibbonControl.CreateBarItem(itemType, name, text, toolTipTitle, toolTipText, image, style, initializer);
            if (iItem is BarItem barItem)
                this.ItemLinks.Add(barItem);
            return iItem;
        }
        public DjRibbonPage DjPage { get; set; }
        public DjRibbonControl DjRibbon { get { return this.DjPage?.DjRibbon; } }

    }
    #region Konkrétní prvky Ribbonu (Buttony atd)

    public class DjRibbonLabel : XBars.BarStaticItem, IDjRibbonItem
    {
        public DjRibbonLabel()
        { }
        DjRibbonItemType IDjRibbonItem.ItemType { get { return DjRibbonItemType.Label; } }
        string IDjRibbonItem.Command { get { return __Command; } set { __Command = value; } } private string __Command;
        object IDjRibbonItem.Data { get { return __Data; } set { __Data = value; } } private object __Data;
        XRibbon.RibbonItemStyles IDjRibbonItem.RibbonStyle { get { return this.RibbonStyle; } set { this.RibbonStyle = value; } }
        XBars.BarItemVisibility IDjRibbonItem.Visibility { get { return this.Visibility; } set { this.Visibility = value; } }
        bool IDjRibbonItem.Visible { get { return this.Visibility != XBars.BarItemVisibility.Never; } set { this.Visibility = (value ? XBars.BarItemVisibility.Always : XBars.BarItemVisibility.Never); } }
        bool IDjRibbonItem.Enabled { get { return this.Enabled; } set { this.Enabled = value; } }
    }
    public class DjRibbonButton : XBars.BarButtonItem, IDjRibbonItem
    {
        public DjRibbonButton()
        { }
        DjRibbonItemType IDjRibbonItem.ItemType { get { return DjRibbonItemType.Button; } }
        string IDjRibbonItem.Command { get { return __Command; } set { __Command = value; } } private string __Command;
        object IDjRibbonItem.Data { get { return __Data; } set { __Data = value; } } private object __Data;
        XRibbon.RibbonItemStyles IDjRibbonItem.RibbonStyle { get { return this.RibbonStyle; } set { this.RibbonStyle = value; } }
        XBars.BarItemVisibility IDjRibbonItem.Visibility { get { return this.Visibility; } set { this.Visibility = value; } }
        bool IDjRibbonItem.Visible { get { return this.Visibility != XBars.BarItemVisibility.Never; } set { this.Visibility = (value ? XBars.BarItemVisibility.Always : XBars.BarItemVisibility.Never); } }
        bool IDjRibbonItem.Enabled { get { return this.Enabled; } set { this.Enabled = value; } }
    }
    public class DjRibbonMenuButton : XBars.BarSubItem, IDjRibbonItem
    {
        public DjRibbonMenuButton()
        { }
        DjRibbonItemType IDjRibbonItem.ItemType { get { return DjRibbonItemType.Label; } }
        string IDjRibbonItem.Command { get { return __Command; } set { __Command = value; } } private string __Command;
        object IDjRibbonItem.Data { get { return __Data; } set { __Data = value; } } private object __Data;
        XRibbon.RibbonItemStyles IDjRibbonItem.RibbonStyle { get { return this.RibbonStyle; } set { this.RibbonStyle = value; } }
        XBars.BarItemVisibility IDjRibbonItem.Visibility { get { return this.Visibility; } set { this.Visibility = value; } }
        bool IDjRibbonItem.Visible { get { return this.Visibility != XBars.BarItemVisibility.Never; } set { this.Visibility = (value ? XBars.BarItemVisibility.Always : XBars.BarItemVisibility.Never); } }
        bool IDjRibbonItem.Enabled { get { return this.Enabled; } set { this.Enabled = value; } }
    }
    public class DjRibbonSkinDropDownButton : XBars.SkinDropDownButtonItem, IDjRibbonItem
    {
        public DjRibbonSkinDropDownButton()
        {
            this.PaintStyle = XBars.BarItemPaintStyle.CaptionGlyph;
            this.RibbonStyle = XRibbon.RibbonItemStyles.Large;
        }
        DjRibbonItemType IDjRibbonItem.ItemType { get { return DjRibbonItemType.SkinDropDownButton; } }
        string IDjRibbonItem.Command { get { return __Command; } set { __Command = value; } } private string __Command;
        object IDjRibbonItem.Data { get { return __Data; } set { __Data = value; } } private object __Data;
        XRibbon.RibbonItemStyles IDjRibbonItem.RibbonStyle { get { return this.RibbonStyle; } set { this.RibbonStyle = value; } }
        XBars.BarItemVisibility IDjRibbonItem.Visibility { get { return this.Visibility; } set { this.Visibility = value; } }
        bool IDjRibbonItem.Visible { get { return this.Visibility != XBars.BarItemVisibility.Never; } set { this.Visibility = (value ? XBars.BarItemVisibility.Always : XBars.BarItemVisibility.Never); } }
        bool IDjRibbonItem.Enabled { get { return this.Enabled; } set { this.Enabled = value; } }
    }
    public class DjRibbonSkinPaletteDropDownButton : XBars.SkinPaletteDropDownButtonItem, IDjRibbonItem
    {
        public DjRibbonSkinPaletteDropDownButton()
        {
            this.PaintStyle = XBars.BarItemPaintStyle.CaptionGlyph;
            this.RibbonStyle = XRibbon.RibbonItemStyles.Large;
        }
        DjRibbonItemType IDjRibbonItem.ItemType { get { return DjRibbonItemType.SkinPaletteDropDownButton; } }
        string IDjRibbonItem.Command { get { return __Command; } set { __Command = value; } } private string __Command;
        object IDjRibbonItem.Data { get { return __Data; } set { __Data = value; } } private object __Data;
        XRibbon.RibbonItemStyles IDjRibbonItem.RibbonStyle { get { return this.RibbonStyle; } set { this.RibbonStyle = value; } }
        XBars.BarItemVisibility IDjRibbonItem.Visibility { get { return this.Visibility; } set { this.Visibility = value; } }
        bool IDjRibbonItem.Visible { get { return this.Visibility != XBars.BarItemVisibility.Never; } set { this.Visibility = (value ? XBars.BarItemVisibility.Always : XBars.BarItemVisibility.Never); } }
        bool IDjRibbonItem.Enabled { get { return this.Enabled; } set { this.Enabled = value; } }
    }
    public interface IDjRibbonItem
    {
        DjRibbonItemType ItemType { get; }
        string Command { get; set; }
        object Data { get; set; }
        XRibbon.RibbonItemStyles RibbonStyle { get; set; }
        bool Visible { get; set; }
        XBars.BarItemVisibility Visibility { get; set; }
        bool Enabled { get; set; }
    }
    public enum DjRibbonItemType
    {
        None,
        Label,
        Button,
        Menu,
        
        SkinDropDownButton,
        SkinPaletteDropDownButton
    }
    #endregion
    #region class DjSuperToolTip : ToolTip
    /// <summary>
    /// ToolTip pro Ribbon atd
    /// </summary>
    public class DjSuperToolTip : XUtils.SuperToolTip
    {
        internal static DjSuperToolTip Create(string toolTipTitle, string toolTipText, string itemCaption = null)
        {
            if (String.IsNullOrEmpty(toolTipTitle) && String.IsNullOrEmpty(toolTipText)) return null;
            if (String.IsNullOrEmpty(toolTipTitle) && !String.IsNullOrEmpty(itemCaption)) toolTipTitle = itemCaption;
            return new DjSuperToolTip(toolTipTitle, toolTipText);
        }
        private DjSuperToolTip(string toolTipTitle, string toolTipText)
        {
            bool hasTitle = !String.IsNullOrEmpty(toolTipTitle);
            bool hasText = !String.IsNullOrEmpty(toolTipText);
            this.SetController(DjToolTipController.DefaultController);

            this.Title = toolTipTitle;
            this.Text = toolTipText;

            if (hasTitle)
            {
                var itemTitle = new XUtils.ToolTipTitleItem() { Text = toolTipTitle };
                itemTitle.ImageOptions.Image = Resources.arrow_right_2_16;
                this.Items.Add(itemTitle);
            }
            if (hasTitle && hasText)
                this.Items.Add(new XUtils.ToolTipSeparatorItem());

            if (hasText)
            {
                var itemText = new XUtils.ToolTipItem() { Text = toolTipText };
                // itemText.ImageOptions.Image = Resources.gpe_tetris_48;
                this.Items.Add(itemText);
            }
        }
        public string Title { get; private set; }
        public string Text { get; private set; }
        public static bool IsEquals(DjSuperToolTip a, DjSuperToolTip b)
        {
            bool an = a is null;
            bool bn = b is null;
            if (an && bn) return true;
            if (an || bn) return false;
            return String.Equals(a.Title, b.Title) && String.Equals(a.Text, b.Text);
        }
    }
    #endregion
    #region class DjToolTipController : ToolTipController s přidanou hodnotou
    /// <summary>
    /// ToolTipController s přidanou hodnotou
    /// </summary>
    public class DjToolTipController : XUtils.ToolTipController
    {
        #region Konstruktor + Default instance + Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolTipAnchor">Ukotvení ToolTipu se odvozuje od ...</param>
        /// <param name="toolTipLocation">Pozice ToolTipu je ... od ukotvení</param>
        public DjToolTipController(XUtils.ToolTipAnchor toolTipAnchor = XUtils.ToolTipAnchor.Object, XUtils.ToolTipLocation toolTipLocation = XUtils.ToolTipLocation.RightBottom)
            : base()
        {
            _InitializeStopWatch();
            _SetDefaultSettings(toolTipAnchor, toolTipLocation);
            _InitializeEvents();
        }
        /// <summary>
        /// Defaultní nastavení
        /// </summary>
        /// <param name="toolTipAnchor">Ukotvení ToolTipu se odvozuje od ...</param>
        /// <param name="toolTipLocation">Pozice ToolTipu je ... od ukotvení</param>
        private void _SetDefaultSettings(XUtils.ToolTipAnchor toolTipAnchor, XUtils.ToolTipLocation toolTipLocation)
        {
            Active = true;
            InitialDelay = 1000;
            ReshowDelay = 500;
            AutoPopDelay = 10000;
            SlowMouseMovePps = DEFAULT_SILENT_PIXEL_PER_SECONDS;
            AutoHideAdaptive = true;
            KeepWhileHovered = false;
            Rounded = true;
            RoundRadius = 20;
            ShowShadow = true;
            ToolTipAnchor = toolTipAnchor;
            ToolTipLocation = toolTipLocation;
            ToolTipStyle = XUtils.ToolTipStyle.Windows7;
            ToolTipType = XUtils.ToolTipType.SuperTip;          // Standard   Flyout   SuperTip;
            ToolTipIndent = 20;
            IconSize = XUtils.ToolTipIconSize.Large;
            CloseOnClick = XUtils.DefaultBoolean.True;
            ShowBeak = true;                                              // Callout beaks are not supported for SuperToolTip objects.
            CloseOnClick = XUtils.DefaultBoolean.True;

            Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;

            __Clients = new List<ClientInfo>();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _ClientsDispose();
            base.Dispose(disposing);
        }
        #endregion
        #region Public
        /// <summary>
        /// Společná instance defaultního controlleru
        /// </summary>
        public static DjToolTipController DjDefaultController
        {
            get
            {
                if (__DjDefaultController is null)
                    __DjDefaultController = new DjToolTipController();
                return __DjDefaultController;
            }
        }
        private static DjToolTipController __DjDefaultController;
        /// <summary>
        /// Počet milisekund mezi zastavením myši a rozsvícením ToolTipu.
        /// Platí pro první tooltip po najetí myši na celý Control. 
        /// Pokud se pohybujeme nad jedním Controlem a měníme Tooltipy (pro různé prvky na jednom Controlu) = poté, kdy už byl první ToolTip rozsvícen a zhasnut,
        /// pak pro druhý a další ToolTipy platí čas <see cref="HoverNextMiliseconds"/>.<br/>
        /// 0 = zobrazit ihned = stále.<br/>
        /// Má rozsah 0 až 1 minuta.<br/>
        /// Odpovídá <see cref="ToolTipController.InitialDelay"/>.
        /// </summary>
        public int HoverFirstMiliseconds
        {
            get
            {
                var ms = InitialDelay;
                return (ms < 0 ? 0 : (ms > 60000 ? 60000 : ms));
            }
            set { InitialDelay = value; }
        }
        /// <summary>
        /// Počet milisekund mezi zastavením myši a rozsvícením ToolTipu.
        /// Platí pro druhý a další ToolTip od dalšího prvku v rámci stejného Controlu = pokud se myš drží nad jedním Controlem. Jakmile z Controlu odejde a poté se na něj vrátí, platí interval <see cref="HoverFirstMiliseconds"/>.<br/>
        /// 0 = zobrazit ihned = stále.<br/>
        /// Má rozsah 0 až 1 minuta.<br/>
        /// Odpovídá <see cref="ToolTipController.InitialDelay"/>.
        /// </summary>
        public int HoverNextMiliseconds
        {
            get
            {
                var ms = ReshowDelay;
                return (ms < 0 ? 0 : (ms > 60000 ? 60000 : ms));
            }
            set { ReshowDelay = value; }
        }
        /// <summary>
        /// Počet milisekund mezi rozsvícením ToolTipu a jeho automatickým zhasnutím.
        /// 0 = nezhasínat nikdy.<br/>
        /// Má rozsah 0 až 10 minut.<br/>
        /// Odpovídá <see cref="ToolTipController.AutoPopDelay"/>.
        /// </summary>
        public int AutoHideMiliseconds
        {
            get
            {
                var ms = AutoPopDelay;
                return (ms < 0 ? 0 : (ms > 600000 ? 600000 : ms));
            }
            set { AutoPopDelay = value; }
        }
        /// <summary>
        /// Čas zobrazení ToolTipu <see cref="AutoHideMiliseconds"/> má být upraven podle délky textu ToolTipu.<br/>
        /// Pokud je false, pak čas <see cref="AutoHideMiliseconds"/> je použit jako konstanta vždy.<br/>
        /// Pokud je true, pak <see cref="AutoHideMiliseconds"/> je dolní hodnota pro texty do 120 znaků, pro texty delší je čas navyšován podle délky textu a ž na 5-ti násobek tohoto času.
        /// </summary>
        public bool AutoHideAdaptive { get; set; }
        /// <summary>
        /// Rychlost myši v pixelech za sekundu, pod kterou se myš považuje za "stojící" a umožní tak rozsvícení ToolTipu i při pomalém pohybu.
        /// Výchozí hodnota je 120 px/sec. Lze nastavit hodnotu 20 - 10000.
        /// Menší hodnota = myš musí skoro stát aby se rozsvítil ToolTip.
        /// Větší hodnota = ToolTip se rozsvítí i za pohybu.
        /// </summary>
        public double SlowMouseMovePps
        {
            get { return __SlowMouseMovePps; }
            set { __SlowMouseMovePps = (value < 20d ? 20d : (value > 10000d ? 10000d : value)); }
        }
        private double __SlowMouseMovePps;

        /// <summary>
        /// Vzdálenost mezi ukazatelem myši a ToolTipem v pixelech. Výchozí je 20. Platné hodnoty jsou 0 - 64 px.
        /// </summary>
        public int ToolTipIndent
        {
            get { return __ToolTipIndent; }
            set
            {
                __ToolTipIndent = (value < 0 ? 0 : (value > 64 ? 64 : value));
            }
        }
        private int __ToolTipIndent;
        #endregion
        #region Eventy ToolTipu
        private void _InitializeEvents()
        {
            this.BeforeShow += _BeforeShow;
        }
        /// <summary>
        /// Před zobrazením ToolTipu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BeforeShow(object sender, XUtils.ToolTipControllerShowEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.ToolTip) && __ActiveClientInfo != null && __ActiveClientInfo.HasContentForSuperTip())
            {
                // Pokud se aktuálně pohybuji nad klientem, který poskytuje specifický ToolTip a má pro něj data (obsah), pak defaultní Tooltip zruším:
                //  => případ DxGridu, který do ToolTipu posílá titulek ColumnHeaderu, když se mu nevejde zobrazit v celé šíři sloupce:
                e.ToolTip = "";
            }
        }
        #endregion
        #region Clients - klientské Controly: evidence, eventhandlery, vyhledání, předání ke konkrétní práci, dispose
        /// <summary>
        /// Přidá klienta.
        /// Pokud klient implementuje <see cref="IDxToolTipClient"/>, 
        /// pak Controller ve vhodném okamžiku volá metodu pro získání ToolTipu pro konkrétní pozici klientského controlu.
        /// </summary>
        /// <param name="client"></param>
        public void AddClient(Control client)
        {
            if (client is null) return;

            _AttachClient(client);
            _AddClient(client);
        }
        /// <summary>
        /// Zapojí eventhandlery do klienta
        /// </summary>
        /// <param name="client"></param>
        private void _AttachClient(Control client)
        {
            _DetachClient(client);
            client.MouseEnter += _Client_MouseEnter;
            client.MouseMove += _Client_MouseMove;
            client.MouseLeave += _Client_MouseLeave;
            client.MouseDown += _Client_MouseDown;
            client.Leave += _Client_Leave;
            client.Disposed += _Client_Disposed;
        }
        /// <summary>
        /// Odpojí eventhandlery z klienta
        /// </summary>
        /// <param name="client"></param>
        private void _DetachClient(Control client)
        {
            client.MouseEnter -= _Client_MouseEnter;
            client.MouseMove -= _Client_MouseMove;
            client.MouseLeave -= _Client_MouseLeave;
            client.MouseDown -= _Client_MouseDown;
            client.Leave -= _Client_Leave;
            client.Disposed -= _Client_Disposed;
        }
        /// <summary>
        /// Event MouseEnter z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseEnter(object sender, EventArgs e)
        {
            _ResetHoverInterval();
        }
        /// <summary>
        /// Event MouseMove z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None && this._TrySearchClient(sender, out var clientInfo))
                _ClientMouseMoveNone(clientInfo, e);
        }
        /// <summary>
        /// Event MouseDown z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseDown(object sender, MouseEventArgs e)
        {
            if (this._TrySearchClient(sender, out var clientInfo))
                _ClientMouseDown(clientInfo, e);
        }
        /// <summary>
        /// Event MouseLeave z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseLeave(object sender, EventArgs e)
        {
            // if (this._TrySearchClient(sender, out var clientInfo))
            //     _ClientMouseLeave(clientInfo, e);
            _ClientMouseLeave();
        }
        /// <summary>
        /// Event Leave z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_Leave(object sender, EventArgs e)
        {
            // if (this._TrySearchClient(sender, out var clientInfo))
            //    _ClientLeave(clientInfo, e);
            _ClientLeave();
        }
        /// <summary>
        /// Disposuje evidenci klientů
        /// </summary>
        private void _ClientsDispose()
        {
            if (__Clients is null) return;
            var clients = __Clients.ToArray();
            foreach (var client in clients)
            {
                if (client != null && client.Client != null)
                    _DetachClient(client.Client);
            }
            __Clients.Clear();
        }
        /// <summary>
        /// Po Dispose klienta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_Disposed(object sender, EventArgs e)
        {
            if (sender is Control client)
            {
                _DetachClient(client);
                _RemoveClient(client);
            }
        }
        /// <summary>
        /// Přidá daného klienta
        /// </summary>
        /// <param name="client"></param>
        private void _AddClient(Control client)
        {
            lock (__Clients)
            {
                if (!__Clients.Any(c => Object.ReferenceEquals(c, client)))
                {
                    int id = ++_LastClientId;
                    __Clients.Add(new ClientInfo(id, client));
                }
            }
        }
        /// <summary>
        /// Zkusí najít klienta odpovídající danému senderu (což by měl být Control)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="clientInfo"></param>
        /// <returns></returns>
        private bool _TrySearchClient(object sender, out ClientInfo clientInfo)
        {
            clientInfo = null;
            return (sender != null && sender is Control control && __Clients.TryGetFirst(ci => Object.ReferenceEquals(ci.Client, control), out clientInfo));
        }
        /// <summary>
        /// Odebere daného klienta
        /// </summary>
        /// <param name="client"></param>
        private void _RemoveClient(Control client)
        {
            lock (__Clients)
            {
                __Clients.RemoveWhere(ci => (Object.ReferenceEquals(ci, client)), ci => ci.Dispose());
            }
        }
        private List<ClientInfo> __Clients;
        /// <summary>
        /// ID posledně přidaného klienta
        /// </summary>
        private int _LastClientId = 0;
        #endregion
        #region class ClientInfo : Balíček informací o jednom klientovi (řízený Control)
        /// <summary>
        /// Balíček informací o jednom klientovi
        /// </summary>
        private class ClientInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="id"></param>
            /// <param name="client"></param>
            public ClientInfo(int id, Control client)
            {
                Id = id;
                Client = client;
                HasIDynamicClient = (client is IDxToolTipDynamicClient);
                IDynamicClient = (HasIDynamicClient ? client as IDxToolTipDynamicClient : null);
                HasIClient = !HasIDynamicClient && (client is IDxToolTipClient);
                IClient = (HasIClient ? client as IDxToolTipClient : null);
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string clientType = this.Client?.GetType().Name;
                string clientText = this.Client?.Text;
                return $"Client {clientType}: '{clientText}'{(HasIDynamicClient ? "; is IDxToolTipDynamicClient" : "")}{(HasIClient ? "; is IClient" : "")}.";
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                Client = null;
                HasIClient = false;
                IClient = null;
            }
            /// <summary>
            /// Jednoduché ID klienta
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// Control klienta
            /// </summary>
            public Control Client { get; private set; }
            /// <summary>
            /// Klient implementuje <see cref="IDxToolTipDynamicClient"/>?
            /// </summary>
            public bool HasIDynamicClient { get; private set; }
            /// <summary>
            /// Klient jako <see cref="IDxToolTipDynamicClient"/>
            /// </summary>
            public IDxToolTipDynamicClient IDynamicClient { get; private set; }
            /// <summary>
            /// Klient implementuje <see cref="IDxToolTipClient"/>?
            /// </summary>
            public bool HasIClient { get; private set; }
            /// <summary>
            /// Klient jako <see cref="IDxToolTipClient"/>
            /// </summary>
            public IDxToolTipClient IClient { get; private set; }
            /// <summary>
            /// Souřadnice myši aktuální
            /// </summary>
            public Point MouseLocation { get; set; }
            /// <summary>
            /// Souřadnice myši minulá, pro detekci malého pohybu
            /// </summary>
            public Point? LastMouseLocation { get; set; }
            /// <summary>
            /// Čas souřadnice <see cref="LastMouseLocation"/>, pro detekci malého pohybu
            /// </summary>
            public long? LastMouseLocationTime { get; set; }
            /// <summary>
            /// Posledně získaný SuperTip z tohoto klienta
            /// </summary>
            public DjSuperToolTip LastSuperTip { get; set; }
            /// <summary>
            /// Posledně použitý argument <see cref="DxToolTipDynamicPrepareArgs"/> pro tohoto klienta, po <see cref="Reset"/> je null
            /// </summary>
            protected DxToolTipDynamicPrepareArgs LastPrepareArgs { get; set; }
            /// <summary>
            /// Vyvolá danou akci v GUI threadu aktuálního controlu <see cref="Client"/>
            /// </summary>
            /// <param name="action"></param>
            public void InvokeGui(Action action)
            {
                var client = Client;
                if (client is null || client.IsDisposed || client.Disposing) return;
                if (client.IsHandleCreated && client.InvokeRequired) client.BeginInvoke(action, null);
                else action();
            }
            /// <summary>
            /// Zkusí získat SuperTip z Controlu, podle jeho typu.<br/>
            /// Metoda vrací:<br/>
            /// true = máme něco s ToolTipem udělat? Rozsvítit nebo zhasnout: podle obsahu out <paramref name="superTip"/>;<br/>
            /// false = nemáme nic dělat, ani zhasínat ani rozsvěcet.
            /// </summary>
            /// <param name="isMouseHover">Jsme voláni po zastavení myši (když chceme rozsvítit ToolTip) = true / nebo za pohybu, když ToolTip svítí (když bychom jej měli zhasnout nebo vyměnit) = false</param>
            /// <param name="isTipVisible">Vstup: ToolTip aktuálně svítí a myš se pohybuje? Má vliv na vyhodnocení</param>
            /// <param name="superTip">Výstup ToolTipu, má význam pouze pokud výstupní hodnota je true: pokud je <paramref name="superTip"/> == null, pak máme zhasnout; pokud není null, pak se má rozsvítit. Pokud by byl shodný jako dosud, pak výstupem je false.</param>
            /// <returns></returns>
            internal DxToolTipChangeType TryGetSuperTip(bool isMouseHover, bool isTipVisible, out DjSuperToolTip superTip)
            {
                superTip = null;
                if (this.HasIDynamicClient)
                {
                    DxToolTipDynamicPrepareArgs args = this.LastPrepareArgs;
                    if (args is null) args = new DxToolTipDynamicPrepareArgs();          // args používám opakovaně, jen ho naplním aktuálními daty
                    args.MouseLocation = this.MouseLocation;
                    args.IsMouseHover = isMouseHover;
                    args.IsTipVisible = isTipVisible;
                    args.DxSuperTip = this.LastSuperTip;
                    args.ToolTipChange = (this.LastSuperTip is null ? DxToolTipChangeType.NoToolTip : DxToolTipChangeType.SameAsLastToolTip);

                    this.IDynamicClient.PrepareSuperTipForPoint(args);
                    this.LastSuperTip = args.DxSuperTip;                                 // Uložím si data pro příští volání, nulovat se budou při resetu
                    this.LastPrepareArgs = args;

                    superTip = args.DxSuperTip;
                    return args.ToolTipChange;
                }

                if (this.HasIClient)
                {
                    // Výměna a uložení nového globálního ToolTipu ze statického klienta:
                    var oldSuperTip = this.LastSuperTip;
                    var newSuperTip = this.IClient?.SuperTip;
                    superTip = newSuperTip;
                    this.LastSuperTip = newSuperTip;

                    // Druh změny:
                    bool oldExists = (oldSuperTip != null);
                    bool newExists = (newSuperTip != null);
                    if (oldExists && newExists)
                    {
                        if (DjSuperToolTip.IsEquals(oldSuperTip, newSuperTip))
                            return DxToolTipChangeType.SameAsLastToolTip;
                        return DxToolTipChangeType.NewToolTip;
                    }
                    if (oldExists && !newExists)
                        return DxToolTipChangeType.NoToolTip;
                    if (!oldExists && newExists)
                        return DxToolTipChangeType.NewToolTip;
                    return DxToolTipChangeType.None;
                }

                return DxToolTipChangeType.None;
            }

            /// <summary>
            /// Má control obsah pro SuperTip? Stejný způsob získání obsahu pro Super tip jako v <see cref="TryGetSuperTip"/>, ale neukládájí se žádné stavy.<br/>
            /// Metoda vrací:<br/>
            /// true = mám nějaké data pro SuperToolTip
            /// false = nemám žádná data pro SuperToolTip.
            /// </summary>
            /// <returns></returns>
            internal bool HasContentForSuperTip()
            {//RMC 0071608 08.01.2024 BROWSE2e - začlenění 2
                if (this.HasIDynamicClient)
                {
                    DxToolTipDynamicPrepareArgs args = new DxToolTipDynamicPrepareArgs();
                    args.MouseLocation = this.MouseLocation;
                    args.ToolTipChange = DxToolTipChangeType.NoToolTip;
                    this.IDynamicClient.PrepareSuperTipForPoint(args);
                    return args.ToolTipChange != DxToolTipChangeType.NoToolTip;
                }
                if (this.HasIClient)
                {
                    // Výměna a uložení nového globálního ToolTipu ze statického klienta:
                    return this.IClient?.SuperTip != null;
                }

                return false;
            }

            /// <summary>
            /// Je voláno při zhasínání ToolTipu, ať už jsou důvody jakékoli
            /// </summary>
            internal void Reset()
            {
                LastSuperTip = null;
                LastPrepareArgs = null;
                ResetLastPoint();
            }
            /// <summary>
            /// Nuluje poslední pozici myši
            /// </summary>
            internal void ResetLastPoint()
            {
                LastMouseLocation = null;
                LastMouseLocationTime = null;
            }
        }
        #endregion
        #region Client ToolTip: nalezení klienta, určení jeho konkrétního jeho ToolTipu
        /// <summary>
        /// Myš se pohybuje bez stisknutého tlačítka nad daným klientem.
        /// Můžeme čekat na její zastavení a rozsvítit Tooltip, nebo jej rozsvěcet okamžitě; anebo když ToolTip svítí, tak jej zhasnout...
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <param name="e"></param>
        private void _ClientMouseMoveNone(ClientInfo clientInfo, MouseEventArgs e)
        {
            _ClientMouseMoveDetectClientChange(clientInfo, e);
            if (!IsHintShown)
                _ClientMouseMoveWaitShow();
            else
                _ClientMouseMoveWaitHide();
        }
        /// <summary>
        /// Kontroluje aktuálního klienta: pokud nějakého máme uloženého odminule, a nyní je klient pod myší jiný, a svítí nám ToolTip, tak jej zhasnu.
        /// Aktuálního klienta si uložím (do <see cref="__ActiveClientInfo"/>) a vložím do něj aktuální pozici myši.
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <param name="e"></param>
        private void _ClientMouseMoveDetectClientChange(ClientInfo clientInfo, MouseEventArgs e)
        {
            // Pokud se myš pohybuje nad jiným Controlem než dříve, musíme starý tooltip zhasnout a zahodit:
            if (__ActiveClientInfo != null && !Object.ReferenceEquals(__ActiveClientInfo, clientInfo))
            {
                _HideTip(true);
                _ResetHoverInterval();
            }

            // Uložíme si aktuálního klienta a do něj pozici myši:
            __ActiveClientInfo = clientInfo;
            __ActiveClientInfo.MouseLocation = e.Location;
        }
        /// <summary>
        /// ToolTip nesvítí a nad klientem se pohybuje myš: čekáme na její zastavení nebo zpomalení (podle <see cref="HoverCurrentMiliseconds"/>) 
        /// a pak zajistíme rozsvícení ToolTipu.
        /// </summary>
        private void _ClientMouseMoveWaitShow()
        {
            var hoverMiliseconds = HoverCurrentMiliseconds;

            // Pokud nám běží časovač __HoverTimerGuid, a známe poslední pozici a čas myši, a nynější pozice a čas odpovídá malé rychlosti pohybu myši,
            //  a pokud čas 'hoverMiliseconds' je kladný, tak nebudeme resetovat Timer = jako by se myš nepohnula, ale stála na místě...:
            if (hoverMiliseconds > 1 && __HoverTimerGuid.HasValue && _ClientMouseMoveIsSlow()) return;

            if (hoverMiliseconds > 1)
                // Máme TooTip rozsvítit až po nějaké době od zastavení myši:
                // Toto volání Timeru (s předaným Guid __HoverTimerGuid) zajistí, že budeme zavoláni (metoda _ClientActivateTip) až po zadané době od posledního "načasování budíka".
                // Průběžné pohyby myši v kratším čase provedou "přenastavení toho samého budíka" na nový čas:
                __HoverTimerGuid = WatchTimer.CallMeAfter(_ClientMouseHoverTimerShowTip, hoverMiliseconds, false, __HoverTimerGuid);
            else
                // 0 = máme dát ToolTip ihned?
                _ClientMouseHoverShowTip();                   // Aktuálně jsme v eventu MouseMove, tedy v GUI threadu...
        }
        /// <summary>
        /// Zkus aktivovat ToolTip, uplynul čas čekání od posledního pohybu myši <see cref="HoverCurrentMiliseconds"/>, 
        /// a nyní jsme volání z threadu na pozadí z třídy <see cref="WatchTimer"/>.
        /// </summary>
        private void _ClientMouseHoverTimerShowTip()
        {
            __HoverTimerGuid = null;
            __ActiveClientInfo?.InvokeGui(_ClientMouseHoverShowTip);
        }
        /// <summary>
        /// Zkus aktivovat ToolTip, uplynul čas čekání od posledního pohybu myši <see cref="HoverCurrentMiliseconds"/> a jsme invokování do GUI threadu,
        /// anebo se má ToolTip rozsvítit okamžitě.
        /// </summary>
        private void _ClientMouseHoverShowTip()
        {
            if (!_CanShowTipOverActiveClient())
            {
                _HideTip(true);
                _RaiseToolTipDebugTextChanged($"ClientMouseHoverShowTip(superTip): cancel - ActiveClient is not valid");
                return;
            }
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return;

            var changeType = clientInfo.TryGetSuperTip(true, __IsHintShown, out DjSuperToolTip superTip);
            switch (changeType)
            {   // Myš se zastavila, možná rozsvítíme ToolTip?
                case DxToolTipChangeType.NewToolTip:
                    _ShowDxSuperTip(superTip);
                    break;
                case DxToolTipChangeType.SameAsLastToolTip:
                    break;
                case DxToolTipChangeType.NoToolTip:
                    _HideTip(false);
                    break;
            }
        }
        /// <summary>
        /// ToolTip svítí a pohybujeme se nad klientem (=máme event MouseMove).
        /// Zhasneme ToolTip? Nebo jej vyměníme? Nebo jej necháme beze změny?
        /// </summary>
        private void _ClientMouseMoveWaitHide()
        {
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return;
            var changeType = clientInfo.TryGetSuperTip(false, __IsHintShown, out DjSuperToolTip superTip);
            switch (changeType)
            {   // ToolTip svítí, možná jej necháme, anebo jej zhasneme?
                case DxToolTipChangeType.NewToolTip:
                    _HideTip(true);                        // Skrýt ToolTip včetně resetu = zapomenout na aktuální pozici a prvek a tooltip, abychom po zhasnutí a po konci pohybu myši detekovali NewToolTip a nikoli SameAsLastToolTip...
                    _ClientMouseMoveWaitShow();            // Tady jsme v eventu MouseMove: nastartujeme HoverTimer a po jeho uplynutí vyhodnotíme od nuly pozici myši a prvek na této pozici.
                    break;
                case DxToolTipChangeType.SameAsLastToolTip:
                    break;
                case DxToolTipChangeType.NoToolTip:
                    _HideTip(true);
                    _ClientMouseMoveWaitShow();            // Tady jsme v eventu MouseMove, tak se sluší nastartovat HoverTimer
                    break;
            }
            clientInfo.ResetLastPoint();
        }

        private void _ClientMouseDown(ClientInfo clientInfo, MouseEventArgs e)
        {
            _HideTip(false);
            _ResetHoverInterval();
        }
        private void _ClientMouseLeave()
        {
            _RaiseToolTipDebugTextChanged($"ClientMouseLeave(): ActiveClient = {__ActiveClientInfo}");
            _ResetHoverInterval();
            _HideTip(true);
        }
        private void _ClientLeave()
        {
            _RaiseToolTipDebugTextChanged($"ClientLeave(): ActiveClient = {__ActiveClientInfo}");
            _ResetHoverInterval();
            _HideTip(true);
        }
        private ClientInfo __ActiveClientInfo;
        private Guid? __HoverTimerGuid;
        /// <summary>
        /// Metoda vrátí true, pokud nynější pozice myši v aktuálním klientu se neliší příliš mnoho od předchozí pozice v tomtéž klientu.
        /// Pokud vrátí true, je to stejné jako by myš stála na místě. Pokud vrátí false, pohybuje se docela rychle.
        /// Metoda si udržuje časoprostorové povědomí v properties klienta <see cref="ClientInfo.LastMouseLocation"/> a <see cref="ClientInfo.LastMouseLocationTime"/>.
        /// </summary>
        /// <returns></returns>
        private bool _ClientMouseMoveIsSlow()
        {
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return false;

            // Najdeme předchozí časoprostorové souřadnice, a načteme i aktuální:
            var lastPoint = clientInfo.LastMouseLocation;
            var lastTime = clientInfo.LastMouseLocationTime;
            var currentPoint = clientInfo.MouseLocation;
            var currentTime = __StopWatch.ElapsedTicks;
            // Aktuální uložíme do předchozích (nulují se až v ClientInfo.ResetLastPoint()):
            clientInfo.LastMouseLocation = currentPoint;
            clientInfo.LastMouseLocationTime = currentTime;

            // Nemáme předešlé souřadnice => nemůže sejednat o malý pohyb (jde asi o první událost MouseMove):
            if (!(lastPoint.HasValue && lastTime.HasValue)) return false;

            // Kolik pixelů za sekundu máme pohybu:
            int dx = lastPoint.Value.X - currentPoint.X;
            if (dx < 0) dx = -dx;
            int dy = lastPoint.Value.Y - currentPoint.Y;
            if (dy < 0) dy = -dy;
            double pixelDistance = (dx > dy ? dx : dy);                        // Kladná hodnota rozdílu souřadnic, ta větší ze směru X | Y;
            double seconds = ((double)(currentTime - lastTime.Value)) / __StopWatchFrequency;    // Čas v sekundách mezi minulým a současným měřením polohy myši
            if (pixelDistance <= 0d || seconds <= 0d) return true;             // Pokud jsme nedetekovali pohyb nebo čas, je to jako by se myš nepohnula.

            double pixelPerSeconds = pixelDistance / seconds;
            bool isSlowMotion = (pixelPerSeconds <= SlowMouseMovePps);

            if (isSlowMotion)
                _RaiseToolTipDebugTextChanged($"Is..slow..motion: {pixelPerSeconds:F3} pixel/seconds    <=   {SlowMouseMovePps}");
            else
                _RaiseToolTipDebugTextChanged($"IsFASTMotion: {pixelPerSeconds:F3} pixel/seconds    >   {SlowMouseMovePps}");

            return isSlowMotion;
        }
        /// <summary>
        /// Počet pixelů za sekundu v pohybu myši, který se ještě považuje za pomalý pohyb a neresetuje časovač odloženého startu ToolTipu, výchozí hodnota.
        /// </summary>
        private const double DEFAULT_SILENT_PIXEL_PER_SECONDS = 600d;
        /// <summary>
        /// Provede inicializaci hodin reálného času pro měření rychlosti pohybu myši
        /// </summary>
        private void _InitializeStopWatch()
        {
            __StopWatchFrequency = System.Diagnostics.Stopwatch.Frequency;
            __StopWatch = new System.Diagnostics.Stopwatch();
            __StopWatch.Start();
        }
        private System.Diagnostics.Stopwatch __StopWatch;
        private double __StopWatchFrequency;
        #endregion
        #region Fyzický ToolTip
        /// <summary>
        /// Skrýt ToolTip
        /// </summary>
        public void HideTip()
        {
            _HideTip(true);
        }
        /// <summary>
        /// Zobrazit ToolTip
        /// </summary>
        /// <param name="superTip"></param>
        private void _ShowDxSuperTip(DjSuperToolTip superTip)
        {
            if (superTip is null)
            {
                _RaiseToolTipDebugTextChanged($"ShowDxSuperTip(superTip): cancel - superTip is null");
                return;
            }

            if (!_CanShowTipOverActiveClient())
            {
                _RaiseToolTipDebugTextChanged($"ShowDxSuperTip(superTip): cancel - ActiveClient is not valid");
                return;
            }

            var args = new XUtils.ToolTipControllerShowEventArgs();
            args.ToolTipType = XUtils.ToolTipType.SuperTip;
            args.SuperTip = superTip;
            args.ToolTipAnchor = this.ToolTipAnchor;
            args.ToolTipLocation = this.ToolTipLocation;
            args.ToolTipIndent = this.ToolTipIndent;
            args.Show = true;
            args.ShowBeak = this.ShowBeak;

            _RaiseToolTipDebugTextChanged($"ShowDxSuperTip(superTip): ShowHint");
            this.ShowHint(args);

            __IsHintShown = true;
            __ActiveClientInfoHasShowAnyToolTip = true;

            _StartHideTimer(superTip);
        }
        /// <summary>
        /// Vrátí true, pokud je možno zobrazit klientský Tooltip = máme platnou instanci aktuálního klienta, a myš se nachází v jeho prostoru.
        /// </summary>
        /// <returns></returns>
        private bool _CanShowTipOverActiveClient()
        {
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return false;
            var mousePoint = Control.MousePosition;
            var screenBounds = clientInfo.Client.RectangleToScreen(clientInfo.Client.ClientRectangle);
            return screenBounds.Contains(mousePoint);
        }
        /// <summary>
        /// Skrýt ToolTip, a pokud je dán parametr <paramref name="reset"/> = true, pak proveď i Reset informace o klientu.
        /// </summary>
        /// <param name="reset"></param>
        private void _HideTip(bool reset)
        {
            WatchTimer.RemoveRef(ref __HoverTimerGuid);
            WatchTimer.RemoveRef(ref __HideTimerGuid);
            if (reset)
            {   // Když myš opouští prostor daného klienta, tak na něj mohu s klidem zapomenout:
                if (__ActiveClientInfo != null) __ActiveClientInfo.Reset();
                __ActiveClientInfo = null;
            }
            if (__IsHintShown)
            {
                this.HideHint();
                __IsHintShown = false;
            }
        }
        /// <summary>
        /// ToolTip byl zobrazen a měl by nyní svítit?
        /// </summary>
        public bool IsHintShown { get { return __IsHintShown; } }
        private bool __IsHintShown;
        /// <summary>
        /// Počet milisekund, za které se rozsvítí ToolTip od zastavení myši, v aktuálním stavu.
        /// Obsahuje <see cref="HoverFirstMiliseconds"/> nebo <see cref="HoverNextMiliseconds"/>, podle toho, zda pro aktuální Control (Client) už byl / nebyl rozsvícen ToolTip.
        /// </summary>
        protected int HoverCurrentMiliseconds { get { return (!__ActiveClientInfoHasShowAnyToolTip ? HoverFirstMiliseconds : HoverNextMiliseconds); } }
        /// <summary>
        /// Resetuje příznak prvního / následujícího ToolTipu, provádí se tehdy, když chceme mít příští interval <see cref="HoverCurrentMiliseconds"/> jako počáteční = <see cref="HoverFirstMiliseconds"/>.
        /// </summary>
        private void _ResetHoverInterval()
        {
            __ActiveClientInfoHasShowAnyToolTip = false;
        }
        /// <summary>
        /// Příznak, že aktuální klient neměl (false) / měl (true) už zobrazen nějaký ToolTip.
        /// Nastaví se na true v <see cref="_ShowDxSuperTip(DjSuperToolTip)"/> a zůstává true i po zhasnutí ToolTipu,
        /// nuluje se na false při opuštění klienta (nebo MouseDown) v metodě <see cref="_ResetHoverInterval"/>,
        /// používá se v <see cref="HoverCurrentMiliseconds"/> pro určení času pro aktuální interval čekání na MouseHover, 
        /// tam se zvolí buď <see cref="HoverFirstMiliseconds"/> nebo <see cref="HoverNextMiliseconds"/>.
        /// </summary>
        private bool __ActiveClientInfoHasShowAnyToolTip;
        /// <summary>
        /// Nastartuj časovač pro automatické skrytí ToolTipu
        /// </summary>
        private void _StartHideTimer(DjSuperToolTip superTip)
        {
            var autoHideMiliseconds = this.AutoHideMiliseconds;
            if (autoHideMiliseconds <= 0) return;

            if (AutoHideAdaptive)
                _ModifyAutoHideAdaptive(ref autoHideMiliseconds, superTip);

            __HideTimerGuid = WatchTimer.CallMeAfter(_AutoHideToolTipTimer, autoHideMiliseconds, false, __HideTimerGuid);
        }
        /// <summary>
        /// Upraví čas <paramref name="autoHideMiliseconds"/> podle délky textu v dodaném ToolTipu
        /// </summary>
        /// <param name="autoHideMiliseconds"></param>
        /// <param name="superTip"></param>
        private void _ModifyAutoHideAdaptive(ref int autoHideMiliseconds, DjSuperToolTip superTip)
        {
            if (superTip is null) return;
            int textLength = (superTip.Text ?? "").Trim().Length;
            if (textLength <= 120) return;
            float ratio = ((float)textLength / 120f);
            if (ratio > 5f) ratio = 5f;
            autoHideMiliseconds = (int)(ratio * (float)autoHideMiliseconds);
        }
        /// <summary>
        /// Uplynul patřičný čas pro schování ToolTipu
        /// </summary>
        private void _AutoHideToolTipTimer()
        {
            var clienInfo = __ActiveClientInfo;
            if (clienInfo != null)
                clienInfo.InvokeGui(_AutoHideToolTip);
            else
                _AutoHideToolTip();
        }
        /// <summary>
        /// Provede automatické schování ToolTipu po čase daním Timerem <see cref="AutoHideMiliseconds"/>.
        /// </summary>
        private void _AutoHideToolTip()
        {
            WatchTimer.RemoveRef(ref __HideTimerGuid);
            _HideTip(false);           // false = bez resetu => budeme si pamatovat, nad kterým prvekm stojím. Pak se pro ten prvek neprovede reaktivace ToolTipu, protože z klienta přijde ChangeType = SameAsLast, a to tooltip nerozsvítíme...
        }
        /// <summary>
        /// ID Timeru, který řídí časové skrytí ToolTipu
        /// </summary>
        private Guid? __HideTimerGuid;
        #endregion
        #region Eventy Tooltipu
        /// <summary>
        /// Vyvolej události <see cref="ToolTipDebugTextChanged"/>;
        /// </summary>
        /// <param name="eventName"></param>
        private void _RaiseToolTipDebugTextChanged(string eventName)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DxToolTipArgs args = new DxToolTipArgs(eventName);
                OnToolTipDebugTextChanged(args);
                ToolTipDebugTextChanged?.Invoke(this, args);
            }
        }
        /// <summary>
        /// ToolTip má událost.
        /// Používá se pouze pro výpisy debugovacích informací do logu společného s klientským controlem. Běžně netřeba.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnToolTipDebugTextChanged(DxToolTipArgs args) { }
        /// <summary>
        /// ToolTip má událost.
        /// Používá se pouze pro výpisy debugovacích informací do logu společného s klientským controlem. Běžně netřeba.
        /// </summary>
        public event DxToolTipHandler ToolTipDebugTextChanged;
        #endregion
    }
    /// <summary>
    /// Data o události v ToolTipu
    /// </summary>
    public class DxToolTipArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="eventName"></param>
        public DxToolTipArgs(string eventName)
        {
            EventName = eventName;
        }
        /// <summary>
        /// Jméno události
        /// </summary>
        public string EventName { get; private set; }
    }
    /// <summary>
    /// Eventhandler události v ToolTipu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxToolTipHandler(object sender, DxToolTipArgs args);
    /// <summary>
    /// Interface pro Control, který zobrazuje jeden konstantní ToolTip pro celou svoji plochu
    /// </summary>
    public interface IDxToolTipClient
    {
        /// <summary>
        /// ToolTip pro Control
        /// </summary>
        DjSuperToolTip SuperTip { get; }
    }
    /// <summary>
    /// Interface pro Control, který chce určovat ToolTip dynamicky podle konkrétní pozice myši na controlu
    /// </summary>
    public interface IDxToolTipDynamicClient
    {
        /// <summary>
        /// Zde control určí, jaký ToolTip má být pro danou pozici myši zobrazen
        /// </summary>
        /// <param name="args"></param>
        void PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs args);
    }
    /// <summary>
    /// Data pro událost <see cref="IDxToolTipDynamicClient.PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs)"/>
    /// </summary>
    public class DxToolTipDynamicPrepareArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxToolTipDynamicPrepareArgs()
        {
            ToolTipChange = DxToolTipChangeType.None;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mouseLocation"></param>
        /// <param name="dxSuperTip"></param>
        public DxToolTipDynamicPrepareArgs(Point mouseLocation, DjSuperToolTip dxSuperTip)
        {
            MouseLocation = mouseLocation;
            DxSuperTip = dxSuperTip;
            ToolTipChange = (dxSuperTip is null ? DxToolTipChangeType.NoToolTip : DxToolTipChangeType.SameAsLastToolTip);
        }
        /// <summary>
        /// Aktuální souřadnice myši v kordinátech kontrolu, pochází z eventu MouseMove.
        /// </summary>
        public Point MouseLocation { get; set; }
        /// <summary>
        /// Jsme voláni po zastavení myši na prvku (Hover = true) anebo při jejím pohybu (false)
        /// </summary>
        public bool IsMouseHover { get; set; }
        /// <summary>
        /// ToolTip je nyní viditelný?
        /// </summary>
        public bool IsTipVisible { get; set; }
        /// <summary>
        /// SuperTip: na vstupu je ten, který byl vygenerován nebo odsouhlasen posledně, 
        /// na výstupu z metody <see cref="IDxToolTipDynamicClient.PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs)"/> může být nově připravený.
        /// </summary>
        public DjSuperToolTip DxSuperTip { get; set; }
        /// <summary>
        /// Typ akce
        /// </summary>
        public DxToolTipChangeType ToolTipChange { get; set; }
    }
    /// <summary>
    /// Informace o nalezeném tooltipu v klientu typu <see cref="IDxToolTipDynamicClient"/>
    /// </summary>
    public enum DxToolTipChangeType
    {
        /// <summary>
        /// Neurčeno... Neřešit ToolTip
        /// </summary>
        None,
        /// <summary>
        /// Pro danou pozici nemá být tooltip, zhasni dosavadní pokud je zobrazen.
        /// </summary>
        NoToolTip,
        /// <summary>
        /// Pro danou pozici je tooltip nový, jiný než dosud.
        /// V tomto případě se aplikuje zhasnutí a čekání po patřičnou dobu, než se zobrazí nový ToolTip.
        /// </summary>
        NewToolTip,
        /// <summary>
        /// Pro danou pozici je stále stejný tooltip jako minule, nech jej svítit.
        /// </summary>
        SameAsLastToolTip
    }
    #endregion
}
