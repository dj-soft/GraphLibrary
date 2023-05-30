using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy SVG images
    /// </summary>
    public class TestSvgForm : DxRibbonForm
    {
        public TestSvgForm()
        {
            this.Text = "Testování SVG ikon a stavu Disabled";
        }
        protected override void DxRibbonPrepare()
        {
            this.DxRibbon.DebugName = "TestSvgRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.DxRibbon.LogActive = false;

            //     this.DxRibbon.ImageRightFull = DxComponent.CreateBitmapImage("Images/ImagesBig/Bart 01bt.png");
            //     this.DxRibbon.ImageRightMini = DxComponent.CreateBitmapImage("Images/ImagesBig/Bart 01c.png");

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.Basic);
            pages.Add(page);

            AddDevExpGroup(page, 7);
            AddAsolGroup(page, 9);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        /// <summary>
        /// Přidá testovací grupu DevExpress
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        private void AddDevExpGroup(DataRibbonPage page, int count)
        {
            string[] resources = new string[]
{
    "svgimages/richedit/customizemergefield.svg",
    "svgimages/richedit/cut.svg",
    "svgimages/richedit/deletecomment.svg",
    "svgimages/richedit/deletetable.svg",
    "svgimages/richedit/deletetablecells.svg",
    "svgimages/richedit/deletetablecolumns.svg",
    "svgimages/richedit/deletetablerows.svg",
    "svgimages/richedit/differentfirstpage.svg",
    "svgimages/richedit/differentoddevenpages.svg",
    "svgimages/richedit/distributed.svg",
    "svgimages/richedit/documentproperties.svg",
    "svgimages/richedit/documentstatistics.svg",
    "svgimages/richedit/draftview.svg"
};
            AddTestGroup(page, resources, "DEVEXPRESS", false, count);
        }
        /// <summary>
        /// Přidá testovací grupu ASOL
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        private void AddAsolGroup(DataRibbonPage page, int count)
        {
            string[] resources = new string[]
{
    "pic/application",
    "pic/application-windows",
    "pic/asset-filled",
    "pic/calendar-selection",
    "pic/certificate-business",
    "pic/certificate-license",
    "pic/cleanup",
    "pic/close",
    "pic/component",
    "pic/constant-filled",
    "pic/conveyor-belt",
    "pic/credit-card-back-filled",
    "pic_0/UI/DynRel/Rel1ExtDoc",
    "pic_0/UI/DynRel/Rel1ExtDoc",
    "pic_0/UI/DynRel/RelNExtDoc",
    "pic_0/UI/DynRel/RelNExtDoc",
    //   "pic/folder10",
    //   "pic/folder20",
    //   "pic/folder30",
    //   "pic/folder40",
    "pic/folder50",
    //   "pic/folder60",
    //   "pic/folder70",
    //   "pic/folder80",
    //   "pic/folder90",
    "pic/folder-action-close-filled",
    "pic/folders-tree-filled",
    //   "pic/form-colour-10",
    //   "pic/form-colour-20",
    //   "pic/form-colour-30",
    //   "pic/form-colour-40",
    "pic/form-colour-50",
    //   "pic/form-colour-60",
    //   "pic/form-colour-70",
    //   "pic/form-colour-80",
    //   "pic/form-colour-90",
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorContains,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorDoesNotContain,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorEndWith,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorDoesNotEndWith,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorEquals,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorNotEquals,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorStartWith,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorDoesNotStartWith,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorLessThan,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorLessThanOrEqualTo,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorGreaterThan,
    //   Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorGreaterThanOrEqualTo,
    "pic/invoice"
};
            AddTestGroup(page, resources, "ASOL", true, count);
        }
        /// <summary>
        /// Přidá testovací grupu
        /// </summary>
        /// <param name="page"></param>
        /// <param name="resources"></param>
        /// <param name="groupText"></param>
        /// <param name="prepareDisabledImage"></param>
        /// <param name="count"></param>
        private void AddTestGroup(DataRibbonPage page, string[] resources, string groupText, bool prepareDisabledImage, int count)
        {
            resources = resources.Shuffle();

            DataRibbonGroup group = new DataRibbonGroup() { GroupId = groupText, GroupText = groupText };
            foreach (var resource in resources)
            {
                if (group.Items.Count >= count) break;
                string name = System.IO.Path.GetFileNameWithoutExtension(resource.Replace("/", "\\"));
                bool enabled = Randomizer.IsTrue(75);
                var button = new DataRibbonItem() 
                { 
                    ItemId = name + (enabled ? "+" : "-"), 
                    Text = name + (enabled ? " Enabled": " Disabled"), 
                    ImageName = resource,
                    ToolTipText = (enabled ? "Enabled:" : "Disabled:") + "\r\n" + resource, 
                    ItemType = RibbonItemType.Button, 
                    RibbonStyle = RibbonItemStyles.Large, 
                    Enabled = enabled,
                    PrepareDisabledImage = prepareDisabledImage
                };
                group.Items.Add(button);
            }

            page.Groups.Add(group);
        }
        /// <summary>
        /// Po kliknutí
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            if (e.Item.ParentGroup.GroupId == "DEVEXPRESS" || e.Item.ParentGroup.GroupId == "ASOL")
            {   // 
                var barItem = e.Item.RibbonItem.Target;
                if (barItem != null)
                    SetEnabled(barItem, !barItem.Enabled);
                PrepareAnyEnabled(e.Item);
            }
        }
        /// <summary>
        /// Zajistí, aby v grupě, kam patří dodaná prvek byl alespoň jeden Enabled prvek.
        /// Pokud není, pak nastaví všechny (vyjma dodaného, pokud jsou tam jiné).
        /// </summary>
        /// <param name="item"></param>
        private void PrepareAnyEnabled(IRibbonItem item)
        {
            var group = item?.ParentGroup;
            if (group is null || group.Items is null) return;

            DevExpress.XtraBars.BarItem barItem;

            // Kolik je jich Enabled?
            int enabledCount = 0;
            foreach (var i in group.Items)
            {
                barItem = i.RibbonItem.Target;
                if (barItem != null && barItem.Enabled)
                    enabledCount++;
            }
            if (enabledCount > 0) return;

            // Nastavím ostatní na true:
            foreach (var i in group.Items)
            {
                if (i.ItemId == item.ItemId) continue;          // Vstupní prvek přeskočím
                barItem = i.RibbonItem.Target;
                if (barItem != null)
                {
                    SetEnabled(barItem, true);
                    enabledCount++;
                }
            }
            if (enabledCount > 0) return;

            // Nic nezbývá jiného než dát Enabled na vstupní prvek:
            barItem = item.RibbonItem.Target;
            if (barItem != null)
            {
                SetEnabled(barItem, true);
                enabledCount++;
            }
        }

        private void SetEnabled(DevExpress.XtraBars.BarItem barItem, bool enabled)
        {
            barItem.Enabled = enabled;
            string caption = barItem.Caption;
            string badCaption = (enabled ? "Disabled" : "Enabled");
            string goodCaption = (enabled ? "Enabled" : "Disabled");
            if (caption.Contains(badCaption))
            {
                caption = caption.Replace(badCaption, goodCaption);
                barItem.Caption = caption;
            }
        }
    }
}
