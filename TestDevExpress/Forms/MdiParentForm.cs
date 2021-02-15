using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DS = DevExpress.Skins;

namespace TestDevExpress.Forms
{
    public partial class MdiParentForm : MdiBaseForm
    {
        public MdiParentForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
        }
        protected override void AsolInitializeControls()
        {
            InitSkinControls();
            InitFormControls();
        }
        #region Ribbon
        protected override void AsolFillRibbon()
        {
            AsolFillRibbonData();
        }
        protected void AsolFillRibbonData()
        {
            List<IRibbonItem> ribbonItems = new List<IRibbonItem>();
            AddBasicItemsToRibbon(ribbonItems);
            RibbonSample.AddItems(ribbonItems, 8);
            AsolRibbon.AddItems(ribbonItems);
        }
        protected void AddBasicItemsToRibbon(List<IRibbonItem> items)
        {
            AddFormItemsToRibbon(items);
            AddSkinItemsToRibbon(items);
            AddRibbonItemsToRibbon(items);
        }
        protected IRibbonItem CreateMainRibbonItem(string itemId, string itemText, string itemImage, string toolTip, object tag = null, Action<RibbonItem> action = null)
        {
            RibbonItem item = new RibbonItem()
            {
                PageId = "MainPage",
                PageOrder = 0,
                PageText = "ZÁKLADNÍ",
                GroupId = "MainGroupForm",
                GroupText = "Okna",
                ItemId = itemId,
                ItemText = itemText,
                ItemImage = itemImage,
                ToolTip = toolTip,
                Tag = tag
            };
            action?.Invoke(item);
            return item;
        }
        protected override void OnRibbonItemClick(IRibbonData ribbonData)
        {
            base.OnRibbonItemClick(ribbonData);
            if (ribbonData.Tag is SkinInfo skinInfo) SelectSkin(skinInfo);
            if (ribbonData.Tag is FormCommands formCommand) FormAction(formCommand);
            if (ribbonData.Tag is RibbonCommands ribbonCommand) RibbonAction(ribbonCommand);
        }
        #endregion
        #region Skiny
        protected void InitSkinControls()
        {
            DevExpress.UserSkins.BonusSkins.Register();
            DS.SkinManager.EnableFormSkins();
            DS.SkinManager.EnableMdiFormSkins();

            _Skins = null;

            SelectSkin("Black");
        }
        protected void AddSkinItemsToRibbon(List<IRibbonItem> items)
        {
            items.Add(CreateMainRibbonItem("MainItem80", "Vzhled", nameof(Properties.Resources.colorize_24_), "Změní vzhled okna atd", action: item => PrepareSkinRibbonItem(item)));
        }
        protected void PrepareSkinRibbonItem(RibbonItem item)
        {
            item.ItemIsFirstInGroup = true;
            item.ItemType = RibbonItemType.Menu;
            List<RibbonItem> subItems = new List<RibbonItem>();
            foreach (var skinGroup in this.Skins)
            {
                var firstItem = skinGroup[0];
                if (skinGroup.Length == 1)
                {   // V grupě je jen jeden prvek => dáme jej přímo:
                    subItems.Add(CreateSkinRibbonItem(firstItem));
                }
                else
                {   // V grupě je více prvků => do pole subItems dáme Group prvek jako Titulek, a do něj potom všechny Skiny dané grupy:
                    RibbonItem subItem = new RibbonItem() { ItemId = firstItem.FamilyOrder, ItemText = firstItem.FamilyName, ItemType = RibbonItemType.Menu };
                    subItems.Add(subItem);
                    List<RibbonItem> subSubItems = new List<RibbonItem>();
                    foreach (var skinItem in skinGroup)
                        subSubItems.Add(CreateSkinRibbonItem(skinItem));
                    subItem.SubItems = subSubItems.ToArray();
                }
            
            }
            item.SubItems = subItems.ToArray();
        }
        protected RibbonItem CreateSkinRibbonItem(SkinInfo skinInfo)
        {
            RibbonItem item = new RibbonItem()
            {
                ItemId = skinInfo.SkinOrder,
                ItemText = skinInfo.SkinName,
                ItemIsFirstInGroup = skinInfo.FirstInGroup,
                Tag = skinInfo
            };
            return item;
        }
        protected void SelectSkin(SkinInfo skinInfo)
        {
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skinInfo.SkinName;
        }
        protected void SelectSkin(DS.SkinContainer skinContainer)
        {
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skinContainer.SkinName;
        }
        protected void SelectSkin(string skinName)
        {
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skinName;
        }
        /// <summary>
        /// Pole dostupných skinů
        /// </summary>
        protected SkinInfo[][] Skins { get { if (_Skins is null) _Skins = SkinInfo.Skins; return _Skins; } }
        private SkinInfo[][] _Skins;
        #endregion
        #region Random Ribbon Content
        protected void AddRibbonItemsToRibbon(List<IRibbonItem> items)
        {
            items.Add(CreateMainRibbonItem("RibbonItem01", "Clear Ribbon", nameof(Properties.Resources.bookmark_24_), "Otevře nové okno jako TAB document", RibbonCommands.ClearAddBasic, action: r => r.ItemIsFirstInGroup = true));
            items.Add(CreateMainRibbonItem("RibbonItem02", "Reset Ribbon", nameof(Properties.Resources.bookmark_3_24_), "Aktivuje některý existující TAB document", RibbonCommands.ClearAddRandom));
            items.Add(CreateMainRibbonItem("RibbonItem03", "Add1 Ribbon", nameof(Properties.Resources.bookmark_toolbar_2_24_), "Zavře některý existující TAB document", RibbonCommands.Add1));
            items.Add(CreateMainRibbonItem("RibbonItem04", "Add5 Ribbon", nameof(Properties.Resources.bookmark_toolbar_4_24_), "Otevře nové okno jako FREE WINDOW", RibbonCommands.Add5));
        }
        protected void RibbonAction(RibbonCommands ribbonCommand)
        {
            AsolRibbon.Freeze = false;

            List<IRibbonItem> ribbonItems = new List<IRibbonItem>();
            switch (ribbonCommand)
            {
                case RibbonCommands.ClearAddBasic:
                    AsolRibbon.Clear();
                    AddBasicItemsToRibbon(ribbonItems);
                    break;
                case RibbonCommands.ClearAddRandom:
                    AsolRibbon.Clear();
                    AddBasicItemsToRibbon(ribbonItems);
                    RibbonSample.AddItems(ribbonItems, 8);
                    break;
                case RibbonCommands.Add1:
                    RibbonSample.AddItems(ribbonItems, 3);
                    break;
                case RibbonCommands.Add5:
                    RibbonSample.AddItems(ribbonItems, 12);
                    break;
            }
            AsolRibbon.AddItems(ribbonItems);

            AsolRibbon.Freeze = false;
        }

