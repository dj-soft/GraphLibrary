using DevExpress.Pdf.ContentGeneration;
using DevExpress.PivotGrid.Internal.ThinClientDataSource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel obsahující <see cref="MsWebView"/> a jednoduchý toolbar (Back - Forward - Refresh - Adresa - Go)
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
            __BackButton = DxComponent.CreateDxSimpleButton(3, 3, 24, 24, __ToolPanel, "", _BackClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameBackDisabled);
            __ForwardButton = DxComponent.CreateDxSimpleButton(30, 3, 24, 24, __ToolPanel, "", _ForwardClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameForwardDisabled);
            __RefreshButton = DxComponent.CreateDxSimpleButton(63, 3, 24, 24, __ToolPanel, "", _RefreshClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameRefreshDisabled);
            __AdressText = DxComponent.CreateDxTextEdit(96, 3, 250, __ToolPanel);
            __GoToButton = DxComponent.CreateDxSimpleButton(340, 3, 24, 24, __ToolPanel, "", _GoToClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameGoTo);
            __BackButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __BackButton.TabStop = false;
            __ForwardButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __ForwardButton.TabStop = false;
            __RefreshButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __RefreshButton.TabStop = false;
            __AdressText.Enter += _AdressEntered;
            __AdressText.KeyDown += _AdressKeyDown;
            __AdressText.KeyPress += _AdressKeyPress;
            __AdressText.Leave += _AdressLeaved;
            __AdressText.LostFocus += _AdressLostFocus;
            __AdressText.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __AdressText.TabStop = false;
            __GoToButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __GoToButton.TabStop = false;

            __MsWebView = new MsWebView();
            _MsWebInitEvents();

            __PictureWeb = new PictureBox() { Visible = false, BorderStyle = System.Windows.Forms.BorderStyle.None };

            __StatusBar = new DxPanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            __StatusText = DxComponent.CreateDxLabel(6, 3, 250, __StatusBar, "Stavová informace...", LabelStyleType.Info, hAlignment: DevExpress.Utils.HorzAlignment.Near);

            this.Controls.Add(__ToolPanel);
            this.Controls.Add(__PictureWeb);
            this.Controls.Add(__MsWebView);
            this.Controls.Add(__StatusBar);
            this.ResumeLayout(false);
            this._DoLayout();
            this._DoEnabled();
        }
        /// <summary>
        /// Po změně velikosti vyvoláme <see cref="_DoLayout"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this._DoLayout();                                                  // Tady jsme v GUI threadu.
        }
        /// <summary>
        /// Provede požadované akce. Je povoleno volat z threadu mimo GUI.
        /// </summary>
        /// <param name="actionTypes"></param>
        internal void DoAction(DxWebViewActionType actionTypes)
        {
            if (actionTypes == DxWebViewActionType.None) return;               // Pokud není co dělat, není třeba ničehož invokovati !

            if (this.InvokeRequired)
            {   // 1x invokace na případně vícero akci
                this.BeginInvoke(new Action<DxWebViewActionType>(DoAction), actionTypes);
            }
            else
            {
                if (actionTypes.HasFlag(DxWebViewActionType.DoLayout)) this._DoLayout();
                if (actionTypes.HasFlag(DxWebViewActionType.DoEnabled)) this._DoEnabled();
                if (actionTypes.HasFlag(DxWebViewActionType.DoShowStaticPicture)) this._DoShowStaticPicture();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeDocumentTitle)) this._DoShowDocumentTitle();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeSourceUrl)) this._DoShowSourceUrl();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeStatusText)) this._DoShowStatusText();
            }
        }
        /// <summary>
        /// Rozmístí vnitřní prvky podle prostoru a podle požadavků.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoLayout()
        {
            var properties = this.WebProperties;

            var size = this.ClientSize;
            int left = 0;
            int width = size.Width;
            int top = 0;
            int bottom = size.Height;

            int buttonSize = DxComponent.ZoomToGui(24, this.CurrentDpi);
            int toolHeight = DxComponent.ZoomToGui(30, this.CurrentDpi);
            int buttonTop = (toolHeight - buttonSize) / 2;
            int paddingX = 0; // buttonTop;

            // Toolbar
            bool isToolbarVisible = properties.IsToolbarVisible;
            this.__ToolPanel.Visible = isToolbarVisible;
            if (isToolbarVisible)
            {
                int toolLeft = paddingX;
                int shiftX = buttonSize * 9 / 8;
                int distanceX = shiftX - buttonSize;

                bool isBackForwardVisible = properties.IsBackForwardButtonsVisible;
                __BackButton.Visible = isBackForwardVisible;
                __ForwardButton.Visible = isBackForwardVisible;
                if (isBackForwardVisible)
                {
                    __BackButton.Bounds = new System.Drawing.Rectangle(toolLeft, buttonTop, buttonSize, buttonSize);
                    toolLeft += shiftX;
                    __ForwardButton.Bounds = new System.Drawing.Rectangle(toolLeft, buttonTop, buttonSize, buttonSize);
                    toolLeft += shiftX;
                }

                bool isRefreshVisible = properties.IsRefreshButtonVisible;
                __RefreshButton.Visible = isRefreshVisible;
                if (isRefreshVisible)
                {
                    __RefreshButton.Bounds = new System.Drawing.Rectangle(toolLeft, buttonTop, buttonSize, buttonSize);
                    toolLeft += shiftX;
                }

                int toolRight = width - paddingX;
                bool isAdressVisible = properties.IsAdressEditorVisible;
                bool isAdressEditable = properties.IsAdressEditorEditable;
                __AdressText.Visible = isAdressVisible;
                __GoToButton.Visible = isAdressVisible && isAdressEditable;
                if (isAdressVisible)
                {
                    if (isAdressEditable)
                    {
                        __GoToButton.Bounds = new System.Drawing.Rectangle((toolRight - buttonSize), buttonTop, buttonSize, buttonSize);
                        toolRight -= shiftX;
                    }

                    int editorHeight = __AdressText.Bounds.Height;
                    int editorTop = buttonTop + buttonSize - editorHeight - 1;
                    __AdressText.Bounds = new System.Drawing.Rectangle(toolLeft, editorTop, (toolRight - toolLeft), editorHeight);
                }

                __ToolPanel.Bounds = new System.Drawing.Rectangle(left, top, width, toolHeight);
                top = top + toolHeight;
            }

            // Statusbar:
            bool isStatusVisible = this.WebProperties.IsStatusRowVisible;
            this.__StatusBar.Visible = isStatusVisible;
            if (isStatusVisible) 
            {
                int textHeight = this.__StatusText.Height;
                int statusHeight = textHeight + 4;
                bottom = bottom - statusHeight;
                this.__StatusBar.Bounds = new System.Drawing.Rectangle(left, bottom, width, statusHeight);
                this.__StatusText.Bounds = new System.Drawing.Rectangle(paddingX, 2, width - (2 * paddingX), textHeight);
            }

            // Web + Picture:
            int wh = bottom - top;
            var oldPicBounds = this.__PictureWeb.Bounds;
            var newWebBounds = new System.Drawing.Rectangle(left, top, width, wh);
            var newPicBounds = newWebBounds;

            // Debug: živý web nechám vlevo nahoře přes 3/4 plochy, a Picture vpravo dole, s tím že prostřední 1/2 se překrývá:
            /*
            int w4 = width / 4;
            int h4 = wh / 4;
            newWebBounds.Width -= w4;
            newWebBounds.Height -= h4;

            newPicBounds.X += w4;
            newPicBounds.Y += h4;
            newPicBounds.Width -= w4;
            newPicBounds.Height -= h4;
            */
            // Debug konec.

            this.__MsWebView.Bounds = newWebBounds;
            this.__PictureWeb.Bounds = newPicBounds;
            if (newPicBounds != oldPicBounds) _TryInternalCaptureMsWebImage();
        }
        /// <summary>
        /// Nastaví Enabled na patřičné prvky, odpovídající aktuálnímu stavu MsWebView.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoEnabled()
        {
            var properties = this.WebProperties;

            // Toolbar
            bool isToolbarVisible = properties.IsToolbarVisible;
            if (isToolbarVisible)
            {
                bool isBackForwardVisible = properties.IsBackForwardButtonsVisible;
                if (isBackForwardVisible)
                {
                    bool canGoBack = properties.CanGoBack; ;
                    __BackButton.Enabled = canGoBack;
                    __BackButton.ImageName = (canGoBack ? ImageNameBackEnabled : ImageNameBackDisabled);

                    bool canGoForward = properties.CanGoForward;
                    __ForwardButton.Enabled = canGoForward;
                    __ForwardButton.ImageName = (canGoForward ? ImageNameForwardEnabled : ImageNameForwardDisabled);
                }

                bool isRefreshVisible = properties.IsRefreshButtonVisible;
                if (isRefreshVisible)
                {
                    bool isRefreshEnabled = true;
                    __RefreshButton.Enabled = isRefreshEnabled;
                    __RefreshButton.ImageName = (isRefreshEnabled ? ImageNameRefreshEnabled : ImageNameRefreshDisabled);
                }
            }
        }
        /// <summary>
        /// Aktualizuje Picture z okna živého webu, podle nastavení <see cref="WebProperties"/>: <see cref="MsWebView.PropertiesInfo.IsStaticPicture"/>
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowStaticPicture()
        {
            var properties = this.WebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (isStaticPicture)
            {   // Static => požádáme o zachycení Image a pak jej vykreslíme:
                this._TryInternalCaptureMsWebImage();
            }
            else
            {   // Živý web => zobrazíme Web a skryjeme Picture:
                if (!this.__MsWebView.Visible)
                    this.__MsWebView.Visible = true;
                if (this.__PictureWeb.Visible)
                    this.__PictureWeb.Visible = false;
            }
        }
        /// <summary>
        /// Aktualizuje titulek po jeho změně v <see cref="MsWebView"/>.
        /// </summary>
        private void _DoShowDocumentTitle()
        { }
        /// <summary>
        /// Aktualizuje text SourceUrl v adresním prostoru.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowSourceUrl()
        {
            var properties = this.WebProperties;
            bool isVisible = properties.IsToolbarVisible && properties.IsAdressEditorVisible;
            bool isInEditState = isVisible && properties.IsAdressEditorEditable && this.__AdressEditorHasFocus;
            if (isVisible && !isInEditState)
            {
                string sourceUrl = properties.CurrentSourceUrl;
                this.__AdressText.Text = sourceUrl;
            }
        }
        /// <summary>
        /// Nastaví text do StatusBaru.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowStatusText()
        {
            var properties = this.WebProperties;
            if (properties.IsStatusRowVisible)
            {
                string statusText = properties.CurrentStatusText;
                this.__StatusText.Text = statusText;
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
        private DxSimpleButton __ForwardButton;
        private DxSimpleButton __RefreshButton;
        private DxTextEdit __AdressText;
        private DxSimpleButton __GoToButton;
        private MsWebView __MsWebView;
        private PictureBox __PictureWeb;
        private DxPanelControl __StatusBar;
        private DxLabelControl __StatusText;

        private const string ImageNameBackEnabled = "images/xaf/templatesv2images/action_navigation_history_back.svg";
        private const string ImageNameBackDisabled = "images/xaf/templatesv2images/action_navigation_history_back_disabled.svg";
        private const string ImageNameForwardEnabled = "images/xaf/templatesv2images/action_navigation_history_forward.svg";
        private const string ImageNameForwardDisabled = "images/xaf/templatesv2images/action_navigation_history_forward_disabled.svg";
        private const string ImageNameRefreshEnabled = "images/xaf/templatesv2images/action_refresh.svg";
        private const string ImageNameRefreshDisabled = "images/xaf/templatesv2images/action_refresh_disabled.svg";
        private const string ImageNameGoTo1 = "images/xaf/templatesv2images/action_simpleaction.svg";
        private const string ImageNameGoTo2 = "devav/actions/pagenext.svg";
        private const string ImageNameGoTo3 = "svgimages/arrows/next.svg";
        private const string ImageNameGoTo4 = "svgimages/business%20objects/bo_validation.svg";
        private const string ImageNameGoTo = ImageNameGoTo2;
        private const string ImageNameValidateEnabled = "images/xaf/templatesv2images/action_validation_validate.svg";
        private const string ImageNameValidateDisabled = "images/xaf/templatesv2images/action_validation_validate_disabled.svg";
        #endregion
        #region Privátní život - buttony a adresní editor
        private void _BackClick(object sender, EventArgs args)
        {
            this.__MsWebView.GoBack();
            this.__MsWebView.Focus();
        }
        private void _ForwardClick(object sender, EventArgs args) 
        {
            this.__MsWebView.GoForward();
            this.__MsWebView.Focus();
        }
        private void _RefreshClick(object sender, EventArgs args)
        {
            this.__MsWebView.Reload();
            this.__MsWebView.Focus();
        }
        private void _AdressEntered(object sender, EventArgs args)
        {
            __AdressEditorHasFocus = true;
            __AdressValueOnEnter = __AdressText.Text;
        }
        private void _AdressKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
        }
        private void _AdressKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            var properties = this.WebProperties;
            if ((properties.IsAdressEditorVisible && properties.IsAdressEditorEditable) && (e.KeyCode == System.Windows.Forms.Keys.Enter))
                this._AdressNavigate();
        }
        private void _AdressLostFocus(object sender, EventArgs e)
        {
            __AdressEditorHasFocus = false;
        }
        private void _AdressLeaved(object sender, EventArgs args)
        {
            __AdressEditorHasFocus = false;
        }
        private void _GoToClick(object sender, EventArgs args)
        {
            var properties = this.WebProperties;
            if (properties.IsAdressEditorVisible && properties.IsAdressEditorEditable)
                this._AdressNavigate();
        }
        private void _AdressNavigate()
        {
            var properties = this.WebProperties;
            properties.UrlAdress = __AdressText.Text;
            this.__MsWebView.Focus();
        }
        private string __AdressValueOnEnter;
        private bool __AdressEditorHasFocus;
        #endregion
        #region Public Eventy a jejich vyvolávání
        /// <summary>
        /// Provede inicializaci eventů z <see cref="__MsWebView"/>, jejich napojení na naše handlery
        /// </summary>
        private void _MsWebInitEvents()
        {
            __MsWebView.MsWebHistoryChanged += _MsWebHistoryChanged;
            __MsWebView.WebCurrentDocumentTitleChanged += _MsWebCurrentDocumentTitleChanged;
            __MsWebView.MsWebCurrentUrlAdressChanged += _MsWebCurrentUrlAdressChanged;
            __MsWebView.MsWebCurrentStatusTextChanged += _MsWebCurrentStatusTextChanged;
            __MsWebView.MsWebNavigationBefore += _MsWebNavigationBefore;
            __MsWebView.MsWebNavigationStarted += _MsWebNavigationStarted;
            __MsWebView.MsWebNavigationStarting += _MsWebNavigationStarted;
            __MsWebView.MsWebNavigationCompleted += _MsWebNavigationCompleted;
            __MsWebView.MsWebImageCaptured += _MsWebImageCaptured;
        }

        private void _MsWebHistoryChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.HistoryChanged");
            OnMsWebCurrentCanGoEnabledChanged();
            MsWebCurrentCanGoEnabledChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="WebProperties"/>: <see cref="MsWebView.PropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.PropertiesInfo.CanGoForward"/>.
        /// </summary>
        protected virtual void OnMsWebCurrentCanGoEnabledChanged() { }
        /// <summary>
        /// Event při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="WebProperties"/>: <see cref="MsWebView.PropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.PropertiesInfo.CanGoForward"/>.
        /// </summary>
        public event EventHandler MsWebCurrentCanGoEnabledChanged;

        /// <summary>
        /// Vyvolá události při změně titulku dokumentu.
        /// </summary>
        private void _MsWebCurrentDocumentTitleChanged(object sender, EventArgs e)
        {
            OnMsWebCurrentDocumentTitleChanged();
            MsWebCurrentDocumentTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně titulku dokumentu.
        /// </summary>
        protected virtual void OnMsWebCurrentDocumentTitleChanged() { }
        /// <summary>
        /// Event při změně titulku dokumentu.
        /// </summary>
        public event EventHandler MsWebCurrentDocumentTitleChanged;

        private void _MsWebCurrentUrlAdressChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.SourceUrlChanged");
            _TryInternalCaptureMsWebImage();
            OnMsWebCurrentUrlAdressChanged();
            MsWebCurrentUrlAdressChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně URL adresy "MsWebCurrentUrlAdress".
        /// </summary>
        protected virtual void OnMsWebCurrentUrlAdressChanged() { }
        /// <summary>
        /// Event při změně URL adresy "MsWebCurrentUrlAdress".
        /// </summary>
        public event EventHandler MsWebCurrentUrlAdressChanged;

        private void _MsWebCurrentStatusTextChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.StatusTextChanged");
            OnMsWebCurrentStatusTextChanged();
            MsWebCurrentStatusTextChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně textu ve StatusBaru.
        /// </summary>
        protected virtual void OnMsWebCurrentStatusTextChanged() { }
        /// <summary>
        /// Event při změně textu ve StatusBaru.
        /// </summary>
        public event EventHandler MsWebCurrentStatusTextChanged;

        private void _MsWebNavigationBefore(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.NavigationBefore");
            __NavigationInProgress = true;
            _ShowWebViewNavigationBefore();
            OnMsWebNavigationBefore();
            MsWebNavigationBefore?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se před zahájením navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationBefore() { }
        /// <summary>
        /// Event před zahájením navigace.
        /// </summary>
        public event EventHandler MsWebNavigationBefore;

        /// <summary>
        /// Vyvolá události po zahájením navigace.
        /// </summary>
        private void _MsWebNavigationStarted(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.NavigationStarted");
            _ShowWebViewNavigationStarted();
            OnMsWebNavigationStarted();
            MsWebNavigationStarted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se po zahájením navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationStarted() { }
        /// <summary>
        /// Eventpo zahájením navigace.
        /// </summary>
        public event EventHandler MsWebNavigationStarted;

        private void _MsWebNavigationCompleted(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.NavigationCompleted");
            __NavigationInProgress = false;
            _TryInternalCaptureMsWebImage();
            OnMsWebNavigationCompleted();
            MsWebNavigationCompleted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při dokončení navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationCompleted() { }
        /// <summary>
        /// Event při dokončení navigace.
        /// </summary>
        public event EventHandler MsWebNavigationCompleted;
        /// <summary>
        /// Příznak, že proběhl event <see cref="_MsWebNavigationBefore"/>, ale dosud neproběhl event <see cref="_MsWebNavigationCompleted"/>.
        /// </summary>
        private bool __NavigationInProgress;
        #endregion
        #region Zachycování statického obrázku z WebView
        /// <summary>
        /// Captures an image of what WebView is displaying.<br/>
        /// Získá obrázek aktuálního stavu WebView a uloží jej do <see cref="LastCapturedWebViewImage"/>. 
        /// Jde o asynchronní metodu: řízení vrátí ihned, a po dokončení akce vyvolá event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        public void CaptureMsWebImage(object requestId)
        {
            this.__MsWebView.CaptureMsWebImage(requestId);           // Zahájí se async načítání, po načtení obrázku bude vyvolán event __MsWebView.MsWebImageCaptured => _MsWebImageCaptured()
        }
        /// <summary>
        /// Metoda zobrazí živý WebView pokud je statický režim, před zahájením aktivní navigace.
        /// Účelem je to, aby živý WebView control mohl plynule zobrazit navigaci.
        /// </summary>
        private void _ShowWebViewNavigationBefore()
        {
            //this.SuspendLayout();
            //if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;
            //if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;
            //this.ResumeLayout();
        }
        /// <summary>
        /// Proběhne poté, kdy WebView dostal novou URL a začíná jeho navigace
        /// </summary>
        private void _ShowWebViewNavigationStarted()
        {
            this.SuspendLayout();
            if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;
            if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;
            this.ResumeLayout();
        }
        /// <summary>
        /// Metodu volá zdejší panel vždy, když mohlo dojít ke změně obrázku, pokud je statický.
        /// Tedy: při změně velikosti controlu, při doběhnutí navigace, při vložená hodnoty <see cref="MsWebView.PropertiesInfo.IsStaticPicture"/> = true, atd<br/>
        /// Zdejší metoda sama otestuje, zda je vhodné získat Image, a pokud ano, pak o něj požádá <see cref="__MsWebView"/>. 
        /// <para/>
        /// Jde o asynchronní operaci, zdejší metoda tedy skončí okamžitě. 
        /// Po získání obrázku (po nějaké době) bude volán eventhandler <see cref="_MsWebImageCaptured(object, MsWebImageCapturedArgs)"/>, 
        /// který detekuje interní request a vyvolá <see cref="_ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs)"/>, a neprovede externí event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        private void _TryInternalCaptureMsWebImage()
        {
            var properties = this.WebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (!isStaticPicture) return;

            string url = __MsWebView.WebProperties.UrlAdress;
            if (String.IsNullOrEmpty(url)) return;

            this.SuspendLayout();
            if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;     // Musí být Visible, jinak nic nenačte
            if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;            // Necháme visible jen živý Web
            this.__PictureWeb.Image = null;
            this.ResumeLayout();

            this.__MsWebView.CaptureMsWebImage(_InternalCaptureRequestId);               // Zahájí se async načítání, po načtení obrázku bude vyvolán event __MsWebView.MsWebImageCaptured => _MsWebImageCaptured()
        }
        /// <summary>
        /// Metoda je volaná po získání dat CapturedImage (jsou dodána v argumentu) pro interní účely, na základě požadavku z metody <see cref="_TryInternalCaptureMsWebImage"/>.
        /// Metoda promítne dodaná data do statického obrázku <see cref="__PictureWeb"/>, a v případě potřeby tenti obrázek zviditelní.
        /// </summary>
        /// <param name="args"></param>
        private void _ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs args)
        {
            // Pokud jsme ve stavu, kdy probíhá navigace = načítá se obsah stránky (ještě nedoběhl event _MsWebNavigationCompleted), tak nebudeme snímat Image:
            if (__NavigationInProgress) return;

            var properties = this.WebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (!isStaticPicture) return;

            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.ImageCaptured");

            // Any Thread => GUI:
            if (this.InvokeRequired) this.BeginInvoke(new Action(reloadMsWebImageCaptured));
            else reloadMsWebImageCaptured();

            // Fyzické načtení Image a další akce, v GUI threadu
            void reloadMsWebImageCaptured()
            {
                this.SuspendLayout();
                this.__PictureWeb.Image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(args.ImageData));
                if (this.__PictureWeb.Visible) this.__PictureWeb.Refresh();
                if (!this.__PictureWeb.Visible) this.__PictureWeb.Visible = true;
                if (this.__MsWebView.Visible) this.__MsWebView.Visible = false;
                this.ResumeLayout();
            }
        }
        /// <summary>
        /// Obsahuje true, pokud my jsmew už vydali požadavek na CaptureImage. Pak existuje <see cref="_InternalCaptureRequestId"/>.
        /// </summary>
        private bool _InternalCaptureRequestExists { get { return __InternalCaptureRequestId.HasValue; } }
        /// <summary>
        /// RequestId pro náš interní požadavek na <see cref="CaptureMsWebImage(object)"/>, odlišuje náš požadavek od požadavků externích
        /// </summary>
        private Guid _InternalCaptureRequestId
        {
            get
            {
                if (!__InternalCaptureRequestId.HasValue)
                    __InternalCaptureRequestId = Guid.NewGuid();
                return __InternalCaptureRequestId.Value;
            }
        }
        private Guid? __InternalCaptureRequestId;
        private void _MsWebImageCaptured(object sender, MsWebImageCapturedArgs args)
        {
            // Tato událost je volaná vždy, když __MsWebView dokončí načítání Image.
            //  To může být vyžádáno buď interně pro naší komponentu, viz _TryInternalCaptureMsWebImage(),
            //  Anebo externě z public metody CaptureMsWebImage().
            // Každý požadavek může nést svoje ID, předávané v args.RequestId. 
            // To odlišuje, kdo žádal. Detekujeme naše interní ID a to řešíme pomocí _ReloadInternalMsWebImageCaptured();
            //  a externí, to posíláme do eventu:
            if (_InternalCaptureRequestExists && args.RequestId != null && args.RequestId is Guid guid && guid == _InternalCaptureRequestId)
            {   // Interní request:
                _ReloadInternalMsWebImageCaptured(args);
            }
            else
            {   // Externí:
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.ImageCaptured");
                OnMsWebImageCaptured(args);
                MsWebImageCaptured?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Obsah obrázku posledně zachyceného metodou <see cref="CaptureMsWebImage"/>
        /// </summary>
        public byte[] LastCapturedWebViewImage { get { return __MsWebView.LastCapturedWebViewImage; } }
        /// <summary>
        /// Volá se po získání ImageCapture.
        /// </summary>
        protected virtual void OnMsWebImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání ImageCapture.
        /// </summary>
        public event MsWebImageCapturedHandler MsWebImageCaptured;
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení v jednom místě.
        /// </summary>
        public MsWebView.PropertiesInfo WebProperties { get { return __MsWebView.WebProperties; } }
        #endregion
    }
    /// <summary>
    /// Panel obsahující <see cref="MsWebView"/> pro zobrazení mapy, a jednoduchý toolbar pro zadání / editaci adresy
    /// </summary>
    public class DxMapViewPanel : DxPanelControl
    {
        #region Konstruktor, tvorba vnitřních controlů, DoLayout, Dispose, proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxMapViewPanel()
        {
            CreateContent();
        }
        /// <summary>
        /// Vytvoří kompletní obsah a vyvolá <see cref="DoLayout"/>
        /// </summary>
        protected void CreateContent()
        {
            this.SuspendLayout();

            __MapProperties = new MapPropertiesInfo(this);
            __WebCoordinates = new DxMapCoordinates();
            _WebCoordinatesInitEvents();

            __ToolPanel = new DxPanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };

            __OpenExternalBrowserButton = DxComponent.CreateDxSimpleButton(3, 3, 24, 24, __ToolPanel, "Otevřít mapu", _ShowMapClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameOpenExternalBrowser, 
                toolTipTitle: DxComponent.Localize(MsgCode.DxMapOpenExternalBrowserTitle), toolTipText: DxComponent.Localize(MsgCode.DxMapOpenExternalBrowserText));
            
            __SearchCoordinatesButton = DxComponent.CreateDxSimpleButton(30, 3, 24, 24, __ToolPanel, "", _FindAdressClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameSearchCoordinates,
                toolTipTitle: DxComponent.Localize(MsgCode.DxMapSearchCoordinatesTitle), toolTipText: DxComponent.Localize(MsgCode.DxMapSearchCoordinatesText));
            
            __CoordinatesText = DxComponent.CreateDxButtonEdit(96, 3, 250, __ToolPanel,
                toolTipTitle: DxComponent.Localize(MsgCode.DxMapCoordinatesTitle), toolTipText: DxComponent.Localize(MsgCode.DxMapCoordinatesText));

            __ReloadMapButton = DxComponent.CreateDxSimpleButton(30, 3, 24, 24, __ToolPanel, "", _ReloadMapClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameReloadMap,
                toolTipTitle: DxComponent.Localize(MsgCode.DxMapReloadMapTitle), toolTipText: DxComponent.Localize(MsgCode.DxMapReloadMapText));

            __AcceptCoordinatesButton = DxComponent.CreateDxSimpleButton(30, 3, 24, 24, __ToolPanel, "", _AcceptCoordinatesClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameAcceptCoordinates,
                toolTipTitle: DxComponent.Localize(MsgCode.DxMapAcceptCoordinatesTitle), toolTipText: DxComponent.Localize(MsgCode.DxMapAcceptCoordinatesText));

            __OpenExternalBrowserButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __OpenExternalBrowserButton.TabStop = false;
            __SearchCoordinatesButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __SearchCoordinatesButton.TabStop = false;
            __CoordinatesText.Enter += _CoordinatesEntered;
            __CoordinatesText.KeyDown += _CoordinatesKeyDown;
            __CoordinatesText.KeyPress += _CoordinatesKeyPress;
            __CoordinatesText.Leave += _CoordinatesLeaved;
            __CoordinatesText.LostFocus += _CoordinatesLostFocus;
            __CoordinatesText.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __CoordinatesText.TabStop = false;
            __ReloadMapButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __ReloadMapButton.TabStop = false;
            __AcceptCoordinatesButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __AcceptCoordinatesButton.TabStop = false;

            _CoordinatesFormatInit();

            __MsWebView = new MsWebView();
            _MsWebInitEvents();

            __PictureWeb = new PictureBox() { Visible = false, BorderStyle = System.Windows.Forms.BorderStyle.None };

            __StatusBar = new DxPanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            __StatusText = DxComponent.CreateDxLabel(6, 3, 250, __StatusBar, "Stavová informace...", LabelStyleType.Info, hAlignment: DevExpress.Utils.HorzAlignment.Near);

            this.Controls.Add(__ToolPanel);
            this.Controls.Add(__PictureWeb);
            this.Controls.Add(__MsWebView);
            this.Controls.Add(__StatusBar);
            this.ResumeLayout(false);
            this._DoLayout();
            this._DoEnabled();
        }
        /// <summary>
        /// Po změně velikosti vyvoláme <see cref="_DoLayout"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this._DoLayout();                                                  // Tady jsme v GUI threadu.
        }
        /// <summary>
        /// Provede požadované akce. Je povoleno volat z threadu mimo GUI.
        /// </summary>
        /// <param name="actionTypes"></param>
        internal void DoAction(DxWebViewActionType actionTypes)
        {
            if (actionTypes == DxWebViewActionType.None) return;               // Pokud není co dělat, není třeba ničehož invokovati !

            if (this.InvokeRequired)
            {   // 1x invokace na případně vícero akci
                this.BeginInvoke(new Action<DxWebViewActionType>(DoAction), actionTypes);
            }
            else
            {
                if (actionTypes.HasFlag(DxWebViewActionType.DoLayout)) this._DoLayout();
                if (actionTypes.HasFlag(DxWebViewActionType.DoEnabled)) this._DoEnabled();
                if (actionTypes.HasFlag(DxWebViewActionType.DoShowStaticPicture)) this._DoShowStaticPicture();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeDocumentTitle)) this._DoShowDocumentTitle();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeSourceUrl)) this._DoShowSourceUrl();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeStatusText)) this._DoShowStatusText();
                if (actionTypes.HasFlag(DxWebViewActionType.DoReloadValue)) this._DoReloadValue();
            }
        }
        /// <summary>
        /// Rozmístí vnitřní prvky podle prostoru a podle požadavků.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoLayout()
        {
            var webProperties = this.WebProperties;
            var mapProperties = this.MapProperties;

            var size = this.ClientSize;
            int left = 0;
            int width = size.Width;
            int top = 0;
            int bottom = size.Height;

            int buttonSize = DxComponent.ZoomToGui(24, this.CurrentDpi);
            int buttonOpenWidth = DxComponent.ZoomToGui(100, this.CurrentDpi);
            int toolHeight = DxComponent.ZoomToGui(30, this.CurrentDpi);
            int buttonTop = (toolHeight - buttonSize) / 2;
            int paddingX = 0; // buttonTop;
            int textMinWidth = DxComponent.ZoomToGui(200, this.CurrentDpi);
            int textMaxWidth = DxComponent.ZoomToGui(350, this.CurrentDpi);
            int shiftX = buttonSize * 9 / 8;
            int distanceX = shiftX - buttonSize;

            // Toolbar
            bool isToolbarVisible = webProperties.IsToolbarVisible;
            this.__ToolPanel.Visible = isToolbarVisible;
            if (isToolbarVisible)
            {
                bool isOpenExternalBrowserVisible = mapProperties.IsOpenExternalBrowserVisible;
                bool isSearchCoordinatesVisible = mapProperties.IsSearchCoordinatesVisible;
                bool isRelaodMapVisible = true;
                bool isAcceptCoordinatesVisible = mapProperties.IsMapEditable;

                // Sumární šířka zobrazených buttonů včetně distance X:
                int buttonsWidth = getWidth(isOpenExternalBrowserVisible, buttonOpenWidth, distanceX) +
                                   getWidth(isSearchCoordinatesVisible, buttonSize, distanceX) +
                                   getWidth(isRelaodMapVisible, buttonSize, distanceX) +
                                   getWidth(isAcceptCoordinatesVisible, buttonSize, distanceX);

                // Šířka textu, zarovnaná do Min - Max:
                int textWidth = width - buttonsWidth;
                textWidth = (textWidth < textMinWidth ? textMinWidth : (textWidth > textMaxWidth ? textMaxWidth : textWidth));

                // Postupně umístíme buttony a textbox:
                int toolLeft = paddingX;
                int buttonBottom = buttonTop + buttonSize;
                setBounds(__OpenExternalBrowserButton, isOpenExternalBrowserVisible, ref toolLeft, buttonBottom, buttonOpenWidth, buttonSize, 0, distanceX);
                setBounds(__SearchCoordinatesButton, isSearchCoordinatesVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);
                setBounds(__CoordinatesText, true, ref toolLeft, buttonBottom, textWidth, __CoordinatesText.Height, 1, distanceX);
                setBounds(__ReloadMapButton, isRelaodMapVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);
                setBounds(__AcceptCoordinatesButton, isAcceptCoordinatesVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);

                __ToolPanel.Bounds = new System.Drawing.Rectangle(left, top, width, toolHeight);
                top = top + toolHeight;
            }

            // Statusbar:
            bool isStatusVisible = this.WebProperties.IsStatusRowVisible;
            this.__StatusBar.Visible = isStatusVisible;
            if (isStatusVisible)
            {
                int textHeight = this.__StatusText.Height;
                int statusHeight = textHeight + 4;
                bottom = bottom - statusHeight;
                this.__StatusBar.Bounds = new System.Drawing.Rectangle(left, bottom, width, statusHeight);
                this.__StatusText.Bounds = new System.Drawing.Rectangle(paddingX, 2, width - (2 * paddingX), textHeight);
            }

            // Web + Picture:
            int webHeight = bottom - top;
            var oldPicBounds = this.__PictureWeb.Bounds;
            var newWebBounds = new System.Drawing.Rectangle(left, top, width, webHeight);
            var newPicBounds = newWebBounds;

            this.__MsWebView.Bounds = newWebBounds;
            this.__PictureWeb.Bounds = newPicBounds;
            if (newPicBounds != oldPicBounds) _TryInternalCaptureMsWebImage();


            int getWidth(bool isVisible, int size, int distance)
            {
                return (isVisible ? (size + distance) : 0);
            }
            void setBounds(Control control, bool isVisible, ref int boundsLeft, int buttonBottom, int boundsWidth, int boundsHeight, int bottomOffset, int distanceX)
            {
                control.Visible = isVisible;
                if (isVisible)
                {
                    int boundsTop = buttonBottom - bottomOffset - boundsHeight;
                    control.Bounds = new System.Drawing.Rectangle(boundsLeft, boundsTop, boundsWidth, boundsHeight);
                    boundsLeft = boundsLeft + boundsWidth + distanceX;
                }
            }
        }
        /// <summary>
        /// Nastaví Enabled na patřičné prvky, odpovídající aktuálnímu stavu MsWebView.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoEnabled()
        {
            var webProperties = this.WebProperties;
            var mapProperties = this.MapProperties;

            bool isMapEditable = mapProperties.IsMapEditable;

            // Toolbar
            bool isToolbarVisible = webProperties.IsToolbarVisible;
            if (isToolbarVisible)
            {
                __CoordinatesText.Enabled = isMapEditable;
            }

            // Mapa:
        }
        /// <summary>
        /// Aktualizuje Picture z okna živého webu, podle nastavení <see cref="WebProperties"/>: <see cref="MsWebView.PropertiesInfo.IsStaticPicture"/>
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowStaticPicture()
        {
            var properties = this.WebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (isStaticPicture)
            {   // Static => požádáme o zachycení Image a pak jej vykreslíme:
                this._TryInternalCaptureMsWebImage();
            }
            else
            {   // Živý web => zobrazíme Web a skryjeme Picture:
                if (!this.__MsWebView.Visible)
                    this.__MsWebView.Visible = true;
                if (this.__PictureWeb.Visible)
                    this.__PictureWeb.Visible = false;
            }
        }
        /// <summary>
        /// Aktualizuje titulek po jeho změně v <see cref="MsWebView"/>.
        /// </summary>
        private void _DoShowDocumentTitle()
        { }
        /// <summary>
        /// Aktualizuje text SourceUrl v adresním prostoru.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowSourceUrl()
        {
            var properties = this.WebProperties;
            bool isVisible = properties.IsToolbarVisible && properties.IsAdressEditorVisible;
            bool isInEditState = isVisible && properties.IsAdressEditorEditable && this.__CoordinatesTextHasFocus;
            if (isVisible && !isInEditState)
            {
                string sourceUrl = properties.CurrentSourceUrl;
                this.__CoordinatesText.Text = sourceUrl;
            }
        }
        /// <summary>
        /// Nastaví text do StatusBaru.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowStatusText()
        {
            var properties = this.WebProperties;
            if (properties.IsStatusRowVisible)
            {
                string statusText = properties.CurrentStatusText;
                this.__StatusText.Text = statusText;
            }
        }
        /// <summary>
        /// Externí aplikace změnila souřadnici, nebo providera - je třeba přenačíst mapu
        /// </summary>
        private void _DoReloadValue()
        {

        }
        /// <summary>
        /// Inicializuje eventy koordinátů
        /// </summary>
        private void _WebCoordinatesInitEvents()
        {
            var webCoordinates = __WebCoordinates;
            webCoordinates.CoordinatesChanged += _WebCoordinatesChanged;
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
        private DxSimpleButton __OpenExternalBrowserButton;
        private DxSimpleButton __SearchCoordinatesButton;
        private DxButtonEdit __CoordinatesText;
        private DxSimpleButton __ReloadMapButton;
        private DxSimpleButton __AcceptCoordinatesButton;
        private MsWebView __MsWebView;
        private PictureBox __PictureWeb;
        private DxPanelControl __StatusBar;
        private DxLabelControl __StatusText;

        private const string ImageNameOpenExternalBrowser = "svgimages/richedit/trackingchanges_next.svg";
        private const string ImageNameSearchCoordinates = "svgimages/dashboards/enablesearch.svg";
        private const string ImageNameReloadMap = "svgimages/dashboards/resetlayoutoptions.svg";
        private const string ImageNameAcceptCoordinates = "svgimages/icon%20builder/travel_mappointer.svg";
        #endregion
        #region Privátní život - buttony a adresní editor a WebView
        #region Buttony
        private void _ShowMapClick(object sender, EventArgs args)
        {
            string urlAdress = this.WebCoordinates.UrlAdress;
            DxComponent.StartProcess(urlAdress);
            this.__MsWebView.Focus();
        }
        private void _FindAdressClick(object sender, EventArgs args)
        {
            // 
            this.__MsWebView.Focus();
        }
        private void _ReloadMapClick(object sender, EventArgs args)
        {
            this._RefreshMap(true);
            this.__MsWebView.Focus();
        }
        private void _AcceptCoordinatesClick(object sender, EventArgs args)
        {
            this._AcceptWebViewCoordinate();
            this.__CoordinatesText.Focus();
        }
        #endregion
        #region Textbox pro zadání koordinátu - jeho eventy, reakce na Enter, předání koordinátů z textboxu do mapových souřadnic, eventhandler ze souřadnic do WebView
        private void _CoordinatesEntered(object sender, EventArgs args)
        {
            __CoordinatesTextHasFocus = true;
            __CoordinatesTextOnEnter = __CoordinatesText.Text;
        }
        private void _CoordinatesKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
        }
        private void _CoordinatesKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode != System.Windows.Forms.Keys.Enter) return;          // Není to klávesa Enter

            string coordinatesOld = __CoordinatesTextOnEnter;
            string coordinatesNew = __CoordinatesText.Text;
            if (String.Equals(coordinatesNew, coordinatesOld)) return;         // Není změna textu

            var mapProperties = this.MapProperties;
            if (mapProperties.IsMapEditable)
                this._CoordinatesAccept(coordinatesNew);                       // Pokud je mapa editovatelná, pak akceptuji nově vepsané koordináty
            else
                __CoordinatesText.Text = coordinatesOld;                       // Needitovatelná mapa => vrátím do textboxu původní hodnotu (on by měl být Disabled)
        }
        private void _CoordinatesLostFocus(object sender, EventArgs e)
        {
            __CoordinatesTextHasFocus = false;
        }
        private void _CoordinatesLeaved(object sender, EventArgs args)
        {
            __CoordinatesTextHasFocus = false;
        }
        /// <summary>
        /// Metoda akceptuje souřadnice zadané do textboxu <see cref="__CoordinatesText"/> a vloží je do koordinátů <see cref="WebCoordinates"/>,
        /// což pravděpodobně vyvolá event <see cref="DxMapCoordinates.CoordinatesChanged"/> a následně nové nastavení URL adresy ve WebView
        /// </summary>
        private void _CoordinatesAccept(string coordinates)
        {
            // Převezmu koordináty zadané v parametru (pochází z '__CoordinatesText.Text'), a vložím je do this.MapProperties.SetCoordinates().
            // To je "vnější hodnota" = obdoba Value nebo Text.
            // Její změna vyvolá event CoordinatesChanged a zavolá reload mapy => this.RefreshMap():
            this.IMapProperties.SetCoordinates(coordinates, true, true);
            this._RefreshAcceptButtonState();
        }
        /// <summary>
        /// Přenačte souřadnice do textboxu a zobrazí aktuální mapu
        /// </summary>
        private void _RefreshMap(bool force = false)
        {
            var mapCoordinates = this.IMapProperties.MapCoordinates;           // Požadovaná souřadnice
            _ReloadCoordinatesText(mapCoordinates);                            // Zobrazím do TextBoxu (bez eventu o změně)

            // Přenesu požadované souřadnice z this.MapProperties.MapCoordinates do WebCoordinates:
            var webCoordinates = this.WebCoordinates;
            webCoordinates.FillFrom(mapCoordinates, true);

            // Přečtu URL a předám ji do WebView:
            string urlAdressNew = webCoordinates.UrlAdress;                    // URL adresa odpovídající aktuálním koordinátům
            var webView = this.__MsWebView;
            string urlAdressOld = webView.MsWebCurrentUrlAdress;
            if (!force && String.Equals(urlAdressNew, urlAdressOld)) return;   // Není force, a není změna URL adresy

            // if (System.Diagnostics.Debugger.IsAttached) System.Windows.Forms.Clipboard.SetText(urlAdressNew);

            this.__MapCoordinateToUrlAdressChangeInProgress = true;
            this.__MapCoordinateUrlAdress = urlAdressNew;
            webView.MsWebRequestedUrlAdress = urlAdressNew;

            this._RefreshAcceptButtonState();

            webView.Focus();
        }
        /// <summary>
        /// Do textboxu vepíše souřadnice z dodaného koordinátu, v aktuálním formátu <see cref="_CoordinateFormatCurrent"/>
        /// </summary>
        /// <param name="mapCoordinates"></param>
        private void _ReloadCoordinatesText(DxMapCoordinates mapCoordinates)
        {
            // Setování textu do __CoordinatesText nevyvolá event o změně, protože tamní změnu řešíme jen po klávese Enter.
            this.__CoordinatesText.Text = mapCoordinates.GetCoordinates(this._CoordinateFormatCurrent);
            this._RefreshAcceptButtonState();
        }
        /// <summary>
        /// Obsah TextBoxu pro koordináty při vstupu focusu
        /// </summary>
        private string __CoordinatesTextOnEnter;
        /// <summary>
        /// TextBox pro koordináty má focus?
        /// </summary>
        private bool __CoordinatesTextHasFocus;
        #endregion
        #region Format - vizuální forma zobrazených koordinátů, nemá vliv na hodnotu souřadnic ani na pozici v mapě
        /// <summary>
        /// Inicializuje objekt <see cref="__CoordinatesText"/> pro validní práci s formáty <see cref="DxMapCoordinatesFormat"/>
        /// </summary>
        private void _CoordinatesFormatInit()
        {
            __CoordinatesText.Properties.Buttons.Clear();
            __CoordinatesText.AddButton(DevExpress.XtraEditors.Controls.ButtonPredefines.SpinLeft, DxComponent.Localize(MsgCode.DxMapCoordinatesButtonText), true);
            __CoordinatesText.AddButton(DevExpress.XtraEditors.Controls.ButtonPredefines.SpinRight, DxComponent.Localize(MsgCode.DxMapCoordinatesButtonText), false);
            __CoordinatesText.ButtonClick += _CoordinatesTextButtonClick;

            // Toto jsou formáty, ve kterých lze zobrazit souřadnice. Až někdo přidá konvertory na další formáty (OLC, PlusPoint), pak se sem přidají...
            __CoordinateFormats = new DxMapCoordinatesFormat[]
            {
                DxMapCoordinatesFormat.Nephrite,
                DxMapCoordinatesFormat.Wgs84ArcSecSuffix,
                DxMapCoordinatesFormat.Wgs84ArcSecPrefix,
                DxMapCoordinatesFormat.Wgs84Decimal,
                DxMapCoordinatesFormat.Wgs84DecimalSuffix,
                DxMapCoordinatesFormat.Wgs84DecimalPrefix
            };
            __CoordinateFormatCurrentIndex = 1;
        }
        /// <summary>
        /// Po kliknutí na buttony změny formátu: posune se na Next/Prev a zobrazí se aktuální text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoordinatesTextButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            var buttonKind = e.Button.Kind;

            bool isLeft = (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinLeft || buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinDown ||
                          (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph && e.Button.Tag is String text1 && String.Equals(text1, "Left")));
            bool isRight = (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinRight || buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinUp ||
                           (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph && e.Button.Tag is String text2 && String.Equals(text2, "Right")));

            int shift = (isLeft ? -1 : (isRight ? +1 : 0));
            if (shift != 0)
            {
                int count = __CoordinateFormats.Length;
                __CoordinateFormatCurrentIndex = (__CoordinateFormatCurrentIndex + shift) % count;

                // Zobrazíme aktuální koordináty v aktuálním (nově určeném) formátu:
                var mapCoordinates = this.IMapProperties.MapCoordinates;
                _ReloadCoordinatesText(mapCoordinates);                      // Setování textu do __CoordinatesText nevyvolá event o změně, protože tamní změnu řešíme jen po klávese Enter.

                // Tato metoda nemění hodnotu souřadnic (mění jen její formální vyjádření), proto neprovádíme reload mapy ani refresh Accept buttonu...
            }
        }
        /// <summary>
        /// Obsahuje aktuálně zvolený formát koordinátů (zadaný pomocí spinnerů v <see cref="__CoordinatesText"/>).
        /// </summary>
        private DxMapCoordinatesFormat _CoordinateFormatCurrent
        {
            get
            {
                var formats = __CoordinateFormats;
                int count = formats?.Length ?? 0;
                if (count > 0)
                {
                    int index = __CoordinateFormatCurrentIndex;
                    if (index >= 0 && index < count)
                        return formats[index];
                }
                return DxMapCoordinatesFormat.Nephrite;
            }
            set
            {
                var formats = __CoordinateFormats;
                int count = formats?.Length ?? 0;
                if (count > 0 && (formats.TryFindFirstIndex(f => f == value, out int index)))
                    __CoordinateFormatCurrentIndex = index;
            }
        }
        /// <summary>
        /// Index aktuálně platného formátu koordinátů (index do pole <see cref="__CoordinateFormats"/>, kde tento prvek obsahuje hodnotu <see cref="_CoordinateFormatCurrent"/>)
        /// </summary>
        private int __CoordinateFormatCurrentIndex;
        /// <summary>
        /// Uživateli dostupné formáty <see cref="DxMapCoordinatesFormat"/>.
        /// </summary>
        private DxMapCoordinatesFormat[] __CoordinateFormats;
        #endregion
        #region Události z WebView a jejich vliv na koordináty
        /// <summary>
        /// Provede inicializaci eventů z <see cref="__MsWebView"/>, jejich napojení na naše handlery
        /// </summary>
        private void _MsWebInitEvents()
        {
            __MsWebView.WebCurrentDocumentTitleChanged += _MsWebCurrentDocumentTitleChanged;
            __MsWebView.MsWebCurrentUrlAdressChanged += _MsWebCurrentUrlAdressChanged;
            __MsWebView.MsWebCurrentStatusTextChanged += _MsWebCurrentStatusTextChanged;
            __MsWebView.MsWebNavigationBefore += _MsWebNavigationBefore;
            __MsWebView.MsWebNavigationStarted += _MsWebNavigationStarted;
            __MsWebView.MsWebNavigationStarting += _MsWebNavigationStarted;
            __MsWebView.MsWebNavigationCompleted += _MsWebNavigationCompleted;
            __MsWebView.MsWebImageCaptured += _MsWebImageCaptured;
        }
        /// <summary>
        /// Vyvoláno po události při změně titulku dokumentu.
        /// </summary>
        private void _MsWebCurrentDocumentTitleChanged(object sender, EventArgs e)
        {
            OnMsWebCurrentDocumentTitleChanged();
            MsWebCurrentDocumentTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno po události při změně URL adresy.
        /// </summary>
        private void _MsWebCurrentUrlAdressChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.SourceUrlChanged");

            var isChangeForCoordinate = __MapCoordinateToUrlAdressChangeInProgress;
            _TryInternalCaptureMsWebImage();
            OnMsWebCurrentUrlAdressChanged();
            MsWebCurrentUrlAdressChanged?.Invoke(this, EventArgs.Empty);

            if (!isChangeForCoordinate)
                // Změna URL adresy NEBYLA prováděna z důvodu změny koordinátů => URL adresu změnil interaktivně uživatel => měli bychom detekovat nové koordináty:
                _ConvertUrlAdressToCoordinates();
        }
        /// <summary>
        /// Vyvoláno po události při změně textu status baru.
        /// </summary>
        private void _MsWebCurrentStatusTextChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.StatusTextChanged");
            OnMsWebCurrentStatusTextChanged();
            MsWebCurrentStatusTextChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno po události před změnou navigace.
        /// </summary>
        private void _MsWebNavigationBefore(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.NavigationBefore");
            __NavigationInProgress = true;
            _ShowWebViewNavigationBefore();
            OnMsWebNavigationBefore();
            MsWebNavigationBefore?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvolá události po zahájením navigace.
        /// </summary>
        private void _MsWebNavigationStarted(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.NavigationStarted");
            _ShowWebViewNavigationStarted();
            OnMsWebNavigationStarted();
            MsWebNavigationStarted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Po dokončení navigace = WebView změnil obsah dokumentu (webová stránka) a má hotovo.
        /// Důvody jsou různé: programová změna URL adresy (=změna koordinátů) anebo uživatelovo klikání a myší přetahování.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebNavigationCompleted(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.NavigationCompleted");

            var isChangeForCoordinate = __MapCoordinateToUrlAdressChangeInProgress;
            __MapCoordinateToUrlAdressChangeInProgress = false;
            __NavigationInProgress = false;
            _TryInternalCaptureMsWebImage();
            OnMsWebNavigationCompleted();
            MsWebNavigationCompleted?.Invoke(this, EventArgs.Empty);

            if (!isChangeForCoordinate)
                // Změna URL adresy NEBYLA prováděna z důvodu změny koordinátů => URL adresu změnil interaktivně uživatel => měli bychom detekovat nové koordináty:
                _ConvertUrlAdressToCoordinates();
        }
        /// <summary>
        /// Metoda je volaná poté, kdy uživatel interaktivně změní URL adresu. Možná došlo ke změně souřadnic?
        /// </summary>
        private void _ConvertUrlAdressToCoordinates()
        {
            var webView = this.__MsWebView;
            string urlAdress = webView.MsWebCurrentUrlAdress;                  // URL adresa ve WebView

            if (DxMapCoordinates.TryParseUrlAdress(urlAdress, out var newCoordinates))
            {   // Uživatel změnil URL adresu na mapě, a my jsme z ní detekovali nové koordináty (souřadnice, zoom, typ mapy atd):

                // Uložíme si nově získané hodnoty (newCoordinates) do naší instance WebCoordinates, tím dojde k události WebCoordinates.CoordinatesChanged => _WebCoordinatesChanged.
                var webCoordinates = this.WebCoordinates;
                webCoordinates.FillFrom(newCoordinates);

                _RefreshAcceptButtonState();
                // Změna pozice na mapě se nepromítá automaticky do CoordinateText, tam svítí vnější datová hodnota!
            }
        }
        /// <summary>
        /// Eventhandler změny koordinátů v <see cref="WebCoordinates"/>: detekuje rozdíl mezi souřadnicemi požadovanými (v <see cref="MapProperties"/>) a aktuálními (<see cref="WebCoordinates"/>),
        /// a odpovídajícím způsobem nastaví dostupnost tlačítka pro akceptování.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _WebCoordinatesChanged(object sender, EventArgs e)
        {
            _RefreshAcceptButtonState();
        }

        /// <summary>
        /// URL adresa odpovídající posledně platným koordinátům
        /// </summary>
        private string __MapCoordinateUrlAdress;
        /// <summary>
        /// Příznak, že jsme změnili koordináty a návazně na to jsme zahájili navigaci na novou adresu,
        /// a dosud neproběhl event <see cref="_MsWebNavigationCompleted"/>.<br/>
        /// Jakmile ten event proběhne, a bude mít zdejší příznak true, pak nejde o změnu koordinátů.<br/>
        /// Pokud ale proběhne event <see cref="_MsWebNavigationCompleted"/> při stavu, kdy <see cref="__MapCoordinateToUrlAdressChangeInProgress"/> bude false,
        /// pak jde o změnu pozice mapy z aktivity uživatele a jde tedy o změnu koordinátů.<br/>
        /// </summary>
        private bool __MapCoordinateToUrlAdressChangeInProgress;
        /// <summary>
        /// Příznak, že proběhl event <see cref="_MsWebNavigationBefore"/>, ale dosud neproběhl event <see cref="_MsWebNavigationCompleted"/>.
        /// </summary>
        private bool __NavigationInProgress;
        #endregion
        #region Akce Accept
        /// <summary>
        /// Metoda porovná souřadnici požadovanou z aplikace (je přítomna v textboxu CoordinatesText a v <see cref="IMapProperties"/>.MapCoordinates) 
        /// proti souřadnici aktuálně zobrazené v mapě (<see cref="WebCoordinates"/>).
        /// Podle výsledku porovnání zobrazí stav Enabled v buttonu <see cref="__AcceptCoordinatesButton"/>
        /// </summary>
        private void _RefreshAcceptButtonState()
        {
            var mapProperties = this.MapProperties;
            if (!mapProperties.IsMapEditable) return;                // Mapa není editovatelná, pak není třeba řešit Accept button a jeho stav

            var mapCoordinates = this.IMapProperties.MapCoordinates; // Souřadnice požadovaná z aplikace, anebo naposledy akceptovaná
            var webCoordinates = this.WebCoordinates;                // Souřadnice právě nyní zobrazená
            var mapPoint = mapCoordinates?.Point;
            var webPoint = webCoordinates?.Point;
            bool isEqual = String.Equals(mapPoint, webPoint, StringComparison.Ordinal);   // true = shodné souřadnice = žádná změna

            this.__AcceptCoordinatesButton.Enabled = !isEqual;
        }
        /// <summary>
        /// Metoda akceptuje souřadnici nalezenou v mapě (<see cref="WebCoordinates"/>) a vloží ji do textboxu CoordinatesText a do <see cref="IMapProperties"/>.MapCoordinates.
        /// </summary>
        private void _AcceptWebViewCoordinate()
        {
            var mapProperties = this.MapProperties;
            if (!mapProperties.IsMapEditable) return;                // Mapa není editovatelná, pak není třeba řešit Accept button a jeho stav

            var webCoordinates = this.WebCoordinates;                // Souřadnice právě nyní zobrazená
            var coordinates = webCoordinates.Coordinates;

            this.IMapProperties.SetCoordinates(coordinates, true, false);     // Event do vnějšího světa, bez reloadu mapy (ta je právě zdrojem dat)
            _ReloadCoordinatesText(this.IMapProperties.MapCoordinates);       // Setování textu do __CoordinatesText nevyvolá event o změně, protože tamní změnu řešíme jen po klávese Enter.
        }
        #endregion
        #endregion
        #region Public Eventy a jejich vyvolávání
        /// <summary>
        /// Volá se při změně titulku dokumentu.
        /// </summary>
        protected virtual void OnMsWebCurrentDocumentTitleChanged() { }
        /// <summary>
        /// Event při změně titulku dokumentu.
        /// </summary>
        public event EventHandler MsWebCurrentDocumentTitleChanged;

        /// <summary>
        /// Volá se při změně URL adresy "MsWebCurrentUrlAdress".
        /// </summary>
        protected virtual void OnMsWebCurrentUrlAdressChanged() { }
        /// <summary>
        /// Event při změně URL adresy "MsWebCurrentUrlAdress".
        /// </summary>
        public event EventHandler MsWebCurrentUrlAdressChanged;

        /// <summary>
        /// Volá se při změně textu ve StatusBaru.
        /// </summary>
        protected virtual void OnMsWebCurrentStatusTextChanged() { }
        /// <summary>
        /// Event při změně textu ve StatusBaru.
        /// </summary>
        public event EventHandler MsWebCurrentStatusTextChanged;

        /// <summary>
        /// Volá se před zahájením navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationBefore() { }
        /// <summary>
        /// Event před zahájením navigace.
        /// </summary>
        public event EventHandler MsWebNavigationBefore;

        /// <summary>
        /// Volá se po zahájením navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationStarted() { }
        /// <summary>
        /// Eventpo zahájením navigace.
        /// </summary>
        public event EventHandler MsWebNavigationStarted;

        /// <summary>
        /// Volá se při dokončení navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationCompleted() { }
        /// <summary>
        /// Event při dokončení navigace.
        /// </summary>
        public event EventHandler MsWebNavigationCompleted;
        #endregion
        #region Zachycování statického obrázku z WebView
        /// <summary>
        /// Captures an image of what WebView is displaying.<br/>
        /// Získá obrázek aktuálního stavu WebView a uloží jej do <see cref="LastCapturedWebViewImage"/>. 
        /// Jde o asynchronní metodu: řízení vrátí ihned, a po dokončení akce vyvolá event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        public void CaptureMsWebImage(object requestId)
        {
            this.__MsWebView.CaptureMsWebImage(requestId);           // Zahájí se async načítání, po načtení obrázku bude vyvolán event __MsWebView.MsWebImageCaptured => _MsWebImageCaptured()
        }
        /// <summary>
        /// Metoda zobrazí živý WebView pokud je statický režim, před zahájením aktivní navigace.
        /// Účelem je to, aby živý WebView control mohl plynule zobrazit navigaci.
        /// </summary>
        private void _ShowWebViewNavigationBefore()
        {
            //this.SuspendLayout();
            //if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;
            //if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;
            //this.ResumeLayout();
        }
        /// <summary>
        /// Proběhne poté, kdy WebView dostal novou URL a začíná jeho navigace
        /// </summary>
        private void _ShowWebViewNavigationStarted()
        {
            this.SuspendLayout();
            if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;
            if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;
            this.ResumeLayout();
        }
        /// <summary>
        /// Metodu volá zdejší panel vždy, když mohlo dojít ke změně obrázku, pokud je statický.
        /// Tedy: při změně velikosti controlu, při doběhnutí navigace, při vložená hodnoty <see cref="MsWebView.PropertiesInfo.IsStaticPicture"/> = true, atd<br/>
        /// Zdejší metoda sama otestuje, zda je vhodné získat Image, a pokud ano, pak o něj požádá <see cref="__MsWebView"/>. 
        /// <para/>
        /// Jde o asynchronní operaci, zdejší metoda tedy skončí okamžitě. 
        /// Po získání obrázku (po nějaké době) bude volán eventhandler <see cref="_MsWebImageCaptured(object, MsWebImageCapturedArgs)"/>, 
        /// který detekuje interní request a vyvolá <see cref="_ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs)"/>, a neprovede externí event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        private void _TryInternalCaptureMsWebImage()
        {
            var properties = this.WebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (!isStaticPicture) return;

            string url = __MsWebView.WebProperties.UrlAdress;
            if (String.IsNullOrEmpty(url)) return;

            this.SuspendLayout();
            if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;     // Musí být Visible, jinak nic nenačte
            if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;            // Necháme visible jen živý Web
            this.__PictureWeb.Image = null;
            this.ResumeLayout();

            this.__MsWebView.CaptureMsWebImage(_InternalCaptureRequestId);               // Zahájí se async načítání, po načtení obrázku bude vyvolán event __MsWebView.MsWebImageCaptured => _MsWebImageCaptured()
        }
        /// <summary>
        /// Metoda je volaná po získání dat CapturedImage (jsou dodána v argumentu) pro interní účely, na základě požadavku z metody <see cref="_TryInternalCaptureMsWebImage"/>.
        /// Metoda promítne dodaná data do statického obrázku <see cref="__PictureWeb"/>, a v případě potřeby tenti obrázek zviditelní.
        /// </summary>
        /// <param name="args"></param>
        private void _ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs args)
        {
            // Pokud jsme ve stavu, kdy probíhá navigace = načítá se obsah stránky (ještě nedoběhl event _MsWebNavigationCompleted), tak nebudeme snímat Image:
            if (__NavigationInProgress) return;

            var properties = this.WebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (!isStaticPicture) return;

            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.ImageCaptured");

            // Any Thread => GUI:
            if (this.InvokeRequired) this.BeginInvoke(new Action(reloadMsWebImageCaptured));
            else reloadMsWebImageCaptured();

            // Fyzické načtení Image a další akce, v GUI threadu
            void reloadMsWebImageCaptured()
            {
                this.SuspendLayout();
                this.__PictureWeb.Image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(args.ImageData));
                if (this.__PictureWeb.Visible) this.__PictureWeb.Refresh();
                if (!this.__PictureWeb.Visible) this.__PictureWeb.Visible = true;
                if (this.__MsWebView.Visible) this.__MsWebView.Visible = false;
                this.ResumeLayout();
            }
        }
        /// <summary>
        /// Obsahuje true, pokud my jsmew už vydali požadavek na CaptureImage. Pak existuje <see cref="_InternalCaptureRequestId"/>.
        /// </summary>
        private bool _InternalCaptureRequestExists { get { return __InternalCaptureRequestId.HasValue; } }
        /// <summary>
        /// RequestId pro náš interní požadavek na <see cref="CaptureMsWebImage(object)"/>, odlišuje náš požadavek od požadavků externích
        /// </summary>
        private Guid _InternalCaptureRequestId
        {
            get
            {
                if (!__InternalCaptureRequestId.HasValue)
                    __InternalCaptureRequestId = Guid.NewGuid();
                return __InternalCaptureRequestId.Value;
            }
        }
        private Guid? __InternalCaptureRequestId;
        private void _MsWebImageCaptured(object sender, MsWebImageCapturedArgs args)
        {
            // Tato událost je volaná vždy, když __MsWebView dokončí načítání Image.
            //  To může být vyžádáno buď interně pro naší komponentu, viz _TryInternalCaptureMsWebImage(),
            //  Anebo externě z public metody CaptureMsWebImage().
            // Každý požadavek může nést svoje ID, předávané v args.RequestId. 
            // To odlišuje, kdo žádal. Detekujeme naše interní ID a to řešíme pomocí _ReloadInternalMsWebImageCaptured();
            //  a externí, to posíláme do eventu:
            if (_InternalCaptureRequestExists && args.RequestId != null && args.RequestId is Guid guid && guid == _InternalCaptureRequestId)
            {   // Interní request:
                _ReloadInternalMsWebImageCaptured(args);
            }
            else
            {   // Externí:
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.ImageCaptured");
                OnMsWebImageCaptured(args);
                MsWebImageCaptured?.Invoke(this, args);
            }
        }
        /// <summary>
        /// Obsah obrázku posledně zachyceného metodou <see cref="CaptureMsWebImage"/>
        /// </summary>
        public byte[] LastCapturedWebViewImage { get { return __MsWebView.LastCapturedWebViewImage; } }
        /// <summary>
        /// Volá se po získání ImageCapture.
        /// </summary>
        protected virtual void OnMsWebImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání ImageCapture.
        /// </summary>
        public event MsWebImageCapturedHandler MsWebImageCaptured;
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení v jednom místě.
        /// </summary>
        public MsWebView.PropertiesInfo WebProperties { get { return __MsWebView.WebProperties; } }
        /// <summary>
        /// Vlastnosti mapy: validní souřadnice, provider atd. Určuje chování mapového controlu.
        /// </summary>
        public MapPropertiesInfo MapProperties { get { return __MapProperties; } } private MapPropertiesInfo __MapProperties;
        /// <summary>
        /// Vlastnosti mapy, Working interface
        /// </summary>
        protected IMapPropertiesInfoWorking IMapProperties { get { return __MapProperties; } }
        /// <summary>
        /// Mapové souřadnice použité pro fyzické zobrazování mapy = reálná pozice mapy.
        /// </summary>
        protected DxMapCoordinates WebCoordinates { get { return __WebCoordinates; } } private DxMapCoordinates __WebCoordinates;
        /// <summary>
        /// Povinně přenačte mapu
        /// </summary>
        public void RefreshMap()
        {
            _RefreshMap(true);
        }
        /// <summary>
        /// Definice vlastností mapy v <see cref="DxMapViewPanel"/>
        /// </summary>
        public class MapPropertiesInfo : IMapPropertiesInfoWorking, IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            public MapPropertiesInfo(DxMapViewPanel owner)
            {
                __Owner = owner;
                _InitValues();
            }
            /// <summary>
            /// Vlastník = <see cref="DxMapViewPanel"/>
            /// </summary>
            private DxMapViewPanel __Owner;
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                __Owner = null;
            }
            /// <summary>
            /// Pokud se dodaná hodnota <paramref name="value"/> liší od hodnoty v proměnné <paramref name="variable"/>, 
            /// pak do proměnné vloží hodnotu a vyvolá <see cref="DoAction(DxWebViewActionType)"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value">hodnota do proměnné</param>
            /// <param name="variable">ref proměnná</param>
            /// <param name="actionType">Druhy prováděných akcí</param>
            /// <param name="actionForce">Požadavek na vyvolání akce i tehdy, když hodnota není změněna. Hodnota false = default = akci provést jen při změně hodnoty.</param>
            private void _SetValueDoAction<T>(T value, ref T variable, DxWebViewActionType actionType, bool actionForce = false)
            {
                if (!actionForce && Object.Equals(value, variable)) return;
                variable = value;
                __Owner.DoAction(actionType);
            }
            /// <summary>
            /// Vloží dodané souřadnice, zajistí vyvolání požadovaných reakcí.
            /// </summary>
            /// <param name="coordinates">Souřadnice</param>
            /// <param name="callEventChanged">Vyvolat událost</param>
            /// <param name="callChangeMapView">Vyvolat změnu mapy</param>
            private void _SetCoordinates(string coordinates, bool callEventChanged, bool callChangeMapView)
            {
                var oldPoint = this.__MapCoordinates.Point;
                this.__MapCoordinates.Coordinates = coordinates;
                var newPoint = this.__MapCoordinates.Point;
                var isChanged = !String.Equals(oldPoint, newPoint, StringComparison.Ordinal);                // Máme reálnou změnu souřadnice?

                if (callEventChanged && isChanged) this.CoordinatesChanged?.Invoke(this, EventArgs.Empty);   // Event do vnějšího světa
                if (callChangeMapView) this.__Owner.RefreshMap();                                            // Překreslení mapy
            }
            /// <summary>
            /// Nastaví výchozí hodnoty
            /// </summary>
            private void _InitValues()
            {
                __MapCoordinates = new DxMapCoordinates();
                __MapCoordinates.ProviderDefault = DxMapCoordinatesProvider.FrameMapy;
                __MapCoordinates.ShowPinAtPoint = true;
                __CoordinatesFormat = DxMapCoordinatesFormat.Nephrite;

                __IsOpenExternalBrowserVisible = true;
                __IsMapEditable = true;
                __IsSearchCoordinatesVisible = false;
                __IsCoordinatesTextVisible = true;
            }
            /// <summary>
            /// Souřadnice uvedené v controlu (v textboxu, a výchozí pozice mapy, na kterou lze mapu reloadnout).
            /// <para/>
            /// Po vložení hodnoty do této property bude v TextBoxu zobrazena zadaná hodnota a mapa bude zobrazena pro tuto pozici, s pomocí zadaného providera (a typu mapy a Zoomu).<br/>
            /// Zadání nové hodnoty do TextBoxu změní zdejší hodnotu (<see cref="Coordinates"/>) a vyvolá událost <see cref="CoordinatesChanged"/>.
            /// Interaktivní posun mapy sám o sobě nezmění tuto hodnotu (<see cref="Coordinates"/>), to provede až button "Accept" = zelená ikonka. Poté dojde k události <see cref="CoordinatesChanged"/>.
            /// </summary>
            public string Coordinates
            {
                get { return __MapCoordinates.GetCoordinates(this.CoordinatesFormat); } 
                set { _SetCoordinates(value, false, true); }
            }
            /// <summary>
            /// Pracovní instance koordinátů = externě zadávaná hodnota
            /// </summary>
            private DxMapCoordinates __MapCoordinates;
            /// <summary>
            /// Událost vyvolaná po změně validované pozice v mapě <see cref="Coordinates"/>.
            /// </summary>
            public event EventHandler CoordinatesChanged;
            /// <summary>
            /// Formát souřadnic, jaký je čten v property <see cref="Coordinates"/>.
            /// Setovat lze formát libovolný. Změna formátu nemá vliv na mapu (na souřadnice).
            /// </summary>
            public DxMapCoordinatesFormat CoordinatesFormat { get { return __CoordinatesFormat; } set { __CoordinatesFormat = value; } } private DxMapCoordinatesFormat __CoordinatesFormat;
            /// <summary>
            /// Mapový provider. 
            /// Změna této hodnoty nezmění interaktivně mapu (mapu změní pouze setování <see cref="Coordinates"/>). 
            /// Mapu lze refreshnout pomocí <see cref="DxMapViewPanel.RefreshMap"/> nebo 
            /// </summary>
            public DxMapCoordinatesProvider CoordinatesProvider { get { return __MapCoordinates.Provider.Value; } set { __MapCoordinates.Provider = value; } }
            /// <summary>
            /// Zobrazovat Pin na souřadnici mapy. Default = true.
            /// Změna této hodnoty nezmění interaktivně mapu (mapu změní pouze setování <see cref="Coordinates"/>). 
            /// Mapu lze refreshnout pomocí <see cref="DxMapViewPanel.RefreshMap"/> nebo 
            /// </summary>
            public bool ShowPinAtPoint { get { return __MapCoordinates.ShowPinAtPoint; } set { __MapCoordinates.ShowPinAtPoint = value;; } }
            /// <summary>
            /// Zajistí přenačtení vizuální mapy s akceptováním zadaného providera, Zoomu atd...
            /// </summary>
            public void CoordinatesRefresh() { this.__Owner.RefreshMap(); }

            /// <summary>
            /// Je možné otevřít mapu v externím prohlížeči (tlačítkem).
            /// </summary>
            public bool IsOpenExternalBrowserVisible { get { return __IsOpenExternalBrowserVisible; } set { _SetValueDoAction(value, ref __IsOpenExternalBrowserVisible, DxWebViewActionType.DoLayout); } } private bool __IsOpenExternalBrowserVisible;
            /// <summary>
            /// Je možné vyhledat souřadnice podle adresy v polích ... (tlačítkem).
            /// </summary>
            public bool IsSearchCoordinatesVisible { get { return __IsSearchCoordinatesVisible; } set { _SetValueDoAction(value, ref __IsSearchCoordinatesVisible, DxWebViewActionType.DoLayout); } } private bool __IsSearchCoordinatesVisible;
            /// <summary>
            /// Je zobrazen textbox pro koordináty souřadnice ... (tlačítkem).
            /// </summary>
            public bool IsCoordinatesTextVisible { get { return __IsCoordinatesTextVisible; } set { _SetValueDoAction(value, ref __IsCoordinatesTextVisible, DxWebViewActionType.DoLayout); } } private bool __IsCoordinatesTextVisible;
            /// <summary>
            /// Je možné editovat pozici na mapě v controlu.
            /// </summary>
            public bool IsMapEditable { get { return __IsMapEditable; } set { _SetValueDoAction(value, ref __IsMapEditable, DxWebViewActionType.DoEnabled); } } private bool __IsMapEditable;

            /// <summary>
            /// Interface member
            /// </summary>
            /// <param name="coordinates"></param>
            /// <param name="callEventChanged"></param>
            /// <param name="callChangeMapView"></param>
            void IMapPropertiesInfoWorking.SetCoordinates(string coordinates, bool callEventChanged, bool callChangeMapView)
            {
                _SetCoordinates(coordinates, callEventChanged, callChangeMapView);
            }
            /// <summary>
            /// Interface member
            /// </summary>
            DxMapCoordinates IMapPropertiesInfoWorking.MapCoordinates { get { return __MapCoordinates; } }

        }
        /// <summary>
        /// Pracovní interface do <see cref="MapProperties"/>
        /// </summary>
        protected interface IMapPropertiesInfoWorking
        {
            /// <summary>
            /// Vloží dodané souřadnice, zajistí vyvolání požadovaných reakcí.
            /// </summary>
            /// <param name="coordinates">Souřadnice</param>
            /// <param name="callEventChanged">Vyvolat událost</param>
            /// <param name="callChangeMapView">Vyvolat změnu mapy</param>
            void SetCoordinates(string coordinates, bool callEventChanged, bool callChangeMapView);
            /// <summary>
            /// Souřadnice dané aplikací
            /// </summary>
            DxMapCoordinates MapCoordinates { get; }
        }
        #endregion
    }
    /// <summary>
    /// Potomek třídy <see cref="Microsoft.Web.WebView2.WinForms.WebView2"/>
    /// </summary>
    public class MsWebView : Microsoft.Web.WebView2.WinForms.WebView2
    {
        #region Konstruktor, Parent akce, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MsWebView()
        {
            __MsWebProperties = new PropertiesInfo(this);
            _InitWebCore();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            __MsWebProperties?.Dispose();
            __MsWebProperties = null;
        }
        /// <summary>
        /// Parent typovaný na <see cref="DxWebViewPanel"/>, nebo null
        /// </summary>
        protected DxWebViewPanel ParentWebView { get { return this.Parent as DxWebViewPanel; } }
        /// <summary>
        /// Vyvolá v parentu akci definovanou parametrem <paramref name="actionTypes"/>
        /// </summary>
        /// <param name="actionTypes"></param>
        private void DoActionInParent(DxWebViewActionType actionTypes)
        {
            ParentWebView?.DoAction(actionTypes);
        }
        /// <summary>
        /// Je inicializováno
        /// </summary>
        public bool IsInitialized { get { return __IsInitialized; } } private bool __IsInitialized;
        #endregion
        #region Lazy inicializace, Web events, navigace, atd...
        /// <summary>
        /// Adresář, kde jsou ukládany režijní soubory WebView2 (aplikace + cache)
        /// </summary>
        public static string AppDataPath
        {
            get
            {
                if (__AppDataPath == null)
                    __AppDataPath = SystemAdapter.LocalUserDataPath;
                return __AppDataPath;
            }
            set
            {
                __AppDataPath = value;
            }
        }
        /// <summary>
        /// Adresář, kde jsou ukládany režijní soubory WebView2 (aplikace + cache)
        /// </summary>
        private static string __AppDataPath;
        /// <summary>
        /// Provede start inicializace EnsureCoreWebView2Async
        /// </summary>
        private void _InitWebCore()
        {
            this.__IsInitialized = false;
            this.__CoreWebInitializerCounter = 1;
            this.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties()
            {
                UserDataFolder = SystemAdapter.LocalUserDataPath
            };

            this.CoreWebView2InitializationCompleted += _CoreWebView2InitializationCompleted;
            var task = this.EnsureCoreWebView2Async();
            task.Wait(50);
        }
        /// <summary>
        /// Počítadlo, kolikrát byl spuštěn proces <c>EnsureCoreWebView2Async()</c>
        /// </summary>
        private int __CoreWebInitializerCounter;
        /// <summary>
        /// Po dokončení procesu <q>EnsureCoreWebView2Async()</q> navážeme eventhandlery a provedeme případnou čekající navigaci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                if (this.CoreWebView2 is null)
                {
                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.CoreWebView2InitializationCompleted: IS NULL; Counter: {__CoreWebInitializerCounter}!");
                    // Možná bych dal druhý pokus?  Ale jen druhý, nikoli nekonečný:
                    if (__CoreWebInitializerCounter == 1)
                    {
                        __CoreWebInitializerCounter = 2;
                        var task = this.EnsureCoreWebView2Async();
                        task.Wait(50);
                    }
                }
                else
                {
                    this.CoreWebView2.SourceChanged += _CoreWeb_SourceChanged;
                    this.CoreWebView2.StatusBarTextChanged += _CoreWeb_StatusTextChanged;
                    this.CoreWebView2.HistoryChanged += _CoreWeb_HistoryChanged;
                    this.CoreWebView2.NewWindowRequested += _CoreWeb_NewWindowRequested;
                    this.CoreWebView2.NavigationStarting += _CoreWeb_NavigationStarting;
                    this.CoreWebView2.NavigationCompleted += _CoreWeb_NavigationCompleted;
                    this.CoreWebView2.DocumentTitleChanged += _CoreWeb_DocumentTitleChanged;

                    this.__IsInitialized = true;

                    // Navigace na URL, která byla zachycena, ale nebyla realizována:
                    this._DoNavigate();

                    // Tady bych asi řešil frontu dalších úkolů, které se nastřádaly v době, kdy Core nebyl inicializován...:

                }
            }
            else
            {
                throw new ApplicationException($"MsWebView2 error: '{e.InitializationException.Message}'", e.InitializationException);
            }
        }
        /// <summary>
        /// Eventhandler pro změnu URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_SourceChanged(object sender, Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs e)
        {
            _DetectChanges();
        }
        /// <summary>
        /// Eventhandler pro změnu StatusText
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_StatusTextChanged(object sender, object e)
        {
            _DetectChanges();
        }
        /// <summary>
        /// Eventhandler po změně v okně historie navigátru. Může mít vliv na tlačítka Back/Forward.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_HistoryChanged(object sender, object e)
        {
            _DetectChanges();
        }
        /// <summary>
        /// Eventhandler při požadavku na otevření nového okna prohlížeče.
        /// Může reagovat na nastavení <see cref="PropertiesInfo.CanOpenNewWindow"/> a pokud není povoleno, pak zajistí otevření nového odkazu v aktuálním controlu namísto v novém okně.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_NewWindowRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
        {
            var properties = this.WebProperties;
            if (properties.CanOpenNewWindow)
                e.NewWindow = this.CoreWebView2;
        }
        /// <summary>
        /// Eventhandler při zahájení navigace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            __MsWebIsInNavigateState = true;
            _RunMsWebNavigationStarting();
        }
        /// <summary>
        /// Eventhandler při dokončení navigace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            __MsWebIsInNavigateState = false;
            _DetectChanges();
            _RunMsWebNavigationCompleted();
        }
        /// <summary>
        /// Po změně titulku dokumentu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_DocumentTitleChanged(object sender, object e)
        {
            _DetectChanges();
        }
        /// <summary>
        /// Vynuluje všechny proměnné související s aktuální i požadovanou adresou a statustextem.
        /// Volá se před tím, než se nastaví nový cíl navigace.
        /// </summary>
        private void _RunMsWebCurrentClear()
        {
            __MsWebRequestedUrlAdress = null;
            __MsWebHtmlContent = null;
            __MsWebNeedNavigate = false;
            __MsWebCurrentUrlAdress = null;
            __MsWebCurrentStatusText = null;
            _RunMsWebCurrentUrlAdressChanged();
            _RunMsWebCurrentStatusTextChanged();
        }
        /// <summary>
        /// Detekuje změny aktuálního 
        /// </summary>
        private void _DetectChanges()
        {
            bool newCanGoBack = this.CanGoBack;
            bool newCanGoForward = this.CanGoForward;
            string newDocumentTitle = this.CoreWebView2?.DocumentTitle;
            string newSourceUrl = this.Source.ToString();
            string newStatusText = this.CoreWebView2?.StatusBarText;

            bool isChangeCanGoBack = __MsWebCanGoBack != newCanGoBack;
            bool isChangeCanGoForward = __MsWebCanGoForward != newCanGoForward;
            bool isChangeDocumentTitle = !String.Equals(__MsWebCurrentDocumentTitle, newDocumentTitle, StringComparison.InvariantCulture);
            bool isChangeSourceUrl = !String.Equals(__MsWebCurrentUrlAdress, newSourceUrl, StringComparison.InvariantCulture);
            bool isChangeStatusText = !String.Equals(__MsWebCurrentStatusText, newStatusText, StringComparison.InvariantCulture);

            __MsWebCanGoBack = newCanGoBack;
            __MsWebCanGoForward = newCanGoForward;
            __MsWebCurrentDocumentTitle = newDocumentTitle;
            __MsWebCurrentUrlAdress = newSourceUrl;
            __MsWebCurrentStatusText = newStatusText;

            DxWebViewActionType actionTypes =
                (isChangeCanGoBack || isChangeCanGoForward ? DxWebViewActionType.DoEnabled : DxWebViewActionType.None) |
                (isChangeDocumentTitle ? DxWebViewActionType.DoChangeDocumentTitle : DxWebViewActionType.None) |
                (isChangeSourceUrl ? DxWebViewActionType.DoChangeSourceUrl : DxWebViewActionType.None) |
                (isChangeStatusText ? DxWebViewActionType.DoChangeStatusText : DxWebViewActionType.None);

            if (actionTypes != DxWebViewActionType.None)
                DoActionInParent(actionTypes);

            if (isChangeCanGoBack || isChangeCanGoForward)
                _RunMsWebHistoryChanged();

            if (isChangeDocumentTitle)
                _RunMsWebCurrentDocumentTitleChanged();

            if (isChangeSourceUrl)
                _RunMsWebCurrentUrlAdressChanged();

            if (isChangeStatusText)
                _RunMsWebCurrentStatusTextChanged();
        }

        /// <summary>
        /// Vyvolá události při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebCanGoBack"/> nebo <see cref="MsWebCanGoForward"/>.
        /// </summary>
        private void _RunMsWebHistoryChanged()
        {
            OnMsWebHistoryChanged();
            MsWebHistoryChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebCanGoBack"/> nebo <see cref="MsWebCanGoForward"/>.
        /// </summary>
        protected virtual void OnMsWebHistoryChanged() { }
        /// <summary>
        /// Event při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebCanGoBack"/> nebo <see cref="MsWebCanGoForward"/>.
        /// </summary>
        public event EventHandler MsWebHistoryChanged;

        /// <summary>
        /// Vyvolá události při změně titulku dokumentu.
        /// </summary>
        private void _RunMsWebCurrentDocumentTitleChanged()
        {
            OnWebCurrentDocumentTitleChanged();
            WebCurrentDocumentTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně titulku dokumentu.
        /// </summary>
        protected virtual void OnWebCurrentDocumentTitleChanged() { }
        /// <summary>
        /// Event při změně titulku dokumentu.
        /// </summary>
        public event EventHandler WebCurrentDocumentTitleChanged;

        /// <summary>
        /// Vyvolá události při změně URL adresy <see cref="MsWebCurrentUrlAdress"/>.
        /// </summary>
        private void _RunMsWebCurrentUrlAdressChanged()
        {
            OnMsWebCurrentUrlAdressChanged();
            MsWebCurrentUrlAdressChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně URL adresy <see cref="MsWebCurrentUrlAdress"/>.
        /// </summary>
        protected virtual void OnMsWebCurrentUrlAdressChanged() { }
        /// <summary>
        /// Event při změně URL adresy <see cref="MsWebCurrentUrlAdress"/>.
        /// </summary>
        public event EventHandler MsWebCurrentUrlAdressChanged;

        /// <summary>
        /// Vyvolá události při změně textu ve StatusBaru.
        /// </summary>
        private void _RunMsWebCurrentStatusTextChanged()
        {
            OnMsWebCurrentStatusTextChanged();
            MsWebCurrentStatusTextChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně textu ve StatusBaru.
        /// </summary>
        protected virtual void OnMsWebCurrentStatusTextChanged() { }
        /// <summary>
        /// Event při změně textu ve StatusBaru.
        /// </summary>
        public event EventHandler MsWebCurrentStatusTextChanged;

        /// <summary>
        /// Vyvolá události před zahájením navigace.
        /// </summary>
        private void _RunMsWebNavigationBefore()
        {
            OnMsWebNavigationBefore();
            MsWebNavigationBefore?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se před zahájením navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationBefore() { }
        /// <summary>
        /// Event před zahájením navigace.
        /// </summary>
        public event EventHandler MsWebNavigationBefore;

        /// <summary>
        /// Vyvolá události po zahájením navigace.
        /// </summary>
        private void _RunMsWebNavigationStarted()
        {
            OnMsWebNavigationStarted();
            MsWebNavigationStarted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se po zahájením navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationStarted() { }
        /// <summary>
        /// Eventpo zahájením navigace.
        /// </summary>
        public event EventHandler MsWebNavigationStarted;

        /// <summary>
        /// Vyvolá události při zahájení navigace.
        /// </summary>
        private void _RunMsWebNavigationStarting()
        {
            OnMsWebNavigationStarting();
            MsWebNavigationStarting?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při zahájení navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationStarting() { }
        /// <summary>
        /// Event při zahájení navigace.
        /// </summary>
        public event EventHandler MsWebNavigationStarting;

        /// <summary>
        /// Vyvolá události při dokončení navigace.
        /// </summary>
        private void _RunMsWebNavigationCompleted()
        {
            OnMsWebNavigationCompleted();
            MsWebNavigationCompleted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při dokončení navigace.
        /// </summary>
        protected virtual void OnMsWebNavigationCompleted() { }
        /// <summary>
        /// Event při dokončení navigace.
        /// </summary>
        public event EventHandler MsWebNavigationCompleted;

        private string __MsWebCurrentDocumentTitle;
        private string __MsWebCurrentUrlAdress;
        private string __MsWebCurrentStatusText;
        private bool __MsWebCanGoBack;
        private bool __MsWebCanGoForward;
        #endregion
        #region CaptureImage: zachycení statického Image celého WebView
        /// <summary>
        /// Obsahuje true, pokud tento samotný Control je Visible, bez ohledu na Visible jeho Parentů.
        /// WinForm control může mít nastaveno Visible = true, ale pokud jeho Parenti nejsou Visible, pak control bude vracet Visible = false.
        /// Na rozdíl od toho bude hodnota <see cref="IsControlVisible"/> obsahovat právě to, co bylo do <see cref="Control.Visible"/> setováno.
        /// </summary>
        public bool IsControlVisible { get { return this.IsSetVisible(); } }
        /// <summary>
        /// Captures an image of what WebView is displaying.<br/>
        /// Získá obrázek aktuálního stavu WebView a uloží jej do <see cref="LastCapturedWebViewImage"/>. 
        /// Jde o asynchronní metodu: řízení vrátí ihned, a po dokončení akce vyvolá event <see cref="MsWebImageCaptured"/>.
        /// <para/>
        /// Pozor: pokud control není Visible, pak tato metoda nic neprovede a event se nevyvolá!
        /// </summary>
        /// <param name="requestId"></param>
        public async void CaptureMsWebImage(object requestId = null)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (this.CoreWebView2 is null)
            {   // Požadavek přišel dřív, než byla dokončena inicializace Core => uložíme request do fronty:
                //   fakt fronta ???
                return;
            }
            if (!this.IsControlVisible) return;

            using (var ms = new System.IO.MemoryStream())
            {
                Task task = this.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                // V řádku 'await' se věci rozdělí:
                await task;
                //  - aktuální metoda vrátí řízení volajícímu, ten si normálně ihned pokračuje svým dalším kódem; 
                //     a zdejší metoda zde "čeká" = očekává impuls od threadu na pozadí...
                //  - až doběhne task, přejde tato metoda  >> v jiném threadu! <<  na další řádek => uloží obsah streamu a vyvolá event:
                var imageData = ms.GetBuffer();
                __LastCapturedWebViewImage = imageData;
                MsWebImageCapturedArgs args = new MsWebImageCapturedArgs(requestId, imageData);
                _RunMsWebImageCaptured(args);
            }
        }
        /// <summary>
        /// Obsah obrázku posledně zachyceného metodou <see cref="CaptureMsWebImage"/>
        /// </summary>
        public byte[] LastCapturedWebViewImage { get { return __LastCapturedWebViewImage; } } private byte[] __LastCapturedWebViewImage;
        /// <summary>
        /// Vyvolá událost 'MsWebImageCaptured'
        /// </summary>
        /// <param name="args"></param>
        private void _RunMsWebImageCaptured(MsWebImageCapturedArgs args)
        {
            OnMsWebImageCaptured(args);
            MsWebImageCaptured?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se po získání ImageCapture.
        /// </summary>
        protected virtual void OnMsWebImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání ImageCapture.
        /// </summary>
        public event MsWebImageCapturedHandler MsWebImageCaptured;
        #endregion
        #region URL adresa, Status bar, navigace, atd...
        /// <summary>
        /// Titulek dokumentu.
        /// </summary>
        public string DocumentTitle { get { return __MsWebCurrentDocumentTitle; } }
        /// <summary>
        /// Požadovaná URL adresa obsahu.
        /// Sem je možno setovat požadovanou adresu, zůstane zde trvale až do setování další adresy.
        /// Na rozdíl od toho aktuální adresa je v <see cref="MsWebCurrentUrlAdress"/>.
        /// </summary>
        public string MsWebRequestedUrlAdress 
        {
            get { return __MsWebRequestedUrlAdress; }
            set
            {
                _RunMsWebCurrentClear();
                __MsWebRequestedUrlAdress = value;
                __MsWebNeedNavigate = !String.IsNullOrEmpty(value);
                _DoNavigate();
            }
        }
        /// <summary>
        /// Požadovaná URL adresa
        /// </summary>
        private string __MsWebRequestedUrlAdress;
        /// <summary>
        /// Externě vložený HTML obsah k zobrazení v prvku.
        /// </summary>
        public string MsWebHtmlContent
        {
            get { return __MsWebHtmlContent; }
            set
            {
                _RunMsWebCurrentClear();
                __MsWebHtmlContent = value;
                __MsWebNeedNavigate = !String.IsNullOrEmpty(value);
                _DoNavigate();
            }
        }
        /// <summary>
        /// Externí HTML content
        /// </summary>
        private string __MsWebHtmlContent;
        /// <summary>
        /// Aktuální stav Enabled pro button Go Back
        /// </summary>
        public bool MsWebCanGoBack { get { return __MsWebCanGoBack; } }
        /// <summary>
        /// Aktuální stav Enabled pro button Go Forward
        /// </summary>
        public bool MsWebCanGoForward { get { return __MsWebCanGoForward; } }
        /// <summary>
        /// Aktuální zobrazená URL adresa
        /// </summary>
        public string MsWebCurrentUrlAdress { get { return __MsWebCurrentUrlAdress; } }
        /// <summary>
        /// Aktuální text Status baru
        /// </summary>
        public string MsWebCurrentStatusText { get { return __MsWebCurrentStatusText; } }
        /// <summary>
        /// Zajistí zobrazení požadované URL adresy <see cref="MsWebRequestedUrlAdress"/> nebo obsahu <see cref="MsWebHtmlContent"/>.
        /// Pouze pokud <see cref="__MsWebNeedNavigate"/> je true a pokud <q>CoreWebView2</q> existuje.
        /// Shodí příznak <see cref="__MsWebNeedNavigate"/> na false po provedení změny.
        /// </summary>
        private void _DoNavigate()
        {
            if (!__MsWebNeedNavigate) return;                        // Pokud nemám určený cíl, kam mám navigovat: nic neděláme.

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(_DoNavigate));
            }
            else
            {
                if (!(this.CoreWebView2 is null))
                {
                    __MsWebNeedNavigate = false;                     // Pokud by nás někdo volal odteď, pak zdejší metoda nic neprovede.
                    _RunMsWebNavigationBefore();                     // Událost před tím, než začneme aktivně měnit URL
                    __MsWebIsInNavigateState = true;
                    if (!String.IsNullOrEmpty(__MsWebRequestedUrlAdress))
                        this.UrlSource = __MsWebRequestedUrlAdress;
                    else if (!String.IsNullOrEmpty(__MsWebHtmlContent))
                        this.NavigateToString(__MsWebHtmlContent);
                    _RunMsWebNavigationStarted();                    // Událost po startu aktivní změny URL
                }
            }
        }
        /// <summary>
        /// URL zobrazené stránky
        /// </summary>
        public string UrlSource
        {
            get
            {
                var source = this.Source;
                if (source is null || !source.IsAbsoluteUri) return null;
                return source.AbsoluteUri;
            }
            set
            {
                if (!this.IsInitialized)
                {
                    EnsureCoreWebView2Async();
                    return;
                }
                if (String.IsNullOrEmpty(value))
                {
                    this.NavigateToString("");
                    return;
                }
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    this.NavigateToString("");
                    return;
                }

                // Význam následujícího je zřejmý po nastudování vnitřního kódu Source.Set:
                //  a) Setovaná hodnota se vepisuje do _source (a to já jako potomek nezajistím);
                //  b) Pokud se setuje shodný text URL jaký už tam je, pak se neprovádí force navigování na zadaný odkaz
                // Takže:
                //  -  Pokud nyní dávám URL jiný než dosud, pak musím setovat Source (aby se dostal do _source)
                //  -  Pokud nyní mám stejný URL jako dosud, pak nemůžu setovat Source protože při stejném URL se neprovede navigace => pak musím provést navigaci ručně zde.
                //       A klidně mohu vynechat setování _source, protože aktuální hodnota tam už je.
                var source = this.Source;
                if (source is null || !source.IsAbsoluteUri || source.AbsoluteUri != value)
                    this.Source = uri;
                else
                {
                    CoreWebView2.Navigate(value);
                    CoreWebView2.Reload();
                }
            }
        }
        private bool __MsWebNeedNavigate;
        private bool __MsWebIsInNavigateState;
        #endregion
        #region class PropertiesInfo : Souhrn vlastností, tak aby byly k dosažení pod jedním místem
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení v jednom místě.
        /// </summary>
        public PropertiesInfo WebProperties { get { return __MsWebProperties; } } private PropertiesInfo __MsWebProperties;
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
            /// Pokud se dodaná hodnota <paramref name="value"/> liší od hodnoty v proměnné <paramref name="variable"/>, 
            /// pak do proměnné vloží hodnotu a vyvolá <see cref="DoActionInParent(DxWebViewActionType)"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value">hodnota do proměnné</param>
            /// <param name="variable">ref proměnná</param>
            /// <param name="actionType">Druhy prováděných akcí</param>
            /// <param name="actionForce">Požadavek na vyvolání akce i tehdy, když hodnota není změněna. Hodnota false = default = akci provést jen při změně hodnoty.</param>
            private void _SetValueDoAction<T>(T value, ref T variable, DxWebViewActionType actionType, bool actionForce = false)
            {
                if (!actionForce && Object.Equals(value, variable)) return;
                variable = value;
                __Owner.DoActionInParent(actionType);
            }
            /// <summary>
            /// Nastaví výchozí hodnoty
            /// </summary>
            private void _InitValues()
            {
                __IsToolbarVisible = true;
                __IsBackForwardButtonsVisible = true;
                __IsRefreshButtonVisible = true;
                __IsAdressEditorVisible = true;
                __IsAdressEditorEditable = true;
                __IsStatusRowVisible = false;
                __CanOpenNewWindow = true;
            }
            /// <summary>
            /// Je toolbar viditelný?
            /// Default = true;
            /// </summary>
            public bool IsToolbarVisible { get { return __IsToolbarVisible; } set { _SetValueDoAction(value, ref __IsToolbarVisible, DxWebViewActionType.DoLayout); } } private bool __IsToolbarVisible;
            /// <summary>
            /// Jsou buttony Back a Next viditelné?
            /// Default = true;
            /// </summary>
            public bool IsBackForwardButtonsVisible { get { return __IsBackForwardButtonsVisible; } set { _SetValueDoAction(value, ref __IsBackForwardButtonsVisible, DxWebViewActionType.DoLayout); } } private bool __IsBackForwardButtonsVisible;
            /// <summary>
            /// Je button Refresh viditelný?
            /// Default = true;
            /// </summary>
            public bool IsRefreshButtonVisible { get { return __IsRefreshButtonVisible; } set { _SetValueDoAction(value, ref __IsRefreshButtonVisible, DxWebViewActionType.DoLayout); } } private bool __IsRefreshButtonVisible;
            /// <summary>
            /// Je adresní pole viditelné?
            /// Default = true;
            /// </summary>
            public bool IsAdressEditorVisible { get { return __IsAdressEditorVisible; } set { _SetValueDoAction(value, ref __IsAdressEditorVisible, DxWebViewActionType.DoLayout); } } private bool __IsAdressEditorVisible;
            /// <summary>
            /// Je adresní pole editovatelné? Pak je i zobrazen button GoTo
            /// Default = true;
            /// </summary>
            public bool IsAdressEditorEditable { get { return __IsAdressEditorEditable; } set { _SetValueDoAction(value, ref __IsAdressEditorEditable, DxWebViewActionType.DoLayout); } } private bool __IsAdressEditorEditable;
            /// <summary>
            /// Je stavový řádek viditelný?
            /// Default = false;
            /// </summary>
            public bool IsStatusRowVisible { get { return __IsStatusRowVisible; } set { _SetValueDoAction(value, ref __IsStatusRowVisible, DxWebViewActionType.DoLayout); } } private bool __IsStatusRowVisible;
            /// <summary>
            /// Může být otevřeno nové okno při požadavku (z odkazu / z kontextového menu)?
            /// Default = true = Otevře se samostatné okno bez vlastností prohlížeče.
            /// Hodnota false = veškeré odkazy se otevírají ve stejném controlu, nikdy v novém okně.
            /// </summary>
            public bool CanOpenNewWindow { get { return __CanOpenNewWindow; } set { _SetValueDoAction(value, ref __CanOpenNewWindow, DxWebViewActionType.None); } } private bool __CanOpenNewWindow;
            /// <summary>
            /// Použít statický výsledek = navigovat na URL, po dokončení navigace získat obraz a ten zobrazovat namísto živého webu.
            /// Default = false = standardní webový prohlížeč.
            /// <para/>
            /// Setování hodnoty true (i když už hodnota true je nasetovaná) vyvolá získání aktuálního Image z WebView a jeho promítnutí do panelu.
            /// <para/>
            /// Tato hodnota není použitelná pro samotný webový control <see cref="MsWebView"/> (ten vždy reprezentuje živý web), ale pouze pro komplexní panel <see cref="DxWebViewPanel"/> 
            /// - ten může zajišťovat získání Image a jeho zobrazení v Panelu namísto živého WebView.
            /// </summary>
            public bool IsStaticPicture { get { return __IsStaticPicture; } set { _SetValueDoAction(value, ref __IsStaticPicture, DxWebViewActionType.DoShowStaticPicture, value); } } private bool __IsStaticPicture;
            /// <summary>
            /// WebView je v procesu probíhající navigace
            /// </summary>
            public bool IsInNavigateState { get { return __Owner.__MsWebIsInNavigateState; } }

            /// <summary>
            /// Titulek dokumentu.
            /// </summary>
            public string DocumentTitle { get { return __Owner.DocumentTitle; } }
            /// <summary>
            /// Požadovaná URL adresa obsahu.
            /// Sem je možno setovat požadovanou adresu, zůstane zde trvale až do setování další adresy.
            /// Na rozdíl od toho aktuální adresa je v <see cref="CurrentSourceUrl"/>.
            /// </summary>
            public string UrlAdress { get { return __Owner.MsWebRequestedUrlAdress; }  set { __Owner.MsWebRequestedUrlAdress = value; } }
            /// <summary>
            /// Externě vložený HTML obsah k zobrazení v prvku.
            /// </summary>
            public string HtmlContent { get { return __Owner.MsWebHtmlContent; } set { __Owner.MsWebHtmlContent = value; } }

            /// <summary>
            /// Aktuální stav Enabled pro button Go Back
            /// </summary>
            public bool CanGoBack { get { return __Owner.MsWebCanGoBack; } }
            /// <summary>
            /// Aktuální stav Enabled pro button Go Forward
            /// </summary>
            public bool CanGoForward { get { return __Owner.MsWebCanGoForward; } }
            /// <summary>
            /// Aktuální zobrazená URL adresa
            /// </summary>
            public string CurrentSourceUrl { get { return __Owner.MsWebCurrentUrlAdress; } }
            /// <summary>
            /// Aktuální text Status baru
            /// </summary>
            public string CurrentStatusText { get { return __Owner.MsWebCurrentStatusText; } }
        }
        #endregion
    }
    #region class DxMapCoordinates : správce souřadnic, kodér + dekodér stringu i URL odkazu na mapy různých zdrojů
    /// <summary>
    /// <see cref="DxMapCoordinates"/> : správce souřadnic, kodér + dekodér stringu i URL odkazu na mapy různých zdrojů
    /// </summary>
    public class DxMapCoordinates
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxMapCoordinates()
        {
            _Reset(ResetSource.Instance);
        }
        /// <summary>
        /// Resetuje hodnoty před vložením nových
        /// </summary>
        /// <param name="source"></param>
        private void _Reset(ResetSource source)
        {
            // Resetuji na pozici: celá ČR 
            this.PointX = 15.7435513m;                     // Zkus tohle:  https://mapy.cz/turisticka?l=0&x=15.7435513&y=49.8152928&z=8
            this.PointY = 49.8152928m;
            this.Zoom = _ZoomState;                        // Zobrazuje cca celou republiku
            this.CenterX = null;
            this.CenterY = null;

            if (source == ResetSource.Instance)
            {   // Tvorba instance:
                this.CoordinatesFormatDefault = DxMapCoordinatesFormat.Wgs84DecimalSuffix;
                this.ZoomDefault = _ZoomTown;              // Zobrazuje cca 10 km na šířku běžného okna
                this.ProviderDefault = DxMapCoordinatesProvider.SeznamMapy;
                this.MapTypeDefault = DxMapCoordinatesMapType.Standard;
                this.InfoPanelVisibleDefault = true;
                this.ShowPinAtPoint = true;

                this.CoordinatesFormat = null;
                this.Provider = null;
                this.MapType = null;
                this.InfoPanelVisible = null;
            }

            if (source == ResetSource.Coordinate)
            {   // Setování koordinátů nemění defaulty, nemění providera ani typ mapy:
                this.CoordinatesFormat = null;
            }

            if (source == ResetSource.UrlAdress)
            {   // Setování URL adresy nemění formát koordinátů, ale resetuje providera i typ mapy:
                this.Provider = null;
                this.MapType = null;
                this.InfoPanelVisible = null;
            }
        }
        /// <summary>
        /// Kdo volá Reset hodnot?
        /// </summary>
        private enum ResetSource
        {
            Instance,
            Coordinate,
            UrlAdress
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _GetCoordinates(DxMapCoordinatesFormat.Wgs84ArcSecPrefix);
        }
        /// <summary>
        /// Oddělovač desetinných míst v aktuální kultuře, pracuje s ním <see cref="Decimal.TryParse(string, out decimal)"/>
        /// </summary>
        private static string _DotChar { get { return System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator; } }
        /// <summary>
        /// Zoom pro mapu na celou ČR
        /// </summary>
        private const int _ZoomState = 8;
        /// <summary>
        /// Zoom pro mapu na jedno město
        /// </summary>
        private const int _ZoomTown = 14;
        #endregion
        #region Public proměnné základní = jednotlivé : Souřadnice X:Y, Střed, Bod, Zoom, Provider, Typ mapy, Defaulty
        /// <summary>
        /// Formát koordinátů (ve formě jednoho stringu) v <see cref="Coordinates"/>.
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="CoordinatesFormatDefault"/>.
        /// </summary>
        public DxMapCoordinatesFormat? CoordinatesFormat { get { return __CoordinatesFormat ?? __CoordinatesFormatDefault; } set { __CoordinatesFormat = value; } } private DxMapCoordinatesFormat? __CoordinatesFormat;
        /// <summary>
        /// Exaktní bod souřadnic X, v rozsahu -180° až +180°
        /// </summary>
        public decimal PointX { get { return __PointX; } set { var coordinatesOld = _CoordinatesSerial; __PointX = _Align((value % 360m), -180m, 180m); _CheckCoordinatesChanged(coordinatesOld); } } private decimal __PointX;
        /// <summary>
        /// Exaktní bod souřadnic Y, v rozsahu -90° (Jih) až +90° (Sever)
        /// </summary>
        public decimal PointY { get { return __PointY; } set { var coordinatesOld = _CoordinatesSerial; __PointY = _Align((value % 180m), -90m, 90m); _CheckCoordinatesChanged(coordinatesOld); } } private decimal __PointY;
        /// <summary>
        /// Střed mapy X, v rozsahu -180° až +180°
        /// </summary>
        public decimal? CenterX { get { return __CenterX; } set { var coordinatesOld = _CoordinatesSerial; __CenterX = _Align((value % 360m), -180m, 180m); _CheckCoordinatesChanged(coordinatesOld); } } private decimal? __CenterX;
        /// <summary>
        /// Střed mapy Y, v rozsahu -90° (Jih) až +90° (Sever)
        /// </summary>
        public decimal? CenterY { get { return __CenterY; } set { var coordinatesOld = _CoordinatesSerial; __CenterY = _Align((value % 180m), -90m, 90m); _CheckCoordinatesChanged(coordinatesOld); } } private decimal? __CenterY;
        /// <summary>
        /// Zoom, v rozsahu 1 (celá planeta) až 20 (jeden pokojíček). Zoom roste exponenciálně, rozdíl 1 číslo je 2-násobek.
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="ProviderDefault"/>.
        /// </summary>
        public int? Zoom { get { return __Zoom ?? __ZoomDefault; } set { var coordinatesOld = _CoordinatesSerial; __Zoom = _AlignN(value, 1, 20); _CheckCoordinatesChanged(coordinatesOld); } } private int? __Zoom;
        /// <summary>
        /// Je definován i střed mapy?
        /// </summary>
        public bool HasCenter { get { return (this.__CenterX.HasValue && this.__CenterY.HasValue); } }

        /// <summary>
        /// Provider mapy (webová stránka).
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="ProviderDefault"/>.
        /// </summary>
        public DxMapCoordinatesProvider? Provider { get { return __Provider ?? __ProviderDefault; } set { __Provider = value; } } private DxMapCoordinatesProvider? __Provider;
        /// <summary>
        /// Typ mapy (standardní, dopravní, turistická, fotoletecká).
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="MapTypeDefault"/>.
        /// </summary>
        public DxMapCoordinatesMapType? MapType { get { return __MapType ?? __MapTypeDefault; } set { __MapType = value; } } private DxMapCoordinatesMapType? __MapType;
        /// <summary>
        /// Viditelnost bočního panelu s detaily.
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="InfoPanelVisibleDefault"/>.
        /// </summary>
        public bool? InfoPanelVisible { get { return __InfoPanelVisible ?? __InfoPanelVisibleDefault; } set { __InfoPanelVisible = value; } } private bool? __InfoPanelVisible;

        /// <summary>
        /// Defaultní formát souřadnic
        /// </summary>
        public DxMapCoordinatesFormat CoordinatesFormatDefault { get { return __CoordinatesFormatDefault; } set { __CoordinatesFormatDefault = value; } } private DxMapCoordinatesFormat __CoordinatesFormatDefault;
        /// <summary>
        /// Defaultní Zoom
        /// </summary>
        public int ZoomDefault { get { return __ZoomDefault; } set { __ZoomDefault = _Align(value, 1, 20); } } private int __ZoomDefault;
        /// <summary>
        /// Defaultní provider
        /// </summary>
        public DxMapCoordinatesProvider ProviderDefault { get { return __ProviderDefault; } set { __ProviderDefault = value; } } private DxMapCoordinatesProvider __ProviderDefault;
        /// <summary>
        /// Defaultní typ mapy
        /// </summary>
        public DxMapCoordinatesMapType MapTypeDefault { get { return __MapTypeDefault; } set { __MapTypeDefault = value; } } private DxMapCoordinatesMapType __MapTypeDefault;
        /// <summary>
        /// Defaultní viditelnost bočního panelu s detaily.
        /// </summary>
        public bool InfoPanelVisibleDefault { get { return __InfoPanelVisibleDefault; } set { __InfoPanelVisibleDefault = value; } } private bool __InfoPanelVisibleDefault;
        /// <summary>
        /// Vložit špendlík na pozici Point (do URL adresy).
        /// Výchozí je true, odpovídá to chování, kdy máme dán jednoduchý koordinát a chceme jej vidět jako exaktní špendlík, nejen jako mapu v jeho okolí.
        /// </summary>
        public bool ShowPinAtPoint { get { return __ShowPinAtPoint; } set { __ShowPinAtPoint = value; } } private bool __ShowPinAtPoint;
        #endregion
        #region Coordinates : Práce se souřadnicemi (set a get, event o změně)
        /// <summary>
        /// Souřadnice v relativně čitelném stringu pro uživatele, v aktuálním formátu <see cref="CoordinatesFormat"/>
        /// </summary>
        public string Coordinates { get { return _GetCoordinates(); } set { _SetCoordinates(value); } }
        /// <summary>
        /// Souřadnice ve formě Nephrite: <b>50.0395802N, 14.4289607E</b>.
        /// Lze setovat libovolný formát, ale načten zpátky bude tento konkrétní formát.
        /// </summary>
        public string CoordinatesNephrite { get { return _GetCoordinates(DxMapCoordinatesFormat.Nephrite); } set { _SetCoordinates(value); } }
        /// <summary>
        /// Souřadnice v jednom stringu pevné struktury, lze porovnávat dvě různé souřadnice pomocí tohoto stringu.
        /// </summary>
        public string Point { get { return $"X:{_FormatDecimal(this.PointX, 12)}; Y:{_FormatDecimal(this.PointY, 12)}"; } }
        /// <summary>
        /// Vrátí textové vyjádření souřadnic v daném / aktuálním formátu
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public string GetCoordinates(DxMapCoordinatesFormat? format = null)
        {
            return _GetCoordinates(format);
        }
        /// <summary>
        /// Opíše do sebe informace z dodaného objektu, volitelně potlačí event 
        /// </summary>
        /// <param name="newCoordinates"></param>
        /// <param name="isSilent"></param>
        public void FillFrom(DxMapCoordinates newCoordinates, bool isSilent = false)
        {
            var coordinatesOld = _CoordinatesSerial;

            try
            {
                __IsSuppressedChanges = true;
                _FillFromAction(newCoordinates);
            }
            finally
            {
                __IsSuppressedChanges = false;
            }

            // Pokud nynější souřadnice jsou změněny a není Silent, vyvoláme event o změně:
            if (!isSilent)
                _CheckCoordinatesChanged(coordinatesOld);
        }
        /// <summary>
        /// Opíše do sebe informace z dodaného objektu
        /// </summary>
        /// <param name="newCoordinates"></param>
        private void _FillFromAction(DxMapCoordinates newCoordinates)
        {
            if (newCoordinates != null)
            {
                // Přenášíme hodnoty proměnných, tím vynecháváme vyhodnocování defaultů = defaulty akceptujeme naše. Máme přenést fyzické hodnoty načtených koordinátů, a ne jejich defaulty!
                this.__PointX = newCoordinates.__PointX;
                this.__PointY = newCoordinates.__PointY;
                this.__Zoom = newCoordinates.__Zoom;
                this.__CenterX = newCoordinates.__CenterX;
                this.__CenterY = newCoordinates.__CenterY;
                this.__Provider = newCoordinates.__Provider;
                this.__MapType = newCoordinates.__MapType;
                this.__InfoPanelVisible = newCoordinates.__InfoPanelVisible;
            }
        }
        /// <summary>
        /// Vloží dodané souřadnice. Poté vyvolá event o změně, pokud reálně došlo ke změně.
        /// </summary>
        /// <param name="coordinates"></param>
        private void _SetCoordinates(string coordinates)
        {
            var coordinatesOld = _CoordinatesSerial;

            try
            {
                __IsSuppressedChanges = true;
                _Reset(ResetSource.Coordinate);
                if (!String.IsNullOrEmpty(coordinates))
                    _SetCoordinatesAction(coordinates);
            }
            finally
            {
                __IsSuppressedChanges = false;
            }

            // Pokud nynější souřadnice jsou změněny, vyvoláme event o změně:
            _CheckCoordinatesChanged(coordinatesOld);
        }
        /// <summary>
        /// Vloží dodané souřadnice. Neřeší event o změně, jen řeší hodnoty.
        /// </summary>
        /// <param name="coordinates"></param>
        private void _SetCoordinatesAction(string coordinates)
        {
            //  Varianty:
            //     Databáze Nephrite
            //                                  50.0395802N, 14.4289607E
            //                                  POINT (14.4009383 50.0694664)
            //     Seznam:
            //  WGS84 stupně                    50.2091744N, 15.8317075E
            //  WGS84 stupně minuty             N 50°12.55047', E 15°49.90245'
            //  WGS84 stupně minuty vteřiny     50°12'33.028"N, 15°49'54.147"E
            //  OLC                             9F2Q6R5J+MM
            //  MGRS                            33UWR
            // 
            //     Google:
            //  Souřadnice                      50.20907681751956, 15.831757887689303
            //  Plus code                       6R5M+R26 Hradec Králové
            //     Další matrixy
            //                                  +9F2P.2CQHRH
            //                                  9F2P2CQH+RH

            // Velká písmena, odebrat krajní mezery, a pokud není formát "POINT" tak i vnitřní mezery (protože detekujeme jiné oddělovače):
            coordinates = coordinates.Trim().ToUpper();
            bool beginPoint = coordinates.StartsWith("POINT", StringComparison.InvariantCultureIgnoreCase) && (coordinates.Length > 7);
            if (!beginPoint)
                coordinates = coordinates.Replace(" ", "").ToUpper();

            bool hasSemiColon = coordinates.Contains(";");
            bool hasColon = coordinates.Contains(",");
            bool hasDot = coordinates.Contains(".");
            bool hasGrade = coordinates.Contains("°");
            bool hasN = coordinates.Contains("N");
            bool hasS = coordinates.Contains("S");
            bool hasE = coordinates.Contains("E");
            bool hasW = coordinates.Contains("W");
            bool hasQ = (hasN || hasS) && (hasE || hasW);            // Máme validní sadu kvadrantů

            decimal pointX, pointY;

            // Center nuluji, protože data vkládám do Point:
            this.CenterX = null;
            this.CenterY = null;

            // Desetinná čárka => tečka?
            if (!hasDot && hasColon && !hasGrade)
            {   // Nenalezena tečka, ale máme čárku => mohlo by jít o záměnu desetinného oddělovače
                // Ale jen když nejsou použity stupně, tam nemusí být desetinné číslo, když se používají úhlové vteřiny!
                coordinates = coordinates.Replace(",", ".");
                hasColon = false;
                hasDot = true;
            }

            // Vyhodnocení varianty zadání:
            if (beginPoint)
            {   // Point má speciální větev:
                coordinates = coordinates.Substring(5).Replace("(", "").Replace(")", "").Trim();      // "POINT (14.4009383 50.0694664)"  =>  "14.4009383 50.0694664"
                var pointParts = coordinates.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (pointParts.Length == 2 && _TryParseDecimal(pointParts[0], out pointX) && _TryParseDecimal(pointParts[1], out pointY))
                {
                    this.CoordinatesFormat = DxMapCoordinatesFormat.NephritePoint;
                    this.PointX = pointX;
                    this.PointY = pointY;
                    this.Zoom = null;                                // null převezme default, což je _ZoomTown
                }
            }
            else if (hasGrade)
            {   // Stupně mají 4 varianty: Wgs84MinuteSuffix, Wgs84MinutePrefix, Wgs84ArcSecSuffix, Wgs84ArcSecPrefix; a musí mít kvadrant:
                if (hasQ)
                {   // Máme validní kvadrant:
                    // Používají tečku jako desetinnou, a čárku k oddělení prvků:
                    var gradeParts = coordinates.Split(',');
                    if (gradeParts.Length == 2 && _TryParseGeoGrade(gradeParts[1], out var fmtX, out pointX) && _TryParseGeoGrade(gradeParts[0], out var fmtY, out pointY))
                    {
                        this.CoordinatesFormat = fmtX ?? fmtY ?? DxMapCoordinatesFormat.Wgs84ArcSecSuffix;
                        this.PointX = pointX;
                        this.PointY = pointY;
                        this.Zoom = null;                                // null převezme default, což je _ZoomTown
                    }
                }
            }
            else if (hasColon)
            {   // Máme oddělovač, máme tedy hodnotu před a za ním => Decimal má dvě varianty: 
                // Nemusíme mít kvadrant (připouštíme zadání se zápornými souřadnicemi, tedy celkem: 50.2091744N   N50.2091744   50.2091744)
                var decimalParts = coordinates.Split(',');
                if (decimalParts.Length == 2 && _TryParseGeoDecimal(decimalParts[1], out var fmtX, out pointX) && _TryParseGeoDecimal(decimalParts[0], out var fmtY, out pointY))
                {
                    this.CoordinatesFormat = fmtX ?? fmtY ?? DxMapCoordinatesFormat.Wgs84Decimal;
                    this.PointX = pointX;
                    this.PointY = pointY;
                    this.Zoom = null;                                // null převezme default, což je _ZoomTown
                }
            }
            else
            {   // Matrix kódy ještě neumím...

            }
        }
        /// <summary>
        /// Vrátí textové vyjádření souřadnic v daném / aktuálním formátu
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private string _GetCoordinates(DxMapCoordinatesFormat? format = null)
        {
            if (!format.HasValue) format = this.CoordinatesFormat;
            decimal pointX = this.PointX;
            decimal pointY = this.PointY;

            var fmt = format.Value;
            switch (fmt)
            {
                case DxMapCoordinatesFormat.Nephrite:
                    return $"{_FormatGeoDecimal(pointY, GeoAxis.Latitude, fmt, 12)}, {_FormatGeoDecimal(pointX, GeoAxis.Longitude, fmt, 12)}";
                case DxMapCoordinatesFormat.NephritePoint:
                    return $"POINT ({_FormatGeoDecimal(pointY, GeoAxis.Longitude, fmt, 12)} {_FormatGeoDecimal(pointX, GeoAxis.Latitude, fmt, 12)})";

                case DxMapCoordinatesFormat.Wgs84Decimal:
                    return $"{_FormatGeoDecimal(pointY, GeoAxis.Latitude, fmt, 12)}, {_FormatGeoDecimal(pointX, GeoAxis.Longitude, fmt, 12)}";
                case DxMapCoordinatesFormat.Wgs84DecimalSuffix:
                    return $"{_FormatGeoDecimal(pointY, GeoAxis.Latitude, fmt, 12)}, {_FormatGeoDecimal(pointX, GeoAxis.Longitude, fmt, 12)}";
                case DxMapCoordinatesFormat.Wgs84DecimalPrefix:
                    return $"{_FormatGeoDecimal(pointY, GeoAxis.Latitude, fmt, 12)}, {_FormatGeoDecimal(pointX, GeoAxis.Longitude, fmt, 12)}";

                case DxMapCoordinatesFormat.Wgs84MinuteSuffix:
                    return $"{_FormatGeoGrade(pointY, GeoAxis.Latitude, fmt)}, {_FormatGeoGrade(pointX, GeoAxis.Longitude, fmt)}";
                case DxMapCoordinatesFormat.Wgs84MinutePrefix:
                    return $"{_FormatGeoGrade(pointY, GeoAxis.Latitude, fmt)}, {_FormatGeoGrade(pointX, GeoAxis.Longitude, fmt)}";
                case DxMapCoordinatesFormat.Wgs84ArcSecSuffix:
                    return $"{_FormatGeoGrade(pointY, GeoAxis.Latitude, fmt)}, {_FormatGeoGrade(pointX, GeoAxis.Longitude, fmt)}";
                case DxMapCoordinatesFormat.Wgs84ArcSecPrefix:
                    return $"{_FormatGeoGrade(pointY, GeoAxis.Latitude, fmt)}, {_FormatGeoGrade(pointX, GeoAxis.Longitude, fmt)}";
            }

            return "";
        }
        /// <summary>
        /// Pokud aktuální hodnota <see cref="_CoordinatesSerial"/> je odlišná od dodané hodnoty (před změnami), a pokud není potlačeno <see cref="__IsSuppressedChanges"/>, pak vyvolá události o změně souřadnic
        /// </summary>
        /// <param name="coordinatesOld"></param>
        private void _CheckCoordinatesChanged(string coordinatesOld)
        {
            if (__IsSuppressedChanges) return;

            string coordinatesNew = _CoordinatesSerial;
            if (String.Equals(coordinatesOld, coordinatesNew)) return;

            _RunCoordinatesChanged();
        }
        /// <summary>
        /// Vyvolá event <see cref="CoordinatesChanged"/>, pokud nejsou eventy potlačeny <see cref="__IsSuppressedChanges"/>.
        /// </summary>
        private void _RunCoordinatesChanged()
        {
            if (__IsSuppressedChanges) return;
            CoordinatesChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost volaná po změně souřadnic
        /// </summary>
        public event EventHandler CoordinatesChanged;
        /// <summary>
        /// Aktuální souřadnice v jednoduchém stringu, pro detekci změny
        /// </summary>
        private string _CoordinatesSerial
        {
            get
            {
                string text = $"{_FormatDecimal(this.PointX, 12)};{_FormatDecimal(this.PointY, 12)};{_FormatIntN(this.Zoom)};";
                if (this.HasCenter)
                    text += $"{_FormatDecimalN(this.CenterX, 7)};{_FormatDecimalN(this.CenterY, 7)};";
                return text;
            }
        }
        /// <summary>
        /// Potlačení eventů o změně, pokud zde bude hodnota true.
        /// </summary>
        private bool __IsSuppressedChanges;
        #endregion
        #region UrlAdress : Práce s URL adresou (set, get, analýza i syntéza, event o změně), konkrétní providery (Seznam, Google, OpenMap)
        /// <summary>
        /// Metoda se pokusí analyzovat dodanou URL adresu a naplnit z ní data do new instance
        /// </summary>
        /// <param name="urlAdress"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public static bool TryParseUrlAdress(string urlAdress, out DxMapCoordinates coordinates)
        {
            coordinates = null;
            if (!Uri.TryCreate(urlAdress, UriKind.RelativeOrAbsolute, out var uri)) return false;

            if (_TryParseUriAsSeznamMapy(uri, ref coordinates)) return true;
            if (_TryParseUriAsFrameMapy(uri, ref coordinates)) return true;
            if (_TryParseUriAsGoogleMaps(uri, ref coordinates)) return true;
            if (_TryParseUriAsOpenStreetMap(uri, ref coordinates)) return true;

            return false;
        }
        /// <summary>
        /// URL adresa mapy
        /// </summary>
        public string UrlAdress { get { return _GetUrlAdress(); } set { _SetUrlAdress(value); } }
        /// <summary>
        /// Vygeneruje a vrátí URL pro daný / aktuální provider (Seznam / Google / OpenStreet) a daný / aktuální typ mapy.
        /// </summary>
        /// <returns></returns>
        private string _GetUrlAdress(DxMapCoordinatesProvider? provider = null, DxMapCoordinatesMapType? mapType = null)
        {
            if (!provider.HasValue) provider = this.Provider;

            switch (provider.Value)
            {
                case DxMapCoordinatesProvider.SeznamMapy:
                    return _GetUrlAdressSeznamMapy(mapType);
                case DxMapCoordinatesProvider.FrameMapy:
                    return _GetUrlAdressFrameMapy(mapType);
                case DxMapCoordinatesProvider.GoogleMaps:
                    return _GetUrlAdressGoogleMaps(mapType);
                case DxMapCoordinatesProvider.OpenStreetMap:
                    return _GetUrlAdressOpenStreetMap(mapType);
                default:
                    return _GetUrlAdressSeznamMapy(mapType);
            }
        }
        /// <summary>
        /// Vloží dodanou URL adresu do koordinátů
        /// </summary>
        /// <param name="urlAdress"></param>
        private void _SetUrlAdress(string urlAdress)
        {
        }
        #region SeznamMapy
        /// <summary>
        /// Pokusí se analyzovat URI obsahující URL adresu, jako souřadnice v provideru SeznamMapy
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static bool _TryParseUriAsSeznamMapy(Uri uri, ref DxMapCoordinates coordinates)
        {
            return _TryParseUriAsSeznamCommon(uri, "mapy.cz", ref coordinates, DxMapCoordinatesProvider.SeznamMapy);
        }
        /// <summary>
        /// Vygeneruje a vrátí URL pro provider SeznamMapy a daný / aktuální typ mapy.
        /// </summary>
        /// <param name="mapType"></param>
        /// <returns></returns>
        private string _GetUrlAdressSeznamMapy(DxMapCoordinatesMapType? mapType = null)
        {
            return _GetUrlAdressSeznamCommon(mapType, "https://mapy.cz/", DxMapCoordinatesProvider.SeznamMapy);
        }
        /// <summary>
        /// Pokusí se analyzovat URI obsahující URL adresu, jako souřadnice v provideru SeznamMapy nebo FrameMapy
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="web"></param>
        /// <param name="coordinates"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private static bool _TryParseUriAsSeznamCommon(Uri uri, string web, ref DxMapCoordinates coordinates, DxMapCoordinatesProvider provider)
        {
            string host = uri.Host;
            if (!String.Equals(host, web, StringComparison.OrdinalIgnoreCase)) return false;

            string localPath = uri.LocalPath;
            DxMapCoordinatesMapType mapType =
                (String.Equals(localPath, "/zakladni") ? DxMapCoordinatesMapType.Standard :
                (String.Equals(localPath, "/letecka") ? DxMapCoordinatesMapType.Photo :
                (String.Equals(localPath, "/turisticka") ? DxMapCoordinatesMapType.Nature :
                (String.Equals(localPath, "/dopravni") ? DxMapCoordinatesMapType.Traffic :
                (String.Equals(localPath, "/humanitarni") ? DxMapCoordinatesMapType.Specific : DxMapCoordinatesMapType.None)))));
            if (mapType == DxMapCoordinatesMapType.None) return false;

            var queryData = _ParseQuery(uri.Query, '?', '&');                  // ?&source=coor&id=15.795172900000000%2C49.949911300000000&x=15.7951729&y=49.9499113&z=8
            if (queryData is null) return false;

            // Pokud najdu X a Y, pak máme souřadnice, jinak nikoliv.
            _SearchValue("x", queryData, out decimal centerX, out bool hasX);
            _SearchValue("y", queryData, out decimal centerY, out bool hasY);
            bool hasXY = hasX && hasY;
            if (!hasXY) return false;

            //  Další atributy jsou optional a budeme je řešit podle toho, zda je najdeme:
            coordinates = new DxMapCoordinates();
            coordinates.Provider = provider;
            coordinates.CenterX = centerX;
            coordinates.CenterY = centerY;
            bool hasPoint = false;

            //  Další atributy jsou optional:
            _SearchValue("z", queryData, out int zoom, out bool hasZoom);
            if (hasZoom) coordinates.Zoom = zoom;

            _SearchValue("source", queryData, out string source, out bool hasSource);
            _SearchValue("id", queryData, out string id, out bool hasId);
            if (hasSource && String.Equals(source, "coor") && hasId && !String.IsNullOrEmpty(id) && id.Contains("%2C"))
            {   // Pokud v URL najdu: "source=coor&id=15.795172900000000%2C49.949911300000000"
                var coords = id.Split(new string[] { "%2C" }, StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length == 2 && _TryParseDecimal(coords[0], out var pointX) && _TryParseDecimal(coords[1], out var pointY))
                {
                    coordinates.PointX = pointX;
                    coordinates.PointY = pointY;
                    hasPoint = true;
                }
            }

            // Není dán explicitní bod => jako Point akceptuji Center, ten je spolehlivě načten:
            if (!hasPoint)
            {
                coordinates.PointX = coordinates.CenterX.Value;
                coordinates.PointY = coordinates.CenterY.Value;
            }

            // Postranní panel:
            bool hasPanel = false;
            if (provider == DxMapCoordinatesProvider.SeznamMapy)
            {   // Parametr "l" má hodnotu "0" = nezobrazit panel; nepřítomnost parametru = zobrazit panel
                _SearchValue("l", queryData, out string sidePanel, out bool hasSidePanel);
                hasPanel = !hasSidePanel || (hasSidePanel && !String.Equals(sidePanel, "0"));
            }
            coordinates.InfoPanelVisible = hasPanel;

            // OK:
            return true;
        }
        /// <summary>
        /// Vygeneruje a vrátí URL pro provider SeznamMapy nebo FrameMapy a daný / aktuální typ mapy.
        /// </summary>
        /// <param name="mapType"></param>
        /// <param name="web"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private string _GetUrlAdressSeznamCommon(DxMapCoordinatesMapType? mapType, string web, DxMapCoordinatesProvider provider)
        {
            // https://mapy.cz/zakladni?l=0&source=coor&id=15.782303855847147%2C49.990992469096604&x=15.7821322&y=49.9893301&z=16

            // Typ mapy:
            if (!mapType.HasValue) mapType = this.MapType;
            string mapTypeUrl = (mapType.Value == DxMapCoordinatesMapType.Standard ? "zakladni" :
                                (mapType.Value == DxMapCoordinatesMapType.Photo ? "letecka" :
                                (mapType.Value == DxMapCoordinatesMapType.Nature ? "turisticka" :
                                (mapType.Value == DxMapCoordinatesMapType.Traffic ? "dopravni" :
                                (mapType.Value == DxMapCoordinatesMapType.Specific ? "humanitarni" : "zakladni")))));

            // Následují parametry v Query. Typicky začínají (názvem proměnné) = (hodnota proměnné) a končí &
            // Poslední & bude nakonec odebrán.

            // Postranní panel? Pouze u providera Seznam. (Provider Frame nemá panel z principu).
            bool withPanel = this.InfoPanelVisible.Value && provider == DxMapCoordinatesProvider.SeznamMapy;
            string sidePanel = (withPanel ? "" : "l=0&");

            // Střed mapy: pokud je dán exaktně (HasCenter) pak jej akceptuji, jinak Pin:
            bool hasCenter = this.HasCenter;
            decimal pointX = this.PointX;
            decimal pointY = this.PointY;
            decimal centerX = (hasCenter ? this.CenterX.Value : pointX);
            decimal centerY = (hasCenter ? this.CenterY.Value : pointY);
            int zoom = this.Zoom.Value;

            // Umístit špendlík do bodu Point? Ano pokud je požadováno, anebo pokud je dán střed (pak Point může být jinde):
            string pinPoint = "";
            bool addPoint = this.ShowPinAtPoint || hasCenter;
            if (addPoint)
            {
                pinPoint = $"source=coor&id={_FormatDecimal(pointX, 15)}%2C{_FormatDecimal(pointY, 15)}&";
            }

            // Střed mapy a Zoom:
            string mapData = $"x={_FormatDecimal(centerX, 7)}&y={_FormatDecimal(centerY, 7)}&z={_FormatInt(zoom)}&";

            // Složit URL:
            string urlAdress = $"{web}{mapTypeUrl}?{sidePanel}{pinPoint}{mapData}";
            if (urlAdress.EndsWith("&")) urlAdress = urlAdress.Substring(0, urlAdress.Length - 1);
            return urlAdress;
        }

        //   https://mapy.cz/zakladni?x=15.7701152&y=49.9681588&z=10               základní
        //   https://mapy.cz/letecka?x=15.7701152&y=49.9681588&z=10                letecká
        //   https://mapy.cz/turisticka?x=15.7701152&y=49.9681588&z=10             turistická
        //   https://mapy.cz/dopravni?x=15.7701152&y=49.9681588&z=10               dopravní
        //   https://mapy.cz/zakladni?l=0&x=15.7701152&y=49.9681588&z=10               základní bez postranního panelu
        //   https://mapy.cz/zakladni?source=coor&id=15.936798397021448%2C50.06913748494087&x=15.9456819&y=50.0629944&z=14           co je zde - bodově
        //   https://mapy.cz/zakladni?source=muni&id=2560&x=15.8354324&y=50.0215148&z=12                                             co je zde - obec
        //   https://mapy.cz/zakladni?source=stre&id=112413&x=15.9639638&y=50.0608455&z=14                                           co je zde - ulice
        //   https://mapy.cz/zakladni?source=addr&id=12769313&x=15.9154587&y=50.0302891&z=16                                         co je zde - číslo popisné, adresa s fotkou
        //   https://mapy.cz/zakladni?l=0&source=coor&id=15.90928966136471%2C50.03222574216687&x=15.9146702&y=50.0303891&z=17        co je zde - bez pravého panelu, ale bod zájmu tam je

        #endregion
        #region FrameMapy
        /// <summary>
        /// Pokusí se analyzovat URI obsahující URL adresu, jako souřadnice v provideru FrameMapy
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static bool _TryParseUriAsFrameMapy(Uri uri, ref DxMapCoordinates coordinates)
        {
            return _TryParseUriAsSeznamCommon(uri, "frame.mapy.cz", ref coordinates, DxMapCoordinatesProvider.FrameMapy);
        }
        /// <summary>
        /// Vygeneruje a vrátí URL pro provider FrameMapy a daný / aktuální typ mapy.
        /// </summary>
        /// <param name="mapType"></param>
        /// <returns></returns>
        private string _GetUrlAdressFrameMapy(DxMapCoordinatesMapType? mapType = null)
        {
            return _GetUrlAdressSeznamCommon(mapType, "https://frame.mapy.cz/", DxMapCoordinatesProvider.SeznamMapy);
        }
        #endregion
        #region GoogleMaps
        /// <summary>
        /// Pokusí se analyzovat URI obsahující URL adresu, jako souřadnice v provideru GoogleMaps
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static bool _TryParseUriAsGoogleMaps(Uri uri, ref DxMapCoordinates coordinates)
        {
            var queryData = _ParseQuery(uri.Query, '@', ',', '&', '?', '/');                  // ?&source=coor&id=15.795172900000000%2C49.949911300000000&x=15.7951729&y=49.9499113&z=8

            return false;
        }
        /// <summary>
        /// Vygeneruje a vrátí URL pro provider GoogleMaps a daný / aktuální typ mapy.
        /// </summary>
        /// <param name="mapType"></param>
        /// <returns></returns>
        private string _GetUrlAdressGoogleMaps(DxMapCoordinatesMapType? mapType = null)
        {
            if (!mapType.HasValue) mapType = this.MapType;

            // https://www.google.com/maps/@49.296045,17.390038,15z?hl=cs-CZ

            string web = "https://www.google.com/maps/";

            // Střed mapy: pokud je dán exaktně (HasCenter) pak jej akceptuji, jinak Pin:
            bool hasCenter = this.HasCenter;
            decimal pointX = this.PointX;
            decimal pointY = this.PointY;
            decimal centerX = (hasCenter ? this.CenterX.Value : pointX);
            decimal centerY = (hasCenter ? this.CenterY.Value : pointY);
            int zoom = this.Zoom.Value;

            // Umístit špendlík do bodu Point? Ano pokud je požadováno, anebo pokud je dán střed (pak Point může být jinde):
            //   Jak na to:   https://developers.google.com/maps/documentation/embed/embedding-map
            //                https://blog.hubspot.com/website/how-to-embed-google-map-in-html
            //   iframe   
            //  <iframe src="https://www.google.com/maps/embed?pb=!1m10!1m8!1m3!1d5134.491529403346!2d15.812057030195488!3d49.95049212566805!3m2!1i1024!2i768!4f13.1!5e0!3m2!1scs!2scz!4v1727839457077!5m2!1scs!2scz" width="600" height="450" style="border:0;" allowfullscreen="" loading="lazy" referrerpolicy="no-referrer-when-downgrade"></iframe>
            string pinPoint = "";
            bool addPoint = this.ShowPinAtPoint || hasCenter;
            if (addPoint)
            {
                pinPoint = $"source=coor&id={_FormatDecimal(pointX, 15)}%2C{_FormatDecimal(pointY, 15)}&";
            }

            // Střed mapy a Zoom:
            string mapData = $"@{_FormatDecimal(centerY,12)},{_FormatDecimal(centerX,12)},{_FormatInt(zoom)}z";

            // Fixně:
            string lang = "hl=cs-CZ";

            string urlAdress = $"{web}{mapData}?{lang}";
            return urlAdress;
        }
        //   https://www.google.com/maps/@49.296045,17.390038,15z?hl=cs-CZ
        //   https://www.google.com/maps/@49.296045,17.390038,15z?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D                      základní
        //   https://www.google.com/maps/@49.296045,17.390038,2823m/data=!3m1!1e3?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D      fotomapa
        //   https://www.google.com/maps/@49.296045,17.390038,15z/data=!5m1!1e1?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D        provoz
        //   https://www.google.com/maps/@49.296045,17.390038,15z/data=!5m1!1e2?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D        veřejná doprava
        //   https://www.google.com/maps/@49.296045,17.390038,15z/data=!5m2!1e4!1e2?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D    terén
        //   https://www.google.com/maps/@49.3012574,17.345449,16z?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D
        #endregion
        #region OpenStreetMap
        /// <summary>
        /// Pokusí se analyzovat URI obsahující URL adresu, jako souřadnice v provideru OpenStreetMap
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static bool _TryParseUriAsOpenStreetMap(Uri uri, ref DxMapCoordinates coordinates)
        {
            string host = uri.Host;
            if (!String.Equals(host, "www.openstreetmap.org", StringComparison.OrdinalIgnoreCase)) return false;

            var queryData = _ParseQuery(uri.Query, '?', '&', '#');             // Query:     ?mlat=50.0385298802&mlon=15.77897773802
            var fragmentData = _ParseQuery(uri.Fragment, '?', '&', '#');       // Fragment:  #map=14/50.03853/15.77898
            if (queryData is null && fragmentData is null) return false;

            _SearchValue("mlat", queryData, out decimal pointY, out bool hasPointY);     // PointY = 50.0385298802
            _SearchValue("mlon", queryData, out decimal pointX, out bool hasPointX);     // PointY = 15.77897773802

            _SearchValue("map", fragmentData, out string center, out bool hasCenter);    // 14/50.03853/15.77898
            int zoom = 0;
            decimal centerX = 0m;
            decimal centerY = 0m;
            bool hasValidMapData = false;
            if (hasCenter && !String.IsNullOrEmpty(center) && center.Contains("/"))
            {
                var centerParts = center.Split('/');
                hasValidMapData = (centerParts.Length == 3 &&
                                    _TryParseInt(centerParts[0], out zoom) &&
                                    _TryParseDecimal(centerParts[1], out centerY) &&
                                    _TryParseDecimal(centerParts[2], out centerX));
            }
            if (!hasValidMapData) return false;

            // Pokud není uveden Point (mlat + mlon), tak jej převezmu z Center:
            if (!hasPointX) pointX = centerX;
            if (!hasPointY) pointY = centerY;

            // Výstup:
            coordinates = new DxMapCoordinates();
            coordinates.Provider = DxMapCoordinatesProvider.OpenStreetMap;
            coordinates.PointX = pointX;
            coordinates.PointY = pointY;
            coordinates.CenterX = centerX;
            coordinates.CenterY = centerY;
            coordinates.Zoom = zoom;

            return true;
        }
        /// <summary>
        /// Vygeneruje a vrátí URL pro provider OpenStreetMap a daný / aktuální typ mapy.
        /// </summary>
        /// <param name="mapType"></param>
        /// <returns></returns>
        private string _GetUrlAdressOpenStreetMap(DxMapCoordinatesMapType? mapType = null)
        {
            if (!mapType.HasValue) mapType = this.MapType;

            string web = "https://www.openstreetmap.org/";
            string mapTypeLayer = (mapType.Value == DxMapCoordinatesMapType.Traffic ? "T" : "");

            // Střed mapy: pokud je dán exaktně (HasCenter) pak jej akceptuji, jinak Pin:
            bool hasCenter = this.HasCenter;
            decimal pointX = this.PointX;
            decimal pointY = this.PointY;
            decimal centerX = (hasCenter ? this.CenterX.Value : pointX);
            decimal centerY = (hasCenter ? this.CenterY.Value : pointY);
            int zoom = this.Zoom.Value;

            // Umístit špendlík do bodu Point? Ano pokud je požadováno, anebo pokud je dán střed (pak Point může být jinde):
            string pinPoint = "";
            string pinPointLayer = "";
            bool addPoint = this.ShowPinAtPoint || hasCenter;
            if (addPoint)
            {
                pinPoint = $"?mlat={_FormatDecimal(pointY, 12)}&mlon={_FormatDecimal(pointX, 12)}";
                pinPointLayer = "";
            }

            // Střed mapy a Zoom:
            string mapData = $"#map={_FormatInt(zoom)}/{_FormatDecimal(centerY, 7)}/{_FormatDecimal(centerX, 7)}";

            // Vrstvy:
            string layers = mapTypeLayer + pinPointLayer;
            if (layers.Length > 0)
                layers = "&layers=" + layers;           // &layers=N


            // Složit URL:
            string urlAdress = $"{web}{pinPoint}{mapData}{layers}";
            return urlAdress;
        }

        //   https://www.openstreetmap.org/#map=14/49.94349/15.79452&layers=N
        //   https://www.openstreetmap.org/?mlat=49.999988&mlon=15.757273#map=19/49.999988/15.757271&layers=T          obsahuje umístěnou značku - a bez postranních panelů a bez cizích poznámek
        //   https://www.openstreetmap.org/?mlat=49.999988&mlon=15.757273#map=16/50.04246/15.82406                     jiné měřítko, std mapa
        //   https://www.openstreetmap.org/#map=14/49.94349/15.79452&layers=N
        //   https://www.openstreetmap.org/#map=12/49.9320/15.7875&layers=N
        //   https://www.openstreetmap.org/directions?from=&to=50.0238%2C15.6009#map=12/50.0280/15.6105&layers=P       Zadaný cíl cesty (From-To), a malý panel vlevo

        #endregion
        #region Support pro analýzu UrlQuery
        /// <summary>
        /// Dodaný string (query z URL) rozdělí (danými separátory) na jednotlivé hodnoty (Key = Value) a jejich seznam vrátí. Prázdné prvky do seznamu nedává.
        /// Pokud by výsledný seznam měl 0 prvků, vrátí null. Jinými slovy, pokud na výstupu není null, pak je tam alespoň jeden prvek.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        private static List<KeyValuePair<string, string>> _ParseQuery(string query, params char[] separators)
        {
            if (String.IsNullOrEmpty(query)) return null;
            var result = new List<KeyValuePair<string, string>>();
            var parts = query.Split(separators);

            foreach (var part in parts)
            {
                int length = part.Length;
                if (length > 0)
                {
                    int eqx = part.IndexOf('=');
                    if (eqx < 0)                                                              // 15.29z
                        result.Add(new KeyValuePair<string, string>(part, null));
                    else if (eqx == 0 && length == 1)                                         // =
                        result.Add(new KeyValuePair<string, string>(part, null));
                    else if (eqx == 0 && length > 1)                                          // =abcd
                        result.Add(new KeyValuePair<string, string>("", part.Substring(1)));
                    else if (eqx > 0 && eqx < (length - 1))                                   // x=15.7967442
                        result.Add(new KeyValuePair<string, string>(part.Substring(0, eqx), part.Substring(eqx + 1)));
                    else if (eqx > 0 && eqx == (length - 1))                                  // 15.7967442=
                        result.Add(new KeyValuePair<string, string>(part, null));
                    else
                        result.Add(new KeyValuePair<string, string>(part, null));
                }
            }

            return (result.Count > 0 ? result : null);
        }
        private static void _SearchValue(string key, List<KeyValuePair<string, string>> queryData, out string value, out bool found)
        {
            if (_TrySearchPair(key, queryData, out var pair))
            {
                value = pair.Value;
                found = true;
            }
            else
            {
                value = null;
                found = false;
            }
        }
        private static void _SearchValue(string key, List<KeyValuePair<string, string>> queryData, out decimal value, out bool found)
        {
            if (_TrySearchPair(key, queryData, out var pair) && _TryParseDecimal(pair.Value, out var number))
            {
                value = number;
                found = true;
            }
            else
            {
                value = 0m;
                found = false;
            }
        }
        private static void _SearchValue(string key, List<KeyValuePair<string, string>> queryData, out int value, out bool found)
        {
            if (_TrySearchPair(key, queryData, out var pair) && _TryParseInt(pair.Value, out var number))
            {
                value = number;
                found = true;
            }
            else
            {
                value = 0;
                found = false;
            }
        }
        private static bool _TrySearchPair(string key, List<KeyValuePair<string, string>> queryData, out KeyValuePair<string, string> value)
        {
            return (queryData.TryGetFirst(kvp => String.Equals(kvp.Key, key), out value));
        }
        #endregion
        #endregion
        #region Privátní support (Parse, Format, Align)
        private static decimal _ParseDecimal(string text)
        {
            var value = _ParseDecimalN(text);
            return value ?? 0m;
        }
        private static bool _TryParseDecimal(string text, out decimal value)
        {
            var valueN = _ParseDecimalN(text);
            value = valueN ?? 0m;
            return valueN.HasValue;
        }
        private static decimal? _ParseDecimalN(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (text.Contains(".") && _DotChar != ".") text = text.Replace(".", _DotChar);
                if (Decimal.TryParse(text, out decimal value)) return value;
            }
            return null;
        }
        private static int _ParseInt(string text)
        {
            if (!String.IsNullOrEmpty(text) && Int32.TryParse(text, out int value)) return value;
            return 0;
        }
        private static bool _TryParseInt(string text, out int value)
        {
            if (!String.IsNullOrEmpty(text) && Int32.TryParse(text, out value)) return true;
            value = 0;
            return false;
        }
        private static bool _TryParseGeoDecimal(string text, out DxMapCoordinatesFormat? format, out decimal value)
        {
            format = null;
            value = 0m;

            // Západ a Jih jsou záporné hodnoty:
            bool isNegative = (text.Contains("W") || text.Contains("S"));
            text = text.Trim();

            // Začínáme nebo končíme značkou světové strany?  Detekujeme a poté ji odebereme:
            //  "50°12'45.612''N"   =>   "50°12'45.612''"
            bool startWith = (text.StartsWith("N") || text.StartsWith("S") || text.StartsWith("E") || text.StartsWith("W"));
            bool endsWith = (text.EndsWith("N") || text.EndsWith("S") || text.EndsWith("E") || text.EndsWith("W"));
            if (startWith && endsWith) return false;
            text = text.Replace("N", "")
                       .Replace("S", "")
                       .Replace("E", "")
                       .Replace("W", "");
            if (text.Length == 0) return false;

            // Nyní máme obyčejné číslo "50.2091744"
            if (!_TryParseDecimal(text, out var number)) return false;

            // OK: odvodíme formát a určíme výslednou hodnotu (aplikujeme negativní pro Sourh / West):
            format = (startWith ? DxMapCoordinatesFormat.Wgs84DecimalPrefix : (endsWith ? DxMapCoordinatesFormat.Wgs84DecimalSuffix : DxMapCoordinatesFormat.Wgs84Decimal));
            value = (isNegative ? -number : number);
            return true;
        }
        private static bool _TryParseGeoGrade(string text, out DxMapCoordinatesFormat? format, out decimal value)
        {
            format = null;
            value = 0m;

            // Západ a Jih jsou záporné hodnoty:
            bool isNegative = (text.Contains("W") || text.Contains("S"));
            text = text.Trim();

            // Začínáme nebo končíme značkou světové strany?  Detekujeme a poté ji odebereme:
            //  "50°12'45.612''N"   =>   "50°12'45.612''"
            bool startWith = (text.StartsWith("N") || text.StartsWith("S") || text.StartsWith("E") || text.StartsWith("W"));
            bool endsWith = (text.EndsWith("N") || text.EndsWith("S") || text.EndsWith("E") || text.EndsWith("W"));
            if (startWith && endsWith) return false;
            text = text.Replace("N", "")
                       .Replace("S", "")
                       .Replace("E", "")
                       .Replace("W", "");
            if (text.Length == 0) return false;

            // Obsahujeme sekundy?  Detekujeme a poté sjednotíme oddělovače:
            //   "50°12'45.612''"   =>  "50;12;45.612;"
            bool hasArcSec = text.Contains("''");
            text = text.Replace("°", ";")
                       .Replace("''", ";")
                       .Replace("'", ";");

            // Text (konvertovaný na sekvenci "50;12;45.612;") rozdělíme na:  50   12   45.612
            //  a postupně převedeme na jedno decimal číslo (za každý další dílec přičítáme hodnotu dělenou dělitelem 1, 60, 3600:
            bool hasValidParts = false;
            var numParts = text.Split(';');
            decimal result = 0m;
            decimal divider = 1m;
            foreach (var numPart in numParts)
            {
                if (numPart.Length > 0 && _TryParseDecimal(numPart, out var number))
                {
                    result += (divider == 1m ? number : (number / divider));
                    hasValidParts = true;
                }
                divider = divider * 60m;
            }
            if (!hasValidParts) return false;

            // OK: odvodíme formát a určíme výslednou hodnotu (aplikujeme negativní pro Sourh / West):
            format = (startWith ? (hasArcSec ? DxMapCoordinatesFormat.Wgs84ArcSecPrefix : DxMapCoordinatesFormat.Wgs84MinutePrefix) : (hasArcSec ? DxMapCoordinatesFormat.Wgs84ArcSecSuffix : DxMapCoordinatesFormat.Wgs84MinuteSuffix));
            value = (isNegative ? -result : result);
            return true;
        }
        private static string _FormatGeoDecimal(decimal value, GeoAxis axis, DxMapCoordinatesFormat format, int decimals = 7)
        {
            bool usePrefix = (format == DxMapCoordinatesFormat.Wgs84DecimalPrefix);
            bool useSuffix = (format == DxMapCoordinatesFormat.Nephrite || format == DxMapCoordinatesFormat.Wgs84DecimalSuffix);
            if (usePrefix || useSuffix)
            {
                string quadrant = _FormatGeoQuadrant(ref value, axis);                   // Zajistí kladnou hodnotu value, a vrátí znak S/N | W/E pro hodnotu a osu GeoAxis
                string text = _FormatDecimal(value, decimals);
                return (usePrefix ? $"{quadrant}{text}" : $"{text}{quadrant}");          // N50.2091744  nebo  50.2091744N
            }
            else
            {
                value = _Align(value, axis);
                string text = _FormatDecimal(value, decimals);                           // -180 až 180  nebo  -90 až 90
                return text;
            }
        }
        private static string _FormatGeoGrade(decimal value, GeoAxis axis, DxMapCoordinatesFormat format)
        {
            bool usePrefix = (format == DxMapCoordinatesFormat.Wgs84MinutePrefix || format == DxMapCoordinatesFormat.Wgs84ArcSecPrefix);
            bool useSuffix = (format == DxMapCoordinatesFormat.Wgs84MinuteSuffix || format == DxMapCoordinatesFormat.Wgs84ArcSecSuffix);
            bool useArcSec = (format == DxMapCoordinatesFormat.Wgs84ArcSecSuffix || format == DxMapCoordinatesFormat.Wgs84ArcSecPrefix);

            if (usePrefix || useSuffix)
            {
                string quadrant = _FormatGeoQuadrant(ref value, axis);                   // Zajistí kladnou hodnotu value, a vrátí znak S/N | W/E pro hodnotu a osu GeoAxis
                string text = _FormatGrade(value, useArcSec);
                return (usePrefix ? $"{quadrant} {text}" : $"{text} {quadrant}");        // N 50°12'45.2''  nebo  50°12'45.2'' N
            }
            else
            {
                value = _Align(value, axis);
                string text = _FormatGrade(value, useArcSec);
                return text;
            }
        }
        private static string _FormatGrade(decimal value, bool useArc)
        {
            bool isNegative = (value < 0m);
            if (isNegative) value = -value;
            string neg = (isNegative ? "-" : "");                    // "-" pro negativní čísla
            decimal grd = Math.Floor(value);                         // 16.75844  =>  16         
            value = 60m * (value - grd);                             // 16.75844  =>   0.75844    =>  45.5064
            if (!useArc)
            {   // 16°45.506425'
                return $"{neg}{_FormatDecimal(grd, 1, 0)}°{_FormatDecimal(value, 2, 6)}'";
            }
            else
            {   // 16°45'30.512
                decimal min = Math.Floor(value);                     // 45.5064   =>  45
                value = 60m * (value - min);                         // 45.5064   =>   0.5064     =>  30.384
                return $"{neg}{_FormatDecimal(grd, 1, 0)}°{_FormatDecimal(min, 2, 0)}'{_FormatDecimal(value, 2, 3)}''";
            }
        }
        /// <summary>
        /// Vrátí kvadrant : N (North) nebo S (South) nebo E (East) nebo W (West) pro danou souřadnici
        /// </summary>
        /// <param name="position"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static string _FormatGeoQuadrant(ref decimal position, GeoAxis axis)
        {
            switch (axis)
            {
                case GeoAxis.Latitude:
                    // Rozsah -90 (jižní) až +90° (severní) pól:
                    bool isSouth = (position < 0m);
                    if (isSouth) position = -position;
                    if (position > 90m) position = 90m;
                    return (isSouth ? "S" : "N");
                case GeoAxis.Longitude:
                    // Rozsah -180 (západní) až +180° (východní) polokoule:
                    bool isWest = (position < 0m);
                    if (isWest) position = -position;
                    if (position > 180m) position = 180m;
                    return (isWest ? "W" : "E");
            }
            return "X";
        }
        private static string _FormatDecimal(decimal value, int decimals = 7)
        {
            decimals = _Align(decimals, 0, 12);
            string fmt1 = "".PadRight(1, '0').PadLeft(12, '#');
            string fmt2 = "".PadRight(decimals, '0');
            string format = $"{fmt1}.{fmt2}";
            string text = (Math.Round(value, decimals)).ToString(format);
            text = text.Replace(_DotChar, ".").Replace(" ", "");
            if (text.Contains(".") && decimals > 0)
            {
                while (text.Length > 1 && text.EndsWith("0"))
                    text = text.Substring(0, (text.Length - 1));
            }
            return text;
        }
        private static string _FormatDecimal(decimal value, int leadings, int decimals = 7)
        {
            leadings = _Align(leadings, 0, 12);
            decimals = _Align(decimals, 0, 12);
            string fmt1 = "".PadRight(leadings, '0').PadLeft(12, '#');
            string fmt2 = "".PadRight(decimals, '0');
            string format = $"{fmt1}.{fmt2}";
            string text = (Math.Round(value, decimals)).ToString(format);
            text = text.Replace(_DotChar, ".").Replace(" ", "");
            if (text.Contains(".") && decimals > 0)
            {
                while (text.Length > 1 && text.EndsWith("0"))
                    text = text.Substring(0, (text.Length - 1));
            }
            return text;
        }
        private static string _FormatDecimalN(decimal? value, int decimals = 7)
        {
            return (value.HasValue ? _FormatDecimal(value.Value, decimals) : "");
        }
        private static string _FormatInt(int value)
        {
            string text = value.ToString();
            return text.Replace(" ", "");
        }
        private static string _FormatIntN(int? value)
        {
            return (value.HasValue ? _FormatInt(value.Value) : "");
        }
        private static int _Align(int value, int min, int max) { return (value < min ? min : (value > max ? max : value)); }
        private static int? _AlignN(int? value, int min, int max) { return (value.HasValue ? (int?)(value < min ? min : (value > max ? max : value)) : (int?)null); }
        private static decimal _Align(decimal value, decimal min, decimal max) { return (value < min ? min : (value > max ? max : value)); }
        private static decimal _Align(decimal value, GeoAxis axis)
        {
            switch (axis)
            {
                case GeoAxis.Latitude:
                    // Rozsah -90 (jižní) až +90° (severní) pól:
                    return _Align(value, -90m, 90m);
                case GeoAxis.Longitude:
                    // Rozsah -180 (západní) až +180° (východní) polokoule:
                    return _Align(value, -180m, 180m);
            }
            return value;
        }
        private static decimal? _Align(decimal? value, decimal min, decimal max) { return (value.HasValue ? (decimal?)(value.Value < min ? min : (value.Value > max ? max : value.Value)) : (decimal?)null); }
        /// <summary>
        /// Osy zeměpisné
        /// </summary>
        private enum GeoAxis 
        {
            /// <summary>
            /// Neurčeno
            /// </summary>
            None,
            /// <summary>
            /// Zeměpisná pozice Y od rovníku (0) na sever k pólu (+90 = severní pól) a na jih (-90 = jižní pól).<br/>
            /// Praha má cca 50°
            /// </summary>
            Latitude,
            /// <summary>
            /// Zeměpisná pozice X od Greenwiche (0) na východ (+180 = přes Východní Evropu a Asii) nebo na západ (-180 = přes Portugalsko, Grónsko k Americe a na Havaj).<br/>
            /// Praha má cca 14.4°
            /// </summary>
            Longitude
        }
        #endregion
    }
    #region Enumy pro mapy
    /// <summary>
    /// Formát koordinátů
    /// </summary>
    public enum DxMapCoordinatesFormat
    {
        //     Databáze Nephrite
        //                                  50.0395802N, 14.4289607E
        //                                  POINT (14.4009383 50.0694664)
        //     Seznam:
        //  WGS84 stupně                    50.2091744N, 15.8317075E
        //  WGS84 stupně minuty             N 50°12.55047', E 15°49.90245'
        //  WGS84 stupně minuty vteřiny     50°12'33.028"N, 15°49'54.147"E
        //  OLC                             9F2Q6R5J+MM
        //  MGRS                            33UWR
        //     Google:
        //  Souřadnice                      50.20907681751956, 15.831757887689303
        //  Plus code                       6R5M+R26 Hradec Králové

        /// <summary>
        /// Nephrite: <b>50.0395802N, 14.4289607E</b>
        /// </summary>
        Nephrite,
        /// <summary>
        /// Nephrite Point: <b>POINT (14.4009383 50.0694664)</b>
        /// </summary>
        NephritePoint,
        /// <summary>
        /// WGS84 desetinný bez kvadrantu: <b>50.2091744, -15.8317075</b>
        /// </summary>
        Wgs84Decimal,
        /// <summary>
        /// WGS84 desetinný s kvadrantem za číslem: <b>50.2091744N, 15.8317075E</b>
        /// </summary>
        Wgs84DecimalSuffix,
        /// <summary>
        /// WGS84 desetinný s kvadrantem před číslem: <b>N50.2091744, E15.8317075</b>
        /// </summary>
        Wgs84DecimalPrefix,
        /// <summary>
        /// WGS84 stupně a minuty s kvadrantem za číslem: <b>50°12.45612'N, 15°45.252'E</b>
        /// </summary>
        Wgs84MinuteSuffix,
        /// <summary>
        /// WGS84 stupně a minuty s kvadrantem před číslem: <b>N 50°12.45612', E 15°45.252'</b>
        /// </summary>
        Wgs84MinutePrefix,
        /// <summary>
        /// WGS84 stupně a minuty a vteřiny s kvadrantem za číslem: <b>50°12'45.612''N, 15°45'25.25''E</b>
        /// </summary>
        Wgs84ArcSecSuffix,
        /// <summary>
        /// WGS84 stupně a minuty a vteřiny s kvadrantem před číslem: <b>N 50°12'45.612'', E 15°45'25.25''</b>
        /// </summary>
        Wgs84ArcSecPrefix,
        /// <summary>
        /// Číselná matice do QR kódů 1: "+9F2P.2CQHRH"
        /// </summary>
        OlcCode,
        /// <summary>
        /// Číselná matice do QR kódů 2: "9F2P2CQH+RH"
        /// </summary>
        PlusCode
    }
    /// <summary>
    /// Poskytovatel mapy
    /// </summary>
    public enum DxMapCoordinatesProvider
    {
        /// <summary>
        /// Žádná
        /// </summary>
        None,
        /// <summary>
        /// https://mapy.cz/
        /// </summary>
        SeznamMapy,
        /// <summary>
        /// https://frame.mapy.cz/
        /// </summary>
        FrameMapy,
        /// <summary>
        /// https://www.google.com/maps
        /// </summary>
        GoogleMaps,
        /// <summary>
        /// https://www.openstreetmap.org/
        /// </summary>
        OpenStreetMap
    }
    /// <summary>
    /// Varianta mapy
    /// </summary>
    public enum DxMapCoordinatesMapType
    {
        /// <summary>
        /// Neurčeno / nezadáno
        /// </summary>
        None = 0,
        /// <summary>
        /// Základní
        /// </summary>
        Standard,
        /// <summary>
        /// Fotomapa
        /// </summary>
        Photo,
        /// <summary>
        /// Dopravní provoz
        /// </summary>
        Traffic,
        /// <summary>
        /// Turistická / přírodní
        /// </summary>
        Nature,
        /// <summary>
        /// Speciální = rozpoznané jiné
        /// </summary>
        Specific
    }
    #endregion
    #endregion
    #region Enumy, servisní třídy...
    /// <summary>
    /// Typy akcí, které má provést Parent panel
    /// </summary>
    [Flags]
    internal enum DxWebViewActionType
    {
        /// <summary>
        /// Nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Přepočítej layout controlů
        /// </summary>
        DoLayout = 0x0001,
        /// <summary>
        /// Přenačti Enabled
        /// </summary>
        DoEnabled = 0x0002,
        /// <summary>
        /// Zobrazit statický obrázek
        /// </summary>
        DoShowStaticPicture = 0x0010,
        /// <summary>
        /// Proveď změnu URL adresy
        /// </summary>
        DoChangeSourceUrl = 0x0020,
        /// <summary>
        /// Proveď změnu textu ve StatusBaru
        /// </summary>
        DoChangeStatusText = 0x0040,
        /// <summary>
        /// Proveď změnu titulku
        /// </summary>
        DoChangeDocumentTitle = 0x0080,
        /// <summary>
        /// Znovu načti hodnotu a zobraz ji
        /// </summary>
        DoReloadValue = 0x0100
    }
    /// <summary>
    /// Data pro událost o načtení dat CaptureImage
    /// </summary>
    public class MsWebImageCapturedArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="imageData"></param>
        public MsWebImageCapturedArgs(object requestId, byte[] imageData)
        {
            this.RequestId = requestId;
            this.ImageData = imageData;
        }
        /// <summary>
        /// ID požadavku
        /// </summary>
        public object RequestId { get; private set; }
        /// <summary>
        /// Data obrázku
        /// </summary>
        public byte[] ImageData { get; private set; }
    }
    /// <summary>
    /// Předpis pro handler události CaptureImage
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void MsWebImageCapturedHandler(object sender, MsWebImageCapturedArgs args);
    #endregion
}
