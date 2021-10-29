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
        #region Konstruktor a layout
        public NativeRibbonForm() : this(CreateMode.DevExpress, false) { }
        public NativeRibbonForm(CreateMode createMode, bool useVoidSlave)
        {
            _UseVoidSlave = useVoidSlave;
            _CreateMode = createMode;
            this.SvgImages = DxComponent.GetResourceKeys(false, true);
            InitRibbon();
            this.Size = new System.Drawing.Size(1000, 800);
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;

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
                    TestDxRibbonInit();
                    break;
                case CreateMode.Asol:
                    AsolDxRibbonInit();
                    break;
            }
        }
        private void AddRibbonsToForm()
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
        private string GetGroupText(int groupIndex)
        {
            string suffix = (groupIndex + 1).ToString();
            int it = groupIndex % 5;
            switch (it)
            {
                case 0: return $"LargeButtons {suffix}";
                case 1: return $"SmallButtons {suffix}";
                case 2: return $"LargeCheckBoxs {suffix}";
                case 3: return $"LargeMenu {suffix}";
                case 4: return $"SmallMenu {suffix}";
            }
            return $"LargeButtons {suffix}";
        }
        #endregion
        #region Native Ribbon
        private void DevExpressInit()
        {
            // NativeRibbonCreate();
            AsolDxRibbonCreate();

            AddRibbonsToForm();
            NativeRibbonFill();
        }
        /// <summary>
        /// Vytvoří instance ribbonů NATIVE
        /// </summary>
        private void NativeRibbonCreate()
        {
            this._Ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this._SlaveRibbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.Ribbon = this._Ribbon;
            this.SlaveRibbon = this._SlaveRibbon;

            this.Ribbon.ApplicationButtonText = "MAIN NATIVE RIBBON";
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this.SlaveRibbon.ApplicationButtonText = "SLAVE NATIVE RIBBON";
            this.SlaveRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
        }
        private void NativeRibbonFill()
        {
            var pageR = CreatePage("MAIN NATIVE PAGE", 9999);
            pageR.Groups.AddRange(this.CreateGroups(9, 8));
            this.Ribbon.Pages.Add(pageR);

            var page0 = CreatePage("SLAVE NATIVE PAGE", 0);
            page0.Groups.AddRange(this.CreateGroups(12, 6));
            this.SlaveRibbon.Pages.Add(page0);

            var page1 = CreatePage("SLAVE NATIVE PAGE", 1);
            page1.Groups.AddRange(this.CreateGroups(8, 9));
            this.SlaveRibbon.Pages.Add(page1);
        }
        private RibbonPage CreatePage(string prefix, int pageIndex)
        {
            string suffix = (pageIndex + 1).ToString();
            return new RibbonPage($"{prefix} {suffix}");
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
            string groupText = GetGroupText(groupIndex);
            RibbonPageGroup group = new RibbonPageGroup(groupText);
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
            switch (it)
            {
                case 0:
                    var item0 = this.Ribbon.Items.CreateButton(text);
                    item0.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
                    item0.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    return item0;
                case 1:
                    var item1 = this.Ribbon.Items.CreateButton(text);
                    item1.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText;
                    item1.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    return item1;
                case 2:
                    var item2 = this.Ribbon.Items.CreateCheckItem(text, false);
                    item2.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText;
                    item2.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    return item2;
                case 3:
                    var subItem30 = this.Ribbon.Items.CreateButton("Položka 1");
                    var subItem31 = this.Ribbon.Items.CreateButton("Položka 2");
                    var subItem32 = this.Ribbon.Items.CreateButton("Položka 3");
                    var item3 = this.Ribbon.Items.CreateMenu(text, subItem30, subItem31, subItem32);
                    item3.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
                    item3.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    return item3;
                case 4:
                    var subItem40 = this.Ribbon.Items.CreateButton("položka 1");
                    var subItem41 = this.Ribbon.Items.CreateButton("položka 2");
                    var subItem42 = this.Ribbon.Items.CreateButton("položka 3");
                    var item4 = this.Ribbon.Items.CreateMenu(text, subItem40, subItem41, subItem42);
                    item4.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText;
                    item4.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    return item4;
            }

            var itemX = this.Ribbon.Items.CreateButton(text);
            itemX.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            itemX.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
            return itemX;
        }
        #endregion
        #region Test Ribbon
        private void TestDxRibbonInit()
        {
            AsolDxRibbonCreate();
            AddRibbonsToForm();
            TestDxRibbonFill();
        }
        private void TestDxRibbonFill()
        {
            //StorePageMethod(this._DxRibbon, "MAIN PAGE", 9999, 9, 8);
            //StorePageMethod(this._SlaveDxRibbon, "SLAVE PAGE", 0, 12, 6);
            //StorePageMethod(this._SlaveDxRibbon, "SLAVE PAGE", 1, 8, 9);
            StoreContentDataDirect(this._DxRibbon, "MAIN TEST PAGE", 1);
            StoreContentDataDirect(this._SlaveDxRibbon, "SLAVE TEST PAGE", 2);
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
        /// <summary>
        /// Vytvoří stránku + grupy + itemy pomocí metod DxRibbonControl typu Create*() na základě jednoduchých dat, nikoli pomocí IRibbonData
        /// </summary>
        /// <param name="dxRibbon"></param>
        /// <param name="prefix"></param>
        /// <param name="pageIndex"></param>
        /// <param name="groupCount"></param>
        /// <param name="itemCount"></param>
        /// <returns></returns>
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
        private void StoreContentDataDirect(DxRibbonControl dxRibbon, string prefix, int pageCount)
        {
            var iPages = CreateIPagesTest(prefix, pageCount);

            // dxRibbon.AddPages(iPages);
            // dxRibbon.AddPagesTestGui(iPages);
            dxRibbon.AddPagesNative(iPages);
        }
        private List<IRibbonPage> CreateIPagesTest(string prefix, int pageCount)
        {
            List<IRibbonPage> iPages = new List<IRibbonPage>();
            for (int p = 0; p < pageCount; p++)
                iPages.Add(CreateIPage($"{prefix} {p}", (pageCount == 1 ? 9 : (p == 0 ? 12 : 8)), (pageCount == 1 ? 8 : (p == 0 ? 6 : 9))));
            return iPages;
        }
        private DataRibbonPage CreateIPage(string pagePrefix, int groupCount, int itemCount)
        {
            DataRibbonPage iPage = new DataRibbonPage() { PageText = pagePrefix };
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
                iPage.Groups.Add(CreateIGroup($"", groupIndex, itemCount));
            return iPage;
        }
        private DataRibbonGroup CreateIGroup(string groupPrefix, int groupIndex, int itemCount)
        {
            string groupText = groupPrefix + GetGroupText(groupIndex);
            DataRibbonGroup iGroup = new DataRibbonGroup() { GroupText = groupText, GroupState = RibbonGroupState.Auto, ChangeMode = ContentChangeMode.ReFill, GroupButtonVisible = false };
            for (int i = 0; i < itemCount; i++)
                iGroup.Items.Add(CreateIItem(groupIndex));
            return iGroup;
        }
        private Noris.Clients.Win.Components.AsolDX.IRibbonItem CreateIItem(int groupIndex)
        {
            string suffix = (++_ItemCount).ToString();
            string text = $"Item {suffix}";
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
                        RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large
                    };
                case 3:
                    return new DataRibbonItem()
                    {
                        ItemType = RibbonItemType.Menu,
                        Text = text,
                        ImageName = svgImage,
                        SubItems = DxRibbonSample.CreateItems(3, 6).ToList(),
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
        #endregion
        #region Asol Ribbon
        private void AsolDxRibbonInit()
        {
            AsolDxRibbonCreate();
            AddRibbonsToForm();
            AsolDxRibbonFill();
        }
        /// <summary>
        /// Vytvoří instance ribbonů ASOL
        /// </summary>
        private void AsolDxRibbonCreate()
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

            this.Ribbon.ApplicationButtonText = "MAIN ASOL RIBBON";
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this.SlaveRibbon.ApplicationButtonText = "SLAVE ASOL RIBBON";
            this.SlaveRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
        }
        private void AsolDxRibbonFill()
        {
            StoreContentData(this._DxRibbon, "MAIN PAGE", 1);
            StoreContentData(this._SlaveDxRibbon, "SLAVE PAGE", 2);
        }
        /// <summary>
        /// Vytvoří data (Pages + Groups + Items) IRibbonData, modifikuje je a vloží 
        /// </summary>
        /// <param name="dxRibbon"></param>
        /// <param name="prefix"></param>
        /// <param name="pageCount"></param>
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

            /*

            // Upravit všechny grupy na velký Auto state:
            var iGroups = iPages.Where(p => p.Groups != null).SelectMany(p => p.Groups).ToArray();
            iGroups.ForEachExec(g => _ModifyGroup(g));

            // Upravit všechny prvky na velký button:
            var iItems = iGroups.Where(g => g.Items != null).SelectMany(g => g.Items).ToArray();
            iItems.ForEachExec(i => _ModifyItem(i));

            */

            // dxRibbon.AddPages(iPages, true);
            dxRibbon.AddPagesNative(iPages);
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

        #endregion
        #region Merge - UnMerge
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
        #endregion
        #region Proměnné a enumy
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
        #endregion
    }
}
