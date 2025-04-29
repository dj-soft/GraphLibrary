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
using TestDevExpress.Components;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Web viewer typu MS WebView2
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "MS WebView2", buttonOrder: 71, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře MS WebView2 prohlížeč (MS Edge based)", tabViewToolTip: "WebView2 Browser")]
    internal class WebView2Form : DxRibbonForm
    {
        #region Konstrukce
        protected override void DxMainContentPrepare()
        {
            __NavControls = new List<Control>();

            // Vlastní WebView:
            __WebViewPanel = new DxWebViewPanel();
            __WebViewPanel.MsWebCurrentDocumentTitleChanged += _MsWebCurrentDocumentTitleChanged;
            this.DxMainPanel.Controls.Add(__WebViewPanel);

            // Buttony, které reprezentují "oblíbené stránky":
            createButton(_ClickButtonNavigate, "seznam", "https://www.seznam.cz/");
            createButton(_ClickButtonNavigate, "mapy A", "https://www.mapy.cz/");
            createButton(_ClickButtonNavigate, "mapy B", @"https://mapy.cz/dopravni?x=14.5802973&y=50.5311090&z=14");
            createButton(_ClickButtonNavigate, "mapy C", @"https://mapy.cz/dopravni?l=0&x=15.8629028&y=50.2145999&z=17");
            createButton(_ClickButtonNavigate, "mapy D", @"https://mapy.cz/dopravni?vlastni-body&ut=Nový bod&uc=9kFczxY5mZ&ud=15°51%2742.665""E 50°12%2754.179""N&x=15.8629028&y=50.2145999&z=17");
            createButton(_ClickButtonNavigate, "google", "https://www.google.com/");
            createButton(_ClickButtonNavigate, "meteo", "https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/short.html?display=var&gmap_zoom=7&prod=czrad_maxz_celdn_masked&opa1=0.6&opa2=0.7&nselect=14&nselect_fct=6&di=1&rep=2&add=4&update=4&lat=49.951&lon=15.797&lang=CZ");
            createButton(_ClickButtonNavigate, "GreenMapa", "<GreenMapa>");

            // Další controly v řadě:
            __WebDisplayModeButton = createDropDownButton(_SelectDisplayModeChange, WebDisplayType.Editable, WebDisplayType.ReadOnly, WebDisplayType.StaticAsync, WebDisplayType.StaticSync);

            _DoContentLayout(true);

            void createButton(EventHandler click, string text, string url)
            {
                var button = DxComponent.CreateDxSimpleButton(0, 0, 150, 32, this.DxMainPanel, text, click, tag: url);
                __NavControls.Add(button);
            }
            DxDropDownButton createDropDownButton(EventHandler<TEventArgs<IMenuItem>> itemClickHandler, params object[] values)
            {
                var items = values.Select(v => (IMenuItem)new DataMenuItem() { Text = v.ToString(), Tag = v }).ToArray();
                var button = DxComponent.CreateDxDropDownButton(0, 0, 150, 32, this.DxMainPanel, "", null, itemClickHandler, subItems: items);
                button.OpenDropDownOnButtonClick = true;
                __NavControls.Add(button);

                // Aktivujeme první položku:
                if (items.Length > 0)
                    itemClickHandler(button, new TEventArgs<IMenuItem>(items[0]));

                return button;
            }
        }
        /// <summary>
        /// Po změně titulku dokumentu jej vepíšu jaké název formuláře
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MsWebCurrentDocumentTitleChanged(object sender, EventArgs e)
        {
            var docTitle = __WebViewPanel.WebProperties.DocumentTitle ?? "";
            if (docTitle.Length > 53) docTitle = docTitle.Substring(0, 50) + "...";
            this.SetGuiValue(t => this.Text = t, docTitle);
        }

        private WebDisplayType __CurrentEditableType;
        private List<Control> __NavControls;
        private DxWebViewPanel __WebViewPanel;
        private DxDropDownButton __WebDisplayModeButton;
        /// <summary>
        /// Provede se po změně velikosti ClientSize panelu <see cref="DxRibbonForm.DxMainPanel"/> i v jiných situacích.
        /// Aktuální stav formuláře lze zjistit v <see cref="DxRibbonBaseForm.ActivityState"/>.
        /// Parametr <paramref name="isSizeChanged"/> říká, zda se od posledního volání této metody změnila velikost Main panelu.
        /// </summary>
        /// <param name="isSizeChanged">Pokud je true, pak od posledního volání této metody se změnila velikost panelu <see cref="DxRibbonForm.DxMainPanel"/> 'ClientSize'. Hodnota false = nezměnila se, ale změnilo se okno nebo něco jiného...</param>
        protected override void DxMainContentDoLayout(bool isSizeChanged)
        {
            _DoContentLayout(isSizeChanged);
        }
        private void _DoContentLayout(bool isSizeChanged)
        {
            var webPanel = __WebViewPanel;
            if (webPanel is null) return;

            var clientSize = this.DxMainPanel.ClientSize;

            int currentDpi = this.CurrentDpi;
            int paddingH = DxComponent.ZoomToGui(6, currentDpi);
            int paddingV = DxComponent.ZoomToGui(6, currentDpi);
            int buttonWidth = DxComponent.ZoomToGui(125, currentDpi);
            int toolHeight = DxComponent.ZoomToGui(28, currentDpi);
            int distanceX = DxComponent.ZoomToGui(4, currentDpi);
            int separatorX = DxComponent.ZoomToGui(12, currentDpi);
            int dropDownWidth = DxComponent.ZoomToGui(140, currentDpi);

            // Controly v Toolbaru:
            int controlX = paddingH;
            int controlY = paddingV;
            Type lastType = null;
            foreach (var control in __NavControls)
            {
                // Oddělit mezerou odlišné typy (Button a DropDown):
                Type currType = control.GetType();
                if (lastType != null && currType != lastType)
                    controlX += separatorX;
                lastType = currType;

                // Souřadnice:
                int controlWidth = ((control is DxDropDownButton) ? dropDownWidth : buttonWidth);
                control.Bounds = new Rectangle(controlX, controlY, controlWidth, toolHeight);
                controlX += (controlWidth + distanceX);
            }

            // WebView:
            int distanceY = DxComponent.ZoomToGui(3, currentDpi);
            int webX = paddingH;
            int webY = paddingV + toolHeight + distanceY;
            int webWidth = clientSize.Width - webX - webX;
            int webHeight = clientSize.Height - paddingV - webY;
            webPanel.Bounds = new Rectangle(webX, webY, webWidth, webHeight);
        }

        private void _ClickButtonNavigate(object sender, EventArgs e)
        {
            var webPanel = __WebViewPanel;
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
                    webPanel.WebProperties.HtmlContent = html;
                }
                else
                {
                    webPanel.WebProperties.UrlAdress = text;
                }
            }
        }

        private void _ClickButtonStaticImage(object sender, EventArgs e)
        {
            var properties = __WebViewPanel.WebProperties;
            var displayMode = properties.DisplayMode;
            // Změnit hodnotu po kliknutí:
            displayMode = (displayMode == DxWebViewDisplayMode.Live ? DxWebViewDisplayMode.CaptureAsync : DxWebViewDisplayMode.Live);
            if (sender is DxSimpleButton button)
            {   // Button: pro režim Capture bude mít ikonu Check, a text bude Bold; opačně pro Live bude bez ikony a Regular:
                bool isCapture = (displayMode == DxWebViewDisplayMode.CaptureAsync || displayMode == DxWebViewDisplayMode.CaptureSync);
                button.ImageName = (isCapture ? "svgimages/diagramicons/check.svg" : null);
                button.Appearance.FontStyleDelta = (isCapture ? (FontStyle.Bold | FontStyle.Underline) : FontStyle.Regular);
            }
            properties.DisplayMode = displayMode;
        }

        private void _SelectDisplayModeChange(object sender, TEventArgs<IMenuItem> e)
        {
            var button = (sender as DxDropDownButton) ?? __WebDisplayModeButton;
            if (button != null && e.Item.Tag is WebDisplayType editableType)
            {
                button.Text = editableType.ToString();
                __CurrentEditableType = editableType;
                var webPanel = __WebViewPanel;
                // webPanel.MapProperties.IsMapEditable = (editableType == WebDisplayType.Editable);
                webPanel.WebProperties.DisplayMode = (editableType == WebDisplayType.Editable ? DxWebViewDisplayMode.Live :
                                                     (editableType == WebDisplayType.ReadOnly ? DxWebViewDisplayMode.Live :
                                                     (editableType == WebDisplayType.StaticAsync ? DxWebViewDisplayMode.CaptureAsync :
                                                     (editableType == WebDisplayType.StaticSync ? DxWebViewDisplayMode.CaptureSync : DxWebViewDisplayMode.Live))));
            }
        }
        /// <summary>
        /// Režimy DisplayMode controlu
        /// </summary>
        private enum WebDisplayType
        {
            Editable,
            ReadOnly,
            StaticAsync,
            StaticSync
        }
        #endregion
    }
}
