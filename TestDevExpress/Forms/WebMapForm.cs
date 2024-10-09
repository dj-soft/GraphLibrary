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
            createButton(_ClickButtonNavigate, "Chrudim", "49.951259217818N, 15.794888555225E");
            createButton(_ClickButtonNavigate, "Pardubice", "50.03852988019973N, 15.778977738020302E");
            createButton(_ClickButtonNavigate, "Hradec Králové", "50.209194071553N, 15.832793535335E");
            createButton(_ClickButtonNavigate, "Staré Ransko", "49.678687155592N, 15.832009125041E");
            createButton(_ClickButtonNavigate, "Orlické hory", "50.286855799634N, 16.387581882703E");
            createButton(_ClickButtonNavigate, "Gargano", "41.8835225N, 16.1818136E");

            // Další controly v řadě:
            var mapProviders = MapProvider.AllProviders;
            __ProviderButton = createDropDownButton(_SelectProviderChange, mapProviders);
            __MapTypeButton = createDropDownButton(_SelectMapTypeChange, DxMapCoordinatesMapType.Standard, DxMapCoordinatesMapType.Photo, DxMapCoordinatesMapType.Traffic);
            __WebDisplayModeButton = createDropDownButton(_SelectDisplayModeChange, WebDisplayType.Editable, WebDisplayType.ReadOnly, WebDisplayType.StaticAsync, WebDisplayType.StaticSync);

            _DoContentLayout();

            DxSimpleButton createButton(EventHandler clickHandler, string text, string url)
            {
                var button = DxComponent.CreateDxSimpleButton(0, 0, 150, 32, this.DxMainPanel, text, clickHandler, tag: url);
                __NavControls.Add(button);
                return button;
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
            var docTitle = __MapViewPanel.MapProperties.DocumentTitle ?? "";
            if (docTitle.Length > 53) docTitle = docTitle.Substring(0, 50) + "...";
            this.SetGuiValue(t => this.Text = t, docTitle);
        }
        private string __CurrentCoordinates;
        private string __CurrentUrlAdress;
        private IMapProvider __CurrentProvider;
        private DxMapCoordinatesMapType __CurrentMapType;
        private WebDisplayType __CurrentEditableType;
        private List<Control> __NavControls;
        private DxMapViewPanel __MapViewPanel;
        private DxDropDownButton __ProviderButton;
        private DxDropDownButton __MapTypeButton;
        private DxDropDownButton __WebDisplayModeButton;
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
            if (sender is Control control && control.Tag is string text)
            {
                __CurrentCoordinates = text;
                _GoToMap(true);
            }
        }

        private void _SelectProviderChange(object sender, TEventArgs<IMenuItem> e)
        {
            var button = (sender as DxDropDownButton) ?? __ProviderButton;
            if (button != null && e.Item.Tag is IMapProvider provider)
            {
                button.Text = provider.ToString();
                __CurrentProvider = provider;
                _GoToMap(true);
            }
        }
        private void _SelectMapTypeChange(object sender, TEventArgs<IMenuItem> e)
        {
            var button = (sender as DxDropDownButton) ?? __MapTypeButton;
            if (button != null && e.Item.Tag is DxMapCoordinatesMapType mapType)
            {
                button.Text = mapType.ToString();
                __CurrentMapType = mapType;
                _GoToMap(true);
            }
        }
        private void _SelectDisplayModeChange(object sender, TEventArgs<IMenuItem> e)
        {
            var button = (sender as DxDropDownButton) ?? __WebDisplayModeButton;
            if (button != null && e.Item.Tag is WebDisplayType editableType)
            {
                button.Text = editableType.ToString();
                __CurrentEditableType = editableType;
                var mapPanel = __MapViewPanel;
                mapPanel.MapProperties.IsMapEditable = (editableType == WebDisplayType.Editable);
                mapPanel.MapProperties.DisplayMode = (editableType == WebDisplayType.Editable ? DxWebViewDisplayMode.Live :
                                                     (editableType == WebDisplayType.ReadOnly ? DxWebViewDisplayMode.Live :
                                                     (editableType == WebDisplayType.StaticAsync ? DxWebViewDisplayMode.CaptureAsync :
                                                     (editableType == WebDisplayType.StaticSync ? DxWebViewDisplayMode.CaptureSync : DxWebViewDisplayMode.Live))));
            }
        }
        private void _GoToMap(bool forceUrl)
        {
            var mapPanel = __MapViewPanel;
            if (mapPanel != null && !String.IsNullOrEmpty(__CurrentCoordinates))
            {
                mapPanel.MapProperties.IsMapEditable = (__CurrentEditableType == WebDisplayType.Editable);
                mapPanel.MapProperties.CoordinatesProvider = __CurrentProvider;
                mapPanel.MapProperties.CoordinatesMapType = __CurrentMapType;
                mapPanel.MapProperties.Coordinates = __CurrentCoordinates;               // Zde se vyvolá Reload mapy
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
