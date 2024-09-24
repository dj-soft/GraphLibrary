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
    [RunFormInfo(groupText: "Testovací okna", buttonText: "MS WebMapa", buttonOrder: 72, buttonImage: "devav/other/map.svg", buttonToolTip: "Otevře MS WebView2 mapy (MS Edge based)", tabViewToolTip: "WebView2 Mapa")]
    internal class WebMapForm : DxRibbonForm
    {
        #region Konstrukce
        protected override void DxMainContentPrepare()
        {
            // Buttony, které reprezentují "oblíbené pozice":
            __NavButtons = new List<DxSimpleButton>();

            createButton("Chrudim", "15.7951729;49.9499113;15", _ClickButtonNavigate);
            createButton("Pardubice", "15.7713549;50.0323932;14", _ClickButtonNavigate);
            createButton("Hradec Králové", "15.8304922;50.2072337;14", _ClickButtonNavigate);
            createButton("Staré Ransko", "15.8308731;49.6790662;18;F", _ClickButtonNavigate);
            createButton("Orlické hory", "16.2956143;50.2435940;11", _ClickButtonNavigate);
            createButton("Gargano", "15.7868100;41.7842548;10", _ClickButtonNavigate);

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
                var coordinates = new DxMapCoordinates();
                coordinates.Coordinates = text;
                string url = coordinates.UrlAdress;
                webPanel.MsWebProperties.UrlAdress = url;

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
