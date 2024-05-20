using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using WinDraw = System.Drawing;
using DxDForm = Noris.Clients.Win.Components.AsolDX.DataForm;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DxLData = Noris.Clients.Win.Components.AsolDX.DataForm.Layout;
using System.Drawing;
using Noris.WS.DataContracts.DxForm;
using Noris.Clients.Win.Components.AsolDX.DataForm;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormX"/>
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "DataForm", buttonOrder: 12, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře okno DataForm verze 3", tabViewToolTip: "Okno zobrazující nový DataForm")]
    public class DataFormV3 : DxRibbonForm, IControlInfoSource
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormV3()
        {
            this.ImageName = "svgimages/spreadsheet/showcompactformpivottable.svg";
            this.ImageNameAdd = "@text|D|#002266||B|3|#88AAFF|#CCEEFF";

            __CurrentId = ++__InstanceCounter;
            __DataFormId = 0;
            _RefreshTitle();
        }
        private void _RefreshTitle()
        {
            bool hasDataForm = (_DxDataFormPanel != null);
            string formId = (hasDataForm ? ":" + __DataFormId.ToString() : "");
            this.Text = $"DataForm V3 [{__CurrentId}{formId}]";

        }
        private int __CurrentId;
        private int __DataFormId;
        private static int __InstanceCounter;
        #endregion
        #region Ribbon - obsah a rozcestník
        /// <summary>
        /// Připraví Ribbon
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            PrepareClipboard();

            this.TestPainting = false;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;
            string radioGroupName = "SamplesGroup";

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);
            pages.Add(page);

            string imageStatusRefresh = "svgimages/xaf/action_refresh.svg";
            string imageTestDrawing = "svgimages/dashboards/textbox.svg";
            string imageDataFormRemove = "svgimages/spreadsheet/removetablerows.svg";
            string imageChangeData = "svgimages/richedit/trackingchanges_allmarkup.svg";        // "svgimages/richedit/trackingchanges_trackchanges.svg";

            group = new DataRibbonGroup() { GroupText = "DataForm" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "StatusRefresh", Text = "Refresh Status", ToolTipText = "Znovu načíst údaje o spotřebě systémových zdrojů do statusbaru", ImageName = imageStatusRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "TestDrawing", Text = "TestDrawing", ToolTipText = "Vykreslování bez fyzických Controlů - pro test rychlosti", ImageName = imageTestDrawing, RibbonStyle = RibbonItemStyles.Large, ItemType = RibbonItemType.CheckButton, Checked = TestPainting });

            // smazat:
            /*
            var groupSvg = new DataRibbonGroup() { GroupText = "GenericSvgImages" };
            groupSvg.Items.Add(new DataRibbonItem() { ItemId = "g1", Text = "Circle", ImageName = "@circle|#881166|95|#EEAADD",  RibbonStyle = RibbonItemStyles.Large });
            groupSvg.Items.Add(new DataRibbonItem() { ItemId = "g2", Text = "Arrow", ImageName = "@arrow|D|red", RibbonStyle = RibbonItemStyles.Large });
            groupSvg.Items.Add(new DataRibbonItem() { ItemId = "g3", Text = "Copy", ImageName = "@edit|copy|green", RibbonStyle = RibbonItemStyles.Large });
            groupSvg.Items.Add(new DataRibbonItem() { ItemId = "g4", Text = "Text", ImageName = "@text|M|#7F00FF|||12|#7777FF|#60D7FF", RibbonStyle = RibbonItemStyles.Large });
            groupSvg.Items.Add(new DataRibbonItem() { ItemId = "g5", Text = "Ikona", ImageName = "@text|m|#7F00FF|||12|#7777FF|#60D7FF", RibbonStyle = RibbonItemStyles.SmallWithText });
            page.Groups.Add(groupSvg);
            */

            // Samply:
            var groupSamples = new DataRibbonGroup() { GroupText = "Ukázky layoutu a počtu řádků" };
            groupSamples.Items.Add(new DataRibbonItem() { ItemId = "DataFormRemove", Text = "Remove DataForm", ToolTipText = "Zahodit DataForm a uvolnit jeho zdroje", ImageName = imageDataFormRemove, ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = radioGroupName, Checked = true });
            groupSamples.Items.Add(new DataRibbonItem() { ItemId = "ChangeData", Text = "Change Data", ToolTipText = "Změní obsah dat / stav Enabled v dataformu", ImageName = imageChangeData });

            // frm.xml:
            string imageOpen = "svgimages/dashboards/open.svg";
            var openFileItems = _GetFrmXmlFileItems();
            groupSamples.Items.Add(new DataRibbonItem() { ItemId = "LoadFormFile", Text = "Load XML", ToolTipText = "Načte definici z XML souboru", ImageName = imageOpen, ItemType = RibbonItemType.Menu, SubItems = openFileItems });

            // WebBrowser testy:
            //string imageWeb = "svgimages/spreadsheet/functionsweb.svg";
            //ListExt<IRibbonItem> linkWebs = this._GetBrowseLinks();
            //groupSamples.Items.Add(new DataRibbonItem() { ItemId = "OpenWww", Text = "WebBrowser", ToolTipText = "Otevře WebBrowser se zvolenou adresou", ImageName = imageWeb, ItemType = RibbonItemType.Menu, SubItems = linkWebs });


            page.Groups.Add(groupSamples);
            string imageTest1 = "svgimages/xaf/actiongroup_easytestrecorder.svg";
            string imageTest2 = "svgimages/spreadsheet/showoutlineformpivottable.svg";
            string imageTest3 = "svgimages/spreadsheet/showtabularformpivottable.svg";

            // Na čísla Sample reagují metody _CreateSampleLayout() a _CreateSampleRows() !
            addSampleButton("A", "Form A", imageTest1, 1, 2, 8, 32, 48);
            addSampleButton("B", "Form B", imageTest2, 1, 2, 8, 12, 36);
            addSampleButton("C", "Table C", imageTest3, 1, 4, 16, 32, 96, 256);


            // _AddPageCombo(pages);


            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);


            // this.TestSvgDisable();

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;

            void addSampleButton(string layoutId, string text, string imageName, params int[] rowsCounts)
            {
                var subItems = new ListExt<IRibbonItem>();
                foreach (int rowsCount in rowsCounts)
                {
                    subItems.Add(new DataRibbonItem()
                    {
                        ItemId = $"CreateSample{layoutId}{rowsCount}",
                        Text = $"Řádků: {rowsCount}",
                        ItemType = RibbonItemType.Button
                    });
                }

                groupSamples.Items.Add(new DataRibbonItem()
                {
                    ItemId = $"CreateLayout{layoutId}",
                    Text = text,
                    ImageName = imageName,
                    ItemType = RibbonItemType.Menu,
                    SubItems = subItems,
                    RadioButtonGroupName = radioGroupName
                });
            }
        }
        private void _AddPageCombo(List<DataRibbonPage> pages)
        {
            var page2 = new DataRibbonPage() { PageId = "Testy", PageText = "Ukázky" };
            var group21 = new DataRibbonGroup() { GroupId = "TestCombo", GroupText = "Combo boxy" };

            DxBorderStyle comboBorder = DxBorderStyle.None;
            DxBorderStyle buttonBorder = DxBorderStyle.None;

            string imageApp1 = "svgimages/chart/charttype_area3d.svg";
            string imageApp2 = "svgimages/chart/charttype_areastacked.svg";
            string imageApp3 = "svgimages/chart/charttype_bar3dstacked.svg";
            string imageApp4 = "svgimages/chart/charttype_doughnut3d.svg";

            string imageClear = "images/xaf/templatesv2images/action_delete.svg";
            string imageApp0 = "svgimages/business%20objects/bo_appearance.svg";

            ListExt<IRibbonItem> items1 = new ListExt<IRibbonItem>();
            items1.Add(new DataRibbonItem() { ItemId = "filt_111", Text = "111 Filtr pro nákupy" });
            items1.Add(new DataRibbonItem() { ItemId = "filt_112", Text = "112 Filtr pro prodeje" });
            items1.Add(new DataRibbonItem() { ItemId = "filt_113", Text = "113 Pro prezentaci" });

            DataSubButton[] subButtons1 = new DataSubButton[]
            {
                new DataSubButton() { ButtonId = "DropDown", ButtonType = PredefinedButtonType.DropDown },
                new DataSubButton() { ButtonId = "Clear", ButtonType = PredefinedButtonType.Clear, Enabled = false, ToolTipTitle = "Zrušit filtr", ToolTipText = "Zruší aplikovaný filtr" },
                new DataSubButton() { ButtonId = "Manager", ButtonType = PredefinedButtonType.Glyph, ImageName = imageApp1, IsLeft = true, ToolTipTitle = "Manager (F4)", ToolTipText = "Otevře okno s nabídkou filtrů", Shortcut = Keys.F4 }
            };
            // string subButtons1 = $"DropDown;/Clear={imageClear}:Zrušit filtr;<Manager={imageApp1}:F4: Otevře okno s nabídkou filtrů...";

            var combo211 = new DataRibbonComboItem() { ItemId = "Combo211", Width = 210, ComboBorderStyle = comboBorder, SubButtonsBorderStyle = buttonBorder, NullValuePrompt = "filtr nezadán", SubItems = items1, SubButtons = DataSubButton.Serialize(subButtons1) };
            group21.Items.Add(combo211);

            ListExt<IRibbonItem> items2 = new ListExt<IRibbonItem>();
            items2.Add(new DataRibbonItem() { ItemId = "temp_211", Text = "211 Šablona pro nákupčí" });
            items2.Add(new DataRibbonItem() { ItemId = "temp_212", Text = "212 Šablona pro prodejce", Checked = true });
            items2.Add(new DataRibbonItem() { ItemId = "temp_213", Text = "213 Šablona pro prezentaci" });
            DataSubButton[] subButtons2 = new DataSubButton[]
            {
                new DataSubButton() { ButtonId = "DropDown", ButtonType = PredefinedButtonType.DropDown },
                new DataSubButton() { ButtonId = "Clear", ButtonType = PredefinedButtonType.Clear, ToolTipTitle = "Zrušit šablonu", ToolTipText = "Zruší aplikovanou šablonu a vrátí výchozí" },
                new DataSubButton() { ButtonId = "Manager", ButtonType = PredefinedButtonType.Glyph, ImageName = imageApp2, IsLeft = true, ToolTipTitle = "Manager (F5)", ToolTipText = "Otevře okno s nabídkou šablon", Shortcut = Keys.F5 }
            };
            // string subButtons2 = $"DropDown;Clear={imageClear}:Zrušit šablonu;<Manager={imageApp2}:F5: Otevře okno s nabídkou šablon...";
            //        subButtons2 = $"Clear={imageClear}:Zrušit šablonu;DropDown;<Manager={imageApp2}:F5: Otevře okno s nabídkou šablon...";

            var combo212 = new DataRibbonComboItem() { ItemId = "Combo212", Width = 210, ComboBorderStyle = comboBorder, SubButtonsBorderStyle = buttonBorder, NullValuePrompt = "šablona nezadána", SubItems = items2, SubButtons = DataSubButton.Serialize(subButtons2) };
            group21.Items.Add(combo212);

            ListExt<IRibbonItem> items3 = new ListExt<IRibbonItem>();
            items3.Add(new DataRibbonItem() { ItemId = "view_311", Text = "311 Pohled do minulosti" });
            items3.Add(new DataRibbonItem() { ItemId = "view_312", Text = "312 Pohled do současnosti" });
            items3.Add(new DataRibbonItem() { ItemId = "view_313", Text = "313 Pohled do budoucnosti", Checked = true });
            DataSubButton[] subButtons3 = new DataSubButton[]
            {
                new DataSubButton() { ButtonId = "DropDown", ButtonType = PredefinedButtonType.DropDown },
                new DataSubButton() { ButtonId = "Clear", ButtonType = PredefinedButtonType.Close, ToolTipTitle = "Zrušit pohled", ToolTipText = "Zruší aplikovaný pohled" },
                new DataSubButton() { ButtonId = "Manager", ButtonType = PredefinedButtonType.Glyph, ImageName = imageApp3, IsLeft = true, ToolTipTitle = "Manager (F7)", ToolTipText = "Otevře okno s nabídkou pohledů", Shortcut = Keys.F7 }
            };
            // string subButtons3 = $"DropDown;Clear={imageClear}:Zrušit pohled;<Manager={imageApp3}:F7: Otevře okno s nabídkou pohledů...";
            //        subButtons3 = $"<Manager={imageApp3}:F7: Otevře okno s nabídkou pohledů...;Clear={imageClear}:Zrušit pohled;DropDown";

            var combo213 = new DataRibbonComboItem() { ItemId = "Combo213", Width = 210, ComboBorderStyle = comboBorder, SubButtonsBorderStyle = buttonBorder, NullValuePrompt = "pohled nezadán", SubItems = items3, SubButtons = DataSubButton.Serialize(subButtons3) };
            group21.Items.Add(combo213);

            page2.Groups.Add(group21);


            var group22 = new DataRibbonGroup() { GroupId = "TestImageArray", GroupText = "SvgImageArrayInfo" };

            // Základní obrázky:
            string image1 = "images/xaf/templatesv2images/action_delete.svg";
            string image2 = "svgimages/chart/charttype_doughnut3d.svg";

            // Bitmapy nelze použít:
            //   image1 = "devav/actions/close_32x32.png";
            //   image2 = "devav/actions/delete_32x32.png";

            // Kombinace základního image1 (100% velikost) + overlay image2 vpravo dole na 60%:
            var image3Array = new Noris.WS.DataContracts.Desktop.Data.SvgImageArrayInfo(image1);
            image3Array.Add(image2, ContentAlignment.BottomRight, 60);
            string image3 = image3Array.Key;

            // Kombinace základního image1 (vlevo nahoře 70% velikost) + overlay image2 vpravo dole na 60%:
            var image4Array = new Noris.WS.DataContracts.Desktop.Data.SvgImageArrayInfo();
            image4Array.Add(image1, ContentAlignment.TopLeft, 70);
            image4Array.Add(image2, ContentAlignment.BottomRight, 60);
            string image4 = image4Array.Key;

            // Deklarace tlačítek Ribbonu:
            group22.Items.Add(new DataRibbonItem() { ItemId = "CombiImage1", Text = "Image1", ImageName = image1 });
            group22.Items.Add(new DataRibbonItem() { ItemId = "CombiImage2", Text = "Image2", ImageName = image2 });
            group22.Items.Add(new DataRibbonItem() { ItemId = "CombiImage12a", Text = "Image12a", ImageName = image3 });
            group22.Items.Add(new DataRibbonItem() { ItemId = "CombiImage12b", Text = "Image12b", ImageName = image4 });

            page2.Groups.Add(group22);


            pages.Add(page2);
        }

        private void TestSvgDisable()
        {
            string svgInpErr = @"<svg width=""32"" height=""32"" viewBox=""0 0 32 32"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M0.5 11.5H29.5V4.5H0.5V11.5Z"" fill=""#FFFFFF""/>
<path d=""M0 4.5V11.5V12H0.5H29.5H30V11.5V4.5V4H29.5H0.5H0V4.5ZM29 5V11H1V5H29Z"" fill=""#383838""/>
<path d=""M0.5 18.5H17.5V17.5H21.59L21.74 17.08L21.95 16.7L22.21 16.36L22.53 16.07L22.9 15.83L23.3 15.65L23.73 15.54L24.18 15.5H26.82L27.27 15.54L27.71 15.65L28.11 15.83L28.47 16.07L28.79 16.36L29.05 16.7L29.26 17.08L29.41 17.5H29.5V11.5H0.5V18.5Z"" fill=""#FFFFFF""/>
<path d=""M0 11.5V18.5V19H0.5H17.5V18H1V12H29V16.63L29.05 16.7L29.26 17.08L29.41 17.5H30V11.5V11H29.5H0.5H0V11.5Z"" fill=""#383838""/>
<path d=""M0.5 25.5H18.5V21.5H17.5V18.5H0.5V25.5Z"" fill=""#92CBEE""/>
<path d=""M0 18.5V25.5V26H0.5H18.5V25H1V19H17.5V18H0.5H0V18.5Z"" fill=""#0964B0""/>
<path d=""M29.135 31.5H21.866C21.115 31.5 20.501 30.886 20.501 30.135V19.5H30.501V30.135C30.5 30.886 29.886 31.5 29.135 31.5ZM32 19.5H19Z"" fill=""#FFFFFF""/>
<path d=""M32 19.5H19M29.135 31.5H21.866C21.115 31.5 20.501 30.886 20.501 30.135V19.5H30.501V30.135C30.5 30.886 29.886 31.5 29.135 31.5Z"" stroke=""#383838"" stroke-miterlimit=""10""/>
<path d=""M25.5 29V22ZM27.5 29V22ZM23.5 29V22Z"" fill=""#FFFFFF""/>
<path d=""M25.5 29V22M27.5 29V22M23.5 29V22"" stroke=""#EE3D3B"" stroke-miterlimit=""10""/>
<path d=""M23.5 19.5V18.183C23.5 17.806 23.806 17.5 24.183 17.5H26.818C27.195 17.5 27.501 17.806 27.501 18.183V19.5"" stroke=""#383838"" stroke-miterlimit=""10""/>
</svg> 
";

            string svgInp = @"<svg width=""32"" height=""32"" viewBox=""0 0 32 32"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
  <g id=""main"">
    <path d=""M0.5 11.5H29.5V4.5H0.5V11.5Z"" fill=""#FFFFFF""/>
    <path d=""M0 4.5V11.5V12H0.5H29.5H30V11.5V4.5V4H29.5H0.5H0V4.5ZM29 5V11H1V5H29Z"" fill=""#383838""/>
    <path d=""M0.5 18.5H17.5V17.5H21.59L21.74 17.08L21.95 16.7L22.21 16.36L22.53 16.07L22.9 15.83L23.3 15.65L23.73 15.54L24.18 15.5H26.82L27.27 15.54L27.71 15.65L28.11 15.83L28.47 16.07L28.79 16.36L29.05 16.7L29.26 17.08L29.41 17.5H29.5V11.5H0.5V18.5Z"" fill=""#FFFFFF""/>
    <path d=""M0 11.5V18.5V19H0.5H17.5V18H1V12H29V16.63L29.05 16.7L29.26 17.08L29.41 17.5H30V11.5V11H29.5H0.5H0V11.5Z"" fill=""#383838""/>
    <path d=""M0.5 25.5H18.5V21.5H17.5V18.5H0.5V25.5Z"" fill=""#92CBEE""/>
    <path d=""M0 18.5V25.5V26H0.5H18.5V25H1V19H17.5V18H0.5H0V18.5Z"" fill=""#0964B0""/>
    <path d=""M29.135 31.5H21.866C21.115 31.5 20.501 30.886 20.501 30.135V19.5H30.501V30.135C30.5 30.886 29.886 31.5 29.135 31.5ZM32 19.5H19Z"" fill=""#FFFFFF""/>
    <path d=""M32 19.5H19M29.135 31.5H21.866C21.115 31.5 20.501 30.886 20.501 30.135V19.5H30.501V30.135C30.5 30.886 29.886 31.5 29.135 31.5Z"" stroke=""#383838"" stroke-miterlimit=""10""/>
    <path d=""M25.5 29V22ZM27.5 29V22ZM23.5 29V22Z"" fill=""#FFFFFF""/>
    <path d=""M25.5 29V22M27.5 29V22M23.5 29V22"" stroke=""#EE3D3B"" stroke-miterlimit=""10""/>
    <path d=""M23.5 19.5V18.183C23.5 17.806 23.806 17.5 24.183 17.5H26.818C27.195 17.5 27.501 17.806 27.501 18.183V19.5"" stroke=""#383838"" stroke-miterlimit=""10""/>
  </g>
</svg>
";

            var modifier = new SvgImageModifier();
            var svgLigDis = modifier.Convert(svgInpErr, DxSvgImagePaletteType.LightSkinDisabled, "itemdel-large.svg", readableXmlFormat: true);
            var svgDarDis = modifier.Convert(svgInpErr, DxSvgImagePaletteType.DarkSkinDisabled, "itemdel-large.svg", readableXmlFormat: true);

            int length = svgLigDis.Length;


        }


        /// <summary>
        /// Odebere DataForm
        /// </summary>
        private void _RemoveMainControls()
        {
            _RemoveDxDataForm();

            GCCollect();
            WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();

            _RefreshTitle();
            RefreshStatusCurrent(false);
        }
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            var itemId = e.Item.ItemId;
            string layoutId = null;
            int sampleRows = 0;
            if (itemId.StartsWith("CreateSample") && itemId.Length >= 14 && Int32.TryParse(itemId.Substring(13), out sampleRows) && sampleRows > 0)
            {
                layoutId = itemId.Substring(12, 1);              // "A" - "Z"
                itemId = "CreateSample";
            }

            string fileName = null;
            if (itemId.StartsWith("LoadFormFileOne_") && e.Item.Tag is string tagText && !String.IsNullOrEmpty(tagText))
            {
                itemId = "LoadFormFileOne";
                fileName = tagText;
            }

            string linkUrl = null;
            if (itemId.StartsWith("OpenWeb") && e.Item.Tag is string urlText && !String.IsNullOrEmpty(urlText))
            {
                itemId = "OpenWeb";
                linkUrl = urlText;
            }

            switch (itemId)
            {
                case "StatusRefresh":
                    RefreshStatusCurrent(true);
                    break;
                case "TestDrawing":
                    this.TestPainting = e.Item?.Checked ?? false; 
                    break;
                case "DataFormRemove":
                    _RemoveMainControls();
                    break;
                case "LogClear":
                    DxComponent.LogClear();
                    break;
                case "CreateSample":
                    _AddDataFormSample(layoutId, sampleRows);
                    break;
                case "ChangeData":
                    _ChangeDataInDataForm();
                    break;
                case "LoadFormFileOne":
                    _LoadDataFrmXml(fileName);
                    break;
                default:
                    var n = itemId;
                    break;
            }
        }
        /// <summary>
        /// Vrátí soupis položek do nabídky Ribbonu pro načítání XML souborů
        /// </summary>
        /// <returns></returns>
        private ListExt<IRibbonItem> _GetFrmXmlFileItems()
        {
            ListExt<IRibbonItem> ribbonItems = new ListExt<IRibbonItem>();

            string iconXml = "svgimages/xaf/modeleditor_action_xml.svg";

            string appPath = DxComponent.ApplicationPath;                           // C:\DavidPrac\GitRepo\dj-soft\GraphLibrary\TestDevExpress\bin 
            string projPath = System.IO.Path.GetDirectoryName(appPath);             // C:\DavidPrac\GitRepo\dj-soft\GraphLibrary\TestDevExpress
            var files = System.IO.Directory.GetFiles(projPath, "*.xml", System.IO.SearchOption.AllDirectories).ToList();
            files.Sort();
            int itemId = 0;
            foreach ( var file in files)
            {
                if (file.EndsWith(".frm.xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    itemId++;
                    string relFile = System.IO.Path.GetFileName(file);
                    DataRibbonItem fileItem = new DataRibbonItem() { ItemId = $"LoadFormFileOne_{itemId}", ImageName = iconXml, Text = relFile, ToolTipTitle = "Načte obsah souboru", ToolTipText = file, Tag = file };
                    ribbonItems.Add(fileItem);
                }
            }

            return ribbonItems;
        }
        #endregion
        #region Status - proměnné, Zobrazení spotřeby paměti
        /// <summary>
        /// Tvorba StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this._StatusItemTitle = CreateStatusBarItem();
            this._StatusItemBefore = CreateStatusBarItem();
            this._StatusItemDeltaConstructor = CreateStatusBarItem();
            this._StatusItemDeltaShow = CreateStatusBarItem();
            this._StatusItemDeltaCurrent = CreateStatusBarItem(2);
            this._StatusItemDeltaForm = CreateStatusBarItem();
            this._StatusItemCurrent = CreateStatusBarItem(1);
            this._StatusItemTime = CreateStatusBarItem(2);

            // V tomto pořadí budou viditelné:
            //   netřeba : this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemTitle);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaCurrent);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemTime);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemCurrent);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemBefore);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaForm);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaConstructor);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaShow);

            this.DxStatusBar.Visible = true;
        }
        private DevExpress.XtraBars.BarStaticItem CreateStatusBarItem(int? fontSizeDelta = null)
        {
            DevExpress.XtraBars.BarStaticItem item = new DevExpress.XtraBars.BarStaticItem();
            item.MinWidth = 240;
            item.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            if (fontSizeDelta.HasValue)
                item.Appearance.FontSizeDelta = fontSizeDelta.Value;
            item.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            return item;
        }
        private void WinProcessReadAfterShown()
        {
            if (WinProcessInfoAfterShown == null)
                WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();
            RefreshStatus(false);
        }
        public DxComponent.WinProcessInfo WinProcessInfoBeforeForm { get; set; }
        public DxComponent.WinProcessInfo WinProcessInfoAfterInit { get; set; }
        public DxComponent.WinProcessInfo WinProcessInfoAfterShown { get; set; }
        private void RefreshStatus(bool withGcCollect)
        {
            string eol = Environment.NewLine;
            this._StatusItemTitle.Caption = ""; // "<b>DataForm tester</b>";

            var before = WinProcessInfoBeforeForm;
            this._StatusItemBefore.Caption = "   Stav před: <b>" + (before?.Text2 ?? "") + "<b>";
            this._StatusItemBefore.Hint = "Obsazená paměť před zahájením otevírání okna:" + eol + (before?.Text4Full ?? "");

            var deltaInit = WinProcessInfoAfterInit - WinProcessInfoBeforeForm;
            this._StatusItemDeltaConstructor.Caption = "   Delta Init: <b>" + (deltaInit?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaConstructor.Hint = "Spotřeba paměti v rámci konstruktoru a inicializaci:" + eol + (deltaInit?.Text4Full ?? "");

            var deltaShow = WinProcessInfoAfterShown - WinProcessInfoAfterInit;
            this._StatusItemDeltaShow.Caption = "   Delta Show: <b>" + (deltaShow?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaShow.Hint = "Spotřeba paměti od dokončení inicializace do konce provádění Show:" + eol + (deltaShow?.Text4Full ?? "");

            RefreshStatusCurrent(withGcCollect);
        }
        private void RefreshStatusCurrent(bool withGcCollect)
        {
            if (withGcCollect) GCCollect();

            string eol = Environment.NewLine;
            var current = DxComponent.WinProcessInfo.GetCurent();

            var deltaCurr = current - WinProcessInfoAfterShown;
            this._StatusItemDeltaCurrent.Caption = "   Delta Current: <b>" + (deltaCurr?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaCurrent.Hint = "Spotřeba paměti DxDataForm (od prázdného zobrazení do aktuálního stavu):" + eol + (deltaCurr?.Text4Full ?? "");

            var deltaForm = current - WinProcessInfoBeforeForm;
            this._StatusItemDeltaForm.Caption = "   Delta Form: <b>" + (deltaForm?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaForm.Hint = "Spotřeba celého aktuálního formuláře (od vytvoření do aktuálního stavu):" + eol + (deltaForm?.Text4Full ?? "");

            this._StatusItemCurrent.Caption = "   Total Current: <b>" + (current?.Text2 ?? "") + "<b>";
            this._StatusItemCurrent.Hint = "Spotřeba paměti aktuálně:" + eol + (current?.Text4Full ?? "");

            var time = _DxShowTimeSpan;
            bool hasTime = time.HasValue;
            this._StatusItemTime.Caption = hasTime ? "   Time: <b>" + time.Value.TotalMilliseconds.ToString("### ##0").Trim() + " ms<b>" : "";
            this._StatusItemTime.Hint = "Čas zobrazení DataFormu od začátku konstruktoru po GotFocus";
        }
        private void GCCollect()
        {
            GC.Collect(0, GCCollectionMode.Forced);
        }
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        private DevExpress.XtraBars.BarStaticItem _StatusItemBefore;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaConstructor;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaShow;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaCurrent;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaForm;
        private DevExpress.XtraBars.BarStaticItem _StatusItemTime;
        private DevExpress.XtraBars.BarStaticItem _StatusItemCurrent;
        #endregion
        #region DataForm načítaný ze souboru .frm.xml
        /// <summary>
        /// Načte definici ze souboru
        /// </summary>
        /// <param name="fileFrmXml"></param>
        private void _LoadDataFrmXml(string fileFrmXml)
        {
            string pathFrmXml = null;
            try
            {
                pathFrmXml = System.IO.Path.GetDirectoryName(fileFrmXml);
                var loadingArgs = new DfTemplateLoadArgs() { TemplateFileName = fileFrmXml, NestedTemplateLoader = loadNested, LogLoadingTime = true };
                var dxInfo = DxDForm.DfTemplateLoader.LoadInfo(loadingArgs);
                if (dxInfo.FormatVersion == FormatVersionType.Version4)
                {
                    var dfForm = DxDForm.DfTemplateLoader.LoadTemplate(loadingArgs);
                    var layoutArgs = new DfTemplateLayoutArgs() { DataForm = dfForm, Errors = loadingArgs.Errors, InfoSource = this };
                    DxDForm.DfTemplateLayout.CreateLayout(layoutArgs);
                    if (layoutArgs.HasErrors)              // Zde jsou i chyby sdílené z procesu Loading
                        DxComponent.ShowMessageWarning($"Zadaný dokument '{fileFrmXml}' obsahuje chyby:\r\n{loadingArgs.ErrorsText}");
                    _ApplyDfForm(dfForm);
                }
                else
                {
                    DxComponent.ShowMessageWarning($"Zadaný dokument '{fileFrmXml}' nemá odpovídající FormatVersion='4', jde o '{dxInfo.FormatVersion}'.");
                }
            }
            catch (Exception ex)
            {
                DxComponent.ShowMessageException(ex);
            }

            // Najde definici nested šablony (v adresáři kde je šablona) a vrátí její obsah
            string loadNested(string name)
            {
                string nestedFrmXml = (!String.IsNullOrEmpty(name) ? System.IO.Path.Combine(pathFrmXml, name) : null);
                if (!System.IO.File.Exists(nestedFrmXml)) return null;         // Neexistující nested šablona
                return System.IO.File.ReadAllText(nestedFrmXml);
            }
        }
        /// <summary>
        /// Vytvoří dataform pro dodaná data ve formátu <see cref="DfForm"/> = načtená ze souboru *.frm.xml
        /// </summary>
        /// <param name="dfForm"></param>
        private void _ApplyDfForm(DfForm dfForm)
        {
            _RemoveMainControls();

            __DataFormId++;
            var dataFormPanel = new DxDForm.DxDataFormPanel() { Dock = DockStyle.Fill };
            dataFormPanel.TestPainting = this.TestPainting;
            dataFormPanel.GotFocus += DxDataForm_GotFocus;

            dataFormPanel.DataForm.DfForm = dfForm;
            dataFormPanel.DataForm.DataFormRows.Store(_CreateSampleRows(dfForm));

            DxMainPanel.Controls.Add(dataFormPanel);

            _DataFormLayoutId = null;
            _DxDataFormPanel = dataFormPanel;

            _RefreshTitle();
            RefreshStatusCurrent(true);
        }
        #endregion
        #region DataForm testovací ručně tvořený
        /// <summary>
        /// Přidá DataForm s daným obsahem
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="sampleRows"></param>
        private void _AddDataFormSample(string layoutId, int sampleRows)
        {
            _RemoveMainControls();

            __DataFormId++;
            var dataFormPanel = new DxDForm.DxDataFormPanel() { Dock = DockStyle.Fill };
            dataFormPanel.TestPainting = this.TestPainting;
            dataFormPanel.GotFocus += DxDataForm_GotFocus;

            dataFormPanel.DataForm.DataFormLayout.Store(_CreateSampleLayout(layoutId, sampleRows));
            dataFormPanel.DataForm.DataFormRows.Store(_CreateSampleRows(layoutId, sampleRows));

            DxMainPanel.Controls.Add(dataFormPanel);

            _DataFormLayoutId = layoutId;
            _DxDataFormPanel = dataFormPanel;
        
            _RefreshTitle();
            RefreshStatusCurrent(true);
        }
        /// <summary>
        /// Změní nějaká data v dataformu
        /// </summary>
        private void _ChangeDataInDataForm()
        {
            var sampleId = _DataFormLayoutId;
            if (_DxDataFormPanel is null || sampleId is null) return;

            var rows = _DxDataFormPanel.DataForm.DataFormRows;

        }
        /// <summary>
        /// Do clipboardu připraví definici typu
        /// </summary>
        private void PrepareClipboard()
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                StringBuilder sb = new StringBuilder();
                // type header:
                sb.AppendLine("    <xs:simpleType name=\"color_name_enum\">");
                sb.AppendLine("        <xs:union memberTypes=\"xs:string\">");
                sb.AppendLine("            <xs:simpleType>");
                sb.AppendLine("                <xs:restriction base=\"xs:string\">");
                sb.AppendLine("");

                // type values:
                var colorType = typeof(Color);
                var properties = colorType.GetProperties().Where(p => p.CanRead && p.PropertyType == colorType && p.GetGetMethod().IsStatic).ToList();
                properties.Sort((a, b) => String.Compare(a.Name, b.Name));
                foreach (var property in properties)
                {
                    var name = property.Name;
                    var value = property.GetValue(null) as Color?;
                    string rgb = "#" + value.Value.R.ToString("X2") + value.Value.G.ToString("X2") + value.Value.B.ToString("X2");
                    var line = $"                    <xs:enumeration value=\"{name}\"><xs:annotation><xs:documentation xml:lang=\"cs-cz\">RGB: {rgb}</xs:documentation></xs:annotation></xs:enumeration>";
                    sb.AppendLine(line);
                }

                // type footer:
                sb.AppendLine("                </xs:restriction>");
                sb.AppendLine("            </xs:simpleType>");
                sb.AppendLine("        </xs:union>");
                sb.AppendLine("    </xs:simpleType>");

                Clipboard.SetText(sb.ToString());
            }
        }
        private void DxDataForm_GotFocus(object sender, EventArgs e)
        {
            if (!_DxShowTimeSpan.HasValue && _DxShowTimeStart.HasValue)
            {
                _DxShowTimeSpan = DateTime.Now - _DxShowTimeStart.Value;
                RefreshStatusCurrent(false);
            }
        }
        /// <summary>
        /// Používat testovací vykreslování
        /// </summary>
        public bool TestPainting
        { 
            get { return __TestPainting; } 
            set 
            {
                __TestPainting = value;
                if (this._DxDataFormPanel != null)
                    this._DxDataFormPanel.TestPainting = value;
            }
        }
        private bool __TestPainting;
        /// <summary>
        /// Odebere DataForm <see cref="_DxDataFormPanel"/> pokud existuje
        /// </summary>
        private void _RemoveDxDataForm()
        {
            var dataForm = _DxDataFormPanel;
            if (dataForm != null)
            {
                if (DxMainPanel.Controls.Contains(dataForm))
                    DxMainPanel.Controls.Remove(dataForm);
                dataForm.GotFocus -= DxDataForm_GotFocus;
                dataForm.Dispose();
                _DxDataFormPanel = null;
            }
            _DataFormLayoutId = null;
            _DxShowTimeStart = null;
            _DxShowTimeSpan = null;
        }
        /// <summary>
        /// Instance dataformu
        /// </summary>
        private DxDForm.DxDataFormPanel _DxDataFormPanel;
        /// <summary>
        /// ID vzorku s daty, který je právě zobrazen. Null pokud není žádný.
        /// </summary>
        private string _DataFormLayoutId;
        /// <summary>
        /// Čas zahájení
        /// </summary>
        private DateTime? _DxShowTimeStart;
        /// <summary>
        /// Doba prvního zobrazení
        /// </summary>
        private TimeSpan? _DxShowTimeSpan;
        #endregion
        #region Layouty
        /// <summary>
        /// Vytvoří a vrátí layout daného ID
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="sampleRows"></param>
        /// <returns></returns>
        private List<DxLData.LayoutControl> _CreateSampleLayout(string layoutId, int sampleRows)
        {
            var result = new List<DxLData.LayoutControl>();

            string[] chartTypes = new string[]
            {
                "svgimages/chart/charttype_bar3d.svg",
                "svgimages/chart/charttype_bar3dstacked100.svg",
                "svgimages/chart/charttype_barstacked100.svg",
                "svgimages/chart/charttype_bubble.svg",
                "svgimages/chart/charttype_doughnut3d.svg",
                "svgimages/chart/charttype_funnel3d.svg",
                "svgimages/chart/charttype_histogram.svg",
                "svgimages/chart/charttype_line3d.svg",
                "svgimages/chart/charttype_manhattanbar.svg",
                "svgimages/chart/charttype_pie3d.svg"
            };

            int leftB = 14;
            int left, leftM, left3, topM;
            int top = 0;
            switch (layoutId)
            {
                case "A":
                    top = 18;
                    left = leftB;
                    addItemPairT("datum", "Datum:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 70, 20);
                    addItemPairT("reference", "Reference:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 120, 20);
                    addItemPairT("nazev", "Název:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 250, 20);
                    leftM = left;
                    topM = top;
                    left = leftB; top += 44; 
                    addItemPairT("pocet", "Počet:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 110, 20);
                    addItemPairT("cena1", "Cena 1ks:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 80, 20);
                    addItemType("button_open", DxDForm.DxRepositoryEditorType.Button, "Otevři", left + 90, 140, null, top + 6, 44, null, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.IconName] = "svgimages/reports/preview.svg";
                    });

                    left = leftB; top += 44;
                    addItemPairT("sazbadph", "Sazba DPH:", DxDForm.DxRepositoryEditorType.ToggleSwitch, top, ref left, 120, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.Label] = "Je to pochoutka";
                        item.Content[DxDData.DxDataFormProperty.IsEnabled] = false;
                        item.Content[DxDData.DxDataFormProperty.FontStyle] = FontStyle.Bold;
                        item.Content[DxDData.DxDataFormProperty.CheckBoxLabelTrue] = "SVÍTÍ!";
                    });
                    addItemPairT("cenacelk", "Cena cel.:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 70, 20);
                    addItemPairT("filename", "Dokument:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 250, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.TextBoxButtons] = new DxDData.TextBoxButtonProperties("Down;Plus");
                    });
                    left = leftB; top += 44;
                    addItemPairT("relation", "Vztah:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 456, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.TextBoxButtons] = new DxDData.TextBoxButtonProperties("SpinLeft;Ellipsis;SpinRight");
                    });

                    left = leftB; top += 44;
                    addItemPairT("valuecombo", "Tracker:", DxDForm.DxRepositoryEditorType.ComboListBox, top, ref left, 180, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.ComboBoxItems] = new DxDData.ImageComboBoxProperties($"A,Anglicky;B,Belgicky;C,Chorvatsky;D,Dánsky;E,Estonsky;F,Finsky;G,Grónsky;H,Holandsky;I,Italsky;J,Japonsky;K,Korejsky;L,Latinsky;M,Moravsky;N,Norsky;O,Otevřeně;P,Pražsky;R,Rakousky;S,Slovensky;T,Turecky;U,Uzavřeně;V,Vizigótsky");
                    });
                    addItemPairT("towncombo", "Výběr města:", DxDForm.DxRepositoryEditorType.ImageComboListBox, top, ref left, 268, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.ComboBoxItems] = new DxDData.ImageComboBoxProperties($"A,Praha,{chartTypes[0]};B,Brno,{chartTypes[1]};C,Chrudim,{chartTypes[2]};D,Domažlice,{chartTypes[3]};H,Horní Planá,{chartTypes[4]};" +
                            $"K,Kostelec nad Orlicí,{chartTypes[5]};L,Lomnice nad Popelkou,{chartTypes[6]};M,Mariánské Lázně,{chartTypes[7]};O,Ostrava,{chartTypes[8]};P,ParDubice,{chartTypes[9]}");
                    });

                    left = leftM; top = topM;
                    addItemPairT("poznamka", "Poznámka:", DxDForm.DxRepositoryEditorType.EditBox, top, ref left, 350, 198);
                    break;
                case "B":
                    top = 16;
                    left = leftB;
                    addItemPairL("datum", "Datum:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 65, 20);
                    addItemPairL("reference", "Reference:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 145, 20);
                    left3 = left;
                    addItemPairL("document1", "Název:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 250, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.TextBoxButtons] = new DxDData.TextBoxButtonProperties("svgimages/richedit/preview.svg;svgimages/richedit/insertequationcaption.svg");
                    });

                    top = 44;
                    left = leftB;
                    addItemPairL("pocet", "Počet:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 90, 20);
                    addItemPairL("cena1", "Cena 1ks:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 120, 20);
                    left = left3;
                    addItemPairL("document2", "Soubor:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 250, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.TextBoxButtons] = new DxDData.TextBoxButtonProperties("svgimages/xaf/modeleditor_hyperlink.svg");
                    });

                    top = 72;
                    left = leftB;
                    addItemPairL("sazbadph", "Sazba DPH:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 140, 20);
                    addItemPairL("cenacelk", "Cena celkem:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 70, 20);
                    left = left3;
                    addItemPairL("document3", "Protokol:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 250, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormProperty.TextBoxButtons] = new DxDData.TextBoxButtonProperties("Plus;Clear");
                    });

                    top = 16;
                    left = 740;
                    addItemPairL("poznamka", "Poznámka:", DxDForm.DxRepositoryEditorType.EditBox, top, ref left, 350, 78);
                    break;
                case "C":
                    addItemType("id1", DxDForm.DxRepositoryEditorType.TextBox, null, 0, 75, null, 2, 20, null);
                    addItemType("id2", DxDForm.DxRepositoryEditorType.TextBox, null, 80, 40, null, 2, 20, null);
                    addItemType("id3", DxDForm.DxRepositoryEditorType.TextBox, null, 125, 40, null, 2, 20, null);
                    addItemType("id4", DxDForm.DxRepositoryEditorType.TextBox, null, 170, 150, null, 2, 20, null);
                    addItemType("id5", DxDForm.DxRepositoryEditorType.TextBox, null, 325, 150, null, 2, 20, null);
                    addItemType("id6", DxDForm.DxRepositoryEditorType.TextBox, null, 480, 100, null, 2, 20, null);
                    addItemType("id7", DxDForm.DxRepositoryEditorType.TextBox, null, 585, 60, null, 2, 20, null);
                    addItemType("id8", DxDForm.DxRepositoryEditorType.TextBox, null, 650, 60, null, 2, 20, null);
                    addItemType("id9", DxDForm.DxRepositoryEditorType.TextBox, null, 715, 200, null, 2, 20, null);
                    addItemType("id0", DxDForm.DxRepositoryEditorType.TextBox, null, 920, 400, null, 2, 20, null);
                    break;
            }

            return result;

            void addItemPairT(string columnId, string labelText, DxDForm.DxRepositoryEditorType columnType, int top, ref int left, int width, int height, Action<DxLData.LayoutControl> modifier = null)
            {
                addItemLabel(columnId + ".label", labelText, left + 3, width - 8, null, top, 18, null, WinDraw.ContentAlignment.MiddleLeft);
                addItemType(columnId, columnType, null, left, width, null, top + 18, height, null, modifier);
                left = left + width + 8;
            }
            void addItemPairL(string columnId, string labelText, DxDForm.DxRepositoryEditorType columnType, int top, ref int left, int width, int height, Action<DxLData.LayoutControl> modifier = null)
            {
                addItemLabel(columnId + ".label", labelText, left, 75, null, top + 2, 18, null, WinDraw.ContentAlignment.MiddleRight);
                left += 80;
                addItemType(columnId, columnType, null, left, width, null, top, height, null, modifier);
                left += (width + 8);
            }
            void addItemLabel(string columnId, string labelText, int? left, int? width, int? right, int? top, int? height, int? bottom, WinDraw.ContentAlignment alignment)
            {
                DxLData.LayoutControl item = new DxLData.LayoutControl()
                {
                    ColumnName = columnId,
                    ColumnType = DxDForm.DxRepositoryEditorType.Label,
                    Label = labelText,
                    DesignBoundsExt = new DxDForm.RectangleExt(left, width, right, top, height, bottom)
                };
                item.SetContent(DxDData.DxDataFormProperty.LabelAlignment, alignment);
                result.Add(item);
            }
            void addItemType(string columnId, DxDForm.DxRepositoryEditorType columnType, string text, int? left, int? width, int? right, int? top, int? height, int? bottom, Action<DxLData.LayoutControl> modifier = null)
            {
                DxLData.LayoutControl item = new DxLData.LayoutControl()
                {
                    ColumnName = columnId,
                    ColumnType = columnType,
                    Label = text,
                    DesignBoundsExt = new DxDForm.RectangleExt(left, width, right, top, height, bottom)
                };
                modifier?.Invoke(item);
                result.Add(item);
            }
        }
        /// <summary>
        /// Vytvoří a vrátí jednotlivé řádky pro layout daného ID
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="sampleRows"></param>
        /// <returns></returns>
        private List<DxDData.DataFormRow> _CreateSampleRows(string layoutId, int sampleRows)
        {
            Randomizer.ActiveWordBook = Randomizer.WordBookType.CampOfSaints;

            var result = new List<DxDData.DataFormRow>();

            Random rand = new Random();

            string aSequence = "ABCDEFGHIJKLMNOPQRSTUVZ";

            for (int r = 0; r < sampleRows; r++)
            {
                switch (layoutId)
                {
                    case "A":
                        addRow(r, "datum;reference;nazev;pocet;cenacelk;filename;valuecombo;towncombo;poznamka", "{dr};Ref {ref};Název {ref};{rnd};{rnd};Dokument {rnd};{A};{A};{memoExt}");
                        break;
                    case "B":
                        addRow(r, "datum;reference;document1;pocet;cena1;document2;document3;poznamka", "{dr};R{ref};Záznam {ref};{rnd};{rnd};{file};{file};{memo}");
                        break;
                    case "C":
                        addRow(r, "id1;id2;id3;id4;id5;id7;id0", "{ref};VYR;SKL;Výroba {ref};Dne {dr};{t};{dr} ==> {rnd}");
                        break;
                }
            }

            return result;

            void addRow(int rId, string columns = null, string values = null)
            {
                var dataRow = new DxDData.DataFormRow() { RowId = rId };

                string rowText = rId.ToString("000");
                string alpha = Randomizer.GetItem(aSequence.ToCharArray()).ToString();                   // Náhodné 1 písmeno ze sekvence aSequence, na místo tokenu {A}
                string dat = DateTime.Now.ToString("d.M.yyyy");
                string tim = DateTime.Now.ToString("HH:mm");
                if (!String.IsNullOrEmpty(columns) && !String.IsNullOrEmpty(values))
                {
                    var cols = columns.Split(';');
                    var vals = values.Split(';');
                    if (cols.Length > 0 && cols.Length == vals.Length)
                    {
                        for (int c = 0; c < cols.Length; c++)
                        {
                            string col = cols[c];
                            string val = vals[c];
                            if (val.Contains("{ref}")) val = val.Replace("{ref}", rowText);
                            if (val.Contains("{d}")) val = val.Replace("{d}", dat);
                            if (val.Contains("{dr}")) val = val.Replace("{dr}", randomDate());
                            if (val.Contains("{t}")) val = val.Replace("{t}", tim);
                            if (val.Contains("{A}")) val = val.Replace("{A}", alpha);
                            if (val.Contains("{rnd}")) val = val.Replace("{rnd}", ((decimal)(rand.Next(10000, 1000000)) / 100m).ToString("### ##0.00").Trim());
                            if (val.Contains("{file}")) val = val.Replace("{file}", randomFile());
                            if (val.Contains("{memo}")) val = val.Replace("{memo}", randomMemo());
                            if (val.Contains("{memoExt}")) val = val.Replace("{memoExt}", randomMemoExt());
                            dataRow.Content[col, DxDData.DxDataFormProperty.Value] = val;
                        }
                    }
                }

                result.Add(dataRow);
            }

            // Náhodné datum -90 +30 dní ku dnešku
            string randomDate()
            {
                var dateRnd = DateTime.Now.AddDays(120d * rand.NextDouble() - 90d);
                return dateRnd.ToString("d.M.yyyy");
            }
            // Náhodný soubor:
            string randomFile()
            {
                return Randomizer.GetSentence(2, 4, false) + "." + Randomizer.GetItem("doc.docx.txt.xls.html.cs.csproj.pdf.png.ico.jpg.mp4.rtf".Split('.'));
            }
            // Náhodná poznámka:
            string randomMemo()
            {
                return Randomizer.GetSentences(2, 7, 3, 5);
            }
            // Náhodná poznámka dlouhá:
            string randomMemoExt()
            {
                return Randomizer.GetSentences(4, 9, 12, 30);
            }
            
        }


        private List<DxDData.DataFormRow> _CreateSampleRows(DfForm dfForm)
        {
            var result = new List<DxDData.DataFormRow>();

            int rowsCount = 1 + ((dfForm.UseNorisClass ?? 1) % 60);
            var controls = DfTemplateLoader.GetAllControls(dfForm);

            result.Add(new DxDData.DataFormRow());
            return result;
        }
        #endregion
        #region IControlInfoSource
        void IControlInfoSource.ValidateControlInfo(Noris.Clients.Win.Components.AsolDX.DataForm.ControlInfo controlInfo)
        {

        }
        #endregion
    }
}
