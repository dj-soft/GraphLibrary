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
        public NativeRibbonForm() : this(CreateMode.Native, false) { }
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
            CreateRibbons();
            JoinRibbons();
            FillRibbons();
        }
        private void CreateRibbons()
        {
            _ItemCount = 0;
            switch (_CreateMode)
            {
                case CreateMode.Native:
                    this._Ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
                    this._SlaveRibbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
                    this.Ribbon = this._Ribbon;
                    this.SlaveRibbon = this._SlaveRibbon;
                    break;
                case CreateMode.UseClasses:
                case CreateMode.UseMethods:
                case CreateMode.UseData:
                    this._DxRibbon = new DxRibbonControl() { LogActive = true };
                    this._DxRibbon.InitUserProperties(true);
                    if (_UseVoidSlave)
                    {
                        this._VoidSlaveDxRibbon = new DxRibbonControl();
                        this._VoidSlaveDxRibbon.InitUserProperties(false);
                    }
                    this._SlaveDxRibbon = new DxRibbonControl();
                    this._SlaveDxRibbon.InitUserProperties(false);
                    this.Ribbon = this._DxRibbon;
                    this.SlaveRibbon = this._SlaveDxRibbon;
                    break;
            }
            this.Ribbon.ApplicationButtonText = "MAIN RIBBON";
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
            this.SlaveRibbon.ApplicationButtonText = (_UseVoidSlave ? "SLAVE - SLAVE RIBBON" : "SLAVE RIBBON");
            this.SlaveRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.True;
        }
        private void FillRibbons()
        {
            switch (_CreateMode)
            {
                case CreateMode.Native:
                case CreateMode.UseClasses:
                    var pageR = CreatePage("MAIN PAGE", 9999);
                    pageR.Groups.AddRange(this.CreateGroups(9, 8));
                    this.Ribbon.Pages.Add(pageR);

                    var page0 = CreatePage("SLAVE PAGE", 0);
                    page0.Groups.AddRange(this.CreateGroups(12, 6));
                    this.SlaveRibbon.Pages.Add(page0);

                    var page1 = CreatePage("SLAVE PAGE", 1);
                    page1.Groups.AddRange(this.CreateGroups(8, 9));
                    this.SlaveRibbon.Pages.Add(page1);
                    break;

                case CreateMode.UseMethods:
                    //StorePageMethod(this._DxRibbon, "MAIN PAGE", 9999, 9, 8);
                    //StorePageMethod(this._SlaveDxRibbon, "SLAVE PAGE", 0, 12, 6);
                    //StorePageMethod(this._SlaveDxRibbon, "SLAVE PAGE", 1, 8, 9);
                    StoreContentDataDirect(this._DxRibbon, "MAIN PAGE", 1);
                    StoreContentDataDirect(this._SlaveDxRibbon, "SLAVE PAGE", 2);
                    break;

                case CreateMode.UseData:
                    StoreContentData(this._DxRibbon, "MAIN PAGE", 1);
                    StoreContentData(this._SlaveDxRibbon, "SLAVE PAGE", 2);
                    break;
            }
        }
        private void JoinRibbons()
        {
            this.Ribbon.Dock = System.Windows.Forms.DockStyle.Top;
            this.Ribbon.Visible = true;
            this.SlaveRibbon.Dock = System.Windows.Forms.DockStyle.None;
            this.SlaveRibbon.Visible = true;
            this.Controls.Add(this.Ribbon);
            this.Controls.Add(this.SlaveRibbon);

            switch (_CreateMode)
            {
                case CreateMode.Native:
                    break;
                case CreateMode.UseClasses:
                case CreateMode.UseMethods:
                    if (_UseVoidSlave)
                        this._VoidSlaveDxRibbon.Visible = false;
                    break;
                case CreateMode.UseData:
                    if (_UseVoidSlave)
                        // Ten VoidSlave nebudu dávat do Controls!
                        this._VoidSlaveDxRibbon.Visible = false;
                    break;
            }
            DxComponent.CreateDxSimpleButton(26, 430, 180, 35, this, "MERGE", _UseDataMergeClick);
            DxComponent.CreateDxSimpleButton(220, 430, 180, 35, this, "UNMERGE", _UseDataUnMergeClick);
            DoLayout();
            // SlaveMerge();
        }
        private RibbonPage CreatePage(string prefix, int pageIndex)
        {
            string suffix = (pageIndex + 1).ToString();
            switch (_CreateMode)
            {
                case CreateMode.Native:
                    return new RibbonPage($"{prefix} NATIVE " + suffix);
                case CreateMode.UseClasses:
                    return new DxRibbonPage(this._DxRibbon, $"{prefix} CLASS " + suffix);
                case CreateMode.UseMethods:
                    return _SlaveDxRibbon.CreatePage($"{prefix} METHOD " + suffix);
            }
            return null;
        }
        private RibbonPageGroup[] CreateGroups(int groupCount, int itemCount)
        {
            List<RibbonPageGroup> groups = new List<RibbonPageGroup>();
            for (int g = 0; g < groupCount; g++)
            {
                RibbonPageGroup group = CreateGroup(g);
                group.ItemLinks.AddRange(this.CreateItems(itemCount));
                groups.Add(group);
            }
            return groups.ToArray();
        }
        private RibbonPageGroup CreateGroup(int groupIndex)
        {
            string suffix = (groupIndex + 1).ToString();
            switch (_CreateMode)
            {
                case CreateMode.Native:
                    RibbonPageGroup group = new RibbonPageGroup("Grupa NATIVE " + suffix);
                    group.State = RibbonPageGroupState.Auto;
                    return group;
                case CreateMode.UseClasses:
                    DxRibbonGroup dxGroup = new DxRibbonGroup("Grupa CLASS " + suffix);
                    dxGroup.State = RibbonPageGroupState.Auto;
                    return dxGroup;
                case CreateMode.UseMethods:
                    DxRibbonGroup mxGroup = _SlaveDxRibbon.CreateGroup("Grupa METHOD " + suffix, null);
                    return mxGroup;
            }
            return null;
        }
        private BarItem[] CreateItems(int itemCount)
        {
            List<BarItem> items = new List<BarItem>();
            for (int i = 0; i < itemCount; i++)
            {
                BarItem item = CreateItem(i);
                items.Add(item);
            }

            return items.ToArray();
        }
        private BarItem CreateItem(int itemIndex)
        {
            string suffix = (this.Ribbon.Items.Count + 1).ToString();
            string svgImage = Random.GetItem(SvgImages);
            string text = "Button " + suffix;
            switch (_CreateMode)
            {
                case CreateMode.Native:
                case CreateMode.UseClasses:
                case CreateMode.UseMethods:
                    BarItem item = this.Ribbon.Items.CreateButton(text);
                    item.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
                    item.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
                    return item;
            }
            return null;
        }

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

            for (int g = 0; g < groupCount; g++)
            {
                text = "Grupa METHOD " + (g + 1).ToString();
                var iGroup = CreateIGroup(text, 0);
                var dxGroup = dxRibbon.CreateGroup(iGroup, dxPage);

                for (int i = 0; i < itemCount; i++)
                {
                    var iItem = CreateIItem();
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
            for (int g = 0; g < groupCount; g++)
                iPage.Groups.Add(CreateIGroup($"Grupa DATA {g}", itemCount));
            return iPage;
        }
        private DataRibbonGroup CreateIGroup(string groupText, int itemCount)
        {
            DataRibbonGroup iGroup = new DataRibbonGroup() { GroupText = groupText, GroupState = RibbonGroupState.Auto, ChangeMode = ContentChangeMode.ReFill, GroupButtonVisible = false };
            for (int i = 0; i < itemCount; i++)
                iGroup.Items.Add(CreateIItem());
            return iGroup;
        }
        private Noris.Clients.Win.Components.AsolDX.IRibbonItem CreateIItem()
        {
            string svgImage = Random.GetItem(SvgImages);
            DataRibbonItem iItem = new DataRibbonItem() { Text = "Button " + (++_ItemCount).ToString(), ImageName = svgImage, RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large };
            return iItem;
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
                case CreateMode.Native:
                    this.Ribbon.MergeRibbon(this.SlaveRibbon);
                    break;
                case CreateMode.UseClasses:
                case CreateMode.UseMethods:
                case CreateMode.UseData:
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
                case CreateMode.Native:
                    this.Ribbon.UnMergeRibbon();
                    break;
                case CreateMode.UseClasses:
                case CreateMode.UseMethods:
                case CreateMode.UseData:
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
        public enum CreateMode { Native, UseClasses, UseMethods, UseData }
    }
}
