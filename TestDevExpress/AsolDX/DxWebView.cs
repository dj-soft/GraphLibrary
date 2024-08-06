using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel obsahující <see cref="MsWebView"/>
    /// </summary>
    public class DxWebViewPanel : DxPanelControl
    {
        #region Konstruktor, tvorba vnitřních controlů, DoLayout, Dispose, proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxWebViewPanel()
        {
            CreateContent();
        }
        /// <summary>
        /// Vytvoří kompletní obsah a vyvolá <see cref="DoLayout"/>
        /// </summary>
        protected void CreateContent()
        {
            this.SuspendLayout();

            __ToolPanel = new DxPanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            __BackButton = DxComponent.CreateDxSimpleButton(3, 3, 24, 24, __ToolPanel, "", __BackClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameBackDisabled);
            __NextButton = DxComponent.CreateDxSimpleButton(30, 3, 24, 24, __ToolPanel, "", __NextClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameForwardDisabled);
            __RefreshButton = DxComponent.CreateDxSimpleButton(63, 3, 24, 24, __ToolPanel, "", __RefreshClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameRefreshDisabled);
            __AdressText = DxComponent.CreateDxTextEdit(96, 3, 250, __ToolPanel);
            __GoToButton = DxComponent.CreateDxSimpleButton(340, 3, 24, 24, __ToolPanel, "", __GoToClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameGoTo);
            __BackButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            __BackButton.TabStop = false;
            __NextButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            __NextButton.TabStop = false;
            __RefreshButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            __RefreshButton.TabStop = false;
            __AdressText.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            __AdressText.TabStop = false;
            __GoToButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            __GoToButton.TabStop = false;

            __MsWebView = new MsWebView();

            __StatusBar = new DxPanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            __StatusText = DxComponent.CreateDxLabel(6, 3, 250, __StatusBar, "Stavová informace...", LabelStyleType.Info, hAlignment: DevExpress.Utils.HorzAlignment.Near);

            this.Controls.Add(__ToolPanel);
            this.Controls.Add(__MsWebView);
            this.Controls.Add(__StatusBar);
            this.ResumeLayout(false);
            this.DoLayout();
        }
        /// <summary>
        /// Po změně velikosti vyvoláme <see cref="DoLayout"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Rozmístí vnitřní prvky podle prostoru a podle požadavků
        /// </summary>
        internal void DoLayout()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(DoLayout));
            }
            else
            {
                var size = this.ClientSize;
                int x = 0;
                int w = size.Width;
                int y = 0;
                int b = size.Height;

                bool isToolbarVisible = this.MsWebProperties.IsToolbarVisible;
                this.__ToolPanel.Visible = isToolbarVisible;
                if (isToolbarVisible)
                {
                    int bs = DxComponent.ZoomToGui(24);
                    int th = DxComponent.ZoomToGui(32);
                    int by = (th - bs) / 2;
                    int bx = by;
                    int dx = bs * 9 / 8;
                    int sx = dx - bs;
                    __BackButton.Bounds = new System.Drawing.Rectangle(bx, by, bs, bs);
                    bx += dx;
                    __NextButton.Bounds = new System.Drawing.Rectangle(bx, by, bs, bs);
                    bx += dx;
                    __RefreshButton.Bounds = new System.Drawing.Rectangle(bx, by, bs, bs);
                    bx += dx;

                    int br = w - dx;
                    __GoToButton.Bounds = new System.Drawing.Rectangle(br, by, bs, bs);

                    int ah = __AdressText.Bounds.Height;
                    int ay = by + bs - ah;
                    __AdressText.Bounds = new System.Drawing.Rectangle(bx, ay, (br - bx - sx), ah);

                    __ToolPanel.Bounds = new System.Drawing.Rectangle(x, y, w, th);
                    y = y + th;
                }

                bool isStatusVisible = this.MsWebProperties.IsStatusRowVisible;
                this.__StatusBar.Visible = isStatusVisible;
                if (isStatusVisible) 
                {
                    int th = this.__StatusText.Height;
                    int sh = th + 4;
                    b = b - sh;
                    this.__StatusBar.Bounds = new System.Drawing.Rectangle(x, b, w, sh);
                    this.__StatusText.Bounds = new System.Drawing.Rectangle(6, 2, w - 12, th);
                }

                int wh = b - y;
                this.__MsWebView.Bounds = new System.Drawing.Rectangle(x, y, w, wh);
            }
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

        }
        private DxPanelControl __ToolPanel;
        private DxSimpleButton __BackButton;
        private DxSimpleButton __NextButton;
        private DxSimpleButton __RefreshButton;
        private DxTextEdit __AdressText;
        private DxSimpleButton __GoToButton;
        private MsWebView __MsWebView;
        private DxPanelControl __StatusBar;
        private DxLabelControl __StatusText;

        private const string ImageNameBackEnabled = "images/xaf/templatesv2images/action_navigation_history_back.svg";
        private const string ImageNameBackDisabled = "images/xaf/templatesv2images/action_navigation_history_back_disabled.svg";
        private const string ImageNameForwardEnabled = "images/xaf/templatesv2images/action_navigation_history_forward.svg";
        private const string ImageNameForwardDisabled = "images/xaf/templatesv2images/action_navigation_history_forward_disabled.svg";
        private const string ImageNameRefreshEnabled = "images/xaf/templatesv2images/action_refresh.svg";
        private const string ImageNameRefreshDisabled = "images/xaf/templatesv2images/action_refresh_disabled.svg";
        private const string ImageNameGoTo = "images/xaf/templatesv2images/action_simpleaction.svg";
        private const string ImageNameValidateEnabled = "images/xaf/templatesv2images/action_validation_validate.svg";
        private const string ImageNameValidateDisabled = "images/xaf/templatesv2images/action_validation_validate_disabled.svg";
        #endregion
        #region Privátní život
        private void __BackClick(object sender, EventArgs args) { }
        private void __NextClick(object sender, EventArgs args) { }
        private void __RefreshClick(object sender, EventArgs args) { }
        private void __AdressEntered(object sender, EventArgs args) { }
        private void __AdressLeaved(object sender, EventArgs args) { }
        private void __GoToClick(object sender, EventArgs args) { }
        #endregion


        #region Public vlastnosti
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení pod jedním místem
        /// </summary>
        public MsWebView.PropertiesInfo MsWebProperties { get { return __MsWebView.Properties; } }

        #endregion
    }
    /// <summary>
    /// Potomek třídy <see cref="Microsoft.Web.WebView2.WinForms.WebView2"/>
    /// </summary>
    public class MsWebView : Microsoft.Web.WebView2.WinForms.WebView2
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MsWebView()
        {
            __Properties = new PropertiesInfo(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            __Properties?.Dispose();
            __Properties = null;
        }
        /// <summary>
        /// Parent typovaný na <see cref="DxWebViewPanel"/>, nebo null
        /// </summary>
        protected DxWebViewPanel ParentWebView { get { return this.Parent as DxWebViewPanel; } }
        #region class PropertiesInfo : Souhrn vlastností, tak aby byly k dosažení pod jedním místem
        /// <summary>
        /// Souhrn vlastností, tak aby byly k dosažení pod jedním místem
        /// </summary>
        public PropertiesInfo Properties { get { return __Properties; } } private PropertiesInfo __Properties;
        /// <summary>
        /// Definice vlastností <see cref="MsWebView"/>
        /// </summary>
        public class PropertiesInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            public PropertiesInfo(MsWebView owner)
            {
                __Owner = owner;
                _InitValues();
            }
            /// <summary>
            /// Vlastník = <see cref="MsWebView"/>
            /// </summary>
            private MsWebView __Owner;
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                __Owner = null;
            }
            /// <summary>
            /// Vyvolá Layout v parentu <see cref="DxWebViewPanel.DoLayout()"/>
            /// </summary>
            private void _ParentDoLayout() { __Owner?.ParentWebView?.DoLayout(); }
            /// <summary>
            /// Pokud se dodaná hodnota <paramref name="value"/> liší od hodnoty v proměnné <paramref name="variable"/>, 
            /// pak do proměnné vloží hodnotu a vyvolá <see cref="_ParentDoLayout"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="variable"></param>
            private void _SetValueDoLayout<T>(T value, ref T variable)
            {
                if (Object.Equals(value, variable)) return;
                variable = value;
                _ParentDoLayout();
            }
            /// <summary>
            /// Nastaví výchozí hodnoty
            /// </summary>
            private void _InitValues()
            {
                __IsToolbarVisible = true;
                __IsBackNextButtonsVisible = true;
                __IsRefreshButtonVisible = true;
                __IsAdressButtonVisible = true;
                __IsAdressButtonEditable = true;
                __IsStatusRowVisible = true;
            }
            /// <summary>
            /// Je toolbar viditelný?
            /// Default = true;
            /// </summary>
            public bool IsToolbarVisible { get { return __IsToolbarVisible; } set { _SetValueDoLayout(value, ref __IsToolbarVisible); } } private bool __IsToolbarVisible;
            /// <summary>
            /// Jsou buttony Back a Next viditelné?
            /// Default = true;
            /// </summary>
            public bool IsBackNextButtonsVisible { get { return __IsBackNextButtonsVisible; } set { _SetValueDoLayout(value, ref __IsBackNextButtonsVisible); } } private bool __IsBackNextButtonsVisible;
            /// <summary>
            /// Je button Refresh viditelný?
            /// Default = true;
            /// </summary>
            public bool IsRefreshButtonVisible { get { return __IsRefreshButtonVisible; } set { _SetValueDoLayout(value, ref __IsRefreshButtonVisible); } } private bool __IsRefreshButtonVisible;
            /// <summary>
            /// Je adresní pole viditelné?
            /// Default = true;
            /// </summary>
            public bool IsAdressButtonVisible { get { return __IsAdressButtonVisible; } set { _SetValueDoLayout(value, ref __IsAdressButtonVisible); } } private bool __IsAdressButtonVisible;
            /// <summary>
            /// Je adresní pole editovatelné? Pak je i zobrazen button GoTo
            /// Default = true;
            /// </summary>
            public bool IsAdressButtonEditable { get { return __IsAdressButtonEditable; } set { _SetValueDoLayout(value, ref __IsAdressButtonEditable); } } private bool __IsAdressButtonEditable;
            /// <summary>
            /// Je stavový řádek viditelný?
            /// Default = true;
            /// </summary>
            public bool IsStatusRowVisible { get { return __IsStatusRowVisible; } set { _SetValueDoLayout(value, ref __IsStatusRowVisible); } } private bool __IsStatusRowVisible;
        }
        #endregion
    }

}
