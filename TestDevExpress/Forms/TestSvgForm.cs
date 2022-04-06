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
        protected override void DxRibbonPrepare()
        {
            this.DxRibbon.DebugName = "TestSvgRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.DxRibbon.LogActive = false;

            this.DxRibbon.ImageRightFull = DxComponent.CreateBitmapImage("Images/ImagesBig/Bart 01bt.png");
            this.DxRibbon.ImageRightMini = DxComponent.CreateBitmapImage("Images/ImagesBig/Bart 01c.png");

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ", MergeOrder = 1, PageOrder = 1 };
            pages.Add(page);
            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: false) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            AddDevExpGroup(page, 7);
            AddAsolGroup(page, 7);

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
            AddTestGroup(page, resources, "DEVEXPRESS", count);
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
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorContains,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorDoesNotContain,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorEndWith,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorDoesNotEndWith,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorEquals,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorNotEquals,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorStartWith,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorDoesNotStartWith,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorLessThan,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorLessThanOrEqualTo,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorGreaterThan,
    Noris.Clients.Win.Components.AsolDX.ImageName.DxFilterOperatorGreaterThanOrEqualTo
};
            AddTestGroup(page, resources, "ASOL", count);
        }
        /// <summary>
        /// Přidá testovací grupu
        /// </summary>
        /// <param name="page"></param>
        /// <param name="resources"></param>
        /// <param name="groupText"></param>
        /// <param name="count"></param>
        private void AddTestGroup(DataRibbonPage page, string[] resources, string groupText, int count)
        {
            resources = resources.Shuffle();

            DataRibbonGroup group = new DataRibbonGroup() { GroupId = groupText, GroupText = groupText };
            foreach (var resource in resources)
            {
                if (group.Items.Count >= count) break;
                string name = System.IO.Path.GetFileNameWithoutExtension(resource.Replace("/", "\\"));
                bool enabled = Random.IsTrue(75);
                var button = new DataRibbonItem() 
                { 
                    ItemId = name + (enabled ? "+" : "-"), 
                    Text = name + (enabled ? " Enabled": " Disabled"), 
                    ImageName = resource,
                    ToolTipText = (enabled ? "Enabled:" : "Disabled:") + "\r\n" + resource, 
                    ItemType = RibbonItemType.Button, 
                    RibbonStyle = RibbonItemStyles.Large, 
                    Enabled = enabled
                };
                group.Items.Add(button);
            }

            page.Groups.Add(group);
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.ImgPick":
                    ImagePickerForm.ShowForm(this);
                    break;
                default:
                    var barItem = e.Item.RibbonItem.Target;
                    if (barItem != null)
                        barItem.Enabled = !barItem.Enabled;
                    PrepareAnyEnabled(e.Item);
                    break;
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
                    barItem.Enabled = true;
                    enabledCount++;
                }
            }
            if (enabledCount > 0) return;

            // Nic nezbývá jiného než dát Enabled na vstupní prvek:
            barItem = item.RibbonItem.Target;
            if (barItem != null)
            {
                barItem.Enabled = true;
                enabledCount++;
            }
        }
    }
}
