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
    [RunFormInfo(groupText: "Testovací okna", buttonText: "MS WebMapa", buttonOrder: 71, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře MS WebView2 prohlížeč (MS Edge based)", tabViewToolTip: "WebView2 Mapa")]
    internal class WebMapForm : DxRibbonForm
    {
        #region Konstrukce
        protected override void DxMainContentPrepare()
        {
            // Buttony, které reprezentují "oblíbené stránky":
            __NavButtons = new List<DxSimpleButton>();

            createButton("seznam", "https://www.seznam.cz/", _ClickButtonNavigate);
            createButton("mapy A", "https://www.mapy.cz/", _ClickButtonNavigate);
            createButton("mapy B", @"https://mapy.cz/dopravni?x=14.5802973&y=50.5311090&z=14", _ClickButtonNavigate);
            createButton("mapy C", @"https://mapy.cz/dopravni?l=0&x=15.8629028&y=50.2145999&z=17", _ClickButtonNavigate);
            createButton("mapy D", @"https://mapy.cz/dopravni?vlastni-body&ut=Nový bod&uc=9kFczxY5mZ&ud=15°51%2742.665""E 50°12%2754.179""N&x=15.8629028&y=50.2145999&z=17", _ClickButtonNavigate);
            createButton("google", "https://www.google.com/", _ClickButtonNavigate);
            createButton("meteo", "https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/short.html?display=var&gmap_zoom=7&prod=czrad_maxz_celdn_masked&opa1=0.6&opa2=0.7&nselect=14&nselect_fct=6&di=1&rep=2&add=4&update=4&lat=49.951&lon=15.797&lang=CZ", _ClickButtonNavigate);
            createButton("GreenMapa", "<GreenMapa>", _ClickButtonNavigate);
            createButton("Static", null, _ClickButtonStaticImage);

            // Vlastní WebView:
            __MapViewPanel = new DxMapViewPanel();
            // __MapViewPanel.MsWebCurrentDocumentTitleChanged += _MsWebCurrentDocumentTitleChanged;
            this.DxMainPanel.Controls.Add(__MapViewPanel);

            _DoContentLayout();

            void createButton(string text, string url, EventHandler click)
            {
                var button = DxComponent.CreateDxSimpleButton(0, 0, 150, 32, this.DxMainPanel, text, click, tag: url);
                __NavButtons.Add(button);
            }
        }
        /// <summary>
        /// Po změně titulku dokumentu jej vepíšu jaké název formuláře
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebCurrentDocumentTitleChanged(object sender, EventArgs e)
        {
            var docTitle = __MapViewPanel.MsWebProperties.DocumentTitle ?? "";
            if (docTitle.Length > 53) docTitle = docTitle.Substring(0, 50) + "...";
            this.SetGuiValue(t => this.Text = t, docTitle);
        }
        private List<DxSimpleButton> __NavButtons;
        private DxMapViewPanel __MapViewPanel;
        /// <summary>
        /// Provede se po změně velikosti ClientSize panelu <see cref="DxRibbonForm.DxMainPanel"/>
        /// </summary>
        protected override void DxMainContentDoLayout()
        {
            _DoContentLayout();
        }
        private void _DoContentLayout()
        {
            var webPanel = __MapViewPanel;
            if (webPanel is null) return;

            var clientSize = this.DxMainPanel.ClientSize;

            int currentDpi = this.CurrentDpi;
            int x0 = DxComponent.ZoomToGui(6, currentDpi);
            int y0 = DxComponent.ZoomToGui(6, currentDpi);
            int bw = DxComponent.ZoomToGui(140, currentDpi);
            int bh = DxComponent.ZoomToGui(28, currentDpi);
            int dw = DxComponent.ZoomToGui(4, currentDpi);

            int bx = x0;
            int by = y0;
            foreach (var button in __NavButtons)
            {
                button.Bounds = new Rectangle(bx, by, bw, bh);
                bx += (bw + dw);
            }

            int ws = DxComponent.ZoomToGui(3, currentDpi);
            int wx = x0;
            int wy = y0 + bh + ws;
            int ww = clientSize.Width - wx - wx;
            int wh = clientSize.Height - y0 - wy;
            webPanel.Bounds = new Rectangle(wx, wy, ww, wh);
        }

        private void _ClickButtonNavigate(object sender, EventArgs e)
        {
            var webPanel = __MapViewPanel;
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
                    webPanel.MsWebProperties.HtmlContent = html;
                }
                else
                {
                    webPanel.MsWebProperties.UrlAdress = text;
                }
            }
        }

        private void _ClickButtonStaticImage(object sender, EventArgs e)
        {
            var properties = __MapViewPanel.MsWebProperties;
            var isStatic = !properties.IsStaticPicture;
            if (sender is DxSimpleButton button)
            {
                string checkImage = "svgimages/diagramicons/check.svg";
                button.Appearance.FontStyleDelta = (isStatic ? (FontStyle.Bold | FontStyle.Underline) : FontStyle.Regular);
                button.ImageName = (isStatic ? checkImage : null);
            }
            properties.IsStaticPicture = isStatic;
        }
        #endregion
    }
}
