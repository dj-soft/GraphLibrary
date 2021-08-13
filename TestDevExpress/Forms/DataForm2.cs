using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    public class DataForm2 : DxRibbonForm
    {
        public DataForm2()
        {
            this.InitializeForm();
        }

        protected void InitializeForm()
        {
            this.Size = new System.Drawing.Size(800, 600);

            this.Text = $"Test DataForm 2 :: {DxComponent.FrameworkName}";

            _DxDataForm2 = new DxDataForm2() { Dock = System.Windows.Forms.DockStyle.Fill };
            this.DxMainPanel.Controls.Add(_DxDataForm2);
        }
        private DxDataForm2 _DxDataForm2;
        protected override void DxRibbonPrepare()
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "DevExpress" };
            pages.Add(page);
            page.Groups.Add(DxRibbonControl.CreateSkinIGroup("DESIGN"));

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);
        }
        protected override void DxStatusPrepare()
        {
            
        }
    }

    public class DxDataForm2 : DxPanelControl
    { }

}
