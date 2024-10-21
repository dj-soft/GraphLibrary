using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using WF = System.Windows.Forms;

using DXB = DevExpress.XtraBars;
using DXE = DevExpress.XtraEditors;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář zobrazující zdroje obrázků DevExpress
    /// </summary>
    public class ImagePickerForm : DxRibbonForm
    {
        #region Konstruktor a static tvorba
        /// <summary>
        /// Konstuktor
        /// </summary>
        public ImagePickerForm()
        {
            InitializeComponent();
            InitDevExpressComponents();
        }
        /// <summary>
        /// Zobrazí ImagePicker
        /// </summary>
        /// <param name="owner"></param>
        public static void ShowForm(WF.IWin32Window owner = null)
        {
            using (ImagePickerForm form = new ImagePickerForm())
            {
                try
                {
                    form.ShowDialog(owner);
                }
                catch (Exception exc)
                {
                    Noris.Clients.Win.Components.DialogArgs args = new Noris.Clients.Win.Components.DialogArgs();
                    args.Title = "Error";
                    args.MessageText = exc.Message;
                    args.PrepareButtons(WF.MessageBoxButtons.OK);
                    Noris.Clients.Win.Components.DialogForm.ShowDialog(args);
                }
            }
        }
        /// <summary>
        /// Vygneruje a vrátí prvek do Ribbonu, který zobrazí this formulář.
        /// Jeho ID = <see cref="RibbonBarItemId"/>
        /// </summary>
        /// <returns></returns>
        public static IRibbonItem CreateRibbonButton()
        {
            return new DataRibbonItem()
            {
                ItemId = RibbonBarItemId, Text = "DevExpress Images", ToolTipText = "Otevře okno s nabídkou systémových ikon",
                ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.Large,
                ImageName = "svgimages/icon%20builder/actions_image.svg", ClickAction = ShowImagePicker
            };
        }
        /// <summary>
        /// ID buttonu, který vytváří metoda <see cref="CreateRibbonButton"/>
        /// </summary>
        public const string RibbonBarItemId = "_SYS__DevExpress_ShowImagePickerForm";
        /// <summary>
        /// Zajistí zobrazení this formuláře jako reakce na kliknutí na Ribbon
        /// </summary>
        /// <param name="menuItem"></param>
        private static void ShowImagePicker(IMenuItem menuItem)
        {
            ImagePickerForm.ShowForm();
        }
        #endregion
        #region Ukládání a obnova pozice okna
        /// <summary>
        /// Pokusí se z konfigurace najít a načíst string popisující pozici okna.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Za toto jméno přidá suffix (začíná podtržítkem a obsahuje XML validní znaky) a vyhledá konfiguraci se suffixem.<br/>
        /// 3. Pokud nenajde konfiguraci se suffixem, vyhledá konfiguraci bez suffixu = obecná, posledně použití (viz <see cref="PositionSaveToConfig(string, string)"/>).<br/>
        /// 4. Nalezený string je ten, který byl uložen v metodě <see cref="PositionSaveToConfig(string, string)"/> a je roven parametru 'positionData'. Pokud položku v konfiguraci nenajde, vrátí null (nebo prázdný string).
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// <para/>
        /// Konverze formátů: Pokud v konfiguraci budou uložena stringová data ve starším formátu, než dokáže obsloužit zpracující třída <see cref="FormStatusInfo"/>, pak konverzi do jejího formátu musí zajistit aplikační kód (protože on ví, jak zpracovat starý formát).<br/>
        /// <b><u>Postup:</u></b><br/>
        /// 1. Po načtení konfigurace se lze dotázat metodou <see cref="FormStatusInfo.IsPositionDataValid(string)"/>, zda načtená data jsou validní.<br/>
        /// 2. Pokud nejsou validní, pak je volající aplikace zkusí analyzovat svým starším (legacy) postupem na prvočinitele;<br/>
        /// 3. A pokud je úspěšně rozpoznala, pak ze základních dat sestaví validní konfirurační string s pomocí metody <see cref="FormStatusInfo.CreatePositionData(bool?, WF.FormWindowState?, Rectangle?, Rectangle?)"/>.<br/>
        /// </summary>
        /// <param name="nameSuffix">Suffix ke jménu konfigurace, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory</param>
        /// <returns></returns>
        protected override string PositionLoadFromConfig(string nameSuffix)
        {
            // INFO: tato metoda (a párová PositionSaveToConfig) neproběhne, protože v property PositionConfigName vracíme jméno konfigurace, 
            //       a předek třídy (resp. support třída FormStatusInfo) řeší načítání a ukládání konfigurace defaultně.
            //  Zdejší kód je zde jen pro ilustraci funkce:
            string positionData = DxComponent.Settings.GetRawValue("FormPosition", PositionConfigName + nameSuffix);
            if (String.IsNullOrEmpty(positionData))
                positionData = DxComponent.Settings.GetRawValue("FormPosition", PositionConfigName);
            return positionData;
        }
        /// <summary>
        /// Do konfigurace uloží dodaná data o pozici okna '<paramref name="positionData"/>'.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Jednak uloží data <paramref name="positionData"/> přímo do položky konfigurace pod svým vlastním jménem bez suffixu = data obecná pro libovolnou konfiguraci monitorů.<br/>
        /// 3. A dále uloží tato data do položky konfigurace, kde za svoje jméno přidá dodaný suffix <paramref name="nameSuffix"/> = tato hodnota se použije po restore na shodné konfiguraci monitorů.<br/>
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// </summary>
        /// <param name="positionData"></param>
        /// <param name="nameSuffix"></param>
        protected override void PositionSaveToConfig(string positionData, string nameSuffix)
        {
            // INFO: tato metoda (a párová PositionSaveToConfig) neproběhne, protože v property PositionConfigName vracíme jméno konfigurace, 
            //       a předek třídy (resp. support třída FormStatusInfo) řeší načítání a ukládání konfigurace defaultně.
            //  Zdejší kód je zde jen pro ilustraci funkce:
            DxComponent.Settings.SetRawValue("FormPosition", PositionConfigName, positionData);
            DxComponent.Settings.SetRawValue("FormPosition", PositionConfigName + nameSuffix, positionData);
        }
        /// <summary>
        /// Jméno konfigurace v subsystému AsolDX.
        /// Pokud bude zde vráceno neprázdné jméno, pak načtení a uložení konfigurace okna zajistí sama třída, která implementuje <see cref="IFormStatusWorking"/>.
        /// Pokud nebude vráceno jméno, budou používány metody <see cref="DxRibbonBaseForm.PositionLoadFromConfig(string)"/> a <see cref="DxRibbonBaseForm.PositionSaveToConfig(string, string)"/>.
        /// </summary>
        protected override string PositionConfigName { get { return "ImagePickerForm"; } }
        #endregion
        #region WinForm designer
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            if (!this.PositionIsFromConfig)
            {
                this.StartPosition = WF.FormStartPosition.CenterScreen;
                this.ClientSize = new System.Drawing.Size(500, 920);
            }
            this.Text = "DevExpress Resources";
        }
        #endregion
        #endregion
        #region Příprava formuláře
        private void InitDevExpressComponents()
        {
            InitDevExpress();
            InitForm();
            InitFrames();
            InitRibbons();
            InitList();
        }
        /// <summary>
        /// Při zobrazení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
        }
        /// <summary>
        /// Inicializace vlastností DevExpress, inciace skinu podle konfigurace
        /// </summary>
        private void InitDevExpress()
        {
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();

            DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpress_StyleChanged;
        }
        /// <summary>
        /// Po změně SKinu uživatelem se uloží do konfigurace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpress_StyleChanged(object sender, EventArgs e)
        {
            string skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
            string paletteName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSvgPaletteName;
        }
        /// <summary>
        /// Inicializace vlastností formuláře
        /// </summary>
        private void InitForm()
        {
            string name = Randomizer.GetItem(new string[]
            {
                "pic_0/win/dashboard/poznamkovy_blok",
                "«devav/actions/printexcludeevaluations.svg»«devav/actions/add.svg<60.60.60.60>»",
                "devav/actions/support_32x32.png",
                "pic/alert-filled-large.svg"
            });

            DxComponent.ApplyIcon(this, name, ResourceImageSizeType.Large, true);
        }
        /// <summary>
        /// Inicializace vnitřního layoutu - splitpanel
        /// </summary>
        private void InitFrames()
        {
        }
        /// <summary>
        /// Inicializace objektu Ribbon a Statusbar
        /// </summary>
        private void InitRibbons()
        {
        }
        #endregion
        #region Ribbon a StatusBar
        /// <summary>
        /// Naplní prvky do Ribbonu, zaháčkuje eventhandlery
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            pages.Add(this.CreateRibbonHomePage(FormRibbonDesignGroupPart.SkinButton | FormRibbonDesignGroupPart.PaletteGallery));
            this.DxRibbon.AddPages(pages, true);
        }
        /// <summary>
        /// Naplní prvky do StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            _StatusInfoTextItem = new DXB.BarStaticItem() { Caption = "Stavový řádek", AutoSize = DXB.BarStaticItemSize.Spring };
            this.StatusBar.ItemLinks.Add(_StatusInfoTextItem);
        }
        /// <summary>
        /// Text zobrazený ve stavovém řádku
        /// </summary>
        private string _StatusText { get { return _StatusInfoTextItem.Caption; } set { _StatusInfoTextItem.Caption = value ?? ""; } }
        private DXB.BarStaticItem _StatusInfoTextItem;
        #endregion
        #region List = DxImagePicker
        private void InitList()
        {
            _ImagePickerList = new DxImagePickerListBox() { Dock = WF.DockStyle.Fill };
            _ImagePickerList.StatusTextChanged += _ImagePickerList_StatusTextChanged;
            DxMainPanel.Controls.Add(_ImagePickerList);
            RefreshStatusText();
        }

        private void _ImagePickerList_StatusTextChanged(object sender, EventArgs e)
        {
            RefreshStatusText();
        }
        private void RefreshStatusText()
        {
            this._StatusText = _ImagePickerList.StatusText;
        }

        private DxImagePickerListBox _ImagePickerList;
        #endregion
    }
}
