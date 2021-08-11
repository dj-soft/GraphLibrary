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

            DXB.Ribbon.RibbonPage pageSkin = new DXB.Ribbon.RibbonPage("SKIN");
            this.Ribbon.Pages.Add(pageSkin);

            pageSkin.Groups.Add(DxRibbonControl.CreateSkinGroup());
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
