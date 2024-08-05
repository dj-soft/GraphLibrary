using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel obsahující <see cref="MsWebView"/>
    /// </summary>
    internal class DxWebViewPanel : DxPanelControl
    {
        public DxWebViewPanel()
        {
        
        
        }

        private MsWebView __MsWebView;
    }
    /// <summary>
    /// Potomek třídy <see cref="Microsoft.Web.WebView2.WinForms.WebView2"/>
    /// </summary>
    internal class MsWebView : Microsoft.Web.WebView2.WinForms.WebView2
    {

    }
}
