// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using DevExpress.XtraRichEdit.Model.History;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX
{
    using Noris.Clients.Win.Components.AsolDX.Map;

    /// <summary>
    /// Panel obsahující <see cref="MsWebView"/> a jednoduchý toolbar (Back - Forward - Refresh - Adresa - Go)
    /// <para/>
    /// Tento Control obsahuje instanci <see cref="WebProperties"/>, pomocí které se control nastavuje a komunikuje s ním.<br/>
    /// Je možno nastavit touto cestou vlastosti:<br/>
    /// <see cref="Value"/> = text URL adresy, pro snadné zadávání hodnoty, shodný jako v <see cref="WebProperties"/> : <see cref="MsWebView.WebPropertiesInfo.UrlAdress"/>;<br/>
    /// <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> = Režim zobrazování, význam má primárně pro EmbeededEditor v Infragistic;<br/>
    /// <see cref="MsWebView.WebPropertiesInfo.CanOpenNewWindow"/> = Možnost otevírání nových oken;<br/>
    /// <see cref="MsWebView.WebPropertiesInfo.IsAdressEditorEditable"/> = Smí uživatel zadat adresu ručně?<br/>
    /// </summary>
    public class DxWebViewPanel : DxPanelControl, IDxWebViewParentPanel
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
        /// Vytvoří kompletní obsah a vyvolá <see cref="_DoLayout"/>
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
        void IDxWebViewParentPanel.DoAction(DxWebViewActionType actionTypes) { _DoAction(actionTypes); }
        /// <summary>
        /// Provede požadované akce. Je povoleno volat z threadu mimo GUI.
        /// </summary>
        /// <param name="actionTypes"></param>
        private void _DoAction(DxWebViewActionType actionTypes)
        {
            if (actionTypes == DxWebViewActionType.None) return;               // Pokud není co dělat, není třeba ničehož invokovati !
            if (this.IsDispose) return;

            if (this.InvokeRequired)
            {   // 1x invokace na případně vícero akci
                this.BeginInvoke(new Action<DxWebViewActionType>(_DoAction), actionTypes);
            }
            else
            {
                if (actionTypes.HasFlag(DxWebViewActionType.DoLayout)) this._DoLayout();
                if (actionTypes.HasFlag(DxWebViewActionType.DoEnabled)) this._DoEnabled();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeDisplayMode)) this._DoChangeDisplayMode();
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
            if (this.IsDispose) return;

            var webProperties = this.WebProperties;

            var size = this.ClientSize;
            int left = 0;
            int width = size.Width;
            int top = 0;
            int bottom = size.Height;

            int buttonSize = DxComponent.ZoomToGui(24, this.CurrentDpi);
            int toolHeight = DxComponent.ZoomToGui(30, this.CurrentDpi);
            int buttonTop = (toolHeight - buttonSize) / 2;
            int paddingX = 0; // buttonTop;
            int textMinWidth = DxComponent.ZoomToGui(200, this.CurrentDpi);
            int textMaxWidth = width;
            int shiftX = buttonSize * 9 / 8;
            int distanceX = shiftX - buttonSize;

            // Toolbar
            bool isToolbarVisible = webProperties.IsToolbarVisible;
            this.__ToolPanel.Visible = isToolbarVisible;
            if (isToolbarVisible)
            {
                // Viditelnost prvků Toolbaru:
                bool isBackForwardVisible = webProperties.IsBackForwardButtonsVisible;
                bool isRefreshVisible = webProperties.IsRefreshButtonVisible;
                bool isAdressVisible = webProperties.IsAdressEditorVisible;
                bool isAdressEditable = webProperties.IsAdressEditorEditable;
                bool isGoToVisible = isAdressVisible && isAdressEditable;

                // Sumární šířka zobrazených buttonů včetně distance X:
                int buttonsWidth = getWidth(isBackForwardVisible, buttonSize, distanceX) +           /* Back    */
                                   getWidth(isBackForwardVisible, buttonSize, distanceX) +           /* Forward */
                                   getWidth(isRefreshVisible, buttonSize, distanceX) +               /* Refresh */
                                   getWidth(isGoToVisible, buttonSize, distanceX);                   /* GoTo    */

                // Šířka textu, zarovnaná do Min - Max:
                int textWidth = width - buttonsWidth;
                textWidth = (textWidth < textMinWidth ? textMinWidth : (textWidth > textMaxWidth ? textMaxWidth : textWidth));

                // Postupně umístíme buttony a textbox:
                int toolLeft = paddingX;
                int buttonBottom = buttonTop + buttonSize;
                setBounds(__BackButton, isBackForwardVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);
                setBounds(__ForwardButton, isBackForwardVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);
                setBounds(__RefreshButton, isRefreshVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);
                setBounds(__AdressText, isAdressVisible, ref toolLeft, buttonBottom, textWidth, __AdressText.Height, 1, distanceX);
                setBounds(__GoToButton, isGoToVisible, ref toolLeft, buttonBottom, buttonSize, buttonSize, 0, distanceX);

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
            // MsWebView by měl v případě potřeby detekovat změnu velikosti a vyvolat znovunačtení obrázku a nakonec událost  ... namísto:   if (newPicBounds != oldPicBounds) _CaptureDisplayImage();


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

            // Toolbar
            bool isToolbarVisible = webProperties.IsToolbarVisible;
            if (isToolbarVisible)
            {
                bool isBackForwardVisible = webProperties.IsBackForwardButtonsVisible;
                if (isBackForwardVisible)
                {
                    bool canGoBack = webProperties.CanGoBack; ;
                    __BackButton.Enabled = canGoBack;
                    __BackButton.ImageName = (canGoBack ? ImageNameBackEnabled : ImageNameBackDisabled);

                    bool canGoForward = webProperties.CanGoForward;
                    __ForwardButton.Enabled = canGoForward;
                    __ForwardButton.ImageName = (canGoForward ? ImageNameForwardEnabled : ImageNameForwardDisabled);
                }

                bool isRefreshVisible = webProperties.IsRefreshButtonVisible;
                if (isRefreshVisible)
                {
                    bool isRefreshEnabled = true;
                    __RefreshButton.Enabled = isRefreshEnabled;
                    __RefreshButton.ImageName = (isRefreshEnabled ? ImageNameRefreshEnabled : ImageNameRefreshDisabled);
                }
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
            __ToolPanel = null;
            __BackButton = null;
            __ForwardButton = null;
            __RefreshButton = null;
            __AdressText = null;
            __GoToButton = null;
            __MsWebView = null;
            __PictureWeb = null;
            __StatusBar = null;
            __StatusText = null;
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
        #region Události z WebView a jejich vliv na koordináty
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
            __MsWebView.MsWebNavigationTotalCompleted += __MsWebNavigationTotalCompleted;
            __MsWebView.MsWebImageCaptured += _MsWebImageCaptured;
            __MsWebView.MsWebDisplayImageCaptured += _MsWebDisplayImageCaptured;

            __MsWebView.WebProperties.DelayedResponseWaitingMs = 100;
        }
        /// <summary>
        /// Vyvoláno po události při změně stavu historie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebHistoryChanged(object sender, EventArgs e)
        {
            OnMsWebCurrentCanGoEnabledChanged();
            MsWebCurrentCanGoEnabledChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno po události při změně titulku dokumentu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebCurrentDocumentTitleChanged(object sender, EventArgs e)
        {
            OnMsWebCurrentDocumentTitleChanged();
            MsWebCurrentDocumentTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno po události při změně URL adresy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebCurrentUrlAdressChanged(object sender, EventArgs e)
        {
            OnMsWebCurrentUrlAdressChanged();
            MsWebCurrentUrlAdressChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno po události při změně textu StatusBar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebCurrentStatusTextChanged(object sender, EventArgs e)
        {
            OnMsWebCurrentStatusTextChanged();
            MsWebCurrentStatusTextChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno před zahájením navigace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebNavigationBefore(object sender, EventArgs e)
        {
            __NavigationInProgress = true;
            OnMsWebNavigationBefore();
            MsWebNavigationBefore?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvolá události po zahájením navigace.
        /// </summary>
        private void _MsWebNavigationStarted(object sender, EventArgs e)
        {
            _ShowWebViewControl();
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
            __NavigationInProgress = false;
            _CaptureDisplayImage();
            OnMsWebNavigationCompleted();
            MsWebNavigationCompleted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Po dokončení navigace = WebView změnil obsah dokumentu (webová stránka) a má hotovo, včetně donačtení Delayed WebResponses.
        /// Důvody jsou různé: programová změna URL adresy (=změna koordinátů) anebo uživatelovo klikání a myší přetahování.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __MsWebNavigationTotalCompleted(object sender, EventArgs e)
        {
            OnMsWebNavigationTotalCompleted();
            MsWebNavigationTotalCompleted?.Invoke(this, EventArgs.Empty);
        }

        #endregion
        #endregion
        #region Public Eventy
        /// <summary>
        /// Volá se při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="WebProperties"/>: <see cref="MsWebView.WebPropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.WebPropertiesInfo.CanGoForward"/>.
        /// </summary>
        protected virtual void OnMsWebCurrentCanGoEnabledChanged() { }
        /// <summary>
        /// Event při změně v historii navigace, kdy se mění Enabled v proměnných <see cref="WebProperties"/>: <see cref="MsWebView.WebPropertiesInfo.CanGoBack"/> nebo <see cref="MsWebView.WebPropertiesInfo.CanGoForward"/>.
        /// </summary>
        public event EventHandler MsWebCurrentCanGoEnabledChanged;

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

        /// <summary>
        /// Volá se při dokončení navigace včetně DelayedRequest.
        /// </summary>
        protected virtual void OnMsWebNavigationTotalCompleted() { }
        /// <summary>
        /// Event při dokončení navigace včetně DelayedRequest.
        /// </summary>
        public event EventHandler MsWebNavigationTotalCompleted;

        /// <summary>
        /// Volá se po získání ImageCapture.
        /// </summary>
        protected virtual void OnMsWebImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání ImageCapture.
        /// </summary>
        public event MsWebImageCapturedHandler MsWebImageCaptured;

        /// <summary>
        /// Příznak, že proběhl event <see cref="_MsWebNavigationBefore"/>, ale dosud neproběhl event <see cref="_MsWebNavigationCompleted"/>.
        /// </summary>
        private bool __NavigationInProgress;
        #endregion
        #region Zachycování statického obrázku z WebView
        /// <summary>
        /// Získá obrázek aktuálního stavu WebView, a po jeho získání volá event event <see cref="MsWebImageCaptured"/>, kde bude načtený obrázek k dispozici.
        /// Jde o asynchronní metodu: řízení vrátí ihned, a až po dokončení akce vyvolá event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        public void LoadMsWebImageAsync(object requestId)
        {
            // Zahájí se async načítání obrázku z existujícího WebView a jeho aktuální adresy:
            this.__MsWebView.LoadMsWebImageAsync(requestId);
            // po načtení obrázku (async) bude vyvolán event __MsWebView.MsWebImageCaptured => _MsWebImageCaptured()
        }
        /// <summary>
        /// WebView dokončil získání Bitmapy = obraz webové stránky, je dodán v argumentu.
        /// Proběhne pouze po načítání pomocí <see cref="LoadMsWebImageAsync(object)"/>. 
        /// Nikoli pro načtení Image pro DisplayMode = Capture...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MsWebImageCaptured(object sender, MsWebImageCapturedArgs args)
        {
            // Externí událost:
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MapViewPanel.ExternalImageCaptured");
            OnMsWebImageCaptured(args);
            MsWebImageCaptured?.Invoke(this, args);
        }
        /// <summary>
        /// Reaguje na změnu <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/>.
        /// Po změně režimu na statický obrázek si vyžádá jeho získání z WebView, poté bude asynchronně obrázek zobrazen.
        /// Aktualizuje Picture z okna živého webu, podle nastavení <see cref="WebProperties"/>: <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/>
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoChangeDisplayMode()
        {
            var webProperties = this.WebProperties;
            bool useStaticPicture = webProperties.UseStaticPicture;
            if (useStaticPicture)
            {   // Static => požádáme o zachycení Image a pak jej vykreslíme:
                this._CaptureDisplayImage();
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
        /// Metodu volá zdejší panel vždy, když mohlo dojít ke změně obrázku, pokud je statický.
        /// Tedy: při změně velikosti controlu, při doběhnutí navigace, při vložení hodnoty <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> = true, atd<br/>
        /// Zdejší metoda sama otestuje, zda je vhodné získat Image, a pokud ano, pak o něj požádá <see cref="__MsWebView"/>. 
        /// <para/>
        /// Jde o asynchronní operaci, zdejší metoda tedy skončí okamžitě. 
        /// Po získání obrázku (po nějaké době) bude volán eventhandler <see cref="_MsWebImageCaptured(object, MsWebImageCapturedArgs)"/>, 
        /// který detekuje interní request a vyvolá <see cref="_ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs)"/>, a neprovede externí event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        private void _CaptureDisplayImage()
        {
            var webProperties = this.WebProperties;
            bool useStaticPicture = webProperties.UseStaticPicture;
            if (!useStaticPicture) return;

            string url = __MsWebView.WebProperties.UrlAdress;
            if (String.IsNullOrEmpty(url)) return;

            this._ShowWebViewControl();
            this.__PictureWeb.Image = null;

            // Zahájí se Async nebo Sync načítání dat (Bitmapa) obrázku z WebView:
            this.__MsWebView.CaptureImage();
            // Po načtení obrázku bude vyvolán event __MsWebView.MsWebDisplayImageCaptured => _MsWebDisplayImageCaptured()
            //  a tam si převezmeme Image a zobrazíme:
        }
        /// <summary>
        /// WebView dokončil získání Bitmapy = obraz webové stránky, je dodán v argumentu.
        /// Používá se pro načtení Image pro DisplayMode = Capture...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MsWebDisplayImageCaptured(object sender, MsWebImageCapturedArgs args)
        {
            // Interní získání Image podle DisplayMode:
            _ReloadInternalMsWebImageCaptured(args);
        }
        /// <summary>
        /// Zajistí fyzické zobrazení controlu WebView, proto aby mohla proběhnout navigace na cílovou URL adresu.
        /// WebView musí být Visible, jinak nic nedělá. 
        /// Pokud tedy aktuální DisplayMode je nějaký Capture, pak WebView byl dosud Invisible a je třeba jej zviditelnit = právě nyní!
        /// </summary>
        private void _ShowWebViewControl()
        {
            if (!this.__MsWebView.IsControlVisible || this.__PictureWeb.Visible)
            {   // Pokud je změna viditelnosti potřebná:
                try
                {
                    this.SuspendLayout();
                    if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;       // Musí být Visible, jinak nic nenačte
                    if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;              // Picture skryjeme, ponecháme Visible jen pro živý Web
                }
                finally
                {
                    this.ResumeLayout();
                }
            }
        }
        /// <summary>
        /// Metoda je volaná po získání dat CapturedImage (jsou dodána v argumentu) pro interní účely, na základě požadavku z metody <see cref="_CaptureDisplayImage"/>.
        /// Metoda promítne dodaná data do statického obrázku <see cref="__PictureWeb"/>, a v případě potřeby tenti obrázek zviditelní.
        /// </summary>
        /// <param name="args"></param>
        private void _ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs args)
        {
            if (!this.WebProperties.UseStaticPicture) return;

            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebViewPanel.InternalImageCaptured");

            // Any Thread => GUI:
            if (this.InvokeRequired) this.BeginInvoke(new Action(reloadMsWebImageCaptured));
            else reloadMsWebImageCaptured();

            // Fyzické načtení Image a další akce, v GUI threadu
            void reloadMsWebImageCaptured()
            {
                try
                {
                    this.SuspendLayout();
                    this.__PictureWeb.Image = args.GetImage();
                    if (this.__PictureWeb.Visible) this.__PictureWeb.Refresh();
                    if (!this.__PictureWeb.Visible) this.__PictureWeb.Visible = true;
                    if (this.__MsWebView.Visible) this.__MsWebView.Visible = false;
                }
                catch { }
                finally
                {
                    this.ResumeLayout();
                }
            }
        }
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Value tohoto panelu = souřadnice zadaného bodu.
        /// Obsahuje totéž jako <see cref="WebProperties"/> : <see cref="MsWebView.WebPropertiesInfo.UrlAdress"/>.
        /// </summary>
        public string Value { get { return this.WebProperties.UrlAdress; } set { if (!this.IsDispose) this.WebProperties.UrlAdress = value; } }
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení v jednom místě.
        /// </summary>
        public MsWebView.WebPropertiesInfo WebProperties { get { return __MsWebView.WebProperties; } }
        #endregion
    }
    /// <summary>
    /// Panel obsahující <see cref="MsWebView"/> pro zobrazení mapy, a jednoduchý toolbar pro zadání / editaci adresy.
    /// <para/>
    /// Tento Control obsahuje instanci <see cref="MapProperties"/>, pomocí které se control nastavuje a komunikuje s ním.<br/>
    /// Je možno nastavit touto cestou vlastosti:<br/>
    /// <see cref="Value"/> = text souřadnice, pro snadné zadávání hodnoty, shodný jako <see cref="MapPropertiesInfo.Coordinates"/>;<br/>
    /// <see cref="MapPropertiesInfo.Coordinates"/> = text souřadnice;<br/>
    /// <see cref="MapPropertiesInfo.CoordinatesProvider"/> = Provider = webová stránka s mapami;<br/>
    /// <see cref="MapPropertiesInfo.CoordinatesMapType"/> = Druh mapy;<br/>
    /// <see cref="MapPropertiesInfo.CoordinatesFormat"/> = Formát textu souřadnic při jejich čtení;<br/>
    /// <see cref="MapPropertiesInfo.CoordinatesChanged"/> = Událost, když uživatel interaktivně změní souřadnice;<br/>
    /// </summary>
    public class DxMapViewPanel : DxPanelControl, IDxWebViewParentPanel
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
        /// Vytvoří kompletní obsah a vyvolá <see cref="_DoLayout"/>
        /// </summary>
        protected void CreateContent()
        {
            this.SuspendLayout();

            __MapProperties = new MapPropertiesInfo(this);
            __WebCoordinates = new MapCoordinates();
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
        void IDxWebViewParentPanel.DoAction(DxWebViewActionType actionTypes) { _DoAction(actionTypes); }
        /// <summary>
        /// Provede požadované akce. Je povoleno volat z threadu mimo GUI.
        /// </summary>
        /// <param name="actionTypes"></param>
        private void _DoAction(DxWebViewActionType actionTypes)
        {
            if (actionTypes == DxWebViewActionType.None) return;               // Pokud není co dělat, není třeba ničehož invokovati !
            if (this.IsDispose) return;

            if (this.InvokeRequired)
            {   // 1x invokace na případně vícero akci
                this.BeginInvoke(new Action<DxWebViewActionType>(_DoAction), actionTypes);
            }
            else
            {
                if (actionTypes.HasFlag(DxWebViewActionType.DoLayout)) this._DoLayout();
                if (actionTypes.HasFlag(DxWebViewActionType.DoEnabled)) this._DoEnabled();
                if (actionTypes.HasFlag(DxWebViewActionType.DoChangeDisplayMode)) this._DoChangeDisplayMode();
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
            if (this.IsDispose) return;

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
                // Viditelnost prvků Toolbaru:
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
                __AcceptCoordinatesButtonVisible = isAcceptCoordinatesVisible;

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
            // MsWebView by měl v případě potřeby detekovat změnu velikosti a vyvolat znovunačtení obrázku a nakonec událost  ... namísto:   if (newPicBounds != oldPicBounds) _CaptureDisplayImage();


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
                __CoordinatesText.Enabled = true;
                __CoordinatesText.ReadOnly = !isMapEditable;

                // Button __AcceptCoordinatesButton má být viditelný podle hodnoty isMapEditable. Pokud se jeho Visible neshoduje s isMapEditable, pak provedu _DoLayout, tam se mu nastaví nejen Visible, ale provede se i správné umístění prvků: 
                if (__AcceptCoordinatesButtonVisible != isMapEditable)
                    _DoLayout();

                if (isMapEditable)
                    _RefreshAcceptButtonState();
            }

            // Mapa:
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
            //   Jsme komponenta  DxMapViewPanel : ta nezobrazuje URL, ale koordináty !!!
            //   A koordináty v textboxu se mění buď jejich zadáním zvenku, anebo akceptováním souřadnic z WebProperties.
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
            __ToolPanel = null;
            __OpenExternalBrowserButton = null;
            __SearchCoordinatesButton = null;
            __CoordinatesText = null;
            __ReloadMapButton = null;
            __AcceptCoordinatesButton = null;
            __MsWebView = null;
            __PictureWeb = null;
            __StatusBar = null;
            __StatusText = null;
        }
        private DxPanelControl __ToolPanel;
        private DxSimpleButton __OpenExternalBrowserButton;
        private DxSimpleButton __SearchCoordinatesButton;
        private DxButtonEdit __CoordinatesText;
        private DxSimpleButton __ReloadMapButton;
        private DxSimpleButton __AcceptCoordinatesButton;
        /// <summary>
        /// Nativní viditelnost prvku <see cref="__AcceptCoordinatesButton"/>.
        /// Tato viditelnost se vyhodnocuje i v <see cref="_DoEnabled"/>, a protože WinForm do viditelnosti konkrétního controlu zahrnují i jeho Parenty, tak si ji eviduje separátně...
        /// </summary>
        private bool __AcceptCoordinatesButtonVisible;
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

            // Pokud uživatel změní hodnotu textu, jde o změnu aplikační hodnoty (nikoli o prostý posun mapy):
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
        /// což pravděpodobně vyvolá event <see cref="MapCoordinates.CoordinatesChanged"/> a následně nové nastavení URL adresy ve WebView
        /// </summary>
        private void _CoordinatesAccept(string coordinates)
        {
            // Převezmu koordináty zadané v parametru (pochází z '__CoordinatesText.Text'), a vložím je do this.MapProperties.SetCoordinates().
            // To je "vnější hodnota" = obdoba Value nebo Text.
            // Její změna vyvolá event CoordinatesChanged a zavolá reload mapy => this.RefreshMap():
            this.IMapProperties.SetCoordinates(coordinates, true, true);       // Proběhne parsování souřadnic, po změně se vyvolá RefreshMap(), tam odtud pak _ReloadCoordinatesText(), tam se používá formát CoordinatesUserFormat
            this._RefreshCurrentUserFormat();                                  // Tady převezmu formát souřadnice z this.IMapProperties.MapCoordinates.CoordinatesFormat do this.CoordinatesUserFormat, a volá se event o změně UserFormat
            this._ReloadCoordinatesText();                                     // Tady znovu naformátuji text souřadnice this.IMapProperties.MapCoordinates podle nově nastaveného UserFormatu = tak jak to uživatel reálně vepsal
            this._RefreshAcceptButtonState();
        }
        /// <summary>
        /// Přenačte souřadnice do textboxu a zobrazí aktuální mapu
        /// </summary>
        private void _RefreshMap(bool force = false)
        {
            if (this.IsDispose) return;

            _ReloadCoordinatesText();                                          // Zobrazím do TextBoxu (bez eventu o změně) souřadnice Požadovaná souřadnice

            // Přenesu požadované souřadnice z this.MapProperties.MapCoordinates do WebCoordinates:
            var mapCoordinates = this.IMapProperties.MapCoordinates;           // Požadovaná souřadnice
            var webCoordinates = this.WebCoordinates;
            webCoordinates.FillFrom(mapCoordinates, true);

            // Přečtu URL a předám ji do WebView:
            string urlAdressNew = webCoordinates.UrlAdress;                    // URL adresa odpovídající aktuálním koordinátům
            var webView = this.__MsWebView;
            string urlAdressOld = webView.MsWebCurrentUrlAdress;
            if (!force && String.Equals(urlAdressNew, urlAdressOld)) return;   // Není force, a není změna URL adresy

            this.__MapCoordinateToUrlAdressChangeInProgress = true;
            this.__MapCoordinateUrlAdress = urlAdressNew;
            webView.MsWebRequestedUrlAdress = urlAdressNew;

            this._RefreshAcceptButtonState();

            webView.Focus();
        }
        /// <summary>
        /// Do textboxu vepíše souřadnice z dodaného koordinátu, v aktuálním formátu <see cref="CoordinatesUserFormat"/>.
        /// Změna textu do __CoordinatesText (setování) nevyvolá event o změně, protože tamní změnu řešíme jen po interaktivní klávese Enter.
        /// </summary>
        /// <param name="mapCoordinates"></param>
        private void _ReloadCoordinatesText(MapCoordinates mapCoordinates = null)
        {
            if (this.IsDispose) return;

            // Setování textu do __CoordinatesText nevyvolá event o změně, protože tamní změnu řešíme jen po klávese Enter.
            if (mapCoordinates is null) mapCoordinates = this.IMapProperties.MapCoordinates;
            this.__CoordinatesText.Text = mapCoordinates.GetCoordinates(this.CoordinatesUserFormat);
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
        /// Inicializuje objekt <see cref="__CoordinatesText"/> pro validní práci s formáty <see cref="MapCoordinatesFormat"/>
        /// </summary>
        private void _CoordinatesFormatInit()
        {
            __CoordinatesText.Properties.Buttons.Clear();
            __CoordinatesText.AddButton(DevExpress.XtraEditors.Controls.ButtonPredefines.SpinLeft, true, DxComponent.Localize(MsgCode.DxMapCoordinatesButtonText), DxComponent.Localize(MsgCode.DxMapCoordinatesButtonTitle));
            __CoordinatesText.AddButton(DevExpress.XtraEditors.Controls.ButtonPredefines.SpinRight, false, DxComponent.Localize(MsgCode.DxMapCoordinatesButtonText), DxComponent.Localize(MsgCode.DxMapCoordinatesButtonTitle));

            // Copy ... a Paste?
            // __CoordinatesText.AddButton("devav/actions/copy.svg", false, DxComponent.Localize(MsgCode.DxKeyActionClipCopyText), DxComponent.Localize(MsgCode.DxKeyActionClipCopyTitle), "Copy");

            __CoordinatesText.ButtonClick += _CoordinatesTextButtonClick;

            // Toto jsou formáty, ve kterých lze zobrazit souřadnice. Až někdo přidá konvertory na další formáty (OLC, PlusPoint), pak se sem přidají...
            __CoordinateFormats = new MapCoordinatesFormat[]
            {
                MapCoordinatesFormat.Nephrite,
                MapCoordinatesFormat.Wgs84ArcSecSuffix,
                MapCoordinatesFormat.Wgs84ArcSecPrefix,
                MapCoordinatesFormat.Wgs84Decimal,
                MapCoordinatesFormat.Wgs84DecimalSuffix,
                MapCoordinatesFormat.Wgs84DecimalPrefix,
                MapCoordinatesFormat.OpenLocationCode
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

            bool isCopy = (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph && e.Button.Tag is String textC && String.Equals(textC, "Copy"));
            if (isCopy)
            {
                string text = (__CoordinatesText.Text ?? "").Trim();
                DxComponent.TryRun(() => System.Windows.Forms.Clipboard.SetText(text), true);
            }
            else
            {
                bool isLeft = (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinLeft || buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinDown ||
                              (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph && e.Button.Tag is String textL && String.Equals(textL, "Left")));
                bool isRight = (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinRight || buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.SpinUp ||
                               (buttonKind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph && e.Button.Tag is String textR && String.Equals(textR, "Right")));

                int shift = (isLeft ? -1 : (isRight ? +1 : 0));
                if (shift != 0)
                {
                    // Property '_CoordinateFormatCurrentIndex ' si sama hlídá validní rozsah 0 až __CoordinateFormats.Length:
                    _CoordinateFormatCurrentIndex = _CoordinateFormatCurrentIndex + shift;

                    // Zobrazíme aktuální koordináty (IMapProperties.MapCoordinates) v aktuálním (= nově určeném) formátu:
                    _ReloadCoordinatesText();

                    // Tato metoda nemění hodnotu souřadnic (mění jen její formální vyjádření), proto neprovádíme reload mapy ani refresh Accept buttonu...

                    // Zde jsme provedli změnu formátu, proto zavoláme event do aplikačního kódu:
                    this.IMapProperties.RunCoordinatesUserFormatChanged();
                }
            }
        }
        /// <summary>
        /// Metoda zajistí převzetí formátu mapové souřadnice z .
        /// Volá se po změně textu uživatelem a jeho akceptování jako souřadnice.
        /// Formát se získá z this.IMapProperties
        /// </summary>
        /// <param name="isSilent"></param>
        private void _RefreshCurrentUserFormat(bool isSilent = false)
        {
            var mapCoordinates = this.IMapProperties.MapCoordinates;
            var mapFormat = mapCoordinates.CoordinatesFormat.Value;            // Takhle je zadaná nová souřadnice
            var oldFormat = this.CoordinatesUserFormat;                        // Tohle je aktuální stav
            if (oldFormat != mapFormat)
            {
                this.CoordinatesUserFormat = mapFormat;
                if (!isSilent)
                    this.IMapProperties.RunCoordinatesUserFormatChanged();
            }
        }
        /// <summary>
        /// Obsahuje aktuálně zvolený formát koordinátů (zadaný pomocí spinnerů v <see cref="__CoordinatesText"/>). V tomto formátu je vidí uživatel.
        /// Změna této property nevyvolá událost o změně.
        /// </summary>
        protected MapCoordinatesFormat CoordinatesUserFormat
        {
            get
            {
                var formats = __CoordinateFormats;
                int count = formats?.Length ?? 0;
                if (count > 0)
                {
                    int index = _CoordinateFormatCurrentIndex;
                    if (index >= 0 && index < count)
                        return formats[index];
                }
                return MapCoordinatesFormat.Nephrite;
            }
            set
            {
                var formats = __CoordinateFormats;
                int count = formats?.Length ?? 0;
                if (count > 0 && (formats.TryFindFirstIndex(f => f == value, out int index)))
                    _CoordinateFormatCurrentIndex = index;
            }
        }
        /// <summary>
        /// Index aktuálně platného formátu koordinátů (index do pole <see cref="__CoordinateFormats"/>, kde tento prvek obsahuje hodnotu <see cref="CoordinatesUserFormat"/>)
        /// </summary>
        private int _CoordinateFormatCurrentIndex
        {
            get { return __CoordinateFormatCurrentIndex; }
            set
            {
                __CoordinateFormatCurrentIndex = CycleNumber(value, __CoordinateFormats.Length);       // Cykluje číslo (i záporné) do rozmezí { 0 až count )
            }
        }
        /// <summary>
        /// Index aktuálně platného formátu koordinátů (index do pole <see cref="__CoordinateFormats"/>, kde tento prvek obsahuje hodnotu <see cref="CoordinatesUserFormat"/>)
        /// </summary>
        private int __CoordinateFormatCurrentIndex;
        /// <summary>
        /// Uživateli dostupné formáty <see cref="MapCoordinatesFormat"/>.
        /// </summary>
        private MapCoordinatesFormat[] __CoordinateFormats;
        /// <summary>
        /// Cykluje číslo do rozmezí 0 (včetně) až count (mimo).<br/>
        /// Pro kladná čísla je shodné s operátorem Modulo: <c>result = number % count;</c><br/>
        /// Pro záporná čísla ale funguje opačně: pro hodnotu -1 vrátí (count - 1) atd.
        /// <para/>
        /// <b>Příklady:</b><br/>
        /// <c>CycleNumber(0, 5) = 0;</c><br/>
        /// <c>CycleNumber(2, 5) = 2;</c><br/>
        /// <c>CycleNumber(4, 5) = 4;</c><br/>
        /// <c>CycleNumber(5, 5) = 0;</c><br/>
        /// <c>CycleNumber(6, 5) = 1;</c><br/>
        /// <c>CycleNumber(-1, 5) = 4;</c><br/>
        /// <c>CycleNumber(-2, 5) = 3;</c><br/>
        /// <c>CycleNumber(-5, 5) = 0;</c><br/>
        /// <c>CycleNumber(-6, 5) = 4;</c><br/>
        /// </summary>
        /// <param name="number"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected static int CycleNumber(int number, int count)
        {
            int result = number % count;                   // Výsledek pro záporná čísla je záporný: (-2 % 5) = -2;   (-6 % 5) = -1; atd
            if (result < 0) result = count + result;       // Pro záporná čísla přidám count a tím cykluji v rozmezí 0 až count...
            return result;
        }
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
            __MsWebView.MsWebNavigationTotalCompleted += __MsWebNavigationTotalCompleted;
            __MsWebView.MsWebImageCaptured += _MsWebImageCaptured;
            __MsWebView.MsWebDisplayImageCaptured += _MsWebDisplayImageCaptured;
            __MsWebView.WebProperties.DelayedResponseWaitingMs = 170;
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
            var isChangeForCoordinate = __MapCoordinateToUrlAdressChangeInProgress;
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
            OnMsWebCurrentStatusTextChanged();
            MsWebCurrentStatusTextChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvoláno po události před změnou navigace.
        /// </summary>
        private void _MsWebNavigationBefore(object sender, EventArgs e)
        {
            __NavigationInProgress = true;
            OnMsWebNavigationBefore();
            MsWebNavigationBefore?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvolá události po zahájením navigace.
        /// </summary>
        private void _MsWebNavigationStarted(object sender, EventArgs e)
        {
            _ShowWebViewControl();
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
            var isChangeForCoordinate = __MapCoordinateToUrlAdressChangeInProgress;
            __MapCoordinateToUrlAdressChangeInProgress = false;
            __NavigationInProgress = false;
            OnMsWebNavigationCompleted();
            MsWebNavigationCompleted?.Invoke(this, EventArgs.Empty);

            if (!isChangeForCoordinate)
                // Změna URL adresy NEBYLA prováděna z důvodu změny koordinátů => URL adresu změnil interaktivně uživatel => měli bychom detekovat nové koordináty:
                _ConvertUrlAdressToCoordinates();
        }
        /// <summary>
        /// Po dokončení navigace = WebView změnil obsah dokumentu (webová stránka) a má hotovo, včetně donačtení Delayed WebResponses.
        /// Důvody jsou různé: programová změna URL adresy (=změna koordinátů) anebo uživatelovo klikání a myší přetahování.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __MsWebNavigationTotalCompleted(object sender, EventArgs e)
        {
            OnMsWebNavigationTotalCompleted();
            MsWebNavigationTotalCompleted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Metoda je volaná poté, kdy uživatel interaktivně změní URL adresu. Možná došlo ke změně souřadnic?
        /// </summary>
        private void _ConvertUrlAdressToCoordinates()
        {
            if (this.IsDispose) return;

            var webView = this.__MsWebView;
            string urlAdress = webView.MsWebCurrentUrlAdress;                  // URL adresa ve WebView

            var currentData = this.IMapProperties.MapCoordinates.MapData;
            if (MapCoordinates.TryParseUrlAdress(urlAdress, currentData, out var parsedData))
            {   // Uživatel změnil URL adresu na mapě, a my jsme z ní detekovali nové koordináty (souřadnice, zoom, typ mapy atd):

                // Uložíme si nově získané hodnoty (newCoordinates) do naší instance WebCoordinates, tím dojde k události WebCoordinates.CoordinatesChanged => _WebCoordinatesChanged.
                var webCoordinates = this.WebCoordinates;
                webCoordinates.FillFrom(parsedData);

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
            if (this.IsDispose) return;

            var mapProperties = this.MapProperties;
            if (!mapProperties.IsMapEditable) return;                          // Mapa není editovatelná, pak není třeba řešit Accept button a jeho stav

            bool isAcceptEnabled = false;

            var webCoordinates = this.WebCoordinates;                          // Souřadnice právě nyní zobrazená
            if (!webCoordinates.IsEmpty)
            {
                var mapCoordinates = this.IMapProperties.MapCoordinates;       // Souřadnice požadovaná z aplikace, anebo naposledy akceptovaná
                var mapPoint = mapCoordinates?.PointText;
                var webPoint = webCoordinates?.PointText;
                bool isEqual = String.Equals(mapPoint, webPoint, StringComparison.Ordinal);
                isAcceptEnabled = !isEqual;                                    // Shodné souřadnice (isEqual = true) => nepovolíme Accept button (není změna adresy)
            }

            this.__AcceptCoordinatesButton.Enabled = isAcceptEnabled;
        }
        /// <summary>
        /// Metoda akceptuje souřadnici nalezenou v mapě (<see cref="WebCoordinates"/>) a vloží ji do textboxu CoordinatesText a do <see cref="IMapProperties"/>.MapCoordinates.
        /// </summary>
        private void _AcceptWebViewCoordinate()
        {
            if (this.IsDispose) return;

            var mapProperties = this.MapProperties;
            if (!mapProperties.IsMapEditable) return;                // Mapa není editovatelná, pak není třeba řešit Accept button a jeho stav

            var webCoordinates = this.WebCoordinates;                // Souřadnice právě nyní zobrazená
            var coordinates = webCoordinates.Coordinates;

            this.IMapProperties.SetCoordinates(coordinates, true, false);     // Event do vnějšího světa, bez reloadu mapy (ta je právě zdrojem dat)
            _ReloadCoordinatesText();
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

        /// <summary>
        /// Volá se při dokončení navigace včetně DelayedRequest.
        /// </summary>
        protected virtual void OnMsWebNavigationTotalCompleted() { }
        /// <summary>
        /// Event při dokončení navigace včetně DelayedRequest.
        /// </summary>
        public event EventHandler MsWebNavigationTotalCompleted;

        /// <summary>
        /// Volá se po získání ImageCapture.
        /// </summary>
        protected virtual void OnMsWebImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání ImageCapture.
        /// </summary>
        public event MsWebImageCapturedHandler MsWebImageCaptured;
        #endregion
        #region Zachycování statického obrázku z WebView
        /// <summary>
        /// Získá obrázek aktuálního stavu WebView, a po jeho získání volá event event <see cref="MsWebImageCaptured"/>, kde bude načtený obrázek k dispozici.
        /// Jde o asynchronní metodu: řízení vrátí ihned, a až po dokončení akce vyvolá event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        public void LoadMsWebImageAsync(object requestId)
        {
            if (this.IsDispose) return;

            // Zahájí se async načítání obrázku z existujícího WebView a jeho aktuální adresy:
            this.__MsWebView.LoadMsWebImageAsync(requestId);
            // po načtení obrázku (async) bude vyvolán event __MsWebView.MsWebImageCaptured => _MsWebImageCaptured()
        }
        /// <summary>
        /// WebView dokončil získání Bitmapy = obraz webové stránky, je dodán v argumentu.
        /// Proběhne pouze po načítání pomocí <see cref="LoadMsWebImageAsync(object)"/>. 
        /// Nikoli pro načtení Image pro DisplayMode = Capture...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MsWebImageCaptured(object sender, MsWebImageCapturedArgs args)
        {
            if (this.IsDispose) return;

            // Externí událost:
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MapViewPanel.ExternalImageCaptured");
            OnMsWebImageCaptured(args);
            MsWebImageCaptured?.Invoke(this, args);
        }
        /// <summary>
        /// Reaguje na změnu <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/>.
        /// Po změně režimu na statický obrázek si vyžádá jeho získání z WebView, poté bude asynchronně obrázek zobrazen.
        /// Aktualizuje Picture z okna živého webu, podle nastavení <see cref="WebProperties"/>: <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/>
        /// Musí být voláno v GUI threadu.
        /// </summary>
        private void _DoChangeDisplayMode()
        {
            var webProperties = this.WebProperties;
            bool useStaticPicture = webProperties.UseStaticPicture;
            if (useStaticPicture)
            {   // Static => požádáme o zachycení Image a pak jej vykreslíme:
                this._CaptureDisplayImage();
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
        /// Metodu volá zdejší panel vždy, když mohlo dojít ke změně obrázku, pokud je statický.
        /// Tedy: při změně velikosti controlu, při doběhnutí navigace, při vložení hodnoty <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> = true, atd<br/>
        /// Zdejší metoda sama otestuje, zda je vhodné získat Image, a pokud ano, pak o něj požádá <see cref="__MsWebView"/>. 
        /// <para/>
        /// Jde o asynchronní operaci, zdejší metoda tedy skončí okamžitě. 
        /// Po získání obrázku (po nějaké době) bude volán eventhandler <see cref="_MsWebImageCaptured(object, MsWebImageCapturedArgs)"/>, 
        /// který detekuje interní request a vyvolá <see cref="_ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs)"/>, a neprovede externí event <see cref="MsWebImageCaptured"/>.
        /// </summary>
        private void _CaptureDisplayImage()
        {
            var webProperties = this.WebProperties;
            bool useStaticPicture = webProperties.UseStaticPicture;
            if (!useStaticPicture) return;

            string url = __MsWebView.WebProperties.UrlAdress;
            if (String.IsNullOrEmpty(url)) return;

            _ShowWebViewControl();
            this.__PictureWeb.Image = null;

            // Zahájí se Async nebo Sync načítání dat (Bitmapa) obrázku z WebView:
            this.__MsWebView.CaptureImage();
            // Po načtení obrázku bude vyvolán event __MsWebView.MsWebDisplayImageCaptured => _MsWebDisplayImageCaptured()
            //  a tam si převezmeme Image a zobrazíme:
        }
        /// <summary>
        /// WebView dokončil získání Bitmapy = obraz webové stránky, je dodán v argumentu.
        /// Používá se pro načtení Image pro DisplayMode = Capture...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MsWebDisplayImageCaptured(object sender, MsWebImageCapturedArgs args)
        {
            // Interní získání Image podle DisplayMode:
            _ReloadInternalMsWebImageCaptured(args);
        }
        /// <summary>
        /// Zajistí fyzické zobrazení controlu WebView, proto aby mohla proběhnout navigace na cílovou URL adresu.
        /// WebView musí být Visible, jinak nic nedělá. 
        /// Pokud tedy aktuální DisplayMode je nějaký Capture, pak WebView byl dosud Invisible a je třeba jej zviditelnit = právě nyní!
        /// </summary>
        private void _ShowWebViewControl()
        {
            if (!this.__MsWebView.IsControlVisible || this.__PictureWeb.Visible)
            {   // Pokud je změna viditelnosti potřebná:
                try
                {
                    this.SuspendLayout();
                    if (!this.__MsWebView.IsControlVisible) this.__MsWebView.Visible = true;       // Musí být Visible, jinak nic nenačte
                    if (this.__PictureWeb.Visible) this.__PictureWeb.Visible = false;              // Picture skryjeme, ponecháme Visible jen pro živý Web
                }
                finally
                {
                    this.ResumeLayout();
                }
            }
        }
        /// <summary>
        /// Metoda je volaná po získání dat CapturedImage (jsou dodána v argumentu) pro interní účely, na základě požadavku z metody <see cref="_CaptureDisplayImage"/>.
        /// Metoda promítne dodaná data do statického obrázku <see cref="__PictureWeb"/>, a v případě potřeby tenti obrázek zviditelní.
        /// </summary>
        /// <param name="args"></param>
        private void _ReloadInternalMsWebImageCaptured(MsWebImageCapturedArgs args)
        {
            if (!this.WebProperties.UseStaticPicture) return;

            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MapViewPanel.InternalImageCaptured");

            // Any Thread => GUI:
            if (this.InvokeRequired) this.BeginInvoke(new Action(reloadMsWebImageCaptured));
            else reloadMsWebImageCaptured();

            // Fyzické načtení Image a další akce, v GUI threadu
            void reloadMsWebImageCaptured()
            {
                try
                {
                    this.SuspendLayout();
                    this.__PictureWeb.Image = args.GetImage();
                    if (this.__PictureWeb.Visible) this.__PictureWeb.Refresh();
                    if (!this.__PictureWeb.Visible) this.__PictureWeb.Visible = true;
                    if (this.__MsWebView.Visible) this.__MsWebView.Visible = false;
                }
                catch { }
                finally
                {
                    this.ResumeLayout();
                }
            }
        }
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Value tohoto panelu = souřadnice zadaného bodu.
        /// Obsahuje totéž jako <see cref="MapPropertiesInfo.Coordinates"/>.
        /// </summary>
        public string Value { get { return this.MapProperties.Coordinates; } set { if (!this.IsDispose) this.MapProperties.Coordinates = value; } }
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení v jednom místě.
        /// Pro mapy obecně není důležitý.
        /// </summary>
        protected MsWebView.WebPropertiesInfo WebProperties { get { return __MsWebView.WebProperties; } }
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
        /// Může se lišit od dat v <see cref="MapProperties"/>, protože tam jsou souřadnice zvenku požadované a navenek akceptované.
        /// </summary>
        protected MapCoordinates WebCoordinates { get { return __WebCoordinates; } } private MapCoordinates __WebCoordinates;
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
            /// Vlastník = <see cref="DxMapViewPanel"/>.
            /// Na rozdíl od sousedních webových <see cref="MsWebView.WebPropertiesInfo"/> (ty jsou součástí web controlu <see cref="MsWebView"/>) 
            /// jsou zdejší mapové properties <see cref="MapPropertiesInfo"/> součástí finálního mapového panelu.
            /// <para/>
            /// Důvody: <br/>
            /// - Webové properties ovlivňují jak vlastní webový prohlížeč, tak i jeho použití pro mapy (obojí se prohlíží ve WebView);<br/>
            /// - Mapové properties nejsou vlastnostmi Webu (tedy <see cref="MsWebView"/>), ale mapového controlu = panelu. Jde o souřadnice a generátor URL.
            /// <para/>
            /// Z toho plyne několik rozdílů:
            /// <see cref="MsWebView.WebPropertiesInfo"/> má ownera <see cref="MsWebView"/>, ale může mít různé Parent panely; proto jeho 
            /// <see cref="DxMapViewPanel.MapPropertiesInfo"/> má Ownera = konkrétní mapový Panel <see cref="DxMapViewPanel"/>.
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
            /// Owner panel typovaný na interface <see cref="IDxWebViewParentPanel"/>, nebo null
            /// </summary>
            protected IDxWebViewParentPanel IParentPanel { get { return this.__Owner as IDxWebViewParentPanel; } }
            /// <summary>
            /// Pokud se dodaná hodnota <paramref name="value"/> liší od hodnoty v proměnné <paramref name="variable"/>, 
            /// pak do proměnné vloží hodnotu a vyvolá <see cref="_DoAction(DxWebViewActionType)"/>.
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
                IParentPanel?.DoAction(actionType);
            }
            /// <summary>
            /// Vloží dodané souřadnice, zajistí vyvolání požadovaných reakcí.
            /// </summary>
            /// <param name="coordinates">Souřadnice</param>
            /// <param name="callEventChanged">Vyvolat událost</param>
            /// <param name="callChangeMapView">Vyvolat změnu mapy</param>
            private void _SetCoordinates(string coordinates, bool callEventChanged, bool callChangeMapView)
            {
                var oldPoint = this.__MapCoordinates.PointText;
                this.__MapCoordinates.Coordinates = coordinates;
                var newPoint = this.__MapCoordinates.PointText;
                var isChanged = !String.Equals(oldPoint, newPoint, StringComparison.Ordinal);                // Máme reálnou změnu souřadnice?

                if (callEventChanged && isChanged) this._RunCoordinatesChanged();                            // Event do vnějšího světa
                if (callChangeMapView) this.__Owner.RefreshMap();                                            // Překreslení mapy
            }
            /// <summary>
            /// Nastaví výchozí hodnoty
            /// </summary>
            private void _InitValues()
            {
                __MapCoordinates = new MapCoordinates();
                __MapCoordinates.ProviderDefault = MapProviders.DefaultProvider;
                __MapCoordinates.ShowPinAtPoint = true;
                __CoordinatesFormat = MapCoordinatesFormat.Nephrite;

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
            private MapCoordinates __MapCoordinates;
            /// <summary>
            /// Vyvolá event <see cref="CoordinatesChanged"/>
            /// </summary>
            private void _RunCoordinatesChanged()
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MapPropertiesInfo.CoordinatesChanged() : {this.__MapCoordinates.CoordinatesDebug}");
                this.CoordinatesChanged?.Invoke(this, EventArgs.Empty);
            }
            /// <summary>
            /// Událost vyvolaná po změně validované pozice v mapě <see cref="Coordinates"/>.
            /// </summary>
            public event EventHandler CoordinatesChanged;
            /// <summary>
            /// Formát souřadnic, jaký je čten v property <see cref="Coordinates"/>.
            /// Setovat lze formát libovolný. Změna formátu nemá vliv na mapu (na souřadnice).
            /// Typicky jde o formát, který je čten a hlavně ukládán do Value a do databáze.
            /// <para/>
            /// Uživatel má zvolen svůj formát zobrazení souřadnice = pro svoji vizualizaci v controlu: <see cref="CoordinatesUserFormat"/>.
            /// Tuto hodnotu lze zvenku nasetovat (typicky po vytvoření controlu po načtení formátu z konfigurace).
            /// Pokud uživatel interaktivně změní svůj formát, je vyvolána událost <see cref="CoordinatesUserFormatChanged"/>.
            /// </summary>
            public MapCoordinatesFormat CoordinatesFormat { get { return __CoordinatesFormat; } set { __CoordinatesFormat = value; } } private MapCoordinatesFormat __CoordinatesFormat;
            /// <summary>
            /// Formát souřadnic, který aktuálně používá uživatel pro zobrazení souřadnic.
            /// Může se lišit od <see cref="CoordinatesFormat"/> (ten se vztahuje k formátu, který používá programový kód), ale skutečná hodnota souřadnic je vždy tatáž.
            /// <para/>
            /// Změna tohoto formátu změní vzhled, který vidí a používá uživatel. Nevyvolá ale event <see cref="CoordinatesUserFormatChanged"/>.
            /// </summary>
            public MapCoordinatesFormat CoordinatesUserFormat { get { return __Owner.CoordinatesUserFormat; } set { __Owner.CoordinatesUserFormat = value; } }
            /// <summary>
            /// Uživatel změnil formát souřadnic, který používá pro vizualizaci.
            /// Vyvolá se event do aplikace.
            /// </summary>
            private void _RunCoordinatesUserFormatChanged()
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MapPropertiesInfo.CoordinatesUserFormatChanged() : {this.CoordinatesUserFormat.ToString()}");
                this.CoordinatesUserFormatChanged?.Invoke(this, EventArgs.Empty);
            }
            /// <summary>
            /// Uživatel interaktivně změnil formát souřadnic.
            /// Pokud aplikační kód chce jeho uživatelský formát ukládat např. do konfigurace, může zde reagovat a zpracovat hodnotu <see cref="CoordinatesUserFormat"/>.
            /// </summary>
            public event EventHandler CoordinatesUserFormatChanged;
            /// <summary>
            /// Mapový provider.<br/>
            /// Změna této hodnoty nezmění interaktivně mapu (mapu změní pouze setování <see cref="Coordinates"/>). 
            /// Mapu lze refreshnout pomocí <see cref="DxMapViewPanel.RefreshMap"/> anebo <see cref="CoordinatesRefresh"/>.
            /// </summary>
            public IMapProvider CoordinatesProvider { get { return __MapCoordinates.Provider; } set { __MapCoordinates.Provider = value; } }
            /// <summary>
            /// Typ mapy (standardní, dopravní, turistická, fotoletecká).<br/>
            /// Změna této hodnoty nezmění interaktivně mapu (mapu změní pouze setování <see cref="Coordinates"/>). 
            /// Mapu lze refreshnout pomocí <see cref="DxMapViewPanel.RefreshMap"/> anebo <see cref="CoordinatesRefresh"/>.
            /// </summary>
            public MapCoordinatesMapType CoordinatesMapType { get { return __MapCoordinates.MapType.Value; } set { __MapCoordinates.MapType = value; } }
            /// <summary>
            /// Viditelnost bočního panelu s detaily.
            /// Změna této hodnoty nezmění interaktivně mapu (mapu změní pouze setování <see cref="Coordinates"/>). 
            /// Mapu lze refreshnout pomocí <see cref="DxMapViewPanel.RefreshMap"/> anebo <see cref="CoordinatesRefresh"/>.
            /// </summary>            
            public bool InfoPanelVisible { get { return __MapCoordinates.InfoPanelVisible.Value; } set { __MapCoordinates.InfoPanelVisible = value; } }
            /// <summary>
            /// Zobrazovat Pin na souřadnici mapy. Default = true.
            /// Změna této hodnoty nezmění interaktivně mapu (mapu změní pouze setování <see cref="Coordinates"/>). 
            /// Mapu lze refreshnout pomocí <see cref="DxMapViewPanel.RefreshMap"/> anebo <see cref="CoordinatesRefresh"/>.
            /// </summary>
            public bool ShowPinAtPoint { get { return __MapCoordinates.ShowPinAtPoint; } set { __MapCoordinates.ShowPinAtPoint = value;; } }
            /// <summary>
            /// Zajistí přenačtení vizuální mapy s akceptováním zadaného providera, Zoomu atd...
            /// </summary>
            public void CoordinatesRefresh() { this.__Owner.RefreshMap(); }
            /// <summary>
            /// Titulek dokumentu.
            /// </summary>
            public string DocumentTitle { get { return this.__Owner.WebProperties.DocumentTitle; } }
            /// <summary>
            /// Režim zobrazení webové stránky = práce s fyzickým prvkem WebView.<br/>
            /// Hodnota <see cref="DxWebViewDisplayMode.Live"/> = pro interaktivní práci (defaultní).
            /// Pokud je <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>, pak Parent má používat pro zobrazení obrázku Picture.<br/>
            /// Pokud je <see cref="DxWebViewDisplayMode.CaptureSync"/>, pak po setování URL adresy je vlákno pozdrženo až do dokončení načítání Picture.<br/>
            /// </summary>
            public DxWebViewDisplayMode DisplayMode { get { return this.__Owner.WebProperties.DisplayMode; } set { this.__Owner.WebProperties.DisplayMode = value; } }
            /// <summary>
            /// Doba čekání (v milisekundách) na opožděné <b><u>WebResourceResponse</u></b> = data (response), které do WebView přicházejí ze serveru i poté, kdy už proběhl event <see cref="MsWebView.MsWebNavigationCompleted"/>.
            /// Tedy: už je načten základ obsahu z požadované URL adresy, ale různé skripty ještě donačítají další data = z hlediska aktivity prohlížeče "na pozadí".
            /// <para/>
            /// Pokud je tato doba null (nebo nula nebo záporné), pak neočekáváme dodání opožděných dat, a ihned po eventu <see cref="MsWebView.MsWebNavigationCompleted"/> proběhne event <see cref="MsWebView.MsWebNavigationTotalCompleted"/>.
            /// <para/>
            /// Pokud je zde kladné číslo, pak nejprve proběhne event <see cref="MsWebView.MsWebNavigationCompleted"/> (ten je dán stejnou událostí ve WebView), a poté čekáme na příchozí data <b><u>WebResourceResponse</u></b>.
            /// Čekáme nejdéle zda zadanou dobu, a když po danou dobu žádná data nepřijdou, pak vyvoláme event <see cref="MsWebView.MsWebNavigationTotalCompleted"/>.
            /// Pokud nějaké Response přijdou, tak od jejich příchodu resetujeme čas čekání = čekáme tedy vždy <see cref="DelayedResponseWaitingMs"/> od poslední <b><u>WebResourceResponse</u></b>.
            /// <para/>
            /// Slouží to primárně k tomu, abychom provedli CaptureImage až po naplnění všech dat ze zadané URL adresy. 
            /// Některé webové adresy načtou kostru webové stránky téměř ihned a ohlásí, že mají hotovo = <see cref="MsWebView.MsWebNavigationCompleted"/>.
            /// A přitom jejich stránka vůbec není kompletní, neobsahují obrázky atd. Ty dotékají opožděně formou těchto <b><u>WebResourceResponse</u></b>.
            /// A až poté proběhne <see cref="MsWebView.MsWebNavigationTotalCompleted"/>.
            /// </summary>
            public int? DelayedResponseWaitingMs { get { return this.__Owner.WebProperties.DelayedResponseWaitingMs; } set { this.__Owner.WebProperties.DelayedResponseWaitingMs = value; } }

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
            MapCoordinates IMapPropertiesInfoWorking.MapCoordinates { get { return __MapCoordinates; } }
            /// <summary>
            /// Uživatel změnil formát souřadnic, který používá pro vizualizaci.
            /// Vyvolá se event do aplikace.
            /// </summary>
            void IMapPropertiesInfoWorking.RunCoordinatesUserFormatChanged() { _RunCoordinatesUserFormatChanged(); }
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
            MapCoordinates MapCoordinates { get; }
            /// <summary>
            /// Uživatel změnil formát souřadnic, který používá pro vizualizaci.
            /// Vyvolá se event do aplikace.
            /// </summary>
            void RunCoordinatesUserFormatChanged();
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
            __MsWebProperties = new WebPropertiesInfo(this);
            this.SizeChanged += _SizeChanged;
            _InitWebCore();
        }
        /// <summary>
        /// Po změně velikosti controlu resetuje paměť načteného Image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SizeChanged(object sender, EventArgs e)
        {
            __LastCapturedWebViewImageReset();
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
        /// Parent typovaný na interface <see cref="IDxWebViewParentPanel"/>, nebo null
        /// </summary>
        protected IDxWebViewParentPanel IParentPanel { get { return this.Parent as IDxWebViewParentPanel; } }
        /// <summary>
        /// Vyvolá v parentu akci definovanou parametrem <paramref name="actionTypes"/>
        /// </summary>
        /// <param name="actionTypes"></param>
        private void DoActionInParent(DxWebViewActionType actionTypes)
        {
            IParentPanel?.DoAction(actionTypes);
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
                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebViewCore.CoreWebView2InitializationCompleted: IS NULL; Counter: {__CoreWebInitializerCounter}!");
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
                    this.CoreWebView2.FrameNavigationCompleted += _CoreWeb_FrameNavigationCompleted;
                    this.CoreWebView2.WebResourceResponseReceived += _CoreWeb_WebResourceResponseReceived;

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
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.SourceChanged");
            _DetectChanges();
        }
        /// <summary>
        /// Eventhandler pro změnu StatusText
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_StatusTextChanged(object sender, object e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.StatusBarTextChanged");
            _DetectChanges();
        }
        /// <summary>
        /// Eventhandler po změně v okně historie navigátru. Může mít vliv na tlačítka Back/Forward.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_HistoryChanged(object sender, object e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.HistoryChanged");
            _DetectChanges();
        }
        /// <summary>
        /// Eventhandler při požadavku na otevření nového okna prohlížeče.
        /// Může reagovat na nastavení <see cref="WebPropertiesInfo.CanOpenNewWindow"/> a pokud není povoleno, pak zajistí otevření nového odkazu v aktuálním controlu namísto v novém okně.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_NewWindowRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.NewWindowRequested");
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
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.NavigationStarting");
            __MsWebIsInNavigateState = true;               // Běží navigace základní URL
            __MsWebIsInNavigateTotalState = true;          // Běží navigace pro přídavné Responses
            __MsWebWaitingForResponses = false;            // Zatím nečekáme na DelayedResponses
            WatchTimer.Remove(__DelayedResponseTimerGuid); // Vypneme budík pro DelayedResponse
            __DelayedResponseTimerGuid = null;             // Zapomeneme na budík pro DelayedResponse.
            _RunMsWebNavigationStarting();
        }
        /// <summary>
        /// Eventhandler při dokončení navigace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.NavigationCompleted");
            __MsWebIsInNavigateState = false;              // Doběhla navigace základní URL, ale přídavné Responses stále mohou běžet
            _DetectChanges();
            _RunMsWebNavigationCompleted();
            _CheckDelayedResponse();                       // Tam se ověří, zda budeme čekat na doběhnutí dalších Responses (odpovědi ze serveru, které přicházejí po NavigationCompleted)
        }
        /// <summary>
        /// Eventhandler při dokončení navigace - včetně čekání na opožděné <b><u>WebResourceResponse</u></b> .
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_NavigationTotalCompleted(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.NavigationTotalCompleted");
            __MsWebIsInNavigateTotalState = false;         // Doběhla navigace pro přídavné Responses
            __MsWebWaitingForResponses = false;            // Již nečekáme na DelayedResponses
            _DetectChanges();
            _RunMsWebNavigationTotalCompleted();
        }
        /// <summary>
        /// Po změně titulku dokumentu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_DocumentTitleChanged(object sender, object e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.DocumentTitleChanged");
            _DetectChanges();
        }
        /// <summary>
        /// Po dokončení navigace Frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _CoreWeb_FrameNavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.FrameNavigationCompleted");
        }
        /// <summary>
        /// Po donačtení dat z webu = přišla další Response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CoreWeb_WebResourceResponseReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "WebViewCore.WebResourceResponseReceived");
            _CheckDelayedResponse();                       // Tam se ověří, zda budeme čekat na doběhnutí dalších Responses (odpovědi ze serveru, které přicházejí po NavigationCompleted)
        }
        /// <summary>
        /// Metoda je volaná po dokončení základní navigace (<see cref="_CoreWeb_NavigationCompleted(object, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs)"/>) 
        /// i po příchodu WebResponse (<see cref="_CoreWeb_WebResourceResponseReceived(object, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseReceivedEventArgs)"/>).
        /// Metoda určí, zda je v parametrech zadané časové okno pro "čekání na opožděné Response" v <see cref="MsWebView.WebPropertiesInfo.DelayedResponseWaitingMs"/>
        /// </summary>
        private void _CheckDelayedResponse()
        {
            if (__MsWebIsInNavigateState) return;          // Pokud stále čekám na konec základní navigace, tak neřeším čekání na Delayed responses
            if (!__MsWebIsInNavigateTotalState) return;    // Jsem už ve stavu, kdy na Delayed responses nečekáme = už jsme čekání skončili.

            var delay = this.WebProperties.DelayedResponseWaitingMs;           // Máme čekat na Delayed responses?
            if (delay.HasValue && delay.Value > 0)
            {
                if (!__MsWebWaitingForResponses)
                    __MsWebWaitingForResponses = true;     // Nyní jsem v časovém okně, kdy očekávám DelayedResponses

                // Od teď, což je buď čas dokončení základní navigace (_CoreWeb_NavigationCompleted)
                //  anebo čas příchodu nějaké DelayedResponse (_CoreWeb_WebResourceResponseReceived) počkáme nějakou dobu (delay milisekund):
                // načasujeme "náš" budík (použijeme identifikátor __DelayedResponseTimerGuid).
                // Pokud v mezidobí přijde další DelayedResponse, pak tento budík (__DelayedResponseTimerGuid) zresetujeme na zadaný čas.
                // Pokud nic nepřijde, uplyne interval budíku a zavolá se akce (_CheckDelayedResponseTimerOut) :
                __DelayedResponseTimerGuid = WatchTimer.CallMeAfter(_CheckDelayedResponseTimerOut, delay.Value, id: __DelayedResponseTimerGuid);
            }
            else
            {   // Nemáme čekat na Delayed responses => vyvoláme NavigationTotalCompleted nyní:
                _DoNavigationTotalCompleted();
            }
        }
        /// <summary>
        /// Uplynul čas budíku <see cref="__DelayedResponseTimerGuid"/>, který čeká na opoždění Responses, a v nastaveném čase už žádná nepřišla.
        /// Vyvoláme akci <b><u>NavigationTotalCompleted</u></b>.
        /// </summary>
        private void _CheckDelayedResponseTimerOut()
        {
            _DoNavigationTotalCompleted();
        }
        /// <summary>
        /// Zajistí provedení akcí při <b><u>NavigationTotalCompleted</u></b>.
        /// </summary>
        private void _DoNavigationTotalCompleted()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(doNavigationTotalCompleted));
            else
                doNavigationTotalCompleted();

            // Až po eventu NavigationTotalCompleted:
            _RunCaptureImageOnTotalCompleted();


            // NavigationTotalCompleted - v GUI threadu:
            void doNavigationTotalCompleted()
            {
                __MsWebIsInNavigateTotalState = false;         // Total navigace je dokončena
                __MsWebWaitingForResponses = false;            // Již nečekáme na DelayedResponses
                __DelayedResponseTimerGuid = null;
                _CoreWeb_NavigationTotalCompleted(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Guid požadavku na <see cref="WatchTimer"/>, který odměřuje čas čekání na Delayed Responses
        /// </summary>
        private Guid? __DelayedResponseTimerGuid;
        /// <summary>
        /// Vynuluje všechny proměnné související s aktuální i požadovanou adresou a statustextem.
        /// Volá se před tím, než se nastaví nový cíl navigace.
        /// </summary>
        private void _RunMsWebCurrentClear()
        {
            __MsWebRequestedUrlAdress = null;
            __MsWebHtmlContent = null;
            __MsWebNeedNavigate = false;
            __MsWebIsInNavigateState = false;
            __MsWebIsInNavigateTotalState = false;
            __DelayedResponseTimerGuid = null;
            __MsWebCurrentUrlAdress = null;
            __MsWebCurrentStatusText = null;
            _RunMsWebCurrentUrlAdressChanged();
            _RunMsWebCurrentStatusTextChanged();
        }
        /// <summary>
        /// Detekuje změny aktuálního URL, titulku, status baru, možností Back/Forward; a vyvolá odpovídající reakce
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

        /// <summary>
        /// Vyvolá události při dokončení navigace včetně donačtení opožděných <b><u>WebResourceResponse</u></b> (pokud jsou očekávané dle <see cref="MsWebView.WebPropertiesInfo.DelayedResponseWaitingMs"/>).
        /// </summary>
        private void _RunMsWebNavigationTotalCompleted()
        {
            OnMsWebNavigationTotalCompleted();
            MsWebNavigationTotalCompleted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se při dokončení navigace včetně donačtení opožděných <b><u>WebResourceResponse</u></b> (pokud jsou očekávané dle <see cref="MsWebView.WebPropertiesInfo.DelayedResponseWaitingMs"/>).
        /// </summary>
        protected virtual void OnMsWebNavigationTotalCompleted() { }
        /// <summary>
        /// Event při dokončení navigace, včetně donačtení opožděných <b><u>WebResourceResponse</u></b> (pokud jsou očekávané dle <see cref="MsWebView.WebPropertiesInfo.DelayedResponseWaitingMs"/>).
        /// </summary>
        public event EventHandler MsWebNavigationTotalCompleted;

        private string __MsWebCurrentDocumentTitle;
        private string __MsWebCurrentUrlAdress;
        private string __MsWebCurrentStatusText;
        private bool __MsWebCanGoBack;
        private bool __MsWebCanGoForward;
        #endregion
        #region LoadMsWebImageAsync a GetMsWebImageSync : zachycení statického Image celého WebView
        /// <summary>
        /// Obsahuje true, pokud tento samotný Control je Visible, bez ohledu na Visible jeho Parentů.
        /// WinForm control může mít nastaveno Visible = true, ale pokud jeho Parenti nejsou Visible, pak control bude vracet Visible = false.
        /// Na rozdíl od toho bude hodnota <see cref="IsControlVisible"/> obsahovat právě to, co bylo do <see cref="Control.Visible"/> setováno.
        /// </summary>
        public bool IsControlVisible { get { return this.IsSetVisible(); } }
        /// <summary>
        /// Zajistí načtení Image pro aktuální web.<br/>
        /// Získá obrázek aktuálního stavu WebView a uloží jej do <see cref="LastCapturedWebViewImageData"/>. 
        /// Jde o asynchronní metodu: řízení vrátí ihned, a po dokončení akce vyvolá event <see cref="MsWebImageCaptured"/>.
        /// <para/>
        /// Pozor: pokud control není Visible, pak tato metoda nic neprovede a event se nevyvolá!
        /// </summary>
        /// <param name="requestId">Identifikátor požadavku</param>
        /// <param name="callback">Metoda, která bude volaná po načtení Image.
        /// null = nebude se volat metoda, ale proběhne standardní event <see cref="MsWebImageCaptured"/>.
        /// Pokud je dodán <paramref name="callback"/>, pak event <see cref="MsWebImageCaptured"/> neproběhne !!!</param>
        public void LoadMsWebImageAsync(object requestId = null, Action<MsWebImageCapturedArgs> callback = null)
        {
            if (_TryGetValidLastCapturedWebViewImage(out var lastData))
            {   // Pokud už máme zachycen Last Image, který je dosud platný, tak nemusíme nic aktuálně načítat, a naše lastData pošleme do eventů:
                callTarget(callback, requestId, lastData, false);
                return;
            }

            if (this.InvokeRequired)
                this.Invoke(new Action(loadMsWebImageAsync));
            else
                loadMsWebImageAsync();

            async void loadMsWebImageAsync()
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
                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.LoadMsWebImageAsync => CapturePreviewAsync...");

                    Task task = this.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                    // V řádku 'await' se věci rozdělí:
                    await task;
                    //  - aktuální metoda v aktuálním threadu vrátí řízení volajícímu, ten si normálně ihned pokračuje svým dalším kódem; 
                    //     a zdejší metoda zde "čeká" = očekává impuls od threadu na pozadí...
                    //  - až doběhne task, přejde tato metoda  >> v tomtéž threadu! <<  na další řádek => uloží obsah streamu do imageData a do args, a vyvolá event MsWebImageCaptured:

                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MsWebView.LoadMsWebImageAsync => Status: {task.Status}");
                    if (!task.IsCompleted) return;

                    var imageData = ms.GetBuffer();
                    callTarget(callback, requestId, imageData, true);
                }
            }

            // Zavolá patřičné cílové metody a předá jim data načteného Image. Uloží dodaná ImageData a aktuální velikost Controlu jako hodnoty Last.
            void callTarget(Action<MsWebImageCapturedArgs> callMethod, object reqId, byte[] imgData, bool setAsLast)
            {
                if (setAsLast)
                    __LastCapturedWebViewImageSet(imgData);

                MsWebImageCapturedArgs args = new MsWebImageCapturedArgs(reqId, imgData);
                if (callMethod != null)
                    callMethod(args);
                else
                    _RunMsWebImageCaptured(args);
            }
        }
        /// <summary>
        /// Získá obrázek aktuálního stavu WebView a vrátí jej, počká si na jeho výsledek.
        /// Jde o synchronní metodu: čeká a vrací výsledke, blokuje thread.
        /// <para/>
        /// Pozor: pokud control není Visible, pak tato metoda nic neprovede a event se nevyvolá!
        /// </summary>
        /// <param name="callback">Metoda, která bude volaná po načtení Image.
        /// null = nebude se volat metoda, ale výstupem metody bude Image..
        /// Pokud je dodán <paramref name="callback"/>, výstupem metody bude null !!!</param>
        public System.Drawing.Image GetMsWebImageSync(Action<MsWebImageCapturedArgs> callback = null)
        {
            System.Drawing.Image image = null;
            if (this.IsDisposed || this.Disposing) return image;

            if (_TryGetValidLastCapturedWebViewImage(out var lastData))
            {   // Pokud už máme zachycen Last Image, který je dosud platný, tak nemusíme nic aktuálně načítat, a naše lastData vrátíme jako Image:
                image = processTarget(callback, lastData, false);
                return image;
            }

            if (this.CoreWebView2 is null)
            {   // Požadavek přišel dřív, než byla dokončena inicializace Core => uložíme request do fronty:
                //   fakt fronta ???
                return image;
            }
            if (!this.IsControlVisible) return image;

            using (var ms = new System.IO.MemoryStream())
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.GetMsWebImageSync => CapturePreviewAsync...");

                Task task = this.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);

                // Tady jsem v GUI threadu, a když bych dal { task.Wait(7000); }, tak zablokuji GUI thread, a MsWebView komponenta nemá šanci nic udělat, protože ona potřebuje GUI thread na práci.
                var isNavigated = DxComponent.SleepUntil(() => task.IsCompleted, TimeSpan.FromSeconds(7d), 100);
                var taskStatus = task.Status;
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"MsWebView.LoadMsWebImageAsync => Status: {task.Status}");

                if (taskStatus != TaskStatus.RanToCompletion) return image;

                var imageData = ms.GetBuffer();
                image = processTarget(callback, imageData, false);
            }
            return image;


            // Pokud je dán callback, pak jej zavolá a předá mu dodaná data.
            // Pokud callback není dán, pak z dodaných dat vytvoří Image a ten vrátí = jde o výstup celé metody.
            // Uloží dodaná ImageData a aktuální velikost Controlu jako hodnoty Last, podle setAsLast.
            System.Drawing.Image processTarget(Action<MsWebImageCapturedArgs> callMethod, byte[] imgData, bool setAsLast)
            {
                if (setAsLast)
                    __LastCapturedWebViewImageSet(imgData);

                System.Drawing.Image img = null;
                if (callMethod != null)
                    callMethod(new MsWebImageCapturedArgs(null, imgData));
                else
                    img = MsWebImageCapturedArgs.CreateImage(imgData);

                return img;
            }
        }
        /// <summary>
        /// Vyvolá událost <see cref="MsWebImageCaptured"/>.
        /// </summary>
        /// <param name="args"></param>
        private void _RunMsWebImageCaptured(MsWebImageCapturedArgs args)
        {
            OnMsWebImageCaptured(args);
            MsWebImageCaptured?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se po získání Image v asynchronní metodě <see cref="LoadMsWebImageAsync(object, Action{MsWebImageCapturedArgs})"/>.
        /// Tento event se nevolá při automatickém načtení Image pro DisplayMode = nějaký Capture... (<see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> : <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>), 
        /// tam se volá event <see cref="MsWebDisplayImageCaptured"/>.
        /// </summary>
        protected virtual void OnMsWebImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání Image v asynchronní metodě <see cref="LoadMsWebImageAsync(object, Action{MsWebImageCapturedArgs})"/>.
        /// Tento event se nevolá při automatickém načtení Image pro DisplayMode = nějaký Capture... (<see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> : <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>), 
        /// tam se volá event <see cref="MsWebDisplayImageCaptured"/>.
        /// </summary>
        public event MsWebImageCapturedHandler MsWebImageCaptured;

        /// <summary>
        /// Vyvolá událost <see cref="MsWebDisplayImageCaptured"/>.
        /// Volá se po automatickém získání Image po načtení obsahu v režimu DisplayMode = nějaký Capture... (<see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> : <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>).
        /// </summary>
        /// <param name="args"></param>
        private void _RunDisplayImageCaptured(MsWebImageCapturedArgs args)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.MsWebDisplayImageCaptured");

            OnMsWebDisplayImageCaptured(args);
            MsWebDisplayImageCaptured?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se po automatickém získání Image po načtení obsahu v režimu DisplayMode = nějaký Capture... (<see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> : <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>).
        /// </summary>
        protected virtual void OnMsWebDisplayImageCaptured(MsWebImageCapturedArgs args) { }
        /// <summary>
        /// Event po získání Image po načtení obsahu v režimu DisplayMode = Capture... (<see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> : <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>).
        /// </summary>
        public event MsWebImageCapturedHandler MsWebDisplayImageCaptured;
        #endregion
        #region DisplayMode: Automatické zachycení Image (podle DisplayMode): CaptureAsync a CaptureSync - blokování Sync threadu, odchycení OnTotalCompleted, volání eventu _RunDisplayImageCaptured
        /// <summary>
        /// Pokud this control je použit v režimu <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> == <see cref="DxWebViewDisplayMode.CaptureSync"/>,
        /// tak v této metodě počká, až doběhne proces <b><u>NavigationTotal</u></b> = úplné dokončení navigace a následné [a]synchronní získání Image.<br/>
        /// Získaný Image je vždy předán do eventu <see cref="MsWebDisplayImageCaptured"/>.<br/>
        /// Pak teprve vrátí řízení.
        /// <para/>
        /// Pokud je jiný režim zobrazování než Capture*, pak vrátí řízení ihned.
        /// </summary>
        public void CaptureImage()
        {
            var displayMode = this.WebProperties.DisplayMode;
            bool isCaptureMode = (displayMode == DxWebViewDisplayMode.CaptureAsync || displayMode == DxWebViewDisplayMode.CaptureSync);
            if (!isCaptureMode) return;

            if (this.InvokeRequired)
                this.Invoke(new Action(startCaptureImage));
            else
                startCaptureImage();

            void startCaptureImage()
            {
                switch (displayMode)
                {   // Pokud jsem v režimu Capture... :
                    case DxWebViewDisplayMode.CaptureAsync:
                        // Asynchronní režim: pokud nyní běží navigování na cíl (NavigateTotalState je true), pak nebudu dělat nic:
                        //  protože po dokončení stahování se předává řízení do _RunCaptureImageOnTotalCompleted(), tam se detekuje režim CaptureAsync,
                        //  a v něm se volá this.LoadMsWebImageAsync(null, this._RunDisplayImageCaptured); => asynchronní získání Image a jeho předání do cílové metody.
                        // Pokud ale navigování již neběží, pak asynchronní získání Image musím nastartovat nyní, tam se vyvolá i event _RunDisplayImageCaptured:
                        if (!__MsWebIsInNavigateTotalState)
                            this.LoadMsWebImageAsync(null, this._RunDisplayImageCaptured);
                        break;
                    case DxWebViewDisplayMode.CaptureSync:
                        // Synchronní režim: vše řeší metoda _StartCapturedImageWaitSync():
                        //  pokud aktuálně probíhá navigace NavigationTotalCompleted, pak synchronně počká na její dokončení;
                        //  po konci této navigace vyvolá synchronní získání Image pomocí this.GetMsWebImageSync(this._RunDisplayImageCaptured);
                        //  a tedy vyvolá event _RunDisplayImageCaptured
                        _StartCapturedImageWaitSync();
                        break;
                }
            }
        }
        /// <summary>
        /// Pokud this control je použit v režimu <see cref="MsWebView.WebPropertiesInfo.DisplayMode"/> == <see cref="DxWebViewDisplayMode.CaptureSync"/>,
        /// tak v této metodě počká, až doběhne proces <b><u>NavigationTotal</u></b> = úplné dokončení navigace a následné synchronní získání Image.
        /// Pak teprve vrátí řízení.<br/>
        /// Pokud je jiný režim zobrazování, pak vrátí řízení ihned.
        /// </summary>
        private void _CaptureImageOnNavigate()
        {
            switch (this.WebProperties.DisplayMode)
            {   // Pokud jsem v režimu Capture... :
                case DxWebViewDisplayMode.CaptureAsync:
                    // Asynchronní => aktuální thread nebudeme blokovat, ale v události _RunCaptureImageOnTotalCompleted() nastartujeme asynchronní získání Image.
                    break;
                case DxWebViewDisplayMode.CaptureSync:
                    // Synchronní => aktuální thread blokujeme, čekáme na NavigationTotalCompleted a následně i GetImageSync():
                    _StartCapturedImageWaitSync();
                    break;
            }
        }
        /// <summary>
        /// V této metodě počká volající (GUI) thread na Total dokončení navigace i na následné synchronní získání Image.
        /// </summary>
        private void _StartCapturedImageWaitSync(double timeOutSeconds = 7)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.CaptureSync => Waiting...");

            // Slušně počkáme na stav { __MsWebIsInNavigateTotalState == false }, máme daný Timeout:
            if (__MsWebIsInNavigateTotalState)
            {
                var isNavigated = DxComponent.SleepUntil(() => !__MsWebIsInNavigateTotalState, TimeSpan.FromSeconds(timeOutSeconds), 100);
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, (isNavigated ? "MsWebView.CaptureSync => TotalCompleted." : "MsWebView.CaptureSync => Timeout expired."));
            }
            else
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.CaptureSync => Navigation is completed.");
            }

            // Získáme synchronní Image = aktuální vlákno si v metodě GetMsWebImageSync() počká na načtení Image (pomocí Task.Wait):
            // A odešleme event do Parenta, že máme CapturedImage = synchronní:
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.CaptureSync => GetMsWebImageSync...");
            this.GetMsWebImageSync(this._RunDisplayImageCaptured);
            
            // A teprve nyní pustíme aktuální Thread dále:
        }
        /// <summary>
        /// Podle režimu DisplayMode zajistí zachycení Image pro režimy <see cref="DxWebViewDisplayMode.CaptureAsync"/> a <see cref="DxWebViewDisplayMode.CaptureSync"/>:<br/>
        /// Pro <see cref="DxWebViewDisplayMode.CaptureAsync"/>: nastartuje asynchronní načtení Image a návazné vyvolání události ...;<br/>
        /// Pro <see cref="DxWebViewDisplayMode.CaptureSync"/>: ukončí čekání synchronního vlákna v metodě <see cref="_StartCapturedImageWaitSync"/>;<br/>
        /// </summary>
        private void _RunCaptureImageOnTotalCompleted()
        {
            switch (this.WebProperties.DisplayMode)
            {   // Pokud jsem v režimu Capture... :
                case DxWebViewDisplayMode.CaptureAsync:
                    // Asynchronní capture: zdejší vlákno nebudeme blokovat, vyžádáme načtení Image pomocí asynchronní metody (tam se použije async Task).
                    // Předáme callback = , ten bude vyvolán po dokončení načítání Image, a ten vyvolá událost ...
                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "MsWebView.LoadImage for CaptureAsync");
                    this.LoadMsWebImageAsync(null, this._RunDisplayImageCaptured);
                    break;
                case DxWebViewDisplayMode.CaptureSync:
                    // Synchronní capture: volající vlákno čeká v metodě _StartCapturedImageWaitSync() (s nějakým TimeOutem), 
                    //  má tam probouzecí kontrolní cykly 100ms (v metodě DxComponent.SleepUntil()), a čeká až bude __MsWebIsInNavigateTotalState = false.
                    // Zajistíme, že __MsWebIsInNavigateTotalState bude false:
                    if (__MsWebIsInNavigateTotalState)
                        __MsWebIsInNavigateTotalState = false;
                    break;
            }
        }
        /// <summary>
        /// Resetuje paměť posledně načteného Image v <see cref="LastCapturedWebViewImageData"/> a tedy i v <see cref="__LastCapturedWebViewImageSize"/>.
        /// </summary>
        private void __LastCapturedWebViewImageReset()
        {
            __LastCapturedWebViewImageSize = null;
            __LastCapturedWebViewImageData = null;
        }
        /// <summary>
        /// Do paměti posledně načteného Image v <see cref="LastCapturedWebViewImageData"/> a tedy i v <see cref="__LastCapturedWebViewImageSize"/> uloží dodaná data a aktuální velikost Controlu.
        /// </summary>
        private void __LastCapturedWebViewImageSet(byte[] imageData)
        {
            __LastCapturedWebViewImageSize = this.Size;
            __LastCapturedWebViewImageData = imageData;
        }
        /// <summary>
        /// Velikost obrázku <see cref="__LastCapturedWebViewImageData"/>
        /// </summary>
        private System.Drawing.Size? __LastCapturedWebViewImageSize;
        /// <summary>
        /// Obsah obrázku posledně zachyceného metodou <see cref="LoadMsWebImageAsync"/>
        /// </summary>
        public byte[] LastCapturedWebViewImageData { get { return (_TryGetValidLastCapturedWebViewImage(out var imageData) ? imageData : null); } } private byte[] __LastCapturedWebViewImageData;
        /// <summary>
        /// Image vygenerovaný z dat <see cref="LastCapturedWebViewImageData"/>
        /// </summary>
        public System.Drawing.Image LastCapturedWebViewImage { get { return (_TryGetValidLastCapturedWebViewImage(out var imageData) ? MsWebImageCapturedArgs.CreateImage(imageData) : null); } }
        /// <summary>
        /// Metoda zkusí získat data validního Image z <see cref="LastCapturedWebViewImageData"/>.
        /// Tedy: pokud jsou nějaká data uložena, a jsou uložena pro aktuální velikost controlu.
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        private bool _TryGetValidLastCapturedWebViewImage(out byte[] imageData)
        {
            imageData = null;
            var lastSize = __LastCapturedWebViewImageSize;
            var lastData = __LastCapturedWebViewImageData;
            if (!lastSize.HasValue || lastData is null) return false;
            var currSize = this.Size;
            if (currSize != lastSize.Value) return false;
            imageData = lastData;
            return true;
        }
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
        /// Režim zobrazení webové stránky = práce s fyzickým prvkem WebView.<br/>
        /// Hodnota <see cref="DxWebViewDisplayMode.Live"/> = pro interaktivní práci (defaultní).
        /// Pokud je <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>, pak Parent má používat pro zobrazení obrázku Picture.<br/>
        /// Pokud je <see cref="DxWebViewDisplayMode.CaptureSync"/>, pak po setování URL adresy je vlákno pozdrženo až do dokončení načítání Picture.<br/>
        /// </summary>
        public DxWebViewDisplayMode MsWebDisplayMode { get { return this.__MsWebProperties.DisplayMode; } set { this.__MsWebProperties.DisplayMode = value; } }
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
                    __MsWebIsInNavigateState = true;                 // Očekáváme konec standardní navigace
                    __MsWebIsInNavigateTotalState = true;            // Očekáváme konec přidaných WebResponses
                    __LastCapturedWebViewImageReset();

                    if (!String.IsNullOrEmpty(__MsWebRequestedUrlAdress))
                        this.UrlSource = __MsWebRequestedUrlAdress;
                    else if (!String.IsNullOrEmpty(__MsWebHtmlContent))
                        this.NavigateToString(__MsWebHtmlContent);
                    
                    _RunMsWebNavigationStarted();                    // Událost po startu aktivní změny URL
                    _CaptureImageOnNavigate();                       // Zajistí zachycení Image pro DisplayMode: CaptureSync i CaptureAsync (pro CaptureSync pozastaví aktuální Thread => až do doběhnutí navigace, které běží OnBackground)
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
        /// <summary>
        /// Má se provést navigace?
        /// <para/>
        /// Je nastaveno na true při setování neprázdné URL adresy do <see cref="MsWebRequestedUrlAdress"/> nebo <see cref="MsWebHtmlContent"/>.
        /// Značí, že je potřeba provést navigaci na daný obsah.
        /// Je nastaveno na false při Clear: <see cref="_RunMsWebCurrentClear"/>, a těsně před zahájením navigace na cílový obsah v <see cref="_DoNavigate"/>.
        /// Pokud je false před započetím <see cref="_DoNavigate"/>, pak se nikam nenaviguje, protože navigace už běží.
        /// </summary>
        private bool __MsWebNeedNavigate;
        /// <summary>
        /// Právě probíhá stahování <b><u>základního obsahu</u></b> zadané URL.<br/>
        /// Po jeho stažení proběhne event <see cref="_CoreWeb_NavigationCompleted(object, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs)"/>.
        /// <para/>
        /// Je nastaveno na true při začátku navigování na obsah, v eventu <see cref="_CoreWeb_NavigationStarting(object, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs)"/>
        /// a při explicitním zahájení navigace v metodě <see cref="_DoNavigate"/>.
        /// Hodnota true značí, že aktuálně běží navigování.
        /// Na false je přepnuto v eventu <see cref="_CoreWeb_NavigationCompleted(object, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs)"/> = navigace na URL je dokončena.
        /// Nicméně i poté mohou do WebView dotékat další data, to hlídá <see cref="__MsWebIsInNavigateTotalState"/>.
        /// </summary>
        private volatile bool __MsWebIsInNavigateState;
        /// <summary>
        /// Právě probíhá stahování základního obsahu zadané URL plus <b><u>očekáváme příchod dalších Responses</u></b>.
        /// Po jeho dokončení proběhne event <see cref="_CoreWeb_NavigationTotalCompleted(object, EventArgs)"/>.
        /// <para/>
        /// Je nastaveno na true při začátku navigování na obsah, v eventu <see cref="_CoreWeb_NavigationStarting(object, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs)"/>
        /// a při explicitním zahájení navigace v metodě <see cref="_DoNavigate"/>. Stejně jako vkládání true do <see cref="__MsWebIsInNavigateState"/>.
        /// Hodnota true značí, že aktuálně běží navigování a možná i doplňování dat po dokončení navigace.
        /// Tato hodnota se nepřepíná na false v eventu <see cref="_CoreWeb_NavigationCompleted(object, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs)"/>, kdy je oficiálně dokončena samotná navigace.
        /// Od té chvíle (tj. od dokončení navigace) zde běží časové okno <see cref="__MsWebWaitingForResponses"/>, 
        /// kdy každá příchozí Response v eventu <see cref="_CoreWeb_WebResourceResponseReceived(object, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseReceivedEventArgs)"/>
        /// má význam "Ještě mi přicházejí další data do stránky, a její obraz tak není kompletní".
        /// Více u proměnné <see cref="__MsWebWaitingForResponses"/>.
        /// </summary>
        private volatile bool __MsWebIsInNavigateTotalState;
        /// <summary>
        /// Již jsme stáhli základní obsah zadané URL, a nyní očekáváme dokončení stahování <b><u>dalších DelayedResponses</u></b>.
        /// </summary>
        private volatile bool __MsWebWaitingForResponses;
        #endregion
        #region class PropertiesInfo : Souhrn vlastností, tak aby byly k dosažení pod jedním místem
        /// <summary>
        /// Souhrn vlastností <see cref="MsWebView"/>, tak aby byly k dosažení v jednom místě.
        /// </summary>
        public WebPropertiesInfo WebProperties { get { return __MsWebProperties; } } private WebPropertiesInfo __MsWebProperties;
        /// <summary>
        /// Definice vlastností <see cref="MsWebView"/>
        /// </summary>
        public class WebPropertiesInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            public WebPropertiesInfo(MsWebView owner)
            {
                __Owner = owner;
                _InitValues();
            }
            /// <summary>
            /// Vlastník = <see cref="MsWebView"/>.
            /// Na rozdíl od sousedních mapových <see cref="DxMapViewPanel.MapPropertiesInfo"/> (ty jsou součástí panelu <see cref="DxMapViewPanel"/>) 
            /// jsou zdejší webové properties <see cref="WebPropertiesInfo"/> součástí vlastní komponenty WebView.
            /// <para/>
            /// Důvody: <br/>
            /// - Webové properties ovlivňují jak vlastní webový prohlížeč, tak i jeho použití pro mapy (obojí se prohlíží ve WebView);<br/>
            /// - Mapové properties nejsou vlastnostmi Webu (tedy <see cref="MsWebView"/>), ale mapového controlu = panelu. Jde o souřadnice a generátor URL.
            /// <para/>
            /// Z toho plyne několik rozdílů:
            /// <see cref="MsWebView.WebPropertiesInfo"/> má ownera <see cref="MsWebView"/>, ale může mít různé Parent panely;
            /// <see cref="DxMapViewPanel.MapPropertiesInfo"/> má Ownera = konkrétní mapový Panel <see cref="DxMapViewPanel"/>.
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
                __DisplayMode = DxWebViewDisplayMode.Live;
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
            /// Aktuální <see cref="DisplayMode"/> má hodnotu odpovídající statickému obrázku (<see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>).
            /// V takovém režimu se řeší automatické získání Picture a jeho promítnutí namísto WebView.
            /// <para/>
            /// Tato hodnota není použitelná pro samotný webový control <see cref="MsWebView"/> (ten vždy reprezentuje živý web), ale pouze pro komplexní panel <see cref="DxWebViewPanel"/> 
            /// - ten může zajišťovat získání Image a jeho zobrazení v Panelu namísto živého WebView.
            /// </summary>
            public bool UseStaticPicture { get { var mode = this.DisplayMode; return (mode == DxWebViewDisplayMode.CaptureAsync || mode == DxWebViewDisplayMode.CaptureSync); } }
            /// <summary>
            /// Režim zobrazení webové stránky = práce s fyzickým prvkem WebView.<br/>
            /// Hodnota <see cref="DxWebViewDisplayMode.Live"/> = pro interaktivní práci (defaultní).
            /// Pokud je <see cref="DxWebViewDisplayMode.CaptureAsync"/> nebo <see cref="DxWebViewDisplayMode.CaptureSync"/>, pak Parent má používat pro zobrazení obrázku Picture.<br/>
            /// Pokud je <see cref="DxWebViewDisplayMode.CaptureSync"/>, pak po setování URL adresy je vlákno pozdrženo až do dokončení načítání Picture.<br/>
            /// <para/>
            /// Tato hodnota není použitelná pro samotný webový control <see cref="MsWebView"/> (ten vždy reprezentuje živý web), ale pouze pro komplexní panel <see cref="DxWebViewPanel"/> 
            /// - ten může zajišťovat získání Image a jeho zobrazení v Panelu namísto živého WebView.
            /// </summary>
            public DxWebViewDisplayMode DisplayMode { get { return __DisplayMode; } set { _SetValueDoAction(value, ref __DisplayMode, DxWebViewActionType.DoChangeDisplayMode, true); } } private DxWebViewDisplayMode __DisplayMode;
            /// <summary>
            /// Doba čekání (v milisekundách) na opožděné <b><u>WebResourceResponse</u></b> = data (response), které do WebView přicházejí ze serveru i poté, kdy už proběhl event <see cref="MsWebView.MsWebNavigationCompleted"/>.
            /// Tedy: už je načten základ obsahu z požadované URL adresy, ale různé skripty ještě donačítají další data = z hlediska aktivity prohlížeče "na pozadí".
            /// <para/>
            /// Pokud je tato doba null (nebo nula nebo záporné), pak neočekáváme dodání opožděných dat, a ihned po eventu <see cref="MsWebView.MsWebNavigationCompleted"/> proběhne event <see cref="MsWebView.MsWebNavigationTotalCompleted"/>.
            /// <para/>
            /// Pokud je zde kladné číslo, pak nejprve proběhne event <see cref="MsWebView.MsWebNavigationCompleted"/> (ten je dán stejnou událostí ve WebView), a poté čekáme na příchozí data <b><u>WebResourceResponse</u></b>.
            /// Čekáme nejdéle zda zadanou dobu, a když po danou dobu žádná data nepřijdou, pak vyvoláme event <see cref="MsWebView.MsWebNavigationTotalCompleted"/>.
            /// Pokud nějaké Response přijdou, tak od jejich příchodu resetujeme čas čekání = čekáme tedy vždy <see cref="DelayedResponseWaitingMs"/> od poslední <b><u>WebResourceResponse</u></b>.
            /// <para/>
            /// Slouží to primárně k tomu, abychom provedli CaptureImage až po naplnění všech dat ze zadané URL adresy. 
            /// Některé webové adresy načtou kostru webové stránky téměř ihned a ohlásí, že mají hotovo = <see cref="MsWebView.MsWebNavigationCompleted"/>.
            /// A přitom jejich stránka vůbec není kompletní, neobsahují obrázky atd. Ty dotékají opožděně formou těchto <b><u>WebResourceResponse</u></b>.
            /// A až poté proběhne <see cref="MsWebView.MsWebNavigationTotalCompleted"/>.
            /// </summary>
            public int? DelayedResponseWaitingMs { get { return __DelayedResponseWaitingMs; } set { _SetValueDoAction(value, ref __DelayedResponseWaitingMs, DxWebViewActionType.None); } } private int? __DelayedResponseWaitingMs;

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
    #region Enumy, servisní třídy...
    /// <summary>
    /// Rozhraní pro parenty prvku <see cref="MsWebView"/>, 
    /// </summary>
    public interface IDxWebViewParentPanel
    {
        /// <summary>
        /// Parent panel provede požadovanou akci, kterou vyvolává WebView komponenta nebo změna hodnoty v Properties
        /// </summary>
        /// <param name="actionTypes"></param>
        void DoAction(DxWebViewActionType actionTypes);
    }
    /// <summary>
    /// Režim zobrazení webové stránky.
    /// 
    /// </summary>
    public enum DxWebViewDisplayMode
    {
        /// <summary>
        /// Neurčeno / nezobrazeno
        /// </summary>
        None,
        /// <summary>
        /// Standardní živý control.
        /// <para/>
        /// Nelze použít tam, kde se z controlu pořídí Bitmapa a ta se poté promítá (Infragistic i DX DataForm).
        /// Protože WebView se nevykreslí do Bitmapy standardním WinForm procesem.<br/>
        /// Tam je třeba použít pravděpodobně <see cref="CaptureSync"/>.
        /// </summary>
        Live,
        /// <summary>
        /// Po změně hodnoty / URL adresy se má zachytit Bitmapa z WebView (specializovaný proces) a ta se následně použije do Parent controlu.<br/>
        /// Asynchronní proces: po získání WebView bitmapy je volán asynchronní event, který bitmapu převezme a zobrazí.
        /// </summary>
        CaptureAsync,
        /// <summary>
        /// Po změně hodnoty / URL adresy se má zachytit Bitmapa z WebView (specializovaný proces) a ta se následně použije do Parent controlu.<br/>
        /// Synchronní proces: po změně hodnot (Value / URL) volající thread je pozastaven až do získání bitmapy.
        /// </summary>
        CaptureSync
    }
    /// <summary>
    /// Typy akcí, které má provést Parent panel
    /// </summary>
    [Flags]
    public enum DxWebViewActionType
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
        DoChangeDisplayMode = 0x0010,
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
        /// <summary>
        /// Vytvoří a vrátí obrázek z <see cref="ImageData"/>.
        /// Vždy je vytvořen new objekt.
        /// Chyba je ututlána a v tom případě je vrácen null.
        /// </summary>
        public System.Drawing.Image GetImage()
        {
            return CreateImage(ImageData);
        }
        /// <summary>
        /// Vytvoří a vrátí obrázek z <paramref name="imageData"/>.
        /// Vždy je vytvořen new objekt.
        /// Chyba je ututlána a v tom případě je vrácen null.
        /// </summary>
        /// <param name="imageData">Data obrázku</param>
        /// <returns></returns>
        public static System.Drawing.Image CreateImage(byte[] imageData)
        {
            if (imageData is null || imageData.Length < 12) return null;
            try
            {
                using (var ms = new System.IO.MemoryStream(imageData))
                    return System.Drawing.Image.FromStream(ms);
            }
            catch
            {
                return null;
            }
        }
    }
    /// <summary>
    /// Předpis pro handler události CaptureImage
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void MsWebImageCapturedHandler(object sender, MsWebImageCapturedArgs args);
    #endregion
}

namespace Noris.Clients.Win.Components.AsolDX.Map
{
    #region class MapCoordinates : správce souřadnic, kodér + dekodér stringu souřadnic i URL odkazu na mapy různých providerů
    /// <summary>
    /// <see cref="MapCoordinates"/> : správce souřadnic, kodér + dekodér stringu souřadnic i URL odkazu na mapy různých providerů
    /// </summary>
    public class MapCoordinates
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MapCoordinates()
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
            this._Point = new PointD(15.7435513d, 49.8152928d);      // Zkus tohle:  https://mapy.cz/turisticka?l=0&x=15.7435513&y=49.8152928&z=8
            this._Zoom = null;                                       // Převezme default, což je Town = cca 10 km
            this._Center = null;

            if (source == ResetSource.Instance)
            {   // Tvorba instance:
                this.CoordinatesFormatDefault = MapCoordinatesFormat.Wgs84DecimalSuffix;
                this.ZoomDefault = _ZoomTown;              // Zobrazuje cca 10 km na šířku běžného okna
                this._Zoom = _ZoomFull;                    // Zobrazuje cca celou republiku
                this.ProviderDefault = MapProviders.CurrentProvider;
                this.MapTypeDefault = MapCoordinatesMapType.Standard;
                this.InfoPanelVisibleDefault = false;
                this.ShowPinAtPoint = true;

                this.CoordinatesFormat = null;
                this.Provider = null;
                this.MapType = null;
                this.InfoPanelVisible = null;
            }

            if (source == ResetSource.Coordinate)
            {   // Setování koordinátů nemění defaulty, nemění providera ani typ mapy:
                this.CoordinatesFormat = null;
                this._Zoom = null;                         // Převezme default, což je Town = cca 10 km
            }

            if (source == ResetSource.UrlAdress)
            {   // Setování URL adresy nemění formát koordinátů, ale resetuje providera i typ mapy:
                this.Provider = null;
                this.MapType = null;
                this.InfoPanelVisible = null;
            }

            this.__IsEmpty = true;
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
            return _GetCoordinates(MapCoordinatesFormat.Wgs84ArcSecPrefix);
        }
        /// <summary>
        /// Oddělovač desetinných míst v aktuální kultuře, pracuje s ním <see cref="Decimal.TryParse(string, out decimal)"/>
        /// </summary>
        private static string _DotChar { get { return System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator; } }
        /// <summary>
        /// Zoom pro mapu na celou ČR
        /// </summary>
        private const int _ZoomFull = 8;
        /// <summary>
        /// Zoom pro mapu na jedno město
        /// </summary>
        private const int _ZoomTown = 14;
        #endregion
        #region Public proměnné základní = jednotlivé : Souřadnice {X:Y}: Bod souřadnice, Zoom, Střed mapy, Provider, Typ mapy, Defaulty
        /// <summary>
        /// Formát koordinátů (ve formě jednoho stringu) v <see cref="Coordinates"/>.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="CoordinatesFormatDefault"/>.
        /// </summary>
        public MapCoordinatesFormat? CoordinatesFormat { get { return __CoordinatesFormat ?? __CoordinatesFormatDefault; } set { __CoordinatesFormat = value; } }
        private MapCoordinatesFormat? __CoordinatesFormat;

        /// <summary>
        /// Exaktní cílový bod souřadnic, v patřičném Geo rozsahu.
        /// Změna hodnoty <b>vyvolá</b> event o změně!
        /// </summary>
        public PointD Point { get { return _Point; } set { var coordinatesOld = _CoordinatesSerial; _Point = value; _CheckCoordinatesChanged(coordinatesOld); } }
        /// <summary>
        /// Exaktní cílový bod souřadnic, v patřičném Geo rozsahu.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        protected PointD _Point { get { return __Point; } set { __Point = MapProviderBase.AlignGeo(value); __IsEmpty = false; } }
        private PointD __Point;

        /// <summary>
        /// Exaktní bod středu mapy, v patřičném Geo rozsahu.
        /// Změna hodnoty <b>vyvolá</b> event o změně!
        /// </summary>
        public PointD? Center { get { return _Center; } set { var coordinatesOld = _CoordinatesSerial; _Center = value; _CheckCoordinatesChanged(coordinatesOld); } }
        /// <summary>
        /// Exaktní bod středu mapy, v patřičném Geo rozsahu.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        protected PointD? _Center { get { return __Center; } set { __Center = MapProviderBase.AlignGeoN(value); __IsEmpty = false; } }
        private PointD? __Center;

        /// <summary>
        /// Zoom, v rozsahu 1 (celá planeta) až 20 (jeden pokojíček). Zoom roste exponenciálně, rozdíl 1 číslo je 2-násobek.
        /// Změna hodnoty <b>vyvolá</b> event o změně!
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="ZoomDefault"/>.
        /// </summary>
        public int? Zoom { get { return _Zoom; } set { var coordinatesOld = _CoordinatesSerial; _Zoom = value; _CheckCoordinatesChanged(coordinatesOld); } }
        /// <summary>
        /// Zoom, v rozsahu 1 (celá planeta) až 20 (jeden pokojíček). Zoom roste exponenciálně, rozdíl 1 číslo je 2-násobek.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="ZoomDefault"/>.
        /// </summary>
        protected int? _Zoom { get { return __Zoom ?? __ZoomDefault; } set { __Zoom = _AlignN(value, 1, 20); } }
        private int? __Zoom;

        /// <summary>
        /// Je definován i střed mapy?
        /// </summary>
        public bool HasCenter { get { return (this.__Center.HasValue); } }

        /// <summary>
        /// Obsahuje true po resetu.
        /// Po vložení nějaké validní hodnoty je false.
        /// </summary>
        public bool IsEmpty { get { return this.__IsEmpty; } }
        private bool __IsEmpty;

        /// <summary>
        /// Provider mapy (webová stránka).
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="ProviderDefault"/>.
        /// </summary>
        public IMapProvider Provider { get { return __Provider ?? __ProviderDefault ?? MapProviders.CurrentProvider; } set { __Provider = value; } }
        private IMapProvider __Provider;
        /// <summary>
        /// Typ mapy (standardní, dopravní, turistická, fotoletecká).
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="MapTypeDefault"/>.
        /// </summary>
        public MapCoordinatesMapType? MapType { get { return __MapType ?? __MapTypeDefault; } set { __MapType = value; } }
        private MapCoordinatesMapType? __MapType;
        /// <summary>
        /// Viditelnost bočního panelu s detaily.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// Lze setovat null, ale při čtení se namísto null čte <see cref="InfoPanelVisibleDefault"/>.
        /// </summary>
        public bool? InfoPanelVisible { get { return __InfoPanelVisible ?? __InfoPanelVisibleDefault; } set { __InfoPanelVisible = value; } }
        private bool? __InfoPanelVisible;

        /// <summary>
        /// Defaultní formát souřadnic.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        public MapCoordinatesFormat CoordinatesFormatDefault { get { return __CoordinatesFormatDefault; } set { __CoordinatesFormatDefault = value; } }
        private MapCoordinatesFormat __CoordinatesFormatDefault;
        /// <summary>
        /// Defaultní Zoom.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        public int ZoomDefault { get { return __ZoomDefault; } set { __ZoomDefault = _Align(value, 1, 20); } }
        private int __ZoomDefault;
        /// <summary>
        /// Defaultní provider.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        public IMapProvider ProviderDefault { get { return __ProviderDefault; } set { __ProviderDefault = value; } }
        private IMapProvider __ProviderDefault;
        /// <summary>
        /// Defaultní typ mapy.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        public MapCoordinatesMapType MapTypeDefault { get { return __MapTypeDefault; } set { __MapTypeDefault = value; } }
        private MapCoordinatesMapType __MapTypeDefault;
        /// <summary>
        /// Defaultní viditelnost bočního panelu s detaily.
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// </summary>
        public bool InfoPanelVisibleDefault { get { return __InfoPanelVisibleDefault; } set { __InfoPanelVisibleDefault = value; } }
        private bool __InfoPanelVisibleDefault;
        /// <summary>
        /// Vložit špendlík na pozici Point (do URL adresy).
        /// Změna hodnoty <b>nevyvolá</b> event o změně!
        /// Výchozí je true, odpovídá to chování, kdy máme dán jednoduchý koordinát a chceme jej vidět jako exaktní špendlík, nejen jako mapu v jeho okolí.
        /// </summary>
        public bool ShowPinAtPoint { get { return __ShowPinAtPoint; } set { __ShowPinAtPoint = value; } }
        private bool __ShowPinAtPoint;
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
        public string CoordinatesNephrite { get { return _GetCoordinates(MapCoordinatesFormat.Nephrite); } set { _SetCoordinates(value); } }
        /// <summary>
        /// Souřadnice v jednom stringu pevné struktury, lze porovnávat dvě různé souřadnice pomocí tohoto stringu.
        /// </summary>
        public string PointText { get { return this.Point.Text; } }
        /// <summary>
        /// Vrátí textové vyjádření souřadnic v daném / aktuálním formátu
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public string GetCoordinates(MapCoordinatesFormat? format = null)
        {
            return _GetCoordinates(format);
        }
        /// <summary>
        /// Opíše do sebe informace z dodaného objektu, volitelně potlačí event 
        /// </summary>
        /// <param name="mapData">Zdrojové souřadnice</param>
        /// <param name="isSilent">Nevolat událost <see cref="CoordinatesChanged"/> po případné změně</param>
        public void FillFrom(MapProviderBase.MapDataInfo mapData, bool isSilent = false)
        {
            var coordinatesOld = _CoordinatesSerial;
            _FillFromAction(mapData);

            // Pokud nynější souřadnice jsou změněny a není Silent, vyvoláme event o změně:
            if (!isSilent)
                _CheckCoordinatesChanged(coordinatesOld);
        }
        /// <summary>
        /// Opíše do sebe informace z dodaného objektu, volitelně potlačí event 
        /// </summary>
        /// <param name="sourceCoordinates">Zdrojové souřadnice</param>
        /// <param name="isSilent">Nevolat událost <see cref="CoordinatesChanged"/> po případné změně</param>
        public void FillFrom(MapCoordinates sourceCoordinates, bool isSilent = false)
        {
            var coordinatesOld = _CoordinatesSerial;
            _FillFromAction(sourceCoordinates);

            // Pokud nynější souřadnice jsou změněny a není Silent, vyvoláme event o změně:
            if (!isSilent)
                _CheckCoordinatesChanged(coordinatesOld);
        }
        /// <summary>
        /// Opíše do sebe informace z dodaného objektu
        /// </summary>
        /// <param name="mapData">Zdrojové souřadnice</param>
        private void _FillFromAction(MapProviderBase.MapDataInfo mapData)
        {
            if (mapData != null)
            {
                // Přenášíme hodnoty proměnných, tím vynecháváme vyhodnocování defaultů = defaulty akceptujeme naše. Máme přenést fyzické hodnoty načtených koordinátů, a ne jejich defaulty!
                this.__Point = mapData.Point;
                this.__Zoom = mapData.Zoom;
                this.__Center = mapData.Center;
                this.__MapType = mapData.MapType;
                this.__InfoPanelVisible = mapData.ShowInfoPanel;
                this.__ShowPinAtPoint = mapData.ShowPointPin ?? false;
                if (mapData.MapProvider != null) this.__Provider = mapData.MapProvider;
                this.__IsEmpty = false;
            }
        }
        /// <summary>
        /// Opíše do sebe informace z dodaného objektu
        /// </summary>
        /// <param name="newCoordinates"></param>
        private void _FillFromAction(MapCoordinates newCoordinates)
        {
            if (newCoordinates != null)
            {
                // Přenášíme hodnoty proměnných, tím vynecháváme vyhodnocování defaultů = defaulty akceptujeme naše. Máme přenést fyzické hodnoty načtených koordinátů, a ne jejich defaulty!
                this.__Point = newCoordinates.__Point;
                this.__Zoom = newCoordinates.__Zoom;
                this.__Center = newCoordinates.__Center;
                this.__Provider = newCoordinates.__Provider;
                this.__MapType = newCoordinates.__MapType;
                this.__InfoPanelVisible = newCoordinates.__InfoPanelVisible;
                this.__ShowPinAtPoint = newCoordinates.__ShowPinAtPoint;
                this.__IsEmpty = newCoordinates.__IsEmpty;
            }
        }
        /// <summary>
        /// Vloží dodané souřadnice. Poté vyvolá event o změně, pokud reálně došlo ke změně.
        /// </summary>
        /// <param name="coordinates">Vkládané souřadnice</param>
        /// <param name="isSilent">Nevolat událost <see cref="CoordinatesChanged"/> po případné změně</param>
        private void _SetCoordinates(string coordinates, bool isSilent = false)
        {
            var coordinatesOld = _CoordinatesSerial;
            _Reset(ResetSource.Coordinate);
            if (!String.IsNullOrEmpty(coordinates))
                _SetCoordinatesAction(coordinates);

            // Pokud nynější souřadnice jsou změněny, vyvoláme event o změně:
            if (!isSilent)
                _CheckCoordinatesChanged(coordinatesOld);
        }
        /// <summary>
        /// Vloží dodané souřadnice. Neřeší event o změně, jen řeší hodnoty.
        /// </summary>
        /// <param name="coordinates"></param>
        private void _SetCoordinatesAction(string coordinates)
        {
            // Center a Zoom nechám null (po Resetu), protože data vkládám do Point.
            PointD point;
            Double pointX, pointY;

            // Kódy OpenLocationCode testuji předem, protože na to mám nářadí:
            if (OpenLocationCodeConvertor.TryParse(coordinates, out point))
            {
                this.CoordinatesFormat = MapCoordinatesFormat.OpenLocationCode;
                this._Point = point;
                return;
            }

            // Následuje analýza a parsování jednoduchého stringu:
            coordinates = coordinates.Trim().ToUpper();

            // Vyhledání hodnot X,Y podle vyhodnocení varianty zadání:
            bool isPoint = coordinates.StartsWith("POINT", StringComparison.InvariantCultureIgnoreCase) && (coordinates.Length > 7);
            if (isPoint)
            {   // Point má speciální větev:
                coordinates = coordinates.Substring(5).Replace("(", "").Replace(")", "").Trim();      // "POINT (14.4009383 50.0694664)"  =>  "14.4009383 50.0694664"
                var pointParts = coordinates.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (pointParts.Length == 2 && MapProviderBase.TryParseDouble(pointParts[0], out pointX) && MapProviderBase.TryParseDouble(pointParts[1], out pointY))
                {   // POINT má pořadí dat: X, Y
                    this.CoordinatesFormat = MapCoordinatesFormat.NephritePoint;
                    this._Point = new PointD(pointX, pointY);
                }
            }
            else
            {   // Ostatní formáty mají jako oddělovač čárku, používají desetinnou tečku a mají pořadí Y, X. Mezery nepotřebujeme. Dokážeme parsovat různé formáty v jedné metodě:
                coordinates = coordinates.Replace(" ", "").ToUpper();
                var coordParts = coordinates.Split(',');
                if (coordParts.Length == 2 &&
                    MapProviderBase.TryParseGeoDouble(coordParts[0], MapProviderBase.GeoAxis.Latitude, out MapProviderBase.GeoAxis axis0, out var fmt0, out var point0) &&
                    MapProviderBase.TryParseGeoDouble(coordParts[1], MapProviderBase.GeoAxis.Longitude, out MapProviderBase.GeoAxis axis1, out var fmt1, out var point1))
                {   // Souřadnice mají defaultní pořadí Y, X. Ale pro jistotu jsme detekovali reálně nalezené značky kvadrantů axis1 a axis0, a hodnoty jsme uložili do fmt1 a fmt0, point1 a point0.
                    // Nyní provedeme detekci pořadí a výsledného formátu:
                    detectCoordinates(axis0, fmt0, point0, axis1, fmt1, point1, out var format, out point);
                    this.CoordinatesFormat = format;
                    this._Point = point;
                }
            }

            // Metoda analyzuje určené hodnoty souřadnic, formátů a os ze souřadnice [0] a [1], a určí formát a souřadný bod (X,Y)
            void detectCoordinates(MapProviderBase.GeoAxis axis0, MapCoordinatesFormat? format0, Double value0, MapProviderBase.GeoAxis axis1, MapCoordinatesFormat? format1, Double value1, out MapCoordinatesFormat format, out PointD pointXY)
            {
                // Nativní pořadí je: 0 = Y,  1 = X
                // Pokud bych ale pro obě souřadnice určil pořadí reverzní (0 = X, 1 = Y), pak je budeme brát opačně:
                bool isNativeOrder = !(axis0 == MapProviderBase.GeoAxis.Longitude && axis1 == MapProviderBase.GeoAxis.Latitude);

                // Bod v nativním pořadí má 0=Y, 1=X;  v reverzním opačně:
                pointXY = isNativeOrder ? new PointD(value1, value0) : new PointD(value0, value1);

                // Formát vezmu: pokud není určen žádný, pak defaul; Pokud je nalezen jen jeden, tak ten jeden; Pokud oba tak je komplexně vyhodnotím:
                bool hasFormat0 = format0.HasValue;
                bool hasFormat1 = format1.HasValue;
                if (!hasFormat0 && !hasFormat1)
                    format = MapCoordinatesFormat.Wgs84Decimal;
                else if (hasFormat0 && !hasFormat1)
                    format = format0.Value;
                else if (!hasFormat0 && hasFormat1)
                    format = format1.Value;
                else
                {   // Mám oba formáty, sečtu je:
                    evaluateFormat(format0.Value, out bool hasPrefix0, out bool hasSuffix0, out bool hasArcGrd0, out bool hasArcSec0);
                    evaluateFormat(format1.Value, out bool hasPrefix1, out bool hasSuffix1, out bool hasArcGrd1, out bool hasArcSec1);
                    bool hasPrefix = hasPrefix0 || hasPrefix1;
                    bool hasSuffix = hasSuffix0 || hasSuffix1;
                    if (hasArcSec0 || hasArcSec1)
                        // Některá souřadnice má i úhlové vteřiny?
                        format = hasPrefix ? MapCoordinatesFormat.Wgs84ArcSecPrefix : MapCoordinatesFormat.Wgs84ArcSecSuffix;
                    else if (hasArcGrd0 || hasArcGrd1)
                        // Některá souřadnice má úhlové stupně a minuty?
                        format = hasPrefix ? MapCoordinatesFormat.Wgs84MinutePrefix : MapCoordinatesFormat.Wgs84MinuteSuffix;
                    else if (hasPrefix)
                        // Nemáme úhly => jsme Decimal: s prefixem?
                        format = MapCoordinatesFormat.Wgs84DecimalPrefix;
                    else if (hasSuffix)
                        // Decimal se Suffixem:
                        format = MapCoordinatesFormat.Wgs84DecimalSuffix;
                    else
                        // Decimal bez prefixu a suffixu:
                        format = MapCoordinatesFormat.Wgs84Decimal;
                }
            }
            // Vyhodnotí vlastnosti formátu do out parametrů:
            void evaluateFormat(MapCoordinatesFormat format, out bool hasPrefix, out bool hasSuffix, out bool hasArcGrd, out bool hasArcSec)
            {
                hasPrefix = (format == MapCoordinatesFormat.Wgs84DecimalPrefix || format == MapCoordinatesFormat.Wgs84MinutePrefix || format == MapCoordinatesFormat.Wgs84ArcSecPrefix);
                hasSuffix = (format == MapCoordinatesFormat.Wgs84DecimalSuffix || format == MapCoordinatesFormat.Wgs84MinuteSuffix || format == MapCoordinatesFormat.Wgs84ArcSecSuffix);
                hasArcGrd = (format == MapCoordinatesFormat.Wgs84MinutePrefix || format == MapCoordinatesFormat.Wgs84MinuteSuffix || format == MapCoordinatesFormat.Wgs84ArcSecPrefix || format == MapCoordinatesFormat.Wgs84ArcSecSuffix);
                hasArcSec = (format == MapCoordinatesFormat.Wgs84ArcSecPrefix || format == MapCoordinatesFormat.Wgs84ArcSecSuffix);
            }



            //  Řešíme tedy varianty:
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
        }
        /// <summary>
        /// Vrátí textové vyjádření souřadnic v daném / aktuálním formátu
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private string _GetCoordinates(MapCoordinatesFormat? format = null)
        {
            if (!format.HasValue) format = this.CoordinatesFormat;
            var point = this._Point;

            var fmt = format.Value;
            switch (fmt)
            {
                case MapCoordinatesFormat.NephritePoint:
                    return $"POINT ({MapProviderBase.FormatGeoDouble(point.X, MapProviderBase.GeoAxis.Longitude, fmt, 12)} {MapProviderBase.FormatGeoDouble(point.Y, MapProviderBase.GeoAxis.Latitude, fmt, 12)})";

                case MapCoordinatesFormat.Nephrite:
                case MapCoordinatesFormat.Wgs84Decimal:
                case MapCoordinatesFormat.Wgs84DecimalSuffix:
                case MapCoordinatesFormat.Wgs84DecimalPrefix:
                case MapCoordinatesFormat.Wgs84MinuteSuffix:
                case MapCoordinatesFormat.Wgs84MinutePrefix:
                case MapCoordinatesFormat.Wgs84ArcSecSuffix:
                case MapCoordinatesFormat.Wgs84ArcSecPrefix:
                    return $"{MapProviderBase.FormatGeoDouble(point.Y, MapProviderBase.GeoAxis.Latitude, fmt, 12)}, {MapProviderBase.FormatGeoDouble(point.X, MapProviderBase.GeoAxis.Longitude, fmt, 12)}";

                case MapCoordinatesFormat.OpenLocationCode:
                    return OpenLocationCodeConvertor.Encode(point.X, point.Y, 12);
            }

            return "";
        }
        /// <summary>
        /// Pokud aktuální hodnota <see cref="_CoordinatesSerial"/> je odlišná od dodané hodnoty (před změnami), vyvolá události o změně souřadnic.
        /// </summary>
        /// <param name="coordinatesOld"></param>
        private void _CheckCoordinatesChanged(string coordinatesOld)
        {
            string coordinatesNew = _CoordinatesSerial;
            if (!String.Equals(coordinatesOld, coordinatesNew))
                _RunCoordinatesChanged();
        }
        /// <summary>
        /// Vyvolá event <see cref="CoordinatesChanged"/>.
        /// </summary>
        private void _RunCoordinatesChanged()
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"DxMapCoordinates.CoordinatesChanged() : {CoordinatesDebug}");
            CoordinatesChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost volaná po změně souřadnic <see cref="Coordinates"/>
        /// </summary>
        public event EventHandler CoordinatesChanged;
        /// <summary>
        /// Aktuální souřadnice v jednoduchém stringu, pro detekci změny
        /// </summary>
        private string _CoordinatesSerial
        {
            get
            {
                string text = $"{this.Point.Text};{_FormatIntN(this._Zoom)};";
                if (this.HasCenter)
                    text += $"{this.Center.Value.Text}";
                return text;
            }
        }
        /// <summary>
        /// Text pro Debug (ve formátu <see cref="MapCoordinatesFormat.Wgs84ArcSecPrefix"/>)
        /// </summary>
        internal string CoordinatesDebug { get { return this._GetCoordinates(MapCoordinatesFormat.Wgs84ArcSecPrefix); } }
        #endregion
        #region UrlAdress : Práce s URL adresou (set, get, analýza i syntéza, event o změně), konkrétní providery (Seznam, Google, OpenMap)
        /// <summary>
        /// Metoda se pokusí analyzovat dodanou URL adresu a naplnit z ní data do instance <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="urlAdress"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        public static bool TryParseUrlAdress(string urlAdress, MapProviderBase.MapDataInfo currentData, out MapProviderBase.MapDataInfo parsedData)
        {
            return _TryParseUrlAdress(urlAdress, currentData, out parsedData);
        }
        /// <summary>
        /// URL adresa mapy.
        /// Vložení takové hodnoty, která změní souřadnice, <b>vyvolá</b> event o změně <see cref="CoordinatesChanged"/>!
        /// </summary>
        public string UrlAdress { get { return _GetUrlAdress(); } set { _SetUrlAdress(value); } }
        /// <summary>
        /// URL adresa mapy.
        /// Vložení takové hodnoty, která změní souřadnice, <b>nevyvolá</b> event o změně <see cref="CoordinatesChanged"/>!
        /// </summary>
        protected string _UrlAdress { get { return _GetUrlAdress(); } set { _SetUrlAdress(value, true); } }
        /// <summary>
        /// Vygeneruje a vrátí URL pro daný / aktuální provider (Seznam / Google / OpenStreet) a daný / aktuální typ mapy.
        /// </summary>
        /// <returns></returns>
        private string _GetUrlAdress(IMapProvider provider = null, MapCoordinatesMapType? mapType = null)
        {
            if (provider is null) provider = this.Provider;
            if (provider is null) return null;
            return provider.GetUrlAdress(this._MapData);
        }
        /// <summary>
        /// Vloží dodanou URL adresu do koordinátů
        /// </summary>
        /// <param name="urlAdress"></param>
        /// <param name="isSilent">Nevolat událost <see cref="CoordinatesChanged"/> po případné změně</param>
        private void _SetUrlAdress(string urlAdress, bool isSilent = false)
        {
            if (_TryParseUrlAdress(urlAdress, this._MapData, out var mapData))
            {
                this.FillFrom(mapData, isSilent);
            }
        }
        /// <summary>
        /// Metoda se pokusí analyzovat dodanou URL adresu a naplnit z ní data do new instance <see cref="MapCoordinates"/>.
        /// </summary>
        /// <param name="urlAdress"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        private static bool _TryParseUrlAdress(string urlAdress, MapProviderBase.MapDataInfo currentData, out MapProviderBase.MapDataInfo parsedData)
        {
            parsedData = null;
            if (!Uri.TryCreate(urlAdress, UriKind.RelativeOrAbsolute, out var uri)) return false;

            var providers = MapProviders.AllProviders;
            foreach (var provider in providers)
            {
                if (provider.TryParseUrlAdress(uri, currentData, out parsedData))
                {
                    parsedData.MapProvider = provider;
                    return true;
                }
            }
            parsedData = null;
            return false;
        }
        /// <summary>
        /// Aktuální data zdejších koordinátů ve formě MapData pro providery <see cref="IMapProvider"/>
        /// </summary>
        public MapProviderBase.MapDataInfo MapData { get { return _MapData; } }
        /// <summary>
        /// Aktuální data zdejších koordinátů ve formě MapData pro providery <see cref="IMapProvider"/>
        /// </summary>
        private MapProviderBase.MapDataInfo _MapData
        {
            get
            {
                var mapData = new MapProviderBase.MapDataInfo()
                {
                    Point = this.Point,
                    Zoom = this.Zoom,
                    Center = this.Point,
                    MapType = this.MapType,
                    MapProvider = this.Provider,
                    ShowPointPin = this.ShowPinAtPoint,
                    ShowInfoPanel = this.InfoPanelVisible
                };
                return mapData;
            }
        }
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
        private static bool _TryParseGeoDecimal(string text, out MapCoordinatesFormat? format, out decimal value)
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
            format = (startWith ? MapCoordinatesFormat.Wgs84DecimalPrefix : (endsWith ? MapCoordinatesFormat.Wgs84DecimalSuffix : MapCoordinatesFormat.Wgs84Decimal));
            value = (isNegative ? -number : number);
            return true;
        }
        private static bool _TryParseGeoGrade(string text, out MapCoordinatesFormat? format, out decimal value)
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
            format = (startWith ? (hasArcSec ? MapCoordinatesFormat.Wgs84ArcSecPrefix : MapCoordinatesFormat.Wgs84MinutePrefix) : (hasArcSec ? MapCoordinatesFormat.Wgs84ArcSecSuffix : MapCoordinatesFormat.Wgs84MinuteSuffix));
            value = (isNegative ? -result : result);
            return true;
        }
        private static string _FormatGeoDecimal(decimal value, GeoAxis axis, MapCoordinatesFormat format, int decimals = 7)
        {
            bool usePrefix = (format == MapCoordinatesFormat.Wgs84DecimalPrefix);
            bool useSuffix = (format == MapCoordinatesFormat.Nephrite || format == MapCoordinatesFormat.Wgs84DecimalSuffix);
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
        private static string _FormatGeoGrade(decimal value, GeoAxis axis, MapCoordinatesFormat format)
        {
            bool usePrefix = (format == MapCoordinatesFormat.Wgs84MinutePrefix || format == MapCoordinatesFormat.Wgs84ArcSecPrefix);
            bool useSuffix = (format == MapCoordinatesFormat.Wgs84MinuteSuffix || format == MapCoordinatesFormat.Wgs84ArcSecSuffix);
            bool useArcSec = (format == MapCoordinatesFormat.Wgs84ArcSecSuffix || format == MapCoordinatesFormat.Wgs84ArcSecPrefix);

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
            text = _FormatEndsZeros(text);
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
            text = _FormatEndsZeros(text);
            return text;
        }
        private static string _FormatEndsZeros(string text)
        {
            text = text.Replace(_DotChar, ".").Replace(" ", "");
            if (text.Contains("."))
            {
                while (text.Length > 1 && text.EndsWith("0"))
                {
                    if (text.Length >= 2 && text.EndsWith(".0"))
                    {   // "xxxx.0"     =>  "0"     a konec  :
                        if (text.Length == 2)
                        {   // ".0"
                            text = "0";
                            break;
                        }
                        else
                        {   // "123.0"  =>  "123"   a konec :
                            text = text.Substring(0, (text.Length - 2));
                            break;
                        }
                    }
                    else
                    {   // "456.250"    => "456.25" a jdeme dál  ...
                        text = text.Substring(0, (text.Length - 1));
                    }
                }
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
    public enum MapCoordinatesFormat
    {
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
        /// OpenLocationCode od Google: <b>9F2Q6R5M+M3</b>
        /// </summary>
        OpenLocationCode
    }
    /// <summary>
    /// Varianta mapy
    /// </summary>
    public enum MapCoordinatesMapType
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
    #region class MapProviders : třída/y, poskytující přístup na mapové podklady na základě daných souřadnic
    /// <summary>
    /// Soupis providerů map, výběr aktuálního providera
    /// </summary>
    public class MapProviders
    {
        #region Static získání a seznam dostupných providerů v aktuální assembly
        /// <summary>
        /// Implicitní provider mapových podkladů, první provider v poli <see cref="AllProviders"/>
        /// </summary>
        public static IMapProvider DefaultProvider { get { var allProviders = AllProviders; return (allProviders.Length > 0 ? allProviders[0] : null); } }
        /// <summary>
        /// Aktuálně vybraná provider, nebo <see cref="DefaultProvider"/>.
        /// </summary>
        public static IMapProvider CurrentProvider { get { return __CurrentProvider ?? DefaultProvider; } set { __CurrentProvider = value; } }
        private static IMapProvider __CurrentProvider;
        /// <summary>
        /// Pole všech mapových providerů, které jsou dostupné v aktuální assembly. 
        /// Toto pole obsahuje i providery, které mají <see cref="IMapProvider.Activity"/> = false.
        /// <para/>
        /// Uživatelskou nabídku obsahuje pole <see cref="UserAccessibleProviders"/>.
        /// </summary>
        public static IMapProvider[] AllProviders
        {
            get
            {
                if (__AllProviders is null)
                    __AllProviders = _GetAllProviders();
                return __AllProviders.ToArray();
            }
        }
        /// <summary>
        /// Pole těch mapových providerů, které se mohou nabídnout uživateli.
        /// Toto pole obsahuje pouze takové providery, které mají <see cref="IMapProvider.Activity"/> = true.
        /// <para/>
        /// Kompletní nabídku všech providerů obsahuje pole <see cref="AllProviders"/>.
        /// </summary>
        public static IMapProvider[] UserAccessibleProviders { get { return AllProviders.Where(p => (p.Activity == MapProviderActivity.Active || p.Activity == MapProviderActivity.InDevelop)).ToArray(); } }
        /// <summary>
        /// Úložiště pole <see cref="AllProviders"/>
        /// </summary>
        private static IMapProvider[] __AllProviders;
        /// <summary>
        /// Z aktuální assembly načte všechny konkrétní mapové providery, a vrátí pole jejich instancí.
        /// </summary>
        /// <returns></returns>
        private static IMapProvider[] _GetAllProviders()
        {
            Type iMapProviderInterface = typeof(IMapProvider);                                               // Typ hledaného interface
            var providerTypes = typeof(MapProviderBase).Assembly.GetTypes().Where(isMapProvider).ToArray();  // Typy popisující hledané třídy, které řádně implementují IMapProvider
            bool isDebugger = System.Diagnostics.Debugger.IsAttached;                                        // Jsme v debuggeru

            var providers = new List<IMapProvider>();
            foreach (var providerType in providerTypes)
            {
                if (tryCreateProvider(providerType, out var provider))
                    providers.Add(provider);
            }
            // ORDER BY SortOrder ASC:
            if (providers.Count > 1)
                providers.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

            return providers.ToArray();


            // Vrátí true, pokud daný typ reprezentuje požadovanou třídu (neabstraktní class, implementuje IMapProvider, má public bezparametrický konstruktor)
            bool isMapProvider(Type type)
            {
                try
                {
                    // Hledáme typ, který je Class a není abstraktní:
                    if (type is null || !type.IsClass || type.IsAbstract) return false;

                    // Typ musí implementovat náš interface:
                    bool hasInterface = type.GetInterfaces().Any(i => i == iMapProviderInterface);
                    if (!hasInterface) return false;

                    // Typ musí mít public bezparametrický konstruktor:
                    bool hasConstructor = type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0);
                    if (!hasConstructor) return false;

                    return true;
                }
                catch { /* Nevyhodnotitelný typ může být například takový, který se odkazuje na nepřítomnou DLLku, pak mám exception "Assembly Xxx not found" */ }
                return false;
            }
            // Vytvoří instanci daného typu a vrátí ji jako IMapProvider. Pokud provider má Activity = neaktivní (v aktuální situaci), pak vrátí null.
            bool tryCreateProvider(Type type, out IMapProvider mapProvider)
            {
                try
                {
                    var instance = System.Activator.CreateInstance(type);
                    if (instance != null && instance is IMapProvider prvdr)
                    {
                        var activity = prvdr.Activity;
                        if (activity == MapProviderActivity.Active || activity == MapProviderActivity.Hidden || (activity == MapProviderActivity.InDevelop && isDebugger))
                        {
                            mapProvider = prvdr;
                            return true;
                        }
                    }
                }
                catch { /*  */ }

                mapProvider = null;
                return false;
            }
        }
        #endregion
    }
    /// <summary>
    /// Abstraktní předek pro mapové providery, nepovinný
    /// </summary>
    public abstract class MapProviderBase : IMapProvider
    {
        #region Explicitní implementace IMapProvider => abstraktní protected prvky
        string IMapProvider.ProviderId { get { return this.ProviderId; } }
        string IMapProvider.ProviderName { get { return this.ProviderName; } }
        MapProviderActivity IMapProvider.Activity { get { return this.Activity; } }
        int IMapProvider.SortOrder { get { return this.SortOrder; } }
        string IMapProvider.GetUrlAdress(MapProviderBase.MapDataInfo data) { return this.GetUrlAdress(data); }
        bool IMapProvider.TryParseUrlAdress(Uri uri, MapDataInfo currentData, out MapProviderBase.MapDataInfo parsedData) { return this.TryParseUrlAdress(uri, currentData, out parsedData); }
        /// <summary>
        /// ID provideru: pod tímto ID může být uložen provider např. v kódu / v konfiguraci
        /// </summary>
        protected abstract string ProviderId { get; }
        /// <summary>
        /// Název provideru: pod tímto názvem bude provider nabízen uživateli
        /// </summary>
        protected abstract string ProviderName { get; }
        /// <summary>
        /// Tohoto providera je možno nabízet uživateli ve výběru providerů?
        /// Některé providery nechceme aktivně nabízet, ale umíme s nimi pracovat.
        /// </summary>
        protected abstract MapProviderActivity Activity { get; }
        /// <summary>
        /// Pořadí provideru v poli mezi ostatními providery, pro nabídky
        /// </summary>
        protected abstract int SortOrder { get; }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected abstract string GetUrlAdress(MapProviderBase.MapDataInfo data);
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        protected abstract bool TryParseUrlAdress(Uri uri, MapDataInfo currentData, out MapProviderBase.MapDataInfo parsedData);
        /// <summary>
        /// Vizualizace = název provideru
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.ProviderName}";
        }
        #endregion
        #region Static support pro parsování / formátování číselných dat souřadnic
        /// <summary>
        /// Parsuje string na Int32.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static int ParseInt(string text)
        {
            if (!String.IsNullOrEmpty(text) && Int32.TryParse(text, out int value)) return value;
            return 0;
        }
        /// <summary>
        /// Parsuje string na Int32.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool TryParseInt(string text, out int value)
        {
            if (!String.IsNullOrEmpty(text) && Int32.TryParse(text, out value)) return true;
            value = 0;
            return false;
        }
        /// <summary>
        /// Parsuje string na Double. Na vstupu akceptuje aktuální desetinný oddělovač podle <c>CurrentCulture</c>.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static Double ParseDouble(string text)
        {
            var value = ParseDoubleN(text);
            return value ?? 0d;
        }
        /// <summary>
        /// Parsuje string na Double. Na vstupu akceptuje aktuální desetinný oddělovač podle <c>CurrentCulture</c>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool TryParseDouble(string text, out Double value)
        {
            var valueN = ParseDoubleN(text);
            value = valueN ?? 0d;
            return valueN.HasValue;
        }
        /// <summary>
        /// Parsuje string na Double. Na vstupu akceptuje aktuální desetinný oddělovač podle <c>CurrentCulture</c>.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static Double? ParseDoubleN(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                string dotChar = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                if (text.Contains(".") && dotChar != ".") text = text.Replace(".", dotChar);
                if (Double.TryParse(text, out var value)) return value;
            }
            return null;
        }
        /// <summary>
        /// Ze vstupního stringu v různorodém formátu získá Double hodnotu a info o vstupním formátu.
        /// Na vstupu může být: <c>"50.2095"</c> nebo <c>"50.456 N"</c> nebo <c>"W15.4809"</c> nebo <c>"S 50°25.366'"</c> nebo <c>"15°25'36.25'' E"</c> a podobně.
        /// <para/>
        /// Zadáním osy <paramref name="axisExpected"/> zajistíme zarovnání hodnoty do rozmezí -90 až +90 pro osu Y = <see cref="GeoAxis.Latitude"/>, nebo -180 až +180  pro osu X = <see cref="GeoAxis.Longitude"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="axisExpected">Očekávaná osa, podle pořadí v zadaných parametrech</param>
        /// <param name="axisFound">out Exaktně detekovaná osa, podle příznaků S/N/W/E v koordinátu. Pokud nebude příznak, pak zde bude <paramref name="axisExpected"/>.</param>
        /// <param name="format"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool TryParseGeoDouble(string text, GeoAxis axisExpected, out GeoAxis axisFound, out MapCoordinatesFormat? format, out Double value)
        {
            axisFound = axisExpected;
            format = null;
            value = 0d;
            if (String.IsNullOrEmpty(text)) return false;

            text = text.Trim().ToUpper();

            // Západ a Jih jsou záporné hodnoty:
            bool isNegativeSide = (text.Contains("W") || text.Contains("S"));

            // Vyhodnotíme zadanou světovou stranu a určíme axisFound:
            bool hasNS = (text.Contains("N") || text.Contains("S"));           // Sever / Jih
            bool hasWE = (text.Contains("W") || text.Contains("E"));           // Západ / Východ
            if (hasNS && !hasWE) axisFound = GeoAxis.Latitude;                 // Pokud máme Sever/Jih a nemáme Západ/Východ, pak je zadána hodnota Y = Latitude    ↕
            else if (!hasNS && hasWE) axisFound = GeoAxis.Longitude;           // Pokud nemáme Sever/Jih a máme Západ/Východ, pak je zadána hodnota X = Longitude  ◄ ►

            // Začínáme nebo končíme značkou světové strany?  Detekujeme její pozici a poté ji odebereme:
            //  "50°12'45.612''N"   =>   "50°12'45.612''"
            bool hasPrefix = (text.StartsWith("N") || text.StartsWith("S") || text.StartsWith("E") || text.StartsWith("W"));
            bool hasSuffix = (text.EndsWith("N") || text.EndsWith("S") || text.EndsWith("E") || text.EndsWith("W"));
            if (hasPrefix && hasSuffix) return false;
            text = text.Replace("N", "")
                       .Replace("S", "")
                       .Replace("E", "")
                       .Replace("W", "")
                       .Trim();

            // Prefix záporné hodnoty:
            bool isNegativeValue = (text.StartsWith("-"));
            if (isNegativeValue) text = text.Replace("-", "");

            if (text.Length == 0) return false;

            // Nyní máme obyčejné číslo "50.2091744", anebo "50°30.25'" anebo "50°30'45.5''"
            double result = 0d;
            bool hasResult = false;
            bool containsArcGrd = text.Contains("°");
            bool containsArcMin = text.Contains("'");
            bool containsArcSec = text.Contains("''");
            if (containsArcGrd || containsArcMin || containsArcSec)
            {   // Jsou tam nějaké stupně nebo minuty:
                string grades = text;
                if (tryGetDoubleBefore(ref grades, "°", out var arcGrd))
                {
                    result += arcGrd;
                    hasResult = true;
                }
                if (tryGetDoubleBefore(ref grades, "'", out var arcMin))
                {
                    result += (arcMin / 60d);
                    hasResult = true;
                }
                if (tryGetDoubleBefore(ref grades, "''", out var arcSec))
                {
                    result += (arcSec / 3600d);
                    hasResult = true;
                }
                // Formát může být jeden ze čtyř: s prefixem (N,S,E,W) nebo se suffixem, a s úhlovými sekundami nebo bez nich:
                //  Pokud by nebyl zadán ani prefix, ani suffix, pak dám implicitně Suffix;
                format = (containsArcSec ? (hasPrefix ? MapCoordinatesFormat.Wgs84ArcSecPrefix : MapCoordinatesFormat.Wgs84ArcSecSuffix) : (hasPrefix ? MapCoordinatesFormat.Wgs84MinutePrefix : MapCoordinatesFormat.Wgs84MinuteSuffix));
            }
            else
            {   // Není zadáno ve stupních => je zadáno v desetinném čísle:
                hasResult = TryParseDouble(text, out result);
                // Formát může být jeden ze tří: s prefixem, se suffixem anebo bez:
                format = (hasPrefix ? MapCoordinatesFormat.Wgs84DecimalPrefix : (hasSuffix ? MapCoordinatesFormat.Wgs84DecimalSuffix : MapCoordinatesFormat.Wgs84Decimal));
            }
            if (!hasResult) return false;

            // Hodnota bude záporná tehdy, když je záporná strana anebo (XOR) záporná hodnota  (South and 20 => -20, anebo North and -20 => -20).
            // Pokud budou oba příznaky negativní (South a "-20"), pak výsledek bude kladný!
            bool isNegative = (isNegativeSide != isNegativeValue);
            value = (isNegative ? -result : result);
            value = AlignGeoDouble(value, axisFound);
            return true;

            // Z dodaného stringu txt odebere část před delimiterem a vyhodnotí jako Double, ve stringu txt ponechá část za delimiterem:
            //  "45°30'" pro delimiter "°" odebere 45 (a převede na Double do 'part'), v txt zůstane "30'"
            bool tryGetDoubleBefore(ref string txt, string delimiter, out double part)
            {
                part = 0d;
                int index = txt.IndexOf(delimiter);
                if (index < 0) return false;
                int txtLength = txt.Length;
                int delLength = delimiter.Length;
                bool hasValue = false;
                if (index > 0)
                {   // Před delimiterem něco je: získám to a převedu na Double:
                    string num = txt.Substring(0, index);
                    hasValue = TryParseDouble(txt.Substring(0, index).Trim(), out part);
                    // Pokud by před delimiterem nic nebylo, pak výsledná hodnota je 0
                }
                // Delimiter byl nalezen: odeberu jej ze začátku textu
                txt = ((index + delLength) < txtLength ? txt.Substring(index).Trim() : "");
                return true;
            }
        }
        /// <summary>
        /// Formátuje danou hodnotu do Geo stringu; formáty Wgs84 : Decimal / Minute / ArcSec;   Prefix / Suffix / (bez)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="axis"></param>
        /// <param name="format"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        internal static string FormatGeoDouble(Double value, GeoAxis axis, MapCoordinatesFormat format, int decimals = 7)
        {
            bool usePrefix = (format == MapCoordinatesFormat.Wgs84DecimalPrefix || format == MapCoordinatesFormat.Wgs84MinutePrefix || format == MapCoordinatesFormat.Wgs84ArcSecPrefix);        // Aplikovat prefix
            bool useSuffix = (format == MapCoordinatesFormat.Wgs84DecimalSuffix || format == MapCoordinatesFormat.Wgs84MinuteSuffix || format == MapCoordinatesFormat.Wgs84ArcSecSuffix);        // Aplikovat suffix
            bool useArcMin = (format == MapCoordinatesFormat.Wgs84MinutePrefix || format == MapCoordinatesFormat.Wgs84MinuteSuffix);             // Použít úhlové stupně a minuty
            bool useArcSec = (format == MapCoordinatesFormat.Wgs84ArcSecSuffix || format == MapCoordinatesFormat.Wgs84ArcSecPrefix);             // Použít úhlové vteřiny

            if (useArcMin || useArcSec)
            {   // Použít stupně:
                if (usePrefix || useSuffix)
                {   // Kladná hodnota a označení světové strany N/S/W/E:
                    string quadrant = getGeoQuadrant(ref value, axis);                   // Zajistí kladnou a zarovnanou hodnotu value, a vrátí znak S/N | W/E pro zápornou/kladnou hodnotu a pro danou osu GeoAxis
                    string text = FormatGrade(value, useArcSec);
                    return (usePrefix ? $"{quadrant} {text}" : $"{text} {quadrant}");    // N 50°12'45.2''  nebo  50°12'45.2'' E
                }
                else
                {   // Kladná nebo záporná hodnota bez označení světové strany:
                    value = AlignGeoDouble(value, axis);                                 // -180 až 180  nebo  -90 až 90
                    string text = FormatGrade(value, useArcSec);                         // 50°30'45.256''   nebo   -16°25.145'
                    return text;
                }
            }
            else
            {   // Použít Double:
                if (usePrefix || useSuffix)
                {   // Kladná hodnota a označení světové strany N/S/W/E:
                    string quadrant = getGeoQuadrant(ref value, axis);                   // Zajistí kladnou a zarovnanou hodnotu value, a vrátí znak S/N | W/E pro zápornou/kladnou hodnotu a pro danou osu GeoAxis
                    string text = FormatDouble(value, decimals);
                    return (usePrefix ? $"{quadrant}{text}" : $"{text}{quadrant}");      // N50.2091744  nebo  50.2091744N
                }
                else
                {   // Kladná nebo záporná hodnota bez označení světové strany:
                    value = AlignGeoDouble(value, axis);                                 // -180 až 180  nebo  -90 až 90
                    string text = FormatDouble(value, decimals);
                    return text;
                }
            }


            // Vrátí kvadrant : N (North) nebo S (South) nebo E (East) nebo W (West) pro danou souřadnici.
            // ref hodnotu případně převede na kladnou, a zarovná do odpovídajících mezí.
            string getGeoQuadrant(ref double position, GeoAxis axis)
            {
                switch (axis)
                {
                    case GeoAxis.Latitude:
                        // Rozsah -90 (jižní) až +90° (severní) pól:
                        bool isSouth = (position < 0d);
                        if (isSouth) position = -position;
                        if (position > 90d) position = 90d;
                        return (isSouth ? "S" : "N");
                    case GeoAxis.Longitude:
                        // Rozsah -180 (západní) až +180° (východní) polokoule:
                        bool isWest = (position < 0d);
                        if (isWest) position = -position;
                        if (position > 180d) position = 180d;
                        return (isWest ? "W" : "E");
                }
                return "X";
            }
        }
        /// <summary>
        /// Zarovná dodanou hodnotu do patřičných mezí -90 až +90 pro osu Y = <see cref="GeoAxis.Latitude"/>, nebo -180 až +180  pro osu X = <see cref="GeoAxis.Longitude"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal static Double AlignGeoDouble(Double value, GeoAxis axis)
        {
            switch (axis)
            {
                case GeoAxis.Latitude:
                    value = (value < -90d ? -90d : (value > 90d ? 90d : value));
                    break;
                case GeoAxis.Longitude:
                    value = (value < -180d ? -180d : (value > 180d ? 180d : value));
                    break;
            }
            return value;
        }
        /// <summary>
        /// Formátuje danou hodnotu jako úhlové stupně a minuty [a vteřiny]. Neomezuje danou hodnotu do nějakým mezí. Výstup je např.  16°45.506425'  nebo  -16°45'30.512''
        /// </summary>
        /// <param name="value"></param>
        /// <param name="useArc"></param>
        /// <returns></returns>
        internal static string FormatGrade(Double value, bool useArc)
        {
            bool isNegative = (value < 0d);
            if (isNegative) value = -value;
            string neg = (isNegative ? "-" : "");                    // "-" pro negativní čísla
            var grd = Math.Floor(value);                             // 16.75844  =>  16                           (celé stupně)
            value = 60d * (value - grd);                             // 16.75844  =>   0.75844    =>  45.5064      (minuty)
            if (!useArc)
            {   // Bez vteřin:   16°45.506425'
                return $"{neg}{FormatDouble(grd, 1, 0)}°{FormatDouble(value, 2, 6)}'";
            }
            else
            {   // S vteřinami:  16°45'30.512''
                var min = Math.Floor(value);                         // 45.5064   =>  45                           (celé minuty)
                value = 60d * (value - min);                         // 45.5064   =>   0.5064     =>  30.384       (vteřiny)
                return $"{neg}{FormatDouble(grd, 1, 0)}°{FormatDouble(min, 2, 0)}'{FormatDouble(value, 2, 3)}''";
            }
        }
        /// <summary>
        /// Formátuje hodnotu Double na string; výstup má desetinnou tečku (ne čárku)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        internal static string FormatDouble(Double value, int decimals = 7)
        {
            decimals = Align(decimals, 0, 12);
            string fmt1 = "".PadRight(1, '0').PadLeft(12, '#');
            string fmt2 = "".PadRight(decimals, '0');
            string format = $"{fmt1}.{fmt2}";
            string text = (Math.Round(value, decimals)).ToString(format);
            text = FormatEndsZeros(text);
            return text;
        }
        /// <summary>
        /// Formátuje hodnotu Double na string; výstup má desetinnou tečku (ne čárku).
        /// Specifikuje se počet znaků celé části, tak abychom vygenerovali pro hodnotu 2.25 výstup např. "02.25"
        /// </summary>
        /// <param name="value"></param>
        /// <param name="leadings"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        internal static string FormatDouble(Double value, int leadings, int decimals = 7)
        {
            leadings = Align(leadings, 0, 12);
            decimals = Align(decimals, 0, 12);
            string fmt1 = "".PadRight(leadings, '0').PadLeft(12, '#');
            string fmt2 = "".PadRight(decimals, '0');
            string format = $"{fmt1}.{fmt2}";
            string text = (Math.Round(value, decimals)).ToString(format);
            text = FormatEndsZeros(text);
            return text;
        }
        /// <summary>
        /// Formátuje hodnotu Decimal na string; výstup má desetinnou tečku (ne čárku)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        internal static string FormatDecimal(Decimal value, int decimals = 7)
        {
            decimals = Align(decimals, 0, 12);
            string fmt1 = "".PadRight(1, '0').PadLeft(12, '#');
            string fmt2 = "".PadRight(decimals, '0');
            string format = $"{fmt1}.{fmt2}";
            string text = (Math.Round(value, decimals)).ToString(format);
            text = FormatEndsZeros(text);
            return text;
        }
        /// <summary>
        /// Formátuje hodnotu Decimal na string; výstup má desetinnou tečku (ne čárku).
        /// Specifikuje se počet znaků celé části, tak abychom vygenerovali pro hodnotu 2.25 výstup např. "02.25"
        /// </summary>
        /// <param name="value"></param>
        /// <param name="leadings"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        internal static string FormatDecimal(Decimal value, int leadings, int decimals = 7)
        {
            leadings = Align(leadings, 0, 12);
            decimals = Align(decimals, 0, 12);
            string fmt1 = "".PadRight(leadings, '0').PadLeft(12, '#');
            string fmt2 = "".PadRight(decimals, '0');
            string format = $"{fmt1}.{fmt2}";
            string text = (Math.Round(value, decimals)).ToString(format);
            text = FormatEndsZeros(text);
            return text;
        }
        /// <summary>
        /// Konvertuje desetinnou čárku na tečku, odebere koncové desetinné nuly i případnou koncovou desetinnou tečku ("012,000" převede na "012")
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static string FormatEndsZeros(string text)
        {
            text = text.Replace(",", ".").Replace(" ", "");
            if (text.Contains("."))
            {
                while (text.Length > 1 && text.EndsWith("0"))
                {
                    if (text.Length >= 2 && text.EndsWith(".0"))
                    {   // "xxxx.0"     =>  "0"     a konec  :
                        if (text.Length == 2)
                        {   // ".0"
                            text = "0";
                            break;
                        }
                        else
                        {   // "123.0"  =>  "123"   a konec :
                            text = text.Substring(0, (text.Length - 2));
                            break;
                        }
                    }
                    else
                    {   // "456.250"    => "456.25" a jdeme dál  ...
                        text = text.Substring(0, (text.Length - 1));
                    }
                }
            }
            return text;
        }
        /// <summary>
        /// Formátuje Nullable Double na string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        internal static string FormatDoubleN(Double? value, int decimals = 7)
        {
            return (value.HasValue ? FormatDouble(value.Value, decimals) : "");
        }
        /// <summary>
        /// Formátuje Int32 na string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string FormatInt(int value)
        {
            string text = value.ToString();
            return text.Replace(" ", "");
        }
        /// <summary>
        /// Formátuje Nullable Int32 na string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string FormatIntN(int? value)
        {
            return (value.HasValue ? FormatInt(value.Value) : "");
        }
        /// <summary>
        /// Zarovná hodnotu do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static int Align(int value, int min, int max) { return (value < min ? min : (value > max ? max : value)); }
        /// <summary>
        /// Zarovná hodnotu do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static int? AlignN(int? value, int min, int max) { return (value.HasValue ? (int?)(value < min ? min : (value > max ? max : value)) : (int?)null); }
        /// <summary>
        /// Zarovná hodnotu do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static Double Align(Double value, Double min, Double max) { return (value < min ? min : (value > max ? max : value)); }
        /// <summary>
        /// Zarovná hodnotu do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static Double? Align(Double? value, Double min, Double max) { return (value.HasValue ? (Double?)(value.Value < min ? min : (value.Value > max ? max : value.Value)) : (Double?)null); }
        /// <summary>
        /// Zarovná hodnotu do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal static Double AlignGeo(Double value, GeoAxis axis)
        {
            switch (axis)
            {
                case GeoAxis.Latitude:
                    // Rozsah -90 (jižní) až +90° (severní) pól:
                    return Align(value, -90d, 90d);
                case GeoAxis.Longitude:
                    // Rozsah -180 (západní) až +180° (východní) polokoule:
                    return Align(value, -180d, 180d);
            }
            return value;
        }
        /// <summary>
        /// Zarovná dodaný bod do Geo souřadnic (X: -180 až +180, Y: -90 až +90).
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        internal static PointD? AlignGeoN(PointD? point)
        {
            if (!point.HasValue) return null;
            return AlignGeo(point.Value);
        }
        /// <summary>
        /// Zarovná dodaný bod do Geo souřadnic (X: -180 až +180, Y: -90 až +90).
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        internal static PointD AlignGeo(PointD point)
        {
            var x = AlignGeo(point.X, GeoAxis.Longitude);
            var y = AlignGeo(point.Y, GeoAxis.Latitude);
            return new PointD(x, y);
        }
        /// <summary>
        /// Osy zeměpisné
        /// </summary>
        internal enum GeoAxis
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
        #region Static support pro analýzu URI Query
        /// <summary>
        /// Dodaný string (query z URL) rozdělí (danými separátory) na jednotlivé hodnoty (Key = Value) a jejich seznam vrátí. Prázdné prvky do seznamu nedává.
        /// Pokud by výsledný seznam měl 0 prvků, vrátí null. Jinými slovy, pokud na výstupu není null, pak je tam alespoň jeden prvek.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        internal static List<KeyValuePair<string, string>> ParseUriQuery(string query, params char[] separators)
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
        internal static void SearchUriQueryValue(string key, List<KeyValuePair<string, string>> queryData, out string value, out bool found)
        {
            if (TrySearchUriQueryPair(key, queryData, out var pair))
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
        internal static void SearchUriQueryValue(string key, List<KeyValuePair<string, string>> queryData, out Double value, out bool found)
        {
            if (TrySearchUriQueryPair(key, queryData, out var pair) && TryParseDouble(pair.Value, out var number))
            {
                value = number;
                found = true;
            }
            else
            {
                value = 0d;
                found = false;
            }
        }
        internal static void SearchUriQueryValue(string key, List<KeyValuePair<string, string>> queryData, out int value, out bool found)
        {
            if (TrySearchUriQueryPair(key, queryData, out var pair) && TryParseInt(pair.Value, out var number))
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
        internal static bool TrySearchUriQueryPair(string key, List<KeyValuePair<string, string>> queryData, out KeyValuePair<string, string> value)
        {
            return (queryData.TryGetFirst(kvp => String.Equals(kvp.Key, key), out value));
        }
        #endregion
        #region class MapDataInfo : souřadnice a další parametry mapy
        /// <summary>
        /// <see cref="MapDataInfo"/> : souřadnice a další parametry mapy
        /// </summary>
        public class MapDataInfo
        {
            /// <summary>
            /// Souřadnice cílového bodu
            /// </summary>
            public PointD Point { get; set; }
            /// <summary>
            /// Souřadnice středu mapy. Pokud nebude zadán, pak střed = <see cref="Point"/>.
            /// </summary>
            public PointD? Center { get; set; }
            /// <summary>
            /// Zoom mapy. Implicitní je 12. Rozsah je 0 - 21.
            /// </summary>
            public int? Zoom { get; set; }
            /// <summary>
            /// Zobrazit Pin v bodě <see cref="Point"/>? Default = ano
            /// </summary>
            public bool? ShowPointPin { get; set; }
            /// <summary>
            /// Zobrazit boční Info panel
            /// </summary>
            public bool? ShowInfoPanel { get; set; }
            /// <summary>
            /// Detekovaný provider mapy. Naplní se při parsování URI.
            /// </summary>
            public IMapProvider MapProvider { get; set; }
            /// <summary>
            /// Typ mapy
            /// </summary>
            public MapCoordinatesMapType? MapType { get; set; }
        }
        #endregion
    }
    /// <summary>
    /// Předpis pro třídy, které reprezentují provider mapových podkladů pro konkrétní stránku
    /// </summary>
    public interface IMapProvider
    {
        /// <summary>
        /// ID provideru: pod tímto ID může být uložen provider např. v kódu / v konfiguraci
        /// </summary>
        string ProviderId { get; }
        /// <summary>
        /// Název provideru: pod tímto názvem bude provider nabízen uživateli
        /// </summary>
        string ProviderName { get; }
        /// <summary>
        /// Tohoto providera je možno nabízet uživateli ve výběru providerů?
        /// Některé providery nechceme aktivně nabízet, ale umíme s nimi pracovat.
        /// </summary>
        MapProviderActivity Activity { get; }
        /// <summary>
        /// Pořadí provideru v poli mezi ostatními providery, pro nabídky
        /// </summary>
        int SortOrder { get; }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string GetUrlAdress(MapProviderBase.MapDataInfo data);
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        bool TryParseUrlAdress(Uri uri, MapProviderBase.MapDataInfo currentData, out MapProviderBase.MapDataInfo parsedData);
    }
    /// <summary>
    /// Aktivita provideru
    /// </summary>
    public enum MapProviderActivity
    {
        /// <summary>
        /// Neaktivní = vypnutý
        /// </summary>
        None = 0,
        /// <summary>
        /// Standardně aktivní (viditelný uživateli, za standardního běhu aplikace)
        /// </summary>
        Active,
        /// <summary>
        /// V aplikaci aktivní, ale uživateli vizuálně nedostupný
        /// </summary>
        Hidden,
        /// <summary>
        /// Aktivní pouze v Debug režimu, dostupný uživateli i aplikačnímu kódu
        /// </summary>
        InDevelop
    }
    #region Konkrétní provideři: pro zadanou souřadnici vygenerují URL, anebo ze zadané URL parsují souřadnici (a další informace)
    /*  Provider implementuje IMapProvider a tím může být použit jako obecný zdroj, nemusí být potomkem MapProviderBase
       * Provider je nalezen v aktuální assembly pouze podle přítomnosti interface IMapProvider, pomocí reflexe
       * Nabízí nějakou svoji identifikaci (kódovou, uživatelskou, přítomnost v nabídce a pořadí v ní)
       * Umí sestavit ze souřadnice cílovou URL,
       * Umí z URL adresy přečíst souřadnice - obě metody jsou volané přes interface
       * Nad rámec metod volaných přes interface může svoje služby nabídnout jako static metody
    */
    /// <summary>
    /// Provider map <b><u>SeznamMapy</u></b>, implementuje <see cref="IMapProvider"/>
    /// </summary>
    public class MapProviderSeznamMapy : MapProviderBase, IMapProvider
    {
        /// <summary>
        /// ID tohoto konkrétního provideru
        /// </summary>
        internal const string Id = "SeznamMapy";
        /// <summary>
        /// ID provideru: pod tímto ID může být uložen provider např. v kódu / v konfiguraci
        /// </summary>
        protected override string ProviderId { get { return Id; } }
        /// <summary>
        /// Název provideru: pod tímto názvem bude provider nabízen uživateli
        /// </summary>
        protected override string ProviderName { get { return "Seznam mapy"; } }
        /// <summary>
        /// Tohoto providera je možno nabízet uživateli ve výběru providerů?
        /// Některé providery nechceme aktivně nabízet, ale umíme s nimi pracovat.
        /// </summary>
        protected override MapProviderActivity Activity { get { return MapProviderActivity.Active; } }
        /// <summary>
        /// Pořadí provideru v poli mezi ostatními providery, pro nabídky
        /// </summary>
        protected override int SortOrder { get { return 100; } }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override string GetUrlAdress(MapDataInfo data) { return CreateUrlAdress(data); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        protected override bool TryParseUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, currentData, out parsedData); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data) { return MapProviderSeznamMapy.CreateUrlAdress(data, Id); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data, string providerId)
        {
            // https://mapy.cz/zakladni?l=0&source=coor&id=15.782303855847147%2C49.990992469096604&x=15.7821322&y=49.9893301&z=16

            // Web:
            string web = (providerId == MapProviderSeznamMapy.Id ? "https://mapy.cz/" :
                         (providerId == MapProviderSeznamFrameMapy.Id ? "https://frame.mapy.cz/" : "https://mapy.cz/"));

            // Typ mapy:
            var mapType = data.MapType.Value;
            string mapTypeUrl = (mapType == MapCoordinatesMapType.Standard ? "zakladni" :
                                (mapType == MapCoordinatesMapType.Photo ? "letecka" :
                                (mapType == MapCoordinatesMapType.Nature ? "turisticka" :
                                (mapType == MapCoordinatesMapType.Traffic ? "dopravni" :
                                (mapType == MapCoordinatesMapType.Specific ? "humanitarni" : "zakladni")))));

            // Následují parametry v Query. Typicky začínají (názvem proměnné) = (hodnota proměnné) a končí &
            // Poslední & bude nakonec odebrán.

            // Postranní panel? Pouze u providera Seznam. (Provider typu Frame nemá infopanel už z principu).
            bool withPanel = data.ShowInfoPanel.Value && providerId == MapProviderSeznamMapy.Id;
            string sidePanel = (withPanel ? "" : "l=0&");

            // Střed mapy: pokud je dán exaktně (HasCenter) pak jej akceptuji, jinak střed bude v Point:
            bool hasCenter = data.Center.HasValue;
            var pointX = data.Point.X;
            var pointY = data.Point.Y;
            var centerX = (hasCenter ? data.Center.Value.X : pointX);
            var centerY = (hasCenter ? data.Center.Value.Y : pointY);
            int zoom = data.Zoom.Value;

            // Umístit špendlík do bodu Point? Ano pokud je požadováno, anebo pokud je dán střed (pak Point může být jinde):
            string pinPoint = "";
            bool addPoint = data.ShowPointPin.Value || hasCenter;
            if (addPoint)
            {
                pinPoint = $"source=coor&id={FormatDouble(pointX, 15)}%2C{FormatDouble(pointY, 15)}&";
            }

            // Střed mapy a Zoom:
            string mapData = $"x={FormatDouble(centerX, 7)}&y={FormatDouble(centerY, 7)}&z={FormatInt(zoom)}&";

            // Složit URL:
            string urlAdress = $"{web}{mapTypeUrl}?{sidePanel}{pinPoint}{mapData}";
            if (urlAdress.EndsWith("&")) urlAdress = urlAdress.Substring(0, urlAdress.Length - 1);
            return urlAdress;
        }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, Id, currentData, out parsedData); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="providerId"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, string providerId, MapDataInfo currentData, out MapDataInfo parsedData)
        {
            parsedData = null;

            // Web:
            string web = (providerId == MapProviderSeznamMapy.Id ? "mapy.cz" :
                         (providerId == MapProviderSeznamFrameMapy.Id ? "frame.mapy.cz/" : "mapy.cz"));
            string host = uri.Host;
            if (!String.Equals(host, web, StringComparison.OrdinalIgnoreCase)) return false;

            // Typ mapy:
            string localPath = uri.LocalPath;
            MapCoordinatesMapType mapType =
                (String.Equals(localPath, "/zakladni") ? MapCoordinatesMapType.Standard :
                (String.Equals(localPath, "/letecka") ? MapCoordinatesMapType.Photo :
                (String.Equals(localPath, "/turisticka") ? MapCoordinatesMapType.Nature :
                (String.Equals(localPath, "/dopravni") ? MapCoordinatesMapType.Traffic :
                (String.Equals(localPath, "/humanitarni") ? MapCoordinatesMapType.Specific : MapCoordinatesMapType.None)))));
            if (mapType == MapCoordinatesMapType.None) return false;

            // Query obsahuje data:
            var queryData = ParseUriQuery(uri.Query, '?', '&');                  // ?&source=coor&id=15.795172900000000%2C49.949911300000000&x=15.7951729&y=49.9499113&z=8
            if (queryData is null) return false;

            // Pokud najdu X a Y, pak máme souřadnice, jinak nikoliv.
            SearchUriQueryValue("x", queryData, out Double centerX, out bool hasCenterX);
            SearchUriQueryValue("y", queryData, out Double centerY, out bool hasCenterY);
            bool hasCenter = hasCenterX && hasCenterY;
            if (!hasCenter) return false;

            // Další atributy jsou optional:

            // Zoom
            SearchUriQueryValue("z", queryData, out int zoom, out bool hasZoom);

            // Souřadnice bodu Point - výchozí je v souřadnici Center:
            bool hasPoint = false;
            Double pointX = centerX;
            Double pointY = centerY;

            // a) Point nalezený pomocí source=coor;id=(kordináty) :
            //    To se do mapy vkládá buď Ctrl+Click, anebo Pravá myš:menu Co je zde?
            SearchUriQueryValue("source", queryData, out string source, out bool hasSource);
            SearchUriQueryValue("id", queryData, out string id, out bool hasId);
            if (hasSource && String.Equals(source, "coor") && hasId && !String.IsNullOrEmpty(id) && id.Contains("%2C"))
            {   // Pokud v URL najdu: "source=coor&id=15.795172900000000%2C49.949911300000000"
                var coords = id.Split(new string[] { "%2C" }, StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length == 2 && TryParseDouble(coords[0], out Double ptX) && TryParseDouble(coords[1], out Double ptY))
                {
                    hasPoint = true;
                    pointX = ptX;
                    pointY = ptY;
                }
            }
            // b) Nebo pokud se uživatel pokusí o "navigování" Pravá myš:trasa sem

            // Postranní panel:
            bool hasPanel = false;
            if (providerId == MapProviderSeznamMapy.Id)
            {   // Parametr "l" má hodnotu "0" = nezobrazit panel; nepřítomnost parametru = zobrazit panel
                SearchUriQueryValue("l", queryData, out string sidePanel, out bool hasSidePanel);
                hasPanel = !hasSidePanel || (hasSidePanel && !String.Equals(sidePanel, "0"));
            }

            // Result:
            parsedData = new MapDataInfo()
            {
                Point = new PointD(pointX, pointY),
                Center = new PointD(centerX, centerY),
                MapType = mapType,
                Zoom = zoom,
                ShowInfoPanel = hasPanel,
                ShowPointPin = hasPoint
            };

            // OK:
            return true;
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
        //   https://mapy.cz/zakladni?moje-mapy&l=0&cat=dashboard&x=15.8010357&y=49.9187780&z=15

        // Více bodů najednou:
        //   https://mapy.cz/zakladni?vlastni-body&l=0&ut=M%C3%ADsto%20%C3%BAtoku&ut=M%C3%ADsto%20zadr%C5%BEen%C3%AD&uc=9hBKDxXwmmcORbET&ud=Na%20P%C5%99%C3%ADkop%C4%9B%20958%2F25%2C%20Praha%2C%20110%2000%2C%20Hlavn%C3%AD%20m%C4%9Bsto%20Praha&ud=Kov%C3%A1k%C5%AF%2C%20Praha%2C%20Hlavn%C3%AD%20m%C4%9Bsto%20Praha&x=14.4139961&y=50.0828279&z=14
    }
    /// <summary>
    /// Provider map <b><u>SeznamFrameMapy</u></b>, implementuje <see cref="IMapProvider"/>
    /// </summary>
    public class MapProviderSeznamFrameMapy : MapProviderBase, IMapProvider
    {
        /// <summary>
        /// ID tohoto konkrétního provideru
        /// </summary>
        internal const string Id = "SeznamFrameMapy";
        /// <summary>
        /// ID provideru: pod tímto ID může být uložen provider např. v kódu / v konfiguraci
        /// </summary>
        protected override string ProviderId { get { return Id; } }
        /// <summary>
        /// Název provideru: pod tímto názvem bude provider nabízen uživateli
        /// </summary>
        protected override string ProviderName { get { return "Seznam Frame mapy"; } }
        /// <summary>
        /// Tohoto providera je možno nabízet uživateli ve výběru providerů?
        /// Některé providery nechceme aktivně nabízet, ale umíme s nimi pracovat.
        /// </summary>
        protected override MapProviderActivity Activity { get { return MapProviderActivity.Active; } }
        /// <summary>
        /// Pořadí provideru v poli mezi ostatními providery, pro nabídky
        /// </summary>
        protected override int SortOrder { get { return 110; } }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override string GetUrlAdress(MapDataInfo data) { return CreateUrlAdress(data); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        protected override bool TryParseUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, currentData, out parsedData); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data) { return CreateUrlAdress(data, Id); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data, string providerId) { return MapProviderSeznamMapy.CreateUrlAdress(data, providerId); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, Id, currentData, out parsedData); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="providerId"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, string providerId, MapDataInfo currentData, out MapDataInfo parsedData) { return MapProviderSeznamMapy.TryAnalyzeUrlAdress(uri, providerId, currentData, out parsedData); }
    }
    /// <summary>
    /// Provider map <b><u>GoogleMaps</u></b>, implementuje <see cref="IMapProvider"/>
    /// </summary>
    public class MapProviderGoogleMaps : MapProviderBase, IMapProvider
    {
        /// <summary>
        /// ID tohoto konkrétního provideru
        /// </summary>
        internal const string Id = "GoogleMaps";
        /// <summary>
        /// ID provideru: pod tímto ID může být uložen provider např. v kódu / v konfiguraci
        /// </summary>
        protected override string ProviderId { get { return Id; } }
        /// <summary>
        /// Název provideru: pod tímto názvem bude provider nabízen uživateli
        /// </summary>
        protected override string ProviderName { get { return "GoogleMaps"; } }
        /// <summary>
        /// Tohoto providera je možno nabízet uživateli ve výběru providerů?
        /// Některé providery nechceme aktivně nabízet, ale umíme s nimi pracovat.
        /// </summary>
        protected override MapProviderActivity Activity { get { return MapProviderActivity.Active; } }
        /// <summary>
        /// Pořadí provideru v poli mezi ostatními providery, pro nabídky
        /// </summary>
        protected override int SortOrder { get { return 300; } }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override string GetUrlAdress(MapDataInfo data) { return CreateUrlAdress(data); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        protected override bool TryParseUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, currentData, out parsedData); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data) { return MapProviderGoogleMaps.CreateUrlAdress(data, Id); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data, string providerId)
        {
            // https://www.google.com/maps/@49.296045,17.390038,15z?hl=cs-CZ

            // Web:
            string web = "https://www.google.com/maps/";

            // Střed mapy: pokud je dán exaktně (HasCenter) pak jej akceptuji, jinak Pin:
            bool hasCenter = data.Center.HasValue;
            var pointX = data.Point.X;
            var pointY = data.Point.Y;
            var centerX = (hasCenter ? data.Center.Value.X : pointX);
            var centerY = (hasCenter ? data.Center.Value.Y : pointY);
            int zoom = data.Zoom.Value;

            // Umístit špendlík do bodu Point? Ano pokud je požadováno, anebo pokud je dán střed (pak Point může být jinde):
            //   Jak na to:   https://developers.google.com/maps/documentation/embed/embedding-map
            //                https://blog.hubspot.com/website/how-to-embed-google-map-in-html
            //   iframe   
            //  <iframe src="https://www.google.com/maps/embed?pb=!1m10!1m8!1m3!1d5134.491529403346!2d15.812057030195488!3d49.95049212566805!3m2!1i1024!2i768!4f13.1!5e0!3m2!1scs!2scz!4v1727839457077!5m2!1scs!2scz" width="600" height="450" style="border:0;" allowfullscreen="" loading="lazy" referrerpolicy="no-referrer-when-downgrade"></iframe>
            string pinPoint = "";
            bool addPoint = data.ShowPointPin.Value || hasCenter;
            if (addPoint)
            {
                pinPoint = $"source=coor&id={FormatDouble(pointX, 15)}%2C{FormatDouble(pointY, 15)}&";
            }

            // Střed mapy a Zoom:
            string mapData = $"@{FormatDouble(centerY, 12)},{FormatDouble(centerX, 12)},{FormatInt(zoom)}z";

            // Fixně:
            string lang = "hl=cs-CZ";

            string urlAdress = $"{web}{mapData}?{lang}";
            return urlAdress;
        }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, Id, currentData, out parsedData); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="providerId"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, string providerId, MapDataInfo currentData, out MapDataInfo parsedData)
        {
            parsedData = null;

            // Web:
            string web = "www.google.com";
            string host = uri.Host;
            if (!String.Equals(host, web, StringComparison.OrdinalIgnoreCase)) return false;

            var parts = ParseUriQuery(uri.PathAndQuery, '/', '?', '&');

            //  https://www.google.com/maps/@49.6835743,15.8558701,4854m/data=!3m1!1e3?hl=cs-CZ&entry=ttu&g_ep=EgoyMDI0MTAwNS4yIKXMDSoASAFQAw%3D%3D

            parsedData = null;
            return false;
        }
    }
    /// <summary>
    /// Provider map <b><u>OpenStreetMap</u></b>, implementuje <see cref="IMapProvider"/>
    /// </summary>
    public class MapProviderOpenStreetMap : MapProviderBase, IMapProvider
    {
        /// <summary>
        /// ID tohoto konkrétního provideru
        /// </summary>
        internal const string Id = "OpenStreetMap";
        /// <summary>
        /// ID provideru: pod tímto ID může být uložen provider např. v kódu / v konfiguraci
        /// </summary>
        protected override string ProviderId { get { return Id; } }
        /// <summary>
        /// Název provideru: pod tímto názvem bude provider nabízen uživateli
        /// </summary>
        protected override string ProviderName { get { return "Open Street Map"; } }
        /// <summary>
        /// Tohoto providera je možno nabízet uživateli ve výběru providerů?
        /// Některé providery nechceme aktivně nabízet, ale umíme s nimi pracovat.
        /// </summary>
        protected override MapProviderActivity Activity { get { return MapProviderActivity.Active; } }
        /// <summary>
        /// Pořadí provideru v poli mezi ostatními providery, pro nabídky
        /// </summary>
        protected override int SortOrder { get { return 400; } }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override string GetUrlAdress(MapDataInfo data) { return CreateUrlAdress(data); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        protected override bool TryParseUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, currentData, out parsedData); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data) { return CreateUrlAdress(data, Id); }
        /// <summary>
        /// Z dodaných dat o mapě <see cref="MapProviderBase.MapDataInfo"/> (souřadnice, typ, zoom atd) vytvoří URL adresu
        /// </summary>
        /// <param name="data"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        internal static string CreateUrlAdress(MapDataInfo data, string providerId)
        {
            // https://www.openstreetmap.org/?mlat=49.999988&mlon=15.757273#map=19/49.999988/15.757271&layers=T          obsahuje umístěnou značku - a bez postranních panelů a bez cizích poznámek

            // Web:
            string web = "https://www.openstreetmap.org/";
            string mapTypeLayer = (data.MapType.Value == MapCoordinatesMapType.Traffic ? "T" : "");

            // Střed mapy: pokud je dán exaktně (HasCenter) pak jej akceptuji, jinak Pin:
            bool hasCenter = data.Center.HasValue;
            var pointX = data.Point.X;
            var pointY = data.Point.Y;
            var centerX = (hasCenter ? data.Center.Value.X : pointX);
            var centerY = (hasCenter ? data.Center.Value.Y : pointY);
            int zoom = data.Zoom.Value;

            // Umístit špendlík do bodu Point? Ano pokud je požadováno, anebo pokud je dán střed (pak Point může být jinde):
            string pinPoint = "";
            string pinPointLayer = "";
            bool addPoint = data.ShowPointPin.Value || hasCenter;
            if (addPoint)
            {
                pinPoint = $"?mlat={FormatDouble(pointY, 12)}&mlon={FormatDouble(pointX, 12)}";
                pinPointLayer = "";
            }

            // Střed mapy a Zoom:
            string mapData = $"#map={FormatInt(zoom)}/{FormatDouble(centerY, 7)}/{FormatDouble(centerX, 7)}";

            // Vrstvy:
            string layers = mapTypeLayer + pinPointLayer;
            if (layers.Length > 0)
                layers = "&layers=" + layers;           // &layers=N


            // Složit URL:
            string urlAdress = $"{web}{pinPoint}{mapData}{layers}";
            return urlAdress;
        }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, MapDataInfo currentData, out MapDataInfo parsedData) { return TryAnalyzeUrlAdress(uri, Id, currentData, out parsedData); }
        /// <summary>
        /// Pokusí se parsovat dodanou URL adresu a vytěžit z ní informace o mapě <see cref="MapProviderBase.MapDataInfo"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="providerId"></param>
        /// <param name="currentData">Aktuální pozice v mapě</param>
        /// <param name="parsedData"></param>
        /// <returns></returns>
        internal static bool TryAnalyzeUrlAdress(Uri uri, string providerId, MapDataInfo currentData, out MapDataInfo parsedData)
        {
            parsedData = null;

            // Web:
            string host = uri.Host;
            if (!String.Equals(host, "www.openstreetmap.org", StringComparison.OrdinalIgnoreCase)) return false;

            var queryData = ParseUriQuery(uri.Query, '?', '&', '#');                               // Query:     ?mlat=50.0385298802&mlon=15.77897773802
            var fragmentData = ParseUriQuery(uri.Fragment, '?', '&', '#');                         // Fragment:  #map=14/50.03853/15.77898
            if (queryData is null && fragmentData is null) return false;

            SearchUriQueryValue("mlat", queryData, out Double pointY, out bool hasPointY);         // PointY = 50.0385298802
            SearchUriQueryValue("mlon", queryData, out Double pointX, out bool hasPointX);         // PointY = 15.77897773802
            bool hasPoint = hasPointX && hasPointY;

            SearchUriQueryValue("map", fragmentData, out string center, out bool hasCenter);       // 14/50.03853/15.77898
            int zoom = 0;
            Double centerX = 0d;
            Double centerY = 0d;
            bool hasValidMapData = false;
            if (hasCenter && !String.IsNullOrEmpty(center) && center.Contains("/"))
            {
                var centerParts = center.Split('/');
                hasValidMapData = (centerParts.Length == 3 &&
                                   TryParseInt(centerParts[0], out zoom) &&
                                   TryParseDouble(centerParts[1], out centerY) &&
                                   TryParseDouble(centerParts[2], out centerX));
            }
            if (!hasValidMapData) return false;

            // Pokud není uveden Point (mlat + mlon), tak jej převezmu z Center:
            if (!hasPoint)
            {
                pointX = centerX;
                pointY = centerY;
            }

            // Layer:
            var mapType = MapCoordinatesMapType.Standard;
            var hasPanel = false;

            // Result:
            parsedData = new MapDataInfo()
            {
                Point = new PointD(pointX, pointY),
                Center = new PointD(centerX, centerY),
                MapType = mapType,
                Zoom = zoom,
                ShowInfoPanel = hasPanel,
                ShowPointPin = hasPoint
            };

            return true;
        }

        /*

        Základní mapa, obsahuje Center (ale bez pointu, bez poznámek, základní typ):
        https://www.openstreetmap.org/#map=14/49.94746/15.80104

        Stejná mapa, ale obsahuje point kousek vedle od středu mapy, bez dalších Note:
        https://www.openstreetmap.org/?mlat=49.951259217818&mlon=15.794888555225#map=14/49.94746/15.80104

        Stejná mapa ale se zapnutím Navigovat sem (zmizí Center pozice mapy, pamatuje si ji od posledně):
        https://www.openstreetmap.org/directions?from=&to=49.95072%2C15.81851

        Zapnutá Navigovat sem (vpravo od středu) a poté posunutá = objeví se i Center mapy:
        https://www.openstreetmap.org/directions?from=&to=49.95072%2C15.81851#map=14/49.94746/15.80190

        Zapnutá Navigovat sem (vpravo od středu) i Navigovat odsud (vlevo od středu) = upravil se Zoom i Center, je doplněn navigační stroj:
        https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route=49.95145%2C15.76959%3B49.95073%2C15.81851#map=15/49.95301/15.79407

        Dopravní mapa na místě první základní mapy:
        https://www.openstreetmap.org/#map=14/49.94746/15.80104&layers=T

        Layers (&layers=T):
        ------
        T = Traffic
        N = Notes
        D = Data k mapě
        G = Veřejné GPS stopy
        Y = CycloOSM
        C = Cyklomapy
        P = Tracestack Topo
        H = Humanitární

        Navigační engines  (directions?engine=fossgis_valhalla_foot):
        -----------------
        graphhopper_car         
        fossgis_osrm_car
        fossgis_valhalla_car

        graphhopper_bicycle
        fossgis_osrm_bike
        fossgis_valhalla_bicycle

        graphhopper_foot
        fossgis_osrm_foot
        fossgis_valhalla_foot           


        // Navigace:
        //   https://www.openstreetmap.org/directions?from=&to=49.95442%2C15.78759#map=16/49.95399/15.79651;


        // Značka:
        //   https://www.openstreetmap.org/?mlat=49.999988&mlon=15.757273#map=19/49.999988/15.757271&layers=T          obsahuje umístěnou značku - a bez postranních panelů a bez cizích poznámek
        //   https://www.openstreetmap.org/#map=14/49.94349/15.79452&layers=N
        //   https://www.openstreetmap.org/?mlat=49.999988&mlon=15.757273#map=16/50.04246/15.82406                     jiné měřítko, std mapa
        //   https://www.openstreetmap.org/#map=14/49.94349/15.79452&layers=N
        //   https://www.openstreetmap.org/#map=12/49.9320/15.7875&layers=N
        //   https://www.openstreetmap.org/directions?from=&to=50.0238%2C15.6009#map=12/50.0280/15.6105&layers=P       Zadaný cíl cesty (From-To), a malý panel vlevo

        */
    }
    #endregion
    #endregion
    #region class OpenLocationCodeConvertor : třída obsahující kodér a dekodér souřadnic ve formátu OpenLocationCode
    /// <summary>
    /// <see cref="OpenLocationCodeConvertor"/> zajišťuje konverze stringu ve formátu <b>OpenLocationCode</b> na zeměpisné souřadnice (Longitude = X, a Latitude = Y) tam i zpět.
    /// </summary>
    public class OpenLocationCodeConvertor
    {
        /// <summary>
        /// Vrátí true, pokud daný string je validní souřadnice.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public static bool IsValid(string coordinates)
        {
            return OpenLocationCode.IsValid(coordinates);
        }
        /// <summary>
        /// Zkusí dekódovat danou souřadnici a vrátit její pozici (střed).
        /// </summary>
        /// <param name="coordinates">Souřadnice</param>
        /// <param name="point">Detekovaný bod, střed prostoru souřadnice.</param>
        /// <returns></returns>
        public static bool TryParse(string coordinates, out PointD point)
        {
            point = PointD.Empty; ;
            if (String.IsNullOrEmpty(coordinates) || coordinates.Contains(",") || coordinates.Trim().Contains(".") || coordinates.Trim().Contains(" ")) return false;   // Před-filtr
            if (!OpenLocationCode.IsValid(coordinates)) return false;

            // Longitude = Zeměpisná pozice X od Greenwiche (0) na východ (+180 = přes Východní Evropu a Asii) nebo na západ (-180 = přes Portugalsko, Grónsko k Americe a na Havaj).
            // Praha má cca 14.4°
            // Latitude = Zeměpisná pozice Y od rovníku (0) na sever k pólu (+90 = severní pól) a na jih (-90 = jižní pól).
            // Praha má cca 50°

            var codeArea = OpenLocationCode.Decode(coordinates);
            point = new PointD(codeArea.CenterLongitude, codeArea.CenterLatitude);
            return true;
        }
        /// <summary>
        /// Vrátí <b>OpenLocationCode</b> pro zadanou souřadnici, v dané přesnosti
        /// </summary>
        /// <param name="pointX"></param>
        /// <param name="pointY"></param>
        /// <param name="codeLength"></param>
        /// <returns></returns>
        public static string Encode(double pointX, double pointY, int? codeLength = null)
        {
            string code = null;
            if (codeLength.HasValue)
                code = OpenLocationCode.Encode(pointY, pointX, codeLength.Value);
            else
                code = OpenLocationCode.Encode(pointY, pointX);
            return code;
        }
        // Následující třídy pochází od JonMcPherson:
        //   https://github.com/JonMcPherson/open-location-code/
        //   https://www.nuget.org/packages/OpenLocationCode
        // Kód nemá licenční omezení.
        #region OpenLocationCode
        /// <summary>
        /// Convert locations to and from convenient codes known as Open Location Codes
        /// or <see href="https://plus.codes/">Plus Codes</see>
        /// <para>
        /// Open Location Codes are short, ~10 character codes that can be used instead of street
        /// addresses. The codes can be generated and decoded offline, and use a reduced character set that
        /// minimises the chance of codes including words.
        /// </para>
        /// The <see href="https://github.com/google/open-location-code/blob/master/API.txt">Open Location Code API</see>
        /// is implemented through the static methods:
        /// <list type="bullet">
        /// <item><see cref="IsValid(string)"/></item>
        /// <item><see cref="IsShort(string)"/></item>
        /// <item><see cref="IsFull(string)"/></item>
        /// <item><see cref="Encode(double, double, int)"/></item>
        /// <item><see cref="Decode(string)"/></item>
        /// <item><see cref="Shorten(string, double, double)"/></item>
        /// <item><see cref="ShortCode.RecoverNearest(string, double, double)"/></item>
        /// </list>
        /// Additionally an object type is provided which can be created using the constructors:
        /// <list type="bullet">
        /// <item><see cref="OpenLocationCode(string)"/></item>
        /// <item><see cref="OpenLocationCode(double, double, int)"/></item>
        /// <item><see cref="ShortCode(string)"/></item>
        /// </list>
        /// <example><code>
        /// OpenLocationCode code = new OpenLocationCode("7JVW52GR+2V");
        /// OpenLocationCode code = new OpenLocationCode(27.175063, 78.042188);
        /// OpenLocationCode code = new OpenLocationCode(27.175063, 78.042188, 11);
        /// OpenLocationCode.ShortCode shortCode = new OpenLocationCode.ShortCode("52GR+2V");
        /// </code></example>
        /// 
        /// With a code object you can invoke the various methods such as to shorten the code
        /// or decode the <see cref="CodeArea"/> coordinates.
        /// <example><code>
        /// OpenLocationCode.ShortCode shortCode = code.shorten(27.176, 78.05);
        /// OpenLocationCode recoveredCode = shortCode.recoverNearest(27.176, 78.05);
        /// 
        /// CodeArea codeArea = code.decode()
        /// </code></example>
        /// </summary>
        protected sealed class OpenLocationCode
        {
            /// <summary>
            /// Provides a normal precision code, approximately 14x14 meters.<br/>
            /// Used to specify encoded code length (<see cref="Encode(double,double,int)"/>)
            /// </summary>
            public const int CodePrecisionNormal = 10;
            /// <summary>
            /// Provides an extra precision code length, approximately 2x3 meters.<br/>
            /// Used to specify encoded code length (<see cref="Encode(double,double,int)"/>)
            /// </summary>
            public const int CodePrecisionExtra = 11;
            // A separator used to break the code into two parts to aid memorability.
            private const char SeparatorCharacter = '+';
            // The number of characters to place before the separator.
            private const int SeparatorPosition = 8;
            // The character used to pad codes.
            private const char PaddingCharacter = '0';
            // The character set used to encode the digit values.
            internal const string CodeAlphabet = "23456789CFGHJMPQRVWX";
            // The base to use to convert numbers to/from.
            private const int EncodingBase = 20; // CodeAlphabet.Length;
            // The encoding base squared also rep
            private const int EncodingBaseSquared = EncodingBase * EncodingBase;
            // The maximum value for latitude in degrees.
            private const int LatitudeMax = 90;
            // The maximum value for longitude in degrees.
            private const int LongitudeMax = 180;
            // Maximum code length using just lat/lng pair encoding.
            private const int PairCodeLength = 10;
            // Number of digits in the grid coding section.
            private const int GridCodeLength = MaxDigitCount - PairCodeLength;
            // Maximum code length for any plus code
            private const int MaxDigitCount = 15;
            // Number of columns in the grid refinement method.
            private const int GridColumns = 4;
            // Number of rows in the grid refinement method.
            private const int GridRows = 5;
            // The maximum latitude digit value for the first grid layer
            private const int FirstLatitudeDigitValueMax = 8; // lat -> 90
            // The maximum longitude digit value for the first grid layer
            private const int FirstLongitudeDigitValueMax = 17; // lon -> 180
            private const long GridRowsMultiplier = 3125; // Pow(GridRows, GridCodeLength)
            private const long GridColumnsMultiplier = 1024; // Pow(GridColumns, GridCodeLength)
            // Value to multiple latitude degrees to convert it to an integer with the maximum encoding
            // precision. I.e. ENCODING_BASE**3 * GRID_ROWS**GRID_CODE_LENGTH
            private const long LatIntegerMultiplier = 8000 * GridRowsMultiplier;
            // Value to multiple longitude degrees to convert it to an integer with the maximum encoding
            // precision. I.e. ENCODING_BASE**3 * GRID_COLUMNS**GRID_CODE_LENGTH
            private const long LngIntegerMultiplier = 8000 * GridColumnsMultiplier;
            // Value of the most significant latitude digit after it has been converted to an integer.
            private const long LatMspValue = LatIntegerMultiplier * EncodingBaseSquared;
            // Value of the most significant longitude digit after it has been converted to an integer.
            private const long LngMspValue = LngIntegerMultiplier * EncodingBaseSquared;
            // The ASCII integer of the minimum digit character used as the offset for indexed code digits
            private static readonly int IndexedDigitValueOffset = CodeAlphabet[0]; // 50
            // The digit values indexed by the character ASCII integer for efficient lookup of a digit value by its character
            private static readonly int[] IndexedDigitValues = new int[CodeAlphabet[CodeAlphabet.Length - 1] - IndexedDigitValueOffset + 1]; // int[38]
            static OpenLocationCode()
            {
                for (int i = 0, digitVal = 0; i < IndexedDigitValues.Length; i++)
                {
                    int digitIndex = CodeAlphabet[digitVal] - IndexedDigitValueOffset;
                    IndexedDigitValues[i] = (digitIndex == i) ? digitVal++ : -1;
                }
            }
            /// <summary>
            /// Creates an <see cref="OpenLocationCode"/> object for the provided full code (or <see cref="CodeDigits"/>).
            /// Use <see cref="ShortCode(string)"/> for short codes.
            /// </summary>
            /// <param name="code">A valid full Open Location Code or <see cref="CodeDigits"/></param>
            /// <exception cref="ArgumentException">If the code is null, not valid, or not full.</exception>
            public OpenLocationCode(string code)
            {
                if (code == null)
                {
                    throw new ArgumentException("code cannot be null");
                }
                Code = NormalizeCode(code.ToUpper());
                if (!IsValidUpperCase(Code) || !IsCodeFull(Code))
                {
                    throw new ArgumentException($"code '{code}' is not a valid full Open Location Code (or code digits).");
                }
                CodeDigits = TrimCode(Code);
            }
            /// <summary>
            /// Creates an <see cref="OpenLocationCode"/> object encoded from the provided latitude/longitude coordinates
            /// and having the provided code length (precision).
            /// </summary>
            /// <param name="latitude">The latitude coordinate in decimal degrees.</param>
            /// <param name="longitude">The longitude coordinate in decimal degrees.</param>
            /// <param name="codeLength">The number of digits in the code (Default: <see cref="CodePrecisionNormal"/>).</param>
            /// <exception cref="ArgumentException">If the code length is invalid (valid lengths: <c>4</c>, <c>6</c>, <c>8</c>, or <c>10+</c>).</exception>
            public OpenLocationCode(double latitude, double longitude, int codeLength = CodePrecisionNormal)
            {
                Code = Encode(latitude, longitude, codeLength);
                CodeDigits = TrimCode(Code);
            }
            /// <summary>
            /// Creates an <see cref="OpenLocationCode"/> object encoded from the provided geographic point coordinates
            /// with the provided code length.
            /// </summary>
            /// <param name="point">The geographic coordinate point.</param>
            /// <param name="codeLength">The desired number of digits in the code (Default: <see cref="CodePrecisionNormal"/>).</param>
            /// /// <exception cref="ArgumentException">If the code length is not valid.</exception>
            /// <remarks>Alternative to <see cref="OpenLocationCode(double, double, int)"/></remarks>
            public OpenLocationCode(GeoPoint point, int codeLength = CodePrecisionNormal) :
                this(point.Latitude, point.Longitude, codeLength)
            { }
            /// <summary>
            /// The code which is a valid full Open Location Code (plus code).
            /// </summary>
            /// <value>The string representation of the code.</value>
            public string Code { get; }
            /// <summary>
            /// The digits of the full code which excludes the separator '+' character and any padding '0' characters.
            /// This is useful to more concisely represent or encode a full Open Location Code
            /// since the code digits can be normalized back into a valid full code.
            /// </summary>
            /// <example>"8FWC2300+" -> "8FWC23", "8FWC2345+G6" -> "8FWC2345G6"</example>
            /// <value>The string representation of the code digits.</value>
            /// <remarks>This is a nonstandard code format.</remarks>
            public string CodeDigits { get; }
            /// <summary>
            /// Decodes this full Open Location Code into a <see cref="CodeArea"/> object
            /// encapsulating the latitude/longitude coordinates of the area bounding box.
            /// </summary>
            /// <returns>The decoded CodeArea for this Open Location Code.</returns>
            public CodeArea Decode()
            {
                return DecodeValid(CodeDigits);
            }
            /// <summary>
            /// Determines if this full Open Location Code is padded which is defined by <see cref="IsPadded(string)"/>.
            /// </summary>
            /// <returns><c>true</c>, if this Open Location Code is a padded, <c>false</c> otherwise.</returns>
            public bool IsPadded()
            {
                return IsCodePadded(Code);
            }
            /// <summary>
            /// Shorten this full Open Location Code by removing four or six digits (depending on the provided reference point).
            /// It removes as many digits as possible.
            /// </summary>
            /// <returns>A new <see cref="ShortCode"/> instance shortened from this Open Location Code.</returns>
            /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
            /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
            /// <exception cref="InvalidOperationException">If this code is padded (<see cref="IsPadded()"/>).</exception>
            /// <exception cref="ArgumentException">If the reference point is too far from this code's center point.</exception>
            public ShortCode Shorten(double referenceLatitude, double referenceLongitude)
            {
                return ShortenValid(Decode(), Code, referenceLatitude, referenceLongitude);
            }
            /// <summary>
            /// Shorten this full Open Location Code by removing four or six digits (depending on the provided reference point).
            /// It removes as many digits as possible.
            /// </summary>
            /// <returns>A new <see cref="ShortCode"/> instance shortened from this Open Location Code.</returns>
            /// <param name="referencePoint">The reference point coordinates</param>
            /// <exception cref="InvalidOperationException">If this code is padded (<see cref="IsPadded()"/>).</exception>
            /// <exception cref="ArgumentException">If the reference point is too far from this code's center point.</exception>
            /// <remarks>Convenient alternative to <see cref="Shorten(double, double)"/></remarks>
            public ShortCode Shorten(GeoPoint referencePoint)
            {
                return Shorten(referencePoint.Latitude, referencePoint.Longitude);
            }
            /// <inheritdoc />
            /// <summary>
            /// Determines whether the provided object is an OpenLocationCode with the same <see cref="Code"/> as this OpenLocationCode.
            /// </summary>
            public override bool Equals(object obj)
            {
                return this == obj || (obj is OpenLocationCode olc && olc.Code == Code);
            }
            /// <returns>The hashcode of the <see cref="Code"/> string.</returns>
            public override int GetHashCode()
            {
                return Code.GetHashCode();
            }
            /// <returns>The <see cref="Code"/> string.</returns>
            public override string ToString()
            {
                return Code;
            }

            // API Spec Implementation

            /// <summary>
            /// Determines if the provided string is a valid Open Location Code sequence.
            /// A valid Open Location Code can be either full or short (XOR).
            /// </summary>
            /// <returns><c>true</c>, if the provided code is a valid Open Location Code, <c>false</c> otherwise.</returns>
            /// <param name="code">The code string to check.</param>
            public static bool IsValid(string code)
            {
                return code != null && IsValidUpperCase(code.ToUpper());
            }
            private static bool IsValidUpperCase(string code)
            {
                if (code.Length < 2)
                {
                    return false;
                }

                // There must be exactly one separator.
                int separatorIndex = code.IndexOf(SeparatorCharacter);
                if (separatorIndex == -1)
                {
                    return false;
                }
                if (separatorIndex != code.LastIndexOf(SeparatorCharacter))
                {
                    return false;
                }
                // There must be an even number of at most eight characters before the separator.
                if (separatorIndex % 2 != 0 || separatorIndex > SeparatorPosition)
                {
                    return false;
                }

                // Check first two characters: only some values from the alphabet are permitted.
                if (separatorIndex == SeparatorPosition)
                {
                    // First latitude character can only have first 9 values.
                    if (CodeAlphabet.IndexOf(code[0]) > FirstLatitudeDigitValueMax)
                    {
                        return false;
                    }

                    // First longitude character can only have first 18 values.
                    if (CodeAlphabet.IndexOf(code[1]) > FirstLongitudeDigitValueMax)
                    {
                        return false;
                    }
                }

                // Check the characters before the separator.
                bool paddingStarted = false;
                for (int i = 0; i < separatorIndex; i++)
                {
                    if (paddingStarted)
                    {
                        // Once padding starts, there must not be anything but padding.
                        if (code[i] != PaddingCharacter)
                        {
                            return false;
                        }
                    }
                    else if (code[i] == PaddingCharacter)
                    {
                        paddingStarted = true;
                        // Short codes cannot have padding
                        if (separatorIndex < SeparatorPosition)
                        {
                            return false;
                        }
                        // Padding can start on even character: 2, 4 or 6.
                        if (i != 2 && i != 4 && i != 6)
                        {
                            return false;
                        }
                    }
                    else if (CodeAlphabet.IndexOf(code[i]) == -1)
                    {
                        return false; // Illegal character.
                    }
                }

                // Check the characters after the separator.
                if (code.Length > separatorIndex + 1)
                {
                    if (paddingStarted)
                    {
                        return false;
                    }
                    // Only one character after separator is forbidden.
                    if (code.Length == separatorIndex + 2)
                    {
                        return false;
                    }
                    for (int i = separatorIndex + 1; i < code.Length; i++)
                    {
                        if (CodeAlphabet.IndexOf(code[i]) == -1)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            /// <summary>
            /// Determines if a code is a valid short Open Location Code.
            /// <para>
            /// A short Open Location Code is a sequence created by removing an even number
            /// of characters from the start of a full Open Location Code. Short codes must
            /// include the separator character and it must be before eight or less characters.
            /// </para>
            /// </summary>
            /// <returns><c>true</c>, if the provided code is a valid short Open Location Code, <c>false</c> otherwise.</returns>
            /// <param name="code">The code string to check.</param>
            public static bool IsShort(string code)
            {
                return IsValid(code) && IsCodeShort(code);
            }
            private static bool IsCodeShort(string code)
            {
                int separatorIndex = code.IndexOf(SeparatorCharacter);
                return separatorIndex >= 0 && separatorIndex < SeparatorPosition;
            }
            /// <summary>
            /// Determines if a code is a valid full Open Location Code.
            /// <para>
            /// Full codes must include the separator character and it must be after eight characters.
            /// </para>
            /// </summary>
            /// <returns><c>true</c>, if the provided code is a valid full Open Location Code, <c>false</c> otherwise.</returns>
            /// <param name="code">The code string to check.</param>
            public static bool IsFull(string code)
            {
                return IsValid(code) && IsCodeFull(code);
            }
            private static bool IsCodeFull(string code)
            {
                return code.IndexOf(SeparatorCharacter) == SeparatorPosition;
            }
            /// <summary>
            /// Determines if a code is a valid padded Open Location Code.
            /// <para>
            /// An Open Location Code is padded when it has only 2, 4, or 6 valid digits
            /// followed by zero <c>'0'</c> as padding to form a full 8 digit code.
            /// If this returns <c>true</c> that the code is padded, then it is also implied
            /// to be full since short codes cannot be padded.
            /// </para>
            /// </summary>
            /// <returns><c>true</c>, if the provided code is a valid padded Open Location Code, <c>false</c> otherwise.</returns>
            /// <param name="code">The code string to check.</param>
            /// <remarks>
            /// This is not apart of the API specification but it is useful to check if a code can
            /// <see cref="Shorten(string, double, double)"/> since padded codes cannot be shortened.
            /// </remarks>
            public static bool IsPadded(string code)
            {
                return IsValid(code) && IsCodePadded(code);
            }
            private static bool IsCodePadded(string code)
            {
                return code.IndexOf(PaddingCharacter) >= 0;
            }
            /// <summary>
            /// Encodes latitude/longitude coordinates into a full Open Location Code of the provided length.
            /// </summary>
            /// <returns>The encoded Open Location Code.</returns>
            /// <param name="latitude">The latitude in decimal degrees.</param>
            /// <param name="longitude">The longitude in decimal degrees.</param>
            /// <param name="codeLength">The number of digits in the code (Default: <see cref="CodePrecisionNormal"/>).</param>
            /// <exception cref="ArgumentException">If the code length is not valid.</exception>
            public static string Encode(double latitude, double longitude, int codeLength = CodePrecisionNormal)
            {
                // Limit the maximum number of digits in the code.
                codeLength = Math.Min(codeLength, MaxDigitCount);
                // Check that the code length requested is valid.
                if (codeLength < 2 || (codeLength < PairCodeLength && codeLength % 2 == 1))
                {
                    throw new ArgumentException($"Illegal code length {codeLength}.");
                }
                // Ensure that latitude and longitude are valid.
                latitude = ClipLatitude(latitude);
                longitude = NormalizeLongitude(longitude);

                // Latitude 90 needs to be adjusted to be just less, so the returned code can also be decoded.
                if ((int)latitude == LatitudeMax)
                {
                    latitude -= 0.9 * ComputeLatitudePrecision(codeLength);
                }

                // Store the code - we build it in reverse and reorder it afterwards.
                StringBuilder reverseCodeBuilder = new StringBuilder();

                // Compute the code.
                // This approach converts each value to an integer after multiplying it by
                // the final precision. This allows us to use only integer operations, so
                // avoiding any accumulation of floating point representation errors.

                // Multiply values by their precision and convert to positive. Rounding
                // avoids/minimises errors due to floating point precision.
                long latVal = (long)(Math.Round((latitude + LatitudeMax) * LatIntegerMultiplier * 1e6) / 1e6);
                long lngVal = (long)(Math.Round((longitude + LongitudeMax) * LngIntegerMultiplier * 1e6) / 1e6);

                if (codeLength > PairCodeLength)
                {
                    for (int i = 0; i < GridCodeLength; i++)
                    {
                        long latDigit = latVal % GridRows;
                        long lngDigit = lngVal % GridColumns;
                        int ndx = (int)(latDigit * GridColumns + lngDigit);
                        reverseCodeBuilder.Append(CodeAlphabet[ndx]);
                        latVal /= GridRows;
                        lngVal /= GridColumns;
                    }
                }
                else
                {
                    latVal /= GridRowsMultiplier;
                    lngVal /= GridColumnsMultiplier;
                }
                // Compute the pair section of the code.
                for (int i = 0; i < PairCodeLength / 2; i++)
                {
                    reverseCodeBuilder.Append(CodeAlphabet[(int)(lngVal % EncodingBase)]);
                    reverseCodeBuilder.Append(CodeAlphabet[(int)(latVal % EncodingBase)]);
                    latVal /= EncodingBase;
                    lngVal /= EncodingBase;
                    // If we are at the separator position, add the separator.
                    if (i == 0)
                    {
                        reverseCodeBuilder.Append(SeparatorCharacter);
                    }
                }
                // Reverse the code.
                char[] reversedCode = reverseCodeBuilder.ToString().ToCharArray();
                Array.Reverse(reversedCode);
                StringBuilder codeBuilder = new StringBuilder(new string(reversedCode));

                // If we need to pad the code, replace some of the digits.
                if (codeLength < SeparatorPosition)
                {
                    codeBuilder.Remove(codeLength, SeparatorPosition - codeLength);
                    for (int i = codeLength; i < SeparatorPosition; i++)
                    {
                        codeBuilder.Insert(i, PaddingCharacter);
                    }
                }
                return codeBuilder.ToString(0, Math.Max(SeparatorPosition + 1, codeLength + 1));
            }
            /// <summary>
            /// Encodes geographic point coordinates into a full Open Location Code of the provided length.
            /// </summary>
            /// <returns>The encoded Open Location Code.</returns>
            /// <param name="point">The geographic point coordinates.</param>
            /// <param name="codeLength">The number of digits in the code (Default: <see cref="CodePrecisionNormal"/>).</param>
            /// <exception cref="ArgumentException">If the code length is not valid.</exception>
            /// <remarks>Alternative too <see cref="Encode(double, double, int)"/></remarks>
            public static string Encode(GeoPoint point, int codeLength = CodePrecisionNormal)
            {
                return Encode(point.Latitude, point.Longitude, codeLength);
            }
            /// <summary>
            /// Decodes a full Open Location Code into a <see cref="CodeArea"/> object
            /// encapsulating the latitude/longitude coordinates of the area bounding box.
            /// </summary>
            /// <returns>The decoded CodeArea for the given location code.</returns>
            /// <param name="code">The Open Location Code to be decoded.</param>
            /// <exception cref="ArgumentException">If the code is not valid or not full.</exception>
            public static CodeArea Decode(string code)
            {
                code = ValidateCode(code);
                if (!IsCodeFull(code))
                {
                    throw new ArgumentException($"{nameof(Decode)}(code: {code}) - code cannot be short.");
                }
                return DecodeValid(TrimCode(code));
            }
            private static CodeArea DecodeValid(string codeDigits)
            {
                // Initialise the values. We work them out as integers and convert them to doubles at the end.
                long latVal = -LatitudeMax * LatIntegerMultiplier;
                long lngVal = -LongitudeMax * LngIntegerMultiplier;
                // Define the place value for the digits. We'll divide this down as we work through the code.
                long latPlaceVal = LatMspValue;
                long lngPlaceVal = LngMspValue;

                int pairPartLength = Math.Min(codeDigits.Length, PairCodeLength);
                int codeLength = Math.Min(codeDigits.Length, MaxDigitCount);
                for (int i = 0; i < pairPartLength; i += 2)
                {
                    latPlaceVal /= EncodingBase;
                    lngPlaceVal /= EncodingBase;
                    latVal += DigitValueOf(codeDigits[i]) * latPlaceVal;
                    lngVal += DigitValueOf(codeDigits[i + 1]) * lngPlaceVal;
                }
                for (int i = PairCodeLength; i < codeLength; i++)
                {
                    latPlaceVal /= GridRows;
                    lngPlaceVal /= GridColumns;
                    int digit = DigitValueOf(codeDigits[i]);
                    int row = digit / GridColumns;
                    int col = digit % GridColumns;
                    latVal += row * latPlaceVal;
                    lngVal += col * lngPlaceVal;
                }
                return new CodeArea(
                    (double)latVal / LatIntegerMultiplier,
                    (double)lngVal / LngIntegerMultiplier,
                    (double)(latVal + latPlaceVal) / LatIntegerMultiplier,
                    (double)(lngVal + lngPlaceVal) / LngIntegerMultiplier,
                    codeLength
                );
            }
            /// <summary>
            /// Shorten a full Open Location Code by removing four or six digits (depending on the provided reference point).
            /// It removes as many digits as possible.
            /// </summary>
            /// <returns>A new <see cref="ShortCode"/> instance shortened from the the provided Open Location Code.</returns>
            /// <param name="code">The Open Location Code to shorten.</param>
            /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
            /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
            /// <exception cref="ArgumentException">If the code is not valid, not full, or is padded.</exception>
            /// <exception cref="ArgumentException">If the reference point is too far from the code's center point.</exception>
            public static ShortCode Shorten(string code, double referenceLatitude, double referenceLongitude)
            {
                code = ValidateCode(code);
                if (!IsCodeFull(code))
                {
                    throw new ArgumentException($"{nameof(Shorten)}(code: \"{code}\") - code cannot be short.");
                }
                if (IsCodePadded(code))
                {
                    throw new ArgumentException($"{nameof(Shorten)}(code: \"{code}\") - code cannot be padded.");
                }
                return ShortenValid(Decode(code), code, referenceLatitude, referenceLongitude);
            }
            private static ShortCode ShortenValid(CodeArea codeArea, string code, double referenceLatitude, double referenceLongitude)
            {
                GeoPoint center = codeArea.Center;
                double range = Math.Max(
                    Math.Abs(referenceLatitude - center.Latitude),
                    Math.Abs(referenceLongitude - center.Longitude)
                );
                // We are going to check to see if we can remove three pairs, two pairs or just one pair of
                // digits from the code.
                for (int i = 4; i >= 1; i--)
                {
                    // Check if we're close enough to shorten. The range must be less than 1/2
                    // the precision to shorten at all, and we want to allow some safety, so
                    // use 0.3 instead of 0.5 as a multiplier.
                    if (range < (ComputeLatitudePrecision(i * 2) * 0.3))
                    {
                        // We're done.
                        return new ShortCode(code.Substring(i * 2), valid: true);
                    }
                }
                throw new ArgumentException("Reference location is too far from the Open Location Code center.");
            }
            private static string ValidateCode(string code)
            {
                if (code == null)
                {
                    throw new ArgumentException("code cannot be null");
                }
                code = code.ToUpper();
                if (!IsValidUpperCase(code))
                {
                    throw new ArgumentException($"code '{code}' is not a valid Open Location Code.");
                }
                return code;
            }

            // Private static utility methods.

            internal static int DigitValueOf(char digitChar)
            {
                return IndexedDigitValues[digitChar - IndexedDigitValueOffset];
            }
            private static double ClipLatitude(double latitude)
            {
                return Math.Min(Math.Max(latitude, -LatitudeMax), LatitudeMax);
            }
            private static double NormalizeLongitude(double longitude)
            {
                while (longitude < -LongitudeMax)
                {
                    longitude += LongitudeMax * 2;
                }
                while (longitude >= LongitudeMax)
                {
                    longitude -= LongitudeMax * 2;
                }
                return longitude;
            }
            /// <summary>
            /// Normalize a location code by adding the separator '+' character and any padding '0' characters
            /// that are necessary to form a valid location code.
            /// </summary>
            private static string NormalizeCode(string code)
            {
                if (code.Length < SeparatorPosition)
                {
                    return code + new string(PaddingCharacter, SeparatorPosition - code.Length) + SeparatorCharacter;
                }
                else if (code.Length == SeparatorPosition)
                {
                    return code + SeparatorCharacter;
                }
                else if (code[SeparatorPosition] != SeparatorCharacter)
                {
                    return code.Substring(0, SeparatorPosition) + SeparatorCharacter + code.Substring(SeparatorPosition);
                }
                return code;
            }
            /// <summary>
            /// Trim a location code by removing the separator '+' character and any padding '0' characters
            /// resulting in only the code digits.
            /// </summary>
            internal static string TrimCode(string code)
            {
                StringBuilder codeBuilder = new StringBuilder();
                foreach (char c in code)
                {
                    if (c != PaddingCharacter && c != SeparatorCharacter)
                    {
                        codeBuilder.Append(c);
                    }
                }
                return codeBuilder.Length != code.Length ? codeBuilder.ToString() : code;
            }
            /// <summary>
            /// Compute the latitude precision value for a given code length. Lengths &lt;= 10 have the same
            /// precision for latitude and longitude, but lengths > 10 have different precisions due to the
            /// grid method having fewer columns than rows.
            /// </summary>
            /// <remarks>Copied from the JS implementation.</remarks>
            private static double ComputeLatitudePrecision(int codeLength)
            {
                if (codeLength <= CodePrecisionNormal)
                {
                    return Math.Pow(EncodingBase, codeLength / -2 + 2);
                }
                return Math.Pow(EncodingBase, -3) / Math.Pow(GridRows, codeLength - PairCodeLength);
            }
            /// <summary>
            /// A class representing a short Open Location Code which is defined by <see cref="IsShort(string)"/>.
            /// <para>
            /// A ShortCode instance can be created the following ways:
            /// <list type="bullet">
            /// <item><see cref="Shorten(double, double)"/> - Shorten a full Open Location Code</item>
            /// <item><see cref="ShortCode(string)"/> - Construct for a valid short Open Location Code</item>
            /// </list>
            /// </para>
            /// A ShortCode can be recovered back to a full Open Location Code using <see cref="RecoverNearest(double, double)"/>
            /// or using the static method <see cref="RecoverNearest(string, double, double)"/> (as defined by the spec).
            /// </summary>
            public class ShortCode
            {

                /// <summary>
                /// Creates a <see cref="ShortCode"/> object for the provided short Open Location Code.
                /// Use <see cref="OpenLocationCode(string)"/> for full codes.
                /// </summary>
                /// <param name="shortCode">A valid short Open Location Code.</param>
                /// <exception cref="ArgumentException">If the code is null, not valid, or not short.</exception>
                public ShortCode(string shortCode)
                {
                    Code = ValidateShortCode(ValidateCode(shortCode));
                }

                // Used internally for short codes which are guaranteed to be valid
                // ReSharper disable once UnusedParameter.Local - because public constructor 
                internal ShortCode(string shortCode, bool valid)
                {
                    Code = shortCode;
                }

                /// <summary>
                /// The code which is a valid short Open Location Code (plus code).
                /// </summary>
                /// <example>9QCJ+2VX</example>
                /// <value>The string representation of the short code.</value>
                public string Code { get; }


                /// <returns>
                /// A new OpenLocationCode instance representing a full Open Location Code
                /// recovered from this (short) Open Location Code, given the reference location.
                /// </returns>
                /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
                /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
                public OpenLocationCode RecoverNearest(double referenceLatitude, double referenceLongitude)
                {
                    return RecoverNearestValid(Code, referenceLatitude, referenceLongitude);
                }
                /// <inheritdoc />
                /// <summary>
                /// Determines whether the provided object is a ShortCode with the same <see cref="Code"/> as this ShortCode.
                /// </summary>
                public override bool Equals(object obj)
                {
                    return obj == this || (obj is ShortCode shortCode && shortCode.Code == Code);
                }
                /// <returns>The hashcode of the <see cref="Code"/> string.</returns>
                public override int GetHashCode()
                {
                    return Code.GetHashCode();
                }
                /// <returns>The <see cref="Code"/> string.</returns>
                public override string ToString()
                {
                    return Code;
                }
                /// <remarks>
                /// Note: if shortCode is already a valid full code,
                /// this will immediately return a new OpenLocationCode instance with that code
                /// </remarks>
                /// <returns>
                /// A new OpenLocationCode instance representing a full Open Location Code
                /// recovered from the provided short Open Location Code, given the reference location.
                /// </returns>
                /// <param name="shortCode">The valid short Open Location Code to recover</param>
                /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
                /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
                /// <exception cref="ArgumentException">If the code is null or not valid.</exception>
                public static OpenLocationCode RecoverNearest(string shortCode, double referenceLatitude, double referenceLongitude)
                {
                    string validCode = ValidateCode(shortCode);
                    if (IsCodeFull(validCode)) return new OpenLocationCode(validCode);

                    return RecoverNearestValid(ValidateShortCode(validCode), referenceLatitude, referenceLongitude);
                }
                private static OpenLocationCode RecoverNearestValid(string shortCode, double referenceLatitude, double referenceLongitude)
                {
                    referenceLatitude = ClipLatitude(referenceLatitude);
                    referenceLongitude = NormalizeLongitude(referenceLongitude);

                    int digitsToRecover = SeparatorPosition - shortCode.IndexOf(SeparatorCharacter);
                    // The precision (height and width) of the missing prefix in degrees.
                    double prefixPrecision = Math.Pow(EncodingBase, 2 - (digitsToRecover / 2));

                    // Use the reference location to generate the prefix.
                    string recoveredPrefix =
                        new OpenLocationCode(referenceLatitude, referenceLongitude).Code.Substring(0, digitsToRecover);
                    // Combine the prefix with the short code and decode it.
                    OpenLocationCode recovered = new OpenLocationCode(recoveredPrefix + shortCode);
                    GeoPoint recoveredCodeAreaCenter = recovered.Decode().Center;
                    // Work out whether the new code area is too far from the reference location. If it is, we
                    // move it. It can only be out by a single precision step.
                    double recoveredLatitude = recoveredCodeAreaCenter.Latitude;
                    double recoveredLongitude = recoveredCodeAreaCenter.Longitude;

                    // Move the recovered latitude by one precision up or down if it is too far from the reference,
                    // unless doing so would lead to an invalid latitude.
                    double latitudeDiff = recoveredLatitude - referenceLatitude;
                    if (latitudeDiff > prefixPrecision / 2 && recoveredLatitude - prefixPrecision > -LatitudeMax)
                    {
                        recoveredLatitude -= prefixPrecision;
                    }
                    else if (latitudeDiff < -prefixPrecision / 2 && recoveredLatitude + prefixPrecision < LatitudeMax)
                    {
                        recoveredLatitude += prefixPrecision;
                    }

                    // Move the recovered longitude by one precision up or down if it is too far from the reference.
                    double longitudeDiff = recoveredCodeAreaCenter.Longitude - referenceLongitude;
                    if (longitudeDiff > prefixPrecision / 2)
                    {
                        recoveredLongitude -= prefixPrecision;
                    }
                    else if (longitudeDiff < -prefixPrecision / 2)
                    {
                        recoveredLongitude += prefixPrecision;
                    }

                    return new OpenLocationCode(recoveredLatitude, recoveredLongitude, recovered.CodeDigits.Length);
                }
                private static string ValidateShortCode(string shortCode)
                {
                    if (!IsCodeShort(shortCode))
                    {
                        throw new ArgumentException($"code '{shortCode}' is not a valid short Open Location Code.");
                    }
                    return shortCode;
                }

            }
        }
        #endregion
        #region SubClasses
        /// <summary>
        /// A square <see cref="GeoArea"/> for the coordinates of a decoded Open Location Code area.
        /// The <see cref="CodeLength"/> of the decoded Open Location Code is also included.
        /// </summary>
        protected class CodeArea : GeoArea
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="southLatitude"></param>
            /// <param name="westLongitude"></param>
            /// <param name="northLatitude"></param>
            /// <param name="eastLongitude"></param>
            /// <param name="codeLength"></param>
            /// <exception cref="ArgumentException"></exception>
            internal CodeArea(double southLatitude, double westLongitude, double northLatitude, double eastLongitude, int codeLength) :
                base(southLatitude, westLongitude, northLatitude, eastLongitude)
            {
                if (southLatitude >= northLatitude || westLongitude >= eastLongitude)
                {
                    throw new ArgumentException("min must be less than max");
                }

                CodeLength = codeLength;
            }
            /// <summary>
            /// Create a new copy of the provided CodeArea
            /// </summary>
            /// <param name="other">The other CodeArea to copy</param>
            public CodeArea(CodeArea other) : base(other)
            {
                CodeLength = other.CodeLength;
            }
            /// <summary>
            /// The length of the decoded Open Location Code.
            /// </summary>
            public int CodeLength { get; }
        }
        /// <summary>
        /// A rectangular area on the geographic coordinate system specified by the minimum and maximum <see cref="GeoPoint"/> coordinates.
        /// The coordinates include the latitude and longitude of the lower left (south west) and upper right (north east) corners.
        /// <para>
        /// Additional properties exist to calculate the <see cref="Center"/> of the bounding box,
        /// and the <see cref="LatitudeHeight"/> or <see cref="LongitudeWidth"/> area dimensions in degrees.
        /// </para>
        /// </summary>
        protected class GeoArea
        {
            /// <summary>
            /// Create a new rectangular GeoArea of the provided min and max geo points.
            /// </summary>
            /// <param name="min">The minimum GeoPoint</param>
            /// <param name="max">The maximum GeoPoint</param>
            /// <exception cref="ArgumentException">If min is greater than or equal to max.</exception>
            public GeoArea(GeoPoint min, GeoPoint max)
            {
                if (min.Latitude >= max.Latitude || min.Longitude >= max.Longitude)
                {
                    throw new ArgumentException("min must be less than max");
                }
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Create a new rectangular GeoArea of the provided min and max geo coordinates.
            /// </summary>
            /// <param name="southLatitude">The minimum south latitude</param>
            /// <param name="westLongitude">The minimum west longitude</param>
            /// <param name="northLatitude">The maximum north latitude</param>
            /// <param name="eastLongitude">The maximum east longitude</param>
            public GeoArea(double southLatitude, double westLongitude, double northLatitude, double eastLongitude) :
                this(new GeoPoint(southLatitude, westLongitude), new GeoPoint(northLatitude, eastLongitude))
            { }
            /// <summary>
            /// Create a new copy of the provided GeoArea
            /// </summary>
            /// <param name="other">The other GeoArea to copy</param>
            public GeoArea(GeoArea other) : this(other.Min, other.Max) { }
            /// <summary>
            /// The min (south west) point coordinates of the area bounds.
            /// </summary>
            public GeoPoint Min { get; }
            /// <summary>
            /// The max (north east) point coordinates of the area bounds.
            /// </summary>
            public GeoPoint Max { get; }
            /// <summary>
            /// The center point of the area which is equidistant between <see cref="Min"/> and <see cref="Max"/>.
            /// </summary>
            public GeoPoint Center => new GeoPoint(CenterLatitude, CenterLongitude);
            /// <summary>
            /// The width of the area in longitude degrees.
            /// </summary>
            public double LongitudeWidth => (double)((decimal)Max.Longitude - (decimal)Min.Longitude);
            /// <summary>
            /// The height of the area in latitude degrees.
            /// </summary>
            public double LatitudeHeight => (double)((decimal)Max.Latitude - (decimal)Min.Latitude);
            /// <summary>The south (min) latitude coordinate in decimal degrees.</summary>
            /// <remarks>Alias to <see cref="Min"/>.<see cref="GeoPoint.Latitude">Latitude</see></remarks>
            public double SouthLatitude => Min.Latitude;
            /// <summary>The west (min) longitude coordinate in decimal degrees.</summary>
            /// <remarks>Alias to <see cref="Min"/>.<see cref="GeoPoint.Longitude">Longitude</see></remarks>
            public double WestLongitude => Min.Longitude;
            /// <summary>The north (max) latitude coordinate in decimal degrees.</summary>
            /// <remarks>Alias to <see cref="Max"/>.<see cref="GeoPoint.Latitude">Latitude</see></remarks>
            public double NorthLatitude => Max.Latitude;
            /// <summary>The east (max) longitude coordinate in decimal degrees.</summary>
            /// <remarks>Alias to <see cref="Max"/>.<see cref="GeoPoint.Longitude">Longitude</see></remarks>
            public double EastLongitude => Max.Longitude;
            /// <summary>The center latitude coordinate in decimal degrees.</summary>
            /// <remarks>Alias to <see cref="Center"/>.<see cref="GeoPoint.Latitude">Latitude</see></remarks>
            public double CenterLatitude => (Min.Latitude + Max.Latitude) / 2;
            /// <summary>The center longitude coordinate in decimal degrees.</summary>
            /// <remarks>Alias to <see cref="Center"/>.<see cref="GeoPoint.Longitude">Longitude</see></remarks>
            public double CenterLongitude => (Min.Longitude + Max.Longitude) / 2;
            /// <returns><c>true</c> if this geo area contains the provided point, <c>false</c> otherwise.</returns>
            /// <param name="point">The point coordinates to check.</param>
            public bool Contains(GeoPoint point)
            {
                return Contains(point.Latitude, point.Longitude);
            }
            /// <returns><c>true</c> if this geo area contains the provided point, <c>false</c> otherwise.</returns>
            /// <param name="latitude">The latitude coordinate of the point to check.</param>
            /// <param name="longitude">The longitude coordinate of the point to check.</param>
            public bool Contains(double latitude, double longitude)
            {
                return Min.Latitude <= latitude && latitude < Max.Latitude
                    && Min.Longitude <= longitude && longitude < Max.Longitude;
            }
        }
        /// <summary>
        /// A point on the geographic coordinate system specified by latitude and longitude coordinates in degrees.
        /// </summary>
        public struct GeoPoint : IEquatable<GeoPoint>
        {
            /// <param name="latitude">The latitude coordinate in decimal degrees.</param>
            /// <param name="longitude">The longitude coordinate in decimal degrees.</param>
            /// <exception cref="ArgumentException">If latitude is out of range -90 to 90.</exception>
            /// <exception cref="ArgumentException">If longitude is out of range -180 to 180.</exception>
            public GeoPoint(double latitude, double longitude)
            {
                if (latitude < -90 || latitude > 90) throw new ArgumentException("latitude is out of range -90 to 90");
                if (longitude < -180 || longitude > 180) throw new ArgumentException("longitude is out of range -180 to 180");

                Latitude = latitude;
                Longitude = longitude;
            }
            /// <summary>
            /// The latitude coordinate in decimal degrees (y axis).
            /// </summary>
            public double Latitude { get; }
            /// <summary>
            /// The longitude coordinate in decimal degrees (x axis).
            /// </summary>
            public double Longitude { get; }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns>A human readable representation of this GeoPoint coordinates.</returns>
            public override string ToString() => $"[Latitude:{Latitude},Longitude:{Longitude}]";
            /// <summary>
            /// Hashcode
            /// </summary>
            /// <returns>The hash code for this GeoPoint coordinates.</returns>
            public override int GetHashCode() => Latitude.GetHashCode() ^ Longitude.GetHashCode();
            /// <inheritdoc />
            /// <summary>
            /// Determines whether the provided object is a GeoPoint with the same
            /// <see cref="Latitude"/> and <see cref="Longitude"/> as this GeoPoint.
            /// </summary>
            public override bool Equals(object obj) => obj is GeoPoint coord && Equals(coord);
            /// <inheritdoc />
            /// <summary>
            /// Determines whether the provided GoePoint has the same
            /// <see cref="Latitude"/> and <see cref="Longitude"/> as this GeoPoint.
            /// </summary>
            public bool Equals(GeoPoint other) => this == other;
            /// <summary>
            /// Equality comparison of 2 GeoPoint coordinates.
            /// </summary>
            public static bool operator ==(GeoPoint a, GeoPoint b) => a.Latitude == b.Latitude && a.Longitude == b.Longitude;
            /// <summary>
            /// Inequality comparison of 2 Geopoint coordinates.
            /// </summary>
            public static bool operator !=(GeoPoint a, GeoPoint b) => !(a == b);
        }
        #endregion
    }
    #endregion
    #region struct PointD a PointM : souřadnice bodu Double a Decimal
    /// <summary>
    /// Souřadnice bodu Double
    /// </summary>
    public struct PointD
    {
        /// <summary>
        /// Represents a point that has point.X and point.Y //     values set to zero.
        /// </summary>
        public static readonly PointD Empty = new PointD(0d, 0d);
        private Double __X;
        private Double __Y;
        /// <summary>
        /// Gets a value indicating whether this point is empty.
        /// </summary>
        public bool IsEmpty { get { return __X == 0d && __Y == 0d; } }
        /// <summary>
        /// Gets or sets the X-coordinate of this point.
        /// </summary>
        public Double X { get { return __X; } set { __X = value; } }
        /// <summary>
        /// Gets or sets the Y-coordinate of this point.
        /// </summary>
        public Double Y { get { return __Y; } set { __Y = value; } }
        /// <summary>
        /// Initializes a new instance of the point class with the specified coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public PointD(Double x, Double y)
        {
            this.__X = x;
            this.__Y = y;
        }
        /// <summary>
        /// Converts the specified point structure to a <see cref="PointD"/> structure.
        /// </summary>
        /// <param name="p"></param>
        public static implicit operator PointD(System.Drawing.Point p)
        {
            return new PointD(p.X, p.Y);
        }
        /// <summary>
        /// Compares two point objects. The result specifies whether the values of the point.X and point.Y properties of the two point objects are equal.
        /// </summary>
        /// <param name="left">A point to compare.</param>
        /// <param name="right">A point to compare.</param>
        /// <returns></returns>
        public static bool operator ==(PointD left, PointD right)
        {
            if (left.X == right.X)
            {
                return left.Y == right.Y;
            }

            return false;
        }
        /// <summary>
        /// Compares two point objects. The result specifies whether the values of the point.X or point.Y properties of the two point objects are unequal.
        /// </summary>
        /// <param name="left">A point to compare.</param>
        /// <param name="right">A point to compare.</param>
        /// <returns>true if the values of either the point.X properties or the point.Y properties of left and right differ; otherwise, false.</returns>
        public static bool operator !=(PointD left, PointD right)
        {
            return !(left == right);
        }
        /// <summary>
        /// Specifies whether this point contains the same coordinates as the specified System.Object.
        /// </summary>
        /// <param name="obj">The System.Object to test.</param>
        /// <returns>true if obj is a point and has the same coordinates as this point.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PointD point))
            {
                return false;
            }

            if (point.X == X)
            {
                return point.Y == Y;
            }

            return false;
        }
        /// <summary>
        /// Returns a hash code for this point.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this point.</returns>
        public override int GetHashCode()
        {
            return __X.GetHashCode() ^ __Y.GetHashCode();
        }
        /// <summary>
        /// Translates this point by the specified amount.
        /// </summary>
        /// <param name="dx">The amount to offset the x-coordinate.</param>
        /// <param name="dy">The amount to offset the y-coordinate.</param>
        public void Offset(Double dx, Double dy)
        {
            X += dx;
            Y += dy;
        }
        /// <summary>
        /// Translates this point by the specified point.
        /// </summary>
        /// <param name="p">The point used offset this point.</param>
        public void Offset(PointD p)
        {
            Offset(p.X, p.Y);
        }
        /// <summary>
        /// Converts this point to a human-readable string.
        /// </summary>
        /// <returns>A string that represents this point.</returns>
        public override string ToString()
        {
            return "{ X=" + X.ToString(System.Globalization.CultureInfo.CurrentCulture) + "; Y=" + Y.ToString(System.Globalization.CultureInfo.CurrentCulture) + " }";
        }
        /// <summary>
        /// Fixní stringový výraz obsahující souřadnice X i Y, pro jednoznačné srovnání
        /// </summary>
        public string Text { get { return $"X: {MapProviderBase.FormatDouble(this.X)}; Y: {MapProviderBase.FormatDouble(this.Y)}"; } }
    }
    /// <summary>
    /// Souřadnice bodu Decimal
    /// </summary>
    public struct PointM
    {
        /// <summary>
        /// Represents a point that has point.X and point.Y //     values set to zero.
        /// </summary>
        public static readonly PointM Empty = new PointM(0m, 0m);
        private decimal __X;
        private decimal __Y;
        /// <summary>
        /// Gets a value indicating whether this point is empty.
        /// </summary>
        public bool IsEmpty { get { return __X == 0m && __Y == 0m; } }
        /// <summary>
        /// Gets or sets the X-coordinate of this point.
        /// </summary>
        public decimal X { get { return __X; } set { __X = value; } }
        /// <summary>
        /// Gets or sets the Y-coordinate of this point.
        /// </summary>
        public decimal Y { get { return __Y; } set { __Y = value; } }
        /// <summary>
        /// Initializes a new instance of the point class with the specified coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public PointM(decimal x, decimal y)
        {
            this.__X = x;
            this.__Y = y;
        }
        /// <summary>
        /// Converts the specified point structure to a <see cref="PointM"/> structure.
        /// </summary>
        /// <param name="p"></param>
        public static implicit operator PointM(System.Drawing.Point p)
        {
            return new PointM(p.X, p.Y);
        }
        /// <summary>
        /// Compares two point objects. The result specifies whether the values of the point.X and point.Y properties of the two point objects are equal.
        /// </summary>
        /// <param name="left">A point to compare.</param>
        /// <param name="right">A point to compare.</param>
        /// <returns></returns>
        public static bool operator ==(PointM left, PointM right)
        {
            if (left.X == right.X)
            {
                return left.Y == right.Y;
            }

            return false;
        }
        /// <summary>
        /// Compares two point objects. The result specifies whether the values of the point.X or point.Y properties of the two point objects are unequal.
        /// </summary>
        /// <param name="left">A point to compare.</param>
        /// <param name="right">A point to compare.</param>
        /// <returns>true if the values of either the point.X properties or the point.Y properties of left and right differ; otherwise, false.</returns>
        public static bool operator !=(PointM left, PointM right)
        {
            return !(left == right);
        }
        /// <summary>
        /// Specifies whether this point contains the same coordinates as the specified System.Object.
        /// </summary>
        /// <param name="obj">The System.Object to test.</param>
        /// <returns>true if obj is a point and has the same coordinates as this point.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PointM point))
            {
                return false;
            }

            if (point.X == X)
            {
                return point.Y == Y;
            }

            return false;
        }
        /// <summary>
        /// Returns a hash code for this point.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this point.</returns>
        public override int GetHashCode()
        {
            return __X.GetHashCode() ^ __Y.GetHashCode();
        }
        /// <summary>
        /// Translates this point by the specified amount.
        /// </summary>
        /// <param name="dx">The amount to offset the x-coordinate.</param>
        /// <param name="dy">The amount to offset the y-coordinate.</param>
        public void Offset(decimal dx, decimal dy)
        {
            X += dx;
            Y += dy;
        }
        /// <summary>
        /// Translates this point by the specified point.
        /// </summary>
        /// <param name="p">The point used offset this point.</param>
        public void Offset(PointM p)
        {
            Offset(p.X, p.Y);
        }
        /// <summary>
        /// Converts this point to a human-readable string.
        /// </summary>
        /// <returns>A string that represents this point.</returns>
        public override string ToString()
        {
            return "{ X=" + X.ToString(System.Globalization.CultureInfo.CurrentCulture) + "; Y=" + Y.ToString(System.Globalization.CultureInfo.CurrentCulture) + " }";
        }
        /// <summary>
        /// Fixní stringový výraz obsahující souřadnice X i Y, pro jednoznačné srovnání
        /// </summary>
        public string Text { get { return $"X: {MapProviderBase.FormatDecimal(this.X)}; Y: {MapProviderBase.FormatDecimal(this.Y)}"; } }
    }
    #endregion
}
