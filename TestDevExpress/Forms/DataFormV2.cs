using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    public class DataFormV2 : DxRibbonForm
    {
        public DataFormV2()
        {
            this.InitializeForm();
        }

        protected void InitializeForm()
        {
            this.Size = new Size(800, 600);

            this.Text = $"Test DataForm 2 :: {DxComponent.FrameworkName}";

            _DxDataForm2 = new DxDataFormV2() { Dock = System.Windows.Forms.DockStyle.Fill };
            this.DxMainPanel.Controls.Add(_DxDataForm2);
        }
        private DxDataFormV2 _DxDataForm2;
        protected override void DxRibbonPrepare()
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ" };
            pages.Add(page);
            page.Groups.Add(DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: true));

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);
        }
        protected override void DxStatusPrepare()
        {
            
        }
    }
}
