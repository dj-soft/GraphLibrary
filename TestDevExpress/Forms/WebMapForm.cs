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

            // Vlastní WebView:
            __MapViewPanel = new DxMapViewPanel();
            __MapViewPanel.MapProperties.ShowPinAtPoint = true;
            __MapViewPanel.MapProperties.IsSearchCoordinatesVisible = true;
            __MapViewPanel.MsWebCurrentDocumentTitleChanged += _MsWebCurrentDocumentTitleChanged;
            this.DxMainPanel.Controls.Add(__MapViewPanel);

            // Buttony, které reprezentují "oblíbené pozice":
            createButton(_ClickButtonNavigate, "Chrudim", "49.95117118051981N, 15.794821530619032E");
            createButton(_ClickButtonNavigate, "Pardubice", "50.03852988019973N, 15.778977738020302E");
            createButton(_ClickButtonNavigate, "Hradec Králové", "50.2072337N, 15.8304922E");
            createButton(_ClickButtonNavigate, "Staré Ransko", "49.67868715559161N, 15.832009125041111E");
            createButton(_ClickButtonNavigate, "Orlické hory", "50.2435940N, 16.2956143E");
            createButton(_ClickButtonNavigate, "Gargano", "41.7842548, 15.7868100E");

            // Další v řadě:
            __ProviderButton = createDropDownButton(_SelectProviderChange, DxMapCoordinatesProvider.SeznamMapy, DxMapCoordinatesProvider.FrameMapy, DxMapCoordinatesProvider.GoogleMaps, DxMapCoordinatesProvider.OpenStreetMap);

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
        /// <summary>
        /// Zajistí umístění prvků do layoutu po Resize atd:
        /// </summary>
        private void _DoContentLayout()
        {
            var webPanel = __MapViewPanel;
            if (webPanel is null) return;

            var clientSize = this.DxMainPanel.ClientSize;

            int currentDpi = this.CurrentDpi;
            int paddingH = DxComponent.ZoomToGui(6, currentDpi);
            int paddingV = DxComponent.ZoomToGui(6, currentDpi);
            int buttonWidth = DxComponent.ZoomToGui(140, currentDpi);
            int toolHeight = DxComponent.ZoomToGui(28, currentDpi);
            int distanceX = DxComponent.ZoomToGui(4, currentDpi);
            int dropDownWidth = DxComponent.ZoomToGui(160, currentDpi);

            // Controly v Toolbaru:
            int controlX = paddingH;
            int controlY = paddingV;
            foreach (var control in __NavControls)
            {
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
            if (sender is Control control && control.Tag is string text)
            {
                __CurrentCoordinates = text;
                _GoToMap(true);
            }
        }

        private void _SelectProviderChange(object sender, TEventArgs<IMenuItem> e)
        {
            var button = (sender as DxDropDownButton) ?? __ProviderButton;
            if (button != null && e.Item.Tag is DxMapCoordinatesProvider provider)
            {
                button.Text = provider.ToString();
                __CurrentProvider = provider;
                _GoToMap(true);
            }
        }
        private void _GoToMap(bool forceUrl)
        {
            var webPanel = __MapViewPanel;
            if (webPanel != null && !String.IsNullOrEmpty(__CurrentCoordinates))
            {
                webPanel.MapProperties.CoordinatesProvider = __CurrentProvider;
                webPanel.MapProperties.Coordinates = __CurrentCoordinates;
                if (forceUrl)
                    webPanel.RefreshMap();
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
