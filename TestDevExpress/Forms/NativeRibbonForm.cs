using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXE = DevExpress.Utils.Extensions;
using DXM = DevExpress.Utils.Menu;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars;

using Noris.Clients.Win.Components.AsolDX;
using DevExpress.XtraBars.Docking.Helpers;

namespace TestDevExpress.Forms
{
    public class NativeRibbonForm : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public NativeRibbonForm() : this(CreateMode.DevExpress, false) { }
        public NativeRibbonForm(CreateMode createMode, bool useVoidSlave)
        {
            _UseVoidSlave = useVoidSlave;
            _CreateMode = createMode;
            this.SvgImages = DxComponent.GetResourceKeys(false, true);
            InitRibbon();
        }
        private CreateMode _CreateMode;
        private bool _UseVoidSlave;
        private void InitRibbon()
        {
            _ItemCount = 0;
            switch (_CreateMode)
            {
                case CreateMode.DevExpress:
                    DevExpressInit();
                    break;
                case CreateMode.Tests:
                    RibbonTestsInit();
                    break;
                case CreateMode.Asol:
                    RibbonAsolInit();
                    break;
            }
        }
        #region DevExpress Ribbon
        private void DevExpressInit()
        {
            DevExpressCreateRibbon();
            AddRibbonToForm();
            DevExpressFillRibbon();
        }
        /// <summary>
        /// Vytvoří instance ribbonů NATIVE
        /// </summary>
        private void DevExpressCreateRibbon()
        {
            this._Ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this._SlaveRibbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.Ribbon = this._Ribbon;
            this.SlaveRibbon = this._SlaveRibbon;

            this.Ribbon.ApplicationButtonText = "MAIN RIBBON";
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this.SlaveRibbon.ApplicationButtonText = (_UseVoidSlave ? "SLAVE - SLAVE RIBBON" : "SLAVE RIBBON");
            this.SlaveRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
        }
        private void DevExpressFillRibbon()
        {
            var pageR = CreatePage("MAIN PAGE", 9999);
            pageR.Groups.AddRange(this.CreateGroups(9, 8));
            this.Ribbon.Pages.Add(pageR);

            var page0 = CreatePage("SLAVE PAGE", 0);
            page0.Groups.AddRange(this.CreateGroups(12, 6));
            this.SlaveRibbon.Pages.Add(page0);

            var page1 = CreatePage("SLAVE PAGE", 1);
            page1.Groups.AddRange(this.CreateGroups(8, 9));
            this.SlaveRibbon.Pages.Add(page1);
        }
        private RibbonPage CreatePage(string prefix, int pageIndex)
        {
            string suffix = (pageIndex + 1).ToString();
            return new RibbonPage($"{prefix} NATIVE " + suffix);
        }
        private RibbonPageGroup[] CreateGroups(int groupCount, int itemCount)
        {
            List<RibbonPageGroup> groups = new List<RibbonPageGroup>();
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                RibbonPageGroup group = CreateGroup(groupIndex);
                group.ItemLinks.AddRange(this.CreateItems(groupIndex, itemCount));
                groups.Add(group);
            }
            return groups.ToArray();
        }
        private RibbonPageGroup CreateGroup(int groupIndex)
        {
            string suffix = (groupIndex + 1).ToString();
            RibbonPageGroup group = new RibbonPageGroup("Grupa NATIVE " + suffix);
            group.State = RibbonPageGroupState.Auto;
            return group;
        }
        private BarItem[] CreateItems(int groupIndex, int itemCount)
        {
            List<BarItem> items = new List<BarItem>();
            for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                BarItem item = CreateItem(groupIndex, itemIndex);
                items.Add(item);
            }
            return items.ToArray();
        }
        private BarItem CreateItem(int groupIndex, int itemIndex)
        {
            string suffix = (this.Ribbon.Items.Count + 1).ToString();
            string svgImage = Random.GetItem(SvgImages);
            string text = "Button " + suffix;
            int it = (groupIndex % 5);
            var style = (it == 0 || it == 2 ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large : DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText);
            BarItem item;
            item = ((it == 0 || it == 1) ? (BarItem)this.Ribbon.Items.CreateButton(text) :
                   ((it == 2 || it == 3) ? (BarItem)this.Ribbon.Items.CreateCheckItem(text, false) :
                         (BarItem)this.Ribbon.Items.CreateButton(text)));
            item.RibbonStyle = style;
            item.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
            return item;
        }
        #endregion
        #region Test Ribbon
        private void RibbonTestsInit()
        {
            DxCreateRibbon();
            AddRibbonToForm();
            TestFillRibbon();
        }
        private void TestFillRibbon()
        {
            //StorePageMethod(this._DxRibbon, "MAIN PAGE", 9999, 9, 8);
            //StorePageMethod(this._SlaveDxRibbon, "SLAVE PAGE", 0, 12, 6);
            //StorePageMethod(this._SlaveDxRibbon, "SLAVE PAGE", 1, 8, 9);
            StoreContentDataDirect(this._DxRibbon, "MAIN PAGE", 1);
            StoreContentDataDirect(this._SlaveDxRibbon, "SLAVE PAGE", 2);
        }
        #endregion
        #region Asol Ribbon
        private void RibbonAsolInit()
        {
            DxCreateRibbon();
            AddRibbonToForm();
            AsolFillRibbon();
        }
        private void AsolFillRibbon()
        {
            StoreContentData(this._DxRibbon, "MAIN PAGE", 1);
            StoreContentData(this._SlaveDxRibbon, "SLAVE PAGE", 2);
        }
        /// <summary>
        /// Vytvoří instance ribbonů ASOL
        /// </summary>
        private void DxCreateRibbon()
        {
            this._DxRibbon = new DxRibbonControl() { LogActive = true };
            this._DxRibbon.InitUserProperties(true);
            if (_UseVoidSlave)
            {
                this._VoidSlaveDxRibbon = new DxRibbonControl();
                this._VoidSlaveDxRibbon.InitUserProperties(false);
                this._VoidSlaveDxRibbon.Visible = false;
            }
            this._SlaveDxRibbon = new DxRibbonControl();
            this._SlaveDxRibbon.InitUserProperties(false);
            this.Ribbon = this._DxRibbon;
            this.SlaveRibbon = this._SlaveDxRibbon;

            this.Ribbon.ApplicationButtonText = "MAIN RIBBON";
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this.SlaveRibbon.ApplicationButtonText = (_UseVoidSlave ? "SLAVE - SLAVE RIBBON" : "SLAVE RIBBON");
            this.SlaveRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
        }
        private void AddRibbonToForm()
        {
            this.Ribbon.Dock = System.Windows.Forms.DockStyle.Top;
            this.Ribbon.Visible = true;
            this.SlaveRibbon.Dock = System.Windows.Forms.DockStyle.None;
            this.SlaveRibbon.Visible = true;
            this.Controls.Add(this.Ribbon);
            this.Controls.Add(this.SlaveRibbon);

            DxComponent.CreateDxSimpleButton(26, 430, 180, 35, this, "MERGE", _UseDataMergeClick);
            DxComponent.CreateDxSimpleButton(220, 430, 180, 35, this, "UNMERGE", _UseDataUnMergeClick);
            DoLayout();
        }
        #endregion



        private DxRibbonPage AddPageDirects(DxRibbonControl dxRibbon, string prefix, int pageIndex, int groupCount, int itemCount)
        {
            string text;

            text = $"{prefix} METHOD {pageIndex}";
            var dxPage = dxRibbon.CreatePage(text);

            for (int g = 0; g < groupCount; g++)
            {
                text = "Grupa METHOD " + (g + 1).ToString();
                var dxGroup = dxRibbon.CreateGroup(text, dxPage);

                for (int i = 0; i < itemCount; i++)
                {
                    text = "Button " + (++_ItemCount).ToString();
                    string svgImage = Random.GetItem(SvgImages);
                    BarItem item = this.Ribbon.Items.CreateButton(text);
                    item.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
                    item.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    dxGroup.ItemLinks.Add(item);
                }
            }
            return dxPage;
        }

        private void StorePageMethod(DxRibbonControl dxRibbon, string prefix, int pageIndex, int groupCount, int itemCount)
        {
            string text;

            text = $"{prefix} METHOD {pageIndex}";
            DataRibbonPage iPage = CreateIPage(text, 0, 0);
            var dxPage = dxRibbon.CreatePage(iPage);

            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                text = "Grupa METHOD " + (groupIndex + 1).ToString();
                var iGroup = CreateIGroup(text, groupIndex, 0);
                var dxGroup = dxRibbon.CreateGroup(iGroup, dxPage);

                for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    var iItem = CreateIItem(groupIndex);
                    dxRibbon.CreateItem(iItem, dxGroup);
                }
            }
        }

        private void StoreContentDataDirect(DxRibbonControl dxRibbon, string prefix, int pageCount)
        {
            List<IRibbonPage> iPages = new List<IRibbonPage>();
            for (int p = 0; p < pageCount; p++)
                iPages.Add(CreateIPage($"{prefix} TEST {p}", (pageCount == 1 ? 9 : (p == 0 ? 12 : 8)), (pageCount == 1 ? 8 : (p == 0 ? 6 : 9))));
            dxRibbon.AddPages(iPages);
            // dxRibbon.AddPagesTestGui(iPages);       ok
            // dxRibbon.AddPagesDirect(iPages);
        }
        private void StoreContentData(DxRibbonControl dxRibbon, string prefix, int pageCount)
        {
            List<IRibbonPage> iPages = new List<IRibbonPage>();

            //for (int p = 0; p < pageCount; p++)
            //    iPages.Add(CreateIPage($"{prefix} STANDARD {p}", (pageCount == 1 ? 9 : (p == 0 ? 12 : 8)), (pageCount == 1 ? 8 : (p == 0 ? 6 : 9))));
            //dxRibbon.AddPages(iPages, true);

            for (int p = 0; p < pageCount; p++)
            {
                int groupCount = (pageCount == 1 ? 8 : (p == 0 ? 6 : 9));
                var pages = DxRibbonSample.CreatePages(prefix, 1, 1, groupCount, groupCount, out var qatItems);
                iPages.AddRange(pages);
            }

            // Upravit všechny grupy na velký Auto state:
            var iGroups = iPages.Where(p => p.Groups != null).SelectMany(p => p.Groups).ToArray();
            iGroups.ForEachExec(g => _ModifyGroup(g));

            // Upravit všechny prvky na velký button:
            var iItems = iGroups.Where(g => g.Items != null).SelectMany(g => g.Items).ToArray();
            iItems.ForEachExec(i => _ModifyItem(i));

            dxRibbon.AddPages(iPages, true);
        }
        private void _ModifyGroup(Noris.Clients.Win.Components.AsolDX.IRibbonGroup iGroup)
        {
            if (!(iGroup is Noris.Clients.Win.Components.AsolDX.DataRibbonGroup dGroup)) return;
            dGroup.GroupState = RibbonGroupState.Auto;
        }
        private void _ModifyItem(Noris.Clients.Win.Components.AsolDX.IRibbonItem iItem)
        {
            if (!(iItem is Noris.Clients.Win.Components.AsolDX.DataRibbonItem dItem)) return;
            if (dItem.ItemType == RibbonItemType.Button) return;

            switch (dItem.ItemType)
            {
                case RibbonItemType.Menu:
                case RibbonItemType.SplitButton:
                    bool isDynamic = (dItem.SubItems is null || dItem.SubItemsContentMode != RibbonContentMode.Static);

                    if (isDynamic)
                    {
                        dItem.ItemType = RibbonItemType.Button;
                        dItem.SubItems = null;
                    }
                    else
                    {

                    }
                    break;

                case RibbonItemType.CheckBoxStandard:
                    dItem.ItemType = RibbonItemType.Button;
                    dItem.SubItems = null;
                    break;

                default:
                    dItem.ItemType = RibbonItemType.Button;
                    dItem.SubItems = null;
                    //dItem.RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large;
                    break;
            }
        }
        private DataRibbonPage CreateIPage(string pageText, int groupCount, int itemCount)
        {
            DataRibbonPage iPage = new DataRibbonPage() { PageText = pageText };
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
                iPage.Groups.Add(CreateIGroup($"Grupa DATA {groupIndex}", groupIndex, itemCount));
            return iPage;
        }
        private DataRibbonGroup CreateIGroup(string groupText, int groupIndex, int itemCount)
        {
            DataRibbonGroup iGroup = new DataRibbonGroup() { GroupText = groupText, GroupState = RibbonGroupState.Auto, ChangeMode = ContentChangeMode.ReFill, GroupButtonVisible = false };
            for (int i = 0; i < itemCount; i++)
                iGroup.Items.Add(CreateIItem(groupIndex));
            return iGroup;
        }
        private Noris.Clients.Win.Components.AsolDX.IRibbonItem CreateIItem(int groupIndex)
        {
            string text = "Button " + (++_ItemCount).ToString();
            string svgImage = Random.GetItem(SvgImages);
            int it = groupIndex % 5;
            switch (it)
            {
                case 0:
                    return new DataRibbonItem() 
                    { 
                        ItemType = RibbonItemType.Button,
                        Text = text, 
                        ImageName = svgImage, 
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large
                    };
                case 1:
                    return new DataRibbonItem()
                    {
                        ItemType = RibbonItemType.Button,
                        Text = text,
                        ImageName = svgImage,
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithText
                    };
                case 2:
                    return new DataRibbonItem()
                    {
                        ItemType = RibbonItemType.CheckBoxStandard,
                        Text = text,
                        ImageName = svgImage,
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithText
                    };
                case 3:
                    return new DataRibbonItem()
                    {
                        ItemType = RibbonItemType.Menu,
                        Text = text,
                        ImageName = svgImage,
                        SubItems = DxRibbonSample.CreateItems(3,6).ToList(),
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large
                    };
                case 4:
                    return new DataRibbonItem()
                    {
                        ItemType = RibbonItemType.Menu,
                        Text = text,
                        ImageName = svgImage,
                        SubItems = DxRibbonSample.CreateItems(3, 6).ToList(),
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithText
                    };
                default:
                    return new DataRibbonItem()
                    {
                        ItemType = RibbonItemType.Button,
                        Text = text,
                        ImageName = svgImage,
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large
                    };
            }
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            DoLayout();
        }
        protected void DoLayout()
        {
            if (this.SlaveRibbon != null)
                this.SlaveRibbon.Bounds = new System.Drawing.Rectangle(0, 220, ClientSize.Width, this.SlaveRibbon.Height);
        }
        private void _UseDataMergeClick(object sender, EventArgs args)
        {
            this.SlaveMerge();
        }
        private void _UseDataUnMergeClick(object sender, EventArgs args)
        {
            this.SlaveUnMerge();
        }
        private void SlaveMerge()
        {
            switch (_CreateMode)
            {
                case CreateMode.DevExpress:
                    this.Ribbon.MergeRibbon(this.SlaveRibbon);
                    break;
                case CreateMode.Tests:
                case CreateMode.Asol:
                    if (_UseVoidSlave)
                    {
                        this._VoidSlaveDxRibbon.MergeChildDxRibbon(this._SlaveDxRibbon);
                        this._DxRibbon.MergeChildDxRibbon(this._VoidSlaveDxRibbon);
                    }
                    else
                    {
                        this._DxRibbon.MergeChildDxRibbon(this._SlaveDxRibbon);
                    }
                    break;
            }
            DoLayout();
        }
        private void SlaveUnMerge()
        {
            switch (_CreateMode)
            {
                case CreateMode.DevExpress:
                    this.Ribbon.UnMergeRibbon();
                    break;
                case CreateMode.Tests:
                case CreateMode.Asol:
                    if (_UseVoidSlave)
                    {
                        this._DxRibbon.UnMergeDxRibbon();
                        this._VoidSlaveDxRibbon.UnMergeDxRibbon();
                    }
                    else
                    {
                        this._DxRibbon.UnMergeDxRibbon();
                    }
                    break;
            }
            DoLayout();
        }
        private int _ItemCount;
        private string[] SvgImages;
        private DevExpress.XtraBars.Ribbon.RibbonControl SlaveRibbon;
        private DevExpress.XtraBars.Ribbon.RibbonControl _Ribbon;
        private DevExpress.XtraBars.Ribbon.RibbonControl _SlaveRibbon;
        private DxRibbonControl _DxRibbon;
        private DxRibbonControl _VoidSlaveDxRibbon;
        private DxRibbonControl _SlaveDxRibbon;
        /// <summary>
        /// Režim vytváření pvků Ribbonu
        /// </summary>
        public enum CreateMode { DevExpress, Tests, Asol }
    }
}
