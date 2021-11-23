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
        /// Vygneruje a vrátí prvek do Ribbonu, který zobrazí this formulář
        /// </summary>
        /// <returns></returns>
        public static IRibbonItem CreateRibbonButton()
        {
            return new DataRibbonItem()
            {
                ItemId = "_SYS__DevExpress_ShowImagePickerForm", Text = "DevExpress Images", ToolTipText = "Otevře okno s nabídkou systémových ikon",
                ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.Large,
                ImageName = "svgimages/icon%20builder/actions_image.svg", ClickAction = ShowImagePicker
            };
        }
        /// <summary>
        /// Zajistí zobrazení this formuláře jako reakce na kliknutí na Ribbon
        /// </summary>
        /// <param name="menuItem"></param>
        private static void ShowImagePicker(IMenuItem menuItem)
        {
            ImagePickerForm.ShowForm();
        }
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
            this.ClientSize = new System.Drawing.Size(800, 450);
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
            string name = Random.GetItem(new string[]
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
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ" };
            pages.Add(page);
            page.Groups.Add(DxRibbonControl.CreateSkinIGroup("DESIGN"));

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);
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
