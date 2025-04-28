using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using WinDraw = System.Drawing;
using DxDForm = Noris.Clients.Win.Components.AsolDX.DataForm;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DxLData = Noris.Clients.Win.Components.AsolDX.DataForm.Layout;
using System.Drawing;
using Noris.Clients.Win.Components.AsolDX.DxForm;
using Noris.Clients.Win.Components.AsolDX.DataForm;
using TestDevExpress.Components;

using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;

using DXR = DevExpress.XtraEditors.Repository;



namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormX"/>
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "DataForm", buttonOrder: 14, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře okno DataForm verze 4", tabViewToolTip: "Okno zobrazující nový DataForm")]
    public class DataFormV4 : DxRibbonForm  //, IControlInfoSource
    {
        protected override void DxRibbonPrepare()
        {
            var ribbonContent = new DataRibbonContent();
            ribbonContent.StatusBarItems.Add(new DataRibbonItem() { ItemType = RibbonItemType.Static, Text = "Obsah GDI" });
            this.DxRibbon.RibbonContent = ribbonContent;
        }
        protected override void DxMainContentPrepare()
        {
            base.DxMainContentPrepare();

            _Layout = new LayoutControl() { Dock = DockStyle.Fill };
            this.DxMainPanel.Controls.Add(_Layout);

            var lc = _Layout;


            for (int q = 0; q < 160; q++)
            {
                var repoItem = new DXR.RepositoryItemTextEdit();
                repoItem.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Flat;
                repoItem.NullText = "xxxxxxxxxxxx";

                var layoutItem = new LayoutRepositoryItem(repoItem);
                layoutItem.Name = "RepoItem" + q.ToString();
                layoutItem.Text = "RepositoryItem " + q.ToString() + ":";
                layoutItem.Size = new WinDraw.Size(360, 20);
                layoutItem.EditValue = Randomizer.GetSentence(2, 6, true);

                _Layout.Root.Add(layoutItem);
            }
        }
        LayoutControl _Layout;
    }
}