        protected enum RibbonCommands { None, ClearAddBasic, ClearAddRandom, Add1, Add5 }
        #endregion
        #region Okna a MDI Manager
        protected void InitFormControls()
        {
            this._DocumentManager = new DevExpress.XtraBars.Docking2010.DocumentManager();
            this._DocumentManager.MdiParent = this;
            this._DocumentManager.View = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView();
            this._DocumentManager.ContainerControl = this.AsolPanel;

            this.AsolPanel.Controls.Add(new Label() { Text = "Toto je základní AsolPanel...", AutoSize = true, Location = new Point(10, 10) });
        }
        protected void AddFormItemsToRibbon(List<IRibbonItem> items)
        {
            items.Add(CreateMainRibbonItem("FormItem01", "Nový TAB", nameof(Properties.Resources.document_new_24_), "Otevře nové okno jako TAB document", FormCommands.NewTab));
            items.Add(CreateMainRibbonItem("FormItem02", "Změň TAB", nameof(Properties.Resources.document_export_3_24_), "Aktivuje některý existující TAB document", FormCommands.ChangeTab));
            items.Add(CreateMainRibbonItem("FormItem03", "Zavři TAB", nameof(Properties.Resources.document_close_2_24_), "Zavře některý existující TAB document", FormCommands.CloseTab));
            items.Add(CreateMainRibbonItem("FormItem04", "Nový FREE", nameof(Properties.Resources.document_new_5_24_), "Otevře nové okno jako FREE WINDOW", FormCommands.NewFree));
        }
        protected void FormAction(FormCommands command)
        {
            switch (command)
            {
                case FormCommands.NewTab:
                    FormActionNewTab();
                    break;
                case FormCommands.ChangeTab:
                    FormActionChangeTab();
                    break;
                case FormCommands.CloseTab:
                    FormActionCloseTab();
                    break;
                case FormCommands.NewFree:
                    FormActionNewFree();
                    break;
            }
        }
        protected void FormActionNewTab()
        {
            MdiChildForm form = new MdiChildForm();
            form.MdiParent = this;
            form.Show();
        }
        protected void FormActionChangeTab()
        { }
        protected void FormActionCloseTab()
        { }
        protected void FormActionNewFree()
        {
            MdiChildForm form = new MdiChildForm();
            // form.MdiParent = this;
            form.Show();
        }
        private MdiManager _MdiManager;
        private DevExpress.XtraBars.Docking2010.DocumentManager _DocumentManager;
        protected enum FormCommands { None, NewTab, ChangeTab, CloseTab, NewFree }
        #endregion
    }

    #region SkinInfo
    public class SkinInfo
    {
        #region Public členy : statické pole všech skinů, instanční data jednoho skinu
        public static SkinInfo[][] Skins { get { return _LoadSkins(); } }
        public DS.SkinContainer Skin { get; private set; }
        public SkinFamily Family { get; private set; }
        public string FamilyName { get; private set; }
        public string FamilyOrder { get; private set; }
        public string SkinOrder { get; private set; }
        public string SkinName { get; private set; }
        public bool Enabled { get; private set; }
        public bool FirstInGroup { get; private set; }
        public override string ToString()
        {
            return $"Family: {FamilyName}, Skin: {SkinName}";
        }
        #endregion
        #region Načtení a konstruktor


        private static SkinInfo[][] _LoadSkins()
        {
            // Primární List = lineární přes všechny grupy:
            List<SkinInfo> skins = new List<SkinInfo>();
            foreach (DS.SkinContainer skin in DS.SkinManager.Default.Skins)
            {
                SkinInfo skinInfo = _CreateSkinInfo(skin);
                skins.Add(skinInfo);
            }

            // Grupovaný List = grupy podle FamilyOrder:
            List<SkinInfo[]> skinGroups = new List<SkinInfo[]>();
            var groups = skins.GroupBy(s => s.FamilyOrder).ToList();
            groups.Sort((a, b) => String.Compare(a.Key, b.Key, StringComparison.CurrentCulture));
            foreach (var group in groups)
            {   // Jedna grupa (group) obsahuje například všechny Skiny pro Office:
                List<SkinInfo> items = new List<SkinInfo>(group);
                items.Sort((a, b) => String.Compare(a.SkinOrder, b.SkinOrder, StringComparison.CurrentCulture));
                _PrepareSkinsDelimiter(items);
                skinGroups.Add(items.ToArray());
            }

            return skinGroups.ToArray();
        }
        private static SkinInfo _CreateSkinInfo(DS.SkinContainer skin)
        {
            string skinName = skin.SkinName;
            SkinFamily family = _GetSkinFamily(skinName);
            string familyName = _GetFamilyName(family);
            string familyOrder = _GetFamilyOrder(family, skinName, familyName);
            string skinOrder = _GetSkinOrder(family, skinName, out bool firstInGroup);

            SkinInfo skinInfo = new SkinInfo()
            {
                Skin = skin,
                Family = family,
                FamilyName = familyName,
                FamilyOrder = familyOrder,
                SkinOrder = skinOrder,
                SkinName = skinName,
                Enabled = (family != SkinFamily.Disabled),
                FirstInGroup = firstInGroup
            };

            return skinInfo;
        }
        private static void _PrepareSkinsDelimiter(List<SkinInfo> items)
        {
            int count = items.Count;
            if (count < 2 || !_HasFamilyDelimiters(items[0].Family)) return;
            var skin0 = items[0];
            for (int i = 1; i < count; i++)
            {
                var skin1 = items[i];
                skin1.FirstInGroup = _IsDelimiterBetweenSkins(skin0, skin1);
                skin0 = skin1;
            }
        }
        private static SkinFamily _GetSkinFamily(string skinName)
        {
            if (skinName.StartsWith("Office ")) return SkinFamily.Office;
            if (skinName.StartsWith("Visual Studio")) return SkinFamily.VisualStudio;
            if (skinName.StartsWith("VS")) return SkinFamily.VisualStudio;
            if (skinName.StartsWith("DevExpress")) return SkinFamily.DevExpress;

            string test = skinName + ";";
            if (_SkinNamesSeasons.Contains(test)) return SkinFamily.Seasons;
            if (_SkinNamesContrast.Contains(test)) return SkinFamily.Contrast;
            if (_SkinNamesColors.Contains(test)) return SkinFamily.Colors;
            if (_SkinNamesThemes.Contains(test)) return SkinFamily.Themes;
            if (_SkinNamesDisabled.Contains(test)) return SkinFamily.Disabled;

            return SkinFamily.Default;
        }
        private static bool _HasFamilyDelimiters(SkinFamily family)
        {
            return (family == SkinFamily.Office);
        }
        private static bool _IsDelimiterBetweenSkins(SkinInfo skin0, SkinInfo skin1)
        {
            switch (skin0.Family)
            {
                case SkinFamily.Office:
                    // "Office 2010..." / "Office 2013..."
                    return (skin0.SkinName.Length >= 11 && skin1.SkinName.Length >= 11 && skin0.SkinName.Substring(0, 10) != skin1.SkinName.Substring(0, 10));
            }
            return false;
        }
        private static string _GetFamilyName(SkinFamily family)
        {
            switch (family)
            {
                case SkinFamily.Office: return "Office";
                case SkinFamily.VisualStudio: return "VisualStudio";
                case SkinFamily.DevExpress: return "VisualStudio";
                case SkinFamily.Seasons: return "Sezónní";
                case SkinFamily.Contrast: return "Kontrastní";
                case SkinFamily.Colors: return "Barevné";
                case SkinFamily.Themes: return "Tematické";
                case SkinFamily.Disabled: return "Zakázané";
                case SkinFamily.Default: return null;
            }
            return null;
        }
        private static string _GetFamilyOrder(SkinFamily family, string skinName, string familyName)
        {
            switch (family)
            {
                case SkinFamily.Office: return "1";
                case SkinFamily.VisualStudio: return "2";
                case SkinFamily.DevExpress: return "2";
                case SkinFamily.Seasons: return "3";
                case SkinFamily.Contrast: return "4";
                case SkinFamily.Colors: return "5";
                case SkinFamily.Themes: return "6";
                case SkinFamily.Disabled: return "7";
                case SkinFamily.Default: return "9." + skinName;     // Každý skin = jeden řádek v hlavní úrovni = negrupovat
            }
            return "A." + skinName;
        }
        private static string _GetSkinOrder(SkinFamily family, string skinName, out bool firstInGroup)
        {
            firstInGroup = false;
            switch (family)
            {
                case SkinFamily.Office:
                    return skinName;
                case SkinFamily.VisualStudio: return "2";
                case SkinFamily.DevExpress: return "2";
                case SkinFamily.Seasons: return "3";
                case SkinFamily.Contrast: return "4";
                case SkinFamily.Colors: return "5";
                case SkinFamily.Themes: return "6";
                case SkinFamily.Disabled: return "7";
                case SkinFamily.Default: return "9." + skinName;     // Každý skin = jeden řádek v hlavní úrovni = negrupovat
            }
            return "A." + skinName;
        }

        private const string _SkinNamesSeasons = "Pumpkin;Springtime;Valentine;Xmas 2008 Blue;Summer 2008;London Liquid Sky;";
        private const string _SkinNamesContrast = "Blueprint;High Contrast; Whiteprint;Sharp;Sharp Plus;";
        private const string _SkinNamesColors = "Basic;Black;Blue;Foggy;Glass Oceans;Seven;Seven Classic;";
        private const string _SkinNamesThemes = "Caramel;Coffee;Dark Side;Darkroom;iMaginary;Lilian;Liquid Sky;McSkin;Metropolis;Metropolis Dark;Money Twins;Stardust;The Asphalt World;The Bezier;";
        private const string _SkinNamesDisabled = "";
        private SkinInfo() { }
        #endregion
    }
    /// <summary>
    /// Rodina Skinu
    /// </summary>
    public enum SkinFamily : int
    {
        Office,
        VisualStudio,
        DevExpress,
        Seasons,
        Contrast,
        Colors,
        Themes,
        Disabled,
        Default
    }
    #endregion
}
