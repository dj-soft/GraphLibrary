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
            __NavControls = new List<Control>();

            // Buttony, které reprezentují "oblíbené pozice":
            createButton(_ClickButtonNavigate, "Chrudim", "15.7951729;49.9499113;15");
            createButton(_ClickButtonNavigate, "Pardubice", "15.7765933;50.0379536;18;S;15.778977738020302;50.03852988019973");              // https://mapy.cz/zakladni?source=coor&id=15.778977738020302%2C50.03852988019973&x=15.7794498&y=50.0384170&z=19
            createButton(_ClickButtonNavigate, "Hradec Králové", "15.8304922;50.2072337;14");
            createButton(_ClickButtonNavigate, "Staré Ransko", "15.8324115; 49.6787496; 17; F; 15.832009125041111; 49.67868715559161");
            createButton(_ClickButtonNavigate, "Orlické hory", "16.2956143;50.2435940;11");
            createButton(_ClickButtonNavigate, "Gargano", "15.7868100;41.7842548;10");

            // Další v řadě:
            __ProviderButton = createDropDownButton(_SelectProviderChange, DxMapCoordinatesProvider.SeznamMapy, DxMapCoordinatesProvider.FrameMapy, DxMapCoordinatesProvider.GoogleMaps, DxMapCoordinatesProvider.OpenStreetMap);

            // Vlastní WebView:
            __MapViewPanel = new DxMapViewPanel();
            __MapViewPanel.MsWebCurrentDocumentTitleChanged += _MsWebCurrentDocumentTitleChanged;
            this.DxMainPanel.Controls.Add(__MapViewPanel);

            _DoContentLayout();

            DxSimpleButton createButton(EventHandler clickHandler, string text, string url)
            {
                var button = DxComponent.CreateDxSimpleButton(0, 0, 150, 32, this.DxMainPanel, text, clickHandler, tag: url);
                __NavControls.Add(button);
                return button;
            }

            DxImageComboBoxEdit createCombo(EventHandler changeHandler, params string[] items)
            {
                var combo = DxComponent.CreateDxImageComboBox(0, 0, 150, this.DxMainPanel, changeHandler, items.ToOneString("\t"));
                __NavControls.Add(combo);
                combo.SelectedIndex = 0;                   // Vyvolá se handler (changeHandler) a nastaví se odpovídající hodnota
                return combo;
            }
            DxDropDownButton createDropDownButton(EventHandler<TEventArgs<IMenuItem>> itemClickHandler, params object[] values)
            {
                var items = values.Select(v => (IMenuItem)new DataMenuItem() { Text = v.ToString(), Tag = v }).ToArray();
                var button = DxComponent.CreateDxDropDownButton(0, 0, 150, 32, this.DxMainPanel, "", null, itemClickHandler, subItems: items);
                button.OpenDropDownOnButtonClick = true;
                __NavControls.Add(button);

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
            var docTitle = __MapViewPanel.WebProperties.DocumentTitle ?? "";
            if (docTitle.Length > 53) docTitle = docTitle.Substring(0, 50) + "...";
            this.SetGuiValue(t => this.Text = t, docTitle);
        }
        private string __CurrentCoordinates;
        private string __CurrentUrlAdress;
        private DxMapCoordinatesProvider __CurrentProvider;
        private DxDropDownButton __ProviderButton;
        private List<Control> __NavControls;
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
            foreach (var button in __NavControls)
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
            if (sender is Control control && control.Tag is string text)
            {
                __CurrentCoordinates = text;
                _GoToMap();
            }
        }

        private void _SelectProviderChange(object sender, TEventArgs<IMenuItem> e)
        {
            var button = (sender as DxDropDownButton) ?? __ProviderButton;
            if (button != null && e.Item.Tag is DxMapCoordinatesProvider provider)
            {
                button.Text = provider.ToString();
                __CurrentProvider = provider;
                _GoToMap();
            }
        }
        private void _GoToMap()
        {
            var webPanel = __MapViewPanel;
            if (webPanel != null && !String.IsNullOrEmpty(__CurrentCoordinates))
            {
                webPanel.MapCoordinates.Provider = __CurrentProvider;
                webPanel.MapCoordinates.Coordinates = __CurrentCoordinates;
            }
        }
        private void _ClickButtonStaticImage(object sender, EventArgs e)
        {
            var properties = __MapViewPanel.WebProperties;
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
