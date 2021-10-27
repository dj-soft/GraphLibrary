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
                    this._DxRibbon = new DxRibbonControl();
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
                case CreateMode.UseMethods:
                    var pageR = CreatePage("MAIN PAGE", 9999);
                    pageR.Groups.AddRange(this.CreateGroups(12, 6));
                    this.Ribbon.Pages.Add(pageR);

                    var page0 = CreatePage("SLAVE PAGE", 0);
                    page0.Groups.AddRange(this.CreateGroups(12, 6));
                    this.SlaveRibbon.Pages.Add(page0);

                    var page1 = CreatePage("SLAVE PAGE", 1);
                    page1.Groups.AddRange(this.CreateGroups(6, 4));
                    this.SlaveRibbon.Pages.Add(page1);
                    break;

                case CreateMode.UseData:
                    var iPagesM = CreateIDefiniton("MAIN PAGE", 1);
                    this._DxRibbon.AddPages(iPagesM);

                    var iPagesS = CreateIDefiniton("SLAVE PAGE", 2);
                    this._SlaveDxRibbon.AddPages(iPagesS);
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
        private IEnumerable<IRibbonPage> CreateIDefiniton(string prefix, int pageCount)
        {
            List<IRibbonPage> iPages = new List<IRibbonPage>();

            int icnt = 0;
            for (int p = 0; p < pageCount; p++)
            {
                DataRibbonPage iPage = new DataRibbonPage() { PageText = $"{prefix} {p}" };

                int groupCount = (p == 0 ? 12 : 6);
                int itemCount = (p == 0 ? 6 : 4);
                for (int g = 0; g < groupCount; g++)
                {
                    DataRibbonGroup iGroup = new DataRibbonGroup() { GroupText = $"Grupa DATA {g}", GroupState = RibbonGroupState.Auto, ChangeMode = ContentChangeMode.ReFill, GroupButtonVisible = false };
                    for (int i = 0; i < itemCount; i++)
                    {
                        string svgImage = Random.GetItem(SvgImages);
                        DataRibbonItem iItem = new DataRibbonItem() { Text = "Button " + (++icnt).ToString(), ImageName = svgImage, RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.Large };
                        iGroup.Items.Add(iItem);
                    }
                    iPage.Groups.Add(iGroup);
                }
                iPages.Add(iPage);
            }

            return iPages;
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
