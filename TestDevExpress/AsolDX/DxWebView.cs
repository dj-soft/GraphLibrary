using System;
using System.Collections.Generic;
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
            var properties = this.MsWebProperties;

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
            bool isStatusVisible = this.MsWebProperties.IsStatusRowVisible;
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
            var properties = this.MsWebProperties;

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
        /// Aktualizuje Picture z okna živého webu, podle nastavení <see cref="MsWebProperties"/>: <see cref="MsWebView.PropertiesInfo.IsStaticPicture"/>
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowStaticPicture()
        {
            var properties = this.MsWebProperties;
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
            var properties = this.MsWebProperties;
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
            var properties = this.MsWebProperties;
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
            var properties = this.MsWebProperties;
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
            var properties = this.MsWebProperties;
            if (properties.IsAdressEditorVisible && properties.IsAdressEditorEditable)
                this._AdressNavigate();
        }
        private void _AdressNavigate()
        {
            var properties = this.MsWebProperties;
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
            __MsWebView.MsWebCurrentSourceUrlChanged += _MsWebCurrentSourceUrlChanged;
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
        /// Volá se při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebProperties"/>: <see cref="MsWebView.PropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.PropertiesInfo.CanGoForward"/>.
        /// </summary>
        protected virtual void OnMsWebCurrentCanGoEnabledChanged() { }
        /// <summary>
        /// Event při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebProperties"/>: <see cref="MsWebView.PropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.PropertiesInfo.CanGoForward"/>.
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

        private void _MsWebCurrentSourceUrlChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.SourceUrlChanged");
            _TryInternalCaptureMsWebImage();
            OnMsWebCurrentSourceUrlChanged();
            MsWebCurrentSourceUrlChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně URL adresy.
        /// </summary>
        protected virtual void OnMsWebCurrentSourceUrlChanged() { }
        /// <summary>
        /// Event při změně URL adresy.
        /// </summary>
        public event EventHandler MsWebCurrentSourceUrlChanged;

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
            var properties = this.MsWebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (!isStaticPicture) return;

            string url = __MsWebView.MsWebProperties.UrlAdress;
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

            var properties = this.MsWebProperties;
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
        public MsWebView.PropertiesInfo MsWebProperties { get { return __MsWebView.MsWebProperties; } }
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

            __ToolPanel = new DxPanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            __OpenExternalBrowserButton = DxComponent.CreateDxSimpleButton(3, 3, 24, 24, __ToolPanel, "", _ShowMapClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameBackDisabled);
            __SearchCoordinatesButton = DxComponent.CreateDxSimpleButton(30, 3, 24, 24, __ToolPanel, "", _FindAdressClick, DevExpress.XtraEditors.Controls.PaintStyles.Light, resourceName: ImageNameForwardDisabled);
            __CoordinatesText = DxComponent.CreateDxTextEdit(96, 3, 250, __ToolPanel);
            __CoordSystemSpin = DxComponent.CreateDxSpinEdit(350, 3, 30, __ToolPanel);

            __OpenExternalBrowserButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __OpenExternalBrowserButton.TabStop = false;
            __SearchCoordinatesButton.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __SearchCoordinatesButton.TabStop = false;
            __CoordinatesText.Enter += _AdressEntered;
            __CoordinatesText.KeyDown += _AdressKeyDown;
            __CoordinatesText.KeyPress += _AdressKeyPress;
            __CoordinatesText.Leave += _AdressLeaved;
            __CoordinatesText.LostFocus += _AdressLostFocus;
            __CoordinatesText.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __CoordinatesText.TabStop = false;
            __CoordSystemSpin.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            __CoordSystemSpin.TabStop = false;

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
            var webProperties = this.MsWebProperties;
            var mapProperties = this.MapProperties;

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
            bool isToolbarVisible = webProperties.IsToolbarVisible;
            this.__ToolPanel.Visible = isToolbarVisible;
            if (isToolbarVisible)
            {
                int toolLeft = paddingX;
                int shiftX = buttonSize * 9 / 8;
                int distanceX = shiftX - buttonSize;

                bool isOpenExternalBrowserVisible = mapProperties.IsOpenExternalBrowserVisible;
                __OpenExternalBrowserButton.Visible = isOpenExternalBrowserVisible;
                if (isOpenExternalBrowserVisible)
                {
                    __OpenExternalBrowserButton.Bounds = new System.Drawing.Rectangle(toolLeft, buttonTop, buttonSize, buttonSize);
                    toolLeft += shiftX;
                }

                bool isSearchCoordinatesVisible = mapProperties.IsSearchCoordinatesVisible;
                __SearchCoordinatesButton.Visible = isSearchCoordinatesVisible;
                {
                    __SearchCoordinatesButton.Bounds = new System.Drawing.Rectangle(toolLeft, buttonTop, buttonSize, buttonSize);
                    toolLeft += shiftX;
                }

                bool isCoordinatesTextVisible = mapProperties.IsCoordinatesTextVisible;
                __CoordinatesText.Visible = isCoordinatesTextVisible;
                __CoordSystemSpin.Visible = isCoordinatesTextVisible;
                if (isCoordinatesTextVisible)
                {
                    int ctHeight = __CoordinatesText.Bounds.Height;
                    int ctTop = buttonTop + buttonSize - ctHeight - 1;
                    int ctWidth = 250;
                    __CoordinatesText.Bounds = new System.Drawing.Rectangle(toolLeft, ctTop, ctWidth, ctHeight);
                    toolLeft += (ctWidth + distanceX);

                    __CoordSystemSpin.Bounds = new System.Drawing.Rectangle(toolLeft, buttonTop, buttonSize, buttonSize);
                    toolLeft += shiftX;
                }

                __ToolPanel.Bounds = new System.Drawing.Rectangle(left, top, width, toolHeight);
                top = top + toolHeight;
            }

            // Statusbar:
            bool isStatusVisible = this.MsWebProperties.IsStatusRowVisible;
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
        }
        /// <summary>
        /// Nastaví Enabled na patřičné prvky, odpovídající aktuálnímu stavu MsWebView.
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoEnabled()
        {
            var webProperties = this.MsWebProperties;
            var mapProperties = this.MapProperties;

            // Toolbar
            bool isToolbarVisible = webProperties.IsToolbarVisible;
            if (isToolbarVisible)
            {
                bool isMapEditable = mapProperties.IsMapEditable;
                __CoordinatesText.Enabled = isMapEditable;
            }
        }
        /// <summary>
        /// Aktualizuje Picture z okna živého webu, podle nastavení <see cref="MsWebProperties"/>: <see cref="MsWebView.PropertiesInfo.IsStaticPicture"/>
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoShowStaticPicture()
        {
            var properties = this.MsWebProperties;
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
            var properties = this.MsWebProperties;
            bool isVisible = properties.IsToolbarVisible && properties.IsAdressEditorVisible;
            bool isInEditState = isVisible && properties.IsAdressEditorEditable && this.__AdressEditorHasFocus;
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
            var properties = this.MsWebProperties;
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
        private DxSimpleButton __OpenExternalBrowserButton;
        private DxSimpleButton __SearchCoordinatesButton;
        private DxTextEdit __CoordinatesText;
        private DxSpinEdit __CoordSystemSpin;
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
        private void _ShowMapClick(object sender, EventArgs args)
        {
            this.__MsWebView.GoBack();
            this.__MsWebView.Focus();
        }
        private void _FindAdressClick(object sender, EventArgs args)
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
            __AdressValueOnEnter = __CoordinatesText.Text;
        }
        private void _AdressKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
        }
        private void _AdressKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            var properties = this.MsWebProperties;
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
            var properties = this.MsWebProperties;
            if (properties.IsAdressEditorVisible && properties.IsAdressEditorEditable)
                this._AdressNavigate();
        }
        private void _AdressNavigate()
        {
            var properties = this.MsWebProperties;
            properties.UrlAdress = __CoordinatesText.Text;
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
            __MsWebView.MsWebCurrentSourceUrlChanged += _MsWebCurrentSourceUrlChanged;
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
        /// Volá se při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebProperties"/>: <see cref="MsWebView.PropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.PropertiesInfo.CanGoForward"/>.
        /// </summary>
        protected virtual void OnMsWebCurrentCanGoEnabledChanged() { }
        /// <summary>
        /// Event při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="MsWebProperties"/>: <see cref="MsWebView.PropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.PropertiesInfo.CanGoForward"/>.
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

        private void _MsWebCurrentSourceUrlChanged(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.SourceUrlChanged");
            _TryInternalCaptureMsWebImage();
            OnMsWebCurrentSourceUrlChanged();
            MsWebCurrentSourceUrlChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně URL adresy.
        /// </summary>
        protected virtual void OnMsWebCurrentSourceUrlChanged() { }
        /// <summary>
        /// Event při změně URL adresy.
        /// </summary>
        public event EventHandler MsWebCurrentSourceUrlChanged;

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
            var properties = this.MsWebProperties;
            bool isStaticPicture = properties.IsStaticPicture;
            if (!isStaticPicture) return;

            string url = __MsWebView.MsWebProperties.UrlAdress;
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

            var properties = this.MsWebProperties;
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
        public MsWebView.PropertiesInfo MsWebProperties { get { return __MsWebView.MsWebProperties; } }
        /// <summary>
        /// Vlastnosti mapy (souřadnice, zoom)
        /// </summary>
        public MapPropertiesInfo MapProperties { get { return __MapProperties; } } private MapPropertiesInfo __MapProperties;
        /// <summary>
        /// Definice vlastností mapy v <see cref="DxMapViewPanel"/>
        /// </summary>
        public class MapPropertiesInfo : IDisposable
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
            /// Nastaví výchozí hodnoty
            /// </summary>
            private void _InitValues()
            {
                __IsOpenExternalBrowserVisible = true;
                __IsMapEditable = true;
                __IsSearchCoordinatesVisible = true;
            }
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

        }
        #endregion
    }
    public class DxMapCoordinates
    {
        public DxMapCoordinates()
        {
            __DotChar = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            _Reset();
        }
        /// <summary>
        /// Oddělovač desetinných míst v aktuální kultuře, pracuje s ním <see cref="Decimal.TryParse(string, out decimal)"/>
        /// </summary>
        private string __DotChar;
        public string Coordinates { get { return _GetCoordinates(); } set { _SetCoordinates(value); } }
        public string UrlAdress { get { return _GetUrlAdress(); } set { _SetUrlAdress(value); } }

        public decimal CenterX { get { return __CenterX; } set { __CenterX = _Align((value % 360m), 0m, 360m); } } private decimal __CenterX;
        public decimal CenterY { get { return __CenterY; } set { __CenterY = _Align((value % 360m), -180m, 180m); } } private decimal __CenterY;
        public int Zoom { get { return __Zoom; } set { __Zoom = _Align(value, 1, 19); } } private int __Zoom;
        public bool HasPoint { get { return (this.PointX.HasValue && this.PointY.HasValue); } }
        public decimal? PointX { get { return __PointX; } set { __PointX = _Align((value % 360m), 0m, 360m); } } private decimal? __PointX;
        public decimal? PointY { get { return __PointY; } set { __PointY = _Align((value % 360m), -180m, 180m); } } private decimal? __PointY;


        private static int _Align(int value, int min, int max) { return (value < min ? min : (value > max ? max : value)); }
        private static decimal _Align(decimal value, decimal min, decimal max) { return (value < min ? min : (value > max ? max : value)); }
        private static decimal? _Align(decimal? value, decimal min, decimal max) { return (value.HasValue ? (decimal?)(value.Value < min ? min : (value.Value > max ? max : value.Value)) : (decimal?)null); }
        private void _SetCoordinates(string coordinates)
        {
            _Reset();
            if (String.IsNullOrEmpty(coordinates)) return;

            //  Varianty:
            // 15.7951729;49.9499113;15
            // 14.4289607, 50.0395802
            // 50.0395802N, 14.4289607E
            // N 50.0395802, E 14.4289607
            // 50°2'22,49"N, 14°25'44,26"E
            // N 50°2'22,49", E 14°25'44,26"
            // +9F2P.2CQHRH
            // 9F2P2CQH+RH
            coordinates = coordinates.Replace(" ", "").ToUpper();

            bool hasSemiColon = coordinates.Contains(";");
            bool hasColon = coordinates.Contains(",");
            bool hasGrade = coordinates.Contains("°");
            bool hasNSEW = (coordinates.Contains("N") || coordinates.Contains("S") || coordinates.Contains("E") || coordinates.Contains("W"));
            bool beginNS = (coordinates.StartsWith("N") || coordinates.StartsWith("S"));

            // Vyhodnocení varianty zadání:

            // Analýza dat konkrétní varianty:

            var parts = coordinates.Split(';');
            int count = parts.Length;
            if (count >= 2)
            {
                this.CenterX = _ParseDecimal(parts[0]);
                this.CenterY = _ParseDecimal(parts[1]);
                this.Zoom = (count >= 3 ? _ParseInt(parts[2]) : 12);
                this.PointX = (count >= 5 ? _ParseDecimalN(parts[3]) : (decimal?)null);
                this.PointY = (count >= 5 ? _ParseDecimalN(parts[4]) : (decimal?)null);
                return;
            }

            // Nevalidní:
            return;


        }
        private string _GetCoordinates()
        {
            string text = $"{this.CenterX}; {this.CenterY}; {this.Zoom}";
            if (this.HasPoint) text += $";{this.PointX}; {this.PointY}";
            text = text.Replace(",", ".");
            return text;
        }
        private void _Reset()
        {
            // https://mapy.cz/turisticka?l=0&x=15.7435513&y=49.8152928&z=8
            this.CenterX = 15.7435513m;
            this.CenterY = 49.8152928m;
            this.PointX = null;
            this.PointY = null;
            this.Zoom = 8;
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


        //   https://www.google.cz/maps/@49.9464515,15.7884627,15z?entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D
        //   https://www.google.com/maps/@49.296045,17.390038,15z?hl=cs-CZ
        //   https://www.google.com/maps/@49.296045,17.390038,15z?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D                      základní
        //   https://www.google.com/maps/@49.296045,17.390038,2823m/data=!3m1!1e3?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D      fotomapa
        //   https://www.google.com/maps/@49.296045,17.390038,15z/data=!5m1!1e1?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D        provoz
        //   https://www.google.com/maps/@49.296045,17.390038,15z/data=!5m1!1e2?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D        veřejná doprava
        //   https://www.google.com/maps/@49.296045,17.390038,15z/data=!5m2!1e4!1e2?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MDkyMi4wIKXMDSoASAFQAw%3D%3D    terén

        //   https://www.openstreetmap.org/#map=14/49.94349/15.79452&layers=N
        //   https://www.openstreetmap.org/#map=12/49.9320/15.7875&layers=N

        private string _GetUrlAdress() 
        {
            string web = "https://mapy.cz/";
            string variant = "zakladni";                // turisticka  letecka  dopravni
            string centerX = _FormatDecimal(this.CenterX);
            string centerY = _FormatDecimal(this.CenterY);
            string pointX = _FormatDecimalN(this.PointX);
            string pointY = _FormatDecimalN(this.PointY);
            string zoom = _FormatInt(this.Zoom);

            string urlAdress;

            // https://mapy.cz/zakladni?l=0&source=coor&id=15.782303855847147%2C49.990992469096604&x=15.7821322&y=49.9893301&z=16
            pointX = centerX;
            pointY = centerY;
            string point = $"&source=coor&id={pointX}%2C{pointY}";

            urlAdress = $"{web}{variant}?l=0{point}&x={centerX}&y={centerY}&z={zoom}";

            return urlAdress;
        }
        private void _SetUrlAdress(string urlAdress)
        { }



        private decimal _ParseDecimal(string text)
        {
            var value = _ParseDecimalN(text);
            return value ?? 0m;
        }
        private decimal? _ParseDecimalN(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (text.Contains(".") && __DotChar != ".") text = text.Replace(".", __DotChar);
                if (Decimal.TryParse(text, out decimal value)) return value;
            }
            return null;
        }
        private int _ParseInt(string text)
        {
            if (!String.IsNullOrEmpty(text) && Int32.TryParse(text, out int value)) return value;
            return 0;
        }
        private string _FormatDecimal(decimal value)
        {
            string text = value.ToString();
            return text.Replace(__DotChar, ".").Replace(" ", "");
        }
        private string _FormatDecimalN(decimal? value)
        {
            return (value.HasValue ? _FormatDecimal(value.Value) : "");
        }
        private string _FormatInt(int value)
        {
            string text = value.ToString();
            return text.Replace(" ", "");
        }
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
        #endregion
        #region Lazy inicializace, Web events, navigace, atd...

        // public static string 

        /// <summary>
        /// Provede start inicializace EnsureCoreWebView2Async
        /// </summary>
        private void _InitWebCore()
        {
            this.__CoreWebInitializerCounter = 1;
            this.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties()
            {
                UserDataFolder = @"c:\Shared\TestDevExpress\WebView_UserData"
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
            var properties = this.MsWebProperties;
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
            __MsWebUrlAdress = null;
            __MsWebHtmlContent = null;
            __MsWebNeedNavigate = false;
            __MsWebCurrentSourceUrl = null;
            __MsWebCurrentStatusText = null;
            _RunMsWebCurrentSourceUrlChanged();
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
            bool isChangeSourceUrl = !String.Equals(__MsWebCurrentSourceUrl, newSourceUrl, StringComparison.InvariantCulture);
            bool isChangeStatusText = !String.Equals(__MsWebCurrentStatusText, newStatusText, StringComparison.InvariantCulture);

            __MsWebCanGoBack = newCanGoBack;
            __MsWebCanGoForward = newCanGoForward;
            __MsWebCurrentDocumentTitle = newDocumentTitle;
            __MsWebCurrentSourceUrl = newSourceUrl;
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
                _RunMsWebCurrentSourceUrlChanged();

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
        /// Vyvolá události při změně URL adresy.
        /// </summary>
        private void _RunMsWebCurrentSourceUrlChanged()
        {
            OnMsWebCurrentSourceUrlChanged();
            MsWebCurrentSourceUrlChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při změně URL adresy.
        /// </summary>
        protected virtual void OnMsWebCurrentSourceUrlChanged() { }
        /// <summary>
        /// Event při změně URL adresy.
        /// </summary>
        public event EventHandler MsWebCurrentSourceUrlChanged;

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
        private string __MsWebCurrentSourceUrl;
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
        /// Na rozdíl od toho aktuální adresa je v <see cref="MsWebCurrentSourceUrl"/>.
        /// </summary>
        public string MsWebUrlAdress 
        {
            get { return __MsWebUrlAdress; }
            set
            {
                _RunMsWebCurrentClear();
                __MsWebUrlAdress = value;
                __MsWebNeedNavigate = !String.IsNullOrEmpty(value);
                _DoNavigate();
            }
        }
        /// <summary>
        /// Požadovaná URL adresa
        /// </summary>
        private string __MsWebUrlAdress;
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
        public string MsWebCurrentSourceUrl { get { return __MsWebCurrentSourceUrl; } }
        /// <summary>
        /// Aktuální text Status baru
        /// </summary>
        public string MsWebCurrentStatusText { get { return __MsWebCurrentStatusText; } }
        /// <summary>
        /// Zajistí zobrazení požadované URL adresy <see cref="MsWebUrlAdress"/> nebo obsahu <see cref="MsWebHtmlContent"/>.
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
                    if (!String.IsNullOrEmpty(__MsWebUrlAdress))
                        this.Source = new Uri(__MsWebUrlAdress);
                    else if (!String.IsNullOrEmpty(__MsWebHtmlContent))
                        this.NavigateToString(__MsWebHtmlContent);
                    _RunMsWebNavigationStarted();                    // Událost po startu aktivní změny URL
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
        public PropertiesInfo MsWebProperties { get { return __MsWebProperties; } } private PropertiesInfo __MsWebProperties;
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
            public string UrlAdress { get { return __Owner.MsWebUrlAdress; }  set { __Owner.MsWebUrlAdress = value; } }
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
            public string CurrentSourceUrl { get { return __Owner.MsWebCurrentSourceUrl; } }
            /// <summary>
            /// Aktuální text Status baru
            /// </summary>
            public string CurrentStatusText { get { return __Owner.MsWebCurrentStatusText; } }
        }
        #endregion
    }
    #region Enumy, servisní třídy...
    /// <summary>
    /// Typy akcí, které má provést Parent panel
    /// </summary>
    [Flags]
    internal enum DxWebViewActionType
    {
        None = 0,
        DoLayout = 0x0001,
        DoEnabled = 0x0002,
        DoShowStaticPicture = 0x0010,
        DoChangeSourceUrl = 0x0020,
        DoChangeStatusText = 0x0040,
        DoChangeDocumentTitle = 0x0080
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
