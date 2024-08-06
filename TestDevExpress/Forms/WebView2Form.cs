using Noris.Clients.Win.Components.AsolDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Web viewer typu MS WebView2
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "MS WebView2", buttonOrder: 71, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře MS WebView2 prohlížeč (MS Edge based)", tabViewToolTip: "WebView2 Browser")]
    internal class WebView2Form : DxRibbonForm
    {
        #region Konstrukce

        protected override void DxMainContentPrepare()
        {
            __DxMainPadding = new Padding(9, 6, 9, 6);
            int x = __DxMainPadding.Left;
            int y = __DxMainPadding.Top;
            int w = 140;
            int h = 32;
            int s = 3;
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "seznam", _ClickButton, tag: "https://www.seznam.cz/");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "mapy A", _ClickButton, tag: "https://www.mapy.cz/");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "mapy B", _ClickButton, tag: @"https://mapy.cz/dopravni?x=14.5802973&y=50.5311090&z=14");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "mapy C", _ClickButton, tag: @"https://mapy.cz/dopravni?l=0&x=15.8629028&y=50.2145999&z=17");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "mapy D", _ClickButton, tag: @"https://mapy.cz/dopravni?vlastni-body&ut=Nový bod&uc=9kFczxY5mZ&ud=15°51%2742.665""E 50°12%2754.179""N&x=15.8629028&y=50.2145999&z=17");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "google", _ClickButton, tag: "https://www.google.com/");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "meteo", _ClickButton, tag: "https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/short.html?display=var&gmap_zoom=7&prod=czrad_maxz_celdn_masked&opa1=0.6&opa2=0.7&nselect=14&nselect_fct=6&di=1&rep=2&add=4&update=4&lat=49.951&lon=15.797&lang=CZ");
            x += (w + s);
            DxComponent.CreateDxSimpleButton(x, y, w, h, this.DxMainPanel, "GreenMapa", _ClickButton, tag: "<GreenMapa>");
            x += (w + s);

            __WebPanelLocation = new Point(__DxMainPadding.Left, __DxMainPadding.Top + h + s);
            __WebViewPanel = new DxWebViewPanel();

            this.DxMainPanel.Controls.Add(__WebViewPanel);
        }
        private Padding __DxMainPadding;
        private Point __WebPanelLocation;
        private DxWebViewPanel __WebViewPanel;
        /// <summary>
        /// Provede se po změně velikosti ClientSize panelu <see cref="DxRibbonForm.DxMainPanel"/>
        /// </summary>
        protected override void DxMainContentDoLayout()
        {
            var webPanel = __WebViewPanel;
            if (webPanel is null) return;

            var clientSize = this.DxMainPanel.ClientSize;
            int x = __WebPanelLocation.X;
            int y = __WebPanelLocation.Y;
            int w = clientSize.Width - __DxMainPadding.Right - x;
            int h = clientSize.Height - __DxMainPadding.Bottom - y;
            webPanel.Bounds = new Rectangle(x, y, w, h);
        }

        private void _ClickButton(object sender, EventArgs e)
        {
            if (sender is Control control && control.Tag is string text)
            {
                if (text == "<GreenMapa>")
                {
                    string html = @"<!DOCTYPE html>
<html>
<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=10"" />
<meta charset=""utf-8"" />
<script type=""text/javascript"" src=""http://api.mapy.cz/loader.js""></script>
<script type=""text/javascript"">Loader.load();</script>
</head>
<body style=""padding: 0; margin: 0"">
<div id=""m"" style=""padding: 0; margin: 0;height:441px;""></div>
</body>
<script type=""text/javascript"">
function afterMapLoaded()
{
window.external.callClientEvent(""AfterMapLoaded"","""");
}
var mapSignalsCallback = function(e) {
afterMapLoaded();
}
var center = SMap.Coords.fromWGS84(15.3351975,49.7420097);
var m = new SMap(JAK.gel(""m""), center, 7);
var mapSignals = m.getSignals();
mapSignals.addListener(null, ""tileset-load"", mapSignalsCallback);
m.addDefaultLayer(SMap.DEF_BASE).enable();
layer = new SMap.Layer.Marker();
var layerId = layer.getId();
m.addLayer(layer);
var sync = new SMap.Control.Sync({});
m.addControl(sync);
</script>
</html>
";
                }
                else
                {
                    // _DoNavigate(text);
                }
            }
        }


        #endregion
        
        /*
       
        private void _RefreshUrl(string uri)
        {
            _NavigatedUri = uri;
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(_RefreshUrlText));
            else
                _RefreshUrlText();
        }
        private void _RefreshUrlText()
        {
            this._UrlAdress.Text = _NavigatedUri;
        }
        private string _NavigatedUri;
       
        private Microsoft.Web.WebView2.WinForms.WebView2 _WView;
        private void _PrepareWView()
        {
            if (_WView != null && Control.ModifierKeys == Keys.Control)
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.Dispose...");
                DxComponent.TryRun(() => _WView.Dispose(), true);
                _WView = null;
            }

            if (this._WView != null)
            {
                _DoLayout();
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.Exists.");
                return;
            }

            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.Creating new instance...");
            this._WView = new Microsoft.Web.WebView2.WinForms.WebView2();

            ((System.ComponentModel.ISupportInitialize)(this._WView)).BeginInit();
            this.Controls.Add(this._WView);
            this._WView.Location = new System.Drawing.Point(12, 49);
            this._WView.Name = "_WebView2";
            this._WView.Size = new System.Drawing.Size(776, 389);
            this._WView.TabIndex = 0;

            //this._WView.OnNavigationStarting += _AxAntView_OnNavigationStarting;
            //this._WView.OnFrameNavigationStarting += _AxAntView_OnFrameNavigationStarting;
            //this._WView.OnFrameNavigationCompleted += _AxAntView_OnFrameNavigationCompleted;
            //this._WView.OnNavigationCompleted += _AxAntView_OnNavigationCompleted;
            //this._WView.OnSourceChanged += _AxAntView_OnSourceChanged;

            ((System.ComponentModel.ISupportInitialize)(this._WView)).EndInit();

            _DoLayout();

            this.__SourceAsync = null;
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.EnsureCoreWebView2Async...");
            this._WView.CoreWebView2InitializationCompleted += _CoreWebView2InitializationCompleted;
            var task =  this._WView.EnsureCoreWebView2Async();
            task.Wait(50);
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.EnsureCoreWebView2Async Wait done; Status: '{task.Status}'");
        }

        private void _CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (this._WView.CoreWebView2 is null)
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.CoreWebView2InitializationCompleted: IS NULL!");

                // this._WView.Reload();
            }
            if (this._WView.CoreWebView2 != null)
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.CoreWebView2InitializationCompleted: Set eventhandlers...");

                this._WView.CoreWebView2.SourceChanged += _WebView2_SourceChanged;
                this._WView.CoreWebView2.StatusBarTextChanged += CoreWebView2_StatusBarTextChanged;

                if (!String.IsNullOrEmpty(__SourceAsync))
                {
                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.CoreWebView2InitializationCompleted: Run Lazy DoNavigate to '{__SourceAsync}'.");
                    _DoNavigate(__SourceAsync);
                }
            }
        }

        private void _DoNavigate(string source, bool asAsync = false)
        {
            if (String.IsNullOrEmpty(source)) return;

            if (this._WView.CoreWebView2 is null)
            {
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.DoNavigate: IS NULL => EnsureCoreWebView2Async...");
                this._WView.EnsureCoreWebView2Async();
                DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.DoNavigate: IS NULL => Delayed (to '{source}').");
                __SourceAsync = source;
            }
            else
            {
                __SourceAsync = null;
                if (!asAsync)
                {
                    DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.DoNavigate Now to '{source}'...");
                    if (!source.StartsWith("<"))
                        _WView.Source = new Uri(source);
                    else
                        _WView.NavigateToString(source);
                }
                else
                { }
            }
        }
        private string __SourceAsync;

        private void _WebView2_SourceChanged(object sender, Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.SourceChanged: '{_WView.Source.ToString()}'");
            _RefreshUrl(_WView.Source.ToString());
        }

        private void CoreWebView2_StatusBarTextChanged(object sender, object e)
        {
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"WebView2.StatusBarTextChanged: '{_WView.CoreWebView2.StatusBarText}'");
        }

    
        */
    }
}
