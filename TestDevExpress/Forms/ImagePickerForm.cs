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
                form.ShowDialog(owner);
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
            // 
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
            _MainPanel = DxComponent.CreateDxPanel(this, WF.DockStyle.Fill, DXE.Controls.BorderStyles.NoBorder);
        }
        /// <summary>
        /// Inicializace objektu Ribbon a Statusbar
        /// </summary>
        private void InitRibbons()
        {
            _DxRibbonControl = new DxRibbonControl();
            _DxRibbonControl.Items.Clear();

            this.Ribbon = _DxRibbonControl;
            this.StatusBar = new DXB.Ribbon.RibbonStatusBar();
            this.StatusBar.Ribbon = this.Ribbon;

            RibbonFillItems();
            StatusFillItems();

            this.Controls.Add(this.Ribbon);
            this.Controls.Add(this.StatusBar);
        }
        DxPanelControl _MainPanel;
        DxRibbonControl _DxRibbonControl;
        #endregion
        #region Ribbon a StatusBar
        /// <summary>
        /// Naplní prvky do Ribbonu, zaháčkuje eventhandlery
        /// </summary>
        private void RibbonFillItems()
        {
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;

            DXB.Ribbon.RibbonPage page1 = new DXB.Ribbon.RibbonPage("SKIN");
            this.Ribbon.Pages.Add(page1);

            DXB.Ribbon.RibbonPageGroup group10 = new DXB.Ribbon.RibbonPageGroup("VOLBA SKINU A PALETY");
            page1.Groups.Add(group10);

            group10.ItemLinks.Add(new DXB.SkinDropDownButtonItem());
            group10.ItemLinks.Add(new DXB.SkinPaletteDropDownButtonItem());
            group10.ItemLinks.Add(new DXB.SkinPaletteRibbonGalleryBarItem());
        }
        /// <summary>
        /// Naplní prvky do StatusBaru
        /// </summary>
        private void StatusFillItems()
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
            _MainPanel.Controls.Add(_ImagePickerList);
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
