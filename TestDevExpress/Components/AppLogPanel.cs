using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Components
{
    /// <summary>
    /// Panel, který obsahuje log aplikace
    /// </summary>
    public class AppLogPanel : DxPanelControl
    {
        public AppLogPanel()
        {
            __LogText = DxComponent.CreateDxMemoEdit(this, DockStyle.Fill, readOnly: true, tabStop: false);
        }
        private DxMemoEdit __LogText;
    }
}
