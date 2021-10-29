using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
#if Compile_TestDevExpress

    /// <summary>
    /// Adapter na systém TestDevExpress
    /// </summary>
    internal class CurrentAdapter : ISystemAdapter
    {
        event EventHandler ISystemAdapter.InteractiveZoomChanged { add { } remove { } }
        decimal ISystemAdapter.ZoomRatio { get { return 1.0m; } }
        string ISystemAdapter.GetMessage(string messageCode, params object[] parameters) { return null; }
        System.Windows.Forms.ImageList ISystemAdapter.GetResourceImageList(ResourceImageSizeType sizeType) { return null; }
        System.Drawing.Image ISystemAdapter.GetResourceImage(string resourceName, ResourceImageSizeType sizeType, string caption) { return null; }
        int ISystemAdapter.GetResourceIndex(string iconName, ResourceImageSizeType sizeType, string caption) { return -1; }
        System.Windows.Forms.Keys ISystemAdapter.GetShortcutKeys(string shortCut) { return System.Windows.Forms.Keys.None; }
    }

    /// <summary>
    /// Rozhraní předepisuje metodu <see cref="HandleEscapeKey()"/>, která umožní řešit klávesu Escape v rámci systému
    /// </summary>
    public interface IEscapeKeyHandler
    {
        /// <summary>
        /// Systém zaregistroval klávesu Escape; a ptá se otevřených oken, které ji chce vyřešit...
        /// </summary>
        /// <returns></returns>
        bool HandleEscapeKey();
    }

#endif

}
